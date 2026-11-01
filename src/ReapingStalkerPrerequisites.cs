using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace CallOfTheWild.Wrath.Prototype
{
    // Qualification only: never add an Improved Critical fact or alter weapon rules.
    internal static class ReapingStalkerPrerequisites
    {
        internal static readonly BlueprintGuid ImprovedCriticalId = BlueprintGuid.Parse("f4201c85a991369408740c6888362e20");
        static readonly BlueprintGuid TalentId = PsychicAssets.Id(ReapingStalker.Token);
        internal static bool IsCritical(BlueprintFeature feature) => feature?.AssetGuid == ImprovedCriticalId;
        internal static bool IsWeapon(WeaponCategory? category) => category == WeaponCategory.Sickle || category == WeaponCategory.Scythe;
        internal static Feature Talent(UnitDescriptor unit) => unit?.Progression?.Features?.Enumerable?.FirstOrDefault(f => f.Blueprint.AssetGuid == TalentId);

        // Called only at the two native prerequisite lookups inside CanSelect.
        // Its earlier ownership check and every later prerequisite remain native.
        internal static Feature GetPrerequisite(UnitDescriptor unit, BlueprintFeature feature, FeatureParam parameter)
            => unit.GetFeature(feature, parameter) ?? (IsCritical(feature) && IsWeapon(parameter?.WeaponCategory) ? Talent(unit) : null);
    }

    [HarmonyPatch(typeof(BlueprintParametrizedFeature), nameof(BlueprintParametrizedFeature.CanSelect))]
    internal static class ReapingStalkerWeaponSelectionPatch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var lookup = AccessTools.Method(typeof(UnitHelper), nameof(UnitHelper.GetFeature), new[] { typeof(UnitDescriptor), typeof(BlueprintFeature), typeof(FeatureParam) });
            var prerequisite = AccessTools.PropertyGetter(typeof(BlueprintParametrizedFeature), nameof(BlueprintParametrizedFeature.Prerequisite));
            var replacement = AccessTools.Method(typeof(ReapingStalkerPrerequisites), nameof(ReapingStalkerPrerequisites.GetPrerequisite));
            bool pending = false; int replaced = 0;
            foreach (var instruction in code) {
                if (instruction.operand is MethodInfo method && method == prerequisite) pending = true;
                if (instruction.operand is MethodInfo called && called == lookup) {
                    if (pending) { instruction.operand = replacement; replaced++; }
                    pending = false;
                }
            }
            if (replaced != 2) throw new InvalidOperationException("Reaping Stalker: native parameter prerequisite lookup contract changed.");
            return code;
        }
    }

    [HarmonyPatch(typeof(PrerequisiteParametrizedFeature), "CheckInternal")]
    internal static class ReapingStalkerExplicitPrerequisitePatch
    {
        [HarmonyPostfix]
        static void Postfix(PrerequisiteParametrizedFeature __instance, FeatureSelectionState __0, UnitDescriptor __1, ref bool __result)
        {
            if (__result || __instance.ParameterType != FeatureParameterType.WeaponCategory ||
                !ReapingStalkerPrerequisites.IsCritical(__instance.Feature) || !ReapingStalkerPrerequisites.IsWeapon(__instance.WeaponCategory) ||
                (__0 != null && __0.IsSelectedInChildren(__instance.Feature))) return;
            __result = ReapingStalkerPrerequisites.Talent(__1) != null;
        }
    }

    [HarmonyPatch(typeof(PrerequisiteFeature), "CheckInternal")]
    internal static class ReapingStalkerParameterRootPrerequisitePatch
    {
        [HarmonyPostfix]
        static void Postfix(PrerequisiteFeature __instance, FeatureSelectionState __0, UnitDescriptor __1, ref bool __result)
        {
            // Mythic/Trickster roots also carry a non-parameterized IC prerequisite.
            // Accept that root only when its own weapon selector enforces the same
            // IC prerequisite. A standalone/unrelated generic prerequisite is untouched.
            if (__result || !ReapingStalkerPrerequisites.IsCritical(__instance.Feature) ||
                !(__instance.OwnerBlueprint is BlueprintParametrizedFeature owner) ||
                owner.ParameterType != FeatureParameterType.WeaponCategory || !ReapingStalkerPrerequisites.IsCritical(owner.Prerequisite) ||
                (__0 != null && __0.IsSelectedInChildren(__instance.Feature))) return;
            __result = ReapingStalkerPrerequisites.Talent(__1) != null;
        }
    }
}
