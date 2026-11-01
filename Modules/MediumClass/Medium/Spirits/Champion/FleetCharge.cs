using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.ElementsSystem;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.View.Animation;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Champion
{
    class FleetCharge
    {
        private static readonly string FeatName = "ChampionFleetCharge";
        private static readonly string DisplayName = "ChampionFleetCharge.Name";
        private static readonly string Description = "ChampionFleetCharge.Description";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(FleetCharge));

        public static ActionList CreateActionList(params GameAction[] actions)
        {
            if (actions == null || actions.Length == 1 && actions[0] == null) actions = Array.Empty<GameAction>();
            return new ActionList() { Actions = actions };
        }

        public static void ConfigureEnabled()
        {
            var prepared = BuffConfigurator.New(FeatName + "PreparedBuff", Guids.ChampionFleetChargePreparedBuff)
                .SetDisplayName("ChampionFleetChargePrepare.Name").SetDescription(Description)
                .SetIcon("assets/icons/fleetcharge.png").SetStacking(StackingType.Replace)
                .Configure();
            prepared.FxOnStart = new Kingmaker.ResourceLinks.PrefabLink();
            prepared.FxOnRemove = new Kingmaker.ResourceLinks.PrefabLink();
            var prepare = AbilityConfigurator.New(FeatName + "PrepareAbility", Guids.ChampionFleetChargePrepareAbility)
                .SetDisplayName("ChampionFleetChargePrepare.Name").SetDescription(Description)
                .SetLocalizedDuration("ChampionFleetChargePrepare.Duration").SetLocalizedSavingThrow("ChampionFleetCharge.NoSave")
                .SetIcon("assets/icons/fleetcharge.png").SetType(AbilityType.Supernatural)
                .SetActionType(UnitCommand.CommandType.Swift).SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true).SetCanTargetEnemies(false).SetCanTargetFriends(false).SetCanTargetPoint(false)
                .AddAbilityEffectRunAction(actions: ActionsBuilder.New().ApplyBuff(prepared,
                    ContextDuration.Fixed(1, DurationRate.Rounds), toCaster: true, isFromSpell: false, isNotDispelable: true))
                .Configure();
            var ability = AbilityConfigurator.New(FeatName + "Ability", Guids.ChampionFleetChargeAbility)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetLocalizedDuration("ChampionFleetCharge.Duration").SetLocalizedSavingThrow("ChampionFleetCharge.NoSave")
                .SetType(AbilityType.Supernatural)
                .SetActionType(UnitCommand.CommandType.Standard)
                .SetIsFullRoundAction(false)
                .SetRange(AbilityRange.Weapon)
                .SetCanTargetEnemies()
                .SetCanTargetSelf(false)
                .SetCanTargetPoint(false)
                .SetCanTargetFriends(false)
                .SetIcon("assets/icons/fleetcharge.png")
                .AddComponent<ChampionFleetChargeLogic>()
                .Configure();

            Logger.Log("Generating Champion Fleet Charge");
            FeatureConfigurator.New(FeatName, Guids.ChampionFleetCharge)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddFacts(new() { prepare, ability })
                .AddFactContextActions(deactivated: ActionsBuilder.New().Add<ContextActionRemoveBuff>(c =>
                    c.m_Buff = prepared.ToReference<BlueprintBuffReference>()))
                .Configure();
        }
    }
}
