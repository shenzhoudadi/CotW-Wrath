using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Each row is one original CotW ki power; ability/stat/advice variants are not extra items.
    internal static class MonkKiPowers01427
    {
        internal const string ModuleName = "Ki powers: five spell adaptations";
        internal const string MonkId = "e8f21e5b58e0569468e420ebea456124";
        internal static readonly string[] PoolIds = { "3049386713ff04245a38b32483362551", "4694f6ac27eaed34abb7d09ab67b4541" };
        internal static readonly string[] ResourceIds = { "9d9c90a9a1f52d04799294bf91c80a82", "7d002c1025fbfe2458f1509bf7a89ce1" };
        internal static readonly string[] Tokens = { "BurstOfAdrenaline", "BurstOfInsight", "CloakOfWinds", "AkashicForm", "FrightfulAspect" };
        static readonly string[] Titles = { "Burst of Adrenaline", "Burst of Insight", "Cloak of Winds", "Akashic Form", "Frightful Aspect" };
        static readonly string[] Effects = {
            "For 1 round gain +8 enhancement to the chosen physical ability score, then become fatigued for 1 round.",
            "For 1 round gain +8 enhancement to the chosen mental ability score, then become dazed for 1 round.",
            "Surround the recipient with protective winds, using the provider spell's effects and duration at your monk caster level.",
            "For 24 hours preserve the recipient with the provider spell's life-saving effect. The preparation requires a full-round action outside combat.",
            "Use the native Frightful Aspect transformation and fear aura for 1 minute per monk level. Existing polymorph and fear immunity rules apply."
        };
        static readonly int[] Levels = { 4, 4, 6, 20, 20 }, Costs = { 1, 1, 2, 3, 3 };
        sealed class PublishedRow
        {
            internal readonly Dictionary<BlueprintGuid, BlueprintScriptableObject> Assets;
            internal readonly Dictionary<BlueprintGuid, string> Definitions;
            internal readonly BlueprintFeature[] Roots;
            internal PublishedRow(IEnumerable<BlueprintScriptableObject> assets, BlueprintFeature[] roots) {
                Assets = assets.ToDictionary(b => b.AssetGuid); Roots = roots;
                Definitions = Assets.ToDictionary(p => p.Key, p => Layout(p.Value));
            }
        }
        sealed class Publication { internal readonly Dictionary<int, PublishedRow> Rows = new Dictionary<int, PublishedRow>(); }
        static readonly ConditionalWeakTable<BlueprintsCache, Publication> published = new ConditionalWeakTable<BlueprintsCache, Publication>();
        // Compare our published private graph with its own committed definition,
        // never with a donor that another mod may change later during startup.
        static string Layout(BlueprintScriptableObject bp) {
            var shape = new List<object> { bp.name, bp.GetType().FullName, bp.ComponentsArray };
            if (bp is BlueprintAbility a) shape.AddRange(new object[] { a.Type, a.ActionType, a.Range, a.CanTargetSelf, a.CanTargetFriends,
                a.CanTargetEnemies, a.CanTargetPoint, a.AvailableMetamagic, a.SpellResistance, a.MaterialComponent, Get(a, "m_Parent"), a.IsFullRoundAction });
            if (bp is BlueprintFeature f) shape.AddRange(new object[] { f.Ranks, f.IsClassFeature, f.Groups });
            if (bp is BlueprintBuff b) shape.AddRange(new object[] { b.Stacking, Get(b, "m_Flags") });
            return Migration01426.Definition(shape);
        }
        static IEnumerable<BlueprintReferenceBase> References(object value, HashSet<object> seen) {
            if (value == null) yield break;
            if (value is BlueprintReferenceBase reference) { yield return reference; yield break; }
            var type = value.GetType();
            if (value is SimpleBlueprint || value is UnityEngine.Object || type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is Type || value is Delegate || !seen.Add(value)) yield break;
            if (value is IEnumerable items) {
                foreach (var item in items) foreach (var r in References(item, seen)) yield return r;
                yield break;
            }
            for (var t = type; t != null && t != typeof(object) && t != typeof(BlueprintComponent); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(f => !f.IsStatic))
                    foreach (var r in References(field.GetValue(value), seen)) yield return r;
        }
        static void RestorePublished(BlueprintsCache cache, PublishedRow row) {
            foreach (var pair in row.Assets) {
                var actual = cache.Load(pair.Key);
                if (!ReferenceEquals(actual, pair.Value))
                    throw new InvalidOperationException("Ki power published graph missing or replaced: " + pair.Value.name + " [" + pair.Key + "]");
                var currentDefinition = Layout(pair.Value);
                if (currentDefinition != row.Definitions[pair.Key])
                    throw new InvalidOperationException("Ki power published graph changed: " + pair.Value.name + " [" + pair.Key + "]; " + Migration01426.Difference(row.Definitions[pair.Key], currentDefinition));
                if (pair.Value.ComponentsArray.Any(c => c == null || !ReferenceEquals(c.OwnerBlueprint, pair.Value)))
                    throw new InvalidOperationException("Ki power published component ownership changed: " + pair.Value.name + " [" + pair.Key + "]");
                var references = References(pair.Value.ComponentsArray, new HashSet<object>()).ToList();
                if (pair.Value is BlueprintAbility a) references.Add((BlueprintAbilityReference)Get(a, "m_Parent"));
                foreach (var reference in references) if (reference != null && row.Assets.TryGetValue(reference.Guid, out var expected)) {
                    var cached = typeof(BlueprintReferenceBase).GetProperty("Cached", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(reference, null);
                    if (cached != null && !ReferenceEquals(cached, expected))
                        throw new InvalidOperationException("Ki power private reference identity changed: " + pair.Value.name + " -> " + reference.Guid);
                }
            }
            var pools = PoolIds.Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection ?? throw new InvalidOperationException("Ki power selection missing: " + id)).ToArray();
            var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try { for (int i = 0; i < pools.Length; i++) T09CombatFeats.Append(new[] { pools[i] }, new[] { row.Roots[i] }); }
            catch { for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", before[i][0]); Set(pools[i], "m_AllFeatures", before[i][1]); } throw; }
        }
        internal static void Register(BlueprintsCache cache) {
            var publication = published.GetValue(cache, _ => new Publication());
            lock (publication) {
            for (int i = 0; i < Tokens.Length; i++) {
                if (publication.Rows.TryGetValue(i, out var row)) { RestorePublished(cache, row); continue; }
                if (new[] { "Monk", "ScaledFist" }.Any(mode => cache.Load(Id("Ki" + Tokens[i] + mode)) != null))
                    throw new InvalidOperationException("Ki power unowned or incomplete existing graph: " + Tokens[i]);
                string id = i < 2 ? Id(Tokens[i]).ToString() : i == 2 ? "6fb10fff27b84bff81746194bf8e38a8" : i == 3 ? "58bcb999b3ad46c289dc68cba9c50bd0" : "e788b02f8d21014488067bdd3ba7b325";
                var raw = cache.Load(BlueprintGuid.Parse(id));
                if (raw == null && (i == 2 || i == 3)) continue; // Existing provider owns these spell effects.
                string expected = i < 2 ? "CotWWrathPsychic" + Tokens[i] : i == 2 ? "CloakOfWindsAbility" : i == 3 ? "NewSpell.UseAkashicForm" : "FrightfulAspect";
                if (!(raw is BlueprintAbility spell) || spell.name != expected || spell.Type != AbilityType.Spell)
                    throw new InvalidOperationException("Ki power source missing or changed: " + expected);
                if (i == 2 && !spell.GetComponents<SpellListComponent>().Any()) continue; // TTT keeps a disabled spell blueprint without registering its spell lists.
                publication.Rows.Add(i, RegisterOne(cache, spell, i));
            }
            }
        }
        static AddFeatureIfHasFact Advice(BlueprintUnitFact unlock, BlueprintAbility ability) {
            var grant = new AddFeatureIfHasFact(); Set(grant, "m_CheckedFact", Ref<BlueprintUnitFactReference>(unlock)); Set(grant, "m_Feature", Ref<BlueprintUnitFactReference>(ability)); return grant;
        }
        static IEnumerable<GameAction> ActionsIn(ActionList list) {
            foreach (var a in list?.Actions ?? Array.Empty<GameAction>()) {
                yield return a;
                if (a is Conditional c) foreach (var x in ActionsIn(c.IfTrue).Concat(ActionsIn(c.IfFalse))) yield return x;
                if (a is ContextActionSavingThrow s) foreach (var x in ActionsIn(s.Actions)) yield return x;
                if (a is ContextActionConditionalSaved b) foreach (var x in ActionsIn(b.Succeed).Concat(ActionsIn(b.Failed))) yield return x;
            }
        }
        static PublishedRow RegisterOne(BlueprintsCache cache, BlueprintAbility source, int index) {
            var f = new PsychicAssets(cache); var monk = f.Native<BlueprintCharacterClass>(MonkId);
            var pools = PoolIds.Select(f.Native<BlueprintFeatureSelection>).ToArray();
            var resources = ResourceIds.Select(f.Native<BlueprintAbilityResource>).ToArray();
            var singleUnlock = f.Native<BlueprintFeature>("4356b5d6d34489747bba68d43924a857");
            var massUnlock = f.Native<BlueprintFeature>("1c02ec3a153954b459becc0d40d3cf60");
            var roots = new List<BlueprintFeature>();
            for (int mode = 0; mode < 2; mode++) {
                string token = "Ki" + Tokens[index] + (mode == 0 ? "Monk" : "ScaledFist");
                string description = "Requires monk level " + Levels[index] + ". Spend " + Costs[index] + " ki points to apply " + Titles[index] + " to yourself, using its original action and duration. Caster level equals monk level. " +
                    (index == 2 ? "Requires TabletopTweaks-Base's Cloak of Winds. " : index == 3 ? "Requires PrestigePlus's Akashic Form; its out-of-combat restriction remains. " : "") +
                    "Sensei Mystic Wisdom permits a single ally for 1 additional ki, or allies within 30 feet for 2 additional ki after the mass improvement. " + Effects[index];
                var parameters = Inquisitions01426.Parameters(f, token, new[] { monk }, mode == 0 ? StatType.Wisdom : StatType.Charisma).OfType<ContextCalculateAbilityParams>().Single();
                BlueprintAbility Convert(BlueprintAbility original, string key, int advice, bool child) {
                    var variants = original.GetComponent<AbilityVariants>();
                    if (variants == null && original.GetComponent<AbilityEffectRunAction>() == null)
                        throw new InvalidOperationException("Ki power source has no native action payload: " + original.name);
                    string label = (advice == 0 ? "Ki Power: " : advice == 1 ? "Sensei Advice: " : "Sensei Advice: Mass ") + Titles[index];
                    var a = f.Clone(original, key, label, description); a.Type = AbilityType.SpellLike; a.AvailableMetamagic = 0;
                    if (child) Set(a, "m_DisplayName", Get(original, "m_DisplayName"));
                    a.Range = advice == 1 ? AbilityRange.Close : AbilityRange.Personal;
                    a.CanTargetSelf = true; a.CanTargetFriends = advice != 0; a.CanTargetEnemies = a.CanTargetPoint = false;
                    a.SpellResistance = false; a.MaterialComponent = new BlueprintAbility.MaterialComponentData();
                    Set(a, "m_Parent", new BlueprintAbilityReference());
                    a.ComponentsArray = a.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is SpellComponent) && !(c is Kingmaker.Craft.CraftInfoComponent) && !(c is AbilityResourceLogic) && !(c is ContextCalculateAbilityParams) && !(c is AbilityVariants))
                        .Concat(new BlueprintComponent[] { CopyData(parameters) }).ToArray();
                    if (variants != null) {
                        var children = variants.Variants.Select((v, n) => Convert(v, key + "Option" + n, advice, true)).ToArray();
                        var menu = new AbilityVariants(); Set(menu, "m_Variants", children.Select(Ref<BlueprintAbilityReference>).ToArray());
                        a.ComponentsArray = a.ComponentsArray.Concat(new[] { menu }).ToArray();
                        foreach (var v in children) Set(v, "m_Parent", Ref<BlueprintAbilityReference>(a));
                    } else {
                        var cost = new AbilityResourceLogic { Amount = Costs[index] + advice }; Set(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resources[mode])); Set(cost, "m_IsSpendResource", true);
                        a.ComponentsArray = a.ComponentsArray.Concat(new[] { cost }).ToArray();
                        if (advice != 0) {
                            foreach (var apply in a.GetComponents<AbilityEffectRunAction>().SelectMany(e => ActionsIn(e.Actions)).OfType<ContextActionApplyBuff>()) {
                                apply.ToCaster = false;
                                if (index < 2) {
                                    // Burst aftereffects belong to the recipient, not the advising Sensei.
                                    var buff = f.Clone(apply.Buff, key + "RecipientBuff", label, description);
                                    foreach (var actions in buff.GetComponents<AddFactContextActions>())
                                        foreach (var after in ActionsIn(actions.Deactivated).OfType<ContextActionApplyBuff>()) after.ToCaster = false;
                                    Set(apply, "m_Buff", Ref<BlueprintBuffReference>(buff));
                                }
                            }
                            foreach (var visual in a.GetComponents<AbilitySpawnFx>()) visual.Anchor = AbilitySpawnFxAnchor.SelectedTarget;
                        }
                        if (advice == 2) {
                            var area = new AbilityTargetsAround(); Set(area, "m_Radius", new Feet(30)); Set(area, "m_TargetType", TargetType.Ally); Set(area, "m_Condition", new ConditionsChecker { Conditions = Array.Empty<Condition>() });
                            a.ComponentsArray = a.ComponentsArray.Concat(new[] { area }).ToArray();
                        }
                    }
                    return a;
                }
                var self = Convert(source, token + "Ability", 0, false);
                var single = Convert(source, token + "Advice", 1, false); var mass = Convert(source, token + "AdviceMass", 2, false);
                var level = new PrerequisiteClassLevel { Level = Levels[index] }; Set(level, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(monk));
                var root = f.Feature(token, "Ki Power: " + Titles[index], description, level, Facts(self), Advice(singleUnlock, single), Advice(massUnlock, mass));
                root.IsClassFeature = true; root.Groups = Array.Empty<FeatureGroup>(); Set(root, "m_Icon", Get(source, "m_Icon")); roots.Add(root);
            }
            var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            var created = f.Assets.Values.Where(a => cache.Load(a.AssetGuid) == null).ToArray();
            if (created.Length != f.Assets.Count) throw new InvalidOperationException("Ki power unowned or incomplete private assets: " + Tokens[index]);
            try {
                Migration01426.Commit(f, Array.Empty<BlueprintFeatureSelection>());
                for (int i = 0; i < pools.Length; i++) T09CombatFeats.Append(new[] { pools[i] }, new[] { (BlueprintFeature)cache.Load(roots[i].AssetGuid) });
                return new PublishedRow(f.Assets.Values, roots.ToArray());
            } catch {
                for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", before[i][0]); Set(pools[i], "m_AllFeatures", before[i][1]); }
                foreach (var bp in created) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }
    }
}
