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
// Amplification and Terror bodies, entry/session/dispatcher/report logic execute;
// unrelated module bodies and scene UI/log endpoints are explicit boundaries.
internal static class StartupRetry01444Tests
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
    const string Icon="0a2f7c6aa81bc6548ac7780d8b70bcbc";
    static string Amp=>Name("PsychicAmplifications");static string Terror=>Name("TerrorWeaver01444");
    static BlueprintsCache cache;
    static Harmony patchOwner;
    static Dictionary<string,int> calls;
    static int checks,cacheCallbacks,localeCallbacks,installs,probePosts,allPasses,optionalPasses,nested,scenarios,interrupted,totalAll,totalInstalls,totalProbes,totalAmp,totalTerror,extraAttempts;
    static bool extraFailure;
    static bool interrupt,nest;
    static void Check(bool ok,string why){if(!ok)throw new Exception("StartupRetry01444: "+why);checks++;}
    static void Add(string name){calls[name]=calls.TryGetValue(name,out var n)?n+1:1;}
    static int Count(string name)=>calls.TryGetValue(name,out var n)?n:0;
    static void Callback(bool locale)
    {
        string name=locale?"LocalizationReadyPatch":"CacheReadyPatch";if(locale)localeCallbacks++;else cacheCallbacks++;
        var type=typeof(ModEntry).GetNestedType(name,BindingFlags.NonPublic);Call(type,"Postfix",locale?Array.Empty<object>():new object[]{cache});
    }
    static bool ModuleBoundary(string name,ref Action register)
    {
        if(interrupt&&name=="Extended Bane"){interrupt=false;interrupted++;throw new InvalidOperationException("01444 initial traversal infrastructure interruption");}
        if(name==Amp||name==Terror)return true;
        register=()=>{
            Add(name);
            if(nest&&name=="Steadfast Personality"){nest=false;nested+=2;Callback(false);Callback(true);}
        };return true;
    }
    static void CountAmp(){Add(Amp);totalAmp++;}
    static void CountTerror(){Add(Terror);totalTerror++;}
    static void CountAll(){allPasses++;totalAll++;}
    static void CountOptional()=>optionalPasses++;
    static bool Skip()=>false;
    static bool Absent(ref bool __result){__result=false;return false;}
    static bool Investigator(ref bool __result)
    {
        if(extraFailure){extraAttempts++;if(extraAttempts<3)throw new InvalidOperationException("01444 optional provider temporarily unavailable");__result=true;return false;}
        __result=false;return false;
    }
    static bool Medium(){Add(Name("MediumIntegration"));return false;}
    static bool Way(){Add(Name("WayOfTheShieldIntegration"));return false;}
    static bool UI(Harmony harmony)
    {
        installs++;totalInstalls++;harmony.Patch(AccessTools.Method(typeof(StartupRetry01444Tests),nameof(Probe)),postfix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(ProbePost)));return false;
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
    static void Configure(BlueprintsCache parent)
    {
        cache=(BlueprintsCache)Call(typeof(RegistrationOrder01444Tests),"Cold",parent);
        calls=new Dictionary<string,int>();installs=probePosts=allPasses=extraAttempts=0;interrupt=extraFailure=false;nest=true;
        Static(typeof(ModEntry),"registrationInProgress",0);foreach(string field in new[]{"registrationCompleted","deferredPatchesAttempted"})Static(typeof(ModEntry),field,false);
        var states=(Dictionary<string,string>)Static(typeof(ModEntry),"moduleStates");states.Clear();foreach(var p in (Dictionary<string,string>)Call(T("ModuleCatalog"),"InitialStates"))states.Add(p.Key,p.Value);
        patchOwner=new Harmony("CotW.StartupRetry01444.Probe."+(++scenarios));Static(typeof(ModEntry),"psychicHarmony",patchOwner);Static(T("PsychicCasting"),"RuntimePatchesAvailable",true);
    }
    static SimpleBlueprint[] Published()
    {
        var tokens=(string[])Static(T("TerrorWeaver01444"),"OwnTokens");return tokens.Select(t=>cache.Load(Id(t))).ToArray();
    }
    static void Healthy()
    {
        Check(Pending()==0&&Completed()&&Status(Amp)=="Registered"&&Status(Terror)=="Registered","entry reaches healthy completion and transitions failed statuses to Registered");
        var assets=Published();Check(assets.All(x=>x!=null)&&assets.Select(x=>x.AssetGuid).Distinct().Count()==9,"all nine actual Terror assets published once");
        var cls=((BlueprintArchetype)assets[0]).GetParentClass();Check(cls.Archetypes.Count(x=>x.AssetGuid==assets[0].AssetGuid)==1,"actual Psychic archetype options contain exactly one Terror Weaver");
        foreach(string name in new[]{"Steadfast Personality",Name("PrimalSorcerer01444"),Name("Rake01444"),Name("HedgeWitch01443"),Name("MediumIntegration"),Name("WayOfTheShieldIntegration")})Check(Count(name)==1,"successful initial publication body never retried: "+name);
        var probe=AccessTools.Method(typeof(StartupRetry01444Tests),nameof(Probe));Check(installs==1&&Harmony.GetPatchInfo(probe).Postfixes.Count(p=>p.owner==patchOwner.Id)==1,"retry never reinstalls deferred patches; one actual Harmony probe owner");
        probe.Invoke(null,new object[]{1});Check(probePosts==1,"one actual installed harmless postfix executes once");
        int amp=Count(Amp),terror=Count(Terror),passes=allPasses;Callback(false);Callback(true);
        Check(Count(Amp)==amp&&Count(Terror)==terror&&allPasses==passes&&Published().Select((x,i)=>ReferenceEquals(x,assets[i])).All(x=>x),"extra ready callbacks skip successful core modules and preserve all blueprint identities");
        Check(installs==1,"extra ready callbacks do not attempt patch installation");
    }
    static void DependencyFailure(BlueprintsCache parent,bool recoverByLocale)
    {
        Configure(parent);var icon=cache.Load(BlueprintGuid.Parse(Icon));Check(icon!=null,"actual native amplification icon dependency present before injected absence");cache.RemoveCachedBlueprint(icon.AssetGuid);
        if(recoverByLocale){Callback(true);Check(installs==0&&Count(Amp)==0,"localization before native cache readiness cannot prematurely install/register");}
        Callback(false);Check(Completed()&&Pending()==2&&Status(Amp)=="Failed"&&Status(Terror)=="Failed","real CacheReady entry captures actual missing-dependency failures");
        Check(Published().All(x=>x==null)&&Count(Amp)==1&&Count(Terror)==1,"actual failed producer/dependent publish no Terror graph");
        Callback(recoverByLocale);Check(Pending()==2&&Count(Amp)==2&&Count(Terror)==2&&allPasses==1,"persistent dependency failure retries only the two pending bodies, not RegisterAll");
        cache.AddCachedBlueprint(icon.AssetGuid,icon);Callback(recoverByLocale);
        Check(Count(Amp)==3&&Count(Terror)==3&&allPasses==1,"dependency restoration recovers via the real ready callback without repeating RegisterAll");Healthy();patchOwner.UnpatchAll(patchOwner.Id);
    }
    static void Interrupted(BlueprintsCache parent)
    {
        Configure(parent);interrupt=true;Callback(false);
        Check(!Completed()&&Pending()==0&&Count("Steadfast Personality")==1&&Count(Amp)==0,"infrastructure interruption leaves later unseen modules incomplete");
        Callback(true);Check(allPasses==2&&Count("Steadfast Personality")==1&&Count(Amp)==1&&Count(Terror)==1,"real LocalizationReady resumes unfinished traversal, skipping committed successes");Healthy();patchOwner.UnpatchAll(patchOwner.Id);
    }
    static void OptionalRecovery(BlueprintsCache parent)
    {
        Configure(parent);extraFailure=true;Callback(false);Check(Pending()==1&&Status(Name("ExtraInvestigatorTalent"))=="Failed","optional publication failure participates in the real initial ledger");
        Callback(true);Check(extraAttempts==3&&Pending()==0&&Status(Name("ExtraInvestigatorTalent"))=="Registered","normal optional reconciliation success retires its previously failed initial action");
        Check(allPasses==1&&Count(Amp)==1&&Count(Terror)==1,"optional recovery never reruns successful core bodies");Healthy();patchOwner.UnpatchAll(patchOwner.Id);extraFailure=false;
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks=cacheCallbacks=localeCallbacks=installs=probePosts=optionalPasses=nested=scenarios=interrupted=totalAll=totalInstalls=totalProbes=totalAmp=totalTerror=0;
        var entry=typeof(ModEntry);var fields=new[]{"modification","psychicHarmony","registrationInProgress","registrationCompleted","deferredPatchesAttempted"};var saved=fields.ToDictionary(n=>n,n=>Static(entry,n));var statuses=(Dictionary<string,string>)Static(entry,"moduleStates");var oldStates=new Dictionary<string,string>(statuses);var original=ResourcesLibrary.BlueprintsCache;var runtime=Static(T("PsychicCasting"),"RuntimePatchesAvailable");
        var snapshotType=typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot",BindingFlags.NonPublic);var snapshot=Activator.CreateInstance(snapshotType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{parent},null);
        var h=new Harmony("CotW.StartupRetry01444.Boundaries");
        try {
            var mod=(OwlcatModification)FormatterServices.GetUninitializedObject(typeof(OwlcatModification));Set(mod,"Logger",FormatterServices.GetUninitializedObject(typeof(Owlcat.Runtime.Core.Logging.LogChannel)));Static(entry,"modification",mod);
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterModule"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(ModuleBoundary)));ScopedAvailable01463Tests.Bind(h,T("BlueprintRegistration"),"ScopeStartupRetry01444Tests");
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterAll"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(CountAll)));
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterOptional"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(CountOptional)));
            h.Patch(AccessTools.Method(T("PsychicAmplifications"),"Register"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(CountAmp)));
            h.Patch(AccessTools.Method(T("TerrorWeaver01444"),"Register"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(CountTerror)));
            h.Patch(AccessTools.Method(T("ExtraInvestigatorTalent"),"Register"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(Investigator)));
            h.Patch(AccessTools.Method(T("ProviderResourceFeats"),"Available"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(Absent)));
            h.Patch(AccessTools.Method(T("MediumIntegration"),"Register"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(Medium)));h.Patch(AccessTools.Method(T("WayOfTheShieldIntegration"),"Register"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(Way)));
            h.Patch(AccessTools.Method(T("PsychicUiPatches"),"Install"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(UI)));
            foreach(string method in new[]{"WriteDiagnostics","TryMenu"})h.Patch(AccessTools.Method(entry,method),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(Skip)));
            h.Patch(AccessTools.Method(typeof(OwlcatModification),"ApplyLocalization"),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(Skip)));
            foreach(string method in new[]{"Log","Error"})h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),method,new[]{typeof(string),typeof(object[])}),prefix:new HarmonyMethod(typeof(StartupRetry01444Tests),nameof(Skip)));
            DependencyFailure(parent,true);DependencyFailure(parent,false);Interrupted(parent);OptionalRecovery(parent);AccessTools.Method(snapshotType,"Verify").Invoke(snapshot,null);Check(true,"original complete parent graph unchanged");
            Check(scenarios==4&&nested==8&&interrupted==1&&cacheCallbacks>0&&localeCallbacks>0,"all real-entry failure/recovery/reentrancy scenarios executed");
            PsychicExpansionTests.Inventory["startupRetry01444Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["scenarios"]=scenarios,["cacheReadyCallbacks"]=cacheCallbacks,["localizationReadyCallbacks"]=localeCallbacks,["reentrantCallbacks"]=nested,["interruptedTraversals"]=interrupted,["actualRegisterAllPasses"]=totalAll,["actualAmplificationBodies"]=totalAmp,["actualTerrorBodies"]=totalTerror,["patchInstallerCalls"]=totalInstalls,["actualProbeExecutions"]=totalProbes,["realEntryRecovery"]=true,["actualAmplificationAndTerrorBodies"]=true,["completedInitialModulesSkipped"]=true,["patchInstallationOncePerStartup"]=true,["actualHarmonyProbe"]=true,["originalParentGraphPreserved"]=true,["boundary"]="The production CacheReadyPatch.Postfix and LocalizationReadyPatch.Postfix are invoked directly; their ModEntry registration, state ledger, initial dispatcher, failed-only retry and normal optional reconciliation execute. Actual amplification/Terror bodies fail on a removed real native icon dependency and recover after restoration. Unrelated module bodies, bundled registration endpoints, menu/diagnostic/log/localization IO and the native UI installer are controlled boundaries. The UI installer boundary installs a real harmless Harmony probe and counts attempts; existing integrated-startup suites cover actual native UI label patch installation. This is callback-path verification, not native cache.Init/async localization scheduling or game scene execution. Optional provider reconciliation remains callable after initial successes. Permanent early patch/preparation failures and author-graph conflicts are not reset; recovery requires a later ready/settings callback, with no timer or polling."};
            Console.WriteLine("PASS: "+checks+" StartupRetry01444 real-entry contracts.");return checks;
        } finally {
            patchOwner?.UnpatchAll(patchOwner.Id);h.UnpatchAll(h.Id);foreach(var p in saved)Static(entry,p.Key,p.Value);statuses.Clear();foreach(var p in oldStates)statuses.Add(p.Key,p.Value);Static(T("PsychicCasting"),"RuntimePatchesAvailable",runtime);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,original);cache=null;
        }
    }
}
