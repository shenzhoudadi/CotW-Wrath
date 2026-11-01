using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using TurnBased.Controllers;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Execute the native flanking and opportunity eligibility getters. Mode and
// consciousness are bounded inputs; dictionaries model engine engagements.
// Resistance resolution and removal are captured, not a Unity combat simulation.
internal static class Psychic035Tests {
 static int n,sr,removed;static bool turnBased,resisted;static UnitState disabled;
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);
 static void Set(object o,string f,object v)=>AccessTools.Field(o.GetType(),f).SetValue(o,v);
 static void Check(bool b,string m){if(!b)throw new Exception("035: "+m);n++;}
 static bool Mode(ref bool __result){__result=turnBased;return false;}
 static bool False(ref bool __result){__result=false;return false;}
 static bool CanAct(UnitState __instance,ref bool __result){__result=__instance!=disabled;return false;}
 static bool SR(){sr++;return false;}
 static bool Resisted(ref bool __result){__result=resisted;return false;}
 static bool Remove(){removed++;return false;}
 static UnitEntityData Unit(BlueprintsCache c,string id,string token){
  var u=Psychic034Tests.Unit(c,id).Unit;var bp=Psychic034Tests.Asset<BlueprintUnit>(c,token+"Unit");
  Set(u.Descriptor,"<Blueprint>k__BackingField",bp);Set(u.Descriptor,"<OriginalBlueprint>k__BackingField",bp);
  Set(u,"<CombatState>k__BackingField",Empty<UnitCombatState>());Set(u.CombatState,"Unit",u);Set(u.CombatState,"m_InCombat",true);
  Set(u.CombatState,"m_EngagedBy",new Dictionary<UnitEntityData,TimeSpan>());Set(u.CombatState,"m_EngagedUnits",new Dictionary<UnitEntityData,TimeSpan>());
  Set(u,"<Commands>k__BackingField",new UnitCommands(u));Set(u,"Passive",new ObservableCountFlag());
  Set(u.State,"m_Conditions",new sbyte[128]);Set(u.State,"m_ConditionsImmunity",new sbyte[128]);Set(u.State,"m_ConditionsExceptions",new List<UnitConditionExceptions>[128]);
  Set(u.State.Features,"CannotBeFlanked",new CountableFlag());
  Set(u.CombatState,"m_AttackOfOpportunityCount",1);return u;
 }
 static Dictionary<UnitEntityData,TimeSpan> Engaged(UnitEntityData u)=>(Dictionary<UnitEntityData,TimeSpan>)Get(u.CombatState,"m_EngagedBy");
 static void Target(UnitEntityData u,UnitEntityData target){
  var cmd=Empty<UnitAttack>();Set(cmd,"<Target>k__BackingField",new TargetWrapper(target));u.Commands.Raw[0]=cmd;
 }
 internal static int Run(BlueprintsCache c){
  var h=new Harmony("CotW035.AllyNativeFlanking");
  void Patch(System.Reflection.MethodBase m,string method)=>h.Patch(m,prefix:new HarmonyMethod(typeof(Psychic035Tests),method));
  Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(Mode));
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),nameof(False));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(CanAct));
  Patch(AccessTools.Method(typeof(RuleSpellResistanceCheck),"OnTrigger"),nameof(SR));
  Patch(AccessTools.PropertyGetter(typeof(RuleSpellResistanceCheck),"IsSpellResisted"),nameof(Resisted));
  Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));
  var production=typeof(PsychicSpiritBind).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpiritualAllyFlankingPatch");
  try{
   var caster=Unit(c,"035-caster","ArcaneEye");var ally=Unit(c,"035-ally","SpiritualAlly");var enemy=Unit(c,"035-enemy","ArcaneEye");var fighter=Unit(c,"035-fighter","ArcaneEye");
   var host=Psychic034Tests.Fact(caster,Psychic034Tests.Asset<BlueprintBuff>(c,"SpiritualAllyController"),caster,15);
   var rt=host.Components.Single(x=>x.SourceBlueprintComponent is PsychicSpiritController);var data=rt.GetData<PsychicSpiritState>();data.Summon=ally;data.Level=15;data.Target=enemy;data.NextAttack=TimeSpan.FromSeconds(120);
   var actor=Psychic034Tests.Fact(ally,Psychic034Tests.Asset<BlueprintBuff>(c,"SpiritualAllyActorBuff"),caster,15);var art=actor.Components.Single(x=>x.SourceBlueprintComponent is PsychicSpiritAttackStats);var component=(PsychicSpiritAttackStats)art.SourceBlueprintComponent;
   Target(fighter,enemy);Engaged(enemy).Add(ally,TimeSpan.Zero);Engaged(enemy).Add(fighter,TimeSpan.Zero);
   turnBased=false;Check(!enemy.CombatState.IsFlanked,"old real-time getter omits the commandless spell ally");
   h.CreateClassProcessor(production).Patch();
   Check(enemy.CombatState.IsFlanked,"patched native getter counts live engaged Ally alongside a normal attacker");
   Engaged(enemy).Remove(fighter);Check(!enemy.CombatState.IsFlanked,"one ally alone does not flank");Engaged(enemy).Add(fighter,TimeSpan.Zero);
   enemy.State.Features.CannotBeFlanked.Retain();Check(!enemy.CombatState.IsFlanked,"native flanking immunity wins");enemy.State.Features.CannotBeFlanked.Release();
   actor.IsSuppressed=true;Check(!enemy.CombatState.IsFlanked,"suppressed actor cannot contribute");actor.IsSuppressed=false;
   host.IsSuppressed=true;Check(!enemy.CombatState.IsFlanked,"suppressed spell host cannot contribute");host.IsSuppressed=false;
   data.Summon=fighter;Check(!enemy.CombatState.IsFlanked,"stale summon identity cannot contribute");data.Summon=ally;
   disabled=ally.State;Check(!enemy.CombatState.IsFlanked,"incapacitated ally cannot contribute");disabled=null;
   Set(enemy.CombatState,"m_InCombat",false);Check(!enemy.CombatState.IsFlanked,"no new flanking outside combat");Set(enemy.CombatState,"m_InCombat",true);
   turnBased=true;Check(enemy.CombatState.IsFlanked,"turn-based native two-engagement flanking retained");turnBased=false;
   var allyFeature=Psychic034Tests.Asset<BlueprintUnit>(c,"SpiritualAllyUnit").AddFacts.Single();
   Check(!allyFeature.GetComponents<AddCondition>().Any(x=>x.Condition==UnitCondition.DisableAttacksOfOpportunity),"Ally no longer installs the prohibiting condition");
   Check(ally.CombatState.CanAttackOfOpportunity,"native opportunity eligibility with one remaining attack");
   var conditions=(sbyte[])Get(ally.State,"m_Conditions");conditions[(int)UnitCondition.DisableAttacksOfOpportunity]=1;
   Check(!ally.CombatState.CanAttackOfOpportunity,"old disabling condition reproduces the opportunity failure");conditions[(int)UnitCondition.DisableAttacksOfOpportunity]=0;
   Set(ally.CombatState,"m_AttackOfOpportunityCount",0);Check(!ally.CombatState.CanAttackOfOpportunity,"spent native opportunity budget still blocks");Set(ally.CombatState,"m_AttackOfOpportunityCount",1);
   foreach(string token in new[]{"SpiritualWeapon","SpiritualAlly","MagesSword","TwilightKnife"}){
    var bp=Psychic034Tests.Asset<BlueprintBuff>(c,token+"ActorBuff");Check(bp.FxOnStart.AssetId=="fdc7f8f37d3f8da42be2a1d35a617001","each spirit uses the audited native looping effect: "+token);
    if(token!="SpiritualAlly")Check(Psychic034Tests.Asset<BlueprintUnit>(c,token+"Unit").AddFacts.Single().GetComponents<AddCondition>().Any(x=>x.Condition==UnitCondition.DisableAttacksOfOpportunity),"other spells retain opportunity restriction: "+token);
   }
   RuleAttackRoll Roll(UnitEntityData target,bool opportunity){var roll=Empty<RuleAttackRoll>();Set(roll,"Initiator",ally);Set(roll,"Target",target);roll.RuleAttackWithWeapon=Empty<RuleAttackWithWeapon>();roll.RuleAttackWithWeapon.IsAttackOfOpportunity=opportunity;return roll;}
   var a=Roll(enemy,true);resisted=false;sr=0;
   using(art.RequestEventContext())component.OnEventAboutToTrigger(a);
   Check(sr==1&&!a.AutoMiss&&data.ResistancePassed.Contains(enemy.UniqueId),"first opportunity shares spell SR resolution and cache");
   using(art.RequestEventContext())component.OnEventAboutToTrigger(Roll(enemy,true));Check(sr==1,"same opponent does not reroll passed SR");
   Check(data.NextAttack==TimeSpan.FromSeconds(120)&&data.Target==enemy,"opportunity does not spend or redirect scheduled attacks");
   var b=Roll(fighter,true);resisted=true;removed=0;using(art.RequestEventContext())component.OnEventAboutToTrigger(b);
   Check(sr==2&&removed==1&&b.AutoMiss&&!data.ResistancePassed.Contains(fighter.UniqueId),"resisted opportunity cannot hit and ends only this casting");
   var ordinary=Roll(fighter,false);using(art.RequestEventContext())component.OnEventAboutToTrigger(ordinary);Check(sr==2&&!ordinary.AutoMiss,"scheduled attack keeps controller handling");
  }finally{h.UnpatchAll(h.Id);disabled=null;turnBased=false;}
  PsychicExpansionTests.Inventory["repair035Assertions"]=n;Console.WriteLine("PASS: "+n+" 035 native flanking/opportunity eligibility and spell-SR checks; engagement dictionaries and mode/scene inputs bounded.");return n;
 }
}
