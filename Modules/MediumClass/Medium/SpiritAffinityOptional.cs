using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Properties;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TabletopTweaks.Core.NewComponents.Properties;

namespace MediumClass.Medium
{
    /// <summary>
    /// Spirit Affinity options backed by external or advanced selections:
    /// - Arcane Discovery (from TabletopTweaks-Base + third-party / CotW mods)
    /// - Advanced Armor Training (from TabletopTweaks-Base)
    /// - Advanced Weapon Training (first pick chooses a weapon group then chains into an
    ///   advanced weapon training; subsequent picks choose an advanced weapon training directly)
    ///
    /// Selections are built in the delayed cache stage and re-synced after GameStarter.FixTMPAssets,
    /// LocalizationManager.WaitForInit, and selection UI extraction so late additions from other
    /// mods or CotW's ReconcileOptional are always included. Class-level prerequisites are handled
    /// by <see cref="PrerequisiteClassLevel_MediumAffinity_Patch"/>.
    /// </summary>
    internal static class SpiritAffinityOptional
    {
        private const string TttArcaneDiscoverySelection = "45b72087-ac2d-4554-aec4-f44be852eddd";
        private const string TttAdvancedArmorTrainingSelection = "5bf17c2a-e9ee-4615-b27c-993629ed9bc3";
        private const string TttAdvancedWeaponTrainingSelection = "538bc739-00c2-425b-9367-7cc02a3048b1";
        private const string NativeWeaponTrainingSelection = "b8cecf4e5e464ad41b79d5b42b76b399";
        private const string NativeAdvancedWeaponTraining1 = "3aa4cbdd4af5ba54888b0dc7f07f80c4";

        private const string TttFighterArmorTrainingProperty = "8889a638-d9be-4379-ab9a-5c2e08fd5015";
        private const string TttFighterWeaponTrainingProperty = "8722e16b-5a55-48f8-a3a6-fe52a89f4fdd";

        private static readonly UnityModManagerNet.UnityModManager.ModEntry.ModLogger Logger =
            Logging.GetLogger(nameof(SpiritAffinityOptional));

        private static readonly List<(string SelectionGuid, string SourceGuid)> SyncTargets =
            new List<(string, string)>();
        private static readonly List<string> WeaponGroupSubSelections = new List<string>();
        private static string WeaponTrainingSourceGuid;

