using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class WizardSubschools01431Tests
{
    static int checks, added, removed, casts, resourceSpends, uidSerial;
    static bool failAppend;
    static int d100;
    static readonly List<Buff> live = new List<Buff>();
    static Type Module => typeof(UndeadBolsterConsecrate01431).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardSubschools01431", true);
    static object Get(object o, string f) => (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).GetValue(o);
    static void Set(object o, string f, object v) => (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).SetValue(o, v);
    static object Call(Type t, string name, params object[] args) => AccessTools.Method(t, name).Invoke(null, args);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static bool Own(SimpleBlueprint b) => b.name != null && new[] { "UndeadSchool", "UndeadBolster", "UndeadCommand", "Prophecy" }.Any(prefix => b.name.StartsWith("CotWWrathPsychic" + prefix, StringComparison.Ordinal));
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static string Definition(object o) => (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new[] { o });
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception("Wizard01431: " + message); }
    static void Register(BlueprintsCache c) => Call(Module, "Register", c);
    static void Optional(BlueprintsCache c) => Call(Module, "Optional", c);
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool Time(ref TimeSpan __result) { __result = TimeSpan.FromSeconds(100); return false; }
    static bool UniqueId(ref string __result) { __result = "Wizard01431-allocated-" + (++uidSerial); return false; }
    static bool Dice(RuleRollDice __instance, ref int __result) { if (!(__instance is RuleRollD100)) return true; __result = d100; return false; }
    static bool TargetAllowed(AbilityData __instance, TargetWrapper __0, ref bool __result) { __result = __0?.Unit != null; return false; }
    static RuleApplySpell ApplyPlace(RuleApplySpell rule) { rule.OnTrigger(Rulebook.CurrentContext); return rule; }
    static Kingmaker.Controllers.AbilityExecutionProcess CastScene(AbilityData ability, AbilityExecutionContext context) => null;
    static IEnumerable<CodeInstruction> CastBoundary(IEnumerable<CodeInstruction> instructions) {
        int place = 0, scene = 0;
        foreach (var code in instructions) {
            if (code.operand is MethodInfo m && m.DeclaringType == typeof(Rulebook) && m.Name == "Trigger" && m.IsGenericMethod && m.GetGenericArguments().SequenceEqual(new[] { typeof(RuleApplySpell) })) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(WizardSubschools01431Tests), nameof(ApplyPlace)); place++; }
            if (code.operand is MethodInfo c && c.DeclaringType == typeof(AbilityData) && c.Name == "Cast" && c.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(AbilityExecutionContext) })) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(WizardSubschools01431Tests), nameof(CastScene)); scene++; }
            yield return code;
        }
        if (place != 1 || scene != 1) throw new InvalidOperationException("Native RuleCastSpell scene boundary changed.");
    }
    static IEnumerable<CodeInstruction> SkillDifficultyBoundary(IEnumerable<CodeInstruction> instructions) {
        var code = instructions.ToList();
        int first = code.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(Kingmaker.Settings.SettingsRoot) && m.Name == "get_Difficulty");
        int last = code.FindIndex(i => i.operand is FieldInfo f && f.Name == "SkillCheckDCBonus");
        if (first < 0 || last != first + 9 || !(code[first + 1].operand is FieldInfo adjustment && adjustment.Name == "StatsAdjustments") ||
            code[first + 3].opcode != OpCodes.Ldarg_1 || !(code[last - 1].operand is MethodInfo preset && preset.Name == "GetAdjustmentPreset") || code[last + 1].opcode != OpCodes.Stind_I4)
            throw new InvalidOperationException("Native skill difficulty-preset lookup changed.");
        // Replace only the ten-instruction scene preset lookup with the same
        // output-address and neutral SkillCheckDCBonus input. All native DC
        // clamping/addition and UpdateValues stat/modifier arithmetic remain.
        for (int i = first; i <= last; i++) { code[i].opcode = i == first ? OpCodes.Ldarg_1 : i == first + 1 ? OpCodes.Ldc_I4_0 : OpCodes.Nop; code[i].operand = null; }
        return code;
    }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == Id("UndeadSchool"))) throw new InvalidOperationException("Wizard01431 append fault"); }
    static bool Rejected(Action action) { try { action(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    static T Hydrate<T>(BlueprintsCache c, string id, params string[] extra) where T : BlueprintScriptableObject, new() => (T)AccessTools.Method(typeof(Migration01429Tests), "Hydrate").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { c, id, extra });
    static UnitEntityData Unit(BlueprintsCache c, string name, int level) {
        var u = (UnitEntityData)Call(typeof(Migration01429Tests), "Unit", c, name, c.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e")), level);
        Set(u.Progression, "m_CharacterLevel", (int?)level);
        Set(u, "Buffs", u.Facts.EnsureFactProcessor<BuffCollection>());
        Set(u, "Abilities", u.Facts.EnsureFactProcessor<AbilityCollection>());
        Set(u, "ActivatableAbilities", u.Facts.EnsureFactProcessor<ActivatableAbilityCollection>());
        Set(u.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(u.Descriptor));
        if (u.Stats.TemporaryHitPoints == null) { var hp = new ModifiableValueTemporaryHitPoints(u.Stats); Set(u.Stats, "TemporaryHitPoints", hp); u.Stats.AllStats.Add(hp); }
        u.Facts.TurnOn(); return u;
    }
    static Feature Fact(UnitEntityData u, BlueprintFeature bp) => (Feature)Call(typeof(Migration01429Tests), "Fact", u, bp);
    static void Remove(Feature fact) => Call(typeof(Migration01429Tests), "Remove", fact);
    static bool Select(UnitEntityData u, BlueprintFeatureSelection pool, BlueprintFeature candidate) => (bool)Call(typeof(EnhancementLifecycle01430Tests), "CanSelect", u, pool, candidate);
    static Buff Add(UnitEntityData u, BlueprintBuff bp, UnitEntityData caster) {
        var context = new MechanicsContext(caster, u.Descriptor, bp); context.SetParams(new AbilityParams { CasterLevel = caster.Progression.Classes[0].Level });
        var b = new Buff(bp, context, null); Set(b, "UniqueId", "Wizard01431-" + Guid.NewGuid().ToString("N")); live.Add(b);
        u.Facts.Add(b); return b;
    }
    sealed class Events : IUnitBuffHandler {
        public void HandleBuffDidAdded(Buff b) { if (live.Contains(b)) added++; }
        public void HandleBuffDidRemoved(Buff b) { if (live.Contains(b)) removed++; }
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = added = removed = casts = resourceSpends = uidSerial = 0; live.Clear();
        var h = new Harmony("CotW01431.WizardNativeContracts"); var savedState = Game.Instance.State; var savedTime = Game.Instance.TimeController;
        var player = Empty<Player>(); player.GameTime = TimeSpan.FromSeconds(100); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true);
        var persistent = Empty<PersistentState>(); persistent.PlayerState = player; Set(Game.Instance, "State", persistent); Set(Game.Instance, "TimeController", Empty<TimeController>());
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(WizardSubschools01431Tests), name));
        Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Time));
        Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(No));
        Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(No));
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No));
        Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore));
        Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController));
        // Native IsPlayersEnemy starts at Faction.EnemyForEveryone and then
        // consults the scene group against Game.BlueprintRoot.PlayerFaction.
        // These constructed casters are controlled friendly actors; bound
        // that scene input, preserving the entire native parameter rule.
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(No));
        Patch(AccessTools.Method(typeof(Player), "GetNewUniqueId"), nameof(UniqueId));
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Howl01428Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(RuleSkillCheck), "AdjustDCByDifficulty"), transpiler: new HarmonyMethod(typeof(WizardSubschools01431Tests), nameof(SkillDifficultyBoundary)));
        var listener = new Events(); EventBus.Subscribe(listener);
        try {
            Registration(cache, h);
            ProphecyFacts(cache);
            Bolster(cache, h);
            Prediction(cache);
            Accordance(cache, h);
            var newAssets = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Select(cache.Load).Where(Own).ToArray();
            PsychicExpansionTests.Inventory["wizard01431Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 2, ["itemIds"] = new JArray("COTW-0375", "COTW-0377"), ["nativeBuffAddEvents"] = added, ["nativeBuffRemoveEvents"] = removed, ["castHandlers"] = casts, ["nativeResourceSpends"] = resourceSpends, ["paidContextMapPersistsAcrossReload"] = false, ["newBlueprints"] = new JArray(newAssets.Select(b => new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name })),
                ["boundary"] = "Current native blueprint hydration; original CanSelect, prerequisite/progression/resource arithmetic, Buff constructor, EntityFactsManager/EntityFact/runtime-component activation/removal/restart/postload and EventBus execute. Native AddFacts is exercised in isolated component hosts with the actual native and new school fact lists. Player unique-ID allocation is a controlled scene endpoint; Attach/Activate/OnPostLoad/Remove are not replaced. Native RulebookEventBus dispatches resolved skill/save/attack inputs, with real weapon construction, natural20/natural1 save inputs and exact native skill enumeration. Their full native condition checks and owned RemoveBuff actions execute; the complete attack/skill/save roll calculations are not claimed. Native AutoMetamagic parameter handling runs behind local resource/book eligibility. Native UnitPartSpecialMetamagic.GetMetamagic verifies that no passive registry entry bypasses eligibility. Original RuleCastSpell.OnTrigger executes with exactly two IL scene substitutions: native RuleApplySpell.OnTrigger is directly invoked, and final AbilityData.Cast scene process returns null. Target scene availability, friendly-caster IsPlayersEnemy=false faction input, neutral difficulty-preset input and D100 are fixed. Native class/stat/DC/metamagic parameter arithmetic is not replaced. Native ContextSetAbilityParams re-entry reproduces consumed-buff metamagic loss; the existing Recalculate postfix restores only this module's paid originating context and descendants. An independently expired native Maximize is not restored; native DC/CL/spell level/concentration and slot/resource counts remain. Native healing-rule construction reads the restored context. The weak context map is runtime-only and does not survive save reload; persistence is not simulated or claimed. Native success/failure decision is not assigned by the cast fixture. Final spell effects, full spell-slot UI, damage application, controlled faction AI, real save/death scene reconstruction and external COP DLL code remain game checks. COP fixture follows the three verified exact source identities; no external package is an assembly dependency. No new global Harmony target." };
            Console.WriteLine("PASS: " + checks + " wizard01431 contracts."); return checks;
        } finally { foreach (var b in live.Where(b => b.Manager != null).ToArray()) b.Manager.Remove(b); EventBus.Unsubscribe(listener); h.UnpatchAll(h.Id); Set(Game.Instance, "TimeController", savedTime); Set(Game.Instance, "State", savedState); }
    }
    static void ProphecyFacts(BlueprintsCache cache)
    {
        var forewarned = (BlueprintFeature)cache.Load(BlueprintGuid.Parse("f8ee4f0f4cf741fa9b561ba4eef9ebef"));
        var nativeBase = (BlueprintFeature)cache.Load(BlueprintGuid.Parse("54d21b3221ea82a4d90d5a91b7872f3d"));
        var prophecyBase = Asset<BlueprintFeature>(cache, "ProphecySchoolBase"); var u = Unit(cache, "Wizard01431-native-grants", 8);
        BlueprintFeature Host(BlueprintFeature source, string name, bool resources = false) {
            var components = source.ComponentsArray.Where(c => c is AddFacts || resources && c is AddAbilityResources).Select(c => (BlueprintComponent)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "CopyData").MakeGenericMethod(typeof(BlueprintComponent)).Invoke(null, new object[] { c })).ToArray();
            var result = new BlueprintFeature { name = name, AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), Ranks = 1, IsClassFeature = true, ComponentsArray = components }; result.OnEnable(); return result;
        }
        Feature GrantHost(BlueprintFeature bp) { var context = new MechanicsContext(u, u.Descriptor, bp); context.SetParams(new AbilityParams { CasterLevel = 8 }); var fact = (Feature)bp.CreateFact(context, u.Descriptor, null); u.Facts.Add(fact); return fact; }
        int initiative = u.Stats.Initiative.ModifiedValue;
        Check(nativeBase.GetComponents<AddFacts>().SelectMany(c => c.Facts).Any(f => ReferenceEquals(f, forewarned)), "native Forewarned grant resolves the hydrated current leaf");
        var old = GrantHost(Host(nativeBase, "Wizard01431-native-facts"));
        Check(u.HasFact(forewarned), "original native AddFacts grants its actual Forewarned leaf");
        Check(u.Stats.Initiative.ModifiedValue == initiative + 4, "original native ContextRankConfig and stat bonus grant half-Wizard initiative: actual " + (u.Stats.Initiative.ModifiedValue - initiative));
        u.Facts.Remove(old); Check(!u.HasFact(forewarned) && u.Stats.Initiative.ModifiedValue == initiative, "native AddFacts removal really removes owned Forewarned effect");
        var current = GrantHost(Host(prophecyBase, "Wizard01431-prophecy-facts"));
        Check(!u.HasFact(forewarned) && u.Stats.Initiative.ModifiedValue == initiative && u.HasFact(Asset<BlueprintAbility>(cache, "ProphecyPrediction")), "native new AddFacts grants Prediction while Forewarned remains absent");
        u.Facts.ApplyPostLoadFixes(); Check(!u.HasFact(forewarned) && u.HasFact(Asset<BlueprintAbility>(cache, "ProphecyPrediction")), "native postload retains replacement fact grants without Forewarned");
        u.Facts.Remove(current); Check(!u.HasFact(Asset<BlueprintAbility>(cache, "ProphecyPrediction")) && u.Stats.Initiative.ModifiedValue == initiative, "native new AddFacts cleanup removes its owned Prediction");
        var toggle = Asset<BlueprintActivatableAbility>(cache, "ProphecyAccordance"); var resource = Asset<BlueprintAbilityResource>(cache, "ProphecyAccordanceResource");
        var greater = GrantHost(Host(Asset<BlueprintFeature>(cache, "ProphecySchoolGreater"), "Wizard01431-prophecy-greater-facts", true));
        Check(u.HasFact(toggle) && u.Resources.GetResourceAmount(resource) == u.Stats.Intelligence.Bonus, "native greater feature grants real toggle and Intelligence daily resource");
        u.Facts.ApplyPostLoadFixes(); Check(u.HasFact(toggle) && u.Resources.GetResourceAmount(resource) == u.Stats.Intelligence.Bonus, "native greater postload preserves its real toggle/resource");
        u.Facts.Remove(greater); Check(!u.HasFact(toggle) && u.Resources.GetResourceAmount(resource) == 0, "native greater source removal cleans owned toggle and resource");
    }
    static void Registration(BlueprintsCache cache, Harmony h)
    {
        // Hydrate leaves before parents resolve their references. Replacing a
        // leaf afterwards would leave a cached reference to an earlier fixture
        // object even though the authored GUID did not change.
        Hydrate<BlueprintFeature>(cache, "f8ee4f0f4cf741fa9b561ba4eef9ebef");
        foreach (string id in new[] { "e9450978cc9feeb468fb8ee3a90607e3", "d7d18ce5c24bd324d96173fdc3309646" }) Hydrate<BlueprintProgression>(cache, id);
        foreach (string id in new[] { "927707dce06627d4f880c90b5575125f", "54d21b3221ea82a4d90d5a91b7872f3d", "a9bb3dcb2e8d44a49ac36c393c114bd9", "09595544116fe5349953f939aeba7611", "bfd0f754df75bd2449bb7d021dc5c7a1" }) Hydrate<BlueprintFeature>(cache, id);
        Hydrate<BlueprintBuff>(cache, "ec931b882e806ce42906597e5585c13f");
        Hydrate<BlueprintAbilityResource>(cache, "16394ff73175b5745957750040a2c99e", "m_MaxAmount", "m_Min");
        var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("5f838049069f1ac4d804ce0862ab5110"));
        var oppositions = new[] { (BlueprintFeature)cache.Load(BlueprintGuid.Parse("a9bb3dcb2e8d44a49ac36c393c114bd9")), (BlueprintFeature)cache.Load(BlueprintGuid.Parse("09595544116fe5349953f939aeba7611")) };
        var sources = new[] { (BlueprintProgression)cache.Load(BlueprintGuid.Parse("e9450978cc9feeb468fb8ee3a90607e3")), (BlueprintProgression)cache.Load(BlueprintGuid.Parse("d7d18ce5c24bd324d96173fdc3309646")) };
        // Native LevelEntry.Features lazily writes m_FeaturesList. Warm only
        // that getter before freezing the complete definition, so later
        // reads are not mistaken for authored donor changes.
        foreach (var source in sources) foreach (var entry in source.LevelEntries) { var refs = entry.Features.ToArray(); Check(refs.Length == ((List<BlueprintFeatureBaseReference>)Get(entry, "m_Features")).Count, "native donor feature cache matches authored list before snapshot"); }
        var snapshots = sources.Select(s => Definition(s.LevelEntries) + Definition(Get(s, "m_Classes"))).ToArray();
        var componentDonors = new[] { "927707dce06627d4f880c90b5575125f", "54d21b3221ea82a4d90d5a91b7872f3d", "ec931b882e806ce42906597e5585c13f" }.Select(id => (BlueprintScriptableObject)cache.Load(BlueprintGuid.Parse(id))).ToArray();
        var componentSnapshots = componentDonors.Select(d => Definition(d.ComponentsArray)).ToArray();
        var savedAssets = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Select(cache.Load).OfType<BlueprintScriptableObject>().Where(Own).ToArray();
        var poolBefore = new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") }; var oppositionBefore = oppositions.Select(o => o.ComponentsArray).ToArray();
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(WizardSubschools01431Tests), nameof(AppendFault)));
        try {
            foreach (var bp in savedAssets) cache.RemoveCachedBlueprint(bp.AssetGuid);
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(pool, field, ((BlueprintFeatureReference[])Get(pool, field)).Where(r => r.Guid != Id("UndeadSchool") && r.Guid != Id("ProphecySchool")).ToArray());
            var before = new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") }; var ids = new HashSet<BlueprintGuid>(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>());
            failAppend = true; Check(Rejected(() => Register(cache)), "fresh append failure rejects"); failAppend = false;
            Check(ids.SetEquals(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>()), "append failure removes every staged asset");
            Check(ReferenceEquals(before[0], Get(pool, "m_Features")) && ReferenceEquals(before[1], Get(pool, "m_AllFeatures")), "append failure restores exact pool arrays");
            Check(oppositions.Select((o, i) => ReferenceEquals(o.ComponentsArray, oppositionBefore[i])).All(x => x), "append failure restores both native opposition arrays");
        } finally { failAppend = false; foreach (var bp in savedAssets) cache.AddCachedBlueprint(bp.AssetGuid, bp); Set(pool, "m_Features", poolBefore[0]); Set(pool, "m_AllFeatures", poolBefore[1]); for (int i = 0; i < 2; i++) oppositions[i].ComponentsArray = oppositionBefore[i]; }
        Register(cache); var roots = new[] { Asset<BlueprintProgression>(cache, "UndeadSchool"), Asset<BlueprintProgression>(cache, "ProphecySchool") }; Register(cache);
        foreach (int i in new[] { 0, 1 }) {
            var root = roots[i]; Check(ReferenceEquals(cache.Load(root.AssetGuid), root), "repeat registration retains root identity");
            Check(!ReferenceEquals(root.LevelEntries, sources[i].LevelEntries) && !ReferenceEquals(Get(root, "m_Classes"), Get(sources[i], "m_Classes")) && !ReferenceEquals(root.UIGroups, sources[i].UIGroups), "school level/classes/UI arrays isolated");
            Check(root.LevelEntries.Single(e => e.Level == 1).Features.Count(f => f is BlueprintFeatureSelection) == 2, "two native opposition choices remain");
            Check(root.LevelEntries.Single(e => e.Level == 1).Features.Any(f => f.AssetGuid == Id(i == 0 ? "UndeadSchoolBase" : "ProphecySchoolBase")), "native cached progression getter grants the replacement first-level feature");
            Check(snapshots[i] == Definition(sources[i].LevelEntries) + Definition(Get(sources[i], "m_Classes")), "native progression donor unchanged");
            var u = Unit(cache, "Wizard01431-selection" + i, 1); var oppositionPool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("6c29030e9fea36949877c43a6f94ff31"));
            Check(Select(u, pool, root), "native school selection permits eligible wizard"); var fact = Fact(u, root);
            Check(!Select(u, oppositionPool, oppositions[i]), "selected school rejects corresponding opposition"); Remove(fact); var opposite = Fact(u, oppositions[i]);
            Check(!Select(u, pool, root), "opposition first rejects school"); Remove(opposite);
            foreach (int level in new[] { 1, 7, 8, 12, 16, 20 }) { u.Progression.Classes[0].Level = level; Check(root.CalcLevel(u.Descriptor) == level, "native wizard progression level " + level); }
        }
        Check(roots[0].LevelEntries.Single(e => e.Level == 8).Features.Select(f => f.AssetGuid).SequenceEqual(sources[0].LevelEntries.Single(e => e.Level == 8).Features.Select(f => f.AssetGuid)), "complete native Life Sight retained");
        Check(!roots[1].LevelEntries.SelectMany(e => e.Features).Any(f => f.AssetGuid.ToString() == "a6839f2a709288e43b68d3281b3bbd8f"), "Prophecy replaces native Forewarned initiative capstone");
        var prophecyFacts = Asset<BlueprintFeature>(cache, "ProphecySchoolBase").GetComponents<AddFacts>().SelectMany(c => c.Facts).ToArray();
        Check(!prophecyFacts.Any(f => f.AssetGuid.ToString() == "f8ee4f0f4cf741fa9b561ba4eef9ebef") && prophecyFacts.Any(f => f.AssetGuid.ToString() == "0997652c1d8eb164caae8a462401a25d"), "Prophecy removes exact Forewarned and retains Diviner's Fortune");
        Check(roots[1].LevelEntries.Single(e => e.Level == 8).Features.Any(f => f.AssetGuid == Id("ProphecySchoolGreater")), "native cached progression getter grants replacement Greater Prophecy");
        Check(componentDonors.Select((d, i) => componentSnapshots[i] == Definition(d.ComponentsArray)).All(v => v), "complete native Necromancy/Divination/Guidance components unchanged");
    }
    static void Bolster(BlueprintsCache cache, Harmony h)
    {
        var caster = Unit(cache, "Wizard01431-bolster-caster", 1); var target = Unit(cache, "Wizard01431-bolster-target", 9);
        var bp = Asset<BlueprintBuff>(cache, "UndeadBolsterBuff"); var ability = Asset<BlueprintAbility>(cache, "UndeadBolster"); var resource = Asset<BlueprintAbilityResource>(cache, "UndeadBolsterResource");
        var targetRestriction = ability.GetComponent<AbilityTargetHasFact>(); Check(!targetRestriction.IsTargetRestrictionPassed(caster, target), "living target rejects Bolster");
        var racial = Fact(target, (BlueprintFeature)cache.Load(BlueprintGuid.Parse("734a29b693e9ec346ba2951b27987e33"))); Check(targetRestriction.IsTargetRestrictionPassed(caster, target), "actual undead fact permits Bolster");
        foreach (int level in new[] { 1, 4, 5, 9, 10, 15, 20 }) {
            caster.Progression.Classes[0].Level = level; int attack = target.Stats.AdditionalAttackBonus.ModifiedValue; int temporary = target.Stats.TemporaryHitPoints.ModifiedValue;
            var buff = Add(target, bp, caster); buff.Context.Recalculate();
            Check(buff.IsActive && buff.IsTurnedOn && buff.IsSubscribedOnEventBus, "native Bolster activated/subscribed");
            Check(target.Stats.AdditionalAttackBonus.ModifiedValue == attack + 1 + level / 5, "native profane attack rank at " + level);
            Check(target.Progression.CharacterLevel == 9 && target.Stats.TemporaryHitPoints.ModifiedValue == temporary + 9, "temporary HP uses nine recipient HD rather than caster level");
            foreach (var bonus in bp.GetComponents<AddContextStatBonus>()) Check(bonus.CalculateBaseValue(buff.Context) == 1 + level / 5 && bonus.Descriptor == ModifierDescriptor.Profane, "all four native profane modifiers scale");
            target.Facts.TurnOff(); Check(target.Stats.AdditionalAttackBonus.ModifiedValue == attack, "manager off removes native stats"); target.Facts.TurnOn(); Check(target.Stats.AdditionalAttackBonus.ModifiedValue == attack + 1 + level / 5, "manager on restores native stats");
            target.Facts.ApplyPostLoadFixes(); target.Facts.Remove(buff); Check(target.Stats.AdditionalAttackBonus.ModifiedValue == attack && target.Stats.TemporaryHitPoints.ModifiedValue == temporary, "native removal balances stats/temporary HP");
            var context = new MechanicsContext(caster, target.Descriptor, ability); context.SetParams(new AbilityParams()); context.Recalculate();
            var apply = ability.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single(); Check(apply.DurationValue.Calculate(context).Seconds == TimeSpan.FromSeconds(6 * Math.Max(1, level / 2)), "Bolster native half-wizard duration");
        }
        foreach (int intelligence in new[] { 1, 10, 18, 30 }) { Set(caster.Stats.Intelligence, "m_BaseValue", intelligence); Set(caster.Stats.Intelligence, "ModifiedValue", intelligence); Check(resource.GetMaxAmount(caster.Descriptor) == Math.Max(0, 3 + (int)Math.Floor((intelligence - 10) / 2.0)), "Bolster native resource maximum"); }
        caster.Stats.Intelligence.BaseValue = 18; caster.Resources.Add(resource, true); var data = new AbilityData(ability, caster.Descriptor); int amount = caster.Resources.GetResourceAmount(resource); var cost = ability.GetComponent<AbilityResourceLogic>(); cost.Spend(data); resourceSpends++; Check(caster.Resources.GetResourceAmount(resource) == amount - 1, "Bolster native Spend pays one"); caster.Resources.Spend(resource, caster.Resources.GetResourceAmount(resource)); Check(!cost.IsAbilityRestrictionPassed(data), "zero resource rejects Bolster");
        Check(bp.GetComponent<TemporaryHitPointsPerLevel>().RemoveWhenHitPointsEnd == false, "losing temporary HP does not wrongly dispel attack/save benefits");
        Check(bp.GetComponent<SavingThrowBonusAgainstSpecificSpells>().Value == 2 && bp.GetComponent<SavingThrowBonusAgainstSpecificSpells>().Spells.Any(a => a.AssetGuid.ToString() == "71b8898b1d26d654b9a3eeac87e3e2f8"), "turn resistance includes native wizard Turn Undead");
        var command = Asset<BlueprintAbility>(cache, "UndeadCommand"); Check(command.Type == AbilityType.Supernatural && !command.SpellResistance && command.AvailableMetamagic == 0, "Command Undead supernatural adaptation"); Check(command.GetComponent<AbilityEffectRunAction>().SavingThrowType == SavingThrowType.Will && command.GetComponents<AbilityTargetHasFact>().Count() == 2, "Command Undead retains Will, undead and nonsummon restrictions");
        Check(command.GetComponents<AbilityTargetHasFact>().All(c => c.IsTargetRestrictionPassed(caster, target)), "native Command Undead target checks accept nonsummoned undead");
        var summon = Psychic034Tests.Fact(target, (BlueprintBuff)cache.Load(BlueprintGuid.Parse("8728e884eeaa8b047be04197ecf1a0e4")), caster, 20);
        Check(!command.GetComponents<AbilityTargetHasFact>().All(c => c.IsTargetRestrictionPassed(caster, target)), "native Command Undead target checks reject summoned undead"); target.Facts.List.Remove(summon); AccessTools.Method(Get(target.Facts, "m_Cache").GetType(), "Clear").Invoke(Get(target.Facts, "m_Cache"), null);
        var first = Asset<BlueprintFeature>(cache, "UndeadSchoolBase");
        Check(first.GetComponent<ReplaceAbilitiesStat>().Stat == StatType.Intelligence && first.GetComponent<ReplaceAbilitiesStat>().Ability.Any(a => a.AssetGuid == ability.AssetGuid), "native SLA Intelligence replacement now targets Bolster");
        Check(first.GetComponent<ReplaceCasterLevelOfAbility>().Spell == ability && first.GetComponent<ReplaceCasterLevelOfAbility>().Class.AssetGuid.ToString() == "ba34257984f4c41408ce1dc2004e342e", "native SLA caster-level replacement now targets Bolster and Wizard");
        h.Patch(AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "ParamsBoundary"));
        foreach (int level in new[] { 1, 8, 19, 20 }) {
            caster.Progression.Classes[0].Level = level;
            var parameters = command.GetComponent<ContextCalculateAbilityParams>().Calculate(new AbilityData(command, caster.Descriptor));
            Check(parameters.CasterLevel == level && parameters.DC == 10 + level / 2 + caster.Stats.Charisma.Bonus, "native Command parameters use Wizard level/Charisma at " + level);
        }
        Consecrate(cache, target, caster, bp); Remove(racial);
    }
    static void Consecrate(BlueprintsCache cache, UnitEntityData target, UnitEntityData caster, BlueprintBuff bolster)
    {
        string markerId = (string)AccessTools.Field(Module, "ConsecrateBuffId").GetValue(null); string areaId = (string)AccessTools.Field(Module, "ConsecrateAreaId").GetValue(null); string abilityId = (string)AccessTools.Field(Module, "ConsecrateAbilityId").GetValue(null);
        var saved = new[] { markerId, areaId, abilityId }.Select(id => cache.Load(BlueprintGuid.Parse(id))).ToArray();
        try {
            foreach (string id in new[] { markerId, areaId, abilityId }) cache.RemoveCachedBlueprint(BlueprintGuid.Parse(id)); Optional(cache); Check(cache.Load(BlueprintGuid.Parse(markerId)) == null, "absent optional provider creates no placeholders");
            var marker = new BlueprintBuff { AssetGuid = BlueprintGuid.Parse(markerId), name = "Consecrate.Buff", ComponentsArray = new BlueprintComponent[] { new SavingThrowBonusAgainstDescriptor { SpellDescriptor = SpellDescriptor.ChannelPositiveHarm, ModifierDescriptor = ModifierDescriptor.Sacred, Value = -3 } } }; marker.OnEnable();
            var effect = new AbilityAreaEffectBuff { Condition = new ConditionsChecker { Conditions = Array.Empty<Condition>() } }; Set(effect, "m_Buff", Ref<BlueprintBuffReference>(marker));
            var area = new BlueprintAbilityAreaEffect { AssetGuid = BlueprintGuid.Parse(areaId), name = "Consecrate.AoE", ComponentsArray = new BlueprintComponent[] { effect } }; area.OnEnable();
            var spawn = new ContextActionSpawnAreaEffect(); Set(spawn, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            var ability = new BlueprintAbility { AssetGuid = BlueprintGuid.Parse(abilityId), name = "Consecrate", Type = AbilityType.Spell, ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation }, new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { spawn } } } } }; ability.OnEnable();
            cache.AddCachedBlueprint(marker.AssetGuid, marker); cache.AddCachedBlueprint(area.AssetGuid, area); cache.AddCachedBlueprint(ability.AssetGuid, ability); Optional(cache); var once = marker.ComponentsArray; Optional(cache);
            Check(ReferenceEquals(once, marker.ComponentsArray) && once.Length == 2, "late exact COP provider integration idempotent");
            var first = Add(target, bolster, caster); var consecrate = Add(target, marker, caster); Check(first.IsDisposed && !target.Buffs.Enumerable.Contains(first), "native COP marker entry immediately removes existing Bolster");
            var during = Add(target, bolster, caster); Check(during.IsDisposed, "Bolster cast into existing Consecrate immediately removed"); target.Facts.Remove(consecrate);
            var after = Add(target, bolster, caster); Check(after.IsActive, "leaving Consecrate permits a new Bolster"); target.Facts.Remove(after);
            var old = ability.ComponentsArray; ability.ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Necromancy } }; ability.OnEnable(); Check(Rejected(() => Optional(cache)) && ReferenceEquals(once, marker.ComponentsArray), "mismatched provider rejected without rewriting its components"); ability.ComponentsArray = old; ability.OnEnable();
        } finally { for (int i = 0; i < 3; i++) { var id = BlueprintGuid.Parse(new[] { markerId, areaId, abilityId }[i]); cache.RemoveCachedBlueprint(id); if (saved[i] != null) cache.AddCachedBlueprint(id, saved[i]); } }
    }
    static void Prediction(BlueprintsCache cache)
    {
        var u = Unit(cache, "Wizard01431-prediction", 10); var bp = Asset<BlueprintBuff>(cache, "ProphecyPredictionBuff");
        var friendlyFaction = new BlueprintFaction { name = "Wizard01431-friendly-faction" };
        var hostileFaction = new BlueprintFaction { name = "Wizard01431-hostile-faction" };
        void SceneGroup(UnitEntityData unit, BlueprintFaction faction, BlueprintFaction attackFaction) {
            // Native IsEnemy reads the scene-owned group faction caches. Seed
            // those inputs as in Howl01428; do not replace IsEnemy or the
            // attack trigger's full CheckConditions calculation.
            var group = new UnitGroup(unit.UniqueId); Set(unit, "m_Group", group); Set(unit, "m_GroupId", unit.UniqueId);
            Set(unit.Descriptor, "m_Faction", faction);
            ((MultiSet<BlueprintFaction>)Get(group, "m_Factions")).Add(faction); group.AttackFactions.Add(attackFaction);
        }
        var enemy = Unit(cache, "Wizard01431-prediction-enemy", 10); var friend = Unit(cache, "Wizard01431-prediction-friend", 10);
        SceneGroup(u, friendlyFaction, hostileFaction); SceneGroup(enemy, hostileFaction, friendlyFaction); SceneGroup(friend, friendlyFaction, hostileFaction);
        Check(u.IsEnemy(enemy) && enemy.IsEnemy(u) && !u.IsEnemy(friend), "native faction/group calculation recognizes the real enemy and friendly scene inputs");
        var weapon = (ItemEntityWeapon)Call(typeof(SpellAndSword01417Tests), "Weapon", u, WeaponCategory.Longsword, "Wizard01431-prediction-weapon");
        var partySkills = bp.GetComponents<AddInitiatorPartySkillRollTrigger>().Select(c => c.Skill).ToArray();
        Check(partySkills.OrderBy(s => s).SequenceEqual(StatTypeHelper.Skills.OrderBy(s => s)) && partySkills.Distinct().Count() == partySkills.Length, "private party triggers cover every real native skill exactly once");
        PsychicExpansionTests.Inventory["wizard01431PredictionNativeSkills"] = new JArray(partySkills.Select(s => s.ToString()));
        PsychicExpansionTests.Inventory["wizard01431PredictionPartyTriggerCount"] = partySkills.Length;
        void Fire<T>(T rule) where T : RulebookEvent => RulebookEventBus.OnEventDidTrigger(rule);
        void Consumes(string label, Action<Buff> action) {
            int baseline = u.Stats.AdditionalAttackBonus.ModifiedValue, removalEvents = removed; var buff = Add(u, bp, u); Check(u.Stats.AdditionalAttackBonus.ModifiedValue == baseline + 4, "native prediction gives +4 luck"); action(buff);
            Check(buff.IsDisposed && u.Stats.AdditionalAttackBonus.ModifiedValue == baseline && removed == removalEvents + 1, "native next " + label + " removes own effect exactly once and restores stats");
        }
        RuleSkillCheck Skill(StatType skill, bool pass) { var rule = new RuleSkillCheck(u, skill, 20); var stat = u.Stats.GetStat(skill); Check(rule.DC == 20 && rule.DifficultyDCAdjustment == 0 && rule.StatValue == (stat is ModifiableValueAttributeStat attribute ? attribute.Bonus : stat.ModifiedValue), "native skill UpdateValues retains real stat arithmetic and neutral difficulty DC"); Set(rule, "D20", new RuleRollD20(u, pass ? 20 : 1)); Set(rule, "m_Success", pass); return rule; }
        RulePartySkillCheck Party(StatType skill, UnitEntityData roller, bool pass) { var rule = Empty<RulePartySkillCheck>(); Set(rule, "Initiator", u); Set(rule, "Roller", roller); Set(rule, "StatType", skill); Set(rule, "DifficultyClass", 20); Set(rule, "StatCheck", Skill(skill, pass)); return rule; }
        foreach (bool pass in new[] { false, true }) {
            Consumes("enemy attack " + pass, buff => { var rule = new RuleAttackRoll(u, enemy, weapon, 0); Set(rule, "D20", new RuleRollD20(u, pass ? 20 : 1)); Set(rule, "Result", pass ? AttackResult.Hit : AttackResult.Miss); Check(rule.IsHit == pass, "native attack result getter sees the resolved hit/miss input"); Fire(rule); });
            Consumes("skill " + pass, buff => { var rule = Skill(StatType.SkillAthletics, pass); Check(rule.Success == pass, "resolved individual skill result input"); Fire(rule); });
            foreach (var saveType in new[] { SavingThrowType.Fortitude, SavingThrowType.Reflex, SavingThrowType.Will })
                Consumes(saveType + " save " + pass, buff => { var rule = new RuleSavingThrow(u, saveType, 20); Set(rule, "D20", new RuleRollD20(u, pass ? 20 : 1)); Check(rule.IsPassed == pass, "native save result getter sees the resolved natural20/natural1 input"); Fire(rule); });
            foreach (var skill in partySkills) Consumes("party " + skill + " " + pass, buff => { var rule = Party(skill, u, pass); Check(rule.Success == pass, "native party result getter sees its real resolved skill check"); Fire(rule); });
        }
        Set(weapon.Blueprint.Type, "m_AttackType", AttackType.Touch); weapon.Blueprint.Type.OnEnable();
        Consumes("enemy touch attack", buff => { var rule = new RuleAttackRoll(u, enemy, weapon, 0); Set(rule, "D20", new RuleRollD20(u, 20)); Set(rule, "Result", AttackResult.Hit); Fire(rule); });
        var negative = Add(u, bp, u); var fake = new RuleAttackRoll(u, enemy, weapon, 0); Set(fake, "IsFake", true); Fire(fake); Check(!negative.IsDisposed, "native fake-attack guard retains prediction");
        var friendlyTouch = new RuleAttackRoll(u, friend, weapon, 0); Fire(friendlyTouch); Check(!negative.IsDisposed, "native friendly-touch condition retains prediction"); Set(weapon.Blueprint.Type, "m_AttackType", AttackType.Melee); weapon.Blueprint.Type.OnEnable();
        var other = Unit(cache, "Wizard01431-other-party-roller", 10); Fire(Party(StatType.SkillAthletics, other, false)); Check(!negative.IsDisposed, "native party roller-ownership guard retains prediction");
        Fire(Party(StatType.Strength, u, false)); Check(!negative.IsDisposed, "native exact-skill comparison rejects unrelated attribute input"); u.Facts.Remove(negative);
        var cleanupLists = bp.ComponentsArray.Select(c => c is AddInitiatorAttackRollTrigger attack ? attack.Action : c is AddInitiatorSkillRollTrigger skill ? skill.Action : c is AddInitiatorPartySkillRollTrigger party ? party.Action : c is AddInitiatorSavingThrowTrigger save ? save.Action : null).Where(a => a != null).ToArray();
        Check(cleanupLists.Length == partySkills.Length + 3 && cleanupLists.All(a => a.Actions.OfType<ContextActionRemoveBuff>().Single().Buff.AssetGuid == bp.AssetGuid), "all native attack/skill/save/expanded-party handlers remove this private Prediction blueprint");
        var prediction = Asset<BlueprintAbility>(cache, "ProphecyPrediction"); Check(prediction.Type == AbilityType.Supernatural && prediction.ActionType == Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift && ((Feet)Get(prediction.GetComponent<AbilityTargetsAround>(), "m_Radius")).Value == 30, "CotW actual swift thirty-foot adaptation");
        Check(prediction.GetComponent<AudibleAbilityRestriction>() != null && ((ConditionsChecker)Get(prediction.GetComponent<AbilityTargetsAround>(), "m_Condition")).Conditions.Single() is AudibleAbilityTargetCondition, "caster and each recipient hearing restriction retained");
    }
    static void Accordance(BlueprintsCache cache, Harmony h)
    {
        var u = Unit(cache, "Wizard01431-accordance", 8); var bp = Asset<BlueprintBuff>(cache, "ProphecyAccordanceBuff"); var resource = Asset<BlueprintAbilityResource>(cache, "ProphecyAccordanceResource"); u.Resources.Add(resource, true);
        var book = (Spellbook)Call(typeof(SpellResourceFeatTests), "Book", u, true, u.Progression.Classes[0].CharacterClass);
        var spellBp = new BlueprintAbility { name = "Wizard01431-EmpowerFixture", Type = AbilityType.Spell, AvailableMetamagic = Metamagic.Empower, ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Evocation } } }; spellBp.OnEnable();
        var spell = (AbilityData)Call(typeof(SpellResourceFeatTests), "Spell", u, book, 3, spellBp);
        PatchDice(h);
        d100 = 73; Check(new RuleRollD100(u).Result == 73 && new RuleRollD20(u, 7).Result == 7, "controlled independent D100 input leaves the native D20 result getter unchanged");
        h.Patch(AccessTools.Method(typeof(RuleCastSpell), "OnTrigger"), transpiler: new HarmonyMethod(typeof(WizardSubschools01431Tests), nameof(CastBoundary)));
        h.Patch(AccessTools.Method(typeof(AbilityData), "CanTarget", new[] { typeof(TargetWrapper) }), prefix: new HarmonyMethod(typeof(WizardSubschools01431Tests), nameof(TargetAllowed)));
        h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellResourceRecalculatePatch", true)).Patch();
        foreach (int level in new[] { 8, 11, 12, 15, 16, 20 }) foreach (bool fail in new[] { false, true }) {
            u.Progression.Classes[0].Level = level; u.Resources.Restore(resource); var buff = Add(u, bp, u); var runtime = buff.Components.Single(c => c.SourceBlueprintComponent is ProphecyAccordance01431); var component = (ProphecyAccordance01431)runtime.SourceBlueprintComponent;
            Set(book, "m_BaseLevelInternal", level); Set(book, "MaxCasterLevel", 20);
            Buff temporaryMaximize = null;
            if (level == 8 && !fail) temporaryMaximize = NativeMetamagicBuff(u, "Maximize", Metamagic.Maximize);
            var parameters = new RuleCalculateAbilityParams(u, spell) { ReplaceDC = 20, ReplaceCasterLevel = level, ReplaceSpellLevel = 3, ReplaceConcentration = 0, ReplaceStatBonusModifier = 0 };
            Rulebook.Trigger(parameters); Check((parameters.Result.Metamagic & Metamagic.Empower) != 0, "native subscribed AutoMetamagic parameter handling adds Empower without learned feat");
            if (temporaryMaximize != null) Check((parameters.Result.Metamagic & Metamagic.Maximize) != 0, "independent native Maximize is actually present on the originating cast");
            Check((u.Ensure<UnitPartSpecialMetamagic>().GetMetamagic(spell) & Metamagic.Empower) == 0, "constant native registry cannot bypass local cast eligibility");
            int risk = level >= 16 ? 10 : level >= 12 ? 15 : 20; d100 = fail ? risk : risk + 1;
            var rule = (RuleCastSpell)Call(typeof(SpellResourceFeatTests), "Cast", u, spell, false, false); Set(rule, "SpellTarget", new TargetWrapper(u)); Set(rule, "Context", new AbilityExecutionContext(spell, parameters.Result, new TargetWrapper(u))); int before = u.Resources.GetResourceAmount(resource);
            Set(rule, "m_TemporaryModifiers", new List<ModifiableValue.Modifier>());
            Set(rule, "m_CustomData", Activator.CreateInstance(AccessTools.Field(typeof(RulebookEvent), "m_CustomData").FieldType));
            Rulebook.Trigger(rule);
            Check(rule.SpellFailureChance == (fail ? 200 : 0), "independent native fizzle boundary at level " + level);
            Check(rule.Success == !fail, "native RuleCastSpell decides actual fizzle/success");
            Check(rule.ShouldSpendResource, "native cast decision requires spell-slot payment even when prophecy fizzles");
            casts++; resourceSpends++; Check(u.Resources.GetResourceAmount(resource) == before - 1 && buff.IsDisposed, "success/failure each consumes exactly one native resource and removes native fact");
            if (temporaryMaximize != null) u.Facts.Remove(temporaryMaximize);
            rule.Context.Recalculate(); Check((rule.Context.Params.Metamagic & Metamagic.Empower) != 0, "paid cast context retains native Empower after armed buff removal/recalculation");
            HealConsumer(rule.Context, u, true);
            CalculatorReentry(rule.Context, u, book, spell, resource, true);
            if (temporaryMaximize != null) Check((rule.Context.Params.Metamagic & Metamagic.Maximize) == 0, "paid snapshot does not revive the independently expired native Maximize");
            var unrelated = new AbilityExecutionContext(spell, parameters.Result.Clone(), new TargetWrapper(u));
            CalculatorReentry(unrelated, u, book, spell, resource, false);
        }
        EmpowerOwnership(cache, u, book, spell, bp, resource);
        u.Resources.Restore(resource); var cancelled = Add(u, bp, u); int original = u.Resources.GetResourceAmount(resource); u.Facts.Remove(cancelled); Check(u.Resources.GetResourceAmount(resource) == original, "canceling armed prophecy consumes no daily use");
        var noBook = new AbilityData(spellBp, u.Descriptor);
        u.Resources.Spend(resource, original); var empty = Add(u, bp, u); var emptyRuntime = empty.Components.Single(c => c.SourceBlueprintComponent is ProphecyAccordance01431); var emptyHandler = (ProphecyAccordance01431)emptyRuntime.SourceBlueprintComponent;
        var emptyParams = new RuleCalculateAbilityParams(u, spell) { ReplaceDC = 20, ReplaceCasterLevel = 20, ReplaceSpellLevel = 3, ReplaceConcentration = 0, ReplaceStatBonusModifier = 0 };
        Rulebook.Trigger(emptyParams);
        Check((emptyParams.Result.Metamagic & Metamagic.Empower) == 0 && (u.Ensure<UnitPartSpecialMetamagic>().GetMetamagic(spell) & Metamagic.Empower) == 0, "zero daily uses reject Empower through both native parameter and registry paths");
        HealConsumer(new AbilityExecutionContext(spell, emptyParams.Result, new TargetWrapper(u)), u, false);
        CalculatorReentry(new AbilityExecutionContext(spell, emptyParams.Result, new TargetWrapper(u)), u, book, spell, resource, false);
        u.Facts.Remove(empty); u.Resources.Restore(resource);
        var noBookBuff = Add(u, bp, u); var noBookRuntime = noBookBuff.Components.Single(c => c.SourceBlueprintComponent is ProphecyAccordance01431);
        var noBookParams = new RuleCalculateAbilityParams(u, noBook) { ReplaceDC = 20, ReplaceCasterLevel = 20, ReplaceSpellLevel = 3, ReplaceConcentration = 0, ReplaceStatBonusModifier = 0 };
        Rulebook.Trigger(noBookParams);
        Check(noBook.Spellbook == null && (noBookParams.Result.Metamagic & Metamagic.Empower) == 0 && (u.Ensure<UnitPartSpecialMetamagic>().GetMetamagic(noBook) & Metamagic.Empower) == 0, "bookless spell does not receive unpaid Empower through native registry or parameters");
        HealConsumer(new AbilityExecutionContext(noBook, noBookParams.Result, new TargetWrapper(u)), u, false);
        u.Facts.Remove(noBookBuff);
        var nonspell = new AbilityData(new BlueprintAbility { Type = AbilityType.Supernatural, ComponentsArray = Array.Empty<BlueprintComponent>() }, u.Descriptor);
        var inert = Add(u, bp, u); var rt = inert.Components.Single(c => c.SourceBlueprintComponent is ProphecyAccordance01431); var handler = (ProphecyAccordance01431)rt.SourceBlueprintComponent; var excluded = (RuleCastSpell)Call(typeof(SpellResourceFeatTests), "Cast", u, nonspell, false, false);
        using (rt.RequestEventContext()) { handler.OnEventAboutToTrigger(excluded); handler.OnEventDidTrigger(excluded); } Check(!inert.IsDisposed && u.Resources.GetResourceAmount(resource) == original, "supernatural abilities do not consume armed prophecy");
        var duplicate = (RuleCastSpell)Call(typeof(SpellResourceFeatTests), "Cast", u, spell, false, true); using (rt.RequestEventContext()) { handler.OnEventAboutToTrigger(duplicate); handler.OnEventDidTrigger(duplicate); } Check(!inert.IsDisposed && u.Resources.GetResourceAmount(resource) == original, "duplicate casts do not consume prophecy"); u.Facts.Remove(inert);
    }
    static void PatchDice(Harmony h) { h.Patch(AccessTools.PropertyGetter(typeof(RuleRollDice), "Result"), prefix: new HarmonyMethod(typeof(WizardSubschools01431Tests), nameof(Dice))); }
    static void HealConsumer(MechanicsContext context, UnitEntityData owner, bool empowered)
    {
        var action = new ContextActionHealTarget { Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 2 } };
        using (context.GetDataScope(owner)) {
            var rule = (RuleHealDamage)AccessTools.Method(typeof(ContextActionHealTarget), "GetHealRule", new[] { typeof(UnitEntityData), typeof(UnitEntityData) }).Invoke(action, new object[] { owner, owner });
            Check(rule.Empower.Value == empowered && (!empowered || rule.Empower.Metamagic == Metamagic.Empower), "native healing effect consumer reads the actual execution-context Empower");
        }
    }
    static void CalculatorReentry(AbilityExecutionContext context, UnitEntityData owner, Spellbook book, AbilityData spell, BlueprintAbilityResource resource, bool paid)
    {
        var native = Rulebook.Trigger(new RuleCalculateAbilityParams(owner, spell)).Result;
        int uses = owner.Resources.GetResourceAmount(resource), slots = book.GetSpontaneousSlots(3);
        Set(context, "m_ParamsCalculator", new ContextSetAbilityParams());
        AccessTools.Method(typeof(MechanicsContext), "RecalculateAbilityParams", new[] { typeof(bool) }).Invoke(context, new object[] { false });
        Check((context.Params.Metamagic & Metamagic.Empower) == 0, "real native calculator reproduces consumed-buff Empower loss before the existing postfix");
        context.Recalculate();
        Check(((context.Params.Metamagic & Metamagic.Empower) != 0) == paid, "real calculator re-entry restores only paid contexts eligible for this module's Empower");
        Check(context.Params.DC == native.DC && context.Params.CasterLevel == native.CasterLevel && context.Params.SpellLevel == native.SpellLevel && context.Params.Concentration == native.Concentration, "paid restoration does not replace native DC, caster level, spell level or concentration");
        Check(owner.Resources.GetResourceAmount(resource) == uses && book.GetSpontaneousSlots(3) == slots, "recalculation does not spend another resource or slot");
        HealConsumer(context, owner, paid);
        if (paid) {
            var child = new MechanicsContext(owner, owner.Descriptor, spell.Blueprint, context); child.SetParams(native.Clone()); Set(child, "m_ParamsCalculator", new ContextSetAbilityParams()); child.Recalculate();
            Check((child.Params.Metamagic & Metamagic.Empower) != 0 && child.Params.DC == native.DC, "descendant effect context matches only its paid originating ability context");
            HealConsumer(child, owner, true);
        }
    }
    static Buff NativeMetamagicBuff(UnitEntityData owner, string name, Metamagic metamagic)
    {
        var native = new AutoMetamagic { Metamagic = metamagic, Once = false, Abilities = new List<BlueprintAbilityReference>() };
        Set(native, "m_AllowedAbilities", Enum.Parse(AccessTools.Field(typeof(AutoMetamagic), "m_AllowedAbilities").FieldType, "SpellOnly")); Set(native, "m_IncludeClasses", Array.Empty<BlueprintCharacterClassReference>()); Set(native, "m_ExcludeClasses", Array.Empty<BlueprintCharacterClassReference>());
        var blueprint = new BlueprintBuff { name = "Wizard01431-native-" + name + "-fixture", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = new BlueprintComponent[] { native } }; blueprint.OnEnable(); return Add(owner, blueprint, owner);
    }
    static RuleCastSpell NativeCast(UnitEntityData owner, AbilityData ability, AbilityParams parameters)
    {
        var rule = (RuleCastSpell)Call(typeof(SpellResourceFeatTests), "Cast", owner, ability, false, false); Set(rule, "SpellTarget", new TargetWrapper(owner)); Set(rule, "Context", new AbilityExecutionContext(ability, parameters, new TargetWrapper(owner)));
        Set(rule, "m_TemporaryModifiers", new List<ModifiableValue.Modifier>()); Set(rule, "m_CustomData", Activator.CreateInstance(AccessTools.Field(typeof(RulebookEvent), "m_CustomData").FieldType)); return Rulebook.Trigger(rule);
    }
    static void EmpowerOwnership(BlueprintsCache cache, UnitEntityData owner, Spellbook book, AbilityData spell, BlueprintBuff buffBlueprint, BlueprintAbilityResource resource)
    {
        var learnedBlueprint = Hydrate<BlueprintFeature>(cache, "a1de1e4f92195b442adb946f0e2b9d4e"); var learned = (Feature)learnedBlueprint.CreateFact(null, owner.Descriptor, null); owner.Facts.Add(learned);
        spell.MetamagicData = new MetamagicData(); spell.MetamagicData.Add(Metamagic.Empower); owner.Resources.Restore(resource); var armed = Add(owner, buffBlueprint, owner);
        Check(owner.HasFact(learnedBlueprint) && learnedBlueprint.GetComponent<AddMetamagicFeat>().Metamagic == Metamagic.Empower, "already-empowered positive case owns the actual native Empower feat");
        var parameters = Rulebook.Trigger(new RuleCalculateAbilityParams(owner, spell) { ReplaceCasterLevel = 20, ReplaceSpellLevel = 3, ReplaceDC = 20, ReplaceConcentration = 0, ReplaceStatBonusModifier = 0 }).Result;
        Check(parameters.Metamagic == Metamagic.Empower && parameters.SpellLevel == 3, "native parameter handling keeps an already-empowered eligible spell at one Empower bit");
        int before = owner.Resources.GetResourceAmount(resource); d100 = 100; var cast = NativeCast(owner, spell, parameters); casts++; resourceSpends++;
        Check(cast.Success && armed.IsDisposed && owner.Resources.GetResourceAmount(resource) == before - 1 && cast.Context.Params.Metamagic == Metamagic.Empower, "eligible already-empowered spell consumes one prophecy use without duplicating Empower"); HealConsumer(cast.Context, owner, true);
        spell.MetamagicData = null; owner.Facts.Remove(learned);
        var incompatibleBlueprint = new BlueprintAbility { name = "Wizard01431-not-Empower-eligible", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), Type = AbilityType.Spell, AvailableMetamagic = 0, ComponentsArray = new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Divination } } }; incompatibleBlueprint.OnEnable();
        var incompatible = (AbilityData)Call(typeof(SpellResourceFeatTests), "Spell", owner, book, 3, incompatibleBlueprint); owner.Resources.Restore(resource); armed = Add(owner, buffBlueprint, owner);
        var alone = Rulebook.Trigger(new RuleCalculateAbilityParams(owner, incompatible) { ReplaceCasterLevel = 20, ReplaceSpellLevel = 3, ReplaceDC = 20, ReplaceConcentration = 0, ReplaceStatBonusModifier = 0 }).Result;
        Check((alone.Metamagic & Metamagic.Empower) == 0, "this component cannot Empower a spell whose native availability excludes Empower");
        var external = NativeMetamagicBuff(owner, "foreign-Empower", Metamagic.Empower);
        parameters = Rulebook.Trigger(new RuleCalculateAbilityParams(owner, incompatible) { ReplaceCasterLevel = 20, ReplaceSpellLevel = 3, ReplaceDC = 20, ReplaceConcentration = 0, ReplaceStatBonusModifier = 0 }).Result;
        Check((parameters.Metamagic & Metamagic.Empower) != 0, "independent native source actually supplies the unrelated Empower bit");
        before = owner.Resources.GetResourceAmount(resource); cast = NativeCast(owner, incompatible, parameters); casts++; resourceSpends++;
        Check(cast.Success && armed.IsDisposed && owner.Resources.GetResourceAmount(resource) == before - 1, "ineligible spell still completes its declared prophecy risk/use transaction"); owner.Facts.Remove(external);
        CalculatorReentry(cast.Context, owner, book, incompatible, resource, false);
        Check((cast.Context.Params.Metamagic & Metamagic.Empower) == 0, "paid prophecy never claims or revives another source's Empower on an ineligible spell");
    }
}
