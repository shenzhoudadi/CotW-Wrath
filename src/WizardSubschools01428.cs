using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WizardSubschools01428
    {
        internal const string ModuleName = "Wizard schools: Arcane Crafter and Teleportation";
        internal const string SchoolPoolId = "5f838049069f1ac4d804ce0862ab5110";
        internal const string FeatPoolId = "8c3102c2ff3b69444b139a98521a4899";
        internal const string WizardId = "ba34257984f4c41408ce1dc2004e342e";
        internal const string CrafterToken = "ArcaneCrafterSchool";
        internal const string TeleportToken = "TeleportationSchool";
        internal const string MetachargeToken = "ArcaneCrafterMetacharge";
        internal static void Register(BlueprintsCache cache) {
            var f = new PsychicAssets(cache);
            var school = f.Native<BlueprintFeatureSelection>(SchoolPoolId);
            var wizard = f.Native<BlueprintCharacterClass>(WizardId);
            var crafter = Crafter(f, f.Native<BlueprintFeatureSelection>(FeatPoolId));
            var teleport = Teleportation(f, wizard);
            Migration01426.Commit(f, new[] { school }, crafter, teleport);
            Optional(cache);
        }
        // Keep the source feature prerequisites, and inspect only the actual wizard
        // bonus pool. Resolving references here cannot invalidate a cache enumeration.
        static BlueprintFeatureReference[] Metamagics(BlueprintFeatureSelection source) {
            var choices = ((BlueprintFeatureReference[])Get(source, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>())
                .Concat((BlueprintFeatureReference[])Get(source, "m_Features") ?? Array.Empty<BlueprintFeatureReference>())
                .Where(r => r != null).GroupBy(r => r.Guid).Select(g => g.First()).ToArray();
            var result = choices.Select(r => r.Get()).Where(b => b != null && b.HasGroup(FeatureGroup.WizardFeat) && b.GetComponent<AddMetamagicFeat>() != null)
                .Select(Ref<BlueprintFeatureReference>).ToArray();
            if (result.Length == 0) throw new InvalidOperationException("Arcane Crafter requires registered wizard metamagic feats.");
            return result;
        }
        internal static void Optional(BlueprintsCache cache) {
            var target = cache.Load(Id(MetachargeToken)) as BlueprintFeatureSelection;
            if (target == null) return;
            var source = cache.Load(BlueprintGuid.Parse(FeatPoolId)) as BlueprintFeatureSelection;
            if (source == null) throw new InvalidOperationException("Wizard bonus feat selection is missing; Metacharge preserved.");
            var choices = Metamagics(source); // Resolve/validate before mutating either field.
            var before = new[] { Get(target, "m_Features"), Get(target, "m_AllFeatures") };
            try { Set(target, "m_Features", choices.ToArray()); Set(target, "m_AllFeatures", choices.ToArray()); }
            catch { Set(target, "m_Features", before[0]); Set(target, "m_AllFeatures", before[1]); throw; }
        }
        static LevelEntry Entry(int level, params BlueprintFeatureBase[] features) {
            var entry = new LevelEntry { Level = level };
            Set(entry, "m_Features", features.Select(Ref<BlueprintFeatureBaseReference>).ToList()); return entry;
        }
        static BlueprintProgression CloneProgression(PsychicAssets f, BlueprintProgression source, string token, string title, string description) {
            if (source.LevelEntries == null || source.LevelEntries.Length == 0) throw new InvalidOperationException("Wizard school progression has no level entries.");
            var p = f.Clone(source, token, title, description);
            p.LevelEntries = CopyData(source.LevelEntries);
            Set(p, "m_Classes", CopyData(Get(source, "m_Classes")));
            Set(p, "m_Archetypes", CopyData(Get(source, "m_Archetypes")));
            Set(p, "m_UIDeterminatorsGroup", CopyData(Get(source, "m_UIDeterminatorsGroup")));
            p.UIGroups = CopyData(source.UIGroups);
            p.IsClassFeature = true;
            return p;
        }
        static BlueprintProgression Crafter(PsychicAssets f, BlueprintFeatureSelection feats) {
            var source = f.Native<BlueprintProgression>("0933849149cfc9244ac05d6a5b57fd80");
            if (source.LevelEntries.Count(e => e.Level == 1) != 1 || source.LevelEntries.First(e => e.Level == 1).Features.Count != 1 ||
                source.LevelEntries.First(e => e.Level == 1).Features[0].AssetGuid.ToString() != "38aab7423d96de84d8e6ab2cdbccce63")
                throw new InvalidOperationException("Universalist first-level feature layout changed; original school preserved.");
            // The clone has to carry the rewritten table. If the universalist module
            // ever runs after this one, Arcane Crafter would silently fall back to
            // the native fixed 8/12/16/20 uses, which is exactly the bug this guard
            // reports instead of shipping.
            if (!source.LevelEntries.Any(e => e.Level == 8 && e.Features.Count == 1 &&
                    e.Features[0].AssetGuid == Id(UniversalistMetamagic0609.RootToken)))
                throw new InvalidOperationException("Universalist Metamagic Mastery must be registered before Arcane Crafter.");
            const string description = "A universalist school option. Metacharge replaces Hand of the Apprentice: at wizard level 3, select one bonus metamagic feat from the registered wizard bonus feats. All prerequisites still apply. Metamagic Mastery is the shared tabletop version: at 8th level one entry lists every metamagic you know, and each use spends daily uses instead of handing out fixed free metamagics at 8th, 12th, 16th and 20th.";
            var choice = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true, IgnorePrerequisites = false, Groups = Array.Empty<FeatureGroup>(), ComponentsArray = Array.Empty<BlueprintComponent>() }, MetachargeToken, "Metacharge", "At wizard level 3, gain one bonus metamagic feat for which you qualify. Only registered wizard metamagic feats are offered.");
            var refs = Metamagics(feats); Set(choice, "m_Features", refs.ToArray()); Set(choice, "m_AllFeatures", refs.ToArray()); Set(choice, "m_Icon", Get(feats, "m_Icon"));
            var p = CloneProgression(f, source, CrafterToken, "Arcane Crafter", description);
            p.LevelEntries = p.LevelEntries.Where(e => e.Level != 1).Concat(new[] { Entry(3, choice) }).OrderBy(e => e.Level).ToArray();
            return p;
        }
        static AddFeatureOnClassLevel GrantAt(BlueprintCharacterClass wizard, BlueprintFeature feature, int level, bool before = false) {
            var grant = new AddFeatureOnClassLevel { Level = level, BeforeThisLevel = before };
            Set(grant, "m_Class", Ref<BlueprintCharacterClassReference>(wizard)); Set(grant, "m_Feature", Ref<BlueprintFeatureReference>(feature));
            Set(grant, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>()); Set(grant, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>()); return grant;
        }
        static BlueprintProgression Teleportation(PsychicAssets f, BlueprintCharacterClass wizard) {
            var source = f.Native<BlueprintProgression>("567801abe990faf4080df566fadcd038");
            var first = f.Native<BlueprintFeature>("cee0f7edbd874a042952ee150f878b84");
            var donor = f.Native<BlueprintAbility>("a9b8be9b87865744382f7c64e599aeb2");
            if (source.LevelEntries.Count(e => e.Level == 1) != 1 || source.LevelEntries.First(e => e.Level == 1).Features.Count(x => x == first) != 1 ||
                first.GetComponents<AddSpecialSpellList>().Count() != 1 || first.GetComponents<AddClassLevelToSummonDuration>().Count() != 1 ||
                first.GetComponents<AddFacts>().Count() != 1 || first.GetComponent<AddFacts>().Facts.Count() != 1 || first.GetComponent<AddFacts>().Facts.First().AssetGuid.ToString() != "b4b575d14c3bab6489aafa17ac2c909e" ||
                donor.GetComponents<AbilityCustomDimensionDoor>().Count() != 1 || donor.GetComponent<AbilityCustomDimensionDoor>().Radius.Meters != 0)
                throw new InvalidOperationException("Conjuration or caster-only dimension door layout changed; original school preserved.");
            const string description = "A conjuration school option. Shift replaces Acid Dart: teleport to a visible valid destination as a supernatural swift action, without provoking attacks of opportunity, 3 + Intelligence modifier times per day (minimum 1). Range is 5 feet per two wizard levels (minimum 5 feet; 50 feet at level 20). No nearby companions accompany you; the native mounted teleport rules remain. Summoner's Charm, the conjuration specialist spell list, both opposition school choices and the native level-8 Dimensional Steps remain.";
            var resource = f.Add(new BlueprintAbilityResource(), "TeleportationShiftResource", "Shift", description);
            var amount = Get(resource, "m_MaxAmount"); Set(amount, "BaseValue", 3); Set(amount, "IncreasedByStat", true); Set(amount, "ResourceBonusStat", StatType.Intelligence);
            foreach (var field in new[] { "m_Class", "m_ClassDiv" }) Set(amount, field, Array.Empty<BlueprintCharacterClassReference>());
            foreach (var field in new[] { "m_Archetypes", "m_ArchetypesDiv" }) Set(amount, field, Array.Empty<BlueprintArchetypeReference>());
            Set(resource, "m_MaxAmount", amount); Set(resource, "m_Min", 1);
            resource.LocalizedName = Text("TeleportationShiftResource", "name", "Shift"); resource.LocalizedDescription = Text("TeleportationShiftResource", "description", "Daily uses: 3 + Intelligence modifier (minimum 1).");
            var grantResource = new AddAbilityResources { RestoreAmount = true, RestoreOnLevelUp = true }; Set(grantResource, "m_Resource", Ref<BlueprintAbilityResourceReference>(resource));
            var feature = f.Clone(first, "TeleportationShiftFeature", "Teleportation", description);
            feature.ComponentsArray = feature.ComponentsArray.Where(c => !(c is AddFacts)).Concat(new BlueprintComponent[] { grantResource }).ToArray();
            Set(feature, "m_Icon", Get(donor, "m_Icon"));
            for (int i = 1; i <= 10; i++) {
                var a = f.Clone(donor, "TeleportationShift" + (i * 5), "Shift", description);
                a.Type = AbilityType.Supernatural; a.ActionType = UnitCommand.CommandType.Swift; Set(a, "m_IsFullRoundAction", false);
                a.Range = AbilityRange.Custom; a.CustomRange = new Feet(i * 5); a.IgnoreMinimalRangeLimit = true; a.AvailableMetamagic = 0; a.SpellResistance = false;
                a.MaterialComponent = new BlueprintAbility.MaterialComponentData(); Set(a, "m_Parent", new BlueprintAbilityReference());
                var cost = new AbilityResourceLogic { Amount = 1 }; Set(cost, "m_IsSpendResource", true); Set(cost, "m_RequiredResource", Ref<BlueprintAbilityResourceReference>(resource));
                a.ComponentsArray = a.ComponentsArray.Where(c => c is AbilityCustomDimensionDoor || c is AbilitySpawnFx || c.GetType().Namespace == "Kingmaker.UnitLogic.Abilities.Components.TargetCheckers")
                    .Concat(new BlueprintComponent[] { cost }).ToArray();
                // Explicitly reject the donor's sight bypass even if its namespace changes.
                a.ComponentsArray = a.ComponentsArray.Where(c => c.GetType().Name != "LineOfSightIgnorance" && c.GetType().Name != "DimensionDoorRestrictionIgnorance").ToArray();
                var leaf = f.Feature("TeleportationShift" + (i * 5) + "Feature", "Shift", description, Facts(a)); leaf.HideInUI = leaf.HideInCharacterSheetAndLevelUp = true;
                var lower = GrantAt(wizard, leaf, i == 1 ? 1 : i * 2);
                if (i < 10) {
                    var window = f.Feature("TeleportationShift" + (i * 5) + "Window", "Shift", description, lower); window.HideInUI = window.HideInCharacterSheetAndLevelUp = true;
                    feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { GrantAt(wizard, window, i * 2 + 2, true) }).ToArray();
                } else feature.ComponentsArray = feature.ComponentsArray.Concat(new BlueprintComponent[] { lower }).ToArray();
            }
            var p = CloneProgression(f, source, TeleportToken, "Teleportation", description);
            p.LevelEntries = p.LevelEntries.Select(e => e.Level != 1 ? e : Entry(1, e.Features.Select(x => x.AssetGuid == first.AssetGuid ? feature : x).ToArray())).ToArray();
            return p;
        }
    }
}
