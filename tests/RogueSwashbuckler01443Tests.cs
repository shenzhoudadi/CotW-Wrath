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
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Mechanics;

internal static class RogueSwashbuckler01443Tests
{
    static readonly Type Module = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.RogueSwashbuckler01443", true);
    static readonly string[] Tokens = (string[])AccessTools.Field(Module, "OwnTokens").GetValue(null);
    const string Rogue = "299aa766dee3cbf4790da4efb8c72484", Progression = "b57b2a75a5abcaf47a01cf84672b50e9", Trick = "c5158a6622d0b694a99efb1d0025d2c1", Talents = "c074a5d615200494b8f2a9c845799d93";
    static int checks, serial, saves, selections, cacheFaults, appendFaults, connectFaults; static bool cacheFault, appendFault, connectFault;
    static readonly List<UnitEntityData> Units = new List<UnitEntityData>();
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static object Call(Type t, string n, params object[] a) => AccessTools.Method(t, n).Invoke(null, a);
    static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static BlueprintGuid Id(string t) => (BlueprintGuid)Call(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id", t);
    static T Asset<T>(BlueprintsCache c, string token) where T : SimpleBlueprint => (T)c.Load(Id(token));
    static T Native<T>(BlueprintsCache c, string id) where T : SimpleBlueprint => (T)c.Load(BlueprintGuid.Parse(id));
    static void Check(bool ok, string message) { if (!ok) throw new Exception("RogueSwashbuckler01443: " + message); checks++; }
    static void Register(BlueprintsCache c) => Call(Module, "Register", c);
    static bool Reject(Action a) { try { a(); return false; } catch (Exception e) { while (e is TargetInvocationException) e = e.InnerException; if (e is InvalidOperationException) return true; throw; } }
    static T Ref<T>(SimpleBlueprint b) where T : BlueprintReferenceBase, new() { var r = BlueprintReferenceBase.CreateTyped<T>(b); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(r, b, null); return r; }
    static void Hydrate(BlueprintsCache c, bool reset = false)
    {
        foreach (var item in new[] { new[] { Rogue, "m_Progression", "m_Archetypes" }, new[] { Progression, "LevelEntries", "UIGroups", "m_UIDeterminatorsGroup" } }) {
            var bp = c.Load(BlueprintGuid.Parse(item[0])); var data = PsychicExpansionTests.NativeRows.Single(r => (string)r["blueprint"]["AssetId"] == item[0])["blueprint"]["Data"];
            foreach (string n in item.Skip(1)) { var f = AccessTools.Field(bp.GetType(), n); var v = f.GetValue(bp); if (reset || v == null || v is Array a && a.Length == 0 || v is BlueprintReferenceBase r && r.Guid == default(BlueprintGuid)) f.SetValue(bp, PsychicExpansionTests.Read(data[n], f.FieldType)); }
        }
    }
    static BlueprintsCache Fresh(BlueprintsCache parent)
    {
        var c = (BlueprintsCache)Call(typeof(TimeStutter01437Tests), "Fresh", parent); foreach (var t in Tokens) c.RemoveCachedBlueprint(Id(t));
        var old = ResourcesLibrary.BlueprintsCache; AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c);
        try { Hydrate(c, true); } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, old); } return c;
    }
    static void CacheFault(BlueprintGuid __0) { if (cacheFault && __0 == Id(Tokens[0])) { cacheFault = false; cacheFaults++; throw new InvalidOperationException("043 rogue post-cache fault"); } }
    static void AppendFault() { if (appendFault) { appendFault = false; appendFaults++; throw new InvalidOperationException("043 rogue post-append fault"); } }
    static void ConnectFault() { if (connectFault) { connectFault = false; connectFaults++; throw new InvalidOperationException("043 rogue post-connect fault"); } }
    static void Registration(BlueprintsCache parent, Harmony h)
    {
        var old = ResourcesLibrary.BlueprintsCache;
        try {
            var c = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c); var cls = Native<BlueprintCharacterClass>(c, Rogue); var rows = cls.Progression.LevelEntries; var groups = cls.Progression.UIGroups; var before = cls.Archetypes.ToArray();
            Register(c); Register(c); var root = Asset<BlueprintArchetype>(c, Tokens[0]);
            Check(Tokens.Length == 4 && Tokens.All(t => c.Load(Id(t)) != null) && cls.Archetypes.Count(a => a == root) == 1, "four stable private nodes, one Rogue archetype");
            Check(ReferenceEquals(rows, cls.Progression.LevelEntries) && cls.Archetypes.Where(a => a != root).SequenceEqual(before) && cls.Progression.UIGroups.Take(groups.Length).SequenceEqual(groups), "all native rows, archetypes and prior UI groups preserved");
            Check(root.GetParentClass() == cls && !root.RemoveSpellbook && !root.ReplaceClassSkills && !root.ChangeCasterType && root.AddSkillPoints == 0, "Rogue parent and unrelated class mechanics retained");
            for (int n = 1; n <= 20; n++) {
                var add = root.GetAddEntry(n).Features.ToArray(); var remove = root.GetRemoveEntry(n).Features.ToArray();
                Check(add.Length == (n == 1 || n % 3 == 0 && n <= 18 ? 1 : 0) && remove.Length == add.Length, "native level-entry access only changes intended levels " + n);
                if (add.Length > 0) Check(add[0].AssetGuid == Id(n == 1 ? Tokens[1] : Tokens[2]) && remove[0].AssetGuid.ToString() == (n == 1 ? "dbb6b3bffe6db3547b31c3711653838e" : "0bcbe9e450b0e7b428f08f66c53c5136"), "exact Trapfinding/Danger Sense replacement at " + n);
            }
            foreach (var token in Tokens) {
                var bad = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, bad); var foreign = (BlueprintScriptableObject)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(c.Load(Id(token)), null); bad.AddCachedBlueprint(foreign.AssetGuid, foreign);
                Check(Reject(() => Register(bad)) && ReferenceEquals(bad.Load(foreign.AssetGuid), foreign) && Tokens.Where(t => t != token).All(t => bad.Load(Id(t)) == null), "private collision retains foreign identity without partial graph " + token);
            }
            foreach (bool cache in new[] { true, false }) {
                var failed = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, failed); var target = Native<BlueprintCharacterClass>(failed, Rogue); var arch = Get(target, "m_Archetypes"); var ui = target.Progression.UIGroups;
                var method = cache ? AccessTools.Method(typeof(BlueprintsCache), "AddCachedBlueprint") : AccessTools.Method(Module, "Append"); var patch = AccessTools.Method(typeof(RogueSwashbuckler01443Tests), cache ? nameof(CacheFault) : nameof(AppendFault)); h.Patch(method, postfix: new HarmonyMethod(patch)); if (cache) cacheFault = true; else appendFault = true;
                try { Check(Reject(() => Register(failed)) && ReferenceEquals(Get(target, "m_Archetypes"), arch) && ReferenceEquals(target.Progression.UIGroups, ui) && Tokens.All(t => failed.Load(Id(t)) == null), "actual post-write failure restores exact native arrays and removes private graph"); }
                finally { cacheFault = appendFault = false; h.Unpatch(method, patch); }
                Register(failed); Check(target.Archetypes.Count(a => a?.AssetGuid == Id(Tokens[0])) == 1, "failed transaction safely retries once");
            }
            foreach (bool published in new[] { false, true }) {
                var failed = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, failed); if (published) Register(failed); var target = Native<BlueprintCharacterClass>(failed, Rogue); var pool = Native<BlueprintFeatureSelection>(failed, Talents); var priorArch = Get(target, "m_Archetypes"); var priorUI = target.Progression.UIGroups; var priorPools = new[] { Get(pool, "m_Features"), Get(pool, "m_AllFeatures") }; var own = published ? Tokens.Select(t => failed.Load(Id(t))).ToArray() : null;
                var method = AccessTools.Method(Module, "Connect"); var patch = AccessTools.Method(typeof(RogueSwashbuckler01443Tests), nameof(ConnectFault)); h.Patch(method, postfix: new HarmonyMethod(patch)); connectFault = true;
                try { Check(Reject(() => Register(failed)) && ReferenceEquals(Get(target, "m_Archetypes"), priorArch) && ReferenceEquals(target.Progression.UIGroups, priorUI) && ReferenceEquals(Get(pool, "m_Features"), priorPools[0]) && ReferenceEquals(Get(pool, "m_AllFeatures"), priorPools[1]) && Tokens.Select((t, i) => published ? ReferenceEquals(failed.Load(Id(t)), own[i]) : failed.Load(Id(t)) == null).All(x => x), "caller rollback after successful Connect postfix fault: published=" + published); }
                finally { connectFault = false; h.Unpatch(method, patch); }
                Register(failed); Check(target.Archetypes.Count(a => a?.AssetGuid == Id(Tokens[0])) == 1, "post-connect rejection restores expected child projections for retry");
            }
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, c); var daring = Asset<BlueprintFeature>(c, Tokens[2]); daring.Ranks = 7; Check(Reject(() => Register(c)), "real authored rank changes remain rejected"); daring.Ranks = 6; Register(c);
            var secondChoice = Asset<BlueprintFeatureSelection>(c, Tokens[3]); var beforeChildren = Get(secondChoice, "m_AllFeatures"); Set(secondChoice, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>());
            try { Check(Reject(() => Register(c)), "private child author references cannot be silently rebaselined from the provider"); } finally { Set(secondChoice, "m_AllFeatures", beforeChildren); } Register(c);
            var component = daring.ComponentsArray[0]; var owner = component.OwnerBlueprint; component.OwnerBlueprint = Asset<BlueprintFeature>(c, Tokens[1]);
            try { Check(Reject(() => Register(c)), "foreign private component ownership remains rejected"); } finally { component.OwnerBlueprint = owner; } Register(c);
            var ext = Fresh(parent); AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, ext); var extClass = Native<BlueprintCharacterClass>(ext, Rogue); var e = new BlueprintArchetype { name = "ForeignRogueSwashbucklerArchetype", AssetGuid = Id("043ForeignRogueSwashbuckler") }; Set(e, "m_ParentClass", extClass); ext.AddCachedBlueprint(e.AssetGuid, e); Set(extClass, "m_Archetypes", ((BlueprintArchetypeReference[])Get(extClass, "m_Archetypes")).Concat(new[] { Ref<BlueprintArchetypeReference>(e) }).ToArray());
            Check(Reject(() => Register(ext)) && Tokens.All(t => ext.Load(Id(t)) == null) && extClass.Archetypes.Contains(e), "external equivalent Rogue archetype retained without duplicate");
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, old); }
    }
    static UnitEntityData Unit(BlueprintsCache c, int level) { var u = (UnitEntityData)Call(typeof(BardMasterpieces01435Tests), "Unit", c, "043-rogue-" + ++serial, Rogue, level); Units.Add(u); return u; }
    static Feature Fact(UnitEntityData u, BlueprintFeature bp) { var f = new Feature(bp, u.Descriptor); u.Facts.Add(f); Check(f.IsTurnedOn, "native feature active " + bp.name); return f; }
    static void Runtime(BlueprintsCache c, Harmony h)
    {
        Register(c); var training = Asset<BlueprintFeature>(c, Tokens[1]); var daring = Asset<BlueprintFeature>(c, Tokens[2]);
        var u = Unit(c, 1); bool previous = u.Proficiencies.Contains(WeaponCategory.Greataxe); var grant = Fact(u, training);
        Check(u.Proficiencies.Contains(WeaponCategory.Greataxe) && u.HasFact(Native<BlueprintFeature>(c, "203992ef5b35c864390b4e4a1e200629")), "native AddFacts grants actual martial proficiency fact"); u.Facts.Remove(grant); Check(u.Proficiencies.Contains(WeaponCategory.Greataxe) == previous, "native grant removal restores previous proficiency");
        var existing = Unit(c, 1); var originalProficiency = Fact(existing, Native<BlueprintFeature>(c, "203992ef5b35c864390b4e4a1e200629")); var wrapper = Fact(existing, training); existing.Facts.Remove(wrapper);
        Check(existing.Proficiencies.Contains(WeaponCategory.Greataxe) && ReferenceEquals(existing.Progression.Features.GetFact(originalProficiency.Blueprint), originalProficiency), "removing Martial Training preserves an independent earlier martial proficiency fact and source");
        foreach (int rank in Enumerable.Range(1, 6)) {
            var unit = Unit(c, 3 * rank); int mobility = unit.Stats.SkillMobility.ModifiedValue, perception = unit.Stats.SkillPerception.ModifiedValue;
            var fact = Fact(unit, daring); for (int r = 1; r < rank; r++) fact.AddRank(); fact.Reapply();
            Check(unit.Stats.SkillMobility.ModifiedValue == mobility + rank && unit.Stats.SkillPerception.ModifiedValue == perception, "native FeatureRank morale Mobility only at rank " + rank);
            var stronger = unit.Stats.SkillMobility.AddModifier(8, fact, ModifierDescriptor.Morale); Check(unit.Stats.SkillMobility.ModifiedValue == mobility + 8, "native morale bonus uses maximum, not sum"); stronger.Remove();
            foreach (var descriptor in new[] { SpellDescriptor.Fear, SpellDescriptor.Fire, SpellDescriptor.None }) foreach (var type in new[] { SavingThrowType.Will, SavingThrowType.Fortitude, SavingThrowType.Reflex }) {
                var ability = new BlueprintAbility { ComponentsArray = new BlueprintComponent[] { new SpellDescriptorComponent { Descriptor = descriptor } } }; ability.OnEnable(); var context = new MechanicsContext(unit, unit.Descriptor, ability); var rule = new RuleSavingThrow(unit, type, 20) { Reason = context }; int before = unit.Stats.SaveWill.ModifiedValue;
                foreach (var runtime in fact.Components.Where(r => r.SourceBlueprintComponent is SavingThrowBonusAgainstDescriptor)) using (runtime.RequestEventContext()) { ((SavingThrowBonusAgainstDescriptor)runtime.SourceBlueprintComponent).OnEventAboutToTrigger(rule); saves++; }
                Check(unit.Stats.SaveWill.ModifiedValue == before + (descriptor == SpellDescriptor.Fear ? rank : 0), "native descriptor handler scopes saving bonus to fear " + rank + "/" + descriptor + "/" + type); rule.OnDidTrigger(); Check(unit.Stats.SaveWill.ModifiedValue == before, "native rule temporary modifiers are removed");
            }
            // Native Remove loses one rank, and re-applies the remaining fact.
            for (int remaining = rank - 1; remaining >= 0; remaining--) { unit.Facts.Remove(fact); Check(unit.Stats.SkillMobility.ModifiedValue == mobility + remaining, "native rank loss recalculates Mobility and final removal clears it: " + remaining); }
        }
        var trick = Native<BlueprintFeatureSelection>(c, Trick); var pool = Native<BlueprintFeatureSelection>(c, Talents); var second = Asset<BlueprintFeatureSelection>(c, Tokens[3]); var fields = new[] { "m_Features", "m_AllFeatures" }; var prior = fields.Select(n => Get(trick, n)).ToArray(); var components = trick.ComponentsArray;
        try {
            // Two real, already-hydrated native feats isolate the native nested
            // selection algorithm from hundreds of unrelated optional entries.
            var leaves = new[] { Native<BlueprintFeature>(c, "9972f33f977fc724c838e59641b2fca5"), Native<BlueprintFeature>(c, "97e216dbb46ae3c4faef90cf6bbe6fd5") }; foreach (var n in fields) Set(trick, n, leaves.Select(Ref<BlueprintFeatureReference>).ToArray());
            h.Patch(AccessTools.Method(typeof(SelectFeature), "Apply"), transpiler: new HarmonyMethod(typeof(Feat01424Tests), "ApplyBoundary"));
            foreach (bool own in new[] { false, true }) {
                var unit = Unit(c, 8); unit.Stats.Strength.BaseValue = unit.Stats.Dexterity.BaseValue = 18; unit.Stats.BaseAttackBonus.BaseValue = 8; if (own) Fact(unit, training); Set(unit.Progression, "m_Selections", Activator.CreateInstance(AccessTools.Field(typeof(UnitProgressionData), "m_Selections").FieldType));
                var state = Empty<LevelUpState>(); Set(state, "m_Unit", unit); Set(state, "Selections", new List<FeatureSelectionState>()); Set(state, "NextCharacterLevel", 8); state.NextClassLevel = 8; state.SelectedClass = Native<BlueprintCharacterClass>(c, Rogue);
                for (int pick = 0; pick < 2; pick++) {
                    var pending = state.AddSelection(null, default(FeatureSource), pool, 2 + pick * 2); Check(pool.CanSelect(unit.Descriptor, state, pending, trick), "native repeatable Combat Trick available " + own + "/" + pick); var choose = new SelectFeature(pending, trick); Check(choose.Check(state, unit.Descriptor), "native parent selection check"); choose.Apply(state, unit.Descriptor); selections++;
                    var nested = state.Selections.Last(s => s.Selection == trick); var child = new SelectFeature(nested, leaves[pick]); Check(child.Check(state, unit.Descriptor), "native combat feat prerequisites remain enforced"); child.Apply(state, unit.Descriptor); selections++; Check(unit.HasFact(leaves[pick]), "native Apply grants distinct real combat feat");
                }
            }
            Register(c); Check(!pool.AllFeatures.Contains(second), "unrestricted native repeatable Combat Trick has no duplicate second UI entry");
            // The exact prerequisite installed by current official TTT-Base
            // Rogue.PatchRogueTalentSelection, after initial CotW publication.
            var ttt = new PrerequisiteNoFeature(); Set(ttt, "m_Feature", Ref<BlueprintFeatureReference>(trick)); trick.ComponentsArray = components.Concat(new BlueprintComponent[] { ttt }).ToArray(); trick.OnEnable(); Register(c); Register(c);
            Check(pool.AllFeatures.Count(f => f == second) == 1 && second.AllFeatures.SequenceEqual(leaves), "late TTT cap exposes one source-projected second choice without changing native children");
            var capped = Unit(c, 8); capped.Stats.Strength.BaseValue = capped.Stats.Dexterity.BaseValue = 18; capped.Stats.BaseAttackBonus.BaseValue = 8; Set(capped.Progression, "m_Selections", Activator.CreateInstance(AccessTools.Field(typeof(UnitProgressionData), "m_Selections").FieldType));
            var state2 = Empty<LevelUpState>(); Set(state2, "m_Unit", capped); Set(state2, "Selections", new List<FeatureSelectionState>()); Set(state2, "NextCharacterLevel", 8); state2.NextClassLevel = 8; state2.SelectedClass = Native<BlueprintCharacterClass>(c, Rogue);
            var firstPending = state2.AddSelection(null, default(FeatureSource), pool, 2); Check(!pool.CanSelect(capped.Descriptor, state2, firstPending, second), "second trick requires both martial training and original trick"); Fact(capped, training); Check(!pool.CanSelect(capped.Descriptor, state2, firstPending, second), "martial training alone cannot bypass first trick");
            var firstChoose = new SelectFeature(firstPending, trick); Check(firstChoose.Check(state2, capped.Descriptor), "TTT capped first trick uses original native choice"); firstChoose.Apply(state2, capped.Descriptor); selections++; var firstNested = state2.Selections.Last(s => s.Selection == trick); var firstLeaf = new SelectFeature(firstNested, leaves[0]); Check(firstLeaf.Check(state2, capped.Descriptor), "first real combat feat eligible"); firstLeaf.Apply(state2, capped.Descriptor); selections++;
            var secondPending = state2.AddSelection(null, default(FeatureSource), pool, 4); Check(!pool.CanSelect(capped.Descriptor, state2, secondPending, trick) && pool.CanSelect(capped.Descriptor, state2, secondPending, second), "TTT original remains once-only while qualified Swashbuckler gets second choice");
            var secondChoose = new SelectFeature(secondPending, second); Check(secondChoose.Check(state2, capped.Descriptor), "native second selection check"); secondChoose.Apply(state2, capped.Descriptor); selections++; var secondNested = state2.Selections.Last(s => s.Selection == second); var secondLeaf = new SelectFeature(secondNested, leaves[1]); Check(secondLeaf.Check(state2, capped.Descriptor), "second feat keeps native prerequisites"); secondLeaf.Apply(state2, capped.Descriptor); selections++;
            var thirdPending = state2.AddSelection(null, default(FeatureSource), pool, 6); Check(!pool.CanSelect(capped.Descriptor, state2, thirdPending, trick) && !pool.CanSelect(capped.Descriptor, state2, thirdPending, second) && capped.HasFact(leaves[0]) && capped.HasFact(leaves[1]), "two combat feats granted with third choice rejected under TTT cap");
            var unrelated = Unit(c, 8); Fact(unrelated, trick); var foreignState = Empty<LevelUpState>(); Set(foreignState, "m_Unit", unrelated); Set(foreignState, "Selections", new List<FeatureSelectionState>()); Set(foreignState, "NextCharacterLevel", 8); foreignState.SelectedClass = Native<BlueprintCharacterClass>(c, Rogue); foreignState.NextClassLevel = 8; var foreignPending = foreignState.AddSelection(null, default(FeatureSource), pool, 4); Check(!pool.CanSelect(unrelated.Descriptor, foreignState, foreignPending, second), "other Rogues cannot select archetype-only second trick");
        } finally { trick.ComponentsArray = components; trick.OnEnable(); for (int i = 0; i < fields.Length; i++) Set(trick, fields[i], prior[i]); }
        Register(c); Check(Asset<BlueprintFeature>(c, Tokens[2]) == daring, "native runtime use keeps authored resource definitions stable");
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = serial = saves = selections = cacheFaults = appendFaults = connectFaults = 0; Units.Clear(); var h = new Harmony("CotW043.RogueSwashbucklerNativeContracts"); var game = Game.Instance; var oldState = game.State; var oldTime = game.TimeController;
        return RunScene(cache, h, game, oldState, oldTime);
    }
    static int RunScene(BlueprintsCache cache, Harmony h, Game game, PersistentState oldState, TimeController oldTime)
    {
        var list = (IList)AccessTools.Field(typeof(BardMasterpieces01435Tests), "units").GetValue(null); int oldCount = list.Count; var current = AccessTools.Field(typeof(BardMasterpieces01435Tests), "current"); var oldContext = current.GetValue(null); current.SetValue(null, null);
        var state = Empty<PersistentState>(); var player = Empty<Player>(); player.GameTime = TimeSpan.FromHours(1); Set(player, "m_Party", new List<UnitEntityData>()); Set(player, "m_CharacterListsValid", true); state.PlayerState = player; Set(game, "State", state); Set(game, "TimeController", Empty<TimeController>());
        void Scene(MethodBase m, string n) => h.Patch(m, prefix: new HarmonyMethod(typeof(BardMasterpieces01435Tests), n));
        try {
            Scene(AccessTools.Method(typeof(FeatureCollection), "CheckStatPrerequisitesAndUpdate"), "Skip"); Scene(AccessTools.PropertyGetter(typeof(Game), "LevelUpController"), "Controller"); Scene(AccessTools.Method(typeof(EntityFact), "Attach"), "Identity"); Scene(AccessTools.Method(typeof(UnitState), "UpdateStatusEffect"), "Skip"); Scene(AccessTools.Method(typeof(UnitEntityData), "UpdateGroup"), "Skip"); Scene(AccessTools.PropertyGetter(typeof(TimeController), "GameTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(TimeController), "RealTime"), "Time"); Scene(AccessTools.PropertyGetter(typeof(Game), "CurrentMode"), "Mode"); Provider01419Tests.PatchLabels(h);
            h.Patch(AccessTools.PropertyGetter(typeof(IgnorePrerequisites), "Ignore"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "No"));
            h.Patch(AccessTools.Method(typeof(BlueprintFeatureSelection), "IsIgnorePrerequisites"), prefix: new HarmonyMethod(typeof(Migration01426Tests), "PoolIgnore"));
            Hydrate(cache); Registration(cache, h); Runtime(cache, h);
            PsychicExpansionTests.Inventory["rogueSwashbuckler01443Contracts"] = new JObject { ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 1, ["itemIds"] = new JArray("COTW-0134"), ["privateResourceCount"] = 4, ["newSpellRootCount"] = 0, ["newHarmonyPatchCount"] = 0, ["newSerializedComponentTypeCount"] = 0, ["nativeSavingThrowHandlers"] = saves, ["nativeSelectFeatureApplyCalls"] = selections, ["nativeCombatTrickRetained"] = true, ["lateTTTCapAdapted"] = true, ["thirdCappedChoiceRejected"] = true, ["duplicateVanillaChoiceAvoided"] = true, ["postCacheWriteFaults"] = cacheFaults, ["postAppendFaults"] = appendFaults, ["postConnectFaults"] = connectFaults, ["baseProgressionPreserved"] = true, ["newBlueprints"] = new JArray(Tokens.Select(t => { var b = cache.Load(Id(t)); return new JObject { ["token"] = t, ["id"] = b.AssetGuid.ToString(), ["type"] = b.GetType().FullName }; })), ["boundary"] = "Actual native Rogue class/progression JSON, GetAddEntry/GetRemoveEntry, feature activation/ranks/reapply/removal, AddFacts/AddProficiencies, FeatureRank/AddContextStatBonus, SavingThrowBonusAgainstDescriptor with native temporary modifier cleanup, and native SelectFeature Check/Apply/nested state execute. Combat Trick uses two real native feat leaves while unrelated child metadata is excluded only within the fixture. SelectFeature's final AddFact endpoint and scene/world/UI services use existing explicit controlled endpoints. Complete character UI, combat presentation and save/reload remain guide observations." };
            Console.WriteLine("PASS: " + checks + " Rogue Swashbuckler01443 native contracts."); return checks;
        } finally { foreach (var u in Units.AsEnumerable().Reverse()) foreach (var fact in u.Facts.List.ToArray()) if (fact.Manager != null) u.Facts.Remove(fact); h.UnpatchAll(h.Id); Set(game, "State", oldState); Set(game, "TimeController", oldTime); current.SetValue(null, oldContext); while (list.Count > oldCount) list.RemoveAt(list.Count - 1); }
    }
}
