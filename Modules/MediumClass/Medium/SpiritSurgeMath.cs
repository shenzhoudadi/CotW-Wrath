using System;
using System.Linq;

namespace MediumClass.Medium
{
    /// <summary>The dice of one Spirit Surge: their type and each individual result.</summary>
    internal sealed class SurgeRoll
    {
        internal SurgeRoll(int[] sides, int[] results, int[] firstDraws = null, int[] secondDraws = null)
        {
            Sides = sides;
            Results = results;
            FirstDraws = firstDraws;
            SecondDraws = secondDraws;
        }

        /// <summary>Faces of each die, in roll order.</summary>
        internal int[] Sides { get; }
        /// <summary>Result of each die, in roll order.</summary>
        internal int[] Results { get; }
        /// <summary>First roll of each die when Assured Surge rerolled it; null for plain surges.</summary>
        internal int[] FirstDraws { get; }
        /// <summary>Second roll of each die when Assured Surge rerolled it; null for plain surges.</summary>
        internal int[] SecondDraws { get; }
        internal bool HasRerolls =>
            FirstDraws != null && SecondDraws != null
            && FirstDraws.Length == Results.Length && SecondDraws.Length == Results.Length;
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

        /// <summary>
        /// Same die rules as <see cref="RollDice(int, bool, bool, bool, Func{int, int})"/>, also
        /// recording both rolls per die when Assured Surge applies so the combat log can show them.
        /// </summary>
        internal static SurgeRoll RollDice(int level, bool prowler, bool hardenedSoul, bool legendaryMarshal, Random random, bool assuredSurge)
        {
            if (!assuredSurge)
                return RollDice(level, prowler, hardenedSoul, legendaryMarshal, sides => Draw(sides, random, false));

            int[] sidesList = legendaryMarshal ? new[] { 6 }
                : hardenedSoul ? new[] { 8, 8 }
                : prowler ? new[] { 6 }
                : new[] { level >= 20 ? 10 : level >= 10 ? 8 : 6 };
            var results = new int[sidesList.Length];
            var firsts = new int[sidesList.Length];
            var seconds = new int[sidesList.Length];
            for (int i = 0; i < sidesList.Length; i++)
            {
                int first = random.Next(1, sidesList[i] + 1);
                int second = random.Next(1, sidesList[i] + 1);
                firsts[i] = first;
                seconds[i] = second;
                results[i] = Math.Max(first, second);
            }
            return new SurgeRoll(sidesList, results, firsts, seconds);
        }

        /// <summary>Total only. Kept for callers that need just the number; it still draws every die.</summary>
        internal static int Roll(int level, bool prowler, bool hardenedSoul, bool legendaryMarshal, Func<int, int> die) =>
            RollDice(level, prowler, hardenedSoul, legendaryMarshal, die).Total;

        /// <summary>
        /// One die. Assured Surge (Spirit Affinity) rolls twice and keeps the higher result; it is a
        /// per-die reroll, so two dice still produce two independent results.
        /// </summary>
        internal static int Draw(int sides, Random random, bool assuredSurge)
        {
            int first = random.Next(1, sides + 1);
            if (!assuredSurge) return first;
            return Math.Max(first, random.Next(1, sides + 1));
        }

        private static SurgeRoll Single(int sides, Func<int, int> die) => new SurgeRoll(new[] { sides }, new[] { die(sides) });
    }
}
