using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class AuraRadiusVisual01458
    {
        // Native Bard DirgeOfDoomArea's 30ft ring, already used by Court Bard.
        // Skald reuses the same prefab for50ft; its gameplay size is not a
        // second visual asset and cannot define this prefab's base radius.
        // AreaEffectView owns spawning/attachment/destruction; scale only its
        // particle instance once, including asynchronous spawns and pooled reuse.
        internal const string RingAsset="20caf000cd4c3434da00a74f4a49dccc";
        internal const float SourceRadiusFeet=30f;
        static readonly HashSet<BlueprintGuid> Areas=new HashSet<BlueprintGuid>(
            PsychicMarauder01457.OwnTokens.Where(t=>t.EndsWith("Area01457")).Select(PsychicAssets.Id).Concat(new[]{PsychicAssets.Id("HallucinogenicAuraArea01458"),PsychicAssets.Id(UrbanBloodrager01463.AreaToken)}));
        static readonly ConditionalWeakTable<IFxHandle,object> Scaled=new ConditionalWeakTable<IFxHandle,object>();
        internal static bool Owns(BlueprintAbilityAreaEffect area)=>area!=null&&Areas.Contains(area.AssetGuid)&&area.Fx?.AssetId==RingAsset;
        internal static void Bind(AreaEffectView view)
        {
            var reference=PsychicAssets.Get(view,"m_Blueprint") as BlueprintAbilityAreaEffectReference;
            var area=reference?.Get();if(!Owns(area))return;
            var handle=PsychicAssets.Get(view,"m_SpawnedFx") as IFxHandle;
            if(handle==null||handle.IsDestroyed||Scaled.TryGetValue(handle,out _))return;
            Scaled.Add(handle,new object());float factor=area.Size.Value/SourceRadiusFeet;
            handle.RunAfterSpawn(effect=>{if(!effect||handle.IsDestroyed)return;Scale(effect,factor);});
        }
        internal static void Scale(GameObject effect,float factor)
        {
            var state=effect.GetComponent<AuraRadiusFxRestore01458>()??effect.AddComponent<AuraRadiusFxRestore01458>();
            state.Restore();state.Capture(effect.transform);
            var original=effect.transform.localScale;
            effect.transform.localScale=new Vector3(original.x*factor,original.y,original.z*factor);
        }
    }
    public sealed class AuraRadiusFxRestore01458:MonoBehaviour
    {
        Transform target;Vector3 original;
        internal void Capture(Transform value){target=value;original=value.localScale;}
        internal void Restore(){if(target)target.localScale=original;target=null;}
        void OnDisable()=>Restore();
    }
    [HarmonyPatch(typeof(AreaEffectView),nameof(AreaEffectView.SpawnFxs))]
    internal static class AuraRadiusSpawn01458Patch
    {
        static void Postfix(AreaEffectView __instance)=>AuraRadiusVisual01458.Bind(__instance);
    }
}
