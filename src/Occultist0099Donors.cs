using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.DLC;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Donors
    {
        internal const string AlchemistBook = "027d37761f3804042afa96fe3e9086cc";
        internal const string AlchemistDay = "bf4a0a03a45275d438b8c59cd5388259";
        internal const string FireShield = "017970c386634351bfdea34d6a4d4c74";
        private const string FireShieldWarm = "54ab6ea1a66d45e3adb5a9afaa723b43";
        private const string FireShieldCold = "b3578d622d7c42539f783c48b4d723a4";
        private static readonly HashSet<BlueprintGuid> CloakLists = new HashSet<BlueprintGuid>(new[]
        {
            "98c05aeff6e3d384f8aec6d584973642", "bad8638d40639d04fa2f80a1cac67d6b", // Bloodrager, Druid
            "4d72e1e7bd6bc4f4caaea7aa43a14639", "29f3c338532390546bc5347826a655c4",  // Magus, Ranger
            "ba0401fdeb4062f40a7aa95b6f07fe89"                                         // Wizard
        }.Select(BlueprintGuid.Parse));

        internal static BlueprintSpellsTable DailyTable(BlueprintsCache cache)
        {
            var book = cache.Load(BlueprintGuid.Parse(AlchemistBook)) as BlueprintSpellbook;
            var day = cache.Load(BlueprintGuid.Parse(AlchemistDay)) as BlueprintSpellsTable;
            var reference = book == null ? null : Get(book, "m_SpellsPerDay") as BlueprintSpellsTableReference;
            if (day?.AssetGuid != BlueprintGuid.Parse(AlchemistDay) || book?.name != "AlchemistSpellbook" ||
                book.AssetGuid != BlueprintGuid.Parse(AlchemistBook) || reference?.Guid != day.AssetGuid) return null;
            for (int circle = 1; circle <= 6; circle++)
            {
                int first = Occultist0099AbjurationCatalog.ClassLevelForCircle(circle);
                if (day.GetCount(first, circle) <= 0 || day.GetCount(first - 1, circle) > 0) return null;
            }
            return day;
        }

        internal static Dictionary<Occultist0099SpellRoot, BlueprintAbility> Resolve(BlueprintsCache cache,
            IEnumerable<Occultist0099SpellRoot> rows)
        {
            var resolved = new Dictionary<Occultist0099SpellRoot, BlueprintAbility>();
            foreach (var row in rows)
            {
                BlueprintAbility root;
                if (row.Source == "External")
                {
                    if (TryExternal(cache, row, out root)) resolved.Add(row, root);
                    continue;
                }
                var guid = BlueprintGuid.Parse(row.Guid);
                root = cache.Load(guid) as BlueprintAbility;
                if (root == null || root.AssetGuid != guid || root.name != row.Name || root.Type != AbilityType.Spell ||
                    PsychicSpellIntegration.ParentGuid(root) != BlueprintGuid.Empty ||
                    root.GetComponent<SpellComponent>()?.School.ToString() != row.School ||
                    (row.Source == "CotW" && !PsychicSpellIntegration.HasExecutableContents(root))) return null;
                if (!root.IsDlcRestricted()) resolved.Add(row, root);
            }
            return resolved;
        }

        internal static bool TryExternal(BlueprintsCache cache, Occultist0099SpellRoot row, out BlueprintAbility root)
        {
            var link = PsychicSharedSpellCatalog.Entries.FirstOrDefault(entry => entry.Guid == row.Guid);
            if (link == null)
                link = new PsychicSpellLink(row.Guid, row.Name, row.Provider,
                    (SpellSchool)Enum.Parse(typeof(SpellSchool), row.School), row.RequiresPublishedList, -1, row.Circle, -1);
            root = null;
            if (link.Name != row.Name || link.Provider != row.Provider || link.School.ToString() != row.School ||
                link.PublishedListRequired != row.RequiresPublishedList) return false;
            if (!PsychicSpellIntegration.Available(cache, link, out root, (id, level) =>
                    row.Guid != Occultist0099AbjurationCatalog.CloakOfWinds || (level == 3 && CloakLists.Contains(id))) ||
                root.IsDlcRestricted()) return false;
            if (row.Guid == Occultist0099AbjurationCatalog.CloakOfWinds)
                return PsychicSpellIntegration.HasRunActions(root);
            return row.Guid != FireShield || HasFireShieldVariants(cache, root);
        }

        private static bool HasFireShieldVariants(BlueprintsCache cache, BlueprintAbility root)
        {
            var component = root.GetComponent<AbilityVariants>();
            if (component == null) return false;
            var variants = Get(component, "m_Variants") as BlueprintAbilityReference[];
            if (variants == null || variants.Length != 2) return false;
            return HasVariant(FireShieldWarm, "FireShieldWarm") && HasVariant(FireShieldCold, "FireShieldCold");

            bool HasVariant(string id, string name)
            {
                var guid = BlueprintGuid.Parse(id);
                var child = cache.Load(guid) as BlueprintAbility;
                return variants.Any(reference => reference?.Guid == guid) && child?.name == name && child.AssetGuid == guid &&
                    PsychicSpellIntegration.ParentGuid(child) == root.AssetGuid && child.Type == AbilityType.Spell &&
                    child.GetComponent<SpellComponent>()?.School == SpellSchool.Evocation &&
                    !child.IsDlcRestricted() && PsychicSpellIntegration.HasRunActions(child);
            }
        }
    }
}
