// Stubs required to compile the linked production Guids.cs and LocalizationText.cs.
namespace MediumClass.Utils
{
    public static class Logging
    {
        public static UnityModManagerNet.UnityModManager.ModEntry.ModLogger GetLogger(string name) => new();
    }
}
namespace UnityModManagerNet
{
    public class UnityModManager { public class ModEntry { public class ModLogger { public void Log(string text) { } public void LogException(string text, Exception e) { } } } }
}
namespace MediumClass.Medium.Spirits.Archmage
{
    public static class Archmage { public const string ArchmageName = "Archmage"; }
}
namespace Kingmaker.Localization
{
    public static class LocalizationManager { public static Shared.Locale CurrentLocale = Shared.Locale.enGB; }
}
namespace Kingmaker.Localization.Shared
{
    public enum Locale { enGB, zhCN }
}
