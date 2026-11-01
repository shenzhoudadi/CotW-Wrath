using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;

internal static class RageLauncher
{
    private static int Main(string[] args)
    {
        if (args.Length != 4) return 2;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            foreach (var dir in new[] { args[0], Path.GetDirectoryName(args[1]) }) {
                var file = Path.Combine(dir, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try { typeof(RageLauncher).Assembly.GetType("RageTests").GetMethod("Run").Invoke(null, new object[] { args }); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}

// Standalone managed tests. Native zip data supplies pool references and the
// registration anchors; unrelated actions are inert sentinels, never executed.
// Rank and modifier arithmetic below calls real game implementations.
internal static class RageTests
{
    private static Type module;
    private static int assertions;
    private static string[] features, buffs, pools, hosts;
    private static Dictionary<string, JObject> native;
    private static string Const(string field) => (string)AccessTools.Field(module, field).GetValue(null);
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); assertions++; }
    private static string Id(JToken token) => ((string)token ?? "").Replace("!bp_", "");
    private static object Get(object o, string f) => AccessTools.Field(o.GetType(), f).GetValue(o);
    private static void Set(object o, string f, object v) => AccessTools.Field(o.GetType(), f).SetValue(o, v);
    private static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    private static T Ref<T>(string id) where T : BlueprintReferenceBase, new() { var r = new T(); Set(r, "deserializedGuid", BlueprintGuid.Parse(id)); return r; }
    private static void Bind(BlueprintReferenceBase r, SimpleBlueprint value) => typeof(BlueprintReferenceBase).GetProperty("Cached", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(r, value, null);
    private static void Register(Fixture f) => AccessTools.Method(module, "Register").Invoke(null, new object[] { f.Cache });
    public static void Run(string[] args)
    {
        module = Assembly.LoadFrom(args[1]).GetType("CallOfTheWild.Wrath.Prototype.RagePowers", true);
        features = (string[])AccessTools.Field(module, "FeatureIds").GetValue(null);
        buffs = (string[])AccessTools.Field(module, "BuffIds").GetValue(null);
        pools = (string[])AccessTools.Field(module, "SelectionIds").GetValue(null);
        hosts = (string[])AccessTools.Field(module, "HostIds").GetValue(null);
        Check(features.Length == 7 && buffs.Length == 7 && features.Concat(buffs).Distinct().Count() == 14, "Seven unique stable feature/buff pairs");
        native = new Dictionary<string, JObject>();
        var wanted = new HashSet<string>(pools.Concat(hosts).Concat(new[] { Const("BarbarianId"), Const("SkaldId"), Const("BloodragerId"), Const("PrimalistId"), Const("FearlessId"), Const("FearlessBuffId"), Const("LethalStanceFeatureId"), Const("LethalStanceEffectId") }));
        foreach (var json in JArray.Parse(File.ReadAllText(args[3])).Cast<JObject>()) {
            var id = (string)json["AssetId"];
            if (wanted.Contains(id)) native.Add(id, json);
        }
        Check(native.Count == wanted.Count, "All required assets found in installed game's blueprint archive");
        var f = new Fixture(); Register(f); ValidateRegistered(f);
        var own = features.Concat(buffs).Select(id => f.Load(id)).ToArray();
        var snapshots = f.Snapshots(); Register(f); Register(f);
        Check(own.SequenceEqual(features.Concat(buffs).Select(id => f.Load(id))), "Repeated registration preserves blueprint objects");
        Check(snapshots.SequenceEqual(f.Snapshots()), "Repeated registration does not duplicate options/actions/shared facts");
        TestRank(f); TestModifiers(f);
        var immune = ((BlueprintBuff)f.Load(buffs[1])).ComponentsArray;
        Check(immune.OfType<AddConditionImmunity>().Single().Condition == UnitCondition.Paralyzed, "Condition immunity covers paralysis only");
        Check(immune.OfType<BuffDescriptorImmunity>().Single().Descriptor == SpellDescriptor.Paralysis, "Spell descriptor immunity covers paralysis only");

        foreach (var id in wanted) {
            var broken = new Fixture(); broken.Cache.RemoveCachedBlueprint(BlueprintGuid.Parse(id)); RejectedUnchanged(broken, "Missing dependency " + id);
        }
        f = new Fixture(); f.Load(hosts[0]).name = "Foreign"; RejectedUnchanged(f, "Foreign dependency identity");
        f = new Fixture(); Set(f.Load(Const("PrimalistId")), "m_ParentClass", f.Load(Const("BarbarianId"))); RejectedUnchanged(f, "Wrong archetype parent");
        f = new Fixture(); var bad = new BlueprintFeature { name = "Foreign", AssetGuid = BlueprintGuid.Parse(features[6]) }; f.Cache.AddCachedBlueprint(bad.AssetGuid, bad); RejectedUnchanged(f, "Last feature GUID collision");
        Check(ReferenceEquals(f.Load(features[6]), bad) && features.Take(6).All(id => f.Load(id) == null), "Collision preserves foreign object and adds no earlier assets");
        f = new Fixture(); f.Actions(0).Activated.Actions = new GameAction[0]; RejectedUnchanged(f, "Missing native child anchor");
        f = new Fixture(); f.StanceComponent.Activated.Actions = new GameAction[0]; RejectedUnchanged(f, "Missing deadly accuracy stance anchor");
        f = new Fixture(); Set(f.Share(0), "m_Facts", new BlueprintUnitFactReference[0]); RejectedUnchanged(f, "Changed native sharing list");
        f = new Fixture(); f.Share(0).FeatureFromSelection = true; Set(f.Share(0), "m_Selection", Ref<BlueprintFeatureSelectionReference>(pools[1])); Register(f);
        Check(((BlueprintFeatureReference[])Get(f.Load(pools[1]), "m_AllFeatures")).Count(r => features.Contains(r.Guid.ToString())) == 7, "Selection-based Skald sharing supported");
        f = new Fixture(); Register(f); ((BlueprintBuff)f.Load(buffs[0])).ComponentsArray.OfType<AddStatBonus>().Single().Value = 2; RejectedUnchanged(f, "Existing own blueprint modified");
        f = new Fixture(); Register(f); ((BlueprintBuff)f.Load(Const("SharpenedBuffId"))).ComponentsArray.OfType<RecalculateConcealment>().Single().IgnorePartial = false; RejectedUnchanged(f, "Existing sharpened accuracy effect modified");
        f = new Fixture(); Register(f); var action = f.Actions(4).Activated.Actions.Last() as Conditional;
        ((ContextActionApplyBuff)action.IfTrue.Actions[0]).AsChild = false; RejectedUnchanged(f, "Existing application lost parent ownership");
        f = new Fixture(); Register(f); var selection = (BlueprintFeatureSelection)f.Load(pools[3]);
        Set(selection, "m_AllFeatures", ((BlueprintFeatureReference[])Get(selection, "m_AllFeatures")).Concat(new[] { Ref<BlueprintFeatureReference>(features[0]) }).ToArray()); RejectedUnchanged(f, "Duplicate existing option");
        File.WriteAllText(args[2], "{\"assertionsPassed\":" + assertions + ",\"scope\":\"Production registration using installed blueprint archive anchors; native rank and stat modifier arithmetic; failure isolation and idempotence. No Unity, combat, aura lifetime, save/reload or in-game UI test.\"}");
        Console.WriteLine("PASS: " + assertions + " rage assertions; standalone managed tests, NOT in-game acceptance.");
    }
    private static void RejectedUnchanged(Fixture f, string message)
    {
        var before = f.Snapshots(); var own = features.Concat(buffs).Select(id => f.Load(id)).ToArray();
        try { Register(f); throw new Exception("Expected rejection: " + message); }
        catch (TargetInvocationException e) { Check(e.InnerException is InvalidOperationException, "Controlled rejection: " + message); }
        Check(before.SequenceEqual(f.Snapshots()) && own.SequenceEqual(features.Concat(buffs).Select(id => f.Load(id))), "Failure leaves shared lists/cache unchanged: " + message);
    }
    private static void ValidateRegistered(Fixture f)
    {
        for (int i = 0; i < 7; i++) {
            var feat = (BlueprintFeature)f.Load(features[i]); var buff = (BlueprintBuff)f.Load(buffs[i]);
            Check(feat.Ranks == 1 && feat.Groups.SequenceEqual(new[] { FeatureGroup.RagePower }), "Single rank rage choice " + i);
            Check(feat.ComponentsArray.OfType<PrerequisiteClassLevel>().Count() == 2 && feat.ComponentsArray.OfType<PrerequisiteArchetypeLevel>().Count() == 1, "Three supported eligibility branches " + i);
            Check(feat.ComponentsArray.OfType<Prerequisite>().All(p => p.Group == Prerequisite.GroupType.Any) && feat.ComponentsArray.OfType<PrerequisiteClassLevel>().All(p => p.Level == (i == 1 ? 12 : 1)) && feat.ComponentsArray.OfType<PrerequisiteArchetypeLevel>().Single().Level == (i == 1 ? 12 : 1), "Correct level gate " + i);
            Check(buff.Stacking == StackingType.Stack && buff.IsHiddenInUI && buff.ComponentsArray.All(c => ReferenceEquals(c.OwnerBlueprint, buff)), "Independent parent-owned effect instances " + i);
        }
        foreach (var id in pools) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) {
            var values = (BlueprintFeatureReference[])Get(f.Load(id), field); var original = native[id]["Data"][field].Select(Id).ToArray();
            Check(values.Take(original.Length).Select(r => r.Guid.ToString()).SequenceEqual(original), "Existing native choice order preserved " + id + field);
            Check(features.All(fId => values.Count(r => r.Guid.ToString() == fId) == 1), "All new choices exactly once " + id + field);
        }
        for (int h = 0; h < 5; h++) {
            var actions = f.Actions(h).Activated.Actions;
            Check(f.OriginalActions[h].SequenceEqual(actions.Take(f.OriginalActions[h].Length)), "Original activation actions preserved " + h);
            for (int i = 0; i < 7; i++) {
                var c = (Conditional)actions[f.OriginalActions[h].Length + i]; var condition = c.ConditionsChecker.Conditions.Single(); var a = (ContextActionApplyBuff)c.IfTrue.Actions.Single();
                Check(condition.GetType() == (h >= 3 ? typeof(ContextConditionCasterHasFact) : typeof(ContextConditionHasFact)) && ((BlueprintReferenceBase)Get(condition, "m_Fact")).Guid.ToString() == features[i], "Power ownership target vs song caster " + h + "/" + i);
                Check(((BlueprintReferenceBase)Get(a, "m_Buff")).Guid.ToString() == buffs[i] && a.AsChild && a.Permanent && a.IsNotDispelable && !a.ToCaster && !a.IgnoreParentContext, "Child buff lifetime and source context " + h + "/" + i);
            }
        }
        for (int h = 0; h < 2; h++) {
            var facts = (BlueprintUnitFactReference[])Get(f.Share(h), "m_Facts");
            Check(features.All(id => facts.Count(r => r.Guid.ToString() == id) == 1) && f.OriginalShared[h].SequenceEqual(facts.Take(f.OriginalShared[h].Length)), "Skald sharing preserves prior facts and grants new choices once");
        }
        var basic = (BlueprintFeatureSelection)f.Load("247a4068296e8be42890143f451b4b45");
        Check(!((BlueprintFeatureReference[])Get(basic, "m_AllFeatures")).Any(r => features.Contains(r.Guid.ToString()) || r.Guid.ToString() == Const("SharpenedFeatureId")), "Rage powers not added to ordinary feats");
        var sharpened = (BlueprintFeature)f.Load(Const("SharpenedFeatureId"));
        var effect = (BlueprintBuff)f.Load(Const("SharpenedBuffId"));
        Check(sharpened.Ranks == 1 && sharpened.Groups.SequenceEqual(new[] { FeatureGroup.RagePower }), "Sharpened Accuracy is one rage choice");
        Check(sharpened.ComponentsArray.OfType<PrerequisiteClassLevel>().All(p => p.Group == Prerequisite.GroupType.Any && p.Level == 8) && sharpened.ComponentsArray.OfType<PrerequisiteArchetypeLevel>().Single().Level == 8, "Sharpened Accuracy requires level 8");
        var required = sharpened.ComponentsArray.OfType<PrerequisiteFeature>().Single();
        Check(required.Group == Prerequisite.GroupType.All && ((BlueprintReferenceBase)Get(required, "m_Feature")).Guid.ToString() == Const("LethalStanceFeatureId"), "Sharpened Accuracy requires lethal stance");
        var concealment = effect.ComponentsArray.OfType<RecalculateConcealment>().Single();
        Check(effect.Stacking == StackingType.Stack && effect.IsHiddenInUI && concealment.IgnorePartial && concealment.TreatTotalAsPartial, "Native concealment step-down while the effect is active");
        foreach (var id in pools) Check(((BlueprintFeatureReference[])Get(f.Load(id), "m_AllFeatures")).Count(r => r.Guid.ToString() == Const("SharpenedFeatureId")) == 1, "Sharpened Accuracy once in each rage pool");
        for (int h = 0; h < 5; h++) Check(!f.Actions(h).Activated.Actions.Any(a => a.name != null && a.name.Contains("SharpenedAccuracy")), "Sharpened Accuracy is not a rage-host effect");
        var stance = f.StanceComponent.Activated.Actions;
        Check(f.OriginalStanceActions.SequenceEqual(stance.Take(f.OriginalStanceActions.Length)), "Lethal stance actions stay ahead of the new rider");
        var rider = (Conditional)stance.Last();
        var riderApply = (ContextActionApplyBuff)rider.IfTrue.Actions.Single();
        Check(rider.ConditionsChecker.Conditions.Single() is ContextConditionHasFact && ((BlueprintReferenceBase)Get(rider.ConditionsChecker.Conditions.Single(), "m_Fact")).Guid.ToString() == Const("SharpenedFeatureId"), "Stance rider checks the owner, not the skald caster");
        Check(((BlueprintReferenceBase)Get(riderApply, "m_Buff")).Guid.ToString() == Const("SharpenedBuffId") && riderApply.AsChild && riderApply.Permanent && !riderApply.IsNotDispelable && !riderApply.ToCaster, "Stance child buff follows the native Deadly Accuracy lifetime");
        for (int h = 0; h < 2; h++) Check(((BlueprintUnitFactReference[])Get(f.Share(h), "m_Facts")).Count(r => r.Guid.ToString() == Const("SharpenedFeatureId")) == 1, "Inspired rage can share the stance power");
    }
    private static void TestRank(Fixture f)
    {
        var classes = new[] { Const("BarbarianId"), Const("SkaldId"), Const("BloodragerId") }.Select(id => new ClassData((BlueprintCharacterClass)f.Load(id))).ToArray();
        var wizard = new ClassData(new BlueprintCharacterClass { name = "Wizard", AssetGuid = BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e") }) { Level = 20 };
        var unit = Empty<UnitEntityData>(); var descriptor = Empty<UnitDescriptor>(); var progression = Empty<UnitProgressionData>();
        Set(unit, "<Descriptor>k__BackingField", descriptor); Set(descriptor, "<Unit>k__BackingField", unit); Set(descriptor, "Progression", progression);
        Set(progression, "Classes", classes.Concat(new[] { wizard }).ToList());
        var context = Empty<MechanicsContext>(); var proxy = FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy", BindingFlags.Public | BindingFlags.NonPublic));
        Set(proxy, "<Entity>k__BackingField", unit); object caster = default(EntityRef<UnitEntityData>); Set(caster, "m_Proxy", proxy); Set(context, "m_CasterRef", caster); Set(context, "m_Params", new AbilityParams { RankBonus = 10 });
        var baseValue = AccessTools.Method(typeof(ContextRankConfig), "GetBaseValue"); var progress = AccessTools.Method(typeof(ContextRankConfig), "ApplyProgression"); var minmax = AccessTools.Method(typeof(ContextRankConfig), "ApplyMinMax");
        var energies = new[] { DamageEnergyType.Acid, DamageEnergyType.Cold, DamageEnergyType.Fire, DamageEnergyType.Electricity, DamageEnergyType.Sonic };
        for (int i = 0; i < 5; i++) {
            var components = ((BlueprintBuff)f.Load(buffs[i + 2])).ComponentsArray; var rank = components.OfType<ContextRankConfig>().Single();
            foreach (var r in (BlueprintCharacterClassReference[])Get(rank, "m_Class")) Bind(r, f.Load(r.Guid.ToString()));
            var arch = (BlueprintReferenceBase)Get(rank, "Archetype"); Bind(arch, f.Load(arch.Guid.ToString()));
            var resistance = components.OfType<AddDamageResistanceEnergy>().Single();
            Check(resistance.Type == energies[i] && resistance.Value.ValueType == ContextValueType.Rank && resistance.Value.ValueRank == AbilityRankType.Default && !resistance.UsePool, "Matching native energy resistance, unlimited pool " + energies[i]);
            Func<int> value = () => (int)minmax.Invoke(rank, new object[] { (int)progress.Invoke(rank, new object[] { (int)baseValue.Invoke(rank, new object[] { context }) }), false, context });
            foreach (var cls in classes) cls.Level = 0;
            for (int c = 0; c < 3; c++) {
                classes[2].Archetypes.Clear(); classes[2].Archetypes.Add((BlueprintArchetype)f.Load(Const("PrimalistId")));
                foreach (var pair in new[] { new[] { 1, 1 }, new[] { 2, 1 }, new[] { 3, 1 }, new[] { 4, 2 }, new[] { 11, 5 }, new[] { 12, 6 }, new[] { 20, 10 } }) {
                    foreach (var cls in classes) cls.Level = 0; classes[c].Level = pair[0];
                    Check(value() == pair[1], "Native half-level/minimum for " + energies[i] + "/class" + c + "/level" + pair[0]);
                }
            }
            classes[0].Level = 4; classes[1].Level = 3; classes[2].Level = 5;
            Check(value() == 6, "Eligible multiclass levels sum before rounding; unrelated Wizard20/rank bonus excluded");
            classes[2].Archetypes.Clear(); Check(value() == 3, "Ordinary Bloodrager levels excluded");
            classes[0].Level = 0; classes[1].Level = 0; Check((int)baseValue.Invoke(rank, new object[] { context }) == 0, "No eligible levels from ordinary Bloodrager or Wizard");
        }
    }
    private static void TestModifiers(Fixture f)
    {
        // Only disable Unity profiling, which cannot initialize in a console
        // process. Native modifier aggregation/removal remains unpatched.
        var harmony = new Harmony("CotW.RageTests.ProfilingOnly");
        harmony.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope), "New", new[] { typeof(string), typeof(UnityEngine.Object) }),
            prefix: new HarmonyMethod(AccessTools.Method(typeof(RageTests), nameof(SkipProfiling))));
        try {
        var c = ((BlueprintBuff)f.Load(buffs[0])).ComponentsArray.OfType<AddStatBonus>().Single();
        Check(c.Stat == StatType.AttackOfOpportunityCount && c.Value == 1, "One additional opportunity");
        var stat = new ModifiableValue(null, c.Stat) { BaseValue = 1 };
        var first = stat.AddModifier(c.Value, Empty<Feature>(), c.Descriptor); Check(stat.ModifiedValue == 2, "One rage source grants one AoO");
        var second = stat.AddModifier(c.Value, Empty<Feature>(), c.Descriptor); Check(stat.ModifiedValue == 2, "Two rage sources do not double count");
        var combat = stat.AddModifier(4, Empty<Feature>(), ModifierDescriptor.UntypedStackable); Check(stat.ModifiedValue == 6, "Other stackable AoO bonuses remain");
        stat.RemoveModifier(first); Check(stat.ModifiedValue == 6, "Removing one source retains another");
        stat.RemoveModifier(second); Check(stat.ModifiedValue == 5, "Removing last source removes only rage bonus");
        stat.RemoveModifier(combat); Check(stat.ModifiedValue == 1, "Native modifier lifecycle returns to baseline");
        } finally { harmony.UnpatchAll("CotW.RageTests.ProfilingOnly"); }
    }
    private static bool SkipProfiling() => false;
    private sealed class Fixture
    {
        public readonly BlueprintsCache Cache;
        public readonly GameAction[][] OriginalActions = new GameAction[5][];
        public readonly BlueprintUnitFactReference[][] OriginalShared = new BlueprintUnitFactReference[2][];
        public GameAction[] OriginalStanceActions;
        public AddFactContextActions StanceComponent;
        public SimpleBlueprint Load(string id) => Cache.Load(BlueprintGuid.Parse(id));
        public readonly AddFactContextActions[] AnchorComponents = new AddFactContextActions[5];
        public AddFactContextActions Actions(int h) => AnchorComponents[h];
        public AddFactsFromCaster Share(int h) => ((BlueprintBuff)Load(hosts[h + 3])).ComponentsArray.OfType<AddFactsFromCaster>().Single();
        public Fixture()
        {
            object[] output = { null }; Cache = (BlueprintsCache)AccessTools.Method(typeof(BlueprintTests), "FullCache").Invoke(null, output);
            foreach (var id in native.Keys) {
                SimpleBlueprint bp = pools.Contains(id) ? (SimpleBlueprint)new BlueprintFeatureSelection() : hosts.Contains(id) || id == Const("FearlessBuffId") || id == Const("LethalStanceEffectId") ? new BlueprintBuff() : id == Const("PrimalistId") ? new BlueprintArchetype() : id == Const("FearlessId") || id == Const("LethalStanceFeatureId") ? new BlueprintFeature() : (SimpleBlueprint)new BlueprintCharacterClass();
                bp.AssetGuid = BlueprintGuid.Parse(id); bp.name = (string)native[id]["TestName"]; Cache.AddCachedBlueprint(bp.AssetGuid, bp);
            }
            foreach (var id in new[] { Const("BarbarianId"), Const("SkaldId"), Const("BloodragerId") }) {
                var refs = native[id]["Data"]["m_Archetypes"].Select(t => Ref<BlueprintArchetypeReference>(Id(t))).ToArray();
                foreach (var r in refs) Bind(r, Load(r.Guid.ToString()) ?? new BlueprintArchetype { AssetGuid = r.Guid });
                Set(Load(id), "m_Archetypes", refs);
            }
            Set(Load(Const("PrimalistId")), "m_ParentClass", Load(Const("BloodragerId")));
            foreach (var id in pools) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) Set(Load(id), field, native[id]["Data"][field].Select(t => Ref<BlueprintFeatureReference>(Id(t))).ToArray());
            for (int h = 0; h < 5; h++) {
                var data = native[hosts[h]]["Data"];
                var components = new List<BlueprintComponent>();
                foreach (var source in data["Components"].Where(c => ((string)c["$type"]).EndsWith(", AddFactContextActions"))) {
                var actions = source["Activated"]["Actions"].Select(a => {
                    var condition = a["ConditionsChecker"]?["Conditions"]?.FirstOrDefault();
                    if (Id(condition?["m_Fact"]) != Const("FearlessId")) return (GameAction)new ContextActionRemoveBuff { name = (string)a["name"] };
                    Condition check = ((string)condition["$type"]).EndsWith(", ContextConditionCasterHasFact") ? (Condition)new ContextConditionCasterHasFact() : new ContextConditionHasFact();
                    check.Not = (bool)condition["Not"]; Set(check, "m_Fact", Ref<BlueprintUnitFactReference>(Id(condition["m_Fact"])));
                    var raw = a["IfTrue"]["Actions"].Single(); var apply = new ContextActionApplyBuff { AsChild = (bool)raw["AsChild"], Permanent = (bool)raw["Permanent"], ToCaster = (bool)raw["ToCaster"] }; Set(apply, "m_Buff", Ref<BlueprintBuffReference>(Id(raw["m_Buff"])));
                    return new Conditional { name = (string)a["name"], ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new[] { check } }, IfTrue = new ActionList { Actions = new GameAction[] { apply } }, IfFalse = new ActionList { Actions = new GameAction[0] } };
                }).ToArray();
                var ac = new AddFactContextActions { name = (string)source["name"], Activated = new ActionList { Actions = actions } };
                components.Add(ac);
                if (source.ToString().Contains(Const("FearlessId"))) { OriginalActions[h] = actions; AnchorComponents[h] = ac; }
                }
                if (h >= 3) {
                    var raw = data["Components"].Single(c => ((string)c["$type"]).EndsWith(", AddFactsFromCaster"));
                    var share = new AddFactsFromCaster { FeatureFromSelection = (bool)raw["FeatureFromSelection"] };
                    var refs = raw["m_Facts"].Select(t => Ref<BlueprintUnitFactReference>(Id(t))).ToArray(); OriginalShared[h - 3] = refs;
                    Set(share, "m_Facts", refs); components.Add(share);
                }
                ((BlueprintBuff)Load(hosts[h])).ComponentsArray = components.ToArray();
            }
            var stanceData = native[Const("LethalStanceEffectId")]["Data"];
            var stanceSource = stanceData["Components"].Single(c => ((string)c["$type"]).EndsWith(", AddFactContextActions"));
            var stanceActions = stanceSource["Activated"]["Actions"].Select(a => {
                var condition = a["ConditionsChecker"]?["Conditions"]?.FirstOrDefault();
                if (Id(condition?["m_Fact"]) != Const("DeadlyAccuracyId")) return (GameAction)new ContextActionRemoveBuff { name = (string)a["name"] };
                var check = new ContextConditionHasFact { Not = (bool)condition["Not"] };
                Set(check, "m_Fact", Ref<BlueprintUnitFactReference>(Id(condition["m_Fact"])));
                var raw = a["IfTrue"]["Actions"].Single();
                var apply = new ContextActionApplyBuff { AsChild = (bool)raw["AsChild"], Permanent = (bool)raw["Permanent"], ToCaster = (bool)raw["ToCaster"] };
                Set(apply, "m_Buff", Ref<BlueprintBuffReference>(Id(raw["m_Buff"])));
                return new Conditional { name = (string)a["name"], ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new[] { check } }, IfTrue = new ActionList { Actions = new GameAction[] { apply } }, IfFalse = new ActionList { Actions = new GameAction[0] } };
            }).ToArray();
            OriginalStanceActions = stanceActions;
            StanceComponent = new AddFactContextActions { name = (string)stanceSource["name"], Activated = new ActionList { Actions = stanceActions } };
            ((BlueprintBuff)Load(Const("LethalStanceEffectId"))).ComponentsArray = new BlueprintComponent[] { StanceComponent };
        }
        public string[] Snapshots()
        {
            var snapshots = new List<string>();
            foreach (var id in pools.Where(id => Load(id) != null)) foreach (var field in new[] { "m_Features", "m_AllFeatures" }) snapshots.Add(id + field + string.Join(",", ((BlueprintFeatureReference[])Get(Load(id), field)).Select(r => r.Guid)));
            for (int h = 0; h < 5; h++) if (Load(hosts[h]) != null) snapshots.Add(hosts[h] + string.Join(",", Actions(h).Activated.Actions.Select(a => a.name)));
            for (int h = 0; h < 2; h++) if (Load(hosts[h + 3]) != null) snapshots.Add(string.Join(",", ((BlueprintUnitFactReference[])Get(Share(h), "m_Facts")).Select(r => r.Guid)));
            if (Load(Const("LethalStanceEffectId")) != null && StanceComponent != null) snapshots.Add("stance" + string.Join(",", StanceComponent.Activated.Actions.Select(a => a.name)));
            return snapshots.ToArray();
        }
    }
}
