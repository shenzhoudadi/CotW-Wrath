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
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class KineticUtility01433Tests
{
    static int checks, serial, saves, applications, added, removed, burnNotifications;
    static bool appendFault, bypassEngine;
    static BlueprintGuid faultRoot;
    static RulebookEventContext current;
    static int d20 = 1, d4 = 1;
    static Type Module => typeof(KineticColdPenalty01433).Assembly.GetType("CallOfTheWild.Wrath.Prototype.KineticUtility01433", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Helper(Type t, string n, params object[] args) => AccessTools.Method(t, n).Invoke(null, args);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Helper(typeof(Migration01426Tests), "Id", token);
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static void Register(BlueprintsCache cache, string method) => AccessTools.Method(Module, method).Invoke(null, new object[] { cache });
    static string Definition(object o) => (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition").Invoke(null, new[] { o });
    static void Check(bool value, string reason) { if (!value) throw new Exception("KineticUtility01433: " + reason); checks++; }
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool NoController(ref LevelUpController __result) { __result = null; return false; }
    static Kingmaker.Settings.StatsAdjustmentsType NormalDifficulty() => Kingmaker.Settings.StatsAdjustmentsType.None;
    static IEnumerable<CodeInstruction> BuffDifficultyBoundary(IEnumerable<CodeInstruction> input) {
        var code = input.ToList(); int start = code.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(Kingmaker.Settings.SettingsRoot) && m.Name == "get_Difficulty");
        int end = code.FindIndex(start, i => i.operand is MethodInfo m && m.Name == "op_Implicit");
        if (start < 0 || end < start || end - start > 4) throw new Exception("Current native buff difficulty chain changed");
        var replacement = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KineticUtility01433Tests), nameof(NormalDifficulty))); replacement.labels.AddRange(code[start].labels);
        code.RemoveRange(start, end - start + 1); code.Insert(start, replacement); return code;
    }
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static void AppendFault(BlueprintFeature[] feats) { if (appendFault && feats.Any(f => f.AssetGuid == faultRoot)) throw new InvalidOperationException("01433 kinetic append fault"); }
    static UnitEntityData Unit(BlueprintsCache cache, string name, int level = 6) => (UnitEntityData)Helper(typeof(PurifyingFlames01428Tests), "Unit", cache, "01433-" + name,
        (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("42a455d9ec1ad924d889272429eb8391")), level);
    static Feature Fact(UnitEntityData unit, BlueprintFeature bp) => (Feature)Helper(typeof(Migration01426Tests), "Fact", unit, bp);
    static bool CanSelect(UnitEntityData unit, BlueprintFeatureSelection pool, BlueprintFeature candidate) => (bool)Helper(typeof(EnhancementLifecycle01430Tests), "CanSelect", unit, pool, candidate);
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection pool, string field) => (BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>();
    static BlueprintsCache Fresh(BlueprintsCache source, string prefix, BlueprintFeatureSelection pool) {
        var fresh = Empty<BlueprintsCache>();
        foreach (var field in typeof(BlueprintsCache).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) field.SetValue(fresh, field.GetValue(source));
        var loaded = (IDictionary)Get(source, "m_LoadedBlueprints"); var copy = (IDictionary)Activator.CreateInstance(loaded.GetType());
        foreach (var key in loaded.Keys.Cast<BlueprintGuid>().ToArray()) if (!source.Load(key).name.StartsWith("CotWWrathPsychic" + prefix, StringComparison.Ordinal)) copy.Add(key, loaded[key]);
        Set(fresh, "m_LoadedBlueprints", copy);
        var privatePool = (BlueprintFeatureSelection)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(pool, null);
        foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(privatePool, field, Refs(pool, field).Where(r => source.Load(r.Guid)?.name?.StartsWith("CotWWrathPsychic" + prefix, StringComparison.Ordinal) != true).ToArray());
        fresh.AddCachedBlueprint(privatePool.AssetGuid, privatePool); return fresh;
    }
    static void Registration(BlueprintsCache cache, Harmony h)
    {
        foreach (var id in (string[])AccessTools.Field(Module, "Dependencies").GetValue(null)) {
            var row = PsychicExpansionTests.NativeRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id);
            var bp = cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject;
            // Earlier suites preseed reference-only dependency shells. Hydrate
            // only those exact empty component shells from current game JSON;
            // real donor graphs remain untouched and are asserted below.
            if (row != null && bp != null && bp.ComponentsArray.Length == 0 && ((JArray)row["blueprint"]["Data"]["Components"]).Count > 0) {
                bp.ComponentsArray = ((JArray)row["blueprint"]["Data"]["Components"]).Select(c => (BlueprintComponent)PsychicExpansionTests.Read(c, typeof(BlueprintComponent))).ToArray();
                bp.OnEnable();
            }
        }
        var pool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("5c883ae0cd6d7d5448b7a420f51f8459"));
        var modules = new[] { "RegisterWindsight", "RegisterColdSnap", "RegisterSmokeStorm" };
        var tokens = new[] { "WindsightWildTalent", "ColdSnapWildTalent", "SmokeStormWildTalent" };
        var ids = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray();
        var donors = ((string[])AccessTools.Field(Module, "Dependencies").GetValue(null)).Distinct().Select(x => cache.Load(BlueprintGuid.Parse(x))).OfType<BlueprintScriptableObject>().ToArray();
        var donorDefinitions = donors.Select(x => Definition(x.ComponentsArray)).ToArray();
        var oldPools = new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") };
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(AppendFault)));
        for (int index = 0; index < modules.Length; index++) {
            Register(cache, modules[index]);
            var root = Asset<BlueprintFeature>(cache, tokens[index]);
            Check(root.IsClassFeature && root.HasGroup(FeatureGroup.KineticWildTalent), "wild-talent root has actual native selection group " + tokens[index]);
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                Check(Refs(pool, field).Count(r => r.Guid == root.AssetGuid) == 1, "unique root in actual " + field);
                var prior = (BlueprintFeatureReference[])oldPools[field == "m_Features" ? 0 : 1];
                Check(Refs(pool, field).Take(prior.Length).SequenceEqual(prior), "original native reference order retained " + field);
            }
            var now = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray().Select(cache.Load).OfType<BlueprintScriptableObject>()
                .Where(b => b.name.StartsWith("CotWWrathPsychic" + (index == 0 ? "Windsight" : index == 1 ? "ColdSnap" : "SmokeStorm"), StringComparison.Ordinal)).ToArray();
            var fresh = Fresh(cache, index == 0 ? "Windsight" : index == 1 ? "ColdSnap" : "SmokeStorm", pool);
            var freshPool = (BlueprintFeatureSelection)fresh.Load(pool.AssetGuid);
            var failurePools = new[] { Get(freshPool, "m_Features"), Get(freshPool, "m_AllFeatures") };
            var before = new HashSet<BlueprintGuid>(((IDictionary)Get(fresh, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>());
            bool rejected = false; faultRoot = root.AssetGuid; appendFault = true;
            try { Register(fresh, modules[index]); } catch (TargetInvocationException e) { rejected = e.InnerException is InvalidOperationException && e.InnerException.Message == "01433 kinetic append fault"; }
            finally { appendFault = false; }
            Check(rejected, "fresh module reaches native append failure " + tokens[index]);
            Check(before.SetEquals(((IDictionary)Get(fresh, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>()), "all staged identities roll back " + tokens[index]);
            Check(ReferenceEquals(failurePools[0], Get(freshPool, "m_Features")) && ReferenceEquals(failurePools[1], Get(freshPool, "m_AllFeatures")), "both actual pool arrays roll back " + tokens[index]);
            Check(ReferenceEquals(root, Asset<BlueprintFeature>(cache, tokens[index])), "isolated fresh-cache failure preserves parent published graph");
            Register(cache, modules[index]); Check(ReferenceEquals(root, Asset<BlueprintFeature>(cache, tokens[index])), "repeat preserves real published root " + tokens[index]);
            var focus = root.GetComponent<PrerequisiteFeaturesFromList>();
            foreach (var element in focus.Features) {
                var unit = Unit(cache, tokens[index] + element.AssetGuid, 5); Fact(unit, element);
                if (index == 1) { Fact(unit, (BlueprintFeature)cache.Load(BlueprintGuid.Parse("1ff5d6e76b7c2fa48be555b77d1ad8b2"))); Fact(unit, (BlueprintFeature)cache.Load(BlueprintGuid.Parse("29ec36fa2a5b8b94ebce170bd369083a"))); }
                Check(!CanSelect(unit, pool, root), "native selection rejects level5 " + tokens[index]);
                unit.Progression.Classes[0].Level = 6; Check(CanSelect(unit, pool, root), "native selection accepts each actual elemental focus at6 " + tokens[index]);
                Fact(unit, root); Check(!CanSelect(unit, pool, root), "native duplicate exclusion " + tokens[index]);
            }
            var wrong = Unit(cache, "wrong-" + tokens[index]); Check(!CanSelect(wrong, pool, root), "wrong element rejected " + tokens[index]);
            if (index == 1) {
                Fact(wrong, focus.Features[0]); Check(!CanSelect(wrong, pool, root), "cold adaptation and shroud both required");
                Fact(wrong, (BlueprintFeature)cache.Load(BlueprintGuid.Parse("1ff5d6e76b7c2fa48be555b77d1ad8b2")));
                Check(!CanSelect(wrong, pool, root), "shroud remains required");
                Fact(wrong, (BlueprintFeature)cache.Load(BlueprintGuid.Parse("29ec36fa2a5b8b94ebce170bd369083a")));
                Check(CanSelect(wrong, pool, root), "all cold prerequisites admit choice");
            }
        }
        Check(donorDefinitions.SequenceEqual(donors.Select(x => Definition(x.ComponentsArray))), "all native elemental, type and burn donors remain byte-definition equal");
        var sight = Asset<BlueprintFeature>(cache, "WindsightWildTalent");
        var sourceAir = (BlueprintFeature)cache.Load(BlueprintGuid.Parse("b182f559f572da342b54bece4404e4e7"));
        var originalAir = sourceAir.ComponentsArray; var ownSight = sight.ComponentsArray;
        try {
            sourceAir.ComponentsArray = originalAir.Concat(new BlueprintComponent[] { new AddFacts { name = "01433-late-native-donor-mutation" } }).ToArray();
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(pool, field, Refs(pool, field).Where(r => r.Guid != sight.AssetGuid).ToArray());
            Register(cache, "RegisterWindsight");
            Check(ReferenceEquals(sight, Asset<BlueprintFeature>(cache, "WindsightWildTalent")) && ReferenceEquals(ownSight, sight.ComponentsArray), "optional retry validates existing private graph without recloning mutable native donor");
            Check(Refs(pool, "m_Features").Count(r => r.Guid == sight.AssetGuid) == 1 && Refs(pool, "m_AllFeatures").Count(r => r.Guid == sight.AssetGuid) == 1, "late rebuilt native pool is reappended exactly once");
        } finally { sourceAir.ComponentsArray = originalAir; }
        var missing = Asset<BlueprintBuff>(cache, "ColdSnapEnabledBuff"); cache.RemoveCachedBlueprint(missing.AssetGuid);
        bool missingRejected = false; try { Register(cache, "RegisterColdSnap"); } catch (TargetInvocationException e) { missingRejected = e.InnerException is InvalidOperationException && e.InnerException.Message.Contains("published identity"); }
        finally { cache.AddCachedBlueprint(missing.AssetGuid, missing); }
        Check(missingRejected && ReferenceEquals(missing, Asset<BlueprintBuff>(cache, "ColdSnapEnabledBuff")), "partial owned graph rejects refresh without synthesizing replacement assets");
        var cold = Asset<BlueprintAbility>(cache, "ColdSnapEnableAbility");
        var smoke = Asset<BlueprintAbility>(cache, "SmokeStormAbility");
        Check(cold.Type == AbilityType.SpellLike && cold.ActionType == UnitCommand.CommandType.Standard && cold.Range == AbilityRange.Personal && cold.AvailableMetamagic == 0, "cold native ability action/targets/metamagic");
        Check(smoke.Type == AbilityType.SpellLike && smoke.ActionType == UnitCommand.CommandType.Standard && smoke.Range == AbilityRange.Medium && smoke.CanTargetPoint && smoke.AvailableMetamagic == 0 && !smoke.SpellResistance, "smoke native ability action/targets/SR/metamagic");
        var toggle = Asset<BlueprintActivatableAbility>(cache, "ColdSnapToggle");
        Check(toggle.ActivationType == AbilityActivationType.WithUnitCommand && (UnitCommand.CommandType)Get(toggle, "m_ActivateWithUnitCommand") == UnitCommand.CommandType.Swift, "cold toggle actually uses native Swift command");
        Check(Asset<BlueprintAbilityAreaEffect>(cache, "ColdSnapArea").Size.Value == 5 && Asset<BlueprintAbilityAreaEffect>(cache, "SmokeStormArea").Size.Value == 20, "native radii exactly5 and20 feet");
        Check(!string.IsNullOrEmpty(Asset<BlueprintAbilityAreaEffect>(cache, "SmokeStormArea").Fx.AssetId), "smoke keeps actual native mist FX dispatch reference");
    }
    static bool False(ref bool __result) { __result = false; return false; }
    static bool Engine(UnitEntityData target, ref Concealment __result) {
        if (!bypassEngine) return true;
        var list = (List<UnitPartConcealment.ConcealmentEntry>)Get(target.Get<UnitPartConcealment>(), "m_Concealments");
        __result = list.Count == 0 ? Concealment.None : list.Max(e => e.Concealment); return false;
    }
    static bool Corpulence(ref float __result) { __result = 0; return false; }
    static bool View(ref Kingmaker.View.UnitEntityView __result) { __result = Empty<Kingmaker.View.UnitEntityView>(); return false; }
    static void Fault() => throw new InvalidOperationException("01433 fog exception");
    static void ConcealmentCases(BlueprintsCache cache, Harmony h)
    {
        var patch = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicSynesthesiaConcealmentPatch");
        var hp = new Harmony("CotW01433.ProductionConcealment"); hp.CreateClassProcessor(patch).Patch();
        var replacement = new Harmony("TabletopTweaks-Base"); replacement.Patch(AccessTools.Method(typeof(UnitPartConcealment), "Calculate"), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(Engine)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.View.UnitEntityView), "Corpulence"), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(Corpulence)));
        h.Patch(typeof(UnitEntityData).GetProperty("View", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).GetGetMethod(), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(View)));
        try {
            foreach (bool replacementEngine in new[] { false, true }) foreach (bool withFeat in new[] { false, true }) foreach (bool outgoing in new[] { false, true }) {
                bypassEngine = replacementEngine;
                var initiator = Unit(cache, "sight"); var target = Unit(cache, "fog-target");
                foreach (var u in new[] { initiator, target }) Set(u, "View", Empty<Kingmaker.View.UnitEntityView>());
                if (withFeat) Fact(initiator, Asset<BlueprintFeature>(cache, "WindsightWildTalent"));
                if (outgoing) Psychic034Tests.Fact(initiator, Asset<BlueprintBuff>(cache, "ObscuringMistBuff"), target, 6);
                var part = target.Ensure<UnitPartConcealment>();
                var fog = new UnitPartConcealment.ConcealmentEntry { Descriptor = ConcealmentDescriptor.Fog, Concealment = Concealment.Total };
                var blur = new UnitPartConcealment.ConcealmentEntry { Descriptor = ConcealmentDescriptor.Blur, Concealment = Concealment.Partial };
                var displacement = new UnitPartConcealment.ConcealmentEntry { Descriptor = ConcealmentDescriptor.Displacement, Concealment = Concealment.Total };
                part.AddConcealment(blur); part.AddConcealment(fog);
                var list = (List<UnitPartConcealment.ConcealmentEntry>)Get(part, "m_Concealments"); var before = list.ToArray();
                Check(UnitPartConcealment.Calculate(initiator, target, true) == (withFeat ? Concealment.Partial : Concealment.Total), "actual native and replacement calculations filter only fog");
                Check(list.SequenceEqual(before), "fog and blur exact reference order restored");
                part.AddConcealment(displacement); before = list.ToArray();
                Check(UnitPartConcealment.Calculate(initiator, target, true) == Concealment.Total, "displacement remains total");
                Check(list.SequenceEqual(before), "all reference ordering retained after calculation");
                if (withFeat) {
                    var begin = AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicOutgoingFog"), "Begin");
                    using ((IDisposable)begin.Invoke(null, new object[] { initiator, target })) {
                        Check(!list.Any(e => e.Descriptor == ConcealmentDescriptor.Fog), "outer scope filters fog");
                        using ((IDisposable)begin.Invoke(null, new object[] { initiator, target })) Check(!list.Any(e => e.Descriptor == ConcealmentDescriptor.Fog), "nested scope keeps fog filtered");
                        Check(!list.Any(e => e.Descriptor == ConcealmentDescriptor.Fog), "inner disposal does not restore outer fog");
                    }
                    Check(list.SequenceEqual(before), "nested scope restores precise original order");
                }
                var error = new Harmony("CotW01433.FogFault"); error.Patch(AccessTools.Method(typeof(UnitPartConcealment), "Calculate"), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(Fault)) { priority = Priority.Last });
                bool threw = false; try { UnitPartConcealment.Calculate(initiator, target, true); } catch (InvalidOperationException) { threw = true; } finally { error.UnpatchAll(error.Id); }
                Check(threw && list.SequenceEqual(before), "native exception propagates with no persistent concealment mutation");
            }
            bypassEngine = false;
            var same = Unit(cache, "same-sight"); Set(same, "View", Empty<Kingmaker.View.UnitEntityView>()); Fact(same, Asset<BlueprintFeature>(cache, "WindsightWildTalent"));
            var samePart = same.Ensure<UnitPartConcealment>(); var entry = new UnitPartConcealment.ConcealmentEntry { Descriptor = ConcealmentDescriptor.Fog, Concealment = Concealment.Total }; samePart.AddConcealment(entry);
            Check(UnitPartConcealment.Calculate(same, same) == Concealment.None, "same-unit fog filtering");
            Check(((List<UnitPartConcealment.ConcealmentEntry>)Get(samePart, "m_Concealments")).Single() == entry, "same-unit calculation restores fog reference");
        } finally { hp.UnpatchAll(hp.Id); replacement.UnpatchAll(replacement.Id); bypassEngine = false; }
    }
    static bool Time(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static bool UniqueId(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "01433-fact-" + ++serial); return true; }
    sealed class Observer : IUnitBuffHandler, IKineticistGlobalHandler {
        public void HandleBuffDidAdded(Buff b) { added++; }
        public void HandleBuffDidRemoved(Buff b) { removed++; }
        public void HandleKineticistBurnValueChanged(UnitPartKineticist p, int prior, AbilityData a) { burnNotifications++; }
    }
    static Buff Add(UnitEntityData owner, BlueprintBuff bp, UnitEntityData caster) {
        var context = new MechanicsContext(caster, owner.Descriptor, bp); context.SetParams(new AbilityParams { CasterLevel = 6, DC = 17 });
        var buff = new Buff(bp, context, null); var result = owner.Facts.Add(buff);
        Check(ReferenceEquals(result, buff) && buff.IsTurnedOn && buff.IsSubscribedOnEventBus, "actual native manager attaches and subscribes " + bp.name);
        return buff;
    }
    static UnitEntityData Live(BlueprintsCache cache, string label) {
        var unit = (UnitEntityData)Helper(typeof(FlameJetLifecycle01430Tests), "Unit", cache, "01433-" + label);
        Set(unit, "m_IsInGame", true); Set(unit, "HoldingState", Empty<SceneEntitiesState>()); return unit;
    }
    static UnitPartKineticist Kineticist(UnitEntityData owner, BlueprintsCache cache, StatType stat = StatType.Constitution) => (UnitPartKineticist)Helper(typeof(PurifyingFlames01428Tests), "Kineticist", owner,
        (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("42a455d9ec1ad924d889272429eb8391")), stat, 5, 2);
    static AreaEffectEntityData Area(string id, MechanicsContext context, BlueprintAbilityAreaEffect blueprint) {
        var area = Empty<AreaEffectEntityData>(); Set(area, "UniqueId", id); Set(area, "m_Context", context); Set(area, "m_Blueprint", blueprint);
        var field = AccessTools.Field(typeof(AreaEffectEntityData), "m_UnitsInside"); field.SetValue(area, Activator.CreateInstance(field.FieldType)); return area;
    }
    static void Cold(BlueprintsCache cache, Harmony h) {
        var caster = Live(cache, "cold-owner"); var other = Live(cache, "cold-other"); var target = Live(cache, "cold-target");
        var part = Kineticist(caster, cache); AccessTools.Property(typeof(UnitPartKineticist), "AcceptedBurn").SetValue(part, 1);
        var enabled = Add(caster, Asset<BlueprintBuff>(cache, "ColdSnapEnabledBuff"), caster);
        var aura = Add(caster, Asset<BlueprintBuff>(cache, "ColdSnapAuraBuff"), caster);
        var otherEnabled = Add(other, Asset<BlueprintBuff>(cache, "ColdSnapEnabledBuff"), other);
        var otherAura = Add(other, Asset<BlueprintBuff>(cache, "ColdSnapAuraBuff"), other);
        var penalty = Asset<BlueprintBuff>(cache, "ColdSnapPenaltyBuff"); int baseline = target.Stats.Dexterity.ModifiedValue;
        var areaBp = Asset<BlueprintAbilityAreaEffect>(cache, "ColdSnapArea"); var owned = areaBp.GetComponent<AbilityAreaEffectBuff>();
        var areaOne = Area("01433-cold-one", aura.Context, areaBp); var areaTwo = Area("01433-cold-two", otherAura.Context, areaBp);
        owned.HandleUnitEnter(aura.Context, areaOne, target);
        var one = target.Buffs.Enumerable.Single(b => b.SourceAreaEffectId == areaOne.UniqueId);
        Check(target.Stats.Dexterity.ModifiedValue == baseline - 4 && one.Blueprint == penalty, "actual native area condition and buff insertion activate cold penalty");
        owned.HandleUnitEnter(otherAura.Context, areaTwo, target);
        var two = target.Buffs.Enumerable.Single(b => b.SourceAreaEffectId == areaTwo.UniqueId);
        Check(target.Stats.Dexterity.ModifiedValue == baseline - 4, "two native area-owned source penalties do not stack");
        owned.HandleUnitEnter(aura.Context, areaOne, caster);
        Check(!caster.Buffs.Enumerable.Any(b => b.Blueprint == penalty), "native area condition excludes caster before any penalty insertion");
        foreach (int amount in new[] { 4, 5, 10 }) {
            var bp = new BlueprintBuff { name = "01433-cold-resistance" + amount, AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = new BlueprintComponent[] { new AddDamageResistanceEnergy { Type = DamageEnergyType.Cold, Value = amount } } }; bp.OnEnable();
            var resistance = Add(target, bp, target);
            Check(target.Stats.Dexterity.ModifiedValue == baseline - (amount >= 5 ? 0 : 4), "native resistance threshold updates through actual buff-added event " + amount);
            target.Facts.Remove(resistance); Check(target.Stats.Dexterity.ModifiedValue == baseline - 4, "native removal immediately restores cold penalty " + amount);
        }
        foreach (var component in new BlueprintComponent[] { new AddEnergyImmunity { Type = DamageEnergyType.Cold }, new AddEnergyDamageImmunity { EnergyType = DamageEnergyType.Cold } }) {
            var immuneBp = new BlueprintBuff { name = "01433-cold-immunity", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = new[] { component } }; immuneBp.OnEnable();
            var immune = Add(target, immuneBp, target); Check(target.Stats.Dexterity.ModifiedValue == baseline, "native cold immunity suppresses penalty " + component.GetType().Name);
            target.Facts.Remove(immune); Check(target.Stats.Dexterity.ModifiedValue == baseline - 4, "immunity removal restores surviving penalty");
        }
        target.Facts.TurnOff(); Check(target.Stats.Dexterity.ModifiedValue == baseline, "native manager shutdown removes all cold modifiers");
        target.Facts.TurnOn(); Check(target.Stats.Dexterity.ModifiedValue == baseline - 4, "native manager restart elects one cold modifier");
        caster.Facts.TurnOff(); Check(caster.Facts.List.Contains(enabled) && caster.Facts.List.Contains(aura), "save-style source Facts.TurnOff preserves paid activation and aura identity");
        caster.Facts.TurnOn(); Check(caster.Facts.List.Contains(enabled) && caster.Facts.List.Contains(aura) && enabled.IsTurnedOn && aura.IsTurnedOn, "source Facts.TurnOn resumes retained activation without paying again");
        one.TurnOff(); Check(target.Stats.Dexterity.ModifiedValue == baseline - 4, "surviving source remains after first suppression");
        two.TurnOff(); Check(target.Stats.Dexterity.ModifiedValue == baseline, "all suppressed sources remove penalty");
        one.TurnOn(); two.TurnOn(); target.Facts.ApplyPostLoadFixes(); Check(target.Stats.Dexterity.ModifiedValue == baseline - 4, "native post-load fixes preserve one modifier");
        owned.HandleUnitExit(aura.Context, areaOne, target); Check(target.Stats.Dexterity.ModifiedValue == baseline - 4, "native first area exit retains other source");
        owned.HandleUnitExit(otherAura.Context, areaTwo, target); Check(target.Stats.Dexterity.ModifiedValue == baseline, "native last area exit leaves no modifier");
        var activation = enabled.Blueprint.GetComponent<KineticColdActivation01433>();
        Check(caster.Buffs.Enumerable.Contains(enabled) && caster.Buffs.Enumerable.Contains(aura), "activation and aura initially owned");
        part.ClearAcceptedBurn(); Check(!caster.Buffs.Enumerable.Contains(enabled) && !caster.Buffs.Enumerable.Contains(aura), "real ClearAcceptedBurn event removes activation and running aura");
        caster.Facts.TurnOff(); other.Facts.TurnOff(); target.Facts.TurnOff();
    }
    static void ColdOwnerAndRest(BlueprintsCache cache, Harmony h) {
        var owner = Live(cache, "cold-root-owner");
        Set(owner, "Abilities", owner.Facts.EnsureFactProcessor<AbilityCollection>());
        Set(owner, "ActivatableAbilities", owner.Facts.EnsureFactProcessor<ActivatableAbilityCollection>());
        Set(owner.Descriptor, "UISettings", new Kingmaker.UI.UnitSettings.UnitUISettings(owner.Descriptor));
        var root = Asset<BlueprintFeature>(cache, "ColdSnapWildTalent");
        var rootFact = (Feature)root.CreateFact(null, owner.Descriptor, null); owner.Facts.Add(rootFact);
        Check(rootFact.IsActive && rootFact.IsTurnedOn && owner.HasFact(Asset<BlueprintAbility>(cache, "ColdSnapEnableAbility")) && owner.HasFact(Asset<BlueprintActivatableAbility>(cache, "ColdSnapToggle")), "actual native Cold Snap root AddFacts grants its activation and toggle");
        var enabled = Add(owner, Asset<BlueprintBuff>(cache, "ColdSnapEnabledBuff"), owner);
        var aura = Add(owner, Asset<BlueprintBuff>(cache, "ColdSnapAuraBuff"), owner);
        rootFact.Reapply();
        Check(rootFact.IsActive && rootFact.IsTurnedOn && owner.Facts.List.Contains(enabled) && owner.Facts.List.Contains(aura) && !enabled.IsDisposed && !aura.IsDisposed, "actual native root Reapply preserves paid activation and same aura");
        rootFact.Deactivate();
        Check(owner.Facts.List.Contains(enabled) && owner.Facts.List.Contains(aura), "temporary native root Deactivate while managed preserves activation and aura");
        rootFact.Activate(); owner.Facts.TurnOff();
        Check(owner.Facts.List.Contains(rootFact) && owner.Facts.List.Contains(enabled) && owner.Facts.List.Contains(aura), "source save TurnOff with actual root preserves entire paid graph");
        owner.Facts.TurnOn();
        Check(rootFact.IsTurnedOn && enabled.IsTurnedOn && aura.IsTurnedOn, "source save TurnOn restores actual root and same paid graph");
        owner.Facts.Remove(rootFact);
        Check(rootFact.IsDisposed && enabled.IsDisposed && aura.IsDisposed && !owner.HasFact(Asset<BlueprintAbility>(cache, "ColdSnapEnableAbility")) && !owner.HasFact(Asset<BlueprintActivatableAbility>(cache, "ColdSnapToggle")), "actual native root removal revokes activation, aura and exact granted abilities");

        var resting = Live(cache, "cold-rest-zero"); var other = Live(cache, "cold-rest-other");
        var part = Kineticist(resting, cache); var otherPart = Kineticist(other, cache);
        AccessTools.Property(typeof(UnitPartKineticist), "AcceptedBurn").SetValue(otherPart, 1);
        var zeroEnabled = Add(resting, Asset<BlueprintBuff>(cache, "ColdSnapEnabledBuff"), resting);
        var zeroAura = Add(resting, Asset<BlueprintBuff>(cache, "ColdSnapAuraBuff"), resting);
        var otherEnabled = Add(other, Asset<BlueprintBuff>(cache, "ColdSnapEnabledBuff"), other);
        var otherAura = Add(other, Asset<BlueprintBuff>(cache, "ColdSnapAuraBuff"), other);
        var normal = new BlueprintBuff { name = "01433-no-rest-removal-control", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = Array.Empty<BlueprintComponent>() }; normal.OnEnable();
        var retained = Add(resting, normal, resting);
        Check(part.AcceptedBurn == 0, "zero-burn rest fixture starts with the actual native zero total");
        part.ClearAcceptedBurn();
        Check(!zeroEnabled.IsDisposed && !zeroAura.IsDisposed, "native zero-to-zero ClearAcceptedBurn emits no event and retains zero-cost activation");
        var restApply = AccessTools.Method(typeof(Kingmaker.Controllers.Rest.RestController), "ApplyRest", new[] { typeof(UnitDescriptor) });
        var replenish = PatchProcessor.GetOriginalInstructions(restApply).Select(i => i.operand).OfType<MethodInfo>().Single(m => m.Name == "Replenish");
        // Rest's RemoveOnRest loop, native Buff.Remove/OnDeactivate and real
        // global rest event stay intact. Only unrelated resource replenishment
        // (full resources/health/slots) is the explicit scene-service boundary.
        h.Patch(replenish, prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(Skip)));
        try { Kingmaker.Controllers.Rest.RestController.ApplyRest(resting.Descriptor); }
        finally { h.Unpatch(replenish, AccessTools.Method(typeof(KineticUtility01433Tests), nameof(Skip))); }
        Check(zeroEnabled.IsDisposed && zeroAura.IsDisposed, "actual native Rest ApplyRest RemoveOnRest loop clears zero-burn activation and its aura");
        Check(!retained.IsDisposed && !otherEnabled.IsDisposed && !otherAura.IsDisposed && otherPart.AcceptedBurn == 1, "native rest preserves non-RemoveOnRest buff and another owner's paid graph");
        owner.Facts.TurnOff(); resting.Facts.TurnOff(); other.Facts.TurnOff();
    }
    static bool Context(ref RulebookEventContext __result) { if (current == null) return true; __result = current; return false; }
    static T NativeRule<T>(T rule) where T : RulebookEvent {
        var prior = current; current = new RulebookEventContext(); current.PushEvent(rule);
        try { if (rule is RuleSavingThrow) saves++; rule.OnTrigger(current); return rule; } finally { current = prior; }
    }
    static T ContextRule<T>(MechanicsContext context, T rule) where T : RulebookEvent { rule.Reason = context; return NativeRule(rule); }
    static IEnumerable<CodeInstruction> RuleBoundary(IEnumerable<CodeInstruction> code) {
        foreach (var i in code) {
            if (i.operand is MethodInfo m && m.IsGenericMethod && m.Name == "TriggerRule" && m.DeclaringType == typeof(MechanicsContext)) { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(KineticUtility01433Tests), nameof(ContextRule)).MakeGenericMethod(m.GetGenericArguments()); }
            else if (i.operand is MethodInfo m2 && m2.IsGenericMethod && m2.Name == "Trigger" && m2.DeclaringType == typeof(Rulebook)) { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(KineticUtility01433Tests), nameof(NativeRule)).MakeGenericMethod(m2.GetGenericArguments()); }
            yield return i;
        }
    }
    static bool Dice(DiceFormula __0, ref int __result) { __result = __0.Dice == DiceType.D20 ? d20 : __0.Dice == DiceType.D4 ? d4 : 0; return false; }
    static bool D20(ref RuleRollD20 __result) { __result = NativeRule(new RuleRollD20(current.LastEvent<RulebookEvent>().Initiator)); return false; }
    static bool Expose(Exception __1) => throw new Exception("KineticUtility01433 action exception", __1);
    static void Smoke(BlueprintsCache cache, Harmony h) {
        h.Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(Context)));
        h.Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(Dice)));
        h.Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(D20)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(No)));
        foreach (var method in new[] { AccessTools.Method(typeof(ContextActionSavingThrow), "RunAction"), AccessTools.Method(typeof(RuleSavingThrow), "OnTrigger"), AccessTools.Method(typeof(RuleSavingThrow), "RollD20"), AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), AccessTools.Method(typeof(AbilityExecutionProcess), "PrepareCast"), AccessTools.Method(typeof(ContextDiceValue), "Calculate", new[] { typeof(MechanicsContext) }), AccessTools.Method(typeof(ContextValueHelper), "CalculateDiceValue"), AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }) })
            h.Patch(method, transpiler: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(RuleBoundary)));
        h.Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(Expose)));
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        var caster = Live(cache, "smoke-caster"); var target = Live(cache, "smoke-target");
        caster.Progression.Classes.Clear(); caster.Progression.Classes.Add(new ClassData((BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("42a455d9ec1ad924d889272429eb8391"))) { Level = 6 });
        Kineticist(caster, cache); caster.Stats.Constitution.BaseValue = 18;
        var ability = Asset<BlueprintAbility>(cache, "SmokeStormAbility");
        var context = new AbilityExecutionContext(new AbilityData(ability, caster.Descriptor), new AbilityParams(), new TargetWrapper(target), null, caster);
        AbilityExecutionProcess.PrepareCast(context);
        Check(context.Params.CasterLevel == 6 && context.Params.DC == 17, "actual native PrepareCast uses kineticist6 and main stat");
        var spawn = (ContextActionSpawnAreaEffect)ability.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        Check(spawn.DurationValue.Calculate(context).Seconds == TimeSpan.FromSeconds(36), "actual native duration is one round per kineticist level");
        var area = spawn.AreaEffect; var run = area.GetComponent<AbilityAreaEffectRunAction>();
        var effect = Empty<AreaEffectEntityData>(); Set(effect, "UniqueId", "01433-smoke"); Set(effect, "m_Context", context); Set(effect, "m_Blueprint", area);
        // Native HandleUnitEnter does no sickness. Native HandleRound iterates
        // the actual area membership list and executes conditional/save/apply.
        var units = AccessTools.Field(typeof(AreaEffectEntityData), "m_UnitsInside"); var list = (IList)Activator.CreateInstance(units.FieldType); units.SetValue(effect, list);
        var info = Activator.CreateInstance(units.FieldType.GetGenericArguments()[0], true); Set(info, "Reference", (UnitReference)target); list.Add(info);
        int before = saves; run.HandleUnitEnter(context, effect, target); Check(saves == before, "entering cloud does not prematurely roll sickness");
        foreach (int roll in new[] { 20, 1 }) {
            d20 = roll; d4 = 4; before = saves; run.HandleRound(context, effect);
            Check(saves == before + 1, "actual native area round dispatches one Fortitude save");
            var sickness = target.Buffs.Enumerable.Where(b => b.Blueprint.AssetGuid.ToString() == "4e42460798665fd4cb9173ffa7ada323").ToArray();
            Check(sickness.Length == (roll == 20 ? 0 : 1), "actual native save success/failure controls Sickened insertion");
            if (sickness.Length > 0) { Check(target.State.HasCondition(UnitCondition.Sickened), "native Sickened component changes target condition"); Check(sickness[0].TimeLeft == TimeSpan.FromSeconds(30), "native d4=4 plus1 yields5 rounds; actual=" + sickness[0].TimeLeft + "; planned=" + sickness[0].PlannedDuration); }
        }
        before = saves; run.HandleRound(context, effect); Check(saves == before, "already sickened target receives no new save or refresh");
        foreach (var buff in target.Buffs.Enumerable.ToArray()) target.Facts.Remove(buff);
        var excluded = area.GetComponent<AbilityAreaEffectRunAction>().Round.Actions.OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().Single().ConditionsChecker.Conditions.OfType<KineticSmokeEligible01433>().Single().Excluded;
        foreach (var type in excluded) { var f = Fact(target, (BlueprintFeature)type.Get()); before = saves; run.HandleRound(context, effect); Check(saves == before && !target.State.HasCondition(UnitCondition.Sickened), "native type exclusion skips smoke save " + type.Guid); target.Facts.List.Remove(f); AccessTools.Method(Get(target.Facts, "m_Cache").GetType(), "Clear").Invoke(Get(target.Facts, "m_Cache"), null); }
        caster.Facts.TurnOff(); target.Facts.TurnOff();
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = saves = applications = added = removed = burnNotifications = 0; current = null;
        var h = new Harmony("CotW01433.KineticUtilityNative"); var observer = new Observer();
        var game = Game.Instance; var oldState = game.State; var oldTime = Get(game, "TimeController");
        var oldWeather = game.BlueprintRoot.WeatherSettings;
        var buffHelper = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var outerCapture = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense");
        var outerDice = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice");
        var outerDiceQueue = (ICollection)AccessTools.Field(typeof(Psychic070Tests), "dice").GetValue(null);
        var oldDiceValues = outerDiceQueue.Cast<int>().ToArray();
        var endpoints = new[] {
            new { Method = (MethodBase)buffHelper, Prefix = outerCapture },
            new { Method = (MethodBase)AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), Prefix = outerDice },
            // The old fixture currently has no D20 prefix. Retain the exact
            // declaring-type guard if that fixture later adds this endpoint.
            new { Method = (MethodBase)AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), Prefix = (MethodInfo)null }
        };
        var suspended = endpoints.SelectMany(e => (Harmony.GetPatchInfo(e.Method)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<HarmonyLib.Patch>())
            .Where(p => e.Prefix != null ? p.PatchMethod == e.Prefix : p.PatchMethod.DeclaringType == typeof(Psychic070Tests))
            .Select(p => new { Original = e.Method, Record = p })).ToArray();
        JObject contract = null;
        void Patch(MethodBase m, string n) => h.Patch(m, prefix: new HarmonyMethod(typeof(KineticUtility01433Tests), n));
        try {
            foreach (var saved in suspended) h.Unpatch(saved.Original, saved.Record.PatchMethod);
            Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip));
            Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No));
            Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore));
            Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(NoController));
            Provider01419Tests.PatchLabels(h);
            var timeField = AccessTools.Field(typeof(Game), "TimeController"); Set(game, "TimeController", FormatterServices.GetUninitializedObject(timeField.FieldType));
            var player = Empty<Player>(); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); player.GameTime = TimeSpan.FromHours(1);
            Set(player, "Weather", new Player.WeatherData());
            game.BlueprintRoot.WeatherSettings = new Kingmaker.Blueprints.Root.WeatherRoot();
            var state = Empty<PersistentState>(); state.PlayerState = player; Set(game, "State", state);
            Patch(AccessTools.PropertyGetter(timeField.FieldType, "GameTime"), nameof(Time));
            Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(False));
            Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(False));
            Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(UniqueId));
            Patch(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), nameof(Skip));
            Patch(AccessTools.Method(AccessTools.Property(typeof(UnitEntityData), "CombatState").PropertyType, "ReturnToStartingPositionIfNeeded"), nameof(Skip));
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(KineticUtility01433Tests), nameof(BuffDifficultyBoundary)));
            Registration(cache, h); ConcealmentCases(cache, h);
            EventBus.Subscribe(observer); Cold(cache, h); ColdOwnerAndRest(cache, h); Smoke(cache, h);
            var moduleAssets = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray().Select(cache.Load)
                .Where(b => b != null && new[] { "Windsight", "ColdSnap", "SmokeStorm" }.Any(p => b.name.StartsWith("CotWWrathPsychic" + p, StringComparison.Ordinal))).OrderBy(b => b.AssetGuid.ToString()).ToArray();
            PsychicExpansionTests.Inventory["kineticUtility01433Contracts"] = contract = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 3,
                ["itemIds"] = new JArray("COTW-0322", "COTW-0324", "COTW-0326"), ["rootFeatureCount"] = 3, ["utilityTalentCount"] = 3, ["newSpellRootCount"] = 0,
                ["newBlueprints"] = new JArray(moduleAssets.Select(b => new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name, ["type"] = b.GetType().FullName })),
                ["nativeSelectionAndPrerequisites"] = true, ["independentRegistrationAndRollback"] = true,
                ["nativeFogOnlyCalculate"] = true, ["replacementFogCalculate"] = true, ["nestedAndExceptionRestore"] = true,
                ["nativeFactManagerAndEventBus"] = true, ["dynamicColdResistanceAndOverlap"] = true,
                ["burnClearRemovesActivation"] = true, ["nativeSmokeAreaSaveAndApply"] = true,
                ["nativeColdRootRemoveAndReapply"] = true, ["nativeColdSaveRestart"] = true, ["nativeColdZeroBurnRestRemovalLoop"] = true,
                ["nativeBuffAddedEvents"] = added, ["nativeBuffRemovedEvents"] = removed, ["nativeSavingThrows"] = saves,
                ["boundary"] = "Actual native CanSelect/prerequisites, native concealment Calculate and production prefix/finalizer (same-initiator nested scopes), native fact manager activation/removal/restart/Reapply/EventBus, actual Cold Snap root AddFacts/remove, live damage-reduction components, ClearAcceptedBurn, RestController.ApplyRest original RemoveOnRest loop and global event, PrepareCast and Area.HandleRound/save/apply paths execute. Rest alone skips unrelated UnitDescriptor.Replenish. Constructed game/unit/area membership services, raw dice, no-View scene services and difficulty presets are explicit boundaries. UniqueAreaEffect definition is checked; actual area allocation/replacement, full scene area movement, render FX, game UI, complete rest and persistence remain game regression cases." };
            Console.WriteLine("PASS: " + checks + " KineticUtility01433 checks."); return checks;
        } finally {
            EventBus.Unsubscribe(observer); h.UnpatchAll(h.Id);
            foreach (var saved in suspended) {
                var p = saved.Record;
                new Harmony(p.owner).Patch(saved.Original, prefix: new HarmonyMethod(p.PatchMethod) {
                    priority = p.priority, before = p.before, after = p.after, debug = p.debug });
            }
            current = null; Set(game, "State", oldState); Set(game, "TimeController", oldTime); game.BlueprintRoot.WeatherSettings = oldWeather;
            if (!outerDiceQueue.Cast<int>().SequenceEqual(oldDiceValues)) throw new Exception("Utility fixture changed the suspended outer ControlledDice queue");
            foreach (var saved in suspended) {
                var p = saved.Record;
                if (!(Harmony.GetPatchInfo(saved.Original)?.Prefixes.Any(now => now.PatchMethod == p.PatchMethod && now.owner == p.owner && now.priority == p.priority && now.debug == p.debug &&
                    (now.before ?? Array.Empty<string>()).SequenceEqual(p.before ?? Array.Empty<string>()) && (now.after ?? Array.Empty<string>()).SequenceEqual(p.after ?? Array.Empty<string>())) ?? false))
                    throw new Exception("Utility fixture failed to restore exact outer prefix metadata: " + p.PatchMethod.Name);
            }
            if (contract != null) { contract["outerFixtureInputsAndPatchesRestored"] = true; contract["outerFixturePrefixesRestored"] = true; contract["outerControlledDiceQueuePreserved"] = true; PsychicExpansionTests.Inventory["utility01433OuterPrefixSuspensionCount"] = suspended.Length; }
        }
    }
}
