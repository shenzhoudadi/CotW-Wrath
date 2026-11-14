using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Kingmaker.UI.MVVM._VM.Settings;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;
using Kingmaker.Localization;

namespace CallOfTheWild.Wrath.Prototype
{
    public static class ModEntry
    {
        private static OwlcatModification modification;
        private static Harmony psychicHarmony;
        private static int registrationInProgress;
        private static bool deferredPatchesAttempted;
        private static readonly ConditionalWeakTable<BlueprintsCache, RegistrationRetry01444> registrations = new ConditionalWeakTable<BlueprintsCache, RegistrationRetry01444>();
        private static bool registrationCompleted;
        private static readonly ModMenuIntegration menu = new ModMenuIntegration();
        private static readonly Dictionary<string, string> moduleStates = ModuleCatalog.InitialStates();
        internal static ModuleStatus[] GetModuleStatuses() => ModuleCatalog.Snapshot(moduleStates);
        internal static void TraceNatureTarget(string line)=>modification?.Logger.Log(line);


        [OwlcatModificationEnterPoint]
        public static void Load(OwlcatModification mod)
        {
            if (modification != null) return;
            modification = mod;
            MediumIntegration.Prepare();
            WayOfTheShieldIntegration.Prepare();
            var harmonyAssembly = typeof(Harmony).Assembly;
            mod.Logger.Log("Harmony runtime: " + harmonyAssembly.FullName + "; path=" + harmonyAssembly.Location);
            CotW.Runtime.HarmonyRuntimeCompatibility.Verify();
            mod.Logger.Log("Harmony compile API baseline: 2.3.6.0; using the process-loaded compatible runtime without loading/replacing another copy.");
            var coreHarmony=new Harmony(mod.Manifest.UniqueName);
            InstallEarlyCorePatches(coreHarmony);
            psychicHarmony=new Harmony(mod.Manifest.UniqueName+".Psychic");
            try { InstallEarlyPsychicPatches(psychicHarmony); }
            catch(Exception exception) {
                psychicHarmony.UnpatchAll(psychicHarmony.Id); PsychicCasting.RuntimePatchesAvailable=false;
                mod.Logger.Error("Psychic runtime patches failed; existing content remains available.\n"+exception);
            }
            mod.Logger.Log("Experimental CotW port " + mod.Manifest.Version + ": seventeen independent feats and two magus arcana, including Artful Dodge, Osyluth Guile, Felling Smash, Devastating Strike and Feral Grace; eight rage power choices, Psychic casting, 133 primary migrated spell roots plus 3 optional Summoner roots, Ferocity/Faith/Pageantry, nine phrenic amplifications and Phrenic Mastery, bundled Medium, optional spell links and optional Brawler integration. Restart required for changes.");
            // Normally the native entry point runs before cache.Init(); handle late entry too.
            if (BlueprintRegistration.CacheIsReady(ResourcesLibrary.BlueprintsCache))
                Register(ResourcesLibrary.BlueprintsCache);
        }

        internal static void InstallEarlyCorePatches(Harmony harmony)
        {
            foreach (var type in PatchTypes().Where(t => !IsPsychicPatch(t)))
                harmony.CreateClassProcessor(type).Patch();
        }

        internal static void InstallEarlyPsychicPatches(Harmony harmony)
        {
            // Mono can initialize a static target when Harmony gets its address.
            // Install UIUtilityTexts patches only after the blueprint cache is ready.
            foreach (var type in PatchTypes().Where(t => IsPsychicPatch(t) && !PsychicUiPatches.IsDeferred(t)))
                harmony.CreateClassProcessor(type).Patch();
        }

