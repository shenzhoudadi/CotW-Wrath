using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Trickster
{
    class TransferMagic
    {
        private static readonly string FeatName = "TricksterTransferMagic";
        private static readonly string DisplayName = "TricksterTransferMagic.Name";
        private static readonly string Description = "TricksterTransferMagic.Description";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(TransferMagic));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Trickster Swift Break (Greater Spirit Power)");

            var buff = BuffConfigurator.New(FeatName + "Buff", Guids.TricksterSwiftBreakBuff)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/transfermagic.png")
                .SetStacking(StackingType.Replace)
                .AddComponent<TricksterSwiftBreakComponent>()
                .Configure();

            // Keep legacy TricksterTransferMagicEffect GUID resolvable for existing saves.
            AbilityConfigurator.New(FeatName + "Effect", Guids.TricksterTransferMagicEffect)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/transfermagic.png")
                .SetType(AbilityType.Supernatural)
                .Configure();

            var ability = AbilityConfigurator.New(FeatName + "Ability", Guids.TricksterTransferMagicAbility)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/transfermagic.png")
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Medium)
                .SetActionType(UnitCommand.CommandType.Swift)
                .SetCanTargetEnemies(true)
                .SetCanTargetSelf(false)
                .SetCanTargetPoint(false)
                .SetCanTargetFriends(false)
                .SetSpellResistance(false)
                .SetAnimation(UnitAnimationActionCastSpell.CastAnimationStyle.Point)
                .AddAbilityResourceLogic(amount: 3, isSpendResource: true, requiredResource: Guids.MediumInfluenceResource)
                .AddAbilityEffectRunAction(
                    actions: ActionsBuilder.New()
                        .Add<ContextActionSpiritInfluence>()
                        .ApplyBuff(buff, ContextDuration.Fixed(1, DurationRate.Rounds), isFromSpell: false, isNotDispelable: true))
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.TricksterTransferMagic)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddFacts(new() { ability })
                .Configure();

            var secondability = AbilityConfigurator.New(FeatName + "AbilitySecondary", Guids.SecondaryTricksterTransferMagicAbility)
                .CopyFrom(ability, c => c is not AbilityResourceLogic)
                .AddAbilityResourceLogic(amount: 3, isSpendResource: true, requiredResource: Guids.MediumInfluenceResourceTrickster)
                .Configure();

            FeatureConfigurator.New(FeatName + "Secondary", Guids.SecondaryTricksterTransferMagic)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddFacts(new() { secondability })
                .AddRemoveFeatureOnApply(Guids.TricksterTransferMagic)
                .Configure();
        }
    }
}
