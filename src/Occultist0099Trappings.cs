using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Trappings
    {
        private const string BladeBarrier = "36c8971e91f1745418cc3ffdfac17b74";
        private const string DivineFavor = "9d5d2d3ffdd73c648af3eb3e585b1113";
        private const string FighterFeatSelection = "41c8486641f7d6d4283ca9dae4147a9f";
        internal static readonly string[] Donors = { BladeBarrier, DivineFavor, FighterFeatSelection };
        internal static readonly string[] Tokens =
        {
            "TrappingsMartialSkillValue", "TrappingsOfTheWarriorResonanceBuff",
            "CounterstrikeBuff", "CounterstrikeToggle", "CombatTrickSelection", "TrappingsOfTheWarriorSource"
        };

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache,
            Func<string, string, string, LocalizedString> text, BlueprintUnitProperty level, BlueprintUnitProperty dc)
        {
            var blade = Occultist0099Illusion.Load<BlueprintAbility>(cache, BladeBarrier);
            var divine = Occultist0099Illusion.Load<BlueprintAbility>(cache, DivineFavor);
            var fighter = Occultist0099Illusion.Load<BlueprintFeatureSelection>(cache, FighterFeatSelection);
            if (blade == null || divine == null || fighter == null || fighter.AllFeatures.Length == 0) return null;

            const string martialText = "When wielding the weapon used as the panoply's associated implement, you treat your base attack bonus as though it were 1 point higher for every 4 points of mental focus invested in Abjuration and Transmutation, to a maximum bonus equal to your occultist level.";
            var value = Occultist0099Panoplies.ResonanceValue("TrappingsMartialSkillValue",
                Occultist0099Panoplies.Trappings, 4, false, true);
            var rank = new ContextRankConfig { name = "TrappingsMartialSkillRank" };
            SetEnum(rank, "m_BaseValueType", "CustomProperty");
            Set(rank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(value));
            var martial = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { rank,
                    new AddContextStatBonus { name = "TrappingsMartialSkill", Stat = StatType.BaseAttackBonus,
                        Value = new ContextValue { ValueType = ContextValueType.Rank },
                        Descriptor = ModifierDescriptor.UntypedStackable } } },
                "TrappingsOfTheWarriorResonanceBuff", "Martial Skill", martialText, divine, text);

            const string counterText = "When you take damage from a melee attack while you threaten the attacker, you may spend 1 point of mental focus as an immediate action to make a single attack of opportunity against it. If the attack hits, it deals additional damage equal to one-third your occultist level (minimum +1). This reaction requires an available swift action and attack of opportunity.";
            var counter = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099Counterstrike { name = "OccultistCounterstrike" } } },
                "CounterstrikeBuff", "Counterstrike", counterText, blade, text);
            var counterToggle = Identify(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.Immediately, DeactivateImmediately = true,
                DoNotTurnOffOnRest = true, Group = ActivatableAbilityGroup.None,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099CounterstrikeAvailable { name = "OccultistCounterstrikeGate" } }
            }, "CounterstrikeToggle", "Counterstrike", counterText, blade, text);
            Set(counterToggle, "m_Buff", Ref<BlueprintBuffReference>(counter));

            const string trickText = "You receive one combat feat from the native fighter combat feat selection. You cannot select this trick more than once.";
            var trick = Identify(new BlueprintFeatureSelection { Ranks = 1, IsClassFeature = true,
                Group = FeatureGroup.None, ComponentsArray = Array.Empty<BlueprintComponent>() },
                "CombatTrickSelection", "Combat Trick", trickText, blade, text);
            var options = fighter.AllFeatures
                .Where(feature => feature != null)
                .Select(feature => Ref<BlueprintFeatureReference>(feature))
                .GroupBy(reference => reference.Guid.ToString())
                .Select(group => group.First())
                .ToArray();
            Set(trick, "m_Features", options);
            Set(trick, "m_AllFeatures", options);
            var once = new PrerequisiteNoFeature { name = "OccultistCombatTrickOnce" };
            Set(once, "m_Feature", Ref<BlueprintFeatureReference>(trick));
            trick.ComponentsArray = new BlueprintComponent[] { once };

            var source = Occultist0099Panoplies.Carrier(Occultist0099Panoplies.Trappings,
                Occultist0099Panoplies.TrappingsToken, martialText, blade, text, counterToggle);
            return new BlueprintScriptableObject[] { value, martial, counter, counterToggle, trick, source };
        }
    }

    [TypeId(Occultist0099Ids.CounterstrikeAvailabilityTypeId)]
    public sealed class Occultist0099CounterstrikeAvailable : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable()
        {
            var state = Owner?.Get<Occultist0099FocusPart>()?.State;
            if (state == null || Occultist0099Focus.PanoplyResonance(state,
                    Occultist0099Panoplies.Trappings) <= 0) return false;
            var runtime = Occultist0099NativeScope.RuntimeOf(Owner, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099));
            return Occultist0099NativeScope.InRuntimeContext(runtime,
                component => ((Occultist0099FocusFact0099)component).OwnsPanoply(
                    Occultist0099Panoplies.Trappings), false);
        }
    }

    // The reaction uses the native UnitAttackOfOpportunity command rather than a
    // hand-built attack: native reach, animation, AoO count and Swift cooldown
    // remain the authority. A per-source-attack table prevents a repeated
    // rulebook callback from charging a second point.
    [TypeId(Occultist0099Ids.CounterstrikeTypeId)]
    public sealed class Occultist0099Counterstrike : UnitBuffComponentDelegate,
        ITargetRulebookHandler<RuleAttackWithWeapon>,
        ITargetRulebookHandler<RuleDealDamage>,
        IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        private static readonly ConditionalWeakTable<RuleAttackWithWeapon, object> Sources =
            new ConditionalWeakTable<RuleAttackWithWeapon, object>();
        private static readonly ConditionalWeakTable<UnitAttackOfOpportunity, Claim> Claims =
            new ConditionalWeakTable<UnitAttackOfOpportunity, Claim>();
        private sealed class Claim
        {
            internal UnitEntityData Enemy;
            internal int Bonus;
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt != null && evt.Target == Owner && evt.MeleeDamage != null) Try(evt, evt.MeleeDamage);
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt) { }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt != null && evt.Target == Owner && evt.Reason.Rule is RuleAttackWithWeapon attack) Try(attack, evt);
        }

        internal bool Try(RuleAttackWithWeapon evt) => Try(evt, evt?.MeleeDamage);

        internal bool Try(RuleAttackWithWeapon evt, RuleDealDamage damage)
        {
            if (evt == null || damage == null || damage.IsFake || damage.Target != Owner || damage.Result <= 0)
                return false;
            if (Sources.TryGetValue(evt, out _)) return false;
            if (Buff == null || !Buff.IsActive || !Buff.IsTurnedOn || Buff.IsSuppressed ||
                evt.Target != Owner || evt.AttackRoll == null || evt.AttackRoll.IsFake || !evt.AttackRoll.IsHit ||
                evt.Weapon?.Blueprint.IsMelee != true || evt.IsAttackOfOpportunity) return false;
            var enemy = evt.Initiator;
            if (enemy == null || enemy == Owner) return false;

            var hand = Owner.GetThreatHand();
            float? reach = hand == null ? null : Owner.GetThreatRange(hand);
            if (hand?.Weapon?.Blueprint.IsMelee != true || !reach.HasValue ||
                !UnitHelper.IsAttackOfOpportunityReach(Owner, enemy, reach.Value)) return false;
            if (Owner.Commands?.Swift != null || Owner.CombatState == null ||
                Owner.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift) ||
                !Owner.CombatState.CanAttackOfOpportunity) return false;

            var runtime = Occultist0099NativeScope.RuntimeOf(Owner, Occultist0099Ids.FocusFactGuid,
                typeof(Occultist0099FocusFact0099));
            string identity = Occultist0099NativeCast.IdentityOf(evt);
            int bonus = 0;
            bool canPay = Occultist0099NativeScope.InRuntimeContext(runtime, component =>
            {
                var focus = (Occultist0099FocusFact0099)component;
                if (!focus.CanPayPanoply(Owner, Occultist0099Panoplies.Trappings, 1, identity)) return false;
                bonus = Math.Max(1, focus.ActualLevel / 3);
                return true;
            }, false);
            if (!canPay) return false;

            var commands = Owner.Commands;
            var before = commands == null ? Array.Empty<UnitCommand>()
                : new[] { commands.Free }.Concat(commands.Queue).Where(command => command != null).ToArray();
            // Request the native AoO before charging Focus or Swift so a
            // late native refusal leaves resources untouched.
            if (!Owner.CombatState.AttackOfOpportunity(enemy)) return false;
            Sources.Add(evt, new object());
            bool paid = Occultist0099NativeScope.InRuntimeContext(runtime, component =>
            {
                var focus = (Occultist0099FocusFact0099)component;
                if (!focus.TryPayPanoply(Owner, Occultist0099Panoplies.Trappings, 1, identity).Applied)
                    return false;
                focus.SyncResourceMirror();
                return true;
            }, false);
            if (!paid) return false;
            FellingSmashTrigger.SpendSwift(Owner, Buff.Blueprint);

            if (commands != null)
            {
                var claim = new Claim { Enemy = enemy, Bonus = bonus };
                foreach (var command in new[] { commands.Free }.Concat(commands.Queue).OfType<UnitAttackOfOpportunity>())
                    if (command.Executor == Owner && command.Target == enemy &&
                        !before.Any(old => ReferenceEquals(old, command)))
                        Claims.GetValue(command, _ => claim);
            }
            return true;
        }

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            var rule = evt?.AttackWithWeapon;
            if (rule == null || rule.Initiator != Owner || !rule.IsAttackOfOpportunity) return;
            if (!(Owner.Commands?.Free is UnitAttackOfOpportunity command &&
                  command.Executor == Owner && Claims.TryGetValue(command, out var claim) &&
                  claim.Enemy == rule.Target)) return;
            evt.AddDamageModifier(claim.Bonus, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
    }
}
