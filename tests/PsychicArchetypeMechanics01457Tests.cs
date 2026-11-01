using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static bool Eligible057(BlueprintFeature feat,UnitEntityData u)=>feat.GetComponents<Prerequisite>().All(p=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{null,u.Descriptor,null}));
    static void MutationMechanics057(BlueprintsCache c,BlueprintArchetype root,Harmony h)
    {
        var physical=A<BlueprintFeature>(c,"PhysicalMutationFeature01457");var bp=A<BlueprintAbility>(c,"PhysicalMutationAbility01457");var resource=A<BlueprintAbilityResource>(c,"PhysicalMutationResource01457");
        foreach(int level in new[]{1,3,7,11,12,15,16,20}){
            var u=EquipmentUnit(c,level,"83fa3b50138c566698801ba5967bbde0");u.Progression.Classes[0].AddArchetype(root);u.Stats.Strength.BaseValue=12;u.Stats.Intelligence.BaseValue=20;
            var feat=Grant(u,physical);Check(resource.GetMaxAmount(u.Descriptor)==level&&u.Resources.GetResourceAmount(resource)==level,"native mutation minutes equal Psychic class level "+level);
            var ability=new AbilityData(bp,u.Descriptor);Check(bp.GetComponents<IAbilityCasterRestriction>().All(r=>r.IsCasterRestrictionPassed(u)),"native mutation caster restrictions permit eligible owner "+level);
            StartMutation057Or058(u,ability);var main=u.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));
            Check(u.Resources.GetResourceAmount(resource)==level-1,"actual native ability payment consumes one mutation minute "+level);
            Check(u.Stats.Strength.ModifiedValue==12+(level<12?4:level<16?6:8)&&u.Stats.Intelligence.ModifiedValue==18,"actual native alchemical Strength and Intelligence penalty scale correctly "+level);
            if(MutationPolicy058)RepeatMutation058(u,ability,main);else Check(!bp.GetComponents<IAbilityCasterRestriction>().All(r=>r.IsCasterRestrictionPassed(u)),"existing physical mutation blocks repeat activation "+level);
            main.Remove();Check(u.Stats.Intelligence.ModifiedValue==20,"native parent removal clears the Intelligence penalty before fatigue cleanup "+level);
            var fatigue=u.Buffs.Enumerable.SingleOrDefault(b=>b.Blueprint.AssetGuid==BlueprintGuid.Parse("e6f2fc5d73d88064583cb828801212f4"));Check(fatigue!=null&&!bp.GetComponents<IAbilityCasterRestriction>().All(r=>r.IsCasterRestrictionPassed(u)),"actual removal applies fatigue and blocks immediate reuse "+level);fatigue.Remove();
            Check(bp.GetComponents<IAbilityCasterRestriction>().All(r=>r.IsCasterRestrictionPassed(u))&&u.Stats.Strength.ModifiedValue==12,"actual fatigue removal restores Strength and permits later activation "+level);
            u.Facts.Remove(feat);
        }
        var owner=EquipmentUnit(c,11,"83fa3b50138c566698801ba5967bbde0");owner.Progression.Classes[0].AddArchetype(root);
        var basic=A<BlueprintFeature>(c,"BodilyMutationsFeature01457");var improved=A<BlueprintFeature>(c,"ImprovedBodilyMutationsFeature01457");
        string[] names=(string[])AccessTools.Field(T("MutationMind01457"),"Mutations").GetValue(null);
        foreach(string key in names)Check(!Eligible057(A<BlueprintFeature>(c,key+"BodilyMutationFeature01457"),owner),"without bodily marker mutation choice is rejected: "+key);
        Grant(owner,basic);
        foreach(string key in names)Check(Eligible057(A<BlueprintFeature>(c,key+"BodilyMutationFeature01457"),owner)==(Array.IndexOf(names,key)<7||(MutationPolicy058&&key=="ElongatedFingers")),"native basic versus improved mutation prerequisite: "+key);
        Grant(owner,improved);
        foreach(string key in names)Check(Eligible057(A<BlueprintFeature>(c,key+"BodilyMutationFeature01457"),owner),"native improved marker permits mutation option: "+key);
        var unrelated=EquipmentUnit(c,20);foreach(string key in names)Check(!Eligible057(A<BlueprintFeature>(c,key+"BodilyMutationFeature01457"),unrelated),"unrelated class cannot select bodily mutation: "+key);
        Grant(owner,physical);Grant(owner,c,"ElongatedLegsBodilyMutationFeature01457");Grant(owner,c,"SlimyBodilyMutationFeature01457");
        int speed=owner.Stats.Speed.ModifiedValue,cmd=owner.Stats.AdditionalCMD.ModifiedValue;
        Check(!owner.Buffs.Enumerable.Any(b=>b.Blueprint.name.Contains("BodilyMutationBuff01457")),"owning bodily mutations alone grants no active effect");
        StartMutation057Or058(owner,new AbilityData(bp,owner.Descriptor));var host=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));
        Check(owner.Stats.Speed.ModifiedValue==speed+10&&owner.Stats.AdditionalCMD.ModifiedValue==cmd+4,"actual native activation applies all selected mutation children");
        Check(owner.Buffs.Enumerable.Count(b=>b.Blueprint.name.Contains("BodilyMutationBuff01457"))==2,"only selected bodily mutations activate");
        host.Remove();Check(owner.Stats.Speed.ModifiedValue==speed&&owner.Stats.AdditionalCMD.ModifiedValue==cmd&&!owner.Buffs.Enumerable.Any(b=>b.Blueprint.name.Contains("BodilyMutationBuff01457")),"actual native parent removal clears child buffs and their modifiers");
        MutationBodies057(c,root);
    }
    static void MarauderMechanics057(BlueprintsCache c,BlueprintArchetype root,Harmony h)
    {
        var skewed=A<BlueprintFeature>(c,"MarauderSkewed01457");
        foreach(var pair in new[]{new[]{20,12},new[]{10,20},new[]{20,8},new[]{8,6}}){var u=EquipmentUnit(c,9,"83fa3b50138c566698801ba5967bbde0");u.Stats.Wisdom.BaseValue=pair[0];u.Stats.Charisma.BaseValue=pair[1];int before=u.Stats.SaveWill.ModifiedValue;var fact=Grant(u,skewed);
            Check(u.Stats.SaveWill.ModifiedValue==before+u.Stats.Charisma.Bonus-u.Stats.Wisdom.Bonus,"native Will replacement uses Charisma even when lower: "+pair[0]+"/"+pair[1]);
            u.Stats.Charisma.BaseValue=22;Check(u.Stats.SaveWill.ModifiedValue==before+u.Stats.Charisma.Bonus-(pair[0]-10)/2,"native Will replacement recalculates when Charisma changes");
            u.Stats.Wisdom.BaseValue=16;Check(u.Stats.SaveWill.ModifiedValue==before+u.Stats.Charisma.Bonus-(pair[0]-10)/2,"native Will replacement remains independent of changed Wisdom");
            u.Facts.Remove(fact);Check(u.Stats.SaveWill.ModifiedValue==before+u.Stats.Wisdom.Bonus-(pair[0]-10)/2,"native Will replacement removal restores current original attribute");}
        foreach(int level in new[]{3,5,6,8,9,11,12,15,18,19,20})foreach(bool charisma in new[]{false,true}){
            var u=EquipmentUnit(c,level,"83fa3b50138c566698801ba5967bbde0");u.Progression.Classes[0].AddArchetype(root);
            string poolFact=(string)AccessTools.Field(T("PsychicClass"),charisma?"CharismaFeatureId":"WisdomFeatureId").GetRawConstantValue();Grant(u,N<BlueprintFeature>(c,poolFact));
            Grant(u,c,"MarauderAuraI01457");if(level>=11)Grant(u,c,"MarauderAuraII01457");if(level>=19)Grant(u,c,"MarauderAuraIII01457");
            var granted=u.Abilities.Enumerable.Select(a=>a.Blueprint).Where(b=>b.name.Contains("Marauder")&&b.name.Contains("01457")).ToArray();int tier=Math.Min(5,level/3-1);
            Check(granted.Length==(level>=19?3:level>=11?2:1),"native class-window and pool facts grant exactly available aura tiers at "+level+" charisma="+charisma);
            Check(granted.All(b=>b.name.Contains(tier+(charisma?"Charisma":"Wisdom"))),"native aura grant exposes only current radius and actual phrenic pool at "+level+" charisma="+charisma);
        }
        MarauderEffects057(c,root,h);
    }
}
