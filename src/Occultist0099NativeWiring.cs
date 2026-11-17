// COTW-0099 development adapter. No class or spellbook is registered here.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.DLC;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
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
        // The active fact that exposes the unit's Mental Focus ledger.
        public static Occultist0099FocusFact0099 FocusOf(UnitEntityData unit)
            => RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099))
                ?.SourceBlueprintComponent as Occultist0099FocusFact0099;

        internal static EntityFactComponent FocusRuntime(AbilityData ability)
        {
            var unit = ability?.Caster?.Unit;
            var granted = ability?.Fact;
            if (unit == null || ability.SourceItem != null || granted == null || !granted.Active || granted.Owner != unit)
                return null;
            var runtime = RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099));
            return OwnerOf(runtime) == unit ? runtime : null;
        }

        public static Occultist0099SpellbookFact0099 BookOf(UnitEntityData unit)
            => RuntimeOf(unit, Occultist0099Ids.SpellbookFactGuid, typeof(Occultist0099SpellbookFact0099))
                ?.SourceBlueprintComponent as Occultist0099SpellbookFact0099;

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
            Type componentType, bool requireActive = true)
        {
            if (unit == null) return null;
            var fact = unit.Descriptor?.Progression?.Features?.Enumerable
                .FirstOrDefault(f => !f.IsDisposed && (!requireActive || f.Active) && f.Blueprint != null &&
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
        public bool CheckCasterRestrictions;
        public bool PayOnEffect;

        // The native ability pipeline calls this component OUTSIDE any fact
        // event context, so it must resolve the live runtime and open that
        // runtime's own context before touching Owner / Data. Ownership is
        // proven from the native EntityFactComponent.Owner, which is readable
        // without a context.
        public override int CalculateCost(AbilityData ability) => Amount;

        public override bool IsAbilityRestrictionPassed(AbilityData ability)
            => CanPay(ability, null);

        internal bool CanPay(AbilityData ability, string identity)
        {
            if (!CasterRestrictionsPass(ability)) return false;
            return Occultist0099NativeScope.InRuntimeContext(Occultist0099NativeScope.FocusRuntime(ability),
                component =>
                {
                    var fact = (Occultist0099FocusFact0099)component;
                    var unit = ability.Caster.Unit;
                    if (GenericPool) return fact.CanPayGeneric(unit, Amount, identity);
                    if (!string.IsNullOrEmpty(Panoply))
                        return fact.CanPayPanoply(unit, Panoply, Amount, identity);
                    return fact.CanPaySchool(unit, School, Amount, identity);
                }, false);
        }

        public override string GetAbilityRestrictionUIText()
            => "Not enough mental focus invested in this implement.";

        // Native SpendResources runs after execution (and on held touch
        // delivery). The rule adapter is the only cast payment entry.
        public override void Spend(AbilityData ability) { }

        private bool CasterRestrictionsPass(AbilityData ability)
            => !CheckCasterRestrictions || (ability?.Caster?.Unit != null &&
                ability.Blueprint.CasterRestrictions.All(restriction => restriction.IsCasterRestrictionPassed(ability.Caster.Unit)));

        internal bool TryPay(AbilityData ability, string identity)
        {
            if (!CasterRestrictionsPass(ability)) return false;
            return Occultist0099NativeScope.InRuntimeContext(Occultist0099NativeScope.FocusRuntime(ability), component =>
            {
                var fact = (Occultist0099FocusFact0099)component;
                var unit = ability.Caster.Unit;
                Occultist0099Payment payment;
                if (GenericPool)
                    payment = fact.TryPayGeneric(unit, Amount, identity);
                else if (!string.IsNullOrEmpty(Panoply))
                    payment = fact.TryPayPanoply(unit, Panoply, Amount, identity);
                else
                    payment = fact.TryPaySchool(unit, School, Amount, identity);
                if (payment.Applied) fact.SyncResourceMirror();
                return payment.Applied;
            }, false);
        }
    }

    [HarmonyPatch(typeof(RuleCastSpell), nameof(RuleCastSpell.OnTrigger))]
    internal static class Occultist0099NativeCast
    {
        private static readonly ConditionalWeakTable<object, string> Identities =
            new ConditionalWeakTable<object, string>();
        private static bool paymentHookInstalled;

        internal static string IdentityOf(object execution)
            => Identities.GetValue(execution, _ => Guid.NewGuid().ToString("N"));

        internal static bool TryGetPaymentIdentity(AbilityExecutionContext context, out string identity)
            => Identities.TryGetValue(context, out identity);

        internal static RuleCastSpell BindCommand(RuleCastSpell rule, UnitUseAbility command)
        {
            if (IsFocusCast(rule.Spell) || FocusOperation(rule.Spell) != null ||
                Occultist0099NativeScope.IsPrivateBook(rule.Spell?.Spellbook))
                Identities.GetValue(rule, _ => IdentityOf(command));
            Occultist0099Paraphernalia.BindCommand(rule, command);
            return rule;
        }

        private static bool IsFocusCast(AbilityData spell)
            => spell?.StickyTouch == null && spell?.ResourceLogic is Occultist0099FocusResourceLogic;

        private static Occultist0099FocusOperation FocusOperation(AbilityData spell)
            => spell?.Blueprint.GetComponent<Occultist0099FocusOperation>();

        private static bool Reject(RuleCastSpell rule)
        {
            rule.ForceFail = true;
            rule.SetSuccess(false);
            return false;
        }

        private static bool Prefix(RuleCastSpell __instance)
        {
            if (!Occultist0099NativeSpellGate.AllowsCast(__instance?.Spell)) return Reject(__instance);
            return paymentHookInstalled || (!IsFocusCast(__instance.Spell) && FocusOperation(__instance.Spell) == null) || Reject(__instance);
        }

        private static bool Commit(RuleCastSpell rule)
        {
            var spell = rule.Spell;
            if (!Occultist0099NativeSpellGate.AllowsCast(spell)) return Reject(rule);
            var operation = FocusOperation(spell);
            if (operation == null && !IsFocusCast(spell))
            {
                if (!Occultist0099Paraphernalia.TryCast(rule)) return Reject(rule);
                Occultist0099Divination.TryEmpower(rule);
                return true;
            }
            bool duplicate = rule.IsDuplicateSpellApplied || rule.Context?.IsDuplicateSpellApplied == true;
            if (operation != null)
            {
                if (duplicate || rule.Context == null) return Reject(rule);
                string identity = IdentityOf(rule);
                if (!operation.CanExecute(spell, identity)) return Reject(rule);
                Identities.GetValue(rule.Context, _ => identity);
                return true;
            }
            var logic = (Occultist0099FocusResourceLogic)spell.ResourceLogic;
            if (logic.PayOnEffect)
            {
                if (duplicate || rule.Context == null) return Reject(rule);
                string identity = IdentityOf(rule);
                if (!logic.CanPay(spell, identity)) return Reject(rule);
                Identities.GetValue(rule.Context, _ => identity);
                return true;
            }
            return (!duplicate && logic.TryPay(spell, IdentityOf(rule))) || Reject(rule);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator il)
        {
            var code = instructions.ToList();
            // The first failure-chance test follows RuleApplySpell.Before,
            // target validation and UMD. Once here a native spell-failure roll
            // still counts as an attempt, even if the effects never run.
            var chance = AccessTools.PropertyGetter(typeof(RuleCastSpell), nameof(RuleCastSpell.SpellFailureChance));
            int index = code.FindIndex(i => i.Calls(chance)) - 1;
            paymentHookInstalled = index >= 0 && code[index].opcode == OpCodes.Ldarg_0;
            if (!paymentHookInstalled) return code;

            var resume = il.DefineLabel();
            var loadRule = new CodeInstruction(OpCodes.Ldarg_0);
            loadRule.labels.AddRange(code[index].labels);
            code[index].labels.Clear();
            code[index].labels.Add(resume);
            code.InsertRange(index, new[]
            {
                loadRule,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Occultist0099NativeCast), nameof(Commit))),
                new CodeInstruction(OpCodes.Brtrue, resume),
                new CodeInstruction(OpCodes.Ret)
            });
            return code;
        }
    }

    [HarmonyPatch(typeof(UnitUseAbility), "OnAction")]
    internal static class Occultist0099NativeCommandCast
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.operand is System.Reflection.MethodInfo method &&
                    method.DeclaringType == typeof(Rulebook) && method.Name == nameof(Rulebook.Trigger) &&
                    method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(RuleCastSpell))
                {
                    var load = new CodeInstruction(OpCodes.Ldarg_0);
                    load.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    yield return load;
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Occultist0099NativeCast), nameof(Occultist0099NativeCast.BindCommand)));
                }
                yield return instruction;
            }
        }
    }

    // Concentration failure happens before the engine creates a RuleCastSpell.
    [HarmonyPatch(typeof(UnitUseAbility), "FailIfConcentrationCheckFailed")]
    internal static class Occultist0099NativeConcentrationFailure
    {
        private static void Postfix(UnitUseAbility __instance, bool __result)
        {
            if (!__result || __instance.Ability.StickyTouch != null ||
                !(__instance.Ability.ResourceLogic is Occultist0099FocusResourceLogic logic)) return;
            logic.TryPay(__instance.Ability, Occultist0099NativeCast.IdentityOf(__instance));
        }
    }

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

        protected override void OnActivate() => RestoreImplementSources();
        protected override void OnPostLoad() => Sync();
        protected override void OnApplyPostLoadFixes() => RestoreImplementSources();

        private void RestoreImplementSources()
        {
            Sync();
            foreach (var source in Owner.Progression.Features.Enumerable.Where(fact => fact.Active && !fact.IsDisposed)
                         .SelectMany(fact => fact.Blueprint.GetComponents<Occultist0099ImplementSlot>())
                         .OrderBy(source => source.ExtraSpells || source.Panoply != null))
                Occultist0099Spells.AddSlot(State, source.SlotKey, source.School, source.ExtraSpells, source.Panoply);
        }

        public void HandleUnitRest(UnitEntityData unit)
        {
            if (unit == null || unit != Owner) return;
            Sync();
        }

        public void HandleUnitGainLevel() => Sync();

        internal void ApplyImplement(Occultist0099ImplementSlot implement, bool present)
        {
            Sync();
            var state = State;
            if (present)
            {
                if (Occultist0099Spells.AddSlot(state, implement.SlotKey, implement.School, implement.ExtraSpells, implement.Panoply)
                        == Occultist0099Outcome.Ok && !implement.ExtraSpells && implement.Panoply == null)
                    RestoreImplementSources();
                return;
            }
            if (!state.Slots.Any(slot => slot.Key == implement.SlotKey && slot.School == implement.School &&
                    slot.ExtraSpells == implement.ExtraSpells && slot.Panoply == implement.Panoply)) return;
            var previous = Occultist0099Spells.KnownRoots(state);
            if (Occultist0099Spells.RemoveSlot(state, implement.SlotKey) != Occultist0099Outcome.Ok) return;
            var removed = new HashSet<string>(previous.Except(Occultist0099Spells.KnownRoots(state)));
            if (removed.Count == 0) return;
            var book = Owner.Descriptor.GetSpellbook(CharacterClass?.Get()?.Spellbook);
            if (!Occultist0099NativeScope.IsPrivateBook(book)) return;
            foreach (var spell in book.GetAllKnownSpells().Where(ability => removed.Contains(ability.Blueprint.AssetGuid.ToString()))
                         .Select(ability => ability.Blueprint).Distinct().ToArray())
                book.RemoveSpell(spell);
        }

        internal bool CanLearn(Spellbook book, Occultist0099ImplementSpellList source, int circle,
            BlueprintAbility spell)
        {
            if (book?.Owner?.Unit != Owner || spell == null || book.IsKnown(spell))
                return false;

            var root = Occultist0099NativeScope.RootOf(spell);
            int intelligence = IntelligenceScore();
            int level = ClassLevel();
            if (root == null || circle != root.Circle || root.IsKnack || spell.Type != AbilityType.Spell ||
                spell.Parent != null || spell.GetComponent<SpellComponent>()?.School.ToString() != root.School ||
                intelligence < 10 + circle || circle > Occultist0099AbjurationCatalog.MaxCircleForLevel(level) ||
                !book.Blueprint.SpellList.GetSpells(circle).Contains(spell))
                return false;

            var state = State;
            if (state.Intelligence != intelligence || state.ClassLevel != level)
            {
                state = state.Clone();
                Occultist0099Spells.SetLevel(state, level);
                Occultist0099Spells.SetIntelligence(state, intelligence);
            }
            return state.Slots.Any(source.Matches) &&
                Occultist0099Spells.Candidates(state, source.SlotKey, circle, r => r.Guid == root.Guid).Any();
        }

        internal void RefreshOffers(LevelUpState levelUp, Spellbook book)
        {
            Sync();
            var state = State;
            foreach (var source in book.Blueprint.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists)
            {
                var list = source.List?.Get();
                if (list == null || list == book.Blueprint.SpellList) continue;
                var previous = levelUp.GetSpellSelection(book.Blueprint, list);
                var next = new Occultist0099NativeSpellOffer(book.Blueprint, list);
                bool changed = false;
                bool ownsSource = state.Slots.Any(source.Matches);
                for (int circle = 1; circle <= 6; circle++)
                {
                    var old = previous?.LevelCount[circle]?.SpellSelections;
                    var selected = old?.FirstOrDefault();
                    if (selected != null && (!state.Quotas.TryGetValue(
                            Occultist0099Spells.QuotaKey(source.SlotKey, circle), out var guid) ||
                            guid != selected.AssetGuid.ToString() || !ownsSource)) selected = null;
                    var visible = next.Visible[circle];
                    if (selected != null) visible.Add(selected);
                    else if (ownsSource && Occultist0099Spells.IntAllowsCircle(state, circle))
                        foreach (var spell in list.GetSpells(circle))
                            if (CanLearn(book, source, circle, spell)) visible.Add(spell);
                    if (visible.Count > 0)
                    {
                        next.SetLevelSpells(circle, 1);
                        next.LevelCount[circle].SpellSelections[0] = selected;
                    }
                    var priorVisible = (previous as Occultist0099NativeSpellOffer)?.Visible[circle];
                    changed |= (visible.Count > 0) != (old?.Length == 1) || selected != old?.FirstOrDefault() ||
                        (priorVisible != null && !visible.SetEquals(priorVisible));
                }
                if (!changed) continue;
                if (previous != null) levelUp.SpellSelections.Remove(previous);
                if (next.LevelCount.Any(row => row != null)) levelUp.SpellSelections.Add(next);
            }
        }

        internal bool RecordLearn(Spellbook book, string slot, int circle, BlueprintAbility spell)
        {
            if (!book.Blueprint.SpellList.GetSpells(circle).Contains(spell)) return false;
            Sync();
            string id = spell.AssetGuid.ToString();
            return Occultist0099Spells.Learn(State, slot, circle, id, root => root.Guid == id)
                == Occultist0099Outcome.Ok;
        }

        internal Occultist0099SpellState CopyState() => State.Clone();

        internal void RestoreCopiedState(Occultist0099SpellState copy) => Data.State = copy;

        public string[] KnownRoots() => Occultist0099Spells.KnownRoots(State);
        public string StateFingerprint() => State.Fingerprint();
        public int OpenQuotaCount(int circle)
            => Occultist0099Spells.QuotaCount(State, circle);
    }

    [TypeId(Occultist0099Ids.SchoolPowerGrantTypeId)]
    public sealed class Occultist0099SchoolPowerGrant : AddFeatureIfHasFact, IUnitGainFactHandler, IUnitLostFactHandler
    {
        public void HandleUnitGainFact(EntityFact fact)
        {
            if (fact?.Blueprint == CheckedFact) base.OnActivate();
        }

        public void HandleUnitLostFact(EntityFact fact)
        {
            if (fact?.Blueprint == CheckedFact) base.OnDeactivate();
        }
    }

    [TypeId(Occultist0099Ids.ImplementSlotTypeId)]
    public sealed class Occultist0099ImplementSlot : UnitFactComponentDelegate
    {
        public string SlotKey;
        public string School = Occultist0099Ids.SliceSchool;
        public bool ExtraSpells;
        public string Panoply;

        protected override void OnActivate() => UpdateSlot(true);
        protected override void OnDeactivate() => UpdateSlot(false);

        private void UpdateSlot(bool present)
            => Occultist0099NativeScope.InFactContext(Owner, Occultist0099Ids.SpellbookFactGuid,
                typeof(Occultist0099SpellbookFact0099), component => ((Occultist0099SpellbookFact0099)component).ApplyImplement(this, present));
    }

    internal sealed class Occultist0099NativeSpellOffer : SpellSelectionData
    {
        internal readonly HashSet<BlueprintAbility>[] Visible = Enumerable.Range(0, 7)
            .Select(_ => new HashSet<BlueprintAbility>()).ToArray();

        internal Occultist0099NativeSpellOffer(BlueprintSpellbook book, BlueprintSpellList list) : base(book, list) { }
    }

    // Native level-up replays ApplySpellbook before feature selections and
    // invokes OnApplyAction after each action on both the preview and commit.
    [HarmonyPatch(typeof(LevelUpState), nameof(LevelUpState.OnApplyAction))]
    internal static class Occultist0099NativeLearnOffers
    {
        private static void Postfix(LevelUpState __instance)
        {
            if (__instance.SelectedClass == null) return;
            var owner = __instance.Unit;
            var blueprint = owner.Progression.GetClassData(__instance.SelectedClass)?.Spellbook;
            if (!Occultist0099NativeScope.IsPrivateBook(blueprint)) return;
            var book = owner.GetSpellbook(blueprint);
            if (book == null) return;
            Occultist0099NativeScope.InFactContext(owner.Unit, Occultist0099Ids.SpellbookFactGuid,
                typeof(Occultist0099SpellbookFact0099),
                component => ((Occultist0099SpellbookFact0099)component).RefreshOffers(__instance, book));
        }
    }

    internal static class Occultist0099NativeSelection
    {
        private static Occultist0099ImplementSpellList SourceFor(BlueprintSpellbook book, BlueprintSpellList list)
            => list == null || !Occultist0099NativeScope.IsPrivateBook(book) || list == book.SpellList ? null :
                book.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
                    .FirstOrDefault(s => s.List?.Get() == list);

        internal static bool Select(SelectSpell action, LevelUpState levelUp, UnitDescriptor owner, bool commit)
        {
            if (levelUp == null || owner == null || action.SpellLevel < 1 || action.SpellLevel > 6 ||
                action.SlotIndex != 0 || action.SpellList == null || action.SpellList == action.Spellbook.SpellList ||
                action.Spell == null)
                return false;
            // The native SelectSpell action serializes the list reference. A
            // replay can only fill this source even if other slots were added,
            // filled, removed, or assigned a different implement in between.
            var source = SourceFor(action.Spellbook, action.SpellList);
            if (source == null || !action.SpellList.GetSpells(action.SpellLevel).Contains(action.Spell)) return false;
            var choice = levelUp.GetSpellSelection(action.Spellbook, action.SpellList);
            if (choice == null || (commit && !choice.CanSpendSlot(action.SpellLevel, action.SlotIndex)))
                return false;
            var runtime = Occultist0099NativeScope.RuntimeOf(owner.Unit,
                Occultist0099Ids.SpellbookFactGuid, typeof(Occultist0099SpellbookFact0099));
            if (Occultist0099NativeScope.OwnerOf(runtime) != owner.Unit) return false;
            var book = owner.GetSpellbook(action.Spellbook);
            return Occultist0099NativeScope.InRuntimeContext(runtime, component =>
            {
                var facts = (Occultist0099SpellbookFact0099)component;
                if (!facts.CanLearn(book, source, action.SpellLevel, action.Spell)) return false;
                return !commit || Learn(facts, book, choice, action, source.SlotKey);
            }, false);
        }

        private sealed class KnownWrite
        {
            internal Spellbook Book;
            internal int Circle;
            internal BlueprintAbility Spell;
        }

        [ThreadStatic] private static KnownWrite permittedWrite;

        internal static bool ConsumeWrite(Spellbook book, int circle, BlueprintAbility spell)
        {
            var permit = permittedWrite;
            if (permit == null || permit.Book != book || permit.Circle != circle || permit.Spell != spell)
                return false;
            permittedWrite = null;
            return true;
        }

        private static bool Learn(Occultist0099SpellbookFact0099 facts, Spellbook book,
            SpellSelectionData choice, SelectSpell action, string slot)
        {
            var previous = permittedWrite;
            bool committed = false;
            try
            {
                permittedWrite = new KnownWrite { Book = book, Circle = action.SpellLevel, Spell = action.Spell };
                // Keep observers out of the transaction; notify after both
                // ledgers and the actual choice slot have been updated.
                if (book.AddKnown(action.SpellLevel, action.Spell, false) == null ||
                    !book.IsKnownOnLevel(action.Spell, action.SpellLevel) ||
                    !choice.CanSpendSlot(action.SpellLevel, action.SlotIndex) ||
                    !facts.RecordLearn(book, slot, action.SpellLevel, action.Spell))
                    return false;
                choice.SpendSlot(action.SpellLevel, action.Spell, action.SlotIndex);
                committed = true;
            }
            finally
            {
                permittedWrite = previous;
                if (!committed && book.IsKnown(action.Spell))
                    book.RemoveSpell(action.Spell);
            }
            EventBus.RaiseEvent<ILearnSpellHandler>(h => h.HandleLearnSpell());
            return true;
        }
    }

    // Use the same native selector and collection hook as the Bard
    // masterpiece choices. Only a private 0099 source needs this filtering.
    [HarmonyPatch(typeof(CharGenSpellsPhaseVM), "CreateSpellVMsCollection")]
    internal static class Occultist0099NativeChoiceList
    {
        private static void Postfix(SpellSelectionData ___m_SelectionData, List<CharGenSpellSelectorItemVM> __result)
        {
            if (!(___m_SelectionData is Occultist0099NativeSpellOffer offer)) return;
            __result.RemoveAll(item =>
            {
                if (item.Level >= 1 && item.Level <= 6 && offer.Visible[item.Level].Contains(item.Spell)) return false;
                item.Dispose();
                return true;
            });
        }
    }

    [HarmonyPatch(typeof(CharGenSpellsPhaseVM), nameof(CharGenSpellsPhaseVM.TryUpdateSamePhase))]
    internal static class Occultist0099NativeChoiceRefresh
    {
        private static bool Prefix(SpellSelectionData __0, SpellSelectionData ___m_SelectionData, ref bool __result)
        {
            if (!(__0 is Occultist0099NativeSpellOffer) || ReferenceEquals(__0, ___m_SelectionData)) return true;
            // CharGen recreates phases by selection object, so all circles of
            // a changed source must refuse reuse together.
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(SelectSpell), nameof(SelectSpell.Check))]
    internal static class Occultist0099NativeSelectCheck
    {
        private static bool Prefix(SelectSpell __instance, ref bool __result)
        {
            if (!Occultist0099NativeScope.IsPrivateBook(__instance.Spellbook) ||
                (__instance.SpellLevel >= 1 && __instance.SpellLevel <= 6)) return true;
            __result = false;
            return false;
        }

        private static void Postfix(SelectSpell __instance, LevelUpState __0, UnitDescriptor __1,
            ref bool __result)
        {
            if (__result && Occultist0099NativeScope.IsPrivateBook(__instance.Spellbook))
                __result = Occultist0099NativeSelection.Select(__instance, __0, __1, false);
        }
    }

    [HarmonyPatch(typeof(SelectSpell), nameof(SelectSpell.Apply))]
    internal static class Occultist0099NativeSelectApply
    {
        private static bool Prefix(SelectSpell __instance, LevelUpState __0, UnitDescriptor __1)
        {
            if (!Occultist0099NativeScope.IsPrivateBook(__instance.Spellbook)) return true;
            Occultist0099NativeSelection.Select(__instance, __0, __1, true);
            return false;
        }
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.AddKnown))]
    internal static class Occultist0099NativeLearnCommit
    {
        private static bool Prefix(Spellbook __instance, int __0, BlueprintAbility __1, ref AbilityData __result)
        {
            if (!Occultist0099NativeScope.IsPrivateBook(__instance)) return true;
            if (Occultist0099NativeSelection.ConsumeWrite(__instance, __0, __1)) return true;
            if (Occultist0099NativeCopy.Permits(__instance, __0, __1)) return true;

            // Fixed 0-circle career grants are independent of implement choices.
            if (__0 == 0 && __1 != null && __1.Type == AbilityType.Spell && __1.Parent == null &&
                Occultist0099AbjurationCatalog.Knacks.Contains(__1.AssetGuid.ToString()))
                return true;

            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(UnitHelper), "CopySpellbook")]
    internal static class Occultist0099NativeCopy
    {
        private sealed class Copy
        {
            internal Spellbook Source;
            internal UnitEntityData Target;
        }

        [ThreadStatic] private static Copy current;

        private static void Prefix(UnitEntityData __0, UnitEntityData __1, out Copy __state)
        {
            __state = current;
            current = null;
            if (__0 == null || __1 == null || __0 == __1) return;
            var source = __0.Descriptor.Spellbooks.FirstOrDefault(Occultist0099NativeScope.IsPrivateBook);
            if (source != null) current = new Copy { Source = source, Target = __1 };
        }

        internal static bool Permits(Spellbook book, int circle, BlueprintAbility root)
            => current != null && root != null && book.Owner.Unit == current.Target &&
               book.Blueprint == current.Source.Blueprint && current.Source.IsKnownOnLevel(root, circle);

        private static void Postfix()
        {
            if (current == null) return;
            var source = current.Source.Owner.Unit;
            var target = current.Target;
            var from = Occultist0099NativeScope.RuntimeOf(source, Occultist0099Ids.SpellbookFactGuid,
                typeof(Occultist0099SpellbookFact0099));
            // UnitHelper.Copy turns the new unit on after copying its facts and book.
            var to = Occultist0099NativeScope.RuntimeOf(target, Occultist0099Ids.SpellbookFactGuid,
                typeof(Occultist0099SpellbookFact0099), requireActive: false);
            var state = Occultist0099NativeScope.InRuntimeContext(from,
                component => ((Occultist0099SpellbookFact0099)component).CopyState(), null);
            if (state != null)
                Occultist0099NativeScope.InRuntimeContext(to,
                    component => ((Occultist0099SpellbookFact0099)component).RestoreCopiedState(state));
        }

        private static void Finalizer(Copy __state) => current = __state;
    }

    internal static class Occultist0099NativeSpellGate
    {
        internal static BlueprintAbility RootBlueprintOf(BlueprintAbility blueprint, AbilityData spell)
        {
            var current = spell;
            while (current?.ConvertedFrom != null) current = current.ConvertedFrom;
            var bp = current?.Blueprint ?? blueprint;
            while (bp?.Parent != null) bp = bp.Parent;
            return bp;
        }

        internal static bool IsPublishedForCast(Spellbook book, BlueprintAbility blueprint, AbilityData spell)
        {
            if (!Occultist0099NativeScope.IsPrivateBook(book)) return true;
            if (spell != null && (spell.SourceItem != null || spell.StickyTouch != null)) return true;
            var rootBp = RootBlueprintOf(blueprint, spell);
            if (rootBp == null) return true;
            var row = Occultist0099SpellCatalog.Find(rootBp.AssetGuid.ToString());
            if (row == null) return true;
            if (rootBp.IsDlcRestricted()) return false;
            if (row.IsKnack) return true;
            var list = book.Blueprint?.SpellList;
            return list != null && list.GetSpells(row.Circle).Contains(rootBp);
        }

        internal static bool AllowsCast(AbilityData spell)
        {
            if (spell == null || spell.SourceItem != null || spell.StickyTouch != null) return true;
            var book = spell.Spellbook;
            return !Occultist0099NativeScope.IsPrivateBook(book) || IsPublishedForCast(book, spell.Blueprint, spell);
        }
    }

    [HarmonyPatch(typeof(Spellbook), "SpendInternal")]
    internal static class Occultist0099NativeSpellbookSpend
    {
        private static bool Prefix(Spellbook __instance, BlueprintAbility __0, AbilityData __1, ref bool __result)
        {
            if (Occultist0099NativeSpellGate.IsPublishedForCast(__instance, __0, __1)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(AbilityData), "get_IsAvailableForCast")]
    internal static class Occultist0099NativeSpellAvailable
    {
        private static void Postfix(AbilityData __instance, ref bool __result)
        {
            if (__result && !Occultist0099NativeSpellGate.AllowsCast(__instance))
                __result = false;
        }
    }
}
