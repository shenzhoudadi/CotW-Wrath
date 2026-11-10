using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using CallOfTheWild.Wrath.Prototype;

// These regressions intentionally use the game's manager/fact/runtime-component
// lifecycle and EventBus. They do not seed lifecycle flags or call Notify.
internal static class EnhancementLifecycle01430Tests
{
    const string OppositionId = "fc519612a3c604446888bb345bca5234";
    const string OppositionPoolId = "6c29030e9fea36949877c43a6f94ff31";
    const string SchoolPoolId = "5f838049069f1ac4d804ce0862ab5110";
    const string SourceId = "b6a604dab356ac34788abf4ad79449ec";
    static int checks, added, removed, selections, nativeOns, nativeOffs, componentOns, componentOffs;
    static bool failAppend;
    static readonly List<UnitEntityData> emptyParty = new List<UnitEntityData>();
    static readonly List<string> failures = new List<string>();
    static readonly List<Buff> lifecycleFacts = new List<Buff>();
    static Type Suite => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Migration01429Tests")).First(t => t != null);
    static Type Module => typeof(EnhancementNaturalArmor01429).Assembly.GetType("CallOfTheWild.Wrath.Prototype.EnhancementSchool01429", true);
    static object Call(string method, params object[] args) => AccessTools.Method(Suite, method).Invoke(null, args);
    static object Get(object value, string field) => Call("Get", value, field);
    static void Set(object value, string field, object next) => Call("Set", value, field, next);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)AccessTools.Method(Suite, "Asset").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { c, token });
    static T Hydrate<T>(BlueprintsCache c, string id) where T : BlueprintScriptableObject, new() => (T)AccessTools.Method(Suite, "Hydrate").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { c, id, Array.Empty<string>() });
    static UnitEntityData Unit(BlueprintsCache c, string name, int level) => (UnitEntityData)Call("Unit", c, name, c.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e")), level);
    static BlueprintFeatureReference Ref(BlueprintFeature b) { var r = BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static void Register(BlueprintsCache c) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { c });
    static void Check(bool condition, string message) { checks++; if (!condition) { failures.Add(message); Console.WriteLine("FAIL Enhancement01430: " + message); } }
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Skip() => false;
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool Time(ref TimeSpan __result) { __result = TimeSpan.FromSeconds(100); return false; }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == (BlueprintGuid)Call("Id", "EnhancementSchool"))) throw new InvalidOperationException("Enhancement01430 append fault"); }
    static void FactOn(EntityFact __instance) { if (lifecycleFacts.Contains(__instance as Buff)) nativeOns++; }
    static void FactOff(EntityFact __instance) { if (lifecycleFacts.Contains(__instance as Buff)) nativeOffs++; }
    static void ComponentOn() { componentOns++; }
    static void ComponentOff() { componentOffs++; }
    sealed class Events : IUnitBuffHandler
    {
        public void HandleBuffDidAdded(Buff b) { if (lifecycleFacts.Contains(b)) added++; }
        public void HandleBuffDidRemoved(Buff b) { if (lifecycleFacts.Contains(b)) removed++; }
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = added = removed = selections = nativeOns = nativeOffs = componentOns = componentOffs = 0;
        failures.Clear(); lifecycleFacts.Clear();
        var h = new Harmony("CotW01430.EnhancementNativeLifecycle");
        var savedTime = Game.Instance.TimeController;
        var savedState = Game.Instance.State;
        var player = (Player)FormatterServices.GetUninitializedObject(typeof(Player));
        player.GameTime = TimeSpan.FromSeconds(100);
        Set(player, "m_Party", emptyParty); Set(player, "m_CharacterListsValid", true);
        // Game.Player is small enough to be inlined by callers exercised in
        // earlier suites. Seed its real storage instead of patching its getter.
        var state = (PersistentState)FormatterServices.GetUninitializedObject(typeof(PersistentState)); state.PlayerState = player;
        Set(Game.Instance, "State", state);
        Set(Game.Instance, "TimeController", FormatterServices.GetUninitializedObject(typeof(TimeController)));
        void Patch(MethodBase method, string prefix) => h.Patch(method, prefix: new HarmonyMethod(typeof(EnhancementLifecycle01430Tests), prefix));
        Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Time));
        Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(No));
        Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(No));
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No));
        Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore));
        Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController));
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(Suite.Assembly.GetType("Howl01428Tests", true), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(EntityFact), "TurnOn"), postfix: new HarmonyMethod(typeof(EnhancementLifecycle01430Tests), nameof(FactOn)));
        h.Patch(AccessTools.Method(typeof(EntityFact), "TurnOff"), postfix: new HarmonyMethod(typeof(EnhancementLifecycle01430Tests), nameof(FactOff)));
        h.Patch(AccessTools.Method(typeof(EnhancementNaturalArmor01429), "OnTurnOn"), postfix: new HarmonyMethod(typeof(EnhancementLifecycle01430Tests), nameof(ComponentOn)));
        h.Patch(AccessTools.Method(typeof(EnhancementNaturalArmor01429), "OnTurnOff"), postfix: new HarmonyMethod(typeof(EnhancementLifecycle01430Tests), nameof(ComponentOff)));
        var listener = new Events(); EventBus.Subscribe(listener);
        Exception primaryFailure = null;
        try {
            foreach (var type in new[] { typeof(EntityFactsManager), typeof(EntityFact), typeof(EntityFactComponent) })
                foreach (var name in (type == typeof(EntityFact) ? new[] { "Attach", "Detach", "Activate", "Deactivate", "TurnOn", "TurnOff" } : type == typeof(EntityFactsManager) ? new[] { "Add", "Remove", "TurnOn", "TurnOff" } : new[] { "TurnOn", "TurnOff" })) {
                    var patches = Harmony.GetPatchInfo(AccessTools.Method(type, name));
                    Check(patches == null || patches.Prefixes.Count == 0 && patches.Transpilers.Count == 0, "native lifecycle has no skipping prefix/transpiler: " + type.Name + "." + name);
                }
            Selection(cache, h);
            foreach (bool reverse in new[] { false, true }) Armor(cache, reverse);
            Check(added == 6 && removed == 6, "real BuffCollection EventBus emits exactly six additions and six removals");
            Check(nativeOns > 0 && nativeOffs > 0 && componentOns == nativeOns && componentOffs == nativeOffs, "every native fact transition reaches the actual Enhancement runtime component");
            var inventoryType = Suite.Assembly.GetType("PsychicExpansionTests", true);
            ((JObject)AccessTools.Field(inventoryType, "Inventory").GetValue(null))["enhancementLifecycle01430Contracts"] = new JObject {
                ["passed"] = failures.Count == 0, ["assertions"] = checks, ["failures"] = JArray.FromObject(failures),
                ["nativeCanSelectCalls"] = selections, ["sourceOrders"] = 2, ["nativeAddedEvents"] = added, ["nativeRemovedEvents"] = removed,
                ["nativeFactTurnOnCalls"] = nativeOns, ["nativeFactTurnOffCalls"] = nativeOffs, ["componentTurnOnCalls"] = componentOns, ["componentTurnOffCalls"] = componentOffs,
                ["nativeManagerLifecycle"] = failures.Count == 0, ["nativeEventBus"] = added == 6 && removed == 6,
                ["sceneFixturePersistentStateInitialized"] = true,
                ["noSyntheticLifecycleNotifications"] = failures.Count == 0, ["noLifecycleMethodReplacement"] = failures.Count == 0,
                ["registrationAppendRollback"] = failures.Count == 0, ["repeatRegistrationIdempotence"] = failures.Count == 0,
                ["positiveAndNegativeCasesCompleted"] = failures.Count == 0,
                ["cases"] = new JArray("weak-first and strong-first native additions", "manager off/on twice without buff notifications", "strong and weak direct turnoff/on", "strong and weak native deactivate/activate", "native post-load fixes", "equal-strength source ownership", "native strongest removal and weaker recovery", "unrelated natural armor retained", "Enhancement and native Transmutation reject own opposition", "opposition first rejects both school roots", "seven other native opposition schools remain selectable", "fresh append fault rolls back assets/pool/opposition", "repeat registration and foreign pool member preservation"),
                ["boundary"] = "Constructed units and selected-school marker facts; actual Buff constructor, EntityFactsManager Add/Remove/TurnOff/TurnOn, EntityFact Attach/Activate/Deactivate/Detach/TurnOff/TurnOn, runtime components, BuffCollection and EventBus execute. Real fixture PersistentState.PlayerState, Player.GameTime and empty native party cache are initialized and original Game.State is restored; no replacement of Player/Party getters. Fixed game time and inactive scene combat; RuleCanApplyBuff difficulty-settings getter uses existing controlled boundary. No game UI, real save file, Unity rendering or game launch. CanSelect and prerequisite checks are original game code." };
            if (failures.Count != 0) throw new Exception("Enhancement01430 regressions failed: " + string.Join("; ", failures));
            Console.WriteLine("PASS: " + checks + " Enhancement01430 native lifecycle/selection contracts; events " + added + "/" + removed + ", fact transitions " + nativeOns + "/" + nativeOffs + ".");
            return checks;
        }
        catch (Exception error) { primaryFailure = error; Console.WriteLine("Enhancement01430 primary failure: " + error); throw; }
        finally {
            Exception cleanupFailure = null;
            try {
                foreach (var b in lifecycleFacts.Where(b => b.Manager != null).ToArray()) {
                    try { b.Manager.Remove(b); }
                    catch (Exception error) { cleanupFailure = cleanupFailure ?? error; Console.WriteLine("Enhancement01430 cleanup failure: " + error); }
                }
            }
            finally {
                try { EventBus.Unsubscribe(listener); }
                finally { h.UnpatchAll(h.Id); Set(Game.Instance, "TimeController", savedTime); Set(Game.Instance, "State", savedState); }
            }
            if (primaryFailure == null && cleanupFailure != null) throw new Exception("Enhancement01430 native cleanup failed", cleanupFailure);
        }
    }
    static void Selection(BlueprintsCache cache, Harmony h)
    {
        // Replace the old suite's reference-only donors with current native JSON,
        // then let the production registration amend the real opposition object.
        var opposition = Hydrate<BlueprintFeature>(cache, OppositionId);
        var oppositionPool = Hydrate<BlueprintFeatureSelection>(cache, OppositionPoolId);
        var choices = ((BlueprintFeatureReference[])Get(oppositionPool, "m_AllFeatures")).Select(r => r.Guid.ToString()).Distinct().ToArray();
        foreach (var id in choices.Where(id => id != OppositionId)) Hydrate<BlueprintFeature>(cache, id);
        foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Set(oppositionPool, field, ((BlueprintFeatureReference[])Get(oppositionPool, field)).Select(r => Ref((BlueprintFeature)cache.Load(r.Guid))).ToArray());
        var nativeComponents = opposition.ComponentsArray;
        var nativeDefinition = (string)Call("Definition", (object)nativeComponents);
        var oppositionPoolBefore = new[] { Get(oppositionPool, "m_Features"), Get(oppositionPool, "m_AllFeatures") };
        var school = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(SchoolPoolId));
        var schoolBefore = new[] { Get(school, "m_Features"), Get(school, "m_AllFeatures") };
        var originalAssets = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray().Select(id => cache.Load(id)).Where(b => b.name?.StartsWith("CotWWrathPsychicEnhancement", StringComparison.Ordinal) == true).ToArray();
        var root = Asset<BlueprintProgression>(cache, "EnhancementSchool");
        // Exercise a genuinely fresh transaction with failure after pool append.
        // Exact prior assets/arrays are restored so later suites retain identity.
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(EnhancementLifecycle01430Tests), nameof(AppendFault)));
        try {
            foreach (var bp in originalAssets) cache.RemoveCachedBlueprint(bp.AssetGuid);
            foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Set(school, field, ((BlueprintFeatureReference[])Get(school, field)).Where(r => r.Guid != root.AssetGuid).ToArray());
            var before = new[] { Get(school, "m_Features"), Get(school, "m_AllFeatures") };
            var ids = new HashSet<BlueprintGuid>(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>());
            bool rejected = false; failAppend = true;
            try { Register(cache); } catch (TargetInvocationException e) { Exception inner = e; while (inner is TargetInvocationException) inner = inner.InnerException; if (inner is InvalidOperationException && inner.Message == "Enhancement01430 append fault") rejected = true; else throw; }
            finally { failAppend = false; }
            Check(rejected, "fresh registration reaches injected append fault");
            Check(ReferenceEquals(opposition.ComponentsArray, nativeComponents) && (string)Call("Definition", (object)opposition.ComponentsArray) == nativeDefinition, "failed registration restores exact native opposition component array and values");
            Check(ReferenceEquals(Get(school, "m_Features"), before[0]) && ReferenceEquals(Get(school, "m_AllFeatures"), before[1]), "failed registration restores exact original pool arrays");
            Check(ids.SetEquals(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>()), "failed registration removes every newly published asset");
        }
        finally { failAppend = false; foreach (var bp in originalAssets) cache.AddCachedBlueprint(bp.AssetGuid, bp); Set(school, "m_Features", schoolBefore[0]); Set(school, "m_AllFeatures", schoolBefore[1]); opposition.ComponentsArray = nativeComponents; }
        Register(cache);
        var once = opposition.ComponentsArray;
        Register(cache);
        Check(ReferenceEquals(once, opposition.ComponentsArray), "repeat registration preserves reciprocal prerequisite array identity");
        Check(opposition.GetComponents<PrerequisiteNoFeature>().Count(p => p.Feature?.AssetGuid == root.AssetGuid) == 1, "one reciprocal prerequisite rejects Enhancement root");
        Check(opposition.GetComponents<PrerequisiteNoFeature>().Any(p => p.Feature?.AssetGuid.ToString() == SourceId), "native Transmutation prohibition retained");
        Check(nativeComponents.All(c => opposition.ComponentsArray.Contains(c)) && (string)Call("Definition", (object)nativeComponents) == nativeDefinition, "native opposition components and settings remain unchanged");
        Check(ReferenceEquals(Get(oppositionPool, "m_Features"), oppositionPoolBefore[0]) && ReferenceEquals(Get(oppositionPool, "m_AllFeatures"), oppositionPoolBefore[1]), "registration preserves every native opposition pool reference and array");
        foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Check(((BlueprintFeatureReference[])Get(school, field)).Select(r => r.Guid).SequenceEqual(((BlueprintFeatureReference[])schoolBefore[field == "m_Features" ? 0 : 1]).Select(r => r.Guid)), "native and third-party school pool membership/order preserved " + field);
        var foreign = new BlueprintFeature { name = "01430ForeignSchoolSelectionMember", AssetGuid = BlueprintGuid.Parse("922f00727a2a41c19f41effdd7c89f73d"), ComponentsArray = Array.Empty<BlueprintComponent>() }; foreign.OnEnable();
        try {
            foreach (var p in new[] { school, oppositionPool }) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Set(p, field, ((BlueprintFeatureReference[])Get(p, field)).Concat(new[] { Ref(foreign) }).ToArray());
            var expected = new[] { school, oppositionPool }.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            Register(cache); Register(cache);
            for (int i = 0; i < 2; i++) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Check(((BlueprintFeatureReference[])Get(i == 0 ? school : oppositionPool, field)).Select(r => r.Guid).SequenceEqual(((BlueprintFeatureReference[])expected[i][field == "m_Features" ? 0 : 1]).Select(r => r.Guid)), "third-party member/order retained after repeated registration " + i + field);
        }
        finally { Set(school, "m_Features", schoolBefore[0]); Set(school, "m_AllFeatures", schoolBefore[1]); Set(oppositionPool, "m_Features", oppositionPoolBefore[0]); Set(oppositionPool, "m_AllFeatures", oppositionPoolBefore[1]); }
        var native = (BlueprintProgression)cache.Load(BlueprintGuid.Parse(SourceId));
        foreach (var selected in new BlueprintFeature[] { root, native }) {
            var u = Unit(cache, "01430-school-" + selected.name, 1); var fact = (Feature)Call("Fact", u, selected);
            Check(!CanSelect(u, oppositionPool, opposition), selected.name + " rejects Transmutation opposition through native CanSelect");
            foreach (var legal in choices.Where(id => id != OppositionId).Select(id => (BlueprintFeature)cache.Load(BlueprintGuid.Parse(id)))) Check(CanSelect(u, oppositionPool, legal), selected.name + " retains legal opposition " + legal.name);
            Call("Remove", fact);
        }
        var inverse = Unit(cache, "01430-opposition-first", 1); var oppositeFact = (Feature)Call("Fact", inverse, opposition);
        Check(!CanSelect(inverse, school, root), "opposition first rejects Enhancement via native CanSelect");
        Check(!CanSelect(inverse, school, native), "opposition first still rejects native Transmutation via CanSelect");
        Call("Remove", oppositeFact);
        Check(CanSelect(inverse, school, root) && CanSelect(inverse, school, native), "both school roots selectable without conflicting opposition");
    }
    static bool CanSelect(UnitEntityData u, BlueprintFeatureSelection pool, BlueprintFeature candidate)
    {
        var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState));
        Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 1); state.NextClassLevel = 1; state.SelectedClass = u.Progression.Classes[0].CharacterClass;
        var selection = state.AddSelection(null, default(FeatureSource), pool, 1); selections++;
        return pool.CanSelect(u.Descriptor, state, selection, candidate);
    }
    static Buff Add(UnitEntityData u, BlueprintBuff bp, UnitEntityData caster, string id)
    {
        var context = new MechanicsContext(caster, u.Descriptor, bp); context.SetParams(new AbilityParams { CasterLevel = caster.Progression.Classes[0].Level });
        var buff = new Buff(bp, context, null); Set(buff, "UniqueId", id); lifecycleFacts.Add(buff);
        Check(ReferenceEquals(u.Facts.Add(buff), buff), "native manager adds distinct instance " + id);
        Check(buff.IsActive && buff.IsTurnedOn && buff.IsSubscribedOnEventBus, "native attach activates and subscribes " + id);
        return buff;
    }
    static void Armor(BlueprintsCache cache, bool reverse)
    {
        var u = Unit(cache, "01430-armor-" + reverse, 1);
        Set(u, "Buffs", u.Facts.EnsureFactProcessor<BuffCollection>());
        u.Facts.TurnOn();
        var weakCaster = Unit(cache, "01430-weak-" + reverse, 10); var strongCaster = Unit(cache, "01430-strong-" + reverse, 20);
        var bp = Asset<BlueprintBuff>(cache, "EnhancementAugmentACBuff");
        int baseline = u.Stats.AC.ModifiedValue;
        var unrelated = u.Stats.AC.AddModifier(7, (EntityFact)null, ModifierDescriptor.NaturalArmor);
        void ArmorIs(int bonus, string step) { Check(u.Stats.AC.ModifiedValue == baseline + 7 + bonus, (reverse ? "strong-first " : "weak-first ") + step + " expected " + (baseline + 7 + bonus) + " actual " + u.Stats.AC.ModifiedValue); Check(u.Stats.AC.Modifiers.Contains(unrelated), "unrelated natural armor preserved at " + step); Check(u.Stats.AC.Modifiers.Count(m => m.Source is Buff b && b.Blueprint.AssetGuid == bp.AssetGuid) == (bonus == 0 ? 0 : 1), "exactly one elected modifier at " + step); }
        var first = Add(u, bp, reverse ? strongCaster : weakCaster, "01430-first-" + reverse); ArmorIs(reverse ? 5 : 3, "first addition");
        var second = Add(u, bp, reverse ? weakCaster : strongCaster, "01430-second-" + reverse); ArmorIs(5, "second addition");
        var weak = reverse ? second : first; var strong = reverse ? first : second;
        for (int i = 0; i < 2; i++) { int eventCount = added + removed; u.Facts.TurnOff(); ArmorIs(0, "manager off " + i); Check(!weak.IsSubscribedOnEventBus && !strong.IsSubscribedOnEventBus, "manager off unsubscribes both effects"); u.Facts.TurnOn(); ArmorIs(5, "manager reenable " + i); Check(weak.IsSubscribedOnEventBus && strong.IsSubscribedOnEventBus, "manager reenable restores native subscriptions"); Check(eventCount == added + removed, "manager restart does not synthesize added/removed notifications"); }
        strong.TurnOff(); ArmorIs(3, "strong off restores weak"); strong.TurnOn(); ArmorIs(5, "strong on");
        weak.TurnOff(); ArmorIs(5, "weak off retains strong"); weak.TurnOn(); ArmorIs(5, "weak on");
        u.Facts.ApplyPostLoadFixes(); ArmorIs(5, "native post-load component fixes");
        int beforeActivationEvents = added + removed;
        strong.Deactivate(); ArmorIs(3, "strong deactivation restores weak"); strong.Activate(); ArmorIs(5, "strong reactivation");
        weak.Deactivate(); ArmorIs(5, "weak deactivation retains strong"); weak.Activate(); ArmorIs(5, "weak reactivation");
        Check(beforeActivationEvents == added + removed, "native deactivate/reactivate requires no artificial buff events");
        var equal = Add(u, bp, strongCaster, "01430-equal-" + reverse); ArmorIs(5, "equal strength addition");
        u.Facts.Remove(strong); ArmorIs(5, "strong removed retains equal"); Check(strong.IsDisposed && strong.Manager == null && !strong.IsSubscribedOnEventBus, "native removal detaches/disposes/unsubscribes strong");
        u.Facts.Remove(equal); ArmorIs(3, "last strong removed restores weak");
        u.Facts.Remove(weak); ArmorIs(0, "last removal"); Check(u.Buffs.Enumerable.All(b => b.Blueprint.AssetGuid != bp.AssetGuid), "native manager/cache contains no removed augmentation");
        u.Stats.AC.RemoveModifier(unrelated); u.Facts.TurnOff();
    }
}
