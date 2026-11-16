// COTW-0099 directed managed host for the unpublished Abjuration slice.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Rest;
using Kingmaker.Controllers.Rest.State;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.DLC;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Inspect;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Settings;
using Kingmaker.UI.MVVM._VM.CharGen;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Spells;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Occultist0099NativeLauncher
{
    private static int Main(string[] args)
    {
        if (args.Length != 5 || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_AEGIS_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_DIVINATION_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_ILLUSION_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_TRANSMUTATION_FIXTURE")))
        {
            Console.Error.WriteLine("Set the Aegis, Divination, Illusion and Transmutation fixtures using ReadOccultist0099Archive.ps1.");
            return 2;
        }
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
    private static int registeredPrivateIdentities;
    private static int armorArchiveRows;
    private static EntityService armorFixtureEntities;
    private static int castDispatches;
    private static int pointsAtDispatch;
    private static Action<RuleApplySpell> beforeDispatch;
    private static Action afterBatchRest;
    private static bool applyNativeEffects;
    private static bool skipFortuneDelivery;
    private static BlueprintBuff rejectedAegisWard;
    private static BlueprintItemEnchantment failedAegisEnchantment;
    private static bool aegisAttachmentInterrupted;
    private static UnitProgressionData respecXpOwner;
    private static BlueprintStatProgression respecXpTable;
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
    private static void NoHitVisual(RuleDealDamage rule) { }
    private static int NeutralDamage(RuleDealDamage rule, int amount) => amount;
    private static IEnumerable<CodeInstruction> DamageFixture(IEnumerable<CodeInstruction> instructions)
    {
        var difficulty = AccessTools.Method(typeof(RuleDealDamage), "ApplyDifficultyModifiers");
        var visual = AccessTools.Method(typeof(Kingmaker.Visual.HitSystem.HitPlayer), "PlayDamageHit");
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(difficulty) || instruction.Calls(visual))
            {
                string boundary = instruction.Calls(difficulty) ? nameof(NeutralDamage) : nameof(NoHitVisual);
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(Occultist0099NativeTests), boundary);
            }
            yield return instruction;
        }
    }
    private static bool NoUnityProfile(ref IDisposable __result) { __result = null; return false; }
    private static bool ProvideRespecExperience(UnitProgressionData __instance, ref BlueprintStatProgression __result)
    {
        if (__instance != respecXpOwner) return true;
        __result = respecXpTable;
        return false;
    }
    private static bool NoNpcDisposition(ref bool __result) { __result = false; return false; }
    private static bool NeutralDifficulty(ref DifficultySettings __result)
    {
        __result = Empty<DifficultySettings>();
        foreach (var name in new[] { "StatsAdjustments", "EnemyCriticalHits" })
        {
            var field = AccessTools.Field(typeof(DifficultySettings), name);
            var setting = FormatterServices.GetUninitializedObject(field.FieldType);
            var valueType = field.FieldType.BaseType.GetGenericArguments()[0];
            AccessTools.Field(field.FieldType.BaseType, "m_CurrentValue").SetValue(setting, Enum.ToObject(valueType, 0));
            AccessTools.Field(field.FieldType.BaseType, "m_CurrentValueIsSet").SetValue(setting, true);
            field.SetValue(__result, setting);
        }
        return false;
    }

    private static bool CastContextBoundary(AbilityData __instance, TargetWrapper __0,
        AbilityParams __1, ref AbilityExecutionContext __result)
    {
        if (!applyNativeEffects)
        {
            __result = Empty<AbilityExecutionContext>();
            return false;
        }
        bool recalculate = __instance.ResourceLogic is Occultist0099FocusResourceLogic &&
            __instance.Blueprint.GetComponent<ContextCalculateAbilityParams>() != null;
        __result = new AbilityExecutionContext(__instance, __1 ?? __instance.Fact?.OverrideParams ??
            (recalculate ? __instance.CalculateParams() : AbilityParams.Empty), __0);
        if (recalculate) __result.Recalculate();
        return false;
    }

    private static bool LegalTargetBoundary(ref bool __result)
    {
        __result = true;
        return false;
    }

    private static bool EffectDispatchBoundary(AbilityData __instance, AbilityExecutionContext __0)
    {
        castDispatches++;
        var unit = __instance.Caster.Unit;
        pointsAtDispatch = unit.Get<Occultist0099FocusPart>()?.State.Available ?? -1;
        if (applyNativeEffects && !(skipFortuneDelivery &&
            __instance.Blueprint.AssetGuid.ToString() == Occultist0099Ids.Id("DivinersFortuneDelivery")))
            foreach (var effect in __instance.Blueprint.GetComponents<AbilityApplyEffect>())
                effect.Apply(__0, __0.MainTarget);
        // The headless host dispatches the managed effects without animation.
        return false;
    }

    private static bool BatchHealBoundary(UnitEntityData unit)
    {
        Rest(unit);
        afterBatchRest?.Invoke();
        return false;
    }

    private static void BeforeApplySpell(RuleApplySpell __instance)
    {
        beforeDispatch?.Invoke(__instance);
    }
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
        harmony.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope), "New",
                new[] { typeof(string), typeof(UnityEngine.Object) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoUnityProfile))));
        harmony.Patch(AccessTools.Method(typeof(RuleDealDamage), nameof(RuleDealDamage.OnTrigger)),
            transpiler: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(DamageFixture))));
        harmony.Patch(AccessTools.PropertyGetter(typeof(SettingsRoot), "Difficulty"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NeutralDifficulty))));
        harmony.Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), "IsAttackingGreenNPC"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoNpcDisposition))));
        harmony.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoNpcDisposition))));
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
        harmony.Patch(AccessTools.Method(typeof(AbilityData), nameof(AbilityData.CreateExecutionContext)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(CastContextBoundary))));
        harmony.Patch(AccessTools.Method(typeof(AbilityData), nameof(AbilityData.CanTarget), new[] { typeof(TargetWrapper) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(LegalTargetBoundary))));
        harmony.Patch(AccessTools.Method(typeof(AbilityData), nameof(AbilityData.Cast)),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(EffectDispatchBoundary))));
        harmony.Patch(AccessTools.Method(typeof(RuleApplySpell), nameof(RuleApplySpell.OnTrigger)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(BeforeApplySpell))));
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
            .Where(t => Occultist0099Slice.IsRuntimePatch(t) &&
                        t.GetCustomAttributes(typeof(HarmonyPatch), false).Any())
            .ToArray();
        var normalStartup = (IEnumerable<Type>)AccessTools.Method(Production("ModEntry"), "PatchTypes")
            .Invoke(null, null);
        Check(normalStartup.Any() && !normalStartup.Any(Occultist0099Slice.IsRuntimePatch),
            "HOST ordinary startup discovers existing patches without installing the unpublished slice");
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
        // Keep one native camping state to prove distinct rests can share its counter.
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
        Set(u, "PreventDirectControl", new CountableFlag());
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
        foreach (var stat in new ModifiableValue[]
        {
            new ModifiableValueTemporaryHitPoints(stats), new ModifiableValueHitPoints(stats),
            new ModifiableValue(stats, StatType.SneakAttack),
            new ModifiableValueSavingThrow(stats, stats.Constitution, StatType.SaveFortitude),
            new ModifiableValueSavingThrow(stats, stats.Dexterity, StatType.SaveReflex),
            new ModifiableValueSavingThrow(stats, stats.Wisdom, StatType.SaveWill)
        })
        {
            Set(stats, stat.Type.ToString(), stat);
            all.Add(stat);
        }

        var state = new UnitState(descriptor);
        Set(descriptor, "State", state);

        var progression = Empty<UnitProgressionData>();
        Set(descriptor, "Progression", progression);
        Set(progression, "Owner", descriptor);
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

    private static SimpleBlueprint Materialise(JObject row, string blueprintName)
    {
        var data = (JObject)row["Data"];
        string typeName = ((string)data["$type"]).Split(',').Last().Trim();
        var type = typeof(BlueprintsCache).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == typeName &&
                                 typeof(BlueprintScriptableObject).IsAssignableFrom(t));
        if (type == null) return null;
        var bp = (BlueprintScriptableObject)Activator.CreateInstance(type);
        bp.AssetGuid = P((string)row["AssetId"]);
        bp.name = blueprintName;
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
                                      "SpellsPerLevel", "CantripsType", "IsMythic", "HitDie", "SkillPoints", "CanTargetPoint", "CustomRange" })
        {
            var info = AccessTools.Field(type, field);
            var value = data[field];
            if (info == null || value == null || value.Type == JTokenType.Null) continue;
            try { info.SetValue(bp, value.ToObject(info.FieldType)); } catch { }
        }
        foreach (var field in new[] { "m_Parent", "m_SpellList", "m_CharacterClass", "m_SpellsPerDay",
                                      "m_Type", "m_Enchantments", "m_DamageType", "m_AttackType",
                                      "m_SpellsKnown", "m_SpellSlots", "m_BaseAttackBonus", "m_FortitudeSave", "m_WillSave" })
        {
            var info = AccessTools.Field(type, field);
            var value = data[field];
            if (info == null || value == null || value.Type == JTokenType.Null) continue;
            info.SetValue(bp, value.ToObject(info.FieldType, Json.Serializer));
        }
        if (bp is Kingmaker.Blueprints.Facts.BlueprintUnitFact)
            Set(bp, "m_Icon", Empty<UnityEngine.Sprite>());
        if (bp is BlueprintSpellsTable table && data["Levels"] != null)
            table.Levels = data["Levels"].ToObject<SpellsLevelEntry[]>();
        if (bp is BlueprintSpellList list && data["SpellsByLevel"] != null)
            list.SpellsByLevel = data["SpellsByLevel"].ToObject<SpellLevelList[]>(Json.Serializer);
        bp.OnEnable();
        cache.AddCachedBlueprint(bp.AssetGuid, bp);
        return bp;
    }

    private static T Native<T>(string guid) where T : SimpleBlueprint
        => cache.Load(P(guid)) as T;

    private static int LoadArchiveDonors(string path)
    {
        int count = 0;
        foreach (var row in JArray.Parse(File.ReadAllText(path)))
        {
            var name = (string)row["name"];
            if (Materialise((JObject)row["blueprint"], name) == null)
                throw new InvalidDataException("Archive fixture could not load " + name);
            count++;
        }
        return count;
    }

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

    private static void StageTransmutationRoots(IEnumerable<Occultist0099SpellRoot> published)
    {
        LoadRows("c4a7f98d743bc784c9d4cf2105852c39", "c927a8b0cd3f5174f8c0b67cdbfde539", "f3c0b267dd17a2a45a40805e31fe3cd1",
            "c0f4e1c24c9cd334ca988ed1bd9d201f", "6cbb732b9d638724a960d784634dcdcf", "e252b26686ab66241afdf33f2adaead6",
            "e84fc922ccf952943b5240293669b171");
        var factory = Activator.CreateInstance(Production("PsychicAssets"), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { cache }, null);
        foreach (var (type, method) in new[] {
            ("PsychicGhostbaneDirge", "Build"), ("PsychicUtilitySpells", "CountlessEyes"), ("PsychicBattleSpells", "Arrows"),
            ("PsychicTravelSpells", "BuildAirTravel"), ("PsychicWeaponSpells", "BuildAnchor"), ("PsychicBoundWeapons", "Blade"),
            ("PsychicCataclysmSpells", "Control") })
            AccessTools.Method(Production(type), method).Invoke(null, new[] { factory });
        var assets = (System.Collections.IDictionary)Get(factory, "Assets");
        foreach (var asset in assets.Values.Cast<BlueprintScriptableObject>())
        {
            asset.OnEnable();
            cache.AddCachedBlueprint(asset.AssetGuid, asset);
        }
        foreach (var row in published)
            Check(Native<BlueprintAbility>(row.Guid)?.GetComponent<SpellComponent>()?.School.ToString() == row.School,
                "HOST the existing CotW builder publishes the Transmutation root " + row.Name);
    }

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
            else if (root.Name == "CotWWrathPsychicDazeMass")
            {
                LoadRows("9934fedff1b14994ea90205d189c8759", "02707231be1d3a74ba7e38a426c8df37", "a95311b3dc996964cbaa30ff9965aaf6",
                    "625827490ea69d84d8e599a33929fdc6", "57614b50e8d86b24395931fffc5e409b", "09478937695300944a179530664e42ec",
                    "3bec99efd9a363242a6c8d9957b75e91", "fd389783027d63343b4a5634bd81645f", "455ac88e22f55804ab87c2467deff1d6",
                    "018af8005220ac94a9a4f47b3e9c2b4e", "9054d3988d491d944ac144e27b6bc318", "706e61781d692a042b35941f14bc41c5");
                var entries = (System.Collections.IEnumerable)AccessTools.Method(Production("PsychicWardingSpells"), "Build")
                    .Invoke(null, new[] { factory });
                built = entries.Cast<object>().Select(entry => (BlueprintAbility)Get(entry, "Ability")).Single(ability => ability.AssetGuid == P(root.Guid));
            }
            else if (root.Name == "CotWWrathPsychicLocateWeakness")
            {
                var entries = (System.Collections.IEnumerable)AccessTools.Method(Production("PsychicInsightSpells"), "Build")
                    .Invoke(null, new[] { factory });
                built = entries.Cast<object>().Select(entry => (BlueprintAbility)Get(entry, "Ability"))
                    .Single(ability => ability.AssetGuid == P(root.Guid));
            }
            else if (root.Name == "CotWWrathPsychicBattlemindLink")
            {
                LoadRows("e84fc922ccf952943b5240293669b171", "47808d23c67033d4bbab86a1070fd62f",
                    "6e81a6679a0889a429dec9cedcf3729c", "0d657aa811b310e4bbd8586e60156a2d");
                AccessTools.Method(Production("PsychicLinkedWard"), "Build").Invoke(null, new[] { factory });
                built = (BlueprintAbility)((System.Collections.IDictionary)Get(factory, "Assets"))["BattlemindLink"];
            }
            else if (root.Name == "CotWWrathPsychicArcaneEye")
            {
                LoadRows("5abc8884c6f15204c8604cb01a2efbab", "41cf93453b027b94886901dbfc680cb9");
                AccessTools.Method(Production("PsychicSpiritWeapons"), "BuildArcaneEye").Invoke(null, new[] { factory });
                var assets = (System.Collections.IDictionary)Get(factory, "Assets");
                built = (BlueprintAbility)assets["ArcaneEye"];
                foreach (var asset in assets.Values.Cast<BlueprintScriptableObject>().Where(asset => asset != built))
                {
                    asset.OnEnable();
                    cache.AddCachedBlueprint(asset.AssetGuid, asset);
                }
            }
            Check(built != null && built.AssetGuid.ToString() == root.Guid,
                "HOST the real CotW builder publishes " + root.Name +
                " at its frozen A identity");
            if (built == null) return;
            built.OnEnable();
            cache.AddCachedBlueprint(built.AssetGuid, built);
            Check(built.GetComponent<SpellComponent>()?.School.ToString() == root.School,
                "HOST the production " + root.Name + " really is " + root.School);
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
            Materialise(bp, (string)row["name"]);
        }
    }

    // ------------------------------------------------------------------
    // Private 0099 blueprints, staged for this directed check only.
    // ------------------------------------------------------------------
    private static BlueprintCharacterClass occultistClass;
    private static BlueprintSpellbook occultistBook;
    private static BlueprintSpellList occultistList;
    private static BlueprintSpellList firstImplementList;
    private static BlueprintSpellList extraImplementList;
    private static BlueprintFeature focusFact;
    private static BlueprintFeature spellbookFact;
    private static BlueprintAbilityResource focusResource;
    private static BlueprintAbility mindBarrier;
    private static BlueprintFeature abjurationImplement;
    private static BlueprintBuff mindBarrierBuff, wardingTalisman;

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
        firstImplementList = Stage(new BlueprintSpellList { SpellsByLevel = rows.ToArray() },
            Occultist0099Ids.Id("slot-1-Abjuration-LearnList"), "0099N.AbjurationFirstList");
        extraImplementList = Stage(new BlueprintSpellList { SpellsByLevel = rows.ToArray() },
            Occultist0099Ids.Id("slot-2-Abjuration-ExtraLearnList"), "0099N.AbjurationExtraList");

        // The private book uses the audited Alchemist six-circle donor table for
        // its per-day counts (A dailySpellSlots.tableDonor) but its OWN identity,
        // list and class. The donor book itself is never modified.
        var donorTable = Native<BlueprintSpellsTable>("bf4a0a03a45275d438b8c59cd5388259");
        var zeroKnown = Stage(new BlueprintSpellsTable
        {
            Levels = Enumerable.Range(0, 21).Select(_ => new SpellsLevelEntry { Count = new int[11] }).ToArray()
        }, Occultist0099Ids.Id("ZeroSpellsKnown"), "0099N.OccultistZeroKnown");
        occultistBook = new BlueprintSpellbook
        {
            Spontaneous = true,
            AllSpellsKnown = false,
            CastingAttribute = StatType.Intelligence,
            ComponentsArray = new BlueprintComponent[]
            {
                new Occultist0099SpellbookMarker0099
                {
                    name = "$0099N$Marker",
                    ImplementLists = new[]
                    {
                        new Occultist0099ImplementSpellList
                        {
                            SlotKey = "slot-1", School = "Abjuration", List = Ref<BlueprintSpellListReference>(firstImplementList)
                        },
                        new Occultist0099ImplementSpellList
                        {
                            SlotKey = "slot-2", School = "Abjuration", ExtraSpells = true,
                            List = Ref<BlueprintSpellListReference>(extraImplementList)
                        }
                    }
                }
            }
        };
        Set(occultistBook, "m_CharacterClass",
            Ref<BlueprintCharacterClassReference>(occultistClass));
        Set(occultistBook, "m_SpellList", Ref<BlueprintSpellListReference>(occultistList));
        Set(occultistBook, "m_SpellsPerDay",
            Ref<BlueprintSpellsTableReference>(donorTable));
        Set(occultistBook, "m_SpellsKnown", Ref<BlueprintSpellsTableReference>(zeroKnown));
        Stage(occultistBook, Occultist0099Ids.BookGuid, "0099N.OccultistSpellbook");
        Set(occultistClass, "m_Spellbook", Ref<BlueprintSpellbookReference>(occultistBook));

        focusResource = Stage(new BlueprintAbilityResource
        {
            ComponentsArray = Array.Empty<BlueprintComponent>()
        }, Occultist0099Ids.FocusResourceGuid, "0099N.MentalFocusResource");

        var effects = (BlueprintScriptableObject[])AccessTools.Method(Production("Occultist0099Abjuration"), "Build")
            .Invoke(null, new object[] { Native<BlueprintAbility>(Occultist0099AbjurationCatalog.MageShield),
                Native<BlueprintAbility>("0a5ddfbcfb3989543ac7c936fc256889"), null });
        foreach (var effect in effects) Stage(effect, effect.AssetGuid.ToString(), effect.name);
        mindBarrier = Native<BlueprintAbility>(Occultist0099Ids.MindBarrierAbilityGuid);
        mindBarrierBuff = Native<BlueprintBuff>(Occultist0099Ids.MindBarrierBuffGuid);
        wardingTalisman = Native<BlueprintBuff>(Occultist0099Ids.WardingTalismanBuffGuid);
        abjurationImplement = Native<BlueprintFeature>(Occultist0099Ids.Id("AbjurationImplementFocus"));

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
            FocusResource = Ref<BlueprintAbilityResourceReference>(focusResource),
            WardingTalisman = Ref<BlueprintBuffReference>(wardingTalisman),
            MindBarrierBuff = Ref<BlueprintBuffReference>(mindBarrierBuff)
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

    private static AbilityData GrantMindBarrier(UnitEntityData unit)
    {
        var ability = unit.Abilities.Enumerable.FirstOrDefault(a => a.Blueprint == mindBarrier);
        if (ability != null) return ability.Data;
        unit.Facts.Add(new Feature(abjurationImplement, unit.Descriptor));
        return unit.Abilities.Enumerable.Single(a => a.Blueprint == mindBarrier).Data;
    }

    private static AbilityData GrantAbility(UnitEntityData unit, BlueprintAbility blueprint)
    {
        var fact = new Ability(blueprint, unit.Descriptor);
        unit.Facts.Add(fact);
        return fact.Data;
    }

    private static UnitUseAbility NativeCommand(AbilityData ability, UnitEntityData unit)
    {
        // The private test ability carries no metamagic. The native getter
        // wraps that check in Unity profiling, which the managed host lacks.
        var target = AccessTools.Method(typeof(AbilityData), nameof(AbilityData.HasMetamagic));
        var prefix = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NotTacticalCombat));
        harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        try { return new UnitUseAbility(ability, unit, UnitCommand.CommandType.Swift); }
        finally { harmony.Unpatch(target, prefix); }
    }

    private static RuleCastSpell CastRule(AbilityData ability, UnitUseAbility command = null)
    {
        var rule = new RuleCastSpell(ability, ability.Caster.Unit);
        if (command != null)
            AccessTools.Method(Production("Occultist0099NativeCast"), "BindCommand").Invoke(null, new object[] { rule, command });
        return Rulebook.Trigger(rule);
    }

    private static void InvestAndLock(UnitEntityData unit, int points)
    {
        GrantMindBarrier(unit);
        InFocus(unit, f =>
        {
            var state = unit.Get<Occultist0099FocusPart>().State;
            Eq(Occultist0099Outcome.Ok, Occultist0099Focus.Invest(state, "Abjuration", points), "MF valid investment");
            Eq(Occultist0099Outcome.Ok, Occultist0099Focus.Lock(state), "MF valid lock");
        });
    }

    private static void AbjurationEffectsNative()
    {
        var unit = Unit("abjuration-effects", occultistClass, 5, 14);
        var outsider = Unit("abjuration-effects-outsider", null, 0, 10);
        var holder = new Feature(focusFact, unit.Descriptor);
        unit.Facts.Add(holder);
        var implement = new Feature(abjurationImplement, unit.Descriptor);
        unit.Facts.Add(implement);
        var lockFocus = GrantAbility(unit, Native<BlueprintAbility>(Occultist0099Ids.LockFocusAbilityGuid));
        var reset = GrantAbility(unit, Native<BlueprintAbility>(Occultist0099Ids.ResetFocusAbilityGuid));
        var invest = unit.Abilities.Enumerable.Single(a => a.Blueprint.AssetGuid == P(Occultist0099Ids.InvestAbilityGuid)).Data;
        var replenish = unit.Abilities.Enumerable.Single(a => a.Blueprint.AssetGuid == P(Occultist0099Ids.ReplenishAbilityGuid)).Data;
        var barrier = unit.Abilities.Enumerable.Single(a => a.Blueprint == mindBarrier).Data;
        var state = unit.Get<Occultist0099FocusPart>().State;
        Check(state.Schools["Abjuration"].Present && lockFocus.Blueprint.IsFullRoundAction &&
              barrier.Blueprint.ActionType == UnitCommand.CommandType.Swift && barrier.Blueprint.CanTargetSelf,
            "MF01 native implement grants a swift barrier and the shared focus ability locks as a full-round action");
        var saves = new[] { unit.Stats.SaveFortitude, unit.Stats.SaveReflex, unit.Stats.SaveWill };
        var baseline = saves.Select(stat => stat.ModifiedValue).ToArray();
        var pool = state.Schools["Abjuration"];
        var abilityEffect = invest.Blueprint.GetComponent<Occultist0099FocusOperation>();
        var target = new TargetWrapper(unit);
        var execution = new AbilityExecutionContext(invest, AbilityParams.Empty, target);
        applyNativeEffects = true;
        try
        {
            var rejected = CastRule(barrier);
            Check(rejected.ForceFail && unit.Buffs.GetBuff(mindBarrierBuff) == null,
                "MF01 native barrier before lock creates no temporary hit points");
            abilityEffect.Apply(execution, new TargetWrapper(outsider));
            abilityEffect.Apply(new AbilityExecutionContext(invest, AbilityParams.Empty,
                new TargetWrapper(outsider), caster: outsider), new TargetWrapper(outsider));
            Check(pool.Invested == 0, "MF01 a foreign target or context caster cannot alter the grant owner's ledger");
            abilityEffect.Apply(execution, target);
            abilityEffect.Apply(execution, target);
            Check(state.Generic == 6 && pool.Invested == 1 && pool.Remaining == 1,
                "MF01 repeated ability-effect execution invests only once");
            CastRule(reset);
            Check(state.Generic == 7 && pool.Invested == 0 && pool.Remaining == 0,
                "MF01 native Reset returns the unspent investment before locking");
            var command = NativeCommand(invest, unit);
            var accepted = CastRule(invest, command);
            int dispatches = castDispatches;
            var duplicate = CastRule(invest, command);
            Check(accepted.Success && duplicate.ForceFail && castDispatches == dispatches &&
                  pool.Invested == 1 && state.Generic == 6,
                "MF01 a second native rule for the same focus command fails before effect dispatch");
            for (int i = 0; i < 3; i++) CastRule(invest);
            Check(state.Generic == 3 && pool.Remaining == 4 && unit.Buffs.GetBuff(wardingTalisman) == null,
                "MF01 native investment does not activate resonance before lock");
            CastRule(lockFocus);
            Check(state.Locked && unit.Buffs.GetBuff(wardingTalisman) != null &&
                  saves.Select((stat, i) => stat.ModifiedValue - baseline[i]).All(delta => delta == 2),
                "MF01 native Warding Talisman applies +2 to all three saves when locked");
            var strongerResistance = saves[0].AddModifier(3, ModifierDescriptor.Resistance);
            Check(saves[0].ModifiedValue == baseline[0] + 3 && saves[1].ModifiedValue == baseline[1] + 2,
                "MF01 native Resistance stacking retains the higher bonus for the affected save");
            strongerResistance.Remove();
            var cost = (Occultist0099FocusResourceLogic)barrier.ResourceLogic;
            Check(cost.IsAbilityRestrictionPassed(barrier) && !abilityEffect.IsAbilityRestrictionPassed(invest),
                "MF01 the native availability check permits the power and refuses locked investment");
            CastRule(barrier);
            var shield = unit.Buffs.GetBuff(mindBarrierBuff);
            Check(shield != null && shield.TimeLeft == TimeSpan.FromSeconds(6) &&
                  unit.Stats.TemporaryHitPoints.ModifiedValue == 10 && state.Generic == 3 && pool.Remaining == 3,
                "MF01 the managed native cast applies the one-round 10-point barrier and pays only Abjuration");
            CastRule(replenish);
            Check(state.Generic == 2 && pool.Remaining == 4 && unit.Buffs.GetBuff(wardingTalisman) != null,
                "MF03 native Replenish trades one general point for one spent school point");

            var damageContext = new AbilityExecutionContext(barrier, AbilityParams.Empty, target);
            Rulebook.Trigger(new RuleDealDamage(outsider, unit, new DirectDamage(DiceFormula.Zero, 6)) { Reason = damageContext });
            Check(unit.Damage == 0 && unit.Stats.TemporaryHitPoints.ModifiedValue == 4 && unit.Buffs.GetBuff(mindBarrierBuff) == shield,
                "MF01 the native damage pipeline consumes six points while keeping the active shield");
            Rulebook.Trigger(new RuleDealDamage(outsider, unit, new DirectDamage(DiceFormula.Zero, 5)) { Reason = damageContext });
            Check(unit.Damage == 1 && unit.Stats.TemporaryHitPoints.ModifiedValue == 0 && unit.Buffs.GetBuff(mindBarrierBuff) == null,
                "MF01 the native temporary-hit-point handler removes the depleted shield and only excess damage lands");

            unit.Progression.GetClassData(occultistClass).Level = 6;
            CastRule(barrier);
            shield = unit.Buffs.GetBuff(mindBarrierBuff);
            Check(unit.Stats.TemporaryHitPoints.ModifiedValue == 12 && state.Budget == 7,
                "MF01 live class level scales the shield without increasing today's focus budget");
            currentTime = shield.EndTime;
            unit.Buffs.TickBuff(shield, currentTime);
            Check(unit.Buffs.GetBuff(mindBarrierBuff) == null && unit.Stats.TemporaryHitPoints.ModifiedValue == 0,
                "MF01 the native expiration removes an unspent barrier at one round");
            for (int i = 0; i < 3; i++) CastRule(barrier);
            Check(pool.Remaining == 0 && saves.Select((stat, i) => stat.ModifiedValue - baseline[i]).All(delta => delta == 2),
                "MF01 spending the school pool to zero preserves all three frozen save bonuses");
            unit.Facts.Remove(implement);
            Check(unit.Buffs.GetBuff(wardingTalisman) == null && unit.Buffs.GetBuff(mindBarrierBuff) == null &&
                  saves.Select((stat, i) => stat.ModifiedValue == baseline[i]).All(unchanged => unchanged),
                "MF01 removal of the native implement revokes both effects and all save modifiers");

            Rest(unit);
            implement = new Feature(abjurationImplement, unit.Descriptor);
            unit.Facts.Add(implement);
            invest = unit.Abilities.Enumerable.Single(a => a.Blueprint.AssetGuid == P(Occultist0099Ids.InvestAbilityGuid)).Data;
            barrier = unit.Abilities.Enumerable.Single(a => a.Blueprint == mindBarrier).Data;
            for (int i = 0; i < 4; i++) CastRule(invest);
            CastRule(lockFocus);
            CastRule(barrier);
            Check(unit.Buffs.GetBuff(mindBarrierBuff) != null && unit.Buffs.GetBuff(wardingTalisman) != null,
                "MF04 pre-rest unit carries both native effects");
            Rest(unit);
            Check(unit.Buffs.GetBuff(mindBarrierBuff) == null && unit.Buffs.GetBuff(wardingTalisman) == null && !state.Locked,
                "MF04 a real rest clears both derived effects and unlocks the ledger");

            for (int i = 0; i < 4; i++) CastRule(invest);
            CastRule(lockFocus);
            CastRule(barrier);
            int identities = state.ChargedExecutions.Count;
            unit.Facts.Remove(holder);
            unit.Facts.Remove(implement);
            unit.Facts.Add(new Feature(focusFact, unit.Descriptor));
            Check(!pool.Present && pool.Remaining == 0 && state.Invalidated == 3 && state.Spent == 1 &&
                  identities == state.ChargedExecutions.Count && unit.Buffs.GetBuff(wardingTalisman) == null,
                "MF09 reattaching focus after losing its implement retires the locked school balance without rearming the ward");
            unit.Facts.Add(new Feature(abjurationImplement, unit.Descriptor));
            barrier = unit.Abilities.Enumerable.Single(a => a.Blueprint == mindBarrier).Data;
            Check(pool.Present && !((Occultist0099FocusResourceLogic)barrier.ResourceLogic).IsAbilityRestrictionPassed(barrier) &&
                  unit.Buffs.GetBuff(wardingTalisman) == null,
                "MF09 same-day implement reacquisition cannot reuse retired points or the old resonance");
            Rest(unit);
            implement = unit.Progression.Features.Enumerable.Single(f => f.Blueprint == abjurationImplement);
            invest = unit.Abilities.Enumerable.Single(a => a.Blueprint.AssetGuid == P(Occultist0099Ids.InvestAbilityGuid)).Data;
            CastRule(invest);
            unit.Facts.Remove(implement);
            Check(state.Generic == state.Budget && state.Invalidated == 0 && pool.Remaining == 0 && !pool.Present,
                "MF09 removing the implement before lock returns its unspent focus to the general pool");
        }
        finally { applyNativeEffects = false; }
        caseStatus["MF01"] = "native_focus_operations_pre_effect_payment_command_identity_buff_damage_and_resonance";
        caseStatus["MF03"] = "native_invest_reset_replenish_and_locked_refusal";
    }

    private static Occultist0099FocusPart TransferRespecSurvivors(UnitEntityData unit, bool keepTemporaryClass = true)
    {
        // The native first-stage commit captures this interface list before
        // replacing the character's parts with the respec unit's save data.
        var survivors = unit.Parts.GetAll<IUnitPartSurviveRespec>(p => p.ShouldSurviveRespec).ToArray();
        Check(survivors.OfType<Occultist0099FocusPart>().Count() == 1,
            "MF09 native survivor enumeration captures the active or detached 0099 authority exactly once");
        unit.Parts.TurnOff();
        var settings = new JsonSerializerSettings { ContractResolver = new RespecContractResolver(), TypeNameHandling = TypeNameHandling.Auto };
        var temporaryParts = new EntityPartsManager(unit);
        if (keepTemporaryClass)
        {
            var temporary = temporaryParts.Ensure<Occultist0099FocusPart>();
            temporary.State = Occultist0099Focus.Create();
            Occultist0099Focus.BeginCycle(temporary.State, 1, 0, "respec-temporary-day", true);
        }
        var replacementParts = JsonConvert.SerializeObject(temporaryParts, settings);
        JsonConvert.PopulateObject(replacementParts, unit.Parts, settings);
        unit.Parts.ClearCache();
        Check(unit.Get<Occultist0099FocusPart>()?.State.LastRestEventId == (keepTemporaryClass ? "respec-temporary-day" : null),
            "MF09 the native respec resolver replaces the old collection with the temporary build's parts");
        foreach (var survivor in survivors) survivor.CopyAfterRespec(unit);
        // ForcePostLoad relinks deserialized parts before native event dispatch.
        unit.Parts.RestoreOwnerLink(unit);
        unit.Parts.TurnOn();
        return unit.Get<Occultist0099FocusPart>();
    }

    private static void SignalRespecCommitted(UnitEntityData unit)
        => EventBus.RaiseEvent<IUnitChangedAfterRespecHandler>(handler => handler.HandleUnitChangedAfterRespec(unit));

    private static void FocusAndLearningArchiveNative(Occultist0099FocusPart part)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new OptInContractResolver(),
            TypeNameHandling = TypeNameHandling.Auto
        };
        string json = JsonConvert.SerializeObject(part, settings);
        var loaded = JsonConvert.DeserializeObject<Occultist0099FocusPart>(json, settings);
        var serializedPart = JObject.Parse(json);
        Check(loaded?.State != null && loaded.State.Fingerprint() == part.State.Fingerprint() &&
              (string)serializedPart["LastSuccessfulRest"] == part.State.LastRestEventId,
            "MF09 the native opt-in serializer preserves all authoritative fields and the last successful rest in the focus part");
        Check(serializedPart["CopyingFacts"] == null && serializedPart["RespecDraft"] == null,
            "MF09 transient preview construction flags stay out of the archived ledger");
        var invalid = part.State.Clone();
        invalid.RespecBudgetCap = invalid.Spent;
        Check(Occultist0099Focus.Validate(invalid) == Occultist0099Outcome.RejectedInvalidState && !invalid.Enabled,
            "MF09 a persisted cap below usable points fails closed even when ordinary conservation holds");

        var spells = Occultist0099Spells.Create(4, 16);
        Occultist0099Spells.AddSlot(spells, "slot-1", "Abjuration");
        Occultist0099Spells.Learn(spells, "slot-1", 1, Occultist0099AbjurationCatalog.MageShield);
        var savedBook = JsonConvert.SerializeObject(new Occultist0099SpellbookData { State = spells }, settings);
        var loadedBook = JsonConvert.DeserializeObject<Occultist0099SpellbookData>(savedBook, settings);
        Check(loadedBook?.State?.Fingerprint() == spells.Fingerprint() &&
              Occultist0099Spells.IsKnown(loadedBook.State, Occultist0099AbjurationCatalog.MageShield),
            "SP03 native opt-in serialization preserves a learned root, its exact slot, and private book provenance");
    }

    private static void FocusPreviewAndRespecNative()
    {
        var live = Unit("focus-preview-live", occultistClass, 5, 18);
        var liveHolder = new Feature(focusFact, live.Descriptor);
        live.Facts.Add(liveHolder);
        var mind = GrantMindBarrier(live);
        var extra = Stage(new BlueprintFeature(), Occultist0099Ids.Id("NativeOnlyExtraAbjuration"), "0099N.ExtraSpells");
        live.Facts.Add(new Feature(extra, live.Descriptor));
        Rest(live);
        InvestAndLock(live, 4);
        var command = NativeCommand(mind, live);
        Check(CastRule(mind, command).Success, "MF09 live source has a paid command before the native preview is opened");
        var oldPart = live.Get<Occultist0099FocusPart>();
        var authority = oldPart.State;
        string restIdentity = authority.LastRestEventId;
        string initial = authority.Fingerprint();
        FocusAndLearningArchiveNative(oldPart);
        var foreign = Unit("focus-respec-foreign", null, 0, 18);
        oldPart.CopyAfterRespec(foreign);
        Check(foreign.Get<Occultist0099FocusPart>() == null,
            "MF09 a survivor captured for one character cannot populate another unit's ledger");

        var preview = Unit("focus-preview-copy", occultistClass, 5, 18);
        preview.PreviewOf = new EntityRef<UnitEntityData>(live);
        preview.Facts.TurnOff();
        AccessTools.Method(typeof(UnitHelper), "CopyFacts").Invoke(null, new object[] { live, preview });
        preview.Facts.TurnOn();
        var draft = preview.Get<Occultist0099FocusPart>().State;
        Check(!ReferenceEquals(authority, draft) && draft.Fingerprint() == initial &&
              preview.Resources.GetResourceAmount(focusResource) == authority.Available,
            "MF09 the real native fact-copy path starts with a deep copy of the paid and locked focus ledger");
        preview.RemoveFact(preview.Progression.Features.Enumerable.Single(f => f.Blueprint == extra));
        Check(draft.Fingerprint() == initial, "MF09 dropping Extra Spells in preview does not revoke the base implement");
        preview.RemoveFact(preview.Progression.Features.Enumerable.Single(f => f.Blueprint == abjurationImplement));
        Check(draft.Invalidated == 3 && draft.Schools["Abjuration"].Remaining == 0 && !draft.Schools["Abjuration"].Present,
            "MF09 actual preview fact removal retires only its own copy of the school balance");
        Stable(initial, authority.Fingerprint(), "MF09 discarding the altered native preview leaves live authority byte-for-byte unchanged");

        var scratch = Unit("focus-respec-vacuum", occultistClass, 1, 10);
        scratch.PreviewOf = new EntityRef<UnitEntityData>(live);
        scratch.Facts.Add(new Feature(focusFact, scratch.Descriptor));
        var scratchState = scratch.Get<Occultist0099FocusPart>().State;
        Check(!ReferenceEquals(scratchState, authority) && scratchState.Fingerprint() == initial &&
              scratch.Resources.GetResourceAmount(focusResource) == 0,
            "MF09 the native respec vacuum sees the inherited day without refilling or exposing it for use");

        var window = Empty<RespecWindowVM>();
        Set(window, "IsRespec", true);
        Set(window, "CurrentUnit", new UniRx.ReactiveProperty<UnitEntityData>(live));
        int completions = 0;
        Set(window, "m_EndAction", new Action(() => completions++));
        RespecWindowVM.Instance = window;
        var sameUnitId = live.UniqueId;
        respecXpOwner = live.Progression;
        respecXpTable = new BlueprintStatProgression { Bonuses = Enumerable.Range(0, 22).Select(i => i * 1000).ToArray() };
        var xpTable = AccessTools.PropertyGetter(typeof(UnitProgressionData), nameof(UnitProgressionData.ExperienceTable));
        var xpBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ProvideRespecExperience));
        harmony.Patch(xpTable, prefix: new HarmonyMethod(xpBoundary));
        try
        {
            var transferred = TransferRespecSurvivors(live);
            var character = live.Progression.GetClassData(occultistClass);
            character.Level = 1;
            Set(live.Progression, "Experience", 5000);
            Set(live.Progression, "m_CharacterLevel", (int?)1);
            Set(live.Progression, "m_MythicLevel", (int?)0);
            SetInt(live, 10);
            var intermediate = live.Progression.Features.Enumerable.Single(f => f.Blueprint == abjurationImplement);
            live.RemoveFact(intermediate);
            SignalRespecCommitted(live);
            Check(!ReferenceEquals(authority, transferred.State) && transferred.State.Fingerprint() == initial &&
                  live.UniqueId == sameUnitId,
                "MF09 the native respec survivor retains the whole cycle through the first committed but unfinished level");
            window.Complete();
            Check(completions == 0 && live.Resources.GetResourceAmount(focusResource) == 0 &&
                  transferred.State.Fingerprint() == initial,
                "MF09 native Complete cannot finalize while the normal progression still has levels to rebuild");
            character.Level = 5;
            Set(live.Progression, "m_CharacterLevel", (int?)5);
            SetInt(live, 18);
            mind = GrantMindBarrier(live);
            window.Complete();
            Stable(initial, transferred.State.Fingerprint(),
                "MF09 ending the native rebuild at the same class and Intelligence preserves paid IDs and investment");
            Check(completions == 1 && live.Buffs.GetBuff(wardingTalisman) != null && live.Resources.GetResourceAmount(focusResource) == 8,
                "MF09 the finalized build recovers the valid ward and projects the original available balance");
            AccessTools.Method(typeof(RespecWindowVM), "DisposeImplementation").Invoke(window, null);
            Stable(initial, transferred.State.Fingerprint(), "MF09 closing the already finalized native window leaves the ledger unchanged");
        }
        finally
        {
            harmony.Unpatch(xpTable, xpBoundary);
            respecXpOwner = null;
            respecXpTable = null;
            RespecWindowVM.Instance = null;
        }

        var partial = Empty<RespecWindowVM>();
        Set(partial, "IsRespec", true);
        Set(partial, "CurrentUnit", new UniRx.ReactiveProperty<UnitEntityData>(live));
        RespecWindowVM.Instance = partial;
        Occultist0099FocusPart lowered;
        try
        {
            lowered = TransferRespecSurvivors(live);
            SetInt(live, 14);
            SignalRespecCommitted(live);
            Check(lowered.State.RespecBudgetCap == null && lowered.State.Available == 8,
                "MF09 a continued native window does not use temporary post-respec stats for the cap");
            AccessTools.Method(typeof(RespecWindowVM), "DisposeImplementation").Invoke(partial, null);
        }
        finally { RespecWindowVM.Instance = null; }
        Check(lowered.State.Budget == 9 && lowered.State.RespecBudgetCap == 7 && lowered.State.Spent == 1 &&
              lowered.State.Invalidated == 2 && lowered.State.Available == 6 && lowered.State.LastRestEventId == restIdentity,
            "MF09 finalized respec with lower Intelligence applies the lower cap without starting a day");
        FocusAndLearningArchiveNative(lowered);
        string clamped = lowered.State.Fingerprint();
        lowered = TransferRespecSurvivors(live);
        SignalRespecCommitted(live);
        Stable(clamped, lowered.State.Fingerprint(), "MF09 repeating the same cap cannot retire the same budget twice");

        var removed = TransferRespecSurvivors(live, keepTemporaryClass: false);
        live.Progression.Classes.Clear();
        live.RemoveFact(live.Progression.Features.Enumerable.Single(f => f.Blueprint == abjurationImplement));
        live.RemoveFact(liveHolder);
        SignalRespecCommitted(live);
        Check(!removed.State.ClassPresent && removed.State.Available == 0 && removed.State.Spent == 1 &&
              removed.State.LastRestEventId == restIdentity && removed.State.ChargedExecutions.Count == 1,
            "MF09 native respec without any Occultist facts retains only disabled day authority and paid IDs");
        live.Progression.Classes.Add(new ClassData(occultistClass) { Level = 5 });
        live.Facts.Add(new Feature(focusFact, live.Descriptor));
        mind = GrantMindBarrier(live);
        Check(!((Occultist0099FocusResourceLogic)mind.ResourceLogic).IsAbilityRestrictionPassed(mind) &&
              live.Resources.GetResourceAmount(focusResource) == 0 && live.Buffs.GetBuff(wardingTalisman) == null,
            "MF09 same-day reacquisition after committed loss cannot mint a new pool or old ward");
        Rest(live);
        Check(removed.State.LastRestEventId != restIdentity && removed.State.RespecBudgetCap == null &&
              removed.State.Available == 7 && removed.State.Spent == 0 && removed.State.ChargedExecutions.Count == 0,
            "MF09 the next real rest clears the retired budget cap and creates a legitimate fresh cycle");
        Check(mindBarrierBuff.IsClassFeature && wardingTalisman.IsClassFeature,
            "MF09 native respec treats both effects as class-owned and does not carry them as ordinary buffs");
        caseStatus["MF09"] = "native_copy_facts_respec_survivor_rebuild_window_and_committed_budget";
    }

    private static void RespecMasteryAuthorityNative()
    {
        var unit = Unit("respec-mastery-provenance", occultistClass, 20, 18);
        unit.Facts.Add(new Feature(focusFact, unit.Descriptor));
        GrantMindBarrier(unit);
        InvestAndLock(unit, 4);
        var state = unit.Get<Occultist0099FocusPart>().State;
        Occultist0099Focus.SetMastery(state, "Abjuration");
        Check(Occultist0099Focus.PaySchool(state, "Abjuration", 1, "old-mastery-charge").FromMastery == 1,
            "MF09 fixture establishes prior mastery spending and its old school authorization");
        var copied = TransferRespecSurvivors(unit);
        SignalRespecCommitted(unit);
        InFocus(unit, f => Check(copied.State.MasterySpent == 1 && copied.State.MasteryRemaining == 3 &&
              copied.State.ChargedExecutions.Contains("old-mastery-charge") && copied.State.MasterySchool == null &&
              f.MindBarrier("Abjuration").EffectiveLevel == 20,
            "MF09 rebuilding the same level and school preserves mastery consumption but requires a newly granted authorization"));
    }

    private static void MF01Native()
    {
        var unit = Unit("mf01", occultistClass, 5, 14);
        var originalFact = new Feature(focusFact, unit.Descriptor);
        unit.Facts.Add(originalFact);
        var ledger = unit.Get<Occultist0099FocusPart>().State;
        var ability = GrantMindBarrier(unit);
        var logic = ability.ResourceLogic as Occultist0099FocusResourceLogic;
        Check(logic != null, "MF01 real AbilityData resolves private resource logic");
        Eq(7, ledger.Available, "MF01 real activation establishes the budget");
        Eq(7, unit.Resources.GetResourceAmount(focusResource), "MF01 real activation projects the budget");
        Check(!logic.IsAbilityRestrictionPassed(ability), "MF01 precheck refuses before lock");
        int startDispatches = castDispatches;
        Check(CastRule(ability).ForceFail, "MF01 native rule refuses before lock");
        Eq(startDispatches, castDispatches, "MF01 unpaid rule does not dispatch");
        Eq(7, ledger.Available, "MF01 unpaid rule leaves balance untouched");

        InvestAndLock(unit, 4);
        int resonance = Occultist0099Focus.Resonance(ledger, "Abjuration");
        var first = NativeCommand(ability, unit);
        var second = NativeCommand(ability, unit);
        Check(first != second && ReferenceEquals(first.Ability, second.Ability),
            "D2-01 distinct native commands share the granted Ability.Data");
        var before = ledger.Fingerprint();
        Check(logic.IsAbilityRestrictionPassed(first.Ability) &&
              logic.IsAbilityRestrictionPassed(second.Ability), "D2-01 repeated read-only precheck succeeds");
        Stable(before, ledger.Fingerprint(), "D2-01 precheck leaves the ledger unchanged");

        Check(CastRule(first.Ability, first).Success, "D2-01 first native rule succeeds");
        Eq(6, pointsAtDispatch, "D2-05 first rule is paid before dispatch");
        AbilityCastRateUtils.SpendResources(ability);
        Eq(6, ledger.Available, "D2-01 native trailing payment does not double charge");
        Check(CastRule(first.Ability, first).ForceFail, "D2-01 repeated command cannot dispatch or charge twice");
        Check(logic.IsAbilityRestrictionPassed(second.Ability), "D2-01 second command can precheck same object");
        Check(CastRule(second.Ability, second).Success, "D2-01 second command succeeds normally");
        Eq(startDispatches + 2, castDispatches, "D2-01 exactly two distinct effects were dispatched");
        Eq(5, ledger.Available, "D2-01 two different commands each cost one point");
        Eq(5, unit.Resources.GetResourceAmount(focusResource), "MF01 authoritative charge updates native mirror");
        Eq(resonance, Occultist0099Focus.Resonance(ledger, "Abjuration"), "MF01 resonance stays at its locked value");

        unit.Facts.Remove(originalFact);
        Check(Focus(unit) == null && !ledger.ClassPresent && !logic.IsAbilityRestrictionPassed(ability),
             "D2-02 removed fact disables payment but retains the detached unit ledger");
        unit.Facts.Add(new Feature(focusFact, unit.Descriptor));
        Check(ReferenceEquals(ledger, unit.Get<Occultist0099FocusPart>().State),
              "D2-02 reattached fact resolves the same unit ledger");
        Eq(5, ledger.Available, "D2-02 same-day fact replacement does not refund");
        Check(unit.Buffs.GetBuff(wardingTalisman) != null, "MF09 intact source reconstructs the frozen ward on focus reattachment");
        Check(CastRule(first.Ability, first).ForceFail, "D2-02 prior command stays deduplicated across fact replacement");

        // The real RuleApplySpell.Before path removes the school after UI
        // precheck. The dispatch boundary must remain untouched.
        Check(logic.IsAbilityRestrictionPassed(ability), "D2-05 precheck succeeds before invalidation");
        var activeImplement = unit.Progression.Features.Enumerable.Single(f => f.Blueprint == abjurationImplement);
        beforeDispatch = e => unit.Facts.Remove(activeImplement);
        try { Check(CastRule(ability).ForceFail, "D2-05 invalidated school fails at the rules entry"); }
        finally { beforeDispatch = null; }
        Eq(startDispatches + 2, castDispatches, "D2-05 late refusal dispatches no effect");
        Eq(2, ledger.Spent, "D2-05 late refusal never debits another pool");
        Eq(2, ledger.Invalidated, "MF09 removing a source directly retires its remaining locked points");
        Check(!logic.IsAbilityRestrictionPassed(ability) && AbilityCastRateUtils.GetAvailableForCastCount(ability) < 0,
            "MF01 the generic reserve is neither payable by this school nor displayed as its use count");
        Check(CastRule(new AbilityData(mindBarrier, unit.Descriptor)).ForceFail,
            "MF01 a constructed power without a granted native ability cannot execute");

        var failed = Unit("failed", occultistClass, 4, 14);
        failed.Facts.Add(new Feature(focusFact, failed.Descriptor));
        InvestAndLock(failed, 2);
        var failedAbility = GrantMindBarrier(failed);
        var fizzle = new RuleCastSpell(failedAbility, failed) { SpellFailureChance = 101 };
        Rulebook.Trigger(fizzle);
        Check(fizzle.IsSpellFailed && !fizzle.Success, "D2-05 legitimate native spell failure remains a fizzle");
        Eq(5, failed.Get<Occultist0099FocusPart>().State.Available, "D2-05 an accepted attempt still pays on a fizzle");
        Eq(startDispatches + 2, castDispatches, "D2-05 native fizzle dispatches no effect");
        caseStatus["MF01"] = "native_pre_effect_payment_and_command_identity";
        caseStatus["MF09"] = "native_fact_reattach_school_loss_and_reacquisition_only";
        caseStatus["MF11"] = "native_dedup_and_late_refusal_sticky_touch_not_run";
    }

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
    private static Occultist0099SpellState SpellStateOf(Occultist0099SpellbookFact0099 fact)
    {
        var data = (Occultist0099SpellbookData)ComponentData(fact);
        if (data.State == null) data.State = Occultist0099Spells.Create(0, 10);
        return data.State;
    }

    // ------------------------------------------------------------------
    // SP01 / SP02 native: SelectSpell.Check / Apply and Spellbook.AddKnown.
    // ------------------------------------------------------------------
    private static LevelUpState LevelState(Spellbook book)
    {
        var state = Empty<LevelUpState>();
        Set(state, "m_Unit", book.Owner.Unit);
        Set(state, "Selections", new List<FeatureSelectionState>());
        Set(state, "SpellSelections", new List<SpellSelectionData>());
        Set(state, "SelectedClass", book.Blueprint.CharacterClass);
        state.OnApplyAction();
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
        Set(book, "m_BaseLevelInternal", 0);
        var state = LevelState(book);
        new ApplySpellbook().Apply(state, u.Descriptor);
        state.OnApplyAction();
        var baseline = state.GetSpellSelection(occultistBook, occultistList);
        Check(book.BaseLevel == 1 && occultistBook.Spontaneous && baseline != null && !baseline.HasEmpty(),
            "SP01N native ApplySpellbook advances the spontaneous private book without granting a generic known slot");
        Check(state.GetSpellSelection(occultistBook, firstImplementList) == null,
            "SP01N the baseline action does not offer an implement before it is acquired");
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
        Eq(1, Occultist0099NativeScope.RootOf(mageShield).Circle,
            "SP01N the native root uses the frozen private circle without modifying it");

        // POSITIVE: the native SelectSpell.Check permits the eligible choice and
        // Apply really spends the slot and really adds the known spell.
        state.OnApplyAction();
        var choice = new SelectSpell(occultistBook, firstImplementList, 1, mageShield, 0);
        Check(NativeCheck(choice, state, u.Descriptor),
            "SP01N native SelectSpell.Check permits an eligible private choice");
        choice.Apply(state, u.Descriptor);
        Check(book.IsKnown(mageShield),
            "SP01N native SelectSpell.Apply really adds the root to the private book");
        Check(!state.GetSpellSelection(occultistBook, firstImplementList).CanSpendSlot(1, 0),
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
        var second = LevelState(book);
        Check(!NativeCheck(new SelectSpell(occultistBook, firstImplementList, 1, mageShield, 0),
                second, u.Descriptor),
            "SP01N native Check refuses an already known root in a fresh slot");

        // NEGATIVE: a circle the class level has not opened yet.
        var resist = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.ResistEnergy);
        var early = LevelState(book);
        Check(!NativeCheck(new SelectSpell(occultistBook, firstImplementList, 2, resist, 0),
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
        var foreignChoice = new SelectSpell(foreignBook, occultistList, 1, mageShield, 0);
        var foreignState = LevelState(foreign);
        foreignState.DemandSpellSelection(foreignBook, occultistList).SetLevelSpells(1, 1);
        // The gate only governs the PRIVATE book; a foreign book keeps native
        // behaviour, and the 0099 ledger must not move either way.
        Check(NativeCheck(foreignChoice, foreignState, u.Descriptor),
            "D2-03 a shared root still passes foreign SelectSpell.Check");
        foreignChoice.Apply(foreignState, u.Descriptor);
        Check(foreign.IsKnown(mageShield), "D2-03 a foreign book learns its shared root through native Apply");
        InBook(u, f => Stable(beforeForeign, f.StateFingerprint(),
            "SP01N a foreign spellbook never writes the 0099 quota ledger"));

        var wrongRoot = Stage(new BlueprintAbility
        {
            Type = AbilityType.Spell,
            ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation } }
        }, Guid.NewGuid().ToString("N"), "0099N.OffList");
        var wrongChoice = new SelectSpell(occultistBook, firstImplementList, 1, wrongRoot, 0);
        var wrongState = LevelState(book);
        Check(!NativeCheck(wrongChoice, wrongState, u.Descriptor), "D2-04 private Check refuses an off-list root");
        Check(book.AddKnown(1, wrongRoot) == null && !book.IsKnown(wrongRoot),
            "D2-04 unapproved private AddKnown refuses the off-list root");
        InBook(u, f => Occultist0099Spells.AddSlot(SpellStateOf(f), "slot-2", "Abjuration", extraSpells: true));
        second.OnApplyAction();
        var choiceForExtra = second.GetSpellSelection(occultistBook, extraImplementList);
        Check(second.GetSpellSelection(occultistBook, firstImplementList) == null && choiceForExtra.CanSpendSlot(1, 0),
            "SP01N the previously filled quota is absent and the newly acquired implement has native index zero");
        var warding = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.Roots
            .Single(r => r.Name == "CotWWrathPsychicWardingWeapon").Guid);
        new SelectSpell(occultistBook, extraImplementList, 1, resist, 0).Apply(second, u.Descriptor);
        Check(choiceForExtra.CanSpendSlot(1, 0) && !book.IsKnown(resist),
            "D2-04 a directly applied wrong-circle choice leaves an actual native slot unused");
        var occupied = new SelectSpell(occultistBook, firstImplementList, 1, warding, 0);
        Check(!NativeCheck(occupied, second, u.Descriptor),
            "D2-04 an action for the first source cannot borrow the new source's native index zero");
        Check(!NativeCheck(new SelectSpell(occultistBook, occultistList, 1, warding, 0), second, u.Descriptor),
            "SP01N the private union list is not a stable source");
        var newSource = new SelectSpell(occultistBook, extraImplementList, 1, warding, 0);
        Check(NativeCheck(newSource, second, u.Descriptor), "SP01N the native extra implement offer selects its own quota");
        newSource.Apply(second, u.Descriptor);
        second.OnApplyAction();
        Check(second.GetSpellSelection(occultistBook, extraImplementList).LevelCount[1].SpellSelections[0] == warding,
            "SP01N refreshing native offers preserves the pending native selection for undo");
        InBook(u, f => Eq(warding.AssetGuid.ToString(),
            SpellStateOf(f).Quotas[Occultist0099Spells.QuotaKey("slot-2", 1)],
            "D2-04 the ledger records the stable source carried by the native action"));
        var preview = Unit("sp01-preview", occultistClass, 1, 16);
        preview.Facts.Add(new Feature(spellbookFact, preview.Descriptor));
        var previewBook = Book(preview);
        Set(preview.Descriptor.DemandSpellbook(foreignBook), "m_BaseLevelInternal", foreign.RawBaseLevel);
        AccessTools.Method(typeof(UnitHelper), "CopySpellbook").Invoke(null, new object[] { u, preview });
        Check(previewBook.IsKnown(mageShield) && previewBook.IsKnown(warding),
            "D2-04 native CopySpellbook retains learned roots in the preview");
        string sourceBefore = null;
        InBook(u, f => sourceBefore = f.StateFingerprint());
        InBook(preview, f =>
        {
            Stable(sourceBefore, f.StateFingerprint(), "D2-04 copied book retains implement provenance and filled quotas");
            Occultist0099Spells.RemoveSlot(SpellStateOf(f), "slot-2");
        });
        InBook(u, f => Stable(sourceBefore, f.StateFingerprint(),
            "D2-04 preview ledger mutations never affect the live book"));
        caseStatus["SP01"] = "native_level_up_offers_select_apply_isolated_book_and_exact_sources";
        caseStatus["SP03"] = "native_copy_provenance_and_preview_isolation_only";
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
        Check(!NativeCheck(new SelectSpell(occultistBook, firstImplementList, 2, resist, 0),
                LevelState(book), u.Descriptor),
            "SP02N native Check refuses circle 2 while the real Int stat is 11");
        Check(book.AddKnown(2, resist) == null,
            "SP02N native AddKnown refuses circle 2 while Int is 11");

        // A live attribute change is seen without requiring a rest.
        SetInt(u, 16);
        Check(NativeCheck(new SelectSpell(occultistBook, firstImplementList, 2, resist, 0),
                LevelState(book), u.Descriptor),
            "SP02N native Check permits circle 2 after the real Int raise");

        // Lower Int again: an established quota survives, new learning does not.
        var state = LevelState(book);
        new SelectSpell(occultistBook, firstImplementList, 2, resist, 0).Apply(state, u.Descriptor);
        Check(book.IsKnown(resist), "SP02N circle 2 root really learned at Int 16");
        InBook(u, f => Occultist0099Spells.AddSlot(SpellStateOf(f), "slot-2", "Abjuration", extraSpells: true));
        var lowInt = LevelState(book);
        var alliedCloak = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.Roots
            .Single(r => r.Name == "CotWWrathPsychicAlliedCloak").Guid);
        SetInt(u, 11);
        Check(!NativeCheck(new SelectSpell(occultistBook, extraImplementList, 2, alliedCloak, 0),
                lowInt, u.Descriptor), "D2-04 a temporary Int drop blocks new learning without rest");
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

        caseStatus["SP02"] = "native_live_int_learning_plus_model_cast_gate";
    }

    private static void ExhaustedImplementQuotaNative()
    {
        var u = Unit("sp01-exhausted", occultistClass, 16, 16);
        u.Facts.Add(new Feature(spellbookFact, u.Descriptor));
        var book = Book(u);
        InBook(u, f =>
        {
            var ledger = SpellStateOf(f);
            Occultist0099Spells.AddSlot(ledger, "slot-1", "Abjuration");
            Occultist0099Spells.AddSlot(ledger, "slot-2", "Abjuration", extraSpells: true);
        });
        var state = LevelState(book);
        var globe = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.GlobeOfInvulnerability);
        var first = new SelectSpell(occultistBook, firstImplementList, 6, globe, 0);
        var extra = new SelectSpell(occultistBook, extraImplementList, 6, globe, 0);
        Check(NativeCheck(first, state, u.Descriptor) && NativeCheck(extra, state, u.Descriptor),
            "SP01N a newly open circle offers one native slot for each implement");
        var reward = Stage(new BlueprintDlcReward(), Guid.NewGuid().ToString("N"), "0099N.UnavailableReward");
        Set(reward, "m_IsAvailable", false);
        var restriction = new DlcCondition { name = "$0099N$GlobeRestriction" };
        Set(restriction, "m_DlcReward", Ref<BlueprintDlcRewardReference>(reward));
        var components = globe.ComponentsArray;
        var row = extraImplementList.SpellsByLevel[6];
        try
        {
            globe.ComponentsArray = components.Concat(new BlueprintComponent[] { restriction }).ToArray();
            Set(row, "m_SpellsFiltered", null);
            Check(row.SpellsRefs.Count == 1 && extraImplementList.GetSpells(6).Count == 0,
                "SP01N native DLC filtering hides the sole raw root");
            var restricted = LevelState(book);
            Check(restricted.GetSpellSelection(occultistBook, extraImplementList).LevelCount[6] == null,
                "SP01N no native slot is allocated for the DLC-hidden root");
            extra.Apply(state, u.Descriptor);
            Check(!book.IsKnown(globe) && state.GetSpellSelection(occultistBook, extraImplementList).CanSpendSlot(6, 0),
                "SP01N directly replaying the stale DLC-hidden choice still cannot spend the native slot");
        }
        finally
        {
            globe.ComponentsArray = components;
            Set(row, "m_SpellsFiltered", null);
        }
        extra.Apply(state, u.Descriptor);
        state.OnApplyAction();
        Check(state.GetSpellSelection(occultistBook, firstImplementList).LevelCount[6] == null &&
              state.GetSpellSelection(occultistBook, extraImplementList).LevelCount[6].SpellSelections[0] == globe,
            "SP01N native offers remove the exhausted page and preserve the pending selection");
        InBook(u, f => Check(SpellStateOf(f).Quotas[Occultist0099Spells.QuotaKey("slot-1", 6)] == null,
            "SP01N the exhausted quota remains open in the source ledger for a later upgrade"));
    }

    private static void ImplementReplayNative()
    {
        var live = Unit("sp01-replay-live", occultistClass, 4, 16);
        live.Facts.Add(new Feature(spellbookFact, live.Descriptor));
        var liveBook = Book(live);
        InBook(live, f =>
        {
            var ledger = SpellStateOf(f);
            Occultist0099Spells.AddSlot(ledger, "slot-1", "Abjuration");
            Occultist0099Spells.AddSlot(ledger, "slot-2", "Abjuration", extraSpells: true);
        });
        var warding = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.WardingWeapon);
        var first = new SelectSpell(occultistBook, firstImplementList, 1, warding, 0);
        var extra = new SelectSpell(occultistBook, extraImplementList, 1, warding, 0);
        var copyBook = AccessTools.Method(typeof(UnitHelper), "CopySpellbook");
        var preview = Unit("sp01-replay-preview", occultistClass, 4, 16);
        preview.Facts.Add(new Feature(spellbookFact, preview.Descriptor));
        var previewBook = Book(preview);
        copyBook.Invoke(null, new object[] { live, preview });
        var state = LevelState(previewBook);
        var before = state.GetSpellSelection(occultistBook, firstImplementList);
        Check(NativeCheck(extra, state, preview.Descriptor), "SP01N a rebuilt preview accepts the same stored source action");
        extra.Apply(state, preview.Descriptor);
        state.OnApplyAction();
        var after = state.GetSpellSelection(occultistBook, firstImplementList);
        Check(before.CanSpendSlot(1, 0) && after.CanSpendSlot(1, 0) && !ReferenceEquals(before, after),
            "SP01N candidate loss replaces the whole source offer while the native slot count is unchanged");
        var phase = Empty<CharGenSpellsPhaseVM>();
        Set(phase, "m_SelectionData", before);
        Check(!phase.TryUpdateSamePhase(after), "SP01N native source phases refuse reuse after a candidate is learned elsewhere");
        Check(previewBook.IsKnown(warding) && !liveBook.IsKnown(warding),
            "SP01N discarding the preview would leave the live book unmodified");

        var changed = Unit("sp01-replay-source-removed", occultistClass, 4, 16);
        changed.Facts.Add(new Feature(spellbookFact, changed.Descriptor));
        var changedBook = Book(changed);
        copyBook.Invoke(null, new object[] { live, changed });
        InBook(changed, f => Occultist0099Spells.RemoveSlot(SpellStateOf(f), "slot-2"));
        state = LevelState(changedBook);
        Check(NativeCheck(first, state, changed.Descriptor) && !NativeCheck(extra, state, changed.Descriptor),
            "SP01N the same unknown root is still offered but replay cannot move it to the surviving source");
        extra.Apply(state, changed.Descriptor);
        Check(!changedBook.IsKnown(warding) && state.GetSpellSelection(occultistBook, firstImplementList).CanSpendSlot(1, 0),
            "SP01N the stale action leaves the surviving source and its native choice slot untouched");
    }

    // ------------------------------------------------------------------
    // Rest identity: the real camp serial, idempotent across repeated callbacks.
    // ------------------------------------------------------------------
    private static void RestNative()
    {
        var unit = Unit("rest", occultistClass, 5, 14);
        var fact = new Feature(focusFact, unit.Descriptor);
        unit.Facts.Add(fact);
        var state = unit.Get<Occultist0099FocusPart>().State;
        var ability = GrantMindBarrier(unit);
        int camp = Game.Instance.Player.Camping.OverallCampsCount;

        InvestAndLock(unit, 3);
        Check(CastRule(ability).Success, "D2-06 paid ability before first successful rest");
        Rest(unit);
        string firstRest = state.LastRestEventId;
        Eq(7, state.Available, "D2-06 first native ApplyRest restores focus");
        Eq(7, unit.Resources.GetResourceAmount(focusResource), "D2-06 native rest refreshes the resource mirror");
        Check(!state.Locked, "D2-06 the next cycle starts unlocked");

        InvestAndLock(unit, 3);
        Check(CastRule(ability).Success, "D2-06 same granted ability pays in the new cycle");
        Rest(unit);
        Eq(camp, Game.Instance.Player.Camping.OverallCampsCount, "D2-06 no camp counter increment in either native rest");
        Check(firstRest != state.LastRestEventId && state.Available == 7,
            "D2-06 consecutive successful native ApplyRest calls get distinct identities despite same camp counter");

        var batch = AccessTools.Method(typeof(RestController), "ApplyRest", new[] { typeof(List<UnitEntityData>) });
        var heal = AccessTools.Method(typeof(RestController), nameof(RestController.HealAndApplyRest));
        var healBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(BatchHealBoundary));
        harmony.Patch(heal, prefix: new HarmonyMethod(healBoundary));
        int applications = 0;
        afterBatchRest = () =>
        {
            if (++applications != 1) return;
            InvestAndLock(unit, 3);
            Check(CastRule(ability).Success, "D2-06 payment follows first application in the batch");
        };
        try
        {
            var controller = Empty<RestController>();
            controller.Status = Empty<RestStatus>();
            batch.Invoke(controller, new object[] { new List<UnitEntityData> { unit, unit } });
            Check(applications == 2 && state.Locked && state.Available == 6,
                "D2-06 repeated per-unit ApplyRest in the real native batch is idempotent");
        }
        finally
        {
            afterBatchRest = null;
            harmony.Unpatch(heal, healBoundary);
        }

        unit.Facts.Remove(fact);
        Rest(unit);
        unit.Facts.Add(new Feature(focusFact, unit.Descriptor));
        Eq(7, state.Available, "D2-06 a successful rest during detachment refreshes on reacquisition");
        caseStatus["MF04"] = "native_rest_identity_batch_and_effect_cleanup";
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
            Occultist0099Ids.FocusResourceLogicTypeId, Occultist0099Ids.FocusPartTypeId,
            Occultist0099Ids.SpellbookFactTypeId, Occultist0099Ids.FocusPropertyTypeId,
            Occultist0099Ids.FocusOperationTypeId, Occultist0099Ids.ImplementSchoolTypeId,
            Occultist0099Ids.ImplementSlotTypeId, Occultist0099Ids.SchoolValueTypeId, Occultist0099Ids.SameRaceTypeId,
            Occultist0099Ids.SchoolPowerGrantTypeId, Occultist0099Ids.EnergyShieldTypeId, Occultist0099Ids.GlobeOfNegationTypeId,
            Occultist0099Ids.AegisOptionTypeId, Occultist0099Ids.AegisTargetTypeId, Occultist0099Ids.AegisArmorTypeId,
            Occultist0099Ids.AegisStorageTypeId, Occultist0099Ids.AegisStoreSpellTypeId, Occultist0099Ids.AegisReleaseTypeId,
            Occultist0099Ids.AegisReleaseActionTypeId, Occultist0099Ids.AegisApplyTypeId, Occultist0099Ids.AegisGrantsTypeId,
            Occultist0099Ids.TransmutationLevelTypeId, Occultist0099Ids.LegacyWeaponOptionTypeId,
            Occultist0099Ids.LegacyWeaponTargetTypeId, Occultist0099Ids.LegacyWeaponBuffTypeId, Occultist0099Ids.PhilosophersTouchBuffTypeId,
            Occultist0099Ids.QuicknessCounterTypeId,
            Occultist0099Ids.ProphecyReadyTypeId, Occultist0099Ids.ShadowTierTypeId, Occultist0099Ids.ShadowHealthTypeId,
            Occultist0099Ids.ShadowHealthActionTypeId
        })
            Check(!nativeIds.Contains(Guid.Parse(id)),
                "GATE private TypeId " + id + " has no native collision");
    }

    private static BlueprintsCache faultedRegistration;

    private static void InterruptRegistration(BlueprintsCache __instance, BlueprintGuid __0)
    {
        if (__instance == faultedRegistration && __0 == P(Occultist0099Ids.BookGuid))
            throw new InvalidOperationException("0099 fixture write interrupted");
    }

    private static BlueprintAbility ProviderSpell(string guid, string name, SpellSchool school, BlueprintBuff effect)
    {
        var apply = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff
            { name = "0099N.ProviderApply", Permanent = true };
        Set(apply, "m_Buff", Ref<BlueprintBuffReference>(effect));
        var spell = new BlueprintAbility
        {
            AssetGuid = P(guid), name = name, Type = AbilityType.Spell, Range = AbilityRange.Personal,
            ComponentsArray = new BlueprintComponent[]
            {
                new SpellComponent { name = "0099N.ProviderSchool", School = school },
                new AbilityEffectRunAction { name = "0099N.ProviderEffect", Actions = new Kingmaker.ElementsSystem.ActionList
                    { Actions = new Kingmaker.ElementsSystem.GameAction[] { apply } } }
            }
        };
        spell.OnEnable();
        return spell;
    }

    private static void ProviderBacklink(BlueprintAbility spell, BlueprintSpellList list, int circle)
    {
        var link = new SpellListComponent { name = "0099N.ProviderList", SpellLevel = circle };
        Set(link, "m_SpellList", Ref<BlueprintSpellListReference>(list));
        spell.ComponentsArray = spell.ComponentsArray.Concat(new BlueprintComponent[] { link }).ToArray();
    }

    private static void AddDanglingProviderVariant(BlueprintAbility spell)
    {
        var missing = new BlueprintAbilityReference();
        missing.ReadGuidFromGuid(P(Occultist0099Ids.Id("HostMissingVariant")));
        var variants = new AbilityVariants { name = "0099N.DanglingProviderVariant" };
        Set(variants, "m_Variants", new[] { missing });
        spell.ComponentsArray = spell.ComponentsArray.Concat(new BlueprintComponent[] { variants }).ToArray();
    }

    private static void FireShieldVariantsNative(BlueprintsCache detached, Occultist0099SpellRoot row, BlueprintSpellList wizardList, BlueprintBuff effect)
    {
        var root = ProviderSpell(row.Guid, row.Name, SpellSchool.Evocation, effect);
        var warm = ProviderSpell("54ab6ea1a66d45e3adb5a9afaa723b43", "FireShieldWarm", SpellSchool.Evocation, effect);
        var cold = ProviderSpell("b3578d622d7c42539f783c48b4d723a4", "FireShieldCold", SpellSchool.Evocation, effect);
        foreach (var child in new[] { warm, cold })
            Set(child, "m_Parent", BlueprintReferenceBase.CreateTyped<BlueprintAbilityReference>(root));
        var variants = new AbilityVariants { name = "0099N.FireShieldVariants" };
        Set(variants, "m_Variants", new[] { Ref<BlueprintAbilityReference>(warm), Ref<BlueprintAbilityReference>(cold) });
        root.ComponentsArray = new BlueprintComponent[] { root.GetComponent<SpellComponent>(), variants };
        ProviderBacklink(root, wizardList, 4);
        wizardList.SpellsByLevel[4].SpellsRefs.Add(Ref<BlueprintAbilityReference>(root));
        detached.AddCachedBlueprint(root.AssetGuid, root);
        detached.AddCachedBlueprint(warm.AssetGuid, warm);
        var resolver = AccessTools.Method(Production("Occultist0099Donors"), "TryExternal");
        bool Registered() => (bool)resolver.Invoke(null, new object[] { detached, row, null });
        Check(!Registered(), "SP08 a reciprocal Fire Shield root with a missing signed Cold variant is not accepted");
        detached.AddCachedBlueprint(cold.AssetGuid, cold);
        Check(Registered(), "SP08 the exact executable Warm and Cold children authorize the existing Fire Shield root");
        cold.GetComponent<AbilityEffectRunAction>().Actions = new Kingmaker.ElementsSystem.ActionList
            { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() };
        AddDanglingProviderVariant(cold);
        Check(!Registered(), "SP08 a Fire Shield child with only a dangling variant rejects the whole optional root");
    }

    private static readonly List<int> EnchantmentSaves = new List<int>();
    private static bool EnchantmentSavePasses;
    private static BlueprintRoot EnchantmentFixtureRoot;

    private static bool EnchantmentRootBoundary(ref BlueprintRoot __result)
    {
        __result = EnchantmentFixtureRoot;
        return false;
    }

    private static bool EnchantmentSaveBoundary(RuleSavingThrow __instance)
    {
        EnchantmentSaves.Add(__instance.DifficultyClass);
        __instance.AutoPass = EnchantmentSavePasses;
        return false;
    }

    private static void EnchantmentNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var bookBlueprint = character.Spellbook;
        var progression = character.Progression;
        var powers = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        var slots = bookBlueprint.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists;
        Check(progression.LevelEntries.SelectMany(entry => entry.Features.Where(f => f == powers).Select(_ => entry.Level))
                  .SequenceEqual(Enumerable.Range(0, 10).Select(i => 2 * i + 1)) &&
              progression.LevelEntries.SelectMany(entry => entry.Features.OfType<BlueprintFeatureSelection>()
                  .Where(selection => selection != powers).Select(_ => entry.Level)).SequenceEqual(new[] { 1, 1, 2, 6, 10, 14, 18 }),
            "ENCH native progression schedules the seven distinct implement choices and ten focus-power choices at the audited levels");
        Check(slots.Length == 65 && slots.Select(slot => slot.List.Guid).Distinct().Count() == slots.Length &&
              slots.GroupBy(slot => slot.School).All(school => school.Count() == 13),
            "HOST all four school and slot alternatives have distinct stable native learning lists");
        var command = Native<BlueprintAbility>("feb70aab86cc17f4bb64432c83737ac2");
        var massDaze = Native<BlueprintAbility>("f48b6e526d69d694507e0d0c4d954eb8");
        var source = slots.Single(slot => slot.SlotKey == "slot-1" && slot.School == "Enchantment").List.Get();
        Check(source.GetSpells(1).Contains(command) && source.GetSpells(4).Contains(massDaze) &&
              !source.GetSpells(1).Any(ability => ability.AssetGuid == P(Occultist0099AbjurationCatalog.MageShield)) &&
              !source.GetSpells(0).Any(),
            "ENCH the private source lists reuse exact Command and Mass Daze roots, with the audited circle and school filtering");

        var unit = Unit("enchantment-spellcaster", character, 7, 16);
        var same = Unit("enchantment-same-race", character, 7, 10);
        var other = Unit("enchantment-other-race", character, 8, 10);
        var skill = new ModifiableValueSkill(unit.Stats, unit.Stats.Charisma, StatType.SkillUseMagicDevice);
        var persuasion = new ModifiableValueSkill(unit.Stats, unit.Stats.Charisma, StatType.SkillPersuasion);
        foreach (var stat in new[] { skill, persuasion })
        {
            unit.Stats.AllStats.Add(stat);
            Set(unit.Stats, stat.Type.ToString(), stat);
        }
        foreach (var stat in new[] { StatType.AdditionalAttackBonus, StatType.AdditionalDamage })
            same.Stats.AllStats.Add(new ModifiableValue(same.Stats, stat));
        var apprentice = Unit("enchantment-umd-progression", character, 2, 16);
        var apprenticeSkill = new ModifiableValueSkill(apprentice.Stats, apprentice.Stats.Charisma, StatType.SkillUseMagicDevice);
        apprentice.Stats.AllStats.Add(apprenticeSkill);
        Set(apprentice.Stats, "SkillUseMagicDevice", apprenticeSkill);
        apprentice.Facts.Add(new Feature(Asset<BlueprintFeature>("MagicItemSkill"), apprentice.Descriptor));
        Eq(1, apprenticeSkill.ModifiedValue, "ENCH Magic Item Skill is +1 when granted at level two");
        apprentice.Progression.GetClassData(character).Level = 4;
        apprentice.Progression.ReapplyFeaturesOnLevelUp();
        Eq(2, apprenticeSkill.ModifiedValue, "ENCH native level-up refresh updates the same Magic Item Skill bonus at level four");
        var race = new BlueprintRace { AssetGuid = P(Occultist0099Ids.Id("HostTestRace")), name = "0099N.TestRace" };
        Set(unit.Progression, "m_Race", race);
        Set(same.Progression, "m_Race", race);
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MagicItemSkill"), unit.Descriptor));
        Eq(3, skill.ModifiedValue, "ENCH Magic Item Skill adds half the actual occultist level to UMD through the native stat");
        var book = unit.Descriptor.DemandSpellbook(bookBlueprint);
        Set(book, "m_BaseLevelInternal", 7);
        var state = LevelState(book);
        var first = state.AddSelection(null, progression, Asset<BlueprintFeatureSelection>("ImplementSelection"), 1);
        var second = state.AddSelection(null, progression, Asset<BlueprintFeatureSelection>("ExtraImplementSelection"), 1);
        var extra = new SelectFeature(second, Asset<BlueprintFeature>("slot-2-Enchantment-ExtraSpells"));
        Check(!extra.Check(state, unit.Descriptor), "ENCH native prerequisites reject the extra spell option before the base school");
        var bindingPower = Asset<BlueprintFeature>("BindingPatternPower");
        var bindingPick = new SelectFeature(state.AddSelection(null, progression, powers, 7), bindingPower);
        Check(!bindingPick.Check(state, unit.Descriptor), "ENCH native focus choice rejects Binding Pattern when its school is missing");
        var choose = new SelectFeature(first, Asset<BlueprintFeature>("slot-1-Enchantment-Implement"));
        Check(choose.Check(state, unit.Descriptor), "ENCH the first school is selectable through the native choice");
        choose.Apply(state, unit.Descriptor);
        var choice = unit.Progression.Features.Enumerable.Single(fact => fact.Blueprint == choose.Item.Feature);
        Check(extra.Check(state, unit.Descriptor) &&
              !new SelectFeature(second, Asset<BlueprintFeature>("slot-2-Enchantment-Implement")).Check(state, unit.Descriptor),
            "ENCH the native choice immediately exposes Extra Spells and excludes a second base grant");
        extra.Apply(state, unit.Descriptor);
        var third = state.AddSelection(null, progression, Asset<BlueprintFeatureSelection>("slot-3-ImplementSelection"), 2);
        Check(!new SelectFeature(third, Asset<BlueprintFeature>("slot-3-Enchantment-ExtraSpells")).Check(state, unit.Descriptor),
            "ENCH only one native Extra Spells pick can be taken per school");
        state.OnApplyAction();
        Check(NativeCheck(new SelectSpell(bookBlueprint, source, 1, command, 0), state, unit.Descriptor),
            "ENCH a real selected implement offers Command to the native spell-learning entry");
        var extraSource = slots.Single(slot => slot.SlotKey == "slot-2" && slot.School == "Enchantment" && slot.ExtraSpells).List.Get();
        var learnExtra = new SelectSpell(bookBlueprint, extraSource, 1, command, 0);
        Check(NativeCheck(learnExtra, state, unit.Descriptor), "ENCH the extra source has a separate native offer");
        learnExtra.Apply(state, unit.Descriptor);
        Check(book.IsKnown(command), "ENCH the extra source learns the root in the private book");
        var focus = unit.Get<Occultist0099FocusPart>().State;
        Check(focus.Schools["Enchantment"].Present && !focus.Schools["Abjuration"].Present,
            "ENCH choosing an implement activates only its school ledger");

        var classData = unit.Progression.GetClassData(character);
        classData.Level = 6;
        Check(!bindingPick.Check(state, unit.Descriptor), "ENCH native focus choice rejects Binding Pattern at real level six");
        classData.Level = 7;
        Check(bindingPick.Check(state, unit.Descriptor), "ENCH native focus choice accepts Binding Pattern at real level seven");
        bindingPick.Apply(state, unit.Descriptor);
        foreach (var power in new[] { Asset<BlueprintFeature>("InspiredAssaultPower"), Asset<BlueprintFeature>("ObeyPower") })
        {
            var pick = new SelectFeature(state.AddSelection(null, progression, powers, 7), power);
            Check(pick.Check(state, unit.Descriptor), "ENCH native focus choice offers " + power.name);
            pick.Apply(state, unit.Descriptor);
        }
        var invest = unit.Abilities.Enumerable.Single(a => a.Blueprint == Asset<BlueprintAbility>("EnchantmentInvestFocusAbility")).Data;
        var lockFocus = unit.Abilities.Enumerable.Single(a => a.Blueprint == Asset<BlueprintAbility>("LockFocusAbility")).Data;
        var cloud = unit.Abilities.Enumerable.Single(a => a.Blueprint == Asset<BlueprintAbility>("CloudMindAbility")).Data;
        var bind = unit.Abilities.Enumerable.Single(a => a.Blueprint == Asset<BlueprintAbility>("BindingPatternAbility")).Data;
        var assault = unit.Abilities.Enumerable.Single(a => a.Blueprint == Asset<BlueprintAbility>("InspiredAssaultAbility")).Data;
        var obey = unit.Abilities.Enumerable.Single(a => a.Blueprint == Asset<BlueprintAbility>("ObeyAbility")).Data;
        var variants = obey.Blueprint.GetComponent<AbilityVariants>().Variants.Select(child => new AbilityData(obey, child)).ToArray();
        Check(variants.Select(variant => variant.Blueprint.AssetGuid).SequenceEqual(new[] { "ObeyHaltAbility", "ObeyFallAbility", "ObeyRunAbility" }
                .Select(token => Asset<BlueprintAbility>(token).AssetGuid)) &&
              variants.All(variant => variant.Blueprint.Parent == obey.Blueprint && variant.Blueprint.CustomRange == new Feet(30)) &&
              command.GetComponent<AbilityVariants>().Variants.Count() == 4 &&
              command.GetComponent<AbilityVariants>().Variants.All(variant => variant.Parent == command),
            "ENCH three private Obey branches preserve all four ordinary Command branches and their native parent");
        Check(variants.All(variant => !((AbilityResourceLogic)variant.ResourceLogic).IsAbilityRestrictionPassed(variant)),
            "ENCH native Obey variants reject spending before focus is locked");

        var saveMethod = AccessTools.Method(typeof(RuleSavingThrow), nameof(RuleSavingThrow.OnTrigger));
        var saveBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(EnchantmentSaveBoundary));
        harmony.Patch(saveMethod, prefix: new HarmonyMethod(saveBoundary));
        applyNativeEffects = true;
        try
        {
            // Executions still dispatch through the real resource adapter and managed actions.
            for (int i = 0; i < 6; i++) Check(CastRule(invest).Success, "ENCH private investment action succeeds before lock");
            Check(CastRule(lockFocus).Success && focus.Schools["Enchantment"].LockedInvestment == 6 &&
                  skill.ModifiedValue == 5 && persuasion.ModifiedValue == 2 && unit.Stats.Charisma.ModifiedValue == 10,
                "ENCH lock adds capped competence to UMD and Persuasion without changing Charisma");
            var presence = unit.Buffs.GetBuff(Asset<BlueprintBuff>("GloriousPresenceBuff"));
            Check(presence != null && presence.Blueprint.GetComponent<CourtBardCharismaCheck01445>().Descriptor == ModifierDescriptor.Competence,
                "ENCH locked resonance attaches the shared native Charisma-check handler with the competence descriptor");
            var charisma = new RuleSkillCheck(unit, StatType.Charisma, 15);
            presence?.CallComponents<CourtBardCharismaCheck01445>(component => component.OnEventAboutToTrigger(charisma));
            Eq(2, charisma.Bonus.ModifiedValue, "ENCH Glorious Presence adds its competence modifier to the Charisma check rather than the ability score");

            RuleCastSpell CastOn(AbilityData spell, UnitEntityData target) => Rulebook.Trigger(new RuleCastSpell(spell, target));
            EnchantmentSavePasses = false;
            var dazed = Native<BlueprintBuff>("9934fedff1b14994ea90205d189c8759");
            var staggered = Native<BlueprintBuff>("df3950af5a783bd4d91ab73eb8fa0fd3");
            var paralysis = Native<BlueprintBuff>("2cfcce5b62d3e6d4082ec31b58468cc8");
            CastOn(cloud, same);
            CastOn(cloud, other);
            Check(same.Buffs.GetBuff(dazed) != null && other.Buffs.GetBuff(staggered) != null &&
                  other.Buffs.GetBuff(dazed) == null && focus.Schools["Enchantment"].Remaining == 4,
                "ENCH two real Cloud Mind casts pay once each and split at equal versus higher Hit Dice");
            var bindingCast = CastOn(bind, same);
            var paralysisBuff = same.Buffs.GetBuff(paralysis);
            Check(bindingCast.Success && paralysisBuff != null && same.Buffs.GetBuff(staggered) != null &&
                  same.State.HasCondition(UnitCondition.Paralyzed) && same.State.HasCondition(UnitCondition.Staggered),
                "ENCH Binding Pattern applies the reused paralysis and stagger on a failed Will save");
            EnchantmentSavePasses = true;
            paralysisBuff?.CallComponents<BuffStatusCondition>(condition => ((Kingmaker.Controllers.Units.ITickEachRound)condition).OnNewRound());
            EnchantmentSavePasses = false;
            Check(same.Buffs.GetBuff(paralysis) == null && same.State.HasCondition(UnitCondition.Staggered) &&
                  !same.State.HasCondition(UnitCondition.Paralyzed),
                "ENCH the reused native re-save releases paralysis and retains stagger for the remaining duration");
            CastOn(assault, same);
            Check(same.Buffs.GetBuff(Asset<BlueprintBuff>("InspiredAssaultBuff")) != null &&
                  same.Stats.GetStat(StatType.AdditionalAttackBonus).ModifiedValue == 2 &&
                  same.Stats.GetStat(StatType.AdditionalDamage).ModifiedValue == 2,
                "ENCH Inspired Assault uses the native attack and damage morale modifiers at the level-six step");
            EnchantmentSaves.Clear();
            CastOn(variants[0], same);
            CastOn(variants[2], other);
            Check(EnchantmentSaves.SequenceEqual(new[] { 18, 16 }) &&
                  same.Buffs.GetBuff(Native<BlueprintBuff>("1baa8a3dffc1f104f9942abe852f631a")) != null &&
                  other.Buffs.GetBuff(Native<BlueprintBuff>("9c8b488120d2b4d4f8243720d754c88e")) != null,
                "ENCH Obey applies each original native action after one Will save, with +2 DC only for the known same race");
            Check(focus.Schools["Enchantment"].Remaining == 0 && unit.Buffs.GetBuff(presence.Blueprint) == presence &&
                  !((AbilityResourceLogic)variants[1].ResourceLogic).IsAbilityRestrictionPassed(variants[1]),
                "ENCH six focus powers exhaust the school, keep the locked resonance, and reject a further Obey variant");
            classData.Level = 20;
            Check(Occultist0099Focus.SetMastery(focus, "Enchantment") == Occultist0099Outcome.Ok && CastOn(assault, same).Success,
                "ENCH the selected mastery state pays a private focus power after the ordinary pool is exhausted");
            var masteredAssault = same.Buffs.GetBuff(Asset<BlueprintBuff>("InspiredAssaultBuff"));
            Check(masteredAssault?.TimeLeft == TimeSpan.FromMinutes(24) &&
                  same.Stats.GetStat(StatType.AdditionalAttackBonus).ModifiedValue == 4,
                "ENCH Inspired Assault preserves the explicit +4 cap and uses effective level 24 for duration");
            Check(CastOn(variants[1], same).Success && same.State.HasCondition(UnitCondition.Prone) &&
                  same.Buffs.GetBuff(Native<BlueprintBuff>("1e9d53aaa310894499ba610f0a2306a3")) != null,
                "ENCH the third Obey branch applies the original native prone effect through the private variant");
        }
        finally
        {
            applyNativeEffects = false;
            EnchantmentSavePasses = false;
            harmony.Unpatch(saveMethod, saveBoundary);
        }
        unit.RemoveFact(choice);
        Check(!focus.Schools["Enchantment"].Present && persuasion.ModifiedValue == 0 && skill.ModifiedValue == 3 &&
              !unit.Abilities.Enumerable.Any(ability => new[] { cloud.Blueprint, bind.Blueprint, assault.Blueprint, obey.Blueprint }.Contains(ability.Blueprint)),
            "ENCH native implement loss withdraws its base and additional abilities and resonance without touching Magic Item Skill");
        Check(!book.IsKnown(command), "ENCH removing the base implement also retires the dependent extra spell");
        unit.Facts.Add(new Feature(choose.Item.Feature, unit.Descriptor));
        state.OnApplyAction();
        Check(unit.Abilities.Enumerable.Any(ability => ability.Blueprint == obey.Blueprint) &&
              !book.IsKnown(command) && NativeCheck(learnExtra, state, unit.Descriptor),
            "ENCH restoring the school also restores the exact existing extra slot and selected powers without relearning the removed root");
        caseStatus["Enchantment"] = "native_progression_choice_focus_and_reused_action_graphs";
    }

    private static bool DispelDiceBoundary(ref RuleRollD20 __result)
    {
        __result = Empty<RuleRollD20>();
        Set(__result, "m_Result", 20);
        return false;
    }

    private static int prophecyRoll;
    private static bool HostilePair(ref bool __result)
    {
        __result = true;
        return false;
    }

    private static bool ProphecyDiceBoundary(ref RuleRollD100 __result)
    {
        __result = Empty<RuleRollD100>();
        Set(__result, "m_Result", prophecyRoll);
        return false;
    }

    private static void DivinationNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("divination-caster", character, 19, 16);
        var recipient = Unit("divination-recipient", character, 4, 10);
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        var perception = new ModifiableValueSkill(unit.Stats, unit.Stats.Wisdom, StatType.SkillPerception);
        unit.Stats.AllStats.Add(perception);
        Set(unit.Stats, "SkillPerception", perception);
        foreach (var caster in new[] { unit, recipient })
        {
            var attack = new ModifiableValue(caster.Stats, StatType.AdditionalAttackBonus);
            caster.Stats.AllStats.Add(attack);
            Set(caster.Stats, "AdditionalAttackBonus", attack);
            foreach (var stat in StatTypeHelper.Skills.Where(stat => caster.Stats.GetStat(stat) == null))
            {
                var skill = new ModifiableValueSkill(caster.Stats, caster.Stats.Wisdom, stat);
                caster.Stats.AllStats.Add(skill);
                Set(caster.Stats, stat.ToString(), skill);
            }
        }
        var armorClass = new ModifiableValueArmorClass(unit.Stats);
        Set(unit.Stats, "AC", armorClass);
        unit.Stats.AllStats.Add(armorClass);

        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Divination-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 19);
        var leveling = LevelState(book);
        var powers = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        foreach (var choice in new[] { (Token: "DivinersFortune", Level: 1), (Token: "DangerSight", Level: 3),
            (Token: "MindEye", Level: 5), (Token: "Prophecy", Level: 9) })
        {
            var selection = leveling.AddSelection(null, character.Progression, powers, choice.Level);
            var pick = new SelectFeature(selection, Asset<BlueprintFeature>(choice.Token + "Power"));
            Check(pick.Check(leveling, unit.Descriptor), "DIV native selection allows " + choice.Token);
            pick.Apply(leveling, unit.Descriptor);
        }
        var list = character.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
            .Single(slot => slot.SlotKey == "slot-1" && slot.School == "Divination").List.Get();
        var divinationRows = ((Occultist0099SpellRoot[])AccessTools.Field(Production("Occultist0099SpellCatalog"), "Roots").GetValue(null))
            .Where(row => row.School == "Divination" && !row.IsKnack).ToArray();
        Check(divinationRows.Length == 8 && divinationRows.All(row => list.GetSpells(row.Circle).Any(spell => spell.AssetGuid == P(row.Guid))) &&
              list.SpellsByLevel.Sum(row => row.Spells.Count()) == 8, "DIV the native learning source publishes exactly the eight audited roots at their Occultist circles");
        var trueStrike = Native<BlueprintAbility>("2c38da66e5a599347ac95b3294acbe00");
        var learn = new SelectSpell(character.Spellbook, list, 1, trueStrike, 0);
        Check(NativeCheck(learn, leveling, unit.Descriptor), "DIV a genuine native Divination source offers True Strike");
        learn.Apply(leveling, unit.Descriptor);
        Check(book.IsKnown(trueStrike), "DIV the selected native root enters the private Occultist book");

        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var pool = focus.Schools["Divination"];
        var dice = AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D100");
        var controlledRoll = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ProphecyDiceBoundary));
        var enemyTest = AccessTools.Method(typeof(UnitEntityData), nameof(UnitEntityData.IsEnemy), new[] { typeof(UnitEntityData) });
        var hostilityBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(HostilePair));
        applyNativeEffects = true;
        harmony.Patch(dice, prefix: new HarmonyMethod(controlledRoll));
        harmony.Patch(enemyTest, prefix: new HarmonyMethod(hostilityBoundary));
        try
        {
            for (int i = 0; i < 15; i++) CastRule(Granted("DivinationInvestFocusAbility"));
            CastRule(Granted("LockFocusAbility"));
            Check(perception.ModifiedValue == 7 && unit.Buffs.GetBuff(Asset<BlueprintBuff>("ThirdEyeBlindsight")) != null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("ThirdEyeBlindsense")) == null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("ThirdEyeSeeInvisibility")) == null &&
                  Asset<BlueprintBuff>("ThirdEyeBuff").StayOnDeath && Asset<BlueprintBuff>("ThirdEyeBlindsight").StayOnDeath,
                "DIV 19/15 locks +7 Perception, native blindsight and an exclusive tier");
            var insight = Granted("SuddenInsightAbility");
            CastRule(insight);
            var insightBuff = unit.Buffs.GetBuff(Asset<BlueprintBuff>("SuddenInsightBuff"));
            var miss = Empty<RuleAttackRoll>();
            var weaponType = new BlueprintWeaponType { Category = WeaponCategory.Longsword };
            var weaponBlueprint = new BlueprintItemWeapon();
            Set(weaponBlueprint, "m_Type", Ref<BlueprintWeaponTypeReference>(weaponType));
            var weapon = Empty<ItemEntityWeapon>();
            Set(weapon, "m_Blueprint", weaponBlueprint);
            var weaponStats = Empty<RuleCalculateWeaponStats>();
            Set(weaponStats, "Weapon", weapon);
            Set(miss, "WeaponStats", weaponStats);
            Set(miss, "Initiator", unit);
            Set(miss, "Target", recipient);
            Set(miss, "Result", AttackResult.Miss);
            Set(miss, "Reason", new RuleReason(insightBuff.Context));
            Check(insightBuff != null && unit.Stats.AdditionalAttackBonus.ModifiedValue == 9 && pool.Remaining == 14,
                "DIV paid Sudden Insight applies the native attack bonus at half the Occultist level");
            insightBuff?.CallComponents<AddInitiatorAttackRollTrigger>(trigger => trigger.OnEventDidTrigger(miss));
            Check(unit.Buffs.GetBuff(Asset<BlueprintBuff>("SuddenInsightBuff")) == null && unit.Stats.AdditionalAttackBonus.ModifiedValue == 0,
                "DIV the first real attack consumes Sudden Insight even when it misses");

            var fortune = Granted("DivinersFortuneAbility");
            skipFortuneDelivery = true;
            CastRule(fortune);
            skipFortuneDelivery = false;
            var held = unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>();
            Check(held?.Ability?.Data.StickyTouch == fortune && pool.Remaining == 13 &&
                unit.Buffs.GetBuff(Asset<BlueprintBuff>("DivinersFortuneBuff")) == null,
                "DIV native sticky touch creates a paid hold while the supplied miss withholds the effect");
            var delivery = held.Ability.Data;
            Rulebook.Trigger(new RuleCastSpell(delivery, recipient));
            var fortuneBuff = recipient.Buffs.GetBuff(Asset<BlueprintBuff>("DivinersFortuneBuff"));
            Check(delivery.Blueprint.GetComponent<AbilityDeliverTouch>() != null && fortuneBuff?.TimeLeft == TimeSpan.FromSeconds(6) &&
                  recipient.Stats.SaveWill.ModifiedValue == 9 && pool.Remaining == 13,
                "DIV the held delivery gives the recipient its original half-level bonus without another charge");
            Set(unit.Descriptor, "Brain", new UnitBrain());
            var combat = Empty<Kingmaker.Controllers.Combat.UnitCombatState>();
            Set(combat, "Unit", unit);
            Set(combat, "m_ManualTarget", recipient);
            Set(unit, "CombatState", combat);
            held.HandleObjectLeaveCombat(unit);
            Check(unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>() == null && combat.ManualTarget == null && pool.Remaining == 13,
                "DIV native combat exit discards the hold and clears its target before bypassing slot refund");

            CastRule(Granted("DangerSightAbility"));
            var defense = unit.Buffs.GetBuff(Asset<BlueprintBuff>("DangerSightBuff"));
            Set(miss, "Initiator", recipient);
            Set(miss, "Target", unit);
            Check(defense?.TimeLeft == TimeSpan.FromMinutes(1) && unit.Stats.SaveReflex.ModifiedValue == 9 && pool.Remaining == 12,
                "DIV Danger Sight arms its shared next-attack-or-save bonus for one minute");
            defense?.CallComponents<AddTargetAttackRollTrigger>(trigger => trigger.OnEventDidTrigger(miss));
            Check(unit.Buffs.GetBuff(Asset<BlueprintBuff>("DangerSightBuff")) == null && unit.Stats.SaveReflex.ModifiedValue == 0,
                "DIV a missed incoming attack removes the entire native Danger Sight benefit");

            var eye = Granted("MindEyeAbility");
            var eyeDonor = Native<BlueprintAbility>("7873f6a427a61105c544507323662f63");
            var spawn = eye.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster>().Single();
            var originalSpawn = eyeDonor.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster>().Single();
            Check(eye.Blueprint.CanTargetPoint && eye.Blueprint.ActionType == UnitCommand.CommandType.Standard && spawn != originalSpawn &&
                  spawn.IsDirectlyControllable && RefGuid(spawn, "m_Blueprint") == RefGuid(originalSpawn, "m_Blueprint") &&
                  eye.ResourceLogic.CalculateCost(eye) == 1, "DIV Mind Eye reuses the genuine CotW Arcane Eye entity and controller with a private focus cost");

            var toggle = unit.ActivatableAbilities.Enumerable.Single(ability => ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("ProphecyAbility"));
            toggle.IsOn = true;
            var dummy = new BlueprintAbility { name = "0099N.EmpowerEligible", AssetGuid = P(Occultist0099Ids.Id("HostProphecySpell")),
                Type = AbilityType.Spell, Range = AbilityRange.Personal, CanTargetSelf = true, AvailableMetamagic = Metamagic.Empower,
                ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation } } };
            dummy.OnEnable();
            var foreignBlueprint = new BlueprintSpellbook { AssetGuid = P(Occultist0099Ids.Id("HostProphecyForeignBook")),
                name = "0099N.ProphecyForeignBook", Spontaneous = true, IsArcane = false, CastingAttribute = StatType.Intelligence,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            Set(foreignBlueprint, "m_SpellList", Ref<BlueprintSpellListReference>(book.Blueprint.SpellList));
            Set(foreignBlueprint, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(book.Blueprint.SpellsPerDay));
            foreignBlueprint.OnEnable();
            var foreign = new AbilityData(dummy, unit.Descriptor.DemandSpellbook(foreignBlueprint), 1);
            var privateSpell = new AbilityData(dummy, book, 1);
            prophecyRoll = 1;
            var excluded = CastRule(foreign);
            Check(excluded.Success && toggle.IsOn && pool.Remaining == 12 && (excluded.Context.Params.Metamagic & Metamagic.Empower) == 0,
                "DIV an Empower-compatible spell in a foreign book does not spend, fizzle or acquire Occultist Empower");
            var ineligible = new BlueprintAbility { name = "0099N.EmpowerIneligible", AssetGuid = P(Occultist0099Ids.Id("HostIneligibleSpell")),
                Type = AbilityType.Spell, Range = AbilityRange.Personal, CanTargetSelf = true, ComponentsArray = Array.Empty<BlueprintComponent>() };
            ineligible.OnEnable();
            CastRule(new AbilityData(ineligible, book, 1));
            Check(toggle.IsOn && pool.Remaining == 12, "DIV a private spell that cannot be empowered does not claim the armed option");
            var failed = CastRule(privateSpell);
            Check(failed.IsSpellFailed && !failed.Success && !toggle.IsOn && pool.Remaining == 11 &&
                  failed.Context.Params.SpellLevel == 1, "DIV a paid prophecy can fizzle and never changes the spell circle");
            toggle.IsOn = true;
            prophecyRoll = 100;
            var success = CastRule(privateSpell);
            Check(success.Success && pool.Remaining == 10 && !toggle.IsOn &&
                  (success.Context.Params.Metamagic & Metamagic.Empower) != 0,
                "DIV a surviving private cast delivers Empower and spends exactly one focus point");
            Check(perception.ModifiedValue == 7, "DIV spending the school changes its balance without reducing locked Third Eye");

            unit.Progression.GetClassData(character).Level = 20;
            Rest(unit);
            for (int i = 0; i < 11; i++) CastRule(Granted("DivinationInvestFocusAbility"));
            Occultist0099Focus.SetMastery(focus, "Divination");
            CastRule(Granted("LockFocusAbility"));
            Check(perception.ModifiedValue == 7 && unit.Buffs.GetBuff(Asset<BlueprintBuff>("ThirdEyeBlindsight")) != null,
                "DIV locked eleven ordinary points plus mastery qualify for the highest Third Eye tier");
            var copied = TransferRespecSurvivors(unit);
            SignalRespecCommitted(unit);
            Check(copied.State.MasterySchool == null && perception.ModifiedValue == 5 &&
                unit.Buffs.GetBuff(Asset<BlueprintBuff>("ThirdEyeBlindsight")) == null &&
                unit.Buffs.GetBuff(Asset<BlueprintBuff>("ThirdEyeSeeInvisibility")) != null,
                "DIV actual respec withdrawal drops its obsolete mastery-only resonance while preserving ordinary investment");
        }
        finally
        {
            applyNativeEffects = false;
            skipFortuneDelivery = false;
            harmony.Unpatch(dice, controlledRoll);
            harmony.Unpatch(enemyTest, hostilityBoundary);
        }
        unit.RemoveFact(implement);
        Check(unit.Buffs.GetBuff(Asset<BlueprintBuff>("ThirdEyeBlindsight")) == null && perception.ModifiedValue == 0,
            "DIV removing the native source withdraws both the perception and sense resonance");
        caseStatus["Divination"] = "native_choice_learning_third_eye_roll_benefits_and_paid_prophecy";

        BlueprintGuid RefGuid(object target, string field) => ((BlueprintReferenceBase)Get(target, field)).Guid;
    }

    private static int distortionRoll, distortionRolls, shadowSpawnCalls;

    private static bool IllusionRollBoundary(RuleRollDice __instance)
    {
        if (!(__instance is RuleRollD100)) return true;
        Set(__instance, "m_Result", distortionRoll);
        distortionRolls++;
        return false;
    }

    private static bool ShadowSpawnBoundary()
    {
        shadowSpawnCalls++;
        return false;
    }

    private static void IllusionNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("illusion-caster", character, 9, 16);
        var sameLevel = Unit("illusion-equal-hd", character, 9, 12);
        var higher = Unit("illusion-higher-hd", character, 10, 12);
        foreach (var actor in new[] { unit, sameLevel, higher })
        {
            actor.SetStealthData(new UnitStealth());
            var stealth = new ModifiableValueSkill(actor.Stats, actor.Stats.Dexterity, StatType.SkillStealth);
            var perception = new ModifiableValueSkill(actor.Stats, actor.Stats.Wisdom, StatType.SkillPerception);
            var attack = new ModifiableValue(actor.Stats, StatType.AdditionalAttackBonus);
            actor.Stats.AllStats.AddRange(new ModifiableValue[] { stealth, perception, attack });
            Set(actor.Stats, "SkillStealth", stealth);
            Set(actor.Stats, "SkillPerception", perception);
            Set(actor.Stats, "AdditionalAttackBonus", attack);
        }
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Illusion-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 9);
        var leveling = LevelState(book);
        var powers = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        foreach (var option in new[] { (Token: "Terror", Level: 1), (Token: "Unseen", Level: 7),
            (Token: "BedevelingAura", Level: 9), (Token: "ShadowBeasts", Level: 9) })
        {
            var selection = leveling.AddSelection(null, character.Progression, powers, option.Level);
            var pick = new SelectFeature(selection, Asset<BlueprintFeature>(option.Token + "Power"));
            Check(pick.Check(leveling, unit.Descriptor), "ILL native selection admits " + option.Token);
            pick.Apply(leveling, unit.Descriptor);
        }
        var list = character.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
            .Single(slot => slot.SlotKey == "slot-1" && slot.School == "Illusion").List.Get();
        var illusionRows = ((Occultist0099SpellRoot[])AccessTools.Field(Production("Occultist0099SpellCatalog"), "Roots").GetValue(null))
            .Where(row => row.School == "Illusion" && !row.IsKnack).ToArray();
        Check(illusionRows.Length == 8 && illusionRows.All(row => list.GetSpells(row.Circle).Any(spell => spell.AssetGuid == P(row.Guid))) &&
              list.SpellsByLevel.Sum(row => row.Spells.Count()) == 8, "ILL all eight learning candidates retain their signed native root and circle");
        var vanish = Native<BlueprintAbility>("f001c73999fb5a543a199f890108d936");
        var learn = new SelectSpell(character.Spellbook, list, 1, vanish, 0);
        Check(NativeCheck(learn, leveling, unit.Descriptor), "ILL native Illusion source offers Vanish");
        learn.Apply(leveling, unit.Descriptor);
        Check(book.IsKnown(vanish), "ILL the selected root enters the private Occultist book");

        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var pool = focus.Schools["Illusion"];
        var d100 = AccessTools.Method(typeof(RuleRollDice), nameof(RuleRollDice.OnTrigger));
        var fixedRoll = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(IllusionRollBoundary));
        var spawn = AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster), "RunAction");
        var countSpawn = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ShadowSpawnBoundary));
        var activateArea = AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect), "OnActivate");
        var noScene = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoReplenish));
        applyNativeEffects = true;
        harmony.Patch(d100, prefix: new HarmonyMethod(fixedRoll));
        harmony.Patch(spawn, prefix: new HarmonyMethod(countSpawn));
        harmony.Patch(activateArea, prefix: new HarmonyMethod(noScene));
        try
        {
            for (int i = 0; i < 11; i++) CastRule(Granted("IllusionInvestFocusAbility"));
            CastRule(Granted("LockFocusAbility"));
            var distortion = Asset<BlueprintBuff>("DistortionBuff");
            Check(unit.Buffs.GetBuff(distortion) != null && distortion.StayOnDeath,
                "ILL locking the school establishes the native permanent Distortion buff");
            var attack = Empty<RuleAttackRoll>();
            Set(attack, "Initiator", sameLevel);
            Set(attack, "Target", unit);
            Set(attack, "AttackType", AttackType.Melee);
            Set(attack, "IgnoreConcealment", true);
            Set(attack, "missChance", new MissChance());
            var higherPassedRoll = Empty<RuleRollD100>();
            Set(higherPassedRoll, "m_Result", 80);
            Set(attack, "MissChanceValue", 20);
            Set(attack, "MissChanceRoll", higherPassedRoll);
            var checkMiss = AccessTools.Method(typeof(RuleAttackRoll), "TryOvercomeTargetConcealmentAndMissChance");
            distortionRoll = 9;
            Check(!(bool)checkMiss.Invoke(attack, null) && attack.MissChanceValue == 9 && distortionRolls == 1,
                "ILL capped Distortion rolls separately and reports the actual failing roll ahead of a higher passed chance");
            sameLevel.State.AddCondition(UnitCondition.SeeInvisibility);
            Check((bool)checkMiss.Invoke(attack, null) && distortionRolls == 1, "ILL native See Invisibility bypasses Distortion without rolling");
            sameLevel.State.RemoveCondition(UnitCondition.SeeInvisibility);
            sameLevel.State.AddCondition(UnitCondition.TrueSeeing);
            Check((bool)checkMiss.Invoke(attack, null) && distortionRolls == 1, "ILL native True Seeing also bypasses it");
            sameLevel.State.RemoveCondition(UnitCondition.TrueSeeing);

            var beam = Granted("ColorBeamAbility");
            Rulebook.Trigger(new RuleCastSpell(beam, sameLevel));
            Rulebook.Trigger(new RuleCastSpell(beam, higher));
            Check(beam.Blueprint.GetComponent<AbilityDeliverProjectile>() != null && beam.Blueprint.CustomRange.Value == 30 &&
                sameLevel.Buffs.GetBuff(Native<BlueprintBuff>("187f88d96a0ef464280706b63635f2af"))?.TimeLeft == TimeSpan.FromSeconds(6) &&
                higher.Buffs.GetBuff(Native<BlueprintBuff>("df6d1025da07524429afbae248845ecc"))?.TimeLeft == TimeSpan.FromSeconds(6) &&
                pool.Remaining == 9, "ILL the original ray graph blinds at exactly the Occultist level and dazzles above it");
            var terror = Granted("TerrorAbility");
            var terrorDelivery = terror.Blueprint.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility;
            Check(terrorDelivery.GetComponent<AbilityDeliverTouch>() != null &&
                terrorDelivery.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single() is PhantasmTerror01447 action &&
                action.AlliesOnly && action.SharedLevel != null && terror.ResourceLogic.CalculateCost(terror) == 1 &&
                terrorDelivery.GetComponent<AbilityResourceLogic>() == null,
                "ILL Terror reuses the allied opportunity action and pays only on the held-touch root");

            var nativeInvisibility = Native<BlueprintBuff>("e6b35473a237a6045969253beb09777c");
            var unseen = Granted("UnseenAbility");
            var unseenLeaves = unseen.GetConversions().ToArray();
            CastRule(unseenLeaves.Single(variant => variant.Blueprint == Asset<BlueprintAbility>("UnseenSelfAbility")));
            Rulebook.Trigger(new RuleCastSpell(unseenLeaves.Single(variant => variant.Blueprint == Asset<BlueprintAbility>("UnseenAllyAbility")), sameLevel));
            Check(unit.Buffs.GetBuff(nativeInvisibility)?.TimeLeft == TimeSpan.FromMinutes(1) &&
                sameLevel.Buffs.GetBuff(nativeInvisibility)?.TimeLeft == TimeSpan.FromMinutes(1) &&
                sameLevel.State.HasCondition(UnitCondition.GreaterInvisibility) && pool.Remaining == 6,
                "ILL native Greater Invisibility is reused for one minute with one-point self and two-point ally variants");

            var aura = Granted("BedevelingAuraAbility");
            CastRule(aura);
            var emitter = unit.Buffs.GetBuff(Asset<BlueprintBuff>("BedevelingAuraBuff"));
            var area = emitter.Blueprint.GetComponent<Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect>().AreaEffect;
            var areaBuff = area.GetComponent<AbilityAreaEffectBuff>();
            Check(emitter.TimeLeft == TimeSpan.FromSeconds(24) && area.Size.Value == 30 && area.AffectEnemies &&
                areaBuff.CheckConditionEveryRound && areaBuff.Buff.GetComponent<PhantasmFlanked01447>() != null && pool.Remaining == 5,
                "ILL paid Bedeveling Aura uses the shared Phantasm enemy aura for half the native Occultist level");
            var firstAura = higher.Buffs.AddBuff(areaBuff.Buff, unit, (TimeSpan?)null);
            var secondAura = higher.Buffs.AddBuff(areaBuff.Buff, sameLevel, (TimeSpan?)null);
            firstAura.SourceAreaEffectId = "0099N.aura.1";
            secondAura.SourceAreaEffectId = "0099N.aura.2";
            firstAura.Remove();
            Check(firstAura != secondAura && higher.Buffs.GetBuff(areaBuff.Buff) == secondAura &&
                higher.State.HasCondition(UnitCondition.Slowed), "ILL an overlapping aura keeps its independent source after the other ends");

            var shadows = Granted("ShadowBeastsAbility");
            var summonChoices = shadows.GetConversions().ToArray();
            var classData = unit.Progression.GetClassData(character);
            foreach (var tier in new[] { (Level: 9, Visible: 24), (Level: 11, Visible: 31), (Level: 13, Visible: 38),
                (Level: 15, Visible: 45), (Level: 17, Visible: 48) })
            {
                classData.Level = tier.Level;
                Eq(tier.Visible, summonChoices.Count(variant => variant.IsVisible()), "ILL native menu filters the signed summon family at level " + tier.Level);
            }
            classData.Level = 9;
            Check(summonChoices.Length == 48 && summonChoices.All(variant => ReferenceEquals(variant.ConvertedFrom, shadows) &&
                variant.ShadowSpellSettings?.Factor?.GetInt(unit) == 60 && !variant.RequireFullRoundAction),
                "ILL all 48 direct native conversions inherit 60% reality and remain standard actions");
            var ninth = summonChoices.First(variant => variant.Blueprint.GetComponent<Occultist0099ShadowTier>().Minimum == 17);
            Check(Rulebook.Trigger(new RuleCastSpell(ninth, new TargetWrapper(UnityEngine.Vector3.zero))).ForceFail && pool.Remaining == 5,
                "ILL a directly submitted higher-tier native variant cannot bypass the UI level restriction or debit focus");
            var lowest = summonChoices.Single(variant => variant.Blueprint == Asset<BlueprintAbility>("ShadowBeasts-8fd74eddd9b6c224693d9ab241f25e84"));
            var cast = Rulebook.Trigger(new RuleCastSpell(lowest, new TargetWrapper(UnityEngine.Vector3.zero)));
            Check(cast.Success && pool.Remaining == 4 && cast.Context.ShadowFactorPercents == 60 &&
                cast.Context.Params.CasterLevel == 9 && cast.Context.Params.DC == 17 && shadowSpawnCalls == 1,
                "ILL the paid native cast supplies 60% shadow reality and the signed Occultist CL/DC to the existing summon action");

            var shadow = Unit("shadow-health-target", character, 1, 10);
            var hp = shadow.Stats.HitPoints;
            hp.BaseValue = 99; // Native level-one HP adds one to this fixture's base.
            var healthBlueprint = Asset<BlueprintBuff>("ShadowBeastsHealth");
            var shadowAction = new Occultist0099ShadowHealthAction { Effect = Ref<BlueprintBuffReference>(healthBlueprint) };
            using (cast.Context.GetDataScope(shadow)) shadowAction.RunAction();
            var health = shadow.Buffs.GetBuff(healthBlueprint);
            Eq(60, hp.ModifiedValue, "ILL a private shadow beast gets 60 percent of maximum HP using a native modifier");
            Check(health != null && health.IsNotDispelable && !health.Context.IsShadow,
                "ILL the inherent HP adjustment cannot be rejected by the stock shadow disbelief gate");
            health.Remove();
            Eq(100, hp.ModifiedValue, "ILL removing the shadow health modifier restores the baseline exactly");
        }
        finally
        {
            applyNativeEffects = false;
            harmony.Unpatch(d100, fixedRoll);
            harmony.Unpatch(spawn, countSpawn);
            harmony.Unpatch(activateArea, noScene);
        }
        unit.RemoveFact(implement);
        Check(unit.Buffs.GetBuff(Asset<BlueprintBuff>("DistortionBuff")) == null &&
            unit.Buffs.GetBuff(Asset<BlueprintBuff>("BedevelingAuraBuff")) == null,
            "ILL removing the school withdraws the passive miss chance and active emitter");
        caseStatus["Illusion"] = "native_learning_private_miss_roll_paid_variants_and_shadow_context";
    }

    private static void TransmutationNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("transmutation-occultist", character, 12, 16);
        var body = Empty<UnitBody>();
        Set(body, "Owner", unit.Descriptor);
        Set(body, "m_HandsEquipmentSets", new[] { new HandsEquipmentSet(unit.Descriptor) });
        Set(unit.Descriptor, "Body", body);
        Set(unit, "IsTurnedOn", true);
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        var speed = new ModifiableValueSpeed(unit.Stats);
        Set(speed, "m_BaseValue", 30);
        Set(speed, "ModifiedValue", 30);
        Set(unit.Stats, "Speed", speed);
        var attack = new ModifiableValue(unit.Stats, StatType.AdditionalAttackBonus);
        Set(unit.Stats, "AdditionalAttackBonus", attack);
        var ac = new ModifiableValueArmorClass(unit.Stats);
        Set(unit.Stats, "AC", ac);
        unit.Stats.AllStats.AddRange(new ModifiableValue[] { speed, attack, ac });
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Transmutation-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 12);
        var leveling = LevelState(book);
        foreach (string token in new[] { "SuddenSpeed", "SizeAlteration", "PhilosophersTouch", "Quickness", "MindOverGravity" })
        {
            var selection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("FocusPowerSelection"), 1);
            var pick = new SelectFeature(selection, Asset<BlueprintFeature>(token + "Power"));
            Check(pick.Check(leveling, unit.Descriptor), "TRAN native selection admits " + token);
            pick.Apply(leveling, unit.Descriptor);
        }
        var list = character.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
            .Single(slot => slot.SlotKey == "slot-1" && slot.School == "Transmutation").List.Get();
        const string keenEdgeId = "64923c9c808143c7ab7adba5a27f0ca4";
        var rows = ((Occultist0099SpellRoot[])AccessTools.Field(Production("Occultist0099SpellCatalog"), "Roots").GetValue(null))
            .Where(row => row.School == "Transmutation" && !row.IsKnack && row.Source != "External").ToArray();
        bool Listed(Occultist0099SpellRoot row) => list.GetSpells(row.Circle).Any(spell => spell.AssetGuid == P(row.Guid));
        Check(rows.Where(row => row.Guid != keenEdgeId).All(Listed) && list.SpellsByLevel.Sum(row => row.Spells.Count()) == rows.Length - 1,
            "TRAN native and existing CotW learning entries keep their signed school and circles, with unowned DLC and absent external spells excluded");
        var reward = Native<BlueprintDlcReward>("b94f823171a84e30ad7a1b892433ab5d");
        var register = AccessTools.Method(Production("Occultist0099NativeRegistration"), "RegisterInDetachedCache");
        Set(reward, "m_IsAvailable", (bool?)true);
        Check((bool)register.Invoke(null, new object[] { detached }) && rows.All(Listed), "TRAN native DLC ownership restores Keen Edge at its signed circle");
        Set(reward, "m_IsAvailable", (bool?)false);
        Check((bool)register.Invoke(null, new object[] { detached }) && !Listed(rows.Single(row => row.Guid == keenEdgeId)),
            "TRAN removing DLC ownership withdraws only the gated candidate");
        var enlargeSpell = Native<BlueprintAbility>("c60969e7f264e6d4b84a1499fdcf9039");
        var learn = new SelectSpell(character.Spellbook, list, 1, enlargeSpell, 0);
        Check(NativeCheck(learn, leveling, unit.Descriptor), "TRAN native implement offers Enlarge Person");
        learn.Apply(leveling, unit.Descriptor);
        Check(book.IsKnown(enlargeSpell), "TRAN reused Enlarge Person enters the private spellbook");

        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(grant => grant.Blueprint == Asset<BlueprintAbility>(token)).Data;
        AbilityData Variant(string root, string token) => new AbilityData(Granted(root), Asset<BlueprintAbility>(token));
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var pool = focus.Schools["Transmutation"];
        var dexterity = Variant("PhysicalEnhancementAbility", "PhysicalEnhancementDexterityAbility");
        var strength = Variant("PhysicalEnhancementAbility", "PhysicalEnhancementStrengthAbility");
        var standard = Variant("LegacyWeaponAbility", "LegacyWeaponStandardAbility");
        var swift = Variant("LegacyWeaponAbility", "LegacyWeaponSwiftAbility");
        var philo = Granted("PhilosophersTouchAbility");
        var touch = Variant("PhilosophersTouchAbility", "PhilosophersTouchColdIronTouch");
        var silver = Variant("PhilosophersTouchAbility", "PhilosophersTouchSilverBurst");
        var adamantine = Variant("PhilosophersTouchAbility", "PhilosophersTouchAdamantineBurst");
        var ward = Asset<BlueprintBuff>("LegacyWeaponBuff");
        var level = unit.Progression.GetClassData(character);
        int Reflex() => unit.Stats.SaveReflex.ModifiedValue;
        int Movement() => unit.Stats.Speed.ModifiedValue;

        var service = AccessTools.PropertyGetter(typeof(EntityService), nameof(EntityService.Instance));
        var serviceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ArmorEntityServiceBoundary));
        var dollRoom = AccessTools.PropertyGetter(typeof(ItemEnchantment), "WielderVisibleInInventory");
        var skipDollRoom = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ArmorDollRoomBoundary));
        armorFixtureEntities = new EntityService();
        harmony.Patch(service, prefix: new HarmonyMethod(serviceBoundary));
        harmony.Patch(dollRoom, prefix: new HarmonyMethod(skipDollRoom));
        applyNativeEffects = true;
        try
        {
            var club = Native<BlueprintItemWeapon>("07863393521453a4da9e98626531eb5f").CreateEntity<ItemEntityWeapon>();
            var sword = Native<BlueprintItemWeapon>("6fd0a849531617844b195f452661b2cd").CreateEntity<ItemEntityWeapon>();
            Equip(club);
            for (int i = 0; i < 12; i++) CastRule(Granted("TransmutationInvestFocusAbility"));
            Check(CastRule(dexterity).Success && unit.Stats.Dexterity.ModifiedValue == 10, "TRAN physical choice applies only after lock");
            Check(CastRule(Granted("LockFocusAbility")).Success && unit.Stats.Dexterity.ModifiedValue == 16,
                "TRAN locked level 12 physical enhancement adds six points to the chosen stat");
            Check(CastRule(strength).ForceFail && unit.Stats.Strength.ModifiedValue == 10 && pool.Remaining == 12,
                "TRAN the native physical selector cannot rewrite a locked snapshot");

            int baseSpeed = Movement();
            var sudden = Granted("SuddenSpeedAbility");
            Check(CastRule(sudden).Success && sudden.Blueprint.ActionType == UnitCommand.CommandType.Swift &&
                Movement() == baseSpeed + 30 && unit.Buffs.GetBuff(Native<BlueprintBuff>("9ea4ec3dc30cd7940a372a4d699032e7"))?.TimeLeft == TimeSpan.FromMinutes(1),
                "TRAN Sudden Speed uses the native movement buff as a one-minute swift power");
            unit.Buffs.GetBuff(Native<BlueprintBuff>("9ea4ec3dc30cd7940a372a4d699032e7"))?.Remove();
            var flight = Granted("MindOverGravityAbility");
            Check(CastRule(flight).Success && Movement() == baseSpeed + 10 &&
                unit.Buffs.GetBuff(Asset<BlueprintBuff>("MindOverGravityBuff"))?.TimeLeft == TimeSpan.FromMinutes(12),
                "TRAN Mind Over Gravity uses the released flight mechanics for one minute per class level");
            unit.Buffs.GetBuff(Asset<BlueprintBuff>("MindOverGravityBuff"))?.Remove();

            int baseReflex = Reflex(), baseAttack = attack.ModifiedValue, baseAc = ac.ModifiedValue;
            var haste = Native<BlueprintBuff>("17113651fa8b40c2a7bbf9721462e646");
            var stockHaste = unit.Buffs.AddBuff(haste, unit, TimeSpan.FromMinutes(1));
            var quickness = Granted("QuicknessAbility");
            Check(CastRule(quickness).Success && unit.Buffs.GetBuff(Asset<BlueprintBuff>("QuicknessBuff"))?.TimeLeft == TimeSpan.FromSeconds(72) &&
                Reflex() == baseReflex + 3 && attack.ModifiedValue == baseAttack + 3 && ac.ModifiedValue == baseAc + 3 && Movement() == baseSpeed + 30,
                "TRAN Quickness combines the native haste benefit and two extra bonuses without duplicating the base modifiers");
            stockHaste.Remove();
            Check(Reflex() == baseReflex + 3 && attack.ModifiedValue == baseAttack + 3 && ac.ModifiedValue == baseAc + 3,
                "TRAN Quickness retains its own full effect when overlapping Haste expires");
            var slowSpell = Native<BlueprintAbility>("f492622e473d34747806bdb39356eb89");
            var slowBuff = Native<BlueprintBuff>("0bc608c3f2b548b44b7146b7530613ac");
            var slowContext = new AbilityExecutionContext(GrantAbility(unit, slowSpell), AbilityParams.Empty, new TargetWrapper(unit));
            var counter = Rulebook.Trigger(new RuleCanApplyBuff(unit, slowBuff, slowContext, TimeSpan.FromMinutes(1)));
            Check(!counter.CanApply && unit.Buffs.GetBuff(Asset<BlueprintBuff>("QuicknessBuff")) == null && Reflex() == baseReflex,
                "TRAN the failed-save native Slow application consumes Quickness and no Slow is applied");

            var enlarge = Variant("SizeAlterationAbility", "EnlargeAlterationAbility");
            var reduce = Variant("SizeAlterationAbility", "ReduceAlterationAbility");
            Check(enlarge.Blueprint.Range == AbilityRange.Touch && reduce.Blueprint.Range == AbilityRange.Touch &&
                enlarge.Blueprint.CanTargetFriends && reduce.Blueprint.CanTargetFriends &&
                enlarge.Blueprint.GetComponent<Kingmaker.UnitLogic.Abilities.Components.TargetCheckers.AbilityTargetHasFact>() == null &&
                reduce.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single() is Conditional normalReduce &&
                normalReduce.IfTrue.Actions.Single() is Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff,
                "TRAN Size Alteration drops only the creature-type restriction while retaining the stock counter-buff action");

            level.Level = 7;
            Check(philo.GetConversions().Any(option => option.Blueprint == touch.Blueprint && option.IsVisible()) &&
                  !philo.GetConversions().Any(option => option.Blueprint == silver.Blueprint && option.IsVisible()) && CastRule(touch).Success &&
                  club.Enchantments.Any(e => e.Blueprint == Native<BlueprintWeaponEnchantment>("e5990dc76d2a613409916071c898eee8")),
                "TRAN low-level Philosopher's Touch applies the stock cold iron property to the selected main hand");
            Equip(sword);
            Check(!club.Enchantments.Any() && sword.Enchantments.Any(e => e.Blueprint == Native<BlueprintWeaponEnchantment>("e5990dc76d2a613409916071c898eee8")),
                "TRAN Philosopher's Touch moves only its own enchantment when the weapon changes");
            level.Level = 8;
            int prior = pool.Remaining;
            Check(CastRule(touch).ForceFail && CastRule(adamantine).ForceFail && pool.Remaining == prior &&
                  silver.Blueprint.GetComponent<AbilityTargetsAround>() != null,
                "TRAN level 8 switches to the native burst shape and rejects the old touch and early adamantine variants before payment");
            level.Level = 12;
            Check(CastRule(silver).Success && sword.Enchantments.Any(e => e.Blueprint == Native<BlueprintWeaponEnchantment>("0ae8fc9f2e255584faf4d14835224875")) &&
                  !sword.Enchantments.Any(e => e.Blueprint == Native<BlueprintWeaponEnchantment>("e5990dc76d2a613409916071c898eee8")),
                "TRAN Philosopher's Touch replaces its previous material with the native silver property");

            var disruption = unit.ActivatableAbilities.Enumerable.Single(item => item.Blueprint == Asset<BlueprintActivatableAbility>("LegacyWeaponDisruptionChoice"));
            Check(!disruption.IsAvailable, "TRAN Disruption rejects a slashing weapon");
            Equip(club);
            Check(disruption.IsAvailable, "TRAN Disruption admits a bludgeoning melee weapon");
            disruption.IsOn = true;
            prior = pool.Remaining;
            var numericOne = Native<BlueprintWeaponEnchantment>("d704f90f54f813043a525f304f6c0050");
            var otherEnhancement = club.AddEnchantment(numericOne, null);
            Check(CastRule(standard).Success && pool.Remaining == prior - 1 && Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(club) == 2 &&
                  club.Enchantments.Any(e => e.Blueprint == Native<BlueprintWeaponEnchantment>("0f20d79b7049c0f4ca54ca3d1ea44baa")) &&
                  unit.Buffs.GetBuff(ward)?.TimeLeft == TimeSpan.FromMinutes(1) && body.PrimaryHand.Lock.Count == 1,
                "TRAN Legacy Weapon buys native Disruption and +1 alongside another source's identical +1 temporary enchantment");
            Check(CastRule(swift).Success && pool.Remaining == prior - 3 && body.PrimaryHand.Lock.Count == 1 &&
                club.Enchantments.Count(e => e.Blueprint == Native<BlueprintWeaponEnchantment>("0f20d79b7049c0f4ca54ca3d1ea44baa")) == 1 &&
                club.Enchantments.Count(e => e.Blueprint == numericOne) == 2 && club.Enchantments.Contains(otherEnhancement),
                "TRAN the two-point swift recast retains one lock and one copy of each of its own properties alongside the other source");
            club.RemoveEnchantment(otherEnhancement);
            level.Level = 6;
            Check(CastRule(standard).ForceFail && pool.Remaining == prior - 3 && unit.Buffs.GetBuff(ward) != null,
                "TRAN the older instance cannot supply the +1 baseline for a special-only replacement");
            level.Level = 12;
            disruption.IsOn = false;
            Equip(null);
            Set(body, "m_EmptyHandWeapon", club);
            Check(CastRule(standard).Success && body.PrimaryHand.Lock.Count == 1 &&
                  Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(club) == 3,
                "TRAN Legacy Weapon can snapshot the current empty-hand weapon");
            Set(body, "m_EmptyHandWeapon", sword);
            EventBus.RaiseEvent(unit, (IUnitEmptyHandWeaponHandler handler) => handler.HandleUnitEmptyHandWeaponUpdated());
            Check(unit.Buffs.GetBuff(ward) == null && body.PrimaryHand.Lock.Count == 0 &&
                  Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(club) == 0,
                "TRAN the native empty-hand change event releases the exact old weapon and lock");
            Equip(club);
            Check(CastRule(standard).Success, "TRAN Legacy Weapon can be applied again after a weapon swap");
            unit.RemoveFact(implement);
            Check(unit.Buffs.GetBuff(ward) == null && body.PrimaryHand.Lock.Count == 0 && unit.Stats.Dexterity.ModifiedValue == 10 &&
                  Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(club) == 0,
                "TRAN removing the implement retracts physical enhancement and its exact Legacy Weapon effects");
        }
        finally
        {
            applyNativeEffects = false;
            harmony.Unpatch(dollRoom, skipDollRoom);
            harmony.Unpatch(service, serviceBoundary);
        }
        caseStatus["Transmutation"] = "native_private_learning_lock_haste_flight_material_and_weapon_enchants";

        void Equip(ItemEntityWeapon item)
        {
            var hand = body.PrimaryHand;
            var previous = hand.MaybeWeapon;
            if (previous != null)
            {
                previous.Facts.TurnOff();
                previous.HoldingSlot = null;
                Set(previous, "m_WielderRef", default(EntityRef<UnitEntityData>));
            }
            hand.SetFakeItem(item);
            if (item != null)
            {
                item.HoldingSlot = hand;
                Set(item, "m_WielderRef", (EntityRef<UnitEntityData>)unit);
                item.Facts.TurnOn();
            }
            EventBus.RaiseEvent((IUnitEquipmentHandler handler) => handler.HandleEquipmentSlotUpdated(hand, previous));
        }
    }

    private static void AegisNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("aegis-wearer", character, 12, 16);
        var enemy = Unit("aegis-melee-attacker", character, 6, 10);
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        var body = Empty<UnitBody>();
        Set(body, "Owner", unit.Descriptor);
        var armorSlot = new ArmorSlot(unit.Descriptor);
        Set(body, "Armor", armorSlot);
        Set(unit.Descriptor, "Body", body);
        Set(unit, "IsTurnedOn", true);
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Abjuration-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var classBook = unit.Descriptor.DemandSpellbook(character.Spellbook);
        var state = LevelState(classBook);
        var selection = state.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("FocusPowerSelection"), 1);
        var pick = new SelectFeature(selection, Asset<BlueprintFeature>("AegisPower"));
        Check(pick.Check(state, unit.Descriptor), "AEGIS Aegis is a first-level native Focus Power choice for an Abjuration implement");
        pick.Apply(state, unit.Descriptor);
        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(grant => grant.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var aegis = Granted("AegisAbility");
        var standard = new AbilityData(aegis, Asset<BlueprintAbility>("AegisStandardAbility"));
        var swift = new AbilityData(aegis, Asset<BlueprintAbility>("AegisSwiftAbility"));
        var ward = Asset<BlueprintBuff>("AegisArmorBuff");
        var pool = unit.Get<Occultist0099FocusPart>().State.Schools["Abjuration"];
        var numeric2 = Native<BlueprintArmorEnchantment>("758b77a97640fd747abf149f5bf538d0");
        var fireHeavy = Native<BlueprintArmorEnchantment>("e7af6912cc308df4e9ee63c8824f2738");
        var armorBlueprint = Native<BlueprintItemArmor>("c65f6fc979d5556489b20e478189cbdd");
        armorFixtureEntities = new EntityService();
        var service = AccessTools.PropertyGetter(typeof(EntityService), nameof(EntityService.Instance));
        var serviceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ArmorEntityServiceBoundary));
        harmony.Patch(service, prefix: new HarmonyMethod(serviceBoundary));
        ItemEntityArmor armor, identical;
        try
        {
            armor = armorBlueprint.CreateEntity<ItemEntityArmor>();
            identical = armorBlueprint.CreateEntity<ItemEntityArmor>();
        }
        finally { harmony.Unpatch(service, serviceBoundary); }
        var baseline = armor.AddEnchantment(numeric2, null);
        var level = unit.Progression.GetClassData(character);
        var budget = AccessTools.Method(Production("Occultist0099Aegis"), "Budget");
        int Points() => (int)budget.Invoke(null, new object[] { unit });
        foreach (var pair in new[] { (Level: 1, Points: 1), (Level: 6, Points: 2), (Level: 12, Points: 3), (Level: 18, Points: 4) })
        {
            level.Level = pair.Level;
            Eq(pair.Points, Points(), "AEGIS native actual class level determines enhancement budget");
        }
        level.Level = 20;
        var focus = unit.Get<Occultist0099FocusPart>().State;
        Occultist0099Focus.SetMastery(focus, "Abjuration");
        Eq(5, Points(), "AEGIS own mastery unlocks the dedicated fifth tier");
        focus.MasterySchool = null;
        level.Level = 12;
        Check(standard.Blueprint.ActionType == UnitCommand.CommandType.Standard && swift.Blueprint.ActionType == UnitCommand.CommandType.Swift &&
              standard.ResourceLogic.CalculateCost(standard) == 1 && swift.ResourceLogic.CalculateCost(swift) == 2,
            "AEGIS original Standard and Swift activation variants cost one and two");
        var enchantRefresh = AccessTools.Method(typeof(ItemEntityArmor), nameof(ItemEntityArmor.RecalculateStats));
        var skipVisualStats = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoReplenish));
        var enchantDollRoom = AccessTools.PropertyGetter(typeof(ItemEnchantment), "WielderVisibleInInventory");
        var skipDollRoom = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ArmorDollRoomBoundary));
        var applyBuffRule = AccessTools.Method(typeof(RuleCanApplyBuff), nameof(RuleCanApplyBuff.OnTrigger));
        var rejectWard = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(RejectAegisWard));
        var attachEnchantment = AccessTools.Method(typeof(ItemEntity), "AddEnchantmentInternal",
            new[] { typeof(BlueprintItemEnchantment), typeof(MechanicsContext), typeof(Rounds?) });
        var failAttachment = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(FailAegisAttachment));
        var loggerInstance = AccessTools.PropertyGetter(typeof(Owlcat.Runtime.Core.Logging.Logger), "Instance");
        var loggerLog = AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.Logger), "Log");
        var offlineLogger = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(AegisLoggerInstanceBoundary));
        var expectedError = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ExpectedAegisFaultLog));
        var elementReport = AccessTools.Method(typeof(ElementLogicException), "ReportInternal");
        var reportElement = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(AegisElementReportBoundary));
        harmony.Patch(enchantRefresh, prefix: new HarmonyMethod(skipVisualStats));
        harmony.Patch(enchantDollRoom, prefix: new HarmonyMethod(skipDollRoom));
        harmony.Patch(applyBuffRule, prefix: new HarmonyMethod(rejectWard));
        harmony.Patch(attachEnchantment, postfix: new HarmonyMethod(failAttachment));
        harmony.Patch(loggerInstance, prefix: new HarmonyMethod(offlineLogger));
        harmony.Patch(loggerLog, prefix: new HarmonyMethod(expectedError));
        harmony.Patch(elementReport, prefix: new HarmonyMethod(reportElement));
        applyNativeEffects = true;
        var previousTime = currentTime;
        try
        {
            var invest = Granted("InvestFocusAbility");
            for (int i = 0; i < 11; i++) CastRule(invest);
            CastRule(Granted("LockFocusAbility"));
            Check(CastRule(standard).ForceFail && pool.Remaining == 11, "AEGIS casting without worn armor cannot debit Mental Focus");
            Equip(armor);
            armorSlot.Lock.Retain();
            rejectedAegisWard = ward;
            CastRule(standard);
            rejectedAegisWard = null;
            Check(pool.Remaining == 11 && unit.Buffs.GetBuff(ward) == null && armorSlot.Lock.Count == 1 &&
                  armor.Enchantments.Count() == 1 && armor.Enchantments.Contains(baseline),
                "AEGIS native buff rejection leaves focus, the armor and its foreign lock untouched");
            Check(CastRule(standard).Success && unit.Buffs.GetBuff(ward)?.TimeLeft == TimeSpan.FromMinutes(1) &&
                  pool.Remaining == 10 && armorSlot.Lock.Count == 2 && !armorSlot.CanRemoveItem(),
                "AEGIS actual native armor slot is locked for one minute with one paid school point");
            Eq(5, Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor), "AEGIS actual native combined enhancement is capped at five");
            var fire = Toggle("AegisFire20Choice");
            var heavyFortification = Toggle("AegisFortification75Choice");
            Check(fire.IsAvailable && !heavyFortification.IsAvailable, "AEGIS native property restriction rejects costs above the budget");
            fire.IsOn = true;
            Check(fire.IsStarted && !armor.Enchantments.Any(item => item.Blueprint == fireHeavy) &&
                  Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor) == 5,
                "AEGIS toggling a property updates the next activation and leaves the current armor snapshot alone");
            var firstWard = unit.Buffs.GetBuff(ward);
            failedAegisEnchantment = fireHeavy;
            CastRule(swift);
            failedAegisEnchantment = null;
            Check(aegisAttachmentInterrupted && pool.Remaining == 10 && unit.Buffs.GetBuff(ward) == firstWard &&
                  unit.Buffs.Enumerable.Count(buff => buff.Blueprint == ward) == 1 && armorSlot.Lock.Count == 2 &&
                  armor.Enchantments.Contains(baseline) && armor.Enchantments.Count() == 2 &&
                  Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor) == 5,
                "AEGIS a late native attachment failure restores the entire prior ward and retains both focus and the foreign lock");
            Check(CastRule(swift).Success && pool.Remaining == 8 && armor.Enchantments.Contains(baseline) &&
                  armor.Enchantments.Any(item => item.Blueprint == fireHeavy) &&
                  Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor) == 3 && armorSlot.Lock.Count == 2,
                "AEGIS real recast pays two and replaces the exact owned snapshot while retaining the foreign stock enchantment and lock");

            fire.IsOn = false;
            Toggle("AegisSpellStoringChoice").IsOn = true;
            CastRule(standard);
            var holder = unit.Progression.Features.Enumerable.Single(feature => feature.Blueprint == Asset<BlueprintFeature>("AegisStorageFact"));
            var payload = holder.Components.Single(component => component.SourceBlueprintComponent is Occultist0099AegisStorage)
                .GetData<Occultist0099AegisStoredSpell>();
            var fixtureTargetBuff = new BlueprintBuff { name = "0099N.AegisTouchDelivered", AssetGuid = P(Occultist0099Ids.Id("HostAegisTouch")),
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            fixtureTargetBuff.OnEnable();
            var touchSpell = ProviderSpell(Occultist0099Ids.Id("HostAegisTouchSpell"), "0099N.AegisTouchSpell", SpellSchool.Necromancy, fixtureTargetBuff);
            touchSpell.Range = AbilityRange.Touch;
            touchSpell.CanTargetEnemies = true;
            touchSpell.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            var spontaneous = StorageBook(true);
            var known = spontaneous.AddKnown(1, touchSpell, false);
            var slots = (int[])Get(spontaneous, "m_SpontaneousSlots");
            slots[1] = 2;
            AbilityData Conversion(AbilityData original) => original.GetConversions().Single(candidate =>
                candidate.Blueprint.GetComponent<Occultist0099AegisStoreSpell>() != null);
            var conversion = Conversion(known);
            Check(conversion.Fact != null && conversion.Fact.Active && conversion.ConvertedFrom == known && conversion.ParamSpellSlot == null &&
                  conversion.Blueprint.ActionType == UnitCommand.CommandType.Standard && conversion.SaveSpellbookSlot,
                "AEGIS the native spontaneous conversion retains its owned fact and a Standard action");
            var wrongLevel = new AbilityData(touchSpell, spontaneous, 2);
            slots[2] = 1;
            Check(!wrongLevel.GetConversions().Any(candidate => candidate.Blueprint.GetComponent<Occultist0099AegisStoreSpell>() != null),
                "AEGIS an available second-level slot cannot impersonate the same spell only known at first level");
            var storedCast = CastRule(conversion);
            Check(storedCast.Success && slots[1] == 1 && payload.Armor == armor && payload.Spell?.Blueprint == touchSpell,
                "AEGIS storing debits the real spontaneous source once and binds the snapshot to the worn armor");
            conversion.Blueprint.GetComponent<Occultist0099AegisStoreSpell>().Apply(storedCast.Context, new TargetWrapper(unit));
            Eq(1, slots[1], "AEGIS a replay of the same storage execution cannot spend another spontaneous slot");

            var prepared = StorageBook(false);
            var memorized = prepared.AddKnown(1, touchSpell, false);
            var selected = new SpellSlot(1, SpellSlotType.Common, 0, prepared) { SpellShell = memorized, Available = true };
            ((List<SpellSlot>[])Get(prepared, "m_MemorizedSpells"))[1] = new List<SpellSlot> { selected };
            var preparedConversion = Conversion(memorized);
            var intelligence = unit.Stats.Intelligence;
            int startingIntelligence = intelligence.BaseValue;
            intelligence.BaseValue = 10;
            Check(!preparedConversion.Blueprint.GetComponent<Occultist0099AegisStoreSpell>().IsAbilityRestrictionPassed(preparedConversion) &&
                  selected.Available, "AEGIS current Intelligence is rechecked against the prepared source before spending");
            intelligence.BaseValue = startingIntelligence;
            CastRule(preparedConversion);
            Check(!selected.Available && slots[1] == 1 && payload.Spell?.Spellbook == prepared && payload.Parameters != null,
                "AEGIS the prepared conversion consumes the selected preparation and leaves the other book untouched");
            var stored = payload.Spell;
            var active = unit.Buffs.GetBuff(ward);
            currentTime = active.EndTime;
            unit.Buffs.TickBuff(active, currentTime);
            Check(unit.Buffs.GetBuff(ward) == null && armorSlot.Lock.Count == 1 && armor.Enchantments.Contains(baseline) &&
                  !unit.Abilities.Enumerable.Any(ability => ability.Blueprint == Asset<BlueprintAbility>("AegisStoreSpellAbility")) &&
                  ReferenceEquals(payload.Spell, stored), "AEGIS expiration withdraws the property and one lock but retains the prepared spell and foreign enhancement");
            armorSlot.Lock.Release();
            Equip(identical);
            CastRule(standard);
            Check(!Toggle("AegisReleaseToggle").IsAvailable && ReferenceEquals(payload.Armor, armor),
                "AEGIS a different item of the same blueprint cannot release the stored spell");
            Check(!conversion.Blueprint.GetComponent<Occultist0099AegisStoreSpell>().IsAbilityRestrictionPassed(conversion),
                "AEGIS a stale conversion from the expired armor's grant cannot write to an identical replacement");
            unit.Buffs.GetBuff(ward).Remove();
            Equip(armor);
            CastRule(standard);
            var release = Toggle("AegisReleaseToggle");
            Check(release.IsAvailable && ReferenceEquals(payload.Spell, stored), "AEGIS renewed spell storing on the same armor restores release access");
            release.IsOn = true;
            var weaponType = new BlueprintWeaponType { Category = WeaponCategory.Longsword };
            var weaponBlueprint = new BlueprintItemWeapon();
            Set(weaponBlueprint, "m_Type", Ref<BlueprintWeaponTypeReference>(weaponType));
            var weapon = Empty<ItemEntityWeapon>();
            Set(weapon, "m_Blueprint", weaponBlueprint);
            var hit = Empty<RuleAttackRoll>();
            Set(hit, "Result", AttackResult.Hit);
            var attack = Empty<RuleAttackWithWeapon>();
            Set(attack, "Weapon", weapon);
            Set(attack, "Initiator", enemy);
            Set(attack, "Target", unit);
            Set(attack, "AttackRoll", hit);
            release.AppliedBuff.CallComponents<AddTargetAttackWithWeaponTrigger>(component => component.OnEventDidTrigger(attack));
            Check(enemy.Buffs.GetBuff(fixtureTargetBuff) != null && payload.Spell == null && !release.IsOn &&
                  !selected.Available && slots[1] == 1, "AEGIS native melee hit releases the stored touch once without spending any second slot");
            Toggle("AegisSpellStoringChoice").IsOn = false;
            unit.Buffs.GetBuff(ward).Remove();
            var armorFeatures = unit.State.Features;
            armorFeatures.FakeEnhancement.Retain();
            try
            {
                CastRule(standard);
                Eq(5, Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor),
                    "AEGIS native Magic Deceiver Fake Enhancement is included once and stays under the combined cap");
                level.Level = 1;
                CastRule(standard);
                Eq(3, Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor),
                    "AEGIS a native enhancement tier that would overspend the budget leaves the existing value intact");
                unit.Buffs.GetBuff(ward).Remove();
            }
            finally { armorFeatures.FakeEnhancement.Release(); }
            level.Level = 12;
            armorFeatures.AdvancedFakeEnhancement.Retain();
            try
            {
                CastRule(standard);
                Eq(4, Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor),
                    "AEGIS preserves the native True Enhancement when no higher stock donor fits the cap");
            }
            finally { armorFeatures.AdvancedFakeEnhancement.Release(); }
            unit.RemoveFact(implement);
            Check(unit.Buffs.GetBuff(ward) == null && armorSlot.Lock.Count == 0 && armor.Enchantments.Contains(baseline),
                "AEGIS loss of the base source releases the active enhancement and preserves the original armor property");
        }
        finally
        {
            currentTime = previousTime;
            applyNativeEffects = false;
            harmony.Unpatch(enchantRefresh, skipVisualStats);
            harmony.Unpatch(enchantDollRoom, skipDollRoom);
            harmony.Unpatch(applyBuffRule, rejectWard);
            harmony.Unpatch(attachEnchantment, failAttachment);
            harmony.Unpatch(loggerInstance, offlineLogger);
            harmony.Unpatch(loggerLog, expectedError);
            harmony.Unpatch(elementReport, reportElement);
            rejectedAegisWard = null;
            failedAegisEnchantment = null;
        }
        caseStatus["Aegis"] = "native_worn_item_snapshot_lock_and_prepared_spontaneous_storage";

        Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility Toggle(string token)
            => unit.ActivatableAbilities.Enumerable.Single(ability => ability.Blueprint.AssetGuid == P(Occultist0099Ids.Id(token)));

        void Equip(ItemEntityArmor item)
        {
            var previous = armorSlot.MaybeArmor;
            if (previous != null)
            {
                previous.HoldingSlot = null;
                Set(previous, "m_WielderRef", default(EntityRef<UnitEntityData>));
            }
            armorSlot.SetFakeItem(item);
            item.HoldingSlot = armorSlot;
            Set(item, "m_WielderRef", (EntityRef<UnitEntityData>)unit);
            item.Facts.TurnOn();
        }

        Spellbook StorageBook(bool spontaneous)
        {
            var bp = new BlueprintSpellbook { AssetGuid = P(Occultist0099Ids.Id("HostAegisBook" + spontaneous)),
                name = "0099N.AegisBook" + spontaneous, Spontaneous = spontaneous, CastingAttribute = StatType.Intelligence,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            Set(bp, "m_SpellList", Ref<BlueprintSpellListReference>(character.Spellbook.SpellList));
            Set(bp, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(character.Spellbook.SpellsPerDay));
            bp.OnEnable();
            var book = unit.Descriptor.DemandSpellbook(bp);
            Set(book, "m_BaseLevelInternal", 12);
            return book;
        }
    }

    private static bool ArmorEntityServiceBoundary(ref EntityService __result)
    {
        __result = armorFixtureEntities;
        return false;
    }

    private static bool ArmorDollRoomBoundary(ref bool __result)
    {
        __result = false;
        return false;
    }

    private static void RejectAegisWard(RuleCanApplyBuff __instance)
    {
        if (__instance.Blueprint == rejectedAegisWard) __instance.CanApply = false;
    }

    private static void FailAegisAttachment(BlueprintItemEnchantment __0, ItemEnchantment __result)
    {
        if (__0 != failedAegisEnchantment || __result == null) return;
        aegisAttachmentInterrupted = __result.Owner.Enchantments.Contains(__result);
        throw new InvalidOperationException("Injected after native Aegis enchantment attachment.");
    }

    private static bool AegisLoggerInstanceBoundary(ref Owlcat.Runtime.Core.Logging.Logger __result)
    {
        __result = Empty<Owlcat.Runtime.Core.Logging.Logger>();
        return false;
    }

    private static bool ExpectedAegisFaultLog(Owlcat.Runtime.Core.Logging.LogSeverity __2, Exception __3, object __4)
    {
        if (__2 < Owlcat.Runtime.Core.Logging.LogSeverity.Error) return false;
        string message = __4?.ToString() ?? "";
        if (failedAegisEnchantment != null && message.Contains("Injected after native Aegis enchantment attachment."))
            Console.WriteLine("0099N EXPECTED LOG: native armor attachment failure was rolled back");
        else
            HostLog(__3 ?? new InvalidOperationException(message));
        return false;
    }

    private static bool AegisElementReportBoundary(ElementLogicException __instance)
    {
        HostLog(__instance.InnerException ?? __instance);
        return false;
    }

    private static void AbjurationPowersNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("abjuration-powers", character, 11, 16);
        var attacker = Unit("abjuration-powers-incoming", character, 12, 16);
        Set(unit, "IsTurnedOn", true);
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Abjuration-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 11);
        var state = LevelState(book);
        var powerSelection = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        foreach (var power in new[] { (Token: "EnergyShield", Level: 3), (Token: "Unraveling", Level: 5), (Token: "GlobeOfNegation", Level: 11) })
        {
            var option = Asset<BlueprintFeature>(power.Token + "Power");
            var pick = new SelectFeature(state.AddSelection(null, character.Progression, powerSelection, power.Level), option);
            var data = unit.Progression.GetClassData(character);
            data.Level = power.Level - 1;
            Check(!pick.Check(state, unit.Descriptor), "ABJ " + power.Token + " unavailable before its actual class level");
            data.Level = power.Level;
            Check(pick.Check(state, unit.Descriptor), "ABJ " + power.Token + " selectable via native Focus Power choice");
            pick.Apply(state, unit.Descriptor);
            data.Level = 11;
        }
        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var invest = Granted("InvestFocusAbility");
        var locked = Granted("LockFocusAbility");
        var energy = Granted("EnergyShieldAbility");
        var globe = Granted("GlobeOfNegationAbility");
        var unravel = Granted("UnravelingAbility");
        var energyBuff = Asset<BlueprintBuff>("EnergyShieldBuff");
        var globeBuff = Asset<BlueprintBuff>("GlobeOfNegationBuff");
        var before = unit.Get<Occultist0099FocusPart>().State;
        var pool = before.Schools["Abjuration"];
        var globeModule = Production("PsychicGlobeSpells");
        harmony.CreateClassProcessor(globeModule.GetNestedType("DeliveryPatch", BindingFlags.NonPublic)).Patch();
        harmony.CreateClassProcessor(globeModule.GetNestedType("AreaPatch", BindingFlags.NonPublic)).Patch();
        var apply = AccessTools.Method(typeof(AbilityExecutionProcess), "DoApplyEffect");
        var delivery = AccessTools.Method(globeModule.GetNestedType("DeliveryPatch", BindingFlags.NonPublic), "Prefix");
        var tryArea = AccessTools.Method(typeof(AreaEffectEntityData), "TryOvercomeSpellResistance");
        var dice = AccessTools.PropertyGetter(AccessTools.Inner(typeof(RulebookEvent), "Dice"), "D20");
        var fixedRoll = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(DispelDiceBoundary));
        var hidden = AccessTools.PropertyGetter(typeof(BlueprintBuff), nameof(BlueprintBuff.HiddenInInspector));
        var visibleFixtureBuff = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NotTacticalCombat));
        harmony.Patch(hidden, prefix: new HarmonyMethod(visibleFixtureBuff));
        harmony.Patch(dice, prefix: new HarmonyMethod(fixedRoll));
        applyNativeEffects = true;
        try
        {
            Check(CastRule(energy).ForceFail && unit.Buffs.GetBuff(energyBuff) == null, "ABJ unlocked Energy Shield pays nothing and grants no protection");
            for (int i = 0; i < 13; i++) CastRule(invest);
            CastRule(locked);
            Check(CastRule(energy).Success && unit.Buffs.GetBuff(energyBuff)?.TimeLeft == TimeSpan.FromMinutes(1) && pool.Remaining == 12,
                "ABJ native Energy Shield pays one school point and starts the one-minute protection");

            var target = new TargetWrapper(unit);
            var damageContext = new AbilityExecutionContext(energy, AbilityParams.Empty, target);
            var nativePool = unit.Get<Kingmaker.UnitLogic.Parts.UnitPartDamageReduction>();
            var resistanceBlueprint = new BlueprintBuff { name = "0099N.HigherElectricResistance",
                AssetGuid = P(Occultist0099Ids.Id("HostElectricResistance")),
                ComponentsArray = new BlueprintComponent[] { new AddDamageResistanceEnergy { name = "0099N.ElectricResistance",
                    Type = DamageEnergyType.Electricity, Value = 60 } } };
            resistanceBlueprint.OnEnable();
            var resistance = unit.Buffs.AddBuff(resistanceBlueprint, unit, TimeSpan.FromMinutes(1));
            var electric = new EnergyDamage(DiceFormula.Zero, 20, DamageEnergyType.Electricity);
            Rulebook.Trigger(new RuleDealDamage(attacker, unit, electric) { Reason = damageContext });
            Check(electric.ReductionBecauseResistance.TotalValue == 20 &&
                  nativePool.AllSources.Single(entry => entry.Settings is Occultist0099EnergyShield).GetCurrentValue() == 55,
                "ABJ stronger native resistance takes the packet instead of stacking or spending Energy Shield");
            resistance.Remove();
            unit.State.AddCondition(UnitCondition.SuppressedEnergyResistance);
            var fire = new EnergyDamage(DiceFormula.Zero, 2, DamageEnergyType.Fire);
            Rulebook.Trigger(new RuleDealDamage(attacker, unit, fire) { Reason = damageContext });
            unit.State.RemoveCondition(UnitCondition.SuppressedEnergyResistance);
            Check(fire.ReductionBecauseResistance.TotalValue == 0 && nativePool.AllSources.Single().GetCurrentValue() == 55,
                "ABJ native suppression bypasses the shield without draining the pool");
            var acid = new EnergyDamage(DiceFormula.Zero, 40, DamageEnergyType.Acid);
            var cold = new EnergyDamage(DiceFormula.Zero, 30, DamageEnergyType.Cold);
            var sonic = new EnergyDamage(DiceFormula.Zero, 5, DamageEnergyType.Sonic);
            var damage = Rulebook.Trigger(new RuleDealDamage(attacker, unit, new DamageBundle(acid, cold, sonic)) { Reason = damageContext });
            Check(acid.ReductionBecauseResistance.TotalValue == 40 && cold.ReductionBecauseResistance.TotalValue == 15 &&
                  sonic.ReductionBecauseResistance.TotalValue == 0 && unit.Buffs.GetBuff(energyBuff) == null && damage.Result == 20,
                "ABJ one native pool absorbs 55 total across acid/cold in the same damage packet; sonic passes and depletion removes the buff");

            Check(CastRule(globe).Success && unit.Buffs.GetBuff(globeBuff) != null && pool.Remaining == 9,
                "ABJ native Globe of Negation pays three school points and binds to the wearer");
            var ordinary = new AbilityData(Native<BlueprintAbility>("55f14bc84d7c85446b07a1b5dd6b2b4c"), attacker.Descriptor);
            var incoming = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 6 }, target);
            int Capacity() => unit.Buffs.GetBuff(globeBuff)?.Components.Single(component =>
                component.SourceBlueprintComponent is Occultist0099GlobeOfNegation).GetData<Occultist0099GlobeData>().Remaining ?? 0;
            bool Allows(AbilityExecutionContext context, UnitEntityData recipient)
                => (bool)delivery.Invoke(null, new object[] { context, new TargetWrapper(recipient), null });
            var cantrip = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 0 }, target);
            Check(Allows(cantrip, unit) && Allows(incoming, attacker) && Capacity() == 11,
                "ABJ original Globe semantics allow cantrips and another recipient without spending capacity");
            var lesserId = (BlueprintGuid)AccessTools.Method(Production("PsychicAssets"), "Id")
                .Invoke(null, new object[] { "GlobeOfInvulnerabilityLesserBuff" });
            var lesserBlueprint = new BlueprintBuff { name = "0099N.StationaryGlobeFixture", AssetGuid = lesserId,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            lesserBlueprint.OnEnable();
            var stationary = unit.Buffs.AddBuff(lesserBlueprint, unit, TimeSpan.FromMinutes(1));
            var lesserCast = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 2 }, target);
            apply.Invoke(null, new object[] { lesserCast, target, null });
            Check(Capacity() == 11, "ABJ an existing CotW stationary globe intercepts its normal spell levels without spending the new globe");
            stationary.Remove();
            apply.Invoke(null, new object[] { incoming, target, null });
            apply.Invoke(null, new object[] { incoming, target, null });
            Check(Capacity() == 5, "ABJ reused native delivery blocks both applications of a sixth-circle cast and charges once");
            var finishing = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 8 }, target);
            apply.Invoke(null, new object[] { finishing, target, null });
            apply.Invoke(null, new object[] { finishing, target, null });
            var newCast = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 1 }, target);
            Check(unit.Buffs.GetBuff(globeBuff) == null && Allows(newCast, unit),
                "ABJ the final over-capacity spell is blocked across its effect chain, collapses the globe, and a new cast goes through");
            var area = Empty<AreaEffectEntityData>();
            Set(area, "m_Context", finishing);
            Set(area, "m_Blueprint", new BlueprintAbilityAreaEffect { SpellResistance = false });
            Set(area, "m_CreationTime", currentTime);
            Check(!(bool)tryArea.Invoke(area, new object[] { unit }),
                "ABJ an area arriving with the terminal direct spell is also blocked for that delivery");
            var sameTick = currentTime;
            try
            {
                currentTime += TimeSpan.FromSeconds(1);
                Check((bool)tryArea.Invoke(area, new object[] { unit }),
                    "ABJ the same area may apply later after the globe has collapsed");
            }
            finally { currentTime = sameTick; }
            CastRule(globe);
            var areaContext = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 3 }, target);
            Set(area, "m_Context", areaContext);
            Set(area, "m_CreationTime", currentTime - TimeSpan.FromSeconds(1));
            Check((bool)tryArea.Invoke(area, new object[] { unit }) && Capacity() == 11, "ABJ existing area effect predating the globe is preserved");
            Set(area, "m_CreationTime", currentTime + TimeSpan.FromSeconds(1));
            Check(!(bool)tryArea.Invoke(area, new object[] { unit }) && !(bool)tryArea.Invoke(area, new object[] { unit }) && Capacity() == 8,
                "ABJ new native area entrance is blocked and repeated entrance checks charge only once for the same spell");
            apply.Invoke(null, new object[] { areaContext, target, null });
            Check(Capacity() == 8, "ABJ an area-to-direct cast shares one globe charge");
            var directFirst = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 4 }, target);
            apply.Invoke(null, new object[] { directFirst, target, null });
            Set(area, "m_Context", directFirst);
            Check(!(bool)tryArea.Invoke(area, new object[] { unit }) && Capacity() == 4,
                "ABJ a direct-to-area cast shares one globe charge");
            var areaLast = new AbilityExecutionContext(ordinary, new AbilityParams { SpellLevel = 6 }, target);
            Set(area, "m_Context", areaLast);
            Check(!(bool)tryArea.Invoke(area, new object[] { unit }) && unit.Buffs.GetBuff(globeBuff) == null,
                "ABJ a terminal area admission consumes the globe without admitting its first application");
            apply.Invoke(null, new object[] { areaLast, target, null });
            CastRule(globe);

            var highBuff = new BlueprintBuff { name = "0099N.DispelHigh", AssetGuid = P(Occultist0099Ids.Id("HostDispelHigh")),
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            var lowBuff = new BlueprintBuff { name = "0099N.DispelLow", AssetGuid = P(Occultist0099Ids.Id("HostDispelLow")),
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            foreach (var buff in new[] { highBuff, lowBuff }) buff.OnEnable();
            var source = new AbilityExecutionContext(ordinary, new AbilityParams { CasterLevel = 8, SpellLevel = 3 }, new TargetWrapper(attacker));
            var protectedBuff = attacker.Buffs.AddBuff(highBuff, source, TimeSpan.FromMinutes(1));
            protectedBuff.IsFromSpell = true;
            var secondary = attacker.Buffs.AddBuff(lowBuff, source, TimeSpan.FromMinutes(1));
            secondary.IsFromSpell = true;
            secondary.Context.SetParams(new AbilityParams { CasterLevel = 2, SpellLevel = 1 });
            var cast = Rulebook.Trigger(new RuleCastSpell(unravel, attacker));
            Check(cast.Success && unravel.Blueprint.Range == AbilityRange.Touch && attacker.Buffs.GetBuff(highBuff) == null &&
                  attacker.Buffs.GetBuff(lowBuff) == secondary && pool.Remaining == 2,
                "ABJ Unraveling runs native one-buff targeted Dispel Magic through a private touch cast and pays one point");
            var selfDispel = unit.Buffs.AddBuff(highBuff, source, TimeSpan.FromMinutes(1));
            selfDispel.IsFromSpell = true;
            selfDispel.Context.SetParams(new AbilityParams { CasterLevel = 14, SpellLevel = 3 });
            var canTarget = AccessTools.Method(typeof(AbilityData), nameof(AbilityData.CanTarget), new[] { typeof(TargetWrapper) });
            var legalBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(LegalTargetBoundary));
            harmony.Unpatch(canTarget, legalBoundary);
            try
            {
                Check(unravel.CanTarget(target) && unravel.CanTarget(new TargetWrapper(attacker)) &&
                      unravel.Blueprint.CanTargetFriends && unravel.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful,
                    "ABJ native Unraveling targeting accepts a creature or self with the original ally indicator");
                Check(CastRule(unravel).Success && unit.Buffs.GetBuff(highBuff) == null && unit.Buffs.GetBuff(globeBuff) != null &&
                      pool.Remaining == 1, "ABJ native self-dispel removes the stronger fixture spell and keeps the unrelated globe");
            }
            finally { harmony.Patch(canTarget, prefix: new HarmonyMethod(legalBoundary)); }
            CastRule(energy);
            unit.RemoveFact(implement);
            Check(unit.Buffs.GetBuff(globeBuff) == null && unit.Buffs.GetBuff(energyBuff) == null,
                "ABJ removing the source ends both active self protections");
        }
        finally
        {
            applyNativeEffects = false;
            harmony.Unpatch(dice, fixedRoll);
            harmony.Unpatch(hidden, visibleFixtureBuff);
        }
        caseStatus["AbjurationAddedPowers"] = "native_shared_energy_pool_targeted_dispel_and_spell_globe";
    }

    private static void RegistrationNative(JArray auditedRows, string donorTable)
    {
        var catalogue = (Occultist0099SpellRoot[])AccessTools.Field(Production("Occultist0099SpellCatalog"), "Roots").GetValue(null);
        Eq(auditedRows.Count, catalogue.Length, "SP08 the production data contains all frozen 143 root identities");
        var byId = catalogue.ToDictionary(row => row.Guid);
        Check(auditedRows.All(audit => byId.TryGetValue((string)audit["root"]["guid"], out var row) &&
              row.Name == (string)audit["root"]["name"] && row.School == (string)audit["school"] &&
              row.Circle == (int)audit["level"] && row.Source == (string)audit["source"] &&
              row.Provider == (string)audit["root"]["provider"] && row.RequiresPublishedList == ((bool?)audit["root"]["publishedListRequired"] ?? false)),
            "SP08 the 143 production mappings agree with the frozen source, schools, circles and provider identities");

        var detached = new BlueprintsCache();
        var register = AccessTools.Method(Production("Occultist0099NativeRegistration"), "RegisterInDetachedCache");
        bool TryRegister(BlueprintsCache target) => (bool)register.Invoke(null, new object[] { target });
        Check(!TryRegister(cache), "SP08 the production development entry refuses the ordinary process cache");
        const string alchemistClassId = "0937bec61c0dabc468428f496580c721";
        const string proficienciesId = "c5e479367d07d62428f2fe92f39c0341";
        var implementedSchools = new HashSet<string> { "Abjuration", "Enchantment", "Divination", "Illusion", "Transmutation" };
        var rows = catalogue.Where(row => (implementedSchools.Contains(row.School) || row.IsKnack) && row.Source != "External").ToArray();
        var coreDonors = (string[])AccessTools.Field(Production("Occultist0099Enchantment"), "DonorIds").GetValue(null);
        var armorDonors = (string[])AccessTools.Field(Production("Occultist0099Aegis"), "Donors").GetValue(null);
        var divinationDonors = (string[])AccessTools.Field(Production("Occultist0099Divination"), "DonorIds").GetValue(null);
        var illusionDonors = (string[])AccessTools.Field(Production("Occultist0099Illusion"), "DonorIds").GetValue(null);
        var shadowDonors = (string[])AccessTools.Field(Production("Occultist0099ShadowBeasts"), "DonorIds").GetValue(null);
        var transmutationDonors = (string[])AccessTools.Field(Production("Occultist0099Transmutation"), "DonorIds").GetValue(null);
        var weaponDonors = (IEnumerable<string>)AccessTools.Property(Production("Occultist0099TransmutationWeapons"), "Donors").GetValue(null);
        var expectedDonors = new[] { alchemistClassId, proficienciesId, "027d37761f3804042afa96fe3e9086cc", "f049fe38f5bb5ae48b252852727ab86a",
            "143775c49ae6b7446b805d3b2e702298" }
            .Concat(coreDonors).Concat(armorDonors).Concat(divinationDonors).Concat(illusionDonors).Concat(shadowDonors)
            .Concat(transmutationDonors).Concat(weaponDonors)
            .Concat(rows.Select(row => row.Guid)).Distinct().ToArray();
        LoadRows(expectedDonors);
        var transmutationRoots = rows.Where(row => row.Source == "CotW" && row.School == "Transmutation").ToArray();
        if (transmutationRoots.Any(row => cache.Load(P(row.Guid)) == null)) StageTransmutationRoots(transmutationRoots);
        foreach (var row in rows.Where(row => row.Source == "CotW" && row.School != "Transmutation" && cache.Load(P(row.Guid)) == null))
            StageAbsentRoot(row);
        foreach (var id in expectedDonors)
        {
            var donor = cache.Load(P(id));
            if (donor != null) detached.AddCachedBlueprint(donor.AssetGuid, donor);
            else Check(false, "HOST B7 donor missing from the archive " + id);
        }
        var resolve = AccessTools.Method(Production("Occultist0099Donors"), "Resolve");
        foreach (var row in rows.Where(row => resolve.Invoke(null, new object[] { detached, new[] { row } }) == null))
            Check(false, "HOST current strict provider contract rejects the expected core root " + row.Name);
        var alchemist = Native<BlueprintCharacterClass>(alchemistClassId);
        var root = Empty<BlueprintRoot>();
        root.AssetGuid = P("2d77316c72b9ed44f888ceefc2a131f6");
        root.name = "BlueprintRoot";
        Set(root, "Progression", new ProgressionRoot());
        var original = new[] { Ref<BlueprintCharacterClassReference>(alchemist) };
        Set(root.Progression, "m_CharacterClasses", original);
        detached.AddCachedBlueprint(root.AssetGuid, root);
        var tokens = (string[])AccessTools.Method(Production("Occultist0099NativeRegistration"), "IdentityTokens").Invoke(null, null);
        var privateIds = tokens.Select(token => P(Occultist0099Ids.Id(token))).ToArray();
        bool Vacant() => privateIds.All(id => detached.Load(id) == null) && ReferenceEquals(Get(root.Progression, "m_CharacterClasses"), original);
        Check(!TryRegister(detached) && Vacant(), "SP08 missing mandatory Alchemist table leaves the real class list and all private cache entries untouched");
        detached.AddCachedBlueprint(P(donorTable), Native<BlueprintSpellsTable>(donorTable));
        var holdBuff = Native<BlueprintBuff>("2cfcce5b62d3e6d4082ec31b58468cc8");
        detached.RemoveCachedBlueprint(holdBuff.AssetGuid);
        Check(!TryRegister(detached) && Vacant(), "SP08 a missing mandatory Enchantment effect cannot publish either school");
        detached.AddCachedBlueprint(holdBuff.AssetGuid, new BlueprintBuff { name = holdBuff.name, AssetGuid = holdBuff.AssetGuid,
            ComponentsArray = Array.Empty<BlueprintComponent>() });
        Check(!TryRegister(detached) && Vacant(), "SP08 an identity-only paralysis donor cannot omit the native effect or its recurring save");
        detached.AddCachedBlueprint(holdBuff.AssetGuid, holdBuff);
        var processRoot = (BlueprintRoot)cache.Load(root.AssetGuid);
        bool temporaryProcessRoot = processRoot == null;
        if (temporaryProcessRoot)
        {
            processRoot = Empty<BlueprintRoot>();
            processRoot.AssetGuid = root.AssetGuid;
            Set(processRoot, "Progression", new ProgressionRoot());
            cache.AddCachedBlueprint(processRoot.AssetGuid, processRoot);
        }
        var independentProgression = root.Progression;
        Set(root, "Progression", processRoot.Progression);
        var processClasses = Get(processRoot.Progression, "m_CharacterClasses");
        Check(!TryRegister(detached) && ReferenceEquals(Get(processRoot.Progression, "m_CharacterClasses"), processClasses) &&
              privateIds.All(id => detached.Load(id) == null), "SP08 a detached cache sharing the process progression root is rejected");
        Set(root, "Progression", independentProgression);
        if (temporaryProcessRoot) cache.RemoveCachedBlueprint(processRoot.AssetGuid);
        var sharedStrings = (Dictionary<string, string>)AccessTools.Field(Production("PsychicAssets"), "Strings").GetValue(null);
        var stringsBefore = new Dictionary<string, string>(sharedStrings);
        var previousPack = Kingmaker.Localization.LocalizationManager.CurrentPack;
        var testPack = new Kingmaker.Localization.LocalizationPack();
        testPack.PutString("0099N.ForeignText", "Unchanged");
        Kingmaker.Localization.LocalizationManager.CurrentPack = testPack;
        bool LocalizationUnchanged() => sharedStrings.Count == stringsBefore.Count &&
            stringsBefore.All(pair => sharedStrings.TryGetValue(pair.Key, out string value) && value == pair.Value) &&
            ((System.Collections.IDictionary)Get(testPack, "m_Strings")).Count == 1;
        var originalShield = Native<BlueprintAbility>(Occultist0099AbjurationCatalog.MageShield);
        var aliasedShield = ProviderSpell(Occultist0099Ids.Id("HostWrongShieldGuid"), originalShield.name, SpellSchool.Abjuration, null);
        detached.AddCachedBlueprint(originalShield.AssetGuid, aliasedShield);
        Check(!TryRegister(detached) && Vacant(), "SP08 a same-name donor stored under the signed key cannot substitute a different payload GUID");
        detached.AddCachedBlueprint(originalShield.AssetGuid, originalShield);
        var occupied = new BlueprintSpellList { AssetGuid = P(Occultist0099Ids.Id("slot-2-Abjuration-ExtraLearnList")),
            name = "CotWWrathOccultist0099slot-2-Abjuration-ExtraLearnList", ComponentsArray = Array.Empty<BlueprintComponent>() };
        detached.AddCachedBlueprint(occupied.AssetGuid, null);
        var detachedEntries = (System.Collections.IDictionary)Get(detached, "m_LoadedBlueprints");
        Check(!TryRegister(detached) && detachedEntries.Contains(occupied.AssetGuid) && Vacant(),
            "SP08 an existing native cache reservation is preserved even when Load has no payload");
        detached.AddCachedBlueprint(occupied.AssetGuid, occupied);
        Check(!TryRegister(detached) && ReferenceEquals(detached.Load(occupied.AssetGuid), occupied) &&
              privateIds.Where(id => id != occupied.AssetGuid).All(id => detached.Load(id) == null) &&
              ReferenceEquals(Get(root.Progression, "m_CharacterClasses"), original),
            "SP08 a signed-looking source-list collision preserves the foreign object and publishes nothing else");
        detached.RemoveCachedBlueprint(occupied.AssetGuid);
        Check(LocalizationUnchanged(), "SP08 rejected donor aliases and collisions leave both global localization stores unchanged");

        var add = AccessTools.Method(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint));
        var fault = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(InterruptRegistration));
        harmony.Patch(add, postfix: new HarmonyMethod(fault));
        bool interrupted = false;
        faultedRegistration = detached;
        try { TryRegister(detached); }
        catch (TargetInvocationException error) when (error.InnerException?.Message == "0099 fixture write interrupted") { interrupted = true; }
        finally { faultedRegistration = null; harmony.Unpatch(add, fault); }
        Check(interrupted && Vacant(), "SP08 interruption after real cache insertion rolls back every owned object and the class list");
        Check(LocalizationUnchanged(), "SP08 rollback preserves both global localization stores");

        bool registered = TryRegister(detached);
        Check(registered, "SP08 restored mandatory donor and vacant identities publish the detached school graph");
        if (!registered) return;
        Check(LocalizationUnchanged(), "SP08 a successful detached graph does not publish process localization");
        Kingmaker.Localization.LocalizationManager.CurrentPack = previousPack;
        var entries = privateIds.Select(detached.Load).ToArray();
        registeredPrivateIdentities = entries.Length;
        Check(entries.All(entry => entry != null) && root.Progression.CharacterClasses.Count() == 2 &&
              root.Progression.CharacterClasses.First() == alchemist,
            "SP08 each signed asset is present and only one class was appended to the unchanged native entry");
        var publishedClass = (BlueprintCharacterClass)detached.Load(P(Occultist0099Ids.ClassGuid));
        var publishedBook = publishedClass.Spellbook;
        var baseList = publishedBook.SpellList;
        var sourceList = publishedBook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists[0].List.Get();
        var listsBefore = baseList.SpellsByLevel;
        Check(publishedBook.CharacterClass == publishedClass && publishedBook.Spontaneous && !publishedBook.IsArcane &&
              publishedBook.GetComponent<PsychicSpellbookMarker>()?.MaximumSpellLevel == 6 &&
              publishedBook.SpellsPerDay == Native<BlueprintSpellsTable>(donorTable) &&
              publishedClass.Progression.LevelEntries.Single(entry => entry.Level == 1).Features.OfType<BlueprintFeatureSelection>().Count() == 3 &&
              baseList.GetSpells(0).Count == 6 && baseList.SpellsByLevel.Length == 11,
            "SP08 the real graph links its Psychic-marked spontaneous Alchemist-table book, two initial implement choices and six fixed knacks");
        Check(TryRegister(detached) && entries.SequenceEqual(privateIds.Select(detached.Load)) &&
              ReferenceEquals(listsBefore, baseList.SpellsByLevel) && root.Progression.CharacterClasses.Count() == 2,
            "SP08 retry preserves object identities, class entry count and unchanged row arrays");
        var firstLevel = Unit("registered-0099-knacks", publishedClass, 1, 16);
        firstLevel.Facts.Add(new Feature((BlueprintFeature)detached.Load(P(Occultist0099Ids.SpellbookFactGuid)), firstLevel.Descriptor));
        var knackBook = firstLevel.Descriptor.DemandSpellbook(publishedBook);
        new ApplySpellbook().Apply(LevelState(knackBook), firstLevel.Descriptor);
        Check(knackBook.BaseLevel == 1 &&
              new HashSet<BlueprintGuid>(knackBook.GetKnownSpells(0).Select(ability => ability.Blueprint.AssetGuid))
                  .SetEquals(Occultist0099AbjurationCatalog.Knacks.Select(P)) &&
              publishedBook.SpellsPerDay.GetCount(1, 0) == 0,
            "SP01 the native first-level book learns the exact six knacks from its list and grants no paid level-zero slot");
        InBook(firstLevel, fact => Eq(0, Occultist0099Spells.QuotaCount(SpellStateOf(fact), 0),
            "SP01 native knacks consume no private implement quota"));

        var effect = new BlueprintBuff { AssetGuid = P(Occultist0099Ids.Id("HostProviderEffect")), name = "0099N.ProviderEffect",
            ComponentsArray = Array.Empty<BlueprintComponent>() };
        var cloak = ProviderSpell(Occultist0099AbjurationCatalog.CloakOfWinds, "CloakOfWindsAbility", SpellSchool.Abjuration, effect);
        detached.AddCachedBlueprint(cloak.AssetGuid, cloak);
        Check(TryRegister(detached) && !baseList.GetSpells(3).Contains(cloak),
            "SP08 TTT root without provider publication does not enter any private circle");
        var wizardList = new BlueprintSpellList { AssetGuid = P("ba0401fdeb4062f40a7aa95b6f07fe89"), name = "WizardSpellList",
            SpellsByLevel = Enumerable.Range(0, 11).Select(circle => new SpellLevelList(circle)).ToArray() };
        detached.AddCachedBlueprint(wizardList.AssetGuid, wizardList);
        ProviderBacklink(cloak, wizardList, 3);
        Check(TryRegister(detached) && !baseList.GetSpells(3).Contains(cloak), "SP08 a lone backlink is not proof that the provider enabled Cloak");
        wizardList.SpellsByLevel[3].SpellsRefs.Add(Ref<BlueprintAbilityReference>(cloak));
        var danglingParent = new BlueprintAbilityReference();
        danglingParent.ReadGuidFromGuid(P(Occultist0099Ids.Id("HostMissingParent")));
        Set(cloak, "m_Parent", danglingParent);
        Check(TryRegister(detached) && !baseList.GetSpells(3).Contains(cloak),
            "SP08 a provider variant with an unresolved signed parent cannot qualify as a root");
        Set(cloak, "m_Parent", null);
        var run = cloak.GetComponent<AbilityEffectRunAction>();
        var actions = run.Actions;
        run.Actions = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() };
        var cloakComponents = cloak.ComponentsArray;
        AddDanglingProviderVariant(cloak);
        Check(TryRegister(detached) && !baseList.GetSpells(3).Contains(cloak),
            "SP08 a reciprocal Cloak placeholder with only a dangling variant remains unavailable");
        cloak.ComponentsArray = cloakComponents;
        run.Actions = actions;
        detached.RemoveCachedBlueprint(wizardList.AssetGuid);
        Check(TryRegister(detached) && !baseList.GetSpells(3).Contains(cloak),
            "SP08 a cached blueprint held by a removed provider list reference cannot authorize a detached graph");
        detached.AddCachedBlueprint(wizardList.AssetGuid, wizardList);
        var nativeRows = wizardList.SpellsByLevel;
        Check(TryRegister(detached) && sourceList.GetSpells(3).Contains(cloak) && baseList.GetSpells(3).Contains(cloak) &&
              entries.SequenceEqual(privateIds.Select(detached.Load)) && root.Progression.CharacterClasses.Count() == 2 &&
              wizardList.SpellsByLevel == nativeRows && wizardList.SpellsByLevel[3].SpellsRefs.Count == 1,
            "SP08 late reciprocal TTT publication refreshes both private rows without replacing the class, book or source-list identity");

        var unit = Unit("registered-0099-source", publishedClass, 7, 16);
        unit.Facts.Add(new Feature((BlueprintFeature)detached.Load(P(Occultist0099Ids.SpellbookFactGuid)), unit.Descriptor));
        var implement = new Feature((BlueprintFeature)detached.Load(P(Occultist0099Ids.Id("slot-1-Abjuration-Implement"))), unit.Descriptor);
        unit.Facts.Add(implement);
        var functioning = unit.Descriptor.DemandSpellbook(publishedBook);
        Set(functioning, "m_BaseLevelInternal", 7);
        var levelUp = LevelState(functioning);
        var learnCloak = new SelectSpell(publishedBook, sourceList, 3, cloak, 0);
        Check(NativeCheck(learnCloak, levelUp, unit.Descriptor), "SP08 a real registered implement and the validated donor table expose Cloak to native selection");
        learnCloak.Apply(levelUp, unit.Descriptor);
        Check(functioning.IsKnown(cloak), "SP08 the private book learns the validated root without enabling a per-provider model flag");
        unit.RemoveFact(implement);
        Check(!functioning.IsKnown(cloak), "SP03 removal of the registered last implement source retires its actual native known spell");
        wizardList.SpellsByLevel[3].SpellsRefs.Clear();
        Check(TryRegister(detached) && !baseList.GetSpells(3).Contains(cloak) && !sourceList.GetSpells(3).Contains(cloak),
            "SP08 withdrawn provider publication removes only the optional private row on retry");
        FireShieldVariantsNative(detached, byId["017970c386634351bfdea34d6a4d4c74"], wizardList, effect);
        var ignorePrerequisites = AccessTools.PropertyGetter(typeof(IgnorePrerequisites), nameof(IgnorePrerequisites.Ignore));
        var retail = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NotTacticalCombat));
        var rootGetter = AccessTools.PropertyGetter(typeof(BlueprintRoot), nameof(BlueprintRoot.Instance));
        var rootBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(EnchantmentRootBoundary));
        var visual = AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx), "RunAction");
        var noVisual = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoReplenish));
        EnchantmentFixtureRoot = root;
        var difficulty = Empty<Kingmaker.Settings.Difficulty.DifficultyPresetsList>();
        difficulty.StatsAdjustmentPresets = new[] { new Kingmaker.Settings.Difficulty.DifficultyPresetsList.StatsAdjustmentPreset() };
        Set(root, "m_DifficultyList", difficulty);
        harmony.Patch(rootGetter, prefix: new HarmonyMethod(rootBoundary));
        harmony.Patch(ignorePrerequisites, prefix: new HarmonyMethod(retail));
        harmony.Patch(visual, prefix: new HarmonyMethod(noVisual));
        try
        {
            EnchantmentNative(detached, publishedClass);
            AbjurationPowersNative(detached, publishedClass);
            AegisNative(detached, publishedClass);
            DivinationNative(detached, publishedClass);
            IllusionNative(detached, publishedClass);
            TransmutationNative(detached, publishedClass);
        }
        finally
        {
            harmony.Unpatch(ignorePrerequisites, retail);
            harmony.Unpatch(rootGetter, rootBoundary);
            harmony.Unpatch(visual, noVisual);
            EnchantmentFixtureRoot = null;
        }
        caseStatus["SP08"] = "native_atomic_detached_five_school_graph_only_full_class_not_run";
        caseStatus["SP03"] = "native_copy_serialization_and_last_registered_implement_revoke_only";
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
        var assetEvidence = JObject.Parse(File.ReadAllText(assetsPath));
        var slots = assetEvidence["dailySpellSlots"];
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
        armorArchiveRows = LoadArchiveDonors(Environment.GetEnvironmentVariable("COTW_0099_AEGIS_FIXTURE"));
        Eq(37, armorArchiveRows, "HOST loads the 20 signed armor enchantments and their native support rows");
        int divinationArchiveRows = LoadArchiveDonors(Environment.GetEnvironmentVariable("COTW_0099_DIVINATION_FIXTURE"));
        Eq(10, divinationArchiveRows, "HOST loads the signed Divination ability, resonance and native spell rows");
        int illusionArchiveRows = LoadArchiveDonors(Environment.GetEnvironmentVariable("COTW_0099_ILLUSION_FIXTURE"));
        Eq(78, illusionArchiveRows, "HOST loads the signed Illusion effects, spell roots, summon families and Wizard list");
        int transmutationArchiveRows = LoadArchiveDonors(Environment.GetEnvironmentVariable("COTW_0099_TRANSMUTATION_FIXTURE"));
        Eq(51, transmutationArchiveRows, "HOST loads the audited Transmutation spell and weapon donors");
        var keenEdgeReward = Native<Kingmaker.DLC.BlueprintDlcReward>("b94f823171a84e30ad7a1b892433ab5d");
        Set(keenEdgeReward, "m_IsAvailable", (bool?)false);
        var keenEdge = Native<BlueprintAbility>("64923c9c808143c7ab7adba5a27f0ca4");
        Check(Kingmaker.DLC.DlcConditionExtension.IsDlcRestricted(keenEdge), "TRAN Keen Edge keeps the native DLC gate");

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
        AbjurationEffectsNative();
        FocusPreviewAndRespecNative();
        RespecMasteryAuthorityNative();
        SP01Native();
        SP02Native();
        ExhaustedImplementQuotaNative();
        ImplementReplayNative();
        RestNative();
        GateNative();
        RegistrationNative((JArray)assetEvidence["spellRoots"], donorTable);
        Check(hostLogs == 0, "HOST native rules report no fixture exceptions");

        // Everything else in the frozen contract is honestly NOT run here.
        foreach (string id in new[]
        {
            "MF02", "MF05", "MF06", "MF07", "MF08", "MF10",
            "SP04", "SP05", "SP06", "SP07"
        })
            caseStatus[id] = "not_run_native";

        var report = new JObject
        {
            ["workPackage"] = "COTW-0099",
            ["contract"] = Occultist0099Ids.Contract,
            ["stage"] = "B12 Transmutation implement, resonance and focus powers",
            ["acceptanceScope"] = "native_entry_points_directed",
            ["playerGameAcceptance"] = false,
            ["gameLaunched"] = false,
            ["ummLaunched"] = false,
            ["slicePublished"] = Occultist0099Ids.SlicePublished,
            ["releaseEligible"] = false,
            ["productionBridgesInstalled"] = bridges,
            ["registeredPrivateIdentities"] = registeredPrivateIdentities,
            ["armorArchiveRows"] = armorArchiveRows,
            ["divinationArchiveRows"] = divinationArchiveRows,
            ["illusionArchiveRows"] = illusionArchiveRows,
            ["transmutationArchiveRows"] = transmutationArchiveRows,
            ["nativeRootsLoaded"] = loadedRoots,
            ["hostStagedRoots"] = hostStaged,
            ["hostStagedRootNames"] = new JArray(stagedRoots),
            ["skippedArchiveComponents"] = skippedComponents,
            ["rootFixtureBoundary"] = "Transmutation uses 14 Wrath roots and 9 roots produced by the existing CotW builders. The native DLC reward availability is supplied to exercise the Keen Edge gate; the four external provider roots are absent.",
            ["checks"] = checks,
            ["failures"] = new JArray(failures),
            ["hostLoggedExceptions"] = hostLogs,
            ["caseStatus"] = JObject.FromObject(caseStatus),
            ["result"] = failures.Count == 0 ? "NATIVE_DIRECTED_PASS" : "NATIVE_DIRECTED_FAIL",
            ["resultMeaning"] = failures.Count == 0
                ? "Detached Transmutation learning, focus lock, haste and Slow, movement, material swaps and owned weapon enhancement passed alongside the current Occultist checks."
                : "At least one native directed assertion failed; see failures.",
            ["boundary"] = "Detached BlueprintsCache, native choice, spellbook and cast payment. Size Alteration checks the native action graph. Philosopher's Touch checks the supplied recipient and burst definition. Slow enters the native buff-application rule after the save branch. Weapon and empty-hand changes use supplied items and the native events. Unity scenes, UI, ordinary startup, full career and disk saves remain outside scope.",
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
