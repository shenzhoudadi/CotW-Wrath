using System.Reflection;
using System.Text.Json;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using MediumClass.Medium;
using MediumClass.Utils;
int checks = 0;
void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
void Same(string want, string got, string label) { checks++; if (want != got) throw new Exception(label + "\n  want: " + want + "\n  got:  " + got); }

// The shipped Medium.json is the template source, so the real wording is what gets rendered.
var shipped = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Medium.json"))).RootElement
    .EnumerateArray().ToDictionary(e => e.GetProperty("Key").GetString(), e => (En: e.GetProperty("enGB").GetString(), Zh: e.GetProperty("zhCN").GetString()));
void UseShippedText(bool chinese)
{
    LocalizationText.Chinese = chinese;
    BlueprintCore.Utils.LocalizationTool.Lookup = key => shipped.TryGetValue(key, out var t) ? new() { Value = chinese ? t.Zh : t.En } : new() { Value = null };
}
var zhStats = new Dictionary<StatType, string> { [StatType.AdditionalAttackBonus] = "攻击", [StatType.SaveWill] = "意志豁免", [StatType.SkillPersuasion] = "交涉" };
var enStats = new Dictionary<StatType, string> { [StatType.AdditionalAttackBonus] = "Attack", [StatType.SaveWill] = "Will save", [StatType.SkillPersuasion] = "Persuasion" };

MessageLogThread Reset(bool chinese = true) {
 typeof(CombatLog).GetField("warned", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, false);
 Kingmaker.Game.Instance = new();
 UseShippedText(chinese);
 Kingmaker.Blueprints.Root.LocalizedTexts.Instance = new();
 Kingmaker.Blueprints.Root.LocalizedTexts.Instance.Stats.Lookup = s => (chinese ? zhStats : enStats)[s];
 Logging.Shared.OnWarning = null;
 Logging.Create = () => Logging.Shared;
 var thread = new MessageLogThread();
 LogThreadService.Instance = new() { AllThreads = new LogThreadBase[] { new(), thread } };
 return thread;
}
var recipient = new UnitEntityData { CharacterName = "B" };
SpiritSurgeReport Marshal(int[] sides, int[] results, int rank, int focus, bool concentration)
{
 var roll = new SurgeRoll(sides, results);
 var report = new SpiritSurgeReport(roll);
 report.Lines.Add(new() { Stats = new[] { StatType.SkillPersuasion }, Total = roll.Total });
 report.Lines.Add(new() { Stats = new[] { StatType.AdditionalAttackBonus, StatType.SaveWill }, MarshalOwner = "A", MarshalSpiritBonus = rank, MarshalFocus = focus, Total = roll.Total + rank + focus });
 if (concentration) report.Lines.Add(new() { IsConcentration = true, MarshalOwner = "A", MarshalSpiritBonus = rank, MarshalFocus = focus, Total = roll.Total + rank + focus });
 return report;
}

// --- the line shows dice type, individual dice, each term and the final bonus ---------------------------------
var thread = Reset();
CombatLog.SpiritSurge(recipient, Marshal(new[] { 6 }, new[] { 4 }, 2, 1, true));
Same("B 灵力涌动：1d6 = 4；交涉：骰点 4 = +4；攻击、意志豁免：骰点 4 + A 的统帅同调 2 + 专攻 1 = +7；专注：骰点 4 + A 的统帅同调 2 + 专攻 1 = +7",
    thread.Messages.Single().Text, "1d6 line names die, each modifier source and the final value (zh)");
thread = Reset();
CombatLog.SpiritSurge(recipient, Marshal(new[] { 8, 8 }, new[] { 3, 6 }, 3, 0, false));
Same("B 灵力涌动：2d8 = 3 + 6 = 9；交涉：骰点 3 + 6 = 9 = +9；攻击、意志豁免：骰点 3 + 6 = 9 + A 的统帅同调 3 = +12",
    thread.Messages.Single().Text, "2d8 shows both dice, not only the sum; zero focus is not listed");
thread = Reset(chinese: false);
CombatLog.SpiritSurge(recipient, Marshal(new[] { 6 }, new[] { 4 }, 2, 1, true));
Same("B Spirit Surge: 1d6 = 4; Persuasion: roll 4 = +4; Attack, Will save: roll 4 + A's Marshal spirit bonus 2 + spirit focus 1 = +7; Concentration: roll 4 + A's Marshal spirit bonus 2 + spirit focus 1 = +7",
    thread.Messages.Single().Text, "English wording");
