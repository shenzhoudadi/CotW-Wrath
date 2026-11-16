using System.Text.Json;
using System.Text.RegularExpressions;
using Mono.Cecil;

// Static audit of the 3rd-level Spirit Affinity implementation. It checks the three things that
// cannot be caught by the progression model test: blueprint GUID hygiene, the shipped localisation
// keys, and that every native component the implementation leans on still has the shape it is used
// with (the game is patched, and a renamed field would silently do nothing).
//
// Usage: SpiritAffinity <game Assembly-CSharp.dll> [built MediumClass.dll]
int checks = 0;
var failures = new List<string>();
void Check(bool ok, string label) { checks++; if (!ok) failures.Add(label); }

string gamePath = args.Length > 0 ? args[0] : null;
string modPath = args.Length > 1 ? args[1] : null;
string root = AppContext.BaseDirectory;
while (root != null && !File.Exists(Path.Combine(root, "MediumClass.csproj"))) root = Path.GetDirectoryName(root);
if (root == null) { Console.Error.WriteLine("FAIL: cannot locate the module directory."); return 1; }

// ---------------------------------------------------------------------------------------------------
// 1. Blueprint GUIDs: every Spirit Affinity id is a real, unique GUID.
// ---------------------------------------------------------------------------------------------------
var guidsSource = File.ReadAllText(Path.Combine(root, "Utilities", "Guids.cs"));
var declared = Regex.Matches(guidsSource, "internal const string (\\w+) = \"([0-9a-fA-F-]+)\";")
    .Select(m => (Name: m.Groups[1].Value, Value: m.Groups[2].Value)).ToList();
var affinity = declared.Where(d => d.Name.StartsWith("MediumSpiritAffinity")).ToList();
Check(affinity.Count >= 18, $"Guids.cs declares the Spirit Affinity ids (found {affinity.Count})");
foreach (var entry in affinity)
    Check(Guid.TryParse(entry.Value, out _), $"{entry.Name} is a parseable GUID ({entry.Value})");
var duplicates = declared.GroupBy(d => d.Value).Where(g => g.Count() > 1).Select(g => string.Join("/", g.Select(x => x.Name))).ToList();
Check(duplicates.Count == 0, "no two Guids.cs constants share a value: " + string.Join(", ", duplicates));

// The native anchors the implementation reuses must still point at the blueprints they were read from.
foreach (var (name, expected) in new[]
{
    ("NativeTrapfinding", "dbb6b3bffe6db3547b31c3711653838e"),
    ("NativeRogueFamiliarSelection", "363cab72f77c47745bf3a8807074d183"),
})
{
    var found = declared.FirstOrDefault(d => d.Name == name);
    Check(found.Value == expected, $"Guids.cs {name} is the blueprint the implementation was written against");
}

// ---------------------------------------------------------------------------------------------------
// 2. Localisation: every key used by the implementation is shipped in both languages, and no
//    Spirit Affinity key is left unused.
// ---------------------------------------------------------------------------------------------------
var used = new HashSet<string>();
var optionalFile = Path.Combine(root, "Medium", "SpiritAffinityOptional.cs");
foreach (var file in Directory.GetFiles(Path.Combine(root, "Medium"), "SpiritAffinity*.cs").Where(f => f != optionalFile))
    foreach (Match m in Regex.Matches(File.ReadAllText(file), "\"(SpiritAffinity\\.[A-Za-z.]+)\""))
    {
        var key = m.Groups[1].Value;
        if (key == "SpiritAffinity.LegendarySkill.")
        {
            foreach (var skill in new[] {
                "SkillAthletics", "SkillMobility", "SkillThievery", "SkillStealth",
                "SkillKnowledgeArcana", "SkillKnowledgeWorld", "SkillLoreNature", "SkillLoreReligion",
                "SkillPerception", "SkillPersuasion", "SkillUseMagicDevice" })
                used.Add(key + skill + ".Name");
            continue;
        }
        used.Add(key);
    }
// The optional file builds "<key>.Name" / "<key>.Description" from a base key.
foreach (Match m in Regex.Matches(File.ReadAllText(optionalFile), "\"(SpiritAffinity\\.[A-Za-z.]+)\""))
{
    var key = m.Groups[1].Value;
    if (key.EndsWith(".Name") || key.EndsWith(".Description")) { used.Add(key); continue; }
    used.Add(key + ".Name");
    used.Add(key + ".Description");
}
Check(used.Count > 0, "the implementation references Spirit Affinity text keys");

