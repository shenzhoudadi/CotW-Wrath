using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using MediumClass.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    /// <summary>
    /// Supplies the medium's class level (minus any fighter levels already counted by the host
    /// property) when computing TabletopTweaks' FighterArmorTrainingProperty /
    /// FighterWeaponTrainingProperty, so advanced armor/weapon trainings use the medium level in
    /// place of fighter level without double-counting multiclass fighter levels.
    /// </summary>
    [TypeId("d94a63f7-3c5c-4c36-9a3f-8b1e2c52e901")]
    public class MediumTrainingLevelGetter : PropertyValueGetter
    {
        public string RequiredFeatureGuid;
        public string MediumClassGuid = Guids.Medium;
        public string FighterClassGuid = "48ac8db94d5de7645906c7d0ad3bcfbd";

        public override int GetBaseValue(UnitEntityData unit)
        {
            if (unit?.Descriptor == null || string.IsNullOrEmpty(RequiredFeatureGuid)) return 0;
            var required = BlueprintGuid.Parse(RequiredFeatureGuid);
            if (!unit.Progression.Features.Enumerable.Any(f => f.Blueprint?.AssetGuid == required))
                return 0;

            var mediumClass = BlueprintCore.Utils.BlueprintTool.Get<BlueprintCharacterClass>(MediumClassGuid);
            if (mediumClass == null) return 0;
            int mediumLevel = unit.Progression.GetClassLevel(mediumClass);
            if (mediumLevel <= 0) return 0;

            var fighterClass = BlueprintCore.Utils.BlueprintTool.Get<BlueprintCharacterClass>(FighterClassGuid);
            int fighterLevel = fighterClass != null ? unit.Progression.GetClassLevel(fighterClass) : 0;
            return Math.Max(0, mediumLevel - fighterLevel);
        }
    }

    /// <summary>
    /// Marks a chosen trained weapon group for Medium Spirit Affinity Weapon Training without
    /// adding a flat +1/+1 attack/damage bonus. Used by UnitPartWeaponTraining patches and
    /// weapon-group feat prerequisites.
    /// </summary>
    [TypeId("9a7e4d23-1b65-4e82-8f1c-3d5b7a9e0412")]
    public class MediumTrainedWeaponGroupComponent : UnitFactComponentDelegate
    {
        public WeaponFighterGroup WeaponGroup;
        public string NativeGroupGuid;

        public override void OnTurnOn()
        {
            base.OnTurnOn();
            Owner?.Ensure<UnitPartWeaponTraining>();
        }
    }

    /// <summary>
    /// Attached to the Medium's Advanced Weapon Training affinity selection so the unit always has
    /// UnitPartWeaponTraining (required by native and TTT advanced weapon trainings).
    /// </summary>
    [TypeId("4e3c82c5-8b1f-4e64-9a4e-631d529a1f10")]
    class MediumAdvancedWeaponTrainingComponent : UnitFactComponentDelegate
    {
        public override void OnTurnOn()
        {
            base.OnTurnOn();
            Owner?.Ensure<UnitPartWeaponTraining>();
        }

        internal static void RefreshFor(UnitEntityData owner)
        {
            var part = owner?.Get<UnitPartWeaponTraining>();
            if (part == null) return;
            EventBus.RaiseEvent(owner, delegate (IUnitPartUpdatedHandler h)
            {
                h.HandleUnitPartUpdated(part);
            });
            // DefensiveWeaponTrainingComponent (TTT-Core) listens to equipment set changes rather than
            // IUnitPartUpdatedHandler, so raise that too when the channelled spirit changes.
            EventBus.RaiseEvent(delegate (IUnitActiveEquipmentSetHandler h)
            {
                h.HandleUnitChangeActiveEquipmentSet(owner.Descriptor);
            });
        }
    }

    internal static class MediumTrainingRules
    {
        internal const string NativeArmorTraining = "3c380607706f209499d951b29d3c44f3";
        internal const string NativeWeaponTrainingSelection = "b8cecf4e5e464ad41b79d5b42b76b399";
        internal const string NativeBravery = "f6388946f9f472f4585591b80e9f2452";
        internal const string WizardClassGuid = "ba34257984f4c41408ce1dc2004e342e";
        internal const string FighterClassGuid = "48ac8db94d5de7645906c7d0ad3bcfbd";

        internal static bool HasFeature(UnitDescriptor unit, string guid)
        {
            if (unit?.Progression?.Features == null || string.IsNullOrEmpty(guid)) return false;
            var parsed = BlueprintGuid.Parse(guid);
            return unit.Progression.Features.Enumerable.Any(f => f.Blueprint?.AssetGuid == parsed);
        }

        internal static bool IsInsideSelection(FeatureSelectionState selectionState, string selectionGuid)
        {
            if (selectionState == null || string.IsNullOrEmpty(selectionGuid)) return false;
            var target = BlueprintGuid.Parse(selectionGuid);
            for (var cur = selectionState; cur != null; cur = cur.Parent)
            {
                if (cur.Selection is BlueprintScriptableObject bp && bp.AssetGuid == target)
                    return true;
            }
            return false;
        }

        internal static int MediumLevel(UnitDescriptor unit)
        {
            if (unit?.Progression == null) return 0;
            var medium = BlueprintCore.Utils.BlueprintTool.Get<BlueprintCharacterClass>(Guids.Medium);
            return medium != null ? unit.Progression.GetClassLevel(medium) : 0;
        }

        internal static int TrainingSpiritBonus(UnitEntityData owner)
        {
            if (owner == null) return 0;
            int active = SpiritBonusValue.Of(owner);
            if (active > 0) return active;
            var baseBonus = BlueprintCore.Utils.BlueprintTool.Get<BlueprintFeature>(Guids.MediumSpiritBonus);
            return baseBonus != null ? owner.Progression.Features.GetRank(baseBonus) : 0;
        }

        private static int _cachedDiscoveryCount = -1;
        private static readonly HashSet<BlueprintGuid> _discoveryBlueprints = new HashSet<BlueprintGuid>();
        private static readonly HashSet<BlueprintGuid> _discoveryResources = new HashSet<BlueprintGuid>();

        private static void EnsureDiscoveryCache()
        {
            var sel = BlueprintCore.Utils.BlueprintTool.Get<BlueprintFeatureSelection>(Guids.MediumSpiritAffinityArcaneDiscoverySelection);
            var list = sel?.m_AllFeatures;
            int count = list?.Length ?? 0;
            if (count == _cachedDiscoveryCount) return;

            _cachedDiscoveryCount = count;
            _discoveryBlueprints.Clear();
            _discoveryResources.Clear();
            if (list == null) return;

            var queue = new Queue<BlueprintScriptableObject>();
            foreach (var r in list)
            {
                var bp = r?.Get();
                if (bp != null) queue.Enqueue(bp);
            }
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == null || !_discoveryBlueprints.Add(cur.AssetGuid)) continue;
                if (cur is BlueprintFeatureSelection sub && sub.m_AllFeatures != null)
                {
                    foreach (var r in sub.m_AllFeatures)
                        if (r?.Get() != null) queue.Enqueue(r.Get());
                }
                foreach (var comp in cur.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (comp is AddAbilityResources res && res.Resource != null)
                        _discoveryResources.Add(res.Resource.AssetGuid);
                    else if (comp is AddFacts facts)
                    {
                        foreach (var f in facts.Facts)
                            if (f != null) queue.Enqueue(f);
                    }
                }
            }
        }

        internal static bool IsArcaneDiscoveryBlueprint(BlueprintScriptableObject bp)
        {
            if (bp == null) return false;
            EnsureDiscoveryCache();
            return _discoveryBlueprints.Contains(bp.AssetGuid);
        }

        internal static bool IsArcaneDiscoveryResource(BlueprintAbilityResource resource)
        {
            if (resource == null) return false;
            EnsureDiscoveryCache();
            return _discoveryResources.Contains(resource.AssetGuid);
        }

        internal static IEnumerable<MediumTrainedWeaponGroupComponent> TrainedGroups(UnitDescriptor unit, FeatureSelectionState selectionState = null)
        {
            if (unit?.Progression?.Features != null)
            {
                foreach (var fact in unit.Progression.Features.Enumerable)
                {
                    var comp = fact?.Blueprint?.GetComponent<MediumTrainedWeaponGroupComponent>();
                    if (comp != null) yield return comp;
                }
            }
            for (var cur = selectionState; cur != null; cur = cur.Parent)
            {
                var comp = cur.SelectedItem?.Feature?.GetComponent<MediumTrainedWeaponGroupComponent>();
                if (comp != null) yield return comp;
                if (cur.Selection is BlueprintScriptableObject bp)
                {
                    var selComp = bp.GetComponent<MediumTrainedWeaponGroupComponent>();
                    if (selComp != null) yield return selComp;
                }
            }
        }

        internal static bool HasTrainedWeapon(UnitDescriptor unit, ItemEntityWeapon weapon)
        {
            if (unit == null || weapon == null) return false;
            foreach (var comp in TrainedGroups(unit))
            {
                if (weapon.Blueprint.FighterGroup.Contains(comp.WeaponGroup))
                    return true;
            }
            return false;
        }

        internal static bool SatisfiedByTrainingAffinity(UnitDescriptor unit, FeatureSelectionState selectionState, BlueprintFeature feature)
        {
            if (feature == null || unit == null) return false;
            var guid = feature.AssetGuid;
            if (guid == BlueprintGuid.Parse(NativeArmorTraining))
                return HasFeature(unit, Guids.MediumSpiritAffinityArmorTraining)
                    || IsInsideSelection(selectionState, Guids.MediumSpiritAffinityArmorTraining);
            if (guid == BlueprintGuid.Parse(NativeWeaponTrainingSelection))
                return HasFeature(unit, Guids.MediumSpiritAffinityWeaponTraining)
                    || IsInsideSelection(selectionState, Guids.MediumSpiritAffinityWeaponTraining);
            if (guid == BlueprintGuid.Parse(NativeBravery))
                return IsInsideSelection(selectionState, Guids.MediumSpiritAffinityWeaponTraining);

            // Native fighter weapon group prerequisites (e.g. Versatile Training, Dazzling Intimidation):
            // satisfied by the weapon group picked with the first Weapon Training affinity.
            string targetId = guid.ToString();
            foreach (var comp in TrainedGroups(unit, selectionState))
            {
                if (string.Equals(comp.NativeGroupGuid, targetId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(UnitPartWeaponTraining))]
    internal static class UnitPartWeaponTraining_MediumAffinity_Patch
    {
        [HarmonyPatch(nameof(UnitPartWeaponTraining.GetWeaponRank), new[] { typeof(ItemEntityWeapon) })]
        [HarmonyPostfix]
        private static void GetWeaponRankPostfix(UnitPartWeaponTraining __instance, ItemEntityWeapon weapon, ref int __result)
        {
            if (weapon == null) return;
            var unit = __instance?.Owner?.Unit;
            if (unit == null || !MediumTrainingRules.HasTrainedWeapon(unit.Descriptor, weapon))
                return;
            int spiritBonus = MediumTrainingRules.TrainingSpiritBonus(unit);
            if (spiritBonus > __result)
                __result = spiritBonus;
        }

        [HarmonyPatch(nameof(UnitPartWeaponTraining.GetMaxWeaponRank))]
        [HarmonyPostfix]
        private static void GetMaxWeaponRankPostfix(UnitPartWeaponTraining __instance, ref int __result)
        {
            var unit = __instance?.Owner?.Unit;
            if (unit?.Body == null) return;
            bool trained = MediumTrainingRules.HasTrainedWeapon(unit.Descriptor, unit.Body.PrimaryHand.MaybeWeapon)
                || MediumTrainingRules.HasTrainedWeapon(unit.Descriptor, unit.Body.SecondaryHand.MaybeWeapon)
                || unit.Body.AdditionalLimbs.Any(l => MediumTrainingRules.HasTrainedWeapon(unit.Descriptor, l.MaybeWeapon));
            if (!trained) return;
            int spiritBonus = MediumTrainingRules.TrainingSpiritBonus(unit);
            if (spiritBonus > __result)
                __result = spiritBonus;
        }
    }

    [HarmonyPatch(typeof(PrerequisiteClassLevel), "CheckInternal")]
    internal static class PrerequisiteClassLevel_MediumAffinity_Patch
    {
        private static void Postfix(PrerequisiteClassLevel __instance, FeatureSelectionState selectionState, UnitDescriptor unit, ref bool __result)
        {
            if (unit == null || __instance.CharacterClass == null) return;
            int mediumLevel = MediumTrainingRules.MediumLevel(unit);
            if (mediumLevel <= 0) return;

            var clsGuid = __instance.CharacterClass.AssetGuid;
            int replacementLevel = 0;
            if (clsGuid == BlueprintGuid.Parse(MediumTrainingRules.WizardClassGuid))
            {
                if (MediumTrainingRules.IsInsideSelection(selectionState, Guids.MediumSpiritAffinityArcaneDiscoverySelection))
                    replacementLevel = mediumLevel;
            }
            else if (clsGuid == BlueprintGuid.Parse(MediumTrainingRules.FighterClassGuid))
            {
                if (MediumTrainingRules.IsInsideSelection(selectionState, Guids.MediumSpiritAffinityArmorTraining)
                    || MediumTrainingRules.IsInsideSelection(selectionState, Guids.MediumSpiritAffinityWeaponTraining))
                {
                    replacementLevel = mediumLevel;
                }
                else if (MediumTrainingRules.HasFeature(unit, Guids.MediumSpiritAffinityArmorTraining)
                    || MediumTrainingRules.HasFeature(unit, Guids.MediumSpiritAffinityWeaponTraining))
                {
                    // Outside the affinity selection: half medium level counts as fighter level for feat prerequisites.
                    replacementLevel = mediumLevel / 2;
                }
            }

            if (replacementLevel <= 0) return;
            int effective = Math.Max(unit.Progression.GetClassLevel(__instance.CharacterClass), replacementLevel);
            bool passed = __instance.Not ? effective < __instance.Level : effective >= __instance.Level;
            if (__instance.Not)
                __result = __result && passed;
            else
                __result = __result || passed;
        }
    }

    [HarmonyPatch(typeof(PrerequisiteFeature), "CheckInternal")]
    internal static class PrerequisiteFeature_MediumAffinity_Patch
    {
        private static void Postfix(PrerequisiteFeature __instance, FeatureSelectionState selectionState, UnitDescriptor unit, ref bool __result)
        {
            if (__result) return;
            if (MediumTrainingRules.SatisfiedByTrainingAffinity(unit, selectionState, __instance.Feature))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(PrerequisiteFeaturesFromList), "CheckInternal")]
    internal static class PrerequisiteFeaturesFromList_MediumAffinity_Patch
    {
        private static void Postfix(PrerequisiteFeaturesFromList __instance, FeatureSelectionState selectionState, UnitDescriptor unit, ref bool __result)
        {
            if (__result || __instance.Amount > 1) return;
            foreach (var feature in __instance.Features)
            {
                if (MediumTrainingRules.SatisfiedByTrainingAffinity(unit, selectionState, feature))
                {
                    __result = true;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// On the first Weapon Training affinity pick, only weapon-group sub-selections are offered
    /// (which then chain directly into Advanced Weapon Training). Once a weapon group has been
    /// chosen, Advanced Weapon Training options are offered directly.
    /// </summary>
    [HarmonyPatch(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.CanSelect))]
    internal static class BlueprintFeatureSelection_MediumWeaponGroupGate_Patch
    {
        private static void Postfix(BlueprintFeatureSelection __instance, UnitDescriptor unit, IFeatureSelectionItem item, ref bool __result)
        {
            if (!__result || __instance == null || unit == null || item?.Feature == null) return;
            if (__instance.AssetGuid != BlueprintGuid.Parse(Guids.MediumSpiritAffinityWeaponTraining)) return;

            bool hasGroup = MediumTrainingRules.HasFeature(unit, Guids.MediumSpiritAffinityWeaponGroupPicked);
            bool isGroupChoice = item.Feature.GetComponent<MediumTrainedWeaponGroupComponent>() != null;
            if (!hasGroup && !isGroupChoice)
                __result = false;
            else if (hasGroup && isGroupChoice)
                __result = false;
        }
    }

    /// <summary>
    /// Native Armed Bravery scales by FeatureRank(Bravery). When a medium took Armed Bravery via
    /// Spirit Affinity Weapon Training without fighter levels, supply TrainingSpiritBonus instead.
    /// Also scales Arcane Discovery ContextRankConfig(ClassLevel, Wizard) using medium level.
    /// </summary>
    [HarmonyPatch(typeof(ContextRankConfig), "GetBaseValue")]
    internal static class ContextRankConfig_ArmedBravery_Patch
    {
        private static void Postfix(ContextRankConfig __instance, MechanicsContext context, ref int __result)
        {
            var owner = context?.MaybeOwner ?? context?.MaybeCaster;
            if (owner == null) return;

            if (__instance.m_BaseValueType == ContextRankBaseValueType.FeatureRank
                && __result <= 0
                && __instance.m_Feature?.deserializedGuid == BlueprintGuid.Parse(MediumTrainingRules.NativeBravery)
                && MediumTrainingRules.HasFeature(owner.Descriptor, Guids.MediumSpiritAffinityWeaponTraining))
            {
                __result = MediumTrainingRules.TrainingSpiritBonus(owner);
                return;
            }

            if (__instance.m_BaseValueType == ContextRankBaseValueType.ClassLevel
                && !__instance.m_ExceptClasses
                && __instance.m_Class != null
                && __instance.m_Class.Any(c => c?.deserializedGuid == BlueprintGuid.Parse(MediumTrainingRules.WizardClassGuid))
                && MediumTrainingRules.HasFeature(owner.Descriptor, Guids.MediumSpiritAffinityArcaneDiscoverySelection)
                && MediumTrainingRules.IsArcaneDiscoveryBlueprint(context.AssociatedBlueprint))
            {
                var wizard = BlueprintCore.Utils.BlueprintTool.Get<BlueprintCharacterClass>(MediumTrainingRules.WizardClassGuid);
                int wizardLevel = wizard != null ? owner.Descriptor.Progression.GetClassLevel(wizard) : 0;
                int mediumLevel = MediumTrainingRules.MediumLevel(owner.Descriptor);
                if (mediumLevel > wizardLevel)
                    __result += mediumLevel - wizardLevel;
            }
        }
    }

    /// <summary>
    /// Arcane Discovery effects that scale by Wizard level via AddFeatureOnClassLevel (e.g.
    /// TTT-Base Idealize's 20th-level upgrade) treat Medium level as Wizard level.
    /// </summary>
    [HarmonyPatch(typeof(AddFeatureOnClassLevel), "IsFeatureShouldBeApplied")]
    internal static class AddFeatureOnClassLevel_ArcaneDiscovery_Patch
    {
        private static void Postfix(AddFeatureOnClassLevel __instance, ref bool __result)
        {
            if (__instance?.Owner == null || __instance.Class?.AssetGuid != BlueprintGuid.Parse(MediumTrainingRules.WizardClassGuid))
                return;
            if (!MediumTrainingRules.HasFeature(__instance.Owner, Guids.MediumSpiritAffinityArcaneDiscoverySelection))
                return;
            if (!MediumTrainingRules.IsArcaneDiscoveryBlueprint(__instance.Fact?.Blueprint))
                return;

            int mediumLevel = MediumTrainingRules.MediumLevel(__instance.Owner);
            int wizardLevel = ReplaceCasterLevelOfAbility.CalculateClassLevel(
                __instance.Class, __instance.AdditionalClasses.ToArray(), __instance.Owner, __instance.Archetypes.ToArray());
            int effective = Math.Max(wizardLevel, mediumLevel);
            __result = __instance.BeforeThisLevel ? effective < __instance.Level : effective >= __instance.Level;
        }
    }

    /// <summary>
    /// Arcane Discovery daily resources that scale by Wizard level (e.g. Time Stutter) treat
    /// Medium level as Wizard level.
    /// </summary>
    [HarmonyPatch(typeof(BlueprintAbilityResource), nameof(BlueprintAbilityResource.GetMaxAmount))]
    internal static class BlueprintAbilityResource_ArcaneDiscovery_Patch
    {
        private static void Postfix(BlueprintAbilityResource __instance, UnitDescriptor unit, ref int __result)
        {
            if (unit == null || __instance == null) return;
            if (!MediumTrainingRules.HasFeature(unit, Guids.MediumSpiritAffinityArcaneDiscoverySelection))
                return;
            if (!MediumTrainingRules.IsArcaneDiscoveryResource(__instance))
                return;

            var wizard = BlueprintCore.Utils.BlueprintTool.Get<BlueprintCharacterClass>(MediumTrainingRules.WizardClassGuid);
            int wizardLevel = wizard != null ? unit.Progression.GetClassLevel(wizard) : 0;
            int mediumLevel = MediumTrainingRules.MediumLevel(unit);
            if (mediumLevel <= wizardLevel) return;

            var amount = Traverse.Create(__instance).Field("m_MaxAmount").GetValue<BlueprintAbilityResource.Amount>();
            int delta = 0;

            if (amount.IncreasedByLevel && amount.Class.Any(c => c?.AssetGuid == BlueprintGuid.Parse(MediumTrainingRules.WizardClassGuid)))
            {
                delta += amount.LevelIncrease * (mediumLevel - wizardLevel);
            }

            if (amount.IncreasedByLevelStartPlusDivStep
                && amount.LevelStep != 0
                && amount.ClassDiv.Any(c => c?.AssetGuid == BlueprintGuid.Parse(MediumTrainingRules.WizardClassGuid)))
            {
                int oldDiv = 0;
                foreach (var c in amount.ClassDiv)
                {
                    bool ok = true;
                    foreach (var arch in amount.ArchetypesDiv)
                    {
                        var cd = unit.Progression.GetClassData(c);
                        ok = cd == null || !c.Archetypes.Contains(arch) || cd.Archetypes.Contains(arch);
                    }
                    oldDiv += ok ? unit.Progression.GetClassLevel(c) : 0;
                }
                int newDiv = oldDiv + (mediumLevel - wizardLevel);
                oldDiv += (int)((unit.Progression.CharacterLevel - oldDiv) * amount.OtherClassesModifier);
                newDiv += (int)((unit.Progression.CharacterLevel - newDiv) * amount.OtherClassesModifier);

                int oldBonus = amount.StartingLevel <= oldDiv
                    ? Math.Max(amount.StartingIncrease + amount.PerStepIncrease * (oldDiv - amount.StartingLevel) / amount.LevelStep, amount.MinClassLevelIncrease)
                    : 0;
                int newBonus = amount.StartingLevel <= newDiv
                    ? Math.Max(amount.StartingIncrease + amount.PerStepIncrease * (newDiv - amount.StartingLevel) / amount.LevelStep, amount.MinClassLevelIncrease)
                    : 0;
                delta += newBonus - oldBonus;
            }

            if (delta > 0)
                __result += delta;
        }
    }
}
