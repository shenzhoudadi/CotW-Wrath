using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace CallOfTheWild.Wrath.Prototype
{
    // The native icon fallback for a fact without m_Icon is "placeholder sprite
    // plus acronym". Both come from the ASSET name, never the display name:
    // BlueprintUnitFact.NameForAcronym => name, and UIUtility.GetAbilityAcronym
    // keeps the first two letters it finds in upper case.
    //
    // Every asset this project publishes is named "CotWWrathPsychic" + token, so
    // the extractor reads "C W W P S C C ..." and truncates to "CW": every
    // icon-less fact in the mod showed the same two letters, which reads as one
    // shared icon. The names are load-bearing (many modules compare
    // name == "CotWWrathPsychic" + token when reconciling), so the fix rewrites
    // only the letters the UI draws.
    internal static class AcronymPresentation
    {
        private const string Prefix = "CotWWrathPsychic";

        // Class tokens carry no information once the class is known and would
        // collapse every acronym back to the same letters. Longest first.
        private static readonly string[] ClassPrefixes = { "SublimeChord" };

        private static readonly Dictionary<string, string> Cache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly object Gate = new object();

        [HarmonyPatch(typeof(Kingmaker.UI.Common.UIUtility), nameof(Kingmaker.UI.Common.UIUtility.GetAbilityAcronym),
            new[] { typeof(string) })]
        private static class AcronymPatch
        {
            private static void Postfix(string __0, ref string __result)
            {
                var acronym = For(__0);
                if (acronym != null) __result = acronym;
            }
        }

        // Returns null for anything that is not one of our asset names, so every
        // native fact (and every localized display name) keeps its own acronym.
        private static string For(string text)
        {
            if (text == null || text.Length <= Prefix.Length) return null;
            if (!text.StartsWith(Prefix, StringComparison.Ordinal)) return null;
            lock (Gate)
            {
                if (Cache.TryGetValue(text, out var cached)) return cached;
                var acronym = Build(text.Substring(Prefix.Length));
                Cache[text] = acronym;
                return acronym;
            }
        }

        private static string Build(string token)
        {
            // A token may carry a '/'-separated identity suffix (the per-book
            // picker features are "SublimeChordBook/<guid>"). Only the head is a
            // name; the GUID would otherwise be split into junk words.
            int cut = token.IndexOf('/');
            if (cut >= 0) token = token.Substring(0, cut);
            foreach (var prefix in ClassPrefixes)
                if (token.Length > prefix.Length && token.StartsWith(prefix, StringComparison.Ordinal))
                {
                    token = token.Substring(prefix.Length);
                    break;
                }
            var words = Words(token);
            if (words.Count == 0) return null;
            var result = new StringBuilder(3);
            if (words.Count == 1)
            {
                // Single word: the first three letters read better than one.
                for (int i = 0; i < words[0].Length && result.Length < 3; i++)
                    if (char.IsLetterOrDigit(words[0][i])) result.Append(char.ToUpperInvariant(words[0][i]));
                return result.Length == 0 ? null : result.ToString();
            }
            // More than three words: the distinguishing part sits at the tail
            // (Casting / Ability / Feature suffixes are shared by siblings).
            int start = words.Count > 3 ? words.Count - 3 : 0;
            for (int i = start; i < words.Count && result.Length < 3; i++)
                result.Append(char.ToUpperInvariant(words[i][0]));
            return result.Length == 0 ? null : result.ToString();
        }

        // CamelCase and digit boundaries, with separators (a token may carry a
        // '/'-separated suffix) treated as word breaks.
        private static List<string> Words(string token)
        {
            var words = new List<string>();
            var current = new StringBuilder();
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (!char.IsLetterOrDigit(c))
                {
                    if (current.Length > 0) { words.Add(current.ToString()); current.Length = 0; }
                    continue;
                }
                if (current.Length > 0)
                {
                    char previous = current[current.Length - 1];
                    bool digit = char.IsDigit(c);
                    if (char.IsDigit(previous) != digit ||
                        (!digit && char.IsUpper(c) && !char.IsUpper(previous)))
                    {
                        words.Add(current.ToString());
                        current.Length = 0;
                    }
                }
                current.Append(c);
            }
            if (current.Length > 0) words.Add(current.ToString());
            return words;
        }
    }
}
