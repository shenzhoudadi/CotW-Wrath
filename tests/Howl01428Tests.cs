using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.Settings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Narrow scene boundaries, with real native condition, save and buff-permission
// resolution. In particular neither IsPassed nor ContextActionApplyBuff is mocked.
internal static class Howl01428Tests
{
    static int checks, saveCalls, resistanceCalls, permissionCalls, buffCalls, fxCalls, conditionAttempts;
    static int d20 = 1, d4 = 3;
    static bool throwUi;
    static RulebookEventContext current;
    static AbilityExecutionContext executing;
    static UnitEntityData[] nearby = Array.Empty<UnitEntityData>();
    static readonly HashSet<UnitEntityData> enemies = new HashSet<UnitEntityData>();
    static readonly Dictionary<UnitEntityData, UnitGroup> groups = new Dictionary<UnitEntityData, UnitGroup>();
    static readonly Dictionary<UnitGroup, UnitEntityData> groupOwners = new Dictionary<UnitGroup, UnitEntityData>();
    static readonly BlueprintFaction fixtureFaction = new BlueprintFaction { name = "01428FriendlyFaction", AssetGuid = BlueprintGuid.Parse("74241428100000000000000000000001"), Neutral = false };
    static readonly BlueprintFaction hostileFaction = new BlueprintFaction { name = "01428HostileFaction", AssetGuid = BlueprintGuid.Parse("74241428100000000000000000000002"), Neutral = false };
    static readonly List<Buff> installed = new List<Buff>();
    static readonly List<RuleSavingThrow> saves = new List<RuleSavingThrow>();
    static readonly List<RuleCanApplyBuff> permissions = new List<RuleCanApplyBuff>();
    static readonly List<string> logMessages = new List<string>();
    static readonly List<string> uiMessages = new List<string>();
    static readonly MethodInfo Process = AccessTools.Method(typeof(AbilityExecutionProcess), "ApplyEffect", new[] { typeof(AbilityExecutionContext), typeof(TargetWrapper), typeof(AbilityApplyEffect), typeof(AbilitySelectTarget), typeof(bool) });
    static readonly MethodInfo Add = AccessTools.Method(typeof(BuffCollection), "AddBuff", new[] { typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Helper<T>(string n, params object[] a) => (T)AccessTools.Method(typeof(Migration01426Tests), n).Invoke(null, a);
    static BlueprintGuid Id(string t) => Helper<BlueprintGuid>("Id", t);
    static T Asset<T>(BlueprintsCache c, string t) where T : SimpleBlueprint => (T)c.Load(Id(t));
    static void Check(bool value, string reason) { if (!value) throw new Exception("Howl01428: " + reason); checks++; }
    static bool Skip() => false;
    static bool Log(string message) { logMessages.Add(message); return false; }
    static bool Name(UnitEntityData __instance, ref string __result) { __result = __instance.UniqueId; return false; }
    static bool False(ref bool __result) { __result = false; return false; }
    static bool Context(ref RulebookEventContext __result) { if (current == null) return true; __result = current; return false; }
    static bool Dice(DiceFormula __0, ref int __result) { __result = __0.Dice == DiceType.D20 ? d20 : __0.Dice == DiceType.D4 ? d4 : 0; return false; }
    static bool ResistanceDie(ref RuleRollD20 __result) { __result = new RuleRollD20(current.LastEvent<RulebookEvent>().Initiator); NativeRule(__result); return false; }
    static bool Nearby(UnityEngine.Vector3 __0, Feet __1, bool __2, bool __3, ref IEnumerable<UnitEntityData> __result) { Check(__1.Value == 30 && __2 && !__3, "native selector requests 30 feet, LOS and live targets"); __result = nearby; return false; }
    static bool Fx(AbilitySpawnFx __instance) { Check(__instance.PrefabLink.AssetId == "d80d51c0a08f35140b10dd1526e540c4", "native cast FX endpoint"); fxCalls++; return false; }
    static bool Expose(Exception __1) { throw new Exception("Howl01428 native action error", __1); }
    static StatsAdjustmentsType Difficulty() => default(StatsAdjustmentsType);
    static string Label(LocalizedString value) { var strings = (Dictionary<string, string>)AccessTools.Field(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Strings").GetValue(null); string text = strings[value.Key]; value.ShouldProcess = value.ShouldProcess || text.Contains("{"); return text; }
    static IEnumerable<CodeInstruction> DifficultyBoundary(IEnumerable<CodeInstruction> input) {
        var c = input.ToList(); int start = c.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(SettingsRoot) && m.Name == "get_Difficulty");
        if (start < 0) throw new Exception("Native RuleCanApplyBuff difficulty boundary changed");
        int end = c.FindIndex(start, i => i.operand is MethodInfo m && m.Name == "op_Implicit");
        if (end < start || end - start > 4) throw new Exception("Unexpected native difficulty getter chain");
        var replacement = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Howl01428Tests), nameof(Difficulty))); replacement.labels.AddRange(c[start].labels);
        c.RemoveRange(start, end - start + 1); c.Insert(start, replacement); return c;
    }
    sealed class Observer : IApplyAbilityEffectHandler, IUnitConditionChangeAttemptHandler, IUnitConditionsChanged, IWasRoll, ISavingThrowFailed, ILogMessageUIHandler {
        public void HandleLogMessage(string message) { if (throwUi) throw new InvalidOperationException("01428 injected UI sink failure"); uiMessages.Add(message); }
        public void OnAbilityEffectApplied(AbilityExecutionContext c) { }
        public void OnTryToApplyAbilityEffect(AbilityExecutionContext c, TargetWrapper t) { }
        public void OnAbilityEffectAppliedToTarget(AbilityExecutionContext c, TargetWrapper t) { }
        public void HandleUnitConditionAddAttempt(UnitEntityData u, UnitCondition c, bool success) { conditionAttempts++; }
        public void HandleUnitConditionsChanged(UnitEntityData u, UnitCondition c) { }
        public void WasRoll(RulebookEvent e, RuleRollD20 roll) { }
        public void HandleSavingThrowFailed(RuleSavingThrow e) { }
    }
    static bool Raise<T>(Action<T> action, bool unused) {
        var observer = new Observer(); if (observer is T typed) action(typed); return true;
    }
    static T ContextRule<T>(MechanicsContext context, T rule) where T : RulebookEvent { rule.Reason = context; return NativeRule(rule); }
    static T NativeRule<T>(T rule) where T : RulebookEvent {
        var old = current; current = new RulebookEventContext(); current.PushEvent(rule);
        try {
            if (rule is RuleSavingThrow saving) {
                saveCalls++; saves.Add(saving);
                Check(saving.Type == SavingThrowType.Will && saving.DifficultyClassBase == 18, "native Will save has expected DC18");
            }
            if (rule is RuleSpellResistanceCheck) resistanceCalls++;
            if (!(rule is RuleApplySpell)) rule.OnTrigger(current);
            return rule;
        } finally { current = old; }
    }
    static IEnumerable<CodeInstruction> Boundary(IEnumerable<CodeInstruction> input) {
        foreach (var c in input) {
            if (c.operand is MethodInfo local && local.DeclaringType == typeof(LocalizedString) && (local.Name == "op_Implicit" || local.Name == "ToString")) { c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(Howl01428Tests), nameof(Label)); }
            else if (c.operand is MethodInfo m && m.IsGenericMethod) {
                if (m.DeclaringType == typeof(Rulebook) && m.Name == "Trigger") { c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(Howl01428Tests), nameof(NativeRule)).MakeGenericMethod(m.GetGenericArguments()); }
                else if (m.DeclaringType == typeof(MechanicsContext) && m.Name == "TriggerRule") { c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(Howl01428Tests), nameof(ContextRule)).MakeGenericMethod(m.GetGenericArguments()); }
                else if (m.DeclaringType == typeof(EventBus) && m.Name == "RaiseEvent" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(bool)) { c.opcode = OpCodes.Call; c.operand = AccessTools.Method(typeof(Howl01428Tests), nameof(Raise)).MakeGenericMethod(m.GetGenericArguments()); }
            }
            yield return c;
        }
    }
    static IEnumerable<CodeInstruction> FeedbackBoundary(IEnumerable<CodeInstruction> input) {
        var code = Boundary(input).ToList();
        for (int i = 1; i < code.Count; i++) {
            if (code[i - 1].operand is FieldInfo f && f.FieldType == typeof(LocalizedString)
                && code[i].operand is MethodInfo m && m.Name == "ToString" && m.DeclaringType == typeof(object)) {
                code[i].opcode = OpCodes.Call;
                code[i].operand = AccessTools.Method(typeof(Howl01428Tests), nameof(Label));
            }
        }
        return code;
    }
    static void Refresh(UnitEntityData u) { var c = Get(u.Facts, "m_Cache"); AccessTools.Method(c.GetType(), "Clear").Invoke(c, null); }
    static UnitEntityData Unit(BlueprintsCache cache, string name) {
        var u = Helper<UnitEntityData>("Unit", cache, name);
        int size = ((sbyte[])Get(u.State, "m_Conditions")).Length;
        Set(u.State, "m_ConditionsExceptions", new List<UnitConditionExceptions>[size]);
        u.Stats.Wisdom.BaseValue = 10; u.Stats.SaveWill.BaseValue = 0; u.Stats.Strength.BaseValue = 18;
        var row = PsychicExpansionTests.NativeRows.First(r => (string)r["name"] == "BarbarianClass");
        var cls = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse((string)row["blueprint"]["AssetId"]));
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData(cls) { Level = 8 });
        Set(u, "m_Position", new UnityEngine.Vector3(groups.Count * 0.5f, 0, 0));
        // Seed the native group as a real scene-owned object. Earlier Harmony
        // calls can inline the getter, so patching only get_Group is insufficient.
        var group = new UnitGroup(name); Set(u, "m_Group", group); Set(u, "m_GroupId", name);
        groups[u] = group; groupOwners[group] = u;
        Set(u.Descriptor, "m_Faction", fixtureFaction);
        ((MultiSet<BlueprintFaction>)Get(group, "m_Factions")).Add(fixtureFaction);
        group.AttackFactions.Add(hostileFaction);
        return u;
    }
    static void Hostile(UnitEntityData unit) {
        enemies.Add(unit); var group = groups[unit];
        Set(unit.Descriptor, "m_Faction", hostileFaction);
        var factions = (MultiSet<BlueprintFaction>)Get(group, "m_Factions"); factions.Remove(fixtureFaction); factions.Add(hostileFaction);
        group.AttackFactions.Clear(); group.AttackFactions.Add(fixtureFaction);
    }
    static void Conditions(Buff fact, bool active) {
        foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is AddCondition || r.SourceBlueprintComponent is AddConditionImmunity))
            using (runtime.RequestEventContext()) AccessTools.Method(runtime.SourceBlueprintComponent.GetType(), active ? "OnTurnOn" : "OnTurnOff").Invoke(runtime.SourceBlueprintComponent, null);
    }
    static Buff Fact(UnitEntityData u, BlueprintBuff bp, UnitEntityData caster, bool conditions = true) {
        var f = Psychic034Tests.Fact(u, bp, caster, 8); if (conditions) Conditions(f, true); return f;
    }
    static void Remove(Buff f) { Conditions(f, false); f.Owner.Facts.List.Remove(f); Refresh(f.Owner); Set(f, "IsTurnedOn", false); }
    // Scene insertion alone is bounded. CloneFor, actual RuleCanApplyBuff and the
    // native runtime AddCondition components execute, so status effects are real
    // counters rather than a manually written m_Conditions positive-test input.
    static bool Insert(BuffCollection __instance, BlueprintBuff blueprint, MechanicsContext parentContext, TimeSpan? duration, ref Buff __result) {
        buffCalls++;
        var owner = __instance.Owner.Unit; var ctx = parentContext.CloneFor(blueprint, owner.Descriptor, null);
        var can = new RuleCanApplyBuff(owner, blueprint, ctx, duration); permissions.Add(can); permissionCalls++;
        foreach (var f in owner.Buffs.Enumerable.Where(b => b.IsTurnedOn && !b.IsSuppressed))
            foreach (var runtime in f.Components.Where(r => r.SourceBlueprintComponent is BuffDescriptorImmunity))
                using (runtime.RequestEventContext()) ((BuffDescriptorImmunity)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(can);
        NativeRule(can);
        if (!can.CanApply) { __result = null; return false; }
        __result = Fact(owner, blueprint, parentContext.MaybeCaster, false); Set(__result, "m_Context", ctx);
        Set(__result, "PlannedDuration", can.Duration); Set(__result, "m_EndTime", can.Duration.HasValue ? (TimeSpan?)(TimeSpan.FromSeconds(100) + can.Duration.Value) : null);
        Conditions(__result, true); installed.Add(__result); return false;
    }
    static AbilityExecutionContext Cast(BlueprintAbility ability, UnitEntityData caster) {
        int beforeUi = uiMessages.Count, beforeLog = logMessages.Count;
        var data = new AbilityData(ability, caster.Descriptor);
        Check(uiMessages.Count == beforeUi && logMessages.Count == beforeLog, "ability data/preview construction emits no cast messages");
        Check(ability.GetComponent<AbilityCasterHasFacts>().IsCasterRestrictionPassed(caster), "native caster rage requirement is satisfied for effect test");
        executing = new AbilityExecutionContext(data, new AbilityParams { CasterLevel = 8, DC = 18 }, new TargetWrapper(caster), null, caster);
        AbilityExecutionProcess.PrepareCast(executing);
        if (ability.ComponentsArray.Any(c => c.GetType().Name == "HowlCastFeedback")) {
            Check(uiMessages.Count == beforeUi + (throwUi ? 0 : 1), "native PrepareCast emits one summary to UI sink, unless isolated sink failure: ui=" + uiMessages.Count + "/before=" + beforeUi + "; log=" + string.Join("|", logMessages.Skip(beforeLog)));
            Check(logMessages.Count == beforeLog + (throwUi ? 2 : 1), "native PrepareCast writes diagnostic and isolates UI failure");
        } else Check(uiMessages.Count == beforeUi && logMessages.Count == beforeLog, "detached diagnostic component emits no extra messages");
        Check(executing.Params.CasterLevel == 8 && executing.Params.DC == 18, "native PrepareCast recalculates own barbarian8/Strength18 DC18");
        Check(executing.SpellDescriptor.HasAnyFlag(SpellDescriptor.Fear | SpellDescriptor.MindAffecting), "native PrepareCast carries ability descriptors");
        executing.FxSpawners = ability.GetComponents<AbilitySpawnFx>().ToList();
        Check(executing.MainTarget.Unit == caster && executing.ClickedTarget.Unit == caster, "real personal click and main target are caster");
        Process.Invoke(null, new object[] { executing, new TargetWrapper(caster), ability.GetComponent<AbilityEffectRunAction>(), ability.GetComponent<AbilityTargetsAround>(), false });
        return executing;
    }
    static BlueprintBuff Immunity(string name, BlueprintComponent component) {
        var b = new BlueprintBuff { name = name, ComponentsArray = new[] { component } }; component.name = name + "Component"; b.OnEnable(); return b;
    }
    internal static int Run(BlueprintsCache cache) {
        checks = saveCalls = resistanceCalls = permissionCalls = buffCalls = fxCalls = conditionAttempts = 0;
        throwUi = false; current = null; enemies.Clear(); groups.Clear(); groupOwners.Clear(); installed.Clear(); saves.Clear(); permissions.Clear(); logMessages.Clear(); uiMessages.Clear();
        var h = new Harmony("CotW01428.HowlNativeResolution");
        var buffHelper = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var outerCapture = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense");
        var outerOwner = Harmony.GetPatchInfo(buffHelper)?.Prefixes.FirstOrDefault(p => p.PatchMethod == outerCapture)?.owner;
        if (outerOwner != null) h.Unpatch(buffHelper, outerCapture);
        void Patch(MethodBase m, string n) { if (m == null) throw new Exception("Missing Howl01428 endpoint " + n); h.Patch(m, prefix: new HarmonyMethod(typeof(Howl01428Tests), n) { priority = Priority.First }); }
        void Transpile(MethodBase m) { h.Patch(m, transpiler: new HarmonyMethod(typeof(Howl01428Tests), nameof(Boundary))); }
        Patch(AccessTools.Method(typeof(GameHelper), "GetTargetsAround", new[] { typeof(UnityEngine.Vector3), typeof(Feet), typeof(bool), typeof(bool) }), nameof(Nearby));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "CharacterName"), nameof(Name));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayerFaction"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), nameof(Context));
        Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), nameof(Dice));
        Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), nameof(ResistanceDie));
        Patch(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), nameof(Skip));
        Patch(AccessTools.Method(typeof(AbilitySpawnFx), "Spawn"), nameof(Fx));
        Patch(AccessTools.Method(typeof(GameHelper), "IsAttackingGreenNPC"), nameof(False));
        Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension), "SkipBecauseOfShadow"), nameof(False));
        Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(Expose));
        Patch(Add, nameof(Insert));
        var production = typeof(HowlShakenTarget).Assembly;
        Patch(AccessTools.Method(production.GetType("CallOfTheWild.Wrath.Prototype.ModEntry"), "LogSpellDiagnostic"), nameof(Log));
        var feedback = production.GetType("CallOfTheWild.Wrath.Prototype.HowlCastFeedback");
        if (feedback != null) h.Patch(AccessTools.Method(feedback, "OnCast"), transpiler: new HarmonyMethod(typeof(Howl01428Tests), nameof(FeedbackBoundary)));
        Transpile(AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }));
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        foreach (var m in new[] { Process, AccessTools.Method(typeof(AbilityExecutionProcess), "DoApplyEffect"), AccessTools.Method(typeof(ContextActionSavingThrow), "RunAction"), AccessTools.Method(typeof(RuleSavingThrow), "OnTrigger"), AccessTools.Method(typeof(RuleSavingThrow), "RollD20"), AccessTools.Method(typeof(RuleSpellResistanceCheck), "OnTrigger"), AccessTools.Method(typeof(ContextDiceValue), "Calculate", new[] { typeof(MechanicsContext) }) }) Transpile(m);
        foreach (var m in typeof(UnitState).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => new[] { "AddCondition", "RemoveCondition", "AddConditionImmunity", "RemoveConditionImmunity" }.Contains(m.Name))) Transpile(m);
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Howl01428Tests), nameof(DifficultyBoundary)));
        try {
            var ability = Asset<BlueprintAbility>(cache, "TerrifyingHowlAbility");
            bool hasFeedback = ability.ComponentsArray.Any(c => c.GetType().Name == "HowlCastFeedback");
            var definition = AccessTools.Method(production.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition", new[] { typeof(object) });
            string beforeDefinition = (string)definition.Invoke(null, new object[] { ability.ComponentsArray });
            var shaken = PsychicExpansionTests.NativeCase<BlueprintBuff>("25ec6cb6ab1845c48a95f9c20b034220");
            var nativeFear = (BlueprintBuff)cache.Load(BlueprintGuid.Parse("f08a7239aa961f34c8301518e71d4cdf"));
            Check(shaken.GetComponent<AddCondition>().Condition == UnitCondition.Shaken && nativeFear.GetComponent<AddCondition>().Condition == UnitCondition.Frightened, "native game conditions audited from Shaken and Frightened blueprints");
            var caster = Unit(cache, "01428-howler"); var target = Unit(cache, "01428-enemy"); Hostile(target); nearby = new[] { target };
            var rageIds = (string[])AccessTools.Field(production.GetType("CallOfTheWild.Wrath.Prototype.RagePowers"), "HostIds").GetValue(null);
            var rageBuff = (BlueprintBuff)cache.Load(BlueprintGuid.Parse(rageIds[0]));
            Check(!ability.GetComponent<AbilityCasterHasFacts>().IsCasterRestrictionPassed(caster), "native ability rejects a caster outside rage");
            Fact(caster, rageBuff, caster, false);
            bool nativeEnemy = caster.IsEnemy(target), nativeAttack = caster.CanAttack(target);
            Check(nativeEnemy && nativeAttack, "native IsEnemy and CanAttack accept hostile group: enemy=" + nativeEnemy + "; attack=" + nativeAttack);
            int before = saveCalls; int fx = fxCalls; Cast(ability, caster);
            Check(saveCalls == before && !target.State.HasCondition(UnitCondition.Frightened) && fxCalls > fx, "027 symptom reproduced when required Shaken is absent: FX and no save or fear");
            var shake = Fact(target, shaken, caster); Check(target.State.HasCondition(UnitCondition.Shaken), "native Shaken OnTurnOn enables prerequisite");
            d20 = 1; Cast(ability, caster);
            Check(saveCalls == before + 1 && !saves.Last().IsPassed && saves.Last().D20.Result == 1 && target.State.HasCondition(UnitCondition.Frightened), "real native failed save results in real Frightened condition: saves=" + saveCalls + "/before=" + before + "; passed=" + (saves.Count == 0 ? "none" : saves.Last().IsPassed.ToString()) + "; d20=" + (saves.Count == 0 ? "none" : saves.Last().D20.Result.ToString()) + "; buffs=" + buffCalls + "; permissions=" + string.Join(",", permissions.Select(x => x.Blueprint.name + ":" + x.CanApply + ":" + x.Immunity)) + "; frightened=" + target.State.HasCondition(UnitCondition.Frightened));
            var fear = installed.Last(b => b.Blueprint.AssetGuid == nativeFear.AssetGuid);
            Check(fear.PlannedDuration == TimeSpan.FromSeconds(24) && fear.IsNotDispelable, "real native d4 duration is four rounds and nondispellable");
            var cooldown = installed.Last(b => b.Blueprint.AssetGuid == Id("TerrifyingHowlImmunity"));
            Check(cooldown.PlannedDuration == TimeSpan.FromDays(1) && cooldown.Context.MaybeCaster == caster, "real inserted cooldown has 24h and actual caster context");
            before = saveCalls; fx = fxCalls; Cast(ability, caster); Check(saveCalls == before && fxCalls > fx, "same caster cooldown reproduces FX with no new save");
            var second = Unit(cache, "01428-second"); Fact(second, rageBuff, second, false); d20 = 20; Cast(ability, second);
            Check(saveCalls == before + 1 && saves.Last().IsPassed && saves.Last().D20.Result == 20, "native natural20 succeeds, separate caster has independent save");
            Check(installed.Count(b => b.Blueprint.AssetGuid == Id("TerrifyingHowlImmunity")) == 2, "success and failure both insert separate caster immunity");
            Remove(fear); Remove(cooldown); before = saveCalls; d20 = 1; Cast(ability, caster); Check(saveCalls == before + 1 && target.State.HasCondition(UnitCondition.Frightened), "expiry/removal of one source permits it again");
            before = saveCalls; Cast(ability, second); Check(saveCalls == before, "other caster immunity remains after first expiry");
            Remove(shake); Check(!target.State.HasCondition(UnitCondition.Shaken), "native Shaken OnTurnOff removes prerequisite");
            var ally = Unit(cache, "01428-ally"); Fact(ally, shaken, caster); nearby = new[] { ally }; before = saveCalls; Cast(ability, caster); Check(saveCalls == before, "native selector excludes shaken ally");
            var unshakable = Unit(cache, "01428-shakenimmune"); Hostile(unshakable); nearby = new[] { unshakable };
            var conditionGuard = Fact(unshakable, Immunity("01428ShakenImmunity", new AddConditionImmunity { Condition = UnitCondition.Shaken }), unshakable);
            Fact(unshakable, shaken, caster); before = saveCalls; Cast(ability, caster); Check(!unshakable.State.HasCondition(UnitCondition.Shaken) && saveCalls == before, "native condition immunity prevents eligibility despite a Shaken fact");
            Remove(conditionGuard); Check(unshakable.State.HasCondition(UnitCondition.Shaken), "removing native immunity exposes remaining Shaken condition");
            d20 = 20; Cast(ability, caster); Check(saveCalls == before + 1 && saves.Last().IsPassed && !unshakable.State.HasCondition(UnitCondition.Frightened), "native successful save inserts cooldown without fear");
            var fearImmune = Unit(cache, "01428-frightenedimmune"); Hostile(fearImmune); nearby = new[] { fearImmune }; Fact(fearImmune, shaken, caster);
            Fact(fearImmune, Immunity("01428FrightenedImmunity", new AddConditionImmunity { Condition = UnitCondition.Frightened }), fearImmune);
            d20 = 1; before = saveCalls; Cast(ability, caster); Check(saveCalls == before + 1 && !fearImmune.State.HasCondition(UnitCondition.Frightened), "native Frightened immunity remains effective after failing the actual save");
            var descriptorImmune = Unit(cache, "01428-descriptorimmune"); Hostile(descriptorImmune); nearby = new[] { descriptorImmune }; Fact(descriptorImmune, shaken, caster);
            Fact(descriptorImmune, Immunity("01428MindBuffImmunity", new BuffDescriptorImmunity { Descriptor = SpellDescriptor.MindAffecting }), descriptorImmune);
            before = saveCalls; Cast(ability, caster); Check(saveCalls == before + 1 && !descriptorImmune.State.HasCondition(UnitCondition.Frightened), "native buff descriptor immunity rejects fear after save");
            Check(permissions.Any(p => p.Initiator == descriptorImmune && p.Blueprint == nativeFear && p.Immunity && !p.CanApply), "actual RuleCanApplyBuff records descriptor rejection");
            var spellImmune = Unit(cache, "01428-spellimmune"); Hostile(spellImmune); nearby = new[] { spellImmune }; Fact(spellImmune, shaken, caster);
            var resistance = spellImmune.Ensure<UnitPartSpellResistance>(); resistance.AddImmunity(SpellImmunityType.SpellDescriptor, AlignmentComponent.None, Array.Empty<BlueprintAbility>(), SpellDescriptor.MindAffecting, null);
            before = saveCalls; Cast(ability, caster); Check(saveCalls == before + 1, "native personal-AOE MainTarget caster follows native spell-immunity convention");
            var direct = new AbilityExecutionContext(new AbilityData(ability, caster.Descriptor), new AbilityParams { CasterLevel = 8, DC = 18 }, new TargetWrapper(spellImmune), null, caster);
            direct.Recalculate(); var sr = NativeRule(new RuleSpellResistanceCheck(direct, spellImmune)); Check(sr.TargetIsImmune && sr.IsSpellResisted, "same native descriptor spell immunity applies with hostile MainTarget: targetImmune=" + sr.TargetIsImmune + "; resisted=" + sr.IsSpellResisted + "; part=" + ReferenceEquals(resistance, spellImmune.Get<UnitPartSpellResistance>()) + "; directImmune=" + resistance.IsImmune(direct) + "; caster=" + direct.MaybeCaster?.UniqueId + "; target=" + direct.MainTarget.Unit?.UniqueId + "; descriptor=" + direct.SpellDescriptor + "; entries=" + resistance.Immunities.Count + "; alignment=" + caster.Alignment.ValueRaw.HasComponent(AlignmentComponent.None));
            nearby = Array.Empty<UnitEntityData>(); before = saveCalls; fx = fxCalls; Cast(ability, caster); Check(saveCalls == before && fxCalls > fx, "empty native scene candidate list gives cast FX without inventing saves");
            {
                if (hasFeedback) Check(uiMessages.Last().Contains("no enemy targets"), "no-target diagnostic explains absent saving throws");
                var eligible = Unit(cache, "01428-mixed-eligible"); Hostile(eligible); Fact(eligible, shaken, caster);
                var unshaken = Unit(cache, "01428-mixed-unshaken"); Hostile(unshaken);
                var silenced = Unit(cache, "01428-mixed-silenced"); Hostile(silenced); Fact(silenced, shaken, caster);
                Fact(silenced, Asset<BlueprintBuff>(cache, "SilenceBuff"), caster, false);
                nearby = new[] { caster, ally, target, eligible, unshaken, silenced }; before = saveCalls; d20 = 20;
                Cast(ability, caster);
                Check(saveCalls == before + 1, "mixed cast resolves only one eligible save, with all native conditions preserved");
                if (hasFeedback) {
                    string summary = uiMessages.Last();
                    Check(summary.Contains("4 enemy target(s)") && summary.Contains("1 meet the target requirements") && summary.Contains("1 with this caster's 24-hour immunity, 1 not shaken, 1 silenced"), "summary reports precise per-cast rejection causes with no extra saves");
                } else Check(uiMessages.Count == 0 && logMessages.Count == 0, "mixed cast has no extra combat or mod diagnostics");
                var afterFailure = Unit(cache, "01428-ui-failure-target"); Hostile(afterFailure); Fact(afterFailure, shaken, caster); nearby = new[] { afterFailure }; before = saveCalls; d20 = 1;
                throwUi = true; try { Cast(ability, caster); } finally { throwUi = false; }
                Check(saveCalls == before + 1 && afterFailure.State.HasCondition(UnitCondition.Frightened), "UI sink exception cannot interrupt native save/effect chain");
                if (hasFeedback) Check(logMessages.Any(x => x.Contains("01428 injected UI sink failure")), "isolated feedback exception is recorded for diagnosis");
                else Check(uiMessages.Count == 0 && logMessages.Count == 0, "native effects do not invoke the removed diagnostic UI sink");
            }
            Check(beforeDefinition == (string)definition.Invoke(null, new object[] { ability.ComponentsArray }), "native condition results and localized format read retain module definition identity");
            AccessTools.Method(production.GetType("CallOfTheWild.Wrath.Prototype.TerrifyingHowl"), "Register").Invoke(null, new object[] { cache });
            Check(ReferenceEquals(ability, Asset<BlueprintAbility>(cache, "TerrifyingHowlAbility")), "registration after real casts retains ability identity");
            Check(resistanceCalls > 0 && permissionCalls > 0 && conditionAttempts > 0, "real native resistance, buff permission and condition notification paths executed");
            PsychicExpansionTests.Inventory["howl01428NativeResolution"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["savingThrowResolutions"] = saveCalls, ["resistanceResolutions"] = resistanceCalls, ["buffPermissionResolutions"] = permissionCalls,
                ["nativeApplyBuffCalls"] = buffCalls, ["conditionAttemptEvents"] = conditionAttempts, ["castFxEndpointCalls"] = fxCalls, ["uiCastSummaries"] = new JArray(uiMessages), ["modDiagnosticMessages"] = new JArray(logMessages), ["feedbackApiPresent"] = feedback != null,
                ["nativeIsPassedUnpatched"] = true, ["nativeApplyBuffUnpatched"] = true, ["personalCasterMainTarget"] = true,
                ["boundary"] = "Native process/selector/action/save resolution/condition lifecycle/buff permission execute. Scene candidate and faction data, raw dice, settings difficulty, status-effect scene renderer and final fact attachment are controlled. BuffDescriptorImmunity is dispatched on active native runtime components; full EventBus/UI and whole-game mod combination remain game cases. This distinguishes absent eligibility from a proven production defect." };
            if (Environment.GetEnvironmentVariable("COTW_HOWL_01428_BASELINE") != "1") {
                Check(feedback != null && !hasFeedback, "legacy diagnostic type remains serializable but is not attached to the ability");
                Check(uiMessages.Count == 0 && logMessages.Count == 0, "all actual casts emit no extra diagnostic messages");
                PsychicExpansionTests.Inventory["howl01429CleanupContracts"] = new JObject {
                    ["passed"] = true, ["legacyFeedbackTypeRetained"] = feedback != null, ["diagnosticComponentAttached"] = hasFeedback,
                    ["extraCombatMessages"] = uiMessages.Count, ["extraModDiagnosticMessages"] = logMessages.Count,
                    ["nativeSavingThrows"] = saveCalls, ["nativeBuffPermissions"] = permissionCalls, ["nativeConditionAttempts"] = conditionAttempts,
                    ["nativeFxCalls"] = fxCalls, ["nativeEffectActionsRetained"] = true };
            }
            ((JObject)PsychicExpansionTests.Inventory["howl01428NativeResolution"])["assertions"] = checks;
            Console.WriteLine("PASS: " + checks + " Howl01428 native save/buff/condition checks."); return checks;
        } finally { h.UnpatchAll(h.Id); if (outerOwner != null) new Harmony(outerOwner).Patch(buffHelper, prefix: new HarmonyMethod(outerCapture)); current = null; executing = null; }
    }
}


