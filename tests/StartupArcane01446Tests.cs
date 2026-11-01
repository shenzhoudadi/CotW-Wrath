using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;
using CallOfTheWild.Wrath.Prototype;

// Invoke the unchanged real CacheReady/LocalizationReady postfix entry points.
// Two new arcane module bodies, entry/session/dispatcher/report logic execute;
// unrelated module bodies and scene UI/log endpoints are explicit boundaries.
internal static class StartupArcane01446Tests
{
    static readonly Assembly Main=typeof(RakeStance01444).Assembly;
    static Type T(string name)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+name,true);
    static object Call(Type type,string method,params object[] args)=>AccessTools.Method(type,method).Invoke(null,args);
    static object Get(object value,string field)=>AccessTools.Field(value.GetType(),field).GetValue(value);
    static void Set(object value,string field,object data)=>AccessTools.Field(value.GetType(),field).SetValue(value,data);
    static object Static(Type type,string field)=>AccessTools.Field(type,field).GetValue(null);
    static void Static(Type type,string field,object value)=>AccessTools.Field(type,field).SetValue(null,value);
    static BlueprintGuid Id(string token)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",token);
    static string Name(string type)=>(string)Static(T(type),"ModuleName");
    static readonly string[] Modules={"BloodlineMutations01446","Admixture01446"};
    static readonly string[] Missing={"a1de1e4f92195b442adb946f0e2b9d4e","52135eada006e9045a848cd659749608"};
    static BlueprintsCache cache;
    static Harmony patchOwner;
    static Dictionary<string,int> calls;
    static int checks,cacheCallbacks,localeCallbacks,installs,probePosts,allPasses,optionalPasses,scenarios,totalAll,totalInstalls,totalProbes;
    static void Check(bool ok,string why){if(!ok)throw new Exception("StartupArcane01446: "+why);checks++;}
    static void Add(string name){calls[name]=calls.TryGetValue(name,out var n)?n+1:1;}
    static int Count(string name)=>calls.TryGetValue(name,out var n)?n:0;
    static void Callback(bool locale)
    {
        string name=locale?"LocalizationReadyPatch":"CacheReadyPatch";if(locale)localeCallbacks++;else cacheCallbacks++;
        var type=typeof(ModEntry).GetNestedType(name,BindingFlags.NonPublic);Call(type,"Postfix",locale?Array.Empty<object>():new object[]{cache});
    }
    static bool ModuleBoundary(string name,ref Action register)
    {
        var actual=register;
        register=()=>{if(inOptional&&name==Name(Modules[0]))optionalHavoc++;else Add(name);if(Modules.Any(m=>Name(m)==name)){try{actual();}catch(Exception error){Console.WriteLine("EXPECTED/DIAGNOSTIC module failure "+name+": "+error);throw;}}};return true;
    }
    static void CountAll(){allPasses++;totalAll++;}
    static bool inOptional;static int optionalHavoc;
    static void CountOptional(){optionalPasses++;inOptional=true;}
    static void EndOptional()=>inOptional=false;
    static bool Skip()=>false;
    static bool Absent(ref bool __result){__result=false;return false;}
    static bool Investigator(ref bool __result){__result=false;return false;}
    static bool Medium(){Add(Name("MediumIntegration"));return false;}
    static bool Way(){Add(Name("WayOfTheShieldIntegration"));return false;}
    static bool UI(Harmony harmony)
    {
        installs++;totalInstalls++;harmony.Patch(AccessTools.Method(typeof(StartupArcane01446Tests),nameof(Probe)),postfix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(ProbePost)));return false;
    }
    static int Probe(int value)=>value+1;
    static void ProbePost(){probePosts++;totalProbes++;}
    static object CurrentState()
    {
        var table=Static(typeof(ModEntry),"registrations");var args=new object[]{cache,null};bool exists=(bool)AccessTools.Method(table.GetType(),"TryGetValue").Invoke(table,args);Check(exists,"real entry creates its per-cache publication state");return args[1];
    }
    static int Pending(){var state=CurrentState();return (int)AccessTools.Property(state.GetType(),"PendingCount").GetValue(state,null);}
    static bool Completed()=>(bool)Get(CurrentState(),"InitialDispatchCompleted");
    static string Status(string name)=>((Dictionary<string,string>)Static(typeof(ModEntry),"moduleStates"))[name];
    static bool silentFailure;static BlueprintGuid silentId;static int silentFailures;
    static bool SilentWrite(BlueprintGuid guid){if(silentFailure&&guid==silentId){silentFailures++;return false;}return true;}
    static void Recovery(BlueprintsCache parent,int n,bool silent=false,bool missingProducer=false)
    {
        cache=(BlueprintsCache)Call(typeof(Archetypes01445Tests),"Fresh",parent);calls=new Dictionary<string,int>();installs=0;scenarios++;
        Static(typeof(ModEntry),"registrationInProgress",0);foreach(string field in new[]{"registrationCompleted","deferredPatchesAttempted"})Static(typeof(ModEntry),field,false);
        var states=(Dictionary<string,string>)Static(typeof(ModEntry),"moduleStates");states.Clear();foreach(var p in (Dictionary<string,string>)Call(T("ModuleCatalog"),"InitialStates"))states.Add(p.Key,p.Value);
        patchOwner=new Harmony("CotW.StartupArcane01446.Probe."+n);Static(typeof(ModEntry),"psychicHarmony",patchOwner);Static(T("PsychicCasting"),"RuntimePatchesAvailable",true);
        if(cache.Load(Id("PrimalSorcererArchetype01444"))==null)Call(T("PrimalSorcerer01444"),"Register",cache);
        var native=cache.Load(missingProducer?Id("PrimalSorcererArchetype01444"):BlueprintGuid.Parse(Missing[n]));Check(native!=null,"actual missing dependency exists "+n);if(!silent)cache.RemoveCachedBlueprint(native.AssetGuid);else{silentFailure=true;silentId=Id((string)Static(T(Modules[n]),"RootToken"));}
        Callback(false);Check(Pending()==1&&Completed()&&Status(Name(Modules[n]))=="Failed","real CacheReady rejects missing dependency "+n);
        foreach(var m in Modules)Check(Count(Name(m))==1,"one initial attempt "+m);
        var identities=Modules.Select((m,i)=>cache.Load(Id((string)Static(T(m),"RootToken")))).ToArray();Check(identities[n]==null&&identities.Where((bp,i)=>i!=n).All(bp=>bp!=null),"only failing module absent; independent later modules publish");
        Callback(true);Check(Pending()==1&&Count(Name(Modules[n]))==2,"real LocalizationReady retries incomplete module only "+n);
        if(!silent)cache.AddCachedBlueprint(native.AssetGuid,native);silentFailure=false;Callback(true);Check(Pending()==0&&Status(Name(Modules[n]))=="Registered"&&Count(Name(Modules[n]))==3,"restored actual dependency recovers "+n);
        foreach(var m in Modules.Where((m,i)=>i!=n))Check(Count(Name(m))==1,"already successful initial body skipped (Drill also reconciles optional pool) "+m);
        for(int i=0;i<Modules.Length;i++){var root=cache.Load(Id((string)Static(T(Modules[i]),"RootToken")));Check(root!=null,"actual module root published "+i);if(i!=n)Check(ReferenceEquals(root,identities[i]),"successful blueprint identity retained "+i);}
        var before=calls.ToDictionary(p=>p.Key,p=>p.Value);Callback(false);Callback(true);foreach(var m in Modules)Check(Count(Name(m))==before[Name(m)],"subsequent callback skips initial success and reconciles Drill pool "+m);
        Check(installs==1,"deferred installer called once across real failure callbacks");patchOwner.UnpatchAll(patchOwner.Id);
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks=cacheCallbacks=localeCallbacks=installs=probePosts=optionalPasses=scenarios=totalAll=totalInstalls=totalProbes=0;
        var entry=typeof(ModEntry);var fields=new[]{"modification","psychicHarmony","registrationInProgress","registrationCompleted","deferredPatchesAttempted"};var saved=fields.ToDictionary(n=>n,n=>Static(entry,n));var statuses=(Dictionary<string,string>)Static(entry,"moduleStates");var oldStates=new Dictionary<string,string>(statuses);var original=ResourcesLibrary.BlueprintsCache;var runtime=Static(T("PsychicCasting"),"RuntimePatchesAvailable");
        var snapshotType=typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot",BindingFlags.NonPublic);var snapshot=Activator.CreateInstance(snapshotType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{parent},null);
        var h=new Harmony("CotW.StartupArcane01446.Boundaries");
        try {
            var mod=(OwlcatModification)FormatterServices.GetUninitializedObject(typeof(OwlcatModification));Set(mod,"Logger",FormatterServices.GetUninitializedObject(typeof(Owlcat.Runtime.Core.Logging.LogChannel)));Static(entry,"modification",mod);
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterModule"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(ModuleBoundary)));
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterAll"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(CountAll)));
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterOptional"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(CountOptional)),postfix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(EndOptional)));
            h.Patch(AccessTools.Method(T("ExtraInvestigatorTalent"),"Register"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(Investigator)));
            h.Patch(AccessTools.Method(T("ProviderResourceFeats"),"Available"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(Absent)));
            h.Patch(AccessTools.Method(T("MediumIntegration"),"Register"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(Medium)));h.Patch(AccessTools.Method(T("WayOfTheShieldIntegration"),"Register"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(Way)));
            h.Patch(AccessTools.Method(T("PsychicUiPatches"),"Install"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(UI)));
            foreach(string method in new[]{"WriteDiagnostics","TryMenu"})h.Patch(AccessTools.Method(entry,method),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(Skip)));
            h.Patch(AccessTools.Method(typeof(OwlcatModification),"ApplyLocalization"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(Skip)));
            foreach(string method in new[]{"Log","Error"})h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),method,new[]{typeof(string),typeof(object[])}),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(Skip)));
            h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),prefix:new HarmonyMethod(typeof(StartupArcane01446Tests),nameof(SilentWrite)));
            for(int n=0;n<2;n++){Recovery(parent,n);Recovery(parent,n,true);}Recovery(parent,0,false,true);AccessTools.Method(snapshotType,"Verify").Invoke(snapshot,null);Check(true,"original complete parent graph unchanged");
            Check(scenarios==5&&cacheCallbacks==10&&localeCallbacks==15&&silentFailures==4,"both actual-module callback recovery scenarios executed");
            PsychicExpansionTests.Inventory["startupArcane01446Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["scenarios"]=scenarios,["cacheReadyCallbacks"]=cacheCallbacks,["localizationReadyCallbacks"]=localeCallbacks,["patchInstallerCalls"]=totalInstalls,["actualNewModuleBodies"]=true,["normalReturnMissingOutputCases"]=2,["actualPrimalProducerDependencyRecovery"]=true,["successfulOptionalHavocReconciliation"]=optionalHavoc>0,["completedModulesSkipped"]=true,["nativeDependencyRecovery"]=true,["originalParentPreserved"]=true,["boundary"]="Production ModEntry CacheReady/LocalizationReady postfixes, ledger, dispatcher and both new module bodies execute. One actual native dependency per module and the actual Primal producer root are removed and restored; two failed calls precede recovery. Unrelated modules, bundled adapters, IO and UI installer are controlled boundaries; a real harmless Harmony probe confirms one deferred installation. Recovery requires a later ready/settings callback. Private graph conflicts, arbitrary provider changes and native cache/scene scheduling are outside these checks."};
            Console.WriteLine("PASS: "+checks+" StartupArcane01446 real-entry contracts.");return checks;
        } finally {
            patchOwner?.UnpatchAll(patchOwner.Id);h.UnpatchAll(h.Id);foreach(var p in saved)Static(entry,p.Key,p.Value);statuses.Clear();foreach(var p in oldStates)statuses.Add(p.Key,p.Value);Static(T("PsychicCasting"),"RuntimePatchesAvailable",runtime);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,original);cache=null;
        }
    }
}
