using System;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Commands;
using Way_of_the_shield.NewFeatsAndAbilities;
using static Way_of_the_shield.NewComponents.OffHandParry;
using Way_of_the_shield;
using Way_of_the_shield.NewComponents;

public static partial class FeatRuntimeTests {
 static void FeedbackProbe(){
  Console.WriteLine("Feedback native AC/stat and rule subscription probe");
  var u=Unit("ac-native");var a=Unit("ac-attacker");
  Set(u.State,"Prone",Empty<ProneState>());
  var ac=new ModifiableValueArmorClass(u.Stats);Set(u.Stats,"AC",ac);u.Stats.AllStats.Add(ac);
  ac.BaseValue=10;ac.AddModifier(8,(EntityFact)null,ModifierDescriptor.Armor);ac.AddModifier(2,(EntityFact)null,ModifierDescriptor.Shield);ac.AddModifier(3,(EntityFact)null,ModifierDescriptor.Deflection);
  Console.WriteLine("AC panel "+ac.ModifiedValue);
  h.Patch(AccessTools.Method(typeof(RuleCalculateAC),"OnTrigger"),transpiler:new HarmonyMethod(typeof(FeatRuntimeTests),nameof(Dispatch)));
  var r=new RuleCalculateAC(a,u,AttackType.Melee);r.OnTrigger(null);
  Check(r.Result==23&&ac.ModifiedValue==23,"native ordinary AC preserves armor/shield/deflection");
  var owner=new Harmony("CotW.WotS.Feedback.Production");
  try {
   Way_of_the_shield.Main.InstallRuntime(owner);
   r=new RuleCalculateAC(a,u,AttackType.Melee);r.OnTrigger(null);
   Check(r.Result==23&&ac.ModifiedValue==23,"all module Harmony patches preserve ordinary native AC");
   NativeShieldAC();NativeTWF();NativeStyle();NativeBastion();NativeParryCount();FeedbackBoundaries();ShieldStacking();Feedback01410();
  } finally { owner.UnpatchAll(owner.Id); }
 }
 static void NativeShieldAC(){
  var u=Unit("actual-shield-ac");Set(u.State,"Prone",Empty<ProneState>());var ac=new ModifiableValueArmorClass(u.Stats);Set(u.Stats,"AC",ac);u.Stats.AllStats.Add(ac);ac.BaseValue=10;
  var shield=Equip(u,ArmorProficiencyGroup.Buckler);ac.AddModifier(8,(EntityFact)null,ModifierDescriptor.Armor);ac.AddItemModifier(1,shield.ArmorComponent,ModifierDescriptor.Shield);ac.AddItemModifier(2,shield.ArmorComponent,ModifierDescriptor.ShieldEnhancement);ac.AddModifier(3,(EntityFact)null,ModifierDescriptor.Deflection);
  var enemy=Unit("shield-ac-enemy");int baseline=ac.ModifiedValue;Check(baseline==24,"native shield enhancement distinct from armor/deflection");
  var flags=u.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>();flags.ShieldDenied.Retain();ShieldAcState.Refresh(u);
  var e=new RuleCalculateAC(enemy,u,AttackType.Melee);e.OnTrigger(null);Check(ac.ModifiedValue==21&&e.Result==21,"denied buckler removes only worn3 from panel and native rule");
  e=new RuleCalculateAC(enemy,u,AttackType.Touch);e.OnTrigger(null);Check(e.Result==13,"touch remains13 without subtracting shield twice");
  flags.ShieldDenied.Release();ShieldAcState.Refresh(u);Check(ac.ModifiedValue==baseline,"native worn shield restores without rebuilding item or other modifiers");
  var visible=Fact(u,(BlueprintBuff)cache.Load(BlueprintGuid.Parse("807ca5b2501444aab22a0f08a64691e1")),u);var defensive=Fact(u,(BlueprintBuff)cache.Load(BlueprintGuid.Parse("6ffd93355fb3bcf4592a5d976b1d32a9")),u);
  var effect=Fact(u,(BlueprintBuff)cache.Load(BlueprintGuid.Parse("29397505648a462a874872b132a00b75")),u);Equip(u,ArmorProficiencyGroup.HeavyShield);Call(effect,typeof(BuffSetRanksOnApply),"SetRanks");ShieldAcState.Refresh(u);
  int extra=1+10/4;Console.WriteLine("Defense rank "+effect.Rank+" context "+effect.Context[AbilityRankType.StatBonus]+" ac "+ac.ModifiedValue+" hasVisible "+u.HasFact(visible.Blueprint)+" hasDefensive "+u.HasFact(defensive.Blueprint)+" on "+effect.IsTurnedOn);Check(ac.ModifiedValue==baseline+extra,"defense extra3 is persistent on character panel");
  e=new RuleCalculateAC(enemy,u,AttackType.Melee);e.OnTrigger(null);Check(e.Result==baseline+extra,"native AC uses persistent defense once");
  flat=true;e=new RuleCalculateAC(enemy,u,AttackType.Melee);e.OnTrigger(null);Check(e.Result==baseline,"flat-footed native value removes defense only");flat=false;
  using(effect.Context.GetDataScope(u))new ContextActionRemoveBuffRanksCustom{m_Buff=Ref<BlueprintBuffReference>(effect.Blueprint),min=0,value=2,ToCaster=true,RemoveWhenZero=false}.RunAction();Check(ac.ModifiedValue==baseline+extra-1,"charge loss immediately updates panel");
  defensive.Remove();ShieldAcState.Refresh(u);Check(ac.ModifiedValue==baseline,"ending native defensive fight removes extra panel AC");
 }
 static void Subscribe(EntityFact f,bool on){if(on)((BlueprintScriptableObject)f.Blueprint).OnEnable();foreach(var rt in f.Components){if(on)Set(rt,"IsTurnedOn",true);AccessTools.Method(typeof(EntityFactComponent),on?"Subscribe":"Unsubscribe").Invoke(rt,null);}AccessTools.Method(typeof(EntityFact),on?"Subscribe":"Unsubscribe").Invoke(f,null);}
 static void NativeTWF(){
  var u=Unit("native-dual");var shield=Equip(u,ArmorProficiencyGroup.LightShield);u.State.Features.ShieldBash.Retain();
  var bp=new BlueprintFeature{Ranks=5};bp.AddComponent(new TwoWeaponFightingAttackPenalty());var f=Feature(u,bp,2);Subscribe(f,true);
  try {
   foreach(bool ordinary in new[]{false,true}) {
    if(ordinary){shields[u.Body.SecondaryHand]=null;Set(u.Body.SecondaryHand,"m_FakeItem",Weapon(WeaponCategory.Dagger));}
    foreach(var w in new[]{u.Body.PrimaryHand.MaybeWeapon,u.Body.SecondaryHand.MaybeWeapon}) {
     var e=new RuleCalculateAttackBonusWithoutTarget(u,w,0){Reason=new RuleReason(f)};RulebookEventBus.OnEventAboutToTrigger(e);
     Console.WriteLine("TWF "+ordinary+" "+w?.Blueprint.Category+" "+e.AllBonuses.Sum(m=>m.Value));
     Check(e.AllBonuses.Sum(m=>m.Value)==-4,"native subscribed TWF each hand, non-light fixture shield or ordinary weapon");
    }
   }
  }finally{Subscribe(f,false);}
 }
 static void NativeStyle(){
  var u=Unit("native-style");var enemy=Unit("native-style-target");var shield=Equip(u,ArmorProficiencyGroup.Buckler);
  var f=Fact(u,Way_of_the_shield.NewFeatsAndAbilities.UpsettingShieldStyle.MainBuff,u);Subscribe(f,true);
  try {
   var e=new RuleDealDamage(u,enemy,new DamageBundle{Weapon=shield.WeaponComponent});Set(e,"Result",5);RulebookEventBus.OnEventDidTrigger(e);
   var penalty=enemy.Buffs.GetBuff(Way_of_the_shield.NewFeatsAndAbilities.UpsettingShieldStyle.StyleBuff);
   Check(penalty!=null,"native subscribed shield damage applies style");Subscribe(penalty,true);
   try{var ab=Empty<RuleCalculateAttackBonus>();Set(ab,"Initiator",enemy);Set(ab,"Target",u);Set(ab,"m_InnerRule",new RuleCalculateAttackBonusWithoutTarget(enemy,Weapon(Kingmaker.Enums.WeaponCategory.Longsword),0));RulebookEventBus.OnEventAboutToTrigger(ab);Check(ab.AllBonuses.Sum(m=>m.Value)==-2,"native subscribed style penalty against caster");}finally{Subscribe(penalty,false);}
  }finally{Subscribe(f,false);}
 }
 static void NativeBastion(){
  var paladin=Unit("bastion-paladin");var enemy=Unit("bastion-enemy");var ally=Unit("bastion-ally");
  var marker=new BlueprintBuff();Add(marker,"5240b74448e345d6b6d4b077c12e75b5");var mark=new BlueprintBuff();mark.AddComponent(new HalveDamageIfHasBuffFromCaster{m_Buff=Ref<BlueprintBuffReference>(marker)});
  var debuff=Fact(enemy,mark,paladin);var aura=Fact(ally,marker,paladin);Subscribe(debuff,true);
  try {
   var damage=new DirectDamage(new DiceFormula(1,DiceType.D6),20);
   var e=new RuleCalculateDamage(new RuleDealDamage(enemy,ally,new DamageBundle(damage)));RulebookEventBus.OnEventAboutToTrigger(e);
   Check(damage.Decline.Value==DamageDeclineType.ByHalf,"native subscribed Bastion matches own ally marker");
  }finally{Subscribe(debuff,false);}
 }
 static void NativeParryCount(){
  var u=Unit("native-parry-count");Equip(u,ArmorProficiencyGroup.Buckler);
  var bp=(BlueprintFeature)cache.Load(BlueprintGuid.Parse("6948b379c0562714d9f6d58ccbfa8faa"));bp.AddComponent(new TwoWeaponFightingAttacks());var f=Feature(u,bp,1);Subscribe(f,true);
  var mf=Feature(u,Mythic_BucklerParry.Mythic_BucklerParryFeature,1);Subscribe(mf,true);nativeCount=true;
  var part=u.Get<OffHandParryUnitPart>();
  try{
   foreach(int rank in new[]{1,2,3,4}){
    Set(f,"Rank",rank);part.HandleTurnStarted(u);part.TryActivate(WeaponCategory.WeaponLightShield,false,new OffHandParryData());
    Console.WriteLine("Actual native parry rank"+rank+" roster "+string.Join(",",part.State.AttackPenalties));
    Check(part.n==(rank==1?1:rank*2-2),"native TWF+Mythic parry roster rank"+rank);
   }
   h.Unpatch(AccessTools.PropertyGetter(typeof(RuleAttackRoll.ParryData),"IsTriggered"),HarmonyPatchType.Prefix,h.Id);
  var enemy=Unit("native-parry-enemy");int attempts=0;
   var missed=Incoming(enemy,u);part.OnEventAboutToTrigger(missed);part.OnEventDidTrigger(missed);
   Check(part.State.Used==0,"native no-roll miss consumes no parry");
   for(int i=0;i<11;i++){
    var e=Incoming(enemy,u);part.OnEventAboutToTrigger(e);if(e.Parry==null)continue;attempts++;
    Set(e.Parry,"Roll",DiceOutcome(10));Set(e,"Result",i<4?AttackResult.Hit:AttackResult.Parried);part.OnEventDidTrigger(e);
   }
   Check(attempts==6&&part.State.Used==6&&part.n==0,"four failed plus two successful actual-triggered parries exhaust6; no seventh attempt");
   part.Deactivate();Subscribe(mf,false);u.Facts.List.Remove(mf);Refresh(u);
   foreach(int rank in new[]{1,2,3,4}){Set(f,"Rank",rank);part.HandleTurnStarted(u);part.TryActivate(WeaponCategory.WeaponLightShield,false,new OffHandParryData());Check(part.n==Math.Max(1,rank-1),"native nonmythic limit "+rank);}
  }finally{nativeCount=false;part.Deactivate();Subscribe(mf,false);Subscribe(f,false);}
 }
 static void ShieldStacking(){
  var u=Unit("shield-stacking");var s=Equip(u,ArmorProficiencyGroup.Buckler);Set(u.State,"Prone",Empty<ProneState>());var ac=new ModifiableValueArmorClass(u.Stats);Set(u.Stats,"AC",ac);u.Stats.AllStats.Add(ac);ac.BaseValue=10;
  ac.AddModifier(8,(EntityFact)null,ModifierDescriptor.Armor);ac.AddModifier(3,(EntityFact)null,ModifierDescriptor.Deflection);ac.AddModifier(2,(EntityFact)null,ModifierDescriptor.Dodge);
  ac.AddItemModifier(5,s.ArmorComponent,ModifierDescriptor.Shield);ac.AddModifier(4,(EntityFact)null,ModifierDescriptor.Shield);
  int stored=ac.Modifiers.Count();Check(ac.ModifiedValue==28,"worn shield wins shield spell stacking normally");u.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>().ShieldDenied.Retain();ac.UpdateValue();
  Check(ac.ModifiedValue==27,"denial allows shield spell fallback before native stacking");var attacker=Unit("stack-attacker");var rule=new RuleCalculateAC(attacker,u,AttackType.Melee);rule.OnTrigger(null);Check(rule.Result==27,"native rule matches fallback panel");
  rule=new RuleCalculateAC(attacker,u,AttackType.Touch);rule.OnTrigger(null);Check(rule.Result==15,"native touch retains dodge and deflection only");flat=true;rule=new RuleCalculateAC(attacker,u,AttackType.Touch);rule.OnTrigger(null);Check(rule.Result==13,"native flat touch excludes dodge");flat=false;
  u.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>().ShieldDenied.Release();ac.UpdateValue();Check(ac.ModifiedValue==28&&ac.Modifiers.Count()==stored,"all original modifier sources survive denial and restore");
 }
}
