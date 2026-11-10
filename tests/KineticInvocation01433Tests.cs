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
using Newtonsoft.Json.Serialization;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
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
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Original manager/fact/burn/Rulebook/EventBus paths. Scene construction,
// final area allocation/view and reference-service reconnect are explicit
// endpoints; production notification hooks are never invoked by the fixture.
internal static class KineticInvocation01433Tests
{
    static int checks, serial, appendFault, buffAdded, buffRemoved, areaAdded, areaRemoved, burnEvents, parameterEvents, spawned;
    static Buff faultBuff;
    static int removeFaults;
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static readonly List<AreaEffectEntityData> areas = new List<AreaEffectEntityData>();
    static readonly List<ParamsObserver> parameterObservers = new List<ParamsObserver>();
    static Type Module => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.KineticInvocation01433", true);
    static Type Factory => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Factory, "Id").Invoke(null, new object[] { token });
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b); return r; }
    static T Copy<T>(T value) => (T)AccessTools.Method(Factory, "CopyData").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { value });
    static string Definition(object value) => (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new[] { value });
    static void Check(bool value, string reason) { if (!value) throw new Exception("KineticInvocation01433: " + reason); checks++; }
    static void Register(BlueprintsCache c) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { c });
    static IEnumerable<SimpleBlueprint> Loaded(BlueprintsCache c) => ((IDictionary)Get(c, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray().Select(c.Load).Where(b => b != null);
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => (BlueprintFeatureReference[])Get(p, field) ?? Array.Empty<BlueprintFeatureReference>();
    static bool Reject(Action action, string message = null) { try { action(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e is InvalidOperationException && (message == null || e.Message.Contains(message)); } }
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Yes(ref bool __result) { __result = true; return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static bool Controller(ref LevelUpController __result) { __result = null; return false; }
    static bool Expose(Exception __1) => throw new Exception("KineticInvocation01433 native action exception", __1);
    static bool PoolIgnore(BlueprintFeatureSelection __instance, ref bool __result) { __result = __instance.IgnorePrerequisites; return false; }
    static void UniqueId(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "01433-inv-fact-" + (++serial)); }
    static void AppendFault(IEnumerable<BlueprintFeatureSelection> selections, BlueprintFeature[] feats) {
        bool ordinary = feats.Any(f => f.AssetGuid == Id("KineticInvocationFeat"));
        if (appendFault == 1 && ordinary || appendFault == 2 && feats.Any(f => f.name.StartsWith("CotWWrathPsychicKineticInvocation") && !ordinary))
            throw new InvalidOperationException("01433 invocation append fault");
    }
    static void RemovalFault(Buff __instance) { if (__instance == faultBuff && removeFaults > 0) { removeFaults--; throw new InvalidOperationException("01433 invocation removal fault"); } }
    static void BooklessSpend(AbilityData data) { Check(data.Spellbook == null, "actual invocation Spend has no spellbook branch"); }
    static IEnumerable<CodeInstruction> SpendBoundary(IEnumerable<CodeInstruction> code) {
        foreach (var i in code) {
            if (i.operand is MethodInfo m && m.DeclaringType == typeof(AbilityData) && m.Name == "SpendFromSpellbook") { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(KineticInvocation01433Tests), nameof(BooklessSpend)); }
            yield return i;
        }
    }
    sealed class Events : IUnitBuffHandler, IAreaEffectHandler, IKineticistGlobalHandler {
        public void HandleBuffDidAdded(Buff buff) { buffAdded++; }
        public void HandleBuffDidRemoved(Buff buff) { buffRemoved++; }
        public void HandleAreaEffectSpawned(AreaEffectEntityData area) { areaAdded++; }
        public void HandleAreaEffectDestroyed(AreaEffectEntityData area) { areaRemoved++; }
        public void HandleKineticistBurnValueChanged(UnitPartKineticist part, int previous, AbilityData ability) { burnEvents++; }
    }
    sealed class ParamsObserver : IInitiatorRulebookHandler<RuleCalculateAbilityParams> {
        readonly UnitEntityData unit;
        internal ParamsObserver(UnitEntityData unit) { this.unit = unit; }
        public UnitEntityData GetSubscribingUnit() => unit;
        public void OnEventAboutToTrigger(RuleCalculateAbilityParams e) { parameterEvents++; }
        public void OnEventDidTrigger(RuleCalculateAbilityParams e) { parameterEvents++; }
    }
    sealed class ZeroCost : IKineticistCalculateAbilityCostHandler {
        internal UnitEntityData Owner;
        public void HandleKineticistCalculateAbilityCost(UnitDescriptor caster, BlueprintAbility ability, ref KineticistAbilityBurnCost cost) {
            if (caster == Owner.Descriptor && ability.GetComponent<KineticInvocationOrigin01433>() != null) cost.Decrease(1, KineticistBurnType.WildTalent);
        }
    }
    static void Proxy(EntityDataBase entity) {
        Set(entity, "UniqueId", "01433-inv-entity-" + (++serial));
        var proxy = FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy", BindingFlags.Public | BindingFlags.NonPublic));
        Set(proxy, "Entity", entity); Set(entity, "Proxy", proxy);
    }
    static UnitEntityData Unit(BlueprintsCache c, string label, int level = 12, StatType stat = StatType.Constitution) {
        var u = (UnitEntityData)AccessTools.Method(typeof(FlameJetLifecycle01430Tests), "Unit").Invoke(null, new object[] { c, "inv-" + label });
        Proxy(u); Set(u, "Abilities", u.Facts.EnsureFactProcessor<AbilityCollection>());
        Set(u.CombatState, "AIData", new CombatAiData());
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c, "42a455d9ec1ad924d889272429eb8391")) { Level = level });
        u.Stats.Constitution.BaseValue = 18; u.Stats.Wisdom.BaseValue = 22; u.Stats.Charisma.BaseValue = 14;
        units.Add(u); var observer = new ParamsObserver(u); parameterObservers.Add(observer); EventBus.Subscribe(observer); return u;
    }
    static UnitPartKineticist Part(UnitEntityData u, BlueprintsCache c, StatType stat = StatType.Constitution, int total = 20, int perRound = 2) =>
        (UnitPartKineticist)AccessTools.Method(typeof(PurifyingFlames01428Tests), "Kineticist").Invoke(null, new object[] { u, Native<BlueprintCharacterClass>(c, "42a455d9ec1ad924d889272429eb8391"), stat, total, perRound });
    static Feature Add(UnitEntityData u, BlueprintFeature bp) {
        var f = new Feature(bp, u.Descriptor); u.Facts.Add(f);
        Check(f.IsActive && f.IsTurnedOn && f.IsSubscribedOnEventBus, "native feature manager activates/subscribes " + bp.name); return f;
    }
    static Feature Presence(UnitEntityData u, BlueprintFeature bp) {
        var original = bp.ComponentsArray; var before = Definition(original);
        try { bp.ComponentsArray = Array.Empty<BlueprintComponent>(); return Add(u, bp); }
        finally { bp.ComponentsArray = original; Check(Definition(bp.ComponentsArray) == before, "unrelated prerequisite activation payload restored"); }
    }
    static BlueprintsCache Fresh(BlueprintsCache parent) {
        var c = new BlueprintsCache();
        var privateIds = Loaded(parent).Where(b => b.name.StartsWith("CotWWrathPsychicKineticInvocation")).Select(b => b.AssetGuid).ToHashSet();
        var cloak = BlueprintGuid.Parse("6fb10fff27b84bff81746194bf8e38a8");
        foreach (var original in Loaded(parent).Where(b => !privateIds.Contains(b.AssetGuid) && b.AssetGuid != cloak)) {
            var value = original;
            if (original is BlueprintFeatureSelection pool) {
                value = (SimpleBlueprint)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null);
                foreach (var field in new[] { "m_Features", "m_AllFeatures" }) {
                    var refs = Copy(Refs(pool, field).Where(r => r != null && !privateIds.Contains(r.Guid)).ToArray());
                    foreach (var r in refs) AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, null);
                    Set(value, field, refs);
                }
            }
            c.AddCachedBlueprint(value.AssetGuid, value);
        }
        foreach (string id in new[] { "6b30813c3709fc44b92dc8fd8191f345", "9256a86aec14ad14e9497f6b60e26f3f" }) {
            var actual = PsychicExpansionTests.NativeCase<BlueprintAbility>(id);
            var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
            foreach (string n in new[] { "Range", "ActionType", "m_IsFullRoundAction", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "CanTargetPoint", "MaterialComponent", "AvailableMetamagic" }) {
                var field = AccessTools.Field(typeof(BlueprintAbility), n); if (field != null && row[n] != null) field.SetValue(actual, PsychicExpansionTests.Read(row[n], field.FieldType));
            }
            c.RemoveCachedBlueprint(actual.AssetGuid); c.AddCachedBlueprint(actual.AssetGuid, actual);
        }
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c); return c;
    }
    static BlueprintFeatureSelection[] Pools(BlueprintsCache c) => new[] { Native<BlueprintFeatureSelection>(c, "247a4068296e8be42890143f451b4b45"), Native<BlueprintFeatureSelection>(c, "5c883ae0cd6d7d5448b7a420f51f8459") };
    static BlueprintAbility Provider(BlueprintsCache c) {
        var bp = new BlueprintAbility { AssetGuid = BlueprintGuid.Parse("6fb10fff27b84bff81746194bf8e38a8"), name = "CloakOfWindsAbility", Type = AbilityType.Spell, ActionType = UnitCommand.CommandType.Standard, Range = AbilityRange.Touch, CanTargetSelf = true, CanTargetFriends = true,
            ComponentsArray = new BlueprintComponent[] { new AbilityEffectRunAction { Actions = new Kingmaker.ElementsSystem.ActionList { Actions = Array.Empty<Kingmaker.ElementsSystem.GameAction>() } }, new SpellComponent { School = SpellSchool.Abjuration } } };
        bp.OnEnable(); c.AddCachedBlueprint(bp.AssetGuid, bp); return bp;
    }
    static void Enable(BlueprintsCache c, BlueprintAbility source) {
        var list = new BlueprintSpellList { name = "01433CloakProviderList", AssetGuid = Id("01433CloakProviderList") };
        list.SpellsByLevel = Enumerable.Range(0, 10).Select(i => new SpellLevelList(i)).ToArray(); list.SpellsByLevel[3].Spells.Add(source);
        c.AddCachedBlueprint(list.AssetGuid, list);
        var membership = new SpellListComponent { SpellLevel = 3 }; Set(membership, "m_SpellList", Ref<BlueprintSpellListReference>(list));
        source.ComponentsArray = source.ComponentsArray.Concat(new[] { membership }).ToArray(); source.OnEnable();
    }
    static IEnumerable<BlueprintAbility> Leaves(BlueprintAbility ability) {
        var children = ability.GetComponent<AbilityVariants>();
        if (children == null) yield return ability;
        else foreach (var child in children.Variants) foreach (var leaf in Leaves(child)) yield return leaf;
    }
    static BlueprintAbility[] Sources(BlueprintsCache c) => new[] { Native<BlueprintAbility>(c, "6fb10fff27b84bff81746194bf8e38a8"), Asset<BlueprintAbility>(c, "WindWalk"), Native<BlueprintAbility>(c, "6b30813c3709fc44b92dc8fd8191f345"), Native<BlueprintAbility>(c, "9256a86aec14ad14e9497f6b60e26f3f"), Asset<BlueprintAbility>(c, "Invigorate"), Asset<BlueprintAbility>(c, "FluidForm"), Asset<BlueprintAbility>(c, "SleetStorm") };
    static void Registration(BlueprintsCache parent, out BlueprintsCache cache) {
        cache = Fresh(parent); var c = cache; var pools = Pools(c);
        var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var initial = Loaded(c).Select(b => b.AssetGuid).ToHashSet();
        foreach (int fault in new[] { 1, 2 }) {
            appendFault = fault; Check(Reject(() => Register(c), "invocation append fault"), "actual ordinary/wild pool post-append rejection " + fault); appendFault = 0;
            Check(Loaded(c).All(b => initial.Contains(b.AssetGuid)), "failed publication leaves no private graph " + fault);
            for (int p = 0; p < 2; p++) for (int f = 0; f < 2; f++) Check(ReferenceEquals(before[p][f], Get(pools[p], f == 0 ? "m_Features" : "m_AllFeatures")), "exact two-pool arrays restored");
        }
        Register(c); Check(Asset<BlueprintFeature>(c, "KineticInvocationFeat") != null && Asset<BlueprintFeature>(c, "KineticInvocationCloakOfWindsTalent") == null, "six core options survive absent provider");
        var cloak = Provider(c); Register(c); Check(Asset<BlueprintFeature>(c, "KineticInvocationCloakOfWindsTalent") == null, "disabled provider spell without real list membership is excluded");
        Enable(c, cloak); var sourceSignatures = Sources(c).Select(b => Definition(b.ComponentsArray)).ToArray(); Register(c); Register(c);
        var feat = Asset<BlueprintFeature>(c, "KineticInvocationFeat"); var tokens = (string[])AccessTools.Field(Module, "Tokens").GetValue(null);
        Check(tokens.Length == 7 && feat.Groups.SequenceEqual(new[] { FeatureGroup.Feat }), "one ordinary feat unlocks seven separately selected talents");
        for (int i = 0; i < 7; i++) {
            var talent = Asset<BlueprintFeature>(c, "KineticInvocation" + tokens[i] + "Talent"); var ability = Asset<BlueprintAbility>(c, "KineticInvocation" + tokens[i] + "Ability"); var donor = Sources(c)[i];
            Check(talent.IsClassFeature && talent.Groups.SequenceEqual(new[] { FeatureGroup.KineticWildTalent }) && talent.GetComponent<AddFacts>().Facts.Single() == ability, "registered wild talent has actual ability grant " + i);
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                Check(Refs(pools[0], field).Count(r => r.Guid == feat.AssetGuid) == 1 && Refs(pools[0], field).All(r => r.Guid != talent.AssetGuid), "ordinary pool contains only feat root once");
                Check(Refs(pools[1], field).Count(r => r.Guid == talent.AssetGuid) == 1 && Refs(pools[1], field).All(r => r.Guid != feat.AssetGuid), "wild pool contains each talent once");
            }
            Check(!ReferenceEquals(ability.ComponentsArray, donor.ComponentsArray) && !ability.ComponentsArray.Intersect(donor.ComponentsArray).Any(), "private root components deep cloned");
            foreach (var leaf in Leaves(ability)) {
                Check(leaf.Type == AbilityType.SpellLike && leaf.AvailableMetamagic == 0 && !leaf.GetComponents<SpellListComponent>().Any() && leaf.MaterialComponent.Count == 0, "private leaf not a spell/list/metamagic/material consumer");
                var cost = leaf.GetComponent<AbilityKineticist>(); Check(cost != null && cost.WildTalentBurnCost == 1 && cost.BlastBurnCost == 0 && cost.InfusionBurnCost == 0, "real native one-burn utility cost");
                Check(leaf.GetComponent<KineticInvocationOrigin01433>().Original.Get() == donor, "private leaf origin remains exact donor");
                Check(leaf.School == donor.School && leaf.School != SpellSchool.None, "actual SLA retains donor school, including Transmutation interactions");
                Check(leaf.GetComponent<ContextCalculateAbilityParams>().UseKineticistMainStat, "native parameters use kineticist configured main stat");
                if (i == 3) Check(leaf.Range == AbilityRange.Personal && leaf.CanTargetSelf && !leaf.CanTargetFriends && !leaf.CanTargetEnemies && !leaf.CanTargetPoint, "Salamander restriction is strictly self only");
                else Check(leaf.Range == donor.Range && leaf.CanTargetSelf == donor.CanTargetSelf && leaf.CanTargetFriends == donor.CanTargetFriends && leaf.CanTargetEnemies == donor.CanTargetEnemies, "existing targeting flags retained");
            }
        }
        Check(sourceSignatures.SequenceEqual(Sources(c).Select(b => Definition(b.ComponentsArray))), "all six real donors and optional provider components unchanged");
        var owned = Loaded(c).Where(b => b.name.StartsWith("CotWWrathPsychicKineticInvocation")).ToArray(); var ownedSignatures = owned.Select(b => Definition(((BlueprintScriptableObject)b).ComponentsArray)).ToArray();
        var wind = Sources(c)[1]; var oldComponents = wind.ComponentsArray; wind.ComponentsArray = oldComponents.Concat(new[] { new AbilityCasterInCombat() }).ToArray();
        try { Register(c); Check(owned.All(b => ReferenceEquals(c.Load(b.AssetGuid), b)) && ownedSignatures.SequenceEqual(owned.Select(b => Definition(((BlueprintScriptableObject)b).ComponentsArray))), "late donor mutation does not reclone published private graph"); }
        finally { wind.ComponentsArray = oldComponents; }
        foreach (var p in pools) foreach (string f in new[] { "m_Features", "m_AllFeatures" }) Set(p, f, Refs(p, f).Where(r => !owned.Any(b => b.AssetGuid == r.Guid)).ToArray());
        var retryBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray(); appendFault = 2;
        Check(Reject(() => Register(c), "invocation append fault"), "published graph repair reaches real second-pool fault"); appendFault = 0;
        for (int p = 0; p < 2; p++) for (int f = 0; f < 2; f++) Check(ReferenceEquals(retryBefore[p][f], Get(pools[p], f == 0 ? "m_Features" : "m_AllFeatures")), "published retry rollback restores exact original arrays");
        Register(c); Check(owned.All(b => ReferenceEquals(c.Load(b.AssetGuid), b)), "pool repair keeps original owned asset instances");
        var target = Asset<BlueprintAbility>(c, "KineticInvocationInvigorateAbility"); var graphComponents = target.ComponentsArray; target.ComponentsArray = graphComponents.Concat(new[] { new AbilityCasterInCombat() }).ToArray();
        Check(Reject(() => Register(c), "published graph"), "changed owned graph fails honestly"); target.ComponentsArray = graphComponents; Register(c);
        var cloakAbility = Asset<BlueprintAbility>(c, "KineticInvocationCloakOfWindsAbility"); var foreignCloak = new BlueprintAbility { AssetGuid = cloakAbility.AssetGuid, name = "foreign optional replacement", ComponentsArray = Array.Empty<BlueprintComponent>() };
        c.RemoveCachedBlueprint(cloakAbility.AssetGuid); c.AddCachedBlueprint(foreignCloak.AssetGuid, foreignCloak);
        try { Register(c); Check(ReferenceEquals(Asset<BlueprintFeature>(c, "KineticInvocationFeat"), feat) && ReferenceEquals(c.Load(foreignCloak.AssetGuid), foreignCloak) && ((string)AccessTools.Field(Module, "LastOptionalDiagnostic").GetValue(null)).Contains("isolated"), "published optional graph corruption is isolated without overwriting foreign resource or failing six core talents"); }
        finally { c.RemoveCachedBlueprint(foreignCloak.AssetGuid); c.AddCachedBlueprint(cloakAbility.AssetGuid, cloakAbility); }
        Register(c);
        var foreign = Fresh(parent); var stub = new BlueprintFeature { AssetGuid = Id("KineticInvocationFeat"), name = "foreign partial graph", ComponentsArray = Array.Empty<BlueprintComponent>() }; foreign.AddCachedBlueprint(stub.AssetGuid, stub);
        Check(Reject(() => Register(foreign), "unowned or incomplete") && ReferenceEquals(foreign.Load(stub.AssetGuid), stub), "foreign partial graph rejected without overwriting foreign asset");
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
    }
    static LevelUpState SelectionState(UnitEntityData u, BlueprintCharacterClass cls, BlueprintFeatureSelection pool, int level, out FeatureSelectionState pending) {
        var state = Empty<LevelUpState>(); Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", level); state.NextClassLevel = level; state.SelectedClass = cls;
        pending = state.AddSelection(null, default(FeatureSource), pool, level); return state;
    }
    static void EligibilityAndCosts(BlueprintsCache c) {
        var feat = Asset<BlueprintFeature>(c, "KineticInvocationFeat"); var pools = Pools(c); var cls = Native<BlueprintCharacterClass>(c, "42a455d9ec1ad924d889272429eb8391");
        var ordinary = Unit(c, "feat", 0); var state = SelectionState(ordinary, cls, pools[0], 1, out var pending);
        Check(!pools[0].CanSelect(ordinary.Descriptor, state, pending, feat), "zero kineticist levels cannot take ordinary feat"); ordinary.Progression.Classes[0].Level = 1;
        Check(pools[0].CanSelect(ordinary.Descriptor, state, pending, feat), "kineticist1 ordinary feat is selectable"); Add(ordinary, feat);
        Check(!pools[0].CanSelect(ordinary.Descriptor, state, pending, feat) && ordinary.Abilities.Enumerable.All(a => a.Blueprint.GetComponent<KineticInvocationOrigin01433>() == null), "feat is not repeatable and grants no utility spell automatically");
        var tokens = (string[])AccessTools.Field(Module, "Tokens").GetValue(null); var levels = new[] { 6, 12, 8, 10, 1, 12, 6 };
        for (int i = 0; i < tokens.Length; i++) {
            var talent = Asset<BlueprintFeature>(c, "KineticInvocation" + tokens[i] + "Talent"); var ability = Asset<BlueprintAbility>(c, "KineticInvocation" + tokens[i] + "Ability"); var element = talent.GetComponent<PrerequisiteFeaturesFromList>();
            Check(element != null && element.Amount == 1 && element.Features.Length > 0, "real element alternatives present " + i);
            var u = Unit(c, "choice-" + i, levels[i]); var pick = SelectionState(u, cls, pools[1], levels[i], out var chosen);
            Check(!pools[1].CanSelect(u.Descriptor, pick, chosen, talent), "no feat/element rejects wild talent " + i);
            var wrong = Unit(c, "wrong-element-" + i, levels[i]); Add(wrong, feat);
            var wrongState = SelectionState(wrong, cls, pools[1], levels[i], out var wrongSelection);
            Check(!pools[1].CanSelect(wrong.Descriptor, wrongState, wrongSelection, talent), "feat alone does not waive required element " + i);
            var alternate = Asset<BlueprintFeature>(c, "KineticInvocation" + tokens[i == 3 || i == 4 ? 0 : 4] + "Talent").GetComponent<PrerequisiteFeaturesFromList>().Features.First(f => !element.Features.Contains(f));
            Presence(wrong, alternate); Check(!pools[1].CanSelect(wrong.Descriptor, wrongState, wrongSelection, talent), "an unrelated native element cannot satisfy this element gate " + i);
            Presence(u, element.Features[0]); Check(!pools[1].CanSelect(u.Descriptor, pick, chosen, talent), "element alone still requires feat " + i);
            Add(u, feat); u.Progression.Classes[0].Level = levels[i] - 1; Check(!pools[1].CanSelect(u.Descriptor, pick, chosen, talent), "one level below rejects " + i); u.Progression.Classes[0].Level = levels[i];
            Check(pools[1].CanSelect(u.Descriptor, pick, chosen, talent), "exact required level and correct element/feat select " + i);
            Add(u, talent); Check(!pools[1].CanSelect(u.Descriptor, pick, chosen, talent), "native duplicate talent rejection " + i);
            var part = Part(u, c, total:2, perRound:1); var leaf = Leaves(ability).First(); var data = new AbilityData(leaf, u.Descriptor); var cost = leaf.GetComponent<AbilityKineticist>();
            Check(ReferenceEquals(data.ResourceLogic, cost) && cost.CalculateCost(data) == 1 && cost.IsAbilityRestrictionPassed(data), "real AbilityData burn dispatcher " + i);
            data.Spend(); Check(part.AcceptedBurn == 1 && part.AcceptedBurnThisRound == 1, "actual native Spend accepts one burn " + i);
            Check(!cost.IsAbilityRestrictionPassed(data), "native per-round burn limit " + i); part.ClearAcceptedBurnThisRound(); data.Spend(); part.ClearAcceptedBurnThisRound();
            Check(part.AcceptedBurn == 2 && !cost.IsAbilityRestrictionPassed(data), "native total burn limit " + i); part.ClearAcceptedBurn(); Check(cost.IsAbilityRestrictionPassed(data), "native ClearAcceptedBurn restores affordability " + i);
            var outsider = Unit(c, "ability-outsider-" + i); var gate = leaf.GetComponent<AbilityCasterHasFacts>();
            Check(gate != null && !gate.IsCasterRestrictionPassed(outsider), "unowned invocation fails actual caster fact checker " + i); Add(outsider, feat);
            Check(gate.IsCasterRestrictionPassed(outsider) && !cost.IsAbilityRestrictionPassed(new AbilityData(leaf, outsider.Descriptor)), "feat gate does not replace native kineticist-part restriction " + i);
        }
        foreach (var stat in new[] { StatType.Constitution, StatType.Wisdom, StatType.Charisma }) {
            var u = Unit(c, "parameters-" + stat, 12); Part(u, c, stat); Add(u, feat);
            var ability = Asset<BlueprintAbility>(c, "KineticInvocationInvigorateAbility"); var ctx = Cast(ability, u);
            Check(ctx.Params.CasterLevel == 12 && ctx.Params.SpellLevel == 6 && ctx.Params.DC == 16 + u.Stats.GetStat<ModifiableValueAttributeStat>(stat).Bonus, "native PrepareCast and Rulebook main-stat CL/DC " + stat);
        }
        Check(parameterEvents > 0 && burnEvents >= 21, "actual Rulebook and native global burn events observed");
    }
    static AbilityExecutionContext Cast(BlueprintAbility ability, UnitEntityData u) {
        var ctx = new AbilityExecutionContext(new AbilityData(ability, u.Descriptor), new AbilityParams(), new TargetWrapper(u), null, u); AbilityExecutionProcess.PrepareCast(ctx); return ctx;
    }
    static void PublishParent(BlueprintsCache parent) {
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, parent);
        foreach (string id in new[] { "6b30813c3709fc44b92dc8fd8191f345", "9256a86aec14ad14e9497f6b60e26f3f" }) {
            var existing = Native<BlueprintAbility>(parent, id);
            if (existing != null && existing.name == "NativeSpell" && existing.Type == AbilityType.Spell && existing.ComponentsArray != null && existing.ComponentsArray.Length == 0) {
                var actual = PsychicExpansionTests.NativeCase<BlueprintAbility>(id); var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
                existing.name = actual.name; existing.ComponentsArray = actual.ComponentsArray;
                foreach (string n in new[] { "Range", "ActionType", "Type", "m_IsFullRoundAction", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "CanTargetPoint", "MaterialComponent", "AvailableMetamagic" }) {
                    var field = AccessTools.Field(typeof(BlueprintAbility), n); if (field != null && row[n] != null) field.SetValue(existing, PsychicExpansionTests.Read(row[n], field.FieldType));
                }
                existing.OnEnable();
            }
        }
        Register(parent); Register(parent);
        var pools = Pools(parent); var feat = Asset<BlueprintFeature>(parent, "KineticInvocationFeat");
        Check(feat != null && pools[0].AllFeatures.Count(f => f == feat) == 1 && !pools[1].AllFeatures.Contains(feat), "actual parent registration exposes ordinary feat before Urgent early-root enumeration");
        foreach (string token in ((string[])AccessTools.Field(Module, "Tokens").GetValue(null)).Skip(1)) {
            var talent = Asset<BlueprintFeature>(parent, "KineticInvocation" + token + "Talent"); Check(talent != null && pools[1].AllFeatures.Count(f => f == talent) == 1 && !pools[0].AllFeatures.Contains(talent), "actual parent registration exposes core utility talent " + token);
        }
    }
    static BlueprintBuff Probe(BlueprintsCache c) {
        var bp = new BlueprintBuff { AssetGuid = Id("01433InvocationSharedPayloadProbe"), name = "01433InvocationSharedPayloadProbe", Stacking = StackingType.Stack, ComponentsArray = Array.Empty<BlueprintComponent>() };
        bp.OnEnable(); c.AddCachedBlueprint(bp.AssetGuid, bp); return bp;
    }
    static Buff Buff(UnitEntityData target, BlueprintBuff bp, MechanicsContext context, bool permanent = false) {
        var buff = new Buff(bp, context, permanent ? (TimeSpan?)null : TimeSpan.FromMinutes(1)); target.Facts.Add(buff);
        Check(buff.IsActive && buff.IsTurnedOn && buff.IsSubscribedOnEventBus && buff.IsPermanent == permanent, "native Buff attach and permanent boundary"); return buff;
    }
    static AreaEffectEntityData Area(MechanicsContext context, BlueprintAbilityAreaEffect bp, bool permanent = false) {
        var area = Empty<AreaEffectEntityData>(); Proxy(area); Set(area, "m_Context", context); Set(area, "m_Blueprint", bp); Set(area, "m_Duration", permanent ? (TimeSpan?)null : TimeSpan.FromSeconds(72));
        areas.Add(area); EventBus.RaiseEvent<IAreaEffectHandler>(h => h.HandleAreaEffectSpawned(area)); return area;
    }
    static bool Spawn(MechanicsContext __0, BlueprintAbilityAreaEffect __1, TargetWrapper __2, TimeSpan? __3, ref AreaEffectEntityData __result) {
        Check(__3.HasValue && __3.Value.TotalSeconds > 0, "native spawn action calculated finite duration"); var area = Area(__0, __1); Set(area, "m_Duration", __3); spawned++;
        // Area controller's Unity object/view allocation is the boundary.
        // Native action's post-spawn UI/shape iteration requires that view.
        __result = null; return false;
    }
    static KineticInvocationState01433 State(Feature feat, out EntityFactComponent runtime) {
        runtime = feat.Components.Single(r => r.SourceBlueprintComponent is KineticInvocationLifetime01433); return runtime.GetData<KineticInvocationState01433>();
    }
    static void Lifetime(BlueprintsCache c, Harmony h) {
        var root = Asset<BlueprintFeature>(c, "KineticInvocationFeat"); var privateAbility = Asset<BlueprintAbility>(c, "KineticInvocationInvigorateAbility"); var nativeAbility = Sources(c)[4]; var areaAbility = Asset<BlueprintAbility>(c, "KineticInvocationSleetStormAbility");
        var u = Unit(c, "lifetime-owner"); var other = Unit(c, "lifetime-other"); var recipient = Unit(c, "lifetime-recipient"); var part = Part(u, c); var otherPart = Part(other, c);
        var ownFact = Add(u, root); var otherFact = Add(other, root); var data = State(ownFact, out var runtime); var otherData = State(otherFact, out var otherRuntime);
        var payload = Probe(c); var ownContext = Cast(privateAbility, u); var otherContext = Cast(privateAbility, other); var normalContext = Cast(nativeAbility, u);
        var own = Buff(recipient, payload, ownContext); var normal = Buff(recipient, payload, normalContext); var foreign = Buff(recipient, payload, otherContext); var permanent = Buff(recipient, payload, ownContext, true);
        Check(data.Buffs.Count == 1 && data.Buffs[0].Fact == own && otherData.Buffs.Count == 1 && otherData.Buffs[0].Fact == foreign, "global native Buff event filters exact caster and private SLA origin, not shared payload");
        var childContext = new MechanicsContext(u, recipient.Descriptor, payload, ownContext); var child = Buff(recipient, payload, childContext);
        Check(data.Buffs.Any(r => r.Fact == child), "private origin is followed through native parent-child contexts");
        var bare = Unit(c, "no-feat"); var barePart = Part(bare, c); var bareEffect = Buff(recipient, payload, Cast(privateAbility, bare)); barePart.AcceptBurn(1, new AbilityData(privateAbility, bare.Descriptor)); barePart.ClearAcceptedBurn();
        Check(!bareEffect.IsDisposed && !data.Buffs.Any(r => r.Fact == bareEffect), "no feat and other caster do not enter owner's cleanup graph");
        var nativeArea = new BlueprintAbilityAreaEffect { name = "01433SharedAreaPayload", AssetGuid = Id("01433SharedAreaPayload"), ComponentsArray = Array.Empty<BlueprintComponent>() }; nativeArea.OnEnable(); c.AddCachedBlueprint(nativeArea.AssetGuid, nativeArea);
        var ownArea = Area(Cast(areaAbility, u), nativeArea); var foreignArea = Area(Cast(areaAbility, other), nativeArea); var originalArea = Area(Cast(Sources(c)[6], u), nativeArea); var permanentArea = Area(Cast(areaAbility, u), nativeArea, true);
        Check(data.Areas.Count == 1 && data.Areas[0].Entity == ownArea && otherData.Areas.Count == 1, "native global Area events retain only finite own invocations");
        part.AcceptBurn(2, new AbilityData(privateAbility, u.Descriptor)); part.HealBurn(1); part.ClearAcceptedBurnThisRound(); part.AcceptBurn(1, new AbilityData(privateAbility, u.Descriptor));
        Check(!own.IsDisposed && !ownArea.DestroyMark && part.AcceptedBurn == 2, "partial heal and increasing burn never remove active effects");
        otherPart.AcceptBurn(1, new AbilityData(privateAbility, other.Descriptor)); otherPart.ClearAcceptedBurn();
        Check(foreign.IsDisposed && foreignArea.DestroyMark && !own.IsDisposed && !ownArea.DestroyMark, "other caster ClearAcceptedBurn removes only their exact effects");
        part.ClearAcceptedBurn();
        Check(own.IsDisposed && child.IsDisposed && ownArea.DestroyMark && data.Buffs.Count == 0 && data.Areas.Count == 0 && data.PendingBuffs.Count == 0 && data.PendingAreas.Count == 0, "actual unmodified native ClearAcceptedBurn/EventBus removes own Buffs and marks own Area");
        Check(!normal.IsDisposed && !permanent.IsDisposed && !bareEffect.IsDisposed && !originalArea.DestroyMark && !permanentArea.DestroyMark, "ordinary spell, permanent effect and other caster boundaries survive cleanup");
        var transmutation = Asset<BlueprintAbility>(c, "KineticInvocationFluidFormAbility"); var transmutationBuff = Buff(recipient, payload, Cast(transmutation, u));
        Check(transmutation.School == SpellSchool.Transmutation && (int)AccessTools.Method(typeof(ScalesAndSkinEffect), "Value").Invoke(null, new object[] { transmutationBuff }) == 2, "actual Scales and Skin recognizes retained kinetic Transmutation SLA at caster level12");
        recipient.Facts.Remove(transmutationBuff);
        faultBuff = Buff(recipient, payload, ownContext); var good = Buff(recipient, payload, ownContext); removeFaults = 1; part.AcceptBurn(1, new AbilityData(privateAbility, u.Descriptor)); part.ClearAcceptedBurn();
        Check(!faultBuff.IsDisposed && good.IsDisposed && data.PendingBuffs.Count == 1 && data.PendingBuffs[0].Fact == faultBuff && !data.Clearing, "one cleanup exception is isolated; exact failed instance remains queued");
        var later = Buff(recipient, payload, ownContext); part.AcceptBurn(1, new AbilityData(privateAbility, u.Descriptor)); new Kingmaker.Controllers.Units.UnitTicksController().TickNextRound(u);
        Check(faultBuff.IsDisposed && !later.IsDisposed && data.PendingBuffs.Count == 0 && data.Buffs.Any(r => r.Fact == later), "native round retry does not kill a later cast in a new burn interval");
        part.ClearAcceptedBurn(); Check(later.IsDisposed, "later interval is independently cleaned on its own clear"); faultBuff = null;
        var free = new ZeroCost { Owner = u }; EventBus.Subscribe(free);
        try {
            var privateData = new AbilityData(privateAbility, u.Descriptor); Check(privateAbility.GetComponent<AbilityKineticist>().CalculateCost(privateData) == 0, "legitimate native cost event can reduce invocation to zero"); privateData.Spend();
            var noBurn = Buff(recipient, payload, ownContext); Check(part.AcceptedBurn == 0, "zero-cost invocation leaves native accepted burn zero");
            part.ClearAcceptedBurn(); Check(!noBurn.IsDisposed, "native zero-to-zero Clear emits no cleanup event");
            EventBus.RaiseEvent<IUnitRestHandler>(rest => rest.HandleUnitRest(other)); Check(!noBurn.IsDisposed, "another unit's actual rest event does not clear own zero-cost effect");
            EventBus.RaiseEvent<IUnitRestHandler>(rest => rest.HandleUnitRest(u)); Check(noBurn.IsDisposed, "own native rest event cleans zero-cost invocation");
        } finally { EventBus.Unsubscribe(free); }
        // The native four-argument wrapper delegates to the five-argument
        // allocation endpoint and can already be JIT-inlined by older suites.
        // Bound the actual allocation endpoint, leaving that wrapper intact.
        var actual = Cast(areaAbility, u); var spawnMethod = AccessTools.Method(typeof(AreaEffectsController), "Spawn", new[] { typeof(MechanicsContext), typeof(BlueprintAbilityAreaEffect), typeof(TargetWrapper), typeof(TimeSpan?), typeof(bool) });
        h.Patch(spawnMethod, prefix: new HarmonyMethod(typeof(KineticInvocation01433Tests), nameof(Spawn)));
        try { areaAbility.GetComponent<AbilityEffectRunAction>().Apply(actual, new TargetWrapper(u)); Check(spawned > 0 && data.Areas.Count > 0, "actual private effect Apply and native ContextActionSpawnAreaEffect reach finite own Area event"); }
        finally { h.Unpatch(spawnMethod, HarmonyPatchType.Prefix, h.Id); }
        part.AcceptBurn(1, new AbilityData(privateAbility, u.Descriptor)); part.ClearAcceptedBurn();
        data.PendingBuffs.Add(default(EntityFactRef<Buff>)); data.PendingAreas.Add(default(EntityRef<AreaEffectEntityData>));
        new Kingmaker.Controllers.Units.UnitTicksController().TickNextRound(u); Check(data.PendingBuffs.Count == 0 && data.PendingAreas.Count == 0, "unresolvable pending references are pruned instead of recurring forever");
        var off = Buff(recipient, payload, ownContext); var offArea = Area(Cast(areaAbility, u), nativeArea);
        ownFact.Reapply(); Check(!off.IsDisposed && !offArea.DestroyMark && ownFact.IsActive && ownFact.IsTurnedOn, "native fact Reapply preserves active invocation ownership");
        ownFact.Deactivate(); Check(!off.IsDisposed && !offArea.DestroyMark, "temporary native Deactivate while fact remains in manager preserves effects"); ownFact.Activate();
        u.Facts.TurnOff(); Check(!off.IsDisposed && !offArea.DestroyMark && !ownFact.IsTurnedOn, "native save/unload manager TurnOff preserves invocation ownership and duration");
        u.Facts.TurnOn(); Check(!off.IsDisposed && !offArea.DestroyMark && ownFact.IsTurnedOn, "native manager restart retains the same owned buff and area instances");
        part.AcceptBurn(1, new AbilityData(privateAbility, u.Descriptor)); part.ClearAcceptedBurn(); Check(off.IsDisposed && offArea.DestroyMark, "manager restart retains working native burn-clear subscription");
        SerializeBoundary(c, ownFact, runtime, payload, ownContext, recipient);
        var lost = Buff(recipient, payload, ownContext); var lostArea = Area(Cast(areaAbility, u), nativeArea);
        u.Facts.Remove(ownFact); Check(lost.IsDisposed && lostArea.DestroyMark && ownFact.IsDisposed, "actual native feat removal revokes exact owned Buffs and Areas");
        Check(buffAdded > 0 && buffRemoved > 0 && areaAdded >= 4 && burnEvents > 0, "real native lifetime event counters are nonzero");
    }
    sealed class ReferenceIds : DefaultContractResolver {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization serialization) {
            var result = base.CreateProperties(type, serialization);
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(EntityRef<>) || type.GetGenericTypeDefinition() == typeof(EntityFactRef<>))) {
                result = result.Where(p => p.PropertyName == "Id" || p.PropertyName == "EntityId" || p.PropertyName == "FactId").ToList(); foreach (var p in result) p.Writable = true;
            }
            return result;
        }
    }
    static void SerializeBoundary(BlueprintsCache c, Feature fact, EntityFactComponent runtime, BlueprintBuff payload, MechanicsContext context, UnitEntityData recipient) {
        var data = runtime.GetData<KineticInvocationState01433>(); var tracked = Buff(recipient, payload, context); var areaBp = new BlueprintAbilityAreaEffect { name = "01433SerializeArea", AssetGuid = Id("01433SerializeArea"), ComponentsArray = Array.Empty<BlueprintComponent>() }; var area = Area(context, areaBp);
        var settings = new JsonSerializerSettings { ContractResolver = new ReferenceIds() }; string json = JsonConvert.SerializeObject(data, settings); var parsed = JObject.Parse(json);
        Check(parsed["Clearing"] == null && parsed["Buffs"] is JArray && parsed["Areas"] is JArray && parsed["PendingBuffs"] is JArray && parsed["PendingAreas"] is JArray, "serialized Data contains only durable active/pending typed-reference lists, no transient guard");
        var restored = JsonConvert.DeserializeObject<KineticInvocationState01433>(json, settings);
        Check(restored.Buffs.Select(r => r.FactId).SequenceEqual(data.Buffs.Select(r => r.FactId)) && restored.Areas.Select(r => r.Id).SequenceEqual(data.Areas.Select(r => r.Id)), "Data JSON roundtrip preserves native fact/entity reference IDs");
        // Standalone fixture reconnects the absent EntityService proxy cache by
        // exact saved IDs. This is not the game's complete save deserializer.
        for (int i = 0; i < restored.Buffs.Count; i++) { object boxed = restored.Buffs[i]; Set(boxed, "m_Proxy", Get(recipient, "Proxy")); restored.Buffs[i] = (EntityFactRef<Buff>)boxed; }
        for (int i = 0; i < restored.Areas.Count; i++) { object boxed = restored.Areas[i]; Set(boxed, "m_Proxy", Get(area, "Proxy")); restored.Areas[i] = (EntityRef<AreaEffectEntityData>)boxed; }
        Check(restored.Buffs.Single().Fact == tracked && restored.Areas.Single().Entity == area, "native typed reference getters resolve restored exact instances after scene proxy reconnect");
        // Test the actual post-load component entry against restored pending
        // data; not a direct call to its burn/buff/area notification hooks.
        restored.PendingBuffs.AddRange(restored.Buffs); restored.PendingAreas.AddRange(restored.Areas); restored.Buffs.Clear(); restored.Areas.Clear();
        var componentData = runtime.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(m => m.Name == "SetData" && m.GetParameters().Length == 1).FirstOrDefault();
        if (componentData != null) componentData.Invoke(runtime, new object[] { restored });
        else {
            // Actual runtime data is the storage object attached to the fact;
            // populate its deserialized lists without replacing runtime identity.
            data.Buffs = restored.Buffs; data.Areas = restored.Areas; data.PendingBuffs = restored.PendingBuffs; data.PendingAreas = restored.PendingAreas;
        }
        runtime.ApplyPostLoadFixes();
        Check(tracked.IsDisposed && area.DestroyMark && runtime.GetData<KineticInvocationState01433>().PendingBuffs.Count == 0 && runtime.GetData<KineticInvocationState01433>().PendingAreas.Count == 0, "actual post-load entry retries restored pending ownership correctly");
    }
    internal static int Run(BlueprintsCache parent) {
        checks = serial = appendFault = buffAdded = buffRemoved = areaAdded = areaRemoved = burnEvents = parameterEvents = spawned = removeFaults = 0; units.Clear(); areas.Clear(); parameterObservers.Clear(); faultBuff = null;
        var strings = (Dictionary<string,string>)AccessTools.Field(Factory, "Strings").GetValue(null); var stringsBefore = new Dictionary<string,string>(strings); Dictionary<string,string> authored = null;
        var h = new Harmony("CotW01433.KineticInvocationNative"); var events = new Events(); JObject contract = null;
        // Older suites retain controlled scene endpoints for their remaining
        // cases. Run this independent native suite without consuming those
        // queues, then restore every original Harmony owner and ordering field.
        var outerDiceQueue = (IEnumerable<int>)AccessTools.Field(typeof(Psychic070Tests), "dice").GetValue(null);
        var diceBefore = outerDiceQueue.ToArray();
        var endpoints = new[] {
            new { Method = (MethodBase)AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) }), Prefix = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense") },
            new { Method = (MethodBase)AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), Prefix = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice") },
            new { Method = (MethodBase)AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), Prefix = (MethodInfo)null }
        };
        var suspended = endpoints.SelectMany(e => (Harmony.GetPatchInfo(e.Method)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<HarmonyLib.Patch>())
            .Where(p => e.Prefix != null ? p.PatchMethod == e.Prefix : p.PatchMethod.DeclaringType == typeof(Psychic070Tests))
            .Select(p => new { Original = e.Method, Record = p })).ToArray();
        foreach (var saved in suspended) h.Unpatch(saved.Original, saved.Record.PatchMethod);
        var originalTime = Game.Instance.TimeController; var originalState = Game.Instance.State;
        var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true);
        var scene = Empty<PersistentState>(); scene.PlayerState = player; Set(Game.Instance, "State", scene); Set(Game.Instance, "TimeController", Empty<TimeController>());
        void Patch(MethodBase method, string name) { if (method == null) throw new Exception("Missing invocation native endpoint " + name); h.Patch(method, prefix:new HarmonyMethod(typeof(KineticInvocation01433Tests), name)); }
        Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Clock)); Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(No));
        Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip)); Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No)); Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), nameof(PoolIgnore)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(Controller)); Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsInCombat"), nameof(Yes));
        // Constructed neutral actors do not have a scene action-bar UI. Keep
        // native ability attach/remove; its player-only slot update is skipped
        // by this explicit actor-faction input, never by replacing the method.
        h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"), prefix:new HarmonyMethod(typeof(KineticInvocation01433Tests), nameof(No)) { priority=Priority.First });
        Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), "IsAttackingGreenNPC"), nameof(No));
        Patch(AccessTools.Method(AccessTools.Property(typeof(UnitEntityData), "CombatState").PropertyType, "ReturnToStartingPositionIfNeeded"), nameof(Skip)); Patch(AccessTools.Method(typeof(Buff), "Remove", Type.EmptyTypes), nameof(RemovalFault));
        h.Patch(AccessTools.Method(typeof(EntityFact), "Attach"), prefix:new HarmonyMethod(typeof(KineticInvocation01433Tests), nameof(UniqueId)));
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix:new HarmonyMethod(typeof(KineticInvocation01433Tests), nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler:new HarmonyMethod(typeof(Howl01428Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler:new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.Method(typeof(AbilityData), "Spend"), transpiler:new HarmonyMethod(typeof(KineticInvocation01433Tests), nameof(SpendBoundary)));
        h.Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), prefix:new HarmonyMethod(typeof(KineticInvocation01433Tests), nameof(Expose)));
        Provider01419Tests.PatchLabels(h); EventBus.Subscribe(events);
        var nativeClear = AccessTools.Method(typeof(UnitPartKineticist), "ClearAcceptedBurn"); var clearPatches = Harmony.GetPatchInfo(nativeClear);
        Check(clearPatches == null || clearPatches.Owners.All(o => o != h.Id), "native ClearAcceptedBurn has no replacement or test transpiler");
        try {
            Registration(parent, out var c); authored = strings.Where(p => p.Key.StartsWith("cotw-wrath.psychic.full.KineticInvocation")).ToDictionary(p => p.Key,p => p.Value);
            EligibilityAndCosts(c); Lifetime(c, h); PublishParent(parent);
            var added = Loaded(c).OfType<BlueprintScriptableObject>().Where(b => b.name.StartsWith("CotWWrathPsychicKineticInvocation")).OrderBy(b => b.AssetGuid.ToString()).ToArray();
            Check(added.OfType<BlueprintFeature>().Count() == 8 && added.OfType<BlueprintAbility>().All(a => a.Type == AbilityType.SpellLike), "final one-feat/seven-talent graph has no new spell roots");
            PsychicExpansionTests.Inventory["kineticInvocation01433Contracts"] = contract = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0243"), ["rootFeatCount"] = 1, ["ordinaryFeatCount"] = 1, ["talentCount"] = 7, ["newSpellRootCount"] = 0,
                ["newBlueprints"] = JArray.FromObject(added.Select(b => new { id=b.AssetGuid.ToString(), name=b.name, type=b.GetType().FullName })), ["donorSchoolsAndScalesAndSkin"] = true,
                ["actualNativeCanSelect"] = true, ["nativeBurnCostAndSpend"] = true, ["nativePrepareCastAndRulebookParams"] = true, ["sourceComponentsAndPrivateGraphPreserved"] = true, ["providerAbsentDisabledLate"] = true, ["twoPoolAppendRollback"] = true, ["foreignPartialGraphRejected"] = true, ["publishedRetryRollback"] = true, ["actualParentPublicationBeforeUrgent"] = true,
                ["nativeFactManagerAndEventBus"] = true, ["unmodifiedClearAcceptedBurnEventBus"] = true, ["exactCasterAndPrivateOriginOwnership"] = true, ["parentChildOrigin"] = true, ["nativeSpellSamePayloadPreserved"] = true, ["partialHealAndIncreasePreserveEffects"] = true, ["failedCleanupRetryPreservesLaterCast"] = true, ["zeroCostOwnRestCleanup"] = true, ["missingPendingRefsPruned"] = true, ["dataReferenceIdsRoundTrip"] = true, ["postLoadPendingEntry"] = true, ["nativeManagerRestartPreservesEffects"] = true, ["nativeReapplyAndTemporaryDeactivatePreserveEffects"] = true, ["nativeFeatRemovalRevokesEffects"] = true, ["publishedOptionalFailureIsolated"] = true,
                ["nativeBuffAddedEvents"] = buffAdded, ["nativeBuffRemovedEvents"] = buffRemoved, ["nativeAreaSpawnedEvents"] = areaAdded, ["nativeBurnValueEvents"] = burnEvents, ["nativeRulebookParameterEvents"] = parameterEvents, ["nativeSpawnActionCalls"] = spawned,
                ["boundary"] = "Constructed native units/cache/game state. Real ordinary/wild CanSelect and prerequisites, native AbilityData resource dispatch/Spend and AbilityKineticist cost/restrictions, UnitPartKineticist AcceptBurn/HealBurn/ClearAcceptedBurn and actual unit/global EventBus, native PrepareCast/Rulebook parameter arithmetic, fact-manager activation/subscription, Buff attach/remove events, global finite Area ownership, actual private AbilityEffectRunAction.Apply/ContextActionSpawnAreaEffect duration and actual MarkForDestroy run. No calls directly invoke tested buff/area/burn notification hooks. UI labels/difficulty/time and no-spellbook dead Unity branch are explicit scene boundaries. Probe buff/area payloads isolate lifetime identity, including a normal original-spell origin using exactly the same payload; full donor combat effects remain in existing donor regression suites/game steps. Optional TTT registration shape and finite spell-list membership are controlled provider inputs, not execution of the external DLL. Area view allocation is bounded after native action duration/provenance calculation. Data typed-reference IDs roundtrip through JSON with reference-only contract resolution; fixture reconnects absent EntityService proxies by exact saved IDs, then executes actual post-load component entry. Complete game serialization/UI/FX/animation is a separate tester step." };
            Console.WriteLine("PASS: " + checks + " Kinetic Invocation01433 native contracts."); return checks;
        } catch(Exception e) { Console.WriteLine("KInv primary test failure before fixture cleanup: " + e); throw;
        } finally {
            appendFault = removeFaults = 0; faultBuff = null;
            try { foreach (var u in units.AsEnumerable().Reverse()) foreach (var f in u.Facts.List.ToArray()) if (f.Manager != null) u.Facts.Remove(f); }
            finally { foreach (var observer in parameterObservers) EventBus.Unsubscribe(observer); EventBus.Unsubscribe(events); h.UnpatchAll(h.Id);
                foreach (var saved in suspended) {
                    var p = saved.Record;
                    new Harmony(p.owner).Patch(saved.Original, prefix:new HarmonyMethod(p.PatchMethod) { priority=p.priority, before=p.before, after=p.after, debug=p.debug });
                }
                if (!outerDiceQueue.SequenceEqual(diceBefore)) throw new Exception("Invocation fixture changed the suspended outer ControlledDice queue");
                foreach (var saved in suspended) {
                    var p = saved.Record;
                    if (!(Harmony.GetPatchInfo(saved.Original)?.Prefixes.Any(now => now.PatchMethod == p.PatchMethod && now.owner == p.owner && now.priority == p.priority && now.debug == p.debug &&
                        (now.before ?? Array.Empty<string>()).SequenceEqual(p.before ?? Array.Empty<string>()) && (now.after ?? Array.Empty<string>()).SequenceEqual(p.after ?? Array.Empty<string>())) ?? false))
                        throw new Exception("Invocation fixture failed to restore exact outer prefix metadata: " + p.PatchMethod.Name);
                }
                Set(Game.Instance, "TimeController", originalTime); Set(Game.Instance, "State", originalState); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, parent);
                strings.Clear(); foreach (var p in stringsBefore) strings.Add(p.Key,p.Value); if (authored != null) foreach (var p in authored) if (!strings.ContainsKey(p.Key)) strings.Add(p.Key,p.Value);
                if (!stringsBefore.All(p => strings.TryGetValue(p.Key, out var value) && value == p.Value)) throw new Exception("Kinetic Invocation fixture changed an existing global text value");
                if (contract != null) { contract["outerFixturePrefixesRestored"] = true; contract["outerControlledDiceQueuePreserved"] = true; contract["outerFixtureInputsAndPatchesRestored"] = true; }
            }
        }
    }
}
