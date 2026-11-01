using BlueprintCore.Blueprints.Configurators.Root;
using BlueprintCore.Utils;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager.ModEntry;
using Kingmaker.PubSubSystem;
using MediumClass.Utils;
using MediumClass.Features.MediumSpecific;

namespace MediumClass
{
    public static class Main
    {
        public static bool Enabled;
        public static bool InitializationFailed { get; private set; }
        public static bool Integrated { get; private set; }
        private static BlueprintCacheInitHandler integratedHandler;
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(Main));
        //        private static readonly LogWrapper Logger = LogWrapper.Get("AddedFeats");

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            // This assembly keeps its historical name for serialized components, but
            // has a single owner and is no longer a separately installed UMM mod.
            Logger.Log("Medium is bundled with Call of the Wild. Disable the standalone Medium Class entry and restart.");
            return false;
        }

        public static void PrepareIntegrated()
        {
            if (integratedHandler != null) return;
            Integrated = true;
            var harmony = new Harmony("CallOfTheWildWrathPrototype.Medium");
            try {
                InstallEarlyPatches(harmony);
                integratedHandler = new BlueprintCacheInitHandler(harmony);
            } catch {
                InitializationFailed = true;
                harmony.UnpatchAll(harmony.Id);
                throw;
            }
        }

        public static void ConfigureIntegrated()
        {
            if (integratedHandler == null) throw new InvalidOperationException("Medium integration was not prepared.");
            // Native mod late entry can occur after cache.Init. The bundled BC
            // loader is guarded internally; replay its asset setup in that case.
            var assets = typeof(BlueprintCore.Utils.Assets.AssetTool).GetNestedType("BlueprintsCaches_Patch", BindingFlags.NonPublic);
            AccessTools.Method(assets, "Prefix").Invoke(null, null);
            // CotW calls this after its shared psychic blueprints have registered.
            integratedHandler.AfterBlueprintCacheInit();
            integratedHandler.AfterBlueprintCachePatches();
            if (InitializationFailed) throw new InvalidOperationException("Medium blueprint registration failed. See the first MediumClass exception.");
            Enabled = true;
        }

        private static bool LegacyLoad(UnityModManager.ModEntry modEntry)
        {
            Harmony harmony = null;
            BlueprintCacheInitHandler handler = null;
            try
            {
                harmony = new Harmony(modEntry.Info.Id);
                // Blueprint mutations cannot be safely undone during a running game.
                modEntry.OnGUI = _ => GUILayout.Label(InitializationFailed
                    ? LocalizationText.Get("Medium Class initialization FAILED. Read the first MediumClass exception, then restart. Do not save.",
                        "通灵者职业初始化失败。请查看首条 MediumClass 异常，然后重启游戏。请勿保存。")
                    : LocalizationText.Get("Maintenance build: game and save compatibility are NOT YET TESTED. Restart after changing mods.",
                        "简体中文维护测试版：游戏与存档兼容性尚未验证。更改 Mod 后请重启游戏。"));
                InstallEarlyPatches(harmony);

                handler = new BlueprintCacheInitHandler(harmony);
                EventBus.Subscribe(handler);

                Logger.Log("Finished patching.");
                return true;
            }
            catch (Exception e)
            {
                InitializationFailed = true;
                Logger.LogException("Failed to patch", e);
                // Never remove another mod's Harmony patches.
                try
                {
                    if (handler != null) EventBus.Unsubscribe(handler);
                    harmony?.UnpatchAll(modEntry.Info.Id);
                }
                catch (Exception cleanupError)
                {
                    Logger.LogException("Failed to clean up this mod's partial installation", cleanupError);
                }
                return false;
            }
        }

        internal static void InstallEarlyPatches(Harmony harmony)
        {
            // Mono may run a target's static constructor while Harmony obtains its
            // method address. UIUtilityTexts reads BlueprintRoot, which is absent here.
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t =>
                t.GetCustomAttributes(typeof(HarmonyPatch), false).Any() && !Medium.PsychicUiPatches.IsDeferred(t)
                && (!Integrated || t.DeclaringType != typeof(Medium.MediumPsychicCasting))))
                harmony.CreateClassProcessor(type).Patch();
        }

        class BlueprintCacheInitHandler : IBlueprintCacheInitHandler
        {
            private readonly Harmony harmony;
            public BlueprintCacheInitHandler(Harmony harmony) { this.harmony = harmony; }
            private enum InitState { NotStarted, Running, Succeeded, Failed }
            private static InitState initialState;
            private static InitState delayedState;

            private static void RunStage(string name, Action action)
            {
                var timer = Stopwatch.StartNew();
                Logger.Log($"BEGIN {name}");
                try
                {
                    action();
                    Logger.Log($"END {name} ({timer.ElapsedMilliseconds} ms)");
                }
                catch (Exception e)
                {
                    Logger.LogException($"FAILED {name} ({timer.ElapsedMilliseconds} ms)", e);
                    throw;
                }
            }

            public void AfterBlueprintCachePatches()
            {
                try
                {
                    if (initialState != InitState.Succeeded || delayedState != InitState.NotStarted)
                    {
                        Logger.Log($"Skipping delayed initialization: initial={initialState}, delayed={delayedState}.");
                        return;
                    }
                    delayedState = InitState.Running;

                    RunStage("delayed feats", ConfigureFeatsDelayed);

                    RunStage("delayed blueprints", () => RootConfigurator.ConfigureDelayedBlueprints());
                    RunStage("optional TTT Medium capstone", Medium.HardenedSoul.ConfigureOptionalIntegration);
                    delayedState = InitState.Succeeded;
                }
                catch (Exception e)
                {
                    delayedState = InitState.Failed;
                    InitializationFailed = true;
                    Logger.LogException("Delayed blueprint configuration failed.", e);
                }
            }

            public void BeforeBlueprintCachePatches()
            {

            }

            public void BeforeBlueprintCacheInit()
            {

            }

            public void AfterBlueprintCacheInit()
            {
                try
                {
                    if (initialState != InitState.NotStarted)
                    {
                        Logger.Log($"Skipping blueprint initialization: {initialState}. Failed initialization requires restart.");
                        return;
                    }
                    initialState = InitState.Running;
                    if (!Integrated) RunStage("psychic UI patches", () => Medium.PsychicUiPatches.Install(harmony));
                    LogWrapper.EnableInternalVerboseLogs();
                    // First strings
                    RunStage("localization", () => LocalizationTool.LoadEmbeddedLocalizationPacks(
                      "MediumClass.Strings.Archmage.json",
                      "MediumClass.Strings.Champion.json",
                      "MediumClass.Strings.Guardian.json",
                      "MediumClass.Strings.Hierophant.json",
                      "MediumClass.Strings.Marshal.json",
                      "MediumClass.Strings.Medium.json",
                      "MediumClass.Strings.Settings.json",
                      "MediumClass.Strings.Feats.json",
                      "MediumClass.Strings.Trickster.json",
                      "MediumClass.Strings.Prowler.json"));

                    // Then settings

                    RunStage("settings", Settings.Init);
                    RunStage("classes", ConfigureClasses);
                    RunStage("homebrew", ConfigureHomebrew);
                    RunStage("feats", ConfigureFeats);
                    RunStage("spells", ConfigureSpells);
                    initialState = InitState.Succeeded;
                }
                catch (Exception e)
                {
                    initialState = InitState.Failed;
                    InitializationFailed = true;
                    Logger.LogException("Failed to initialize.", e);
                }
            }
            private static void ConfigureHomebrew()
            {
                Logger.Log("Configuring homebrew.");
            }
            private static void ConfigureClasses()
            {
                Logger.Log("Configuring Classes.");
                Medium.MediumClass.ConfigureEnabled();
                // 变体依赖已配置好的通灵者本体（职业与升级表）。
                SpiritBinder.Configure();
                Prowler.ProwlerBuild.ConfigureBlueprints();
            }
            private static void ConfigureClassFeats()
            {
                Logger.Log("Configuring class features.");
            }
            private static void ConfigureFeats()
            {   
                Logger.Log("Configuring features.");
                BackgroundMedium.ConfigureEnabled();
                Medium.MythicSpirits.Configure();
                Medium.MythicInfluence.Configure();
                Medium.MythicSpiritInfluence.Configure();
                Medium.HardenedSoul.Configure();
                Features.MediumSpecific.SpiritFocus.ConfigureEnabled();
            }
            private static void ConfigureSpells()
            {
                Logger.Log("Configuring spells.");
            }
            private static void ConfigureFeatsDelayed()
            {
                Logger.Log("Configuring delayed.");
            }
        }
    }
}


