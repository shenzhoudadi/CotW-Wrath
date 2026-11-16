using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    /// <summary>
    /// Ensures PrestigePlus's Esoteric Knight prestige class supports CotW's
    /// psychic casters (Psychic, Amnesiac, Medium, Mindblade, Psychic Detective,
    /// and any future spellbook carrying PsychicSpellbookMarker).
    /// </summary>
    internal static class EsotericKnightPsychicCompat
    {
        internal const string EsotericKnightClassId = "0829a992-9641-49ed-be17-47e6ba023716";
        internal const string KnightSelectionId = "757e4730-c99f-4d9b-b795-bd927da4f769";
        private const string ClericClassId = "67819271767a9dd4fbfd4ae700befea0";

        private static readonly HashSet<BlueprintGuid> EsotericConversionFeatureIds = new HashSet<BlueprintGuid>
        {
            BlueprintGuid.Parse("eb739899-2921-46bd-ac91-ea71095d6249"), // ConfoundingProjection
            BlueprintGuid.Parse("ac8ac4ba-d54c-411b-90ad-217a3b2849ad"), // StepthroughReality
            BlueprintGuid.Parse("279288a3-f361-416b-b2c9-287bb436d273"), // PsychicRage
            BlueprintGuid.Parse("a40c8449-7751-4072-94a4-2a6227c90b1b"), // PsychicDaring
            BlueprintGuid.Parse("3ed4bc97-f870-485b-9087-63d20a033885"), // PhantomArmory
            BlueprintGuid.Parse("c72c31b1-e202-49ac-b742-d1e572ea48da"), // ShadowProjection
        };

        private static readonly HashSet<BlueprintGuid> InjectedBooks = new HashSet<BlueprintGuid>();
        private static bool ClassPatched;
        private static BlueprintsCache CachedCache;
        private static bool Pending = true;

        internal static bool IsPsychicBook(BlueprintSpellbook book)
            => book != null && !book.IsMythic && !book.IsAlchemist && book.SpellsPerDay != null &&
               book.CharacterClass != null && !book.CharacterClass.PrestigeClass &&
               (PsychicCasting.IsPsychic(book) || book.AssetGuid == BlueprintGuid.Parse(MediumIntegration.BookId));

        internal static void Register(BlueprintsCache cache)
        {
            if (cache == null) return;
            CachedCache = cache;
            Reconcile(cache);
        }

        internal static void Reconcile(BlueprintsCache cache)
        {
            if (cache == null) return;
            try
            {
                var knight = cache.Load(BlueprintGuid.Parse(EsotericKnightClassId)) as BlueprintCharacterClass;
                var selection = cache.Load(BlueprintGuid.Parse(KnightSelectionId)) as BlueprintFeatureSelection;
                if (knight == null || selection == null) return;

                if (!ClassPatched)
                {
                    var comps = (knight.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                        .Where(c => !(c is PrerequisiteClassLevel pcl &&
                            (pcl.CharacterClass?.AssetGuid == BlueprintGuid.Parse(KineticistClassBridge0020.KineticistId)
                             || pcl.CharacterClass?.AssetGuid == BlueprintGuid.Parse(ClericClassId)))
                            && !(c is EsotericKnightPsychicPrerequisite)
                            && !(c is EsotericKnightEntryPrerequisite))
                        .ToList();
                    comps.Add(new EsotericKnightEntryPrerequisite
                    {
                        name = "EsotericKnightEntryPrerequisite",
                        Group = Prerequisite.GroupType.All
                    });
                    knight.ComponentsArray = comps.ToArray();
                    ClassPatched = true;
                }

                var f = new PsychicAssets(cache);
                var snapshot = SublimeChord.Snapshot(cache);
                var psychicBooks = snapshot.OfType<BlueprintSpellbook>().Where(IsPsychicBook)
                    .OrderBy(b => b.AssetGuid.ToString(), StringComparer.Ordinal).ToArray();
                if (psychicBooks.Length == 0) return;

                var added = new List<BlueprintFeatureBase>();
                var currentAll = (BlueprintFeatureReference[])Get(selection, "m_AllFeatures") ?? Array.Empty<BlueprintFeatureReference>();
                var existingIds = new HashSet<BlueprintGuid>(currentAll.Where(r => r != null).Select(r => r.Guid));

                foreach (var book in psychicBooks)
                {
                    if (InjectedBooks.Contains(book.AssetGuid)) continue;
                    var prog = BuildProgression(cache, f, knight, book);
                    if (prog != null)
                    {
                        InjectedBooks.Add(book.AssetGuid);
                        if (!existingIds.Contains(prog.AssetGuid))
                            added.Add(prog);
                    }
                }

                if (added.Count > 0)
                {
                    var refs = added.Select(Ref<BlueprintFeatureReference>).ToArray();
                    Set(selection, "m_AllFeatures", currentAll.Concat(refs).ToArray());
                    var currentOwn = (BlueprintFeatureReference[])Get(selection, "m_Features") ?? Array.Empty<BlueprintFeatureReference>();
                    Set(selection, "m_Features", currentOwn.Concat(refs).ToArray());
                }
                Pending = false;
            }
            catch (Exception e)
            {
                ModEntry.LogSpellDiagnostic("Esoteric Knight psychic compatibility skipped: " + e.Message);
            }
        }

        private static BlueprintProgression BuildProgression(BlueprintsCache cache, PsychicAssets f,
            BlueprintCharacterClass knight, BlueprintSpellbook book)
        {
            var token = "EsotericKnightPsychic/" + book.AssetGuid;
            if (f.Assets.TryGetValue(token, out var staged)) return (BlueprintProgression)staged;
            var existing = cache.Load(Id(token)) as BlueprintProgression;
            if (existing != null) return existing;

            var cls = book.CharacterClass;
            string label = cls.Name ?? book.name;
            if (cls.Spellbook != book && !string.IsNullOrEmpty(book.Name))
                label = label + "（" + book.Name + "）";

            var prog = f.Add(new BlueprintProgression
            {
                Ranks = 1,
                IsClassFeature = true,
                LevelEntries = new[] { 2, 4, 6, 8, 10 }.Select(lvl =>
                {
                    var stepToken = token + "/AdvanceL" + lvl;
                    var step = f.Feature(stepToken, "异能法术推进：" + label,
                        "提升该异能施法职业的法术位与可知法术。",
                        new EsotericKnightPsychicAdvance
                        {
                            name = "EsotericKnightPsychicAdvance",
                            Book = Ref<BlueprintSpellbookReference>(book)
                        });
                    step.HideInUI = true;
                    step.HideInCharacterSheetAndLevelUp = true;
                    step.IsClassFeature = true;
                    step.Ranks = 1;
                    step.OnEnable();
                    cache.AddCachedBlueprint(step.AssetGuid, step);

                    var entry = new LevelEntry { Level = lvl };
                    entry.SetFeatures(new BlueprintFeatureBase[] { step });
                    return entry;
                }).ToArray(),
                ComponentsArray = new BlueprintComponent[]
                {
                    new SpellbookOwnedPrerequisite
                    {
                        name = "SpellbookOwned",
                        Book = Ref<BlueprintSpellbookReference>(book),
                        MinSpellLevel = 1
                    }
                }
            }, token, "异能施法：" + label,
                "自第2级起每2个异能骑士等级，你视为在原有的异能施法职业上提升了1个施法者等级（获得每日法术位与可知法术，不获得其他职业特性）。");

            Set(prog, "m_Classes", new[]
            {
                new BlueprintProgression.ClassWithLevel
                {
                    m_Class = Ref<BlueprintCharacterClassReference>(knight),
                    AdditionalLevel = 0
                }
            });
            Set(prog, "m_Archetypes", Array.Empty<BlueprintProgression.ArchetypeWithLevel>());
            if (cls.m_Icon != null) Set(prog, "m_Icon", cls.m_Icon);

            prog.OnEnable();
            cache.AddCachedBlueprint(prog.AssetGuid, prog);
            return prog;
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.AddCachedBlueprint))]
        private static class PublishedPatch
        {
            private static void Postfix(SimpleBlueprint __1)
            {
                if (__1 == null) return;
                if (__1.AssetGuid == BlueprintGuid.Parse(EsotericKnightClassId)
                    || __1.AssetGuid == BlueprintGuid.Parse(KnightSelectionId)
                    || (__1 is BlueprintSpellbook b && IsPsychicBook(b)))
                {
                    Pending = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintFeatureSelection), "get_AllFeatures")]
        private static class SelectionOpenedPatch
        {
            private static void Prefix(BlueprintFeatureSelection __instance)
            {
                if (!Pending || CachedCache == null || __instance == null) return;
                if (__instance.AssetGuid == BlueprintGuid.Parse(KnightSelectionId))
                    Reconcile(CachedCache);
            }
        }

        // Esoteric Knight's Psychic Esoterica features wire AddSpontaneousSpellConversion
        // to ClericClass by default. Mirror those conversions onto any psychic spellbook
        // the unit owns, and avoid demanding an empty Cleric spellbook if the unit is not a Cleric.
        // Walk base types with DeclaredOnly because UnitFactComponentDelegate<T>
        // shadows Fact with `new`, which makes AccessTools.Property throw AmbiguousMatchException.
        private static System.Reflection.PropertyInfo DeclaredProperty(Type type, string name)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.DeclaredOnly;
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var prop = t.GetProperty(name, flags);
                if (prop != null) return prop;
            }
            return null;
        }

        private static readonly System.Reflection.PropertyInfo ConversionFactProp =
            DeclaredProperty(typeof(SpontaneousSpellConversion), "Fact");
        private static readonly System.Reflection.PropertyInfo ConversionOwnerProp =
            DeclaredProperty(typeof(SpontaneousSpellConversion), "Owner");

        [HarmonyPatch(typeof(SpontaneousSpellConversion), "OnTurnOn")]
        private static class SpontaneousConversionOnPatch
        {
            private static bool Prefix(SpontaneousSpellConversion __instance)
            {
                if (__instance == null) return true;
                var fact = ConversionFactProp?.GetValue(__instance, null) as Kingmaker.UnitLogic.UnitFact;
                var owner = ConversionOwnerProp?.GetValue(__instance, null) as Kingmaker.EntitySystem.Entities.UnitEntityData;
                if (fact?.Blueprint == null || owner?.Descriptor == null) return true;
                if (!EsotericConversionFeatureIds.Contains(fact.Blueprint.AssetGuid)) return true;
                if (__instance.CharacterClass?.AssetGuid != BlueprintGuid.Parse(ClericClassId)) return true;

                string id = $"{fact.Blueprint}#{__instance.name}";
                var spells = __instance.SpellsByLevel.ToArray();
                foreach (var book in owner.Descriptor.Spellbooks)
                {
                    if (book != null && IsPsychicBook(book.Blueprint))
                        book.AddSpellConversionList(id, spells);
                }

                // Only let native OnTurnOn run if the unit actually has Cleric spellbook/class.
                return owner.Descriptor.GetSpellbook(__instance.CharacterClass) != null
                    || owner.Descriptor.Progression.GetClassData(__instance.CharacterClass) != null;
            }
        }

        [HarmonyPatch(typeof(SpontaneousSpellConversion), "OnTurnOff")]
        private static class SpontaneousConversionOffPatch
        {
            private static bool Prefix(SpontaneousSpellConversion __instance)
            {
                if (__instance == null) return true;
                var fact = ConversionFactProp?.GetValue(__instance, null) as Kingmaker.UnitLogic.UnitFact;
                var owner = ConversionOwnerProp?.GetValue(__instance, null) as Kingmaker.EntitySystem.Entities.UnitEntityData;
                if (fact?.Blueprint == null || owner?.Descriptor == null) return true;
                if (!EsotericConversionFeatureIds.Contains(fact.Blueprint.AssetGuid)) return true;
                if (__instance.CharacterClass?.AssetGuid != BlueprintGuid.Parse(ClericClassId)) return true;

                string id = $"{fact.Blueprint}#{__instance.name}";
                foreach (var book in owner.Descriptor.Spellbooks)
                {
                    if (book != null && IsPsychicBook(book.Blueprint))
                        book.RemoveSpellConversionList(id);
                }

                return owner.Descriptor.GetSpellbook(__instance.CharacterClass) != null
                    || owner.Descriptor.Progression.GetClassData(__instance.CharacterClass) != null;
            }
        }
    }

    [TypeId("d1c0000000000000000000000000000f")]
    public sealed class EsotericKnightPsychicPrerequisite : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
            => EsotericKnightEntryPrerequisite.HasPsychicSpellLevel1(unit);

        protected override string GetUITextInternal(UnitDescriptor unit) => "能够施放1环或更高环位异能法术";
    }

    [TypeId("d1c00000000000000000000000000011")]
    public sealed class EsotericKnightEntryPrerequisite : Prerequisite
    {
        private static readonly BlueprintGuid NativeBurnId = BlueprintGuid.Parse(KineticistClassBridge0020.BurnFeatureId);
        private static readonly BlueprintGuid NativeBlastId = BlueprintGuid.Parse(KineticistClassBridge0020.BlastRankId);
        private static readonly BlueprintGuid WaterDancerBurnId = BlueprintGuid.Parse("1c338fd6-9946-4413-ba48-d5d12e40b43c");
        private static readonly BlueprintGuid HavockerBurnId = Id(KineticistClassBridge0020.BridgeToken);
        private static readonly BlueprintGuid HavockerBlastId = Id(KineticistClassBridge0020.CloneToken(NativeBlastId));

        internal static bool HasBurnAndBlast(UnitDescriptor unit)
        {
            if (unit?.Progression?.Features == null) return false;
            bool hasBurn = false;
            bool hasBlast = false;
            foreach (var f in unit.Progression.Features.Enumerable)
            {
                var id = f?.Blueprint?.AssetGuid;
                if (id == null) continue;
                var guid = id.Value;
                if (guid == NativeBurnId || guid == HavockerBurnId || guid == WaterDancerBurnId)
                    hasBurn = true;
                else if (guid == NativeBlastId || guid == HavockerBlastId)
                    hasBlast = true;
            }
            if (!hasBurn && unit.Unit?.Get<Kingmaker.UnitLogic.Class.Kineticist.UnitPartKineticist>() != null)
                hasBurn = true;
            return hasBurn && hasBlast;
        }

        internal static bool HasPsychicSpellLevel1(UnitDescriptor unit)
        {
            if (unit == null) return false;
            foreach (var sb in unit.Spellbooks)
            {
                if (sb != null && EsotericKnightPsychicCompat.IsPsychicBook(sb.Blueprint) && sb.MaxSpellLevel >= 1)
                    return true;
            }
            foreach (var cls in unit.Progression.Classes)
            {
                var book = cls.Spellbook;
                if (book != null && EsotericKnightPsychicCompat.IsPsychicBook(book) &&
                    unit.DemandSpellbook(cls.CharacterClass).MaxSpellLevel >= 1)
                    return true;
            }
            return false;
        }

        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
            => HasBurnAndBlast(unit) || HasPsychicSpellLevel1(unit);

        protected override string GetUITextInternal(UnitDescriptor unit)
            => "拥有“超载（Burn）”与“念动力冲击（Kinetic Blast）”能力，或能够施放1环异能法术";
    }

    [TypeId("d1c00000000000000000000000000010")]
    public sealed class EsotericKnightPsychicAdvance : UnitFactComponentDelegate
    {
        public BlueprintSpellbookReference Book;

        protected override void OnActivate()
        {
            var bookBp = Book?.Get();
            if (bookBp == null || Owner == null) return;

            var spellbook = Owner.DemandSpellbook(bookBp);
            int prevLevel = spellbook.BaseLevel;
            spellbook.AddBaseLevel();
            int nextLevel = spellbook.BaseLevel;

            var state = Game.Instance?.LevelUpController?.State;
            if (state == null) return;

            var selectionData = state.DemandSpellSelection(bookBp, bookBp.SpellList);
            if (bookBp.SpellsKnown != null)
            {
                for (int i = 0; i <= 10; i++)
                {
                    int prevCount = Math.Max(0, bookBp.SpellsKnown.GetCount(prevLevel, i));
                    int nextCount = Math.Max(0, bookBp.SpellsKnown.GetCount(nextLevel, i));
                    int delta = Math.Max(0, nextCount - prevCount);
                    if (delta > 0)
                        selectionData.SetLevelSpells(i, (selectionData.LevelCount[i]?.SpellSelections?.Length ?? 0) + delta);
                }
            }
            int maxSpellLevel = spellbook.MaxSpellLevel;
            if (bookBp.SpellsPerLevel > 0)
            {
                if (prevLevel == 0)
                {
                    selectionData.SetExtraSpells(0, maxSpellLevel);
                    selectionData.ExtraByStat = true;
                    selectionData.UpdateMaxLevelSpells(Owner);
                }
                else
                {
                    selectionData.SetExtraSpells(bookBp.SpellsPerLevel, maxSpellLevel);
                }
            }
        }
    }
}
