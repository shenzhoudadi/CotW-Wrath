using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;
using Kingmaker.UnitLogic.Buffs.Components;using Kingmaker.Controllers.Units;
using Kingmaker.Blueprints;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem;using Kingmaker.EntitySystem.Entities;using Kingmaker.PubSubSystem;using Kingmaker.RuleSystem;using Kingmaker.RuleSystem.Rules;using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;using Kingmaker.UnitLogic.Buffs;using Kingmaker.UnitLogic.Buffs.Blueprints;using Kingmaker.UnitLogic.Mechanics.Properties;using Kingmaker.UnitLogic.Parts;
namespace CallOfTheWild.Wrath.Prototype{
 [TypeId("45ec5161b9324c20ac57a16f61000201")]
 public sealed class TotemMasterRank01461:PropertyValueGetter{
  public BlueprintCharacterClassReference Class;
  protected override int GetBaseValue(UnitEntityData unit){var owner=unit?.Get<UnitPartPet>()?.Master??unit;int level=owner?.Progression.GetClassLevel(Class.Get())??0;return level>=15?3:level>=8?2:level>=1?1:0;}
 }
 [TypeId("45ec5161b9324c20ac57a16f61000206")]
 public sealed class TotemCompanionRefresh01461:UnitFactComponentDelegate,IOwnerGainLevelHandler{
  public BlueprintFeatureReference Feature;
  protected override void OnTurnOn()=>Refresh();protected override void OnApplyPostLoadFixes()=>Refresh();public void HandleUnitGainLevel()=>Refresh();
  void Refresh(){foreach(var reference in Owner.Pets.ToArray()){var pet=reference.Entity;if(pet?.Master!=Owner||pet.Get<UnitPartPet>()?.Type!=Kingmaker.Enums.PetType.AnimalCompanion)continue;var fact=pet.Progression.Features.GetFact(Feature.Get());if(fact?.IsActive==true)fact.Reapply();}}
 }
 [TypeId("45ec5161b9324c20ac57a16f61000202")]
 public sealed class TotemStrengthRestriction01461:ActivatableAbilityRestriction{
  public BlueprintCharacterClassReference Class;public int Minimum;public BlueprintFeatureReference[] Companions;
  protected override bool IsAvailable()=>Owner.Progression.GetClassLevel(Class.Get())>=Minimum&&(Companions.Length==0||Companions.Any(r=>Owner.HasFact(r.Get())));
 }
 public sealed class TotemStrengthState01461{[JsonProperty]public EntityFactRef<Buff> Effect,Lease;}
 [TypeId("45ec5161b9324c20ac57a16f61000203")]
 public sealed class TotemStrengthMode01461:UnitBuffComponentDelegate<TotemStrengthState01461>,ITickEachRound,IOwnerGainLevelHandler{
  public BlueprintCharacterClassReference Class;public int Minimum;public BlueprintFeatureReference[] Companions;public BlueprintBuffReference Low,High;
  protected override void OnActivate()=>Refresh();protected override void OnTurnOn()=>Refresh();protected override void OnTurnOff()=>Clear();protected override void OnDeactivate(){if(!Buff.IsReapplying)Clear();}protected override void OnApplyPostLoadFixes()=>Refresh();public void OnNewRound()=>Refresh();public void HandleUnitGainLevel()=>Refresh();
  Buff Owned()=>Owner?.Buffs.Enumerable.FirstOrDefault(b=>b.UniqueId==Data.Effect.FactId&&(b.Blueprint==Low.Get()||b.Blueprint==High.Get()));
  void Clear(){var owned=Owned();Data.Effect=default;Data.Lease=default;if(owned!=null&&!owned.IsDisposed)owned.Remove();}
  internal void Refresh(Buff excluding=null){
   if(Owner==null)return;var lease=Owner.Buffs.Enumerable.FirstOrDefault(b=>b!=excluding&&b.Blueprint.AssetGuid==PsychicAssets.Id(TotemBonded01461.LeaseToken)&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&!b.IsDisposed&&b.TimeLeft>TimeSpan.Zero);
   int level=Owner.Progression.GetClassLevel(Class.Get());if(lease==null||!Buff.IsActive||!Buff.IsTurnedOn||Buff.IsSuppressed||level<Minimum||(Companions.Length>0&&!Companions.Any(r=>Owner.HasFact(r.Get())))){Clear();return;}
   var desired=(level>=15?High:Low).Get();var owned=Owned();if(owned!=null&&!owned.IsDisposed&&owned.Blueprint==desired&&Data.Lease.FactId==lease.UniqueId)return;Clear();
   var applied=Owner.Descriptor.AddBuff(desired,lease.Context,lease.TimeLeft);if(applied!=null){applied.IsNotDispelable=true;Data.Effect=applied;Data.Lease=lease;}
  }
  internal static void RefreshAll(UnitEntityData owner,Buff excluding=null){if(owner==null)return;foreach(var buff in owner.Buffs.Enumerable.Where(b=>b.Blueprint.GetComponent<TotemStrengthMode01461>()!=null).ToArray())foreach(var runtime in buff.Components.Where(r=>r.SourceBlueprintComponent is TotemStrengthMode01461))using(runtime.RequestEventContext())((TotemStrengthMode01461)runtime.SourceBlueprintComponent).Refresh(excluding);}
 }
 [TypeId("45ec5161b9324c20ac57a16f61000204")]
 public sealed class TotemStrengthLease01461:UnitBuffComponentDelegate{
  protected override void OnActivate()=>TotemStrengthMode01461.RefreshAll(Owner);protected override void OnTurnOn()=>TotemStrengthMode01461.RefreshAll(Owner);protected override void OnTurnOff()=>TotemStrengthMode01461.RefreshAll(Owner,Buff);protected override void OnDeactivate(){if(!Buff.IsReapplying)TotemStrengthMode01461.RefreshAll(Owner,Buff);}protected override void OnApplyPostLoadFixes()=>TotemStrengthMode01461.RefreshAll(Owner);
 }
 [TypeId("45ec5161b9324c20ac57a16f61000205")]
 public sealed class TotemPowerfulCharge01461:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateWeaponStats>,IInitiatorRulebookSubscriber{
  public BlueprintBuffReference Charge;
  public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt){}
  public void OnEventDidTrigger(RuleCalculateWeaponStats evt){var attack=evt.AttackWithWeapon;if(attack==null||!attack.IsFirstAttack||attack.IsAttackOfOpportunity||evt.Weapon?.Blueprint.IsMelee!=true||!Owner.HasFact(Charge.Get())||evt.WeaponDamageDice.Modifications?.Any(m=>ReferenceEquals(m.Source,Fact))==true)return;var dice=evt.WeaponDamageDice.ModifiedValue;evt.WeaponDamageDice.Modify(new DiceFormula(dice.Rolls*2,dice.Dice),Fact);}
 }
}
