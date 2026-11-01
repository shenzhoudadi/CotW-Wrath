using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using CallOfTheWild.Wrath.Prototype;

internal static class Prerequisites01426Tests
{
    static int checks;
    static readonly System.Reflection.Assembly Mod = typeof(LinnormStyleEffect).Assembly;
    static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o, string f, object v) => (AccessTools.Field(o.GetType(), f) ?? AccessTools.Field(o.GetType(), "<" + f + ">k__BackingField")).SetValue(o, v);
    static void Check(bool ok, string why) { if (!ok) throw new Exception("Prerequisites01426: " + why); checks++; }
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static bool No(ref bool __result) { __result = false; return false; }
    static bool Skip() => false;
    static bool NullController(ref LevelUpController __result) { __result = null; return false; }
    static UnitEntityData Unit(BlueprintsCache c, string n) => (UnitEntityData)AccessTools.Method(typeof(SpellAndSword01417Tests), "Unit").Invoke(null, new object[] { c, n });
    static Feature Fact(UnitEntityData u, BlueprintFeature f, FeatureParam parameter = null) => (Feature)AccessTools.Method(typeof(FeralGraceTests), "Fact").Invoke(null, new object[] { u, f, parameter });
    static bool Need(Prerequisite p, UnitEntityData u) => (bool)AccessTools.Method(p.GetType(), "CheckInternal").Invoke(p, new object[] { null, u.Descriptor, null });
    static BlueprintParametrizedFeature Native(BlueprintsCache cache, string id) {
        var bp = PsychicExpansionTests.NativeCase<BlueprintParametrizedFeature>(id);
        var d = PsychicExpansionTests.NativeRows.Single(x => (string)x["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
        foreach (string field in new[] { "ParameterType", "RequireProficiency", "m_Prerequisite", "Ranks" }) {
            var f = AccessTools.Field(bp.GetType(), field); f.SetValue(bp, PsychicExpansionTests.Read(d[field], f.FieldType));
        }
        cache.AddCachedBlueprint(bp.AssetGuid, bp); return bp;
    }
    internal static int Run(BlueprintsCache cache) {
        checks = 0; var h = new Harmony("CotW01426.PrerequisiteContracts");
        h.Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), prefix: new HarmonyMethod(typeof(Prerequisites01426Tests), nameof(Skip)));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(Prerequisites01426Tests), nameof(No)));
        h.Patch(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), prefix: new HarmonyMethod(typeof(Prerequisites01426Tests), nameof(NullController)));
        Provider01419Tests.PatchLabels(h);
        // Install the actual production patch classes, not copies of their logic.
        foreach (string name in new[] { "ReapingStalkerWeaponSelectionPatch", "ReapingStalkerExplicitPrerequisitePatch", "ReapingStalkerParameterRootPrerequisitePatch" })
            h.CreateClassProcessor(Mod.GetType("CallOfTheWild.Wrath.Prototype." + name, true)).Patch();
        var critical = Native(cache, "f4201c85a991369408740c6888362e20");
        var mythic = Native(cache, "8bc0190a4ec04bd489eec290aeaa6d07");
        var trickster = Native(cache, "56f94badbba018b4b8277ce6e2e79e72");
        var talent = (BlueprintFeature)cache.Load(BlueprintGuid.Parse("d6f431f7f89b21f0f729823390703c50"));
        var plain = Unit(cache, "01426-without" ); var owner = Unit(cache, "01426-with-talent"); Fact(owner, talent);
        var pending = Empty<FeatureSelectionState>(); Set(pending, "Selection", mythic);
        var state = Empty<LevelUpState>(); Set(state, "m_Unit", owner); Set(state, "Selections", new List<FeatureSelectionState>());
        try {
            foreach (var u in new[] { plain, owner }) foreach (var category in new[] { WeaponCategory.Sickle, WeaponCategory.Scythe, WeaponCategory.Longsword, WeaponCategory.Bite }) {
                var need = new PrerequisiteParametrizedFeature { ParameterType = FeatureParameterType.WeaponCategory, WeaponCategory = category };
                Set(need, "m_Feature", Ref<BlueprintFeatureReference>(critical));
                Check(Need(need, u) == (u == owner && (category == WeaponCategory.Sickle || category == WeaponCategory.Scythe)), "actual explicit weapon prerequisite " + u.CharacterName + category);
                need.ParameterType = FeatureParameterType.SpellSchool;
                Check(!Need(need, u), "wrong parameter kind does not qualify");
                need.ParameterType = FeatureParameterType.WeaponCategory; Set(need, "m_Feature", Ref<BlueprintFeatureReference>(mythic));
                Check(!Need(need, u), "not Improved Critical (Mythic)");
            }
            var generic = new PrerequisiteFeature(); Set(generic, "m_Feature", Ref<BlueprintFeatureReference>(critical));
            Check(!Need(generic, owner), "standalone generic IC prerequisite is not broadened");
            Check(Need(mythic.GetComponent<PrerequisiteFeature>(), owner) && !Need(mythic.GetComponent<PrerequisiteFeature>(), plain), "native mythic root gate recognizes the two eligible weapon options");
            Check(!owner.HasFact(critical) && owner.Descriptor.GetFeature(critical, WeaponCategory.Sickle) == null, "qualification does not grant actual IC facts globally");
            foreach (var bp in new[] { mythic, trickster }) {
                var categories = new[] { WeaponCategory.Sickle, WeaponCategory.Scythe, WeaponCategory.Longsword };
                var items = categories.Select(c => new FeatureUIData(bp, c, c.ToString(), "", null, c.ToString())).ToArray(); Set(bp, "m_CachedItems", items);
                // Trickster's player-wide path prerequisite must still be enforced.
                var originalComponents = bp.ComponentsArray;
                var playerNeed = bp.GetComponent<PrerequisitePlayerHasFeature>();
                if (playerNeed != null) {
                    var replacement = new PrerequisiteStatValue { Stat = StatType.BaseAttackBonus, Value = 30 };
                    var original = bp.ComponentsArray; bp.ComponentsArray = original.Select(c => ReferenceEquals(c, playerNeed) ? replacement : c).ToArray(); bp.OnEnable();
                    owner.Descriptor.Proficiencies.Add(WeaponCategory.Sickle);
                    Check(!bp.MeetsPrerequisites(pending, owner.Descriptor, state, false, false), "native root prerequisite evaluation rejects unrelated missing condition");
                    owner.Stats.BaseAttackBonus.BaseValue = 30;
                    Check(bp.MeetsPrerequisites(pending, owner.Descriptor, state, false, false) && bp.CanSelect(owner.Descriptor, state, pending, items[0]), "root and parameter both pass only after independent condition is satisfied");
                    // Retain a controlled ordinary condition instead of invoking Player/Unity services.
                }
                foreach (var u in new[] { plain, owner }) {
                    foreach (int i in Enumerable.Range(0, items.Length)) {
                        u.Descriptor.Proficiencies.Add(categories[i]);
                        Check(bp.CanSelect(u.Descriptor, state, pending, items[i]) == (u == owner && i < 2), "native weapon-selection path " + bp.name + categories[i] + (u == owner));
                    }
                }
                while (owner.Descriptor.Proficiencies.Contains(WeaponCategory.Scythe)) owner.Descriptor.Proficiencies.Remove(WeaponCategory.Scythe);
                Check(!bp.CanSelect(owner.Descriptor, state, pending, items[1]), "proficiency remains required"); owner.Descriptor.Proficiencies.Add(WeaponCategory.Scythe);
                Fact(owner, bp, WeaponCategory.Sickle);
                Check(!bp.CanSelect(owner.Descriptor, state, pending, items[0]), "duplicate actual chosen feat remains rejected");
                Set(bp, "m_CachedItems", null); bp.ComponentsArray = originalComponents; bp.OnEnable();
            }
            Fact(plain, critical, WeaponCategory.Longsword);
            var longsword = new PrerequisiteParametrizedFeature { ParameterType = FeatureParameterType.WeaponCategory, WeaponCategory = WeaponCategory.Longsword }; Set(longsword, "m_Feature", Ref<BlueprintFeatureReference>(critical));
            Check(Need(longsword, plain), "real IC on another weapon retains normal behavior");
            var other = new BlueprintParametrizedFeature { ParameterType = FeatureParameterType.WeaponCategory, RequireProficiency = false, ComponentsArray = Array.Empty<BlueprintComponent>() }; Set(other, "m_Prerequisite", Ref<BlueprintParametrizedFeatureReference>(mythic));
            var otherItem = new FeatureUIData(other, WeaponCategory.Scythe, "scythe", "", null, "scythe"); Set(other, "m_CachedItems", new[] { otherItem }); other.OnEnable();
            Check(!other.CanSelect(owner.Descriptor, state, pending, otherItem), "talent does not bypass later mythic/chain prerequisites");
            var preview = Unit(cache, "01426-preview"); Fact(preview, talent);
            longsword.WeaponCategory = WeaponCategory.Scythe; Check(Need(longsword, preview), "qualification reads preview's own facts");
            var talentFact = preview.Progression.Features.Enumerable.Single(f => f.Blueprint == talent); preview.Facts.List.Remove(talentFact); AccessTools.Method(typeof(FeralGraceTests), "Refresh").Invoke(null, new object[] { preview });
            Check(!Need(longsword, preview), "removal/respec immediately revokes qualification");
            PsychicExpansionTests.Inventory["prerequisites01426Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["productionPatchesInstalled"] = 3, ["nativeCanSelect"] = true, ["noGrantedCriticalFacts"] = true, ["boundary"] = "Actual native explicit prerequisites and parametrized CanSelect with production patches. Controlled unit/level-up data, cached labels/items, IgnorePrerequisites=false. Trickster player-wide service replaced by an ordinary failing/passing native prerequisite for isolation; original blueprint prerequisite identity separately audited. Combat regressions remain the unchanged 01425 27 cases." };
            Console.WriteLine("PASS: " + checks + " Reaping Stalker prerequisite contracts."); return checks;
        }
        finally { h.UnpatchAll(h.Id); }
    }
}
