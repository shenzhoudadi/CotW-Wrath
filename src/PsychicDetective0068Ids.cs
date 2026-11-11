using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0068 / S-PSYCHIC-0068. All private identities use the existing
    // deterministic PsychicAssets token namespace; every token is prefixed so
    // the detective graph can never collide with an existing Psychic asset.
    internal static class PsychicDetective0068Ids
    {
        internal const string ModuleName = "COTW-0068 Psychic Detective";

        internal const string ArchetypeToken = "PsychicDetective0068Archetype";
        internal const string BookToken = "PsychicDetective0068Spellbook";
        internal const string ListToken = "PsychicDetective0068SpellList";
        internal const string PoolToken = "PsychicDetective0068PhrenicPool";
        internal const string CastingToken = "PsychicDetective0068Spellcasting";
        internal const string MeddlerToken = "PsychicDetective0068Meddler";
        internal const string DabblerToken = "PsychicDetective0068PhrenicDabbler";
        internal const string AmplificationSelectionToken = "PsychicDetective0068AmplificationSelection";
        internal const string MenuRootToken = "PsychicDetective0068PoolMenu";
        internal const string MenuFeedbackToken = "PsychicDetective0068PsychofeedbackMenu";
        internal const string ExpandedPoolTalentToken = "PsychicDetective0068ExpandedPoolTalent";
        internal const string ExtraAmplificationTalentToken = "PsychicDetective0068ExtraAmplificationTalent";

        internal const string ClassId = ExtraInvestigatorTalent.ClassId;
        internal const string ProgressionId = ExtraInvestigatorTalent.ProgressionId;
        internal const string TalentSelectionId = ExtraInvestigatorTalent.TalentSelectionId;
        internal const string PoisonResistanceId = "152ea6d8f279a73489c9dc0815e9e9b1";
        internal const string PoisonImmunityId = "03d1877a5ffcbd34d9501b005434cb4f";
        internal const string AlchemistDiscoveryId = "f68b959943348034d8469c165442e019";
        internal const string DetectMagicId = "ee0b69e90bac14446a4cf9a050f87f2e";
        internal const string BardBookId = "bc04fc157a8801d41b877ad0d9af03dd";
        internal const string BardSpellsPerDayId = "0a8eec9ca5c0dc64795243ab3c55d924";
        internal const string BardSpellsKnownId = "90de6b4f591d1184497fbfbae6e16547";
        internal const string FindTrapsId = "4709274b2080b6444a3c11c6ebbe2404";
        internal const string BanishmentId = "d361391f645db984bbf58907711a146a";
        internal const string GreaterStunningBarrierId = "9e33ed477ddd4fad9d90fb64b1a6f732";

        // The four native discovery features are only banned through the real
        // Investigator talent ancestry. Other classes keep their own donors.
        internal static readonly string[] ForbiddenDiscoveryIds = {
            "cee8f65448ce71c4b8b8ca13751dd8ea", // Mutagen
            "2673ccdd6df742d42a8c94977c76a984", // Enhance Potion
            "1db8e126b77e71d4d89dcfa50fe87e93", // Extend Potion
            "57d5077b301ade749b840b0ea9230bb9"  // Infusion
        };

        internal static BlueprintGuid Id(string token) => PsychicAssets.Id(token);
        internal static BlueprintGuid BookId => Id(BookToken);
        internal static BlueprintGuid PoolId => Id(PoolToken);
        internal static BlueprintGuid ArchetypeId => Id(ArchetypeToken);
        internal static BlueprintGuid MenuRootId => Id(MenuRootToken);
        internal static BlueprintGuid MenuFeedbackId => Id(MenuFeedbackToken);

        internal static readonly BlueprintGuid LegacyBookId = BlueprintGuid.Parse(PsychicClass.BookId);

        internal static bool IsDetectiveBook(BlueprintSpellbook book) => book != null && book.AssetGuid == BookId;
        internal static bool IsDetectiveBook(BlueprintGuid id) => id == BookId;

        internal static BlueprintAbilityResource DetectivePool(UnitEntityData unit)
        {
            var resource = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(PoolId);
            return resource != null && unit?.Resources?.ContainsResource(resource) == true ? resource : null;
        }

        // Ten mode-backed kinds plus the eleventh, Psychofeedback, which is a
        // native spell-slot conversion feature rather than a mode buff. No
        // Major amplification, discipline or Phrenic Mastery is ever granted.
        internal static readonly PsychicAmplificationKind[] ModeKinds = {
            PsychicAmplificationKind.BiokineticHealing,
            PsychicAmplificationKind.ConjuredArmor,
            PsychicAmplificationKind.DefensivePrognostication,
            PsychicAmplificationKind.FocusedForce,
            PsychicAmplificationKind.MindsEye,
            PsychicAmplificationKind.OverpoweringMind,
            PsychicAmplificationKind.WillOfTheDead,
            PsychicAmplificationKind.OngoingDefense,
            PsychicAmplificationKind.RelentlessCasting,
            PsychicAmplificationKind.UndercastSurge
        };

        internal static readonly string[] BasicFeatureTokens = {
            "BiokineticHealingFeature",
            "ConjuredArmorFeature",
            "DefensivePrognosticationFeature",
            "FocusedForceFeature",
            "MindsEyeFeature",
            "OverpoweringMindFeature",
            "WillOfTheDeadFeature01458",
            "OngoingDefenseFeature",
            "RelentlessCastingFeature",
            "UndercastSurgeFeature01458",
            "PsychofeedbackFeature"
        };

        internal static bool IsDetectiveFeature(BlueprintScriptableObject bp)
            => bp?.name != null && bp.name.StartsWith("CotWWrathPsychicPsychicDetective0068", StringComparison.Ordinal);
    }
}
