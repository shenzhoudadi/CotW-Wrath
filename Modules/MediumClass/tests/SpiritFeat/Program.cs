using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mono.Cecil;

// Static audit of the 4th-level Spirit Feat implementation. The progression model test already
// checks that the pick lands on 4/8/12/16/20; this audit covers what that test cannot see: the
// blueprint ids, the shipped localisation, that the native feat list this pool copies still has the
// shape it is copied with, and that the built module really wires the pool up.
//
// Usage: SpiritFeat <game Assembly-CSharp.dll> [built MediumClass.dll [blueprints.zip]]
int checks = 0;
var failures = new List<string>();
void Check(bool ok, string label) { checks++; if (!ok) failures.Add(label); }

string gamePath = args.Length > 0 ? args[0] : null;
string modPath = args.Length > 1 ? args[1] : null;
string zipPath = args.Length > 2 ? args[2] : null;
string root = AppContext.BaseDirectory;
while (root != null && !File.Exists(Path.Combine(root, "MediumClass.csproj"))) root = Path.GetDirectoryName(root);
if (root == null) { Console.Error.WriteLine("FAIL: cannot locate the module directory."); return 1; }

// ---------------------------------------------------------------------------------------------------
// 1. Blueprint ids: the selection is a real, unique GUID and the copied native list is the one the
//    implementation was written against.
// ---------------------------------------------------------------------------------------------------
var guidsSource = File.ReadAllText(Path.Combine(root, "Utilities", "Guids.cs"));
var declared = Regex.Matches(guidsSource, "internal const string (\\w+) = \"([0-9a-fA-F-]+)\";")
    .Select(m => (Name: m.Groups[1].Value, Value: m.Groups[2].Value)).ToList();
var spiritFeat = declared.Where(d => d.Name == "MediumSpiritFeat").ToList();
Check(spiritFeat.Count == 1, $"Guids.cs declares MediumSpiritFeat once (found {spiritFeat.Count})");
foreach (var entry in spiritFeat)
    Check(Guid.TryParse(entry.Value, out _), $"MediumSpiritFeat is a parseable GUID ({entry.Value})");
var duplicates = declared.GroupBy(d => d.Value).Where(g => g.Count() > 1)
    .Select(g => string.Join("/", g.Select(x => x.Name))).ToList();
Check(duplicates.Count == 0, "no two Guids.cs constants share a value: " + string.Join(", ", duplicates));
Check(declared.FirstOrDefault(d => d.Name == "NativeBasicFeatSelection").Value == "247a4068296e8be42890143f451b4b45",
    "Guids.cs NativeBasicFeatSelection is the native feat list the pool is copied from");

// ---------------------------------------------------------------------------------------------------
// 2. Localisation: both keys the implementation uses are shipped in both languages, and no Spirit
//    Feat key is left unused.
// ---------------------------------------------------------------------------------------------------
var implementation = File.ReadAllText(Path.Combine(root, "Medium", "SpiritFeat.cs"));
var used = Regex.Matches(implementation, "\"(SpiritFeat\\.[A-Za-z.]+)\"").Select(m => m.Groups[1].Value)
    .ToHashSet();
Check(used.Count > 0, "the implementation references Spirit Feat text keys");

var pack = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Strings", "Medium.json"))).RootElement;
var shipped = new Dictionary<string, (string En, string Zh)>();
foreach (var entry in pack.EnumerateArray())
    shipped[entry.GetProperty("Key").GetString()] =
        (entry.GetProperty("enGB").GetString(), entry.GetProperty("zhCN").GetString());
