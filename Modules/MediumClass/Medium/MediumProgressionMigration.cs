using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCore.Utils;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium
{
    /// <summary>
    /// 0.3.4 recovery for characters created while <c>MediumProgression</c> had no class
    /// (<c>BlueprintProgression.CalcLevel</c> then returns the character level).
    ///
    /// Affected saves hold Medium level entries granted by TOTAL level: the level-1 kit can be
    /// missing and later entries (Propitation at "9", Spirit Power at "6" ...) can have been granted
    /// early by levels taken in other classes. Recovery is computed from what the unit actually
    /// holds: for every feature the number of ranks recorded with source MediumProgression is
    /// compared with the number the entries up to the real Medium level prescribe.
    ///
    /// * Missing ranks are added (facts only; no resource, use counter, spell slot or influence
    ///   value is touched or refilled).
    /// * Ranks recorded from MediumProgression that no entry up to the Medium level prescribes are
    ///   removed. Only features that appear in some level entry of the progression (any level,
    ///   including the archetype's own entries) are ever considered: the native level-up records the
    ///   progression as the source of feats picked through its selections too, and those picks, and
    ///   the selection facts themselves, must never be touched. Ranks with any other recorded source
    ///   (Prowler/Bloodrager, items) are never inspected as surplus.
    /// * Additions never exceed the feature's rank cap (a rank the game would refuse to add would
    ///   otherwise be planned again on every load).
    /// * Feature selections (Spirit Binder bonus feats) cannot be granted or revoked outside the
    ///   level-up UI, and a picked feat may already be a prerequisite of something else, so they
    ///   are reported and left untouched. See 修复说明.md for the retrain procedure.
    /// * The stored progression level is reset to the Medium class level so the next level-up
    ///   grants the right entry.
    ///
    /// Idempotent by construction: a second run finds every count equal and does nothing.
    /// </summary>
    internal static class MediumProgressionMigration
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumProgressionMigration));

        /// <summary>What one unit holds of one feature, as far as the Medium progression is concerned.</summary>
        internal readonly struct Held
        {
            internal Held(int rank, IReadOnlyList<int> mediumLevels, bool tracked)
            {
                Rank = rank;
                MediumLevels = mediumLevels;
                Tracked = tracked;
            }
            internal readonly int Rank;
            /// <summary>Levels of the ranks whose recorded source is MediumProgression.</summary>
            internal readonly IReadOnlyList<int> MediumLevels;
            /// <summary>False when the per-rank source list is missing or incomplete (very old saves).</summary>
            internal readonly bool Tracked;
        }

        internal sealed class Plan<T>
        {
            internal readonly List<(T Feature, int Level)> Add = new List<(T, int)>();
            internal readonly List<(T Feature, int Level)> Remove = new List<(T, int)>();
            internal readonly List<T> Unverifiable = new List<T>();
            internal bool IsEmpty => Add.Count == 0 && Remove.Count == 0;
        }

        /// <param name="mediumLevel">The character's Medium class level (never the total level).</param>
        /// <param name="entries">Features of every progression entry 1..mediumLevel, selections excluded.</param>
        /// <param name="held">Null when the unit does not own the feature at all.</param>
        /// <param name="mediumSourced">Owned features that carry a MediumProgression source, expected or not.</param>
        /// <param name="maxRanks">Rank cap of the feature blueprint.</param>
        internal static Plan<T> BuildPlan<T>(int mediumLevel, IEnumerable<(int Level, T Feature)> entries,
            Func<T, Held?> held, IEnumerable<T> mediumSourced, Func<T, int> maxRanks)
        {
            var plan = new Plan<T>();
            var expected = new Dictionary<T, List<int>>();
            foreach (var entry in entries.Where(e => e.Level >= 1 && e.Level <= mediumLevel).OrderBy(e => e.Level))
            {
                if (!expected.TryGetValue(entry.Feature, out var levels)) expected[entry.Feature] = levels = new List<int>();
                levels.Add(entry.Level);
            }
            var candidates = expected.Keys.Concat(mediumSourced).Distinct().ToList();
            foreach (var feature in candidates)
            {
                int cap = Math.Max(1, maxRanks(feature));
                var wanted = expected.TryGetValue(feature, out var list) ? list.Take(cap).ToList() : new List<int>();
                var have = held(feature);
                if (cap == 1)
                {
                    bool fromMedium = have.HasValue && have.Value.MediumLevels.Count > 0;
                    if (wanted.Count > 0 && !have.HasValue) plan.Add.Add((feature, wanted[0]));
                    else if (wanted.Count == 0 && fromMedium) plan.Remove.Add((feature, have.Value.MediumLevels[0]));
                    continue;
                }
                if (have.HasValue && !have.Value.Tracked)
                {
                    plan.Unverifiable.Add(feature);
                    continue;
                }
                var actual = have.HasValue ? have.Value.MediumLevels.OrderBy(l => l).ToList() : new List<int>();
                // Ranks recorded at exactly a prescribed level are settled; the rest are ranks granted
                // at the wrong level. Wrong-level ranks count toward the prescription (rank count is
                // what matters mechanically); only the excess is removed, highest level first.
                var unmatchedWanted = new List<int>(wanted);
                var loose = new List<int>();
                foreach (int level in actual)
                    if (!unmatchedWanted.Remove(level)) loose.Add(level);
                int need = unmatchedWanted.Count - loose.Count;
                // The game refuses ranks beyond the blueprint's cap (other sources may already fill it).
                int capacity = cap - (have.HasValue ? have.Value.Rank : 0);
                for (int i = 0; i < Math.Min(need, capacity); i++) plan.Add.Add((feature, unmatchedWanted[i]));
                for (int i = 0; i < -need; i++) plan.Remove.Add((feature, loose[loose.Count - 1 - i]));
            }
            return plan;
        }

        /// <summary>Entry point (UnitDescriptor.ApplyPostLoadFixes). Never throws into the load.</summary>
        internal static void Run(UnitDescriptor owner)
        {
            try
            {
                Apply(owner);
            }
            catch (Exception e)
            {
                try { Logger.LogException("Medium progression recovery failed; the save is loaded unchanged", e); }
                catch { /* logging is optional */ }
            }
        }

        private static Feature Find(UnitDescriptor owner, BlueprintFeature blueprint) =>
            owner.Progression.Features.Enumerable.FirstOrDefault(f => f.Blueprint == blueprint);

        private static void Apply(UnitDescriptor owner)
        {
            var unit = owner?.Unit;
            if (unit == null) return;
            var medium = BlueprintTool.Get<BlueprintCharacterClass>(Guids.Medium);
            var progression = BlueprintTool.Get<BlueprintProgression>(Guids.MediumProgression);
            if (medium == null || progression == null) return;
            int level = owner.Progression.GetClassLevel(medium);
            if (level < 1) return;
            var data = owner.Progression.GetProgression(progression);
            if (data == null) return;

            var entries = new List<(int Level, BlueprintFeature Feature)>();
            int skippedSelections = 0;
            for (int l = 1; l <= level; l++)
                foreach (var feature in data.GetLevelEntry(l).Features)
                {
                    // Selections and nested progressions need the level-up UI to make a choice.
                    if (feature is IFeatureSelection || feature is BlueprintProgression) { skippedSelections++; continue; }
                    if (feature is BlueprintFeature plain) entries.Add((l, plain));
                }

            // Features that belong to the progression itself, at any level and including the archetype's own entries.
            // Feats picked through a selection also record the progression as their source and are NOT in here.
            var own = new HashSet<BlueprintFeature>(progression.LevelEntries.Concat(data.LevelEntries)
                .SelectMany(e => e.Features).OfType<BlueprintFeature>()
                .Where(f => !(f is IFeatureSelection) && !(f is BlueprintProgression)));
            var sourced = owner.Progression.Features.Enumerable
                .Where(f => own.Contains(f.Blueprint) && (f.SourceProgression == progression
                    || (f.RankToSource != null && f.RankToSource.Any(s => s.Blueprint == progression))))
                .Select(f => f.Blueprint)
                .ToList();

            Held? Describe(BlueprintFeature blueprint)
            {
                var fact = Find(owner, blueprint);
                if (fact == null) return null;
                if (blueprint.Ranks <= 1)
                    return new Held(fact.Rank, fact.SourceProgression == progression
                        ? new[] { fact.SourceLevel } : Array.Empty<int>(), true);
                var ranks = fact.RankToSource;
                bool tracked = ranks != null && ranks.Count == fact.Rank;
                return new Held(fact.Rank, ranks == null ? Array.Empty<int>()
                    : ranks.Where(s => s.Blueprint == progression).Select(s => s.Level).ToList(), tracked);
            }

            var plan = BuildPlan(level, entries, Describe, sourced, f => f.Ranks);
            foreach (var (feature, source) in plan.Remove)
            {
                var fact = Find(owner, feature);
                if (fact == null) continue;
                if (fact.Rank > 1 && fact.RankToSource != null)
                {
                    // Native SetSource can record several ranks at the same source/level.
                    // This plan item removes one rank, so remove exactly one matching record.
                    int sourceIndex = fact.RankToSource.FindLastIndex(s => s.Blueprint == progression && s.Level == source);
                    if (sourceIndex < 0) continue;
                    fact.RankToSource.RemoveAt(sourceIndex);
                    fact.SetSourceSameAsLastRankToSourceValue();
                    fact.RemoveRank();
                }
                else unit.RemoveFact(feature);
                Logger.Log($"Recovery: removed {feature.name} granted at Medium progression level {source} (Medium class level {level}).");
            }
            foreach (var (feature, target) in plan.Add)
            {
                var fact = unit.AddFact(feature) as Feature;
                fact?.SetSource(progression, target);
                Logger.Log($"Recovery: granted missing {feature.name} of Medium level {target} (Medium class level {level}).");
            }
            foreach (var feature in plan.Unverifiable)
                Logger.Log($"Recovery: {feature.name} has no complete per-rank source record; left unchanged.");
            if (skippedSelections > 0)
                Logger.Log($"Recovery: {skippedSelections} feature selection(s) up to Medium level {level} are not auto-repaired (see 修复说明).");

            // Always align the stored level: CalcLevel now yields the Medium class level, and a stale
            // total-level value would make the next level-up skip its entry.
            if (data.Level != level) data.Level = level;
        }
    }
}
