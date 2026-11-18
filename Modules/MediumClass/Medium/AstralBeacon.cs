using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.NewComponents;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium
{
    class AstralBeacon
    {
        private static readonly string FeatName = "AstralBeacon";
        private static readonly string DisplayName = "AstralBeacon.Name";
        private static readonly string Description = "AstralBeacon.Description";
        private const string IconPrefix = "assets/icons/";
        private const string TranceOfThreeIconName = "tranceofthree.png";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(AstralBeacon));

        public static void ConfigureEnabled()
        {
            AbilityResourceConfigurator.New(FeatName + "Champion", Guids.MediumInfluenceResourceChampion).SetMaxAmount(new BlueprintAbilityResource.Amount
            {
                BaseValue = 5,
                IncreasedByLevel = false,
                IncreasedByLevelStartPlusDivStep = false,
                StartingLevel = 0,
                StartingIncrease = 0,
                LevelStep = 0,
                PerStepIncrease = 0,
                MinClassLevelIncrease = 0,
                IncreasedByStat = false,
                ResourceBonusStat = StatType.Charisma
            }).Configure();

            AbilityResourceConfigurator.New(FeatName + "Archmage", Guids.MediumInfluenceResourceArchmage).SetMaxAmount(new BlueprintAbilityResource.Amount
            {
                BaseValue = 5,
                IncreasedByLevel = false,
                IncreasedByLevelStartPlusDivStep = false,
                StartingLevel = 0,
                StartingIncrease = 0,
                LevelStep = 0,
                PerStepIncrease = 0,
                MinClassLevelIncrease = 0,
                IncreasedByStat = false,
                ResourceBonusStat = StatType.Charisma
            }).Configure();

            

            AbilityResourceConfigurator.New(FeatName + "Guardian", Guids.MediumInfluenceResourceGuardian).SetMaxAmount(new BlueprintAbilityResource.Amount
            {
                BaseValue = 5,
                IncreasedByLevel = false,
                IncreasedByLevelStartPlusDivStep = false,
                StartingLevel = 0,
                StartingIncrease = 0,
                LevelStep = 0,
                PerStepIncrease = 0,
                MinClassLevelIncrease = 0,
                IncreasedByStat = false,
                ResourceBonusStat = StatType.Charisma
            }).Configure();

            AbilityResourceConfigurator.New(FeatName + "Hierophant", Guids.MediumInfluenceResourceHierophant).SetMaxAmount(new BlueprintAbilityResource.Amount
            {
                BaseValue = 5,
                IncreasedByLevel = false,
                IncreasedByLevelStartPlusDivStep = false,
                StartingLevel = 0,
                StartingIncrease = 0,
                LevelStep = 0,
                PerStepIncrease = 0,
                MinClassLevelIncrease = 0,
                IncreasedByStat = false,
                ResourceBonusStat = StatType.Charisma
            }).Configure();

            AbilityResourceConfigurator.New(FeatName + "Marshal", Guids.MediumInfluenceResourceMarshal).SetMaxAmount(new BlueprintAbilityResource.Amount
            {
                BaseValue = 5,
                IncreasedByLevel = false,
                IncreasedByLevelStartPlusDivStep = false,
                StartingLevel = 0,
                StartingIncrease = 0,
                LevelStep = 0,
                PerStepIncrease = 0,
                MinClassLevelIncrease = 0,
                IncreasedByStat = false,
                ResourceBonusStat = StatType.Charisma
            }).Configure();

            AbilityResourceConfigurator.New(FeatName + "Trickster", Guids.MediumInfluenceResourceTrickster).SetMaxAmount(new BlueprintAbilityResource.Amount
            {
                BaseValue = 5,
                IncreasedByLevel = false,
                IncreasedByLevelStartPlusDivStep = false,
                StartingLevel = 0,
                StartingIncrease = 0,
                LevelStep = 0,
                PerStepIncrease = 0,
                MinClassLevelIncrease = 0,
                IncreasedByStat = false,
                ResourceBonusStat = StatType.Charisma
            }).Configure();

            // 20 级重做：星界灯塔不再是「全天候自动借用其余五个职阶」，
            // 而是一个自由动作：承受 2 点共鸣，指定一个当前未降灵的职阶，
            // 获得它的中等／高等／至高英灵之力，持续 2 轮。
            // 授予与撤销都走 ApplySpirits.GrantSecondary/RevokeSecondary，
            // 与真正降灵共用同一套 overwrite 与诡术师 rank 规则。
            // 变体按 LegendVariants（降灵特性的技能表）自动生成，新增传奇英灵
            // 会自动出现在这里，不需要改这个文件。
            var variants = new List<Blueprint<BlueprintAbilityReference>>();
            foreach (var legend in LegendVariants.All())
                variants.Add(Variant(legend.Spirit, legend.Channel));

            var ability = AbilityConfigurator.New(FeatName + "Ability", Guids.AstralBeaconAbility)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(IconPrefix + TranceOfThreeIconName)
                .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
                .AddComponent<AbilityRequirementHasSpirit>()
                .AddAbilityVariants(variants)
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.AstralBeacon)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(IconPrefix + TranceOfThreeIconName)
                .AddAbilityResources(amount: 0, resource: Guids.MediumInfluenceResourceArchmage, useThisAsResource: false, restoreAmount: true)
                .AddAbilityResources(amount: 0, resource: Guids.MediumInfluenceResourceChampion, useThisAsResource: false, restoreAmount: true)
                .AddAbilityResources(amount: 0, resource: Guids.MediumInfluenceResourceGuardian, useThisAsResource: false, restoreAmount: true)
                .AddAbilityResources(amount: 0, resource: Guids.MediumInfluenceResourceHierophant, useThisAsResource: false, restoreAmount: true)
                .AddAbilityResources(amount: 0, resource: Guids.MediumInfluenceResourceMarshal, useThisAsResource: false, restoreAmount: true)
                .AddAbilityResources(amount: 0, resource: Guids.MediumInfluenceResourceTrickster, useThisAsResource: false, restoreAmount: true)
                .AddFacts(new() { ability })
                .Configure();
        }

        /// <summary>
        /// One legend's variant: a free action that costs two influence and
        /// applies a two-round buff. The buff carries the legend, and
        /// <see cref="MediumAstralBeaconComponent"/> turns that into the
        /// intermediate/greater/supreme powers for its duration.
        /// Name and icon come from the legend's own channel ability, so the
        /// player sees the same legend they pick in the channel list.
        /// </summary>
        private static BlueprintAbility Variant(BlueprintCharacterClassReference legend, BlueprintAbility channel)
        {
            var suffix = LegendVariants.Suffix(legend, channel);
            var name = LegendVariants.NameKey(legend, channel, DisplayName);
            var icon = LegendVariants.Icon(channel);
            var spirit = BlueprintTool.GetRef<BlueprintCharacterClassReference>(legend.ToString());

            var buffConfig = BuffConfigurator.New(FeatName + "Buff" + suffix, LegendVariants.Id("AstralBeacon.Buff", legend))
                .SetDisplayName(name)
                .SetDescription(Description)
                .AddComponent<MediumAstralBeaconComponent>(c => c.Spirit = spirit);
            var buff = (icon != null ? buffConfig.SetIcon(icon) : buffConfig.SetIcon(IconPrefix + TranceOfThreeIconName))
                .Configure();

            var abilityConfig = AbilityConfigurator.New(FeatName + "Ability" + suffix, LegendVariants.Id("AstralBeacon.Ability", legend))
                .SetDisplayName(name)
                .SetDescription(Description)
                .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
                .AddComponent<AbilityRequirementHasSpirit>()
                .AddComponent<AbilityRequirementSpiritAvailable>(c => c.Spirit = spirit)
                .AddAbilityResourceLogic(amount: 2, requiredResource: Guids.MediumInfluenceResource, isSpendResource: true)
                .AddAbilityEffectRunAction(actions: ActionsBuilder.New()
                    .Add<ContextActionSpiritInfluence>()
                    .ApplyBuff(buff, ContextDuration.Fixed(2, DurationRate.Rounds), toCaster: true));
            return (icon != null ? abilityConfig.SetIcon(icon) : abilityConfig.SetIcon(IconPrefix + TranceOfThreeIconName))
                .Configure();
        }
    }
}
