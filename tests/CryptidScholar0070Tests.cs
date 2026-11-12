using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Inspect;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using UnityEngine;
using CallOfTheWild.Wrath.Prototype;

// COTW-0070 B-stage directed checks, contract C-0070-20261112-r1.
// The host enters with COTW_0070_CASE=cryptidscholar0070 and routes straight
// here after the shared Psychic fixture; no historical behaviour suite runs.
//
// Scope: real game Managed blueprint types, the real production CryptidScholar
// registration and runtime, real native knowledge checks (skill/training/DC),
// real rulebook event dispatch for AC/saves/damage, real native resource
// spend/restore, real fact/buff lifecycles and real CanSelect through the
// production Harmony prefix.
//
// Declared fixture boundaries (NOT player-game acceptance): HA provider roots
// are staged at the frozen A-evidence identities; classification facts missing
// from the reviewed archive are staged at their audited GUIDs; the scene-dependent
// inspect-visibility gate, area scan, faction model and dice are controlled host
// inputs. No game, no UMM, no SaveManager/retraining controller, no full
// historical suite and no whole-library matrix.
internal static class CryptidScholar0070Tests
{
    private const string ClassId = "adb9e138bee9ecc4db246b64d563f900";
    private const string ProgressionId = "5eaea532547c7ce40a84563e15fd3440";
    private const string OldBookId = "961e6b4edd3c80e4a9fe4435deff6fe7";
    private const string TalentSelectionId = "da89239cc5859a1478007cd5e6d44f67";
    private const string PoisonResistanceId = "152ea6d8f279a73489c9dc0815e9e9b1";
    private const string PoisonImmunityId = "03d1877a5ffcbd34d9501b005434cb4f";
    private const string StudiedCombatFeatureId = "5a10de57f3d1a584c917f830c32742c9";
    private const string StudiedStrikeFeatureId = "0d545d60ed6aa1f48acd73c78676b94d";
    private const string InspirationFeatureId = "78681fd7849fd5c44ac67c0701d459ec";
    private const string InspirationResourceId = "60b4629bd1e92194382ae6a754f738ac";
    private const string BasicFeatsId = "247a4068296e8be42890143f451b4b45";

    private const string AberrationType = "3bec99efd9a363242a6c8d9957b75e91";
    private const string AnimalType = "a95311b3dc996964cbaa30ff9965aaf6";
    private const string ConstructType = "fd389783027d63343b4a5634bd81645f";
    private const string DragonType = "455ac88e22f55804ab87c2467deff1d6";
    private const string FeyType = "018af8005220ac94a9a4f47b3e9c2b4e";
    private const string MagicalBeastType = "625827490ea69d84d8e599a33929fdc6";
    private const string MonstrousHumanoidType = "57614b50e8d86b24395931fffc5e409b";
    private const string OutsiderType = "9054d3988d491d944ac144e27b6bc318";
    private const string PlantType = "706e61781d692a042b35941f14bc41c5";
    private const string UndeadType = "734a29b693e9ec346ba2951b27987e33";
    private const string VerminType = "09478937695300944a179530664e42ec";
    private const string GiantSubtype = "f9c388137f4faa74aac9065a68b56880";
    private const string LycanthropeType = "9a122a03b2548d94ba90546083d999c0";
    private const string ReptilianSubtype = "8b8809fd7f9039840ba2374310d7c634";
    private const string GoblinRace = "9d168ca7100e9314385ce66852385451";
    private const string HumanRace = "0a5d473ead98b0646b94495af250fdc4";
    private const string ElfRace = "25a5878d125338244896ebd3238226c8";
    private const string DwarfRace = "c4faf439f0e70bd40b5e36ee80d06be7";
    private const string AasimarRace = "b7f02ba92b363064fb873963bec275ee";
    private const string GnomeRace = "ef35a22c9a27da345a4528f0d5889157";
    private const string HalfElfRace = "b3646842ffbd01643ab4dac7479b20b0";
    private const string HalflingRace = "b0c3ef2729c498f47970bb50fa1acd30";
    private const string HalfOrcRace = "1dc20e195581a804890ddc74218bfd8e";
    private const string DemonOfMagic = "1b466705276e3124ab43f865e282c6e8";
    private const string DemonOfSlaughter = "2fe24d538bbcea34cbe9e8600d92bdd2";
    private const string DemonOfStrength = "561041cdb5887464883c55c75219a9dc";
    private const string NotADemon = "e2986f96fa1cd3b4f8d9dfd8a9907731";
    private const string InstantEnemyBuff = "82574f7d14a28e64fab8867fbaa17715";
    private const string PlantTypeFake = "b0efed5c0c814e3486fb8c8932af3bcc";

    private static readonly string[] ExcludedTalents =
    {
        "4f497e31844a1004087cf1b97330ed72", "27ac309cb4aa14241a7d550991262084",
        "56323e0c3d28d9a428d30189e93b5e68", "20c725db6c9c7f14d82ee7feb762a068",
        "80c3fd96e00c8364e8c24139d622c84b", "2885cbaf40b531e45b17a06182b5536e",
        "01e76de8d3649374f8a21f54d4637b44", "e3bc416aceb3d8445a89144b8f2d7a95",
        "6643e114543c16c4a921bdd381106f28", "f0e7e42950661c6488fe214aaaa2375a"
    };

    private static int checks, hostIds, hostLogs, serial;
    private static int? forcedDice;
    private static IEnumerable<UnitEntityData> areaUnits = Array.Empty<UnitEntityData>();
    private static readonly HashSet<UnitEntityData> Enemies = new HashSet<UnitEntityData>();
    private static readonly HashSet<BlueprintGuid> ClassificationIds = new HashSet<BlueprintGuid>(
        new[] { AberrationType, AnimalType, ConstructType, DragonType, FeyType, MagicalBeastType, MonstrousHumanoidType,
                OutsiderType, PlantType, UndeadType, VerminType, GiantSubtype, LycanthropeType, ReptilianSubtype,
                GoblinRace, HumanRace, ElfRace, DwarfRace, AasimarRace, GnomeRace, HalfElfRace, HalflingRace, HalfOrcRace,
                DemonOfMagic, DemonOfSlaughter, DemonOfStrength, NotADemon, InstantEnemyBuff, PlantTypeFake }.Select(P));

    private static readonly List<UnitEntityData> world = new List<UnitEntityData>();

    private static bool NotPlayerFaction(ref bool __result) { __result = false; return false; }
    private static bool NotTacticalCombat(ref bool __result) { __result = false; return false; }
    private static bool KeepPrerequisites(ref bool __result) { __result = false; return false; }
    private static bool NextUniqueId(ref string __result) { __result = "0070-" + (++hostIds).ToString("X"); return false; }
    private static bool HostLogFirst(Exception __0) { HostLog(__0); return false; }
    private static bool HostLogSecond(object __0, Exception __1) { HostLog(__1); return false; }
    private static void HostLog(Exception error)
    {
        if (error == null) return;
        hostLogs++;
        Console.WriteLine("0070 HOST LOG: " + error);
    }
    private static bool Targets(ref IEnumerable<UnitEntityData> __result) { __result = areaUnits; return false; }
    private static bool InspectAllowed(ref bool __result) { __result = true; return false; }
    private static bool ForceD(DiceFormula formula, ref int __result)
    {
        __result = forcedDice ?? 10;
        return false;
    }
    private static bool AllyModel(UnitEntityData __instance, UnitEntityData __0, ref bool __result)
    { __result = __0 != null && Enemies.Contains(__instance) == Enemies.Contains(__0); return false; }
    private static bool EnemyModel(UnitEntityData __instance, UnitEntityData __0, ref bool __result)
    { __result = __0 != null && Enemies.Contains(__instance) != Enemies.Contains(__0); return false; }

