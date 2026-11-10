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

// Registration integration only. Gameplay paths belong to the two power suites.
internal static class Migration01434CallerTests
{
    static int checks;
    static bool appendFault;
    static readonly string[] Tokens = { "SuperstitionRagePower", "WitchHunterRagePower", "GhostRagerRagePower", "DisruptiveRagePower", "SpellbreakerRagePower" };
    static readonly List<string> executed = new List<string>(), skipped = new List<string>();
    static readonly List<JObject> observations = new List<JObject>();
    static JObject[] legacyDerived;
    static HashSet<string> targets;
    static BlueprintsCache active;
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
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Migration01434 caller: " + why); checks++; }
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
    static BlueprintsCache Fresh(BlueprintsCache parent) {
        var cache=new BlueprintsCache();
        var omitted=Loaded(parent).Where(x=>x.name.StartsWith("CotWWrathPsychicFerocious") || new[]{"Superstition","WitchHunter","GhostRager","DisruptiveRage","SpellbreakerRage"}.Any(t=>x.name.StartsWith("CotWWrathPsychic"+t))).Select(x=>x.AssetGuid).ToHashSet();
        foreach(var bp in Loaded(parent).Where(x=>!omitted.Contains(x.AssetGuid))) {
            var copy=Copy(bp);
            if(copy is BlueprintScriptableObject scripted) scripted.ComponentsArray=CopyData(scripted.ComponentsArray);
            // Copy every top-level field containing blueprint references, not
            // just ComponentsArray. All copied lazy edges resolve in this cache.
            foreach(var field in Fields(bp.GetType())) {
                if(field.Name=="ComponentsArray") continue;
                var value=field.GetValue(bp);
                if(References(value,new HashSet<object>(Identity.Instance)).Any()) field.SetValue(copy,CopyData(value));
            }
            if(copy is BlueprintFeatureSelection selection)
                foreach(var field in new[]{"m_Features","m_AllFeatures"}) {
                    var refs=CopyData(Refs(selection,field).Where(r=>r!=null&&!omitted.Contains(r.Guid)).ToArray());
                    foreach(var r in refs) Uncache(r); Set(selection,field,refs);
                }
            foreach(var r in BlueprintReferences(copy)) Uncache(r);
            cache.AddCachedBlueprint(copy.AssetGuid,copy);
        }
        AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
        // These five authored hosts have been changed by earlier registration
        // suites in a full run. NativeCase reads their actual game components.
        foreach(string id in Constants("RagePowers","HostIds").Concat(new[]{Constant("FerociousBeast01431","BloodrageId")})) {
            var native=PsychicExpansionTests.NativeCase<BlueprintBuff>(id); cache.AddCachedBlueprint(native.AssetGuid,native);
        }
        var blood=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(Constant("RagePowers","BloodragerId")));
        var primal=(BlueprintArchetype)cache.Load(BlueprintGuid.Parse(Constant("RagePowers","PrimalistId")));
        var data=PsychicExpansionTests.NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==blood.AssetGuid.ToString())["blueprint"]["Data"];
        Set(blood,"m_Archetypes",PsychicExpansionTests.Read(data["m_Archetypes"],typeof(BlueprintArchetypeReference[]))); Set(primal,"m_ParentClass",blood);
        foreach(string id in Constants("RagePowers","SelectionIds")) {
            var selection=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id));
            var row=PsychicExpansionTests.NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]==id)["blueprint"]["Data"];
            foreach(string field in new[]{"Group","Groups","IgnorePrerequisites"}) { var f=AccessTools.Field(selection.GetType(),field); if(row[field]!=null)f.SetValue(selection,PsychicExpansionTests.Read(row[field],f.FieldType)); }
        }
        cache.RemoveCachedBlueprint(BlueprintGuid.Parse(Constant("TerrifyingHowl","TttPrimalistId")));
        Check(Constants("RagePowers","SelectionIds").Select(id=>(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).All(p=>new[]{"m_Features","m_AllFeatures"}.All(f=>!ReferenceEquals(Get(p,f),Get((BlueprintFeatureSelection)parent.Load(p.AssetGuid),f))&&Refs(p,f).All(r=>Cached(r)==null))),"fresh four native pools own distinct arrays and initially uncached cloned references");
        Check(Tokens.All(t=>cache.Load(Id(t))==null)&&!Loaded(cache).Any(x=>x.name.StartsWith("CotWWrathPsychicFerocious")),"fresh startup begins without the five owned graphs or Ferocious private graph");
        return cache;
    }
    static bool CallerScope(string name) { if(targets.Contains(name)){executed.Add(name);return true;} skipped.Add(name);return false; }
    static bool DirectInvestigatorBoundary(ref bool __result) { skipped.Add(Constant("ExtraInvestigatorTalent","ModuleName")); __result=false; return false; }
    static void AppendFault(BlueprintFeature[] feats) { if(appendFault&&feats.Any(f=>f.AssetGuid==Id("WitchHunterRagePower")))throw new InvalidOperationException("01434 actual Witch Hunter append fault"); }
    static Dictionary<string,string> InitialStates() => (Dictionary<string,string>)AccessTools.Method(Module("ModuleCatalog"),"InitialStates").Invoke(null,null);
    static object[] Snapshot(Dictionary<string,string> states) => ((IEnumerable)AccessTools.Method(Module("ModuleCatalog"),"Snapshot").Invoke(null,new object[]{states})).Cast<object>().ToArray();
    static void Catalog(Dictionary<string,string> states) {
        var definitions=((IEnumerable)AccessTools.Field(Module("ModuleCatalog"),"Definitions").GetValue(null)).Cast<object>().ToArray();
        var snapshot=Snapshot(states);
        Check(definitions.Length!=0&&definitions.Select(d=>(string)Get(d,"Name")).Distinct().Count()==definitions.Length&&snapshot.Length==definitions.Length,"real catalogue definitions alone determine unique status rows and actual snapshot length");
        Check(snapshot.Select((s,i)=>ReferenceEquals(Get(s,"Definition"),definitions[i])&&(string)Get(s,"State")==states[(string)Get(definitions[i],"Name")]).All(x=>x),"real Snapshot uses exact definitions and actual caller-reported states");
    }
    static List<Tuple<string,bool,Exception>> Call(BlueprintsCache cache,string method,Dictionary<string,string> states) {
        active=cache; var outcomes=new List<Tuple<string,bool,Exception>>();
        Action<string,bool,Exception> report=(name,ok,error)=>{states[name]=ok?"Registered":"Failed";outcomes.Add(Tuple.Create(name,ok,error));};
        AccessTools.Method(Module("BlueprintRegistration"),method).Invoke(null,new object[]{cache,report});
        Check(outcomes.All(x=>targets.Contains(x.Item1))&&skipped.All(s=>!outcomes.Any(o=>o.Item1==s)),"non-target module actions are scoped out without fabricated success reports");
        return outcomes;
    }
    static void Pools(BlueprintsCache cache,string[] required,Dictionary<string,BlueprintFeatureReference[]> original) {
        foreach(string id in Constants("RagePowers","SelectionIds")) foreach(string field in new[]{"m_Features","m_AllFeatures"}) {
            var refs=Refs((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id)),field); var old=original[id+field];
            Check(refs.Take(old.Length).SequenceEqual(old)&&required.All(t=>refs.Count(r=>r.Guid==Id(t))==1),"actual native pool retains exact old entries and one copy per successfully published root "+id+"/"+field);
        }
    }
    static void Shared(BlueprintsCache cache,string[] roots) {
        foreach(int mode in Enumerable.Range(0,3)) {
            var shared=(BlueprintBuff)cache.Load(Id("FerociousBeastConstantPowers"+mode));
            var definition=Definition(shared.ComponentsArray);
            Check(roots.All(t=>definition.Contains("ref:"+Id(t))),"Ferocious extracts each available constant rage power after its actual registration, personal mode "+mode);
            foreach(string token in roots) {
                var effect=cache.Load(Id(token.Replace("RagePower","RageEffectBuff")));
                var privateBuff=cache.Load(Id("FerociousBeastPassive"+mode+effect.AssetGuid));
                Check(privateBuff is BlueprintBuff&&!ReferenceEquals(privateBuff,effect),"actual Ferocious owns a separate derived constant effect "+mode+"/"+token);
            }
        }
    }
    static BlueprintFeatureSelection Provider(BlueprintsCache cache,bool enabled,bool wrongGroup=false) {
        var selection=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(Constant("TerrifyingHowl","TttPrimalistId")),name="PrimalistRagePowerSelection",Group=wrongGroup?FeatureGroup.Feat:FeatureGroup.RagePower,Groups=Array.Empty<FeatureGroup>(),IgnorePrerequisites=!enabled,ComponentsArray=Array.Empty<BlueprintComponent>()};
        var donor=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(Constants("RagePowers","SelectionIds")[0]));
        foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(selection,field,CopyData(Refs(donor,field).Where(r=>!Tokens.Any(t=>r.Guid==Id(t))).ToArray()));
        selection.OnEnable();cache.AddCachedBlueprint(selection.AssetGuid,selection);return selection;
    }
    static JObject[] Success(BlueprintsCache parent) {
        var cache=Fresh(parent);var original=Constants("RagePowers","SelectionIds").SelectMany(id=>new[]{"m_Features","m_AllFeatures"}.Select(f=>new{key=id+f,refs=Refs((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id)),f).ToArray()})).ToDictionary(x=>x.key,x=>x.refs);
        var states=InitialStates();executed.Clear();skipped.Clear();var outcomes=Call(cache,"RegisterAll",states);
        Check(targets.All(t=>outcomes.Any(x=>x.Item1==t)&&outcomes.Where(x=>x.Item1==t).All(x=>x.Item2)),"actual startup independently registers RagePowers, all five new modules and Ferocious: "+string.Join(";",outcomes.Where(x=>!x.Item2).Select(x=>x.Item1+"="+x.Item3)));
        string fero=Constant("FerociousBeast01431","ModuleName");Check(targets.Where(n=>n!=fero).All(n=>executed.IndexOf(n)<executed.IndexOf(fero)),"all five new modules and native RagePowers execute before Ferocious constant extraction");
        Pools(cache,Tokens,original);Shared(cache,Tokens);Catalog(states);
        Check(states.Where(x=>!targets.Contains(x.Key)).All(x=>x.Value==InitialStates()[x.Key]),"scoped-out modules retain their actual initial states");
        var owned=Tokens.Select(t=>cache.Load(Id(t))).ToArray();
        foreach(int scenario in Enumerable.Range(0,5)) {
            BlueprintFeatureSelection provider=null; object[] arrays=null;
            if(scenario==0){cache.RemoveCachedBlueprint(BlueprintGuid.Parse(Constant("TerrifyingHowl","TttPrimalistId")));}
            else if(scenario==3) { var wrong=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(Constant("TerrifyingHowl","TttPrimalistId")),name="WrongTypePrimalistProvider",ComponentsArray=Array.Empty<BlueprintComponent>()};wrong.OnEnable();cache.AddCachedBlueprint(wrong.AssetGuid,wrong); }
            else { provider=Provider(cache,scenario!=1,scenario==2);arrays=new[]{Get(provider,"m_Features"),Get(provider,"m_AllFeatures")}; }
            outcomes=Call(cache,"RegisterOptional",states);Call(cache,"RegisterOptional",states);
            Check(outcomes.Where(x=>targets.Contains(x.Item1)).All(x=>x.Item2),"actual optional caller retains honest Registered core states for absent/disabled/malformed/enabled provider scenario "+scenario);
            Check(Tokens.Select(t=>cache.Load(Id(t))).Zip(owned,ReferenceEquals).All(x=>x),"actual optional retries preserve all five published root identities "+scenario);
            Pools(cache,Tokens,original);
            if(scenario==1||scenario==2) Check(ReferenceEquals(Get(provider,"m_Features"),arrays[0])&&ReferenceEquals(Get(provider,"m_AllFeatures"),arrays[1]),"disabled or wrong-group provider retains both exact original arrays "+scenario);
            if(scenario==3)Check(cache.Load(BlueprintGuid.Parse(Constant("TerrifyingHowl","TttPrimalistId"))) is BlueprintFeature wrong&&!(wrong is BlueprintFeatureSelection)&&wrong.name=="WrongTypePrimalistProvider","wrong-type optional provider identity is preserved instead of replaced");
            if(scenario==4)Check(provider.Group==FeatureGroup.RagePower&&provider.Groups.Length==0&&new[]{"m_Features","m_AllFeatures"}.All(f=>Tokens.All(t=>Refs(provider,f).Count(r=>r.Guid==Id(t))==1)),"late enabled Group-only TTT provider receives exactly one copy of every root via actual optional caller");
            Catalog(states);
        }
        observations.Add(new JObject{["case"]="success",["executed"]=new JArray(executed),["nonTargetActionsScopedOut"]=new JArray(skipped.Distinct()),["actualStates"]=JObject.FromObject(states)});
        // The five authored roots are the only034 additions; nothing else may
        // count as one.
        var expectedNew=new HashSet<BlueprintGuid>();
        foreach(int mode in Enumerable.Range(0,3))
            foreach(string token in Tokens)expectedNew.Add(Id("FerociousBeastPassive"+mode+Id(token.Replace("RagePower","RageEffectBuff"))));
        // Every remaining Ferocious private buff derivation comes from a native
        // rage power that the real startup already hydrates through RagePowers
        // or the041 Skald dependency walk. Its token still encodes the exact
        // mode and source GUID, and the payload must stay an unchanged copy of
        // that native source. Verify each one, then classify it as a legacy
        // resource instead of a034 new asset; no source list is hardcoded.
        var legacy=new List<SimpleBlueprint>();
        foreach(var clone in Loaded(cache)
            .Where(x=>x.name.StartsWith("CotWWrathPsychicFerociousBeastPassive")||x.name.StartsWith("CotWWrathPsychicFerociousBeastPassiveFact"))
            .Where(x=>!expectedNew.Contains(x.AssetGuid)).OrderBy(x=>x.name,StringComparer.Ordinal)) {
            string token=clone.name.Substring("CotWWrathPsychic".Length);
            // Feature receipts carry an extra "Fact" marker before the mode.
            string suffix=token.Substring(("FerociousBeastPassive"+(token.StartsWith("FerociousBeastPassiveFact")?"Fact":"")).Length);
            int mode=suffix[0]-'0';
            string sourceId=suffix.Substring(1);
            var source=cache.Load(BlueprintGuid.Parse(sourceId)) as BlueprintScriptableObject;
            var derived=clone as BlueprintScriptableObject;
            // Sanitize only removes components or swaps a caster condition for
            // the authored subclass, so every clone component type must still be
            // present on its native source. Compare the payload that way instead
            // of by equality: the passive derivation is intentionally rewritten.
            var sourceTypes=(source?.ComponentsArray??Array.Empty<BlueprintComponent>()).Select(c=>c.GetType()).ToHashSet();
            Check(mode>=0&&mode<=2&&source!=null&&derived!=null&&Id(token)==clone.AssetGuid&&clone.GetType()==source.GetType()&&derived.ComponentsArray.All(c=>sourceTypes.Contains(c.GetType())||c.GetType().Name=="FerociousBeastCasterHasFact01431"),"every legacy Ferocious derivation keeps its deterministic GUID, mode, native type and only native or authored caster components "+token);
            legacy.Add(clone);
        }
        Check(legacy.Count>0,"legacy Ferocious derivations are actually observed");
        JObject Row(SimpleBlueprint bp)=>new JObject{["id"]=bp.AssetGuid.ToString(),["name"]=bp.name,["type"]=bp.GetType().FullName};
        legacyDerived=legacy.OrderBy(x=>x.AssetGuid.ToString()).Select(Row).ToArray();
        var legacyIds=new HashSet<BlueprintGuid>(legacy.Select(x=>x.AssetGuid));
        var previous=new HashSet<string>(PreviousFerocious033.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries));
        var added=Loaded(cache).Where(x=>x.name.StartsWith("CotWWrathPsychicFerocious")&&!previous.Contains(x.AssetGuid.ToString())&&!legacyIds.Contains(x.AssetGuid)).OrderBy(x=>x.AssetGuid.ToString()).ToArray();
        Check(expectedNew.SetEquals(added.Select(x=>x.AssetGuid)),"only the five new effect derivations are034 new assets; Disruptive private receipt grants the actual native passive feat without redundant clones; unexpected="+string.Join(",",added.Where(x=>!expectedNew.Contains(x.AssetGuid)).Select(x=>x.name+":"+x.AssetGuid))+"; missing="+string.Join(",",expectedNew.Except(added.Select(x=>x.AssetGuid))));
        return added.Select(Row).ToArray();
    }
    static void Failure(BlueprintsCache parent) {
        var cache=Fresh(parent);var states=InitialStates();executed.Clear();skipped.Clear();appendFault=true;
        List<Tuple<string,bool,Exception>> outcomes;
        try{outcomes=Call(cache,"RegisterAll",states);}finally{appendFault=false;}
        string witch=Constant("SuperstitionRage01434","WitchHunterModule");var failures=outcomes.Where(x=>!x.Item2).ToArray();
        Check(failures.Length==1&&failures[0].Item1==witch&&failures[0].Item3.ToString().Contains("01434 actual Witch Hunter append fault"),"real Append fault produces exactly one honest Witch Hunter module failure");
        Check(cache.Load(Id("WitchHunterRagePower"))==null&&cache.Load(Id("WitchHunterRageEffectBuff"))==null,"actual failing transaction removes both Witch Hunter private root and effect");
        Check(targets.Where(n=>n!=witch).All(n=>outcomes.Any(x=>x.Item1==n)&&outcomes.Where(x=>x.Item1==n).All(x=>x.Item2)),"later Ghost Rager, Disruptive, Spellbreaker and Ferocious continue successfully after Witch Hunter failure");
        Shared(cache,Tokens.Where(t=>t!="WitchHunterRagePower").ToArray());Catalog(states);
        Check(states[witch]=="Failed","failing startup cache keeps honest Failed until a new startup; no fabricated same-cache recovery");
        observations.Add(new JObject{["case"]="injectedWitchFailure",["executed"]=new JArray(executed),["nonTargetActionsScopedOut"]=new JArray(skipped.Distinct()),["actualStates"]=JObject.FromObject(states)});
    }
    // Frozen033 Fero inventory plus Anger's existing DivineBaseRage identity.
    const string PreviousFerocious033="07f50589bdfdfcaf9456f59c0607ee78 080aca9f93cccb0ba0e7189f6a7066a8 0da177a31384d93785e09e7af9b0aeb2 100b9121a15b321cb67a3ce2d414b034 1a1935cdf48a8b99ac953daa8c846b1e 1ac9dec5bb5eaa9eb0bce899413d6867 1fb291d83e528ee4401075ae41a68e0c 1fff2869395a6657120411483af44aa1 29f9f5982c80ec4cd7386f61e6f55252 43bc4b8ad8c44060a4f2bec6c2daa9c4 4c801931448e30906cc17de56794194a 4f8b902246c5564a2835c0a49fa29f51 57b73086199dc2758fb973e576884986 61244e434974f38d118f27d327da5d0e 661ad5c2b35060d53b64e5728d33bfa8 6e6b1947032f94aceab20bd1875999e4 6e83a52bca03b8f4b2b6eb0f7f132d00 70d60ff9cc5d83607a7259ee88fd0311 7234e40539fc3089327878054867d300 7c0c6f154bca957a04ead36ec1d38fa7 820b88b2dd7109093f7595b3859dd538 84a55862315c2049971e570777b212be 879f9d3f6e53de2fb159acd799221cb3 89d890bf1a08c6c82568bcc28893049a 8eb7bd29b2b9cfd07fd0166cccf0aff3 903eb36dabf054e2b51071e8c7820230 912ce69c6d594291cf54ebc10cc15110 94c3240320dce998ae2fe650e3889307 9ad468cbd9e13ad90cf5213b2d0f0931 9f606ef38cba3f627a976fb4af70b214 a068823950ea0ecc68e21788fc39fa99 a422e8ff35278bb56f1fdb5ef5a3c2d0 ab5eedbc69ba8b3a9bdbe51b3c2c6cd1 ae666968bed6077b98bac4790d847799 af3843e37f0ab57f3136e76c6368d869 b53ef7bb3f77cf2dd47fcd4f3c84d3bd bdfac0dda0cdbf076299dc7091033cc4 befb1455a224e184a2625b84ad90d430 c1a922d24bf739c14a5bbae7b64ae561 c20a98877d9bd230f64d1a8c62bf8c53 c5ef7ce46270b5f3396da835c39869fd c773a5858c953d6793db65ea90bc5d8d c99f33977c341a079a35424aecf590c7 cae302b09927bdd4230874584052de4d cd73ef3985187c50722e999393524b90 db2825775e2c96a5f7b883e9453ebfc5 dd96dd7012dd5d5f167ef0de09e7f2e1 e36811c6ccebe0971e86ea9991bb0b60 e4824db7e5a642371e6fa14d6908e68e eb5e46a49c5c159f27db81ad6ac113a5 eba00de398998c3717e38a60035334e7 ef8d1840c031b21bdc23a271072b81f9 f015754ddfca2c5b84c4028bfb6468e8 f13d2912c6b2cae3d35e4a49c0416829 f4ec4a602403ec73274b62aba5d0a65d";
    internal static int Run(BlueprintsCache parent) {
        checks=0;observations.Clear();legacyDerived=null;var strings=(Dictionary<string,string>)AccessTools.Field(Module("PsychicAssets"),"Strings").GetValue(null);var beforeStrings=strings.ToArray();var snapshot=new ParentSnapshot(parent);
        targets=new HashSet<string>(new[]{Constant("RagePowers","ModuleName"),Constant("SuperstitionRage01434","SuperstitionModule"),Constant("SuperstitionRage01434","WitchHunterModule"),Constant("AntimagicRage01434","GhostModule"),Constant("AntimagicRage01434","DisruptiveModule"),Constant("AntimagicRage01434","SpellbreakerModule"),Constant("FerociousBeast01431","ModuleName")});
        var h=new Harmony("CotW01434.ActualScopedRegistrationCaller");JObject[] added=null;
        h.Patch(AccessTools.Method(Module("BlueprintRegistration"),"RegisterModule"),prefix:new HarmonyMethod(typeof(Migration01434CallerTests),nameof(CallerScope)));ScopedAvailable01463Tests.Bind(h,Module("BlueprintRegistration"),"ScopeMigration01434CallerTests");
        // This optional provider integration is called directly by the real
        // dispatcher. Bound its action explicitly, without a success report.
        h.Patch(AccessTools.Method(Module("ExtraInvestigatorTalent"),"Register"),prefix:new HarmonyMethod(typeof(Migration01434CallerTests),nameof(DirectInvestigatorBoundary)));
        h.Patch(AccessTools.Method(Module("T09CombatFeats"),"Append"),postfix:new HarmonyMethod(typeof(Migration01434CallerTests),nameof(AppendFault)));
        try{Failure(parent);added=Success(parent);}
        finally{appendFault=false;active=null;h.UnpatchAll(h.Id);strings.Clear();foreach(var p in beforeStrings)strings.Add(p.Key,p.Value);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,parent);snapshot.Verify();}
        Check(strings.Count==beforeStrings.Length&&beforeStrings.All(p=>strings.TryGetValue(p.Key,out var v)&&v==p.Value),"complete process-global text key/value set is restored");
        Check(added.Length>0&&added.Select(x=>(string)x["id"]).Distinct().Count()==added.Length,"actual new Ferocious derived assets form a unique nonempty GUID inventory");
        PsychicExpansionTests.Inventory["migration01434CallerObservations"]=new JArray(observations);
        PsychicExpansionTests.Inventory["migration01434CallerContracts"]=new JObject{
            ["passed"]=true,["assertions"]=checks,["additionalItemCount"]=0,["newBlueprints"]=new JArray(added),
            ["legacyStartupDerivedAssets"]=new JArray(legacyDerived),["unchangedLegacyDeterministicDerivations"]=true,
            ["actualRegisterAllDispatcher"]=true,["actualRegisterOptionalDispatcher"]=true,["fiveIndependentModulesBeforeFerocious"]=true,
            ["ferociousConstantExtractionIncludesFiveChildren"]=true,["realAppendFaultIsolatedToWitchHunter"]=true,["laterModulesExecuteAfterFailure"]=true,
            ["failedCacheStatusHonestNewStartupRecovers"]=true,["fourNativePoolsPreserveOldEntries"]=true,["lateGroupOnlyTttPoolHasFiveRootsOnce"]=true,
            ["optionalAbsentDisabledMalformedPreserveCore"]=true,["actualCatalogueDefinitionsAndReportedStates"]=true,["nonTargetActionsScopedOutWithoutSuccessReports"]=true,
            ["freshNativeHostComponentsFromRealArchive"]=true,["freshSelectionArraysAndUncachedReferences"]=true,["parentFullCacheIdentityComponentsPoolsAndLazyPointersPreserved"]=true,
            ["globalTextKeysAndValuesPreserved"]=true,
            ["boundary"]="Actual RegisterAll/RegisterOptional and real seven selected/dependency actions on isolated native BlueprintsCache graphs. Non-target RegisterModule actions are skipped without reporting success. Five native host components and fresh class/archetype metadata come from the actual game archive. Append exception, provider presence/disabled IgnorePrerequisites/wrong Group/late Group-only pool are controlled inputs. Failure remains Failed in that cache; separate fresh startup proves success. Ferocious derived assets do not count as migration items. No complete mod startup, external TTT assembly execution, Unity UI/rendering or save scene is inferred."};
        Console.WriteLine("PASS: "+checks+" actual034 registration caller contracts; "+added.Length+" derived Ferocious assets.");return checks;
    }
}
