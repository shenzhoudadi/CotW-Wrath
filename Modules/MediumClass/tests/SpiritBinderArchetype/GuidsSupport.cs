// The linked production Guids.cs needs only these three symbols to compile.
namespace MediumClass.Utils
{
    public static class Logging
    {
        public static UnityModManagerNet.UnityModManager.ModEntry.ModLogger GetLogger(string name) => new();
    }
}
namespace UnityModManagerNet
{
    public class UnityModManager { public class ModEntry { public class ModLogger { } } }
}
namespace MediumClass.Medium.Spirits.Archmage
{
    public static class Archmage { public const string ArchmageName = "Archmage"; }
}
