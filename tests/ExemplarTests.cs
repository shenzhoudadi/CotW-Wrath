using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

internal static class ExemplarTests
{
    private static string managed, modPath;
    private static int assertions;
    private static Type registration;
    private static MethodInfo BaseValue;
    private static MethodInfo Progression;
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int RunTests(string[] args)
    {
        if (args.Length != 3) return 2;
        managed = Path.GetFullPath(args[0]); modPath = Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            foreach (var dir in new[] { managed, Path.GetDirectoryName(modPath) }) {
                var path = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try {
            Run();
            File.WriteAllText(args[2], "{\"assertionsPassed\":" + assertions + ",\"scope\":\"Real game GetBaseValue and ApplyProgression with production Harmony correction and seeded caster/classes; no Unity/aura/combat/save test\"}");
            return 0;
        } catch (Exception e) { for (var ex = e; ex != null; ex = ex.InnerException) Console.Error.WriteLine(ex.GetType().FullName + ": " + ex.Message + "\n" + ex.StackTrace); return 1; }
    }
    private static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); assertions++; }
    private static void Register(Fixture f) => registration.GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { f.Cache });
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        BaseValue = AccessTools.Method(typeof(ContextRankConfig), "GetBaseValue");
        Progression = AccessTools.Method(typeof(ContextRankConfig), "ApplyProgression");
        var mod = Assembly.LoadFrom(modPath);
        registration = mod.GetType("CallOfTheWild.Wrath.Prototype.ExemplarInspireCourage", true);
        var f = new Fixture();
        f.SetLevels(5, 0, 0, true, false);
        Assert(f.RankValue() == 2, "Source-shaped Exemplar 5 should reproduce early +2 before patch");
        var harmony = new Harmony("CotW.ExemplarFixture");
        try {
            harmony.PatchAll(mod);
            Assert(f.RankValue() == 2, "Patch should be inert before validated registration");
            Register(f);
            foreach (var item in new[] { new[] {3,1}, new[] {4,1}, new[] {5,1}, new[] {6,1}, new[] {7,2}, new[] {10,2}, new[] {11,2}, new[] {12,2}, new[] {13,3}, new[] {16,3}, new[] {17,3}, new[] {18,3}, new[] {19,4}, new[] {20,4} }) {
                f.SetLevels(item[0], 0, 0, true, false);
                Assert(f.RankValue() == item[1], "Wrong pure Exemplar rank at " + item[0]);
            }
            f.SetLevels(5, 5, 0, true, false); Assert(f.RankValue() == 2, "Bard multiclass contribution reduced");
            f.SetLevels(5, 4, 0, true, false); Assert(f.RankValue() == 1, "Mixed class maximum incorrectly summed");
            f.SetLevels(5, 0, 6, true, true); Assert(f.RankValue() == 2, "Sensei contribution reduced");
            f.SetLevels(5, 0, 6, true, false); Assert(f.RankValue() == 1, "Ordinary monk incorrectly counted");
            f.SetLevels(5, 0, 0, false, false); Assert(f.Base() == 0, "Ordinary Brawler counted as Exemplar");
            f.SetLevels(0, 5, 0, false, false); Assert(f.RankValue() == 2, "Pure Bard changed");
            f.SetLevels(0, 0, 5, false, true); Assert(f.RankValue() == 2, "Pure Sensei changed");
            f.SetLevels(5, 0, 0, true, false); f.Params.RankBonus = 2;
            Assert(f.Base() == 5 && f.RankValue() == 2, "Native rank bonus lost");
            Set(f.Rank, "m_DisableRankBonus", true);
            Assert(f.Base() == 3, "Disabled rank bonus applied");
            Set(f.Rank, "m_DisableRankBonus", false); f.Params.RankBonus = 0;
            var originalArray = Get(f.Rank, "m_Class");
            Register(f); Assert(f.RankValue() == 1 && f.RankValue() == 1, "Correction compounds on re-registration/query");
            Assert(ReferenceEquals(originalArray, Get(f.Rank, "m_Class")) && ((Array)originalArray).Length == 3, "Shared class array modified");
            Assert(f.BrawlerData.Level == 5, "Character level mutated");
            var unrelated = (ContextRankConfig)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(f.Rank, null);
            Assert((int)BaseValue.Invoke(unrelated, new object[] { f.Context }) == 5, "Unregistered rank component was changed");
            // Existing foreign postfix result must not be corrected twice.
            var patch = registration.GetNestedType("RankPatch", BindingFlags.NonPublic).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
            object[] changedResult = { f.Rank, f.Context, 3 };
            patch.Invoke(null, changedResult); Assert((int)changedResult[2] == 3, "Already corrected result changed again");
            Set(f.Rank, "m_StepLevel", 5); Assert(f.Base() == 5, "Later incompatible schema change ignored");
            ExpectRejected(f); Assert(f.Base() == 5, "Failed registration left correction enabled");
            f = new Fixture(); f.Exemplar.name = "Collision"; ExpectRejected(f);
            f = new Fixture(); Set(f.Exemplar, "m_ParentClass", f.Bard); ExpectRejected(f);
            f = new Fixture(); Set(f.Rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(f.Bard) }); ExpectRejected(f);
            f = new Fixture(); Set(f.Rank, "m_AdditionalArchetypes", new BlueprintArchetypeReference[0]); ExpectRejected(f);
            f = new Fixture(); f.Cache.RemoveCachedBlueprint(f.Effect.AssetGuid); ExpectRejected(f);
        } finally { harmony.UnpatchAll("CotW.ExemplarFixture"); }
        f = new Fixture(); Register(f); f.SetLevels(5,0,0,true,false);
        Assert(f.RankValue() == 2, "Owner unpatch failed to restore original native result");
        Console.WriteLine("PASS: " + assertions + " Exemplar assertions; real native base/progression + production Harmony patch; no Unity/aura/combat/save test.");
    }
    private static void ExpectRejected(Fixture f)
    {
        try { Register(f); throw new Exception("Expected guard rejection"); }
        catch (TargetInvocationException e) { Assert(e.InnerException is InvalidOperationException, "Unexpected registration exception"); }
    }
    private sealed class Fixture
    {
        public readonly BlueprintsCache Cache = new BlueprintsCache();
        public readonly BlueprintCharacterClass Brawler = Class("1c44d137122540d5aed818fd7902625f", "RMBrawlerHybridClass");
        public readonly BlueprintCharacterClass Bard = Class("772c83a25e2268e448e841dcd548235f", "BardClass");
        public readonly BlueprintCharacterClass Monk = Class("e8f21e5b58e0569468e420ebea456124", "MonkClass");
        public readonly BlueprintArchetype Exemplar = new BlueprintArchetype { name = "BrawlerExemplar", AssetGuid = BlueprintGuid.Parse("0864a8c5eae54557bb25b30ba5c46e66") };
        private readonly BlueprintArchetype sensei = new BlueprintArchetype { name = "Sensei", AssetGuid = BlueprintGuid.Parse("f8767821ec805bf479706392fcc3394c") };
        public readonly ContextRankConfig Rank = new ContextRankConfig();
        public readonly BlueprintBuff Effect = new BlueprintBuff { name = "InspireCourageEffectBuff", AssetGuid = BlueprintGuid.Parse("6d6d9e06b76f5204a8b7856c78607d5d") };
        public readonly MechanicsContext Context = Empty<MechanicsContext>();
        public readonly AbilityParams Params = new AbilityParams();
        public readonly ClassData BrawlerData, BardData, MonkData;
        public Fixture()
        {
            Set(Brawler, "m_Archetypes", new[] { Ref<BlueprintArchetypeReference>(Exemplar) });
            Set(Bard, "m_Archetypes", new BlueprintArchetypeReference[0]);
            Set(Monk, "m_Archetypes", new[] { Ref<BlueprintArchetypeReference>(sensei) });
            Set(Exemplar, "m_ParentClass", Brawler);
            Set(sensei, "m_ParentClass", Monk);
            Set(Rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(Bard), Ref<BlueprintCharacterClassReference>(Monk), Ref<BlueprintCharacterClassReference>(Brawler) });
            Set(Rank, "Archetype", Ref<BlueprintArchetypeReference>(sensei));
            Set(Rank, "m_AdditionalArchetypes", new[] { Ref<BlueprintArchetypeReference>(Exemplar) });
            Set(Rank, "m_BaseValueType", ContextRankBaseValueType.MaxClassLevelWithArchetype);
            Set(Rank, "m_Progression", ContextRankProgression.StartPlusDivStep);
            Set(Rank, "m_StartLevel", -1); Set(Rank, "m_StepLevel", 6);
            Effect.ComponentsArray = new BlueprintComponent[] { Rank };
            foreach (var b in new SimpleBlueprint[] { Brawler, Exemplar, Effect }) Cache.AddCachedBlueprint(b.AssetGuid, b);
            var unit = Empty<UnitEntityData>(); var descriptor = Empty<UnitDescriptor>(); var progression = Empty<UnitProgressionData>();
            Set(unit, "<Descriptor>k__BackingField", descriptor); Set(descriptor, "<Unit>k__BackingField", unit); Set(descriptor, "Progression", progression);
            BrawlerData = new ClassData(Brawler); BardData = new ClassData(Bard); MonkData = new ClassData(Monk);
            Set(progression, "Classes", new List<ClassData> { BrawlerData, BardData, MonkData });
            var proxy = FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy", BindingFlags.Public | BindingFlags.NonPublic));
            Set(proxy, "<Entity>k__BackingField", unit);
            object casterRef = default(EntityRef<UnitEntityData>); Set(casterRef, "m_Proxy", proxy);
            Set(Context, "m_CasterRef", casterRef); Set(Context, "m_Params", Params);
        }
        public void SetLevels(int brawler, int bard, int monk, bool exemplar, bool hasSensei)
        {
            BrawlerData.Level = brawler; BardData.Level = bard; MonkData.Level = monk;
            BrawlerData.Archetypes.Clear(); MonkData.Archetypes.Clear();
            if (exemplar) BrawlerData.Archetypes.Add(Exemplar);
            if (hasSensei) MonkData.Archetypes.Add(sensei);
        }
        public int Base() => (int)BaseValue.Invoke(Rank, new object[] { Context });
        public int RankValue() => (int)Progression.Invoke(Rank, new object[] { Base() });
    }
    private static BlueprintCharacterClass Class(string id, string name) => new BlueprintCharacterClass { name = name, AssetGuid = BlueprintGuid.Parse(id) };
    private static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static T Ref<T>(SimpleBlueprint value) where T : BlueprintReferenceBase, new()
    {
        var r = BlueprintReferenceBase.CreateTyped<T>(value);
        typeof(BlueprintReferenceBase).GetProperty("Cached", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(r, value, null);
        return r;
    }
    private static object Get(object value, string name) => AccessTools.Field(value.GetType(), name).GetValue(value);
    private static void Set(object value, string name, object fieldValue) => AccessTools.Field(value.GetType(), name).SetValue(value, fieldValue);
}

internal static class ExemplarLauncher
{
    private static int Main(string[] args)
    {
        if (args.Length != 3 && args.Length != 5) return 2;
        if(args.Length==5){var scene=Path.GetFullPath(args[3]);using(var algorithm=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(scene)){var hash=BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","");if(hash!=args[4]||scene==Path.Combine(Path.GetFullPath(args[0]),"UnityEngine.CoreModule.dll")||Path.GetFileName(scene)!="UnityEngine.CoreModule.dll")throw new Exception("058 Exemplar scene dependency must match the audited copy.");}Assembly.LoadFrom(scene);args=args.Take(3).ToArray();}
        var dirs = new[] { Path.GetFullPath(args[0]), Path.GetDirectoryName(Path.GetFullPath(args[1])) };
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            foreach (var dir in dirs) {
                var path = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        return (int)Assembly.GetExecutingAssembly().GetType("ExemplarTests").GetMethod("RunTests", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { args });
    }
}
