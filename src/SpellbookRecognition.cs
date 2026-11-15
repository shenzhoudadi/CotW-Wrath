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
        private static bool Pending;
        private static int Scans;
        private static readonly HashSet<BlueprintGuid> KnownPickers = new HashSet<BlueprintGuid>();
        private static readonly object PickerGate = new object();

        internal static void Register(BlueprintsCache cache)
        {
            Cache = cache;
            // The toggle is read once per session: injected variants live in the
            // blueprint graph, so changing it takes effect after a restart.
            if (!SpellbookRecognitionSettings.Enabled) return;
            Reconcile(cache);
        }

        // Late providers are handled through the same bounded rescan as Sublime
        // Chord: no timer, no per-frame polling, no forced load of the whole TOC.
        internal static void Reconcile(BlueprintsCache cache)
        {
            if (cache == null || Reconciling || !SpellbookRecognitionSettings.Enabled) return;
            // Bounded: the first pass runs at cache init, later passes only when
            // a provider published something. Ten is far more than needed and
            // keeps a pathological UI loop from turning this into a stall.
            if (Interlocked.Increment(ref Scans) > 10) return;
            Reconciling = true;
            try
            {
                var f = new PsychicAssets(cache);
                var snapshot = SublimeChord.Snapshot(cache);
                var books = snapshot.OfType<BlueprintSpellbook>().Where(Eligible).ToArray();
                if (books.Length == 0) return;
                foreach (var selection in snapshot.OfType<BlueprintFeatureSelection>())
                {
                    var refs = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures");
                    if (refs == null || refs.Length == 0 || refs.Length > 200) continue;
                    if (!IsPicker(cache, selection)) continue;
                    try { Inject(cache, f, selection, books); }
                    catch (Exception e) { ModEntry.LogSpellDiagnostic("Spellbook recognition skipped " + selection.AssetGuid + ": " + e.Message); }
                }
            }
            catch (Exception e) { ModEntry.LogSpellDiagnostic("Spellbook recognition skipped: " + e.Message); }
            finally { Reconciling = false; }
        }

        // Only base casting classes: a prestige class's own book (Sublime Chord,
        // Psychic Detective) is not something another prestige class builds on.
        // Restrict to a class's default book so archetype variants do not
        // multiply the injected set; variants are already offered natively or by
        // the provider that created them.
        private static bool Eligible(BlueprintSpellbook book)
            => book != null && !book.IsMythic && book.SpellsPerDay != null &&
               book.CharacterClass != null && !book.CharacterClass.PrestigeClass &&
               book.CharacterClass.Spellbook == book;

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
                var bp = reference == null ? null : cache.Load(reference.Guid);
                BlueprintSpellbook book;
                if (bp is BlueprintFeatureReplaceSpellbook feature) book = feature.Spellbook;
                else if (bp is BlueprintProgression progression) book = ProgressionBook(progression);
                else return false;
                if (book == null) return false;
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
            if (featureTemplates.Length == 0 && progressionTemplates.Length == 0) return;

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

            var added = new List<BlueprintFeatureBase>();
            // A mixed picker can hold both arcane and divine variants; take one
            // template per side so neither half is silently dropped.
            var arcaneFeature = featureTemplates.FirstOrDefault(t => t.Spellbook.IsArcane);
            var divineFeature = featureTemplates.FirstOrDefault(t => !t.Spellbook.IsArcane);
            var arcaneProgression = progressionTemplates.FirstOrDefault(p => ProgressionBook(p).IsArcane);
            var divineProgression = progressionTemplates.FirstOrDefault(p => !ProgressionBook(p).IsArcane);

            if (arcaneFeature != null)
                AddGroup(books, true, existing, added, b => FeatureVariant(cache, f, arcaneFeature, b));
            if (divineFeature != null)
                AddGroup(books, false, existing, added, b => FeatureVariant(cache, f, divineFeature, b));
            if (arcaneProgression != null)
                AddGroup(books, true, existing, added, b => ProgressionVariant(cache, f, arcaneProgression, b));
            if (divineProgression != null)
                AddGroup(books, false, existing, added, b => ProgressionVariant(cache, f, divineProgression, b));

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
            var clone = f.Clone(template, token, "施法职业：" + (cls.Name ?? book.name),
                "以该职业的法术书作为本进阶职业的施法基础；不改变该职业本身的法术位或可知法术。");
            // m_Spellbook lives on the feature, not in ComponentsArray, so the
            // memberwise clone still aliases the template's reference. Replace it
            // with a fresh reference instead of mutating the shared one.
            Set(clone, "m_Spellbook", Ref<BlueprintSpellbookReference>(book));
            foreach (var component in clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                Retarget(component, template.Spellbook, book);
            // Every archetype variant of one class shares that class, so a class
            // gate alone would list all of them at once. Require actual ownership
            // of this exact spellbook.
            clone.ComponentsArray = clone.ComponentsArray
                .Concat(new BlueprintComponent[] {
                    new SpellbookOwnedPrerequisite { name = "SpellbookOwned",
                        Book = Ref<BlueprintSpellbookReference>(book) }
                }).ToArray();
            clone.OnEnable();
            cache.AddCachedBlueprint(clone.AssetGuid, clone);
            return clone;
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
            var clone = f.Clone(template, token, "施法职业：" + (cls.Name ?? book.name),
                "以该职业的法术书作为本进阶职业的施法基础；不改变该职业本身的法术位或可知法术。");
            // LevelEntries is not part of ComponentsArray, so the memberwise
            // clone still shares the template's entry objects. Copy them before
            // editing; blueprint references inside stay shared on purpose.
            clone.LevelEntries = CopyData(template.LevelEntries);
            var classes = (BlueprintProgression.ClassWithLevel[])Get(clone, "m_Classes")
                          ?? Array.Empty<BlueprintProgression.ClassWithLevel>();
            Set(clone, "m_Classes", new[] {
                new BlueprintProgression.ClassWithLevel {
                    m_Class = Ref<BlueprintCharacterClassReference>(cls),
                    AdditionalLevel = classes.Length > 0 ? classes[0].AdditionalLevel : 0 }
            });
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
                if (__1 is BlueprintSpellbook || __1 is BlueprintFeatureSelection) Pending = true;
            }
        }

        [HarmonyPatch(typeof(BlueprintFeatureSelection), "get_AllFeatures")]
        private static class SelectionOpenedPatch
        {
            private static void Postfix()
            {
                if (!Pending || Cache == null) return;
                Pending = false;
                Reconcile(Cache);
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

        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
        {
            var book = Book?.Get();
            if (book == null) return false;
            foreach (var owned in unit.Spellbooks)
                if (owned?.Blueprint == book) return true;
            // A level-up preview unit can lag a level behind on spellbook
            // instances; the class's default book is unambiguous when the class
            // is present. Archetype variants deliberately skip this fallback.
            var cls = book.CharacterClass;
            return cls != null && cls.Spellbook == book &&
                   unit.Progression.Classes.Any(c => c.CharacterClass == cls);
        }

        protected override string GetUITextInternal(UnitDescriptor unit) => "必须已拥有该施法职业";
    }
}
