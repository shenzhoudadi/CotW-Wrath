using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.Blueprints.Facts;
using System.Runtime.Serialization;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Ki01427Tests
{
    static int checks, casts; static bool fault; static UnitEntityData recipient;
    static readonly List<BlueprintBuff> applied = new List<BlueprintBuff>();
    static Type Module => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.MonkKiPowers01427", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Helper<T>(string n, params object[] a) => (T)AccessTools.Method(typeof(Migration01426Tests), n).Invoke(null, a);
    static BlueprintGuid Id(string token) => Helper<BlueprintGuid>("Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var v = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(v, b); return v; }
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Ki01427: " + why); checks++; }
    static void Register(BlueprintsCache c) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { c });
    static void AppendFault(BlueprintFeature[] feats) { if (fault && feats.Any(x => x.AssetGuid == Id("KiBurstOfAdrenalineScaledFist"))) throw new InvalidOperationException("01427 Ki append injection"); }
    static bool Apply(ContextActionApplyBuff __instance) {
        var context = ContextData<MechanicsContext.Data>.Current; applied.Add(__instance.Buff);
        var unit = __instance.ToCaster ? context.Context.MaybeCaster : context.CurrentTarget.Unit;
        Check(unit == recipient, "native apply recipient is correct for self/advice"); casts++; return false;
    }
    static string Signature(BlueprintScriptableObject source) => (string)AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition").Invoke(null, new object[] { source.ComponentsArray });
    static int adviceGrants, adviceRemovals;
    static bool AdviceGrant(UnitEntityData __0, BlueprintUnitFact __1, ref EntityFact __result) { Check(__1 is BlueprintAbility && __1.name.Contains("Advice"), "native Sensei grant dispatches correct ability"); __result = (EntityFact)FormatterServices.GetUninitializedObject(typeof(Ability)); adviceGrants++; return false; }
    static bool AdviceRemove(UnitEntityData __0, EntityFact __1) { if (__1 != null) adviceRemovals++; return false; }
    static void AdviceLifecycle(BlueprintsCache cache, Harmony h) {
        var add = typeof(UnitHelper).GetMethods().Single(m => m.Name == "AddFact" && !m.IsGenericMethod && m.GetParameters().Select(a => a.ParameterType).SequenceEqual(new[] { typeof(UnitEntityData), typeof(BlueprintUnitFact), typeof(MechanicsContext), typeof(FeatureParam) }));
        var remove = AccessTools.Method(typeof(UnitHelper), "RemoveFact", new[] { typeof(UnitEntityData), typeof(EntityFact) });
        h.Patch(add, prefix: new HarmonyMethod(typeof(Ki01427Tests), nameof(AdviceGrant))); h.Patch(remove, prefix: new HarmonyMethod(typeof(Ki01427Tests), nameof(AdviceRemove)));
        try { foreach (bool unlockFirst in new[] { false, true }) {
            var unit = Helper<UnitEntityData>("Unit", cache, "01427-sensei-lifecycle"); var root = Asset<BlueprintFeature>(cache, "KiBurstOfAdrenalineMonk");
            if (unlockFirst) foreach (var c in root.GetComponents<AddFeatureIfHasFact>()) Helper<Feature>("Fact", unit, (BlueprintFeature)c.CheckedFact);
            var fact = Helper<Feature>("Fact", unit, root);
            foreach (var runtime in fact.Components.Where(c => c.SourceBlueprintComponent is AddFeatureIfHasFact)) {
                var c = (AddFeatureIfHasFact)runtime.SourceBlueprintComponent; int n = adviceGrants;
                using (runtime.RequestEventContext()) AccessTools.Method(typeof(AddFeatureIfHasFact), "OnActivate").Invoke(c, null);
                Check(adviceGrants == n + (unlockFirst ? 1 : 0), "native Sensei activation respects unlock ownership");
                if (!unlockFirst) Helper<Feature>("Fact", unit, (BlueprintFeature)c.CheckedFact);
                using (runtime.RequestEventContext()) { c.HandleUnitGainLevel(); c.HandleUnitGainLevel(); }
                Check(adviceGrants == n + 1, "native late level event grants once without duplicate");
                int removed = adviceRemovals; using (runtime.RequestEventContext()) AccessTools.Method(typeof(AddFeatureIfHasFact), "OnDeactivate").Invoke(c, null);
                Check(adviceRemovals == removed + 1, "native loss of ki option removes its advice grant");
            }
        }} finally { h.Unpatch(add, HarmonyPatchType.Prefix, h.Id); h.Unpatch(remove, HarmonyPatchType.Prefix, h.Id); }
    }
    internal static int Run(BlueprintsCache cache) {
        RegistrationFeatKi01432Tests.PrepareMetadataFrightfulDonor(cache);
        var parentCache = cache;
        cache = RegistrationFeatKi01432Tests.FreshKiCache(parentCache);
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache);
        checks = casts = 0; var h = new Harmony("CotW01427.KiContracts");
        var poolIds = (string[])AccessTools.Field(Module, "PoolIds").GetValue(null); var pools = poolIds.Select(x => (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(x))).ToArray();
        // Earlier optional-integration tests can already register the Burst rows.
        // Use a genuinely new cache; deleting a committed graph in the live
        // cache must remain an error in the production identity contract.
        var priorKi = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Select(cache.Load).Where(x => x.name.StartsWith("CotWWrathPsychicKi")).Select(x => x.AssetGuid).ToHashSet();
        foreach (var pool in pools) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(pool, field, ((BlueprintFeatureReference[])Get(pool, field)).Where(x => !priorKi.Contains(x.Guid)).ToArray());
        foreach (var id in priorKi) cache.RemoveCachedBlueprint(id);
        var providers = new[] { BlueprintGuid.Parse("6fb10fff27b84bff81746194bf8e38a8"), BlueprintGuid.Parse("58bcb999b3ad46c289dc68cba9c50bd0") };
        var oldProviders = providers.Select(cache.Load).ToArray();
        foreach (var id in providers) cache.RemoveCachedBlueprint(id);
        foreach (string id in (string[])AccessTools.Field(Module, "ResourceIds").GetValue(null)) {
            var resource = (BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse(id));
            var native = PsychicExpansionTests.NativeRows.First(x => (string)x["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
            foreach (var value in ((JObject)native).Properties().Where(x => x.Name != "Components")) {
                var field = AccessTools.Field(typeof(BlueprintAbilityResource), value.Name);
                if (field != null && !field.IsInitOnly) field.SetValue(resource, PsychicExpansionTests.Read(value.Value, field.FieldType));
            }
        }
        var source = PsychicExpansionTests.NativeCase<BlueprintAbility>("e788b02f8d21014488067bdd3ba7b325");
        var row = PsychicExpansionTests.NativeRows.First(x => (string)x["blueprint"]["AssetId"] == source.AssetGuid.ToString())["blueprint"]["Data"];
        foreach (string field in new[] { "Range", "ActionType", "Type", "m_IsFullRoundAction", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies" }) {
            var info = AccessTools.Field(typeof(BlueprintAbility), field); info.SetValue(source, PsychicExpansionTests.Read(row[field], info.FieldType));
        }
        cache.AddCachedBlueprint(source.AssetGuid, source);
        var originals = new[] { Asset<BlueprintAbility>(cache, "BurstOfAdrenaline"), Asset<BlueprintAbility>(cache, "BurstOfInsight"), source };
        var signatures = originals.Select(Signature).ToArray();
        var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
        var initial = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToHashSet();
        h.Patch(AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(Ki01427Tests), nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "Skip"));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "No"));
        h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "PoolIgnore"));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game), "LevelUpController"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "NullController"));
        Provider01419Tests.PatchLabels(h);
        h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "No"));
        h.Patch(AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "ParamsBoundary"));
        try {
            fault = true; bool rejected = false; try { Register(cache); } catch (TargetInvocationException e) { rejected = e.InnerException is InvalidOperationException && e.InnerException.Message.Contains("01427 Ki append"); if (!rejected) throw; } finally { fault = false; }
            Check(rejected, "actual second-pool append failure reached");
            Check(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().All(initial.Contains), "failed item leaves no partially registered assets");
            for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) Check(ReferenceEquals(Get(pools[i], j == 0 ? "m_Features" : "m_AllFeatures"), before[i][j]), "rollback restores both pools exact arrays");
            Register(cache); Register(cache);
            Check(Asset<BlueprintFeature>(cache, "KiCloakOfWindsMonk") == null && Asset<BlueprintFeature>(cache, "KiAkashicFormMonk") == null, "missing providers do not register their ki options");
            var cloak = Provider(cache, providers[0], false); cache.AddCachedBlueprint(cloak.AssetGuid, cloak);
            Register(cache); Check(Asset<BlueprintFeature>(cache, "KiCloakOfWindsMonk") == null, "TTT disabled spell with no list membership is skipped");
            var membership = new SpellListComponent { SpellLevel = 3 }; Set(membership, "m_SpellList", Ref<BlueprintSpellListReference>((BlueprintSpellList)cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89"))));
            cloak.ComponentsArray = cloak.ComponentsArray.Concat(new[] { membership }).ToArray(); cloak.OnEnable();
            var akashic = Provider(cache, providers[1], true); cache.AddCachedBlueprint(akashic.AssetGuid, akashic);
            var providerSignatures = new[] { Signature(cloak), Signature(akashic) };
            Register(cache); Register(cache);
            AdviceLifecycle(cache, h);
            var tokens = (string[])AccessTools.Field(Module, "Tokens").GetValue(null); var levels = new[] { 4, 4, 6, 20, 20 }; var costs = new[] { 1, 1, 2, 3, 3 };
            var u = Helper<UnitEntityData>("Unit", cache, "01427-monk"); var target = Helper<UnitEntityData>("Unit", cache, "01427-advice-recipient");
            var monk = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("e8f21e5b58e0569468e420ebea456124")); var cd = new ClassData(monk) { Level = 20 }; u.Progression.Classes.Clear(); u.Progression.Classes.Add(cd); u.Stats.Wisdom.BaseValue = 18; u.Stats.Charisma.BaseValue = 22;
            for (int i = 0; i < tokens.Length; i++) for (int mode = 0; mode < 2; mode++) {
                string key = "Ki" + tokens[i] + (mode == 0 ? "Monk" : "ScaledFist"); var root = Asset<BlueprintFeature>(cache, key);
                for (int p = 0; p < 2; p++) Check(((BlueprintFeatureReference[])Get(pools[p], "m_AllFeatures")).Count(x => x.Guid == root.AssetGuid) == (p == mode ? 1 : 0), "Wis/Cha option enters only its real native pool");
                cd.Level = levels[i] - 1; Check(!root.MeetsPrerequisites(null, u.Descriptor, null, false, false), "one level below rejected " + key); cd.Level++; Check(root.MeetsPrerequisites(null, u.Descriptor, null, false, false), "required monk level accepted " + key);
                var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState)); Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", cd.Level); state.NextClassLevel = cd.Level; state.SelectedClass = monk;
                var pending = state.AddSelection(null, default(FeatureSource), pools[mode], cd.Level); Check(pools[mode].CanSelect(u.Descriptor, state, pending, root), "native ki pool CanSelect accepts qualified new option"); cd.Level--; Check(!pools[mode].CanSelect(u.Descriptor, state, pending, root), "native ki pool CanSelect rejects insufficient level"); cd.Level++;
                var ability = Asset<BlueprintAbility>(cache, key + "Ability"); Check(root.GetComponent<AddFacts>().Facts.Single() == ability && root.GetComponent<AddAbilityResources>() == null, "grants ability without creating/refilling ki resource");
                Check(root.GetComponents<AddFeatureIfHasFact>().Count() == 2 && root.GetComponents<AddFeatureIfHasFact>().All(x => x.CheckedFact != null && x.Feature != null), "native Sensei level-aware conditional grant references resolve");
                foreach (int advice in new[] { 0, 1, 2 }) {
                    var a = Asset<BlueprintAbility>(cache, key + (advice == 0 ? "Ability" : advice == 1 ? "Advice" : "AdviceMass"));
                    Check(a.Range == (advice == 1 ? AbilityRange.Close : AbilityRange.Personal) && !a.CanTargetEnemies && a.CanTargetFriends == (advice != 0), "correct self/single/mass target flags");
                    var variants = a.GetComponent<AbilityVariants>(); var children = variants == null ? new[] { a } : variants.Variants.ToArray();
                    if (i < 2) Check(children.Length == 3 && a.GetComponent<AbilityResourceLogic>() == null && children.All(v => v.Parent == a), "three Burst variants and only leaf resource costs");
                    foreach (var v in children) {
                        Check(v.Type == AbilityType.SpellLike && v.AvailableMetamagic == 0 && !v.GetComponents<SpellListComponent>().Any() && v.MaterialComponent.Count == 0, "SLA has no slot/list/material or metamagic claim");
                        Check(v.ActionType == (i < 2 ? UnitCommand.CommandType.Swift : UnitCommand.CommandType.Standard) && v.IsFullRoundAction == (i == 3), "original action/full-round requirement retained");
                        var cost = v.GetComponent<AbilityResourceLogic>(); var data = new AbilityData(v, u.Descriptor);
                        Check(cost.CalculateCost(data) == costs[i] + advice && cost.RequiredResource.AssetGuid.ToString() == (mode == 0 ? "9d9c90a9a1f52d04799294bf91c80a82" : "7d002c1025fbfe2458f1509bf7a89ce1"), "resource identity and advice surcharge");
                        u.Descriptor.Resources.Add(cost.RequiredResource, true); u.Descriptor.Resources.Restore(cost.RequiredResource); int amount = u.Descriptor.Resources.GetResourceAmount(cost.RequiredResource);
                        Check(cost.IsAbilityRestrictionPassed(data), "native ki availability " + key + " advice=" + advice + " amount=" + amount); cost.Spend(data); Check(u.Descriptor.Resources.GetResourceAmount(cost.RequiredResource) == amount - costs[i] - advice, "native spend exact ki cost");
                        u.Descriptor.Resources.Spend(cost.RequiredResource, u.Descriptor.Resources.GetResourceAmount(cost.RequiredResource)); Check(!cost.IsAbilityRestrictionPassed(data), "empty ki pool rejects ability"); u.Descriptor.Resources.Restore(cost.RequiredResource);
                        var param = v.GetComponent<ContextCalculateAbilityParams>().Calculate(data); Check(param.CasterLevel == cd.Level && param.DC == 10 + cd.Level / 2 + (mode == 0 ? 4 : 6), "monk CL and correct Wisdom/Charisma DC");
                        if (advice == 2) { var area = v.GetComponent<AbilityTargetsAround>(); Check(area != null && area.AoERadius.Value == 30 && area.Targets == TargetType.Ally && Get(area, "m_Condition") is ConditionsChecker, "native mass selector includes initialized conditions"); }
                        var action = v.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().FirstOrDefault();
                        Check(action != null, "real cloned effect has buff action");
                        if (i < 2 && advice != 0) { var after = action.Buff.GetComponent<AddFactContextActions>(); Check(!action.ToCaster && after.Deactivated.Actions.OfType<ContextActionApplyBuff>().All(x => !x.ToCaster), "Burst advice bonus and penalty both belong to recipient"); }
                        h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), prefix: new HarmonyMethod(typeof(Ki01427Tests), nameof(Apply)));
                        try { recipient = advice == 0 ? u : target; var ctx = new AbilityExecutionContext(data, param, recipient); v.GetComponent<AbilityEffectRunAction>().Apply(ctx, recipient); }
                        finally { h.Unpatch(AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), HarmonyPatchType.Prefix, h.Id); }
                    }
                }
            }
            for (int i = 0; i < originals.Length; i++) Check(Signature(originals[i]) == signatures[i], "existing spell source components unchanged");
            Check(Signature(cloak) == providerSignatures[0] && Signature(akashic) == providerSignatures[1], "external spell components unchanged");
            for (int p = 0; p < 2; p++) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Check(((BlueprintFeatureReference[])Get(pools[p], field)).Take(((BlueprintFeatureReference[])before[p][field == "m_Features" ? 0 : 1]).Length).SequenceEqual((BlueprintFeatureReference[])before[p][field == "m_Features" ? 0 : 1]), "all original ki options retained");
            Register(cache);
            var added = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Where(x => !initial.Contains(x) && !providers.Contains(x)).Select(cache.Load).Where(x => x.name.StartsWith("CotWWrathPsychicKi")).ToArray();
            Check(added.Length == 120 && added.Count(x => x is BlueprintFeature) == 10, "complete five-item graph:120 assets and10 Wis/Cha feature roots");
            PsychicExpansionTests.Inventory["ki01427Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 5, ["nativeApplyCalls"] = casts, ["providerAbsentDisabledLate"] = true, ["secondPoolRollback"] = true, ["sourceComponentsPreserved"] = true, ["newBlueprints"] = new JArray(added.Select(x => new JObject { ["name"] = x.name, ["id"] = x.AssetGuid.ToString(), ["type"] = x.GetType().Name })), ["boundary"] = "Production registration; real native level checks, resource spending/restrictions/restoration, parameter/DC calculation and AbilityEffectRunAction.Apply. Native current game Frightful Aspect and existing CotW Burst donors; external provider ability structure seeded from audited source, provider runtime effects and final Buff insertion remain boundaries. Native Sensei activation, late level grant, duplicate prevention and removal executed with final fact insertion boundary; live upgrade/UI and full external DLL combination follow tester steps." };
            Console.WriteLine("PASS: " + checks + " ki migration contracts; " + casts + " native apply calls."); return checks;
        } finally { fault = false; h.UnpatchAll(h.Id); for (int i = 0; i < providers.Length; i++) { cache.RemoveCachedBlueprint(providers[i]); if (oldProviders[i] != null) cache.AddCachedBlueprint(providers[i], oldProviders[i]); } AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, parentCache); }
    }
    static BlueprintAbility Provider(BlueprintsCache c, BlueprintGuid id, bool akashic) {
        var effect = new ContextActionApplyBuff { DurationValue = new ContextDurationValue { Rate = akashic ? DurationRate.Days : DurationRate.Minutes, BonusValue = akashic ? new ContextValue { Value = 1 } : new ContextValue { ValueType = ContextValueType.Rank } }, IsFromSpell = true };
        Set(effect, "m_Buff", Ref<BlueprintBuffReference>((BlueprintBuff)c.Load(BlueprintGuid.Parse("035ed56eb973f0e469a288ff5991c9ff"))));
        var rank = new ContextRankConfig(); Set(rank, "m_BaseValueType", ContextRankBaseValueType.CasterLevel); Set(rank, "m_Progression", ContextRankProgression.AsIs);
        var a = new BlueprintAbility { AssetGuid = id, name = akashic ? "NewSpell.UseAkashicForm" : "CloakOfWindsAbility", Type = AbilityType.Spell, Range = akashic ? AbilityRange.Personal : AbilityRange.Close, ActionType = UnitCommand.CommandType.Standard, CanTargetSelf = true, CanTargetFriends = !akashic,
            ComponentsArray = new BlueprintComponent[] { new AbilityEffectRunAction { Actions = new ActionList { Actions = new GameAction[] { effect } } }, rank, new SpellComponent { School = akashic ? SpellSchool.Necromancy : SpellSchool.Abjuration } } };
        Set(a, "m_IsFullRoundAction", akashic); Set(a, "m_Icon", Get(Asset<BlueprintAbility>(c, "BurstOfAdrenaline"), "m_Icon"));
        if (akashic) { var restriction = new AbilityCasterInCombat(); Set(restriction, "Not", true); a.ComponentsArray = a.ComponentsArray.Concat(new[] { restriction }).ToArray(); }
        a.OnEnable(); return a;
    }
}
