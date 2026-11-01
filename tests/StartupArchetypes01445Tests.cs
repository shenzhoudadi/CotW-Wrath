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
// Five new archetype bodies, entry/session/dispatcher/report logic execute;
// unrelated module bodies and scene UI/log endpoints are explicit boundaries.
internal static class StartupArchetypes01445Tests
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
    static readonly string[] Modules={"CourtBard01445","NatureBondedMagus01445","SageCounselor01445","GraveWarden01445","DrillSergeant01445"};
    static readonly string[] Missing={"6b81d1245d2b45b49bb98a8e7d32c64d","bad8638d40639d04fa2f80a1cac67d6b","332362f3bd39ebe46a740a36960fdcb4","0413915f355a38146bc6ad40cdf27b3f","d87e2f6a9278ac04caeb0f93eff95fcb"};
    static BlueprintsCache cache;
    static Harmony patchOwner;
    static Dictionary<string,int> calls;
    static int checks,cacheCallbacks,localeCallbacks,installs,probePosts,allPasses,optionalPasses,scenarios,totalAll,totalInstalls,totalProbes;
    static void Check(bool ok,string why){if(!ok)throw new Exception("StartupArchetypes01445: "+why);checks++;}
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
        register=()=>{Add(name);if(Modules.Any(m=>Name(m)==name)){try{actual();}catch(Exception error){Console.WriteLine("EXPECTED/DIAGNOSTIC module failure "+name+": "+error);throw;}}};return true;
    }
    static void CountAll(){allPasses++;totalAll++;}
    static void CountOptional()=>optionalPasses++;
    static bool Skip()=>false;
    static bool Absent(ref bool __result){__result=false;return false;}
    static bool Investigator(ref bool __result){__result=false;return false;}
    static bool Medium(){Add(Name("MediumIntegration"));return false;}
    static bool Way(){Add(Name("WayOfTheShieldIntegration"));return false;}
    static bool UI(Harmony harmony)
    {
        installs++;totalInstalls++;harmony.Patch(AccessTools.Method(typeof(StartupArchetypes01445Tests),nameof(Probe)),postfix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(ProbePost)));return false;
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
    static void Recovery(BlueprintsCache parent,int n)
    {
        cache=(BlueprintsCache)Call(typeof(Archetypes01445Tests),"Fresh",parent);calls=new Dictionary<string,int>();installs=0;scenarios++;
        Static(typeof(ModEntry),"registrationInProgress",0);foreach(string field in new[]{"registrationCompleted","deferredPatchesAttempted"})Static(typeof(ModEntry),field,false);
        var states=(Dictionary<string,string>)Static(typeof(ModEntry),"moduleStates");states.Clear();foreach(var p in (Dictionary<string,string>)Call(T("ModuleCatalog"),"InitialStates"))states.Add(p.Key,p.Value);
        patchOwner=new Harmony("CotW.StartupArchetypes01445.Probe."+n);Static(typeof(ModEntry),"psychicHarmony",patchOwner);Static(T("PsychicCasting"),"RuntimePatchesAvailable",true);
        var native=cache.Load(BlueprintGuid.Parse(Missing[n]));Check(native!=null,"actual missing dependency exists "+n);cache.RemoveCachedBlueprint(native.AssetGuid);
        Callback(false);Check(Pending()==1&&Completed()&&Status(Name(Modules[n]))=="Failed","real CacheReady rejects missing dependency "+n);
        foreach(var m in Modules)Check(Count(Name(m))==1,"one initial attempt "+m);
        var identities=Modules.Select((m,i)=>cache.Load(Id((string)Static(T(m),"RootToken")))).ToArray();Check(identities[n]==null&&identities.Where((bp,i)=>i!=n).All(bp=>bp!=null),"only failing archetype absent; independent later modules publish");
        Callback(true);Check(Pending()==1&&Count(Name(Modules[n]))==2,"real LocalizationReady retries incomplete module only "+n);
        cache.AddCachedBlueprint(native.AssetGuid,native);Callback(true);Check(Pending()==0&&Status(Name(Modules[n]))=="Registered"&&Count(Name(Modules[n]))==(n==4?4:3),"restored actual dependency recovers "+n);
        foreach(var m in Modules.Where((m,i)=>i!=n))Check(m=="DrillSergeant01445"||Count(Name(m))==1,"already successful initial body skipped (Drill also reconciles optional pool) "+m);
        for(int i=0;i<Modules.Length;i++){var root=(BlueprintArchetype)cache.Load(Id((string)Static(T(Modules[i]),"RootToken")));Check(root!=null&&root.GetParentClass().Archetypes.Count(a=>a==root)==1,"one actual parent archetype entry "+i);if(i!=n)Check(ReferenceEquals(root,identities[i]),"successful blueprint identity retained "+i);}
        var before=calls.ToDictionary(p=>p.Key,p=>p.Value);Callback(false);Callback(true);foreach(var m in Modules)Check(Count(Name(m))==before[Name(m)]+(m=="DrillSergeant01445"?1:0),"subsequent callback skips initial success and reconciles Drill pool "+m);
        if(n==4)LateRecovery();
        Check(installs==1,"deferred installer called once across real failure callbacks");patchOwner.UnpatchAll(patchOwner.Id);
    }
    static void LateRecovery()
    {
        var pool=(Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("d87e2f6a9278ac04caeb0f93eff95fcb"));
        var old=(BlueprintFeatureReference[])Get(pool,"m_AllFeatures");
        var late=new BlueprintFeature{AssetGuid=Id("Startup01445LateTeamwork"),name="Startup01445LateTeamwork"};late.OnEnable();cache.AddCachedBlueprint(late.AssetGuid,late);
        Set(pool,"m_AllFeatures",old.Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(late)}).ToArray());
        var collision=new BlueprintFeature{AssetGuid=Id("DrillSergeant"+late.AssetGuid+"Marker01445"),name="external-collision"};cache.AddCachedBlueprint(collision.AssetGuid,collision);
        Callback(true);Check(Status(Name("DrillSergeant01445"))=="Failed"&&ReferenceEquals(cache.Load(collision.AssetGuid),collision),"late real callback rejects and preserves foreign GUID");
        var selections=new[]{"Tactician","GreaterTactician","MasterTactician"}.Select(n=>(Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection)cache.Load(Id("DrillSergeant"+n+"01445"))).ToArray();
        Check(selections.All(s=>!s.AllFeatures.Contains(late)),"late callback failure leaves all three choices unchanged");
        cache.RemoveCachedBlueprint(collision.AssetGuid);Callback(true);
        Check(Status(Name("DrillSergeant01445"))=="Registered"&&selections.All(s=>s.AllFeatures.Count(f=>f==late)==1),"real LocalizationReady recovers late pool after conflict removed");
        var ids=new[]{"Toggle","Marker","Shared"}.Select(s=>Id("DrillSergeant"+late.AssetGuid+s+"01445")).ToArray();var resources=ids.Select(cache.Load).ToArray();
        Check(resources.All(x=>x!=null),"late real callback publishes all three actual resources");
        Callback(true);Check(ids.Select(cache.Load).SequenceEqual(resources)&&selections.All(s=>s.AllFeatures.Count(f=>f==late)==1),"repeated ready preserves late GUIDs and unique selection entries");
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks=cacheCallbacks=localeCallbacks=installs=probePosts=optionalPasses=scenarios=totalAll=totalInstalls=totalProbes=0;
        var entry=typeof(ModEntry);var fields=new[]{"modification","psychicHarmony","registrationInProgress","registrationCompleted","deferredPatchesAttempted"};var saved=fields.ToDictionary(n=>n,n=>Static(entry,n));var statuses=(Dictionary<string,string>)Static(entry,"moduleStates");var oldStates=new Dictionary<string,string>(statuses);var original=ResourcesLibrary.BlueprintsCache;var runtime=Static(T("PsychicCasting"),"RuntimePatchesAvailable");
        var snapshotType=typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot",BindingFlags.NonPublic);var snapshot=Activator.CreateInstance(snapshotType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{parent},null);
        var h=new Harmony("CotW.StartupArchetypes01445.Boundaries");
        try {
            var mod=(OwlcatModification)FormatterServices.GetUninitializedObject(typeof(OwlcatModification));Set(mod,"Logger",FormatterServices.GetUninitializedObject(typeof(Owlcat.Runtime.Core.Logging.LogChannel)));Static(entry,"modification",mod);
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterModule"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(ModuleBoundary)));
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterAll"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(CountAll)));
            h.Patch(AccessTools.Method(T("BlueprintRegistration"),"RegisterOptional"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(CountOptional)));
            h.Patch(AccessTools.Method(T("ExtraInvestigatorTalent"),"Register"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(Investigator)));
            h.Patch(AccessTools.Method(T("ProviderResourceFeats"),"Available"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(Absent)));
            h.Patch(AccessTools.Method(T("MediumIntegration"),"Register"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(Medium)));h.Patch(AccessTools.Method(T("WayOfTheShieldIntegration"),"Register"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(Way)));
            h.Patch(AccessTools.Method(T("PsychicUiPatches"),"Install"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(UI)));
            foreach(string method in new[]{"WriteDiagnostics","TryMenu"})h.Patch(AccessTools.Method(entry,method),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(Skip)));
            h.Patch(AccessTools.Method(typeof(OwlcatModification),"ApplyLocalization"),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(Skip)));
            foreach(string method in new[]{"Log","Error"})h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),method,new[]{typeof(string),typeof(object[])}),prefix:new HarmonyMethod(typeof(StartupArchetypes01445Tests),nameof(Skip)));
            for(int n=0;n<5;n++)Recovery(parent,n);AccessTools.Method(snapshotType,"Verify").Invoke(snapshot,null);Check(true,"original complete parent graph unchanged");
            Check(scenarios==5&&cacheCallbacks==10&&localeCallbacks==18,"five actual-module callback recovery scenarios executed");
            PsychicExpansionTests.Inventory["startupArchetypes01445Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["scenarios"]=scenarios,["cacheReadyCallbacks"]=cacheCallbacks,["localizationReadyCallbacks"]=localeCallbacks,["patchInstallerCalls"]=totalInstalls,["actualNewModuleBodies"]=true,["completedModulesSkipped"]=true,["lateDrillRealCallbackRecovery"]=true,["nativeDependencyRecovery"]=true,["originalParentPreserved"]=true,["boundary"]="Production ModEntry CacheReady/LocalizationReady postfixes, ledger, dispatcher and all five new module bodies execute. One actual native dependency per module is removed and restored; two failed calls precede recovery. Unrelated modules, bundled adapters, IO and UI installer are controlled boundaries; a real harmless Harmony probe confirms one deferred installation. Recovery requires a later ready/settings callback. Private graph conflicts, arbitrary provider changes and native cache/scene scheduling are outside these checks."};
            Console.WriteLine("PASS: "+checks+" StartupArchetypes01445 real-entry contracts.");return checks;
        } finally {
            patchOwner?.UnpatchAll(patchOwner.Id);h.UnpatchAll(h.Id);foreach(var p in saved)Static(entry,p.Key,p.Value);statuses.Clear();foreach(var p in oldStates)statuses.Add(p.Key,p.Value);Static(T("PsychicCasting"),"RuntimePatchesAvailable",runtime);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,original);cache=null;
        }
    }
}
