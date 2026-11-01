using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicFieryShuriken {
  internal static void Build(PsychicAssets f){
   const string text="Create two fiery shuriken plus one per two caster levels beyond 3rd, maximum eight at caster level 15. Each projectile makes its own ranged touch attack for 1d8 fire damage; spell resistance applies and there is no save. Choose to launch all at one target immediately, or launch one and retain the rest for 1 round per caster level. Later, launch one as a swift action, or two to seven as a standard action. Launching retained shuriken does not provoke attacks of opportunity. Recasting replaces any retained shuriken. This follows Call of the Wild's initial one/all selection.";
   var donor=f.Native<BlueprintAbility>("4ecdf240d81533f47a5279f5075296b9");var icon=donor.Icon;
   var resource=f.Add(new BlueprintAbilityResource(),"FieryShurikenResource","Fiery Shuriken",text);resource.LocalizedName=Text("FieryShurikenResource","name","Fiery Shuriken");resource.LocalizedDescription=Text("FieryShurikenResource","description",text);var amount=Get(resource,"m_MaxAmount");Set(amount,"BaseValue",8);Set(resource,"m_MaxAmount",amount);
   var host=f.Buff("FieryShurikenBuff","Fiery Shuriken",text,new PsychicSeedPool {Resource=Ref<BlueprintAbilityResourceReference>(resource),Shuriken=true});Set(host,"m_Icon",icon);SetEnum(host,"m_Flags","RemoveOnRest");
   BlueprintAbility Projectile(string token,string title,int count,bool original,bool all=false){
    var a=f.Clone(donor,token,title,text);var delivery=a.GetComponent<AbilityDeliverProjectile>();if(delivery==null||delivery.Projectiles.Length==0)throw new InvalidOperationException("Fiery Shuriken requires native fire bolt projectile.");Set(delivery,"m_Projectiles",Enumerable.Repeat(Ref<BlueprintProjectileReference>(delivery.Projectiles[0]),count).ToArray());delivery.DelayBetweenProjectiles=.2f;delivery.UseMaxProjectilesCount=all;delivery.MaxProjectilesCountRank=AbilityRankType.StatBonus;
    var damage=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=DamageEnergyType.Fire},Value=new ContextDiceValue {DiceType=DiceType.D8,DiceCountValue=1,BonusValue=0},DisableSneakDamage=true,IgnoreCritical=true};
    a.ComponentsArray=new BlueprintComponent[]{new SpellComponent {School=SpellSchool.Conjuration},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Fire},delivery,Run(damage)};a.Type=original?AbilityType.Spell:AbilityType.Supernatural;a.ActionType=!original&&count==1?UnitCommand.CommandType.Swift:UnitCommand.CommandType.Standard;a.Range=AbilityRange.Close;a.CanTargetSelf=false;a.CanTargetFriends=true;a.CanTargetEnemies=true;a.SpellResistance=true;a.AvailableMetamagic=Metamagic.Reach|Metamagic.Empower|Metamagic.Maximize|Metamagic.Piercing|(original?Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken:0);a.LocalizedDuration=Text(token,"duration",original?"1 round per caster level (retained projectiles)":"Instantaneous");a.LocalizedSavingThrow=Text(token,"savingThrow","");Set(a,"m_Icon",icon);return a;
   }
   var choices=new BlueprintAbility[7];for(int i=1;i<=7;i++){
    var a=Projectile("FieryShurikenLaunch"+i,"Fiery Shuriken: "+i+(i==1?" (Swift)":" (Standard)"),i,false);var resourceLogic=new AbilityResourceLogic {Amount=i};Set(resourceLogic,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));Set(resourceLogic,"m_IsSpendResource",true);var needs=new AbilityCasterHasFacts();Set(needs,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(host)});a.ComponentsArray=a.ComponentsArray.Concat(new BlueprintComponent[]{resourceLogic,needs}).ToArray();choices[i-1]=a;var original=new ReplaceAbilityParamsWithContext();Set(original,"m_Ability",Ref<BlueprintAbilityReference>(a));host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)original}).ToArray();
   }
   BlueprintAbility Menu(string token,string title,BlueprintAbility[] options,bool spell){var v=new AbilityVariants();Set(v,"m_Variants",options.Select(Ref<BlueprintAbilityReference>).ToArray());var a=f.Ability(token,title,text,spell?AbilityType.Spell:AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Close,new SpellComponent {School=SpellSchool.Conjuration},v);a.SpellResistance=true;a.AvailableMetamagic=options[0].AvailableMetamagic;Set(a,"m_Icon",icon);a.LocalizedDuration=Text(token,"duration",spell?"1 round per caster level":"Instantaneous");a.LocalizedSavingThrow=Text(token,"savingThrow","");foreach(var o in options)Set(o,"m_Parent",Ref<BlueprintAbilityReference>(a));return a;}
   var menu=Menu("FieryShurikenLaunch","Fiery Shuriken: Launch",choices,false);host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)Facts(menu)}).ToArray();
   var one=Projectile("FieryShurikenOne","Fiery Shuriken: Launch One and Retain Rest",1,true);var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");var apply=Apply(host,0,toCaster:true);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};var start=new AbilityExecuteActionOnCast {Conditions=new ConditionsChecker {Conditions=Array.Empty<Condition>()},Actions=Actions(apply)};one.ComponentsArray=one.ComponentsArray.Concat(new BlueprintComponent[]{rank,start}).ToArray();
   var all=Projectile("FieryShurikenAll","Fiery Shuriken: Launch All",8,true,true);var number=new ContextRankConfig();SetEnum(number,"m_BaseValueType","CasterLevel");SetEnum(number,"m_Type","StatBonus");SetEnum(number,"m_Progression","DelayedStartPlusDivStep");Set(number,"m_StartLevel",1);Set(number,"m_StepLevel",2);Set(number,"m_UseMin",true);Set(number,"m_Min",2);Set(number,"m_UseMax",true);Set(number,"m_Max",8);var remove=new ContextActionRemoveBuff {ToCaster=true};Set(remove,"m_Buff",Ref<BlueprintBuffReference>(host));all.ComponentsArray=all.ComponentsArray.Concat(new BlueprintComponent[]{number,new AbilityExecuteActionOnCast {Conditions=new ConditionsChecker {Conditions=Array.Empty<Condition>()},Actions=Actions(remove)}}).ToArray();
   Menu("FieryShuriken","Fiery Shuriken",new[]{one,all},true);foreach(var pair in f.Assets.Where(x=>x.Key.StartsWith("FieryShuriken")))PsychicRepairComponents.UniqueNames(pair.Value);
  }
 }
}
