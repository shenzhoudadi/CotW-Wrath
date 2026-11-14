// Adapted from Holic75/KingmakerRebalance (MIT); see LICENSE-CotW.txt.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using UnityEngine;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class UrbanSkald01443
    {
        internal const string ModuleName = "Urban Skald", RootToken = "UrbanSkaldArchetype01443";
        internal const string ProficiencyToken = "UrbanSkaldProficiencies01443", ControlledToken = "UrbanSkaldControlledRage01443";
        internal const string CrowdToken = "UrbanSkaldBackOfTheCrowd01443", MockeryToken = "UrbanSkaldInfuriatingMockery01443";
        internal const string SkaldId = "6afa347d804838b48bda16acb0573dc0", ProgressionId = "26418fed2bc153245972a5b54204ed75";
        internal const string RageId = "1a639eadc2c3ed546bc4bb236864cd0c", ProficiencyId = "64e3e3fb96c439b4f8d896339b2f358a";
        internal const string DirgeId = "12a03dce517a43e45b7203c8f8d4e110", DrId = "d9446a35d1401cf418bb9b5e0e199d57";
        internal const string EffectId = "75b3978757908d24aaaecaf2dc209b89", MythicEffectId = "ee51f69e69cb43c3bc7c02002e482d17";
        internal const string MainId = "220e58fee6ab0bf4fab8d1a66b01edd0", AreaId = "ac783feed9dea91469d61ebb5f9d2e97", AbilityId = "264e93ac44ace16488226b8f7756bf26";
        internal const string PenaltyId = "345d36cd45f5614409824209f26d0130", RefusalId = "9fd2dd27d838f0049ab56e9da3508b25";
        internal const string MediumId = "46f4fb320f35704488ba3d513397789d", FrightAreaId = "55c526a79761a3c48a3cc974a09bfef7";
        internal const string MasterId = "ae4d45a39a91dee4fb4200d7a677d9a7", MythicSongId = "69fad80df0d44dfea19aef513db959c2";
        internal static readonly string[] NativeIds = { SkaldId, ProgressionId, RageId, ProficiencyId, DirgeId, DrId, EffectId, MythicEffectId, MainId, AreaId, AbilityId, PenaltyId, RefusalId, MediumId, FrightAreaId, MasterId, MythicSongId, "772c83a25e2268e448e841dcd548235f" };
        internal static readonly string[] Stats = { "Strength", "Dexterity", "Constitution" };
        internal static string Token(string stat, string kind) => "UrbanSkald" + stat + kind + "01443";
        internal static string AllocationToken(string stat, int portion, string kind) => "UrbanSkaldAllocation" + stat + portion + kind + "01442";
        internal static readonly string[] AllocationAbilities = Stats.SelectMany(s => Enumerable.Range(0, 3).Select(n => AllocationToken(s, n, "Ability"))).ToArray();
        // Original CotW uses these real Transmutation physical-enhancement
        // toggles as the STR/DEX/CON icon donors. InspiredRageFeature has no icon.
        internal static readonly string[] AllocationIconIds = { "c7773d1b408fea24dbbb0f7bf3eb864e", "3553bda4d6dfe6344ad89b25f7be939a", "99cf556b967c2074ca284e127d815711" };
        internal static readonly string[] OwnTokens = new[] { RootToken, ProficiencyToken, ControlledToken, CrowdToken, MockeryToken, "UrbanSkaldNoCasting01443", "UrbanSkaldMockeryBuff01443", "UrbanSkaldMockeryArea01443", "UrbanSkaldMockeryMain01443", "UrbanSkaldMockeryAbility01443" }
            .Concat(Stats.SelectMany(s => new[] { "Effect", "MythicEffect", "Area", "Main", "Ability" }.Select(k => Token(s, k))))
            .Concat(Stats.SelectMany(s => Enumerable.Range(0, 3).SelectMany(n => new[] { "Ability", "Buff", "Point" }.Select(k => AllocationToken(s, n, k))))).ToArray();
        const string ControlledDescription = "Use the persistent Allocation toggles to distribute a total +2 morale bonus among Strength, Dexterity and Constitution, in increments of 2. The available total increases to +4 at Skald level 8 and +6 at 16. Then activate this single Controlled Inspired Rage performance. Each selected toggle contributes +2; turn one off before reallocating a fully assigned budget. Accepting allies can cast normally. No Inspired Rage Will bonus or AC penalty. Native rage powers, acceptance, resource and mythic song improvements are retained.";
        const string CrowdDescription = "While adjacent to at least two living allies (within 7 feet), gain +1 dodge AC at Skald level 3, +2 at 9 and +3 at 15. The benefit ends as soon as fewer than two allies are adjacent.";
        const string MockeryDescription = "At Skald level 10, perform to infuriate enemies within 30 feet. Will negates (DC 10 + half your Skald level + Charisma modifier). Affected enemies take -2 AC and attack and must pass concentration checks to cast (DC 15 + spell level). The effect ends when the enemy leaves or the song ends. This is a mind-affecting emotion effect; successful saves do not grant a new 24-hour immunity.";
        sealed class Publication { internal BlueprintScriptableObject[] Assets; internal string[] Definitions; }
        static readonly ConditionalWeakTable<BlueprintsCache, Publication> Published = new ConditionalWeakTable<BlueprintsCache, Publication>();
        static LevelEntry Entry(int level, params BlueprintFeature[] features) { var e = new LevelEntry { Level = level }; Set(e, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e; }
        static string Definition(BlueprintScriptableObject b)
        {
            object Entries(LevelEntry[] values) => (values ?? Array.Empty<LevelEntry>()).Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray();
            object data = b is BlueprintArchetype a ? (object)new object[] { a.GetParentClass(), Entries(a.AddFeatures), Entries(a.RemoveFeatures), a.ReplaceStartingEquipment, Get(a, "m_StartingItems") }
                : b is BlueprintActivatableAbility ability ? new object[] { ability.Buff, ability.Group, ability.ActivationType, ability.WeightInGroup }
                : b is BlueprintBuff buff ? new object[] { buff.Stacking, buff.Ranks }
                : b is BlueprintAbilityAreaEffect area ? new object[] { area.Size, area.Shape, area.SpellResistance, area.AffectDead }
                : b is BlueprintFeature feature ? new object[] { feature.Groups, feature.Ranks } : null;
            return Migration01426.Definition(new object[] { b.name, b.ComponentsArray, data });
        }
        // Rewrite only already deep-copied component data. Native blueprints and
        // resource links are references, never traversed or mutated.
        static void Rewrite(object value, IDictionary<BlueprintGuid, BlueprintScriptableObject> map, HashSet<object> seen = null)
        {
            if (value == null) return;
            if (value is BlueprintReferenceBase reference) {
                if (map.TryGetValue(reference.Guid, out var target)) { Set(reference, "deserializedGuid", target.AssetGuid); AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(reference, target, null); }
                return;
            }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is SimpleBlueprint || value is UnityEngine.Object || value is Type || value is Delegate) return;
            seen = seen ?? new HashSet<object>(Identity.Instance); if (!seen.Add(value)) return;
            if (value is IEnumerable list) { foreach (var item in list) Rewrite(item, map, seen); return; }
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!field.IsStatic) Rewrite(field.GetValue(value), map, seen);
        }
        sealed class Identity : IEqualityComparer<object> { internal static readonly Identity Instance = new Identity(); public new bool Equals(object x, object y) => ReferenceEquals(x, y); public int GetHashCode(object x) => RuntimeHelpers.GetHashCode(x); }
        static IEnumerable<Element> Elements(object value, HashSet<object> seen = null)
        {
            if (value == null || value is string || value is SimpleBlueprint || value is BlueprintReferenceBase || value is UnityEngine.Object || value.GetType().IsPrimitive || value.GetType().IsEnum) yield break;
            seen = seen ?? new HashSet<object>(Identity.Instance); if (!seen.Add(value)) yield break;
            if (value is Element element) yield return element;
            if (value is IEnumerable list) { foreach (var item in list) foreach (var e in Elements(item, seen)) yield return e; yield break; }
            for (var current = value.GetType(); current != null && current != typeof(object); current = current.BaseType)
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!field.IsStatic) foreach (var e in Elements(field.GetValue(value), seen)) yield return e;
        }
        static void PrepareElements(BlueprintScriptableObject bp)
        {
            // MemberwiseClone inherits the donor's deserialized element list.
            // Give this module its own list before OnEnable can reassign Owner.
            Set(bp, "m_AllElements", new List<Element>());
            foreach (var element in Elements(bp.ComponentsArray)) bp.AddToElementsList(element);
        }
        static void ValidateOwners(BlueprintScriptableObject bp)
        {
            if (bp.ComponentsArray == null || bp.ComponentsArray.Any(c => c == null || !ReferenceEquals(c.OwnerBlueprint, bp)) || bp.ElementsArray == null || bp.ElementsArray.Any(e => e == null || !ReferenceEquals(e.Owner, bp)))
                throw new InvalidOperationException("Urban Skald private component/element ownership changed: " + bp.name);
        }
        static ActionList StripCastingPenalty(ActionList list, BlueprintBuff noCasting)
        {
            return Actions((list.Actions ?? Array.Empty<GameAction>()).Select(action => {
                if (action is Conditional condition) { condition.IfTrue = StripCastingPenalty(condition.IfTrue, noCasting); condition.IfFalse = StripCastingPenalty(condition.IfFalse, noCasting); }
                if (action is ContextActionApplyBuff apply && apply.Buff == noCasting)
                    return (GameAction)null;
                return action;
            }).Where(a => a != null).ToArray());
        }
        static BlueprintBuff Effect(PsychicAssets f, BlueprintBuff source, string token, string stat, BlueprintBuff noCasting)
        {
            var effect = f.Clone(source, token, "Controlled Inspired Rage", ControlledDescription);
            var nativeBonus = effect.GetComponents<AttackTypeAttackBonus>().ToArray();
            if (nativeBonus.Length != 1 || effect.GetComponents<AddFactsFromCaster>().Count() != 1 || !effect.GetComponents<AddFactContextActions>().Any()) throw new InvalidOperationException("Urban Skald native rage sharing/bonus layout changed.");
            var rankTypes = new[] { AbilityRankType.DamageDiceAlternative, AbilityRankType.SpeedBonus, AbilityRankType.DamageBonus };
            if (effect.GetComponents<ContextRankConfig>().Any(c => rankTypes.Contains((AbilityRankType)Get(c, "m_Type")))) throw new InvalidOperationException("Urban Skald allocation rank slots changed in the native effect.");
            var additions = new List<BlueprintComponent>();
            for (int i = 0; i < Stats.Length; i++) {
                var rank = new ContextRankConfig(); Set(rank, "m_Type", rankTypes[i]); SetEnum(rank, "m_BaseValueType", "FeatureList"); SetEnum(rank, "m_Progression", "AsIs");
                Set(rank, "m_FeatureList", Enumerable.Range(0, 3).Select(n => Ref<BlueprintFeatureReference>(f.Assets[AllocationToken(Stats[i], n, "Point")])).ToArray());
                additions.Add(rank); additions.Add(new AddContextStatBonus { Stat = (StatType)Enum.Parse(typeof(StatType), Stats[i]), Descriptor = ModifierDescriptor.Morale, Multiplier = 2, Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = rankTypes[i] } });
            }
            var recalc = new RecalculateOnFactsChangeOnAnyPartyUnit(); Set(recalc, "m_CheckedFacts", Stats.SelectMany(s => Enumerable.Range(0, 3).Select(n => Ref<BlueprintUnitFactReference>(f.Assets[AllocationToken(s, n, "Point")]))).ToArray()); additions.Add(recalc);
            effect.ComponentsArray = effect.ComponentsArray.Where(c => !(c is AttackTypeAttackBonus) && !(c is WeaponAttackTypeDamageBonus) && !(c is WeaponGroupDamageBonus) && !(c is TemporaryHitPointsPerLevel) && !(c is AddContextStatBonus)).Concat(additions).ToArray();
            Rewrite(effect.ComponentsArray, new Dictionary<BlueprintGuid, BlueprintScriptableObject> { [BlueprintGuid.Parse(PenaltyId)] = noCasting });
            foreach (var actions in effect.GetComponents<AddFactContextActions>()) actions.Activated = StripCastingPenalty(actions.Activated, noCasting);
            var descriptor = effect.GetComponent<SpellDescriptorComponent>();
            if (descriptor == null) effect.ComponentsArray = effect.ComponentsArray.Concat(new BlueprintComponent[] { new SpellDescriptorComponent { Descriptor = SpellDescriptor.MindAffecting | SpellDescriptor.Emotion } }).ToArray();
            else descriptor.Descriptor |= SpellDescriptor.MindAffecting | SpellDescriptor.Emotion;
            return effect;
        }
        static ContextActionRemoveBuff Remove(BlueprintBuff buff, bool fromCaster = true) { var action = new ContextActionRemoveBuff { OnlyFromCaster = fromCaster }; Set(action, "m_Buff", Ref<BlueprintBuffReference>(buff)); return action; }
        static void Append(BlueprintCharacterClass cls, BlueprintProgression progression, BlueprintArchetype archetype, BlueprintFeature[] ui, BlueprintBuff refusal, BlueprintBuff[] effects)
        {
            var originalArchetypes = (BlueprintArchetypeReference[])Get(cls, "m_Archetypes"); var oldUI = progression.UIGroups; var oldDet = (BlueprintFeatureBaseReference[])Get(progression, "m_UIDeterminatorsGroup");
            var handler = refusal.GetComponents<AddFactContextActions>().SingleOrDefault() ?? throw new InvalidOperationException("Urban Skald requires native accept/refuse lifecycle."); var oldActions = handler.Deactivated;
            try {
                if (!(originalArchetypes ?? Array.Empty<BlueprintArchetypeReference>()).Any(r => r.Guid == archetype.AssetGuid)) Set(cls, "m_Archetypes", (originalArchetypes ?? Array.Empty<BlueprintArchetypeReference>()).Concat(new[] { Ref<BlueprintArchetypeReference>(archetype) }).ToArray());
                var groups = (oldUI ?? Array.Empty<UIGroup>()).Where(g => g.Features.Any(ui.Contains)).ToArray();
                if (groups.Length == 0) { var group = new UIGroup(); Set(group, "m_Features", ui.Select(Ref<BlueprintFeatureBaseReference>).ToList()); progression.UIGroups = (oldUI ?? Array.Empty<UIGroup>()).Concat(new[] { group }).ToArray(); }
                else if (groups.Length != 1 || !groups[0].Features.SequenceEqual(ui)) throw new InvalidOperationException("Urban Skald UI group changed.");
                var missingDet = ui.Take(2).Where(f => !(oldDet ?? Array.Empty<BlueprintFeatureBaseReference>()).Any(r => r.Guid == f.AssetGuid)).Select(Ref<BlueprintFeatureBaseReference>).ToArray();
                if (missingDet.Length != 0) Set(progression, "m_UIDeterminatorsGroup", (oldDet ?? Array.Empty<BlueprintFeatureBaseReference>()).Concat(missingDet).ToArray());
                var actions = oldActions.Actions ?? Array.Empty<GameAction>();
                foreach (var effect in effects) {
                    string name = "UrbanSkaldRefuse" + effect.AssetGuid; var found = actions.Where(a => a?.name == name).ToArray();
                    if (found.Length > 1 || found.Length == 1 && (!(found[0] is ContextActionRemoveBuff r) || r.Buff != effect || r.OnlyFromCaster)) throw new InvalidOperationException("Urban Skald refusal cleanup changed.");
                    if (found.Length == 0) { var remove = Remove(effect, false); remove.name = name; remove.Owner = refusal; actions = actions.Concat(new GameAction[] { remove }).ToArray(); }
                }
                if (!ReferenceEquals(actions, oldActions.Actions)) handler.Deactivated = Actions(actions);
            } catch { Set(cls, "m_Archetypes", originalArchetypes); progression.UIGroups = oldUI; Set(progression, "m_UIDeterminatorsGroup", oldDet); handler.Deactivated = oldActions; throw; }
        }
        // Caller-level rollback also encloses exceptions thrown by Harmony
        // postfixes after Append has successfully returned on a published graph.
        static void Attach(BlueprintCharacterClass cls, BlueprintProgression progression, BlueprintArchetype archetype, BlueprintFeature[] ui, BlueprintBuff refusal, BlueprintBuff[] effects)
        {
            var originalArchetypes = Get(cls, "m_Archetypes"); var originalUI = progression.UIGroups; var originalDet = Get(progression, "m_UIDeterminatorsGroup");
            var handler = refusal.GetComponents<AddFactContextActions>().Single(); var originalActions = handler.Deactivated;
            try { Append(cls, progression, archetype, ui, refusal, effects); }
            catch { Set(cls, "m_Archetypes", originalArchetypes); progression.UIGroups = originalUI; Set(progression, "m_UIDeterminatorsGroup", originalDet); handler.Deactivated = originalActions; throw; }
        }
        internal static void Register(BlueprintsCache cache)
        {
            var state = Published.GetValue(cache, _ => new Publication()); lock (state) {
                var f = new PsychicAssets(cache); var cls = f.Native<BlueprintCharacterClass>(SkaldId); var progression = f.Native<BlueprintProgression>(ProgressionId);
                var rage = f.Native<BlueprintFeature>(RageId); var profSource = f.Native<BlueprintFeature>(ProficiencyId); var dirge = f.Native<BlueprintFeature>(DirgeId); var dr = f.Native<BlueprintFeature>(DrId);
                var refusal = f.Native<BlueprintBuff>(RefusalId); var effectSource = f.Native<BlueprintBuff>(EffectId); var mythicSource = f.Native<BlueprintBuff>(MythicEffectId);
                if (cls.Progression != progression || !progression.LevelEntries.Any(e => e.Level == 1 && e.Features.Contains(rage) && e.Features.Contains(profSource)) || !progression.LevelEntries.Any(e => e.Level == 10 && e.Features.Contains(dirge)) || new[] { 9, 14, 19 }.Any(n => !progression.LevelEntries.Any(e => e.Level == n && e.Features.Contains(dr)))) throw new InvalidOperationException("Urban Skald requires the actual native Skald progression.");
                if (cls.Archetypes.Any(a => a != null && a.AssetGuid != Id(RootToken) && (a.name ?? "").Replace("_", "").Replace(" ", "").Replace("-", "").IndexOf("UrbanSkald", StringComparison.OrdinalIgnoreCase) >= 0)) throw new InvalidOperationException("External Urban Skald archetype already exists; preserved.");
                if (state.Assets != null) {
                    for (int i = 0; i < state.Assets.Length; i++) { if (BlueprintRegistration.StrictPublishedGraphChecks) ValidateOwners(state.Assets[i]); if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) || (BlueprintRegistration.StrictPublishedGraphChecks && Definition(state.Assets[i]) != state.Definitions[i])) throw new InvalidOperationException("Urban Skald private graph changed: " + state.Assets[i].name); }
                    Attach(cls, progression, (BlueprintArchetype)cache.Load(Id(RootToken)), new[] { ProficiencyToken, ControlledToken, CrowdToken, MockeryToken }.Select(t => (BlueprintFeature)cache.Load(Id(t))).ToArray(), refusal, Stats.SelectMany(s => new[] { "Effect", "MythicEffect" }.Select(k => (BlueprintBuff)cache.Load(Id(Token(s, k))))).ToArray()); return;
                }
                foreach (var token in OwnTokens) if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Urban Skald private identity collision: " + token);
                var prof = f.Clone(profSource, ProficiencyToken, "Urban Skald Proficiencies", "Retain native Skald weapon, light armor and shield proficiency and armored casting, but not medium armor proficiency or medium-armored casting.");
                var grant = prof.GetComponents<AddFacts>().Single(); var facts = (BlueprintUnitFactReference[])Get(grant, "m_Facts");
                if (facts.Count(r => r.Guid == BlueprintGuid.Parse(MediumId)) != 1) throw new InvalidOperationException("Urban Skald medium armor proficiency anchor changed.");
                Set(grant, "m_Facts", facts.Where(r => r.Guid != BlueprintGuid.Parse(MediumId)).ToArray());
                foreach (var armor in prof.GetComponents<ArcaneArmorProficiency>()) armor.Armor = armor.Armor.Where(a => a != Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.Medium).ToArray();
                // Preserve the old component name/type for native save receipt
                // reconciliation. Disabled prevents new activation; the native
                // ApplyPostLoadFixes path deactivates an old active receipt and
                // balances its own condition count before disposing it.
                var noCasting = f.Clone(f.Native<BlueprintBuff>(PenaltyId), "UrbanSkaldNoCasting01443", "Controlled Inspired Rage", "Accepting allies can cast normally; no AC penalty.");
                noCasting.ComponentsArray = noCasting.ComponentsArray.OfType<ForbidSpellCasting>().Cast<BlueprintComponent>().ToArray();
                if (noCasting.ComponentsArray.Length != 1) throw new InvalidOperationException("Urban Skald native casting restriction receipt changed.");
                var oldForbid = noCasting.ComponentsArray[0]; var retired = new UrbanSkaldRetiredCasting01442();
                for (var type = typeof(ForbidSpellCasting); type != null && type != typeof(object); type = type.BaseType)
                    foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!field.IsStatic && !field.IsInitOnly) field.SetValue(retired, CopyData(field.GetValue(oldForbid)));
                retired.Disabled = true; noCasting.ComponentsArray = new BlueprintComponent[] { retired };
                var allocations = new List<BlueprintActivatableAbility>();
                foreach (string attribute in Stats) foreach (int portion in Enumerable.Range(0, 3)) {
                    string label = "Allocation: " + attribute + " +2 (" + (portion + 1) + ")";
                    var point = f.Feature(AllocationToken(attribute, portion, "Point"), label, "One allocated 2-point contribution."); point.HideInUI = point.HideInCharacterSheetAndLevelUp = true;
                    var marker = f.Buff(AllocationToken(attribute, portion, "Buff"), label, ControlledDescription, Facts(point)); marker.Stacking = StackingType.Replace; SetEnum(marker, "m_Flags", "HiddenInUi");
                    var toggle = f.Add(new BlueprintActivatableAbility { Group = ActivatableAbilityGroup.None, ActivationType = AbilityActivationType.Immediately, DeactivateIfCombatEnded = false, DeactivateIfOwnerDisabled = false, DeactivateIfOwnerUnconscious = false, DoNotTurnOffOnRest = true, IsOnByDefault = attribute == "Strength" && portion == 0 }, AllocationToken(attribute, portion, "Ability"), label, ControlledDescription);
                    Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(marker)); Set(toggle, "m_Icon", Get(f.Native<BlueprintActivatableAbility>(AllocationIconIds[Array.IndexOf(Stats, attribute)]), "m_Icon")); toggle.ComponentsArray = new BlueprintComponent[] { new UrbanSkaldAllocationRestriction01442 { Portion = portion } }; allocations.Add(toggle);
                }
                var abilities = new List<BlueprintActivatableAbility>(); var effects = new List<BlueprintBuff>();
                foreach (string stat in Stats) {
                    var normal = Effect(f, effectSource, Token(stat, "Effect"), stat, noCasting); var mythic = Effect(f, mythicSource, Token(stat, "MythicEffect"), stat, noCasting); effects.Add(normal); effects.Add(mythic);
                    var area = f.Clone(f.Native<BlueprintAbilityAreaEffect>(AreaId), Token(stat, "Area"), "Controlled Inspired Rage (" + stat + ")", ControlledDescription);
                    var main = f.Clone(f.Native<BlueprintBuff>(MainId), Token(stat, "Main"), "Controlled Inspired Rage (" + stat + ")", ControlledDescription);
                    var ability = f.Clone(f.Native<BlueprintActivatableAbility>(AbilityId), Token(stat, "Ability"), "Controlled Inspired Rage (" + stat + ")", ControlledDescription);
                    var map = new Dictionary<BlueprintGuid, BlueprintScriptableObject> { [BlueprintGuid.Parse(EffectId)] = normal, [BlueprintGuid.Parse(MythicEffectId)] = mythic, [BlueprintGuid.Parse(AreaId)] = area, [BlueprintGuid.Parse(MainId)] = main };
                    Rewrite(area.ComponentsArray, map); Rewrite(main.ComponentsArray, map); Set(ability, "m_Buff", Ref<BlueprintBuffReference>(main)); abilities.Add(ability);
                    if (stat == "Strength") { Set(ability, "m_DisplayName", Text(Token(stat, "Ability"), "name", "Controlled Inspired Rage")); }
                    else ability.HiddenInUI = true; // old saved references remain resolvable; native AddFacts post-load reconciliation removes obsolete owned grants.
                    var privateExits = area.GetComponents<AbilityAreaEffectRunAction>().SelectMany(c => c.UnitExit.Actions ?? Array.Empty<GameAction>()).OfType<ContextActionRemoveBuff>().Where(a => a.Buff == normal || a.Buff == mythic).ToArray();
                    if (privateExits.Length != 2) throw new InvalidOperationException("Urban Skald requires native normal/mythic song exit actions.");
                    foreach (var exit in privateExits) exit.OnlyFromCaster = true;
                }
                var controlled = f.Feature(ControlledToken, "Controlled Inspired Rage", ControlledDescription, Facts(new Kingmaker.Blueprints.Facts.BlueprintUnitFact[] { abilities[0] }.Concat(allocations).ToArray())); Set(controlled, "m_Icon", rage.Icon);
                var crowd = f.Feature(CrowdToken, "Back of the Crowd", CrowdDescription, new UrbanSkaldCrowd01443()); Set(crowd, "m_Icon", prof.Icon);
                var mockBuff = f.Buff("UrbanSkaldMockeryBuff01443", "Infuriating Mockery", MockeryDescription,
                    new AddStatBonus { Stat = StatType.AC, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable },
                    new AddStatBonus { Stat = StatType.AdditionalAttackBonus, Value = -2, Descriptor = ModifierDescriptor.UntypedStackable },
                    new AddCondition { Condition = UnitCondition.SpellCastingIsDifficult }, new SpellDescriptorComponent { Descriptor = SpellDescriptor.MindAffecting | SpellDescriptor.Emotion });
                mockBuff.Stacking = StackingType.Replace;
                mockBuff.FxOnStart=CopyData(f.Native<BlueprintBuff>("4b1f07a71a982824988d7f48cd49f3f8").FxOnStart);
                SetEnum(mockBuff, "m_Flags", "Harmful"); Set(mockBuff, "m_Icon", dirge.Icon);
                var mockArea = f.Clone(f.Native<BlueprintAbilityAreaEffect>(FrightAreaId), "UrbanSkaldMockeryArea01443", "Infuriating Mockery", MockeryDescription);
                var enter = new Conditional { ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { new ContextConditionIsEnemy() } }, IfFalse = Actions() };
                var apply = Apply(mockBuff, 0, fromSpell: false); apply.Permanent = true; apply.AsChild = true; apply.IsNotDispelable = true;
                enter.IfTrue = Actions(new ContextActionSavingThrow { Type = SavingThrowType.Will, Actions = Actions(Saved(Array.Empty<GameAction>(), new GameAction[] { apply })) });
                var classParams = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma }; Set(classParams, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls));
                mockArea.ComponentsArray = new BlueprintComponent[] { new AbilityAreaEffectRunAction { UnitEnter = Actions(enter), UnitExit = Actions(Remove(mockBuff)), UnitMove = Actions(), Round = Actions() }, classParams, new SpellDescriptorComponent { Descriptor = SpellDescriptor.MindAffecting | SpellDescriptor.Emotion } };
                var mockMain = f.Clone(f.Native<BlueprintBuff>(MainId), "UrbanSkaldMockeryMain01443", "Infuriating Mockery", MockeryDescription);
                var addArea = new AddAreaEffect(); Set(addArea, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(mockArea)); mockMain.ComponentsArray = new BlueprintComponent[] { addArea };
                var mockAbility = f.Clone(f.Native<BlueprintActivatableAbility>(AbilityId), "UrbanSkaldMockeryAbility01443", "Infuriating Mockery", MockeryDescription); Set(mockAbility, "m_Buff", Ref<BlueprintBuffReference>(mockMain));
                var mockery = f.Feature(MockeryToken, "Infuriating Mockery", MockeryDescription, Facts(mockAbility)); Set(mockery, "m_Icon", dirge.Icon);
                var archetype = f.Add(new BlueprintArchetype { AddFeatures = new[] { Entry(1, prof, controlled), Entry(3, crowd), Entry(10, mockery) }, RemoveFeatures = new[] { Entry(1, profSource, rage), Entry(9, dr), Entry(10, dirge), Entry(14, dr), Entry(19, dr) } }, RootToken, "Urban Skald", "");
                archetype.ReplaceStartingEquipment = true; Set(archetype, "m_StartingItems", CopyData(Get(f.Native<BlueprintCharacterClass>("772c83a25e2268e448e841dcd548235f"), "m_StartingItems")));
                Set(archetype, "m_ParentClass", cls); archetype.LocalizedName = Text(RootToken, "name", "Urban Skald"); archetype.LocalizedDescription = Text(RootToken, "description", "A city-trained Skald with controlled inspired rage, Back of the Crowd and Infuriating Mockery. Replaces medium armor proficiency, Inspired Rage, Dirge of Doom and Skald damage reduction. Wrath has no Song of Marching to remove; all other native features and spellcasting remain unchanged.");
                var staged = f.Assets.Values.ToArray(); foreach (var blueprint in staged) PrepareElements(blueprint);
                var oldArchetypes = Get(cls, "m_Archetypes"); var oldUI = progression.UIGroups; var oldDet = Get(progression, "m_UIDeterminatorsGroup"); var refusalActions = refusal.GetComponents<AddFactContextActions>().Single().Deactivated;
                try {
                    Migration01426.Commit(f, Array.Empty<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>());
                    foreach (var blueprint in staged) ValidateOwners(blueprint);
                    var definitions = staged.Select(Definition).ToArray(); Attach(cls, progression, archetype, new[] { prof, controlled, crowd, mockery }, refusal, effects.ToArray()); state.Assets = staged; state.Definitions = definitions;
                } catch { Set(cls, "m_Archetypes", oldArchetypes); progression.UIGroups = oldUI; Set(progression, "m_UIDeterminatorsGroup", oldDet); refusal.GetComponents<AddFactContextActions>().Single().Deactivated = refusalActions; foreach (var b in staged) if (ReferenceEquals(cache.Load(b.AssetGuid), b)) cache.RemoveCachedBlueprint(b.AssetGuid); throw; }
            }
        }
    }

    // Native OnTurnOn skips both counters for IgnoreFeature, while its old
    // OnTurnOff does not repeat that guard. The retired PRIVATE template keeps
    // the same component name and EmptyComponentData runtime schema so native
    // saved-receipt rebinding works, and balances only a source that contributed.
    [TypeId("346bf0488d9a45e8956d7e3a620b167a")]
    public sealed class UrbanSkaldRetiredCasting01442 : ForbidSpellCasting
    {
        protected override void OnTurnOff()
        {
            var unit = Owner; if (unit == null) return;
            var ignore = IgnoreFeature; if (ignore != null && unit.HasFact(ignore)) return;
            base.OnTurnOff();
        }
    }

    // A private availability rule avoids borrowing a native/other-mod group or
    // extending the engine's fixed enum-sized group array. Selection state,
    // markers, rank calculation, bonuses and save reconciliation remain native.
    [TypeId("0d74cba56cce4a7098e8ebf5a2abe677")]
    public sealed class UrbanSkaldAllocationRestriction01442 : Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestriction
    {
        public int Portion;
        protected override bool IsAvailable()
        {
            var unit = Owner; if (unit == null || unit.Descriptor == null) return false;
            var cls = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(UrbanSkald01443.SkaldId); if (cls == null) return false;
            int level = unit.Descriptor.Progression.GetClassLevel(cls), budget = level >= 16 ? 3 : level >= 8 ? 2 : level >= 1 ? 1 : 0;
            if (Portion < 0 || Portion >= budget) return false;
            var ids = new HashSet<BlueprintGuid>(UrbanSkald01443.AllocationAbilities.Select(PsychicAssets.Id));
            var active = unit.ActivatableAbilities.RawFacts.Where(a => ids.Contains(a.Blueprint.AssetGuid) && a.IsOn).ToArray();
            var current = Fact as ActivatableAbility;
            if (current != null && current.IsOn) return active.OrderBy(a => a.Blueprint.AssetGuid.ToString()).Take(budget).Contains(current);
            return active.Length < budget;
        }
    }

    // Calculated on an AC rule, not by a movement/Update loop. The native unit
    // enumeration stops at the second live nearby ally and adds no persistent buffs.
    [TypeId("1808b74130144c0fb02fd40fd26f2e3c")]
    public sealed class UrbanSkaldCrowd01443 : UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateAC>
    {
        internal static bool EnoughAllies(UnitEntityData owner)
        {
            if (owner == null) return false;
            int count = 0; const float radius = 2.1336f;
            foreach (var unit in Kingmaker.Designers.GameHelper.GetTargetsAround(owner.Position, radius, false, false))
                if (unit != null && unit != owner && !unit.State.IsDead && owner.IsAlly(unit) && (unit.Position - owner.Position).sqrMagnitude <= radius * radius && ++count >= 2) return true;
            return false;
        }
        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            var unit = Owner; if (unit == null || evt.Target != unit || !EnoughAllies(unit)) return;
            int level = unit.Descriptor.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(UrbanSkald01443.SkaldId));
            if (level >= 3) evt.AddModifier(Math.Min(3, 1 + (level - 3) / 6), Fact, ModifierDescriptor.Dodge);
        }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
    }
}
