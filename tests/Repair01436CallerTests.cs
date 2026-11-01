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
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

// Actual registration dispatch only; rule execution belongs to the two discovery/masterpiece suites.
internal static class Repair01436CallerTests
{
    static int checks, poolAssertions, freshSpellListAssertions;
    static string optionalFaultPool;

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
    static string Definition(object value) => (string)AccessTools.Method(Module("Migration01426"), "Definition").Invoke(null, new object[] { value });
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => (BlueprintFeatureReference[])Get(p, field) ?? Array.Empty<BlueprintFeatureReference>();
    static object Cached(BlueprintReferenceBase r) => AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").GetValue(r, null);
    static void Uncache(BlueprintReferenceBase r) => AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, null, null);
    static SimpleBlueprint[] Loaded(BlueprintsCache cache) => ((IDictionary)Get(cache, "m_LoadedBlueprints")).Values.Cast<object>().Select(entry=>(SimpleBlueprint)Get(entry,"Blueprint")).Where(bp=>bp!=null).ToArray();
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Repair01436 caller: " + why); checks++; }
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
        // Native reference proxies resolve BlueprintReference.Get() while enumerating.
        // Inspect their authored fields without warming the parent's lazy pointers.
        if (!(t.Namespace=="Kingmaker.Blueprints"&&t.Name.StartsWith("Reference",StringComparison.Ordinal)&&t.Name.Contains("Proxy"))&&value is IEnumerable sequence) { foreach (var item in sequence) foreach (var reference in References(item,seen)) yield return reference; yield break; }
        foreach (var f in Fields(t)) foreach (var reference in References(f.GetValue(value),seen)) yield return reference;
    }
    static IEnumerable<BlueprintReferenceBase> BlueprintReferences(SimpleBlueprint bp) => Fields(bp.GetType()).SelectMany(f=>References(f.GetValue(bp),new HashSet<object>(Identity.Instance))).Distinct<BlueprintReferenceBase>(Identity.Instance);
    sealed class ParentSnapshot {
        readonly BlueprintsCache cache; readonly SimpleBlueprint[] objects;
        readonly Dictionary<BlueprintScriptableObject,BlueprintComponent[]> arrays;
        readonly Dictionary<BlueprintScriptableObject,BlueprintComponent[]> componentItems;
        readonly Dictionary<BlueprintScriptableObject,string> definitions;
        readonly Dictionary<BlueprintFeatureSelection,object[]> pools;
        readonly Dictionary<BlueprintSpellList,SpellLevelList[]> spellRows;
        readonly Dictionary<BlueprintSpellList,SpellLevelList[]> spellRowItems;
        readonly Dictionary<BlueprintSpellList,int> spellMaxLevels;
        readonly Dictionary<SpellLevelList,object[]> spellRowData;
        readonly BlueprintReferenceBase[] references; readonly object[] pointers;
        internal ParentSnapshot(BlueprintsCache value) {
            cache=value; objects=Loaded(cache);
            // Capture before authored-shape checks as well: snapshot creation
            // must not hide a lazy target warmed by its own verification work.
            references=objects.SelectMany(BlueprintReferences).Distinct<BlueprintReferenceBase>(Identity.Instance).ToArray(); pointers=references.Select(Cached).ToArray();
            arrays=objects.OfType<BlueprintScriptableObject>().ToDictionary(x=>x,x=>x.ComponentsArray);
            componentItems=arrays.ToDictionary(x=>x.Key,x=>x.Value?.ToArray());
            definitions=arrays.ToDictionary(x=>x.Key,x=>Definition(x.Value));
            pools=objects.OfType<BlueprintFeatureSelection>().ToDictionary(x=>x,x=>new object[]{Get(x,"m_Features"),Get(x,"m_AllFeatures"),Refs(x,"m_Features").ToArray(),Refs(x,"m_AllFeatures").ToArray()});
            spellRows=objects.OfType<BlueprintSpellList>().ToDictionary(x=>x,x=>x.SpellsByLevel);
            spellRowItems=spellRows.ToDictionary(x=>x.Key,x=>x.Value?.ToArray());
            spellMaxLevels=spellRows.ToDictionary(x=>x.Key,x=>(int)Get(x.Key,"m_MaxLevel"));
            spellRowData=spellRows.Values.Where(v=>v!=null).SelectMany(v=>v).Where(v=>v!=null).Distinct().ToDictionary(x=>x,x=>new object[]{x.SpellLevel,Get(x,"m_Spells"),Get(x,"m_SpellsProxy"),((List<BlueprintAbilityReference>)Get(x,"m_Spells"))?.ToArray(),Get(x,"m_SpellsFiltered"),(Get(x,"m_SpellsFiltered") as List<BlueprintAbility>)?.ToArray()});
        }
        internal void Verify() {
            var actual=Loaded(cache).ToDictionary(x=>x.AssetGuid);
            Check(actual.Count==objects.Length && objects.All(x=>actual.TryGetValue(x.AssetGuid,out var p)&&ReferenceEquals(p,x)),"parent full cache GUID/object graph remains exact");
            Check(arrays.All(x=>ReferenceEquals(x.Key.ComponentsArray,x.Value)) && componentItems.All(x=>x.Value==null?x.Key.ComponentsArray==null:x.Key.ComponentsArray.Length==x.Value.Length&&x.Key.ComponentsArray.Zip(x.Value,ReferenceEquals).All(v=>v)) && definitions.All(x=>Definition(x.Key.ComponentsArray)==x.Value),"parent all component array/element identities and authored graph definitions remain exact");
            Check(pools.All(x=>ReferenceEquals(Get(x.Key,"m_Features"),x.Value[0])&&ReferenceEquals(Get(x.Key,"m_AllFeatures"),x.Value[1])&&Refs(x.Key,"m_Features").Length==((BlueprintFeatureReference[])x.Value[2]).Length&&Refs(x.Key,"m_AllFeatures").Length==((BlueprintFeatureReference[])x.Value[3]).Length&&Refs(x.Key,"m_Features").Zip((BlueprintFeatureReference[])x.Value[2],ReferenceEquals).All(v=>v)&&Refs(x.Key,"m_AllFeatures").Zip((BlueprintFeatureReference[])x.Value[3],ReferenceEquals).All(v=>v)),"parent all selection array identities, entries and order remain exact");
            Check(references.Select(Cached).Zip(pointers,ReferenceEquals).All(x=>x),"parent every observed lazy reference Cached pointer remains exact, including null pointers");
            Check(spellRows.All(p=>ReferenceEquals(p.Key.SpellsByLevel,p.Value))&&spellRowItems.All(p=>p.Value==null?p.Key.SpellsByLevel==null:p.Key.SpellsByLevel.Length==p.Value.Length&&p.Key.SpellsByLevel.Zip(p.Value,ReferenceEquals).All(v=>v))&&spellMaxLevels.All(p=>(int)Get(p.Key,"m_MaxLevel")==p.Value)&&spellRowData.All(p=>p.Key.SpellLevel==(int)p.Value[0]&&ReferenceEquals(Get(p.Key,"m_Spells"),p.Value[1])&&ReferenceEquals(Get(p.Key,"m_SpellsProxy"),p.Value[2])&&ReferenceEquals(Get(p.Key,"m_SpellsFiltered"),p.Value[4])&&(((BlueprintAbility[])p.Value[5])==null?Get(p.Key,"m_SpellsFiltered")==null:((List<BlueprintAbility>)Get(p.Key,"m_SpellsFiltered")).SequenceEqual((BlueprintAbility[])p.Value[5]))&&(((BlueprintAbilityReference[])p.Value[3])==null?Get(p.Key,"m_Spells")==null:((List<BlueprintAbilityReference>)Get(p.Key,"m_Spells")).Count==((BlueprintAbilityReference[])p.Value[3]).Length&&((List<BlueprintAbilityReference>)Get(p.Key,"m_Spells")).Zip((BlueprintAbilityReference[])p.Value[3],ReferenceEquals).All(v=>v))),"parent native spell-list array/row identities and order, row level, original lists/reference identities/order, filtered lists, max-level caches and proxies remain exact");
        }
    }
    static bool IsOwned(SimpleBlueprint bp) => OwnIds.Contains(bp.AssetGuid);
    static BlueprintsCache Fresh(BlueprintsCache parent) {
        var cache=new BlueprintsCache();
        var omitted=new HashSet<BlueprintGuid>(OwnIds);
        foreach(string id in new[]{DiscoveryId,PrestigeId,PrestigeCoreId}.Concat(Constants("BardMasterpieces01435","PrestigeFeatures")).Concat(Constants("BardMasterpieces01435","PrestigeAbilities")))omitted.Add(BlueprintGuid.Parse(id));
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
            if(copy is BlueprintSpellList list)list.SpellsByLevel=CopyData(((BlueprintSpellList)bp).SpellsByLevel);
            if(copy is BlueprintSpellList listRows && listRows.SpellsByLevel!=null)
                foreach(var row in listRows.SpellsByLevel.Where(r=>r!=null)) {
                    var refs=Get(row,"m_Spells") as List<BlueprintAbilityReference>;
                    if(refs!=null&&refs.Any(r=>r!=null&&omitted.Contains(r.Guid))){Set(row,"m_Spells",refs.Where(r=>r==null||!omitted.Contains(r.Guid)).ToList());Set(row,"m_SpellsProxy",null);Set(row,"m_SpellsFiltered",null);}
                }
            // The unconditional row copy above can create new reference objects;
            // uncache the final graph, not its superseded preliminary copy.
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
        Check(new[]{DiscoveryId,PrestigeId,PrestigeCoreId}.All(id=>cache.Load(BlueprintGuid.Parse(id))==null),"fresh cache genuinely has neither optional provider before installation");
        int freshListsStarted=checks;
        foreach(var list in Loaded(cache).OfType<BlueprintSpellList>()){
            var original=(BlueprintSpellList)parent.Load(list.AssetGuid);
            bool independent=list.SpellsByLevel==null?original.SpellsByLevel==null:!ReferenceEquals(list.SpellsByLevel,original.SpellsByLevel)&&list.SpellsByLevel.Length==original.SpellsByLevel.Length;
            if(list.SpellsByLevel!=null)for(int i=0;i<list.SpellsByLevel.Length;i++){
                var row=list.SpellsByLevel[i];var old=original.SpellsByLevel[i];
                independent&=row==null?old==null:!ReferenceEquals(row,old)&&(Get(row,"m_Spells")==null||!ReferenceEquals(Get(row,"m_Spells"),Get(old,"m_Spells")));
            }
            Check(independent,"fresh spell lists own independent row arrays, rows and reference lists including empty rows: "+list.AssetGuid);
            Check(BlueprintReferences(list).All(r=>Cached(r)==null),"fresh spell-list final row/reference/proxy graph does not retain parent cached targets: "+list.AssetGuid);
        }
        freshSpellListAssertions+=checks-freshListsStarted;
        Check(Constants("WizardDiscoveries01435","CorePoolIds").Select(id=>(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).All(p=>new[]{"m_Features","m_AllFeatures"}.All(f=>!ReferenceEquals(Get(p,f),Get((BlueprintFeatureSelection)parent.Load(p.AssetGuid),f))&&Refs(p,f).All(r=>Cached(r)==null))),"fresh four discovery native pools own distinct arrays and uncached cloned references");
        return cache;
    }

    static void ParentSpellListVerifierProbe(BlueprintsCache parent){
        var c=Fresh(parent);var snapshot=new ParentSnapshot(c);
        var list=(BlueprintSpellList)c.Load(BlueprintGuid.Parse(Constant("BardMasterpieces01435","BardSpellList")));
        var row=list.SpellsByLevel.First(r=>r!=null&&Get(r,"m_Spells") is List<BlueprintAbilityReference>);
        var original=(List<BlueprintAbilityReference>)Get(row,"m_Spells");
        bool rejected=false;
        try{
            Set(row,"m_Spells",new List<BlueprintAbilityReference>(original));
            try{snapshot.Verify();}catch(Exception e){rejected=e.Message.Contains("parent native spell-list array/row identities");}
        }finally{Set(row,"m_Spells",original);}
        Check(rejected,"parent snapshot rejects a same-contents replacement of a real native Bard row reference list");
        snapshot.Verify();
        observations.Add(new JObject{["case"]="nativeParentSpellListVerifierRejectsReplacementAndRestoresExactState"});
    }

    static string DiscoveryId=>Constant("WizardDiscoveries01435","DiscoveryPoolId");
    static string PrestigeId=>Constant("BardMasterpieces01435","PrestigeSelection");
    static string PrestigeCoreId=>Constant("BardMasterpieces01435","PrestigeCore");
    static BlueprintFeatureSelection Discovery(BlueprintsCache c,int shape=0)=>(BlueprintFeatureSelection)AccessTools.Method(typeof(WizardDiscoveries01435Tests),"InstallProvider01436").Invoke(null,new object[]{c,shape});
    static void Prestige(BlueprintsCache c,int shape=0)=>AccessTools.Method(typeof(BardMasterpieces01435Tests),"InstallPrestigeProvider01436").Invoke(null,new object[]{c,shape});
    static bool CallerScope(string name) {if(targets.Contains(name)){executed.Add(name);return true;}skipped.Add(name);return false;}
    static bool DirectInvestigatorBoundary(ref bool __result){skipped.Add(Constant("ExtraInvestigatorTalent","ModuleName"));__result=false;return false;}
    static void CacheFault(BlueprintGuid __0){if(cacheFaultToken!=null&&__0==Id(cacheFaultToken))throw new InvalidOperationException("01436 actual native AddCachedBlueprint fault "+cacheFaultToken);}
    static void AppendFault(IEnumerable<BlueprintFeatureSelection> __0,BlueprintFeature[] feats){if(optionalFaultPool!=null&&__0.Any(p=>p.AssetGuid==BlueprintGuid.Parse(optionalFaultPool))&&feats.Any(f=>RootTokens.Any(t=>f.AssetGuid==Id(t))))throw new InvalidOperationException("01436 actual optional post-Append fault");}
    static Dictionary<string,string> InitialStates()=>(Dictionary<string,string>)AccessTools.Method(Module("ModuleCatalog"),"InitialStates").Invoke(null,null);
    static string[] ModuleNames()=>new[]{Constant("WizardDiscoveries01435","ForestModule"),Constant("WizardDiscoveries01435","ResilientModule"),Constant("BardMasterpieces01435","ClamorModule"),Constant("BardMasterpieces01435","DanceModule"),Constant("BardMasterpieces01435","SymphonyModule")};
    static void Catalog(Dictionary<string,string> states){
        var defs=((IEnumerable)AccessTools.Field(Module("ModuleCatalog"),"Definitions").GetValue(null)).Cast<object>().ToArray();
        var rows=((IEnumerable)AccessTools.Method(Module("ModuleCatalog"),"Snapshot").Invoke(null,new object[]{states})).Cast<object>().ToArray();
        Check(defs.Select(d=>(string)Get(d,"Name")).Distinct().Count()==defs.Length&&rows.Length==defs.Length,"real module definitions determine both status and name counts");
        Check(rows.Select((r,i)=>ReferenceEquals(Get(r,"Definition"),defs[i])&&(string)Get(r,"State")==states[(string)Get(defs[i],"Name")]).All(x=>x),"real menu snapshot matches exact actual caller states");
    }
    static List<Tuple<string,bool,Exception>> Call(BlueprintsCache cache,string method,Dictionary<string,string> states){
        var outcomes=new List<Tuple<string,bool,Exception>>();
        Action<string,bool,Exception> report=(name,ok,error)=>{states[name]=ok?"Registered":"Failed";outcomes.Add(Tuple.Create(name,ok,error));};
        AccessTools.Method(Module("BlueprintRegistration"),method).Invoke(null,new object[]{cache,report});
        Check(outcomes.All(o=>targets.Contains(o.Item1))&&skipped.All(s=>outcomes.All(o=>o.Item1!=s)),"non-target actions are explicitly scoped out, never reported as successful");
        return outcomes;
    }
    static Dictionary<string,BlueprintFeatureReference[]> PoolSnapshot(BlueprintsCache c)=>Loaded(c).OfType<BlueprintFeatureSelection>().SelectMany(p=>new[]{"m_Features","m_AllFeatures"}.Select(f=>new{key=p.AssetGuid+"/"+f,refs=Refs(p,f).ToArray()})).ToDictionary(x=>x.key,x=>x.refs);
    static bool RetiredChoice(BlueprintFeatureReference r)=>r!=null&&(RootTokens.Any(t=>r.Guid==Id(t))||r.Guid==Id("BardicMasterpieces01435Selection"));
    static void Pools(BlueprintsCache c,Dictionary<string,BlueprintFeatureReference[]> before){
        int started=checks;
        foreach(var entry in before){
            var bits=entry.Key.Split('/');var now=Refs((BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(bits[0])),bits[1]);
            var expected=entry.Value.Where(r=>!RetiredChoice(r)).ToArray();var kept=now.Where(r=>!RetiredChoice(r)).ToArray();
            Check(kept.Length==expected.Length&&kept.Zip(expected,ReferenceEquals).All(x=>x),"all unrelated reference objects and order survive exact scoped cleanup: "+entry.Key);
            Check(now.Where(RetiredChoice).GroupBy(r=>r.Guid).All(g=>g.Count()==1),"no owned leaf is registered twice: "+entry.Key);
        }
        foreach(var pool in Loaded(c).OfType<BlueprintFeatureSelection>())foreach(string field in new[]{"m_Features","m_AllFeatures"}){
            var refs=Refs(pool,field);
            Check(refs.All(r=>r==null||r.Guid!=Id("BardicMasterpieces01435Selection")),"legacy CotW container is absent from every actual choice pool: "+pool.AssetGuid+"/"+field);
            foreach(string token in RootTokens){
                string allowed=RootTokens.Take(2).Contains(token)?DiscoveryId:PrestigeId;
                Check(pool.AssetGuid==BlueprintGuid.Parse(allowed)||refs.All(r=>r==null||r.Guid!=Id(token)),"owned leaf appears only in its provider submenu: "+token+"/"+pool.AssetGuid+"/"+field);
            }
        }
        poolAssertions+=checks-started;
    }
    static void NoDirectRoots(BlueprintsCache c){
        foreach(string id in Constants("WizardDiscoveries01435","CorePoolIds")){
            var p=c.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection;
            Check(p!=null&&new[]{"m_Features","m_AllFeatures"}.All(f=>Refs(p,f).All(r=>!RetiredChoice(r))),"ordinary/mythic/Wizard/Secret direct roots are removed: "+id);
        }
        var legacy=c.Load(Id("BardicMasterpieces01435Selection")) as BlueprintFeatureSelection;
        Check(legacy!=null&&legacy.HideInUI,"old private selection identity remains hidden for existing saved facts, not a new choice");
    }
    static JObject[] Inventory(BlueprintsCache c)=>Loaded(c).Where(IsOwned).OrderBy(b=>b.AssetGuid.ToString()).Select(b=>new JObject{["id"]=b.AssetGuid.ToString(),["name"]=b.name,["type"]=b.GetType().FullName}).ToArray();
    static void SeedLegacy(BlueprintsCache c){
        foreach(var p in Loaded(c).OfType<BlueprintFeatureSelection>().Where(p=>Constants("WizardDiscoveries01435","CorePoolIds").Contains(p.AssetGuid.ToString())))foreach(string f in new[]{"m_Features","m_AllFeatures"}){
            var extras=RootTokens.Concat(new[]{"BardicMasterpieces01435Selection"}).Select(t=>{var r=new BlueprintFeatureReference();Set(r,"deserializedGuid",Id(t));return r;});
            Set(p,f,Refs(p,f).Concat(extras).ToArray());
        }
    }
    static void CoreFailure(BlueprintsCache parent,string token,bool providers=false){
        var c=Fresh(parent);Discovery(c,providers?0:2);Prestige(c,providers?0:1);var before=PoolSnapshot(c);var states=InitialStates();executed.Clear();skipped.Clear();cacheFaultToken=token;
        List<Tuple<string,bool,Exception>> outcomes;
        try{outcomes=Call(c,"RegisterAll",states);}finally{cacheFaultToken=null;}
        string failed=ModuleNames()[Array.IndexOf(RootTokens,token)];var errors=outcomes.Where(o=>!o.Item2).ToArray();
        Check(errors.Length>=1&&errors.All(e=>e.Item1==failed&&e.Item3!=null&&e.Item3.ToString().Contains("01436 actual native AddCachedBlueprint fault")),"real cache fault fails only its owning module: "+token+"; actual errors: "+string.Join("; ",errors.Select(e=>e.Item1+"="+e.Item3)));
        Check(c.Load(Id(token))==null&&states[failed]=="Failed","failed root is rolled back and status remains truthful: "+token);
        var privateTokens=Array.IndexOf(RootTokens,token)<2?new[]{token}:Constants("BardMasterpieces01435","OwnTokens").Where(t=>t.StartsWith(token,StringComparison.Ordinal)).ToArray();
        Check(privateTokens.All(t=>c.Load(Id(t))==null),"entire failed module-private asset graph is absent after native insertion failure: "+token);
        if(providers)Check(Loaded(c).OfType<BlueprintSpellList>().SelectMany(l=>l.SpellsByLevel??Array.Empty<SpellLevelList>()).Where(r=>r!=null).SelectMany(r=>(List<BlueprintAbilityReference>)Get(r,"m_Spells")??new List<BlueprintAbilityReference>()).All(r=>r==null||!privateTokens.Any(t=>r.Guid==Id(t))),"valid providers never receive a failed module's acquisition marker");
        Check(targets.Where(t=>t!=failed).All(t=>outcomes.Any(o=>o.Item1==t)&&outcomes.Where(o=>o.Item1==t).All(o=>o.Item2)),"other four real module actions continue after failure: "+token);
        var optional=Call(c,"RegisterOptional",states);Check(optional.All(o=>o.Item1!=failed)&&states[failed]=="Failed","root-gated optional retry cannot hide failed core registration");
        Pools(c,before);Catalog(states);observations.Add(new JObject{["case"]="nativeCacheFault:"+token+(providers?":validProviders":""),["actualStates"]=JObject.FromObject(states)});
    }
    static void Connected(BlueprintsCache c){
        var discovery=c.Load(BlueprintGuid.Parse(DiscoveryId)) as BlueprintFeatureSelection;var prestige=c.Load(BlueprintGuid.Parse(PrestigeId)) as BlueprintFeatureSelection;
        Check(discovery!=null&&prestige!=null,"actual recognized provider submenu objects remain");
        foreach(string f in new[]{"m_Features","m_AllFeatures"}){
            foreach(string t in RootTokens.Take(2))Check(Refs(discovery,f).Count(r=>r!=null&&r.Guid==Id(t))==1,"two discoveries are provider children exactly once");
            foreach(string t in RootTokens.Skip(2))Check(Refs(prestige,f).Count(r=>r!=null&&r.Guid==Id(t))==1,"three masterpieces are PrestigePlus children exactly once");
        }
    }
    static JObject[] Success(BlueprintsCache parent){
        var c=Fresh(parent);Discovery(c,2);Prestige(c,1);SeedLegacy(c);var before=PoolSnapshot(c);var states=InitialStates();executed.Clear();skipped.Clear();
        var outcomes=Call(c,"RegisterAll",states);
        Check(targets.All(t=>outcomes.Any(o=>o.Item1==t)&&outcomes.Where(o=>o.Item1==t).All(o=>o.Item2)),"actual RegisterAll independently publishes all five existing effect graphs without providers");
        Check(ModuleNames().Select(n=>executed.IndexOf(n)).SequenceEqual(ModuleNames().Select(n=>executed.IndexOf(n)).OrderBy(x=>x)),"five first actual module actions retain their order");
        Pools(c,before);NoDirectRoots(c);Catalog(states);
        var owned=Loaded(c).Where(IsOwned).ToDictionary(b=>b.AssetGuid,b=>b);
        Check(new HashSet<BlueprintGuid>(owned.Keys).SetEquals(OwnIds),"all retained assets and new acquisition-only resources are actually published");
        Discovery(c,0);Prestige(c,0);before=PoolSnapshot(c);
        foreach(int repeat in Enumerable.Range(0,3)){
            outcomes=Call(c,"RegisterOptional",states);
            Check(targets.All(t=>outcomes.Any(o=>o.Item1==t)&&outcomes.Where(o=>o.Item1==t).All(o=>o.Item2)),"late optional providers retain honest success: "+repeat);
            Connected(c);Pools(c,before);NoDirectRoots(c);Catalog(states);
            Check(owned.All(p=>ReferenceEquals(c.Load(p.Key),p.Value)),"every already-published own object retains identity through late/repeated providers");
        }
        Check(states.Where(p=>!targets.Contains(p.Key)).All(p=>p.Value==InitialStates()[p.Key]),"non-target modules retain original native initial states");
        observations.Add(new JObject{["case"]="legacyCleanupAbsentThenLateCompleteAndRepeatedProviders",["actualStates"]=JObject.FromObject(states),["executed"]=new JArray(executed),["nonTargetActionsScopedOut"]=new JArray(skipped.Distinct())});
        return Inventory(c);
    }
    static void ProviderShape(BlueprintsCache parent,bool wizard,int shape){
        var c=Fresh(parent);Discovery(c,wizard?shape:2);Prestige(c,wizard?1:shape);var before=PoolSnapshot(c);var states=InitialStates();executed.Clear();skipped.Clear();
        var rawWizard=c.Load(BlueprintGuid.Parse(DiscoveryId)) as BlueprintScriptableObject;var rawPrestige=c.Load(BlueprintGuid.Parse(PrestigeId)) as BlueprintScriptableObject;
        var snapshots=new[]{rawWizard,rawPrestige}.Where(p=>p!=null).ToDictionary(p=>p,p=>new{components=p.ComponentsArray,definition=Definition(p.ComponentsArray),features=p is BlueprintFeatureSelection s?Get(s,"m_Features"):null,all=p is BlueprintFeatureSelection q?Get(q,"m_AllFeatures"):null});
        Call(c,"RegisterAll",states);Call(c,"RegisterOptional",states);
        Check(targets.All(t=>states[t]=="Registered"),"absent/incomplete optional provider cannot fail existing five effects");
        if((wizard&&shape>=3)||(!wizard&&shape>=2))foreach(var pair in snapshots){var bp=pair.Key;var s=pair.Value;Check(ReferenceEquals(bp.ComponentsArray,s.components)&&Definition(bp.ComponentsArray)==s.definition&&(!(bp is BlueprintFeatureSelection)||ReferenceEquals(Get(bp,"m_Features"),s.features)&&ReferenceEquals(Get(bp,"m_AllFeatures"),s.all)),"malformed external object is preserved without partial connection");}
        Pools(c,before);NoDirectRoots(c);Catalog(states);observations.Add(new JObject{["case"]=(wizard?"discoveryShape:":"prestigeShape:")+shape,["actualStates"]=JObject.FromObject(states)});
    }
    static void OptionalAppendFault(BlueprintsCache parent,bool wizard){
        var c=Fresh(parent);Discovery(c,0);Prestige(c,0);var before=PoolSnapshot(c);var states=InitialStates();executed.Clear();skipped.Clear();optionalFaultPool=wizard?DiscoveryId:PrestigeId;
        var faulted=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(optionalFaultPool));var arrays=new[]{Get(faulted,"m_Features"),Get(faulted,"m_AllFeatures")};
        try{Call(c,"RegisterAll",states);Call(c,"RegisterOptional",states);}finally{optionalFaultPool=null;}
        Check(targets.All(t=>states[t]=="Registered"),"post-mutation provider exception is isolated from all five core states");
        Check(ReferenceEquals(Get(faulted,"m_Features"),arrays[0])&&ReferenceEquals(Get(faulted,"m_AllFeatures"),arrays[1]),"post-Append fault restores exact original provider arrays");
        string module=wizard?"WizardDiscoveries01435":"BardMasterpieces01435";
        Check(((string)AccessTools.Field(Module(module),"LastOptionalDiagnostic").GetValue(null))?.Contains("01436 actual optional post-Append fault")==true,"optional failure is recorded with its actual cause");
        var roots=RootTokens.Select(t=>c.Load(Id(t))).ToArray();Call(c,"RegisterOptional",states);Call(c,"RegisterOptional",states);
        Check(roots.Zip(RootTokens.Select(t=>c.Load(Id(t))),ReferenceEquals).All(x=>x),"successful optional retry never republishes existing own roots");
        Connected(c);Pools(c,before);NoDirectRoots(c);Catalog(states);observations.Add(new JObject{["case"]=wizard?"nativeDiscoveryPostAppendFaultAndRetry":"nativePrestigePostAppendFaultAndRetry",["actualStates"]=JObject.FromObject(states)});
    }
    static void ForeignLegacyContainer(BlueprintsCache parent){
        var c=Fresh(parent);Discovery(c,2);Prestige(c,1);var before=PoolSnapshot(c);var states=InitialStates();executed.Clear();skipped.Clear();
        var foreign=new BlueprintFeatureSelection{AssetGuid=Id("BardicMasterpieces01435Selection"),name="CotWWrathPsychicBardicMasterpieces01435Selection",Ranks=10,Groups=new[]{FeatureGroup.Feat},ComponentsArray=Array.Empty<BlueprintComponent>()};
        foreach(string f in new[]{"m_Features","m_AllFeatures"})Set(foreign,f,Array.Empty<BlueprintFeatureReference>());
        foreign.OnEnable();c.AddCachedBlueprint(foreign.AssetGuid,foreign);var arrays=new[]{Get(foreign,"m_Features"),Get(foreign,"m_AllFeatures")};var components=foreign.ComponentsArray;
        var outcomes=Call(c,"RegisterAll",states);var errors=outcomes.Where(o=>!o.Item2).ToArray();
        Check(errors.Select(o=>o.Item1).Distinct().OrderBy(x=>x).SequenceEqual(ModuleNames().Skip(2).OrderBy(x=>x)),"foreign same-GUID/name/type legacy selection fails all three own publications only");
        Check(ModuleNames().Take(2).All(n=>states[n]=="Registered"),"foreign legacy identity leaves both real discovery actions successful");
        Check(ReferenceEquals(c.Load(foreign.AssetGuid),foreign)&&ReferenceEquals(foreign.ComponentsArray,components)&&ReferenceEquals(Get(foreign,"m_Features"),arrays[0])&&ReferenceEquals(Get(foreign,"m_AllFeatures"),arrays[1])&&!foreign.HideInUI&&RootTokens.Skip(2).All(t=>c.Load(Id(t))==null),"foreign legacy object is never adopted, hidden, rebuilt or extended and no rejected root remains");
        Call(c,"RegisterOptional",states);Check(ModuleNames().Skip(2).All(n=>states[n]=="Failed"),"optional dispatch cannot conceal core foreign-identity failures");
        Pools(c,before);Catalog(states);observations.Add(new JObject{["case"]="foreignSameIdentityLegacyContainerRejectedAndPreserved",["actualStates"]=JObject.FromObject(states)});
    }
    internal static int Run(BlueprintsCache parent){
        checks=0;poolAssertions=0;freshSpellListAssertions=0;observations.Clear();cacheFaultToken=null;optionalFaultPool=null;
        OwnIds=new HashSet<BlueprintGuid>(RootTokens.Take(2).Concat(Constants("BardMasterpieces01435","OwnTokens")).Select(Id));
        var strings=(Dictionary<string,string>)AccessTools.Field(Module("PsychicAssets"),"Strings").GetValue(null);var oldStrings=strings.ToArray();var snapshot=new ParentSnapshot(parent);
        var diagnostics=new[]{Module("WizardDiscoveries01435"),Module("BardMasterpieces01435")}.SelectMany(AccessTools.GetDeclaredFields).Where(f=>f.IsStatic&&f.FieldType==typeof(string)&&f.Name.Contains("Diagnostic")).Select(f=>Tuple.Create(f,f.GetValue(null))).ToArray();
        targets=new HashSet<string>(ModuleNames());JObject[] observed=null;var h=new Harmony("CotW01436.ActualScopedRegistrationCaller");
        h.Patch(AccessTools.Method(Module("BlueprintRegistration"),"RegisterModule"),prefix:new HarmonyMethod(typeof(Repair01436CallerTests),nameof(CallerScope)));
        h.Patch(AccessTools.Method(Module("ExtraInvestigatorTalent"),"Register"),prefix:new HarmonyMethod(typeof(Repair01436CallerTests),nameof(DirectInvestigatorBoundary)));
        h.Patch(AccessTools.Method(Module("T09CombatFeats"),"Append"),postfix:new HarmonyMethod(typeof(Repair01436CallerTests),nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),prefix:new HarmonyMethod(typeof(Repair01436CallerTests),nameof(CacheFault)));
        try{
            ParentSpellListVerifierProbe(parent);
            foreach(string token in RootTokens)CoreFailure(parent,token);
            CoreFailure(parent,RootTokens[2],true);
            observed=Success(parent);
            ForeignLegacyContainer(parent);
            foreach(int shape in Enumerable.Range(1,5))ProviderShape(parent,true,shape);
            foreach(int shape in Enumerable.Range(1,4))ProviderShape(parent,false,shape);
            OptionalAppendFault(parent,true);OptionalAppendFault(parent,false);
        }finally{
            cacheFaultToken=null;optionalFaultPool=null;h.UnpatchAll(h.Id);strings.Clear();foreach(var p in oldStrings)strings.Add(p.Key,p.Value);foreach(var d in diagnostics)d.Item1.SetValue(null,d.Item2);
            AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,parent);snapshot.Verify();
        }
        Check(strings.Count==oldStrings.Length&&oldStrings.All(p=>strings.TryGetValue(p.Key,out var v)&&v==p.Value),"all prior localization keys and values are restored");
        Check(observed.Length==OwnIds.Count&&observed.Select(x=>(string)x["id"]).Distinct().Count()==observed.Length,"actual private graph inventory is unique and independent from repair item count");
        PsychicExpansionTests.Inventory["repair01436CallerObservations"]=new JArray(observations);
        PsychicExpansionTests.Inventory["repair01436CallerContracts"]=new JObject{
            ["passed"]=true,["assertions"]=checks,["selectionPoolAssertions"]=poolAssertions,["freshSpellListAssertions"]=freshSpellListAssertions,["cacheDependentAssertions"]=poolAssertions+freshSpellListAssertions,["cacheDependentSegments"]=new JObject{["selectionPoolAssertions"]=poolAssertions,["freshSpellListAssertions"]=freshSpellListAssertions},["additionalItemCount"]=0,["newBlueprints"]=new JArray(),["repairItemIds"]=new JArray("COTW-0364","COTW-0369","COTW-0378","COTW-0379","COTW-0380"),["observedOwnedBlueprints"]=new JArray(observed),
            ["actualRegisterAllAndOptionalDispatchers"]=true,["fiveIndependentFailuresRollBackAndLaterActionsContinue"]=true,["realPostAppendProviderFailuresIsolatedAndRetried"]=true,["realModuleCatalogueAndHonestStates"]=true,["legacyDirectChoicesRemovedOnlyByOwnedIdentity"]=true,["onlyProviderSubmenuChoices"]=true,["legacyPrivateContainerHiddenAndNotSelectable"]=true,["foreignSameIdentityContainerRejectedAndPreserved"]=true,["sourceFaithfulAbsentMalformedAndLateProviders"]=true,["allOwnedIdentitiesRetainedAcrossRetry"]=true,["allUnrelatedPoolReferencesAndOrderPreserved"]=true,["parentCacheComponentsPoolsLazyPointersAndStringsRestored"]=true,["parentSpellListRowsReferencesAndProxiesRestored"]=true,["nativeParentSpellListVerifierRejectsReplacement"]=true,["freshFinalSpellReferenceGraphsUncached"]=true,["nonTargetActionsScopedOutWithoutSuccessReports"]=true,
            ["boundary"]="Actual production RegisterAll/RegisterOptional dispatch with five real module actions on deep-copied native caches. TTT/PrestigePlus provider metadata is a source-faithful controlled fixture, not execution of complete provider assemblies or rendered startup. Actual native AddCachedBlueprint and T09 Append-after-mutation faults exercise rollback, status honesty, later actions and retry. Acquiring known spells and effect rules are exercised separately by their native rule suites. Parent cache identity, components, selection arrays, lazy reference pointers and strings are restored. No game process, UI rendering, scene geometry or complete save scene is inferred."};
        Console.WriteLine("PASS: "+checks+" actual036 repair caller contracts; "+observed.Length+" observed private assets.");return checks;
    }
}
