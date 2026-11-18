using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using Kingmaker.Enums;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Archmage
{
    class SeanceBoon
    {
        private static readonly string FeatName = "ArchmageSeanceBoon";
        private static readonly string DisplayName = "ArchmageSeanceBoon.Name";
        private static readonly string Description = "ArchmageSeanceBoon.Description";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(SeanceBoon));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Archmage Seance Boon");
            var buff = BuffConfigurator.New(FeatName + "Buff", Guids.ArchmageSeanceBoonBuff)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/spiritarchmage.png")
                .AddIncreaseAllSpellsDC(descriptor: ModifierDescriptor.UntypedStackable, spellsOnly: false, value: 1)
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.ArchmageSeanceBoon)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/spiritarchmage.png")
                .AddFacts(new() { buff })
                .Configure();
        }
    }
}
