using Kingmaker.EntitySystem.Stats;
using System.Linq;

namespace MediumClass.Medium
{
    // Marshal's passive bonus and Marshal's Order have different scopes.
    // Never add to the Charisma score: that would also change spell DCs/slots.
    internal static class MarshalSpiritRules
    {
        internal static StatType[] PassiveBonusStats => new[] {
            StatType.SkillPersuasion, StatType.SkillUseMagicDevice
        };

        internal static StatType[] AttributeChecks => new[] { StatType.Strength, StatType.Dexterity,
            StatType.Constitution, StatType.Intelligence, StatType.Wisdom, StatType.Charisma };

        // The active surge retains the broad roll coverage of Marshal's Order.
        internal static StatType[] SurgeStats => new[] { StatType.AdditionalAttackBonus }
            .Concat(StatTypeHelper.Saves).Concat(StatTypeHelper.Skills).Distinct().ToArray();
    }
}
