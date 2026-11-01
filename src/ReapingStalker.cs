using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Original CotW talent, implemented entirely with native rule components.
    internal static class ReapingStalker
    {
        internal const string ModuleName = "Reaping Stalker";
        internal const string Token = "ReapingStalkerSlayerTalent";
        internal const string AdvancedId = "a33b99f95322d6741af83e9381b2391c";
        internal const string SickleId = "ec2da496c7936e14c9a28ce616a6b4cd";
        internal const string ScytheId = "4eacfc7e152930a45a1a16217c35011c";
        internal const string TttExtraId = "ac527291106543039bf277236b478389";
        internal static readonly string[] PoolIds = {
            "04430ad24988baa4daa0bcd4f1c7d118", "43d1b15873e926848be2abf0ea3ad9a8",
            "913b9cf25c9536949b43a2651b7ffb66", "53f7237f5b1447bb851ba68045a00e41"
        };

        static WeaponTypeCriticalEdgeIncrease Critical(BlueprintWeaponType weapon)
        {
            var component = new WeaponTypeCriticalEdgeIncrease();
            Set(component, "m_WeaponType", Ref<BlueprintWeaponTypeReference>(weapon));
            return component;
        }

        static void Append(BlueprintFeatureSelection[] pools, BlueprintFeature talent)
        {
            var before = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try { T09CombatFeats.Append(pools, new[] { talent }); }
            catch {
                for (int i = 0; i < pools.Length; i++) {
                    Set(pools[i], "m_Features", before[i][0]); Set(pools[i], "m_AllFeatures", before[i][1]);
                }
                throw;
            }
        }

        internal static void Register(BlueprintsCache cache)
        {
            var assets = new PsychicAssets(cache);
            var pools = PoolIds.Select(assets.Native<BlueprintFeatureSelection>).ToArray();
            var advanced = assets.Native<BlueprintFeature>(AdvancedId);
            var sickle = assets.Native<BlueprintWeaponType>(SickleId);
            var scythe = assets.Native<BlueprintWeaponType>(ScytheId);
            if (sickle.Category != WeaponCategory.Sickle || scythe.Category != WeaponCategory.Scythe ||
                sickle.CriticalRollEdge != 20 || scythe.CriticalRollEdge != 20)
                throw new InvalidOperationException("Reaping Stalker requires the native sickle/scythe weapon type contract.");
            var prerequisite = new PrerequisiteFeature();
            Set(prerequisite, "m_Feature", Ref<BlueprintFeatureReference>(advanced));
            var talent = assets.Feature(Token, ModuleName,
                "Advanced slayer talent. Treat sickles and scythes you wield as one size larger for damage dice. Their critical threat range becomes 19-20; this uses the same native threat-range increase as Improved Critical and keen, and does not stack with them. Counts as Improved Critical (sickle) and Improved Critical (scythe) for weapon-specific feat prerequisites only; other prerequisites still apply. Requires Advanced Talents. This talent can be selected once.",
                prerequisite,
                new WeaponSizeChange { CheckWeaponCategory = true, Category = WeaponCategory.Sickle, SizeCategoryChange = 1 },
                new WeaponSizeChange { CheckWeaponCategory = true, Category = WeaponCategory.Scythe, SizeCategoryChange = 1 },
                Critical(sickle), Critical(scythe));
            talent.Groups = new[] { FeatureGroup.SlayerTalent };
            talent.IsClassFeature = true;
            Set(talent, "m_Icon", Get(scythe, "m_Icon"));
            PsychicRepairComponents.UniqueNames(talent);
            var previous = cache.Load(talent.AssetGuid);
            if (previous != null) {
                var owned = previous as BlueprintFeature;
                if (previous.GetType() != typeof(BlueprintFeature) || owned.name != talent.name || owned.Ranks != 1 ||
                    owned.ComponentsArray == null || owned.ComponentsArray.Any(c => c == null) ||
                    !owned.ComponentsArray.Select(PsychicExpansion.Signature).SequenceEqual(talent.ComponentsArray.Select(PsychicExpansion.Signature)))
                    throw new InvalidOperationException("Reaping Stalker identity collision; existing blueprint preserved.");
                talent = owned;
            }
            try {
                if (previous == null) { assets.Enable(); cache.AddCachedBlueprint(talent.AssetGuid, talent); }
                Append(pools, talent);
            }
            catch {
                if (previous == null && ReferenceEquals(cache.Load(talent.AssetGuid), talent)) cache.RemoveCachedBlueprint(talent.AssetGuid);
                throw;
            }
        }

        // TTT replaces the native Extra Slayer Talent with its own selection.
        // Its absence never blocks the native talent; recheck late providers via
        // the existing optional-registration/settings route, without scanning caches.
        internal static void Optional(BlueprintsCache cache)
        {
            var talent = cache.Load(Id(Token)) as BlueprintFeature;
            var provider = cache.Load(BlueprintGuid.Parse(TttExtraId));
            if (talent == null || provider == null) return;
            if (!(provider is BlueprintFeatureSelection selection) || selection.name != "ExtraSlayerTalent" ||
                selection.IgnorePrerequisites || !selection.HasGroup(FeatureGroup.Feat))
                throw new InvalidOperationException("Unexpected TabletopTweaks Extra Slayer Talent selection; preserved.");
            Append(new[] { selection }, talent);
        }
    }
}
