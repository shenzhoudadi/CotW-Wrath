using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;

namespace CallOfTheWild.Wrath.Prototype
{
    // Repair registrations only. Ability implementations and prerequisites are unchanged.
    internal static class CombatFeatPoolRepair
    {
        // Audited against every CombatFeat selection in the installed game's blueprints.zip.
        // Advanced Weapon Training also uses CombatFeat, but is a restricted submenu.
        internal static readonly string[] NativeIds = {
            BlueprintRegistration.BasicFeatsId,
            "41c8486641f7d6d4283ca9dae4147a9f", "66befe7b24c42dd458952e3c47c93563",
            "da03141df23f3fe45b0c7c323a8e5a0e", "79c6421dbdb028c4fa0c31b8eea95f16",
            "c5158a6622d0b694a99efb1d0025d2c1", "dd17090d14958ef48ba601688b611970",
            "ef1cd58e0b7fc7f45baedb09407a1cd1", "90f105c8e31a6224ea319e6a810e4af8",
            "0ce1432869c74b778ab48890edf2021d", "8e627812dc034b9db12fa396fdc9ec75",
            "db3c26e791cb4b70a82203b28563da02", "f1add10c87fa4563ad5f71779eecde19",
            "78fffe8e5d5bc574a9fd5efbbb364a03", "303fd456ddb14437946e344bad9a893b"
        };
        // Optional stable CotW pool identities; absent providers remain optional.
        // Wrath ports and other clones are also recognized by combat group + full-list anchors.
        internal static readonly string[] OptionalIds = {
            BlueprintRegistration.BrawlerCombatFeatSelectionId,
            "043c6c84676d4c2e8e09b251a012b850", "41de9e66013a4391ab412545ea907260",
            "3cdb38b60b5d4cd59ef42a3ccda5137b", "69da0205e7264d218862562c160198a5",
            "a78d8183d209483c957e9746ec7adefd", "5706b9e24b2245649199c39b36207b6c",
            "9e2aa2c873ba4287a3a9c0c39384bbca", "ae0e022e43a74f38b9ca656e5550324c"
        };
        private static readonly HashSet<BlueprintGuid> Known = new HashSet<BlueprintGuid>(NativeIds.Concat(OptionalIds).Select(BlueprintGuid.Parse));
        private static readonly BlueprintGuid[] Anchors = {
            BlueprintGuid.Parse("4c44724ffa8844f4d9bedb5bb27d144a"), // Combat Expertise
            BlueprintGuid.Parse("97e216dbb46ae3c4faef90cf6bbe6fd5"), // Dodge
            BlueprintGuid.Parse("9972f33f977fc724c838e59641b2fca5")  // Power Attack
        };

        private static bool IsGeneralPool(BlueprintFeatureSelection selection)
        {
            if (selection == null) return false;
            if (Known.Contains(selection.AssetGuid)) return true;
            if (selection.Group != FeatureGroup.CombatFeat && selection.Group2 != FeatureGroup.CombatFeat) return false;
            var refs = (BlueprintFeatureReference[])PsychicAssets.Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            return Anchors.All(id => refs.Any(x => x?.Guid == id));
        }

        internal static void Repair(BlueprintsCache cache, BlueprintFeatureSelection selection)
        {
            if (cache == null || selection == null) return;
            TeamworkFeats.RepairPool(cache, selection);
            CooperationFeats.RepairPool(cache, selection);
            LinnormFeats.RepairStylePool(cache, selection);
            SpellAndSwordFeats.RepairStylePool(cache, selection);
            if (!IsGeneralPool(selection)) return;
            var chosen = ChosenWeaponFeats.Tokens.Select(t => cache.Load(PsychicAssets.Id(t)) as BlueprintFeature).ToArray();
            var feats = chosen.Where((f,i)=>f?.GetComponent<PrerequisiteChosenWeapon>()!=null&&f.name=="CotWWrathPsychic"+ChosenWeaponFeats.Tokens[i])
                .Concat(WayOfTheShieldIntegration.CombatIds.Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeature).Where(f=>f?.HasGroup(FeatureGroup.CombatFeat)==true&&f.HasGroup(FeatureGroup.Feat)))
                .Concat(TeamworkFeats.Tokens.Take(3).Select(t=>cache.Load(PsychicAssets.Id(t)) as BlueprintFeature).Where(f=>f?.HasGroup(FeatureGroup.CombatFeat)==true))
                .Concat(CooperationFeats.Tokens.Skip(2).Select(t=>cache.Load(PsychicAssets.Id(t)) as BlueprintFeature).Where(f=>f?.HasGroup(FeatureGroup.CombatFeat)==true))
                .Concat(LinnormFeats.Tokens.Select(t=>cache.Load(PsychicAssets.Id(t)) as BlueprintFeature).Where(f=>f?.HasGroup(FeatureGroup.CombatFeat)==true))
                .Concat(SpellAndSwordFeats.Tokens.Skip(2).Take(2).Select(t=>cache.Load(PsychicAssets.Id(t)) as BlueprintFeature).Where(f=>f?.HasGroup(FeatureGroup.CombatFeat)==true))
                .Concat(ProviderResourceFeats.Tokens.Select(t=>cache.Load(PsychicAssets.Id(t)) as BlueprintFeature).Where(f=>f?.HasGroup(FeatureGroup.CombatFeat)==true))
                .Concat(new[]{cache.Load(ImprovedTwoWeaponFeint.FeatId) as BlueprintFeature}.Where(f=>f?.GetComponent<ImprovedFeintWindow>()!=null)).Distinct().ToArray();
            if(feats.Length==0)return;
            foreach (var feat in feats)
                if (feat.Groups == null || !feat.HasGroup(FeatureGroup.Feat) || !feat.HasGroup(FeatureGroup.CombatFeat))
                    feat.Groups = (feat.Groups ?? Array.Empty<FeatureGroup>()).Concat(new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat }).Distinct().ToArray();
            if (new[] { "m_Features", "m_AllFeatures" }.Any(field => {
                var refs = (BlueprintFeatureReference[])PsychicAssets.Get(selection, field) ?? Array.Empty<BlueprintFeatureReference>();
                return feats.Any(feat => !refs.Any(x => x?.Guid == feat.AssetGuid));
            })) T09CombatFeats.Append(new[] { selection }, feats);
        }

        internal static void Reconcile(BlueprintsCache cache)
        {
            foreach (var id in NativeIds.Concat(OptionalIds))
                Repair(cache, cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection);
            // Snapshot first: resolving feat references can add entries to the cache.
            var loaded = new List<BlueprintFeatureSelection>();
            cache.ForEachLoaded((id, bp) => { if (bp is BlueprintFeatureSelection selection) loaded.Add(selection); });
            foreach (var selection in loaded) Repair(cache, selection);
        }
    }

    [HarmonyPatch]
    internal static class CombatFeatPoolOpeningPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.ExtractSelectionItems));
            yield return AccessTools.PropertyGetter(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.Items));
            yield return AccessTools.Method(typeof(BlueprintFeatureSelection), nameof(BlueprintFeatureSelection.CanSelect));
        }
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(BlueprintFeatureSelection __instance)
            => CombatFeatPoolRepair.Repair(ResourcesLibrary.BlueprintsCache, __instance);
    }
}
