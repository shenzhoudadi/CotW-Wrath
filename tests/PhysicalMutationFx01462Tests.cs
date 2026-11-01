using System;using System.Linq;using System.Collections.Generic;using HarmonyLib;
using Kingmaker;using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.UnitLogic.Buffs;using Kingmaker.UnitLogic.Buffs.Blueprints;using Kingmaker.Visual.Particles;using Kingmaker.Visual.Particles.FxSpawnSystem;
internal static partial class MagusBatch01454Tests{
 static readonly List<string> PhysicalFxRequests062=new List<string>();
 static readonly List<FxHandle058> PhysicalFxHandles062=new List<FxHandle058>();
 // Renderer allocation is the explicit standalone endpoint. Native buff
 // activation/renewal/deactivation and ClearParticleEffect are not replaced.
 static bool PhysicalRender062(Buff __instance){
  if(__instance.Blueprint.AssetGuid!=Id("PhysicalMutationBuff01457"))return true;
  string asset=__instance.Blueprint.FxOnStart?.AssetId;PhysicalFxRequests062.Add(asset);
  if(!string.IsNullOrEmpty(asset)){var handle=new FxHandle058();PhysicalFxHandles062.Add(handle);Set(__instance,"m_ParticleEffect",handle);}
  return false;
 }
 static bool PhysicalDestroy062(IFxHandle __0){if(!( __0 is FxHandle058 handle)||!PhysicalFxHandles062.Contains(handle))return true;handle.HandleDestroy();return false;}
 static void PhysicalFx062(BlueprintsCache c,Harmony h){
  var spawn=AccessTools.Method(typeof(Buff),nameof(Buff.TrySpawnParticleEffect));var destroy=AccessTools.Method(typeof(FxHelper),nameof(FxHelper.Destroy),new[]{typeof(IFxHandle),typeof(bool)});
  h.Patch(spawn,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PhysicalRender062)));
  h.Patch(destroy,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PhysicalDestroy062)));
  var clock=Game.Instance.Player.GameTime;PhysicalFxRequests062.Clear();PhysicalFxHandles062.Clear();
  try{
   var owner=EquipmentUnit(c,15,Psychic058);owner.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(c,"MutationMindArchetype01457"));Grant(owner,c,"PhysicalMutationFeature01457");var pool=A<BlueprintAbilityResource>(c,"PhysicalMutationResource01457");var toggle=Toggle(owner,c,"PhysicalMutationToggle01458");int points=owner.Resources.GetResourceAmount(pool);
   ToggleOn(toggle);var buff=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));var end=buff.EndTime;
   Check(PhysicalFxRequests062.SequenceEqual(new[]{"3b42d149d55dfe5469d9967f22d7e020"})&&PhysicalFxHandles062.Count==1,"062 actual native mutation activation requests original CotW start prefab at renderer endpoint");
   Check(buff.Blueprint.FxOnRemove!=null&&string.IsNullOrEmpty(buff.Blueprint.FxOnRemove.AssetId),"062 native removal has a valid empty link, matching original CotW without a separate end burst");
   var handle=PhysicalFxHandles062.Single();ToggleOff(toggle);Check(buff.IsAttached&&!handle.IsDestroyed&&owner.Resources.GetResourceAmount(pool)==points-1,"062 native controller-off retains paid main buff and its owned FX handle");
   ToggleOn(toggle);Check(PhysicalFxRequests062.Count==1&&buff.EndTime==end&&owner.Resources.GetResourceAmount(pool)==points-1,"062 reopening paid mutation neither respawns activation FX nor buys another minute");
   Game.Instance.Player.GameTime=end;owner.Buffs.TickBuff(buff,end);Check(buff.IsAttached&&PhysicalFxRequests062.Count==1&&!handle.IsDestroyed&&owner.Resources.GetResourceAmount(pool)==points-2,"062 native scheduled renewal preserves one FX owner without removal/reapplication or repeated activation particles");
   ToggleOff(toggle);Game.Instance.Player.GameTime=buff.EndTime;owner.Buffs.TickBuff(buff,buff.EndTime);
   Check(!buff.IsAttached&&handle.IsDestroyed&&AccessTools.Field(typeof(Buff),"m_ParticleEffect").GetValue(buff)==null,"062 actual native expiry/deactivation clears and destroys the owned visual handle");
   foreach(var b in owner.Buffs.Enumerable.ToArray())b.Remove();ToggleOn(toggle);var second=owner.Buffs.Enumerable.Single(b=>b.Blueprint.AssetGuid==Id("PhysicalMutationBuff01457"));Check(PhysicalFxRequests062.Count==2&&PhysicalFxHandles062.Count==2,"062 later fresh activation requests its own visual once");second.Remove();
   Check(PhysicalFxHandles062.All(a=>a.IsDestroyed)&&!toggle.IsOn,"062 native external removal destroys the second visual and stops mutation maintenance");
  }finally{Game.Instance.Player.GameTime=clock;h.Unpatch(spawn,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(PhysicalRender062)));h.Unpatch(destroy,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(PhysicalDestroy062)));}
 }
}
