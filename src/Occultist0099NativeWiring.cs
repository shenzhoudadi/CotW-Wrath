// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// D-0099-01 closure: the real native Wrath entry points that read and write the
// authoritative state in Occultist0099Rules.cs / Occultist0099Spells.cs.
//
// Three chains are wired here, and nothing else:
//   1. Mental Focus payment. Occultist0099FocusResourceLogic is a real
//      AbilityResourceLogic subclass. The native cast pipeline reaches it via
//      BlueprintAbility.GetComponent<AbilityResourceLogic>() and
//      AbilityCastRateUtils.SpendResources -> AbilityData.ResourceLogic.Spend.
//      The UI precheck (IAbilityRestriction) and the actual execution therefore
//      call the SAME authoritative logic; there is no mirrored spend counter.
//   2. Spell learning. Occultist0099LearnGate sits on the private spell roots and
//      gates the native SelectSpell.Check / Spellbook.AddKnown paths, so a
//      refused choice never reaches SpellSelectionData.SpendSlot or AddKnown.
//   3. Rest / level identity. Occultist0099SpellbookFact carries the per-unit
//      spell state and resynchronises it from the real ClassData.Level and the
//      real Intelligence stat on rest and on level-up.
//
// Ownership boundary: every gate below first proves that the acting spellbook,
// caster and root belong to the 0099 private slice. Foreign books, foreign
// resources, items and other units fall through to unchanged native behaviour.
//
// This slice is development-only. Nothing here registers a class, spellbook or
// progression blueprint in the shared registration chain.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    // ---------------------------------------------------------------------
    // Shared resolution helpers. Every native entry point below uses these, so
    // the ownership proof is written once and cannot drift between the UI
    // precheck, the payment and the learning gate.
    // ---------------------------------------------------------------------
    public static class Occultist0099NativeScope
    {
        // The one fact that owns the authoritative Mental Focus state.
        public static Occultist0099FocusFact0099 FocusOf(UnitEntityData unit)
        {
            if (unit == null || unit.Descriptor == null) return null;
            var fact = unit.Descriptor.Progression?.Features?.Enumerable
                .FirstOrDefault(f => !f.IsDisposed &&
                    f.Blueprint != null &&
                    f.Blueprint.AssetGuid.ToString() == Occultist0099Ids.FocusFactGuid);
            if (fact == null) return null;
            foreach (var runtime in fact.Components
                .Where(r => !r.IsDisposed &&
                            r.SourceBlueprintComponent is Occultist0099FocusFact0099))
                return (Occultist0099FocusFact0099)runtime.SourceBlueprintComponent;
            return null;
        }

        public static Occultist0099SpellbookFact0099 BookOf(UnitEntityData unit)
        {
            if (unit == null || unit.Descriptor == null) return null;
            var fact = unit.Descriptor.Progression?.Features?.Enumerable
                .FirstOrDefault(f => !f.IsDisposed &&
                    f.Blueprint != null &&
                    f.Blueprint.AssetGuid.ToString() == Occultist0099Ids.SpellbookFactGuid);
            if (fact == null) return null;
            foreach (var runtime in fact.Components
                .Where(r => !r.IsDisposed &&
                            r.SourceBlueprintComponent is Occultist0099SpellbookFact0099))
                return (Occultist0099SpellbookFact0099)runtime.SourceBlueprintComponent;
            return null;
        }

        // A book is the 0099 private book only when it carries the private
        // marker AND its identity is the private BookGuid. Sharing a spell list
        // with a foreign class never makes a foreign book ours.
        public static bool IsPrivateBook(BlueprintSpellbook book)
        {
            if (book == null) return false;
            if (book.AssetGuid.ToString() != Occultist0099Ids.BookGuid) return false;
            return book.GetComponent<Occultist0099SpellbookMarker0099>() != null;
        }

        public static bool IsPrivateBook(Spellbook book)
            => book != null && IsPrivateBook(book.Blueprint);

        // The root must be one of the slice's audited Abjuration identities.
        public static Occultist0099SpellRoot RootOf(BlueprintAbility ability)
        {
            if (ability == null) return null;
            return Occultist0099AbjurationCatalog.Find(ability.AssetGuid.ToString());
        }

        // The live runtime component of a fact, or null. The runtime (not the
        // blueprint component) is what owns the per-unit data and can open the
        // native event context.
        public static EntityFactComponent RuntimeOf(UnitEntityData unit, string factGuid,
            Type componentType)
        {
            if (unit == null) return null;
            var fact = unit.Descriptor?.Progression?.Features?.Enumerable
                .FirstOrDefault(f => !f.IsDisposed && f.Blueprint != null &&
                                     f.Blueprint.AssetGuid.ToString() == factGuid);
            if (fact == null) return null;
            return fact.Components.FirstOrDefault(r => !r.IsDisposed &&
                componentType.IsInstanceOfType(r.SourceBlueprintComponent));
        }

        // The unit a fact runtime is actually attached to, read from the native
        // EntityFactComponent.Owner. This works OUTSIDE an event context, which
        // is exactly where the native ability/resource pipeline calls us.
        public static UnitEntityData OwnerOf(EntityFactComponent runtime)
            => runtime?.Owner as UnitEntityData;

        // Runs a body inside the real native component event context of the
        // first matching runtime of a fact, exactly as the shipped fact runtimes
        // do. Without this the Owner/Data accessors of a
        // UnitFactComponentDelegate have no runtime.
        public static bool InFactContext(UnitEntityData unit, string factGuid,
            Type componentType, Action<BlueprintComponent> body)
            => InRuntimeContext(RuntimeOf(unit, factGuid, componentType), body);

        // Same, for a runtime the caller already resolved. Every native entry
        // point below resolves the runtime once, proves ownership from it, and
        // then opens its context, so the precheck and the payment of one cast
        // read the same per-unit data.
        public static bool InRuntimeContext(EntityFactComponent runtime,
            Action<BlueprintComponent> body)
        {
            if (runtime == null || body == null) return false;
            using (runtime.RequestEventContext())
                body(runtime.SourceBlueprintComponent);
            return true;
        }

        public static T InRuntimeContext<T>(EntityFactComponent runtime,
            Func<BlueprintComponent, T> body, T absent)
        {
            if (runtime == null || body == null) return absent;
            using (runtime.RequestEventContext())
                return body(runtime.SourceBlueprintComponent);
        }
    }

    // ---------------------------------------------------------------------
    // 1. Mental Focus: real AbilityResourceLogic subclass.
    //
    // BlueprintAbilityResource.GetMaxAmount is NOT virtual, so the authoritative
    // budget cannot be expressed as a native resource maximum. Instead the
    // native resource is only a display mirror: AddAbilityResources attaches it
    // with RestoreAmount=false, and this component synchronises its Amount from
    // the authoritative Available after every accepted transaction. The mirror
    // is never debited by this component, so the Kingmaker ConnectResource path
    // cannot produce a second charge.
    // ---------------------------------------------------------------------
    [TypeId(Occultist0099Ids.FocusResourceLogicTypeId)]
    public sealed class Occultist0099FocusResourceLogic : AbilityResourceLogic
    {
        // The school whose ordinary pool R pays for this focus power.
        public string School = Occultist0099Ids.SliceSchool;

        // A Panoply power projects over its member pools instead. Exactly one of
        // School / Panoply is used; Panoply wins when set.
        public string Panoply;

        // Repower Construct spends from the generic pool G, not from a school.
        public bool GenericPool;

        // The native ability pipeline calls this component OUTSIDE any fact
        // event context, so it must resolve the live runtime and open that
        // runtime's own context before touching Owner / Data. Ownership is
        // proven from the native EntityFactComponent.Owner, which is readable
        // without a context.
        private static EntityFactComponent Runtime(AbilityData ability)
        {
            var unit = ability?.Caster?.Unit;
            if (unit == null) return null;
            var runtime = Occultist0099NativeScope.RuntimeOf(unit,
                Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099));
            return Occultist0099NativeScope.OwnerOf(runtime) == unit ? runtime : null;
        }

        // Stable per-cast identity. AbilityData.UniqueId is minted once per cast
        // by the native constructor, so the precheck and the spend of ONE cast
        // share it, while two different casts never collapse into one charge.
        private static string ExecutionId(AbilityData ability)
            => ability == null ? null : ability.UniqueId;

        public override int CalculateCost(AbilityData ability) => Amount;

        public override bool IsAbilityRestrictionPassed(AbilityData ability)
        {
            string id = ExecutionId(ability);
            return Occultist0099NativeScope.InRuntimeContext(Runtime(ability),
                component =>
                {
                    var fact = (Occultist0099FocusFact0099)component;
                    var unit = ability.Caster.Unit;
                    if (GenericPool) return fact.CanPayGeneric(unit, Amount, id);
                    if (!string.IsNullOrEmpty(Panoply))
                        return fact.CanPayPanoply(unit, Panoply, Amount, id);
                    return fact.CanPaySchool(unit, School, Amount, id);
                }, false);
        }

        public override string GetAbilityRestrictionUIText()
            => "Not enough mental focus invested in this implement.";

        public override void Spend(AbilityData ability)
        {
            string id = ExecutionId(ability);
            Occultist0099NativeScope.InRuntimeContext(Runtime(ability), component =>
            {
                var fact = (Occultist0099FocusFact0099)component;
                var unit = ability.Caster.Unit;
                Occultist0099Payment payment;
                if (GenericPool)
                    payment = fact.TryPayGeneric(unit, Amount, id);
                else if (!string.IsNullOrEmpty(Panoply))
                    payment = fact.TryPayPanoply(unit, Panoply, Amount, id);
                else
                    payment = fact.TryPaySchool(unit, School, Amount, id);
                // A refused transaction leaves the authoritative state, the
                // native mirror and every effect untouched (MF01 / MF11).
                if (!payment.Applied) return;
                fact.SyncResourceMirror();
            });
        }
    }

    // ---------------------------------------------------------------------
    // 2. Spell learning gate. Attached to the private spell roots, it is read by
    // the native SelectSpell.Check postfix and by the Spellbook.AddKnown prefix.
    // The gate never grants anything by itself: it only refuses, so a rejected
    // choice cannot reach SpendSlot / AddKnown at all.
    // ---------------------------------------------------------------------
    [TypeId(Occultist0099Ids.LearnGateTypeId)]
    public sealed class Occultist0099LearnGate : BlueprintComponent
    {
        // The private circle of this root. It is the 0099 circle, not the
        // native circle of the donor spell.
        public int Circle;

        // Public so the directed native host can prove the gate really is
        // attached to the audited root through the native GetComponent path.
        public static Occultist0099LearnGate For(BlueprintAbility ability)
        {
            if (ability == null) return null;
            if (Occultist0099NativeScope.RootOf(ability) == null) return null;
            return ability.GetComponent<Occultist0099LearnGate>();
        }

        // A learning attempt is legal only when the acting book is the private
        // book, the owner actually holds the 0099 spell state, and the
        // authoritative candidate list still offers this canonical root for one
        // of the owner's open implement quotas at this circle.
        // The level-up pipeline calls this OUTSIDE any fact event context, so
        // the live runtime is resolved first and its own native context opened
        // before the per-unit spell state is read.
        internal bool CanLearn(UnitDescriptor owner, BlueprintSpellbook bookBlueprint,
            int spellLevel)
        {
            if (owner == null || !Occultist0099NativeScope.IsPrivateBook(bookBlueprint))
                return false;
            if (spellLevel != Circle) return false;
            string rootGuid = Occultist0099NativeScope.RootOf(
                OwnerBlueprint as BlueprintAbility)?.Guid;
            if (string.IsNullOrEmpty(rootGuid)) return false;
            var runtime = Occultist0099NativeScope.RuntimeOf(owner.Unit,
                Occultist0099Ids.SpellbookFactGuid, typeof(Occultist0099SpellbookFact0099));
            if (Occultist0099NativeScope.OwnerOf(runtime) != owner.Unit) return false;
            return Occultist0099NativeScope.InRuntimeContext(runtime,
                component => ((Occultist0099SpellbookFact0099)component).CanLearnRoot(
                    owner.Unit, owner.GetSpellbook(bookBlueprint), Circle, rootGuid),
                false);
        }
    }

    // ---------------------------------------------------------------------
    // 3. Per-unit spell state holder + native resynchronisation.
    // ---------------------------------------------------------------------
    public sealed class Occultist0099SpellbookData
    {
        [JsonProperty] public Occultist0099SpellState State;
    }

    [TypeId(Occultist0099Ids.SpellbookFactTypeId)]
    public sealed class Occultist0099SpellbookFact0099 :
        UnitFactComponentDelegate<Occultist0099SpellbookData>,
        IUnitRestHandler, IOwnerGainLevelHandler, IUnitSubscriber
    {
        public BlueprintCharacterClassReference CharacterClass;

        private Occultist0099SpellState State
        {
            get
            {
                if (Data.State == null)
                    Data.State = Occultist0099Spells.Create(0, IntelligenceScore());
                return Data.State;
            }
        }

        private int ClassLevel()
        {
            var cls = CharacterClass?.Get();
            if (cls == null || Owner == null) return 0;
            return Owner.Progression.GetClassLevel(cls);
        }

        private int IntelligenceScore()
            => Owner?.Stats?.Intelligence?.ModifiedValue ?? 10;

        // Level and Int are read from the real native ClassData / stat, never
        // from a mirrored counter. Lowering Int gates further learning but keeps
        // every established quota and source (C:31-32).
        internal void Sync()
        {
            var state = State;
            Occultist0099Spells.SetLevel(state, ClassLevel());
            Occultist0099Spells.SetIntelligence(state, IntelligenceScore());
        }

        protected override void OnActivate() => Sync();
        protected override void OnPostLoad() => Sync();
        protected override void OnApplyPostLoadFixes() => Sync();

        public void HandleUnitRest(UnitEntityData unit)
        {
            if (unit == null || unit != Owner) return;
            Sync();
        }

        public void HandleUnitGainLevel() => Sync();

        public bool IsOwnedBy(UnitEntityData unit) => unit != null && unit == Owner;

        // The authoritative candidate check used by the native learning gate.
        // A root is learnable when at least one open quota of one owned
        // implement offers it right now.
        public bool CanLearnRoot(UnitEntityData unit, Spellbook book, int circle,
            string rootGuid)
        {
            if (!IsOwnedBy(unit) || string.IsNullOrEmpty(rootGuid)) return false;
            if (!Occultist0099NativeScope.IsPrivateBook(book)) return false;
            var state = State;
            string canonical = Occultist0099Spells.CanonicalOf(state, rootGuid);
            return state.Slots.Any(slot =>
                Occultist0099Spells.Candidates(state, slot.Key, circle)
                    .Any(r => r.Guid == canonical));
        }

        // Records an accepted native learning choice against the exact quota
        // that offered it. Called only after the native gate passed.
        public Occultist0099Outcome RecordLearn(UnitEntityData unit, Spellbook book,
            int circle, string rootGuid)
        {
            if (!IsOwnedBy(unit) || !Occultist0099NativeScope.IsPrivateBook(book))
                return Occultist0099Outcome.RejectedNotOwned;
            var state = State;
            string canonical = Occultist0099Spells.CanonicalOf(state, rootGuid);
            foreach (var slot in state.Slots)
            {
                if (!Occultist0099Spells.Candidates(state, slot.Key, circle)
                        .Any(r => r.Guid == canonical)) continue;
                return Occultist0099Spells.Learn(state, slot.Key, circle, canonical);
            }
            return Occultist0099Outcome.RejectedInsufficient;
        }

        public string[] KnownRoots() => Occultist0099Spells.KnownRoots(State);
        public string StateFingerprint() => State.Fingerprint();
        public int OpenQuotaCount(int circle)
            => Occultist0099Spells.QuotaCount(State, circle);
    }

    // ---------------------------------------------------------------------
    // Native bridges. Each one is scoped to the 0099 private identities first;
    // anything else returns immediately and native behaviour is unchanged.
    // ---------------------------------------------------------------------
    [HarmonyPatch(typeof(SelectSpell), nameof(SelectSpell.Check))]
    internal static class Occultist0099NativeSelectCheck
    {
        private static void Postfix(SelectSpell __instance, UnitDescriptor __1,
            ref bool __result)
        {
            if (!__result) return;
            var gate = Occultist0099LearnGate.For(__instance.Spell);
            if (gate == null) return;
            __result = gate.CanLearn(__1, __instance.Spellbook, __instance.SpellLevel);
        }
    }

    // The commit path. LevelUpController.ApplyLevelUpActions calls Check then
    // Apply; this prefix is the second, independent refusal point so that even a
    // caller that skips Check cannot make the private book learn an ineligible
    // root. A refusal returns null and never touches the known-spell lists.
    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.AddKnown))]
    internal static class Occultist0099NativeLearnCommit
    {
        private static bool Prefix(Spellbook __instance, int __0, BlueprintAbility __1,
            ref AbilityData __result)
        {
            var gate = Occultist0099LearnGate.For(__1);
            if (gate == null) return true;
            if (!Occultist0099NativeScope.IsPrivateBook(__instance)) return true;
            var root = Occultist0099NativeScope.RootOf(__1);
            var unit = __instance.Owner?.Unit;
            bool allowed = false;
            bool reached = Occultist0099NativeScope.InFactContext(unit,
                Occultist0099Ids.SpellbookFactGuid,
                typeof(Occultist0099SpellbookFact0099),
                component =>
                {
                    var holder = (Occultist0099SpellbookFact0099)component;
                    allowed = holder.CanLearnRoot(unit, __instance, __0, root.Guid) &&
                        holder.RecordLearn(unit, __instance, __0, root.Guid) ==
                            Occultist0099Outcome.Ok;
                });
            // No holder, or a refused authoritative transaction: the native
            // known-spell lists, the selection slots and the quota all stay
            // exactly as they were.
            if (!reached || !allowed) { __result = null; return false; }
            return true;
        }
    }
}
