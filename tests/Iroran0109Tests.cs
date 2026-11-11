// COTW-0109 B1 directed checks: contract C-0109-20261111-r1 Q06, cases
// C0109-10/11/12. Real RuleRollD20 events through the native OnTrigger/Roll and
// public Reroll(EntityFact,bool) entries, with the dice RNG fixed at the
// RulebookEvent.Dice.D boundary. Aura sources are registered on the real
// per-unit part with constructed (unattached) buffs; the buff/area component
// lifecycle is exercised by the B3 registration checks, not here.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

internal static class Iroran0109Tests
{
    private static Assembly candidate;
    private static int checks;
    private static readonly Queue<int> rolls = new Queue<int>();
    private static int draws;
    private static readonly List<Tuple<UnitEntityData, int, Buff>> registered = new List<Tuple<UnitEntityData, int, Buff>>();
    private static Type T(string name) => candidate.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    private static object Get(object o, string f)
    {
        var field = AccessTools.Field(o.GetType(), f);
        if (field != null) return field.GetValue(o);
        return AccessTools.Property(o.GetType(), f)?.GetValue(o, null);
    }
    private static void Set(object o, string f, object v) =>
        (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).SetValue(o, v);
    private static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("0109 CHECK FAILED: " + message);
        checks++;
        Console.WriteLine("0109 CHECK: " + message);
    }
    private static bool FixedDice(DiceFormula __0, ref int __result)
    {
        if (rolls.Count == 0) throw new Exception("0109 CHECK FAILED: RNG over-draw, the primitive consumed more dice than the batch allows");
        draws++;
        __result = rolls.Dequeue();
        return false;
    }
    private static void Expect(params int[] values) { rolls.Clear(); foreach (var v in values) rolls.Enqueue(v); draws = 0; }
    private static UnitEntityData trialUnit;
    private static UnitProgressionData trialProgression;
    private static bool TrialCaster(ref UnitEntityData __result)
    {
        if (trialUnit == null) return true;
        __result = trialUnit;
        return false;
    }
    private static bool TrialProgression(ref UnitProgressionData __result)
    {
        if (trialProgression == null) return true;
        __result = trialProgression;
        return false;
    }
    private static int TrialInsight(BlueprintBuff mark, AttackBonusAgainstTarget attack, BlueprintCharacterClass paladin, int level)
    {
        trialUnit = Unit();
        var descriptor = (UnitDescriptor)Get(trialUnit, "Descriptor");
        trialProgression = new UnitProgressionData();
        var classes = (List<ClassData>)Get(trialProgression, "Classes");
        if (classes == null)
        {
            classes = new List<ClassData>();
            Set(trialProgression, "Classes", classes);
        }
        classes.Add(new ClassData(paladin) { Level = level });
        Set(descriptor, "Progression", trialProgression);
        var harmony = new Harmony("cotw-0109-trial-rank");
        harmony.Patch(AccessTools.PropertyGetter(typeof(MechanicsContext), "MaybeCaster"),
            prefix: new HarmonyMethod(typeof(Iroran0109Tests), nameof(TrialCaster)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "Progression"),
            prefix: new HarmonyMethod(typeof(Iroran0109Tests), nameof(TrialProgression)));
        try
        {
            var context = new MechanicsContext(trialUnit, descriptor, mark);
            context.RecalculateRanks();
            return attack.Value.Calculate(context);
        }
        finally
        {
            harmony.UnpatchAll("cotw-0109-trial-rank");
            trialUnit = null;
            trialProgression = null;
        }
    }

    private static UnitEntityData Unit(UnitLifeState life = UnitLifeState.Conscious)
    {
        var unit = Empty<UnitEntityData>();
        var descriptor = Empty<UnitDescriptor>();
        Set(unit, "<Descriptor>k__BackingField", descriptor);
        Set(descriptor, "<Unit>k__BackingField", unit);
        Set(unit, "Parts", new EntityPartsManager(unit));
        var state = Empty<UnitState>();
        Set(state, "<LifeState>k__BackingField", life);
        Set(descriptor, "State", state);
        return unit;
    }
    private static object Part(UnitEntityData unit)
    {
        var manager = (EntityPartsManager)Get(unit, "Parts");
        return AccessTools.Method(typeof(EntityPartsManager), "Ensure").MakeGenericMethod(T("IroranRerolls0109+AuraPart")).Invoke(manager, null);
    }
    private static object Kind(int value) => Enum.ToObject(T("IroranRerolls0109+AuraKind"), value);
    private static Buff Aura(UnitEntityData unit, int kind, UnitEntityData emitter)
    {
        var blueprint = new BlueprintBuff();
        var buff = new Buff(blueprint, new MechanicsContext(emitter, emitter.Descriptor, blueprint), null);
        AccessTools.Method(T("IroranRerolls0109+AuraPart"), "Add")
            .Invoke(Part(unit), new[] { Kind(kind), buff, emitter });
        registered.Add(Tuple.Create(unit, kind, buff));
        return buff;
    }
    private static void ClearAuras()
    {
        var remove = AccessTools.Method(T("IroranRerolls0109+AuraPart"), "Remove");
        foreach (var entry in registered) remove.Invoke(Part(entry.Item1), new[] { Kind(entry.Item2), entry.Item3 });
        registered.Clear();
    }
    private static EntityFact Source() => Empty<Feature>();
    private static int Amount(RuleRollDice roll) => (int)AccessTools.Field(typeof(RuleRollDice), "m_RerollAmount").GetValue(roll);
    private static RuleRollD20 Roll(UnitEntityData unit) { var r = new RuleRollD20(unit); r.OnTrigger(new RulebookEventContext()); return r; }
    private static bool HistoryIs(RuleRollDice roll, params int[] expected) => roll.RollHistory != null && roll.RollHistory.SequenceEqual(expected);

    internal static void Run(Assembly assembly, string reportPath)
    {
        candidate = assembly;
        var harmony = new Harmony("CotWIroran0109.Directed");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Iroran0109Tests), nameof(FixedDice))));
            harmony.CreateClassProcessor(T("IroranRerolls0109+RollPatch")).Patch();
            harmony.CreateClassProcessor(T("IroranRerolls0109+LateRerollPatch")).Patch();
            Cases();
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        var report = new JObject
        {
            ["case"] = "iroran0109",
            ["contract"] = "C-0109-20261111-r1",
            ["workPackage"] = "COTW-0109",
            ["stage"] = "B1 reroll primitive (Q06, C0109-10/11/12); full archetype registration not claimed",
            ["assertionsPassed"] = checks,
            ["scope"] = "Real RuleRollD20 native entries with fixed RNG at RulebookEvent.Dice.D; constructed aura buffs on the real per-unit part. No game, no UMM, no area-scene lifecycle."
        };
        if (reportPath == "-") Console.WriteLine(report.ToString());
        else
        {
            var path = System.IO.Path.GetFullPath(reportPath);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, report.ToString());
        }
        Console.WriteLine("PASS: " + checks + " Iroran 0109 B1 reroll assertions. Standalone managed verification, not a game test.");
    }

    private static void Cases()
    {
        var paladin = Unit();
        var ally = Unit();
        var other = Unit();

        // C0109-10: aggregate take-worst with ally Excellence, fixed RNG 19/2/13.
        Aura(ally, 0, paladin);
        var r = new RuleRollD20(ally);
        r.AddReroll(1, false, Source());
        Expect(19, 2, 13);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 13 && HistoryIs(r, 19, 2, 13) && draws == 3, "C0109-10 ally Excellence second-lowest of [19,2,13] is 13, raw history kept");
        ClearAuras();

        // Same sources without any aura stay native take-worst.
        r = new RuleRollD20(ally);
        r.AddReroll(1, false, Source());
        Expect(19, 2);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 2 && HistoryIs(r, 19, 2) && draws == 2, "C0109-10 no aura keeps native take-worst 2");

        // Self Excellence with only negative sources rolls the base die alone.
        Aura(paladin, 0, paladin);
        r = new RuleRollD20(paladin);
        r.AddReroll(1, false, Source());
        Expect(19);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 19 && HistoryIs(r, 19) && draws == 1 && Amount(r) == 0 && (r.Rerolls == null || r.Rerolls.Count == 0),
            "C0109-10 self immunity filters the only negative source, base die 19 alone, no admitted sources recorded");

        // Self mixed sources: negatives drop, positives survive even though the
        // native signed sum was negative.
        r = new RuleRollD20(paladin);
        var negative = Source();
        var positive = Source();
        r.AddReroll(2, false, negative);
        r.AddReroll(1, true, positive);
        Expect(5, 17);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 17 && HistoryIs(r, 5, 17) && draws == 2 && Amount(r) == 1 && r.Rerolls.Count == 1 && r.Rerolls[0].Source == positive,
            "C0109-10 self mixed keeps the positive source, take-best 17, only the admitted source recorded");
        ClearAuras();

        // Ally net zero: signed merge cancels, no enhancement die.
        Aura(ally, 0, paladin);
        r = new RuleRollD20(ally);
        r.AddReroll(1, false, Source());
        r.AddReroll(1, true, Source());
        Expect(7);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 7 && HistoryIs(r, 7) && draws == 1, "C0109-10 ally net zero stays one base die");

        // Ally multiple negatives: second-lowest over every sample.
        r = new RuleRollD20(ally);
        r.AddReroll(2, false, Source());
        Expect(19, 2, 17, 13);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 13 && HistoryIs(r, 19, 2, 17, 13) && draws == 4, "C0109-10 ally two negatives take second-lowest 13 of [19,2,17,13]");
        ClearAuras();

        // C0109-11: Perfection take-best adds exactly one die.
        Aura(ally, 1, paladin);
        r = new RuleRollD20(ally);
        r.AddReroll(1, true, Source());
        Expect(2, 19, 13);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 19 && HistoryIs(r, 2, 19, 13) && draws == 3, "C0109-11 Perfection take-max of [2,19,13] is 19, exactly one extra die");

        // Overlapping same-kind auras from two emitters still add one die.
        Aura(ally, 1, other);
        r = new RuleRollD20(ally);
        r.AddReroll(1, true, Source());
        Expect(2, 19, 13);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 19 && draws == 3, "C0109-11 two Perfection emitters do not stack a second extra die");

        // Several positive sources still get only one Perfection die.
        r = new RuleRollD20(ally);
        r.AddReroll(1, true, Source());
        r.AddReroll(1, true, Source());
        Expect(2, 19, 13, 5);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 19 && HistoryIs(r, 2, 19, 13, 5) && draws == 4, "C0109-11 two positive sources merge natively, Perfection adds one die");

        // Zero rerolls never grant a free die.
        r = new RuleRollD20(ally);
        Expect(4);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 4 && draws == 1, "C0109-11 zero rerolls keep one die under Perfection");
        ClearAuras();

        // PreRollDice is consumed once and reused as the base die.
        Aura(ally, 0, paladin);
        r = new RuleRollD20(ally);
        Expect(19, 2, 13);
        Check(r.PreRollDice() == 19, "C0109-11 PreRollDice draws the base 19 once");
        r.AddReroll(1, false, Source());
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 13 && HistoryIs(r, 19, 2, 13) && draws == 3, "C0109-11 pre-rolled base is not redrawn, Excellence batch still 13");

        // Fixed Override keeps the native no-roll path.
        r = new RuleRollD20(ally, 15);
        r.AddReroll(1, false, Source());
        Expect();
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 15 && draws == 0, "C0109-11 ResultOverride 15 skips rolling entirely");

        // Fake rolls stay native even with an active aura.
        r = new RuleRollD20(ally) { IsFake = true };
        r.AddReroll(1, false, Source());
        Expect(19, 2);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 2 && draws == 2, "C0109-11 fake D20 keeps native take-worst, no aura die");

        // Non-D20 dice are never touched.
        var d6 = new RuleRollDice(ally, new DiceFormula(1, DiceType.D6));
        d6.AddReroll(1, false, Source());
        Expect(5, 3);
        d6.OnTrigger(new RulebookEventContext());
        Check(d6.Result == 3 && draws == 2, "C0109-11 non-D20 roll keeps native merge, no aura die");

        // ReplaceOneWithMax: raw history, effective merge values.
        r = new RuleRollD20(ally) { ReplaceOneWithMax = true };
        r.AddReroll(1, false, Source());
        Expect(1, 1, 13);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 13 && HistoryIs(r, 1, 1, 13) && draws == 3,
            "C0109-11 ReplaceOneWithMax Excellence samples [1,20,13] take 13, raw history [1,1,13]");
        ClearAuras();
        r = new RuleRollD20(ally) { ReplaceOneWithMax = true };
        r.AddReroll(1, false, Source());
        Expect(1, 1);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 20 && HistoryIs(r, 1, 1) && r.ReplacedOne, "C0109-11 native ReplaceOneWithMax final 1 reads as 20 without aura");
        Aura(ally, 1, paladin);
        r = new RuleRollD20(ally) { ReplaceOneWithMax = true };
        r.AddReroll(1, true, Source());
        Expect(1, 5, 1);
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 20 && HistoryIs(r, 1, 5, 1) && draws == 3, "C0109-11 Perfection extra die converts 1 to 20 before take-max");
        ClearAuras();

        // C0109-12: late public Reroll entry. Self negative request draws nothing.
        Aura(paladin, 0, paladin);
        Expect(10);
        r = Roll(paladin);
        int drawsBefore = draws;
        r.Reroll(Source(), false);
        Check(r.Result == 10 && HistoryIs(r, 10) && draws == drawsBefore && Amount(r) == 0 && (r.Rerolls == null || r.Rerolls.Count == 0),
            "C0109-12 self negative late Reroll is rejected whole: no record, no draw");
        ClearAuras();

        // Ally late negative: batch [prior,new,extra], history preserved.
        Aura(ally, 0, paladin);
        Expect(19, 2, 13, 7, 16);
        r = Roll(ally);
        r.Reroll(Source(), false);
        Check(r.Result == 13 && HistoryIs(r, 19, 2, 13) && draws == 3, "C0109-12 ally late negative batch [19,2,13] takes 13");
        // A second late batch judges from the prior valid result, not old history.
        r.Reroll(Source(), false);
        Check(r.Result == 13 && HistoryIs(r, 19, 2, 13, 7, 16) && draws == 5, "C0109-12 second late batch [13,7,16] takes 13, earlier history untouched");
        ClearAuras();

        // Ally late positive under Perfection.
        Aura(ally, 1, paladin);
        Expect(2, 19, 13);
        r = Roll(ally);
        r.Reroll(Source(), true);
        Check(r.Result == 19 && HistoryIs(r, 2, 19, 13) && draws == 3, "C0109-12 ally late positive batch [2,19,13] takes 19");
        ClearAuras();

        // Self late positive under Perfection also gets the extra die.
        Aura(paladin, 1, paladin);
        Expect(2, 19, 13);
        r = Roll(paladin);
        r.Reroll(Source(), true);
        Check(r.Result == 19 && HistoryIs(r, 2, 19, 13) && draws == 3, "C0109-12 self late positive under Perfection takes 19");
        ClearAuras();

        // Emitter unconscious restores native behaviour at both entries.
        var sleeper = Unit(UnitLifeState.Unconscious);
        Aura(ally, 0, sleeper);
        Expect(19, 2, 2);
        r = new RuleRollD20(ally);
        r.AddReroll(1, false, Source());
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 2 && draws == 2, "C0109-12 unconscious emitter grants no aggregate aura die");
        r.Reroll(Source(), false);
        Check(r.Result == 2 && draws == 3 && HistoryIs(r, 19, 2, 2), "C0109-12 unconscious emitter grants no late aura die");
        ClearAuras();

        // Suppressed aura buff restores native behaviour.
        var suppressor = Aura(ally, 0, paladin);
        suppressor.IsSuppressed = true;
        Expect(19, 2);
        r = new RuleRollD20(ally);
        r.AddReroll(1, false, Source());
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 2 && draws == 2, "C0109-12 suppressed aura buff grants no aura die");
        suppressor.IsSuppressed = false;

        // Removed source restores native behaviour.
        ClearAuras();
        Expect(19, 2);
        r = new RuleRollD20(ally);
        r.AddReroll(1, false, Source());
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 2 && draws == 2, "C0109-12 removed aura source grants no aura die");

        // Self immunity takes priority over an ally Excellence on the same roller.
        Aura(paladin, 0, paladin);
        Aura(paladin, 0, other);
        Expect(19);
        r = new RuleRollD20(paladin);
        r.AddReroll(1, false, Source());
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 19 && draws == 1, "C0109-12 self immunity wins over ally Excellence on the same roller");
        ClearAuras();

        // Two Excellence emitters on one ally still enhance once.
        Aura(ally, 0, paladin);
        Aura(ally, 0, other);
        Expect(19, 2, 13);
        r = new RuleRollD20(ally);
        r.AddReroll(1, false, Source());
        r.OnTrigger(new RulebookEventContext());
        Check(r.Result == 13 && draws == 3, "C0109-12 two Excellence emitters do not stack a second extra die");

        // State does not leak across events: a fresh roll on the same roller
        // starts a fresh history and source list.
        Expect(6);
        r = Roll(ally);
        Check(r.Result == 6 && HistoryIs(r, 6) && (r.Rerolls == null || r.Rerolls.Count == 0), "C0109-12 fresh event starts clean history and sources");
        Check(rolls.Count == 0, "C0109-12 every queued die was consumed exactly once");
        ClearAuras();
    }

    // COTW-0109 B3: the archetype registration graph against the reviewed native
    // row fixture. The reroll primitive above covers C0109-10/11/12; these checks
    // cover the publication wiring for C0109-01..09 at the structure level the
    // isolated managed host can prove. Runtime unit behaviour (modifier stacks,
    // resource spending, enchant lifetimes) stays untested here and is recorded
    // as an untested boundary for D.
    private static BlueprintGuid Token(string field)
    {
        var module = T("IroranPaladin0109");
        return (BlueprintGuid)AccessTools.Method(T("PsychicAssets"), "Id").Invoke(null, new object[] { (string)AccessTools.Field(module, field).GetValue(null) });
    }
    private static BlueprintGuid G(string guid) => BlueprintGuid.Parse(guid);
    private static SimpleBlueprint Load(BlueprintsCache cache, BlueprintGuid guid) => cache.Load(guid);
    private static object Component(BlueprintScriptableObject bp, string typeName) =>
        bp.ComponentsArray.FirstOrDefault(c => c.GetType().Name == typeName);
    private static T Component<T>(BlueprintScriptableObject bp) where T : BlueprintComponent =>
        bp.ComponentsArray.OfType<T>().FirstOrDefault();
    private static IEnumerable<BlueprintFeatureBaseReference> EntryFeatures(LevelEntry[] entries, int level) =>
        entries.Where(e => e.Level == level).SelectMany(e => (IEnumerable<BlueprintFeatureBaseReference>)Get(e, "m_Features"));
    private static IEnumerable<BlueprintGuid> EntryGuids(LevelEntry[] entries, int level) =>
        EntryFeatures(entries, level).Select(r => r.Guid);

    internal static void RunRegistration(Assembly assembly, string reportPath, string nativeFixture)
    {
        candidate = assembly;
        checks = 0;
        var module = T("IroranPaladin0109");
        var data = JObject.Parse(System.IO.File.ReadAllText(nativeFixture));
        var cache = new BlueprintsCache();
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        PsychicExpansionTests.LoadNativeRows(cache, data, true);
        AccessTools.Method(module, "Register").Invoke(null, new object[] { cache });
        Console.WriteLine("0109 registration: publication complete, directed checks follow");

        var root = (BlueprintArchetype)Load(cache, Token("RootToken"));
        var paladin = (BlueprintCharacterClass)Load(cache, G("bfa11238e7ae3544bbeb4d0b92e897ec"));
        var iroriGuid = G("23a77a5985de08349820429ce1b5a234");
        var irori = (BlueprintFeature)Load(cache, iroriGuid);
        var archetypeRefs = (BlueprintArchetypeReference[])Get(paladin, "m_Archetypes");
        Check(root != null && root.GetParentClass() == paladin && archetypeRefs.Count(r => r?.Guid == root.AssetGuid) == 1,
            "C0109-01 archetype root attached to the native paladin class exactly once");

        // Complete frozen level table, exact levels and feature identities.
        var addLevels = new[] { 1, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 20 };
        var removeLevels = new[] { 1, 3, 4, 5, 7, 10, 11, 13, 16, 19 };
        Check(root.AddFeatures.Select(e => e.Level).OrderBy(l => l).SequenceEqual(addLevels) &&
              root.RemoveFeatures.Select(e => e.Level).OrderBy(l => l).SequenceEqual(removeLevels),
            "C0109-01 add/remove level tables carry exactly the contract levels");
        var expectedAdd = new Dictionary<int, string[]>
        {
            [1] = new[] { "DeityToken", "ProficienciesToken", null, "UnarmedToken", "ConfidentDefenseToken", "TrialToken" },
            [3] = new[] { "ExcellenceToken" },
            [4] = new string[] { "KiToken", null, "TrialExtraToken" },
            [5] = new[] { "DivineBodyToken" },
            [7] = new string[] { null },
            [8] = new string[] { null, "TrialExtraToken" },
            [9] = new[] { "DivineBodyExtraUseToken" },
            [10] = new string[] { null },
            [11] = new string[] { "PerfectionToken", null },
            [12] = new[] { "TrialExtraToken" },
            [13] = new[] { "DivineBodyExtraUseToken" },
            [14] = new string[] { null },
            [16] = new string[] { null, "TrialExtraToken" },
            [17] = new string[] { null, "DivineBodyExtraUseToken" },
            [20] = new string[] { null, "TrialExtraToken" }
        };
        var addNative = new Dictionary<int, string[]>
        {
            [1] = new[] { "7812ad3672a4b9a4fb894ea402095167" },
            [4] = new[] { "1188005ee3160f84f8bed8b19a7d46cf" },
            [7] = new[] { "7b657938fde78b14cae10fc0d3dcb991" },
            [8] = new[] { "c8db0772b7059ec4eabe55b7e0e79824" },
            [10] = new[] { "34439e527a8f5fb4588024e71960dd42" },
            [11] = new[] { "d2f45a2034d4f7643ba1a450bc5c4c06" },
            [14] = new[] { "6d73f49b602e29a43a6faa2ea1e4a425" },
            [16] = new[] { "ddc10a3463bd4d54dbcbe993655cf64e" },
            [17] = new[] { "f17c3ba33bb44d44782cb3851d823011" },
            [20] = new[] { "b936ee90c070edb46bd76025dc1c5936" }
        };
        var removeNative = new Dictionary<int, string[]>
        {
            [1] = new[] { "b10ff88c03308b649b50c31611c2fefb", "3a6db57fce75b0244a6a5819528ddf26" },
            [3] = new[] { "e45ab30f49215054e83b4ea12165409f" },
            [4] = new[] { "cb6d55dda5ab906459d18a435994a760", "0f5c99ffb9c084545bbbe960b825d137" },
            [5] = new[] { "ad7dc4eba7bf92f4aba23f716d7a9ba6" },
            [7] = new[] { "0f5c99ffb9c084545bbbe960b825d137" },
            [10] = new[] { "0f5c99ffb9c084545bbbe960b825d137" },
            [11] = new[] { "9f13fdd044ccb8a439f27417481cb00e" },
            [13] = new[] { "0f5c99ffb9c084545bbbe960b825d137" },
            [16] = new[] { "0f5c99ffb9c084545bbbe960b825d137" },
            [19] = new[] { "0f5c99ffb9c084545bbbe960b825d137" }
        };
        foreach (var level in addLevels)
        {
            var expected = (expectedAdd[level] ?? new string[0]).Where(t => t != null).Select(Token)
                .Concat(addNative.TryGetValue(level, out var natives) ? natives.Select(G) : Enumerable.Empty<BlueprintGuid>()).ToList();
            var actual = EntryGuids(root.AddFeatures, level).ToList();
            Check(actual.Count == expected.Count && !actual.Except(expected).Any(),
                "C0109-01 add level " + level + " carries the exact frozen feature identities");
        }
        foreach (var level in removeLevels)
        {
            var expected = removeNative[level].Select(G).ToList();
            var actual = EntryGuids(root.RemoveFeatures, level).ToList();
            Check(actual.Count == expected.Count && !actual.Except(expected).Any(),
                "C0109-01 remove level " + level + " carries the exact frozen feature identities");
        }

        // Deity prerequisites: one PrerequisiteNoFeature per other native deity
        // plus the Irori alignment mask; existing religion is never replaced.
        var deities = (BlueprintFeatureSelection)Load(cache, G("59e7a76987fe3b547b9cce045f4db3e4"));
        var allDeities = (BlueprintFeatureReference[])Get(deities, "m_AllFeatures");
        var otherDeities = allDeities.Where(d => d != null && d.Guid != iroriGuid).Select(d => d.Guid).Distinct().ToList();
        var noFeatures = root.ComponentsArray.OfType<PrerequisiteNoFeature>().ToArray();
        Check(noFeatures.Length == otherDeities.Count &&
              noFeatures.Select(p => ((BlueprintFeatureReference)Get(p, "m_Feature")).Guid).All(g => otherDeities.Contains(g)) &&
              root.ComponentsArray.Length == otherDeities.Count + 1,
            "C0109-01 one PrerequisiteNoFeature per other native deity and nothing else on the root");
        var alignment = root.ComponentsArray.OfType<PrerequisiteAlignment>().Single();
        Check(alignment.Alignment == (AlignmentMaskType.Lawful | AlignmentMaskType.TrueNeutral),
            "C0109-01 Irori alignment mask Lawful|TrueNeutral preserved");
        var deity = (BlueprintFeature)Load(cache, Token("DeityToken"));
        var grant = Component<AddFeatureIfHasFact>(deity);
        Check(grant != null && grant.Not &&
              ((BlueprintUnitFactReference)Get(grant, "m_CheckedFact")).Guid == iroriGuid &&
              ((BlueprintUnitFactReference)Get(grant, "m_Feature")).Guid == iroriGuid,
            "C0109-01 level 1 grants the real Irori fact once and adopts an existing choice");
        Check(root.ReplaceClassSkills &&
              root.ClassSkills.Contains(StatType.SkillAthletics) && root.ClassSkills.Contains(StatType.SkillMobility) &&
              paladin.ClassSkills.All(s => root.ClassSkills.Contains(s)) &&
              root.ClassSkills.Length == paladin.ClassSkills.Concat(new[] { StatType.SkillAthletics, StatType.SkillMobility }).Distinct().Count(),
            "C0109-01 class skills add Athletics and Mobility with dedup, native skills preserved");
        var headers = (BlueprintFeatureBaseReference[])Get(paladin.Progression, "m_UIDeterminatorsGroup");
        var defense = (BlueprintFeature)Load(cache, Token("ConfidentDefenseToken"));
        Check(headers.Count(h => h?.Guid == deity.AssetGuid) == 1 && headers.Count(h => h?.Guid == defense.AssetGuid) == 1,
            "C0109-01 deity and Confident Defense appear once in the native determinators");
        var uiGroup = paladin.Progression.UIGroups.FirstOrDefault(g =>
            g != null && ((IEnumerable<BlueprintFeatureBaseReference>)Get(g, "m_Features")).Any(r => r?.Guid == Token("TrialToken")));
        Check(uiGroup != null && ((IEnumerable<BlueprintFeatureBaseReference>)Get(uiGroup, "m_Features")).Select(r => r.Guid)
                  .SequenceEqual(new[] { Token("DeityToken"), Token("ProficienciesToken"), Token("UnarmedToken"), Token("ConfidentDefenseToken"), Token("TrialToken"), Token("KiToken"), Token("DivineBodyToken"), Token("ExcellenceToken"), Token("PerfectionToken") }),
            "C0109-01 one new UI group holds exactly the nine added pillars");

        // Personal Trial: uses, mark identity, payment and value sources.
        var trial = (BlueprintFeature)Load(cache, Token("TrialToken"));
        var trialAbility = (BlueprintAbility)Load(cache, Token("TrialAbilityToken"));
        var mark = (BlueprintBuff)Load(cache, Token("TrialMarkToken"));
        var trialResource = (BlueprintAbilityResource)Load(cache, Token("TrialResourceToken"));
        var trialExtra = (BlueprintFeature)Load(cache, Token("TrialExtraToken"));
        var trialAmount = Get(trialResource, "m_MaxAmount");
        Check((int)Get(trialAmount, "BaseValue") == 1 && !(bool)Get(trialAmount, "IncreasedByLevelStartPlusDivStep"),
            "C0109-03 trial pool has no own scaling; the 4/8/12/16/20 extra-use features provide 1..6");
        var trialExtraRow = data["requiredNative"].Single(r => (string)r["blueprint"]["AssetId"] == "0f5c99ffb9c084545bbbe960b825d137");
        // LoadNativeRows does not materialize Ranks, so the archive row carries
        // the native value and the clone is checked through its redirect.
        Check((int)trialExtraRow["blueprint"]["Data"]["Ranks"] == 10 &&
              ((BlueprintAbilityResourceReference)Get(trialExtra.GetComponent<IncreaseResourceAmount>(), "m_Resource")).Guid == trialResource.AssetGuid,
            "C0109-03 trial extra uses rank up from the native donor and raise only this package's pool");
        Check(mark.Stacking == StackingType.Stack && (Convert.ToInt64(Get(mark, "m_Flags")) & 0x10) != 0 &&
              mark.ComponentsArray.Length == 6,
            "C0109-03 mark buff stacks per caster, is removed on rest and carries the five contract components plus its rank");
        var attack = Component<AttackBonusAgainstTarget>(mark);
        var acBonus = Component<ACBonusAgainstTarget>(mark);
        var damage = Component<DamageBonusAgainstTarget>(mark);
        var missingCaster = Component<RemoveBuffIfCasterIsMissing>(mark);
        Check(Component<UniqueBuff>(mark) != null && missingCaster != null && missingCaster.RemoveOnCasterDeath &&
              attack.CheckCaster && attack.Descriptor == ModifierDescriptor.Insight && attack.Value.ValueType == ContextValueType.Rank &&
              acBonus.CheckCaster && acBonus.Descriptor == ModifierDescriptor.Insight && acBonus.Value.ValueType == ContextValueType.Rank &&
              damage.CheckCaster && damage.ApplyToSpellDamage && damage.Value.ValueType == ContextValueType.Rank,
            "C0109-04 attack/AC/saving-throw-grade bonuses are insight rank values against the caster's own mark");
        Check(trialAbility.Type == AbilityType.Supernatural && trialAbility.ActionType == UnitCommand.CommandType.Swift &&
              trialAbility.Range == AbilityRange.Medium && trialAbility.CanTargetEnemies && !trialAbility.CanTargetFriends,
            "C0109-03 swift supernatural medium-range trial ability against one enemy, no alignment gate");
        var trialCost = trialAbility.GetComponent<AbilityResourceLogic>();
        var trialRank = trialAbility.GetComponent<ContextRankConfig>();
        Check((bool)Get(trialCost, "m_IsSpendResource") && trialCost.Amount == 1 &&
              ((BlueprintAbilityResourceReference)Get(trialCost, "m_RequiredResource")).Guid == trialResource.AssetGuid,
            "C0109-03 the ability spends exactly one point of this package's pool");
        Check(Enum.GetName(typeof(ContextRankProgression), Get(trialRank, "m_Progression")) == "OnePlusDivStep" &&
              (int)Get(trialRank, "m_StepLevel") == 4 &&
              ((BlueprintCharacterClassReference[])Get(trialRank, "m_Class")).Select(r => r.Guid).SequenceEqual(new[] { paladin.AssetGuid }),
            "C0109-03 value rank is 1 + paladin level / 4 through the paladin class");
        var markRank = mark.GetComponent<ContextRankConfig>();
        Check(markRank != null &&
              Enum.GetName(typeof(ContextRankProgression), Get(markRank, "m_Progression")) == "OnePlusDivStep" &&
              (int)Get(markRank, "m_StepLevel") == 4 &&
              ((BlueprintCharacterClassReference[])Get(markRank, "m_Class")).Select(r => r.Guid).SequenceEqual(new[] { paladin.AssetGuid }),
            "D-F01 the mark buff itself carries the paladin 1 + level/4 rank");
        foreach (int level in new[] { 1, 4, 20 })
            Check(TrialInsight(mark, attack, paladin, level) == 1 + level / 4,
                "D-F01 a real mark-buff context gives insight " + (1 + level / 4) + " at paladin level " + level);
        var trialApply = trialAbility.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
        Check(trialApply.Permanent && ((BlueprintBuffReference)Get(trialApply, "m_Buff")).Guid == mark.AssetGuid,
            "C0109-03 the cast applies this package's mark permanently to the selected target");
        var saves = Component(trial, "IroranPersonalTrial0109");
        var trialFacts = trial.GetComponent<AddFacts>();
        Check(saves != null && ((BlueprintBuffReference)Get(saves, "Mark")).Guid == mark.AssetGuid &&
              ((BlueprintCharacterClassReference)Get(saves, "Paladin")).Guid == paladin.AssetGuid &&
              trialFacts != null && trialFacts.Facts.Select(f => f.AssetGuid).SequenceEqual(new[] { trialAbility.AssetGuid }),
            "C0109-04 the save-bonus component tracks this mark and the paladin class");
        var trialGrant = trial.GetComponent<AddAbilityResources>();
        Check(trialGrant != null &&
              ((BlueprintAbilityResourceReference)Get(trialGrant, "m_Resource")).Guid == trialResource.AssetGuid,
            "C0109-03 the feature grants only this package's pool");

        // Ki pool: shared native Charisma pool, extra attack donor and Ignore DR.
        var ki = (BlueprintFeature)Load(cache, Token("KiToken"));
        var kiComponent = Component(ki, "IroranKi0109");
        var kiGuid = G("7d002c1025fbfe2458f1509bf7a89ce1");
        Check(kiComponent != null &&
              ((BlueprintAbilityResourceReference)Get(kiComponent, "Ki")).Guid == kiGuid &&
              ((BlueprintUnitFactReference)Get(kiComponent, "ScaledFistKi")).Guid == G("ae98ab7bda409ef4bb39149a212d6732"),
            "C0109-07 ki maximum handler targets the native Charisma pool and the real Scaled Fist ki source");
        var ignoreDrAbility = (BlueprintAbility)Load(cache, Token("KiIgnoreDrAbilityToken"));
        var ignoreDrBuff = (BlueprintBuff)Load(cache, Token("KiIgnoreDrBuffToken"));
        var kiFacts = ki.GetComponent<AddFacts>();
        Check(kiFacts.Facts.Select(f => f.AssetGuid).SequenceEqual(new[] { G("ca948bb4ce1a2014fbf4d8d44b553074"), ignoreDrAbility.AssetGuid }),
            "C0109-07 the feature grants the native Scaled Fist extra attack and this package's Ignore DR");
        var ignoreDrCost = ignoreDrAbility.GetComponent<AbilityResourceLogic>();
        Check(ignoreDrAbility.Type == AbilityType.Supernatural && ignoreDrAbility.ActionType == UnitCommand.CommandType.Free &&
              ignoreDrAbility.Range == AbilityRange.Personal && ignoreDrAbility.CanTargetSelf &&
              (bool)Get(ignoreDrCost, "m_IsSpendResource") && ignoreDrCost.Amount == 1 &&
              ((BlueprintAbilityResourceReference)Get(ignoreDrCost, "m_RequiredResource")).Guid == kiGuid,
            "C0109-04 Ignore DR is a free self ability spending one point of the same shared pool");
        var ignoreDrApply = ignoreDrAbility.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
        Check(ignoreDrApply.AsChild && !ignoreDrApply.IsFromSpell && ignoreDrApply.DurationValue.BonusValue.Value == 1 &&
              ignoreDrApply.DurationValue.Rate == DurationRate.Rounds &&
              ((BlueprintBuffReference)Get(ignoreDrApply, "m_Buff")).Guid == ignoreDrBuff.AssetGuid,
            "C0109-04 Ignore DR applies its own buff for one round as a child");
        var ignoreDrMark = Component(ignoreDrBuff, "IroranTrialMark0109");
        Check(ignoreDrMark != null && ((BlueprintBuffReference)Get(ignoreDrMark, "Mark")).Guid == mark.AssetGuid,
            "C0109-08 DR bypass only reads this package's own trial mark");

        // Unarmed: six private tiers over the native fists, native base feature
        // detection, Improved Unarmed Strike as the native fact.
        var unarmed = (BlueprintFeature)Load(cache, Token("UnarmedToken"));
        var tierWeapons = new[] { "f60c5a820b69fb243a4cce5d1d07d06e", "9e87f72f8069310469d4431e925f035a", "ee31a19c8aa965f46a90ae02d59d4af7", "889e2927a67bd724ea1c203fe1b8b652", "84af5e0b4f5f251449aac2b530fab3d0", "7afc18ecb2cd96541b5cb8395f40d48c" };
        var manager = Component(unarmed, "IroranUnarmed0109");
        var tiers = (BlueprintUnitFactReference[])Get(manager, "Tiers");
        Check(tiers.Length == 6 && ((BlueprintUnitFactReference)Get(manager, "MonkUnarmedStrike")).Guid == G("c3fbeb2ffebaaa64aa38ce7a0bb18fb0"),
            "C0109-05 the manager watches the real monk unarmed base feature and six own tiers");
        for (int i = 0; i < 6; i++)
        {
            var tier = (BlueprintFeature)Load(cache, tiers[i].Guid);
            var @override = Component<EmptyHandWeaponOverride>(tier);
            Check(@override != null && @override.IsPermanent && @override.IsMonkUnarmedStrike &&
                  ((BlueprintItemWeaponReference)Get(@override, "m_Weapon")).Guid == G(tierWeapons[i]) &&
                  tier.HideInUI && tier.HideInCharacterSheetAndLevelUp,
                "C0109-05 tier " + (i + 1) + " holds the native monk fist via native EmptyHandWeaponOverride, hidden");
        }

        // Confident Defense: own dodge component plus native Charisma recalc.
        var recalculate = Component<RecalculateOnStatChange>(defense);
        Check(kiComponent != null && recalculate != null && recalculate.Stat == StatType.Charisma && !recalculate.UseKineticistMainStat,
            "C0109-06 Confident Defense recalculates with the native Charisma watcher");
        Check(((BlueprintArchetypeReference)Get(Component(defense, "IroranConfidentDefense0109"), "ScaledFist")).Guid == G("5868fc82eb11a4244926363983897279") &&
              ((BlueprintBuffReference)Get(Component(defense, "IroranConfidentDefense0109"), "ScaledFistCharismaAC")).Guid == G("64acb179cc6a4f19bb3513d094b28d02"),
            "C0109-06 the top-up consults the real Scaled Fist archetype and its native Charisma AC buff");

        // Divine Body: the native bond graph with a private pool and unarmed gate.
        var body = (BlueprintFeature)Load(cache, Token("DivineBodyToken"));
        var bondAbility = (BlueprintAbility)Load(cache, Token("DivineBodyAbilityToken"));
        var bondResource = (BlueprintAbilityResource)Load(cache, Token("DivineBodyResourceToken"));
        var bodyExtra = (BlueprintFeature)Load(cache, Token("DivineBodyExtraUseToken"));
        Check(!bondAbility.NeedEquipWeapons && bondAbility.CanTargetSelf && !bondAbility.CanTargetFriends && !bondAbility.CanTargetEnemies &&
              Component(bondAbility, "IroranDivineBodyCasterCheck0109") != null,
            "C0109-09 the bond switch cast no longer requires an equipped weapon and carries the unarmed gate");
        var bondCost = bondAbility.GetComponent<AbilityResourceLogic>();
        Check((bool)Get(bondCost, "m_IsSpendResource") && bondCost.Amount == 1 &&
              ((BlueprintAbilityResourceReference)Get(bondCost, "m_RequiredResource")).Guid == bondResource.AssetGuid &&
              (int)Get(Get(bondResource, "m_MaxAmount"), "BaseValue") == 1,
            "C0109-09 the bond spends this package's own pool, never the native paladin pool");
        var bodyFacts = body.GetComponent<AddFacts>();
        Check(bodyFacts.Facts.Select(f => f.AssetGuid).SequenceEqual(new[] { bondAbility.AssetGuid, G("7902941ef70a0dc44bcfc174d6193386"), G("27d76f1afda08a64d897cc81201b5218") }),
            "C0109-09 the feature grants the clone plus the native Flaming and Keen choices");
        Check(Component(body, "IroranDivineBodyCleanup0109") != null &&
              ((BlueprintAbilityResourceReference)Get(body.GetComponent<AddAbilityResources>(), "m_Resource")).Guid == bondResource.AssetGuid,
            "C0109-09 source removal clears this package's enchants and grants only its own pool");
        var bondActions = bondAbility.GetComponent<AbilityEffectRunAction>().Actions.Actions;
        var enchant = bondActions.FirstOrDefault(a => a.GetType().Name == "IroranDivineBody0109");
        var durationApply = bondActions.OfType<ContextActionApplyBuff>().Single();
        Check(bondActions.Length == 2 && enchant != null &&
              (ActivatableAbilityGroup)Get(enchant, "Group") == ActivatableAbilityGroup.DivineWeaponProperty,
            "C0109-09 the enchant action drives the native DivineWeaponProperty budget");
        var defaults = (BlueprintItemEnchantmentReference[])Get(enchant, "DefaultEnchantments");
        Check(defaults.Select(r => r.Guid).SequenceEqual(new[] { "d704f90f54f813043a525f304f6c0050", "9e9bab3020ec5f64499e007880b37e52", "d072b841ba0668846adeb007f623bd6c", "6a6a0901d799ceb49b33d4851ff72132", "746ee366e50611146821d61e391edf16" }.Select(G)) &&
              ((ContextDurationValue)Get(enchant, "DurationValue")).Rate == DurationRate.Minutes,
            "C0109-09 the native temporary enhancement ladder and minute duration are reused");
        Check(durationApply.AsChild && !durationApply.IsFromSpell &&
              ((BlueprintBuffReference)Get(durationApply, "m_Buff")).Guid == G("bf570774501886f47b395a4bfe75eeb2") &&
              durationApply.DurationValue.Rate == DurationRate.Minutes,
            "C0109-09 the native bond duration marker is applied as a child for the same duration");
        var bondRank = bondAbility.GetComponent<ContextRankConfig>();
        Check(Enum.GetName(typeof(ContextRankProgression), Get(bondRank, "m_Progression")) == "AsIs" &&
              ((BlueprintCharacterClassReference[])Get(bondRank, "m_Class")).Select(r => r.Guid).SequenceEqual(new[] { paladin.AssetGuid }),
            "C0109-09 the bond duration rank is the paladin class level in minutes");
        Check(((BlueprintAbilityResourceReference)Get(bodyExtra.GetComponent<IncreaseResourceAmount>(), "m_Resource")).Guid == bondResource.AssetGuid,
            "C0109-09 the 9/13/17 additional uses raise only this package's pool");

        // Auras: the native carrier/area chain with the B1 reroll component.
        // m_TargetType is a private enum LoadNativeRows does not materialize, so
        // the targeting facts are asserted from the archive row itself.
        var areaRow = data["requiredNative"].Single(r => (string)r["blueprint"]["AssetId"] == "7ced0efa297bd5142ab749f6e33b112b");
        Check((string)areaRow["blueprint"]["Data"]["m_TargetType"] == "Ally" &&
              (bool)areaRow["blueprint"]["Data"]["AffectEnemies"] == false &&
              (double)areaRow["blueprint"]["Data"]["Size"]["m_Value"] == 13.0,
            "C0109-10/11 the donor area archive row targets allies only with the 13-foot radius");
        Check(AuraChecks(cache, "Excellence", 0) && AuraChecks(cache, "Perfection", 1),
            "C0109-10/11 both auras reuse the native carrier and area with the reroll component on the ally buff");

        // Idempotency: a second registration re-validates and adds nothing.
        AccessTools.Method(module, "Register").Invoke(null, new object[] { cache });
        archetypeRefs = (BlueprintArchetypeReference[])Get(paladin, "m_Archetypes");
        Check(archetypeRefs.Count(r => r?.Guid == root.AssetGuid) == 1 && paladin.Progression.UIGroups.Count(g => ReferenceEquals(g, uiGroup)) == 1,
            "C0109-02 repeated registration adds no second archetype entry or UI group");

        // Rollback: a missing native donor must leave no trace behind.
        var broken = JObject.Parse(System.IO.File.ReadAllText(nativeFixture));
        broken["requiredNative"] = new JArray(((JArray)broken["requiredNative"]).Where(r => (string)r["blueprint"]["AssetId"] != "e8f21e5b58e0569468e420ebea456124").ToArray());
        var brokenCache = new BlueprintsCache();
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, brokenCache);
        PsychicExpansionTests.LoadNativeRows(brokenCache, broken, true);
        bool threw = false;
        try { AccessTools.Method(module, "Register").Invoke(null, new object[] { brokenCache }); }
        catch (TargetInvocationException) { threw = true; }
        var brokenPaladin = (BlueprintCharacterClass)Load(brokenCache, paladin.AssetGuid);
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        Check(threw && brokenPaladin != null &&
              ((BlueprintArchetypeReference[])Get(brokenPaladin, "m_Archetypes")).All(r => r?.Guid != root.AssetGuid) &&
              Load(brokenCache, Token("RootToken")) == null,
            "C0109-02 a missing donor aborts publication with no archetype attachment and no root asset");

        var report = new JObject
        {
            ["case"] = "iroran0109-registration",
            ["contract"] = "C-0109-20261111-r1",
            ["workPackage"] = "COTW-0109",
            ["stage"] = "B3 archetype publication graph (C0109-01..09 structure); reroll behaviour covered by the B1 case in the same run",
            ["assertionsPassed"] = checks,
            ["nativeFixture"] = nativeFixture,
            ["scope"] = "Directed registration checks only against the reviewed native row fixture; no Psychic or historical suites, no game or UMM. Runtime unit behaviour (modifier stacks, resource spending, enchant lifetimes, area-scene lifecycle) remains an untested boundary for D."
        };
        if (reportPath == "-") Console.WriteLine(report.ToString());
        else
        {
            var path = System.IO.Path.GetFullPath(reportPath);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, report.ToString());
        }
        Console.WriteLine("PASS: " + checks + " Iroran 0109 B3 registration assertions. Standalone managed verification, not a game test.");
    }

    private static bool AuraChecks(BlueprintsCache cache, string suffix, int kind)
    {
        var module = T("IroranPaladin0109");
        var token = (string)AccessTools.Field(module, "ExcellenceToken").GetValue(null);
        if (suffix == "Perfection") token = (string)AccessTools.Field(module, "PerfectionToken").GetValue(null);
        var feature = (BlueprintFeature)cache.Load((BlueprintGuid)AccessTools.Method(T("PsychicAssets"), "Id").Invoke(null, new object[] { token }));
        if (feature == null) return false;
        var aura = Component<AuraFeatureComponent>(feature);
        if (aura == null) return false;
        var carrierGuid = ((BlueprintBuffReference)Get(aura, "m_Buff")).Guid;
        var carrier = (BlueprintBuff)cache.Load(carrierGuid);
        if (carrier == null) return false;
        var add = Component<AddAreaEffect>(carrier);
        if (add == null) return false;
        var area = (BlueprintAbilityAreaEffect)cache.Load(((BlueprintAbilityAreaEffectReference)Get(add, "m_AreaEffect")).Guid);
        if (area == null) return false;
        var applied = Component<AbilityAreaEffectBuff>(area);
        var ally = (BlueprintBuff)cache.Load(((BlueprintBuffReference)Get(applied, "m_Buff")).Guid);
        if (ally == null) return false;
        var feet = (Feet)Get(area, "Size");
        var component = Component(ally, "AuraComponent");
        return feet.Value == 13 &&
               component != null && Convert.ToInt32(Get(component, "Kind")) == kind &&
               Convert.ToInt64(Get(applied, "CheckConditionEveryRound")) == 0;
    }
}