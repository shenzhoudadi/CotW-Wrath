using HarmonyLib;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;

namespace MediumClass.Medium
{
    [HarmonyPatch(typeof(AbilityCollection), "PrepareFactForDetach")]
    internal static class MediumSpiritAbilityRemovalPatch
    {
        private static bool Prefix(Ability fact, ref Ability __result)
        {
            if (!MediumSpiritPowerCleanup.IsRemoving(fact)) return true;
            // Only the exact fact currently removed by our spirit catalogue sweep.
            // Continue native detach, slot cleanup, events, cache removal and disposal.
            __result = fact;
            return false;
        }
    }

    [HarmonyPatch(typeof(ActivatableAbilityCollection), "PrepareFactForDetach")]
    internal static class MediumSpiritToggleRemovalPatch
    {
        private static bool Prefix(ActivatableAbility fact, ref ActivatableAbility __result)
        {
            if (!MediumSpiritPowerCleanup.IsRemoving(fact)) return true;
            // Native toggle collections retain slotted facts independently of
            // AbilityCollection. The spirit cleanup/old-save repair has already
            // checked ownership; continue native deactivation, UI notification,
            // cache removal and disposal instead of keeping a disabled button.
            __result = fact;
            return false;
        }
    }
}
