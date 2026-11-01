using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using MediumClass.Medium;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Utils
{
    // Writes mod lines to the on-screen combat log. Every failure is swallowed
    // and reported once to the UMM log: a log line must never break the
    // mechanic that reports it.
    internal static class CombatLog
    {
        private const string SpiritSurgeKey = "SpiritSurge.CombatLog";
        private static bool warned;

        /// <summary>
        /// One line per surge: recipient, dice type and every individual die, then per target group the
        /// terms that add up to the final bonus (roll, granting Marshal's spirit bonus, spirit focus).
        /// The report is the same object the modifiers were applied from.
        /// </summary>
        internal static void SpiritSurge(UnitEntityData unit, SpiritSurgeReport report)
        {
            try
            {
                if (report == null || report.Lines.Count == 0) return;
                var roll = report.Roll;
                // "4" for one die, "3 + 6 = 9" for several: the dice are always visible, never only the sum.
                string faces = roll.Results.Length == 1
                    ? roll.Results[0].ToString()
                    : string.Join(" + ", roll.Results) + " = " + roll.Total;
                var segments = report.Lines.Select(line => Segment(line, faces));
                var header = Template(SpiritSurgeKey, "{0} Spirit Surge: {1} = {2}; {3}", "{0} 灵力涌动：{1} = {2}；{3}", 0, 1, 2, 3);
                Write(string.Format(header, unit?.CharacterName ?? "", roll.Notation, faces,
                    string.Join(LocalizationText.Get("; ", "；"), segments)));
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        private static string Segment(SpiritSurgeReport.Line line, string faces)
        {
            string label = line.IsConcentration
                ? Template(SpiritSurgeKey + ".Concentration", "Concentration", "专注")
                : string.Join(LocalizationText.Get(", ", "、"), line.Stats.Select(StatName));
            var terms = new List<string> { string.Format(Template(SpiritSurgeKey + ".Roll", "roll {0}", "骰点 {0}", 0), faces) };
            if (line.MarshalOwner != null && line.MarshalSpiritBonus != 0)
                terms.Add(string.Format(Template(SpiritSurgeKey + ".MarshalBonus", "{0}'s Marshal spirit bonus {1}", "{0} 的统帅同调 {1}", 0, 1),
                    line.MarshalOwner, line.MarshalSpiritBonus));
            if (line.MarshalOwner != null && line.MarshalFocus != 0)
                terms.Add(string.Format(Template(SpiritSurgeKey + ".Focus", "spirit focus {0}", "专攻 {0}", 0), line.MarshalFocus));
            return string.Format(Template(SpiritSurgeKey + ".Line", "{0}: {1} = +{2}", "{0}：{1} = +{2}", 0, 1, 2),
                label, string.Join(" + ", terms), line.Total);
        }

        // A localized template is used only if it carries every placeholder the code fills in and formats
        // cleanly with exactly those arguments; otherwise (missing key, damaged text) the built-in wording
        // is used instead, so a bad text can neither drop a value nor make string.Format throw.
        private static string Template(string key, string english, string chinese, params int[] required)
        {
            var text = Text(key);
            if (!string.IsNullOrEmpty(text) && required.All(i => text.Contains("{" + i + "}")) && FormatsWith(text, required.Length))
                return text;
            return LocalizationText.Get(english, chinese);
        }

        private static bool FormatsWith(string text, int arguments)
        {
            try
            {
                string.Format(text, Enumerable.Repeat((object)"x", arguments).ToArray());
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Same lookup as Settings.GetString; a missing key falls back to the caller.
        private static string Text(string key)
        {
            try
            {
                LocalizedString text = LocalizationTool.GetString(key);
                return text?.ToString();
            }
            catch
            {
                return null;
            }
        }

        // The game's own stat names in the current UI language (the same table
        // TabletopTweaks-Core reads); the enum name only if the table has none.
        private static string StatName(StatType stat)
        {
            try
            {
                var name = LocalizedTexts.Instance?.Stats?.GetText(stat);
                return string.IsNullOrEmpty(name) ? stat.ToString() : name;
            }
            catch
            {
                return stat.ToString();
            }
        }

        // Checked against the game assembly: LogThreadService.Instance and
        // AllThreads are public; AddMessage is protected on LogThreadBase and
        // reachable through the publicized Assembly-CSharp, like the protected
        // OnTurnOn overrides elsewhere in this mod. The first MessageLogThread is
        // the plain-text one Setup() registers for the Common channel.
        private static void Write(string text)
        {
            if (Game.Instance?.Player == null) return;
            var thread = LogThreadService.Instance?.AllThreads?.OfType<MessageLogThread>().FirstOrDefault();
            if (thread == null)
            {
                Warn(new InvalidOperationException("No message log thread; Spirit Surge rolls are not shown on screen."));
                return;
            }
            // A zeroed Color is transparent, so the text colour is set explicitly.
            thread.AddMessage(new CombatLogMessage(text, Color.white, PrefixIcon.None));
        }

        private static void Warn(Exception e)
        {
            if (warned) return;
            warned = true;
            try
            {
                Logging.GetLogger(nameof(CombatLog)).LogException("Could not write to the combat log", e);
            }
            catch
            {
                // The fallback log is optional too; no logging error may escape.
            }
        }
    }
}
