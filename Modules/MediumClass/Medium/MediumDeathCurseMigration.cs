using System.Linq;
using BlueprintCore.Utils;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Utilities;
using MediumClass.Utils;

namespace MediumClass.Medium
{
    internal static class MediumDeathCurseMigration
    {
        internal static readonly string[] LegacyBuffGuids = {
            Guids.MediumAdrielDeathCurseBuffNegativeFire,
            Guids.MediumAdrielDeathCurseBuffNegativeCold,
            Guids.MediumAdrielDeathCurseBuffNegativeElectricity,
            Guids.MediumAdrielDeathCurseBuffNegativeAcid,
            Guids.MediumAdrielDeathCurseBuffFireCold,
            Guids.MediumAdrielDeathCurseBuffFireElectricity,
            Guids.MediumAdrielDeathCurseBuffFireAcid,
            Guids.MediumAdrielDeathCurseBuffColdElectricity,
            Guids.MediumAdrielDeathCurseBuffColdAcid,
            Guids.MediumAdrielDeathCurseBuffElectricityAcid,
        };

        internal static void RemoveLegacyBuffs(UnitEntityData unit)
        {
            var legacy = LegacyBuffGuids.Select(BlueprintTool.Get<BlueprintBuff>).ToArray();
            foreach (var buff in unit.Buffs.Enumerable.Where(b => legacy.Contains(b.Blueprint)).ToArray())
                unit.Buffs.RemoveFact(buff);
        }

        /// <summary>
        /// 0.3.3 saves may hold 3-5 independent energy toggles; 0.3.4 allows two. Keeps the first two in
        /// the unit's buff-list (switch-on) order, removes the rest. Unrelated buffs are never touched;
        /// idempotent. The removal is an ordinary fact removal, so the next save no longer contains it.
        /// </summary>
        internal static void NormalizeActiveEnergies(UnitEntityData unit)
        {
            if (unit == null) return;
            try
            {
                int removed = MediumDeathCurseRules.Normalize(unit, buff => unit.Buffs.RemoveFact(buff));
                if (removed > 0)
                    Logging.GetLogger(nameof(MediumDeathCurseMigration))
                        .Log($"Death Curse: {removed} energy toggle(s) beyond the limit of {MediumDeathCurseRules.MaxActive} were switched off.");
            }
            catch (System.Exception e)
            {
                // Never fail the load: the damage path caps the effect at two energies anyway.
                try { Logging.GetLogger(nameof(MediumDeathCurseMigration)).LogException("Death Curse normalisation failed", e); }
                catch { /* logging is optional */ }
            }
        }
    }
}
