using System;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Controllers.Units;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicFireSeeds {
  internal static void Build(PsychicAssets f){
   const string text="Create fire seeds for 10 minutes per caster level. The recipient gains a pool of fire dice equal to caster level, maximum 20. Choose 1 to 10 dice per standard-action throw, spending that many dice. A ranged touch hit deals that many d4 fire damage, plus 1 fire damage per die to all creatures within 5 feet, including the primary target. A miss deals only splash damage. No save; spell resistance does not apply. This follows Call of the Wild's acorn-grenade implementation. Recasting replaces the remaining pool; the abilities disappear when the spell ends.";
   var icon=f.Native<BlueprintAbility>("f72f8f03bf0136c4180cd1d70eb773a5").Icon;
   var resource=f.Add(new BlueprintAbilityResource(),"FireSeedsResource","Fire Seeds",text);
   resource.LocalizedName=Text("FireSeedsResource","name","Fire Seeds");resource.LocalizedDescription=Text("FireSeedsResource","description",text);
   var amount=Get(resource,"m_MaxAmount");Set(amount,"BaseValue",20);Set(resource,"m_MaxAmount",amount);
   var host=f.Buff("FireSeedsBuff","Fire Seeds",text,new PsychicSeedPool {Resource=Ref<BlueprintAbilityResourceReference>(resource)});Set(host,"m_Icon",icon);SetEnum(host,"m_Flags","RemoveOnRest");
   var donor=f.Native<BlueprintAbility>("5fa0111ac60ed194db82d3110a9d0352");var variants=new BlueprintAbility[10];
   for(int i=1;i<=10;i++){
    var hit=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=DamageEnergyType.Fire},Value=new ContextDiceValue {DiceType=DiceType.D4,DiceCountValue=i,BonusValue=0},IgnoreCritical=true,DisableSneakDamage=true};
    var splash=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=DamageEnergyType.Fire},Value=new ContextDiceValue {DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=i},IgnoreCritical=true,DisableSneakDamage=true,IsAoE=true};
    var a=f.Clone(donor,"FireSeedsThrow"+i,"Fire Seeds: "+i+"d4",text);
    var delivery=a.GetComponent<AbilityDeliverProjectile>();var targets=a.GetComponent<AbilityTargetsAround>();
    if(delivery==null||targets==null)throw new InvalidOperationException("Fire Seeds requires native bomb projectile and splash selector.");
    var spend=new AbilityResourceLogic {Amount=i};Set(spend,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));Set(spend,"m_IsSpendResource",true);
    var needs=new AbilityCasterHasFacts();Set(needs,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(host)});
    a.ComponentsArray=new BlueprintComponent[]{delivery,targets,spend,needs,new AbilityCasterNotPolymorphed(),new SpellComponent {School=SpellSchool.Conjuration},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Fire},Run(new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{new ContextConditionIsMainTarget()}},IfTrue=Actions(hit,splash),IfFalse=Actions(splash)}),new AbilityEffectMiss {UseTargetSelector=true,MissAction=Actions(splash)}};
    a.Type=AbilityType.SpellLike;a.ActionType=UnitCommand.CommandType.Standard;a.Range=AbilityRange.Custom;a.CustomRange=new Feet(25);a.SpellResistance=false;a.CanTargetSelf=false;a.CanTargetFriends=true;a.CanTargetEnemies=true;a.CanTargetPoint=false;a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize;Set(a,"m_Icon",icon);Display(a,"Instantaneous");variants[i-1]=a;
   }
   var choices=new AbilityVariants();Set(choices,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
   var menu=f.Ability("FireSeedsThrow","Fire Seeds: Throw",text,AbilityType.SpellLike,UnitCommand.CommandType.Standard,AbilityRange.Custom,choices);menu.CustomRange=new Feet(25);menu.SpellResistance=false;Set(menu,"m_Icon",icon);Display(menu,"Instantaneous");
   foreach(var a in variants){Set(a,"m_Parent",Ref<BlueprintAbilityReference>(menu));var original=new ReplaceAbilityParamsWithContext();Set(original,"m_Ability",Ref<BlueprintAbilityReference>(a));host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)original}).ToArray();}
   host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)Facts(menu)}).ToArray();
   var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");var apply=Apply(host,0,DurationRate.TenMinutes);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
   var spell=f.Ability("FireSeeds","Fire Seeds",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Touch,rank,new SpellComponent {School=SpellSchool.Conjuration},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Fire},Run(apply),new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId="930c1a4aa129b8344a40c8c401d99a04"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget});
   spell.CanTargetSelf=true;spell.CanTargetFriends=true;spell.CanTargetEnemies=false;spell.NotOffensive=true;spell.SpellResistance=false;spell.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Extend|Metamagic.Reach|Metamagic.Heighten|Metamagic.Quicken;Set(spell,"m_Icon",icon);Display(spell,"10 minutes per caster level");
   foreach(var p in f.Assets.Where(x=>x.Key.StartsWith("FireSeeds")))PsychicRepairComponents.UniqueNames(p.Value);
  }
  static void Display(BlueprintAbility a,string duration){a.LocalizedDuration=Text(a.name,"duration",duration);a.LocalizedSavingThrow=Text(a.name,"savingThrow","");}
 }
 [TypeId("d3087e54eaaa4f03b8c696a8b7bbc5e5")]
 public sealed class PsychicSeedPool:UnitBuffComponentDelegate,ITickEachRound {
  public BlueprintAbilityResourceReference Resource;public bool Shuriken;
  protected override void OnActivate(){var pool=Resource.Get();var r=Owner.Descriptor.Resources;r.Add(pool,false);r.Spend(pool,r.GetResourceAmount(pool));r.Restore(pool,Shuriken?Math.Min(7,1+Math.Max(0,Context.Params.CasterLevel-3)/2):Math.Max(0,Math.Min(20,Context.Params.CasterLevel)));}
  protected override void OnDeactivate(){Owner.Descriptor.Resources.Remove(Resource.Get());}
  public void OnNewRound(){if(Owner.Descriptor.Resources.GetResourceAmount(Resource.Get())==0)Buff.Remove();}
 }
}
