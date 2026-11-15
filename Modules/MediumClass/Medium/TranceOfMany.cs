using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium.NewComponents;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System.Collections.Generic;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium
{
    /// <summary>
    /// 10 级「多重降灵」：降灵之后还能再选一位尚未降灵的英灵，只借用它的英灵
    /// 之力——10 级给中等，14 级加高等，18 级加至高。与神话「多重降灵」不同，
    /// 它不会把那位英灵变成真正的第二主英灵：不给英灵同调、降灵奖励、共鸣惩罚
    /// 或法术，只借用那几层力量。
    ///
    /// 形态：一个父技能 + 每位英灵一个变体技能（子选项）。变体由
    /// <see cref="LegendVariants"/> 从降灵特性自动生成，所以新增传奇英灵会
    /// 自动出现在这里。
    /// </summary>
    class TranceOfMany
    {
        private static readonly string FeatName = "MediumTranceOfMany";
        private static readonly string DisplayName = "MediumTranceOfMany.Name";
        private static readonly string Description = "MediumTranceOfMany.Description";
        private const string IconPrefix = "assets/icons/";
        private const string IconName = "tranceofthree.png";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(TranceOfMany));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Medium Trance of Many");

            var variants = new List<Blueprint<BlueprintAbilityReference>>();
            foreach (var legend in LegendVariants.All())
                variants.Add(Variant(legend.Spirit, legend.Channel));

            var ability = AbilityConfigurator.New(FeatName + "Ability", Guids.MediumTranceOfManyAbility)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(IconPrefix + IconName)
                .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
                .AddComponent<AbilityRequirementHasSpirit>()
                .AddAbilityVariants(variants)
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.MediumTranceOfMany)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(IconPrefix + IconName)
                .SetHideInUI(false)
                .SetHideNotAvailibleInUI(false)
                .SetIsClassFeature(true)
                .SetReapplyOnLevelUp(false)
                .SetRanks(1)
                .AddFacts(new() { ability })
                .Configure();
        }

        /// <summary>
        /// One legend's variant: a free action that applies a buff lasting until
        /// the channel ends. The buff's component reads the medium's level to
        /// decide how many tiers to lend and upgrades itself on level up.
        /// </summary>
        private static BlueprintAbility Variant(BlueprintCharacterClassReference legend, BlueprintAbility channel)
        {
            var suffix = LegendVariants.Suffix(legend, channel);
            var name = LegendVariants.NameKey(legend, channel, DisplayName);
            var icon = LegendVariants.Icon(channel);
            var spirit = BlueprintTool.GetRef<BlueprintCharacterClassReference>(legend.ToString());

            var buffConfig = BuffConfigurator.New(FeatName + "Buff" + suffix, LegendVariants.Id("TranceOfMany.Buff", legend))
                .SetDisplayName(name)
                .SetDescription(Description)
                .AddComponent<MediumTranceOfManyComponent>(c => c.Spirit = spirit);
            var buff = (icon != null ? buffConfig.SetIcon(icon) : buffConfig.SetIcon(IconPrefix + IconName))
                .Configure();
            SecondaryLend.Track(buff);

            var abilityConfig = AbilityConfigurator.New(FeatName + "Ability" + suffix, LegendVariants.Id("TranceOfMany.Ability", legend))
                .SetDisplayName(name)
                .SetDescription(Description)
                .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
                .AddComponent<AbilityRequirementHasSpirit>()
                .AddComponent<AbilityRequirementSpiritAvailable>(c => c.Spirit = spirit)
                .AddAbilityEffectRunAction(actions: ActionsBuilder.New()
                    .ApplyBuffPermanentFixed(buff, asChild: false, toCaster: true));
            return (icon != null ? abilityConfig.SetIcon(icon) : abilityConfig.SetIcon(IconPrefix + IconName))
                .Configure();
        }
    }
}
