using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using CallOfTheWild.Wrath.Prototype;

internal static class RegistrationFeatKi01432Tests
{
    static int checks;
    static bool retryAppendFault;
    static void RetryAppendFault(BlueprintFeature[] feats) { if (retryAppendFault && feats.Any(f => f.AssetGuid == Id("KiBurstOfAdrenalineScaledFist"))) throw new InvalidOperationException("01432 retry second-pool append fault"); }
    static readonly List<string> callerExecuted = new List<string>(), callerSkipped = new List<string>();
    static bool CallerScope(string name) {
        if (new[] { "Advanced talent: Feat", "Ki powers: five spell adaptations", "Psychic casting prototype", "Psychic spells and disciplines", "Ferocious Beast rage powers", "Inquisition: Anger" }.Contains(name)) { callerExecuted.Add(name); return true; }
        callerSkipped.Add(name); return false;
    }
    static Type Module(string name) => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype." + name, true);
    static object Get(object o, string field) => AccessTools.Field(o.GetType(), field).GetValue(o);
    static void Set(object o, string field, object value) => AccessTools.Field(o.GetType(), field).SetValue(o, value);
    static BlueprintGuid Id(string token) => (BlueprintGuid)AccessTools.Method(Module("PsychicAssets"), "Id").Invoke(null, new object[] { token });
    static T Asset<T>(BlueprintsCache cache, string token) where T : SimpleBlueprint => (T)cache.Load(Id(token));
    static T Ref<T>(SimpleBlueprint bp) where T : BlueprintReferenceBase, new() {
        var r = BlueprintReferenceBase.CreateTyped<T>(bp); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, bp, null); return r;
    }
    static SimpleBlueprint Copy(SimpleBlueprint bp) => (SimpleBlueprint)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(bp, null);
    static IEnumerable<BlueprintReferenceBase> References(object value, HashSet<object> seen) {
        if (value == null) yield break;
        if (value is BlueprintReferenceBase reference) { yield return reference; yield break; }
        var type = value.GetType();
        if (value is SimpleBlueprint || value is UnityEngine.Object || type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is Type || value is Delegate || !seen.Add(value)) yield break;
        if (value is IEnumerable items) { foreach (var item in items) foreach (var r in References(item, seen)) yield return r; yield break; }
        for (var t = type; t != null && t != typeof(object) && t != typeof(BlueprintComponent); t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(f => !f.IsStatic))
                foreach (var r in References(field.GetValue(value), seen)) yield return r;
    }
    static object Cached(BlueprintReferenceBase reference) => AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").GetValue(reference, null);
    static void SetCached(BlueprintReferenceBase reference, object value) => AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(reference, value, null);
    static BlueprintFeatureReference[] IsolatedSelectionRefs(BlueprintFeatureSelection selection, string field, HashSet<BlueprintGuid> excluded) {
        var references = CopyData(Refs(selection, field).Where(r => r != null && !excluded.Contains(r.Guid)).ToArray());
        // CopyData intentionally preserves blueprint object references for
        // production cloning. A second native cache must instead resolve its
        // copied references against that cache, never a live parent blueprint.
        foreach (var reference in references) SetCached(reference, null);
        return references;
    }
    static void Register(BlueprintsCache cache, string name, string method = "Register") => AccessTools.Method(Module(name), method).Invoke(null, new object[] { cache });
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => (BlueprintFeatureReference[])Get(p, field) ?? Array.Empty<BlueprintFeatureReference>();
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Registration01432: " + why); checks++; }
    static bool Rejected(Action action, string text) {
        try { action(); return false; }
        catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e is InvalidOperationException && e.Message.Contains(text); }
    }
    static IEnumerable<SimpleBlueprint> Loaded(BlueprintsCache cache) => ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToArray().Select(cache.Load);
    internal static BlueprintsCache FreshKiCache(BlueprintsCache source) {
        var cache = new BlueprintsCache();
        var retired = Loaded(source).Where(x => x.name.StartsWith("CotWWrathPsychicKi")).Select(x => x.AssetGuid).ToHashSet();
        foreach (var bp in Loaded(source).Where(x => !retired.Contains(x.AssetGuid))) {
            SimpleBlueprint copy = bp;
            if (bp is BlueprintFeatureSelection selection) {
                copy = Copy(bp);
                foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(copy, field, IsolatedSelectionRefs(selection, field, retired));
            }
            if (bp is BlueprintAbilityResource) copy = Copy(bp);
            cache.AddCachedBlueprint(copy.AssetGuid, copy);
        }
        return cache;
    }
    static void BasicEntries(BlueprintFeatureSelection basic, params BlueprintFeatureReference[] refs) {
        Set(basic, "m_Features", refs.ToArray()); Set(basic, "m_AllFeatures", refs.ToArray());
    }
    static BlueprintFeatureSelection Selection(BlueprintsCache cache, string token, params BlueprintFeatureReference[] refs) {
        var bp = new BlueprintFeatureSelection { AssetGuid = Id(token), name = token, ComponentsArray = Array.Empty<BlueprintComponent>() };
        BasicEntries(bp, refs); bp.OnEnable(); cache.AddCachedBlueprint(bp.AssetGuid, bp); return bp;
    }
    static void Talent(BlueprintsCache parent) {
        var cache = FreshKiCache(parent); var id = Id("RogueAdvancedFeatTalent"); cache.RemoveCachedBlueprint(id);
        var basic = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));
        var direct = new BlueprintFeature { AssetGuid = Id("01432DirectFeat"), name = "01432DirectFeat", ComponentsArray = Array.Empty<BlueprintComponent>() };
        var parameter = new BlueprintParametrizedFeature { AssetGuid = Id("01432ParameterFeat"), name = "01432ParameterFeat", ComponentsArray = Array.Empty<BlueprintComponent>() };
        cache.AddCachedBlueprint(direct.AssetGuid, direct); cache.AddCachedBlueprint(parameter.AssetGuid, parameter);
        var leafId = Id("01432LateNestedLeaf"); var missing = new BlueprintFeatureReference(); missing.ReadGuidFromJson(leafId.ToString());
        var nested = Selection(cache, "01432NestedOrdinary", Ref<BlueprintFeatureReference>(direct), missing);
        var cycle = Selection(cache, "01432SelectionCycle"); BasicEntries(cycle, Ref<BlueprintFeatureReference>(cycle));
        BasicEntries(basic, Ref<BlueprintFeatureReference>(direct), Ref<BlueprintFeatureReference>(parameter), Ref<BlueprintFeatureReference>(nested), Ref<BlueprintFeatureReference>(cycle));
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        Register(cache, "RogueFeatTalent"); var talent = Asset<BlueprintFeatureSelection>(cache, "RogueAdvancedFeatTalent");
        Check(talent != null && talent.AllFeatures.Contains(direct) && talent.AllFeatures.Contains(parameter), "initial actual registration retains direct and parameterized feats");
        Check(!talent.AllFeatures.Contains(nested) && !talent.AllFeatures.Contains(cycle), "unresolved nested branch and cycle are deferred/excluded");
        var skipped = (string[])AccessTools.Method(Module("RogueFeatTalent"), "SkippedChoices").Invoke(null, new object[] { cache });
        Check(skipped.Length == 1 && skipped[0].Contains(leafId.ToString()), "diagnostic names the deferred external child GUID");
        var rootReferences = new[] { Get(talent, "m_Features"), Get(talent, "m_AllFeatures") };
        Register(cache, "RogueFeatTalent", "Optional"); Register(cache, "RogueFeatTalent", "Optional");
        Check(ReferenceEquals(cache.Load(id), talent) && talent.AllFeatures.Count() == 2, "actual late optional refresh succeeds without replacing published talent");
        var leaf = new BlueprintFeature { AssetGuid = leafId, name = "01432LateNestedLeaf", ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(leafId, leaf);
        Register(cache, "RogueFeatTalent", "Optional");
        Check(talent.AllFeatures.Contains(nested) && !talent.AllFeatures.Contains(cycle), "late published child restores only valid branch");
        Check(((string[])AccessTools.Method(Module("RogueFeatTalent"), "SkippedChoices").Invoke(null, new object[] { cache })).Length == 0, "resolved diagnostic clears on real refresh");
        BasicEntries(nested, Ref<BlueprintFeatureReference>(talent)); Register(cache, "RogueFeatTalent", "Optional");
        Check(!talent.AllFeatures.Contains(nested) && talent.AllFeatures.Contains(parameter), "late recursive return to advanced Feat remains forbidden");
        foreach (string extra in new[] { "739de36c402e4560b586cf79d00e3cbb", "04586da4ae28422da09e40d8241d36dc" }) {
            var provider = Selection(cache, "01432ExtraProvider" + extra); cache.RemoveCachedBlueprint(provider.AssetGuid); provider.AssetGuid = BlueprintGuid.Parse(extra); cache.AddCachedBlueprint(provider.AssetGuid, provider);
            BasicEntries(basic, Ref<BlueprintFeatureReference>(direct), Ref<BlueprintFeatureReference>(provider)); Register(cache, "RogueFeatTalent", "Optional");
            Check(!talent.AllFeatures.Contains(provider), "Extra Talent identity excluded even before child publication " + extra);
        }
        BasicEntries(basic, Ref<BlueprintFeatureReference>(cycle));
        var previous = new[] { Get(talent, "m_Features"), Get(talent, "m_AllFeatures") };
        Check(Rejected(() => Register(cache, "RogueFeatTalent", "Optional"), "no non-recursive"), "genuinely empty safe choice set fails honestly");
        Check(ReferenceEquals(Get(talent, "m_Features"), previous[0]) && ReferenceEquals(Get(talent, "m_AllFeatures"), previous[1]), "failed refresh retains exact previously usable arrays");
    }
    static BlueprintAbility NativeFrightfulDonor() {
        var source = PsychicExpansionTests.NativeCase<BlueprintAbility>("e788b02f8d21014488067bdd3ba7b325");
        var row = PsychicExpansionTests.NativeRows.First(x => (string)x["blueprint"]["AssetId"] == source.AssetGuid.ToString())["blueprint"]["Data"];
        foreach (string field in new[] { "Range", "ActionType", "Type", "m_IsFullRoundAction", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies" }) {
            var info = AccessTools.Field(typeof(BlueprintAbility), field); info.SetValue(source, PsychicExpansionTests.Read(row[field], info.FieldType));
        }
        return source;
    }
    static BlueprintAbility NativeFrightful(BlueprintsCache cache) {
        var source = NativeFrightfulDonor(); cache.AddCachedBlueprint(source.AssetGuid, source); return source;
    }
    internal static bool PrepareMetadataFrightfulDonor(BlueprintsCache cache) {
        var id = BlueprintGuid.Parse("e788b02f8d21014488067bdd3ba7b325");
        if (!(cache.Load(id) is BlueprintAbility shell) || shell.name != "NativeSpell" || shell.Type != AbilityType.Spell ||
            shell.ComponentsArray == null || shell.ComponentsArray.Length != 0) return false;
        // PsychicTests.Fixture deliberately seeds its 139 catalog spells as
        // empty NativeSpell placeholders. The old Ki suite used to replace
        // this donor in the parent cache as an incidental setup side effect.
        // Prepare only that exact fixture shell, in place, before isolating Ki.
        var source = NativeFrightfulDonor(); shell.name = source.name; shell.ComponentsArray = source.ComponentsArray;
        foreach (string field in new[] { "Range", "ActionType", "Type", "m_IsFullRoundAction", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "CanTargetPoint", "SpellResistance", "EffectOnEnemy", "EffectOnAlly", "m_Parent" })
            Set(shell, field, Get(source, field));
        shell.OnEnable(); return true;
    }
    static void MetadataDonor(BlueprintsCache parent) {
        var cache = FreshKiCache(parent); var id = BlueprintGuid.Parse("e788b02f8d21014488067bdd3ba7b325");
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        var shell = new BlueprintAbility { AssetGuid = id, name = "NativeSpell", Type = AbilityType.Spell, ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(id, shell);
        var originalReference = Ref<BlueprintAbilityReference>(shell);
        Check(PrepareMetadataFrightfulDonor(cache), "known native catalog fixture shell is prepared from the real archive donor");
        Check(ReferenceEquals(cache.Load(id), shell) && ReferenceEquals(originalReference.Get(), shell), "native donor fixture preparation preserves existing blueprint and reference identities");
        Check(shell.name == "FrightfulAspect" && shell.Type == AbilityType.Spell && shell.Range == AbilityRange.Personal && shell.GetComponent<AbilityEffectRunAction>() != null, "prepared fixture has real native name, type, targeting and action payload");
        var checker = new AbilityCasterInCombat(); shell.ComponentsArray = shell.ComponentsArray.Concat(new BlueprintComponent[] { checker }).ToArray();
        Check(!PrepareMetadataFrightfulDonor(cache) && shell.GetComponent<AbilityCasterInCombat>() == checker, "already-real donor and later external changes are never overwritten by fixture preparation");
        var foreign = new BlueprintAbility { AssetGuid = id, name = "ForeignFrightfulAspect", Type = AbilityType.Spell, ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(id, foreign);
        Check(!PrepareMetadataFrightfulDonor(cache) && ReferenceEquals(cache.Load(id), foreign) && foreign.name == "ForeignFrightfulAspect" && foreign.ComponentsArray.Length == 0, "foreign same-GUID ability is not disguised by donor fixture preparation");
        foreign.name = "FrightfulAspect";
        Check(!PrepareMetadataFrightfulDonor(cache) && foreign.ComponentsArray.Length == 0, "correctly named but broken real donor remains broken for production validation");
        var wrongType = new BlueprintFeature { AssetGuid = id, name = "NativeSpell", ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(id, wrongType);
        Check(!PrepareMetadataFrightfulDonor(cache) && ReferenceEquals(cache.Load(id), wrongType), "foreign wrong-type identity is not repaired by test-only donor preparation");
    }
    static void Ki(BlueprintsCache parent) {
        var cache = FreshKiCache(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        var native = NativeFrightful(cache); var providers = new[] { BlueprintGuid.Parse("6fb10fff27b84bff81746194bf8e38a8"), BlueprintGuid.Parse("58bcb999b3ad46c289dc68cba9c50bd0") };
        foreach (var guid in providers) cache.RemoveCachedBlueprint(guid);
        Register(cache, "MonkKiPowers01427");
        var owned = Loaded(cache).OfType<BlueprintScriptableObject>().Where(x => x.name.StartsWith("CotWWrathPsychicKi")).ToArray();
        var before = owned.ToDictionary(x => x.AssetGuid, x => (string)AccessTools.Method(Module("MonkKiPowers01427"), "Layout").Invoke(null, new object[] { x }));
        Check(owned.Length > 0 && owned.OfType<BlueprintFeature>().Count() == 6, "actual first registration creates three always-available rows");
        // A caster checker survives conversion. Resource/list components are
        // stripped and would not exercise the reported old definition collision.
        native.ComponentsArray = native.ComponentsArray.Concat(new BlueprintComponent[] { new AbilityCasterInCombat() }).ToArray(); native.OnEnable();
        Register(cache, "MonkKiPowers01427"); Register(cache, "MonkKiPowers01427");
        Check(owned.All(x => ReferenceEquals(cache.Load(x.AssetGuid), x)), "late donor mutation preserves every committed private identity");
        Check(owned.All(x => before[x.AssetGuid] == (string)AccessTools.Method(Module("MonkKiPowers01427"), "Layout").Invoke(null, new object[] { x })), "late donor mutation does not re-clone or change ki effects");
        var roots = new[] { Asset<BlueprintFeature>(cache, "KiFrightfulAspectMonk"), Asset<BlueprintFeature>(cache, "KiFrightfulAspectScaledFist") };
        var pools = ((string[])AccessTools.Field(Module("MonkKiPowers01427"), "PoolIds").GetValue(null)).Select(s => (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(s))).ToArray();
        for (int i = 0; i < pools.Length; i++) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(pools[i], field, Refs(pools[i], field).Where(r => r.Guid != roots[i].AssetGuid).ToArray());
        Register(cache, "MonkKiPowers01427");
        for (int i = 0; i < pools.Length; i++) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Check(Refs(pools[i], field).Count(r => r.Guid == roots[i].AssetGuid) == 1, "actual optional refresh repairs one exact pool reference");
        var retryBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var retryHook = new Harmony("CotW01432.PublishedKiRetryRollback");
        retryHook.Patch(AccessTools.Method(Module("T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(RegistrationFeatKi01432Tests), nameof(RetryAppendFault)));
        try {
            retryAppendFault = true;
            Check(Rejected(() => Register(cache, "MonkKiPowers01427"), "01432 retry"), "published-row retry reaches actual second-pool append fault");
            for (int i = 0; i < pools.Length; i++) for (int j = 0; j < 2; j++) Check(ReferenceEquals(Get(pools[i], j == 0 ? "m_Features" : "m_AllFeatures"), retryBefore[i][j]), "published-row retry failure restores exact pool array " + i + "/" + j);
            Check(owned.All(x => ReferenceEquals(cache.Load(x.AssetGuid), x)), "published-row retry failure preserves every committed private asset");
        } finally { retryAppendFault = false; retryHook.UnpatchAll(retryHook.Id); }
        Register(cache, "MonkKiPowers01427"); Check(owned.All(x => ReferenceEquals(cache.Load(x.AssetGuid), x)), "retry after append failure validates the retained publication record");
        var ability = Asset<BlueprintAbility>(cache, "KiFrightfulAspectMonkAbility");
        cache.RemoveCachedBlueprint(ability.AssetGuid);
        Check(Rejected(() => Register(cache, "MonkKiPowers01427"), "published graph"), "missing private child is rejected rather than silently rebuilt"); cache.AddCachedBlueprint(ability.AssetGuid, ability);
        var foreign = (BlueprintAbility)Copy(ability); cache.AddCachedBlueprint(foreign.AssetGuid, foreign);
        Check(Rejected(() => Register(cache, "MonkKiPowers01427"), "published graph"), "foreign same-GUID same-layout object is rejected"); cache.AddCachedBlueprint(ability.AssetGuid, ability);
        var resource = ability.GetComponent<AbilityResourceLogic>(); int cost = resource.Amount; resource.Amount++;
        Check(Rejected(() => Register(cache, "MonkKiPowers01427"), "published graph"), "owned ki cost corruption is not masked"); resource.Amount = cost;
        var facts = roots[0].GetComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>(); var privateRef = ((BlueprintUnitFactReference[])Get(facts, "m_Facts"))[0];
        AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(privateRef, foreign, null);
        Check(Rejected(() => Register(cache, "MonkKiPowers01427"), "reference identity"), "foreign cached private edge is rejected even with correct GUID"); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(privateRef, ability, null);
        Register(cache, "MonkKiPowers01427");
        var cloak = (BlueprintAbility)AccessTools.Method(typeof(Ki01427Tests), "Provider").Invoke(null, new object[] { cache, providers[0], false }); cache.AddCachedBlueprint(cloak.AssetGuid, cloak);
        Register(cache, "MonkKiPowers01427"); Check(Asset<BlueprintFeature>(cache, "KiCloakOfWindsMonk") == null, "late disabled provider remains excluded");
        var membership = new SpellListComponent { SpellLevel = 3 }; Set(membership, "m_SpellList", Ref<BlueprintSpellListReference>((BlueprintSpellList)cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89"))));
        cloak.ComponentsArray = cloak.ComponentsArray.Concat(new[] { membership }).ToArray(); cloak.OnEnable();
        var akashic = (BlueprintAbility)AccessTools.Method(typeof(Ki01427Tests), "Provider").Invoke(null, new object[] { cache, providers[1], true }); cache.AddCachedBlueprint(akashic.AssetGuid, akashic);
        Register(cache, "MonkKiPowers01427"); Register(cache, "MonkKiPowers01427");
        Check(Loaded(cache).OfType<BlueprintFeature>().Count(x => x.name.StartsWith("CotWWrathPsychicKi")) == 10, "late enabled providers first-register all five rows once");
        var partial = FreshKiCache(parent); NativeFrightful(partial); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, partial);
        var stolen = (BlueprintAbility)Copy(ability); partial.AddCachedBlueprint(stolen.AssetGuid, stolen);
        Check(Rejected(() => Register(partial, "MonkKiPowers01427"), "unowned or incomplete private"), "unowned child without root cannot be adopted by new-cache registration");
        Check(ReferenceEquals(partial.Load(stolen.AssetGuid), stolen) && Asset<BlueprintFeature>(partial, "KiFrightfulAspectMonk") == null, "foreign partial graph retained without root publication");
        var fresh = FreshKiCache(parent); NativeFrightful(fresh); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, fresh); Register(fresh, "MonkKiPowers01427");
        Check(Asset<BlueprintFeature>(fresh, "KiFrightfulAspectMonk") != roots[0], "new real cache builds its own private graph");
    }
    static T CopyData<T>(T value) => (T)AccessTools.Method(Module("PsychicAssets"), "CopyData").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { value });
    static void ActualCaller(BlueprintsCache parent) {
        var cache = new BlueprintsCache();
        var parentDaemon = (BlueprintFeature)parent.Load(BlueprintGuid.Parse("45102fd7aab96f94d81ec80768549e12"));
        var parentDaemonBasis = (BlueprintBuff)parent.Load(BlueprintGuid.Parse("a8957a1c6f212244d969645bc2fa7c25"));
        var parentDaemonArrays = new[] { parentDaemon.ComponentsArray, parentDaemonBasis.ComponentsArray };
        var parentDaemonDefinitions = parentDaemonArrays.Select(x => (string)AccessTools.Method(Module("Migration01426"), "Definition").Invoke(null, new object[] { x })).ToArray();
        var parentDaemonReferences = parentDaemonArrays.SelectMany(x => References(x, new HashSet<object>())).ToArray();
        var parentDaemonCached = parentDaemonReferences.Select(Cached).ToArray();
        var own = Loaded(parent).Where(x => x.name.StartsWith("CotW")).Select(x => x.AssetGuid).ToHashSet();
        foreach (var bp in Loaded(parent).Where(x => !own.Contains(x.AssetGuid))) {
            var copy = Copy(bp);
            if (copy is BlueprintScriptableObject scriptable) {
                scriptable.ComponentsArray = CopyData(scriptable.ComponentsArray);
                foreach (var reference in References(scriptable.ComponentsArray, new HashSet<object>())) SetCached(reference, null);
            }
            if (copy is BlueprintFeatureSelection selection) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(selection, field, IsolatedSelectionRefs(selection, field, own));
            if (copy is BlueprintSpellList spellList) {
                spellList.SpellsByLevel = CopyData(spellList.SpellsByLevel);
                foreach (var row in spellList.SpellsByLevel) {
                    Set(row, "m_Spells", ((List<BlueprintAbilityReference>)Get(row, "m_Spells")).Where(r => r != null && !own.Contains(r.Guid)).ToList());
                    Set(row, "m_SpellsProxy", null); Set(row, "m_SpellsFiltered", null);
                }
            }
            if (copy is BlueprintRoot root) {
                root.Progression = CopyData(root.Progression);
                Set(root.Progression, "m_CharacterClasses", ((BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses")).Where(r => !own.Contains(r.Guid)).ToArray());
            }
            cache.AddCachedBlueprint(copy.AssetGuid, copy);
        }
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache); NativeFrightful(cache);
        // These native hosts may already contain the preceding suites' CotW
        // observers. Restore actual game-authored donors for a fresh startup.
        foreach (string id in new[] { "da8ce41ac3cd74742b80984ccc3c9613", "3513326cd64f475781799685c57fa452", "ecc22ca1eea1bf6488a0d7c6ee2527d8", "5eac31e457999334b98f98b60fc73b2f" }) {
            var original = PsychicExpansionTests.NativeCase<BlueprintBuff>(id); cache.AddCachedBlueprint(original.AssetGuid, original);
        }
        var blood = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("d77e67a814d686842802c9cfd8ef8499"));
        var primal = (BlueprintArchetype)Copy(cache.Load(BlueprintGuid.Parse("4d588290ed0736e4f8cd3f4c3beacd69"))); cache.AddCachedBlueprint(primal.AssetGuid, primal);
        var bloodRow = PsychicExpansionTests.NativeRows.Single(row => (string)row["blueprint"]["AssetId"] == blood.AssetGuid.ToString())["blueprint"]["Data"];
        Set(blood, "m_Archetypes", PsychicExpansionTests.Read(bloodRow["m_Archetypes"], typeof(BlueprintArchetypeReference[]))); Set(primal, "m_ParentClass", blood);
        var nativeRagePool = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("28710502f46848d48b3f0d6132817c4e"));
        var parentRagePool = (BlueprintFeatureSelection)parent.Load(nativeRagePool.AssetGuid);
        var parentRageDaemonRef = Refs(parentRagePool, "m_AllFeatures").Single(r => r.Guid == parentDaemon.AssetGuid);
        var isolatedDaemonRef = Refs(nativeRagePool, "m_AllFeatures").Single(r => r.Guid == parentDaemon.AssetGuid);
        var retainedParentCacheRef = CopyData(parentRageDaemonRef); SetCached(retainedParentCacheRef, parentDaemon);
        Check(ReferenceEquals(retainedParentCacheRef.Get(), parentDaemon), "negative: a copied reference retaining Cached crosses into the parent graph despite a fresh global cache");
        SetCached(retainedParentCacheRef, null);
        Check(ReferenceEquals(retainedParentCacheRef.Get(), cache.Load(parentDaemon.AssetGuid)), "positive: clearing only the copied Cached reference resolves against the fresh cache");
        Check(!ReferenceEquals(parentRageDaemonRef, isolatedDaemonRef) && Cached(isolatedDaemonRef) == null, "fresh selection owns an uncached clone rather than a live parent reference");
        Check(ReferenceEquals(isolatedDaemonRef.Get(), cache.Load(parentDaemon.AssetGuid)) && !ReferenceEquals(isolatedDaemonRef.Get(), parentDaemon), "fresh selection resolves a fresh native root, never the parent root");
        var isolatedExtra = ((BlueprintFeature)isolatedDaemonRef.Get()).GetComponents<Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects>().First();
        Check(ReferenceEquals(isolatedExtra.ExtraEffectBuff, cache.Load(parentDaemonBasis.AssetGuid)) && !ReferenceEquals(isolatedExtra.ExtraEffectBuff, parentDaemonBasis), "fresh donor components resolve a fresh basis without mutating parent lazy caches");
        var poolData = PsychicExpansionTests.NativeRows.Single(row => (string)row["blueprint"]["AssetId"] == nativeRagePool.AssetGuid.ToString())["blueprint"]["Data"];
        var ttt = (BlueprintFeatureSelection)Copy(nativeRagePool); ttt.AssetGuid = BlueprintGuid.Parse("6baf176bc22c4a41ba421bce3fcd9497"); ttt.name = "PrimalistRagePowerSelection";
        ttt.Group = (FeatureGroup)PsychicExpansionTests.Read(poolData["Group"], typeof(FeatureGroup)); ttt.Groups = (FeatureGroup[])PsychicExpansionTests.Read(poolData["Groups"], typeof(FeatureGroup[]));
        cache.AddCachedBlueprint(ttt.AssetGuid, ttt);
        Check(ttt.Group == FeatureGroup.RagePower && ttt.Groups.Length == 0 && !ttt.IgnorePrerequisites, "actual caller seeds current TTT native-clone Group-only layout");
        Check(Asset<BlueprintAbility>(cache, "BurstOfAdrenaline") == null && Asset<BlueprintAbility>(cache, "BurstOfInsight") == null, "actual initial caller starts before own Burst donors exist");
        var basic = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));
        var direct = new BlueprintFeature { AssetGuid = Id("01432CallerDirectFeat"), name = "01432CallerDirectFeat", ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(direct.AssetGuid, direct);
        BasicEntries(basic, Ref<BlueprintFeatureReference>(direct));
        callerExecuted.Clear(); callerSkipped.Clear(); var h = new Harmony("CotW01432.ScopedActualRegistrationCaller");
        h.Patch(AccessTools.Method(Module("BlueprintRegistration"), "RegisterModule"), prefix: new HarmonyMethod(typeof(RegistrationFeatKi01432Tests), nameof(CallerScope)));
        var outcomes = new List<Tuple<string, bool, Exception>>(); Action<string, bool, Exception> report = (name, ok, error) => outcomes.Add(Tuple.Create(name, ok, error));
        try {
            AccessTools.Method(Module("BlueprintRegistration"), "RegisterAll").Invoke(null, new object[] { cache, report });
            foreach (string name in new[] { "Advanced talent: Feat", "Psychic casting prototype", "Psychic spells and disciplines", "Ki powers: five spell adaptations", "Ferocious Beast rage powers", "Inquisition: Anger" }) {
                var calls = outcomes.Where(x => x.Item1 == name).ToArray();
                Check(calls.Length != 0 && calls.All(x => x.Item2), "real RegisterAll reports successful target/dependency " + name + ": " + string.Join(";", calls.Where(x => !x.Item2).Select(x => x.Item3?.ToString())));
            }
            Check(callerExecuted.IndexOf("Psychic spells and disciplines") < callerExecuted.IndexOf("Ki powers: five spell adaptations"), "real dispatcher executes dependency before first Ki registration");
            Check(Asset<BlueprintAbility>(cache, "BurstOfAdrenaline") != null && Asset<BlueprintFeature>(cache, "KiBurstOfAdrenalineMonk") != null, "real owned dependency publication feeds real initial Ki registration");
            foreach (string token in new[] { "FerociousBeastFeature", "FerociousBeastGreaterFeature", "AngerInquisition" }) Check(cache.Load(Id(token)) != null, "real initial caller publishes cache-searchable missing resource " + token);
            foreach (string token in new[] { "FerociousBeastFeature", "FerociousBeastGreaterFeature" }) Check(Refs(ttt, "m_AllFeatures").Count(r => r.Guid == Id(token)) == 1, "real initial caller adds root to actual Group-only provider " + token);
            var lateId = Id("01432CallerLateFeat"); var missing = new BlueprintFeatureReference(); missing.ReadGuidFromJson(lateId.ToString());
            var nested = Selection(cache, "01432CallerExternalNested", missing); BasicEntries(basic, Ref<BlueprintFeatureReference>(direct), Ref<BlueprintFeatureReference>(nested));
            var source = (BlueprintAbility)cache.Load(BlueprintGuid.Parse("e788b02f8d21014488067bdd3ba7b325")); source.ComponentsArray = source.ComponentsArray.Concat(new BlueprintComponent[] { new AbilityCasterInCombat() }).ToArray(); source.OnEnable();
            outcomes.Clear(); AccessTools.Method(Module("BlueprintRegistration"), "RegisterOptional").Invoke(null, new object[] { cache, report });
            foreach (string name in new[] { "Advanced talent: Feat", "Ki powers: five spell adaptations", "Ferocious Beast rage powers" }) Check(outcomes.Any(x => x.Item1 == name && x.Item2) && !outcomes.Any(x => x.Item1 == name && !x.Item2), "real late caller reports Registered after reported failing trigger " + name);
            Check(!Asset<BlueprintFeatureSelection>(cache, "RogueAdvancedFeatTalent").AllFeatures.Contains(nested), "real late caller safely defers external unresolved branch");
            var leaf = new BlueprintFeature { AssetGuid = lateId, name = "01432CallerLateFeat", ComponentsArray = Array.Empty<BlueprintComponent>() }; cache.AddCachedBlueprint(lateId, leaf);
            outcomes.Clear(); AccessTools.Method(Module("BlueprintRegistration"), "RegisterOptional").Invoke(null, new object[] { cache, report });
            Check(Asset<BlueprintFeatureSelection>(cache, "RogueAdvancedFeatTalent").AllFeatures.Contains(nested) && outcomes.Where(x => x.Item1 == "Advanced talent: Feat" || x.Item1 == "Ki powers: five spell adaptations").All(x => x.Item2), "real repeated optional caller includes newly resolved feat and keeps both modules successful");
        } finally {
            h.UnpatchAll(h.Id);
            Check(ReferenceEquals(parent.Load(parentDaemon.AssetGuid), parentDaemon) && ReferenceEquals(parent.Load(parentDaemonBasis.AssetGuid), parentDaemonBasis), "actual caller keeps exact parent daemon blueprint identities");
            Check(ReferenceEquals(parentDaemon.ComponentsArray, parentDaemonArrays[0]) && ReferenceEquals(parentDaemonBasis.ComponentsArray, parentDaemonArrays[1]), "actual caller keeps exact parent daemon component arrays");
            Check(parentDaemonArrays.Select(x => (string)AccessTools.Method(Module("Migration01426"), "Definition").Invoke(null, new object[] { x })).SequenceEqual(parentDaemonDefinitions), "actual caller leaves parent daemon authored definitions unchanged");
            Check(parentDaemonReferences.Select(Cached).Zip(parentDaemonCached, ReferenceEquals).All(x => x), "actual caller leaves every parent daemon Cached pointer unchanged, including initially-null pointers");
        }
    }
    internal static int Run(BlueprintsCache cache) {
        checks = 0;
        var strings = (Dictionary<string, string>)AccessTools.Field(Module("PsychicAssets"), "Strings").GetValue(null);
        var originalStrings = strings.ToArray();
        try {
            Talent(cache); Ki(cache); ActualCaller(cache); MetadataDonor(cache);
        } finally {
            // The real caller intentionally builds PsychicExpansion again.
            // Its Text registrations also rewrite a process-global dictionary,
            // which the build later exports. Restore the complete prior key
            // set and values, including removing keys created in this scope.
            strings.Clear(); foreach (var pair in originalStrings) strings.Add(pair.Key, pair.Value);
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        }
        Check(strings.Count == originalStrings.Length && originalStrings.All(pair => strings.TryGetValue(pair.Key, out var value) && value == pair.Value), "scoped registration restores every original global text key/value and removes scope-created keys");
        PsychicExpansionTests.Inventory["registrationFeatKi01432Contracts"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["initialAndActualOptionalRefresh"] = true,
                ["unresolvedBranchesDeferredUntilReady"] = true, ["recursiveAndExtraTalentExcluded"] = true,
                ["donorLateMutationPreservesOwnedGraph"] = true, ["ownedReferenceAndDefinitionValidation"] = true,
                ["foreignPartialGraphRejected"] = true, ["optionalProvidersAbsentDisabledLate"] = true,
                ["publishedRetrySecondPoolRollback"] = true,
                ["realRegisterAllAndOptionalCaller"] = true, ["realOwnedDonorsPublishBeforeKi"] = true,
                ["realFerociousAngerInitialCaller"] = true, ["realTttGroupOnlyCallerRegistration"] = true,
                ["isolatedNativeReferenceCaches"] = true, ["parentDaemonDefinitionsAndCachedPointersPreserved"] = true,
                ["knownMetadataDonorPreparedInPlace"] = true, ["realAndForeignDonorsNeverOverwritten"] = true,
                ["globalTextKeysAndValuesPreserved"] = true,
                ["callerModulesExecuted"] = new JArray(callerExecuted), ["callerNonTargetModuleActionBoundaries"] = new JArray(callerSkipped.Distinct()),
                ["boundary"] = "Actual RegisterAll dispatcher and repeated RegisterOptional callers execute real Feat/Ki, Ferocious/Anger and Psychic class/expansion dependency actions on game BlueprintsCache. Other module actions are scoped out at RegisterModule, without fabricated success reports. TTT's current native-pool clone layout, external nested/provider blueprints and deliberately corrupt cache graphs are controlled; no Unity rendering or complete external assembly execution. Original native ki action/resource/Sensei suite remains separate." };
        Console.WriteLine("PASS: " + checks + " Feat/Ki late-registration repair contracts."); return checks;
    }
}
