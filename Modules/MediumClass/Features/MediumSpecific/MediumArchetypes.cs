using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using MediumClass.Medium;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Features.MediumSpecific
{
    internal static class MediumAttributeRules
    {
        private static bool HasFact(UnitDescriptor owner, string guid)
        {
            if (owner == null || string.IsNullOrEmpty(guid)) return false;
            var bp = BlueprintTool.Get<BlueprintFeature>(guid);
            return bp != null && owner.HasFact(bp);
        }

        internal static bool HasDiminishedSpiritBonus(UnitDescriptor owner) =>
            HasFact(owner, Guids.DevotedMediumDiminishedSpiritBonus);

        internal static bool HasPactOfDedication(UnitDescriptor owner) =>
            HasFact(owner, Guids.DevotedMediumPactOfDedication);

        internal static bool HasDevotedSpirits(UnitDescriptor owner) =>
            HasFact(owner, Guids.DevotedMediumDevotedSpirits);

        internal static bool HasScholarlyMeditations(UnitDescriptor owner) =>
            HasFact(owner, Guids.LoremasterScholarlyMeditations);

        internal static bool HasFocusedSpiritualism(UnitDescriptor owner) =>
            HasFact(owner, Guids.LoremasterFocusedSpiritualism);

        internal static StatType CastingStat(UnitDescriptor owner)
        {
            if (HasPactOfDedication(owner)) return StatType.Wisdom;
            if (HasScholarlyMeditations(owner)) return StatType.Intelligence;
            return StatType.Charisma;
        }
    }

    internal static class MediumArchetypes
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumArchetypes));

        internal static void Configure()
        {
            ConfigureDevotedMedium();
            ConfigureLoremaster();
        }

        private static void ConfigureDevotedMedium()
        {
            var spellbook = MediumSpellbook.CreateVariantSpellbook(
                "DevotedMediumSpellbook", Guids.DevotedMediumSpellbook, StatType.Wisdom);

            var diminished = FeatureConfigurator.New("DevotedMediumDiminishedSpiritBonus", Guids.DevotedMediumDiminishedSpiritBonus)
                .SetDisplayName("DevotedMediumDiminishedSpiritBonus.Name")
                .SetDescription("DevotedMediumDiminishedSpiritBonus.Description")
                .SetIcon("assets/icons/tranceofthree.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .Configure();

            var pact = FeatureConfigurator.New("DevotedMediumPactOfDedication", Guids.DevotedMediumPactOfDedication)
                .SetDisplayName("DevotedMediumPactOfDedication.Name")
                .SetDescription("DevotedMediumPactOfDedication.Description")
                .SetIcon("assets/icons/spirithierophant.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .Configure();

            var devotedSpirits = FeatureConfigurator.New("DevotedMediumDevotedSpirits", Guids.DevotedMediumDevotedSpirits)
                .SetDisplayName("DevotedMediumDevotedSpirits.Name")
                .SetDescription("DevotedMediumDevotedSpirits.Description")
                .SetIcon("assets/icons/tranceofthree.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .Configure();

            ArchetypeConfigurator.New("DevotedMediumArchetype", Guids.DevotedMediumArchetype, Guids.Medium)
                .SetLocalizedName("DevotedMediumArchetype.Name")
                .SetLocalizedDescription("DevotedMediumArchetype.Description")
                .SetReplaceSpellbook(spellbook)
                .SetRemoveFeatures(LevelEntryBuilder.New()
                    .AddEntry(10, Guids.MediumTranceOfMany))
                .SetAddFeatures(LevelEntryBuilder.New()
                    .AddEntry(1, diminished, pact, devotedSpirits))
                .Configure();

            Logger.Log("Devoted Medium archetype configured for the Medium class.");
        }

        private static void ConfigureLoremaster()
        {
            var spellbook = MediumSpellbook.CreateVariantSpellbook(
                "LoremasterMediumSpellbook", Guids.LoremasterSpellbook, StatType.Intelligence);

            var meditations = FeatureConfigurator.New("LoremasterScholarlyMeditations", Guids.LoremasterScholarlyMeditations)
                .SetDisplayName("LoremasterScholarlyMeditations.Name")
                .SetDescription("LoremasterScholarlyMeditations.Description")
                .SetIcon("assets/icons/spiritarchmage.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .Configure();

            var focused = FeatureConfigurator.New("LoremasterFocusedSpiritualism", Guids.LoremasterFocusedSpiritualism)
                .SetDisplayName("LoremasterFocusedSpiritualism.Name")
                .SetDescription("LoremasterFocusedSpiritualism.Description")
                .SetIcon("assets/icons/arcanesurge.png")
                .SetIsClassFeature(true)
                .SetRanks(1)
                .Configure();

            ArchetypeConfigurator.New("LoremasterArchetype", Guids.LoremasterArchetype, Guids.Medium)
                .SetLocalizedName("LoremasterArchetype.Name")
                .SetLocalizedDescription("LoremasterArchetype.Description")
                .SetReplaceSpellbook(spellbook)
                .SetRemoveFeatures(LevelEntryBuilder.New()
                    .AddEntry(1, Guids.SpiritSurge)
                    .AddEntry(10, Guids.SpiritSurge)
                    .AddEntry(20, Guids.SpiritSurge))
                .SetAddFeatures(LevelEntryBuilder.New()
                    .AddEntry(1, meditations, focused))
                .Configure();

            Logger.Log("Loremaster archetype configured for the Medium class.");
        }
    }

    [HarmonyPatch(typeof(BlueprintAbilityResource), nameof(BlueprintAbilityResource.GetMaxAmount))]
    internal static class MediumArchetypeEnergyFontResourcePatch
    {
        private static readonly BlueprintGuid EnergyFontResourceId = BlueprintGuid.Parse(Guids.HierophantEnergyFontResource);

        private static void Postfix(BlueprintAbilityResource __instance, UnitDescriptor __0, ref int __result)
        {
            if (__instance == null || __0 == null || __instance.AssetGuid != EnergyFontResourceId) return;
            var statType = MediumAttributeRules.CastingStat(__0);
            if (statType == StatType.Charisma) return;
            var stat = __0.Stats.GetStat(statType) as ModifiableValueAttributeStat;
            __result = Math.Max(0, 1 + (stat?.Bonus ?? 0));
        }
    }

    [HarmonyPatch(typeof(RuleCalculateAbilityParams), nameof(RuleCalculateAbilityParams.OnTrigger))]
    internal static class MediumArchetypeChannelDCPatch
    {
        private static readonly HashSet<BlueprintGuid> EnergyFontAbilities = new HashSet<BlueprintGuid>
        {
            BlueprintGuid.Parse(Guids.HierophantEnergyFontPositiveHeal),
            BlueprintGuid.Parse(Guids.HierophantEnergyFontPositiveHarm),
            BlueprintGuid.Parse(Guids.HierophantEnergyFontNegativeHarm),
            BlueprintGuid.Parse(Guids.HierophantEnergyFontNegativeHeal),
        };

        private static void Prefix(RuleCalculateAbilityParams __instance)
        {
            if (__instance?.Blueprint == null || !EnergyFontAbilities.Contains(__instance.Blueprint.AssetGuid)) return;
            var statType = MediumAttributeRules.CastingStat(__instance.Initiator?.Descriptor);
            if (statType != StatType.Charisma)
                __instance.ReplaceStat = statType;
        }
    }

    [HarmonyPatch(typeof(KeepAlliesAlive), "RefreshAttacksCount")]
    internal static class MediumArchetypeLegendaryGuardianStatPatch
    {
        private static readonly BlueprintGuid LegendaryGuardianId = BlueprintGuid.Parse(Guids.LegendaryGuardian);

        private static void Postfix(KeepAlliesAlive __instance)
        {
            if (__instance?.Fact?.Blueprint?.AssetGuid != LegendaryGuardianId) return;
            var statType = MediumAttributeRules.CastingStat(__instance.Owner?.Descriptor);
            if (statType == StatType.Charisma) return;
            var stat = __instance.Owner?.Stats?.GetStat(statType) as ModifiableValueAttributeStat;
            __instance.Data.LeftAttacksCount = stat?.Bonus ?? 0;
        }
    }
}
