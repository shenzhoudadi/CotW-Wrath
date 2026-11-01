using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;

internal static class PerformanceLauncher
{
    private static int Main(string[] args)
    {
        if (args.Length != 4) return 2;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            foreach (var dir in new[] { args[0], Path.GetDirectoryName(args[1]), Path.GetDirectoryName(args[2]) }) {
                var p = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(p)) return Assembly.LoadFrom(p);
            }
            return null;
        };
        try { typeof(PerformanceLauncher).Assembly.GetType("PerformanceTests").GetMethod("Run").Invoke(null, new object[] { args }); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}

internal static class PerformanceTests
{
    private static int assertions;
    private static Type provider;
    private static MethodInfo register;
    public static void Run(string[] args)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
            Check(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(args[2]))).Replace("-", "") == "2822BD555D9F9387BA1261E9C19F124B1F8C313EED3C1E0BA35844D83CA7AFA1", "Test reference is the audited official Brawler 1.0.9 DLL");
        var brawlerDll = Assembly.LoadFrom(args[2]);
        provider = brawlerDll.GetType("BrawlerClassWrath.NewMechanics.IncreaseResourcesByClassWithArchetype", true);
        register = Assembly.LoadFrom(args[1]).GetType("CallOfTheWild.Wrath.Prototype.ExemplarPerformanceResource", true).GetMethod("Register", BindingFlags.NonPublic | BindingFlags.Static);
        var f = new Fixture();
        Check(f.Total(3, 0, 12, true) == 7, "Reproduce 3rd-level CHA12 seven rounds using actual 1.0.9 callback");
        Check(f.Total(7, 0, 12, true) == 15, "Uncorrected excess grows with Brawler levels");
        var oldComponents = f.ResourceFact.ComponentsArray;
        var oldClassRefs = f.Classes(); var oldArchetypeRefs = f.Archetypes();
        Register(f);
        Check(ReferenceEquals(oldComponents, f.ResourceFact.ComponentsArray), "All original resource handlers preserved");
        Check(f.Classes().Length == 2 && ReferenceEquals(oldClassRefs[0], f.Classes()[0]) && ReferenceEquals(oldClassRefs[1], f.Classes()[1]), "Bard and unrelated provider refs preserve identity/order");
        Check(f.Archetypes().Length == 1 && ReferenceEquals(oldArchetypeRefs[0], f.Archetypes()[0]), "Other archetype ref preserved");
        foreach (int level in new[] { 3, 4, 5, 7, 11, 15, 20 })
            foreach (int cha in new[] { 8, 10, 12, 18 })
                Check(f.Total(level, 0, cha, true) == level + (cha - 10) / 2, "Pure Exemplar rounds at L" + level + " CHA" + cha);
        Check(f.Total(3, 2, 12, true) == 8, "Bard2/Exemplar3 keeps both native Bard contributions and Exemplar net contribution");
        Check(f.Total(7, 2, 18, true) == 15, "Mixed progression does not lose Bard or CHA bonuses");
        Check(f.Total(0, 5, 12, false) == 13, "Pure Bard remains 2+2*Bard+CHA");
        Check(f.Total(3, 5, 12, false) == 13, "Ordinary Brawler adds no performance rounds");
        Check(f.Total(3, 0, 12, true, 6) == 10, "Unrelated +6 resource bonus retained");
        Check(f.Total(3, 0, 12, true, 0, 4) == 8, "Unrelated class contribution retained");
        Check(f.Total(3, 0, 12, true, 0, 4, false) == 4, "Retained native archetype filter still gates its class");
        var once = f.Classes(); Register(f); Register(f);
        Check(ReferenceEquals(once, f.Classes()) && f.Total(3, 0, 12, true) == 4, "Retry does not subtract again or replace arrays");
        // Simulate the provider reintroducing its original list on a later startup checkpoint.
        SetAmount(f.Resource, "m_Class", oldClassRefs); SetAmount(f.Resource, "m_Archetypes", oldArchetypeRefs);
        Register(f); Check(f.Total(3, 0, 12, true) == 4, "Reintroduced duplicate is reconciled once");
        var preCorrected = new Fixture();
        SetAmount(preCorrected.Resource, "m_Class", preCorrected.Classes().Take(2).ToArray());
        SetAmount(preCorrected.Resource, "m_Archetypes", preCorrected.Archetypes().Take(1).ToArray());
        Register(preCorrected); Check(preCorrected.Total(3, 0, 12, true) == 4, "Already correct provider remains correct");
        var emptyReference = new Fixture(); Set(emptyReference.Native, "m_Archetype", new BlueprintArchetypeReference());
        Register(emptyReference); Check(emptyReference.Total(3, 0, 12, true) == 4, "Empty native archetype reference is treated as absent");
        foreach (var test in new Action<Fixture>[] {
            x => Set(x.External, "base_value", -1),
            x => Set(x.External, "Resource", new BlueprintAbilityResource()),
            x => x.Native.BaseValue = 3,
            x => Set(x.Native, "m_Archetype", Ref<BlueprintArchetypeReference>(x.Exemplar)),
            x => SetAmount(x.Resource, "LevelIncrease", 2),
            x => SetAmount(x.Resource, "IncreasedByStat", true),
            x => x.ResourceFact.ComponentsArray = new BlueprintComponent[] { x.Native },
            x => x.ResourceFact.ComponentsArray = new BlueprintComponent[] { x.Native, x.External, x.External },
            x => x.Exemplar.name = "ForeignArchetype",
            x => SetAmount(x.Resource, "m_Class", x.Classes().Concat(new[] { x.Classes().Last() }).ToArray()),
            x => SetAmount(x.Resource, "m_Archetypes", x.Archetypes().Take(1).ToArray()),
            x => x.Cache.RemoveCachedBlueprint(x.Resource.AssetGuid)
        }) {
            var bad = new Fixture(); test(bad); var before = bad.Classes();
            bool rejected = false;
            try { Register(bad); } catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { rejected = true; }
            Check(rejected && ReferenceEquals(before, bad.Classes()), "Changed/partial schema fails without mutating the shared resource");
        }
        File.WriteAllText(args[3], "{\"assertionsPassed\":" + assertions + ",\"providerAssemblyVersion\":\"" + brawlerDll.GetName().Version +
            "\",\"scope\":\"Production blueprint correction, native GetMaxAmount and IncreaseResourcesByClass, actual Brawler 1.0.9 callback with seeded unit/class/CHA; callbacks invoked explicitly, no game startup/live EventBus subscription/rest/save/UI test\"}");
        Console.WriteLine("PASS: " + assertions + " performance resource assertions; actual Brawler release callback; no game startup/rest/save test.");
    }

    private sealed class Fixture
    {
        public readonly BlueprintsCache Cache = new BlueprintsCache();
        public readonly BlueprintCharacterClass Brawler = Class("1c44d137122540d5aed818fd7902625f", "RMBrawlerHybridClass");
        public readonly BlueprintCharacterClass Bard = Class("772c83a25e2268e448e841dcd548235f", "BardClass");
        public readonly BlueprintCharacterClass Other = Class("19a2d9e58d916d04db4cd7ad2c7a3ee2", "OtherNativeClass");
        public readonly BlueprintArchetype Exemplar = new BlueprintArchetype { name = "BrawlerExemplar", AssetGuid = BlueprintGuid.Parse("0864a8c5eae54557bb25b30ba5c46e66") };
        public readonly BlueprintAbilityResource Resource = new BlueprintAbilityResource { name = "BardicPerformanceResource", AssetGuid = BlueprintGuid.Parse("e190ba276831b5c4fa28737e5e49e6a6") };
        public readonly BlueprintFeature ResourceFact = new BlueprintFeature { name = "BardicPerformanceResourceFact", AssetGuid = BlueprintGuid.Parse("b92bfc201c6a79e49afd0b5cfbfc269f") };
        public readonly IncreaseResourcesByClass Native;
        public readonly UnitFactComponentDelegate External;
        private readonly BlueprintArchetype otherArchetype;
        private readonly UnitDescriptor descriptor;
        private readonly ModifiableValueAttributeStat charisma;
        private readonly ClassData brawlerData, bardData, otherData;
        private readonly EntityFactComponent nativeRuntime, externalRuntime;
        public Fixture()
        {
            Set(Brawler, "m_Archetypes", new[] { Ref<BlueprintArchetypeReference>(Exemplar) }); Set(Exemplar, "m_ParentClass", Brawler);
            Set(Bard, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); Set(Other, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            otherArchetype = new BlueprintArchetype { AssetGuid = BlueprintGuid.Parse("2bed52fbf927d4848b7a36324ee8117e") };
            Set(Other, "m_Archetypes", new[] { Ref<BlueprintArchetypeReference>(otherArchetype) });
            SetAmount(Resource, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(Bard), Ref<BlueprintCharacterClassReference>(Other), Ref<BlueprintCharacterClassReference>(Brawler) });
            SetAmount(Resource, "m_Archetypes", new[] { Ref<BlueprintArchetypeReference>(otherArchetype), Ref<BlueprintArchetypeReference>(Exemplar) });
            SetAmount(Resource, "IncreasedByLevel", true); SetAmount(Resource, "LevelIncrease", 1);
            Native = new IncreaseResourcesByClass { BaseValue = 2, Stat = StatType.Charisma };
            Set(Native, "m_Resource", Ref<BlueprintAbilityResourceReference>(Resource)); Set(Native, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Bard));
            External = (UnitFactComponentDelegate)Activator.CreateInstance(provider);
            Set(External, "Resource", Resource); Set(External, "CharacterClass", Brawler); Set(External, "Archetype", Exemplar); Set(External, "base_value", -2);
            ResourceFact.ComponentsArray = new BlueprintComponent[] { Native, External };
            foreach (var bp in new SimpleBlueprint[] { Brawler, Bard, Other, Exemplar, Resource, ResourceFact }) Cache.AddCachedBlueprint(bp.AssetGuid, bp);
            var unit = Empty<UnitEntityData>(); descriptor = Empty<UnitDescriptor>(); var stats = Empty<CharacterStats>();
            var progression = Empty<UnitProgressionData>(); charisma = Empty<ModifiableValueAttributeStat>();
            Set(unit, "<Descriptor>k__BackingField", descriptor); Set(descriptor, "<Unit>k__BackingField", unit);
            Set(descriptor, "Stats", stats); Set(stats, "Charisma", charisma); Set(charisma, "Type", StatType.Charisma); Set(stats, "<AllStats>k__BackingField", new List<ModifiableValue> { charisma }); Set(descriptor, "Progression", progression);
            brawlerData = new ClassData(Brawler); bardData = new ClassData(Bard); otherData = new ClassData(Other);
            Set(progression, "Classes", new List<ClassData> { brawlerData, bardData, otherData });
            var fact = new UnitFact(); Set(fact, "<Manager>k__BackingField", new EntityFactsManager(unit)); Set(fact, "<IsActive>k__BackingField", true);
            nativeRuntime = Native.CreateRuntimeFactComponent(); nativeRuntime.Setup(Native); nativeRuntime.RestoreOwnerLink(fact);
            externalRuntime = External.CreateRuntimeFactComponent(); externalRuntime.Setup(External); externalRuntime.RestoreOwnerLink(fact);
        }
        public BlueprintCharacterClassReference[] Classes() => (BlueprintCharacterClassReference[])Get(Get(Resource, "m_MaxAmount"), "m_Class");
        public BlueprintArchetypeReference[] Archetypes() => (BlueprintArchetypeReference[])Get(Get(Resource, "m_MaxAmount"), "m_Archetypes");
        public int Total(int brawler, int bard, int cha, bool exemplar, int extra = 0, int other = 0, bool otherEligible = true)
        {
            brawlerData.Level = brawler; bardData.Level = bard; otherData.Level = other;
            brawlerData.Archetypes.Clear(); if (exemplar) brawlerData.Archetypes.Add(Exemplar);
            otherData.Archetypes.Clear(); if (otherEligible) otherData.Archetypes.Add(otherArchetype);
            Set(charisma, "<ModifiedValue>k__BackingField", cha);
            int result = Resource.GetMaxAmount(descriptor);
            // Explicit callbacks isolate the formula from Unity's live subscriber lifecycle.
            using (nativeRuntime.RequestEventContext()) Native.CalculateMaxResourceAmount(Resource, ref result);
            using (externalRuntime.RequestEventContext()) ((IResourceAmountBonusHandler)External).CalculateMaxResourceAmount(Resource, ref result);
            return result + extra;
        }
    }
    private static void Register(Fixture f) => register.Invoke(null, new object[] { f.Cache });
    private static void SetAmount(object resource, string name, object value) { var a = Get(resource, "m_MaxAmount"); Set(a, name, value); Set(resource, "m_MaxAmount", a); }
    private static object Get(object o, string n) => AccessTools.Field(o.GetType(), n).GetValue(o);
    private static void Set(object o, string n, object v) => AccessTools.Field(o.GetType(), n).SetValue(o, v);
    private static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static BlueprintCharacterClass Class(string guid, string name) => new BlueprintCharacterClass { AssetGuid = BlueprintGuid.Parse(guid), name = name };
    private static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() {
        var r = BlueprintReferenceBase.CreateTyped<T>(bp);
        typeof(BlueprintReferenceBase).GetProperty("Cached", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(r, bp, null); return r;
    }
    private static void Check(bool ok, string msg) { if (!ok) throw new Exception(msg); assertions++; }
}
