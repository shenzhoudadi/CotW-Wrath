using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.FactLogic;
using CallOfTheWild.Wrath.Prototype;

// The dispatcher model checks declared producer order for EVERY reached named
// module; it never fabricates a successful production registration report.
// Two real dependent bodies then run on an isolated cache without the122
// amplification blueprints/progression links, so warm fixtures cannot mask it.
internal static class RegistrationOrder01444Tests
{
    static readonly Assembly Main=typeof(RakeStance01444).Assembly;
    static Type Type(string n)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+n,true);
    static object Call(Type t,string m,params object[] a)=>AccessTools.Method(t,m).Invoke(null,a);
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static string Const(string t,string f)=>(string)AccessTools.Field(Type(t),f).GetValue(null);
    static BlueprintGuid Id(string token)=>(BlueprintGuid)Call(Type("PsychicAssets"),"Id",token);
    static string Name(string t)=>Const(t,"ModuleName");
    static readonly string Amp=Name("PsychicAmplifications"),Terror=Name("TerrorWeaver01444");
    static JObject manifest;
    static HashSet<string> ready,seen;
    static List<string> trace;
    static bool model,inject;
    static BlueprintsCache planCache058;
    static int checks,entries,optionalCalls,coldRuns,preFixFailures,failedProvider,retries;
    static void Check(bool ok,string why){if(!ok)throw new Exception("RegistrationOrder01444: "+why);checks++;}
    static bool OptionalPrefix(){if(model)optionalCalls++;return true;}
    static bool ModulePrefix(string name,ref Action register)
    {
        if(!model){if(name!=Amp&&name!=Terror)return false;if(name==Amp&&inject){inject=false;register=()=>{throw new InvalidOperationException("01444 injected amplification producer failure");};}return true;}
        Check(manifest["modules"][name]!=null,"every dispatcher entry has reviewed dependency metadata: "+name);
        foreach(string dependency in manifest["modules"][name].Values<string>())Check(ready.Contains(dependency),"producer must precede consumer: "+dependency+" -> "+name);
        ready.Add(name);seen.Add(name);trace.Add(name);entries++;
        var delayed=manifest["delayedOptionalRootTokens"]?[name] as JArray;
        if(delayed!=null)foreach(string token in delayed.Values<string>())planCache058.AddCachedBlueprint(Id(token),new BlueprintFeature{AssetGuid=Id(token),name="OrderDelayedReachabilityMarker058"});
        return false;
    }
    static bool DirectInvestigator(ref bool __result)
    {
        if(model){string name=Name("ExtraInvestigatorTalent");Check(manifest["modules"][name]!=null,"direct optional module also declared");seen.Add(name);trace.Add(name);ready.Add(name);entries++;}
        __result=false;return false;
    }
    static bool ProviderAvailable(ref bool __result){if(!model)return true;__result=true;return false;}
    static bool AvailablePrefix(string name){Action unused=()=>{};return ModulePrefix(name,ref unused);}
    static BlueprintsCache PlanCache()
    {
        var c=new BlueprintsCache();
        foreach(string token in manifest["optionalRootTokens"].Values<string>())c.AddCachedBlueprint(Id(token),new BlueprintFeature{AssetGuid=Id(token),name="OrderReachabilityMarker"});
        foreach(string id in new[]{Const("BlueprintRegistration","BrawlerClassId"),Const("BlueprintRegistration","BrawlerCombatFeatSelectionId"),Const("ExemplarInspireCourage","ArchetypeId"),Const("PsychicClass","ListId")}){var guid=BlueprintGuid.Parse(id);c.AddCachedBlueprint(guid,new BlueprintFeature{AssetGuid=guid,name="ExternalReachabilityMarker"});}
        var tokens=(string[])AccessTools.Field(Type("ChosenWeaponFeats"),"Tokens").GetValue(null);var chosen=new BlueprintFeature{AssetGuid=Id(tokens[0]),ComponentsArray=new BlueprintComponent[]{(BlueprintComponent)Activator.CreateInstance(Type("PrerequisiteChosenWeapon"))}};c.AddCachedBlueprint(chosen.AssetGuid,chosen);
        var training=(string[])AccessTools.Field(Type("T11TrainingOptions"),"FeatTokens").GetValue(null);var t=new BlueprintFeature{AssetGuid=Id(training[0])};c.AddCachedBlueprint(t.AssetGuid,t);return c;
    }
    static void Plan()
    {
        model=true;ready=new HashSet<string>();seen=new HashSet<string>();trace=new List<string>();var c=PlanCache();planCache058=c;int reports=0;Action<string,bool,Exception> report=(n,p,e)=>reports++;
        Call(Type("BlueprintRegistration"),"RegisterAll",c,report);
        var first=trace.ToArray();Check(first.Index(Amp)<first.Index(Terror),"actual initial dispatcher places producer before Terror Weaver");
        for(int i=0;i<2;i++)Call(Type("BlueprintRegistration"),"RegisterOptional",c,report);
        Check(optionalCalls==3&&reports==0,"actual initial/late/repeated Optional paths checked without fabricated success reports");
        Check(seen.SetEquals(manifest["modules"].Children<JProperty>().Select(p=>p.Name).Except(new[]{Name("MediumIntegration"),Name("WayOfTheShieldIntegration")})),"every RegisterAll/Optional module reached, separate bundled startup adapters explicitly outside scope");
        var old=first.ToList();old.Remove(Terror);old.Insert(old.IndexOf(Amp),Terror);var simulated=new HashSet<string>();bool rejected=false;
        foreach(string name in old){if(manifest["modules"][name].Values<string>().Any(d=>!simulated.Contains(d))){rejected=true;break;}simulated.Add(name);}
        Check(rejected,"old production order is rejected even with all reachability markers preloaded");model=false;
    }
    static int Index(this string[] array,string value)=>Array.IndexOf(array,value);
    static BlueprintsCache Cold(BlueprintsCache parent)
    {
        var amps=((IEnumerable)AccessTools.Field(typeof(Psychic070Tests),"newAssets").GetValue(null)).Cast<BlueprintScriptableObject>().Select(x=>x.AssetGuid).ToArray();Check(amps.Length==122,"actual amplification fixture supplies its complete122 owned IDs");
        var terror=((string[])AccessTools.Field(Type("TerrorWeaver01444"),"OwnTokens").GetValue(null)).Select(Id).ToArray();var omitted=new HashSet<BlueprintGuid>(amps.Concat(terror));
        var field=AccessTools.Field(typeof(Repair01436CallerTests),"OwnIds");var previous=field.GetValue(null);BlueprintsCache c;
        try{field.SetValue(null,omitted);c=(BlueprintsCache)Call(typeof(Repair01436CallerTests),"Fresh",parent);}finally{field.SetValue(null,previous);}
        AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,c);Call(typeof(Archetypes01444Tests),"Hydrate",c);
        var progression=(BlueprintProgression)c.Load(BlueprintGuid.Parse(Const("PsychicClass","ProgressionId")));
        // Removed choices resolve to null through native references; rebuild
        // entries without those now-absent owned features before republishing.
        Check(progression.LevelEntries.SelectMany(e=>e.Features).Count(f=>f==null)==9,"only the nine removed amplification progression references are unresolved");
        progression.LevelEntries=progression.LevelEntries.Select(e=>{var copy=new LevelEntry{Level=e.Level};copy.SetFeatures(e.Features.Where(f=>f!=null&&!omitted.Contains(f.AssetGuid)).ToArray());return copy;}).ToArray();
        Check(omitted.All(id=>c.Load(id)==null)&&progression.LevelEntries.All(e=>e.Features.All(f=>!omitted.Contains(f.AssetGuid))),"cold amplification/Terror cache and progression have no preseeded assets/links");
        Check(c.Load(Id("AuraOfDoom"))!=null&&c.Load(Id("AuraOfDoomShakenBuff"))!=null,"earlier Expansion foundation provides actual Doom assets");return c;
    }
    static bool Reject(Action action,string message)
    {
        try{action();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;Check(e is InvalidOperationException&&e.Message.Contains(message),"exact native production dependency guard: "+e.Message);return true;}
    }
    static void Actual(BlueprintsCache parent)
    {
        var tokens=(string[])AccessTools.Field(Type("TerrorWeaver01444"),"OwnTokens").GetValue(null);var c=Cold(parent);
        Check(Reject(()=>Call(Type("TerrorWeaver01444"),"Register",c),"requires the existing Psychic amplification"),"old order reproduces the user's exact dependency exception");preFixFailures++;
        Check(tokens.All(t=>c.Load(Id(t))==null),"failed early call publishes no partial Terror assets");
        var reports=new List<Tuple<string,bool,Exception>>();Action<string,bool,Exception> report=(n,p,e)=>reports.Add(Tuple.Create(n,p,e));
        Call(Type("BlueprintRegistration"),"RegisterAll",c,report);coldRuns++;
        Check(reports.Count==2&&reports[0].Item1==Amp&&reports[1].Item1==Terror&&reports.All(x=>x.Item2&&x.Item3==null),"actual cold RegisterAll executes real amplification then Terror bodies successfully");
        var root=(BlueprintArchetype)c.Load(Id(tokens[0]));var cls=(BlueprintCharacterClass)c.Load(BlueprintGuid.Parse(Const("PsychicClass","ClassId")));
        Check(root!=null&&cls.Archetypes.Count(x=>x.AssetGuid==root.AssetGuid)==1&&root.GetParentClass()==cls,"Terror appears exactly once in actual Psychic archetype options");
        Check(root.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{11,19})&&root.RemoveFeatures.All(e=>e.Features.Single().AssetGuid==Id("PhrenicAmplificationSelection")),"archetype replaces the newly published actual choices at11/19");
        var identities=tokens.Select(t=>c.Load(Id(t))).ToArray();reports.Clear();Call(Type("BlueprintRegistration"),"RegisterAll",c,report);coldRuns++;
        Check(reports.Count==2&&reports.All(x=>x.Item2)&&tokens.Select((t,i)=>ReferenceEquals(c.Load(Id(t)),identities[i])).All(x=>x),"repeat dispatcher preserves Terror identities");
        c=Cold(parent);reports.Clear();inject=true;Call(Type("BlueprintRegistration"),"RegisterAll",c,report);coldRuns++;failedProvider++;
        Check(reports.Count==2&&reports.All(x=>!x.Item2)&&reports[0].Item3.Message.Contains("injected amplification")&&reports[1].Item3.Message.Contains("requires the existing"),"failed prerequisite is reported honestly; dependent does not fabricate success");
        Check(tokens.All(t=>c.Load(Id(t))==null),"failed prerequisite leaves no partial archetype");reports.Clear();Call(Type("BlueprintRegistration"),"RegisterAll",c,report);coldRuns++;retries++;
        Check(reports.Count==2&&reports.All(x=>x.Item2)&&tokens.All(t=>c.Load(Id(t))!=null),"actual dispatcher retry after producer failure publishes the complete archetype");
        c=Cold(parent);Call(Type("PsychicAmplifications"),"Register",c);var shaken=c.Load(Id("AuraOfDoomShakenBuff"));c.RemoveCachedBlueprint(shaken.AssetGuid);
        Check(Reject(()=>Call(Type("TerrorWeaver01444"),"Register",c),"requires the existing"),"actual missing Doom dependency remains guarded");Check(tokens.All(t=>c.Load(Id(t))==null),"missing Doom never publishes partial archetype");c.AddCachedBlueprint(shaken.AssetGuid,shaken);Call(Type("TerrorWeaver01444"),"Register",c);Check(tokens.All(t=>c.Load(Id(t))!=null),"restored Doom dependency permits complete registration");
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks=entries=optionalCalls=coldRuns=preFixFailures=failedProvider=retries=0;
        using(var stream=typeof(RegistrationOrder01444Tests).Assembly.GetManifestResourceStream("CotW.Tests.RegistrationDependencies01444.json"))using(var reader=new StreamReader(stream))manifest=JObject.Parse(reader.ReadToEnd());
        var catalogue=((IEnumerable)AccessTools.Field(Type("ModuleCatalog"),"Definitions").GetValue(null)).Cast<object>().Select(x=>(string)Get(x,"Name")).ToArray();Check(catalogue.Length==(int)manifest["catalogueCount"]&&new HashSet<string>(catalogue).SetEquals(manifest["modules"].Children<JProperty>().Select(p=>p.Name)),"catalogue changes require dependency review, no implicit empty defaults");
        var h=new Harmony("CotW.RegistrationOrder01444");var original=ResourcesLibrary.BlueprintsCache;
        var snapshotType=typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot",BindingFlags.NonPublic);var snapshot=Activator.CreateInstance(snapshotType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{parent},null);
        try {
            h.Patch(AccessTools.Method(Type("BlueprintRegistration"),"RegisterModule"),prefix:new HarmonyMethod(typeof(RegistrationOrder01444Tests),nameof(ModulePrefix)));
            h.Patch(AccessTools.Method(Type("BlueprintRegistration"),"RegisterAvailableModule"),prefix:new HarmonyMethod(typeof(RegistrationOrder01444Tests),nameof(AvailablePrefix)));
            h.Patch(AccessTools.Method(Type("BlueprintRegistration"),"RegisterOptional"),prefix:new HarmonyMethod(typeof(RegistrationOrder01444Tests),nameof(OptionalPrefix)));
            h.Patch(AccessTools.Method(Type("ExtraInvestigatorTalent"),"Register"),prefix:new HarmonyMethod(typeof(RegistrationOrder01444Tests),nameof(DirectInvestigator)));
            h.Patch(AccessTools.Method(Type("ProviderResourceFeats"),"Available"),prefix:new HarmonyMethod(typeof(RegistrationOrder01444Tests),nameof(ProviderAvailable)));
            Plan();Actual(parent);AccessTools.Method(snapshotType,"Verify").Invoke(snapshot,null);Check(true,"original complete parent graph remains unchanged");
            PsychicExpansionTests.Inventory["registrationOrder01444Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["catalogueModulesReviewed"]=catalogue.Length,["dispatcherEntriesChecked"]=entries,["modulesReached"]=seen.Count,["optionalPasses"]=optionalCalls,["actualColdDispatches"]=coldRuns,["preFixDependencyFailures"]=preFixFailures,["injectedProviderFailures"]=failedProvider,["providerFailureRetries"]=retries,["actualDispatcherOrder"]=true,["oldOrderRejected"]=true,["actualColdAmplificationAndTerrorBodies"]=true,["actualPsychicArchetypeOptionPresent"]=true,["allOtherProductionBodiesPreserved"]=true,["modelTrace"]=JArray.FromObject(trace),["boundary"]="Actual RegisterAll/RegisterOptional dispatch order is checked against explicit reviewed intra-package dependency declarations, with typed reachability markers exercising optional branches and a simulated published set starting empty. Model actions are skipped without fabricated registration reports; native/external input initialization is not inferred. Actual amplification and Terror registration bodies execute on deep isolated caches with all122 amplification assets/links and nine Terror assets absent, while prior Class/Expansion foundations are supplied. The exact old exception, real archetype-list insertion, producer failure, complete retry, missing Doom guard and identity-preserving repeat execute. Other module bodies retain their dedicated native contracts. No game/UI/save/external mod initialization is inferred."};
            Console.WriteLine("PASS: "+checks+" RegistrationOrder01444 contracts.");return checks;
        } finally{model=false;inject=false;h.UnpatchAll(h.Id);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,original);}
    }
}
