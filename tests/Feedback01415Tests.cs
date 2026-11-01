using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Kingmaker.Visual.Animation.Kingmaker;
using UnityEngine;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

internal static class Feedback01415Tests {
 static int checks,queries,animations,starts;
 static bool tb=true,legal=true,render;
 static UnitEntityData enemy;
 static UnitEntityData[] world;
 static UnitEntityView view;
 static UnitAnimationManager manager;
 static UnitAnimationActionHandle handle;
 static RulebookSubscriptionManager<IGlobalRulebookSubscriber> subscribers;
 static readonly List<TeamworkRangedReactionCommand> pending=new List<TeamworkRangedReactionCommand>();
 static readonly List<RuleAttackWithWeapon> shots=new List<RuleAttackWithWeapon>();
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Set(object o,string name,object value)=>(AccessTools.Field(o.GetType(),name)??AccessTools.Field(o.GetType(),"<"+name+">k__BackingField")).SetValue(o,value);
 static void Check(bool b,string message){if(!b)throw new Exception("Feedback01415: "+message);checks++;}
 static object Helper(string name,params object[] args)=>AccessTools.Method(typeof(TeamworkFeatTests),name).Invoke(null,args);
 static UnitEntityData Unit(BlueprintsCache c,string id)=>(UnitEntityData)Helper("Unit",c,id);
 static Feature Fact(UnitEntityData unit,BlueprintFeature bp)=>(Feature)Helper("Fact",unit,bp);
 static T Asset<T>(BlueprintsCache c,string id)where T:SimpleBlueprint=>Psychic034Tests.Asset<T>(c,id);
 static void Call(EntityFact fact,Type type,string name,params object[] args)=>Helper("Call",fact,type,name,args);
 static ItemEntityWeapon Weapon(UnitEntityData u,bool ranged,string id)=>(ItemEntityWeapon)Helper("Weapon",u,ranged,id);
 static void Equip(UnitEntityData unit,ItemEntityWeapon weapon)=>AccessTools.Method(typeof(WildShapeFeatTests),"Equip").Invoke(null,new object[]{unit.Body.PrimaryHand,weapon});
 static RuleAttackWithWeapon Attack(UnitEntityData u,UnitEntityData t,ItemEntityWeapon weapon)=>(RuleAttackWithWeapon)Helper("Attack",u,t,weapon,false,false);
 static bool Targets(ref IEnumerable<UnitEntityData> __result){queries++;__result=world;return false;}
 static bool Ally(UnitEntityData __instance,UnitEntityData __0,ref bool __result){__result=(__instance==enemy)==(__0==enemy);return false;}
 static bool CanAttack(ref bool __result){__result=legal;return false;}
 static bool Yes(ref bool __result){__result=true;return false;}
 static bool No(ref bool __result){__result=false;return false;}
 static bool Skip()=>false;
 static bool Mode(ref bool __result){__result=tb;return false;}
 static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}
 static bool Delta(ref float __result){__result=0;return false;}
 static bool Hit(RuleAttackRoll __instance,ref bool __result){__result=!__instance.AutoMiss;return false;}
 static bool View(ref UnitEntityView __result){__result=render?view:null;return false;}
 static bool Manager(ref UnitAnimationManager __result){__result=manager;return false;}
 static bool NotEqual(UnityEngine.Object __0,UnityEngine.Object __1,ref bool __result){if(!ReferenceEquals(__0,manager)&&!ReferenceEquals(__1,manager))return true;__result=!ReferenceEquals(__0,__1);return false;}
 static bool CreateAnimation(AttackHandInfo __instance){
  animations++;Check(__instance.Hand.MaybeWeapon.Blueprint.IsRanged&&__instance.Target==enemy,"native hand animation uses ready ranged weapon and target");
  handle=Empty<UnitAnimationActionHandle>();Set(__instance,"AnimationHandle",handle);return false;
 }
 static bool StartAnimation(UnitCommand __instance,UnitAnimationActionHandle __0){starts++;Set(__instance,"Animation",__0);Set(__instance,"HasAnimation",true);return false;}
 sealed class Busy:UnitAttackOfOpportunity {internal Busy(UnitEntityData t):base(t){} public override bool IsInterruptible=>false;}
 static void Queue(UnitEntityData __0,TeamworkRangedReactionCommand __1){
  pending.Add(__1);Check(__0.Commands.Queue.First.Value==__1&&__1.Executor==__0,"native priority queue retains initialized reaction");
 }
 static bool Reaction(RuleAttackWithWeapon __0,ref RuleAttackWithWeapon __result){
  Check(__0.ExtraAttack&&__0.IsFirstAttack&&__0.AttacksCount==1&&!__0.SkipAnimation&&!__0.IsAttackOfOpportunity,"one normal ranged reaction; no opportunity cost");
  Check(__0.Initiator.CombatState.Cooldown.SwiftAction==6,"reserved swift pool stays paid at shot time");
  shots.Add(__0);Set(__0,"AttackRoll",Attack(__0.Initiator,__0.Target,__0.Weapon).AttackRoll);
  subscribers.OnEventAboutToTrigger(__0);subscribers.OnEventDidTrigger(__0);__result=__0;return false;
 }
 static void Register(params EntityFact[] facts){
  subscribers=new RulebookSubscriptionManager<IGlobalRulebookSubscriber>();
  foreach(var fact in facts)foreach(var runtime in fact.Components)if(runtime.SourceBlueprintComponent is IGlobalRulebookSubscriber s)subscribers.Subscribe(s,(ISubscriptionProxy)runtime);
 }
 static void Dispatch(RuleAttackWithWeapon rule)=>subscribers.OnEventDidTrigger(rule);
 static void Fire(){
  foreach(var command in pending.ToArray()){
   AccessTools.Method(typeof(TeamworkRangedReactionCommand),"OnAction").Invoke(command,null);
   if(command.Executor.Commands.Free==command)command.Executor.Commands.Raw[(int)UnitCommand.CommandType.Free]=null;
   command.Executor.Commands.Queue.Remove(command);pending.Remove(command);
  }
 }
 static void Reset(UnitEntityData unit){unit.CombatState.Cooldown.SwiftAction=0;unit.Commands.Raw[(int)UnitCommand.CommandType.Free]=null;unit.Commands.Queue.Clear();}
 static TurnController Turn(UnitEntityData u)=>(TurnController)AccessTools.Method(typeof(Gambit01414Tests),"Turn").Invoke(null,new object[]{u,null});
 static void Swarm(BlueprintsCache cache){
  enemy=Unit(cache,"01415-hostile");var holders=Enumerable.Range(0,6).Select(i=>Unit(cache,"01415-swarm-"+i)).ToArray();
  var distant=Enumerable.Range(0,32).Select(i=>Unit(cache,"01415-distant-"+i)).ToArray();
  foreach(var u in distant)Set(u,"m_Position",new Vector3(40,0,0));world=holders.Concat(distant).ToArray();
  for(int i=0;i<6;i++)Set(holders[i],"m_Position",new Vector3(i*.2f,0,0));
  var swarm=Asset<BlueprintFeature>(cache,"SwarmScatterFeat");var dummy=Asset<BlueprintFeature>(cache,"BondedMindFeat");
  foreach(var u in holders)for(int i=0;i<64;i++)Fact(u,dummy);
  var facts=holders.Select(u=>Fact(u,swarm)).ToArray();
  foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"OnTurnOn");
  Check(holders.All(u=>u.Stats.AC.ModifiedValue==15),"six holders start with five independent neighbors");
  var references=holders.Select((u,i)=>u.Stats.AC.Modifiers.Single(m=>m.Source==facts[i])).ToArray();queries=0;
  for(int eventIndex=0;eventIndex<1000;eventIndex++)foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"HandleEntityPositionChanged",distant[eventIndex%32]);
  Check(queries==0,"6000 unrelated movement callbacks do zero world queries");
  Check(holders.Select((u,i)=>ReferenceEquals(references[i],u.Stats.AC.Modifiers.Single(m=>m.Source==facts[i]))).All(b=>b),"irrelevant movement does not recreate any AC modifier");
  queries=0;var timer=Stopwatch.StartNew();
  for(int e=0;e<360;e++){var moved=holders[e%6];Set(moved,"m_Position",new Vector3((e%6)*.2f,0,(e%2)*.01f));foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"HandleEntityPositionChanged",moved);}
  timer.Stop();double fast=timer.Elapsed.TotalMilliseconds;int optimizedQueries=queries;
  Check(queries==360,"simultaneous-holder movement queries once per moved owner, not once per subscriber");
  Check(holders.Select((u,i)=>ReferenceEquals(references[i],u.Stats.AC.Modifiers.Single(m=>m.Source==facts[i]))).All(b=>b),"movement inside adjacency keeps exact modifier instances");
  // Frozen 0.14.14 used this unchanged method for every movement subscriber.
  var old=AccessTools.Method(typeof(SwarmScatterDefense).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkFeats"),"Adjacent");queries=0;timer.Restart();
  for(int e=0;e<360;e++)foreach(var u in holders)Check((int)old.Invoke(null,new object[]{u})==5,"baseline/current adjacency result agrees");
  timer.Stop();double baseline=timer.Elapsed.TotalMilliseconds;
  Check(queries==2160,"legacy movement has six neighborhood scans per event");
  PsychicExpansionTests.Inventory["swarm01415Performance"]=new JObject{["holders"]=6,["factsPerHolder"]=65,["worldUnits"]=world.Length,["movementEvents"]=360,["newWorldQueries"]=optimizedQueries,["oldWorldQueries"]=queries,["newMilliseconds"]=fast,["oldMilliseconds"]=baseline,["fixtureIncludesReflectionOverhead"]=true};
  Set(holders[5],"m_Position",new Vector3(20,0,0));foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"HandleEntityPositionChanged",holders[5]);
  Check(holders.Take(5).All(u=>u.Stats.AC.ModifiedValue==14)&&holders[5].Stats.AC.ModifiedValue==10,"separation instantly drops every affected AC and clears moved owner's modifier");
  Set(holders[5],"m_Position",Vector3.zero);foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"HandleEntityPositionChanged",holders[5]);Check(holders.All(u=>u.Stats.AC.ModifiedValue==15),"rejoining restores exact count once");
  Set(facts[5],"IsTurnedOn",false);foreach(var f in facts.Take(5))Call(f,typeof(SwarmScatterDefense),"HandleEntityPositionChanged",holders[5]);Check(holders[0].Stats.AC.ModifiedValue==14,"disabled moved partner immediately stops contributing");
  holders[0].State.Features.SoloTactics.Retain();Call(facts[0],typeof(SwarmScatterDefense),"OnNewRound");Check(holders[0].Stats.AC.ModifiedValue==15,"Solo Tactics still accepts adjacent ally without active feat");holders[0].State.Features.SoloTactics.Release();
  holders[0].Stats.AC.AddModifier(9,facts[0],Kingmaker.Enums.ModifierDescriptor.Circumstance);Call(facts[0],typeof(SwarmScatterDefense),"OnApplyPostLoadFixes");Check(holders[0].Stats.AC.ModifiedValue==14&&holders[0].Stats.AC.Modifiers.Count(m=>m.Source==facts[0])==1,"load reconciliation removes stale duplicates");
  var data=facts[0].Components.Single(r=>r.SourceBlueprintComponent is SwarmScatterDefense).GetData<SwarmScatterData>();Check(JsonConvert.SerializeObject(data)=="{}","neighbor cache and modifier references are excluded from saves");
  foreach(var f in facts)Call(f,typeof(SwarmScatterDefense),"OnTurnOff");
 }
 static void Reactions(BlueprintsCache c,Harmony harmony){
  var coop=typeof(BrokenWingGambit).Assembly.GetType("CallOfTheWild.Wrath.Prototype.CooperationFeats");AccessTools.Method(coop,"Register").Invoke(null,new object[]{c});
  var bait=Unit(c,"01415-bait");enemy=Unit(c,"01415-enemy");var archer=Unit(c,"01415-both");var second=Unit(c,"01415-second");var shooter=Unit(c,"01415-trigger");world=new[]{bait,archer,second,shooter};
  var wing=Asset<BlueprintFeature>(c,"BrokenWingGambitFeat");var paw=Asset<BlueprintFeature>(c,"WoundedPawGambitFeat");var opportunity=Asset<BlueprintFeature>(c,"TargetOfOpportunityFeat");
  var baitFact=Fact(bait,wing);var firstFact=Fact(archer,wing);var secondFact=Fact(second,wing);Fact(archer,paw);Fact(second,paw);Fact(archer,opportunity);Fact(second,opportunity);Fact(shooter,opportunity);
  Psychic034Tests.Fact(bait,Asset<BlueprintBuff>(c,"BrokenWingEnabled"),bait,10);
  var pawBuff=Psychic034Tests.Fact(archer,Asset<BlueprintBuff>(c,"WoundedPawEnabled"),archer,10);Psychic034Tests.Fact(second,Asset<BlueprintBuff>(c,"WoundedPawEnabled"),second,10);
  var targetBuff=Psychic034Tests.Fact(archer,Asset<BlueprintBuff>(c,"TargetOpportunityEnabled"),archer,10);var secondTarget=Psychic034Tests.Fact(second,Asset<BlueprintBuff>(c,"TargetOpportunityEnabled"),second,10);
  var sword=Weapon(bait,false,"01415-sword");var enemySword=Weapon(enemy,false,"01415-enemy-sword");var bow=Weapon(archer,true,"01415-bow");var bow2=Weapon(second,true,"01415-bow2");Equip(archer,bow);Equip(second,bow2);Set(enemy,"m_Position",new Vector3(2,0,0));Set(archer,"m_Position",new Vector3(3,0,0));Set(second,"m_Position",new Vector3(4,0,0));
  harmony.CreateClassProcessor(coop.Assembly.GetType("CallOfTheWild.Wrath.Prototype.GambitUnusedSwiftPatch")).Patch();
  harmony.CreateClassProcessor(coop.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkRangedReactionTurnPatch")).Patch();
  for(int round=0;round<4;round++){
   Reset(archer);Reset(second);Register(round%2==0?new EntityFact[]{baitFact,firstFact,secondFact,targetBuff,secondTarget}:new EntityFact[]{secondTarget,targetBuff,secondFact,firstFact,baitFact});
   Turn(archer).ForceToEnd(true);Turn(second).ForceToEnd(true);Check(archer.CombatState.Cooldown.SwiftAction==0&&second.CombatState.Cooldown.SwiftAction==0,"both toggles preserve unused pool after native end turn");
   int before=shots.Count;
   if(round%2==0){Dispatch(Attack(bait,enemy,sword));Dispatch(Attack(enemy,bait,enemySword));}
   else Dispatch(Attack(shooter,enemy,bow));
   Check(pending.Count==2&&shots.Count==before,"two independent responses queue with both toggles, before animated release");
   foreach(var command in pending)Check(command.Type==UnitCommand.CommandType.Free&&command.ReactionAction&&!command.IsOneFrameCommand&&!command.ShouldSkipAnimation,"animated off-turn free command does not use ordinary actions");
   Dispatch(Attack(shooter,enemy,bow));Dispatch(Attack(bait,enemy,sword));Dispatch(Attack(enemy,bait,enemySword));Check(pending.Count==2,"later competing trigger cannot spend already reserved pools");
   Fire();Check(shots.Count==before+2&&shots.Skip(before).Select(s=>s.Initiator).Distinct().Count()==2&&pending.Count==0,"each archer fires once; reaction hit cannot recursively trigger either feat");
  }
  Reset(archer);Reset(second);Set(pawBuff,"IsTurnedOn",false);Set(secondTarget,"IsTurnedOn",false);Register(baitFact,firstFact,secondFact,targetBuff,secondTarget);
  Dispatch(Attack(shooter,enemy,bow));Check(pending.Count==1&&pending[0].Executor==archer,"Target remains independently enabled when Wounded preference is off");Fire();
  Reset(archer);Set(targetBuff,"IsTurnedOn",false);Set(pawBuff,"IsTurnedOn",true);Dispatch(Attack(bait,enemy,sword));Dispatch(Attack(enemy,bait,enemySword));Check(pending.Any(p=>p.Executor==archer),"Wounded remains independently enabled when Target consent is off");Fire();
  Reset(archer);Set(targetBuff,"IsTurnedOn",true);Set(pawBuff,"IsTurnedOn",false);Turn(archer).ForceToEnd(true);Check(archer.CombatState.Cooldown.SwiftAction==0,"Target-only consent retains unused off-turn swift pool");
  Reset(archer);Reset(second);legal=false;Dispatch(Attack(shooter,enemy,bow));Check(pending.Count==0&&archer.CombatState.Cooldown.SwiftAction==0,"invalid target does not consume shared pool");legal=true;
  var busy=new Busy(enemy);busy.Init(archer);archer.Commands.Raw[(int)UnitCommand.CommandType.Free]=busy;
  var planned=new UnitAttack(enemy);Set(planned,"Executor",archer);archer.Commands.Queue.AddLast(planned);
  Dispatch(Attack(shooter,enemy,bow));Check(pending.Count==1&&archer.Commands.Free==busy&&archer.Commands.Queue.Contains(pending[0])&&archer.Commands.Queue.Contains(planned),"busy reaction slot and planned normal attack survive priority reaction insertion");
  var queued=pending[0];archer.Commands.Raw[(int)UnitCommand.CommandType.Free]=queued;archer.Commands.Queue.Remove(queued);pending.Clear();
  Check((bool)AccessTools.Method(typeof(UnitCommandController),"TickCommandTurnBased").Invoke(new UnitCommandController(),new object[]{queued}),"off-turn reaction passes actual native turn gate between turns");
  using(Game.Instance.TurnBasedCombatController.WaitingForUI.Scope())Check(!(bool)AccessTools.Method(typeof(UnitCommandController),"TickCommandTurnBased").Invoke(new UnitCommandController(),new object[]{queued}),"native UI-wait guard still pauses the ranged reaction");
  var blocking=new UnitAttack(enemy);Set(blocking,"Executor",archer);Set(blocking,"IsStarted",true);var blockingAnimation=Empty<UnitAnimationActionHandle>();Set(blocking,"Animation",blockingAnimation);archer.Commands.Raw[(int)UnitCommand.CommandType.Standard]=blocking;
  Check(!queued.CanStart,"running ordinary weapon animation blocks reaction start without replacement");Set(blockingAnimation,"IsFinished",true);Check(queued.CanStart,"reaction can start as soon as ordinary animation finishes");archer.Commands.Raw[(int)UnitCommand.CommandType.Standard]=null;
  render=true;Set(queued,"IsStarted",true);AccessTools.Method(typeof(TeamworkRangedReactionCommand),"OnStart").Invoke(queued,null);
  Check(animations==1&&starts==1&&queued.Animation==handle,"actual reaction OnStart constructs native hand animation and starts it");
  int shotsBefore=shots.Count;queued.Tick();Check(shots.Count==shotsBefore&&!queued.IsActed,"native command Tick waits before animation act frame");
  handle.IsActed=true;queued.Tick();Check(shots.Count==shotsBefore+1&&queued.IsActed,"native Tick releases exactly one shot at act frame");queued.Tick();Check(shots.Count==shotsBefore+1,"repeated native Tick cannot emit duplicate shot");
  Check(queued.Animation.AttackTargetDistance==1f,"ranged animation tracks the actual target distance");
  render=false;Reset(archer);Dispatch(Attack(shooter,enemy,bow));var cancel=pending.Single();Set(enemy,"m_Position",new Vector3(50,0,0));Fire();Check(shots.Count==shotsBefore+1&&archer.CombatState.Cooldown.SwiftAction==6,"target leaves range while queued: no delayed illegal shot and no refund");
  tb=false;Set(enemy,"m_Position",new Vector3(2,0,0));Reset(archer);Set(pawBuff,"IsTurnedOn",true);
  Dispatch(Attack(shooter,enemy,bow));Check(pending.Count==1,"both toggles also admit Target reaction in real time");Fire();
  Reset(archer);Dispatch(Attack(bait,enemy,sword));Dispatch(Attack(enemy,bait,enemySword));Check(pending.Any(p=>p.Executor==archer),"both toggles also admit Wounded reaction in real time");Fire();tb=true;
  Check(AccessTools.Field(typeof(TeamworkRangedReactionCommand),"m_Context").IsDefined(typeof(JsonPropertyAttribute),false)&&AccessTools.Field(typeof(TeamworkRangedReactionCommand),"m_Resolved").IsDefined(typeof(JsonPropertyAttribute),false),"pending reaction context and duplicate guard have save fields");
 }
 internal static int Run(BlueprintsCache cache){
  checks=queries=animations=starts=0;tb=true;legal=true;render=false;pending.Clear();shots.Clear();view=Empty<UnitEntityView>();manager=Empty<UnitAnimationManager>();
  var oldTime=Game.Instance.TimeController;var oldCombat=Game.Instance.TurnBasedCombatController;Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());Set(Game.Instance,"TurnBasedCombatController",Empty<CombatController>());Set(Game.Instance.TurnBasedCombatController,"WaitingForUI",new Kingmaker.Utility.CountingGuard());
  var h=new Harmony("CotW01415.Feedback");
  void Patch(System.Reflection.MethodBase m,string name)=>h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01415Tests),name));
  Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"GetTargetsAround",new[]{typeof(Vector3),typeof(float),typeof(bool),typeof(bool)}),nameof(Targets));
  Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Ally));Patch(AccessTools.Method(typeof(UnitEntityData),"CanAttack",new[]{typeof(UnitEntityData)}),nameof(CanAttack));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),nameof(Hit));
  Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(Mode));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"DeltaTime"),nameof(Delta));
  Patch(AccessTools.PropertyGetter(typeof(EntityDataBase),"IsInState"),nameof(Yes));Patch(AccessTools.DeclaredProperty(typeof(UnitEntityData),"View").GetGetMethod(),nameof(View));Patch(AccessTools.DeclaredProperty(typeof(UnitEntityView),"AnimationManager").GetGetMethod(),nameof(Manager));
  Patch(AccessTools.Method(typeof(UnityEngine.Object),"op_Inequality"),nameof(NotEqual));Patch(AccessTools.Method(typeof(AttackHandInfo),"CreateAnimationHandleForAttack"),nameof(CreateAnimation));Patch(AccessTools.Method(typeof(UnitCommand),"StartAnimation",new[]{typeof(UnitAnimationActionHandle)}),nameof(StartAnimation));
  h.Patch(AccessTools.Method(typeof(TeamworkRangedReactionCommand).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkRangedReaction"),"Enqueue"),postfix:new HarmonyMethod(typeof(Feedback01415Tests),nameof(Queue)));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleAttackWithWeapon),AccessTools.Method(typeof(Feedback01415Tests),nameof(Reaction)));
  try{Swarm(cache);Reactions(cache,h);PsychicExpansionTests.Inventory["feedback01415Assertions"]=checks;return checks;}
  finally{h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",oldTime);Set(Game.Instance,"TurnBasedCombatController",oldCombat);pending.Clear();world=Array.Empty<UnitEntityData>();}
 }
}
