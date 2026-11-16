// COTW-0099 Occultist, B stage, first vertical slice: Abjuration.
// Native Wrath adapter for the authoritative state in Occultist0099Rules.cs.
// The same state object is used by the UI precheck, the real payment and the
// rest/level-up events; no mirrored spend counter exists.
//
// This slice is development-only. Nothing here registers a class, spellbook or
// progression blueprint in the shared registration chain; that happens once
// eight schools, four Panoplies and the full private spell table are complete.
//
// D-0099-08 fixes:
//  - Rest identity no longer depends on GameTime.Ticks. A successful rest is
//    identified by the monotonic CampingState.OverallCampsCount (incremented
//    once per camp in RestController.CampCoroutine, BEFORE ApplyRest fires
//    HandleUnitRest), so repeated callbacks of the same rest are idempotent
//    and two different rests can never collapse into one id.
//  - OnActivate refuses to build a B=0 cycle when the CharacterClass reference
//    is unset or the class level is 0; it waits for a real rest/acquisition.
//  - [TypeId] attributes reference the Ids constants, not hardcoded hex.
using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Rest;
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

    [TypeId(Occultist0099Ids.FocusFactTypeId)]
    public sealed class Occultist0099FocusFact0099 :
        UnitFactComponentDelegate<Occultist0099NativeData>,
        IUnitRestHandler, IRestFinishedHandler, IUnitSubscriber
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

        private int IntelligenceScore()
        {
            if (Owner == null || Owner.Stats == null || Owner.Stats.Intelligence == null)
                return 10;
            return Owner.Stats.Intelligence.ModifiedValue;
        }

        // D-0099-08: the rest event identity is the camp serial number, which
        // RestController increments exactly once per camp before any
        // HandleUnitRest callback. It is stable across the multiple per-unit
        // callbacks of one rest and distinct between rests, unlike GameTime
        // ticks (which advance mid-rest and can be 0 on exception).
        private string RestEventId()
        {
            string unit = Owner == null ? "unknown" : Owner.UniqueId;
            int camp = -1;
            try
            {
                if (Game.Instance != null && Game.Instance.Player != null &&
                    Game.Instance.Player.Camping != null)
                    camp = Game.Instance.Player.Camping.OverallCampsCount;
            }
            catch (NullReferenceException) { camp = -1; }
            return unit + ":camp:" + camp;
        }

        // First real acquisition: only establish a cycle when the class is
        // actually present at a positive level. A missing reference or level 0
        // must NOT create a B=0 cycle that would reject every later invest.
        protected override void OnActivate()
        {
            var state = State;
            if (string.IsNullOrEmpty(state.LastRestEventId))
            {
                int level = ClassLevel();
                if (CharacterClass != null && CharacterClass.Get() != null && level > 0)
                {
                    Occultist0099Focus.BeginCycle(state, level, IntModifier(),
                        RestEventId(), true);
                    state.Intelligence = IntelligenceScore();
                }
                // else: wait for a real rest / acquisition; do not arm B=0.
            }
            else
            {
                Occultist0099Focus.ReacquireClass(state); // MF09: no free refill
            }
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
        // the same rest event are idempotent in the authoritative state because
        // the camp serial is unchanged across them.
        public void HandleUnitRest(UnitEntityData unit)
        {
            if (unit == null || unit != Owner) return;
            var state = State;
            int level = ClassLevel();
            if (CharacterClass == null || CharacterClass.Get() == null || level <= 0)
            {
                // No valid class yet: do not create a degenerate cycle.
                Occultist0099Focus.Validate(state);
                return;
            }
            Occultist0099Focus.BeginCycle(state, level, IntModifier(),
                RestEventId(), false);
            state.Intelligence = IntelligenceScore();
            Occultist0099Focus.Validate(state);
        }

        // Rest-finished is a secondary confirmation path. It uses the same camp
        // serial identity, so it is idempotent with HandleUnitRest and never
        // re-arms a cycle that the per-unit callback already established.
        public void HandleRestFinished(RestStatus status)
        {
            if (status == null || !status.RestSucceeded) return;
            var unit = Owner;
            if (unit == null) return;
            HandleUnitRest(unit);
        }

        public bool IsOwnedBy(UnitEntityData unit) => unit != null && unit == Owner;

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
        public BlueprintAbilityResourceReference FocusResource;

        public void SyncResourceMirror()
        {
            var resource = FocusResource?.Get();
            var resources = Owner?.Descriptor?.Resources;
            if (resource == null || resources == null) return;
            // PersistantResources is the native serialized view of the same
            // live UnitAbilityResource objects, so this writes the real counter
            // without reaching into a private dictionary.
            var native = resources.PersistantResources
                .FirstOrDefault(r => r.Blueprint == resource);
            if (native == null) return;
            native.Amount = State.Available;
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
            => Occultist0099Focus.Resonance(State, school);

        public Occultist0099FocusPower MindBarrier(string school)
            => Occultist0099Focus.MindBarrier(State, school);

        public string StateFingerprint() => State.Fingerprint();
    }

    // Private spellbook marker. Only this book may use the private circle
    // table; backlink/global spell-list data of other books stays untouched.
    [TypeId(Occultist0099Ids.SpellbookMarkerTypeId)]
    public sealed class Occultist0099SpellbookMarker0099 : BlueprintComponent
    {
        public int MaximumSpellLevel = 6;
        public bool ZeroQuotaSpellsKnownTable = true;

        public int PrivateCircle(string rootGuid, int nativeCircle)
            => Occultist0099BookScope.CircleOf(Occultist0099Ids.BookGuid,
                rootGuid, nativeCircle);
    }
}