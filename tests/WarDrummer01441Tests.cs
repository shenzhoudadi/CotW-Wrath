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
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

internal static class WarDrummer01441Tests
{
    static readonly Type Module = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.WarDrummer01441", true);
    static readonly string[] Tokens = (string[])AccessTools.Field(Module, "OwnTokens").GetValue(null);
    static int checks, serial, nativeDamageCalls, nativeCriticalCalls, nativeSongApplications, cacheFaults, appendFaults;
    static bool cacheFault, appendFault;
    static readonly List<UnitEntityData> Units = new List<UnitEntityData>();
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static object Call(Type t, string n, params object[] args) => AccessTools.Method(t, n).Invoke(null, args);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string token) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id", token);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static void Check(bool value, string what) { if (!value) throw new Exception("WarDrummer01441: " + what); checks++; }
    static void Register(BlueprintsCache c) => Call(Module, "Register", c);
    static bool Reject(Action a) { try { a(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static void Hydrate(BlueprintsCache c, bool reset = false)
    {
        var critical = Native<BlueprintParametrizedFeature>(c, "f4201c85a991369408740c6888362e20");
        var criticalData = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == critical.AssetGuid.ToString())["blueprint"]["Data"];
        critical.ParameterType = (FeatureParameterType)PsychicExpansionTests.Read(criticalData["ParameterType"], typeof(FeatureParameterType));
        foreach (var entry in new[] {
            new[] { "6afa347d804838b48bda16acb0573dc0", "m_Progression", "m_Archetypes" },
            new[] { "26418fed2bc153245972a5b54204ed75", "LevelEntries", "UIGroups", "m_UIDeterminatorsGroup" }
        }) {
            var bp = c.Load(BlueprintGuid.Parse(entry[0])); var data = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == entry[0])["blueprint"]["Data"];
            foreach (string name in entry.Skip(1)) {
                var field = AccessTools.Field(bp.GetType(), name); var current = field.GetValue(bp);
                if (reset || current == null || current is Array a && a.Length == 0 || current is BlueprintReferenceBase b && b.Guid == default(BlueprintGuid)) field.SetValue(bp, PsychicExpansionTests.Read(data[name], field.FieldType));
            }
        }
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var c = (BlueprintsCache)Call(typeof(TimeStutter01437Tests), "Fresh", parent);
        foreach (var token in Tokens) c.RemoveCachedBlueprint(Id(token));
        var before = ResourcesLibrary.BlueprintsCache;
        AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
        try {
            Hydrate(c, true);
            foreach (var id in new[] { "75b3978757908d24aaaecaf2dc209b89", "ee51f69e69cb43c3bc7c02002e482d17" }) {
                var bp = Native<BlueprintBuff>(c, id); var row = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == id);
                bp.ComponentsArray = ((JArray)row["blueprint"]["Data"]["Components"]).Select(x => (BlueprintComponent)PsychicExpansionTests.Read(x, typeof(BlueprintComponent))).ToArray(); bp.OnEnable();
            }
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, before); }
        return c;
    }
    static void CacheFault(BlueprintGuid __0) { if (cacheFault && __0 == Id(Tokens[0])) { cacheFault = false; cacheFaults++; throw new InvalidOperationException("041 post-cache-write fault"); } }
    static void AppendFault() { if (appendFault) { appendFault = false; appendFaults++; throw new InvalidOperationException("041 post-append fault"); } }
    static void Registration(BlueprintsCache parent, Harmony h)
    {
        var original = ResourcesLibrary.BlueprintsCache;
        try {
            var c = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
            var cls = Native<BlueprintCharacterClass>(c, "6afa347d804838b48bda16acb0573dc0"); var progression = cls.Progression;
            var rows = progression.LevelEntries; var arrays = rows.Select(e => Get(e, "m_Features")).ToArray(); var priorArchetypes = cls.Archetypes.ToArray();
            Register(c); var root = Asset<BlueprintArchetype>(c, Tokens[0]); Register(c);
            Check(Tokens.Length == 13 && Tokens.All(t => c.Load(Id(t)) != null) && cls.Archetypes.Count(a => a == root) == 1, "one archetype and exactly thirteen stable private nodes");
            Check(ReferenceEquals(rows, progression.LevelEntries) && rows.Select((e, i) => ReferenceEquals(Get(e, "m_Features"), arrays[i])).All(x => x) && cls.Archetypes.Where(a => a != root).SequenceEqual(priorArchetypes), "base progression rows and every previous archetype preserved");
            Check(root.GetParentClass() == cls && !root.RemoveSpellbook && !root.ChangeCasterType && !root.ReplaceClassSkills && root.AddSkillPoints == 0, "actual native Skald parent with unchanged casting, skills and BAB/saves");
            Check(root.AddFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 3, 6 }) && root.RemoveFeatures.Select(e => e.Level).SequenceEqual(new[] { 1, 7 }), "exact original CotW addition/removal levels");
            Check(root.RemoveFeatures[0].Features.Select(f => f.AssetGuid.ToString()).SequenceEqual(new[] { "b2cc2d1d1844d0443866164466613c5c", "64e3e3fb96c439b4f8d896339b2f358a" }) && root.RemoveFeatures[1].Features.Single().AssetGuid.ToString() == "d2a8fde8985691045b90e1ec57e3cc57", "only native knowledge/proficiency and level7 versatile choice replaced");
            Check(Asset<BlueprintFeatureSelection>(c, "WarDrummerCriticalSelection01441").IgnorePrerequisites && Asset<BlueprintFeatureSelection>(c, "WarDrummerCriticalSelection01441").AllFeatures.Count() == 2, "level6 bonus choice has precisely club and greatclub and bypasses BAB8");
            var external = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, external);
            var externalClass = Native<BlueprintCharacterClass>(external, "6afa347d804838b48bda16acb0573dc0");
            var externalArchetype = new BlueprintArchetype { name = "Provider_War Drummer_Archetype", AssetGuid = BlueprintGuid.Parse("04100000000000000000000000000001") }; Set(externalArchetype, "m_ParentClass", externalClass); external.AddCachedBlueprint(externalArchetype.AssetGuid, externalArchetype);
            var nativeArchetypes = (BlueprintArchetypeReference[])Get(externalClass, "m_Archetypes"); Set(externalClass, "m_Archetypes", nativeArchetypes.Concat(new[] { Ref<BlueprintArchetypeReference>(externalArchetype) }).ToArray());
            Check(Reject(() => Register(external)) && Tokens.All(t => external.Load(Id(t)) == null) && externalClass.Archetypes.Any(a => ReferenceEquals(a, externalArchetype)), "external equivalent is retained without a second War Drummer");
            foreach (var token in Tokens) {
                var conflict = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, conflict);
                var foreign = (BlueprintScriptableObject)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(c.Load(Id(token)), null); conflict.AddCachedBlueprint(foreign.AssetGuid, foreign);
                Check(Reject(() => Register(conflict)) && ReferenceEquals(conflict.Load(foreign.AssetGuid), foreign) && Tokens.Where(t => t != token).All(t => conflict.Load(Id(t)) == null), "foreign identity retained without partial graph: " + token);
            }
            foreach (bool afterCache in new[] { true, false }) {
                var failed = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, failed);
                var failedClass = Native<BlueprintCharacterClass>(failed, "6afa347d804838b48bda16acb0573dc0"); var arch = Get(failedClass, "m_Archetypes"); var ui = failedClass.Progression.UIGroups;
                var method = afterCache ? AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint") : AccessTools.Method(Module, "Append");
                h.Patch(method, postfix: new HarmonyMethod(typeof(WarDrummer01441Tests), afterCache ? nameof(CacheFault) : nameof(AppendFault)));
                if (afterCache) cacheFault = true; else appendFault = true;
                try { Check(Reject(() => Register(failed)) && Tokens.All(t => failed.Load(Id(t)) == null) && ReferenceEquals(Get(failedClass, "m_Archetypes"), arch) && ReferenceEquals(failedClass.Progression.UIGroups, ui), "real post-write/post-append fault restores native arrays and removes new graph"); }
                finally { cacheFault = appendFault = false; h.Unpatch(method, AccessTools.Method(typeof(WarDrummer01441Tests), afterCache ? nameof(CacheFault) : nameof(AppendFault))); }
                Register(failed); Check(failedClass.Archetypes.Count(a => a != null && a.AssetGuid == Id(Tokens[0])) == 1, "failed transaction retries once");
            }
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, original); }
    }
    static UnitEntityData Unit(BlueprintsCache c, int level)
    {
        var u = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c, "041-war-drummer-" + ++serial, "6afa347d804838b48bda16acb0573dc0", Math.Max(1, level));
        Units.Add(u); u.Stats.BaseAttackBonus.BaseValue = level; Set(u, "m_Position", new UnityEngine.Vector3(serial * 3, 0, 0)); return u;
    }
    static Feature Fact(UnitEntityData u, BlueprintFeature bp) { var f = new Feature(bp, u.Descriptor); u.Facts.Add(f); Check(f.IsTurnedOn, "native feature activation " + bp.name); return f; }
    static ItemEntityWeapon Weapon(BlueprintsCache c, UnitEntityData u, string type)
    {
        var bp = new BlueprintItemWeapon { name = "041-weapon", AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")) }; Set(bp, "m_Type", Ref<BlueprintWeaponTypeReference>(Native<BlueprintWeaponType>(c, type)));
        var w = Empty<ItemEntityWeapon>(); Set(w, "m_Blueprint", bp); Set(w, "HoldingSlot", u.Body.PrimaryHand); return w;
    }
    static int Damage(UnitEntityData u, ItemEntityWeapon weapon)
    {
        var rule = new RuleCalculateWeaponStats(u, weapon);
        foreach (var fact in u.Facts.List.Where(f => f.Active).ToArray()) foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is WeaponTypeDamageBonus))
            using (runtime.RequestEventContext()) { ((WeaponTypeDamageBonus)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(rule); nativeDamageCalls++; }
        return rule.DamageModifiers.Modifiers.Sum(m => m.Value);
    }
    static void Runtime(BlueprintsCache c)
    {
        Register(c);
        var prof = Asset<BlueprintFeature>(c, "WarDrummerProficiencies01441"); var mien = Asset<BlueprintFeature>(c, "WarDrummerFearsomeMien01441"); var rhythm = Asset<BlueprintFeature>(c, "WarDrummerDeadlyRhythm01441"); var effect = Asset<BlueprintBuff>(c, "WarDrummerDeadlyRhythmBuff01441");
        var profUnit = Unit(c, 1); var profFact = Fact(profUnit, prof);
        Check(profUnit.Proficiencies.Contains(WeaponCategory.Club) && profUnit.Proficiencies.Contains(WeaponCategory.Greatclub) && !profUnit.Proficiencies.Contains(WeaponCategory.Longsword), "native simple/greatclub proficiency with martial weapons excluded");
        profUnit.Facts.Remove(profFact); Check(!profUnit.Proficiencies.Contains(WeaponCategory.Greatclub), "native proficiency removal clears only its contribution");
        foreach (int level in new[] { 1, 2, 3, 6, 7, 11, 15, 19, 20 }) {
            var u = Unit(c, level); var beforeBluff = u.Stats.GetStat(StatType.CheckBluff).ModifiedValue; var beforeIntimidate = u.Stats.GetStat(StatType.CheckIntimidate).ModifiedValue;
            var beforeDiplomacy = u.Stats.GetStat(StatType.CheckDiplomacy).ModifiedValue; var beforeKnowledge = u.Stats.GetStat(StatType.SkillKnowledgeWorld).ModifiedValue;
            var mienFact = Fact(u, mien); int half = Math.Max(1, level / 2);
            Check(u.Stats.GetStat(StatType.CheckBluff).ModifiedValue == beforeBluff + half && u.Stats.GetStat(StatType.CheckIntimidate).ModifiedValue == beforeIntimidate + half && u.Stats.GetStat(StatType.CheckDiplomacy).ModifiedValue == beforeDiplomacy && u.Stats.GetStat(StatType.SkillKnowledgeWorld).ModifiedValue == beforeKnowledge, "actual native half-level Bluff/Intimidate only at level " + level);
            u.Facts.Remove(mienFact); Check(u.Stats.GetStat(StatType.CheckBluff).ModifiedValue == beforeBluff, "skill bonus cleans on feature removal");
            Fact(u, rhythm); var club = Weapon(c, u, "26aa0672af2c7d84ba93bec37758c712"); var greatclub = Weapon(c, u, "1b8c24cd1f9358c48839bb39266468c3"); var sword = Weapon(c, u, "d56c44bc9eb10204c8b386a02c7eed21");
            Check(Damage(u, club) == 0 && Damage(u, greatclub) == 0, "rhythm feature alone grants no permanent damage");
            foreach (string hostId in new[] { "75b3978757908d24aaaecaf2dc209b89", "ee51f69e69cb43c3bc7c02002e482d17" }) {
                var hostBp = Native<BlueprintBuff>(c, hostId); var host = u.Facts.Add(new Buff(hostBp, new MechanicsContext(u, u.Descriptor, hostBp), null)); nativeSongApplications++;
                int expected = new[] { 3, 7, 11, 15, 19 }.Count(n => level >= n);
                Check(u.Buffs.Enumerable.Count(b => b.Blueprint == effect) == 1 && Damage(u, club) == expected && Damage(u, greatclub) == expected && Damage(u, sword) == 0, "native self Inspired Rage and exact club/greatclub damage at level " + level);
                var receiver = Unit(c, level); Fact(receiver, rhythm); var foreignHost = receiver.Facts.Add(new Buff(hostBp, new MechanicsContext(u, receiver.Descriptor, hostBp), null));
                Check(receiver.Buffs.Enumerable.All(b => b.Blueprint != effect) && Damage(receiver, Weapon(c, receiver, "26aa0672af2c7d84ba93bec37758c712")) == 0, "another singer's rage never grants personal rhythm");
                receiver.Facts.Remove(foreignHost); u.Facts.Remove(host);
                Check(!u.Buffs.Enumerable.Any(b => b.Blueprint == effect) && Damage(u, club) == 0 && !u.Facts.List.Any(f => f.Blueprint.name.StartsWith("CotWWrathPsychicWarDrummerRhythmTier")), "native parent/child removal clears all damage tier facts");
            }
        }
        foreach (string token in new[] { "WarDrummerCriticalClub01441", "WarDrummerCriticalGreatclub01441" }) {
            var u = Unit(c, 6); var option = Asset<BlueprintFeature>(c, token); var before = u.Progression.Features.Enumerable.Count(f => f.Blueprint.AssetGuid.ToString() == "f4201c85a991369408740c6888362e20"); var wrapper = Fact(u, option);
            var granted = u.Progression.Features.Enumerable.Where(f => f.Blueprint.AssetGuid.ToString() == "f4201c85a991369408740c6888362e20").ToArray();
            Check(granted.Length == before + 1 && granted.Last().Param.WeaponCategory == (token.Contains("Greatclub") ? WeaponCategory.Greatclub : WeaponCategory.Club), "native parametrized Improved Critical is granted at BAB6");
            foreach (var type in new[] { "26aa0672af2c7d84ba93bec37758c712", "1b8c24cd1f9358c48839bb39266468c3", "d56c44bc9eb10204c8b386a02c7eed21" }) {
                var weapon = Weapon(c, u, type); var stats = new RuleCalculateWeaponStats(u, weapon);
                foreach (var runtime in granted.Last().Components.Where(r => r.SourceBlueprintComponent is ImprovedCriticalParametrized)) using (runtime.RequestEventContext()) { ((ImprovedCriticalParametrized)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(stats); nativeCriticalCalls++; }
                Check(stats.DoubleCriticalEdge == (weapon.Blueprint.Category == granted.Last().Param.WeaponCategory), "native critical handler changes only the chosen category");
            }
            u.Facts.Remove(wrapper); Check(u.Progression.Features.Enumerable.Count(f => f.Blueprint.AssetGuid.ToString() == "f4201c85a991369408740c6888362e20") == before, "native bonus critical fact cleans on removal");
        }
        var mixed = Unit(c, 3); mixed.Progression.Classes.Add(new ClassData(Native<BlueprintCharacterClass>(c, "ba34257984f4c41408ce1dc2004e342e")) { Level = 17 });
        int bluff = mixed.Stats.GetStat(StatType.CheckBluff).ModifiedValue; var mixedMien = Fact(mixed, mien);
        Check(mixed.Stats.GetStat(StatType.CheckBluff).ModifiedValue == bluff + 1, "Skald3/Wizard17 uses Skald level for Fearsome Mien");
        var mixedBuff = mixed.Facts.Add(new Buff(effect, new MechanicsContext(mixed, mixed.Descriptor, effect), null));
        Check(Damage(mixed, Weapon(c, mixed, "26aa0672af2c7d84ba93bec37758c712")) == 1, "Skald3/Wizard17 uses Skald level for all native damage thresholds");
        mixed.Facts.Remove(mixedBuff); mixed.Facts.Remove(mixedMien);
        Register(c); Check(ReferenceEquals(effect, Asset<BlueprintBuff>(c, "WarDrummerDeadlyRhythmBuff01441")), "runtime use does not mutate authored blueprint definitions");
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = nativeDamageCalls = nativeCriticalCalls = nativeSongApplications = cacheFaults = appendFaults = 0; Units.Clear();
        var h = new Harmony("CotW041.WarDrummerNativeContracts"); var game = Game.Instance; var previousState = game.State; var previousTime = game.TimeController;
        var helperUnits = (IList)AccessTools.Field(typeof(BardMasterpieces01435Tests), "units").GetValue(null); int helperCount = helperUnits.Count;
        var helperCurrent = AccessTools.Field(typeof(BardMasterpieces01435Tests), "current"); var previousContext = helperCurrent.GetValue(null); helperCurrent.SetValue(null, null);
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        var addBuff = AccessTools.Method(typeof(UnitHelper), "AddBuff", new[] { typeof(UnitDescriptor), typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?) });
        var endpoints = new[] { new { Method = (MethodBase)addBuff, Prefix = AccessTools.Method(typeof(Psychic070Tests), "CaptureDefense") }, new { Method = (MethodBase)AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) }), Prefix = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice") } };
        var suspended = endpoints.SelectMany(e => (Harmony.GetPatchInfo(e.Method)?.Prefixes.AsEnumerable() ?? Enumerable.Empty<Patch>()).Where(p => p.PatchMethod == e.Prefix).Select(p => new { Original = e.Method, Record = p })).ToArray();
        void Scene(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), name));
        try {
            foreach (var p in suspended) h.Unpatch(p.Original, p.Record.PatchMethod);
            Scene(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), "Skip"); Scene(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), "Controller"); Scene(AccessTools.Method(typeof(EntityFact), "Attach"), "Identity"); Scene(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), "Skip"); Scene(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), "Skip");
            Scene(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(TimeController), "RealTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(Game), "CurrentMode"), "Mode"); Scene(AccessTools.PropertyGetter(typeof(UnitEntityData), "IsPlayersEnemy"), "False"); Scene(AccessTools.Method(typeof(TurnBased.Controllers.CombatController), "IsInTurnBasedCombat"), "False"); Scene(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper), "IsActive"), "False"); Scene(AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect), "IsOwnerAvatarValid"), "False");
            h.Patch(AccessTools.PropertyGetter(typeof(Rulebook), "CurrentContext"), prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "Context"));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff), "RunAction"), transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "RuleBoundary"));
            h.Patch(AccessTools.Method(typeof(RuleCanApplyBuff), "OnTrigger"), transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "DifficultyBoundary"));
            h.Patch(AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams), "OnTrigger"), transpiler: new HarmonyMethod(typeof(Migration01426Tests), "DifficultyBoundary"));
            h.Patch(AccessTools.Method(typeof(ContextCalculateAbilityParams), "Calculate", new[] { typeof(MechanicsContext), typeof(BlueprintScriptableObject), typeof(UnitEntityData), typeof(Kingmaker.UnitLogic.Abilities.AbilityData) }), transpiler: new HarmonyMethod(typeof(BardMasterpieces01435Tests), "RuleBoundary"));
            h.Patch(AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(int), typeof(DiceType) }), transpiler: new HarmonyMethod(typeof(TimeStopSpell01438Tests), "RandomEndpoint"));
            Provider01419Tests.PatchLabels(h); Hydrate(cache); Registration(cache, h); Runtime(cache);
            PsychicExpansionTests.Inventory["warDrummer01441Contracts"] = new JObject {
                ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0033"), ["archetypeCount"] = 1, ["ordinaryFeatCount"] = 0, ["newSpellRootCount"] = 0,
                ["newBlueprints"] = new JArray(Tokens.Select(t => { var b = cache.Load(Id(t)); return new JObject { ["token"] = t, ["id"] = b.AssetGuid.ToString(), ["type"] = b.GetType().FullName }; })),
                ["nativeSongApplications"] = nativeSongApplications, ["nativeDamageHandlers"] = nativeDamageCalls, ["nativeCriticalHandlers"] = nativeCriticalCalls, ["postCacheWriteFaults"] = cacheFaults, ["postAppendFaults"] = appendFaults,
                ["privateResourceCount"] = 13, ["newHarmonyPatchCount"] = 0, ["newSerializedComponentTypeCount"] = 0, ["baseProgressionPreserved"] = true, ["personalOnlyRhythm"] = true,
                ["boundary"] = "Actual game class/progression JSON, native fact managers, AddFacts/AddProficiencies, ContextRankConfig/AddContextStatBonus, Inspired Rage activation/ContextActionApplyBuff/child removal, AddFeatureOnClassLevel and AddParametrizedFeatures execute. Weapon damage calls the attached native WeaponTypeDamageBonus handlers with real RuleCalculateWeaponStats and records native modifiers; final HP/combat commands are outside this fixture. Unit/world/clock/UI/area allocation, Unity RNG and difficulty-service endpoints use the established native test scene boundaries; exact legacy queued-dice and buff-capture test prefixes are suspended and restored. Character creation/level-up UI and complete game save/reload are observation steps in the guide."
            };
            Console.WriteLine("PASS: " + checks + " WarDrummer01441 native contracts."); return checks;
        } finally {
            foreach (var u in Units.AsEnumerable().Reverse()) foreach (var fact in u.Facts.List.ToArray()) if (fact.Manager != null) u.Facts.Remove(fact);
            h.UnpatchAll(h.Id); foreach (var saved in suspended) { var p = saved.Record; new Harmony(p.owner).Patch(saved.Original, prefix: new HarmonyMethod(p.PatchMethod) { priority = p.priority, before = p.before, after = p.after, debug = p.debug }); }
            Set(game, "State", previousState); Set(game, "TimeController", previousTime); helperCurrent.SetValue(null, previousContext); while (helperUnits.Count > helperCount) helperUnits.RemoveAt(helperUnits.Count - 1);
        }
    }
}
