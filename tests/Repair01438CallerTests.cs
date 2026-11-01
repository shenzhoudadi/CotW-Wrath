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
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

// Production dispatch, status and publication checks on independent native caches.
// Provider metadata comes from the already audited036 fixture, not a game scene.
internal static class Repair01438CallerTests
{
    static int checks, dispatches, faults;
    static string faultToken;
    static readonly List<string> skipped = new List<string>();
    static readonly List<JObject> observations = new List<JObject>();
    static Type Module(string name) => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    static object Get(object o, string field) => AccessTools.Field(o.GetType(), field).GetValue(o);
    static void Set(object o, string field, object v) => AccessTools.Field(o.GetType(), field).SetValue(o, v);
    static string Constant(string type, string field) => (string)AccessTools.Field(Module(type), field).GetValue(null);
    static string[] Tokens => ((string[])AccessTools.Field(Module("TimeStutter01437"), "OwnTokens").GetValue(null)).Concat((string[])AccessTools.Field(Module("TimeStop01438"), "OwnTokens").GetValue(null)).ToArray();
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Module("PsychicAssets"), "Id").Invoke(null, new object[] { token });
    static string[] Names => new[] { Constant("WizardDiscoveries01435", "ForestModule"), Constant("WizardDiscoveries01435", "ResilientModule"), Constant("TimeStutter01437", "ModuleName"), Constant("TimeStop01438", "ModuleName") };
    static SimpleBlueprint[] Loaded(BlueprintsCache c) => ((IDictionary)Get(c, "m_LoadedBlueprints")).Values.Cast<object>().Select(e => (SimpleBlueprint)Get(e, "Blueprint")).Where(b => b != null).ToArray();
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string f) => Get(p, f) as BlueprintFeatureReference[] ?? Array.Empty<BlueprintFeatureReference>();
    static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception("Repair038 caller: " + message); }
    static HashSet<string> enabled;
    static bool Scope(string name) { if (enabled.Contains(name)) return true; skipped.Add(name); return false; }
    static bool Investigator(ref bool __result) { __result = false; skipped.Add(Constant("ExtraInvestigatorTalent", "ModuleName")); return false; }
    static void Fault(BlueprintGuid __0) { if (faultToken != null && __0 == Id(faultToken)) { var failed = faultToken; faultToken = null; faults++; throw new InvalidOperationException("038 real post-write cache fault " + failed); } }
    static T Native<T>(string id) where T : SimpleBlueprint => (T)AccessTools.Method(typeof(PsychicExpansionTests), "NativeCase").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { id });
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        // Reuse036's uncached deep-copy and spell-row isolation utility. Its
        // owned set is temporarily extended; its tests and production are unchanged.
        var ownField = AccessTools.Field(typeof(Repair01436CallerTests), "OwnIds");
        var original = ownField.GetValue(null);
        var oldTokens = (string[])AccessTools.Field(Module("BardMasterpieces01435"), "OwnTokens").GetValue(null);
        ownField.SetValue(null, new HashSet<BlueprintGuid>(oldTokens.Concat(Tokens).Concat((string[])AccessTools.Field(Module("Invoker01437"), "OwnTokens").GetValue(null)).Concat(new[] { "ForestsBlessingDiscovery", "ResilientIllusionsDiscovery" }).Select(Id)));
        BlueprintsCache c;
        try { c = (BlueprintsCache)AccessTools.Method(typeof(Repair01436CallerTests), "Fresh").Invoke(null, new object[] { parent }); }
        finally { ownField.SetValue(null, original); }
        // The exact original class/progression must not inherit an earlier
        // test's already-linked archetype or UI group, even in focused runs.
        foreach (SimpleBlueprint b in new SimpleBlueprint[] {
            Native<BlueprintCharacterClass>(Constant("Invoker01437", "WitchClassId")), Native<BlueprintProgression>(Constant("Invoker01437", "WitchProgressionId")),
            Native<BlueprintAbility>(Constant("TimeStutter01437", "NativeAbility")), Native<BlueprintBuff>(Constant("TimeStutter01437", "NativeBuff")),
            Native<BlueprintAbilityAreaEffect>(Constant("TimeStutter01437", "NativeArea")), Native<BlueprintBuff>(Constant("TimeStutter01437", "NativeEffect"))
        }) c.AddCachedBlueprint(b.AssetGuid, b);
        TimeStutter01437Tests.PrepareNative(c);
        DiscoveryAccess01438Tests.PrepareNative(c);
        Check(Tokens.All(t => c.Load(Id(t)) == null), "fresh cache excludes all newly owned assets");
        return c;
    }
    static BlueprintFeatureSelection Provider(BlueprintsCache c) {
        var pool=(BlueprintFeatureSelection)AccessTools.Method(typeof(DiscoveryAccess01438Tests),"Ttt").Invoke(null,new object[]{c,true});
        AccessTools.Method(typeof(DiscoveryAccess01438Tests),"Ecm").Invoke(null,new object[]{c,pool}); return pool;
    }
    static Dictionary<string, string> Initial() => (Dictionary<string, string>)AccessTools.Method(Module("ModuleCatalog"), "InitialStates").Invoke(null, null);
    static void Catalog(Dictionary<string, string> states)
    {
        var defs = ((IEnumerable)AccessTools.Field(Module("ModuleCatalog"), "Definitions").GetValue(null)).Cast<object>().ToArray();
        var rows = ((IEnumerable)AccessTools.Method(Module("ModuleCatalog"), "Snapshot").Invoke(null, new object[] { states })).Cast<object>().ToArray();
        Check(defs.Length == rows.Length && defs.Select(d => (string)Get(d, "Name")).Distinct().Count() == defs.Length, "real definitions uniquely determine menu names and row count");
        Check(rows.Select((r, i) => ReferenceEquals(Get(r, "Definition"), defs[i]) && (string)Get(r, "State") == states[(string)Get(defs[i], "Name")]).All(v => v), "real menu snapshot retains exact actual dispatcher states");
    }
    static List<Tuple<string, bool, Exception>> Dispatch(BlueprintsCache c, string method, Dictionary<string, string> states)
    {
        dispatches++;
        var results = new List<Tuple<string, bool, Exception>>();
        Action<string, bool, Exception> report = (name, ok, error) => { states[name] = ok ? "Registered" : "Failed"; results.Add(Tuple.Create(name, ok, error)); };
        AccessTools.Method(Module("BlueprintRegistration"), method).Invoke(null, new object[] { c, report });
        Check(results.All(r => enabled.Contains(r.Item1)) && skipped.All(n => results.All(r => r.Item1 != n)), "unrelated actions are scoped out without fabricated Registered reports");
        return results;
    }
    static Dictionary<string, BlueprintFeatureReference[]> Pools(BlueprintsCache c) => Loaded(c).OfType<BlueprintFeatureSelection>().SelectMany(p => new[] { "m_Features", "m_AllFeatures" }.Select(f => new { key = p.AssetGuid + "/" + f, refs = Refs(p, f).ToArray() })).ToDictionary(x => x.key, x => x.refs);
    static void PreservePools(BlueprintsCache c, Dictionary<string, BlueprintFeatureReference[]> before)
    {
        var allowed = new HashSet<BlueprintGuid>(new[] { "ForestsBlessingDiscovery", "ResilientIllusionsDiscovery", "TimeStutterDiscovery" }.Select(Id));
        var provider = BlueprintGuid.Parse(Constant("WizardDiscoveries01435", "DiscoveryPoolId"));
        Check(before.All(kv => {
            var parts = kv.Key.Split('/'); var pool = (BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(parts[0]));
            var now = Refs(pool, parts[1]); var kept = now.Where(r => r == null || (!allowed.Contains(r.Guid) && r.Guid != provider)).ToArray();
            var expected = kv.Value.Where(r => r == null || (!allowed.Contains(r.Guid) && r.Guid != provider)).ToArray();
            return kept.Length == expected.Length && kept.Zip(expected, ReferenceEquals).All(v => v) && now.Where(r => r != null && allowed.Contains(r.Guid)).All(r => pool.AssetGuid == provider);
        }), "every unrelated pool reference/object/order survives, with discovery roots only in the actual provider submenu");
        Check(Loaded(c).OfType<BlueprintFeatureSelection>().All(p => new[] { "m_Features", "m_AllFeatures" }.All(f => Refs(p, f).Where(r => r != null && allowed.Contains(r.Guid)).GroupBy(r => r.Guid).All(g => g.Count() == 1))), "no discovery duplicate after repeated real calls");
    }

    static JObject[] Success(BlueprintsCache parent)
    {
        enabled=new HashSet<string>(Names);var c=Fresh(parent);var before=Pools(c);var states=Initial();skipped.Clear();
        var result=Dispatch(c,"RegisterAll",states);
        Check(Names.All(n=>result.Any(r=>r.Item1==n)&&result.Where(r=>r.Item1==n).All(r=>r.Item2)),"four actual module actions succeed with absent discovery provider");
        var owned=Tokens.Select(t=>c.Load(Id(t))).ToArray();Check(owned.All(b=>b!=null)&&owned.Select(b=>b.AssetGuid).Distinct().Count()==owned.Length,"all ten private Time Stutter/Time Stop objects publish once");
        PreservePools(c,before);Catalog(states);
        Provider(c);
        for(int repeat=0;repeat<3;repeat++){
            result=Dispatch(c,"RegisterOptional",states);
            Check(Names.All(n=>result.Any(r=>r.Item1==n)&&result.Where(r=>r.Item1==n).All(r=>r.Item2)),"late ECM Any provider and repeated optional actual calls all remain successful");
            var pool=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(Constant("WizardDiscoveries01435","DiscoveryPoolId")));
            Check(new[]{"m_Features","m_AllFeatures"}.All(f=>new[]{"ForestsBlessingDiscovery","ResilientIllusionsDiscovery","TimeStutterDiscovery"}.All(t=>Refs(pool,f).Count(r=>r?.Guid==Id(t))==1)),"all three discoveries are unique real provider children");
            Check(owned.All(b=>ReferenceEquals(c.Load(b.AssetGuid),b)),"optional retries preserve every published object identity");
            PreservePools(c,before);Catalog(states);
        }
        Check(states.Where(k=>!Names.Contains(k.Key)).All(k=>k.Value==Initial()[k.Key]),"skipped modules including Invoker never receive fabricated Registered states");
        observations.Add(new JObject { ["case"]="fourActualModulesLateTttEcmAnyAndThreeOptionalPasses",["actualStates"]=JObject.FromObject(states) });
        return owned.OrderBy(b=>b.AssetGuid.ToString()).Select(b=>new JObject { ["id"]=b.AssetGuid.ToString(),["name"]=b.name,["type"]=b.GetType().FullName }).ToArray();
    }
    static void InsertionFailure(BlueprintsCache parent,bool spell)
    {
        enabled=new HashSet<string>(Names);var c=Fresh(parent);Provider(c);var before=Pools(c);var states=Initial();skipped.Clear();
        string module=spell?"TimeStop01438":"TimeStutter01437";string failed=Constant(module,"ModuleName");var tokens=(string[])AccessTools.Field(Module(module),"OwnTokens").GetValue(null);
        faultToken=Constant(module,"AreaToken");List<Tuple<string,bool,Exception>> results;
        try { results=Dispatch(c,"RegisterAll",states); }finally{faultToken=null;}
        Check(results.Any(r=>r.Item1==failed&&!r.Item2&&r.Item3!=null&&r.Item3.ToString().Contains("038 real post-write cache fault")),"actual post-write insertion exception is reported for its owning graph");
        Check(states[failed]=="Failed"&&tokens.All(t=>c.Load(Id(t))==null),"every object of the failed graph is rolled back after cache already wrote the faulted area");
        Check(Names.Where(n=>n!=failed).All(n=>states[n]=="Registered"),"later real actions continue after a failed graph");
        var optional=Dispatch(c,"RegisterOptional",states);Check(optional.All(r=>r.Item1!=failed)&&states[failed]=="Failed","missing-root optional gate cannot conceal failed core");
        var retry=Dispatch(c,"RegisterAll",states);Check(Names.All(n=>states[n]=="Registered")&&tokens.All(t=>c.Load(Id(t))!=null),"subsequent actual core retry reconstructs complete graph and recovers status");
        PreservePools(c,before);Catalog(states);observations.Add(new JObject { ["case"]="postWriteAreaFaultAndActualCoreRetry:"+module,["actualStates"]=JObject.FromObject(states) });
    }
    static void ForeignRoot(BlueprintsCache parent,bool spell)
    {
        enabled=new HashSet<string>(Names);var c=Fresh(parent);Provider(c);var states=Initial();skipped.Clear();
        string module=spell?"TimeStop01438":"TimeStutter01437",token=Constant(module,"RootToken"),failed=Constant(module,"ModuleName");
        BlueprintScriptableObject foreign=spell?(BlueprintScriptableObject)new BlueprintAbility():new BlueprintFeature();foreign.AssetGuid=Id(token);foreign.name="CotWWrathPsychic"+token;foreign.ComponentsArray=Array.Empty<BlueprintComponent>();
        var originalComponents=foreign.ComponentsArray;c.AddCachedBlueprint(foreign.AssetGuid,foreign);var before=Pools(c);
        var results=Dispatch(c,"RegisterAll",states);Check(results.Any(r=>r.Item1==failed&&!r.Item2&&r.Item3 is InvalidOperationException)&&states[failed]=="Failed","foreign same-GUID/type/name core failure stays Failed through automatic optional dispatch");
        var retry=Dispatch(c,"RegisterOptional",states);Check(retry.Any(r=>r.Item1==failed&&!r.Item2&&r.Item3 is InvalidOperationException)&&states[failed]=="Failed","explicit optional cannot falsely register foreign occupied root");
        Check(ReferenceEquals(c.Load(foreign.AssetGuid),foreign)&&ReferenceEquals(foreign.ComponentsArray,originalComponents),"foreign object and component array remain exact");
        var tokens=(string[])AccessTools.Field(Module(module),"OwnTokens").GetValue(null);Check(tokens.Where(t=>t!=token).All(t=>c.Load(Id(t))==null),"foreign root leaves no own partial graph");
        Check(Names.Where(n=>n!=failed).All(n=>states[n]=="Registered"),"foreign root does not stop the other real modules");PreservePools(c,before);Catalog(states);
        observations.Add(new JObject { ["case"]="foreignRootFailedAcrossCoreAndOptional:"+module,["actualStates"]=JObject.FromObject(states) });
    }
    static void SpellWithoutDiscoveries(BlueprintsCache parent)
    {
        enabled=new HashSet<string>(new[]{Constant("TimeStop01438","ModuleName")});var c=Fresh(parent);var states=Initial();skipped.Clear();
        var listId=BlueprintGuid.Parse(Constant("PsychicClass","ListId"));var psychic=(Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList)c.Load(listId);Check(psychic!=null&&psychic.SpellsByLevel.Length>9,"fresh optional Psychic list is a real independently copied existing table");c.RemoveCachedBlueprint(listId);
        var before=Pools(c);Dispatch(c,"RegisterAll",states);
        Check(states[Names[3]]=="Registered"&&Names.Take(3).All(n=>states[n]==Initial()[n]),"independent player spell registers with all three discovery modules genuinely scoped off");
        Check(new[]{"ForestsBlessingDiscovery","ResilientIllusionsDiscovery","TimeStutterDiscovery"}.All(t=>c.Load(Id(t))==null),"spell publication does not create or require discoveries");
        var root=(BlueprintAbility)c.Load(Id(Constant("TimeStop01438","RootToken")));Check(root!=null&&root.GetComponents<Kingmaker.Blueprints.Classes.Spells.SpellListComponent>().Count()==1,"Psychic absent publishes only native wizard reciprocal list metadata");
        c.AddCachedBlueprint(listId,psychic);Dispatch(c,"RegisterOptional",states);Dispatch(c,"RegisterOptional",states);
        var row=psychic.SpellsByLevel[9];var spells=(List<BlueprintAbilityReference>)Get(row,"m_Spells");Check(spells.Count(r=>r?.Guid==root.AssetGuid)==1&&root.GetComponents<Kingmaker.Blueprints.Classes.Spells.SpellListComponent>().Count()==2,"late actual Psychic list gets level-9 spell exactly once with reciprocal metadata");
        Check(ReferenceEquals(c.Load(root.AssetGuid),root)&&((string[])AccessTools.Field(Module("TimeStop01438"),"OwnTokens").GetValue(null)).All(t=>c.Load(Id(t))!=null),"late Psychic registration retains complete independent published graph identity");
        PreservePools(c,before);Catalog(states);Check(states.Where(k=>!enabled.Contains(k.Key)).All(k=>k.Value==Initial()[k.Key]),"only enabled spell reports a state in independent mode");
        observations.Add(new JObject { ["case"]="spellIndependentOfDiscoveriesAndLatePsychic",["actualStates"]=JObject.FromObject(states) });
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks = dispatches = faults = 0; observations.Clear(); skipped.Clear(); enabled = new HashSet<string>(Names);
        TimeStutter01437Tests.PrepareNative(parent);
        var source = (BlueprintAbility)parent.Load(BlueprintGuid.Parse(Constant("TimeStutter01437", "NativeAbility")));
        var elementList = Get(source, "m_AllElements"); var elements = source.ElementsArray.ToArray(); var owners = elements.Select(e => e.Owner).ToArray();
        string[] cacheNames = { "m_CachedRestrictions", "m_CachedTargetRestrictions", "m_CachedCasterRestrictions" };
        var sourceCaches = cacheNames.Select(n => Get(source, n)).ToArray();
        Func<bool> exactSource = () => ReferenceEquals(Get(source, "m_AllElements"), elementList) && source.ElementsArray.SequenceEqual(elements) && elements.Select(e => e.Owner).Zip(owners, ReferenceEquals).All(v => v) && cacheNames.Select(n => Get(source, n)).Zip(sourceCaches, ReferenceEquals).All(v => v);
        Check(elements.Length > 0 && elements.All(e => ReferenceEquals(e.Owner, source)), "actual native parent has warmed elements with exact owner references before isolation");
        var originalOwner = elements[0].Owner;
        try { elements[0].Owner = new BlueprintAbility { AssetGuid = source.AssetGuid, name = source.name }; Check(!exactSource(), "parent element verifier rejects a different owner object even with the identical native GUID"); }
        finally { elements[0].Owner = originalOwner; }
        Check(exactSource(), "negative ownership probe restores exact parent state");
        var strings = (Dictionary<string, string>)AccessTools.Field(Module("PsychicAssets"), "Strings").GetValue(null); var oldStrings = strings.ToArray();
        var oldSnapshotType = typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot", BindingFlags.NonPublic);
        var snapshot = Activator.CreateInstance(oldSnapshotType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { parent }, null);
        var diagnostics = new[] { Module("WizardDiscoveries01435"), Module("BardMasterpieces01435") }.SelectMany(AccessTools.GetDeclaredFields).Where(f => f.IsStatic && f.FieldType == typeof(string) && f.Name.Contains("Diagnostic")).Select(f => Tuple.Create(f, f.GetValue(null))).ToArray();
        var h = new Harmony("CotW038.ActualRegistrationCaller"); JObject[] inventory;
        h.Patch(AccessTools.Method(Module("BlueprintRegistration"), "RegisterModule"), prefix: new HarmonyMethod(typeof(Repair01438CallerTests), nameof(Scope)));
        h.Patch(AccessTools.Method(Module("ExtraInvestigatorTalent"), "Register"), prefix: new HarmonyMethod(typeof(Repair01438CallerTests), nameof(Investigator)));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"), postfix: new HarmonyMethod(typeof(Repair01438CallerTests), nameof(Fault)));
        try { inventory = Success(parent); InsertionFailure(parent, false); InsertionFailure(parent, true); ForeignRoot(parent, false); ForeignRoot(parent, true); SpellWithoutDiscoveries(parent); }
        finally {
            faultToken = null; h.UnpatchAll(h.Id); strings.Clear(); foreach (var kv in oldStrings) strings.Add(kv.Key, kv.Value);
            foreach (var d in diagnostics) d.Item1.SetValue(null, d.Item2);
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, parent);
            AccessTools.Method(oldSnapshotType, "Verify").Invoke(snapshot, null);
            Check(exactSource(), "all native parent element-list/item/owner and prewarmed restriction-cache identities survive every isolated caller case");
        }
        Check(faults == 2 && dispatches == 17, "two actual post-write insertion faults and seventeen production dispatch calls were observed");
        PsychicExpansionTests.Inventory["repair01438CallerObservations"] = new JArray(observations);
        PsychicExpansionTests.Inventory["repair01438CallerContracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 0, ["newBlueprints"] = new JArray(), ["observedOwnedBlueprints"] = new JArray(inventory),
            ["actualDispatcherCalls"] = dispatches, ["actualPostWriteInsertionFaults"] = faults,
            ["actualRegisterAllAndOptionalDispatchers"] = true, ["nativeCacheFailuresRollbackAndLaterActionsContinue"] = true, ["lateProviderAndRepeatIdentityPreserved"] = true,
            ["foreignOccupiedRootsFailWithoutFalseRegistered"] = true,
            ["nativeParentElementOwnersAndWarmCachesPreserved"] = true,
            ["realModuleCatalogueAndHonestStates"] = true, ["allUnrelatedPoolReferencesAndOrderPreserved"] = true, ["parentCacheAndStringsRestored"] = true,
            ["boundary"] = "Actual RegisterAll/RegisterOptional calls on independent copies of native cache data, with three discovery actions and an independent player spell; unrelated actions deliberately skipped without success reports. Source-faithful controlled TTT and ECM Any provider metadata is not a complete provider process. Native post-write cache insertion faults, complete graph rollback, subsequent core retries and menu snapshots are observed. No game startup, rendered UI, animation or save scene is inferred." };
        Console.WriteLine("PASS: " + checks + " actual038 caller contracts."); return checks;
    }
}
