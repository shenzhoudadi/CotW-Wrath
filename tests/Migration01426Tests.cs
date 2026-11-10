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
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Migration01426Tests
{
    static int checks, saves, applyCalls;
    static bool failPublish, failAppend, savePassed, enemy = true;
    static BlueprintGuid faultId;
    static readonly List<Tuple<BlueprintBuff, UnitEntityData, UnitEntityData, ContextDurationValue>> applied = new List<Tuple<BlueprintBuff, UnitEntityData, UnitEntityData, ContextDurationValue>>();
    static Type Module(string name) => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o, string f, object v) => (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).SetValue(o, v);
    static object Get(object o, string f) => (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).GetValue(o);
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Migration01426: " + why); checks++; }
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Module("PsychicAssets"), "Id").Invoke(null, new object[] { token });
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static void Register(BlueprintsCache c, string module) => AccessTools.Method(Module(module), "Register").Invoke(null, new object[] { c });
    static bool InjectionRejected(Action f) { try { f(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (!(e is InvalidOperationException) || !e.Message.Contains("01426 injection")) throw new Exception("Unexpected registration failure before injection", e); return true; } }
    static bool Rejected(Action f) { try { f(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e is InvalidOperationException; } }
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static bool NullController(ref LevelUpController __result) { __result = null; return false; }
    static void PublishFault(BlueprintGuid guid) { if (failPublish && guid == faultId) throw new InvalidOperationException("01426 injection after cache insert"); }
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(f => f.AssetGuid == faultId)) throw new InvalidOperationException("01426 injection after pool append"); }
    static UnitEntityData Unit(BlueprintsCache c, string n) {
        var u = (UnitEntityData)AccessTools.Method(typeof(SpellAndSword01417Tests), "Unit").Invoke(null, new object[] { c, n });
        int size = Enum.GetValues(typeof(UnitCondition)).Cast<UnitCondition>().Max(x => (int)x) + 1;
        Set(u.State, "m_Conditions", new sbyte[size]); Set(u.State, "m_ConditionsImmunity", new sbyte[size]); Set(u.State, "Owner", u.Descriptor);
        return u;
    }
    static Feature Fact(UnitEntityData u, BlueprintFeature b) => (Feature)AccessTools.Method(typeof(Linnorm01416Tests), "Fact").Invoke(null, new object[] { u, b });
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => (BlueprintFeatureReference[])Get(p, field) ?? Array.Empty<BlueprintFeatureReference>();
    static bool Save(RuleSavingThrow rule, ref RuleSavingThrow __result) { saves++; Check(rule.Type == SavingThrowType.Will, "howl uses Will"); __result = rule; return false; }
    static bool Passed(ref bool __result) { __result = savePassed; return false; }
    static bool Enemy(ref bool __result) { __result = enemy; return false; }
    static bool Expose(Exception __1) { throw new Exception("01426 native action error", __1); }
    static RuleCalculateAbilityParams CompleteParams(RuleCalculateAbilityParams rule) { rule.OnTrigger(null); return rule; }
    static RuleCalculateAbilityParams CompleteContextParams(MechanicsContext context, RuleCalculateAbilityParams rule) => CompleteParams(rule);
    static Kingmaker.Settings.Difficulty.DifficultyPresetsList.StatsAdjustmentPreset DifficultyPreset() => new Kingmaker.Settings.Difficulty.DifficultyPresetsList.StatsAdjustmentPreset();
    static IEnumerable<CodeInstruction> DifficultyBoundary(IEnumerable<CodeInstruction> code) {
        var list = code.ToList(); int first = list.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(Kingmaker.Blueprints.Root.BlueprintRoot) && m.Name == "get_Instance");
        int last = list.FindIndex(first, i => i.operand is MethodInfo m && m.Name == "GetAdjustmentPreset");
        if (first < 0 || last != first + 5) throw new InvalidOperationException("Native difficulty lookup changed.");
        var replacement = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Migration01426Tests), nameof(DifficultyPreset))); replacement.labels.AddRange(list[first].labels);
        list.RemoveRange(first, last - first + 1); list.Insert(first, replacement); return list;
    }
    static IEnumerable<CodeInstruction> ParamsBoundary(IEnumerable<CodeInstruction> code) {
        foreach (var instruction in code) {
            if (instruction.operand is MethodInfo m && m.DeclaringType == typeof(Rulebook) && m.Name == "Trigger" && m.IsGenericMethod && m.GetGenericArguments().SequenceEqual(new[] { typeof(RuleCalculateAbilityParams) })) {
                instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Migration01426Tests), nameof(CompleteParams));
            }
            if (instruction.operand is MethodInfo c && c.DeclaringType == typeof(MechanicsContext) && c.Name == "TriggerRule" && c.IsGenericMethod && c.GetGenericArguments().SequenceEqual(new[] { typeof(RuleCalculateAbilityParams) })) {
                instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(Migration01426Tests), nameof(CompleteContextParams));
            }
            yield return instruction;
        }
    }
    static bool Apply(ContextActionApplyBuff __instance) {
        var scope = ContextData<MechanicsContext.Data>.Current; var target = scope.CurrentTarget.Unit; var caster = scope.Context.MaybeCaster;
        applied.Add(Tuple.Create(__instance.Buff, target, caster, __instance.DurationValue));
        if (__instance.Buff.AssetGuid == Id("TerrifyingHowlImmunity")) Psychic034Tests.Fact(target, __instance.Buff, caster, 8);
        return false;
    }
    static MechanicsContext Context(UnitEntityData caster, BlueprintScriptableObject source, UnitEntityData target = null) {
        var ctx = new MechanicsContext(caster, (target ?? caster).Descriptor, source, null, new TargetWrapper(target ?? caster));
        ctx.SetParams(new AbilityParams { DC = 22, CasterLevel = 8 }); return ctx;
    }
    internal static int Run(BlueprintsCache cache, string evidence) {
        checks = saves = applyCalls = 0; var h = new Harmony("CotW01426.MigrationContracts");
        h.Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(Skip)));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(No)));
        h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(PoolIgnore)));
        h.Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(NullController)));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"), postfix: new HarmonyMethod(typeof(Migration01426Tests), nameof(PublishFault)));
        h.Patch(AccessTools.Method(Module("T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(Migration01426Tests), nameof(AppendFault)));
        Provider01419Tests.PatchLabels(h);
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), nameof(DifficultyBoundary)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(No)));
        h.Patch(AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }), transpiler: new HarmonyMethod(typeof(Migration01426Tests), nameof(ParamsBoundary)));
        h.Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(Expose)));
        var basic = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));
        var oldBasic = new[] { Get(basic, "m_Features"), Get(basic, "m_AllFeatures") };
        // The managed fixture has selected game effects, not every native feat.
        // As in the whole-pool audit, unhydrated native entries retain exact IDs
        // with reference-only metadata objects; chosen test children are real.
        var data = JObject.Parse(System.IO.File.ReadAllText(evidence));
        // Earlier spell integration tests deliberately use reference-only spell
        // entries. Restore these donors from the real game archive before cloning.
        foreach (string id in new[] { "d7cbd2004ce66a042aeab2e95a3c5c61", "55a037e514c0ee14a8e3ed14b47061de", "8d6073201e5395d458b8251386d72df1" }) {
            var donor = PsychicExpansionTests.NativeCase<BlueprintAbility>(id);
            var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
            foreach (string field in new[] { "Range", "ActionType", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "CanTargetPoint" }) {
                var info = AccessTools.Field(typeof(BlueprintAbility), field); info.SetValue(donor, PsychicExpansionTests.Read(row[field], info.FieldType));
            }
            cache.AddCachedBlueprint(donor.AssetGuid, donor);
        }
        var valid = Refs(basic, "m_AllFeatures").Select(r => r.Get() != null ? r : Ref<BlueprintFeatureReference>(new BlueprintFeature { AssetGuid = r.Guid, name = "NativeReference_" + r.Guid, ComponentsArray = Array.Empty<BlueprintComponent>() })).ToArray();
        foreach (var r in valid) Check(data["nativeBlueprintTypes"][r.Guid.ToString()] != null || r.Get().name.StartsWith("CotW") || r.Get().name.StartsWith("014"), "ordinary source identity belongs to native or registered fixture");
        Set(basic, "m_AllFeatures", valid); Set(basic, "m_Features", valid.ToArray());
        try {
            var registered = new List<SimpleBlueprint>();
            var cases = new[] { new[] { "RogueFeatTalent", "RogueAdvancedFeatTalent" }, new[] { "TerrifyingHowl", "TerrifyingHowlRagePower" }, new[] { "Inquisitions01426", "ConversionInquisition" } };
            foreach (var item in cases) {
                var pools = ((string[])AccessTools.Field(Module(item[0]), "PoolIds").GetValue(null)).Select(id => (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).ToArray();
                var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
                var keys = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToHashSet(); faultId = Id(item[1]);
                failPublish = true; Check(InjectionRejected(() => Register(cache, item[0])), "actual publication fault reached " + item[0]); failPublish = false;
                Check(cache.Load(faultId) == null, "partial root removed " + item[0]);
                failAppend = true; Check(InjectionRejected(() => Register(cache, item[0])), "actual append fault reached " + item[0]); failAppend = false;
                Check(cache.Load(faultId) == null, "append fault removes graph root " + item[0]);
                Check(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().All(keys.Contains), "rollback removes every newly published asset " + item[0]);
                for (int i = 0; i < pools.Length; i++) for (int j = 0; j < 2; j++) Check(ReferenceEquals(Get(pools[i], j == 0 ? "m_Features" : "m_AllFeatures"), before[i][j]), "exact array rollback " + item[0] + i + j);
                var collision = new BlueprintFeature { AssetGuid = faultId, name = "01426Foreign", ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(faultId, collision);
                Check(Rejected(() => Register(cache, item[0])) && ReferenceEquals(cache.Load(faultId), collision), "foreign ID preserved " + item[0]); cache.RemoveCachedBlueprint(faultId);
                Register(cache, item[0]); var root = cache.Load(faultId); Register(cache, item[0]); Check(ReferenceEquals(cache.Load(faultId), root), "repeat registration keeps root " + item[0]);
                registered.AddRange(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Where(id => !keys.Contains(id)).Select(cache.Load));
                for (int i = 0; i < pools.Length; i++) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                    var original = (BlueprintFeatureReference[])before[i][field == "m_Features" ? 0 : 1] ?? Array.Empty<BlueprintFeatureReference>(); var now = Refs(pools[i], field);
                    Check(now.Take(original.Length).SequenceEqual(original), "original pool reference/order preserved " + item[0]); Check(now.Count(r => r.Guid == faultId) == 1, "one root per pool " + item[0]);
                }
            }
            Talent(cache, h, basic); Domains(cache); Howl(cache, h);
            var tokens = new[] { "RogueAdvancedFeatTalent", "TerrifyingHowlRagePower", "ConversionInquisition", "ValorInquisition", "PersistenceInquisition" };
            foreach (string token in tokens) Check(!Refs(basic, "m_AllFeatures").Any(r => r.Guid == Id(token)), "class option is not an ordinary feat " + token);
            PsychicExpansionTests.Inventory["migration01426Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItems"] = new JArray(tokens), ["additionalItemCount"] = 5, ["nativeSelectFeatureApplyCalls"] = applyCalls, ["transactionFaults"] = 6, ["optionalRefreshRollback"] = true, ["recursiveSelectionExcluded"] = true, ["newBlueprints"] = new JArray(registered.OrderBy(b => b.name).Select(b => new JObject { ["name"] = b.name, ["id"] = b.AssetGuid.ToString(), ["type"] = b.GetType().Name })), ["boundary"] = "Actual production module registration/rollback, native selection and progression level calculation, native final params/DC/ranks/resource spending/restoration/stat replacement, native howl action tree and caster-specific buff predicate. Controlled unit/scene and final buff insertion, neutral difficulty preset and direct parameter-rule completion instead of EventBus, deterministic saving-throw outcomes and enemy relationship; native nonselected feat entries may be reference-only metadata. No full Unity startup/rendering or save load." };
            Console.WriteLine("PASS: " + checks + " five-item migration contracts."); return checks;
        }
        finally { failPublish = failAppend = false; Set(basic, "m_Features", oldBasic[0]); Set(basic, "m_AllFeatures", oldBasic[1]); h.UnpatchAll(h.Id); }
    }
    static void Talent(BlueprintsCache cache, Harmony h, BlueprintFeatureSelection basic) {
        var root = Asset<BlueprintFeatureSelection>(cache, "RogueAdvancedFeatTalent"); var u = Unit(cache, "01426-talent");
        var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("c074a5d615200494b8f2a9c845799d93"));
        var state = Empty<LevelUpState>(); Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 10);
        if (u.Progression.Classes.Count == 0) u.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("67819271767a9dd4fbfd4ae700befea0"))) { Level = 10 });
        state.NextClassLevel = 10; state.SelectedClass = u.Progression.Classes[0].CharacterClass;
        var pending = state.AddSelection(null, default(FeatureSource), pool, 10);
        Check(!pool.CanSelect(u.Descriptor, state, pending, root), "Feat requires Advanced Talents"); Fact(u, (BlueprintFeature)cache.Load(BlueprintGuid.Parse("a33b99f95322d6741af83e9381b2391c")));
        Check(pool.CanSelect(u.Descriptor, state, pending, root), "qualified advanced talent is selectable");
        Check(root.Ranks == 1 && !root.IgnorePrerequisites && root.Groups.Contains(FeatureGroup.RogueTalent), "native once-only nested selection");
        var leaf = new BlueprintFeature { AssetGuid = Id("01426TestFeat"), name = "01426TestFeat", Ranks = 1, ComponentsArray = new BlueprintComponent[] { new PrerequisiteStatValue { Stat = StatType.Strength, Value = 13 } } }; leaf.OnEnable(); cache.AddCachedBlueprint(leaf.AssetGuid, leaf);
        var oldRefs = Refs(basic, "m_AllFeatures"); Set(basic, "m_AllFeatures", oldRefs.Concat(new[] { Ref<BlueprintFeatureReference>(leaf) }).ToArray());
        AccessTools.Method(Module("RogueFeatTalent"), "Optional").Invoke(null, new object[] { cache });
        Check(Refs(root, "m_AllFeatures").Count(r => r.Guid == leaf.AssetGuid) == 1, "late ordinary feat reaches advanced talent exactly once");
        u.Stats.Strength.BaseValue = 12; Check(!root.CanSelect(u.Descriptor, state, pending, leaf), "child prerequisites remain enforced"); u.Stats.Strength.BaseValue = 13; Check(root.CanSelect(u.Descriptor, state, pending, leaf), "eligible child selectable");
        var selections = AccessTools.Field(typeof(UnitProgressionData), "m_Selections"); selections.SetValue(u.Progression, Activator.CreateInstance(selections.FieldType));
        h.Patch(AccessTools.Method(typeof(SelectFeature), "Apply"), transpiler: new HarmonyMethod(typeof(Feat01424Tests), "ApplyBoundary"));
        var choose = new SelectFeature(pending, root); Check(choose.Check(state, u.Descriptor), "native root SelectFeature.Check"); choose.Apply(state, u.Descriptor); applyCalls++;
        var child = state.Selections.Single(s => s.Selection == root); Check(child.Parent == pending && u.HasFact(root), "native Apply creates child after owning root");
        var selectLeaf = new SelectFeature(child, leaf); Check(selectLeaf.Check(state, u.Descriptor), "native child SelectFeature.Check"); selectLeaf.Apply(state, u.Descriptor); applyCalls++;
        Check(u.HasFact(leaf) && child.SelectedItem == leaf, "native Apply grants selected ordinary feat");
        var second = state.AddSelection(null, default(FeatureSource), pool, 12); Check(!pool.CanSelect(u.Descriptor, state, second, root), "cannot buy advanced Feat twice or recurse through Extra Talent");
        var loop = new BlueprintFeatureSelection { AssetGuid = Id("01426RecursiveSelection"), name = "01426RecursiveSelection", ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(loop, "m_AllFeatures", new[] { Ref<BlueprintFeatureReference>(root) }); Set(loop, "m_Features", new[] { Ref<BlueprintFeatureReference>(root) }); loop.OnEnable();
        Set(basic, "m_AllFeatures", oldRefs.Concat(new[] { Ref<BlueprintFeatureReference>(leaf), Ref<BlueprintFeatureReference>(loop) }).ToArray());
        AccessTools.Method(Module("RogueFeatTalent"), "Optional").Invoke(null, new object[] { cache });
        Check(!root.AllFeatures.Contains(loop) && root.AllFeatures.Contains(leaf), "recursive branch excluded while valid ordinary child retained");
        Check(!root.AllFeatures.Any(f => f.AssetGuid.ToString() == "14d9089df87a43b696fa9451ca2f0a12"), "native Extra Rogue Talent return path excluded only inside Feat");
        var beforeRoot = new[] { Get(root, "m_Features"), Get(root, "m_AllFeatures") }; faultId = root.AssetGuid; failAppend = true;
        try { Check(InjectionRejected(() => AccessTools.Method(Module("RogueFeatTalent"), "Optional").Invoke(null, new object[] { cache })), "optional refresh append fault reached"); } finally { failAppend = false; }
        Check(ReferenceEquals(Get(root, "m_Features"), beforeRoot[0]) && ReferenceEquals(Get(root, "m_AllFeatures"), beforeRoot[1]), "optional refresh restores both exact arrays");
        Set(basic, "m_AllFeatures", oldRefs); AccessTools.Method(Module("RogueFeatTalent"), "Optional").Invoke(null, new object[] { cache }); cache.RemoveCachedBlueprint(leaf.AssetGuid);
    }
    static void Domains(BlueprintsCache cache) {
        var u = Unit(cache, "01426-inquisitions"); var cleric = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("67819271767a9dd4fbfd4ae700befea0")); var inquisitor = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("f1a70d9e1b0b41e49874e1fa9052a1ce"));
        u.Progression.Classes.Clear(); var cl = new ClassData(cleric) { Level = 4 }; var iq = new ClassData(inquisitor) { Level = 4 }; u.Progression.Classes.Add(cl); u.Progression.Classes.Add(iq);
        foreach (string token in new[] { "ConversionInquisition", "ValorInquisition", "PersistenceInquisition" }) {
            var p = Asset<BlueprintProgression>(cache, token); Check(p.CalcLevel(u.Descriptor) == 8, "native cleric/inquisitor sum " + token);
            Check(p.LevelEntries.Select(e => e.Level).SequenceEqual(token == "PersistenceInquisition" ? new[] { 1, 6 } : new[] { 1, 8 }), "correct milestones " + token);
            Check(p.LevelEntries.SelectMany(e => e.Features).All(f => f != null) && p.ComponentsArray.All(c => !(c is AddKnownSpell)), "valid grants without domain spells " + token);
            bool any = token == "ConversionInquisition"; Check(p.MeetsPrerequisites(null, u.Descriptor, null, false, false) == any, "deity restriction before faith " + token);
        }
        Fact(u, (BlueprintFeature)cache.Load(BlueprintGuid.Parse("88d5da04361b16746bf5b65795e0c38c")));
        foreach (string token in new[] { "ValorInquisition", "PersistenceInquisition" }) Check(Asset<BlueprintProgression>(cache, token).MeetsPrerequisites(null, u.Descriptor, null, false, false), "Iomedae eligible " + token);
        foreach (int value in new[] { 1, 6, 10, 18, 30 }) {
            Set(u.Stats.Wisdom, "m_BaseValue", value); Set(u.Stats.Wisdom, "ModifiedValue", value);
            foreach (string token in new[] { "ValorTouchOfResolveUses", "PersistenceRelentlessFootingUses" }) Check(Asset<BlueprintAbilityResource>(cache, token).GetMaxAmount(u.Descriptor) == Math.Max(1, 3 + (int)Math.Floor((value - 10) / 2.0)), "native Wisdom resource " + token + value);
            Check(Asset<BlueprintAbilityResource>(cache, "PersistenceInnerStrengthUses").GetMaxAmount(u.Descriptor) == 1, "Inner Strength fixed shared daily use");
            Check(Asset<BlueprintAbilityResource>(cache, "ConversionSwayingWordUses").GetMaxAmount(u.Descriptor) == 1, "Swaying Word fixed daily use");
        }
        var ability = Asset<BlueprintAbility>(cache, "ConversionSwayingWord");
        Set(u.Stats.Wisdom, "m_BaseValue", 18); Set(u.Stats.Wisdom, "ModifiedValue", 18);
        foreach (int level in new[] { 1, 7, 8, 9, 20 }) {
            cl.Level = level; iq.Level = 0; var ctx = Context(u, ability);
            Check(ability.GetComponents<ContextRankConfig>().Single(c => (AbilityRankType)Get(c, "m_Type") == AbilityRankType.Default).GetValue(ctx) == level, "native caster-level rank " + level);
            Check(ability.GetComponents<ContextRankConfig>().Single(c => (AbilityRankType)Get(c, "m_Type") == AbilityRankType.StatBonus).GetValue(ctx) == level / 2, "native DC level rank " + level);
            // Each level uses a fresh owner: directly editing ClassData does not
            // deliver the game's level-up cache invalidation notifications.
            var atLevel = Unit(cache, "01426-word-level-" + level); atLevel.Progression.Classes.Clear(); atLevel.Progression.Classes.Add(new ClassData(cleric) { Level = level });
            var parameters = ability.GetComponent<ContextCalculateAbilityParams>().Calculate(new AbilityData(ability, atLevel.Descriptor));
            Check(parameters.CasterLevel == level && parameters.DC == 14 + level / 2, "native final caster level and Wisdom DC " + level + ": CL=" + parameters.CasterLevel + " DC=" + parameters.DC);
            var castParameters = ability.GetComponent<ContextCalculateAbilityParams>().Calculate(Context(atLevel, ability));
            Check(castParameters.CasterLevel == parameters.CasterLevel && castParameters.DC == parameters.DC, "native casting and preview agree " + level);
        }
        var word = ability.GetComponent<AbilityEffectRunAction>(); var dominate = ((ContextActionConditionalSaved)word.Actions.Actions.Single()).Failed.Actions.OfType<ContextActionApplyBuff>().Single();
        Check(word.SavingThrowType == SavingThrowType.Will && dominate.DurationValue.Rate == DurationRate.Minutes && dominate.DurationValue.BonusValue.Value == 1 && !dominate.Permanent, "Swaying Word one minute Will negates");
        Check(ability.Type == AbilityType.SpellLike && ability.AvailableMetamagic == 0 && ability.GetComponents<SpellListComponent>().Count() == 0, "SLA does not enter spell lists or metamagic");
        var resolve = Asset<BlueprintAbility>(cache, "ValorTouchOfResolve"); Check(resolve.Range == AbilityRange.Touch && resolve.GetComponent<AbilityTargetsAround>() == null && !resolve.CanTargetEnemies, "Touch of Resolve is single friendly touch");
        var fearless = Asset<BlueprintFeature>(cache, "ValorFearless"); Check(fearless.GetComponents<AddConditionImmunity>().Select(c => c.Condition).OrderBy(x => x).SequenceEqual(new[] { UnitCondition.Frightened, UnitCondition.Shaken, UnitCondition.Cowering }.OrderBy(x => x)), "native fearless immunities");
        var foot = Asset<BlueprintAbility>(cache, "PersistenceRelentlessFooting"); var speed = (ContextActionApplyBuff)foot.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        Check(foot.ActionType == Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift && speed.DurationValue.BonusValue.Value == 1 && speed.Buff.AssetGuid.ToString() == "035ed56eb973f0e469a288ff5991c9ff", "one-round native enhancement speed, swift action");
        var inner = Asset<BlueprintAbility>(cache, "PersistenceInnerStrength"); var variants = inner.GetComponent<AbilityVariants>().Variants;
        Check(variants.Length == 7 && variants.Select(a => a.GetComponent<AbilityResourceLogic>().RequiredResource).Distinct().Count() == 1, "seven conditions count as one root and one shared use");
        foreach (var a in variants) { Check(a.Parent == inner && a.Range == AbilityRange.Personal && a.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionRemoveBuffsByDescriptor>().Count() == 1, "one chosen condition " + a.name); foreach (int level in new[] { 6, 7, 12, 20 }) { cl.Level = 0; iq.Level = level; Check(a.GetComponent<ContextRankConfig>().GetValue(Context(u, a)) == level / 2, "native healing dice rank " + level); } }
        foreach (var a in new[] { ability, resolve, foot, variants[0] }) {
            var cost = a.GetComponent<AbilityResourceLogic>(); var resource = cost.RequiredResource; var spell = new AbilityData(a, u.Descriptor);
            u.Descriptor.Resources.Add(resource, true); int maximum = resource.GetMaxAmount(u.Descriptor);
            Check(cost.IsAbilityRestrictionPassed(spell) && cost.CalculateCost(spell) == 1, "native resource available " + a.name);
            cost.Spend(spell); Check(u.Descriptor.Resources.GetResourceAmount(resource) == maximum - 1, "native spend one " + a.name);
            u.Descriptor.Resources.Spend(resource, u.Descriptor.Resources.GetResourceAmount(resource));
            Check(!cost.IsAbilityRestrictionPassed(spell), "native empty resource disables " + a.name);
            if (a == variants[0]) Check(variants.All(v => !v.GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(new AbilityData(v, u.Descriptor))), "all seven alternatives exhaust together");
            u.Descriptor.Resources.Restore(resource); Check(u.Descriptor.Resources.GetResourceAmount(resource) == maximum, "native restore " + a.name);
        }
        var charm = Fact(u, Asset<BlueprintFeature>(cache, "ConversionCharmOfWisdom")); var runtime = charm.Components.Single(); var component = runtime.SourceBlueprintComponent;
        using (runtime.RequestEventContext()) AccessTools.Method(component.GetType(), "OnTurnOn").Invoke(component, null);
        Check(u.Stats.SkillPersuasion.BaseAttribute.Type == StatType.Wisdom, "native stat replacement actually changes Persuasion base attribute");
        using (runtime.RequestEventContext()) AccessTools.Method(component.GetType(), "OnTurnOff").Invoke(component, null);
        Check(u.Stats.SkillPersuasion.BaseAttribute.Type == StatType.Charisma, "native stat replacement removal restores original");
    }
    static void Howl(BlueprintsCache cache, Harmony h) {
        var ability = Asset<BlueprintAbility>(cache, "TerrifyingHowlAbility"); var power = Asset<BlueprintFeature>(cache, "TerrifyingHowlRagePower");
        var caster = Unit(cache, "01426-howler-a"); var other = Unit(cache, "01426-howler-b"); var target = Unit(cache, "01426-shaken");
        var barb = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("f7d7eb166b3dd594fb330d085df41853")); var levels = new ClassData(barb) { Level = 7 }; caster.Progression.Classes.Clear(); caster.Progression.Classes.Add(levels);
        Check(!power.MeetsPrerequisites(null, caster.Descriptor, null, false, false), "Howl rejects level seven"); levels.Level = 8; Check(power.MeetsPrerequisites(null, caster.Descriptor, null, false, false), "Howl accepts barbarian eight");
        var rage = ability.GetComponent<AbilityCasterHasFacts>(); Check(!rage.IsCasterRestrictionPassed(caster), "Howl cannot be used outside rage");
        Psychic034Tests.Fact(caster, (BlueprintBuff)cache.Load(BlueprintGuid.Parse("da8ce41ac3cd74742b80984ccc3c9613")), caster, 8); Check(rage.IsCasterRestrictionPassed(caster), "normal self rage permits Howl");
        Check(ability.GetComponent<AbilityTargetsAround>().AoERadius == new Feet(30) && ability.GetComponent<AbilityTargetsAround>().Targets == TargetType.Enemy && !ability.SpellResistance, "native enemy area 30 feet, extraordinary bypasses SR");
        caster.Stats.Strength.BaseValue = 18; var parameters = ability.GetComponent<ContextCalculateAbilityParams>().Calculate(new AbilityData(ability, caster.Descriptor));
        Check(parameters.CasterLevel == 8 && parameters.DC == 18, "native barbarian level and Strength final DC");
        var blood = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("d77e67a814d686842802c9cfd8ef8499"));
        var primalist = (BlueprintArchetype)cache.Load(BlueprintGuid.Parse("4d588290ed0736e4f8cd3f4c3beacd69"));
        var nativeClassRow = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == blood.AssetGuid.ToString())["blueprint"]["Data"];
        Set(blood, "m_Archetypes", PsychicExpansionTests.Read(nativeClassRow["m_Archetypes"], typeof(BlueprintArchetypeReference[])));
        var br = new ClassData(blood) { Level = 8 }; other.Progression.Classes.Clear(); other.Progression.Classes.Add(br); other.Stats.Strength.BaseValue = 18;
        Check(!power.MeetsPrerequisites(null, other.Descriptor, null, false, false), "ordinary bloodrager excluded");
        Check(ability.GetComponent<ContextCalculateAbilityParams>().Calculate(new AbilityData(ability, other.Descriptor)).DC == 14, "non-primalist levels do not inflate forced Howl DC");
        br.Archetypes.Add(primalist);
        Check(power.MeetsPrerequisites(null, other.Descriptor, null, false, false) && ability.GetComponent<ContextCalculateAbilityParams>().Calculate(new AbilityData(ability, other.Descriptor)).DC == 18, "Primalist eight qualifies and scales natively");
        var skald = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("2476514e31791394fa140f1a07941c96")); Check(Refs(skald, "m_AllFeatures").Any(r => r.Guid == power.AssetGuid), "01427 follows native Renewed Vigor in Skald menu");
        PsychicRuleTestBoundary.Patch(h, typeof(RuleSavingThrow), AccessTools.Method(typeof(Migration01426Tests), nameof(Save)));
        h.Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow), "IsPassed"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(Passed)));
        h.Patch(AccessTools.Method(typeof(ContextConditionIsEnemy), "CheckCondition"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(Enemy)));
        h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), prefix: new HarmonyMethod(typeof(Migration01426Tests), nameof(Apply)));
        void Run(UnitEntityData c, UnitEntityData t) { var ctx = Context(c, ability, t); using (ctx.GetDataScope(t)) ability.GetComponent<AbilityEffectRunAction>().Actions.Run(); }
        int before = saves; Run(caster, target); Check(saves == before, "unshaken target neither saves nor acquires immunity");
        ((sbyte[])Get(target.State, "m_Conditions"))[(int)UnitCondition.Shaken] = 1; savePassed = false; Run(caster, target);
        Check(saves == before + 1 && applied.Count(a => a.Item1.AssetGuid.ToString() == "f08a7239aa961f34c8301518e71d4cdf") == 1, "failed native branch applies frightened");
        Check(applied.Last().Item4.Rate == DurationRate.Days && applied.Last().Item4.BonusValue.Value == 1, "immunity lasts a day");
        Run(caster, target); Check(saves == before + 1, "same howler blocked by actual buff-from-caster predicate");
        savePassed = true; Run(other, target); Check(saves == before + 2 && applied.Count(a => a.Item1.AssetGuid.ToString() == "f08a7239aa961f34c8301518e71d4cdf") == 1, "other howler still acts; success prevents fear");
        Run(other, target); Check(saves == before + 2, "successful save also grants caster-specific immunity");
        var fresh = Unit(cache, "01426-fresh"); ((sbyte[])Get(fresh.State, "m_Conditions"))[(int)UnitCondition.Shaken] = 1; enemy = false; Run(caster, fresh); Check(saves == before + 2, "allies excluded"); enemy = true;
        var silence = Psychic034Tests.Fact(fresh, Asset<BlueprintBuff>(cache, "SilenceBuff"), fresh, 8); Run(caster, fresh); Check(saves == before + 2, "silenced listener excluded");
        Check(!ability.GetComponent<AudibleAbilityRestriction>().IsCasterRestrictionPassed(fresh), "silenced howler restriction");
        Set(silence, "IsSuppressed", true); Check(ability.GetComponent<AudibleAbilityRestriction>().IsCasterRestrictionPassed(fresh), "suppressed silence does not block");
        var immunity = Asset<BlueprintBuff>(cache, "TerrifyingHowlImmunity"); Check(immunity.Stacking == StackingType.Stack && target.Buffs.Enumerable.Count(b => b.Blueprint == immunity) == 2, "two howlers retain independent immunity records");
    }
}


