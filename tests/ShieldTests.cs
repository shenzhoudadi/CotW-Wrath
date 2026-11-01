using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;

internal static class ShieldLauncher
{
    private static int Main(string[] args)
    {
        if (args.Length != 3) return 2;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            foreach (var dir in new[] { args[0], Path.GetDirectoryName(args[1]) }) {
                var file = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try {
            typeof(ShieldLauncher).Assembly.GetType("ShieldTests").GetMethod("Run").Invoke(null, new object[] { args });
            return 0;
        } catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}

internal static class ShieldTests
{
    private const string Id = "ae0f6962109c4f46a97958fe8b11e5ac";
    private const string BrawlerClass = "1c44d137122540d5aed818fd7902625f";
    private const string BrawlerSelection = "93acb287d012446f9bc4f6203d3e57d6";
    private static readonly string[] Dependencies = {
        "48ac8db94d5de7645906c7d0ad3bcfbd", "ac57069b6bf8c904086171683992a92a",
        "3c380607706f209499d951b29d3c44f3", "8d6acd9ac87cf0149b7d92eb6ed105d2"
    };
    private static readonly string[] Pools = { "247a4068296e8be42890143f451b4b45",
        "66befe7b24c42dd458952e3c47c93563", "41c8486641f7d6d4283ca9dae4147a9f",
        "da03141df23f3fe45b0c7c323a8e5a0e", "79c6421dbdb028c4fa0c31b8eea95f16", "c5158a6622d0b694a99efb1d0025d2c1" };
    private static int assertions;
    private static Type registration, identity;
    public static void Run(string[] args)
    {
        var mod = Assembly.LoadFrom(args[1]);
        registration = mod.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration", true);
        identity = mod.GetType("CallOfTheWild.Wrath.Prototype.ContentIdentity", true);
        var cache = Cache();
        var results = Register(cache);
        var coreModules = new System.Collections.Generic.HashSet<string>(new[] { "Steadfast Personality", "Extended Bane", "Improved Combat Expertise", "Shielded Mage" });
        Check(coreModules.SetEquals(results.Where(p => p.Value).Select(p => p.Key)), "Only the four seeded core modules register when all other dependencies are unavailable");
        var catalogue = (System.Collections.IEnumerable)AccessTools.Field(mod.GetType("CallOfTheWild.Wrath.Prototype.ModuleCatalog", true), "Definitions").GetValue(null);
        var catalogueNames = new System.Collections.Generic.HashSet<string>(catalogue.Cast<object>().Select(d => (string)AccessTools.Field(d.GetType(), "Name").GetValue(d)));
        Check(results.Keys.All(catalogueNames.Contains), "Actual registration caller reports only names defined by the live module catalogue");
        var psychicFlag=AccessTools.Field(mod.GetType("CallOfTheWild.Wrath.Prototype.PsychicCasting"),"RuntimePatchesAvailable");
        psychicFlag.SetValue(null,false);
        try { var fallback=Register(cache); Check(fallback.Count(p=>p.Value)==4 && !fallback["Psychic casting prototype"] && !fallback["Psychic spells and disciplines"],"Psychic runtime failure is isolated from the older four core modules"); }
        finally { psychicFlag.SetValue(null,true); }
        var feat = (BlueprintFeature)cache.Load(BlueprintGuid.Parse(Id));
        var c = feat.ComponentsArray.OfType<ArcaneSpellFailureIncrease>().Single();
        Check(feat.Ranks == 1 && feat.Groups.Contains(FeatureGroup.CombatFeat), "Single-rank combat feat");
        Check(feat.ComponentsArray.All(x => ReferenceEquals(x.OwnerBlueprint, feat)), "Component owner links");
        var bab = feat.ComponentsArray.OfType<PrerequisiteStatValue>().Single();
        var fighter = feat.ComponentsArray.OfType<PrerequisiteClassLevel>().Single();
        var training = feat.ComponentsArray.OfType<PrerequisiteFeaturesFromList>().Single();
        Check(bab.Group == Prerequisite.GroupType.Any && bab.Value == 3 && bab.Stat == Kingmaker.EntitySystem.Stats.StatType.BaseAttackBonus,
            "BAB 3 alternative prerequisite");
        Check(fighter.Group == Prerequisite.GroupType.Any && fighter.Level == 1 &&
            ((BlueprintCharacterClassReference)AccessTools.Field(fighter.GetType(), "m_CharacterClass").GetValue(fighter)).Guid.ToString() == Dependencies[0], "Fighter 1 alternative prerequisite");
        Check(training.Group == Prerequisite.GroupType.All && training.Amount == 1 &&
            ((BlueprintFeatureReference[])AccessTools.Field(training.GetType(), "m_Features").GetValue(training)).Select(r => r.Guid.ToString()).SequenceEqual(Dependencies.Skip(1)), "Additional mandatory shield or training prerequisite");
        Register(cache);
        Check(ReferenceEquals(feat, cache.Load(feat.AssetGuid)), "Stable blueprint on repeat");
        foreach (var pool in Pools)
            foreach (var field in new[] { "m_Features", "m_AllFeatures" })
                Check(Refs(cache, pool, field).Count(r => r.Guid.ToString() == Id) == 1, "One entry in " + pool + "/" + field);

        foreach (var dependency in Dependencies.Concat(new[] { Pools[2] })) {
            var missing = Cache(); missing.RemoveCachedBlueprint(BlueprintGuid.Parse(dependency));
            var r = Register(missing);
            Check(!r["Shielded Mage"] && missing.Load(BlueprintGuid.Parse(Id)) == null, "Missing dependency fails without orphan: " + dependency);
            Check(!Refs(missing, Pools[0], "m_AllFeatures").Any(x => x.Guid.ToString() == Id), "Missing dependency leaves no partial selection");
            Check(r["Steadfast Personality"] && r["Extended Bane"], "Independent modules survive");
        }
        var changed = Cache(); changed.Load(BlueprintGuid.Parse(Dependencies[1])).name = "ForeignShieldFocus";
        Check(!Register(changed)["Shielded Mage"], "Wrong dependency identity rejected");
        var collision = Cache();
        var foreign = new BlueprintFeature { AssetGuid = BlueprintGuid.Parse(Id), name = "CotWWrathPrototypeShieldedMage", Ranks = 1 };
        collision.AddCachedBlueprint(foreign.AssetGuid, foreign);
        Check(!Register(collision)["Shielded Mage"] && ReferenceEquals(foreign, collision.Load(foreign.AssetGuid)), "Matching name with foreign layout preserved and rejected");

        // Provider appears after core registration; retry only optional modules.
        var cls = new BlueprintCharacterClass { name = "RMBrawlerHybridClass", AssetGuid = BlueprintGuid.Parse(BrawlerClass) };
        var poolBp = new BlueprintFeatureSelection { name = "RMBrawlerCombatFeat", AssetGuid = BlueprintGuid.Parse(BrawlerSelection) };
        cache.AddCachedBlueprint(cls.AssetGuid, cls); cache.AddCachedBlueprint(poolBp.AssetGuid, poolBp);
        var firstAttempt = Register(cache, "RegisterOptional");
        Check(!firstAttempt["Brawler combat feat integration"], "Incomplete provider is rejected");
        typeof(BlueprintTests).GetMethod("CompleteBrawlerProgression", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { cache, cls, poolBp });
        var unrelated = new BlueprintFeature { name = "ShieldedMageFeature", AssetGuid = BlueprintGuid.Parse("b655a2efbc7744898d3761f32e794fbd") };
        cache.AddCachedBlueprint(unrelated.AssetGuid, unrelated);
        AccessTools.Field(poolBp.GetType(), "m_AllFeatures").SetValue(poolBp, new[] { BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(unrelated) });
        Check(Register(cache, "RegisterOptional")["Brawler combat feat integration"], "Complete late provider recovers");
        Register(cache, "RegisterOptional");
        Check(Refs(cache, BrawlerSelection, "m_AllFeatures").Length == 3 && Refs(cache, BrawlerSelection, "m_Features").Length == 2, "Both combat feats added once, foreign entries preserved");
        var diagnostics = (string)identity.GetMethod("Describe", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { cache });
        Check(diagnostics.Contains("POTENTIAL OVERLAP") && diagnostics.Contains(unrelated.AssetGuid.ToString()) && diagnostics.Contains("presence is not gameplay verification"), "Overlap report is diagnostic, not a false compatibility certificate");

        var surviving = Cache();
        surviving.RemoveCachedBlueprint(BlueprintGuid.Parse("8a63b06d20838954e97eb444f805ec89"));
        var survivorResults = Register(surviving);
        Check(survivorResults["Shielded Mage"] && !survivorResults["Improved Combat Expertise"], "Shield feat survives unrelated combat formula failure");
        surviving.AddCachedBlueprint(cls.AssetGuid, cls); surviving.AddCachedBlueprint(poolBp.AssetGuid, poolBp);
        var progression = cache.Load(BlueprintGuid.Parse("080095b7ce034e0eaec62d789f37333a"));
        surviving.AddCachedBlueprint(progression.AssetGuid, progression);
        AccessTools.Field(poolBp.GetType(), "m_Features").SetValue(poolBp, Array.Empty<BlueprintFeatureReference>());
        AccessTools.Field(poolBp.GetType(), "m_AllFeatures").SetValue(poolBp, Array.Empty<BlueprintFeatureReference>());
        Check(Register(surviving, "RegisterOptional")["Brawler combat feat integration"] &&
            Refs(surviving, BrawlerSelection, "m_AllFeatures").Single().Guid.ToString() == Id, "Brawler receives surviving feat independently");

        // Exercise the actual game callback and actual game final calculation,
        // with seeded rank/equipment, not a second implementation of the formula.
        var fact = Empty<Feature>(); Set(fact, "<Rank>k__BackingField", 1);
        var runtime = c.CreateRuntimeFactComponent(); runtime.Setup(c); runtime.RestoreOwnerLink(fact);
        foreach (var pair in new[] { new[] { 0, 0 }, new[] { 5, 0 }, new[] { 15, 0 }, new[] { 20, 5 }, new[] { 50, 35 } }) {
            Check(Query(c, runtime, pair[0], null) == pair[1], "Native shield result " + pair[0]);
            Check(Query(c, runtime, pair[0], 35) == pair[1] + 35, "Body armor unchanged at shield " + pair[0]);
        }
        Check(Query(c, runtime, null, 35) == 35, "Unequipped shield provides no armor reduction");
        Check(Query(c, runtime, 50, 35, true) == 35, "Ignore-shield native rule preserved");
        Check(Query(c, runtime, 20, 35, false, false) == 55, "Without feat callback original failure restored");
        Check(Query(c, runtime, 20, 35, false, true, -5) == 35, "Another shield reduction composes with feat");
        File.WriteAllText(args[2], "{\"assertionsPassed\":" + assertions + ",\"scope\":\"Production registration, late-provider retry, collision diagnostics; real native shield callback and failure calculation with seeded equipment/rank; no Unity, live prerequisite UI, EventBus, respec or save test\"}");
        Console.WriteLine("PASS: " + assertions + " Shielded Mage / identity / coexistence-fixture assertions.");
    }
    private static BlueprintsCache Cache()
    {
        var cache = (BlueprintsCache)typeof(BlueprintTests).GetMethod("FullCache", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { null });
        var names = new[] { "FighterClass", "ShieldFocus", "ArmorTraining", "SteelbloodArmorTraining" };
        for (int i = 0; i < names.Length; i++) {
            SimpleBlueprint bp = i == 0 ? (SimpleBlueprint)new BlueprintCharacterClass() : new BlueprintFeature();
            bp.name = names[i]; bp.AssetGuid = BlueprintGuid.Parse(Dependencies[i]); cache.AddCachedBlueprint(bp.AssetGuid, bp);
        }
        return cache;
    }
    private static Dictionary<string, bool> Register(BlueprintsCache cache, string method = "RegisterAll")
    {
        var results = new Dictionary<string, bool>();
        Action<string, bool, Exception> report = (n, ok, e) => results[n] = ok;
        registration.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { cache, report });
        return results;
    }
    private static BlueprintFeatureReference[] Refs(BlueprintsCache cache, string id, string field)
        => (BlueprintFeatureReference[])AccessTools.Field(typeof(BlueprintFeatureSelection), field).GetValue(cache.Load(BlueprintGuid.Parse(id))) ?? Array.Empty<BlueprintFeatureReference>();
    private static int Query(ArcaneSpellFailureIncrease c, EntityFactComponent runtime, int? shield, int? armor, bool ignoreShield = false, bool apply = true, int otherBonus = 0)
    {
        var rule = Empty<RuleCalculateArcaneSpellFailureChance>();
        Set(rule, "Shield", Item(shield)); Set(rule, "Armor", Item(armor)); rule.IgnoreShield = ignoreShield;
        rule.AddShieldBonus(otherBonus);
        if (apply) using (runtime.RequestEventContext()) c.OnEventAboutToTrigger(rule);
        rule.OnTrigger(null); return rule.Result;
    }
    private static ItemEntityArmor Item(int? chance)
    {
        if (!chance.HasValue) return null;
        var type = new BlueprintArmorType(); Set(type, "m_ArcaneSpellFailureChance", chance.Value);
        var reference = BlueprintReferenceBase.CreateTyped<BlueprintArmorTypeReference>(type);
        typeof(BlueprintReferenceBase).GetProperty("Cached", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(reference, type, null);
        var blueprint = new BlueprintItemArmor(); Set(blueprint, "m_Type", reference);
        var item = Empty<ItemEntityArmor>(); Set(item, "m_Blueprint", blueprint); return item;
    }
    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); assertions++; }
}
