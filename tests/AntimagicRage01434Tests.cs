using System;
using System.Collections;
using System.Collections.Generic;
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
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Actual manager/fact/subscription/AC/defensive-casting/Counterstrike/forced-AoO
// queue and allowance paths. Scene geometry, fixed roll inputs and the final
// scene-command scheduler are explicit endpoints; production hooks stay intact.
internal static class AntimagicRage01434Tests
{
    static readonly string[] Tokens = { "GhostRagerRagePower", "DisruptiveRagePower", "SpellbreakerRagePower" };
    static readonly string[] Methods = { "RegisterGhostRager", "RegisterDisruptive", "RegisterSpellbreaker" };
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static readonly List<UnitAttackOfOpportunity> submitted = new List<UnitAttackOfOpportunity>();
    static int checks, serial, selections, defensiveCalls, nativeParams, nativeQueueCalls, nativeOpportunityEvents, prepareCalls, acCalls;
    static bool flatFooted, aware = true, melee = true, failAppend;
    static BlueprintGuid faultRoot;
    static int roll = 10;
    static RulebookEventContext current;
    // Earlier suites already patched and removed this native body. Re-emit
    // the exact original instructions after installing scene getter inputs,
    // preventing a stale wrapper/JIT inline from retaining an old scene getter.
    static IEnumerable<CodeInstruction> RefreshOpportunity(IEnumerable<CodeInstruction> input) => input;
    static string ILSignature(IEnumerable<CodeInstruction> instructions) {
        var codes = instructions.ToArray(); var labels = new Dictionary<Label, int>();
        for (int i = 0; i < codes.Length; i++) foreach (var label in codes[i].labels) labels[label] = i;
        string Operand(object value) {
            if (value is Label label) return "label:" + labels[label];
            if (value is Label[] many) return "labels:" + string.Join(",", many.Select(l => labels[l]));
            if (value is LocalBuilder local) return "local:" + local.LocalIndex + ":" + local.LocalType.FullName;
            if (value is Type type) return "type:" + type.AssemblyQualifiedName;
            if (value is MemberInfo member) return "token:" + member.Module.ModuleVersionId + ":" + member.MetadataToken + ":" + member;
            return value?.ToString() ?? "null";
        }
        return string.Join("\n", codes.Select(code => code.opcode.Name + "|" + Operand(code.operand) + "|" + string.Join(",", code.blocks.Select(block => block.blockType + ":" + block.catchType?.FullName))));
    }
    static UnitEntityData enemy;
    static Type Module => typeof(GhostRagerTouchArmor01434).Assembly.GetType("CallOfTheWild.Wrath.Prototype.AntimagicRage01434", true);
    static Type Helper => Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.RagePowerMigration01434", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(typeof(Migration01426Tests), "Id").Invoke(null, new object[] { token });
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("AntimagicRage01434: " + reason); checks++; }
    static string Definition(object value) => (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Register(BlueprintsCache c, string method = "Register") => AccessTools.Method(Module, method).Invoke(null, new object[] { c });
    static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null); return r; }
    static bool Skip() => false;
    static bool False(ref bool __result) { __result = false; return false; }
    static bool True(ref bool __result) { __result = true; return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static bool GameMode(ref Kingmaker.GameModes.GameModeType __result) { __result = Kingmaker.GameModes.GameModeType.Default; return false; }
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static void Identity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "antimagic034-fact-" + ++serial); }
    static bool HostName(BlueprintUnitFact __instance, ref string __result) { __result = __instance.name; return false; }
    static bool Ally(UnitEntityData __instance, UnitEntityData __0, ref bool __result) { __result = (__instance == enemy) == (__0 == enemy); return false; }
    static readonly UnitGroupMemory SceneMemory = Empty<UnitGroupMemory>();
    static bool Memory(ref UnitGroupMemory __result) { __result = SceneMemory; return false; }
    static bool Contains(ref bool __result) { __result = aware; return false; }
    static bool Hand(UnitEntityData __0, ref WeaponSlot __result) { __result = melee ? __0.Body.PrimaryHand : null; return false; }
    static bool MayAttack(ref bool __result) { __result = melee; return false; }
    static bool Submit(UnitCommand __0) { Check(__0 is UnitAttackOfOpportunity, "native forced opportunity constructs native UnitAttackOfOpportunity"); submitted.Add((UnitAttackOfOpportunity)__0); return false; }
    static void Queued() => nativeQueueCalls++;
    static RuleCheckTargetFlatFooted Flat(RuleCheckTargetFlatFooted rule) { Set(rule, "IsFlatFooted", flatFooted); return rule; }
    static bool IsUnityObject(UnityEngine.Object value) => !ReferenceEquals(value, null);
    static IEnumerable<CodeInstruction> ACScene(IEnumerable<CodeInstruction> input) {
        foreach (var code in input) {
            if (code.operand is MethodInfo m && m.DeclaringType == typeof(UnityEngine.Object) && m.Name == "op_Implicit") { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(AntimagicRage01434Tests), nameof(IsUnityObject)); }
            if (code.operand is MethodInfo rule && rule.DeclaringType == typeof(Rulebook) && rule.IsGenericMethod && rule.GetGenericArguments()[0] == typeof(RuleCheckTargetFlatFooted)) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(AntimagicRage01434Tests), nameof(Flat)); }
            yield return code;
        }
    }
    static bool Context(ref RulebookEventContext __result) { if (current == null) return true; __result = current; return false; }
    static bool D20(ref RuleRollD20 __result) { __result = RuleRollD20.FromInt(current.LastEvent<RulebookEvent>().Initiator, roll); return false; }
    static RuleCalculateAbilityParams Parameters(RuleCalculateAbilityParams rule) {
        nativeParams++; rule.ReplaceCasterLevel = 12; rule.ReplaceSpellLevel = 1; rule.ReplaceConcentration = 10;
        RulebookEventBus.OnEventAboutToTrigger(rule); rule.OnTrigger(current); RulebookEventBus.OnEventDidTrigger(rule); return rule;
    }
    static IEnumerable<CodeInstruction> DefensiveBoundary(IEnumerable<CodeInstruction> input) {
        foreach (var code in input) {
            if (code.operand is MethodInfo m && m.DeclaringType == typeof(Rulebook) && m.Name == "Trigger" && m.IsGenericMethod && m.GetGenericArguments()[0] == typeof(RuleCalculateAbilityParams)) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(AntimagicRage01434Tests), nameof(Parameters)); }
            yield return code;
        }
    }
    sealed class OpportunityObserver : IAttackOfOpportunityHandler {
        public void HandleAttackOfOpportunity(UnitEntityData unit, UnitEntityData target) { nativeOpportunityEvents++; }
    }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == faultRoot)) throw new InvalidOperationException("034 antimagic injected append failure"); }
    static UnitEntityData Unit(BlueprintsCache cache, string label, string classId = "f7d7eb166b3dd594fb330d085df41853", int level = 12) {
        var u = (UnitEntityData)AccessTools.Method(typeof(FlameJetLifecycle01430Tests), "Unit").Invoke(null, new object[] { cache, "antimagic034-" + label });
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, classId)) { Level = level });
        Set(u, "UniqueId", "antimagic034-unit-" + ++serial); var proxy = FormatterServices.GetUninitializedObject(AccessTools.Inner(typeof(EntityService), "Proxy")); Set(proxy, "Entity", u); Set(u, "Proxy", proxy);
        Set(u, "Abilities", u.Facts.EnsureFactProcessor<Kingmaker.UnitLogic.Abilities.AbilityCollection>());
        Set(u, "CombatState", Empty<UnitCombatState>()); Set(u.CombatState, "Unit", u); Set(u.CombatState, "Cooldown", new UnitCombatState.Cooldowns());
        Set(u.CombatState, "m_EngagedBy", new Dictionary<UnitEntityData, TimeSpan>()); Set(u.CombatState, "m_EngagedUnits", new Dictionary<UnitEntityData, TimeSpan>());
        Set(u.CombatState, "DisengageAttackTargets", new List<UnitEntityData>()); Set(u.CombatState, "m_InCombat", true); Set(u, "Commands", new UnitCommands(u));
        if (u.Passive == null) Set(u, "Passive", new ObservableCountFlag());
        u.Stats.AC.BaseValue = 10; u.Stats.Dexterity.BaseValue = 14; u.Stats.Charisma.BaseValue = 18;
        units.Add(u); return u;
    }
    static Feature Add(UnitEntityData u, BlueprintFeature bp) { var fact = new Feature(bp, u.Descriptor); u.Facts.Add(fact); Check(fact.IsActive && fact.IsTurnedOn, "native feature activation " + bp.name); return fact; }
    static Buff Effect(UnitEntityData target, BlueprintBuff bp, UnitEntityData caster = null) { var fact = new Buff(bp, new MechanicsContext(caster ?? target, target.Descriptor, bp), null); target.Facts.Add(fact); Check(fact.IsActive && fact.IsTurnedOn, "native buff activation " + bp.name); return fact; }
    static void Remove(EntityFact fact) { if (fact.Manager != null) fact.Manager.Remove(fact); }
    static bool CanSelect(UnitEntityData u, BlueprintFeatureSelection p, BlueprintFeature root) { selections++; return (bool)AccessTools.Method(typeof(EnhancementLifecycle01430Tests), "CanSelect").Invoke(null, new object[] { u, p, root }); }
    static void Registration(BlueprintsCache cache) {
        var ids = (string[])AccessTools.Field(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.RagePowers"), "SelectionIds").GetValue(null);
        var pools = ids.Select(id => Native<BlueprintFeatureSelection>(cache, id)).ToArray();
        var before = pools.Select(p => new[] { ((BlueprintFeatureReference[])Get(p, "m_Features")).Select(r => r.Guid).ToArray(), ((BlueprintFeatureReference[])Get(p, "m_AllFeatures")).Select(r => r.Guid).ToArray() }).ToArray();
        var nativeDisruptive = Native<BlueprintFeature>(cache, "f3ef866a9da642c4b9f7dcd76c3567fe");
        var validate = AccessTools.Method(typeof(RageDisruptiveGrant01434), "ValidateNativeGrant");
        validate.Invoke(null, new object[] { nativeDisruptive }); Check(true, "strict native passive graph validates actual latest game payload");
        var oldNativeComponents = nativeDisruptive.ComponentsArray;
        foreach (BlueprintComponent injected in new BlueprintComponent[] { null, new AddFacts() }) {
            bool rejected = false; nativeDisruptive.ComponentsArray = oldNativeComponents.Concat(new[] { injected }).ToArray();
            try { validate.Invoke(null, new object[] { nativeDisruptive }); } catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; } finally { nativeDisruptive.ComponentsArray = oldNativeComponents; }
            Check(rejected, "strict passive graph rejects null components and extra grants without altering native payload");
        }
        var nativeAura = Native<BlueprintBuff>(cache, "b5dc034170f2f7249bafedf6c6f526dc"); var oldAuraComponents = nativeAura.ComponentsArray;
        nativeAura.ComponentsArray = oldAuraComponents.Concat(new BlueprintComponent[] { new AddFacts() }).ToArray();
        bool rejectsNested = false; try { validate.Invoke(null, new object[] { nativeDisruptive }); } catch (TargetInvocationException ex) { rejectsNested = ex.InnerException is InvalidOperationException; } finally { nativeAura.ComponentsArray = oldAuraComponents; }
        Check(rejectsNested, "strict passive graph rejects nested grant payload in exact native aura");
        var nativeArea = Native<BlueprintAbilityAreaEffect>(cache, "e57906afd2116b94aac2220d5912f4ea").GetComponent<AbilityAreaEffectRunAction>(); var oldEnter = nativeArea.UnitEnter;
        nativeArea.UnitEnter = new Kingmaker.ElementsSystem.ActionList { Actions = new Kingmaker.ElementsSystem.GameAction[] { new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage() } };
        bool rejectsAction = false; try { validate.Invoke(null, new object[] { nativeDisruptive }); } catch (TargetInvocationException ex) { rejectsAction = ex.InnerException is InvalidOperationException; } finally { nativeArea.UnitEnter = oldEnter; }
        Check(rejectsAction && typeof(RageDisruptiveGrant01434).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length == 0, "runtime grant has no injectable fact field and strict graph rejects active nested payload");
        Register(cache); var roots = Tokens.Select(t => Asset<BlueprintFeature>(cache, t)).ToArray();
        for (int i = 0; i < roots.Length; i++) {
            Check(roots[i].Groups.SequenceEqual(new[] { FeatureGroup.RagePower }) && roots[i].IsClassFeature && roots[i].Ranks == 1, "separate native rage-power root " + Tokens[i]);
            Check(roots[i].ComponentsArray.Select(c => c.name).Distinct().Count() == roots[i].ComponentsArray.Length, "unique authored runtime component names");
            foreach (var p in pools) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Check(((BlueprintFeatureReference[])Get(p, field)).Count(r => r.Guid == roots[i].AssetGuid) == 1, "new power appears once in real pool " + p.name);
            Register(cache, Methods[i]); Check(ReferenceEquals(roots[i], Asset<BlueprintFeature>(cache, Tokens[i])), "repeat preserves exact root identity");
        }
        for (int i = 0; i < pools.Length; i++) for (int j = 0; j < 2; j++) Check(((BlueprintFeatureReference[])Get(pools[i], j == 0 ? "m_Features" : "m_AllFeatures")).Select(r => r.Guid).Take(before[i][j].Length).SequenceEqual(before[i][j]), "all previous pool entries/order retained");
        foreach (int i in Enumerable.Range(0, 3)) {
            var root = roots[i]; var oldGroups = root.Groups; root.Groups = new[] { FeatureGroup.Feat };
            bool rejected = false; try { Register(cache, Methods[i]); } catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; } finally { root.Groups = oldGroups; }
            Check(rejected && ReferenceEquals(cache.Load(root.AssetGuid), root), "foreign authored metadata rejects without replacement");
            var arrays = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            faultRoot = root.AssetGuid; failAppend = true; rejected = false;
            try { Register(cache, Methods[i]); } catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; } finally { failAppend = false; }
            Check(rejected, "real late append failure is detected");
            for (int p = 0; p < pools.Length; p++) for (int j = 0; j < 2; j++) Check(ReferenceEquals(arrays[p][j], Get(pools[p], j == 0 ? "m_Features" : "m_AllFeatures")), "late append rollback retains exact native array");
            Register(cache, Methods[i]);
        }
        var pool = pools[0];
        foreach (int i in Enumerable.Range(0, 3)) foreach (int level in new[] { new[] { 6, 8, 12 }[i] - 1, new[] { 6, 8, 12 }[i] }) {
            var u = Unit(cache, "selection-" + i + "-" + level, level: level);
            Check(!CanSelect(u, pool, roots[i]), "missing chain prerequisite rejected");
            Add(u, Asset<BlueprintFeature>(cache, i == 2 ? "DisruptiveRagePower" : "SuperstitionRagePower"));
            bool legal = level == new[] { 6, 8, 12 }[i]; Check(CanSelect(u, pool, roots[i]) == legal, "native actual minimum level and prior power " + i + "/" + level);
            if (legal) { var owned = Add(u, roots[i]); Check(!CanSelect(u, pool, roots[i]), "already owned power excluded"); Remove(owned); Check(CanSelect(u, pool, roots[i]), "native removal restores eligibility"); }
        }
    }
    static int Armor(UnitEntityData attacker, UnitEntityData defender, AttackType attack) {
        var rule = new RuleCalculateAC(attacker, defender, attack); RulebookEventBus.OnEventAboutToTrigger(rule); rule.OnTrigger(null); RulebookEventBus.OnEventDidTrigger(rule); acCalls++; return rule.Result;
    }
    static void Ghost(BlueprintsCache cache) {
        var attacker = Unit(cache, "ghost-attacker"); var root = Asset<BlueprintFeature>(cache, Tokens[0]); var buff = Asset<BlueprintBuff>(cache, "GhostRagerRageEffectBuff");
        foreach (bool reverse in new[] { false, true }) foreach (int copies in new[] { 2, 3 }) foreach (int gap in new[] { 1, 4, 12 }) foreach (int morale in new[] { 0, 2, 8 }) {
            var u = Unit(cache, "ghost-overlap", level: 6); var overlapCaster = Unit(cache, "ghost-overlap-source", level: 20);
            u.Stats.AC.AddModifier(gap, (EntityFact)null, ModifierDescriptor.Armor); u.Stats.AC.AddModifier(morale, (EntityFact)null, ModifierDescriptor.Morale);
            int normal = Armor(attacker, u, AttackType.Melee), touch = Armor(attacker, u, AttackType.Touch);
            var weak = new List<Buff>(); Buff strong = null;
            if (reverse) strong = Effect(u, buff, overlapCaster);
            for (int i = 1; i < copies; i++) weak.Add(Effect(u, buff));
            if (!reverse) strong = Effect(u, buff, overlapCaster);
            Check(Armor(attacker, u, AttackType.Touch) == touch + Math.Min(gap, Math.Max(0, 7 - morale)) && Armor(attacker, u, AttackType.Touch) <= normal, "two/three Ghost sources cannot inflate full-AC cap in either order " + copies + "/" + gap + "/" + morale + "/" + reverse);
            var repeat = new RuleCalculateAC(attacker, u, AttackType.Touch); RulebookEventBus.OnEventAboutToTrigger(repeat); var once = repeat.AllBonuses.ToArray(); RulebookEventBus.OnEventAboutToTrigger(repeat);
            Check(repeat.AllBonuses.SequenceEqual(once), "one native AC event cannot receive Ghost contribution twice");
            strong.TurnOff(); Check(Armor(attacker, u, AttackType.Touch) == touch + Math.Min(gap, Math.Max(0, 3 - morale)), "strong-source turn off exposes highest remaining weak source");
            strong.TurnOn(); Check(Armor(attacker, u, AttackType.Touch) == touch + Math.Min(gap, Math.Max(0, 7 - morale)), "strong-source reenable restores one capped bonus");
            Remove(strong); Check(Armor(attacker, u, AttackType.Touch) == touch + Math.Min(gap, Math.Max(0, 3 - morale)), "strong-source removal keeps weaker effect");
            foreach (var effect in weak) Remove(effect); Check(Armor(attacker, u, AttackType.Touch) == touch && Armor(attacker, u, AttackType.Melee) == normal, "all overlapping sources removed restore original AC");
        }
        foreach (int level in new[] { 6, 7, 8, 11, 12, 16, 20, 24 }) foreach (int gap in new[] { 0, 1, 3, 10 }) foreach (int morale in new[] { 0, 2, 4, 8 }) foreach (bool flat in new[] { false, true }) {
            var u = Unit(cache, "ghost-ac", level: level); var feature = Add(u, root); flatFooted = flat;
            var armor = u.Stats.AC.AddModifier(gap, (EntityFact)null, ModifierDescriptor.Armor); var existing = u.Stats.AC.AddModifier(morale, (EntityFact)null, ModifierDescriptor.Morale);
            int ordinary = Armor(attacker, u, AttackType.Melee), touch = Armor(attacker, u, AttackType.Touch), ranged = Armor(attacker, u, AttackType.Ranged);
            var effect = Effect(u, buff); int bonus = Math.Min(7, 2 + level / 4), expected = Math.Min(gap, Math.Max(0, bonus - morale));
            Check(Armor(attacker, u, AttackType.Touch) == touch + expected && Armor(attacker, u, AttackType.RangedTouch) == touch + expected, "native final touch AC applies morale highest only and full cap " + level + "/" + gap + "/" + morale + "/" + flat);
            Check(Armor(attacker, u, AttackType.Melee) == ordinary && Armor(attacker, u, AttackType.Ranged) == ranged, "ordinary AC is unchanged including flatfooted ordinary attacks");
            Check(u.Stats.AC.ModifiedValue == 12 + gap + morale && u.Stats.AC.Modifiers.Count(m => ReferenceEquals(m.Source, effect)) == 0, "temporary native AC scope leaves no permanent modifier");
            effect.TurnOff(); Check(Armor(attacker, u, AttackType.Touch) == touch, "native effect turn off removes touch benefit"); effect.TurnOn(); Check(Armor(attacker, u, AttackType.Touch) == touch + expected, "native turn on restores one benefit");
            Remove(effect); Check(Armor(attacker, u, AttackType.Touch) == touch, "native manager remove removes touch bonus"); Remove(feature); u.Stats.AC.RemoveModifier(armor); u.Stats.AC.RemoveModifier(existing); u.Facts.TurnOff();
        }
        flatFooted = false;
        var source = Unit(cache, "ghost-source", level: 20); var pet = Unit(cache, "ghost-recipient", level: 1); pet.Stats.AC.AddModifier(12, (EntityFact)null, ModifierDescriptor.Armor);
        var shared = Effect(pet, buff, source); Check(Armor(attacker, pet, AttackType.Touch) == 19, "shared effect uses original caster20 and not recipient1"); Remove(shared);
        var wielder = Unit(cache, "ghost-damage"); var target = Unit(cache, "ghost-target"); var ghost = Effect(wielder, buff);
        var incorporeal = Native<BlueprintFeature>(cache, "c4a7f98d743bc784c9d4cf2105852c39");
        var copy = AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "CopyData").MakeGenericMethod(typeof(BlueprintComponent));
        var defense = new BlueprintBuff { name = "antimagic034-native-incorporeal-defense", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")),
            ComponentsArray = incorporeal.ComponentsArray.Where(c => c is AddIncorporealDamageDivisor || c is GhostCriticalAndPrecisionImmunity).Select(c => (BlueprintComponent)copy.Invoke(null, new object[] { c })).ToArray() };
        Check(defense.ComponentsArray.Length == 2, "actual native incorporeal divisor and precision/critical defense components available"); defense.OnEnable(); var nativeDefense = Effect(target, defense);
        foreach (bool active in new[] { true, false, true }) {
            if (active) ghost.TurnOn(); else ghost.TurnOff();
            var physical = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1, DiceType.D6)), 0, PhysicalDamageForm.Slashing);
            var energy = new EnergyDamage(new DiceFormula(1, DiceType.D6), DamageEnergyType.Fire);
            var bundle = new DamageBundle(physical, energy) { Weapon = Empty<ItemEntityWeapon>() }; var parent = new RuleDealDamage(wielder, target, bundle); var damage = new RulePrepareDamage(parent);
            RulebookEventBus.OnEventAboutToTrigger(damage); RulebookEventBus.OnEventDidTrigger(damage); prepareCalls++;
            Check(((physical.Reality & DamageRealityType.Ghost) != 0) == active && energy.Reality == 0, "native outgoing physical Ghost property scope and no energy rewrite");
            Check(physical.EnchantmentTotal == 0, "nonmagical physical attack remains nonmagical while bypassing incorporeal reality");
            var calculate = Empty<RuleCalculateDamage>(); Set(calculate, "ParentRule", parent); Set(calculate, "Initiator", wielder); Set(calculate, "Target", target); RulebookEventBus.OnEventAboutToTrigger(calculate); RulebookEventBus.OnEventDidTrigger(calculate);
            Check(physical.Decline.Value == (active ? DamageDeclineType.None : DamageDeclineType.Total), "actual native incorporeal defenses admit full nonmagical physical Ghost damage only while active");
            Check(energy.Declines.Any(d => d.IncorporealDamageDivisor), "outgoing Ghost power does not bypass native energy reduction");
        }
        Remove(ghost); Remove(nativeDefense);
    }
    static void Disruptive(BlueprintsCache cache) {
        var feat = Native<BlueprintFeature>(cache, "f3ef866a9da642c4b9f7dcd76c3567fe"); var effect = Asset<BlueprintBuff>(cache, "DisruptiveRageEffectBuff");
        foreach (bool preexisting in new[] { false, true }) foreach (bool firstTemporary in new[] { false, true }) {
            var u = Unit(cache, "disruptive-ownership"); var fighter = Native<BlueprintCharacterClass>(cache, "48ac8db94d5de7645906c7d0ad3bcfbd");
            u.Progression.Classes.Add(new ClassData(fighter) { Level = 6 }); Feature owned = null;
            if (preexisting) { owned = u.Progression.Features.AddFeature(feat, null); owned.SetSource(new FeatureSource(fighter), 6); Check(owned.SourceClass == fighter && owned.SourceLevel == 6 && owned.Rank == 1, "actual native permanent feat AddFeature and fighter6 source are authored"); }
            var temp = Effect(u, effect); Check(u.HasFact(feat), "native AddFacts grants actual feat, not same-name clone");
            if (preexisting && firstTemporary) { Remove(owned); Check(u.HasFact(feat), "removing independent feat retains temporary source"); }
            temp.Deactivate(); Check(u.HasFact(feat) == (preexisting && !firstTemporary), "native temporary deactivation preserves only independent source preexisting=" + preexisting + ", nativeRemovedFirst=" + firstTemporary + ", has=" + u.HasFact(feat) + ", facts=" + string.Join("/", u.Facts.List.Select(f => f.Blueprint?.name + ":" + f.IsActive + ":" + f.IsTurnedOn)));
            temp.Activate(); Check(u.HasFact(feat), "native temporary activation restores feat"); Remove(temp);
            Check(u.HasFact(feat) == (preexisting && !firstTemporary), "temporary manager removal never deletes independent feat"); if (owned?.Manager != null) Remove(owned);
        }
        Feature NativeFeat(UnitEntityData unit) => unit.Progression.Features.Enumerable.SingleOrDefault(f => f.Blueprint == feat);
        RageDisruptiveState01434 Receipt(Buff b) => b.Components.Single(c => c.SourceBlueprintComponent is RageDisruptiveGrant01434).GetData<RageDisruptiveState01434>();
        foreach (bool reverse in new[] { false, true }) {
            var u = Unit(cache, "disruptive-multiple-sources"); var a = Effect(u, effect); var b = Effect(u, effect); var owned = NativeFeat(u);
            Check(owned != null && owned.IsTemporary && Receipt(a).Owned && Receipt(b).Owned && Receipt(a).Feature.FactId == owned.UniqueId && Receipt(b).Feature.FactId == owned.UniqueId, "two native rage sources share exactly one owned native feat and persistent receipt");
            var saveSettings = new JsonSerializerSettings { ContractResolver = new Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver() };
            var saveSerializer = JsonSerializer.Create(saveSettings);
            var receiptA = JObject.FromObject(Receipt(a), saveSerializer); var receiptB = JObject.FromObject(Receipt(b), saveSerializer);
            Check(receiptA.Properties().Select(p => p.Name).OrderBy(n => n).SequenceEqual(new[] { "Feature", "Owned" }) && (bool)receiptA["Owned"], "serialized Disruptive receipt contains native fact reference and ownership only");
            JsonConvert.PopulateObject(receiptA.ToString(), Receipt(a), saveSettings); JsonConvert.PopulateObject(receiptB.ToString(), Receipt(b), saveSettings);
            var originalFacts = u.Facts.List.ToArray(); u.Facts.TurnOff(); Check(ReferenceEquals(NativeFeat(u), owned) && Receipt(a).Owned && Receipt(b).Owned && u.Facts.List.SequenceEqual(originalFacts), "manager suspension preserves both JSON-restored receipts and exact native fact collection");
            u.Facts.TurnOn(); Check(ReferenceEquals(NativeFeat(u), owned) && NativeFeat(u).Rank == 1, "manager resume never duplicates or reclaims the native feature");
            u.Facts.ApplyPostLoadFixes(); Check(ReferenceEquals(NativeFeat(u), owned) && owned.Rank == 1 && Receipt(a).Owned && Receipt(b).Owned, "actual native manager post-load fixes preserve restored two-source receipts and native feat identity");
            a.TurnOff(); a.TurnOn(); Check(ReferenceEquals(NativeFeat(u), owned), "direct TurnOff/On preserves native temporary grant lifetime");
            a.Reapply(); Check(ReferenceEquals(NativeFeat(u), owned) && Receipt(a).Owned && Receipt(b).Owned, "native Reapply preserves both source receipts and owned feature identity");
            Remove(reverse ? b : a); Check(ReferenceEquals(NativeFeat(u), owned), "first source removal transfers ownership to remaining native rage source");
            Remove(reverse ? a : b); Check(!u.HasFact(feat), "last native rage source removes only the temporary instance");
        }
        var nativeFighter = Native<BlueprintCharacterClass>(cache, "48ac8db94d5de7645906c7d0ad3bcfbd");
        foreach (bool sourcedCandidate in new[] { false, true }) {
            var u = Unit(cache, "disruptive-late-permanent"); u.Progression.Classes.Add(new ClassData(nativeFighter) { Level = 6 });
            var a = Effect(u, effect); var b = Effect(u, effect); var temporary = NativeFeat(u);
            var contextAbility = new BlueprintAbility { name = "antimagic034-external-grant", Type = AbilityType.Supernatural, ComponentsArray = Array.Empty<BlueprintComponent>() }; contextAbility.OnEnable();
            var candidateContext = new MechanicsContext(u, u.Descriptor, contextAbility);
            var sourceBlueprint = new BlueprintFeature { name = "antimagic034-legitimate-external-source", Ranks = 1, ComponentsArray = Array.Empty<BlueprintComponent>() }; sourceBlueprint.OnEnable();
            var metadataSource = Add(u, sourceBlueprint);
            var candidate = new Feature(feat, u.Descriptor, sourcedCandidate ? candidateContext : null) { IgnorePrerequisites = sourcedCandidate };
            if (sourcedCandidate) { candidate.SetSource(new FeatureSource(nativeFighter), 6); candidate.SourceAbility = contextAbility; Set(candidate, "SourceFact", metadataSource); }
            var authoredContext = candidate.Context;
            var permanent = u.Facts.Add(candidate);
            if (!sourcedCandidate) permanent.SetSource(new FeatureSource(nativeFighter), 6);
            Check(ReferenceEquals(permanent, temporary) && !permanent.IsTemporary && !Receipt(a).Owned && !Receipt(b).Owned && permanent.SourceClass == nativeFighter && permanent.SourceLevel == 6 && permanent.Rank == 1, "native late permanent acquisition hands off exact instance and both ownership receipts with fighter6 metadata");
            Check(ReferenceEquals(permanent.Context, authoredContext) && ReferenceEquals(permanent.SourceAbility, sourcedCandidate ? contextAbility : null) && ReferenceEquals(permanent.SourceFact, sourcedCandidate ? metadataSource : null) && permanent.IgnorePrerequisites == sourcedCandidate, "handoff copies candidate context and legal source metadata rather than retaining temporary context sourced=" + sourcedCandidate + ", ctx=" + ReferenceEquals(permanent.Context, authoredContext) + ", ability=" + ReferenceEquals(permanent.SourceAbility, sourcedCandidate ? contextAbility : null) + ", fact=" + ReferenceEquals(permanent.SourceFact, sourcedCandidate ? metadataSource : null) + ", ignore=" + permanent.IgnorePrerequisites);
            Remove(a); Remove(b); Check(ReferenceEquals(NativeFeat(u), permanent) && permanent.SourceClass == nativeFighter && permanent.SourceLevel == 6, "both rage sources removed after permanent acquisition retain actual native permanent feat and source"); Remove(permanent); Remove(metadataSource);
        }
        foreach (bool foreignFirst in new[] { false, true }) foreach (bool removeForeignFirst in new[] { false, true }) {
            var u = Unit(cache, "disruptive-foreign-grant");
            var grant = new AddFacts { name = "antimagic034-external-addfacts" }; Set(grant, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(feat) });
            var external = new BlueprintFeature { name = "antimagic034-external-feature", Ranks = 1, ComponentsArray = new BlueprintComponent[] { grant } }; external.OnEnable();
            Feature foreign = foreignFirst ? Add(u, external) : null; var a = Effect(u, effect); var b = Effect(u, effect); if (!foreignFirst) foreign = Add(u, external);
            Check(u.HasFact(feat) && !Receipt(a).Owned && !Receipt(b).Owned && u.Progression.Features.Enumerable.Count(f => f.Blueprint == feat) == 1, "external native AddFacts before/after two rage sources borrows or receives one native feat");
            if (removeForeignFirst) {
                Remove(foreign); Check(u.HasFact(feat) && Receipt(a).Owned && Receipt(b).Owned, "native foreign AddFacts removal lost-fact event reacquires one own temporary native feature");
                var replacement = NativeFeat(u); Remove(a); Check(ReferenceEquals(NativeFeat(u), replacement), "remaining source retains reacquired feature"); Remove(b); Check(!u.HasFact(feat), "last source cleans reacquired feature after external grant ended");
            } else {
                var borrowed = NativeFeat(u); Remove(a); Remove(b); Check(ReferenceEquals(NativeFeat(u), borrowed), "rage sources ending first preserve native external AddFacts grant"); Remove(foreign); Check(!u.HasFact(feat), "external native AddFacts final removal retains its original ownership behavior");
            }
        }
        {
            var u = Unit(cache, "disruptive-unrelated-native-path"); var incoming = new Feature(feat, u.Descriptor); var existing = u.Facts.Add(incoming); var second = u.Facts.Add(new Feature(feat, u.Descriptor));
            Check(ReferenceEquals(incoming, existing) && ReferenceEquals(second, existing) && !existing.IsTemporary && existing.Source == null, "no own rage source leaves native rank-one merge and source behavior unchanged"); Remove(existing);
            var a = Effect(u, effect); var unrelated = new BlueprintFeature { name = "antimagic034-unrelated", Ranks = 1, ComponentsArray = Array.Empty<BlueprintComponent>() }; unrelated.OnEnable();
            var other = u.Progression.Features.AddFeature(unrelated, null); other.SetSource(new FeatureSource(nativeFighter), 6); var duplicate = u.Progression.Features.AddFeature(unrelated, null);
            Check(ReferenceEquals(other, duplicate) && other.SourceClass == nativeFighter && other.SourceLevel == 6 && Receipt(a).Owned, "unrelated feat merge is wholly untouched while Disruptive source exists"); Remove(a); Check(u.HasFact(unrelated) && !u.HasFact(feat), "unrelated native feature survives own Disruptive cleanup"); Remove(other);
        }
        var caster = Unit(cache, "disruptive-caster"); var hostile = Unit(cache, "disruptive-hostile"); var ally = Unit(cache, "disruptive-ally"); enemy = hostile;
        var area = Native<BlueprintAbilityAreaEffect>(cache, "e57906afd2116b94aac2220d5912f4ea"); var run = area.GetComponent<AbilityAreaEffectRunAction>();
        var context = new MechanicsContext(caster, caster.Descriptor, area); context.SetParams(new AbilityParams());
        var instance = Empty<AreaEffectEntityData>(); Set(instance, "m_Context", context); Set(instance, "m_Blueprint", area); Set(instance, "UniqueId", "antimagic034-native-area");
        foreach (var target in new[] { hostile, ally }) { run.HandleUnitEnter(context, instance, target); Check(target.HasFact(Native<BlueprintBuff>(cache, "00c4eb8ffaf4b7d47bea8c8307704f3c")) == (target == hostile), "actual native area enemy/allied target condition"); }
        var ability = new BlueprintAbility { name = "antimagic034-spell", Type = AbilityType.Spell, ComponentsArray = Array.Empty<BlueprintComponent>() }; ability.OnEnable();
        var check = new RuleCalculateAbilityParams(hostile, new AbilityData(ability, hostile.Descriptor)); Parameters(check); Check(check.Result.Concentration == 6, "actual native aura ConcentrationBonus applies minus4");
        run.HandleUnitExit(context, instance, hostile); var plain = new RuleCalculateAbilityParams(hostile, new AbilityData(ability, hostile.Descriptor)); Parameters(plain); Check(plain.Result.Concentration == 10, "actual native aura exit removes concentration penalty");
        enemy = null;
    }
    static void Engage(UnitEntityData attacker, UnitEntityData target) {
        ((IDictionary)Get(attacker.CombatState, "m_EngagedUnits"))[target] = TimeSpan.FromHours(1);
        ((IDictionary)Get(target.CombatState, "m_EngagedBy"))[attacker] = TimeSpan.FromHours(1);
    }
    static RuleCheckCastingDefensively Defensive(UnitEntityData caster, UnitCombatEngagementController controller) {
        var bp = new BlueprintAbility { name = "antimagic034-defensive", Type = AbilityType.Spell, ComponentsArray = Array.Empty<BlueprintComponent>() }; bp.OnEnable();
        var rule = new RuleCheckCastingDefensively(new AbilityData(bp, caster.Descriptor)); var before = current; current = new RulebookEventContext(); current.PushEvent(rule);
        try { rule.OnTrigger(current); defensiveCalls++; new MagusController().OnEventDidTrigger(rule); controller.Tick(); return rule; } finally { current = before; }
    }
    static void Spellbreaker(BlueprintsCache cache, UnitCombatEngagementController controller) {
        var effect = Asset<BlueprintBuff>(cache, "SpellbreakerRageEffectBuff"); var owner = Unit(cache, "spellbreaker-owner"); var caster = Unit(cache, "spellbreaker-caster"); enemy = caster;
        var part = owner.Ensure<UnitPartMagus>(); part.Counterstrike.Retain(); part.WandWielder.Retain(); part.SpellCombat.Unlock();
        var buff = Effect(owner, effect); Check(part.Counterstrike.Count == 2 && part.WandWielder.Count == 1 && part.SpellCombat.Unlocked, "temporary retained flag composes with independent Magus mechanics");
        owner.Facts.TurnOff(); Check(part.Counterstrike.Count == 1 && ReferenceEquals(owner.Get<UnitPartMagus>(), part), "manager off releases exactly own counter and preserves part"); owner.Facts.TurnOn(); Check(part.Counterstrike.Count == 2, "manager restart retains once");
        buff.TurnOff(); buff.TurnOn(); Check(part.Counterstrike.Count == 2, "direct effect restart does not duplicate Counterstrike"); Remove(buff); Check(part.Counterstrike.Count == 1 && part.WandWielder.Count == 1 && part.SpellCombat.Unlocked && ReferenceEquals(owner.Get<UnitPartMagus>(), part), "removal preserves other Counterstrike/WandWielder/SpellCombat sources"); part.Counterstrike.Release();
        var one = Effect(owner, effect); var two = Effect(owner, effect); Check(part.Counterstrike.Count == 2, "two separate rage sources retain separately"); Remove(one); Check(part.Counterstrike.Count == 1, "removing one source retains other"); Remove(two); Check(part.Counterstrike.Count == 0, "last source releases only own final contribution");
        var nativeCounterstrike = Native<BlueprintFeature>(cache, "cd96b7275c206da4899c69ae127ffda6");
        foreach (bool nativeFirst in new[] { false, true }) {
            var independent = Add(owner, nativeCounterstrike); var temporary = Effect(owner, effect);
            Check(part.Counterstrike.Count == 2, "actual native Counterstrike fact and temporary source both retain");
            if (nativeFirst) { Remove(independent); Check(part.Counterstrike.Count == 1 && ReferenceEquals(owner.Get<UnitPartMagus>(), part), "actual native Counterstrike removal preserves active Spellbreaker on a Magus"); Remove(temporary); }
            else { Remove(temporary); Check(part.Counterstrike.Count == 1, "temporary removal preserves actual native Counterstrike fact"); Remove(independent); }
            Check(part.Counterstrike.Count == 0 && part.WandWielder.Count == 1 && part.SpellCombat.Unlocked, "both actual removal orders preserve unrelated Magus mechanics");
        }
        foreach (bool nativeFirst in new[] { false, true }) {
            var outsider = Unit(cache, "spellbreaker-native-part-removal"); var independent = Add(outsider, nativeCounterstrike);
            var first = Effect(outsider, effect); var second = Effect(outsider, effect); var originalPart = outsider.Get<UnitPartMagus>();
            Check(originalPart.Counterstrike.Count == 3, "non-magus native Counterstrike plus two own sources retain separately");
            if (nativeFirst) {
                Remove(independent); var replacement = outsider.Get<UnitPartMagus>();
                Check(replacement != null && !ReferenceEquals(replacement, originalPart) && replacement.Counterstrike.Count == 2 && originalPart.Counterstrike.Count == 0, "native lost-fact event transfers both own receipts after native whole-part removal replacementNull=" + (replacement == null) + ", same=" + ReferenceEquals(replacement, originalPart) + ", newCount=" + replacement?.Counterstrike.Count + ", oldCount=" + originalPart.Counterstrike.Count + ", first=" + first.IsActive + "/" + first.IsTurnedOn + ", second=" + second.IsActive + "/" + second.IsTurnedOn);
                Remove(first); Check(replacement.Counterstrike.Count == 1, "first migrated source removal retains second"); Remove(second); Check(replacement.Counterstrike.Count == 0, "last migrated source removal balances exactly");
            } else {
                Remove(first); Remove(second); Check(originalPart.Counterstrike.Count == 1, "own sources removed before native fact retain its flag"); Remove(independent); Check(outsider.Get<UnitPartMagus>() == null, "native final part removal remains original behavior when no own source is active");
            }
        }
        var nativeWand = Native<BlueprintFeature>(cache, "5826562b74d529e41af2d60feeca093d");
        foreach (bool magus in new[] { false, true }) foreach (bool ownSources in new[] { false, true }) {
            var u = Unit(cache, "spellbreaker-wand-native-exit", magus ? "45a4607686d96a1498891b3286121780" : "f7d7eb166b3dd594fb330d085df41853");
            var wand = Add(u, nativeWand); var original = u.Get<UnitPartMagus>(); if (magus) original.SpellCombat.Unlock();
            var a = ownSources ? Effect(u, effect) : null; var b = ownSources ? Effect(u, effect) : null;
            Check(original.WandWielder.Count == 1 && original.Counterstrike.Count == (ownSources ? 2 : 0), "actual native Wand Wielder and own source counts are independent before removal");
            Remove(wand); var after = u.Get<UnitPartMagus>();
            if (magus) Check(ReferenceEquals(after, original) && after.SpellCombat.Unlocked && after.WandWielder.Count == 0 && after.Counterstrike.Count == (ownSources ? 2 : 0), "native Wand Wielder exit on real Magus fixture preserves original Spell Combat part and only own counters");
            else if (ownSources) Check(after != null && !ReferenceEquals(after, original) && after.Counterstrike.Count == 2 && after.WandWielder.Count == 0 && original.Counterstrike.Count == 0, "actual native non-Counterstrike whole-part removal restores only two own Counterstrike receipts");
            else Check(after == null, "native Wand Wielder part deletion with no own sources remains original behavior");
            if (ownSources) { Remove(a); Check(after.Counterstrike.Count == 1, "Wand-removal reconciled first source balances once"); Remove(b); Check(after.Counterstrike.Count == 0, "Wand-removal reconciled last source balances once"); }
        }
        {
            var u = Unit(cache, "spellbreaker-wand-inactive-own"); var wand = Add(u, nativeWand); var inactive = Effect(u, effect); inactive.TurnOff();
            Remove(wand); Check(u.Get<UnitPartMagus>() == null, "native foreign part exit does not recreate a part for turned-off own source");
            inactive.TurnOn(); Check(u.Get<UnitPartMagus>().Counterstrike.Count == 1 && u.Get<UnitPartMagus>().WandWielder.Count == 0, "reenabling own source after native foreign exit restores only own flag"); Remove(inactive); Check(u.Get<UnitPartMagus>().Counterstrike.Count == 0, "reenabled own source releases exactly once");
        }
        var receipt = new RageCounterstrikeState01434 { Retained = true, RetainedPart = part };
        var jsonReceipt = JObject.FromObject(receipt);
        Check(jsonReceipt.Properties().Select(p => p.Name).SequenceEqual(new[] { "Retained" }) && (bool)jsonReceipt["Retained"], "persistent receipt contains own bool but no part or world reference");
        var loadedOwner = Unit(cache, "spellbreaker-controlled-load"); var loaded = Effect(loadedOwner, effect); loaded.TurnOff();
        var loadedRuntime = loaded.Components.Single(c => c.SourceBlueprintComponent is RageSpellbreakerCounterstrike01434);
        var loadedData = loadedRuntime.GetData<RageCounterstrikeState01434>();
        JsonConvert.PopulateObject(jsonReceipt.ToString(), loadedData);
        var loadedPart = loadedOwner.Ensure<UnitPartMagus>(); loadedPart.Counterstrike.Retain(); loadedPart.Counterstrike.Retain();
        string nativeCounter = JsonConvert.SerializeObject(loadedPart.Counterstrike);
        loaded.TurnOn(); Check(loadedPart.Counterstrike.Count == 2 && JsonConvert.SerializeObject(loadedPart.Counterstrike) == nativeCounter, "controlled persisted native counter plus own receipt reenable does not retain again");
        loaded.TurnOff(); Check(loadedPart.Counterstrike.Count == 1 && !loadedData.Retained, "controlled restored receipt releases once and retains independent native counter"); Remove(loaded); loadedPart.Counterstrike.Release();
        var active = Effect(owner, effect); Engage(owner, caster); owner.CombatState.AttackOfOpportunityCount = 1;
        roll = 10; int commands = submitted.Count; var success = Defensive(caster, controller);
        Check(success.Success && success.DC == 17 && success.Concentration == 10 && submitted.Count == commands + 1 && owner.CombatState.AttackOfOpportunityCount == 0, "actual native successful defensive cast -> MagusController -> forced queue -> one native opportunity command and spend");
        commands = submitted.Count; Defensive(caster, controller); Check(submitted.Count == commands, "native opportunity allowance exhausted rejects further reaction");
        owner.CombatState.AttackOfOpportunityCount = 1; owner.CombatState.ClearAttackOfOpportunityTickData(); roll = 1;
        var failed = Defensive(caster, controller); Check(!failed.Success && submitted.Count == commands && owner.CombatState.AttackOfOpportunityCount == 1, "failed defensive check adds no Spellbreaker reaction or allowance spend");
        roll = 10; active.TurnOff(); Defensive(caster, controller); Check(submitted.Count == commands, "inactive rage effect cannot trigger Counterstrike"); active.TurnOn();
        ((IDictionary)Get(owner.CombatState, "m_EngagedUnits")).Clear(); ((IDictionary)Get(caster.CombatState, "m_EngagedBy")).Clear(); Defensive(caster, controller); Check(submitted.Count == commands, "not threatening the caster queues nothing"); Engage(owner, caster);
        aware = false; Defensive(caster, controller); Check(submitted.Count == commands && owner.CombatState.AttackOfOpportunityCount == 1, "native awareness check rejects final opportunity without spend"); aware = true;
        melee = false; Defensive(caster, controller); Check(submitted.Count == commands, "no melee threat hand rejects native opportunity"); melee = true;
        owner.State.AddCondition(UnitCondition.DisableAttacksOfOpportunity); Defensive(caster, controller); Check(submitted.Count == commands, "native DisableAttacksOfOpportunity condition prevents reaction"); owner.State.RemoveCondition(UnitCondition.DisableAttacksOfOpportunity);
        owner.CombatState.ClearAttackOfOpportunityTickData(); Defensive(caster, controller); Check(submitted.Count == commands + 1 && owner.CombatState.AttackOfOpportunityCount == 0, "cleared native restrictions restore exactly one normal reaction");
        Remove(active); enemy = null;
    }
    internal static int Run(BlueprintsCache cache) {
        checks = serial = selections = defensiveCalls = nativeParams = nativeQueueCalls = nativeOpportunityEvents = prepareCalls = acCalls = 0; units.Clear(); submitted.Clear(); flatFooted = false; aware = melee = true;
        var h = new Harmony("CotW01434.AntimagicNativeContracts"); h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.RageDisruptiveAcquisition01434", true)).Patch(); h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.RageSpellbreakerNativeRemoval01434", true)).Patch(); var game = Game.Instance; var stateBefore = game.State; var timeBefore = game.TimeController; var controllerBefore = game.CombatEngagementController;
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player;
        var pool = Empty<EntityPool<UnitEntityData>>(); Set(pool, "m_Entities", new List<UnitEntityData>()); Set(state, "Units", pool); Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        var controller = new UnitCombatEngagementController(); Set(game, "CombatEngagementController", controller); var observer = new OpportunityObserver(); EventBus.Subscribe(observer);
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(AntimagicRage01434Tests), name));
        Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Clock)); Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(Identity)); Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(False)); Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController)); Patch(AccessTools.PropertyGetter(typeof(Game), "CurrentMode"), nameof(GameMode)); Patch(AccessTools.PropertyGetter(typeof(BlueprintUnitFact), "Name"), nameof(HostName));
        Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(False));
        Patch(AccessTools.Method(typeof(UnitEntityData), "IsAlly", new[] { typeof(UnitEntityData) }), nameof(Ally)); Patch(AccessTools.Method(typeof(AddAreaEffect), "IsOwnerAvatarValid"), nameof(False));
        Patch(AccessTools.PropertyGetter(typeof(UnitCombatState), "IsWaitingInitiative"), nameof(False)); Patch(AccessTools.Method(typeof(UnitGroupMemory), "Contains", new[] { typeof(UnitEntityData) }), nameof(Contains)); Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "Memory"), nameof(Memory)); Patch(AccessTools.Method(typeof(UnitHelper), "GetThreatHand"), nameof(Hand));
        Patch(AccessTools.Method(typeof(UnitHelper), "CanAttack", new[] { typeof(UnitEntityData), typeof(Func<UnitEntityData, ItemEntityWeapon>) }), nameof(MayAttack)); Patch(AccessTools.Method(typeof(UnitCommand), "CommandTargetUntargetable"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "HasMotionThisTick"), nameof(False)); Patch(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), nameof(Skip)); Patch(AccessTools.Method(typeof(UnitCommands), "Run", new[] { typeof(UnitCommand) }), nameof(Submit));
        var nativeOpportunity = AccessTools.Method(typeof(UnitCombatState), "AttackOfOpportunity");
        var nativeInstructions = PatchProcessor.GetOriginalInstructions(nativeOpportunity, (ILGenerator)null);
        string originalOpportunityIL = ILSignature(nativeInstructions);
        Check(ReferenceEquals(RefreshOpportunity(nativeInstructions), nativeInstructions), "scene wrapper refresh returns exact native instruction sequence instance");
        string beforeOpportunityIL = ILSignature(PatchProcessor.GetCurrentInstructions(nativeOpportunity, int.MaxValue, null));
        h.Patch(nativeOpportunity, transpiler: new HarmonyMethod(typeof(AntimagicRage01434Tests), nameof(RefreshOpportunity)));
        string refreshedOpportunityIL = ILSignature(PatchProcessor.GetCurrentInstructions(nativeOpportunity, int.MaxValue, null));
        Check(beforeOpportunityIL == originalOpportunityIL && refreshedOpportunityIL == originalOpportunityIL, "native AoO before/after identity refresh preserves all opcodes, member tokens, local types, branch targets and exception blocks");
        h.Patch(AccessTools.Method(typeof(UnitCombatEngagementController), "ForceAttackOfOpportunity"), postfix: new HarmonyMethod(typeof(AntimagicRage01434Tests), nameof(Queued)));
        Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), nameof(Context)); Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), nameof(D20));
        h.Patch(AccessTools.Method(typeof(RuleCheckCastingDefensively), "OnTrigger"), transpiler: new HarmonyMethod(typeof(AntimagicRage01434Tests), nameof(DefensiveBoundary))); h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Howl01428Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(RuleCalculateAC), "OnTrigger"), transpiler: new HarmonyMethod(typeof(AntimagicRage01434Tests), nameof(ACScene)));
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(AntimagicRage01434Tests), nameof(AppendFault)));
        var buffHelper = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var capture = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense"); var outer = Harmony.GetPatchInfo(buffHelper)?.Prefixes.SingleOrDefault(p => p.PatchMethod == capture); if (outer != null) h.Unpatch(buffHelper, capture);
        Provider01419Tests.PatchLabels(h);
        try {
            SuperstitionRage01434Tests.PrepareNative(cache); AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.SuperstitionRage01434"), "Register").Invoke(null, new object[] { cache });
            foreach (string id in (string[])AccessTools.Field(Module, "Dependencies").GetValue(null)) {
                var bp = cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject; var row = PsychicExpansionTests.NativeRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id);
                if (bp != null && bp.ComponentsArray.Length == 0 && row != null && ((JArray)row["blueprint"]["Data"]["Components"]).Count > 0) { bp.ComponentsArray = ((JArray)row["blueprint"]["Data"]["Components"]).Select(c => (BlueprintComponent)PsychicExpansionTests.Read(c, typeof(BlueprintComponent))).ToArray(); bp.OnEnable(); }
            }
            Registration(cache); Ghost(cache); Disruptive(cache); Spellbreaker(cache, controller);
            Check(defensiveCalls > 0 && nativeParams > 0 && nativeQueueCalls > 0 && submitted.Count > 0 && nativeOpportunityEvents > 0 && prepareCalls > 0 && acCalls > 0, "native effect and complete defensive-casting opportunity paths executed");
            var newAssets = Tokens.SelectMany(t => new[] { cache.Load(Id(t)), cache.Load(Id(t.Replace("RagePower", "RageEffectBuff"))) }).ToArray();
            PsychicExpansionTests.Inventory["antimagicRage01434Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 3, ["ordinaryFeatCount"] = 0, ["itemIds"] = new JArray("COTW-0263", "COTW-0284", "COTW-0285"), ["nativeCanSelectCalls"] = selections, ["nativeACCalls"] = acCalls, ["nativeOutgoingDamageCalls"] = prepareCalls, ["nativeDefensiveCastingCalls"] = defensiveCalls, ["nativeParameterCalls"] = nativeParams, ["nativeForcedOpportunityQueueCalls"] = nativeQueueCalls, ["nativeOpportunityCommands"] = submitted.Count, ["nativeOpportunityEvents"] = nativeOpportunityEvents, ["nativeOpportunityIdentityRefreshPreservesIL"] = true, ["nativeOpportunityInstructionCount"] = nativeInstructions.Count, ["nativeOpportunityIlSha256"] = BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(originalOpportunityIL))).Replace("-", ""),
                ["nativeRegistrationAndRepeatIdentity"] = true, ["nativePoolAppendRollback"] = true, ["nativeTouchACMoraleAndFullCap"] = true, ["ordinaryAndFlatFootedACPreserved"] = true, ["nativeOutgoingGhostPhysicalScope"] = true, ["nativeDisruptiveFactOwnership"] = true, ["nativeDisruptiveAreaEntryExitAndConcentration"] = true, ["nativeCounterstrikeLifecycleComposes"] = true, ["nativeDefensiveSuccessMagusQueueAllowanceAndCommand"] = true, ["spellbreakerUsesOriginalCotwSuccessRule"] = true, ["newHarmonyTargetCount"] = 2, ["nativeCounterstrikePostRemovalReconciliation"] = true, ["nativeWandWielderRemovalReconcilesOnlyOwnReceipts"] = true, ["nativeDisruptiveAcquisitionHandoff"] = true, ["nativeDisruptiveMultipleSourcesAndSerializedReceipt"] = true,
                ["newBlueprints"] = JArray.FromObject(newAssets.Select(a => new { id = a.AssetGuid.ToString(), name = a.name, type = a.GetType().FullName })),
                ["boundary"] = "Native manager/fact Add/Remove/TurnOn/TurnOff, real pool CanSelect, original RulebookEventBus subscription proxies, native RuleCalculateAC final descriptor/flatfooted/touch arithmetic, original AddOutgoingPhysicalDamageProperty, native rank-one FeatureCollection acquisition, explicit temporary feature receipts and external AddFacts ownership handoff, exact native Disruptive area HandleUnitEnter/Exit action/condition/buff paths and ConcentrationBonus execute. RuleCheckCastingDefensively.OnTrigger and RuleCalculateAbilityParams.OnTrigger execute with fixed CL12/SL1/base concentration10/d20 and neutral difficulty inputs. The narrow native AddMagusMechanicPart.OnDeactivate postfix executes after its original whole-part removal and moves only outstanding own receipts. Native MagusController checks real engagement dictionaries, native ForceAttackOfOpportunity queues, original engagement Tick drains the queue, original UnitCombatState.AttackOfOpportunity applies native allowance/condition/awareness/threat-hand checks, emits the actual event, creates native UnitAttackOfOpportunity and spends once. Empty world EntityPool, absent view/area spawning, world group membership/knowledge/weapon availability/initiative, default scene game mode and final command scheduler are bounded scene inputs; final damage/animation and complete game saves are tester cases. No tested production lifecycle or rule handler is replaced. A test-only identity transpiler re-emits the native opportunity body after scene getters are installed; before/after original opcode/member-token/branch/local/exception signatures are checked equal. Only the generic nested Rulebook parameter entry is redirected to native event subscription and original native OnTrigger; native flatfooted result is a controlled condition input. Unity object existence in native AC uses a managed null-reference scene equivalent. Full five-host/fact-selection/late-provider startup transactions are additionally exercised by the caller suite." };
            Console.WriteLine("PASS: " + checks + " AntimagicRage01434 native contracts."); return checks;
        } finally {
            foreach (var u in units.AsEnumerable().Reverse()) foreach (var fact in u.Facts.List.ToArray()) if (fact.Manager != null) u.Facts.Remove(fact);
            EventBus.Unsubscribe(observer); h.UnpatchAll(h.Id); if (outer != null) new Harmony(outer.owner).Patch(buffHelper, prefix: new HarmonyMethod(capture) { priority = outer.priority, before = outer.before, after = outer.after });
            Set(game, "State", stateBefore); Set(game, "TimeController", timeBefore); Set(game, "CombatEngagementController", controllerBefore); units.Clear(); enemy = null; current = null;
        }
    }
}
