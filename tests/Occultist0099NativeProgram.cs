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
using Kingmaker.Blueprints.Items.Equipment;
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
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Occultist0099NativeLauncher
{
    private static int Main(string[] args)
    {
        if (args.Length != 5 || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_AEGIS_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_CONJURATION_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_DIVINATION_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_EVOCATION_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_ILLUSION_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_NECROMANCY_FIXTURE")) ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COTW_0099_TRANSMUTATION_FIXTURE")))
        {
            Console.Error.WriteLine("Set the Aegis, Conjuration, Divination, Evocation, Illusion, Necromancy and Transmutation fixtures using ReadOccultist0099Archive.ps1.");
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
    private static bool useRealFactions;
    private static bool allowOpportunity;
    private static bool repowerSpawnNull;
    private static UnitEntityData repowerSpawned;
    private static Action repowerSpawnCallback;
    private static BlueprintFaction repowerEnemyFaction;
    private static BlueprintFeature repowerConstructType;
    private static BlueprintCharacterClass repowerCharacter;
    private static bool NotPlayerFaction(ref bool __result) { if (useRealFactions) return true; __result = false; return false; }
    private static bool NotTacticalCombat(ref bool __result) { __result = false; return false; }
    private static bool AllowTrue(ref bool __result) { __result = true; return false; }
    private static bool OpportunityBoundary(ref bool __result) { __result = allowOpportunity; return false; }
    private static bool RepowerRootBoundary(ref BlueprintRoot __result) { __result = EnchantmentFixtureRoot; return false; }
    private static bool RepowerTimerBoundary(ref IObservable<long> __result) { __result = UniRx.Observable.Empty<long>(); return false; }
    private static bool RepowerSpawnBoundary(BlueprintUnit __0, ref UnitEntityData __result)
    {
        if (repowerSpawnNull)
        {
            __result = null;
            return false;
        }
        var callback = repowerSpawnCallback;
        repowerSpawnCallback = null;
        callback?.Invoke();
        var summoned = Unit("repower-spawned-" + (++serial), repowerCharacter, 0, 10);
        Set(summoned.Descriptor, "Blueprint", __0);
        Set(summoned.Descriptor, "OriginalBlueprint", __0);
        Set(summoned.Descriptor, "m_Faction", repowerEnemyFaction);
        var attackFactions = new UnitAttackFactions(summoned.Descriptor);
        if (repowerEnemyFaction != null) attackFactions.Match(repowerEnemyFaction.AttackFactions);
        Set(summoned.Descriptor, "AttackFactions", attackFactions);
        Set(summoned.Descriptor, "HasOwnInventory", true);
        Set(summoned.Descriptor, "m_Inventory", new Kingmaker.Items.ItemsCollection(summoned));
        summoned.Group.ResetFactionSet();
        summoned.Group.UpdateAttackFactionsCache();
        if (repowerConstructType != null) summoned.Facts.Add(new Feature(repowerConstructType, summoned.Descriptor));
        repowerSpawned = summoned;
        __result = summoned;
        return false;
    }
    private static WeaponSlot counterHand;
    private static bool CounterThreatHand(out WeaponSlot __result) { __result = counterHand; return false; }
    private static bool CounterThreatRange(out float? __result) { __result = 5f; return false; }
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

    // Removing an implement fact tears down the native activatable collection.
    // That collection is UI bookkeeping in the headless host and needs no
    // scene/service state; skipping it keeps the real fact and restriction
    // teardown intact.
    private static bool SkipActivatableDetach() => false;

    // Context stat bonuses are verified through the real properties and stat
    // modifiers. Their native OnTurnOn also touches a scene service that the
    // detached host intentionally does not create; contain only that known
    // NullReference after the modifier has been applied.
    private static Exception IgnoreDetachedStatBonus(AddContextStatBonus __instance, Exception __exception)
        => __exception is NullReferenceException ? null : __exception;

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
        harmony.Patch(AccessTools.Method(typeof(ActivatableAbilityCollection), "OnFactWillDetach",
                new[] { typeof(ActivatableAbility) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(SkipActivatableDetach))));
        harmony.Patch(AccessTools.Method(typeof(AddContextStatBonus), "OnTurnOn"),
            finalizer: new HarmonyMethod(AccessTools.Method(typeof(Occultist0099NativeTests), nameof(IgnoreDetachedStatBonus))));
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
        var startupBridges = normalStartup.Where(Occultist0099Slice.IsRuntimePatch).ToArray();
        Check(normalStartup.Any(type => !Occultist0099Slice.IsRuntimePatch(type)) &&
              new HashSet<Type>(startupBridges).SetEquals(bridges),
            "HOST ordinary startup discovers every Occultist production bridge and keeps the other mods' patches");
        foreach (var type in bridges) harmony.CreateClassProcessor(type).Patch();
        harmony.CreateClassProcessor(Production("MimicMetamagicEligibility01458Patch")).Patch();
        harmony.CreateClassProcessor(Production("MimicMetamagicRecalculate01458Patch")).Patch();
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
            Set(value, "PermanentValue", score);
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
                                      "SpellsPerLevel", "CantripsType", "IsMythic", "HitDie", "SkillPoints", "CanTargetPoint", "CustomRange",
                                      "Shape", "Size", "SpellResistance", "AffectEnemies", "AggroEnemies" })
        {
            var info = AccessTools.Field(type, field);
            var value = data[field];
            if (info == null || value == null || value.Type == JTokenType.Null) continue;
            try { info.SetValue(bp, value.ToObject(info.FieldType)); } catch { }
        }
        foreach (var field in new[] { "m_Parent", "m_SpellList", "m_CharacterClass", "m_SpellsPerDay",
                                      "m_Type", "m_Enchantments", "m_DamageType", "m_AttackType",
                                      "m_SpellsKnown", "m_SpellSlots", "m_BaseAttackBonus", "m_FortitudeSave", "m_WillSave",
                                      "m_Features", "m_AllFeatures", "m_Buff" })
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

    // The native familiar selection is the authority: each bond feature grants one
    // activatable ability whose buff carries the native AddFamiliar critter.
    private static string[] FamiliarDonorIds()
    {
        var selection = Native<BlueprintFeatureSelection>("363cab72f77c47745bf3a8807074d183");
        var ids = new List<string>();
        foreach (var reference in (BlueprintFeatureReference[])Get(selection, "m_AllFeatures"))
        {
            ids.Add(reference.Guid.ToString());
            var feature = Native<BlueprintFeature>(reference.Guid.ToString());
            foreach (var granted in (BlueprintUnitFactReference[])Get(feature.GetComponent<AddFacts>(), "m_Facts"))
            {
                ids.Add(granted.Guid.ToString());
                var toggle = Native<BlueprintActivatableAbility>(granted.Guid.ToString());
                if (toggle?.Buff != null) ids.Add(toggle.Buff.AssetGuid.ToString());
            }
        }
        return ids.Distinct().ToArray();
    }

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

    private static void StageNecromancyRoots(IEnumerable<Occultist0099SpellRoot> published)
    {
        // Bestow Curse variants and their buffs, the Screech icon, the Suffocation
        // icon and the native staggered buff. All from the audited archive.
        LoadRows("8e7cfa5f213a90549aadd18f8f6f4664", "69851cc3b821c2d479ac1f2d86e8ffa5", "0c853a9f35a7bf749821ebe5d06fade7",
            "a9c4c2552b9a9564e8ee2eb3b20cc66d", "f428939d629fa9942a55e1a144edd294", "caae9592917719a41b601b678a8e6ddf",
            "c092750ba895e014cb24a25e2e8274a7", "7fbb7799e8684434e80487cef9cc7f09", "de92c96c86cb2cd4c8eb8e2881b84d99",
            "cc0aeb74b35cb7147bff6c53538bbc76", "df3950af5a783bd4d91ab73eb8fa0fd3");
        var factory = Activator.CreateInstance(Production("PsychicAssets"), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { cache }, null);
        AccessTools.Method(Production("PsychicCurseSpells"), "Build").Invoke(null, new[] { factory });
        AccessTools.Method(Production("PsychicCataclysmSpells"), "Suffocate").Invoke(null, new[] { factory });
        var assets = (System.Collections.IDictionary)Get(factory, "Assets");
        foreach (var asset in assets.Values.Cast<BlueprintScriptableObject>())
        {
            asset.OnEnable();
            cache.AddCachedBlueprint(asset.AssetGuid, asset);
        }
        foreach (var row in published)
            Check(Native<BlueprintAbility>(row.Guid)?.GetComponent<SpellComponent>()?.School.ToString() == row.School,
                "HOST the existing CotW builder publishes the Necromancy root " + row.Name);
    }

    private static void StageConjurationRoots(IEnumerable<Occultist0099SpellRoot> published)
    {
        var factory = Activator.CreateInstance(Production("PsychicAssets"), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { cache }, null);
        // Blade Tutor and the two fog roots come from the existing CotW builders.
        AccessTools.Method(Production("PsychicBladeTutor"), "Build").Invoke(null, new object[] { factory });
        AccessTools.Method(Production("PsychicWeatherSpells"), "Mist").Invoke(null, new object[] { factory, false });
        AccessTools.Method(Production("PsychicWeatherSpells"), "Mist").Invoke(null, new object[] { factory, true });
        var assets = (System.Collections.IDictionary)Get(factory, "Assets");
        foreach (var asset in assets.Values.Cast<BlueprintScriptableObject>())
        {
            asset.OnEnable();
            cache.AddCachedBlueprint(asset.AssetGuid, asset);
        }
        foreach (var id in new[] { "91651f6f9af8b82e5aff6e2cb7aedfa3", "ea8d3fc6fe47a0ce20df9b472eebf1a3" })
            Check(Native<BlueprintAbility>(id)?.GetComponent<SpellComponent>()?.School == SpellSchool.Conjuration,
                "HOST the existing CotW weather builder publishes the fog root " + id);
        foreach (var row in published)
            Check(Native<BlueprintAbility>(row.Guid)?.GetComponent<SpellComponent>()?.School.ToString() == row.School,
                "HOST the existing CotW builder publishes the Conjuration root " + row.Name);
    }

    private static void StageEvocationRoots(IEnumerable<Occultist0099SpellRoot> published)
    {
        var factoryType = Production("PsychicAssets");
        // The existing CotW builders read native icon, effect and weapon donors. Load
        // exactly the rows each builder asks for from the audited archive, and count
        // them. Each retry uses a fresh factory so a partial graph cannot collide.
        var loadedDonors = new List<string>();
        void Build(string type, string method)
        {
            for (int attempt = 0; attempt < 400; attempt++)
            {
                var attemptFactory = Activator.CreateInstance(factoryType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { cache }, null);
                try
                {
                    AccessTools.Method(Production(type), method, new[] { factoryType }).Invoke(null, new[] { attemptFactory });
                    foreach (var asset in ((System.Collections.IDictionary)Get(attemptFactory, "Assets")).Values.Cast<BlueprintScriptableObject>())
                    {
                        asset.OnEnable();
                        cache.AddCachedBlueprint(asset.AssetGuid, asset);
                    }
                    return;
                }
                catch (TargetInvocationException error)
                {
                    var inner = error.InnerException;
                    const string marker = "Psychic native dependency missing: ";
                    if (inner == null || !inner.Message.StartsWith(marker, StringComparison.Ordinal)) throw;
                    string id = inner.Message.Substring(marker.Length).Trim();
                    if (loadedDonors.Contains(id)) throw;
                    LoadRows(id);
                    if (cache.Load(P(id)) == null) throw;
                    loadedDonors.Add(id);
                }
            }
            throw new InvalidOperationException("Evocation staging did not converge for " + type);
        }
        // ForceIcon decodes an embedded PNG into a Unity texture, which needs a
        // Unity runtime. The icon is presentation only; the spell graph is not.
        var forceIcon = AccessTools.Method(Production("PsychicRestorativeSpells"), "ForceIcon");
        var iconBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(HostIcon));
        harmony.Patch(forceIcon, prefix: new HarmonyMethod(iconBoundary));
        try
        {
        foreach (var type in new[] { "PsychicOrbSpells", "PsychicSpiritWeapons", "PsychicStorageSpells" }) Build(type, "Build");
        Build("PsychicRestorativeSpells", "ForceSword");
        Build("PsychicWeatherSpells", "River");
        }
        finally { harmony.Unpatch(forceIcon, iconBoundary); }
        Console.WriteLine("0099N FIXTURE: Evocation CotW builders read " + loadedDonors.Count + " extra audited native donor rows");
        foreach (var row in published)
            Check(Native<BlueprintAbility>(row.Guid)?.GetComponent<SpellComponent>()?.School.ToString() == row.School,
                "HOST the existing CotW builder publishes the Evocation root " + row.Name);
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

    // A hand-applied native selection commits facts while the rebuild window is
    // open, so InFactContext stays closed for the components that fire during
    // Apply. The game closes that window through Occultist0099FocusPart's
    // level-up postfix and the preview finalizer, both of which end in this
    // production entry point; the fixture uses the same one.
    private static void Rebuild(UnitEntityData u)
        => AccessTools.Method(typeof(Occultist0099FocusPart), "RefreshBuild",
            new[] { typeof(UnitEntityData), typeof(bool) }).Invoke(null, new object[] { u, false });

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
        {
            rule.Context.CommandType = command.Type;
            AccessTools.Method(Production("Occultist0099NativeCast"), "BindCommand")
                .Invoke(null, new object[] { rule, command });
        }
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

    // MF08 native wiring: the level-20 Implement Mastery selection binds the
    // four-point own-school allowance, effective level and resonance re-capture
    // through Occultist0099ImplementMastery. Numbers mirror the model MF08
    // (L=20, Int +5, A=2, Transmutation=1, mastery Abjuration).
    private static void MasteryNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("mastery", character, 20, 20);
        var saves = new[] { unit.Stats.SaveFortitude, unit.Stats.SaveReflex, unit.Stats.SaveWill };
        var baseline = saves.Select(stat => stat.ModifiedValue).ToArray();
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var abjurationChoice = new Feature(Asset<BlueprintFeature>("slot-1-Abjuration-Implement"), unit.Descriptor);
        var transmutationChoice = new Feature(Asset<BlueprintFeature>("slot-2-Transmutation-Implement"), unit.Descriptor);
        unit.Facts.Add(abjurationChoice);
        unit.Facts.Add(transmutationChoice);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 20);
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var selection = Asset<BlueprintFeatureSelection>("ImplementMasterySelection");
        Eq(8, selection.AllFeatures.Length, "MF08 the mastery selection offers exactly the eight base schools");
        Check(character.Progression.LevelEntries.Single(entry => entry.Level == 20).Features.Contains(selection),
            "MF08 the career grants the mastery selection at level 20");
        var leveling = LevelState(book);
        var masterySelection = leveling.AddSelection(null, character.Progression, selection, 19);
        var pickAbjuration = new SelectFeature(masterySelection, Asset<BlueprintFeature>("AbjurationImplementMasteryFeature"));
        var pickDivination = new SelectFeature(masterySelection, Asset<BlueprintFeature>("DivinationImplementMasteryFeature"));
            Check(pickAbjuration.Check(leveling, unit.Descriptor) && !pickDivination.Check(leveling, unit.Descriptor),
                "MF08 mastery requires the learned base implement and refuses an unlearned school");
        var barrier = unit.Abilities.Enumerable.Single(ability => ability.Blueprint.AssetGuid == P(Occultist0099Ids.MindBarrierAbilityGuid)).Data;
        var barrierLogic = (Occultist0099FocusResourceLogic)barrier.ResourceLogic;
        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 2; i++) Check(CastRule(Granted("InvestFocusAbility")).Success, "MF08 native Abjuration investment");
            Check(CastRule(Granted("TransmutationInvestFocusAbility")).Success, "MF08 native Transmutation investment");
            Check(CastRule(Granted("LockFocusAbility")).Success, "MF08 native focus lock");
            Eq(22, focus.Generic, "MF08 G=22 after investing three of the 25-point budget");
            Eq(1, Occultist0099Focus.Resonance(focus, "Abjuration"), "MF08 locked Abjuration resonance before mastery");
            Eq(0, Occultist0099Focus.Resonance(focus, "Transmutation"), "MF08 one invested Transmutation point gives no physical resonance");
            Check(saves.Select((stat, i) => stat.ModifiedValue - baseline[i]).All(delta => delta == 1),
                "MF08 the native ward applies +1 to all three saves before mastery");
            pickAbjuration.Apply(leveling, unit.Descriptor);
            leveling.OnApplyAction();
            Eq("Abjuration", focus.MasterySchool, "MF08 the native selection binds mastery Abjuration");
            Eq(4, focus.MasteryRemaining, "MF08 mastery grants H=4");
            Eq(0, focus.MasterySpent, "MF08 mastery starts with T=0");
            InFocus(unit, f =>
            {
                Eq(24, f.MindBarrier("Abjuration").EffectiveLevel, "MF08 own-school effective level L+4");
                Eq(48, f.MindBarrier("Abjuration").Absorption, "MF08 own-school absorption 48");
                Eq(27, f.MindBarrier("Abjuration").SaveDC, "MF08 own-school DC 27");
                Eq(20, f.MindBarrier("Transmutation").EffectiveLevel, "MF08 other school keeps effective level L");
                Eq(25, f.MindBarrier("Transmutation").SaveDC, "MF08 other school keeps DC 25");
            });
            // The fifth equipment tier is internal to the production assembly;
            // AegisNative already pins it through the real Aegis Budget path.
            Eq(3, Occultist0099Focus.Resonance(focus, "Abjuration"),
                "MF08 the four mastery points enter the own-school resonance snapshot");
            Eq(0, Occultist0099Focus.Resonance(focus, "Transmutation"),
                "MF08 mastery leaves the other school's resonance untouched");
            Check(unit.Buffs.GetBuff(Asset<BlueprintBuff>("WardingTalismanBuff")) != null &&
                  saves.Select((stat, i) => stat.ModifiedValue - baseline[i]).All(delta => delta == 3),
                "MF08 the native ward re-syncs to +3 on all three saves from the mastery snapshot");
            Eq(20, book.BaseLevel, "MF08 the private book caster level stays the actual L=20");
            // Available is the ordinary budget (G + sum R); the mastery H is a
            // separate own-school allowance and never enters the mirror.
            Eq(25, focus.Available, "MF08 available counts G plus R, not the mastery allowance");
            Eq(25, unit.Resources.GetResourceAmount(Asset<BlueprintAbilityResource>("MentalFocusResource")),
                "MF08 the native resource mirror projects the ordinary budget");
            Check(barrierLogic.IsAbilityRestrictionPassed(barrier), "MF08 the barrier precheck passes with mastery");
            Check(CastRule(barrier).Success, "MF08 the native Mind Barrier cast succeeds");
            Eq(3, focus.MasteryRemaining, "MF08 H pays first: H=3");
            Eq(1, focus.MasterySpent, "MF08 T=1");
            Eq(2, focus.Schools["Abjuration"].Remaining, "MF08 ordinary R stays 2");
            Eq(0, focus.Spent, "MF08 U counts ordinary points only");
            Eq(48, unit.Stats.TemporaryHitPoints.ModifiedValue, "MF08 the native barrier applies 48 temporary hit points");
            Eq(3, Occultist0099Focus.Resonance(focus, "Abjuration"), "MF08 spending keeps the locked resonance snapshot");
            // A new native level-up state grants the third implement slot, the
            // same shape the Trappings check uses; the earlier mastery state is
            // already selected and cannot grant a second feature.
            var trappingsLeveling = LevelState(book);
            var trappingsSelection = trappingsLeveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("slot-3-ImplementSelection"), 2);
            var trappingsPick = new SelectFeature(trappingsSelection, Asset<BlueprintFeature>("slot-3-TrappingsOfTheWarrior"));
            Check(trappingsPick.Check(trappingsLeveling, unit.Descriptor), "MF08 Trappings is selectable with both member implements");
            trappingsPick.Apply(trappingsLeveling, unit.Descriptor);
            trappingsLeveling.OnApplyAction();
            Rebuild(unit);
            var resonanceBuff = Asset<BlueprintBuff>("TrappingsOfTheWarriorResonanceBuff");
            var counterToggle = unit.ActivatableAbilities.Enumerable.Single(ability =>
                ability.Blueprint == Asset<BlueprintActivatableAbility>("CounterstrikeToggle"));
            // Trappings grants one base attack bonus per four locked points with
            // a cap at the occultist level, so three locked points unlock
            // CounterstrikeToggle and stay below the four-point Martial Skill threshold.
            Check(counterToggle.IsAvailable && !counterToggle.IsOn &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("CounterstrikeBuff")) == null &&
                  unit.Buffs.GetBuff(resonanceBuff) == null,
                "MF08 three locked points unlock CounterstrikeToggle and stay below the four-point Martial Skill threshold");
            counterToggle.IsOn = true;
            Check(counterToggle.IsOn && unit.Buffs.GetBuff(Asset<BlueprintBuff>("CounterstrikeBuff")) != null,
                "MF08 arming CounterstrikeToggle attaches CounterstrikeBuff");
            var panoplyPayment = Occultist0099Focus.PayPanoply(focus, "Trappings of the Warrior", 1, "mf08-panoply");
            Eq(Occultist0099Outcome.Ok, panoplyPayment.Outcome, "MF08 Panoply payment succeeds");
            Eq(1, panoplyPayment.FromMembers["Abjuration"], "MF08 the tie takes Abjuration first in A JSON member order");
            Eq(3, focus.MasteryRemaining, "MF08 Panoply payment never touches H");
            Eq(1, focus.MasterySpent, "MF08 T stays 1 after the Panoply payment");
            Eq(1, focus.Schools["Abjuration"].Remaining, "MF08 Abjuration R=1 after the Panoply point");
            Eq(1, focus.Schools["Transmutation"].Remaining, "MF08 Transmutation R=1 untouched");
            Eq(3, Occultist0099Focus.PanoplyResonance(focus, "Trappings of the Warrior"),
                "MF08 the Panoply snapshot keeps the locked member investments");
            // The switch is a fresh native selection window (a same-day respec
            // grants the level-20 choice again); SelectFeature.Check refuses a
            // second pick on an already-selected FeatureSelectionState.
            var switchLeveling = LevelState(book);
            var switchSelection = switchLeveling.AddSelection(null, character.Progression, selection, 19);
            var pickTransmutation = new SelectFeature(switchSelection, Asset<BlueprintFeature>("TransmutationImplementMasteryFeature"));
            // The native level-up replaces the level-20 choice instead of
            // stacking a second mastery feature; drop the old fact first so the
            // switch path here matches what the game applies.
            unit.Facts.Remove(unit.Progression.Features.Enumerable.Single(f => f.Blueprint == Asset<BlueprintFeature>("AbjurationImplementMasteryFeature")));
            Check(pickTransmutation.Check(switchLeveling, unit.Descriptor), "MF08 a same-day mastery switch to a learned school is legal");
            pickTransmutation.Apply(switchLeveling, unit.Descriptor);
            switchLeveling.OnApplyAction();
            Rebuild(unit);
            Eq("Transmutation", focus.MasterySchool, "MF08 the switch rebinds mastery to Transmutation");
            Eq(1, focus.MasterySpent, "MF08 T carries across the switch");
            Eq(3, focus.MasteryRemaining, "MF08 H never exceeds 4-T after the switch");
            InFocus(unit, f =>
            {
                Eq(20, f.MindBarrier("Abjuration").EffectiveLevel, "MF08 the old mastery-only effective level is revoked");
                Eq(25, f.MindBarrier("Abjuration").SaveDC, "MF08 the old mastery-only DC is revoked");
                Eq(24, f.MindBarrier("Transmutation").EffectiveLevel, "MF08 the new mastery school gains L+4");
            });
            Eq(1, Occultist0099Focus.Resonance(focus, "Abjuration"), "MF08 the old school's mastery resonance contribution is revoked");
            Check(saves.Select((stat, i) => stat.ModifiedValue - baseline[i]).All(delta => delta == 1),
                "MF08 the native ward returns to +1 after the old mastery contribution is revoked");
            Check(CastRule(barrier).Success, "MF08 the barrier still casts after the switch");
            Eq(3, focus.MasteryRemaining, "MF08 after the switch the ordinary R pays: H stays 3");
            Eq(0, focus.Schools["Abjuration"].Remaining, "MF08 Abjuration R=0");
            Eq(2, focus.Spent, "MF08 U=2 counts both ordinary payments");
            unit.Facts.Remove(unit.Progression.Features.Enumerable.Single(f => f.Blueprint == Asset<BlueprintFeature>("TransmutationImplementMasteryFeature")));
            Check(focus.MasterySchool == null && focus.MasterySpent == 1 && focus.MasteryRemaining == 3,
                "MF08 losing the mastery source drops the identity but keeps the cycle's T/H record");
            InFocus(unit, f => Eq(20, f.MindBarrier("Transmutation").EffectiveLevel,
                "MF08 the new school's mastery-only contribution is revoked with its source"));
            // Losing the mastery source keeps the cycle's T/H record; a fresh
            // native selection window re-grants the choice (same-day respec).
            var regrantLeveling = LevelState(book);
            var regrantSelection = regrantLeveling.AddSelection(null, character.Progression, selection, 19);
            var regrantPick = new SelectFeature(regrantSelection, Asset<BlueprintFeature>("AbjurationImplementMasteryFeature"));
            Check(regrantPick.Check(regrantLeveling, unit.Descriptor), "MF08 a re-grant window accepts the learned school again");
            regrantPick.Apply(regrantLeveling, unit.Descriptor);
            regrantLeveling.OnApplyAction();
            Rebuild(unit);
            Eq("Abjuration", focus.MasterySchool, "MF08 re-granting a mastery feature rebinds its school");
            Eq(3, focus.MasteryRemaining, "MF08 the rebind keeps H=3");
            Rest(unit);
            Eq("Abjuration", focus.MasterySchool, "MF08 a real rest re-grants mastery from the active feature (C:21)");
            Eq(4, focus.MasteryRemaining, "MF08 the fresh cycle restores H=4");
            Eq(0, focus.MasterySpent, "MF08 the fresh cycle clears T");
            Eq(25, focus.Generic, "MF08 the fresh cycle restores the full budget");
            Check(!focus.Locked && unit.Buffs.GetBuff(Asset<BlueprintBuff>("WardingTalismanBuff")) == null,
                "MF08 the rest unlocks the ledger and removes the derived ward");
            for (int i = 0; i < 2; i++) CastRule(Granted("InvestFocusAbility"));
            CastRule(Granted("TransmutationInvestFocusAbility"));
            CastRule(Granted("LockFocusAbility"));
            Eq(3, Occultist0099Focus.Resonance(focus, "Abjuration"),
                "MF08 the re-granted mastery re-enters the resonance snapshot after the next lock");
            Check(saves.Select((stat, i) => stat.ModifiedValue - baseline[i]).All(delta => delta == 3),
                "MF08 the re-locked ward applies +3 again");
            unit.Facts.Remove(abjurationChoice);
            Check(focus.MasterySchool == null && !focus.Schools["Abjuration"].Present &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("WardingTalismanBuff")) == null,
                "MF08 losing the mastered school's implement drops the mastery identity and its ward");
            InFocus(unit, f => Eq(20, f.MindBarrier("Transmutation").EffectiveLevel,
                "MF08 no virtual level survives the mastered school's loss"));
        }
        finally { applyNativeEffects = false; }
        caseStatus["MF08"] = "native_mastery_selection_allowance_effective_level_resonance_switch_rest_and_source_loss";
    }

    // MF05 native wiring: level-8 Repower Construct charges one generic point
    // through Occultist0099FocusResourceLogic (PayGeneric, legal before lock),
    // accepts only a destroyed enemy construct, records the corpse once, and
    // summons a friendly construct through RuleSummonUnit into its own pool
    // with the 8/12/16 lifetime ladder and expiration cleanup.
    private static void RepowerNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var playerFaction = new BlueprintFaction { name = "0099N.RepowerPlayerFaction", AssetGuid = P(Occultist0099Ids.Id("HostRepowerPlayerFaction")) };
        Set(playerFaction, "m_AttackFactions", Array.Empty<BlueprintFactionReference>());
        Set(playerFaction, "m_AllyFactions", Array.Empty<BlueprintFactionReference>());
        playerFaction.OnEnable();
        var enemyFaction = new BlueprintFaction { name = "0099N.RepowerEnemyFaction", AssetGuid = P(Occultist0099Ids.Id("HostRepowerEnemyFaction")) };
        Set(enemyFaction, "m_AttackFactions", new[] { Ref<BlueprintFactionReference>(playerFaction) });
        Set(enemyFaction, "m_AllyFactions", Array.Empty<BlueprintFactionReference>());
        enemyFaction.OnEnable();

        void BindFaction(UnitEntityData target, BlueprintFaction faction)
        {
            Set(target.Descriptor, "m_Faction", faction);
            Set(target.Descriptor, "m_IsPlayerFactionCached", (bool?)null);
            var attackFactions = new UnitAttackFactions(target.Descriptor);
            attackFactions.Match(faction.AttackFactions);
            Set(target.Descriptor, "AttackFactions", attackFactions);
            Set(target.Descriptor, "HasOwnInventory", true);
            Set(target.Descriptor, "m_Inventory", new Kingmaker.Items.ItemsCollection(target));
            target.Group.ResetFactionSet();
            target.Group.UpdateAttackFactionsCache();
        }

        var summonedUnitBuff = new BlueprintBuff
        {
            name = "0099N.SummonedUnitBuff", AssetGuid = P(Occultist0099Ids.Id("HostSummonedUnitBuff")),
            ComponentsArray = new BlueprintComponent[] { new Kingmaker.UnitLogic.Buffs.Components.SummonedUnitBuff { name = "0099N.SummonedUnit" } }
        };
        var appearBuff = new BlueprintBuff
        {
            name = "0099N.SummonedUnitAppearBuff", AssetGuid = P(Occultist0099Ids.Id("HostSummonedUnitAppearBuff")),
            ComponentsArray = Array.Empty<BlueprintComponent>()
        };
        var dismemberedBuff = new BlueprintBuff
        {
            name = "0099N.DismemberedUnitBuff", AssetGuid = P(Occultist0099Ids.Id("HostDismemberedUnitBuff")),
            ComponentsArray = new BlueprintComponent[] { new Kingmaker.UnitLogic.FactLogic.DropLootAndDestroyOnDeactivate { name = "0099N.DropLootDestroy" } }
        };
        foreach (var sb in new[] { summonedUnitBuff, appearBuff, dismemberedBuff }) sb.OnEnable();
        var rootRefCached = AccessTools.Property(typeof(BlueprintReferenceBase), "Cached");
        var oldRootRefCached = rootRefCached.GetValue(ResourcesLibrary.RootRef, null);
        var oldPlayerFaction = Get(EnchantmentFixtureRoot, "m_PlayerFaction");
        var oldSystemMechanics = EnchantmentFixtureRoot.SystemMechanics;
        var sysMechanics = new Kingmaker.Blueprints.Root.SystemMechanicsRoot();
        Set(sysMechanics, "m_SummonedUnitBuff", Ref<BlueprintBuffReference>(summonedUnitBuff));
        Set(sysMechanics, "m_SummonedUnitAppearBuff", Ref<BlueprintBuffReference>(appearBuff));
        Set(sysMechanics, "m_DismemberedUnitBuff", Ref<BlueprintBuffReference>(dismemberedBuff));
        Set(EnchantmentFixtureRoot, "m_PlayerFaction", Ref<BlueprintFactionReference>(playerFaction));
        EnchantmentFixtureRoot.SystemMechanics = sysMechanics;
        rootRefCached.SetValue(ResourcesLibrary.RootRef, EnchantmentFixtureRoot, null);

        var game = Kingmaker.Game.Instance;
        var oldEntityCreator = game.EntityCreator;
        var oldUnits = Get(game.State, "Units");
        var oldTbmMandatory = game.Player.IsTurnBasedStateMandatory;
        var oldTbmValue = game.Player.MandatoryTurnBasedModeState;
        var previousTime = currentTime;
        Set(game, "EntityCreator", Empty<EntityCreationController>());
        Set(game.State, "Units", new Kingmaker.EntitySystem.EntityPool<UnitEntityData>(_ => true));
        game.Player.IsTurnBasedStateMandatory = true;
        game.Player.MandatoryTurnBasedModeState = false;
        servantPools = new Kingmaker.AreaLogic.SummonPool.SummonPoolsManager();
        repowerSpawnNull = false;
        repowerSpawned = null;
        repowerEnemyFaction = enemyFaction;
        repowerConstructType = null;
        repowerCharacter = character;

        var resourcesRoot = AccessTools.Method(typeof(ResourcesLibrary), nameof(ResourcesLibrary.GetRoot));
        var resourcesRootBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(RepowerRootBoundary));
        var spawnMethod = AccessTools.Method(typeof(EntityCreationController), nameof(EntityCreationController.SpawnUnit),
            new[] { typeof(BlueprintUnit), typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(Kingmaker.EntitySystem.SceneEntitiesState), typeof(string) });
        var spawnBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(RepowerSpawnBoundary));
        var poolsGetter = AccessTools.PropertyGetter(typeof(Kingmaker.Game), "SummonPools");
        var poolsBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantPoolsBoundary));
        var timerMethod = AccessTools.Method(typeof(UniRx.Observable), nameof(UniRx.Observable.Timer), new[] { typeof(TimeSpan) });
        var timerBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(RepowerTimerBoundary));
        var loggerInstance = AccessTools.PropertyGetter(typeof(Owlcat.Runtime.Core.Logging.Logger), "Instance");
        var loggerBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(AegisLoggerInstanceBoundary));
        var loggerLog = AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.Logger), "Log");
        var loggerNoop = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoReplenish));
        var errorWithReportMethods = typeof(Kingmaker.QA.LogChannelEx).GetMethods()
            .Where(method => method.Name == nameof(Kingmaker.QA.LogChannelEx.ErrorWithReport))
            .ToArray();
        var canTarget = AccessTools.Method(typeof(AbilityData), nameof(AbilityData.CanTarget), new[] { typeof(TargetWrapper) });
        var legalBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(LegalTargetBoundary));
        harmony.Patch(resourcesRoot, prefix: new HarmonyMethod(resourcesRootBoundary));
        harmony.Patch(spawnMethod, prefix: new HarmonyMethod(spawnBoundary));
        harmony.Patch(poolsGetter, prefix: new HarmonyMethod(poolsBoundary));
        harmony.Patch(timerMethod, prefix: new HarmonyMethod(timerBoundary));
        harmony.Patch(loggerInstance, prefix: new HarmonyMethod(loggerBoundary));
        harmony.Patch(loggerLog, prefix: new HarmonyMethod(loggerNoop));
        foreach (var errorWithReport in errorWithReportMethods)
            harmony.Patch(errorWithReport, prefix: new HarmonyMethod(loggerNoop));
        harmony.Unpatch(canTarget, legalBoundary);

        Occultist0099FocusState focus = null;
        UnitEntityData corpse = null;
        try
        {
            var unit = Unit("repower", character, 8, 16);
            BindFaction(unit, playerFaction);
            Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
            var feature = Asset<BlueprintFeature>("RepowerConstructFeature");
            Check(character.Progression.LevelEntries.Single(entry => entry.Level == 8).Features.Contains(feature) &&
                  character.Progression.LevelEntries.Where(entry => entry.Level != 8)
                      .All(entry => !entry.Features.Contains(feature)),
                "MF05 the career grants Repower Construct at level 8 only");
            var ability = Asset<BlueprintAbility>("RepowerConstructAbility");
            var logic = ability.GetComponent<Occultist0099FocusResourceLogic>();
            Check(ability.Type == AbilityType.Supernatural && ability.ActionType == UnitCommand.CommandType.Swift &&
                  ability.Range == AbilityRange.Medium && ability.CanCastToDeadTarget && !ability.CanTargetFriends &&
                  logic.GenericPool && logic.CalculateCost(null) == 1,
                "MF05 Repower Construct is a swift supernatural medium-range power that targets enemy corpses and spends one generic point");
            var constructType = Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f");
            repowerConstructType = constructType;
            var consumedBuff = Asset<BlueprintFeature>("RepowerConstructConsumed");
            var lifecycleBuff = Asset<BlueprintBuff>("RepowerConstructLifecycleBuff");
            var pool = Asset<BlueprintSummonPool>("RepowerConstructSummonPool");

            corpse = Unit("repower-corpse", null, 0, 10);
            corpse.Blueprint.Visual = new UnitVisualParams { BloodPuddleFx = new Kingmaker.ResourceLinks.PrefabLink() };
            BindFaction(corpse, enemyFaction);
            corpse.Facts.Add(new Feature(constructType, corpse.Descriptor));
            Set(corpse.Descriptor.State, "<LifeState>k__BackingField", UnitLifeState.Dead);
            var allyCorpse = Unit("repower-ally-corpse", null, 0, 10);
            BindFaction(allyCorpse, playerFaction);
            allyCorpse.Facts.Add(new Feature(constructType, allyCorpse.Descriptor));
            Set(allyCorpse.Descriptor.State, "<LifeState>k__BackingField", UnitLifeState.Dead);
            var living = Unit("repower-living", null, 0, 10);
            BindFaction(living, enemyFaction);
            living.Facts.Add(new Feature(constructType, living.Descriptor));
            var undead = Unit("repower-undead", null, 0, 10);
            BindFaction(undead, enemyFaction);
            var restriction = ability.GetComponent<Occultist0099RepowerTarget>();

            var data = GrantAbility(unit, ability);
            focus = unit.Get<Occultist0099FocusPart>().State;
            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("slot-1-Abjuration-Implement"), unit.Descriptor));
            Eq(11, focus.Generic, "MF05 level 8 with Int +3 starts at G=11");
            Eq(Occultist0099Outcome.Ok, Occultist0099Focus.Invest(focus, "Abjuration", 4),
                "MF05 the cycle invests four points like the model fixture");
            Eq(7, focus.Generic, "MF05 the generic pool funds the investment");
            Check(logic.IsAbilityRestrictionPassed(data), "MF05 the native precheck accepts a payable generic pool");

            useRealFactions = true;
            applyNativeEffects = true;
            Check(corpse.State.IsDead && !corpse.IsPlayerFaction && corpse.Descriptor.AttackFactions.IsPlayerEnemy &&
                  restriction.IsTargetRestrictionPassed(unit, new TargetWrapper(corpse)) &&
                  data.CanTarget(new TargetWrapper(corpse)) &&
                  !restriction.IsTargetRestrictionPassed(unit, new TargetWrapper(allyCorpse)) &&
                  !data.CanTarget(new TargetWrapper(allyCorpse)) &&
                  !restriction.IsTargetRestrictionPassed(unit, new TargetWrapper(living)) &&
                  !data.CanTarget(new TargetWrapper(living)) &&
                  !restriction.IsTargetRestrictionPassed(unit, new TargetWrapper(undead)) &&
                  !restriction.IsTargetRestrictionPassed(unit, new TargetWrapper(unit)),
                "MF05 the native target check accepts only a destroyed non-player construct");

            repowerSpawnNull = true;
            var nullSpawnCast = Rulebook.Trigger(new RuleCastSpell(data, new TargetWrapper(corpse)));
            repowerSpawnNull = false;
            Check(repowerSpawned == null && focus.Generic == 7 && focus.Spent == 0 &&
                  !corpse.Descriptor.HasFact(consumedBuff) && !focus.ConsumedCorpses.Contains(corpse.UniqueId) &&
                  data.CanTarget(new TargetWrapper(corpse)),
                "MF05 when RuleSummonUnit produces no unit, Repower Construct spends neither generic focus nor the corpse record");

            bool reentrySkipped = false;
            repowerSpawnCallback = () =>
            {
                Rulebook.Trigger(new RuleCastSpell(data, new TargetWrapper(corpse)));
                reentrySkipped = repowerSpawned == null && focus.Generic == 7 &&
                    !focus.ConsumedCorpses.Contains(corpse.UniqueId);
            };
            var cast = Rulebook.Trigger(new RuleCastSpell(data, new TargetWrapper(corpse)));
            Check(reentrySkipped && focus.Generic == 6 &&
                servantPools.GetPool(pool)?.Count == 1 &&
                focus.ConsumedCorpses.Count == 1,
                "MF05 a nested cast on the same corpse creates one summon, spends one point, and records one corpse");
            Check(cast.Success && repowerSpawned != null,
                "MF05 live Repower Construct cast succeeds and triggers RuleSummonUnit");
            Eq(6, focus.Generic, "MF05 the general cost is exactly one point");
            Eq(1, focus.Spent, "MF05 U counts the general point");
            Check(corpse.State.IsDead && !corpse.IsPlayerFaction &&
                  corpse.Descriptor.HasFact(consumedBuff) && focus.ConsumedCorpses.Contains(corpse.UniqueId) &&
                  !restriction.IsTargetRestrictionPassed(unit, new TargetWrapper(corpse)),
                "MF05 the original enemy corpse stays dead and is retired once");
            Check(!repowerSpawned.State.IsDead && repowerSpawned.IsPlayerFaction &&
                  !repowerSpawned.Descriptor.AttackFactions.IsPlayerEnemy && unit.IsAlly(repowerSpawned) &&
                  repowerSpawned.Get<Kingmaker.UnitLogic.Parts.UnitPartSummonedMonster>()?.Summoner == unit &&
                  repowerSpawned.State.HasCondition(UnitCondition.Unlootable) &&
                  servantPools.GetPool(pool).Units.Contains(repowerSpawned),
                "MF05 RuleSummonUnit switches the enemy construct to the caster's faction, marks it summoned and registers it in the private pool");
            var activeLife = repowerSpawned.Buffs.GetBuff(lifecycleBuff);
            var activeSummon = repowerSpawned.Buffs.GetBuff(summonedUnitBuff);
            Check(activeLife != null && activeLife.IsNotDispelable && activeLife.TimeLeft == TimeSpan.FromSeconds(6) &&
                  activeSummon != null && activeSummon.TimeLeft == TimeSpan.FromSeconds(6),
                "MF05 the repowered summon carries both the non-dispellable lifecycle buff and native SummonedUnitBuff");

            var priorSpawn = repowerSpawned;
            var secondCast = Rulebook.Trigger(new RuleCastSpell(data, new TargetWrapper(corpse)));
            Check(!data.CanTarget(new TargetWrapper(corpse)) && !secondCast.Success &&
                  ReferenceEquals(repowerSpawned, priorSpawn) && focus.Generic == 6,
                "MF05 a second cast against the consumed corpse is refused by CanTarget without spending focus or spawning");

            currentTime = activeLife.EndTime;
            repowerSpawned.Buffs.TickBuff(activeLife, currentTime);
            Check(repowerSpawned.Buffs.GetBuff(lifecycleBuff) == null &&
                  servantPools.GetPool(pool).Count == 0 &&
                  repowerSpawned.Buffs.GetBuff(summonedUnitBuff) == null,
                "MF05 duration expiry unregisters the construct from the summon pool and removes SummonedUnitBuff");
            var dismember = repowerSpawned.Buffs.GetBuff(dismemberedBuff);
            Check(dismember != null && !repowerSpawned.DestroyMark,
                "MF05 removing SummonedUnitBuff enters native DismemberedUnitBuff cleanup");
            currentTime = dismember.EndTime;
            repowerSpawned.Buffs.TickBuff(dismember, currentTime);
            Check(repowerSpawned.DestroyMark,
                "MF05 native summon cleanup marks the expired repowered construct for destruction");
        }
        finally
        {
            applyNativeEffects = false;
            useRealFactions = false;
            currentTime = previousTime;
            harmony.Patch(canTarget, prefix: new HarmonyMethod(legalBoundary));
            foreach (var errorWithReport in errorWithReportMethods)
                harmony.Unpatch(errorWithReport, loggerNoop);
            harmony.Unpatch(resourcesRoot, resourcesRootBoundary);
            harmony.Unpatch(spawnMethod, spawnBoundary);
            harmony.Unpatch(poolsGetter, poolsBoundary);
            harmony.Unpatch(timerMethod, timerBoundary);
            harmony.Unpatch(loggerInstance, loggerBoundary);
            harmony.Unpatch(loggerLog, loggerNoop);
            Set(EnchantmentFixtureRoot, "m_PlayerFaction", oldPlayerFaction);
            EnchantmentFixtureRoot.SystemMechanics = oldSystemMechanics;
            rootRefCached.SetValue(ResourcesLibrary.RootRef, oldRootRefCached, null);
            Set(game, "EntityCreator", oldEntityCreator);
            Set(game.State, "Units", oldUnits);
            game.Player.IsTurnBasedStateMandatory = oldTbmMandatory;
            game.Player.MandatoryTurnBasedModeState = oldTbmValue;
            repowerSpawnNull = false;
            repowerSpawned = null;
            repowerSpawnCallback = null;
            repowerEnemyFaction = null;
            repowerConstructType = null;
            repowerCharacter = null;
            servantPools?.Dispose();
            servantPools = null;
        }

        var life = AccessTools.Method(typeof(Occultist0099RepowerConstruct), "LifeSpan");
        Check((TimeSpan)life.Invoke(null, new object[] { 8 }) == TimeSpan.FromSeconds(6) &&
              (TimeSpan)life.Invoke(null, new object[] { 12 }) == TimeSpan.FromSeconds(72) &&
              (TimeSpan)life.Invoke(null, new object[] { 16 }) == TimeSpan.FromMinutes(16),
            "MF05 the 8/12/16 ladder is one round, level rounds and level minutes");
        Eq(Occultist0099Outcome.Ok, Occultist0099Focus.Reset(focus), "MF05 the unspent investment resets");
        Eq(10, focus.Generic, "MF05 the reset restores the budget minus the general point");
        Eq(0, focus.Schools["Abjuration"].Invested, "MF05 the reset returns the school investment");
        Eq(1, focus.Spent, "MF05 the general point stays spent");
        Check(focus.ConsumedCorpses.Contains(corpse.UniqueId), "MF05 the corpse record survives the reset");
        caseStatus["MF05"] = "native_generic_payment_enemy_corpse_rule_summon_takeover_pool_and_expiry";
    }

    // COTW-0099 ordinary startup wiring. The published flag is still off, so
    // the module must reject a missing cache; the shared entry must refuse
    // any cache that is not the process cache, and
    // repeating the entry must never duplicate the class reference. The
    // transaction's own refusal matrix (missing donor, occupied identity,
    // interrupted write, optional late provider) runs through the detached
    // channel in RegistrationNative, which is the same RegisterCore body.
    private static void StartupNative(BlueprintsCache detached)
    {
        var startup = Production("Occultist0099Startup");
        var register = AccessTools.Method(startup, "Register");
        var enabled = (bool)AccessTools.Method(Production("Occultist0099Slice"), "StartupEnabled").Invoke(null, null);
        Check(enabled, "SP08 startup enables the complete Occultist in the TEST DLL");
        bool rejectedEmpty = false;
        try { register.Invoke(null, new object[] { null }); }
        catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { rejectedEmpty = true; }
        var publishedClass = cache.Load(P(Occultist0099Ids.ClassGuid));
        Check(rejectedEmpty && ReferenceEquals(cache.Load(P(Occultist0099Ids.ClassGuid)), publishedClass),
            "SP08 the enabled entry refuses a missing cache and leaves the process class object untouched");

        register.Invoke(null, new object[] { detached });
        register.Invoke(null, new object[] { detached });
        var shared = AccessTools.Method(Production("Occultist0099NativeRegistration"), "RegisterShared");
        Check(!(bool)shared.Invoke(null, new object[] { detached }) && !(bool)shared.Invoke(null, new object[] { null }),
            "SP08 the shared startup entry refuses a detached or missing cache");
        var references = (BlueprintCharacterClassReference[])Get(EnchantmentFixtureRoot.Progression, "m_CharacterClasses");
        Check(references.Count(reference => reference?.Guid == P(Occultist0099Ids.ClassGuid)) == 1,
            "SP08 the startup entry keeps exactly one class reference after repeated calls");

        // The shipped wiring itself: the module is called from ModEntry with
        // its own retry name, after the modules it depends on. The decline
        // branch (RegisterCore false -> InvalidOperationException) is the
        // retry signal that chain catches.
        string source = null;
        var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(hostReportPath)));
        for (int up = 0; up < 5 && directory != null && source == null; up++, directory = directory.Parent)
        {
            var file = Path.Combine(directory.FullName, "src", "ModEntry.cs");
            if (File.Exists(file)) source = file;
        }
        Check(source != null, "SP08 the shipped ModEntry source is reachable from the report path");
        if (source == null) return;
        var text = File.ReadAllText(source);
        int module = text.IndexOf("Occultist0099Startup.ModuleName", StringComparison.Ordinal);
        int shield = text.IndexOf("WayOfTheShieldIntegration.ModuleName", StringComparison.Ordinal);
        var call = module < 0 ? "" : text.Substring(module, Math.Min(240, text.Length - module));
        Check(shield >= 0 && module > shield && call.Contains("Occultist0099Startup.Register(cache)") &&
              call.Contains("ReportModule"),
            "SP08 the ordinary startup chain calls the Occultist module with its own retry name after the earlier modules");

        // Controlled shared-startup check: verify real ModEntry -> Occultist0099Startup
        // -> RegisterShared localization publishing, LocalizationReadyPatch re-application,
        // and ReconcileOptional late-donor refresh in an isolated BlueprintsCache while
        // using the enabled production gate and restoring static state afterwards.
        var sharedCache = new BlueprintsCache();
        Set(sharedCache, "m_PackSerializer", FormatterServices.GetUninitializedObject(
            AccessTools.Field(typeof(BlueprintsCache), "m_PackSerializer").FieldType));
        var tokenIds = ((string[])AccessTools.Method(Production("Occultist0099NativeRegistration"), "IdentityTokens")
            .Invoke(null, new object[] { detached })).Select(t => P(Occultist0099Ids.Id(t))).ToHashSet();
        var rootGuid = P("2d77316c72b9ed44f888ceefc2a131f6");
        var cloakGuid = P(Occultist0099AbjurationCatalog.CloakOfWinds);
        var wizardListGuid = P("ba0401fdeb4062f40a7aa95b6f07fe89");
        var detachedEntries = (System.Collections.IDictionary)Get(detached, "m_LoadedBlueprints");
        foreach (System.Collections.DictionaryEntry entry in detachedEntries)
        {
            var id = (BlueprintGuid)entry.Key;
            if (tokenIds.Contains(id) || id == rootGuid || id == cloakGuid || id == wizardListGuid) continue;
            var bp = Get(entry.Value, "Blueprint") as SimpleBlueprint;
            if (bp != null) sharedCache.AddCachedBlueprint(id, bp);
        }
        var alchemist = (BlueprintCharacterClass)detached.Load(P("0937bec61c0dabc468428f496580c721"));
        var sharedRoot = Empty<BlueprintRoot>();
        sharedRoot.AssetGuid = rootGuid;
        sharedRoot.name = "BlueprintRoot";
        Set(sharedRoot, "Progression", new ProgressionRoot());
        Set(sharedRoot.Progression, "m_CharacterClasses", new[] { Ref<BlueprintCharacterClassReference>(alchemist) });
        sharedCache.AddCachedBlueprint(rootGuid, sharedRoot);
        var detachedWizardList = (BlueprintSpellList)cache.Load(wizardListGuid);
        var sharedWizardList = new BlueprintSpellList
        {
            AssetGuid = wizardListGuid,
            name = detachedWizardList.name,
            SpellsByLevel = detachedWizardList.SpellsByLevel.Select(row =>
            {
                var copy = new SpellLevelList(row.SpellLevel);
                if (row.SpellsRefs != null)
                    foreach (var r in row.SpellsRefs)
                        if (r != null && r.Guid != cloakGuid) copy.SpellsRefs.Add(r);
                return copy;
            }).ToArray()
        };
        sharedCache.AddCachedBlueprint(wizardListGuid, sharedWizardList);

        var modType = Production("ModEntry");
        var modField = AccessTools.Field(modType, "modification");
        var regCompletedField = AccessTools.Field(modType, "registrationCompleted");
        var deferredField = AccessTools.Field(modType, "deferredPatchesAttempted");
        var moduleStatesField = AccessTools.Field(modType, "moduleStates");
        var regsField = AccessTools.Field(modType, "registrations");
        var stringsField = AccessTools.Field(Production("PsychicAssets"), "Strings");

        var oldCache = ResourcesLibrary.BlueprintsCache;
        var oldPack = Kingmaker.Localization.LocalizationManager.CurrentPack;
        var oldMod = modField.GetValue(null);
        var oldRegCompleted = (bool)regCompletedField.GetValue(null);
        var oldDeferred = (bool)deferredField.GetValue(null);
        var oldStrings = new Dictionary<string, string>((Dictionary<string, string>)stringsField.GetValue(null));
        var oldStates = new Dictionary<string, string>((Dictionary<string, string>)moduleStatesField.GetValue(null));

        var dummyMod = Empty<Kingmaker.Modding.OwlcatModification>();
        Set(dummyMod, "Logger", Owlcat.Runtime.Core.Logging.LogChannelFactory.GetOrCreate("0099N.Startup"));
        var noop = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoReplenish));
        var mEnabled = AccessTools.Method(Production("Occultist0099Slice"), "StartupEnabled");
        var mRegInit = AccessTools.Method(Production("BlueprintRegistration"), "RegisterInitial");
        var mRetryInit = AccessTools.Method(Production("BlueprintRegistration"), "RetryInitial");
        var mRegOpt = AccessTools.Method(Production("BlueprintRegistration"), "RegisterOptional");
        var mMedium = AccessTools.Method(Production("MediumIntegration"), "Register");
        var mShieldReg = AccessTools.Method(Production("WayOfTheShieldIntegration"), "Register");
        var mShieldLoc = AccessTools.Method(Production("WayOfTheShieldIntegration"), "ApplyLocalization");
        var mDiag = AccessTools.Method(modType, "WriteDiagnostics");
        var mTryMenu = AccessTools.Method(modType, "TryMenu");
        var mModLoc = AccessTools.Method(typeof(Kingmaker.Modding.OwlcatModification), nameof(Kingmaker.Modding.OwlcatModification.ApplyLocalization));
        var mMenuLoc = AccessTools.Method(Production("ModMenuIntegration"), "RefreshLocalization");
        var sLoggerInstance = AccessTools.PropertyGetter(typeof(Owlcat.Runtime.Core.Logging.Logger), "Instance");
        var sLoggerBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(AegisLoggerInstanceBoundary));
        var sLoggerLog = AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.Logger), "Log");

        harmony.Patch(mRegInit, prefix: new HarmonyMethod(noop));
        harmony.Patch(mRetryInit, prefix: new HarmonyMethod(noop));
        harmony.Patch(mRegOpt, prefix: new HarmonyMethod(noop));
        harmony.Patch(mMedium, prefix: new HarmonyMethod(noop));
        harmony.Patch(mShieldReg, prefix: new HarmonyMethod(noop));
        harmony.Patch(mShieldLoc, prefix: new HarmonyMethod(noop));
        harmony.Patch(mDiag, prefix: new HarmonyMethod(noop));
        harmony.Patch(mTryMenu, prefix: new HarmonyMethod(noop));
        harmony.Patch(mModLoc, prefix: new HarmonyMethod(noop));
        harmony.Patch(mMenuLoc, prefix: new HarmonyMethod(noop));
        harmony.Patch(sLoggerInstance, prefix: new HarmonyMethod(sLoggerBoundary));
        harmony.Patch(sLoggerLog, prefix: new HarmonyMethod(noop));
        try
        {
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, sharedCache);
            var startPack = new Kingmaker.Localization.LocalizationPack();
            Kingmaker.Localization.LocalizationManager.CurrentPack = startPack;
            modField.SetValue(null, dummyMod);
            regCompletedField.SetValue(null, false);
            deferredField.SetValue(null, true);

            var cacheReady = AccessTools.Method(modType.GetNestedType("CacheReadyPatch", BindingFlags.NonPublic), "Postfix");
            var locReady = AccessTools.Method(modType.GetNestedType("LocalizationReadyPatch", BindingFlags.NonPublic), "Postfix");
            cacheReady.Invoke(null, new object[] { sharedCache });

            var sharedClass = sharedCache.Load(P(Occultist0099Ids.ClassGuid)) as BlueprintCharacterClass;
            Check(sharedClass != null, "SP08 shared startup registers OccultistCharacterClass into the active cache");
            if (sharedClass == null) return;
            register.Invoke(null, new object[] { sharedCache });
            var sharedReferences = (BlueprintCharacterClassReference[])Get(sharedRoot.Progression, "m_CharacterClasses");
            Check(sharedReferences.Count(reference => reference?.Guid == sharedClass.AssetGuid) == 1 &&
                  ReferenceEquals(sharedCache.Load(sharedClass.AssetGuid), sharedClass),
                "SP08 normal registration retries without replacing the class or adding a duplicate");
            string ResolveLoc(Kingmaker.Localization.LocalizedString ls) =>
                ls != null && !string.IsNullOrEmpty(ls.Key)
                    ? Kingmaker.Localization.LocalizationManager.CurrentPack?.GetText(ls.Key, false) ?? ""
                    : "";
            string ResolveDesc(BlueprintAbility ability) =>
                ability == null ? "" : ResolveLoc(Get(ability, "m_Description") as Kingmaker.Localization.LocalizedString);
            var sharedBarrier = sharedCache.Load(P(Occultist0099Ids.MindBarrierAbilityGuid)) as BlueprintAbility;
            var sharedRepower = sharedCache.Load(P(Occultist0099Ids.Id("RepowerConstructAbility"))) as BlueprintAbility;
            Eq("Occultist", ResolveLoc(sharedClass.LocalizedName),
                "SP08 shared startup registers the Occultist localized class name");
            var barrierDesc = ResolveDesc(sharedBarrier);
            var repowerDesc = ResolveDesc(sharedRepower);
            Check(!string.IsNullOrEmpty(barrierDesc) && barrierDesc.Contains("temporary hit points") &&
                  !string.IsNullOrEmpty(repowerDesc) && repowerDesc.Contains("destroyed construct"),
                "SP08 shared startup registers and resolves non-empty Occultist ability descriptions");

            var freshPack = new Kingmaker.Localization.LocalizationPack();
            Kingmaker.Localization.LocalizationManager.CurrentPack = freshPack;
            Eq("", ResolveLoc(sharedClass.LocalizedName),
                "SP08 a freshly replaced localization pack lacks Occultist strings before ApplyLocalization");
            locReady.Invoke(null, null);
            Eq("Occultist", ResolveLoc(sharedClass.LocalizedName),
                "SP08 LocalizationReadyPatch reapplies the Occultist class name onto a replaced pack");
            var reappliedBarrierDesc = ResolveDesc(sharedBarrier);
            Check(!string.IsNullOrEmpty(reappliedBarrierDesc) &&
                  reappliedBarrierDesc.Contains("temporary hit points"),
                "SP08 LocalizationReadyPatch reapplies Occultist ability descriptions onto a replaced pack");

            var sharedBaseList = sharedClass.Spellbook.SpellList;
            var sharedAbjurationList = sharedClass.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>()
                .ImplementLists.Single(slot => slot.SlotKey == "slot-1" && slot.School == "Abjuration").List.Get();
            var lateEffect = new BlueprintBuff
            {
                AssetGuid = P(Occultist0099Ids.Id("HostSharedLateProviderEffect")),
                name = "0099N.SharedLateProviderEffect", ComponentsArray = Array.Empty<BlueprintComponent>()
            };
            lateEffect.OnEnable();
            sharedCache.AddCachedBlueprint(lateEffect.AssetGuid, lateEffect);
            var lateCloak = ProviderSpell(Occultist0099AbjurationCatalog.CloakOfWinds, "CloakOfWindsAbility", SpellSchool.Abjuration, lateEffect);
            ProviderBacklink(lateCloak, sharedWizardList, 3);
            sharedWizardList.SpellsByLevel[3].SpellsRefs.Add(Ref<BlueprintAbilityReference>(lateCloak));
            sharedCache.AddCachedBlueprint(lateCloak.AssetGuid, lateCloak);
            Check(!sharedBaseList.GetSpells(3).Contains(lateCloak) && !sharedAbjurationList.GetSpells(3).Contains(lateCloak),
                "SP08 late optional donor is not in the published Occultist spell lists before ReconcileOptional");

            locReady.Invoke(null, null);
            Check(ReferenceEquals(sharedCache.Load(P(Occultist0099Ids.ClassGuid)), sharedClass) &&
                  sharedBaseList.GetSpells(3).Contains(lateCloak) &&
                  sharedAbjurationList.GetSpells(3).Contains(lateCloak),
                "SP08 ReconcileOptional refreshes published Occultist base and implement spell lists when a late optional donor arrives");

            var sharedCaster = Unit("shared-cloak-caster", sharedClass, 7, 16);
            var sharedBody = Empty<UnitBody>();
            Set(sharedBody, "Owner", sharedCaster.Descriptor);
            Set(sharedCaster.Descriptor, "Body", sharedBody);
            Set(sharedCaster.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
            sharedCaster.Facts.Add(new Feature((BlueprintFeature)sharedCache.Load(P(Occultist0099Ids.SpellbookFactGuid)), sharedCaster.Descriptor));
            sharedCaster.Facts.Add(new Feature((BlueprintFeature)sharedCache.Load(P(Occultist0099Ids.Id("slot-1-Abjuration-Implement"))), sharedCaster.Descriptor));
            var sharedBook = sharedCaster.Descriptor.DemandSpellbook(sharedClass.Spellbook);
            Set(sharedBook, "m_BaseLevelInternal", 7);
            sharedBook.Rest();
            var sharedLeveling = LevelState(sharedBook);
            var learnSharedCloak = new SelectSpell(sharedClass.Spellbook, sharedAbjurationList, 3, lateCloak, 0);
            Check(NativeCheck(learnSharedCloak, sharedLeveling, sharedCaster.Descriptor),
                "SP08 published optional Cloak can be learned from the published shared implement list");
            learnSharedCloak.Apply(sharedLeveling, sharedCaster.Descriptor);
            var learnedCloak = sharedBook.GetKnownSpells(3).Single(s => s.Blueprint == lateCloak);
            bool previousApplyEffects = applyNativeEffects;
            applyNativeEffects = true;
            try
            {
                Check(learnedCloak.IsAvailable && learnedCloak.IsAvailableForCast && sharedBook.CanSpend(learnedCloak) &&
                      CastRule(learnedCloak).Success && sharedCaster.Buffs.GetBuff(lateEffect) != null,
                    "SP08 learned optional Cloak casts normally while the provider remains published");
                sharedCaster.Buffs.GetBuff(lateEffect)?.Remove();
                int slotsBeforeWithdraw = sharedBook.GetSpontaneousSlots(3);
                Check(slotsBeforeWithdraw > 0, "SP08 shared caster holds spontaneous third-circle slots before withdrawal");

                for (int i = sharedWizardList.SpellsByLevel[3].SpellsRefs.Count - 1; i >= 0; i--)
                    if (sharedWizardList.SpellsByLevel[3].SpellsRefs[i]?.Guid == cloakGuid)
                        sharedWizardList.SpellsByLevel[3].SpellsRefs.RemoveAt(i);
                locReady.Invoke(null, null);
                Check(!sharedBaseList.GetSpells(3).Contains(lateCloak) && !sharedAbjurationList.GetSpells(3).Contains(lateCloak),
                    "SP08 ReconcileOptional withdraws the optional spell when the late provider removes it");

                InBook(sharedCaster, fact => Check(
                    SpellStateOf(fact).Quotas[Occultist0099Spells.QuotaKey("slot-1", 3)] == Occultist0099AbjurationCatalog.CloakOfWinds &&
                    Occultist0099Spells.QuotaCount(SpellStateOf(fact), 3) == 1,
                    "SP08 withdrawing the optional provider preserves the learned circle-3 quota"));
                var sameKnownWhenWithdrawn = sharedBook.GetKnownSpells(3).Single(s => s.Blueprint == lateCloak);
                var withdrawnCast = CastRule(learnedCloak);
                Check(sharedBook.IsKnown(lateCloak) && ReferenceEquals(sameKnownWhenWithdrawn, learnedCloak) &&
                      learnedCloak.Blueprint.AssetGuid == cloakGuid &&
                      !learnedCloak.IsAvailable && !learnedCloak.IsAvailableForCast &&
                      !sharedBook.CanSpend(learnedCloak) && !sharedBook.Spend(learnedCloak) &&
                      sharedBook.GetSpontaneousSlots(3) == slotsBeforeWithdraw &&
                      withdrawnCast.ForceFail && !withdrawnCast.Success && sharedCaster.Buffs.GetBuff(lateEffect) == null,
                    "SP08 withdrawn optional Cloak preserves its root GUID and quota while blocking casting and slot spending");

                sharedWizardList.SpellsByLevel[3].SpellsRefs.Add(Ref<BlueprintAbilityReference>(lateCloak));
                locReady.Invoke(null, null);
                var restoredKnown = sharedBook.GetKnownSpells(3).Single(s => s.Blueprint == lateCloak);
                Check(sharedBaseList.GetSpells(3).Contains(lateCloak) && sharedAbjurationList.GetSpells(3).Contains(lateCloak) &&
                      ReferenceEquals(restoredKnown, learnedCloak) &&
                      learnedCloak.IsAvailable && learnedCloak.IsAvailableForCast && sharedBook.CanSpend(learnedCloak) &&
                      CastRule(learnedCloak).Success && sharedCaster.Buffs.GetBuff(lateEffect) != null &&
                      sharedBook.Spend(learnedCloak) && sharedBook.GetSpontaneousSlots(3) == slotsBeforeWithdraw - 1,
                    "SP08 restoring the optional provider re-enables the exact same learned Cloak ability and slot spend");
            }
            finally { applyNativeEffects = previousApplyEffects; }
        }
        finally
        {
            harmony.Unpatch(mRegInit, noop);
            harmony.Unpatch(mRetryInit, noop);
            harmony.Unpatch(mRegOpt, noop);
            harmony.Unpatch(mMedium, noop);
            harmony.Unpatch(mShieldReg, noop);
            harmony.Unpatch(mShieldLoc, noop);
            harmony.Unpatch(mDiag, noop);
            harmony.Unpatch(mTryMenu, noop);
            harmony.Unpatch(mModLoc, noop);
            harmony.Unpatch(mMenuLoc, noop);
            harmony.Unpatch(sLoggerInstance, sLoggerBoundary);
            harmony.Unpatch(sLoggerLog, noop);
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, oldCache);
            Kingmaker.Localization.LocalizationManager.CurrentPack = oldPack;
            modField.SetValue(null, oldMod);
            regCompletedField.SetValue(null, oldRegCompleted);
            deferredField.SetValue(null, oldDeferred);
            var liveStrings = (Dictionary<string, string>)stringsField.GetValue(null);
            liveStrings.Clear();
            foreach (var pair in oldStrings) liveStrings[pair.Key] = pair.Value;
            var liveStates = (Dictionary<string, string>)moduleStatesField.GetValue(null);
            liveStates.Clear();
            foreach (var pair in oldStates) liveStates[pair.Key] = pair.Value;
            var regs = regsField.GetValue(null);
            regs.GetType().GetMethod("Remove")?.Invoke(regs, new object[] { sharedCache });
        }
        Check(Occultist0099Slice.Published && (bool)mEnabled.Invoke(null, null),
            "SP08 the production gate is still enabled after the shared startup verification");
    }

    // B18 career growth, learning entry: the private book gains its circles as
    // the career advances. The A table opens 1/4/7/10/13/16 (C:31), and the
    // native level-up window is the only place a new root can be learned, so
    // this fixture drives the registered character at the ladder's steps.
    private static void SpellbookGrowthNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var dispel = Native<BlueprintAbility>("92681f181b507b34ea87018e8f7a528a");            // circle 3
        var stoneskin = Native<BlueprintAbility>("c66e86905f7606c4eaa5c774f0357b2b");          // circle 4
        var resistance = Native<BlueprintAbility>("0a5ddfbcfb3989543ac7c936fc256889");         // circle 5
        var disintegrate = Native<BlueprintAbility>("4aa7942c3e62a164387a73184bca3fc1");       // Transmutation circle 6
        Check(dispel != null && stoneskin != null && resistance != null && disintegrate != null,
            "SP08 the growth fixture resolves the audited circle 3 to 5 Abjuration roots and native Disintegrate (circle 6)");
        (UnitEntityData Unit, Spellbook Book, BlueprintSpellList List) Growth(string label, int level, string school = "Abjuration")
        {
            var unit = Unit(label, character, level, 16);
            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("slot-1-" + school + "-Implement"), unit.Descriptor));
            var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
            Set(book, "m_BaseLevelInternal", level);
            var list = book.Blueprint.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
                .Single(slot => slot.SlotKey == "slot-1" && slot.School == school).List.Get();
            return (unit, book, list);
        }

        var seven = Growth("growth-7", 7);
        var sevenWindow = LevelState(seven.Book);
        Check(NativeCheck(new SelectSpell(character.Spellbook, seven.List, 3, dispel, 0), sevenWindow, seven.Unit.Descriptor) &&
              !NativeCheck(new SelectSpell(character.Spellbook, seven.List, 4, stoneskin, 0), sevenWindow, seven.Unit.Descriptor),
            "SP08 at L=7 the level-up window opens the third circle and keeps the fourth closed");

        var ten = Growth("growth-10", 10);
        var tenWindow = LevelState(ten.Book);
        Check(NativeCheck(new SelectSpell(character.Spellbook, ten.List, 4, stoneskin, 0), tenWindow, ten.Unit.Descriptor) &&
              !NativeCheck(new SelectSpell(character.Spellbook, ten.List, 5, resistance, 0), tenWindow, ten.Unit.Descriptor),
            "SP08 at L=10 the fourth circle opens and the fifth stays closed");

        var thirteen = Growth("growth-13", 13);
        var thirteenWindow = LevelState(thirteen.Book);
        var learn = new SelectSpell(character.Spellbook, thirteen.List, 5, resistance, 0);
        Check(NativeCheck(learn, thirteenWindow, thirteen.Unit.Descriptor),
            "SP08 at L=13 the fifth circle opens through the native window");
        learn.Apply(thirteenWindow, thirteen.Unit.Descriptor);
        Check(thirteen.Book.IsKnown(resistance), "SP08 the newly opened fifth circle really learns at L=13");
        InBook(thirteen.Unit, f => Eq(resistance.AssetGuid.ToString(),
            SpellStateOf(f).Quotas[Occultist0099Spells.QuotaKey("slot-1", 5)],
            "SP08 the grown circle records its canonical root in the opened quota"));

        var fifteen = Growth("growth-15-trans", 15, "Transmutation");
        var fifteenWindow = LevelState(fifteen.Book);
        Check(!NativeCheck(new SelectSpell(character.Spellbook, fifteen.List, 6, disintegrate, 0), fifteenWindow, fifteen.Unit.Descriptor),
            "SP08 at L=15 Transmutation keeps the sixth circle closed for native Disintegrate");
        InBook(fifteen.Unit, f => Check(!SpellStateOf(f).Quotas.ContainsKey(Occultist0099Spells.QuotaKey("slot-1", 6)),
            "SP08 at L=15 no circle-6 quota is written when Disintegrate is refused"));

        var sixteen = Growth("growth-16-trans", 16, "Transmutation");
        var sixteenWindow = LevelState(sixteen.Book);
        var learnSix = new SelectSpell(character.Spellbook, sixteen.List, 6, disintegrate, 0);
        Check(NativeCheck(learnSix, sixteenWindow, sixteen.Unit.Descriptor),
            "SP08 at L=16 the level-up window opens the sixth circle for native Disintegrate");
        learnSix.Apply(sixteenWindow, sixteen.Unit.Descriptor);
        Check(sixteen.Book.IsKnown(disintegrate) &&
              sixteen.Book.GetKnownSpells(6).Any(ability => ability.Blueprint == disintegrate),
            "SP08 at L=16 Disintegrate is learned into the sixth circle of the private book");
        InBook(sixteen.Unit, f => Eq(disintegrate.AssetGuid.ToString(),
            SpellStateOf(f).Quotas[Occultist0099Spells.QuotaKey("slot-1", 6)],
            "SP08 at L=16 Disintegrate records its canonical root in the circle-6 quota"));

        caseStatus["SP08-growth"] =
            "native_level_up_window_opens_circles_3_4_5_6_at_the_audited_ladder";
    }

    private static void PsychicCrossBookIsolationNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var telepathType = Production("MagaambyanTelepath01447");
        var psychicClassType = Production("PsychicClass");
        var psychicCastingType = Production("PsychicCasting");
        var natureTargetType = Production("NatureTargeting01448");
        var undercastType = Production("PsychicUndercasting");
        var tempKnowledgeType = Production("TemporaryKnowledge01464");
        string PsyConst(Type t, string field) => (string)AccessTools.Field(t, field).GetValue(null);
        var naturePaid = (Metamagic)AccessTools.Field(telepathType, "Paid").GetValue(null);
        var logicalMeta = (Metamagic)AccessTools.Field(psychicCastingType, "Logical").GetValue(null);
        var isPsychicMethod = AccessTools.Method(psychicCastingType, "IsPsychic", new[] { typeof(AbilityData) });
        var isUndercastMethod = AccessTools.Method(undercastType, "IsUndercastBook");
        var reconcileUndercastMethod = AccessTools.Method(undercastType, "Reconcile");
        var canPayNatureMethod = AccessTools.Method(natureTargetType, "CanPay");
        bool IsPsychicAbility(AbilityData a) => (bool)isPsychicMethod.Invoke(null, new object[] { a });
        bool IsUndercastBook(BlueprintSpellbook b) => (bool)isUndercastMethod.Invoke(null, new object[] { b });
        void ReconcileUndercast(Spellbook b) => reconcileUndercastMethod.Invoke(null, new object[] { b });
        bool CanPayNature(AbilityData a, UnitEntityData u) => (bool)canPayNatureMethod.Invoke(null, new object[] { a, u });

        var plantFeature = Native<BlueprintFeature>(PsyConst(telepathType, "PlantId"));
        var centerBuff = new BlueprintBuff
        {
            name = "0099N.Sp07CenterBuff", AssetGuid = P(PsyConst(psychicClassType, "CenterBuffId")),
            Stacking = StackingType.Replace, ComponentsArray = Array.Empty<BlueprintComponent>()
        };
        centerBuff.OnEnable();
        var psyList = new BlueprintSpellList
        {
            name = "0099N.Sp07PsyList", AssetGuid = P(Occultist0099Ids.Id("HostSp07PsyList")),
            SpellsByLevel = Enumerable.Range(0, 10).Select(c => new SpellLevelList(c)).ToArray()
        };
        psyList.OnEnable();
        var psyClass = new BlueprintCharacterClass
        {
            name = "0099N.Sp07PsyClass", AssetGuid = P(PsyConst(psychicClassType, "ClassId")),
            ComponentsArray = Array.Empty<BlueprintComponent>()
        };
        var psyBook = new BlueprintSpellbook
        {
            name = "0099N.Sp07PsyBook", AssetGuid = P(PsyConst(psychicClassType, "BookId")),
            Spontaneous = true, IsArcane = false, CastingAttribute = StatType.Intelligence,
            ComponentsArray = new BlueprintComponent[] { new PsychicSpellbookMarker { MaximumSpellLevel = 9 } }
        };
        Set(psyBook, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(psyClass));
        Set(psyBook, "m_SpellList", Ref<BlueprintSpellListReference>(psyList));
        Set(psyBook, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(character.Spellbook.SpellsPerDay));
        psyBook.OnEnable();
        Set(psyClass, "m_Spellbook", Ref<BlueprintSpellbookReference>(psyBook));
        psyClass.OnEnable();
        BlueprintAbilityResource PoolBp(string id, string name, StatType stat)
        {
            var r = new BlueprintAbilityResource { name = name, AssetGuid = P(id) };
            var amount = Get(r, "m_MaxAmount");
            Set(amount, "BaseValue", 4);
            Set(amount, "IncreasedByStat", false);
            Set(amount, "ResourceBonusStat", stat);
            Set(r, "m_MaxAmount", amount);
            r.OnEnable();
            return r;
        }
        var wisPool = PoolBp(PsyConst(psychicClassType, "WisdomPoolId"), "0099N.Sp07WisPool", StatType.Wisdom);
        var chaPool = PoolBp(PsyConst(psychicClassType, "CharismaPoolId"), "0099N.Sp07ChaPool", StatType.Charisma);
        var natureBuff = new BlueprintBuff
        {
            name = "0099N.Sp07NatureBuff", AssetGuid = P(Occultist0099Ids.Id("HostSp07NatureBuff")),
            ComponentsArray = new BlueprintComponent[] { new NaturesCommand01447() }
        };
        natureBuff.OnEnable();
        var ampBuff = new BlueprintBuff
        {
            name = "0099N.Sp07AmpBuff", AssetGuid = P(Occultist0099Ids.Id("HostSp07AmpBuff")),
            ComponentsArray = new BlueprintComponent[]
            {
                new PsychicAmplificationComponent
                {
                    Kind = PsychicAmplificationKind.OverpoweringMind, Cost = 2, Strength = 1, MinimumLevel = 1,
                    CharacterClass = Ref<BlueprintCharacterClassReference>(psyClass),
                    WisdomPool = Ref<BlueprintAbilityResourceReference>(wisPool),
                    CharismaPool = Ref<BlueprintAbilityResourceReference>(chaPool)
                }
            }
        };
        ampBuff.OnEnable();
        var fearBuff = new BlueprintBuff
        {
            name = "0099N.Sp07FearBuff", AssetGuid = P(Occultist0099Ids.Id("HostSp07FearBuff")),
            ComponentsArray = new BlueprintComponent[]
            {
                new SpellDescriptorComponent { Descriptor = SpellDescriptor.Fear }
            }
        };
        fearBuff.OnEnable();
        var plantRestriction = new AbilityTargetHasFact { name = "0099N.Sp07NoPlant", Inverted = true };
        Set(plantRestriction, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(plantFeature) });
        var mindSpell = new BlueprintAbility
        {
            name = "0099N.Sp07MindSpell", AssetGuid = P(Occultist0099Ids.Id("HostSp07MindSpell")),
            Type = AbilityType.Spell, Range = AbilityRange.Unlimited,
            CanTargetEnemies = true, CanTargetFriends = true, CanTargetSelf = false,
            AvailableMetamagic = logicalMeta,
            ComponentsArray = new BlueprintComponent[]
            {
                new SpellComponent { School = SpellSchool.Enchantment },
                new SpellDescriptorComponent { Descriptor = SpellDescriptor.MindAffecting },
                plantRestriction
            }
        };
        mindSpell.OnEnable();
        var lowFamily = new BlueprintAbility
        {
            name = "CotWWrathPsychicSp07Low", AssetGuid = P(Occultist0099Ids.Id("HostSp07LowFamily")),
            Type = AbilityType.Spell,
            ComponentsArray = new BlueprintComponent[]
            {
                new PsychicUndercastFamily { Family = "Sp07Test", Circle = 1, LowerSpells = Array.Empty<BlueprintAbilityReference>() }
            }
        };
        lowFamily.OnEnable();
        var highFamily = new BlueprintAbility
        {
            name = "CotWWrathPsychicSp07High", AssetGuid = P(Occultist0099Ids.Id("HostSp07HighFamily")),
            Type = AbilityType.Spell,
            ComponentsArray = new BlueprintComponent[]
            {
                new PsychicUndercastFamily { Family = "Sp07Test", Circle = 2, LowerSpells = new[] { Ref<BlueprintAbilityReference>(lowFamily) } }
            }
        };
        highFamily.OnEnable();

        var stagedCacheIds = new List<BlueprintGuid>();
        void StageInCache(SimpleBlueprint bp)
        {
            cache.AddCachedBlueprint(bp.AssetGuid, bp);
            stagedCacheIds.Add(bp.AssetGuid);
        }
        StageInCache(centerBuff);
        StageInCache(psyClass);
        StageInCache(psyBook);
        StageInCache(psyList);
        StageInCache(wisPool);
        StageInCache(chaPool);
        StageInCache(lowFamily);
        StageInCache(highFamily);

        var sp07Harmony = new Harmony("cotw.0099n.sp07");
        var canTarget = AccessTools.Method(typeof(AbilityData), nameof(AbilityData.CanTarget), new[] { typeof(TargetWrapper) });
        var legalBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(LegalTargetBoundary));
        var createContext = AccessTools.Method(typeof(AbilityData), nameof(AbilityData.CreateExecutionContext));
        var castBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(CastContextBoundary));
        foreach (var type in new[]
        {
            psychicCastingType.GetNestedType("EmotionPatch", BindingFlags.NonPublic),
            psychicCastingType.GetNestedType("ThoughtPatch", BindingFlags.NonPublic),
            psychicCastingType.GetNestedType("SourcePatch", BindingFlags.NonPublic),
            psychicCastingType.GetNestedType("LogicalSelectionPatch", BindingFlags.NonPublic),
            psychicCastingType.GetNestedType("LogicalCostPatch", BindingFlags.NonPublic),
            Production("NaturePaidTarget01448Patch"),
            Production("NatureTargetScope01448Patch"),
            Production("NatureTargetFact01448Patch")
        }.Concat(undercastType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
            .Concat(tempKnowledgeType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Any())))
            sp07Harmony.CreateClassProcessor(type).Patch();
        harmony.Unpatch(canTarget, legalBoundary);
        harmony.Unpatch(createContext, castBoundary);
        try
        {
            var unit = Unit("sp07-multiclass", character, 10, 16);
            var sp07Body = Empty<UnitBody>();
            Set(sp07Body, "Owner", unit.Descriptor);
            Set(unit.Descriptor, "Body", sp07Body);
            ((List<ClassData>)Get(unit.Progression, "Classes")).Add(new ClassData(psyClass) { Level = 11 });
            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("slot-1-Enchantment-Implement"), unit.Descriptor));
            var occBook = unit.Descriptor.DemandSpellbook(character.Spellbook);
            Set(occBook, "m_BaseLevelInternal", 10);
            occBook.Rest();
            var psyBookInst = unit.Descriptor.DemandSpellbook(psyBook);
            Set(psyBookInst, "m_BaseLevelInternal", 11);
            psyBookInst.Rest();
            unit.Resources.Add(wisPool, true);
            unit.Resources.Add(chaPool, true);

            var occData = new AbilityData(mindSpell, occBook, 1);
            var logicalOcc = new AbilityData(mindSpell, occBook, 1)
            {
                MetamagicData = new Kingmaker.UnitLogic.Abilities.MetamagicData { SpellLevelCost = 1 }
            };
            logicalOcc.MetamagicData.Add(logicalMeta);
            var psyData = new AbilityData(mindSpell, psyBookInst, 1);
            Check(IsPsychicAbility(occData) &&
                  !IsUndercastBook(occBook.Blueprint) &&
                  IsUndercastBook(psyBookInst.Blueprint),
                "SP07 OccultistSpellbook opts into PsychicCasting while opting out of PsychicUndercasting");
            Check(occData.IsAvailableForCast && logicalOcc.IsAvailableForCast,
                "SP07 baseline OccultistSpellbook spell and Logical variant are castable before Fear");

            int unfocusedConc = occData.CalculateParams().Concentration;
            var centered = unit.Buffs.AddBuff(centerBuff, unit, (TimeSpan?)null);
            int centeredConc = occData.CalculateParams().Concentration;
            centered.Remove();
            Eq(unfocusedConc + 10, centeredConc,
                "SP07 shared Psychic thought-component penalty and Center Self recovery apply to OccultistSpellbook");

            var fear = unit.Buffs.AddBuff(fearBuff, unit, (TimeSpan?)null);
            Check(!occData.IsAvailableForCast,
                "SP07 shared Psychic emotion component blocks OccultistSpellbook casting under Fear");
            Check(logicalOcc.IsAvailableForCast,
                "SP07 Logical Spell lets OccultistSpellbook bypass the shared emotion block");
            fear.Remove();

            var collect = new RuleCollectMetamagic(occBook, mindSpell, 1);
            var logicalFeat = new Feature(new BlueprintFeature
            {
                AssetGuid = P(PsyConst(psychicClassType, "LogicalId")), name = "0099N.LogicalFeat",
                ComponentsArray = new BlueprintComponent[] { new AddMetamagicFeat { Metamagic = logicalMeta } }
            }, unit.Descriptor);
            Rulebook.Trigger(collect);
            Check(!collect.SpellMetamagics.Contains(logicalFeat), "SP07 baseline RuleCollectMetamagic has no Logical feat yet");
            collect.AddMetamagic(logicalFeat);
            Check(collect.SpellMetamagics.Contains(logicalFeat),
                "SP07 shared LogicalSelectionPatch allows Logical metamagic on OccultistSpellbook");

            ((List<AbilityData>[])Get(occBook, "m_KnownSpells"))[2].Add(new AbilityData(highFamily, occBook, 2));
            ReconcileUndercast(occBook);
            occBook.PostLoad(unit.Descriptor);
            Check(occBook.IsKnown(highFamily) && !occBook.IsKnown(lowFamily),
                "SP07 PsychicUndercasting ignores OccultistSpellbook on reconcile and PostLoad");
            psyBookInst.AddKnown(2, highFamily);
            Check(psyBookInst.IsKnown(highFamily) && psyBookInst.IsKnown(lowFamily) &&
                  psyBookInst.GetKnownSpells(1).Single(s => s.Blueprint == lowFamily).IsTemporary,
                "SP07 PsychicUndercasting continues to add temporary lower-circle spells on PsychicClass.BookId");

            var plantTarget = Unit("sp07-plant", null, 0, 10);
            plantTarget.Facts.Add(new Feature(plantFeature, plantTarget.Descriptor));
            var humanoidTarget = Unit("sp07-humanoid", null, 0, 10);

            var activeNature = unit.Buffs.AddBuff(natureBuff, unit, (TimeSpan?)null);
            Eq(4, unit.Resources.GetResourceAmount(wisPool), "SP07 phrenic Wisdom pool starts at 4");
            Check(!CanPayNature(occData, unit) &&
                  !occData.CanTarget(new TargetWrapper(plantTarget)) &&
                  occData.CanTarget(new TargetWrapper(humanoidTarget)) &&
                  CanPayNature(psyData, unit) &&
                  psyData.CanTarget(new TargetWrapper(plantTarget)),
                "SP07 Nature's Command bypasses Plant targeting on PsychicClass.BookId only, never on OccultistSpellbook");

            var occNatureCast = Rulebook.Trigger(new RuleCastSpell(occData, new TargetWrapper(humanoidTarget)));
            Eq(4, unit.Resources.GetResourceAmount(wisPool),
                "SP07 casting a mind-affecting spell from OccultistSpellbook spends no phrenic points on Nature's Command");
            Check((occNatureCast.Context.Params.Metamagic & naturePaid) == 0,
                "SP07 OccultistSpellbook cast never marks MagaambyanTelepath.Paid");

            var psyNatureCast = Rulebook.Trigger(new RuleCastSpell(psyData, new TargetWrapper(plantTarget)));
            Eq(2, unit.Resources.GetResourceAmount(wisPool),
                "SP07 casting the same root from PsychicClass.BookId spends 2 phrenic points on Nature's Command");
            Check((psyNatureCast.Context.Params.Metamagic & naturePaid) == naturePaid,
                "SP07 PsychicClass.BookId cast marks MagaambyanTelepath.Paid");
            activeNature.Remove();

            unit.Resources.Restore(wisPool, 2);
            Eq(4, unit.Resources.GetResourceAmount(wisPool), "SP07 fixture restores Wisdom pool to 4");
            var activeAmp = unit.Buffs.AddBuff(ampBuff, unit, (TimeSpan?)null);
            int baseOccDc = occData.CalculateParams().DC;
            int basePsyDc = psyData.CalculateParams().DC;
            var occAmpCast = Rulebook.Trigger(new RuleCastSpell(occData, new TargetWrapper(humanoidTarget)));
            Eq(4, unit.Resources.GetResourceAmount(wisPool),
                "SP07 OccultistSpellbook cast spends no phrenic points on Psychic Overpowering Mind");
            Eq(baseOccDc, occAmpCast.Context.Params.DC,
                "SP07 OccultistSpellbook cast receives no Overpowering Mind DC bonus");
            var psyAmpCast = Rulebook.Trigger(new RuleCastSpell(psyData, new TargetWrapper(humanoidTarget)));
            Eq(2, unit.Resources.GetResourceAmount(wisPool),
                "SP07 PsychicClass.BookId cast spends 2 phrenic points on Overpowering Mind");
            Eq(basePsyDc + 1, psyAmpCast.Context.Params.DC,
                "SP07 PsychicClass.BookId cast receives +1 DC from Overpowering Mind");
            activeAmp.Remove();
        }
        finally
        {
            sp07Harmony.UnpatchAll("cotw.0099n.sp07");
            harmony.Patch(canTarget, prefix: new HarmonyMethod(legalBoundary));
            harmony.Patch(createContext, prefix: new HarmonyMethod(castBoundary));
            foreach (var id in stagedCacheIds) cache.RemoveCachedBlueprint(id);
        }
        caseStatus["SP07"] = "native_private_book_psychic_casting_shared_without_amplification_nature_or_undercast_leak";
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
    // The TEST build publishes the complete map.
    // ------------------------------------------------------------------
    private static void GateNative()
    {
        Check(Occultist0099Slice.Published,
            "GATE the complete class is enabled in the TEST DLL");
        Check(Occultist0099Slice.CanRegister(true),
            "GATE CanRegister accepts a complete map on the TEST build");
        Check(!Occultist0099Slice.CanRegister(false),
            "GATE CanRegister refuses an incomplete map");
        var binder = (GuidClassBinder)Json.Serializer.Binder;
        Check(binder.BindToType(null, Occultist0099Ids.FocusPartTypeId) == typeof(Occultist0099FocusPart),
            "GATE the existing Focus Part TypeId resolves to the persisted focus ledger type");
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
            Occultist0099Ids.PowerLevelTypeId, Occultist0099Ids.LegacyWeaponOptionTypeId,
            Occultist0099Ids.LegacyWeaponTargetTypeId, Occultist0099Ids.LegacyWeaponBuffTypeId, Occultist0099Ids.PhilosophersTouchBuffTypeId,
            Occultist0099Ids.QuicknessCounterTypeId,
            Occultist0099Ids.PanoplyCarrierTypeId, Occultist0099Ids.PanoplyValueTypeId,
            Occultist0099Ids.PanoplyVisibilityTypeId, Occultist0099Ids.FontOfHealingTypeId,
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
    // The runner always writes its report inside the repository, so the host can
    // walk up from it to hold the shipped startup wiring to its source text.
    private static string hostReportPath;

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
        var masterySelection = Asset<BlueprintFeatureSelection>("ImplementMasterySelection");
        Check(progression.LevelEntries.SelectMany(entry => entry.Features.Where(f => f == powers).Select(_ => entry.Level))
                  .SequenceEqual(Enumerable.Range(0, 10).Select(i => 2 * i + 1)) &&
              progression.LevelEntries.SelectMany(entry => entry.Features.OfType<BlueprintFeatureSelection>()
                  .Where(selection => selection != powers && selection != masterySelection).Select(_ => entry.Level))
                  .SequenceEqual(new[] { 1, 1, 2, 6, 10, 14, 18 }) &&
              progression.LevelEntries.Where(entry => entry.Features.Contains(masterySelection))
                  .Select(entry => entry.Level).SequenceEqual(new[] { 20 }),
            "ENCH native progression schedules the seven distinct implement choices, ten focus-power choices and the level-20 mastery selection at the audited levels");
        Check(slots.Length == 123 && slots.Select(slot => slot.List.Guid).Distinct().Count() == slots.Length &&
              slots.Where(slot => slot.School != null).GroupBy(slot => slot.School).All(school => school.Count() == 13) &&
              slots.Count(slot => slot.Panoply == "Trappings of the Warrior") == 5 &&
              slots.Count(slot => slot.Panoply == "Mage's Paraphernalia") == 4 &&
              slots.Count(slot => slot.Panoply == "Performer's Accoutrements") == 5 &&
              slots.Count(slot => slot.Panoply == "Saint's Holy Regalia") == 5,
            "HOST all eight schools and four Panoplies have distinct stable native learning lists");
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

    private static int servantSpawnCalls;
    private static readonly List<int> servantSpawnLevels = new List<int>();
    private static BlueprintSummonPool servantPool;
    private static UnitEntityData servantCaster;
    private static Kingmaker.AreaLogic.SummonPool.SummonPoolsManager servantPools;

    private static bool ServantPoolsBoundary(ref Kingmaker.AreaLogic.SummonPool.SummonPoolsManager __result)
    {
        __result = servantPools;
        return false;
    }

    // Stands in for the Unity spawn only. The production action still supplies the
    // unit, pool, duration and Hit Dice request, and the native pool still counts.
    private static bool ServantSpawnBoundary(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster __instance)
    {
        servantSpawnCalls++;
        var context = ContextData<MechanicsContext.Data>.Current?.Context;
        servantSpawnLevels.Add(__instance.LevelValue.Calculate(context));
        var spawned = Unit("necro-servant-" + servantSpawnCalls, null, 0, 10);
        spawned.Ensure<Kingmaker.UnitLogic.Parts.UnitPartSummonedMonster>().Init(servantCaster);
        servantPools.GetPool(servantPool).Register(spawned);
        return false;
    }

    private static bool ServantRangeBoundary(ref bool __result)
    {
        __result = true;
        return false;
    }

    // UnityEngine.Random is an ECall the headless host cannot run. Damage dice are
    // pinned so the held-touch lifecycle can be observed without a Unity runtime.
    private static bool ServantDiceBoundary(ref int __result)
    {
        __result = 1;
        return false;
    }

    // Pins the dispel d20 so the count limit, not the roll, decides the outcome.
    // The native OnTrigger still runs and performs its own buff removal.
    private static void PurgeDispelBoundary(RuleDispelMagic __instance) => __instance.SetRollOverride(20);

    // HiddenInInspector is an inspector-only flag whose getter reads localization,
    // which needs a Unity runtime. It has no bearing on dispel eligibility here.
    private static bool PurgeInspectorBoundary(ref bool __result)
    {
        __result = false;
        return false;
    }

    private static void ConjurationNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("conjuration-caster", character, 9, 16);
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Conjuration-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 9);
        var leveling = LevelState(book);
        var powers = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        foreach (var option in new[] { (Token: "FleshMend", Level: 3), (Token: "PsychicFog", Level: 1),
            (Token: "PurgeCorruption", Level: 5), (Token: "SideStep", Level: 7) })
        {
            var selection = leveling.AddSelection(null, character.Progression, powers, option.Level);
            var pick = new SelectFeature(selection, Asset<BlueprintFeature>(option.Token + "Power"));
            Check(pick.Check(leveling, unit.Descriptor), "CONJ native selection admits " + option.Token);
            pick.Apply(leveling, unit.Descriptor);
        }
        var list = character.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
            .Single(slot => slot.SlotKey == "slot-1" && slot.School == "Conjuration").List.Get();
        var rows = ((Occultist0099SpellRoot[])AccessTools.Field(Production("Occultist0099SpellCatalog"), "Roots").GetValue(null))
            .Where(row => row.School == "Conjuration" && !row.IsKnack && row.Source != "External").ToArray();
        Eq(13, rows.Length, "CONJ the signed Conjuration table has 13 native or existing CotW candidates");
        Check(rows.All(row => list.GetSpells(row.Circle).Any(spell => spell.AssetGuid == P(row.Guid))) &&
              list.SpellsByLevel.Sum(row => row.Spells.Count()) == 13,
            "CONJ every learning candidate keeps its signed native root and Occultist circle");
        var cure = Native<BlueprintAbility>("5590652e1c2225c4ca30c4a699ab3649");
        var learn = new SelectSpell(character.Spellbook, list, 1, cure, 0);
        Check(NativeCheck(learn, leveling, unit.Descriptor), "CONJ the native implement source offers Cure Light Wounds");
        learn.Apply(leveling, unit.Descriptor);
        Check(book.IsKnown(cure), "CONJ the reused root enters the private Occultist book");

        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var pool = focus.Schools["Conjuration"];
        var classData = unit.Progression.GetClassData(character);
        var castingBuff = Asset<BlueprintBuff>("CastingFocusBuff");
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 12; i++) CastRule(Granted("ConjurationInvestFocusAbility"));
            Check(unit.Buffs.GetBuff(castingBuff) == null, "CONJ Casting Focus stays absent before the lock");
            Check(CastRule(Granted("LockFocusAbility")).Success && unit.Buffs.GetBuff(castingBuff) != null,
                "CONJ locking the school establishes the Casting Focus resonance buff");
            // Twelve points at level 9: one round per two points, capped at L.
            Eq(6, Occultist0099Focus.Resonance(focus, "Conjuration"),
                "CONJ twelve locked points give six extra rounds at one per two points");
            classData.Level = 4;
            Set(book, "m_BaseLevelInternal", 4);
            Check(CastRule(Granted("ConjurationInvestFocusAbility")).ForceFail,
                "CONJ a locked snapshot cannot be re-invested");
            Eq(6, Occultist0099Focus.Resonance(focus, "Conjuration"),
                "CONJ the locked resonance snapshot survives a later class-level change");
            classData.Level = 9;
            Set(book, "m_BaseLevelInternal", 9);
            // The cap is the locked effective level: a level-4 lock caps at four,
            // and Implement Mastery lifts that cap by its four virtual levels.
            Occultist0099FocusState CapFixture(string mastery)
            {
                var state = Occultist0099Focus.Create();
                state.ClassPresent = true;
                state.Enabled = true;
                state.Level = 4;
                state.IntModifier = 3;
                state.Intelligence = 16;
                state.Budget = 15;
                state.Generic = 3;
                state.MasterySchool = mastery;
                state.Schools["Conjuration"].Present = true;
                state.Schools["Conjuration"].Invested = 12;
                state.Schools["Conjuration"].Remaining = 12;
                Eq(Occultist0099Outcome.Ok, Occultist0099Focus.Lock(state, 4),
                    "CONJ the level-4 cap fixture locks (mastery " + (mastery ?? "none") + ")");
                return state;
            }
            Eq(4, Occultist0099Focus.Resonance(CapFixture(null), "Conjuration"),
                "CONJ twelve points at level 4 cap the bonus at the locked level, not at six");
            // Mastery both adds its four points to the locked investment and lifts the
            // cap to the effective level: min(12+4, 2*(4+4))/2 = 8.
            Eq(8, Occultist0099Focus.Resonance(CapFixture("Conjuration"), "Conjuration"),
                "CONJ Implement Mastery adds four invested points and caps at the effective level, per the original class-scaling rank");

            // The duration hook only extends rounds-per-level Conjuration spells
            // cast from this class's private book.
            var roundsBuff = new BlueprintBuff { name = "0099N.ConjurationRoundsBuff",
                AssetGuid = P(Occultist0099Ids.Id("HostConjurationRoundsBuff")), Stacking = StackingType.Replace,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            roundsBuff.OnEnable();
            var minutesBuff = new BlueprintBuff { name = "0099N.ConjurationMinutesBuff",
                AssetGuid = P(Occultist0099Ids.Id("HostConjurationMinutesBuff")), Stacking = StackingType.Replace,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            minutesBuff.OnEnable();
            BlueprintAbility Probe(string token, SpellSchool school, DurationRate rate, BlueprintBuff effect)
            {
                var apply = new ContextActionApplyBuff { DurationValue = new ContextDurationValue
                    { Rate = rate, DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1 } };
                Set(apply, "m_Buff", Ref<BlueprintBuffReference>(effect));
                var probe = new BlueprintAbility { name = "0099N." + token, AssetGuid = P(Occultist0099Ids.Id(token)),
                    Type = AbilityType.Spell, Range = AbilityRange.Personal, CanTargetSelf = true,
                    ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = school },
                        new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { apply } } } } };
                probe.OnEnable();
                return probe;
            }
            var conjRounds = Probe("HostConjurationRoundsSpell", SpellSchool.Conjuration, DurationRate.Rounds, roundsBuff);
            var conjMinutes = Probe("HostConjurationMinutesSpell", SpellSchool.Conjuration, DurationRate.Minutes, minutesBuff);
            var evocRounds = Probe("HostEvocationRoundsSpell", SpellSchool.Evocation, DurationRate.Rounds, roundsBuff);
            TimeSpan? Duration(BlueprintAbility probe, Spellbook source)
            {
                var ability = new AbilityData(probe, source, 1);
                var context = ability.CreateExecutionContext(new TargetWrapper(unit));
                var action = probe.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
                using (context.GetDataScope(unit))
                    return (TimeSpan?)AccessTools.Method(typeof(ContextActionApplyBuff), "CalculateDuration")
                        .Invoke(action, new object[] { context });
            }
            Eq(TimeSpan.FromSeconds(6 + 36), Duration(conjRounds, book),
                "CONJ a private-book rounds-per-level Conjuration spell gains the full resonance in extra rounds");
            Eq(TimeSpan.FromSeconds(60), Duration(conjMinutes, book),
                "CONJ a minutes-per-level Conjuration spell is not extended");
            Eq(TimeSpan.FromSeconds(6), Duration(evocRounds, book),
                "CONJ another school's rounds-per-level spell is not extended");
            var foreignBook = new BlueprintSpellbook { AssetGuid = P(Occultist0099Ids.Id("HostConjurationForeignBook")),
                name = "0099N.ConjurationForeignBook", Spontaneous = true, IsArcane = false, CastingAttribute = StatType.Intelligence,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            Set(foreignBook, "m_SpellList", Ref<BlueprintSpellListReference>(book.Blueprint.SpellList));
            Set(foreignBook, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(book.Blueprint.SpellsPerDay));
            foreignBook.OnEnable();
            Eq(TimeSpan.FromSeconds(6), Duration(conjRounds, unit.Descriptor.DemandSpellbook(foreignBook)),
                "CONJ a multiclass spellbook sharing the same list gains no Casting Focus rounds");
            // Disclosed boundary: area-effect lifetimes use ContextActionSpawnAreaEffect,
            // which neither upstream nor this port extends. Hungry Pit is the signed
            // 5th-circle example, so pin that its area duration is untouched.
            var hungryPit = Native<BlueprintAbility>("f63f4d1806b78604a952b3958892ce1c");
            var pitSpawn = hungryPit.GetComponent<AbilityEffectRunAction>().Actions.Actions
                .OfType<ContextActionSpawnAreaEffect>().Single();
            Eq(DurationRate.Rounds, pitSpawn.DurationValue.Rate,
                "CONJ Hungry Pit really is a rounds-per-level Conjuration area spell");
            Rounds AreaLifetime()
            {
                var context = new AbilityData(hungryPit, book, 5).CreateExecutionContext(new TargetWrapper(unit));
                return pitSpawn.DurationValue.Calculate(context);
            }
            var pitWithResonance = AreaLifetime();
            unit.Buffs.GetBuff(castingBuff).Remove();
            var pitWithoutResonance = AreaLifetime();
            unit.Buffs.AddBuff(castingBuff, unit, (TimeSpan?)null);
            Eq(pitWithoutResonance, pitWithResonance,
                "CONJ Casting Focus leaves the area lifetime unchanged; only buffs and summons are extended");

            // D14-F1: drive the summon side of the same resonance through the native
            // RuleSummonUnit bonus, which RuleSummonUnit.OnTrigger adds to the duration.
            var skeleton = Native<BlueprintUnit>("53e228ba7fe18104c93dc4b7294a1b30");
            var castingComponent = (Occultist0099CastingFocus)castingBuff.GetComponent<Occultist0099CastingFocus>();
            Rounds SummonBonus(BlueprintAbility probe, Spellbook source)
            {
                var context = new AbilityData(probe, source, 1).CreateExecutionContext(new TargetWrapper(unit));
                var summon = new RuleSummonUnit(unit, skeleton, UnityEngine.Vector3.zero, 10.Rounds(), 0) { Context = context };
                var runtime = unit.Buffs.GetBuff(castingBuff).Components
                    .First(component => component.SourceBlueprintComponent is Occultist0099CastingFocus);
                Occultist0099NativeScope.InRuntimeContext(runtime, _ => castingComponent.OnEventAboutToTrigger(summon));
                return summon.BonusDuration;
            }
            Eq(6.Rounds(), SummonBonus(conjRounds, book),
                "CONJ a private-book Conjuration summon gains the full resonance through the native RuleSummonUnit bonus");
            Eq(0.Rounds(), SummonBonus(evocRounds, book),
                "CONJ another school's summon gains no Casting Focus rounds");
            Eq(0.Rounds(), SummonBonus(conjRounds, unit.Descriptor.DemandSpellbook(foreignBook)),
                "CONJ a multiclass spellbook summon gains no Casting Focus rounds");
            unit.Buffs.GetBuff(castingBuff).Remove();
            Eq(TimeSpan.FromSeconds(6), Duration(conjRounds, book),
                "CONJ without the resonance buff the same spell keeps its native duration");
            // On the summon side the gate is the buff itself: the native rulebook only
            // dispatches the handler while the buff is attached, so its absence is the
            // observable condition here rather than a simulated dispatch.
            Check(unit.Buffs.GetBuff(castingBuff) == null,
                "CONJ removing the resonance buff detaches the native summon-duration handler");
            unit.Buffs.AddBuff(castingBuff, unit, (TimeSpan?)null);

            // Servitor picks the summon tier by level and always lasts one minute.
            var servitor = Granted("ServitorAbility");
            var servitorBranches = servitor.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<Conditional>().ToArray();
            Eq(7, servitorBranches.Length, "CONJ Servitor carries exactly the seven signed summon tiers");
            var servitorTier = servitor.Blueprint.GetComponents<ContextRankConfig>()
                .Single(rank => rank.Type == AbilityRankType.StatBonus);
            Eq(7, (int)Get(servitorTier, "m_Max"), "CONJ the summon tier caps at Summon Monster VII");
            var servitorMinute = servitor.Blueprint.GetComponents<ContextRankConfig>()
                .Single(rank => rank.Type == AbilityRankType.Default);
            Eq(10, (int)Get(servitorMinute, "m_Min"), "CONJ Servitor pins the native summon duration to ten rounds");
            Eq(10, (int)Get(servitorMinute, "m_Max"), "CONJ Servitor's duration cannot grow with level");
            Check(!(bool)Get(servitor.Blueprint, "m_IsFullRoundAction"), "CONJ Servitor is a standard action, not the native full-round summon");
            int before = pool.Remaining;
            // The Unity spawn is out of scope; the paid cast and its ranks are not.
            var spawnMethod = AccessTools.Method(typeof(ContextActionSpawnMonster), "RunAction");
            var spawnBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ShadowSpawnBoundary));
            harmony.Patch(spawnMethod, prefix: new HarmonyMethod(spawnBoundary));
            RuleCastSpell servitorCast;
            try { servitorCast = Rulebook.Trigger(new RuleCastSpell(servitor, new TargetWrapper(UnityEngine.Vector3.zero))); }
            finally { harmony.Unpatch(spawnMethod, spawnBoundary); }
            Check(servitorCast.Success && pool.Remaining == before - 1,
                "CONJ Servitor pays exactly one Conjuration point");
            Eq(10, servitorCast.Context[AbilityRankType.Default], "CONJ the paid cast supplies the one-minute duration rank");
            Eq(3, servitorCast.Context[AbilityRankType.StatBonus], "CONJ at level 9 the paid cast selects Summon Monster III");

            // Flesh Mend heals the living and refuses the excluded creature types.
            var mend = Granted("FleshMendAbility");
            var mendDice = mend.Blueprint.GetComponents<ContextRankConfig>()
                .Single(rank => rank.Type == AbilityRankType.DamageDice);
            Eq(5, (int)Get(mendDice, "m_Max"), "CONJ Flesh Mend caps at five d8");
            Eq(3, (int)Get(mendDice, "m_StartLevel"), "CONJ Flesh Mend's extra dice start at 3rd level");
            Eq(4, (int)Get(mendDice, "m_StepLevel"), "CONJ Flesh Mend gains a die every four levels");
            var heal = mend.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionHealTarget>().Single();
            Eq(DiceType.D8, heal.Value.DiceType, "CONJ Flesh Mend uses d8 healing dice");
            var mendTarget = Unit("conjuration-mend-target", character, 5, 10);
            var mendRule = mend.Blueprint.GetComponent<AbilityTargetHasFact>();
            Check(mendRule.Inverted && mendRule.CheckedFacts.Length == 3,
                "CONJ Flesh Mend excludes undead, constructs and elementals through the native target fact rule");
            Check(mend.Blueprint.CanTargetFriends && !mend.Blueprint.CanTargetEnemies && mend.Blueprint.NotOffensive,
                "CONJ Flesh Mend is a friendly touch power");

            // Psychic Fog: mist costs one, solid costs two and opens at 7th level.
            var fog = Granted("PsychicFogAbility");
            var fogChoices = fog.GetConversions().ToArray();
            Eq(2, fogChoices.Length, "CONJ Psychic Fog offers the mist and solid branches");
            var mistChoice = fogChoices.Single(choice => choice.Blueprint == Asset<BlueprintAbility>("PsychicFogMistAbility"));
            var solidChoice = fogChoices.Single(choice => choice.Blueprint == Asset<BlueprintAbility>("PsychicFogSolidAbility"));
            Eq(1, mistChoice.Blueprint.GetComponent<Occultist0099FocusResourceLogic>().Amount, "CONJ the mist branch costs one point");
            Eq(2, solidChoice.Blueprint.GetComponent<Occultist0099FocusResourceLogic>().Amount, "CONJ the solid branch costs two points");
            Check(solidChoice.IsVisible(), "CONJ at level 9 the solid fog branch is available");
            classData.Level = 6;
            Check(!solidChoice.IsVisible() && mistChoice.IsVisible(),
                "CONJ below 7th level only the mist branch is offered");
            int gateFocus = pool.Remaining;
            Check(Rulebook.Trigger(new RuleCastSpell(solidChoice, new TargetWrapper(UnityEngine.Vector3.zero))).ForceFail &&
                  pool.Remaining == gateFocus,
                "CONJ a directly submitted below-7 solid fog is refused at payment without spending focus");
            classData.Level = 9;

            // Purge Corruption keeps both native cleansing roots.
            var purge = Granted("PurgeCorruptionAbility");
            var purgeChoices = purge.GetConversions().ToArray();
            Eq(2, purgeChoices.Length, "CONJ Purge Corruption offers neutralize poison and remove disease");
            Check(purgeChoices.All(choice => choice.Blueprint.GetComponent<Occultist0099FocusResourceLogic>().Amount == 1 &&
                  choice.Blueprint.GetComponent<AbilityEffectRunAction>() != null),
                "CONJ each cleansing branch costs one point and keeps its native effect graph");
            // Both native donors leave the stop flag false, which makes the native action
            // use int.MaxValue. The private copies must cure exactly one effect per use.
            var deepElements = AccessTools.Method(Production("SpellBatch01447"), "Elements");
            var identityComparer = AccessTools.Property(Production("PsychicAssets"), "IdentityComparer").GetValue(null);
            IEnumerable<object> Walk(AbilityEffectRunAction action)
                => ((System.Collections.IEnumerable)deepElements.Invoke(null,
                    new object[] { new[] { action }, Activator.CreateInstance(typeof(HashSet<object>), identityComparer) })).Cast<object>();
            var dispels = purgeChoices.SelectMany(choice => Walk(choice.Blueprint.GetComponent<AbilityEffectRunAction>()))
                .OfType<ContextActionDispelMagic>().ToArray();
            Eq(2, dispels.Length, "CONJ each cleansing branch keeps its native dispel action");
            Check(dispels.All(dispel => (bool)Get(dispel, "m_StopAfterCountRemoved")),
                "CONJ the private cleansing copies stop after a successful removal, unlike the native donors");
            var poisonTarget = Unit("conjuration-purge-target", character, 5, 10);
            BlueprintBuff Ailment(string token)
            {
                var buff = new BlueprintBuff { name = "0099N." + token, AssetGuid = P(Occultist0099Ids.Id(token)),
                    Stacking = StackingType.Stack, ComponentsArray = Array.Empty<BlueprintComponent>() };
                // HiddenInInspector reads the localized name, so the fixture needs real
                // localization keys rather than an empty LocalizedString.
                Set(buff, "m_DisplayName", new Kingmaker.Localization.LocalizedString { Key = "0099N." + token + ".name" });
                Set(buff, "m_Description", new Kingmaker.Localization.LocalizedString { Key = "0099N." + token + ".description" });
                buff.OnEnable();
                return buff;
            }
            var firstAilment = Ailment("HostPurgeAilmentOne");
            var secondAilment = Ailment("HostPurgeAilmentTwo");
            // The native dispel filters by descriptor, so the fixture ailments must
            // carry the Poison descriptor the cleansing donor looks for.
            var ailmentSource = new BlueprintAbility { name = "0099N.HostPurgeAilmentSource",
                AssetGuid = P(Occultist0099Ids.Id("HostPurgeAilmentSource")), Type = AbilityType.Spell,
                Range = AbilityRange.Personal, CanTargetSelf = true,
                ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Necromancy },
                    new SpellDescriptorComponent { Descriptor = SpellDescriptor.Poison } } };
            ailmentSource.OnEnable();
            var ailmentContext = new AbilityData(ailmentSource, book, 1).CreateExecutionContext(new TargetWrapper(poisonTarget));
            foreach (var ailment in new[] { firstAilment, secondAilment })
                poisonTarget.Descriptor.AddBuff(ailment, ailmentContext, TimeSpan.FromMinutes(1));
            Check(poisonTarget.Buffs.GetBuff(firstAilment) != null && poisonTarget.Buffs.GetBuff(secondAilment) != null,
                "CONJ the purge fixture carries two separate removable effects");
            var dispelRule = AccessTools.Method(typeof(RuleDispelMagic), nameof(RuleDispelMagic.OnTrigger));
            var dispelBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(PurgeDispelBoundary));
            var purgeDice = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) });
            var purgeDiceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantDiceBoundary));
            var inspectorGetter = AccessTools.PropertyGetter(typeof(Kingmaker.UnitLogic.Buffs.Buff), "HiddenInInspector");
            var inspectorBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(PurgeInspectorBoundary));
            harmony.Patch(dispelRule, prefix: new HarmonyMethod(dispelBoundary));
            harmony.Patch(purgeDice, prefix: new HarmonyMethod(purgeDiceBoundary));
            harmony.Patch(inspectorGetter, prefix: new HarmonyMethod(inspectorBoundary));
            try
            {
                var poisonBranch = purgeChoices.Single(choice => choice.Blueprint == Asset<BlueprintAbility>("PurgeCorruptionPoisonAbility"));
                // The host dispatches the paid cast's effects itself; applying them a
                // second time here would double the removal and hide the count limit.
                Check(Rulebook.Trigger(new RuleCastSpell(poisonBranch, new TargetWrapper(poisonTarget))).Success,
                    "CONJ the cleansing cast is accepted");
            }
            finally
            {
                harmony.Unpatch(dispelRule, dispelBoundary);
                harmony.Unpatch(purgeDice, purgeDiceBoundary);
                harmony.Unpatch(inspectorGetter, inspectorBoundary);
            }
            int remainingAilments = new[] { firstAilment, secondAilment }.Count(ailment => poisonTarget.Buffs.GetBuff(ailment) != null);
            Eq(1, remainingAilments, "CONJ one Purge Corruption use removes exactly one of two matching effects");

            // Side Step is a move action whose range scales with the actual level.
            var step = Granted("SideStepAbility");
            Eq(UnitCommand.CommandType.Move, step.Blueprint.ActionType, "CONJ Side Step is a move action");
            Eq(AbilityRange.Custom, step.Blueprint.Range, "CONJ Side Step uses a custom range");
            Eq(90, step.Blueprint.GetRange(false, step).Value, "CONJ at level 9 Side Step reaches 90 feet");
            classData.Level = 12;
            Eq(120, step.Blueprint.GetRange(false, step).Value, "CONJ Side Step's range follows the actual occultist level");
            classData.Level = 9;
            Eq(10, Asset<BlueprintAbility>("SideStepAbility").CustomRange.Value,
                "CONJ the published base range stays the native ten-foot field, scaled only for the private ability");
        }
        finally { applyNativeEffects = false; }
        unit.RemoveFact(implement);
        Check(unit.Buffs.GetBuff(castingBuff) == null,
            "CONJ removing the Conjuration implement retracts its resonance buff");
        caseStatus["Conjuration"] = "native_private_learning_casting_focus_duration_servitor_tiers_and_level_gates";
    }

    private static void PerformersNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("performers", character, 9, 16);
        var stealth = new ModifiableValueSkill(unit.Stats, unit.Stats.Dexterity, StatType.SkillStealth);
        var thievery = new ModifiableValueSkill(unit.Stats, unit.Stats.Dexterity, StatType.SkillThievery);
        var persuasion = new ModifiableValueSkill(unit.Stats, unit.Stats.Charisma, StatType.SkillPersuasion);
        var magicDevice = new ModifiableValueSkill(unit.Stats, unit.Stats.Charisma, StatType.SkillUseMagicDevice);
        Set(unit.Stats, "SkillStealth", stealth);
        Set(unit.Stats, "SkillThievery", thievery);
        Set(unit.Stats, "SkillPersuasion", persuasion);
        Set(unit.Stats, "SkillUseMagicDevice", magicDevice);
        unit.Stats.AllStats.AddRange(new ModifiableValue[] { stealth, thievery, persuasion, magicDevice });
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("slot-1-Enchantment-Implement"), unit.Descriptor));
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 9);
        var leveling = LevelState(book);
        var selection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("slot-3-ImplementSelection"), 2);
        var pick = new SelectFeature(selection, Asset<BlueprintFeature>("slot-3-PerformersAccoutrements"));
        Check(!pick.Check(leveling, unit.Descriptor), "PANOPLY Performer requires both member implements");
        var illusion = new Feature(Asset<BlueprintFeature>("slot-2-Illusion-Implement"), unit.Descriptor);
        unit.Facts.Add(illusion);
        Check(pick.Check(leveling, unit.Descriptor), "PANOPLY the third native implement slot can choose Performer after both schools");

        var focus = unit.Get<Occultist0099FocusPart>().State;
        var enchantmentFocus = focus.Schools["Enchantment"];
        var illusionFocus = focus.Schools["Illusion"];
        var edgeBlueprint = Asset<BlueprintAbility>("TrickstersEdgeAbility");
        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 5; i++) CastRule(Granted("EnchantmentInvestFocusAbility"));
            for (int i = 0; i < 4; i++) CastRule(Granted("IllusionInvestFocusAbility"));
            CastRule(Granted("LockFocusAbility"));
            Check(!edgeBlueprint.GetComponent<Occultist0099FocusResourceLogic>()
                    .IsAbilityRestrictionPassed(new AbilityData(edgeBlueprint, unit.Descriptor)),
                "PANOPLY locking both schools alone never authorizes a Panoply payment");
            pick.Apply(leveling, unit.Descriptor);
            leveling.OnApplyAction();
            var passive = Asset<BlueprintBuff>("PerformersAccoutrementsResonanceBuff");
            Check(unit.Buffs.GetBuff(passive) != null && stealth.ModifiedValue == 3 && thievery.ModifiedValue == 3,
                "PANOPLY choosing Performer activates Trickster's Skill from the locked member investments");
            var nextSelection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("slot-4-ImplementSelection"), 6);
            var duplicate = new SelectFeature(nextSelection, Asset<BlueprintFeature>("slot-4-PerformersAccoutrements"));
            Check(!duplicate.Check(leveling, unit.Descriptor), "PANOPLY Performer cannot occupy two native slots");

            var list = character.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
                .Single(entry => entry.Panoply == "Performer's Accoutrements" && entry.SlotKey == "slot-3").List.Get();
            var command = Native<BlueprintAbility>("feb70aab86cc17f4bb64432c83737ac2");
            var vanish = Native<BlueprintAbility>("f001c73999fb5a543a199f890108d936");
            Check(list.GetSpells(1).Contains(command) && list.GetSpells(1).Contains(vanish) &&
                  !list.GetSpells(1).Any(spell => spell.AssetGuid == P(Occultist0099AbjurationCatalog.MageShield)) && !list.GetSpells(0).Any(),
                "PANOPLY the private learning list is the member union at the existing circles");
            var learn = new SelectSpell(character.Spellbook, list, 1, vanish, 0);
            Check(NativeCheck(learn, leveling, unit.Descriptor), "PANOPLY the native learner offers a spell from the member union");
            learn.Apply(leveling, unit.Descriptor);
            Check(book.IsKnown(vanish), "PANOPLY the sole Panoply grant learns the chosen root");

            var edge = Granted("TrickstersEdgeAbility");
            var edgeBuff = Asset<BlueprintBuff>("TrickstersEdgeBuff");
            Check(edge.Blueprint.ActionType == UnitCommand.CommandType.Swift && CastRule(edge).Success &&
                  enchantmentFocus.Remaining == 3 && illusionFocus.Remaining == 4 && unit.Buffs.GetBuff(edgeBuff) != null,
                "PANOPLY Trickster's Edge pays two points through the shared highest-balance and tie-break rules");
            Check(stealth.ModifiedValue == 3 && thievery.ModifiedValue == 3,
                "PANOPLY spending member focus does not reduce the locked skill resonance");
            var focusSelection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("FocusPowerSelection"), 9);
            var puppetPick = new SelectFeature(focusSelection, Asset<BlueprintFeature>("PuppetMasterPower"));
            Check(puppetPick.Check(leveling, unit.Descriptor), "PANOPLY level 9 can choose Puppet Master");
            puppetPick.Apply(leveling, unit.Descriptor);
            var puppet = Granted("PuppetMasterAbility");
            Check(puppet.CalculateParams().DC == 17 && puppet.ResourceLogic.CalculateCost(puppet) == 3,
                "PANOPLY Puppet Master uses actual occultist half-level and Intelligence for DC and costs three");
            var classData = unit.Progression.GetClassData(character);
            classData.Level = 8;
            Check(CastRule(puppet).ForceFail && enchantmentFocus.Remaining + illusionFocus.Remaining == 7,
                "PANOPLY level 8 cannot pay for or execute Puppet Master");
            classData.Level = 9;
            unit.RemoveFact(illusion);
            Check(unit.Buffs.GetBuff(passive) == null && unit.Buffs.GetBuff(edgeBuff) == null && !book.IsKnown(vanish) &&
                  !edge.IsVisible() && CastRule(edge).ForceFail && enchantmentFocus.Remaining == 3,
                "PANOPLY losing a member revokes its sole spell source and effects and prevents further payment");
        }
        finally { applyNativeEffects = false; }
        caseStatus["PerformersAccoutrements"] = "native_member_choice_union_learn_atomic_payment_and_source_loss";
    }

    private static void TrappingsNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("trappings", character, 9, 16);
        var bab = new ModifiableValue(unit.Stats, StatType.BaseAttackBonus);
        Set(unit.Stats, "BaseAttackBonus", bab);
        unit.Stats.AllStats.Add(bab);
        var body = Empty<UnitBody>();
        Set(body, "Owner", unit.Descriptor);
        Set(body, "m_HandsEquipmentSets", new[] { new HandsEquipmentSet(unit.Descriptor) });
        Set(unit.Descriptor, "Body", body);
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var abjuration = new Feature(Asset<BlueprintFeature>("slot-1-Abjuration-Implement"), unit.Descriptor);
        var transmutation = new Feature(Asset<BlueprintFeature>("slot-2-Transmutation-Implement"), unit.Descriptor);
        unit.Facts.Add(abjuration);
        var sword = Empty<ItemEntityWeapon>();
        Set(sword, "m_Blueprint", Native<BlueprintItemWeapon>("6fd0a849531617844b195f452661b2cd"));
        body.PrimaryHand.SetFakeItem(sword);
        sword.HoldingSlot = body.PrimaryHand;
        Set(sword, "m_WielderRef", (EntityRef<UnitEntityData>)unit);

        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 9);
        var leveling = LevelState(book);
        var selection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("slot-3-ImplementSelection"), 2);
        var pick = new SelectFeature(selection, Asset<BlueprintFeature>("slot-3-TrappingsOfTheWarrior"));
        Check(!pick.Check(leveling, unit.Descriptor), "TRAP Trappings requires both member implements");
        unit.Facts.Add(transmutation);
        Check(pick.Check(leveling, unit.Descriptor), "TRAP the third native implement slot accepts Trappings after Transmutation");

        var trick = Asset<BlueprintFeatureSelection>("CombatTrickSelection");
        var nativeTricks = Native<BlueprintFeatureSelection>("41c8486641f7d6d4283ca9dae4147a9f");
        Check(trick.AllFeatures.Length == nativeTricks.AllFeatures.Length &&
              trick.AllFeatures.Select(feature => feature.AssetGuid).SequenceEqual(nativeTricks.AllFeatures.Select(feature => feature.AssetGuid)) &&
              trick.GetComponents<PrerequisiteNoFeature>().Any(gate => gate.Feature == trick),
            "TRAP Combat Trick reuses the complete native fighter combat feat pool once");

        var focusPowerSelection = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        var beforeTrappingsPower = leveling.AddSelection(null, character.Progression, focusPowerSelection, 1);
        Check(!new SelectFeature(beforeTrappingsPower, trick).Check(leveling, unit.Descriptor),
            "TRAP FocusPowerSelection blocks Combat Trick before Trappings of the Warrior is chosen");

        pick.Apply(leveling, unit.Descriptor);
        leveling.OnApplyAction();
        Rebuild(unit);

        var focus = unit.Get<Occultist0099FocusPart>().State;
        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var resonance = Asset<BlueprintBuff>("TrappingsOfTheWarriorResonanceBuff");
        var counter = Asset<BlueprintBuff>("CounterstrikeBuff");
        var counterToggle = unit.ActivatableAbilities.Enumerable.Single(ability =>
            ability.Blueprint == Asset<BlueprintActivatableAbility>("CounterstrikeToggle"));
        Check(!counterToggle.IsAvailable && !counterToggle.IsOn &&
              unit.Buffs.GetBuff(counter) == null && unit.Buffs.GetBuff(resonance) == null &&
              !unit.Descriptor.HasFact(trick),
            "TRAP selecting Trappings before focus lock grants disabled CounterstrikeToggle without Combat Trick or active buffs");

        var trickSelectionSlot = leveling.AddSelection(null, character.Progression, focusPowerSelection, 3);
        var trickPick = new SelectFeature(trickSelectionSlot, trick);
        Check(trickPick.Check(leveling, unit.Descriptor),
            "TRAP FocusPowerSelection admits Combat Trick once Trappings and its member implements are owned");
        trickPick.Apply(leveling, unit.Descriptor);
        var nestedTrick = leveling.Selections.Single(s => s.Selection == trick && !s.Selected);
        var combatFeatPick = trick.AllFeatures
            .Select(feat => new SelectFeature(nestedTrick, feat))
            .FirstOrDefault(action => action.Check(leveling, unit.Descriptor));
        Check(combatFeatPick != null,
            "TRAP choosing Combat Trick opens a native nested Fighter combat feat selection");
        var chosenCombatFeat = combatFeatPick.Item.Feature;
        combatFeatPick.Apply(leveling, unit.Descriptor);
        leveling.OnApplyAction();
        Check(unit.Descriptor.HasFact(trick) && unit.Descriptor.HasFact(chosenCombatFeat),
            "TRAP Combat Trick grants the chosen Fighter combat feat from its nested selection");
        var duplicateTrickSlot = leveling.AddSelection(null, character.Progression, focusPowerSelection, 5);
        Check(!new SelectFeature(duplicateTrickSlot, trick).Check(leveling, unit.Descriptor),
            "TRAP Combat Trick cannot be chosen a second time through FocusPowerSelection");

        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 4; i++) CastRule(Granted("InvestFocusAbility"));
            for (int i = 0; i < 4; i++) CastRule(Granted("TransmutationInvestFocusAbility"));
            Check(!counterToggle.IsAvailable && unit.Buffs.GetBuff(resonance) == null && unit.Buffs.GetBuff(counter) == null,
                "TRAP neither Martial Skill nor CounterstrikeToggle is active before focus lock");
            Check(CastRule(Granted("LockFocusAbility")).Success, "TRAP focus lock succeeds");
            Check(unit.Buffs.GetBuff(resonance) != null && bab.ModifiedValue == 2 &&
                  counterToggle.IsAvailable && !counterToggle.IsOn && unit.Buffs.GetBuff(counter) == null,
                "TRAP locking eight invested points grants +2 BAB and unlocks CounterstrikeToggle");
            counterToggle.IsOn = true;
            Check(counterToggle.IsOn && unit.Buffs.GetBuff(counter) != null,
                "TRAP arming CounterstrikeToggle attaches CounterstrikeBuff");

            Rest(unit);
            Check(!counterToggle.IsOn && !counterToggle.IsAvailable &&
                  unit.Buffs.GetBuff(counter) == null && unit.Buffs.GetBuff(resonance) == null &&
                  unit.ActivatableAbilities.Enumerable.Contains(counterToggle),
                "TRAP resting clears CounterstrikeBuff and turns off the toggle while keeping CounterstrikeToggle on the unit");
            Check(unit.Progression.Features.GetFact(chosenCombatFeat).Active,
                "D32 resting without losing the panoply keeps the Combat Trick feat active before re-lock");
            for (int i = 0; i < 4; i++) CastRule(Granted("InvestFocusAbility"));
            for (int i = 0; i < 4; i++) CastRule(Granted("TransmutationInvestFocusAbility"));
            Check(CastRule(Granted("LockFocusAbility")).Success && counterToggle.IsAvailable,
                "TRAP re-locking focus after rest re-enables CounterstrikeToggle");
            counterToggle.IsOn = true;
            Check(counterToggle.IsOn && unit.Buffs.GetBuff(counter) != null && bab.ModifiedValue == 2,
                "TRAP re-arming CounterstrikeToggle on Day 2 restores CounterstrikeBuff and Martial Skill");
            var remaining = focus.Schools["Abjuration"].Remaining + focus.Schools["Transmutation"].Remaining;
            var spent = Occultist0099Focus.PayPanoply(focus, "Trappings of the Warrior", 2, "TRAP:paid-resonance");
            Check(spent.Applied && spent.FromMembers.Count == 2 &&
                  focus.Schools["Abjuration"].Remaining + focus.Schools["Transmutation"].Remaining == remaining - 2 &&
                  bab.ModifiedValue == 2,
                "TRAP a two-point Panoply payment is atomic and leaves the locked BAB resonance unchanged");

            var combat = Empty<Kingmaker.Controllers.Combat.UnitCombatState>();
            Set(combat, "Unit", unit);
            var cooldownField = AccessTools.Field(typeof(Kingmaker.Controllers.Combat.UnitCombatState), "Cooldown");
            var cooldown = FormatterServices.GetUninitializedObject(cooldownField.FieldType);
            Set(cooldown, "SwiftAction", 0f);
            Set(combat, "Cooldown", cooldown);
            Set(unit, "CombatState", combat);
            var enemy = Unit("trappings-enemy", null, 0, 10);
            var hit = Empty<RuleAttackRoll>();
            Set(hit, "Result", AttackResult.Hit);
            var attack = Empty<RuleAttackWithWeapon>();
            Set(attack, "Weapon", sword);
            Set(attack, "Initiator", enemy);
            Set(attack, "Target", unit);
            Set(attack, "AttackRoll", hit);
            var reach = AccessTools.Method(typeof(UnitHelper), nameof(UnitHelper.IsAttackOfOpportunityReach));
            var available = AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.Combat.UnitCombatState), "CanAttackOfOpportunity");
            var opportunity = AccessTools.Method(typeof(Kingmaker.Controllers.Combat.UnitCombatState), "AttackOfOpportunity");
            var allow = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(AllowTrue));
            var oppBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(OpportunityBoundary));
            counterHand = body.PrimaryHand;
            var hand = AccessTools.Method(typeof(UnitHelper), "GetThreatHand");
            var threatRange = AccessTools.Method(typeof(UnitHelper), "GetThreatRange");
            var handBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(CounterThreatHand));
            var rangeBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(CounterThreatRange));
            harmony.Patch(reach, prefix: new HarmonyMethod(allow));
            harmony.Patch(available, prefix: new HarmonyMethod(allow));
            harmony.Patch(opportunity, prefix: new HarmonyMethod(oppBoundary));
            harmony.Patch(hand, prefix: new HarmonyMethod(handBoundary));
            harmony.Patch(threatRange, prefix: new HarmonyMethod(rangeBoundary));
            try
            {
                var runtime = unit.Buffs.GetBuff(counter).Components.Single(component =>
                    component.SourceBlueprintComponent is Occultist0099Counterstrike);
                var zeroAttack = Empty<RuleAttackWithWeapon>();
                Set(zeroAttack, "Weapon", sword);
                Set(zeroAttack, "Initiator", enemy);
                Set(zeroAttack, "Target", unit);
                Set(zeroAttack, "AttackRoll", hit);
                var zeroDamage = new RuleDealDamage(enemy, unit, new DamageBundle()) { Reason = new RuleReason(zeroAttack) };
                Set(zeroDamage, "Result", 0);
                Set(zeroAttack, "MeleeDamage", zeroDamage);

                var nullDamageAttack = Empty<RuleAttackWithWeapon>();
                Set(nullDamageAttack, "Weapon", sword);
                Set(nullDamageAttack, "Initiator", enemy);
                Set(nullDamageAttack, "Target", unit);
                Set(nullDamageAttack, "AttackRoll", hit);

                var beforeCounter = focus.Schools["Abjuration"].Remaining + focus.Schools["Transmutation"].Remaining;
                allowOpportunity = true;
                Occultist0099NativeScope.InRuntimeContext(runtime, component =>
                {
                    var cs = (Occultist0099Counterstrike)component;
                    cs.OnEventDidTrigger(nullDamageAttack);
                    cs.OnEventDidTrigger(zeroDamage);
                    cs.OnEventDidTrigger(zeroAttack);
                });
                Check(focus.Schools["Abjuration"].Remaining + focus.Schools["Transmutation"].Remaining == beforeCounter &&
                      (float)Get(cooldown, "SwiftAction") == 0f,
                    "TRAP a zero-damage or damage-free melee hit does not attempt Counterstrike or spend Focus or Swift");

                var refusedAttack = Empty<RuleAttackWithWeapon>();
                Set(refusedAttack, "Weapon", sword);
                Set(refusedAttack, "Initiator", enemy);
                Set(refusedAttack, "Target", unit);
                Set(refusedAttack, "AttackRoll", hit);
                var refusedDamage = new RuleDealDamage(enemy, unit, new DamageBundle()) { Reason = new RuleReason(refusedAttack) };
                Set(refusedDamage, "Result", 5);
                Set(refusedAttack, "MeleeDamage", refusedDamage);
                allowOpportunity = false;
                Occultist0099NativeScope.InRuntimeContext(runtime, component =>
                    ((Occultist0099Counterstrike)component).OnEventDidTrigger(refusedDamage));
                Check(focus.Schools["Abjuration"].Remaining + focus.Schools["Transmutation"].Remaining == beforeCounter &&
                      (float)Get(cooldown, "SwiftAction") == 0f,
                    "TRAP when native AttackOfOpportunity refuses, Counterstrike spends neither Focus nor Swift");

                var damage = new RuleDealDamage(enemy, unit, new DamageBundle()) { Reason = new RuleReason(attack) };
                Set(damage, "Result", 5);
                Set(attack, "MeleeDamage", damage);
                allowOpportunity = true;
                var tryRule = AccessTools.Method(typeof(Occultist0099Counterstrike), "Try",
                    new[] { typeof(RuleAttackWithWeapon), typeof(RuleDealDamage) });
                bool duplicate = true;
                Occultist0099NativeScope.InRuntimeContext(runtime, component =>
                {
                    var cs = (Occultist0099Counterstrike)component;
                    cs.OnEventDidTrigger(damage);
                    cs.OnEventDidTrigger(attack);
                    duplicate = (bool)tryRule.Invoke(cs, new object[] { attack, damage });
                });
                Check(!duplicate &&
                      focus.Schools["Abjuration"].Remaining + focus.Schools["Transmutation"].Remaining == beforeCounter - 1 &&
                      (float)Get(cooldown, "SwiftAction") > 0f,
                    "TRAP a damaging melee hit triggers Counterstrike once, spends one Focus and Swift, and ignores duplicate callbacks");
            }
            finally
            {
                allowOpportunity = false;
                harmony.Unpatch(reach, allow);
                harmony.Unpatch(available, allow);
                harmony.Unpatch(opportunity, oppBoundary);
                harmony.Unpatch(hand, handBoundary);
                harmony.Unpatch(threatRange, rangeBoundary);
                counterHand = null;
            }

            unit.RemoveFact(transmutation);
            Check(unit.Buffs.GetBuff(resonance) == null && unit.Buffs.GetBuff(counter) == null &&
                  !counterToggle.IsOn && !counterToggle.IsAvailable && bab.ModifiedValue == 0,
                "TRAP losing Transmutation revokes Martial Skill, CounterstrikeBuff, and CounterstrikeToggle availability");
            var combatFact = unit.Progression.Features.GetFact(chosenCombatFeat);
            Check(combatFact != null && combatFact.Active && combatFact.IsEnabled,
                "TRAP already-learned Combat Trick feat stays active as a normal feat per Kingmaker original behavior");
        }
        finally { applyNativeEffects = false; }
        caseStatus["TrappingsOfTheWarrior"] = "native_member_choice_martial_skill_counterstrike_dedupe_and_combat_trick";
    }

    private static void ParaphernaliaNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("paraphernalia", character, 12, 22);
        foreach (var stat in new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld,
                     StatType.SkillLoreNature, StatType.SkillLoreReligion })
        {
            var skill = new ModifiableValueSkill(unit.Stats, unit.Stats.Intelligence, stat);
            Set(unit.Stats, stat.ToString(), skill);
            unit.Stats.AllStats.Add(skill);
        }
        var loreStats = new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld,
            StatType.SkillLoreNature, StatType.SkillLoreReligion };
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var divination = new Feature(Asset<BlueprintFeature>("slot-1-Divination-Implement"), unit.Descriptor);
        var evocation = new Feature(Asset<BlueprintFeature>("slot-2-Evocation-Implement"), unit.Descriptor);
        var necromancy = new Feature(Asset<BlueprintFeature>("slot-3-Necromancy-Implement"), unit.Descriptor);
        unit.Facts.Add(divination);
        unit.Facts.Add(evocation);
        unit.Facts.Add(necromancy);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 12);
        var leveling = LevelState(book);
        var selection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("slot-4-ImplementSelection"), 6);
        var pick = new SelectFeature(selection, Asset<BlueprintFeature>("slot-4-MagesParaphernalia"));
        Check(pick.Check(leveling, unit.Descriptor), "MAGE native implement slot accepts Mage's Paraphernalia after its three members");

        var focus = unit.Get<Occultist0099FocusPart>().State;
        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var resonance = Asset<BlueprintBuff>("MagesParaphernaliaResonanceBuff");
        var player = Kingmaker.Game.Instance.Player;
        var startRealTime = player.RealTime;
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 4; i++) CastRule(Granted("DivinationInvestFocusAbility"));
            for (int i = 0; i < 4; i++) CastRule(Granted("EvocationInvestFocusAbility"));
            for (int i = 0; i < 4; i++) CastRule(Granted("NecromancyInvestFocusAbility"));
            Check(CastRule(Granted("LockFocusAbility")).Success, "MAGE focus lock succeeds");
            pick.Apply(leveling, unit.Descriptor);
            leveling.OnApplyAction();
            int intelligenceBonus = (unit.Stats.Intelligence.ModifiedValue - 10) / 2;
            Check(unit.Buffs.GetBuff(resonance) != null &&
                  Asset<Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty>("ScholarlyKnowledgeValue").GetInt(unit) == 3 &&
                  loreStats.All(stat => unit.Stats.GetStat(stat).ModifiedValue == intelligenceBonus + 3),
                "MAGE Scholarly Knowledge gives +3 to the four lore skills at twelve invested points");

            var focusPowerSelection = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
            var knowledgeSel = Asset<BlueprintFeatureSelection>("MetamagicKnowledgeSelection");
            var masterPower = Asset<BlueprintFeature>("MetamagicMasterPower");
            var masterFeature = Asset<BlueprintFeature>("MetamagicMasterFeature");
            Check(!unit.Descriptor.HasFact(knowledgeSel) && !unit.Descriptor.HasFact(masterFeature) &&
                  !unit.ActivatableAbilities.Enumerable.Any(a => a.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("MetamagicMasterEmpowerToggle")),
                "MAGE Paraphernalia grants Spell Power automatically while withholding Metamagic Knowledge and Metamagic Master for FocusPowerSelection");

            var knowledgeSlot = leveling.AddSelection(null, character.Progression, focusPowerSelection, 7);
            var knowledgePick = new SelectFeature(knowledgeSlot, knowledgeSel);
            Check(knowledgePick.Check(leveling, unit.Descriptor),
                "MAGE FocusPowerSelection admits MetamagicKnowledgeSelection once Paraphernalia is owned");
            knowledgePick.Apply(leveling, unit.Descriptor);
            var nestedMeta = leveling.Selections.Single(s => s.Selection == knowledgeSel && !s.Selected);
            var empower = Native<BlueprintFeature>("a1de1e4f92195b442adb946f0e2b9d4e");
            var empowerPick = new SelectFeature(nestedMeta, empower);
            Check(empowerPick.Check(leveling, unit.Descriptor),
                "MAGE Metamagic Knowledge opens its native nested Wizard metamagic feat selection");
            empowerPick.Apply(leveling, unit.Descriptor);
            leveling.OnApplyAction();
            Check(unit.Descriptor.HasFact(knowledgeSel) && unit.Descriptor.HasFact(empower),
                "MAGE Metamagic Knowledge grants the selected metamagic feat");
            var dupKnowledgeSlot = leveling.AddSelection(null, character.Progression, focusPowerSelection, 9);
            Check(!new SelectFeature(dupKnowledgeSlot, knowledgeSel).Check(leveling, unit.Descriptor),
                "MAGE Metamagic Knowledge cannot be selected twice");

            var masterSlot = leveling.AddSelection(null, character.Progression, focusPowerSelection, 11);
            var masterPick = new SelectFeature(masterSlot, masterPower);
            Check(masterPick.Check(leveling, unit.Descriptor),
                "MAGE FocusPowerSelection admits MetamagicMasterPower once Paraphernalia is owned");
            masterPick.Apply(leveling, unit.Descriptor);
            leveling.OnApplyAction();
            Check(unit.Descriptor.HasFact(masterFeature),
                "MAGE selecting MetamagicMasterPower grants Metamagic Master toggles");

            var bonus2 = unit.ActivatableAbilities.Enumerable.Single(ability => ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("SpellPower2Toggle"));
            var bonus3 = unit.ActivatableAbilities.Enumerable.Single(ability => ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("SpellPower3Toggle"));
            var bonus4 = unit.ActivatableAbilities.Enumerable.Single(ability => ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("SpellPower4Toggle"));
            unit.Progression.GetClassData(character).Level = 11;
            Check(bonus2.IsAvailable && !bonus3.IsAvailable && !bonus4.IsAvailable,
                "MAGE +2 is open at level eleven while +3 and +4 are not");
            unit.Progression.GetClassData(character).Level = 12;
            Check(bonus2.IsAvailable && bonus3.IsAvailable && !bonus4.IsAvailable,
                "MAGE +3 opens at level twelve while +4 is not");
            unit.Progression.GetClassData(character).Level = 18;
            Check(bonus4.IsAvailable, "MAGE +4 opens at level eighteen");
            unit.Progression.GetClassData(character).Level = 12;

            var dummy = new BlueprintAbility { name = "0099N.MageEligible", AssetGuid = P(Occultist0099Ids.Id("HostMageSpell")),
                Type = AbilityType.Spell, Range = AbilityRange.Personal, CanTargetSelf = true,
                AvailableMetamagic = Metamagic.Empower | Metamagic.Extend | Metamagic.Quicken | Metamagic.Reach,
                ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation } } };
            dummy.OnEnable();
            var spell = new AbilityData(dummy, book, 1);
            int baseCL = spell.CalculateParams().CasterLevel;
            bonus2.IsOn = true;
            Eq(baseCL + 2, spell.CalculateParams().CasterLevel, "MAGE arming +2 only changes private-book cast parameters");
            var armedSpell = new AbilityData(dummy, book, 1);
            var armedFact = new Ability(dummy, unit.Descriptor) { Spellbook = book };
            Set(armedFact, "OverrideParams", armedSpell.CalculateParams());
            Set(armedSpell, "m_Fact", armedFact);
            int remaining = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            var cast = CastRule(armedSpell);
            int afterCast = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            Check(cast.Success && cast.Context.Params.CasterLevel == baseCL + 2 &&
                  afterCast == remaining - 2,
                "MAGE a qualifying private cast gains +2 caster level and pays exactly two Panoply points");
            bonus2.IsOn = false;

            var foreignBook = new BlueprintSpellbook { AssetGuid = P(Occultist0099Ids.Id("HostMageForeignBook")),
                name = "0099N.MageForeignBook", Spontaneous = true, IsArcane = false, CastingAttribute = StatType.Intelligence,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            Set(foreignBook, "m_SpellList", Ref<BlueprintSpellListReference>(character.Spellbook.SpellList));
            Set(foreignBook, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(character.Spellbook.SpellsPerDay));
            foreignBook.OnEnable();
            var foreignSpellbook = unit.Descriptor.DemandSpellbook(foreignBook);
            Set(foreignSpellbook, "m_BaseLevelInternal", 12);
            var foreignSpell = new AbilityData(dummy, foreignSpellbook, 1);
            var foreignFact = new Ability(dummy, unit.Descriptor);
            Set(foreignFact, "OverrideParams", foreignSpell.CalculateParams());
            Set(foreignSpell, "m_Fact", foreignFact);
            remaining = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            var foreignCast = CastRule(foreignSpell);
            Check(foreignCast.Success && foreignCast.Context.Params.CasterLevel == baseCL &&
                  focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining == remaining,
                "MAGE a multiclass book sharing the private root gets no caster-level bonus and no debit");

            var extend = Native<BlueprintFeature>("f180e72e4a9cbaa4da8be9bc958132ef");
            unit.Facts.Add(new Feature(extend, unit.Descriptor));
            var metaToggle = unit.ActivatableAbilities.Enumerable.Single(ability => ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("MetamagicMasterEmpowerToggle"));
            var extendToggle = unit.ActivatableAbilities.Enumerable.Single(ability => ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("MetamagicMasterExtendToggle"));
            metaToggle.IsOn = true;
            int cost = (int)AccessTools.Method(typeof(Kingmaker.UnitLogic.Abilities.MetamagicHelper), "DefaultCost")
                .Invoke(null, new object[] { Metamagic.Empower });
            remaining = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            var metaSpell = new AbilityData(dummy, book, 1);
            var metaFact = new Ability(dummy, unit.Descriptor);
            Set(metaFact, "OverrideParams", metaSpell.CalculateParams());
            Set(metaSpell, "m_Fact", metaFact);
            var metaCast = CastRule(metaSpell);
            Check(metaCast.Success && (metaCast.Context.Params.Metamagic & Metamagic.Empower) != 0 &&
                  focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining == remaining - cost,
                "MAGE a known and applicable metamagic is applied free of slot cost and charged its original level increase");
            var again = (bool)AccessTools.Method(Production("Occultist0099Paraphernalia"), "TryCast")
                .Invoke(null, new object[] { metaCast });
            Check(!again && focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining == remaining - cost,
                "MAGE a repeated cast event cannot charge the same metamagic twice");
            metaToggle.IsOn = false;

            // Quicken and Reach must enter AbilityData.HasMetamagic, UnitUseAbility command
            // creation, BlueprintAbility.GetRange and MechanicsContext.Recalculate when funded.
            var quickenFeat = Native<BlueprintFeature>("ef7ece7bb5bb66a41b256976b27f424e");
            var reachFeat = Native<BlueprintFeature>("46fad72f54a33dc4692d3b62eca7bb78");
            unit.Facts.Add(new Feature(quickenFeat, unit.Descriptor));
            unit.Facts.Add(new Feature(reachFeat, unit.Descriptor));
            var quickenToggle = unit.ActivatableAbilities.Enumerable.Single(ability =>
                ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("MetamagicMasterQuickenToggle"));
            var reachToggle = unit.ActivatableAbilities.Enumerable.Single(ability =>
                ability.Blueprint == Asset<Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility>("MetamagicMasterReachToggle"));
            var closeRoot = new BlueprintAbility { name = "0099N.MageCloseSpell", AssetGuid = P(Occultist0099Ids.Id("HostMageCloseSpell")),
                Type = AbilityType.Spell, Range = AbilityRange.Close, ActionType = UnitCommand.CommandType.Standard,
                CanTargetSelf = true, AvailableMetamagic = Metamagic.Quicken | Metamagic.Reach,
                ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation } } };
            closeRoot.OnEnable();
            var touchDelivery = new BlueprintAbility { name = "0099N.MageTouchDelivery", AssetGuid = P(Occultist0099Ids.Id("HostMageTouchDelivery")),
                Type = AbilityType.Spell, Range = AbilityRange.Touch, ActionType = UnitCommand.CommandType.Standard,
                CanTargetSelf = true,
                ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation } } };
            touchDelivery.OnEnable();
            var stickyTouch = new Kingmaker.UnitLogic.Abilities.Components.AbilityEffectStickyTouch { name = "0099N.MageSticky" };
            Set(stickyTouch, "m_TouchDeliveryAbility", Ref<BlueprintAbilityReference>(touchDelivery));
            var touchRoot = new BlueprintAbility { name = "0099N.MageTouchRoot", AssetGuid = P(Occultist0099Ids.Id("HostMageTouchRoot")),
                Type = AbilityType.Spell, Range = AbilityRange.Touch, ActionType = UnitCommand.CommandType.Standard,
                CanTargetSelf = true, AvailableMetamagic = Metamagic.Reach,
                ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation }, stickyTouch } };
            touchRoot.OnEnable();
            var closeData = new AbilityData(closeRoot, book, 1);
            var touchData = new AbilityData(touchRoot, book, 1);
            Check(!closeData.HasMetamagic(Metamagic.Quicken) && !closeData.HasMetamagic(Metamagic.Reach) &&
                  closeData.ActionType == UnitCommand.CommandType.Standard &&
                  Kingmaker.UnitLogic.Commands.UnitUseAbility.CreateCastCommand(closeData, new TargetWrapper(unit)).Type == UnitCommand.CommandType.Standard &&
                  closeData.Blueprint.GetRange(closeData.HasMetamagic(Metamagic.Reach), closeData) == 30.Feet() &&
                  touchData.Blueprint.GetRange(touchData.HasMetamagic(Metamagic.Reach), touchData) == 0.Feet(),
                "MAGE baseline spell keeps standard action and base range before Quicken or Reach toggles are armed");

            var groups = unit.Ensure<Kingmaker.UnitLogic.Parts.UnitPartActivatableAbility>();
            Check(quickenToggle.Blueprint.Group != ActivatableAbilityGroup.None &&
                  quickenToggle.Blueprint.Group == reachToggle.Blueprint.Group &&
                  groups.GetGroupSize(quickenToggle.Blueprint.Group) == 1 &&
                  bonus2.Blueprint.Group != ActivatableAbilityGroup.None &&
                  bonus2.Blueprint.Group == bonus3.Blueprint.Group &&
                  bonus3.Blueprint.Group == bonus4.Blueprint.Group &&
                  bonus2.Blueprint.Group != quickenToggle.Blueprint.Group &&
                  groups.GetGroupSize(bonus2.Blueprint.Group) == 1,
                "MAGE Spell Power and Metamagic Master use separate dedicated native groups each with one slot");
            void SelectMetamagic(ActivatableAbility toggle)
            {
                // The stock switch orders modes by activation time.
                player.RealTime += TimeSpan.FromMilliseconds(1);
                toggle.IsOn = true;
            }
            SelectMetamagic(quickenToggle);
            var quickenedCommand = (UnitUseAbility)UnitUseAbility.CreateCastCommand(closeData, new TargetWrapper(unit));
            Check(quickenToggle.IsOn && !reachToggle.IsOn && closeData.HasMetamagic(Metamagic.Quicken) &&
                  !closeData.HasMetamagic(Metamagic.Reach) && closeData.ActionType == UnitCommand.CommandType.Swift &&
                  quickenedCommand.Type == UnitCommand.CommandType.Swift &&
                  closeData.Blueprint.GetRange(false, closeData) == 30.Feet(),
                "MAGE Quicken creates a Swift command at the spell's original range");
            SelectMetamagic(reachToggle);
            var reachedTouchCommand = (Kingmaker.UnitLogic.Commands.UnitUseAbility)
                Kingmaker.UnitLogic.Commands.UnitUseAbility.CreateCastCommand(touchData, new TargetWrapper(unit));
            Check(reachToggle.IsOn && !quickenToggle.IsOn && quickenToggle.AppliedBuff == null &&
                  !closeData.HasMetamagic(Metamagic.Quicken) && closeData.HasMetamagic(Metamagic.Reach) &&
                  closeData.ActionType == UnitCommand.CommandType.Standard &&
                  closeData.Blueprint.GetRange(closeData.HasMetamagic(Metamagic.Reach), closeData) == 40.Feet() &&
                  touchData.Blueprint.GetRange(touchData.HasMetamagic(Metamagic.Reach), touchData) == 30.Feet() &&
                  reachedTouchCommand.Ability.Blueprint == touchDelivery &&
                  reachedTouchCommand.Ability.HasMetamagic(Metamagic.Reach) &&
                  reachedTouchCommand.Ability.Blueprint.GetRange(reachedTouchCommand.Ability.HasMetamagic(Metamagic.Reach), reachedTouchCommand.Ability) == 30.Feet(),
                "MAGE selecting Reach switches Quicken off and creates a standard ranged touch command");

            int MageBalance() => focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining +
                focus.Schools["Necromancy"].Remaining;
            int beforeQueued = MageBalance();
            var staleQuicken = CastRule(quickenedCommand.Ability, quickenedCommand);
            Check(staleQuicken.Context.CommandType == UnitCommand.CommandType.Swift &&
                  staleQuicken.ForceFail && !staleQuicken.Success && MageBalance() == beforeQueued,
                "MAGE a queued Quicken command is rejected without spending after switching to Reach");
            SelectMetamagic(quickenToggle);
            var staleReach = CastRule(reachedTouchCommand.Ability, reachedTouchCommand);
            Check(quickenToggle.IsOn && !reachToggle.IsOn && reachToggle.AppliedBuff == null &&
                  staleReach.ForceFail && !staleReach.Success && MageBalance() == beforeQueued,
                "MAGE switching back to Quicken turns Reach off and invalidates its queued command without spending");

            var previousCombatState = unit.CombatState;
            var previousCommands = unit.Commands;
            try
            {
                Set(unit, "Commands", new Kingmaker.UnitLogic.Commands.UnitCommands(unit));
                var mageCombat = Empty<Kingmaker.Controllers.Combat.UnitCombatState>();
                Set(mageCombat, "Unit", unit);
                Set(mageCombat, "m_InCombat", true);
                var mageCooldownField = AccessTools.Field(typeof(Kingmaker.Controllers.Combat.UnitCombatState), "Cooldown");
                var mageCooldown = FormatterServices.GetUninitializedObject(mageCooldownField.FieldType);
                Set(mageCooldown, "SwiftAction", 6f);
                Set(mageCooldown, "StandardAction", 0f);
                Set(mageCombat, "Cooldown", mageCooldown);
                Set(unit, "CombatState", mageCombat);

                quickenToggle.IsOn = true;
                int beforeSwiftCd = MageBalance();
                var stdCmd = (UnitUseAbility)UnitUseAbility.CreateCastCommand(closeData, new TargetWrapper(unit));
                Check(beforeSwiftCd >= 4 && !closeData.HasMetamagic(Metamagic.Quicken) &&
                      closeData.ActionType == UnitCommand.CommandType.Standard &&
                      stdCmd.Type == UnitCommand.CommandType.Standard,
                    "MAGE Quicken stays off ActionType and CreateCastCommand when swift action is on cooldown");
                var stdCast = CastRule(stdCmd.Ability, stdCmd);
                Check(stdCast.Success && MageBalance() == beforeSwiftCd &&
                      (stdCast.Context.Params.Metamagic & Metamagic.Quicken) == 0,
                    "MAGE casting a standard-downgraded spell while swift is on cooldown spends no Quicken focus");

                var stdCmd2 = (UnitUseAbility)UnitUseAbility.CreateCastCommand(closeData, new TargetWrapper(unit));
                quickenToggle.IsOn = false;
                var stdCast2 = CastRule(stdCmd2.Ability, stdCmd2);
                Check(stdCast2.Success && !stdCast2.ForceFail && MageBalance() == beforeSwiftCd,
                    "MAGE disarming Quicken after a swift-cooldown standard command still allows execution without spending");

                Set(mageCooldown, "SwiftAction", 0f);
                quickenToggle.IsOn = true;
                var queuedSwift = (UnitUseAbility)UnitUseAbility.CreateCastCommand(closeData, new TargetWrapper(unit));
                Check(queuedSwift.Type == UnitCommand.CommandType.Swift,
                    "MAGE clearing swift cooldown lets Quicken build a Swift command");
                Set(mageCooldown, "SwiftAction", 6f);
                var staleSwiftCast = CastRule(queuedSwift.Ability, queuedSwift);
                Check(staleSwiftCast.ForceFail && !staleSwiftCast.Success && MageBalance() == beforeSwiftCd,
                    "MAGE a queued Swift command is rejected without spending once swift action enters cooldown");
            }
            finally
            {
                Set(unit, "CombatState", previousCombatState);
                Set(unit, "Commands", previousCommands);
            }

            remaining = MageBalance();
            var qrFact = new Ability(closeRoot, unit.Descriptor) { Spellbook = book };
            var qrData = new AbilityData(closeRoot, book, 1);
            Set(qrFact, "OverrideParams", qrData.CalculateParams());
            Set(qrData, "m_Fact", qrFact);
            var fundedCommand = (UnitUseAbility)UnitUseAbility.CreateCastCommand(qrData, new TargetWrapper(unit));
            var qrCast = CastRule(fundedCommand.Ability, fundedCommand);
            int afterQr = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            quickenToggle.IsOn = false;
            reachToggle.IsOn = false;
            qrCast.Context.Recalculate();
            var childCtx = qrCast.Context.CloneFor(closeRoot);
            childCtx.SetParams(AbilityParams.Empty);
            childCtx.Recalculate();
            Check(qrCast.Success && fundedCommand.Type == UnitCommand.CommandType.Swift && afterQr == remaining - 4 &&
                  (qrFact.OverrideParams.Metamagic & (Metamagic.Quicken | Metamagic.Reach)) == 0 &&
                  (qrCast.Context.Params.Metamagic & (Metamagic.Quicken | Metamagic.Reach)) == Metamagic.Quicken &&
                  (childCtx.Params.Metamagic & (Metamagic.Quicken | Metamagic.Reach)) == Metamagic.Quicken,
                "MAGE funded Quicken alone deducts 4 points and survives recalculation after its toggle turns off");
            SelectMetamagic(reachToggle);
            var fundedReach = (UnitUseAbility)UnitUseAbility.CreateCastCommand(touchData, new TargetWrapper(unit));
            var reachCast = CastRule(fundedReach.Ability, fundedReach);
            Check(reachCast.Success && fundedReach.Type == UnitCommand.CommandType.Standard &&
                  fundedReach.Ability.Blueprint == touchDelivery && MageBalance() == afterQr - 1 &&
                  (reachCast.Context.Params.Metamagic & (Metamagic.Quicken | Metamagic.Reach)) == Metamagic.Reach,
                "MAGE the live converted Reach command applies only Reach and deducts one point");
            reachToggle.IsOn = false;

            var detachedPrivate = Empty<AbilityData>();
            Set(detachedPrivate, "Blueprint", closeRoot);
            Set(detachedPrivate, "m_SpellbookBlueprint", character.Spellbook);
            var detachedForeign = Empty<AbilityData>();
            Set(detachedForeign, "Blueprint", closeRoot);
            Set(detachedForeign, "m_SpellbookBlueprint", foreignBook);
            Check(!detachedPrivate.HasMetamagic(Metamagic.Quicken) &&
                  !detachedForeign.HasMetamagic(Metamagic.Quicken),
                "MAGE MimicMetamagicEligibility01458Patch ignores caster-less AbilityData without throwing");
            extendToggle.IsOn = true;
            var extendOnce = new MetamagicOnNextSpell { name = "ExtendOnce", Metamagic = Metamagic.Extend };
            var onceBlueprint = new BlueprintBuff
            {
                name = "0099N.NativeExtendOnce", AssetGuid = P(Occultist0099Ids.Id("HostNativeExtendOnce")),
                ComponentsArray = new BlueprintComponent[] { extendOnce }
            };
            onceBlueprint.OnEnable();
            var extendBuff = unit.Buffs.AddBuff(onceBlueprint, unit, (TimeSpan?)null);
            var extendRuntime = extendBuff.Components.Single(component => component.SourceBlueprintComponent == extendOnce);
            var oneShotSpell = new AbilityData(dummy, book, 1);
            var nativeParams = new RuleCalculateAbilityParams(unit, oneShotSpell);
            using (extendRuntime.RequestEventContext())
                extendOnce.OnEventAboutToTrigger(nativeParams);
            nativeParams.OnTrigger(null);
            var oneShotCast = new RuleCastSpell(oneShotSpell, new TargetWrapper(unit));
            oneShotCast.Context.SetParams(nativeParams.Result);
            using (extendRuntime.RequestEventContext())
                extendOnce.OnEventAboutToTrigger(oneShotCast);
            int beforeOneShot = MageBalance();
            Rulebook.Trigger(oneShotCast);
            Check(!extendBuff.Active && oneShotCast.Success && MageBalance() == beforeOneShot &&
                  (oneShotCast.Context.Params.Metamagic & Metamagic.Extend) != 0,
                "MAGE native one-shot Extend remains free after its native buff is consumed before commit");
            extendToggle.IsOn = false;
            for (int i = 0; i < 3; i++)
                Check(CastRule(Granted("DivinationReplenishFocusAbility")).Success,
                    "MAGE replenishes 3 generic points before multi-toggle Spell Power test");

            // Spell Power toggles switch within their own size-1 group (separate from Metamagic Master),
            // and the multi-buff fallback still selects only +4 CL (+4, cost 4), never +9 CL.
            unit.Progression.GetClassData(character).Level = 18;
            Set(book, "m_BaseLevelInternal", 18);
            int baseCL18 = spell.CalculateParams().CasterLevel;
            void SelectSpellPower(ActivatableAbility toggle)
            {
                player.RealTime += TimeSpan.FromMilliseconds(1);
                toggle.IsOn = true;
            }
            SelectSpellPower(bonus2);
            SelectSpellPower(bonus3);
            Check(bonus3.IsOn && !bonus2.IsOn && bonus2.AppliedBuff == null,
                "MAGE selecting Spell Power (+3) switches Spell Power (+2) off within its exclusive group");
            SelectSpellPower(bonus4);
            Check(bonus4.IsOn && !bonus3.IsOn && bonus3.AppliedBuff == null,
                "MAGE selecting Spell Power (+4) switches Spell Power (+3) off within its exclusive group");
            SelectMetamagic(extendToggle);
            Check(bonus4.IsOn && extendToggle.IsOn,
                "MAGE Spell Power and Metamagic Master keep separate groups so one of each can stay active");
            extendToggle.IsOn = false;
            var forcedBuff2 = unit.Buffs.AddBuff(Asset<BlueprintBuff>("SpellPower2Buff"), unit, (TimeSpan?)null);
            var forcedBuff3 = unit.Buffs.AddBuff(Asset<BlueprintBuff>("SpellPower3Buff"), unit, (TimeSpan?)null);
            Eq(baseCL18 + 4, spell.CalculateParams().CasterLevel,
                "MAGE toggling Spell Power and force-attaching all three buffs selects only +4 CL instead of accumulating +9");
            remaining = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            var multiSpell = new AbilityData(dummy, book, 1);
            var multiFact = new Ability(dummy, unit.Descriptor);
            Set(multiFact, "OverrideParams", multiSpell.CalculateParams());
            Set(multiSpell, "m_Fact", multiFact);
            var multiCast = CastRule(multiSpell);
            int afterMulti = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            Check(multiCast.Success && multiCast.Context.Params.CasterLevel == baseCL18 + 4 && afterMulti == remaining - 4,
                "MAGE parameter calculation and cast payment share the single +4 Spell Power tier and deduct 4 points");
            forcedBuff3?.Remove();
            forcedBuff2?.Remove();
            bonus3.IsOn = false;
            bonus4.IsOn = false;
            bonus2.IsOn = false;
            Set(book, "m_BaseLevelInternal", 12);
            unit.Progression.GetClassData(character).Level = 12;

            // Combined Spell Power (+2, cost 2) + Extend (cost 1) with exact balance = 2:
            // must reject atomically with zero partial deduction, then succeed at balance = 3.
            while (focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining > 2)
                Occultist0099Focus.PayPanoply(focus, "Mage's Paraphernalia", 1, "mage-drain-" + (++serial));
            Eq(2, focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining,
                "MAGE fixture drains Panoply balance to exactly two points");
            quickenToggle.IsOn = true;
            Check(!closeData.HasMetamagic(Metamagic.Quicken) && closeData.ActionType == UnitCommand.CommandType.Standard,
                "MAGE Quicken (cost 4) stays inactive in native queries when Panoply balance is only 2");
            quickenToggle.IsOn = false;

            bonus2.IsOn = true;
            extendToggle.IsOn = true;
            Eq(baseCL, spell.CalculateParams().CasterLevel,
                "MAGE combined cost (2 + 1) exceeding balance 2 does not pre-grant +2 CL");
            var splitSpell = new AbilityData(dummy, book, 1);
            var splitFact = new Ability(dummy, unit.Descriptor);
            Set(splitFact, "OverrideParams", splitSpell.CalculateParams());
            Set(splitSpell, "m_Fact", splitFact);
            var splitFail = CastRule(splitSpell);
            int balanceAfterFail = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            Check(splitFail.ForceFail && !splitFail.Success && balanceAfterFail == 2,
                "MAGE Spell Power (+2) + 1-pt Metamagic Master with balance 2 fails atomically without partial deduction");
            Check(CastRule(Granted("DivinationReplenishFocusAbility")).Success &&
                  focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining == 3,
                "MAGE replenishing 1 generic point raises Panoply balance to 3");
            var splitOkSpell = new AbilityData(dummy, book, 1);
            var splitOkFact = new Ability(dummy, unit.Descriptor);
            Set(splitOkFact, "OverrideParams", splitOkSpell.CalculateParams());
            Set(splitOkSpell, "m_Fact", splitOkFact);
            var splitOk = CastRule(splitOkSpell);
            int balanceAfterOk = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            Check(splitOk.Success && splitOk.Context.Params.CasterLevel == baseCL + 2 &&
                  (splitOk.Context.Params.Metamagic & Metamagic.Extend) != 0 && balanceAfterOk == 0,
                "MAGE balance 3 pays Spell Power (+2) + Extend (1) in one atomic transaction");

            // Already-metamagic spell: Extend toggle active on a spell with baked Extend
            // adds 0 to cost, still casts at 0 balance, and lets balance=2 fund Spell Power (+2).
            bonus2.IsOn = false;
            var preExtended = new AbilityData(dummy, book, 2)
                { MetamagicData = new Kingmaker.UnitLogic.Abilities.MetamagicData { SpellLevelCost = 1 } };
            preExtended.MetamagicData.Add(Metamagic.Extend);
            var preExtendedFact = new Ability(dummy, unit.Descriptor) { Spellbook = book };
            Set(preExtendedFact, "OverrideParams", preExtended.CalculateParams());
            Set(preExtended, "m_Fact", preExtendedFact);
            var preExtCastZero = CastRule(preExtended);
            int balanceAfterPreExtZero = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            Check(preExtCastZero.Success && !preExtCastZero.ForceFail &&
                  (preExtCastZero.Context.Params.Metamagic & Metamagic.Extend) != 0 &&
                  balanceAfterPreExtZero == 0,
                "MAGE spell already carrying Extend skips duplicate Metamagic Master fee and casts at zero balance");

            for (int i = 0; i < 2; i++)
                Check(CastRule(Granted("DivinationReplenishFocusAbility")).Success,
                    "MAGE replenishes 2 generic points for already-extended Spell Power combination");
            bonus2.IsOn = true;
            var preExtended2 = new AbilityData(dummy, book, 2)
                { MetamagicData = new Kingmaker.UnitLogic.Abilities.MetamagicData { SpellLevelCost = 1 } };
            preExtended2.MetamagicData.Add(Metamagic.Extend);
            Eq(baseCL + 2, preExtended2.CalculateParams().CasterLevel,
                "MAGE balance 2 projects +2 CL when matching Extend metamagic is already on the spell");
            var preExtendedFact2 = new Ability(dummy, unit.Descriptor) { Spellbook = book };
            Set(preExtendedFact2, "OverrideParams", preExtended2.CalculateParams());
            Set(preExtended2, "m_Fact", preExtendedFact2);
            var preExtCastTwo = CastRule(preExtended2);
            int balanceAfterPreExtTwo = focus.Schools["Divination"].Remaining + focus.Schools["Evocation"].Remaining + focus.Schools["Necromancy"].Remaining;
            Check(preExtCastTwo.Success && preExtCastTwo.Context.Params.CasterLevel == baseCL + 2 &&
                  (preExtCastTwo.Context.Params.Metamagic & Metamagic.Extend) != 0 && balanceAfterPreExtTwo == 0,
                "MAGE balance 2 pays only Spell Power (+2) when Extend is already present on the spell");

            SelectSpellPower(bonus2);
            SelectMetamagic(extendToggle);
            Check(bonus2.IsOn && extendToggle.IsOn &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("SpellPower2Buff")) != null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("MetamagicMasterExtendBuff")) != null,
                "MAGE Spell Power and Metamagic Master arm their buffs before member loss");
            unit.RemoveFact(necromancy);
            Check(unit.Buffs.GetBuff(resonance) == null && !metaToggle.IsAvailable &&
                  !bonus2.IsOn && !extendToggle.IsOn && !bonus2.IsAvailable && !extendToggle.IsAvailable &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("SpellPower2Buff")) == null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("MetamagicMasterExtendBuff")) == null,
                "MAGE losing a member retracts Scholarly Knowledge, dependent options, and active Spell Power / Metamagic Master buffs");
            var learnedMetamagic = unit.Progression.Features.GetFact(empower);
            Check(learnedMetamagic != null && learnedMetamagic.Active && learnedMetamagic.IsEnabled,
                "MAGE already-learned Metamagic Knowledge feat stays active as a normal feat per Kingmaker original behavior");

            unit.Facts.Add(new Feature(Asset<BlueprintFeature>("slot-3-Necromancy-Implement"), unit.Descriptor));
            Rest(unit);
            Check(CastRule(Granted("DivinationInvestFocusAbility")).Success &&
                  CastRule(Granted("EvocationInvestFocusAbility")).Success &&
                  CastRule(Granted("NecromancyInvestFocusAbility")).Success &&
                  CastRule(Granted("LockFocusAbility")).Success,
                "MAGE Day 2 focus lock succeeds after restoring the missing implement");
            SelectSpellPower(bonus2);
            SelectMetamagic(extendToggle);
            Check(bonus2.IsOn && extendToggle.IsOn &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("SpellPower2Buff")) != null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("MetamagicMasterExtendBuff")) != null,
                "MAGE Day 2 toggles arm both buffs");
            Rest(unit);
            Check(!bonus2.IsOn && !extendToggle.IsOn && !bonus2.IsAvailable && !extendToggle.IsAvailable &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("SpellPower2Buff")) == null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("MetamagicMasterExtendBuff")) == null,
                "MAGE resting immediately clears active Spell Power and Metamagic Master buffs and turns off their toggles");
        }
        finally
        {
            player.RealTime = startRealTime;
            applyNativeEffects = false;
        }
        caseStatus["MagesParaphernalia"] = "native_member_choice_scholarship_spell_power_private_scope_exclusive_metamagic_and_dedupe";
        caseStatus["MF07"] = caseStatus["MagesParaphernalia"];
    }

    private static void RegaliaNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("regalia", character, 9, 16);
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("slot-1-Abjuration-Implement"), unit.Descriptor));
        var conjuration = new Feature(Asset<BlueprintFeature>("slot-2-Conjuration-Implement"), unit.Descriptor);
        unit.Facts.Add(conjuration);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 9);
        var leveling = LevelState(book);
        var selection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("slot-3-ImplementSelection"), 2);
        var pick = new SelectFeature(selection, Asset<BlueprintFeature>("slot-3-SaintsHolyRegalia"));
        Check(pick.Check(leveling, unit.Descriptor), "PANOPLY Saint's Regalia requires its two original member schools");
        pick.Apply(leveling, unit.Descriptor);
        Check(unit.Abilities.Enumerable.Any(ability => ability.Blueprint == Asset<BlueprintAbility>("RestoringTouchAbility")) &&
              !unit.Abilities.Enumerable.Any(ability => ability.Blueprint == Asset<BlueprintAbility>("GuardianAuraAbility")),
            "PANOPLY Regalia grants Restoring Touch directly and withholds Guardian Aura for FocusPowerSelection");
        var powerSelection = leveling.AddSelection(null, character.Progression, Asset<BlueprintFeatureSelection>("FocusPowerSelection"), 9);
        var guardianPick = new SelectFeature(powerSelection, Asset<BlueprintFeature>("GuardianAuraPower"));
        Check(guardianPick.Check(leveling, unit.Descriptor), "PANOPLY Regalia offers the Guardian Aura focus power");
        guardianPick.Apply(leveling, unit.Descriptor);
        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var font = Asset<BlueprintBuff>("SaintsHolyRegaliaResonanceBuff");
        var healRule = AccessTools.Method(typeof(RuleHealDamage), nameof(RuleHealDamage.OnTrigger));
        var areaEntry = AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect), "OnActivate");
        var skipScene = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(NoReplenish));
        harmony.Patch(healRule, prefix: new HarmonyMethod(skipScene));
        harmony.Patch(areaEntry, prefix: new HarmonyMethod(skipScene));
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 5; i++) CastRule(Granted("InvestFocusAbility"));
            for (int i = 0; i < 5; i++) CastRule(Granted("ConjurationInvestFocusAbility"));
            CastRule(Granted("LockFocusAbility"));
            Check(unit.Buffs.GetBuff(font)?.Context[AbilityRankType.Default] == 4,
                "PANOPLY Font of Healing caps five points of projected healing at half actual level");

            var cure = Native<BlueprintAbility>("5590652e1c2225c4ca30c4a699ab3649");
            var ordinary = new AbilityData(cure, book, 1);
            Eq(4, HealingBonus(ordinary), "PANOPLY the native healing rule adds Font of Healing to a private-book Cure");
            var delivery = cure.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility;
            if (delivery != null)
                Eq(4, HealingBonus(new AbilityData(delivery, book, 1)), "PANOPLY the audited sticky Cure delivery retains its source's bonus");
            var falseBook = new BlueprintSpellbook { name = "0099N.OtherHealingBook", Spontaneous = true,
                CastingAttribute = StatType.Intelligence, AssetGuid = P(Occultist0099Ids.Id("HostRegaliaForeignBook")),
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            Set(falseBook, "m_SpellList", Ref<BlueprintSpellListReference>(character.Spellbook.SpellList));
            Set(falseBook, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(character.Spellbook.SpellsPerDay));
            falseBook.OnEnable();
            Eq(0, HealingBonus(new AbilityData(cure, unit.Descriptor.DemandSpellbook(falseBook), 1)),
                "PANOPLY a foreign spellbook sharing the same Cure root gains no Font healing");

            var restore = Granted("RestoringTouchAbility");
            var drain = new AbilityData(restore, Asset<BlueprintAbility>("RestoringTouchDrainAbility"));
            int spent = focus.Schools["Abjuration"].Remaining + focus.Schools["Conjuration"].Remaining;
            Check(!drain.IsVisible() && CastRule(drain).ForceFail &&
                  spent == focus.Schools["Abjuration"].Remaining + focus.Schools["Conjuration"].Remaining,
                "PANOPLY Restoring Touch drain variant cannot spend at level 9");
            unit.Progression.GetClassData(character).Level = 10;
            Check(drain.IsVisible() && drain.ResourceLogic.CalculateCost(drain) == 3 &&
                  drain.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealStatDamage>().Single().HealDrain,
                "PANOPLY level 10 Restoring Touch uses native ability-drain restoration for three points");
            unit.Progression.GetClassData(character).Level = 9;

            var guardian = Granted("GuardianAuraAbility");
            var aura = Asset<BlueprintBuff>("GuardianAuraBuff");
            Check(guardian.Blueprint.IsFullRoundAction && CastRule(guardian).Success &&
                  unit.Buffs.GetBuff(aura)?.TimeLeft == TimeSpan.FromMinutes(1) &&
                  unit.State.HasCondition(UnitCondition.Slowed) &&
                  Asset<BlueprintAbilityAreaEffect>("GuardianAuraArea").Size == new Feet(20),
                "PANOPLY Guardian Aura uses the native full-round slot, movement penalty and one-minute ally area");
            unit.RemoveFact(conjuration);
            Check(!unit.State.HasCondition(UnitCondition.Slowed) && unit.Buffs.GetBuff(aura) == null && HealingBonus(ordinary) == 0,
                "PANOPLY removing Conjuration retracts the Regalia spell rider and its ongoing aura");
        }
        finally
        {
            applyNativeEffects = false;
            harmony.Unpatch(healRule, skipScene);
            harmony.Unpatch(areaEntry, skipScene);
        }
        caseStatus["SaintsHolyRegalia"] = "native_regalia_heal_rule_book_scope_drain_gate_aura_identity";

        int HealingBonus(AbilityData ability)
        {
            var context = new AbilityExecutionContext(ability, AbilityParams.Empty, new TargetWrapper(unit));
            var rule = Rulebook.Trigger(new RuleHealDamage(unit, unit, 1) { Reason = context });
            return (int)rule.AdditionalBonus;
        }
    }

    private static void EvocationNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("evocation-caster", character, 9, 16);
        var body = Empty<UnitBody>();
        Set(body, "Owner", unit.Descriptor);
        Set(body, "m_HandsEquipmentSets", new[] { new HandsEquipmentSet(unit.Descriptor) });
        Set(unit.Descriptor, "Body", body);
        Set(unit, "IsTurnedOn", true);
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        var attackStat = new ModifiableValue(unit.Stats, StatType.AdditionalAttackBonus);
        Set(unit.Stats, "AdditionalAttackBonus", attackStat);
        var acStat = new ModifiableValueArmorClass(unit.Stats);
        Set(unit.Stats, "AC", acStat);
        unit.Stats.AllStats.AddRange(new ModifiableValue[] { attackStat, acStat });
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Evocation-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 9);
        var leveling = LevelState(book);
        var powers = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        foreach (var option in new[] { (Token: "EnergyBlast", Level: 5), (Token: "Radiance", Level: 1),
            (Token: "LightMatrix", Level: 5), (Token: "WallOfPower", Level: 9) })
        {
            var selection = leveling.AddSelection(null, character.Progression, powers, option.Level);
            var pick = new SelectFeature(selection, Asset<BlueprintFeature>(option.Token + "Power"));
            Check(pick.Check(leveling, unit.Descriptor), "EVOC native selection admits " + option.Token);
            pick.Apply(leveling, unit.Descriptor);
        }
        var list = character.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
            .Single(slot => slot.SlotKey == "slot-1" && slot.School == "Evocation").List.Get();
        var rows = ((Occultist0099SpellRoot[])AccessTools.Field(Production("Occultist0099SpellCatalog"), "Roots").GetValue(null))
            .Where(row => row.School == "Evocation" && !row.IsKnack && row.Source != "External").ToArray();
        Eq(23, rows.Length, "EVOC the signed Evocation table has 23 non-knack native or existing CotW candidates");
        Check(rows.All(row => list.GetSpells(row.Circle).Any(spell => spell.AssetGuid == P(row.Guid))) &&
              list.SpellsByLevel.Sum(row => row.Spells.Count()) == 23,
            "EVOC every learning candidate keeps its signed native root and Occultist circle");

        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var pool = focus.Schools["Evocation"];
        var intenseBuff = Asset<BlueprintBuff>("IntenseFocusBuff");
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 12; i++) CastRule(Granted("EvocationInvestFocusAbility"));
            Check(unit.Buffs.GetBuff(intenseBuff) == null, "EVOC Intense Focus stays absent before the lock");
            Check(CastRule(Granted("LockFocusAbility")).Success && unit.Buffs.GetBuff(intenseBuff) != null,
                "EVOC locking the school establishes the Intense Focus resonance buff");
            Eq(5, Occultist0099Focus.Resonance(focus, "Evocation"),
                "EVOC twelve locked points give +5 damage at one per two points, capped at 1 + half level");

            var ray = Granted("EnergyRayAbility");
            var rayLeaves = ray.GetConversions().ToArray();
            Eq(4, rayLeaves.Length, "EVOC Energy Ray offers exactly the four signed energy types");
            Check(rayLeaves.All(leaf => leaf.Blueprint.GetComponent<AbilityDeliverProjectile>() != null &&
                  leaf.Blueprint.GetComponent<Occultist0099FocusResourceLogic>().Amount == 1),
                "EVOC each ray is a ranged touch attack costing one point");
            var rayRank = rayLeaves[0].Blueprint.GetComponents<ContextRankConfig>().Single();
            Eq(10, (int)Get(rayRank, "m_Max"), "EVOC Energy Ray caps at ten d6");
            Eq(1, (int)Get(rayRank, "m_StartLevel"), "EVOC Energy Ray dice start at 1st level");
            Eq(2, (int)Get(rayRank, "m_StepLevel"), "EVOC Energy Ray gains a die every two levels");
            var rayDamage = rayLeaves[0].Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions
                .OfType<ContextActionDealDamage>().Single();
            Eq(DiceType.D6, rayDamage.Value.DiceType, "EVOC Energy Ray uses d6");

            var blast = Granted("EnergyBlastAbility");
            var blastLeaves = blast.GetConversions().ToArray();
            Eq(4, blastLeaves.Length, "EVOC Energy Blast offers the same four energy types");
            Check(blastLeaves.All(leaf => leaf.Blueprint.GetComponent<Occultist0099FocusResourceLogic>().Amount == 2),
                "EVOC Energy Blast costs two points");
            Eq(20, blastLeaves[0].Blueprint.GetComponent<AbilityTargetsAround>().AoERadius.Value,
                "EVOC Energy Blast is a twenty-foot burst");
            var blastSave = blastLeaves[0].Blueprint.GetComponent<AbilityEffectRunAction>();
            Eq(SavingThrowType.Reflex, blastSave.SavingThrowType, "EVOC Energy Blast allows a Reflex save");
            var blastDamage = blastSave.Actions.Actions.OfType<ContextActionDealDamage>().Single();
            Eq(DiceType.D4, blastDamage.Value.DiceType, "EVOC Energy Blast uses d4 per level");
            Check(blastDamage.IsAoE && blastDamage.HalfIfSaved, "EVOC Energy Blast halves on a successful save");

            var wall = Granted("WallOfPowerAbility");
            Eq(4, wall.GetConversions().Count(), "EVOC Wall of Power offers the four elemental walls");
            var wallArea = Asset<BlueprintAbilityAreaEffect>("WallOfPowerArea-Fire");
            Eq(AreaEffectShape.Wall, wallArea.Shape, "EVOC the wall reuses the native wall shape");
            var wallRun = wallArea.GetComponent<AbilityAreaEffectRunAction>();
            Check(wallRun.Round.Actions.OfType<ContextActionDealDamage>().Any() &&
                  wallRun.UnitEnter.Actions.OfType<ContextActionDealDamage>().Any(),
                "EVOC the wall damages on entering and each round through the native area action");
            Eq(2, wallRun.Round.Actions.OfType<ContextActionDealDamage>().Single().Value.DiceCountValue.Value,
                "EVOC the wall deals two d6 plus the occultist level");
            var wallSpawn = wall.GetConversions().Last().Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions
                .OfType<ContextActionSpawnAreaEffect>().Single();
            Eq(DurationRate.Rounds, wallSpawn.DurationValue.Rate, "EVOC the wall lasts rounds per occultist level");

            var matrix = Granted("LightMatrixAbility");
            var releaseBlueprint = Asset<BlueprintAbility>("LightMatrixReleaseAbility");
            Eq(UnitCommand.CommandType.Move, releaseBlueprint.ActionType, "EVOC the orb release is a move action");
            Check(releaseBlueprint.GetComponent<Occultist0099FocusResourceLogic>() == null,
                "EVOC releasing the orb never charges focus again");
            Check(Asset<BlueprintBuff>("LightMatrixBuff").GetComponent<AddFacts>() != null,
                "EVOC the orb buff grants the release ability");
            int matrixBefore = pool.Remaining;
            Check(CastRule(matrix).Success && pool.Remaining == matrixBefore - 1 &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("LightMatrixBuff")) != null,
                "EVOC creating the orb pays one point and attaches its buff");

            var glow = Asset<BlueprintWeaponEnchantment>("RadianceGlowEnchantment");
            var donor = Native<BlueprintWeaponEnchantment>("6cbb732b9d638724a960d784634dcdcf");
            var dazzlingId = (BlueprintGuid)AccessTools.Method(Production("PsychicAssets"), "Id")
                .Invoke(null, new object[] { "DazzlingBladeGlowEnchantment" });
            Check(glow.AssetGuid != donor.AssetGuid && glow.AssetGuid != dazzlingId,
                "EVOC Radiance owns a distinct enchantment identity, so Dazzling Blade expiring cannot extinguish it");
            Check(glow.ComponentsArray.Length == 0, "EVOC the glow is purely visual, carrying no mechanical components");
            var radiance = Granted("RadianceAbility");
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
            }
            var service = AccessTools.PropertyGetter(typeof(EntityService), nameof(EntityService.Instance));
            var serviceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ArmorEntityServiceBoundary));
            var dollRoom = AccessTools.PropertyGetter(typeof(ItemEnchantment), "WielderVisibleInInventory");
            var skipDollRoom = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ArmorDollRoomBoundary));
            armorFixtureEntities = new EntityService();
            harmony.Patch(service, prefix: new HarmonyMethod(serviceBoundary));
            harmony.Patch(dollRoom, prefix: new HarmonyMethod(skipDollRoom));
            try
            {
                var club = Native<BlueprintItemWeapon>("07863393521453a4da9e98626531eb5f").CreateEntity<ItemEntityWeapon>();
                var sword = Native<BlueprintItemWeapon>("6fd0a849531617844b195f452661b2cd").CreateEntity<ItemEntityWeapon>();
                Equip(club);
                Check(CastRule(radiance).Success && unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")) != null,
                    "EVOC Radiance attaches its glow buff");
                Check(club.Enchantments.Any(enchantment => enchantment.Blueprint == glow),
                    "EVOC the glow enchantment is applied to the equipped main-hand weapon");
                Equip(sword);
                // The native event bus dispatches inside the component's own fact context.
                foreach (var runtime in unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")).Components
                    .Where(component => component.SourceBlueprintComponent is IUnitEquipmentHandler))
                    Occultist0099NativeScope.InRuntimeContext(runtime, component =>
                        ((IUnitEquipmentHandler)component).HandleEquipmentSlotUpdated(body.PrimaryHand, club));
                Check(sword.Enchantments.Any(enchantment => enchantment.Blueprint == glow) &&
                      !club.Enchantments.Any(enchantment => enchantment.Blueprint == glow),
                    "EVOC the glow follows the current main-hand weapon instead of locking the original");

                var radianceComponent = (Occultist0099RadianceBuff)Asset<BlueprintBuff>("RadianceBuff").GetComponent<Occultist0099RadianceBuff>();
                var radianceRuntime = unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")).Components
                    .First(component => component.SourceBlueprintComponent is Occultist0099RadianceBuff);
                var foe = Unit("evocation-radiance-foe", character, 5, 10);
                // The native Faerie Fire components adjust Stealth, so the fixture needs it.
                var foeStealth = new ModifiableValueSkill(foe.Stats, foe.Stats.Dexterity, StatType.SkillStealth);
                Set(foe.Stats, "SkillStealth", foeStealth);
                foe.Stats.AllStats.Add(foeStealth);
                // The outline duration rolls 1d4 through UnityEngine.Random.
                var outlineDice = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(int), typeof(DiceType) });
                var outlineDiceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantDiceBoundary));
                harmony.Patch(outlineDice, prefix: new HarmonyMethod(outlineDiceBoundary));
                void Crit(ItemEntityWeapon weapon)
                {
                    var roll = Empty<RuleAttackWithWeapon>();
                    Set(roll, "Weapon", weapon);
                    Set(roll, "Target", foe);
                    var attackRoll = Empty<RuleAttackRoll>();
                    Set(attackRoll, "IsCriticalConfirmed", true);
                    Set(roll, "AttackRoll", attackRoll);
                    Occultist0099NativeScope.InRuntimeContext(radianceRuntime, _ => radianceComponent.OnEventDidTrigger(roll));
                }
                Crit(club);
                Check(unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")) != null &&
                      foe.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceEffectBuff")) == null,
                    "EVOC a critical with a weapon that is no longer glowing does not discharge Radiance");
                Crit(sword);
                Check(foe.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceEffectBuff")) != null &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")) == null,
                    "EVOC only the glowing weapon's confirmed critical outlines the foe and ends the glow");
                Eq(2, Asset<BlueprintBuff>("RadianceEffectBuff").GetComponent<AttackBonusAgainstTarget>().Value.Value,
                    "EVOC the outline grants a +2 bonus on attacks against the foe");
                harmony.Unpatch(outlineDice, outlineDiceBoundary);

                Check(CastRule(radiance).Success &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")) != null &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("LightMatrixBuff")) != null &&
                      sword.Enchantments.Any(enchantment => enchantment.Blueprint == glow) &&
                      unit.Abilities.Enumerable.Any(ability => ability.Blueprint == releaseBlueprint),
                    "EVOC Radiance and Light Matrix are active before rest");
                Rest(unit);
                Check(unit.Buffs.GetBuff(intenseBuff) == null &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")) == null &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("LightMatrixBuff")) == null &&
                      !sword.Enchantments.Any(enchantment => enchantment.Blueprint == glow) &&
                      !unit.Abilities.Enumerable.Any(ability => ability.Blueprint == releaseBlueprint),
                    "EVOC resting clears active Radiance, Light Matrix, its release ability, and Intense Focus");
                for (int i = 0; i < 2; i++) CastRule(Granted("EvocationInvestFocusAbility"));
                Check(CastRule(Granted("LockFocusAbility")).Success &&
                      CastRule(matrix).Success && CastRule(radiance).Success &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("LightMatrixBuff")) != null &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")) != null &&
                      unit.Abilities.Enumerable.Any(ability => ability.Blueprint == releaseBlueprint),
                    "EVOC re-locking allows casting Light Matrix and Radiance again");
                unit.RemoveFact(implement);
                Check(unit.Buffs.GetBuff(intenseBuff) == null &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("RadianceBuff")) == null &&
                      unit.Buffs.GetBuff(Asset<BlueprintBuff>("LightMatrixBuff")) == null &&
                      !sword.Enchantments.Any(enchantment => enchantment.Blueprint == glow) &&
                      !unit.Abilities.Enumerable.Any(ability => ability.Blueprint == releaseBlueprint),
                    "EVOC removing the Evocation implement clears its resonance and active orb/glow buffs");
            }
            finally
            {
                harmony.Unpatch(dollRoom, skipDollRoom);
                harmony.Unpatch(service, serviceBoundary);
            }
        }
        finally { applyNativeEffects = false; }
        caseStatus["Evocation"] = "native_private_learning_intense_focus_rays_blasts_walls_orb_radiance_and_cleanup";
    }

    private static void NecromancyNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
        var unit = Unit("necromancy-caster", character, 12, 16);
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        // The 13 native familiar bond features grant these stats; the fixture must
        // carry every one so granting and revoking a puppet uses the real components.
        var familiarAc = new ModifiableValueArmorClass(unit.Stats);
        Set(unit.Stats, "AC", familiarAc);
        unit.Stats.AllStats.Add(familiarAc);
        var initiative = new ModifiableValue(unit.Stats, StatType.Initiative);
        Set(unit.Stats, "Initiative", initiative);
        unit.Stats.AllStats.Add(initiative);
        foreach (var stat in new[] { StatType.SkillAthletics, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature,
            StatType.SkillMobility, StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillStealth })
        {
            var skill = new ModifiableValueSkill(unit.Stats, unit.Stats.Dexterity, stat);
            Set(unit.Stats, stat.ToString(), skill);
            unit.Stats.AllStats.Add(skill);
        }
        Set(unit.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("MentalFocusFact"), unit.Descriptor));
        unit.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), unit.Descriptor));
        var implement = new Feature(Asset<BlueprintFeature>("slot-1-Necromancy-Implement"), unit.Descriptor);
        unit.Facts.Add(implement);
        var book = unit.Descriptor.DemandSpellbook(character.Spellbook);
        Set(book, "m_BaseLevelInternal", 12);
        var leveling = LevelState(book);
        var powers = Asset<BlueprintFeatureSelection>("FocusPowerSelection");
        foreach (var option in new[] { (Token: "FleshRot", Level: 3), (Token: "PainWave", Level: 7),
            (Token: "SoulboundPuppet", Level: 1), (Token: "SpiritShroud", Level: 3), (Token: "NecromanticServant", Level: 3) })
        {
            var selection = leveling.AddSelection(null, character.Progression, powers, option.Level);
            var pick = new SelectFeature(selection, Asset<BlueprintFeature>(option.Token + "Power"));
            Check(pick.Check(leveling, unit.Descriptor), "NEC native selection admits " + option.Token);
            pick.Apply(leveling, unit.Descriptor);
        }
        var register = AccessTools.Method(Production("Occultist0099NativeRegistration"), "RegisterInDetachedCache");
        var list = character.Spellbook.GetComponent<Occultist0099SpellbookMarker0099>().ImplementLists
            .Single(slot => slot.SlotKey == "slot-1" && slot.School == "Necromancy").List.Get();
        const string animateLesserId = "57fcf8016cf04da4a8b33d2add14de7e";
        var rows = ((Occultist0099SpellRoot[])AccessTools.Field(Production("Occultist0099SpellCatalog"), "Roots").GetValue(null))
            .Where(row => row.School == "Necromancy" && !row.IsKnack && row.Source != "External").ToArray();
        bool Listed(Occultist0099SpellRoot row) => list.GetSpells(row.Circle).Any(spell => spell.AssetGuid == P(row.Guid));
        Eq(21, rows.Length, "NEC the signed Necromancy table has 21 non-knack native or existing CotW candidates");
        Check(rows.Where(row => row.Guid != animateLesserId).All(Listed) &&
            list.SpellsByLevel.Sum(row => row.Spells.Count()) == 20,
            "NEC unowned DLC excludes only Lesser Animate Dead from the private learning list");
        var reward = Native<BlueprintDlcReward>("1c893fd3059d4b09bd37d089aff7866e");
        Set(reward, "m_IsAvailable", (bool?)true);
        Check((bool)register.Invoke(null, new object[] { detached }) && rows.All(Listed),
        "NEC native DLC5 ownership restores Lesser Animate Dead at its signed second circle");
        Set(reward, "m_IsAvailable", (bool?)false);
        Check((bool)register.Invoke(null, new object[] { detached }) && !Listed(rows.Single(row => row.Guid == animateLesserId)),
        "NEC removing DLC5 ownership withdraws only the gated candidate");
        var causeFear = Native<BlueprintAbility>("bd81a3931aa285a4f9844585b5d97e51");
        var learn = new SelectSpell(character.Spellbook, list, 1, causeFear, 0);
        Check(NativeCheck(learn, leveling, unit.Descriptor), "NEC the native implement source offers Cause Fear");
        learn.Apply(leveling, unit.Descriptor);
        Check(book.IsKnown(causeFear), "NEC the reused root enters the private Occultist book");

        AbilityData Granted(string token) => unit.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        var focus = unit.Get<Occultist0099FocusPart>().State;
        var pool = focus.Schools["Necromancy"];
        var classData = unit.Progression.GetClassData(character);
        var focusBuff = Asset<BlueprintBuff>("NecromanticFocusBuff");
        servantPool = Asset<BlueprintSummonPool>("NecromanticServantPool");
        servantCaster = unit;
        servantPools = new Kingmaker.AreaLogic.SummonPool.SummonPoolsManager();
        var poolsGetter = AccessTools.PropertyGetter(typeof(Kingmaker.Game), "SummonPools");
        var poolsBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantPoolsBoundary));
        var spawnMethod = AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster), "RunAction");
        var spawnBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantSpawnBoundary));
        harmony.Patch(poolsGetter, prefix: new HarmonyMethod(poolsBoundary));
        harmony.Patch(spawnMethod, prefix: new HarmonyMethod(spawnBoundary));
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 12; i++) CastRule(Granted("NecromancyInvestFocusAbility"));
            Check(unit.Buffs.GetBuff(focusBuff) == null, "NEC Necromantic Focus stays absent before the lock");
            Check(CastRule(Granted("LockFocusAbility")).Success && unit.Buffs.GetBuff(focusBuff) != null,
                "NEC locking the school establishes the Necromantic Focus resonance buff");
            Eq(3, Occultist0099Focus.Resonance(focus, "Necromancy"),
                "NEC twelve locked points give a +3 undead DC bonus at one per four points");
            // Wrath has no undead-HD control pool: native Animate Dead is a timed
            // spawn and no native component tracks controlled undead HD. This is the
            // authoritative model allowance only, with no runtime consumer yet.
            Eq(24, (int)AccessTools.Method(Production("Occultist0099Focus"), "NecromanticControlBonus").Invoke(null, new object[] { focus }),
                "NEC the model records two controlled undead Hit Dice per locked point (no native HD pool exists to consume it)");

            // The resonance is a save-DC bonus on this class's own spellbook casts only.
            var undeadTarget = Unit("necro-undead", character, 5, 10);
            undeadTarget.Facts.Add(new Feature(Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33"), undeadTarget.Descriptor));
            var livingTarget = Unit("necro-living", character, 5, 10);
            var handler = (Occultist0099NecromanticFocus)focusBuff.GetComponent<Occultist0099NecromanticFocus>();
            int SaveBonus(UnitEntityData target, AbilityData source)
            {
                var save = new RuleSavingThrow(target, SavingThrowType.Will, 15);
                if (source != null) save.Reason = new RuleReason(source.CreateExecutionContext(new TargetWrapper(target)));
                int before = save.DifficultyClass;
                // The native rulebook dispatches a buff component inside its own fact
                // context, so the resonance is exercised the same way here.
                var runtime = unit.Buffs.GetBuff(focusBuff).Components
                    .First(component => component.SourceBlueprintComponent is Occultist0099NecromanticFocus);
                Occultist0099NativeScope.InRuntimeContext(runtime, _ => handler.OnEventAboutToTrigger(save));
                return save.DifficultyClass - before;
            }
            var spell = new AbilityData(causeFear, book, 1);
            Eq(3, SaveBonus(undeadTarget, spell), "NEC an own-book spell against an undead target gains the full resonance DC");
            Eq(0, SaveBonus(livingTarget, spell), "NEC a living target is unaffected by Necromantic Focus");
            var mindFear = Granted("MindFearAbility");
            Eq(0, SaveBonus(undeadTarget, mindFear), "NEC a focus power is not a spellbook cast and gains no resonance DC");
            var foreignBook = new BlueprintSpellbook { AssetGuid = P(Occultist0099Ids.Id("HostNecromancyForeignBook")),
                name = "0099N.NecromancyForeignBook", Spontaneous = true, IsArcane = false, CastingAttribute = StatType.Intelligence,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            Set(foreignBook, "m_SpellList", Ref<BlueprintSpellListReference>(book.Blueprint.SpellList));
            Set(foreignBook, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(book.Blueprint.SpellsPerDay));
            foreignBook.OnEnable();
            Eq(0, SaveBonus(undeadTarget, new AbilityData(causeFear, unit.Descriptor.DemandSpellbook(foreignBook), 1)),
                "NEC a multiclass spellbook sharing the same list gains no Occultist resonance DC");
            var itemSpell = new AbilityData(causeFear, book, 1);
            var service = AccessTools.PropertyGetter(typeof(EntityService), nameof(EntityService.Instance));
            var serviceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ArmorEntityServiceBoundary));
            armorFixtureEntities = new EntityService();
            harmony.Patch(service, prefix: new HarmonyMethod(serviceBoundary));
            ItemEntityUsable scroll;
            try { scroll = Native<BlueprintItemEquipmentUsable>("fe244c39bdd5cb64eae65af23c6759de").CreateEntity<ItemEntityUsable>(); }
            finally { harmony.Unpatch(service, serviceBoundary); }
            var itemFact = new Ability(causeFear, unit.Descriptor);
            itemFact.SetSourceItem(scroll);
            Set(itemSpell, "m_Fact", itemFact);
            Check(itemSpell.SourceItem != null, "NEC the item fixture really carries a source item");
            Eq(0, SaveBonus(undeadTarget, itemSpell), "NEC a scroll cast of the same root gains no Occultist resonance DC");
            Eq(0, SaveBonus(undeadTarget, null), "NEC a save with no ability source gains no resonance DC");

            // Soulbound Puppet reuses the native familiar bond features.
            var puppetRoot = Granted("SoulboundPuppetAbility");
            var puppets = puppetRoot.GetConversions().ToArray();
            Eq(13, puppets.Length, "NEC Soulbound Puppet offers exactly the 13 native familiar choices");
            var catFeature = Native<BlueprintFeature>("1cb0b559ca2e31e4d9dc65de012fa82f");
            var dogFeature = Native<BlueprintFeature>("f111037444d5b6243bbbeb7fc9056ed3");
            var cat = puppets.Single(choice => choice.Blueprint.name.EndsWith("SoulboundPuppet-" + catFeature.AssetGuid));
            var dog = puppets.Single(choice => choice.Blueprint.name.EndsWith("SoulboundPuppet-" + dogFeature.AssetGuid));
            Check(puppets.All(choice => (bool)Get(choice.Blueprint, "m_IsFullRoundAction") && choice.Blueprint.Range == AbilityRange.Personal &&
                choice.Blueprint.GetComponent<Occultist0099FocusResourceLogic>().Amount == 1),
                "NEC every puppet choice is a full-round personal power costing one Necromancy point");
            int remaining = pool.Remaining;
            Check(CastRule(cat).Success && unit.Descriptor.HasFact(catFeature) && pool.Remaining == remaining - 1,
                "NEC the puppet grants the native familiar bond feature and pays one point");
            var catBuff = unit.Buffs.Enumerable.Single(buff => buff.Blueprint.name.EndsWith("SoulboundPuppetBuff-" + catFeature.AssetGuid));
            Eq(TimeSpan.FromMinutes(120), catBuff.TimeLeft, "NEC the puppet lasts ten minutes per occultist level");
            Check(CastRule(dog).Success && unit.Descriptor.HasFact(dogFeature) && !unit.Descriptor.HasFact(catFeature),
                "NEC a second puppet replaces the first instead of being blocked by its own granted familiar");
            Check(unit.Buffs.Enumerable.Count(buff => buff.Blueprint.name.Contains("SoulboundPuppetBuff-")) == 1,
                "NEC only one soulbound puppet buff remains active");
            unit.RemoveFact(unit.Buffs.Enumerable.Single(buff => buff.Blueprint.name.Contains("SoulboundPuppetBuff-")));
            var ownFamiliar = new Feature(catFeature, unit.Descriptor);
            unit.Facts.Add(ownFamiliar);
            Check(!dog.IsAvailableForCast && Rulebook.Trigger(new RuleCastSpell(dog, new TargetWrapper(unit))).ForceFail,
                "NEC a familiar of the character's own blocks the puppet power without spending focus");
            unit.RemoveFact(ownFamiliar);
            Check(dog.IsAvailableForCast, "NEC removing the character's own familiar restores the puppet power");

            // Necromantic Servant: real corpses, a private pool, and the level branches.
            var servantRoot = Granted("NecromanticServantAbility");
            var choices = servantRoot.GetConversions().ToArray();
            Eq(2, choices.Length, "NEC Necromantic Servant has a single-corpse and an area branch");
            var single = choices.Single(choice => choice.Blueprint == Asset<BlueprintAbility>("NecromanticServantSingleAbility"));
            var area = choices.Single(choice => choice.Blueprint == Asset<BlueprintAbility>("NecromanticServantAreaAbility"));
            Check(single.IsVisible() && !area.IsVisible(),
                "NEC below 13th level only the single-corpse animation is offered");
            var corpse = Unit("necro-corpse", character, 4, 10);
            Set(corpse.State, "LifeState", UnitLifeState.Dead);
            var consumed = Asset<BlueprintFeature>("NecromanticServantConsumed");
            var raise = Asset<BlueprintAbility>("NecromanticServantSingleAbility").GetComponent<AbilityEffectRunAction>()
                .Actions.Actions.OfType<Occultist0099AnimateServant>().Single();
            var animatable = AccessTools.Method(Production("Occultist0099AnimateServant"), "Animatable");
            bool CanAnimate(UnitEntityData target) => (bool)animatable.Invoke(null, new object[] { unit, target, consumed, raise.ExcludedTypes });
            Check(CanAnimate(corpse),
                "NEC a dead non-faction corpse of an eligible type is animatable");
            var living = Unit("necro-living-corpse", character, 4, 10);
            Check(!CanAnimate(living), "NEC a living creature is not a valid corpse");
            foreach (string type in new[] { "734a29b693e9ec346ba2951b27987e33", "fd389783027d63343b4a5634bd81645f",
                "198fd8924dabcb5478d0f78bd453c586", "3bec99efd9a363242a6c8d9957b75e91",
                "09478937695300944a179530664e42ec", "706e61781d692a042b35941f14bc41c5" })
            {
                var typed = Unit("necro-typed-" + type, character, 4, 10);
                Set(typed.State, "LifeState", UnitLifeState.Dead);
                var fact = new Feature(Native<BlueprintFeature>(type), typed.Descriptor);
                typed.Facts.Add(fact);
                Check(!CanAnimate(typed), "NEC creature type " + type + " cannot be raised as a skeleton");
                typed.RemoveFact(fact);
                Check(CanAnimate(typed), "NEC the same corpse becomes eligible once the excluding type is gone");
            }
            int before = pool.Remaining;
            servantSpawnCalls = 0;
            servantSpawnLevels.Clear();
            RuleCastSpell Raise(UnitEntityData target) => Rulebook.Trigger(new RuleCastSpell(single, new TargetWrapper(target)));
            Check(Raise(corpse).Success && servantSpawnCalls == 1 && pool.Remaining == before - 1 &&
                corpse.Descriptor.HasFact(consumed),
                "NEC raising a corpse pays one point, spawns one native servant and retires that corpse");
            Eq(12, servantSpawnLevels.Single(), "NEC the native summon request carries the occultist level as servant Hit Dice");
            Check(!CanAnimate(corpse), "NEC a consumed corpse is no longer animatable");
            // Half-level cap: level 12 allows six servants, one already exists.
            for (int i = 0; i < 5; i++)
            {
                var next = Unit("necro-cap-" + i, character, 4, 10);
                Set(next.State, "LifeState", UnitLifeState.Dead);
                Check(Raise(next).Success && next.Descriptor.HasFact(consumed),
                    "NEC servant " + (i + 2) + " of six is raised");
            }
            var overCap = Unit("necro-over-cap", character, 4, 10);
            Set(overCap.State, "LifeState", UnitLifeState.Dead);
            int capCalls = servantSpawnCalls;
            int capFocus = pool.Remaining;
            Raise(overCap);
            Check(servantSpawnCalls == capCalls && !overCap.Descriptor.HasFact(consumed),
                "NEC the half-level servant cap refuses a seventh servant and leaves the corpse usable");
            // Disclosed upstream behaviour: the cap is checked inside the action, so an
            // over-cap attempt still spends its point, exactly like a missed touch.
            Eq(capFocus - 1, pool.Remaining,
                "NEC an over-cap attempt still debits one point, matching the original action-time cap check");
            Eq(6, servantPools.GetPool(servantPool).Count, "NEC the dedicated pool holds exactly the six servants");

            // 13th level switches to the area branch; one cast pays once for many corpses.
            classData.Level = 13;
            Check(!single.IsVisible() && area.IsVisible(),
                "NEC at 13th level the area animation replaces the single-corpse action");
            var burst = area.Blueprint.GetComponent<AbilityTargetsAround>();
            Eq(10, burst.AoERadius.Value, "NEC the area animation is a ten-foot burst");
            Check((bool)Get(burst, "m_IncludeDead"), "NEC the burst selects dead units");
            var anchors = new List<UnitEntityData>();
            for (int i = 0; i < 3; i++)
            {
                var body = Unit("necro-burst-" + i, character, 4, 10);
                Set(body.State, "LifeState", UnitLifeState.Dead);
                anchors.Add(body);
            }
            // Free the pool so the burst can raise all three.
            foreach (var existing in servantPools.GetPool(servantPool).Units.ToArray())
                servantPools.GetPool(servantPool).Unregister(existing);
            var areaContext = area.CreateExecutionContext(new TargetWrapper(anchors[0]));
            // The headless host has no Unity scene. Only the geometry probe is
            // stubbed; the native burst still applies its own dead/condition filters.
            var scene = new EntityPool<UnitEntityData>();
            scene.All.Add(unit);
            scene.All.AddRange(anchors);
            // The native pool only enumerates units the engine considers in-game.
            foreach (var member in scene.All) Set(member, "m_IsInGame", true);
            var previousUnits = Get(Kingmaker.Game.Instance.State, "Units");
            Set(Kingmaker.Game.Instance.State, "Units", scene);
            var inRange = AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), "IsUnitInRange",
            new[] { typeof(UnitEntityData), typeof(UnityEngine.Vector3), typeof(float), typeof(bool) });
            var rangeBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantRangeBoundary));
            harmony.Patch(inRange, prefix: new HarmonyMethod(rangeBoundary));
            TargetWrapper[] selected;
            try { selected = burst.Select(areaContext, new TargetWrapper(anchors[0])).ToArray(); }
            finally
            {
                harmony.Unpatch(inRange, rangeBoundary);
                Set(Kingmaker.Game.Instance.State, "Units", previousUnits);
            }
            Eq(3, selected.Length, "NEC the native burst selection returns exactly the three nearby corpses");
            int areaBefore = pool.Remaining;
            servantSpawnCalls = 0;
            var areaCast = Rulebook.Trigger(new RuleCastSpell(area, new TargetWrapper(anchors[0])));
            Check(areaCast.Success && pool.Remaining == areaBefore - 1,
                "NEC one area cast debits exactly one Necromancy point");
            var effect = area.Blueprint.GetComponents<AbilityApplyEffect>().Single();
            foreach (var target in selected) effect.Apply(areaCast.Context, target);
            Check(pool.Remaining == areaBefore - 1 && servantSpawnCalls == selected.Length,
                "NEC animating every selected corpse in the burst never charges focus a second time");
            Check(anchors.All(body => body.Descriptor.HasFact(consumed)),
                "NEC each corpse raised by the burst is retired individually");

            // 9th level bloody template and 17th level teamwork sharing.
            var bloody = Asset<BlueprintBuff>("NecromanticServantBloodyBuff");
            var teamwork = Native<BlueprintBuff>("a603a90d24a636c41910b3868f434447");
            var damage = Asset<BlueprintBuff>("NecromanticServantDamageBuff");
            var spawn = raise.Spawn;
            Check(spawn.AfterSpawn.Actions.OfType<ContextActionApplyBuff>().Any(action => action.Buff == damage) &&
                spawn.AfterSpawn.Actions.OfType<Conditional>().Any(branch => branch.IfTrue.Actions.OfType<ContextActionApplyBuff>().Any(action => action.Buff == bloody)) &&
                spawn.AfterSpawn.Actions.OfType<Conditional>().Any(branch => branch.IfTrue.Actions.OfType<ContextActionApplyBuff>().Any(action => action.Buff == teamwork)),
                "NEC the native spawn carries the damage bonus, bloody template and native teamwork share");
            // The native summon's own AfterSpawn must survive: a tier marker buff and
            // the Cruoromancer / Reanimator conditionals, then the two servant branches.
            var nativeAfter = Native<BlueprintAbility>("57fcf8016cf04da4a8b33d2add14de7e")
                .GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnMonster>().Single().AfterSpawn.Actions;
            Check(spawn.AfterSpawn.Actions.Take(nativeAfter.Length).Select(action => action.GetType())
                    .SequenceEqual(nativeAfter.Select(action => action.GetType())) &&
                  spawn.AfterSpawn.Actions.OfType<ContextActionApplyBuff>().Any(action =>
                      action.Buff == Native<BlueprintBuff>("6fcdf014694b2b542a867763b4369cb3")),
                "NEC the native summon AfterSpawn is preserved ahead of the servant effects");
            var branches = spawn.AfterSpawn.Actions.OfType<Conditional>().Skip(nativeAfter.OfType<Conditional>().Count()).ToArray();
            Eq(2, branches.Length, "NEC the servant adds exactly the bloody and teamwork level branches");
            int Threshold(Conditional branch)
            {
                var compare = (ContextConditionCompare)branch.ConditionsChecker.Conditions.Single();
                Eq(AbilityRankType.DamageDice, ((ContextValue)Get(compare, "CheckValue")).ValueRank,
                    "NEC the level branch reads the template rank");
                return ((ContextValue)Get(compare, "TargetValue")).Value;
            }
            Eq(1, Threshold(branches[0]), "NEC the bloody template opens at template rank one, which starts at 9th level");
            Eq(2, Threshold(branches[1]), "NEC teamwork sharing opens at template rank two, eight levels later at 17th");
            var templateRank = area.Blueprint.GetComponents<ContextRankConfig>()
                .Single(rank => rank.Type == AbilityRankType.DamageDice);
            Eq(9, (int)Get(templateRank, "m_StartLevel"), "NEC the template rank starts at 9th level");
            Eq(8, (int)Get(templateRank, "m_StepLevel"), "NEC the teamwork branch follows eight levels later at 17th");
            Eq("DelayedStartPlusDivStep", Get(templateRank, "m_Progression").ToString(),
                "NEC the template rank uses the original delayed start progression");
            var capRank = area.Blueprint.GetComponents<ContextRankConfig>()
                .Single(rank => rank.Type == AbilityRankType.DamageDiceAlternative);
            Eq("Div2", Get(capRank, "m_Progression").ToString(), "NEC the servant cap is half the effective level");
            Eq(6, raise.MaximumUnits.Calculate(areaCast.Context), "NEC at level 13 the servant cap resolves to six");
            Check(bloody.GetComponent<AddEffectContextFastHealing>() != null &&
                bloody.GetComponent<AddStatBonus>()?.Stat == StatType.Charisma &&
                bloody.GetComponent<AddStatBonus>().Value == 4,
                "NEC the bloody template uses native fast healing and +4 Charisma");
            Check(teamwork.GetComponent<AddFactsFromCaster>() != null,
                "NEC teamwork sharing reuses the native caster-feat copy without modifying it");

            // Mind Fear uses the native Close band, which is 30 feet in Wrath.
            var fearAbility = Granted("MindFearAbility");
            Eq(AbilityRange.Close, fearAbility.Blueprint.Range, "NEC Mind Fear uses the native Close range band");
            Eq(30, fearAbility.Blueprint.GetRange(false, fearAbility).Value, "NEC Mind Fear reaches the signed 30 feet");

            // D13-F1: a paid held Flesh Rot touch must not survive losing the school
            // or a successful rest.
            var rot = Granted("FleshRotAbility");
            var diceMethod = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) });
            var diceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantDiceBoundary));
            harmony.Patch(diceMethod, prefix: new HarmonyMethod(diceBoundary));
            int rotBefore = pool.Remaining;
            Check(CastRule(rot).Success && pool.Remaining == rotBefore - 1,
                "NEC Flesh Rot pays on the casting action");
            var heldRot = unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>();
            Check(heldRot?.Ability?.Blueprint == Asset<BlueprintAbility>("FleshRotDelivery") &&
                  heldRot.Ability.Data.StickyTouch?.Blueprint == Asset<BlueprintAbility>("FleshRotAbility"),
                "NEC Flesh Rot leaves a native sticky-touch hold on its delivery ability");
            int beforeCombatLeave = pool.Remaining;
            Check(!unit.IsInCombat, "NEC fixture unit is out of combat before Flesh Rot combat-leave cleanup");
            Set(unit.Descriptor, "Brain", new UnitBrain());
            var rotCombat = Empty<Kingmaker.Controllers.Combat.UnitCombatState>();
            Set(rotCombat, "Unit", unit);
            Set(rotCombat, "m_ManualTarget", livingTarget);
            Set(unit, "CombatState", rotCombat);
            heldRot.HandleObjectLeaveCombat(unit);
            Set(unit, "CombatState", null);
            Check(unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>() == null &&
                  rotCombat.ManualTarget == null &&
                  pool.Remaining == beforeCombatLeave && hostLogs == 0,
                "NEC Flesh Rot HandleObjectLeaveCombat discards held touch and clears manual target without throwing or refunding focus");
            Check(CastRule(rot).Success && unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>() != null,
                "NEC Flesh Rot re-arms held touch before rest cleanup");
            Rest(unit);
            Check(unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>() == null,
                "NEC a successful rest discards the paid held Flesh Rot touch");
            CastRule(Granted("NecromancyInvestFocusAbility"));
            CastRule(Granted("LockFocusAbility"));
            Check(CastRule(rot).Success && unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>() != null,
                "NEC a fresh cycle can hold Flesh Rot again");
            unit.RemoveFact(implement);
            harmony.Unpatch(diceMethod, diceBoundary);
            Check(unit.Get<Kingmaker.UnitLogic.Parts.UnitPartTouch>() == null,
                "NEC losing the Necromancy implement discards the paid held Flesh Rot touch");
        }
        finally
        {
            applyNativeEffects = false;
            harmony.Unpatch(spawnMethod, spawnBoundary);
            harmony.Unpatch(poolsGetter, poolsBoundary);
            servantPools = null;
        }
        unit.RemoveFact(implement);
        Check(unit.Buffs.GetBuff(focusBuff) == null,
            "NEC removing the Necromancy implement retracts its resonance buff");

        // Losing the whole class must retract every private Necromancy effect on the
        // caster, not just the held touch, while leaving a familiar of their own alone.
        var retire = Unit("necromancy-retire", character, 12, 16);
        Set(retire.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(retire.Descriptor));
        var retireAc = new ModifiableValueArmorClass(retire.Stats);
        Set(retire.Stats, "AC", retireAc);
        retire.Stats.AllStats.Add(retireAc);
        var retireInit = new ModifiableValue(retire.Stats, StatType.Initiative);
        Set(retire.Stats, "Initiative", retireInit);
        retire.Stats.AllStats.Add(retireInit);
        foreach (var stat in new[] { StatType.SkillAthletics, StatType.SkillKnowledgeWorld, StatType.SkillLoreNature,
            StatType.SkillMobility, StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillStealth })
        {
            var skill = new ModifiableValueSkill(retire.Stats, retire.Stats.Dexterity, stat);
            Set(retire.Stats, stat.ToString(), skill);
            retire.Stats.AllStats.Add(skill);
        }
        Set(retire.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>());
        var retireFact = new Feature(Asset<BlueprintFeature>("MentalFocusFact"), retire.Descriptor);
        retire.Facts.Add(retireFact);
        retire.Facts.Add(new Feature(Asset<BlueprintFeature>("OccultistSpellbookFact"), retire.Descriptor));
        retire.Facts.Add(new Feature(Asset<BlueprintFeature>("slot-1-Necromancy-Implement"), retire.Descriptor));
        var retireBook = retire.Descriptor.DemandSpellbook(character.Spellbook);
        Set(retireBook, "m_BaseLevelInternal", 12);
        var retireLeveling = LevelState(retireBook);
        foreach (string token in new[] { "SoulboundPuppet", "SpiritShroud" })
        {
            var selection = retireLeveling.AddSelection(null, character.Progression, powers, 3);
            new SelectFeature(selection, Asset<BlueprintFeature>(token + "Power")).Apply(retireLeveling, retire.Descriptor);
        }
        AbilityData RetireGranted(string token) => retire.Abilities.Enumerable.Single(ability => ability.Blueprint == Asset<BlueprintAbility>(token)).Data;
        // Spirit Shroud rolls its temporary hit points through the two-argument dice
        // overload, which also reaches UnityEngine.Random.
        var retireDice = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(int), typeof(DiceType) });
        var retireDiceBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(ServantDiceBoundary));
        harmony.Patch(retireDice, prefix: new HarmonyMethod(retireDiceBoundary));
        applyNativeEffects = true;
        try
        {
            for (int i = 0; i < 3; i++) CastRule(RetireGranted("NecromancyInvestFocusAbility"));
            CastRule(RetireGranted("LockFocusAbility"));
            Check(CastRule(RetireGranted("SpiritShroudAbility")).Success &&
                  retire.Buffs.GetBuff(Asset<BlueprintBuff>("SpiritShroudBuff")) != null,
                "NEC Spirit Shroud applies its private temporary-hit-point buff");
            var ratFeature = Native<BlueprintFeature>("4d48365690ea9a746a74d19c31562788");
            var ratPuppet = RetireGranted("SoulboundPuppetAbility").GetConversions()
                .Single(choice => choice.Blueprint.name.EndsWith("SoulboundPuppet-" + ratFeature.AssetGuid));
            Check(CastRule(ratPuppet).Success && retire.Descriptor.HasFact(ratFeature),
                "NEC the retire fixture holds a native rat familiar granted by the puppet");
            // A familiar the character owns independently, from a different source.
            var ownFeature = Native<BlueprintFeature>("1cb0b559ca2e31e4d9dc65de012fa82f");
            retire.Facts.Add(new Feature(ownFeature, retire.Descriptor));
            retire.Progression.GetClassData(character).Level = 0;
            retire.Facts.Remove(retireFact);
            Check(retire.Buffs.GetBuff(Asset<BlueprintBuff>("SpiritShroudBuff")) == null,
                "NEC losing the whole class retracts Spirit Shroud before its natural expiry");
            Check(!retire.Descriptor.HasFact(ratFeature),
                "NEC losing the whole class retracts the temporary familiar the puppet granted");
            Check(retire.Descriptor.HasFact(ownFeature),
                "NEC losing the whole class leaves a familiar the character owns from another source");
        }
        finally
        {
            applyNativeEffects = false;
            harmony.Unpatch(retireDice, retireDiceBoundary);
        }
        caseStatus["Necromancy"] = "native_private_learning_dc_resonance_familiars_corpses_pool_level_branches_and_class_retirement";
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
            Check(CastRule(standard).Success && CastRule(flight).Success &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("MindOverGravityBuff")) != null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("PhilosophersTouchSilverBuff")) != null &&
                  unit.Buffs.GetBuff(ward) != null,
                "TRAN Legacy Weapon, Philosopher's Touch, and Mind Over Gravity are active before implement loss");
            unit.RemoveFact(implement);
            Check(unit.Buffs.GetBuff(ward) == null && body.PrimaryHand.Lock.Count == 0 && unit.Stats.Dexterity.ModifiedValue == 10 &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("MindOverGravityBuff")) == null &&
                  unit.Buffs.GetBuff(Asset<BlueprintBuff>("PhilosophersTouchSilverBuff")) == null &&
                  !club.Enchantments.Any() && !sword.Enchantments.Any() &&
                  Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(club) == 0,
                "TRAN removing the implement retracts physical enhancement, Mind Over Gravity, Philosopher's Touch, and Legacy Weapon effects");
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

    private static void TransmutationLifetimeNative(BlueprintsCache detached, BlueprintCharacterClass character)
    {
        T Asset<T>(string token) where T : BlueprintScriptableObject => (T)detached.Load(P(Occultist0099Ids.Id(token)));
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
            foreach (string ending in new[] { "implement loss", "rest", "class removal", "respec part replacement", "respec class removal" })
            {
                var caster = Recipient("lifetime-caster-" + ending);
                var ally = Recipient("lifetime-ally-" + ending);
                var focusFact = new Feature(Asset<BlueprintFeature>("MentalFocusFact"), caster.Descriptor);
                caster.Facts.Add(focusFact);
                var implement = new Feature(Asset<BlueprintFeature>("slot-1-Transmutation-Implement"), caster.Descriptor);
                var enchantImplement = new Feature(Asset<BlueprintFeature>("slot-2-Enchantment-Implement"), caster.Descriptor);
                var divImplement = new Feature(Asset<BlueprintFeature>("slot-3-Divination-Implement"), caster.Descriptor);
                caster.Facts.Add(implement);
                caster.Facts.Add(enchantImplement);
                caster.Facts.Add(divImplement);
                for (int i = 0; i < 4; i++) CastRule(GrantAbility(caster, Asset<BlueprintAbility>("TransmutationInvestFocusAbility")));
                for (int i = 0; i < 2; i++) CastRule(GrantAbility(caster, Asset<BlueprintAbility>("EnchantmentInvestFocusAbility")));
                for (int i = 0; i < 2; i++) CastRule(GrantAbility(caster, Asset<BlueprintAbility>("DivinationInvestFocusAbility")));
                CastRule(GrantAbility(caster, Asset<BlueprintAbility>("LockFocusAbility")));
                var quick = Asset<BlueprintBuff>("QuicknessBuff");
                var material = Asset<BlueprintBuff>("PhilosophersTouchColdIronBuff");
                var assault = Asset<BlueprintBuff>("InspiredAssaultBuff");
                var fortune = Asset<BlueprintBuff>("DivinersFortuneBuff");
                var quickCast = Rulebook.Trigger(new RuleCastSpell(GrantAbility(caster, Asset<BlueprintAbility>("QuicknessAbility")), new TargetWrapper(ally)));
                var materialCast = Rulebook.Trigger(new RuleCastSpell(GrantAbility(caster, Asset<BlueprintAbility>("PhilosophersTouchColdIronTouch")), new TargetWrapper(ally)));
                var assaultCast = Rulebook.Trigger(new RuleCastSpell(GrantAbility(caster, Asset<BlueprintAbility>("InspiredAssaultAbility")), new TargetWrapper(ally)));
                var fortuneCast = Rulebook.Trigger(new RuleCastSpell(GrantAbility(caster, Asset<BlueprintAbility>("DivinersFortuneDelivery")), new TargetWrapper(ally)));
                var incomingQuick = caster.Buffs.AddBuff(quick, ally, TimeSpan.FromMinutes(1));
                var incomingMaterial = caster.Buffs.AddBuff(material, ally, TimeSpan.FromMinutes(1));
                var incomingAssault = caster.Buffs.AddBuff(assault, ally, TimeSpan.FromMinutes(1));
                var incomingFortune = caster.Buffs.AddBuff(fortune, ally, TimeSpan.FromSeconds(6));
                Check(quickCast.Success && materialCast.Success && assaultCast.Success && fortuneCast.Success &&
                      ally.Buffs.GetBuff(quick)?.Context.MaybeCaster == caster &&
                      ally.Buffs.GetBuff(material)?.Context.MaybeCaster == caster &&
                      ally.Buffs.GetBuff(assault)?.Context.MaybeCaster == caster &&
                      ally.Buffs.GetBuff(fortune)?.Context.MaybeCaster == caster &&
                      ally.Body.PrimaryHand.MaybeWeapon.Enchantments.Any(),
                    "D32 " + ending + ": actual casts put Quickness, material enchantment, Inspired Assault and Diviner's Fortune on the allied recipient");
                Check(incomingQuick?.Context.MaybeCaster == ally && incomingMaterial?.Context.MaybeCaster == ally &&
                      incomingAssault?.Context.MaybeCaster == ally && incomingFortune?.Context.MaybeCaster == ally &&
                      caster.Body.PrimaryHand.MaybeWeapon.Enchantments.Any(),
                    "D32 " + ending + ": reciprocal buffs belong to the other caster");
                if (ending == "rest") Rest(caster);
                else if (ending == "implement loss")
                {
                    caster.RemoveFact(implement);
                    caster.RemoveFact(enchantImplement);
                    caster.RemoveFact(divImplement);
                }
                else if (ending.StartsWith("respec", StringComparison.Ordinal))
                {
                    // RespecOnCommit replaces Parts before calling the old
                    // survivor; exercise that boundary without opening its UI.
                    var oldPart = caster.Get<Occultist0099FocusPart>();
                    caster.Remove<Occultist0099FocusPart>();
                    oldPart.CopyAfterRespec(caster);
                    caster.RemoveFact(implement);
                    caster.RemoveFact(enchantImplement);
                    caster.RemoveFact(divImplement);
                    if (ending == "respec class removal")
                    {
                        caster.Progression.GetClassData(character).Level = 0;
                        caster.Facts.Remove(focusFact);
                    }
                    AccessTools.Method(typeof(Occultist0099FocusPart), "FinishRespec")
                        .Invoke(caster.Get<Occultist0099FocusPart>(), Array.Empty<object>());
                }
                else
                {
                    caster.Progression.GetClassData(character).Level = 0;
                    caster.Facts.Remove(focusFact);
                }
                Check(ally.Buffs.GetBuff(quick) == null && ally.Buffs.GetBuff(material) == null &&
                      ally.Buffs.GetBuff(assault) == null && ally.Buffs.GetBuff(fortune) == null &&
                      !ally.Body.PrimaryHand.MaybeWeapon.Enchantments.Any(),
                    "D32 " + ending + ": retracts the caster's remote Quickness, material enchantment, Inspired Assault and Diviner's Fortune");
                Check(ReferenceEquals(caster.Buffs.GetBuff(quick), incomingQuick) &&
                      ReferenceEquals(caster.Buffs.GetBuff(material), incomingMaterial) &&
                      ReferenceEquals(caster.Buffs.GetBuff(assault), incomingAssault) &&
                      ReferenceEquals(caster.Buffs.GetBuff(fortune), incomingFortune) &&
                      caster.Body.PrimaryHand.MaybeWeapon.Enchantments.Any(),
                    "D32 " + ending + ": preserves the other caster's incoming buffs and enchantment");
                incomingQuick.Remove();
                incomingMaterial.Remove();
                incomingAssault.Remove();
                incomingFortune.Remove();
            }
        }
        finally
        {
            applyNativeEffects = false;
            harmony.Unpatch(dollRoom, skipDollRoom);
            harmony.Unpatch(service, serviceBoundary);
        }
        caseStatus["D32TransmutationOwnership"] = "native_allied_cast_and_caster_scoped_rest_implement_loss_class_removal_respec_part_boundary";

        UnitEntityData Recipient(string label)
        {
            var unit = Unit(label, character, 7, 16);
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
            var damage = new ModifiableValue(unit.Stats, StatType.AdditionalDamage);
            Set(unit.Stats, "AdditionalDamage", damage);
            var ac = new ModifiableValueArmorClass(unit.Stats);
            Set(unit.Stats, "AC", ac);
            unit.Stats.AllStats.AddRange(new ModifiableValue[] { speed, attack, damage, ac });
            foreach (var stat in StatTypeHelper.Skills.Where(s => unit.Stats.GetStat(s) == null))
            {
                var skill = new ModifiableValueSkill(unit.Stats, unit.Stats.Wisdom, stat);
                unit.Stats.AllStats.Add(skill);
                Set(unit.Stats, stat.ToString(), skill);
            }
            var weapon = Native<BlueprintItemWeapon>("07863393521453a4da9e98626531eb5f").CreateEntity<ItemEntityWeapon>();
            body.PrimaryHand.SetFakeItem(weapon);
            weapon.HoldingSlot = body.PrimaryHand;
            Set(weapon, "m_WielderRef", (EntityRef<UnitEntityData>)unit);
            weapon.Facts.TurnOn();
            return unit;
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

            unit.Buffs.GetBuff(ward)?.Remove();
            Rest(unit);
            var divImp = new Feature(Asset<BlueprintFeature>("slot-2-Divination-Implement"), unit.Descriptor);
            var evoImp = new Feature(Asset<BlueprintFeature>("slot-3-Evocation-Implement"), unit.Descriptor);
            var necImp = new Feature(Asset<BlueprintFeature>("slot-4-Necromancy-Implement"), unit.Descriptor);
            var paraphernaliaImp = new Feature(Asset<BlueprintFeature>("slot-5-MagesParaphernalia"), unit.Descriptor);
            unit.Facts.Add(divImp);
            unit.Facts.Add(evoImp);
            unit.Facts.Add(necImp);
            unit.Facts.Add(paraphernaliaImp);
            for (int i = 0; i < 2; i++) CastRule(invest);
            CastRule(Granted("DivinationInvestFocusAbility"));
            CastRule(Granted("EvocationInvestFocusAbility"));
            CastRule(Granted("NecromancyInvestFocusAbility"));
            Check(CastRule(Granted("LockFocusAbility")).Success,
                "AEGIS locks Abjuration and Paraphernalia pools before storage caster-level check");
            Toggle("AegisSpellStoringChoice").IsOn = true;
            Check(CastRule(standard).Success, "AEGIS arms Spell Storing for private-book Spell Power test");
            Set(classBook, "m_BaseLevelInternal", 12);
            var privateTouch = new AbilityData(touchSpell, classBook, 1);
            var classKnown1 = ((List<AbilityData>[])Get(classBook, "m_KnownSpells"))[1];
            classKnown1.Add(privateTouch);
            ((int[])Get(classBook, "m_SpontaneousSlots"))[1] = 1;
            var aegisSpellPower2 = Toggle("SpellPower2Toggle");
            aegisSpellPower2.IsOn = true;
            Eq(14, privateTouch.CalculateParams().CasterLevel,
                "AEGIS baseline private spell previews +2 Spell Power CL before storing");
            var privateStoreCast = CastRule(Conversion(privateTouch));
            int panoplyRemainingAfterStore = focus.Schools["Divination"].Remaining +
                                            focus.Schools["Evocation"].Remaining +
                                            focus.Schools["Necromancy"].Remaining;
            Check(privateStoreCast.Success && payload.Parameters.CasterLevel == 12 &&
                  panoplyRemainingAfterStore == 3,
                "AEGIS storing a private-book touch spell excludes free Spell Power CL and spends no Panoply focus");
            aegisSpellPower2.IsOn = false;
            Toggle("AegisSpellStoringChoice").IsOn = false;
            classKnown1.Remove(privateTouch);
            unit.RemoveFact(paraphernaliaImp);
            unit.RemoveFact(necImp);
            unit.RemoveFact(evoImp);
            unit.RemoveFact(divImp);

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
        var implementedSchools = new HashSet<string> { "Abjuration", "Conjuration", "Enchantment", "Divination", "Evocation", "Illusion", "Necromancy", "Transmutation" };
        var rows = catalogue.Where(row => (implementedSchools.Contains(row.School) || row.IsKnack) && row.Source != "External").ToArray();
        var coreDonors = (string[])AccessTools.Field(Production("Occultist0099Enchantment"), "DonorIds").GetValue(null);
        var armorDonors = (string[])AccessTools.Field(Production("Occultist0099Aegis"), "Donors").GetValue(null);
        var divinationDonors = (string[])AccessTools.Field(Production("Occultist0099Divination"), "DonorIds").GetValue(null);
        var illusionDonors = (string[])AccessTools.Field(Production("Occultist0099Illusion"), "DonorIds").GetValue(null);
        var shadowDonors = (string[])AccessTools.Field(Production("Occultist0099ShadowBeasts"), "DonorIds").GetValue(null);
        var necromancyDonors = (string[])AccessTools.Field(Production("Occultist0099Necromancy"), "Donors").GetValue(null);
        var familiarDonors = FamiliarDonorIds();
        var conjurationDonors = (string[])AccessTools.Field(Production("Occultist0099Conjuration"), "Donors").GetValue(null);
        var evocationDonors = (string[])AccessTools.Field(Production("Occultist0099Evocation"), "Donors").GetValue(null);
        var transmutationDonors = (string[])AccessTools.Field(Production("Occultist0099Transmutation"), "DonorIds").GetValue(null);
        var weaponDonors = (IEnumerable<string>)AccessTools.Property(Production("Occultist0099TransmutationWeapons"), "Donors").GetValue(null);
        var panoplyDonors = (string[])AccessTools.Field(Production("Occultist0099Panoplies"), "Donors").GetValue(null);
        var fighterRow = ((JArray)nativeRows["requiredNative"]).Cast<JObject>()
            .Single(row => (string)row["blueprint"]["AssetId"] == "41c8486641f7d6d4283ca9dae4147a9f");
        var fighterPool = ((JArray)fighterRow["blueprint"]["Data"]["m_AllFeatures"])
            .Select(token => ((string)token).Substring(4)).Distinct().ToArray();
        var expectedDonors = new[] { alchemistClassId, proficienciesId, "027d37761f3804042afa96fe3e9086cc", "f049fe38f5bb5ae48b252852727ab86a",
            "143775c49ae6b7446b805d3b2e702298" }
            .Concat(coreDonors).Concat(armorDonors).Concat(divinationDonors).Concat(illusionDonors).Concat(shadowDonors)
            .Concat(necromancyDonors).Concat(familiarDonors).Concat(conjurationDonors).Concat(evocationDonors)
            .Concat(transmutationDonors).Concat(weaponDonors).Concat(panoplyDonors).Concat(fighterPool)
            .Concat(rows.Select(row => row.Guid)).Distinct().ToArray();
        LoadRows(expectedDonors);
        var transmutationRoots = rows.Where(row => row.Source == "CotW" && row.School == "Transmutation").ToArray();
        if (transmutationRoots.Any(row => cache.Load(P(row.Guid)) == null)) StageTransmutationRoots(transmutationRoots);
        var necromancyRoots = rows.Where(row => row.Source == "CotW" && row.School == "Necromancy").ToArray();
        if (necromancyRoots.Any(row => cache.Load(P(row.Guid)) == null)) StageNecromancyRoots(necromancyRoots);
        var conjurationRoots = rows.Where(row => row.Source == "CotW" && row.School == "Conjuration").ToArray();
        if (conjurationRoots.Any(row => cache.Load(P(row.Guid)) == null) ||
            cache.Load(P("91651f6f9af8b82e5aff6e2cb7aedfa3")) == null) StageConjurationRoots(conjurationRoots);
        var evocationRoots = rows.Where(row => row.Source == "CotW" && row.School == "Evocation").ToArray();
        if (evocationRoots.Any(row => cache.Load(P(row.Guid)) == null)) StageEvocationRoots(evocationRoots);
        foreach (var row in rows.Where(row => row.Source == "CotW" && row.School != "Transmutation" && row.School != "Necromancy" &&
                 row.School != "Conjuration" && row.School != "Evocation" && cache.Load(P(row.Guid)) == null))
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
        var tokens = (string[])AccessTools.Method(Production("Occultist0099NativeRegistration"), "IdentityTokens").Invoke(null, new object[] { detached });
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

        // The career Knacks feature grants the six abilities and binds their
        // cast parameters to the occultist class (Cantrip, LevelStep 1).
        var knacksFeature = (BlueprintFeature)detached.Load(P(Occultist0099Ids.Id("OccultistKnacksFeature")));
        var knacksGrant = new Feature(knacksFeature, firstLevel.Descriptor);
        firstLevel.Facts.Add(knacksGrant);
        Check(Occultist0099AbjurationCatalog.Knacks.All(guid =>
                  firstLevel.Abilities.Enumerable.Any(ability => ability.Blueprint.AssetGuid == P(guid))),
            "SP01 the career knacks feature grants all six knack abilities");
        var knackData = knackBook.GetKnownSpells(0).First(ability =>
            ability.Blueprint.AssetGuid == P(Occultist0099AbjurationCatalog.Knacks[0]));
        Eq(0, knackData.SpellLevel, "SP01 a knack learned at circle zero keeps spell level zero");
        Eq(0, knackBook.GetSpellLevel(knackData), "SP01 the private book reports circle zero for the knack");
        Check(knackBook.CanSpend(knackData, false), "SP01 the private book can always spend a circle-zero knack");
        int firstCircleSlots = knackBook.GetSpontaneousSlots(1);
        for (int i = 0; i < 5; i++)
        {
            Check(knackBook.Spend(knackData, false), "SP01 spending a knack succeeds on every repetition");
            Eq(firstCircleSlots, knackBook.GetSpontaneousSlots(1), "SP01 spending a knack never consumes a paid first-circle slot");
        }
        Eq(0, knackBook.GetSpontaneousSlots(0), "SP01 knacks draw on no level-zero slot pool");
        // Losing the career feature retracts the granted abilities; the learned
        // book entries and their unlimited nature survive a real rest.
        firstLevel.Facts.Remove(knacksGrant);
        Check(Occultist0099AbjurationCatalog.Knacks.All(guid =>
                  !firstLevel.Abilities.Enumerable.Any(ability => ability.Blueprint.AssetGuid == P(guid))),
            "SP01 removing the knacks feature retracts all six granted abilities");
        firstLevel.Facts.Add(new Feature(knacksFeature, firstLevel.Descriptor));
        Rest(firstLevel);
        Check(knackBook.GetKnownSpells(0).Count == 6 && knackBook.CanSpend(knackData, false),
            "SP01 a real rest keeps the six knacks known and unlimited");

        // The binding assertions below run the real RuleCalculateAbilityParams
        // event chain, which needs the difficulty root service; the same
        // fixture the school checks install at line ~5225 serves it here.
        var knackDifficulty = Empty<Kingmaker.Settings.Difficulty.DifficultyPresetsList>();
        knackDifficulty.StatsAdjustmentPresets = new[] { new Kingmaker.Settings.Difficulty.DifficultyPresetsList.StatsAdjustmentPreset() };
        Set(root, "m_DifficultyList", knackDifficulty);
        EnchantmentFixtureRoot = root;
        var knackRootGetter = AccessTools.PropertyGetter(typeof(BlueprintRoot), nameof(BlueprintRoot.Instance));
        var knackRootBoundary = AccessTools.Method(typeof(Occultist0099NativeTests), nameof(EnchantmentRootBoundary));
        harmony.Patch(knackRootGetter, prefix: new HarmonyMethod(knackRootBoundary));
        try
        {
            Eq(1, knackData.CalculateParams().CasterLevel, "SP01 the knack binding sets caster level to the class level at first level");
            Eq(0, knackData.CalculateParams().SpellLevel, "SP01 the knack binding pins the effective spell level to zero");
            firstLevel.Progression.GetClassData(publishedClass).Level = 7;
            Eq(7, knackData.CalculateParams().CasterLevel, "SP01 the knack caster level tracks the live class level");
            // The upstream createCantrips binding (Common.cs:315-335) is keyed by
            // ability identity, not by spellbook: on a multiclass unit the native
            // BindAbilitiesToClass applies the occultist class level to the same
            // root cast from a foreign book too. This directed check records that
            // real native behaviour; the private book keeps its own base level
            // untouched and the foreign book's slots are its own.
            var knackForeignBook = new BlueprintSpellbook { AssetGuid = P(Occultist0099Ids.Id("HostKnackForeignBook")),
                name = "0099N.KnackForeignBook", Spontaneous = true, IsArcane = false, CastingAttribute = StatType.Intelligence,
                ComponentsArray = Array.Empty<BlueprintComponent>() };
            Set(knackForeignBook, "m_SpellList", Ref<BlueprintSpellListReference>(baseList));
            Set(knackForeignBook, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(publishedBook.SpellsPerDay));
            knackForeignBook.OnEnable();
            var foreignKnackBook = firstLevel.Descriptor.DemandSpellbook(knackForeignBook);
            Set(foreignKnackBook, "m_BaseLevelInternal", 1);
            var foreignKnack = new AbilityData(knackData.Blueprint, foreignKnackBook, 0);
            Eq(1, foreignKnackBook.BaseLevel,
                "SP01 the foreign book keeps its own base level while sharing the knack root");
            Eq(7, foreignKnack.CalculateParams().CasterLevel,
                "SP01 the native identity-keyed knack binding applies the occultist class level to the shared root on the foreign book (upstream createCantrips semantics)");
            Eq(7, knackData.CalculateParams().CasterLevel,
                "SP01 the private-book knack still reports the occultist class level after the foreign cast");
            // The private book's base level tracks the class through ApplySpellbook,
            // not through the raw ClassData edit above; only the binding reads the
            // live class level. The foreign book keeps base level 1 and, like any
            // circle-zero entry, spends without a slot.
            Check(knackBook.BaseLevel == 1 && foreignKnackBook.BaseLevel == 1 &&
                  foreignKnackBook.CanSpend(foreignKnack, false) && foreignKnackBook.Spend(foreignKnack, false),
                "SP01 the foreign cast leaves both book base levels intact and the circle-zero knack spendable from either book");
            firstLevel.Progression.GetClassData(publishedClass).Level = 1;
            Rest(firstLevel);
            Eq(1, knackData.CalculateParams().CasterLevel,
                "SP01 a real rest keeps the knack caster level bound to the class level");
        }
        finally { harmony.Unpatch(knackRootGetter, knackRootBoundary); EnchantmentFixtureRoot = null; }

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
        var wizardRows = wizardList.SpellsByLevel;
        Check(TryRegister(detached) && sourceList.GetSpells(3).Contains(cloak) && baseList.GetSpells(3).Contains(cloak) &&
              entries.SequenceEqual(privateIds.Select(detached.Load)) && root.Progression.CharacterClasses.Count() == 2 &&
              wizardList.SpellsByLevel == wizardRows && wizardList.SpellsByLevel[3].SpellsRefs.Count == 1,
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
            NecromancyNative(detached, publishedClass);
            ConjurationNative(detached, publishedClass);
            EvocationNative(detached, publishedClass);
            TrappingsNative(detached, publishedClass);
            ParaphernaliaNative(detached, publishedClass);
            PerformersNative(detached, publishedClass);
            RegaliaNative(detached, publishedClass);
            TransmutationNative(detached, publishedClass);
            TransmutationLifetimeNative(detached, publishedClass);
            MasteryNative(detached, publishedClass);
            RepowerNative(detached, publishedClass);
            SpellbookGrowthNative(detached, publishedClass);
            PsychicCrossBookIsolationNative(detached, publishedClass);
            StartupNative(detached);
        }
        finally
        {
            harmony.Unpatch(ignorePrerequisites, retail);
            harmony.Unpatch(rootGetter, rootBoundary);
            harmony.Unpatch(visual, noVisual);
            EnchantmentFixtureRoot = null;
        }
        caseStatus["SP08"] = "native_atomic_detached_eight_schools_panoplies_and_isolated_shared_startup_with_slice_unpublished";
        caseStatus["SP03"] = "native_copy_serialization_and_last_registered_implement_revoke_only";
    }

    // ------------------------------------------------------------------
    public static void Run(string[] args)
    {
        string managed = args[0], candidate = args[1], reportPath = args[2];
        string assetsPath = args[3], nativePath = args[4];
        hostReportPath = reportPath;
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
        int necromancyArchiveRows = LoadArchiveDonors(Environment.GetEnvironmentVariable("COTW_0099_NECROMANCY_FIXTURE"));
        Eq(76, necromancyArchiveRows, "HOST loads the signed Necromancy effects, corpse types and immunities, skeleton, DLC gate, scroll and 13 native familiars");
        int evocationArchiveRows = LoadArchiveDonors(Environment.GetEnvironmentVariable("COTW_0099_EVOCATION_FIXTURE"));
        Eq(145, evocationArchiveRows, "HOST loads the signed Evocation rays, walls, light, glow and spirit-weapon donors");
        int conjurationArchiveRows = LoadArchiveDonors(Environment.GetEnvironmentVariable("COTW_0099_CONJURATION_FIXTURE"));
        Eq(10, conjurationArchiveRows, "HOST loads the signed Conjuration healing, cleansing, teleport and icon donors");
        var animateLesserReward = Native<Kingmaker.DLC.BlueprintDlcReward>("1c893fd3059d4b09bd37d089aff7866e");
        Set(animateLesserReward, "m_IsAvailable", (bool?)false);
        var animateLesser = Native<BlueprintAbility>("57fcf8016cf04da4a8b33d2add14de7e");
        Check(Kingmaker.DLC.DlcConditionExtension.IsDlcRestricted(animateLesser),
            "NEC Lesser Animate Dead keeps its native DLC5 campaign gate");
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
            "MF02", "MF06", "MF10",
            "SP04", "SP05", "SP06"
        })
            caseStatus[id] = "not_run_native";

        var report = new JObject
        {
            ["workPackage"] = "COTW-0099",
            ["contract"] = Occultist0099Ids.Contract,
            ["stage"] = "Joint TEST: eight-school career, Focus Part TypeId and enabled ordinary startup",
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
            ["necromancyArchiveRows"] = necromancyArchiveRows,
            ["evocationArchiveRows"] = evocationArchiveRows,
            ["conjurationArchiveRows"] = conjurationArchiveRows,
            ["transmutationArchiveRows"] = transmutationArchiveRows,
            ["nativeRootsLoaded"] = loadedRoots,
            ["hostStagedRoots"] = hostStaged,
            ["hostStagedRootNames"] = new JArray(stagedRoots),
            ["skippedArchiveComponents"] = skippedComponents,
            ["rootFixtureBoundary"] = "Transmutation uses 14 Wrath roots and 9 roots produced by the existing CotW builders; Necromancy uses 19 Wrath roots and 2 existing CotW roots; Conjuration uses 12 Wrath roots and 1 existing CotW root, plus two existing CotW fog roots as focus-power donors. Native DLC reward availability is supplied to exercise the Keen Edge and Lesser Animate Dead gates; the external provider roots are absent.",
            ["checks"] = checks,
            ["failures"] = new JArray(failures),
            ["hostLoggedExceptions"] = hostLogs,
            ["caseStatus"] = JObject.FromObject(caseStatus),
            ["result"] = failures.Count == 0 ? "NATIVE_DIRECTED_PASS" : "NATIVE_DIRECTED_FAIL",
            ["resultMeaning"] = failures.Count == 0
                ? "Detached native Knacks, private-book circle growth across circles 3-6 (including native Disintegrate at L=15/16), level-20 Implement Mastery, live level-8 Repower Construct RuleSummonUnit takeover, null-spawn PayOnEffect safety and expiry, Panoply FocusPowerSelection wiring (Combat Trick, Metamagic Knowledge, Metamagic Master, Guardian Aura vs base Restoring Touch & Counterstrike toggle), separate size-1 Spell Power and Metamagic Master exclusive groups, rest/combat-end/member-loss buff & held-touch cleanup, SP07 cross-book Psychic isolation, and shared startup localization plus ReconcileOptional late-donor refresh passed with the production gate enabled."
                : "At least one native directed assertion failed; see failures.",
            ["boundary"] = "Detached BlueprintsCache, native choice, spellbook and cast payment. Necromantic Focus enters the native RuleSavingThrow with the buff's own fact context. Soulbound Puppet grants and revokes the real native familiar bond features. Necromantic Servant validates real corpse state and creature types, counts the native summon pool, and drives the native burst selection; only the Unity spawn and the geometry probe are stubbed, so HD, duration and pool registration requests are asserted rather than a live creature. Casting Focus is checked on both sides: the buff side through the native CalculateDuration path, and the summon side by driving the handler with a real RuleSummonUnit and reading BonusDuration. Native rulebook dispatch itself is not simulated, so buff attachment is the observable gate for the summon handler. Casting Focus cap is pinned with locked level-4 and Implement Mastery fixtures. Side Step range is checked through the native GetRange resolution, not a live teleport. Servitor branch selection is asserted from the paid cast ranks; the Unity spawn is stubbed, so no live creature is created. Implement Mastery is checked through the real level-20 selection prerequisite, feature activation/deactivation, the native Mind Barrier cast and temporary hit points, the Warding Talisman save modifiers, the resource mirror, a same-day switch, a real rest re-grant and implement source loss; the Panoply payment in that fixture goes through the authoritative PayPanoply entry, not a live Counterstrike event. Paraphernalia checks native AbilityData.HasMetamagic, UnitUseAbility.CreateCastCommand, BlueprintAbility.GetRange, RuleCalculateAbilityParams, MechanicsContext.Recalculate, duplicate-metamagic exclusion, independent size-1 Spell Power and Metamagic Master groups, FocusPowerSelection nested selection for Metamagic Knowledge and Metamagic Master, rest/member-loss buff cleanup, and combined atomic payment. Counterstrike checks its availability toggle across unlocked choice, lock, rest, Day-2 re-lock, and member loss, Combat Trick nested selection in FocusPowerSelection, and is driven through its damage and attack handlers with UnitHelper reach/hand and UnitCombatState.AttackOfOpportunity stubbed to assert both 0-damage hits and native AoO refusals leave Focus and Swift untouched. Flesh Rot checks combat-end UnitPartTouch.HandleObjectLeaveCombat cleanup, rest, and school-loss cleanup; Evocation and Transmutation check rest and school-loss cleanup of Light Matrix, Radiance, Mind Over Gravity, and Philosopher's Touch. SP07 stages a synthetic PsychicClass/BookId/pool/amplification fixture in cache and patches PsychicCasting/NatureTargeting/PsychicUndercasting to verify thought/emotion/Logical inheritance vs amplification, Nature's Command, and undercasting isolation across OccultistSpellbook and PsychicClass.BookId. Repower Construct executes live RuleCastSpell -> RuleSummonUnit (including null-spawn refusal via PayOnEffect before charge) against an enemy construct corpse with EntityCreationController.SpawnUnit, UniRx.Observable.Timer, ResourcesLibrary.GetRoot and FX logging stubbed, verifying friendly faction switch, summon pool registration, SummonedUnitBuff attachment and expiration cleanup (DestroyMark). Ordinary startup is verified with the release gate enabled in an isolated shared cache, exercising RegisterShared localization publishing, LocalizationReadyPatch pack replacement, ReconcileOptional late optional CloakOfWinds refresh/withdrawal and withdrawn-spell cast/spend gating with preserved GUID and quota while stubbing unrelated mod modules and restoring all static state. The private book's learning entry is driven at the audited ladder for circles 3, 4, 5 (Abjuration L=7/10/13) and circle 6 (Transmutation native Disintegrate at L=15/16). Unity scenes, UI, the real mod load, a real third-party provider and a disk save remain outside scope.",
            ["panoplyBoundary"] = "Performer checks native member choice and uniqueness, union learning and loss, swift cast payment and the Puppet Master level gate and DC. Regalia enters the native healing rule with final health application stubbed, checks sticky and foreign spellbook sources, and applies Guardian Aura with Unity area activation stubbed. Puppet Master domination delivery and ability point recovery were not executed.",
            ["notRun"] = new JArray(
                "game/UMM startup", "player gameplay acceptance",
                "full historical suites", "live game shared class/spellbook/progression registration",
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
