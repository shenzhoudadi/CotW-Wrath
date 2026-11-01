using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MediumClass.Medium.NewActions;
using MediumClass.Medium.NewComponents;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;

namespace MediumClass.Medium.Spirits.RobinHood
{
    internal static class RobinHood
    {
        private static UnityEngine.Sprite Icon => AbilityRefs.SenseVitals.Reference.Get().Icon;
        internal static void ConfigureEnabled()
        {
            CharacterClassConfigurator.New("RobinHood",Guids.RobinHood).Configure();
            FeatureConfigurator.New("RobinHoodLesser",Guids.RobinHoodLesser)
                .SetDisplayName("RobinHoodLesser.Name").SetDescription("RobinHoodLesser.Description").SetIcon(Icon).SetIsClassFeature()
                .AddComponent<AddProficiencies>(c=>{
                    c.WeaponProficiencies=new[]{WeaponCategory.Longbow,WeaponCategory.Shortbow};
                    c.ArmorProficiencies=System.Array.Empty<Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup>();
                })
                .AddComponent<RobinHoodLesserComponent>().Configure();
            FeatureConfigurator.New("RobinHoodGreater",Guids.RobinHoodGreater)
                .SetDisplayName("RobinHoodGreater.Name").SetDescription("RobinHoodGreater.Description").SetIcon(Icon).SetIsClassFeature()
                .AddComponent<RobinHoodDistantArcher>().Configure();
            var hidden=BuffConfigurator.New("RobinHoodUnseenBuff",Guids.RobinHoodUnseenBuff)
                .CopyFrom(BuffRefs.InvisibilityGreaterBuff,c=>true)
                .SetDisplayName("RobinHoodSupreme.Name").SetDescription("RobinHoodSupreme.Description")
                .SetStacking(StackingType.Replace).AddComponent<RobinHoodUnseenAttacker>().Configure();
            var duration=new ContextDurationValue { Rate=DurationRate.Rounds,DiceType=DiceType.Zero,DiceCountValue=0,
                BonusValue=ContextValues.Rank(AbilityRankType.Default) };
            var ability=AbilityConfigurator.New("RobinHoodUnseenAbility",Guids.RobinHoodUnseenAbility)
                .SetDisplayName("RobinHoodSupreme.Name").SetDescription("RobinHoodSupreme.Description").SetIcon(hidden.Icon)
                .SetType(AbilityType.Supernatural).SetRange(AbilityRange.Personal).SetCanTargetSelf(true)
                .SetActionType(UnitCommand.CommandType.Swift)
                .SetLocalizedDuration("RobinHoodSupreme.Duration").SetLocalizedSavingThrow("RobinHood.NoSave")
                .AddComponent<AbilityRequirementSpecificSpirit>(c=>c.Spirit=BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.RobinHood))
                .AddAbilityResourceLogic(requiredResource:BlueprintTool.GetRef<BlueprintAbilityResourceReference>(Guids.MediumInfluenceResource),amount:1,isSpendResource:true)
                .AddContextRankConfig(ContextRankConfigs.ClassLevel(new[]{Guids.Medium}))
                .AddAbilityEffectRunAction(actions:ActionsBuilder.New().Add<ContextActionApplyBuff>(c=>{
                    c.m_Buff=hidden.ToReference<BlueprintBuffReference>();c.ToCaster=true;c.IsNotDispelable=true;c.DurationValue=duration;
                })).Configure();
            FeatureConfigurator.New("RobinHoodSupreme",Guids.RobinHoodSupreme)
                .SetDisplayName("RobinHoodSupreme.Name").SetDescription("RobinHoodSupreme.Description").SetIcon(hidden.Icon).SetIsClassFeature()
                .AddFacts(new(){ability}).AddFactContextActions(deactivated:ActionsBuilder.New().Add<ContextActionRemoveBuff>(c=>c.m_Buff=hidden.ToReference<BlueprintBuffReference>())).Configure();
            var progression=ProgressionConfigurator.New("RobinHoodProgression",Guids.RobinHoodProgression)
                .AddToClasses(Guids.RobinHood).SetLevelEntries(LevelEntryBuilder.New()
                    .AddEntry(1,Guids.RobinHoodLesser).AddEntry(6,Guids.TricksterSurpriseStrike,Guids.TricksterSurpriseStrike)
                    .AddEntry(9,Guids.TricksterSurpriseStrike).AddEntry(11,Guids.RobinHoodGreater)
                    .AddEntry(12,Guids.TricksterSurpriseStrike).AddEntry(15,Guids.TricksterSurpriseStrike)
                    .AddEntry(17,Guids.RobinHoodSupreme).AddEntry(18,Guids.TricksterSurpriseStrike)).Configure();
            CharacterClassConfigurator.For(Guids.RobinHood)
                .SetLocalizedName("RobinHood.Name").SetLocalizedDescription("RobinHood.Description")
                .SetSkillPoints(0).SetHitDie(DiceType.D8).SetPrestigeClass(false).SetIsMythic(false)
                .SetBaseAttackBonus(StatProgressionRefs.BABMedium.Reference.Get()).SetFortitudeSave(StatProgressionRefs.SavesLow.Reference.Get())
                .SetReflexSave(StatProgressionRefs.SavesLow.Reference.Get()).SetWillSave(StatProgressionRefs.SavesHigh.Reference.Get())
                .SetProgression(progression).Configure();
        }
        internal static BlueprintAbility ChannelAbility() => AbilityConfigurator.New("MediumChannelSpiritAbilityRobinHood",Guids.MediumChannelSpiritAbilityRobinHood)
            .SetDisplayName("RobinHood.Name").SetDescription("RobinHood.Description").SetIcon(Icon)
            .AddComponent<AbilityRequirementHasSpirit>(c=>c.Not=true).AddComponent<RobinHoodAlignmentRequirement>()
            .SetActionType(UnitCommand.CommandType.Free)
            .AddAbilityEffectRunAction(actions:ActionsBuilder.New()
                .Add<ContextActionApplySpirit>(c=>c.Spirit=BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.RobinHood))
                .Add<ContextActionSpiritInfluence>())
            .AddAbilityResourceLogic(requiredResource:BlueprintTool.GetRef<BlueprintAbilityResourceReference>(Guids.MediumInfluenceResource),amount:1,isSpendResource:true).Configure();
        internal static void ConfigureEntry(MediumSpiritComponent c)
        {
            c.SpiritClass=BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.RobinHood);
            c.SpiritInfluencePenalty=BlueprintTool.GetRef<BlueprintBuffReference>(Guids.MediumInfluenceDebuff);
            c.SpiritInfluence=BlueprintTool.GetRef<BlueprintAbilityResourceReference>(Guids.MediumInfluenceResourceTrickster);
            c.SpiritBonusFeature=BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.MediumSpiritBonus);
            c.SpiritSeanceBoon=BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.TricksterSeanceBoon);
            c.SpiritLesserPower=BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.RobinHoodLesser);
            c.SpiritIntermediatePower=BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.TricksterSurpriseStrike);
            c.SpiritGreaterPower=BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.RobinHoodGreater);
            c.SpiritSupremePower=BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.RobinHoodSupreme);
            c.Stats=RobinHoodRules.AttunementStats;
            c.Penalties=new[]{StatType.AC,StatType.AdditionalCMB,StatType.AdditionalCMD,StatType.SaveWill,StatType.SaveFortitude};
        }
    }
}
