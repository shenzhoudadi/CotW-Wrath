// COTW-0099 Occultist, B stage. D-0099-01 directed native check.
// Contract C-0099-20261115-r1, sections O-S01 (Mental Focus) / O-S02 (spells).
//
// SCOPE. This is an isolated managed host, NOT player-game acceptance. It runs
// the real Wrath entry points against the real game Managed assemblies:
//   * AbilityCastRateUtils.SpendResources -> AbilityData.ResourceLogic
//     -> the shipped Occultist0099FocusResourceLogic (a real
//     AbilityResourceLogic subclass) -> the authoritative state machine.
//   * SelectSpell.Check / SelectSpell.Apply and Spellbook.AddKnown, with the
//     shipped Occultist0099NativeSelectCheck / Occultist0099NativeLearnCommit
//     Harmony bridges installed exactly as production installs them.
//   * RestController.ApplyRest -> EventBus IUnitRestHandler, with the camp
//     serial identity read from the real CampingState.OverallCampsCount.
//   * The real EntityFactsManager / Feature / UnitFactComponentDelegate
//     lifecycle, so OnActivate and the component event context are native.
//
// DECLARED BOUNDARIES. Unity rendering, the level-up UI/VM layer, the whole
// rest coroutine (sleep phases, encounters, cutscenes), save/load transport,
// real third-party provider assemblies and the full historical suites are NOT
// run. UnitDescriptor.Replenish is skipped inside ApplyRest exactly as the
// reviewed 0081 host does, because it restores every unrelated resource of the
// descriptor; the 0099 refill is proven through the authoritative state and the
// real UnitAbilityResourceCollection instead.
//
// The private class / spellbook / list / fact blueprints are STAGED at their
// private 0099 identities for this directed check only. SlicePublished stays
// false and no shared registration chain is touched.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Rest;
using Kingmaker.Controllers.Rest.State;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Inspect;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using CallOfTheWild.Wrath.Prototype;

internal static class Occultist0099NativeLauncher
{
    private static int Main(string[] args)
    {
        if (args.Length != 5) return 2;
        var runtimePath = Environment.GetEnvironmentVariable("COTW_HARMONY_MATRIX_PATH");
        if (!string.IsNullOrEmpty(runtimePath)) Assembly.LoadFrom(runtimePath);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            if (new AssemblyName(e.Name).Name == "0Harmony" &&
                !string.IsNullOrEmpty(runtimePath))
                return Assembly.LoadFrom(runtimePath);
            foreach (var dir in new[] { args[0], Path.GetDirectoryName(args[1]) })
            {
                var file = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try
        {
            Occultist0099NativeTests.Run(args);
            return 0;
        }
        catch (Exception e)
        {
            while (e != null)
            {
                Console.Error.WriteLine(e.GetType().FullName + ": " + e.Message +
                                        "\n" + e.StackTrace);
                e = e.InnerException;
            }
            return 1;
        }
    }
}

internal static class Occultist0099NativeTests
{
    private static int checks, hostLogs, serial;
    private static readonly List<string> failures = new List<string>();
    private static readonly Dictionary<string, string> caseStatus =
        new Dictionary<string, string>();
    private static Harmony harmony;
    private static BlueprintsCache cache;
    private static JObject nativeRows;
    private static readonly List<UnitEntityData> world = new List<UnitEntityData>();
    private static int skippedComponents;
    private static TimeSpan currentTime = TimeSpan.FromHours(8);

    // ------------------------------------------------------------------
    // Assertion helpers. A failed check is recorded and reported, never
    // silently swallowed.
    // ------------------------------------------------------------------
    private static void Check(bool ok, string message)
    {
        checks++;
        if (ok) { Console.WriteLine("0099N CHECK: " + message); return; }
        failures.Add(message);
        Console.WriteLine("0099N CHECK FAILED: " + message);
    }
    private static void Eq<T>(T expected, T actual, string message)
        => Check(EqualityComparer<T>.Default.Equals(expected, actual),
            message + " (expected " + expected + ", actual " + actual + ")");
    private static void Stable(string before, string after, string message)
        => Check(before == after, message);

    private static object Get(object o, string f)
        => AccessTools.Field(o.GetType(), f)?.GetValue(o)
           ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")?.GetValue(o);
    private static void Set(object o, string f, object v)
        => (AccessTools.Field(o.GetType(), f)
            ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")
            ?? throw new MissingFieldException(o.GetType().FullName, f)).SetValue(o, v);
    private static T Empty<T>() where T : class
        => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static BlueprintGuid P(string id) => BlueprintGuid.Parse(id);
    private static TRef Ref<TRef>(SimpleBlueprint bp) where TRef : BlueprintReferenceBase, new()
    {
        var r = BlueprintReferenceBase.CreateTyped<TRef>(bp);
        AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null);
        return r;
    }

    // ------------------------------------------------------------------
    // Host boundaries. Each one is a scene/service substitution, never a rule
    // substitution: no 0099 decision is made by any of these.
    // ------------------------------------------------------------------
    private static bool NotPlayerFaction(ref bool __result) { __result = false; return false; }
    private static bool NotTacticalCombat(ref bool __result) { __result = false; return false; }
    private static bool NextUniqueId(ref string __result)
    { __result = "0099N-" + (++serial).ToString("X"); return false; }
    private static bool TimeGetter(ref TimeSpan __result) { __result = currentTime; return false; }
    private static bool NoReplenish() => false;
    private static bool HostLogFirst(Exception __0) { HostLog(__0); return false; }
    private static bool HostLogSecond(object __0, Exception __1) { HostLog(__1); return false; }
    private static void HostLog(Exception error)
    {
        if (error == null) return;
        hostLogs++;
        Console.WriteLine("0099N HOST LOG: " + error);
    }