        private static IEnumerable<Type> PatchTypes() => Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Any());
        private static bool IsPsychicPatch(Type type) => type.DeclaringType == typeof(TopplingSpell) || type == typeof(PsychicSpiritualAllyFlankingPatch) || type == typeof(PsychicEyeControlPatch) || type == typeof(PsychicSpiritAttackLogPatch) || type == typeof(PsychicEyeClickPatch) || type == typeof(PsychicEyeHoverPatch) || type.DeclaringType == typeof(PsychicCasting) ||
            type.DeclaringType == typeof(PsychicAmplificationMenu) ||
            type.DeclaringType == typeof(PsychicUndercasting) ||
            type.DeclaringType == typeof(PsychicStoragePatch) || type.DeclaringType == typeof(PsychicLinkedWardPatches) || type == typeof(PsychicHexCooldownPatch) || type == typeof(PsychicHexTargetRestrictionsPatch) || type == typeof(PsychicFlightGroundPatch) || type == typeof(PsychicEtherMissPatch) || type == typeof(PsychicWindTravelPatch) || type == typeof(PsychicWallBlindnessDurationPatch) || type == typeof(PsychicRelentlessCastingPatch) || type == typeof(PsychicOngoingDefensePatch) || type == typeof(PsychicSynesthesiaConcealmentPatch) || type == typeof(PsychicDivideMindPatch) || type == typeof(PsychicDivideMindActionPatch) || type == typeof(PsychicInvigorateChargePatch) || type.DeclaringType == typeof(PsychicGlobeSpells) || type.DeclaringType == typeof(PsychicMawCleanup) || type.DeclaringType == typeof(PsychicBladeTutor) || type.DeclaringType == typeof(PsychicGhostbaneDirge);

        private static void ApplyLocalization()
        {
            if (modification == null || LocalizationManager.CurrentPack == null) return;
            try { modification.ApplyLocalization(); PsychicAssets.ApplyLocalization(); WayOfTheShieldIntegration.ApplyLocalization(); PsychicAssets.ApplyQLLocalization(); MimicCatalogue01461.ApplyLabels(); menu.RefreshLocalization(); }
            catch (Exception exception)
            {
                modification.Logger.Error("Failed to apply prototype localization after initialization: " + exception);
            }
        }

        private static void Register(BlueprintsCache cache)
        {
            if (modification == null || cache == null || Interlocked.CompareExchange(ref registrationInProgress, 1, 0) != 0) return;
            try {
                var state = registrations.GetValue(cache, _ => new RegistrationRetry01444());
                if (!deferredPatchesAttempted && PsychicCasting.RuntimePatchesAvailable)
                {
                    deferredPatchesAttempted = true;
                    try
                    {
                        PsychicUiPatches.Install(psychicHarmony);
                        modification.Logger.Log("Psychic UI patches installed after blueprint cache initialization.");
                    }
                    catch (Exception exception)
                    {
                        psychicHarmony.UnpatchAll(psychicHarmony.Id);
                        PsychicCasting.RuntimePatchesAvailable = false;
                        modification.Logger.Error("Deferred Psychic UI patches failed; existing content remains available.\n" + exception);
                    }
                }
                if (!state.InitialDispatchCompleted) {
                    BlueprintRegistration.RegisterInitial(cache, state, ReportModule);
                    state.Run(MediumIntegration.ModuleName, () => MediumIntegration.Register(cache), ReportModule);
                    state.Run(WayOfTheShieldIntegration.ModuleName, () => WayOfTheShieldIntegration.Register(cache), ReportModule);
                    state.InitialDispatchCompleted = true;
                } else if (state.PendingCount != 0) BlueprintRegistration.RetryInitial(state, ReportModule);
                registrationCompleted = true;
                // The wall preview owns a persistent GameObject rather than any
                // blueprint, so it is installed once, outside the registration
                // transaction, and must never abort registration.
                try { AreaPreview0293.Install(message => modification.Logger.Log(message)); }
                catch (Exception exception) { modification.Logger.Error("Wall AoE preview install failed: " + exception); }
                BlueprintRegistration.PublishedGraphChecksRelaxed = true;
                WriteDiagnostics(state.PendingCount == 0 ? "Initial registration complete." : "Initial registration completed with pending modules; later ready/settings callbacks can retry them.");
                TryMenu();
            } catch (Exception exception) {
                // Keep committed successes. If traversal itself was interrupted,
                // the next callback resumes RegisterAll with those names skipped.
                modification.Logger.Error("Registration pass interrupted; unfinished modules remain retryable.\n" + exception);
            } finally { Volatile.Write(ref registrationInProgress, 0); }
        }

