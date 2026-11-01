using System;
using System.Linq;

namespace MediumClass.Medium
{
    /// <summary>The dice of one Spirit Surge: their type and each individual result.</summary>
    internal sealed class SurgeRoll
    {
        internal SurgeRoll(int[] sides, int[] results)
        {
            Sides = sides;
            Results = results;
        }

        /// <summary>Faces of each die, in roll order.</summary>
        internal int[] Sides { get; }
        /// <summary>Result of each die, in roll order.</summary>
        internal int[] Results { get; }
        internal int Total => Results.Sum();

        /// <summary>"1d6", "2d8". Mixed dice would list each group; the surge only ever uses one type.</summary>
        internal string Notation => string.Join(" + ", Sides.GroupBy(s => s).Select(g => g.Count() + "d" + g.Key));

        /// <summary>True when the stored dice are internally consistent (guards against a damaged save).</summary>
        internal static bool IsValid(int[] sides, int[] results)
        {
            if (sides == null || results == null || sides.Length == 0 || sides.Length != results.Length) return false;
            for (int i = 0; i < sides.Length; i++)
                if (sides[i] < 2 || results[i] < 1 || results[i] > sides[i]) return false;
            return true;
        }
    }

    internal static class SpiritSurgeMath
    {
        /// <summary>
        /// One surge = one call. Legendary Marshal has its own fixed die. Hardened Soul replaces the
        /// ordinary surge with two independent eight-sided dice. The dice are drawn left to right,
        /// once each; callers keep the returned roll and never draw again for the same effect.
        /// </summary>
        internal static SurgeRoll RollDice(int level, bool prowler, bool hardenedSoul, bool legendaryMarshal, Func<int, int> die)
        {
            if (legendaryMarshal) return Single(6, die);
            if (hardenedSoul) return new SurgeRoll(new[] { 8, 8 }, new[] { die(8), die(8) });
            if (prowler) return Single(6, die);
            return Single(level >= 20 ? 10 : level >= 10 ? 8 : 6, die);
        }

        /// <summary>Total only. Kept for callers that need just the number; it still draws every die.</summary>
        internal static int Roll(int level, bool prowler, bool hardenedSoul, bool legendaryMarshal, Func<int, int> die) =>
            RollDice(level, prowler, hardenedSoul, legendaryMarshal, die).Total;

        private static SurgeRoll Single(int sides, Func<int, int> die) => new SurgeRoll(new[] { sides }, new[] { die(sides) });
    }
}
