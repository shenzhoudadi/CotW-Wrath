using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;

namespace CallOfTheWild.Wrath.Prototype
{
    // Keep MediumClass.dll's assembly identity for saved components. One native
    // entry point owns both modules; no second UMM entry or casting patch owner.
    internal static class MediumIntegration
    {
        internal const string ModuleName = "Medium class";
        internal const string BookId = "182182848e8e40c8b029adfa64eba37f";
        internal const string CenterId = "719630b974ad5a9f8bf3742c3b07dd3f";
        private static Type entry;
        private static Exception preparationError;
        private static bool prepared, configured;

        internal static void Prepare()
        {
            if (prepared) return;
            prepared = true;
            try {
                var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(ModEntry).Assembly.Location),
                    "../Modules/Medium/MediumClass.dll"));
                var existing = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "MediumClass");
                if (existing != null && !string.Equals(Path.GetFullPath(existing.Location), path, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Standalone Medium Class is loaded. Disable/remove its UMM installation, keep the bundled module, and restart.");
                if (!File.Exists(path) || !File.Exists(Path.Combine(Path.GetDirectoryName(path), "mediumclass_assets")))
                    throw new FileNotFoundException("The bundled Medium module or asset bundle is missing. Reinstall the complete CotW archive.");
                entry = (existing ?? Assembly.LoadFrom(path)).GetType("MediumClass.Main", true);
                entry.GetMethod("PrepareIntegrated").Invoke(null, null);
            } catch (Exception e) { preparationError = e; }
        }

        internal static void Register(BlueprintsCache cache)
        {
            if (configured) return;
            if (!prepared || preparationError != null)
                throw new InvalidOperationException("Bundled Medium could not start. It needs the existing UMM framework, ModMenu and Tabletop Tweaks Core dependencies; inspect the inner exception.", preparationError);
            if (!PsychicCasting.RuntimePatchesAvailable || cache.Load(BlueprintGuid.Parse(PsychicClass.LogicalId)) == null)
                throw new InvalidOperationException("Medium requires successful shared Psychic casting registration.");
            entry.GetMethod("ConfigureIntegrated").Invoke(null, null);
            BindSharedCasting(cache);
            configured = true;
        }

        internal static void BindSharedCasting(BlueprintsCache cache)
        {
            var book = cache.Load(BlueprintGuid.Parse(BookId)) as BlueprintSpellbook
                ?? throw new InvalidOperationException("Medium spellbook was not registered.");
            var logical = cache.Load(BlueprintGuid.Parse(PsychicClass.LogicalId)) as BlueprintFeature
                ?? throw new InvalidOperationException("Shared Logical Spell was not registered.");
            BindSharedBlueprints(book, logical);
        }

        internal static void BindSharedBlueprints(BlueprintSpellbook book, BlueprintFeature logical)
        {
            var medium = book.CharacterClass ?? throw new InvalidOperationException("Medium spellbook has no class.");
            if (!book.GetComponents<PsychicSpellbookMarker>().Any()) {
                var marker = new PsychicSpellbookMarker { name = "CotWSharedPsychicCasting", MaximumSpellLevel = 6 };
                book.ComponentsArray = book.ComponentsArray.Concat(new BlueprintComponent[] { marker }).ToArray();
            }
            PsychicCasting.CenterBuffs.Add(BlueprintGuid.Parse(CenterId));
            foreach (var requirement in logical.GetComponents<PrerequisiteClassLevel>())
                requirement.Group = Prerequisite.GroupType.Any;
            if (!logical.GetComponents<PrerequisiteClassLevel>().Any(p =>
                ((BlueprintCharacterClassReference)AccessTools.Field(typeof(PrerequisiteClassLevel), "m_CharacterClass").GetValue(p))?.Guid == medium.AssetGuid)) {
                var requirement = new PrerequisiteClassLevel { name = "LogicalRequiresMedium", Level = 1, Group = Prerequisite.GroupType.Any };
                AccessTools.Field(typeof(PrerequisiteClassLevel), "m_CharacterClass").SetValue(requirement,
                    BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(medium));
                logical.ComponentsArray = logical.ComponentsArray.Concat(new BlueprintComponent[] { requirement }).ToArray();
            }
        }
    }
}