foreach (var key in used.OrderBy(k => k))
{
    Check(shipped.ContainsKey(key), $"Medium.json ships {key}");
    if (!shipped.TryGetValue(key, out var text)) continue;
    Check(!string.IsNullOrWhiteSpace(text.En), $"{key} has English text");
    Check(!string.IsNullOrWhiteSpace(text.Zh), $"{key} has Chinese text");
    Check(text.Zh.Any(c => c > 0x7F), $"{key} Chinese text is actually translated");
}
var unused = shipped.Keys.Where(k => k.StartsWith("SpiritFeat.") && !used.Contains(k)).ToList();
Check(unused.Count == 0, "no orphaned Spirit Feat text keys: " + string.Join(", ", unused));
// The description has to keep the levels the progression grants, or the two drift apart.
Check(shipped.TryGetValue("SpiritFeat.Description", out var description)
        && description.Zh.Contains("4") && description.Zh.Contains("8") && description.Zh.Contains("20"),
    "the description names the levels the picks come at");

// ---------------------------------------------------------------------------------------------------
// 3. Native anchors: the pool is a copy of the game's own feat selection, so the members it is read
//    and written through have to still exist.
// ---------------------------------------------------------------------------------------------------
if (gamePath == null)
{
    Console.Error.WriteLine("SKIP: no game assembly, native anchor checks not run.");
}
else
{
    using var game = AssemblyDefinition.ReadAssembly(gamePath);
    var module = game.MainModule;
    TypeDefinition Type(string fullName) => module.GetType(fullName);
    bool HasField(string type, string field) => Type(type)?.Fields.Any(f => f.Name == field) == true;
    string FieldType(string type, string field) => Type(type)?.Fields.FirstOrDefault(f => f.Name == field)?.FieldType.FullName;

    const string selection = "Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection";
    Check(FieldType(selection, "m_AllFeatures") == "Kingmaker.Blueprints.BlueprintFeatureReference[]",
        "BlueprintFeatureSelection.m_AllFeatures is still the feature reference array the pool is copied into");
    Check(HasField(selection, "Group"), "BlueprintFeatureSelection.Group still exists");
    // The pool only works because the game refuses a feature that is already at its rank cap, which
    // is what stops the same feat being taken twice through five picks.
    var canSelect = Type(selection)?.Methods.FirstOrDefault(m => m.Name == "CanSelect");
    var canSelectRefs = canSelect?.Body.Instructions.Select(i => (i.Operand as MemberReference)?.Name).Where(n => n != null).ToList();
    Check(canSelectRefs?.Contains("GetRank") == true && canSelectRefs.Contains("Ranks"),
        "BlueprintFeatureSelection.CanSelect still refuses a feature that reached its own Ranks");
    Check(canSelectRefs?.Contains("MeetsPrerequisites") == true,
        "BlueprintFeatureSelection.CanSelect still applies the feat's prerequisites");
    var items = Type(selection)?.Properties.FirstOrDefault(p => p.Name == "Items")?.GetMethod;
    Check(items?.Body.Instructions.Any(i => (i.Operand as MemberReference)?.Name == "get_AllFeatures") == true,
        "BlueprintFeatureSelection.Items still lists m_AllFeatures");
    Check(module.GetType("Kingmaker.Blueprints.Classes.FeatureGroup")?.Fields.Any(f => f.Name == "Feat") == true,
        "FeatureGroup.Feat still exists (the pool's group)");
}

// ---------------------------------------------------------------------------------------------------
// 4. The native feat list really is the game's whole feat pool, so the copy offers every feat.
// ---------------------------------------------------------------------------------------------------
if (zipPath == null || !File.Exists(zipPath))
{
    Console.Error.WriteLine("SKIP: no blueprints.zip, native feat list checks not run.");
}
else
{
    using var zip = ZipFile.OpenRead(zipPath);
    var entry = zip.Entries.FirstOrDefault(e => e.FullName == "Classes/Basic/BasicFeatSelection.jbp");
    Check(entry != null, "blueprints.zip ships Classes/Basic/BasicFeatSelection.jbp");
    if (entry != null)
    {
        using var reader = new StreamReader(entry.Open());
        var data = JsonDocument.Parse(reader.ReadToEnd().TrimStart('\uFEFF')).RootElement.GetProperty("Data");
        var group = data.GetProperty("Group").GetString();
        var features = data.GetProperty("m_AllFeatures").GetArrayLength();
        Check(group == "Feat", $"the native feat selection is grouped as Feat ({group})");
        Check(features > 200, $"the native feat selection is the whole feat pool ({features} entries)");
        Check(data.GetProperty("Ranks").GetInt32() == 1,
            "the native feat selection is a one-pick selection, so the pool has to be copied rather than reused");
    }
}

