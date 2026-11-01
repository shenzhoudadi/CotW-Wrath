using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Controllers.Units;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using TurnBased.Controllers;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class LinnormOpening
    {
        [JsonProperty, JsonConverter(typeof(ChargeUnitReferenceConverter))] public UnitReference Enemy;
        [JsonProperty] public TimeSpan Expires;
        [JsonProperty] public bool Hit;
    }
    public sealed class LinnormData
    {
        [JsonProperty] public List<LinnormOpening> Openings = new List<LinnormOpening>();
    }

    // Retain the native command, hand, animation and AoO payment path. There is
    // no direct RuleAttackWithWeapon call and no separate action/counter pool.
    internal static class LinnormOpportunity
    {
        static readonly ConditionalWeakTable<UnitAttackOfOpportunity, object> Commands = new ConditionalWeakTable<UnitAttackOfOpportunity, object>();
        static readonly ConditionalWeakTable<RuleAttackWithWeapon, object> Reactions = new ConditionalWeakTable<RuleAttackWithWeapon, object>();
        internal static bool IsReaction(RuleAttackWithWeapon rule)
        {
            if (Reactions.TryGetValue(rule, out _)) return true;
            if (rule.IsAttackOfOpportunity && rule.Initiator?.Commands?.Free is UnitAttackOfOpportunity command && command.Target == rule.Target && Commands.TryGetValue(command, out _)) {
                Reactions.GetValue(rule, _ => new object()); return true;
            }
            return false;
        }
        internal static bool Try(UnitEntityData owner, UnitEntityData enemy, bool adjacent)
        {
            if (owner == null || enemy == null || owner == enemy || owner.IsDisposed || enemy.IsDisposed || owner.State.IsDead || enemy.State.IsDead || !owner.State.CanAct || enemy.IsAlly(owner) || !owner.CanAttack(enemy)) return false;
            var hand = owner.GetThreatHand();
            if (hand?.Weapon?.Blueprint.IsUnarmed != true || !owner.CombatState.CanAttackOfOpportunity) return false;
            var range = owner.GetThreatRange(hand);
            if (!range.HasValue || !UnitHelper.IsAttackOfOpportunityReach(owner, enemy, adjacent ? Math.Min(range.Value, 1.524f) : range.Value)) return false;
            var commands = owner.Commands; var before = new[] { commands.Free }.Concat(commands.Queue).ToArray();
            if (!owner.CombatState.AttackOfOpportunity(enemy)) return false;
            foreach (var command in new[] { commands.Free }.Concat(commands.Queue).OfType<UnitAttackOfOpportunity>())
                if (command.Executor == owner && command.Target == enemy && !before.Any(x => ReferenceEquals(x, command))) Commands.GetValue(command, _ => new object());
            return true;
        }
    }

    [TypeId("b72c424863c0494ab7e119a4cdb3dc02")]
    public sealed class LinnormStyleEffect : UnitBuffComponentDelegate<LinnormData>,
        ITargetRulebookHandler<RuleCalculateAC>, ITargetRulebookHandler<RuleAttackWithWeapon>,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookHandler<RuleAttackRoll>, ITurnBasedModeHandler, ITickEachRound, IGlobalSubscriber
    {
        bool Ready => Buff.IsActive && Buff.IsTurnedOn && !Buff.IsSuppressed && LinnormFeats.Has(Owner, 0);
        void Expire() { if (Data.Openings == null) Data.Openings = new List<LinnormOpening>(); Data.Openings.RemoveAll(x => x.Enemy.Value == null || x.Enemy.Value.State.IsDead || (!CombatController.IsInTurnBasedCombat() && x.Expires <= Game.Instance.TimeController.GameTime)); }
        LinnormOpening Opening(UnitEntityData enemy) { Expire(); return Data.Openings.FirstOrDefault(x => x.Enemy.Value == enemy); }
        public void OnEventAboutToTrigger(RuleCalculateAC evt) { if (Ready && (evt.AttackType == AttackType.Melee || evt.AttackType == AttackType.Touch)) evt.AddModifier(-2, Fact, ModifierDescriptor.UntypedStackable); }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (!Ready || evt.Target != Owner || evt.AttackRoll == null || evt.AttackRoll.IsFake || evt.Weapon?.Blueprint.IsMelee != true || evt.Initiator == null || evt.Initiator.State.IsDead || evt.Initiator.IsAlly(Owner) || LinnormOpportunity.IsReaction(evt)) return;
            var owner = Owner; var data = Data; Expire();
            var opening = data.Openings.FirstOrDefault(x => x.Enemy.Value == evt.Initiator);
            if (opening == null) { opening = new LinnormOpening { Enemy = evt.Initiator }; data.Openings.Add(opening); }
            opening.Hit |= evt.AttackRoll.IsHit; opening.Expires = Game.Instance.TimeController.GameTime + TimeSpan.FromSeconds(6);
            var allowance = LinnormWrathAllowance.For(owner);
            if (evt.AttackRoll.IsHit && allowance != null && (!allowance.Used || !CombatController.IsInTurnBasedCombat() && allowance.Expires <= Game.Instance.TimeController.GameTime) && LinnormOpportunity.Try(owner, evt.Initiator, false)) {
                allowance.Used = true; allowance.Expires = Game.Instance.TimeController.GameTime + TimeSpan.FromSeconds(6);
            }
        }
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt) { if (Ready && evt.Initiator == Owner && evt.Weapon?.Blueprint.IsUnarmed == true && LinnormFeats.Has(Owner, 1) && Opening(evt.Target)?.Hit == true) evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable); }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            // Completed weapon stats contain the actual Strength/Dexterity
            // choice, including Agile and Mythic Weapon Finesse. Replace only
            // that contribution before native damage/critical calculation.
            if (!Ready || evt.Initiator != Owner || evt.IsFake || evt.Weapon?.Blueprint.IsUnarmed != true || Opening(evt.Target) == null) return;
            var stats = evt.WeaponStats; var current = stats?.DamageBonusStat;
            if (current != StatType.Strength && current != StatType.Dexterity || stats.OverrideDamageBonus.HasValue || stats.DamageDescription.Count == 0) return;
            int old = ((ModifiableValueAttributeStat)Owner.Stats.GetStat(current.Value)).Bonus, wisdom = Owner.Stats.Wisdom.Bonus;
            if (wisdom <= 0 || wisdom <= old) return;
            float multiplier = stats.DamageBonusStatMultiplier + stats.AdditionalDamageBonusStatMultiplier;
            int delta = (int)(wisdom * multiplier) - (int)(old * (old < 0 ? 1f : multiplier));
            stats.DamageDescription[0].AddModifier(new Kingmaker.RuleSystem.Rules.Modifier(delta, Fact, ModifierDescriptor.UntypedStackable));
            stats.OverrideDamageBonusStat(StatType.Wisdom);
        }
        public void OnEventDidTrigger(RuleAttackRoll evt) { }
        public void HandleTurnStarted(UnitEntityData unit) => Data.Openings.RemoveAll(x => x.Enemy.Value == unit);
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck rule) { }
        public void OnNewRound() => Expire();
        protected override void OnTurnOff() => Data.Openings.Clear();
        protected override void OnApplyPostLoadFixes() => Expire();
    }

    public sealed class LinnormWrathData { [JsonProperty] public bool Used; [JsonProperty] public TimeSpan Expires; }
    [TypeId("e61f9564c33246a6a9c19bde34bf4402")]
    public sealed class LinnormWrathAllowance : UnitFactComponentDelegate<LinnormWrathData>, ITurnBasedModeHandler
    {
        internal static LinnormWrathData For(UnitEntityData owner) => owner.Facts.List.Where(f => f.Blueprint.AssetGuid == LinnormFeats.Ids[2] && T11TrainingOptions.Active(owner, f)).SelectMany(f => f.Components).FirstOrDefault(c => c.SourceBlueprintComponent is LinnormWrathAllowance)?.GetData<LinnormWrathData>();
        public void HandleTurnStarted(UnitEntityData unit) { if (unit == Owner) Data.Used = false; }
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck rule) { }
    }

    [TypeId("019e6b81817748a693764c63f8e760fd")]
    public sealed class ViciousStompReaction : UnitFactComponentDelegate, IKnockOffHandler, IGlobalSubscriber
    {
        public void HandleKnockOff(UnitEntityData initiator, UnitEntityData target)
        {
            if (Fact.IsActive && Fact.IsTurnedOn && !Fact.IsDisposed && target != null && target != Owner && target.State.Prone.ShouldBeActive) LinnormOpportunity.Try(Owner, target, true);
        }
    }
}
