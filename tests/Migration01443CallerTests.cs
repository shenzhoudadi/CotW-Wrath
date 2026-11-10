using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

// Dispatcher contracts only. Each archetype's native gameplay contracts live in
// its dedicated suite. Non-target module actions are skipped without a report.
internal static class Migration01443CallerTests
{
    static readonly string[] Types = { "RogueSwashbuckler01443", "Sunsinger01443", "UrbanSkald01443" };
    static readonly Assembly Production = typeof(HowlShakenTarget).Assembly;
    static readonly List<string> executed = new List<string>(), skipped = new List<string>();
    static HashSet<string> targets;
    static HashSet<BlueprintGuid> ownIds;
    static int checks, dispatches, hydrated, enables;
    static Type Module(string name) => Production.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    static object Get(object value, string field) => AccessTools.Field(value.GetType(), field).GetValue(value);
    static void Set(object value, string field, object data) => AccessTools.Field(value.GetType(), field).SetValue(value, data);
    static object Call(Type type, string method, params object[] values) => AccessTools.Method(type, method).Invoke(null, values);
    static string Constant(string type, string field) => (string)AccessTools.Field(Module(type), field).GetValue(null);
    static string[] Constants(string type, string field) => (string[])AccessTools.Field(Module(type), field).GetValue(null);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Module("PsychicAssets"), "Id", token);
    // Migration01426.Definition also has an (object, IEqualityComparer) overload.
    static string Definition(object value) => (string)AccessTools.Method(Module("Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new object[] { value });
    static SimpleBlueprint[] Loaded(BlueprintsCache cache) => ((IDictionary)Get(cache, "m_LoadedBlueprints")).Values.Cast<object>().Select(e => (SimpleBlueprint)Get(e, "Blueprint")).Where(b => b != null).ToArray();
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection pool, string field) => (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
    static void Check(bool value, string why) { if (!value) throw new Exception("Migration01443Caller: " + why); checks++; }
    sealed class Identity : IEqualityComparer<object>
    {
        internal static readonly Identity Instance = new Identity();
        public new bool Equals(object a, object b) => ReferenceEquals(a, b);
        public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
    }
    static IEnumerable<Element> Elements(object value, HashSet<object> seen = null)
    {
        if (value == null || value is string || value is SimpleBlueprint || value is BlueprintReferenceBase || value is UnityEngine.Object || value is Type || value is Delegate || value.GetType().IsPrimitive || value.GetType().IsEnum) yield break;
        seen = seen ?? new HashSet<object>(Identity.Instance); if (!seen.Add(value)) yield break;
        if (value is Element element) yield return element;
        if (value is IEnumerable list) { foreach (var item in list) foreach (var e in Elements(item, seen)) yield return e; yield break; }
        for (var type = value.GetType(); type != null && type != typeof(object); type = type.BaseType)
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (!field.IsStatic) foreach (var e in Elements(field.GetValue(value), seen)) yield return e;
    }
    static bool Scope(string name) { if (targets.Contains(name)) { executed.Add(name); return true; } skipped.Add(name); return false; }
    static bool DirectInvestigatorBoundary(ref bool __result) { skipped.Add(Constant("ExtraInvestigatorTalent", "ModuleName")); __result = false; return false; }
    static Dictionary<string, string> States() => (Dictionary<string, string>)Call(Module("ModuleCatalog"), "InitialStates");
    static void Catalogue(Dictionary<string, string> states)
    {
        var definitions = ((IEnumerable)AccessTools.Field(Module("ModuleCatalog"), "Definitions").GetValue(null)).Cast<object>().ToArray();
        var names = definitions.Select(d => (string)Get(d, "Name")).ToArray();
        var snapshot = ((IEnumerable)Call(Module("ModuleCatalog"), "Snapshot", states)).Cast<object>().ToArray();
        Check(names.Length > 0 && names.Distinct().Count() == names.Length && names.Length == states.Count && snapshot.Length == definitions.Length, "real caller dictionary, catalogue names and menu snapshot share one unique definition collection");
        Check(targets.All(names.Contains), "all three actual dispatcher names are defined in the catalogue");
        Check(snapshot.Select((status, i) => ReferenceEquals(Get(status, "Definition"), definitions[i]) && (string)Get(status, "State") == states[names[i]]).All(x => x), "every menu status comes from its actual caller state and exact definition object");
    }
    static List<Tuple<string, bool, Exception>> Dispatch(BlueprintsCache cache, string method, Dictionary<string, string> states)
    {
        var reports = new List<Tuple<string, bool, Exception>>();
        Action<string, bool, Exception> report = (name, passed, error) => { states[name] = passed ? "Registered" : "Failed"; reports.Add(Tuple.Create(name, passed, error)); };
        Call(Module("BlueprintRegistration"), method, cache, report); dispatches++;
        Check(reports.All(r => targets.Contains(r.Item1)) && reports.All(r => r.Item2 && r.Item3 == null), "actual " + method + " reports only three successful scoped actions: " + string.Join("; ", reports.Where(r => !r.Item2).Select(r => r.Item1 + ": " + r.Item3)));
        Check(targets.All(t => reports.Any(r => r.Item1 == t)), "actual " + method + " reaches each new archetype through its real dispatcher call");
        Check(states.Where(p => !targets.Contains(p.Key)).All(p => p.Value == States()[p.Key]), "skipped modules keep their original catalogue states instead of fabricated success");
        Catalogue(states); return reports;
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var field = AccessTools.Field(typeof(Repair01436CallerTests), "OwnIds"); var previous = field.GetValue(null);
        var omit = new HashSet<BlueprintGuid>(ownIds);
        foreach (var token in Constants("BardMasterpieces01435", "OwnTokens").Concat(Constants("TimeStutter01437", "OwnTokens")).Concat(new[] { "ForestsBlessingDiscovery", "ResilientIllusionsDiscovery" })) omit.Add(Id(token));
        field.SetValue(null, omit);
        BlueprintsCache cache;
        try { cache = (BlueprintsCache)Call(typeof(Repair01436CallerTests), "Fresh", parent); }
        finally { field.SetValue(null, previous); }
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        // Recreate each actual native donor. The shared copier deliberately does
        // not treat Element.Owner as authored reference data, so warmed lists
        // must not be inherited from the caller's parent cache.
        var nativeIds = Types.SelectMany(t => Constants(t, "NativeIds")).Distinct().ToArray();
        foreach (string id in nativeIds) {
            var before = cache.Load(BlueprintGuid.Parse(id));
            var rows = PsychicExpansionTests.NativeRows.Where(r => (string)r["blueprint"]["AssetId"] == id).ToArray();
            Check(before is BlueprintScriptableObject && rows.Length == 1, "one genuine native archived dependency exists: " + id);
            var bp = (BlueprintScriptableObject)AccessTools.Method(typeof(PsychicExpansionTests), "NativeCase").MakeGenericMethod(before.GetType()).Invoke(null, new object[] { id });
            cache.AddCachedBlueprint(bp.AssetGuid, bp);
        }
        foreach (string id in nativeIds) {
            var bp = (BlueprintScriptableObject)cache.Load(BlueprintGuid.Parse(id));
            var row = (JObject)PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
            foreach (var property in row.Properties()) {
                if (property.Name == "Components" || property.Name == "m_Icon" || property.Name.StartsWith("$", StringComparison.Ordinal)) continue;
                var nativeField = AccessTools.Field(bp.GetType(), property.Name);
                if (nativeField != null && !nativeField.IsStatic && !nativeField.IsInitOnly) nativeField.SetValue(bp, PsychicExpansionTests.Read(property.Value, nativeField.FieldType));
            }
            Set(bp, "m_AllElements", new List<Element>());
            foreach (var element in Elements(bp.ComponentsArray)) bp.AddToElementsList(element);
            bp.OnEnable(); hydrated++; enables++;
            Check(!ReferenceEquals(parent.Load(bp.AssetGuid), bp) && bp.ComponentsArray.All(c => c != null && ReferenceEquals(c.OwnerBlueprint, bp)) && bp.ElementsArray.All(e => ReferenceEquals(e.Owner, bp)), "native dependency is separately hydrated, element-registered and enabled: " + id);
        }
        Check(ownIds.All(id => cache.Load(id) == null), "fresh native caller graph omits every new private node before real registration");
        return cache;
    }
    internal static int Run(BlueprintsCache parent, string evidence)
    {
        checks = dispatches = hydrated = enables = 0; executed.Clear(); skipped.Clear();
        targets = new HashSet<string>(Types.Select(t => Constant(t, "ModuleName")));
        ownIds = new HashSet<BlueprintGuid>(Types.SelectMany(t => Constants(t, "OwnTokens")).Select(Id));
        Check(targets.Count == 3 && ownIds.Count == Types.Sum(t => Constants(t, "OwnTokens").Length), "three independent modules own disjoint complete resource sets");
        Check(!string.IsNullOrEmpty(evidence) && File.Exists(evidence), "real native extraction evidence path is present");
        var strings = (Dictionary<string, string>)AccessTools.Field(Module("PsychicAssets"), "Strings").GetValue(null); var oldStrings = strings.ToArray();
        var originalCache = ResourcesLibrary.BlueprintsCache;
        var snapshotType = typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot", BindingFlags.NonPublic);
        var parentSnapshot = Activator.CreateInstance(snapshotType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { parent }, null);
        var harmony = new Harmony("CotW01443.ActualScopedCaller");
        try {
            harmony.Patch(AccessTools.Method(Module("BlueprintRegistration"), "RegisterModule"), prefix: new HarmonyMethod(typeof(Migration01443CallerTests), nameof(Scope)));ScopedAvailable01463Tests.Bind(harmony,Module("BlueprintRegistration"),"ScopeMigration01443CallerTests");
            harmony.Patch(AccessTools.Method(Module("ExtraInvestigatorTalent"), "Register"), prefix: new HarmonyMethod(typeof(Migration01443CallerTests), nameof(DirectInvestigatorBoundary)));
            var cache = Fresh(parent); var before = Loaded(cache);
            string refusalId = Constant("UrbanSkald01443", "RefusalId");
            var untouched = before.OfType<BlueprintScriptableObject>().Where(b => b.AssetGuid != BlueprintGuid.Parse(refusalId)).ToDictionary(b => b, b => new { Components = b.ComponentsArray, Items = b.ComponentsArray.ToArray(), Definition = Definition(b.ComponentsArray), Elements = b.ElementsArray.ToArray(), Owners = b.ElementsArray.Select(e => e.Owner).ToArray(), ElementList = Get(b, "m_AllElements") });
            var pools = before.OfType<BlueprintFeatureSelection>().SelectMany(p => new[] { "m_Features", "m_AllFeatures" }.Select(field => new { Pool = p, Field = field, Refs = Refs(p, field).ToArray() })).ToArray();
            var classes = new[] { Constant("RogueSwashbuckler01443", "RogueId"), Constant("Sunsinger01443", "SkaldId") }.Select(id => (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(id))).ToArray();
            var oldArchetypes = classes.Select(c => ((BlueprintArchetypeReference[])Get(c, "m_Archetypes")).ToArray()).ToArray();
            var progressions = classes.Select(c => c.Progression).ToArray();
            var oldGroups = progressions.Select(p => (p.UIGroups ?? Array.Empty<UIGroup>()).ToArray()).ToArray();
            var oldDet = progressions.Select(p => ((BlueprintFeatureBaseReference[])Get(p, "m_UIDeterminatorsGroup") ?? Array.Empty<BlueprintFeatureBaseReference>()).ToArray()).ToArray();
            var refusal = (BlueprintScriptableObject)cache.Load(BlueprintGuid.Parse(refusalId)); var refusalHandler = refusal.GetComponents<AddFactContextActions>().Single(); var oldRefusalActions = refusalHandler.Deactivated.Actions.ToArray();
            var states = States(); Catalogue(states); Dispatch(cache, "RegisterAll", states);
            var assets = ownIds.ToDictionary(id => id, id => cache.Load(id));
            Check(assets.All(p => p.Value != null) && Loaded(cache).Count(b => ownIds.Contains(b.AssetGuid)) == ownIds.Count, "real caller publishes exactly the complete private graph, with no duplicate GUIDs");
            var authored = Types.ToDictionary(t => t, t => Constants(t, "OwnTokens").ToDictionary(token => token, token => (string)Call(Module(t), "Definition", cache.Load(Id(token)))));
            foreach (string method in new[] { "RegisterOptional", "RegisterAll", "RegisterOptional" }) {
                Dispatch(cache, method, states);
                Check(assets.All(p => ReferenceEquals(cache.Load(p.Key), p.Value)), "real repeated " + method + " retains every private identity");
                Check(authored.All(t => t.Value.All(p => (string)Call(Module(t.Key), "Definition", cache.Load(Id(p.Key))) == p.Value)), "real repeated " + method + " retains every authored private definition");
            }
            foreach (var pool in pools) {
                var actual = Refs(pool.Pool, pool.Field);
                Check(actual.Length >= pool.Refs.Length && actual.Take(pool.Refs.Length).Zip(pool.Refs, ReferenceEquals).All(x => x), "every original selection reference object and order is preserved: " + pool.Pool.AssetGuid + "/" + pool.Field);
                Check(actual.Where(r => r != null && ownIds.Contains(r.Guid)).GroupBy(r => r.Guid).All(g => g.Count() == 1), "new selections are unique in each existing pool: " + pool.Pool.AssetGuid + "/" + pool.Field);
            }
            for (int i = 0; i < classes.Length; i++) {
                var actual = (BlueprintArchetypeReference[])Get(classes[i], "m_Archetypes");
                Check(actual.Length >= oldArchetypes[i].Length && actual.Take(oldArchetypes[i].Length).Zip(oldArchetypes[i], ReferenceEquals).All(x => x), "native class retains all previous archetype references and order");
                Check(progressions[i].UIGroups.Length >= oldGroups[i].Length && progressions[i].UIGroups.Take(oldGroups[i].Length).Zip(oldGroups[i], ReferenceEquals).All(x => x), "native progression retains all previous UI group objects and order");
                var determinators = (BlueprintFeatureBaseReference[])Get(progressions[i], "m_UIDeterminatorsGroup") ?? Array.Empty<BlueprintFeatureBaseReference>();
                Check(determinators.Length >= oldDet[i].Length && determinators.Take(oldDet[i].Length).Zip(oldDet[i], ReferenceEquals).All(x => x), "native progression retains all prior determinator references and order");
            }
            foreach (string type in Types) {
                var root = (BlueprintArchetype)cache.Load(Id(Constant(type, "RootToken")));
                Check(root != null && ((BlueprintArchetypeReference[])Get(root.GetParentClass(), "m_Archetypes")).Count(r => r.Guid == root.AssetGuid) == 1, "each real native class has one exact new archetype root: " + type);
            }
            Check(before.All(b => ReferenceEquals(cache.Load(b.AssetGuid), b)), "all pre-existing native and unrelated production blueprint identities are retained in the cache");
            Check(untouched.All(p => ReferenceEquals(p.Key.ComponentsArray, p.Value.Components) && p.Key.ComponentsArray.Length == p.Value.Items.Length && p.Key.ComponentsArray.Zip(p.Value.Items, ReferenceEquals).All(x => x) && Definition(p.Key.ComponentsArray) == p.Value.Definition), "all non-target pre-existing component identities, order and authored definitions are unchanged");
            Check(untouched.All(p => ReferenceEquals(Get(p.Key, "m_AllElements"), p.Value.ElementList) && p.Key.ElementsArray.SequenceEqual(p.Value.Elements) && p.Key.ElementsArray.Select((e, i) => ReferenceEquals(e.Owner, p.Value.Owners[i])).All(x => x)), "every pre-existing non-target element list, element and owner survives all real dispatcher passes");
            var cleanup = refusalHandler.Deactivated.Actions;
            Check(cleanup.Take(oldRefusalActions.Length).Zip(oldRefusalActions, ReferenceEquals).All(x => x) && cleanup.Length == oldRefusalActions.Length + 6 && cleanup.Skip(oldRefusalActions.Length).Select(a => a.name).Distinct().Count() == 6, "only six unique named controlled-rage cleanup actions extend the real native refusal lifecycle");
            Check(skipped.Count > 0 && executed.Count > 0 && states.Where(p => targets.Contains(p.Key)).All(p => p.Value == "Registered"), "explicit dispatcher scoping preserves honest successful new-module states");
            PsychicExpansionTests.Inventory["migration01443CallerContracts"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["actualRegisterAllDispatcher"] = true, ["actualRegisterOptionalDispatcher"] = true,
                ["actualDispatches"] = dispatches, ["moduleCount"] = targets.Count, ["nativeHydratedDependencies"] = hydrated, ["nativeOnEnableCalls"] = enables, ["privateResourceCount"] = ownIds.Count,
                ["exactNewRootsAndRetriedIdentities"] = true, ["actualCatalogueDefinitionsAndCallerStates"] = true, ["nonTargetActionsSkippedWithoutSuccessReports"] = true,
                ["nonTargetGraphsAndNativeDonorElementsPreserved"] = true, ["oldPoolReferencesAndOrderPreserved"] = true,
                ["executed"] = new JArray(executed), ["scopedOut"] = new JArray(skipped.Distinct()), ["actualStates"] = JObject.FromObject(states),
                ["nativeEvidenceSha256"] = BitConverter.ToString(SHA256.Create().ComputeHash(File.ReadAllBytes(evidence))).Replace("-", ""),
                ["boundary"] = "Actual RegisterAll/RegisterOptional and all three new archetype actions run on a deep isolated cache. Their native classes, complete progression fields, donors and owned element lists are reconstructed from actual native archived records before dispatcher invocation; no provider or game process initialization is inferred. Non-target RegisterModule actions and the direct optional Investigator callback are explicitly skipped without fabricated success. The original caller cache is verified by the established full ParentSnapshot and global texts restored. Dedicated archetype suites own rule execution; complete UI, presentation, scene scheduling and full save/load are game observation steps." };
        } finally {
            harmony.UnpatchAll(harmony.Id); strings.Clear(); foreach (var pair in oldStrings) strings.Add(pair.Key, pair.Value);
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, originalCache);
            AccessTools.Method(snapshotType, "Verify").Invoke(parentSnapshot, null);
        }
        Check(strings.Count == oldStrings.Length && oldStrings.All(p => strings.TryGetValue(p.Key, out var value) && value == p.Value), "all original localization keys and values are restored");
        ((JObject)PsychicExpansionTests.Inventory["migration01443CallerContracts"])["assertions"] = checks;
        Console.WriteLine("PASS: " + checks + " Migration01443 actual caller contracts."); return checks;
    }
}
