// COTW-0070 Cryptid Scholar. Homebrew-Archetypes Investigator variant keeping
// the provider's spellbook, inspiration pool and odd-level talents, replacing
// poison resistance/immunity and every studied combat/strike grant with
// Intuitive Lore, Opportune Advice and Knowledgeable Strike. Contract
// C-0070-20261112-r1; identities frozen in A-evidence.json.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class CryptidScholar0070Ids
    {
        internal const string ModuleName = "COTW-0070 Cryptid Scholar";
        internal const string ArchetypeToken = "CryptidScholarArchetype0070";
        internal const string LoreToken = "CryptidScholar0070IntuitiveLore";
        internal const string AdviceFeatureToken = "CryptidScholar0070OpportuneAdvice";
        internal const string StrikeFeatureToken = "CryptidScholar0070KnowledgeableStrike";
        internal const string AdviceAbilityToken = "CryptidScholar0070OpportuneAdviceAbility";
        internal const string StrikeAbilityToken = "CryptidScholar0070KnowledgeableStrikeAbility";
        internal const string AdviceBuffToken = "CryptidScholar0070AdviceBuff";
        internal const string StrikeBuffToken = "CryptidScholar0070StrikeBuff";

        internal const string ClassId = "adb9e138bee9ecc4db246b64d563f900";
        internal const string ProgressionId = "5eaea532547c7ce40a84563e15fd3440";
        internal const string OldBookId = "961e6b4edd3c80e4a9fe4435deff6fe7";
        internal const string TalentSelectionId = "da89239cc5859a1478007cd5e6d44f67";
        internal const string PoisonResistanceId = "152ea6d8f279a73489c9dc0815e9e9b1";
        internal const string PoisonImmunityId = "03d1877a5ffcbd34d9501b005434cb4f";
        internal const string StudiedCombatFeatureId = "5a10de57f3d1a584c917f830c32742c9";
        internal const string StudiedStrikeFeatureId = "0d545d60ed6aa1f48acd73c78676b94d";
        internal const string InspirationFeatureId = "78681fd7849fd5c44ac67c0701d459ec";
        internal const string InspirationResourceId = "60b4629bd1e92194382ae6a754f738ac";
        internal const string ShieldOfFaithId = "183d5bb91dea3a1489a6db6c9cb64445";
        internal const string TrueStrikeId = "2c38da66e5a599347ac95b3294acbe00";

        internal static readonly int[] RemoveStudiedCombatLevels = { 4, 6, 8, 10, 12, 14, 16, 18, 20 };
        internal static readonly int[] RetainTalentLevels = { 3, 5, 7, 9, 11, 13, 15, 17, 19 };

        // The original nine study talents; Ranged Study has both a selection
        // root and a derived feature entry.
        internal static readonly string[] ExcludedTalentIds =
        {
            "4f497e31844a1004087cf1b97330ed72", // QuickStudyFeature
            "27ac309cb4aa14241a7d550991262084", // ProlongedStudyFeature
            "56323e0c3d28d9a428d30189e93b5e68", // SappingOffensiveFeature
            "20c725db6c9c7f14d82ee7feb762a068", // SickeningOffensiveFeature
            "80c3fd96e00c8364e8c24139d622c84b", // BlindingStrikeFeature
            "2885cbaf40b531e45b17a06182b5536e", // ConfusingStrikeFeature
            "01e76de8d3649374f8a21f54d4637b44", // StudiedDefenseFeature
            "e3bc416aceb3d8445a89144b8f2d7a95", // TopplingStrikeFeature
            "6643e114543c16c4a921bdd381106f28", // RangedStudySelection
            "f0e7e42950661c6488fe214aaaa2375a"  // RangedStudyFeature
        };

        internal static BlueprintGuid ArchetypeId => Id(ArchetypeToken);
    }

    internal static class CryptidScholar0070
    {
        internal const string ModuleName = CryptidScholar0070Ids.ModuleName;
        internal const string Description = "Cryptid scholars research monsters that lurk secretly at the edge of civilization, developing a deep expertise regarding their anatomy, habits, and ecology. Although competent as lone monster hunters, cryptid scholars excel when they have associates with whom to share their insights.";
        internal static string LastSummary = "Not registered";
        internal static string[] LastMissing = Array.Empty<string>();

        private const string AdviceText = "At 4th level, when the cryptid scholar succeeds at a Knowledge check to identify a monster's special powers or vulnerabilities, he can take a move action to share his insights with his allies. Allies within 30 feet who can hear the cryptid scholar gain a +1 insight bonus to their ACs and on saving throws against extraordinary, supernatural, and spell-like abilities used by creatures of the same type and all the same subtypes as the monster identified. This bonus lasts for a number of rounds equal to the cryptid scholar's Intelligence modifier (minimum 1) or until he uses knowledgeable strike, whichever comes first. This bonus increases by 1 at 8th level and every 4 investigator levels thereafter (to a maximum of +5 at 20th level). A creature cannot benefit from opportune advice regarding more than one specific kind of monster at a time. Once the cryptid scholar has used this ability to provide a bonus against a specific kind of monster, he can't grant a bonus against that same kind of monster again for 24 hours, unless he expends one use of inspiration when taking a move action to use this ability.";
        private const string StrikeText = "At 4th level, the cryptid scholar can direct allies to exploit a monster's weaknesses. If the cryptid scholar ends his opportune advice early as a move action, each ally within 30 feet who can hear the cryptid scholar deals additional damage on its next successful unarmed, natural, or weapon attack against that specific kind of monster within 1 round. The additional damage is 1d6 at 4th level and increases by 1d6 for every 4 investigator levels thereafter (to a maximum of 5d6 at 20th level). The damage of knowledgeable strike is precision damage and is not multiplied on a critical hit; creatures that are immune to sneak attacks are also immune to knowledgeable strike. Ranged attacks gain this additional damage only against a target within 30 feet. The ally must be able to see the target well enough to pick out a vital spot and must be able to reach such a spot. Knowledgeable strike cannot be used against a creature with concealment.";

        private static T Provider<T>(BlueprintsCache cache, string id, string name, List<string> missing) where T : SimpleBlueprint
        {
            var bp = cache.Load(BlueprintGuid.Parse(id));
            if (bp == null) { missing.Add(name + " missing: " + id); return null; }
            if (bp.GetType() != typeof(T) || bp.name != name || bp.AssetGuid != BlueprintGuid.Parse(id))
                throw new InvalidOperationException("Unexpected HomebrewArchetypes identity: " + name + " / " + id);
            return (T)bp;
        }

        private static void RequireNative(BlueprintsCache cache, string id, string label, List<string> missing)
        {
            if (cache.Load(BlueprintGuid.Parse(id)) == null) missing.Add("Native dependency missing: " + label);
        }

        private static bool HasFeature(BlueprintProgression progression, int level, BlueprintFeatureBase feature)
        {
            var entry = progression.LevelEntries.FirstOrDefault(e => e.Level == level);
            return entry != null && entry.Features.Any(f => f.AssetGuid == feature.AssetGuid);
        }

        // Q01. Every real HA root, its actual references and the frozen grant
        // levels must be present before a single private blueprint is published.
        internal static string[] MissingProvider(BlueprintsCache cache)
        {
            var missing = new List<string>();
            if (cache == null) { missing.Add("BlueprintsCache is null"); return missing.ToArray(); }
            var cls = Provider<BlueprintCharacterClass>(cache, CryptidScholar0070Ids.ClassId, "InvestigatorClass", missing);
            var progression = Provider<BlueprintProgression>(cache, CryptidScholar0070Ids.ProgressionId, "InvestigatorProgression", missing);
            var book = Provider<BlueprintSpellbook>(cache, CryptidScholar0070Ids.OldBookId, "InvestigatorSpellbook", missing);
            var talents = Provider<BlueprintFeatureSelection>(cache, CryptidScholar0070Ids.TalentSelectionId, "InvestigatorTalentSelection", missing);
            var poison = Provider<BlueprintFeature>(cache, CryptidScholar0070Ids.PoisonResistanceId, "InvestigatorPoisonResistance", missing);
            var immunity = Provider<BlueprintFeature>(cache, CryptidScholar0070Ids.PoisonImmunityId, "InvestigatorPoisonImmunity", missing);
            var combat = Provider<BlueprintFeature>(cache, CryptidScholar0070Ids.StudiedCombatFeatureId, "InvestigatorStudiedCombatFeature", missing);
            var strike = Provider<BlueprintFeature>(cache, CryptidScholar0070Ids.StudiedStrikeFeatureId, "StudiedStrikeFeature", missing);
            var inspiration = Provider<BlueprintFeature>(cache, CryptidScholar0070Ids.InspirationFeatureId, "InspirationFeature", missing);
            var resource = Provider<BlueprintAbilityResource>(cache, CryptidScholar0070Ids.InspirationResourceId, "InspirationResource", missing);
            RequireNative(cache, CryptidScholar0070Ids.ShieldOfFaithId, "ShieldOfFaith icon donor", missing);
            RequireNative(cache, CryptidScholar0070Ids.TrueStrikeId, "TrueStrike icon donor", missing);
            if (cls != null && progression != null)
            {
                var link = (BlueprintProgressionReference)Get(cls, "m_Progression");
                if (link == null || link.Guid != progression.AssetGuid) missing.Add("Investigator class does not use its expected progression");
                if (book == null || cls.Spellbook == null || cls.Spellbook.AssetGuid != book.AssetGuid)
                    missing.Add("Investigator class lost its provider spellbook identity");
            }
            if (progression != null && poison != null && immunity != null && combat != null && strike != null && talents != null && inspiration != null)
            {
                foreach (int level in new[] { 2, 5, 8 }) if (!HasFeature(progression, level, poison)) missing.Add("Progression level " + level + " lacks poison resistance");
                if (!HasFeature(progression, 11, immunity)) missing.Add("Progression level 11 lacks poison immunity");
                foreach (int level in CryptidScholar0070Ids.RemoveStudiedCombatLevels) if (!HasFeature(progression, level, combat)) missing.Add("Progression level " + level + " lacks studied combat");
                if (!HasFeature(progression, 4, strike)) missing.Add("Progression level 4 lacks studied strike");
                foreach (int level in CryptidScholar0070Ids.RetainTalentLevels) if (!HasFeature(progression, level, talents)) missing.Add("Progression level " + level + " lacks investigator talent");
                if (!HasFeature(progression, 1, inspiration)) missing.Add("Progression level 1 lacks inspiration");
            }
            if (inspiration != null && resource != null)
            {
                var add = inspiration.GetComponent<AddAbilityResources>();
                if (add == null || add.Resource == null || add.Resource.AssetGuid != resource.AssetGuid)
                    missing.Add("InspirationFeature does not grant the real HA InspirationResource");
            }
            return missing.ToArray();
        }

        private static ContextRankConfig StatRank(StatType stat, AbilityRankType type, int min)
        {
            var rank = new ContextRankConfig { name = "CryptidScholar0070" + stat + type };
            Set(rank, "m_Type", type);
            SetEnum(rank, "m_BaseValueType", "StatBonus");
            Set(rank, "m_Stat", stat);
            Set(rank, "m_UseMin", true);
            Set(rank, "m_Min", min);
            SetEnum(rank, "m_Progression", "AsIs");
            return rank;
        }

        internal static bool Register(BlueprintsCache cache)
        {
            var missing = MissingProvider(cache);
            LastMissing = missing;
            if (missing.Length != 0) { LastSummary = "Deferred: " + string.Join("; ", missing); return false; }
            var cls = cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.ClassId)) as BlueprintCharacterClass;
            var progression = cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.ProgressionId)) as BlueprintProgression;
            var poison = cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.PoisonResistanceId)) as BlueprintFeature;
            var immunity = cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.PoisonImmunityId)) as BlueprintFeature;
            var combat = cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.StudiedCombatFeatureId)) as BlueprintFeature;
            var strike = cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.StudiedStrikeFeatureId)) as BlueprintFeature;
            var resource = cache.Load(BlueprintGuid.Parse(CryptidScholar0070Ids.InspirationResourceId)) as BlueprintAbilityResource;

            var existingRoot = cache.Load(Id(CryptidScholar0070Ids.ArchetypeToken)) as BlueprintArchetype;
            if (existingRoot != null)
            {
                Verify(cache, cls, existingRoot);
                LastSummary = "Already registered; variant graph verified.";
                return true;
            }

            var f = new PsychicAssets(cache);
            var lore = f.Feature(CryptidScholar0070Ids.LoreToken, "Intuitive Lore",
                "A cryptid scholar adds his Intelligence modifier to Lore (Nature) and Lore (Religion) checks and his Wisdom modifier to Knowledge (Arcana) and Knowledge (World) checks. These bonuses are untyped and stack with other sources; a negative modifier never reduces the skill.",
                new AddContextStatBonus { name = "CryptidLoreNature", Stat = StatType.SkillLoreNature, Descriptor = ModifierDescriptor.UntypedStackable, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default } },
                new AddContextStatBonus { name = "CryptidLoreReligion", Stat = StatType.SkillLoreReligion, Descriptor = ModifierDescriptor.UntypedStackable, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default } },
                new AddContextStatBonus { name = "CryptidKnowledgeArcana", Stat = StatType.SkillKnowledgeArcana, Descriptor = ModifierDescriptor.UntypedStackable, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.StatBonus } },
                new AddContextStatBonus { name = "CryptidKnowledgeWorld", Stat = StatType.SkillKnowledgeWorld, Descriptor = ModifierDescriptor.UntypedStackable, Multiplier = 1, Value = new ContextValue { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.StatBonus } },
                StatRank(StatType.Intelligence, AbilityRankType.Default, 0),
                StatRank(StatType.Wisdom, AbilityRankType.StatBonus, 0),
                new RecalculateOnStatChange { name = "CryptidRecalculateIntelligence", Stat = StatType.Intelligence },
                new RecalculateOnStatChange { name = "CryptidRecalculateWisdom", Stat = StatType.Wisdom },
                new CryptidScholarLedger0070 { name = "CryptidScholarLedger0070" });
            lore.IsClassFeature = true;

            var adviceBuff = f.Buff(CryptidScholar0070Ids.AdviceBuffToken, "Opportune Advice",
                "Insight bonus to AC against attacks from the identified monster category, and on saving throws against extraordinary, supernatural and spell-like abilities used by creatures of that exact category.",
                new CryptidAdviceBuff0070 { name = "CryptidAdviceRules0070" });
            var strikeBuff = f.Buff(CryptidScholar0070Ids.StrikeBuffToken, "Knowledgeable Strike",
                "The next successful unarmed, natural or weapon attack against the identified monster category deals additional precision damage. Ranged attacks qualify only within 30 feet. The opportunity ends after one qualifying hit or one round.",
                new CryptidStrikeBuff0070 { name = "CryptidStrikeRules0070" });
            // D2-F01/F02: staging must coexist with old tokens. Native Replace
            // would remove another source's live buff during AddBuff, before
            // the grant transaction commits; retirement is explicit on commit.
            adviceBuff.Stacking = StackingType.Stack;
            strikeBuff.Stacking = StackingType.Stack;

            var adviceAction = new CryptidAdviceAction0070
            {
                name = "CryptidAdviceCast0070",
                CharacterClass = Ref<BlueprintCharacterClassReference>(cls),
                Resource = Ref<BlueprintAbilityResourceReference>(resource),
                AdviceBuff = Ref<BlueprintBuffReference>(adviceBuff)
            };
            var adviceAbility = f.Ability(CryptidScholar0070Ids.AdviceAbilityToken, "Opportune Advice",
                AdviceText,
                AbilityType.Extraordinary, UnitCommand.CommandType.Move, AbilityRange.Unlimited,
                StatRank(StatType.Intelligence, AbilityRankType.Default, 1),
                new AudibleAbilityRestriction(),
                new CryptidAdviceTarget0070(),
                Run(adviceAction));
            adviceAbility.CanTargetFriends = false;
            adviceAbility.SpellResistance = false;
            Set(adviceAbility, "m_Icon", Get(f.Native<BlueprintAbility>(CryptidScholar0070Ids.ShieldOfFaithId), "m_Icon"));
            adviceAbility.LocalizedDuration = Text(adviceAbility.name, "duration", "Intelligence modifier rounds (minimum 1); same category 24 hours");

            var strikeAction = new CryptidStrikeAction0070
            {
                name = "CryptidStrikeCast0070",
                AdviceBuff = Ref<BlueprintBuffReference>(adviceBuff),
                StrikeBuff = Ref<BlueprintBuffReference>(strikeBuff)
            };
            var strikeAbility = f.Ability(CryptidScholar0070Ids.StrikeAbilityToken, "Knowledgeable Strike",
                StrikeText,
                AbilityType.Extraordinary, UnitCommand.CommandType.Move, AbilityRange.Personal,
                new AudibleAbilityRestriction(),
                Run(strikeAction));
            strikeAbility.SpellResistance = false;
            Set(strikeAbility, "m_Icon", Get(f.Native<BlueprintAbility>(CryptidScholar0070Ids.TrueStrikeId), "m_Icon"));
            strikeAbility.LocalizedDuration = Text(strikeAbility.name, "duration", "1 round");

            var adviceFeature = f.Feature(CryptidScholar0070Ids.AdviceFeatureToken, "Opportune Advice", AdviceText, Facts(adviceAbility));
            adviceFeature.IsClassFeature = true;
            Set(adviceFeature, "m_Icon", Get(adviceAbility, "m_Icon"));
            var strikeFeature = f.Feature(CryptidScholar0070Ids.StrikeFeatureToken, "Knowledgeable Strike", StrikeText, Facts(strikeAbility));
            strikeFeature.IsClassFeature = true;
            Set(strikeFeature, "m_Icon", Get(strikeAbility, "m_Icon"));

            var add = new[] { Entry(1, lore), Entry(4, adviceFeature, strikeFeature) };
            var remove = new List<LevelEntry>
            {
                Entry(2, poison), Entry(4, combat, strike), Entry(5, poison), Entry(8, poison, combat), Entry(11, immunity)
            };
            foreach (int level in CryptidScholar0070Ids.RemoveStudiedCombatLevels.Where(l => l != 4 && l != 8))
                remove.Add(Entry(level, combat));
            var root = SpellBatch01447.Archetype(f, cls, CryptidScholar0070Ids.ArchetypeToken, "Cryptid Scholar", Description,
                add, remove.OrderBy(e => e.Level).ToArray());
            var ui = MagusBatch01454.Group(lore, adviceFeature, strikeFeature);

            var undo = MagusBatch01454.CaptureClass(cls);
            MagusBatch01454.Publish(ModuleName, f,
                () => MagusBatch01454.ConnectArchetype(cls, root, ui),
                undo,
                () => { MagusBatch01454.VerifyArchetype(cls, root, ui); Verify(cache, cls, root); });
            LastSummary = "Registered Cryptid Scholar variant: Intuitive Lore, Opportune Advice, Knowledgeable Strike; provider spellbook, inspiration and talents retained.";
            return true;
        }

        // Q01/Q02. Structural verification of the published graph, also used on
        // the idempotent re-entry path.
        internal static void Verify(BlueprintsCache cache, BlueprintCharacterClass cls, BlueprintArchetype root)
        {
            var archetypes = (BlueprintArchetypeReference[])Get(cls, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>();
            if (archetypes.Count(r => r?.Guid == root.AssetGuid) != 1) throw new InvalidOperationException("0070 archetype link missing or duplicated.");
            if (root.GetParentClass() != cls) throw new InvalidOperationException("0070 archetype parent class mismatch.");
            if (root.ReplaceSpellbook != null || root.ChangeCasterType || root.ReplaceClassSkills)
                throw new InvalidOperationException("0070 must keep the provider spellbook, caster type and class skills.");
            if (root.AddFeatures.Length != 2 || root.AddFeatures.Select(e => e.Level).OrderBy(l => l).ToArray().SequenceEqual(new[] { 1, 4 }) != true)
                throw new InvalidOperationException("0070 add entries must be exactly levels 1 and 4.");
            var level1 = root.AddFeatures.Single(e => e.Level == 1);
            if (!level1.Features.Any(f => f.AssetGuid == Id(CryptidScholar0070Ids.LoreToken)))
                throw new InvalidOperationException("0070 level 1 must grant Intuitive Lore.");
            var level4 = root.AddFeatures.Single(e => e.Level == 4);
            if (!level4.Features.Any(f => f.AssetGuid == Id(CryptidScholar0070Ids.AdviceFeatureToken)) ||
                !level4.Features.Any(f => f.AssetGuid == Id(CryptidScholar0070Ids.StrikeFeatureToken)))
                throw new InvalidOperationException("0070 level 4 must grant Opportune Advice and Knowledgeable Strike.");
            var expectedRemove = new Dictionary<int, string[]>
            {
                [2] = new[] { CryptidScholar0070Ids.PoisonResistanceId },
                [4] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId, CryptidScholar0070Ids.StudiedStrikeFeatureId },
                [5] = new[] { CryptidScholar0070Ids.PoisonResistanceId },
                [6] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId },
                [8] = new[] { CryptidScholar0070Ids.PoisonResistanceId, CryptidScholar0070Ids.StudiedCombatFeatureId },
                [10] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId },
                [11] = new[] { CryptidScholar0070Ids.PoisonImmunityId },
                [12] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId },
                [14] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId },
                [16] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId },
                [18] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId },
                [20] = new[] { CryptidScholar0070Ids.StudiedCombatFeatureId }
            };
            if (root.RemoveFeatures.Length != expectedRemove.Count)
                throw new InvalidOperationException("0070 remove entries incomplete.");
            foreach (var pair in expectedRemove)
            {
                var entry = root.RemoveFeatures.FirstOrDefault(e => e.Level == pair.Key);
                if (entry == null || pair.Value.Any(id => !entry.Features.Any(f => f.AssetGuid == BlueprintGuid.Parse(id))))
                    throw new InvalidOperationException("0070 remove entry wrong at level " + pair.Key);
            }
            var lore = cache.Load(Id(CryptidScholar0070Ids.LoreToken)) as BlueprintFeature;
            if (lore == null || lore.GetComponent<CryptidScholarLedger0070>() == null)
                throw new InvalidOperationException("0070 Intuitive Lore ledger missing.");
            var advice = cache.Load(Id(CryptidScholar0070Ids.AdviceAbilityToken)) as BlueprintAbility;
            var adviceAction = advice?.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions?.OfType<CryptidAdviceAction0070>().FirstOrDefault();
            if (adviceAction == null || adviceAction.Resource?.Guid != BlueprintGuid.Parse(CryptidScholar0070Ids.InspirationResourceId))
                throw new InvalidOperationException("0070 advice must pay from the real HA inspiration resource.");
            var strike = cache.Load(Id(CryptidScholar0070Ids.StrikeAbilityToken)) as BlueprintAbility;
            if (strike?.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions?.OfType<CryptidStrikeAction0070>().Any() != true)
                throw new InvalidOperationException("0070 strike conversion action missing.");
        }
    }
}