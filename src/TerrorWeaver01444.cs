// Adapted from Holic75/KingmakerRebalance (MIT), Psychic.createTerrorWeaver.
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01444;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class TerrorWeaver01444
    {
        internal const string ModuleName="Psychic archetype: Terror Weaver",RootToken="TerrorWeaverArchetype01444";
        internal const string CommandId="feb70aab86cc17f4bb64432c83737ac2",BaneId="8bc64d869456b004b9db255cdd1ea734",FearId="f08a7239aa961f34c8301518e71d4cdf",IconId="e788b02f8d21014488067bdd3ba7b325";
        internal static readonly string[] OwnTokens=new[]{RootToken,"TerrorManipulation01444","TerrorAuraKnown01444","TerrorAuraMove01444","TerrorAuraSwift01444"}.Concat(new[]{"Move","Swift"}.SelectMany(a=>new[]{"Wisdom","Charisma"}.Select(p=>"Terror"+a+p+"Ability01444"))).ToArray();
        const string Description="A number of exceptional tribesmen have developed potent psychic abilities, unlocked by generations of exposure to a specific combination of mind-altering fungi and radiation. Powerful manipulators, they often rule smaller tribes from the shadows, but with only few able to match their mental acuity, their only limit is their own ambition.";
        const string AuraText="At 11th level, a terror weaver can spend 1 phrenic pool point, as a move action, to make every enemy affected by his aura of doom spell frightened for one round.\nAt 19th level, he can do it a swift action instead.";
        static AddKnownSpell Known(BlueprintCharacterClass cls,BlueprintAbility spell,int level) {var c=new AddKnownSpell{SpellLevel=level};Set(c,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));Set(c,"m_Spell",Ref<BlueprintAbilityReference>(spell));return c;}
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(PsychicClass.ClassId);
            if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);
            var amplification=(BlueprintFeature)cache.Load(Id("PhrenicAmplificationSelection"));var doom=(BlueprintAbility)cache.Load(Id("AuraOfDoom"));var shaken=(BlueprintBuff)cache.Load(Id("AuraOfDoomShakenBuff"));
            if(amplification==null||doom==null||shaken==null||new[]{11,19}.Any(n=>cls.Progression.LevelEntries.Count(e=>e.Level==n&&e.Features.Any(x=>x?.AssetGuid==amplification.AssetGuid))!=1))throw new InvalidOperationException("Terror Weaver requires the existing Psychic amplification and Aura of Doom graphs.");
            var command=f.Native<BlueprintAbility>(CommandId);var icon=f.Native<BlueprintAbility>(IconId);var bane=f.Native<BlueprintAbility>(BaneId);var fear=f.Native<BlueprintBuff>(FearId);
            var manipulation=f.Feature(OwnTokens[1],"Manipulation","At 2nd level, a terror weaver adds command spell to his list of known spells.",Known(cls,command,1));Set(manipulation,"m_Icon",Get(command,"m_Icon"));
            var known=f.Feature(OwnTokens[2],"Aura of Intimidation","At 9th level, a terror weaver adds aura of doom spell to his list of known spells.",Known(cls,doom,4));Set(known,"m_Icon",Get(doom,"m_Icon"));
            var grants=new BlueprintFeature[2];
            for(int action=0;action<2;action++) {
                var parts=new BlueprintComponent[2];
                for(int pool=0;pool<2;pool++) {
                    var match=new ContextConditionHasBuffFromCaster();Set(match,"m_Buff",Ref<BlueprintBuffReference>(shaken));
                    var enemy=new ContextConditionIsEnemy();var apply=Apply(fear,1,fromSpell:false);apply.IsNotDispelable=true;
                    var conditional=new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{enemy,match}},IfTrue=Actions(apply),IfFalse=Actions()};
                    string token="Terror"+(action==0?"Move":"Swift")+(pool==0?"Wisdom":"Charisma")+"Ability01444";
                    var resource=f.Native<BlueprintAbilityResource>(pool==0?PsychicClass.WisdomPoolId:PsychicClass.CharismaPoolId);
                    var cost=new AbilityResourceLogic{Amount=1};Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));Set(cost,"m_IsSpendResource",true);
                    Text(token,"savingThrow","None");
                    var ability=f.Ability(token,"Aura of Intimidation"+(action==0?"":" (Swift Action)"),AuraText,AbilityType.Supernatural,action==0?UnitCommand.CommandType.Move:UnitCommand.CommandType.Swift,AbilityRange.Personal,
                        (AbilityTargetsAround)CopyData(bane.GetComponent<AbilityTargetsAround>()),Run(conditional),cost);
                    ability.ComponentsArray=ability.ComponentsArray.Concat(bane.GetComponents<AbilitySpawnFx>().Select(v=>(BlueprintComponent)CopyData(v))).ToArray();
                    ability.CanTargetSelf=true;ability.CanTargetFriends=false;ability.CanTargetEnemies=false;ability.SpellResistance=false;Set(ability,"m_Icon",Get(icon,"m_Icon"));
                    ability.LocalizedDuration=Text(token,"duration","1 round");ability.LocalizedSavingThrow=Text(token,"savingThrow","None");
                    // The current port has separate native Wisdom/Charisma
                    // pool facts. Grant only the ability backed by the owner's
                    // chosen pool, using the actual native conditional grant.
                    var grant=new AddFeatureIfHasFact();Set(grant,"m_CheckedFact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>(pool==0?PsychicClass.WisdomFeatureId:PsychicClass.CharismaFeatureId)));Set(grant,"m_Feature",Ref<BlueprintUnitFactReference>(ability));parts[pool]=grant;
                }
                grants[action]=f.Feature(OwnTokens[action+3],action==0?"Aura of Intimidation":"Aura of Intimidation (Swift Action)",AuraText,parts);Set(grants[action],"m_Icon",Get(icon,"m_Icon"));
            }
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Terror Weaver"),LocalizedDescription=Text(RootToken,"description",Description),AddFeatures=new[]{Entry(2,manipulation),Entry(9,known),Entry(11,grants[0]),Entry(19,grants[1])},RemoveFeatures=new[]{Entry(11,amplification),Entry(19,amplification)},ComponentsArray=Array.Empty<BlueprintComponent>()},RootToken,"Terror Weaver",Description);Set(root,"m_ParentClass",cls);
            Publish(cache,f,OwnTokens,cls,root,new BlueprintFeatureBase[]{manipulation,known,grants[0],grants[1]});
        }
    }
}
