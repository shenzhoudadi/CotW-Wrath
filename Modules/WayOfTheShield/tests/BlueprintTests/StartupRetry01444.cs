using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;
using Way_of_the_shield;
using ShieldMain = Way_of_the_shield.Main;

// The complete real shield bridge, installer, builders and patch set execute.
// Only unrelated modules and parent scene/service endpoints are controlled.
internal static class ShieldStartupRetry01444
{
    static Assembly parent;
    static int installs, configurations, cacheCallbacks, localeCallbacks, checks;
    static readonly List<Exception> failures = new List<Exception>();
    static Type T(string name) => parent.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    static FieldInfo F(Type type, string name) => AccessTools.Field(type, name);
    static bool Skip() => false;
    static bool Absent(ref bool __result) { __result = false; return false; }
    static bool OtherModule(ref Action register) { register = () => { }; return true; }
    static bool UrbanAvailableScope063(string name) => name != "Bloodrager archetype: Urban Bloodrager";
    static void Installed() => installs++;
    static void Configured() => configurations++;
    
    static void Report(string name, bool succeeded, Exception exception)
    { if (name == "Way of the Shield" && !succeeded) failures.Add(exception); }
    static void Check(bool value, string message)
    { if (!value) throw new Exception("ShieldStartupRetry01444: " + message); checks++; }
    static void Callback(BlueprintsCache cache, bool locale)
    {
        if (locale) localeCallbacks++; else cacheCallbacks++;
        var type = T("ModEntry").GetNestedType(locale ? "LocalizationReadyPatch" : "CacheReadyPatch", BindingFlags.NonPublic);
        AccessTools.Method(type, "Postfix").Invoke(null, locale ? Array.Empty<object>() : new object[] { cache });
    }
    static Dictionary<string, int> Patches()
    {
        var rows = new Dictionary<string, int>();
        foreach (var method in Harmony.GetAllPatchedMethods()) {
            var info = Harmony.GetPatchInfo(method);
            foreach (var part in new[] { Tuple.Create("prefix", info.Prefixes), Tuple.Create("postfix", info.Postfixes), Tuple.Create("transpiler", info.Transpilers), Tuple.Create("finalizer", info.Finalizers) })
                foreach (var patch in part.Item2.Where(p => p.owner == "CotW.WayOfTheShield")) {
                    string key = method.DeclaringType.FullName + "::" + method + "|" + part.Item1 + "|" + patch.PatchMethod.DeclaringType.FullName + "::" + patch.PatchMethod;
                    rows[key] = rows.TryGetValue(key, out var count) ? count + 1 : 1;
                }
        }
        return rows;
    }
    static bool Same(Dictionary<string, int> a, Dictionary<string, int> b)
        => a.Count == b.Count && a.All(r => b.TryGetValue(r.Key, out var count) && count == r.Value);
    static int Pending(BlueprintsCache cache)
    {
        object table = F(T("ModEntry"), "registrations").GetValue(null);
        var args = new object[] { cache, null };
        Check((bool)AccessTools.Method(table.GetType(), "TryGetValue").Invoke(table, args), "actual ModEntry owns the cache ledger");
        return (int)AccessTools.Property(args[1].GetType(), "PendingCount").GetValue(args[1], null);
    }
    internal static JObject Run(BlueprintsCache cache, string parentPath)
    {
        parent = Assembly.LoadFrom(parentPath);
        installs = configurations = cacheCallbacks = localeCallbacks = checks = 0; failures.Clear();
        var entry = T("ModEntry"); var bridge = T("WayOfTheShieldIntegration");
        var saved = new Dictionary<FieldInfo, object>();
        void Set(Type type, string name, object value) { var field = F(type, name); saved[field] = field.GetValue(null); field.SetValue(null, value); }
        var states = (Dictionary<string, string>)F(entry, "moduleStates").GetValue(null); var oldStates = new Dictionary<string, string>(states);
        var h = new Harmony("CotW.ShieldStartupRetry01444.Boundaries");
        void Prefix(Type type, string method, string hook) => h.Patch(AccessTools.Method(type, method), prefix: new HarmonyMethod(typeof(ShieldStartupRetry01444), hook));
        void Observe(Type type, string method, string hook) => h.Patch(AccessTools.Method(type, method), postfix: new HarmonyMethod(typeof(ShieldStartupRetry01444), hook));
        var faultText = Environment.GetEnvironmentVariable("COTW_SHIELD_RETRY_FAULT");
        var fault = string.IsNullOrEmpty(faultText) ? new JObject { ["case"] = "sacred-throw", ["dependency"] = "d7963e1fcf260c148877afd3252dbc91", ["completedBeforeFailure"] = 11 } : JObject.Parse(faultText);
        string dependency = (string)fault["dependency"], caseName = (string)fault["case"];
        var donor = cache.Load(BlueprintGuid.Parse(dependency));
        var scriptableDonor = donor as BlueprintScriptableObject;
        var donorComponents = scriptableDonor?.ComponentsArray;
        string missingComponent = (string)fault["component"];
        bool faultActive = false;
        object mechanics = null, mythicReference = null;
        FieldInfo mythicField = null;
        if ((bool?)fault["rootMythicNull"] == true) {
            var root = F(typeof(BlueprintRegistrationTests), "root").GetValue(null);
            mechanics = F(root.GetType(), "SystemMechanics").GetValue(root);
            mythicField = F(mechanics.GetType(), "m_MythicFeatSelection");
            mythicReference = mythicField.GetValue(mechanics);
        }
        try {
            Check(donor != null, "actual native dependency exists for " + caseName);
            Check(!Patches().Any(), "cold shield owner has no preexisting patches");
            // Model completed bundle preparation; host DLL locations differ from
            // the installed bundle. Register/Configure/InstallRuntime stay real.
            Set(bridge, "entry", typeof(ShieldMain)); Set(bridge, "prepared", true); Set(bridge, "external", false); Set(bridge, "error", null); Set(bridge, "configured", false);
            Set(entry, "modification", FormatterServices.GetUninitializedObject(typeof(OwlcatModification)));
            Set(entry, "registrationInProgress", 0); Set(entry, "registrationCompleted", false); Set(entry, "deferredPatchesAttempted", true);
            Prefix(T("BlueprintRegistration"), "RegisterModule", nameof(OtherModule));
            Prefix(T("BlueprintRegistration"), "RegisterAvailableModule", nameof(UrbanAvailableScope063));
            Prefix(T("ExtraInvestigatorTalent"), "Register", nameof(Absent)); Prefix(T("ProviderResourceFeats"), "Available", nameof(Absent));
            Prefix(T("MediumIntegration"), "Register", nameof(Skip));
            foreach (string method in new[] { "WriteDiagnostics", "TryMenu", "ApplyLocalization" }) Prefix(entry, method, nameof(Skip));
            // Supply only the logger endpoint, preserving actual ReportModule.
            var mod = F(entry, "modification").GetValue(null);
            F(typeof(OwlcatModification), "Logger").SetValue(mod, FormatterServices.GetUninitializedObject(typeof(Owlcat.Runtime.Core.Logging.LogChannel)));
            Observe(entry, "ReportModule", nameof(Report));
            Prefix(typeof(ShieldMain), "InstallRuntime", nameof(Installed)); Prefix(bridge, "Register", nameof(Configured));
            if (missingComponent == null) cache.RemoveCachedBlueprint(donor.AssetGuid);
            else {
                Check(donorComponents != null && donorComponents.Any(c => c.GetType().Name == missingComponent), "actual native component exists for " + caseName);
                scriptableDonor.ComponentsArray = donorComponents.Where(c => c.GetType().Name != missingComponent).ToArray();
            }
            if (mythicField != null) mythicField.SetValue(mechanics, null);
            faultActive = true;
            Callback(cache, false);
            Check(Pending(cache) == 1 && failures.Count == 1, "actual CacheReady records real incomplete blueprint step; pending=" + Pending(cache) + "; failures=" + string.Join("\n", failures));
            if (caseName == "sacred-throw") Check(failures[0].ToString().Contains("Sacred Shield dependency missing/wrong type: " + dependency), "original actual Sacred dependency throw path retained");
            bool returnedIncomplete = failures[0].ToString().Contains("returned without publishing");
            if ((bool?)fault["expectNormalReturn"] == true) Check(returnedIncomplete, "actual builder returned normally without its required publication; result gate rejects completion");
            Check(installs == 1 && configurations == 1, "real shield bridge and runtime installer reached incomplete blueprint step");
            Check((bool)F(typeof(ShieldMain), "runtimeInstalled").GetValue(null) && !(bool)F(typeof(ShieldMain), "configured").GetValue(null), "patch success committed independently of blueprint failure");
            var steps = (IEnumerable<MethodInfo>)F(typeof(ShieldMain), "completedBlueprintSteps").GetValue(null);
            int completedBeforeFailure = (int)fault["completedBeforeFailure"];
            Check(steps.Count() == completedBeforeFailure && steps.All(m => m.DeclaringType != typeof(Way_of_the_shield.NewArchetypes.SacredShield)), "only actually completed builders committed; incomplete builder remains retryable");
            var first = Patches(); Check(first.Count == 59 && first.All(p => p.Value == 1), "complete actual owner patch entries installed once");
            Check(first.Keys.Any(k => k.Contains("MountedPowerAttack::")), "real mounted Power Attack transpiler installed");
            var partial = Utilities.Owned.ToDictionary(p => p.Key, p => p.Value);
            var components = partial.ToDictionary(p => p.Key, p => p.Value.ComponentsArray);
            var loadedComponents = new Dictionary<BlueprintScriptableObject, BlueprintComponent[]>();
            cache.ForEachLoaded((id, asset) => { if (asset is BlueprintScriptableObject bp) loadedComponents[bp] = bp.ComponentsArray; });
            int cacheCount = loadedComponents.Count;
            if (caseName == "upsetting-normal-return")
                Check(!(bool)F(typeof(Way_of_the_shield.NewFeatsAndAbilities.UpsettingShieldStyle), "Created").GetValue(null)
                    && cache.Load(BlueprintGuid.Parse("7da0bf0d789d4528b4c3b707eb98fbd2")) == null,
                    "actual Upsetting normal return leaves neither Created nor published style");
            Check(cache.Load(BlueprintGuid.Parse("56f19f65e28b4c3fb03e425fb047e08a")) == null, "failure happens after earlier real builders, before archetype publication");
            Callback(cache, true);
            Check(Pending(cache) == 1 && failures.Count == 2, "actual LocalizationReady retries the still-failing shield body; failures=" + string.Join("\n", failures));
            Check(installs == 1 && configurations == 2 && Same(first, Patches()), "persistent blueprint failure does not reinstall or remove any owner patch");
            Check(partial.All(p => ReferenceEquals(cache.Load(p.Key), p.Value)), "partial successful blueprint identities preserved across failure");
            Check(steps.Count() == completedBeforeFailure && partial.All(p => ReferenceEquals(p.Value.ComponentsArray, components[p.Key])), "persistent failure never repeats completed blueprint components");
            Check(loadedComponents.All(p => ReferenceEquals(p.Key.ComponentsArray, p.Value)), "repeated failure preserves every loaded native and private component array");
            int retryCacheCount = 0; cache.ForEachLoaded((id, asset) => { if (asset is BlueprintScriptableObject) retryCacheCount++; });
            Check(cacheCount == retryCacheCount && Utilities.Owned.Count == partial.Count, "repeated failure publishes no duplicate or additional GUID");
            if (missingComponent == null) cache.AddCachedBlueprint(donor.AssetGuid, donor);
            else scriptableDonor.ComponentsArray = donorComponents;
            if (mythicField != null) mythicField.SetValue(mechanics, mythicReference);
            faultActive = false;
            Callback(cache, true);
            Check(Pending(cache) == 0 && configurations == 3, "dependency restoration recovers via real LocalizationReady; failures=" + string.Join("\n", failures));
            Check((bool)F(typeof(ShieldMain), "configured").GetValue(null) && (bool)F(bridge, "configured").GetValue(null), "real bundled module and parent bridge commit blueprint success");
            Check(states["Way of the Shield"] == "Registered" && failures.Count == 2, "real module report transitions Failed to Registered");
            Check(installs == 1 && Same(first, Patches()), "successful recovery retains every exact single-owner patch entry");
            Check(partial.All(p => ReferenceEquals(cache.Load(p.Key), p.Value)), "recovery preserves already published shield objects");
            Check(steps.Count() == 12 && partial.All(p => ReferenceEquals(p.Value.ComponentsArray, components[p.Key])), "recovery commits only the remaining builder without duplicating earlier components");
            var owned = Utilities.Owned.ToDictionary(p => p.Key, p => p.Value);
            Callback(cache, false); Callback(cache, true);
            Check(configurations == 3 && installs == 1 && Same(first, Patches()), "later ready callbacks skip completed shield registration and patch installation");
            Check(owned.Count == Utilities.Owned.Count && owned.All(p => ReferenceEquals(cache.Load(p.Key), p.Value)), "later callbacks preserve all final assets");
            return new JObject { ["case"] = caseName, ["dependency"] = dependency, ["component"] = missingComponent, ["completedBeforeFailure"] = completedBeforeFailure, ["actualNormalReturnRejected"] = returnedIncomplete, ["passed"] = true, ["assertions"] = checks, ["cacheReadyCallbacks"] = cacheCallbacks, ["localizationReadyCallbacks"] = localeCallbacks, ["actualBridgeCalls"] = configurations, ["actualInstallRuntimeCalls"] = installs, ["dependencyFailures"] = failures.Count, ["singleOwnerPatchEntries"] = first.Count, ["exactPatchSnapshotPreserved"] = true, ["actualParentCallbacks"] = true, ["actualShieldBridgeAndBuilders"] = true, ["mountedTranspilerPreserved"] = true, ["successfulRegistrationSkipped"] = true, ["boundary"] = "Production ModEntry CacheReady/LocalizationReady postfixes, pending ledger, actual WayOfTheShieldIntegration.Register, ConfigureIntegrated, InstallRuntime and all shield builders execute. Each fresh process removes/restores the specified actual native dependency or component. The Sacred case executes the original late exception; safe early-return builders execute and must fail the publication-result gate. Unsafe partial-publication paths are preflighted. Bundle preparation fields and unrelated initial/optional/Medium bodies plus parent menu/log/localization IO are controlled; native game Init/async scheduling/scene execution are outside this host. Existing visual/root/profile fixture endpoints are retained. No shield registration body or runtime patch installer is replaced." };
        } finally {
            // The following whole-module checks must inspect the recovered
            // native graph, including components added during recovery.
            if (faultActive) {
                if (donor != null) cache.AddCachedBlueprint(donor.AssetGuid, donor);
                if (scriptableDonor != null && missingComponent != null) scriptableDonor.ComponentsArray = donorComponents;
                if (mythicField != null) mythicField.SetValue(mechanics, mythicReference);
            }
            h.UnpatchAll(h.Id);
            foreach (var row in saved) row.Key.SetValue(null, row.Value);
            states.Clear(); foreach (var row in oldStates) states.Add(row.Key, row.Value);
        }
    }
}
