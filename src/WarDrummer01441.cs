// Adapted from Holic75/KingmakerRebalance (MIT); see LICENSE-CotW.txt.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WarDrummer01441
    {
        internal const string ModuleName = "War Drummer";
        internal const string RootToken = "WarDrummerArchetype01441";
        internal const string ProficiencyToken = "WarDrummerProficiencies01441", MienToken = "WarDrummerFearsomeMien01441";
        internal const string RhythmToken = "WarDrummerDeadlyRhythm01441", BuffToken = "WarDrummerDeadlyRhythmBuff01441";
        internal const string SelectionToken = "WarDrummerCriticalSelection01441";
        internal const string SkaldId = "6afa347d804838b48bda16acb0573dc0", ProgressionId = "26418fed2bc153245972a5b54204ed75";
        internal const string KnowledgeId = "b2cc2d1d1844d0443866164466613c5c", ProficiencyId = "64e3e3fb96c439b4f8d896339b2f358a";
        internal const string VersatileId = "d2a8fde8985691045b90e1ec57e3cc57", MartialId = "203992ef5b35c864390b4e4a1e200629";
        internal const string ClubId = "26aa0672af2c7d84ba93bec37758c712", GreatclubId = "1b8c24cd1f9358c48839bb39266468c3";
        internal const string CriticalId = "f4201c85a991369408740c6888362e20";
        const string RhythmDescription = "While performing your own Inspired Rage, gain +1 damage with clubs and greatclubs at Skald level 3, plus 1 at levels 7, 11, 15 and 19 (maximum +5). The benefit is personal and is not shared with allies.";
        internal static readonly string[] HostIds = { "75b3978757908d24aaaecaf2dc209b89", "ee51f69e69cb43c3bc7c02002e482d17" };
        internal static readonly int[] Thresholds = { 3, 7, 11, 15, 19 };
        internal static readonly string[] OwnTokens = new[] { RootToken, ProficiencyToken, MienToken, RhythmToken, BuffToken, SelectionToken, "WarDrummerCriticalClub01441", "WarDrummerCriticalGreatclub01441" }
            .Concat(Thresholds.Select(n => "WarDrummerRhythmTier" + n + "01441")).ToArray();
        sealed class Publication { internal BlueprintScriptableObject[] Assets; internal string[] Definitions; }
        static readonly ConditionalWeakTable<BlueprintsCache, Publication> Published = new ConditionalWeakTable<BlueprintsCache, Publication>();
        static LevelEntry Entry(int level, params BlueprintFeature[] facts) { var e = new LevelEntry { Level = level }; Set(e, "m_Features", facts.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e; }
        static string Definition(BlueprintScriptableObject bp)
        {
            object shape;
            if (bp is BlueprintArchetype a) shape = new object[] { a.GetParentClass(), a.AddFeatures.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveFeatures.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveSpellbook, a.ReplaceClassSkills, a.ChangeCasterType, a.AddSkillPoints };
            else if (bp is BlueprintFeatureSelection s) shape = new object[] { s.Ranks, s.Groups, s.IgnorePrerequisites, Get(s, "m_Features"), Get(s, "m_AllFeatures") };
            else if (bp is BlueprintFeature f) shape = new object[] { f.Ranks, f.Groups, f.IsClassFeature, f.HideInCharacterSheetAndLevelUp };
            else if (bp is BlueprintBuff b) shape = new object[] { b.Stacking, b.Ranks, Get(b, "m_Flags") };
            else throw new InvalidOperationException("Unexpected War Drummer resource type.");
            return Migration01426.Definition(new object[] { bp.name, bp.ComponentsArray, shape });
        }
        static AddParametrizedFeatures Critical(BlueprintParametrizedFeature feat, WeaponCategory category)
        {
            var c = new AddParametrizedFeatures();
            var itemType = AccessTools.Field(typeof(AddParametrizedFeatures), "m_Features").FieldType.GetElementType();
            var item = Activator.CreateInstance(itemType, true); Set(item, "m_Feature", Ref<BlueprintParametrizedFeatureReference>(feat)); Set(item, "ParamWeaponCategory", category); Set(item, "ParamObject", new AnyBlueprintReference());
            var array = Array.CreateInstance(itemType, 1); array.SetValue(item, 0); Set(c, "m_Features", array); return c;
        }
        static Conditional Trigger(BlueprintFeature feature, BlueprintBuff effect, BlueprintBuff host)
        {
            var has = new ContextConditionHasFact(); Set(has, "m_Fact", Ref<BlueprintUnitFactReference>(feature));
            var self = new ContextConditionIsCaster();
            var apply = Apply(effect, 0, fromSpell: false); apply.Permanent = true; apply.AsChild = true; apply.IsNotDispelable = true;
            var action = new Conditional { name = "WarDrummer01441SelfRhythm", ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { has, self } }, IfTrue = Actions(apply), IfFalse = Actions() };
            foreach (var element in new Element[] { action, has, self, apply }) element.Owner = host;
            return action;
        }
        static AddFactContextActions HostActions(BlueprintBuff host)
        {
            var matches = host.GetComponents<AddFactContextActions>().Where(c => (c.Activated?.Actions ?? Array.Empty<GameAction>()).OfType<Conditional>().Any(a =>
                (a.ConditionsChecker?.Conditions ?? Array.Empty<Condition>()).Any(condition => (condition is ContextConditionHasFact || condition is ContextConditionCasterHasFact) &&
                (Get(condition, "m_Fact") as BlueprintReferenceBase)?.Guid == BlueprintGuid.Parse(RagePowers.FearlessId)))).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("War Drummer native Inspired Rage anchor changed: " + host.name);
            return matches[0];
        }
        // Append only this archetype, its UI references and two self-only song
        // actions. All original arrays/elements are retained, with exact rollback.
        static void Append(BlueprintCharacterClass skald, BlueprintProgression progression, BlueprintArchetype archetype, BlueprintFeature prof, BlueprintFeature mien, BlueprintFeature rhythm, BlueprintFeatureSelection choice, BlueprintBuff buff, BlueprintBuff[] hosts)
        {
            var oldArchetypes = (BlueprintArchetypeReference[])Get(skald, "m_Archetypes"); var oldUI = progression.UIGroups;
            var oldDet = (BlueprintFeatureBaseReference[])Get(progression, "m_UIDeterminatorsGroup");
            var handlers = hosts.Select(HostActions).ToArray(); var oldActions = handlers.Select(h => h.Activated).ToArray();
            try {
                if (!oldArchetypes.Any(r => r.Guid == archetype.AssetGuid)) Set(skald, "m_Archetypes", oldArchetypes.Concat(new[] { Ref<BlueprintArchetypeReference>(archetype) }).ToArray());
                var uiFacts = new BlueprintFeature[] { prof, mien, rhythm, choice };
                var ownGroups = (oldUI ?? Array.Empty<UIGroup>()).Where(g => g.Features.Any(f => uiFacts.Contains(f))).ToArray();
                if (ownGroups.Length == 0) { var ui = new UIGroup(); Set(ui, "m_Features", uiFacts.Select(Ref<BlueprintFeatureBaseReference>).ToList()); progression.UIGroups = (oldUI ?? Array.Empty<UIGroup>()).Concat(new[] { ui }).ToArray(); }
                else if (ownGroups.Length != 1 || !ownGroups[0].Features.SequenceEqual(uiFacts)) throw new InvalidOperationException("War Drummer UI group changed.");
                Set(progression, "m_UIDeterminatorsGroup", (oldDet ?? Array.Empty<BlueprintFeatureBaseReference>()).Concat(new[] { prof, mien }.Where(f => !(oldDet ?? Array.Empty<BlueprintFeatureBaseReference>()).Any(r => r.Guid == f.AssetGuid)).Select(Ref<BlueprintFeatureBaseReference>)).ToArray());
                for (int i = 0; i < hosts.Length; i++) {
                    var expected = Trigger(rhythm, buff, hosts[i]); var before = oldActions[i].Actions ?? Array.Empty<GameAction>();
                    var found = before.Where(a => a?.name == expected.name).ToArray();
                    if (found.Length > 1 || found.Length == 1 && Migration01426.Definition(found[0]) != Migration01426.Definition(expected)) throw new InvalidOperationException("War Drummer song action changed.");
                    if (found.Length == 0) handlers[i].Activated = Actions(before.Concat(new GameAction[] { expected }).ToArray());
                }
            } catch {
                Set(skald, "m_Archetypes", oldArchetypes); progression.UIGroups = oldUI; Set(progression, "m_UIDeterminatorsGroup", oldDet);
                for (int i = 0; i < handlers.Length; i++) handlers[i].Activated = oldActions[i]; throw;
            }
        }
        static void Attach(BlueprintCharacterClass skald, BlueprintProgression progression, BlueprintArchetype archetype, BlueprintFeature prof, BlueprintFeature mien, BlueprintFeature rhythm, BlueprintFeatureSelection choice, BlueprintBuff buff, BlueprintBuff[] hosts)
        {
            var oldArchetypes = Get(skald, "m_Archetypes"); var oldUI = progression.UIGroups; var oldDet = Get(progression, "m_UIDeterminatorsGroup");
            var handlers = hosts.Select(HostActions).ToArray(); var actions = handlers.Select(h => h.Activated).ToArray();
            try { Append(skald, progression, archetype, prof, mien, rhythm, choice, buff, hosts); }
            catch { Set(skald, "m_Archetypes", oldArchetypes); progression.UIGroups = oldUI; Set(progression, "m_UIDeterminatorsGroup", oldDet); for (int i = 0; i < handlers.Length; i++) handlers[i].Activated = actions[i]; throw; }
        }
        internal static void Register(BlueprintsCache cache)
        {
            var state = Published.GetValue(cache, _ => new Publication());
            lock (state) {
                var f = new PsychicAssets(cache); var skald = f.Native<BlueprintCharacterClass>(SkaldId); var progression = f.Native<BlueprintProgression>(ProgressionId);
                var knowledge = f.Native<BlueprintFeature>(KnowledgeId); var sourceProf = f.Native<BlueprintFeature>(ProficiencyId); var versatile = f.Native<BlueprintFeature>(VersatileId);
                var hosts = HostIds.Select(f.Native<BlueprintBuff>).ToArray();
                if (skald.Progression != progression || !progression.LevelEntries.Single(e => e.Level == 1).Features.Contains(knowledge) || !progression.LevelEntries.Single(e => e.Level == 1).Features.Contains(sourceProf) || !progression.LevelEntries.Single(e => e.Level == 7).Features.Contains(versatile))
                    throw new InvalidOperationException("War Drummer requires the actual native Skald progression.");
                if (skald.Archetypes.Any(a => a != null && a.AssetGuid != Id(RootToken) && (a.name ?? "").Replace("_", "").Replace(" ", "").Replace("-", "").IndexOf("WarDrummer", StringComparison.OrdinalIgnoreCase) >= 0)) throw new InvalidOperationException("An external War Drummer archetype already exists; preserved.");
                if (state.Assets != null) {
                    for (int i = 0; i < state.Assets.Length; i++) if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) || Definition(state.Assets[i]) != state.Definitions[i]) throw new InvalidOperationException("War Drummer published graph changed.");
                    Attach(skald, progression, (BlueprintArchetype)cache.Load(Id(RootToken)), (BlueprintFeature)cache.Load(Id(ProficiencyToken)), (BlueprintFeature)cache.Load(Id(MienToken)), (BlueprintFeature)cache.Load(Id(RhythmToken)), (BlueprintFeatureSelection)cache.Load(Id(SelectionToken)), (BlueprintBuff)cache.Load(Id(BuffToken)), hosts); return;
                }
                foreach (var token in OwnTokens) if (cache.Load(Id(token)) != null) throw new InvalidOperationException("War Drummer private identity collision: " + token);
                var prof = f.Clone(sourceProf, ProficiencyToken, "War Drummer Proficiencies", "Proficient with simple weapons and the greatclub. Native Skald armor, shield and armored casting proficiencies are retained; general martial weapon proficiency is replaced.");
                var grant = prof.GetComponents<AddFacts>().Single(); var refs = (BlueprintUnitFactReference[])Get(grant, "m_Facts");
                if (refs.Count(r => r.Guid == BlueprintGuid.Parse(MartialId)) != 1) throw new InvalidOperationException("War Drummer martial proficiency anchor changed.");
                Set(grant, "m_Facts", refs.Where(r => r.Guid != BlueprintGuid.Parse(MartialId)).ToArray());
                prof.ComponentsArray = prof.ComponentsArray.Concat(new BlueprintComponent[] { new AddProficiencies { WeaponProficiencies = new[] { WeaponCategory.Greatclub } } }).ToArray();
                var mien = f.Clone(knowledge, MienToken, "Fearsome Mien", "Add half your Skald level (minimum 1) to Bluff and Intimidate checks. This does not increase Diplomacy or other skill checks.");
                mien.ComponentsArray = mien.ComponentsArray.Where(c => !(c is AddContextStatBonus)).Concat(new BlueprintComponent[] {
                    new AddContextStatBonus { Stat = StatType.CheckBluff, Descriptor = ModifierDescriptor.UntypedStackable, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank } },
                    new AddContextStatBonus { Stat = StatType.CheckIntimidate, Descriptor = ModifierDescriptor.UntypedStackable, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank } }
                }).ToArray();
                var rhythm = f.Feature(RhythmToken, "Deadly Rhythm", RhythmDescription);
                var club = f.Native<Kingmaker.Blueprints.Items.Weapons.BlueprintWeaponType>(ClubId); var greatclub = f.Native<Kingmaker.Blueprints.Items.Weapons.BlueprintWeaponType>(GreatclubId);
                if (club.Category != WeaponCategory.Club || greatclub.Category != WeaponCategory.Greatclub) throw new InvalidOperationException("War Drummer weapon types changed.");
                var tiers = new List<BlueprintComponent>();
                foreach (int level in Thresholds) {
                    var c = new WeaponTypeDamageBonus { DamageBonus = 1 }; Set(c, "m_WeaponType", Ref<BlueprintWeaponTypeReference>(club));
                    var g = new WeaponTypeDamageBonus { DamageBonus = 1 }; Set(g, "m_WeaponType", Ref<BlueprintWeaponTypeReference>(greatclub));
                    var tier = f.Feature("WarDrummerRhythmTier" + level + "01441", "Deadly Rhythm", "Personal club/greatclub damage contribution while performing Inspired Rage.", c, g); tier.HideInCharacterSheetAndLevelUp = true;
                    var add = new AddFeatureOnClassLevel { Level = level }; Set(add, "m_Class", Ref<BlueprintCharacterClassReference>(skald)); Set(add, "m_Feature", Ref<BlueprintFeatureReference>(tier)); Set(add, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>()); Set(add, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); tiers.Add(add);
                }
                var buff = f.Buff(BuffToken, "Deadly Rhythm", RhythmDescription, tiers.ToArray()); buff.Stacking = StackingType.Replace; Set(buff, "m_Flags", Enum.ToObject(AccessTools.Field(typeof(BlueprintBuff), "m_Flags").FieldType, 2));
                var critical = f.Native<BlueprintParametrizedFeature>(CriticalId);
                if (critical.ParameterType != FeatureParameterType.WeaponCategory) throw new InvalidOperationException("War Drummer requires native weapon-category Improved Critical.");
                var options = new[] { f.Feature("WarDrummerCriticalClub01441", "Improved Critical (Club)", "Gain Improved Critical (club) as a bonus feat.", Critical(critical, WeaponCategory.Club)), f.Feature("WarDrummerCriticalGreatclub01441", "Improved Critical (Greatclub)", "Gain Improved Critical (greatclub) as a bonus feat.", Critical(critical, WeaponCategory.Greatclub)) };
                var choice = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true, IgnorePrerequisites = true, Groups = Array.Empty<FeatureGroup>() }, SelectionToken, "Improved Critical (Club or Greatclub)", "At Skald level 6, choose Improved Critical with either the club or the greatclub as a bonus feat, bypassing its normal prerequisites.");
                Set(choice, "m_Features", options.Select(Ref<BlueprintFeatureReference>).ToArray()); Set(choice, "m_AllFeatures", options.Select(Ref<BlueprintFeatureReference>).ToArray());
                Set(choice, "m_Icon", critical.Icon);
                foreach (var option in options) { option.HideInCharacterSheetAndLevelUp = true; Set(option, "m_Icon", critical.Icon); }
                var archetype = f.Add(new BlueprintArchetype { AddFeatures = new[] { Entry(1, prof, mien), Entry(3, rhythm), Entry(6, choice) }, RemoveFeatures = new[] { Entry(1, knowledge, sourceProf), Entry(7, versatile) } }, RootToken, "War Drummer", "");
                Set(archetype, "m_ParentClass", skald); archetype.LocalizedName = Text(RootToken, "name", "War Drummer");
                archetype.LocalizedDescription = Text(RootToken, "description", "A Skald who fights with clubs and greatclubs. Replaces martial weapon proficiency and Bardic Knowledge at level 1 and the level-7 Versatile Performance choice. Gains Fearsome Mien at level 1, personal Deadly Rhythm at level 3, and Improved Critical (club or greatclub) at level 6. Other Skald abilities and spellcasting are unchanged.");
                var staged = f.Assets.Values.ToArray();
                try {
                    Migration01426.Commit(f, Array.Empty<BlueprintFeatureSelection>());
                    var definitions = staged.Select(Definition).ToArray();
                    Attach(skald, progression, archetype, prof, mien, rhythm, choice, buff, hosts);
                    state.Assets = staged; state.Definitions = definitions;
                } catch { foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid); throw; }
            }
        }
    }
}
