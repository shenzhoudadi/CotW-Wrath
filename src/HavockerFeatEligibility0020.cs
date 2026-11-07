// K01 Q-11: optional providers keep their blueprints and original prerequisites.
// Only the two actually audited TTT feats need a learned-hex source guard.
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class HavockerFeatEligibility0020
    {
        const string Split = "0b938825baf14a70818562a3cfe50493", Major = "0943638214534753a225f524d0377b31";
        static bool AuditedProvider(BlueprintFeature bp) {
            if (bp == null) return false;
            string id = bp.AssetGuid.ToString();
            if ((id != Split && id != Major) || !ReferenceEquals(ResourcesLibrary.BlueprintsCache.Load(bp.AssetGuid), bp)) return false;
            if (id == Split && bp.name == "SplitHex")
                return bp.ComponentsArray.Any(c => c.GetType().FullName == "TabletopTweaks.Core.NewComponents.AbilitySpecific.SplitHexComponent" && c.GetType().Assembly.GetName().Name == "TabletopTweaks-Core");
            if (id == Major && bp.name == "SplitHexMajor")
                return bp.GetComponents<PrerequisiteFeature>().Any(p => p.Feature?.AssetGuid.ToString() == Split)
                    && bp.ComponentsArray.Any(c => c.GetType().FullName == "TabletopTweaks.Core.NewComponents.Prerequisites.PrerequisiteCasterLevel" && c.GetType().Assembly.GetName().Name == "TabletopTweaks-Core");
            return false;
        }
        internal static bool Permit(Prerequisite prerequisite, UnitDescriptor unit) {
            if (!Havocker0020.IsHavocker(unit) || !AuditedProvider(prerequisite.OwnerBlueprint as BlueprintFeature)) return true;
            // Reuse the existing learned native ability predicate, and accept
            // real learned feature sources from the two actual hex selections
            // (including provider-added options and passive hexes). Do not infer
            // a hex merely from class levels or the selection marker itself.
            if (unit.Unit.Abilities.Enumerable.Any(a => a.IsActive && a.IsTurnedOn && SpellResourceFeats.Hex(a.Blueprint))) return true;
            foreach (string id in new[] { "9846043cf51251a4897728ed6e24e76f", "4223fe18c75d4d14787af196a04e14e7" }) {
                var pool = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(id);
                if (pool != null && unit.Unit.Facts.List.Any(f => f is Feature && f.IsActive && f.IsTurnedOn && pool.AllFeatures.Contains(f.Blueprint))) return true;
            }
            return false;
        }
        internal static void Install(Harmony harmony) => harmony.CreateClassProcessor(typeof(QueryPatch)).Patch();
        [HarmonyPatch]
        internal static class QueryPatch {
            [HarmonyTargetMethods] static IEnumerable<MethodBase> TargetMethods() {
                yield return AccessTools.Method(typeof(Prerequisite), nameof(Prerequisite.Check));
                yield return AccessTools.Method(typeof(Prerequisite), nameof(Prerequisite.CheckRaw));
            }
            [HarmonyPostfix] static void Postfix(Prerequisite __instance, UnitDescriptor unit, ref bool __result) {
                if (__result && !Permit(__instance, unit)) __result = false;
            }
        }
    }
}
