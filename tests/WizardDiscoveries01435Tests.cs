using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
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
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Real native feature lifecycle, selection and Rulebook subscription managers;
// actual native parameter/save/shadow/damage-factor methods execute. Unit/scene
// setup, rule entry scheduling and fixed raw rolls are explicit fixture inputs.
internal static class WizardDiscoveries01435Tests
{
    static readonly Type Module = typeof(ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardDiscoveries01435", true);
    static readonly Type Factory = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    static readonly string[] Tokens = { "ForestsBlessingDiscovery", "ResilientIllusionsDiscovery" };
    static readonly string[] Pools = { "247a4068296e8be42890143f451b4b45", "e10c4f18a6c8b4342afe6954bde0587b", "8c3102c2ff3b69444b139a98521a4899", "689959eef3e972e458b52598dcc2c752" };
    const string Wizard = "ba34257984f4c41408ce1dc2004e342e", Druid = "610d836f3a3a9ed42a4349b62f002e96", Arcanist = "52dbfd8505e22f84fad8d702611f60b7";
    const string WizardList = "ba0401fdeb4062f40a7aa95b6f07fe89", DruidList = "bad8638d40639d04fa2f80a1cac67d6b", OptionalPool = "45b72087ac2d4554aec4f44be852eddd";
    static readonly List<UnitEntityData> Units = new List<UnitEntityData>();
    static readonly List<RuleSavingThrow> Saves = new List<RuleSavingThrow>();
    static readonly List<JObject> Choices = new List<JObject>();
    static readonly Queue<int> Rolls = new Queue<int>();
    static int checks, serial, parameters, nativeSaves, rawRolls, appendFault, saveFault, selectionCalls, snapshotChecks, providerRefreshFaults;
    static BlueprintFeatureSelection providerRefreshTarget;
    static bool appendOptionalOnly;
    static RulebookEventContext current;
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static object Call(Type t, string n, params object[] args) => AccessTools.Method(t, n).Invoke(null, args);
    static BlueprintGuid Id(string t) => (BlueprintGuid)Call(Factory, "Id", t);
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static T Copy<T>(T value) => (T)AccessTools.Method(Factory, "CopyData").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { value });
    static T Clone<T>(T source) where T : BlueprintScriptableObject { var value = (T)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(source, null); value.ComponentsArray = Copy(source.ComponentsArray); return value; }
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b); return r; }
    static void Check(bool value, string reason) { if (!value) throw new Exception("WizardDiscoveries01435: " + reason); checks++; }
    static void Register(BlueprintsCache c, bool forest) => Call(Module, forest ? "RegisterForest" : "RegisterResilient", c);
    static bool Reject(Action f) { try { f(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e is InvalidOperationException; } }
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static bool Controller(ref LevelUpController __result) { __result = null; return false; }
    static void Identity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "035-discovery-fact-" + ++serial); }
    static bool Context(ref RulebookEventContext __result) { if (current == null) return true; __result = current; return false; }
    static bool Dice(DiceFormula __0, ref int __result) { if (__0.Dice != DiceType.D20) { __result = 0; return false; } if (Rolls.Count == 0) throw new Exception("035 discovery unexpected raw d20"); __result = Rolls.Dequeue(); rawRolls++; return false; }
    static bool D20(ref RuleRollD20 __result) { __result = NativeRule(new RuleRollD20(current.LastEvent<RulebookEvent>().Initiator)); return false; }
    static bool Expose(Exception __1) => throw new Exception("035 discovery native action exception", __1);
    static void AppendFault(IEnumerable<BlueprintFeatureSelection> selections, BlueprintFeature[] feats) { if (appendFault > 0 && (!appendOptionalOnly || selections.Any(p => p.AssetGuid == BlueprintGuid.Parse(OptionalPool))) && feats.Any(f => Tokens.Any(t => f.AssetGuid == Id(t)))) { appendFault--; throw new InvalidOperationException("035 discovery append fault"); } }
    static void ProviderRefreshFault(BlueprintScriptableObject __instance) { if (ReferenceEquals(__instance, providerRefreshTarget) && providerRefreshFaults > 0) { providerRefreshFaults--; throw new InvalidOperationException("036 external provider refresh fault"); } }
    static T NativeRule<T>(T rule) where T : RulebookEvent
    {
        if (rule is RuleSavingThrow && saveFault > 0) { saveFault--; throw new InvalidOperationException("035 native save fault"); }
        var previous = current; current = new RulebookEventContext(); current.PushEvent(rule);
        var initiator = new RulebookSubscriptionManager<IInitiatorRulebookSubscriber>();
        var global = new RulebookSubscriptionManager<IGlobalRulebookSubscriber>();
        foreach (var unit in Units) foreach (var fact in unit.Facts.List.ToArray().Where(f => f.IsActive && f.IsTurnedOn && !f.IsDisposed && (!(f is Buff b) || !b.IsSuppressed)))
            foreach (var runtime in fact.Components) {
                if (unit == rule.Initiator && runtime.SourceBlueprintComponent is IInitiatorRulebookSubscriber i) initiator.Subscribe(i, (ISubscriptionProxy)runtime);
                if (runtime.SourceBlueprintComponent is IGlobalRulebookSubscriber g) global.Subscribe(g, (ISubscriptionProxy)runtime);
            }
        try {
            initiator.OnEventAboutToTrigger(rule); global.OnEventAboutToTrigger(rule);
            if (rule is RuleCalculateAbilityParams) parameters++;
            if (rule is RuleSavingThrow save) { nativeSaves++; Saves.Add(save); }
            rule.OnTrigger(current); initiator.OnEventDidTrigger(rule); global.OnEventDidTrigger(rule); rule.OnDidTrigger(); return rule;
        } finally { current = previous; }
    }
    static T ContextRule<T>(MechanicsContext context, T rule) where T : RulebookEvent { rule.Reason = context; return NativeRule(rule); }
    static IEnumerable<CodeInstruction> RuleBoundary(IEnumerable<CodeInstruction> code)
    {
        foreach (var i in code) {
            if (i.operand is MethodInfo m && m.IsGenericMethod && m.Name == "TriggerRule" && m.DeclaringType == typeof(MechanicsContext)) { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(WizardDiscoveries01435Tests), nameof(ContextRule)).MakeGenericMethod(m.GetGenericArguments()); }
            else if (i.operand is MethodInfo r && r.IsGenericMethod && r.Name == "Trigger" && r.DeclaringType == typeof(Rulebook)) { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(WizardDiscoveries01435Tests), nameof(NativeRule)).MakeGenericMethod(r.GetGenericArguments()); }
            yield return i;
        }
    }
    internal static void PrepareNative(BlueprintsCache cache)
    {
        var ids = Pools.Concat(new[] { Wizard, Druid, Arcanist, WizardList, DruidList, "6717dbaef00c0eb4897a1c908a75dfe5", "870af83be6572594d84d276d7fc583e0", "12fb4a4c22549c74d949e2916a2f0b6a", "1f2e6019ece86d64baa5effa15e81ecc", "caac251ca7601324bbe000372a0a1005", "237427308e48c3341b3d532b9d3a001f", "2d81362af43aeac4387a3d4fced489c3" });
        foreach (string id in ids) {
            var bp = Native<BlueprintScriptableObject>(cache, id); var row = PsychicExpansionTests.NativeRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id);
            if (bp == null) throw new Exception("035 native dependency missing " + id);
            // Native selection arrays are populated from allNativeSelectionPools,
            // not duplicated in requiredNative by the shared extractor.
            if (row == null) continue;
            if (bp.ComponentsArray.Length == 0 && ((JArray)row["blueprint"]["Data"]["Components"]).Count > 0) { bp.ComponentsArray = ((JArray)row["blueprint"]["Data"]["Components"]).Select(c => (BlueprintComponent)PsychicExpansionTests.Read(c, typeof(BlueprintComponent))).ToArray(); bp.OnEnable(); }
            if (bp is BlueprintFeatureSelection selection) foreach (string field in new[] { "Group", "Groups", "IgnorePrerequisites" }) { var fi = AccessTools.Field(selection.GetType(), field); if (fi != null && row["blueprint"]["Data"][field] != null) fi.SetValue(selection, PsychicExpansionTests.Read(row["blueprint"]["Data"][field], fi.FieldType)); }
        }
    }
    static UnitEntityData Unit(BlueprintsCache cache, string label, int level = 8, bool wizard = true)
    {
        var u = (UnitEntityData)Call(typeof(FlameJetLifecycle01430Tests), "Unit", cache, "discovery-" + label);
        Set(u, "UniqueId", "035-discovery-unit-" + ++serial);
        var proxy = FormatterServices.GetUninitializedObject(typeof(EntityService).GetNestedType("Proxy", BindingFlags.Public | BindingFlags.NonPublic)); Set(proxy, "Entity", u); Set(u, "Proxy", proxy);
        u.Progression.Classes.Clear(); u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, wizard ? Wizard : Druid)) { Level = level });
        u.Stats.Intelligence.BaseValue = 18; u.Stats.Wisdom.BaseValue = 10; u.Stats.SaveWill.BaseValue = u.Stats.SaveFortitude.BaseValue = u.Stats.SaveReflex.BaseValue = 0;
        Units.Add(u); return u;
    }
    static Feature Add(UnitEntityData u, BlueprintFeature bp) { var feature = new Feature(bp, u.Descriptor); u.Facts.Add(feature); Check(feature.IsActive && feature.IsTurnedOn && feature.IsSubscribedOnEventBus, "native feature activates/subscribes " + bp.name); return feature; }
    static bool CanSelect(UnitEntityData u, BlueprintFeatureSelection pool, BlueprintFeature root) { selectionCalls++; return (bool)Call(typeof(EnhancementLifecycle01430Tests), "CanSelect", u, pool, root); }
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => (BlueprintFeatureReference[])Get(p, field) ?? Array.Empty<BlueprintFeatureReference>();
    // These fixtures reproduce the exact current official TTT parent metadata.
    // They do not execute the external provider assembly or change its settings.
    internal static BlueprintFeatureSelection InstallProvider01436(BlueprintsCache cache, int shape)
    {
        cache.RemoveCachedBlueprint(BlueprintGuid.Parse(OptionalPool));
        if (shape == 2) return null; // Provider absent / whole mod disabled.
        if (shape == 4) {
            var wrong = new BlueprintFeature { AssetGuid = BlueprintGuid.Parse(OptionalPool), name = "ArcaneDiscoverySelection", ComponentsArray = Array.Empty<BlueprintComponent>() };
            wrong.OnEnable(); cache.AddCachedBlueprint(wrong.AssetGuid, wrong); return null;
        }
        var level = new PrerequisiteClassLevel { Level = 1, Group = Prerequisite.GroupType.All };
        Set(level, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Native<BlueprintCharacterClass>(cache, Wizard)));
        var pool = new BlueprintFeatureSelection { AssetGuid = BlueprintGuid.Parse(OptionalPool), name = "ArcaneDiscoverySelection", IsClassFeature = true,
            Groups = shape == 1 ? Array.Empty<FeatureGroup>() : new[] { FeatureGroup.Feat, FeatureGroup.WizardFeat },
            Group = shape == 1 ? FeatureGroup.WizardFeat : FeatureGroup.None, IgnorePrerequisites = shape == 3,
            ComponentsArray = shape == 5 ? Array.Empty<BlueprintComponent>() : new BlueprintComponent[] { level } };
        Set(pool, "m_Features", Array.Empty<BlueprintFeatureReference>()); Set(pool, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>());
        pool.OnEnable(); cache.AddCachedBlueprint(pool.AssetGuid, pool); return pool;
    }
    static UnitEntityData Classes(BlueprintsCache cache, string label, int wizard, int arcanist)
    {
        var unit = Unit(cache, label); unit.Progression.Classes.Clear();
        if (wizard > 0) unit.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, Wizard)) { Level = wizard });
        if (arcanist > 0) unit.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, Arcanist)) { Level = arcanist });
        if (unit.Progression.Classes.Count == 0) unit.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, Druid)) { Level = 20 });
        return unit;
    }
    static void Append(BlueprintFeatureSelection[] pools, params BlueprintFeature[] roots) => Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", pools, roots);
    static void Registration(BlueprintsCache cache, Harmony h)
    {
        int initialChecks = checks;
        PrepareNative(cache);
        var oldProvider = cache.Load(BlueprintGuid.Parse(OptionalPool));
        var pools = Pools.Select(id => Native<BlueprintFeatureSelection>(cache, id)).ToArray();
        var originalPools = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var extraPool = new BlueprintFeatureSelection { AssetGuid = BlueprintGuid.Parse("583c85ed9be244d7bff580f8b966e366"), name = "036-unrelated-external-pool", ComponentsArray = Array.Empty<BlueprintComponent>() };
        Set(extraPool, "m_Features", Array.Empty<BlueprintFeatureReference>()); Set(extraPool, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>()); extraPool.OnEnable(); cache.AddCachedBlueprint(extraPool.AssetGuid, extraPool);
        try {
            var provider = InstallProvider01436(cache, 0);
            var unrelated = new PrerequisiteStatValue { Stat = StatType.Intelligence, Value = 14 };
            provider.ComponentsArray = provider.ComponentsArray.Concat(new BlueprintComponent[] { unrelated }).ToArray(); provider.OnEnable();
            var originalWizard = provider.GetComponent<PrerequisiteClassLevel>();
            var foreign = new BlueprintFeature { name = "036-provider-discovery-without-class-prerequisite", AssetGuid = BlueprintGuid.Parse("538f1b4a17f44b59b797799cfb8396cc"), Ranks = 1, ComponentsArray = Array.Empty<BlueprintComponent>() }; foreign.OnEnable();
            Append(new[] { provider }, foreign); Append(new[] { pools[0], pools[2] }, provider);
            Register(cache, true); Register(cache, false);
            var roots = Tokens.Select(t => Asset<BlueprintFeature>(cache, t)).ToArray();
            foreach (var root in roots) Check(root.Ranks == 1 && root.Groups.Length == 0 && root.IsClassFeature, "discovery leaf is not a standalone feat: " + root.name);
            Check(roots.All(r => ReferenceEquals(cache.Load(r.AssetGuid), r)), "stable035 discovery identities retained");
            var adapter = provider.GetComponent<WizardDiscoveryEntry01436>();
            Check(adapter != null && provider.ComponentsArray.Count(c => c is WizardDiscoveryEntry01436) == 1 && provider.ComponentsArray.Contains(unrelated), "only audited wizard entry replaced; all unrelated parent prerequisites retained");
            Check(originalWizard.Level == 1 && originalWizard.Group == Prerequisite.GroupType.All && originalWizard.CharacterClass.AssetGuid == BlueprintGuid.Parse(Wizard), "original native wizard entry object not mutated");
            Append(pools.Concat(new[] { extraPool }).ToArray(), roots); // Real035 direct-registration regression shape.
            Register(cache, true); Register(cache, false);
            foreach (var pool in pools.Concat(new[] { extraPool })) foreach (string field in new[] { "m_Features", "m_AllFeatures" })
                Check(!Refs(pool, field).Any(r => Tokens.Any(t => r?.Guid == Id(t))), "old direct leaf refs removed from all loaded external pools: " + pool.name + "/" + field);
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) foreach (var root in roots)
                Check(Refs(provider, field).Count(r => r?.Guid == root.AssetGuid) == 1, "single discovery registration under the actual provider parent");
            // The real, existing opening prefix can repair earlier combat
            // registrations on its first call. Observe that native opening
            // before freezing arrays; never stub or suppress its behavior.
            var warm = Classes(cache, "036-existing-pool-opening", 20, 0);
            foreach (var pool in pools) Check(!CanSelect(warm, pool, roots[0]), "real existing pool-opening repair never restores standalone discovery leaves");
            var post = pools.Select(p => new object[] { p.IgnorePrerequisites, p.Group, p.Groups, Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            var combinations = new[] { new[]{0,0},new[]{0,9},new[]{0,10},new[]{0,15},new[]{0,16},new[]{0,20},new[]{4,9},new[]{7,15},new[]{4,10},new[]{7,16},new[]{5,0},new[]{8,0},new[]{1,20} };
            foreach (var levels in combinations) {
                int wizard = levels[0], arcanist = levels[1]; var unit = Classes(cache, "036-choice-" + wizard + "-" + arcanist, wizard, arcanist);
                bool entry = CanSelect(unit, pools[0], provider);
                Check(entry == (wizard >= 1 || arcanist >= 10), "native outer parent wizard/arcanist access: " + wizard + "/" + arcanist);
                foreach (int i in new[] { 0, 1 }) {
                    bool actual = CanSelect(unit, provider, roots[i]);
                    Check(actual == (wizard >= (i == 0 ? 5 : 8) || arcanist >= (i == 0 ? 10 : 16)), "native leaf OR uses independent class thresholds, never mixed sum: " + wizard + "/" + arcanist + "/" + i);
                    Choices.Add(new JObject { ["pool"] = provider.AssetGuid.ToString(), ["wizard"] = wizard, ["arcanist"] = arcanist, ["token"] = Tokens[i], ["owned"] = false, ["parentCanSelect"] = entry, ["canSelect"] = actual });
                    foreach (var external in pools.Concat(new[] { extraPool })) Check(!CanSelect(unit, external, roots[i]), "actual native external-pool CanSelect rejects unlisted discovery even in Secret: " + external.name);
                }
                bool other = CanSelect(unit, provider, foreign);
                if (entry) Check(other == (wizard >= 1), "Arcanist-only entry does not unlock unrelated provider discoveries; native Wizard path unchanged");
            }
            var pureArcanist=Classes(cache,"036-guard-scope",0,10);var forged=Clone(roots[0]);forged.OnEnable();
            Check(!CanSelect(pureArcanist,provider,forged),"new Arcanist branch requires the exact published root instance, not a forged same-GUID feature");
            Append(new[]{extraPool},foreign);Check(CanSelect(pureArcanist,extraPool,foreign),"narrow discovery guard leaves unrelated selection pools and native classless entries untouched");
            var owner = Classes(cache, "036-already-owned", 0, 16);
            foreach (var root in roots) { var fact = Add(owner, root); Check(!CanSelect(owner, provider, root), "native rank/no-feature prohibits repeated discovery"); owner.Facts.Remove(fact); }
            var low = Classes(cache, "036-owned-forest-at10", 0, 10); var known = Add(low, roots[0]); Check(!CanSelect(low, pools[0], provider), "Arcanist parent has no extra eligibility when the only level-qualified discovery is already owned"); low.Facts.Remove(known);
            foreach (int wizard in new[] { 0, 20 }) { var unit = Classes(cache, "036-parent-stat-" + wizard, wizard, 20); unit.Stats.Intelligence.BaseValue = 10; Check(!CanSelect(unit, pools[0], provider), "unrelated native parent stat prerequisite retained for both class paths"); }
            var blocked = Classes(cache, "036-leaf-stat", 0, 20); var oldComponents = roots[0].ComponentsArray;
            roots[0].ComponentsArray = oldComponents.Concat(new BlueprintComponent[] { new PrerequisiteStatValue { Stat = StatType.Intelligence, Value = 20 } }).ToArray(); roots[0].OnEnable();
            Check(!CanSelect(blocked, provider, roots[0]), "Arcanist allowance never skips unrelated native leaf prerequisites"); roots[0].ComponentsArray = oldComponents; roots[0].OnEnable();
            var fake = Classes(cache, "036-native-fake-wizard", 0, 0);
            var mapping = new Kingmaker.Designers.Mechanics.Facts.ClassLevelsForPrerequisites { Modifier = 1, Summand = 0 };
            Set(mapping, "m_ActualClass", Ref<BlueprintCharacterClassReference>(Native<BlueprintCharacterClass>(cache, Druid))); Set(mapping, "m_FakeClass", Ref<BlueprintCharacterClassReference>(Native<BlueprintCharacterClass>(cache, Wizard)));
            var training = new BlueprintFeature { name = "036-native-fake-wizard-training", AssetGuid = BlueprintGuid.Parse("576b2d95cfb54a829ca0cb78e2a1fe29"), ComponentsArray = new BlueprintComponent[] { mapping } }; training.OnEnable(); var trained = Add(fake, training);
            Check(CanSelect(fake, pools[0], provider) && CanSelect(fake, provider, foreign) && CanSelect(fake, provider, roots[1]), "actual native fake-Wizard levels preserve original entry and non-CotW choices");
            Set(mapping, "m_ForSelection", Ref<BlueprintFeatureSelectionReference>(extraPool)); Check(!CanSelect(fake, pools[0], provider), "native ForSelection mismatch still prevents fake-Wizard entry");
            Set(mapping, "m_ForSelection", Ref<BlueprintFeatureSelectionReference>(pools[0])); Check(CanSelect(fake, pools[0], provider) && CanSelect(fake, provider, foreign), "native scoped fake-Wizard entry stays recognized by the narrow guard"); fake.Facts.Remove(trained);
            Check(pools.Select((p, i) => Equals(p.IgnorePrerequisites, post[i][0]) && Equals(p.Group, post[i][1]) && ReferenceEquals(p.Groups, post[i][2]) &&
                Refs(p,"m_Features").SequenceEqual((BlueprintFeatureReference[])post[i][3]) && Refs(p,"m_AllFeatures").SequenceEqual((BlueprintFeatureReference[])post[i][4])).All(x => x),
                "real native choice checks retain exact external entry objects/order and flags; existing opening prefix can reallocate unchanged array shells");
            var mutated = roots[0].GetComponent<ForestBlessing01435>(); var oldRef = mutated.WizardList; mutated.WizardList = null; Check(Reject(() => Register(cache, true)), "published mutated discovery definition still rejected"); mutated.WizardList = oldRef;
            // Actual post-Append fault, after both array cleanup and the new
            // parent adapter mutation, must restore all previous object arrays.
            provider = InstallProvider01436(cache, 0); Append(pools, roots); var previous = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            var providerBefore = new[] { Get(provider, "m_Features"), Get(provider, "m_AllFeatures") }; var providerComponents = provider.ComponentsArray;
            h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(WizardDiscoveries01435Tests), nameof(AppendFault)));
            appendOptionalOnly = true; appendFault = 1; Register(cache, true); appendOptionalOnly = false;
            Check(pools.Select((p,i) => ReferenceEquals(Get(p,"m_Features"),previous[i][0]) && ReferenceEquals(Get(p,"m_AllFeatures"),previous[i][1])).All(x => x) && ReferenceEquals(provider.ComponentsArray,providerComponents) && ReferenceEquals(Get(provider,"m_Features"),providerBefore[0]) && ReferenceEquals(Get(provider,"m_AllFeatures"),providerBefore[1]), "real post-Append exception rolls back provider components and all pre-cleanup arrays exactly");
            Register(cache, true); Register(cache, false); Check(provider.GetComponent<WizardDiscoveryEntry01436>() != null && roots.All(r => Refs(provider,"m_AllFeatures").Count(x => x?.Guid == r.AssetGuid) == 1), "subsequent real retry rebuilds a single valid parent adapter and both leaves");
            provider=InstallProvider01436(cache,0);var beforeRefresh=provider.ComponentsArray;
            h.Patch(AccessTools.DeclaredMethod(typeof(BlueprintScriptableObject),"OnEnable"),prefix:new HarmonyMethod(typeof(WizardDiscoveries01435Tests),nameof(ProviderRefreshFault)));
            providerRefreshTarget=provider;providerRefreshFaults=2;Register(cache,true);providerRefreshTarget=null;
            Check(providerRefreshFaults==0&&ReferenceEquals(provider.ComponentsArray,beforeRefresh)&&roots.All(r=>ReferenceEquals(cache.Load(r.AssetGuid),r)),"both foreign provider refresh and its rollback refresh exceptions stay isolated from discovery core resources");
            Register(cache,true);Check(provider.GetComponent<WizardDiscoveryEntry01436>()!=null,"real retry recovers after optional native refresh failures");
            foreach (int shape in new[] { 1, 2, 3, 4, 5 }) {
                var optional = InstallProvider01436(cache, shape); var components = optional?.ComponentsArray;
                var arrays = optional == null ? null : new[] { Get(optional,"m_Features"),Get(optional,"m_AllFeatures") };
                Call(Module,"RegisterOptional",cache); Call(Module,"RegisterOptional",cache);
                Check(roots.All(r => ReferenceEquals(cache.Load(r.AssetGuid),r)), "absent or malformed optional provider cannot remove existing saved discovery resources: " + shape);
                if (shape == 1) Check(roots.All(r => Refs(optional,"m_AllFeatures").Count(x => x?.Guid == r.AssetGuid)==1), "Group-only provider metadata accepted with the same real native entry prerequisite");
                else if (optional != null) Check(ReferenceEquals(optional.ComponentsArray,components) && ReferenceEquals(Get(optional,"m_Features"),arrays[0]) && ReferenceEquals(Get(optional,"m_AllFeatures"),arrays[1]), "unsafe optional provider remains structurally untouched: " + shape);
            }
            provider=InstallProvider01436(cache,0);Call(Module,"RegisterOptional",cache);Check(roots.All(r=>Refs(provider,"m_AllFeatures").Count(x=>x?.Guid==r.AssetGuid)==1),"late valid provider connects existing saved identities once");
            CacheSnapshots(cache, h, roots[0], provider);
            PsychicExpansionTests.Inventory["wizardDiscoveryAccess01436Contracts"] = new JObject {
                ["passed"]=true,["assertions"]=checks-initialChecks,["additionalItemCount"]=0,["ordinaryFeatCount"]=0,["repairItemIds"]=new JArray("COTW-0364","COTW-0369"),["newBlueprints"]=new JArray(),
                ["nativeExternalPoolMembershipExcluded"]=true,["nativeWizardOrDoubleArcanistThresholds"]=true,["nativeMixedClassLevelsNotSummed"]=true,["nativeParentAndLeafUnrelatedPrerequisitesRetained"]=true,
                ["nativeOriginalWizardFakeClassAndSelectionScopePreserved"]=true,["nativeArcanistEntryCannotUnlockOtherDiscoveries"]=true,["nativeRankAndOwnedEligibilityBoundaries"]=true,
                ["optionalProviderAbsentMalformedGroupOnlyLateRetry"]=true,["nativePostAppendRestoresComponentsAndAllPoolArrays"]=true,["same035ResourceIdentitiesAndEffectsRetained"]=true,["actualSelectionMatrix"]=new JArray(Choices),
                ["nativeExactRootIdentityAndUnrelatedPoolBoundaries"]=true,
                ["newProductionTypeIdCount"]=1,["newBlueprintCount"]=0,["newCanSelectPrefixCount"]=1,["newUniqueHarmonyTargetCount"]=0,
                ["nativeCanSelectCalls"]=selectionCalls,["nativeCacheSnapshotPasses"]=snapshotChecks,["nativeConcurrentCacheReplacementAndSnapshotFailureIsolation"]=true,
                ["optionalProviderRefreshAndRollbackRefreshFailureIsolated"]=true,
                ["boundary"]="Native CanSelect, class/stat/no-feature prerequisites, original native fake-class mapping and provider parent component paths execute. The provider fixture reproduces official TTT metadata; no provider assembly, live leveling UI or complete save serialization is executed." };
        } finally {
            cache.RemoveCachedBlueprint(extraPool.AssetGuid); cache.RemoveCachedBlueprint(BlueprintGuid.Parse(OptionalPool)); if(oldProvider!=null)cache.AddCachedBlueprint(oldProvider.AssetGuid,oldProvider);
            for(int i=0;i<pools.Length;i++){Set(pools[i],"m_Features",originalPools[i][0]);Set(pools[i],"m_AllFeatures",originalPools[i][1]);}
        }
    }
    static void CacheSnapshots(BlueprintsCache cache, Harmony h, BlueprintFeature root, BlueprintFeatureSelection provider)
    {
        var loaded = (IDictionary)Get(cache, "m_LoadedBlueprints"); object entry = loaded[root.AssetGuid];
        bool oldFailed = false; int first = 0;
        try { cache.ForEachLoaded((id, bp) => { if (first++ == 0) loaded[root.AssetGuid] = entry; }); }
        catch (InvalidOperationException) { oldFailed = true; }
        Check(oldFailed, "real native ForEachLoaded reproduces unlocked replacement invalidation without any blueprint loading");
        int writes = 0; var stop = new ManualResetEvent(false); var started = new ManualResetEvent(false);
        var worker = new Thread(() => { started.Set(); while (!stop.WaitOne(0)) { loaded[root.AssetGuid] = entry; Interlocked.Increment(ref writes); Thread.Yield(); } });
        worker.Start(); started.WaitOne();
        try { for (int i=0;i<8;i++) { Register(cache, true); snapshotChecks++; Check(((string)AccessTools.Field(Module,"LastOptionalDiagnostic").GetValue(null)).StartsWith("Arcane discovery provider selection connected"), "actual cache snapshot/reconcile succeeds during unlocked native cache replacements"); } }
        finally { stop.Set(); worker.Join(); stop.Dispose(); started.Dispose(); }
        Check(writes > 0 && Equals(loaded[root.AssetGuid], entry) && ReferenceEquals(Get(loaded[root.AssetGuid], "Blueprint"), root), "concurrent cache fixture preserves the exact native entry fields after real writes");
        var arrays = new[] { Get(provider,"m_Features"), Get(provider,"m_AllFeatures") }; var components = provider.ComponentsArray;
        var published=AccessTools.Field(Module,"Published").GetValue(null);var lookup=new object[]{cache,null};
        Check((bool)AccessTools.Method(published.GetType(),"TryGetValue").Invoke(published,lookup),"real optional reconciliation uses the actual published cache state");
        var gate=Get(cache,"m_Lock");Set(cache,"m_Lock",null);
        try { Call(Module,"ReconcileAccess",cache,lookup[1],root); }
        finally { Set(cache,"m_Lock",gate); }
        Check(ReferenceEquals(cache.Load(root.AssetGuid),root) && ReferenceEquals(provider.ComponentsArray,components) && ReferenceEquals(Get(provider,"m_Features"),arrays[0]) && ReferenceEquals(Get(provider,"m_AllFeatures"),arrays[1]) && ((string)AccessTools.Field(Module,"LastOptionalDiagnostic").GetValue(null)).Contains("cache snapshot unavailable"), "actual optional reconciliation isolates a temporarily unavailable cache snapshot and preserves resources, components and arrays");
        Register(cache,true);Check(((string)AccessTools.Field(Module,"LastOptionalDiagnostic").GetValue(null)).StartsWith("Arcane discovery provider selection connected"), "subsequent actual retry recovers after isolated snapshot failure");
    }
    static RuleCalculateAbilityParams Params(UnitEntityData caster, BlueprintAbility spell, Spellbook book = null)
    {
        var e = new RuleCalculateAbilityParams(caster, spell, book) { ReplaceCasterLevel = 8, ReplaceSpellLevel = 3, ReplaceDC = 16, ReplaceStatBonusModifier = 4 };
        return NativeRule(e);
    }
    static void Forest(BlueprintsCache cache)
    {
        var caster = Unit(cache, "forest-caster"); var other = Unit(cache, "forest-other"); var fact = Add(caster, Asset<BlueprintFeature>(cache, Tokens[0]));
        var wl = Native<BlueprintSpellList>(cache, WizardList); var dl = Native<BlueprintSpellList>(cache, DruidList);
        var both = wl.SpellsByLevel.SelectMany(l => l.Spells).Where(s => s != null && s.name != "NativeSpell" && s.Type == AbilityType.Spell && dl.Contains(s)).Distinct().ToArray();
        var wizardOnly = wl.SpellsByLevel.SelectMany(l => l.Spells).First(s => s != null && s.name != "NativeSpell" && s.Type == AbilityType.Spell && !dl.Contains(s));
        var druidOnly = dl.SpellsByLevel.SelectMany(l => l.Spells).First(s => s != null && s.name != "NativeSpell" && s.Type == AbilityType.Spell && !wl.Contains(s));
        Check(both.Length > 0, "real native Wizard/Druid spell intersection exists");
        foreach (var spell in both.Take(25)) { var e = Params(caster, spell); Check(e.Result.CasterLevel == 9 && e.Result.DC == 17, "native parameter rule shared-list spell " + spell.name); }
        foreach (var spell in new[] { wizardOnly, druidOnly }) { var e = Params(caster, spell); Check(e.Result.CasterLevel == 8 && e.Result.DC == 16, "single-list spell unchanged " + spell.name); }
        var chosen = both[0]; Check(Params(other, chosen).Result.CasterLevel == 8, "another caster cannot borrow owned discovery");
        Console.WriteLine("035 Forest examples shared=" + string.Join(",", both.Take(8).Select(s => s.name)) + "; wizardOnly=" + wizardOnly.name + "; druidOnly=" + druidOnly.name);
        var duplicateRoot = Clone(Asset<BlueprintFeature>(cache, Tokens[0])); duplicateRoot.AssetGuid = BlueprintGuid.Parse("7ef8ad44fd36468f8850c1ad37aa08be"); duplicateRoot.name = "035-independent-Forest-grant"; duplicateRoot.OnEnable(); var duplicate = Add(caster, duplicateRoot);
        Check(Params(caster, chosen).Result.CasterLevel == 9 && Params(caster, chosen).Result.DC == 17, "multiple actual subscribed sources apply once per parameter rule"); caster.Facts.Remove(duplicate);
        var oldType = chosen.Type; try { foreach (var type in new[] { AbilityType.SpellLike, AbilityType.Supernatural, AbilityType.Extraordinary }) { chosen.Type = type; Check(Params(caster, chosen).Result.CasterLevel == 8, "nonspell ability excluded " + type); } } finally { chosen.Type = oldType; }
        var child = Clone(chosen); child.AssetGuid = BlueprintGuid.Parse("67aac0fdfb824d7a96f5346882d125cb"); child.name = "035-discovery-valid-variant"; Set(child, "m_Parent", Ref<BlueprintAbilityReference>(chosen)); child.OnEnable();
        Check(Params(caster, child).Result.CasterLevel == 8, "unlisted arbitrary Parent does not borrow qualification");
        var components = chosen.ComponentsArray; try { var variants = new AbilityVariants(); Set(variants, "m_Variants", new[] { Ref<BlueprintAbilityReference>(child) }); chosen.ComponentsArray = components.Concat(new[] { variants }).ToArray(); chosen.OnEnable(); Check(Params(caster, child).Result.CasterLevel == 9, "actual listed native-shaped variant uses parent spell intersection"); } finally { chosen.ComponentsArray = components; chosen.OnEnable(); }
        var oldWizardRows = wl.SpellsByLevel; var oldDruidRows = dl.SpellsByLevel;
        SpellLevelList[] Appended(SpellLevelList[] rows, BlueprintAbility spell) {
            var result = (SpellLevelList[])rows.Clone(); var row = new SpellLevelList(1); Set(row, "m_Spells", rows[1].SpellsRefs.Concat(new[] { Ref<BlueprintAbilityReference>(spell) }).ToList()); result[1] = row; return result;
        }
        try {
            wl.SpellsByLevel = Appended(oldWizardRows, child); dl.SpellsByLevel = Appended(oldDruidRows, child);
            Check(Params(caster, child).Result.CasterLevel == 9, "explicitly listed variant qualifies directly without borrowing unrelated Parent");
            wl.SpellsByLevel = oldWizardRows; dl.SpellsByLevel = Appended(oldDruidRows, wizardOnly);
            Check(Params(caster, wizardOnly).Result.CasterLevel == 9, "actual later list addition is read from current native list rows");
        } finally { wl.SpellsByLevel = oldWizardRows; dl.SpellsByLevel = oldDruidRows; }
        Check(ReferenceEquals(wl.SpellsByLevel, oldWizardRows) && ReferenceEquals(dl.SpellsByLevel, oldDruidRows) && Params(caster, wizardOnly).Result.CasterLevel == 8, "native list arrays and original qualification restored exactly");
        var bookBlueprint = new BlueprintSpellbook { name = "035-other-book", CastingAttribute = StatType.Wisdom, ComponentsArray = Array.Empty<BlueprintComponent>() }; bookBlueprint.OnEnable(); var book = Empty<Spellbook>(); Set(book, "Blueprint", bookBlueprint);
        Check(Params(caster, chosen, book).Result.CasterLevel == 9, "same shared spell from another spellbook qualifies");
        fact.TurnOff(); Check(Params(caster, chosen).Result.CasterLevel == 8, "direct native TurnOff disables discovery"); fact.TurnOn(); Check(Params(caster, chosen).Result.CasterLevel == 9, "direct native TurnOn restores discovery");
        caster.Facts.TurnOff(); Check(Params(caster, chosen).Result.CasterLevel == 8, "manager TurnOff disables discovery"); caster.Facts.TurnOn(); Check(Params(caster, chosen).Result.CasterLevel == 9, "manager TurnOn restores discovery");
        caster.Facts.Remove(fact); Check(Params(caster, chosen).Result.CasterLevel == 8, "native removal removes bonus");
    }
    static AbilityExecutionContext Cast(BlueprintAbility spell, UnitEntityData caster, UnitEntityData target, int dc = 16, int cl = 8) => new AbilityExecutionContext(new AbilityData(spell, caster.Descriptor), new AbilityParams { CasterLevel = cl, DC = dc }, new TargetWrapper(target), null, caster);
    static RuleSavingThrow Save(MechanicsContext context, UnitEntityData target, SavingThrowType type, int checkRoll, int savingRoll, bool qualifies)
    {
        Rolls.Clear(); if (qualifies) Rolls.Enqueue(checkRoll); Rolls.Enqueue(savingRoll);
        var e = new RuleSavingThrow(target, type, context.Params.DC) { Reason = context }; NativeRule(e);
        Check(Rolls.Count == 0, "expected exact check/save dice usage"); return e;
    }
    static void Resilient(BlueprintsCache cache, Harmony h)
    {
        var caster = Unit(cache, "illusion-caster"); var target = Unit(cache, "illusion-target"); var other = Unit(cache, "illusion-other"); var feature = Add(caster, Asset<BlueprintFeature>(cache, Tokens[1]));
        var ids = (string[])AccessTools.Field(Module, "PhantasmalIds").GetValue(null);
        foreach (string id in ids) foreach (int checkRoll in new[] { 1, 5, 10, 20 }) foreach (int saveRoll in new[] { 1, 10, 20 }) {
            var context = Cast(Native<BlueprintAbility>(cache, id), caster, target); var e = Save(context, target, SavingThrowType.Will, checkRoll, saveRoll, true);
            Check(e.DifficultyClass == Math.Max(16, checkRoll + 8), "exact marked Will disbelief DC " + id + "/" + checkRoll);
            Check(e.IsPassed == (saveRoll == 20), "native natural1/20 and intermediate save semantics " + saveRoll);
            Check(context.Params.DC == 16, "shared spell context DC never mutated");
        }
        var phantasmal = Native<BlueprintAbility>(cache, ids[0]); var cast = Cast(phantasmal, caster, target);
        Rolls.Clear(); Rolls.Enqueue(10); var noContext = NativeRule(new RuleSavingThrow(target, SavingThrowType.Will, 16)); Check(noContext.DifficultyClass == 16 && Rolls.Count == 0, "missing rule reason/context safely excluded");
        Rolls.Clear(); Rolls.Enqueue(20); Rolls.Enqueue(10); var high = new RuleSavingThrow(target, SavingThrowType.Will, 100) { Reason = cast }; high.AddBonusDC(5); NativeRule(high); Check(high.DifficultyClass == 105 && Rolls.Count == 0, "existing DC modifiers remain and low caster check cannot reduce final DC");
        Rolls.Clear(); Rolls.Enqueue(20); Rolls.Enqueue(20); int applySaves = nativeSaves;
        phantasmal.GetComponent<AbilityEffectRunAction>().Apply(cast, target);
        Check(nativeSaves == applySaves + 1 && Saves.Last().DifficultyClass == 28 && Saves.Last().IsPassed && Rolls.Count == 0, "actual native Phantasmal Killer Apply generates boosted Will before unchanged successful payload");
        var duplicateRoot = Clone(Asset<BlueprintFeature>(cache, Tokens[1])); duplicateRoot.AssetGuid = BlueprintGuid.Parse("bbfeaa3c2f1d4680a51c5faf1f0aa308"); duplicateRoot.name = "035-independent-Resilient-grant"; duplicateRoot.OnEnable(); var duplicate = Add(caster, duplicateRoot);
        Check(Save(cast, target, SavingThrowType.Will, 20, 10, true).DifficultyClass == 28, "two subscribed Resilient sources roll once per save"); caster.Facts.Remove(duplicate);
        var fort = Save(cast, target, SavingThrowType.Fortitude, 20, 10, false); Check(fort.DifficultyClass == 16, "Phantasmal Killer Fortitude followup is not disbelief");
        var otherCast = Cast(phantasmal, other, target); Check(Save(otherCast, target, SavingThrowType.Will, 20, 10, false).DifficultyClass == 16, "other caster cannot borrow Resilient");
        var normal = Clone(phantasmal); normal.AssetGuid = BlueprintGuid.Parse("46621eaadea14747b3ec2c03e2ff78f1"); normal.name = "035-ordinary-illusion"; normal.OnEnable();
        Check(Save(Cast(normal, caster, target), target, SavingThrowType.Will, 20, 10, false).DifficultyClass == 16, "unmarked illusion Will excluded");
        var shadow = Cast(Native<BlueprintAbility>(cache, "caac251ca7601324bbe000372a0a1005"), caster, target); shadow.ShadowFactorPercents = 20;
        Rolls.Clear(); Rolls.Enqueue(20); Rolls.Enqueue(10); bool affected = shadow.TryAffectByShadow(target, false);
        Check(affected && Saves.Last().DifficultyClass == 28 && Rolls.Count == 0, "native shadow disbelief creates actual boosted failed Will");
        int oldSaveCount = nativeSaves, oldRaw = rawRolls; Check(shadow.TryAffectByShadow(target, false) && nativeSaves == oldSaveCount && rawRolls == oldRaw, "native repeated same target uses actual saved disbelief state");
        Check(Save(shadow, target, SavingThrowType.Will, 20, 10, false).DifficultyClass == 16, "ordinary save in shadow context is not automatically disbelief");
        var second = Unit(cache, "second-shadow-target"); Rolls.Clear(); Rolls.Enqueue(20); Rolls.Enqueue(20); Check(!shadow.TryAffectByShadow(second, false) && Saves.Last().IsPassed && Rolls.Count == 0, "second target has its own disbelief check and natural20");
        var damageContext = Cast(Native<BlueprintAbility>(cache, "caac251ca7601324bbe000372a0a1005"), caster, target); damageContext.ShadowFactorPercents = 20;
        var damage = new RuleDealDamage(caster, target, new DamageBundle(new EnergyDamage(new ModifiableDiceFormula(DiceFormula.Zero), 50, DamageEnergyType.Fire))) { Reason = damageContext };
        Rolls.Clear(); Rolls.Enqueue(20); Rolls.Enqueue(10);
        Check(ShadowSpellLogicExtension.TryApplyShadowDamageFactor(damage, target, 50) == 50 && Saves.Last().DifficultyClass == 28 && Rolls.Count == 0, "native damage shadow helper reaches exact boosted disbelief path");
        var converted = new AbilityData(Native<BlueprintAbility>(cache, "2d81362af43aeac4387a3d4fced489c3"), caster.Descriptor) { ConvertedFrom = new AbilityData(Native<BlueprintAbility>(cache, "237427308e48c3341b3d532b9d3a001f"), caster.Descriptor) };
        var convertedContext = new AbilityExecutionContext(converted, new AbilityParams { CasterLevel = 8, DC = 16 }, target, null, caster); convertedContext.ShadowFactorPercents = 20;
        Check(converted.ShadowSpellSettings != null && convertedContext.SpellSchool == SpellSchool.Evocation, "native Shadow Evocation conversion keeps emulated Fireball school");
        var convertedDamage = new RuleDealDamage(caster, target, new DamageBundle(new EnergyDamage(new ModifiableDiceFormula(DiceFormula.Zero), 50, DamageEnergyType.Fire))) { Reason = convertedContext };
        Rolls.Clear(); Rolls.Enqueue(20); Rolls.Enqueue(10);
        Check(ShadowSpellLogicExtension.TryApplyShadowDamageFactor(convertedDamage, target, 50) == 50 && Saves.Last().DifficultyClass == 28 && Rolls.Count == 0, "actual converted Evocation payload receives illusion disbelief increase");
        var faulted = Cast(Native<BlueprintAbility>(cache, "caac251ca7601324bbe000372a0a1005"), caster, target); faulted.ShadowFactorPercents = 20; saveFault = 1;
        Check(Reject(() => faulted.TryAffectByShadow(target, false)), "exception through native shadow method preserved");
        Check(Save(faulted, target, SavingThrowType.Will, 20, 10, false).DifficultyClass == 16, "Harmony finalizer removes scope after native exception");
        feature.TurnOff(); Check(Save(cast, target, SavingThrowType.Will, 20, 10, false).DifficultyClass == 16, "direct TurnOff removes Resilient"); feature.TurnOn(); Check(Save(cast, target, SavingThrowType.Will, 20, 10, true).DifficultyClass == 28, "direct TurnOn restores Resilient");
        caster.Facts.TurnOff(); Check(Save(cast, target, SavingThrowType.Will, 20, 10, false).DifficultyClass == 16, "manager TurnOff removes Resilient"); caster.Facts.TurnOn(); Check(Save(cast, target, SavingThrowType.Will, 20, 10, true).DifficultyClass == 28, "manager TurnOn restores Resilient");
        caster.Facts.Remove(feature); Check(Save(cast, target, SavingThrowType.Will, 20, 10, false).DifficultyClass == 16, "removal removes Resilient");
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = parameters = nativeSaves = rawRolls = appendFault = saveFault = selectionCalls = snapshotChecks = providerRefreshFaults = 0; providerRefreshTarget = null; appendOptionalOnly = false; current = null; Units.Clear(); Saves.Clear(); Rolls.Clear(); Choices.Clear();
        var h = new Harmony("CotW01435.DiscoveriesNative"); var game = Game.Instance; var previousTime = Get(game, "TimeController"); var previousState = game.State;
        var dice = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }); var outerControlled = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice");
        var suspended = (Harmony.GetPatchInfo(dice)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<HarmonyLib.Patch>()).Where(p => p.PatchMethod == outerControlled).ToArray();
        var outerQueue = (ICollection)AccessTools.Field(typeof(Psychic070Tests), "dice").GetValue(null); var outerValues = outerQueue.Cast<int>().ToArray(); JObject contract = null;
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(WizardDiscoveries01435Tests), name));
        try {
            foreach (var p in suspended) h.Unpatch(dice, p.PatchMethod);
            Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip)); Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(Controller));
            Patch(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), nameof(Skip)); Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(Identity));
            Patch(AccessTools.Method(AccessTools.Property(typeof(UnitEntityData), "CombatState").PropertyType, "ReturnToStartingPositionIfNeeded"), nameof(Skip));
            Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor), "IsPlayerFaction"), nameof(No)); Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper), "IsAttackingGreenNPC"), nameof(No));
            Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), nameof(Context)); Patch(dice, nameof(Dice)); Patch(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), nameof(D20));
            Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(No));
            Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(Expose));
            Provider01419Tests.PatchLabels(h);
            var timeField = AccessTools.Field(typeof(Game), "TimeController"); Set(game, "TimeController", FormatterServices.GetUninitializedObject(timeField.FieldType)); Patch(AccessTools.PropertyGetter(timeField.FieldType, "GameTime"), nameof(Clock));
            var player = Empty<Player>(); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); player.GameTime = TimeSpan.FromHours(1); var state = Empty<PersistentState>(); state.PlayerState = player; Set(game, "State", state);
            foreach (var m in new[] { AccessTools.Method(typeof(MechanicsContext), "TryAffectByShadow"), AccessTools.Method(typeof(AbilityEffectRunAction), "Apply"), AccessTools.Method(typeof(ContextActionSavingThrow), "RunAction") }) h.Patch(m, transpiler: new HarmonyMethod(typeof(WizardDiscoveries01435Tests), nameof(RuleBoundary)));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
            var scope = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.ResilientDisbeliefScope01435", true);
            h.Patch(AccessTools.Method(typeof(MechanicsContext), "TryAffectByShadow"), prefix: new HarmonyMethod(scope, "Prefix"), finalizer: new HarmonyMethod(scope, "Finalizer"));
            var choose = AccessTools.Method(typeof(BlueprintFeatureSelection), "CanSelect");
            h.Patch(choose, prefix: new HarmonyMethod(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardDiscoveryChoice01436Patch", true), "Prefix"));
            h.Patch(choose, prefix: new HarmonyMethod(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolOpeningPatch", true), "Prefix"));
            Check(Harmony.GetPatchInfo(choose).Prefixes.Count(p => p.owner == h.Id) == 2, "actual new narrow guard and existing combat pool prefix share the original native CanSelect target");
            Registration(cache, h); Forest(cache); Resilient(cache, h);
            ((JObject)PsychicExpansionTests.Inventory["wizardDiscoveryAccess01436Contracts"])["nativeExistingCombatPrefixCooperation"] = true;
            PsychicExpansionTests.Inventory["wizardDiscoveries01435Contracts"] = contract = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 2, ["ordinaryFeatCount"] = 0, ["arcaneDiscoveryCount"] = 2, ["newSpellRootCount"] = 0,
                ["itemIds"] = new JArray("COTW-0364", "COTW-0369"), ["newBlueprints"] = new JArray(Tokens.Select(t => Asset<BlueprintFeature>(cache, t)).Select(b => new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name, ["type"] = b.GetType().FullName })),
                ["nativeParameterRules"] = parameters, ["nativeSavingThrows"] = nativeSaves, ["fixedRawD20Rolls"] = rawRolls,
                ["nativeCorePoolPreservationAndClassLevelSelection"] = true, ["nativeIndependentPublicationAndRetry"] = true, ["optionalTttAbsentDisabledMalformedAndLate"] = true,
                ["nativeDualListParametersAndOtherSpellbook"] = true, ["realVariantAndUnrelatedParentBoundaries"] = true, ["nativeFactLifecycleAndOtherCasterBoundaries"] = true,
                ["originalFourPhantasmalWillAndFortitudeBoundaries"] = true, ["nativeShadowDisbeliefAndDamageFactorPath"] = true, ["ordinaryShadowWillNotDisbelief"] = true,
                ["nativeNaturalOneTwentyAndDCCannotDecrease"] = true, ["nativePerTargetShadowCacheAndExceptionScopeCleanup"] = true,
                ["nativeConvertedShadowEvocationRetainsEmulatedSchool"] = true, ["multipleOwnedSourcesApplyOnlyOncePerRule"] = true, ["nativePhantasmalAbilityApplyPath"] = true,
                ["nativeExternalPoolMembershipAndDiscoveryClassSelection"] = true, ["nativeDiscoveryOwnedRankBoundaries"] = true, ["nativeExternalPoolNativeFlagsAndArraysPreserved"] = true, ["actualFourPoolChoiceMatrix"] = new JArray(Choices),
                ["boundary"] = "Native selection/prerequisites, feature manager activation/turn off/removal, native runtime subscription proxies, RuleCalculateAbilityParams.OnTrigger and RuleSavingThrow.OnTrigger execute. Native MechanicsContext.TryAffectByShadow and ShadowSpellLogicExtension.TryApplyShadowDamageFactor generate actual disbelief saves and cached per-target state. Rule entry scheduling uses native subscription managers under an explicit fixture bridge. Raw rolls, unit/scene setup, labels and final scene services are controlled endpoints. Full damage HP, upgrade UI, animation and complete save/load are game regression steps. No game process started." };
            Console.WriteLine("PASS: " + checks + " WizardDiscoveries01435 checks."); return checks;
        } finally {
            foreach (var unit in Units) unit.Facts.TurnOff(); h.UnpatchAll(h.Id);
            foreach (var p in suspended) new Harmony(p.owner).Patch(dice, prefix: new HarmonyMethod(p.PatchMethod) { priority = p.priority, before = p.before, after = p.after, debug = p.debug });
            current = null; Set(game, "State", previousState); Set(game, "TimeController", previousTime);
            if (!outerQueue.Cast<int>().SequenceEqual(outerValues)) throw new Exception("035 discovery outer dice inputs changed");
            foreach (var p in suspended) if (!(Harmony.GetPatchInfo(dice)?.Prefixes.Any(n => n.owner == p.owner && n.PatchMethod == p.PatchMethod && n.priority == p.priority && (n.before ?? Array.Empty<string>()).SequenceEqual(p.before ?? Array.Empty<string>()) && (n.after ?? Array.Empty<string>()).SequenceEqual(p.after ?? Array.Empty<string>())) ?? false)) throw new Exception("035 discovery outer dice patch metadata not restored");
            if (contract != null) contract["outerFixtureDiceAndPatchMetadataRestored"] = true;
        }
    }
}
