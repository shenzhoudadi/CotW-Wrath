using System.Linq;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Hierophant
{
    class LegendaryHierophant
    {
        private static readonly string FeatName = "HierophantSupreme";
        private static readonly string DisplayName = "HierophantSupreme.Name";
        private static readonly string Description = "HierophantSupreme.Description";
        private static readonly string EbonsMiracleGuid = "355a9447-47b2-4ffc-8e2d-7759380e63b4";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(LegendaryHierophant));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Hierophant Supreme Power");

            var resource = AbilityResourceConfigurator.New(FeatName + "Resource", Guids.HierophantSupremeResource)
                .SetMaxAmount(new BlueprintAbilityResource.Amount
                {
                    BaseValue = 1,
                    IncreasedByLevel = false,
                    IncreasedByLevelStartPlusDivStep = false,
                    StartingLevel = 0,
                    StartingIncrease = 0,
                    LevelStep = 0,
                    PerStepIncrease = 0,
                    MinClassLevelIncrease = 0,
                    IncreasedByStat = false,
                    ResourceBonusStat = StatType.Charisma
                })
                .SetMax(10)
                .SetUseMax(false)
                .Configure();

            // Preserve legacy blueprints so old saved references still resolve.
            AbilityConfigurator.New(FeatName + "Ability4", Guids.HierophantSupremeAbility4)
                .SetDisplayName("HierophantSupreme8.Name")
                .SetDescription("HierophantSupreme8.Description")
                .SetIcon(AbilityRefs.AngelArmyOfHeaven.Reference.Get().Icon)
                .Configure();

            AbilityConfigurator.New(FeatName + "Ability3", Guids.HierophantSupremeAbility3)
                .SetDisplayName("HierophantSupreme9.Name")
                .SetDescription("HierophantSupreme9.Description")
                .SetIcon("assets/icons/legendaryhierophant.png")
                .Configure();

            AbilityConfigurator.New(FeatName + "Ability2", Guids.HierophantSupremeAbility2)
                .SetDisplayName("HierophantSupreme8.Name")
                .SetDescription("HierophantSupreme8.Description")
                .SetIcon(AbilityRefs.Guidance.Reference.Get().Icon)
                .Configure();

            AbilityConfigurator.New(FeatName + "Ability1", Guids.HierophantSupremeAbility1)
                .SetDisplayName("HierophantSupreme7.Name")
                .SetDescription("HierophantSupreme7.Description")
                .SetIcon("assets/icons/legendaryhierophant1.png")
                .Configure();

            var miracleAbility = AbilityConfigurator.New(FeatName + "MiracleAbility", Guids.HierophantSupremeMiracleAbility)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(FeatureRefs.DomainMastery.Reference.Get()?.Icon)
                .SetType(AbilityType.SpellLike)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .SetNotOffensive(true)
                .SetActionType(UnitCommand.CommandType.Standard)
                .AddSpellComponent(school: SpellSchool.Evocation)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: resource)
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.HierophantSupreme)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon("assets/icons/legendaryhierophant.png")
                .AddAbilityResources(amount: 0, resource: resource, restoreAmount: true, restoreOnLevelUp: false, useThisAsResource: false)
                .AddFacts(new() { miracleAbility })
                .SetRanks(1)
                .Configure();
        }

        internal static void ConfigureOptionalIntegration()
        {
            var ebonsMiracle = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(BlueprintGuid.Parse(EbonsMiracleGuid));
            if (ebonsMiracle == null)
            {
                Logger.Log("EbonsContentMod Miracle blueprint not found; Hierophant Supreme Miracle variants skipped.");
                return;
            }

            var variants = ebonsMiracle.GetComponent<AbilityVariants>();
            if (variants?.m_Variants == null || variants.m_Variants.Length == 0)
            {
                Logger.Log("EbonsContentMod Miracle blueprint has no variants; skipping.");
                return;
            }

            var configurator = AbilityConfigurator.For(Guids.HierophantSupremeMiracleAbility)
                .SetIcon(ebonsMiracle.Icon)
                .SetActionType(ebonsMiracle.ActionType)
                .SetRange(ebonsMiracle.Range)
                .SetCanTargetSelf(ebonsMiracle.CanTargetSelf)
                .SetNotOffensive(ebonsMiracle.NotOffensive)
                .SetShowNameForVariant(ebonsMiracle.ShowNameForVariant)
                .SetAvailableMetamagic(ebonsMiracle.AvailableMetamagic)
                .AddAbilityVariants(variants.m_Variants
                    .Where(r => r?.Get() != null)
                    .Select(r => (Blueprint<BlueprintAbilityReference>)r.Get())
                    .ToList());
            configurator.Configure();

            Logger.Log($"Bound EbonsContentMod Miracle ({variants.m_Variants.Length} variants) to Hierophant Supreme.");
        }
    }
}
