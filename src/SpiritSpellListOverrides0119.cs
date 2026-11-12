using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    /// <summary>
    /// COTW-0119. Brings the four native shaman spirit spell lists in line with
    /// the original CotW spirit-guide table before any spirit consumer reads
    /// them. Owlcat's native lists use different spells at these four circles;
    /// the original table uses Wall of Fire (Battle/Flame 4), Incendiary Cloud
    /// (Flame 8) and Frostbite (Waves 1). Those three roots are supplied by
    /// ExpandedContent / CharacterOptionsPlus and are validated before any
    /// shared blueprint is touched.
    ///
    /// The edits target the shared native spell lists (the same objects the
    /// native Spirit progressions reference from AddSpellsToDescription), so
    /// every consumer - the real Shaman, Spirit Whisperer and Spirit Guide -
    /// sees one consistent table. Snapshots are restored on any failure, and
    /// the private marker blueprint makes registration idempotent.
    /// </summary>
    internal static class SpiritSpellListOverrides0119
    {
        internal const string ModuleName = "Spirit Spell List Overrides";
        internal const string RootToken = "SpiritSpellListOverrides0119";

        const string BattleSpellListId = "e788846aaf449404db11e51182174be8";
        const string FlameSpellListId = "659fbc54fc519b44dacacc78e7d46dec";
        const string WavesSpellListId = "190ebde2d9d80c94783bcb73b9106d99";

        const string WallOfFireId = "80189142f7c640f39195defdc9777b27";
        const string IncendiaryCloudId = "99bad8fca7534acdb86866940eef62da";
        const string FrostbiteId = "67b29b99f13e40a48a1a95fbd909a298";

        internal sealed class Override
        {
            internal readonly string ListId, SpellId;
            internal readonly int Circle;
            internal Override(string listId, int circle, string spellId) { ListId = listId; Circle = circle; SpellId = spellId; }
        }

        // Hypnotic Pattern (Heavens 2) is deliberately absent: Heavens is not a
        // native Wrath spirit, so there is no native list to overwrite. The
        // spirit consumers pick it up from the provider's own list if present.
        internal static readonly Override[] Overrides =
        {
            new Override(BattleSpellListId, 4, WallOfFireId),
            new Override(FlameSpellListId, 4, WallOfFireId),
            new Override(FlameSpellListId, 8, IncendiaryCloudId),
            new Override(WavesSpellListId, 1, FrostbiteId)
        };

        internal static bool Register(BlueprintsCache cache)
        {
            var marker = cache.Load(Id(RootToken));
            if (marker != null)
            {
                if (marker.name != "CotWWrathPsychic" + RootToken)
                    throw new InvalidOperationException("0119 spell-list override marker GUID is occupied by an unexpected blueprint.");
                Verify(cache);
                return true;
            }

            // Validate every root before touching a shared blueprint. Missing
            // providers make the whole module decline; RegisterAvailableModule
            // retries when those mods register later in the same load.
            foreach (var id in Overrides.Select(o => o.SpellId).Distinct())
                if (!(cache.Load(BlueprintGuid.Parse(id)) is BlueprintAbility ability) || ability.Type != AbilityType.Spell)
                    return false;
            foreach (var id in Overrides.Select(o => o.ListId).Distinct())
                if (!(cache.Load(BlueprintGuid.Parse(id)) is BlueprintSpellList))
                    return false;

            var lists = Overrides.Select(o => o.ListId).Distinct()
                .ToDictionary(id => id, id => (BlueprintSpellList)cache.Load(BlueprintGuid.Parse(id)));
            foreach (var list in lists.Values)
                if (list.SpellsByLevel == null || list.SpellsByLevel.Length <= 9)
                    return false;

            var f = new PsychicAssets(cache);
            var tokenAsset = f.Add(new BlueprintFeature { Ranks = 1, HideInUI = true, HideInCharacterSheetAndLevelUp = true },
                RootToken, "", "");

            // Snapshot the exact list objects. The field is a List<> on the
            // SpellLevelList; replacing it with a new list keeps the original
            // object intact for rollback.
            var snapshots = new Dictionary<SpellLevelList, object[]>();
            SpellLevelList Ring(Override o)
            {
                var entry = lists[o.ListId].SpellsByLevel.FirstOrDefault(e => e != null && e.SpellLevel == o.Circle);
                if (entry == null) throw new InvalidOperationException("0119 missing circle " + o.Circle + " on " + o.ListId);
                if (!snapshots.ContainsKey(entry)) snapshots.Add(entry, new[]
                {
                    Get(entry, "m_Spells"), Get(entry, "m_SpellsProxy"), Get(entry, "m_SpellsFiltered")
                });
                return entry;
            }

            try
            {
                foreach (var group in Overrides.GroupBy(o => o.ListId + "/" + o.Circle))
                {
                    var first = group.First();
                    var entry = Ring(first);
                    var replacement = new List<BlueprintAbilityReference>();
                    foreach (var o in group)
                        replacement.Add(Ref<BlueprintAbilityReference>((BlueprintAbility)cache.Load(BlueprintGuid.Parse(o.SpellId))));
                    // SetValue rejects an incompatible container, which rolls the
                    // whole transaction back instead of leaving a mixed table.
                    Set(entry, "m_Spells", replacement);
                    // SpellLevelList caches its resolved and filtered views. Clear
                    // both after replacing the authored references, exactly like
                    // the project's other shared spell-list integrations.
                    Set(entry, "m_SpellsProxy", null);
                    Set(entry, "m_SpellsFiltered", null);
                }
                Verify(cache);
                f.Enable();
                cache.AddCachedBlueprint(tokenAsset.AssetGuid, tokenAsset);
                return true;
            }
            catch
            {
                foreach (var pair in snapshots)
                {
                    Set(pair.Key, "m_Spells", pair.Value[0]);
                    Set(pair.Key, "m_SpellsProxy", pair.Value[1]);
                    Set(pair.Key, "m_SpellsFiltered", pair.Value[2]);
                }
                throw;
            }
        }

        static void Verify(BlueprintsCache cache)
        {
            string Norm(string guid) => guid.Replace("-", "").ToLowerInvariant();
            foreach (var group in Overrides.GroupBy(o => o.ListId + "/" + o.Circle))
            {
                var list = (BlueprintSpellList)cache.Load(BlueprintGuid.Parse(group.First().ListId));
                var entry = list.SpellsByLevel.FirstOrDefault(e => e != null && e.SpellLevel == group.First().Circle);
                if (entry == null) throw new InvalidOperationException("0119 circle vanished after registration.");
                var references = (Get(entry, "m_Spells") as List<BlueprintAbilityReference>)
                    ?? new List<BlueprintAbilityReference>();
                var authored = references.Where(r => r != null)
                    .Select(r => Norm(r.Guid.ToString())).ToArray();
                var resolved = (entry.Spells ?? Array.Empty<BlueprintAbility>()).Where(a => a != null)
                    .Select(a => Norm(a.AssetGuid.ToString())).ToArray();
                var expected = group.Select(o => Norm(o.SpellId)).ToArray();
                // Another mod may legally append a spell to the same circle after
                // our edit, and resolved views may be rebuilt in a different order.
                // Require every replacement to be present in both views rather
                // than demanding exact array equality.
                if (expected.Any(g => !authored.Contains(g) || !resolved.Contains(g)))
                    throw new InvalidOperationException("0119 spell-list override was not applied at " + group.Key +
                        "; authored=[" + string.Join(",", authored) + "] resolved=[" + string.Join(",", resolved) +
                        "] expected=[" + string.Join(",", expected) + "]");
            }
        }
    }
}
