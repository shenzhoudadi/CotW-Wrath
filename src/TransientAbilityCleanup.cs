// Residual temporary abilities, added in 0.14.77.
//
// Two short-lived abilities are granted as ordinary Ability facts with
// HasActionBarSlot set, so the game also creates an action-bar slot for them:
//
//  * the delivery half of a sticky touch spell (Shocking Grasp, Vampiric Touch,
//    Cure Light Wounds, ...), created by AbilityEffectStickyTouch through
//    UnitPartTouch.Init;
//  * the wild shape "turn back" ability, granted while a WildShape/BeastShape
//    buff is active.
//
// Neither is meant to outlive its source.
//
// Why RemoveFact alone does not remove the wild shape one
// -----------------------------------------------------
// AbilityCollection.PrepareFactForDetach is the native gate that every removal
// request from this unit passes through:
//
//     if (fact.Data.HasActionBarSlot)
//     {
//         fact.TurnOff();
//         fact.Data.TemporarilyDisabled = true;
//         EventBus.RaiseEvent(... HandleAbilityRemoved ...);
//         return null;                     // <- removal request dropped
//     }
//     return fact;
//
// A null return makes EntityFactsManager.Remove skip the removal entirely, so
// RemoveFact looks like it succeeded while the ability is merely parked as a
// disabled icon. This is native bookkeeping: re-granting the same ability later
// finds the disabled instance, calls TurnOn and clears TemporarilyDisabled, so
// the bar keeps its position instead of accumulating duplicates.
//
// HasActionBarSlot is not a bool but Kingmaker.Utility.CountableFlag, a
// reference count kept by UnitUISettings.SetSlot (Retain) and
// UnitUISettings.RemoveSlot (Release, clamped by Math.Max so double release is
// harmless). Releasing the slot is therefore the native way to make the fact
// removable again; the ordinary RemoveFact path then performs the real removal,
// including Owner.UISettings.RemoveSlot inside OnFactWillDetach because the
// flag is already clear. No block-list of fact ids, no direct field poking, no
// replacement of the facts manager.
//
// Triggers are strictly event driven, one pass per occurrence:
//   * UnitBody.CancelPolymorphEffect - the moment a shape ends (Polymorph
//     .OnDeactivate has already tried and been refused, so this retries with the
//     slot released);
//   * EntityFactsManager.ApplyPostLoadFixes - once per entity after a save is
//     loaded, so debris saved by an older build is cleared on first load;
//   * TouchSpellsController.OnAbilityEffectApplied - a touch delivery consumed
//     its charge.
//
// Nothing here runs on a timer. An earlier build swept on Game.Tick and that
// rebuilt the action bar every second, so this file intentionally has no tick
// hook, and TransientDiagnostics below has none either.
using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class TransientAbilityCleanup
    {
        // WildShapeTurnBackAbility: referenced by every Classes/Druid/WildShape
        // shape buff, which is how the revert button reaches the bar.
        internal const string TurnBackAbilityId = "a2cb181ee69860b46b82844a3a8569b8";
        // TurnBackAbilityStandart is the standard-action sibling used by the
        // same shape buffs (CotW's own Wildshape.cs names both).
        private const string TurnBackStandardAbilityId = "bd09b025ee2a82f46afab922c4decca9";
        // CharacterOptionsPlus and EbonsContentMod both ship this component type
        // under the same TypeId. It is the only consumer of UnitPartTouch that
        // counts charges instead of removing the part outright, so a delivery
        // ability carrying it may legitimately stay for its remaining touches.
        private const string ChargeCounterName = "TouchCharges";

        internal static void Report(Exception exception) => Report(exception?.ToString());

        internal static void Report(string message)
        {
            try { Console.WriteLine("[CotW-Cleanup] " + message); } catch (Exception) { }
        }

        internal static bool HasTouchDelivery(BlueprintAbility blueprint)
        {
            var components = blueprint?.ComponentsArray;
            if (components == null) return false;
            for (var index = 0; index < components.Length; index++)
            {
                if (components[index] is AbilityDeliverTouch) return true;
            }
            return false;
        }

        internal static bool HasChargeCounter(BlueprintAbility blueprint)
        {
            var components = blueprint?.ComponentsArray;
            if (components == null) return false;
            for (var index = 0; index < components.Length; index++)
            {
                var component = components[index];
                if (component != null && component.GetType().Name == ChargeCounterName) return true;
            }
            return false;
        }

        internal static bool HoldsBarSlot(Ability ability) =>
            ability?.Data != null && ability.Data.HasActionBarSlot;

        // Releases the slot entry and with it the CountableFlag, which is what
        // the native detach gate looks at.
        //
        // Two steps on purpose:
        //   1. UnitUISettings.RemoveSlot replaces the slot that holds this
        //      AbilityData with an empty one, and SetSlot releases the flag that
        //      slot retained. This keeps the bar layout consistent.
        //   2. If the flag is still set the ability had no slot on the bar, so
        //      the stray retain is released directly. CountableFlag.Release is a
        //      clamped public API (Math.Max(count - 1, 0)), so this cannot
        //      underflow and repeating it is harmless. No private setter, no
        //      backing-field write.
        internal static bool ReleaseBarSlot(UnitDescriptor owner, Ability ability)
        {
            try
            {
                if (ability?.Data == null) return false;
                if (!ability.Data.HasActionBarSlot) return true;
                owner?.UISettings?.RemoveSlot(ability.Data);
                if (!ability.Data.HasActionBarSlot) return true;
                ability.Data.HasActionBarSlot.Release();
                return !ability.Data.HasActionBarSlot;
            }
            catch (Exception exception)
            {
                Report(exception);
                return false;
            }
        }

        // WildShapeTurnBackAbility (free) and TurnBackAbilityStandart: the
        // revert buttons that wild shape buffs grant. Both are ordinary
        // abilities carrying AbilityDeliverTouch, which is why they also show up
        // in the touch-delivery event log; they must never be mistaken for a
        // leftover delivery fact.
        private static readonly string[] TurnBackAbilityIds =
        {
            TurnBackAbilityId,
            TurnBackStandardAbilityId,
        };

        internal static bool IsTurnBack(Ability ability)
        {
            if (ability?.Blueprint == null) return false;
            var guid = ability.Blueprint.AssetGuid.ToString();
            for (var index = 0; index < TurnBackAbilityIds.Length; index++)
                if (guid == TurnBackAbilityIds[index]) return true;
            return false;
        }

        // A live shape's revert button is enabled. Once the native detach gate
        // refuses removal it leaves the fact turned off and parked with
        // TemporarilyDisabled set, which is exactly the state this cleanup
        // exists for. Requiring that flag keeps an in-progress shape safe no
        // matter when this runs relative to buff activation.
        internal static bool IsParkedTurnBack(Ability ability) =>
            IsTurnBack(ability) && ability.Data != null && ability.Data.TemporarilyDisabled;

        // The fact manager's own removal is the only path that also updates its
        // type cache and raises the detach notifications, so the slot is the
        // only thing this helper touches before handing over to RemoveFact.
        //
        // parkedOnly is used on load: a save taken mid-shape legitimately
        // carries a live revert button, and only the parked (disabled) instance
        // is debris. When a shape has just ended the caller already knows no
        // revert button can be legitimate, so every instance may go.
        internal static int RemoveTurnBack(UnitDescriptor owner, bool parkedOnly)
        {
            if (owner?.Unit == null || owner.Facts?.List == null) return 0;
            var removed = 0;
            foreach (var ability in owner.Facts.List.OfType<Ability>().ToArray())
            {
                if (parkedOnly ? !IsParkedTurnBack(ability) : !IsTurnBack(ability)) continue;
                ReleaseBarSlot(owner, ability);
                owner.Unit.RemoveFact(ability);
                if (owner.Facts.List.Contains(ability))
                {
                    Report("turn-back fact " + ability.UniqueId + " still attached after release (slot=" +
                        HoldsBarSlot(ability) + ", parked=" + Read(ability) + ")");
                    continue;
                }
                removed++;
            }
            if (removed > 0) Report("removed " + removed + " stray turn-back fact(s)");
            return removed;
        }

        private static string Read(Ability ability) => ability?.Data == null ? "?" : ability.Data.TemporarilyDisabled.ToString();

        // A delivery ability is ownerless when no UnitPartTouch points at this
        // instance any more: the part creates the fact, and removing the part
        // removes the fact, so a leftover without a part is debris. Charge
        // counted deliveries are left alone; they keep their remaining touches.
        //
        // requireParked is used on load. UnitPartTouch is a runtime part, so a
        // save taken while a touch charge was held can legitimately restore the
        // delivery fact before the part exists; only an instance the native
        // detach gate already turned off and parked is provably debris.
        internal static bool OwnerlessDelivery(UnitEntityData unit, Ability ability, bool requireParked)
        {
            if (unit == null || ability?.Blueprint == null) return false;
            // The revert buttons also carry AbilityDeliverTouch; they belong to
            // the shape, not to a UnitPartTouch, and are handled separately.
            if (IsTurnBack(ability)) return false;
            if (HasChargeCounter(ability.Blueprint)) return false;
            if (!HasTouchDelivery(ability.Blueprint)) return false;
            if (requireParked && (ability.Data == null || !ability.Data.TemporarilyDisabled)) return false;
            var part = unit.Get<UnitPartTouch>();
            return part == null || !ReferenceEquals(part.Ability, ability);
        }

        // Only ever called from a touch or load event, never from a timer.
        internal static int RemoveOwnerlessDeliveries(UnitEntityData unit, bool requireParked)
        {
            if (unit?.Descriptor?.Facts?.List == null) return 0;
            var removed = 0;
            foreach (var ability in unit.Descriptor.Facts.List.OfType<Ability>().ToArray())
            {
                if (!OwnerlessDelivery(unit, ability, requireParked)) continue;
                ReleaseBarSlot(unit.Descriptor, ability);
                unit.RemoveFact(ability);
                if (!unit.Descriptor.Facts.List.Contains(ability)) removed++;
            }
            return removed;
        }
    }

    // The shape is over: Polymorph.OnDeactivate has already asked for the
    // turn-back fact to be removed and been refused because the ability still
    // held a bar slot. Release that slot and repeat the same native request.
    [HarmonyPatch(typeof(UnitBody), nameof(UnitBody.CancelPolymorphEffect))]
    internal static class PolymorphExitTurnBackCleanup
    {
        private static void Postfix(UnitBody __instance)
        {
            try
            {
                var owner = __instance?.Owner;
                if (owner?.Unit == null) return;
                if (__instance.IsPolymorphed) return;
                var removed = TransientAbilityCleanup.RemoveTurnBack(owner, false);
                if (removed > 0)
                    TransientDiagnostics.Log("shape ended, cleared " + removed + " turn-back fact(s) on " +
                        TransientDiagnostics.Who(owner.Unit));
            }
            catch (Exception exception) { TransientAbilityCleanup.Report(exception); }
        }
    }

    // Debris written to a save by an earlier build. Runs once per entity right
    // after the save has been restored: EntityDataBase.ApplyPostLoadFixes calls
    // Facts.ApplyPostLoadFixes, whose own loop over a snapshot has already
    // finished, so removing facts here cannot disturb it.
    //
    // Only two things are removed, and both are provably ownerless:
    //   * a turn-back ability that is parked (TemporarilyDisabled) while the
    //     unit is not shaped - a live shape keeps its enabled revert button;
    //   * a touch delivery fact whose UnitPartTouch no longer points at it.
    [HarmonyPatch(typeof(EntityFactsManager), nameof(EntityFactsManager.ApplyPostLoadFixes))]
    internal static class LoadedTransientCleanup
    {
        private static void Postfix(EntityFactsManager __instance)
        {
            try
            {
                var unit = __instance?.Owner as UnitEntityData;
                if (unit?.Descriptor?.Facts?.List == null) return;
                if (unit.Body?.IsPolymorphed != true &&
                    unit.Descriptor.Facts.List.OfType<Ability>().Any(TransientAbilityCleanup.IsParkedTurnBack))
                {
                    var removed = TransientAbilityCleanup.RemoveTurnBack(unit.Descriptor, true);
                    if (removed > 0)
                        TransientDiagnostics.Log("load cleared " + removed + " turn-back fact(s) on " +
                            TransientDiagnostics.Who(unit));
                }
                var deliveries = TransientAbilityCleanup.RemoveOwnerlessDeliveries(unit, true);
                if (deliveries > 0)
                    TransientDiagnostics.Log("load cleared " + deliveries + " ownerless delivery fact(s) on " +
                        TransientDiagnostics.Who(unit));
            }
            catch (Exception exception) { TransientAbilityCleanup.Report(exception); }
        }
    }

    [HarmonyPatch(typeof(TouchSpellsController), nameof(TouchSpellsController.OnAbilityEffectApplied))]
    internal static class TouchDeliveryFactCleanup
    {
        private static void Postfix(AbilityExecutionContext context)
        {
            try
            {
                var caster = context?.MaybeCaster;
                if (caster == null) return;
                var part = caster.Get<UnitPartTouch>();
                if (part == null)
                {
                    // The native handler already released the part; nothing to do
                    // beyond clearing a delivery fact that outlived it. Here the
                    // part is the authority, so no parked check is needed.
                    TransientAbilityCleanup.RemoveOwnerlessDeliveries(caster, false);
                    return;
                }
                var ability = part.Ability;
                // Same guard as the native handler: only the part that matches the
                // applied ability is ours to remove.
                if (ability == null || ability.Data != context.Ability) return;
                // Charge-counted delivery abilities keep their remaining touches.
                if (TransientAbilityCleanup.HasChargeCounter(ability.Blueprint)) return;
                TransientAbilityCleanup.Report("touch delivery kept after apply, removing part " + ability.UniqueId);
                caster.Remove<UnitPartTouch>();
                TransientAbilityCleanup.RemoveOwnerlessDeliveries(caster, false);
            }
            catch (Exception exception) { TransientAbilityCleanup.Report(exception); }
        }
    }
}
