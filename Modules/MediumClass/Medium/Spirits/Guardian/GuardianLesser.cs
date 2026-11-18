using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Guardian
{
    class GuardianLesser
    {
        private static readonly string FeatName = "GuardianLesser";
        private static readonly string DisplayName = "GuardianLesser.Name";
        private static readonly string Description = "GuardianLesser.Description";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(GuardianLesser));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Guardian Lesser Spirit Power");

            var shieldOtherBuff = BuffConfigurator.New(FeatName + "ShieldOtherBuff", Guids.GuardianLesserShieldOtherBuff)
                .SetDisplayName("GuardianShieldOther.Name")
                .SetDescription("GuardianShieldOther.Description")
                .SetIcon("assets/icons/spiritguardian.png")
                .SetFlags(BlueprintBuff.Flags.RemoveOnRest)
                .SetStacking(StackingType.Replace)
                .AddComponent<GuardianShieldOtherComponent>()
                .Configure();

            var shieldOtherToggle = ActivatableAbilityConfigurator.New(FeatName + "ShieldOtherToggle", Guids.GuardianLesserShieldOtherToggle)
                .SetDisplayName("GuardianShieldOther.Name")
                .SetDescription("GuardianShieldOther.Description")
                .SetIcon("assets/icons/spiritguardian.png")
                .SetBuff(shieldOtherBuff)
                .SetActivationType(AbilityActivationType.Immediately)
                .SetActivateWithUnitCommand(UnitCommand.CommandType.Free)
                .SetDeactivateImmediately(true)
                .SetDeactivateIfOwnerDisabled(true)
                .SetDeactivateIfOwnerUnconscious(true)
                .Configure();

            FeatureConfigurator.New(FeatName, Guids.GuardianLesser)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddFacts(new() {
                    FeatureRefs.HeavyArmorProficiency.Reference.Get(),
                    FeatureRefs.ShieldsProficiency.Reference.Get(),
                    FeatureRefs.TowerShieldProficiency.Reference.Get(),
                    shieldOtherToggle
                })
                .Configure();
        }
    }
}
