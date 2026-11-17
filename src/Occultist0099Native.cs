// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// Native Wrath adapter for the authoritative state in Occultist0099Rules.cs.
// The same state object is used by the UI precheck, the real payment and the
// rest/level-up events; no mirrored spend counter exists.
//
// This slice is development-only. Nothing here registers a class, spellbook or
// progression blueprint in the shared registration chain; that happens once
// eight schools, four Panoplies and the full private spell table are complete.
//
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Rest;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.MVVM._VM.CharGen;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class Occultist0099NativeData
    {
        // Imported if a save from the development adapter still stores fact data.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Occultist0099FocusState State;
    }

    [Serializable]
    public sealed class Occultist0099TrackedBuff
    {
        [JsonProperty] public string School;
        [JsonProperty] public EntityFactRef<Buff> Buff;
    }

    [TypeId(Occultist0099Ids.FocusPartTypeId)]
    public sealed class Occultist0099FocusPart : UnitPart, IUnitPartSurviveRespec, IUnitChangedAfterRespecHandler, IAreaHandler
    {
        [JsonProperty] public Occultist0099FocusState State;
        [JsonProperty] internal string LastSuccessfulRest;
        // Native fact references keep the recipient and exact buff instance,
        // including effects cast on allies. Never infer ownership from its name.
        [JsonProperty] internal List<Occultist0099TrackedBuff> SchoolBuffs = new List<Occultist0099TrackedBuff>();
        [JsonProperty] internal List<EntityFactRef<Buff>> PendingSchoolBuffs = new List<EntityFactRef<Buff>>();
        [JsonProperty] private bool m_RespecPending;
        [JsonIgnore] internal bool CopyingFacts;
        [JsonIgnore] internal bool RespecDraft;

        internal bool Rebuilding => m_RespecPending || RespecDraft;
        internal bool DeferReconciliation => CopyingFacts || Rebuilding;

        public bool ShouldSurviveRespec => State != null;

        internal static Occultist0099FocusPart PreviewSource(UnitEntityData unit)
        {
            for (var parent = unit.PreviewOf.Entity; parent != null; parent = parent.PreviewOf.Entity)
            {
                var part = parent.Get<Occultist0099FocusPart>();
                if (part?.State != null) return part;
            }
            return null;
        }

        internal void CopyLedger(Occultist0099FocusPart source)
        {
            State = source.State.Clone();
            LastSuccessfulRest = source.LastSuccessfulRest;
        }

        public void CopyAfterRespec(UnitEntityData toUnit)
        {
            if (toUnit != Owner || State == null) return;
            var restored = toUnit.Ensure<Occultist0099FocusPart>();
            restored.CopyLedger(this);
            // The committed native respec replaces Parts from its preview.
            // Preserve live recipients here, never in the preview CopyLedger.
            restored.SchoolBuffs = SchoolBuffs.Select(entry => new Occultist0099TrackedBuff
                { School = entry.School, Buff = entry.Buff }).ToList();
            restored.PendingSchoolBuffs = new List<EntityFactRef<Buff>>(PendingSchoolBuffs);
            restored.m_RespecPending = true;
            restored.RespecDraft = false;
        }

        internal static bool RebuildWindowOpen(UnitEntityData unit)
        {
            var window = RespecWindowVM.Instance;
            return window?.IsRespec == true && ReferenceEquals(window.CurrentUnit?.Value, unit);
        }

        public void HandleUnitChangedAfterRespec(UnitEntityData unit)
        {
            if (unit != Owner || !m_RespecPending) return;
            if (RebuildWindowOpen(unit)) RefreshBuild(unit, false);
            else FinishRespec();
        }

        internal void FinishRespec()
        {
            if (!m_RespecPending || Owner == null) return;
            m_RespecPending = false;
            if (!RefreshBuild(Owner, true))
            {
                Occultist0099Focus.RetireClass(State);
                ClearSchoolBuffs(null);
            }
        }

        internal void ClearSchoolBuffs(string school)
        {
            for (int i = SchoolBuffs.Count - 1; i >= 0; i--)
            {
                var entry = SchoolBuffs[i];
                if (school != null && entry.School != school) continue;
                if (!PendingSchoolBuffs.Contains(entry.Buff)) PendingSchoolBuffs.Add(entry.Buff);
                SchoolBuffs.RemoveAt(i);
            }
            FlushSchoolBuffs();
        }

        private void FlushSchoolBuffs()
        {
            foreach (var reference in PendingSchoolBuffs.ToArray())
            {
                // Keep unresolved recipients pending across area changes. A new
                // focus cycle must not make an old effect legitimate again.
                if (reference.Entity == null) continue;
                var buff = reference.Fact;
                if (buff?.Context?.MaybeCaster == Owner) buff.Remove();
                PendingSchoolBuffs.Remove(reference);
            }
        }

        public void OnAreaBeginUnloading() { }
        public void OnAreaDidLoad() => FlushSchoolBuffs();

        internal static bool RefreshBuild(UnitEntityData unit, bool respec)
        {
            var runtime = Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099));
            return Occultist0099NativeScope.InRuntimeContext(runtime,
                component => ((Occultist0099FocusFact0099)component).RefreshBuild(respec), false);
        }
    }

    [HarmonyPatch(typeof(UnitHelper), "CopyFacts")]
    internal static class Occultist0099NativeFocusPreview
    {
        private static void Prefix(UnitEntityData __0, UnitEntityData __1, out Occultist0099FocusPart __state)
        {
            __state = null;
            if (__0 == __1 || __1.PreviewOf.Entity != __0) return;
            var direct = __0.Get<Occultist0099FocusPart>();
            var source = direct?.State != null ? direct : Occultist0099FocusPart.PreviewSource(__1);
            if (source == null) return;
            __state = __1.Ensure<Occultist0099FocusPart>();
            __state.CopyLedger(source);
            __state.RespecDraft = source != direct || source.Rebuilding;
            __state.CopyingFacts = true;
        }

        private static void Finalizer(UnitEntityData __1, Occultist0099FocusPart __state, Exception __exception)
        {
            if (__state == null) return;
            __state.CopyingFacts = false;
            if (__exception == null) Occultist0099FocusPart.RefreshBuild(__1, false);
        }
    }

    [HarmonyPatch(typeof(RespecWindowVM), nameof(RespecWindowVM.Complete))]
    internal static class Occultist0099NativeRespecComplete
    {
        private static void Prefix(RespecWindowVM __instance)
        {
            if (!__instance.IsRespec) return;
            var unit = __instance.CurrentUnit?.Value;
            var part = unit?.Get<Occultist0099FocusPart>();
            if (part?.Rebuilding == true && !unit.Progression.CanLevelUpNormal && !unit.Progression.CanLevelUpMythic)
                part.FinishRespec();
        }
    }

    // Existing partial-respec variations can close the native window sooner.
    // At that point the currently committed build, if any, is authoritative.
    [HarmonyPatch(typeof(RespecWindowVM), "DisposeImplementation")]
    internal static class Occultist0099NativeRespecClosed
    {
        private static void Prefix(RespecWindowVM __instance)
        {
            if (__instance.IsRespec) __instance.CurrentUnit?.Value?.Get<Occultist0099FocusPart>()?.FinishRespec();
        }
    }

    [HarmonyPatch(typeof(UnitEntityData), "OnApplyPostLoadFixes")]
    internal static class Occultist0099NativeRespecLoad
    {
        private static void Postfix(UnitEntityData __instance)
        {
            // An interrupted rebuild can be loaded without its old window.
            // Use the final descriptor and body after native stat/load fixes.
            if (!ContextData<UnitEntityData.RespecCommit>.Current && !Occultist0099FocusPart.RebuildWindowOpen(__instance))
                __instance.Get<Occultist0099FocusPart>()?.FinishRespec();
        }
    }

    [TypeId(Occultist0099Ids.FocusFactTypeId)]
    public sealed class Occultist0099FocusFact0099 :
        UnitFactComponentDelegate<Occultist0099NativeData>, IUnitBuffHandler, IGlobalSubscriber
    {
        public BlueprintCharacterClassReference CharacterClass;
        public BlueprintAbilityResourceReference FocusResource;
        public BlueprintBuffReference WardingTalisman;
        public BlueprintBuffReference MindBarrierBuff;
        public BlueprintBuffReference[] AbjurationEffects = Array.Empty<BlueprintBuffReference>();
        public BlueprintBuffReference[] DivinationEffects = Array.Empty<BlueprintBuffReference>();
        public BlueprintBuffReference[] EnchantmentEffects = Array.Empty<BlueprintBuffReference>();
        public BlueprintBuffReference[] NecromancyEffects = Array.Empty<BlueprintBuffReference>();
        // The temporary familiars this class granted. Removing these buffs lets the
        // native AddFacts detach only its own granted fact, leaving any familiar the
        // character owns from another class untouched.
        public BlueprintBuffReference[] NecromancyPuppets = Array.Empty<BlueprintBuffReference>();
        public BlueprintBuffReference IllusionAura;
        public BlueprintBuffReference LegacyWeapon;
        public BlueprintBuffReference[] EvocationEffects = Array.Empty<BlueprintBuffReference>();
        public BlueprintBuffReference[] TransmutationEffects = Array.Empty<BlueprintBuffReference>();
        public Occultist0099SchoolResonance[] SchoolResonances = Array.Empty<Occultist0099SchoolResonance>();
        public Occultist0099PanoplyResonance[] Panoplies = Array.Empty<Occultist0099PanoplyResonance>();

        private Occultist0099FocusPart Ledger
        {
            get
            {
                var part = Owner.Ensure<Occultist0099FocusPart>();
                if (part.State == null)
                {
                    var source = Occultist0099FocusPart.PreviewSource(Owner);
                    if (source != null)
                    {
                        part.CopyLedger(source);
                        // UnitHelper.Respec starts a vacuum unit without CopyFacts.
                        part.RespecDraft = true;
                    }
                    else part.State = Data.State ?? Occultist0099Focus.Create();
                }
                Data.State = null;
                return part;
            }
        }

        private Occultist0099FocusState State => Ledger.State;

        internal int ActualLevel => ClassLevel();

        private int ClassLevel()
        {
            var cls = CharacterClass?.Get();
            return cls == null ? 0 : Owner.Progression.GetClassLevel(cls);
        }

        private int IntModifier() => Owner.Stats?.Intelligence?.Bonus ?? 0;

        private bool BeginCycle(string identity, bool firstAcquisition)
        {
            var state = State;
            if (Occultist0099Focus.BeginCycle(state, ClassLevel(), IntModifier(),
                    identity, firstAcquisition) != Occultist0099Outcome.Ok) return false;
            state.Intelligence = Owner.Stats?.Intelligence?.ModifiedValue ?? 10;
            return true;
        }

        protected override void OnActivate() => RefreshBuild(false);

        internal bool RefreshBuild(bool respec)
        {
            int level = ClassLevel();
            if (level <= 0) return false;
            var part = Ledger;
            if (part.DeferReconciliation)
            {
                SyncResourceMirror();
                SyncResonance();
                return true;
            }
            if (part.LastSuccessfulRest != null && part.LastSuccessfulRest != part.State.LastRestEventId)
                BeginCycle(part.LastSuccessfulRest, false);
            else if (string.IsNullOrEmpty(part.State.LastRestEventId))
                BeginCycle("first-acquisition", true);
            else
                Occultist0099Focus.ReacquireClass(part.State);

            Occultist0099Focus.Validate(part.State);
            ReconcileSchools();
            if (respec)
            {
                // The rebuilt class will bind its actual mastery selection.
                // Until then only the spent and remaining mastery points survive.
                Occultist0099Focus.RespecMastery(part.State, null);
                Occultist0099Focus.RespecBudgetDown(part.State, level, IntModifier());
            }
            // A vacuum respec rebuild or an interrupted load can finish after
            // the mastery feature was reattached without a fresh OnActivate;
            // re-bind it from the active features exactly like a rest does.
            ReconcileMastery();
            SyncResourceMirror();
            SyncResonance();
            return true;
        }

        protected override void OnDeactivate()
        {
            var part = Owner.Get<Occultist0099FocusPart>();
            if (part?.State == null || part.DeferReconciliation) return;
            if (ClassLevel() <= 0) Occultist0099Focus.RetireClass(part.State);
            else Occultist0099Focus.RemoveClass(part.State);
            RemoveAllSchoolEffects();
            SyncResonance();
        }

        protected override void OnPostLoad() => Occultist0099Focus.Validate(State);

        protected override void OnApplyPostLoadFixes()
        {
            if (!Ledger.DeferReconciliation)
            {
                Occultist0099Focus.Validate(State);
                ReconcileSchools();
            }
            SyncResourceMirror();
            SyncResonance();
        }

        internal void Rest(string identity)
        {
            if (ClassLevel() <= 0 || Ledger.DeferReconciliation) return;
            bool reset = BeginCycle(identity, false);
            Occultist0099Focus.Validate(State);
            SyncResourceMirror();
            if (reset) RemoveAllSchoolEffects();
            ReconcileMastery();
            SyncResonance();
        }

        // C:21 "精通重新授予": BeginCycle clears the mastery identity on a real
        // rest, so the still-active mastery feature re-binds its school here.
        // A missing implement makes SetMastery refuse and the re-grant stays
        // closed until the school is learned again.
        private void ReconcileMastery()
        {
            var state = State;
            if (!state.Enabled || state.MasterySchool != null) return;
            var schools = new HashSet<string>(Owner.Progression.Features.Enumerable.Where(f => f.Active)
                .SelectMany(f => f.Blueprint.GetComponents<Occultist0099ImplementMastery>()).Select(source => source.School));
            foreach (string school in schools)
                if (Occultist0099Focus.SetMastery(state, school) == Occultist0099Outcome.Ok) break;
        }

        // Repower uses the generic pool and commits only after a successful summon.
        internal bool CanConsumeConstruct(string corpseId)
            => !Ledger.DeferReconciliation &&
               Occultist0099Focus.CanConsumeConstruct(State, corpseId);

        internal bool TryRepowerConstruct(UnitEntityData caster, string corpseId, string executionId)
            => IsOwnedBy(caster) && !Ledger.DeferReconciliation &&
               Occultist0099Focus.RepowerConstruct(State, corpseId, executionId) == Occultist0099Outcome.Ok;

        internal int RepowerLevel() => ClassLevel();

        // Implement Mastery activation/deactivation and the post-rest re-grant
        // land here. The rule layer owns the H/T ledger and the resonance
        // re-capture; this entry only mirrors the resource display and rebuilds
        // the resonance buffs from the new locked snapshot.
        internal void SetMasteryPresent(string school, bool present)
        {
            if (Ledger.DeferReconciliation) return;
            var result = present ? Occultist0099Focus.SetMastery(State, school) :
                Occultist0099Focus.RespecMastery(State, null);
            if (result != Occultist0099Outcome.Ok) return;
            SyncResourceMirror();
            SyncResonance();
        }

        internal void SetSchoolPresent(string school, bool present)
        {
            if (Ledger.DeferReconciliation) return;
            var result = present ? Occultist0099Focus.SetSchoolPresent(State, school, true) :
                Occultist0099Focus.RespecRemoveSchool(State, school);
            if (result != Occultist0099Outcome.Ok) return;
            SyncResourceMirror();
            if (!present) RemoveSchoolEffects(school);
            SyncResonance();
        }

        private void ReconcileSchools()
        {
            var state = State;
            if (!state.Enabled)
            {
                RemoveAllSchoolEffects();
                return;
            }
            var active = new HashSet<string>(Owner.Progression.Features.Enumerable.Where(f => f.Active)
                .SelectMany(f => f.Blueprint.GetComponents<Occultist0099ImplementSchool>()).Select(source => source.School));
            foreach (string school in Occultist0099Schools.Order)
            {
                bool present = active.Contains(school);
                if (present == state.Schools[school].Present) continue;
                if (present) Occultist0099Focus.SetSchoolPresent(state, school, true);
                else Occultist0099Focus.RespecRemoveSchool(state, school);
            }
            if (!CanRetainMindBarrier()) RemoveSchoolEffects(Occultist0099Ids.SliceSchool);
            foreach (string school in new[]
            {
                "Divination", Occultist0099Enchantment.School, Occultist0099Illusion.School,
                Occultist0099Evocation.School, Occultist0099Transmutation.School, Occultist0099Necromancy.School
            })
                if (!state.Locked || !state.Schools[school].Present) RemoveSchoolEffects(school);
        }

        internal bool Operate(Occultist0099FocusAction action, string school, string physicalAttribute, string executionId, bool commit)
        {
            if (!IsOwnedBy(Owner) || (commit && executionId == null)) return false;
            var current = State;
            if (executionId != null && current.ChargedExecutions.Contains(executionId)) return false;
            var state = commit ? current : current.Clone();
            Occultist0099Outcome outcome;
            switch (action)
            {
                case Occultist0099FocusAction.Invest: outcome = Occultist0099Focus.Invest(state, school, 1); break;
                case Occultist0099FocusAction.Reset: outcome = Occultist0099Focus.Reset(state); break;
                case Occultist0099FocusAction.Lock: outcome = Occultist0099Focus.Lock(state, ClassLevel()); break;
                case Occultist0099FocusAction.Replenish: outcome = Occultist0099Focus.Replenish(state, school); break;
                case Occultist0099FocusAction.ChoosePhysical: outcome = Occultist0099Focus.ChoosePhysicalAttribute(state, physicalAttribute); break;
                default: return false;
            }
            if (outcome != Occultist0099Outcome.Ok) return false;
            if (commit)
            {
                state.ChargedExecutions.Add(executionId);
                SyncResourceMirror();
                if (action == Occultist0099FocusAction.Lock) SyncResonance();
            }
            return true;
        }

        private void RemoveAllSchoolEffects()
        {
            foreach (string school in Occultist0099Schools.Order)
                RemoveSchoolEffects(school);
        }

        private void RemoveSchoolEffects(string school)
        {
            if (school == "Divination" && Occultist0099Divination.HoldsFortune(Owner.Get<UnitPartTouch>()))
                Owner.Remove<UnitPartTouch>();
            else if (school == Occultist0099Illusion.School && Occultist0099Illusion.HoldsTerror(Owner.Get<UnitPartTouch>()))
                Owner.Remove<UnitPartTouch>();
            else if (school == Occultist0099Necromancy.School && Occultist0099Necromancy.HoldsFleshRot(Owner.Get<UnitPartTouch>()))
                Owner.Remove<UnitPartTouch>();

            if (school == Occultist0099Ids.SliceSchool)
            {
                RemoveLocalBuffs(new[] { MindBarrierBuff }.Concat(AbjurationEffects));
                return;
            }
            if (school == Occultist0099Illusion.School)
            {
                RemoveLocalBuffs(new[] { IllusionAura });
                return;
            }
            if (school == Occultist0099Evocation.School)
            {
                RemoveLocalBuffs(EvocationEffects);
                return;
            }
            if (school == Occultist0099Necromancy.School)
            {
                RemoveLocalBuffs(NecromancyEffects.Concat(NecromancyPuppets));
                return;
            }
            if (school != "Divination" && school != Occultist0099Enchantment.School &&
                school != Occultist0099Transmutation.School) return;

            var tracked = Ledger.SchoolBuffs;
            // Include local effects for development saves made before tracking
            // was added, but preserve buffs that another Occultist cast here.
            foreach (var effect in Owner.Buffs.Enumerable
                .Where(buff => buff != null && buff.Context?.MaybeCaster == Owner &&
                               SchoolOfTrackedBuff(buff.Blueprint) == school).ToArray())
                if (!tracked.Any(entry => entry.Buff == effect))
                    tracked.Add(new Occultist0099TrackedBuff { School = school, Buff = effect });
            Ledger.ClearSchoolBuffs(school);
        }

        private void RemoveLocalBuffs(IEnumerable<BlueprintBuffReference> references)
        {
            foreach (var reference in references)
            {
                var effect = reference?.Get();
                if (effect != null) Owner.Buffs.GetBuff(effect)?.Remove();
            }
        }

        private static bool MatchesBlueprint(BlueprintBuffReference[] references, BlueprintGuid guid)
            => references.Any(reference => reference != null && reference.Guid == guid);

        private string SchoolOfTrackedBuff(BlueprintBuff blueprint)
        {
            if (blueprint == null) return null;
            var guid = blueprint.AssetGuid;
            if (guid == LegacyWeapon?.Guid || MatchesBlueprint(TransmutationEffects, guid))
                return Occultist0099Transmutation.School;
            if (MatchesBlueprint(EnchantmentEffects, guid))
                return Occultist0099Enchantment.School;
            if (MatchesBlueprint(DivinationEffects, guid))
                return "Divination";
            return null;
        }

        public void HandleBuffDidAdded(Buff buff)
        {
            if (buff == null || buff.Context?.MaybeCaster != Owner) return;
            string school = SchoolOfTrackedBuff(buff.Blueprint);
            if (school == null) return;
            var tracked = Ledger.SchoolBuffs;
            if (!tracked.Any(entry => entry.Buff.FactId == buff.UniqueId))
                tracked.Add(new Occultist0099TrackedBuff { School = school, Buff = buff });
        }

        public void HandleBuffDidRemoved(Buff buff)
        {
            if (buff == null) return;
            var part = Owner.Get<Occultist0099FocusPart>();
            part?.SchoolBuffs.RemoveAll(entry => entry.Buff == buff);
            part?.PendingSchoolBuffs.RemoveAll(reference => reference == buff);
        }

        private void SyncResonance()
        {
            SyncResonanceBuff(Occultist0099Ids.SliceSchool, WardingTalisman?.Get());
            foreach (var resonance in SchoolResonances)
                SyncResonanceBuff(resonance.School, resonance.Buff?.Get(), resonance.Tier, resonance.PhysicalAttribute);
            foreach (var panoply in Panoplies)
                SyncPanoply(panoply.Name, true);
        }

        internal void SyncPanoply(string name, bool present)
        {
            var panoply = Panoplies.FirstOrDefault(entry => entry.Name == name);
            if (panoply == null) return;
            bool active = present && OwnsPanoply(name) && State.Enabled && State.Locked &&
                          Occultist0099Focus.PanoplyResonance(State, name) > 0;
            var buff = panoply.Buff?.Get();
            if (buff != null)
            {
                var current = Owner.Buffs.GetBuff(buff);
                int amount = active ? panoply.Value?.Get()?.GetInt(Owner) ?? 0 : 0;
                if (current != null && (amount <= 0 || current.Context[Kingmaker.Enums.AbilityRankType.Default] != amount))
                {
                    current.Remove();
                    current = null;
                }
                if (current == null && amount > 0) Owner.Buffs.AddBuff(buff, Owner, (TimeSpan?)null);
            }
            if (!active)
                foreach (var reference in panoply.ActiveEffects)
                {
                    var effect = reference?.Get();
                    if (effect != null) Owner.Buffs.GetBuff(effect)?.Remove();
                }
        }

        private void SyncResonanceBuff(string school, BlueprintBuff blueprint, int tier = 0, string physicalAttribute = null)
        {
            if (blueprint == null) return;
            var current = Owner.Buffs.GetBuff(blueprint);
            int amount = WardingTalismanResonance(school);
            if (amount <= 0 || (tier > 0 && Occultist0099Focus.ResonanceTier(State, school) != tier) ||
                (physicalAttribute != null && physicalAttribute != State.PhysicalAttribute))
                current?.Remove();
            else if (current == null)
                Owner.Buffs.AddBuff(blueprint, Owner, (TimeSpan?)null);
            else if (ResonanceBuffStale(current, school, amount))
            {
                current.Remove();
                Owner.Buffs.AddBuff(blueprint, Owner, (TimeSpan?)null);
            }
        }

        // AddContextStatBonus evaluates its context value once, in OnTurnOn,
        // and leaves a fixed-value modifier; a buff without ContextRankConfig
        // (WardingTalismanBuff) has no context rank to compare against. After
        // Implement Mastery re-captures the Abjuration snapshot (C:34) the
        // living ward would keep the stale save bonus, so compare the modifier
        // the buff actually applied with the current resonance amount and
        // rebuild on drift. Ranked buffs keep the context-rank comparison that
        // covers their own value source.
        private bool ResonanceBuffStale(Kingmaker.UnitLogic.Buffs.Buff current, string school, int amount)
        {
            if ((school == "Divination" || school == "Transmutation") &&
                current.Blueprint.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>() != null)
                return current.Context[Kingmaker.Enums.AbilityRankType.Default] != amount;
            if (school != Occultist0099Ids.SliceSchool) return false;
            var ward = Owner.Stats.GetStat(Kingmaker.EntitySystem.Stats.StatType.SaveFortitude);
            var applied = ward?.Modifiers.FirstOrDefault(modifier => modifier.Source == current &&
                modifier.ModDescriptor == Kingmaker.Enums.ModifierDescriptor.Resistance);
            return applied != null && applied.ModValue != amount;
        }

        public bool IsOwnedBy(UnitEntityData unit)
            => unit != null && unit == Owner && ClassLevel() > 0 && !Ledger.Rebuilding;

        public bool CanPaySchool(UnitEntityData caster, string school, int cost,
            string executionId)
            => IsOwnedBy(caster) &&
               Occultist0099Focus.CanPaySchool(State, school, cost, executionId);

        // D-0099-01: the Panoply and generic-pool counterparts, so the native
        // AbilityResourceLogic subclass can precheck and pay every pool kind
        // through the same authoritative state.
        public bool CanPayPanoply(UnitEntityData caster, string panoply, int cost,
            string executionId)
            => IsOwnedBy(caster) && OwnsPanoply(panoply) &&
               Occultist0099Focus.CanPayPanoply(State, panoply, cost, executionId);

        public bool CanPayGeneric(UnitEntityData caster, int cost, string executionId)
            => IsOwnedBy(caster) &&
               Occultist0099Focus.CanPayGeneric(State, cost, executionId);

        public Occultist0099Payment TryPayGeneric(UnitEntityData caster, int cost,
            string executionId)
        {
            if (!IsOwnedBy(caster))
                return new Occultist0099Payment
                {
                    Outcome = Occultist0099Outcome.RejectedNotOwned
                };
            return Occultist0099Focus.PayGeneric(State, cost, executionId);
        }

        // The native resource is a DISPLAY MIRROR only. BlueprintAbilityResource
        // .GetMaxAmount is not virtual, so the authoritative budget cannot live
        // there; AddAbilityResources attaches it with RestoreAmount=false and
        // this method writes the authoritative Available into it after each
        // accepted transaction. Nothing here debits the mirror, so the native
        // ConnectResource path cannot produce a second charge.
        public void SyncResourceMirror()
        {
            var resource = FocusResource?.Get();
            var resources = Owner.Descriptor.Resources;
            if (resource == null || resources == null) return;
            // PersistantResources is the native serialized view of the same
            // live UnitAbilityResource objects, so this writes the real counter
            // without reaching into a private dictionary.
            var native = resources.PersistantResources
                .FirstOrDefault(r => r.Blueprint == resource);
            if (native == null) return;
            native.Amount = State.ClassPresent && State.Enabled && !Ledger.Rebuilding ? State.Available : 0;
        }

        public int AuthoritativeAvailable() => State.Available;
        public bool IsLocked() => State.Locked;

        // Native precheck and execution both land here. The core rechecks the
        // whole transaction before touching a balance, so a changed balance
        // between CanPay and execution fails atomically (MF11).
        public Occultist0099Payment TryPaySchool(UnitEntityData caster, string school,
            int cost, string executionId)
        {
            if (!IsOwnedBy(caster))
                return new Occultist0099Payment
                {
                    Outcome = Occultist0099Outcome.RejectedNotOwned
                };
            return Occultist0099Focus.PaySchool(State, school, cost, executionId);
        }

        public Occultist0099Payment TryPayPanoply(UnitEntityData caster, string panoply,
            int cost, string executionId)
        {
            if (!IsOwnedBy(caster) || !OwnsPanoply(panoply))
                return new Occultist0099Payment
                {
                    Outcome = Occultist0099Outcome.RejectedNotOwned
                };
            return Occultist0099Focus.PayPanoply(State, panoply, cost, executionId);
        }

        public int WardingTalismanResonance(string school)
            => IsOwnedBy(Owner) ? Occultist0099Focus.Resonance(State, school) : 0;

        internal bool OwnsPanoply(string name)
        {
            var members = Occultist0099Schools.MembersOf(name);
            var source = Panoplies.FirstOrDefault(entry => entry.Name == name)?.Source?.Get();
            return members != null && source != null && IsOwnedBy(Owner) && State.Enabled &&
                Owner.Descriptor.HasFact(source) && members.All(school => State.Schools[school].Present);
        }

        internal int PanoplyBonus(string name, int divisor, bool halfLevelCap, bool capAtLevel = false)
        {
            if (!OwnsPanoply(name) || divisor < 1) return 0;
            int amount = Occultist0099Focus.PanoplyResonance(State, name) / divisor;
            if (halfLevelCap) amount = Math.Min(amount, ActualLevel / 2);
            if (capAtLevel) amount = Math.Min(amount, ActualLevel);
            return amount;
        }

        public Occultist0099FocusPower MindBarrier(string school)
            => Occultist0099Focus.MindBarrier(State, school, ClassLevel(), IntModifier());

        internal int EquipmentBudget(string school)
        {
            if (!IsOwnedBy(Owner) || !State.Enabled || !State.Schools[school].Present) return 0;
            int level = ClassLevel();
            return 1 + Math.Min(3, level / 6) +
                (level >= 20 && State.MasterySchool == school ? 1 : 0);
        }

        internal bool CanRetainMindBarrier()
            => State.Enabled && State.ClassPresent && State.Locked &&
               State.Schools[Occultist0099Ids.SliceSchool].Present && IsOwnedBy(Owner);

        public string StateFingerprint() => State.Fingerprint();
    }

    [Serializable]
    public sealed class Occultist0099SchoolResonance
    {
        public string School;
        public BlueprintBuffReference Buff;
        public int Tier;
        public string PhysicalAttribute;
    }

    [Serializable]
    public sealed class Occultist0099PanoplyResonance
    {
        public string Name;
        public BlueprintFeatureReference Source;
        public BlueprintBuffReference Buff;
        public BlueprintUnitPropertyReference Value;
        public BlueprintBuffReference[] ActiveEffects = Array.Empty<BlueprintBuffReference>();
    }

    // One identity for a batch of applied rests, including paired units.
    // Scripted immediate rest uses this same native batch without a camp increment.
    [HarmonyPatch(typeof(RestController), "ApplyRest", new[] { typeof(List<UnitEntityData>) })]
    internal static class Occultist0099NativeRestBatch
    {
        [ThreadStatic] internal static string Identity;

        private static void Prefix(out string __state)
        {
            __state = Identity;
            if (Identity == null) Identity = Guid.NewGuid().ToString("N");
        }

        private static void Finalizer(string __state) => Identity = __state;
    }

    [HarmonyPatch(typeof(RestController), nameof(RestController.ApplyRest), new[] { typeof(UnitDescriptor) })]
    internal static class Occultist0099NativeRest
    {
        private static void Postfix(UnitDescriptor __0)
        {
            var unit = __0?.Unit;
            if (unit?.PreviewOf.Entity != null) return;
            var part = unit?.Get<Occultist0099FocusPart>();
            var runtime = Occultist0099NativeScope.RuntimeOf(unit,
                Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099));
            if (part == null && runtime == null) return;

            string identity = Occultist0099NativeRestBatch.Identity ?? Guid.NewGuid().ToString("N");
            if (part != null) part.LastSuccessfulRest = identity;
            Occultist0099NativeScope.InRuntimeContext(runtime,
                component => ((Occultist0099FocusFact0099)component).Rest(identity));
        }
    }

    [Serializable]
    public sealed class Occultist0099ImplementSpellList
    {
        public string SlotKey;
        public string School;
        public string Panoply;
        public bool ExtraSpells;
        public BlueprintSpellListReference List;

        public bool Matches(Occultist0099Slot slot)
            => slot.Key == SlotKey && slot.School == School && slot.Panoply == Panoply &&
               slot.ExtraSpells == ExtraSpells;
    }

    // Private spellbook marker. Only this book may use the private circle
    // table; backlink/global spell-list data of other books stays untouched.
    [TypeId(Occultist0099Ids.SpellbookMarkerTypeId)]
    public sealed class Occultist0099SpellbookMarker0099 : BlueprintComponent
    {
        public int MaximumSpellLevel = 6;
        public bool ZeroQuotaSpellsKnownTable = true;
        public Occultist0099ImplementSpellList[] ImplementLists = Array.Empty<Occultist0099ImplementSpellList>();

        public int PrivateCircle(string rootGuid, int nativeCircle)
            => Occultist0099BookScope.CircleOf(Occultist0099Ids.BookGuid,
                rootGuid, nativeCircle);
    }
}