    private static Assembly Main => AppDomain.CurrentDomain.GetAssemblies()
        .First(a => a.GetName().Name == "CallOfTheWild.Wrath.Prototype");
    private static Type T(string name) => Main.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    private static BlueprintGuid P(string id) => BlueprintGuid.Parse(id);
    private static object Get(object o, string f) => AccessTools.Field(o.GetType(), f)?.GetValue(o)
        ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")?.GetValue(o);
    private static void Set(object o, string f, object v) =>
        (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")
         ?? throw new MissingFieldException(o.GetType().FullName, f)).SetValue(o, v);
    private static TEmpty Empty<TEmpty>() where TEmpty : class => (TEmpty)FormatterServices.GetUninitializedObject(typeof(TEmpty));
    private static TReference Ref<TReference>(SimpleBlueprint bp) where TReference : BlueprintReferenceBase, new()
    {
        var r = BlueprintReferenceBase.CreateTyped<TReference>(bp);
        AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null);
        return r;
    }
    private static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(T("PsychicAssets"), "Id")
        .Invoke(null, new object[] { token });
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("0070 CHECK FAILED: " + message);
        checks++;
        Console.WriteLine("0070 CHECK: " + message);
    }
    private static object Call(string type, string name, params object[] args) =>
        AccessTools.Method(T(type), name).Invoke(null, args);
    private static object Call(Type type, string name, params object[] args) =>
        AccessTools.Method(type, name).Invoke(null, args);

    private static T Stage<T>(BlueprintsCache cache, T bp, string guid, string name) where T : SimpleBlueprint
    {
        bp.AssetGuid = P(guid); bp.name = name;
        if (bp is BlueprintUnitFact) Set(bp, "m_Icon", Empty<UnityEngine.Sprite>());
        if (bp is BlueprintScriptableObject so && so.ComponentsArray == null)
            so.ComponentsArray = Array.Empty<BlueprintComponent>();
        bp.OnEnable();
        cache.AddCachedBlueprint(bp.AssetGuid, bp);
        return bp;
    }
    private static T StageIfMissing<T>(BlueprintsCache cache, string guid, string name) where T : SimpleBlueprint, new()
    {
        var existing = cache.Load(P(guid)) as T;
        if (existing != null) return existing;
        return Stage(cache, new T(), guid, name);
    }
    private static LevelEntry Entry(int level, params BlueprintFeatureBase[] facts)
    {
        var e = new LevelEntry { Level = level };
        Set(e, "m_Features", facts.Select(f => Ref<BlueprintFeatureBaseReference>(f)).ToList());
        return e;
    }

    private static string[] Missing(BlueprintsCache cache) => (string[])Call("CryptidScholar0070", "MissingProvider", cache);
    private static bool Register(BlueprintsCache cache) => (bool)Call("CryptidScholar0070", "Register", cache);
    private static string[] LastMissing() => (string[])AccessTools.Field(T("CryptidScholar0070"), "LastMissing").GetValue(null);
    private static string LastSummary() => (string)AccessTools.Field(T("CryptidScholar0070"), "LastSummary").GetValue(null);

    private static void HostGame()
    {
        if (Kingmaker.Game.HasInstance) return;
        var game = (Kingmaker.Game)FormatterServices.GetUninitializedObject(typeof(Kingmaker.Game));
        AccessTools.Field(typeof(Kingmaker.Game), "s_Instance").SetValue(null, game);
        var stateField = AccessTools.Field(typeof(Kingmaker.Game), "State");
        var state = FormatterServices.GetUninitializedObject(stateField.FieldType);
        stateField.SetValue(game, state);
        AccessTools.Field(stateField.FieldType, "PlayerState").SetValue(state,
            FormatterServices.GetUninitializedObject(typeof(Kingmaker.Player)));
        var player = AccessTools.Field(stateField.FieldType, "PlayerState").GetValue(state);
        Set(player, "m_Party", new List<UnitEntityData>());
        Set(player, "m_PartyAndPets", new List<UnitEntityData>());
        Set(player, "m_PartyAndPetsDetached", new List<UnitEntityData>());
        Set(player, "PartyCharacters", new List<UnitReference>());
        Set(player, "m_CharacterListsValid", true);
        AccessTools.Field(typeof(Kingmaker.Game), "TimeController").SetValue(game,
            FormatterServices.GetUninitializedObject(typeof(Kingmaker.Controllers.TimeController)));
        AccessTools.Field(typeof(Kingmaker.Game), "Rulebook").SetValue(game,
            Activator.CreateInstance(typeof(Kingmaker.RuleSystem.Rulebook)));
        var managerField = AccessTools.Field(typeof(Kingmaker.Player), "InspectUnitsManager");
        managerField.SetValue(player, Activator.CreateInstance(typeof(InspectUnitsManager)));
    }
    private static readonly Lazy<Type> ExperienceType = new Lazy<Type>(() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .First(t => t.Name == "Experience" && typeof(BlueprintComponent).IsAssignableFrom(t) && t.GetField("CR") != null));
    private static TimeSpan currentTime;
    private static bool TimeGetter(ref TimeSpan __result) { __result = currentTime; return false; }
    private static bool NoBattleLog(ref bool __result) { __result = false; return false; }

    // Declared host boundary: classification facts are attached as identity
    // markers. Their native grant chains (e.g. demon grouping AddFacts) reference
    // blueprints outside this directed fixture and are not exercised here.
    private static bool SkipClassificationGrants(object __instance)
    {
        try
        {
            var factProp = __instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(pp => pp.Name == "Fact" && pp.GetIndexParameters().Length == 0);
            var fact = factProp?.GetValue(__instance, null);
            var bpProp = fact?.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(pp => pp.Name == "Blueprint" && pp.GetIndexParameters().Length == 0);
            if (bpProp?.GetValue(fact, null) is SimpleBlueprint bp && ClassificationIds.Contains(bp.AssetGuid)) return false;
        }
        catch { }
        return true;
    }
    // Declared scene boundary (same scope as the AntimagicRage AC transpiler):
    // the native AC descriptor arithmetic runs for real inside the temporary
    // modifiers scope; prone/view/flat-footed scene checks and the UI modifier
    // breakdown are controlled inputs, not player-game rendering.
    private static bool ACScene(RuleCalculateAC __instance)
    {
        var ac = __instance.Target.Stats.AC;
        using (ac.GetTemporaryModifiersScope(__instance.AllBonuses))
        {
            int result = __instance.AttackType.IsTouch() ? ac.Touch : (int)ac;
            Set(__instance, "Result", result);
            Set(__instance, "IsTargetFlatFooted", false);
        }
        return false;
    }
    private static bool NotFlatFooted(object __instance)
    {
        var prop = __instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(pp => pp.Name == "IsFlatFooted" && pp.GetIndexParameters().Length == 0);
        prop?.SetValue(__instance, false);
        return false;
    }
    private static bool NeutralDifficulty(int originalDC, ref int dcAdjustment, bool ignoreDifficultyBonusToDC, ref int __result)
    {
        dcAdjustment = 0;
        __result = originalDC;
        return false;
    }

    private static bool ReportElementError(object __instance)
    {
        Console.WriteLine("0070 ACTION EXCEPTION: " + __instance);
        foreach (var prop in __instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            try { Console.WriteLine("0070 ACTION PROP " + prop.Name + ": " + prop.GetValue(__instance)); } catch { }
        }
        return false;
    }
    private static bool ReportException(object __0, Exception __1)
    {
        Console.WriteLine("0070 ACTION EXCEPTION: " + __1);
        return false;
    }

    private static void SetGameTime(TimeSpan time) { currentTime = time; }
    private static TimeSpan GameTime() => Kingmaker.Game.Instance.TimeController.GameTime;

    private static UnitEntityData Unit(BlueprintsCache cache, BlueprintCharacterClass cls, int level, string label)
    {
        var caster = new Psychic070Tests.Caster(cache);
        var u = caster.Unit;
        var facts = new EntityFactsManager(u); Set(u, "Facts", facts);
        var buffs = new BuffCollection(); buffs.Initialize(facts); Set(u, "Buffs", buffs);
        var all = (List<ModifiableValue>)Get(u.Stats, "<AllStats>k__BackingField");
        foreach (var stat in new[] { StatType.Intelligence, StatType.Wisdom, StatType.Charisma, StatType.Strength, StatType.Dexterity, StatType.Constitution })
        {
            var existing = Get(u.Stats, stat.ToString()) as ModifiableValueAttributeStat;
            if (existing != null) { existing.BaseValue = 10; continue; }
            var value = new ModifiableValueAttributeStat(u.Stats, stat) { BaseValue = 10 };
            Set(u.Stats, stat.ToString(), value); all.Add(value);
        }
        foreach (var pair in new[] {
            new[] { StatType.SaveFortitude, StatType.Constitution },
            new[] { StatType.SaveReflex, StatType.Dexterity },
            new[] { StatType.SaveWill, StatType.Wisdom } })
        {
            var save = new ModifiableValueSavingThrow(u.Stats, (ModifiableValueAttributeStat)Get(u.Stats, pair[1].ToString()), pair[0]) { BaseValue = 0 };
            Set(u.Stats, pair[0].ToString(), save); all.Add(save);
        }
        foreach (var pair in new[] {
            new[] { StatType.SkillLoreNature, StatType.Intelligence },
            new[] { StatType.SkillLoreReligion, StatType.Intelligence },
            new[] { StatType.SkillKnowledgeArcana, StatType.Wisdom },
            new[] { StatType.SkillKnowledgeWorld, StatType.Wisdom } })
        {
            var skill = new ModifiableValueSkill(u.Stats, (ModifiableValueAttributeStat)Get(u.Stats, pair[1].ToString()), pair[0]) { BaseValue = 0 };
            Set(u.Stats, pair[0].ToString(), skill); all.Add(skill);
        }
        var ac = new ModifiableValueArmorClass(u.Stats) { BaseValue = 10 };
        Set(u.Stats, "AC", ac); all.Add(ac);
        var state = u.Descriptor.State;
        Set(state, "m_Conditions", new sbyte[100]);
        Set(state, "m_ConditionsImmunity", new sbyte[100]);
        foreach (var field in state.Features.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.FieldType == typeof(CountableFlag) && field.GetValue(state.Features) == null)
                field.SetValue(state.Features, new CountableFlag());
        if (cls != null)
        {
            var classData = new ClassData(cls) { Level = level };
            u.Progression.Classes.Clear(); u.Progression.Classes.Add(classData);
        }
        else u.Progression.Classes.Clear();
        Set(u.Progression, "Features", u.Facts.EnsureFactProcessor<FeatureCollection>());
        Set(u, "ActivatableAbilities", u.Facts.EnsureFactProcessor<Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityCollection>());
        Set(u.Descriptor, "Resources", new UnitAbilityResourceCollection(u.Descriptor));
        Set(u.Descriptor, "Proficiencies", new UnitProficiency());
        AccessTools.Field(typeof(UnitDescriptor), "m_Spellbooks").SetValue(u.Descriptor,
            new Dictionary<BlueprintSpellbook, Spellbook>());
        Set(u, "UniqueId", "0070-unit-" + (++serial));
        var proxy = FormatterServices.GetUninitializedObject(AccessTools.Inner(typeof(EntityService), "Proxy"));
        Set(proxy, "Entity", u); Set(u, "Proxy", proxy);
        Set(u, "m_Position", Vector3.zero);
        Set(u, "CombatState", Empty<UnitCombatState>());
        Set(u.CombatState, "Unit", u);
        Set(u.CombatState, "Cooldown", new UnitCombatState.Cooldowns());
        Set(u.CombatState, "m_EngagedBy", new Dictionary<UnitEntityData, TimeSpan>());
        Set(u.CombatState, "m_EngagedUnits", new Dictionary<UnitEntityData, TimeSpan>());
        Set(u.CombatState, "DisengageAttackTargets", new List<UnitEntityData>());
        Set(u, "Abilities", u.Facts.EnsureFactProcessor<Kingmaker.UnitLogic.Abilities.AbilityCollection>());
        var neutral = new BlueprintUnit { name = "0070-body-" + label, AssetGuid = P(Guid.NewGuid().ToString("N")), ComponentsArray = Array.Empty<BlueprintComponent>() };
        neutral.OnEnable();
        Set(u.Descriptor, "Blueprint", neutral); Set(u.Descriptor, "OriginalBlueprint", neutral);
        facts.TurnOn();
        world.Add(u);
        ((List<UnitEntityData>)Get(Kingmaker.Game.Instance.Player, "m_PartyAndPets")).Add(u);
        return u;
    }
    private static UnitEntityData Enemy(BlueprintsCache cache, string label, float cr, params string[] factGuids)
    {
        var u = Unit(cache, null, 0, label);
        Enemies.Add(u);
        foreach (var guid in factGuids)
        {
            var bp = cache.Load(P(guid));
            // Classification facts are identity markers in this host: their
            // native mechanical components (undead health, demon grants, ...)
            // are stripped for attachment so bare fixture units stay valid.
            if (bp is BlueprintFeature feature)
            {
                var saved = feature.ComponentsArray;
                feature.ComponentsArray = Array.Empty<BlueprintComponent>();
                Fact(u, feature);
                feature.ComponentsArray = saved;
            }
            else if (bp is BlueprintBuff buff)
            {
                var saved = buff.ComponentsArray;
                buff.ComponentsArray = Array.Empty<BlueprintComponent>();
                Effect(u, buff, u);
                buff.ComponentsArray = saved;
            }
        }
        var expType = ExperienceType.Value;
        var experience = (BlueprintComponent)Activator.CreateInstance(expType);
        expType.GetField("CR").SetValue(experience, (int)cr);
        expType.GetField("Modifier").SetValue(experience, 1.0f);
        var inspection = new BlueprintUnit { name = "0070-inspect-" + label, AssetGuid = P(Guid.NewGuid().ToString("N")), ComponentsArray = new[] { experience } };
        var type = new BlueprintUnitType { name = "0070-type-" + label, AssetGuid = P(Guid.NewGuid().ToString("N")), KnowledgeStat = StatType.SkillLoreNature };
        type.OnEnable();
        Set(inspection, "m_Type", Ref<BlueprintUnitTypeReference>(type));
        inspection.OnEnable();
        Set(u.Descriptor, "ReplaceBlueprintForInspection", inspection);
        return u;
    }
    private static void Resource(UnitEntityData u, BlueprintAbilityResource bp, int amount)
    {
        var collection = u.Descriptor.Resources;
        var entry = new UnitAbilityResource(bp);
        Set(entry, "Amount", amount);
        var map = (System.Collections.IDictionary)Get(collection, "m_Resources");
        if (map == null) { map = new Dictionary<BlueprintScriptableObject, UnitAbilityResource>(); Set(collection, "m_Resources", map); }
        map[bp] = entry;
    }
    private static int ResourceAmount(UnitEntityData u, BlueprintAbilityResource bp)
        => u.Descriptor.Resources.GetResourceAmount(bp);
    private static Feature Fact(UnitEntityData u, BlueprintFeature bp)
    {
        var fact = new Feature(bp, u.Descriptor);
        Set(fact, "UniqueId", Guid.NewGuid().ToString("N"));
        u.Facts.Add(fact);
        return fact;
    }
    private static Buff Effect(UnitEntityData target, BlueprintBuff bp, UnitEntityData caster, TimeSpan? duration = null)
    {
        var fact = new Buff(bp, new MechanicsContext(caster ?? target, target.Descriptor, bp), duration);
        target.Facts.Add(fact);
        return fact;
    }
    private static void Remove(EntityFact fact) { if (fact?.Manager != null) fact.Manager.Remove(fact); }

    private static T WithLedger<T>(UnitEntityData u, Func<CryptidScholarLedgerData0070, T> f)
    {
        var method = AccessTools.Method(Main.GetType("CallOfTheWild.Wrath.Prototype.CryptidScholarRuntime0070", true), "WithLedger").MakeGenericMethod(typeof(T));
        return (T)method.Invoke(null, new object[] { u, f });
    }
    private static string KeyOf(UnitEntityData u) => (string)Call("CryptidClassification0070", "KeyOf", u);

    private static BlueprintsCache MainCache;

    // ------------------------------------------------------------- staging ---
    private static BlueprintCharacterClass haClass;
    private static BlueprintProgression haProgression;
    private static BlueprintFeatureSelection haTalents;
    private static BlueprintFeature poison, immunity, combat, strike, inspiration;
    private static BlueprintAbilityResource haResource;
    private static BlueprintFeature[] excludedTalentRoots;
    private static BlueprintFeature legalTalent;

    private static void StageProvider(BlueprintsCache cache)
    {
        var oldBook = Stage(cache, new BlueprintSpellbook { Spontaneous = true, CastingAttribute = StatType.Intelligence, IsArcane = true }, OldBookId, "InvestigatorSpellbook");
        poison = Stage(cache, new BlueprintFeature(), PoisonResistanceId, "InvestigatorPoisonResistance");
        immunity = Stage(cache, new BlueprintFeature(), PoisonImmunityId, "InvestigatorPoisonImmunity");
        combat = Stage(cache, new BlueprintFeature(), StudiedCombatFeatureId, "InvestigatorStudiedCombatFeature");
        strike = Stage(cache, new BlueprintFeature(), StudiedStrikeFeatureId, "StudiedStrikeFeature");
        haResource = Stage(cache, new BlueprintAbilityResource(), InspirationResourceId, "InspirationResource");
        inspiration = Stage(cache, new BlueprintFeature(), InspirationFeatureId, "InspirationFeature");
        var grant = new AddAbilityResources { name = "HAInspirationGrant", RestoreAmount = true };
        Set(grant, "m_Resource", Ref<BlueprintAbilityResourceReference>(haResource));
        inspiration.ComponentsArray = new BlueprintComponent[] { grant };
        inspiration.OnEnable();

        haTalents = Stage(cache, new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true, IgnorePrerequisites = false, ComponentsArray = Array.Empty<BlueprintComponent>() }, TalentSelectionId, "InvestigatorTalentSelection");
        excludedTalentRoots = ExcludedTalents.Select((id, i) => Stage(cache, new BlueprintFeature(), id, "HATalent" + i)).ToArray();
        legalTalent = Stage(cache, new BlueprintFeature(), "a1b2c3d4e5f60718293a4b5c6d7e8f90", "LegalInvestigatorTalent");
        var options = excludedTalentRoots.Concat(new[] { legalTalent }).ToArray();
        Set(haTalents, "m_Features", options.Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());
        Set(haTalents, "m_AllFeatures", options.Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());

        var entries = new List<LevelEntry>();
        for (int level = 1; level <= 20; level++)
        {
            var grants = new List<BlueprintFeatureBase>();
            if (level == 1) grants.Add(inspiration);
            if (level == 2 || level == 5 || level == 8) grants.Add(poison);
            if (level == 11) grants.Add(immunity);
            if (level >= 4 && level % 2 == 0) grants.Add(combat);
            if (level == 4) grants.Add(strike);
            if (level >= 3 && level <= 19 && level % 2 == 1) grants.Add(haTalents);
            entries.Add(Entry(level, grants.ToArray()));
        }
        haProgression = Stage(cache, new BlueprintProgression { LevelEntries = entries.ToArray(), UIGroups = Array.Empty<UIGroup>(), ComponentsArray = Array.Empty<BlueprintComponent>() }, ProgressionId, "InvestigatorProgression");
        Set(haProgression, "m_UIDeterminatorsGroup", Array.Empty<BlueprintFeatureBaseReference>());
        haClass = Stage(cache, new BlueprintCharacterClass { IsArcaneCaster = false, IsDivineCaster = false, ComponentsArray = Array.Empty<BlueprintComponent>() }, ClassId, "InvestigatorClass");
        Set(haClass, "m_Progression", Ref<BlueprintProgressionReference>(haProgression));
        Set(haClass, "m_Spellbook", Ref<BlueprintSpellbookReference>(oldBook));
        Set(haClass, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
    }

    private static void StageClassificationFacts(BlueprintsCache cache)
    {
        var names = new Dictionary<string, string>
        {
            [AberrationType] = "AberrationType", [AnimalType] = "AnimalType", [ConstructType] = "ConstructType",
            [DragonType] = "DragonType", [FeyType] = "FeyType", [MagicalBeastType] = "MagicalBeastType",
            [MonstrousHumanoidType] = "MonstrousHumanoidType", [OutsiderType] = "OutsiderType",
            [PlantType] = "PlantType", [UndeadType] = "UndeadType", [VerminType] = "VerminType",
            [GiantSubtype] = "GiantSubtype", [LycanthropeType] = "LycanthropeType", [ReptilianSubtype] = "ReptilianSubtype",
            [GoblinRace] = "GoblinRace", [HumanRace] = "HumanRace", [ElfRace] = "ElfRace", [DwarfRace] = "DwarfRace",
            [AasimarRace] = "AasimarRace", [GnomeRace] = "GnomeRace", [HalfElfRace] = "HalfElfRace",
            [HalflingRace] = "HalflingRace", [HalfOrcRace] = "HalfOrcRace",
            [DemonOfMagic] = "DemonOfMagicFeature", [DemonOfSlaughter] = "DemonOfSlaughterFeature",
            [DemonOfStrength] = "DemonOfStrengthFeature", [NotADemon] = "NotADemonFeature",
            [InstantEnemyBuff] = "InstantEnemyBuff", [PlantTypeFake] = "PlantTypeFake"
        };
        foreach (var pair in names)
        {
            if (cache.Load(P(pair.Key)) != null) continue;
            if (pair.Key == InstantEnemyBuff) Stage(cache, new BlueprintBuff(), pair.Key, pair.Value);
            else Stage(cache, new BlueprintFeature(), pair.Key, pair.Value);
        }
        StageIfMissing<BlueprintAbility>(cache, "183d5bb91dea3a1489a6db6c9cb64445", "ShieldOfFaith");
        StageIfMissing<BlueprintAbility>(cache, "2c38da66e5a599347ac95b3294acbe00", "TrueStrike");
    }

    private static BlueprintArchetype Root(BlueprintsCache cache) => (BlueprintArchetype)cache.Load(Id("CryptidScholarArchetype0070"));
    private static BlueprintFeature Lore(BlueprintsCache cache) => (BlueprintFeature)cache.Load(Id("CryptidScholar0070IntuitiveLore"));
    private static BlueprintAbility AdviceAbility(BlueprintsCache cache) => (BlueprintAbility)cache.Load(Id("CryptidScholar0070OpportuneAdviceAbility"));
    private static BlueprintAbility StrikeAbility(BlueprintsCache cache) => (BlueprintAbility)cache.Load(Id("CryptidScholar0070KnowledgeableStrikeAbility"));
    private static BlueprintBuff AdviceBuff(BlueprintsCache cache) => (BlueprintBuff)cache.Load(Id("CryptidScholar0070AdviceBuff"));
    private static BlueprintBuff StrikeBuff(BlueprintsCache cache) => (BlueprintBuff)cache.Load(Id("CryptidScholar0070StrikeBuff"));

    private static UnitEntityData Scholar(BlueprintsCache cache, int level, string label, int intScore = 16, int wisScore = 14)
    {
        var u = Unit(cache, haClass, level, label);
        u.Progression.Classes[0].AddArchetype(Root(cache));
        Fact(u, Lore(cache));
        u.Stats.Intelligence.BaseValue = intScore;
        u.Stats.Wisdom.BaseValue = wisScore;
        Resource(u, haResource, 3);
        return u;
    }

    // Real native ability apply path: AbilityEffectRunAction.Apply with a real
    // AbilityExecutionContext and target wrapper, exactly as production casts.
    private static void CastAdvice(UnitEntityData caster, UnitEntityData target)
    {
        var ability = new AbilityData(AdviceAbility(MainCache), caster.Descriptor);
        object casterRef = default(EntityRef<UnitEntityData>);
        Set(casterRef, "m_Proxy", Get(caster, "<Proxy>k__BackingField"));
        Set(ability, "m_CasterRef", casterRef);
        var context = new AbilityExecutionContext(ability, new AbilityParams { CasterLevel = caster.Progression.GetClassLevel(haClass) }, new TargetWrapper(target));
        context.RecalculateRanks();
        AdviceAbility(MainCache).GetComponent<AbilityEffectRunAction>().Apply(context, new TargetWrapper(target));
    }
    private static void CastStrike(UnitEntityData caster)
    {
        var ability = new AbilityData(StrikeAbility(MainCache), caster.Descriptor);
        object casterRef = default(EntityRef<UnitEntityData>);
        Set(casterRef, "m_Proxy", Get(caster, "<Proxy>k__BackingField"));
        Set(ability, "m_CasterRef", casterRef);
        var context = new AbilityExecutionContext(ability, new AbilityParams { CasterLevel = caster.Progression.GetClassLevel(haClass) }, new TargetWrapper(caster));
        StrikeAbility(MainCache).GetComponent<AbilityEffectRunAction>().Apply(context, new TargetWrapper(caster));
    }

    private static int Armor(UnitEntityData attacker, UnitEntityData defender, AttackType attack)
    {
        var rule = new RuleCalculateAC(attacker, defender, attack);
        RulebookEventBus.OnEventAboutToTrigger(rule);
        rule.OnTrigger(null);
        RulebookEventBus.OnEventDidTrigger(rule);
        return rule.Result;
    }
    private static RuleSavingThrow SaveEvent(UnitEntityData unit, SavingThrowType type, UnitEntityData sourceCaster, AbilityType abilityType)
    {
        var bp = new BlueprintAbility { Type = abilityType, ComponentsArray = Array.Empty<BlueprintComponent>() };
        bp.OnEnable();
        var ability = new AbilityData(bp, sourceCaster.Descriptor);
        object casterRef = default(EntityRef<UnitEntityData>);
        Set(casterRef, "m_Proxy", Get(sourceCaster, "<Proxy>k__BackingField"));
        Set(ability, "m_CasterRef", casterRef);
        var context = new AbilityExecutionContext(ability, new AbilityParams { CasterLevel = 10, DC = 20 }, new TargetWrapper(unit));
        return new RuleSavingThrow(unit, type, 20) { Reason = new RuleReason(context) };
    }
    private static int SaveDelta(UnitEntityData u, StatType stat, int baseline) => u.Stats.GetStat(stat).ModifiedValue - baseline;
    private static int LoreBonus(ModifiableValue stat)
        => stat.Modifiers.Where(m => m.ModDescriptor == ModifierDescriptor.UntypedStackable).Sum(m => m.ModValue);


    private static ItemEntityWeapon Weapon(BlueprintItemWeapon bp)
    {
        var weapon = Empty<ItemEntityWeapon>();
        var field = AccessTools.Field(typeof(ItemEntityWeapon), "m_Blueprint")
            ?? AccessTools.Field(typeof(ItemEntityWeapon), "<Blueprint>k__BackingField")
            ?? throw new MissingFieldException("ItemEntityWeapon blueprint field");
        field.SetValue(weapon, bp);
        return weapon;
    }
    private static RuleAttackRoll Attack(UnitEntityData attacker, UnitEntityData target, ItemEntityWeapon weapon, bool hit)
    {
        var roll = new RuleAttackRoll(attacker, target, weapon, 0);
        roll.SetFake(hit ? AttackResult.Hit : AttackResult.Miss);
        return roll;
    }
    private static RulePrepareDamage DamageEvent(UnitEntityData attacker, UnitEntityData target, RuleAttackRoll roll, ItemEntityWeapon weapon)
    {
        var physical = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1, DiceType.D8)), 0, PhysicalDamageForm.Slashing);
        var bundle = new DamageBundle(physical) { Weapon = weapon };
        var parent = new RuleDealDamage(attacker, target, bundle) { AttackRoll = roll };
        return new RulePrepareDamage(parent);
    }
    private static int PrecisionPackages(RulePrepareDamage evt)
        => evt.DamageBundle.Count(d => d.Precision);

    // ------------------------------------------------------------------ run ---
    internal static int Run(BlueprintsCache parent, Harmony h, JObject nativeRows)
    {
        checks = 0; hostIds = 0; hostLogs = 0; serial = 0;
        MainCache = parent;
        Console.WriteLine("0070 stage: isolated managed host, real game blueprint cache, C-0070-20261112-r1 directed cases");
        HostGame();
        h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(NotPlayerFaction))));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(NotTacticalCombat))));
        h.Patch(AccessTools.Method(typeof(Kingmaker.Player), "GetNewUniqueId"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(NextUniqueId))));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(KeepPrerequisites))));
        foreach (var signature in new[] {
            new[] { typeof(Exception), typeof(string), typeof(object[]) },
            new[] { typeof(UnityEngine.Object), typeof(Exception), typeof(string), typeof(object[]) },
            new[] { typeof(Owlcat.Runtime.Core.Logging.ICanBeLogContext), typeof(Exception), typeof(string), typeof(object[]) } })
        {
            var method = AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel), "Exception", signature);
            if (method == null) continue;
            h.Patch(method, prefix: new HarmonyMethod(signature.Length == 3
                ? AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(HostLogFirst))
                : AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(HostLogSecond))));
        }
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type elt = null;
            try { elt = asm.GetType("Kingmaker.ElementsSystem.ElementLogicException"); } catch { }
            if (elt == null) continue;
            var report = AccessTools.Method(elt, "ReportInternal");
            if (report != null)
                h.Patch(report, prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(ReportElementError))));
        }
        h.Patch(AccessTools.Method(typeof(AddFacts), "UpdateFacts"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(SkipClassificationGrants))));
        h.Patch(AccessTools.PropertyGetter(typeof(RulebookEvent), "DisableBattleLog"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(NoBattleLog))));
        h.Patch(AccessTools.Method(typeof(RuleCalculateAC), "OnTrigger"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(ACScene))));
        h.Patch(AccessTools.Method(typeof(RuleSkillCheck), "AdjustDCByDifficulty"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(NeutralDifficulty))));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController), "GameTime"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(TimeGetter))));
        h.Patch(AccessTools.Method(typeof(GameHelper), "GetTargetsAround", new[] { typeof(Vector3), typeof(float), typeof(bool), typeof(bool) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(Targets))));
        h.Patch(AccessTools.Method(typeof(InspectUnitsHelper), "IsInspectAllow", new[] { typeof(UnitEntityData) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(InspectAllowed))));
        var diceType = typeof(RulebookEvent).GetNestedType("Dice", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        h.Patch(AccessTools.Method(diceType, "D", new[] { typeof(DiceFormula) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(ForceD))));
        h.Patch(AccessTools.Method(typeof(UnitEntityData), "IsAlly", new[] { typeof(UnitEntityData) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(AllyModel))));
        h.Patch(AccessTools.Method(typeof(UnitEntityData), "IsEnemy", new[] { typeof(UnitEntityData) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(CryptidScholar0070Tests), nameof(EnemyModel))));
        // Real production talent prefix; no other production patch is installed.
        h.CreateClassProcessor(T("CryptidScholarTalent0070Patch")).Patch();

        var cache = parent;
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        StageProvider(cache);
        StageClassificationFacts(cache);
        SetGameTime(TimeSpan.Zero);

        GateAndLateLoad(cache);
        ConflictRollback(cache);
        LevelTable(cache);
        IntuitiveLore(cache);
        KnowledgeCredential(cache);
        ExactClassification(cache);
        FakeCategories(cache);
        CooldownClocks(cache);
        RealPayment(cache);
        RangeAndHearing(cache);
        ScalingAndOverride(cache);
        DefenseSources(cache);
        StrikeConversion(cache);
        RealWeaponHit(cache);
        PrecisionBoundaries(cache);
        WeaponAndRangeBoundaries(cache);
        TalentRestriction(cache);
        PlainHaAndOldVariant(cache);
        RestoreAndCleanup(cache);
        ExpiryAndTypeChange(cache);

        Console.WriteLine("0070 RESULT: " + checks + " directed assertions passed; staged provider fixture, not player-game validation.");
        Console.WriteLine("0070 NOT RUN: game/UMM startup, full historical suites, real HA plugin load, SaveManager/retraining controller, whole-library matrix.");
        return checks;
    }

    // ================= C0070-01: gate and real late load =================
    private static void GateAndLateLoad(BlueprintsCache cache)
    {
        var empty = new BlueprintsCache();
        var blank = Missing(empty);
        Check(blank.Length > 0 && blank.Any(m => m.Contains("InvestigatorClass missing")),
            "C01 empty cache defers with an explicit HA provider blocker");
        Check(!Register(empty) && LastMissing().Length == blank.Length,
            "C01 deferred registration publishes nothing and re-reports the same blockers");
        Check(empty.Load(Id("CryptidScholarArchetype0070")) == null,
            "C01 no private root after deferral");

        Check(Missing(cache).Length == 0, "C01 full audited provider satisfies the atomic gate");
        Check(Root(cache) == null, "C01 no scholar root exists before the optional provider retry");
        bool published = false;
        Action<string, bool, Exception> report = (name, succeeded, error) =>
        {
            if (name != "COTW-0070 Cryptid Scholar") return;
            if (error != null) throw new InvalidOperationException("Scholar optional publication failed", error);
            published = succeeded;
        };
        Call("BlueprintRegistration", "RegisterOptional", cache, report);
        Check(published && Root(cache) != null, "C01 production RegisterOptional publishes the scholar while the root was absent");
        Check(LastSummary().StartsWith("Registered", StringComparison.Ordinal), "C01 summary reports the committed graph");
        var identities = PrivateAssets(cache).Select(a => a.AssetGuid).ToArray();
        var links = ((BlueprintArchetypeReference[])Get(haClass, "m_Archetypes")).Select(r => r.Guid).ToArray();
        Check(Register(cache) && LastSummary().StartsWith("Already registered", StringComparison.Ordinal),
            "C01 second registration takes the verified re-entry path");
        Check(PrivateAssets(cache).Select(a => a.AssetGuid).SequenceEqual(identities),
            "C01 re-entry publishes no new private identity");
        Check(((BlueprintArchetypeReference[])Get(haClass, "m_Archetypes")).Select(r => r.Guid).SequenceEqual(links),
            "C01 re-entry does not duplicate the archetype link");
        Check(haProgression.UIGroups.Length == 1, "C01 exactly one private UI group appended");
    }

    private static BlueprintScriptableObject[] PrivateAssets(BlueprintsCache cache) =>
        ((System.Collections.IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>()
            .Select(cache.Load).OfType<BlueprintScriptableObject>()
            .Where(a => a.name != null && a.name.StartsWith("CotWWrathPsychicCryptidScholar", StringComparison.Ordinal))
            .ToArray();

    // ================= C0070-02: conflict rollback =================
    private static void ConflictRollback(BlueprintsCache cache)
    {
        var wrong = new BlueprintsCache();
        Stage(wrong, new BlueprintFeature(), ClassId, "ForeignIdentity");
        bool rejected = false;
        try { Missing(wrong); } catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { rejected = true; }
        Check(rejected, "C02 same-GUID wrong-type provider identity is rejected");
        wrong.RemoveCachedBlueprint(P(ClassId));
        Check(Missing(wrong).Any(m => m.Contains("InvestigatorClass missing")),
            "C02 removing the foreign identity leaves a clean retryable deferral");

        // Wrong progression reference on an otherwise complete provider.
        var badLink = new BlueprintsCache();
        StageProviderInto(badLink);
        var foreignProgression = Stage(badLink, new BlueprintProgression { LevelEntries = Array.Empty<LevelEntry>(), ComponentsArray = Array.Empty<BlueprintComponent>() }, Guid.NewGuid().ToString("N"), "ForeignProgression");
        Set(badLink.Load(P(ClassId)), "m_Progression", Ref<BlueprintProgressionReference>(foreignProgression));
        Check(Missing(badLink).Any(m => m.Contains("expected progression")), "C02 wrong progression reference is a blocker");
        Check(!Register(badLink) && badLink.Load(Id("CryptidScholarArchetype0070")) == null, "C02 wrong progression publishes nothing");

        // Missing grant level.
        var badGrant = new BlueprintsCache();
        StageProviderInto(badGrant);
        var progression = (BlueprintProgression)badGrant.Load(P(ProgressionId));
        var level6 = progression.LevelEntries.Single(e => e.Level == 6);
        Set(level6, "m_Features", new List<BlueprintFeatureBaseReference>());
        Check(Missing(badGrant).Any(m => m.Contains("level 6 lacks studied combat")), "C02 missing studied-combat grant is a blocker");
        Check(!Register(badGrant), "C02 missing grant publishes nothing");

        // Private GUID collision: occupy the archetype identity with a foreign asset.
        var collision = new BlueprintsCache();
        StageProviderInto(collision);
        StageClassificationFactsInto(collision);
        var squatter = Stage(collision, new BlueprintFeature(), Id("CryptidScholarArchetype0070").ToString(), "Squatter");
        bool collisionRejected = false;
        try { Register(collision); } catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { collisionRejected = true; }
        Check(collisionRejected, "C02 private GUID collision rejects the whole publication");
        Check(ReferenceEquals(collision.Load(Id("CryptidScholarArchetype0070")), squatter), "C02 the foreign asset is preserved");
        Check(((BlueprintArchetypeReference[])Get(collision.Load(P(ClassId)), "m_Archetypes")).Length == 0,
            "C02 collision leaves the provider class unlinked");
        collision.RemoveCachedBlueprint(Id("CryptidScholarArchetype0070"));
        Check(Register(collision) && Root(collision) != null, "C02 after the collision is cleared the same provider retries successfully");
    }
    private static void StageProviderInto(BlueprintsCache cache)
    {
        var savedClass = haClass; var savedProgression = haProgression; var savedTalents = haTalents;
        var savedPoison = poison; var savedImmunity = immunity; var savedCombat = combat; var savedStrike = strike;
        var savedInspiration = inspiration; var savedResource = haResource; var savedExcluded = excludedTalentRoots; var savedLegal = legalTalent;
        StageProvider(cache);
        haClass = savedClass; haProgression = savedProgression; haTalents = savedTalents;
        poison = savedPoison; immunity = savedImmunity; combat = savedCombat; strike = savedStrike;
        inspiration = savedInspiration; haResource = savedResource; excludedTalentRoots = savedExcluded; legalTalent = savedLegal;
    }
    private static void StageClassificationFactsInto(BlueprintsCache cache)
    {
        var saved = MainCache; MainCache = cache; StageClassificationFacts(cache); MainCache = saved;
    }

    // ================= C0070-03: full HA level table =================
    private static void LevelTable(BlueprintsCache cache)
    {
        var root = Root(cache);
        Check(root.AddFeatures.Length == 2 && root.AddFeatures.Select(e => e.Level).OrderBy(l => l).SequenceEqual(new[] { 1, 4 }),
            "C03 add entries are exactly levels 1 and 4");
        Check(root.AddFeatures.Single(e => e.Level == 1).Features.Any(f => f.AssetGuid == Id("CryptidScholar0070IntuitiveLore")),
            "C03 level 1 grants Intuitive Lore");
        var level4 = root.AddFeatures.Single(e => e.Level == 4).Features.ToArray();
        Check(level4.Any(f => f.AssetGuid == Id("CryptidScholar0070OpportuneAdvice")) &&
              level4.Any(f => f.AssetGuid == Id("CryptidScholar0070KnowledgeableStrike")),
            "C03 level 4 grants Opportune Advice and Knowledgeable Strike");
        var remove = root.RemoveFeatures.ToDictionary(e => e.Level, e => e.Features.Select(f => f.AssetGuid).ToArray());
        Check(remove.Count == 12, "C03 twelve remove entries cover every replaced grant");
        foreach (int level in new[] { 2, 5, 8 })
            Check(remove[level].Contains(P(PoisonResistanceId)), "C03 poison resistance removed at " + level);
        Check(remove[11].Contains(P(PoisonImmunityId)), "C03 poison immunity removed at 11");
        Check(remove[4].Contains(P(StudiedStrikeFeatureId)), "C03 studied strike removed at 4");
        foreach (int level in new[] { 4, 6, 8, 10, 12, 14, 16, 18, 20 })
            Check(remove[level].Contains(P(StudiedCombatFeatureId)), "C03 studied combat removed at even level " + level);
        Check(root.ReplaceSpellbook == null && !root.ChangeCasterType && !root.ReplaceClassSkills,
            "C03 provider spellbook, caster type and class skills are retained");
        // The provider progression itself is untouched: odd talents, inspiration
        // and the old book all remain for plain HA investigators.
        foreach (int level in new[] { 3, 5, 7, 9, 11, 13, 15, 17, 19 })
            Check(haProgression.LevelEntries.Single(e => e.Level == level).Features.Any(f => f.AssetGuid == haTalents.AssetGuid),
                "C03 provider odd-level talent grant preserved at " + level);
        Check(haProgression.LevelEntries.Single(e => e.Level == 1).Features.Any(f => f.AssetGuid == inspiration.AssetGuid),
            "C03 provider inspiration grant preserved at 1");
        Check(haClass.Spellbook.AssetGuid == P(OldBookId), "C03 provider spellbook identity preserved");
        var plain = Unit(cache, haClass, 20, "plain-levels");
        Check(plain.Progression.GetClassLevel(haClass) == 20, "C03 plain HA investigator levels normally");
    }

    // ================= C0070-04: Intuitive Lore real skills =================
    private static void IntuitiveLore(BlueprintsCache cache)
    {
        var lore = Lore(cache);
        var bonuses = lore.GetComponents<AddContextStatBonus>().ToArray();
        Check(bonuses.Length == 4 && bonuses.All(b => b.Descriptor == ModifierDescriptor.UntypedStackable),
            "C04 four untyped stackable skill contributions");
        Check(bonuses.Single(b => b.Stat == StatType.SkillLoreNature).Value.ValueRank == AbilityRankType.Default &&
              bonuses.Single(b => b.Stat == StatType.SkillLoreReligion).Value.ValueRank == AbilityRankType.Default &&
              bonuses.Single(b => b.Stat == StatType.SkillKnowledgeArcana).Value.ValueRank == AbilityRankType.StatBonus &&
              bonuses.Single(b => b.Stat == StatType.SkillKnowledgeWorld).Value.ValueRank == AbilityRankType.StatBonus,
            "C04 Nature/Religion ride the Intelligence rank, Arcana/World the Wisdom rank");
        Check(lore.GetComponents<RecalculateOnStatChange>().Select(r => r.Stat).OrderBy(s => s).SequenceEqual(new[] { StatType.Intelligence, StatType.Wisdom }),
            "C04 attribute changes recalculate both ranks");

        var u = Unit(cache, haClass, 4, "lore-skills");
        u.Progression.Classes[0].AddArchetype(Root(cache));
        u.Stats.Intelligence.BaseValue = 16; // +3
        u.Stats.Wisdom.BaseValue = 14;       // +2
        Resource(u, haResource, 3);
        var nature = u.Stats.GetStat(StatType.SkillLoreNature);
        var religion = u.Stats.GetStat(StatType.SkillLoreReligion);
        var arcana = u.Stats.GetStat(StatType.SkillKnowledgeArcana);
        var world = u.Stats.GetStat(StatType.SkillKnowledgeWorld);
        int bn = nature.ModifiedValue, br = religion.ModifiedValue, ba = arcana.ModifiedValue, bw = world.ModifiedValue;
        var loreFact = Fact(u, lore);
        var wisStat = u.Stats.GetStat(StatType.Wisdom);
        var rc2 = lore.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>().Single(r => (AbilityRankType)Get(r, "m_Type") == AbilityRankType.StatBonus);
        var baseValue = AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig), "GetBaseValue").Invoke(rc2, new object[] { loreFact.Context });
        var cv = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.StatBonus };
        Check(nature.ModifiedValue - bn == 3 && religion.ModifiedValue - br == 3 &&
              arcana.ModifiedValue - ba == 2 && world.ModifiedValue - bw == 2,
            "C04 real native skill deltas +3/+3/+2/+2 from Int 16 / Wis 14");
        var independent = nature.AddModifier(2, (EntityFact)null, ModifierDescriptor.Racial);
        u.Stats.Intelligence.BaseValue = 6;  // -2 -> contributes 0
        u.Stats.Wisdom.BaseValue = 18;       // +4
        Check(LoreBonus(nature) == 0 && LoreBonus(religion) == 0 && LoreBonus(arcana) == 4 && LoreBonus(world) == 4,
            "C04 attribute change recalculates: negative Intelligence contributes 0, Wisdom +4 applies");
        Check(nature.Modifiers.Any(m => m.ModDescriptor == ModifierDescriptor.Racial && m.ModValue == 2),
            "C04 independent racial contribution retained through recalculation");
        u.Stats.Intelligence.BaseValue = 16;
        Check(LoreBonus(nature) == 3, "C04 restoring Intelligence restores the +3 contribution");
        Remove(loreFact);
        Check(LoreBonus(nature) == 0 && LoreBonus(religion) == 0 && LoreBonus(arcana) == 0 && LoreBonus(world) == 0 &&
              nature.Modifiers.Any(m => m.ModDescriptor == ModifierDescriptor.Racial && m.ModValue == 2),
            "C04 removing the lore fact removes only its own contribution");
        nature.RemoveModifier(independent);
    }

    // ================= C0070-05: scholar's own check =================
    private static void KnowledgeCredential(BlueprintsCache cache)
    {
        var scholar = Scholar(cache, 4, "credential");
        var other = Scholar(cache, 4, "credential-other");
        var target = Enemy(cache, "credential-target", 1, AberrationType); // DC 11
        areaUnits = new[] { scholar };
        // Trained Lore Nature: success is guaranteed at +30, failure at +1 vs DC 135.
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        other.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;

        // Bestiary fully unlocked for the party does not create a credential.
        var manager = (InspectUnitsManager)Get(Kingmaker.Game.Instance.Player, "InspectUnitsManager");
        manager.ForceRevealUnitInfo(target);
        var key = (string)Call("CryptidClassification0070", "KeyOf", target);
        Check(key != null && key.StartsWith(AberrationType), "C05 aberration classification resolves");
        Check(!CredentialOf(scholar).Contains(key), "C05 revealed bestiary alone creates no credential");
        // Another scholar's success does not create this scholar's credential.
        areaUnits = new[] { other };
        CastAdvice(other, target);
        Check(CredentialOf(other).Contains(key) && !CredentialOf(scholar).Contains(key),
            "C05 a second scholar's success is independent");
        Check(!HasAdvice(scholar, other), "C05 other scholar's advice did not reach this scholar");
        areaUnits = new[] { scholar };

        // Own failure: high CR makes the check impossible.
        var hard = Enemy(cache, "credential-hard", 100, AberrationType); // DC 135
        var before = ResourceAmount(scholar, haResource);
        CastAdvice(scholar, hard);
        Check(!CredentialOf(scholar).Contains(key), "C05 own failed check creates no credential even with bestiary unlocked");
        Check(ResourceAmount(scholar, haResource) == before && AdviceBatchOf(scholar) == null,
            "C05 failure has no resource or batch side effect");
        // Own success records the credential; reuse needs no new roll.
        CastAdvice(scholar, target);
        Check(CredentialOf(scholar).Contains(key), "C05 own real success records the credential");
        var batch1 = AdviceBatchOf(scholar);
        Check(batch1 != null && batch1.Key == key, "C05 success issues the matching batch");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 1; // would now fail DC 11
        SetGameTime(GameTime() + TimeSpan.FromHours(25));
        CastAdvice(scholar, target);
        var batch2 = AdviceBatchOf(scholar);
        Check(batch2 != null && batch2.Id != batch1.Id, "C05 persisted credential reuses without a new successful roll");
    }
    private static List<string> CredentialOf(UnitEntityData u) => WithLedger(u, d => d.Credentials);
    private static CryptidAdviceBatch0070 AdviceBatchOf(UnitEntityData u) => WithLedger(u, d => d.Batch);

    // ================= C0070-06: exact classification =================
    private static void ExactClassification(BlueprintsCache cache)
    {
        var a = Enemy(cache, "class-a", 1, OutsiderType, DemonOfMagic);
        var b = Enemy(cache, "class-b", 1, OutsiderType, DemonOfMagic);
        var less = Enemy(cache, "class-less", 1, OutsiderType);
        var more = Enemy(cache, "class-more", 1, OutsiderType, DemonOfMagic, DemonOfSlaughter);
        var keyA = (string)Call("CryptidClassification0070", "KeyOf", a);
        var keyB = (string)Call("CryptidClassification0070", "KeyOf", b);
        var keyLess = (string)Call("CryptidClassification0070", "KeyOf", less);
        var keyMore = (string)Call("CryptidClassification0070", "KeyOf", more);
        Check(keyA == keyB, "C06 same broad type and same subtype set match across species");
        Check(keyA != keyLess && keyA != keyMore, "C06 missing or extra subtype does not match");
        // Order and duplicates normalise: add the same facts in reverse order.
        var reordered = Enemy(cache, "class-reordered", 1, DemonOfMagic, OutsiderType);
        Fact(reordered, cache.Load(P(DemonOfMagic)) as BlueprintFeature);
        Check((string)Call("CryptidClassification0070", "KeyOf", reordered) == keyA,
            "C06 duplicate and reversed fact order normalise to the same key");
        var scholar = Scholar(cache, 8, "class-scholar");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        areaUnits = new[] { scholar };
        CastAdvice(scholar, a);
        var batch = AdviceBatchOf(scholar);
        Check(batch.Key == keyA, "C06 issued batch carries the exact key");
        var acMatch = Armor(b, scholar, AttackType.Melee);
        var acLess = Armor(less, scholar, AttackType.Melee);
        var acMore = Armor(more, scholar, AttackType.Melee);
        Check(acMatch == 10 + 2 && acLess == 10 && acMore == 10,
            "C06 runtime AC matches only the exact category (level 8 = +2)");
    }

    // ================= C0070-07: fake categories =================
    private static void FakeCategories(BlueprintsCache cache)
    {
        var instant = Enemy(cache, "fake-instant", 1, InstantEnemyBuff);
        var fakePlant = Enemy(cache, "fake-plant", 1, PlantTypeFake);
        var nameless = Enemy(cache, "fake-none", 1);
        var human = Enemy(cache, "fake-human", 1, HumanRace);
        var giant = Enemy(cache, "real-giant", 1, GiantSubtype);
        var goblin = Enemy(cache, "real-goblin", 1, GoblinRace);
        var reptilian = Enemy(cache, "real-reptilian", 1, ReptilianSubtype);
        Check((string)Call("CryptidClassification0070", "KeyOf", instant) == null,
            "C07 InstantEnemyBuff alone is not a category");
        Check((string)Call("CryptidClassification0070", "KeyOf", fakePlant) == null,
            "C07 PlantTypeFake alone is not a category");
        Check((string)Call("CryptidClassification0070", "KeyOf", nameless) == null,
            "C07 unclassified unit is explicitly rejected, no default key");
        Check((string)Call("CryptidClassification0070", "KeyOf", human) == null,
            "C07 ordinary human is not a selectable group");
        Check((string)Call("CryptidClassification0070", "KeyOf", giant) != null &&
              (string)Call("CryptidClassification0070", "KeyOf", goblin) != null &&
              (string)Call("CryptidClassification0070", "KeyOf", reptilian) != null,
            "C07 giant/goblin/reptilian humanoid entries resolve");
        var scholar = Scholar(cache, 4, "fake-scholar");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        areaUnits = new[] { scholar };
        var before = ResourceAmount(scholar, haResource);
        CastAdvice(scholar, instant);
        CastAdvice(scholar, human);
        Check(AdviceBatchOf(scholar) == null && ResourceAmount(scholar, haResource) == before,
            "C07 fake or unsupported targets produce no batch, credential or debit");
        Check(!CredentialOf(scholar).Any(), "C07 no credentials from rejected categories");
    }

    // ================= C0070-08: 24h independent clocks =================
    private static void CooldownClocks(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.Zero);
        var a = Scholar(cache, 4, "clock-a");
        var b = Scholar(cache, 4, "clock-b");
        a.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        b.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var aberration = Enemy(cache, "clock-aberration", 1, AberrationType);
        var undead = Enemy(cache, "clock-undead", 1, UndeadType);
        areaUnits = new[] { a, b };
        CastAdvice(a, aberration);
        Check(ResourceAmount(a, haResource) == 3, "C08 first use of a category is free");
        CastAdvice(a, undead);
        Check(ResourceAmount(a, haResource) == 3, "C08 a different category is independently free");
        CastAdvice(b, aberration);
        Check(ResourceAmount(b, haResource) == 3, "C08 a second scholar's first use is free");
        // Within 24h: paid for the same scholar+category only.
        SetGameTime(TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59));
        CastAdvice(a, aberration);
        Check(ResourceAmount(a, haResource) == 2, "C08 23:59 same category costs one inspiration");
        var undeadReceipt = ReceiptOf(a, KeyOf(undead));
        Check(undeadReceipt.Expires == TimeSpan.FromHours(24), "C08 other category receipt unchanged by the paid reuse");
        var paidReceipt = ReceiptOf(a, KeyOf(aberration));
        Check(paidReceipt.Expires == TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59) + TimeSpan.FromHours(24),
            "C08 paid reuse restarts that category's 24h receipt");
        CastAdvice(b, aberration);
        Check(ResourceAmount(b, haResource) == 2, "C08 scholar B pays independently within its own window");
        // The untouched undead clock expires at exactly 24:00: free again.
        SetGameTime(TimeSpan.FromHours(24));
        CastAdvice(a, undead);
        Check(ResourceAmount(a, haResource) == 2, "C08 at exactly 24:00 the untouched category is free again");
        // The paid aberration clock runs to 47:59; still paid at 24:00.
        CastAdvice(a, aberration);
        Check(ResourceAmount(a, haResource) == 1, "C08 the restarted category is still paid at 24:00");
        // B's original aberration clock expired at 24:00 (B paid at 23:59 -> 47:59).
        SetGameTime(TimeSpan.FromHours(48));
        CastAdvice(b, aberration);
        Check(ResourceAmount(b, haResource) == 2, "C08 scholar B's restarted clock expires independently at 47:59");
        CastAdvice(a, aberration);
        Check(ResourceAmount(a, haResource) == 1, "C08 scholar A's clock expires independently at 47:59");
        // Combat boundaries do not reset the clock; rest is only elapsed time.
        var receipt = ReceiptOf(a, KeyOf(aberration));
        Check(receipt != null && receipt.Expires == TimeSpan.FromHours(72),
            "C08 the free re-issue at 48:00 restarts that category's 24h receipt only");
    }
    private static CryptidCooldownReceipt0070 ReceiptOf(UnitEntityData u, string key)
        => WithLedger(u, d => d.Receipts.FirstOrDefault(r => r.Key == key));

    // ================= C0070-09: real payment, rejection, replay =================
    private static void RealPayment(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(10));
        var scholar = Scholar(cache, 4, "pay");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var target = Enemy(cache, "pay-target", 1, AnimalType);
        areaUnits = new[] { scholar };
        Resource(scholar, haResource, 2);
        CastAdvice(scholar, target);
        Check(ResourceAmount(scholar, haResource) == 2, "C09 first issuance does not debit");
        CastAdvice(scholar, target);
        Check(ResourceAmount(scholar, haResource) == 1, "C09 cooldown reuse debits exactly one real HA inspiration");
        var batch = AdviceBatchOf(scholar);
        // Replay the same cast context: no second debit, no new batch.
        var ability = new AbilityData(AdviceAbility(cache), scholar.Descriptor);
        object casterRef = default(EntityRef<UnitEntityData>);
        Set(casterRef, "m_Proxy", Get(scholar, "<Proxy>k__BackingField"));
        Set(ability, "m_CasterRef", casterRef);
        var context = new AbilityExecutionContext(ability, new AbilityParams(), new TargetWrapper(target));
        var run = AdviceAbility(cache).GetComponent<AbilityEffectRunAction>();
        run.Apply(context, new TargetWrapper(target));
        var afterFirst = ResourceAmount(scholar, haResource);
        run.Apply(context, new TargetWrapper(target));
        Check(ResourceAmount(scholar, haResource) == afterFirst && AdviceBatchOf(scholar).Id == batch.Id + 1,
            "C09 duplicate event replay neither debits twice nor re-issues");
        // Balance 0 within cooldown: rejected without side effects.
        Resource(scholar, haResource, 0);
        var held = AdviceBatchOf(scholar);
        CastAdvice(scholar, target);
        Check(ResourceAmount(scholar, haResource) == 0 && AdviceBatchOf(scholar).Id == held.Id,
            "C09 zero balance rejects without changing the batch or going negative");
        // No eligible recipient (silenced caster): rejected without debit.
        Resource(scholar, haResource, 2);
        var silence = new BlueprintBuff { AssetGuid = P("e4b8a158e8aabfe41b5e0bd334d8c386"), name = "Silence", ComponentsArray = Array.Empty<BlueprintComponent>() };
        silence.OnEnable();
        var silenceBuff = Effect(scholar, silence, scholar);
        areaUnits = Array.Empty<UnitEntityData>();
        CastAdvice(scholar, target);
        Check(ResourceAmount(scholar, haResource) == 2,
            "C09 no eligible recipient leaves resources untouched");
        Remove(silenceBuff);
        areaUnits = new[] { scholar };
    }

    // ================= C0070-10: range and hearing =================
    private static void RangeAndHearing(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(20));
        var scholar = Scholar(cache, 4, "range");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var target = Enemy(cache, "range-target", 1, FeyType);
        var near = Scholar(cache, 4, "range-near");
        var boundary = Scholar(cache, 4, "range-boundary");
        var far = Scholar(cache, 4, "range-far");
        var dead = Scholar(cache, 4, "range-dead");
        var unconscious = Scholar(cache, 4, "range-unconscious");
        var silenced = Scholar(cache, 4, "range-silenced");
        Set(near, "m_Position", new Vector3(3, 0, 0));
        Set(boundary, "m_Position", new Vector3(9.144f, 0, 0));
        Set(far, "m_Position", new Vector3(9.2f, 0, 0));
        Set(dead, "m_Position", new Vector3(1, 0, 0));
        Set(dead.State, "LifeState", UnitLifeState.Dead);
        Set(unconscious, "m_Position", new Vector3(1, 0, 0));
        Set(unconscious.State, "LifeState", UnitLifeState.Unconscious);
        Set(silenced, "m_Position", new Vector3(1, 0, 0));
        var silence = new BlueprintBuff { AssetGuid = P("e4b8a158e8aabfe41b5e0bd334d8c386"), name = "Silence", ComponentsArray = Array.Empty<BlueprintComponent>() };
        silence.OnEnable();
        Effect(silenced, silence, silenced);
        areaUnits = new[] { scholar, near, boundary, far, dead, unconscious, silenced };
        CastAdvice(scholar, target);
        Check(HasAdvice(near) && HasAdvice(boundary) && HasAdvice(scholar),
            "C10 near, exactly-30ft and qualifying self receive Advice");
        Check(!HasAdvice(far) && !HasAdvice(dead) && !HasAdvice(unconscious) && !HasAdvice(silenced),
            "C10 beyond 30ft, dead, unconscious and silenced units receive nothing");
        // Silenced scholar cannot issue at all.
        SetGameTime(TimeSpan.FromDays(21));
        var quiet = Scholar(cache, 4, "range-quiet-scholar");
        quiet.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        Effect(quiet, silence, quiet);
        areaUnits = new[] { quiet, near };
        var before = ResourceAmount(quiet, haResource);
        CastAdvice(quiet, target);
        Check(AdviceBatchOf(quiet) == null && ResourceAmount(quiet, haResource) == before && !HasAdvice(near, quiet),
            "C10 silenced scholar issues no batch, no debit, no cooldown write");
        Check(ReceiptOf(quiet, (string)Call("CryptidClassification0070", "KeyOf", target)) == null,
            "C10 silenced scholar writes no receipt");
    }
    private static bool HasAdvice(UnitEntityData u, UnitEntityData caster = null)
        => u.Buffs.Enumerable.Any(b => b.Blueprint.AssetGuid == AdviceBuff(MainCache).AssetGuid &&
            (caster == null || ReferenceEquals(b.Context?.MaybeCaster, caster)));

    // ================= C0070-11: scaling, duration, override =================
    private static void ScalingAndOverride(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(30));
        foreach (var pair in new[] { new[] { 4, 1 }, new[] { 8, 2 }, new[] { 12, 3 }, new[] { 16, 4 }, new[] { 20, 5 } })
        {
            var scholar = Scholar(cache, pair[0], "scale-" + pair[0]);
            scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
            var target = Enemy(cache, "scale-target-" + pair[0], 1, DragonType);
            areaUnits = new[] { scholar };
            CastAdvice(scholar, target);
            var batch = AdviceBatchOf(scholar);
            Check(batch.Bonus == pair[1], "C11 level " + pair[0] + " bonus is +" + pair[1]);
            Check(batch.Expires - GameTime() == TimeSpan.FromSeconds(6 * 3),
                "C11 Int 16 (+3) lasts three rounds at level " + pair[0]);
            var attacker = Enemy(cache, "scale-attacker-" + pair[0], 1, DragonType);
            Check(Armor(attacker, scholar, AttackType.Melee) == 10 + pair[1],
                "C11 real AC event applies the level-scaled insight bonus");
        }
        var low = Scholar(cache, 4, "scale-low-int", intScore: 8); // -1
        low.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var lowTarget = Enemy(cache, "scale-low-target", 1, DragonType);
        areaUnits = new[] { low };
        CastAdvice(low, lowTarget);
        Check(AdviceBatchOf(low).Expires - GameTime() == TimeSpan.FromSeconds(6),
            "C11 negative Intelligence modifier still lasts one round");
        // Two scholars, one ally: the last valid advice replaces the earlier.
        var a = Scholar(cache, 8, "override-a");
        var b = Scholar(cache, 12, "override-b");
        var ally = Scholar(cache, 4, "override-ally");
        a.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        b.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var ta = Enemy(cache, "override-ta", 1, ConstructType);
        var tb = Enemy(cache, "override-tb", 1, VerminType);
        areaUnits = new[] { a, b, ally };
        CastAdvice(a, ta);
        Check(HasAdvice(ally, a), "C11 ally receives scholar A's advice");
        CastAdvice(b, tb);
        Check(HasAdvice(ally, b) && !HasAdvice(ally, a), "C11 ally keeps only the last valid advice");
        // B's failed cast must not delete A's remaining holders.
        var ally2 = Scholar(cache, 4, "override-ally2");
        areaUnits = new[] { a, b, ally2 };
        CastAdvice(a, ta);
        Check(HasAdvice(ally2, a), "C11 second ally holds A's advice");
        b.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 1;
        var hard = Enemy(cache, "override-hard", 100, FeyType);
        areaUnits = new[] { a, b, ally2 };
        CastAdvice(b, hard);
        Check(HasAdvice(ally2, a), "C11 another scholar's failure does not remove A's active source");
    }

    // ================= C0070-12: AC and save sources =================
    private static void DefenseSources(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(40));
        var scholar = Scholar(cache, 8, "defense");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var monster = Enemy(cache, "defense-monster", 1, MagicalBeastType);
        var otherMonster = Enemy(cache, "defense-other", 1, UndeadType);
        areaUnits = new[] { scholar };
        CastAdvice(scholar, monster);
        var baseline = scholar.Stats.AC.ModifiedValue;
        Check(Armor(monster, scholar, AttackType.Melee) == baseline + 2, "C12 matching attacker grants insight AC");
        Check(Armor(otherMonster, scholar, AttackType.Melee) == baseline, "C12 non-matching attacker grants nothing");
        Check(Armor(monster, scholar, AttackType.Melee) == baseline + 2, "C12 repeated AC events do not stack the insight bonus");
        // Saves: Ex/Su/Sp from the matching category qualify; ordinary spells do not.
        foreach (var type in new[] { AbilityType.Extraordinary, AbilityType.Supernatural, AbilityType.SpellLike })
        {
            var stat = StatType.SaveWill;
            var before = scholar.Stats.GetStat(stat).ModifiedValue;
            var evt = SaveEvent(scholar, SavingThrowType.Will, monster, type);
            RulebookEventBus.OnEventAboutToTrigger(evt);
            Check(SaveDelta(scholar, stat, before) == 2, "C12 matching " + type + " source grants insight save bonus");
            evt.OnDidTrigger();
            Check(scholar.Stats.GetStat(stat).ModifiedValue == before, "C12 save bonus is cleaned up with the event");
        }
        foreach (var type in new[] { AbilityType.Spell, AbilityType.Extraordinary })
        {
            var source = type == AbilityType.Spell ? monster : otherMonster;
            var before = scholar.Stats.SaveWill.ModifiedValue;
            var evt = SaveEvent(scholar, SavingThrowType.Will, source, type);
            RulebookEventBus.OnEventAboutToTrigger(evt);
            Check(SaveDelta(scholar, StatType.SaveWill, before) == 0,
                "C12 " + (type == AbilityType.Spell ? "ordinary spell" : "non-matching category") + " grants no save bonus");
            evt.OnDidTrigger();
        }
        var bare = new RuleSavingThrow(scholar, SavingThrowType.Will, 20);
        var bareBefore = scholar.Stats.SaveWill.ModifiedValue;
        RulebookEventBus.OnEventAboutToTrigger(bare);
        Check(SaveDelta(scholar, StatType.SaveWill, bareBefore) == 0, "C12 save without a source grants nothing");
        bare.OnDidTrigger();
        // Native saving-throw replacement: the bonus joins the real candidate.
        var replace = scholar.Ensure<UnitPartSavingThrowsReplace>();
        Set(replace, "SavingThrowStat", StatType.SaveWill);
        Set(replace, "ReplaceToStat", StatType.SaveReflex);
        scholar.Stats.SaveReflex.BaseValue = 10;
        var reflexBefore = scholar.Stats.SaveReflex.ModifiedValue;
        var willBefore = scholar.Stats.SaveWill.ModifiedValue;
        var replaced = SaveEvent(scholar, SavingThrowType.Will, monster, AbilityType.Supernatural);
        RulebookEventBus.OnEventAboutToTrigger(replaced);
        Check(SaveDelta(scholar, StatType.SaveReflex, reflexBefore) == 2 && scholar.Stats.SaveWill.ModifiedValue == willBefore,
            "C12 native replacement candidate receives the bonus, original stat untouched");
        replaced.OnDidTrigger();
        Check(scholar.Stats.SaveReflex.ModifiedValue == reflexBefore, "C12 replaced candidate bonus cleaned up");
        scholar.Parts.Remove<UnitPartSavingThrowsReplace>();
        // Existing insight from another source does not stack.
        var insight = scholar.Stats.AC.AddModifier(5, (EntityFact)null, ModifierDescriptor.Insight);
        Check(Armor(monster, scholar, AttackType.Melee) == baseline + 5, "C12 native insight non-stacking preserved");
        scholar.Stats.AC.RemoveModifier(insight);
    }

    // ================= C0070-13: strike batch conversion =================
    private static void StrikeConversion(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(50));
        var a = Scholar(cache, 8, "strike-a");
        var b = Scholar(cache, 8, "strike-b");
        var ally = Scholar(cache, 4, "strike-ally");
        var gone = Scholar(cache, 4, "strike-gone");
        a.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        b.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var ta = Enemy(cache, "strike-ta", 1, PlantType);
        var tb = Enemy(cache, "strike-tb", 1, PlantType);
        Set(gone, "m_Position", new Vector3(1, 0, 0));
        areaUnits = new[] { a, b, ally, gone };
        CastAdvice(a, ta);
        CastAdvice(b, tb);
        Check(HasAdvice(ally, b) && !HasAdvice(ally, a), "C13 ally holds only the last advice (B)");
        // Re-issue A last so ally and the far holder carry A's batch.
        CastAdvice(a, ta);
        Check(HasAdvice(ally, a) && HasAdvice(gone, a), "C13 A's batch reaches ally and the far holder");
        var batchB = AdviceBatchOf(b);
        Set(gone, "m_Position", new Vector3(20, 0, 0));
        CastStrike(a);
        Check(AdviceBatchOf(a) == null, "C13 conversion ends A's own batch");
        Check(!HasAdvice(ally, a) && !HasAdvice(gone, a) && !HasAdvice(a, a),
            "C13 all A-batch advice tokens are retired");
        Check(HasStrike(ally) && HasStrike(a) && !HasStrike(gone),
            "C13 in-range audible same-source holders get Strike; the far holder does not");
        Check(AdviceBatchOf(b) != null && AdviceBatchOf(b).Id == batchB.Id,
            "C13 scholar B's batch is untouched");
        Check(StrikeTokenOf(ally).Dice == 2, "C13 strike dice use the original advice source's level (8 = 2d6)");
        // No eligible holder: rejected without ending the batch.
        SetGameTime(TimeSpan.FromDays(51));
        var lonely = Scholar(cache, 4, "strike-lonely");
        lonely.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var tl = Enemy(cache, "strike-tl", 1, PlantType);
        areaUnits = new[] { lonely };
        CastAdvice(lonely, tl);
        Set(lonely, "m_Position", new Vector3(50, 0, 0));
        // The scholar itself is a holder but now... it is still its own center;
        // instead silence it so no eligible holder remains.
        var silence = new BlueprintBuff { AssetGuid = P("e4b8a158e8aabfe41b5e0bd334d8c386"), name = "Silence", ComponentsArray = Array.Empty<BlueprintComponent>() };
        silence.OnEnable();
        Effect(lonely, silence, lonely);
        var held = AdviceBatchOf(lonely);
        CastStrike(lonely);
        Check(AdviceBatchOf(lonely) != null && AdviceBatchOf(lonely).Id == held.Id && !HasStrike(lonely),
            "C13 no eligible recipient rejects the conversion without ending the batch");
    }
    private static bool HasStrike(UnitEntityData u)
        => u.Buffs.Enumerable.Any(b => b.Blueprint.AssetGuid == StrikeBuff(MainCache).AssetGuid);
    private static CryptidStrikeToken0070 StrikeTokenOf(UnitEntityData u)
    {
        var buff = u.Buffs.Enumerable.First(b => b.Blueprint.AssetGuid == StrikeBuff(MainCache).AssetGuid);
        foreach (var runtime in buff.Components.Where(r => r.SourceBlueprintComponent is CryptidStrikeBuff0070))
            using (runtime.RequestEventContext())
                return ((CryptidStrikeBuff0070)runtime.SourceBlueprintComponent).Token;
        return null;
    }

    // ================= C0070-14: real next weapon hit =================
    private static void RealWeaponHit(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(60));
        var scholar = Scholar(cache, 8, "hit");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var monster = Enemy(cache, "hit-monster", 1, AnimalType);
        var other = Enemy(cache, "hit-other", 1, UndeadType);
        areaUnits = new[] { scholar };
        CastAdvice(scholar, monster);
        CastStrike(scholar);
        Check(HasStrike(scholar), "C14 scholar holds the converted strike");
        var melee = MeleeWeapon();
        // Miss: not consumed.
        var miss = DamageEvent(scholar, monster, Attack(scholar, monster, melee, false), melee);
        RulebookEventBus.OnEventAboutToTrigger(miss); RulebookEventBus.OnEventDidTrigger(miss);
        Check(PrecisionPackages(miss) == 0 && HasStrike(scholar), "C14 a miss neither adds precision nor consumes");
        // Hit on a different category: not consumed.
        var wrong = DamageEvent(scholar, other, Attack(scholar, other, melee, true), melee);
        RulebookEventBus.OnEventAboutToTrigger(wrong); RulebookEventBus.OnEventDidTrigger(wrong);
        Check(PrecisionPackages(wrong) == 0 && HasStrike(scholar), "C14 a hit on another category does not consume");
        // Matching hit: one precision package, consumed once across bundles.
        var roll = Attack(scholar, monster, melee, true);
        var first = DamageEvent(scholar, monster, roll, melee);
        RulebookEventBus.OnEventAboutToTrigger(first); RulebookEventBus.OnEventDidTrigger(first);
        Check(PrecisionPackages(first) == 1 && !HasStrike(scholar), "C14 matching real hit adds exactly one precision package and consumes");
        var second = DamageEvent(scholar, monster, roll, melee);
        RulebookEventBus.OnEventAboutToTrigger(second); RulebookEventBus.OnEventDidTrigger(second);
        Check(PrecisionPackages(second) == 0, "C14 a second damage bundle of the same attack adds nothing");
        var replay = DamageEvent(scholar, monster, roll, melee);
        RulebookEventBus.OnEventAboutToTrigger(replay); RulebookEventBus.OnEventDidTrigger(replay);
        Check(PrecisionPackages(replay) == 0, "C14 duplicate callback adds nothing");
        var weaponDamage = first.DamageBundle.First();
        Check(!weaponDamage.Precision, "C14 native ordinary weapon damage is preserved");
    }
    private static ItemEntityWeapon MakeWeapon(string name, AttackType attack, WeaponCategory? category = null)
    {
        var wt = new BlueprintWeaponType { name = name + "Type", AssetGuid = P(Guid.NewGuid().ToString("N")), ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(wt, "m_AttackType", attack);
        if (category.HasValue) Set(wt, "Category", category.Value);
        wt.OnEnable();
        var bp = new BlueprintItemWeapon { name = name, AssetGuid = P(Guid.NewGuid().ToString("N")), ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(bp, "m_Type", Ref<BlueprintWeaponTypeReference>(wt));
        bp.OnEnable();
        return Weapon(bp);
    }
    private static ItemEntityWeapon MeleeWeapon() => MakeWeapon("0070-longsword", AttackType.Melee);
    private static ItemEntityWeapon RangedWeapon() => MakeWeapon("0070-shortbow", AttackType.Ranged);

    // ================= C0070-15: precision, crit, concealment =================
    private static void PrecisionBoundaries(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(70));
        var scholar = Scholar(cache, 12, "precision");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var monster = Enemy(cache, "precision-monster", 1, AnimalType);
        areaUnits = new[] { scholar };
        CastAdvice(scholar, monster);
        CastStrike(scholar);
        var melee = MeleeWeapon();
        // Critical hit: precision package has critical multiplier 1.
        var critRoll = Attack(scholar, monster, melee, true);
        Set(critRoll, "Result", AttackResult.CriticalHit);
        var crit = DamageEvent(scholar, monster, critRoll, melee);
        RulebookEventBus.OnEventAboutToTrigger(crit); RulebookEventBus.OnEventDidTrigger(crit);
        var precision = crit.DamageBundle.First(d => d.Precision);
        Check(precision.CriticalModifier == 1 && !precision.Sneak,
            "C15 independent precision package has crit multiplier 1 and is not a sneak event");
        Check(precision.Dice.ModifiedValue.Rolls == 3,
            "C15 level 12 strike is 3 dice (d6 authored in production)");
        // Concealment: hit through concealment does not consume.
        CastAdvice(scholar, monster); CastStrike(scholar);
        var concealedRoll = Attack(scholar, monster, melee, true);
        var concealment = new RuleConcealmentCheck(scholar, monster, true);
        Set(concealment, "Concealment", Concealment.Partial);
        Set(concealedRoll, "ConcealmentCheck", concealment);
        var concealed = DamageEvent(scholar, monster, concealedRoll, melee);
        RulebookEventBus.OnEventAboutToTrigger(concealed); RulebookEventBus.OnEventDidTrigger(concealed);
        Check(PrecisionPackages(concealed) == 0 && HasStrike(scholar),
            "C15 concealment rejects without consuming the opportunity");
        // Precision immunity: not consumed.
        var immuneRoll = Attack(scholar, monster, melee, true);
        Set(immuneRoll, "ImmuneToSneakAttack", true);
        var immune = DamageEvent(scholar, monster, immuneRoll, melee);
        RulebookEventBus.OnEventAboutToTrigger(immune); RulebookEventBus.OnEventDidTrigger(immune);
        Check(PrecisionPackages(immune) == 0 && HasStrike(scholar),
            "C15 precision immunity rejects without consuming");
        // DisablePrecisionDamage: not consumed.
        var disabledRoll = Attack(scholar, monster, melee, true);
        var disabled = DamageEvent(scholar, monster, disabledRoll, melee);
        disabled.ParentRule.DisablePrecisionDamage = true;
        RulebookEventBus.OnEventAboutToTrigger(disabled); RulebookEventBus.OnEventDidTrigger(disabled);
        Check(PrecisionPackages(disabled) == 0 && HasStrike(scholar),
            "C15 DisablePrecisionDamage rejects without consuming");
        // Fortification: native protection negates the precision package.
        var fortRoll = Attack(scholar, monster, melee, true);
        Set(fortRoll, "FortificationChance", 100);
        forcedDice = 1; // d100 = 1 never overcomes 100% fortification
        var fort = DamageEvent(scholar, monster, fortRoll, melee);
        RulebookEventBus.OnEventAboutToTrigger(fort); RulebookEventBus.OnEventDidTrigger(fort);
        forcedDice = null;
        Check(PrecisionPackages(fort) == 0 && !HasStrike(scholar),
            "C15 failed native fortification negates the precision damage once the threshold passed");
    }

    // ================= C0070-16: weapon types and ranged boundary =================
    private static void WeaponAndRangeBoundaries(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(80));
        var scholar = Scholar(cache, 4, "weapon");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var monster = Enemy(cache, "weapon-monster", 1, AnimalType);
        areaUnits = new[] { scholar };
        var melee = MeleeWeapon();
        var ranged = RangedWeapon();
        foreach (var weapon in new[] { melee, ranged })
        {
            CastAdvice(scholar, monster); CastStrike(scholar);
            Set(monster, "m_Position", new Vector3(9.144f, 0, 0));
            var evt = DamageEvent(scholar, monster, Attack(scholar, monster, weapon, true), weapon);
            RulebookEventBus.OnEventAboutToTrigger(evt); RulebookEventBus.OnEventDidTrigger(evt);
            Check(PrecisionPackages(evt) == 1, "C16 " + weapon.Blueprint.name + " at exactly 30ft triggers");
        }
        // Unarmed and natural weapons are real weapons in the bundle.
        foreach (var category in new[] { WeaponCategory.UnarmedStrike, WeaponCategory.Bite })
        {
            var item = MakeWeapon("0070-" + category, AttackType.Melee, category);
            CastAdvice(scholar, monster); CastStrike(scholar);
            Set(monster, "m_Position", Vector3.zero);
            var evt = DamageEvent(scholar, monster, Attack(scholar, monster, item, true), item);
            RulebookEventBus.OnEventAboutToTrigger(evt); RulebookEventBus.OnEventDidTrigger(evt);
            Check(PrecisionPackages(evt) == 1, "C16 " + category + " attack triggers");
        }
        // Ranged beyond 30ft: not consumed.
        Resource(scholar, haResource, 10);
        CastAdvice(scholar, monster); CastStrike(scholar);
        Set(monster, "m_Position", new Vector3(9.2f, 0, 0));
        var far = DamageEvent(scholar, monster, Attack(scholar, monster, ranged, true), ranged);
        RulebookEventBus.OnEventAboutToTrigger(far); RulebookEventBus.OnEventDidTrigger(far);
        Check(PrecisionPackages(far) == 0 && HasStrike(scholar), "C16 ranged beyond 30ft does not consume");
        // Pure ray: attack roll but no weapon in the bundle.
        var ray = DamageEvent(scholar, monster, Attack(scholar, monster, melee, true), null);
        RulebookEventBus.OnEventAboutToTrigger(ray); RulebookEventBus.OnEventDidTrigger(ray);
        Check(PrecisionPackages(ray) == 0 && HasStrike(scholar), "C16 a ray without a weapon does not consume");
        // Non-weapon direct damage: no attack roll at all.
        var spell = DamageEvent(scholar, monster, null, null);
        RulebookEventBus.OnEventAboutToTrigger(spell); RulebookEventBus.OnEventDidTrigger(spell);
        Check(PrecisionPackages(spell) == 0 && HasStrike(scholar), "C16 independent spell damage does not consume");
        // Melee still works after the rejections.
        Set(monster, "m_Position", Vector3.zero);
        var final = DamageEvent(scholar, monster, Attack(scholar, monster, melee, true), melee);
        RulebookEventBus.OnEventAboutToTrigger(final); RulebookEventBus.OnEventDidTrigger(final);
        Check(PrecisionPackages(final) == 1 && !HasStrike(scholar), "C16 the preserved opportunity fires on the next qualifying melee hit");
    }

    // ================= C0070-17: talent restriction =================
    private static void TalentRestriction(BlueprintsCache cache)
    {
        var scholar = Scholar(cache, 5, "talent");
        var plain = Unit(cache, haClass, 5, "talent-plain");
        var state = Empty<LevelUpState>();
        Set(state, "m_Unit", scholar);
        Set(state, "Selections", new List<FeatureSelectionState>());
        Set(state, "NextCharacterLevel", 5);
        state.NextClassLevel = 5;
        state.SelectedClass = haClass;
        var pending = state.AddSelection(null, default(FeatureSource), haTalents, 1);
        Func<UnitEntityData, BlueprintFeature, bool> canSelect = (u, f) => haTalents.CanSelect(u.Descriptor, state, pending, f);
        foreach (var talent in excludedTalentRoots)
            Check(!canSelect(scholar, talent), "C17 scholar talent path rejects " + talent.name);
        Check(canSelect(scholar, legalTalent), "C17 scholar keeps legal talent options");
        foreach (var talent in excludedTalentRoots)
            Check(canSelect(plain, talent), "C17 plain HA investigator keeps " + talent.name);
        // Nested selection and Extra Investigator Talent ancestry.
        var nested = Stage(cache, new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true, ComponentsArray = Array.Empty<BlueprintComponent>() }, Guid.NewGuid().ToString("N"), "NestedTalentSelection");
        Set(nested, "m_Features", excludedTalentRoots.Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());
        Set(nested, "m_AllFeatures", excludedTalentRoots.Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());
        var nestedState = state.AddSelection(pending, default(FeatureSource), nested, 1);
        Check(!haTalents.CanSelect(scholar.Descriptor, state, nestedState, excludedTalentRoots[0]),
            "C17 nested selection ancestry is restricted");
        var extraId = Id("ExtraInvestigatorTalentFeat");
        var extra = cache.Load(extraId) as BlueprintFeatureSelection;
        if (extra == null)
        {
            extra = Stage(cache, new BlueprintFeatureSelection { Ranks = 1, Groups = new[] { FeatureGroup.Feat }, IsClassFeature = true, ComponentsArray = Array.Empty<BlueprintComponent>() }, extraId.ToString(), "CotWWrathPsychicExtraInvestigatorTalentFeat");
            Set(extra, "m_Features", excludedTalentRoots.Concat(new[] { legalTalent }).Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());
            Set(extra, "m_AllFeatures", excludedTalentRoots.Concat(new[] { legalTalent }).Select(f => Ref<BlueprintFeatureReference>(f)).ToArray());
        }
        var extraState = state.AddSelection(null, default(FeatureSource), extra, 1);
        Check(!extra.CanSelect(scholar.Descriptor, state, extraState, excludedTalentRoots[0]),
            "C17 Extra Investigator Talent entry is restricted for the scholar");
        Check(extra.CanSelect(plain.Descriptor, state, extraState, excludedTalentRoots[0]),
            "C17 Extra Investigator Talent stays open for plain HA");
        var foreign = state.AddSelection(null, default(FeatureSource), (BlueprintFeatureSelection)cache.Load(P(BasicFeatsId)), 1);
        Check(haTalents.CanSelect(scholar.Descriptor, state, foreign, excludedTalentRoots[0]),
            "C17 an unrelated feat selection ancestry is not restricted");
    }

    // ================= C0070-18: plain HA and old variant =================
    private static void PlainHaAndOldVariant(BlueprintsCache cache)
    {
        var entriesBefore = haProgression.LevelEntries.Select(e => e.Level + ":" + string.Join(",", e.Features.Select(f => f.AssetGuid))).ToArray();
        Check(Register(cache), "C18 re-registration is idempotent");
        var entriesAfter = haProgression.LevelEntries.Select(e => e.Level + ":" + string.Join(",", e.Features.Select(f => f.AssetGuid))).ToArray();
        Check(entriesBefore.SequenceEqual(entriesAfter), "C18 provider progression facts are never rewritten");
        Check(haClass.Spellbook.AssetGuid == P(OldBookId), "C18 provider spellbook stays the old book");
        // The 0068 detective scope is independent: HasDetective is false for a
        // scholar and HasCryptidScholar is false for a detective-shaped unit.
        var scholar = Scholar(cache, 5, "scope-scholar");
        var detectiveScope = (bool)Call(T("PsychicDetectiveTalent0068Patch"), "HasDetective", scholar.Descriptor);
        Check(!detectiveScope, "C18 the 0068 detective restriction does not fire on a scholar");
        var foreignArchetype = Stage(cache, new BlueprintArchetype { ComponentsArray = Array.Empty<BlueprintComponent>() }, Guid.NewGuid().ToString("N"), "ForeignArchetype");
        Set(foreignArchetype, "m_ParentClass", haClass);
        var other = Unit(cache, haClass, 5, "scope-other");
        other.Progression.Classes[0].AddArchetype(foreignArchetype);
        var scholarScope = (bool)Call(T("CryptidScholarTalent0070Patch"), "HasCryptidScholar", other.Descriptor);
        Check(!scholarScope, "C18 the scholar restriction does not fire on another archetype");
        var shared = (bool)Call(T("PsychicDetectiveTalent0068Patch"), "FromInvestigatorTalents",
            Empty<FeatureSelectionState>());
        Check(!shared, "C18 shared ancestry helper still resolves an empty state safely");
    }

    // ================= C0070-19: restore and same-source cleanup =================
    private static void RestoreAndCleanup(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(90));
        var a = Scholar(cache, 8, "restore-a");
        var b = Scholar(cache, 8, "restore-b");
        var ally = Scholar(cache, 4, "restore-ally");
        a.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        b.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var target = Enemy(cache, "restore-target", 1, AnimalType);
        areaUnits = new[] { a, b, ally };
        CastAdvice(a, target);
        CastStrike(a);
        Check(HasStrike(ally) && HasStrike(b) && HasStrike(a), "C19 all same-source holders hold A's strike");
        CastAdvice(b, target);
        Check(HasAdvice(ally, b), "C19 ally now holds B's advice");
        var ledgerFact = a.Facts.List.OfType<Feature>().First(f => f.Blueprint.AssetGuid == Lore(cache).AssetGuid);
        var runtime = ledgerFact.Components.First(r => r.SourceBlueprintComponent is CryptidScholarLedger0070);
        CryptidScholarLedgerData0070 data;
        using (runtime.RequestEventContext()) data = ((CryptidScholarLedger0070)runtime.SourceBlueprintComponent).Ledger;
        var settings = new JsonSerializerSettings { ContractResolver = new Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver() };
        var serializer = JsonSerializer.Create(settings);
        var json = JObject.FromObject(data, serializer);
        var restored = json.ToObject<CryptidScholarLedgerData0070>(serializer);
        Check(restored.Credentials.SequenceEqual(data.Credentials) && restored.NextBatchId == data.NextBatchId &&
              restored.Receipts.Count == data.Receipts.Count &&
              restored.Receipts[0].Expires == data.Receipts[0].Expires,
            "C19 ledger data round-trips credentials, receipts and batch identity");
        var strikeJson = JObject.FromObject(StrikeTokenOf(ally), serializer);
        var strikeRestored = strikeJson.ToObject<CryptidStrikeToken0070>(serializer);
        Check(strikeRestored.Key == StrikeTokenOf(ally).Key && strikeRestored.Dice == 2 && strikeRestored.Expires == StrikeTokenOf(ally).Expires,
            "C19 strike token round-trips category, dice and expiry");
        // Repeated post-load fixes neither refund nor re-grant.
        var balance = ResourceAmount(a, haResource);
        var postLoad = AccessTools.Method(typeof(CryptidScholarLedger0070), "OnApplyPostLoadFixes");
        using (runtime.RequestEventContext()) postLoad.Invoke(runtime.SourceBlueprintComponent, null);
        using (runtime.RequestEventContext()) postLoad.Invoke(runtime.SourceBlueprintComponent, null);
        Check(ResourceAmount(a, haResource) == balance && HasStrike(ally),
            "C19 repeated load fixes neither refund resources nor re-grant or drop opportunities");
        // Losing the variant retires only this scholar's authorisations.
        Remove(ledgerFact);
        Check(!HasStrike(ally) && !HasStrike(b) && !world.Any(u => HasAdvice(u, a)),
            "C19 losing the variant removes A's strikes and advice everywhere");
        Check(AdviceBatchOf(b) != null && HasAdvice(ally, b),
            "C19 scholar B's batch and the ally's B-advice survive");
        Check(CredentialOf(b).Count > 0, "C19 scholar B's credentials survive");
    }

    // ================= C0070-20: expiry and type change =================
    private static void ExpiryAndTypeChange(BlueprintsCache cache)
    {
        SetGameTime(TimeSpan.FromDays(100));
        var scholar = Scholar(cache, 4, "expiry", intScore: 16); // 3 rounds
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 30;
        var target = Enemy(cache, "expiry-target", 1, AnimalType);
        areaUnits = new[] { scholar };
        CastAdvice(scholar, target);
        var batch = AdviceBatchOf(scholar);
        var attacker = Enemy(cache, "expiry-attacker", 1, AnimalType);
        Check(Armor(attacker, scholar, AttackType.Melee) == 11, "C20 active advice applies before expiry (level 4 = +1)");
        SetGameTime(batch.Expires + TimeSpan.FromSeconds(1));
        Check(Armor(attacker, scholar, AttackType.Melee) == 10,
            "C20 expired advice no longer settles even if the token remains");
        CastStrike(scholar);
        Check(!HasStrike(scholar), "C20 an expired batch cannot be converted");
        // Strike expiry.
        SetGameTime(TimeSpan.FromDays(101));
        CastAdvice(scholar, target);
        CastStrike(scholar);
        Check(HasStrike(scholar), "C20 fresh strike exists");
        var token = StrikeTokenOf(scholar);
        SetGameTime(token.Expires + TimeSpan.FromSeconds(1));
        var melee = MeleeWeapon();
        var evt = DamageEvent(scholar, target, Attack(scholar, target, melee, true), melee);
        RulebookEventBus.OnEventAboutToTrigger(evt); RulebookEventBus.OnEventDidTrigger(evt);
        Check(PrecisionPackages(evt) == 0, "C20 expired strike does not add precision");
        // Type change: old credentials only match the original key.
        SetGameTime(TimeSpan.FromDays(102));
        var oldKey = (string)Call("CryptidClassification0070", "KeyOf", target);
        Check(CredentialOf(scholar).Contains(oldKey), "C20 original credential exists");
        var animalFact = target.Facts.List.OfType<Feature>().First(f => f.Blueprint.AssetGuid == P(AnimalType));
        Remove(animalFact);
        var undead = (BlueprintFeature)cache.Load(P(UndeadType));
        var undeadComponents = undead.ComponentsArray;
        undead.ComponentsArray = Array.Empty<BlueprintComponent>();
        Fact(target, undead);
        undead.ComponentsArray = undeadComponents;
        var newKey = (string)Call("CryptidClassification0070", "KeyOf", target);
        Check(newKey != oldKey && !CredentialOf(scholar).Contains(newKey),
            "C20 changed type produces a new key without inheriting the old credential");
        scholar.Stats.GetStat(StatType.SkillLoreNature).BaseValue = 1; // cannot succeed DC 11 now
        // The changed target itself is DC 11; with skill 1 the check may fail.
        forcedDice = 1;
        CastAdvice(scholar, target);
        forcedDice = null;
        Check(!CredentialOf(scholar).Contains(newKey),
            "C20 a failed re-identification of the new type creates no credential");
        forcedDice = 20;
        CastAdvice(scholar, target);
        forcedDice = null;
        Check(CredentialOf(scholar).Contains(newKey) && AdviceBatchOf(scholar).Key == newKey,
            "C20 a real successful re-identification records the new category");
    }
}