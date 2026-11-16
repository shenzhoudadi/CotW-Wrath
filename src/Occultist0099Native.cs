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
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
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

    [TypeId(Occultist0099Ids.FocusPartTypeId)]
    public sealed class Occultist0099FocusPart : UnitPart
    {
        [JsonProperty] public Occultist0099FocusState State;
        [JsonProperty] internal string LastSuccessfulRest;
    }

    [TypeId(Occultist0099Ids.FocusFactTypeId)]
    public sealed class Occultist0099FocusFact0099 :
        UnitFactComponentDelegate<Occultist0099NativeData>
    {
        public BlueprintCharacterClassReference CharacterClass;
        public BlueprintAbilityResourceReference FocusResource;
        public BlueprintBuffReference WardingTalisman;
        public BlueprintBuffReference MindBarrierBuff;

        private Occultist0099FocusPart Ledger
        {
            get
            {
                var part = Owner.Ensure<Occultist0099FocusPart>();
                if (part.State == null) part.State = Data.State ?? Occultist0099Focus.Create();
                Data.State = null;
                return part;
            }
        }

        private Occultist0099FocusState State => Ledger.State;

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

        protected override void OnActivate()
        {
            if (ClassLevel() <= 0) return;
            var part = Ledger;
            if (part.LastSuccessfulRest != null && part.LastSuccessfulRest != part.State.LastRestEventId)
                BeginCycle(part.LastSuccessfulRest, false);
            else if (string.IsNullOrEmpty(part.State.LastRestEventId))
                BeginCycle("first-acquisition", true);
            else
                Occultist0099Focus.ReacquireClass(part.State);

            if (ClassLevel() < part.State.Level)
                Occultist0099Focus.RespecBudgetDown(part.State, ClassLevel(), IntModifier());
            Occultist0099Focus.Validate(part.State);
            ReconcileSchools();
            SyncResourceMirror();
            SyncWardingTalisman();
        }

        protected override void OnDeactivate()
        {
            var part = Owner.Get<Occultist0099FocusPart>();
            if (part?.State == null) return;
            if (ClassLevel() <= 0 && part.State.ClassPresent)
                foreach (string school in Occultist0099Schools.Order)
                    Occultist0099Focus.RespecRemoveSchool(part.State, school);
            Occultist0099Focus.RemoveClass(part.State);
            RemoveMindBarrier();
            SyncWardingTalisman();
        }

        protected override void OnPostLoad() => Occultist0099Focus.Validate(State);

        protected override void OnApplyPostLoadFixes()
        {
            Occultist0099Focus.Validate(State);
            ReconcileSchools();
            SyncResourceMirror();
            SyncWardingTalisman();
        }

        internal void Rest(string identity)
        {
            if (ClassLevel() <= 0) return;
            bool reset = BeginCycle(identity, false);
            Occultist0099Focus.Validate(State);
            SyncResourceMirror();
            if (reset) RemoveMindBarrier();
            SyncWardingTalisman();
        }

        internal void SetSchoolPresent(string school, bool present)
        {
            var result = present ? Occultist0099Focus.SetSchoolPresent(State, school, true) :
                Occultist0099Focus.RespecRemoveSchool(State, school);
            if (result != Occultist0099Outcome.Ok) return;
            SyncResourceMirror();
            if (school != Occultist0099Ids.SliceSchool) return;
            if (!present) RemoveMindBarrier();
            SyncWardingTalisman();
        }

        private void ReconcileSchools()
        {
            var state = State;
            if (!state.Enabled)
            {
                RemoveMindBarrier();
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
            if (!CanRetainMindBarrier()) RemoveMindBarrier();
        }

        internal bool Operate(Occultist0099FocusAction action, string school, string executionId, bool commit)
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
                default: return false;
            }
            if (outcome != Occultist0099Outcome.Ok) return false;
            if (commit)
            {
                state.ChargedExecutions.Add(executionId);
                SyncResourceMirror();
                if (action == Occultist0099FocusAction.Lock) SyncWardingTalisman();
            }
            return true;
        }

        private void RemoveMindBarrier()
        {
            var blueprint = MindBarrierBuff?.Get();
            if (blueprint != null) Owner.Buffs.GetBuff(blueprint)?.Remove();
        }

        private void SyncWardingTalisman()
        {
            var blueprint = WardingTalisman?.Get();
            if (blueprint == null) return;
            var current = Owner.Buffs.GetBuff(blueprint);
            if (WardingTalismanResonance(Occultist0099Ids.SliceSchool) <= 0)
                current?.Remove();
            else if (current == null)
                Owner.Buffs.AddBuff(blueprint, Owner, (TimeSpan?)null);
        }

        public bool IsOwnedBy(UnitEntityData unit) => unit != null && unit == Owner && ClassLevel() > 0;

        public bool CanPaySchool(UnitEntityData caster, string school, int cost,
            string executionId)
            => IsOwnedBy(caster) &&
               Occultist0099Focus.CanPaySchool(State, school, cost, executionId);

        // D-0099-01: the Panoply and generic-pool counterparts, so the native
        // AbilityResourceLogic subclass can precheck and pay every pool kind
        // through the same authoritative state.
        public bool CanPayPanoply(UnitEntityData caster, string panoply, int cost,
            string executionId)
            => IsOwnedBy(caster) &&
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
            native.Amount = State.ClassPresent && State.Enabled ? State.Available : 0;
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
            if (!IsOwnedBy(caster))
                return new Occultist0099Payment
                {
                    Outcome = Occultist0099Outcome.RejectedNotOwned
                };
            return Occultist0099Focus.PayPanoply(State, panoply, cost, executionId);
        }

        public int WardingTalismanResonance(string school)
            => ClassLevel() > 0 ? Occultist0099Focus.Resonance(State, school) : 0;

        public Occultist0099FocusPower MindBarrier(string school)
            => Occultist0099Focus.MindBarrier(State, school, ClassLevel(), IntModifier());

        internal bool CanRetainMindBarrier()
            => State.Enabled && State.ClassPresent && State.Locked &&
               State.Schools[Occultist0099Ids.SliceSchool].Present && ClassLevel() > 0;

        public string StateFingerprint() => State.Fingerprint();
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
