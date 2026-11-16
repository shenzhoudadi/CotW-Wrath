using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using MediumClass.Features.MediumSpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using System;

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
        internal static int AdjustedBaseRank(UnitEntityData owner, BlueprintFeature bonusFeature)
        {
            if (owner?.Progression == null || bonusFeature == null) return 0;
            int rank = owner.Progression.Features.GetRank(bonusFeature);
            if (rank <= 0) return 0;

            int mediumLevel = MediumSpiritSpellbookRules.MediumLevel(owner.Descriptor);
            if (MediumAttributeRules.HasDiminishedSpiritBonus(owner.Descriptor))
            {
                int penalty = mediumLevel >= 12 ? 2 : 1;
                rank = Math.Max(0, rank - penalty);
            }
            if (MediumAttributeRules.HasFocusedSpiritualism(owner.Descriptor))
            {
                int bonus = 1
                    + (mediumLevel >= 10 ? 1 : 0)
                    + (mediumLevel >= 14 ? 1 : 0)
                    + (mediumLevel >= 18 ? 1 : 0);
                rank += bonus;
            }
            return rank;
        }

        internal static int ForSpirit(UnitEntityData owner, BlueprintCharacterClassReference spirit)
        {
            var medium = owner?.Get<UnitPartMedium>();
            if (medium == null || spirit == null || !medium.Spirits.TryGetValue(spirit, out var entry)) return 0;
            return AdjustedBaseRank(owner, entry.SpiritBonus.SpiritBonusFeature?.Get())
                + MultiSpiritRules.EffectiveSpiritFocus(medium, spirit);
        }

        internal static int Of(UnitEntityData owner)
        {
            var medium = owner?.Get<UnitPartMedium>();
            if (medium == null) return 0;
            int best = 0;
            foreach (var spirit in medium.ActiveSpiritClasses)
            {
                int value = ForSpirit(owner, spirit);
                if (value > best) best = value;
            }
            return best;
        }
    }
}
