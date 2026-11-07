// Havocker, K01-HAVOCKER-20261106-r1. Adapted from Holic75/KingmakerRebalance
// (MIT), Classes/Witch.cs. Native mechanics remain owned by Owlcat.
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
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Havocker0020
    {
        internal const string ModuleName = "Witch archetype: Havocker";
        internal const string RootToken = "HavockerWitchArchetype0020", ElementToken = "PatronElementFeatureSelection0020";
        internal const string InfusionId = "58d6f8e9eea63f6418b107ce64f315ea", InfusionToken = "HavockerInfusionSelectionFeature0020";
        internal const string SpecializationToken = "InfusionSpecializationHavockerFeature0020";
        internal static readonly string[] Elements = { "Air", "Earth", "Fire", "Water" };
        // Native Exploding Arrows leaves have no parameter calculator and fall
        // back to generic character-level/Cha parameters. Q-07 requires the
        // same native Witch/kinetic-main-stat calculator as our other blasts.
        // This finite list only configures already-staged private leaves; it
        // grants no Archer feature/element and never changes their donors.
        internal static readonly string[] ExplodingArrowsIds = {
            "fea5c544704a4dd29a654316c52982c0", "d89f7e8024bd4ba3be8c42923c43fe1f",
            "3d1a23a5a01b40d9a756e38816e294b7", "9b3bf9206bf9451d81c00d96ef7db7ee",
            "3219a6f0969747ab870e3828b32e2e0b", "35d2f23a727a4e358ea230525c6afd9d",
            "d9cccdb1f9354014957176447145bc62", "7707d4b8ed864757bf019dc86286df7b",
            "bdcf5efe1d714e46886c2a5eb9012167", "f922cbd782f648e5928c6866af0c5809",
            "94ef0e254f814fd4ade9e168b7aeef4f", "24ab8ee216d947af9d79d1f3c08e3041",
            "03d2afbd2c9f442faab75ad8cd2e7578", "bd8d0e05e3b944138bcaae7366518e07",
            "dd75e4532ee14cc1995481b21006874b", "7c5a34ed8b074396be2af47cc38fb529",
            "7f5398082d3a45ab838cdc66988dbf80"
        };
        internal static readonly string[] ExplodingPropertyIds = { "b98f2e23c10f4194b7568905bafba22c", "7ccb3098ddff4d7096cbe4c6bfd1fddc", "ccca4a8e5d014a7a90e344c2c15b4595" };
        internal const string ChainTargetsPropertyId = "4a18040254d040f78c298f10649eab71";
        static void ConfigureChainTargets(KineticistClassBridge0020 graph, BlueprintCharacterClass witch, BlueprintArchetype root) {
            // DLC3_KineticRicochetProperty is 1 + character level / 2.
            // Q-07 keeps that math but only Witch contributes for our graph.
            // Do not mutate the native property or any projectile/hit logic.
            var property = graph.Stage<BlueprintUnitProperty>(ChainTargetsPropertyId);
            var original = property.GetComponents<SimplePropertyGetter>().ToArray();
            if (property.ComponentsArray.Length != 1 || original.Length != 1 || original[0].Property != UnitProperty.Level)
                throw new InvalidOperationException("B-K11-CHAIN-PROPERTY-SCHEMA: " + ChainTargetsPropertyId);
            var level = new ClassLevelGetter { Settings = original[0].Settings };
            Set(level, "m_Class", Ref<BlueprintCharacterClassReference>(witch)); Set(level, "m_Archetype", Ref<BlueprintArchetypeReference>(root));
            property.ComponentsArray = new BlueprintComponent[] { level };
        }
        static void ConfigureExplodingArrowsParameters(KineticistClassBridge0020 graph, BlueprintCharacterClass witch, BlueprintArchetype root) {
            // Custom DC and physical/energy damage read separate properties.
            // Keep their native operations, progression and Dex-based DC;
            // rebind the class/archetype and only the Con main-stat getter.
            foreach (string id in ExplodingPropertyIds) {
                var property = graph.Stage<BlueprintUnitProperty>(id);
                var levels = property.GetComponents<ClassLevelGetter>().ToArray();
                if (levels.Length != (id == "ccca4a8e5d014a7a90e344c2c15b4595" ? 0 : 1))
                    throw new InvalidOperationException("B-K10-EXPLODING-PROPERTY-SCHEMA: " + id);
                foreach (var level in levels) {
                    if (level.Class.AssetGuid.ToString() != KineticistClassBridge0020.KineticistId || level.Archetype?.AssetGuid.ToString() != "e0684d0a6ef44cae9d095f5318618a85")
                        throw new InvalidOperationException("B-K10-EXPLODING-PROPERTY-SCHEMA: " + id);
                    Set(level, "m_Class", Ref<BlueprintCharacterClassReference>(witch)); Set(level, "m_Archetype", Ref<BlueprintArchetypeReference>(root));
                }
                property.ComponentsArray = property.ComponentsArray.Select(c => c is SimplePropertyGetter stat && stat.Property == UnitProperty.StatBonusConstitution
                    ? (BlueprintComponent)new Kingmaker.UnitLogic.Class.Kineticist.Properties.KineticistMainStatBonusPropertyGetter { Settings = stat.Settings }
                    : c).ToArray();
            }
            foreach (string id in ExplodingArrowsIds) {
                var ability = graph.Copy<BlueprintAbility>(id);
                if (ability.GetComponent<ContextAbilityParamsCalculator>() != null)
                    throw new InvalidOperationException("B-K10-EXPLODING-PARAMETER-SCHEMA: donor acquired a calculator: " + id);
                var parameters = new ContextCalculateAbilityParamsBasedOnClass {
                    UseKineticistMainStat = true, StatType = Kingmaker.EntitySystem.Stats.StatType.Intelligence
                };
                Set(parameters, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(witch));
                ability.ComponentsArray = ability.ComponentsArray.Concat(new BlueprintComponent[] { parameters }).ToArray();
            }
        }
        internal static readonly string[] FocusIds = { "2bd0d44953a536f489082534c48f8e31", "c6816ad80a3df9c4ea7d3b012b06bacd", "3d8d3d6678b901444a07984294a1bc24", "7ab8947ce2e19c44a9edcf5fd1466686" };
        const string Description = "A havocker channels her patron's elemental power as kinetic blasts, using her witch level and Intelligence. She retains prepared witch spellcasting, but replaces her familiar, patron and hex choices. Choose Air, Earth, Fire or Water at level 1; gain an infusion at levels 4, 8, 12, 16 and 20, Spellburn at level 6, and infusion specialization at levels 10, 12, 16 and 20 (up to 4). She cannot take kineticist levels, accept burn, or gain extra elements, wild talents, elemental overflow or metakinesis from this archetype.";
        sealed class Receipt {
            internal KineticistClassBridge0020 Graph;
            internal BlueprintCharacterClass Witch, Kineticist;
            internal BlueprintArchetype Root;
            internal UIGroup Group;
            internal PrerequisiteNoArchetype Exclusion;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Receipt> Published = new ConditionalWeakTable<BlueprintsCache, Receipt>();
        internal static bool OwnNode(BlueprintScriptableObject node) => node != null && Published.TryGetValue(ResourcesLibrary.BlueprintsCache, out var receipt) && receipt.Graph.IsStaged(node);
        internal static bool IsHavocker(UnitDescriptor unit) => unit?.Progression.Classes.Any(c => c.Level > 0 && c.CharacterClass.AssetGuid == BlueprintGuid.Parse(KineticistClassBridge0020.WitchId) && c.Archetypes.Any(a => a.AssetGuid == Id(RootToken))) == true;
        internal static bool MayEnterKineticist(UnitDescriptor unit) => !IsHavocker(unit);
        internal static bool MayEnterHavocker(UnitDescriptor unit) => unit != null && unit.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(KineticistClassBridge0020.KineticistId)) == 0;
        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features) {
            var entry = new LevelEntry { Level = level }; Set(entry, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return entry;
        }

        // Only the rooted grant/execution closure is visited. Prerequisites,
        // visibility tests and condition facts are references to eligibility,
        // not new grants. Inverse prerequisite indexes and class libraries are
        // never followed. Shared projectile/status/property assets stay native.
        static IEnumerable<BlueprintScriptableObject> ExecutionEdges(KineticistClassBridge0020 graph, object value, string fieldName, HashSet<object> seen, string ownerType = null) {
            if (value == null) yield break;
            if (value is BlueprintReferenceBase reference) {
                if (reference.Guid == BlueprintGuid.Empty) yield break;
                var target = graph.Resolve(reference.Guid);
                if (target == null) throw new InvalidOperationException("Havocker execution reference missing: " + reference.Guid + "; field=" + fieldName + "; owner=" + ownerType + "; type=" + reference.GetType().FullName);
                string kind = target.GetType().Name;
                bool grant = fieldName == "m_Facts" || fieldName == "m_Feature" || fieldName == "m_PermanentFeature" || fieldName == "m_Features" || fieldName == "m_AllFeatures";
                if (target is BlueprintAbility || target is BlueprintActivatableAbility ||
                    target is BlueprintFeature && grant || kind == "BlueprintAbilityAreaEffect" ||
                    kind == "BlueprintItemWeapon" || kind == "BlueprintWeaponEnchantment" ||
                    target is Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff && fieldName == "m_Buff") yield return target;
                yield break;
            }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is SimpleBlueprint || value is UnityEngine.Object || value is Type || value is Delegate || value is BlueprintGuid || value is TimeSpan) yield break;
            if (value is Prerequisite || value is Condition || type.Name.StartsWith("AbilityShowIf", StringComparison.Ordinal) || type.Name.StartsWith("AbilityHideIf", StringComparison.Ordinal)) yield break;
            if (!type.IsValueType && !seen.Add(value)) yield break;
            if (value is IEnumerable list) { foreach (var item in list) foreach (var edge in ExecutionEdges(graph, item, fieldName, seen, ownerType)) yield return edge; yield break; }
            for (var t = type; t != null && t != typeof(object) && t != typeof(BlueprintComponent) && t != typeof(Element); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(f => !f.IsStatic))
                    foreach (var edge in ExecutionEdges(graph, field.GetValue(value), field.Name, seen, type.FullName)) yield return edge;
        }
        static void Expand(KineticistClassBridge0020 graph, IEnumerable<BlueprintScriptableObject> roots) {
            var queue = new Queue<BlueprintScriptableObject>(roots); var visited = new HashSet<BlueprintGuid>();
            while (queue.Count != 0) {
                var copy = queue.Dequeue(); if (!visited.Add(copy.AssetGuid)) continue;
                // An unexpected schema must fail before publication, rather than
                // accidentally traversing an unrelated library through a new edge.
                if (visited.Count > 1200) throw new InvalidOperationException("B-F02-UNEXPECTED-GRANT-CLOSURE: more than 1200 owned kinetic nodes.");
                var values = new List<object> { copy.ComponentsArray };
                if (copy is BlueprintProgression progression) values.Add(progression.LevelEntries);
                if (copy is BlueprintFeatureSelection selection) { values.Add(Get(selection, "m_Features")); values.Add(Get(selection, "m_AllFeatures")); }
                if (copy is BlueprintAbility ability) values.Add(Get(ability, "m_Parent"));
                if (copy is BlueprintActivatableAbility toggle) values.Add(Get(toggle, "m_Buff"));
                // Weapons store enchantments in authored equipment fields, not
                // components. Areas/enchantments can similarly hold execution
                // references outside their components. Never follow weapon
                // types, visual assets or the class library through these.
                string kind = copy.GetType().Name;
                var authoredEdges = kind == "BlueprintItemWeapon" || kind == "BlueprintWeaponEnchantment" || kind == "BlueprintAbilityAreaEffect"
                    ? KineticistClassBridge0020.AuthoredFields(copy.GetType()).SelectMany(f => ExecutionEdges(graph, f.GetValue(copy), f.Name, new HashSet<object>(IdentityComparer)))
                    : Enumerable.Empty<BlueprintScriptableObject>();
                // Fact components contain area/weapon execution edges too.
                BlueprintScriptableObject[] edges;
                try { edges = values.SelectMany(v => ExecutionEdges(graph, v, copy is BlueprintActivatableAbility ? "m_Buff" : "m_Features", new HashSet<object>(IdentityComparer))).Concat(authoredEdges).ToArray(); }
                catch (Exception error) { throw new InvalidOperationException("Havocker graph node " + copy.name + ": " + error.Message, error); }
                foreach (var edge in edges) {
                    if (graph.IsStaged(edge)) queue.Enqueue(edge);
                    else if (graph.Copies.TryGetValue(edge.AssetGuid, out var staged)) queue.Enqueue(staged);
                    else queue.Enqueue(graph.Stage<BlueprintScriptableObject>(edge.AssetGuid.ToString()));
                }
            }
        }

        // Native gather contains a Blood Kineticist-only visual branch pointing
        // to a deleted buff (d579...). Q-06 excludes that entire archetype, and
        // F-0020 never grants its blood feature. Specialize only this private
        // branch to its original non-blood path, preserving native gather cost,
        // duration, trigger and every reachable elemental action.
        static void SpecializeGather(KineticistClassBridge0020 graph) {
            void RewriteActions(ActionList list) {
                if (list?.Actions == null) return;
                list.Actions = list.Actions.SelectMany(action => {
                    if (!(action is Conditional conditional)) return new[] { action };
                    var conditions = conditional.ConditionsChecker.Conditions;
                    if (conditions.Length == 1 && conditions[0] is ContextConditionHasFact fact && !fact.Not &&
                        ((BlueprintUnitFactReference)Get(fact, "m_Fact")).Guid == BlueprintGuid.Parse("c347c1e4f8a30494ba83a29e76cbca0e")) {
                        RewriteActions(conditional.IfFalse); return conditional.IfFalse.Actions;
                    }
                    RewriteActions(conditional.IfTrue); RewriteActions(conditional.IfFalse); return new[] { action };
                }).ToArray();
            }
            var settings = graph.Copy<BlueprintFeature>(KineticistClassBridge0020.BurnFeatureId).GetComponent<AddKineticistPart>();
            foreach (var source in new[] { settings.GatherPowerBuff1, settings.GatherPowerBuff2, settings.GatherPowerBuff3 })
                foreach (var actions in graph.Copy<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(source.AssetGuid.ToString()).GetComponents<AddFactContextActions>()) {
                    RewriteActions(actions.Activated); RewriteActions(actions.Deactivated); RewriteActions(actions.NewRound);
                }
        }

        internal static KineticistClassBridge0020 Register(BlueprintsCache cache) {
            var assets = new PsychicAssets(cache);
            var witch = assets.Native<BlueprintCharacterClass>(KineticistClassBridge0020.WitchId);
            var kineticist = assets.Native<BlueprintCharacterClass>(KineticistClassBridge0020.KineticistId);
            if (Published.TryGetValue(cache, out var prior)) {
                if (!ReferenceEquals(prior.Witch, witch) || !ReferenceEquals(prior.Kineticist, kineticist)) throw new InvalidOperationException("Havocker parent identity changed.");
                prior.Graph.Verify(); Connect(prior); return prior.Graph;
            }
            var familiar = assets.Native<BlueprintFeature>("29a333b7ccad3214ea3a51943fa0d8e9");
            var patron = assets.Native<BlueprintFeature>("381cf4c890815d049a4420c6f31d063f");
            var hex = assets.Native<BlueprintFeature>("9846043cf51251a4897728ed6e24e76f");
            int[] hexLevels = { 1,2,4,6,8,10,12,14,16,18,20 };
            if (witch.name != "WitchClass" || witch.Progression.AssetGuid != BlueprintGuid.Parse("c5e9e2b086a9814409fb6a29a5acdd0f") ||
                hexLevels.Any(n => !witch.Progression.LevelEntries.Any(e => e.Level == n && e.Features.Contains(hex)))) throw new InvalidOperationException("Havocker native Witch replacement schema changed.");
            var graph = KineticistClassBridge0020.StageCore(cache);
            HavockerSpellburn0020.Stage(graph);
            SpecializeGather(graph);
            var element = graph.Stage<BlueprintFeatureSelection>(KineticistClassBridge0020.ElementPoolId, ElementToken);
            var focus = FocusIds.Select((id, i) => {
                var p = graph.Stage<BlueprintProgression>(id, "HavockerElementalFocus" + Elements[i] + "0020");
                p.LevelEntries = p.LevelEntries.Where(e => e.Level == 1).ToArray();
                p.UIGroups = Array.Empty<UIGroup>(); Set(p, "m_UIDeterminatorsGroup", Array.Empty<BlueprintFeatureBaseReference>());
                return p;
            }).ToArray();
            Set(element, "m_Features", focus.Select(Ref<BlueprintFeatureReference>).ToArray()); Set(element, "m_AllFeatures", focus.Select(Ref<BlueprintFeatureReference>).ToArray());
            element.ComponentsArray = new BlueprintComponent[] { new AddProficiencies { WeaponProficiencies = new[] { Kingmaker.Enums.WeaponCategory.KineticBlast } } };
            var infusion = graph.Stage<BlueprintFeatureSelection>(InfusionId, InfusionToken);
            Expand(graph, focus.Cast<BlueprintScriptableObject>().Concat(new[] { infusion, graph.Copy<BlueprintFeature>(KineticistClassBridge0020.GatherFeatureId) }));
            foreach (var ability in graph.Copies.Values.OfType<BlueprintAbility>().Where(a => a.GetComponents<AbilityKineticist>().Any()).ToArray()) HavockerSpellburn0020.PrepareBlast(graph, ability);
            var settings = graph.Copy<BlueprintFeature>(KineticistClassBridge0020.BurnFeatureId).GetComponent<AddKineticistPart>();
            Set(settings, "m_Blasts", graph.Copies.Values.OfType<BlueprintAbility>().Where(a => a.GetComponent<HavockerBlastCost0020>() != null && (Get(a, "m_Parent") == null || ((BlueprintAbilityReference)Get(a, "m_Parent")).Guid == BlueprintGuid.Empty)).Select(Ref<BlueprintAbilityReference>).ToArray());
            // The existing reducer computes the frozen level table once. This
            // ranked display fact must not add the native modifier a second time.
            var specialization = graph.Add(new BlueprintFeature { Ranks = 4, IsClassFeature = true, ComponentsArray = Array.Empty<BlueprintComponent>() }, SpecializationToken, "Infusion Specialization", "At witch levels 10, 12, 16 and 20, reduce infusion burn by 1 (maximum 4). Applied before Spellburn and native gather power. Does not reduce other burn categories.");
            var noKineticist = new PrerequisiteNoClassLevel(); Set(noKineticist, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(kineticist));
            var add = new List<LevelEntry> { Entry(1, element, graph.Copy<BlueprintFeature>(KineticistClassBridge0020.BurnFeatureId), graph.Copy<BlueprintProgression>(KineticistClassBridge0020.BlastProgressionId), graph.Copy<BlueprintFeature>(KineticistClassBridge0020.GatherFeatureId)), Entry(6, graph.Own<BlueprintFeature>(HavockerSpellburn0020.FeatureToken)) };
            foreach (int n in new[] { 4,8,10,12,16,20 }) add.Add(Entry(n, new BlueprintFeatureBase[] { n % 4 == 0 ? infusion : null, n >= 10 ? specialization : null }.Where(f => f != null).ToArray()));
            var root = graph.Add(new BlueprintArchetype { LocalizedName = Text(RootToken, "name", "Havocker"), LocalizedDescription = Text(RootToken, "description", Description), AddFeatures = add.OrderBy(e => e.Level).ToArray(), RemoveFeatures = hexLevels.Select(n => n == 1 ? Entry(n, familiar, patron, hex) : Entry(n, hex)).ToArray(), ComponentsArray = new BlueprintComponent[] { noKineticist } }, RootToken, "Havocker", Description);
            Set(root, "m_ParentClass", witch);
            ConfigureExplodingArrowsParameters(graph, witch, root);
            ConfigureChainTargets(graph, witch, root);
            var exclusion = new PrerequisiteNoArchetype { name = "$CotWHavocker0020NoKineticist", OwnerBlueprint = kineticist, CheckInProgression = true };
            Set(exclusion, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(witch)); Set(exclusion, "m_Archetype", Ref<BlueprintArchetypeReference>(root));
            var group = new UIGroup(); Set(group, "m_Features", new BlueprintFeatureBase[] { element, infusion, graph.Own<BlueprintFeature>(HavockerSpellburn0020.FeatureToken), specialization }.Select(Ref<BlueprintFeatureBaseReference>).ToList());
            var receipt = new Receipt { Graph = graph, Witch = witch, Kineticist = kineticist, Root = root, Exclusion = exclusion, Group = group };
            var oldRoots = Get(witch, "m_Archetypes"); var oldGroups = witch.Progression.UIGroups; var oldComponents = kineticist.ComponentsArray;
            graph.Seal();
            graph.PublishConnected(() => Connect(receipt), () => { Set(witch, "m_Archetypes", oldRoots); witch.Progression.UIGroups = oldGroups; kineticist.ComponentsArray = oldComponents; });
            Published.Add(cache, receipt); return graph;
        }

        static void Connect(Receipt r) {
            var roots = (BlueprintArchetypeReference[])Get(r.Witch, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>();
            var groups = r.Witch.Progression.UIGroups ?? Array.Empty<UIGroup>();
            var own = roots.Where(x => x?.Guid == r.Root.AssetGuid).ToArray();
            var exclusions = r.Kineticist.ComponentsArray.Where(c => c?.name == r.Exclusion.name).ToArray();
            var matching = groups.Where(g => g?.Features.Any(f => f?.AssetGuid == Id(ElementToken)) == true).ToArray();
            if (own.Length > 1 || own.Length == 1 && !ReferenceEquals(own[0].Get(), r.Root) || exclusions.Length > 1 || exclusions.Length == 1 && !ReferenceEquals(exclusions[0], r.Exclusion) || matching.Length > 1 || matching.Length == 1 && !ReferenceEquals(matching[0], r.Group)) throw new InvalidOperationException("Havocker parent attachment collision; no parent changes applied.");
            if (own.Length == 0) Set(r.Witch, "m_Archetypes", roots.Concat(new[] { Ref<BlueprintArchetypeReference>(r.Root) }).ToArray());
            if (matching.Length == 0) r.Witch.Progression.UIGroups = groups.Concat(new[] { r.Group }).ToArray();
            if (exclusions.Length == 0) r.Kineticist.ComponentsArray = r.Kineticist.ComponentsArray.Concat(new BlueprintComponent[] { r.Exclusion }).ToArray();
        }
        internal static void Install(Harmony harmony) {
            harmony.CreateClassProcessor(typeof(HavockerClassEntry0020Patch)).Patch(); harmony.CreateClassProcessor(typeof(HavockerArchetypeEntry0020Patch)).Patch();
            HavockerBlade0020.Install(harmony);
            HavockerFeatEligibility0020.Install(harmony);
        }
    }

    // Re-check immediately before mutation as well as the native UI Check.
    // No global fact or class-level query is changed.
    [HarmonyPatch(typeof(SelectClass), nameof(SelectClass.Apply))]
    internal static class HavockerClassEntry0020Patch {
        [HarmonyPrefix] static bool Prefix(SelectClass __instance, UnitDescriptor unit) {
            if (__instance.CharacterClass.AssetGuid != BlueprintGuid.Parse(KineticistClassBridge0020.KineticistId) || Havocker0020.MayEnterKineticist(unit)) return true;
            ModEntry.LogSpellDiagnostic("Havocker: kineticist class entry rejected before level commitment."); return false;
        }
    }
    [HarmonyPatch(typeof(AddArchetype), nameof(AddArchetype.Apply))]
    internal static class HavockerArchetypeEntry0020Patch {
        [HarmonyPrefix] static bool Prefix(AddArchetype __instance, UnitDescriptor unit) {
            if (__instance.Archetype.AssetGuid != Id(Havocker0020.RootToken) || Havocker0020.MayEnterHavocker(unit)) return true;
            ModEntry.LogSpellDiagnostic("Havocker: archetype entry rejected before commitment (kineticist levels present)."); return false;
        }
    }
}
