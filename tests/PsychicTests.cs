using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

internal static class PsychicLauncher
{
    private static int Main(string[] args) {
        if (args.Length != 5) return 2;
        var runtimePath = Environment.GetEnvironmentVariable("COTW_HARMONY_MATRIX_PATH");
        if (!string.IsNullOrEmpty(runtimePath)) Assembly.LoadFrom(runtimePath);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            if (new AssemblyName(e.Name).Name == "0Harmony" && !string.IsNullOrEmpty(runtimePath)) return Assembly.LoadFrom(runtimePath);
            foreach (var dir in new[] { args[0], Path.GetDirectoryName(args[1]) }) {
                var file = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            } return null;
        };
        try { typeof(PsychicLauncher).Assembly.GetType("PsychicTests").GetMethod("Run").Invoke(null, new object[] { args }); return 0; }
        catch (Exception e) { while(e != null) { Console.Error.WriteLine(e.GetType().FullName + ": " + e.Message + "\n" + e.StackTrace); e = e.InnerException; } return 1; }
    }
}

internal static class PsychicTests
{
    private static Type module, casting;
    private static int assertions;
    private static string Const(string field) => (string)AccessTools.Field(module, field).GetValue(null);
    private static object Get(object o, string f) => AccessTools.Field(o.GetType(), f).GetValue(o);
    private static void Set(object o, string f, object v) => AccessTools.Field(o.GetType(), f).SetValue(o, v);
    private static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() => BlueprintReferenceBase.CreateTyped<T>(bp);
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); assertions++; }
    private static object Call(string name, params object[] args) => AccessTools.Method(casting, name).Invoke(null, args);
    private static void Register(Fixture f) => AccessTools.Method(module, "Register").Invoke(null, new object[] { f.Cache });
    public static void Run(string[] args)
    {
        RequiredHarmonyRuntime.Verify(Path.Combine(args[0],"0Harmony.dll"));
        var profiling = new Harmony("CotWPsychic.ProfilingOnly");
        profiling.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope), "New", new[] { typeof(string), typeof(UnityEngine.Object) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PsychicTests), nameof(SkipProfiling))));
        // A caster-free duration context avoids native dice/game execution. Its
        // diagnostic logging must also avoid Unity's Application log callback.
        profiling.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel), "Error", new[] { typeof(string), typeof(object[]) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PsychicTests), nameof(SkipProfiling))));
        var assembly = Assembly.LoadFrom(args[1]);
        // COTW-0109: route the directed Iroran case before any Psychic fixture or
        // historical suite; the reroll primitive needs no blueprint cache. When the
        // reviewed native row fixture is supplied, the B3 registration checks run
        // in the same isolated host right after the B1 assertions.
        if (Environment.GetEnvironmentVariable("COTW_0109_CASE") == "iroran0109")
        {
            Iroran0109Tests.Run(assembly, args[2]);
            if (args[4] != "-")
                Iroran0109Tests.RunRegistration(assembly,
                    System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(args[2])), "iroran0109-registration-report.json"),
                    args[4]);
            return;
        }
        module = assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicClass", true);
        casting = assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicCasting", true);
        var native = JArray.Parse(File.ReadAllText(args[3]));
        var anchorEvidence = JObject.Parse(File.ReadAllText(args[4]))["requiredNative"];
        foreach (var pair in new[] { new[] { "RootId", "BlueprintRoot", "BlueprintRoot" },
            new[] { "WizardId", "WizardClass", "BlueprintCharacterClass" },
            new[] { "SorcererBookId", "SorcererSpellbook", "BlueprintSpellbook" },
            new[] { "ProficienciesId", "SorcererProficiencies", "BlueprintFeature" },
            new[] { "ExtendFeatId", "ExtendSpellFeat", "BlueprintFeature" } })
        {
            var row = anchorEvidence.SingleOrDefault(j => (string)j["blueprint"]["AssetId"] == Const(pair[0]));
            Check(row != null && (string)row["name"] == pair[1] &&
                ((string)row["blueprint"]["Data"]["$type"]).EndsWith(", " + pair[2]),
                "Foundation dependency GUID, real archive name and type match: " + pair[0]);
        }
        var catalog = (string[][])AccessTools.Field(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpellCatalog"), "ByLevel").GetValue(null);
        Check(catalog.Length == 10 && catalog.Sum(l => l.Length) == 139, "Audited 139 native spell subset in ten levels");
        foreach (var id in catalog.SelectMany(l => l)) Check(native.Any(j => (string)j["AssetId"] == id && ((string)j["Data"]["$type"]).EndsWith(", BlueprintAbility")), "Catalog spell exists in native input " + id);
        var ids = (string[])AccessTools.Field(module, "OwnIds").GetValue(null);
        Check(ids.Distinct().Count() == ids.Length && !native.Any(j => ids.Contains((string)j["AssetId"])), "New identities unique and absent from native input");
        Console.WriteLine("Psychic: registration fixture"); var f = new Fixture(catalog); Register(f);
        // COTW-0068: route the directed detective case directly after the shared
        // registration fixture. It materialises only the reviewed native row
        // archive and stages the audited HA provider identities; no historical
        // Psychic behaviour suite, full catalogue or game process is entered.
        if (Environment.GetEnvironmentVariable("COTW_0068_CASE") == "psychicdetective0068")
        {
            var rows0068 = JObject.Parse(File.ReadAllText(args[4]));
            PsychicExpansionTests.LoadNativeRows(f.Cache, rows0068, true);
            PsychicExpansionTests.HydrateEmptyTables(f.Cache, rows0068);
            PsychicExpansionTests.HydrateCachedSpellbooks(f.Cache, rows0068);
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, f.Cache);
            var directed0068 = new Harmony("CotWPsychicDetective0068.Directed");
            int passed0068 = PsychicDetective0068Tests.Run(f.Cache, directed0068, rows0068);
            var report0068 = new JObject { ["case"] = "psychicdetective0068", ["assertionsPassed"] = passed0068,
                ["scope"] = "Directed 0068 checks only; shared fixture registration and staged HA provider fixture, no historical Psychic behaviour suites." };
            if (args[2] == "-") Console.WriteLine(report0068.ToString());
            else {
                var path0068 = Path.GetFullPath(args[2]);
                Directory.CreateDirectory(Path.GetDirectoryName(path0068));
                File.WriteAllText(path0068, report0068.ToString());
            }
            Console.WriteLine("PASS: " + passed0068 + " Psychic Detective 0068 directed assertions. Standalone managed verification, not a game test.");
            return;
        }
        // COTW-0070: route the directed Cryptid Scholar case directly after
        // the shared registration fixture, exactly like the 0068 case. Only the
        // reviewed native rows plus the audited 0070 classification rows load.
        if (Environment.GetEnvironmentVariable("COTW_0070_CASE") == "cryptidscholar0070")
        {
            var rows0070 = JObject.Parse(File.ReadAllText(args[4]));
            PsychicExpansionTests.LoadNativeRows(f.Cache, rows0070, true);
            PsychicExpansionTests.HydrateEmptyTables(f.Cache, rows0070);
            PsychicExpansionTests.HydrateCachedSpellbooks(f.Cache, rows0070);
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, f.Cache);
            var directed0070 = new Harmony("CotWCryptidScholar0070.Directed");
            int passed0070 = CryptidScholar0070Tests.Run(f.Cache, directed0070, rows0070);
            var report0070 = new JObject { ["case"] = "cryptidscholar0070", ["assertionsPassed"] = passed0070,
                ["scope"] = "Directed 0070 checks only; shared fixture registration and staged HA provider fixture, no historical Psychic behaviour suites." };
            if (args[2] == "-") Console.WriteLine(report0070.ToString());
            else {
                var path0070 = Path.GetFullPath(args[2]);
                Directory.CreateDirectory(Path.GetDirectoryName(path0070));
                File.WriteAllText(path0070, report0070.ToString());
            }
            Console.WriteLine("PASS: " + passed0070 + " Cryptid Scholar 0070 directed assertions. Standalone managed verification, not a game test.");
            return;
        }
        // D-F07: route the 0060 directed case directly after the shared fixture
        // is registered. Nothing below (casting, expansion, historical toggles)
        // belongs to this work package and must not run under its banner.
        if (Environment.GetEnvironmentVariable("COTW_0060_CASE") == "spiritguide0060")
        {
            // Fresh() rebuilds registration dependencies from the reviewed native
            // row fixture. Load only that data row set here; no historical
            // behaviour suite is executed for this work package.
            var nativeRows = JObject.Parse(File.ReadAllText(args[4]));
            PsychicExpansionTests.LoadNativeRows(f.Cache, nativeRows, true);
            PsychicExpansionTests.HydrateEmptyTables(f.Cache, nativeRows);
            PsychicExpansionTests.HydrateCachedSpellbooks(f.Cache, nativeRows);
            // Blueprint references resolve through the library cache, exactly as
            // the production registration expects.
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, f.Cache);
            var guide = new Harmony("CotWSpiritGuide0060.Directed");
            int count = SpiritGuide0060Tests.Run(f.Cache, guide);
            var report = new JObject { ["case"] = "spiritguide0060", ["assertionsPassed"] = count,
                ["scope"] = "Directed 0060 checks only; shared fixture registration, no historical Psychic behaviour suites." };
            if (args[2] == "-") Console.WriteLine(report.ToString());
            else {
                var path = Path.GetFullPath(args[2]);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, report.ToString());
            }
            Console.WriteLine("PASS: " + count + " Spirit Guide 0060 directed assertions. Standalone managed verification, not a game test.");
            return;
        }
        var character = (BlueprintCharacterClass)f.Load(Const("ClassId")); var book = (BlueprintSpellbook)f.Load(Const("BookId"));
        Check(!character.IsArcaneCaster && !character.IsDivineCaster && !character.PrestigeClass && !character.HideInUI, "Visible independent psychic base class");
        Check(character.Spellbook == book && book.CharacterClass == character && book.CastingAttribute == StatType.Intelligence && book.Spontaneous && !book.IsArcane, "Intelligence spontaneous spellbook and reciprocal class links");
        Check(!book.AllSpellsKnown && !book.CanCopyScrolls && book.CantripsType == CantripsType.Cantrips, "Native limited spells-known and unlimited cantrips");
        Check(book.SpellsKnown == f.NativeBook.SpellsKnown && book.SpellsPerDay == f.NativeBook.SpellsPerDay, "Verified native nine-circle progression tables");
        Check((bool)AccessTools.Method(casting, "IsPsychic", new[] { typeof(BlueprintSpellbook) }).Invoke(null, new object[] { book }), "Psychic marker classification");
        Check(!(bool)AccessTools.Method(casting, "IsPsychic", new[] { typeof(BlueprintSpellbook) }).Invoke(null, new object[] { f.NativeBook }), "Native book is unchanged");
        Check(character.Progression.LevelEntries.Length == 20 && character.Progression.LevelEntries[0].Features.Count() == 3, "Playable class level-one casting/proficiencies/pool entry");
        var center = (BlueprintAbility)f.Load(Const("CenterAbilityId")); var action = center.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single() as ContextActionApplyBuff;
        Check(center.ActionType == UnitCommand.CommandType.Move && center.Range == AbilityRange.Personal && action.ToCaster && action.IsNotDispelable && !action.Permanent && action.DurationValue.BonusValue.Value == 1, "Center Self is a self-only move action for one round");
        var durationContext = Empty<MechanicsContext>(); durationContext.SetParams(new AbilityParams());
        Check(action.DurationValue.DiceCountValue != null && action.DurationValue.Calculate(durationContext).Value == 1,
            "Actual native Center Self duration calculation succeeds for one round");
        bool oldDurationFails = false;
        try { new ContextDurationValue { Rate = Kingmaker.UnitLogic.Mechanics.DurationRate.Rounds, BonusValue = 1 }.Calculate(durationContext); }
        catch (NullReferenceException) { oldDurationFails = true; }
        Check(oldDurationFails, "Old omitted dice-count field reproduces native duration failure");
        var applyFactory = assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
        foreach (var sample in new[] { new[] { 1, 0, 1 }, new[] { 2, 0, 2 }, new[] { 1, 1, 10 } })
        {
            var rate = sample[1] == 0 ? DurationRate.Rounds : DurationRate.Minutes;
            var applied = (ContextActionApplyBuff)AccessTools.Method(applyFactory, "Apply").Invoke(null,
                new object[] { new BlueprintBuff { name = "DurationProbe" }, sample[0], rate, false, true });
            Check(applied.DurationValue.DiceCountValue != null && applied.DurationValue.Calculate(durationContext).Value == sample[2],
                "Native exclusive spell/discipline buff duration calculated: " + sample[0] + " " + rate);
        }
        var logical = (Metamagic)AccessTools.Field(casting, "Logical").GetValue(null);
        Check(logical == (Metamagic)(1 << 29) && logical != Metamagic.Piercing && logical != Metamagic.Intensified, "Logical fixed flag avoids native collisions");
        var own = ids.Select(f.Load).ToArray(); var snapshots = f.Snapshot(); Register(f); Register(f);
        Check(own.SequenceEqual(ids.Select(f.Load)) && snapshots.SequenceEqual(f.Snapshot()), "Repeated registration preserves assets and lists");
        Check(f.NativeBook.IsArcane && f.NativeBook.CastingAttribute == StatType.Charisma, "Native Sorcerer unaffected");
        foreach (var bad in new[] { Const("ClassId"), Const("BookId"), Const("CenterBuffId"), Const("LogicalId") }) {
            var broken = new Fixture(catalog); var foreign = new BlueprintFeature { name = "Foreign", AssetGuid = BlueprintGuid.Parse(bad) }; broken.Cache.AddCachedBlueprint(foreign.AssetGuid, foreign);
            var before = broken.Snapshot(); ExpectFailure(broken);
            Check(before.SequenceEqual(broken.Snapshot()) && broken.Load(bad) == foreign && ids.Count(id => broken.Load(id) != null) == 1, "Conflict fails before native mutation " + bad);
        }
        var incomplete = new Fixture(catalog); incomplete.Cache.RemoveCachedBlueprint(BlueprintGuid.Parse(catalog[9][0])); var old = incomplete.Snapshot(); ExpectFailure(incomplete);
        Check(old.SequenceEqual(incomplete.Snapshot()) && ids.All(id => incomplete.Load(id) == null), "Missing required spell leaves no partial assets");
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, f.Cache);
        TestCasting(f, book, logical);
        assertions += PsychicExpansionTests.Run(assembly, f.Cache, args[4]);
        assertions += Psychic070Tests.Run(assembly, f.Cache, args[4]);
        assertions += PsychicToggleTests.Run(assembly, f.Cache);
        Register(f);
        profiling.UnpatchAll(profiling.Id);
        var result=PsychicExpansionTests.Inventory;
        result["assertionsPassed"]=assertions;
        result["scope"]="Real game managed types; casting/registration, metamagic, native spellbook learn/promote/remove/PostLoad, resource formula, buff duration calculation, damage-action routing and extension conflict checks. Native Buff.OnRemove with Unity validity/FX replacement; native tooltip block construction and CharGenSpellsPhaseVM.TryUpdateSamePhase with UpdateSelection rendering callback bypassed. Native SelectSpell.Apply and current-level selection snapshots. Constructed entity proxies; fixture faction/profiling shims and caster-free diagnostic Error logging bypass; native damage action captured before combat. 0.7.0 also executes amplification handlers with native resource spend/refund and real RuleHealDamage routing (OnTrigger intercepted), and captures UnitHelper.AddBuff at the live-game boundary. Constructed Game/Rulebook proxy and minimum-attribute/faction shims; optional provider blueprints use contract fixtures, native mappings checked against the installed archive. Older suites capture final buff insertion. The 0.14.28 suites additionally execute native saving throws, ApplyBuff permission and condition lifecycles, poison dispel resolution, burn spending and level-grant lifecycles with explicit scene/dispatch/dice boundaries; see each contract. Unity rendering, real combat, complete rest and leveling UI, external provider assembly execution and game save serialization/reload are outside these managed checks.";
        // A dash is an explicit stdout sink. Device names such as NUL are not
        // regular files on .NET Framework and must not be used as report files.
        if (args[2] == "-") Console.WriteLine(result.ToString());
        else {
            var reportPath = Path.GetFullPath(args[2]);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, result.ToString());
        }
        Console.WriteLine("PASS: " + assertions + " Psychic casting assertions. Standalone managed verification, not a game test.");
    }
    private static bool SkipProfiling() => false;
    private static void ExpectFailure(Fixture f) {
        try { Register(f); } catch (TargetInvocationException e) when(e.InnerException is InvalidOperationException) { assertions++; return; }
        throw new Exception("Expected isolated Psychic registration failure.");
    }
    private static void TestCasting(Fixture f, BlueprintSpellbook blueprint, Metamagic logical)
    {
        var unit = Empty<UnitEntityData>(); var descriptor = Empty<UnitDescriptor>();
        Set(unit, "<Descriptor>k__BackingField", descriptor); Set(descriptor, "<Unit>k__BackingField", unit);
        Set(unit, "Parts", new EntityPartsManager(unit));
        var manager = new EntityFactsManager(unit); var buffs = new BuffCollection(); buffs.Initialize(manager); Set(unit, "<Buffs>k__BackingField", buffs);
        var book = Empty<Spellbook>(); Set(book, "Blueprint", blueprint); Set(book, "<Owner>k__BackingField", descriptor);
        var spell = Empty<AbilityData>(); Set(spell, "Blueprint", new BlueprintAbility { Type = AbilityType.Spell }); Set(spell, "m_CachedSpellbook", book);
        var proxy = FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy", BindingFlags.Public | BindingFlags.NonPublic)); Set(proxy, "<Entity>k__BackingField", unit);
        object casterRef = default(EntityRef<UnitEntityData>); Set(casterRef, "m_Proxy", proxy); Set(spell, "m_CasterRef", casterRef);
        Buff MakeBuff(BlueprintBuff bp, SpellDescriptor descriptorFlags = SpellDescriptor.None) {
            var b = Empty<Buff>(); Set(b, "<Blueprint>k__BackingField", bp); Set(b, "<IsTurnedOn>k__BackingField", true);
            var context = Empty<MechanicsContext>(); Set(context, "<SpellDescriptor>k__BackingField", descriptorFlags); Set(b, "m_Context", context); return b;
        }
        Check(!(bool)Call("IsEmotionBlocked", spell), "Healthy psychic can cast");
        foreach (var flag in new[] { SpellDescriptor.Fear, SpellDescriptor.Shaken, SpellDescriptor.NegativeEmotion, SpellDescriptor.Frightened }) {
            var b = MakeBuff(new BlueprintBuff(), flag); buffs.Enumerable.Add(b);
            Check((bool)Call("IsEmotionBlocked", spell), "Negative effect blocks " + flag);
            Set(b, "<IsSuppressed>k__BackingField", true); Check(!(bool)Call("IsEmotionBlocked", spell), "Suppressed effect does not block"); Set(b, "<IsSuppressed>k__BackingField", false);
            spell.MetamagicData = new MetamagicData(); spell.MetamagicData.Add(logical); Check(!(bool)Call("IsEmotionBlocked", spell), "Logical bypasses emotion"); spell.MetamagicData = null;
            Set(book, "Blueprint", f.NativeBook); Check(!(bool)Call("IsEmotionBlocked", spell), "Mixed character's ordinary book unaffected"); Set(book, "Blueprint", blueprint); buffs.Enumerable.Clear();
        }
        buffs.Enumerable.Add(MakeBuff(new BlueprintBuff(), SpellDescriptor.Emotion)); Check(!(bool)Call("IsEmotionBlocked", spell), "Positive generic emotion is allowed"); buffs.Enumerable.Clear();
        var nativeDescriptor = new BlueprintBuff { ComponentsArray = new BlueprintComponent[] { new SpellDescriptorComponent { Descriptor = SpellDescriptor.Shaken } } }; nativeDescriptor.OnEnable();
        buffs.Enumerable.Add(MakeBuff(nativeDescriptor)); Check((bool)Call("IsEmotionBlocked", spell), "Descriptor on buff blueprint blocks without source context descriptor"); buffs.Enumerable.Clear();
        RuleCalculateAbilityParams Params() { var r = Empty<RuleCalculateAbilityParams>(); Set(r, "Spellbook", book); Set(r, "AbilityData", spell); Set(r, "Initiator", unit); return r; }
        var evt = Params(); Call("ApplyThoughtPenalty", evt); Check((int)Get(evt, "m_BonusConcentration") == -10, "Actual native concentration modifier -10");
        var center = MakeBuff((BlueprintBuff)f.Load(Const("CenterBuffId"))); buffs.Enumerable.Add(center); evt = Params(); evt.AddBonusConcentration(7); Call("ApplyThoughtPenalty", evt);
        Check((int)Get(evt, "m_BonusConcentration") == 7, "Center Self restores normal concentration, retaining other bonuses"); Set(center, "<IsSuppressed>k__BackingField", true); evt = Params(); Call("ApplyThoughtPenalty", evt); Check((int)Get(evt, "m_BonusConcentration") == -10, "Suppressed Center Self grants no benefit"); buffs.Enumerable.Clear();
        spell.MetamagicData = new MetamagicData(); spell.MetamagicData.Add(logical); evt = Params(); Call("ApplyThoughtPenalty", evt); Check((int)Get(evt, "m_BonusConcentration") == -10, "Logical does not remove thought components"); spell.MetamagicData = null;
        Set(book, "Blueprint", f.NativeBook); evt = Params(); Call("ApplyThoughtPenalty", evt); Check((int)Get(evt, "m_BonusConcentration") == 0, "Other book concentration unaffected"); Set(book, "Blueprint", blueprint);
        var state = Empty<UnitState>(); Set(state, "m_Conditions", new sbyte[100]); Set(state, "m_ConditionsImmunity", new sbyte[100]);
        var silenceBp = new BlueprintBuff { AssetGuid = BlueprintGuid.Parse("e4b8a158e8aabfe41b5e0bd334d8c386"), ComponentsArray = new BlueprintComponent[] { new AddCondition { Condition = UnitCondition.SpellcastingForbidden } } }; silenceBp.OnEnable();
        buffs.Enumerable.Add(MakeBuff(silenceBp)); ((sbyte[])Get(state, "m_Conditions"))[(int)UnitCondition.SpellcastingForbidden] = 1;
        Check(!(bool)Call("CheckCastingCondition", state, UnitCondition.SpellcastingForbidden, spell), "Audited silence alone does not block psychic casting");
        ((sbyte[])Get(state, "m_Conditions"))[(int)UnitCondition.SpellcastingForbidden] = 2;
        Check((bool)Call("CheckCastingCondition", state, UnitCondition.SpellcastingForbidden, spell), "Silence plus another restriction remains blocked");
        ((sbyte[])Get(state, "m_Conditions"))[(int)UnitCondition.SpellcastingForbidden] = 1; Set(book, "Blueprint", f.NativeBook);
        Check((bool)Call("CheckCastingCondition", state, UnitCondition.SpellcastingForbidden, spell), "Ordinary spells remain silenced"); Set(book, "Blueprint", blueprint); buffs.Enumerable.Clear();
        Check((bool)Call("CheckCastingCondition", state, UnitCondition.SpellcastingForbidden, spell), "Unknown anti-casting source remains blocked");
        var harmony = new Harmony("CotWPsychic.IsolatedHelpers");
        try {
            // Apply only pure metamagic helpers/collection and UI name hooks here.
            foreach (var name in new[] { "LogicalCostPatch", "LogicalNamePatch", "LogicalListPatch", "LogicalSelectionPatch" }) harmony.CreateClassProcessor(casting.GetNestedType(name, BindingFlags.NonPublic)).Patch();
            Check(logical.DefaultCost() == 1 && Metamagic.Piercing.DefaultCost() == 1 && Metamagic.Quicken.DefaultCost() == 4, "Actual patched cost preserves native metamagic");
            var feature = Empty<Feature>(); Set(feature, "<Blueprint>k__BackingField", f.Load(Const("LogicalId")));
            var component = ((BlueprintFeature)f.Load(Const("LogicalId"))).GetComponent<AddMetamagicFeat>();
            var runtime = component.CreateRuntimeFactComponent(); runtime.Setup(component); runtime.RestoreOwnerLink(feature);
            Set(feature, "Components", new List<EntityFactComponent> { runtime });
            var collect = Empty<RuleCollectMetamagic>(); Set(collect, "Spellbook", book); Set(collect, "Spell", spell.Blueprint); Set(collect, "m_SpellLevel", 1); Set(collect, "KnownMetamagics", new List<Feature>()); Set(collect, "SpellMetamagics", new List<Feature>());
            collect.AddMetamagic(feature); Check(collect.SpellMetamagics.Contains(feature), "Logical selection offered for psychic book without modifying shared spell masks");
            Set(collect, "SpellMetamagics", new List<Feature>()); Set(book, "Blueprint", f.NativeBook); collect.AddMetamagic(feature); Check(!collect.SpellMetamagics.Contains(feature), "Logical not offered on ordinary book"); Set(book, "Blueprint", blueprint);
            Set(collect, "SpellMetamagics", new List<Feature>()); Set(collect, "m_SpellLevel", 9); collect.AddMetamagic(feature); Check(!collect.SpellMetamagics.Contains(feature), "Logical cannot create tenth-circle psychic spell");
            Check(Kingmaker.UI.Common.UIUtilityTexts.GetMetamagicName(logical) == "Logical", "English metamagic UI label");
        } finally { harmony.UnpatchAll(harmony.Id); }
    }
    private sealed class Fixture
    {
        public readonly BlueprintsCache Cache = new BlueprintsCache();
        public readonly BlueprintSpellbook NativeBook;
        private readonly BlueprintRoot root;
        private readonly BlueprintFeatureSelection basic;
        public SimpleBlueprint Load(string id) => Cache.Load(BlueprintGuid.Parse(id));
        private T Add<T>(T obj, string id, string name) where T : SimpleBlueprint { obj.AssetGuid = BlueprintGuid.Parse(id); obj.name = name; Cache.AddCachedBlueprint(obj.AssetGuid, obj); return obj; }
        public Fixture(string[][] catalog) {
            SetStaticCache(Cache);
            var centerIcon = Add(new BlueprintBuff(), "00369aa0a76141a479382e360b1f3dd7", "ResiliencyJudgmentBuffMagic");
            Set(centerIcon,"m_Icon",Empty<UnityEngine.Sprite>());
            var wizard = Add(new BlueprintCharacterClass(), Const("WizardId"), "WizardClass");
            var known = Add(new BlueprintSpellsTable(), "d232bc78d967a964bac4d4d38e7ca5f4", "SorcererSpellsKnownTable");
            var perDay = Add(new BlueprintSpellsTable(), "dbff16956df2eda48a1da5c9617cc836", "SorcererSpellsPerDayTable");
            NativeBook = Add(new BlueprintSpellbook { Spontaneous = true, IsArcane = true, CastingAttribute = StatType.Charisma }, Const("SorcererBookId"), "SorcererSpellbook");
            Set(NativeBook, "m_SpellsKnown", Ref<BlueprintSpellsTableReference>(known)); Set(NativeBook, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(perDay));
            Add(new BlueprintFeature(), Const("ProficienciesId"), "SorcererProficiencies"); Add(new BlueprintFeature(), Const("ExtendFeatId"), "ExtendSpellFeat");
            basic = Add(new BlueprintFeatureSelection(), "247a4068296e8be42890143f451b4b45", "BasicFeatSelection");
            root = Add(new BlueprintRoot { Progression = new ProgressionRoot() }, Const("RootId"), "BlueprintRoot");
            Set(root.Progression, "m_CharacterClasses", new[] { Ref<BlueprintCharacterClassReference>(wizard) });
            foreach (var id in catalog.SelectMany(l => l)) { var nativeSpell = Add(new BlueprintAbility { Type = AbilityType.Spell }, id, "NativeSpell"); Set(nativeSpell,"m_Icon",Empty<UnityEngine.Sprite>()); }
        }
        private static void SetStaticCache(BlueprintsCache cache) => AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        public string[] Snapshot() => new[] { string.Join(",", ((BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses")).Select(r => r.Guid)),
            string.Join(",", ((BlueprintFeatureReference[])Get(basic, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>()).Select(r => r.Guid)),
            string.Join(",", ((BlueprintFeatureReference[])Get(basic, "m_Features") ?? Array.Empty<BlueprintFeatureReference>()).Select(r => r.Guid)) };
    }
}
