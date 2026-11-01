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
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
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
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Native Apply, RuleSavingThrow, RuleHealDamage, fact manager, subscription
// proxies and selection entry points execute. Unit/scene setup, raw dice,
// label/FX services and final HP notification are explicit scene boundaries.
internal static class SuperstitionRage01434Tests
{
    static readonly Type Module = typeof(SuperstitionDefense01434).Assembly.GetType("CallOfTheWild.Wrath.Prototype.SuperstitionRage01434", true);
    static readonly Type Migration = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.RagePowerMigration01434", true);
    static readonly Type Assets = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    internal static readonly string[] RootTokens = { "SuperstitionRagePower", "WitchHunterRagePower" };
    static readonly string[] HostIds = { "da8ce41ac3cd74742b80984ccc3c9613", "3513326cd64f475781799685c57fa452", "ecc22ca1eea1bf6488a0d7c6ee2527d8", "75b3978757908d24aaaecaf2dc209b89", "ee51f69e69cb43c3bc7c02002e482d17" };
    static readonly string[] PoolIds = { "28710502f46848d48b3f0d6132817c4e", "2476514e31791394fa140f1a07941c96", "609f0e5336084442a0dafa3abd4d31c5", "0c7f01fbbe687bb4baff8195cb02fe6a" };
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static int checks, serial, saves, heals, applications, additions, removals, selections, d20 = 10;
    static bool ally = true;
    static RulebookEventContext current;
    static readonly List<RuleSavingThrow> recordedSaves = new List<RuleSavingThrow>();
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Call(Type t, string n, params object[] args) => AccessTools.Method(t, n).Invoke(null, args);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Assets, "Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static void Check(bool value, string message) { if (!value) throw new Exception("SuperstitionRage01434: " + message); checks++; }
    static bool Reject(Action f) { try { f(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e is InvalidOperationException; } }
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool Allied(UnitEntityData __instance, UnitEntityData __0, ref bool __result) { __result = ReferenceEquals(__instance, __0) || ally; return false; }
    static void Identity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "034-super-fact-" + ++serial); }
    static bool Context(ref RulebookEventContext __result) { if (current == null) return true; __result = current; return false; }
    static bool Dice(DiceFormula __0, ref int __result) { __result = __0.Dice == DiceType.D20 ? d20 : 0; return false; }
    static bool D20(ref RuleRollD20 __result) { __result = NativeRule(new RuleRollD20(current.LastEvent<RulebookEvent>().Initiator)); return false; }
    static bool Expose(Exception __1) => throw new Exception("01434 native action exception", __1);
    sealed class Events : IUnitBuffHandler {
        public void HandleBuffDidAdded(Buff b) { additions++; }
        public void HandleBuffDidRemoved(Buff b) { removals++; }
    }
    static T NativeRule<T>(T rule) where T : RulebookEvent {
        var old = current; current = new RulebookEventContext(); current.PushEvent(rule);
        var initiator = new RulebookSubscriptionManager<IInitiatorRulebookSubscriber>();
        foreach (var fact in rule.Initiator.Facts.List.ToArray().Where(f => f.IsActive && f.IsTurnedOn && !f.IsDisposed && (!(f is Buff b) || !b.IsSuppressed)))
            foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is IInitiatorRulebookSubscriber))
                initiator.Subscribe((IInitiatorRulebookSubscriber)runtime.SourceBlueprintComponent, (ISubscriptionProxy)runtime);
        try {
            initiator.OnEventAboutToTrigger(rule); if (rule is RuleSavingThrow s) { saves++; recordedSaves.Add(s); }
            rule.OnTrigger(current); if (rule is RuleHealDamage) heals++; initiator.OnEventDidTrigger(rule); rule.OnDidTrigger(); return rule;
        } finally { current = old; }
    }
    static T ContextRule<T>(MechanicsContext context, T rule) where T : RulebookEvent { rule.Reason = context; return NativeRule(rule); }
    static IEnumerable<CodeInstruction> RuleBoundary(IEnumerable<CodeInstruction> code) {
        foreach (var i in code) {
            if (i.operand is MethodInfo m && m.IsGenericMethod && m.Name == "TriggerRule" && m.DeclaringType == typeof(MechanicsContext)) { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(SuperstitionRage01434Tests), nameof(ContextRule)).MakeGenericMethod(m.GetGenericArguments()); }
            else if (i.operand is MethodInfo r && r.IsGenericMethod && r.Name == "Trigger" && r.DeclaringType == typeof(Rulebook)) { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(SuperstitionRage01434Tests), nameof(NativeRule)).MakeGenericMethod(r.GetGenericArguments()); }
            yield return i;
        }
    }
    // The real sticky entry and UnitPartTouch.Init run; the scene command queue
    // after the charge was created is the endpoint, not a simulated touch hit.
    static IEnumerable<CodeInstruction> StickyQueueBoundary(IEnumerable<CodeInstruction> code) {
        int endpoints = 0;
        foreach (var i in code) {
            yield return i;
            if (i.operand is MethodInfo m && m.DeclaringType == typeof(UnitPartTouch) && m.Name == "Init") { yield return new CodeInstruction(OpCodes.Ret); endpoints++; }
        }
        if (endpoints != 1) throw new InvalidOperationException("01434 sticky native charge endpoint changed");
    }
    internal static void PrepareNative(BlueprintsCache cache) {
        var ids = new HashSet<string>(HostIds.Concat(PoolIds).Concat(new[] { "f7d7eb166b3dd594fb330d085df41853", "6afa347d804838b48bda16acb0573dc0", "d77e67a814d686842802c9cfd8ef8499", "4d588290ed0736e4f8cd3f4c3beacd69", "0a5ddfbcfb3989543ac7c936fc256889", "ce0ece459ebed9941bb096f559f36fa8", "5590652e1c2225c4ca30c4a699ab3649" }));
        foreach (var field in new[] { "WillNegates", "FortNegates", "HealingHalved" }) foreach (var id in (string[])AccessTools.Field(Module, field).GetValue(null)) ids.Add(id);
        foreach (var id in ids) {
            var row = PsychicExpansionTests.NativeRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id);
            var bp = Native<BlueprintScriptableObject>(cache, id);
            if (bp == null || row == null) throw new Exception("01434 exact native fixture row missing " + id);
            if (bp.ComponentsArray.Length == 0 && ((JArray)row["blueprint"]["Data"]["Components"]).Count > 0) {
                bp.ComponentsArray = ((JArray)row["blueprint"]["Data"]["Components"]).Select(c => (BlueprintComponent)PsychicExpansionTests.Read(c, typeof(BlueprintComponent))).ToArray(); bp.OnEnable();
            }
            if (bp is BlueprintFeatureSelection pool && PoolIds.Contains(id) && pool.Group == FeatureGroup.None) {
                foreach (var name in new[] { "Group", "Groups", "IgnorePrerequisites" }) { var field = AccessTools.Field(pool.GetType(), name); if (field != null) field.SetValue(pool, PsychicExpansionTests.Read(row["blueprint"]["Data"][name], field.FieldType)); }
            }
        }
        var blood = Native<BlueprintCharacterClass>(cache, "d77e67a814d686842802c9cfd8ef8499");
        var primal = Native<BlueprintArchetype>(cache, "4d588290ed0736e4f8cd3f4c3beacd69");
        // The common reader omits class membership and the runtime parent cache.
        // Populate only those absent fixture fields using the actual native row.
        if (blood.Archetypes.Count() == 0) {
            var row = PsychicExpansionTests.NativeRows.First(r => (string)r["blueprint"]["AssetId"] == blood.AssetGuid.ToString());
            Set(blood, "m_Archetypes", PsychicExpansionTests.Read(row["blueprint"]["Data"]["m_Archetypes"], typeof(BlueprintArchetypeReference[])));
        }
        if (Get(primal, "m_ParentClass") == null) Set(primal, "m_ParentClass", blood);
    }
    static UnitEntityData Unit(BlueprintsCache c, string name, string classId = "f7d7eb166b3dd594fb330d085df41853", int level = 8) {
        var u = (UnitEntityData)Call(typeof(FlameJetLifecycle01430Tests), "Unit", c, "034-super-" + name);
        Set(u, "UniqueId", "034-super-unit-" + ++serial);
        var proxy = FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy", BindingFlags.Public | BindingFlags.NonPublic)); Set(proxy, "Entity", u); Set(u, "Proxy", proxy);
        Set(u, "Abilities", u.Facts.EnsureFactProcessor<AbilityCollection>());
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c, classId)) { Level = level });
        u.Stats.Constitution.BaseValue = u.Stats.Dexterity.BaseValue = u.Stats.Wisdom.BaseValue = 10;
        u.Stats.SaveWill.BaseValue = u.Stats.SaveFortitude.BaseValue = u.Stats.SaveReflex.BaseValue = 0;
        units.Add(u); return u;
    }
    static Feature Feature(UnitEntityData owner, BlueprintFeature bp) { var f = new Feature(bp, owner.Descriptor); owner.Facts.Add(f); Check(f.IsTurnedOn && f.IsSubscribedOnEventBus, "native feature lifecycle subscribes " + bp.name); return f; }
    static Buff Buff(UnitEntityData target, BlueprintBuff bp, UnitEntityData caster) {
        var b = new Buff(bp, new MechanicsContext(caster, target.Descriptor, bp), null); target.Facts.Add(b);
        Check(b.IsTurnedOn && b.IsActive && b.IsSubscribedOnEventBus, "native manager activates and subscribes " + bp.name); return b;
    }
    static bool CanSelect(UnitEntityData u, BlueprintFeatureSelection pool, BlueprintFeature feat) { selections++; return (bool)Call(typeof(EnhancementLifecycle01430Tests), "CanSelect", u, pool, feat); }
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection pool, string field) => (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
    static void Registration(BlueprintsCache cache, Harmony h) {
        PrepareNative(cache); Call(Module, "Register", cache);
        var roots = RootTokens.Select(t => Asset<BlueprintFeature>(cache, t)).ToArray();
        foreach (var root in roots) {
            Check(root.IsClassFeature && root.HasGroup(FeatureGroup.RagePower) && !root.HasGroup(FeatureGroup.Feat), "only native rage-power entry " + root.name);
            foreach (var p in PoolIds.Select(id => Native<BlueprintFeatureSelection>(cache, id))) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Check(Refs(p, field).Count(r => r.Guid == root.AssetGuid) == 1, "one root in " + p.name + "/" + field);
        }
        foreach (var id in HostIds) {
            var host = Native<BlueprintBuff>(cache, id);
            foreach (string token in RootTokens) {
                var child = host.GetComponents<AddFactContextActions>().SelectMany(a => a.Activated.Actions).Where(a => a.name == "01434_" + token).Cast<Conditional>().ToArray();
                Check(child.Length == 1 && child[0].IfTrue.Actions.Single() is ContextActionApplyBuff a && a.Permanent && a.AsChild && !a.ToCaster, "one parent-owned permanent child action " + host.name + "/" + token);
            }
        }
        var identities = roots.Select(r => r).ToArray(); Call(Module, "Register", cache);
        Check(roots.Select((r, i) => ReferenceEquals(r, Asset<BlueprintFeature>(cache, RootTokens[i]))).All(x => x), "repeat registration preserves all own identities");
        foreach (string cls in new[] { "f7d7eb166b3dd594fb330d085df41853", "6afa347d804838b48bda16acb0573dc0", "d77e67a814d686842802c9cfd8ef8499", "48ac8db94d5de7645906c7d0ad3bcfbd" }) {
            var u = Unit(cache, "choice-" + cls, cls, 1); bool valid = cls == "f7d7eb166b3dd594fb330d085df41853" || cls == "6afa347d804838b48bda16acb0573dc0";
            var pool = Native<BlueprintFeatureSelection>(cache, PoolIds[0]);
            Check(CanSelect(u, pool, roots[0]) == valid, "native choice class boundary " + cls);
            if (cls == "d77e67a814d686842802c9cfd8ef8499") { u.Progression.Classes[0].Archetypes.Add(Native<BlueprintArchetype>(cache, "4d588290ed0736e4f8cd3f4c3beacd69")); valid = true; Check(CanSelect(u, pool, roots[0]), "native Primalist-specific class path"); }
            if (!valid) continue;
            Check(!CanSelect(u, pool, roots[1]), "Witch Hunter requires actual owned Superstition");
            var super = Feature(u, roots[0]); Check(!CanSelect(u, pool, roots[0]) && CanSelect(u, pool, roots[1]), "native ownership enables dependent option and excludes repeat Superstition");
            var witch = Feature(u, roots[1]); Check(!CanSelect(u, pool, roots[1]), "no duplicate Witch Hunter"); u.Facts.Remove(witch); u.Facts.Remove(super);
        }
        var ids = new HashSet<BlueprintGuid>(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>());
        foreach (string list in new[] { "WillNegates", "FortNegates", "HealingHalved" }) {
            var rows = (string[])AccessTools.Field(Module, list).GetValue(null);
            Check(rows.Length == (list == "WillNegates" ? 98 : list == "FortNegates" ? 36 : 20), "complete original harmless list rows " + list);
            foreach (string id in rows) Check(ids.Contains(BlueprintGuid.Parse(id)), "actual native harmless GUID retained " + id);
        }
    }
    static AbilityExecutionContext Cast(BlueprintAbility spell, UnitEntityData caster, UnitEntityData target, int dc = 16) => new AbilityExecutionContext(new AbilityData(spell, caster.Descriptor), new AbilityParams { CasterLevel = 8, DC = dc }, new TargetWrapper(target), null, caster);
    sealed class ApplyCount : GameAction {
        public override string GetCaption() => "01434 observed effect endpoint";
        public override void RunAction() { applications++; }
    }
    static BlueprintAbility Ability(BlueprintsCache cache, string id, AbilityType type = AbilityType.Spell) {
        var original = Native<BlueprintAbility>(cache, id);
        var bp = (BlueprintAbility)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null);
        bp.Type = type; bp.ComponentsArray = new BlueprintComponent[] { new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { new ApplyCount() } } } }; bp.OnEnable(); return bp;
    }
    static void Harmless(BlueprintsCache cache) {
        var caster = Unit(cache, "caster"); var target = Unit(cache, "target"); var other = Unit(cache, "other");
        var defense = Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"); var active = Buff(target, defense, target);
        Buff(other, defense, other); Buff(target, defense, caster);
        foreach (var row in new[] { new { Id = "9e1ad5d6f87d19e4d8883d63a6e35568", Save = SavingThrowType.Will }, new { Id = "486eaff58293f6441a5c2759c4872f98", Save = SavingThrowType.Fortitude } }) {
            var spell = Ability(cache, row.Id); var effect = spell.GetComponent<AbilityEffectRunAction>();
            foreach (int roll in new[] { 1, 20 }) {
                d20 = roll; var ctx = Cast(spell, caster, target); int before = saves, did = applications;
                effect.Apply(ctx, target); Check(saves == before + 1 && recordedSaves.Last().Type == row.Save && applications == did + (roll == 1 ? 1 : 0), "native Apply harmless pass/fail and actual save type " + row.Id + "/" + roll);
                effect.Apply(ctx, target); Check(saves == before + 1 && applications == did + (roll == 1 ? 2 : 0), "same cast-target does not reroll with overlapping markers or repeated effects");
                effect.Apply(ctx, other); Check(saves == before + 2, "same cast independent recipient rolls once");
            }
            foreach (var type in new[] { AbilityType.SpellLike, AbilityType.Supernatural, AbilityType.Extraordinary }) {
                var nonSpell = Ability(cache, row.Id, type); int before = saves; nonSpell.GetComponent<AbilityEffectRunAction>().Apply(Cast(nonSpell, caster, target), target); Check(saves == before, "non-spell harmless source keeps native behavior " + type);
            }
            int previous = saves;
            effect.Apply(Cast(spell, caster, caster), caster); Check(saves == previous, "self source never forced to resist");
            ally = false; effect.Apply(Cast(spell, caster, target), target); Check(saves == previous, "enemy source keeps native save rules"); ally = true;
            var outsider = Unit(cache, "not-raging"); Feature(outsider, Asset<BlueprintFeature>(cache, RootTokens[0])); effect.Apply(Cast(spell, caster, outsider), outsider); Check(saves == previous, "owning feat without rage effect does not force harmless save");
            var builtIn = Ability(cache, row.Id); builtIn.GetComponent<AbilityEffectRunAction>().SavingThrowType = row.Save; d20 = 20;
            builtIn.GetComponent<AbilityEffectRunAction>().Apply(Cast(builtIn, caster, target), target); Check(saves == previous + 1, "actual native outer saving throw is not doubled");
        }
        var unknown = Ability(cache, "0a5ddfbcfb3989543ac7c936fc256889"); unknown.AssetGuid = BlueprintGuid.Parse("8568ea6bd3bf44d390c032b8758a0034"); int s0 = saves;
        unknown.GetComponent<AbilityEffectRunAction>().Apply(Cast(unknown, caster, target), target); Check(saves == s0, "unlisted spell never receives an invented saving throw");
        var ownedMarkers = target.Buffs.Enumerable.Where(b => b.Blueprint.GetComponent<SuperstitionDefense01434>() != null).ToArray();
        var known = Ability(cache, "9e1ad5d6f87d19e4d8883d63a6e35568"); int offBefore = saves;
        foreach (var marker in ownedMarkers) marker.TurnOff();
        Check(ownedMarkers.All(b => b.IsActive && !b.IsTurnedOn), "native direct TurnOff retains active fact but disables runtime components");
        known.GetComponent<AbilityEffectRunAction>().Apply(Cast(known, caster, target), target); Check(saves == offBefore, "all direct-off markers cannot force harmless save");
        foreach (var marker in ownedMarkers) marker.TurnOn();
        known.GetComponent<AbilityEffectRunAction>().Apply(Cast(known, caster, target), target); Check(saves == offBefore + 1, "native direct TurnOn restores harmless resistance once");
        target.Facts.TurnOff(); int managerBefore = saves;
        known.GetComponent<AbilityEffectRunAction>().Apply(Cast(known, caster, target), target); Check(saves == managerBefore, "native manager TurnOff disables all harmless resistance");
        target.Facts.TurnOn(); known.GetComponent<AbilityEffectRunAction>().Apply(Cast(known, caster, target), target); Check(saves == managerBefore + 1, "native manager TurnOn restores harmless resistance once");
        active.Manager.Remove(active); Check(active.IsDisposed, "native fact removal disposes specific overlapping source only");
    }
    static RuleHealDamage Heal(MechanicsContext context, UnitEntityData target, int amount = 40, float resistance = 0, bool fake = false, bool interrupted = false, bool empowered = false) {
        var rule = new RuleHealDamage(context.MaybeCaster, target, DiceFormula.Zero, amount) { Reason = context, IsFake = fake, Interrupted = interrupted };
        rule.HealResistance.Set(resistance, (EntityFact)null); if (empowered) rule.Empower.Set(true, (EntityFact)null);
        target.Damage = 90; NativeRule(rule); return rule;
    }
    static void Healing(BlueprintsCache cache) {
        var caster = Unit(cache, "heal-caster"); var target = Unit(cache, "heal-target"); var other = Unit(cache, "heal-other");
        Buff(target, Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"), target); Buff(target, Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"), caster); Buff(other, Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"), other);
        var cure = Ability(cache, "47808d23c67033d4bbab86a1070fd62f");
        foreach (int roll in new[] { 1, 20 }) {
            d20 = roll; var ctx = Cast(cure, caster, target); int before = saves;
            var r = Heal(ctx, target); Check(r.Value == (roll == 20 ? 20 : 40) && target.Damage == (roll == 20 ? 70 : 50) && saves == before + 1, "actual native healing pass/fail and HP math " + roll + ": Value=" + r.Value + ", Damage=" + target.Damage + ", saves=" + (saves - before) + ", interrupted=" + r.Interrupted + ", fake=" + r.IsFake + ", resistance=" + (float)r.HealResistance);
            var r2 = Heal(ctx, target); Check(r2.Value == r.Value && saves == before + 1, "same cast repeated heal shares save without double half");
            Heal(ctx, other); Check(saves == before + 2, "same healing cast rolls independently for another recipient");
        }
        d20 = 20;
        foreach (float resistance in new[] { 0f, .25f, .5f, 1f }) foreach (bool empower in new[] { false, true }) {
            var ctx = Cast(cure, caster, target); var r = Heal(ctx, target, 40, resistance, empowered: empower);
            Check(r.Value == (int)(40 * (empower ? 1.5f : 1f) * (1 - resistance) * .5f), "native final healing composes original resistance and Empower " + resistance + "/" + empower);
        }
        foreach (bool interrupted in new[] { false, true }) { int before = saves; var r = Heal(Cast(cure, caster, target), target, fake: !interrupted, interrupted: interrupted); Check(saves == before, "fake or interrupted healing does not roll or alter resistance"); }
        int prior = saves; Heal(Cast(cure, caster, caster), caster); Check(saves == prior, "self heal unaffected");
        ally = false; Heal(Cast(cure, caster, target), target); Check(saves == prior, "enemy heal unaffected"); ally = true;
        var sla = Ability(cache, cure.AssetGuid.ToString(), AbilityType.SpellLike); Heal(Cast(sla, caster, target), target); Check(saves == prior, "healing SLA stays outside original harmless-spell-only rule");
        var unlisted = Ability(cache, cure.AssetGuid.ToString()); unlisted.AssetGuid = BlueprintGuid.Parse("8568ea6bd3bf44d390c032b8758a0034"); Heal(Cast(unlisted, caster, target), target); Check(saves == prior, "unlisted healing unaffected");
    }
    static void StickyHealing(BlueprintsCache cache) {
        var wrapper = Native<BlueprintAbility>(cache, "5590652e1c2225c4ca30c4a699ab3649");
        var delivery = Native<BlueprintAbility>(cache, "47808d23c67033d4bbab86a1070fd62f");
        var sticky = wrapper.GetComponent<AbilityEffectStickyTouch>();
        Check(sticky != null && sticky.TouchDeliveryAbility == delivery && delivery.GetComponent<AbilityDeliverTouch>() != null, "actual native Cure wrapper points to original whitelist delivery with native touch component");
        var healAction = ((Conditional)delivery.GetComponent<AbilityEffectRunAction>().Actions.Actions[0]).IfTrue.Actions.OfType<ContextActionHealTarget>().Single();
        foreach (int roll in new[] { 1, 20 }) {
            var caster = Unit(cache, "sticky-caster-" + roll); var target = Unit(cache, "sticky-target-" + roll);
            Buff(target, Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"), target);
            var cast = Cast(wrapper, caster, target, 23); d20 = roll;
            try {
                sticky.Apply(cast, target);
                var part = caster.Get<UnitPartTouch>(); var charged = part?.Ability;
                Check(charged != null && charged.IsTurnedOn && ReferenceEquals(charged.Data.Blueprint, delivery) && ReferenceEquals(charged.StickyTouch, cast.Ability), "native sticky Apply and UnitPartTouch.Init create charged delivery with original cast identity");
                Check(charged.OverrideParams.DC == cast.Params.DC && charged.OverrideParams.CasterLevel == cast.Params.CasterLevel, "native sticky charge retains source DC and caster level");
                var context = new AbilityExecutionContext(charged.Data, charged.OverrideParams, target, null, caster);
                Check(ReferenceEquals(context.SourceAbility, delivery) && ReferenceEquals(context.SourceAbilityContext, context), "native delivery execution context uses exact delivery, not wrapper or arbitrary blueprint parent");
                int full = healAction.Value.BonusValue.Calculate(context); int before = saves, healed = heals; target.Damage = 90;
                delivery.GetComponent<AbilityEffectRunAction>().Apply(context, target);
                int expected = roll == 20 ? full / 2 : full;
                Check(full > 0 && saves == before + 1 && heals == healed + 1 && target.Damage == 90 - expected && recordedSaves.Last().DifficultyClassBase == 23, "native Cure payload conditions and HealTarget.RunAction retain delivery identity, source DC and pass/fail HP math " + roll + "/" + full + "/" + target.Damage);
                var unrelated = Ability(cache, delivery.AssetGuid.ToString()); unrelated.AssetGuid = BlueprintGuid.Parse("8568ea6bd3bf44d390c032b8758a0034"); Set(unrelated, "m_Parent", Ref<BlueprintAbilityReference>(delivery));
                before = saves; var separate = Heal(Cast(unrelated, caster, target), target);
                Check(ReferenceEquals(unrelated.Parent, delivery) && saves == before && separate.Value == 40, "unlisted ability cannot borrow listed BlueprintAbility.Parent healing eligibility");
                var nested = new MechanicsContext(caster, target.Descriptor, unrelated, context);
                Check(ReferenceEquals(nested.SourceAbility, unrelated), "native nested ability source overrides parent mechanics context");
                Heal(nested, target); Check(saves == before, "unlisted nested ability cannot borrow listed ancestor mechanics context eligibility");
                Heal(cast, target); Check(saves == before, "sticky cast wrapper itself is outside exact delivery whitelist");
            } finally { caster.Remove<UnitPartTouch>(); }
        }
    }
    static void Bonus(BlueprintsCache cache) {
        foreach (int level in new[] { 1, 3, 4, 7, 8, 12, 20 }) {
            var owner = Unit(cache, "save-level-" + level, level: level); var caster = Unit(cache, "hostile-level-" + level); var b = Buff(owner, Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"), owner);
            d20 = 10;
            foreach (AbilityType type in new[] { AbilityType.Spell, AbilityType.SpellLike, AbilityType.Supernatural, AbilityType.Extraordinary }) {
                var spell = Ability(cache, "9e1ad5d6f87d19e4d8883d63a6e35568", type); var ctx = Cast(spell, caster, owner);
                foreach (SavingThrowType save in new[] { SavingThrowType.Fortitude, SavingThrowType.Reflex, SavingThrowType.Will }) {
                    var rule = NativeRule(new RuleSavingThrow(owner, save, 100) { Reason = ctx }); int expected = type == AbilityType.Spell || type == AbilityType.SpellLike ? 2 + level / 4 : 0;
                    Check(rule.RollResult == 10 + expected, "native save type/classification bonus " + level + "/" + type + "/" + save + " actual " + rule.RollResult);
                }
            }
            owner.Facts.Remove(b);
        }
        var shared = Unit(cache, "save-overlap", level: 4); var singer = Unit(cache, "save-singer", "6afa347d804838b48bda16acb0573dc0", 20); var enemy = Unit(cache, "save-overlap-enemy");
        Buff(shared, Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"), shared); var stronger = Buff(shared, Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff"), singer);
        var context = Cast(Ability(cache, "9e1ad5d6f87d19e4d8883d63a6e35568"), enemy, shared);
        d20 = 10; Check(NativeRule(new RuleSavingThrow(shared, SavingThrowType.Will, 100) { Reason = context }).RollResult == 17, "native competence stacking selects stronger level20 source, not sum");
        var external = shared.Stats.SaveWill.AddModifier(9, stronger, ModifierDescriptor.Competence);
        Check(NativeRule(new RuleSavingThrow(shared, SavingThrowType.Will, 100) { Reason = context }).RollResult == 19, "stronger existing competence bonus remains authoritative"); external.Remove();
        shared.Facts.Remove(stronger); Check(NativeRule(new RuleSavingThrow(shared, SavingThrowType.Will, 100) { Reason = context }).RollResult == 13, "native stronger-source removal restores level4 competence source");
    }
    static void HostLifecycle(BlueprintsCache cache) {
        for (int mode = 0; mode < HostIds.Length; mode++) {
            var source = Unit(cache, "host-source-" + mode, mode >= 3 ? "6afa347d804838b48bda16acb0573dc0" : "f7d7eb166b3dd594fb330d085df41853", 20);
            var receiver = Unit(cache, "host-recipient-" + mode, level: 1);
            var nativeHost = Native<BlueprintBuff>(cache, HostIds[mode]);
            // Exact production append from the real native host, executed by a
            // native AddFactContextActions delegate and parent-child lifecycle.
            // Unrelated rage tier/resources/FX remain outside this wrapper.
            var actions = nativeHost.GetComponents<AddFactContextActions>().SelectMany(a => a.Activated?.Actions ?? Array.Empty<GameAction>()).Where(a => RootTokens.Any(t => a.name == "01434_" + t)).ToArray();
            Check(actions.Length == 2, "real host owns both newly authored child actions " + mode);
            var wrapper = new BlueprintBuff { name = "034-native-host-actions-" + mode, AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), Stacking = StackingType.Stack,
                ComponentsArray = new BlueprintComponent[] { new AddFactContextActions { Activated = new ActionList { Actions = actions }, Deactivated = new ActionList { Actions = Array.Empty<GameAction>() }, NewRound = new ActionList { Actions = Array.Empty<GameAction>() }, Dispose = new ActionList { Actions = Array.Empty<GameAction>() } } } }; wrapper.OnEnable();
            var empty = Buff(receiver, wrapper, source);
            Check(!receiver.Buffs.Enumerable.Any(b => b.Blueprint == Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff")), "actual native child action rejects missing power " + mode); receiver.Facts.Remove(empty);
            var actualOwner = mode >= 3 ? source : receiver;
            Feature(actualOwner, Asset<BlueprintFeature>(cache, RootTokens[0])); Feature(actualOwner, Asset<BlueprintFeature>(cache, RootTokens[1]));
            var parent = Buff(receiver, wrapper, source);
            var children = receiver.Buffs.Enumerable.Where(b => b != parent && b.IsActive && (b.Blueprint == Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff") || b.Blueprint == Asset<BlueprintBuff>(cache, "WitchHunterRageEffectBuff"))).ToArray();
            Check(children.Length == 2 && children.All(b => b.Context.MaybeCaster == source), "native host actions apply two effects preserving actual source caster " + mode);
            Check(children.Single(b => b.Blueprint.GetComponent<SuperstitionDefense01434>() != null).Blueprint.GetComponent<ContextRankConfig>().GetValue(children.Single(b => b.Blueprint.GetComponent<SuperstitionDefense01434>() != null).Context) == 7, "shared effect scales with level20 rage source rather than level1 recipient " + mode);
            receiver.Facts.Remove(parent); Check(children.All(b => b.IsDisposed), "native parent removal clears both exact child effects " + mode);
            var wrongOwner = mode >= 3 ? receiver : source;
            var old = actualOwner.Progression.Features.Enumerable.Where(f => RootTokens.Any(t => f.Blueprint.AssetGuid == Id(t))).ToArray(); foreach (var f in old) actualOwner.Facts.Remove(f);
            Feature(wrongOwner, Asset<BlueprintFeature>(cache, RootTokens[0]));
            var wrong = Buff(receiver, wrapper, source); Check(!receiver.Buffs.Enumerable.Any(b => b.Blueprint == Asset<BlueprintBuff>(cache, "SuperstitionRageEffectBuff")), "native gate distinguishes caster-owned Skald and recipient-owned personal rage " + mode); receiver.Facts.Remove(wrong);
        }
    }
    static int DamageBonus(UnitEntityData owner, UnitEntityData target) {
        var first = new PhysicalDamage(new ModifiableDiceFormula(DiceFormula.Zero), 10, PhysicalDamageForm.Slashing);
        var second = new PhysicalDamage(new ModifiableDiceFormula(DiceFormula.Zero), 10, PhysicalDamageForm.Piercing);
        var calculate = new RuleCalculateDamage(new RuleDealDamage(owner, target, new DamageBundle(first, second)));
        var manager = new RulebookSubscriptionManager<IInitiatorRulebookSubscriber>();
        foreach (var fact in owner.Facts.List.Where(f => f.IsActive && f.IsTurnedOn && !f.IsDisposed && (!(f is Buff b) || !b.IsSuppressed))) foreach (var r in fact.Components.Where(r => r.SourceBlueprintComponent is WitchHunterDamage01434)) manager.Subscribe((IInitiatorRulebookSubscriber)r.SourceBlueprintComponent, (ISubscriptionProxy)r);
        manager.OnEventAboutToTrigger(calculate); manager.OnEventAboutToTrigger(calculate);
        Check(!second.Modifiers.Any(), "Witch Hunter never adds bonus to subsequent damage buckets"); return first.Modifiers.Sum(m => m.Value);
    }
    static void WitchHunter(BlueprintsCache cache) {
        var owner = Unit(cache, "witch-owner", level: 8); var caster = Unit(cache, "witch-stronger", level: 20); var target = Unit(cache, "mundane");
        var b = Buff(owner, Asset<BlueprintBuff>(cache, "WitchHunterRageEffectBuff"), owner);
        Check(DamageBonus(owner, target) == 0, "ordinary target does not qualify");
        foreach (AbilityType type in new[] { AbilityType.Spell, AbilityType.SpellLike, AbilityType.Supernatural, AbilityType.Extraordinary }) {
            var ability = Ability(cache, "9e1ad5d6f87d19e4d8883d63a6e35568", type); var f = new Ability(ability, target.Descriptor); target.Facts.Add(f);
            Check(DamageBonus(owner, target) == (type == AbilityType.Spell || type == AbilityType.SpellLike ? 3 : 0), "native AbilityCollection source classification " + type); target.Facts.Remove(f);
        }
        var spell = new Ability(Ability(cache, "9e1ad5d6f87d19e4d8883d63a6e35568"), target.Descriptor); target.Facts.Add(spell);
        var strong = Buff(owner, Asset<BlueprintBuff>(cache, "WitchHunterRageEffectBuff"), caster);
        Check(DamageBonus(owner, target) == 6, "overlapping sources pick stronger source rank once");
        strong.TurnOff(); Check(strong.IsActive && !strong.IsTurnedOn && DamageBonus(owner, target) == 3, "native stronger-source direct TurnOff restores active weaker source");
        strong.TurnOn(); Check(DamageBonus(owner, target) == 6, "native stronger-source direct TurnOn restores strongest source once");
        owner.Facts.TurnOff(); Check(DamageBonus(owner, target) == 0, "native manager TurnOff disables all damage sources"); owner.Facts.TurnOn(); Check(DamageBonus(owner, target) == 6, "native manager TurnOn restores strongest damage source once");
        owner.Facts.Remove(strong); Check(DamageBonus(owner, target) == 3, "native stronger-source removal restores weaker effect");
        owner.Facts.Remove(b); Check(DamageBonus(owner, target) == 0, "native effect removal clears damage rider");
        foreach (bool alchemist in new[] { false, true }) {
            var book = new BlueprintSpellbook { name = "034-book", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = alchemist ? new BlueprintComponent[] { new IsAlchemistSpellbook() } : Array.Empty<BlueprintComponent>() }; book.OnEnable();
            var cls = new BlueprintCharacterClass { name = "034-caster-class", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")) }; Set(cls, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));
            var bookUser = Unit(cache, "book-class"); bookUser.Progression.Classes.Clear(); bookUser.Progression.Classes.Add(new ClassData(cls) { Level = 1 });
            Check((bool)Call(typeof(WitchHunterDamage01434), "SpellUser", bookUser) == !alchemist, "native class spellbook metadata and extract-only boundary " + alchemist);
        }
    }
    internal static int Run(BlueprintsCache cache) {
        checks = serial = saves = heals = applications = additions = removals = selections = 0; current = null; ally = true; units.Clear(); recordedSaves.Clear();
        var h = new Harmony("CotW01434.SuperstitionNative"); var observer = new Events(); var game = Game.Instance;
        var oldTime = Get(game, "TimeController"); var oldState = game.State;
        var capture = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense"); var controlled = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice");
        var healingCapture = AccessTools.Method(typeof(Psychic070Tests), "CaptureHealing"); var healingAmount = AccessTools.Field(typeof(Psychic070Tests), "healAmount"); var healingBefore = healingAmount.GetValue(null);
        var buffHelper = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var diceMethod = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) });
        var queue = (ICollection)AccessTools.Field(typeof(Psychic070Tests), "dice").GetValue(null); var queueBefore = queue.Cast<int>().ToArray();
        var suspended = new[] { buffHelper, diceMethod, AccessTools.Method(typeof(RuleHealDamage), "OnTrigger") }.SelectMany(m => (Harmony.GetPatchInfo(m)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<HarmonyLib.Patch>()).Where(p => p.PatchMethod == capture || p.PatchMethod == controlled || p.PatchMethod == healingCapture).Select(p => new { Method = m, Patch = p })).ToArray();
        JObject contract = null;
        void Patch(MethodBase m, string n) => h.Patch(m, prefix: new HarmonyMethod(typeof(SuperstitionRage01434Tests), n));
        try {
            foreach (var p in suspended) h.Unpatch(p.Method, p.Patch.PatchMethod);
            Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip)); Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No));
            Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController));
            Patch(AccessTools.Method(typeof(UnitEntityData), "IsAlly", new[] { typeof(UnitEntityData) }), nameof(Allied));
            Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(No));
            h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"), prefix: new HarmonyMethod(typeof(SuperstitionRage01434Tests), nameof(No)) { priority = Priority.First });
            // Constructed neutral units have no scene hostility/green-NPC AI.
            Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), "IsAttackingGreenNPC"), nameof(No));
            Patch(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), nameof(Skip)); Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(Identity));
            Patch(AccessTools.Method(AccessTools.Property(typeof(UnitEntityData), "CombatState").PropertyType, "ReturnToStartingPositionIfNeeded"), nameof(Skip));
            Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), nameof(Context)); Patch(diceMethod, nameof(Dice)); Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), nameof(D20));
            Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(No));
            Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(Expose));
            Provider01419Tests.PatchLabels(h);
            var timeField = AccessTools.Field(typeof(Game), "TimeController"); Set(game, "TimeController", FormatterServices.GetUninitializedObject(timeField.FieldType)); Patch(AccessTools.PropertyGetter(timeField.FieldType, "GameTime"), nameof(Clock));
            var player = Empty<Player>(); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); player.GameTime = TimeSpan.FromHours(1);
            var state = Empty<PersistentState>(); state.PlayerState = player; Set(game, "State", state);
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(KineticUtility01433Tests), "BuffDifficultyBoundary"));
            foreach (var m in new[] { AccessTools.Method(Module, "Passed"), AccessTools.Method(typeof(AbilityEffectRunAction), "Apply"), AccessTools.Method(typeof(ContextActionSavingThrow), "RunAction"), AccessTools.Method(typeof(ContextActionHealTarget), "RunAction") }) h.Patch(m, transpiler: new HarmonyMethod(typeof(SuperstitionRage01434Tests), nameof(RuleBoundary)));
            h.Patch(AccessTools.Method(typeof(AbilityEffectStickyTouch), "Apply"), transpiler: new HarmonyMethod(typeof(SuperstitionRage01434Tests), nameof(StickyQueueBoundary)));
            var applyPatch = Module.GetNestedType("HarmlessApplyPatch", BindingFlags.NonPublic); var healPatch = Module.GetNestedType("HarmlessHealPatch", BindingFlags.NonPublic);
            h.Patch(AccessTools.Method(typeof(AbilityEffectRunAction), "Apply"), prefix: new HarmonyMethod(applyPatch, "Prefix")); h.Patch(AccessTools.Method(typeof(RuleHealDamage), "OnTrigger"), prefix: new HarmonyMethod(healPatch, "Prefix"));
            EventBus.Subscribe(observer); Registration(cache, h); Bonus(cache); HostLifecycle(cache); Harmless(cache); Healing(cache); StickyHealing(cache); WitchHunter(cache);
            var newAssets = RootTokens.Concat(new[] { "SuperstitionRageEffectBuff", "WitchHunterRageEffectBuff" }).Select(t => cache.Load(Id(t))).ToArray();
            PsychicExpansionTests.Inventory["superstitionRage01434Contracts"] = contract = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 2, ["ragePowerCount"] = 2, ["ordinaryFeatCount"] = 0, ["newSpellRootCount"] = 0,
                ["itemIds"] = new JArray("COTW-0262", "COTW-0264"), ["newBlueprints"] = new JArray(newAssets.Select(b => new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name, ["type"] = b.GetType().FullName })),
                ["originalWillRows"] = 98, ["originalFortRows"] = 36, ["originalHealingRows"] = 20, ["nativeSavingThrows"] = saves, ["nativeHealingRules"] = heals,
                ["nativeFactAddedEvents"] = additions, ["nativeFactRemovedEvents"] = removals, ["nativeSelections"] = selections,
                ["nativeApplyPassFailAndExactLists"] = true, ["multiTargetAndOverlapSaveDeduplication"] = true, ["nativeOuterSaveNotDoubled"] = true,
                ["nativeFinalHealingMath"] = true, ["fakeInterruptedAndSourceBoundaries"] = true, ["nativeCompetenceBonusAndClassScaling"] = true,
                ["nativeAppendedHostActionLifecycle"] = true, ["nativeCasterAndReceiverOwnershipGates"] = true,
                ["nativeDamageSubscriptionProxy"] = true, ["nativeAbilityCollectionAndClassBookBoundaries"] = true,
                ["nativeDirectAndManagerTurnOffOn"] = true, ["strongSourceTurnOffRestoresWeaker"] = true,
                ["nativeStickyTouchChargeAndDeliveryHealing"] = true, ["unrelatedAbilityParentsCannotBorrowHarmlessEligibility"] = true,
                ["boundary"] = "Native selection/prerequisites, fact-manager activation/removal and EventBus, native AbilityEffectRunAction.Apply and real RuleSavingThrow/RuleHealDamage.OnTrigger run. Native Cure StickyTouch.Apply runs through UnitPartTouch.Init and produces real charged delivery AbilityData; scene auto-cast command scheduling is the controlled endpoint. Charged AbilityData constructs the native delivery context, whose actual Cure payload conditions and HealTarget.RunAction execute to real HP changes. Touch hit/commands/animation are outside this fixture. Rulebook entry dispatch uses native subscription managers and attached native runtime proxies; raw dice and unit/scene setup are controlled. Other harmless Apply payloads count accepted effects. Damage proxy tests inspect actual native BaseDamage modifiers; complete damage OnTrigger/HP, UI, animations and full scene saves remain game regression steps. No game process was started." };
            Console.WriteLine("PASS: " + checks + " SuperstitionRage01434 checks."); return checks;
        } finally {
            foreach (var u in units) u.Facts.TurnOff(); EventBus.Unsubscribe(observer); h.UnpatchAll(h.Id);
            foreach (var saved in suspended) { var p = saved.Patch; new Harmony(p.owner).Patch(saved.Method, prefix: new HarmonyMethod(p.PatchMethod) { priority = p.priority, before = p.before, after = p.after, debug = p.debug }); }
            current = null; Set(game, "State", oldState); Set(game, "TimeController", oldTime);
            if (!queue.Cast<int>().SequenceEqual(queueBefore)) throw new Exception("01434 outer dice queue changed");
            if (!Equals(healingBefore, healingAmount.GetValue(null))) throw new Exception("01434 outer captured-healing accumulator changed");
            foreach (var saved in suspended) { var p = saved.Patch; if (!(Harmony.GetPatchInfo(saved.Method)?.Prefixes.Any(n => n.owner == p.owner && n.PatchMethod == p.PatchMethod && n.priority == p.priority && n.debug == p.debug && (n.before ?? Array.Empty<string>()).SequenceEqual(p.before ?? Array.Empty<string>()) && (n.after ?? Array.Empty<string>()).SequenceEqual(p.after ?? Array.Empty<string>())) ?? false)) throw new Exception("01434 outer prefix metadata restore failed"); }
            if (contract != null) contract["outerFixtureInputsAndPatchesRestored"] = true;
        }
    }
}
