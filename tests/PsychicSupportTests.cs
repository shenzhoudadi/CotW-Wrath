using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Components;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicSupportTests
{
 static int count,removed,applied,bowAttacks;static RuleAttackWithWeapon lastAttack;static ItemEntityWeapon equipped;static bool undead;static ContextActionApplyBuff application;static EntityFactsManager facts;
 static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
 static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
 static void Check(bool ok,string why){if(!ok)throw new Exception("Support: "+why);count++;}
 static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicEtherPenalty).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
 static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
 static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
 static void Clear(){facts.List.Clear();Refresh();}
 static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
 static MechanicsContext Context(UnitEntityData a,BlueprintScriptableObject bp,int cl=10,Metamagic mm=0){var ctx=new MechanicsContext(a,a.Descriptor,bp,null,new TargetWrapper(a));ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=3,Metamagic=mm});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(ctx);Set(ctx,"m_Ranks",ranks);return ctx;}
 static ItemEntityWeapon Weapon(AttackType attack){var type=new BlueprintWeaponType();Set(type,"m_AttackType",attack);var bp=new BlueprintItemWeapon();var r=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,type,null);Set(bp,"m_Type",r);var w=Empty<ItemEntityWeapon>();Set(w,"m_Blueprint",bp);return w;}
 internal static int Run(BlueprintsCache cache){
  var a=new Psychic070Tests.Caster(cache);var unit=a.Unit;facts=new EntityFactsManager(unit);Set(unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(unit,"<Buffs>k__BackingField",buffs);
  var h=new Harmony("CotW01318.SupportBoundaries");h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(PsychicSupportTests),nameof(Remove)));h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicSupportTests),nameof(Apply)));h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor),"IsUndead"),prefix:new HarmonyMethod(typeof(PsychicSupportTests),nameof(Undead)));
  try{Metadata(cache,unit);Bow(cache,unit,h);Ether(cache,unit);Cloak(cache,unit);Smite(cache,unit);}finally{h.UnpatchAll(h.Id);Clear();}
  PsychicExpansionTests.Inventory["supportSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" support checks; real context duration and rule callbacks, source/caster filtering, cooldown and smite replacement; final buff/scene endpoints substituted.");return count;
 }
 static void Metadata(BlueprintsCache c,UnitEntityData u){
  string[] names={"CondensedEther","BarrowHaze","BowSpirit","AlliedCloak","SmiteAbomination"};int[][] levels={new[]{5},new[]{3},new[]{4},new[]{3,3,3},new[]{4,5}};
  for(int i=0;i<names.Length;i++){
   var bp=Asset<BlueprintAbility>(c,names[i]);Check(bp.Parent==null&&bp.Type==AbilityType.Spell&&!bp.SpellResistance&&!ReferenceEquals(bp.Icon,null),"Root, icon and SR "+names[i]);Check(bp.GetComponents<SpellListComponent>().Select(l=>l.SpellLevel).OrderBy(l=>l).SequenceEqual(levels[i]),"Class levels "+names[i]);foreach(var l in bp.GetComponents<SpellListComponent>())Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(s=>s==bp)==1,"Reciprocal list unique");
   if(i==4)continue;var action=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();var duration=(action as ContextActionApplyBuff)?.DurationValue??((ContextActionSpawnAreaEffect)action).DurationValue;
   foreach(int cl in new[]{1,5,10,20,40})foreach(bool ext in new[]{false,true})Check(duration.Calculate(Context(u,bp,cl,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromSeconds(cl*(i<2?60:6)*(ext?2:1)),"Duration and Extend "+names[i]);
  }
  var shot=Asset<BlueprintAbility>(c,"BowSpiritShot");var attack=shot.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();Check(shot.Type==AbilityType.Supernatural&&shot.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift&&shot.Range==AbilityRange.Weapon&&attack is PsychicBowSpiritAttack,"Single swift native equipped weapon attack");
  foreach(var bp in new[]{shot,Asset<BlueprintAbility>(c,"BowSpirit")})Check(bp.GetComponent<Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterMainWeaponCheck>().Category.SequenceEqual(new[]{WeaponCategory.Longbow,WeaponCategory.Shortbow}),"Only bows, composite included");
  Check(Asset<BlueprintBuff>(c,"BowSpiritBuff").GetComponent<AddFacts>().Facts.Single()==shot,"Native temporary grant ownership");
  var shield=Asset<BlueprintBuff>(c,"AlliedCloakBuff").GetComponent<AddStatBonus>();Check(shield.Stat==StatType.AC&&shield.Value==2&&shield.Descriptor==ModifierDescriptor.Shield,"Cloak native shield stacking");var menu=Asset<BlueprintAbility>(c,"AlliedCloakAid");Check(menu.GetComponent<AbilityVariants>().Variants.Length==2&&menu.GetComponent<AbilityVariants>().Variants.All(v=>v.Parent==menu),"One cloak menu, two child variants");
 }
 static void Bow(BlueprintsCache c,UnitEntityData u,Harmony h){
  var body=Empty<Kingmaker.Items.UnitBody>();Set(u.Descriptor,"<Body>k__BackingField",body);var hand=Empty<HandSlot>();
  h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Items.UnitBody),"PrimaryHand"),prefix:new HarmonyMethod(typeof(PsychicSupportTests),nameof(Hand)));
  h.Patch(AccessTools.PropertyGetter(typeof(HandSlot),"MaybeWeapon"),prefix:new HarmonyMethod(typeof(PsychicSupportTests),nameof(Equipped)));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleAttackWithWeapon),AccessTools.Method(typeof(PsychicSupportTests),nameof(Attack)));
  var spell=Asset<BlueprintAbility>(c,"BowSpiritShot");var action=(PsychicBowSpiritAttack)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
  foreach(var category in new[]{WeaponCategory.Longbow,WeaponCategory.Shortbow,WeaponCategory.LightCrossbow,WeaponCategory.Longsword}){
   equipped=Weapon(category==WeaponCategory.Longsword?AttackType.Melee:AttackType.Ranged);Set(equipped.Blueprint.Type,"Category",category);bowAttacks=0;var ctx=Context(u,spell);using(ctx.GetDataScope(u))action.RunAction();var expected=category==WeaponCategory.Longbow||category==WeaponCategory.Shortbow;Check(bowAttacks==(expected?1:0),"Actual primary bow rule invoked once; crossbow/sword rejected");if(expected)Check(lastAttack.Weapon==equipped&&lastAttack.ExtraAttack&&!lastAttack.IsFullAttack&&lastAttack.AttacksCount==1&&!lastAttack.AutoHit,"Ordinary bow hit/crit/damage rules retained");
  }
  equipped=null;bowAttacks=0;using(Context(u,spell).GetDataScope(u))action.RunAction();Check(bowAttacks==0,"No equipped weapon safely rejected");
 }
 static bool Hand(ref HandSlot __result){__result=Empty<HandSlot>();return false;}
 static bool Equipped(ref ItemEntityWeapon __result){__result=equipped;return false;}
 static bool Attack(RuleAttackWithWeapon rule,ref RuleAttackWithWeapon __result){bowAttacks++;lastAttack=rule;__result=rule;return false;}
 static void Ether(BlueprintsCache c,UnitEntityData u){
  var bp=Asset<BlueprintBuff>(c,"CondensedEtherBuff");var comp=bp.GetComponent<PsychicEtherPenalty>();Check(bp.GetComponent<AddCondition>().Condition==UnitCondition.Slowed,"Slowed includes incorporeal, no immunity bypass");
  foreach(int copies in new[]{1,2,3})foreach(bool deactivate in new[]{false,true}){
   Clear();for(int i=0;i<copies;i++)facts.List.Add(Fact(bp,Context(u,bp)));Refresh();if(deactivate)Set(facts.List[0],"<IsTurnedOn>k__BackingField",false);
   var ac=new RuleCalculateAC(u,u,AttackType.Melee);foreach(var b in u.Buffs.Enumerable.Where(b=>b.IsTurnedOn)){var rt=Runtime(comp,b);using(rt.RequestEventContext())comp.OnEventAboutToTrigger(ac);}Check(ac.AllBonuses.Sum(m=>m.Value)==(deactivate&&copies==1?0:-2),"Ether AC overlap and source handoff");
   foreach(var type in new[]{SavingThrowType.Fortitude,SavingThrowType.Reflex,SavingThrowType.Will}){var save=Empty<RuleSavingThrow>();Set(save,"Initiator",u);Set(save,"Type",type);foreach(var b in u.Buffs.Enumerable.Where(b=>b.IsTurnedOn)){var rt=Runtime(comp,b);using(rt.RequestEventContext())comp.OnEventAboutToTrigger(save);}Check(save.AllBonuses.Sum(m=>m.Value)==(type==SavingThrowType.Reflex&&(!deactivate||copies>1)?-2:0),"Only Reflex and one penalty across overlapping sources");}
  }Clear();
 }
 static void Cloak(BlueprintsCache c,UnitEntityData u){
  var other=new Psychic070Tests.Caster(c).Unit;
  foreach(bool defense in new[]{false,true})foreach(var attack in new[]{AttackType.Melee,AttackType.Touch,AttackType.Ranged,AttackType.RangedTouch})foreach(bool owner in new[]{false,true}){
   Clear();var bp=Asset<BlueprintBuff>(c,"AlliedCloak"+(defense?"Defense":"Attack")+"Buff");var comp=bp.GetComponent<PsychicCloakAid>();var b=Fact(bp,Context(u,bp));facts.List.Add(b);Refresh();var rt=Runtime(comp,b);var weapon=Weapon(attack);Set(u.Stats,"AdditionalAttackBonus",new ModifiableValue(u.Stats,StatType.AdditionalAttackBonus));
   var ab=new RuleCalculateAttackBonusWithoutTarget(u,new RuleCalculateWeaponStats(u,weapon,null),0);var ac=new RuleCalculateAC(other,u,attack);using(rt.RequestEventContext()){comp.OnEventAboutToTrigger(ab);comp.OnEventAboutToTrigger(ac);}Check(ab.AllBonuses.Sum(x=>x.Value)==(!defense&&weapon.Blueprint.IsMelee?2:0),"Cloak aid only melee attack");Check(ac.AllBonuses.Sum(x=>x.Value)==(defense&&(attack==AttackType.Melee||attack==AttackType.Touch)?2:0),"Cloak AC only melee");
   var e=Empty<RuleAttackWithWeapon>();Set(e,"Initiator",defense?other:owner?u:other);Set(e,"Target",defense?(owner?u:other):other);Set(e,"Weapon",weapon);removed=0;using(rt.RequestEventContext())comp.OnEventDidTrigger(e);Check(removed==(owner&&weapon.Blueprint.IsMelee?1:0),"Completed melee consumes once, independent of hit; unrelated/ranged do not");
  }
  var ability=Asset<BlueprintAbility>(c,"AlliedCloakAttack");var ready=ability.GetComponent<PsychicCloakReady>();var cd=ready.Cooldown.Get();Clear();Check(ready.IsCasterRestrictionPassed(u),"No cooldown permits aid");var cooldown=Fact(cd,Context(u,ability));facts.List.Add(cooldown);Refresh();Check(!ready.IsCasterRestrictionPassed(u),"Both variants share cooldown");Set(cooldown,"<IsTurnedOn>k__BackingField",false);Check(ready.IsCasterRestrictionPassed(u),"Inactive cooldown does not block");
  foreach(var token in new[]{"AlliedCloakAttack","AlliedCloakDefense"}){var bp=Asset<BlueprintAbility>(c,token);var actions=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.Cast<ContextActionApplyBuff>().ToArray();Check(actions.Length==2&&actions.All(x=>x.IsNotDispelable&&x.DurationValue.Calculate(Context(u,bp)).Seconds==TimeSpan.FromSeconds(6)),"Once-per-round aid and nondispellable cooldown");}
  Clear();
 }
 static void Smite(BlueprintsCache c,UnitEntityData u){
  var spell=Asset<BlueprintAbility>(c,"SmiteAbomination");var action=(PsychicSmiteApply)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();var bp=action.Mark.Get();var comp=bp.GetComponent<PsychicSmiteExclusion>();var other=new Psychic070Tests.Caster(c).Unit;
  Check(!bp.ComponentsArray.Any(x=>x.GetType().Name=="RemoveBuffOnLoad"||x is AddFactContextActions),"Timed mark survives load, no paladin gear AC branch");var ac=bp.GetComponent<Kingmaker.Designers.Mechanics.Buffs.ACBonusAgainstTarget>();Check(ac.Descriptor==ModifierDescriptor.Deflection&&ac.CheckCaster&&ac.Value.ValueShared==AbilitySharedValue.StatBonus,"Native target AC uses our shared stat, strictly deflection");
  foreach(int wis in new[]{6,10,20,30})foreach(int cha in new[]{6,10,24,40})foreach(int cl in new[]{1,10,20,35})foreach(bool ext in new[]{false,true}){
   Clear();foreach(var pair in new[]{Tuple.Create(StatType.Wisdom,wis),Tuple.Create(StatType.Charisma,cha)}){var stat=new ModifiableValueAttributeStat(u.Stats,pair.Item1);Set(stat,"m_BaseValue",pair.Item2);Set(stat,"<ModifiedValue>k__BackingField",pair.Item2);Set(u.Stats,pair.Item1.ToString(),stat);}
   var ctx=Context(u,spell,cl,ext?Metamagic.Extend:0);undead=true;var old=Fact(action.NativeSmite.Get(),ctx);var foreign=Fact(action.NativeSmite.Get(),Context(other,spell));facts.List.Add(old);facts.List.Add(foreign);Refresh();applied=removed=0;using(ctx.GetDataScope(u))action.RunAction();Check(applied==1&&removed==1&&!facts.List.Contains(old)&&facts.List.Contains(foreign),"Same-caster native mark replaced, foreign caster preserved");Check(ctx[AbilitySharedValue.StatBonus]==Math.Max(0,Math.Max((wis-10)/2,(cha-10)/2))&&ctx[AbilitySharedValue.DamageBonus]==Math.Min(20,cl),"Wis/Cha higher and CL cap");Check(application.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(Math.Min(cl,20)*6*(ext?2:1)),"Smite cap before Extend");
  }
  Clear();foreach(bool isUndead in new[]{false,true}){undead=isUndead;var ctx=Context(u,spell);applied=0;Check(spell.GetComponent<PsychicSmiteTarget>().IsTargetRestrictionPassed(u,new TargetWrapper(u))==isUndead,"Undead target restriction");using(ctx.GetDataScope(u))action.RunAction();Check(applied==(isUndead?1:0),"Execution rechecks undead");}
  foreach(bool own in new[]{false,true}){Clear();var ours=Fact(bp,Context(u,spell));facts.List.Add(ours);Refresh();var rt=Runtime(comp,ours);var native=Fact(action.NativeSmite.Get(),Context(own?u:other,spell));removed=0;using(rt.RequestEventContext())comp.HandleBuffDidAdded(native);Check(removed==(own?1:0),"Reverse native smite replaces same caster only");}Clear();
 }
 static bool Remove(Buff __instance){removed++;facts.List.Remove(__instance);Refresh();return false;}
 static bool Apply(ContextActionApplyBuff __instance){applied++;application=__instance;return false;}
 static bool Undead(ref bool __result){__result=undead;return false;}
}
