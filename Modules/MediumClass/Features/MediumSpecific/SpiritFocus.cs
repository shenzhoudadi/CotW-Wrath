using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Features.MediumSpecific
{
    class SpiritFocus
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(SpiritFocus));
        private static readonly string FeatName = "SpiritFocus";
        private static readonly string DisplayName = "SpiritFocus.Name";
        private static readonly string Description = "SpiritFocus.Description";

        // Native general feat pool. Kept here rather than in Guids: it is a game
        // blueprint, not one of ours (same convention as HardenedSoul).
        private const string BasicFeatSelectionGuid = "247a4068296e8be42890143f451b4b45";

        // The six spirits are the rows of the Spirit Focus chooser, not general
        // feats of their own: no Feat group here, only the selection carries it.
        // Ranks 1 lets each spirit be taken once while leaving the others open.
        private static void ConfigureSpirit(string guid, string name, string spiritClass, string icon)
        {
            FeatureConfigurator.New(name, guid)
                .SetDisplayName(name + ".Name")
                .SetDescription(Description)
                .SetIcon(icon)
                .SetIsClassFeature(false)
                .SetRanks(1)
                .SetHideInUI(false)
                .SetHideNotAvailibleInUI(false)
                .AddComponent<PrerequisiteFeature>(c =>
                {
                    c.m_Feature = BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.MediumSpiritBonus);
                })
                .AddComponent<MediumSpiritFocusComponent>(c => c.Spirit = BlueprintTool.GetRef<BlueprintCharacterClassReference>(spiritClass))
                .Configure();
        }

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Spirit Focus feats.");

            ConfigureSpirit(Guids.SpiritFocusArchmage, FeatName + "Archmage", Guids.Archmage, "assets/icons/spiritarchmage.png");
            ConfigureSpirit(Guids.SpiritFocusChampion, FeatName + "Champion", Guids.Champion, "assets/icons/spiritchampion.png");
            ConfigureSpirit(Guids.SpiritFocusGuardian, FeatName + "Guardian", Guids.Guardian, "assets/icons/spiritguardian.png");
            ConfigureSpirit(Guids.SpiritFocusHierophant, FeatName + "Hierophant", Guids.Hierophant, "assets/icons/spirithierophant.png");
            ConfigureSpirit(Guids.SpiritFocusMarshal, FeatName + "Marshal", Guids.Marshal, "assets/icons/spiritmarshal.png");
            ConfigureSpirit(Guids.SpiritFocusTrickster, FeatName + "Trickster", Guids.Trickster, "assets/icons/spirittrickster.png");

            // One "Spirit Focus" row in the general feat picker, opening the six
            // spirits. Not a class feature: it is a general feat.
            var selection = FeatureSelectionConfigurator.New(FeatName + "Selection", Guids.SpiritFocusSelection)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/tranceofthree.png")
                .SetHideInUI(false)
                .SetHideNotAvailibleInUI(false)
                .SetReapplyOnLevelUp(false)
                .AddToGroups(FeatureGroup.Feat)
                .AddComponent<PrerequisiteFeature>(c =>
                {
                    c.m_Feature = BlueprintTool.GetRef<BlueprintFeatureReference>(Guids.MediumSpiritBonus);
                })
                .AddToAllFeatures(Guids.SpiritFocusArchmage, Guids.SpiritFocusChampion, Guids.SpiritFocusGuardian, Guids.SpiritFocusHierophant, Guids.SpiritFocusMarshal, Guids.SpiritFocusTrickster)
                .SetAllowNonContextActions(false)
                .Configure();

            AddToGeneralFeats(selection);
        }

        // The Feat group alone never put this selection in the picker (0.3.0).
        // Both configurators share the PopulateSelections path, but
        // FeatureSelectionConfigurator.New calls For(bp, updateSelections: false)
        // while FeatureConfigurator.New passes true — so a selection is skipped no
        // matter which groups it carries. The group tag is kept because every
        // native selection in the pool carries it. Append once; the GUID check
        // keeps a single entry should BlueprintCore ever register it as well.
        private static void AddToGeneralFeats(BlueprintFeatureSelection selection)
        {
            var pool = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(BlueprintGuid.Parse(BasicFeatSelectionGuid));
            if (pool == null)
            {
                Logger.Log("General feat selection not found; Spirit Focus is not offered as a general feat.");
                return;
            }
            var features = pool.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>();
            if (features.Any(f => f?.deserializedGuid == selection.AssetGuid)) return;
            pool.m_AllFeatures = features.Concat(new[] { selection.ToReference<BlueprintFeatureReference>() }).ToArray();
        }
    }
}
