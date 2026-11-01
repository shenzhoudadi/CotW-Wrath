using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints.JsonSystem;
using BlueprintCore.Utils;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Utilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    [TypeId("dc9f13c32c605cd996ec13d15a35ee96")]
    public sealed class ChampionFleetChargeLogic : AbilityCustomLogic, IAbilityRestriction
    {
        // Blueprint components are shared. Track each execution separately, without save data.
        private static readonly ConditionalWeakTable<AbilityExecutionContext, UnitAttack> Attacks = new();
        private sealed class Committed { }
        private static readonly ConditionalWeakTable<AbilityExecutionContext, Committed> Executions = new();

        public override bool IsEngageUnit => true;

        public bool IsAbilityRestrictionPassed(AbilityData ability) =>
            ability?.Caster?.Unit != null && ability.Caster.Unit.Buffs.GetBuff(
                BlueprintTool.Get<BlueprintBuff>(Guids.ChampionFleetChargePreparedBuff)) != null;

        public string GetAbilityRestrictionUIText() => "Use Prepare Fleet Charge (a swift action) first.";

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context?.Caster;
            if (caster == null || target?.Unit == null || Executions.TryGetValue(context, out _))
                yield break;
            var prepared = caster.Buffs.GetBuff(BlueprintTool.Get<BlueprintBuff>(Guids.ChampionFleetChargePreparedBuff));
            if (prepared == null) yield break;

            // The outer ability pays the standard action. This child is ticked directly,
            // never queued in UnitCommandController, so it pays no second action cost.
            // ForceFullAttack permits the full sequence even after an ordinary move.
            var attack = new UnitAttack(target.Unit)
            {
                ForceFullAttack = true,
                IsSingleAttack = false,
                IsCharge = false
            };
            attack.IgnoreCooldown();
            attack.Init(caster);
            if (!attack.IsUnitCloseEnough())
                yield break;

            // The native preparation spell pays swift. The attack spell pays standard.
            // Consume only after range validation, once even if this iterator is retried.
            Executions.Add(context, new Committed());
            prepared.Remove();
            Attacks.Add(context, attack);
            try
            {
                // The native weapon command supplies attacks, animations and projectiles.
                // No charging state is applied: Sudden Attack participates normally.
                attack.Start();
                while (!attack.IsFinished)
                {
                    attack.Tick();
                    yield return null;
                }
                while (attack.LaunchedProjectiles.Any(p => p.OnTheWay && !p.IsHitWall))
                    yield return null;
            }
            finally
            {
                Cleanup(context);
            }
        }

        public override void Cleanup(AbilityExecutionContext context)
        {
            if (context == null || !Attacks.TryGetValue(context, out var attack)) return;
            Attacks.Remove(context);
            if (!attack.IsFinished) attack.Interrupt(true);
        }
    }
}
