using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Class.Kineticist.Properties;
using Kingmaker.UnitLogic.Mechanics.Properties;

// Execute the shipped registration method against real game managed types in a
// standalone process. Seed only an in-memory cache; never initialize the game.
internal static class BlueprintTests
{
    private const string BasicId = "247a4068296e8be42890143f451b4b45";
    private const string IconId = "175d1577bb6c9a04baf88eec99c66334";
    private const string FeatId = "e7f2ef3960ee4a7e88d6fb773b0a6dcc";
    private const string ExtendedId = "f362a8fdff7146d5b2e614c39349718d";
    private const string BaneId = "7ddf7fbeecbe78342b83171d888028cf";
    private const string ResourceId = "a708945b17c56fa4196e8d20f8af1b0d";
    private const string ExtraBaneId = "756dc2f7340b0b34285a1dc367ff7359";
    private const string ImprovedCombatExpertiseId = "c01055f33991416bb1ecc332a578e7cd";
    private const string CombatExpertiseId = "4c44724ffa8844f4d9bedb5bb27d144a";
    private const string PenaltyPropertyId = "8a63b06d20838954e97eb444f805ec89";
    private const string BrawlerClassId = "1c44d137122540d5aed818fd7902625f";
    private const string BrawlerSelectionId = "93acb287d012446f9bc4f6203d3e57d6";
    private static readonly string[] CombatSelectionIds = {
        "66befe7b24c42dd458952e3c47c93563",
        "41c8486641f7d6d4283ca9dae4147a9f",
        "da03141df23f3fe45b0c7c323a8e5a0e",
        "79c6421dbdb028c4fa0c31b8eea95f16",
        "c5158a6622d0b694a99efb1d0025d2c1"
    };
    private static MethodInfo register;
    private static MethodInfo registerAll;
    private static string managedPath;
    private static string modPath;
    private static int assertions;

