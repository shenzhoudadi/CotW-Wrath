using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using UnityEngine;
using TurnBased.Controllers;

namespace CallOfTheWild.Wrath.Prototype {
 public sealed class GambitOffer {
  [JsonProperty,JsonConverter(typeof(ChargeUnitReferenceConverter))] public UnitReference Enemy;
  [JsonProperty] public TimeSpan Expires;
  [JsonProperty] public int RemainingTurnEnds;
 }
 public sealed class GambitData { [JsonProperty] public List<GambitOffer> Offers=new List<GambitOffer>(); }
 [TypeId("cf7de3a4e2eb4a3ea7d008a5287cba97")]
 public sealed class BrokenWingGambit:UnitFactComponentDelegate<GambitData>,IGlobalRulebookHandler<RuleAttackWithWeapon>,IGlobalRulebookHandler<RuleCalculateAttackBonus>,IGlobalRulebookHandler<RuleCalculateWeaponStats>,ITickEachRound,IGlobalSubscriber {
  [ThreadStatic] static bool resolving;
  static readonly ConditionalWeakTable<RuleAttackWithWeapon,object> Reactions=new ConditionalWeakTable<RuleAttackWithWeapon,object>();
  static readonly ConditionalWeakTable<UnitAttackOfOpportunity,object> OpportunityReactions=new ConditionalWeakTable<UnitAttackOfOpportunity,object>();
  void Expire()=>Data.Offers.RemoveAll(x=>x.Enemy.Value==null||x.Enemy.Value.State.IsDead||(!CombatController.IsInTurnBasedCombat()&&x.Expires<=Game.Instance.TimeController.GameTime));
  bool Lured(UnitEntityData enemy){Expire();return Data.Offers.Any(x=>x.Enemy.Value==enemy);}
  internal void EndTurn(){foreach(var x in Data.Offers)x.RemainingTurnEnds--;Data.Offers.RemoveAll(x=>x.RemainingTurnEnds<=0);}
  public void OnEventAboutToTrigger(RuleCalculateAttackBonus e){if(e.Target==Owner&&Lured(e.Initiator))e.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleCalculateAttackBonus e){}
  public void OnEventAboutToTrigger(RuleCalculateWeaponStats e){if(e.AttackWithWeapon?.Target==Owner&&Lured(e.Initiator))e.AddDamageModifier(2,Fact,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleCalculateWeaponStats e){}
  public void OnEventAboutToTrigger(RuleAttackWithWeapon e){
   if(Reactions.TryGetValue(e,out _)||!e.IsAttackOfOpportunity)return;
   // Native AoOs execute later, after Respond has returned. Tag the one rule
   // emitted by the exact queued command, not other ordinary opportunity attacks.
   if(e.Initiator?.Commands?.Free is UnitAttackOfOpportunity command&&command.Executor==e.Initiator&&command.Target==e.Target&&OpportunityReactions.TryGetValue(command,out _)){
    Reactions.Add(e,new object());OpportunityReactions.Remove(command);
   }
  }
  public void OnEventDidTrigger(RuleAttackWithWeapon e){
   if(resolving||Reactions.TryGetValue(e,out _)||TeamworkRangedReaction.IsReaction(e)||e.AttackRoll==null||e.AttackRoll.IsFake)return;
   if(e.Target==Owner&&Lured(e.Initiator)){
    Data.Offers.RemoveAll(x=>x.Enemy.Value==e.Initiator);
    Respond(e.Initiator);
   }
   if(e.Initiator==Owner&&e.AttackRoll.IsHit&&e.Weapon?.Blueprint.IsMelee==true&&e.Target!=null&&!e.Target.IsAlly(Owner)&&!e.Target.State.IsDead&&CooperationFeats.Enabled(Owner,"BrokenWingEnabled")){
    Expire();Data.Offers.RemoveAll(x=>x.Enemy.Value==e.Target);
    var current=Game.Instance.TurnBasedCombatController?.CurrentTurn;
    bool ownTurn=current?.Rider==Owner||current?.Mount==Owner;
    Data.Offers.Add(new GambitOffer{Enemy=e.Target,Expires=Game.Instance.TimeController.GameTime+TimeSpan.FromSeconds(6),RemainingTurnEnds=ownTurn?2:1});
   }
  }
  void Respond(UnitEntityData enemy){
   // Component delegates are shared by every holder. Keep the accepting owner
   // and mechanics context fixed while nested reaction rules dispatch to them.
   var owner=Owner;var context=Context;
   var allies=TeamworkFeats.Nearby(enemy,30f).Where(a=>a!=owner&&a!=enemy&&a.IsAlly(owner)&&!a.State.IsDead&&a.State.CanAct&&CooperationFeats.Has(a,"BrokenWingGambitFeat")).Distinct().ToArray();
   try{resolving=true;
    // Snapshot the neighborhood; native threat/reach and action counters gate each response.
    foreach(var ally in allies){
     if(!TeamworkFeats.Partner(ally,owner,"BrokenWingGambitFeat")||enemy.State.IsDead||ally.State.IsDead||!ally.State.CanAct||!ally.CanAttack(enemy))continue;
     var weapon=ally.Body.PrimaryHand.MaybeWeapon;
     if(CooperationFeats.Has(ally,"WoundedPawGambitFeat")&&CooperationFeats.Enabled(ally,"WoundedPawEnabled")&&weapon?.Blueprint.IsRanged==true&&Vector3.Distance(ally.Position,enemy.Position)<=9.144f&&ally.Commands.Swift==null&&!ally.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift)){
      TeamworkRangedReaction.TryQueue(ally,enemy,context,ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(PsychicAssets.Id("WoundedPawEnabled")));
     }else {
       var hand=ally.GetThreatHand();var range=hand==null?(float?)null:ally.GetThreatRange(hand);
       if(range.HasValue&&ally.CombatState.CanAttackOfOpportunity&&UnitHelper.IsAttackOfOpportunityReach(ally,enemy,range.Value)){
        var commands=ally.Commands;var previous=new[]{commands.Free}.Concat(commands.Queue).ToArray();
        // Run can retain the old Free action and enqueue the new opportunity.
        // Compare command identities across both slots so deferred reactions
        // remain tagged without suppressing pre-existing ordinary AoOs.
        if(ally.CombatState.AttackOfOpportunity(enemy))
         foreach(var command in new[]{commands.Free}.Concat(commands.Queue).OfType<UnitAttackOfOpportunity>())
          if(command.Executor==ally&&command.Target==enemy&&!previous.Any(old=>ReferenceEquals(old,command)))
           OpportunityReactions.GetValue(command,_=>new object());
       }
     }
    }
   }finally{resolving=false;}
  }
  public void OnNewRound()=>Expire();
  protected override void OnTurnOff()=>Data.Offers.Clear();
  protected override void OnApplyPostLoadFixes()=>Expire();
 }
 [HarmonyPatch(typeof(TurnController),"End")]
 internal static class GambitTurnEndPatch {
  [HarmonyPostfix] static void Postfix(TurnController __instance){End(__instance.Rider);if(__instance.Mount!=__instance.Rider)End(__instance.Mount);}
  internal static void End(UnitEntityData unit){
   if(unit==null)return;
   foreach(var fact in unit.Facts.List.Where(f=>f.Blueprint.AssetGuid==PsychicAssets.Id("BrokenWingGambitFeat")&&T11TrainingOptions.Active(unit,f)))
    foreach(var rt in fact.Components.Where(r=>r.SourceBlueprintComponent is BrokenWingGambit))using(rt.RequestEventContext())((BrokenWingGambit)rt.SourceBlueprintComponent).EndTurn();
  }
 }
 [HarmonyPatch(typeof(TurnController),nameof(TurnController.ForceToEnd))]
 internal static class GambitUnusedSwiftPatch {
  // ForceToEnd spends every action even when the player merely ends the turn.
  // That synthetic swift cooldown must not cancel an opted-in off-turn reaction.
  // Actual swift expenditures (including an earlier reaction) are never refunded.
  [HarmonyPrefix] static void Prefix(TurnController __instance,bool setCooldowns,out UnitEntityData[] __state){
   __state=setCooldowns&&CombatController.IsInTurnBasedCombat()
    ?new[]{__instance.Rider,__instance.Mount}.Where(u=>u!=null&&u.IsInCombat&&!u.State.IsDead&&u.State.CanAct&&u.CombatState.Cooldown.SwiftAction==0f&&TeamworkRangedReaction.Enabled(u)).Distinct().ToArray()
    :Array.Empty<UnitEntityData>();
  }
  [HarmonyPostfix,HarmonyPriority(Priority.Last)] static void Postfix(UnitEntityData[] __state){
   foreach(var unit in __state)if(unit.CombatState.Cooldown.SwiftAction==6f)unit.CombatState.Cooldown.SwiftAction=0f;
  }
 }
 [TypeId("825bfae8f7b5448b988aa51d1c90fc6f")]
 public sealed class StunningAssaultEffect:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,IInitiatorRulebookHandler<RuleCalculateCMB>,IInitiatorRulebookHandler<RuleAttackWithWeapon>,ITurnBasedModeHandler {
  bool Ready=>Buff.IsActive&&Buff.IsTurnedOn&&!Buff.IsSuppressed&&CooperationFeats.Has(Owner,"StunningAssaultFeat");
  public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget e){if(Ready&&e.Weapon?.Blueprint.IsMelee==true)e.AddModifier(-5,Fact,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget e){}
  public void OnEventAboutToTrigger(RuleCalculateCMB e){if(Ready)e.AddModifier(-5,Fact,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleCalculateCMB e){}
  public void OnEventAboutToTrigger(RuleAttackWithWeapon e){}
  public void OnEventDidTrigger(RuleAttackWithWeapon e){
   if(!Ready||e.AttackRoll?.IsHit!=true||e.AttackRoll.IsFake||e.Weapon?.Blueprint.IsMelee!=true||e.Target==null||e.Target.State.IsDead)return;
   var save=Context.TriggerRule(new RuleSavingThrow(e.Target,SavingThrowType.Fortitude,10+Owner.Stats.BaseAttackBonus.ModifiedValue));
   if(save.IsPassed)return;
   var bp=ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(BlueprintGuid.Parse("09d39b38bb7c6014394b6daced9bacd3"));
   var action=PsychicAssets.Apply(bp,1,fromSpell:false);action.IsNotDispelable=true;using(Context.GetDataScope(e.Target))action.RunAction();
  }
  public void HandleTurnStarted(UnitEntityData unit){if(unit==Owner)Buff.Remove();}
  public void HandleSurpriseRoundStarted(){}
  public void HandleRoundStarted(int round){}
  public void HandleUnitControlChanged(UnitEntityData unit){}
  public void HandleUnitNotSurprised(UnitEntityData unit,RuleSkillCheck rule){}
 }
}
