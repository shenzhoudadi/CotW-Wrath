using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual.Animation.Kingmaker;
using UnityEngine;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

// Native managers, fact/runtime activation, AddFacts ownership, resources,
// properties, stat modifiers, selection and UnitCommand.Tick remain execution
// subjects. Incoming hit outcomes and Unity animation/scene endpoints are inputs.
internal static class AngerInquisition01431Tests
{
    static readonly string[] ClassIds = { "67819271767a9dd4fbfd4ae700befea0", "f1a70d9e1b0b41e49874e1fa9052a1ce" };
    static readonly string[] PoolIds = { "48525e5da45c9c243a343fc6545dbdb9", "43281c3d7fe18cc4d91928395837cd1e" };
    static readonly string[] DeityIds = { "8f49a5d8528a82c44b8c117a89f6b68c", "04bc2b62273ab744092d992ed72bff41" };
    static readonly string[] Tokens = { "AngerHatefulRetortUses", "AngerHatefulRetortBuff", "AngerHatefulRetortToggle", "AngerHatefulRetortFeature", "AngerDivineRageUses", "AngerRageHitPointsProperty", "AngerDivineRageBuff", "AngerDivineRageToggle", "AngerDivineRageFeature", "AngerInquisition", "FerociousBeastDivineBaseRage" };
    static Type Module => typeof(AngerHatefulRetort01431).Assembly.GetType("CallOfTheWild.Wrath.Prototype.AngerInquisition01431", true);
    static Type ReactionModule => Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkRangedReaction", true);
    static int checks, serial, selectCalls, resourceSpends, nativeRoundCalls, nativeOns, nativeOffs, added, removed, animations, starts, ticks, fxBoundaryCalls, actionFxBoundaryCalls;
    static bool failAppend, turnBased, mayAttack, mayAct, ally, render, recursive;
    static UnitEntityData owner, enemy;
    static Buff consent, counterConsent;
    static UnitEntityView view;
    static UnitAnimationManager animationManager;
    static UnitAnimationActionHandle animation;
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static readonly List<RuleAttackWithWeapon> reactions = new List<RuleAttackWithWeapon>();
    static readonly List<BlueprintScriptableObject> assets = new List<BlueprintScriptableObject>();
    static object Get(object obj, string field) => (AccessTools.Field(obj.GetType(), field) ?? AccessTools.Field(obj.GetType(), "<" + field + ">k__BackingField")).GetValue(obj);
    static void Set(object obj, string field, object value) => (AccessTools.Field(obj.GetType(), field) ?? AccessTools.Field(obj.GetType(), "<" + field + ">k__BackingField")).SetValue(obj, value);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Helper(string method, params object[] args) => AccessTools.Method(typeof(Migration01426Tests), method).Invoke(null, args);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Helper("Id", token);
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static T Native<T>(BlueprintsCache cache, string id) where T : SimpleBlueprint => (T)cache.Load(BlueprintGuid.Parse(id));
    static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null); return r; }
    static void Check(bool condition, string message) { if (!condition) throw new Exception("Anger01431: " + message); checks++; }
    static void Register(BlueprintsCache cache) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache });
    static string Definition(object value) => (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition").Invoke(null, new[] { value });
    static bool Skip() => false;
    static bool ParticleSceneBoundary() { fxBoundaryCalls++; return false; }
    static bool ActionFxSceneBoundary() { actionFxBoundaryCalls++; return false; }
    static bool ExposeActionFailure(Exception __1) {
        var original = __1;
        while (original is Kingmaker.ElementsSystem.ElementLogicException && original.InnerException != null) original = original.InnerException;
        Console.WriteLine("Anger01431 actual ActionList failure (QA report wrapper " + __1.GetType().FullName + "): " + original);
        ExceptionDispatchInfo.Capture(original).Throw(); return false;
    }
    static bool False(ref bool __result) { __result = false; return false; }
    static bool True(ref bool __result) { __result = true; return false; }
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool Time(ref TimeSpan __result) { __result = TimeSpan.FromSeconds(100); return false; }
    static bool Delta(ref float __result) { __result = 0; return false; }
    static bool Mode(ref bool __result) { __result = turnBased; return false; }
    static bool CanAttack(ref bool __result) { __result = mayAttack; return false; }
    static bool CanAct(ref bool __result) { __result = mayAct; return false; }
    static bool Ally(UnitEntityData __instance, UnitEntityData __0, ref bool __result) { __result = ally || ReferenceEquals(__instance, __0); return false; }
    static bool Hand(UnitEntityData __0, ref WeaponSlot __result) { __result = __0.Body.PrimaryHand; return false; }
    static bool Range(ref float? __result) { __result = 1.524f; return false; }
    static bool Reach(UnitEntityData __0, UnitEntityData __1, float __2, ref bool __result) { __result = (Position(__0) - Position(__1)).sqrMagnitude <= __2 * __2; return false; }
    static Vector3 Position(UnitEntityData unit) => (Vector3)Get(unit, "m_Position");
    static void NativeIdentity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "anger031-fact-" + ++serial); }
    static void FactOn(EntityFact __instance) { if (units.Contains(__instance.Owner as UnitEntityData)) nativeOns++; }
    static void FactOff(EntityFact __instance) { if (units.Contains(__instance.Owner as UnitEntityData)) nativeOffs++; }
    static void Spend(UnitAbilityResourceCollection __instance, BlueprintScriptableObject __0, int __1) { if (__0?.AssetGuid == Id("AngerHatefulRetortUses") || __0?.AssetGuid == Id("AngerDivineRageUses")) resourceSpends++; }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == Id("AngerInquisition"))) throw new InvalidOperationException("031 Anger append fault"); }
    static bool View(UnitEntityData __instance, ref UnitEntityView __result) { __result = render && __instance == owner ? view : null; return false; }
    static bool Manager(ref UnitAnimationManager __result) { __result = animationManager; return false; }
    static bool NotEqual(UnityEngine.Object __0, UnityEngine.Object __1, ref bool __result) { if (!ReferenceEquals(__0, animationManager) && !ReferenceEquals(__1, animationManager)) return true; __result = !ReferenceEquals(__0, __1); return false; }
    static bool CreateAnimation(AttackHandInfo __instance) {
        animations++; Check(__instance.Hand.Weapon.Blueprint.IsMelee && __instance.Target == enemy, "animation input uses equipped melee weapon and correct attacker");
        animation = Empty<UnitAnimationActionHandle>(); Set(__instance, "AnimationHandle", animation); return false;
    }
    static bool StartAnimation(UnitCommand __instance, UnitAnimationActionHandle __0) { starts++; Set(__instance, "Animation", __0); Set(__instance, "HasAnimation", true); return false; }
    static bool CaptureReaction(RuleAttackWithWeapon __0, ref RuleAttackWithWeapon __result) {
        Check(__0.Initiator == owner && __0.Target == enemy && __0.Weapon == owner.Body.PrimaryHand.Weapon, "action frame constructs exact owner/target/equipped weapon rule");
        Check(__0.ExtraAttack && __0.IsFirstAttack && __0.AttacksCount == 1 && __0.AttackBonusPenalty == 0 && !__0.IsAttackOfOpportunity && !__0.SkipAnimation, "one highest-BAB normal animated melee rule with no AoO allowance");
        Check((bool)AccessTools.Method(ReactionModule, "IsReaction").Invoke(null, new object[] { __0 }), "generated rule carries existing reaction recursion marker");
        reactions.Add(__0); Set(__0, "AttackRoll", Incoming(owner, enemy, __0.Weapon).AttackRoll);
        if (recursive) { int queued = enemy.Commands.Queue.Count; var resource = ((AngerHatefulRetort01431)counterConsent.Components.Single(r => r.SourceBlueprintComponent is AngerHatefulRetort01431).SourceBlueprintComponent).Resource.Get(); int uses = enemy.Resources.GetResourceAmount(resource); Dispatch(counterConsent, __0); Check(enemy.Commands.Queue.Count == queued && enemy.Resources.GetResourceAmount(resource) == uses, "enemy consenting retort rejects a real hit originating from current retort command"); }
        __result = __0; return false;
    }
    sealed class Observer : IUnitBuffHandler {
        public void HandleBuffDidAdded(Buff buff) { if (units.Contains(buff.Owner)) added++; }
        public void HandleBuffDidRemoved(Buff buff) { if (units.Contains(buff.Owner)) removed++; }
    }
    static UnitEntityData Unit(BlueprintsCache cache, string name, string classId = null, int level = 6) {
        var unit = (UnitEntityData)Helper("Unit", cache, "anger031-" + name);
        unit.Progression.Classes.Clear(); unit.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, classId ?? ClassIds[0])) { Level = level });
        var neutral = new BlueprintUnit { name = "anger031-neutral-" + name, AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = Array.Empty<BlueprintComponent>() }; neutral.OnEnable();
        Set(unit.Descriptor, "Blueprint", neutral); Set(unit.Descriptor, "OriginalBlueprint", neutral);
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        Check(unit.UISettings.Owner == unit.Descriptor, "native UnitUISettings constructor binds collection detach action-bar owner");
        Set(unit, "Buffs", unit.Facts.EnsureFactProcessor<BuffCollection>()); Set(unit.Progression, "Features", unit.Facts.EnsureFactProcessor<FeatureCollection>());
        Set(unit, "ActivatableAbilities", unit.Facts.EnsureFactProcessor<ActivatableAbilityCollection>()); Set(unit, "Abilities", unit.Facts.EnsureFactProcessor<AbilityCollection>());
        foreach (var field in unit.State.Features.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) if (field.FieldType == typeof(CountableFlag) && field.GetValue(unit.State.Features) == null) field.SetValue(unit.State.Features, new CountableFlag());
        foreach (string fieldName in new[] { "m_ConditionsExceptions", "m_StatusEffects" }) { var field = AccessTools.Field(typeof(UnitState), fieldName); if (field.GetValue(unit.State) == null) field.SetValue(unit.State, field.FieldType.IsArray ? Array.CreateInstance(field.FieldType.GetElementType(), Enum.GetValues(typeof(UnitCondition)).Cast<UnitCondition>().Max(c => (int)c) + 1) : Activator.CreateInstance(field.FieldType)); }
        unit.Stats.Wisdom.BaseValue = 18; unit.Stats.HitPoints.BaseValue = 100; unit.Stats.BaseAttackBonus.BaseValue = 8; Set(unit, "m_Position", Vector3.zero);
        units.Add(unit); unit.Facts.TurnOn(); return unit;
    }
    static Feature Add(UnitEntityData unit, BlueprintFeature blueprint) {
        if (blueprint.GetComponents<AddFacts>().Any()) Console.WriteLine("Anger01431 native grant: " + blueprint.name + " to " + unit.UniqueId + "; referenced facts=" + string.Join(",", blueprint.GetComponents<AddFacts>().SelectMany(a => (BlueprintUnitFactReference[])Get(a, "m_Facts")).Select(r => r.Guid + ":" + (r.Get()?.name ?? "MISSING"))));
        var fact = (Feature)blueprint.CreateFact(null, unit.Descriptor, null); var actual = unit.Facts.Add(fact);
        Check(ReferenceEquals(actual, fact) && fact.IsActive && fact.IsTurnedOn, "native Feature CreateFact/manager Add activates " + blueprint.name); return fact;
    }
    static Buff Add(UnitEntityData unit, BlueprintBuff blueprint) {
        var buff = new Buff(blueprint, new MechanicsContext(unit, unit.Descriptor, blueprint), null); Set(buff, "UniqueId", "anger031-buff-" + ++serial);
        Check(ReferenceEquals(unit.Facts.Add(buff), buff) && buff.IsActive && buff.IsTurnedOn && buff.IsSubscribedOnEventBus, "native Buff Add attaches and subscribes " + blueprint.name); return buff;
    }
    static void Remove(EntityFact fact) { var manager = fact.Manager; if (manager != null) { manager.Remove(fact); Check(fact.IsDisposed && fact.Manager == null, "native Remove disposes/detaches " + fact.Blueprint.name); } }
    static void Life(UnitEntityData unit, bool dead) {
        var next = dead ? UnitLifeState.Dead : UnitLifeState.Conscious; var previous = unit.State.LifeState;
        Check(unit.State.UpdateLifeState(next) == (previous != next), "native UpdateLifeState reports the actual transition");
        Check(unit.State.LifeState == next && unit.State.IsDead == dead, "native LifeState/IsDead agrees with death or living input");
    }
    static bool CanSelect(UnitEntityData unit, BlueprintFeatureSelection pool, BlueprintFeature feature) {
        var state = Empty<LevelUpState>(); Set(state, "m_Unit", unit); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", unit.Progression.Classes[0].Level); state.NextClassLevel = unit.Progression.Classes[0].Level; state.SelectedClass = unit.Progression.Classes[0].CharacterClass;
        var pick = state.AddSelection(null, default(FeatureSource), pool, 1); selectCalls++; return pool.CanSelect(unit.Descriptor, state, pick, feature);
    }
    static ItemEntityWeapon Weapon(UnitEntityData unit, bool ranged, string name) => (ItemEntityWeapon)AccessTools.Method(typeof(TeamworkFeatTests), "Weapon").Invoke(null, new object[] { unit, ranged, "anger031-" + name });
    static void Equip(UnitEntityData unit, ItemEntityWeapon weapon) => AccessTools.Method(typeof(WildShapeFeatTests), "Equip").Invoke(null, new object[] { unit.Body.PrimaryHand, weapon });
    static RuleAttackWithWeapon Incoming(UnitEntityData attacker, UnitEntityData target, ItemEntityWeapon weapon, bool hit = true, bool fake = false) {
        var attack = Empty<RuleAttackWithWeapon>(); Set(attack, "Initiator", attacker); Set(attack, "Target", target); Set(attack, "Weapon", weapon); Set(attack, "AttacksCount", 1);
        var roll = Empty<RuleAttackRoll>(); Set(roll, "Initiator", attacker); Set(roll, "Target", target); Set(roll, "Result", hit ? AttackResult.Hit : AttackResult.Miss); Set(roll, "IsFake", fake); Set(attack, "AttackRoll", roll);
        Check(roll.IsHit == hit, "native IsHit resolves fixture Hit/Miss result"); return attack;
    }
    static void Dispatch(Buff buff, RuleAttackWithWeapon attack) {
        var runtime = buff.Components.Single(r => r.SourceBlueprintComponent is AngerHatefulRetort01431);
        using (runtime.RequestEventContext()) ((AngerHatefulRetort01431)runtime.SourceBlueprintComponent).OnEventDidTrigger(attack);
    }
    static void LifecycleIntegrity() {
        foreach (var type in new[] { typeof(EntityFactsManager), typeof(EntityFact), typeof(EntityFactComponent) }) foreach (string name in (type == typeof(EntityFact) ? new[] { "Attach", "Detach", "Activate", "Deactivate", "TurnOn", "TurnOff" } : type == typeof(EntityFactsManager) ? new[] { "Add", "Remove", "TurnOn", "TurnOff" } : new[] { "TurnOn", "TurnOff" })) {
            var patch = Harmony.GetPatchInfo(AccessTools.Method(type, name));
            Check(patch == null || patch.Transpilers.Count == 0 && patch.Prefixes.All(p => p.PatchMethod == AccessTools.Method(typeof(AngerInquisition01431Tests), nameof(NativeIdentity))), "key native lifecycle is not replaced: " + type.Name + "." + name);
        }
        var spend = Harmony.GetPatchInfo(AccessTools.Method(typeof(UnitAbilityResourceCollection), "Spend")); Check(spend == null || spend.Prefixes.Count == 0 && spend.Transpilers.Count == 0, "native resource Spend is original");
    }
    static void Registration(BlueprintsCache cache, Harmony harmony) {
        // The shared cache's class instance may have been hydrated by earlier
        // suites. Restore the actual authored archetype GUID list and bind only
        // its nonserialized parent cache; preserve production validation.
        var blood = Native<BlueprintCharacterClass>(cache, "d77e67a814d686842802c9cfd8ef8499");
        var primal = Native<BlueprintArchetype>(cache, "4d588290ed0736e4f8cd3f4c3beacd69");
        var bloodData = PsychicExpansionTests.NativeRows.Single(row => (string)row["blueprint"]["AssetId"] == blood.AssetGuid.ToString())["blueprint"]["Data"];
        Set(blood, "m_Archetypes", PsychicExpansionTests.Read(bloodData["m_Archetypes"], typeof(BlueprintArchetypeReference[])));
        Set(primal, "m_ParentClass", blood);
        Check(blood.Archetypes.Contains(primal) && primal.GetParentClass() == blood, "actual Primalist GUID membership and runtime Bloodrager parent cache agree");
        var rage = Native<BlueprintBuff>(cache, "da8ce41ac3cd74742b80984ccc3c9613"); var toggle = Native<BlueprintActivatableAbility>(cache, "df6a2cce8e3a9bd4592fb1968b83f730");
        IndependentRegistration(cache);
        AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.FerociousBeast01431"), "Register").Invoke(null, new object[] { cache });
        Register(cache);
        string rageDefinition = Definition(rage.ComponentsArray), toggleDefinition = Definition(toggle.ComponentsArray);
        Register(cache); assets.AddRange(Tokens.Select(token => (BlueprintScriptableObject)cache.Load(Id(token)))); Check(assets.All(a => a != null) && assets.Select(a => a.AssetGuid).Distinct().Count() == Tokens.Length, "all Anger assets exist with unique stable identities");
        var root = Asset<BlueprintProgression>(cache, "AngerInquisition"); var pools = PoolIds.Select(id => Native<BlueprintFeatureSelection>(cache, id)).ToArray();
        var arrays = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        harmony.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(AngerInquisition01431Tests), nameof(AppendFault)));
        try {
            foreach (var asset in assets) cache.RemoveCachedBlueprint(asset.AssetGuid);
            foreach (var pool in pools) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(pool, field, ((BlueprintFeatureReference[])Get(pool, field)).Where(r => r.Guid != root.AssetGuid).ToArray());
            var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray(); var keys = new HashSet<BlueprintGuid>(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>());
            bool rejected = false; failAppend = true;
            try { Register(cache); } catch (Exception error) { while (error is TargetInvocationException) error = error.InnerException; if (!(error is InvalidOperationException) || error.Message != "031 Anger append fault") throw; rejected = true; }
            finally { failAppend = false; }
            Check(rejected && keys.SetEquals(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>()), "fresh native publication/append fault rolls back every Anger asset");
            for (int i = 0; i < pools.Length; i++) for (int j = 0; j < 2; j++) Check(ReferenceEquals(Get(pools[i], j == 0 ? "m_Features" : "m_AllFeatures"), before[i][j]), "append rollback restores exact original pool array");
        } finally { failAppend = false; foreach (var asset in assets) cache.AddCachedBlueprint(asset.AssetGuid, asset); for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", arrays[i][0]); Set(pools[i], "m_AllFeatures", arrays[i][1]); } }
        Register(cache); Register(cache);
        foreach (var asset in assets) Check(ReferenceEquals(cache.Load(asset.AssetGuid), asset), "repeat registration preserves identity " + asset.name);
        foreach (var pool in pools) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Check(((BlueprintFeatureReference[])Get(pool, field)).Count(r => r.Guid == root.AssetGuid) == 1, "Anger occurs once in native domain selection " + pool.name);
        Check(rageDefinition == Definition(rage.ComponentsArray) && toggleDefinition == Definition(toggle.ComponentsArray), "original rage buff/toggle definitions untouched");
        var copy = Asset<BlueprintBuff>(cache, "AngerDivineRageBuff");
        var originalRetained = rage.ComponentsArray.Where(c => !(c is TemporaryHitPointsPerLevel) && !(c is FerociousBeastHost01431)).ToArray();
        var copiedRetained = copy.ComponentsArray.Where(c => !(c is TemporaryHitPointsFromAbilityValue) && !(c is FerociousBeastHost01431)).ToArray();
        Check(Definition(originalRetained) == Definition(copiedRetained) && originalRetained.Zip(copiedRetained, (a, b) => !ReferenceEquals(a, b)).All(v => v), "native rage effects retained as independent deep copies; only temporary HP source replaced");
        Check(!copy.ComponentsArray.Any(c => c is TemporaryHitPointsPerLevel) && copy.GetComponents<TemporaryHitPointsFromAbilityValue>().Count() == 1, "Divine Anger has exactly one custom-property native temporary-HP component");
        var driver = copy.GetComponent<FerociousBeastHost01431>(); Check(driver != null && driver.Mode == 3 && driver.SourceHost.Get() == copy && driver.Resource.Get() == Asset<BlueprintAbilityResource>(cache, "AngerDivineRageUses") && driver.BaseRage.Get() == Asset<BlueprintBuff>(cache, "FerociousBeastDivineBaseRage"), "Divine Anger personal companion driver binds only its private source/resource/effect");
        Check(root.LevelEntries.Select(e => e.Level).SequenceEqual(new[] { 1, 6, 14, 20 }) && root.GiveFeaturesForPreviousLevels, "native progression grant levels1/6/14/20 and retrospective grants");
        Check(root.LevelEntries.Single(e => e.Level == 14).Features.Single().AssetGuid.ToString() == "ce49c579fe0bcc647a32c96929fae982" && root.LevelEntries.Single(e => e.Level == 20).Features.Single().AssetGuid.ToString() == "ca9343d75a83a2745a22fa11c383153a", "native greater/tireless identities retained");
        Check(root.ComponentsArray.All(c => c is Prerequisite) && root.LevelEntries.SelectMany(e => e.Features).All(f => !f.ComponentsArray.Any(c => c.GetType().Name.Contains("SpellList") || c.GetType().Name.Contains("Spellbook"))), "Anger grants no domain spells/slots");
    }
    static void IndependentRegistration(BlueprintsCache parent) {
        // A previously published Anger driver can legitimately retain a resolved
        // BasePower.Cached reference after a test removes that root from the cache.
        // Use a genuinely fresh graph, rather than clearing a live reference or
        // mistaking the preceding Ferocious suite's publication for first startup.
        var loaded = ((IDictionary)Get(parent, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray().Select(parent.Load).ToArray();
        var angerIds = Tokens.Select(Id).ToHashSet();
        var excluded = loaded.Where(bp => angerIds.Contains(bp.AssetGuid) || bp.name.StartsWith("CotWWrathPsychicFerociousBeast", StringComparison.Ordinal)).Select(bp => bp.AssetGuid).ToHashSet();
        var hosts = new[] { "da8ce41ac3cd74742b80984ccc3c9613", "3513326cd64f475781799685c57fa452", "ecc22ca1eea1bf6488a0d7c6ee2527d8" }.Select(BlueprintGuid.Parse).ToHashSet();
        var cache = new BlueprintsCache();
        foreach (var bp in loaded.Where(bp => !excluded.Contains(bp.AssetGuid))) {
            SimpleBlueprint copy = bp;
            if (hosts.Contains(bp.AssetGuid)) {
                copy = (SimpleBlueprint)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(bp, null);
                var components = ((BlueprintScriptableObject)bp).ComponentsArray.Where(c => !(c is FerociousBeastHost01431)).ToArray();
                ((BlueprintScriptableObject)copy).ComponentsArray = (BlueprintComponent[])AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "CopyData").MakeGenericMethod(typeof(BlueprintComponent[])).Invoke(null, new object[] { components });
            }
            if (bp is BlueprintFeatureSelection selection) {
                copy = (SimpleBlueprint)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(bp, null);
                foreach (string field in new[] { "m_Features", "m_AllFeatures" })
                    Set(copy, field, (((BlueprintFeatureReference[])Get(selection, field)) ?? Array.Empty<BlueprintFeatureReference>()).Where(r => r == null || !excluded.Contains(r.Guid)).ToArray());
            }
            cache.AddCachedBlueprint(copy.AssetGuid, copy);
        }
        var previousCache = ResourcesLibrary.BlueprintsCache;
        try {
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
            var ferociousId = Id("FerociousBeastFeature");
            Check(angerIds.All(id => cache.Load(id) == null) && cache.Load(ferociousId) == null && !Native<BlueprintBuff>(cache, "da8ce41ac3cd74742b80984ccc3c9613").GetComponents<FerociousBeastHost01431>().Any(), "isolated startup has no Anger assets, Ferocious assets or prior observer");
            Register(cache);
            var root = Asset<BlueprintProgression>(cache, "AngerInquisition"); var rage = Asset<BlueprintBuff>(cache, "AngerDivineRageBuff");
            Check(root != null && rage != null && PoolIds.All(id => ((BlueprintFeatureReference[])Get(Native<BlueprintFeatureSelection>(cache, id), "m_AllFeatures")).Any(r => r.Guid == root.AssetGuid)), "Anger native core/cache/domain pools register independently after Ferocious failure");
            var driver = rage.GetComponent<FerociousBeastHost01431>();
            Check(driver != null && driver.BasePower.Guid == ferociousId && driver.BasePower.Get() == null, "standalone bridge keeps stable deferred Ferocious root without inventing an unresolved donor");
            var definition = Definition(rage.ComponentsArray);
            AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.FerociousBeast01431"), "Register").Invoke(null, new object[] { cache });
            Register(cache);
            Check(ReferenceEquals(root, Asset<BlueprintProgression>(cache, "AngerInquisition")) && ReferenceEquals(rage, Asset<BlueprintBuff>(cache, "AngerDivineRageBuff")) && definition == Definition(rage.ComponentsArray), "later Ferocious publication and Anger retry preserve independently published identities and definitions");
            Check(driver.BasePower.Get() == Asset<BlueprintFeature>(cache, "FerociousBeastFeature"), "deferred bridge resolves the actual late-published Ferocious root");
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, previousCache); }
    }
    static void SelectionAndAmounts(BlueprintsCache cache) {
        var root = Asset<BlueprintProgression>(cache, "AngerInquisition"); var rageUses = Asset<BlueprintAbilityResource>(cache, "AngerDivineRageUses"); var retortUses = Asset<BlueprintAbilityResource>(cache, "AngerHatefulRetortUses"); var property = Asset<BlueprintUnitProperty>(cache, "AngerRageHitPointsProperty");
        foreach (string classId in ClassIds) foreach (int level in new[] { 1, 5, 6, 13, 14, 19, 20 }) {
            var unit = Unit(cache, classId + "-" + level, classId, level); Check(root.CalcLevel(unit.Descriptor) == level, "native Anger progression uses selected cleric/inquisitor class level");
            foreach (int wisdom in new[] { 6, 10, 18, 24 }) { unit.Stats.Wisdom.BaseValue = wisdom; Check(rageUses.GetMaxAmount(unit.Descriptor) == Math.Max(0, level - 4 + (wisdom - 10) / 2), "native Divine Anger maximum level-4+Wisdom modifier floor0"); Check(retortUses.GetMaxAmount(unit.Descriptor) == 1, "native Hateful Retort maximum one independent of level/Wisdom"); }
            unit.Stats.Wisdom.BaseValue = 18;
            Check(property.GetInt(unit) == Math.Max(1, level - 3) * 2, "native custom property effective level minus3, basic rage HP");
            var greater = Add(unit, Native<BlueprintFeature>(cache, "ce49c579fe0bcc647a32c96929fae982")); Check(property.GetInt(unit) == Math.Max(1, level - 3) * 3, "native property greater-rage multiplier");
            var mighty = Add(unit, Native<BlueprintFeature>(cache, "06a7e5b60020ad947aed107d82d1f897")); Check(property.GetInt(unit) == Math.Max(1, level - 3) * 4, "native property preserves previously learned mighty-rage improvement"); Remove(mighty); Remove(greater);
        }
        var multiclass = Unit(cache, "multiclass", ClassIds[0], 4); multiclass.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, ClassIds[1])) { Level = 5 }); multiclass.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, "ba34257984f4c41408ce1dc2004e342e")) { Level = 10 });
        Check(root.CalcLevel(multiclass.Descriptor) == 9 && rageUses.GetMaxAmount(multiclass.Descriptor) == 9 && property.GetInt(multiclass) == 12, "native cleric/inquisitor sums exclude unrelated wizard levels");
        var limitlessUnit = Unit(cache, "limitless-property", level: 20); var rageGrant = Add(limitlessUnit, Asset<BlueprintFeature>(cache, "AngerDivineRageFeature"));
        var limitless = Add(limitlessUnit, Native<BlueprintFeature>(cache, "5cb58e6e406525342842a073fb70d068"));
        Check(property.GetInt(limitlessUnit) == 34 + limitlessUnit.Resources.GetResourceAmount(rageUses) / 2, "native property retains Limitless Rage bonus using private Divine Anger resource"); limitlessUnit.Resources.Spend(rageUses, 3);
        Check(property.GetInt(limitlessUnit) == 34 + limitlessUnit.Resources.GetResourceAmount(rageUses) / 2, "Limitless property uses actual remaining private rounds with native integer division"); Remove(limitless); Check(property.GetInt(limitlessUnit) == 34, "removing native Limitless fact removes only its HP contribution"); Remove(rageGrant);
        foreach (string poolId in PoolIds) foreach (string classId in ClassIds) foreach (string deityId in DeityIds) {
            var unit = Unit(cache, "select-" + poolId + classId + deityId, classId, 1); var pool = Native<BlueprintFeatureSelection>(cache, poolId);
            Check(!CanSelect(unit, pool, root), "native CanSelect rejects absent permitted deity");
            // The selected deity is an eligibility input. Native HasFact uses
            // the actual blueprint instance, so a same-GUID replacement is not
            // sufficient. Isolate its unrelated weapon/domain/equipment grant
            // tree on that cached object, then restore the exact component array.
            // Anger's own AddFacts and all fact lifecycle methods stay native.
            var deitySource = Native<BlueprintFeature>(cache, deityId);
            var deityComponents = deitySource.ComponentsArray; var deityDefinition = Definition(deitySource);
            try {
                deitySource.ComponentsArray = deityComponents.Where(c => c is Prerequisite).ToArray();
                var deity = Add(unit, deitySource);
                Check(ReferenceEquals(deity.Blueprint, deitySource) && unit.HasFact(deitySource), "native selected-deity fact retains the exact cached blueprint identity");
                Check(CanSelect(unit, pool, root), "native CanSelect accepts Gorum/Rovagug in both domain pools/classes");
                var own = Add(unit, root); Check(!CanSelect(unit, pool, root), "native selection rejects repeated Anger root"); Remove(own); Check(CanSelect(unit, pool, root), "native root removal restores choice"); Remove(deity); Check(!CanSelect(unit, pool, root), "removing deity restores rejection");
            } finally { deitySource.ComponentsArray = deityComponents; }
            Check(ReferenceEquals(deitySource.ComponentsArray, deityComponents) && Definition(deitySource) == deityDefinition && ReferenceEquals(cache.Load(deitySource.AssetGuid), deitySource), "selection fixture restores exact native deity components, definition and cache identity");
        }
    }
    static void GrantsAndRage(BlueprintsCache cache) {
        var retortFeature = Asset<BlueprintFeature>(cache, "AngerHatefulRetortFeature"); var rageFeature = Asset<BlueprintFeature>(cache, "AngerDivineRageFeature"); var retortUses = Asset<BlueprintAbilityResource>(cache, "AngerHatefulRetortUses"); var rageUses = Asset<BlueprintAbilityResource>(cache, "AngerDivineRageUses");
        var retortToggle = Asset<BlueprintActivatableAbility>(cache, "AngerHatefulRetortToggle"); var rageToggle = Asset<BlueprintActivatableAbility>(cache, "AngerDivineRageToggle");
        var separate = Unit(cache, "separate-rage-pools"); separate.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, "f7d7eb166b3dd594fb330d085df41853")) { Level = 10 });
        var ordinaryUses = Native<BlueprintAbilityResource>(cache, "24353fcf8096ea54684a72bf58dedbc9");
        var resourceRow = PsychicExpansionTests.NativeRows.Single(row => (string)row["blueprint"]["AssetId"] == ordinaryUses.AssetGuid.ToString())["blueprint"]["Data"];
        var amountField = AccessTools.Field(typeof(BlueprintAbilityResource), "m_MaxAmount"); amountField.SetValue(ordinaryUses, PsychicExpansionTests.Read(resourceRow["m_MaxAmount"], amountField.FieldType));
        foreach (string fieldName in new[] { "m_Min", "m_Max", "m_UseMax", "IsDomainAbility" }) { var field = AccessTools.Field(typeof(BlueprintAbilityResource), fieldName); if (field != null && resourceRow[fieldName] != null) field.SetValue(ordinaryUses, PsychicExpansionTests.Read(resourceRow[fieldName], field.FieldType)); }
        separate.Resources.Add(ordinaryUses, true); Check(separate.Resources.GetResourceAmount(ordinaryUses) > 0, "ordinary native barbarian pool is available as an independent control"); separate.Resources.Spend(ordinaryUses, 1); int ordinaryAmount = separate.Resources.GetResourceAmount(ordinaryUses);
        var ownRage = Add(separate, rageFeature); Check(separate.Resources.GetResourceAmount(ordinaryUses) == ordinaryAmount && separate.Resources.GetResourceAmount(rageUses) == 6, "native Divine Anger grant does not refill ordinary rage or count barbarian levels");
        var toggleFact = ownRage.Components.Single(r => r.SourceBlueprintComponent is AddFacts).GetData<AddFactsData>().AppliedFacts.OfType<ActivatableAbility>().Single(); var spendRuntime = toggleFact.Components.Single(r => r.SourceBlueprintComponent is ActivatableAbilityResourceLogic); var spendLogic = (ActivatableAbilityResourceLogic)spendRuntime.SourceBlueprintComponent;
        Check(spendLogic.SpendType == ActivatableAbilityResourceLogic.ResourceSpendType.NewRound && spendLogic.RequiredResource == rageUses && spendLogic.FreeBlueprint?.AssetGuid.ToString() == "5cb58e6e406525342842a073fb70d068", "actual native round resource logic binds Divine pool and retains Limitless marker");
        using (spendRuntime.RequestEventContext()) ((IActivatableAbilitySpendResourceLogic)spendLogic).OnNewRound(); nativeRoundCalls++;
        Check(separate.Resources.GetResourceAmount(rageUses) == 5 && separate.Resources.GetResourceAmount(ordinaryUses) == ordinaryAmount, "actual native OnNewRound pays one Divine round and preserves ordinary rage amount");
        var freeRage = Add(separate, Native<BlueprintFeature>(cache, "5cb58e6e406525342842a073fb70d068")); int freeOrdinary = separate.Resources.GetResourceAmount(ordinaryUses);
        using (spendRuntime.RequestEventContext()) ((IActivatableAbilitySpendResourceLogic)spendLogic).OnNewRound(); nativeRoundCalls++;
        Check(separate.Resources.GetResourceAmount(rageUses) == 5 && separate.Resources.GetResourceAmount(ordinaryUses) == freeOrdinary, "original native free-resource callback preserves both pools with Limitless fact"); Remove(freeRage);
        separate.Resources.Spend(rageUses, 1); Check(separate.Resources.GetResourceAmount(ordinaryUses) == ordinaryAmount, "private Divine Anger Spend preserves ordinary rage amount"); Remove(ownRage); Check(separate.Resources.ContainsResource(ordinaryUses) && separate.Resources.GetResourceAmount(ordinaryUses) == ordinaryAmount, "source removal retains ordinary rage pool");
        foreach (bool foreign in new[] { false, true }) {
            var unit = Unit(cache, "grant-" + foreign, level: 20); EntityFact existing = foreign ? unit.Facts.Add(retortToggle.CreateFact(null, unit.Descriptor, null)) : null;
            var retort = Add(unit, retortFeature); var granted = retort.Components.Single(r => r.SourceBlueprintComponent is AddFacts).GetData<AddFactsData>().AppliedFacts;
            Check(unit.HasFact(retortToggle) && unit.Resources.GetResourceAmount(retortUses) == 1, "native AddFacts/AddAbilityResources grant retort toggle and one use");
            Check(granted.Single().Blueprint == retortToggle && (!foreign || granted.Single() != existing), "native AddFacts tracks one separate source-owned toggle");
            var rage = Add(unit, rageFeature); Check(unit.HasFact(rageToggle) && unit.Resources.GetResourceAmount(rageUses) == 20, "native Divine Anger feature grants distinct toggle and pool");
            // Native AddFacts tracks its own applied fact. The active foreign
            // toggle remains a separate fact; the native collection only reuses
            // a matching fact when it was temporarily disabled by action-bar
            // removal. A manager restart must preserve this exact initial set.
            var initialToggles = unit.Facts.List.Where(f => f.Blueprint == rageToggle || f.Blueprint == retortToggle).ToArray();
            Check(initialToggles.Count(f => f.Blueprint == rageToggle) == 1 && initialToggles.Count(f => f.Blueprint == retortToggle) == (foreign ? 2 : 1) && (!foreign || initialToggles.Contains(existing)), "native initial grant has exactly one own toggle per source and preserves foreign fact identity");
            unit.Resources.Spend(retortUses, 1); unit.Resources.Spend(rageUses, 2); int before = unit.Resources.GetResourceAmount(rageUses);
            for (int pass = 0; pass < 2; pass++) {
                unit.Facts.TurnOff(); Check(initialToggles.All(f => !f.IsTurnedOn), "native manager off turns off every owned and foreign toggle"); unit.Facts.TurnOn();
                Check(unit.Resources.GetResourceAmount(retortUses) == 0 && unit.Resources.GetResourceAmount(rageUses) == before, "native manager restart does not refill either daily resource");
                var currentToggles = unit.Facts.List.Where(f => f.Blueprint == rageToggle || f.Blueprint == retortToggle).ToArray();
                Check(currentToggles.Length == initialToggles.Length && currentToggles.All(initialToggles.Contains) && initialToggles.All(f => f.Manager == unit.Facts && f.IsActive && f.IsTurnedOn), "native manager restart retains exact owned/foreign toggle identities, counts and active state without adding any fact");
            }
            Remove(rage); Check(!unit.HasFact(rageToggle) && !unit.Resources.ContainsResource(rageUses) && unit.HasFact(retortToggle), "native source removal clears only Divine Anger grant/pool"); Check(unit.UISettings.Dirty, "native ActivatableAbilityCollection detach marks real owner UISettings dirty");
            Remove(retort); Check(!unit.Resources.ContainsResource(retortUses) && unit.HasFact(retortToggle) == foreign, "native retort source removal preserves preexisting foreign toggle"); if (existing != null) Remove(existing);
        }
        var raging = Unit(cache, "native-rage", level: 20); Add(raging, Native<BlueprintFeature>(cache, "ce49c579fe0bcc647a32c96929fae982")); Add(raging, Native<BlueprintFeature>(cache, "ca9343d75a83a2745a22fa11c383153a"));
        int ac = raging.Stats.AC.ModifiedValue, will = raging.Stats.SaveWill.ModifiedValue, hp = raging.Stats.TemporaryHitPoints.ModifiedValue;
        var buff = Add(raging, Asset<BlueprintBuff>(cache, "AngerDivineRageBuff"));
        Check(raging.Stats.AC.ModifiedValue == ac - 2 && raging.Stats.SaveWill.ModifiedValue == will + 3 && raging.Stats.TemporaryHitPoints.ModifiedValue == hp + 51, "actual native rage AC/Will modifiers and level17 greater-rage HP property apply");
        raging.Facts.TurnOff(); Check(raging.Stats.AC.ModifiedValue == ac && raging.Stats.SaveWill.ModifiedValue == will, "native manager off removes rage stat sources"); raging.Facts.TurnOn(); Check(raging.Stats.AC.ModifiedValue == ac - 2 && raging.Stats.SaveWill.ModifiedValue == will + 3, "native manager on restores one native rage stat source");
        buff.Deactivate(); Check(raging.Stats.AC.ModifiedValue == ac && raging.Stats.SaveWill.ModifiedValue == will && raging.Stats.TemporaryHitPoints.ModifiedValue == hp, "actual native deactivation restores stats and removes owned temporary HP"); buff.Activate(); Check(raging.Stats.TemporaryHitPoints.ModifiedValue == hp + 51, "native reactivation recalculates HP from original property"); Remove(buff);
        Check(raging.Stats.AC.ModifiedValue == ac && raging.Stats.SaveWill.ModifiedValue == will && raging.Stats.TemporaryHitPoints.ModifiedValue == hp, "actual native rage removal leaves no owned modifier");
    }
    static AngerRetortCommand01431 Queue(BlueprintAbilityResource uses, RuleAttackWithWeapon hit) {
        owner.Resources.Restore(uses); int amount = owner.Resources.GetResourceAmount(uses), before = reactions.Count, spendBefore = resourceSpends; Dispatch(consent, hit);
        Check(owner.Commands.Queue.Count(c => c is AngerRetortCommand01431) == 1 && reactions.Count == before, "successful hit queues one reaction before action frame");
        Check(owner.Resources.GetResourceAmount(uses) == amount - 1 && resourceSpends == spendBefore + 1, "legal queue spends exactly one real native daily resource");
        var command = owner.Commands.Queue.OfType<AngerRetortCommand01431>().Single(); Check(command.Executor == owner && command.Type == UnitCommand.CommandType.Free && command.ReactionAction && !command.IsOneFrameCommand && !command.ShouldSkipAnimation, "native queue holds initialized animated free reaction"); return command;
    }
    static void ClearCommands() { owner.Commands.Queue.Clear(); owner.Commands.Raw[(int)UnitCommand.CommandType.Free] = null; owner.Commands.Raw[(int)UnitCommand.CommandType.Standard] = null; }
    static void Retort(BlueprintsCache cache, Harmony harmony) {
        owner = Unit(cache, "retort-owner"); enemy = Unit(cache, "retort-enemy"); Set(enemy, "m_Position", new Vector3(1, 0, 0));
        var sword = Weapon(owner, false, "owner-sword"); var enemySword = Weapon(enemy, false, "enemy-sword"); var bow = Weapon(enemy, true, "enemy-bow"); Equip(owner, sword); Equip(enemy, enemySword);
        var feature = Add(owner, Asset<BlueprintFeature>(cache, "AngerHatefulRetortFeature")); var enemyFeature = Add(enemy, Asset<BlueprintFeature>(cache, "AngerHatefulRetortFeature")); var uses = Asset<BlueprintAbilityResource>(cache, "AngerHatefulRetortUses"); consent = Add(owner, Asset<BlueprintBuff>(cache, "AngerHatefulRetortBuff")); counterConsent = Add(enemy, Asset<BlueprintBuff>(cache, "AngerHatefulRetortBuff"));
        void Rejected(RuleAttackWithWeapon attack, string reason) { int amount = owner.Resources.GetResourceAmount(uses), spent = resourceSpends, queued = owner.Commands.Queue.Count, shots = reactions.Count; Dispatch(consent, attack); Check(owner.Resources.GetResourceAmount(uses) == amount && resourceSpends == spent && owner.Commands.Queue.Count == queued && reactions.Count == shots, reason + " preserves daily use and queue"); }
        Rejected(Incoming(enemy, owner, enemySword, false), "miss"); Rejected(Incoming(enemy, owner, enemySword, fake: true), "preview"); Rejected(Incoming(enemy, owner, bow), "ranged hit");
        Rejected(Incoming(enemy, owner, null), "no real weapon"); Rejected(Incoming(null, owner, enemySword), "no attacker"); Rejected(Incoming(enemy, enemy, enemySword), "different recipient"); Rejected(Incoming(owner, owner, sword), "self-hit");
        ally = true; Rejected(Incoming(enemy, owner, enemySword), "ally"); ally = false;
        Life(enemy, true); Rejected(Incoming(enemy, owner, enemySword), "dead attacker"); Life(enemy, false);
        Life(owner, true); Rejected(Incoming(enemy, owner, enemySword), "dead recipient"); Life(owner, false);
        mayAct = false; Rejected(Incoming(enemy, owner, enemySword), "owner cannot act"); mayAct = true; mayAttack = false; Rejected(Incoming(enemy, owner, enemySword), "owner cannot attack"); mayAttack = true;
        Set(enemy, "m_Position", new Vector3(20, 0, 0)); Rejected(Incoming(enemy, owner, enemySword), "out of reach"); Set(enemy, "m_Position", new Vector3(1, 0, 0));
        Equip(owner, bow); Rejected(Incoming(enemy, owner, enemySword), "ranged-only owner"); Equip(owner, sword);
        consent.TurnOff(); Rejected(Incoming(enemy, owner, enemySword), "consent off"); consent.TurnOn(); consent.Deactivate(); Rejected(Incoming(enemy, owner, enemySword), "consent inactive"); consent.Activate(); consent.IsSuppressed = true; Rejected(Incoming(enemy, owner, enemySword), "suppressed consent"); consent.IsSuppressed = false;
        var touchBlueprint = Native<BlueprintItemWeapon>(cache, "bb337517547de1a4189518d404ec49d4"); var touchType = Native<BlueprintWeaponType>(cache, "ba9cd5a303b089f458aa9f07aa10ea10");
        Check(touchBlueprint.Type == touchType && touchType.Category == WeaponCategory.Touch && touchType.AttackType == AttackType.Touch && touchBlueprint.IsMelee && !touchBlueprint.IsRanged, "actual native TouchItem/TouchType is a melee touch weapon");
        var touchWeapon = Weapon(enemy, false, "native-touch"); Set(touchWeapon, "m_Blueprint", touchBlueprint);
        var touchHit = new RuleAttackWithWeapon(enemy, owner, touchWeapon, 0); var touchRoll = new RuleAttackRoll(enemy, owner, touchWeapon, 0); Set(touchRoll, "Result", AttackResult.Hit); Set(touchHit, "AttackRoll", touchRoll);
        Check(!touchRoll.IsFake && touchHit.Weapon.Blueprint == touchBlueprint && touchRoll.IsHit, "native RuleAttackWithWeapon/RuleAttackRoll constructors accept real native melee Touch identity without preview state");
        var originalGate = new Kingmaker.UnitLogic.Mechanics.Components.AddTargetAttackWithWeaponTrigger { OnlyMelee = true, OnlyHit = true, NotReach = true, WaitForAttackResolve = true, CheckCategory = false };
        Check((bool)AccessTools.Method(originalGate.GetType(), "CheckConditions").Invoke(originalGate, new object[] { touchHit }), "original native CotW melee trigger predicate accepts the native Touch hit");
        var touchCommand = Queue(uses, touchHit); Check(owner.Commands.Queue.Contains(touchCommand), "real native melee Touch hit queues the same paid normal melee retort"); owner.Commands.Queue.Remove(touchCommand); owner.Resources.Restore(uses);
        Rejected(Incoming(enemy, owner, touchWeapon, false), "native touch miss"); Rejected(Incoming(enemy, owner, touchWeapon, fake: true), "native touch preview");
        var rayWeapon = Weapon(enemy, true, "ray-negative"); rayWeapon.Blueprint.Type.Category = WeaponCategory.Ray; Set(rayWeapon.Blueprint.Type, "m_AttackType", AttackType.RangedTouch); rayWeapon.Blueprint.Type.OnEnable();
        Check(rayWeapon.Blueprint.Type.AttackType == AttackType.RangedTouch && rayWeapon.Blueprint.IsRanged && !rayWeapon.Blueprint.IsMelee, "native IsMelee rejects the ranged-touch Ray fixture independent of category filtering"); Rejected(Incoming(enemy, owner, rayWeapon), "ranged-touch Ray hit");
        owner.Resources.Spend(uses, 1); Rejected(Incoming(enemy, owner, enemySword), "zero daily uses"); owner.Resources.Restore(uses);
        owner.CombatState.Cooldown.StandardAction = 4; owner.CombatState.Cooldown.SwiftAction = 5; owner.CombatState.Cooldown.AttackOfOpportunity = 6; int aoo = owner.CombatState.AttackOfOpportunityCount;
        var ordinary = new UnitAttack(enemy); Set(ordinary, "Executor", owner); owner.Commands.Queue.AddLast(ordinary);
        var queued = Queue(uses, Incoming(enemy, owner, enemySword)); Check(owner.Commands.Queue.First.Value == queued && owner.Commands.Queue.Contains(ordinary), "retort priority insertion preserves scheduled ordinary attack");
        Rejected(Incoming(enemy, owner, enemySword), "second hit while paid use empty");
        owner.Commands.Queue.Remove(queued); owner.Commands.Raw[(int)UnitCommand.CommandType.Free] = queued; turnBased = true;
        harmony.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkRangedReactionTurnPatch")).Patch();
        Check((bool)AccessTools.Method(typeof(UnitCommandController), "TickCommandTurnBased").Invoke(new UnitCommandController(), new object[] { queued }), "actual existing turn scheduler admits off-turn Anger free reaction");
        using (Game.Instance.TurnBasedCombatController.WaitingForUI.Scope()) Check(!(bool)AccessTools.Method(typeof(UnitCommandController), "TickCommandTurnBased").Invoke(new UnitCommandController(), new object[] { queued }), "existing UI-wait guard pauses Anger reaction");
        var blocking = new UnitAttack(enemy); Set(blocking, "Executor", owner); Set(blocking, "IsStarted", true); var blockingAnimation = Empty<UnitAnimationActionHandle>(); Set(blocking, "Animation", blockingAnimation); owner.Commands.Raw[(int)UnitCommand.CommandType.Standard] = blocking;
        Check(!queued.CanStart, "ordinary running attack animation delays retort without replacing command"); Set(blockingAnimation, "IsFinished", true); Check(queued.CanStart, "retort starts after ordinary animation completes"); owner.Commands.Raw[(int)UnitCommand.CommandType.Standard] = null;
        render = true; Set(queued, "IsStarted", true); AccessTools.Method(typeof(AngerRetortCommand01431), "OnStart").Invoke(queued, null);
        Check(animations == 1 && starts == 1 && queued.Animation == animation, "actual OnStart requests normal native melee hand animation");
        int count = reactions.Count; queued.Tick(); ticks++; Check(reactions.Count == count && !queued.IsActed, "native Tick waits before animation act frame");
        owner.Resources.Restore(uses); recursive = true; animation.IsActed = true; queued.Tick(); ticks++; Check(reactions.Count == count + 1 && queued.IsActed && owner.Resources.GetResourceAmount(uses) == 1, "native Tick action frame emits once and rejects command-origin recursive hit without spending again");
        recursive = false; queued.Tick(); ticks++; Check(reactions.Count == count + 1, "repeated native Tick cannot repeat retort action");
        Check(owner.CombatState.Cooldown.StandardAction == 4 && owner.CombatState.Cooldown.SwiftAction == 5 && owner.CombatState.Cooldown.AttackOfOpportunity == 6 && owner.CombatState.AttackOfOpportunityCount == aoo, "retort spends no standard/swift/AoO allowance");
        Check(queued.Animation.AttackTargetDistance == 1f, "melee animation uses actual fixture target distance");
        render = false; ClearCommands();
        foreach (string invalidation in new[] { "consent-removed", "target-left", "weapon-changed", "target-dead" }) {
            var command = Queue(uses, Incoming(enemy, owner, enemySword)); int prior = reactions.Count;
            if (invalidation == "consent-removed") Remove(consent); else if (invalidation == "target-left") Set(enemy, "m_Position", new Vector3(30, 0, 0)); else if (invalidation == "weapon-changed") Equip(owner, Weapon(owner, false, "replacement")); else Life(enemy, true);
            Check(command.ShouldBeInterrupted && (UnitCommand.ResultType)AccessTools.Method(typeof(AngerRetortCommand01431), "OnAction").Invoke(command, null) == UnitCommand.ResultType.Fail && reactions.Count == prior && owner.Resources.GetResourceAmount(uses) == 0, "queued " + invalidation + " cancels illegal reaction with no attack/refund");
            ClearCommands(); Set(enemy, "m_Position", new Vector3(1, 0, 0)); if (invalidation == "target-dead") Life(enemy, false); Equip(owner, sword); if (invalidation == "consent-removed") consent = Add(owner, Asset<BlueprintBuff>(cache, "AngerHatefulRetortBuff"));
        }
        turnBased = false; var realTime = Queue(uses, Incoming(enemy, owner, enemySword)); int earlier = reactions.Count;
        Check((UnitCommand.ResultType)AccessTools.Method(typeof(AngerRetortCommand01431), "OnAction").Invoke(realTime, null) == UnitCommand.ResultType.Success && reactions.Count == earlier + 1, "real-time free-command action uses same legal single attack");
        Check((UnitCommand.ResultType)AccessTools.Method(typeof(AngerRetortCommand01431), "OnAction").Invoke(realTime, null) == UnitCommand.ResultType.Fail && reactions.Count == earlier + 1, "direct repeated OnAction is independently idempotent"); ClearCommands();
        foreach (string field in new[] { "m_Context", "m_WeaponId", "m_Consent", "m_Resolved" }) Check(AccessTools.Field(typeof(AngerRetortCommand01431), field).IsDefined(typeof(JsonPropertyAttribute), false), "pending retort saves " + field);
        Remove(consent); Remove(counterConsent); Remove(feature); Remove(enemyFeature); owner = enemy = null; consent = counterConsent = null;
    }
    internal static int Run(BlueprintsCache cache) {
        checks = serial = selectCalls = resourceSpends = nativeRoundCalls = nativeOns = nativeOffs = added = removed = animations = starts = ticks = fxBoundaryCalls = actionFxBoundaryCalls = 0; units.Clear(); reactions.Clear(); assets.Clear(); failAppend = turnBased = render = recursive = ally = false; mayAct = mayAttack = true;
        view = Empty<UnitEntityView>(); animationManager = Empty<UnitAnimationManager>(); var game = Game.Instance; var oldState = game.State; var oldTime = game.TimeController; var oldCombat = game.TurnBasedCombatController;
        var player = Empty<Player>(); player.GameTime = TimeSpan.FromSeconds(100); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); var state = Empty<PersistentState>(); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>()); Set(game, "TurnBasedCombatController", Empty<CombatController>()); Set(game.TurnBasedCombatController, "WaitingForUI", new CountingGuard());
        var harmony = new Harmony("CotW01431.AngerNativeContracts"); var observer = new Observer();
        void Patch(MethodBase method, string prefix) => harmony.Patch(method, prefix: new HarmonyMethod(typeof(AngerInquisition01431Tests), prefix));
        Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Time)); Patch(AccessTools.PropertyGetter(typeof(TimeController), "DeltaTime"), nameof(Delta)); Patch(AccessTools.Method(typeof(CombatController), "IsInTurnBasedCombat"), nameof(Mode)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(False));
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip)); Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(False)); Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController));
        Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(NativeIdentity)); harmony.Patch(AccessTools.Method(typeof(EntityFact), "TurnOn"), postfix: new HarmonyMethod(typeof(AngerInquisition01431Tests), nameof(FactOn))); harmony.Patch(AccessTools.Method(typeof(EntityFact), "TurnOff"), postfix: new HarmonyMethod(typeof(AngerInquisition01431Tests), nameof(FactOff))); harmony.Patch(AccessTools.Method(typeof(UnitAbilityResourceCollection), "Spend"), postfix: new HarmonyMethod(typeof(AngerInquisition01431Tests), nameof(Spend)));
        harmony.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Howl01428Tests), "DifficultyBoundary"));
        // These exact native methods contain only prefab loading and particle
        // SpawnFxOnUnit calls. Retain their blueprint components and parameters;
        // bound only the view/prefab scene endpoints, never manager/fact/runtime
        // activation or the rage/HP/resource/companion gameplay components.
        var particles = typeof(BuffParticleEffectPlay);
        Patch(AccessTools.Method(particles, "OnActivate"), nameof(ParticleSceneBoundary));
        Patch(AccessTools.Method(particles, "OnDeactivate"), nameof(ParticleSceneBoundary));
        Patch(AccessTools.Method(particles, "Kingmaker.Controllers.Units.ITickEachRound.OnNewRound"), nameof(ParticleSceneBoundary));
        // Current native RunAction only loads a prefab and spawns it on the
        // target view/point. Keep the action node and real parent ActionList;
        // bound this pure scene FX action without replacing any other action.
        Patch(AccessTools.Method(typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnFx), "RunAction"), nameof(ActionFxSceneBoundary));
        // ActionList has already executed the real action before this reporting
        // call. Rethrow its original error with the original stack instead of
        // entering Unity logger/report UI; never turn an action failure into a pass.
        Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(ExposeActionFailure));
        Patch(AccessTools.Method(typeof(UnitEntityData), "IsAlly", new[] { typeof(UnitEntityData) }), nameof(Ally)); Patch(AccessTools.Method(typeof(UnitEntityData), "CanAttack", new[] { typeof(UnitEntityData) }), nameof(CanAttack)); Patch(AccessTools.PropertyGetter(typeof(UnitState), "CanAct"), nameof(CanAct));
        Patch(AccessTools.Method(typeof(UnitHelper), "GetThreatHand"), nameof(Hand)); Patch(AccessTools.Method(typeof(UnitHelper), "GetThreatRange"), nameof(Range)); Patch(AccessTools.Method(typeof(UnitHelper), "IsAttackOfOpportunityReach"), nameof(Reach)); Patch(AccessTools.PropertyGetter(typeof(EntityDataBase), "IsInState"), nameof(True));
        Patch(AccessTools.DeclaredProperty(typeof(UnitEntityData), "View").GetGetMethod(), nameof(View)); Patch(AccessTools.DeclaredProperty(typeof(UnitEntityView), "AnimationManager").GetGetMethod(), nameof(Manager)); Patch(AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality"), nameof(NotEqual)); Patch(AccessTools.Method(typeof(AttackHandInfo), "CreateAnimationHandleForAttack"), nameof(CreateAnimation)); Patch(AccessTools.Method(typeof(UnitCommand), "StartAnimation", new[] { typeof(UnitAnimationActionHandle) }), nameof(StartAnimation));
        PsychicRuleTestBoundary.Patch(harmony, typeof(RuleAttackWithWeapon), AccessTools.Method(typeof(AngerInquisition01431Tests), nameof(CaptureReaction))); Provider01419Tests.PatchLabels(harmony); EventBus.Subscribe(observer);
        Exception primaryFailure = null;
        try {
            LifecycleIntegrity(); Registration(cache, harmony); SelectionAndAmounts(cache); GrantsAndRage(cache); Retort(cache, harmony);
            Check(nativeOns > 0 && nativeOffs > 0 && added > 0 && removed > 0 && fxBoundaryCalls > 0 && actionFxBoundaryCalls > 0, "actual native lifecycle and BuffCollection EventBus observed with retained particle/action FX scene endpoints bounded");
            PsychicExpansionTests.Inventory["anger01431Contracts"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0299"), ["ordinaryFeatCount"] = 0, ["nativeCanSelectCalls"] = selectCalls, ["nativeResourceSpendCalls"] = resourceSpends, ["nativeRoundResourceCalls"] = nativeRoundCalls,
                ["nativeManagerLifecycle"] = true, ["nativeFactTurnOnCalls"] = nativeOns, ["nativeFactTurnOffCalls"] = nativeOffs, ["nativeAddedEvents"] = added, ["nativeRemovedEvents"] = removed, ["boundedParticleSceneCalls"] = fxBoundaryCalls, ["boundedActionFxSceneCalls"] = actionFxBoundaryCalls,
                ["noLifecycleMethodReplacement"] = true, ["nativeAddFactsOwnership"] = true, ["registrationAppendRollback"] = true, ["repeatRegistrationIdempotence"] = true, ["independentOfFerociousRegistration"] = true, ["lateFerociousBridgeResolves"] = true,
                ["nativeResourceAmountsAndSpend"] = true, ["nativeRagePropertyAndStats"] = true, ["retortNormalAnimatedFreeCommand"] = true, ["nativeCommandTicks"] = ticks, ["animationRequests"] = animations,
                ["nativeActionFrameAndRecursionGuard"] = true, ["illegalRetortCasesPreserveResource"] = true, ["queuedConsentRemovalCancels"] = true,
                ["newBlueprints"] = new JArray(assets.Select(a => new JObject { ["id"] = a.AssetGuid.ToString(), ["name"] = a.name, ["type"] = a.GetType().FullName })),
                ["boundary"] = "Real PersistentState.PlayerState/GameTime and empty party cache, original owner UnitUISettings constructor and native ability/activatable/fact collections. Native selection CanSelect uses actual cached Gorum/Rovagug blueprint instances and native Feature CreateFact/Add/Remove. Their unrelated weapon/domain/equipment grant components are temporarily excluded as eligibility inputs; exact original component arrays, definitions and cache identity are restored. Native resource GetMaxAmount/Add/Restore/Spend/Remove and actual ActivatableAbilityResourceLogic.OnNewRound callbacks (finite and Limitless), blueprint property GetInt, CreateFact/manager Attach/Activate/Deactivate/TurnOn/TurnOff/Remove, production Anger AddFacts ownership, cloned rage stat/HP components and BuffCollection EventBus execute. Native round callbacks run directly on the actual granted toggle runtime; whole game round scheduling remains a tester case. Feature prerequisite update/level-up UI, unique-ID allocation, tactical state, threat-hand/reach geometry/CanAttack/CanAct/ally scene inputs and Unity animation creation/start are controlled. Retained BuffParticleEffectPlay blueprint payloads remain unchanged; its pure prefab/view particle endpoints OnActivate/OnDeactivate/ITickEachRound.OnNewRound are skipped in the fixture. Retained ContextActionSpawnFx nodes are unchanged; only their pure prefab/view/point FX RunAction endpoint is skipped. Actual parent ActionList.Run and every other subject action execute; the exact QA ExceptionWithReport endpoint rethrows the original ElementLogicException.InnerException with its original stack, so Unity report/logger initialization cannot mask an action failure or convert it to success. Death/living inputs execute native UnitState.UpdateLifeState and assert native LifeState/IsDead before retort rejection and queued invalidation. Incoming Hit/Miss/fake events are fixture outcomes delivered to actual runtime components; reaction OnStart and native UnitCommand.Tick wait for the act frame and construct the real RuleAttackWithWeapon, whose final rule/scene damage execution is captured. Progression level/grant definitions are checked; complete native level-up UI and rendered combat, final damage, animation assets, full save/load and full external-mod combination remain game checks. Native manager/fact/gameplay-component lifecycle, production AddFacts and resource spend are not skipped or transpiled." };
            Console.WriteLine("PASS: " + checks + " Anger01431 native contracts; native ticks " + ticks + ", resource spends " + resourceSpends); return checks;
        } catch (Exception error) { primaryFailure = error; Console.WriteLine("Anger01431 primary failure: " + error); throw; }
        finally {
            render = false; owner = enemy = null; consent = counterConsent = null; failAppend = false;
            Exception cleanupFailure = null;
            try {
                foreach (var unit in units) foreach (var fact in unit.Facts.List.OrderBy(f => f is Buff ? 0 : 1).ToArray()) if (fact.Manager != null) {
                    try { fact.Manager.Remove(fact); }
                    catch (Exception error) { if (cleanupFailure == null) cleanupFailure = error; Console.WriteLine("Anger01431 cleanup failure: " + error); }
                }
            }
            finally { EventBus.Unsubscribe(observer); harmony.UnpatchAll(harmony.Id); Set(game, "State", oldState); Set(game, "TimeController", oldTime); Set(game, "TurnBasedCombatController", oldCombat); units.Clear(); }
            if (primaryFailure == null && cleanupFailure != null) throw new Exception("Anger01431 native cleanup failed", cleanupFailure);
        }
    }
}