// ---------------------------------------------------------------------------------------------------
// 5. The built module: the pool is copied at configuration time, re-synced in the delayed phase and
//    granted by the progression.
// ---------------------------------------------------------------------------------------------------
if (modPath == null || !File.Exists(modPath))
{
    Console.Error.WriteLine("SKIP: no built module, wiring checks not run.");
}
else
{
    using var mod = AssemblyDefinition.ReadAssembly(modPath);
    var type = mod.MainModule.GetType("MediumClass.Medium.SpiritFeat");
    Check(type != null, "the built module contains SpiritFeat");
    if (type != null)
    {
        var configure = type.Methods.FirstOrDefault(m => m.Name == "ConfigureEnabled");
        var delayed = type.Methods.FirstOrDefault(m => m.Name == "ConfigureDelayed");
        Check(configure != null, "the built module contains SpiritFeat.ConfigureEnabled");
        Check(delayed != null, "the built module contains SpiritFeat.ConfigureDelayed");
        var calls = configure?.Body.Instructions.Select(i => (i.Operand as MemberReference)?.Name).Where(n => n != null).ToList()
            ?? new List<string>();
        Check(calls.Contains("SetAllFeatures") || calls.Contains("AddToAllFeatures"), "ConfigureEnabled copies the pool into the selection");
        Check(calls.Contains("SetRanks"), "ConfigureEnabled sets the number of picks");
        Check(calls.Contains("SetGroup"), "ConfigureEnabled keeps the feat group so new feats join the pool");
        Check(calls.Contains("NativePool"), "ConfigureEnabled reads the native feat list");
        var delayedCalls = delayed?.Body.Instructions.Select(i => (i.Operand as MemberReference)?.Name).Where(n => n != null).ToList()
            ?? new List<string>();
        Check(delayedCalls.Contains("Get") && delayedCalls.Contains("Concat"),
            "ConfigureDelayed re-reads the native list and appends what is new");

        var progression = mod.MainModule.GetType("MediumClass.Medium.MediumProgression")?.Methods.FirstOrDefault(m => m.Name == "Configure");
        var progressionCalls = progression?.Body.Instructions.Select(i => (i.Operand as MemberReference))
            .Where(r => r != null).ToList() ?? new List<MemberReference>();
        Check(progressionCalls.Any(c => c.Name == "ConfigureEnabled" && c.DeclaringType?.Name == "SpiritFeat"),
            "MediumProgression builds the Spirit Feat selection before the level entries");
        var main = mod.MainModule.GetType("MediumClass.Main")?.NestedTypes
            .SelectMany(t => t.Methods).FirstOrDefault(m => m.Name == "AfterBlueprintCachePatches");
        Check(main?.Body.Instructions.Any(i => (i.Operand as MemberReference)?.Name == "ConfigureDelayed"
                && (i.Operand as MemberReference)?.DeclaringType?.Name == "SpiritFeat") == true,
            "the delayed blueprint phase re-syncs the pool");
    }
}

if (failures.Count > 0)
{
    foreach (var failure in failures) Console.Error.WriteLine("FAIL: " + failure);
    Console.Error.WriteLine($"{failures.Count} of {checks} checks failed.");
    return 1;
}
Console.WriteLine($"PASS: {checks} Spirit Feat audits (GUID hygiene, shipped localisation, native anchors, native feat pool, build wiring); static inspection, not Unity gameplay.");
return 0;
