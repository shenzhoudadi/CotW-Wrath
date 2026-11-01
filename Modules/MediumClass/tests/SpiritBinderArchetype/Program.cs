using System.Text.Json;
using MediumClass.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;

int checks = 0;
var failures = new List<string>();
void Check(bool condition, string label) { checks++; if (!condition) failures.Add(label); }

if (args.Length < 2)
{
    Console.Error.WriteLine("用法：SpiritBinderArchetype <打包产物 MediumClass.dll> <游戏 Assembly-CSharp.dll> [Strings/Feats.json]");
    return 1;
}

IEnumerable<MethodReference> Calls(MethodDefinition method) =>
    method.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>();
IEnumerable<FieldReference> Fields(MethodDefinition method) =>
    method.Body.Instructions.Select(i => i.Operand).OfType<FieldReference>();
int Count(IEnumerable<MethodReference> calls, string declaringType, string name) =>
    calls.Count(c => c.DeclaringType.Name == declaringType && c.Name == name);

// AddEntry(level, params Blueprint<BlueprintFeatureBaseReference>[]): walk back from the
// call to the parameter array, then to the level constant that precedes it.
int LevelAt(IList<Instruction> il, int call)
{
    int newArray = -1;
    for (int i = call - 1; i >= 0 && newArray < 0; i--)
        if (il[i].OpCode == OpCodes.Newarr) newArray = i;
    if (newArray < 2) throw new Exception("unexpected AddEntry IL: no parameter array");
    for (int i = newArray - 2; i >= 0; i--)
        if (Constant(il[i]) is int level) return level;
    throw new Exception("unexpected AddEntry IL: no level constant");
}
int? Constant(Instruction instruction) => instruction.OpCode.Code switch
{
    Code.Ldc_I4_0 => 0, Code.Ldc_I4_1 => 1, Code.Ldc_I4_2 => 2, Code.Ldc_I4_3 => 3,
    Code.Ldc_I4_4 => 4, Code.Ldc_I4_5 => 5, Code.Ldc_I4_6 => 6, Code.Ldc_I4_7 => 7,
    Code.Ldc_I4_8 => 8, Code.Ldc_I4_S => Convert.ToInt32(instruction.Operand),
    Code.Ldc_I4 => Convert.ToInt32(instruction.Operand),
    _ => null
};

// Native contracts: an archetype adds/removes features per level, and a feature
// selection is itself a feature, so nesting the two native feat pools inside one
// selection is type-legal (the game's LoremasterSecretSelection does the same).
using (var game = AssemblyDefinition.ReadAssembly(args[1]))
{
    var selection = game.MainModule.GetType("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection");
    Check(selection?.BaseType.FullName == "Kingmaker.Blueprints.Classes.BlueprintFeature",
        "native feature selection is itself a feature, so one selection may nest another");
    Check(selection?.Fields.Single(f => f.Name == "m_AllFeatures").FieldType.FullName
        == "Kingmaker.Blueprints.BlueprintFeatureReference[]", "native selection pool stores feature references");
    var archetype = game.MainModule.GetType("Kingmaker.Blueprints.Classes.BlueprintArchetype");
    foreach (var name in new[] { "AddFeatures", "RemoveFeatures" })
        Check(archetype?.Fields.Single(f => f.Name == name).FieldType.FullName == "Kingmaker.Blueprints.Classes.LevelEntry[]",
            "native archetype " + name + " is a per-level list");
    Check(game.MainModule.GetType("Kingmaker.Blueprints.Classes.LevelEntry")?.Fields.Single(f => f.Name == "m_Features")
        .FieldType.FullName == "System.Collections.Generic.List`1<Kingmaker.Blueprints.BlueprintFeatureBaseReference>",
        "native level entry holds base feature references");
}

var shipped = new HashSet<string>();
string stringsPath = args.Length > 2 ? args[2] : "Strings/Feats.json";
try
{
    using var pack = JsonDocument.Parse(File.ReadAllText(stringsPath));
    foreach (var entry in pack.RootElement.EnumerateArray()) shipped.Add(entry.GetProperty("Key").GetString());
}
catch (Exception e)
{
    failures.Add("shipped localization " + stringsPath + " could not be read: " + e.Message);
}

