using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicDisciplines
    {
        internal static readonly string[] FerocitySpells = {
            "2c38da66e5a599347ac95b3294acbe00", "4c3d08935262b6544ae97599b3a9556d", "5ab0d42fb68c9e34abae4921822b9d63",
            "0087fc2d64b6095478bc7b8d7d512caf", "c66e86905f7606c4eaa5c774f0357b2b", "27203d62eb3d4184c9aced94f22e1806",
            "df2a0ba6b6dcecf429cbb80a56fee5cf", "e788b02f8d21014488067bdd3ba7b325", "1f01a098d737ec6419aedc4e7ad61fdd" };
        internal static BlueprintFeatureSelection Build(PsychicAssets f, BlueprintCharacterClass character)
        {
            const string sensesText = "Spend 1 phrenic point as a standard action to gain blindsense within 30 feet for 1 minute. At psychic level 11, blindsense is permanent and this action instead grants blindsight within 30 feet for 1 minute.";
            var senses1 = f.Buff("FerocityBlindsenseBuff", "Enhanced Senses: Blindsense", sensesText, new Blindsense { name="FerocityBlindsense", Range=new Feet(30) });
            var senses2 = f.Buff("FerocityBlindsightBuff", "Enhanced Senses: Blindsight", sensesText, new Blindsense { name="FerocityBlindsight", Range=new Feet(30), Blindsight=true });
            BlueprintFeature Sense(string token, BlueprintBuff buff, bool upgraded) {
                var cost = new AbilityResourceLogic { name="EnhancedSensesPhrenicCost", Amount=1 };
                Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(PsychicClass.WisdomPoolId))); Set(cost,"m_IsSpendResource",true);
                var apply = Apply(buff,1,DurationRate.Minutes,toCaster:true,fromSpell:false); apply.IsNotDispelable=true;
                var ability = f.Ability(token+"Ability",upgraded?"Enhanced Senses: Blindsight":"Enhanced Senses: Blindsense",sensesText,AbilityType.Extraordinary,UnitCommand.CommandType.Standard,AbilityRange.Personal,Run(apply),cost);
                return upgraded ? f.Feature(token,"Enhanced Senses",sensesText,Facts(ability),new Blindsense { name="PermanentFerocityBlindsense", Range=new Feet(30) }) : f.Feature(token,"Enhanced Senses",sensesText,Facts(ability));
            }
            var low = Sense("FerocitySensesLow",senses1,false); var high = Sense("FerocitySensesHigh",senses2,true);
            AddFeatureOnClassLevel AtLevel(BlueprintFeature feature, int level, bool before=false) {
                var add = new AddFeatureOnClassLevel { name="FerocityGrant"+feature.name, Level=level, BeforeThisLevel=before };
                Set(add,"m_Class",Ref<BlueprintCharacterClassReference>(character)); Set(add,"m_Feature",Ref<BlueprintFeatureReference>(feature));
                Set(add,"m_AdditionalClasses",Array.Empty<BlueprintCharacterClassReference>());Set(add,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>()); return add;
            }
            var senses=f.Feature("FerocitySenses","Enhanced Senses",sensesText,AtLevel(low,11,true),AtLevel(high,11));
            const string ferocityText="At psychic level 5, gain Diehard. While below 25% of your maximum hit points, gain +4 morale bonuses to Strength and Dexterity and a +2 morale bonus on Fortitude saves.";
            var wounded=f.Buff("FerocityWoundedBuff","Ferocity",ferocityText,
                new AddStatBonus { name="FerocityStrength", Stat=StatType.Strength,Value=4,Descriptor=ModifierDescriptor.Morale },
                new AddStatBonus { name="FerocityDexterity", Stat=StatType.Dexterity,Value=4,Descriptor=ModifierDescriptor.Morale },
                new AddStatBonus { name="FerocityFortitude", Stat=StatType.SaveFortitude,Value=2,Descriptor=ModifierDescriptor.Morale });
            var health=new BuffOnHealthTickingTrigger { name="FerocityBelowQuarterHP", HealthPercent=0.25f }; Set(health,"m_TriggeredBuff",Ref<BlueprintBuffReference>(wounded));
            var ferocity=f.Feature("FerocityWounded","Ferocity",ferocityText,Facts(f.Native<BlueprintFeature>("86669ce8759f9d7478565db69b8c19ad")),health);
            const string furyText="At psychic level 13, enter Primal Fury as a free action, gaining the native transformation effect. Use a total number of rounds per day equal to your psychic level. End the effect as a free action. Ending it causes fatigue for 1 minute. You cannot activate it while fatigued or exhausted, and it ends if you become unconscious or disabled.";
            var furyBuff=f.Clone(f.Native<BlueprintBuff>("287682389d2011b41b5a65195d9cbc84"),"FerocityPrimalFuryBuff","Primal Fury",furyText);
            var fatigue=Apply(f.Native<BlueprintBuff>("e6f2fc5d73d88064583cb828801212f4"),1,DurationRate.Minutes,toCaster:true,fromSpell:false); fatigue.IsNotDispelable=true;
            furyBuff.ComponentsArray=furyBuff.ComponentsArray.Concat(new BlueprintComponent[] { new AddFactContextActions { name="PrimalFuryAftereffect", Deactivated=Actions(fatigue) } }).ToArray();
            var rounds=f.Add(new BlueprintAbilityResource(),"FerocityPrimalFuryRounds","Primal Fury Rounds",furyText);
            var amount=Get(rounds,"m_MaxAmount"); Set(amount,"IncreasedByLevel",true); Set(amount,"LevelIncrease",1); Set(amount,"m_Class",new[] { Ref<BlueprintCharacterClassReference>(character) }); Set(amount,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>()); Set(amount,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>()); Set(amount,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>()); Set(rounds,"m_MaxAmount",amount);
            var spend=new ActivatableAbilityResourceLogic { name="PrimalFuryRoundCost", SpendType=ActivatableAbilityResourceLogic.ResourceSpendType.NewRound }; Set(spend,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(rounds));
            var toggle=f.Add(new BlueprintActivatableAbility { ActivationType=AbilityActivationType.WithUnitCommand, DeactivateIfOwnerDisabled=true, DeactivateIfOwnerUnconscious=true,
                ComponentsArray=new BlueprintComponent[] { spend,new RestrictionHasUnitCondition { name="NoFatiguedPrimalFury", Condition=UnitCondition.Fatigued,Invert=true },new RestrictionHasUnitCondition { name="NoExhaustedPrimalFury", Condition=UnitCondition.Exhausted,Invert=true } } },"FerocityPrimalFuryToggle","Primal Fury",furyText);
            Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(furyBuff)); Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);
            var restore=new AddAbilityResources { name="PrimalFuryDailyRounds", RestoreAmount=true, RestoreOnLevelUp=true }; Set(restore,"m_Resource",Ref<BlueprintAbilityResourceReference>(rounds));
            var fury=f.Feature("FerocityPrimalFury","Primal Fury",furyText,Facts(toggle),restore);
            var bonusLevels=new[] { 1,4,6,8,10,12,14,16,18 };
            var bonusNames=new[] { "True Strike", "Bull's Strength", "Heroism", "Freedom of Movement", "Stoneskin", "Transformation", "Mind Blank", "Frightful Aspect", "Foresight" };
            var bonuses=FerocitySpells.Select((id,i) => {
                var add=new AddKnownSpell { name="FerocityBonusSpell"+(i+1),SpellLevel=i+1 }; Set(add,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(character)); Set(add,"m_Spell",Ref<BlueprintAbilityReference>(f.Native<BlueprintAbility>(id)));
                return f.Feature("FerocityBonusSpell"+(i+1),"Discipline Spell: "+bonusNames[i],"Learn "+bonusNames[i]+" in addition to the normal Psychic spells known.",add);
            }).ToArray();
            var discipline=f.Add(new BlueprintProgression { Ranks=1,IsClassFeature=true,LevelEntries=bonusLevels.Select((level,i) => {
                var entry=new LevelEntry { Level=level }; entry.SetFeatures(i==0 ? new BlueprintFeatureBase[] { bonuses[i],senses,f.Native<BlueprintFeature>(PsychicClass.WisdomFeatureId) } : new BlueprintFeatureBase[] { bonuses[i] }); return entry;
            }).Concat(new[] { Entry(5,ferocity),Entry(13,fury) }).OrderBy(e=>e.Level).ToArray(),UIGroups=Array.Empty<UIGroup>() },"FerocityDiscipline","Ferocity","Channel your primal subconscious through Wisdom. Gain Enhanced Senses, Ferocity, Primal Fury and nine bonus discipline spells. This discipline's phrenic pool is half your psychic level plus your Wisdom modifier.");
            Set(discipline,"m_Classes",new[] { new BlueprintProgression.ClassWithLevel { m_Class=Ref<BlueprintCharacterClassReference>(character) } });
            var faith=PsychicFaith.Build(f,character);
            var pageantry=PsychicPageantry.Build(f,character);
            var selection=f.Add(new BlueprintFeatureSelection { Ranks=1,IsClassFeature=true },"DisciplineSelection","Psychic Discipline (Prototype)","Choose a psychic discipline. This experimental build includes Ferocity, Faith and Pageantry; three other CotW disciplines are still being ported. Nine basic phrenic amplifications are available in a separate class selection.");
            Set(selection,"m_AllFeatures",new[] { Ref<BlueprintFeatureReference>(discipline),Ref<BlueprintFeatureReference>(faith),Ref<BlueprintFeatureReference>(pageantry) }); Set(selection,"m_Features",new[] { Ref<BlueprintFeatureReference>(discipline),Ref<BlueprintFeatureReference>(faith),Ref<BlueprintFeatureReference>(pageantry) }); return selection;
        }
        private static LevelEntry Entry(int level, BlueprintFeatureBase feature) { var entry=new LevelEntry { Level=level }; entry.SetFeatures(new[] { feature }); return entry; }
    }
}
