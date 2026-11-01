using System;
using System.Linq;
using Kingmaker.Controllers.Units;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

namespace Way_of_the_shield.NewComponents {
 [TypeId("df977e98b20d4e2aad72dce7ad003f66")]
 public sealed class ShieldedDefenseState : UnitBuffComponentDelegate, IUnitBuffHandler, ITickEachRound {
  const string Effect="29397505648a462a874872b132a00b75",Defensive="6ffd93355fb3bcf4592a5d976b1d32a9";
  void Sync(bool reset=false){
   var effect=ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Effect);if(effect==null)return;
   bool valid=Fact.IsTurnedOn&&Owner.Buffs.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Defensive));
   var active=Owner.Buffs.GetBuff(effect);
   if(valid){
    if(active==null)active=Owner.Descriptor.AddBuff(effect,new MechanicsContext(Owner,Owner,OwnerBlueprint,Context),null);
    if(active!=null&&reset)active.CallComponents<BuffSetRanksOnApply>(c=>c.SetRanks());
   }else active?.Remove();
   ShieldAcState.Refresh(Owner);
  }
  protected override void OnTurnOn()=>Sync();
  protected override void OnTurnOff(){Owner.Buffs.GetBuff(ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Effect))?.Remove();ShieldAcState.Refresh(Owner);}
  protected override void OnApplyPostLoadFixes()=>Sync();
  public void HandleBuffDidAdded(Buff buff){if(buff.Owner==Owner&&buff.Blueprint.AssetGuid.ToString()==Defensive)Sync(true);}
  public void HandleBuffDidRemoved(Buff buff){if(buff.Owner==Owner&&buff.Blueprint.AssetGuid.ToString()==Defensive)Sync();}
  void ITickEachRound.OnNewRound()=>Sync(true);
 }
}
