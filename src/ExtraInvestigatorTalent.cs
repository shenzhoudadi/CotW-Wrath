using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Optional HA integration: consume the loaded talent selection, without
    // cloning unknown provider grant components or altering its progression.
    internal static class ExtraInvestigatorTalent
    {
        internal const string ModuleName = "Extra Investigator Talent";
        internal const string Token = "ExtraInvestigatorTalentFeat";
        internal const string ClassId = "adb9e138bee9ecc4db246b64d563f900";
        internal const string ProgressionId = "5eaea532547c7ce40a84563e15fd3440";
        internal const string TalentSelectionId = "da89239cc5859a1478007cd5e6d44f67";
        internal static BlueprintGuid FeatId => Id(Token);
        internal static string LastSummary = "HomebrewArchetypes Investigator talents have not been detected.";

        static T Provider<T>(BlueprintsCache cache, string id, string name) where T : SimpleBlueprint
        {
            var bp = cache.Load(BlueprintGuid.Parse(id));
            if (bp == null) return null;
            if (bp.GetType() != typeof(T) || bp.name != name || bp.AssetGuid != BlueprintGuid.Parse(id))
                throw new InvalidOperationException("Unexpected HomebrewArchetypes identity: " + name + " / " + id);
            return (T)bp;
        }

        static BlueprintFeatureReference[] References(BlueprintFeatureSelection selection, string field)
            => (BlueprintFeatureReference[])Get(selection, field) ?? Array.Empty<BlueprintFeatureReference>();

        static BlueprintFeatureReference[] CopyReferences(BlueprintsCache cache, BlueprintFeatureReference[] source)
            => source.Select(r => Ref<BlueprintFeatureReference>((BlueprintFeature)cache.Load(r.Guid))).ToArray();

        // Inspect nested selections before exposing them to native recursive
        // availability checks. Missing children indicate unfinished registration.
        static bool ReadyGraph(BlueprintsCache cache, BlueprintFeatureSelection selection,
            HashSet<BlueprintGuid> visiting, HashSet<BlueprintGuid> complete)
        {
            if (complete.Contains(selection.AssetGuid)) return true;
            if (!visiting.Add(selection.AssetGuid))
                throw new InvalidOperationException("Cyclic Investigator talent selection: " + selection.AssetGuid);
            try
            {
                var all = References(selection, "m_AllFeatures");
                if (all.Length == 0) return false;
                foreach (var reference in References(selection, "m_Features").Concat(all))
                {
                    if (reference == null) return false;
                    if (reference.Guid == FeatId)
                        throw new InvalidOperationException("Investigator talents refer back to Extra Investigator Talent.");
                    var bp = cache.Load(reference.Guid);
                    if (bp == null) return false;
                    if (!(bp is BlueprintFeature) || bp.AssetGuid != reference.Guid)
                        throw new InvalidOperationException("Invalid Investigator talent reference: " + reference.Guid);
                    if (bp is BlueprintFeatureSelection nested && !ReadyGraph(cache, nested, visiting, complete)) return false;
                }
                complete.Add(selection.AssetGuid);
                return true;
            }
            finally { visiting.Remove(selection.AssetGuid); }
        }

        static string[] PrerequisiteSignatures(IEnumerable<BlueprintComponent> components)
            => components.OfType<Prerequisite>().Select(p => PsychicExpansion.Signature(p)).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        static bool SameReferences(BlueprintFeatureReference[] a, BlueprintFeatureReference[] b)
            => a.All(r => r != null) && b.All(r => r != null) &&
               new HashSet<BlueprintGuid>(a.Select(r => r.Guid)).SetEquals(b.Select(r => r.Guid));

        static BlueprintFeatureSelection Equivalent(BlueprintsCache cache, BlueprintFeatureSelection basic,
            BlueprintFeatureSelection donor, BlueprintComponent[] prerequisites)
        {
            var expected = PrerequisiteSignatures(prerequisites);
            // Native Items enumerates m_AllFeatures; an orphaned m_Features
            // entry is not an available external replacement.
            foreach (var reference in References(basic, "m_AllFeatures"))
            {
                if (reference == null || reference.Guid == donor.AssetGuid || reference.Guid == FeatId) continue;
                var candidate = cache.Load(reference.Guid) as BlueprintFeatureSelection;
                if (candidate == null || candidate.GetType() != typeof(BlueprintFeatureSelection) ||
                    !candidate.HasGroup(FeatureGroup.Feat) || candidate.HideInUI || candidate.IgnorePrerequisites ||
                    candidate.Mode != donor.Mode || candidate.ExceptWhiteListed != donor.ExceptWhiteListed ||
                    candidate.Obligatory != donor.Obligatory || candidate.Group != donor.Group || candidate.Group2 != donor.Group2 ||
                    candidate.ShowThisSelection != donor.ShowThisSelection || candidate.ComponentsArray == null) continue;
                // Do not hide our entry for a stricter choice or a feature with
                // extra gameplay effects. Feature tags are presentation metadata.
                if (candidate.ComponentsArray.Any(c => c == null || (!(c is Prerequisite) && c.GetType().Name != "FeatureTagsComponent"))) continue;
                if (!PrerequisiteSignatures(candidate.ComponentsArray).SequenceEqual(expected)) continue;
                if (SameReferences(References(candidate, "m_Features"), References(donor, "m_Features")) &&
                    SameReferences(References(candidate, "m_AllFeatures"), References(donor, "m_AllFeatures"))) return candidate;
            }
            return null;
        }

        static void ValidateOwned(SimpleBlueprint existing, BlueprintFeatureSelection donor, BlueprintComponent[] components)
        {
            if (existing == null) return;
            var selection = existing as BlueprintFeatureSelection;
            if (existing.GetType() != typeof(BlueprintFeatureSelection) || existing.AssetGuid != FeatId || existing.name != "CotWWrathPsychic" + Token ||
                selection.Ranks != 1 || selection.Groups == null || !selection.Groups.SequenceEqual(new[] { FeatureGroup.Feat }) ||
                selection.IgnorePrerequisites || selection.Mode != donor.Mode || selection.ComponentsArray == null ||
                selection.ComponentsArray.Any(c => !(c is Prerequisite)) ||
                !PrerequisiteSignatures(selection.ComponentsArray).SequenceEqual(PrerequisiteSignatures(components)))
                throw new InvalidOperationException("Extra Investigator Talent identity collision; existing blueprint preserved.");
        }

        // False means absent/unfinished provider or an existing equivalent entry;
        // callers must not report a missing dependency as successful registration.
        internal static bool Register(BlueprintsCache cache)
        {
            if (cache == null) return false;
            var character = Provider<BlueprintCharacterClass>(cache, ClassId, "InvestigatorClass");
            var progression = Provider<BlueprintProgression>(cache, ProgressionId, "InvestigatorProgression");
            var donor = Provider<BlueprintFeatureSelection>(cache, TalentSelectionId, "InvestigatorTalentSelection");
            if (character == null || progression == null || donor == null) return false;
            var link = (BlueprintProgressionReference)Get(character, "m_Progression");
            if (link == null) return false;
            if (link.Guid != progression.AssetGuid)
                throw new InvalidOperationException("Investigator class does not use its expected progression.");
            if (progression.LevelEntries == null || !progression.LevelEntries.Any(level => level != null &&
                ((List<BlueprintFeatureBaseReference>)Get(level, "m_Features"))?.Any(r => r?.Guid == donor.AssetGuid) == true)) return false;
            if (donor.IgnorePrerequisites)
                throw new InvalidOperationException("Investigator talent selection bypasses talent prerequisites.");
            if (!ReadyGraph(cache, donor, new HashSet<BlueprintGuid>(), new HashSet<BlueprintGuid>())) return false;
            if (donor.ComponentsArray == null || donor.ComponentsArray.Any(c => c == null)) return false;

            var basic = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BasicFeatsId)) as BlueprintFeatureSelection;
            if (basic == null) return false;
            var requires = new PrerequisiteFeature { name = "ExtraInvestigatorTalentRequiresTalents", Group = Prerequisite.GroupType.All };
            Set(requires, "m_Feature", Ref<BlueprintFeatureReference>(donor));
            var components = donor.ComponentsArray.OfType<Prerequisite>().Select(p => (BlueprintComponent)CopyData(p))
                .Concat(new BlueprintComponent[] { requires }).ToArray();
            var existing = cache.Load(FeatId);
            ValidateOwned(existing, donor, components);
            var equivalent = Equivalent(cache, basic, donor, components);
            var oldBasicFeatures = References(basic, "m_Features");
            var oldBasicAll = References(basic, "m_AllFeatures");
            if (equivalent != null)
            {
                // A later provider may add the same feat. Keep our saved identity
                // but remove only its ordinary-pool links, never foreign entries.
                try
                {
                    if (oldBasicFeatures.Any(r => r?.Guid == FeatId)) Set(basic, "m_Features", oldBasicFeatures.Where(r => r?.Guid != FeatId).ToArray());
                    if (oldBasicAll.Any(r => r?.Guid == FeatId)) Set(basic, "m_AllFeatures", oldBasicAll.Where(r => r?.Guid != FeatId).ToArray());
                }
                catch { Set(basic, "m_Features", oldBasicFeatures); Set(basic, "m_AllFeatures", oldBasicAll); throw; }
                LastSummary = "Existing equivalent ordinary feat retained: " + equivalent.name + " / " + equivalent.AssetGuid;
                return false;
            }

            var features = CopyReferences(cache, References(donor, "m_Features"));
            var all = CopyReferences(cache, References(donor, "m_AllFeatures"));
            var assets = new PsychicAssets(cache);
            var root = existing as BlueprintFeatureSelection;
            if (root == null)
            {
                root = assets.Add(new BlueprintFeatureSelection {
                    Ranks = 1, Groups = new[] { FeatureGroup.Feat }, IsClassFeature = true, ReapplyOnLevelUp = true,
                    Mode = donor.Mode, Group = donor.Group, Group2 = donor.Group2, IgnorePrerequisites = false,
                    ExceptWhiteListed = donor.ExceptWhiteListed, Obligatory = donor.Obligatory,
                    ShowThisSelection = donor.ShowThisSelection, ComponentsArray = components
                }, Token, ModuleName, "Gain one additional investigator talent for which you qualify. Requires the investigator talent class feature from HomebrewArchetypes. You can select this feat multiple times, choosing a talent each time; each talent retains its own prerequisites and limits.");
                Set(root, "m_Icon", Get(donor, "m_Icon"));
                PsychicRepairComponents.UniqueNames(root);
            }
            var oldFeatures = References(root, "m_Features");
            var oldAll = References(root, "m_AllFeatures");
            try
            {
                Set(root, "m_Features", features); Set(root, "m_AllFeatures", all);
                if (existing == null) { assets.Enable(); cache.AddCachedBlueprint(root.AssetGuid, root); }
                T09CombatFeats.Append(new[] { basic }, new BlueprintFeature[] { root });
            }
            catch
            {
                Set(basic, "m_Features", oldBasicFeatures); Set(basic, "m_AllFeatures", oldBasicAll);
                Set(root, "m_Features", oldFeatures); Set(root, "m_AllFeatures", oldAll);
                if (existing == null && ReferenceEquals(cache.Load(FeatId), root)) cache.RemoveCachedBlueprint(FeatId);
                throw;
            }
            LastSummary = "Registered from the loaded HomebrewArchetypes Investigator talent selection.";
            return true;
        }
    }
}
