using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

// Actual registration dispatch only; rule execution belongs to the two discovery/masterpiece suites.
internal static class Migration01435CallerTests
{
    static int checks;
    static bool forestAppendFault, optionalVersatileFault;
    const string VersatileId="94e2cd84bf3a8e04f8609fe502892f4f";
    static string cacheFaultToken;
    static readonly string[] RootTokens = { "ForestsBlessingDiscovery", "ResilientIllusionsDiscovery", "ClamorOfHeavensMasterpiece", "DanceOf23StepsMasterpiece", "SymphonyOfElysianHeartMasterpiece" };
    static HashSet<BlueprintGuid> OwnIds;
    static readonly List<string> executed = new List<string>(), skipped = new List<string>();
    static readonly List<JObject> observations = new List<JObject>();
    static HashSet<string> targets;
    static Type Module(string name) => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    static object Get(object o, string field) => AccessTools.Field(o.GetType(), field).GetValue(o);
    static void Set(object o, string field, object value) => AccessTools.Field(o.GetType(), field).SetValue(o, value);
    static string Constant(string type, string field) => (string)AccessTools.Field(Module(type), field).GetValue(null);
    static string[] Constants(string type, string field) => (string[])AccessTools.Field(Module(type), field).GetValue(null);
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Module("PsychicAssets"), "Id").Invoke(null, new object[] { token });
    static T CopyData<T>(T value) => (T)AccessTools.Method(Module("PsychicAssets"), "CopyData").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { value });
    static SimpleBlueprint Copy(SimpleBlueprint value) => (SimpleBlueprint)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(value, null);
    static string Definition(object value) => (string)AccessTools.Method(Module("Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new object[] { value });
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => (BlueprintFeatureReference[])Get(p, field) ?? Array.Empty<BlueprintFeatureReference>();
    static object Cached(BlueprintReferenceBase r) => AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").GetValue(r, null);
    static void Uncache(BlueprintReferenceBase r) => AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, null, null);
    static SimpleBlueprint[] Loaded(BlueprintsCache cache) => ((IDictionary)Get(cache, "m_LoadedBlueprints")).Values.Cast<object>().Select(entry=>(SimpleBlueprint)Get(entry,"Blueprint")).Where(bp=>bp!=null).ToArray();
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Migration01435 caller: " + why); checks++; }
    static IEnumerable<FieldInfo> Fields(Type type) {
        for (var t=type; t!=null && t!=typeof(object); t=t.BaseType)
            foreach (var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(x=>!x.IsStatic)) yield return f;
    }
    sealed class Identity : IEqualityComparer<object> {
        internal static readonly Identity Instance=new Identity();
        public new bool Equals(object a,object b)=>ReferenceEquals(a,b);
        public int GetHashCode(object o)=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
    }
    static IEnumerable<BlueprintReferenceBase> References(object value, HashSet<object> seen) {
        if (value==null) yield break;
        if (value is BlueprintReferenceBase r) { yield return r; yield break; }
        var t=value.GetType();
        if (value is SimpleBlueprint || value is UnityEngine.Object || t.IsPrimitive || t.IsEnum || value is string || value is decimal || value is Type || value is Delegate || !seen.Add(value)) yield break;
        if (value is IEnumerable sequence) { foreach (var item in sequence) foreach (var reference in References(item,seen)) yield return reference; yield break; }
        foreach (var f in Fields(t)) foreach (var reference in References(f.GetValue(value),seen)) yield return reference;
    }
    static IEnumerable<BlueprintReferenceBase> BlueprintReferences(SimpleBlueprint bp) => Fields(bp.GetType()).SelectMany(f=>References(f.GetValue(bp),new HashSet<object>(Identity.Instance))).Distinct<BlueprintReferenceBase>(Identity.Instance);
    sealed class ParentSnapshot {
        readonly BlueprintsCache cache; readonly SimpleBlueprint[] objects;
        readonly Dictionary<BlueprintScriptableObject,BlueprintComponent[]> arrays;
        readonly Dictionary<BlueprintScriptableObject,BlueprintComponent[]> componentItems;
        readonly Dictionary<BlueprintScriptableObject,string> definitions;
        readonly Dictionary<BlueprintFeatureSelection,object[]> pools;
        readonly BlueprintReferenceBase[] references; readonly object[] pointers;
        internal ParentSnapshot(BlueprintsCache value) {
            cache=value; objects=Loaded(cache);
            arrays=objects.OfType<BlueprintScriptableObject>().ToDictionary(x=>x,x=>x.ComponentsArray);
            componentItems=arrays.ToDictionary(x=>x.Key,x=>x.Value?.ToArray());
            definitions=arrays.ToDictionary(x=>x.Key,x=>Definition(x.Value));
            pools=objects.OfType<BlueprintFeatureSelection>().ToDictionary(x=>x,x=>new object[]{Get(x,"m_Features"),Get(x,"m_AllFeatures"),Refs(x,"m_Features").ToArray(),Refs(x,"m_AllFeatures").ToArray()});
            references=objects.SelectMany(BlueprintReferences).Distinct<BlueprintReferenceBase>(Identity.Instance).ToArray(); pointers=references.Select(Cached).ToArray();
        }
        internal void Verify() {
            var actual=Loaded(cache).ToDictionary(x=>x.AssetGuid);
            Check(actual.Count==objects.Length && objects.All(x=>actual.TryGetValue(x.AssetGuid,out var p)&&ReferenceEquals(p,x)),"parent full cache GUID/object graph remains exact");
            Check(arrays.All(x=>ReferenceEquals(x.Key.ComponentsArray,x.Value)) && componentItems.All(x=>x.Value==null?x.Key.ComponentsArray==null:x.Key.ComponentsArray.Length==x.Value.Length&&x.Key.ComponentsArray.Zip(x.Value,ReferenceEquals).All(v=>v)) && definitions.All(x=>Definition(x.Key.ComponentsArray)==x.Value),"parent all component array/element identities and authored graph definitions remain exact");
            Check(pools.All(x=>ReferenceEquals(Get(x.Key,"m_Features"),x.Value[0])&&ReferenceEquals(Get(x.Key,"m_AllFeatures"),x.Value[1])&&Refs(x.Key,"m_Features").Length==((BlueprintFeatureReference[])x.Value[2]).Length&&Refs(x.Key,"m_AllFeatures").Length==((BlueprintFeatureReference[])x.Value[3]).Length&&Refs(x.Key,"m_Features").Zip((BlueprintFeatureReference[])x.Value[2],ReferenceEquals).All(v=>v)&&Refs(x.Key,"m_AllFeatures").Zip((BlueprintFeatureReference[])x.Value[3],ReferenceEquals).All(v=>v)),"parent all selection array identities, entries and order remain exact");
            Check(references.Select(Cached).Zip(pointers,ReferenceEquals).All(x=>x),"parent every observed lazy reference Cached pointer remains exact, including null pointers");
        }
    }
    static bool IsOwned(SimpleBlueprint bp) => OwnIds.Contains(bp.AssetGuid);
    static BlueprintsCache Fresh(BlueprintsCache parent) {
        var cache=new BlueprintsCache();
        var omitted=new HashSet<BlueprintGuid>(Loaded(parent).Where(IsOwned).Select(x=>x.AssetGuid));
        foreach(var token in RootTokens.Concat(new[]{"BardicMasterpieces01435Selection"}))omitted.Add(Id(token));
        foreach(var bp in Loaded(parent).Where(x=>!omitted.Contains(x.AssetGuid))) {
            var copy=Copy(bp);
            if(copy is BlueprintScriptableObject scripted)scripted.ComponentsArray=CopyData(scripted.ComponentsArray);
            foreach(var field in Fields(bp.GetType())) {
                if(field.Name=="ComponentsArray")continue;
                if(References(field.GetValue(bp),new HashSet<object>(Identity.Instance)).Any())field.SetValue(copy,CopyData(field.GetValue(bp)));
            }
            if(copy is BlueprintFeatureSelection pool)foreach(string field in new[]{"m_Features","m_AllFeatures"}) {
                var refs=CopyData(Refs(pool,field).Where(r=>r!=null&&!omitted.Contains(r.Guid)).ToArray());
                foreach(var r in refs)Uncache(r);Set(pool,field,refs);
            }
            foreach(var r in BlueprintReferences(copy))Uncache(r);
            cache.AddCachedBlueprint(copy.AssetGuid,copy);
        }
        // Generic earlier fixtures deliberately contain minimal native spell
        // components. This fresh cache reads the actual game archive for the
        // newly audited registration dependencies, never modifying the parent.
        foreach(string id in Constants("WizardDiscoveries01435","PhantasmalIds").Concat(new[]{Constant("BardMasterpieces01435","BardResourceFact"),Constant("BardMasterpieces01435","SkaldResourceFact"),"09d39b38bb7c6014394b6daced9bacd3","df3950af5a783bd4d91ab73eb8fa0fd3","0ec36e7596a4928489d2049e1e1c76a7","25ec6cb6ab1845c48a95f9c20b034220","1533e782fca42b84ea370fc1dcbf4fc1"})) {
            var existing=cache.Load(BlueprintGuid.Parse(id));Check(existing!=null,"real native registration dependency is present: "+id);
            var authored=(SimpleBlueprint)AccessTools.Method(typeof(PsychicExpansionTests),"NativeCase").MakeGenericMethod(existing.GetType()).Invoke(null,new object[]{id});
            cache.AddCachedBlueprint(authored.AssetGuid,authored);
            Check(!ReferenceEquals(authored,parent.Load(authored.AssetGuid))&&((BlueprintScriptableObject)authored).ComponentsArray.All(c=>c!=null),"fresh registration dependency has real native authored components and a separate object: "+id);
        }
        cache.RemoveCachedBlueprint(BlueprintGuid.Parse(Constant("WizardDiscoveries01435","DiscoveryPoolId")));
        AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
        Check(RootTokens.All(t=>cache.Load(Id(t))==null)&&!Loaded(cache).Any(IsOwned),"fresh startup omits the complete035 private graph and old selection references");
        Check(Constants("WizardDiscoveries01435","CorePoolIds").Select(id=>(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).All(p=>new[]{"m_Features","m_AllFeatures"}.All(f=>!ReferenceEquals(Get(p,f),Get((BlueprintFeatureSelection)parent.Load(p.AssetGuid),f))&&Refs(p,f).All(r=>Cached(r)==null))),"fresh four discovery native pools own distinct arrays and uncached cloned references");
        return cache;
    }
    static bool CallerScope(string name) { if(targets.Contains(name)){executed.Add(name);return true;}skipped.Add(name);return false; }
    static bool DirectInvestigatorBoundary(ref bool __result) { skipped.Add(Constant("ExtraInvestigatorTalent","ModuleName"));__result=false;return false; }
    static void AppendFault(IEnumerable<BlueprintFeatureSelection> __0,BlueprintFeature[] feats) { if(forestAppendFault&&feats.Any(f=>f.AssetGuid==Id(RootTokens[0])))throw new InvalidOperationException("01435 actual Forest Append fault");if(optionalVersatileFault&&__0.Any(p=>p.AssetGuid==BlueprintGuid.Parse(VersatileId)))throw new InvalidOperationException("01435 actual optional Versatile Append fault"); }
    static void CacheFault(BlueprintGuid __0) { if(cacheFaultToken!=null&&__0==Id(cacheFaultToken))throw new InvalidOperationException("01435 actual native AddCachedBlueprint fault "+cacheFaultToken); }
    static Dictionary<string,string> InitialStates()=>(Dictionary<string,string>)AccessTools.Method(Module("ModuleCatalog"),"InitialStates").Invoke(null,null);
    static void Catalog(Dictionary<string,string> states) {
        var defs=((IEnumerable)AccessTools.Field(Module("ModuleCatalog"),"Definitions").GetValue(null)).Cast<object>().ToArray();
        var snapshot=((IEnumerable)AccessTools.Method(Module("ModuleCatalog"),"Snapshot").Invoke(null,new object[]{states})).Cast<object>().ToArray();
        Check(defs.Length>0&&defs.Select(x=>(string)Get(x,"Name")).Distinct().Count()==defs.Length&&snapshot.Length==defs.Length,"one actual module definition collection determines unique names and real caller status count");
        Check(snapshot.Select((x,i)=>ReferenceEquals(Get(x,"Definition"),defs[i])&&(string)Get(x,"State")==states[(string)Get(defs[i],"Name")]).All(x=>x),"real Snapshot reports exact current definitions and actual caller state values");
    }
    static List<Tuple<string,bool,Exception>> Call(BlueprintsCache cache,string method,Dictionary<string,string> states) {
        var outcomes=new List<Tuple<string,bool,Exception>>();
        Action<string,bool,Exception> report=(name,ok,error)=>{states[name]=ok?"Registered":"Failed";outcomes.Add(Tuple.Create(name,ok,error));};
        AccessTools.Method(Module("BlueprintRegistration"),method).Invoke(null,new object[]{cache,report});
        Check(outcomes.All(x=>targets.Contains(x.Item1))&&skipped.All(s=>!outcomes.Any(o=>o.Item1==s)),"non-target module actions are explicitly skipped without reporting fabricated success");
        return outcomes;
    }
    static Dictionary<string,BlueprintFeatureReference[]> PoolSnapshot(BlueprintsCache cache)=>Loaded(cache).OfType<BlueprintFeatureSelection>().SelectMany(p=>new[]{"m_Features","m_AllFeatures"}.Select(f=>new{key=p.AssetGuid+"/"+f,refs=Refs(p,f).ToArray()})).ToDictionary(x=>x.key,x=>x.refs);
    static void Pools(BlueprintsCache cache,Dictionary<string,BlueprintFeatureReference[]> old,string missing=null) {
        foreach(var entry in old) {
            var parts=entry.Key.Split('/');var actual=Refs((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(parts[0])),parts[1]);
            Check(actual.Length>=entry.Value.Length&&actual.Take(entry.Value.Length).Zip(entry.Value,ReferenceEquals).All(x=>x),"every pre-existing selection retains exact reference objects and order: "+entry.Key);
            Check(actual.Where(r=>r!=null&&RootTokens.Any(t=>r.Guid==Id(t))).GroupBy(r=>r.Guid).All(g=>g.Count()==1),"no035 leaf is duplicated in an actual selection: "+entry.Key);
        }
        foreach(string poolId in Constants("WizardDiscoveries01435","CorePoolIds"))foreach(string field in new[]{"m_Features","m_AllFeatures"}) {
            var refs=Refs((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(poolId)),field);
            foreach(string token in RootTokens.Take(2))Check(refs.Count(r=>r.Guid==Id(token))==(token==missing?0:1),"four actual discovery pools contain exactly each successful discovery root: "+poolId+"/"+field+"/"+token);
        }
        var container=cache.Load(Id("BardicMasterpieces01435Selection")) as BlueprintFeatureSelection;
        Check(container!=null,"masterpiece selection container is published as infrastructure for the three original options");
        foreach(string field in new[]{"m_Features","m_AllFeatures"})foreach(string token in RootTokens.Skip(2))Check(Refs(container,field).Count(r=>r.Guid==Id(token))==(token==missing?0:1),"masterpiece container has one successful leaf, no failed leaf: "+field+"/"+token);
    }
    static void Failure(BlueprintsCache parent,string token,bool append) {
        var cache=Fresh(parent);var before=PoolSnapshot(cache);var states=InitialStates();executed.Clear();skipped.Clear();
        forestAppendFault=append;cacheFaultToken=append?null:token;
        List<Tuple<string,bool,Exception>> outcomes;
        try {outcomes=Call(cache,"RegisterAll",states);}
        finally {forestAppendFault=false;cacheFaultToken=null;}
        int index=Array.IndexOf(RootTokens,token);string failed=ModuleNames()[index];var errors=outcomes.Where(x=>!x.Item2).ToArray();
        Check(errors.Length>=1&&errors.All(x=>x.Item1==failed)&&errors.All(x=>x.Item3!=null&&x.Item3.ToString().Contains(append?"01435 actual Forest Append fault":"01435 actual native AddCachedBlueprint fault")),"actual transaction fault isolates only selected module: "+token+"; failures="+string.Join("; ",errors.Select(x=>x.Item1+"="+x.Item3)));
        Check(cache.Load(Id(token))==null&&states[failed]=="Failed","failed root is absent and its real reported state remains Failed: "+token);
        Check(targets.Where(n=>n!=failed).All(n=>outcomes.Any(x=>x.Item1==n)&&outcomes.Where(x=>x.Item1==n).All(x=>x.Item2)),"all other four real actions continue and report success after fault: "+token);
        Check(targets.Where(n=>n!=failed).All(n=>states[n]=="Registered"),"actual remaining module states are Registered: "+token);
        var optional=Call(cache,"RegisterOptional",states);
        Check(!optional.Any(x=>x.Item1==failed)&&states[failed]=="Failed","actual root-gated optional dispatcher cannot hide missing core failure: "+token);
        Pools(cache,before,token);Catalog(states);
        observations.Add(new JObject{["case"]=append?"forestNativeAppendFailure":"nativeCacheInsertionFailure:"+token,["executed"]=new JArray(executed),["nonTargetActionsScopedOut"]=new JArray(skipped.Distinct()),["actualStates"]=JObject.FromObject(states)});
    }
    static BlueprintFeatureSelection Provider(BlueprintsCache cache,int scenario) {
        var p=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(Constant("WizardDiscoveries01435","DiscoveryPoolId")),name=scenario==3?"WrongProviderName":"ArcaneDiscoverySelection",Group=scenario==2?FeatureGroup.Feat:scenario==6?FeatureGroup.None:FeatureGroup.WizardFeat,Groups=scenario==6?new[]{FeatureGroup.WizardFeat}:Array.Empty<FeatureGroup>(),IgnorePrerequisites=scenario==1,ComponentsArray=Array.Empty<BlueprintComponent>()};
        foreach(string field in new[]{"m_Features","m_AllFeatures"}) { var reference=new BlueprintFeatureReference();Set(reference,"deserializedGuid",BlueprintGuid.Parse(Constant("BlueprintRegistration","IronWillId")));Set(p,field,new[]{reference}); }
        p.OnEnable();cache.AddCachedBlueprint(p.AssetGuid,p);return p;
    }
    static JObject[] Success(BlueprintsCache parent) {
        var cache=Fresh(parent);var before=PoolSnapshot(cache);var states=InitialStates();executed.Clear();skipped.Clear();var outcomes=Call(cache,"RegisterAll",states);
        Check(targets.All(t=>outcomes.Any(x=>x.Item1==t)&&outcomes.Where(x=>x.Item1==t).All(x=>x.Item2)),"actual startup independently publishes all five modules: "+string.Join(";",outcomes.Where(x=>!x.Item2).Select(x=>x.Item1+"="+x.Item3)));
        Check(ModuleNames().Select(n=>executed.IndexOf(n)).SequenceEqual(ModuleNames().Select(n=>executed.IndexOf(n)).OrderBy(x=>x)),"actual first execution order is Forest, Resilient, Clamor, Dance, Symphony");
        Pools(cache,before);Catalog(states);
        var owned=Loaded(cache).Where(IsOwned).ToDictionary(x=>x.AssetGuid,x=>x);
        Check(RootTokens.All(t=>owned.ContainsKey(Id(t)))&&owned.ContainsKey(Id("BardicMasterpieces01435Selection")),"owned observed graph contains the five real roots and one non-counted container");
        var definitions=owned.Values.OfType<BlueprintScriptableObject>().ToDictionary(x=>x.AssetGuid,x=>Definition(new object[]{x.name,x.GetType().FullName,x.ComponentsArray,x is BlueprintFeatureSelection p?Refs(p,"m_Features"):null,x is BlueprintFeatureSelection q?Refs(q,"m_AllFeatures"):null}));
        foreach(int scenario in Enumerable.Range(0,7)) {
            var id=BlueprintGuid.Parse(Constant("WizardDiscoveries01435","DiscoveryPoolId"));BlueprintFeatureSelection provider=null;object[] arrays=null;
            cache.RemoveCachedBlueprint(id);
            if(scenario==4) {var wrong=new BlueprintFeature{AssetGuid=id,name="WrongDiscoveryType",ComponentsArray=Array.Empty<BlueprintComponent>()};wrong.OnEnable();cache.AddCachedBlueprint(id,wrong);}
            else if(scenario!=0) {provider=Provider(cache,scenario);arrays=new[]{Get(provider,"m_Features"),Get(provider,"m_AllFeatures")};}
            outcomes=Call(cache,"RegisterOptional",states);Call(cache,"RegisterOptional",states);
            Check(targets.All(t=>outcomes.Any(x=>x.Item1==t)&&outcomes.Where(x=>x.Item1==t).All(x=>x.Item2)),"optional real dispatcher preserves truthful core success: provider scenario "+scenario);
            Check(owned.All(x=>ReferenceEquals(cache.Load(x.Key),x.Value)),"all published035 objects retain exact identity through repeated optional calls: "+scenario);
            Check(definitions.All(x=>x.Value==Definition(new object[]{owned[x.Key].name,owned[x.Key].GetType().FullName,((BlueprintScriptableObject)owned[x.Key]).ComponentsArray,owned[x.Key] is BlueprintFeatureSelection p?Refs(p,"m_Features"):null,owned[x.Key] is BlueprintFeatureSelection q?Refs(q,"m_AllFeatures"):null})),"all035 graph component/branch definitions remain exact through retries: "+scenario);
            if(scenario>=1&&scenario<=3)Check(ReferenceEquals(Get(provider,"m_Features"),arrays[0])&&ReferenceEquals(Get(provider,"m_AllFeatures"),arrays[1]),"disabled or malformed optional provider keeps both old arrays exact: "+scenario);
            if(scenario==4)Check(cache.Load(id) is BlueprintFeature wrong&&!(wrong is BlueprintFeatureSelection)&&wrong.name=="WrongDiscoveryType","wrong-type optional provider is not replaced");
            if(scenario>=5)Check(new[]{"m_Features","m_AllFeatures"}.All(f=>RootTokens.Take(2).All(t=>Refs(provider,f).Count(r=>r.Guid==Id(t))==1)&&RootTokens.Skip(2).All(t=>Refs(provider,f).All(r=>r.Guid!=Id(t)))) ,"late Group/Groups-only provider receives each discovery once and no masterpiece leaves: "+scenario);
            Pools(cache,before);Catalog(states);
        }
        Check(states.Where(x=>!targets.Contains(x.Key)).All(x=>x.Value==InitialStates()[x.Key]),"scoped-out modules retain exact native initial states");
        observations.Add(new JObject{["case"]="successAndSevenOptionalProviderShapes",["executed"]=new JArray(executed),["nonTargetActionsScopedOut"]=new JArray(skipped.Distinct()),["actualStates"]=JObject.FromObject(states)});
        return owned.Values.OrderBy(x=>x.AssetGuid.ToString()).Select(x=>new JObject{["id"]=x.AssetGuid.ToString(),["name"]=x.name,["type"]=x.GetType().FullName}).ToArray();
    }
    static void ForeignContainer(BlueprintsCache parent) {
        var cache=Fresh(parent);var before=PoolSnapshot(cache);var states=InitialStates();executed.Clear();skipped.Clear();
        var foreign=new BlueprintFeatureSelection{AssetGuid=Id("BardicMasterpieces01435Selection"),name="CotWWrathPsychicBardicMasterpieces01435Selection",Ranks=10,Groups=new[]{FeatureGroup.Feat},ComponentsArray=Array.Empty<BlueprintComponent>()};
        foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(foreign,field,Array.Empty<BlueprintFeatureReference>());
        foreign.OnEnable();cache.AddCachedBlueprint(foreign.AssetGuid,foreign);var arrays=new[]{Get(foreign,"m_Features"),Get(foreign,"m_AllFeatures")};var components=foreign.ComponentsArray;
        var outcomes=Call(cache,"RegisterAll",states);var errors=outcomes.Where(x=>!x.Item2).ToArray();
        Check(errors.Length>=3&&errors.Select(x=>x.Item1).Distinct().OrderBy(x=>x).SequenceEqual(ModuleNames().Skip(2).OrderBy(x=>x)),"unowned same-GUID/name/type private container rejects all three masterpieces through actual report paths");
        Check(ModuleNames().Take(2).All(n=>outcomes.Any(x=>x.Item1==n)&&outcomes.Where(x=>x.Item1==n).All(x=>x.Item2)),"foreign masterpiece container does not block the two independent discoveries");
        Check(ReferenceEquals(cache.Load(foreign.AssetGuid),foreign)&&ReferenceEquals(foreign.ComponentsArray,components)&&ReferenceEquals(Get(foreign,"m_Features"),arrays[0])&&ReferenceEquals(Get(foreign,"m_AllFeatures"),arrays[1])&&RootTokens.Skip(2).All(t=>cache.Load(Id(t))==null),"foreign graph is not adopted/replaced/extended, and no rejected masterpiece root remains");
        Call(cache,"RegisterOptional",states);Check(ModuleNames().Skip(2).All(n=>states[n]=="Failed"),"optional dispatcher does not hide a foreign-container core failure");
        foreach(var entry in before){var parts=entry.Key.Split('/');var current=Refs((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(parts[0])),parts[1]);Check(current.Length>=entry.Value.Length&&current.Take(entry.Value.Length).Zip(entry.Value,ReferenceEquals).All(x=>x),"foreign-container rejection preserves existing selection prefix: "+entry.Key);}
        Catalog(states);observations.Add(new JObject{["case"]="foreignSameIdentityPrivateContainer",["actualStates"]=JObject.FromObject(states)});
    }
    static void OptionalVersatileFault(BlueprintsCache parent) {
        var cache=Fresh(parent);var states=InitialStates();executed.Clear();skipped.Clear();
        cache.RemoveCachedBlueprint(BlueprintGuid.Parse(VersatileId));
        var provider=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(VersatileId),name="VersatilePerformanceSelection",ComponentsArray=Array.Empty<BlueprintComponent>(),Groups=Array.Empty<FeatureGroup>()};
        foreach(string field in new[]{"m_Features","m_AllFeatures"}){var reference=new BlueprintFeatureReference();Set(reference,"deserializedGuid",BlueprintGuid.Parse(Constant("BlueprintRegistration","IronWillId")));Set(provider,field,new[]{reference});}
        provider.OnEnable();cache.AddCachedBlueprint(provider.AssetGuid,provider);var arrays=new[]{Get(provider,"m_Features"),Get(provider,"m_AllFeatures")};var before=PoolSnapshot(cache);
        List<Tuple<string,bool,Exception>> outcomes;optionalVersatileFault=true;
        try {outcomes=Call(cache,"RegisterAll",states);Call(cache,"RegisterOptional",states);}
        finally {optionalVersatileFault=false;}
        Check(targets.All(n=>outcomes.Any(x=>x.Item1==n)&&outcomes.Where(x=>x.Item1==n).All(x=>x.Item2))&&states.Where(x=>targets.Contains(x.Key)).All(x=>x.Value=="Registered"),"actual optional Versatile append exception is isolated from all five core module reports");
        Check(ReferenceEquals(Get(provider,"m_Features"),arrays[0])&&ReferenceEquals(Get(provider,"m_AllFeatures"),arrays[1]),"actual post-mutation optional Versatile fault restores both exact old arrays");
        Check(((string)AccessTools.Field(Module("BardMasterpieces01435"),"LastOptionalDiagnostic").GetValue(null)).Contains("01435 actual optional Versatile Append fault"),"real optional failure is recorded diagnostically rather than hidden");
        Pools(cache,before);var roots=RootTokens.Select(t=>cache.Load(Id(t))).ToArray();Call(cache,"RegisterOptional",states);Call(cache,"RegisterOptional",states);
        Check(roots.Zip(RootTokens.Select(t=>cache.Load(Id(t))),ReferenceEquals).All(x=>x),"late optional retry preserves every already-published root identity");
        Check(new[]{"m_Features","m_AllFeatures"}.All(f=>Refs(provider,f).Length==((BlueprintFeatureReference[])arrays[0]).Length+1&&Refs(provider,f).Count(r=>r.Guid==Id("BardicMasterpieces01435Selection"))==1),"after removing the controlled fault, actual optional dispatcher appends one container only");
        Catalog(states);observations.Add(new JObject{["case"]="optionalVersatilePostMutationFaultAndRealRetry",["actualStates"]=JObject.FromObject(states)});
    }
    static string[] ModuleNames()=>new[]{Constant("WizardDiscoveries01435","ForestModule"),Constant("WizardDiscoveries01435","ResilientModule"),Constant("BardMasterpieces01435","ClamorModule"),Constant("BardMasterpieces01435","DanceModule"),Constant("BardMasterpieces01435","SymphonyModule")};
    internal static int Run(BlueprintsCache parent) {
        checks=0;observations.Clear();forestAppendFault=false;cacheFaultToken=null;optionalVersatileFault=false;OwnIds=new HashSet<BlueprintGuid>(RootTokens.Take(2).Concat(Constants("BardMasterpieces01435","OwnTokens")).Select(Id));
        var strings=(Dictionary<string,string>)AccessTools.Field(Module("PsychicAssets"),"Strings").GetValue(null);var beforeStrings=strings.ToArray();var snapshot=new ParentSnapshot(parent);targets=new HashSet<string>(ModuleNames());
        var diagnostics=new[]{Module("WizardDiscoveries01435"),Module("BardMasterpieces01435")}.SelectMany(AccessTools.GetDeclaredFields).Where(f=>f.IsStatic&&f.FieldType==typeof(string)&&f.Name.Contains("Diagnostic")).Select(f=>Tuple.Create(f,f.GetValue(null))).ToArray();
        var h=new Harmony("CotW01435.ActualScopedRegistrationCaller");JObject[] observed=null;
        h.Patch(AccessTools.Method(Module("BlueprintRegistration"),"RegisterModule"),prefix:new HarmonyMethod(typeof(Migration01435CallerTests),nameof(CallerScope)));ScopedAvailable01463Tests.Bind(h,Module("BlueprintRegistration"),"ScopeMigration01435CallerTests");
        h.Patch(AccessTools.Method(Module("ExtraInvestigatorTalent"),"Register"),prefix:new HarmonyMethod(typeof(Migration01435CallerTests),nameof(DirectInvestigatorBoundary)));
        h.Patch(AccessTools.Method(Module("T09CombatFeats"),"Append"),postfix:new HarmonyMethod(typeof(Migration01435CallerTests),nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),prefix:new HarmonyMethod(typeof(Migration01435CallerTests),nameof(CacheFault)));
        try {Failure(parent,RootTokens[0],true);foreach(string token in RootTokens)Failure(parent,token,false);observed=Success(parent);ForeignContainer(parent);OptionalVersatileFault(parent);}
        finally {forestAppendFault=false;cacheFaultToken=null;optionalVersatileFault=false;h.UnpatchAll(h.Id);strings.Clear();foreach(var p in beforeStrings)strings.Add(p.Key,p.Value);foreach(var d in diagnostics)d.Item1.SetValue(null,d.Item2);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,parent);snapshot.Verify();}
        Check(strings.Count==beforeStrings.Length&&beforeStrings.All(p=>strings.TryGetValue(p.Key,out var v)&&v==p.Value),"complete global text key/value set is restored");
        Check(new HashSet<string>(observed.Select(x=>(string)x["id"])).SetEquals(OwnIds.Select(x=>x.ToString()))&&observed.Select(x=>(string)x["id"]).Distinct().Count()==observed.Length,"actual observed035 assets form a unique inventory, independently of migration item counts");
        PsychicExpansionTests.Inventory["migration01435CallerObservations"]=new JArray(observations);
        PsychicExpansionTests.Inventory["migration01435CallerContracts"]=new JObject{
            ["passed"]=true,["assertions"]=checks,["additionalItemCount"]=0,["newBlueprints"]=new JArray(),["observedOwnedBlueprints"]=new JArray(observed),
            ["foreignSameIdentityContainerRejectedAndPreserved"]=true,["optionalVersatilePostMutationFailureIsolatedAndRetried"]=true,["actualRegisterAllDispatcher"]=true,["actualRegisterOptionalDispatcher"]=true,["fiveIndependentModulesAndExecutionOrder"]=true,["realNativeAppendAndCacheFaults"]=true,["allFiveFailuresIsolatedAndRolledBack"]=true,["laterModulesExecuteAfterFailure"]=true,["failedCacheStateHonestUntilNewStartup"]=true,["allPriorSelectionReferencesAndOrderPreserved"]=true,["fourDiscoveryPoolsAndMasterpieceContainerExact"]=true,["lateGroupAndGroupsOnlyDiscoveryPool"]=true,["optionalAbsentDisabledMalformedPreserveCore"]=true,["allOwnedBlueprintIdentitiesAndDefinitionsPreserved"]=true,["actualCatalogueDefinitionsAndReportedStates"]=true,["nonTargetActionsScopedOutWithoutSuccessReports"]=true,["freshNativeAuthoredRegistrationDependencies"]=true,["freshSelectionArraysAndUncachedReferences"]=true,["parentFullCacheIdentityComponentsPoolsAndLazyPointersPreserved"]=true,["globalTextKeysAndValuesPreserved"]=true,
            ["boundary"]="Actual RegisterAll/RegisterOptional with five real independent discovery/masterpiece actions on deep-copied isolated native caches. Four phantasmal spells and actual Bard/Skald resource facts/status/Freedom of Movement components are refreshed from the real native archive within each fresh cache, preserving the original parent. Non-target actions are skipped without success reports. Actual Append-after-mutation and native AddCachedBlueprint exceptions are controlled faults; each module rollback and remaining four real actions execute. Optional absent/disabled/wrong group/name/type/late Group or Groups-only discovery provider shapes, unowned private container, and optional Versatile pool are controlled metadata. The actual optional Versatile Append mutates before a postfix throws; real production rollback, diagnostic and later successful retry execute. Parent full cache/component/selection/lazy-pointer identities and global strings are restored. Observed asset inventory includes existing module assets, not additional migrations or assets beyond the two rule-suite inventories. No complete game startup, external provider assembly, UI/rendering or complete save scene is inferred."};
        Console.WriteLine("PASS: "+checks+" actual035 registration caller contracts; "+observed.Length+" observed assets.");return checks;
    }
}
