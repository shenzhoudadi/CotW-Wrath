using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicTemporalStasis {
  internal static PsychicSpells.Entry Build(PsychicAssets f){
   const string text="A melee touch attack places the target in suspended animation for 1 round per caster level. Fortitude negates; spell resistance applies. The target cannot act, move or be targeted, takes no hit point damage, ability damage/drain or negative levels, and cannot be healed. Existing buff timers and periodic ticks pause until it returns. A missed touch retains the charge.";
   var b=f.Clone(f.Native<BlueprintBuff>("8fff364a212d4b4894b5e222f3696cdd"),"TemporalStasisBuff","Temporal Stasis",text);Set(b,"m_Icon",PsychicOriginalIcons.Load("TemporalStasis"));SetEnum(b,"m_Flags","Harmful");b.Stacking=StackingType.Replace;b.IsClassFeature=false;b.ComponentsArray=b.ComponentsArray.Where(c=>!(c is AddCondition a&&a.Condition==UnitCondition.Paralyzed)).Concat(new[]{(BlueprintComponent)new PsychicStasisPause()}).ToArray();b.FxOnStart=new PrefabLink {AssetId="eb0e36f1de0c05347963262d56d90cf5"};
   var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");var apply=Apply(b,0);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
   var delivery=new AbilityDeliverTouch();Set(delivery,"m_TouchWeapon",Ref<BlueprintItemWeaponReference>(f.Native<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>("bb337517547de1a4189518d404ec49d4")));
   var touch=f.Clone(f.Native<BlueprintAbility>("3c17035ec4717674cae2e841a190e757"),"TemporalStasisTouch","Temporal Stasis",text);touch.ComponentsArray=new BlueprintComponent[]{delivery,new SpellComponent {School=SpellSchool.Transmutation},rank,Save(SavingThrowType.Fortitude,new ContextActionConditionalSaved {Succeed=Actions(),Failed=Actions(apply)}),new AbilitySpawnFx {PrefabLink=b.FxOnStart,Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget}};
   var root=f.Clone(f.Native<BlueprintAbility>("ab395d2335d3f384e99dddee8562978f"),"TemporalStasis","Temporal Stasis",text);var sticky=new AbilityEffectStickyTouch();Set(sticky,"m_TouchDeliveryAbility",Ref<BlueprintAbilityReference>(touch));root.ComponentsArray=new BlueprintComponent[]{sticky,new SpellComponent {School=SpellSchool.Transmutation}};
   foreach(var a in new[]{root,touch}){a.Range=AbilityRange.Touch;a.CanTargetSelf=false;a.CanTargetFriends=true;a.CanTargetEnemies=true;a.CanTargetPoint=false;a.SpellResistance=true;a.EffectOnAlly=a.EffectOnEnemy=AbilityEffectOnUnit.Harmful;a.AvailableMetamagic=Metamagic.Heighten|Metamagic.Extend|Metamagic.Quicken|Metamagic.Reach|Metamagic.Piercing;Set(a,"m_Parent",new BlueprintAbilityReference());Set(a,"m_Icon",b.Icon);a.LocalizedDuration=Text(a.name,"duration","1 round per caster level");a.LocalizedSavingThrow=Text(a.name,"savingThrow","Fortitude negates");}
   return new PsychicSpells.Entry {Ability=root,Level=8};
  }
 }
 public sealed class PsychicStasisPauseData {
  [JsonProperty] public bool Shifted;
  [JsonProperty] public TimeSpan Until,Amount;
  [JsonProperty] public List<string> BuffIds=new List<string>();
 }
 [TypeId("3a54bdf7c20246b0a58400e37c38abfa")]
 public sealed class PsychicStasisPause:UnitBuffComponentDelegate<PsychicStasisPauseData>,IInitiatorRulebookHandler<RuleSavingThrow>,ITargetRulebookHandler<RuleDealStatDamage>,ITargetRulebookHandler<RuleDrainEnergy>,ITargetRulebookHandler<RuleHealDamage>,ISubscriber {
  static TimeSpan SafeAdd(TimeSpan value,TimeSpan delta)=>value==TimeSpan.MaxValue?value:TimeSpan.FromTicks(delta.Ticks>0&&value.Ticks>TimeSpan.MaxValue.Ticks-delta.Ticks?TimeSpan.MaxValue.Ticks:value.Ticks+delta.Ticks);
  static void Shift(Buff b,TimeSpan by){b.EndTime=SafeAdd(b.EndTime,by);Set(b,"<NextTickTime>k__BackingField",SafeAdd((TimeSpan)Get(b,"<NextTickTime>k__BackingField"),by));if(b.HasNextResourceSpendingTime)b.NextResourceSpendingTime=SafeAdd(b.NextResourceSpendingTime,by);}
  protected override void OnTurnOn(){if(Data.Shifted)return;Data.Amount=Buff.TimeLeft;if(Data.Amount<=TimeSpan.Zero||Data.Amount==TimeSpan.MaxValue)return;Data.Until=Buff.EndTime;Data.BuffIds.Clear();foreach(var b in Owner.Buffs.Enumerable.Where(x=>x!=Buff)){Shift(b,Data.Amount);Data.BuffIds.Add(b.UniqueId);}Data.Shifted=true;}
  protected override void OnTurnOff(){if(!Data.Shifted)return;var left=Data.Until-Kingmaker.Game.Instance.TimeController.GameTime;if(left>Data.Amount)left=Data.Amount;if(left>TimeSpan.Zero)foreach(var b in Owner.Buffs.Enumerable.Where(x=>x!=Buff&&Data.BuffIds.Contains(x.UniqueId)))Shift(b,-left);Data.BuffIds.Clear();Data.Shifted=false;}
  public void OnEventAboutToTrigger(RuleSavingThrow e){if(e.Initiator==Owner)e.AutoPass=true;}
  public void OnEventAboutToTrigger(RuleDealStatDamage e){if(e.Target==Owner)e.Immune=true;}
  public void OnEventAboutToTrigger(RuleDrainEnergy e){if(e.Target==Owner)e.TargetIsImmune=true;}
  public void OnEventAboutToTrigger(RuleHealDamage e){if(e.Target==Owner)e.Interrupted=true;}
  public void OnEventDidTrigger(RuleSavingThrow e){}public void OnEventDidTrigger(RuleDealStatDamage e){}public void OnEventDidTrigger(RuleDrainEnergy e){}public void OnEventDidTrigger(RuleHealDamage e){}
 }
}
