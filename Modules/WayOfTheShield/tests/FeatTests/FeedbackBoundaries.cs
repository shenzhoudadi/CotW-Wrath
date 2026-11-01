using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.PubSubSystem;
using Way_of_the_shield;
using Way_of_the_shield.NewComponents;

public static partial class FeatRuntimeTests {
 static UnitEntityData mount;
 static bool Difficulty(ref Kingmaker.Settings.DifficultySettings __result){__result=Empty<Kingmaker.Settings.DifficultySettings>();return false;}
 static bool NotPlayer(ref bool __result){__result=false;return false;}
 static bool ThrowLog(Exception __0){throw new Exception("Native subscription exception",__0);}
 static bool Mount(UnitEntityData __0,ref UnitEntityData __result){__result=mount;return false;}
 static void FeedbackBoundaries(){h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Exception",new[]{typeof(Exception),typeof(string),typeof(object[])}),prefix:new HarmonyMethod(typeof(FeatRuntimeTests),nameof(ThrowLog)));PowerScaling();DualMatrix();StyleFallback();DefenseActivation();BastionVisible();}
 static void PowerScaling(){
  var u=Unit("mounted-power");var comp=new WeaponParametersDamageBonus{PowerAttackScaling=true};
  var greater=new BlueprintFeature();var mythic=new BlueprintFeature();Add(greater,"8f1eb9f0a45d432db56fe14c5af39825");Add(mythic,"cf5f7e14fbb94bbba0ffbca548947a1e");
  comp.m_GreaterPowerAttackBlueprint=Ref<BlueprintFeatureReference>(greater);comp.m_MythicBlueprint=Ref<BlueprintFeatureReference>(mythic);
  var method=AccessTools.Method(typeof(WeaponParametersDamageBonus),"CalculatePowerAttackScaling");
  var saddle=AccessTools.Method(typeof(UnitHelper),"GetSaddledUnit");h.Patch(saddle,prefix:new HarmonyMethod(typeof(FeatRuntimeTests),nameof(Mount)));
  try{
   foreach(var category in new[]{WeaponCategory.Spear,WeaponCategory.Longspear,WeaponCategory.Longsword})foreach(bool riding in new[]{false,true}){
    mount=riding?Unit("mount"):null;var weapon=Weapon(category);var rule=Empty<RuleCalculateWeaponStats>();Set(rule,"Initiator",u);Set(rule,"Weapon",weapon);
    bool eligible=riding&&category!=WeaponCategory.Longsword;
    Check((int)method.Invoke(comp,new object[]{rule,6})==(eligible?9:6),"native Power Attack mounted category boundary "+category+"/"+riding);
    var mf=Feature(u,mythic,1);Check((int)method.Invoke(comp,new object[]{rule,9})==(eligible?15:9),"native mythic scaling preserved");u.Facts.List.Remove(mf);Refresh(u);
    var gf=Feature(u,greater,1);Check((int)method.Invoke(comp,new object[]{rule,6})==(eligible?12:6),"native greater scaling preserved");u.Facts.List.Remove(gf);Refresh(u);
    Set(rule,"IsSecondary",true);Check((int)method.Invoke(comp,new object[]{rule,6})==3,"secondary scaling not promoted");Set(rule,"IsSecondary",false);
    comp.PowerAttackScaling=false;Check((int)method.Invoke(comp,new object[]{rule,6})==6,"non-Power-Attack bonus unchanged");comp.PowerAttackScaling=true;
   }
  }finally{mount=null;h.Unpatch(saddle,HarmonyPatchType.Prefix,h.Id);}
 }
 static void DualMatrix(){
  foreach(bool reserved in new[]{false,true})foreach(bool light in new[]{false,true})foreach(int rank in new[]{1,2,4})foreach(bool mythic in new[]{false,true}){
   var u=Unit("dual-matrix");var shield=Equip(u,ArmorProficiencyGroup.LightShield);u.State.Features.ShieldBash.Retain();Set(shield.WeaponComponent.Blueprint.Type,"m_IsLight",light);
   var mbp=new BlueprintFeature();var bp=new BlueprintFeature{Ranks=5};bp.AddComponent(new TwoWeaponFightingAttackPenalty{m_MythicBlueprint=Ref<BlueprintFeatureReference>(mbp)});var f=Feature(u,bp,rank);if(mythic)Feature(u,mbp,1);Subscribe(f,true);
   if(reserved)u.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>().ForceDualWieldingPenalties.Retain();
   try{
    foreach(bool master in new[]{false,true}){
     if(master)u.State.AdditionalFeatures.ShieldMaster.Retain();
     foreach(bool main in new[]{true,false}){
      var weapon=main?u.Body.PrimaryHand.MaybeWeapon:shield.WeaponComponent;
      var e=new RuleCalculateAttackBonusWithoutTarget(u,weapon,0){Reason=new RuleReason(f)};RulebookEventBus.OnEventAboutToTrigger(e);
      int expected=master&&!main?0:ShieldRuntimePatches.CalculateForcedDualWieldingPenalty(rank,main,light,mythic,false);
      Check(e.AllBonuses.Sum(m=>m.Value)==expected,"subscribed native dual matrix "+reserved+"/"+light+"/"+rank+"/"+mythic+"/"+master+"/"+main);
     }
    }
   }finally{Subscribe(f,false);}
  }
 }
 static void StyleFallback(){
  var u=Unit("fallback-style");var enemy=Unit("fallback-target");var shield=Equip(u,ArmorProficiencyGroup.Buckler);
  var f=Fact(u,Way_of_the_shield.NewFeatsAndAbilities.UpsettingShieldStyle.MainBuff,u);Subscribe(f,true);
  try{
   var incoming=Incoming(u,enemy);Set(incoming.WeaponStats,"Weapon",shield.WeaponComponent);
   var e=new RuleDealDamage(u,enemy,new DamageBundle()){AttackRoll=incoming};Set(e,"Result",5);RulebookEventBus.OnEventDidTrigger(e);
   Check(enemy.Buffs.HasFact(Way_of_the_shield.NewFeatsAndAbilities.UpsettingShieldStyle.StyleBuff),"style uses native AttackRoll shield when bundle source absent");
   var other=Unit("ordinary-hit");var normal=new RuleDealDamage(u,other,new DamageBundle()){AttackRoll=Incoming(u,other)};Set(normal,"Result",5);RulebookEventBus.OnEventDidTrigger(normal);
   Check(!other.Buffs.HasFact(Way_of_the_shield.NewFeatsAndAbilities.UpsettingShieldStyle.StyleBuff),"ordinary weapon cannot apply upsetting penalty");
   var zero=Unit("no-damage");var blocked=new RuleDealDamage(u,zero,new DamageBundle()){AttackRoll=incoming};RulebookEventBus.OnEventDidTrigger(blocked);Check(!zero.Buffs.HasFact(Way_of_the_shield.NewFeatsAndAbilities.UpsettingShieldStyle.StyleBuff),"zero damage cannot apply penalty");
  }finally{Subscribe(f,false);}
 }
 static void DefenseActivation(){
  foreach(bool defensiveFirst in new[]{false,true}){
   var u=Unit("defense-order");Equip(u,ArmorProficiencyGroup.HeavyShield);var ac=new ModifiableValueArmorClass(u.Stats);Set(u.Stats,"AC",ac);u.Stats.AllStats.Add(ac);ac.BaseValue=10;ac.AddModifier(8,(EntityFact)null,ModifierDescriptor.Armor);
   var main=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("807ca5b2501444aab22a0f08a64691e1"));var defensive=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("6ffd93355fb3bcf4592a5d976b1d32a9"));var effect=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("29397505648a462a874872b132a00b75"));
   Kingmaker.UnitLogic.Buffs.Buff fight=null;if(defensiveFirst)fight=Fact(u,defensive,u);var visible=Fact(u,main,u);Call(visible,typeof(ShieldedDefenseState),"OnTurnOn");
   if(!defensiveFirst){Check(!u.Buffs.HasFact(effect),"defense waits for actual defensive fight buff");fight=Fact(u,defensive,u);Call(visible,typeof(ShieldedDefenseState),"HandleBuffDidAdded",fight);}
   var active=u.Buffs.GetBuff(effect);Check(active!=null&&active.Context[Kingmaker.Enums.AbilityRankType.StatBonus]==3&&ac.ModifiedValue==21,"either activation order initializes native rank and panel");
   using(active.Context.GetDataScope(u))new ContextActionRemoveBuffRanksCustom{m_Buff=Ref<BlueprintBuffReference>(effect),min=0,value=2,ToCaster=true,RemoveWhenZero=false}.RunAction();int spent=active.Rank;
   Call(visible,typeof(ShieldedDefenseState),"OnApplyPostLoadFixes");Check(active.Rank==spent&&ac.ModifiedValue==20,"postload binder preserves spent ranks");
   ac.UpdateValue();Check(ac.ModifiedValue==20,"panel update is idempotent");
   shields[u.Body.SecondaryHand]=null;ac.UpdateValue();Check(ac.ModifiedValue==18,"equipment loss immediately removes defense from panel");
   Equip(u,ArmorProficiencyGroup.HeavyShield);ac.UpdateValue();Check(ac.ModifiedValue==20,"re-equip preserves remaining defense ranks");
   fight.Remove();Call(visible,typeof(ShieldedDefenseState),"HandleBuffDidRemoved",fight);Check(!u.Buffs.HasFact(effect)&&ac.ModifiedValue==18,"ending defensive fight removes owned effect without clearing armor");
  }
 }
 static void BastionVisible(){
  var paladin=Unit("visible-paladin");var wrong=Unit("other-paladin");var enemy=Unit("visible-enemy");var marker=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("5240b74448e345d6b6d4b077c12e75b5"));
  var mark=new BlueprintBuff();mark.AddComponent(new HalveDamageIfHasBuffFromCaster{m_Buff=Ref<BlueprintBuffReference>(marker)});var debuff=Fact(enemy,mark,paladin);Subscribe(debuff,true);
  h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Settings.SettingsRoot),"Difficulty"),prefix:new HarmonyMethod(typeof(FeatRuntimeTests),nameof(Difficulty)));
  h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsPlayerFaction"),prefix:new HarmonyMethod(typeof(FeatRuntimeTests),nameof(NotPlayer)));
  try{foreach(string id in new[]{"f7e1a37ceeaa41628cd1b04731eaee69","14bf28d3c74e4fb0a952d8f114633f66"})foreach(bool own in new[]{false,true}){
   var aura=new BlueprintBuff();Add(aura,id);var ally=Unit("visible-ally");Fact(ally,aura,own?paladin:wrong);
   var d=new DirectDamage(DiceFormula.Zero,40){PreRolledValue=40};var e=new RuleCalculateDamage(new RuleDealDamage(enemy,ally,new DamageBundle(d))){Reason=new RuleReason(debuff)};RulebookEventBus.OnEventAboutToTrigger(e);Check((d.Decline.Value==DamageDeclineType.ByHalf)==own,"Bastion real aura identity and caster matching "+id+"/"+own);
   var value=(DamageValue)AccessTools.Method(typeof(RuleCalculateDamage),"CalculateDamageValue").Invoke(e,new object[]{d});Check(value.ValueWithoutReduction==(own?20:40),"native damage calculation actually halves40 to20 only for matching aura");
   if(own){RulebookEventBus.OnEventAboutToTrigger(e);Check(d.Decline.Value==DamageDeclineType.ByHalf,"duplicate Bastion handler cannot quarter damage");}
  }
  Fact(paladin,marker,paladin);var self=new DirectDamage(DiceFormula.Zero,40);var sr=new RuleCalculateDamage(new RuleDealDamage(enemy,paladin,new DamageBundle(self)));RulebookEventBus.OnEventAboutToTrigger(sr);Check(self.Decline.Value!=DamageDeclineType.ByHalf,"Bastion excludes own paladin");
  }finally{Subscribe(debuff,false);h.Unpatch(AccessTools.PropertyGetter(typeof(Kingmaker.Settings.SettingsRoot),"Difficulty"),HarmonyPatchType.Prefix,h.Id);h.Unpatch(AccessTools.PropertyGetter(typeof(UnitEntityData),"IsPlayerFaction"),HarmonyPatchType.Prefix,h.Id);}
 }
}
