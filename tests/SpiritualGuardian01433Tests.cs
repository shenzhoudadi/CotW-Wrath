using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
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
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Controllers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// The original native first-cast DoApplyEffect, production Bind/Attack/AoO paths,
// native fact lifecycle, native rule arithmetic and subscription proxies execute.
// Generic Rulebook entry calls are redirected to native subscription managers;
// dice, world position, equipment insertion, particles and final weapon attacks
// are explicit scene boundaries. No real Unity scene/save is inferred from this.
internal static class SpiritualGuardian01433Tests
{
    const string BasicId = "247a4068296e8be42890143f451b4b45", Token = "SpiritualGuardianFeat";
    static readonly Type Module = typeof(SpiritualGuardianPenetration01433).Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpiritualGuardian01433", true);
    static readonly Type Assets = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static readonly List<RuleSpellResistanceCheck> resistanceRules = new List<RuleSpellResistanceCheck>();
    static readonly List<RuleAttackWithWeapon> attacks = new List<RuleAttackWithWeapon>();
    static RulebookSubscriptionManager<IInitiatorRulebookSubscriber> subscribers;
    static int checks, serial, nativeApplyCalls, srReplacements, initialApplyCalls, nativeFeatureAdds, nativeFeatureRemoves, selectCalls, redirectChecks, opportunityChecks;
    static bool failAppend;
    static UnitDescriptor wielder;
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Assets, "Id").Invoke(null, new object[] { token });
    static object Get(object value, string name) => (AccessTools.Field(value.GetType(), name) ?? AccessTools.Field(value.GetType(), "<" + name + ">k__BackingField")).GetValue(value);
    static void Set(object value, string name, object data) => (AccessTools.Field(value.GetType(), name) ?? AccessTools.Field(value.GetType(), "<" + name + ">k__BackingField")).SetValue(value, data);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static T Ref<T>(SimpleBlueprint value) where T : BlueprintReferenceBase, new() { var result = BlueprintReferenceBase.CreateTyped<T>(value); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(result, value, null); return result; }
    static void Check(bool value, string message) { if (!value) throw new Exception("SpiritualGuardian01433: " + message); checks++; }
    static void Register(BlueprintsCache cache) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { cache });
    static bool Rejected(Action action) { try { action(); return false; } catch (Exception error) { while (error is TargetInvocationException) error = error.InnerException; return error is InvalidOperationException; } }
    static bool Skip() => false;
    static bool False(ref bool __result) { __result = false; return false; }
    static bool True(ref bool __result) { __result = true; return false; }
    static bool NullController(ref LevelUpController __result) { __result = null; return false; }
    static bool Ignore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool DisplayName(BlueprintUnitFact __instance, ref string __result) { __result = __instance.name; return false; }
    static bool Time(ref TimeSpan __result) { __result = TimeSpan.FromSeconds(100); return false; }
    static void Identity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "guardian033-fact-" + ++serial); }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == Id(Token))) throw new InvalidOperationException("01433 guardian injected append failure"); }
    static bool NoWielder(ref UnitDescriptor __result) { __result = wielder; return false; }
    static bool Zero(ref int __result) { __result = 0; return false; }
    static bool Enemy(UnitEntityData __instance, UnitEntityData __0, ref bool __result) { __result = !ReferenceEquals(__instance, __0); return false; }
    static bool Range(ref bool __result) { __result = true; return false; }
    static bool Equip(UnitEntityData unit, BlueprintItemWeapon blueprint, ref bool __result) {
        var item = Empty<ItemEntityWeapon>(); Set(item, "m_Blueprint", blueprint); Set(item, "HoldingSlot", unit.Body.PrimaryHand);
        Set(unit.Body.PrimaryHand, "m_FakeItem", item); Set(unit.Body.PrimaryHand, "m_IsItemFake", true); __result = true; return false;
    }
    static bool Strike(RuleAttackWithWeapon __0, ref RuleAttackWithWeapon __result) { attacks.Add(__0); __result = __0; return false; }
    static bool SR(RuleSpellResistanceCheck __0, ref RuleSpellResistanceCheck __result) { __result = CompleteSR(__0); return false; }
    static RuleSpellResistanceCheck CompleteSR(RuleSpellResistanceCheck rule) {
        subscribers.OnEventAboutToTrigger(rule); rule.OnTrigger(null); subscribers.OnEventDidTrigger(rule);
        resistanceRules.Add(rule); return rule;
    }
    static RuleApplySpell CompleteApply(RuleApplySpell rule) { nativeApplyCalls++; return rule; }
    static bool FixedD20(ref RuleRollD20 __result) { __result = Empty<RuleRollD20>(); Set(__result, "m_Result", 10); return false; }
    static IEnumerable<CodeInstruction> FirstCastBoundary(IEnumerable<CodeInstruction> instructions) {
        foreach (var instruction in instructions) {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(Rulebook) && method.Name == "Trigger" && method.IsGenericMethod) {
                var rule = method.GetGenericArguments().Single();
                if (rule == typeof(RuleSpellResistanceCheck)) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(SpiritualGuardian01433Tests), nameof(CompleteSR)); srReplacements++; }
                if (rule == typeof(RuleApplySpell)) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(SpiritualGuardian01433Tests), nameof(CompleteApply)); }
            }
            yield return instruction;
        }
    }
    sealed class ApplyBoundary : AbilityApplyEffect {
        public override void Apply(AbilityExecutionContext context, TargetWrapper target) { initialApplyCalls++; }
    }
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection pool, string field) => (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
    static BlueprintsCache Fresh(BlueprintsCache source) {
        var cache = new BlueprintsCache();
        foreach (var id in ((IDictionary)Get(source, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray()) {
            if (id == Id(Token)) continue;
            var bp = source.Load(id);
            if (id.ToString() == BasicId) {
                bp = (SimpleBlueprint)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(bp, null);
                foreach (string field in new[] { "m_Features", "m_AllFeatures" })
                    Set(bp, field, Refs((BlueprintFeatureSelection)bp, field).Where(r => r.Guid != Id(Token)).Select(r => Ref<BlueprintFeatureReference>((BlueprintFeature)cache.Load(r.Guid) ?? (BlueprintFeature)source.Load(r.Guid))).ToArray());
            }
            cache.AddCachedBlueprint(id, bp);
        }
        return cache;
    }
    static void Registration(BlueprintsCache source) {
        var oldCache = ResourcesLibrary.BlueprintsCache;
        try {
            var cache = Fresh(source); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
            var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(BasicId));
            var before = new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") };
            var ids = before.Cast<BlueprintFeatureReference[]>().Select(rs => rs.Select(r => r.Guid).ToArray()).ToArray();
            var leaf = cache.Load(Id("SpiritualAllyForm4")); cache.RemoveCachedBlueprint(leaf.AssetGuid);
            Check(Rejected(() => Register(cache)) && cache.Load(Id(Token)) == null && ReferenceEquals(Get(pool, "m_AllFeatures"), before[1]), "missing guardian source leaf rejects an unusable feat before pool mutation"); cache.AddCachedBlueprint(leaf.AssetGuid, leaf);
            failAppend = true; Check(Rejected(() => Register(cache)), "actual append fault rejects registration"); failAppend = false;
            Check(cache.Load(Id(Token)) == null && ReferenceEquals(Get(pool, "m_Features"), before[0]) && ReferenceEquals(Get(pool, "m_AllFeatures"), before[1]), "failed publication restores both exact native pool arrays and removes new asset");
            Register(cache); var feat = Asset<BlueprintFeature>(cache, Token);
            Check(feat != null && feat.Groups.SequenceEqual(new[] { FeatureGroup.Feat }) && feat.Ranks == 1 && !feat.IsClassFeature, "one ordinary noncombat feat with no class system dependency");
            Check(feat.GetComponents<Prerequisite>().Count() == 1 && feat.GetComponent<PrerequisiteNoFeature>().Feature == feat, "only self exclusion, no invented Spiritualist or spellbook requirement");
            Check(feat.GetComponent<SpiritualGuardianPenetration01433>() != null && !ReferenceEquals(feat.Icon, null), "real local rule subscriber and original Spiritual Ally icon");
            foreach (int i in new[] { 0, 1 }) {
                var actual = Refs(pool, i == 0 ? "m_Features" : "m_AllFeatures");
                Check(actual.Take(ids[i].Length).Select(r => r.Guid).SequenceEqual(ids[i]) && actual.Count(r => r.Guid == feat.AssetGuid) == 1, "all original pool entries/order preserved, new root appended once");
            }
            Register(cache); Register(cache); Check(ReferenceEquals(Asset<BlueprintFeature>(cache, Token), feat), "repeat registration retains published blueprint instance");
            var previous = feat.Groups; feat.Groups = new[] { FeatureGroup.CombatFeat }; Check(Rejected(() => Register(cache)), "foreign feature metadata rejected"); feat.Groups = previous;
            var components = feat.ComponentsArray; feat.ComponentsArray = Array.Empty<BlueprintComponent>(); Check(Rejected(() => Register(cache)), "missing owned rule component rejected without replacement"); feat.ComponentsArray = components;
            cache.RemoveCachedBlueprint(feat.AssetGuid); var foreign = new BlueprintBuff { AssetGuid = feat.AssetGuid, name = feat.name, ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(foreign.AssetGuid, foreign);
            Check(Rejected(() => Register(cache)) && ReferenceEquals(cache.Load(feat.AssetGuid), foreign), "wrong type same GUID is preserved and rejected");
        } finally { failAppend = false; AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, oldCache); }
        Register(source);
    }
    static UnitEntityData Unit(BlueprintsCache cache, string name) {
        var unit = (UnitEntityData)AccessTools.Method(typeof(SpellAndSword01417Tests), "Unit").Invoke(null, new object[] { cache, "guardian033-" + name });
        Set(unit.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(unit.Descriptor));
        Set(unit, "Buffs", unit.Facts.EnsureFactProcessor<BuffCollection>()); Set(unit.Progression, "Features", unit.Facts.EnsureFactProcessor<FeatureCollection>());
        foreach (var field in unit.State.Features.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.FieldType == typeof(CountableFlag) && field.GetValue(unit.State.Features) == null) field.SetValue(unit.State.Features, new CountableFlag());
        unit.Facts.TurnOn(); units.Add(unit); return unit;
    }
    static Feature Add(UnitEntityData unit, BlueprintFeature feat) {
        var fact = (Feature)feat.CreateFact(null, unit.Descriptor, null); var actual = unit.Facts.Add(fact); nativeFeatureAdds++;
        Check(ReferenceEquals(actual, fact) && fact.IsActive && fact.IsTurnedOn, "native CreateFact/Add activates feat on exact owner"); return fact;
    }
    static void Remove(Feature feature) { var manager = feature.Manager; manager.Remove(feature); nativeFeatureRemoves++; Check(feature.IsDisposed && feature.Manager == null, "native Remove detaches feature"); }
    static void Subscribe(params Feature[] features) {
        subscribers = new RulebookSubscriptionManager<IInitiatorRulebookSubscriber>();
        foreach (var feature in features) {
            var runtime = feature.Components.Single(c => c.SourceBlueprintComponent is SpiritualGuardianPenetration01433);
            subscribers.Subscribe((IInitiatorRulebookSubscriber)runtime.SourceBlueprintComponent, (ISubscriptionProxy)runtime);
        }
    }
    static MechanicsContext Context(UnitEntityData caster, UnitEntityData target, BlueprintScriptableObject source, int level = 16) {
        var context = new MechanicsContext(caster, target.Descriptor, source, null, new TargetWrapper(target)); context.SetParams(new AbilityParams { CasterLevel = level, DC = 22 }); return context;
    }
    static bool CanSelect(UnitEntityData unit, BlueprintFeatureSelection pool, BlueprintFeature feat) {
        var state = Empty<LevelUpState>(); Set(state, "m_Unit", unit); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", unit.Progression.Classes[0].Level); state.NextClassLevel = unit.Progression.Classes[0].Level; state.SelectedClass = unit.Progression.Classes[0].CharacterClass;
        var pick = state.AddSelection(null, default(FeatureSource), pool, 1); selectCalls++; return pool.CanSelect(unit.Descriptor, state, pick, feat);
    }
    static void Choices(BlueprintsCache cache) {
        var unit = Unit(cache, "selection"); var feat = Asset<BlueprintFeature>(cache, Token); var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(BasicId));
        foreach (int level in new[] { 1, 10, 20 }) { unit.Progression.Classes[0].Level = level; Check(CanSelect(unit, pool, feat), "native selection allows arbitrary class and level " + level); }
        var fact = Add(unit, feat); Check(!CanSelect(unit, pool, feat), "native selection excludes already owned feat"); Remove(fact); Check(CanSelect(unit, pool, feat), "removing feat restores ordinary selection");
    }
    static void FirstCast(BlueprintsCache cache, UnitEntityData caster, UnitEntityData target, string token, int expected, bool resisted = false) {
        var spell = Asset<BlueprintAbility>(cache, token);
        var ability = Empty<AbilityData>(); Set(ability, "Blueprint", spell); Set(ability, "m_CasterRef", Get(Context(caster, target, spell), "m_CasterRef"));
        var context = Empty<AbilityExecutionContext>(); Set(context, "Ability", ability); Set(context, "AssociatedBlueprint", spell); Set(context, "m_CasterRef", Get(ability, "m_CasterRef")); Set(context, "ClickedTarget", new TargetWrapper(target)); context.SetParams(new AbilityParams { CasterLevel = 16, DC = 22 });
        int calls = resistanceRules.Count, applied = initialApplyCalls;
        AccessTools.Method(typeof(AbilityExecutionProcess), "DoApplyEffect").Invoke(null, new object[] { context, new TargetWrapper(target), new ApplyBoundary() });
        Check(resistanceRules.Count == calls + 1 && resistanceRules.Last().SpellPenetration == 16 + expected && resistanceRules.Last().IsSpellResisted == resisted && initialApplyCalls == applied + (resisted ? 0 : 1), "actual native initial DoApplyEffect SR adds exact bonus and gates effect for " + token);
    }
    static void Resistance(BlueprintsCache cache) {
        UnitEntityData owner = Unit(cache, "sr-owner"), other = Unit(cache, "sr-other"), enemy = Unit(cache, "sr-enemy");
        var feat = Asset<BlueprintFeature>(cache, Token); var owned = Add(owner, feat); var foreign = Add(other, feat); Subscribe(owned, foreign);
        foreach (string token in new[] { "SpiritualWeapon", "SpiritualAlly" }.SelectMany(root => new[] { root }.Concat(Enumerable.Range(0, 5).Select(i => root + "Form" + i)))) {
            FirstCast(cache, owner, enemy, token, 2); FirstCast(cache, other, enemy, token, 2);
        }
        foreach (string token in new[] { "MagesSword", "TwilightKnife", "ArcaneEye" }) FirstCast(cache, owner, enemy, token, 0);
        var unrelated = new BlueprintAbility { AssetGuid = Id("01433ForeignGuardian"), name = "SpiritualWeapon", ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(unrelated, "m_Parent", Ref<BlueprintAbilityReference>(Asset<BlueprintAbility>(cache, "SpiritualWeapon")));
        Check(CompleteSR(new RuleSpellResistanceCheck(Context(owner, enemy, unrelated), enemy)).SpellPenetration == 16, "foreign same-name or parent-linked spell does not qualify");
        var wrong = new RuleSpellResistanceCheck(Context(other, enemy, Asset<BlueprintAbility>(cache, "SpiritualWeapon")), enemy); Set(wrong, "Initiator", owner);
        Check(CompleteSR(wrong).SpellPenetration == 16, "mismatched caster and initiator cannot borrow feat");
        var combined = new RuleSpellResistanceCheck(Context(owner, enemy, Asset<BlueprintAbility>(cache, "SpiritualWeapon")), enemy); combined.AddSpellPenetration(3, ModifierDescriptor.UntypedStackable);
        Check(CompleteSR(combined).SpellPenetration == 21, "native SR preserves another +3 penetration bonus and adds guardian +2 exactly once");
        Check(combined.Context.Params.CasterLevel == 16, "penetration bonus does not rewrite spell caster level");
        var part = enemy.Ensure<UnitPartSpellResistance>(); int sr = part.AddResistance(27, "guardian033-native-sr", null, null, null);
        FirstCast(cache, owner, enemy, "SpiritualWeaponForm0", 2);
        Remove(owned); FirstCast(cache, owner, enemy, "SpiritualWeaponForm0", 0, true); FirstCast(cache, other, enemy, "SpiritualAllyForm0", 2); Remove(foreign);
        FirstCast(cache, other, enemy, "SpiritualAllyForm0", 0, true); part.Remove(sr);
    }
    static void BindAndDamage(BlueprintsCache cache) {
        foreach (int kind in new[] { 0, 1, 2, 3 }) foreach (bool enabled in new[] { false, true }) foreach (int cl in new[] { 1, 6, 11, 16, 20 }) {
            string token = new[] { "SpiritualWeapon", "SpiritualAlly", "MagesSword", "TwilightKnife" }[kind];
            UnitEntityData caster = Unit(cache, "caster-" + ++serial), actor = Unit(cache, "actor-" + serial), target = Unit(cache, "target-" + serial);
            caster.Stats.BaseAttackBonus.BaseValue = 7;
            Feature feat = enabled ? Add(caster, Asset<BlueprintFeature>(cache, Token)) : null;
            Subscribe(feat == null ? Array.Empty<Feature>() : new[] { feat });
            var bp = Asset<BlueprintUnit>(cache, token + "Unit"); Set(actor.Descriptor, "Blueprint", bp); Set(actor.Descriptor, "OriginalBlueprint", bp);
            var host = Psychic034Tests.Fact(caster, Asset<BlueprintBuff>(cache, token + "Controller"), caster, cl);
            var source = Asset<BlueprintAbility>(cache, kind < 2 ? token + "Form3" : token);
            var sourceContext = Context(caster, target, source, cl); Set(host, "m_Context", sourceContext);
            var runtime = host.Components.Single(c => c.SourceBlueprintComponent is PsychicSpiritController); var state = runtime.GetData<PsychicSpiritState>(); state.NextAttack = TimeSpan.FromSeconds(120);
            var bind = new PsychicSpiritBind { Kind = kind, Choice = 3, Controller = Ref<BlueprintBuffReference>(host.Blueprint) };
            using (sourceContext.GetDataScope(new TargetWrapper(actor))) bind.RunAction();
            int expected = enabled && kind < 2 ? cl : 7;
            Check(state.Bab == expected && actor.Stats.BaseAttackBonus.BaseValue == (kind == 2 ? cl : expected), "real Bind keeps controller/actor attack bases synchronized for kind " + kind + " feat " + enabled + " CL" + cl);
            Check(state.Summon == actor && state.Level == cl && state.Target == target, "normal owner/summon/target/CL binding retained");
            var weapon = actor.Body.PrimaryHand.MaybeWeapon;
            if (weapon == null) {
                Equip(actor, Asset<BlueprintItemWeapon>(cache, token + "Item"), ref dummy);
                weapon = actor.Body.PrimaryHand.MaybeWeapon;
            }
            var actorBuff = Psychic034Tests.Fact(actor, Asset<BlueprintBuff>(cache, token + "ActorBuff"), caster, cl);
            var art = actorBuff.Components.Single(c => c.SourceBlueprintComponent is PsychicSpiritAttackStats); var component = (PsychicSpiritAttackStats)art.SourceBlueprintComponent;
            actor.Stats.Strength.BaseValue = 26; wielder = actor.Descriptor;
            var damage = new RuleCalculateWeaponStats(actor, weapon);
            using (art.RequestEventContext()) component.OnEventAboutToTrigger(damage);
            damage.OnTrigger(null); int normal = kind == 2 ? 3 : kind == 3 ? 0 : Math.Min(5, cl / 3);
            Check(damage.DamageDescription[0].CreateDamage().Bonus == normal + (enabled && kind < 2 ? 2 : 0), "native final damage adds exact guardian +2 only, without Strength leakage");
            state.NextAttack = TimeSpan.Zero; int attackBefore = attacks.Count;
            using (runtime.RequestEventContext()) AccessTools.Method(typeof(PsychicSpiritController), "Attack").Invoke(runtime.SourceBlueprintComponent, new object[] { false });
            int count = kind >= 2 ? 1 : Math.Max(1, 1 + (expected - 1) / 5);
            Check(attacks.Count - attackBefore == count && attacks.Skip(attackBefore).Select(a => a.AttackBonusPenalty).SequenceEqual(Enumerable.Range(0, count).Select(i => i * 5)), "real controller schedules exact native iterative count and penalties");
            var numeric = (PsychicSpiritState)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(state, null); numeric.Summon = numeric.Target = null;
            var roundtrip = JsonConvert.DeserializeObject<PsychicSpiritState>(JsonConvert.SerializeObject(numeric));
            Check(roundtrip.Bab == state.Bab && roundtrip.Level == state.Level && roundtrip.NextAttack == state.NextAttack, "actual existing serialized state retains cast-time BAB/CL/timing");
            Set(damage, "m_Triggered", true);
            var outer = new RuleCalculateAttackBonus(actor, target, damage, 0); var inner = (RuleCalculateAttackBonusWithoutTarget)Get(outer, "m_InnerRule");
            using (art.RequestEventContext()) component.OnEventAboutToTrigger(inner); inner.OnTrigger(null); Set(outer, "Result", inner.Result);
            string details = (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpiritAttackLogPatch"), "Details").Invoke(null, new object[] { outer });
            Check(details.Contains(kind == 2 ? "Caster level: +" + cl : "Spell base attack bonus: +" + expected), "resolved log reflects actual spell attack base without assuming current caster BAB");
            caster.Stats.BaseAttackBonus.BaseValue = 1;
            if (enabled) { Remove(feat); Check(state.Bab == expected && actor.Stats.BaseAttackBonus.BaseValue == (kind == 2 ? cl : expected), "feat removal does not rewrite previously cast BAB snapshot"); }
            string historical = (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSpiritAttackLogPatch"), "Details").Invoke(null, new object[] { outer });
            Check(historical == details, "later caster/feat changes do not alter resolved attack log");
            roundtrip.Summon = state.Summon; roundtrip.Target = state.Target; Set(runtime, "m_Data", roundtrip);
            Check(runtime.GetData<PsychicSpiritState>().Bab == expected, "runtime accepts restored scalar state while entity references are controlled fixtures");
        }
    }
    static void RedirectAndOpportunity(BlueprintsCache cache) {
        foreach (bool enabled in new[] { false, true }) {
            UnitEntityData caster = Unit(cache, "reaction-owner-" + enabled), actor = Unit(cache, "reaction-actor-" + enabled), first = Unit(cache, "reaction-first-" + enabled), second = Unit(cache, "reaction-second-" + enabled);
            Feature feat = enabled ? Add(caster, Asset<BlueprintFeature>(cache, Token)) : null; Subscribe(feat == null ? Array.Empty<Feature>() : new[] { feat });
            var blueprint = Asset<BlueprintUnit>(cache, "SpiritualAllyUnit"); Set(actor.Descriptor, "Blueprint", blueprint); Set(actor.Descriptor, "OriginalBlueprint", blueprint);
            Equip(actor, Asset<BlueprintItemWeapon>(cache, "SpiritualAllyItem"), ref dummy);
            var host = Psychic034Tests.Fact(caster, Asset<BlueprintBuff>(cache, "SpiritualAllyController"), caster, 16); var source = Asset<BlueprintAbility>(cache, "SpiritualAllyForm3");
            Set(host, "m_Context", Context(caster, first, source)); var rt = host.Components.Single(c => c.SourceBlueprintComponent is PsychicSpiritController); var data = rt.GetData<PsychicSpiritState>(); data.Summon = actor; data.Target = first; data.Level = 16; data.Bab = enabled ? 16 : 7; data.NextAttack = TimeSpan.Zero;
            var actorBuff = Psychic034Tests.Fact(actor, Asset<BlueprintBuff>(cache, "SpiritualAllyActorBuff"), caster, 16); var art = actorBuff.Components.Single(c => c.SourceBlueprintComponent is PsychicSpiritAttackStats); var component = (PsychicSpiritAttackStats)art.SourceBlueprintComponent;
            var redirect = new PsychicSpiritRedirect { Controller = Ref<BlueprintBuffReference>(host.Blueprint) }; int before = resistanceRules.Count, fired = attacks.Count;
            using (Context(caster, first, source).GetDataScope(new TargetWrapper(first))) redirect.RunAction();
            Check(resistanceRules.Count == before + 1 && resistanceRules.Last().SpellPenetration == (enabled ? 18 : 16) && attacks.Count == fired + 1, "actual redirect resolves native SR once with scoped +2 before single attack"); redirectChecks++;
            data.NextAttack = TimeSpan.Zero; using (Context(caster, first, source).GetDataScope(new TargetWrapper(first))) redirect.RunAction();
            Check(resistanceRules.Count == before + 1, "redirect to passed opponent uses unchanged per-casting SR cache");
            var attack = Empty<RuleAttackRoll>(); Set(attack, "Initiator", actor); Set(attack, "Target", second); attack.RuleAttackWithWeapon = Empty<RuleAttackWithWeapon>(); attack.RuleAttackWithWeapon.IsAttackOfOpportunity = true;
            before = resistanceRules.Count; fired = attacks.Count; var deadline = data.NextAttack;
            using (art.RequestEventContext()) component.OnEventAboutToTrigger(attack);
            Check(resistanceRules.Count == before + 1 && resistanceRules.Last().SpellPenetration == (enabled ? 18 : 16) && !attack.AutoMiss && data.ResistancePassed.Contains(second.UniqueId), "actual Ally AoO path attributes native SR bonus to original caster"); opportunityChecks++;
            using (art.RequestEventContext()) component.OnEventAboutToTrigger(attack);
            Check(resistanceRules.Count == before + 1 && data.NextAttack == deadline && attacks.Count == fired, "AoO reuses SR success and preserves scheduled attack deadline/budget");
            attack.RuleAttackWithWeapon.IsAttackOfOpportunity = false; using (art.RequestEventContext()) component.OnEventAboutToTrigger(attack);
            Check(resistanceRules.Count == before + 1, "ordinary attack is not rerouted through opportunity SR");
            if (feat != null) Remove(feat);
        }
    }
    static void OrdinaryCreatureAndForeignCaster(BlueprintsCache cache) {
        UnitEntityData caster = Unit(cache, "damage-holder"), other = Unit(cache, "damage-other"), actor = Unit(cache, "damage-actor");
        var feat = Add(caster, Asset<BlueprintFeature>(cache, Token)); var actorFeat = Add(actor, Asset<BlueprintFeature>(cache, Token));
        var item = (ItemEntityWeapon)AccessTools.Method(typeof(Linnorm01416Tests), "Weapon").Invoke(null, new object[] { actor, WeaponCategory.Longsword, "guardian033-ordinary-weapon" });
        actor.Stats.Strength.BaseValue = 10; wielder = actor.Descriptor;
        var ordinary = new RuleCalculateWeaponStats(actor, item); ordinary.OnTrigger(null);
        Check(ordinary.DamageDescription[0].CreateDamage().Bonus == 0, "ordinary creature holding the feat gains no weapon damage bonus");
        var buff = Psychic034Tests.Fact(actor, Asset<BlueprintBuff>(cache, "SpiritualWeaponActorBuff"), other, 12); var runtime = buff.Components.Single(c => c.SourceBlueprintComponent is PsychicSpiritAttackStats); var component = (PsychicSpiritAttackStats)runtime.SourceBlueprintComponent;
        Equip(actor, Asset<BlueprintItemWeapon>(cache, "SpiritualWeaponItem"), ref dummy);
        var foreign = new RuleCalculateWeaponStats(actor, actor.Body.PrimaryHand.MaybeWeapon); using (runtime.RequestEventContext()) component.OnEventAboutToTrigger(foreign); foreign.OnTrigger(null);
        Check(foreign.DamageDescription[0].CreateDamage().Bonus == 4, "actor's feat cannot empower a casting belonging to another caster without it");
        Set(buff, "m_Context", Context(caster, actor, Asset<BlueprintAbility>(cache, "SpiritualWeaponForm3"), 12));
        var owned = new RuleCalculateWeaponStats(actor, actor.Body.PrimaryHand.MaybeWeapon); using (runtime.RequestEventContext()) component.OnEventAboutToTrigger(owned); owned.OnTrigger(null);
        Check(owned.DamageDescription[0].CreateDamage().Bonus == 6, "caster and actor both holding feat still contribute only +2");
        Remove(actorFeat); Remove(feat);
    }
    static bool dummy;
    internal static int Run(BlueprintsCache cache) {
        checks = serial = nativeApplyCalls = srReplacements = initialApplyCalls = nativeFeatureAdds = nativeFeatureRemoves = selectCalls = redirectChecks = opportunityChecks = 0; units.Clear(); attacks.Clear(); resistanceRules.Clear();
        var h = new Harmony("CotW01433.SpiritualGuardianContracts"); var game = Game.Instance; var oldState = Get(game, "State"); var oldTime = Get(game, "TimeController");
        var player = Empty<Player>(); player.GameTime = TimeSpan.FromSeconds(100); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); var state = Empty<PersistentState>(); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(SpiritualGuardian01433Tests), name));
        Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(Identity)); Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(False)); Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(Ignore)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NullController));
        Patch(AccessTools.PropertyGetter(typeof(BlueprintUnitFact), "Name"), nameof(DisplayName));
        Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Time)); Patch(AccessTools.Method(typeof(UnitEntityData), "IsEnemy", new[] { typeof(UnitEntityData) }), nameof(Enemy));
        Patch(AccessTools.PropertyGetter(typeof(ItemEntityWeapon), "HoldInTwoHands"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(ItemEntity), "EnchantmentValue"), nameof(Zero)); Patch(AccessTools.PropertyGetter(typeof(ItemEntity), "Wielder"), nameof(NoWielder)); Patch(AccessTools.Method(typeof(RuleCalculateWeaponSizeBonus), "OnTrigger"), nameof(Skip));
        Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpiritWeaponForms"), "Equip"), nameof(Equip));
        Patch(AccessTools.Method(typeof(PsychicSpiritController), "InRange"), nameof(Range)); Patch(AccessTools.Method(typeof(PsychicSpiritController), "PlaceNear"), nameof(Skip));
        Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.SpiritWeaponAnimatedVisual"), "Swing"), nameof(Skip));
        Patch(AccessTools.Method(typeof(AbilityExecutionProcess), "SpawnFxs"), nameof(Skip));
        Patch(AccessTools.PropertyGetter(AccessTools.Inner(typeof(RulebookEvent), "Dice"), "D20"), nameof(FixedD20));
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(SpiritualGuardian01433Tests), nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(AbilityExecutionProcess), "DoApplyEffect"), transpiler: new HarmonyMethod(typeof(SpiritualGuardian01433Tests), nameof(FirstCastBoundary)));
        PsychicRuleTestBoundary.Patch(h, typeof(RuleSpellResistanceCheck), AccessTools.Method(typeof(SpiritualGuardian01433Tests), nameof(SR)));
        PsychicRuleTestBoundary.Patch(h, typeof(RuleAttackWithWeapon), AccessTools.Method(typeof(SpiritualGuardian01433Tests), nameof(Strike)));
        Provider01419Tests.PatchLabels(h);
        try {
            Registration(cache); Choices(cache); Resistance(cache); BindAndDamage(cache); RedirectAndOpportunity(cache); OrdinaryCreatureAndForeignCaster(cache);
            Check(srReplacements == 1 && nativeApplyCalls > 0 && nativeFeatureAdds > 0 && nativeFeatureRemoves > 0, "native first-cast site, apply endpoint and feature lifecycle all executed");
            PsychicExpansionTests.Inventory["spiritualGuardian01433Contracts"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["ordinaryFeatCount"] = 1, ["itemIds"] = new JArray("COTW-0247"),
                ["nativeInitialApplyCalls"] = initialApplyCalls, ["nativeRuleApplyCalls"] = nativeApplyCalls, ["nativeFeatureAdds"] = nativeFeatureAdds, ["nativeFeatureRemoves"] = nativeFeatureRemoves, ["nativeCanSelectCalls"] = selectCalls, ["redirectCases"] = redirectChecks, ["opportunityCases"] = opportunityChecks,
                ["registrationRollbackAndIdentity"] = true, ["nativeSelectionPreview"] = true, ["nativeInitialSRPath"] = true, ["nativeRedirectAndAoOSRPaths"] = true, ["nativeControllerBindingAndIteratives"] = true, ["nativeDamageCalculation"] = true, ["casterAndSpellScope"] = true, ["serializedCastTimeSnapshot"] = true, ["resolvedHistoricalAttackLog"] = true,
                ["newBlueprints"] = new JArray(new JObject { ["id"] = Id(Token).ToString(), ["name"] = Asset<BlueprintFeature>(cache, Token).name, ["type"] = typeof(BlueprintFeature).FullName }),
                ["boundary"] = "Native Feature CreateFact/manager Add/Remove/TurnOn and selection CanSelect execute; prerequisite UI updates/unique IDs/display text are bounded. Native AbilityExecutionProcess.DoApplyEffect constructs initial SR; only generic Rulebook calls are redirected to original native subscription proxies and original RuleSpellResistanceCheck.OnTrigger. D20=10, empty or native UnitPartSpellResistance SR=27 and final RuleApplySpell/effect/FX endpoints are inputs. Production Bind, controller Attack and original native weapon stats execute. Equipment insertion, scene position/range, animation Swing and final weapon attack are bounded. Actual existing state scalar fields JSON-round-trip with entity references excluded and reattached as controlled inputs; full save/load, Unity UI and combat rendering remain tester steps."
            };
            Console.WriteLine("PASS: " + checks + " SpiritualGuardian01433 native registration, selection, first-cast SR, binding, iterative and damage checks."); return checks;
        } finally {
            foreach (var unit in units) foreach (var fact in unit.Facts.List.OfType<Feature>().ToArray()) if (fact.Manager != null) fact.Manager.Remove(fact);
            h.UnpatchAll(h.Id); Set(game, "State", oldState); Set(game, "TimeController", oldTime); units.Clear(); wielder = null; subscribers = null;
        }
    }
}
