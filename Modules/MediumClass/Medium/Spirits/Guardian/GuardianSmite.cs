using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Guardian
{
    class GuardianSmite
    {
        private static readonly string FeatName = "GuardianGreater";
        private static readonly string DisplayName = "GuardianGreater.Name";
        private static readonly string Description = "GuardianGreater.Description";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(GuardianSmite));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Guardian Determination (Greater Spirit Power)");

            var reachBuff = BuffConfigurator.New(FeatName + "ReachBuff", Guids.GuardianGreaterReachBuff)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/guardiansmite.png")
                .SetFlags(BlueprintBuff.Flags.RemoveOnRest)
                .SetStacking(StackingType.Replace)
                .AddComponent<GuardianDeterminationReachComponent>()
                .Configure();

            // Keep legacy temp-HP blueprints resolvable for existing saves, but do not grant them.
            var tempHpBuff = BuffConfigurator.New(FeatName + "TempHpBuff", Guids.GuardianGreaterTempHpBuff)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/spiritguardian.png")
                .SetFlags(BlueprintBuff.Flags.RemoveOnRest)
                .SetStacking(StackingType.Replace)
                .AddComponent<GuardianDeterminationTempHpComponent>()
                .Configure();

            var fullRoundAbility = AbilityConfigurator.New(FeatName + "Ability", Guids.GuardianGreaterAbility)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/guardiansmite.png")
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .SetActionType(UnitCommand.CommandType.Standard)
                .SetIsFullRoundAction(true)
                .AddAbilityEffectRunAction(
                    actions: ActionsBuilder.New()
                        .ApplyBuff(reachBuff, ContextDuration.Fixed(1, DurationRate.Rounds), toCaster: true, isFromSpell: false, isNotDispelable: true))
                .Configure();

            var freeReachAbility = AbilityConfigurator.New(FeatName + "FreeAbility", Guids.GuardianGreaterFreeAbility)
                .SetDisplayName("GuardianGreaterFree.Name")
                .SetDescription("GuardianGreaterFree.Description")
                .SetIcon("assets/icons/guardiansmite.png")
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .SetActionType(UnitCommand.CommandType.Free)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: Guids.MediumInfluenceResource)
                .AddAbilityEffectRunAction(
                    actions: ActionsBuilder.New()
                        .Add<ContextActionSpiritInfluence>()
                        .ApplyBuff(reachBuff, ContextDuration.Fixed(1, DurationRate.Rounds), toCaster: true, isFromSpell: false, isNotDispelable: true))
                .Configure();

            var tempHpAbility = AbilityConfigurator.New(FeatName + "TempHpAbility", Guids.GuardianGreaterTempHpAbility)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/spiritguardian.png")
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .SetActionType(UnitCommand.CommandType.Free)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: Guids.MediumInfluenceResource)
                .AddAbilityEffectRunAction(
                    actions: ActionsBuilder.New()
                        .Add<ContextActionSpiritInfluence>()
                        .ApplyBuff(tempHpBuff, ContextDuration.Fixed(10, DurationRate.Rounds), toCaster: true, isFromSpell: false, isNotDispelable: true))
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.GuardianGreater)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddFacts(new() { fullRoundAbility, freeReachAbility })
                .Configure();

            var secondaryFreeReach = AbilityConfigurator.New(FeatName + "SecondaryFreeAbility", Guids.SecondaryGuardianGreaterFreeAbility)
                .CopyFrom(freeReachAbility, c => c is not AbilityResourceLogic)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: Guids.MediumInfluenceResourceGuardian)
                .Configure();

            AbilityConfigurator.New(FeatName + "SecondaryTempHpAbility", Guids.SecondaryGuardianGreaterTempHpAbility)
                .CopyFrom(tempHpAbility, c => c is not AbilityResourceLogic)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: Guids.MediumInfluenceResourceGuardian)
                .Configure();

            // Keep SecondaryGuardianGreaterAbility guid resolvable for existing saves.
            AbilityConfigurator.New(FeatName + "SecondaryAbility", Guids.SecondaryGuardianGreaterAbility)
                .CopyFrom(fullRoundAbility)
                .Configure();

            FeatureConfigurator.New(FeatName + "Secondary", Guids.SecondaryGuardianGreater)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddFacts(new() { fullRoundAbility, secondaryFreeReach })
                .AddRemoveFeatureOnApply(Guids.GuardianGreater)
                .Configure();
        }
    }
}
