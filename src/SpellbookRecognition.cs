// Universal spellbook recognition for every prestige class that asks the
// player to pick a base casting class — native (Arcane Trickster, Dragon
// Disciple, Eldritch Knight, Loremaster, Mystic Theurge, Winter Witch) and
// third-party (Prestige-Plus Spellupgrade/Spellupgrade2 and any other mod that
// builds the same shape). Discovery is structural, not a GUID list, so a mod
// adding a new picker is covered without touching this file.
//
// Two template shapes exist and both are cloned per target spellbook:
//   * BlueprintFeatureReplaceSpellbook (m_Spellbook)
//   * BlueprintProgression whose level-up features carry AddSpellbookLevel
// Third-party prerequisites are retargeted generically: object references,
// class references and plain GUID strings (Prestige-Plus stores Book as a
// string). Native blueprints are never mutated.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Designers.Mechanics.Facts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SpellbookRecognition
    {
        private static BlueprintsCache Cache;
        private static bool Reconciling;
        private static int KnownBookCount = -1;
        private static BlueprintSpellbook[] CachedBooks = Array.Empty<BlueprintSpellbook>();
        private static readonly HashSet<BlueprintGuid> KnownPickers = new HashSet<BlueprintGuid>();
        private static readonly Dictionary<BlueprintGuid, int> CheckedNonPickers = new Dictionary<BlueprintGuid, int>();
        private static readonly Dictionary<BlueprintGuid, int> InjectedBookVersion = new Dictionary<BlueprintGuid, int>();
        private static readonly object PickerGate = new object();

        // Native prestige spellbook pickers + PrestigePlus pickers: load explicitly
        // during Register because they are still unloaded TOC entries at cache init.
        private const string DragonDiscipleProgressionId = "69fc2bad2eb331346a6c777423e0d0f7";
        private const string PrestigePlusSpellupgrade2Id = "05dc9561054241bd9e9f404f59ab68c5";
        private const string DragonDiscipleSelectionId = "8c1ba14c0b6dcdb439c56341385ee474";

        private static readonly string[] KnownPickerGuids = {
            "ae04b7cdeb88b024b9fd3882cc7d3d76", // ArcaneTricksterSpellbookSelection
            "8c1ba14c0b6dcdb439c56341385ee474", // DragonDiscipleSpellbookSelection
            "dc3ab8d0484467a4787979d93114ebc3", // EldritchKnightSpellbookSelection
            "7a28ab4dfc010834eabc770152997e87", // LoremasterSpellbookSelection
            "97f510c6483523c49bc779e93e4c4568", // MysticTheurgeArcaneSpellbookSelection
            "7cd057944ce7896479717778330a4933", // MysticTheurgeDivineSpellbookSelection
            "ea20b26d9d0ede540af3c74246dade41", // WinterWitchSpellbookSelection
            "68782aa7a302b6d43a42a71c6e9b5277", // HellknightSigniferSpellbook
            PrestigePlusSpellupgrade2Id,        // PrestigePlus Spellupgrade2
            "e995a687ec474a299afe1c2715e0cc24", // PrestigePlus Spellupgrade
            "7e72ea5394887f04a8cc48b305d3a6ac", // HomebrewArchetypes RazPSpellbookSelection
            "638b1fbb6bc04ec481adde3ddbc6ae5b", // ExpandedContent StargazerSpellbook
            "f5007987c484485a92e30821881f908d"  // MysticalMayhem CyphermageSpellbookSelection
        };

        internal static void Register(BlueprintsCache cache)
        {
            Cache = cache;
            // The toggle is read once per session: injected variants live in the
            // blueprint graph, so changing it takes effect after a restart.
            if (!SpellbookRecognitionSettings.Enabled) return;
            foreach (var id in KnownPickerGuids)
            {
                try { cache.Load(BlueprintGuid.Parse(id)); } catch { }
            }
            Reconcile(cache);
        }

        // Late providers are handled on-demand when any selection is opened,
        // without polling or forcing load of the whole TOC.
        internal static void Reconcile(BlueprintsCache cache)
        {
            if (cache == null || Reconciling || !SpellbookRecognitionSettings.Enabled) return;
            Reconciling = true;
            try
            {
                CleanupDragonDiscipleDuplicate(cache);
                var f = new PsychicAssets(cache);
                RefreshBooks(cache);
                if (CachedBooks.Length == 0) return;
                var snapshot = SublimeChord.Snapshot(cache);
                foreach (var selection in snapshot.OfType<BlueprintFeatureSelection>())
                    TryInjectSelection(cache, f, selection);
            }
            catch (Exception e) { ModEntry.LogSpellDiagnostic("Spellbook recognition skipped: " + e.Message); }
            finally { Reconciling = false; }
        }

        private static void CleanupDragonDiscipleDuplicate(BlueprintsCache cache)
        {
            var prog = cache.Load(BlueprintGuid.Parse(DragonDiscipleProgressionId)) as BlueprintProgression;
            if (prog?.LevelEntries == null) return;
            var ppId = BlueprintGuid.Parse(PrestigePlusSpellupgrade2Id);
            var ddId = BlueprintGuid.Parse(DragonDiscipleSelectionId);
            bool hasLevel2Native = prog.LevelEntries.Any(e => e.Level == 2 && e.Features.Any(f => f != null && f.AssetGuid == ddId));
            if (!hasLevel2Native) return;
            var level1 = prog.LevelEntries.FirstOrDefault(e => e.Level == 1);
            if (level1 == null) return;
            var l1Refs = Get(level1, "m_Features") as List<BlueprintFeatureBaseReference>;
            if (l1Refs == null) return;
            l1Refs.RemoveAll(r => r != null && r.Guid == ppId);
        }

        private static void RefreshBooks(BlueprintsCache cache)
        {
            var snapshot = SublimeChord.Snapshot(cache);
            var books = snapshot.OfType<BlueprintSpellbook>().Where(Eligible).ToArray();
            if (books.Length != KnownBookCount)
            {
                CachedBooks = books;
                KnownBookCount = books.Length;
            }
        }

        private static void TryInjectSelection(BlueprintsCache cache, PsychicAssets f, BlueprintFeatureSelection selection)
        {
            if (selection == null) return;
            var refs = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures");
            int count = refs?.Length ?? 0;
            if (count == 0 || count > 250) return;

            lock (PickerGate)
            {
                if (InjectedBookVersion.TryGetValue(selection.AssetGuid, out int ver) && ver == KnownBookCount)
                    return;
                if (CheckedNonPickers.TryGetValue(selection.AssetGuid, out int nonCount) && nonCount == count)
                    return;
            }

            if (!IsPicker(cache, selection))
            {
                lock (PickerGate) CheckedNonPickers[selection.AssetGuid] = count;
                return;
            }

            try
            {
                Inject(cache, f, selection, CachedBooks);
                lock (PickerGate) InjectedBookVersion[selection.AssetGuid] = KnownBookCount;
            }
            catch (Exception e)
            {
                ModEntry.LogSpellDiagnostic("Spellbook recognition skipped " + selection.AssetGuid + ": " + e.Message);
            }
        }

        // Include base classes, archetype spellbooks (Skirnir, etc.), and standalone
        // prestige casting classes like Sublime Chord, while excluding mythic,
        // alchemist, and psychic/medium spellbooks.
        private static bool Eligible(BlueprintSpellbook book)
            => book != null && !book.IsMythic && !book.IsAlchemist && !PsychicCasting.IsPsychic(book) &&
               book.AssetGuid != BlueprintGuid.Parse(MediumIntegration.BookId) &&
               book.SpellsPerDay != null && book.CharacterClass != null &&
               (!book.CharacterClass.PrestigeClass || book == SublimeChord.Spellbook);

        // A picker is a feature selection whose candidates are ALL spellbook
        // variants. Mixed selections are left alone: they are something else
        // (talent pools, school choices) that happens to reference a book.
        private static bool IsPicker(BlueprintsCache cache, BlueprintFeatureSelection selection)
        {
            lock (PickerGate) if (KnownPickers.Contains(selection.AssetGuid)) return true;
            bool result = CheckPicker(cache, selection);
            if (result) lock (PickerGate) KnownPickers.Add(selection.AssetGuid);
            return result;
        }

        private static bool CheckPicker(BlueprintsCache cache, BlueprintFeatureSelection selection)
        {
            var refs = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures");
            if (refs == null || refs.Length == 0) return false;
            int books = 0;
            bool hasBaseBook = false;
            foreach (var reference in refs)
            {
                if (reference == null) continue;
                var bp = cache.Load(reference.Guid);
                if (bp == null) continue;
                BlueprintSpellbook book;
                if (bp is BlueprintFeatureReplaceSpellbook feature) book = feature.Spellbook;
                else if (bp is BlueprintProgression progression) book = ProgressionBook(progression);
                else if (bp is BlueprintFeature directFeature) book = DirectLevelUpBook(directFeature);
                else return false;
                if (book == null) continue;
                books++;
                if (book.CharacterClass != null && book.CharacterClass.Spellbook == book) hasBaseBook = true;
            }
            // A picker whose candidates are all archetype variants of ONE class
            // (for example ThassilonianSchoolSelection) is a school choice, not a
            // "pick your base casting class" picker. Requiring at least one
            // class-default book separates the two shapes.
            return books > 0 && hasBaseBook;
        }

        private static void Inject(BlueprintsCache cache, PsychicAssets f,
            BlueprintFeatureSelection selection, BlueprintSpellbook[] books)
        {
            var current = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures")
                          ?? Array.Empty<BlueprintFeatureReference>();
            var entries = current.Select(r => r == null ? null : cache.Load(r.Guid))
                .Where(b => b != null).ToArray();

            var featureTemplates = entries.OfType<BlueprintFeatureReplaceSpellbook>()
                .Where(t => t.Spellbook != null).ToArray();
            var progressionTemplates = entries.OfType<BlueprintProgression>()
                .Where(p => ProgressionBook(p) != null).ToArray();
            var directTemplates = entries.OfType<BlueprintFeature>()
                .Where(b => !(b is BlueprintFeatureReplaceSpellbook) && !(b is BlueprintProgression) && DirectLevelUpBook(b) != null)
                .ToArray();
            if (featureTemplates.Length == 0 && progressionTemplates.Length == 0 && directTemplates.Length == 0) return;

            // Ensure BlueprintFeatureReplaceSpellbook pickers (such as PrestigePlus
            // Spellupgrade2) run at ReplaceSpellbook priority before ApplySpellbook,
            // otherwise selecting a spellbook at level 1 has no effect on slots/CL.
            if (featureTemplates.Length > 0 && selection.Group == FeatureGroup.None)
            {
                selection.Group = FeatureGroup.ReplaceSpellbook;
                var groups = selection.Groups ?? Array.Empty<FeatureGroup>();
                if (!groups.Contains(FeatureGroup.ReplaceSpellbook))
                    selection.Groups = groups.Concat(new[] { FeatureGroup.ReplaceSpellbook }).ToArray();
            }

            // Anything the picker already offers is skipped, whichever shape it
            // used. This is what keeps a third-party picker (which already lists
            // many modded books) from receiving duplicates.
            var existing = new HashSet<BlueprintGuid>();
            foreach (var t in featureTemplates) existing.Add(t.Spellbook.AssetGuid);
            foreach (var p in progressionTemplates)
            {
                var b = ProgressionBook(p);
                if (b != null) existing.Add(b.AssetGuid);
            }
            foreach (var d in directTemplates)
            {
                var b = DirectLevelUpBook(d);
                if (b != null) existing.Add(b.AssetGuid);
            }

            var added = new List<BlueprintFeatureBase>();
            // A mixed picker can hold both arcane and divine variants; take one
            // template per side so neither half is silently dropped.
            var arcaneFeature = featureTemplates.FirstOrDefault(t => t.Spellbook.IsArcane);
            var divineFeature = featureTemplates.FirstOrDefault(t => !t.Spellbook.IsArcane);
            var arcaneProgression = progressionTemplates.FirstOrDefault(p => ProgressionBook(p).IsArcane);
            var divineProgression = progressionTemplates.FirstOrDefault(p => !ProgressionBook(p).IsArcane);
            var arcaneDirect = directTemplates.FirstOrDefault(d => DirectLevelUpBook(d).IsArcane);
            var divineDirect = directTemplates.FirstOrDefault(d => !DirectLevelUpBook(d).IsArcane);

            if (arcaneFeature != null)
                AddGroup(books, true, existing, added, b => FeatureVariant(cache, f, arcaneFeature, b));
            if (divineFeature != null)
                AddGroup(books, false, existing, added, b => FeatureVariant(cache, f, divineFeature, b));
            if (arcaneProgression != null)
                AddGroup(books, true, existing, added, b => ProgressionVariant(cache, f, arcaneProgression, b));
            if (divineProgression != null)
                AddGroup(books, false, existing, added, b => ProgressionVariant(cache, f, divineProgression, b));
            if (arcaneDirect != null)
                AddGroup(books, true, existing, added, b => DirectFeatureVariant(cache, f, arcaneDirect, b));
            if (divineDirect != null)
                AddGroup(books, false, existing, added, b => DirectFeatureVariant(cache, f, divineDirect, b));

            Append(selection, added);
        }

        private static void AddGroup(BlueprintSpellbook[] books, bool arcane, HashSet<BlueprintGuid> existing,
            List<BlueprintFeatureBase> added, Func<BlueprintSpellbook, BlueprintFeatureBase> make)
        {
            foreach (var book in books)
            {
                if (book.IsArcane != arcane || existing.Contains(book.AssetGuid)) continue;
                var variant = make(book);
                if (variant == null) continue;
                existing.Add(book.AssetGuid);
                added.Add(variant);
            }
        }

        private static void Append(BlueprintFeatureSelection selection, List<BlueprintFeatureBase> added)
        {
            if (added.Count == 0) return;
            var refs = added.Select(r => Ref<BlueprintFeatureReference>(r)).ToArray();
            var current = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures")
                          ?? Array.Empty<BlueprintFeatureReference>();
            Set(selection, "m_AllFeatures", current.Concat(refs).ToArray());
            var own = (BlueprintFeatureReference[])Get(selection, "m_Features")
                      ?? Array.Empty<BlueprintFeatureReference>();
            Set(selection, "m_Features", own.Concat(refs).ToArray());
        }

        private static BlueprintFeature FeatureVariant(BlueprintsCache cache, PsychicAssets f,
            BlueprintFeatureReplaceSpellbook template, BlueprintSpellbook book)
        {
            var token = "SpellbookRecognition/" + template.AssetGuid + "/" + book.AssetGuid;
            if (f.Assets.TryGetValue(token, out var staged)) return (BlueprintFeature)staged;
            var existing = cache.Load(Id(token)) as BlueprintFeature;
            if (existing != null) return existing;

            var cls = book.CharacterClass;
            string label = cls.Name ?? book.name;
            if (cls.Spellbook != book && !string.IsNullOrEmpty(book.Name))
                label = label + "（" + book.Name + "）";
            var clone = f.Clone(template, token, "施法职业：" + label,
                "以该职业的法术书作为本进阶职业的施法基础；不改变该职业本身的法术位或可知法术。");
            // m_Spellbook lives on the feature, not in ComponentsArray, so the
            // memberwise clone still aliases the template's reference. Replace it
            // with a fresh reference instead of mutating the shared one.
            Set(clone, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));
            int minSpellLevel = ExtractMinSpellLevel(clone.ComponentsArray);
            var kept = (clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => !(c is Prerequisite))
                .ToArray();
            foreach (var component in kept)
                Retarget(component, template.Spellbook, book);
            clone.ComponentsArray = kept
                .Concat(new BlueprintComponent[] {
                    new SpellbookOwnedPrerequisite { name = "SpellbookOwned",
                        Book = Ref<BlueprintSpellbookReference>(book),
                        MinSpellLevel = minSpellLevel }
                }).ToArray();
            clone.OnEnable();
            cache.AddCachedBlueprint(clone.AssetGuid, clone);
            return clone;
        }

        private static int ExtractMinSpellLevel(BlueprintComponent[] components)
        {
            int maxReq = 0;
            if (components == null) return 0;
            foreach (var c in components)
            {
                if (c is PrerequisiteClassSpellLevel pcs && pcs.RequiredSpellLevel > maxReq)
                    maxReq = pcs.RequiredSpellLevel;
            }
            return maxReq;
        }

        private static BlueprintProgression ProgressionVariant(BlueprintsCache cache, PsychicAssets f,
            BlueprintProgression template, BlueprintSpellbook book)
        {
            var token = "SpellbookRecognitionP/" + template.AssetGuid + "/" + book.AssetGuid;
            if (f.Assets.TryGetValue(token, out var staged)) return (BlueprintProgression)staged;
            var existing = cache.Load(Id(token)) as BlueprintProgression;
            if (existing != null) return existing;

            var templateBook = ProgressionBook(template);
            var cls = book.CharacterClass;
            string label = cls.Name ?? book.name;
            if (cls.Spellbook != book && !string.IsNullOrEmpty(book.Name))
                label = label + "（" + book.Name + "）";
            var clone = f.Clone(template, token, "施法职业：" + label,
                "以该职业的法术书作为本进阶职业的施法基础；不改变该职业本身的法术位或可知法术。");
            // LevelEntries is not part of ComponentsArray, so the memberwise
            // clone still shares the template's entry objects. Copy them before
            // editing; blueprint references inside stay shared on purpose.
            // Keep template's m_Classes (the prestige class) so progression level
            // tracks prestige class levels rather than the base class.
            clone.LevelEntries = CopyData(template.LevelEntries);
            var classes = (BlueprintProgression.ClassWithLevel[])Get(clone, "m_Classes")
                          ?? Array.Empty<BlueprintProgression.ClassWithLevel>();
            Set(clone, "m_Classes", CopyData(classes));
            int minSpellLevel = ExtractMinSpellLevel(clone.ComponentsArray);
            var kept = (clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => !(c is Prerequisite))
                .ToArray();
            foreach (var component in kept)
                Retarget(component, templateBook, book);
            clone.ComponentsArray = kept
                .Concat(new BlueprintComponent[] {
                    new SpellbookOwnedPrerequisite { name = "SpellbookOwned",
                        Book = Ref<BlueprintSpellbookReference>(book),
                        MinSpellLevel = minSpellLevel }
                }).ToArray();
            // The same level-up feature is referenced from every LevelEntry, so
            // clone it once per source GUID and reuse that instance. Each clone
            // must also be published: a progression referencing an unpublished
            // feature would resolve it to null at runtime.
            var levelUps = new Dictionary<BlueprintGuid, BlueprintFeatureBase>();
            foreach (var entry in clone.LevelEntries ?? Array.Empty<LevelEntry>())
            {
                var features = entry.Features;
                for (int i = 0; i < features.Count; i++)
                {
                    var feature = features[i];
                    if (feature == null) continue;
                    if (!(feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                        .OfType<AddSpellbookLevel>().Any()) continue;
                    if (!levelUps.TryGetValue(feature.AssetGuid, out var fclone))
                    {
                        var ftoken = token + "/" + feature.AssetGuid;
                        fclone = f.Clone(feature, ftoken, feature.name, "");
                        foreach (var level in (fclone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                            .OfType<AddSpellbookLevel>())
                            Set(level, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));
                        foreach (var component in fclone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                            Retarget(component, templateBook, book);
                        fclone.OnEnable();
                        cache.AddCachedBlueprint(fclone.AssetGuid, fclone);
                        levelUps[feature.AssetGuid] = fclone;
                    }
                    features[i] = fclone;
                }
                entry.SetFeatures(features);
            }
            clone.OnEnable();
            cache.AddCachedBlueprint(clone.AssetGuid, clone);
            return clone;
        }

        // Third-party prerequisites come in three shapes: blueprint references,
        // class references, and plain GUID strings (Prestige-Plus stores the
        // book as a string). Retarget all three so a cloned variant never asks
        // for the template's book instead of the target one.
        private static void Retarget(object component, BlueprintSpellbook from, BlueprintSpellbook to)
        {
            if (component == null || from == null || to == null) return;
            var fromClass = from.CharacterClass;
            var toClass = to.CharacterClass;
            for (var type = component.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                     BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic) continue;
                    object value;
                    try { value = field.GetValue(component); } catch { continue; }
                    if (value == null) continue;
                    if (value is BlueprintSpellbookReference bookReference)
                    {
                        if (bookReference.Guid == from.AssetGuid)
                            field.SetValue(component, Ref<BlueprintSpellbookReference>(to));
                        continue;
                    }
                    if (value is BlueprintCharacterClassReference classReference)
                    {
                        if (fromClass != null && toClass != null && classReference.Guid == fromClass.AssetGuid)
                            field.SetValue(component, Ref<BlueprintCharacterClassReference>(toClass));
                        continue;
                    }
                    if (value is string text && SameGuid(text, from.AssetGuid))
                        field.SetValue(component, to.AssetGuid.ToString());
                }
        }

        private static bool SameGuid(string text, BlueprintGuid guid)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var cleaned = text.Replace("{", "").Replace("}", "").Replace("-", "").Trim();
            return string.Equals(cleaned, guid.ToString().Replace("-", ""), StringComparison.OrdinalIgnoreCase);
        }

        private static BlueprintSpellbook DirectLevelUpBook(BlueprintFeature feature)
        {
            if (feature == null) return null;
            foreach (var level in (feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .OfType<AddSpellbookLevel>())
            {
                var book = (Get(level, "m_Spellbook") as BlueprintSpellbookReference)?.Get();
                if (book != null) return book;
            }
            return null;
        }

        private static BlueprintFeature DirectFeatureVariant(BlueprintsCache cache, PsychicAssets f,
            BlueprintFeature template, BlueprintSpellbook book)
        {
            var token = "SpellbookRecognitionD/" + template.AssetGuid + "/" + book.AssetGuid;
            if (f.Assets.TryGetValue(token, out var staged)) return (BlueprintFeature)staged;
            var existing = cache.Load(Id(token)) as BlueprintFeature;
            if (existing != null) return existing;

            var templateBook = DirectLevelUpBook(template);
            var cls = book.CharacterClass;
            string label = cls.Name ?? book.name;
            if (cls.Spellbook != book && !string.IsNullOrEmpty(book.Name))
                label = label + "（" + book.Name + "）";
            var clone = f.Clone(template, token, "施法职业：" + label,
                "以该职业的法术书作为本进阶职业的施法基础；不改变该职业本身的法术位或可知法术。");
            int minSpellLevel = ExtractMinSpellLevel(clone.ComponentsArray);
            var kept = (clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => !(c is Prerequisite))
                .ToArray();
            foreach (var level in kept.OfType<AddSpellbookLevel>())
                Set(level, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));
            foreach (var component in kept)
                Retarget(component, templateBook, book);
            clone.ComponentsArray = kept
                .Concat(new BlueprintComponent[] {
                    new SpellbookOwnedPrerequisite { name = "SpellbookOwned",
                        Book = Ref<BlueprintSpellbookReference>(book),
                        MinSpellLevel = minSpellLevel }
                }).ToArray();
            clone.OnEnable();
            cache.AddCachedBlueprint(clone.AssetGuid, clone);
            return clone;
        }

        private static BlueprintSpellbook ProgressionBook(BlueprintProgression progression)
        {
            foreach (var entry in progression.LevelEntries ?? Array.Empty<LevelEntry>())
                foreach (var feature in entry.Features)
                {
                    if (feature == null) continue;
                    foreach (var level in (feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                        .OfType<AddSpellbookLevel>())
                    {
                        var book = (Get(level, "m_Spellbook") as BlueprintSpellbookReference)?.Get();
                        if (book != null) return book;
                    }
                }
            return null;
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint))]
        private static class PublishedPatch
        {
            private static void Postfix(SimpleBlueprint __1)
            {
                if (__1 is BlueprintSpellbook b && Eligible(b))
                    KnownBookCount = -1;
            }
        }

        [HarmonyPatch(typeof(BlueprintFeatureSelection), "get_AllFeatures")]
        private static class SelectionOpenedPatch
        {
            private static void Prefix(BlueprintFeatureSelection __instance)
            {
                if (Cache == null || Reconciling || __instance == null || !SpellbookRecognitionSettings.Enabled) return;
                var refs = (BlueprintFeatureReference[])Get(__instance, "m_AllFeatures");
                int count = refs?.Length ?? 0;
                if (count == 0 || count > 250) return;

                lock (PickerGate)
                {
                    if (InjectedBookVersion.TryGetValue(__instance.AssetGuid, out int ver) && ver == KnownBookCount)
                        return;
                    if (CheckedNonPickers.TryGetValue(__instance.AssetGuid, out int nonCount) && nonCount == count)
                        return;
                }

                Reconciling = true;
                try
                {
                    CleanupDragonDiscipleDuplicate(Cache);
                    if (KnownBookCount <= 0) RefreshBooks(Cache);
                    if (CachedBooks.Length > 0)
                        TryInjectSelection(Cache, new PsychicAssets(Cache), __instance);
                }
                catch (Exception e) { ModEntry.LogSpellDiagnostic("Spellbook recognition on-open skipped: " + e.Message); }
                finally { Reconciling = false; }
            }
        }
    }

    // Persisted next to the mod assembly. ModMenu's toggle is only the UI: the
    // registration-time decision must be readable before the settings page has
    // ever been built, so a tiny side file is the reliable store.
    internal static class SpellbookRecognitionSettings
    {
        private const string FileName = "spellbook-recognition.json";
        private static readonly object Gate = new object();
        private static bool? enabled;

        internal static bool Enabled
        {
            get { lock (Gate) return enabled ?? (bool)(enabled = Load()); }
        }

        internal static void Set(bool value)
        {
            lock (Gate) { enabled = value; Save(value); }
        }

        private static string FilePath
        {
            get
            {
                try
                {
                    var dir = Path.GetDirectoryName(typeof(SpellbookRecognitionSettings).Assembly.Location);
                    if (!string.IsNullOrEmpty(dir)) return Path.Combine(dir, FileName);
                }
                catch { }
                return null;
            }
        }

        private static bool Load()
        {
            try
            {
                var path = FilePath;
                if (path == null || !File.Exists(path)) return true;   // on by default
                var text = File.ReadAllText(path);
                var match = System.Text.RegularExpressions.Regex.Match(text, "\"enabled\"\\s*:\\s*(true|false)");
                if (match.Success) return match.Groups[1].Value == "true";
            }
            catch { }
            return true;
        }

        private static void Save(bool value)
        {
            try
            {
                var path = FilePath;
                if (path != null) File.WriteAllText(path,
                    "{\n  \"enabled\": " + (value ? "true" : "false") + "\n}\n");
            }
            catch { }
        }
    }

    [TypeId("d1c0000000000000000000000000000e")]
    public sealed class SpellbookOwnedPrerequisite : Prerequisite
    {
        public BlueprintSpellbookReference Book;
        public int MinSpellLevel;

        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            var book = Book?.Get();
            if (book == null || unit == null) return false;

            Spellbook instance = unit.GetSpellbook(book);
            if (instance == null)
            {
                var cls = book.CharacterClass;
                if (cls != null && cls.Spellbook == book &&
                    unit.Progression.Classes.Any(c => c.CharacterClass == cls))
                {
                    instance = unit.DemandSpellbook(cls);
                }
            }
            if (instance == null) return false;
            return MinSpellLevel <= 0 || instance.MaxSpellLevel >= MinSpellLevel;
        }

        protected override string GetUITextInternal(UnitDescriptor unit)
            => MinSpellLevel > 0 ? $"必须已拥有该施法职业且可施放 {MinSpellLevel} 环法术" : "必须已拥有该施法职业";
    }
}