    private static void InstallBoundaries()
    {
        harmony.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"),
            prefix: new HarmonyMethod(AccessTools.Method(
                typeof(Occultist0099NativeTests), nameof(NotPlayerFaction))));
        harmony.Patch(AccessTools.PropertyGetter(
                typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"),
            prefix: new HarmonyMethod(AccessTools.Method(
                typeof(Occultist0099NativeTests), nameof(NotTacticalCombat))));
        harmony.Patch(AccessTools.Method(typeof(Kingmaker.Player), "GetNewUniqueId"),
            prefix: new HarmonyMethod(AccessTools.Method(
                typeof(Occultist0099NativeTests), nameof(NextUniqueId))));
        harmony.Patch(AccessTools.PropertyGetter(
                typeof(Kingmaker.Controllers.TimeController), "GameTime"),
            prefix: new HarmonyMethod(AccessTools.Method(
                typeof(Occultist0099NativeTests), nameof(TimeGetter))));
        foreach (var signature in new[]
        {
            new[] { typeof(Exception), typeof(string), typeof(object[]) },
            new[] { typeof(UnityEngine.Object), typeof(Exception), typeof(string), typeof(object[]) },
            new[] { typeof(Owlcat.Runtime.Core.Logging.ICanBeLogContext),
                    typeof(Exception), typeof(string), typeof(object[]) }
        })
        {
            var method = AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),
                "Exception", signature);
            if (method == null) continue;
            harmony.Patch(method, prefix: new HarmonyMethod(signature.Length == 3
                ? AccessTools.Method(typeof(Occultist0099NativeTests), nameof(HostLogFirst))
                : AccessTools.Method(typeof(Occultist0099NativeTests), nameof(HostLogSecond))));
        }
    }

    // The shipped production bridges, installed through the same
    // CreateClassProcessor path the module uses at startup.
    private static int InstallProductionBridges()
    {
        var assembly = typeof(Occultist0099Ids).Assembly;
        var bridges = assembly.GetTypes()
            .Where(t => t.Name.StartsWith("Occultist0099Native", StringComparison.Ordinal) &&
                        t.GetCustomAttributes(typeof(HarmonyPatch), false).Any())
            .ToArray();
        foreach (var type in bridges) harmony.CreateClassProcessor(type).Patch();
        return bridges.Length;
    }

    // ------------------------------------------------------------------
    // Game / unit fixture, following the reviewed CryptidScholar0070 shape.
    // ------------------------------------------------------------------
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
        var player = (Kingmaker.Player)AccessTools.Field(stateField.FieldType, "PlayerState")
            .GetValue(state);
        Set(player, "m_Party", new List<UnitEntityData>());
        Set(player, "m_PartyAndPets", new List<UnitEntityData>());
        Set(player, "m_PartyAndPetsDetached", new List<UnitEntityData>());
        Set(player, "PartyCharacters", new List<UnitReference>());
        Set(player, "m_CharacterListsValid", true);
        // The real CampingState: its OverallCampsCount is the rest identity the
        // production adapter reads. It is a genuine native object, not a stub.
        Set(player, "m_Camping", new CampingState());
        AccessTools.Field(typeof(Kingmaker.Game), "TimeController").SetValue(game,
            FormatterServices.GetUninitializedObject(
                typeof(Kingmaker.Controllers.TimeController)));
        AccessTools.Field(typeof(Kingmaker.Game), "Rulebook").SetValue(game,
            Activator.CreateInstance(typeof(Kingmaker.RuleSystem.Rulebook)));
        AccessTools.Field(typeof(Kingmaker.Player), "InspectUnitsManager").SetValue(player,
            Activator.CreateInstance(typeof(InspectUnitsManager)));
    }

    private static UnitEntityData Unit(string label, BlueprintCharacterClass cls,
        int level, int intelligence)
    {
        var u = Empty<UnitEntityData>();
        var descriptor = Empty<UnitDescriptor>();
        Set(u, "Descriptor", descriptor);
        Set(descriptor, "Unit", u);
        Set(u, "Parts", new EntityPartsManager(u));
        Set(u, "UniqueId", "0099N-unit-" + label);
        var proxy = FormatterServices.GetUninitializedObject(
            AccessTools.Inner(typeof(EntityService), "Proxy"));
        Set(proxy, "Entity", u);
        Set(u, "Proxy", proxy);

        var facts = new EntityFactsManager(u);
        Set(u, "Facts", facts);
        Set(u, "Buffs", facts.EnsureFactProcessor<Kingmaker.UnitLogic.Buffs.BuffCollection>());

        var stats = Empty<CharacterStats>();
        Set(stats, "Owner", descriptor);
        Set(descriptor, "Stats", stats);
        var all = new List<ModifiableValue>();
        Set(stats, "AllStats", all);
        foreach (var stat in new[]
        {
            StatType.Intelligence, StatType.Wisdom, StatType.Charisma,
            StatType.Strength, StatType.Dexterity, StatType.Constitution
        })
        {
            var value = new ModifiableValueAttributeStat(stats, stat);
            Set(value, "Type", stat);
            int score = stat == StatType.Intelligence ? intelligence : 10;
            Set(value, "m_BaseValue", score);
            Set(value, "ModifiedValue", score);
            Set(stats, stat.ToString(), value);
            all.Add(value);
        }

        var state = Empty<UnitState>();
        var mechanics = Empty<UnitMechanicFeatures>();
        Set(mechanics, "DomainsByIntelligenceFeature", new Kingmaker.Utility.CountableFlag());
        Set(state, "Features", mechanics);
        Set(state, "m_Conditions", new sbyte[100]);
        Set(state, "m_ConditionsImmunity", new sbyte[100]);
        Set(descriptor, "State", state);

        var progression = Empty<UnitProgressionData>();
        Set(descriptor, "Progression", progression);
        Set(progression, "Classes", new List<ClassData>());
        if (cls != null && level > 0)
            ((List<ClassData>)Get(progression, "Classes")).Add(new ClassData(cls) { Level = level });
        Set(progression, "Features", facts.EnsureFactProcessor<FeatureCollection>());
        Set(u, "ActivatableAbilities",
            facts.EnsureFactProcessor<Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityCollection>());
        Set(u, "Abilities",
            facts.EnsureFactProcessor<Kingmaker.UnitLogic.Abilities.AbilityCollection>());
        Set(descriptor, "Resources", new UnitAbilityResourceCollection(descriptor));
        Set(descriptor, "Proficiencies", new UnitProficiency());
        Set(descriptor, "m_Spellbooks", new Dictionary<BlueprintSpellbook, Spellbook>());

        var body = new BlueprintUnit
        {
            name = "0099N-body-" + label,
            AssetGuid = P(Guid.NewGuid().ToString("N")),
            ComponentsArray = Array.Empty<BlueprintComponent>()
        };
        body.OnEnable();
        Set(descriptor, "Blueprint", body);
        Set(descriptor, "OriginalBlueprint", body);
        facts.TurnOn();
        world.Add(u);
        ((List<UnitEntityData>)Get(Kingmaker.Game.Instance.Player, "m_PartyAndPets")).Add(u);
        return u;
    }

    private static void SetInt(UnitEntityData u, int score)
    {
        var stat = u.Stats.Intelligence;
        Set(stat, "m_BaseValue", score);
        Set(stat, "ModifiedValue", score);
    }

    // ------------------------------------------------------------------
    // Native row materialisation. Only the audited rows this directed check
    // needs are loaded; the archive itself is a read-only input owned by an
    // earlier package.
    // ------------------------------------------------------------------
    private static readonly HashSet<string> neededTypes = new HashSet<string>
    {
        "BlueprintAbility", "BlueprintSpellbook", "BlueprintSpellsTable",
        "BlueprintSpellList", "BlueprintCharacterClass", "BlueprintFeature",
        "BlueprintAbilityResource", "BlueprintBuff", "BlueprintUnitFact"
    };

    private static void BindTypeIds()
    {
        var binder = (GuidClassBinder)Json.Serializer.Binder;
        foreach (var assembly in new[] { typeof(BlueprintsCache).Assembly,
                                         typeof(Occultist0099Ids).Assembly })
        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttributes(typeof(TypeIdAttribute), false)
                .Cast<TypeIdAttribute>().SingleOrDefault();
            if (attribute != null) binder.AddToCache(type, attribute.GuidString);
        }
    }

    private static SimpleBlueprint Materialise(JObject row)
    {
        var data = (JObject)row["Data"];
        string typeName = ((string)data["$type"]).Split(',').Last().Trim();
        var type = typeof(BlueprintsCache).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == typeName &&
                                 typeof(BlueprintScriptableObject).IsAssignableFrom(t));
        if (type == null) return null;
        var bp = (BlueprintScriptableObject)Activator.CreateInstance(type);
        bp.AssetGuid = P((string)row["AssetId"]);
        bp.name = "$0099N$" + Guid.NewGuid().ToString("N");
        // The archived component graph is deserialised by the real native
        // polymorphic serializer, so the shipped SpellComponent / SpellListComponent
        // identities and school data are genuine.
        var components = (JArray)data["Components"] ?? new JArray();
        var loaded = new List<BlueprintComponent>();
        // BlueprintComponent.OnDeserialized reads Json.BlueprintBeingRead.Data to
        // set OwnerBlueprint, exactly as the real archive loader does. Without
        // that context every native component throws, so the whole graph would
        // silently vanish. The wrapper is the same one the reviewed 01446 host
        // uses, and it is restored afterwards.
        var previousRead = Json.BlueprintBeingRead;
        try
        {
            Json.BlueprintBeingRead = new BlueprintJsonWrapper(bp);
            foreach (var component in components)
            {
                try
                {
                    using (var reader = component.CreateReader())
                    {
                        var instance = Json.Serializer.Deserialize(reader) as BlueprintComponent;
                        if (instance != null) loaded.Add(instance);
                    }
                }
                catch (Exception error)
                {
                    // A component outside this directed fixture is skipped, but
                    // the skip is always disclosed with its real cause.
                    var inner = error;
                    while (inner.InnerException != null) inner = inner.InnerException;
                    skippedComponents++;
                    Console.WriteLine("0099N FIXTURE SKIP: " + row["AssetId"] + " " +
                                      component["$type"] + " -> " +
                                      inner.GetType().Name + ": " + inner.Message);
                }
            }
        }
        finally { Json.BlueprintBeingRead = previousRead; }
        bp.ComponentsArray = loaded.ToArray();
        foreach (var field in new[] { "Type", "ActionType", "Range", "Spontaneous",
                                      "AllSpellsKnown", "CastingAttribute", "IsArcane",
                                      "SpellsPerLevel", "CantripsType", "IsMythic" })
        {
            var info = AccessTools.Field(type, field);
            var value = data[field];
            if (info == null || value == null || value.Type == JTokenType.Null) continue;
            try { info.SetValue(bp, value.ToObject(info.FieldType)); } catch { }
        }
        foreach (var field in new[] { "m_SpellList", "m_CharacterClass", "m_SpellsPerDay",
                                      "m_SpellsKnown", "m_SpellSlots" })
        {
            var info = AccessTools.Field(type, field);
            var value = data[field];
            if (info == null || value == null || value.Type == JTokenType.Null) continue;
            try { info.SetValue(bp, value.ToObject(info.FieldType)); } catch { }
        }
        if (bp is Kingmaker.Blueprints.Facts.BlueprintUnitFact)
            Set(bp, "m_Icon", Empty<UnityEngine.Sprite>());
        if (bp is BlueprintSpellsTable table && data["Levels"] != null)
            table.Levels = data["Levels"].ToObject<SpellsLevelEntry[]>();
        bp.OnEnable();
        cache.AddCachedBlueprint(bp.AssetGuid, bp);
        return bp;
    }

    private static T Native<T>(string guid) where T : SimpleBlueprint
        => cache.Load(P(guid)) as T;

    // ------------------------------------------------------------------
    // Roots the audited game archive does not carry.
    //
    // The four CotW roots are THIS project's own production output, so their
    // real builders run here (PsychicWardingSpells / PsychicGlobeSpells /
    // PsychicSupportSpells.Cloak) against the real native donors already loaded
    // from the archive. Only the Unity texture decode behind
    // PsychicOriginalIcons.Load is substituted, because ImageConversion is a
    // Unity native call an isolated managed host cannot make. That is a
    // disclosed presentation boundary, never a rule substitution.
    //
    // The one External root belongs to TabletopTweaks-Base, which is not
    // installed. It is staged at its frozen A identity with a real Abjuration
    // SpellComponent and a real executable effect path, and the production
    // provider gate still decides whether it is ever a candidate.
    // ------------------------------------------------------------------
    private static bool iconBoundaryInstalled;
    private static bool HostIcon(ref UnityEngine.Sprite __result)
    { __result = Empty<UnityEngine.Sprite>(); return false; }

    private static Type Production(string typeName)
        => typeof(Occultist0099Ids).Assembly
            .GetType("CallOfTheWild.Wrath.Prototype." + typeName, true);

    private static void StageAbsentRoot(Occultist0099SpellRoot root)
    {
        if (!iconBoundaryInstalled)
        {
            harmony.Patch(AccessTools.Method(Production("PsychicOriginalIcons"), "Load"),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(Occultist0099NativeTests), nameof(HostIcon))));
            iconBoundaryInstalled = true;
        }

        if (root.Source == "CotW")
        {
            // The real CotW builders read native icon/effect donors. Those rows
            // are in the same audited archive; load exactly the ones the builder
            // asks for, from the reviewed fixture, never invented.
            if (root.Guid == Occultist0099AbjurationCatalog.AlliedCloak)
                LoadRows("e418c20c8ce362943a8025d82c865c1c");   // CapeOfWasps icon donor
            var factory = Activator.CreateInstance(Production("PsychicAssets"),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new object[] { cache }, null);
            BlueprintAbility built = null;
            if (root.Guid == Occultist0099AbjurationCatalog.WardingWeapon)
            {
                // The builder is lazy; only the first entry (Warding Weapon) is
                // enumerated, so no unrelated spell is constructed.
                var entries = (System.Collections.IEnumerable)AccessTools
                    .Method(Production("PsychicWardingSpells"), "Build")
                    .Invoke(null, new[] { factory });
                foreach (var entry in entries)
                {
                    built = (BlueprintAbility)Get(entry, "Ability");
                    break;
                }
            }
            else if (root.Guid == Occultist0099AbjurationCatalog.GlobeOfInvulnerability ||
                     root.Guid == "7243c2cbfe8b027021a9f7b0270d7171")
            {
                var entries = (System.Collections.IEnumerable)AccessTools
                    .Method(Production("PsychicGlobeSpells"), "Build")
                    .Invoke(null, new[] { factory });
                foreach (var entry in entries)
                {
                    var ability = (BlueprintAbility)Get(entry, "Ability");
                    if (ability.AssetGuid.ToString() == root.Guid) { built = ability; break; }
                }
            }
            else if (root.Guid == Occultist0099AbjurationCatalog.AlliedCloak)
            {
                AccessTools.Method(Production("PsychicSupportSpells"), "Cloak")
                    .Invoke(null, new[] { factory });
                var assets = (System.Collections.IDictionary)Get(factory, "Assets");
                built = (BlueprintAbility)assets["AlliedCloak"];
            }
            Check(built != null && built.AssetGuid.ToString() == root.Guid,
                "HOST the real CotW builder publishes " + root.Name +
                " at its frozen A identity");
            if (built == null) return;
            built.OnEnable();
            cache.AddCachedBlueprint(built.AssetGuid, built);
            Check(built.GetComponent<SpellComponent>()?.School == SpellSchool.Abjuration,
                "HOST the production " + root.Name + " really is Abjuration");
            return;
        }

        // External provider root: contract-shaped, explicitly not the real
        // third-party blueprint.
        var apply = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff
        {
            name = "$0099N$ExternalEffect",
            DurationValue = new Kingmaker.UnitLogic.Mechanics.ContextDurationValue
            {
                Rate = Kingmaker.UnitLogic.Mechanics.DurationRate.Rounds,
                DiceType = Kingmaker.RuleSystem.DiceType.Zero,
                DiceCountValue = 0,
                BonusValue = new Kingmaker.UnitLogic.Mechanics.ContextValue
                { ValueType = Kingmaker.UnitLogic.Mechanics.ContextValueType.Simple, Value = 1 }
            }
        };
        var staged = new BlueprintAbility
        {
            Type = AbilityType.Spell,
            ActionType = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
            Range = AbilityRange.Personal,
            CanTargetSelf = true,
            ComponentsArray = new BlueprintComponent[]
            {
                new SpellComponent { name = "$0099N$ExternalSchool", School = SpellSchool.Abjuration },
                new AbilityEffectRunAction
                {
                    name = "$0099N$ExternalActions",
                    Actions = new Kingmaker.ElementsSystem.ActionList
                    { Actions = new Kingmaker.ElementsSystem.GameAction[] { apply } }
                }
            }
        };
        Stage(staged, root.Guid, "0099N.HostStaged." + root.Name);
        Console.WriteLine("0099N FIXTURE: host-staged external root " + root.Name +
                          " (" + root.Provider + "); the real provider assembly is NOT loaded");
    }

    private static void LoadRows(params string[] guids)
    {
        var wanted = new HashSet<string>(guids);
        foreach (var row in (JArray)nativeRows["requiredNative"])
        {
            var bp = (JObject)row["blueprint"];
            string id = (string)bp["AssetId"];
            if (!wanted.Contains(id) || cache.Load(P(id)) != null) continue;
            Materialise(bp);
        }
    }

    // ------------------------------------------------------------------
    // Private 0099 blueprints, staged for this directed check only.
    // ------------------------------------------------------------------
    private static BlueprintCharacterClass occultistClass;
    private static BlueprintSpellbook occultistBook;
    private static BlueprintSpellList occultistList;
    private static BlueprintFeature focusFact;
    private static BlueprintFeature spellbookFact;
    private static BlueprintAbilityResource focusResource;
    private static BlueprintAbility mindBarrier;

    private static T Stage<T>(T bp, string guid, string name) where T : SimpleBlueprint
    {
        bp.AssetGuid = P(guid);
        bp.name = name;
        if (bp is Kingmaker.Blueprints.Facts.BlueprintUnitFact)
            Set(bp, "m_Icon", Empty<UnityEngine.Sprite>());
        if (bp is BlueprintScriptableObject so && so.ComponentsArray == null)
            so.ComponentsArray = Array.Empty<BlueprintComponent>();
        bp.OnEnable();
        cache.AddCachedBlueprint(bp.AssetGuid, bp);
        return bp;
    }

    private static void StagePrivateSlice()
    {
        occultistClass = Stage(new BlueprintCharacterClass
        {
            ComponentsArray = Array.Empty<BlueprintComponent>()
        }, Occultist0099Ids.ClassGuid, "0099N.OccultistClass");

        // The private six-circle list. Its rows carry the audited Abjuration
        // roots at their PRIVATE circles, taken from the production catalogue.
        occultistList = new BlueprintSpellList();
        var rows = new List<SpellLevelList>();
        for (int circle = 0; circle <= 6; circle++)
        {
            var row = new SpellLevelList(circle);
            foreach (var root in Occultist0099AbjurationCatalog.Roots
                         .Where(r => r.Circle == circle))
            {
                var ability = Native<BlueprintAbility>(root.Guid);
                if (ability != null) row.SpellsRefs.Add(Ref<BlueprintAbilityReference>(ability));
            }
            rows.Add(row);
        }
        occultistList.SpellsByLevel = rows.ToArray();
        Stage(occultistList, Occultist0099Ids.SpellListGuid, "0099N.OccultistSpellList");

        // The private book uses the audited Alchemist six-circle donor table for
        // its per-day counts (A dailySpellSlots.tableDonor) but its OWN identity,
        // list and class. The donor book itself is never modified.
        var donorTable = Native<BlueprintSpellsTable>("bf4a0a03a45275d438b8c59cd5388259");
        occultistBook = new BlueprintSpellbook
        {
            Spontaneous = false,
            AllSpellsKnown = false,
            CastingAttribute = StatType.Intelligence,
            ComponentsArray = new BlueprintComponent[]
            {
                new Occultist0099SpellbookMarker0099 { name = "$0099N$Marker" }
            }
        };
        Set(occultistBook, "m_CharacterClass",
            Ref<BlueprintCharacterClassReference>(occultistClass));
        Set(occultistBook, "m_SpellList", Ref<BlueprintSpellListReference>(occultistList));
        Set(occultistBook, "m_SpellsPerDay",
            Ref<BlueprintSpellsTableReference>(donorTable));
        Stage(occultistBook, Occultist0099Ids.BookGuid, "0099N.OccultistSpellbook");

        focusResource = Stage(new BlueprintAbilityResource
        {
            ComponentsArray = Array.Empty<BlueprintComponent>()
        }, Occultist0099Ids.FocusResourceGuid, "0099N.MentalFocusResource");

        // The authoritative Mental Focus fact. AddAbilityResources attaches the
        // display mirror with RestoreAmount=false, so the native maximum never
        // becomes a second source of truth.
        var addResource = new AddAbilityResources { name = "$0099N$AddResource",
                                                   RestoreAmount = false };
        Set(addResource, "m_Resource", Ref<BlueprintAbilityResourceReference>(focusResource));
        var focusComponent = new Occultist0099FocusFact0099
        {
            name = "$0099N$FocusFact",
            CharacterClass = Ref<BlueprintCharacterClassReference>(occultistClass),
            FocusResource = Ref<BlueprintAbilityResourceReference>(focusResource)
        };
        focusFact = Stage(new BlueprintFeature
        {
            ComponentsArray = new BlueprintComponent[] { addResource, focusComponent }
        }, Occultist0099Ids.FocusFactGuid, "0099N.MentalFocusFact");

        var bookComponent = new Occultist0099SpellbookFact0099
        {
            name = "$0099N$SpellbookFact",
            CharacterClass = Ref<BlueprintCharacterClassReference>(occultistClass)
        };
        spellbookFact = Stage(new BlueprintFeature
        {
            ComponentsArray = new BlueprintComponent[] { bookComponent }
        }, Occultist0099Ids.SpellbookFactGuid, "0099N.OccultistSpellbookFact");

        // A real focus power ability whose resource logic is the shipped 0099
        // subclass. The native cast pipeline finds it through
        // BlueprintAbility.GetComponent<AbilityResourceLogic>().
        mindBarrier = Stage(new BlueprintAbility
        {
            Type = AbilityType.SpellLike,
            ComponentsArray = new BlueprintComponent[]
            {
                new Occultist0099FocusResourceLogic
                {
                    name = "$0099N$FocusCost",
                    School = "Abjuration",
                    Amount = 1
                }
            }
        }, Occultist0099Ids.MindBarrierAbilityGuid, "0099N.MindBarrier");
        Set(mindBarrier.GetComponent<AbilityResourceLogic>(), "m_RequiredResource",
            Ref<BlueprintAbilityResourceReference>(focusResource));
        Set(mindBarrier.GetComponent<AbilityResourceLogic>(), "m_IsSpendResource", true);

        // Attach the private learning gate to each audited root at its private
        // circle. The root's own native components stay exactly as archived.
        foreach (var root in Occultist0099AbjurationCatalog.Roots)
        {
            var ability = Native<BlueprintAbility>(root.Guid);
            if (ability == null) continue;
            ability.ComponentsArray = ability.ComponentsArray
                .Concat(new BlueprintComponent[]
                {
                    new Occultist0099LearnGate
                    {
                        name = "$0099N$LearnGate", Circle = root.Circle
                    }
                }).ToArray();
            ability.OnEnable();
        }
    }

    private static Spellbook Book(UnitEntityData u)
    {
        var book = u.Descriptor.DemandSpellbook(occultistBook);
        Set(book, "m_BaseLevelInternal", u.Progression.GetClassLevel(occultistClass));
        return book;
    }

    private static Occultist0099FocusFact0099 Focus(UnitEntityData u)
        => Occultist0099NativeScope.FocusOf(u);
    private static Occultist0099SpellbookFact0099 BookFact(UnitEntityData u)
        => Occultist0099NativeScope.BookOf(u);

    private static void InFocus(UnitEntityData u, Action<Occultist0099FocusFact0099> body)
        => Occultist0099NativeScope.InFactContext(u, Occultist0099Ids.FocusFactGuid,
            typeof(Occultist0099FocusFact0099), c => body((Occultist0099FocusFact0099)c));
    private static void InBook(UnitEntityData u, Action<Occultist0099SpellbookFact0099> body)
        => Occultist0099NativeScope.InFactContext(u, Occultist0099Ids.SpellbookFactGuid,
            typeof(Occultist0099SpellbookFact0099), c => body((Occultist0099SpellbookFact0099)c));

    // The real rest entry point. Replenish is skipped exactly as the reviewed
    // 0081 host does; the RemoveOnRest loop and the IUnitRestHandler dispatch
    // are the genuine native code.
    private static void Rest(UnitEntityData u)
    {
        var applyRest = AccessTools.Method(typeof(RestController), "ApplyRest",
            new[] { typeof(UnitDescriptor) });
        var replenish = PatchProcessor.GetOriginalInstructions(applyRest)
            .Select(i => i.operand).OfType<MethodInfo>()
            .Single(m => m.Name == "Replenish");
        var prefix = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoReplenish));
        harmony.Patch(replenish, prefix: new HarmonyMethod(prefix));
        try { RestController.ApplyRest(u.Descriptor); }
        finally { harmony.Unpatch(replenish, prefix); }
    }

    private static void NextCamp()
        => Kingmaker.Game.Instance.Player.Camping.OverallCampsCount++;

    // ------------------------------------------------------------------
    // MF01 native: the real AbilityResourceLogic chain.
    // ------------------------------------------------------------------
    private static void MF01Native()
    {
        var u = Unit("mf01", occultistClass, 5, 14);   // M = +2, B = 7
        u.Facts.Add(new Feature(focusFact, u.Descriptor));
        var book = Book(u);
        var fact = Focus(u);
        Check(fact != null, "MF01N the real fact manager attaches the 0099 focus component");

        // The cycle was established by the native OnActivate through the real
        // component event context, reading the real ClassData and Int stat.
        int budget = 0, generic = 0;
        InFocus(u, f => { budget = f.AuthoritativeAvailable(); generic = budget; });
        Eq(7, budget, "MF01N native OnActivate builds B = max(0, L + M) = 7");

        InFocus(u, f =>
        {
            Occultist0099Focus.SetSchoolPresent(StateOf(f), "Abjuration", true);
            Occultist0099Focus.Invest(StateOf(f), "Abjuration", 4);
        });

        var logic = mindBarrier.GetComponent<Occultist0099FocusResourceLogic>();
        Check(logic != null,
            "MF01N the native GetComponent<AbilityResourceLogic> resolves the shipped 0099 subclass");
        Check(mindBarrier.GetComponent<AbilityResourceLogic>() is Occultist0099FocusResourceLogic,
            "MF01N the shipped cost component IS an AbilityResourceLogic, not a parallel gate");

        var ability = new AbilityData(mindBarrier, u.Descriptor);
        Check(ReferenceEquals(ability.ResourceLogic, logic),
            "MF01N AbilityData.ResourceLogic is the shipped 0099 component");

        // PRE-LOCK refusal. The UI precheck and the real spend both run; the
        // authoritative state, the native mirror and the effects must not move.
        string before = null;
        InFocus(u, f => before = f.StateFingerprint());
        Check(!logic.IsAbilityRestrictionPassed(ability),
            "MF01N native precheck refuses Mind Barrier before Full Round Lock");
        int mirrorBefore = u.Resources.GetResourceAmount(focusResource);
        AbilityCastRateUtils.SpendResources(ability);
        InFocus(u, f => Stable(before, f.StateFingerprint(),
            "MF01N a refused native spend changes no authoritative byte"));
        Eq(mirrorBefore, u.Resources.GetResourceAmount(focusResource),
            "MF01N a refused native spend charges no native resource");

        // LOCK, then the real spend succeeds and charges exactly one.
        InFocus(u, f => Eq(Occultist0099Outcome.Ok,
            Occultist0099Focus.Lock(StateOf(f)), "MF01N Full Round Lock"));
        var cast = new AbilityData(mindBarrier, u.Descriptor);
        Check(logic.IsAbilityRestrictionPassed(cast),
            "MF01N native precheck permits Mind Barrier after lock");
        int availableBefore = 0;
        InFocus(u, f => availableBefore = f.AuthoritativeAvailable());
        AbilityCastRateUtils.SpendResources(cast);
        int availableAfter = 0;
        InFocus(u, f => availableAfter = f.AuthoritativeAvailable());
        Eq(availableBefore - 1, availableAfter,
            "MF01N the real SpendResources charges exactly one Abjuration point");
        Eq(availableAfter, u.Resources.GetResourceAmount(focusResource),
            "MF01N the native resource mirror equals the authoritative available, no double debit");

        // Same execution id twice: the native pipeline may call Spend again for
        // one cast; the cycle-scoped dedup identity must refuse the second.
        AbilityCastRateUtils.SpendResources(cast);
        int availableRepeat = 0;
        InFocus(u, f => availableRepeat = f.AuthoritativeAvailable());
        Eq(availableAfter, availableRepeat,
            "MF01N a repeated spend of the SAME cast id charges nothing (RejectedDuplicate)");

        // A different cast is a different execution id and pays normally.
        var second = new AbilityData(mindBarrier, u.Descriptor);
        Check(second.UniqueId != cast.UniqueId,
            "MF01N the native AbilityData mints a distinct execution identity per cast");
        AbilityCastRateUtils.SpendResources(second);
        int availableSecond = 0;
        InFocus(u, f => availableSecond = f.AuthoritativeAvailable());
        Eq(availableRepeat - 1, availableSecond,
            "MF01N a genuinely new cast pays one more point");

        // Exhaust the pool, then prove the refusal is atomic.
        for (int i = 0; i < 2; i++)
            AbilityCastRateUtils.SpendResources(new AbilityData(mindBarrier, u.Descriptor));
        string exhausted = null;
        InFocus(u, f => exhausted = f.StateFingerprint());
        var denied = new AbilityData(mindBarrier, u.Descriptor);
        Check(!logic.IsAbilityRestrictionPassed(denied),
            "MF01N native precheck refuses when the school pool is exhausted");
        AbilityCastRateUtils.SpendResources(denied);
        InFocus(u, f => Stable(exhausted, f.StateFingerprint(),
            "MF01N an exhausted native spend changes no authoritative byte"));

        caseStatus["MF01"] = "native_ability_resource_logic_pass";
    }

    // The persisted component data of a native fact. Data is declared on the
    // generic EntityFactComponentDelegate<TEntity, TData> base, so the lookup
    // walks the base chain; it is only meaningful inside RequestEventContext,
    // which is exactly how InFocus / InBook call it.
    private static object ComponentData(object fact)
    {
        for (var type = fact.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty("Data",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);
            if (property != null) return property.GetValue(fact, null);
        }
        throw new InvalidOperationException("0099N: no component Data on " + fact.GetType());
    }

    // The authoritative state behind a native fact, read inside its context.
    private static Occultist0099FocusState StateOf(Occultist0099FocusFact0099 fact)
    {
        var data = (Occultist0099NativeData)ComponentData(fact);
        if (data.State == null) data.State = Occultist0099Focus.Create();
        return data.State;
    }

    private static Occultist0099SpellState SpellStateOf(Occultist0099SpellbookFact0099 fact)
    {
        var data = (Occultist0099SpellbookData)ComponentData(fact);
        if (data.State == null) data.State = Occultist0099Spells.Create(0, 10);
        return data.State;
    }

    // ------------------------------------------------------------------
    // SP01 / SP02 native: SelectSpell.Check / Apply and Spellbook.AddKnown.
    // ------------------------------------------------------------------
    private static LevelUpState ChoiceState(Spellbook book, int circle, int slots)
    {
        var state = Empty<LevelUpState>();
        Set(state, "m_Unit", book.Owner.Unit);
        Set(state, "Selections", new List<FeatureSelectionState>());
        Set(state, "SpellSelections", new List<SpellSelectionData>());
        Set(state, "SelectedClass", occultistClass);
        var selection = state.DemandSpellSelection(book.Blueprint, book.Blueprint.SpellList);
        for (int i = 0; i < 11; i++) selection.SetLevelSpells(i, i == circle ? slots : 0);
        return state;
    }

    private static bool NativeCheck(SelectSpell action, LevelUpState state, UnitDescriptor owner)
        => (bool)AccessTools.Method(typeof(SelectSpell), nameof(SelectSpell.Check))
            .Invoke(action, new object[] { state, owner });

    private static void SP01Native()
    {
        var u = Unit("sp01", occultistClass, 1, 16);
        u.Facts.Add(new Feature(spellbookFact, u.Descriptor));
        var book = Book(u);
        Check(BookFact(u) != null,
            "SP01N the real fact manager attaches the 0099 spellbook component");

        // The native OnActivate read the real ClassData.Level (1) and Int (16).
        InBook(u, f =>
        {
            var s = SpellStateOf(f);
            Eq(1, s.ClassLevel, "SP01N native OnActivate reads the real class level");
            Eq(16, s.Intelligence, "SP01N native OnActivate reads the real Intelligence stat");
            Eq(Occultist0099Outcome.Ok,
                Occultist0099Spells.AddSlot(s, "slot-1", "Abjuration"),
                "SP01N first Abjuration implement opens its quotas");
        });

        var mageShield = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.MageShield);
        Check(mageShield != null, "SP01N the audited MageShield root is loaded from the game archive");
        Check(mageShield.GetComponent<SpellComponent>()?.School == SpellSchool.Abjuration,
            "SP01N the archived native SpellComponent really is Abjuration");
        var gate = Occultist0099LearnGate.For(mageShield);
        Check(gate != null && gate.Circle == 1,
            "SP01N the shipped learning gate is attached at the private circle 1");

        // POSITIVE: the native SelectSpell.Check permits the eligible choice and
        // Apply really spends the slot and really adds the known spell.
        var state = ChoiceState(book, 1, 1);
        var choice = new SelectSpell(occultistBook, occultistList, 1, mageShield, 0);
        Check(NativeCheck(choice, state, u.Descriptor),
            "SP01N native SelectSpell.Check permits an eligible private choice");
        choice.Apply(state, u.Descriptor);
        Check(book.IsKnown(mageShield),
            "SP01N native SelectSpell.Apply really adds the root to the private book");
        Check(!state.GetSpellSelection(occultistBook, occultistList).CanSpendSlot(1, 0),
            "SP01N the native selection slot is really consumed");
        InBook(u, f =>
        {
            var s = SpellStateOf(f);
            Eq(1, Occultist0099Spells.KnownRoots(s).Length,
                "SP01N the authoritative quota ledger records exactly one learned root");
            Eq(Occultist0099AbjurationCatalog.MageShield,
                s.Quotas[Occultist0099Spells.QuotaKey("slot-1", 1)],
                "SP01N the quota holds the canonical MageShield identity");
        });

        // NEGATIVE: the same root cannot occupy a second slot in the same book.
        var second = ChoiceState(book, 1, 1);
        Check(!NativeCheck(new SelectSpell(occultistBook, occultistList, 1, mageShield, 0),
                second, u.Descriptor),
            "SP01N native Check refuses an already known root in a fresh slot");

        // NEGATIVE: a circle the class level has not opened yet.
        var resist = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.ResistEnergy);
        var early = ChoiceState(book, 2, 1);
        Check(!NativeCheck(new SelectSpell(occultistBook, occultistList, 2, resist, 0),
                early, u.Descriptor),
            "SP01N native Check refuses circle 2 at class level 1");

        // NEGATIVE: the commit path is an independent refusal point. Calling
        // AddKnown directly (bypassing Check) must not learn anything.
        string before = null;
        InBook(u, f => before = f.StateFingerprint());
        int knownBefore = book.GetKnownSpells(2).Count;
        var refused = book.AddKnown(2, resist);
        Check(refused == null,
            "SP01N the shipped AddKnown prefix refuses an ineligible circle even without Check");
        Eq(knownBefore, book.GetKnownSpells(2).Count,
            "SP01N a refused AddKnown leaves the native known-spell list untouched");
        InBook(u, f => Stable(before, f.StateFingerprint(),
            "SP01N a refused AddKnown changes no authoritative byte"));

        // NEGATIVE: a foreign book that shares nothing with the private book
        // must never be gated by, nor leak into, the 0099 ledger.
        var foreignBook = new BlueprintSpellbook
        {
            Spontaneous = false,
            CastingAttribute = StatType.Intelligence,
            ComponentsArray = Array.Empty<BlueprintComponent>()
        };
        Set(foreignBook, "m_CharacterClass",
            Ref<BlueprintCharacterClassReference>(occultistClass));
        Set(foreignBook, "m_SpellList", Ref<BlueprintSpellListReference>(occultistList));
        Set(foreignBook, "m_SpellsPerDay", Get(occultistBook, "m_SpellsPerDay"));
        Stage(foreignBook, Guid.NewGuid().ToString("N"), "0099N.ForeignBook");
        var foreign = u.Descriptor.DemandSpellbook(foreignBook);
        Set(foreign, "m_BaseLevelInternal", 1);
        string beforeForeign = null;
        InBook(u, f => beforeForeign = f.StateFingerprint());
        var foreignChoice = new SelectSpell(foreignBook, occultistList, 1, resist, 0);
        var foreignState = ChoiceState(foreign, 1, 1);
        // The gate only governs the PRIVATE book; a foreign book keeps native
        // behaviour, and the 0099 ledger must not move either way.
        foreignChoice.Apply(foreignState, u.Descriptor);
        InBook(u, f => Stable(beforeForeign, f.StateFingerprint(),
            "SP01N a foreign spellbook never writes the 0099 quota ledger"));

        caseStatus["SP01"] = "native_select_spell_pass";
    }

    private static void SP02Native()
    {
        // Int gate through the real stat and the real learning chain.
        var u = Unit("sp02", occultistClass, 4, 11);   // circle 2 open by level, Int 11
        u.Facts.Add(new Feature(spellbookFact, u.Descriptor));
        var book = Book(u);
        InBook(u, f =>
        {
            var s = SpellStateOf(f);
            Occultist0099Spells.AddSlot(s, "slot-1", "Abjuration");
            Eq(1, Occultist0099Spells.MaxCircle(s),
                "SP02N Int 11 caps the open circle at 1 even at class level 4");
        });
        var resist = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.ResistEnergy);
        Check(!NativeCheck(new SelectSpell(occultistBook, occultistList, 2, resist, 0),
                ChoiceState(book, 2, 1), u.Descriptor),
            "SP02N native Check refuses circle 2 while the real Int stat is 11");
        Check(book.AddKnown(2, resist) == null,
            "SP02N native AddKnown refuses circle 2 while Int is 11");

        // Raise the REAL stat and re-run the real rest entry point.
        SetInt(u, 16);
        NextCamp();
        Rest(u);
        InBook(u, f =>
        {
            var s = SpellStateOf(f);
            Eq(16, s.Intelligence,
                "SP02N the real rest callback resynchronises Int from the native stat");
            Eq(2, Occultist0099Spells.MaxCircle(s),
                "SP02N circle 2 opens once the real Int stat reaches 12+");
        });
        Check(NativeCheck(new SelectSpell(occultistBook, occultistList, 2, resist, 0),
                ChoiceState(book, 2, 1), u.Descriptor),
            "SP02N native Check permits circle 2 after the real Int raise");

        // Lower Int again: an established quota survives, new learning does not.
        var state = ChoiceState(book, 2, 1);
        new SelectSpell(occultistBook, occultistList, 2, resist, 0).Apply(state, u.Descriptor);
        Check(book.IsKnown(resist), "SP02N circle 2 root really learned at Int 16");
        SetInt(u, 11);
        NextCamp();
        Rest(u);
        InBook(u, f =>
        {
            var s = SpellStateOf(f);
            Check(Occultist0099Spells.IsKnown(s, Occultist0099AbjurationCatalog.ResistEnergy),
                "SP02N a temporary Int drop keeps the established quota and source");
            Check(!Occultist0099Spells.CanCast(s, Occultist0099AbjurationCatalog.ResistEnergy),
                "SP02N a temporary Int drop blocks casting that circle");
        });
        var dispel = Native<BlueprintAbility>("92681f181b507b34ea87018e8f7a528a");
        Check(dispel != null && book.AddKnown(2, dispel) == null,
            "SP02N a second circle-2 root cannot be learned while Int is 11");

        caseStatus["SP02"] = "native_int_gate_pass";
    }

    // ------------------------------------------------------------------
    // Rest identity: the real camp serial, idempotent across repeated callbacks.
    // ------------------------------------------------------------------
    private static void RestNative()
    {
        var u = Unit("rest", occultistClass, 5, 14);
        u.Facts.Add(new Feature(focusFact, u.Descriptor));
        Book(u);
        InFocus(u, f =>
        {
            Occultist0099Focus.SetSchoolPresent(StateOf(f), "Abjuration", true);
            Occultist0099Focus.Invest(StateOf(f), "Abjuration", 4);
            Occultist0099Focus.Lock(StateOf(f));
        });
        AbilityCastRateUtils.SpendResources(new AbilityData(mindBarrier, u.Descriptor));
        int spentAvailable = 0;
        InFocus(u, f => spentAvailable = f.AuthoritativeAvailable());

        // A NEW camp serial really refills.
        NextCamp();
        Rest(u);
        int refilled = 0;
        InFocus(u, f => refilled = f.AuthoritativeAvailable());
        Eq(7, refilled, "RESTN a new camp serial really rebuilds B and clears the spend");
        Check(refilled > spentAvailable, "RESTN the new cycle actually restores focus");
        InFocus(u, f => Check(!f.IsLocked(),
            "RESTN the new cycle starts unlocked, so MF01 still gates payment"));

        // The refreshed cycle must be invested and locked again before any
        // focus power can be paid; that is the frozen MF01 rule, not a fixture
        // shortcut.
        InFocus(u, f =>
        {
            Occultist0099Focus.SetSchoolPresent(StateOf(f), "Abjuration", true);
            Occultist0099Focus.Invest(StateOf(f), "Abjuration", 4);
            Occultist0099Focus.Lock(StateOf(f));
        });

        // Repeated callbacks of the SAME camp serial are idempotent.
        AbilityCastRateUtils.SpendResources(new AbilityData(mindBarrier, u.Descriptor));
        int afterSpend = 0;
        InFocus(u, f => afterSpend = f.AuthoritativeAvailable());
        Eq(6, afterSpend, "RESTN the relocked new cycle really pays one point");
        Rest(u);
        Rest(u);
        int afterRepeat = 0;
        InFocus(u, f => afterRepeat = f.AuthoritativeAvailable());
        Eq(afterSpend, afterRepeat,
            "RESTN repeated rest callbacks of one camp serial never refill again");

        // The dedup identity survives the repeat: the same execution id is still
        // refused, a new one still pays.
        var sameId = new AbilityData(mindBarrier, u.Descriptor);
        AbilityCastRateUtils.SpendResources(sameId);
        int afterNew = 0;
        InFocus(u, f => afterNew = f.AuthoritativeAvailable());
        Eq(afterRepeat - 1, afterNew, "RESTN a new cast after the repeated rest still pays");
        AbilityCastRateUtils.SpendResources(sameId);
        int afterDup = 0;
        InFocus(u, f => afterDup = f.AuthoritativeAvailable());
        Eq(afterNew, afterDup, "RESTN the same cast id is still refused after the repeated rest");

        caseStatus["MF07"] = "native_rest_camp_identity_pass";
    }

    // ------------------------------------------------------------------
    // Registration gate: the slice must stay unpublished.
    // ------------------------------------------------------------------
    private static void GateNative()
    {
        Check(!Occultist0099Ids.SlicePublished,
            "GATE the B slice remains unpublished");
        Check(!Occultist0099Slice.CanRegister(true),
            "GATE CanRegister refuses even with a complete map while unpublished");
        Check(!Occultist0099Slice.CanRegister(false),
            "GATE CanRegister refuses an incomplete map");
        // The private TypeIds must not collide with any native component.
        var nativeIds = new HashSet<Guid>(typeof(BlueprintsCache).Assembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes(typeof(TypeIdAttribute), false)
                .Cast<TypeIdAttribute>()).Select(a => a.Guid));
        foreach (var id in new[]
        {
            Occultist0099Ids.FocusFactTypeId, Occultist0099Ids.SpellbookMarkerTypeId,
            Occultist0099Ids.FocusResourceLogicTypeId, Occultist0099Ids.LearnGateTypeId,
            Occultist0099Ids.SpellbookFactTypeId
        })
            Check(!nativeIds.Contains(Guid.Parse(id)),
                "GATE private TypeId " + id + " has no native collision");
        caseStatus["SP08"] = "native_gate_only_registration_not_run";
    }

    // ------------------------------------------------------------------
    public static void Run(string[] args)
    {
        string managed = args[0], candidate = args[1], reportPath = args[2];
        string assetsPath = args[3], nativePath = args[4];
        Console.WriteLine("0099N HOST: candidate=" + candidate);
        nativeRows = JObject.Parse(File.ReadAllText(nativePath));

        // The frozen A evidence is the authority for the donor identities and
        // the circle-opening levels; the host proves the shipped catalogue
        // agrees with it instead of restating the numbers.
        var slots = JObject.Parse(File.ReadAllText(assetsPath))["dailySpellSlots"];
        string donorBook = (string)slots["spellbookDonor"];
        string donorTable = (string)slots["tableDonor"];
        Check(donorBook == "027d37761f3804042afa96fe3e9086cc" &&
              donorTable == "bf4a0a03a45275d438b8c59cd5388259",
            "HOST the frozen A evidence still names the audited Alchemist donors");
        for (int circle = 1; circle <= 6; circle++)
            Eq((int)slots["levelForSpellCircle"][circle.ToString()],
                Occultist0099AbjurationCatalog.ClassLevelForCircle(circle),
                "HOST the shipped circle " + circle + " opening level matches the A evidence");

        harmony = new Harmony("cotw.b.0099.native");
        BindTypeIds();
        InstallBoundaries();
        int bridges = InstallProductionBridges();
        Check(bridges >= 2,
            "HOST the shipped 0099 native Harmony bridges are installed (" + bridges + ")");

        cache = new BlueprintsCache();
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        HostGame();

        // Load only the audited rows this directed check needs.
        var roots = Occultist0099AbjurationCatalog.Roots.Select(r => r.Guid)
            .Concat(new[] { donorTable, donorBook })
            .ToArray();
        LoadRows(roots);
        // Fixture boundary, disclosed exactly as the reviewed 0060 host does.
        // The audited archive carries the Wrath-source roots. The four CotW
        // roots are this project's own production output (PsychicWardingSpells /
        // PsychicGlobeSpells / PsychicSupportSpells) and the one External root
        // belongs to TabletopTweaks-Base, which is not installed here. Those
        // five are host-staged at their frozen A identities with a real
        // SpellComponent and a real executable effect path, and counted.
        int loadedRoots = 0, hostStaged = 0;
        var stagedRoots = new List<string>();
        foreach (var root in Occultist0099AbjurationCatalog.Roots)
        {
            if (Native<BlueprintAbility>(root.Guid) != null) { loadedRoots++; continue; }
            StageAbsentRoot(root);
            hostStaged++;
            stagedRoots.Add(root.Name + "(" + root.Source + ")");
        }
        Eq(Occultist0099AbjurationCatalog.Roots.Length, loadedRoots + hostStaged,
            "HOST every audited Abjuration root is present at its frozen identity");
        Eq(13, loadedRoots,
            "HOST exactly the 13 Wrath-source roots come from the audited game archive");
        Eq(5, hostStaged,
            "HOST exactly the 4 CotW and 1 External roots are host-staged, not archived");
        Console.WriteLine("0099N FIXTURE: archive=" + loadedRoots +
                          " host-staged=" + hostStaged + " [" +
                          string.Join(", ", stagedRoots) + "]");
        Check(Native<BlueprintSpellsTable>(donorTable)?.Levels?.Length > 0,
            "HOST the audited Alchemist six-circle donor table is loaded with real rows");

        StagePrivateSlice();

        MF01Native();
        SP01Native();
        SP02Native();
        RestNative();
        GateNative();

        // Everything else in the frozen contract is honestly NOT run here.
        foreach (string id in new[]
        {
            "MF02", "MF03", "MF04", "MF05", "MF06", "MF08", "MF09", "MF10", "MF11",
            "SP03", "SP04", "SP05", "SP06", "SP07"
        })
            caseStatus[id] = "not_run_native";

        var report = new JObject
        {
            ["workPackage"] = "COTW-0099",
            ["contract"] = Occultist0099Ids.Contract,
            ["stage"] = "B directed native check (D-0099-01 closure)",
            ["acceptanceScope"] = "native_entry_points_directed",
            ["playerGameAcceptance"] = false,
            ["gameLaunched"] = false,
            ["ummLaunched"] = false,
            ["slicePublished"] = Occultist0099Ids.SlicePublished,
            ["releaseEligible"] = false,
            ["productionBridgesInstalled"] = bridges,
            ["nativeRootsLoaded"] = loadedRoots,
            ["hostStagedRoots"] = hostStaged,
            ["hostStagedRootNames"] = new JArray(stagedRoots),
            ["skippedArchiveComponents"] = skippedComponents,
            ["rootFixtureBoundary"] = "13 of the 18 audited Abjuration roots come from the reviewed Wrath archive fixture. The 4 CotW roots are built by this project's own production builders (PsychicWardingSpells / PsychicGlobeSpells / PsychicSupportSpells.Cloak) against real archived native donors; only PsychicOriginalIcons.Load is substituted because ImageConversion is a Unity native call. The 1 External root (CloakOfWindsAbility, TabletopTweaks-Base) is host-staged at its frozen A identity with a real Abjuration SpellComponent and a real executable effect path; the real provider assembly is NOT loaded, so its provider gate is exercised only through the authoritative state, never through a real third-party blueprint.",
            ["checks"] = checks,
            ["failures"] = new JArray(failures),
            ["hostLoggedExceptions"] = hostLogs,
            ["caseStatus"] = JObject.FromObject(caseStatus),
            ["result"] = failures.Count == 0 ? "NATIVE_DIRECTED_PASS" : "NATIVE_DIRECTED_FAIL",
            ["resultMeaning"] = failures.Count == 0
                ? "The real Wrath AbilityResourceLogic payment chain, the real SelectSpell/AddKnown learning chain and the real ApplyRest camp identity executed against the authoritative 0099 state. This is NOT player-game acceptance and NOT a release sign-off."
                : "At least one native directed assertion failed; see failures.",
            ["boundary"] = "Real: BlueprintsCache and the native polymorphic component deserializer, EntityFactsManager/Feature/UnitFactComponentDelegate activation and event context, AbilityData construction and UniqueId, AbilityCastRateUtils.SpendResources, AbilityResourceLogic subclass dispatch, UnitAbilityResourceCollection, SelectSpell.Check/Apply, SpellSelectionData slot accounting, Spellbook.AddKnown/IsKnown, RestController.ApplyRest RemoveOnRest loop and IUnitRestHandler dispatch, CampingState.OverallCampsCount. Boundaries: constructed Game/Player/unit proxies, no-View scene services, a fixed TimeController clock, LogChannel capture, and UnitDescriptor.Replenish skipped inside ApplyRest (it restores every unrelated descriptor resource). NOT run: Unity rendering, the level-up UI/VM layer, the whole rest coroutine, save/load transport, real third-party provider assemblies, the shared registration chain and the full historical suites.",
            ["notRun"] = new JArray(
                "game/UMM startup", "player gameplay acceptance",
                "full historical suites", "shared class/spellbook/progression registration",
                "level-up UI and respec controller", "save/load transport",
                "real TabletopTweaks / other provider assembly load",
                "packaging, version bump, upload and issue closure")
        };
        File.WriteAllText(reportPath, report.ToString(Newtonsoft.Json.Formatting.Indented));

        Console.WriteLine("0099N RESULT: " + report["result"] + ": " + checks);
        Console.WriteLine("0099N NOT RUN: " + string.Join(", ",
            caseStatus.Where(p => p.Value == "not_run_native").Select(p => p.Key)));
        if (failures.Count > 0)
            throw new Exception("0099N native directed check failed: " + failures.Count +
                                " of " + checks);
        Console.WriteLine("PASS: 0099 native directed check");
    }
}
