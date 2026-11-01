using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Real native selection, resources, stat values, skill dice, saves and buff
// permissions. The scene insertion and event dispatch boundaries are explicit;
// combat attack outcomes are fixture inputs, not a rendered combat simulation.
internal static class Inquisitions01428Tests
{
    static int checks, saveCalls, insertCalls, permissionCalls, resistanceCalls, immuneCalls, unitPositions;
    static bool failAppend, failPublish;
    static RulebookEventContext current;
    static readonly Queue<int> dice = new Queue<int>();
    static readonly List<Buff> installed = new List<Buff>();
    static readonly List<RuleSavingThrow> saves = new List<RuleSavingThrow>();
    static readonly List<RuleCanApplyBuff> permissions = new List<RuleCanApplyBuff>();
    static Type Module => typeof(HeresyWisdomChecks).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Inquisitions01428", true);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Helper<T>(string n, params object[] a) => (T)AccessTools.Method(typeof(Migration01426Tests), n).Invoke(null, a);
    static BlueprintGuid Id(string token) => Helper<BlueprintGuid>("Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static void Check(bool value, string reason) { if (!value) throw new Exception("Inquisitions01428: " + reason); checks++; }
    static void Register(BlueprintsCache c) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { c });
    static string Signature(BlueprintScriptableObject b) => (string)AccessTools.Method(typeof(HeresyWisdomChecks).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition").Invoke(null, new object[] { b.ComponentsArray });
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == Id("HeresyInquisition"))) throw new InvalidOperationException("01428 inquisition append injection"); }
    static void PublishFault(BlueprintGuid guid) { if (failPublish && guid == Id("ImprisonmentInquisition")) throw new InvalidOperationException("01428 inquisition publish injection"); }
    static bool Rejected(Action action, string text)
    {
        try { action(); return false; }
        catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (!(e is InvalidOperationException) || !e.Message.Contains(text)) throw new Exception("Unexpected rejection", e); return true; }
    }
    static bool Skip() => false;
    static bool False(ref bool __result) { __result = false; return false; }
    static bool Context(ref RulebookEventContext __result) { if (current == null) return true; __result = current; return false; }
    static bool Dice(DiceFormula __0, ref int __result) { __result = __0.Dice == DiceType.Zero ? 0 : __0.Dice == DiceType.D4 ? 3 : dice.Count > 0 ? dice.Dequeue() : 1; return false; }
    static bool ResistanceDie(ref RuleRollD20 __result) { __result = NativeRule(new RuleRollD20(current.CurrentEvent.Initiator, 1)); return false; }
    static int Minimum(int min, int max) => min;
    static bool SkillDifficulty(int originalDC, out int dcAdjustment, ref int __result) { dcAdjustment = 0; __result = originalDC; return false; }
    static bool Expose(Exception __1) { throw new Exception("01428 native inquisition action", __1); }
    static bool Name(UnitEntityData __instance, ref string __result) { __result = __instance.UniqueId; return false; }

    sealed class Observer : IUnitConditionChangeAttemptHandler, IUnitConditionsChanged, IWasRoll, ISavingThrowFailed
    {
        public void HandleUnitConditionAddAttempt(UnitEntityData unit, UnitCondition condition, bool success) { }
        public void HandleUnitConditionsChanged(UnitEntityData unit, UnitCondition condition) { }
        public void WasRoll(RulebookEvent rule, RuleRollD20 roll) { }
        public void HandleSavingThrowFailed(RuleSavingThrow rule) { }
    }
    static bool Raise<T>(Action<T> action, bool unused) { if (new Observer() is T typed) action(typed); return true; }
    static T ContextRule<T>(MechanicsContext context, T rule) where T : RulebookEvent { rule.Reason = context; return NativeRule(rule); }
    static T InstanceRule<T>(Rulebook unused, T rule) where T : RulebookEvent => NativeRule(rule);
    static T NativeRule<T>(T rule) where T : RulebookEvent
    {
        var old = current; if (current == null) current = new RulebookEventContext(); current.PushEvent(rule);
        try
        {
            if (rule is RuleSkillCheck skill)
                foreach (var fact in skill.Initiator.Facts.List.ToArray())
                    foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is HeresyWisdomChecks))
                        using (runtime.RequestEventContext()) ((HeresyWisdomChecks)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(skill);
            if (rule is RuleRollD20 roll)
                foreach (var fact in roll.Initiator.Buffs.Enumerable.ToArray())
                    foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is HeresyInfiltrationReroll))
                        using (runtime.RequestEventContext()) ((HeresyInfiltrationReroll)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(roll);
            if (rule is RuleSavingThrow save)
            {
                saves.Add(save); saveCalls++;
                var attack = current.EventStack.OfType<RuleAttackRoll>().LastOrDefault();
                if (attack != null) Check(attack.Target == save.Initiator, "nested native save stays on the attack target in the actual rule stack");
            }
            if (!(rule is RuleApplySpell)) rule.OnTrigger(current);
            if (rule is RuleSpellResistanceCheck resistance) { resistanceCalls++; if (resistance.TargetIsImmune) immuneCalls++; }
            return rule;
        }
        finally { current.PopEvent(rule); current = old; }
    }
    static IEnumerable<CodeInstruction> Boundary(IEnumerable<CodeInstruction> input)
    {
        foreach (var instruction in input)
        {
            if (instruction.operand is MethodInfo random && random.DeclaringType == typeof(UnityEngine.Random) && random.Name == "Range" && random.ReturnType == typeof(int))
            { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Inquisitions01428Tests), nameof(Minimum)); }
            if (instruction.operand is MethodInfo m && m.IsGenericMethod)
            {
                if (m.DeclaringType == typeof(Rulebook) && m.Name == "Trigger") { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Inquisitions01428Tests), nameof(NativeRule)).MakeGenericMethod(m.GetGenericArguments()); }
                else if (m.DeclaringType == typeof(Rulebook) && m.Name == "TriggerEvent") { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Inquisitions01428Tests), nameof(InstanceRule)).MakeGenericMethod(m.GetGenericArguments()); }
                else if (typeof(MechanicsContext).IsAssignableFrom(m.DeclaringType) && m.Name == "TriggerRule") { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Inquisitions01428Tests), nameof(ContextRule)).MakeGenericMethod(m.GetGenericArguments()); }
                else if (m.DeclaringType == typeof(EventBus) && m.Name == "RaiseEvent" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(bool)) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Inquisitions01428Tests), nameof(Raise)).MakeGenericMethod(m.GetGenericArguments()); }
            }
            yield return instruction;
        }
    }
    static UnitEntityData Unit(BlueprintsCache cache, string name, int level = 8, int wisdom = 18)
    {
        var u = Helper<UnitEntityData>("Unit", cache, name);
        Set(u.State, "m_ConditionsExceptions", new List<UnitConditionExceptions>[((sbyte[])Get(u.State, "m_Conditions")).Length]);
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("67819271767a9dd4fbfd4ae700befea0"))) { Level = level });
        u.Stats.Wisdom.BaseValue = wisdom; u.Stats.Charisma.BaseValue = 10; u.Stats.SaveWill.BaseValue = u.Stats.SaveFortitude.BaseValue = 0;
        Set(u, "m_Position", new UnityEngine.Vector3(++unitPositions, 0, 0));
        return u;
    }
    static void Conditions(Buff fact, bool on)
    {
        foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is AddCondition || r.SourceBlueprintComponent is AddConditionImmunity || r.SourceBlueprintComponent is BuffStatusCondition))
            using (runtime.RequestEventContext()) AccessTools.Method(runtime.SourceBlueprintComponent.GetType(), on ? "OnTurnOn" : "OnTurnOff").Invoke(runtime.SourceBlueprintComponent, null);
    }
    static Buff BuffFact(UnitEntityData u, BlueprintBuff bp, UnitEntityData caster, bool condition = false)
    {
        var fact = Psychic034Tests.Fact(u, bp, caster, 8); if (condition) Conditions(fact, true); return fact;
    }
    static void Call(EntityFact fact, Type type, string method, params object[] args)
    {
        var runtime = fact.Components.Single(r => r.SourceBlueprintComponent.GetType() == type);
        var attack = args.FirstOrDefault() as RuleAttackRoll;
        var previous = current;
        if (attack != null) { current = current?.Clone() ?? new RulebookEventContext(); current.PushEvent(attack); }
        try { using (runtime.RequestEventContext()) AccessTools.Method(type, method).Invoke(runtime.SourceBlueprintComponent, args); }
        finally { if (attack != null) { current.PopEvent(attack); current = previous; } }
    }
    static bool Insert(BuffCollection __instance, BlueprintBuff blueprint, MechanicsContext parentContext, TimeSpan? duration, ref Buff __result)
    {
        insertCalls++; var owner = __instance.Owner.Unit; var ctx = parentContext.CloneFor(blueprint, owner.Descriptor, null);
        var can = new RuleCanApplyBuff(owner, blueprint, ctx, duration); permissions.Add(can); permissionCalls++;
        foreach (var fact in owner.Buffs.Enumerable.Where(b => b.IsTurnedOn && !b.IsSuppressed))
            foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is BuffDescriptorImmunity))
                using (runtime.RequestEventContext()) ((BuffDescriptorImmunity)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(can);
        NativeRule(can); if (!can.CanApply) { __result = null; return false; }
        __result = BuffFact(owner, blueprint, parentContext.MaybeCaster); Set(__result, "m_Context", ctx); Set(__result, "PlannedDuration", can.Duration);
        Conditions(__result, true); installed.Add(__result); return false;
    }
    static BlueprintBuff Immunity(string name, BlueprintComponent component)
    {
        var bp = new BlueprintBuff { name = name, ComponentsArray = new[] { component } }; component.name = name + "Component"; bp.OnEnable(); return bp;
    }
    static bool Remove(Buff __instance)
    {
        Conditions(__instance, false); __instance.Owner.Facts.List.Remove(__instance);
        var cache = Get(__instance.Owner.Facts, "m_Cache"); AccessTools.Method(cache.GetType(), "Clear").Invoke(cache, null);
        Set(__instance, "IsTurnedOn", false); return false;
    }
    static T Hydrate<T>(BlueprintsCache cache, string id) where T : BlueprintScriptableObject, new()
    {
        var bp = PsychicExpansionTests.NativeCase<T>(id);
        var data = PsychicExpansionTests.NativeRows.First(r => (string)r["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
        foreach (var value in ((JObject)data).Properties().Where(p => p.Name != "Components"))
        {
            var field = AccessTools.Field(typeof(T), value.Name);
            if (field != null && !field.IsInitOnly) field.SetValue(bp, PsychicExpansionTests.Read(value.Value, field.FieldType));
        }
        cache.AddCachedBlueprint(bp.AssetGuid, bp); return bp;
    }

    internal static int Run(BlueprintsCache cache)
    {
        checks = saveCalls = insertCalls = permissionCalls = resistanceCalls = immuneCalls = unitPositions = 0; failAppend = failPublish = false; current = null;
        installed.Clear(); saves.Clear(); permissions.Clear(); dice.Clear();
        var h = new Harmony("CotW01428.InquisitionContracts");
        void Patch(MethodBase method, string name, Type owner = null) { if (method == null) throw new Exception("Missing endpoint " + name); h.Patch(method, prefix: new HarmonyMethod(owner ?? typeof(Inquisitions01428Tests), name) { priority = Priority.First }); }
        void Transpile(MethodBase method) { h.Patch(method, transpiler: new HarmonyMethod(typeof(Inquisitions01428Tests), nameof(Boundary))); }
        var buffHelper = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var outerCapture = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense");
        var outerOwner = Harmony.GetPatchInfo(buffHelper)?.Prefixes.FirstOrDefault(p => p.PatchMethod == outerCapture)?.owner;
        if (outerOwner != null) h.Unpatch(buffHelper, outerCapture);
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), "No", typeof(Migration01426Tests));
        Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), "PoolIgnore", typeof(Migration01426Tests));
        Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game), "LevelUpController"), "NullController", typeof(Migration01426Tests));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayerFaction"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "CharacterName"), nameof(Name));
        Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), nameof(Context));
        Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), nameof(Dice));
        Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), nameof(ResistanceDie));
        Patch(AccessTools.Method(typeof(RuleSkillCheck), "AdjustDCByDifficulty"), nameof(SkillDifficulty));
        Patch(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), nameof(Skip));
        Patch(AccessTools.Method(typeof(GameHelper), "IsAttackingGreenNPC"), nameof(False));
        Patch(AccessTools.Method(typeof(ShadowSpellLogicExtension), "SkipBecauseOfShadow"), nameof(False));
        Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(Expose));
        Patch(AccessTools.Method(typeof(BuffCollection), "AddBuff", new[] { typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) }), nameof(Insert));
        Patch(AccessTools.Method(typeof(Buff), "Remove", Type.EmptyTypes), nameof(Remove));
        Provider01419Tests.PatchLabels(h);
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Howl01428Tests), "DifficultyBoundary"));
        foreach (var m in new[] { AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }), AccessTools.Method(typeof(ContextActionSavingThrow), "RunAction"), AccessTools.Method(typeof(RuleSavingThrow), "OnTrigger"), AccessTools.Method(typeof(RuleSavingThrow), "RollD20"), AccessTools.Method(typeof(RuleSkillCheck), "RollD20"), AccessTools.Method(typeof(ContextDiceValue), "Calculate", new[] { typeof(MechanicsContext) }) }) Transpile(m);
        foreach (var m in typeof(UnitState).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => new[] { "AddCondition", "RemoveCondition", "AddConditionImmunity", "RemoveConditionImmunity" }.Contains(m.Name))) Transpile(m);
        Transpile(AccessTools.Method(typeof(AbilityEffectRunAction), "Apply"));
        Transpile(AccessTools.Method(typeof(RuleSpellResistanceCheck), "OnTrigger"));
        Transpile(AccessTools.Method(typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger"));
        Transpile(AccessTools.Method(typeof(RuleRollD20), "FromInt"));
        Transpile(AccessTools.Method(typeof(RuleSkillCheck), "OnTrigger"));
        foreach (var m in typeof(BuffStatusCondition).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name.EndsWith("OnNewRound"))) Transpile(m);
        try
        {
            // The focused runner skips the earlier 026 suite. Build its already
            // accepted Conversion source only as the coexistence fixture.
            if (Asset<BlueprintFeature>(cache, "ConversionCharmOfWisdom") == null)
            {
                foreach (string id in new[] { "d7cbd2004ce66a042aeab2e95a3c5c61", "55a037e514c0ee14a8e3ed14b47061de", "8d6073201e5395d458b8251386d72df1" }) Hydrate<BlueprintAbility>(cache, id);
                AccessTools.Method(typeof(HeresyWisdomChecks).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Inquisitions01426"), "Register").Invoke(null, new object[] { cache });
            }
            Registration(cache, h);
            SelectionAndParameters(cache);
            WisdomAndRerolls(cache);
            AttackEffects(cache);
            PsychicExpansionTests.Inventory["inquisitions01428Contracts"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 2,
                ["items"] = new JArray("Heresy Inquisition", "Imprisonment Inquisition"),
                ["nativeSavingThrows"] = saveCalls, ["nativeBuffPermissions"] = permissionCalls, ["sceneBuffInsertions"] = insertCalls,
                ["publicationAndAppendRollback"] = true, ["nativePoolsPreserved"] = true, ["deityAndDuplicatePrerequisites"] = true,
                ["signedWisdomAndConversion"] = true, ["failedRerollSharedResource"] = true,
                ["criticalMeleeRangedTouchRay"] = true, ["fortificationFakeEmptyGuards"] = true,
                ["nativeHoldPerRoundSaves"] = true, ["nativeBuffAndConditionImmunity"] = true,
                ["nativeSpellImmunityGate"] = true, ["nativeSpellResistanceCalls"] = resistanceCalls, ["nativeImmuneTargets"] = immuneCalls,
                ["fixedRollAndSuccessBonusDoNotSpend"] = true,
                ["boundary"] = "Actual production register/rollback/repeat and original pool preservation; native deity prerequisites and CanSelect, native class scaling/DC/resources, native dependent skill values and failed D20 rerolls, real RuleSavingThrow and RuleCanApplyBuff with native condition components. Deterministic dice, neutral difficulty, event dispatch restricted to tested components and final scene insertion are controlled. Attack outcome/critical/fortification flags are fixture inputs. Full Unity rendering, combat animation, live upgrades, save persistence and external DLL combination belong to the tester steps."
            };
            Console.WriteLine("PASS: " + checks + " inquisition contracts; " + saveCalls + " native saves."); return checks;
        }
        finally
        {
            failAppend = failPublish = false; current = null; h.UnpatchAll(h.Id);
            if (outerOwner != null) new Harmony(outerOwner).Patch(buffHelper, prefix: new HarmonyMethod(outerCapture));
        }
    }

    static void Registration(BlueprintsCache cache, Harmony h)
    {
        var source = Hydrate<BlueprintAbility>(cache, "69851cc3b821c2d479ac1f2d86e8ffa5");
        Hydrate<BlueprintBuff>(cache, "f7f6330726121cf4b90a6086b05d2e38"); Hydrate<BlueprintBuff>(cache, "2cfcce5b62d3e6d4082ec31b58468cc8");
        var pools = ((string[])AccessTools.Field(Module, "PoolIds").GetValue(null)).Select(s => (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(s))).ToArray();
        var initial = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToHashSet();
        var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var donorSignature = Signature(source);
        h.Patch(AccessTools.Method(typeof(HeresyWisdomChecks).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(Inquisitions01428Tests), nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"), postfix: new HarmonyMethod(typeof(Inquisitions01428Tests), nameof(PublishFault)));
        foreach (bool publish in new[] { true, false })
        {
            failPublish = publish; failAppend = !publish;
            Check(Rejected(() => Register(cache), "01428 inquisition"), "real publication/append fault reached"); failPublish = failAppend = false;
            Check(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().All(initial.Contains), "transaction removes every partial blueprint");
            for (int p = 0; p < pools.Length; p++) for (int f = 0; f < 2; f++) Check(ReferenceEquals(Get(pools[p], f == 0 ? "m_Features" : "m_AllFeatures"), before[p][f]), "exact two-pool array rollback");
        }
        var deityPool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("59e7a76987fe3b547b9cce045f4db3e4"));
        var deityRefs = (BlueprintFeatureReference[])Get(deityPool, "m_AllFeatures");
        Set(deityPool, "m_AllFeatures", deityRefs.Where(r => r.Get()?.name != "AbadarFeature").ToArray());
        try { Check(Rejected(() => Register(cache), "deity dependency"), "missing native deity fails before publishing"); }
        finally { Set(deityPool, "m_AllFeatures", deityRefs); }
        var foreign = new BlueprintFeature { AssetGuid = Id("HeresyInquisition"), name = "01428ForeignHeresy", ComponentsArray = Array.Empty<BlueprintComponent>() };
        cache.AddCachedBlueprint(foreign.AssetGuid, foreign);
        try { Check(Rejected(() => Register(cache), "identity collision") && ReferenceEquals(cache.Load(foreign.AssetGuid), foreign), "foreign blueprint collision fails without overwriting it"); }
        finally { cache.RemoveCachedBlueprint(foreign.AssetGuid); }
        Register(cache); var roots = new[] { Asset<BlueprintProgression>(cache, "HeresyInquisition"), Asset<BlueprintProgression>(cache, "ImprisonmentInquisition") };
        Register(cache);
        foreach (var root in roots) Check(ReferenceEquals(cache.Load(root.AssetGuid), root), "repeat registration retains original root instance");
        for (int p = 0; p < pools.Length; p++) foreach (string field in new[] { "m_Features", "m_AllFeatures" })
        {
            var old = (BlueprintFeatureReference[])before[p][field == "m_Features" ? 0 : 1]; var now = (BlueprintFeatureReference[])Get(pools[p], field);
            Check(now.Take(old.Length).SequenceEqual(old), "native and earlier verified pool entries/order preserved");
            foreach (var root in roots) Check(now.Count(r => r.Guid == root.AssetGuid) == 1, "one new root per native pool");
        }
        Check(Signature(source) == donorSignature, "native curse source unchanged");
        var added = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Where(id => !initial.Contains(id)).Select(cache.Load).ToArray();
        Check(added.All(b => b != null), "every new cached reference is valid");
        PsychicExpansionTests.Inventory["inquisitions01428Blueprints"] = new JArray(added.OrderBy(b => b.name).Select(b => new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name, ["type"] = b.GetType().Name }));
    }

    static void SelectionAndParameters(BlueprintsCache cache)
    {
        var deityPool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("59e7a76987fe3b547b9cce045f4db3e4"));
        var heresyGods = (string[])AccessTools.Field(Module, "HeresyDeities").GetValue(null);
        foreach (string token in new[] { "HeresyInquisition", "ImprisonmentInquisition" })
        {
            var root = Asset<BlueprintProgression>(cache, token);
            Check(root.LevelEntries.Select(e => e.Level).SequenceEqual(token == "HeresyInquisition" ? new[] { 1, 4, 8 } : new[] { 1, 8 }), "original level milestones");
            Check(root.LevelEntries.SelectMany(e => e.Features).All(f => f != null) && !root.GetComponents<AddKnownSpell>().Any() && root.Groups.SequenceEqual(new[] { FeatureGroup.Domain }), "resolved domain progression grants no spellbook slots or spells");
            foreach (string deity in heresyGods.Concat(new[] { "CaydenCailean", "Irori" }))
            {
                var u = Unit(cache, token + deity);
                Check(!root.MeetsPrerequisites(null, u.Descriptor, null, false, false), "no faith is rejected");
                var god = deityPool.AllFeatures.Single(f => f.name == deity + "Feature"); Helper<Feature>("Fact", u, god);
                bool allowed = token == "HeresyInquisition" ? heresyGods.Contains(deity) : new[] { "Abadar", "Asmodeus", "Torag" }.Contains(deity);
                Check(root.MeetsPrerequisites(null, u.Descriptor, null, false, false) == allowed, "exact original allowed deity " + token + "/" + deity);
                if (deity != "Abadar") continue;
                foreach (var id in (string[])AccessTools.Field(Module, "PoolIds").GetValue(null))
                {
                    var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id)); var state = Empty<LevelUpState>();
                    Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 8);
                    state.NextClassLevel = 8; state.SelectedClass = u.Progression.Classes[0].CharacterClass;
                    var pending = state.AddSelection(null, default(FeatureSource), pool, 8);
                    Check(pool.CanSelect(u.Descriptor, state, pending, root), "native CanSelect accepts eligible deity in both pools");
                }
                Helper<Feature>("Fact", u, root); Check(!root.MeetsPrerequisites(null, u.Descriptor, null, false, false), "already owned inquisition is not selectable twice");
            }
        }
        foreach (int level in new[] { 1, 4, 7, 8, 9, 20 }) foreach (int wisdom in new[] { 1, 6, 10, 18 })
        {
            var u = Unit(cache, "01428-scale" + level + wisdom, level, wisdom); int bonus = u.Stats.Wisdom.Bonus;
            foreach (string token in new[] { "HeresyWordOfAnathemaAbility", "ImprisonmentCagingStrikeEffect", "ImprisonmentDivinePrisonEffect" })
            {
                var ability = Asset<BlueprintAbility>(cache, token); var data = new AbilityData(ability, u.Descriptor);
                var preview = data.CalculateParams(); var context = data.CreateExecutionContext(u, preview);
                Check(preview.CasterLevel == level && preview.DC == 10 + level / 2 + bonus, "native ability params follow current owner level and Wisdom " + token);
                Check(context.Params.DC == preview.DC && context.Params.CasterLevel == level, "casting context agrees with preview");
                Check(ability.AvailableMetamagic == 0 && !ability.GetComponents<SpellListComponent>().Any(), "granted power is outside spellbook and metamagic");
            }
            Check(Asset<BlueprintAbilityResource>(cache, "HeresyBlessedInfiltrationUses").GetMaxAmount(u.Descriptor) == Math.Max(0, bonus), "failed-check resource minimum zero");
            Check(Asset<BlueprintAbilityResource>(cache, "ImprisonmentCagingStrikeUses").GetMaxAmount(u.Descriptor) == Math.Max(0, 3 + bonus), "critical resource 3 + Wisdom minimum0 as original resource");
            foreach (string token in new[] { "HeresyWordOfAnathemaUses", "ImprisonmentDivinePrisonUses" }) Check(Asset<BlueprintAbilityResource>(cache, token).GetMaxAmount(u.Descriptor) == 1, "fixed daily resource");
        }
        var mixed = Unit(cache, "01428-mixed", 3); mixed.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("f1a70d9e1b0b41e49874e1fa9052a1ce"))) { Level = 5 });
        foreach (string token in new[] { "HeresyInquisition", "ImprisonmentInquisition" }) Check(Asset<BlueprintProgression>(cache, token).CalcLevel(mixed.Descriptor) == 8, "native cleric plus inquisitor levels sum");
        var word = Asset<BlueprintAbility>(cache, "HeresyWordOfAnathemaAbility");
        Check(word.Type == AbilityType.SpellLike && word.Range == AbilityRange.Custom && word.CustomRange.Value == 60 && word.SpellResistance && word.CanTargetEnemies && !word.CanTargetSelf && !word.CanTargetFriends && !word.CanTargetPoint, "anathema original SLA target/range/SR");
        var cost = word.GetComponent<AbilityResourceLogic>(); var resource = cost.RequiredResource; mixed.Resources.Add(resource, true);
        var cast = new AbilityData(word, mixed.Descriptor); Check(cost.IsAbilityRestrictionPassed(cast), "native Word daily use available"); cost.Spend(cast);
        Check(!cost.IsAbilityRestrictionPassed(cast), "native Word rejects exhausted daily resource"); mixed.Resources.Restore(resource); Check(cost.IsAbilityRestrictionPassed(cast), "native rest restores Word");
        var listener = Unit(cache, "01428-listener"); var audible = word.GetComponent<AudibleAbilityRestriction>();
        Check(audible.IsTargetRestrictionPassed(mixed, listener), "Word accepts audible caster and listener");
        var silence = BuffFact(listener, Asset<BlueprintBuff>(cache, "SilenceBuff"), listener);
        Check(!audible.IsTargetRestrictionPassed(mixed, listener), "Word rejects silenced listener"); silence.IsSuppressed = true;
        Check(audible.IsTargetRestrictionPassed(mixed, listener), "suppressed silence does not reject Word listener");
        BuffFact(mixed, Asset<BlueprintBuff>(cache, "SilenceBuff"), mixed);
        Check(!audible.IsCasterRestrictionPassed(mixed) && !audible.IsTargetRestrictionPassed(mixed, listener), "Word rejects silenced caster in both native restriction interfaces");
    }

    static void WisdomAndRerolls(BlueprintsCache cache)
    {
        foreach (int wisdom in new[] { 6, 18 })
        {
            var u = Unit(cache, "01428-wisdom" + wisdom, 8, wisdom); u.Stats.Charisma.BaseValue = 14;
            var root = Helper<Feature>("Fact", u, Asset<BlueprintFeature>(cache, "HeresyRighteousInfiltration"));
            foreach (var skill in new[] { StatType.CheckBluff, StatType.CheckIntimidate, StatType.CheckDiplomacy, StatType.SkillPerception })
            {
                var baseline = new RuleSkillCheck(u, skill, 100); int before = baseline.TotalBonus;
                Call(root, typeof(HeresyWisdomChecks), "OnEventAboutToTrigger", baseline); Call(root, typeof(HeresyWisdomChecks), "OnEventAboutToTrigger", baseline);
                int expected = skill == StatType.CheckBluff || skill == StatType.CheckIntimidate ? u.Stats.Wisdom.Bonus - u.Stats.Charisma.Bonus : 0;
                Check(baseline.TotalBonus - before == expected, "signed Wisdom replacement once; unrelated skills unchanged");
            }
            var conversion = Helper<Feature>("Fact", u, Asset<BlueprintFeature>(cache, "ConversionCharmOfWisdom"));
            foreach (var runtime in conversion.Components) using (runtime.RequestEventContext()) AccessTools.Method(runtime.SourceBlueprintComponent.GetType(), "OnTurnOn").Invoke(runtime.SourceBlueprintComponent, null);
            Check(u.Stats.SkillPersuasion.BaseAttribute.Type == StatType.Wisdom, "actual Conversion replacement applied");
            var combined = new RuleSkillCheck(u, StatType.CheckBluff, 100); int original = combined.TotalBonus; Call(root, typeof(HeresyWisdomChecks), "OnEventAboutToTrigger", combined);
            Check(combined.TotalBonus == original, "Conversion plus Heresy does not add Wisdom twice");
        }
        var caster = Unit(cache, "01428-reroll"); var resource = Asset<BlueprintAbilityResource>(cache, "HeresyBlessedInfiltrationUses"); caster.Resources.Add(resource, true);
        var bluff = BuffFact(caster, Asset<BlueprintBuff>(cache, "HeresyBlessedInfiltrationBluffBuff"), caster);
        var dip = BuffFact(caster, Asset<BlueprintBuff>(cache, "HeresyBlessedInfiltrationDiplomacyBuff"), caster);
        var stealth = BuffFact(caster, Asset<BlueprintBuff>(cache, "HeresyBlessedInfiltrationStealthBuff"), caster);
        int beforeResource = caster.Resources.GetResourceAmount(resource);
        foreach (var skill in new[] { StatType.CheckBluff, StatType.CheckDiplomacy, StatType.SkillStealth })
        {
            dice.Enqueue(1); dice.Enqueue(20); var rule = NativeRule(new RuleSkillCheck(caster, skill, 15));
            Check(rule.Success && rule.D20.RollHistory.Count == 2 && rule.D20.Result == 20, "failed native skill rerolls and takes better result " + skill);
            Check(caster.Resources.GetResourceAmount(resource) == --beforeResource, "matching option spends exactly one shared use");
        }
        dice.Enqueue(20); var success = NativeRule(new RuleSkillCheck(caster, StatType.CheckBluff, 15));
        Check(success.Success && success.D20.RollHistory.Count == 1 && caster.Resources.GetResourceAmount(resource) == beforeResource, "successful first roll does not spend or reroll");
        dice.Enqueue(1); var unrelated = NativeRule(new RuleSkillCheck(caster, StatType.SkillPerception, 40));
        Check(unrelated.D20.RollHistory.Count == 1 && caster.Resources.GetResourceAmount(resource) == beforeResource, "unrelated skill has no reroll");
        var parent = new RuleSkillCheck(caster, StatType.CheckBluff, 40); var fake = new RuleRollD20(caster) { IsFake = true }; current = new RulebookEventContext(); current.PushEvent(parent); current.PushEvent(fake);
        Call(bluff, typeof(HeresyInfiltrationReroll), "OnEventAboutToTrigger", fake); current = null;
        Check((fake.Rerolls == null || !fake.Rerolls.Any()) && caster.Resources.GetResourceAmount(resource) == beforeResource, "fake roll does not spend");
        dice.Enqueue(1); dice.Enqueue(2); var stillFailed = NativeRule(new RuleSkillCheck(caster, StatType.CheckBluff, 40));
        Check(!stillFailed.Success && stillFailed.D20.Result == 2 && caster.Resources.GetResourceAmount(resource) == 0, "a reroll can still fail and pays last use");
        dice.Enqueue(1); var empty = NativeRule(new RuleSkillCheck(caster, StatType.CheckDiplomacy, 40)); Check(empty.D20.RollHistory.Count == 1, "other enabled option cannot overspend empty shared resource");
        caster.Resources.Restore(resource); Set(bluff, "IsTurnedOn", false); dice.Enqueue(1);
        var off = NativeRule(new RuleSkillCheck(caster, StatType.CheckBluff, 40)); Check(off.D20.RollHistory.Count == 1 && caster.Resources.GetResourceAmount(resource) == 4, "disabled option cannot reroll after rest");
        Set(bluff, "IsTurnedOn", true); var repeated = new RuleRollD20(caster); parent = new RuleSkillCheck(caster, StatType.CheckBluff, 40);
        current = new RulebookEventContext(); current.PushEvent(parent); current.PushEvent(repeated); dice.Enqueue(1);
        Call(bluff, typeof(HeresyInfiltrationReroll), "OnEventAboutToTrigger", repeated); Call(bluff, typeof(HeresyInfiltrationReroll), "OnEventAboutToTrigger", repeated); current = null;
        Check(repeated.Rerolls.Count == 1 && caster.Resources.GetResourceAmount(resource) == 3, "repeated dispatch of same D20 spends once and schedules once");
        current = new RulebookEventContext(); current.PushEvent(new RuleSkillCheck(caster, StatType.CheckBluff, 40));
        var fixedRoll = RuleRollD20.FromInt(caster, 5); current = null;
        Check(fixedRoll.Result == 5 && fixedRoll.Rerolls == null && caster.Resources.GetResourceAmount(resource) == 3, "native FromInt fixed failure never spends on an impossible reroll");
        var scripted = NativeRule(new RuleSkillCheck(caster, StatType.CheckBluff, 40) { EnsureSuccess = false });
        Check(!scripted.Success && scripted.D20.Rerolls == null && caster.Resources.GetResourceAmount(resource) == 3, "native scripted EnsureSuccess=false keeps fixed failure without spending");
        dice.Enqueue(1); var successBonus = NativeRule(new RuleSkillCheck(caster, StatType.CheckBluff, 15) { SuccessBonus = 20 });
        Check(successBonus.Success && successBonus.D20.RollHistory.Count == 1 && caster.Resources.GetResourceAmount(resource) == 3, "native conditional SuccessBonus avoids unnecessary reroll charge");
    }

    static RuleAttackRoll Attack(UnitEntityData caster, UnitEntityData target, AttackType attackType, bool critical = true, bool hit = true)
    {
        var category = attackType == AttackType.Touch ? WeaponCategory.Touch : attackType == AttackType.RangedTouch ? WeaponCategory.Ray : attackType == AttackType.Ranged ? WeaponCategory.Longbow : WeaponCategory.Longsword;
        var weapon = (ItemEntityWeapon)AccessTools.Method(typeof(Linnorm01416Tests), "Weapon").Invoke(null, new object[] { caster, category, "01428-attack" });
        var rule = new RuleAttackRoll(caster, target, weapon, 0) { AttackType = attackType };
        Set(rule, "Result", hit ? AttackResult.Hit : AttackResult.Miss); Set(rule, "IsCriticalConfirmed", critical); return rule;
    }
    static void AttackEffects(BlueprintsCache cache)
    {
        var caster = Unit(cache, "01428-attacker"); var target = Unit(cache, "01428-target", 8, 10);
        var caging = BuffFact(caster, Asset<BlueprintBuff>(cache, "ImprisonmentCagingStrikeReady"), caster);
        var prison = BuffFact(caster, Asset<BlueprintBuff>(cache, "ImprisonmentDivinePrisonReady"), caster);
        var cageResource = Asset<BlueprintAbilityResource>(cache, "ImprisonmentCagingStrikeUses"); var prisonResource = Asset<BlueprintAbilityResource>(cache, "ImprisonmentDivinePrisonUses");
        caster.Resources.Add(cageResource, true); caster.Resources.Add(prisonResource, true);
        foreach (var type in new[] { AttackType.Melee, AttackType.Ranged, AttackType.Touch, AttackType.RangedTouch })
        {
            var roll = Attack(caster, target, type); int before = saveCalls, uses = caster.Resources.GetResourceAmount(cageResource); dice.Enqueue(1);
            Call(caging, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", roll); Call(caging, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", roll);
            Check(saveCalls == before + 1 && saves.Last().Type == SavingThrowType.Fortitude && saves.Last().DifficultyClassBase == 18 && !saves.Last().IsPassed, "critical triggers one real Fort DC18 save, including ray/touch");
            Check(caster.Resources.GetResourceAmount(cageResource) == uses - 1 && target.State.HasCondition(UnitCondition.Entangled), "same attack pays once and native entangle condition applies");
            var applied = installed.Last(); Check(applied.PlannedDuration == TimeSpan.FromSeconds(18) && applied.Context.MaybeCaster == caster && applied.Context.Params.DC == 18 && applied.IsNotDispelable, "native 1d4 duration, caster and DC context preserved");
        }
        foreach (string invalid in new[] { "miss", "normal", "fortification", "fake", "off", "suppressed" })
        {
            var roll = Attack(caster, target, AttackType.Melee, invalid != "normal", invalid != "miss");
            if (invalid == "fortification") Set(roll, "FortificationNegatesCriticalHit", true); if (invalid == "fake") roll.IsFake = true;
            if (invalid == "off") Set(caging, "IsTurnedOn", false); if (invalid == "suppressed") caging.IsSuppressed = true;
            int before = saveCalls, uses = caster.Resources.GetResourceAmount(cageResource); Call(caging, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", roll);
            Check(before == saveCalls && uses == caster.Resources.GetResourceAmount(cageResource), "invalid critical trigger rejected " + invalid);
            Set(caging, "IsTurnedOn", true); if (invalid == "suppressed") caging.IsSuppressed = false;
        }
        int beforeSave = saveCalls, beforeBuff = installed.Count; dice.Enqueue(20);
        Call(caging, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, target, AttackType.Melee));
        Check(saveCalls == beforeSave + 1 && saves.Last().IsPassed && installed.Count == beforeBuff, "successful native Fort save prevents entangle");
        caster.Resources.Spend(cageResource, caster.Resources.GetResourceAmount(cageResource)); beforeSave = saveCalls;
        Call(caging, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, target, AttackType.Melee)); Check(saveCalls == beforeSave, "no charge means no free critical effect");
        foreach (var type in new[] { AttackType.Ranged, AttackType.RangedTouch })
        {
            beforeSave = saveCalls; Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, target, type));
            Check(saveCalls == beforeSave && caster.Resources.GetResourceAmount(prisonResource) == 1, "Divine Prison rejects ranged attacks and rays");
        }
        dice.Enqueue(1); beforeSave = saveCalls; Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, target, AttackType.Touch, false));
        Check(saveCalls == beforeSave + 1 && saves.Last().Type == SavingThrowType.Will && saves.Last().DifficultyClassBase == 18 && !saves.Last().IsPassed, "successful noncritical melee touch runs real Will save");
        var held = installed.Last(); Check(held.Blueprint.AssetGuid.ToString() == "2cfcce5b62d3e6d4082ec31b58468cc8" && held.PlannedDuration == TimeSpan.FromSeconds(48) && held.Context.Params.DC == 18 && target.State.HasCondition(UnitCondition.Paralyzed), "real Hold Monster lasts class level rounds with native paralysis and resave DC: buff=" + held.Blueprint.name + "; duration=" + held.PlannedDuration + "; DC=" + held.Context.Params.DC + "; condition=" + target.State.HasCondition(UnitCondition.Paralyzed));
        var status = held.Components.Single(r => r.SourceBlueprintComponent is BuffStatusCondition);
        Check(((BuffStatusCondition)status.SourceBlueprintComponent).SaveEachRound, "native Hold Monster retains per-round save component");
        dice.Enqueue(1); beforeSave = saveCalls;
        using (status.RequestEventContext()) ((ITickEachRound)status.SourceBlueprintComponent).OnNewRound();
        Check(saveCalls == beforeSave + 1 && saves.Last().DifficultyClassBase == 18 && !saves.Last().IsPassed && target.State.HasCondition(UnitCondition.Paralyzed), "actual native failed next-round save keeps Hold Monster at stored DC18");
        dice.Enqueue(20);
        using (status.RequestEventContext()) ((ITickEachRound)status.SourceBlueprintComponent).OnNewRound();
        Check(saves.Last().IsPassed && !target.State.HasCondition(UnitCondition.Paralyzed) && !held.IsTurnedOn, "actual native successful next-round save removes paralysis");
        beforeSave = saveCalls; Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, target, AttackType.Melee)); Check(saveCalls == beforeSave, "once-daily Prison cannot repeat without resource");
        var immune = Unit(cache, "01428-mindimmune", 8, 10); BuffFact(immune, Immunity("01428MindImmunity", new BuffDescriptorImmunity { Descriptor = SpellDescriptor.MindAffecting }), immune);
        caster.Resources.Restore(prisonResource); dice.Enqueue(1); beforeBuff = installed.Count;
        Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, immune, AttackType.Melee, false));
        Check(installed.Count == beforeBuff && !immune.State.HasCondition(UnitCondition.Paralyzed) && permissions.Last().Immunity && caster.Resources.GetResourceAmount(prisonResource) == 0, "native mind-affecting buff immunity blocks Prison, qualifying attack still pays");
        var conditionImmune = Unit(cache, "01428-paralysisimmune", 8, 10); BuffFact(conditionImmune, Immunity("01428ParalysisImmunity", new AddConditionImmunity { Condition = UnitCondition.Paralyzed }), conditionImmune, true);
        caster.Resources.Restore(prisonResource); dice.Enqueue(1);
        Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, conditionImmune, AttackType.Melee, false));
        Check(!conditionImmune.State.HasCondition(UnitCondition.Paralyzed), "native paralysis condition immunity remains respected");
        var spellImmune = Unit(cache, "01428-spell-descriptor-only", 8, 10); var resistancePart = spellImmune.Ensure<UnitPartSpellResistance>();
        resistancePart.AddResistance(1000, "01428NativeSR", null, null, null);
        int immunityId = resistancePart.AddImmunity(SpellImmunityType.SpellDescriptor, AlignmentComponent.None, Array.Empty<BlueprintAbility>(), SpellDescriptor.MindAffecting, null);
        caster.Resources.Restore(prisonResource); beforeSave = saveCalls; int beforeResistance = resistanceCalls; int beforeImmunity = immuneCalls;
        Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, spellImmune, AttackType.Melee, false));
        Check(resistanceCalls == beforeResistance + 1 && immuneCalls == beforeImmunity + 1 && saveCalls == beforeSave && !spellImmune.State.HasCondition(UnitCondition.Paralyzed) && caster.Resources.GetResourceAmount(prisonResource) == 0, "independent native spell-descriptor immunity blocks Prison before save but still spends the qualifying hit: checks=" + resistanceCalls + "/" + beforeResistance + "; immune=" + immuneCalls + "/" + beforeImmunity + "; saves=" + saveCalls + "/" + beforeSave + "; condition=" + spellImmune.State.HasCondition(UnitCondition.Paralyzed) + "; resource=" + caster.Resources.GetResourceAmount(prisonResource));
        resistancePart.Remove(immunityId); caster.Resources.Restore(prisonResource); dice.Enqueue(1); beforeResistance = resistanceCalls;
        Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, spellImmune, AttackType.Melee, false));
        Check(resistanceCalls == beforeResistance + 1 && saveCalls == beforeSave + 1 && spellImmune.State.HasCondition(UnitCondition.Paralyzed) && caster.Resources.GetResourceAmount(prisonResource) == 0, "removing descriptor immunity allows supernatural Prison through numeric SR1000");
        var other = Unit(cache, "01428-other-owner"); var otherCaging = BuffFact(other, Asset<BlueprintBuff>(cache, "ImprisonmentCagingStrikeReady"), other); other.Resources.Add(cageResource, true);
        beforeSave = saveCalls;
        Call(otherCaging, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", Attack(caster, target, AttackType.Melee));
        Check(saveCalls == beforeSave && other.Resources.GetResourceAmount(cageResource) == 7, "another owner's toggle cannot spend on this attack");
        caster.Resources.Restore(cageResource); caster.Resources.Restore(prisonResource); var both = Attack(caster, target, AttackType.Melee); dice.Enqueue(1); dice.Enqueue(1); beforeSave = saveCalls;
        Call(caging, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", both); Call(prison, typeof(InquisitionImprisonmentAttack), "OnEventDidTrigger", both);
        Check(saveCalls == beforeSave + 2 && caster.Resources.GetResourceAmount(cageResource) == 6 && caster.Resources.GetResourceAmount(prisonResource) == 0, "both enabled independent powers resolve once from one qualifying melee critical");
        var word = Asset<BlueprintAbility>(cache, "HeresyWordOfAnathemaAbility"); var data = new AbilityData(word, caster.Descriptor);
        dice.Enqueue(1); var context = data.CreateExecutionContext(target, data.CalculateParams()); word.GetComponent<AbilityEffectRunAction>().Apply(context, target);
        var curse = installed.Last(); Check(saves.Last().Type == SavingThrowType.Will && curse.PlannedDuration == TimeSpan.FromMinutes(1) && curse.Context.Params.DC == 18, "Word uses native Will save and one-minute original curse buff");
    }
}
