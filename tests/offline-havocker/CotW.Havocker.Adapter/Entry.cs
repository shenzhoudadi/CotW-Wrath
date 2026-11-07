// This local test adapter does not invoke CotW's full historical registration.
namespace CotW.Havocker.Adapter
{
    public static class Entry
    {
        public static bool Load(UnityModManagerNet.UnityModManager.ModEntry entry) => entry != null;
    }
}
