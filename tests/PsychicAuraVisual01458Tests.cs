using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    sealed class FxHandle058:IFxHandle
    {
        public FxSpawnRequest Request=>null;public FxPriority Priority=>default;public bool IsSpawned{get;private set;}public bool IsDestroyed{get;private set;}public GameObject SpawnedObject{get;private set;}public event Action<GameObject> OnSpawn;public int Requests;
        public void RunAfterSpawn(Action<GameObject> action){Requests++;if(IsSpawned)action(SpawnedObject);else OnSpawn+=action;}
        public void HandleDestroy(){IsDestroyed=true;}
        internal void Spawn(GameObject value){SpawnedObject=value;IsSpawned=true;OnSpawn?.Invoke(value);OnSpawn=null;}
    }
    static Transform fxTransform058;static AuraRadiusFxRestore01458 fxRestore058;static Vector3 fxScale058;
    static Transform FxTransform058(GameObject value)=>fxTransform058;
    static Vector3 FxScaleGet058(Transform value)=>fxScale058;
    static void FxScaleSet058(Transform value,Vector3 scale)=>fxScale058=scale;
    static AuraRadiusFxRestore01458 FxState058(GameObject value)=>fxRestore058;
    static bool FxTruthy058(UnityEngine.Object value)=>!ReferenceEquals(value,null);
    static AuraRadiusFxRestore01458 FxStateAdd058(GameObject value)=>fxRestore058=(AuraRadiusFxRestore01458)FormatterServices.GetUninitializedObject(typeof(AuraRadiusFxRestore01458));
    static IEnumerable<CodeInstruction> AuraVisualEndpoints058(IEnumerable<CodeInstruction> input)
    {
        var bounded=(IEnumerable<CodeInstruction>)AccessTools.Method(typeof(Arcane01446Tests),"FxBoundary").Invoke(null,new object[]{input});
        foreach(var code in bounded){var m=code.operand as MethodInfo;string replace=null;
            if(m?.DeclaringType==typeof(UnityEngine.Object)&&m.Name=="op_Implicit")replace=nameof(FxTruthy058);
            if(m?.DeclaringType==typeof(GameObject)){
                if(m.Name=="get_transform")replace=nameof(FxTransform058);
                if(m.IsGenericMethod&&m.GetGenericArguments().SequenceEqual(new[]{typeof(AuraRadiusFxRestore01458)}))replace=m.Name=="GetComponent"?nameof(FxState058):m.Name=="AddComponent"?nameof(FxStateAdd058):null;
            }
            if(m?.DeclaringType==typeof(Transform))replace=m.Name=="get_localScale"?nameof(FxScaleGet058):m.Name=="set_localScale"?nameof(FxScaleSet058):null;
            if(m?.DeclaringType==typeof(AuraRadiusFxRestore01458))replace=m.Name=="Restore"?nameof(AuraRestoreNative058):m.Name=="Capture"?nameof(AuraCaptureNative058):null;
            if(m?.DeclaringType==T("AuraRadiusVisual01458")&&m.Name=="Scale")replace=nameof(AuraScaleNative058);
            if(replace!=null){code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(MagusBatch01454Tests),replace);}yield return code;
        }
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void AuraScaleNative058(GameObject effect,float factor){IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>AuraVisualEndpoints058(code);_=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;throw new NotImplementedException("actual production Scale reverse body required");}
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void AuraCaptureNative058(AuraRadiusFxRestore01458 state,Transform target){IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>AuraVisualEndpoints058(code);_=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;throw new NotImplementedException("actual production Capture reverse body required");}
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void AuraRestoreNative058(AuraRadiusFxRestore01458 state){IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>AuraVisualEndpoints058(code);_=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;throw new NotImplementedException("actual production Restore reverse body required");}
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void AuraDisableNative058(AuraRadiusFxRestore01458 state){IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>AuraVisualEndpoints058(code);_=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;throw new NotImplementedException("actual production OnDisable reverse body required");}
    static void AuraVisual058(BlueprintsCache c,Harmony h)
    {
        var visual=T("AuraRadiusVisual01458");var restore=typeof(AuraRadiusFxRestore01458);var patched=new List<MethodInfo>();
        foreach(var pair in new[]{new[]{AccessTools.Method(visual,"Scale"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(AuraScaleNative058))},new[]{AccessTools.Method(restore,"Capture"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(AuraCaptureNative058))},new[]{AccessTools.Method(restore,"Restore"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(AuraRestoreNative058))},new[]{AccessTools.Method(restore,"OnDisable"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(AuraDisableNative058))}})h.CreateReversePatcher(pair[0],new HarmonyMethod(pair[1])).Patch();
        foreach(var type in Main.GetTypes().Where(t=>t.DeclaringType==visual))foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(m=>m.Name.StartsWith("<Bind>b__",StringComparison.Ordinal)&&m.GetMethodBody()!=null)){
            h.Patch(method,transpiler:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(AuraVisualEndpoints058)));patched.Add(method);
        }
        Check(patched.Count==1,"exact one authored deferred visual callback executes with reverse-copied production scale/lifecycle bodies and bounded Unity API endpoints");
        try{
            fxTransform058=(Transform)FormatterServices.GetUninitializedObject(typeof(Transform));var effect=(GameObject)FormatterServices.GetUninitializedObject(typeof(GameObject));fxRestore058=null;fxScale058=new Vector3(2,3,4);
            var view=(AreaEffectView)FormatterServices.GetUninitializedObject(typeof(AreaEffectView));var area=A<BlueprintAbilityAreaEffect>(c,"MarauderI0Area01457");Set(view,"m_Blueprint",BlueprintReferenceBase.CreateTyped<BlueprintAbilityAreaEffectReference>(area));var handle=new FxHandle058();Set(view,"m_SpawnedFx",handle);
            Call(T("AuraRadiusSpawn01458Patch"),"Postfix",view);Call(T("AuraRadiusSpawn01458Patch"),"Postfix",view);Check(handle.Requests==1,"actual aura binding registers exactly one deferred callback per immutable native handle");handle.Spawn(effect);
            Check(Math.Abs(fxScale058.x-2f/3)<.0001&&fxScale058.y==3&&Math.Abs(fxScale058.z-4f/3)<.0001,"actual production callback scales ten-foot gameplay aura from thirty-foot source without scaling height");
            AuraDisableNative058(fxRestore058);Check(fxScale058==new Vector3(2,3,4),"actual production pooled-FX OnDisable restores original local scale");AuraRestoreNative058(fxRestore058);Check(fxScale058==new Vector3(2,3,4),"repeated production restoration is idempotent");
            area=A<BlueprintAbilityAreaEffect>(c,"MarauderI5Area01457");Set(view,"m_Blueprint",BlueprintReferenceBase.CreateTyped<BlueprintAbilityAreaEffectReference>(area));var second=new FxHandle058();Set(view,"m_SpawnedFx",second);Call(T("AuraRadiusSpawn01458Patch"),"Postfix",view);second.Spawn(effect);
            Check(Math.Abs(fxScale058.x-2f*35/30)<.0001&&Math.Abs(fxScale058.z-4f*35/30)<.0001,"actual pooled object can be reused for thirty-five-foot aura without compounding previous radius");AuraDisableNative058(fxRestore058);
            var destroyed=new FxHandle058();Set(view,"m_SpawnedFx",destroyed);Call(T("AuraRadiusSpawn01458Patch"),"Postfix",view);destroyed.HandleDestroy();destroyed.Spawn(effect);Check(fxScale058==new Vector3(2,3,4),"destroyed asynchronous native handle cannot scale a recycled effect");
            Set(view,"m_Blueprint",BlueprintReferenceBase.CreateTyped<BlueprintAbilityAreaEffectReference>(N<BlueprintAbilityAreaEffect>(c,"4a15b95f8e173dc4fb56924fe5598dcf")));var foreign=new FxHandle058();Set(view,"m_SpawnedFx",foreign);Call(T("AuraRadiusSpawn01458Patch"),"Postfix",view);Check(foreign.Requests==0,"original native Dirge aura sharing ring resource remains untouched");
        }finally{foreach(var method in patched)h.Unpatch(method,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(AuraVisualEndpoints058)));fxRestore058=null;fxTransform058=null;}
    }
}
