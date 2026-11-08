// Adapted from Holic75/KingmakerRebalance Psychic.createEsotericStarseeker (MIT).
// Contract C01-STARSEEKER-20261107-r1. The 90 constellation spells are encoded
// verbatim from A-catalog90.json (the sole authority per contract line 257);
// the erroneous contract 8.2 table names/GUIDs are intentionally not used.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class EsotericStarseeker0081
    {
        internal const string ModuleName = "0081 Esoteric Starseeker";
        internal const string RootToken = "EsotericStarseekerArchetype0081";
        internal const string DisciplineSelectionToken = "EsotericStarseekerDisciplineSelection0081";
        internal const string FeatureToken = "WrittenInTheStarsFeature0081";
        internal const string ResourceToken = "WrittenInTheStarsResource0081";
        internal const string ParentToken = "WrittenInTheStarsAbility0081";
        // Native Foresight supplies the feature/parent icon and any missing L1 icon.
        internal const string ForesightId = "1f01a098d737ec6419aedc4e7ad61fdd";

        internal const string ArchetypeDesc =
            "The celestial bodies travel through the sky in fixed patterns, and knowledgeable astrologers rely on these stellar constants to make accurate predictions, but the constellations can also be a source of power. An esoteric starseeker is a psychic who focuses her studies on uncovering the secrets of the sky and the constellations of the Cosmic Caravan, able to read the stars as well as she can read minds.";
        internal const string ParentBaseDesc =
            "An esoteric starseeker can attune herself to a constellation of the Cosmic Caravan, gaining knowledge of new spells from it. She gains one bonus constellation spell for each spell level she can cast. At 11th level, she can attune herself to two constellations at once, choosing between the spells offered by both constellations.\nAttuning to a constellation is a full-round action that spends one daily use of Written in the Stars (one use per day, two per day from 11th level). An already-attuned constellation can only be changed by attuning to a different one; attuning to a third constellation at 11th level replaces the oldest attuned constellation. Attunement lasts until the next rest, which also refreshes the daily uses. The Cosmic Caravan and their associated spells are as follows:\n";
        internal const string FeatureTail =
            "An esoteric starseeker does not gain discipline bonus spells.";

        internal static string VariantToken(Constellation c) => "WrittenInTheStars" + c.Token + "Ability0081";
        internal static string BuffToken(Constellation c) => "WrittenInTheStars" + c.Token + "Buff0081";

        internal sealed class Constellation
        {
            internal readonly string Token, Display;
            internal readonly string[] Spells;
            internal Constellation(string token, string display, params string[] spells) { Token = token; Display = display; Spells = spells; }
        }

        // 10 constellations x 9 circles = 90 resolved GUIDs, verbatim from
        // A-catalog90.json (73 Wrath native + 12 CotW internal + 5 external).
        // Daughter L2 uses the ExpandedContent HypnoticPatternAbility override.
        static readonly Constellation[] Catalog = {
            new Constellation("LanternBearer", "The Lantern Bearer",
                "4d9bf81b7939b304185d58a09960f589", "ce7dad2b25acf85429b6c9550787b2d9", "bf0accce250381a44b857d4af6c8e10d",
                "4b8265132f9c8174f87ce7fa6d0fe47b", "4cf3d0fae3239ec478f51e86f49161cb", "f8cea58227f59c64399044a82c9735c4",
                "b22fd434bdb60fb4ba1068206402c4cf", "e96424f70ff884947b06f41a765b7658", "5e36df08c71748f7936bce310181fb71"),
            new Constellation("Newlyweds", "The Newlyweds",
                "88367310478c10b47903463c5d0152b0", "c7104f7526c4c524f91474614054547e", "55c8366f5298b04e4ba678f49bc46005",
                "7792da00c85b9e042a0fdfc2b66ec9a8", "a0fc99f0933d01643b2b8fe570caa4c5", "15a04c40f84545949abeedef7279751a",
                "f3277b49cc8ab2f4b03464bd11ef6a4b", "f958ef62eea5050418fb92dfa944c631", "41cf93453b027b94886901dbfc680cb9"),
            new Constellation("Daughter", "The Daughter",
                "90e59f4a4ada87243b7b3535a06d0638", "56b8f0304a704a67b3c35cbe8c774854", "5ab0d42fb68c9e34abae4921822b9d63",
                "0087fc2d64b6095478bc7b8d7d512caf", "d38aaf487e29c3d43a3bffa4a4a55f8f", "7f71a70d822af94458dc1a235507e972",
                "1e2d1489781b10a45a3b70192bba9be3", "120c84ce90b778cb82f025c2b62a9bf9", "43740dab07286fe4aa00a6ee104ce7c1"),
            new Constellation("Rider", "The Rider",
                "b0a50deb87a2b4e7bf38d9aff5c9ffcc", "4709274b2080b6444a3c11c6ebbe2404", "2d4263d80f5136b4296d6eb43a221d7d",
                "f09453607e683784c8fca646eec49162", "20b548bf09bb3ea4bafea78dcb4f3db6", "36c8971e91f1745418cc3ffdfac17b74",
                "da1b292d91ba37948893cdbe9ea89e28", "e788b02f8d21014488067bdd3ba7b325", "1f01a098d737ec6419aedc4e7ad61fdd"),
            new Constellation("Patriarch", "The Patriarch",
                "feb70aab86cc17f4bb64432c83737ac2", "f9ce1e9088de0d502a98bcce636b0615", "92681f181b507b34ea87018e8f7a528a",
                "76a629d019275b94184a1a8733cac45e", "8266f8809fc2de62bbd5ab64abe4b683", "f0f761b808dc4b149b08eaf44b99f633",
                "2ddc377f87b94aba9e8bab6c03f477e8", "a5c56f0f699daec44b7aedd8b273b08a", "52b5df2a97df18242aec67610616ded0"),
            new Constellation("Wagon", "The Wagon",
                "4f8181e7a7f1d904fbaea64220e83379", "464a7193519429f48b4d190acb753cf0", "486eaff58293f6441a5c2759c4872f98",
                "4a648b57935a59547b7a2ee86fb4f26a", "41e8a952da7a5c247b3ec1c2dbb73018", "cee712a5129a14e1973f89e542671288",
                "99e39437184a46b994da740ce399f591", "8b0761f3873aefb976c0f47fec3c600c", "585f7776285d17b60b9e8e2fe42a69be"),
            new Constellation("Pack", "The Pack",
                "7bdb6a9fb6b37614e96f155748ae50c6", "a4ad1b8fa11e7c347a608c004efce9d5", "97b991256e43bb140b263c326f690ce2",
                "7ed74a3ec8c458d4fb50b192fd7be6ef", "630c8b85d9f07a64f917d79cb5905741", "f6bcea6db14f0814d99b54856e918b92",
                "ab167fd8203c1314bac6568932f1752f", "d3ac756a229830243a72e84f3ab050d0", "22b9044aa229815429d57d0a30e4b739"),
            new Constellation("Mother", "The Mother",
                "9e1ad5d6f87d19e4d8883d63a6e35568", "03a9630394d10164a9410882d31572f0", "c3647ce7d6da49faa6e86830841e56e1",
                "c66e86905f7606c4eaa5c774f0357b2b", "0a5ddfbcfb3989543ac7c936fc256889", "b39dd2d96af348236b0117f57e5e445d",
                "df2a0ba6b6dcecf429cbb80a56fee5cf", "7ef49f184922063499b8f1346fb7f521", "58bcb999b3ad46c289dc68cba9c50bd0"),
            new Constellation("Stranger", "The Stranger",
                "f001c73999fb5a543a199f890108d936", "89940cde01689fb46946b2f8cd7b66b7", "903092f6488f9ce45a80943923576ab3",
                "ecaa0def35b38f949bd1976a6c9539e0", "12fb4a4c22549c74d949e2916a2f0b6a", "1f2e6019ece86d64baa5effa15e81ecc",
                "98310a099009bbd4dbdf66bcef58b4cd", "0e67fa8f011662c43934d486acc50253", "fc27ca9e65567cf7830601564a677d68"),
            new Constellation("Follower", "The Follower",
                "fbdd8c455ac4cde4a9a3e18c84af9485", "981a4173d895e71184d9a7f557a9e1d4", "8eead52509987034ea9025d60cc05985",
                "f34fb78eaaec141469079af124bcfa0f", "8878d0c46dfbd564e9d5756349d5e439", "a89dcbbab8f40e44e920cc60636097cf",
                "d361391f645db984bbf58907711a146a", "3b646e1db3403b940bf620e01d2ce0c7", "37302f72b06ced1408bf5bb965766d46"),
        };

        // The five external-provider spells run the full five-fold contract check
        // through PsychicSpellIntegration.Available; the other 85 use ValidSpell.
        static readonly HashSet<string> External = new HashSet<string>(StringComparer.Ordinal) {
            "56b8f0304a704a67b3c35cbe8c774854", // Daughter L2 HypnoticPatternAbility (ExpandedContent)
            "2ddc377f87b94aba9e8bab6c03f477e8", // Patriarch L7 MassFlySpell (TomeOfTheFirebird)
            "99e39437184a46b994da740ce399f591", // Wagon L7 ParticulateFormAbility (ExpandedContent)
            "c3647ce7d6da49faa6e86830841e56e1", // Mother L3 InvisibilityPurge (CharacterOptionsPlus)
            "58bcb999b3ad46c289dc68cba9c50bd0", // Mother L9 NewSpell.UseAkashicForm (PrestigePlus)
        };

        static readonly FieldInfo KnownClassField = AccessTools.Field(typeof(AddSpellKnownTemporary), "m_CharacterClass");
        static readonly FieldInfo KnownSpellField = AccessTools.Field(typeof(AddSpellKnownTemporary), "m_Spell");
        static void SetKnown(object obj, FieldInfo field, object value) {
            if (field == null) throw new MissingFieldException("AddSpellKnownTemporary", "m_CharacterClass/m_Spell");
            field.SetValue(obj, value);
        }

        static BlueprintAbility Resolve(BlueprintsCache cache, string guid) {
            if (External.Contains(guid)) {
                var link = PsychicSharedSpellCatalog.Entries.FirstOrDefault(l => l.Guid == guid);
                if (link == null) return null;
                return PsychicSpellIntegration.Available(cache, link, out var ability) ? ability : null;
            }
            return ValidSpell(cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility) ? cache.Load(BlueprintGuid.Parse(guid)) as BlueprintAbility : null;
        }
        static bool ValidSpell(BlueprintAbility a) => a != null && a.Type == AbilityType.Spell && a.Parent == null && Executable(a);
        static bool Executable(BlueprintAbility a) =>
            a.GetComponent<AbilityEffectRunAction>() != null || a.GetComponent<AbilityVariants>() != null ||
            a.GetComponent<AbilityEffectStickyTouch>() != null || a.GetComponent<AbilityShadowSpell>() != null ||
            a.GetComponent<AdditionalAbilityEffectRunActionOnClickedTarget>() != null;

        static string Ordinal(int n) => (n >= 11 && n <= 13) ? "th" : (n % 10 == 1 ? "st" : n % 10 == 2 ? "nd" : n % 10 == 3 ? "rd" : "th");
        static string SpellDescription(BlueprintAbility[] spells) =>
            string.Join(", ", spells.Select((s, i) => s.Name + " (" + (i + 1) + Ordinal(i + 1) + ")")) + ".";

        internal static bool Register(BlueprintsCache cache)
        {
            var cls = cache.Load(BlueprintGuid.Parse(PsychicClass.ClassId)) as BlueprintCharacterClass;
            if (cls == null) return false;
            if (Existing(cache, RootToken, cls)) return true;
            // The completed discipline pool and the six spell-less Magaambyan
            // progressions must both be published before this archetype builds.
            if (!ArcaneBatch01446.Existing(cache, PsychicClassCompletion01458.ModuleName)) return false;

            var disciplines = cache.Load(Id("DisciplineSelection")) as BlueprintFeatureSelection;
            var magaambyan = cache.Load(Id("MagaambyanDisciplineSelection01447")) as BlueprintFeatureSelection;
            var amp = cache.Load(Id("PhrenicAmplificationSelection")) as BlueprintFeatureSelection;
            var major = cache.Load(BlueprintGuid.Parse(PsychicClass.MajorId)) as BlueprintFeature;
            if (disciplines == null || magaambyan == null || amp == null || major == null) return false;
            if (disciplines.AllFeatures.Count() != 6 || magaambyan.AllFeatures.Count() != 6) return false;

            var progression = cls.Progression;
            if (!progression.LevelEntries.Any(e => e.Level == 1 && e.Features.Contains(disciplines))) return false;
            if (!progression.LevelEntries.Any(e => e.Level == 1 && e.Features.Contains(amp))) return false;
            if (!progression.LevelEntries.Any(e => e.Level == 11 && e.Features.Contains(amp))) return false;
            if (!progression.LevelEntries.Any(e => e.Level == 11 && e.Features.Contains(major))) return false;

            // Atomic gate: validate all 90 constellation spells before building
            // anything. Any missing/invalid spell returns false with zero publish.
            var resolved = new BlueprintAbility[Catalog.Length][];
            for (int c = 0; c < Catalog.Length; c++) {
                var row = new BlueprintAbility[9];
                for (int i = 0; i < 9; i++) {
                    var spell = Resolve(cache, Catalog[c].Spells[i]);
                    if (spell == null) return false;
                    row[i] = spell;
                }
                resolved[c] = row;
            }

            var f = new PsychicAssets(cache);
            var foresight = f.Native<BlueprintAbility>(ForesightId);

            // Independent selection referencing Magaambyan's six spell-less
            // discipline progressions; the 01447 receipt itself is never touched.
            var noSpells = f.Clone(disciplines, DisciplineSelectionToken, "Psychic Discipline", MagaambyanTelepath01447.DisciplineText);
            var options = magaambyan.AllFeatures.ToArray();
            foreach (string field in new[] { "m_Features", "m_AllFeatures" })
                Set(noSpells, field, options.Select(Ref<BlueprintFeatureReference>).ToArray());
            // PsychicDisciplines.Build never assigns the shared selection an icon, so
            // a bare clone would publish one. Inherit the donor's icon when it has
            // one and otherwise fall back to Foresight, exactly as the Magaambyan
            // Telepath spell-less selection does.
            SpellBatch01447.Icon(noSpells, disciplines);
            if (ReferenceEquals(noSpells.Icon, null)) SpellBatch01447.Icon(noSpells, foresight);

            // 1/day at levels 1-10, 2/day at 11-20, never 3 at 21.
            var resource = Resource(f, ResourceToken, cls, 1, 11);
            var max = Get(resource, "m_MaxAmount");
            Set(max, "LevelStep", 100); Set(max, "PerStepIncrease", 0);
            Set(resource, "m_MaxAmount", max);

            string parentFull = ParentBaseDesc + string.Concat(Catalog.Select((c, ci) => c.Display + ": " + SpellDescription(resolved[ci]) + "\n"));

            var variants = new List<BlueprintAbility>();
            for (int ci = 0; ci < Catalog.Length; ci++) {
                var c = Catalog[ci];
                var spells = resolved[ci];
                string spellsDesc = SpellDescription(spells);
                // Reference equality, never UnityEngine.Object's overloaded ==: a
                // deserialized sprite reference whose native instance is not resident
                // yet still compares "null" and would silently drop every
                // constellation's own artwork in favour of the fallback. This is the
                // same idiom MagaambyanTelepath01447 uses for its icon fallbacks.
                var iconSource = !ReferenceEquals(spells[0].Icon, null) ? spells[0] : foresight;

                // Nine circle-gated temporary-known components on one buff; each
                // is its own native ledger key. Distinct names keep the reload
                // name-match unambiguous within this buff.
                var components = Enumerable.Range(1, 9).Select(circle => {
                    var comp = new ConstellationCircleSpell0081 { Circle = circle, Level = circle, OnlySpontaneous = true, name = "StarseekerCircle" + circle };
                    SetKnown(comp, KnownClassField, Ref<BlueprintCharacterClassReference>(cls));
                    SetKnown(comp, KnownSpellField, Ref<BlueprintAbilityReference>(spells[circle - 1]));
                    return (BlueprintComponent)comp;
                }).ToArray();
                var buff = f.Buff(BuffToken(c), "Written in the Stars: " + c.Display, "Constellation spells: " + spellsDesc, components);
                SetEnum(buff, "m_Flags", "RemoveOnRest, StayOnDeath");
                SpellBatch01447.Icon(buff, iconSource);

                var variant = f.Ability(VariantToken(c), "Written in the Stars: " + c.Display, "Constellation spells: " + spellsDesc,
                    AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Personal,
                    Cost(resource, 1),
                    new WrittenInTheStarsRestriction0081 { Constellation = Ref<BlueprintBuffReference>(buff) },
                    Run(new WrittenInTheStarsApplyAction0081 { Constellation = Ref<BlueprintBuffReference>(buff) }));
                Set(variant, "m_IsFullRoundAction", true);
                variant.CanTargetPoint = false; variant.NotOffensive = true;
                SpellBatch01447.Icon(variant, iconSource);
                variants.Add(variant);
            }

            var menu = new AbilityVariants();
            Set(menu, "m_Variants", variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            var parent = f.Ability(ParentToken, "Written in the Stars", parentFull,
                AbilityType.Supernatural, UnitCommand.CommandType.Standard, AbilityRange.Personal, menu);
            Set(parent, "m_IsFullRoundAction", true);
            parent.CanTargetPoint = false; parent.NotOffensive = true;
            SpellBatch01447.Icon(parent, foresight);
            foreach (var a in variants) Set(a, "m_Parent", Ref<BlueprintAbilityReference>(parent));

            var feature = f.Feature(FeatureToken, "Written in the Stars", parentFull + FeatureTail,
                Facts(parent), Grant(resource), new WrittenInTheStarsFeatureCleanup0081());
            feature.IsClassFeature = true;
            SpellBatch01447.Icon(feature, foresight);

            var root = SpellBatch01447.Archetype(f, cls, RootToken, "Esoteric Starseeker", ArchetypeDesc,
                new[] { Entry(1, noSpells, feature), Entry(15, major) },
                new[] { Entry(1, disciplines, amp), Entry(11, amp, major) });

            // Prime each native lazy LevelEntry list before freezing definitions.
            foreach (var entry in root.AddFeatures.Concat(root.RemoveFeatures)) entry.Features.ToArray();
            foreach (var ft in f.Assets.Values.OfType<BlueprintFeature>()) ft.IsClassFeature = true;
            foreach (var fact in f.Assets.Values.OfType<BlueprintUnitFact>())
                if (ReferenceEquals(fact.Icon, null)) throw new InvalidOperationException("0081 published fact has no icon: " + fact.name);

            SpellBatch01447.PublishArchetype(cache, f, cls, root, new BlueprintFeatureBase[] { noSpells, feature });
            return true;
        }
    }

    // Circle-gated temporary known spell. Subclasses the native component so this
    // instance is the ledger key and Fact (the buff) is the native source. Only
    // circles the caster can actually cast are registered; level-up unlocks more.
    // EmptyComponentData: never touch Data. Reconcile fires from the native part
    // methods via TemporaryKnowledge01464, so Sync never calls it directly.
    [TypeId("0081c4b29e7d4a5183f0112233440003")]
    [AllowMultipleComponents]
    public sealed class ConstellationCircleSpell0081 : AddSpellKnownTemporary, IOwnerGainLevelHandler
    {
        public int Circle;
        static readonly FieldInfo EntriesField = AccessTools.Field(typeof(UnitPartTemporarySpellsKnown), "m_Entries");
        static readonly FieldInfo EntryNameField = AccessTools.Field(typeof(UnitPartTemporarySpellsKnown.Entry), "m_ComponentName");

        protected override void OnActivate() => Sync();
        protected override void OnTurnOn() => Sync();
        protected override void OnPostLoad() => Sync();
        protected override void OnApplyPostLoadFixes() => Sync();
        protected override void OnDeactivate() { base.OnDeactivate(); Dirty(); }
        public void HandleUnitGainLevel() => Sync();

        void Sync()
        {
            var owner = Owner;
            if (owner == null) return;
            var book = StarseekerRuntime.Book(owner);
            int max = book?.MaxSpellLevel ?? 0;
            var part = owner.Get<UnitPartTemporarySpellsKnown>();
            bool registered = HasEntry(part);
            if (book != null && max >= Circle && !registered)
                owner.Ensure<UnitPartTemporarySpellsKnown>().AddEntry(Fact, this);
            else if (registered && (book == null || max < Circle))
                part.RemoveEntry(Fact, this);
            Dirty();
        }

        // Match by source fact and stable component name: the native Component
        // reference is null between deserialization and the part's OnPostLoad, so
        // an identity match would transiently miss and duplicate the entry.
        bool HasEntry(UnitPartTemporarySpellsKnown part)
        {
            if (part == null) return false;
            var entries = EntriesField.GetValue(part) as List<UnitPartTemporarySpellsKnown.Entry>;
            if (entries == null) return false;
            foreach (var entry in entries)
                if (!entry.Removed && ReferenceEquals(entry.Source, Fact) && (string)EntryNameField.GetValue(entry) == this.name)
                    return true;
            return false;
        }

        void Dirty()
        {
            try { Owner.UISettings?.SetDirty(); }
            catch (Exception error) { ModEntry.LogSpellDiagnostic("Written in the Stars action bar refresh failed: " + error); }
        }
    }

    // Blocks attuning to an already-active constellation (zero cost) and requires
    // a psychic spellbook that can cast 1st-level spells.
    [TypeId("0081c4b29e7d4a5183f0112233440001")]
    public sealed class WrittenInTheStarsRestriction0081 : BlueprintComponent, IAbilityCasterRestriction
    {
        public BlueprintBuffReference Constellation;
        public bool IsCasterRestrictionPassed(UnitEntityData caster) {
            var book = StarseekerRuntime.Book(caster);
            return book != null && book.MaxSpellLevel >= 1 && !caster.Buffs.HasFact(Constellation.Get());
        }
        public string GetAbilityCasterRestrictionUIText() =>
            "You must be able to cast 1st-level psychic spells and not already be attuned to this constellation.";
    }

    // Applies the target constellation buff, spending the daily use handled by the
    // AbilityResourceLogic. Enforces the active cap (1 before 11th level, 2 after)
    // with FIFO replacement of the oldest attunement. Never refunds the resource.
    [TypeId("0081c4b29e7d4a5183f0112233440002")]
    public sealed class WrittenInTheStarsApplyAction0081 : ContextAction
    {
        public BlueprintBuffReference Constellation;
        public override string GetCaption() => "Attune to a constellation of the Cosmic Caravan";
        public override void RunAction()
        {
            var owner = Context.MaybeCaster;
            if (owner == null) return;
            var book = StarseekerRuntime.Book(owner);
            if (book == null || book.MaxSpellLevel < 1) return;
            var buff = Constellation.Get();
            if (buff == null || owner.Buffs.HasFact(buff)) return;
            var cls = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId));
            int cap = owner.Progression.GetClassLevel(cls) >= 11 ? 2 : 1;
            // Buffs enumerate in activation order, so the earliest are the oldest.
            var active = owner.Buffs.Enumerable.Where(b => b.Blueprint.GetComponent<ConstellationCircleSpell0081>() != null).ToArray();
            int toRemove = active.Length - (cap - 1);
            for (int i = 0; i < toRemove; i++) active[i].Remove();
            StarseekerRuntime.Add(owner, buff, Context, null);
        }
    }

    // Respec/removal cleanup: drop every active constellation buff so their circle
    // components release all temporary known spells. EmptyComponentData: no Data.
    [TypeId("0081c4b29e7d4a5183f0112233440004")]
    public sealed class WrittenInTheStarsFeatureCleanup0081 : UnitFactComponentDelegate
    {
        protected override void OnDeactivate() {
            var owner = Owner;
            if (owner == null) return;
            foreach (var buff in owner.Buffs.Enumerable.Where(b => b.Blueprint.GetComponent<ConstellationCircleSpell0081>() != null).ToArray())
                buff.Remove();
        }
    }

    internal static class StarseekerRuntime
    {
        // The esoteric starseeker keeps the standard psychic spellbook; there is
        // no private replacement book. Return it only for archetype holders.
        internal static Spellbook Book(UnitEntityData owner)
        {
            var cls = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId));
            var data = owner?.Progression.GetClassData(cls);
            if (data?.Archetypes.Any(a => a.AssetGuid == PsychicAssets.Id(EsotericStarseeker0081.RootToken)) != true) return null;
            var book = owner.Descriptor.GetSpellbook(data.Spellbook);
            return book != null && PsychicCasting.IsPsychic(book.Blueprint) ? book : null;
        }
        internal static Buff Add(UnitEntityData owner, BlueprintBuff bp, MechanicsContext context, TimeSpan? duration)
        {
            var buff = owner.Buffs.AddBuff(bp, context, duration);
            if (buff != null) buff.IsNotDispelable = true;
            return buff;
        }
    }
}
