using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Units;
using TurnBased.Controllers;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class TeamworkRangedReaction {
  internal const float Range=9.144f;
  static readonly ConditionalWeakTable<RuleAttackWithWeapon,object> Reactions=new ConditionalWeakTable<RuleAttackWithWeapon,object>();
  internal static bool IsReaction(RuleAttackWithWeapon rule)=>rule!=null&&Reactions.TryGetValue(rule,out _);
  internal static void Mark(RuleAttackWithWeapon rule)=>Reactions.GetValue(rule,_=>new object());
  internal static bool Legal(UnitEntityData unit,UnitEntityData target)=>unit!=null&&target!=null&&!unit.IsDisposed&&!target.IsDisposed&&!unit.State.IsDead&&!target.State.IsDead&&unit.State.CanAct&&unit.Body.PrimaryHand.MaybeWeapon?.Blueprint.IsRanged==true&&(unit.Position-target.Position).sqrMagnitude<=Range*Range&&unit.CanAttack(target);
  internal static bool TryQueue(UnitEntityData unit,UnitEntityData target,MechanicsContext context,BlueprintBuff source){
   if(!Legal(unit,target)||unit.Commands.Swift!=null||unit.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift))return false;
   var command=new TeamworkRangedReactionCommand(target,context,unit.Body.PrimaryHand.MaybeWeapon.UniqueId);
   // Reserve the shared native pool before the scheduler can dispatch events.
   // First qualifying ordinary trigger wins; a queued shot cannot spend twice.
   FellingSmashTrigger.SpendSwift(unit,source);
   Enqueue(unit,command);return true;
  }
  internal static void Enqueue(UnitEntityData unit,TeamworkRangedReactionCommand command){
   // AddToQueueFirst calls Run when Free is empty, which clears planned actions.
   // Initialize and insert into the native public queue instead. UpdateQueue
   // promotes it normally, after any command already occupying the Free slot.
   command.Init(unit);unit.Commands.Queue.AddFirst(command);
  }
  internal static bool Enabled(UnitEntityData unit)=>
   CooperationFeats.Has(unit,"WoundedPawGambitFeat")&&CooperationFeats.Enabled(unit,"WoundedPawEnabled")||
   TeamworkFeats.Has(unit,"TargetOfOpportunityFeat")&&CooperationFeats.Enabled(unit,"TargetOpportunityEnabled");
 }
 [HarmonyPatch(typeof(UnitCommandController),"TickCommandTurnBased")]
 internal static class TeamworkRangedReactionTurnPatch {
  [HarmonyPrefix] static bool Prefix(UnitCommand __0,ref bool __result){
   if(!(__0 is TeamworkRangedReactionCommand)&&!(__0 is AngerRetortCommand01431))return true;
   // The native off-turn shortcut recognizes only melee AoO command types.
   // Allow these exact reaction commands through that gate, retaining UI pause.
   __result=!CombatController.IsInTurnBasedCombat()||!Game.Instance.TurnBasedCombatController.WaitingForUI;
   return false;
  }
 }
 // A free command occupies the native reaction slot, but waits for the weapon
 // animation's act frame. No standard/move action or opportunity counter is used.
 [TypeId("0a472157eddd41da9f1d11b04653c771")]
 public sealed class TeamworkRangedReactionCommand:UnitCommand {
  [JsonProperty] MechanicsContext m_Context;
  [JsonProperty] string m_WeaponId;
  [JsonProperty] bool m_Resolved;
  [JsonIgnore] AttackHandInfo m_Attack;
  public TeamworkRangedReactionCommand(JsonConstructorMark mark):base(mark){}
  internal TeamworkRangedReactionCommand(UnitEntityData target,MechanicsContext context,string weaponId):base(CommandType.Free,target){
   m_Context=context;m_WeaponId=weaponId;ApproachRadius=1000000f;ReactionAction=true;IgnoreCooldown();DoNotUpdateManualTarget();
  }
  public override bool IsOneFrameCommand=>false;
  public override bool IsInterruptible=>false;
  public override bool ShouldTurnToTarget=>true;
  public override bool MarkManualTarget=>false;
  public override bool NeedEquipWeapons=>true;
  public override bool CanStart {
   get {
    if(!base.CanStart||Executor==null)return false;
    // Standard and Free commands have separate slots. Wait for any existing
    // weapon/cast animation instead of replacing its handle mid-action.
    foreach(var command in Executor.Commands.Raw)
     if(command!=this&&command?.IsRunning==true&&command.IsAnimating)return false;
    return true;
   }
  }
  bool Ready=>TeamworkRangedReaction.Legal(Executor,Target?.Unit)&&Executor.Body.PrimaryHand.MaybeWeapon.UniqueId==m_WeaponId;
  public override bool ShouldBeInterrupted=>!Ready;
  protected override void OnStart(){
   base.OnStart();
   if(!Ready){Interrupt();return;}
   m_Attack=new AttackHandInfo(Executor.Body.PrimaryHand,0){Target=Target.Unit};
   if(Executor.View?.AnimationManager!=null&&!ShouldSkipAnimation){
    m_Attack.CreateAnimationHandleForAttack(new[]{m_Attack},Executor.GetSaddledUnit()!=null);
    if(m_Attack.AnimationHandle!=null){
     m_Attack.AnimationHandle.AttackTargetDistance=(Target.Unit.Position-Executor.Position).magnitude;
     StartAnimation(m_Attack.AnimationHandle);return;
    }
   }
   ScheduleAct();
  }
  protected override ResultType OnAction(){
   if(m_Resolved||m_Context==null||!Ready)return ResultType.Fail;
   m_Resolved=true;
   var rule=new RuleAttackWithWeapon(Executor,Target.Unit,Executor.Body.PrimaryHand.MaybeWeapon,0){ExtraAttack=true,IsFirstAttack=true,AttacksCount=1,SkipAnimation=false,WeaponSlot=Executor.Body.PrimaryHand,Reason=new RuleReason(m_Context)};
   TeamworkRangedReaction.Mark(rule);
   if(m_Attack!=null)m_Attack.Rule=rule;
   m_Context.TriggerRule(rule);
   if(m_Attack!=null)m_Attack.IsHit=rule.AttackRoll?.IsHit==true;
   return ResultType.Success;
  }
 }
}
