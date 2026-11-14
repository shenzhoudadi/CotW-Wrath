// Kinetic Knight Blade Rush: the native Kineticist archetype receives DarkCodex's
// Blade Rush infusion at 3rd level, mirroring how the same archetype already
// hands out Blade Whirlwind at 9th level (KineticKnightArchetype.AddFeatures).
// Blade Rush itself is DarkCodex content, so the module stays dormant when that
// provider is absent and never publishes a blueprint of its own.
//
// DarkCodex is a Unity Mod Manager mod and publishes its blueprints after the
// native modification entry point runs, so the first attempt almost always
// misses. The attempt therefore arms a cache watcher instead of failing: as soon
// as the Blade Rush feature or ability is added to the cache the archetype is
// patched and the module reports success.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class KineticKnightBladeRush
    {
        internal const string ModuleName = "Kinetic Knight Blade Rush";
        internal static string LastSummary = "Not registered";

        internal const string ArchetypeId = "7d61d9b2250260a45b18c5634524a8fb";
        // DarkCodex fixes these two GUIDs in DarkCodex/Settings/blueprints.txt.
        internal const string RushFeatureId = "350b164b8fd942f4a37af9fe31d6b97a";
        internal const string RushAbilityId = "e6b5691631c7417696af64bdef75f706";
        internal const int RushLevel = 3;

        static readonly BlueprintGuid RushGuid = BlueprintGuid.Parse(RushFeatureId);
        static readonly ConditionalWeakTable<BlueprintsCache, object> Done = new ConditionalWeakTable<BlueprintsCache, object>();

        // True while the provider has not appeared yet; read on every blueprint
        // insertion, so the watcher can return immediately once it is settled.
        internal static bool Waiting;
        internal static Action<string, bool, Exception> Report;

        internal static bool Register(BlueprintsCache cache, Action<string, bool, Exception> report)
        {
            Report = report;
            if (Done.TryGetValue(cache, out _)) { LastSummary = "Already registered"; return true; }
            if (TryAttach(cache)) return true;
            // Not published yet. Do not treat this as "provider absent": the
            // watcher finishes the work as soon as Blade Rush enters the cache.
            Waiting = true;
            LastSummary = "Waiting for the Blade Rush provider to be published.";
            return false;
        }

        internal static void ProviderPublished(BlueprintsCache cache, BlueprintGuid published)
        {
            if (!Waiting) return;
            if (published != RushGuid && published != BlueprintGuid.Parse(RushAbilityId)) return;
            if (Done.TryGetValue(cache, out _)) { Waiting = false; return; }
            if (!TryAttach(cache)) return;
            Waiting = false;
            var report = Report;
            if (report != null) report(ModuleName, true, null);
        }

        static bool TryAttach(BlueprintsCache cache)
        {
            var rush = cache.Load(RushGuid) as BlueprintFeature;
            var ability = cache.Load(BlueprintGuid.Parse(RushAbilityId)) as BlueprintAbility;
            if (rush == null || ability == null) return false;
            var archetype = cache.Load(BlueprintGuid.Parse(ArchetypeId)) as BlueprintArchetype
                ?? throw new InvalidOperationException("Kinetic Knight archetype is unavailable.");
            var origin = archetype.AddFeatures ?? Array.Empty<LevelEntry>();
            var before = origin.ToArray();
            if (before.Any(HasRush))
            {
                Done.Add(cache, new object());
                LastSummary = "Blade Rush is already granted at 3rd level.";
                return true;
            }
            try
            {
                var index = Array.FindIndex(before, e => e.Level == RushLevel);
                LevelEntry entry;
                if (index >= 0)
                {
                    // Copy the entry instead of editing the native one, so undo
                    // restores the original array without a stale mutation.
                    entry = new LevelEntry { Level = RushLevel };
                    Set(entry, "m_Features", Features(before[index]).Concat(new[] { Ref<BlueprintFeatureBaseReference>(rush) }).ToList());
                    before[index] = entry;
                }
                else
                {
                    entry = new LevelEntry { Level = RushLevel };
                    Set(entry, "m_Features", new List<BlueprintFeatureBaseReference> { Ref<BlueprintFeatureBaseReference>(rush) });
                }
                archetype.AddFeatures = index >= 0
                    ? before
                    : before.Concat(new[] { entry }).OrderBy(e => e.Level).ToArray();
                Verify(cache, rush);
            }
            catch
            {
                archetype.AddFeatures = origin;
                throw;
            }
            Done.Add(cache, new object());
            LastSummary = "Added the Blade Rush infusion to the Kinetic Knight at 3rd level.";
            return true;
        }

        static bool HasRush(LevelEntry entry)
            => entry != null && entry.Level == RushLevel &&
               Features(entry).Any(r => r != null && r.Guid == RushGuid);

        // Native level entries deserialize m_Features either as a list or as an
        // array depending on the blueprint; both satisfy the interface, so the
        // scan never misses an existing grant.
        static IEnumerable<BlueprintFeatureBaseReference> Features(LevelEntry entry)
            => Get(entry, "m_Features") as IEnumerable<BlueprintFeatureBaseReference>
               ?? Enumerable.Empty<BlueprintFeatureBaseReference>();

        static void Verify(BlueprintsCache cache, BlueprintFeature rush)
        {
            var archetype = cache.Load(BlueprintGuid.Parse(ArchetypeId)) as BlueprintArchetype
                ?? throw new InvalidOperationException("Kinetic Knight archetype disappeared.");
            var grants = (archetype.AddFeatures ?? Array.Empty<LevelEntry>())
                .Where(e => e.Level == RushLevel)
                .SelectMany(Features)
                .Count(r => r != null && r.Guid == rush.AssetGuid);
            if (grants != 1)
                throw new InvalidOperationException("Kinetic Knight lists Blade Rush " + grants + " times at 3rd level.");
        }
    }

    // Late-provider hook: DarkCodex adds Blade Rush through
    // ResourcesLibrary.BlueprintsCache.AddCachedBlueprint.
    [HarmonyPatch(typeof(BlueprintsCache), "AddCachedBlueprint")]
    internal static class KineticKnightBladeRushWatcher
    {
        private static void Postfix(BlueprintsCache __instance, BlueprintGuid __0)
        {
            if (!KineticKnightBladeRush.Waiting) return;
            KineticKnightBladeRush.ProviderPublished(__instance, __0);
        }
    }
}