using (var mod = AssemblyDefinition.ReadAssembly(args[0]))
{
    // BlueprintCore publishes an archetype to the class handed to New().
    var configurator = mod.MainModule.GetType("BlueprintCore.Blueprints.CustomConfigurators.Classes.ArchetypeConfigurator");
    Check(configurator != null, "installer ships the BlueprintCore archetype configurator");
    if (configurator != null)
    {
        Check(Calls(configurator.Methods.Single(m => m.Name == "New")).Any(m => m.Name == "SetClass"),
            "BlueprintCore New keeps the parent class");
        Check(Calls(configurator.Methods.Single(m => m.Name == "OnConfigureCompleted")).Any(m => m.Name == "AddToArchetypes"),
            "BlueprintCore publishes the archetype to its parent class on configure");
    }

    var binder = mod.MainModule.GetType("MediumClass.Features.MediumSpecific.SpiritBinder");
    Check(binder != null, "installer ships the spirit binder archetype");
    if (binder == null) { Report(); return failures.Count == 0 ? 0 : 1; }

    var configure = binder.Methods.Single(m => m.Name == "Configure");
    var literals = configure.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldstr).Select(i => (string)i.Operand).ToArray();
    foreach (var (label, guid) in new[]
    {
        ("archetype", Guids.SpiritBinderArchetype),
        ("marker feature", Guids.SpiritBinderFeature),
        ("bonus feat selection", Guids.SpiritBinderBonusFeat),
        ("parent medium class", Guids.Medium)
    })
        Check(literals.Contains(guid), "installer creates the " + label + " with its saved GUID");

    foreach (var key in new[]
    {
        "SpiritBinderArchetype.Name", "SpiritBinderArchetype.Description",
        "SpiritBinderBonusFeat.Name", "SpiritBinderBonusFeat.Description"
    })
    {
        Check(literals.Contains(key), "installer localizes " + key);
        Check(shipped.Contains(key), "shipped localization defines " + key);
    }

    var calls = Calls(configure).ToArray();
    Check(Count(calls, "FeatureConfigurator", "New") == 1, "exactly one marker feature is created");
    Check(Count(calls, "FeatureSelectionConfigurator", "New") == 1, "exactly one merged bonus feat selection is created");
    Check(Count(calls, "ArchetypeConfigurator", "New") == 1, "exactly one archetype is created");
    Check(Count(calls, "ArchetypeConfigurator", "SetAddFeatures") == 1, "the archetype publishes one feature schedule");
    Check(Count(calls, "ArchetypeConfigurator", "SetRemoveFeatures") == 0,
        "the archetype removes nothing: the replaced tabletop abilities are not implemented here");

    var fields = Fields(configure).ToArray();
    Check(fields.Count(f => f.DeclaringType.Name == "FeatureSelectionRefs" && f.Name == "FighterFeatSelection") == 1,
        "merged pool includes the native fighter bonus feats");
    Check(fields.Count(f => f.DeclaringType.Name == "FeatureSelectionRefs" && f.Name == "WizardFeatSelection") == 1,
        "merged pool includes the native wizard bonus feats");
    Check(calls.Count(c => c.Name == "AddToAllFeatures") == 1, "the merged pool is filled once");
    Check(calls.Count(c => c.Name == "Cast") == 2, "both native pools are nested instead of copied");

    var il = configure.Body.Instructions;
    var levels = new List<int>();
    for (int i = 0; i < il.Count; i++)
        if (il[i].Operand is MethodReference call && call.Name == "AddEntry") levels.Add(LevelAt(il, i));
    Check(levels.SequenceEqual(new[] { 1, 2, 3, 5, 7, 9, 11 }),
        "level schedule is the marker at 1 and a bonus feat at 2/3/5/7/9/11, found " + string.Join(",", levels));

    var rules = mod.MainModule.GetType("MediumClass.Medium.MediumInfluenceRules");
    Check(Count(Calls(rules.Methods.Single(m => m.Name == "Cap")), "SpiritBinder", "HasFeature") == 1,
        "the influence cap reads the binder marker");
    Check(Count(Calls(rules.Methods.Single(m => m.Name == "PenaltyApplies")), "SpiritBinder", "HasFeature") == 1,
        "the penalty rule reads the binder marker");
    Check(Calls(rules.Methods.Single(m => m.Name == "RefreshPenalty")).Any(m => m.Name == "PenaltyApplies"),
        "refreshing the debuff uses the single penalty rule");
    var applies = Calls(rules.Methods.Single(m => m.Name == "PenaltyApplies")).ToArray();
    Check(applies.Any(m => m.Name == "HasSpirit"), "the permanent penalty still requires a channeled spirit");
    Check(applies.Any(m => m.Name == "Amount"), "an ordinary medium still needs the influence threshold");

    var channel = mod.MainModule.GetType("MediumClass.Medium.NewActions.ContextActionApplySpirit")
        .Methods.Single(m => m.Name == "RunAction");
    Check(Calls(channel).Count(m => m.Name == "HandleInfluencePenalty") == 2,
        "both channel branches refresh the penalty, so a binder's first channel is penalized immediately");

    Check(mod.MainModule.GetType("MediumClass.Medium.NewUnitParts.UnitPartMedium")
        .Methods.All(m => m.Name != "IsInfluencePenalty"),
        "no second copy of the influence threshold survives on the medium unit part");

    var initializer = mod.MainModule.GetType("MediumClass.Main")?.NestedTypes
        .SingleOrDefault(t => t.Name == "BlueprintCacheInitHandler");
    var classes = initializer?.Methods.SingleOrDefault(m => m.Name == "ConfigureClasses");
    Check(classes != null, "the archetype is created during blueprint class initialization");
    if (classes != null)
    {
        var order = Calls(classes).Select(m => m.DeclaringType.Name).ToArray();
        Check(Array.IndexOf(order, "MediumClass") >= 0 && Array.IndexOf(order, "MediumClass") < Array.IndexOf(order, "SpiritBinder"),
            "the archetype is created after the medium class it attaches to");
    }
}

Report();
return failures.Count == 0 ? 0 : 1;

void Report()
{
    Console.WriteLine($"Spirit binder: {checks - failures.Count}/{checks} checks passed against the installer DLL, the installed game assembly and the shipped localization; no Unity gameplay.");
    foreach (var failure in failures) Console.Error.WriteLine(failure);
}
