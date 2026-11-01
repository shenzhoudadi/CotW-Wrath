using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;

namespace CallOfTheWild.Wrath.Prototype
{
    // Preserve GUIDs used by saves. Similar display names are never enough to
    // adopt, delete or replace a third-party blueprint.
    internal static class ContentIdentity
    {
        internal static void Validate(BlueprintFeature existing, BlueprintComponent[] expected)
        {
            if (existing.Ranks != 1 || existing.ComponentsArray == null ||
                expected.Any(required => !existing.ComponentsArray.Any(actual => actual != null &&
                    actual.GetType() == required.GetType() && actual.name == required.name)))
                throw new InvalidOperationException("Existing content identity has an unexpected component layout: " +
                    existing.name + " / " + existing.AssetGuid + ". Preserving the foreign asset.");
        }

        internal static string Describe(BlueprintsCache cache)
        {
            var lines = new List<string> { "Compatibility inventory (presence is not gameplay verification):" };
            var providers = new[] { "0Harmony", "UnityModManager", "ModMenu", "BrawlerClassWrath",
                "TabletopTweaks-Core", "TabletopTweaks-Base", "CharacterOptionsPlus", "ExpandedContent" };
            foreach (var provider in providers) {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == provider).ToArray();
                lines.Add(provider + ": " + (assemblies.Length == 0 ? "Not loaded" :
                    string.Join(", ", assemblies.Select(a => a.GetName().Version + " @ " + a.Location))));
            }
            foreach (var entry in new[] {
                new[] { BlueprintRegistration.FeatureId, BlueprintRegistration.FeatureName },
                new[] { BlueprintRegistration.ExtendedBaneId, "CotWWrathPrototypeExtendedBane" },
                new[] { BlueprintRegistration.ImprovedCombatExpertiseId, "CotWWrathPrototypeImprovedCombatExpertise" },
                new[] { ShieldedMage.Id, ShieldedMage.Name }
            }.Concat(RagePowers.FeatureIds.Select((id, i) => new[] { id, "CotWWrathPrototype" + RagePowers.Tokens[i] + "RagePower" }))
            .Concat(new[] { new[] { RagePowers.SharpenedFeatureId, "CotWWrathPrototypeSharpenedAccuracyRagePower" },
                new[] { PsychicClass.ClassId,"CotWWrathPrototypePsychicClass" },new[] { PsychicClass.BookId,"CotWWrathPrototypePsychicSpellbook" },
                new[] { PsychicClass.LogicalId,"CotWWrathPrototypeLogicalSpell" },new[] { PsychicAssets.Id("DisciplineSelection").ToString(),"CotWWrathPsychicDisciplineSelection" },
                new[] { PsychicAssets.Id("FerocityDiscipline").ToString(),"CotWWrathPsychicFerocityDiscipline" } })) {
                var bp = cache.Load(BlueprintGuid.Parse(entry[0]));
                lines.Add(entry[1] + " / " + entry[0] + ": " + (bp == null ? "Missing" : bp.GetType().Name + " " + bp.name));
            }
            // Inspect actual selectable objects. Candidate names are diagnostics only;
            // semantic equivalence must be audited before any provider is reused.
            var selections = new[] { BlueprintRegistration.BasicFeatsId, BlueprintRegistration.BrawlerCombatFeatSelectionId }
                .Concat(BlueprintRegistration.NativeCombatFeatSelectionIds).Concat(RagePowers.SelectionIds);
            var refs = new List<BlueprintFeature>();
            foreach (var id in selections) {
                var selection = cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection;
                if (selection == null) continue;
                var field = typeof(BlueprintFeatureSelection).GetField("m_AllFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var reference in (BlueprintFeatureReference[])field.GetValue(selection) ?? Array.Empty<BlueprintFeatureReference>()) {
                    var feature = reference == null ? null : cache.Load(reference.Guid) as BlueprintFeature;
                    if (feature != null) refs.Add(feature);
                }
            }
            foreach (var group in refs.Distinct().GroupBy(f => Canonical(f.name)).Where(g => g.Key != null &&
                g.Select(f => f.AssetGuid).Distinct().Count() > 1))
                lines.Add("POTENTIAL OVERLAP (no automatic merge): " + group.Key + " / " +
                    string.Join(", ", group.Select(f => f.AssetGuid.ToString()).Distinct()));
            return string.Join("\n", lines);
        }

        private static string Canonical(string name)
        {
            if (name == null) return null;
            foreach (var token in new[] { "SteadfastPersonality", "ExtendedBane", "ImprovedCombatExpertise", "ShieldedMage" })
                if (name == token || name == token + "Feature" || name == "CotWWrathPrototype" + token) return token;
            foreach (var token in RagePowers.Tokens.Concat(new[] { "SharpenedAccuracy" }))
                if (name == token || name == token + "Feature" || name == token + "RagePower" || name == "CotWWrathPrototype" + token + "RagePower") return token;
            return null;
        }
    }
}
