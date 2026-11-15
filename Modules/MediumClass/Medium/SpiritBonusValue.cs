using Kingmaker.EntitySystem.Entities;
using MediumClass.Medium.NewUnitParts;

namespace MediumClass.Medium
{
    /// <summary>
    /// The number a medium's "spirit bonus" currently is: the highest rank plus spirit focus among
    /// the spirits it has actually channelled. Temporary lends (Astral Beacon, Trance of Many) never
    /// enter ActiveSpiritClasses, so they do not count — the same rule the AC bonus and the mythic
    /// multi-spirit cap use.
    ///
    /// Read on demand instead of cached on the unit, so channelling a different spirit is reflected
    /// without a refresh hook.
    /// </summary>
    internal static class SpiritBonusValue
    {
        internal static int Of(UnitEntityData owner)
        {
            var medium = owner?.Get<UnitPartMedium>();
            if (medium == null) return 0;
            int best = 0;
            foreach (var spirit in medium.ActiveSpiritClasses)
            {
                if (!medium.Spirits.ContainsKey(spirit)) continue;
                var entry = medium.Spirits[spirit];
                int value = owner.Progression.Features.GetRank(entry.SpiritBonus.SpiritBonusFeature)
                    + MultiSpiritRules.EffectiveSpiritFocus(medium, spirit);
                if (value > best) best = value;
            }
            return best;
        }
    }
}
