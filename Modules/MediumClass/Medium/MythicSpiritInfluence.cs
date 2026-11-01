using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;

namespace MediumClass.Medium
{
    // Mythic Spirit Influence: allies mirror the Medium's channeled spirit bonus.
    internal static class MythicSpiritInfluence
    {
        internal const string FeatureGuid = "3a22cb29-e705-4cba-ab2a-fc4be87176cc";
        private const string DisplayName = "MediumSpiritInfluence.Name";
        private const string Description = "MediumSpiritInfluence.Description";

        internal static void Configure()
        {
            // Hidden marker on the Medium, applied while a seance is active.
            BlueprintBuff marker = BuffConfigurator.New("MediumSpiritInfluenceBuff", Guids.MediumSpiritInfluenceBuff)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetFlags(BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.RemoveOnRest)
                .AddComponent<MediumContextSpiritInfluenceComponent>()
                .Configure();

            // The mirrored bonus each ally receives while the Medium channels.
            FeatureConfigurator.New("MediumSpiritInfluenceAlly", Guids.MediumSpiritInfluenceAlly)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/tranceofthree.png")
                .SetIsClassFeature(true)
                .AddComponent<MediumSpiritInfluenceAllyComponent>()
                .Configure();

            // Let BlueprintCore register the MythicAbility group once in every
            // matching selection. Do not append the same feature a second time.
            FeatureConfigurator.New("MediumSpiritInfluence", FeatureGuid)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/tranceofthree.png")
                .AddToGroups(FeatureGroup.MythicAbility)
                .AddPrerequisiteFeature(Guids.MediumChannelSpirit)
                .AddComponent<MediumSpiritInfluenceFeatureComponent>()
                .AddFactsChangeTrigger(checkedFacts: new() { BlueprintTool.GetRef<BlueprintUnitFactReference>(Guids.MediumChannelSpiritPrimarySpiritBuff) },
                    onFactGainedActions: ActionsBuilder.New().ApplyBuffPermanent(marker),
                    onFactLostActions: ActionsBuilder.New().RemoveBuff(marker))
                .Configure();
        }
    }
}
