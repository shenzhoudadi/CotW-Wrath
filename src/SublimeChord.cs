// Sublime Chord (超凡之弦) — original Wrath content, not a Kingmaker port.
// Chinese in-game text is intentional for this class (user decision 2026-11-14).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SublimeChord
    {
        internal const string ModuleName = "Sublime Chord";

        // Native anchors. Only resource/class identities are pinned; every
        // performance ability is discovered from the live blueprint cache.
        internal const string BardClassId = "772c83a25e2268e448e841dcd548235f";
        internal const string WizardClassId = "ba34257984f4c41408ce1dc2004e342e";
        internal const string BardSpellListId = "25a5013493bdcf74bb2424532214d0c8";
        internal const string WizardSpellListId = "ba0401fdeb4062f40a7aa95b6f07fe89";
        internal const string BardicPerformanceResourceId = "e190ba276831b5c4fa28737e5e49e6a6";
        internal const string RagingSongResourceId = "4a2302c4ec2cfb042bba67d825babfec";
        internal const string BardicPerformanceResourceFactId = "b92bfc201c6a79e49afd0b5cfbfc269f";
        internal const string RagingSongFeatureId = "334cb75aa673d1d4bb279761c2ef5cf1";
        internal const string BardicKnowledgeId = "65cff8410a336654486c98fd3bacd8c5";
        internal const string TimeStopAbilityId = "70e55e5b0ac84af78c084eb6cf47ae06";
        internal const string TimeStopEffectBuffId = "8fff364a212d4b4894b5e222f3696cdd";
        internal const string SkaldClassId = "6afa347d804838b48bda16acb0573dc0";
        internal const string WallOfFireFxId = "098a29fefbbc4564281afa5a6887cd2c";
        internal const string BardMovePerformanceId = "36931765983e96d4bb07ce7844cd897e";
        internal const string BardSwiftPerformanceId = "fd4ec50bc895a614194df6b9232004b9";
        internal const string SkaldMovePerformanceId = "f24dc98de96671d4288cba09584e249b";
        internal const string SkaldSwiftPerformanceId = "0c12540bf7e45f04faa84730136cb726";
        internal const string RootId = "2d77316c72b9ed44f888ceefc2a131f6";

        internal static string T(string token) => "SublimeChord" + token;
        private static BlueprintCharacterClass Class;
        private static BlueprintSpellbook Book;
        private static BlueprintFeatureSelection BookSelection;
        private static BlueprintsCache Cache;
        private static bool Reconciling;
        private static int DiscoveriesLeft = 12;
        private static int Probes;

        internal static BlueprintsCache CurrentCache => Cache;

        // Registration runs in BlueprintsCache.Init's postfix, when most native
        // performance blueprints are still just TOC entries. Discovery is
        // therefore repeated lazily from real gameplay events, never from a
        // timer or a per-frame tick. The budget caps the total cost.
        internal static void LazyDiscover()
        {
            if (++Probes != 1 && Probes % 64 != 0) return;
            if (DiscoveriesLeft <= 0 || Cache == null) return;
            DiscoveriesLeft--;
            Reconcile(Cache);
        }

        private static T Native<T>(BlueprintsCache cache, string id) where T : SimpleBlueprint
            => cache.Load(BlueprintGuid.Parse(id)) as T ?? throw new InvalidOperationException("Sublime Chord native dependency missing: " + id);

        // Per-day / spells-known tables from the 10-level chart supplied by the user.
        private static readonly int[][] PerDay = {
            new[]{2,1,0,0,0,0}, new[]{2,2,0,0,0,0}, new[]{3,2,1,0,0,0}, new[]{3,3,2,0,0,0},
            new[]{3,3,2,1,0,0}, new[]{4,3,3,2,0,0}, new[]{4,4,3,2,1,0}, new[]{4,4,3,3,2,0},
            new[]{4,4,4,3,2,1}, new[]{4,4,4,3,3,2}
        };
        private static readonly int[][] Known = {
            new[]{3,1,0,0,0,0}, new[]{4,2,0,0,0,0}, new[]{4,2,1,0,0,0}, new[]{4,3,2,0,0,0},
            new[]{4,3,2,1,0,0}, new[]{4,4,3,2,0,0}, new[]{4,4,3,2,1,0}, new[]{4,4,4,3,2,0},
            new[]{4,4,4,3,2,1}, new[]{4,4,4,4,3,2}
        };

        private static BlueprintSpellsTable Table(PsychicAssets f, string token, int[][] rows)
        {
            var levels = new SpellsLevelEntry[rows.Length + 1];
            levels[0] = new SpellsLevelEntry { Count = new int[10] };
            for (int i = 0; i < rows.Length; i++)
            {
                var count = new int[10];
                for (int ring = 4; ring <= 9; ring++) count[ring] = rows[i][ring - 4];
                levels[i + 1] = new SpellsLevelEntry { Count = count };
            }
            return f.Add(new BlueprintSpellsTable { Levels = levels }, T(token), "", "");
        }

        private static BlueprintAbilityReference GuidRef(BlueprintGuid guid)
        {
            var reference = new BlueprintAbilityReference();
            AccessTools.Field(typeof(BlueprintReferenceBase), "deserializedGuid").SetValue(reference, guid);
            return reference;
        }

        // Union of the Bard and Wizard lists. A spell is placed at the LOWEST
        // level >= 4 that either list offers; spells available only below 4th
        // level are not part of the Sublime Chord list (user decision).
        private static BlueprintSpellList MergedList(PsychicAssets f, BlueprintSpellList bard, BlueprintSpellList wizard)
        {
            var levels = new Dictionary<BlueprintGuid, int>();
            foreach (var list in new[] { bard, wizard })
                foreach (var row in list.SpellsByLevel ?? Array.Empty<SpellLevelList>())
                {
                    if (row.SpellLevel < 4 || row.SpellLevel > 9) continue;
                    var spells = Get(row, "m_Spells") as IList;
                    if (spells == null) continue;
                    foreach (var item in spells)
                    {
                        var reference = item as BlueprintAbilityReference;
                        if (reference == null) continue;
                        if (!levels.TryGetValue(reference.Guid, out var current) || row.SpellLevel < current)
                            levels[reference.Guid] = row.SpellLevel;
                    }
                }
            var rows = Enumerable.Range(0, 11).Select(ring =>
            {
                var entry = new SpellLevelList(ring);
                // SpellLevelList.m_Spells is a List<BlueprintAbilityReference>,
                // NOT an array. Reflection SetValue rejects an array here and
                // aborts the whole module registration at runtime.
                Set(entry, "m_Spells", levels.Where(pair => pair.Value == ring)
                    .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
                    .Select(pair => GuidRef(pair.Key)).ToList());
                return entry;
            }).ToArray();
            return f.Add(new BlueprintSpellList { SpellsByLevel = rows }, T("SpellList"), "", "");
        }

        internal static void Register(BlueprintsCache cache)
        {
            Cache = cache;
            var f = new PsychicAssets(cache);
            var bard = Native<BlueprintCharacterClass>(cache, BardClassId);
            var wizard = Native<BlueprintCharacterClass>(cache, WizardClassId);
            var root = Native<BlueprintRoot>(cache, RootId);
            var bardList = Native<BlueprintSpellList>(cache, BardSpellListId);
            var wizardList = Native<BlueprintSpellList>(cache, WizardSpellListId);
            if (root.Progression == null || bardList.SpellsByLevel == null || wizardList.SpellsByLevel == null)
                throw new InvalidOperationException("Sublime Chord native progression/spell-list layout differs.");

            var perDay = Table(f, "SpellsPerDay", PerDay);
            var known = Table(f, "SpellsKnown", Known);
            var list = MergedList(f, bardList, wizardList);
            Book = f.Add(new BlueprintSpellbook {
                Name = Text(T("Spellbook"), "name", "超凡之弦法术"), CastingAttribute = StatType.Charisma,
                Spontaneous = true, SpellsPerLevel = 0, AllSpellsKnown = false, CanCopyScrolls = false,
                CantripsType = CantripsType.Cantrips, IsArcane = true, CasterLevelModifier = 0,
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, T("SpellbookAsset"), "", "");
            Set(Book, "m_SpellsPerDay", Ref<BlueprintSpellsTableReference>(perDay));
            Set(Book, "m_SpellsKnown", Ref<BlueprintSpellsTableReference>(known));
            Set(Book, "m_SpellList", Ref<BlueprintSpellListReference>(list));

            Class = f.Add(new BlueprintCharacterClass {
                LocalizedName = Text(T("Class"), "name", "超凡之弦"),
                LocalizedDescription = Text(T("Class"), "description", "所有的超凡之弦都需要具备吟游诗人的基础，因为吟唱是掌握「第一首歌」力量的第一步。然而，音乐仅仅是理解无限未知的一个工具；一个超凡之弦同时也必须学习数学和星象学。作为放弃继续学习吟唱的回报，超凡之弦能掌握很多超出一般吟游诗人能使用的范围的法术。"),
                LocalizedDescriptionShort = Text(T("Class"), "short", "以歌声触及更高环级奥术的进阶职业。"),
                SkillPoints = 4, PrestigeClass = true, HideInUI = false,
                ClassSkills = new[] { StatType.SkillKnowledgeArcana, StatType.SkillKnowledgeWorld, StatType.SkillPerception, StatType.SkillPersuasion },
                IsArcaneCaster = true, IsDivineCaster = false,
                RecommendedAttributes = new[] { StatType.Charisma, StatType.Intelligence },
                NotRecommendedAttributes = Array.Empty<StatType>(),
                PrimaryColor = bard.PrimaryColor, SecondaryColor = bard.SecondaryColor, StartingGold = bard.StartingGold,
                MaleEquipmentEntities = bard.MaleEquipmentEntities, FemaleEquipmentEntities = bard.FemaleEquipmentEntities,
                ComponentsArray = new BlueprintComponent[] {
                    new PrerequisiteStatValue { Stat = StatType.SkillKnowledgeArcana, Value = 10 },
                    new PrerequisiteStatValue { Stat = StatType.SkillPersuasion, Value = 10 },
                    new PrerequisiteStatValue { Stat = StatType.SkillPerception, Value = 6 },
                    new PrerequisiteCasterTypeSpellLevel { IsArcane = true, RequiredSpellLevel = 3 },
                    ListPrerequisite(cache)
                }
            }, T("ClassAsset"), "", "");
            Set(Class, "HitDie", Get(wizard, "HitDie"));
            Set(Class, "m_BaseAttackBonus", Get(bard, "m_BaseAttackBonus"));   // BABMedium
            Set(Class, "m_FortitudeSave", Get(bard, "m_FortitudeSave"));       // SavesLow
            Set(Class, "m_ReflexSave", Get(wizard, "m_ReflexSave"));           // SavesLow
            Set(Class, "m_WillSave", Get(bard, "m_WillSave"));                 // SavesHigh
            foreach (var field in new[] { "m_StartingItems", "m_EquipmentEntities", "m_Difficulty" })
                Set(Class, field, Get(bard, field));
            Set(Class, "m_Archetypes", Array.Empty<BlueprintArchetypeReference>());
            Set(Class, "m_Spellbook", Ref<BlueprintSpellbookReference>(Book));
            Set(Book, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(Class));

            var casting = f.Add(new BlueprintFeature { Ranks = 1, IsClassFeature = true }, T("Casting"),
                "超凡之弦法术", "从 1 级起，超凡之弦获得一份独立的自发奥术法术书，使用魅力施法，只能施展 4 至 9 环法术。");
            BookSelection = f.Add(new BlueprintFeatureSelection { Ranks = 1 }, T("BookSelection"), "", "");
            Set(BookSelection, "m_AllFeatures", Array.Empty<BlueprintFeatureReference>());
            Set(BookSelection, "m_Features", Array.Empty<BlueprintFeatureReference>());
            Set(BookSelection, "m_DisplayName", Text(T("BookSelection"), "name", "选择奥术施法职业"));
            Set(BookSelection, "m_Description", Text(T("BookSelection"), "description", "选择进阶本职业前已有的奥术施法职业。超凡之弦施放自身法术与该职业法术时，两者的施法者等级叠加计算。"));

            var features = SublimeChordPerformances.Build(f, cache, Class, Book);
            var level1 = new BlueprintFeatureBase[] { casting, BookSelection, features.Knowledge, features.Performance };
            var entries = Enumerable.Range(1, 10).Select(level =>
            {
                var entry = new LevelEntry { Level = level };
                var grant = new List<BlueprintFeatureBase>();
                if (level == 1) grant.AddRange(level1);
                if (level == 2) grant.Add(features.ArcaneSong);
                if (level == 6) grant.Add(features.EternalSong);
                if (level == 10) grant.Add(features.InfiniteFire);
                entry.SetFeatures(grant.ToArray());
                return entry;
            }).ToArray();
            var progression = f.Add(new BlueprintProgression { Ranks = 1, LevelEntries = entries, UIGroups = Array.Empty<UIGroup>() },
                T("Progression"), "", "");
            Set(progression, "m_Classes", new[] { new BlueprintProgression.ClassWithLevel { m_Class = Ref<BlueprintCharacterClassReference>(Class) } });
            Set(progression, "m_UIDeterminatorsGroup", new BlueprintFeatureBaseReference[] {
                Ref<BlueprintFeatureBaseReference>(casting), Ref<BlueprintFeatureBaseReference>(BookSelection) });
            Set(Class, "m_Progression", Ref<BlueprintProgressionReference>(progression));

            var assets = f.Assets.Values.ToArray();
            f.Enable();
            var existing = assets.Select(bp => cache.Load(bp.AssetGuid)).ToArray();
            for (int i = 0; i < assets.Length; i++)
                if (existing[i] != null && existing[i].GetType() != assets[i].GetType())
                    throw new InvalidOperationException("Sublime Chord GUID belongs to unexpected content: " + assets[i].AssetGuid);
            if (existing.Any(bp => bp != null) && existing.Any(bp => bp == null))
                throw new InvalidOperationException("Sublime Chord registration is only partially present.");

            var previousClasses = (BlueprintCharacterClassReference[])Get(root.Progression, "m_CharacterClasses") ?? Array.Empty<BlueprintCharacterClassReference>();
            var added = new List<SimpleBlueprint>();
            try
            {
                for (int i = 0; i < assets.Length; i++)
                    if (existing[i] == null) { cache.AddCachedBlueprint(assets[i].AssetGuid, assets[i]); added.Add(assets[i]); }
                if (!previousClasses.Any(r => r?.Guid == Class.AssetGuid))
                    Set(root.Progression, "m_CharacterClasses", previousClasses.Concat(new[] { Ref<BlueprintCharacterClassReference>(Class) }).ToArray());
            }
            catch
            {
                Set(root.Progression, "m_CharacterClasses", previousClasses);
                foreach (var bp in added) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
            Reconcile(cache);
        }

        private static PrerequisiteFeaturesFromList ListPrerequisite(BlueprintsCache cache)
        {
            var result = new PrerequisiteFeaturesFromList { Amount = 1 };
            var bardFact = Native<BlueprintFeature>(cache, BardicPerformanceResourceFactId);
            var skaldFact = Native<BlueprintFeature>(cache, RagingSongFeatureId);
            Set(result, "m_Features", new[] { Ref<BlueprintFeatureReference>(bardFact), Ref<BlueprintFeatureReference>(skaldFact) });
            return result;
        }

        // ---------- automatic arcane spellbook catalogue (SC-only pilot) ----------
        private static readonly Dictionary<BlueprintGuid, BlueprintFeature> PublishedBooks = new Dictionary<BlueprintGuid, BlueprintFeature>();

        private static BlueprintFeature BookFeature(BlueprintsCache cache, PsychicAssets f, BlueprintSpellbook book)
        {
            var token = "SublimeChordBook/" + book.AssetGuid;
            if (f.Assets.TryGetValue(token, out var staged)) return (BlueprintFeature)staged;
            var existing = cache.Load(Id(token)) as BlueprintFeature;
            if (existing != null) return existing;
            // Choosing a spellbook ONLY links caster levels. Adding a
            // spellbook level here would also grant extra slots/known spells,
            // which the class explicitly does not do.
            var link = new SublimeChordCasterLevelLink {
                name = "SublimeChordCasterLevelLink",
                Selected = Ref<BlueprintSpellbookReference>(book),
                Own = Ref<BlueprintSpellbookReference>(Book),
                Class = Ref<BlueprintCharacterClassReference>(Class)
            };
            var prereq = new SublimeChordBookPrerequisite { Book = Ref<BlueprintSpellbookReference>(book) };
            // Use the class's readable name, not the internal blueprint asset
            // name: the picker must be obvious even with archetype variants.
            var ownerClass = book.CharacterClass;
            var feature = f.Add(new BlueprintFeature { Ranks = 1, IsClassFeature = true,
                ComponentsArray = new BlueprintComponent[] { link, prereq } },
                token, "奥术施法职业：" + (ownerClass?.Name ?? book.name),
                "把超凡之弦等级叠加到该职业的施法者等级，并让该职业法术与超凡之弦法术共享叠加后的施法者等级；不增加该职业的法术位或可知法术。");
            // Every picker entry shares the "Book/<guid>" token shape, so the
            // acronym fallback cannot tell them apart. The class icon is both
            // distinct and meaningful here, and it is the native resource.
            if (ownerClass?.m_Icon != null) Set(feature, "m_Icon", ownerClass.m_Icon);
            feature.OnEnable();
            cache.AddCachedBlueprint(feature.AssetGuid, feature);
            return feature;
        }

        // Deliberately broad: archetype and third-party spellbooks must stay
        // discoverable. The picker is narrowed to the books THIS character
        // actually owns (see NarrowBooks), so breadth here never becomes a
        // wall of options in the level-up UI.
        private static bool Eligible(BlueprintSpellbook book)
        {
            if (book == null || book == Book || book.IsMythic || !book.IsArcane) return false;
            return book.SpellsPerDay != null;
        }

        // Thread-safe snapshot of already-deserialized blueprints. The cache TOC
        // holds ~250k entries whose Blueprint field is still null; forcing Load()
        // on all of them would be the expensive mistake this avoids.
        internal static SimpleBlueprint[] Snapshot(BlueprintsCache cache)
        {
            var loaded = (IDictionary)Get(cache, "m_LoadedBlueprints");
            var gate = Get(cache, "m_Lock");
            var field = AccessTools.Field(loaded.GetType().GetGenericArguments()[1], "Blueprint");
            object[] entries = null;
            for (int attempt = 0; ; attempt++)
                try { lock (gate) { entries = new object[loaded.Count]; loaded.Values.CopyTo(entries, 0); } break; }
                catch (Exception e) when (attempt < 2 && (e is ArgumentException || e is InvalidOperationException || e is IndexOutOfRangeException)) { }
            return entries.Where(e => e != null).Select(e => field.GetValue(e)).OfType<SimpleBlueprint>().ToArray();
        }

        internal static void Reconcile(BlueprintsCache cache)
        {
            if (cache == null || Class == null || BookSelection == null || Reconciling) return;
            Reconciling = true;
            try
            {
                var f = new PsychicAssets(cache);
                var snapshot = Snapshot(cache);
                var candidates = snapshot.OfType<BlueprintSpellbook>().Where(Eligible)
                    .OrderBy(b => b.AssetGuid.ToString(), StringComparer.Ordinal).ToArray();
                var features = candidates.Select(book => {
                    if (!PublishedBooks.TryGetValue(book.AssetGuid, out var feature) || feature == null)
                    { feature = BookFeature(cache, f, book); PublishedBooks[book.AssetGuid] = feature; }
                    return feature;
                }).ToArray();
                var current = (BlueprintFeatureReference[])Get(BookSelection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
                var ids = new HashSet<BlueprintGuid>(current.Where(r => r != null).Select(r => r.Guid));
                var missing = features.Where(x => !ids.Contains(x.AssetGuid)).ToArray();
                if (missing.Length > 0)
                {
                    Set(BookSelection, "m_AllFeatures", current.Concat(missing.Select(Ref<BlueprintFeatureReference>)).ToArray());
                    var own = (BlueprintFeatureReference[])Get(BookSelection, "m_Features") ?? Array.Empty<BlueprintFeatureReference>();
                    Set(BookSelection, "m_Features", own.Concat(missing.Select(Ref<BlueprintFeatureReference>)).ToArray());
                }
                SublimeChordPerformances.Reconcile(cache, Class, snapshot);
            }
            catch (Exception e) { ModEntry.LogSpellDiagnostic("Sublime Chord reconcile skipped: " + e.Message); }
            finally { Reconciling = false; }
        }

        // Late-provider hook: O(1) check per registered blueprint, no polling.
        // Buffs matter too: performance rank configs live on effect buffs, not
        // on the activatable ability that spends the resource.
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint))]
        private static class BookPublishedPatch
        {
            private static void Postfix(SimpleBlueprint __1)
            {
                if (__1 is BlueprintSpellbook && Eligible(__1 as BlueprintSpellbook)) Pending = true;
                else if (__1 is BlueprintAbility || __1 is BlueprintActivatableAbility ||
                         __1 is BlueprintBuff || __1 is BlueprintFeature) Pending = true;
            }
        }
        private static bool Pending;

        // Native performances are still TOC entries when the cache initializes,
        // so the first few selection openings are the reliable discovery points;
        // later providers are marked by BookPublishedPatch. No startup scan.
        private static int SelectionOpens;

        // The catalogue is broad on purpose, but showing every archetype/
        // third-party spellbook to one character is unusable. Narrow it to
        // the spellbooks this level-up unit actually owns. Returning the
        // filtered array (and rewriting both feature lists) keeps Items,
        // CanSelect and the UI consistent within the same getter call.
        private static BlueprintFeatureReference[] NarrowBooks()
        {
            if (BookSelection == null || PublishedBooks.Count == 0) return null;
            var controller = Kingmaker.Game.Instance?.LevelUpController;
            var descriptor = (controller?.Preview ?? controller?.Unit)?.Descriptor;
            if (descriptor == null) return null;
            var owned = new HashSet<BlueprintGuid>();
            foreach (var spellbook in descriptor.Spellbooks)
                if (spellbook?.Blueprint != null) owned.Add(spellbook.Blueprint.AssetGuid);
            // A level-up preview unit can lag a level behind on spellbook
            // instances; class-default books for classes the unit actually
            // has are always unambiguous and keep the picker from coming up
            // empty. Archetype variants still rely on exact ownership.
            foreach (var classData in descriptor.Progression.Classes)
            {
                var defaultBook = classData?.CharacterClass?.Spellbook;
                if (defaultBook != null) owned.Add(defaultBook.AssetGuid);
            }
            if (owned.Count == 0) return null;
            var refs = PublishedBooks.Where(pair => owned.Contains(pair.Key) && pair.Value != null)
                .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
                .Select(pair => Ref<BlueprintFeatureReference>(pair.Value)).ToArray();
            Set(BookSelection, "m_AllFeatures", refs);
            Set(BookSelection, "m_Features", refs);
            return refs;
        }

        // Any feature-selection UI is a lazy rescan point, not just our own
        // spellbook picker. No work happens on startup.
        [HarmonyPatch(typeof(BlueprintFeatureSelection), "get_AllFeatures")]
        private static class SelectionOpenedPatch
        {
            private static void Postfix(BlueprintFeatureSelection __instance,
                ref ReferenceArrayProxy<BlueprintFeature, BlueprintFeatureReference> __result)
            {
                if (Cache == null) return;
                if (Pending || SelectionOpens < 6) { Pending = false; Reconcile(Cache); }
                SelectionOpens++;
                if (__instance != BookSelection) return;
                var narrowed = NarrowBooks();
                if (narrowed != null)
                    __result = new ReferenceArrayProxy<BlueprintFeature, BlueprintFeatureReference>(narrowed);
            }
        }

        [TypeId("d1c00000000000000000000000000001")]
        public sealed class SublimeChordCasterLevelLink : UnitFactComponentDelegate,
            IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IInitiatorRulebookSubscriber
        {
            public BlueprintSpellbookReference Selected;
            public BlueprintSpellbookReference Own;
            public BlueprintCharacterClassReference Class;
            public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
            {
                var owner = Owner;
                if (owner == null || evt.Spellbook == null || Class == null) return;
                int sc = owner.Progression.GetClassLevel(Class.Get());
                if (sc <= 0) return;
                var source = evt.Spellbook.Blueprint;
                if (Selected != null && source == Selected.Get())
                    evt.AddBonusCasterLevel(sc, ModifierDescriptor.UntypedStackable);
                else if (Own != null && source == Own.Get())
                {
                    // Read-only lookup: DemandSpellbook would create a book on a
                    // character who never had it.
                    var selected = Selected?.Get();
                    var other = selected == null ? null : owner.Spellbooks.FirstOrDefault(b => b.Blueprint == selected);
                    if (other != null && other.CasterLevel > 0)
                        evt.AddBonusCasterLevel(other.CasterLevel, ModifierDescriptor.UntypedStackable);
                }
            }
            public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
        }

        [TypeId("d1c00000000000000000000000000002")]
        public sealed class SublimeChordBookPrerequisite : Prerequisite
        {
            public BlueprintSpellbookReference Book;
            protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
            {
                var book = Book?.Get();
                if (book == null) return false;
                // Exact ownership is the reliable test: every archetype variant
                // shares CharacterClass with its base class, so comparing the
                // class alone made ALL Wizard/Bard variants selectable at once.
                foreach (var owned in unit.Spellbooks)
                    if (owned?.Blueprint == book) return true;
                // A level-up preview unit can lag a level behind; the class's
                // default spellbook is still unambiguous when the class is
                // present. Variants deliberately do not take this fallback.
                var cls = book.CharacterClass;
                return cls != null && cls.Spellbook == book &&
                       unit.Progression.Classes.Any(c => c.CharacterClass == cls);
            }
            protected override string GetUITextInternal(UnitDescriptor unit) => "必须已拥有该奥术施法职业";
        }

        // Native ContextActionSkillCheck only exposes Success/Failure or the DC
        // difference. This action keeps the raw RollResult (Wrath has no natural-1
        // auto-failure for skill checks) and publishes it as a shared value.
        [TypeId("d1c00000000000000000000000000003")]
        public sealed class SublimeChordSkillRoll : ContextAction
        {
            public StatType Stat;
            public AbilitySharedValue Output;
            public int Multiplier = 1;
            public bool Once;
            public AbilitySharedValue Guard = AbilitySharedValue.DungeonBoonValue;
            public override string GetCaption() => "超凡之弦沟通检定";
            public override void RunAction()
            {
                // AoE applies the action list once per target; the guard keeps a
                // single check for the whole cast without any polling/timer.
                if (Once && Context[Guard] != 0) return;
                var caster = Context?.MaybeCaster;
                if (caster == null) return;
                var check = new Kingmaker.RuleSystem.Rules.RuleSkillCheck(caster, Stat, 0) { Reason = Context, ShowAnyway = true };
                Context.TriggerRule(check);
                Context[Output] = Math.Max(0, check.RollResult) * Multiplier;
                if (Once) Context[Guard] = 1;
            }
        }
    }
}
