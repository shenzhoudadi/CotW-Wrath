using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Units;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.ResourceManagement;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

internal static class Sunsinger01443Tests
{
    static readonly Type Module = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.Sunsinger01443", true);
    static readonly string[] Tokens = (string[])AccessTools.Field(Module, "OwnTokens").GetValue(null);
    const string Skald = "6afa347d804838b48bda16acb0573dc0", Deities = "59e7a76987fe3b547b9cce045f4db3e4", Sarenrae = "c1c4f7f64842e7e48849e5e67be11a1b";
    const string Root = "SunsingerArchetype01443", Channel = "SunsingerChannelSolarEnergy01443", Resource = "SunsingerChannelResource01443", Heal = "SunsingerChannelHeal01443", Harm = "SunsingerChannelHarm01443", Extra = "SunsingerExtraChannel01443";
    const string Toggle = "SunsingerPillarToggle01443", Host = "SunsingerPillarHost01443", Area = "SunsingerPillarArea01443", Effect = "SunsingerPillarEffect01443", Immune = "SunsingerPillarImmunity01443";
    static int checks, serial, cacheFaults, appendFaults, spends, saves, castPrepares, outlines;
    static int healRules, damageRules;
    static int warmedDonors, jsonRoundTrips, resourceLinkLoads;
    static int sacredCatalogueEndpoints, specificImmunityBranches, nativeRankChanges;
    static RuleHealDamage lastHeal;
    static RuleDealDamage lastDamage;
    static bool cacheFault, appendFault;
    static readonly List<UnitEntityData> Units = new List<UnitEntityData>();
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static object Call(Type t, string n, params object[] args) => AccessTools.Method(t, n).Invoke(null, args);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static void Check(bool ok, string what) { if (!ok) throw new Exception("Sunsinger01443: " + what); checks++; }
    static void Register(BlueprintsCache c) => Call(Module, "Register", c);
    static void Stable(BlueprintsCache c)
    {
        try { Register(c); }
        catch {
            var table = AccessTools.Field(Module, "Published").GetValue(null); var args = new object[] { c, null }; AccessTools.Method(table.GetType(), "TryGetValue").Invoke(table, args); var publication = args[1];
            if (publication != null) { var assets = (BlueprintScriptableObject[])Get(publication, "Assets"); var before = (string[])Get(publication, "Definitions"); for (int i = 0; i < assets.Length; i++) { var after = (string)Call(Module, "Definition", assets[i]); if (before[i] == after) continue; int n = 0; while (n < before[i].Length && n < after.Length && before[i][n] == after[n]) n++; Console.Error.WriteLine("043 authored definition delta " + assets[i].name + " at " + n + " BEFORE=" + before[i].Substring(Math.Max(0, n - 120), Math.Min(500, before[i].Length - Math.Max(0, n - 120))) + " AFTER=" + after.Substring(Math.Max(0, n - 120), Math.Min(500, after.Length - Math.Max(0, n - 120)))); } }
            throw;
        }
    }
    static bool Reject(Action action) { try { action(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    // Migration01426.Definition also has an (object, IEqualityComparer) overload.
    static string Definition(object value) => (string)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.Migration01426"), "Definition", new[] { typeof(object) }).Invoke(null, new object[] { value });
    static bool Enemy(UnitEntityData __instance, UnitEntityData __0, ref bool __result) { var hostile = (UnitEntityData)AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").GetValue(null); __result = (__instance == hostile) != (__0 == hostile); return false; }
    static bool SacredCatalogue(AddFacts __instance)
    {
        if (__instance.OwnerBlueprint?.AssetGuid.ToString() != Sarenrae || __instance.name != "$AddFacts$b10fd6bc-12f8-4488-b479-84ed0cf6977d") return true;
        var actual = (BlueprintUnitFactReference[])Get(__instance, "m_Facts"); var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == Sarenrae)["blueprint"]["Data"]["Components"].Single(r => (string)r["name"] == __instance.name);
        if (actual.Length != 30 || !actual.Select(r => "!bp_" + r.Guid).SequenceEqual(row["m_Facts"].Values<string>())) throw new Exception("043 Sarenrae independent native Sacred Weapon catalogue endpoint changed");
        sacredCatalogueEndpoints++; return false;
    }
    static bool Dice(DiceFormula __0, ref int __result) { __result = __0.Dice == DiceType.D20 ? (int)AccessTools.Field(typeof(BardMasterpieces01435Tests), "roll").GetValue(null) : __0.Dice == DiceType.Zero ? 0 : __0.Rolls * 4; return false; }
    static BundledResourceHandle<UnityEngine.GameObject> PrefabRequest(string id, bool hold) { var handle = Empty<BundledResourceHandle<UnityEngine.GameObject>>(); GC.SuppressFinalize(handle); return handle; }
    static UnityEngine.GameObject PrefabObject(BundledResourceHandle<UnityEngine.GameObject> handle) => null;
    static bool PrefabHasLoaded(string id) => false;
    static IEnumerable<CodeInstruction> LoadBoundary(IEnumerable<CodeInstruction> input)
    {
        int requests = 0, objects = 0, held = 0; foreach (var code in input) { if (code.operand is MethodInfo m) {
            if (m.DeclaringType.IsGenericType && m.DeclaringType.GetGenericTypeDefinition() == typeof(BundledResourceHandle<>) && m.Name == "Request") { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Sunsinger01443Tests), nameof(PrefabRequest)); requests++; }
            else if (m.DeclaringType.IsGenericType && m.DeclaringType.GetGenericTypeDefinition() == typeof(BundledResourceHandle<>) && m.Name == "get_Object") { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Sunsinger01443Tests), nameof(PrefabObject)); objects++; }
            else if (m == AccessTools.Method(typeof(ResourcesLibrary), "HasLoadedResource", new[] { typeof(string) })) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Sunsinger01443Tests), nameof(PrefabHasLoaded)); held++; }
        } yield return code; } if (requests != 1 || objects != 2 || held != 1) throw new Exception("043 native Load resource endpoints changed");
    }
    static void NoUnload(string assetId) { }
    static IEnumerable<CodeInstruction> UnloadBoundary(IEnumerable<CodeInstruction> input)
    {
        int count = 0; foreach (var code in input) { if (code.operand is MethodInfo method && method == AccessTools.Method(typeof(ResourcesLibrary), "ForceUnloadResource", new[] { typeof(string) })) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Sunsinger01443Tests), nameof(NoUnload)); count++; } yield return code; } if (count != 1) throw new Exception("043 native ForceUnload resource endpoint changed");
    }
    static T EffectRule<T>(MechanicsContext context, T rule) where T : RulebookEvent
    {
        if (rule is RuleDealDamage damage) { damageRules++; lastDamage = damage; return rule; }
        if (rule is RuleHealDamage heal) { healRules++; lastHeal = heal; heal.IsFake = true; }
        return (T)AccessTools.Method(typeof(BardMasterpieces01435Tests), "ContextRule").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { context, rule });
    }
    static IEnumerable<CodeInstruction> EffectBoundary(IEnumerable<CodeInstruction> input)
    {
        foreach (var code in input) { if (code.operand is MethodInfo m && m.IsGenericMethod && m.Name == "TriggerRule" && m.DeclaringType == typeof(MechanicsContext)) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(Sunsinger01443Tests), nameof(EffectRule)).MakeGenericMethod(m.GetGenericArguments()); } yield return code; }
    }
    static void Hydrate(BlueprintsCache c, bool reset)
    {
        foreach (string id in ((string[])AccessTools.Field(Module, "NativeIds").GetValue(null)).Concat(new[] { "d5ee498e19722854198439629c1841a5" })) {
            var bp = c.Load(BlueprintGuid.Parse(id)) as BlueprintScriptableObject ?? throw new Exception("043 native dependency missing " + id);
            var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == id)["blueprint"]["Data"];
            var names = bp is BlueprintCharacterClass ? new[] { "m_Progression", "m_Archetypes" } : bp is BlueprintProgression ? new[] { "LevelEntries", "UIGroups", "m_UIDeterminatorsGroup" } : bp is BlueprintAbilityResource ? new[] { "m_MaxAmount", "m_Min", "m_UseMax", "m_Max" } : bp is BlueprintFeatureSelection ? new[] { "m_Features", "m_AllFeatures", "Ranks", "Groups", "IsClassFeature", "IgnorePrerequisites" } : bp is BlueprintAbility ? new[] { "Type", "ActionType", "Range", "CanTargetSelf", "CanTargetFriends", "CanTargetEnemies", "SpellResistance", "AvailableMetamagic" } : bp is BlueprintActivatableAbility ? new[] { "m_Buff", "Group", "ActivationType", "m_ActivateWithUnitCommand", "DeactivateIfCombatEnded", "DeactivateIfOwnerDisabled", "OnlyInCombat" } : bp is BlueprintAbilityAreaEffect ? new[] { "m_TargetType", "Size", "Shape", "AffectEnemies", "AggroEnemies", "AffectDead", "SpellResistance", "Fx" } : bp is BlueprintBuff ? new[] { "Stacking", "m_Flags", "Ranks" } : new[] { "Ranks", "Groups", "IsClassFeature" };
            foreach (var name in names) { var field = AccessTools.Field(bp.GetType(), name); if (field == null || row[name] == null) continue; var current = field.GetValue(bp); bool parent = bp is BlueprintFeatureSelection || bp is BlueprintProgression || bp is BlueprintCharacterClass; bool absent = current == null || current is Array a && a.Length == 0 || current is BlueprintReferenceBase r && r.Guid == default(BlueprintGuid); if (reset || !parent || absent) field.SetValue(bp, PsychicExpansionTests.Read(row[name], field.FieldType)); }
        }
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var c = (BlueprintsCache)Call(typeof(TimeStutter01437Tests), "Fresh", parent); foreach (var token in Tokens) c.RemoveCachedBlueprint(Id(token));
        var before = ResourcesLibrary.BlueprintsCache; AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
        try {
            foreach (string id in new[] { "f5fc9a1a2a3c1a946a31b320d1dd31b2", "279447a6bf2d3544d93a0a39c3b8e91d", "cd9f19775bd9d3343a31a065e93f0c47", "ddaec3a5845bc7d4191792529b687d65" }.Concat((string[])AccessTools.Field(Module, "FascinateIds").GetValue(null))) {
                var prior = c.Load(BlueprintGuid.Parse(id)); var fresh = (SimpleBlueprint)AccessTools.Method(typeof(PsychicExpansionTests), "NativeCase").MakeGenericMethod(prior.GetType()).Invoke(null, new object[] { id }); c.AddCachedBlueprint(fresh.AssetGuid, fresh);
            }
            Hydrate(c, true);
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, before); } return c;
    }
    static void WalkElements(object value, HashSet<object> seen, Action<Element> action)
    {
        if (value == null || value is string || value is SimpleBlueprint || value is BlueprintReferenceBase || value is UnityEngine.Object || value.GetType().IsPrimitive || value.GetType().IsEnum || !seen.Add(value)) return;
        if (value is Element element) action(element);
        if (value is IEnumerable list) { foreach (var item in list) WalkElements(item, seen, action); return; }
        for (var t = value.GetType(); t != null && t != typeof(object) && t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType) foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!f.IsStatic) WalkElements(f.GetValue(value), seen, action);
    }
    static void WarmDonorClones(BlueprintsCache parent)
    {
        var previous = ResourcesLibrary.BlueprintsCache;
        try {
            var c = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
            var donors = new[] { "f5fc9a1a2a3c1a946a31b320d1dd31b2", "279447a6bf2d3544d93a0a39c3b8e91d", "cd9f19775bd9d3343a31a065e93f0c47", "ddaec3a5845bc7d4191792529b687d65" }.Concat((string[])AccessTools.Field(Module, "FascinateIds").GetValue(null)).Select(id => Native<BlueprintScriptableObject>(c, id)).ToArray();
            foreach (var donor in donors) { WalkElements(donor.ComponentsArray, new HashSet<object>(), e => donor.AddToElementsList(e)); donor.OnEnable(); if (donor is BlueprintAbility ability) { var a = ability.Restrictions; var b = ability.TargetRestrictions; var d = ability.CasterRestrictions; var full = ability.IsFullRoundAction; } }
            var lists = donors.Select(d => Get(d, "m_AllElements")).ToArray(); var arrays = donors.Select(d => d.ElementsArray).ToArray(); var members = arrays.Select(a => a.ToArray()).ToArray(); var owners = members.Select(a => a.Select(e => e.Owner).ToArray()).ToArray(); var definitions = donors.Select(d => Definition(d.ComponentsArray)).ToArray();
            Register(c); warmedDonors += donors.Length;
            for (int i = 0; i < donors.Length; i++) Check(ReferenceEquals(lists[i], Get(donors[i], "m_AllElements")) && ReferenceEquals(arrays[i], donors[i].ElementsArray) && members[i].SequenceEqual(donors[i].ElementsArray) && donors[i].ElementsArray.Select((e, n) => ReferenceEquals(e.Owner, owners[i][n])).All(x => x) && definitions[i] == Definition(donors[i].ComponentsArray), "real native warmed donor list/elements/owners/components preserved " + donors[i].name);
            foreach (var token in Tokens) { var bp = Asset<BlueprintScriptableObject>(c, token); Check(lists.All(list => !ReferenceEquals(list, Get(bp, "m_AllElements"))) && bp.ElementsArray.All(e => ReferenceEquals(e.Owner, bp) && donors.All(d => !d.ElementsArray.Contains(e))), "private element list and ownership never alias any real warm donor " + token); }
            Stable(c);
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, previous); }
    }
    static void CacheFault(BlueprintGuid __0) { if (cacheFault && __0 == Id(Root)) { cacheFault = false; cacheFaults++; throw new InvalidOperationException("043 Sunsinger post-cache-write fault"); } }
    static void AppendFault() { if (appendFault) { appendFault = false; appendFaults++; throw new InvalidOperationException("043 Sunsinger post-append fault"); } }
    static void Registration(BlueprintsCache parent, Harmony h)
    {
        var original = ResourcesLibrary.BlueprintsCache;
        try {
            var c = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c); var cls = Native<BlueprintCharacterClass>(c, Skald); var progression = cls.Progression;
            var rows = progression.LevelEntries; var features = rows.Select(e => Get(e, "m_Features")).ToArray(); var archetypes = cls.Archetypes.ToArray();
            Register(c); Register(c); var root = Asset<BlueprintArchetype>(c, Root);
            Check(Tokens.Length == 13 && Tokens.All(t => c.Load(Id(t)) != null) && cls.Archetypes.Count(a => a == root) == 1, "one complete archetype and thirteen stable private nodes");
            Check(ReferenceEquals(rows, progression.LevelEntries) && rows.Select((e, i) => ReferenceEquals(Get(e, "m_Features"), features[i])).All(x => x) && cls.Archetypes.Where(a => a != root).SequenceEqual(archetypes), "all real native progression rows and previous archetypes retained");
            Check(root.GetParentClass() == cls && root.AddFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 5, 6 }) && root.RemoveFeatures.Select(e => e.Level).SequenceEqual(new[] { 5, 11, 17 }) && !root.RemoveSpellbook && !root.ChangeCasterType, "exact native Kenning replacements leave unrelated Wrath powers and casting unchanged");
            foreach (int level in Enumerable.Range(1, 20)) { var added = root.GetAddEntry(level); var removed = root.GetRemoveEntry(level); if (added != null) { var read = added.Features.ToArray(); Check(read.Length == (new[] { 1, 5, 6 }.Contains(level) ? 1 : 0), "real native GetAddEntry and feature proxy at level " + level); } Check((removed?.Features.Count() ?? 0) == (new[] { 5, 11, 17 }.Contains(level) ? 1 : 0), "real native GetRemoveEntry contains only exact Kenning replacements at " + level); }
            Register(c); Check(true, "normal native progression reads do not change authored identity");
            var authoredLevel = root.AddFeatures[0].Level; root.AddFeatures[0].Level = 2; try { Check(Reject(() => Register(c)), "authored progression level tampering is rejected"); } finally { root.AddFeatures[0].Level = authoredLevel; }
            var authoredFacts = Get(root.AddFeatures[0], "m_Features"); Set(root.AddFeatures[0], "m_Features", new List<BlueprintFeatureBaseReference>()); try { Check(Reject(() => Register(c)), "authored progression feature tampering is rejected"); } finally { Set(root.AddFeatures[0], "m_Features", authoredFacts); }
            foreach (var token in Tokens) {
                var foreign = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, foreign); var b = (BlueprintScriptableObject)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(c.Load(Id(token)), null); foreign.AddCachedBlueprint(b.AssetGuid, b);
                Check(Reject(() => Register(foreign)) && ReferenceEquals(foreign.Load(b.AssetGuid), b) && Tokens.Where(t => t != token).All(t => foreign.Load(Id(t)) == null), "foreign identity preserved without partial registration " + token);
            }
            foreach (bool atCache in new[] { true, false }) {
                var failed = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, failed); var target = Native<BlueprintCharacterClass>(failed, Skald); var before = Get(target, "m_Archetypes"); var ui = target.Progression.UIGroups;
                var pools = ((string[])AccessTools.Field(Module, "PoolIds").GetValue(null)).Select(id => Native<BlueprintFeatureSelection>(failed, id)).ToArray(); var poolBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
                var method = atCache ? AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint") : AccessTools.Method(Module, "Append"); var patch = AccessTools.Method(typeof(Sunsinger01443Tests), atCache ? nameof(CacheFault) : nameof(AppendFault)); h.Patch(method, postfix: new HarmonyMethod(patch)); if (atCache) cacheFault = true; else appendFault = true;
                try { Check(Reject(() => Register(failed)) && Tokens.All(t => failed.Load(Id(t)) == null) && ReferenceEquals(Get(target, "m_Archetypes"), before) && ReferenceEquals(target.Progression.UIGroups, ui) && pools.Select((p, i) => ReferenceEquals(Get(p, "m_Features"), poolBefore[i][0]) && ReferenceEquals(Get(p, "m_AllFeatures"), poolBefore[i][1])).All(x => x), "real cache-write/parent/pool append rollback is complete"); }
                finally { cacheFault = appendFault = false; h.Unpatch(method, patch); }
                Register(failed); Check(target.Archetypes.Count(a => a != null && a.AssetGuid == Id(Root)) == 1, "failed transaction retries exactly once");
            }
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, original); }
    }
    static UnitEntityData Unit(BlueprintsCache c, int level = 6)
    {
        var u = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c, "043-sunsinger-" + ++serial, Skald, level); Units.Add(u); return u;
    }
    static Feature Fact(UnitEntityData u, BlueprintFeature bp) { var f = new Feature(bp, u.Descriptor); u.Facts.Add(f); Check(f.IsTurnedOn, "native fact activates " + bp.name); return f; }
    static void Selection(BlueprintsCache c)
    {
        var root = Asset<BlueprintArchetype>(c, Root); var extra = Asset<BlueprintFeature>(c, Extra); var pool = Native<BlueprintFeatureSelection>(c, "247a4068296e8be42890143f451b4b45");
        foreach (var alignment in new[] { Alignment.LawfulGood, Alignment.NeutralGood, Alignment.ChaoticGood, Alignment.TrueNeutral, Alignment.LawfulNeutral, Alignment.ChaoticNeutral, Alignment.LawfulEvil, Alignment.NeutralEvil, Alignment.ChaoticEvil }) {
            var u = Unit(c, 1); u.Descriptor.Alignment.Set(alignment); bool restricted;
            Check(root.MeetsPrerequisites(u.Descriptor, null, out restricted) == (alignment == Alignment.LawfulGood || alignment == Alignment.NeutralGood || alignment == Alignment.ChaoticGood || alignment == Alignment.TrueNeutral), "actual native archetype prerequisite caller gates alignment " + alignment);
        }
        var candidate = Unit(c, 5); Check(!(bool)Call(typeof(EnhancementLifecycle01430Tests), "CanSelect", candidate, pool, extra), "native ordinary feat selection rejects absent solar channel"); var feature = Fact(candidate, Asset<BlueprintFeature>(c, Channel)); Check((bool)Call(typeof(EnhancementLifecycle01430Tests), "CanSelect", candidate, pool, extra), "native ordinary feat selection permits solar channel owner"); candidate.Facts.Remove(feature);
        var noGod = Unit(c, 1); noGod.Descriptor.Alignment.Set(Alignment.NeutralGood); var atheist = Fact(noGod, Native<BlueprintFeature>(c, "92c0d2da0a836ce418a267093c09ca54")); bool deityRestricted;
        Check(!root.MeetsPrerequisites(noGod.Descriptor, null, out deityRestricted), "real native prerequisite caller rejects an existing different religion"); noGod.Facts.Remove(atheist); Check(root.MeetsPrerequisites(noGod.Descriptor, null, out deityRestricted), "removing conflicting religion restores legitimate choice");
        var grant = Fact(noGod, Asset<BlueprintFeature>(c, "SunsingerSarenrae01443")); Check(noGod.Descriptor.HasFact(Native<BlueprintFeature>(c, Sarenrae)), "real private native conditional grant supplies the actual Sarenrae identity"); noGod.Facts.Remove(grant); Check(!noGod.Descriptor.HasFact(Native<BlueprintFeature>(c, Sarenrae)), "removing only native grant source revokes its own religion fact");
        var already = Unit(c, 1); already.Descriptor.Alignment.Set(Alignment.NeutralGood); var priorGod = Fact(already, Native<BlueprintFeature>(c, Sarenrae)); Check(root.MeetsPrerequisites(already.Descriptor, null, out deityRestricted), "existing Sarenrae worshipper remains eligible"); var additionalGrant = Fact(already, Asset<BlueprintFeature>(c, "SunsingerSarenrae01443")); already.Facts.Remove(additionalGrant); Check(already.Facts.List.Contains(priorGod) && already.Descriptor.HasFact(Native<BlueprintFeature>(c, Sarenrae)), "native grant removal preserves an independently existing Sarenrae source");
    }
    static void TttReplacement(BlueprintsCache parent)
    {
        var previous = ResourcesLibrary.BlueprintsCache;
        try {
            foreach (int order in new[] { 0, 1, 2 }) {
                var c = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c); var cls = Native<BlueprintCharacterClass>(c, Skald); var p = cls.Progression;
                void AddActualTttRows() { foreach (var e in p.LevelEntries.Where(e => new[] { 5, 11, 17 }.Contains(e.Level))) { var raw = (List<BlueprintFeatureBaseReference>)Get(e, "m_Features"); raw.Add(Ref<BlueprintFeatureBaseReference>(Native<BlueprintFeature>(c, e.Level == 5 ? "d385b8c302e720c43aa17b8170bc6ae2" : "590d0f09d7da13d4a9382d144b8439f6"))); } }
                if (order == 1) AddActualTttRows(); Register(c); if (order == 2) AddActualTttRows(); Register(c);
                var u = Unit(c, 20); var root = Asset<BlueprintArchetype>(c, Root); Set(u.Progression, "m_Progressions", new Dictionary<BlueprintProgression, ProgressionData>()); var pd = u.Progression.SureProgressionData(p); pd.AddArchetype(root);
                foreach (int level in new[] { 5, 11, 17 }) {
                    var parentFacts = p.LevelEntries.Single(e => e.Level == level).Features.ToArray(); var effective = pd.GetLevelEntry(level).Features.ToArray(); var removedId = level == 5 ? "d385b8c302e720c43aa17b8170bc6ae2" : "590d0f09d7da13d4a9382d144b8439f6";
                    Check(parentFacts.Any(x => x?.AssetGuid.ToString() == removedId) == (order != 0), "real parent retains exact TTT/native shape at " + order + "/" + level);
                    Check(!effective.Any(x => x?.AssetGuid.ToString() == removedId) && parentFacts.Where(x => x != null && x.AssetGuid.ToString() != removedId).All(effective.Contains), "native ProgressionData effective entries remove only Kenning before/after provider append at " + order + "/" + level);
                }
                Stable(c);
            }
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, previous); }
    }
    static MethodInfo LinkMethod(Type runtime, string name)
    {
        // The closed link type is unresolvable in this process: a second
        // UnityEngine.CoreModule identity makes the GameObject constraint
        // fail, so the runtime type keeps ContainsGenericParameters and
        // Harmony refuses its methods. Re-close each base with the type
        // arguments that base already carries, which are the game's own
        // identities, and only then reflect the declared method.
        var parameters = name == "Load" ? new[] { typeof(bool), typeof(bool) } : Type.EmptyTypes;
        for (var type = runtime; type != null; type = type.BaseType)
        {
            var target = type;
            if (target.ContainsGenericParameters && target.IsGenericType)
            {
                var arguments = target.GetGenericArguments();
                if (arguments.All(a => !a.ContainsGenericParameters))
                    target = target.GetGenericTypeDefinition().MakeGenericType(arguments);
            }
            var found = target.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, parameters, null);
            if (found != null && !found.ContainsGenericParameters) return found;
        }
        throw new MissingMethodException(runtime.FullName + "." + name);
    }
    static void LinkWarmup(BlueprintsCache c, Harmony h)
    {
        var area = Asset<BlueprintAbilityAreaEffect>(c, Area); var link = area.Fx; string id = link.AssetId;
        var load = LinkMethod(link.GetType(), "Load"); var unload = LinkMethod(link.GetType(), "ForceUnload");
        h.Patch(load, transpiler: new HarmonyMethod(typeof(Sunsinger01443Tests), nameof(LoadBoundary))); h.Patch(unload, transpiler: new HarmonyMethod(typeof(Sunsinger01443Tests), nameof(UnloadBoundary)));
        try {
            Check(Get(link, "<m_Handle>k__BackingField") == null, "native resource handle starts cold"); link.Load(); resourceLinkLoads++; Check(Get(link, "<m_Handle>k__BackingField") != null, "real native WeakResourceLink.Load writes a handle with resource allocator/object endpoints controlled"); Stable(c);
            link.ForceUnload(); Check(Get(link, "<m_Handle>k__BackingField") == null, "real native ForceUnload clears its handle while asset identity stays intact"); Stable(c);
            link.AssetId = "04300000000000000000000000000001"; try { Check(Reject(() => Register(c)), "different authored effect AssetId remains rejected after handle warmup"); } finally { link.AssetId = id; } Stable(c);
        } finally { h.Unpatch(load, AccessTools.Method(typeof(Sunsinger01443Tests), nameof(LoadBoundary))); h.Unpatch(unload, AccessTools.Method(typeof(Sunsinger01443Tests), nameof(UnloadBoundary))); }
    }
    static void NativeJson(BlueprintsCache c)
    {
        foreach (string token in new[] { Heal, Harm, Toggle }) {
            var bp = Asset<BlueprintScriptableObject>(c, token); var component = token == Toggle ? (BlueprintComponent)bp.GetComponent<ActivatableAbilityResourceLogic>() : bp.GetComponent<ContextRankConfig>(); var previous = Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
            try {
                Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead = new BlueprintJsonWrapper(bp); var writer = new StringWriter(); var json = new JsonTextWriter(writer); Kingmaker.Blueprints.JsonSystem.Json.Serializer.Serialize(json, component); json.Flush(); var copy = (BlueprintComponent)Kingmaker.Blueprints.JsonSystem.Json.Serializer.Deserialize(new JsonTextReader(new StringReader(writer.ToString())), component.GetType()); copy.OwnerBlueprint = bp; bp.ComponentsArray = bp.ComponentsArray.Select(x => ReferenceEquals(x, component) ? copy : x).ToArray(); bp.OnEnable(); jsonRoundTrips++; Stable(c);
                if (copy is ContextRankConfig) { var empty = Get(copy, "m_Feature"); Set(copy, "m_Feature", Ref<BlueprintFeatureReference>(Native<BlueprintFeature>(c, "d385b8c302e720c43aa17b8170bc6ae2"))); try { Check(Reject(() => Register(c)), "nonempty authored GUID substitution remains rejected after native JSON roundtrip"); } finally { Set(copy, "m_Feature", empty); } Stable(c); }
            } finally { Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead = previous; }
        }
        var host = Asset<BlueprintBuff>(c, Host); var owned = host.ComponentsArray[0]; var owner = owned.OwnerBlueprint; owned.OwnerBlueprint = (BlueprintScriptableObject)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(owner, null); try { Check(Reject(() => Register(c)), "same-GUID foreign component owner is rejected"); } finally { owned.OwnerBlueprint = owner; } Stable(c);
    }
    static void Resources(BlueprintsCache c)
    {
        var feature = Asset<BlueprintFeature>(c, Channel); var extra = Asset<BlueprintFeature>(c, Extra); var resource = Asset<BlueprintAbilityResource>(c, Resource); var heal = Asset<BlueprintAbility>(c, Heal); var harm = Asset<BlueprintAbility>(c, Harm);
        foreach (var v in new[] { new[] { 5, 1 }, new[] { 10, 1 }, new[] { 11, 2 }, new[] { 16, 2 }, new[] { 17, 3 }, new[] { 20, 3 } }) {
            var u = Unit(c, v[0]); var data = new AbilityData(heal, u.Descriptor); Check(!heal.GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(data), "no free channel without its feature"); var fact = Fact(u, feature);
            Check(resource.GetMaxAmount(u.Descriptor) == v[1] && u.Resources.GetResourceAmount(resource) == v[1], "native Skald channel maximum " + v[0]);
            for (int n = v[1]; n > 0; n--) { var b = n % 2 == 0 ? heal : harm; var a = new AbilityData(b, u.Descriptor); Check(b.GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(a), "native mode shares available daily pool"); AbilityCastRateUtils.SpendResources(a); spends++; Check(u.Resources.GetResourceAmount(resource) == n - 1, "real native cast caller pays one shared use"); }
            Check(!heal.GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(data) && !harm.GetComponent<AbilityResourceLogic>().IsAbilityRestrictionPassed(new AbilityData(harm, u.Descriptor)), "exhaustion blocks both modes"); u.Resources.Restore(resource); Check(u.Resources.GetResourceAmount(resource) == v[1], "rest restores same real maximum");
            var extraFact = Fact(u, extra); u.Resources.Restore(resource); Check(resource.GetMaxAmount(u.Descriptor) == v[1] + 2, "real native Extra Channel adds exactly two uses"); extraFact.AddRank(); nativeRankChanges++; Check(extraFact.Rank == 2 && resource.GetMaxAmount(u.Descriptor) == v[1] + 4, "native repeatable Extra Channel rank adds exactly two more uses"); extraFact.RemoveRank(); nativeRankChanges++; Check(resource.GetMaxAmount(u.Descriptor) == v[1] + 2, "native rank removal balances only one Extra Channel choice"); u.Facts.Remove(extraFact); Check(resource.GetMaxAmount(u.Descriptor) == v[1], "removing extra feat restores base maximum");
            u.Facts.Remove(fact); Check(!u.Abilities.Enumerable.Any(a => a.Blueprint == heal || a.Blueprint == harm), "removing feature revokes both native abilities");
        }
        var mixed = Unit(c, 5); mixed.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c, "67819271767a9dd4fbfd4ae700befea0")) { Level = 15 }); Fact(mixed, feature); Check(resource.GetMaxAmount(mixed.Descriptor) == 1, "Cleric levels do not increase Sunsinger daily uses");
        foreach (var ability in new[] { heal, harm }) { var context = new MechanicsContext(mixed, mixed.Descriptor, ability); Check(ability.GetComponents<ContextRankConfig>().Single(r => Get(r, "m_Type").Equals(AbilityRankType.Default)).GetValue(context) == 3, "native channel dice use only Skald5"); Check(ability.GetComponents<ContextRankConfig>().Single(r => Get(r, "m_Type").Equals(AbilityRankType.StatBonus)).GetValue(context) == 2, "faerie fire duration rank is floor half rather than channel dice"); }
        var restored = Unit(c, 11); var persisted = Fact(restored, feature); AbilityCastRateUtils.SpendResources(new AbilityData(heal, restored.Descriptor)); int paid = restored.Resources.GetResourceAmount(resource); restored.Facts.TurnOff(); restored.Facts.TurnOn(); restored.Facts.ApplyPostLoadFixes();
        Check(restored.Facts.List.Contains(persisted) && restored.Abilities.Enumerable.Count(a => a.Blueprint == heal) == 1 && restored.Abilities.Enumerable.Count(a => a.Blueprint == harm) == 1 && restored.Resources.GetResourceAmount(resource) == paid, "actual native fact-manager suspend/restore/post-load keeps identity and paid resource without duplication/refill");
    }
    static void Activation(BlueprintsCache c, Harmony h)
    {
        var u = Unit(c, 6); var pillar = Asset<BlueprintFeature>(c, "SunsingerPillarOfLight01443"); var resource = Native<BlueprintAbilityResource>(c, "4a2302c4ec2cfb042bba67d825babfec"); var root = Fact(u, pillar); var ability = u.ActivatableAbilities.RawFacts.Single(a => a.Blueprint.AssetGuid == Id(Toggle));
        Check(!ability.IsAvailableByResources, "owning sunlight performance does not manufacture Raging Song rounds"); Fact(u, Native<BlueprintFeature>(c, "334cb75aa673d1d4bb279761c2ef5cf1")); u.Resources.Restore(resource); int amount = u.Resources.GetResourceAmount(resource);
        h.Patch(AccessTools.Method(typeof(UnitActivatableAbilitiesController), "GetTimeToNextRound"), prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "NextRound")); h.Patch(AccessTools.Method(typeof(UnitActivatableAbilitiesController), "CanTickNewRound"), prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "TickAllowed"));
        Call(typeof(BardMasterpieces01435Tests), "Start", u, ability); Check(ability.IsStarted && ability.AppliedBuff.Blueprint.AssetGuid == Id(Host) && u.Resources.GetResourceAmount(resource) == amount - 1, "native performance start plus first controller tick grant host and spend one Skald round"); Call(typeof(BardMasterpieces01435Tests), "Tick", u, ability); Check(u.Resources.GetResourceAmount(resource) == amount - 2, "native following controller round spends exactly one");
        var host = ability.AppliedBuff; ability.IsOn = false; Call(typeof(BardMasterpieces01435Tests), "Tick", u, ability); Check(!ability.IsStarted && (host.IsDisposed || host.TimeLeft <= TimeSpan.FromSeconds(6)), "native stop retains its original final-round duration behavior");
        u.Resources.Spend(resource, u.Resources.GetResourceAmount(resource)); Check(!ability.IsAvailableByResources, "zero real Raging Song rounds blocks a fresh performance"); u.Facts.Remove(root); Check(!u.ActivatableAbilities.RawFacts.Contains(ability), "removing own pillar feature revokes native toggle");
    }
    static void Outline(BlueprintsCache c)
    {
        foreach (int level in new[] { 5, 6, 11, 17, 20 }) foreach (bool healing in new[] { true, false }) foreach (bool enemy in new[] { true, false }) {
            var caster = Unit(c, level); var target = Unit(c); Fact(caster, Asset<BlueprintFeature>(c, Channel)); var bp = Asset<BlueprintAbility>(c, healing ? Heal : Harm); var ability = new AbilityData(bp, caster.Descriptor);
            AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").SetValue(null, enemy ? target : null);
            var context = new AbilityExecutionContext(ability, new AbilityParams(), new TargetWrapper(caster), null, caster); AbilityExecutionProcess.PrepareCast(context); castPrepares++;
            var outline = bp.GetComponent<AbilityEffectRunAction>().Actions.Actions[1]; using (context.GetDataScope(target)) outline.RunAction(); outlines++;
            var applied = target.Buffs.Enumerable.Where(b => b.Blueprint.AssetGuid.ToString() == "cc383a9eaae4d2b45a925d442b367b54").ToArray(); Check(applied.Length == (enemy ? 1 : 0), "native enemy relationship controls faerie fire in either mode"); if (enemy) Check(applied[0].TimeLeft == TimeSpan.FromSeconds((level / 2) * 6), "real buff end time uses half-level rounds at " + level);
        }
        AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").SetValue(null, null);
    }
    static void ChannelEffects(BlueprintsCache c)
    {
        foreach (bool healing in new[] { true, false }) foreach (bool undead in new[] { true, false }) foreach (bool enemy in new[] { true, false }) foreach (bool selective in new[] { true, false }) foreach (bool passed in new[] { true, false }) {
            var caster = Unit(c, 5); var target = Unit(c); Fact(caster, Asset<BlueprintFeature>(c, Channel));
            if (undead) Fact(target, Native<BlueprintFeature>(c, "d5ee498e19722854198439629c1841a5")); if (selective) Fact(caster, Native<BlueprintFeature>(c, "fd30c69417b434d47b6b03b9c1f568ff"));
            AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").SetValue(null, enemy ? target : null); AccessTools.Field(typeof(BardMasterpieces01435Tests), "roll").SetValue(null, passed ? 20 : 1);
            var bp = Asset<BlueprintAbility>(c, healing ? Heal : Harm); var ability = new AbilityData(bp, caster.Descriptor); var context = new AbilityExecutionContext(ability, new AbilityParams(), new TargetWrapper(caster), null, caster); AbilityExecutionProcess.PrepareCast(context); castPrepares++;
            int healBefore = healRules, damageBefore = damageRules;
            using (context.GetDataScope(target)) bp.GetComponent<AbilityEffectRunAction>().Apply(context, new TargetWrapper(target));
            bool heals = healing && !undead && (!selective || !enemy), harms = !healing && undead && (!selective || enemy);
            Check(healRules - healBefore == (heals ? 1 : 0) && damageRules - damageBefore == (harms ? 1 : 0), "full native channel action branch preserves living/undead/faction/Selective Channel " + healing + "/" + undead + "/" + enemy + "/" + selective);
            if (heals) Check(lastHeal.ValueWithoutReduction == 12 && lastHeal.IsFake, "native healing rule consumes shared 3d6 roll without asserting rendered HP lifecycle");
            if (harms) Check(lastDamage.HalfBecauseSavingThrow == passed && lastDamage.DamageBundle.Single().Dice.ModifiedValue.Rolls == 3 && lastDamage.DamageBundle.Single() is EnergyDamage energy && energy.EnergyType == Kingmaker.Enums.Damage.DamageEnergyType.PositiveEnergy, "native damage action submits actual 3d6 positive-energy rule with save-half flag");
            Check(target.Buffs.Enumerable.Any(b => b.Blueprint.AssetGuid.ToString() == "cc383a9eaae4d2b45a925d442b367b54") == enemy, "outline independently reaches every enemy, including nonchannel targets and Selective Channel exclusions");
        }
        AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").SetValue(null, null);
    }
    static void Pillar(BlueprintsCache c)
    {
        var caster = Unit(c, 6); var victim = Unit(c); var friend = Unit(c); var hostBp = Asset<BlueprintBuff>(c, Host); var areaBp = Asset<BlueprintAbilityAreaEffect>(c, Area); var effect = Asset<BlueprintBuff>(c, Effect); var immune = Asset<BlueprintBuff>(c, Immune);
        var context = new MechanicsContext(caster, caster.Descriptor, hostBp); var host = caster.Facts.Add(new Buff(hostBp, context, null));
        var areaContext = new MechanicsContext(caster, caster.Descriptor, areaBp, host.Context); areaContext.Recalculate();
        var area = (AreaEffectEntityData)Call(typeof(BardMasterpieces01435Tests), "Area", "043-sunsinger-area", areaContext, areaBp, new[] { victim, friend }); var action = areaBp.GetComponent<AbilityAreaEffectRunAction>();
        AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").SetValue(null, victim); AccessTools.Field(typeof(BardMasterpieces01435Tests), "roll").SetValue(null, 1);
        areaBp.HandleUnitEnter(area.Context, area, friend); Check(!friend.Buffs.Enumerable.Any(b => b.Blueprint == effect), "actual native area dispatch excludes ally");
        var specific = new SpecificBuffImmunity(); Set(specific, "m_Buff", Ref<BlueprintBuffReference>(Native<BlueprintBuff>(c, "2d4bd347dec7d8648afd502ee40ae661"))); var guardBp = new BlueprintBuff { name = "043-native-fascinate-immunity-test", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), ComponentsArray = new BlueprintComponent[] { specific } }; guardBp.OnEnable(); var guard = victim.Facts.Add(new Buff(guardBp, context, null)); Check(guard.IsTurnedOn, "native immunity guard buff activates");
        areaBp.HandleUnitEnter(area.Context, area, victim); specificImmunityBranches++; Check(!victim.Buffs.Enumerable.Any(b => b.Blueprint == effect), "attached original-GUID native Fascinate immunity also rejects private Pillar effect after failed save");
        guard.TurnOff(); areaBp.HandleUnitEnter(area.Context, area, victim); specificImmunityBranches++; Check(victim.Buffs.Enumerable.Any(b => b.Blueprint == effect), "turned-off native specific immunity does not block Pillar"); areaBp.HandleUnitExit(area.Context, area, victim); guard.TurnOn();
        guard.IsSuppressed = true; areaBp.HandleUnitEnter(area.Context, area, victim); specificImmunityBranches++; Check(victim.Buffs.Enumerable.Any(b => b.Blueprint == effect), "suppressed original-GUID immunity is not incorrectly treated as active"); areaBp.HandleUnitExit(area.Context, area, victim); guard.IsSuppressed = false;
        Set(specific, "m_Buff", Ref<BlueprintBuffReference>(Native<BlueprintBuff>(c, "cc383a9eaae4d2b45a925d442b367b54"))); areaBp.HandleUnitEnter(area.Context, area, victim); specificImmunityBranches++; Check(victim.Buffs.Enumerable.Any(b => b.Blueprint == effect), "unrelated native effect immunity does not overblock Pillar"); areaBp.HandleUnitExit(area.Context, area, victim); victim.Facts.Remove(guard);
        areaBp.HandleUnitEnter(area.Context, area, victim); saves++; Check(victim.Buffs.Enumerable.Any(b => b.Blueprint == effect) && victim.State.HasCondition(UnitCondition.Dazed), "actual area dispatch and failed native Will save apply native fascinating condition");
        var lastSave = (RuleSavingThrow)AccessTools.Field(typeof(BardMasterpieces01435Tests), "lastSave").GetValue(null); Check(lastSave.DifficultyClassBase == 17, "actual area DC is 10+half Skald6+Cha18");
        areaBp.HandleUnitExit(area.Context, area, victim); Check(!victim.Buffs.Enumerable.Any(b => b.Blueprint == effect), "actual area exit removes fascinating effect");
        areaBp.HandleUnitEnter(area.Context, area, victim); saves++; var damaged = victim.Buffs.Enumerable.Single(b => b.Blueprint == effect); var damage = new RuleDealDamage(caster, victim, new DirectDamage(new DiceFormula(0, DiceType.Zero), 10)); Set(damage, "Result", 10);
        foreach (var runtime in damaged.Components.Where(r => r.SourceBlueprintComponent is AddIncomingDamageTrigger)) using (runtime.RequestEventContext()) ((AddIncomingDamageTrigger)runtime.SourceBlueprintComponent).OnEventDidTrigger(damage);
        Check(!victim.Buffs.Enumerable.Any(b => b.Blueprint == effect) && victim.Buffs.Enumerable.Any(b => b.Blueprint == immune), "attached real native incoming-damage handler breaks fascination and grants cooldown"); foreach (var b in victim.Buffs.Enumerable.Where(b => b.Blueprint == immune).ToArray()) victim.Facts.Remove(b);
        AccessTools.Field(typeof(BardMasterpieces01435Tests), "roll").SetValue(null, 20); areaBp.HandleUnitEnter(area.Context, area, victim); saves++; Check(!victim.Buffs.Enumerable.Any(b => b.Blueprint == effect) && victim.Buffs.Enumerable.Any(b => b.Blueprint == immune && b.TimeLeft == TimeSpan.FromDays(1)), "successful native save grants private 24-hour immunity");
        var before = AccessTools.Field(typeof(BardMasterpieces01435Tests), "saves").GetValue(null); AccessTools.Field(typeof(BardMasterpieces01435Tests), "roll").SetValue(null, 1); areaBp.HandleUnitEnter(area.Context, area, victim); Check(Equals(before, AccessTools.Field(typeof(BardMasterpieces01435Tests), "saves").GetValue(null)), "immunity blocks a new native save");
        Check(Asset<BlueprintActivatableAbility>(c, Toggle).Group == ActivatableAbilityGroup.BardicPerformance && Asset<BlueprintActivatableAbility>(c, Toggle).GetComponent<ActivatableAbilityResourceLogic>().RequiredResource.AssetGuid.ToString() == "4a2302c4ec2cfb042bba67d825babfec", "native performance group and Skald resource retained");
        caster.Facts.Remove(host); Check(!caster.Buffs.Enumerable.Contains(host), "native host removes without private lifecycle code");
        AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").SetValue(null, null);
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = cacheFaults = appendFaults = spends = saves = castPrepares = outlines = healRules = damageRules = warmedDonors = jsonRoundTrips = resourceLinkLoads = sacredCatalogueEndpoints = specificImmunityBranches = nativeRankChanges = 0; Units.Clear(); var h = new Harmony("CotW043.SunsingerNativeContracts"); var game = Game.Instance; var beforeState = game.State; var beforeTime = game.TimeController;
        var originalCache = ResourcesLibrary.BlueprintsCache;
        var helperUnits = (IList)AccessTools.Field(typeof(BardMasterpieces01435Tests), "units").GetValue(null); int helperCount = helperUnits.Count; var helperCurrent = AccessTools.Field(typeof(BardMasterpieces01435Tests), "current"); var beforeContext = helperCurrent.GetValue(null); helperCurrent.SetValue(null, null);
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        var endpoints = new[] { new { Method = (MethodBase)AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) }), Prefix = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense") }, new { Method = (MethodBase)AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), Prefix = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice") }, new { Method = (MethodBase)AccessTools.Method(typeof(RuleHealDamage), "OnTrigger"), Prefix = AccessTools.Method(typeof(Psychic070Tests), "CaptureHealing") } };
        var suspended = endpoints.SelectMany(e => (Harmony.GetPatchInfo(e.Method)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<Patch>()).Where(p => p.PatchMethod == e.Prefix).Select(p => new { Original = e.Method, Record = p })).ToArray();
        void Scene(MethodBase m, string n) => h.Patch(m, prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), n));
        try {
            foreach (var p in suspended) h.Unpatch(p.Original, p.Record.PatchMethod);
            Scene(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), "Skip"); Scene(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), "Controller"); Scene(AccessTools.Method(typeof(EntityFact), "Attach"), "Identity"); Scene(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), "Skip"); Scene(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), "Skip");
            h.Patch(AccessTools.Method(typeof(AddFacts), "UpdateFacts"), prefix: new HarmonyMethod(typeof(Sunsinger01443Tests), nameof(SacredCatalogue)));
            Scene(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), "False"); Scene(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), "Ignore");
            h.Patch(AccessTools.Method(typeof(UnitEntityData), "IsEnemy", new[] { typeof(UnitEntityData) }), prefix: new HarmonyMethod(typeof(Sunsinger01443Tests), nameof(Enemy)));
            Scene(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(TimeController), "RealTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(Game), "CurrentMode"), "Mode"); Scene(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), "False"); Scene(AccessTools.Method(typeof(UnitEntityData), "IsAlly"), "Ally"); Scene(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), "False"); Scene(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), "False"); Scene(AccessTools.Method(typeof(AddAreaEffect), "IsOwnerAvatarValid"), "False"); Scene(AccessTools.Method(typeof(ContextActionSpawnFx), "RunAction"), "Fx");
            Scene(AccessTools.Method(typeof(UnitCommands), "Run", new[] { typeof(UnitCommand) }), "Submit"); Scene(AccessTools.Method(typeof(UnitEntityData), "IsCurrentUnit"), "CurrentUnit"); Scene(AccessTools.Method(typeof(Player), "IsTurnBasedModeOn"), "False");
            h.Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "Context"));
            Scene(AccessTools.Method(typeof(Kingmaker.QA.LogChannelEx), "ExceptionWithReport", new[] { typeof(Owlcat.Runtime.Core.Logging.LogChannel), typeof(Exception), typeof(string), typeof(object[]) }), "Expose");
            h.Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), prefix: new HarmonyMethod(typeof(Sunsinger01443Tests), nameof(Dice))); Scene(AccessTools.PropertyGetter(typeof(RulebookEvent.Dice), "D20"), "D20");
            h.Patch(AccessTools.Method(typeof(ContextActionHealTarget), "RunAction"), transpiler: new HarmonyMethod(typeof(Sunsinger01443Tests), nameof(EffectBoundary))); h.Patch(AccessTools.Method(typeof(ContextActionDealDamage), "DealHitPointsDamage"), transpiler: new HarmonyMethod(typeof(Sunsinger01443Tests), nameof(EffectBoundary)));
            foreach (var method in new[] { AccessTools.Method(typeof(ContextActionSavingThrow), "RunAction"), AccessTools.Method(typeof(RuleSavingThrow), "OnTrigger"), AccessTools.Method(typeof(RuleSavingThrow), "RollD20"), AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), AccessTools.Method(typeof(AbilityExecutionProcess), "PrepareCast"), AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(AbilityData) }) }) h.Patch(method, transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "RuleBoundary"));
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "DifficultyBoundary")); h.Patch(AccessTools.Method(typeof(RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
            Provider01419Tests.PatchLabels(h); Registration(cache, h); WarmDonorClones(cache); TttReplacement(cache); cache = Fresh(cache); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, cache); Register(cache); Selection(cache); Resources(cache); Activation(cache, h); Outline(cache); ChannelEffects(cache); Pillar(cache); LinkWarmup(cache, h); NativeJson(cache); Stable(cache);
            PsychicExpansionTests.Inventory["sunsinger01443Contracts"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0034"), ["archetypeCount"] = 1,
                ["newSpellRootCount"] = 0, ["privateResourceCount"] = 13, ["newHarmonyPatchCount"] = 0, ["newSerializedComponentTypeCount"] = 0,
                ["resourceSpends"] = spends, ["castPrepares"] = castPrepares, ["outlinePaths"] = outlines, ["nativeAreaSaveEntries"] = saves,
                ["nativeChannelHealingRuleCalls"] = healRules, ["nativeChannelDamageDispatches"] = damageRules, ["nativeRepeatableRankChanges"] = nativeRankChanges,
                ["nativeOriginalGuidImmunityBranches"] = specificImmunityBranches, ["warmNativeDonorsPreserved"] = warmedDonors, ["nativeJsonRoundTrips"] = jsonRoundTrips,
                ["nativeResourceLinkLoads"] = resourceLinkLoads, ["sarenraeCatalogueEndpoints"] = sacredCatalogueEndpoints,
                ["postCacheWriteFaults"] = cacheFaults, ["postAppendFaults"] = appendFaults, ["baseProgressionPreserved"] = true,
                ["nativeProgressionCallerOrders"] = new JArray("vanilla", "TTT source grants before Sunsinger", "TTT source grants after Sunsinger"),
                ["exactKenningReplacements"] = new JArray("5:d385b8c302e720c43aa17b8170bc6ae2", "11:590d0f09d7da13d4a9382d144b8439f6", "17:590d0f09d7da13d4a9382d144b8439f6"),
                ["wrathAbsentWithoutProvider"] = new JArray("Song of Marching", "Spell Kenning and extra uses"),
                ["newBlueprints"] = new JArray(Tokens.Select(t => { var b = cache.Load(Id(t)); return new JObject { ["token"] = t, ["id"] = b.AssetGuid.ToString(), ["type"] = b.GetType().FullName }; })),
                ["boundary"] = "Actual game archive records and managed native prerequisite/selection, AddFeatureIfHasFact religion grant/removal, AddFacts/resources, real cast-cost caller, PrepareCast ranks/DC, original channel conditions/healing/save/damage construction, ContextActionApplyBuff duration and RuleCanApplyBuff, native area dispatcher/Will save/entry/exit/immunity and incoming-damage handler execute. Healing uses the real RuleHealDamage with IsFake, damage ends at final rule dispatch: rendered HP/combat is not asserted. Exact native TTT-shaped grants run through ProgressionData.GetLevelEntry before/after registration; full TTT initialization is outside this fixture. Native WeakResourceLink.Load/ForceUnload bodies execute with exact counted allocator/object/loaded-state/unload rendering calls controlled. Only Sarenrae source AddFacts $AddFacts$b10fd6bc-12f8-4488-b479-84ed0cf6977d with all 30 exact archive GUIDs is a controlled Sacred Weapon catalogue endpoint; its independent derived catalogue is not executed. Original wide-catalogue enum SawtoothSaber failure (attempt03) and missing-catalogue native AddFacts NRE (attempt19) are retained. Other Sarenrae components, actual fact identity and native grant lifecycle execute. Unit/world/UI/clock/difficulty/dice/rendering endpoints use the established managed scene. Full rendered commands, positional scan, complete save/load and player UI remain tester observation steps; this contract does not represent those observations."
            };
            Console.WriteLine("PASS: " + checks + " Sunsinger01443 native contracts."); return checks;
        } finally {
            AccessTools.Field(typeof(BardMasterpieces01435Tests), "hostile").SetValue(null, null); foreach (var u in Units.AsEnumerable().Reverse()) foreach (var fact in u.Facts.List.ToArray()) if (fact.Manager != null) u.Facts.Remove(fact); h.UnpatchAll(h.Id);
            foreach (var p in suspended) new Harmony(p.Record.owner).Patch(p.Original, prefix: new HarmonyMethod(p.Record.PatchMethod) { priority = p.Record.priority, before = p.Record.before, after = p.Record.after, debug = p.Record.debug });
            Set(game, "State", beforeState); Set(game, "TimeController", beforeTime); helperCurrent.SetValue(null, beforeContext); while (helperUnits.Count > helperCount) helperUnits.RemoveAt(helperUnits.Count - 1);
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, originalCache);
        }
    }
}
