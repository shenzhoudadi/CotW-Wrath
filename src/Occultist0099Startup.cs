using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0099 ordinary startup registration. The class graph is self
    // contained, so a missing required donor, a private identity already owned
    // by another asset, or an unexpected cache state must surface as a module
    // failure that the shared registration retry can revisit; a late optional
    // donor (DLC ownership, a third-party table) is picked up by the next pass
    // because the same transaction recomputes the graph and re-runs
    // UpdateSpellLists on the published one.
    internal static class Occultist0099Startup
    {
        internal const string ModuleName = "Occultist (COTW-0099)";

        internal static void Register(BlueprintsCache cache)
        {
            if (!Occultist0099Slice.StartupEnabled()) return;
            if (cache == null)
                throw new InvalidOperationException("Occultist registration received no blueprint cache.");
            bool registered = ReferenceEquals(cache, ResourcesLibrary.BlueprintsCache)
                ? Occultist0099NativeRegistration.RegisterShared(cache)
                : Occultist0099NativeRegistration.RegisterInDetachedCache(cache);
            if (!registered)
                throw new InvalidOperationException(
                    "Occultist registration declined: a required donor, a private identity or the cache state is not ready; the module stays retryable.");
        }

        internal static void ReconcileOptional(BlueprintsCache cache, Action<string, bool, Exception> report)
        {
            if (!Occultist0099Slice.StartupEnabled() || cache == null ||
                !Occultist0099NativeRegistration.IsPublished(cache)) return;
            try
            {
                Register(cache);
                report?.Invoke(ModuleName, true, null);
            }
            catch (Exception error)
            {
                report?.Invoke(ModuleName, false, error);
            }
        }
    }
}
