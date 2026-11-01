using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Combat;
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
using UnityEngine;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

internal static class Gambit01414Tests {
 static readonly Type Module=typeof(BrokenWingGambit).Assembly.GetType("CallOfTheWild.Wrath.Prototype.CooperationFeats");
 static int checks,aoo,opportunityRules;
 static bool tb,passing,legal,reach,queueAoO,deferAoO;
 static float delta;
 static TimeSpan now;
 static UnitEntityData enemy,bait;
 static UnitEntityData[] world;
 static RulebookSubscriptionManager<IGlobalRulebookSubscriber> subscribers;
 static readonly List<RuleAttackWithWeapon> shots=new List<RuleAttackWithWeapon>();
 static readonly List<RuleAttackWithWeapon> meleeReactions=new List<RuleAttackWithWeapon>();
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string text){if(!b)throw new Exception("Gambit01414: "+text);checks++;}
 static object Helper(string method,params object[] args)=>AccessTools.Method(typeof(TeamworkFeatTests),method).Invoke(null,args);
 static T Asset<T>(BlueprintsCache cache,string token)where T:SimpleBlueprint=>Psychic034Tests.Asset<T>(cache,token);
 static UnitEntityData Unit(BlueprintsCache c,string id){var unit=(UnitEntityData)Helper("Unit",c,id);Set(unit.Descriptor,"Stats",new CharacterStats(unit.Descriptor));return unit;}
 static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)Helper("Fact",u,bp);
 static RuleAttackWithWeapon Attack(UnitEntityData u,UnitEntityData t,ItemEntityWeapon weapon,bool miss=false)=>(RuleAttackWithWeapon)Helper("Attack",u,t,weapon,false,miss);
 static ItemEntityWeapon Weapon(UnitEntityData u,bool ranged,string id)=>(ItemEntityWeapon)Helper("Weapon",u,ranged,id);
 static GambitData Data(Feature fact)=>fact.Components.Single(r=>r.SourceBlueprintComponent is BrokenWingGambit).GetData<GambitData>();
 static void ClearActions(UnitEntityData unit)=>AccessTools.Method(typeof(UnitCombatState.Cooldowns),"Clear").Invoke(unit.CombatState.Cooldown,null);
 static void Equip(UnitEntityData unit,ItemEntityWeapon weapon)=>AccessTools.Method(typeof(WildShapeFeatTests),"Equip").Invoke(null,new object[]{unit.Body.PrimaryHand,weapon});
 static void Register(params Feature[] facts){
  subscribers=new RulebookSubscriptionManager<IGlobalRulebookSubscriber>();
  foreach(var fact in facts){var runtime=fact.Components.Single(r=>r.SourceBlueprintComponent is BrokenWingGambit);subscribers.Subscribe((IGlobalRulebookSubscriber)runtime.SourceBlueprintComponent,(ISubscriptionProxy)runtime);}
 }
 static int Bonus(UnitEntityData attacker,UnitEntityData target,ItemEntityWeapon weapon){var rule=new RuleCalculateAttackBonus(attacker,target,weapon,0);subscribers.OnEventAboutToTrigger(rule);return rule.AllBonuses.Sum(m=>m.Value);}
 static int Damage(UnitEntityData attacker,UnitEntityData target,ItemEntityWeapon weapon){var attack=new RuleAttackWithWeapon(attacker,target,weapon,0);subscribers.OnEventAboutToTrigger(attack.WeaponStats);return attack.WeaponStats.DamageModifiers.Modifiers.Sum(m=>m.Value);}
 static void Accept(UnitEntityData target,ItemEntityWeapon weapon,bool miss=false)=>subscribers.OnEventDidTrigger(Attack(enemy,target,weapon,miss));
 static void Offer(UnitEntityData owner,ItemEntityWeapon weapon)=>subscribers.OnEventDidTrigger(Attack(owner,enemy,weapon));
 static TurnController Turn(UnitEntityData rider,UnitEntityData mount=null){
  var turn=Empty<TurnController>();Set(turn,"m_Rider",(UnitReference)rider);Set(turn,"m_Mount",(UnitReference)mount);
  Set(turn,"m_RiderCooldown",rider.CombatState.Cooldown);Set(turn,"m_MountCooldown",mount?.CombatState.Cooldown);
  var movement=AccessTools.Inner(typeof(TurnController),"MovementStats");
  Set(turn,"m_RiderMovementStats",Activator.CreateInstance(movement,true));
  if(mount!=null)Set(turn,"m_MountMovementStats",Activator.CreateInstance(movement,true));
  return turn;
 }
 static void Tick(params UnitEntityData[] units){var controller=new UnitCombatCooldownsController();foreach(var unit in units)AccessTools.Method(typeof(UnitCombatCooldownsController),"TickOnUnit").Invoke(controller,new object[]{unit});now+=TimeSpan.FromSeconds(delta);}
 static bool Mode(ref bool __result){__result=tb;return false;}
 static bool Passing(ref bool __result){__result=passing;return false;}
 static bool Time(ref TimeSpan __result){__result=now;return false;}
 static bool Delta(ref float __result){__result=delta;return false;}
 static bool Yes(ref bool __result){__result=true;return false;}
 static bool No(ref bool __result){__result=false;return false;}
 static bool Skip()=>false;
 static bool Ally(UnitEntityData __instance,UnitEntityData __0,ref bool __result){__result=(__instance==enemy)==(__0==enemy);return false;}
 static bool CanAttack(ref bool __result){__result=legal;return false;}
 static bool Reach(ref bool __result){__result=reach;return false;}
 sealed class BusyOpportunity:UnitAttackOfOpportunity {
  internal BusyOpportunity(UnitEntityData target):base(target){}
  public override bool IsInterruptible=>false;
 }
 static UnitAttackOfOpportunity Opportunity(UnitEntityData unit,UnitEntityData target,bool run=true){
  var command=new UnitAttackOfOpportunity(target);Set(command,"Executor",unit);Set(command,"Hand",unit.Body.PrimaryHand);if(run)unit.Commands.Raw[(int)UnitCommand.CommandType.Free]=command;return command;
 }
 static bool Aoo(UnitCombatState __instance,UnitEntityData __0,ref bool __result){
  aoo++;
  if(queueAoO){
   var command=Opportunity(__instance.Unit,__0,!deferAoO);
   if(deferAoO)Check((bool)AccessTools.Method(typeof(UnitCommands),"TryAddToQueueInsteadOfRunImmediately").Invoke(__instance.Unit.Commands,new object[]{command}),"native command scheduler queues behind an uninterruptible action");
  }
  __result=true;return false;
 }
 static bool Hit(RuleAttackRoll __instance,ref bool __result){__result=!__instance.AutoMiss;return false;}
 static bool Targets(ref IEnumerable<UnitEntityData> __result){__result=world;return false;}
 static bool Reaction(RuleAttackWithWeapon __0,ref RuleAttackWithWeapon __result){
  if(__0.IsAttackOfOpportunity){
   meleeReactions.Add(__0);Set(__0,"AttackRoll",Attack(__0.Initiator,__0.Target,__0.Weapon).AttackRoll);
   subscribers.OnEventAboutToTrigger(__0);subscribers.OnEventAboutToTrigger(__0.WeaponStats);subscribers.OnEventDidTrigger(__0);__result=__0;return false;
  }
  Check(__0.ExtraAttack&&__0.IsFirstAttack&&__0.AttacksCount==1&&!__0.SkipAnimation,"reaction uses one native animated ranged attack");
  Check(__0.Initiator.CombatState.Cooldown.SwiftAction==6f,"native swift payment precedes each independent response");
  Check(__0.Reason.Context.MaybeCaster==bait,"nested global dispatch retains accepting owner's mechanics context");
  shots.Add(__0);Set(__0,"AttackRoll",Attack(__0.Initiator,__0.Target,__0.Weapon).AttackRoll);
  subscribers.OnEventAboutToTrigger(__0.WeaponStats);subscribers.OnEventDidTrigger(__0);
  __result=__0;return false;
 }
 // Native opportunity commands call static Rulebook.Trigger rather than the
 // MechanicsContext.TriggerRule entrypoint captured by the shared boundary.
 // Redirect just that call: the actual OnAction still builds and flags its rule.
 static IEnumerable<CodeInstruction> CaptureOpportunity(IEnumerable<CodeInstruction> instructions){
  foreach(var instruction in instructions){
   var method=instruction.operand as System.Reflection.MethodInfo;
   if(method?.DeclaringType==typeof(Rulebook)&&method.Name=="Trigger"&&method.IsGenericMethod&&method.GetGenericArguments().SequenceEqual(new[]{typeof(RuleAttackWithWeapon)})&&method.GetParameters().Length==1){
    instruction.opcode=System.Reflection.Emit.OpCodes.Call;
    instruction.operand=AccessTools.Method(typeof(Gambit01414Tests),nameof(DeliverOpportunity));
   }
   yield return instruction;
  }
 }
 static RuleAttackWithWeapon DeliverOpportunity(RuleAttackWithWeapon rule){
  opportunityRules++;Check(rule.IsAttackOfOpportunity,"native opportunity command flags its constructed rule");
  RuleAttackWithWeapon result=rule;Reaction(rule,ref result);return result;
 }
 internal static int Run(BlueprintsCache cache){
  checks=aoo=opportunityRules=0;shots.Clear();meleeReactions.Clear();queueAoO=deferAoO=false;tb=true;passing=true;legal=reach=true;delta=0f;now=TimeSpan.FromSeconds(100);
  var savedTime=Game.Instance.TimeController;var savedCombat=Game.Instance.TurnBasedCombatController;
  Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());Set(Game.Instance,"TurnBasedCombatController",Empty<CombatController>());
  var harmony=new Harmony("CotW01414.GambitRegression");
  void Patch(System.Reflection.MethodBase method,string name)=>harmony.Patch(method,prefix:new HarmonyMethod(typeof(Gambit01414Tests),name));
  Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(Mode));Patch(AccessTools.Method(typeof(CombatController),"IsPassing"),nameof(Passing));
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameDeltaTime"),nameof(Delta));
  Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"GetTargetsAround",new[]{typeof(Vector3),typeof(float),typeof(bool),typeof(bool)}),nameof(Targets));
  Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Ally));Patch(AccessTools.Method(typeof(UnitEntityData),"CanAttack",new[]{typeof(UnitEntityData)}),nameof(CanAttack));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),nameof(Hit));
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),nameof(No));Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));
  Patch(AccessTools.Method(typeof(UnitHelper),"IsAttackOfOpportunityReach"),nameof(Reach));Patch(AccessTools.PropertyGetter(typeof(UnitCombatState),"CanAttackOfOpportunity"),nameof(Yes));Patch(AccessTools.Method(typeof(UnitCombatState),"AttackOfOpportunity"),nameof(Aoo));
  PsychicRuleTestBoundary.Patch(harmony,typeof(RuleAttackWithWeapon),AccessTools.Method(typeof(Gambit01414Tests),nameof(Reaction)));
  TeamworkReactionTestBoundary.Patch(harmony);
  harmony.Patch(AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction"),transpiler:new HarmonyMethod(typeof(Gambit01414Tests),nameof(CaptureOpportunity)));
  harmony.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.GambitUnusedSwiftPatch")).Patch();
  try{
   AccessTools.Method(Module,"Register").Invoke(null,new object[]{cache});
   var wing=Asset<BlueprintFeature>(cache,"BrokenWingGambitFeat");var paw=Asset<BlueprintFeature>(cache,"WoundedPawGambitFeat");
   bait=Unit(cache,"01414-bait");var otherBait=Unit(cache,"01414-other-bait");enemy=Unit(cache,"01414-enemy");var first=Unit(cache,"01414-bow-first");var second=Unit(cache,"01414-bow-second");var ordinary=Unit(cache,"01414-no-gambit");
   var baitFact=Fact(bait,wing);var otherFact=Fact(otherBait,wing);var firstFact=Fact(first,wing);var secondFact=Fact(second,wing);Fact(first,paw);Fact(second,paw);
   var firstToggle=Psychic034Tests.Fact(first,Asset<BlueprintBuff>(cache,"WoundedPawEnabled"),first,10);var secondToggle=Psychic034Tests.Fact(second,Asset<BlueprintBuff>(cache,"WoundedPawEnabled"),second,10);
   var baitToggle=Psychic034Tests.Fact(bait,Asset<BlueprintBuff>(cache,"BrokenWingEnabled"),bait,10);Psychic034Tests.Fact(otherBait,Asset<BlueprintBuff>(cache,"BrokenWingEnabled"),otherBait,10);
   var sword=Weapon(bait,false,"01414-sword");var enemySword=Weapon(enemy,false,"01414-enemy-sword");var bow1=Weapon(first,true,"01414-bow1");var bow2=Weapon(second,true,"01414-bow2");Equip(first,bow1);Equip(second,bow2);
   Set(enemy,"m_Position",new Vector3(2,0,0));Set(first,"m_Position",new Vector3(3,0,0));Set(second,"m_Position",new Vector3(4,0,0));world=new[]{bait,first,second};
   Register(baitFact,firstFact,otherFact,secondFact);
   Check(ReferenceEquals(baitFact.Components.Single(r=>r.SourceBlueprintComponent is BrokenWingGambit).SourceBlueprintComponent,firstFact.Components.Single(r=>r.SourceBlueprintComponent is BrokenWingGambit).SourceBlueprintComponent),"fixture exercises one shared blueprint delegate with separate native runtimes");
   Offer(bait,sword);Offer(otherBait,sword);Offer(bait,sword);
   Check(Data(baitFact).Offers.Count==1&&Data(otherFact).Offers.Count==1&&Data(firstFact).Offers.Count==0,"native subscription dispatch isolates owners and refreshes without stacking");
   Check(Bonus(enemy,bait,enemySword)==2&&Damage(enemy,bait,enemySword)==2,"actual attack-stat event route gives accepting enemy exactly +2 attack and damage");
   Check(Bonus(enemy,otherBait,enemySword)==2&&Bonus(enemy,first,enemySword)==0,"separate baiters have independent target-bound bonuses");
   var fake=Attack(enemy,bait,enemySword);Set(fake.AttackRoll,"IsFake",true);subscribers.OnEventDidTrigger(fake);Check(Bonus(enemy,bait,enemySword)==2,"preview cannot consume the offer");
   var miss=Attack(bait,enemy,sword,true);Data(baitFact).Offers.Clear();subscribers.OnEventDidTrigger(miss);Check(Bonus(enemy,bait,enemySword)==0,"melee miss does not grant +2");
   var rangedOffer=Attack(bait,enemy,bow1);subscribers.OnEventDidTrigger(rangedOffer);Check(Data(baitFact).Offers.Count==0,"ranged hit cannot supply an offer");
   var abilityHit=Attack(bait,enemy,sword);abilityHit.ExtraAttack=true;subscribers.OnEventDidTrigger(abilityHit);Check(Bonus(enemy,bait,enemySword)==2,"ordinary ability/extra melee hit uses the same native weapon event route");
   ClearActions(first);ClearActions(second);Accept(bait,enemySword,true);
   Check(shots.Count==2&&shots.Select(r=>r.Initiator).Distinct().Count()==2&&aoo==0,"both archers independently react to a missed acceptance, without duplicate AoO");
   Check(Bonus(enemy,bait,enemySword)==0&&Bonus(enemy,otherBait,enemySword)==2,"only the first weapon attack consumes its owner's offer");
   foreach(var shot in shots.ToArray())subscribers.OnEventDidTrigger(shot);Check(Data(baitFact).Offers.Count==0&&Data(firstFact).Offers.Count==0&&Data(secondFact).Offers.Count==0,"nested and delayed reaction events cannot create recursive offers");
   Data(otherFact).Offers.Clear();

   // ForceToEnd is the real native method. No test prefix replaces its writes.
   for(int round=0;round<6;round++){
    ClearActions(first);ClearActions(second); // The native Cooldowns.Clear called by TurnController.Prepare.
    Register(round%2==0?new[]{baitFact,firstFact,secondFact,otherFact}:new[]{secondFact,otherFact,firstFact,baitFact});
    world=round%2==0?new[]{bait,first,second}:new[]{second,bait,first,second};
    if(round%3==0)Turn(first,second).ForceToEnd(true);else{Turn(first).ForceToEnd(true);Turn(second).ForceToEnd(round%2==0);}
    Check(first.CombatState.Cooldown.SwiftAction==0f&&second.CombatState.Cooldown.SwiftAction==0f,"ending or delaying turns preserves both genuinely unused swift actions, round "+round);
    Check(first.CombatState.Cooldown.StandardAction==6f&&first.CombatState.Cooldown.MoveAction==3f,"end-turn preservation leaves standard and move expenditure unchanged");
    delta=1f;Tick(first,second);var previous=shots.Count;Offer(bait,sword);
    Check(Bonus(enemy,bait,enemySword)==2&&Damage(enemy,bait,enemySword)==2,"fresh round has exactly +2 before accepting attack");Accept(bait,enemySword,round%2!=0);
    Check(shots.Count==previous+2&&shots.Skip(previous).Select(s=>s.Initiator).Distinct().Count()==2,"both holders respond across repeated rounds and subscription orders, round "+round);
    Offer(bait,sword);Accept(bait,enemySword);Check(shots.Count==previous+2,"a second opening cannot spend an already used swift pool");
    Turn(first,second).ForceToEnd(true);Check(first.CombatState.Cooldown.SwiftAction==6f&&second.CombatState.Cooldown.SwiftAction==6f,"end turn never refunds the genuine reaction payments");
    delta=6f;Tick(first,second);Check(!first.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift)&&!second.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift),"native passing-time cooldown controller recovers both pools");
   }
   ClearActions(ordinary);Turn(ordinary).ForceToEnd(true);Check(ordinary.CombatState.Cooldown.SwiftAction==6f,"non-Gambit unit retains ordinary native force-end cooldown");
   ClearActions(first);Set(firstToggle,"IsTurnedOn",false);Turn(first).ForceToEnd(true);Check(first.CombatState.Cooldown.SwiftAction==6f,"disabled ranged consent receives no off-turn preservation");Set(firstToggle,"IsTurnedOn",true);
   ClearActions(first);first.SpendAction(UnitCommand.CommandType.Swift,false,0f);ClearActions(second);Turn(first,second).ForceToEnd(true);Check(first.CombatState.Cooldown.SwiftAction==6f&&second.CombatState.Cooldown.SwiftAction==0f,"mounted pair preserves only its individually unspent swift action");
   var before=shots.Count;Offer(bait,sword);Accept(bait,enemySword);Check(shots.Count==before+1&&shots.Last().Initiator==second,"one actual spent swift blocks only that archer");
   ClearActions(first);ClearActions(second);first.Commands.Raw[(int)UnitCommand.CommandType.Swift]=Empty<UnitUseAbility>();Set(second,"m_Position",new Vector3(12,0,0));before=shots.Count;Offer(bait,sword);Accept(bait,enemySword);Check(shots.Count==before&&first.CombatState.Cooldown.SwiftAction==0&&second.CombatState.Cooldown.SwiftAction==0,"queued swift and out-of-range archer are both rejected without payment");first.Commands.Raw[(int)UnitCommand.CommandType.Swift]=null;Set(second,"m_Position",new Vector3(4,0,0));
   legal=false;Offer(bait,sword);Accept(bait,enemySword);Check(shots.Count==before&&first.CombatState.Cooldown.SwiftAction==0&&second.CombatState.Cooldown.SwiftAction==0,"illegal native targets never consume actions");legal=true;

   // The native AoO command creates its weapon rule later, outside Respond's
   // stack. Only the exact command scheduled by Gambit must remain tagged.
   world=new[]{bait,first};Equip(first,Weapon(first,false,"01414-reaction-sword"));Set(firstToggle,"IsTurnedOn",false);
   Psychic034Tests.Fact(first,Asset<BlueprintBuff>(cache,"BrokenWingEnabled"),first,10);queueAoO=true;
   Offer(bait,sword);Accept(bait,enemySword);var generated=first.Commands.Free as UnitAttackOfOpportunity;
   Check(generated!=null&&generated.Target==enemy,"Gambit preserves the native delayed opportunity command");
   AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(generated,null);
   Check(meleeReactions.Count==1&&Data(firstFact).Offers.Count==0,"delayed native generated AoO cannot recursively offer a Gambit");
   foreach(var rule in meleeReactions.ToArray())subscribers.OnEventDidTrigger(rule);
   Check(Data(firstFact).Offers.Count==0,"generated rule remains tagged after every holder's global dispatch and later callbacks");
   var ordinaryOpportunity=Opportunity(first,enemy);AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(ordinaryOpportunity,null);
   Check(Data(firstFact).Offers.Count==1&&Bonus(enemy,first,enemySword)==2&&Damage(enemy,first,enemySword)==2,"ordinary non-Gambit AoO melee hit supplies the required +2 offer");
   Data(firstFact).Offers.Clear();first.Commands.Raw[(int)UnitCommand.CommandType.Free]=null;
   Offer(bait,sword);Accept(bait,enemySword);var cancelled=first.Commands.Free as UnitAttackOfOpportunity;first.Commands.Raw[(int)UnitCommand.CommandType.Free]=null;
   var replacement=Opportunity(first,enemy);Check(!ReferenceEquals(cancelled,replacement),"cancelled generated command is distinct from the later ordinary command");AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(replacement,null);
   Check(Data(firstFact).Offers.Count==1&&Bonus(enemy,first,enemySword)==2,"cancelling a generated AoO never poisons an unrelated later opportunity");Data(firstFact).Offers.Clear();
   // Run's real queue helper keeps the old Free command while adding the new
   // reaction. The queue can already contain an unrelated ordinary opportunity.
   deferAoO=true;var busy=new BusyOpportunity(enemy);Set(busy,"Executor",first);Set(busy,"Hand",first.Body.PrimaryHand);first.Commands.Raw[(int)UnitCommand.CommandType.Free]=busy;
   var alreadyQueued=Opportunity(first,enemy,false);first.Commands.Queue.AddLast(alreadyQueued);
   Offer(bait,sword);Accept(bait,enemySword);var queued=first.Commands.Queue.Last.Value as UnitAttackOfOpportunity;
   Check(ReferenceEquals(first.Commands.Free,busy)&&first.Commands.Queue.Count==2&&queued!=null&&!ReferenceEquals(queued,alreadyQueued),"native scheduling retains Free and appends a distinct generated opportunity");
   first.Commands.Queue.Remove(queued);first.Commands.Raw[(int)UnitCommand.CommandType.Free]=queued;AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(queued,null);
   Check(Data(firstFact).Offers.Count==0,"queued Gambit opportunity stays tagged when later promoted to Free and executed");
   first.Commands.Queue.Remove(alreadyQueued);first.Commands.Raw[(int)UnitCommand.CommandType.Free]=alreadyQueued;AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(alreadyQueued,null);
   Check(Data(firstFact).Offers.Count==1&&Bonus(enemy,first,enemySword)==2,"pre-existing queued ordinary opportunity is never captured by Gambit's new-command marker");Data(firstFact).Offers.Clear();
   first.Commands.Raw[(int)UnitCommand.CommandType.Free]=busy;Offer(bait,sword);Accept(bait,enemySword);var cancelledQueued=first.Commands.Queue.Single();first.Commands.Queue.Remove(cancelledQueued);
   replacement=Opportunity(first,enemy);AccessTools.Method(typeof(UnitAttackOfOpportunity),"OnAction").Invoke(replacement,null);
   Check(!ReferenceEquals(cancelledQueued,replacement)&&Data(firstFact).Offers.Count==1&&Bonus(enemy,first,enemySword)==2,"cancelling a queued generated opportunity leaves a later ordinary opportunity eligible");Data(firstFact).Offers.Clear();deferAoO=false;
   Check(opportunityRules==6,"all six generated and ordinary native opportunity commands reach the controlled rule-delivery endpoint");
   first.Commands.Raw[(int)UnitCommand.CommandType.Free]=null;queueAoO=false;Equip(first,bow1);Set(firstToggle,"IsTurnedOn",true);world=new[]{bait,first,second};
   Set(baitToggle,"IsTurnedOn",false);Offer(bait,sword);Check(Bonus(enemy,bait,enemySword)==0,"offer consent off prevents fresh +2");Set(baitToggle,"IsTurnedOn",true);
   Offer(bait,sword);var saved=JsonConvert.DeserializeObject<GambitData>(JsonConvert.SerializeObject(Data(baitFact)));Check(saved.Offers.Single().Enemy.UniqueId==enemy.UniqueId&&saved.Offers[0].RemainingTurnEnds==1,"offer identity and turn deadline survive serialization");
   AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.GambitTurnEndPatch"),"End").Invoke(null,new object[]{bait});Check(Bonus(enemy,bait,enemySword)==0,"off-turn offer ends at next owner's turn end");
   tb=false;Offer(bait,sword);now+=TimeSpan.FromSeconds(6);Check(Bonus(enemy,bait,enemySword)==0,"real-time offer ends at six seconds");ClearActions(first);Turn(first).ForceToEnd(true);Check(first.CombatState.Cooldown.SwiftAction==6f,"real-time mode does not change ForceToEnd");
   PsychicExpansionTests.Inventory["gambit01414Assertions"]=checks;
   PsychicExpansionTests.Inventory["gambit01414Scope"]=new JObject{["nativeForceToEnd"]=true,["nativeCooldownTick"]=true,["nativeMultiOwnerSubscriptionDispatch"]=true,["nativeSwiftPayment"]=true,["nativeDelayedAoOAction"]=true,["nativeOpportunityQueueHelper"]=true,["rounds"]=6,["boundary"]="World/side/CanAct/target legality/roll-hit, AoO submission approval, queue promotion and final ranged projectile delivery are controlled; the real TryAddToQueueInsteadOfRunImmediately populates the queue behind a fixture uninterruptible command, and real UnitAttackOfOpportunity.OnAction creates delayed weapon rules. No Unity scene or full save loading."};
   Console.WriteLine("PASS: "+checks+" Gambit checks, native ForceToEnd/cooldown/payment, two archers for six rounds, mounted pair and shared-delegate nested dispatch.");return checks;
  }finally{harmony.UnpatchAll(harmony.Id);Set(Game.Instance,"TimeController",savedTime);Set(Game.Instance,"TurnBasedCombatController",savedCombat);shots.Clear();meleeReactions.Clear();subscribers=null;world=Array.Empty<UnitEntityData>();bait=enemy=null;}
 }
}
