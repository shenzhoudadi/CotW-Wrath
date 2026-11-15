// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// Native Wrath adapter for the authoritative state in Occultist0099Rules.cs.
// The same state object is used by the UI precheck, the real payment and the
// rest/level-up events; no mirrored spend counter exists.
//
// This slice is development-only. Nothing here registers a class, spellbook or
// progression blueprint in the shared registration chain; that happens once
// eight schools, four Panoplies and the full private spell table are complete.
using System;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class Occultist0099NativeData
    {
        [JsonProperty] public Occultist0099FocusState State = Occultist0099Focus.Create();
    }

    [TypeId("0099c0e1a1b24c0d8e5f600000000001")]
    public sealed class Occultist0099FocusFact0099 :
        UnitFactComponentDelegate<Occultist0099NativeData>, IUnitRestHandler, IUnitSubscriber
    {
        // Set by the eventual registration package. Until then the native fact
        // is never attached to a blueprint and no in-game behaviour changes.
        public BlueprintCharacterClassReference CharacterClass;

        private Occultist0099FocusState State
        {
            get
            {
                if (Data.State == null) Data.State = Occultist0099Focus.Create();
                return Data.State;
            }
        }

        private int ClassLevel()
        {
            var cls = CharacterClass == null ? null : CharacterClass.Get();
            if (cls == null || Owner == null) return 0;
            return Owner.Progression.GetClassLevel(cls);
        }

        private int IntModifier()
        {
            if (Owner == null || Owner.Stats == null || Owner.Stats.Intelligence == null)
                return 0;
            return Owner.Stats.Intelligence.Bonus;
        }

        private string RestEventId()
        {
            long ticks = 0;
            try
            {
                if (Game.Instance != null && Game.Instance.TimeController != null)
                    ticks = Game.Instance.TimeController.GameTime.Ticks;
            }
            catch (NullReferenceException) { ticks = 0; }
            return (Owner == null ? "unknown" : Owner.UniqueId) + ":" + ticks;
        }

        protected override void OnActivate()
        {
            var state = State;
            if (string.IsNullOrEmpty(state.LastRestEventId))
                Occultist0099Focus.BeginCycle(state, ClassLevel(), IntModifier(),
                    RestEventId(), true);
            else
                Occultist0099Focus.ReacquireClass(state); // MF09: no free refill
            Occultist0099Focus.Validate(state);
        }

        protected override void OnPostLoad()
        {
            Occultist0099Focus.Validate(State);
        }

        protected override void OnApplyPostLoadFixes()
        {
            // Read-only recovery: buff rebuild, reload and reattached facts must
            // restore the existing snapshot without re-arming or recharging.
            Occultist0099Focus.Validate(State);
        }

        // Successful rest is the only cycle boundary. Duplicate callbacks for
        // the same rest event are idempotent in the authoritative state.
        public void HandleUnitRest(UnitEntityData unit)
        {
            if (unit == null || unit != Owner) return;
            var state = State;
            Occultist0099Focus.BeginCycle(state, ClassLevel(), IntModifier(),
                RestEventId(), false);
            Occultist0099Focus.Validate(state);
        }

        public bool IsOwnedBy(UnitEntityData unit) => unit != null && unit == Owner;

        public bool CanPaySchool(UnitEntityData caster, string school, int cost,
            string executionId)
            => IsOwnedBy(caster) &&
               Occultist0099Focus.CanPaySchool(State, school, cost, executionId);

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
            => Occultist0099Focus.Resonance(State, school);

        public Occultist0099FocusPower MindBarrier(string school)
            => Occultist0099Focus.MindBarrier(State, school);

        public string StateFingerprint() => State.Fingerprint();
    }

    // Private spellbook marker. Only this book may use the private circle
    // table; backlink/global spell-list data of other books stays untouched.
    [TypeId("0099c0e1a1b24c0d8e5f600000000002")]
    public sealed class Occultist0099SpellbookMarker0099 : BlueprintComponent
    {
        public int MaximumSpellLevel = 6;
        public bool ZeroQuotaSpellsKnownTable = true;

        public int PrivateCircle(string rootGuid, int nativeCircle)
            => Occultist0099BookScope.CircleOf(Occultist0099Ids.BookGuid,
                rootGuid, nativeCircle);
    }
}