var pack = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Strings", "Feats.json"))).RootElement;
var shipped = new Dictionary<string, (string En, string Zh)>();
foreach (var entry in pack.EnumerateArray())
    shipped[entry.GetProperty("Key").GetString()] =
        (entry.GetProperty("enGB").GetString(), entry.GetProperty("zhCN").GetString());
foreach (var key in used.OrderBy(k => k))
{
    Check(shipped.ContainsKey(key), $"Feats.json ships {key}");
    if (!shipped.TryGetValue(key, out var text)) continue;
    Check(!string.IsNullOrWhiteSpace(text.En), $"{key} has English text");
    Check(!string.IsNullOrWhiteSpace(text.Zh), $"{key} has Chinese text");
    Check(text.Zh.Any(c => c > 0x7F), $"{key} Chinese text is actually translated");
}
var unused = shipped.Keys.Where(k => k.StartsWith("SpiritAffinity.") && !used.Contains(k)).ToList();
Check(unused.Count == 0, "no orphaned Spirit Affinity text keys: " + string.Join(", ", unused));

// ---------------------------------------------------------------------------------------------------
// 3. Native anchors: the component fields and helper methods the implementation depends on.
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
    bool HasMethod(string type, string method, int parameters) =>
        Type(type)?.Methods.Any(m => m.Name == method && m.Parameters.Count == parameters) == true;
    string FieldType(string type, string field) => Type(type)?.Fields.FirstOrDefault(f => f.Name == field)?.FieldType.FullName;

    // Aligned Weaponry / Spirit Avatar.
    const string damageProperty = "Kingmaker.UnitLogic.FactLogic.AddOutgoingPhysicalDamageProperty";
    foreach (var field in new[] { "AddAlignment", "MyAlignment", "AddReality", "Reality" })
        Check(HasField(damageProperty, field), $"{damageProperty}.{field} still exists (aligned weaponry / ghost touch)");
    Check(FieldType(damageProperty, "Reality") == "Kingmaker.Enums.Damage.DamageRealityType",
        "AddOutgoingPhysicalDamageProperty.Reality is still the reality enum");

    // Spiritsense.
    const string blindsense = "Kingmaker.Designers.Mechanics.Facts.Blindsense";
    foreach (var field in new[] { "Range", "Blindsight" })
        Check(HasField(blindsense, field), $"{blindsense}.{field} still exists");

    // Uncanny Dodge.
    const string flatFooted = "Kingmaker.Designers.Mechanics.Facts.FlatFootedIgnore";
    Check(FieldType(flatFooted, "Type") == "Kingmaker.Designers.Mechanics.Facts.FlatFootedIgnoreType",
        "FlatFootedIgnore.Type is still the flat-footed enum");
    Check(module.GetType("Kingmaker.Designers.Mechanics.Facts.FlatFootedIgnoreType")?.Fields.Any(f => f.Name == "UncannyDodge") == true,
        "FlatFootedIgnoreType.UncannyDodge still exists");

    // Spirit Warding.
    const string savingThrow = "Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor";
    foreach (var field in new[] { "SpellDescriptor", "Value", "ModifierDescriptor" })
        Check(HasField(savingThrow, field), $"{savingThrow}.{field} still exists");
    Check(FieldType("Kingmaker.UnitLogic.FactLogic.AddEnergyImmunity", "Type") != null, "AddEnergyImmunity.Type still exists");
    Check(Type("Kingmaker.UnitLogic.FactLogic.AddImmunityToEnergyDrain") != null, "AddImmunityToEnergyDrain still exists");
    Check(Type("Kingmaker.UnitLogic.FactLogic.SpellImmunityToSpellDescriptor") != null, "SpellImmunityToSpellDescriptor still exists");

    // Spiritual Defense.
    const string acStat = "Kingmaker.EntitySystem.Stats.ModifiableValueArmorClass";
    foreach (var field in new[] { "FilterAllowedForTouch", "FilterAllowedForFlatFootedTouch" })
        Check(HasField(acStat, field), $"{acStat}.{field} still exists");
    foreach (var method in new[] { "OnUpdate", "ApplyModifiersFiltered", "GetModifiers", "AddModifierUnique", "RemoveModifiersFrom" })
        Check(Type(acStat)?.Methods.Any(m => m.Name == method) == true || Type("Kingmaker.EntitySystem.Stats.ModifiableValue")?.Methods.Any(m => m.Name == method) == true,
            $"ModifiableValueArmorClass.{method} still exists");
    Check(Type("Kingmaker.UI.Common.StatModifiersBreakdown")?.Methods.Any(m => m.Name == "GetDisplayArmorModifiers") == true,
        "StatModifiersBreakdown.GetDisplayArmorModifiers still exists");
    const string restrictions = "Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.RestrictionsHelper";
    foreach (var method in new[] { "CheckHasArmor", "CheckHasShield" })
        Check(HasMethod(restrictions, method, 1), $"RestrictionsHelper.{method} still exists");
    Check(Type(restrictions)?.Methods.Any(m => m.Name == "CheckArmor" && m.Parameters.Count == 2
            && m.Parameters[1].ParameterType.FullName == "Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup") == true,
        "RestrictionsHelper.CheckArmor(unit, ArmorProficiencyGroup) still exists");

    // Spirit Warding: negative energy channeling is a real descriptor, so no ability list is needed.
    Check(module.GetType("Kingmaker.Blueprints.Classes.Spells.SpellDescriptor")?.Fields.Any(f => f.Name == "ChannelNegativeHarm") == true,
        "SpellDescriptor.ChannelNegativeHarm still exists (spirit warding covers channeled negative energy)");
    Check(FieldType(savingThrow, "SpellDescriptor") != null, "SavingThrowBonusAgainstDescriptor still filters on descriptors");
    Check(Type("Kingmaker.UnitLogic.FactLogic.BuffDescriptorImmunity") != null, "BuffDescriptorImmunity still exists");

    // Uncanny Dodge: flank immunity is the native Improved Uncanny Dodge mechanics flag.
    var mechanics = Type("Kingmaker.UnitLogic.FactLogic.AddMechanicsFeature");
    Check(mechanics?.NestedTypes.Any(t => t.Name == "MechanicsFeatureType"
            && t.Fields.Any(f => f.Name == "CannotBeFlanked")) == true,
        "AddMechanicsFeature.MechanicsFeatureType.CannotBeFlanked still exists (flank immunity)");

    // Advanced Armor / Weapon Training: UnitPartWeaponTraining, prerequisites and PropertyValueGetter anchors.
    const string unitPartWt = "Kingmaker.UnitLogic.Parts.UnitPartWeaponTraining";
    Check(Type(unitPartWt)?.Methods.Any(m => m.Name == "GetWeaponRank" && m.Parameters.Count == 1
            && m.Parameters[0].ParameterType.FullName == "Kingmaker.Items.ItemEntityWeapon") == true,
        "UnitPartWeaponTraining.GetWeaponRank(ItemEntityWeapon) still exists");
    Check(HasMethod(unitPartWt, "GetMaxWeaponRank", 0), "UnitPartWeaponTraining.GetMaxWeaponRank() still exists");
    Check(Type("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature")?.Methods.Any(m => m.Name == "CheckInternal") == true,
        "PrerequisiteFeature.CheckInternal still exists");
    Check(Type("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeaturesFromList")?.Methods.Any(m => m.Name == "CheckInternal") == true,
        "PrerequisiteFeaturesFromList.CheckInternal still exists");
    Check(Type("Kingmaker.UnitLogic.Mechanics.Properties.PropertyValueGetter")?.Methods.Any(m => m.Name == "GetBaseValue") == true,
        "PropertyValueGetter.GetBaseValue still exists");
    Check(Type("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig")?.Methods.Any(m => m.Name == "GetBaseValue") == true,
        "ContextRankConfig.GetBaseValue still exists");
    Check(Type("Kingmaker.GameStarter")?.Methods.Any(m => m.Name == "FixTMPAssets") == true,
        "GameStarter.FixTMPAssets still exists (late pool sync)");
    Check(Type("Kingmaker.Localization.LocalizationManager")?.Methods.Any(m => m.Name == "WaitForInit") == true,
        "LocalizationManager.WaitForInit still exists (post-ReconcileOptional sync)");
    Check(Type("Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection")?.Methods.Any(m => m.Name == "ExtractSelectionItems") == true,
        "BlueprintFeatureSelection.ExtractSelectionItems still exists (UI open pool sync)");
    Check(Type("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel")?.Methods.Any(m => m.Name == "CheckInternal") == true,
        "PrerequisiteClassLevel.CheckInternal still exists");

    // Arcane Discovery / Armor / Weapon Training: the class substitution must be consumed by
    // PrerequisiteClassLevel, otherwise the copied options would stay locked behind wizard/fighter levels.
    const string classLevels = "Kingmaker.Designers.Mechanics.Facts.ClassLevelsForPrerequisites";
    foreach (var field in new[] { "m_FakeClass", "m_ActualClass", "m_ForSelection", "Modifier", "Summand" })
        Check(HasField(classLevels, field), $"{classLevels}.{field} still exists");
    var prerequisite = Type("Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel");
    var getClassLevel = prerequisite?.Methods.FirstOrDefault(m => m.Name == "GetClassLevel");
    var factSelection = getClassLevel?.Body.Instructions
        .Select(i => i.Operand as GenericInstanceMethod)
        .FirstOrDefault(g => g?.ElementMethod?.Name == "SelectFactComponents");
    Check(factSelection?.GenericArguments.Any(a => a.Name == "ClassLevelsForPrerequisites") == true,
        "PrerequisiteClassLevel.GetClassLevel still reads the unit's ClassLevelsForPrerequisites");
}

