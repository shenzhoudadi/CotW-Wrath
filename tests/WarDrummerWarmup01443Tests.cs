using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;

internal static class WarDrummerWarmup01443Tests
{
    static readonly Type Module = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WarDrummer01441", true);
    static void Register(BlueprintsCache cache) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache });
    static bool Reject(Action a) { try { a(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    internal static int Run(BlueprintsCache cache)
    {
        // Reuse the exact native class fixture, then warm through the same
        // native accessors used to assemble archetype level-up entries.
        AccessTools.Method(typeof(WarDrummer01441Tests), "Hydrate").Invoke(null, new object[] { cache, false });
        Register(cache); var id = (BlueprintGuid)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id").Invoke(null, new object[] { "WarDrummerArchetype01441" }); var root = (BlueprintArchetype)cache.Load(id);
        int checks = 0, calls = 0; void Check(bool ok, string message) { if (!ok) throw new Exception("WarDrummerWarmup01443: " + message); checks++; }
        var added = root.AddFeatures; var removed = root.RemoveFeatures;
        for (int n = 1; n <= 20; n++) { root.GetAddEntry(n); root.GetRemoveEntry(n); calls += 2; }
        Register(cache); Register(cache); Check(ReferenceEquals(cache.Load(id), root) && ReferenceEquals(root.AddFeatures, added) && ReferenceEquals(root.RemoveFeatures, removed), "native lazy feature list warming preserves exact private identities and author arrays");
        int level = added[0].Level; added[0].Level = 20;
        try { Check(Reject(() => Register(cache)), "authored add level mutation remains rejected"); } finally { added[0].Level = level; }
        var field = AccessTools.Field(typeof(LevelEntry), "m_Features"); var original = field.GetValue(removed[0]); field.SetValue(removed[0], Activator.CreateInstance(field.FieldType));
        try { Check(Reject(() => Register(cache)), "authored remove reference mutation remains rejected even after cache warmup"); } finally { field.SetValue(removed[0], original); }
        Register(cache); Check(ReferenceEquals(cache.Load(id), root), "restored author graph retries without rebuilding identities");
        PsychicExpansionTests.Inventory["archetypeWarmup01443Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["nativeGetEntryCalls"] = calls, ["authorLevelMutationRejected"] = true, ["authorReferenceMutationRejected"] = true, ["privateIdentityPreserved"] = true, ["boundary"] = "Actual War Drummer Register and native BlueprintArchetype.GetAddEntry/GetRemoveEntry lazy-list warming. Only Level plus raw m_Features are authored archetype rows; LevelEntry.m_FeaturesList is an equivalent native resolved cache. Authored levels/references remain checked. No gameplay-effect change." };
        Console.WriteLine("PASS: " + checks + " WarDrummer archetype warmup01443 checks."); return checks;
    }
}
