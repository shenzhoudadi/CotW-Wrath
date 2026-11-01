using System;using System.Linq;using System.Reflection;using HarmonyLib;using Kingmaker;using Kingmaker.Controllers;using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem;using Kingmaker.EntitySystem.Entities;using Kingmaker.UnitLogic.Buffs;using Kingmaker.UnitLogic.Buffs.Components;using Kingmaker.UnitLogic.FactLogic;using Kingmaker.UnitLogic.Mechanics;using Kingmaker.UnitLogic.Abilities.Blueprints;
internal static partial class MagusBatch01454Tests{
 static int AuraSpawns060;static AreaEffectEntityData AuraLast060;
 static bool Avatar060(AddAreaEffect component)=>true;
 static System.Collections.Generic.IEnumerable<CodeInstruction> AvatarInput060(System.Collections.Generic.IEnumerable<CodeInstruction> instructions){int count=0,allocation=0;foreach(var i in instructions){if(i.operand is MethodInfo m){if(m.DeclaringType==typeof(AddAreaEffect)&&m.Name=="IsOwnerAvatarValid"){i.opcode=System.Reflection.Emit.OpCodes.Call;i.operand=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(Avatar060));count++;}if(m.DeclaringType==typeof(AreaEffectsController)&&m.Name=="SpawnAttachedToTarget"){i.opcode=System.Reflection.Emit.OpCodes.Call;i.operand=AccessTools.Method(typeof(MagusBatch01454Tests),nameof(SpawnArea060));allocation++;}}yield return i;}if(count!=1||allocation!=1)throw new InvalidOperationException("060 actual native aura body must contain exactly one avatar and one scene-allocation endpoint.");}
 static AreaEffectEntityData SpawnArea060(MechanicsContext context,BlueprintAbilityAreaEffect blueprint,UnitEntityData owner,TimeSpan? duration){Check(context!=null&&context.MaybeCaster==owner,"060 native aura emitter passes its actual host context and owner to scene allocation");AuraSpawns060++;return AuraLast060=(AreaEffectEntityData)AccessTools.Method(typeof(TimeStutter01437Tests),"MakeArea").Invoke(null,new object[]{"060-emitter-"+AuraSpawns060,context,blueprint,new[]{owner}});}
 static void AuraInputs060(UnitEntityData owner,Harmony h){
  Set(owner,"HoldingState",System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SceneEntitiesState)));Check(owner.IsInState,"060 aura actor has an actual native nonnull scene membership input");
  foreach(string method in new[]{"OnActivate","OnAreaActivated"})h.Patch(AccessTools.Method(typeof(AddAreaEffect),method),transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(AvatarInput060)));
 }
 static void AuraControl060(BlueprintsCache c,Harmony h){var owner=EquipmentUnit(c,13,Psychic058);var fact=Grant(owner,c,"HallucinogenicAuraFeature01458");var host=owner.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==Id("HallucinogenicAuraHost01460"));var carrier=(EntityFact)host??fact;var runtime=carrier.Components.Single(r=>r.SourceBlueprintComponent is AddAreaEffect);AuraInputs060(owner,h);using(runtime.RequestEventContext())AccessTools.Method(typeof(AddAreaEffect),"OnActivate").Invoke(runtime.SourceBlueprintComponent,null);Check(AuraSpawns060==1,"060 actual native emitter activation has a real Buff context and reaches scene allocation");}
 static Buff AuraLifecycle060(UnitEntityData owner,Kingmaker.UnitLogic.Feature fact,Buff host,Harmony h){
  AuraInputs060(owner,h);
  var runtime=host.Components.Single(r=>r.SourceBlueprintComponent is AddAreaEffect);var emitter=runtime.SourceBlueprintComponent;
  using(runtime.RequestEventContext())AccessTools.Method(typeof(AddAreaEffect),"OnActivate").Invoke(emitter,null);
  Check(AuraSpawns060==1&&AuraLast060.Context.MaybeCaster==owner,"060 actual native AddAreaEffect.OnActivate reaches allocation exactly once outside combat");var original=AuraLast060;
  using(runtime.RequestEventContext()){((AddAreaEffect)emitter).OnAreaActivated();Check(AuraSpawns060==1,"060 repeated native scene activation keeps the existing aura instance");((AddAreaEffect)emitter).OnAreaBeginUnloading();Check(original.IsEnded,"060 actual native scene unload ForceEnd retires only this host's area");((AddAreaEffect)emitter).OnAreaActivated();Check(AuraSpawns060==2&&!ReferenceEquals(AuraLast060,original),"060 native area activation recreates the emitter after scene unload");}
  var current=AuraLast060;host.Remove();Check(current.IsEnded,"060 actual host removal executes native emitter cleanup");
  var grant=fact.Components.Single(r=>r.SourceBlueprintComponent is AddFacts);
  using(grant.RequestEventContext())AccessTools.Method(typeof(AddFacts),"OnApplyPostLoadFixes").Invoke(grant.SourceBlueprintComponent,null);
  host=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("HallucinogenicAuraHost01460"));Check(host.Context.MaybeCaster==owner&&AuraSpawns060==3,"060 native AddFacts old-character postload restores missing permanent aura and actually starts its emitter");
  using(grant.RequestEventContext())AccessTools.Method(typeof(AddFacts),"OnApplyPostLoadFixes").Invoke(grant.SourceBlueprintComponent,null);
  Check(owner.Buffs.Enumerable.Count(b=>b.Blueprint.AssetGuid==host.Blueprint.AssetGuid)==1&&AuraSpawns060==3,"060 repeated native AddFacts postload does not duplicate aura or allocate again");
  return host;
 }
}
