using System;using System.Collections.Generic;using System.Linq;using Newtonsoft.Json;
using Kingmaker.Blueprints;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem;using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Buffs;using Kingmaker.UnitLogic.Buffs.Blueprints;using Kingmaker.UnitLogic.Buffs.Components;
namespace CallOfTheWild.Wrath.Prototype{
 public sealed class SynapticDelayData01460{
  [JsonProperty]public List<EntityFactRef<Buff>> Watched=new List<EntityFactRef<Buff>>();
  [JsonProperty]public bool Initialized,Fired;
 }
 [TypeId("45ec5160b9324c20ac57a16f60000201")]
 public sealed class SynapticDelay01460:UnitBuffComponentDelegate<SynapticDelayData01460>,IUnitBuffHandler{
  internal void Follow(Buff source){Data.Initialized=true;if(!Data.Watched.Any(r=>r.FactId==source.UniqueId))Data.Watched.Add(source);}
  bool SameCast(Buff b)=>b!=Buff&&b.Owner.Unit==Owner&&b.Blueprint.AssetGuid!=PsychicAssets.Id("SynapticShockConfusion01458")&&b.Context?.SourceAbilityContext!=null&&ReferenceEquals(b.Context.SourceAbilityContext,Context.SourceAbilityContext);
  public void HandleBuffDidAdded(Buff b){if(Data.Initialized&&!Data.Fired&&SameCast(b))Follow(b);}
  public void HandleBuffDidRemoved(Buff b){if(!Data.Initialized||Data.Fired||b==Buff)return;if(Data.Watched.RemoveAll(r=>r.FactId==b.UniqueId)>0&&Data.Watched.Count==0)Buff.Remove();}
  void Reconcile(){if(!Data.Initialized||Data.Fired)return;Data.Watched.RemoveAll(r=>r.Fact==null||!r.Fact.IsAttached);if(Data.Watched.Count==0){Finish();Buff.Remove();}}
  protected override void OnTurnOn()=>Reconcile();
  protected override void OnPostLoad(){base.OnPostLoad();Reconcile();}
  protected override void OnTurnOff()=>Finish();
  void Finish(){
   if(!Data.Initialized||Data.Fired||Data.Watched.Any(r=>r.Fact!=null&&r.Fact.IsAttached)||Owner.State.IsDead||Owner.State.MarkedForDeath)return;
   Data.Fired=true;var confusion=ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(PsychicAssets.Id("SynapticShockConfusion01458"));
   if(confusion!=null)PsychicDisciplineRuntime01458.Apply(Owner,confusion,Context,TimeSpan.FromSeconds(6));
  }
 }
}
