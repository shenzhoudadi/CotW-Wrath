using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    /// <summary>
    /// COTW-0119. The original CotW Battle mystery grants its second bonus spell
    /// at 4th oracle level (a 2nd-circle spell). Replace exactly that one grant
    /// with the migrated Fog Cloud, leaving every other mystery spell alone.
    ///
    /// "4th level" here is the level the spell is gained, not the spell's circle.
    ///
    /// Discovery happens against the live mystery selection, not a frozen GUID
    /// table: Owlcat publishes each mystery as a BlueprintFeature carrying the
    /// whole bonus-spell table on one feature behind an AddFeatureOnClassLevel
    /// gate (Battle: gate level 2 -> the nine AddKnownSpell entries, circle 1..9,
    /// so the 4th-level grant is the single SpellLevel == 2 entry). Third-party
    /// providers use a BlueprintProgression with one grant per level entry, which
    /// the same walk covers. The shared native component is snapshotted and
    /// restored on any failure, and a private marker keeps registration
    /// idempotent.
    /// </summary>
    internal static class BattleMysteryBonusSpell0119
    {
        internal const string ModuleName = "Battle Mystery Bonus Spell";
        internal const string RootToken = "BattleMysteryBonusSpell0119";

        // OracleMysterySelection - Owlcat's own selection, the one the oracle reads.
        const string MysterySelectionId = "5531b975dcdf0e24c98f1ff7e017e741";

        // 4th oracle level is when the 2nd circle arrives.
        const int GrantCircle = 2;
        const int GrantLevel = 4;

        internal static bool Register(BlueprintsCache cache)
        {
            var fog = cache.Load(Id("FogCloud")) as BlueprintAbility;
            if (fog == null) return false; // the migrated Fog Cloud is not published yet

            var existing = cache.Load(Id(RootToken));
            if (existing != null)
            {
                if (existing.name != "CotWWrathPsychic" + RootToken)
                    throw new InvalidOperationException("0119 Battle mystery marker GUID is occupied by an unexpected blueprint.");
                Verify(cache, fog);
                return true;
            }
            if (!(cache.Load(BlueprintGuid.Parse(MysterySelectionId)) is BlueprintFeatureSelection)) return false;

            var grant = FindGrant(cache);
            var before = Get(grant, "m_Spell");

            var f = new PsychicAssets(cache);
            var marker = f.Add(new BlueprintFeature { Ranks = 1, HideInUI = true, HideInCharacterSheetAndLevelUp = true },
                RootToken, "", "");
            try
            {
                Set(grant, "m_Spell", Ref<BlueprintAbilityReference>(fog));
                Verify(cache, fog);
                f.Enable();
                cache.AddCachedBlueprint(marker.AssetGuid, marker);
                return true;
            }
            catch
            {
                Set(grant, "m_Spell", before);
                if (ReferenceEquals(cache.Load(marker.AssetGuid), marker)) cache.RemoveCachedBlueprint(marker.AssetGuid);
                throw;
            }
        }

        static void Verify(BlueprintsCache cache, BlueprintAbility fog)
        {
            var grant = FindGrant(cache);
            var current = Get(grant, "m_Spell") as BlueprintAbilityReference;
            if (current == null || current.Guid != fog.AssetGuid)
                throw new InvalidOperationException("0119 Battle mystery 4th-level bonus spell is not Fog Cloud.");
        }

        static AddKnownSpell FindGrant(BlueprintsCache cache)
        {
            var battle = FindBattle(cache)
                ?? throw new InvalidOperationException("0119 Oracle Battle mystery was not found in the live mystery selection.");

            // Progression-shaped provider: a grant that hangs off the level-4
            // entry or a level-4 gate is unambiguous, so take it first.
            var atFour = Spells(GrantedAtLevel(battle, GrantLevel)).Where(k => k.SpellLevel == GrantCircle).ToArray();
            if (atFour.Length == 1) return atFour[0];

            // Owlcat's shape: the spells feature sits behind a gate of a different
            // level and carries all nine circles, so the circle is the selector.
            var byCircle = Spells(Reach(battle)).Where(k => k.SpellLevel == GrantCircle).ToArray();
            if (byCircle.Length == 1) return byCircle[0];
            if (byCircle.Length == 0)
                throw new InvalidOperationException("0119 Battle mystery grants no 2nd-circle bonus spell.");
            throw new InvalidOperationException("0119 Battle mystery 2nd-circle bonus spell grant is ambiguous (" +
                string.Join(", ", byCircle.Select(k => k.Spell?.name ?? "<null>")) + ").");
        }

        static BlueprintFeatureBase FindBattle(BlueprintsCache cache)
        {
            var selection = cache.Load(BlueprintGuid.Parse(MysterySelectionId)) as BlueprintFeatureSelection;
            var all = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            return all.Select(r => r?.Get() as BlueprintFeatureBase).FirstOrDefault(IsBattle);
        }

        static bool IsBattle(BlueprintFeatureBase fact)
        {
            if (fact == null) return false;
            string localized = null;
            try { localized = fact.Name; } catch { /* localization not resolved */ }
            return (localized ?? "").IndexOf("Battle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (fact.name ?? "").IndexOf("Battle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static IEnumerable<AddKnownSpell> Spells(IEnumerable<BlueprintFeatureBase> facts) =>
            facts.Where(f => f != null).SelectMany(f => f.GetComponents<AddKnownSpell>());

        // Facts a mystery grants at one level: progression level entries plus
        // AddFeatureOnClassLevel gates, which is how a BlueprintFeature publishes
        // levels too.
        static BlueprintFeatureBase[] GrantedAtLevel(BlueprintFeatureBase root, int level)
        {
            var result = new List<BlueprintFeatureBase>();
            if (root is BlueprintProgression progression && progression.LevelEntries != null)
                foreach (var entry in progression.LevelEntries)
                    if (entry != null && entry.Level == level && entry.Features != null)
                        result.AddRange(entry.Features.Where(f => f != null));
            foreach (var gate in root.GetComponents<AddFeatureOnClassLevel>())
                if (gate.Level == level)
                {
                    var target = (Get(gate, "m_Feature") as BlueprintFeatureReference)?.Get() as BlueprintFeatureBase;
                    if (target != null) result.Add(target);
                }
            return result.Distinct().ToArray();
        }

        // Walk everything the mystery can hand out, so a spells feature reachable
        // only through a class-level gate or an AddFacts grant is covered.
        static BlueprintFeatureBase[] Reach(BlueprintFeatureBase root)
        {
            var seen = new HashSet<BlueprintFeatureBase>();
            var queue = new Queue<BlueprintFeatureBase>();
            queue.Enqueue(root);
            var result = new List<BlueprintFeatureBase>();
            while (queue.Count != 0)
            {
                var feature = queue.Dequeue();
                if (feature == null || !seen.Add(feature)) continue;
                result.Add(feature);

                if (feature is BlueprintProgression progression && progression.LevelEntries != null)
                    foreach (var entry in progression.LevelEntries)
                        if (entry?.Features != null)
                            foreach (var granted in entry.Features)
                                if (granted != null) queue.Enqueue(granted);

                foreach (var gate in feature.GetComponents<AddFeatureOnClassLevel>())
                {
                    var target = (Get(gate, "m_Feature") as BlueprintFeatureReference)?.Get() as BlueprintFeatureBase;
                    if (target != null) queue.Enqueue(target);
                }
                foreach (var grants in feature.GetComponents<AddFacts>())
                    foreach (var fact in grants.Facts.OfType<BlueprintFeatureBase>())
                        queue.Enqueue(fact);
            }
            return result.ToArray();
        }
    }
}