        private static void ReportModule(string name, bool succeeded, Exception exception)
        {
            var state = succeeded ? "Registered" : "Failed";
            if (moduleStates.TryGetValue(name, out var old) && old == state) return;
            moduleStates[name] = state;
            var line = (succeeded ? "Registered: " : "Registration failed: ") + name;
            if (succeeded) modification.Logger.Log(line);
            else modification.Logger.Error(line + "\n" + exception);
        }

        private static void WriteDiagnostics(string message)
        {
            modification.Logger.Log(message);
            modification.Logger.Log(PsychicSpellIntegration.LastSummary);
            try { modification.Logger.Log(ContentIdentity.Describe(ResourcesLibrary.BlueprintsCache)); }
            catch (Exception exception) { modification.Logger.Error("Compatibility inventory failed: " + exception); }
        }

        internal static void LogSpellDiagnostic(string message)
        {
            // A missing logger must not affect learning (including standalone tests).
            try { modification?.Logger.Log(message); } catch { }
        }

        private static void ReconcileOptional()
        {
            if (modification == null || Volatile.Read(ref registrationInProgress) != 0) return;
            var cache = ResourcesLibrary.BlueprintsCache;
            if (cache == null) return;
            if (!registrations.TryGetValue(cache, out var state)) {
                if (!BlueprintRegistration.CacheIsReady(cache)) return;
                Register(cache);
            } else if (!state.InitialDispatchCompleted || state.PendingCount != 0) Register(cache);
            if (!registrationCompleted || Volatile.Read(ref registrationInProgress) != 0) return;
            // A provider may have registered after our cache postfix. Repair only
            // the optional lists after retrying failed initial publications.
            // Existing successful provider reconciliation remains unchanged.
            if (Interlocked.CompareExchange(ref registrationInProgress, 1, 0) != 0) return;
            registrations.TryGetValue(cache, out state);
            try { BlueprintRegistration.RegisterOptional(cache, (name, succeeded, error) => {
                if (succeeded) state?.Complete(name);
                ReportModule(name, succeeded, error);
            }); }
            catch (Exception exception) { modification.Logger.Error("Optional integration retry failed: " + exception); }
            finally { Volatile.Write(ref registrationInProgress, 0); }
        }

        private static void TryMenu()
        {
            if (modification == null || !registrationCompleted) return;
            RegisterMenu(menu, ModMenuIntegration.FindAssembly(), modification, modification.Manifest.Version,
                WriteDiagnostics, message => modification.Logger.Error(message));
        }

        internal static void RegisterMenu(ModMenuIntegration integration, Assembly assembly, OwlcatModification mod,
            string version, Action<string> log, Action<string> error) =>
            integration.TryRegister(assembly, mod, version, GetModuleStatuses(), log, error);

        // Runs before ModMenu's SettingsVM postfix builds its dropdown. Also handles a
        // dependency loaded after the blueprint cache, without running Unity on a worker thread.
        [HarmonyPatch]
        private static class SettingsOpeningPatch
        {
            private static MethodBase TargetMethod() => typeof(SettingsVM)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
            [HarmonyPrefix]
            private static void Prefix() { ReconcileOptional(); TryMenu(); }
        }

        [HarmonyPatch(typeof(LocalizationManager), "OnLocaleChanged")]
        private static class MenuLocalePatch
        {
            [HarmonyPostfix]
            private static void Postfix() { ApplyLocalization(); TryMenu(); }
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        private static class CacheReadyPatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(BlueprintsCache __instance) => Register(__instance);
        }

        // The native mod manager can start before async localization has completed.
        // Reapply this mod's strings after the final pack has been installed.
        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.WaitForInit))]
        private static class LocalizationReadyPatch
        {
            [HarmonyPostfix]
            private static void Postfix() { ApplyLocalization(); ReconcileOptional(); TryMenu(); }
        }
    }
}