thread = Reset();
var plain = new SpiritSurgeReport(new SurgeRoll(new[] { 10 }, new[] { 9 }));
plain.Lines.Add(new() { Stats = new[] { StatType.SaveWill }, Total = 9 });
CombatLog.SpiritSurge(recipient, plain);
Same("B 灵力涌动：1d10 = 9；意志豁免：骰点 9 = +9", thread.Messages.Single().Text, "no Marshal term, no invented modifiers");
Check(new SurgeRoll(new[] { 8, 8 }, new[] { 1, 1 }).Notation == "2d8" && new SurgeRoll(new[] { 6 }, new[] { 6 }).Notation == "1d6", "dice notation");

// --- empty and missing reports write nothing ----------------------------------------------------------------------
thread = Reset();
CombatLog.SpiritSurge(recipient, new SpiritSurgeReport(new SurgeRoll(new[] { 6 }, new[] { 3 })));
CombatLog.SpiritSurge(recipient, null);
Check(thread.Messages.Count == 0, "reports without lines or null do not write");

// --- a damaged or missing localized template falls back to the built-in wording ---------------------------
thread = Reset();
BlueprintCore.Utils.LocalizationTool.Lookup = _ => new() { Value = "{0}: {1}" };
CombatLog.SpiritSurge(recipient, Marshal(new[] { 6 }, new[] { 4 }, 2, 1, false));
Check(thread.Messages.Count == 1 && thread.Messages[0].Text.Contains("1d6 = 4") && thread.Messages[0].Text.Contains("+7"), "templates without the required placeholders use the built-in text");

// --- failure isolation (unchanged guarantees) -----------------------------------------------------------------------
var report0 = Marshal(new[] { 6 }, new[] { 4 }, 2, 1, true);
foreach (bool constructionFailure in new[] { false, true }) {
 Reset(); int warnings = 0;
 Logging.Shared.OnWarning = () => throw new Exception("fallback log failed");
 Logging.Create = () => { warnings++; if (constructionFailure) throw new Exception("logger unavailable"); return Logging.Shared; };
 BlueprintCore.Utils.LocalizationTool.Lookup = _ => throw new Exception("text lookup failed");
 Kingmaker.Blueprints.Root.LocalizedTexts.Instance.Stats.Lookup = _ => throw new Exception("stat text missing");
 var before = LogThreadService.Instance.AllThreads.OfType<MessageLogThread>().Single();
 CombatLog.SpiritSurge(recipient, report0);
 Check(before.Messages.Count == 1 && before.Messages[0].Text.Contains("AdditionalAttackBonus"), "lookup failures use fallbacks");
 // A throwing message thread makes the warning path run; it never escapes and is attempted once.
 before.BeforeAdd = () => throw new Exception("UI failed");
 CombatLog.SpiritSurge(recipient, report0);
 CombatLog.SpiritSurge(recipient, report0);
 Check(warnings == 1, "UI failure warning attempted once, and a failing fallback log never escapes");
}
var broken = Reset();
broken.BeforeAdd = () => throw new Exception("UI failed");
Logging.Create = () => new() { OnWarning = () => throw new Exception("fallback log failed") };
CombatLog.SpiritSurge(recipient, report0);
Check(broken.Messages.Count == 0, "UI and fallback failure return without a message");
Reset(); LogThreadService.Instance.AllThreads = Array.Empty<LogThreadBase>();
Logging.Create = () => throw new Exception("logger unavailable");
CombatLog.SpiritSurge(recipient, report0);
Check(true, "missing thread and logger failure do not escape");
Reset(); Logging.Create = () => throw new Exception("logger unavailable");
CombatLog.SpiritSurge(null, null);
Check(true, "null recipient and report with logger failure do not escape");
var brokenReport = new SpiritSurgeReport(new SurgeRoll(new[] { 6 }, new int[0]));
brokenReport.Lines.Add(new() { Stats = new[] { StatType.SaveWill } });
Reset(); CombatLog.SpiritSurge(recipient, brokenReport);
Check(true, "an inconsistent report (no dice) is contained");
var noPlayer = Reset(); Kingmaker.Game.Instance.Player = null;
CombatLog.SpiritSurge(recipient, report0);
Check(noPlayer.Messages.Count == 0, "no player does not write");
Console.WriteLine($"PASS: {checks} production combat-log checks (wording from the shipped Medium.json, dice/modifier detail, failure isolation); API doubles, no Unity rendering.");
