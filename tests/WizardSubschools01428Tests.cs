using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class WizardSubschools01428Tests
{
    static int checks, grants, removals, applyCalls;
    static bool failAppend, nav = true, fog, island;
    static readonly List<Feature> levelFacts = new List<Feature>();
    static Type Module => typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WizardSubschools01428", true);
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static T Helper<T>(string n, params object[] a) => (T)AccessTools.Method(typeof(Migration01426Tests), n).Invoke(null, a);
    static BlueprintGuid Id(string token) => Helper<BlueprintGuid>("Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static BlueprintFeatureReference Reference(BlueprintFeature b) { var r = BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static void Check(bool b, string why) { if (!b) throw new Exception("Wizard01428: " + why); checks++; }
    static void Register(BlueprintsCache c) => AccessTools.Method(Module, "Register").Invoke(null, new object[] { c });
    static void Optional(BlueprintsCache c) => AccessTools.Method(Module, "Optional").Invoke(null, new object[] { c });
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string field) => (BlueprintFeatureReference[])Get(p, field) ?? Array.Empty<BlueprintFeatureReference>();
    static string Definition(object o) => (string)AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition").Invoke(null, new[] { o });
    static void AppendFault(BlueprintFeature[] feats) { if (failAppend && feats.Any(b => b.AssetGuid == Id("TeleportationSchool"))) throw new InvalidOperationException("01428 wizard transaction injection"); }
    static bool Reject(Action f) { try { f(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; return e is InvalidOperationException; } }
    static T Hydrate<T>(BlueprintsCache cache, string id, params string[] extra) where T : BlueprintScriptableObject, new() {
        var rows = PsychicExpansionTests.NativeRows.Where(r => (string)r["blueprint"]["AssetId"] == id).ToArray();
        Check(rows.Length == 1, "unique actual native row " + id);
        var previous = cache.Load(BlueprintGuid.Parse(id)) as BlueprintUnitFact;
        var obj = PsychicExpansionTests.NativeCase<T>(id); var row = rows[0]["blueprint"]["Data"];
        if (obj is BlueprintUnitFact && previous != null) Set(obj, "m_Icon", Get(previous, "m_Icon")); // Keep the harness's asset-only icon boundary.
        var fields = new[] { "Ranks", "Groups", "IsClassFeature", "IgnorePrerequisites", "Group", "Group2", "Mode", "m_Features", "m_AllFeatures", "LevelEntries", "m_Classes", "m_Archetypes", "m_UIDeterminatorsGroup", "UIGroups", "GiveFeaturesForPreviousLevels" }.Concat(extra);
        foreach (string name in fields.Distinct()) { var field = AccessTools.Field(typeof(T), name); if (field != null && row[name] != null) field.SetValue(obj, PsychicExpansionTests.Read(row[name], field.FieldType)); }
        obj.OnEnable(); cache.AddCachedBlueprint(obj.AssetGuid, obj); return obj;
    }
    static void ActivateGates(Feature fact, string name) {
        foreach (var runtime in fact.Components.Where(c => c.SourceBlueprintComponent is AddFeatureOnClassLevel).ToArray())
            using (runtime.RequestEventContext()) AccessTools.Method(typeof(AddFeatureOnClassLevel), name).Invoke(runtime.SourceBlueprintComponent, null);
    }
    static bool Grant(UnitEntityData __0, BlueprintUnitFact __1, ref EntityFact __result) {
        if (!__1.name.StartsWith("CotWWrathPsychicTeleportationShift")) return true;
        var f = Helper<Feature>("Fact", __0, (BlueprintFeature)__1); levelFacts.Add(f); grants++; __result = f; ActivateGates(f, "OnActivate"); return false;
    }
    static bool Remove(UnitEntityData __0, EntityFact __1) {
        if (__1 == null) return false;
        if (!(__1 is Feature f) || !f.Blueprint.name.StartsWith("CotWWrathPsychicTeleportationShift")) return true;
        ActivateGates(f, "OnDeactivate"); levelFacts.Remove(f); f.Owner.Facts.List.Remove(f);
        AccessTools.Method(Get(f.Owner.Facts, "m_Cache").GetType(), "Clear").Invoke(Get(f.Owner.Facts, "m_Cache"), null); removals++; return false;
    }
    static bool Nav(UnityEngine.Vector3 point) => nav;
    static bool Fog(UnityEngine.Vector3 point) => fog;
    static bool Island(TargetWrapper target) => island;
    static IEnumerable<CodeInstruction> DestinationSceneBoundary(IEnumerable<CodeInstruction> instructions) {
        int replaced = 0;
        foreach (var instruction in instructions) {
            if (instruction.operand is MethodInfo method) {
                string replacement = method.DeclaringType == typeof(AbilityCustomDimensionDoor) && method.Name == "CheckTargetIsOnDisabledIsland" ? nameof(Island) :
                    method.DeclaringType.Name == "ObstacleAnalyzer" && method.Name == "IsPointInsideNavMesh" ? nameof(Nav) :
                    method.DeclaringType.Name == "FogOfWarController" && method.Name == "IsInFogOfWar" ? nameof(Fog) : null;
                if (replacement != null) { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(WizardSubschools01428Tests), replacement); replaced++; }
            }
            yield return instruction;
        }
        if (replaced != 3) throw new InvalidOperationException("Native DimensionDoor scene input calls changed: " + replaced);
    }
    internal static int Run(BlueprintsCache cache) {
        checks = grants = removals = applyCalls = 0; levelFacts.Clear();
        var h = new Harmony("cotw.tests.wizard01428");
        var school = Hydrate<BlueprintFeatureSelection>(cache, "5f838049069f1ac4d804ce0862ab5110");
        var previousFeats = (BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("8c3102c2ff3b69444b139a98521a4899"));
        var registeredFeats = previousFeats == null ? Array.Empty<BlueprintFeatureReference>() : Refs(previousFeats, "m_Features").Concat(Refs(previousFeats, "m_AllFeatures")).Where(r => r != null).ToArray();
        var feats = Hydrate<BlueprintFeatureSelection>(cache, "8c3102c2ff3b69444b139a98521a4899");
        foreach (string field in new[] { "m_Features", "m_AllFeatures" }) Set(feats, field, Refs(feats, field).Concat(registeredFeats).GroupBy(r => r.Guid).Select(g => g.First()).ToArray());
        var universalist = Hydrate<BlueprintProgression>(cache, "0933849149cfc9244ac05d6a5b57fd80");
        var conjuration = Hydrate<BlueprintProgression>(cache, "567801abe990faf4080df566fadcd038");
        var baseFeature = Hydrate<BlueprintFeature>(cache, "cee0f7edbd874a042952ee150f878b84");
        var door = Hydrate<BlueprintAbility>(cache, "a9b8be9b87865744382f7c64e599aeb2", "Range", "CustomRange", "ActionType", "CanTargetPoint", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "m_Parent", "AvailableMetamagic", "Animation", "MaterialComponent");
        foreach (var row in PsychicExpansionTests.NativeRows.Where(r => ((JArray)r["blueprint"]["Data"]["Components"] ?? new JArray()).Any(c => ((string)c["$type"])?.EndsWith(", AddMetamagicFeat") == true)).ToArray())
            Hydrate<BlueprintFeature>(cache, (string)row["blueprint"]["AssetId"]);
        // Restore genuine linked feature metadata; the parent harness leaves some
        // unrelated entries reference-only, which must not replace these donors.
        var required = universalist.LevelEntries.Concat(conjuration.LevelEntries).SelectMany(e => (List<BlueprintFeatureBaseReference>)Get(e, "m_Features")).Select(x => x.Guid.ToString()).ToArray();
        foreach (string id in required.Where(x => x != baseFeature.AssetGuid.ToString()).Distinct()) {
            var row = PsychicExpansionTests.NativeRows.FirstOrDefault(r => (string)r["blueprint"]["AssetId"] == id);
            Check(row != null, "source progression dependency extracted " + id);
            if (((string)row["blueprint"]["Data"]["$type"]).EndsWith(", BlueprintFeatureSelection")) Hydrate<BlueprintFeatureSelection>(cache, id);
            else Hydrate<BlueprintFeature>(cache, id);
        }
        // Native LevelEntry.Features materializes a private read cache. Warm that
        // cache before comparing authored donors so ordinary resolution is not
        // misreported as a change to their actual feature lists.
        foreach (var entry in universalist.LevelEntries.Concat(conjuration.LevelEntries)) Check(entry.Features.All(x => x != null), "all native progression links resolve");
        var snapshots = new[] { Definition(universalist.LevelEntries), Definition(conjuration.LevelEntries), Definition(baseFeature.ComponentsArray), Definition(door.ComponentsArray), Definition(Get(universalist, "m_Classes")), Definition(Get(conjuration, "m_Classes")) };
        var before = new[] { Get(school, "m_Features"), Get(school, "m_AllFeatures") };
        var initial = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().ToHashSet();
        h.Patch(AccessTools.Method(typeof(HowlShakenTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats"), "Append"), postfix: new HarmonyMethod(typeof(WizardSubschools01428Tests), nameof(AppendFault)));
        h.Patch(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "Skip"));
        h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "No"));
        h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "PoolIgnore"));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game), "LevelUpController"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "NullController"));
        Provider01419Tests.PatchLabels(h);
        try {
            failAppend = true; Check(Reject(() => Register(cache)), "injected append failure rejected"); failAppend = false;
            Check(((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().All(initial.Contains), "failed transaction removes all owned new blueprints");
            Check(ReferenceEquals(before[0], Get(school, "m_Features")) && ReferenceEquals(before[1], Get(school, "m_AllFeatures")), "school arrays restored after mutation fault");
            Register(cache); var crafter = Asset<BlueprintProgression>(cache, "ArcaneCrafterSchool"); var teleport = Asset<BlueprintProgression>(cache, "TeleportationSchool"); Register(cache);
            Check(ReferenceEquals(crafter, Asset<BlueprintProgression>(cache, "ArcaneCrafterSchool")), "idempotent registration keeps root identity");
            foreach (string field in new[] { "m_Features", "m_AllFeatures" }) {
                var old = (BlueprintFeatureReference[])before[field == "m_Features" ? 0 : 1] ?? Array.Empty<BlueprintFeatureReference>(); var now = Refs(school, field);
                Check(now.Take(old.Length).SequenceEqual(old), "all original school entries/order retained " + field);
                Check(now.Count(r => r.Guid == crafter.AssetGuid) == 1 && now.Count(r => r.Guid == teleport.AssetGuid) == 1, "two distinct roots exactly once " + field);
            }
            Check(crafter.LevelEntries.Select(e => e.Level).SequenceEqual(new[] { 3, 8, 12, 16, 20 }), "Crafter replaces Hand at1 with Metacharge at3");
            foreach (var entry in universalist.LevelEntries.Where(e => e.Level > 1)) Check(Definition(entry) == Definition(crafter.LevelEntries.Single(e => e.Level == entry.Level)), "native universalist mastery retained " + entry.Level);
            Check(teleport.LevelEntries[0].Features.Count(x => x.AssetGuid.ToString() == "6c29030e9fea36949877c43a6f94ff31") == 2, "both native opposition choices retained");
            Check(Definition(teleport.LevelEntries.Single(e => e.Level == 8)) == Definition(conjuration.LevelEntries.Single(e => e.Level == 8)), "native level8 Dimensional Steps retained");
            Check(!ReferenceEquals(crafter.LevelEntries, universalist.LevelEntries) && !ReferenceEquals(teleport.LevelEntries, conjuration.LevelEntries) && !ReferenceEquals(Get(crafter, "m_Classes"), Get(universalist, "m_Classes")), "progression arrays isolated");
            var shift = Asset<BlueprintFeature>(cache, "TeleportationShiftFeature");
            Check(shift.GetComponent<AddFacts>() == null && Definition(shift.GetComponent<AddSpecialSpellList>()) == Definition(baseFeature.GetComponent<AddSpecialSpellList>()) && Definition(shift.GetComponent<AddClassLevelToSummonDuration>()) == Definition(baseFeature.GetComponent<AddClassLevelToSummonDuration>()), "Acid Dart replaced; native specialist list and summon duration preserved");
            var wizard = (BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e")); var u = Helper<UnitEntityData>("Unit", cache, "01428-wizard");
            u.Progression.Classes.Clear(); var cd = new ClassData(wizard) { Level = 1 }; u.Progression.Classes.Add(cd);
            Selection(cache, h, u, cd, school, feats, crafter, teleport);
            Shift(cache, h, u, cd, shift);
            var after = new[] { Definition(universalist.LevelEntries), Definition(conjuration.LevelEntries), Definition(baseFeature.ComponentsArray), Definition(door.ComponentsArray), Definition(Get(universalist, "m_Classes")), Definition(Get(conjuration, "m_Classes")) };
            Check(snapshots.SequenceEqual(after), "all native donor components/classes/progression arrays unchanged; changed indices=" + string.Join(",", Enumerable.Range(0, snapshots.Length).Where(i => snapshots[i] != after[i])));
            var added = ((IDictionary)Get(cache, "m_LoadedBlueprints")).Keys.Cast<BlueprintGuid>().Where(id => !initial.Contains(id)).Select(cache.Load).Where(b => b.name.StartsWith("CotWWrathPsychic") && (b.name.Contains("Teleportation") || b.name.Contains("ArcaneCrafter"))).ToArray();
            PsychicExpansionTests.Inventory["wizardSubschools01428Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 2, ["nativeSelectFeatureApplyCalls"] = applyCalls, ["nativeLevelGrants"] = grants, ["nativeLevelRemovals"] = removals, ["newBlueprints"] = new JArray(added.Select(b => new JObject { ["id"] = b.AssetGuid.ToString(), ["name"] = b.name })), ["boundary"] = "Actual native selection, progression level, AddFeatureOnClassLevel lifecycle, range/resource and DimensionDoor target restriction/GetTargets execute. Unit creation/final feature insertion, navmesh/fog/island scene inputs and SelectFeature final insertion are controlled. DimensionDoor scene relocation, mounted animation, complete leveling UI and save reconstruction remain game steps. No new global Harmony or spellbook patch." };
            Console.WriteLine("PASS: " + checks + " wizard subschool contracts."); return checks;
        } finally { failAppend = false; h.UnpatchAll(h.Id); }
    }
    static void Selection(BlueprintsCache cache, Harmony h, UnitEntityData u, ClassData cd, BlueprintFeatureSelection school, BlueprintFeatureSelection feats, BlueprintProgression crafter, BlueprintProgression teleport) {
        var state = (LevelUpState)FormatterServices.GetUninitializedObject(typeof(LevelUpState)); Set(state, "m_Unit", u); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 1); state.NextClassLevel = 1; state.SelectedClass = cd.CharacterClass;
        var pending = state.AddSelection(null, default(FeatureSource), school, 1);
        Check(school.CanSelect(u.Descriptor, state, pending, crafter) && school.CanSelect(u.Descriptor, state, pending, teleport), "native school selection offers both roots at wizard1");
        var forbidden = (BlueprintFeature)cache.Load(BlueprintGuid.Parse("ca4a0d68c0408d74bb83ade784ebeb0d")); var fact = Helper<Feature>("Fact", u, forbidden);
        Check(!school.CanSelect(u.Descriptor, state, pending, teleport), "native Conjuration opposition prerequisite retained"); u.Facts.List.Remove(fact); AccessTools.Method(Get(u.Facts, "m_Cache").GetType(), "Clear").Invoke(Get(u.Facts, "m_Cache"), null);
        var choice = Asset<BlueprintFeatureSelection>(cache, "ArcaneCrafterMetacharge");
        Check(choice.AllFeatures.Any() && choice.AllFeatures.All(b => b.HasGroup(FeatureGroup.WizardFeat) && b.GetComponent<AddMetamagicFeat>() != null), "actual registered metamagics only");
        Check(choice.AllFeatures.All(b => Refs(feats, "m_AllFeatures").Any(r => r.Guid == b.AssetGuid) || Refs(feats, "m_Features").Any(r => r.Guid == b.AssetGuid)), "all metamagic choices originate in native wizard pool");
        var old = new[] { Get(feats, "m_Features"), Get(feats, "m_AllFeatures") };
        var leaf = new BlueprintFeature { AssetGuid = Id("01428WizardLateMetamagic"), name = "01428LateMetamagicFixture", Ranks = 1, Groups = new[] { FeatureGroup.WizardFeat }, ComponentsArray = new BlueprintComponent[] { new AddMetamagicFeat { Metamagic = Metamagic.Extend }, new PrerequisiteStatValue { Stat = StatType.Intelligence, Value = 18 } } }; leaf.OnEnable(); cache.AddCachedBlueprint(leaf.AssetGuid, leaf);
        try {
            Set(feats, "m_AllFeatures", Refs(feats, "m_AllFeatures").Concat(new[] { Reference(leaf), Reference(leaf) }).ToArray()); Optional(cache); Optional(cache);
            Check(choice.AllFeatures.Count(b => b == leaf) == 1, "late metamagic provider refresh deduplicates actual pool");
            Register(cache); Check(ReferenceEquals(choice, Asset<BlueprintFeatureSelection>(cache, "ArcaneCrafterMetacharge")) && choice.AllFeatures.Count(b => b == leaf) == 1, "repeat registration after late provider preserves identity and dynamic choices");
            cd.Level = 3; state.NextClassLevel = 3; Set(state, "NextCharacterLevel", 3); var pick = state.AddSelection(null, default(FeatureSource), choice, 3);
            u.Stats.Intelligence.BaseValue = 16; Check(!choice.CanSelect(u.Descriptor, state, pick, leaf), "child prerequisite blocks low Intelligence"); u.Stats.Intelligence.BaseValue = 18; Check(choice.CanSelect(u.Descriptor, state, pick, leaf), "eligible child uses native CanSelect");
            Set(u.Progression, "m_Selections", Activator.CreateInstance(AccessTools.Field(typeof(UnitProgressionData), "m_Selections").FieldType));
            h.Patch(AccessTools.Method(typeof(SelectFeature), "Apply"), transpiler: new HarmonyMethod(typeof(Feat01424Tests), "ApplyBoundary"));
            var select = new SelectFeature(pick, leaf); Check(select.Check(state, u.Descriptor), "native SelectFeature.Check accepts qualified bonus metamagic"); select.Apply(state, u.Descriptor); applyCalls++;
            Check(u.HasFact(leaf) && pick.SelectedItem == leaf, "native selection applies chosen metamagic fact");
            var refsBefore = Get(choice, "m_AllFeatures"); Set(feats, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>()); Set(feats, "m_Features", Array.Empty<BlueprintFeatureReference>());
            Check(Reject(() => Optional(cache)) && ReferenceEquals(refsBefore, Get(choice, "m_AllFeatures")), "invalid late provider leaves valid Metacharge pool untouched");
        } finally { Set(feats, "m_Features", old[0]); Set(feats, "m_AllFeatures", old[1]); Optional(cache); cache.RemoveCachedBlueprint(leaf.AssetGuid); }
        foreach (int level in new[] { 1, 2, 3, 7, 8, 12, 16, 20 }) { cd.Level = level; Check(crafter.CalcLevel(u.Descriptor) == level && teleport.CalcLevel(u.Descriptor) == level, "native wizard progression level " + level); }
    }
    static void Shift(BlueprintsCache cache, Harmony h, UnitEntityData u, ClassData cd, BlueprintFeature shift) {
        var resource = Asset<BlueprintAbilityResource>(cache, "TeleportationShiftResource");
        foreach (int stat in new[] { 1, 6, 10, 18, 30 }) { Set(u.Stats.Intelligence, "m_BaseValue", stat); Set(u.Stats.Intelligence, "ModifiedValue", stat); Check(resource.GetMaxAmount(u.Descriptor) == Math.Max(1, 3 + (int)Math.Floor((stat - 10) / 2.0)), "native Intelligence pool " + stat); }
        Set(u.Stats.Intelligence, "m_BaseValue", 18); Set(u.Stats.Intelligence, "ModifiedValue", 18); u.Descriptor.Resources.Add(resource, true);
        h.Patch(typeof(UnitHelper).GetMethods().Single(m => m.Name == "AddFact" && !m.IsGenericMethod && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(UnitEntityData), typeof(BlueprintUnitFact), typeof(MechanicsContext), typeof(FeatureParam) })), prefix: new HarmonyMethod(typeof(WizardSubschools01428Tests), nameof(Grant)));
        h.Patch(AccessTools.Method(typeof(UnitHelper), "RemoveFact", new[] { typeof(UnitEntityData), typeof(EntityFact) }), prefix: new HarmonyMethod(typeof(WizardSubschools01428Tests), nameof(Remove)));
        var host = Helper<Feature>("Fact", u, shift); cd.Level = 1; ActivateGates(host, "OnActivate");
        foreach (int level in new[] { 1, 2, 3, 4, 5, 8, 18, 19, 20, 21, 4, 1, 0 }) {
            cd.Level = level; ActivateGates(host, "HandleUnitGainLevel"); foreach (var f in levelFacts.ToArray()) ActivateGates(f, "HandleUnitGainLevel");
            var abilities = levelFacts.SelectMany(f => f.Blueprint.GetComponents<AddFacts>()).SelectMany(a => a.Facts).OfType<BlueprintAbility>().ToArray();
            Check(abilities.Length == (level == 0 ? 0 : 1), "exactly one native level-window Shift " + level);
            if (level == 0) continue;
            var a = abilities[0]; int expected = 5 * Math.Min(10, Math.Max(1, level / 2)); var data = new AbilityData(a, u.Descriptor);
            Check(Math.Abs(a.GetRange(false, data).Meters - new Feet(expected).Meters) < .001f, "native custom range " + level);
            Check(a.Type == AbilityType.Supernatural && a.ActionType == Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift && a.AvailableMetamagic == 0 && a.Parent == null, "supernatural swift standalone Shift");
            Check(!data.ProvokeAttackOfOpportunity, "native supernatural Shift does not provoke");
            Check(a.ComponentsArray.All(c => c.GetType().Name != "LineOfSightIgnorance" && c.GetType().Name != "DimensionDoorRestrictionIgnorance"), "no sight or validity bypass");
            var cost = a.GetComponent<AbilityResourceLogic>(); u.Descriptor.Resources.Restore(resource); int amount = u.Descriptor.Resources.GetResourceAmount(resource);
            Check(cost.CalculateCost(data) == 1 && cost.IsAbilityRestrictionPassed(data), "native resource availability"); cost.Spend(data); Check(u.Descriptor.Resources.GetResourceAmount(resource) == amount - 1, "spend exactly once");
            u.Descriptor.Resources.Spend(resource, u.Descriptor.Resources.GetResourceAmount(resource)); Check(!cost.IsAbilityRestrictionPassed(data), "zero uses rejects Shift");
            var delivery = a.GetComponent<AbilityCustomDimensionDoor>(); var targets = (List<UnitEntityData>)AccessTools.Method(typeof(AbilityCustomDimensionDoor), "GetTargets").Invoke(delivery, new object[] { u });
            Check(delivery.Radius.Meters == 0 && targets.Count == 1 && targets[0] == u, "native caster-only teleport candidate list");
        }
        cd.Level = 8; ActivateGates(host, "HandleUnitGainLevel"); foreach (var f in levelFacts.ToArray()) ActivateGates(f, "HandleUnitGainLevel");
        int prior = grants; ActivateGates(host, "OnActivate"); foreach (var f in levelFacts.ToArray()) ActivateGates(f, "OnActivate"); Check(grants == prior, "native repeat activation retains existing AppliedFact without duplicates");
        ActivateGates(host, "OnPostLoad"); foreach (var f in levelFacts.ToArray()) ActivateGates(f, "OnPostLoad"); Check(grants == prior, "native postload with owned facts keeps same grants");
        var classBefore = cd.Level; cd.Level = 0;
        var druid = new ClassData((BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse("610d836f3a3a9ed42a4349b62f002e96"))) { Level = 19 }; u.Progression.Classes.Add(druid);
        Check(Asset<BlueprintProgression>(cache, "TeleportationSchool").CalcLevel(u.Descriptor) == 0, "nineteen other class levels do not advance wizard school");
        ActivateGates(host, "HandleUnitGainLevel"); foreach (var f in levelFacts.ToArray()) ActivateGates(f, "HandleUnitGainLevel");
        Check(!levelFacts.SelectMany(f => f.Blueprint.GetComponents<AddFacts>()).SelectMany(a => a.Facts).OfType<BlueprintAbility>().Any(), "other class levels cannot grant Shift"); u.Progression.Classes.Remove(druid); cd.Level = classBefore;
        ActivateGates(host, "OnDeactivate"); Check(levelFacts.Count == 0 && removals > 0, "native removal cascades all range branches");
        var a5 = Asset<BlueprintAbility>(cache, "TeleportationShift5"); var d5 = a5.GetComponent<AbilityCustomDimensionDoor>();
        h.Patch(AccessTools.Method(typeof(AbilityCustomDimensionDoor), "IsTargetRestrictionPassed"), transpiler: new HarmonyMethod(typeof(WizardSubschools01428Tests), nameof(DestinationSceneBoundary)));
        var point = new TargetWrapper(new UnityEngine.Vector3(1, 0, 1)); nav = true; fog = island = false; Check(d5.IsTargetRestrictionPassed(u, point), "native valid visible navmesh destination allowed");
        nav = false; Check(!d5.IsTargetRestrictionPassed(u, point), "native off-navmesh point rejected"); nav = true; fog = true; Check(!d5.IsTargetRestrictionPassed(u, point), "native fog point rejected"); fog = false; island = true; Check(!d5.IsTargetRestrictionPassed(u, point), "native disabled island rejected"); island = false;
    }
}
