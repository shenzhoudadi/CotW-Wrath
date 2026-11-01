using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Controllers.Units;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using UnityEngine;
using TurnBased.Controllers;

namespace CallOfTheWild.Wrath.Prototype {
 [TypeId("c7bb64bbd2904e7cb33c2b119043612f")]
 public sealed class CoordinatedShotBonus:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAttackBonus> {
  public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt){
   if(evt.Weapon?.Blueprint.IsRanged!=true||evt.Target==null)return;
   if(evt.Target.CombatState.EngagedBy.Any(a=>TeamworkFeats.Partner(Owner,a,TeamworkFeats.Tokens[0])&&!TeamworkFeats.Covers(Owner,evt.Target,a)))
    evt.AddModifier(evt.Target.CombatState.IsFlanked?2:1,Fact,ModifierDescriptor.UntypedStackable);
  }
  public void OnEventDidTrigger(RuleCalculateAttackBonus evt){}
 }
 public sealed class ChargeOpening {
  [JsonProperty,JsonConverter(typeof(ChargeUnitReferenceConverter))] public UnitReference Charger,Target;
  [JsonProperty] public TimeSpan Expires;
 }
 public sealed class ChargeUnitReferenceConverter:JsonConverter {
  public override bool CanConvert(Type type)=>type==typeof(UnitReference);
  public override void WriteJson(JsonWriter writer,object value,JsonSerializer serializer)=>writer.WriteValue(((UnitReference)value).UniqueId);
  public override object ReadJson(JsonReader reader,Type type,object existing,JsonSerializer serializer)=>new UnitReference((string)reader.Value);
 }
 public sealed class ChargeOpeningData {[JsonProperty] public List<ChargeOpening> Openings=new List<ChargeOpening>();}
 [TypeId("45f0c2de42c24e1698d717f491cc0c4e")]
 public sealed class DistractingChargeOpening:UnitFactComponentDelegate<ChargeOpeningData>,IGlobalRulebookHandler<RuleAttackWithWeapon>,IInitiatorRulebookHandler<RuleCalculateAttackBonus>,IInitiatorRulebookHandler<RuleAttackRoll>,ITurnBasedModeHandler,ITickEachRound,IGlobalSubscriber {
  void Expire()=>Data.Openings.RemoveAll(x=>x.Target.Value==null||x.Charger.Value==null||x.Charger.Value.State.IsDead||x.Target.Value.State.IsDead||(!CombatController.IsInTurnBasedCombat()&&x.Expires<=Game.Instance.TimeController.GameTime));
  public void OnEventAboutToTrigger(RuleAttackWithWeapon evt){}
  public void OnEventDidTrigger(RuleAttackWithWeapon evt){
   if(!evt.IsCharge||evt.AttackRoll?.IsHit!=true||evt.AttackRoll.IsFake||evt.Target==null||!TeamworkFeats.Partner(Owner,evt.Initiator,TeamworkFeats.Tokens[1]))return;
   Expire();Data.Openings.RemoveAll(x=>x.Charger.Value==evt.Initiator&&x.Target.Value==evt.Target);
   Data.Openings.Add(new ChargeOpening{Charger=evt.Initiator,Target=evt.Target,Expires=Game.Instance.TimeController.GameTime+TimeSpan.FromSeconds(6)});
  }
  public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt){Expire();if(Data.Openings.Any(x=>x.Target.Value==evt.Target))evt.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleCalculateAttackBonus evt){}
  public void OnEventAboutToTrigger(RuleAttackRoll evt){}
  public void OnEventDidTrigger(RuleAttackRoll evt){if(!evt.IsFake)Data.Openings.RemoveAll(x=>x.Target.Value==evt.Target);}
  public void HandleTurnStarted(UnitEntityData unit)=>Data.Openings.RemoveAll(x=>x.Charger.Value==unit);
  public void HandleSurpriseRoundStarted(){}
  public void HandleRoundStarted(int round){}
  public void HandleUnitControlChanged(UnitEntityData unit){}
  public void HandleUnitNotSurprised(UnitEntityData unit,RuleSkillCheck rule){}
  public void OnNewRound()=>Expire();
  protected override void OnTurnOff()=>Data.Openings.Clear();
  protected override void OnApplyPostLoadFixes()=>Expire();
 }
 [TypeId("d42f764daecf443c81f7e42da537b668")]
 public sealed class TargetOpportunityReaction:UnitBuffComponentDelegate,IGlobalRulebookHandler<RuleAttackWithWeapon>,IGlobalSubscriber {
  internal static bool IsReaction(RuleAttackWithWeapon rule)=>TeamworkRangedReaction.IsReaction(rule);
  public void OnEventAboutToTrigger(RuleAttackWithWeapon evt){}
  public void OnEventDidTrigger(RuleAttackWithWeapon evt){
   if(Buff.IsSuppressed||!Buff.IsTurnedOn||IsReaction(evt)||evt.AttackRoll?.IsHit!=true||evt.AttackRoll.IsFake||evt.Weapon?.Blueprint.IsRanged!=true||!TeamworkFeats.Has(Owner,TeamworkFeats.Tokens[2])||!TeamworkFeats.Partner(Owner,evt.Initiator,TeamworkFeats.Tokens[2]))return;
   TeamworkRangedReaction.TryQueue(Owner,evt.Target,Context,Buff.Blueprint);
  }
 }
 public sealed class SwarmScatterData {
  [JsonIgnore] public ModifiableValue.Modifier Modifier;
  [JsonIgnore] public HashSet<UnitEntityData> Neighbors=new HashSet<UnitEntityData>();
  [JsonIgnore] public bool Initialized;
 }
 [TypeId("85e026cf56ab40c096cb1a320a778aed")]
 public sealed class SwarmScatterDefense:UnitFactComponentDelegate<SwarmScatterData>,IEntityPositionChangedHandler,ITickEachRound,IGlobalSubscriber {
  const float Radius=2.1336f;
  static readonly Kingmaker.Blueprints.BlueprintGuid FeatId=PsychicAssets.Id("SwarmScatterFeat");
  bool Eligible(UnitEntityData unit){
   // Distance first: unrelated movement must not query the world or scan facts.
   if(unit==null||unit==Owner||(unit.Position-Owner.Position).sqrMagnitude>Radius*Radius||unit.IsDisposed||unit.State.IsDead||!unit.IsAlly(Owner))return false;
   if(Owner.State.Features.SoloTactics)return true;
   var facts=unit.Facts.List;
   for(int i=0;i<facts.Count;i++){var f=facts[i];if(f.Blueprint.AssetGuid==FeatId&&f.IsActive&&f.IsTurnedOn&&!f.IsDisposed)return true;}
   return false;
  }
  void Clear(){foreach(var m in Owner.Stats.AC.Modifiers.Where(m=>m.Source==Fact).ToArray())Owner.Stats.AC.RemoveModifier(m);Data.Modifier=null;Data.Neighbors?.Clear();Data.Initialized=false;}
  void Apply(){
   int bonus=Data.Neighbors.Count;
   if(Data.Modifier?.ModValue==bonus||bonus==0&&Data.Modifier==null)return;
   if(Data.Modifier!=null)Owner.Stats.AC.RemoveModifier(Data.Modifier);
   Data.Modifier=bonus>0?Owner.Stats.AC.AddModifier(bonus,Fact,ModifierDescriptor.Circumstance):null;
  }
  void Refresh(){
   if(Data.Neighbors==null)Data.Neighbors=new HashSet<UnitEntityData>();
   Data.Neighbors.Clear();
   foreach(var unit in Kingmaker.Designers.GameHelper.GetTargetsAround(Owner.Position,Radius,false,false))if(Eligible(unit))Data.Neighbors.Add(unit);
   Data.Initialized=true;Apply();
  }
  protected override void OnTurnOn()=>Refresh();
  protected override void OnTurnOff()=>Clear();
  protected override void OnApplyPostLoadFixes(){Clear();Refresh();}
  public void HandleEntityPositionChanged(EntityDataBase unit){
   if(!Data.Initialized||unit==Owner){Refresh();return;}
   if(!(unit is UnitEntityData moved))return;
   // The event identifies the only changed pair. Keep the native spatial query
   // for this holder's own movement and round/load reconciliation only.
   bool changed=Eligible(moved)?Data.Neighbors.Add(moved):Data.Neighbors.Remove(moved);
   if(changed)Apply();
  }
  public void OnNewRound()=>Refresh();
 }
}
