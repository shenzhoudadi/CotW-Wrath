namespace Kingmaker { public class Game { public static Game Instance = new(); public object Player = new(); } }
namespace Kingmaker.EntitySystem.Entities { public class UnitEntityData { public string CharacterName = "Tester"; } }
namespace Kingmaker.EntitySystem.Stats { public enum StatType { AdditionalAttackBonus, SaveWill, SkillPersuasion } }
namespace Kingmaker.Localization { public class LocalizedString { public string Value; public override string ToString() => Value; } }
namespace BlueprintCore.Utils { public static class LocalizationTool { public static Func<string, Kingmaker.Localization.LocalizedString> Lookup = _ => new() { Value = "{0}: {1}" }; public static Kingmaker.Localization.LocalizedString GetString(string key) => Lookup(key); } }
namespace Kingmaker.Blueprints.Root { public class LocalizedTexts { public static LocalizedTexts Instance = new(); public StatsStrings Stats = new(); } public class StatsStrings { public Func<Kingmaker.EntitySystem.Stats.StatType, string> Lookup = s => s.ToString(); public string GetText(Kingmaker.EntitySystem.Stats.StatType stat) => Lookup(stat); } }
namespace UnityEngine { public struct Color { public static Color white => new(); } }
namespace Kingmaker.UI.Models.Log.CombatLog_ThreadSystem {
 public enum PrefixIcon { None }
 public class CombatLogMessage { public string Text; public CombatLogMessage(string text, UnityEngine.Color color, PrefixIcon icon, object template = null, bool hasTooltip = true) { Text = text; } }
 public class LogThreadBase { public readonly List<CombatLogMessage> Messages = new(); public Action BeforeAdd; public void AddMessage(CombatLogMessage message) { BeforeAdd?.Invoke(); Messages.Add(message); } }
 public class LogThreadService { public static LogThreadService Instance = new(); public IEnumerable<LogThreadBase> AllThreads = Array.Empty<LogThreadBase>(); }
}
namespace Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common { public class MessageLogThread : Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreadBase { } }
namespace UnityModManagerNet { public class UnityModManager { public class ModEntry { public class ModLogger { public Action OnWarning; public void LogException(string text, Exception e) => OnWarning?.Invoke(); } } } }
namespace MediumClass.Utils {
 public static class Logging { public static readonly UnityModManagerNet.UnityModManager.ModEntry.ModLogger Shared = new(); public static Func<UnityModManagerNet.UnityModManager.ModEntry.ModLogger> Create = () => Shared; public static UnityModManagerNet.UnityModManager.ModEntry.ModLogger GetLogger(string name) => Create(); }
 public static class LocalizationText { public static bool Chinese; public static string Get(string english, string chinese) => Chinese ? chinese : english; }
}
