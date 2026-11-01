using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;
using static CallOfTheWild.Wrath.Prototype.PsychicRemainingDisciplines01458;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicCompletionAmplifications01458
    {
        static readonly string[] NativeMetamagics={"46fad72f54a33dc4692d3b62eca7bb78","7f2b282626862e345935bbea5e66424b","85f3340093d144dd944fff9a9adfd2f2","a1de1e4f92195b442adb946f0e2b9d4e","cd26b9fa3f734461a0fcedc81cafaaac","ef7ece7bb5bb66a41b256976b27f424e","f180e72e4a9cbaa4da8be9bc958132ef","fbf5d9ce931f47f3a0c818b3f8ef8414"};
        internal static BlueprintActivatableAbility Toggle(PsychicAssets f,string token,BlueprintBuff buff,bool amplification=true)
        {
            var t=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,ComponentsArray=Array.Empty<BlueprintComponent>()},token+"Toggle",buff.Name,buff.Description);
            Set(t,"m_Buff",Ref<BlueprintBuffReference>(buff));Set(t,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);Icon(t,buff);return t;
        }
        static PrerequisiteFeature Major(BlueprintCharacterClass cls){var p=new PrerequisiteFeature();var r=new BlueprintFeatureReference();Set(r,"deserializedGuid",BlueprintGuid.Parse(PsychicClass.MajorId));Set(p,"m_Feature",r);return p;}
        static PrerequisiteFeature Need(BlueprintFeature feat){var r=new PrerequisiteFeature();Set(r,"m_Feature",Ref<BlueprintFeatureReference>(feat));return r;}
        static PrerequisiteNoFeature No(BlueprintFeature feat){var r=new PrerequisiteNoFeature();Set(r,"m_Feature",Ref<BlueprintFeatureReference>(feat));return r;}
        internal static BlueprintFeature[] Build(PsychicAssets f,BlueprintCharacterClass cls,List<BlueprintActivatableAbility> modes)
        {
            WillOfTheDead01458.Initialize(f.Cache);
            var result=new List<BlueprintFeature>();
            BlueprintActivatableAbility Mode(string token,string title,string text,PsychicAmplificationKind kind,int price,int strength,int minimum,string icon){
                var c=new PsychicAmplificationComponent{Kind=kind,Cost=price,Strength=strength,MinimumLevel=minimum,CharacterClass=Ref<BlueprintCharacterClassReference>(cls),WisdomPool=Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(PsychicClass.WisdomPoolId)),CharismaPool=Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(PsychicClass.CharismaPoolId))};
                var b=f.Buff(token+"Mode01458",title,text,c);Icon(b,f.Native<BlueprintUnitFact>(icon));var t=Toggle(f,token+"01458",b);modes.Add(t);return t;
            }
            var dead=Mode("WillOfTheDead",N("WillOfTheDeadBuff"),D("WillOfTheDeadBuff"),PsychicAmplificationKind.WillOfTheDead,2,0,1,"1a5e7191279e7cd479b17a6ca438498c");
            var deadFeature=f.Feature("WillOfTheDeadFeature01458",dead.Name,dead.Description,Facts(dead));Icon(deadFeature,dead);result.Add(deadFeature);
            var surgeModes=Enumerable.Range(1,5).Select(i=>Mode("UndercastSurge"+i,"Undercast Surge "+i,D("UndercastSurge1Buff"),PsychicAmplificationKind.UndercastSurge,2*i,i,1,"c3a8f31778c3980498d8f00c980be5f5")).ToArray();
            var surge=f.Feature("UndercastSurgeFeature01458","Undercast Surge",D("UndercastSurge1Buff"),Facts(surgeModes));Icon(surge,surgeModes[0]);result.Add(surge);
            var marker=f.Buff("SynapticShockTarget01458",N("SynapticShockTargetBuff"),D("SynapticShockTargetBuff"));marker.Stacking=StackingType.Stack;Icon(marker,f.Native<BlueprintAbility>("cf6c901fb7acc904e85c63b342e9c949"));
            var confusion=f.Clone(f.Native<BlueprintBuff>("886c7407dc629dc499b9f1465ff382df"),"SynapticShockConfusion01458","Synaptic Shock: Confusion",marker.Description);confusion.Stacking=StackingType.Stack;
            var pending=f.Buff("SynapticShockPending01460","Synaptic Shock: Pending Confusion",marker.Description,new SynapticDelay01460());pending.Stacking=StackingType.Stack;Icon(pending,marker);
            var choose=f.Ability("SynapticShockTargetAbility01458",marker.Name,marker.Description,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Long,Run(new SelectSynapticTarget01458()));choose.CanTargetSelf=choose.CanTargetFriends=false;Icon(choose,marker);
            var synaptic=Mode("SynapticShock","Synaptic Shock",marker.Description,PsychicAmplificationKind.SynapticShock,1,0,11,"cf6c901fb7acc904e85c63b342e9c949");
            var synapticFeature=f.Feature("SynapticShockFeature01458",synaptic.Name,synaptic.Description,Major(cls),Facts(synaptic,choose),new SynapticShock01458{Marker=Ref<BlueprintBuffReference>(marker)});Icon(synapticFeature,marker);result.Add(synapticFeature);
            var door=f.Native<BlueprintAbility>("a9b8be9b87865744382f7c64e599aeb2");
            var permits=Enumerable.Range(1,9).Select(i=>f.Buff("SpaceRendingPermit"+i+"01458","Space-rending Spell",D("1SpaceRendingSpellBuff"))).ToArray();
            var travel=new List<BlueprintAbility>();foreach(string pool in new[]{PsychicClass.WisdomPoolId,PsychicClass.CharismaPoolId})for(int i=1;i<=9;i++){
                var a=f.Clone(door,"SpaceRending"+(pool==PsychicClass.WisdomPoolId?"Wisdom":"Charisma")+i+"Ability01458","Space-rending Spell: "+(i*10)+" feet",D("1SpaceRendingSpellBuff"));
                a.ComponentsArray=a.ComponentsArray.Where(c=>!(c is AbilityVariants)&&!(c is AbilityResourceLogic)).Concat(new BlueprintComponent[]{Cost(f.Native<BlueprintAbilityResource>(pool),i),new SpaceRendingCaster01458{Minimum=i,Resource=Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(pool))},Run(new EndSpaceRending01458())}).ToArray();
                a.Type=AbilityType.Supernatural;a.ActionType=UnitCommand.CommandType.Free;a.Range=AbilityRange.Custom;a.CustomRange=(i*10).Feet();a.CanTargetPoint=true;a.CanTargetSelf=a.CanTargetFriends=a.CanTargetEnemies=false;Set(a,"m_Parent",new BlueprintAbilityReference());travel.Add(a);
            }
            var variants=new AbilityVariants();Set(variants,"m_Variants",travel.Select(Ref<BlueprintAbilityReference>).ToArray());var wrapper=f.Ability("SpaceRendingAbility01458","Space-rending Spell",D("1SpaceRendingSpellBuff"),AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,variants);Icon(wrapper,door);foreach(var a in travel)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(wrapper));
            var space=Mode("SpaceRendingSpell","Space-rending Spell",D("1SpaceRendingSpellBuff"),PsychicAmplificationKind.SpaceRendingSpell,0,0,11,"a9b8be9b87865744382f7c64e599aeb2");var spaceFeature=f.Feature("SpaceRendingSpellFeature01458",space.Name,space.Description,Major(cls),Facts(space,wrapper));Icon(spaceFeature,space);result.Add(spaceFeature);
            var dual=f.Feature("DualAmplificationFeature01458",N("DualAmplificationFeature"),D("DualAmplificationFeature"),Major(cls),new DualAmplification01458());Icon(dual,f.Native<BlueprintUnitFact>("f180e72e4a9cbaa4da8be9bc958132ef"));result.Add(dual);
            result.AddRange(Mimic(f,cls,modes));return result.ToArray();
        }
        internal static int MetaCost(Metamagic meta)=>meta==PsychicCasting.Logical||meta==TopplingSpell.Metamagic?1:meta.DefaultCost();
        static IEnumerable<BlueprintFeature> Mimic(PsychicAssets f,BlueprintCharacterClass cls,List<BlueprintActivatableAbility> modes)
        {
            var stock=NativeMetamagics.Select(f.Native<BlueprintFeature>).Concat(new[]{f.Native<BlueprintFeature>(PsychicClass.LogicalId),f.Cache.Load(Id("TopplingSpellFeat")) as BlueprintFeature}.Where(v=>v!=null)).ToArray();
            return MimicCatalogue01461.Build(f,cls,modes,stock);
        }
    }
}
