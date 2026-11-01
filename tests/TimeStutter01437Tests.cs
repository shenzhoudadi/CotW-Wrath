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
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
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
using Kingmaker.UI.MVVM;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

// Native Time Stop JSON, native feature/resource managers and ability/area
// actions execute. World area allocation and UI refresh are scene endpoints.
internal static class TimeStutter01437Tests
{
    static readonly Type Module = typeof(ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStutter01437", true);
    static readonly Type Factory = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    static readonly Type Discovery = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardDiscoveries01435", true);
    const string Root = "TimeStutterDiscovery", Ability = "TimeStutterAbility", Resource = "TimeStutterResource";
    const string NativeRoot = "70e55e5b0ac84af78c084eb6cf47ae06", Host = "f4d026d3bf5a4e8183ded452e8d87260", Area = "22f6aa316f394e7c88814f98227ed421", Effect = "8fff364a212d4b4894b5e222f3696cdd";
    const string Wizard = "ba34257984f4c41408ce1dc2004e342e", Arcanist = "52dbfd8505e22f84fad8d702611f60b7", Provider = "45b72087ac2d4554aec4f44be852eddd";
    static string[] Tokens => (string[])AccessTools.Field(Module, "OwnTokens").GetValue(null);
    static readonly List<UnitEntityData> Units = new List<UnitEntityData>();
    static readonly HashSet<BlueprintsCache> Prepared = new HashSet<BlueprintsCache>();
    static int checks, serial, choices, casts, applications, areaEntries, expirations, resourceSpends, damageImmunities, cacheFaults, appendFaults, definitionFaults, foreignCollisions, areaExits, areaEnds;
    static bool injectCache, injectAppend, injectDefinition;
    static TimeSpan now;
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static object Call(Type t, string n, params object[] a) => AccessTools.Method(t, n).Invoke(null, a);
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Factory, "Id", token);
    static T Native<T>(BlueprintsCache cache, string id) where T : SimpleBlueprint => (T)cache.Load(BlueprintGuid.Parse(id));
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Check(bool ok, string message) { if (!ok) throw new Exception("TimeStutter01437: " + message); checks++; }
    static void Register(BlueprintsCache cache) => Call(Module, "Register", cache);
    static string Definition(object o) => (string)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition", o);
    static bool Rejected(Action action) { try { action(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    static bool Skip() => false;
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Clock(ref TimeSpan __result) { __result = now; return false; }
    static bool Controller(ref LevelUpController __result) { __result = null; return false; }
    static bool Identity(EntityFact __instance) { if (string.IsNullOrEmpty(__instance.UniqueId)) Set(__instance, "UniqueId", "037-time-fact-" + ++serial); return true; }
    static bool UI(ref RootUIContext __result) { __result = Empty<RootUIContext>(); return false; }
    static bool Expose(Exception __1) => throw new Exception("037 native Time Stutter action exception", __1);
    static void Membership(AreaEffectEntityData area) { }
    static void ObserveExit(BlueprintAbilityAreaEffect __instance) { if (__instance.AssetGuid == Id("TimeStutterArea")) areaExits++; }
    static IEnumerable<CodeInstruction> EndMembershipBoundary(IEnumerable<CodeInstruction> input)
    {
        var code = input.ToList(); int replacements = 0;
        var critical = new[] { "HandleUnitExit", "Clear", "MarkForDestroy" }.ToDictionary(n => n, n => code.Count(i => i.operand is MethodInfo m && m.Name == n));
        if (critical.Any(p => p.Value != 1)) throw new Exception("037 native area end cleanup IL changed");
        foreach (var i in code) if (i.operand is MethodInfo m && m.DeclaringType == typeof(AreaEffectEntityData) && m.Name == "UpdateViewAndUnits") { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(TimeStutter01437Tests), nameof(Membership)); replacements++; }
        if (replacements != 1 || critical.Any(p => code.Count(i => i.operand is MethodInfo m && m.Name == p.Key) != p.Value)) throw new Exception("037 area membership endpoint changed native cleanup calls");
        return code;
    }
    static void CacheFault(BlueprintGuid __0) { if (injectCache && __0 == Id(Root)) { injectCache = false; cacheFaults++; throw new InvalidOperationException("037 real post-add cache fault"); } }
    static void DefinitionFault(BlueprintScriptableObject __0) { if (injectDefinition && __0.AssetGuid == Id("TimeStutterArea")) { injectDefinition = false; definitionFaults++; throw new InvalidOperationException("037 real post-commit definition fault"); } }
    static void AppendFault(IEnumerable<BlueprintFeatureSelection> selections, BlueprintFeature[] feats) { if (injectAppend && selections.Any(p => p.AssetGuid == BlueprintGuid.Parse(Provider)) && feats.Any(f => f.AssetGuid == Id(Root))) { injectAppend = false; appendFaults++; throw new InvalidOperationException("037 real post-append provider fault"); } }
    internal static void PrepareNative(BlueprintsCache cache)
    {
        if (!Prepared.Add(cache)) return;
        // The shared reader hydrates only selected spell fields. Copy the rest
        // of these four actual native records, leaving Unity icon loading out.
        foreach (string id in new[] { NativeRoot, Host, Area, Effect }) {
            var bp = cache.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject;
            var rows = PsychicExpansionTests.NativeRows.Where(r => (string)r["blueprint"]["AssetId"] == id).ToArray();
            if (bp == null || rows.Length != 1) throw new Exception("037 actual native Time Stop donor missing " + id);
            foreach (var p in ((JObject)rows[0]["blueprint"]["Data"]).Properties()) {
                if (p.Name == "Components" || p.Name == "m_Icon" || p.Name == "$type") continue;
                var field = AccessTools.Field(bp.GetType(), p.Name);
                if (field != null && !field.IsInitOnly) field.SetValue(bp, PsychicExpansionTests.Read(p.Value, field.FieldType));
            }
            bp.OnEnable();
        }
        var source = Native<BlueprintAbility>(cache, NativeRoot);
        var apply = (ContextActionApplyBuff)source.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        source.AddToElementsList(apply); source.OnEnable();
        // Actual native deserialization registers elements before OnEnable.
        // Also exercise the real lazy getters before our copy is constructed.
        var restrictions = source.Restrictions; var targets = source.TargetRestrictions; var casters = source.CasterRestrictions;
    }
    static UnitEntityData Unit(BlueprintsCache cache, string label, int wizard, int arcanist = 0)
    {
        var u = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", cache, "037-time-" + label, Wizard, Math.Max(1, wizard));
        u.Progression.Classes.Clear();
        if (wizard > 0) u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, Wizard)) { Level = wizard });
        if (arcanist > 0) u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, Arcanist)) { Level = arcanist });
        if (wizard == 0 && arcanist == 0) u.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(cache, "610d836f3a3a9ed42a4349b62f002e96")) { Level = 20 });
        u.Stats.Intelligence.BaseValue = 18; Units.Add(u); return u;
    }
    static Feature Fact(UnitEntityData u, BlueprintFeature root) { var fact = new Feature(root, u.Descriptor); u.Facts.Add(fact); Check(fact.IsActive && fact.IsTurnedOn && fact.IsSubscribedOnEventBus, "native selected feature activates"); return fact; }
    static bool Select(UnitEntityData unit, BlueprintFeatureSelection pool, BlueprintFeature root) { choices++; return (bool)Call(typeof(EnhancementLifecycle01430Tests), "CanSelect", unit, pool, root); }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        // The older full-graph copier normally initializes its exclusion set
        // from Run. This standalone suite supplies that same actual token set
        // temporarily instead of relying on another suite having run first.
        var field = AccessTools.Field(typeof(Repair01436CallerTests), "OwnIds"); var previous = field.GetValue(null);
        var bard = Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BardMasterpieces01435", true);
        var tokens = (string[])AccessTools.Field(bard, "OwnTokens").GetValue(null);
        field.SetValue(null, new HashSet<BlueprintGuid>(tokens.Concat(Tokens).Concat(new[] { "ForestsBlessingDiscovery", "ResilientIllusionsDiscovery" }).Select(Id)));
        try {
            var c = (BlueprintsCache)Call(typeof(Repair01436CallerTests), "Fresh", parent);
            foreach (string token in Tokens) c.RemoveCachedBlueprint(Id(token));
            // Old copier intentionally does not clone fields containing only
            // Element.Owner (a SimpleBlueprint), so a warmed native element
            // list can still alias the parent. Rehydrate these four donors as
            // genuinely new native objects before any new OnEnable call.
            foreach (string id in new[] { NativeRoot, Host, Area, Effect }) {
                var previousDonor = c.Load(BlueprintGuid.Parse(id));
                var freshDonor = (SimpleBlueprint)AccessTools.Method(typeof(PsychicExpansionTests), "NativeCase").MakeGenericMethod(previousDonor.GetType()).Invoke(null, new object[] { id });
                c.AddCachedBlueprint(freshDonor.AssetGuid, freshDonor);
            }
            PrepareNative(c);
            return c;
        } finally { field.SetValue(null, previous); }
    }
    static void Registration(BlueprintsCache cache, Harmony h)
    {
        PrepareNative(cache);
        var originalSource = Native<BlueprintAbility>(cache, NativeRoot); var originalElements = originalSource.ElementsArray; var originalElementItems = originalElements.ToArray();
        var originalOwners = originalElementItems.Select(e => e.Owner).ToArray();
        var originalRestrictions = originalSource.Restrictions; var originalTargets = originalSource.TargetRestrictions; var originalCasters = originalSource.CasterRestrictions;
        Register(cache);
        var copiedAbility = Asset<BlueprintAbility>(cache, Ability); var copiedApply = copiedAbility.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        Check(ReferenceEquals(originalSource.ElementsArray, originalElements) && originalElements.SequenceEqual(originalElementItems) && originalElementItems.Select(e => e.Owner).SequenceEqual(originalOwners), "native donor retains exact registered element list/items/owners");
        Check(copiedAbility.ElementsArray.Count == 1 && ReferenceEquals(copiedAbility.ElementsArray[0], copiedApply) && ReferenceEquals(copiedApply.Owner, copiedAbility) && !ReferenceEquals(copiedApply, originalElementItems[0]), "private action has separate native element ownership and list");
        Check(ReferenceEquals(originalSource.Restrictions, originalRestrictions) && ReferenceEquals(originalSource.TargetRestrictions, originalTargets) && ReferenceEquals(originalSource.CasterRestrictions, originalCasters), "all prewarmed original restriction caches remain untouched");
        Check(copiedAbility.Restrictions.Length == 1 && ReferenceEquals(copiedAbility.Restrictions[0], copiedAbility.GetComponent<AbilityResourceLogic>()), "native copied ability restriction getter includes its own daily resource after donor cache warmup");
        var original = cache.Load(BlueprintGuid.Parse(Provider));
        var pools = new[] { "247a4068296e8be42890143f451b4b45", "e10c4f18a6c8b4342afe6954bde0587b", "8c3102c2ff3b69444b139a98521a4899", "689959eef3e972e458b52598dcc2c752" }.Select(id => Native<BlueprintFeatureSelection>(cache, id)).ToArray();
        var snapshots = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var root = Asset<BlueprintFeature>(cache, Root);
        try {
            var provider = WizardDiscoveries01435Tests.InstallProvider01436(cache, 0);
            Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { pools[0], pools[2] }, new BlueprintFeature[] { provider });
            Register(cache); Register(cache);
            Check(root.Groups.Length == 0 && root.IsClassFeature && root.Ranks == 1, "only an Arcane Discovery child, not a standalone feat");
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                var refs = (BlueprintFeatureReference[])Get(provider, field); Check(refs.Count(r => r.Guid == root.AssetGuid) == 1, "provider exactly one stable child " + field);
                foreach (var pool in pools) Check(!((BlueprintFeatureReference[])Get(pool, field)).Any(r => r.Guid == root.AssetGuid), "ordinary/wizard pools exclude direct Time Stutter leaf");
            }
            var foreign = new BlueprintFeature { name = "037-unrelated-discovery", AssetGuid = BlueprintGuid.Parse("b36e847b63fa4919a2f75ec4557abd5c"), ComponentsArray = Array.Empty<BlueprintComponent>(), Ranks = 1 }; foreign.OnEnable();
            Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append", new[] { provider }, new[] { foreign });
            foreach (var pair in new[] { new[] { 0, 0 }, new[] { 0, 19 }, new[] { 0, 20 }, new[] { 9, 0 }, new[] { 10, 0 }, new[] { 15, 0 }, new[] { 20, 0 }, new[] { 9, 19 }, new[] { 9, 20 }, new[] { 10, 19 } }) {
                var unit = Unit(cache, "selection-" + pair[0] + "-" + pair[1], pair[0], pair[1]);
                bool qualified = pair[0] >= 10 || pair[1] >= 20;
                Check(Select(unit, provider, root) == qualified, "native independent Wizard10 OR Arcanist20 threshold " + string.Join("/", pair));
                foreach (var pool in pools) Check(!Select(unit, pool, root), "native parent pools never present leaf directly");
                // Native leaf CanSelect does not also evaluate its parent.
                // Test the arcanist-only restriction only where the parent is
                // genuinely open, and separately prove outer entry exclusion.
                if (pair[0] > 0 || pair[1] >= 20)
                    Check(Select(unit, provider, foreign) == (pair[0] > 0), "arcanist bridge never opens unrelated discoveries " + string.Join("/", pair));
                else Check(!Select(unit, pools[0], provider), "unqualified arcanist cannot enter the real parent " + string.Join("/", pair));
                if (qualified) { var fact = Fact(unit, root); Check(!Select(unit, provider, root), "owned rank prevents choosing discovery twice"); unit.Facts.Remove(fact); }
            }
            var forged = (BlueprintFeature)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(root, null);
            Check(!Select(Unit(cache, "forged", 0, 20), provider, forged), "arcanist route rejects same-GUID foreign root object");
            var arcanistOwner = Unit(cache, "parent-availability", 0, 20);
            Check(Select(arcanistOwner, pools[0], provider), "native ordinary parent entry reaches qualified discovery");
            var owned = Fact(arcanistOwner, root);
            // The two 036 discoveries remain separately eligible; owning our
            // leaf does not erase or bypass their native qualifications.
            Check(!Select(arcanistOwner, provider, root), "known Time Stutter leaf is exhausted independently of other discoveries");
            arcanistOwner.Facts.Remove(owned);
            var previousComponents = root.ComponentsArray;
            root.ComponentsArray = previousComponents.Concat(new BlueprintComponent[] { new PrerequisiteStatValue { Stat = StatType.Intelligence, Value = 30 } }).ToArray(); root.OnEnable();
            Check(!Select(arcanistOwner, provider, root), "additional unrelated leaf prerequisites still apply"); root.ComponentsArray = previousComponents; root.OnEnable();
            var c = Fresh(cache); var noProvider = c.Load(BlueprintGuid.Parse(Provider)); Check(noProvider == null, "fresh fixture actually omits external provider");
            Register(c); var published = Asset<BlueprintFeature>(c, Root); Check(published != null, "absent provider retains saved discovery definitions");
            var late = WizardDiscoveries01435Tests.InstallProvider01436(c, 0); Register(c);
            Check(ReferenceEquals(Asset<BlueprintFeature>(c, Root), published) && ((BlueprintFeatureReference[])Get(late, "m_AllFeatures")).Count(r => r.Guid == published.AssetGuid) == 1, "late provider uses identical root exactly once");
            var collisionCache = Fresh(cache);
            foreach (string token in Tokens) {
                var expected = (BlueprintScriptableObject)cache.Load(Id(token));
                var foreignRoot = (BlueprintScriptableObject)Activator.CreateInstance(expected.GetType()); foreignRoot.AssetGuid = Id(token); foreignRoot.name = "foreign-same-guid-" + token; foreignRoot.ComponentsArray = Array.Empty<BlueprintComponent>(); foreignRoot.OnEnable(); collisionCache.AddCachedBlueprint(foreignRoot.AssetGuid, foreignRoot);
                Check(Rejected(() => Register(collisionCache)) && ReferenceEquals(collisionCache.Load(foreignRoot.AssetGuid), foreignRoot) && Tokens.Where(t => t != token).All(t => collisionCache.Load(Id(t)) == null), "every foreign graph identity is rejected without claiming or overwriting any asset: " + token); foreignCollisions++;
                if (token == Root) Check(Rejected(() => Call(Module, "RegisterOptional", collisionCache)) && ReferenceEquals(collisionCache.Load(foreignRoot.AssetGuid), foreignRoot), "optional retry cannot falsely report a foreign same-GUID root as registered");
                collisionCache.RemoveCachedBlueprint(foreignRoot.AssetGuid);
            }
            var failed = Fresh(cache); injectCache = true; h.Patch(AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"), postfix: new HarmonyMethod(typeof(TimeStutter01437Tests), nameof(CacheFault)));
            try { Check(Rejected(() => Register(failed)), "actual AddCached post-write exception propagates into transaction"); } finally { injectCache = false; h.Unpatch(AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint"), AccessTools.Method(typeof(TimeStutter01437Tests), nameof(CacheFault))); }
            Check(Tokens.All(t => failed.Load(Id(t)) == null), "failed publication rolls all six private identities back"); Register(failed); Check(Tokens.All(t => failed.Load(Id(t)) != null), "clean retry succeeds after actual rollback");
            var definitionCache = Fresh(cache); injectDefinition = true; var define = AccessTools.Method(Module, "Definition"); h.Patch(define, prefix: new HarmonyMethod(typeof(TimeStutter01437Tests), nameof(DefinitionFault)));
            try { Check(Rejected(() => Register(definitionCache)), "actual post-commit definition exception is rejected before publishing a receipt"); } finally { injectDefinition = false; h.Unpatch(define, AccessTools.Method(typeof(TimeStutter01437Tests), nameof(DefinitionFault))); }
            Check(Tokens.All(t => definitionCache.Load(Id(t)) == null), "definition failure rolls all six cached assets back"); Register(definitionCache); Check(Tokens.All(t => definitionCache.Load(Id(t)) != null), "clean retry follows failed definition without an incomplete publication receipt");
            var providerFault = Fresh(cache); var target = WizardDiscoveries01435Tests.InstallProvider01436(providerFault, 0); var oldComponents = target.ComponentsArray; var oldArrays = new[] { Get(target, "m_Features"), Get(target, "m_AllFeatures") };
            injectAppend = true; var append = AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"); h.Patch(append, postfix: new HarmonyMethod(typeof(TimeStutter01437Tests), nameof(AppendFault)));
            try { Register(providerFault); } finally { injectAppend = false; h.Unpatch(append, AccessTools.Method(typeof(TimeStutter01437Tests), nameof(AppendFault))); }
            Check(ReferenceEquals(target.ComponentsArray, oldComponents) && ReferenceEquals(Get(target, "m_Features"), oldArrays[0]) && ReferenceEquals(Get(target, "m_AllFeatures"), oldArrays[1]), "optional provider post-mutation failure restores components and both exact array objects");
            Register(providerFault); Check(((BlueprintFeatureReference[])Get(target, "m_AllFeatures")).Count(r => r.Guid == Id(Root)) == 1, "provider retry after isolated failure connects once");
            Check(ReferenceEquals(originalSource.ElementsArray, originalElements) && originalElements.SequenceEqual(originalElementItems) && originalElementItems.Select(e => e.Owner).Zip(originalOwners, ReferenceEquals).All(v => v), "every fresh fault/retry cache preserves exact parent donor element ownership by object reference");
        } finally {
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
            cache.RemoveCachedBlueprint(BlueprintGuid.Parse(Provider)); if (original != null) cache.AddCachedBlueprint(original.AssetGuid, original);
            for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", snapshots[i][0]); Set(pools[i], "m_AllFeatures", snapshots[i][1]); }
        }
    }
    static void Resources(BlueprintsCache cache)
    {
        var root = Asset<BlueprintFeature>(cache, Root); var ability = Asset<BlueprintAbility>(cache, Ability); var resource = Asset<BlueprintAbilityResource>(cache, Resource); var logic = ability.GetComponent<AbilityResourceLogic>();
        foreach (var tuple in new[] { new[] { 10, 0, 1 }, new[] { 14, 0, 1 }, new[] { 15, 0, 2 }, new[] { 19, 0, 2 }, new[] { 20, 0, 3 }, new[] { 0, 20, 1 }, new[] { 9, 20, 1 }, new[] { 20, 20, 3 } }) {
            var unit = Unit(cache, "daily-" + string.Join("-", tuple), tuple[0], tuple[1]); var data = new AbilityData(ability, unit.Descriptor);
            Check(!logic.IsAbilityRestrictionPassed(data) && unit.Resources.GetResourceAmount(resource) == 0, "without selected root no free resource");
            var feature = Fact(unit, root); Check(unit.Abilities.Enumerable.Any(a => a.Blueprint == ability), "native AddFacts grants actual spell-like ability");
            Check(resource.GetMaxAmount(unit.Descriptor) == tuple[2] && unit.Resources.GetResourceAmount(resource) == tuple[2], "native daily maximum and initial restore " + string.Join("/", tuple));
            for (int i = tuple[2]; i > 0; i--) {
                Check(logic.IsAbilityRestrictionPassed(data), "native resource restriction permits remaining use");
                AbilityCastRateUtils.SpendResources(data); resourceSpends++;
                Check(unit.Resources.GetResourceAmount(resource) == i - 1, "actual native ability resource caller spends exactly one");
            }
            Check(!logic.IsAbilityRestrictionPassed(data), "native resource restriction rejects exhausted discovery");
            logic.Spend(data); Check(unit.Resources.GetResourceAmount(resource) == 0, "zero resource cannot become negative or refund");
            unit.Resources.Restore(resource); Check(unit.Resources.GetResourceAmount(resource) == tuple[2], "native restore returns only this unit to real maximum");
            unit.Facts.Remove(feature); Check(!unit.Abilities.Enumerable.Any(a => a.Blueprint == ability) && unit.Resources.GetResourceAmount(resource) == 0, "native feature removal revokes granted ability and owned resource");
        }
        var first = Unit(cache, "independent-first", 20); var second = Unit(cache, "independent-second", 20); Fact(first, root); Fact(second, root);
        logic.Spend(new AbilityData(ability, first.Descriptor)); resourceSpends++;
        Check(first.Resources.GetResourceAmount(resource) == 2 && second.Resources.GetResourceAmount(resource) == 3, "daily uses are independent per caster");
        Check(ability.Type == AbilityType.SpellLike && ability.ActionType == Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard && ability.AvailableMetamagic == 0 && !ability.GetComponents<SpellListComponent>().Any(), "spell-like standard action has no spellbook root, metamagic or spell-list registration");
    }
    static Buff Cast(BlueprintsCache cache, UnitEntityData caster)
    {
        var bp = Asset<BlueprintAbility>(cache, Ability); var data = new AbilityData(bp, caster.Descriptor);
        var context = new AbilityExecutionContext(data, new AbilityParams(), new TargetWrapper(caster), null, caster);
        AbilityExecutionProcess.PrepareCast(context); casts++;
        var run = bp.GetComponent<AbilityEffectRunAction>(); var apply = run.Actions.Actions.Single() as ContextActionApplyBuff;
        Check(apply.DurationValue.Calculate(context).Seconds == TimeSpan.FromSeconds(6), "native duration calculation is exactly one round");
        using (context.GetDataScope(caster)) run.Apply(context, new TargetWrapper(caster)); applications++;
        var buff = caster.Buffs.Enumerable.Single(b => b.Blueprint.AssetGuid == Id("TimeStutterHost"));
        Check(!buff.IsPermanent && buff.EndTime == now + TimeSpan.FromSeconds(6) && buff.IsTurnedOn, "native ability action applies private host for six seconds"); return buff;
    }
    static AreaEffectEntityData MakeArea(string label, MechanicsContext context, BlueprintAbilityAreaEffect blueprint, params UnitEntityData[] units)
    {
        var area = (AreaEffectEntityData)Call(typeof(BardMasterpieces01435Tests), "Area", "037-time-area-" + label, context, blueprint, units);
        var proxy = FormatterServices.GetUninitializedObject(AccessTools.Inner(typeof(EntityService), "Proxy")); Set(proxy, "Entity", area); Set(area, "Proxy", proxy);
        return area;
    }
    static void BindArea(Buff host, AreaEffectEntityData area)
    {
        var runtime = host.Components.Single(c => c.SourceBlueprintComponent is AddAreaEffect);
        using (runtime.RequestEventContext()) {
            var component = runtime.SourceBlueprintComponent;
            var data = AccessTools.Property(component.GetType(), "Data").GetValue(component, null);
            Set(data, "AreaEffectInstance", (EntityRef<AreaEffectEntityData>)area);
        }
    }
    static void EndArea(AreaEffectEntityData area)
    {
        Check(area.IsEnded, "real AddAreaEffect.OnDeactivate calls native ForceEnd before scene area update");
        // Scene controller scheduling is supplied by the fixture. The actual
        // native HandleEnd body retains its whole exit/clear/destroy chain.
        AccessTools.Method(typeof(AreaEffectEntityData), "HandleEnd").Invoke(area, null); areaEnds++;
        Check(area.DestroyMark, "real native HandleEnd queues completed area for destruction");
    }
    static void Effects(BlueprintsCache cache)
    {
        var original = new[] { NativeRoot, Host, Area, Effect }.Select(id => Native<BlueprintScriptableObject>(cache, id)).ToArray();
        var shapes = original.Select(b => Definition(b.ComponentsArray)).ToArray(); var arrays = original.Select(b => b.ComponentsArray).ToArray();
        var originalElements = original.Select(b => b.ElementsArray).ToArray(); var originalOwners = originalElements.Select(e => e.Select(a => a.Owner).ToArray()).ToArray();
        var source = (BlueprintAbility)original[0]; var apply = (ContextActionApplyBuff)source.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        Check(apply.DurationValue.DiceType == DiceType.D4 && apply.DurationValue.BonusValue.Value == 1, "native Time Stop remains D4+1 rounds");
        var caster = Unit(cache, "cast", 20); var other = Unit(cache, "second-cast", 20); var target = Unit(cache, "target", 10); var observer = Unit(cache, "unaffected", 10);
        Set(caster, "m_Position", new UnityEngine.Vector3(0, 0, 0)); Set(other, "m_Position", new UnityEngine.Vector3(1, 0, 0)); Set(target, "m_Position", new UnityEngine.Vector3(2, 0, 0)); Set(observer, "m_Position", new UnityEngine.Vector3(5, 0, 0));
        Check(new TargetWrapper(caster) != new TargetWrapper(target), "actual native TargetWrapper equality distinguishes scene positions before IsCaster condition");
        var host = Cast(cache, caster); var otherHost = Cast(cache, other);
        var areaBlueprint = Asset<BlueprintAbilityAreaEffect>(cache, "TimeStutterArea"); var effectBlueprint = Asset<BlueprintBuff>(cache, "TimeStutterEffect");
        Check(areaBlueprint.Shape == ((BlueprintAbilityAreaEffect)original[2]).Shape && areaBlueprint.Size == ((BlueprintAbilityAreaEffect)original[2]).Size && areaBlueprint.Fx.AssetId == ((BlueprintAbilityAreaEffect)original[2]).Fx.AssetId, "private area preserves exact original native shape, size and visual asset");
        foreach (string field in new[] { "m_TargetType", "m_Tags", "SpellResistance", "AffectEnemies", "AggroEnemies", "AffectDead", "IgnoreSleepingUnits", "CanBeUsedInTacticalCombat", "m_TickRoundAfterSpawn" })
            Check(Equals(Get(areaBlueprint, field), Get(original[2], field)), "private area keeps exact native target/filter field " + field);
        var privateAbility = Asset<BlueprintAbility>(cache, Ability);
        foreach (string field in new[] { "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "CanTargetPoint" })
            Check(Equals(Get(privateAbility, field), Get(source, field)), "private ability keeps exact native target field " + field);
        Check(effectBlueprint.Stacking == StackingType.Stack && ((BlueprintBuff)original[3]).Stacking == StackingType.Replace && ((BlueprintBuff)original[1]).Stacking == StackingType.Ignore, "private source-owned effects stack independently; native Effect Replace and Host Ignore remain exact");
        var area = MakeArea("first", host.Context, areaBlueprint, caster, target); var second = MakeArea("second", otherHost.Context, areaBlueprint, other, target);
        BindArea(host, area); BindArea(otherHost, second);
        var firstEndTime = host.EndTime;
        var recast = Cast(cache, caster);
        Check(ReferenceEquals(recast, host) && host.EndTime == firstEndTime && caster.Buffs.Enumerable.Count(b => b.Blueprint == host.Blueprint) == 1, "actual native Host Ignore recast keeps the original active host and expiry without duplicate");
        var nativeAreaRuntime = host.Components.Single(c => c.SourceBlueprintComponent is AddAreaEffect);
        using (nativeAreaRuntime.RequestEventContext()) {
            var nativeAreaData = AccessTools.Property(nativeAreaRuntime.SourceBlueprintComponent.GetType(), "Data").GetValue(nativeAreaRuntime.SourceBlueprintComponent, null);
            Check(Equals(Get(nativeAreaData, "AreaEffectInstance"), (EntityRef<AreaEffectEntityData>)area) && !area.IsEnded, "actual native Host Ignore recast keeps its already-bound source area rather than creating or ending another");
        }
        Check(ContextData<Buff.Data>.Current == null, "ordinary area entry has no artificially seeded host Buff.Data");
        areaBlueprint.HandleUnitEnter(host.Context, area, caster); areaEntries++;
        Check(!caster.Buffs.Enumerable.Any(b => b.Blueprint == effectBlueprint), "actual native area condition excludes original caster");
        areaBlueprint.HandleUnitEnter(host.Context, area, target); areaEntries++;
        var effect = target.Buffs.Enumerable.Single(b => b.Blueprint == effectBlueprint);
        Check(effect.IsTurnedOn && effect.IsPermanent && effect.SourceAreaEffectId == area.UniqueId, "native area-owned permanent child is tied to precise area cleanup, not an invented duration");
        areaBlueprint.HandleUnitEnter(host.Context, area, target); areaEntries++;
        Check(target.Buffs.Enumerable.Count(b => b.Blueprint == effectBlueprint) == 1, "actual repeated native enter cannot duplicate the same source-area effect");
        foreach (var condition in new[] { UnitCondition.CantAct, UnitCondition.CantMove, UnitCondition.CanNotAttack, UnitCondition.Paralyzed }) Check(target.State.HasCondition(condition) && !caster.State.HasCondition(condition) && !observer.State.HasCondition(condition), "native condition applies only to actual area target " + condition);
        Check((bool)target.State.Features.IsUntargetable && !(bool)caster.State.Features.IsUntargetable, "native Untargetable runtime flag only applies to frozen creature");
        var damage = new EnergyDamage(new DiceFormula(1, DiceType.D6), 0, DamageEnergyType.Fire); var calculate = new RuleCalculateDamage(new RuleDealDamage(caster, target, new DamageBundle(damage)));
        foreach (var runtime in effect.Components.Where(c => c.SourceBlueprintComponent is CompleteDamageImmunity)) using (runtime.RequestEventContext()) { ((CompleteDamageImmunity)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(calculate); damageImmunities++; }
        Check(damage.Declines.Any(d => d.Type == DamageDeclineType.Total), "actual native damage-immunity handler declines incoming damage while frozen");
        areaBlueprint.HandleUnitEnter(otherHost.Context, second, target); areaEntries++;
        Check(target.Buffs.Enumerable.Count(b => b.Blueprint == effectBlueprint) == 2 && target.Buffs.Enumerable.Count(b => b.SourceAreaEffectId == area.UniqueId) == 1 && target.Buffs.Enumerable.Count(b => b.SourceAreaEffectId == second.UniqueId) == 1, "two native source areas retain separate real effects on the same target");
        areaBlueprint.HandleUnitExit(host.Context, area, target);
        Check(effect.IsDisposed && target.Buffs.Enumerable.Count(b => b.Blueprint == effectBlueprint) == 1 && target.State.HasCondition(UnitCondition.CantAct), "actual native exit removes only first source and leaves independent second freeze active");
        areaBlueprint.HandleUnitExit(host.Context, area, target);
        Check(target.Buffs.Enumerable.Count(b => b.Blueprint == effectBlueprint) == 1, "repeated first-source exit cannot remove another area's effect");
        areaBlueprint.HandleUnitEnter(host.Context, area, target); areaEntries++;
        Check(target.Buffs.Enumerable.Count(b => b.Blueprint == effectBlueprint) == 2, "actual native re-entry restores only the exited source");
        caster.Facts.Remove(host);
        Check(target.Buffs.Enumerable.Count(b => b.Blueprint == effectBlueprint) == 2, "native ForceEnd requests area shutdown without falsely claiming synchronous cleanup");
        EndArea(area);
        Check(target.Buffs.Enumerable.Count(b => b.Blueprint == effectBlueprint) == 1 && target.Buffs.Enumerable.Single(b => b.Blueprint == effectBlueprint).SourceAreaEffectId == second.UniqueId && target.State.HasCondition(UnitCondition.CantMove), "actual native area HandleEnd precisely clears early-removed host's source and retains second source");
        now = otherHost.EndTime; other.Buffs.TickBuff(otherHost, now); expirations++;
        Check(otherHost.IsDisposed && target.Buffs.Enumerable.Any(b => b.Blueprint == effectBlueprint), "actual one-round host expiry requests source area end before area controller update");
        EndArea(second);
        Check(!target.Buffs.Enumerable.Any(b => b.Blueprint == effectBlueprint) && !(bool)target.State.Features.IsUntargetable, "normal native area end removes final source and targeting flag");
        foreach (var condition in new[] { UnitCondition.CantAct, UnitCondition.CantMove, UnitCondition.CanNotAttack, UnitCondition.Paralyzed }) Check(!target.State.HasCondition(condition), "native final-source cleanup releases condition " + condition);
        Check(original.Select((b, i) => ReferenceEquals(b.ComponentsArray, arrays[i]) && Definition(b.ComponentsArray) == shapes[i] && ReferenceEquals(b.ElementsArray, originalElements[i]) && b.ElementsArray.Select(e => e.Owner).Zip(originalOwners[i], ReferenceEquals).All(v => v)).All(v => v), "all four native Time Stop graphs keep exact components, elements and owner objects after casts/cleanup");
        Check(apply.DurationValue.DiceType == DiceType.D4 && ((BlueprintBuff)original[3]).Stacking == StackingType.Replace && ((BlueprintBuff)original[1]).Stacking == StackingType.Ignore, "native Time Stop duration and both native stacking modes remain unchanged");
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = choices = casts = applications = areaEntries = expirations = resourceSpends = damageImmunities = cacheFaults = appendFaults = definitionFaults = foreignCollisions = areaExits = areaEnds = 0; Units.Clear(); injectAppend = injectCache = injectDefinition = false; now = TimeSpan.FromHours(1);
        var h = new Harmony("CotW01437.TimeStutterNative"); var game = Game.Instance; var previousState = game.State; var previousTime = game.TimeController;
        return RunNative(cache, h, game, previousState, previousTime);
    }
    static int RunNative(BlueprintsCache cache, Harmony h, Game game, PersistentState previousState, TimeController previousTime)
    {
        var helperUnits = (IList)AccessTools.Field(typeof(BardMasterpieces01435Tests), "units").GetValue(null); int oldHelperCount = helperUnits.Count;
        var helperCurrent = AccessTools.Field(typeof(BardMasterpieces01435Tests), "current"); var priorContext = helperCurrent.GetValue(null); helperCurrent.SetValue(null, null);
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = now; Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        var addBuff = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var capture = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense");
        var suspended = (Harmony.GetPatchInfo(addBuff)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<HarmonyLib.Patch>()).Where(p => p.PatchMethod == capture).ToArray();
        void Patch(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(TimeStutter01437Tests), name));
        Exception failure = null;
        try {
            foreach (var p in suspended) h.Unpatch(addBuff, p.PatchMethod);
            Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), nameof(Skip)); Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), nameof(Controller));
            Patch(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), nameof(Skip)); Patch(AccessTools.Method(typeof(EntityFact), "Attach"), nameof(Identity)); Patch(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), nameof(Skip));
            Patch(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), nameof(Clock)); Patch(AccessTools.PropertyGetter(typeof(TimeController), "RealTime"), nameof(Clock));
            Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), nameof(No)); Patch(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), nameof(No)); Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), nameof(No));
            Patch(AccessTools.Method(typeof(AddAreaEffect), "IsOwnerAvatarValid"), nameof(No));
            h.Patch(AccessTools.Method(typeof(BlueprintAbilityAreaEffect), "HandleUnitExit"), prefix: new HarmonyMethod(typeof(TimeStutter01437Tests), nameof(ObserveExit)));
            h.Patch(AccessTools.Method(typeof(AreaEffectEntityData), "HandleEnd"), transpiler: new HarmonyMethod(typeof(TimeStutter01437Tests), nameof(EndMembershipBoundary)));
            Patch(AccessTools.PropertyGetter(typeof(RootUIContext), "Instance"), nameof(UI)); Patch(AccessTools.Method(typeof(RootUIContext), "ForceRescanOvertipVMs"), nameof(Skip));
            Patch(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), nameof(Expose)); Provider01419Tests.PatchLabels(h);
            h.Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "Context"));
            foreach (var m in new[] { AccessTools.Method(typeof(AbilityExecutionProcess), "PrepareCast"), AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction") }) h.Patch(m, transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "RuleBoundary"));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary")); h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "DifficultyBoundary"));
            h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "CanSelect"), prefix: new HarmonyMethod(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardDiscoveryChoice01436Patch"), "Prefix"));
            Registration(cache, h); Resources(cache); Effects(cache);
            Check(casts > 0 && applications > 0 && areaEntries > 0 && expirations > 0 && resourceSpends > 0 && damageImmunities > 0 && cacheFaults == 1 && appendFaults == 1 && definitionFaults == 1 && foreignCollisions == 6 && areaExits == 6 && areaEnds == 2, "all claimed native path and injected failure counters actually execute");
            PsychicExpansionTests.Inventory["timeStutter01437Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0370"), ["ordinaryFeatCount"] = 0, ["arcaneDiscoveryCount"] = 1, ["newSpellRootCount"] = 0,
                ["newBlueprints"] = new JArray(Tokens.Select(t => { var bp = cache.Load(Id(t)); return new JObject { ["id"] = bp.AssetGuid.ToString(), ["name"] = bp.name, ["type"] = bp.GetType().FullName }; })),
                ["nativeCanSelectCalls"] = choices, ["nativePrepareCasts"] = casts, ["nativeAbilityApplyCalls"] = applications, ["nativeAreaEnterCalls"] = areaEntries, ["nativeBuffExpiryCalls"] = expirations, ["nativeResourceSpends"] = resourceSpends, ["nativeDamageImmunityHandlers"] = damageImmunities, ["nativePostAddRollbackFaults"] = cacheFaults, ["nativePostAppendRollbackFaults"] = appendFaults, ["nativePostCommitDefinitionRollbackFaults"] = definitionFaults, ["foreignGraphCollisionCases"] = foreignCollisions, ["nativeAreaExitCalls"] = areaExits, ["nativeAreaHandleEndCalls"] = areaEnds,
                ["boundary"] = "Actual native CanSelect/prerequisites, feature/resource managers, AbilityCastRateUtils.SpendResources, PrepareCast, AbilityEffectRunAction.Apply, ContextActionApplyBuff, native area blueprint dispatch/condition/enter/exit, native AddAreaEffect.OnDeactivate/ForceEnd and actual area HandleEnd source cleanup after early removal and BuffCollection.TickBuff expiry execute. The native damage-immunity handler is called with its attached runtime context; final HP damage is not simulated. Native JSON is copied without Unity asset loading. Scene unit setup/clock/rule entry scheduling, absent area allocation, membership scan (the single UpdateViewAndUnits call inside HandleEnd), area-controller scheduling and overtip UI refresh are controlled endpoints; IsPlayersEnemy is controlled false, so rendered faction/spatial targeting is not simulated; rendered time flow, VFX and complete game save/reload remain tester observations." };
            Console.WriteLine("PASS: " + checks + " TimeStutter01437 checks.");
            return checks + TimeStopLifecycle01438Tests.Run(cache, h) + TimeStopSpell01438Tests.RunNative(cache, h) + TimeStopTimers01438Tests.RunNative(cache, h) + SaveLoadTimers01442Tests.RunNative(cache, h) + TimeStopSpellbook01438Tests.RunNative(cache, h) + TimeStopDismiss01440Tests.Run(cache);
        } catch (Exception e) { failure = e; Console.Error.WriteLine("037 time primary failure: " + e); throw; }
        finally {
            foreach (var unit in Units.AsEnumerable().Reverse()) foreach (var fact in unit.Facts.List.ToArray()) if (fact.Manager != null) try { unit.Facts.Remove(fact); } catch (Exception e) { if (failure == null) throw; Console.Error.WriteLine("037 cleanup: " + e.Message); }
            h.UnpatchAll(h.Id); foreach (var p in suspended) new Harmony(p.owner).Patch(addBuff, prefix: new HarmonyMethod(p.PatchMethod) { priority = p.priority, before = p.before, after = p.after, debug = p.debug });
            while (helperUnits.Count > oldHelperCount) helperUnits.RemoveAt(helperUnits.Count - 1); helperCurrent.SetValue(null, priorContext); Set(game, "State", previousState); Set(game, "TimeController", previousTime); Units.Clear();
        }
    }
}