        internal static void ConfigureDelayed()
        {
            try
            {
                var affinity = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.MediumSpiritAffinity);
                if (affinity == null) return;

                SyncTargets.Clear();
                WeaponGroupSubSelections.Clear();
                WeaponTrainingSourceGuid = null;

                var added = new List<BlueprintFeatureReference>();
                bool hasTttBase = Settings.IsTTTBaseEnabled();

                if (hasTttBase)
                {
                    AddSelection(added, "MediumSpiritAffinityArcaneDiscoverySelection",
                        Guids.MediumSpiritAffinityArcaneDiscoverySelection, TttArcaneDiscoverySelection,
                        "SpiritAffinity.ArcaneDiscovery", "assets/icons/wildarcana1.png", ranks: 1);

                    AddSelection(added, "MediumSpiritAffinityArmorTraining",
                        Guids.MediumSpiritAffinityArmorTraining, TttAdvancedArmorTrainingSelection,
                        "SpiritAffinity.ArmorTraining", "assets/icons/spiritguardian.png", ranks: 5);
                }

                // Weapon Training: first pick chooses a weapon group (chaining into Advanced Weapon
                // Training), subsequent picks go straight to Advanced Weapon Training.
                string weaponSource = hasTttBase && HasOptions(TttAdvancedWeaponTrainingSelection)
                    ? TttAdvancedWeaponTrainingSelection
                    : NativeAdvancedWeaponTraining1;
                var weaponSelection = ConfigureWeaponTrainingSelection(weaponSource);
                if (weaponSelection != null)
                {
                    added.Add(weaponSelection.ToReference<BlueprintFeatureReference>());
                    WeaponTrainingSourceGuid = weaponSource;
                    SyncTargets.Add((Guids.MediumSpiritAffinityWeaponTraining, weaponSource));
                    Logger.Log("Spirit Affinity: Weapon Training uses source " + weaponSource);
                }

                if (added.Count > 0)
                {
                    AppendChoices(affinity, added);
                }

                if (hasTttBase)
                {
                    PatchTrainingProperty(TttFighterArmorTrainingProperty, Guids.MediumSpiritAffinityArmorTraining);
                    PatchTrainingProperty(TttFighterWeaponTrainingProperty, Guids.MediumSpiritAffinityWeaponTraining);
                }

                Logger.Log("Spirit Affinity: configured " + added.Count + " optional/advanced entries.");
            }
            catch (Exception e)
            {
                // A missing or changed optional API must not block character creation.
                Logger.Log("Skipped optional Spirit Affinity entries: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static BlueprintFeatureSelection ConfigureWeaponTrainingSelection(string weaponSourceGuid)
        {
            var awtSource = BlueprintTool.Get<BlueprintFeatureSelection>(weaponSourceGuid);
            var awtOptions = awtSource?.m_AllFeatures;
            if (awtOptions == null || awtOptions.Length == 0)
            {
                Logger.Log("Spirit Affinity: " + weaponSourceGuid + " has no options; Weapon Training skipped.");
                return null;
            }

            var pickedMarker = FeatureConfigurator
                .New("MediumSpiritAffinityWeaponGroupPicked", Guids.MediumSpiritAffinityWeaponGroupPicked)
                .SetDisplayName("SpiritAffinity.WeaponTraining.Name")
                .SetDescription("SpiritAffinity.WeaponTraining.Description")
                .SetIsClassFeature(true)
                .SetHideInUI(true)
                .SetHideInCharacterSheetAndLevelUp(true)
                .SetRanks(1)
                .Configure();

            var groupSelections = new List<BlueprintFeatureReference>();
            var wtSource = BlueprintTool.Get<BlueprintFeatureSelection>(NativeWeaponTrainingSelection);
            foreach (var reference in wtSource?.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
            {
                var nativeGroup = reference?.Get();
                var atk = nativeGroup?.GetComponent<WeaponGroupAttackBonus>();
                if (atk == null) continue;

                string subGuid = VariantId.Of("MediumWeaponGroupChoice", nativeGroup.AssetGuid.ToString());
                var sub = FeatureSelectionConfigurator
                    .New("MediumSpiritAffinityWeaponGroup_" + nativeGroup.name, subGuid)
                    .SetDisplayName(nativeGroup.m_DisplayName)
                    .SetDescription(nativeGroup.m_Description)
                    .SetIsClassFeature(true)
                    .SetRanks(1)
                    .SetIgnorePrerequisites(false)
                    .SetReapplyOnLevelUp(false)
                    .AddComponent<MediumTrainedWeaponGroupComponent>(c =>
                    {
                        c.WeaponGroup = atk.WeaponGroup;
                        c.NativeGroupGuid = nativeGroup.AssetGuid.ToString();
                    })
                    .AddFacts(new() { pickedMarker })
                    .AddToAllFeatures(awtOptions.Select(o => (Blueprint<BlueprintFeatureReference>)o).ToArray())
                    .Configure();
                sub.m_Icon = nativeGroup.m_Icon;
                sub.m_Features = sub.m_AllFeatures;
                groupSelections.Add(sub.ToReference<BlueprintFeatureReference>());
                WeaponGroupSubSelections.Add(subGuid);
            }

            var combined = groupSelections.Concat(awtOptions).ToArray();
            var selection = FeatureSelectionConfigurator
                .New("MediumSpiritAffinityWeaponTraining", Guids.MediumSpiritAffinityWeaponTraining)
                .SetDisplayName("SpiritAffinity.WeaponTraining.Name")
                .SetDescription("SpiritAffinity.WeaponTraining.Description")
                .SetIcon("assets/icons/spiritchampion.png")
                .SetIsClassFeature(true)
                .SetRanks(5)
                .SetIgnorePrerequisites(false)
                .SetReapplyOnLevelUp(false)
                .AddComponent<MediumAdvancedWeaponTrainingComponent>()
                .AddToAllFeatures(combined.Select(o => (Blueprint<BlueprintFeatureReference>)o).ToArray())
                .Configure();
            selection.m_Features = selection.m_AllFeatures;
            return selection;
        }

        /// <summary>
        /// Re-syncs copied option lists after late patches (e.g. EbonsContentMod, MysticalMayhem,
        /// or CotW's ReconcileOptional after WaitForInit) have appended options to source selections.
        /// Idempotent and append-only.
        /// </summary>
        internal static void SyncPools()
        {
            try
            {
                foreach (var (selectionGuid, sourceGuid) in SyncTargets)
                {
                    var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionGuid);
                    var source = BlueprintTool.Get<BlueprintFeatureSelection>(sourceGuid);
                    if (selection == null || source?.m_AllFeatures == null) continue;

                    var known = new HashSet<string>((selection.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                        .Where(r => r != null).Select(r => r.deserializedGuid.ToString()));
                    var newcomers = source.m_AllFeatures
                        .Where(r => r != null && known.Add(r.deserializedGuid.ToString()))
                        .ToArray();
                    if (newcomers.Length > 0)
                    {
                        AppendChoices(selection, newcomers.ToList());
                        Logger.Log("Spirit Affinity: synced " + newcomers.Length + " late option(s) into " + selection.name + ".");
                    }
                }

                SyncSelectionFromNativeFeats(Guids.MediumSpiritAffinityInsightFromBeyond, metamagicOnly: false);
                SyncSelectionFromNativeFeats(Guids.MediumSpiritAffinityForcefulMetamagic, metamagicOnly: true);
                SpiritAffinity.SyncForcefulMetamagicPresentation();

                if (!string.IsNullOrEmpty(WeaponTrainingSourceGuid) && WeaponGroupSubSelections.Count > 0)
                {
                    var source = BlueprintTool.Get<BlueprintFeatureSelection>(WeaponTrainingSourceGuid);
                    if (source?.m_AllFeatures != null)
                    {
                        foreach (var subGuid in WeaponGroupSubSelections)
                        {
                            var sub = BlueprintTool.Get<BlueprintFeatureSelection>(subGuid);
                            if (sub == null) continue;
                            var known = new HashSet<string>((sub.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                                .Where(r => r != null).Select(r => r.deserializedGuid.ToString()));
                            var newcomers = source.m_AllFeatures
                                .Where(r => r != null && known.Add(r.deserializedGuid.ToString()))
                                .ToArray();
                            if (newcomers.Length > 0)
                                AppendChoices(sub, newcomers.ToList());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Spirit Affinity: late pool sync skipped: " + e.GetType().Name + ": " + e.Message);
            }
        }

        internal static bool IsManagedSelection(BlueprintGuid guid)
        {
            string s = guid.ToString();
            if (string.Equals(s, Guids.MediumSpiritAffinityInsightFromBeyond, StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, Guids.MediumSpiritAffinityForcefulMetamagic, StringComparison.OrdinalIgnoreCase))
                return true;
            if (SyncTargets.Any(t => string.Equals(t.SelectionGuid, s, StringComparison.OrdinalIgnoreCase)))
                return true;
            return WeaponGroupSubSelections.Any(g => string.Equals(g, s, StringComparison.OrdinalIgnoreCase));
        }

        private static void SyncSelectionFromNativeFeats(string selectionGuid, bool metamagicOnly)
        {
            var selection = BlueprintTool.Get<BlueprintFeatureSelection>(selectionGuid);
            var native = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.NativeBasicFeatSelection);
            if (selection == null || native?.m_AllFeatures == null) return;

            var known = new HashSet<string>((selection.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                .Where(r => r != null).Select(r => r.deserializedGuid.ToString()));
            var newcomers = native.m_AllFeatures
                .Where(r => r != null
                    && (!metamagicOnly || r.Get()?.GetComponent<Kingmaker.UnitLogic.FactLogic.AddMetamagicFeat>() != null)
                    && known.Add(r.deserializedGuid.ToString()))
                .ToArray();
            if (newcomers.Length > 0)
                AppendChoices(selection, newcomers.ToList());
        }

        private static bool HasOptions(string sourceGuid)
        {
            var source = BlueprintTool.Get<BlueprintFeatureSelection>(sourceGuid);
            return source?.m_AllFeatures != null && source.m_AllFeatures.Length > 0;
        }

        /// <summary>Copies one source selection's options into our own selection.</summary>
        private static BlueprintFeatureSelection AddSelection(
            List<BlueprintFeatureReference> added,
            string name, string guid, string sourceGuid, string textKey, string icon, int ranks,
            Action<FeatureSelectionConfigurator> configure = null)
        {
            var source = BlueprintTool.Get<BlueprintFeatureSelection>(sourceGuid);
            var options = source?.m_AllFeatures;
            if (options == null || options.Length == 0)
            {
                Logger.Log("Spirit Affinity: " + sourceGuid + " has no options; entry skipped.");
                return null;
            }

            var builder = FeatureSelectionConfigurator.New(name, guid)
                .SetDisplayName(textKey + ".Name")
                .SetDescription(textKey + ".Description")
                .SetIcon(icon)
                .SetIsClassFeature(true)
                .SetRanks(ranks)
                .SetIgnorePrerequisites(false)
                .SetReapplyOnLevelUp(false)
                .AddToAllFeatures(options.Select(o => (Blueprint<BlueprintFeatureReference>)o).ToArray());
            configure?.Invoke(builder);
            var selection = builder.Configure();
            selection.m_Features = selection.m_AllFeatures;

            added.Add(selection.ToReference<BlueprintFeatureReference>());
            SyncTargets.Add((guid, sourceGuid));
            Logger.Log("Spirit Affinity: " + name + " offers " + options.Length + " options.");
            return selection;
        }

        private static void PatchTrainingProperty(string propertyGuid, string requiredAffinityGuid)
        {
            var prop = BlueprintTool.Get<BlueprintUnitProperty>(propertyGuid);
            var composite = prop?.ComponentsArray?.OfType<CompositeCustomPropertyGetter>().FirstOrDefault();
            if (composite == null) return;
            var existing = composite.Properties ?? Array.Empty<CompositeCustomPropertyGetter.ComplexCustomProperty>();
            if (existing.Any(p => p?.Property is MediumTrainingLevelGetter g && g.RequiredFeatureGuid == requiredAffinityGuid))
                return;

            composite.Properties = existing.Concat(new[]
            {
                new CompositeCustomPropertyGetter.ComplexCustomProperty
                {
                    Property = new MediumTrainingLevelGetter
                    {
                        RequiredFeatureGuid = requiredAffinityGuid
                    }
                }
            }).ToArray();
        }

        private static void AppendChoices(BlueprintFeatureSelection selection, List<BlueprintFeatureReference> added)
        {
            var merged = (selection.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                .Concat(added)
                .Where(r => r != null)
                .GroupBy(r => r.deserializedGuid)
                .Select(g => g.First())
                .ToArray();
            selection.m_AllFeatures = merged;
            selection.m_Features = merged;
        }
    }

    [HarmonyPatch(typeof(GameStarter), "FixTMPAssets")]
    internal static class GameStarter_LatePoolSync_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            SpiritAffinityOptional.SyncPools();
            SpiritFeat.ConfigureDelayed();
        }
    }

    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.WaitForInit))]
    internal static class LocalizationManager_LatePoolSync_Patch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            SpiritAffinityOptional.SyncPools();
            SpiritFeat.ConfigureDelayed();
        }
    }

    [HarmonyPatch(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.ExtractSelectionItems))]
    internal static class BlueprintFeatureSelection_LatePoolSync_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(BlueprintFeatureSelection __instance)
        {
            if (__instance == null) return;
            if (SpiritAffinityOptional.IsManagedSelection(__instance.AssetGuid))
                SpiritAffinityOptional.SyncPools();
            else if (__instance.AssetGuid == BlueprintGuid.Parse(Guids.MediumSpiritFeat))
                SpiritFeat.ConfigureDelayed();
        }
    }
}
