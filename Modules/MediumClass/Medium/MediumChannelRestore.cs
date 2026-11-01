using System.Linq;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.NewComponents;
using MediumClass.Utilities;

namespace MediumClass.Medium
{
    internal static class MediumChannelRestore
    {
        internal static void Restore(UnitDescriptor owner)
        {
            if (owner?.Unit == null) return;
            // 0.3.4: first, so the registrations below already see the recovered feature set.
            MediumProgressionMigration.Run(owner);
            MediumDeathCurseMigration.RemoveLegacyBuffs(owner.Unit);
            MediumDeathCurseMigration.NormalizeActiveEnergies(owner.Unit);
            // The catalogue is derived from owned class facts, never serialized.
            // Rebuild it even when native fact TurnOn has not happened yet.
            bool found = false;
            foreach (var id in new[] { Guids.MediumChannelSpirit, Guids.ProwlerSpirit })
            {
                var fact = owner.Unit.Facts.Get(BlueprintTool.Get<BlueprintFeature>(id));
                if (fact == null || !fact.IsActive) continue;
                found = true;
                fact.CallComponents<MediumSpiritComponent>(c => c.Register());
            }
            if (!found) return;
            var state = owner.Unit.Get<UnitPartMedium>();
            if (state == null) return;

            // Focus can turn on before the catalogue on older saves. Reconcile
            // its derived entries only after every spirit has been registered.
            foreach (var fact in owner.Unit.Facts.List.Where(f => f.IsActive).ToArray())
                fact.CallComponents<MediumSpiritFocusComponent>(c => c.OnTurnOn());

            var channel = owner.Buffs.Enumerable.FirstOrDefault(b => b.Blueprint ==
                BlueprintTool.Get<BlueprintBuff>(Guids.MediumChannelSpiritPrimarySpiritBuff));
            if (channel == null)
            {
                state.ClearChannelSelection();
                owner.Unit.Get<UnitPartMediumPreparedSpells>()?.Clear();
                MediumSpiritPowerCleanup.RevokeAll(owner.Unit);
                MediumSpiritPowerCleanup.RevokeSeanceOrphans(owner.Unit);
                MediumDeathCurseModes.Restore(owner);
                return;
            }
            // A present but not-yet-activated marker can occur during restoration.
            // Preserve its powers and saved choice until native activation finishes.
            if (!channel.IsActive) return;
            state.RestoreSavedSelection();
            // Restore only derived powers. Reapply/OnDeactivate would end the
            // session, reset influence, and remove its prepared spell grants.
            channel.CallComponents<ApplySpirits>(c => c.RefreshActiveSpirits());
            MediumDeathCurseModes.Restore(owner);
            foreach (var buff in owner.Buffs.Enumerable.Where(b => b.IsActive && b.IsTurnedOn
                && b.Blueprint == BlueprintTool.Get<BlueprintBuff>(Guids.MediumSharedSeanceBuff)).ToArray())
                buff.CallComponents<MediumContextSharedSeanceComponent>(c => c.OnTurnOn());
            foreach (var buff in owner.Buffs.Enumerable.Where(b => b.IsActive && b.IsTurnedOn
                && b.Blueprint == BlueprintTool.Get<BlueprintBuff>(Guids.MediumSpiritInfluenceBuff)).ToArray())
                buff.CallComponents<MediumContextSpiritInfluenceComponent>(c => c.RefreshAllies());
        }
    }

    [HarmonyPatch(typeof(UnitDescriptor), nameof(UnitDescriptor.ApplyPostLoadFixes))]
    internal static class MediumChannelRestorePatch
    {
        // Before native spellbook fixes and both of our spell/influence postfixes:
        // they must see the recovered six-circle progression and channel choice.
        private static void Prefix(UnitDescriptor __instance) => MediumChannelRestore.Restore(__instance);
    }
}
