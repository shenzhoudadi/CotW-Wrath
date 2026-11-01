using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using Kingmaker.Visual.Particles;
using UnityEngine;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static int relocated058;static Vector3 destination058;
    static GameObject DoorPrefab058(Kingmaker.ResourceLinks.WeakResourceLink<GameObject> link,bool unused,bool async)=>null;
    static IFxHandle DoorPortal058(GameObject prefab,UnitEntityView view,bool? invert,string bone,Vector3 offset,FxPriority priority)=>null;
    static void DoorRelax058(Vector3[] points,float[] radii,int count,Vector3? center){Check(count==1&&points.Length==1,"native Dimension Door computes exactly one caster destination before scene navmesh endpoint");}
    static void DoorWake058(UnitEntityData unit,float duration,bool force){}
    static Transform DoorGameTransform058(GameObject value)=>throw new Exception("absent portal object must not access an engine transform");
    static Transform DoorComponentTransform058(Component value)=>throw new Exception("absent portal component must not access an engine transform");
    static Vector3 DoorPosition058(Transform value)=>throw new Exception("absent portal transform must not read scene position");
    static void DoorSetPosition058(Transform value,Vector3 point)=>throw new Exception("absent portal transform must not write scene position");
    static Quaternion DoorQuaternion058(float x,float y,float z)=>throw new Exception("point-target teleport must not enter unit-target facing scene endpoint");
    static IEnumerator DoorMove058(DimensionDoorSettings settings,UnitEntityData unit,TargetWrapper target,Vector3 point,Vector3? from,Vector3? to,bool isCaster,TimeSpan? delay)
    {
        Check(isCaster&&settings.Targets.Count==1&&settings.Targets[0]==unit,"native Dimension Door relocation endpoint contains only its real caster");yield return null;relocated058++;destination058=point;Set(unit,"m_Position",point);
    }
    static IEnumerable<CodeInstruction> DoorScene058(IEnumerable<CodeInstruction> input)
    {
        var bounded=(IEnumerable<CodeInstruction>)AccessTools.Method(typeof(Arcane01446Tests),"FxBoundary").Invoke(null,new object[]{input});
        foreach(var code in bounded){var m=code.operand as MethodInfo;string replacement=null;
            if(m?.DeclaringType==typeof(Kingmaker.ResourceLinks.WeakResourceLink<GameObject>)&&m.Name=="Load")replacement=nameof(DoorPrefab058);
            if(m?.DeclaringType==typeof(FxHelper)&&m.Name=="SpawnFxOnUnit")replacement=nameof(DoorPortal058);
            if(m?.DeclaringType==typeof(FreePlaceSelector)&&m.Name=="RelaxPoints")replacement=nameof(DoorRelax058);
            if(m?.DeclaringType==typeof(UnitEntityData)&&m.Name=="Wake")replacement=nameof(DoorWake058);
            if(m?.DeclaringType==typeof(AbilityCustomDimensionDoor)&&m.Name=="CreateTeleportationRoutine")replacement=nameof(DoorMove058);
            if(m?.DeclaringType==typeof(IEnumerator)&&m.Name=="MoveNext")replacement=nameof(DoorStep058);
            if(m?.DeclaringType==typeof(GameObject)&&m.Name=="get_transform")replacement=nameof(DoorGameTransform058);
            if(m?.DeclaringType==typeof(Component)&&m.Name=="get_transform")replacement=nameof(DoorComponentTransform058);
            if(m?.DeclaringType==typeof(Transform))replacement=m.Name=="get_position"?nameof(DoorPosition058):m.Name=="set_position"?nameof(DoorSetPosition058):null;
            if(m?.DeclaringType==typeof(Quaternion)&&m.Name=="Euler")replacement=nameof(DoorQuaternion058);
            if(replacement!=null){code.opcode=OpCodes.Call;code.operand=AccessTools.Method(typeof(MagusBatch01454Tests),replacement);}yield return code;
        }
    }
    static Type DoorIterator058(int number)=>typeof(AbilityCustomDimensionDoor).GetNestedType("<Deliver>d__"+number,BindingFlags.Public|BindingFlags.NonPublic);
    static IEnumerable<CodeInstruction> DoorCast058(IEnumerable<CodeInstruction> code,int number){foreach(var instruction in DoorScene058(code)){yield return instruction;if(instruction.opcode==OpCodes.Ldarg_0)yield return new CodeInstruction(OpCodes.Castclass,DoorIterator058(number));}}
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static bool DoorOuterNative058(object iterator){IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>DoorCast058(code,37);_=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;throw new NotImplementedException("actual native outer DimensionDoor reverse body required");}
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static bool DoorInnerNative058(object iterator){IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)=>DoorCast058(code,38);_=(Func<IEnumerable<CodeInstruction>,IEnumerable<CodeInstruction>>)Transpiler;throw new NotImplementedException("actual native inner DimensionDoor reverse body required");}
    static bool DoorStep058(IEnumerator iterator)=>iterator.GetType()==DoorIterator058(37)?DoorOuterNative058(iterator):iterator.GetType()==DoorIterator058(38)?DoorInnerNative058(iterator):iterator.MoveNext();
    static void Space058(BlueprintsCache c,Harmony h)
    {
        var nested=typeof(AbilityCustomDimensionDoor).GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic).Where(t=>t.Name=="<Deliver>d__37"||t.Name=="<Deliver>d__38").ToArray();Check(nested.Length==2,"both actual native Dimension Door delivery iterators are explicitly identified");
        foreach(var pair in new[]{new[]{AccessTools.Method(DoorIterator058(37),"MoveNext"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(DoorOuterNative058))},new[]{AccessTools.Method(DoorIterator058(38),"MoveNext"),AccessTools.Method(typeof(MagusBatch01454Tests),nameof(DoorInnerNative058))}})h.CreateReversePatcher(pair[0],new HarmonyMethod(pair[1])).Patch();
        var corp=AccessTools.PropertyGetter(typeof(UnitEntityView),"Corpulence");h.Patch(corp,prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Corpulence058)));var view=AccessTools.Field(typeof(EntityDataBase),"<View>k__BackingField");
        try{
            var owner=EquipmentUnit(c,13,Psychic058);var pool=Pool058(c,owner);var book=Book058(c,owner);Grant(owner,c,"SpaceRendingSpellFeature01458");var toggle=Toggle(owner,c,"SpaceRendingSpell01458Toggle");ToggleOn(toggle);var linked=Known058(c,book,3,"MindThrust3");var spell=A<BlueprintAbility>(c,"SpaceRendingCharisma2Ability01458");var ability=new AbilityData(spell,owner.Descriptor);
            var restriction=spell.GetComponent<SpaceRendingCaster01458>();Check(!restriction.IsCasterRestrictionPassed(owner),"actual teleport restriction rejects absence of a linked spell permit");int before=owner.Resources.GetResourceAmount(pool);ModeEvents058(Linked058(linked,owner),true);Check(restriction.IsCasterRestrictionPassed(owner)&&owner.Resources.GetResourceAmount(pool)==before,"native successful third-circle linked cast grants permit without upfront teleport cost");
            Check(!A<BlueprintAbility>(c,"SpaceRendingCharisma4Ability01458").GetComponent<SpaceRendingCaster01458>().IsCasterRestrictionPassed(owner),"actual fourth-distance option rejects a third-circle permit");Check(!A<BlueprintAbility>(c,"SpaceRendingWisdom2Ability01458").GetComponent<SpaceRendingCaster01458>().IsCasterRestrictionPassed(owner),"actual alternate-pool option rejects owner lacking that resource");
            var target=new TargetWrapper(new Vector3(3,0,0));var context=new AbilityExecutionContext(ability,ability.CalculateParams(),target,new RulebookEventContext(),owner);context.FxSpawners=spell.GetComponents<AbilitySpawnFx>().ToList();AbilityCastRateUtils.SpendResources(ability);Check(owner.Resources.GetResourceAmount(pool)==before-2,"actual native teleport resource payment charges selected twenty-foot cost once");
            var oldView=view.GetValue(owner);view.SetValue(owner,ViewInput058);relocated058=0;var delivery=spell.GetComponent<AbilityCustomDimensionDoor>().Deliver(context,target);int steps=0,finals=0;
            try{while(DoorStep058(delivery)){
                Check(++steps<=8,"native Dimension Door bounded delivery completes without an endless coroutine");if(delivery.Current==null){Check(restriction.IsCasterRestrictionPassed(owner),"permit remains while actual native relocation routine is pending");continue;}
                Check(relocated058==1&&delivery.Current.Target.Point==target.Point,"native final delivery target is yielded only after the caster relocation endpoint completes");
                AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect",new[]{typeof(AbilityExecutionContext),typeof(TargetWrapper),typeof(AbilityApplyEffect)}).Invoke(null,new object[]{context,delivery.Current.Target,spell.GetComponent<AbilityApplyEffect>()});finals++;
            }}finally{delivery.Dispose();view.SetValue(owner,oldView);}
            Check(finals==1&&destination058==target.Point&&!restriction.IsCasterRestrictionPassed(owner)&&owner.Resources.GetResourceAmount(pool)==before-2,"actual native final delivery applies appended permit-consumption action once without extra payment");
            ModeEvents058(Linked058(linked,owner),true);var permit=owner.Buffs.Enumerable.Single(v=>v.Blueprint.AssetGuid==Id("SpaceRendingPermit301458"));var clock=Game.Instance.Player.GameTime;Game.Instance.Player.GameTime=permit.EndTime;owner.Buffs.TickBuff(permit,permit.EndTime);Check(!restriction.IsCasterRestrictionPassed(owner),"actual native one-round expiry closes unused teleport permission");Game.Instance.Player.GameTime=clock;
        }finally{h.Unpatch(corp,AccessTools.Method(typeof(MagusBatch01454Tests),nameof(Corpulence058)));}
        PsychicExpansionTests.Inventory["psychicSpace01458Contract"]=new JObject{["passed"]=true,["assertions"]=checks,["nativeDeliveryIterators"]=2,["boundary"]="Both actual native delivery iterators and final target/action dispatch execute. Navmesh point relaxation, pure portal rendering and terminal coroutine relocation are explicit scene inputs; real Unity movement, animation and full command scheduling require scene observation."};
    }
}
