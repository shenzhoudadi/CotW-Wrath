using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediumClass.Medium
{
    /// <summary>
    /// 4th-level Spirit Feat: one extra feat at 4/8/12/16/20.
    ///
    /// "An additional feat for which he qualifies" is the game's own feat pool, so the selection is
    /// a copy of the native BasicFeatSelection option list instead of a hand-written list: every
    /// feat the game ships (and every feat another mod adds to that list) is offered, and the
    /// ordinary prerequisite check decides whether the medium may take it. The copy is repeated in
    /// the delayed phase because other mods patch the native list after our blueprints exist.
    /// </summary>
    internal static class SpiritFeat
    {
        internal const string SelectionName = "MediumSpiritFeat";
        internal const int Picks = 5;
        internal static readonly int[] PickLevels = { 4, 8, 12, 16, 20 };

        private static readonly UnityModManagerNet.UnityModManager.ModEntry.ModLogger Logger =
            Logging.GetLogger(nameof(SpiritFeat));

        internal static void ConfigureEnabled()
        {
            var pool = NativePool();
            var selection = FeatureSelectionConfigurator
                .New(SelectionName, Guids.MediumSpiritFeat)
                .SetDisplayName("SpiritFeat.Name")
                .SetDescription("SpiritFeat.Description")
                .SetIcon("assets/icons/foregospirit.png")
                .SetIsClassFeature(true)
                .SetRanks(Picks)
                // Same group as the native feat selection, so the level-up screen files this step
                // with the other feat choices. (BlueprintCore's group auto-append only covers the
                // selections it knows about by reference, so late feats are picked up by the
                // delayed sync below instead.)
                .SetGroup(FeatureGroup.Feat)
                .SetIgnorePrerequisites(false)
                .SetReapplyOnLevelUp(false)
                .SetHideInUI(false)
                .SetAllFeatures(pool.Select(p => (Blueprint<BlueprintFeatureReference>)p).ToArray())
                .Configure();
            // Group is not None, so only m_AllFeatures is read at runtime; keep both lists in step
            // anyway (the affinity selection does the same).
            selection.m_Features = pool;
            Logger.Log("Spirit Feat: " + pool.Length + " feats at levels " + string.Join("/", PickLevels));
        }

        /// <summary>
        /// Late sync: other mods patch the native feat list while the blueprint cache is patched,
        /// which can be after our own configuration. Adding the newcomers keeps the pool equal to
        /// what a normal character can pick. Idempotent, append only.
        /// </summary>
        internal static void ConfigureDelayed()
        {
            try
            {
                var selection = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.MediumSpiritFeat);
                var native = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.NativeBasicFeatSelection);
                if (selection == null || native == null) return;

                var known = new HashSet<string>((selection.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                    .Where(r => r != null).Select(r => r.deserializedGuid.ToString()));
                var added = (native.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                    .Where(r => r != null && known.Add(r.deserializedGuid.ToString()))
                    .ToArray();
                if (added.Length == 0) return;

                var merged = (selection.m_AllFeatures ?? Array.Empty<BlueprintFeatureReference>())
                    .Concat(added).ToArray();
                selection.m_AllFeatures = merged;
                selection.m_Features = merged;
                Logger.Log("Spirit Feat: " + added.Length + " feat(s) added after the blueprint patches (pool "
                    + merged.Length + ").");
            }
            catch (Exception e)
            {
                // A changed optional API must never block character creation.
                Logger.Log("Spirit Feat: late pool sync skipped: " + e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>The native feat list, copied by reference so nothing has to be maintained here.</summary>
        private static BlueprintFeatureReference[] NativePool()
        {
            var native = BlueprintTool.Get<BlueprintFeatureSelection>(Guids.NativeBasicFeatSelection);
            if (native?.m_AllFeatures == null || native.m_AllFeatures.Length == 0)
            {
                Logger.Log("Spirit Feat: the native feat selection is unavailable; the pool stays empty.");
                return Array.Empty<BlueprintFeatureReference>();
            }
            return native.m_AllFeatures.ToArray();
        }
    }
}
