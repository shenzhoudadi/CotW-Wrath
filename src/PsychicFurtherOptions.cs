using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // CotW's slot-sacrifice amplification uses native conversion lists, not a
    // linked-spell toggle: the original spell's effects are never executed.
    internal static class PsychicFurtherOptions
    {
        internal static BlueprintFeature Psychofeedback(PsychicAssets f, BlueprintCharacterClass character)
        {
            const string text="As a standard action, spend 2 phrenic points and sacrifice a Psychic spell slot of 2nd level or higher. Gain an enhancement bonus to Strength, Dexterity, or Constitution equal to the slot's level for 1 minute per psychic level. Choose the conversion under a known Psychic spell of that level; the original spell is not cast. Enhancement bonuses to the same attribute do not stack.";
            var stats=new[]{StatType.Strength,StatType.Dexterity,StatType.Constitution};
            var icons=new[]{"4c3d08935262b6544ae97599b3a9556d","de7a025d48ad5da4991e7d3c682cf69d","a900628aea19aa74aad0ece0e65d091a"};
            var grants=new List<BlueprintComponent>();
            var effects=new BlueprintBuff[3,10];
            for(int s=0;s<3;s++) for(int level=2;level<=9;level++) {
                var buff=f.Buff("Psychofeedback"+stats[s]+level+"Buff","Psychofeedback: +"+level+" "+stats[s],text,
                    new AddStatBonus {name="PsychofeedbackEnhancement",Stat=stats[s],Value=level,Descriptor=ModifierDescriptor.Enhancement});
                Set(buff,"m_Icon",f.Native<BlueprintAbility>(icons[s]).Icon);effects[s,level]=buff;
            }
            foreach(bool charisma in new[]{false,true}) {
                string poolName=charisma?"Charisma":"Wisdom";
                var conversions=new List<BlueprintComponent>();
                for(int s=0;s<3;s++) {
                    // Wrath's ReferenceArrayProxy dereferences every slot while
                    // registering a conversion list. Empty slots need reference
                    // objects with an empty GUID, rather than null references.
                    var spells=Enumerable.Range(0,10).Select(_=>new BlueprintAbilityReference()).ToArray();
                    for(int level=2;level<=9;level++) {
                        var rank=new ContextRankConfig {name="PsychofeedbackClassDuration"};
                        SetEnum(rank,"m_BaseValueType","ClassLevel");Set(rank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(character)});
                        var action=Apply(effects[s,level],0,DurationRate.Minutes,toCaster:true,fromSpell:false);action.IsNotDispelable=true;
                        action.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.Default};
                        var cost=new AbilityResourceLogic {name="PsychofeedbackPhrenicCost",Amount=2};
                        Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(charisma?PsychicClass.CharismaPoolId:PsychicClass.WisdomPoolId)));Set(cost,"m_IsSpendResource",true);
                        string token="Psychofeedback"+poolName+stats[s]+level;
                        var ability=f.Ability(token,"Psychofeedback: +"+level+" "+stats[s],text,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,rank,cost,Run(action));
                        ability.LocalizedDuration=Text(token,"duration","1 minute per psychic level");
                        Set(ability,"m_Icon",effects[s,level].Icon);spells[level]=Ref<BlueprintAbilityReference>(ability);
                    }
                    var conversion=new SpontaneousSpellConversion {name="Psychofeedback"+poolName+stats[s]};
                    Set(conversion,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(character));Set(conversion,"m_SpellsByLevel",spells);conversions.Add(conversion);
                }
                var feature=f.Feature("Psychofeedback"+poolName+"Conversions","Psychofeedback",text,conversions.ToArray());feature.HideInUI=true;
                Set(feature,"m_Icon",f.Native<BlueprintAbility>(icons[0]).Icon);
                var grant=new AddFeatureIfHasFact {name="PsychofeedbackFor"+poolName};
                Set(grant,"m_CheckedFact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>(charisma?PsychicClass.CharismaFeatureId:PsychicClass.WisdomFeatureId)));
                Set(grant,"m_Feature",Ref<BlueprintUnitFactReference>(feature));grants.Add(grant);
            }
            var result=f.Feature("PsychofeedbackFeature","Psychofeedback",text,grants.ToArray());
            Set(result,"m_Icon",f.Native<BlueprintAbility>(icons[0]).Icon);return result;
        }

        internal static BlueprintFeature[] Feats(PsychicAssets f, BlueprintCharacterClass character, BlueprintFeature[] choices)
        {
            PrerequisiteClassLevel ClassRequirement() { var c=new PrerequisiteClassLevel {name="RequiresPsychic",Level=1};Set(c,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(character));return c; }
            PrerequisiteFeaturesFromList PoolRequirement() {var c=new PrerequisiteFeaturesFromList {name="RequiresPhrenicPool",Amount=1};Set(c,"m_Features",new[]{Ref<BlueprintFeatureReference>(f.Native<BlueprintFeature>(PsychicClass.WisdomFeatureId)),Ref<BlueprintFeatureReference>(f.Native<BlueprintFeature>(PsychicClass.CharismaFeatureId))});return c;}
            IncreaseResourceAmount More(string id,string name) {var c=new IncreaseResourceAmount {name=name,Value=2};Set(c,"m_Resource",Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(id)));return c;}
            var pool=f.Feature("ExpandedPhrenicPoolFeat","Expanded Phrenic Pool","Your phrenic pool increases by 2 points. You can take this feat multiple times, up to 10 times; its effects stack.",
                ClassRequirement(),PoolRequirement(),More(PsychicClass.WisdomPoolId,"ExpandWisdomPool"),More(PsychicClass.CharismaPoolId,"ExpandCharismaPool"));
            pool.Ranks=10;pool.Groups=new[]{FeatureGroup.Feat};
            var extra=f.Add(new BlueprintFeatureSelection {Ranks=1,Groups=new[]{FeatureGroup.Feat},ComponentsArray=new BlueprintComponent[]{ClassRequirement(),PoolRequirement()}},"ExtraPhrenicAmplificationFeat","Extra Phrenic Amplification (Psychic)","Learn one additional phrenic amplification. You must meet the requirements of the selected amplification. You can take this feat once.");
            Set(extra,"m_Features",choices.Select(Ref<BlueprintFeatureReference>).ToArray());Set(extra,"m_AllFeatures",choices.Select(Ref<BlueprintFeatureReference>).ToArray());
            return new BlueprintFeature[]{pool,extra};
        }
    }
}
