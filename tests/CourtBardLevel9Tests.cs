using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.Controllers;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Commands;

internal static partial class Archetypes01445Tests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CourtNativeFreeOriginal(ActivatableAbilityResourceLogic instance)=>throw new NotSupportedException("reverse patch stub");
    static bool CourtNullReference(Action action){try{action();return false;}catch(NullReferenceException){return true;}}
    static IEnumerable<CodeInstruction> CourtAutofillBoundary(IEnumerable<CodeInstruction> input)
    {
        var code=input.ToList();int found=0;
        for(int i=0;i<code.Count;i++){
            if(code[i].operand is MethodInfo m&&m.DeclaringType==typeof(Kingmaker.Settings.SettingsRoot)&&m.Name=="get_Game"){
                Check(code[i+1].operand is FieldInfo a&&a.Name=="Main"&&code[i+2].operand is FieldInfo b&&b.Name=="AutofillActionbarSlots"&&code[i+3].operand is MethodInfo c&&c.Name=="op_Implicit","exact native autofill settings endpoint");
                code[i].opcode=OpCodes.Ldc_I4_1;code[i].operand=null;code.RemoveRange(i+1,3);found++;
            }
        }
        Check(found==1,"only native settings IO replaced; actual actionbar rebuild retained");return code;
    }
    static void CourtLevel9(BlueprintsCache cache,Harmony harmony)
    {
        int before=checks;
        harmony.Patch(AccessTools.Method(typeof(UnitUISettings),"TryToInitialize"),transpiler:new HarmonyMethod(typeof(Archetypes01445Tests),nameof(CourtAutofillBoundary)));
        var getter=AccessTools.PropertyGetter(typeof(ActivatableAbilityResourceLogic),"IsFreeBecauseOfFact");
        harmony.CreateReversePatcher(getter,new HarmonyMethod(typeof(Archetypes01445Tests),nameof(CourtNativeFreeOriginal))).Patch();
        harmony.CreateClassProcessor(Main.GetType("CallOfTheWild.Wrath.Prototype.CourtBardFreePerformance01445Patch",true)).Patch();
        var bardResource=Native<BlueprintAbilityResource>(cache,"e190ba276831b5c4fa28737e5e49e6a6");
        var cls=Native<BlueprintCharacterClass>(cache,"772c83a25e2268e448e841dcd548235f");
        var archetype=Asset<BlueprintArchetype>(cache,"CourtBardArchetype01445");
        var nativeToggle=Native<BlueprintActivatableAbility>(cache,"be36959e44ac33641ba9e0204f3d227b");
        var nativeRow=PsychicExpansionTests.NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==nativeToggle.AssetGuid.ToString())["blueprint"]["Data"];
        foreach(string name in new[]{"m_Buff","Group","WeightInGroup","ActivationType","m_ActivateWithUnitCommand","DeactivateIfOwnerDisabled","DeactivateIfCombatEnded"}){
            var field=AccessTools.Field(nativeToggle.GetType(),name);field.SetValue(nativeToggle,PsychicExpansionTests.Read(nativeRow[name],field.FieldType));
        }
        Check(cls.Progression.LevelEntries.Single(e=>e.Level==9).Features.Single().AssetGuid.ToString()=="9ae0f32c72f8df84dab023d1b34641dc"&&!archetype.AddFeatures.Any(e=>e.Level==9),"actual native level 9 grants only Inspire Greatness, Court adds none");
        foreach(int level in new[]{8,9,10,13,14}){
            Console.WriteLine("Court level9 integration "+level);
            var unit=Unit(cache,0,level);
            unit.Progression.Classes[0].Archetypes.Add(archetype);
            Fact(unit,Native<BlueprintFeature>(cache,"b92bfc201c6a79e49afd0b5cfbfc269f"));
            foreach(var entry in archetype.AddFeatures.Where(e=>e.Level<=level))foreach(var f in entry.Features.OfType<BlueprintFeature>())Fact(unit,f);
            Fact(unit,Native<BlueprintFeature>(cache,"36931765983e96d4bb07ce7844cd897e"));
            if(level>=9)Fact(unit,Native<BlueprintFeature>(cache,"9ae0f32c72f8df84dab023d1b34641dc"));
            if(level>=13)Fact(unit,Native<BlueprintFeature>(cache,"fd4ec50bc895a614194df6b9232004b9"));
            unit.Resources.Restore(bardResource,1000);
            var ui=new UnitUISettings(unit.Descriptor);Set(unit.Descriptor,"UISettings",ui);
            Console.WriteLine("Court actual UI auto-fill "+level);
            ui.TryToInitialize();
            Console.WriteLine("Court actual UI post-load "+level);
            ui.PostLoad(unit.Descriptor);ui.TryToInitialize();
            foreach(var ability in unit.ActivatableAbilities.Enumerable.ToArray()){
                Console.WriteLine("Court actionbar query "+ability.Blueprint.name);
                var count=ability.ResourceCount;var available=ability.IsAvailable;
                Check(available,"native resource/restriction query is available "+level+ability.Blueprint.name);
                var slot=new MechanicActionBarSlotActivableAbility{ActivatableAbility=ability};
                Console.WriteLine("Court command "+UnitActivateAbility.GetCommandType(ability)+" count="+slot.GetResource()+" buff="+ability.Blueprint.Buff?.name);
                Check(slot.GetResource()==(count??-1),"actual actionbar resource getter matches native count");
                if(ability.Blueprint.Group==ActivatableAbilityGroup.BardicPerformance){
                    ability.IsOn=true;ability.TryStart();
                    Check(ability.IsStarted&&ability.AppliedBuff!=null,"actual native start "+ability.Blueprint.name);
                    ability.OnNewRound();
                    Check(ability.IsAvailable,"native round resource availability "+ability.Blueprint.name);
                    ability.TurnOffImmediately();
                }
            }
        }
        int queries=0;
        var limitlessId=BlueprintGuid.Parse("b5c00e781d3a45aa9a2ae169c5210b47");var prior=cache.Load(limitlessId);
        var limitless=new BlueprintFeature{AssetGuid=limitlessId,name="LimitlessBardicPerformance",Ranks=1};limitless.OnEnable();cache.AddCachedBlueprint(limitlessId,limitless);
        var earlier=new BlueprintFeature{AssetGuid=Id("CourtEarlierFreeFactFixture"),name="EarlierFreePerformance",Ranks=1};earlier.OnEnable();cache.AddCachedBlueprint(earlier.AssetGuid,earlier);
        try{
            foreach(var blueprint in new[]{nativeToggle,Asset<BlueprintActivatableAbility>(cache,"CourtBardSatireToggle01445")}){
                var unit=Unit(cache,0,9);unit.Progression.Classes[0].Archetypes.Add(archetype);Fact(unit,Native<BlueprintFeature>(cache,"b92bfc201c6a79e49afd0b5cfbfc269f"));unit.Resources.Restore(bardResource,1000);
                var ability=(ActivatableAbility)unit.Facts.Add(blueprint.CreateFact(null,unit.Descriptor,null));var logic=blueprint.GetComponent<ActivatableAbilityResourceLogic>();var runtime=Runtime(ability,logic);
                var oldList=Get(logic,"m_FreeBlueprints");var oldSingle=Get(logic,"m_FreeBlueprint");var oldUse=Get(logic,"m_UseListForFreeBlueprints");
                try{
                    var injectedSingle=BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(earlier);Set(logic,"m_FreeBlueprint",injectedSingle);Set(logic,"m_UseListForFreeBlueprints",true);Set(logic,"m_FreeBlueprints",null);
                    using(runtime.RequestEventContext())Check(CourtNullReference(()=>CourtNativeFreeOriginal(logic)),"unpatched native getter reproduces log null-list exception");
                    var slot=new MechanicActionBarSlotActivableAbility{ActivatableAbility=ability};int count=unit.Resources.GetResourceAmount(bardResource);
                    for(int i=0;i<250;i++){Check(slot.GetResource()==count&&ability.IsAvailable,"corrupted-list native actionbar refresh remains live");queries++;}
                    ability.IsOn=true;ability.TryStart();ability.OnNewRound();Check(unit.Resources.GetResourceAmount(bardResource)==count-1,"malformed list without owned free feat still spends normally");ability.TurnOffImmediately();
                    var own=Fact(unit,earlier);Check(ability.ResourceCount==null&&ability.IsAvailable,"surviving earlier free fact remains effective");unit.Facts.Remove(own);
                    var dc=Fact(unit,limitless);Check(ability.ResourceCount==null,"stable DarkCodex limitless identity remains effective");
                    count=unit.Resources.GetResourceAmount(bardResource);ability.IsOn=true;ability.TryStart();ability.OnNewRound();Check(unit.Resources.GetResourceAmount(bardResource)==count,"DarkCodex free performance does not consume a round");ability.TurnOffImmediately();unit.Facts.Remove(dc);
                    Check(slot.GetResource()==count,"removing limitless restores finite count");
                    var ui=new UnitUISettings(unit.Descriptor);Set(unit.Descriptor,"UISettings",ui);ui.TryToInitialize();ui.PostLoad(unit.Descriptor);ui.TryToInitialize();Check(slot.GetResource()==count,"malformed blueprint still safe after native UI post-load and rebuild");
                    Check(Get(logic,"m_FreeBlueprints")==null&&(bool)Get(logic,"m_UseListForFreeBlueprints")&&ReferenceEquals(Get(logic,"m_FreeBlueprint"),injectedSingle),"repair reads state without rewriting shared blueprint");
                    unit.Progression.Classes[0].Archetypes.Remove(archetype);using(runtime.RequestEventContext())Check(CourtNullReference(()=>{var ignored=logic.IsFreeBecauseOfFact;}),"ordinary Bard outside Court retains original getter behavior");unit.Progression.Classes[0].Archetypes.Add(archetype);
                    var required=Get(logic,"m_RequiredResource");Set(logic,"m_RequiredResource",BlueprintReferenceBase.CreateTyped<BlueprintAbilityResourceReference>(Asset<BlueprintAbilityResource>(cache,"CourtBardHeraldicResource01445")));
                    try{using(runtime.RequestEventContext())Check(CourtNullReference(()=>{var ignored=logic.IsFreeBecauseOfFact;}),"unrelated resource remains outside patch scope");}finally{Set(logic,"m_RequiredResource",required);}
                    Set(logic,"m_FreeBlueprints",new[]{BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(earlier)});own=Fact(unit,earlier);Check(ability.ResourceCount==null,"valid nonnull native list retains original semantics");unit.Facts.Remove(own);
                    Set(logic,"m_FreeBlueprints",Array.Empty<BlueprintUnitFactReference>());Check(ability.ResourceCount==count,"valid empty native list remains finite");
                }finally{Set(logic,"m_FreeBlueprints",oldList);Set(logic,"m_FreeBlueprint",oldSingle);Set(logic,"m_UseListForFreeBlueprints",oldUse);}
            }
        }finally{if(prior==null)cache.RemoveCachedBlueprint(limitlessId);else cache.AddCachedBlueprint(limitlessId,prior);cache.RemoveCachedBlueprint(earlier.AssetGuid);}
        PsychicExpansionTests.Inventory["courtBardLevel901445Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-before,["nativeLevel9Identified"]=true,["nativeNullListNegativeControl"]=true,["nativeActionbarRefreshQueries"]=queries,["earlierAndDarkCodexFreeFactsRetained"]=true,["nativeFiniteRoundSpend"]=true,["nativeUiPostLoadRebuild"]=true,["validListsAndOtherBardsUnchanged"]=true,["sharedBlueprintsNotRewritten"]=true};
    }
}
