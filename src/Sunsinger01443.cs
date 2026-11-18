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
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Sunsinger01443
    {
        internal const string ModuleName = "Sunsinger";
        internal const string RootToken = "SunsingerArchetype01443", DeityToken = "SunsingerSarenrae01443", ChannelToken = "SunsingerChannelSolarEnergy01443", ResourceToken = "SunsingerChannelResource01443";
        internal const string HealToken = "SunsingerChannelHeal01443", HarmToken = "SunsingerChannelHarm01443", ExtraToken = "SunsingerExtraChannel01443", PillarToken = "SunsingerPillarOfLight01443";
        internal const string ToggleToken = "SunsingerPillarToggle01443", HostToken = "SunsingerPillarHost01443", AreaToken = "SunsingerPillarArea01443", EffectToken = "SunsingerPillarEffect01443", ImmunityToken = "SunsingerPillarImmunity01443";
        internal const string SkaldId = "6afa347d804838b48bda16acb0573dc0", ProgressionId = "26418fed2bc153245972a5b54204ed75", DeitiesId = "59e7a76987fe3b547b9cce045f4db3e4", SarenraeId = "c1c4f7f64842e7e48849e5e67be11a1b";
        internal const string HealId = "f5fc9a1a2a3c1a946a31b320d1dd31b2", HarmId = "279447a6bf2d3544d93a0a39c3b8e91d", FireId = "cc383a9eaae4d2b45a925d442b367b54", ExtraId = "cd9f19775bd9d3343a31a065e93f0c47", SongResourceId = "4a2302c4ec2cfb042bba67d825babfec";
        internal const string KenningId = "d385b8c302e720c43aa17b8170bc6ae2", KenningExtraId = "590d0f09d7da13d4a9382d144b8439f6";
        internal static readonly string[] FascinateIds = { "993908ad3fb81f34ba0ed168b7c61f58", "555930f121b364a4e82670b433028728", "a4fc1c0798359974e99e1d790935501d", "2d4bd347dec7d8648afd502ee40ae661", "a50373fa77d30d34c8c6efb198b36921" };
        internal static readonly string[] PoolIds = { "247a4068296e8be42890143f451b4b45", "d332c1748445e8f4f9e92763123e31bd", "a21acdafc0169f5488a9bd3256e2e65b" };
        internal static readonly string[] NativeIds = new[] { SkaldId, ProgressionId, DeitiesId, SarenraeId, HealId, HarmId, FireId, ExtraId, SongResourceId, KenningId, KenningExtraId, "ddaec3a5845bc7d4191792529b687d65" }.Concat(FascinateIds).Concat(PoolIds).ToArray();
        internal static readonly string[] OwnTokens = { RootToken, DeityToken, ChannelToken, ResourceToken, HealToken, HarmToken, ExtraToken, PillarToken, ToggleToken, HostToken, AreaToken, EffectToken, ImmunityToken };
        const string ChannelText = "At Skald level 5, channel positive energy in a 30-foot burst to heal living creatures or harm undead, using your Skald level as cleric level and Charisma for the save DC. Use once per day, twice at level 11 and three times at level 17. Either mode also outlines enemies in the burst with faerie fire for half your Skald level in rounds. Extra Channel (Sunsinger) grants two additional daily uses.";
        const string PillarText = "At Skald level 6, perform Pillar of Light using your Raging Song rounds. A pillar of sunlight fills a 30-foot radius and fascinates enemies as the native bardic Fascinate performance, using your Skald level and Charisma. A successful Will save or damage grants the native 24-hour immunity. Damage, leaving the area and stopping the performance end its effect. Only one bardic performance can be maintained.";
        sealed class Publication { internal BlueprintScriptableObject[] Assets; internal string[] Definitions; }
        static readonly ConditionalWeakTable<BlueprintsCache, Publication> Published = new ConditionalWeakTable<BlueprintsCache, Publication>();
        static LevelEntry Entry(int level, params BlueprintFeature[] features) { var e = new LevelEntry { Level = level }; Set(e, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return e; }
        static T Clone<T>(PsychicAssets f, T source, string token, string name, string description) where T : BlueprintScriptableObject
        {
            var clone = f.Clone(source, token, name, description);
            // Native JSON populated this list on the donor before OnEnable.
            // Rebuild it from our private components, never inherit or mutate it.
            Set(clone, "m_AllElements", new List<Element>());
            if (clone is BlueprintAbility ability) foreach (string field in new[] { "m_CachedRestrictions", "m_CachedTargetRestrictions", "m_CachedCasterRestrictions", "m_SpellComponent", "m_AbilityIsFullRoundInTurnBased", "m_StickyTouch", "m_AbilityVariants", "m_AbilityShadowSpell", "m_AbilityKineticist", "m_HasVariants", "m_IsCantrip" }) {
                var info = AccessTools.Field(typeof(BlueprintAbility), field) ?? throw new MissingFieldException(typeof(BlueprintAbility).FullName, field);
                Set(ability, field, info.FieldType.IsValueType ? Activator.CreateInstance(info.FieldType) : null);
            }
            return clone;
        }
        // This module validates authored graphs, not Unity load handles or the
        // context-derived nullable AoE cache written by native damage actions.
        // IsAoE, every real link type/id, order and other fields remain checked.
        static string Authored(object value, HashSet<object> path)
        {
            if (value == null) return "null";
            if (value is BlueprintReferenceBase reference) return "ref:" + reference.Guid;
            if (value is SimpleBlueprint blueprint) return "bp:" + blueprint.AssetGuid;
            if (value is WeakResourceLink link) return value.GetType().FullName + ":" + link.AssetId;
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal) return type.FullName + ":" + value;
            if (value is UnityEngine.Object) return type.FullName;
            if (!path.Add(value)) return "cycle:" + type.FullName;
            try {
                if (value is IEnumerable list) return "[" + string.Join(",", list.Cast<object>().Select(x => Authored(x, path))) + "]";
                var fields = new List<FieldInfo>();
                for (var t = type; t != null && t != typeof(object) && t != typeof(BlueprintComponent) && t != typeof(UnityEngine.Object); t = t.BaseType)
                    fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(field => !field.IsStatic &&
                        !(field.DeclaringType == typeof(Condition) && field.Name == "<LastResult>k__BackingField") &&
                        !(field.DeclaringType == typeof(ContextActionDealDamage) && field.Name == "m_IsAOE") &&
                        !(field.DeclaringType == typeof(Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts) && field.Name == "m_FactsMissingCache")));
                return type.FullName + "{" + string.Join(";", fields.OrderBy(field => field.DeclaringType.FullName + field.Name).Select(field => { var v = field.GetValue(value); return field.Name + "=" + (v == null && typeof(BlueprintReferenceBase).IsAssignableFrom(field.FieldType) ? "ref:" + default(BlueprintGuid) : Authored(v, path)); })) + "}";
            } finally { path.Remove(value); }
        }
        static string Definition(BlueprintScriptableObject b)
        {
            object shape;
            if (b is BlueprintArchetype a) shape = new object[] { a.GetParentClass(), a.AddFeatures?.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveFeatures?.Select(e => new object[] { e.Level, Get(e, "m_Features") }).ToArray(), a.RemoveSpellbook, a.ChangeCasterType, a.ReplaceClassSkills };
            else if (b is BlueprintFeature f) shape = new object[] { f.Ranks, f.Groups, f.IsClassFeature };
            else if (b is BlueprintAbility x) shape = new object[] { x.Type, x.ActionType, x.Range, x.CanTargetSelf, x.CanTargetEnemies, x.CanTargetFriends, Get(x, "m_Parent") };
            else if (b is BlueprintActivatableAbility t) shape = new object[] { t.ActivationType, t.Group, Get(t, "m_Buff"), Get(t, "m_ActivateWithUnitCommand"), t.DeactivateIfCombatEnded, t.DeactivateIfOwnerDisabled };
            else if (b is BlueprintAbilityAreaEffect area) shape = new object[] { area.Size, area.Shape, area.AffectEnemies, area.SpellResistance, area.Fx };
            else if (b is BlueprintBuff buff) shape = new object[] { buff.Stacking, buff.Ranks, Get(buff, "m_Flags") };
            else if (b is BlueprintAbilityResource r) shape = new object[] { Get(r, "m_MaxAmount"), Get(r, "m_Min") };
            else throw new InvalidOperationException("Unexpected Sunsinger resource type.");
            return Authored(new object[] { b.name, b.ComponentsArray, shape }, new HashSet<object>());
        }
        static void ValidateOwnership(BlueprintScriptableObject b)
        {
            if (b.ComponentsArray == null || b.ComponentsArray.Any(c => c == null || !ReferenceEquals(c.OwnerBlueprint, b)) || b.ElementsArray.Any(e => e == null || !ReferenceEquals(e.Owner, b)))
                throw new InvalidOperationException("Sunsinger private component/element ownership changed: " + b.name);
        }
        // Only traverse private deep-copied component graphs. Native blueprints,
        // Unity assets, strings and delegates are terminal read-only references.
        static object Rewrite(object value, Dictionary<BlueprintGuid, BlueprintScriptableObject> map, HashSet<object> seen)
        {
            if (value == null) return null;
            if (value is BlueprintReferenceBase reference) {
                if (!map.TryGetValue(reference.Guid, out var target)) return value;
                return typeof(PsychicAssets).GetMethod("Ref", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(value.GetType()).Invoke(null, new object[] { target });
            }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is SimpleBlueprint || value is UnityEngine.Object || value is Type || value is Delegate || !seen.Add(value)) return value;
            if (value is IList list) { for (int i = 0; i < list.Count; i++) list[i] = Rewrite(list[i], map, seen); return value; }
            for (var t = type; t != null && t != typeof(object) && t != typeof(BlueprintComponent); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!field.IsStatic) field.SetValue(value, Rewrite(field.GetValue(value), map, seen));
            return value;
        }
        static void OwnElements(object value, BlueprintScriptableObject owner, HashSet<object> seen)
        {
            if (value == null || value is string || value is SimpleBlueprint || value is BlueprintReferenceBase || value is UnityEngine.Object || value.GetType().IsPrimitive || value.GetType().IsEnum || !seen.Add(value)) return;
            if (value is Element element) { element.Owner = owner; owner.AddToElementsList(element); }
            if (value is IEnumerable list) { foreach (var item in list) OwnElements(item, owner, seen); return; }
            for (var t = value.GetType(); t != null && t != typeof(object) && t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!field.IsStatic) OwnElements(field.GetValue(value), owner, seen);
        }
        static ContextRankConfig Rank(BlueprintCharacterClass cls, AbilityRankType kind, string progression)
        {
            var rank = new ContextRankConfig(); SetEnum(rank, "m_Type", kind.ToString()); SetEnum(rank, "m_BaseValueType", "ClassLevel"); SetEnum(rank, "m_Progression", progression);
            Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(cls) }); return rank;
        }
        static ContextCalculateAbilityParamsBasedOnClass Params(BlueprintCharacterClass cls) { var p = new ContextCalculateAbilityParamsBasedOnClass { StatType = StatType.Charisma }; Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls)); return p; }
        static BlueprintAbility Channel(PsychicAssets f, BlueprintCharacterClass cls, BlueprintAbilityResource resource, bool heal)
        {
            var b = Clone(f, f.Native<BlueprintAbility>(heal ? HealId : HarmId), heal ? HealToken : HarmToken, heal ? "Channel Solar Energy — Heal Living" : "Channel Solar Energy — Harm Undead", ChannelText);
            var originalActions = b.GetComponent<AbilityEffectRunAction>() ?? throw new InvalidOperationException("Native channel actions are missing.");
            var originals = new List<GameAction>();
            void Collect(object o, HashSet<object> seen) {
                if (o == null || o is string || o is SimpleBlueprint || o is BlueprintReferenceBase || o is UnityEngine.Object || o.GetType().IsPrimitive || o.GetType().IsEnum || !seen.Add(o)) return;
                if (o is GameAction action) originals.Add(action);
                if (o is IEnumerable list) { foreach (var item in list) Collect(item, seen); return; }
                for (var t = o.GetType(); t != null && t != typeof(object) && t != typeof(Element); t = t.BaseType) foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) if (!field.IsStatic) Collect(field.GetValue(o), seen);
            }
            Collect(originalActions.Actions, new HashSet<object>());
            // The first native branch is the actual channel operation, including
            // Selective Channel, negative-energy affinity and its native save.
            // Later Cleric-archetype bookkeeping is not a Sunsinger ability.
            var core = originalActions.Actions.Actions.FirstOrDefault() as Conditional;
            var coreActions = new List<GameAction>();
            if (core != null) { var prior = originals; originals = coreActions; Collect(core, new HashSet<object>()); originals = prior; }
            if (core == null || (heal ? !coreActions.OfType<ContextActionHealTarget>().Any() : !coreActions.OfType<ContextActionDealDamage>().Any())) throw new InvalidOperationException("Native channel target/effect branch changed; preserved.");
            var outline = Apply(f.Native<BlueprintBuff>(FireId), 0, fromSpell: false); outline.IsNotDispelable = true; outline.DurationValue.BonusValue = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.StatBonus };
            var around = b.GetComponent<AbilityTargetsAround>() ?? throw new InvalidOperationException("Native channel targets are missing.");
            var nativeTargetCondition = CopyData((ConditionsChecker)Get(around, "m_Condition"));
            Set(around, "m_Condition", new ConditionsChecker { Operation = Operation.And, Conditions = Array.Empty<Condition>() });
            var cost = b.GetComponent<AbilityResourceLogic>() ?? throw new InvalidOperationException("Native channel cost is missing."); Set(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource)); cost.Amount = 1;
            b.ComponentsArray = b.ComponentsArray.Where(c => !(c is AbilityEffectRunAction) && !(c is ContextCalculateAbilityParamsBasedOnClass) && !(c is ContextRankConfig r && Get(r, "m_Type").Equals(AbilityRankType.Default)) && c.GetType().Name != "AbilityUseOnRest").Concat(new BlueprintComponent[] {
                Rank(cls, AbilityRankType.Default, "OnePlusDiv2"), Rank(cls, AbilityRankType.StatBonus, "Div2"), Params(cls),
                Run(new Conditional { ConditionsChecker = nativeTargetCondition, IfTrue = Actions(core), IfFalse = Actions() },
                    new Conditional { ConditionsChecker = new ConditionsChecker { Operation = Operation.And, Conditions = new Condition[] { new ContextConditionIsEnemy() } }, IfTrue = Actions(outline), IfFalse = Actions() })
            }).ToArray();
            Set(b, "m_Parent", new BlueprintAbilityReference()); b.LocalizedDuration = Text(heal ? HealToken : HarmToken, "duration", "Instantaneous; faerie fire lasts half your Skald level in rounds"); return b;
        }
        static void Append(BlueprintCharacterClass cls, BlueprintProgression progression, BlueprintArchetype archetype, BlueprintFeature[] features, BlueprintFeature extra, BlueprintFeatureSelection[] pools)
        {
            var before = (BlueprintArchetypeReference[])Get(cls, "m_Archetypes"); var uiBefore = progression.UIGroups; var determBefore = (BlueprintFeatureBaseReference[])Get(progression, "m_UIDeterminatorsGroup");
            var poolBefore = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try {
                if (before.Count(r => r != null && r.Guid == archetype.AssetGuid) > 1) throw new InvalidOperationException("Sunsinger duplicate native archetype entry.");
                if (!before.Any(r => r != null && r.Guid == archetype.AssetGuid)) Set(cls, "m_Archetypes", before.Concat(new[] { Ref<BlueprintArchetypeReference>(archetype) }).ToArray());
                var ownIds = new HashSet<BlueprintGuid>(features.Select(x => x.AssetGuid)); var ownGroups = (uiBefore ?? Array.Empty<UIGroup>()).Where(g => ((List<BlueprintFeatureBaseReference>)Get(g, "m_Features") ?? new List<BlueprintFeatureBaseReference>()).Any(r => r != null && ownIds.Contains(r.Guid))).ToArray();
                if (ownGroups.Length == 0) { var ui = new UIGroup(); Set(ui, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); progression.UIGroups = (uiBefore ?? Array.Empty<UIGroup>()).Concat(new[] { ui }).ToArray(); }
                else if (ownGroups.Length != 1 || !((List<BlueprintFeatureBaseReference>)Get(ownGroups[0], "m_Features")).Select(r => r?.Guid ?? default(BlueprintGuid)).SequenceEqual(features.Select(x => x.AssetGuid))) throw new InvalidOperationException("Sunsinger native UI group changed.");
                if ((determBefore ?? Array.Empty<BlueprintFeatureBaseReference>()).Count(r => r != null && r.Guid == features[0].AssetGuid) > 1) throw new InvalidOperationException("Sunsinger duplicate native determinator entry.");
                if (!(determBefore ?? Array.Empty<BlueprintFeatureBaseReference>()).Any(r => r != null && r.Guid == features[0].AssetGuid)) Set(progression, "m_UIDeterminatorsGroup", (determBefore ?? Array.Empty<BlueprintFeatureBaseReference>()).Concat(new[] { Ref<BlueprintFeatureBaseReference>(features[0]) }).ToArray());
                foreach (var pool in pools) foreach (string field in new[] { "m_Features", "m_AllFeatures" }) if (((BlueprintFeatureReference[])Get(pool, field) ?? Array.Empty<BlueprintFeatureReference>()).Count(r => r != null && r.Guid == extra.AssetGuid) > 1) throw new InvalidOperationException("Sunsinger duplicate Extra Channel entry.");
                T09CombatFeats.Append(pools, new[] { extra });
            } catch {
                Set(cls, "m_Archetypes", before); progression.UIGroups = uiBefore; Set(progression, "m_UIDeterminatorsGroup", determBefore);
                for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", poolBefore[i][0]); Set(pools[i], "m_AllFeatures", poolBefore[i][1]); } throw;
            }
        }
        static void Attach(BlueprintCharacterClass cls, BlueprintProgression p, BlueprintArchetype a, BlueprintFeature[] features, BlueprintFeature extra, BlueprintFeatureSelection[] pools)
        {
            var before = Get(cls, "m_Archetypes"); var ui = p.UIGroups; var determ = Get(p, "m_UIDeterminatorsGroup"); var previous = pools.Select(x => new[] { Get(x, "m_Features"), Get(x, "m_AllFeatures") }).ToArray();
            try { Append(cls, p, a, features, extra, pools); } catch { Set(cls, "m_Archetypes", before); p.UIGroups = ui; Set(p, "m_UIDeterminatorsGroup", determ); for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", previous[i][0]); Set(pools[i], "m_AllFeatures", previous[i][1]); } throw; }
        }
        internal static void Register(BlueprintsCache cache)
        {
            var state = Published.GetValue(cache, _ => new Publication());
            lock (state) {
                var f = new PsychicAssets(cache); var cls = f.Native<BlueprintCharacterClass>(SkaldId); var progression = f.Native<BlueprintProgression>(ProgressionId);
                if (cls.Progression != progression) throw new InvalidOperationException("Sunsinger requires the actual native Skald progression.");
                var pools = PoolIds.Select(f.Native<BlueprintFeatureSelection>).Where(p => ((BlueprintFeatureReference[])Get(p, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>()).Any(x => x != null && x.Guid == BlueprintGuid.Parse(ExtraId))).ToArray();
                if (!pools.Any(p => p.AssetGuid == BlueprintGuid.Parse(PoolIds[0]))) throw new InvalidOperationException("Sunsinger requires the actual native Extra Channel feat pool.");
                if (cls.Archetypes.Any(a => a != null && a.AssetGuid != Id(RootToken) && (a.name ?? "").Replace("_", "").Replace(" ", "").Replace("-", "").IndexOf("Sunsinger", StringComparison.OrdinalIgnoreCase) >= 0)) throw new InvalidOperationException("An external Sunsinger archetype already exists; preserved.");
                if (state.Assets != null) {
                    for (int i = 0; i < state.Assets.Length; i++) { if (BlueprintRegistration.StrictPublishedGraphChecks) ValidateOwnership(state.Assets[i]); if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) || (BlueprintRegistration.StrictPublishedGraphChecks && Definition(state.Assets[i]) != state.Definitions[i])) throw new InvalidOperationException("Sunsinger published graph changed: " + state.Assets[i].name); }
                    Attach(cls, progression, (BlueprintArchetype)cache.Load(Id(RootToken)), new[] { DeityToken, ChannelToken, PillarToken }.Select(t => (BlueprintFeature)cache.Load(Id(t))).ToArray(), (BlueprintFeature)cache.Load(Id(ExtraToken)), pools); return;
                }
                foreach (var token in OwnTokens) if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Sunsinger private identity collision: " + token);
                var religion = f.Native<BlueprintFeature>(SarenraeId); var deities = f.Native<BlueprintFeatureSelection>(DeitiesId);
                // AddFacts adopts an existing same-blueprint fact and removes it
                // with this source. Native conditional granting avoids taking
                // ownership of religion the character already chose.
                var grantReligion = new AddFeatureIfHasFact { Not = true }; Set(grantReligion, "m_CheckedFact", Ref<BlueprintUnitFactReference>(religion)); Set(grantReligion, "m_Feature", Ref<BlueprintUnitFactReference>(religion));
                var deity = f.Feature(DeityToken, "Sarenrae", "A Sunsinger worships Sarenrae and must be lawful good, neutral good, chaotic good or true neutral.", grantReligion); deity.IsClassFeature = true; Set(deity, "m_Icon", religion.Icon);
                var resource = f.Add(new BlueprintAbilityResource(), ResourceToken, "Channel Solar Energy", ChannelText); var amount = Get(resource, "m_MaxAmount");
                Set(amount, "BaseValue", 1); Set(amount, "IncreasedByLevelStartPlusDivStep", true); Set(amount, "StartingLevel", 11); Set(amount, "StartingIncrease", 1); Set(amount, "LevelStep", 6); Set(amount, "PerStepIncrease", 1); Set(amount, "m_ClassDiv", new[] { Ref<BlueprintCharacterClassReference>(cls) }); Set(amount, "m_ArchetypesDiv", Array.Empty<BlueprintArchetypeReference>()); Set(amount, "m_Class", Array.Empty<BlueprintCharacterClassReference>()); Set(amount, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); Set(resource, "m_MaxAmount", amount); Set(resource, "m_Min", 0);
                resource.LocalizedName = Text(ResourceToken, "name", "Channel Solar Energy"); resource.LocalizedDescription = Text(ResourceToken, "description", ChannelText);
                var heal = Channel(f, cls, resource, true); var harm = Channel(f, cls, resource, false);
                var grant = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(grant, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
                var channel = f.Feature(ChannelToken, "Channel Solar Energy", ChannelText, grant, Facts(heal, harm)); channel.IsClassFeature = true; Set(channel, "m_Icon", heal.Icon);
                var extra = Clone(f, f.Native<BlueprintFeature>(ExtraId), ExtraToken, "Extra Channel (Sunsinger)", "You can channel solar energy two additional times per day. Requires Channel Solar Energy. This feat can be taken multiple times.");
                var increase = extra.GetComponents<IncreaseResourceAmount>().ToArray(); if (increase.Length != 1) throw new InvalidOperationException("Native Extra Channel resource component changed."); Set(increase[0], "m_Resource", Ref<BlueprintAbilityResourceReference>(resource)); increase[0].Value = 2;
                extra.ComponentsArray = extra.ComponentsArray.Where(c => !(c is Prerequisite)).Concat(new BlueprintComponent[] { Migration01426.Require(channel) }).ToArray();
                var toggle = Clone(f, f.Native<BlueprintActivatableAbility>(FascinateIds[0]), ToggleToken, "Pillar of Light", PillarText); var host = Clone(f, f.Native<BlueprintBuff>(FascinateIds[1]), HostToken, "Pillar of Light", PillarText); var area = Clone(f, f.Native<BlueprintAbilityAreaEffect>(FascinateIds[2]), AreaToken, "Pillar of Light", PillarText); var effectBuff = Clone(f, f.Native<BlueprintBuff>(FascinateIds[3]), EffectToken, "Pillar of Light", PillarText); var immune = Clone(f, f.Native<BlueprintBuff>(FascinateIds[4]), ImmunityToken, "Pillar of Light: Immunity", "Immune to this fascinating sunlight performance for 24 hours, as with native Fascinate.");
                if (toggle.GetComponents<ActivatableAbilityResourceLogic>().Count() != 1 || host.GetComponents<Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect>().Count() != 1 || area.GetComponents<ContextCalculateAbilityParamsBasedOnClass>().Count() != 1 || area.GetComponents<AbilityAreaEffectRunAction>().Count() != 1)
                    throw new InvalidOperationException("Native Fascinate component anchors changed; no Sunsinger resources were published.");
                var map = new Dictionary<BlueprintGuid, BlueprintScriptableObject> { [BlueprintGuid.Parse(FascinateIds[0])] = toggle, [BlueprintGuid.Parse(FascinateIds[1])] = host, [BlueprintGuid.Parse(FascinateIds[2])] = area, [BlueprintGuid.Parse(FascinateIds[3])] = effectBuff, [BlueprintGuid.Parse(FascinateIds[4])] = immune, [BlueprintGuid.Parse("e190ba276831b5c4fa28737e5e49e6a6")] = f.Native<BlueprintAbilityResource>(SongResourceId) };
                foreach (var node in new BlueprintScriptableObject[] { toggle, host, area, effectBuff, immune }) foreach (var c in node.ComponentsArray) Rewrite(c, map, new HashSet<object>());
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(host)); foreach (var p in area.GetComponents<ContextCalculateAbilityParamsBasedOnClass>()) Set(p, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls)); area.Fx = new PrefabLink { AssetId = "79cd602c3311fda459f1e7c62d7ec9a1" };
                var enter = area.GetComponent<AbilityAreaEffectRunAction>().UnitEnter.Actions.SingleOrDefault() as Conditional;
                var save = enter?.IfTrue.Actions.SingleOrDefault() as ContextActionSavingThrow; var branch = save?.Actions.Actions.SingleOrDefault() as ContextActionConditionalSaved;
                if (branch == null || branch.Failed.Actions.Length != 1 || !(branch.Failed.Actions[0] is ContextActionApplyBuff)) throw new InvalidOperationException("Native Fascinate save/application path changed; preserved.");
                branch.Failed = Actions(new Conditional { ConditionsChecker = new ConditionsChecker { Conditions = new Condition[] { new MasterpieceNativeStatusAllowed01435 { NativeBuff = Ref<BlueprintBuffReference>(f.Native<BlueprintBuff>(FascinateIds[3])) } } }, IfTrue = branch.Failed, IfFalse = Actions() });
                var pillar = Clone(f, f.Native<BlueprintFeature>("ddaec3a5845bc7d4191792529b687d65"), PillarToken, "Pillar of Light", PillarText); pillar.ComponentsArray = new BlueprintComponent[] { Facts(toggle) }; pillar.IsClassFeature = true;
                var prerequisites = ((BlueprintFeatureReference[])Get(deities, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>()).Where(d => d != null && d.Guid != religion.AssetGuid).GroupBy(d => d.Guid).Select(g => { var p = new PrerequisiteNoFeature(); Set(p, "m_Feature", CopyData(g.First())); return (BlueprintComponent)p; }).Concat(new BlueprintComponent[] { new PrerequisiteAlignment { Alignment = AlignmentMaskType.Good | AlignmentMaskType.TrueNeutral } }).ToArray();
                var archetype = f.Add(new BlueprintArchetype { AddFeatures = new[] { Entry(1, deity), Entry(5, channel), Entry(6, pillar) }, RemoveFeatures = new[] { Entry(5, f.Native<BlueprintFeature>(KenningId)), Entry(11, f.Native<BlueprintFeature>(KenningExtraId)), Entry(17, f.Native<BlueprintFeature>(KenningExtraId)) }, ComponentsArray = prerequisites }, RootToken, "Sunsinger", ""); Set(archetype, "m_ParentClass", cls);
                archetype.LocalizedName = Text(RootToken, "name", "Sunsinger"); archetype.LocalizedDescription = Text(RootToken, "description", "A Skald devoted to Sarenrae. Gains Channel Solar Energy at level 5 and Pillar of Light at level 6. Requires a good or true-neutral alignment and Sarenrae. Replaces Spell Kenning at level 5 and its extra uses at levels 11 and 17 whenever these native features are supplied, including TabletopTweaks. Vanilla Wrath grants neither those features nor Song of Marching; no unrelated Rage Power, Versatile Performance or spellcasting feature is removed.");
                foreach (var bp in f.Assets.Values) foreach (var c in bp.ComponentsArray) OwnElements(c, bp, new HashSet<object>());
                var staged = f.Assets.Values.ToArray();
                try { Migration01426.Commit(f, Array.Empty<BlueprintFeatureSelection>()); foreach (var bp in staged) ValidateOwnership(bp); var definitions = staged.Select(Definition).ToArray(); Attach(cls, progression, archetype, new[] { deity, channel, pillar }, extra, pools); state.Assets = staged; state.Definitions = definitions; }
                catch { foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid); throw; }
            }
        }
    }
}