// ---------------------------------------------------------------------------------------------------
// 4. The built assembly really builds one selection that offers every entry.
// ---------------------------------------------------------------------------------------------------
if (modPath == null || !File.Exists(modPath))
{
    Console.Error.WriteLine("SKIP: no built module, wiring checks not run.");
}
else
{
    using var mod = AssemblyDefinition.ReadAssembly(modPath);
    var configure = mod.MainModule.GetType("MediumClass.Medium.SpiritAffinity")?
        .Methods.FirstOrDefault(m => m.Name == "ConfigureEnabled");
    Check(configure != null, "the built module contains SpiritAffinity.ConfigureEnabled");
    if (configure != null)
    {
        var calls = configure.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().ToList();
        Check(calls.Count(c => c.Name == "AddToAllFeatures") == 1, "every affinity entry goes into one selection pool");
        Check(calls.Any(c => c.Name == "SetRanks"), "the selection carries the number of picks");
        // Each entry builder is its own method; all of them must be called.
        var builders = mod.MainModule.GetType("MediumClass.Medium.SpiritAffinity")?
            .Methods.Where(m => m.Name != "ConfigureEnabled" && m.IsStatic && m.ReturnType.Name == "BlueprintFeature")
            .Select(m => m.Name).ToList() ?? new List<string>();
        var called = calls.Where(c => c.DeclaringType.Name == "SpiritAffinity").Select(c => c.Name).ToList();
        var missing = builders.Where(b => !called.Contains(b)).ToList();
        Check(missing.Count == 0, "every entry builder is invoked: missing " + string.Join(", ", missing));
        Console.WriteLine($"Spirit Affinity entry builders: {builders.Count}");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.MediumTrainingLevelGetter") != null,
            "the built module ships the medium training level getter");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.MediumTrainedWeaponGroupComponent") != null,
            "the built module ships the trained weapon group marker component");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.MediumAdvancedWeaponTrainingComponent") != null,
            "the built module ships the advanced weapon training component");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.PrerequisiteClassLevel_MediumAffinity_Patch") != null,
            "the built module ships the class-level prerequisite patch");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.BlueprintFeatureSelection_MediumWeaponGroupGate_Patch") != null,
            "the built module ships the weapon-group gate patch");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.AddFeatureOnClassLevel_ArcaneDiscovery_Patch") != null,
            "the built module ships the arcane discovery class-level scaling patch");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.BlueprintAbilityResource_ArcaneDiscovery_Patch") != null,
            "the built module ships the arcane discovery resource scaling patch");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.MediumSpiritualDefenseComponent") != null,
            "the built module ships the spiritual defense component");
        Check(mod.MainModule.GetType("MediumClass.Medium.NewComponents.AbilitySpecific.ModifiableValueArmorClass_SpiritualDefenseTouch_Patch") != null,
            "the built module ships the spiritual defense touch AC patch");
        Check(mod.MainModule.GetType("MediumClass.Medium.GameStarter_LatePoolSync_Patch") != null,
            "the built module ships the late pool sync patch");
        Check(mod.MainModule.GetType("MediumClass.Medium.LocalizationManager_LatePoolSync_Patch") != null,
            "the built module ships the localization-ready pool sync patch");
        Check(mod.MainModule.GetType("MediumClass.Medium.BlueprintFeatureSelection_LatePoolSync_Patch") != null,
            "the built module ships the UI-open pool sync patch");
    }
}

if (failures.Count > 0)
{
    foreach (var failure in failures) Console.Error.WriteLine("FAIL: " + failure);
    Console.Error.WriteLine($"{failures.Count} of {checks} checks failed.");
    return 1;
}
Console.WriteLine($"PASS: {checks} Spirit Affinity audits (GUID hygiene, shipped localisation, native anchors, build wiring); static inspection, not Unity gameplay.");
return 0;
