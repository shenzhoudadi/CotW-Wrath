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
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using CallOfTheWild.Wrath.Prototype;

// The retired names/IDs below are negative-test data, never registration inputs.
internal static class RegistrationRemoval01432Tests
{
    static int checks;
    const string Prefix = "CallOfTheWild.Wrath.Prototype.";
    const string RetiredModule = "Rage powers: Atavism Totem";
    static readonly string[] RetiredTypes = {
        "AtavismTotem01429", "AtavismTotemPrerequisite", "AtavismBiteSize01429", "AtavismExistingBite"
    };
    static readonly string[] RetiredTypeIds = {
        "bf9b8e121fd74cdbb3211cdaef5dd729", "130b42006ef14f99b7bfa267c2f661ae", "cf49a9a07bbb4d3ead11d1c3c1fa5d88"
    };
    static readonly string[] RetiredTokens = {
        "AtavismTotemLesser", "AtavismTotem", "AtavismTotemGreater", "AtavismTotemBiteBuff",
        "AtavismTotemBiteSizeBuff", "AtavismTotemFerocityBuff", "AtavismTotemTrampleEffect",
        "AtavismTotemOverrun", "AtavismTotemTrampleBuff"
    };
    // Exact current-game donors, kept separate from this mod's retired copies.
    static readonly string[] NativeDonorIds = {
        "a67b51a8074ae47438280be0a87b01b6", // AnimalFuryRageBuff
        "955e356c813de1743a98ab3485d5bc69", // Ferocity
        "9292099e5fd70f84fb07fbb9b8b6a5a5", // MammothTrample
        "1a3b471ecea51f7439a946b23577fd70", // OverrunAbility
        "d99dfc9238a8a6646b32be09057c1729"  // BeastTotemLesserFeature
    };
    static Assembly Production => typeof(FlameJetHover01429).Assembly;
    static Type Module(string name) => Production.GetType(Prefix + name, true);
    static object Get(object value, string field) => AccessTools.Field(value.GetType(), field).GetValue(value);
    static string Definition(object value) => (string)AccessTools.Method(Module("Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new[] { value });
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Module("PsychicAssets"), "Id").Invoke(null, new object[] { token });
    static string[] RetiredDerivedTokens() => Enumerable.Range(0, 3).SelectMany(mode => RetiredTokens.SelectMany(token => new[] {
        "FerociousBeastPassive" + mode + Id(token), "FerociousBeastPassiveFact" + mode + Id(token)
    })).ToArray();
    static void Check(bool value, string message) { if (!value) throw new Exception("RegistrationRemoval01432: " + message); checks++; }
    // The actual native dictionary stores BlueprintCacheEntry wrappers. Freeze
    // keys first, then resolve through its real accessor outside the enumerator.
    static SimpleBlueprint[] Snapshot(BlueprintsCache cache) => ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray().Select(cache.Load).Where(b => b != null).ToArray();

    static void AssemblyAndMenu()
    {
        foreach (var name in RetiredTypes) Check(Production.GetType(Prefix + name) == null, "retired production type absent: " + name);
        var types = Production.GetTypes();
        Check(!types.Any(t => t.Namespace == Prefix.TrimEnd('.') && t.Name.IndexOf("Atavism", StringComparison.OrdinalIgnoreCase) >= 0), "no renamed Atavism production implementation remains");
        var ids = types.SelectMany(t => t.GetCustomAttributesData()).Where(a => a.AttributeType.Name == "TypeIdAttribute")
            .SelectMany(a => a.ConstructorArguments).Select(a => a.Value as string).Where(s => s != null).ToArray();
        foreach (var id in RetiredTypeIds) Check(!ids.Contains(id), "retired serialization TypeId absent: " + id);
        var definitions = ((IEnumerable)AccessTools.Field(Module("ModuleCatalog"), "Definitions").GetValue(null)).Cast<object>().ToArray();
        Check(definitions.Length > 0 && !definitions.Any(d => (string)Get(d, "Name") == RetiredModule), "actual module catalogue omits retired menu row");
        var statuses = ((IEnumerable)AccessTools.Method(Module("ModEntry"), "GetModuleStatuses").Invoke(null, null)).Cast<object>().ToArray();
        Check(statuses.Length == definitions.Length, "actual status caller and menu catalogue still have the same number of entries");
        Check(!statuses.Any(s => (string)Get(Get(s, "Definition"), "Name") == RetiredModule), "actual status snapshot omits retired module");
    }
    static void NoRetiredAssets(BlueprintsCache cache)
    {
        var retiredIds = new HashSet<BlueprintGuid>(RetiredTokens.Concat(RetiredDerivedTokens()).Select(Id));
        var retiredReferenceMarkers = retiredIds.Select(id => "ref:" + id).ToArray();
        var loaded = Snapshot(cache); // No resolving cache references inside a live enumerator.
        foreach (var token in RetiredTokens) {
            var identity = Id(token);
            Check(!loaded.Any(b => b.AssetGuid == identity), "retired authored root/private asset absent from real cache: " + token);
        }
        foreach (var token in RetiredDerivedTokens()) {
            var identity = Id(token);
            Check(!loaded.Any(b => b.AssetGuid == identity), "no Ferocious private clone of a retired Atavism source remains: " + token);
        }
        Check(!loaded.Any(b => b.name != null && b.name.StartsWith("CotWWrathPsychicAtavismTotem", StringComparison.Ordinal)), "no additional authored Atavism private asset published");
        foreach (var pool in loaded.OfType<BlueprintFeatureSelection>()) {
            foreach (var field in new[] { "m_Features", "m_AllFeatures" }) {
                var refs = (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
                Check(!refs.Any(r => r != null && retiredIds.Contains(r.Guid)), "retired entry absent from selection " + pool.name + "." + field);
            }
        }
        foreach (var bp in loaded.OfType<BlueprintScriptableObject>()) {
            var graph = Definition(bp.ComponentsArray);
            Check(!graph.Contains("AtavismTotem01429_"), "no retired rage action / totem-family prerequisite on " + bp.name);
            Check(!retiredReferenceMarkers.Any(graph.Contains), "no retired asset reference in authored component graph " + bp.name);
        }
        var strings = (IDictionary)AccessTools.Field(Module("PsychicAssets"), "Strings").GetValue(null);
        Check(!strings.Keys.Cast<string>().Any(k => k.StartsWith("cotw-wrath.psychic.full.AtavismTotem", StringComparison.Ordinal)), "runtime strings no longer register retired descriptions");
    }
    static void NativeAndForeignAnalogues(BlueprintsCache cache)
    {
        var donors = NativeDonorIds.Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject).ToArray();
        for (int i = 0; i < donors.Length; i++) Check(donors[i] != null && donors[i].AssetGuid == BlueprintGuid.Parse(NativeDonorIds[i]), "native analogue remains loadable: " + NativeDonorIds[i]);
        var donorGraphs = donors.Select(b => Definition(b.ComponentsArray)).ToArray();
        var pools = ((string[])AccessTools.Field(Module("RagePowers"), "SelectionIds").GetValue(null)).Select(id => (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).ToArray();
        var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var foreignId = BlueprintGuid.Parse("0014329c4dd74de2af2b07c2e3393147");
        Check(cache.Load(foreignId) == null, "external analogue sentinel identity unused");
        var foreign = new BlueprintFeature { name = "OtherModAtavismTotemForeignFixture", AssetGuid = foreignId, Groups = new[] { FeatureGroup.RagePower }, ComponentsArray = Array.Empty<BlueprintComponent>() };
        foreign.OnEnable(); cache.AddCachedBlueprint(foreignId, foreign);
        var foreignGraph = Definition(foreign.ComponentsArray);
        try {
            foreach (var pool in pools) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) {
                var refs = (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
                var reference = BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(foreign);
                AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(reference, foreign, null);
                AccessTools.Field(pool.GetType(), field).SetValue(pool, refs.Concat(new[] { reference }).ToArray());
            }
            // Re-enter the real native rage registration, rather than a copy of
            // its selection-appending code or a string-only absence check.
            AccessTools.Method(Module("RagePowers"), "Register").Invoke(null, new object[] { cache });
            Check(ReferenceEquals(cache.Load(foreignId), foreign) && Definition(foreign.ComponentsArray) == foreignGraph, "same-name external analogue is not removed or modified");
            for (int i = 0; i < pools.Length; i++) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) {
                var refs = (BlueprintFeatureReference[])Get(pools[i], field);
                var original = (BlueprintFeatureReference[])before[i][field == "m_Features" ? 0 : 1] ?? Array.Empty<BlueprintFeatureReference>();
                Check(refs.Count(r => r.Guid == foreignId) == 1, "external analogue retained once in native pool " + pools[i].name);
                Check(refs.Take(original.Length).Select(r => r.Guid).SequenceEqual(original.Select(r => r.Guid)), "native and pre-existing pool entries retain order: " + pools[i].name);
            }
            for (int i = 0; i < donors.Length; i++) Check(ReferenceEquals(cache.Load(donors[i].AssetGuid), donors[i]) && Definition(donors[i].ComponentsArray) == donorGraphs[i], "native Animal Fury/Ferocity/Trample/Overrun/totem graph retained: " + donors[i].name);
        }
        finally {
            for (int i = 0; i < pools.Length; i++) { AccessTools.Field(pools[i].GetType(), "m_Features").SetValue(pools[i], before[i][0]); AccessTools.Field(pools[i].GetType(), "m_AllFeatures").SetValue(pools[i], before[i][1]); }
            if (ReferenceEquals(cache.Load(foreignId), foreign)) cache.RemoveCachedBlueprint(foreignId);
        }
    }
    static void RejectsRetired(Action check, string message)
    {
        try { check(); }
        catch (Exception exception) {
            if (exception.Message.StartsWith("RegistrationRemoval01432:", StringComparison.Ordinal)) { checks++; return; }
            throw;
        }
        throw new Exception("RegistrationRemoval01432: residue fixture was incorrectly accepted: " + message);
    }
    static void ResidueFixtures(BlueprintsCache cache)
    {
        var retired = new BlueprintFeature { AssetGuid = Id(RetiredTokens[0]), name = "CotWWrathPsychic" + RetiredTokens[0], ComponentsArray = Array.Empty<BlueprintComponent>() };
        retired.OnEnable(); cache.AddCachedBlueprint(retired.AssetGuid, retired);
        try { RejectsRetired(() => NoRetiredAssets(cache), "retired exact root GUID"); }
        finally { cache.RemoveCachedBlueprint(retired.AssetGuid); }

        var hidden = new BlueprintFeature { AssetGuid = BlueprintGuid.Parse("001432b01cd74de2af2b07c2e3393147"), name = "CotWWrathPsychicAtavismTotemUnlistedPrivateEffect", ComponentsArray = Array.Empty<BlueprintComponent>() };
        hidden.OnEnable(); cache.AddCachedBlueprint(hidden.AssetGuid, hidden);
        try { RejectsRetired(() => NoRetiredAssets(cache), "additional retired private-name asset"); }
        finally { cache.RemoveCachedBlueprint(hidden.AssetGuid); }

        var poolId = ((string[])AccessTools.Field(Module("RagePowers"), "SelectionIds").GetValue(null))[0];
        var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(poolId));
        var before = Get(pool, "m_AllFeatures");
        var reference = BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(retired);
        AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(reference, retired, null);
        AccessTools.Field(pool.GetType(), "m_AllFeatures").SetValue(pool, ((BlueprintFeatureReference[])before ?? Array.Empty<BlueprintFeatureReference>()).Concat(new[] { reference }).ToArray());
        try { RejectsRetired(() => NoRetiredAssets(cache), "dangling retired selection reference"); }
        finally { AccessTools.Field(pool.GetType(), "m_AllFeatures").SetValue(pool, before); }

        var grant = new AddFacts();
        var factReference = BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(retired);
        AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(factReference, retired, null);
        AccessTools.Field(typeof(AddFacts), "m_Facts").SetValue(grant, new[] { factReference });
        var holder = new BlueprintFeature { AssetGuid = BlueprintGuid.Parse("001432b02cd74de2af2b07c2e3393147"), name = "OtherModDanglingRetiredReferenceFixture", ComponentsArray = new BlueprintComponent[] { grant } };
        holder.OnEnable(); cache.AddCachedBlueprint(holder.AssetGuid, holder);
        try { RejectsRetired(() => NoRetiredAssets(cache), "retired asset referenced by an otherwise unrelated component graph"); }
        finally { cache.RemoveCachedBlueprint(holder.AssetGuid); }
    }
    static string SourceProject()
    {
        // The frozen outputs/ copy is optional now; the live tree is canonical.
        foreach (var candidate in new[] { TestPaths.RepoRoot, Path.Combine(TestPaths.EvidenceRoot, "outputs", "CallOfTheWild.Wrath.Prototype") })
            if (File.Exists(Path.Combine(candidate, "src", "ModEntry.cs"))) return candidate;
        throw new DirectoryNotFoundException("Canonical source tree required for the removal contract.");
    }
    static void SourceRemoval()
    {
        var project = SourceProject();
        Check(!File.Exists(Path.Combine(project, "src", "AtavismTotem01429.cs")), "retired implementation source physically removed");
        foreach (var file in Directory.GetFiles(Path.Combine(project, "src"), "*.cs", SearchOption.AllDirectories)) {
            var text = File.ReadAllText(file);
            foreach (var name in RetiredTypes) Check(!text.Contains(name), "no retired production reference in " + Path.GetFileName(file));
        }
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = 0;
        AssemblyAndMenu(); SourceRemoval(); NoRetiredAssets(cache); ResidueFixtures(cache); NativeAndForeignAnalogues(cache); NoRetiredAssets(cache);
        PsychicExpansionTests.Inventory["registrationRemoval01432Contracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["newItemsThisRelease"] = 0,
            ["retiredRootCount"] = 3, ["retiredAuthoredAssetCount"] = RetiredTokens.Length,
            ["retiredDerivedAssetIdsChecked"] = RetiredDerivedTokens().Length,
            ["retiredTypeIds"] = JArray.FromObject(RetiredTypeIds), ["retiredProductionTypesAbsent"] = true,
            ["actualModuleCatalogueAndStatusCaller"] = true, ["allCachedSelectionsAndComponentGraphsClean"] = true,
            ["nativeDonorCount"] = NativeDonorIds.Length, ["nativeDonorIdentityAndGraphPreserved"] = true,
            ["foreignSameNameAnaloguePreserved"] = true, ["realRageRegistrationReentry"] = true,
            ["negativeResidueFixtures"] = 4,
            ["boundary"] = "Current native cache and catalogue/status caller are inspected; real RagePowers.Register is re-entered with a staged external analogue and then fixture entries are removed. The analogue is test data, not an external DLL. Native donor definitions and reference order are compared before/after; no game is launched and no old-save loading is claimed."
        };
        Console.WriteLine("PASS: " + checks + " 01432 Atavism removal/native-donor contracts.");
        return checks;
    }
}
