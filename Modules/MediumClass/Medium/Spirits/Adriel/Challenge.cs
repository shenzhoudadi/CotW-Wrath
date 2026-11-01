using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using MediumClass.Medium.NewComponents;
using MediumClass.Utilities;
using MediumClass.Utils;
using System.Collections.Generic;
using UnityEngine;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Adriel
{
    /// <summary>
    /// 阿德里尔的次级英灵之力：挑战。
    ///
    /// 复刻原生骑将挑战链（能力→目标减益→自身增益），但等级计算使用
    /// 角色等级而非骑将职业等级；另附加：攻击克服挑战目标的所有伤害减免、
    /// 挑战期间所有豁免+2。
    /// </summary>
    class Challenge
    {
        private static readonly string FeatName = "MediumAdrielChallenge";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(Challenge));

        private static Sprite Icon =>
            BlueprintTool.Get<BlueprintAbility>("9d5d58ff40e39ff4681670463abe99c9").Icon;

        private static BlueprintUnitFactReference TargetFact =>
            BlueprintTool.GetRef<BlueprintUnitFactReference>(Guids.MediumAdrielChallengeBuffTarget);

        private static BlueprintBuffReference SelfBuff =>
            BlueprintTool.GetRef<BlueprintBuffReference>(Guids.MediumAdrielChallengeBuffSelf);

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Adriel Challenge");

            AbilityResourceConfigurator.New(FeatName + "Resource", Guids.MediumAdrielChallengeResource)
                .SetMaxAmount(new BlueprintAbilityResource.Amount
                {
                    BaseValue = 1,
                    IncreasedByLevel = false,
                    IncreasedByLevelStartPlusDivStep = true,
                    StartingLevel = 4,
                    StartingIncrease = 1,
                    LevelStep = 3,
                    PerStepIncrease = 1,
                    MinClassLevelIncrease = 0,
                    IncreasedByStat = false,
                    ResourceBonusStat = StatType.Charisma,
                    // 无职业除数列表时，OtherClassesModifier 把步进计数变成
                    // 总角色等级：每日次数 1/1/1/2/2/2/3…（等同按角色等级的骑将挑战）。
                    OtherClassesModifier = 1f,
                })
                .Configure();

            var buffSelf = BuffConfigurator.New(FeatName + "BuffSelf", Guids.MediumAdrielChallengeBuffSelf)
                .SetDisplayName("MediumAdrielChallengeBuffSelf.Name")
                .SetDescription("MediumAdrielChallenge.Description")
                .SetIcon(Icon)
                .SetFlags(BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.RemoveOnRest)
                .SetStacking(StackingType.Replace)
                .AddComponent<ACBonusAgainstFactOwner>(c =>
                {
                    c.m_CheckedFact = TargetFact;
                    c.Bonus = -2;
                    c.Descriptor = ModifierDescriptor.Penalty;
                    c.NoFact = true;
                })
                .AddComponent<DamageBonusAgainstFactOwner>(c =>
                {
                    c.m_CheckedFact = TargetFact;
                    c.DamageBonus = 0;
                    c.Bonus = ContextValues.Shared(AbilitySharedValue.Damage);
                    c.Descriptor = ModifierDescriptor.UntypedStackable;
                })
                .AddComponent<IgnoreDamageReductionOnAttack>(c =>
                {
                    c.CheckEnemyFact = true;
                    c.m_CheckedFact = TargetFact;
                })
                .AddStatBonus(descriptor: ModifierDescriptor.UntypedStackable, stat: StatType.SaveFortitude, value: 2)
                .AddStatBonus(descriptor: ModifierDescriptor.UntypedStackable, stat: StatType.SaveReflex, value: 2)
                .AddStatBonus(descriptor: ModifierDescriptor.UntypedStackable, stat: StatType.SaveWill, value: 2)
                .Configure();

            var buffTarget = BuffConfigurator.New(FeatName + "BuffTarget", Guids.MediumAdrielChallengeBuffTarget)
                .SetDisplayName("MediumAdrielChallengeBuffTarget.Name")
                .SetDescription("MediumAdrielChallengeBuffTarget.Description")
                .SetIcon(Icon)
                .SetStacking(StackingType.Replace)
                .AddUniqueBuff()
                .AddFactContextActions(
                    activated: ActionsBuilder.New()
                        .Add<ContextActionApplyBuff>(c =>
                        {
                            c.m_Buff = SelfBuff;
                            c.Permanent = true;
                            c.ToCaster = true;
                            c.AsChild = true;
                            c.SameDuration = true;
                        }),
                    deactivated: ActionsBuilder.New()
                        .Add<ContextActionRemoveBuff>(c =>
                        {
                            c.m_Buff = SelfBuff;
                            c.ToCaster = true;
                        }))
                .Configure();

            var ability = AbilityConfigurator.New(FeatName + "Ability", Guids.MediumAdrielChallengeAbility)
                .SetDisplayName("MediumAdrielChallengeAbility.Name")
                .SetDescription("MediumAdrielChallengeAbility.Description")
                .SetIcon(Icon)
                .SetType(AbilityType.Extraordinary)
                .SetRange(AbilityRange.Long)
                .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
                .SetCanTargetEnemies(true)
                .SetCanTargetSelf(false)
                .SetCanTargetFriends(false)
                .SetCanTargetPoint(false)
                .AddAbilityResourceLogic(requiredResource: BlueprintTool.GetRef<BlueprintAbilityResourceReference>(Guids.MediumAdrielChallengeResource), amount: 1, isSpendResource: true)
                .AddContextRankConfig(ContextRankConfigs.CharacterLevel())
                .AddComponent<ContextCalculateSharedValue>(c =>
                {
                    c.ValueType = AbilitySharedValue.Damage;
                    c.Value = new ContextDiceValue
                    {
                        DiceType = DiceType.Zero,
                        DiceCountValue = ContextValues.Constant(0),
                        BonusValue = ContextValues.Rank(),
                    };
                })
                .AddAbilityEffectRunAction(
                    actions: ActionsBuilder.New()
                        .Add<ContextActionApplyBuff>(c =>
                        {
                            c.m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(Guids.MediumAdrielChallengeBuffTarget);
                            c.Permanent = true;
                            c.AsChild = true;
                        }))
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.MediumAdrielChallenge)
                .SetDisplayName("MediumAdrielChallenge.Name")
                .SetDescription("MediumAdrielChallenge.Description")
                .SetIcon(Icon)
                .SetIsClassFeature(true)
                .SetHideInUI(false)
                .SetHideNotAvailibleInUI(false)
                .AddFacts(new() { ability })
                .AddComponent<AddAbilityResources>(c =>
                {
                    c.UseThisAsResource = false;
                    c.m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(Guids.MediumAdrielChallengeResource);
                    c.Amount = 0;
                    c.RestoreAmount = true;
                    c.RestoreOnLevelUp = false;
                })
                .Configure();
        }
    }
}
