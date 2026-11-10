using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using CallOfTheWild.Wrath.Prototype;

// Provider metadata and registration order are source-faithful controlled inputs,
// not a claim that an external mod or a game scene was launched. Native Items,
// CanSelect, nested availability, SelectFeature.Check/Apply and AddSelection run.
internal static class DiscoveryAccess01438Tests
{
    const string ProviderId = "45b72087ac2d4554aec4f44be852eddd";
    const string Wizard = "ba34257984f4c41408ce1dc2004e342e", Arcanist = "52dbfd8505e22f84fad8d702611f60b7";
    const string Basic = "247a4068296e8be42890143f451b4b45", Mythic = "e10c4f18a6c8b4342afe6954bde0587b", WizardFeats = "8c3102c2ff3b69444b139a98521a4899", Loremaster = "689959eef3e972e458b52598dcc2c752";
    static readonly string[] Exploits = { "b8bf3d5023f2d8c428fdf6438cecaea7", "2ba8a0040e0149e9ae9bfcb01a8ff01d", "0a7065e13f4449fa81bfb33199bf7f6a" };
    static readonly string[] Tokens = { "ForestsBlessingDiscovery", "ResilientIllusionsDiscovery", "TimeStutterDiscovery" };
    // The WizardFeat-group members of current Core's actual FeatSelectionSet.
    // Basic and Mythic are unconditional AddAsFeat writes; the General feat fix
    // subsequently removes this parent from LoremasterWizardFeatSelection.
    static readonly string[] FeatPools = { Basic, Mythic, WizardFeats, Loremaster };
    static readonly List<UnitEntityData> units = new List<UnitEntityData>();
    static readonly List<JObject> choices = new List<JObject>();
    static JArray nativeSelectionRows;
    static int checks, serial, nativeItems, nativeChoices, nativeApplies, fault;
    static Type Module => typeof(ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardDiscoveries01435", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Call(Type type, string name, params object[] args) => AccessTools.Method(type, name).Invoke(null, args);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp); return r; }
    static void Check(bool ok, string why) { if (!ok) throw new Exception("DiscoveryAccess01438: " + why); checks++; }
    static bool Skip() => false;
    static bool False(ref bool __result) { __result = false; return false; }
    static bool Controller(ref LevelUpController __result) { __result = null; return false; }
    static bool Clock(ref TimeSpan __result) { __result = TimeSpan.FromHours(1); return false; }
    static void Identity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "038-discovery-fact-" + ++serial); }
    static void Fault(IEnumerable<BlueprintFeatureSelection> __0) { if (fault > 0 && __0.Any(p => p.AssetGuid == BlueprintGuid.Parse(ProviderId))) { fault--; throw new InvalidOperationException("038 post-append access fault"); } }
    static string Diagnostic => (string)AccessTools.Field(Module, "LastOptionalDiagnostic").GetValue(null);
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => Get(p, field) as BlueprintFeatureReference[] ?? Array.Empty<BlueprintFeatureReference>();
    static void Append(BlueprintFeatureSelection p, params BlueprintFeature[] features) => Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { p }, features);
    static void Register(BlueprintsCache c)
    {
        Call(Module, "RegisterForest", c); Call(Module, "RegisterResilient", c);
        Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStutter01437"), "Register", c);
    }
    static BlueprintFeature[] Roots(BlueprintsCache c) => Tokens.Select(t => Asset<BlueprintFeature>(c, t)).ToArray();
    internal static void PrepareNative(BlueprintsCache c)
    {
        WizardDiscoveries01435Tests.PrepareNative(c); TimeStutter01437Tests.PrepareNative(c);
        // The shared extractor deliberately stores the full native core pools
        // in allNativeSelectionPools instead of duplicating requiredNative.
        // Read that same immutable host input, not a fabricated pool shape.
        if (nativeSelectionRows == null) nativeSelectionRows = (JArray)JObject.Parse(System.IO.File.ReadAllText(Environment.GetCommandLineArgs().Last()))["allNativeSelectionPools"];
        foreach (var id in FeatPools.Concat(Exploits)) {
            var row = PsychicExpansionTests.NativeRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id) ?? nativeSelectionRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id);
            var pool = Native<BlueprintFeatureSelection>(c, id);
            if (pool == null || row == null) throw new InvalidOperationException("038 real native selection metadata missing " + id);
            var nativeComponents = (JArray)row["blueprint"]["Data"]["Components"];
            if (pool.ComponentsArray.Length == 0 && nativeComponents.Count > 0) { pool.ComponentsArray = nativeComponents.Select(x => (BlueprintComponent)PsychicExpansionTests.Read(x, typeof(BlueprintComponent))).ToArray(); pool.OnEnable(); }
            foreach (string field in new[] { "Group", "Group2", "Groups", "IgnorePrerequisites", "Obligatory", "Mode" }) {
                var fi = AccessTools.Field(pool.GetType(), field); var value = row["blueprint"]["Data"][field];
                if (value != null) fi.SetValue(pool, PsychicExpansionTests.Read(value, fi.FieldType));
            }
        }
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var field = AccessTools.Field(typeof(Repair01436CallerTests), "OwnIds"); var previous = field.GetValue(null);
        var bard = (string[])AccessTools.Field(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BardMasterpieces01435"), "OwnTokens").GetValue(null);
        var old = previous as HashSet<BlueprintGuid> ?? new HashSet<BlueprintGuid>(Tokens.Take(2).Concat(bard).Select(Id));
        var time = (string[])AccessTools.Field(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStutter01437"), "OwnTokens").GetValue(null);
        field.SetValue(null, new HashSet<BlueprintGuid>(old.Concat(Tokens.Concat(time).Select(Id))));
        try {
            var c = (BlueprintsCache)Call(typeof(Repair01436CallerTests), "Fresh", parent); PrepareNative(c);
            // Earlier broad fixtures intentionally use a reduced BasicFeat
            // seed. For this independent entry case restore the actual native
            // arrays (including DLC5 Extra Arcanist Exploit) only in the fresh
            // cache, before either provider publishes anything.
            foreach (var id in FeatPools.Concat(Exploits)) {
                var row = PsychicExpansionTests.NativeRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id) ?? nativeSelectionRows.First(r => (string)r["blueprint"]["AssetId"] == id);
                var pool = Native<BlueprintFeatureSelection>(c, id);
                foreach (string name in new[] { "m_Features", "m_AllFeatures" }) Set(pool, name, PsychicExpansionTests.Read(row["blueprint"]["Data"][name], typeof(BlueprintFeatureReference[])));
            }
            return c;
        }
        finally { field.SetValue(null, previous); }
    }
    static PrerequisiteClassLevel Class(BlueprintsCache c, string id, Prerequisite.GroupType group)
    {
        var p = new PrerequisiteClassLevel { Level = 1, Group = group }; Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Native<BlueprintCharacterClass>(c, id))); return p;
    }
    static BlueprintFeatureSelection Ttt(BlueprintsCache c, bool addAsFeat)
    {
        var p = new BlueprintFeatureSelection { AssetGuid = BlueprintGuid.Parse(ProviderId), name = "ArcaneDiscoverySelection", IsClassFeature = true,
            Groups = new[] { FeatureGroup.Feat, FeatureGroup.WizardFeat }, ComponentsArray = new BlueprintComponent[] { Class(c, Wizard, Prerequisite.GroupType.All) } };
        Set(p, "m_Features", Array.Empty<BlueprintFeatureReference>()); Set(p, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>()); p.OnEnable(); c.AddCachedBlueprint(p.AssetGuid, p);
        // Knowledge Is Power has no class-level prerequisite of its own. Its
        // runtime CMD/CMB components are outside this entry-only fixture.
        var foreign = new BlueprintFeature { AssetGuid = BlueprintGuid.Parse("1b02398482b94aa9a1c312886178e8c1"), name = "KnowledgeIsPower", Ranks = 1, IsClassFeature = true, ReapplyOnLevelUp = true, ComponentsArray = Array.Empty<BlueprintComponent>() }; foreign.OnEnable(); c.AddCachedBlueprint(foreign.AssetGuid, foreign); Append(p, foreign);
        if (addAsFeat) PublishTttParent(c, p);
        return p;
    }
    static void PublishTttParent(BlueprintsCache c, BlueprintFeatureSelection p)
    {
        foreach (string id in FeatPools) {
            var selection = Native<BlueprintFeatureSelection>(c, id);
            if (id == Basic || id == Mythic || p.Groups.Contains(selection.Group) || p.Groups.Contains(selection.Group2)) Append(selection, p);
        }
        // Current TTT Base Bugfixes/General/FeatSelections.cs:34.
        var loremaster = Native<BlueprintFeatureSelection>(c, Loremaster);
        foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(loremaster, field, Refs(loremaster, field).Where(r => r?.Guid != p.AssetGuid).ToArray());
    }
    static void Ecm(BlueprintsCache c, BlueprintFeatureSelection p)
    {
        // Exact current ECM ArcanistExploits.Configure class-gate change. This
        // intentionally also removes our derived PrerequisiteClassLevel adapter.
        p.ComponentsArray = p.ComponentsArray.Where(x => !(x is PrerequisiteClassLevel)).Concat(new BlueprintComponent[] { Class(c, Wizard, Prerequisite.GroupType.Any), Class(c, Arcanist, Prerequisite.GroupType.Any) }).ToArray();
        p.IgnorePrerequisites = false; p.Obligatory = false; p.OnEnable();
        foreach (var id in Exploits) { var pool = Native<BlueprintFeatureSelection>(c, id); Set(pool, "m_AllFeatures", Refs(pool, "m_AllFeatures").Concat(new[] { Ref<BlueprintFeatureReference>(p) }).GroupBy(r => r.Guid).Select(g => g.First()).ToArray()); }
    }
    static UnitEntityData Unit(BlueprintsCache c, int wizard, int arcanist)
    {
        var u = (UnitEntityData)Call(typeof(FlameJetLifecycle01430Tests), "Unit", c, "038-entry-" + ++serial);
        Set(u, "UniqueId", "038-discovery-unit-" + serial); u.Progression.Classes.Clear();
        if (wizard > 0) u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c, Wizard)) { Level = wizard });
        if (arcanist > 0) u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c, Arcanist)) { Level = arcanist });
        if (u.Progression.Classes.Count == 0) u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c, "610d836f3a3a9ed42a4349b62f002e96")) { Level = 20 });
        Set(u.Progression, "m_Selections", new Dictionary<BlueprintFeatureSelection, FeatureSelectionData>()); u.Stats.Intelligence.BaseValue = 18; units.Add(u); return u;
    }
    static LevelUpState State(UnitEntityData u)
    {
        var state = Empty<LevelUpState>(); Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", u.Progression.Classes.Max(x => x.Level)); state.NextClassLevel = u.Progression.Classes[0].Level; state.SelectedClass = u.Progression.Classes[0].CharacterClass; return state;
    }
    static bool Choice(UnitEntityData u, LevelUpState state, FeatureSelectionState selection, BlueprintFeature feature)
    { nativeChoices++; return new SelectFeature(selection, feature).Check(state, u.Descriptor); }
    static bool Choice(UnitEntityData u, BlueprintFeatureSelection pool, BlueprintFeature feature)
    { var state = State(u); var s = state.AddSelection(null, new FeatureSource(state.SelectedClass), pool, state.NextClassLevel); return Choice(u, state, s, feature); }
    static void Path(BlueprintsCache c, BlueprintFeatureSelection parent, int wizard, int arcanist, bool ecm)
    {
        var u = Unit(c, wizard, arcanist); var state = State(u); var source = new FeatureSource(state.SelectedClass); var s = state.AddSelection(null, source, parent, state.NextClassLevel);
        var p = Native<BlueprintFeatureSelection>(c, ProviderId); var items = parent.ExtractSelectionItems(u.Descriptor, u.Descriptor).ToArray(); nativeItems++;
        Check(items.Count(x => ReferenceEquals(x.Feature, p)) == 1, "actual provider publisher exposes parent once in Items " + parent.name);
        bool legal = Choice(u, state, s, p); bool expect = ecm ? wizard >= 1 || arcanist >= 1 : wizard >= 1 || arcanist >= 10;
        Check(legal == expect, "real native nested parent eligibility " + parent.name + " " + wizard + "/" + arcanist);
        if (!legal) return;
        new SelectFeature(s, p).Apply(state, u.Descriptor); nativeApplies++;
        Check(s.Next != null && ReferenceEquals(s.Next.Parent, s) && ReferenceEquals(s.Next.Selection, p) && ReferenceEquals(s.Next.Source.Blueprint, source.Blueprint), "native SelectFeature.Apply creates exact SubSelection and retains class source");
        var nested = p.ExtractSelectionItems(u.Descriptor, u.Descriptor).ToArray(); nativeItems++;
        foreach (int index in new[] { 0, 1, 2 }) {
            var root = Roots(c)[index]; Check(nested.Count(x => ReferenceEquals(x.Feature, root)) == 1, "native child Items contains each exact published leaf once");
            bool available = Choice(u, state, s.Next, root); int required = new[] { 5, 8, 10 }[index];
            Check(available == (wizard >= required || arcanist >= required * 2), "real native leaf gate uses independent classes " + index + " " + wizard + "/" + arcanist);
            Check(!Choice(u, parent, root), "own leaves never appear as direct ordinary/bonus/exploit choices");
            choices.Add(new JObject { ["parent"] = parent.AssetGuid.ToString(), ["ecm"] = ecm, ["wizard"] = wizard, ["arcanist"] = arcanist, ["leaf"] = root.AssetGuid.ToString(), ["available"] = available });
        }
        var external = nested.Single(x => !Tokens.Any(t => x.Feature.AssetGuid == Id(t))).Feature;
        Check(Choice(u, state, s.Next, external) == (wizard >= 1 || ecm), "ECM native permissions retained; pristine TTT Arcanist extension stays own-only");
    }
    static void Permutations(BlueprintsCache parent)
    {
        foreach (string order in new[] { "TTT", "ECM-before", "ECM-after" }) {
            var c = Fresh(parent); var p = Ttt(c, true);
            if (order == "ECM-before") Ecm(c, p);
            Register(c);
            if (order == "ECM-after") { Check(p.GetComponent<WizardDiscoveryEntry01436>() != null, "pristine TTT first installed the narrow adapter"); Ecm(c, p); Register(c); }
            bool ecm = order != "TTT";
            Check(Diagnostic.StartsWith("Arcane discovery provider selection connected"), "current source-faithful provider shape really connected " + order);
            Check((p.GetComponent<WizardDiscoveryEntry01436>() == null) == ecm, "ECM existing native permission is never replaced by the narrow fallback");
            var components = p.ComponentsArray; var original = components.ToArray(); var references = p.AllFeatures.ToArray(); Register(c);
            Check(ReferenceEquals(components, p.ComponentsArray) && p.ComponentsArray.Zip(original, ReferenceEquals).All(x => x) && references.SequenceEqual(p.AllFeatures), "repeat reconnect preserves external component objects and exact selection identities");
            foreach (var levels in new[] { new[] { 4, 0 }, new[] { 5, 0 }, new[] { 7, 0 }, new[] { 8, 0 }, new[] { 9, 0 }, new[] { 10, 0 }, new[] { 0, 9 }, new[] { 0, 10 }, new[] { 0, 15 }, new[] { 0, 16 }, new[] { 0, 19 }, new[] { 0, 20 }, new[] { 4, 9 }, new[] { 7, 15 }, new[] { 9, 19 } }) {
                Path(c, Native<BlueprintFeatureSelection>(c, Basic), levels[0], levels[1], ecm);
                if (levels[0] > 0) Path(c, Native<BlueprintFeatureSelection>(c, WizardFeats), levels[0], levels[1], ecm);
            }
            if (ecm) { Path(c, Native<BlueprintFeatureSelection>(c, Exploits[0]), 0, 20, true); Path(c, Native<BlueprintFeatureSelection>(c, Exploits[1]), 20, 0, true); }
            var extra = Native<BlueprintFeatureSelection>(c, Exploits[2]); var owner = Unit(c, 0, 20);
            if (ecm) { Check(!Choice(owner, Native<BlueprintFeatureSelection>(c, Basic), extra), "native Extra Arcanist Exploit is unavailable without either original source fact"); var grant = owner.Progression.Features.AddFeature(Native<BlueprintFeatureSelection>(c, Exploits[0])); Check(grant != null, "native exploit source fact exists before testing Extra Arcanist Exploit"); PathWithOwner(c, extra, owner, p); }
            else Check(extra.Items.All(x => !ReferenceEquals(x.Feature, p)), "TTT alone does not invent an Arcanist exploit entry");
            Check(Native<BlueprintFeatureSelection>(c, Loremaster).Items.All(x => !ReferenceEquals(x.Feature, p)), "TTT actual General fix keeps its original Loremaster exclusion");
        }
    }
    static void PathWithOwner(BlueprintsCache c, BlueprintFeatureSelection extra, UnitEntityData owner, BlueprintFeatureSelection p)
    {
        Check(Choice(owner, Native<BlueprintFeatureSelection>(c, Basic), extra), "native ordinary-feat gate accepts Extra Arcanist Exploit only with its actual exploit source fact");
        var state = State(owner); var s = state.AddSelection(null, new FeatureSource(state.SelectedClass), extra, 20);
        Check(extra.ExtractSelectionItems(owner.Descriptor, owner.Descriptor).Any(x => ReferenceEquals(x.Feature, p)) && Choice(owner, state, s, p), "actual Extra Arcanist Exploit Items and native source prerequisites expose existing ECM parent"); nativeItems++;
        new SelectFeature(s, p).Apply(state, owner.Descriptor); nativeApplies++;
        Check(s.Next != null && Roots(c).All(r => Choice(owner, state, s.Next, r)), "actual Extra exploit SubSelection can choose all three level-qualified own leaves");
    }
    static void DeferredAndRollback(BlueprintsCache parent, Harmony h)
    {
        var c = Fresh(parent); Register(c); Check(Native<BlueprintFeatureSelection>(c, ProviderId) == null && Roots(c).All(r => r != null), "absent provider retains saved-feature roots without inventing a menu");
        var p = Ttt(c, false); Register(c); Check(Native<BlueprintFeatureSelection>(c, Basic).Items.All(x => !ReferenceEquals(x.Feature, p)), "unpublished provider root is not mistaken for an actual parent entrance");
        PublishTttParent(c, p); Register(c); Path(c, Native<BlueprintFeatureSelection>(c, Basic), 10, 0, false);
        var refs = new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }; var broken = p.GetComponent<WizardDiscoveryEntry01436>(); broken.Level = 2;
        Append(Native<BlueprintFeatureSelection>(c, Basic), Roots(c)); Register(c);
        Check(Diagnostic.Contains("entry deferred") && ReferenceEquals(refs[0], Get(p, "m_Features")) && ReferenceEquals(refs[1], Get(p, "m_AllFeatures")), "modified adapter defers without stripping already connected real-provider children");
        Check(Native<BlueprintFeatureSelection>(c, Basic).Items.All(x => !Tokens.Any(t => x.Feature.AssetGuid == Id(t))), "deferred provider still cannot leak direct own leaves into ordinary feats");
        Ecm(c, p); Register(c); Check(p.GetComponent<WizardDiscoveryEntry01436>() == null && Roots(c).All(r => p.Items.Any(x => ReferenceEquals(x.Feature, r))), "later real ECM replacement recovers a changed adapter without adopting it");
        var actual = p.ComponentsArray; var unknown = Class(c, Wizard, Prerequisite.GroupType.All); unknown.Level = 2; p.ComponentsArray = new BlueprintComponent[] { unknown }; p.OnEnable(); refs = new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }; Register(c);
        Check(Diagnostic.Contains("entry deferred") && ReferenceEquals(p.ComponentsArray[0], unknown) && ReferenceEquals(refs[1], Get(p, "m_AllFeatures")), "unknown schema is retained and deferred, never rewritten as a supported gate"); p.ComponentsArray = actual; p.OnEnable(); Register(c);
        var stat = new PrerequisiteStatValue { Stat = StatType.Intelligence, Value = 20 }; p.ComponentsArray = actual.Concat(new BlueprintComponent[] { stat }).ToArray(); p.OnEnable();
        Check(!Choice(Unit(c, 20, 20), Native<BlueprintFeatureSelection>(c, Basic), p), "unrelated parent stat prerequisite still blocks both supported class branches"); p.ComponentsArray = actual; p.OnEnable();
        var fresh = Fresh(parent); var provider = Ttt(fresh, true); Ecm(fresh, provider); var before = new[] { Get(provider, "m_Features"), Get(provider, "m_AllFeatures"), provider.ComponentsArray };
        h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { typeof(IEnumerable<BlueprintFeatureSelection>), typeof(BlueprintFeature[]) }), postfix: new HarmonyMethod(typeof(DiscoveryAccess01438Tests), nameof(Fault))); fault = 1; Call(Module, "RegisterForest", fresh);
        Check(fault == 0 && ReferenceEquals(Get(provider, "m_Features"), before[0]) && ReferenceEquals(Get(provider, "m_AllFeatures"), before[1]) && ReferenceEquals(provider.ComponentsArray, before[2]), "post-append exception restores exact real ECM parent arrays and components"); Register(fresh);
        Check(Roots(fresh).All(r => provider.Items.Any(x => ReferenceEquals(x.Feature, r))), "later optional retry reconnects all three roots after the isolated fault");
    }
    internal static bool ReproduceOld(BlueprintsCache parent)
    {
        var c = Fresh(parent); var p = Ttt(c, true); Register(c); Ecm(c, p); Register(c);
        return Diagnostic.Contains("original wizard entry prerequisite changed") && Roots(c).All(r => p.Items.All(x => !ReferenceEquals(x.Feature, r)));
    }
    internal static int Run(BlueprintsCache parent)
    {
        checks = nativeItems = nativeChoices = nativeApplies = fault = 0; units.Clear(); choices.Clear();
        var h = new Harmony("CotW038.DiscoveryAccess"); var priorCache = ResourcesLibrary.BlueprintsCache; var game = Game.Instance; var priorState = game.State; var priorTime = game.TimeController;
        var strings = (Dictionary<string, string>)AccessTools.Field(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Strings").GetValue(null); var oldStrings = strings.ToArray();
        var snapshotType = typeof(Repair01436CallerTests).GetNestedType("ParentSnapshot", BindingFlags.NonPublic); var snapshot = Activator.CreateInstance(snapshotType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { parent }, null);
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(DiscoveryAccess01438Tests), name));
        try {
            Provider01419Tests.PatchLabels(h); Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip)); Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(Controller)); Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Clock));
            Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(Identity));
            Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(False)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(False));
            h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardDiscoveryChoice01436Patch", true)).Patch();
            if (Environment.GetEnvironmentVariable("COTW_DISCOVERY_01438_REPRO") == "1") {
                Check(ReproduceOld(parent), "the unchanged delivered037 DLL removes all three leaves after the current real ECM gate replacement");
                PsychicExpansionTests.Inventory["discoveryAccess01438PreFixReproduction"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["reproducedOldEcmRemoval"] = true, ["diagnostic"] = Diagnostic, ["boundary"] = "Delivered037 production DLL with current official TTT then ECM metadata change, using real cache and source-faithful controlled provider registration; no current-user process or new machine log is inferred." };
                Console.WriteLine("PASS: delivered037 ECM access failure reproduced."); return checks;
            }
            Permutations(parent); DeferredAndRollback(parent, h);
            PsychicExpansionTests.Inventory["discoveryAccess01438Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 0, ["newBlueprints"] = new JArray(), ["nativeItemsExtractions"] = nativeItems, ["nativeSelectFeatureChecks"] = nativeChoices, ["nativeSelectFeatureApplies"] = nativeApplies, ["choices"] = JArray.FromObject(choices),
                ["ecmNativeAnyGatePreserved"] = true, ["bothLoadOrders"] = true, ["narrowTttArcanistFallbackPreserved"] = true, ["deferredProviderChildrenPreserved"] = true, ["ordinaryLeafLeakPrevented"] = true, ["postAppendRollback"] = true,
                ["boundary"] = "Exact current TTT/ECM parent metadata and documented registration operations are controlled source-faithful inputs. Native Items/ExtractSelectionItems, CanSelect, nested availability, SelectFeature.Check/Apply and class-source SubSelection execute using installed game assemblies. No external mod process, rendered upgrade UI, save/respec scene or game startup is asserted. Existing discovery effects and Invoker are outside this entry-only repair." };
            Console.WriteLine("PASS: " + checks + " DiscoveryAccess01438 native entry contracts."); return checks;
        } finally {
            foreach (var u in units) foreach (var f in u.Facts.List.ToArray()) if (f.Manager != null) u.Facts.Remove(f);
            h.UnpatchAll(h.Id); strings.Clear(); foreach (var kv in oldStrings) strings.Add(kv.Key, kv.Value); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, priorCache); Set(game, "State", priorState); Set(game, "TimeController", priorTime); units.Clear(); AccessTools.Method(snapshotType, "Verify").Invoke(snapshot, null);
        }
    }
}
