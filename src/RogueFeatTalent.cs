using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class RogueFeatTalent
    {
        internal const string ModuleName = "Advanced talent: Feat";
        internal const string Token = "RogueAdvancedFeatTalent";
        sealed class ChoiceDiagnostics { internal string[] Skipped = Array.Empty<string>(); }
        static readonly ConditionalWeakTable<BlueprintsCache, ChoiceDiagnostics> diagnostics = new ConditionalWeakTable<BlueprintsCache, ChoiceDiagnostics>();
        internal static string[] SkippedChoices(BlueprintsCache cache) => diagnostics.GetValue(cache, _ => new ChoiceDiagnostics()).Skipped.ToArray();
        internal static readonly string[] PoolIds = ReapingStalker.PoolIds.Concat(new[] {
            "c074a5d615200494b8f2a9c845799d93", "14d9089df87a43b696fa9451ca2f0a12",
            "1edbb68422c046f98a86f4726f6721b2", "290bbcc3c3bb92144b853fd8fb8ff452", "4b7018b1ed4b27140a5e7adfacaaf9c6"
        }).ToArray();
        internal static void Register(BlueprintsCache cache) {
            var f = new PsychicAssets(cache);
            var source = f.Native<BlueprintFeatureSelection>(BlueprintRegistration.BasicFeatsId);
            var pools = PoolIds.Select(f.Native<BlueprintFeatureSelection>).ToArray();
            var talent = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true,
                Groups = new[] { FeatureGroup.RogueTalent, FeatureGroup.SlayerTalent }, Mode = source.Mode,
                IgnorePrerequisites = false, ComponentsArray = new BlueprintComponent[] { Migration01426.Require(f.Native<BlueprintFeature>(ReapingStalker.AdvancedId)) }
            }, Token, "Feat (Advanced Talent)", "Choose an ordinary feat for which you qualify in place of a rogue or slayer talent. Requires Advanced Talents. This advanced talent can be selected only once; all prerequisites and parameter choices of the chosen feat still apply. Selections that lead back to this talent (including Extra Rogue/Slayer Talent) are excluded to prevent a recursive selection menu.");
            var once = new PrerequisiteNoFeature(); Set(once, "m_Feature", Ref<BlueprintFeatureReference>(talent));
            talent.ComponentsArray = talent.ComponentsArray.Concat(new BlueprintComponent[] { once }).ToArray();
            Set(talent, "m_Icon", Get(source, "m_Icon")); SetChoices(cache, source, talent);
            Migration01426.Commit(f, pools, talent);
        }
        static void SetChoices(BlueprintsCache cache, BlueprintFeatureSelection source, BlueprintFeatureSelection target) {
            var refs = ((BlueprintFeatureReference[])Get(source, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
                .Where(r => r != null && r.Guid != Id(Token)).GroupBy(r => r.Guid).Select(g => g.First()).ToArray();
            if (refs.Length == 0) throw new InvalidOperationException("Advanced talent Feat requires a valid ordinary feat selection.");
            var skipped = new List<string>();
            refs = refs.Where(r => {
                var unresolved = new List<string>();
                bool unsafeBranch = ReturnsToTalent(r.Get(), new HashSet<BlueprintGuid>(), unresolved);
                if (unresolved.Count != 0) skipped.Add(r.Guid + ": " + string.Join(", ", unresolved.Distinct()));
                return !unsafeBranch;
            }).ToArray();
            if (refs.Length == 0) throw new InvalidOperationException("Advanced talent Feat has no non-recursive ordinary choices.");
            Set(target, "m_Features", refs.ToArray()); Set(target, "m_AllFeatures", refs.ToArray());
            var current = diagnostics.GetValue(cache, _ => new ChoiceDiagnostics()); var next = skipped.OrderBy(x => x).ToArray();
            if (!current.Skipped.SequenceEqual(next)) {
                current.Skipped = next;
                ModEntry.LogSpellDiagnostic(ModuleName + ": " + (next.Length == 0 ? "All ordinary selection branches resolve." : "Deferred unresolved ordinary selection branches: " + string.Join("; ", next)));
            }
        }
        static bool ReturnsToTalent(BlueprintFeature feature, HashSet<BlueprintGuid> visited, List<string> unresolved) {
            // A late optional selection can contain references whose provider has
            // not published its children yet. Defer that entire branch instead
            // of either admitting an unproven recursive path or failing Feat.
            if (feature == null) { unresolved.Add("unresolved feature"); return true; }
            if (feature.AssetGuid == Id(Token) || PoolIds.Contains(feature.AssetGuid.ToString()) ||
                new[] { ReapingStalker.TttExtraId, "739de36c402e4560b586cf79d00e3cbb", "04586da4ae28422da09e40d8241d36dc" }.Contains(feature.AssetGuid.ToString())) return true;
            if (!(feature is BlueprintFeatureSelection selection)) return false;
            if (!visited.Add(feature.AssetGuid)) return true;
            var children = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
            bool result = children.Length == 0;
            if (result) unresolved.Add(feature.AssetGuid + " empty selection");
            foreach (var reference in children) {
                if (reference == null) { unresolved.Add(feature.AssetGuid + " null child"); result = true; }
                else {
                    var child = reference.Get();
                    if (child == null) { unresolved.Add(reference.Guid + " unresolved child"); result = true; }
                    else if (ReturnsToTalent(child, visited, unresolved)) result = true;
                }
            }
            visited.Remove(feature.AssetGuid); return result;
        }
        internal static void Optional(BlueprintsCache cache) {
            var talent = cache.Load(Id(Token)) as BlueprintFeatureSelection;
            if (talent == null) return;
            var source = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BasicFeatsId)) as BlueprintFeatureSelection;
            var providers = new[] {
                new[] { ReapingStalker.TttExtraId, "ExtraSlayerTalent" },
                new[] { "739de36c402e4560b586cf79d00e3cbb", "ExtraRogueTalent" },
                new[] { "04586da4ae28422da09e40d8241d36dc", "ExtraRogueTalentSylvan" }
            };
            var pools = providers.Select(entry => {
                var ttt = cache.Load(BlueprintGuid.Parse(entry[0])) as BlueprintFeatureSelection;
                return ttt != null && ttt.name == entry[1] && ttt.HasGroup(FeatureGroup.Feat) && !ttt.IgnorePrerequisites ? ttt : null;
            }).Where(p => p != null).ToArray();
            var affected = pools.Concat(new[] { talent }).ToArray();
            var before = affected.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try {
                if (source != null) SetChoices(cache, source, talent);
                T09CombatFeats.Append(pools, new BlueprintFeature[] { talent });
            } catch {
                for (int i = 0; i < affected.Length; i++) { Set(affected[i], "m_Features", before[i][0]); Set(affected[i], "m_AllFeatures", before[i][1]); }
                throw;
            }
        }
    }
}
