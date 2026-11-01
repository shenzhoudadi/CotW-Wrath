using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicUiPatches
    {
        private static Type[] Types() => new[] {
            typeof(PsychicCasting).GetNestedType("LogicalNamePatch", BindingFlags.NonPublic),
            typeof(PsychicCasting).GetNestedType("LogicalListPatch", BindingFlags.NonPublic)
        };
        internal static bool IsDeferred(Type type) => Types().Contains(type);

        internal static void Install(Harmony harmony)
        {
            // Check the root without touching UIUtilityTexts or creating Game.Instance.
            // An early failed UIUtilityTexts initializer is permanent until process exit.
            if (ResourcesLibrary.GetRoot()?.UIRoot?.TooltipColors == null)
                throw new InvalidOperationException("Psychic UI patches require initialized BlueprintRoot/UIRoot/TooltipColors.");
            foreach (var type in Types())
            {
                var target = AccessTools.Method(typeof(Kingmaker.UI.Common.UIUtilityTexts),
                    type.Name == "LogicalNamePatch" ? "GetMetamagicName" : "GetMetamagicList");
                if (Harmony.GetPatchInfo(target)?.Owners.Contains(harmony.Id) != true)
                    harmony.CreateClassProcessor(type).Patch();
            }
        }
    }
}
