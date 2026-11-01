using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Controllers.Units;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.TurnBasedMode.Controllers;
using TurnBased.Controllers;
using Newtonsoft.Json;
using Way_of_the_shield.NewFeatsAndAbilities;

namespace Way_of_the_shield.NewComponents
{
    public class UpsettingShieldComponent : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleDealDamage>
    {
        public int checkerRanks = 1;
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            var weapon=evt?.DamageBundle?.Weapon as ItemEntityWeapon ?? evt?.AttackRoll?.Weapon;
            if (TacticalCombatHelper.IsActive || weapon==null ||
                !weapon.IsShield || weapon.Shield?.ArmorComponent?.Blueprint.ProficiencyGroup != ArmorProficiencyGroup.Buckler ||
                evt.Result < 1 || evt.Target == null) return;
            BlueprintBuff effect = Owner.Progression.Features.HasFact(UpsettingShieldStyle.Vengeance) ? UpsettingShieldStyle.VengeanceBuff :
                Owner.Progression.Features.HasFact(UpsettingShieldStyle.Strike) ? UpsettingShieldStyle.StrikeBuff : UpsettingShieldStyle.StyleBuff;
            // Refresh only this caster's penalty. Repeated shield hits cannot stack -2 or refill AoOs.
            foreach (var previous in evt.Target.Buffs.Enumerable.Where(b => b.Context?.MaybeCaster == Owner &&
                (b.Blueprint == UpsettingShieldStyle.StyleBuff || b.Blueprint == UpsettingShieldStyle.StrikeBuff || b.Blueprint == UpsettingShieldStyle.VengeanceBuff)).ToArray()) previous.Remove();
            var duration = CombatController.IsInTurnBasedCombat() ? (TimeSpan?)null : new Rounds(1).Seconds;
            var applied = evt.Target.Descriptor.AddBuff(effect, Owner, duration, null);
            if (applied != null) applied.IsNotDispelable = true;
            if (effect != UpsettingShieldStyle.StyleBuff && !Owner.Buffs.HasFact(UpsettingShieldStyle.checker))
                Owner.Descriptor.AddBuff(UpsettingShieldStyle.checker, Owner, duration, null);
        }
    }
    public sealed class UpsettingRoundData
    {
        [JsonProperty] public HashSet<string> TriggeredTargets = new();
        [JsonProperty] public bool Initialized;
        [JsonProperty] public TimeSpan Expires;
    }
    [TypeId("e8670054108248fda3590910a3ac3e95")]
    public sealed class UpsettingRoundTracker : UnitBuffComponentDelegate<UpsettingRoundData>, ITickEachRound, ITurnBasedModeHandler, IGlobalSubscriber
    {
        protected override void OnActivate()
        {
            Data.TriggeredTargets ??= new HashSet<string>();
            if (!Data.Initialized) { Data.Initialized = true; Data.Expires = Game.Instance.TimeController.GameTime + new Rounds(1).Seconds; }
        }
        public bool TrySpend(UnitEntityData target)
        {
            if (target == null || !Fact.IsActive || !Owner.Buffs.HasFact(UpsettingShieldStyle.MainBuff)) return false;
            if (!CombatController.IsInTurnBasedCombat() && Game.Instance.TimeController.GameTime >= Data.Expires) return false;
            return Data.TriggeredTargets.Add(target.UniqueId);
        }
        public void OnNewRound() { if (!CombatController.IsInTurnBasedCombat() && Game.Instance.TimeController.GameTime >= Data.Expires) Buff.Remove(); }
        public void HandleTurnStarted(UnitEntityData unit) { if (unit == Owner) Buff.Remove(); }
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck check) { }
    }
    public sealed class UpsettingLifetimeData
    {
        [JsonProperty] public bool Initialized;
        [JsonProperty] public TimeSpan Expires;
    }
    [TypeId("0a21dee8f7eb4ddd8029715be5620ada")]
    public sealed class UpsettingPenaltyLifetime : UnitBuffComponentDelegate<UpsettingLifetimeData>, ITickEachRound, ITurnBasedModeHandler, IGlobalSubscriber
    {
        protected override void OnActivate() { if (!Data.Initialized) { Data.Initialized = true; Data.Expires = Game.Instance.TimeController.GameTime + new Rounds(1).Seconds; } }
        public void OnNewRound() { if (!CombatController.IsInTurnBasedCombat() && Game.Instance.TimeController.GameTime >= Data.Expires) Buff.Remove(); }
        public void HandleTurnStarted(UnitEntityData unit) { if (unit == Context?.MaybeCaster) Buff.Remove(); }
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck check) { }
    }
}
