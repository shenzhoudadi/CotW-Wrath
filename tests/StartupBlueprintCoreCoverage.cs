using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

internal static class StartupBlueprintCoreCoverage
{
    internal const int PatchCount = 5;
    private static readonly string[] Classes = {
        "BlueprintCore.Utils.LocalizationTool+LocalizationManager_Patch",
        "BlueprintCore.Utils.Assets.AssetTool+AssetBundle_Patch",
        "BlueprintCore.Utils.Assets.AssetTool+BundlesLoadService_Patch",
        "BlueprintCore.Utils.Assets.AssetTool+BlueprintsCaches_Patch",
        "BlueprintCore.UnitParts.Replacements.UnitPartBuffSuppressFixed+Buff_OnAttach_Suppression_Patch"
    };
    private static readonly MethodBase[] Targets = {
        AccessTools.Method(typeof(Kingmaker.Localization.LocalizationManager), "OnLocaleChanged"),
        AccessTools.Method(typeof(UnityEngine.AssetBundle), "LoadAsset", new[] { typeof(string), typeof(Type) }),
        AccessTools.Method(typeof(Kingmaker.BundlesLoading.BundlesLoadService), "GetBundleNameForAsset"),
        AccessTools.Method(typeof(Kingmaker.Blueprints.JsonSystem.BlueprintsCache), "Init"),
        AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.Buff), "OnAttach")
    };

    internal static void VerifyInstalled(Assembly medium, Harmony owner)
    {
        var classes = medium.GetTypes().Where(t => t.Namespace != null && t.Namespace.StartsWith("BlueprintCore") &&
            t.GetCustomAttributes(typeof(HarmonyPatch), false).Any()).ToArray();
        if (classes.Length != PatchCount) throw new Exception("Unexpected bundled BlueprintCore patch class count.");
        for (var i = 0; i < Classes.Length; i++)
        {
            var type = medium.GetType(Classes[i], true);
            var target = Targets[i] ?? throw new Exception("Missing actual BlueprintCore target: " + Classes[i]);
            var info = Harmony.GetPatchInfo(target);
            if (info == null) throw new Exception("BlueprintCore target was never patched: " + target);
            var hooks = info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers)
                .Where(p => p.owner == owner.Id && p.PatchMethod.DeclaringType == type).ToArray();
            var expected = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m =>
                m.GetCustomAttributes(typeof(HarmonyPrefix), false).Any() ||
                m.GetCustomAttributes(typeof(HarmonyPostfix), false).Any()).ToArray();
            if (expected.Length != 1 || hooks.Length != 1 || hooks[0].PatchMethod != expected[0])
                throw new Exception("Bundled BlueprintCore hook missing, duplicated or bound to the wrong target: " + type.FullName);
            Console.WriteLine("Installed BlueprintCore patch: " + type.FullName + " -> " + target);
        }
    }

    internal static void VerifyRemoved(Harmony owner)
    {
        foreach (var target in Targets)
            if (Harmony.GetPatchInfo(target)?.Owners.Contains(owner.Id) == true)
                throw new Exception("BlueprintCore owner remains after cleanup: " + target);
    }
}
