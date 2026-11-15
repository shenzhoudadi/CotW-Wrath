using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium
{
    class Influence
    {
        private static readonly string FeatName = "MediumInfluence";
        internal const string DisplayName = "MediumInfluence.Name";
        private static readonly string Description = "MediumInfluence.Description";

        private static readonly ModLogger Logger = Logging.GetLogger(nameof(Influence));

        public static void ConfigureEnabled()
        {

            var debuff = BuffConfigurator.New(FeatName + "Debuff", Guids.MediumInfluenceDebuff).Configure();
            BuffConfigurator.For(debuff)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddRestTrigger(action: ActionsBuilder.New().RemoveBuff(debuff))
                .SetIcon(BuffRefs.Fatigued.Reference.Get().Icon)
                .AddComponent<MediumInfluencePenaltyComponent>()
                .Configure();

            // Mirror of MediumInfluenceRules.BaseCap: 3 plus half the Medium class
            // level plus the Charisma modifier. InfluenceCapPatch replaces
            // GetMaxAmount for every influence resource, so this block is the
            // fallback for paths that bypass the patch, not the live formula.
            var resource = AbilityResourceConfigurator.New(FeatName + "Resource", Guids.MediumInfluenceResource)
                .SetMaxAmount(ResourceAmountBuilder.New(3)
                    .IncreaseByLevelStartPlusDivStep(classes: new[] { Guids.Medium },
                        otherClassLevelsMultiplier: 0f, startingLevel: 0, startingBonus: 0,
                        levelsPerStep: 2, bonusPerStep: 1, minBonus: 0)
                    .IncreaseByStat(StatType.Charisma))
                .SetUseMax(false)
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.MediumInfluence)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIsClassFeature()
                .SetHideInCharacterSheetAndLevelUp()
                .SetHideInUI()
                .AddAbilityResources(amount: 0, resource: resource, restoreAmount: false, restoreOnLevelUp: false, useThisAsResource: false)
                .AddAbilityResources(amount: 0, resource: Guids.HierophantEnergyFontResource, restoreAmount: true, restoreOnLevelUp: false, useThisAsResource: false)
                .AddAbilityResources(amount: 1, resource: Guids.TricksterLegendaryTricksterResource, restoreAmount: true)
                .AddAbilityResources(amount: 1, restoreAmount: true, resource: Guids.TricksterEdgeResource)
                .Configure();
        }
        
    }
}
