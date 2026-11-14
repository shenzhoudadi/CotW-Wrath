// COTW-0046 F-0046: Shaman archetype "Speaker for the Past".
// Adapted from Holic75/KingmakerRebalance (MIT), Shaman.createSpeakerForThePast,
// pinned at 1332fb0db844b7863f484ca978bea2349fe49769.
//
// Contract: C01-SPEAKER-0046-20261108-r1. Automatically discovers and reuses
// the 18 bonus spells (Time + Ancestors) and 16 revelations from the published
// Oracle Time Mystery and native Ancestors Mystery, deep-cloning every owned
// revelation subgraph into private PsychicAssets bound to ShamanClass +
// SpeakerForThePast + Wisdom, while applying Speaker-specific D-review adapters
// (Blood of Heroes fear-only Will, Sacred Council CL check variants, Spirit of
// the Warrior Shaman-level BAB + Transformation mutual exclusion).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static partial class SpeakerForThePast0046
    {
        internal const string ModuleName = "Shaman archetype: Speaker for the Past";
        internal const string RootToken = "SpeakerForThePastArchetype0046";
        internal const string MysteriesToken = "MysteriesOfThePastFeature0046";
        internal const string SpellListToken = "MysteriesOfThePastSpellList0046";
        internal const string SelectionToken = "RevelationPastSelection0046";

        const string ShamanClassId = "145f1d3d360a7ad48bd95d392c81b38e";
        const string ShamanProgressionId = "d04a543c82b45694c805301bce9dac24";
        const string ShamanSpellbookId = "44f16931dabdff643bfe2a48138e769f";
        const string ShamanSpelllistId = "c0c40e42f07ff104fa85492da464ac69";
        const string SandsOfTimeId = "da49f07e431b47abaad41ef28df487f7";
        const string BloodOfHeroesId = "1b9eebdfa2ad9d44eb60446d465b0cfd";
        const string SacredCouncilId = "a8ae26103124a1c409279392b8f56238";
        const string SpiritOfTheWarriorId = "b63f198db73edbc4b84d863cb681f5bc";
        const string SpiritShieldId = "899d6aa944ea9654fa829edcaedcf073";

        internal static string GateReport = "not probed";
        internal static string ProviderReport = "none";
        static bool Reject(string reason) { GateReport = reason; return false; }

        // Ruling #3: the 4 removals are exact progression entries.
        static readonly (int Level, string Guid)[] Removals = {
            (1, "d22f319fefac4ca4b90f03ac5cb9c714"),
            (4, "70e6075a72633ba49804ccb6203fb18c"),
            (6, "2674f98616188814fbba9f8fccf43564"),
            (14, "f15d95a394e950a42b1d6afbb9f781d9"),
        };

        // Read-only native leaf assets (Ruling #1). Referenced or left intact
        // when deep-cloning Oracle revelation subgraphs for Speaker for the Past.
        internal const string IconDonorId = "486eaff58293f6441a5c2759c4872f98"; // Haste
        internal const string ShakenId = "25ec6cb6ab1845c48a95f9c20b034220";
        internal const string FrightenedId = "f08a7239aa961f34c8301518e71d4cdf";
        internal const string BlurBuffId = "dd3ad347240624d46a11a092b4dd4674";
        internal const string DisplacementBuffId = "00402bae4442a854081264e498e7a833";
        internal const string TrueSeeingBuffId = "09b4b69169304474296484c74aa12027";
        internal const string ForesightBuffId = "8c385a7610aa409468f3a6c0f904ac92";
        internal const string SpiritShieldMissBuffId = "cef9569005de6234bb8b69e45f9509d8";
        internal const string IncorporealId = "c4a7f98d743bc784c9d4cf2105852c39";
        internal const string UndeadTypeId = "734a29b693e9ec346ba2951b27987e33";
        internal const string ConstructTypeId = "fd389783027d63343b4a5634bd81645f";

        static readonly string[] LeafIds = {
            IconDonorId, ShakenId, FrightenedId, BlurBuffId, DisplacementBuffId,
            TrueSeeingBuffId, ForesightBuffId, SpiritShieldMissBuffId, IncorporealId,
            UndeadTypeId, ConstructTypeId
        };

        static readonly HashSet<BlueprintGuid> SharedVanillaLeafIds = BuildSharedLeafSet();

        static HashSet<BlueprintGuid> BuildSharedLeafSet()
        {
            var set = new HashSet<BlueprintGuid>();
            foreach (var id in LeafIds.Concat(new[] {
                TouchDeliveryId, TouchCastId, TouchWeaponId, TransformationBuffId, StasisBuffId,
                DimensionDoorCasterOnlyId, DimensionDoorMassId, HasteId, SlowId, GhostTouchId, KeenId,
                SandsOfTimeId
            }))
                set.Add(BlueprintGuid.Parse(id));
            set.Add(Id(TimeStop01438.RootToken));
            return set;
        }

        internal static Dictionary<string, BlueprintUnitFact> LoadLeafAssets(BlueprintsCache cache, out string missingId)
        {
            var leaf = new Dictionary<string, BlueprintUnitFact>();
            foreach (var id in LeafIds)
            {
                var asset = cache.Load(BlueprintGuid.Parse(id)) as BlueprintUnitFact;
                if (asset == null) { missingId = id; return null; }
                leaf[id] = asset;
            }
            missingId = null;
            return leaf;
        }

        const string ArchetypeText =
            "A speaker for the past serves as the voice of the ancestors of her people. " +
            "She trades the spirit animal, wandering spirit and wandering hexes for the mysteries of the past: " +
            "a private list of ancestral spells and five revelations drawn from the Ancestor and Time mysteries. " +
            "Her revelations scale with her shaman level and Wisdom, never with Oracle levels or Charisma.";
        const string MysteriesText =
            "A speaker for the past adds Knowledge (World), Perception and Use Magic Device as class skills, " +
            "and gains two ancestral spells at each spell circle from 1st through 9th. " +
            "These spells are learned in the shaman's primary spellbook and prepared in normal spell slots; " +
            "they are never added to the spirit magic slots.";
        const string SelectionText =
            "At 4th level and again at 6th, 12th, 14th and 20th level, a speaker for the past selects one " +
            "revelation from the Ancestor and Time mysteries. Each revelation may be selected only once.";

        internal sealed class ResCfg
        {
            internal int Base, Step, Start, PerStep = 1, LevelIncrease;
            internal bool ByStat, ByLevel;
            internal static ResCfg Div(int step, int start, int perStep = 1) => new ResCfg { Step = step, Start = start, PerStep = perStep };
            internal static ResCfg Gated(int start, int step, int perStep = 1) => new ResCfg { Step = step, Start = start, PerStep = perStep };
            internal static ResCfg Stat(int baseValue) => new ResCfg { Base = baseValue, ByStat = true };
            internal static ResCfg Level(int increase) => new ResCfg { ByLevel = true, LevelIncrease = increase };
        }

        internal sealed class Rev
        {
            internal string Token, Name, Text;
            internal int Min;
            internal string ResToken;
            internal ResCfg Res;
            internal UnitCommand.CommandType Action;
            internal AbilityRange Range;
            internal int Duration = 1;
            internal DurationRate Rate = DurationRate.Rounds;
            internal bool Personal;
            internal string[] ApplyIds = Array.Empty<string>();
            internal string[] OwnBuffTokens = Array.Empty<string>();
            internal bool Passive;
            internal int GrantLevel;
            internal string GrantToken, GrantName, GrantBuffId;
        }

        internal static bool ValidSpell(BlueprintAbility a) =>
            a != null && a.Type == AbilityType.Spell && a.Parent == null &&
            (a.GetComponent<AbilityEffectRunAction>() != null || a.GetComponent<AbilityVariants>() != null ||
             a.GetComponent<AbilityEffectStickyTouch>() != null || a.GetComponent<AbilityShadowSpell>() != null ||
             a.GetComponent<AdditionalAbilityEffectRunActionOnClickedTarget>() != null);

        static void Wire(object obj, string field, object value, List<string> wired)
        {
            var type = obj.GetType();
            var f = AccessTools.Field(type, field) ??
                    AccessTools.Field(type, "<" + field.TrimStart('m', '_') + ">k__BackingField");
            if (f == null)
            {
                foreach (var candidate in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (candidate.Name.IndexOf(field.TrimStart('m', '_'), StringComparison.OrdinalIgnoreCase) >= 0 &&
                        value != null && candidate.FieldType.IsInstanceOfType(value))
                    { f = candidate; break; }
            }
            if (f == null || (value != null && !f.FieldType.IsInstanceOfType(value))) return;
            f.SetValue(obj, value);
            wired.Add(f.Name);
        }

        // ------------------------------------------------------------------
        // Ruling #11: atomic pre-validation. Auto-discovers spells & revelations
        // from TimeOracleMysteryFeature0046 + OracleAncestorsMysteryFeature.
        // ------------------------------------------------------------------
        internal static bool Register(BlueprintsCache cache)
        {
            GateReport = "preflight";
            var cls = cache.Load(BlueprintGuid.Parse(ShamanClassId)) as BlueprintCharacterClass;
            if (cls == null) return Reject("reject: ShamanClass " + ShamanClassId + " missing");
            if (ArchetypeBatch01445.Existing(cache, RootToken, cls)) { GateReport = "already published (idempotent)"; return true; }
            if (cache.Load(Id(RootToken)) != null) { GateReport = "already published (root present)"; return true; }

            var progression = cache.Load(BlueprintGuid.Parse(ShamanProgressionId)) as BlueprintProgression;
            var spellbook = cache.Load(BlueprintGuid.Parse(ShamanSpellbookId)) as BlueprintSpellbook;
            var nativeList = cache.Load(BlueprintGuid.Parse(ShamanSpelllistId)) as BlueprintSpellList;
            if (progression == null || spellbook == null || nativeList == null)
                return Reject("reject: Shaman progression/spellbook/native spell list missing");
            if (!ReferenceEquals(cls.Progression, progression))
                return Reject("reject: ShamanClass.Progression is not " + ShamanProgressionId);

            foreach (var removal in Removals)
            {
                var id = BlueprintGuid.Parse(removal.Guid);
                if (!progression.LevelEntries.Any(e => e.Level == removal.Level && e.Features.Any(x => x != null && x.AssetGuid == id)))
                    return Reject("reject: removal L" + removal.Level + " " + removal.Guid + " not on Shaman progression");
            }

            // Require Time Mystery and Ancestors Mystery as upstream sources.
            var oracleClass = cache.Load(BlueprintGuid.Parse(TimeMystery0046.OracleClassId)) as BlueprintCharacterClass;
            var arcanistClass = cache.Load(BlueprintGuid.Parse(TimeMystery0046.ArcanistClassId)) as BlueprintCharacterClass;
            var timeMystery = cache.Load(Id(TimeMystery0046.RootToken)) as BlueprintFeature;
            var ancestorsMystery = cache.Load(BlueprintGuid.Parse(TimeMystery0046.OracleAncestorsMysteryId)) as BlueprintFeature;
            var oracleRevelationSelection = cache.Load(BlueprintGuid.Parse(TimeMystery0046.OracleRevelationSelectionId)) as BlueprintFeatureSelection;
            if (timeMystery == null)
            {
                var link = PsychicSharedSpellCatalog.Entries.FirstOrDefault(l => l.Guid == SandsOfTimeId);
                if (link != null && !PsychicSpellIntegration.Available(cache, link, out _))
                {
                    ProviderReport = "missing: " + link.Provider + " / " + link.Name + " / " + link.Guid;
                    return Reject("reject: external provider " + link.Provider + " has not published " +
                        link.Name + " (" + link.Guid + "); TimeOracleMysteryFeature0046 absent; Register returns false by contract");
                }
                return Reject("reject: TimeOracleMysteryFeature0046 (" + Id(TimeMystery0046.RootToken) + ") not registered; " + TimeMystery0046.GateReport);
            }
            if (oracleClass == null || ancestorsMystery == null || oracleRevelationSelection == null)
                return Reject("reject: OracleClass / OracleAncestorsMysteryFeature / OracleRevelationSelection missing");

            // Auto-discover 18 bonus spells (9 Time + 9 Ancestors) from mystery level-2 spell grants.
            if (!DiscoverMysterySpells(oracleClass, timeMystery, ancestorsMystery, out var resolved, out var spellErr))
                return Reject(spellErr);

            var sandsLink = PsychicSharedSpellCatalog.Entries.FirstOrDefault(l => l.Guid == SandsOfTimeId);
            if (sandsLink != null)
            {
                if (!PsychicSpellIntegration.Available(cache, sandsLink, out var providerSands))
                {
                    ProviderReport = "missing: " + sandsLink.Provider + " / " + sandsLink.Name + " / " + SandsOfTimeId;
                    return Reject("reject: external provider " + sandsLink.Provider +
                        " has not published " + sandsLink.Name + " (" + SandsOfTimeId + "); Register returns false by contract");
                }
                if (resolved[3].All(s => s.AssetGuid != providerSands.AssetGuid))
                    return Reject("reject: discovered Time Mystery circle-3 spell does not match external " + SandsOfTimeId);
                ProviderReport = "available: " + sandsLink.Provider + " / " + sandsLink.Name + " / " + SandsOfTimeId;
            }

            // Auto-discover 16 revelations (8 Time + 8 Ancestors) from OracleRevelationSelection.
            if (!DiscoverMysteryRevelations(oracleRevelationSelection, timeMystery, ancestorsMystery, out var sourceRevelations, out var revErr))
                return Reject(revErr);

            var leaf = LoadLeafAssets(cache, out var missingLeaf);
            if (leaf == null) return Reject("reject: native leaf " + missingLeaf + " missing");
            foreach (var donor in ExtraNativeIds)
            {
                var asset = cache.Load(BlueprintGuid.Parse(donor.Guid));
                if (asset == null || !donor.Type.IsInstanceOfType(asset))
                    return Reject("reject: native donor " + donor.Guid + " missing or not a " + donor.Type.Name);
            }

            var f = new PsychicAssets(cache);

            // Private spell list: 1..9 x 2 discovered from Time + Ancestors, no spirit-magic hook.
            var list = SpellBatch01447.List(f, SpellListToken,
                level => level >= 1 && level <= 9 ? resolved[level] : Array.Empty<BlueprintAbility>());

            // Create archetype root first so every cloned revelation binds to Shaman + Speaker.
            var root = f.Add(new BlueprintArchetype {
                LocalizedName = Text(RootToken, "name", "Speaker for the Past"),
                LocalizedDescription = Text(RootToken, "description", ArchetypeText),
                AddFeatures = Array.Empty<LevelEntry>(), RemoveFeatures = Array.Empty<LevelEntry>(),
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, RootToken, "Speaker for the Past", ArchetypeText);
            Set(root, "m_ParentClass", cls);
            root.ReplaceClassSkills = true;
            root.ClassSkills = new[] {
                StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillKnowledgeWorld,
                StatType.SkillLoreNature, StatType.SkillLoreReligion, StatType.SkillPersuasion,
                StatType.SkillPerception, StatType.SkillUseMagicDevice
            };

            var learnComponents = new List<BlueprintComponent>();
            var learnType = AccessTools.TypeByName("Kingmaker.UnitLogic.FactLogic.LearnSpellList");
            if (learnType == null)
                LearnSpellListReport = "unavailable: native LearnSpellList type not found; SpeakerSpellbookSync0046 remains the active spellbook hook";
            else
            {
                var learn = (BlueprintComponent)Activator.CreateInstance(learnType);
                learn.name = "Speaker0046LearnSpellList";
                var wired = new List<string>();
                Wire(learn, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(cls), wired);
                Wire(learn, "m_Archetype", Ref<BlueprintArchetypeReference>(root), wired);
                Wire(learn, "m_SpellList", Ref<BlueprintSpellListReference>(list), wired);
                LearnSpellListReport = learnType.FullName + " fields=[" +
                    string.Join(",", learnType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Select(x => x.Name)) + "] wired=[" + string.Join(",", wired) + "]";
                if (wired.Count < 3)
                {
                    GateReport = "reject: native LearnSpellList changed shape: " + LearnSpellListReport;
                    throw new InvalidOperationException("COTW-0046: " + GateReport);
                }
                learnComponents.Add(learn);
            }
            var sync = new SpeakerSpellbookSync0046 { name = "Speaker0046SpellbookSync" };
            sync.CharacterClassSpellbook = Ref<BlueprintSpellbookReference>(spellbook);
            sync.SpellList = Ref<BlueprintSpellListReference>(list);
            sync.NativeShamanSpellList = Ref<BlueprintSpellListReference>(nativeList);
            learnComponents.Add(sync);
            var mysteries = f.Feature(MysteriesToken, "Mysteries of the Past", MysteriesText, learnComponents.ToArray());
            mysteries.IsClassFeature = true;
            RevelationBridge0046.Icon(mysteries, leaf[IconDonorId]);

            // Auto-clone and rebind every discovered Time + Ancestors revelation.
            var cloner = new RevelationCloner(f, cache, cls, root, oracleClass, arcanistClass, timeMystery, ancestorsMystery, leaf[IconDonorId]);
            var adaptedFeatures = sourceRevelations.Select(cloner.Adapt).ToArray();

            var selection = f.Add(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true, IgnorePrerequisites = false },
                SelectionToken, "Revelations of the Past", SelectionText);
            foreach (var field in new[] { "m_Features", "m_AllFeatures" })
                Set(selection, field, adaptedFeatures.Select(Ref<BlueprintFeatureReference>).ToArray());
            RevelationBridge0046.Icon(selection, leaf[IconDonorId]);

            root.AddFeatures = new[] {
                ArchetypeBatch01445.Entry(1, mysteries),
                ArchetypeBatch01445.Entry(4, selection), ArchetypeBatch01445.Entry(6, selection),
                ArchetypeBatch01445.Entry(12, selection), ArchetypeBatch01445.Entry(14, selection),
                ArchetypeBatch01445.Entry(20, selection),
            };
            root.RemoveFeatures = Removals.Select(r =>
                ArchetypeBatch01445.Entry(r.Level, cache.Load(BlueprintGuid.Parse(r.Guid)) as BlueprintFeatureBase)).ToArray();
            if (root.RemoveFeatures.Any(x => x == null)) return Reject("reject: a removal entry resolved to null");

            SpellBatch01447.PublishArchetype(cache, f, cls, root, new BlueprintFeatureBase[] { mysteries, selection });
            GateReport = "published: auto-read 18 spells / 16 revelations / " + ProviderReport;
            return true;
        }

        // ------------------------------------------------------------------
        // Automatic discovery from Time Mystery + Ancestors Mystery
        // ------------------------------------------------------------------
        static bool DiscoverMysterySpells(BlueprintCharacterClass oracleClass, BlueprintFeature timeMystery,
            BlueprintFeature ancestorsMystery, out BlueprintAbility[][] resolved, out string error)
        {
            resolved = new BlueprintAbility[10][];
            var timeByCircle = ReadMysterySpellTable(oracleClass, timeMystery);
            var ancByCircle = ReadMysterySpellTable(oracleClass, ancestorsMystery);
            for (int circle = 1; circle <= 9; circle++)
            {
                if (!timeByCircle.TryGetValue(circle, out var tSpell) || !ValidSpell(tSpell))
                { error = "reject: discovered Time Mystery circle " + circle + " spell missing or invalid"; return false; }
                if (!ancByCircle.TryGetValue(circle, out var aSpell) || !ValidSpell(aSpell))
                { error = "reject: discovered Ancestors Mystery circle " + circle + " spell missing or invalid"; return false; }
                resolved[circle] = new[] { tSpell, aSpell };
            }
            error = null;
            return true;
        }

        static Dictionary<int, BlueprintAbility> ReadMysterySpellTable(BlueprintCharacterClass oracleClass, BlueprintFeature mystery)
        {
            var dict = new Dictionary<int, BlueprintAbility>();
            var grants = mystery.ComponentsArray?.OfType<AddFeatureOnClassLevel>()
                .Where(g => g.Level == 2 && (g.Feature != null))
                .Select(g => g.Feature)
                .ToArray() ?? Array.Empty<BlueprintFeature>();
            foreach (var grant in grants)
            {
                foreach (var ak in grant.ComponentsArray?.OfType<AddKnownSpell>() ?? Array.Empty<AddKnownSpell>())
                {
                    var spellCls = (Get(ak, "m_CharacterClass") as BlueprintCharacterClassReference)?.Get();
                    if (spellCls != null && spellCls.AssetGuid != oracleClass.AssetGuid) continue;
                    var spell = (Get(ak, "m_Spell") as BlueprintAbilityReference)?.Get();
                    if (spell != null && ak.SpellLevel >= 1 && ak.SpellLevel <= 9 && !dict.ContainsKey(ak.SpellLevel))
                        dict[ak.SpellLevel] = spell;
                }
            }
            return dict;
        }

        static bool DiscoverMysteryRevelations(BlueprintFeatureSelection oracleSelection, BlueprintFeature timeMystery,
            BlueprintFeature ancestorsMystery, out BlueprintFeature[] revelations, out string error)
        {
            var allRefs = (Get(oracleSelection, "m_AllFeatures") as BlueprintFeatureReference[]) ?? Array.Empty<BlueprintFeatureReference>();
            if (allRefs.Length == 0)
                allRefs = (Get(oracleSelection, "m_Features") as BlueprintFeatureReference[]) ?? Array.Empty<BlueprintFeatureReference>();

            var seen = new HashSet<BlueprintGuid>();
            var list = new List<BlueprintFeature>();
            foreach (var r in allRefs)
            {
                var feat = r?.Get();
                if (feat == null || seen.Contains(feat.AssetGuid)) continue;
                var prereqs = feat.ComponentsArray?.OfType<PrerequisiteFeaturesFromList>().ToArray() ?? Array.Empty<PrerequisiteFeaturesFromList>();
                bool matches = prereqs.Any(p =>
                    ((Get(p, "m_Features") as BlueprintFeatureReference[]) ?? Array.Empty<BlueprintFeatureReference>())
                    .Any(mf => mf != null && (mf.Guid == timeMystery.AssetGuid || mf.Guid == ancestorsMystery.AssetGuid)));
                if (matches)
                {
                    seen.Add(feat.AssetGuid);
                    list.Add(feat);
                }
            }

            if (list.Count != 16)
            {
                revelations = null;
                error = "reject: expected 16 auto-discovered Time/Ancestors revelations in OracleRevelationSelection, found " + list.Count;
                return false;
            }
            revelations = list.ToArray();
            error = null;
            return true;
        }

        // ------------------------------------------------------------------
        // Generic Oracle -> Speaker subgraph deep-cloner + A3 D-fix adapters
        // ------------------------------------------------------------------
        sealed class RevelationCloner
        {
            readonly PsychicAssets f;
            readonly BlueprintsCache cache;
            readonly BlueprintCharacterClass shaman;
            readonly BlueprintArchetype speaker;
            readonly BlueprintCharacterClass oracle;
            readonly BlueprintCharacterClass arcanist;
            readonly RevelationBinding0046 bind;
            readonly HashSet<BlueprintGuid> ignoredMysteries;
            readonly BlueprintUnitFact iconDonor;
            readonly BlueprintBuff transformationBuff;
            readonly Dictionary<BlueprintGuid, SimpleBlueprint> clonedMap = new Dictionary<BlueprintGuid, SimpleBlueprint>();

            int seq;

            internal RevelationCloner(PsychicAssets f, BlueprintsCache cache, BlueprintCharacterClass shaman,
                BlueprintArchetype speaker, BlueprintCharacterClass oracle, BlueprintCharacterClass arcanist,
                BlueprintFeature timeMystery, BlueprintFeature ancestorsMystery, BlueprintUnitFact iconDonor)
            {
                this.f = f;
                this.cache = cache;
                this.shaman = shaman;
                this.speaker = speaker;
                this.oracle = oracle;
                this.arcanist = arcanist;
                bind = RevelationBridge0046.ForSpeaker(shaman, speaker);
                ignoredMysteries = new HashSet<BlueprintGuid> { timeMystery.AssetGuid, ancestorsMystery.AssetGuid };
                this.iconDonor = iconDonor;
                transformationBuff = cache.Load(BlueprintGuid.Parse(TransformationBuffId)) as BlueprintBuff;
            }

            internal BlueprintFeature Adapt(BlueprintFeature src)
            {
                string stem = ExtractStem(src);
                string featureToken = stem + "ShamanRevelationFeature0046";
                string resToken = stem + "ShamanRevelationResource0046";
                string displayName = FallbackName(src, stem);
                string displayDesc = FallbackDesc(src, displayName);

                var feat = f.Clone(src, featureToken, displayName, displayDesc);
                CopyIconOrDonor(src, feat);
                feat.Groups = Array.Empty<FeatureGroup>();
                feat.Ranks = 1;
                feat.IsClassFeature = true;
                feat.HideInUI = false;
                feat.HideInCharacterSheetAndLevelUp = false;
                clonedMap[src.AssetGuid] = feat;

                bool isSacredCouncil = src.AssetGuid == BlueprintGuid.Parse(SacredCouncilId);
                bool isBloodOfHeroes = src.AssetGuid == BlueprintGuid.Parse(BloodOfHeroesId);
                bool isSpiritOfWarrior = src.AssetGuid == BlueprintGuid.Parse(SpiritOfTheWarriorId);

                BlueprintAbilityResource primaryResource = null;
                foreach (var aarPre in (feat.ComponentsArray ?? Array.Empty<BlueprintComponent>()).OfType<AddAbilityResources>())
                {
                    var preRes = (Get(aarPre, "m_Resource") as BlueprintAbilityResourceReference)?.Get();
                    if (preRes != null)
                    {
                        var clonedPre = CloneResource(preRes, resToken);
                        if (primaryResource == null) primaryResource = clonedPre;
                    }
                }
                var nextComps = new List<BlueprintComponent>();

                foreach (var comp in feat.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (comp == null || comp is PrerequisiteFeaturesFromList) continue;

                    if (comp is PrerequisiteClassLevel pcl)
                    {
                        var pCls = (Get(pcl, "m_CharacterClass") as BlueprintCharacterClassReference)?.Get();
                        if (pCls == null || pCls.AssetGuid == oracle.AssetGuid)
                            nextComps.Add(RevelationBridge0046.Level(bind, pcl.Level));
                        else
                            nextComps.Add(pcl);
                        continue;
                    }

                    if (comp is AddAbilityResources aar)
                    {
                        var srcRes = (Get(aar, "m_Resource") as BlueprintAbilityResourceReference)?.Get();
                        if (srcRes != null)
                        {
                            var clonedRes = CloneResource(srcRes, resToken);
                            if (primaryResource == null) primaryResource = clonedRes;
                            Set(aar, "m_Resource", Ref<BlueprintAbilityResourceReference>(clonedRes));
                        }
                        nextComps.Add(aar);
                        continue;
                    }

                    if (isSacredCouncil && comp is AddFacts)
                        continue; // Replaced below by the 5-variant SacredCouncil adapter.

                    RebindObjectGraph(comp, featureToken, new HashSet<object>(IdentityComparer));
                    nextComps.Add(comp);
                }

                if (isSacredCouncil)
                {
                    var councilParent = SacredCouncilVariants(f, bind, featureToken, displayName, displayDesc, primaryResource, iconDonor);
                    nextComps.Add(Facts(councilParent));
                }

                feat.ComponentsArray = nextComps.ToArray();

                if (isBloodOfHeroes)
                    ApplyBloodOfHeroesAdapter(feat);
                if (isSpiritOfWarrior)
                    ApplySpiritOfTheWarriorAdapter(feat);

                return feat;
            }

            BlueprintAbilityResource CloneResource(BlueprintAbilityResource srcRes, string preferredToken)
            {
                if (clonedMap.TryGetValue(srcRes.AssetGuid, out var existing))
                    return (BlueprintAbilityResource)existing;

                string token = !f.Assets.ContainsKey(preferredToken) ? preferredToken : preferredToken + (++seq);
                var res = f.Clone(srcRes, token, "", "");
                var maxAmount = CopyData(Get(srcRes, "m_MaxAmount"));
                RevelationBridge0046.BindResource(maxAmount, bind);
                var stat = Get(maxAmount, "ResourceBonusStat");
                if (stat is StatType st && st == StatType.Charisma)
                    Set(maxAmount, "ResourceBonusStat", StatType.Wisdom);
                Set(res, "m_MaxAmount", maxAmount);
                clonedMap[srcRes.AssetGuid] = res;
                return res;
            }

            SimpleBlueprint CloneChild(SimpleBlueprint src, string ownerToken, string roleHint = null)
            {
                if (src == null) return null;
                if (clonedMap.TryGetValue(src.AssetGuid, out var existing)) return existing;
                if (ShouldPreserveReference(src)) return src;

                string token = ChooseChildToken(src, ownerToken, roleHint);
                SimpleBlueprint copy;
                if (src is BlueprintAbilityResource resSrc)
                    return CloneResource(resSrc, token);
                if (src is BlueprintUnitFact factSrc)
                {
                    string n = PeekText(factSrc);
                    string d = PeekText(factSrc, true);
                    copy = f.Clone(factSrc, token, n ?? token, d ?? n ?? token);
                    if (n == null)
                    {
                        var origName = Get(factSrc, "m_DisplayName");
                        var origDesc = Get(factSrc, "m_Description");
                        if (origName != null) Set(copy, "m_DisplayName", origName);
                        if (origDesc != null) Set(copy, "m_Description", origDesc);
                    }
                    CopyIconOrDonor(factSrc, (BlueprintUnitFact)copy);
                    if (copy is BlueprintAbility ab) EnsureAbilityRebound(ab);
                }
                else
                {
                    return src;
                }

                clonedMap[src.AssetGuid] = copy;

                if (copy is BlueprintScriptableObject sbo)
                {
                    var outComps = new List<BlueprintComponent>();
                    foreach (var comp in sbo.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                    {
                        if (comp == null) continue;
                        if (comp is PrerequisiteClassLevel pcl)
                        {
                            var pCls = (Get(pcl, "m_CharacterClass") as BlueprintCharacterClassReference)?.Get();
                            if (pCls == null || pCls.AssetGuid == oracle.AssetGuid)
                            { outComps.Add(RevelationBridge0046.Level(bind, pcl.Level)); continue; }
                        }
                        RebindObjectGraph(comp, ownerToken, new HashSet<object>(IdentityComparer));
                        outComps.Add(comp);
                    }
                    if (copy is BlueprintAbility ability)
                    {
                        bool hasParamCalc = outComps.Any(c =>
                            c is ContextCalculateAbilityParamsBasedOnClass ||
                            c.GetType().Name.StartsWith("ContextCalculateAbilityParams", StringComparison.Ordinal));
                        bool needsParams = ability.Parent == null || outComps.Any(c => c is AbilityEffectRunAction);
                        if (!hasParamCalc && needsParams)
                            outComps.Add(RevelationBridge0046.Params(bind.PrimaryClass, bind.CastingStat));
                    }
                    sbo.ComponentsArray = outComps.ToArray();
                    RebindObjectGraph(sbo, ownerToken, new HashSet<object>(IdentityComparer), skipComponents: true);
                }

                return copy;
            }

            void EnsureAbilityRebound(BlueprintAbility ab)
            {
                if (ab.GetComponent<AbilityResourceLogic>() != null)
                    ab.AvailableMetamagic = 0;
            }

            bool ShouldPreserveReference(SimpleBlueprint src)
            {
                if (src == null || SharedVanillaLeafIds.Contains(src.AssetGuid)) return true;
                if (ignoredMysteries.Contains(src.AssetGuid)) return true;
                if (src is BlueprintCharacterClass || src is BlueprintArchetype || src is BlueprintFeatureSelection ||
                    src is BlueprintSpellbook || src is BlueprintSpellList || src is BlueprintItemWeapon ||
                    src is BlueprintItemEnchantment)
                    return true;
                if (src is BlueprintFeature bf && bf.Groups != null &&
                    (bf.Groups.Contains(FeatureGroup.OracleMystery) ||
                     bf.Groups.Contains(FeatureGroup.EnlightenedPhilosopherMystery) ||
                     bf.Groups.Contains(FeatureGroup.DivineHerbalistMystery)))
                    return true;
                // Only clone revelation-owned features, abilities, buffs, toggles, and resources.
                if (!(src is BlueprintFeature || src is BlueprintAbility || src is BlueprintBuff ||
                      src is BlueprintActivatableAbility || src is BlueprintAbilityResource))
                    return true;
                string n = src.name ?? "";
                return !(n.StartsWith("CotWWrathPsychic", StringComparison.Ordinal) ||
                         n.StartsWith("OracleRevelation", StringComparison.Ordinal));
            }

            string ChooseChildToken(SimpleBlueprint src, string ownerToken, string roleHint)
            {
                if (src is BlueprintAbilityResource)
                {
                    string resTok = ownerToken.Replace("ShamanRevelationFeature0046", "ShamanRevelationResource0046");
                    if (!f.Assets.ContainsKey(resTok)) return resTok;
                }
                if (src is BlueprintBuff && (roleHint == "Buff" || (src.name != null && src.name.EndsWith("Buff", StringComparison.Ordinal))))
                {
                    string buffTok = ownerToken + "Buff";
                    if (!f.Assets.ContainsKey(buffTok)) return buffTok;
                }
                return ownerToken + "Node" + (++seq);
            }

            void RebindObjectGraph(object obj, string ownerToken, HashSet<object> visited, bool skipComponents = false)
            {
                if (obj == null || !visited.Add(obj)) return;

                if (obj is Array arr)
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var elem = arr.GetValue(i);
                        if (elem is BlueprintReferenceBase rb)
                        {
                            var replaced = RebindReference(rb, ownerToken, visited);
                            if (replaced != null && !ReferenceEquals(replaced, rb)) arr.SetValue(replaced, i);
                        }
                        else if (ShouldRecurse(elem))
                        {
                            RebindObjectGraph(elem, ownerToken, visited);
                        }
                    }
                    return;
                }

                if (obj is IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var elem = list[i];
                        if (elem is BlueprintReferenceBase rb)
                        {
                            var replaced = RebindReference(rb, ownerToken, visited);
                            if (replaced != null && !ReferenceEquals(replaced, rb)) list[i] = replaced;
                        }
                        else if (ShouldRecurse(elem))
                        {
                            RebindObjectGraph(elem, ownerToken, visited);
                        }
                    }
                    return;
                }

                if (obj is ContextRankConfig rank)
                    RebindContextRankConfig(rank);
                else if (obj is ContextCalculateAbilityParamsBasedOnClass calc)
                    RebindAbilityParams(calc);
                else if (obj is RecalculateOnStatChange recalc && recalc.Stat == StatType.Charisma)
                    recalc.Stat = StatType.Wisdom;
                else if (obj is AddFeatureOnClassLevel af)
                {
                    Set(af, "m_Class", Ref<BlueprintCharacterClassReference>(shaman));
                    Set(af, "m_AdditionalClasses", Array.Empty<BlueprintCharacterClassReference>());
                    Set(af, "m_Archetypes", new[] { Ref<BlueprintArchetypeReference>(speaker) });
                }

                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                for (var t = obj.GetType(); t != null && t != typeof(object) && t != typeof(SimpleBlueprint) && t != typeof(BlueprintScriptableObject); t = t.BaseType)
                {
                    foreach (var field in t.GetFields(flags))
                    {
                        if (field.IsNotSerialized || field.DeclaringType != t) continue;
                        if (skipComponents && (field.Name == "ComponentsArray" || field.Name == "Components" || field.Name == "m_AllElements"))
                            continue;
                        var val = field.GetValue(obj);
                        if (val == null) continue;
                        if (val is BlueprintReferenceBase rb)
                        {
                            var replaced = RebindReference(rb, ownerToken, visited, field.Name);
                            if (replaced != null && !ReferenceEquals(replaced, rb)) field.SetValue(obj, replaced);
                        }
                        else if (ShouldRecurse(val))
                        {
                            RebindObjectGraph(val, ownerToken, visited);
                        }
                    }
                }
            }

            BlueprintReferenceBase RebindReference(BlueprintReferenceBase rb, string ownerToken, HashSet<object> visited, string fieldName = null)
            {
                var target = ResolveRef(rb);
                if (target == null) return rb;

                if (target.AssetGuid == oracle.AssetGuid || (arcanist != null && target.AssetGuid == arcanist.AssetGuid))
                    return CreateTypedRef(rb.GetType(), shaman);
                if (target is BlueprintArchetype archTarget && archTarget.AssetGuid != speaker.AssetGuid)
                    return CreateTypedRef(rb.GetType(), speaker);

                if (clonedMap.TryGetValue(target.AssetGuid, out var alreadyCloned) && alreadyCloned != null && alreadyCloned.AssetGuid != target.AssetGuid)
                    return CreateTypedRef(rb.GetType(), alreadyCloned);

                if (ShouldPreserveReference(target)) return rb;

                var cloned = CloneChild(target, ownerToken, fieldName == "m_Buff" ? "Buff" : null);
                if (cloned != null && cloned.AssetGuid != target.AssetGuid)
                    return CreateTypedRef(rb.GetType(), cloned);
                return rb;
            }

            SimpleBlueprint ResolveRef(BlueprintReferenceBase rb)
            {
                var cached = AccessTools.Property(typeof(BlueprintReferenceBase), "Cached")?.GetValue(rb, null) as SimpleBlueprint;
                if (cached != null) return cached;
                if (rb.Guid == BlueprintGuid.Empty) return null;
                return cache.Load(rb.Guid);
            }

            static BlueprintReferenceBase CreateTypedRef(Type refType, SimpleBlueprint target)
            {
                var next = (BlueprintReferenceBase)Activator.CreateInstance(refType);
                AccessTools.Property(typeof(BlueprintReferenceBase), "Cached").SetValue(next, target, null);
                AccessTools.Field(typeof(BlueprintReferenceBase), "guid").SetValue(next, target.AssetGuid.ToString());
                return next;
            }

            static bool ShouldRecurse(object val)
            {
                if (val == null) return false;
                var type = val.GetType();
                if (type.IsPrimitive || type.IsEnum || val is string || val is decimal || val is DateTime ||
                    val is TimeSpan || val is Guid || val is SimpleBlueprint || val is UnityEngine.Object ||
                    val is Type || val is Delegate || val is BlueprintGuid)
                    return false;
                return true;
            }

            void RebindContextRankConfig(ContextRankConfig rank)
            {
                var baseType = Get(rank, "m_BaseValueType")?.ToString();
                if (baseType == "StatBonus")
                {
                    if (Get(rank, "m_Stat") is StatType st && st == StatType.Charisma)
                        Set(rank, "m_Stat", StatType.Wisdom);
                    return;
                }
                if (baseType == "ClassLevel" || baseType == "SummClassLevelWithArchetype")
                {
                    SetEnum(rank, "m_BaseValueType", "SummClassLevelWithArchetype");
                    Set(rank, "m_Class", new[] { Ref<BlueprintCharacterClassReference>(shaman) });
                    Set(rank, "Archetype", Ref<BlueprintArchetypeReference>(speaker));
                    Set(rank, "m_AdditionalArchetypes", new[] { Ref<BlueprintArchetypeReference>(speaker) });
                }
            }

            void RebindAbilityParams(ContextCalculateAbilityParamsBasedOnClass calc)
            {
                Set(calc, "m_CharacterClass", Ref<BlueprintCharacterClassReference>(shaman));
                Set(calc, "StatType", StatType.Wisdom);
            }

            // --------------------------------------------------------------
            // A3 Surgical D-review adapters (Speaker clones only; native
            // Oracle Ancestors blueprints remain completely unmodified).
            // --------------------------------------------------------------
            void ApplyBloodOfHeroesAdapter(BlueprintFeature feat)
            {
                foreach (var buff in CollectReachableBuffs(feat))
                {
                    var comps = (buff.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                        .Where(c => c != null &&
                            !(c is SavingThrowContextBonusAgainstDescriptor) &&
                            !(c is AddContextStatBonus acsb && acsb.Stat == StatType.SaveWill) &&
                            !(c is AddStatBonus asb && asb.Stat == StatType.SaveWill))
                        .ToList();
                    comps.Add(new SpeakerFearWillBonus0046 {
                        name = "Speaker0046FearWill",
                        ShamanClass = Ref<BlueprintCharacterClassReference>(shaman),
                        Archetype = Ref<BlueprintArchetypeReference>(speaker)
                    });
                    buff.Stacking = StackingType.Replace;
                    buff.ComponentsArray = comps.ToArray();
                }
            }

            void ApplySpiritOfTheWarriorAdapter(BlueprintFeature feat)
            {
                foreach (var toggle in CollectReachableToggles(feat))
                {
                    toggle.ActivationType = AbilityActivationType.WithUnitCommand;
                    Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Standard);
                    var comps = (toggle.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Where(c => c != null).ToList();
                    if (transformationBuff != null && !comps.Any(c => c is RestrictionHasFact))
                    {
                        var restriction = new RestrictionHasFact { name = "Speaker0046WarriorTransformRestriction", Not = true };
                        Set(restriction, "m_Feature", Ref<BlueprintUnitFactReference>(transformationBuff));
                        comps.Add(restriction);
                    }
                    toggle.ComponentsArray = comps.ToArray();
                }

                foreach (var buff in CollectReachableBuffs(feat))
                {
                    var comps = (buff.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                        .Where(c => c != null &&
                            !(c is AddContextStatBonus acsb && (acsb.Stat == StatType.BaseAttackBonus || acsb.Stat == StatType.SaveFortitude)) &&
                            !(c is AddStatBonus asb && (asb.Stat == StatType.BaseAttackBonus || asb.Stat == StatType.SaveFortitude)) &&
                            !(c is RecalculateOnStatChange r && r.Stat == StatType.BaseAttackBonus) &&
                            !(c is ContextRankConfig))
                        .ToList();
                    if (!comps.Any(c => c is SpeakerWarriorBab0046))
                        comps.Add(new SpeakerWarriorBab0046 {
                            name = "Speaker0046WarriorBab",
                            ShamanClass = Ref<BlueprintCharacterClassReference>(shaman),
                            Archetype = Ref<BlueprintArchetypeReference>(speaker)
                        });
                    if (transformationBuff != null && !comps.Any(c => c is SpeakerBuffMutex0046))
                        comps.Add(new SpeakerBuffMutex0046 {
                            name = "Speaker0046WarriorTransformMutex",
                            IncompatibleBuff = Ref<BlueprintBuffReference>(transformationBuff)
                        });
                    buff.ComponentsArray = comps.ToArray();
                }
            }

            BlueprintBuff[] CollectReachableBuffs(BlueprintFeature rootFeat)
            {
                var buffs = new HashSet<BlueprintBuff>();
                var owned = new HashSet<SimpleBlueprint>(clonedMap.Values);
                var seen = new HashSet<object>(IdentityComparer);
                CollectAssets<BlueprintBuff>(rootFeat, buffs, seen, owned, allowBlueprint: true);
                return buffs.ToArray();
            }

            BlueprintActivatableAbility[] CollectReachableToggles(BlueprintFeature rootFeat)
            {
                var toggles = new HashSet<BlueprintActivatableAbility>();
                var owned = new HashSet<SimpleBlueprint>(clonedMap.Values);
                var seen = new HashSet<object>(IdentityComparer);
                CollectAssets<BlueprintActivatableAbility>(rootFeat, toggles, seen, owned, allowBlueprint: true);
                return toggles.ToArray();
            }

            void CollectAssets<T>(object node, HashSet<T> found, HashSet<object> seen,
                HashSet<SimpleBlueprint> owned, bool allowBlueprint = false) where T : SimpleBlueprint
            {
                if (node == null || !seen.Add(node)) return;
                if (node is T match) found.Add(match);
                if (node is BlueprintReferenceBase rb)
                {
                    var target = ResolveRef(rb);
                    if (target != null && !SharedVanillaLeafIds.Contains(target.AssetGuid) && owned.Contains(target))
                        CollectAssets(target, found, seen, owned, allowBlueprint: true);
                    return;
                }
                if (node is IEnumerable seq && !(node is string))
                {
                    foreach (var item in seq) CollectAssets(item, found, seen, owned);
                    return;
                }
                if (!(allowBlueprint && node is SimpleBlueprint) && !ShouldRecurse(node)) return;
                if (node is BlueprintScriptableObject sbo)
                    CollectAssets(sbo.ComponentsArray, found, seen, owned);
                for (var t = node.GetType(); t != null && t != typeof(object); t = t.BaseType)
                {
                    foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (field.IsNotSerialized || field.DeclaringType != t) continue;
                        if (typeof(BlueprintReferenceBase).IsAssignableFrom(field.FieldType) ||
                            typeof(IEnumerable).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(string) ||
                            !field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                        {
                            CollectAssets(field.GetValue(node), found, seen, owned);
                        }
                    }
                }
            }

            void CopyIconOrDonor(BlueprintUnitFact src, BlueprintUnitFact target)
            {
                var icon = Get(src, "m_Icon");
                if (icon != null) Set(target, "m_Icon", icon);
                else RevelationBridge0046.Icon(target, iconDonor);
            }

            static string ExtractStem(BlueprintFeature src)
            {
                string n = src.name ?? "";
                if (n.StartsWith("CotWWrathPsychic", StringComparison.Ordinal))
                    n = n.Substring("CotWWrathPsychic".Length);
                if (n.StartsWith("OracleRevelation", StringComparison.Ordinal))
                    n = n.Substring("OracleRevelation".Length);
                if (n.EndsWith("OracleRevelation0046", StringComparison.Ordinal))
                    n = n.Substring(0, n.Length - "OracleRevelation0046".Length);
                return string.IsNullOrWhiteSpace(n) ? "Revelation" + src.AssetGuid.ToString().Substring(0, 6) : n;
            }

            static string FallbackName(BlueprintFeature src, string stem)
            {
                string peeked = PeekText(src);
                if (!string.IsNullOrWhiteSpace(peeked)) return peeked;
                switch (stem)
                {
                    case "BloodOfHeroes": return "Blood of Heroes";
                    case "PhantomTouch": return "Phantom Touch";
                    case "SacredCouncil": return "Sacred Council";
                    case "SpiritOfTheWarrior": return "Spirit of the Warrior";
                    case "SpiritShield": return "Spirit Shield";
                    case "SpiritWalk": return "Spirit Walk";
                    case "StormOfSouls": return "Storm of Souls";
                    default: return stem;
                }
            }

            static string FallbackDesc(BlueprintFeature src, string name)
            {
                string peeked = PeekText(src, true);
                return !string.IsNullOrWhiteSpace(peeked) ? peeked : name;
            }
        }
    }
}