    private static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3) { Console.Error.WriteLine("Expected game Managed directory, prototype DLL, optional JSON result path."); return 2; }
        managedPath = Path.GetFullPath(args[0]);
        modPath = Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try {
            Run();
            if (args.Length == 3) File.WriteAllText(args[2], "{\"scenariosPassed\":24,\"assertionsPassed\":" + assertions + "}");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        var filename = new AssemblyName(args.Name).Name + ".dll";
        foreach (var directory in new[] { managedPath, Path.GetDirectoryName(modPath) })
        {
            var path = Path.Combine(directory, filename);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        register = Assembly.LoadFrom(modPath).GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration")
            .GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic);
        registerAll = register.DeclaringType.GetMethod("RegisterAll", BindingFlags.Static | BindingFlags.NonPublic);

        // 1: Preserve different pre-existing entries in both real selection arrays.
        var cache = Cache(out var selection);
        var first = Reference("93147d5ed4624ee9b2f28cf1f88e1765");
        var second = Reference("93147d5ed4624ee9b2f28cf1f88e1766");
        Set(selection, "m_Features", new[] { first });
        Set(selection, "m_AllFeatures", new[] { first, second });
        Register(cache);
        var feature = cache.Load(BlueprintGuid.Parse(FeatId)) as BlueprintFeature;
        Assert(feature != null, "Created feat");
        Assert(feature.Ranks == 1 && feature.Groups.SequenceEqual(new[] { FeatureGroup.Feat }), "Normal single-rank feat");
        Assert(feature.ComponentsArray.Length == 1 && ReferenceEquals(feature.ComponentsArray[0].OwnerBlueprint, feature), "Component owner initialized");
        Assert(Get(selection, "m_Features").Length == 2 && ReferenceEquals(Get(selection, "m_Features")[0], first), "Visible entries preserved");
        Assert(Get(selection, "m_AllFeatures").Length == 3 && ReferenceEquals(Get(selection, "m_AllFeatures")[1], second), "All entries preserved independently");
        Assert(Get(selection, "m_AllFeatures").Last().Guid == feature.AssetGuid, "Typed reference uses correct GUID");
        var display = (LocalizedString)typeof(BlueprintUnitFact).GetField("m_DisplayName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feature);
        Assert(display.Key == "650aef18-9d51-4af7-9609-23e0e53c5dd9", "Localization key wired");

        // 2: Repeating the actual registration does not duplicate either array or blueprint.
        Register(cache);
        Assert(ReferenceEquals(cache.Load(feature.AssetGuid), feature), "Blueprint identity preserved on repeat");
        Assert(Get(selection, "m_Features").Length == 2 && Get(selection, "m_AllFeatures").Length == 3, "No duplicate on repeat");

        // 3: An independently reset list is repaired without overwriting the other list.
        Set(selection, "m_Features", new[] { first });
        Register(cache);
        Assert(Get(selection, "m_Features").Length == 2 && Get(selection, "m_AllFeatures").Length == 3, "Independent list repair");

        // 4: Missing basic selection must fail before adding anything to the cache.
        var empty = new BlueprintsCache();
        ExpectFailure(empty);
        Assert(empty.Load(BlueprintGuid.Parse(FeatId)) == null, "No partial asset without selection");

        // 5: Missing icon dependency must preserve the original selection arrays.
        var missingIcon = Cache(out var untouched);
        missingIcon.RemoveCachedBlueprint(BlueprintGuid.Parse(IconId));
        var before = Get(untouched, "m_AllFeatures");
        ExpectFailure(missingIcon);
        Assert(missingIcon.Load(BlueprintGuid.Parse(FeatId)) == null, "No partial asset without icon dependency");
        Assert(ReferenceEquals(before, Get(untouched, "m_AllFeatures")), "No selection mutation on dependency failure");

        // 6: A colliding GUID is rejected without replacing its owner's asset.
        var collision = Cache(out var collisionSelection);
        var occupant = new BlueprintFeature { name = "AnotherModAsset", AssetGuid = BlueprintGuid.Parse(FeatId) };
        collision.AddCachedBlueprint(occupant.AssetGuid, occupant);
        ExpectFailure(collision);
        Assert(ReferenceEquals(collision.Load(occupant.AssetGuid), occupant), "GUID collision preserves existing asset");
        Assert(Get(collisionSelection, "m_AllFeatures").Length == 0, "GUID collision preserves selection");

        // 7: Null/empty serialized list fields are handled without a null dereference.
        var nullLists = Cache(out var nullSelection);
        Set(nullSelection, "m_Features", null);
        Set(nullSelection, "m_AllFeatures", null);
        Register(nullLists);
        Assert(Get(nullSelection, "m_Features").Length == 1 && Get(nullSelection, "m_AllFeatures").Length == 1, "Null lists initialized");

        // 8: All modules append normally; prerequisites and resource references are explicit.
        var both = FullCache(out var bothSelection);
        var reports = RegisterAll(both);
        Assert(reports.SequenceEqual(new[] { true, true, true }), "Original three feat modules registered");
        var extended = (BlueprintFeature)both.Load(BlueprintGuid.Parse(ExtendedId));
        Assert(Get(bothSelection, "m_AllFeatures").Select(r => r.Guid.ToString()).SequenceEqual(new[] { FeatId, ExtendedId, ImprovedCombatExpertiseId }), "Three distinct feats in normal pool");
        Assert(extended.Ranks == 1 && extended.Groups.SequenceEqual(new[] { FeatureGroup.Feat }), "Extended Bane is single-rank normal feat");
        Assert(extended.ComponentsArray.All(c => ReferenceEquals(c.OwnerBlueprint, extended)), "All added component owners initialized");
        var prerequisite = extended.ComponentsArray.OfType<PrerequisiteFeature>().Single();
        var requirementRef = (BlueprintFeatureReference)typeof(PrerequisiteFeature).GetField("m_Feature", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(prerequisite);
        Assert(requirementRef.Guid.ToString() == BaneId, "Requires real Bane feature, not a hard-coded class level");
        var resourceComponent = extended.ComponentsArray.Single(c => c.GetType().Name == "ExtendedBaneComponent");
        var resourceReference = (BlueprintAbilityResourceReference)resourceComponent.GetType().GetField("Resource").GetValue(resourceComponent);
        Assert(resourceReference.Guid.ToString() == ResourceId, "Bonus targets only original Bane resource");
        var oldComponent = ((BlueprintFeature)both.Load(BlueprintGuid.Parse(FeatId))).ComponentsArray.Single();
        Assert(oldComponent.name == "SteadfastPersonalitySaveReplacement" && oldComponent.GetType().FullName == "CallOfTheWild.Wrath.Prototype.SteadfastPersonalityComponent", "0.1.1 component identity preserved");
        var improved = (BlueprintFeature)both.Load(BlueprintGuid.Parse(ImprovedCombatExpertiseId));
        Assert(improved.Ranks == 1 && improved.Groups.Contains(FeatureGroup.CombatFeat), "Improved Combat Expertise is a single-rank combat feat");
        var statRequirements = improved.ComponentsArray.OfType<PrerequisiteStatValue>().ToArray();
        Assert(statRequirements.Any(item => item.Stat == StatType.Intelligence && item.Value == 13) &&
            statRequirements.Any(item => item.Stat == StatType.BaseAttackBonus && item.Value == 8), "Intelligence and BAB prerequisites preserved");
        var combatRequirement = improved.ComponentsArray.OfType<PrerequisiteFeature>().Single();
        var combatRequirementRef = (BlueprintFeatureReference)typeof(PrerequisiteFeature).GetField("m_Feature", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combatRequirement);
        Assert(combatRequirementRef.Guid.ToString() == CombatExpertiseId, "Combat Expertise prerequisite preserved");
        Assert(PenaltyRefs(both).Count(item => item.Guid.ToString() == ImprovedCombatExpertiseId) == 2, "Native penalty property counts feat twice for -2");
        Assert(PenaltyRefs(both).Length == 5 &&
            PenaltyRefs(both).Take(3).Select(item => item.Guid.ToString()).SequenceEqual(new[] {
                "7444d4913a1b1be459bac3b12c6a2933", "7444d4913a1b1be459bac3b12c6a2933",
                "e01152417a8ac2248b4f69711b819441" }), "Native penalty entries stay in their original order");
        foreach (var id in CombatSelectionIds) {
            var combatSelection = (BlueprintFeatureSelection)both.Load(BlueprintGuid.Parse(id));
            Assert(Get(combatSelection, "m_Features").Single().Guid.ToString() == ImprovedCombatExpertiseId &&
                Get(combatSelection, "m_AllFeatures").Single().Guid.ToString() == ImprovedCombatExpertiseId,
                "Native combat selection receives feat in both lists: " + id);
        }

        // 9: Reinitializing the batch keeps the same instances and list size.
        RegisterAll(both);
        Assert(ReferenceEquals(extended, both.Load(extended.AssetGuid)) &&
            ReferenceEquals(improved, both.Load(improved.AssetGuid)) &&
            Get(bothSelection, "m_AllFeatures").Length == 3, "Batch repeat does not duplicate");
        Assert(PenaltyRefs(both).Count(item => item.Guid.ToString() == ImprovedCombatExpertiseId) == 2, "Penalty property remains exactly -2 on repeat");

        // 10: A missing resource disables only Extended Bane.
        var missingResource = FullCache(out var missingResourceSelection);
        missingResource.RemoveCachedBlueprint(BlueprintGuid.Parse(ResourceId));
        Assert(RegisterAll(missingResource).SequenceEqual(new[] { true, false, true }), "Missing resource dependency isolated");
        Assert(missingResource.Load(BlueprintGuid.Parse(FeatId)) != null && missingResource.Load(BlueprintGuid.Parse(ExtendedId)) == null, "Existing-save blueprint survives resource failure");
        Assert(Get(missingResourceSelection, "m_AllFeatures").Length == 2, "No partial Extended Bane entry");

        // 11: The reverse failure is also isolated.
        var missingOldIcon = FullCache(out var missingOldSelection);
        missingOldIcon.RemoveCachedBlueprint(BlueprintGuid.Parse(IconId));
        Assert(RegisterAll(missingOldIcon).SequenceEqual(new[] { false, true, true }), "New modules survive old-module dependency failure");
        Assert(missingOldIcon.Load(BlueprintGuid.Parse(ExtendedId)) != null && Get(missingOldSelection, "m_AllFeatures").Length == 2, "Successful modules remain usable");

        // 12: New GUID collision does not replace another asset or roll back the old module.
        var newCollision = FullCache(out var newCollisionSelection);
        var occupied = new BlueprintFeature { name = "OtherModFeat", AssetGuid = BlueprintGuid.Parse(ExtendedId) };
        newCollision.AddCachedBlueprint(occupied.AssetGuid, occupied);
        Assert(RegisterAll(newCollision).SequenceEqual(new[] { true, false, true }), "Extended Bane GUID collision isolated");
        Assert(ReferenceEquals(occupied, newCollision.Load(occupied.AssetGuid)) && Get(newCollisionSelection, "m_AllFeatures").Length == 2, "Collision preserves owner and other modules");

        // 13: All prerequisites are resolved before the new module mutates the cache.
        var missingBane = FullCache(out var missingBaneSelection);
        missingBane.RemoveCachedBlueprint(BlueprintGuid.Parse(BaneId));
        Assert(RegisterAll(missingBane).SequenceEqual(new[] { true, false, true }), "Missing Bane prerequisite fails closed");
        Assert(missingBane.Load(BlueprintGuid.Parse(ExtendedId)) == null && Get(missingBaneSelection, "m_AllFeatures").Length == 2, "No orphan Extended Bane blueprint without prerequisite");

        // 14: Batch registration preserves unrelated entries independently in both arrays.
        var mixed = FullCache(out var mixedSelection);
        Set(mixedSelection, "m_Features", new[] { first });
        Set(mixedSelection, "m_AllFeatures", new[] { second, first });
        RegisterAll(mixed);
        Assert(Get(mixedSelection, "m_Features").Length == 4 && ReferenceEquals(Get(mixedSelection, "m_Features")[0], first), "Batch preserves visible entries");
        Assert(Get(mixedSelection, "m_AllFeatures").Length == 5 && ReferenceEquals(Get(mixedSelection, "m_AllFeatures")[0], second), "Batch preserves all entries");

        // 15: Missing native penalty property affects only the new combat feat.
        var missingProperty = FullCache(out var missingPropertySelection);
        missingProperty.RemoveCachedBlueprint(BlueprintGuid.Parse(PenaltyPropertyId));
        Assert(RegisterAll(missingProperty).SequenceEqual(new[] { true, true, false }), "Missing penalty property isolated");
        Assert(Get(missingPropertySelection, "m_AllFeatures").Length == 2 && missingProperty.Load(BlueprintGuid.Parse(ImprovedCombatExpertiseId)) == null, "No orphan combat feat without penalty property");

        // 16: A different native penalty formula must fail closed.
        var differentFormula = FullCache(out var differentFormulaSelection);
        PenaltyComponent(differentFormula).FeatureBonus = -2;
        Assert(RegisterAll(differentFormula).SequenceEqual(new[] { true, true, false }), "Changed native penalty formula rejected");
        Assert(Get(differentFormulaSelection, "m_AllFeatures").Length == 2 && PenaltyRefs(differentFormula).Length == 3, "Changed formula leaves selections and property intact");

        // 17: A GUID collision rolls back the property as well as all selections.
        var combatCollision = FullCache(out var combatCollisionSelection);
        var combatOccupant = new BlueprintFeature { name = "OtherModFeat", AssetGuid = BlueprintGuid.Parse(ImprovedCombatExpertiseId) };
        combatCollision.AddCachedBlueprint(combatOccupant.AssetGuid, combatOccupant);
        Assert(RegisterAll(combatCollision).SequenceEqual(new[] { true, true, false }), "Combat feat GUID collision isolated");
        Assert(ReferenceEquals(combatOccupant, combatCollision.Load(combatOccupant.AssetGuid)) &&
            Get(combatCollisionSelection, "m_AllFeatures").Length == 2 &&
            PenaltyRefs(combatCollision).Length == 3, "Combat collision preserves owner and native property");

        // 18: Missing secondary selection cannot leave a feat in the other pools.
        var missingSelection = FullCache(out var missingSelectionBasic);
        missingSelection.RemoveCachedBlueprint(BlueprintGuid.Parse(CombatSelectionIds[1]));
        Assert(RegisterAll(missingSelection).SequenceEqual(new[] { true, true, false }), "Missing combat selection isolated");
        Assert(Get(missingSelectionBasic, "m_AllFeatures").Length == 2 &&
            missingSelection.Load(BlueprintGuid.Parse(ImprovedCombatExpertiseId)) == null &&
            PenaltyRefs(missingSelection).Length == 3, "Missing selection leaves no partial combat registration");

        // 19: The independent Brawler port has its own combat feat list. Add the
        // feat there without copying the class or replacing another mod's entries.
        var brawlerCache = FullCache(out _);
        var brawlerClass = new BlueprintCharacterClass { name = "RMBrawlerHybridClass", AssetGuid = BlueprintGuid.Parse(BrawlerClassId) };
        var brawlerSelection = new BlueprintFeatureSelection { name = "RMBrawlerCombatFeat", AssetGuid = BlueprintGuid.Parse(BrawlerSelectionId) };
        brawlerCache.AddCachedBlueprint(brawlerClass.AssetGuid, brawlerClass);
        brawlerCache.AddCachedBlueprint(brawlerSelection.AssetGuid, brawlerSelection);
        CompleteBrawlerProgression(brawlerCache, brawlerClass, brawlerSelection);
        Set(brawlerSelection, "m_Features", new[] { first });
        Set(brawlerSelection, "m_AllFeatures", new[] { second });
        Assert(RegisterAll(brawlerCache).SequenceEqual(new[] { true, true, true, true }), "Optional Brawler integration registered");
        Assert(Get(brawlerSelection, "m_Features").Length == 2 && ReferenceEquals(Get(brawlerSelection, "m_Features")[0], first), "Brawler visible list preserves earlier feats");
        Assert(Get(brawlerSelection, "m_AllFeatures").Length == 2 && ReferenceEquals(Get(brawlerSelection, "m_AllFeatures")[0], second), "Brawler full list preserves earlier feats");
        Assert(Get(brawlerSelection, "m_AllFeatures").Last().Guid.ToString() == ImprovedCombatExpertiseId, "Brawler bonus selection offers new combat feat");
        RegisterAll(brawlerCache);
        Assert(Get(brawlerSelection, "m_Features").Length == 2 && Get(brawlerSelection, "m_AllFeatures").Length == 2, "Brawler integration is idempotent");

        // 20: A partly initialized Brawler does not affect the core three feats.
        var partialBrawler = FullCache(out var partialBasic);
        var partialSelection = new BlueprintFeatureSelection { name = "RMBrawlerCombatFeat", AssetGuid = BlueprintGuid.Parse(BrawlerSelectionId) };
        partialBrawler.AddCachedBlueprint(partialSelection.AssetGuid, partialSelection);
        Assert(RegisterAll(partialBrawler).SequenceEqual(new[] { true, true, true, false }), "Incomplete optional Brawler registration is isolated");
        Assert(Get(partialBasic, "m_AllFeatures").Length == 3 && Get(partialSelection, "m_AllFeatures").Length == 0, "Incomplete Brawler leaves core and foreign selection intact");

        // 21: A foreign blueprint using the published GUID is never modified.
        var impostorCache = FullCache(out _);
        var impostorClass = new BlueprintCharacterClass { name = "OtherClass", AssetGuid = BlueprintGuid.Parse(BrawlerClassId) };
        var impostorSelection = new BlueprintFeatureSelection { name = "RMBrawlerCombatFeat", AssetGuid = BlueprintGuid.Parse(BrawlerSelectionId) };
        impostorCache.AddCachedBlueprint(impostorClass.AssetGuid, impostorClass);
        impostorCache.AddCachedBlueprint(impostorSelection.AssetGuid, impostorSelection);
        Assert(RegisterAll(impostorCache).SequenceEqual(new[] { true, true, true, false }), "Brawler class GUID collision rejected");
        Assert(Get(impostorSelection, "m_AllFeatures").Length == 0, "Foreign Brawler selection remains untouched");

        // 22: The other mod may copy fighter feats after we have already added
        // ours; the optional integration must not create a second choice.
        var copiedCache = FullCache(out _);
        var copiedClass = new BlueprintCharacterClass { name = "RMBrawlerHybridClass", AssetGuid = BlueprintGuid.Parse(BrawlerClassId) };
        var copiedSelection = new BlueprintFeatureSelection { name = "RMBrawlerCombatFeat", AssetGuid = BlueprintGuid.Parse(BrawlerSelectionId) };
        copiedCache.AddCachedBlueprint(copiedClass.AssetGuid, copiedClass);
        copiedCache.AddCachedBlueprint(copiedSelection.AssetGuid, copiedSelection);
        CompleteBrawlerProgression(copiedCache, copiedClass, copiedSelection);
        Set(copiedSelection, "m_Features", new[] { Reference(ImprovedCombatExpertiseId) });
        Set(copiedSelection, "m_AllFeatures", new[] { Reference(ImprovedCombatExpertiseId) });
        Assert(RegisterAll(copiedCache).SequenceEqual(new[] { true, true, true, true }), "Brawler copied-feat path registered");
        Assert(Get(copiedSelection, "m_Features").Length == 1 && Get(copiedSelection, "m_AllFeatures").Length == 1, "Copied Brawler feat remains single in both lists");

        // 23: A Brawler whose class and selection exist but whose progression
        // failed mid-registration must not be reported as a usable integration.
        var unfinishedCache = FullCache(out var unfinishedBasic);
        var unfinishedClass = new BlueprintCharacterClass { name = "RMBrawlerHybridClass", AssetGuid = BlueprintGuid.Parse(BrawlerClassId) };
        var unfinishedSelection = new BlueprintFeatureSelection { name = "RMBrawlerCombatFeat", AssetGuid = BlueprintGuid.Parse(BrawlerSelectionId) };
        unfinishedCache.AddCachedBlueprint(unfinishedClass.AssetGuid, unfinishedClass);
        unfinishedCache.AddCachedBlueprint(unfinishedSelection.AssetGuid, unfinishedSelection);
        Assert(RegisterAll(unfinishedCache).SequenceEqual(new[] { true, true, true, false }), "Unfinished Brawler progression rejected");
        Assert(Get(unfinishedSelection, "m_AllFeatures").Length == 0 && Get(unfinishedBasic, "m_AllFeatures").Length == 3,
            "Unfinished Brawler leaves core feats and foreign selection intact");
        // 24: A conflicting Exemplar asset must fail independently of all three feats.
        var incompatibleExemplar = FullCache(out var exemplarBasic);
        var foreignArchetype = new BlueprintArchetype { name = "ForeignArchetype", AssetGuid = BlueprintGuid.Parse("0864a8c5eae54557bb25b30ba5c46e66") };
        incompatibleExemplar.AddCachedBlueprint(foreignArchetype.AssetGuid, foreignArchetype);
        Assert(RegisterAll(incompatibleExemplar).SequenceEqual(new[] { true, true, true, false }), "Exemplar failure isolated from feat registration");
        Assert(Get(exemplarBasic, "m_AllFeatures").Length == 3, "Exemplar failure preserves all core feats");
        Console.WriteLine($"PASS: 24 managed blueprint scenarios, {assertions} assertions; real game types, seeded in-memory blueprints. No game launch/combat/save test.");
    }

    private static BlueprintsCache FullCache(out BlueprintFeatureSelection selection)
    {
        var cache = Cache(out selection);
        foreach (var id in new[] { BaneId, ExtraBaneId, CombatExpertiseId }) {
            var feature = new BlueprintFeature { name = "TestDependency", AssetGuid = BlueprintGuid.Parse(id) };
            cache.AddCachedBlueprint(feature.AssetGuid, feature);
        }
        foreach (var id in CombatSelectionIds) {
            var combatSelection = new BlueprintFeatureSelection { name = "NativeCombatSelection", AssetGuid = BlueprintGuid.Parse(id) };
            cache.AddCachedBlueprint(combatSelection.AssetGuid, combatSelection);
        }
        var penaltyComponent = new BaseAttackPropertyWithFeatureList {
            name = "CombatExpertisePenalty", BaseValue = 1, BaseAttackDiv = 4, BaseAttackZero = 0, FeatureBonus = -1
        };
        var penalty = new BlueprintUnitProperty {
            name = "CombatExpertiseAttackPenaltyProperty", AssetGuid = BlueprintGuid.Parse(PenaltyPropertyId),
            ComponentsArray = new BlueprintComponent[] { penaltyComponent }
        };
        penalty.OnEnable();
        typeof(BaseAttackPropertyWithFeatureList).GetField("m_Features", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(penaltyComponent, new[] {
                Reference("7444d4913a1b1be459bac3b12c6a2933"),
                Reference("7444d4913a1b1be459bac3b12c6a2933"),
                Reference("e01152417a8ac2248b4f69711b819441")
            });
        cache.AddCachedBlueprint(penalty.AssetGuid, penalty);
        var resource = new BlueprintAbilityResource { name = "TestBaneResource", AssetGuid = BlueprintGuid.Parse(ResourceId) };
        cache.AddCachedBlueprint(resource.AssetGuid, resource);
        return cache;
    }

    private static void CompleteBrawlerProgression(BlueprintsCache cache, BlueprintCharacterClass brawlerClass,
        BlueprintFeatureSelection combatSelection)
    {
        var entry = new LevelEntry { Level = 1 };
        entry.SetFeatures(new BlueprintFeatureBase[] { combatSelection });
        var progression = new BlueprintProgression {
            name = "RMBrawlerProgression", AssetGuid = BlueprintGuid.Parse("080095b7ce034e0eaec62d789f37333a"),
            LevelEntries = new[] { entry }
        };
        cache.AddCachedBlueprint(progression.AssetGuid, progression);
        typeof(BlueprintCharacterClass).GetField("m_Progression", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(brawlerClass, BlueprintReferenceBase.CreateTyped<BlueprintProgressionReference>(progression));
    }

    private static bool[] RegisterAll(BlueprintsCache cache)
    {
        var results = new System.Collections.Generic.List<bool>();
        // This cache intentionally seeds only the original three feats and
        // optional Brawler / Exemplar. New modules have native-data independent suites.
        // Select the subjects instead of excluding every future module by name.
        var subjects = new[] { "Steadfast Personality", "Extended Bane", "Improved Combat Expertise", "Brawler combat feat integration", "Exemplar Inspire Courage scaling" };
        Action<string, bool, Exception> report = (name, ok, exception) => { if (subjects.Contains(name)) results.Add(ok); };
        registerAll.Invoke(null, new object[] { cache, report });
        return results.ToArray();
    }

    private static BlueprintsCache Cache(out BlueprintFeatureSelection selection)
    {
        var cache = new BlueprintsCache();
        selection = new BlueprintFeatureSelection { name = "BasicFeatSelection", AssetGuid = BlueprintGuid.Parse(BasicId) };
        cache.AddCachedBlueprint(selection.AssetGuid, selection);
        var icon = new BlueprintFeature { name = "IronWill", AssetGuid = BlueprintGuid.Parse(IconId) };
        cache.AddCachedBlueprint(icon.AssetGuid, icon);
        return cache;
    }

    private static BlueprintFeatureReference Reference(string id)
    {
        var reference = new BlueprintFeatureReference();
        reference.ReadGuidFromJson(id);
        return reference;
    }
    private static BaseAttackPropertyWithFeatureList PenaltyComponent(BlueprintsCache cache)
        => ((BlueprintUnitProperty)cache.Load(BlueprintGuid.Parse(PenaltyPropertyId)))
            .ComponentsArray.OfType<BaseAttackPropertyWithFeatureList>().Single();
    private static BlueprintFeatureReference[] PenaltyRefs(BlueprintsCache cache)
        => (BlueprintFeatureReference[])typeof(BaseAttackPropertyWithFeatureList)
            .GetField("m_Features", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(PenaltyComponent(cache));
    private static FieldInfo Field(string name) => typeof(BlueprintFeatureSelection).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static BlueprintFeatureReference[] Get(BlueprintFeatureSelection selection, string name) => (BlueprintFeatureReference[])Field(name).GetValue(selection);
    private static void Set(BlueprintFeatureSelection selection, string name, BlueprintFeatureReference[] value) => Field(name).SetValue(selection, value);
    private static void Register(BlueprintsCache cache) => register.Invoke(null, new object[] { cache });
    private static void ExpectFailure(BlueprintsCache cache)
    {
        try { Register(cache); }
        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException) { assertions++; return; }
        throw new Exception("Expected dependency/collision failure.");
    }
    private static void Assert(bool passed, string message)
    {
        if (!passed) throw new Exception("FAILED: " + message);
        assertions++;
    }
}
