using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace CallOfTheWild.Wrath.Prototype
{
    // Lives on the learned feature, not the toggle: delayed projectiles and
    // summoned creatures still receive the amplification paid for by their cast.
    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [TypeId("e9b1651bb57751d9b9b0cf3293224b44")]
    public sealed class PsychicLinkedSpellEffects : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RulePrepareDamage>, IInitiatorRulebookHandler<RuleAttackRoll>,
        IInitiatorRulebookHandler<RuleSummonUnit>, IInitiatorRulebookSubscriber
    {
        public PsychicAmplificationKind Kind;
        private static readonly ConditionalWeakTable<object,object> Applied=new ConditionalWeakTable<object,object>();
        internal static DiceType LargerDie(DiceType die) => die==DiceType.D4?DiceType.D6:
            die==DiceType.D6?DiceType.D8:die==DiceType.D8?DiceType.D10:die==DiceType.D10?DiceType.D12:die;
        internal static bool HasForceDice(BlueprintAbility spell)
        {
            return Walk(spell.ComponentsArray,new HashSet<object>(),0).OfType<ContextActionDealDamage>()
                .Any(d=>d.DamageType.Type==DamageType.Force && LargerDie(d.Value.DiceType)!=d.Value.DiceType &&
                    d.Value.DiceCountValue!=null && (d.Value.DiceCountValue.ValueType!=Kingmaker.UnitLogic.Mechanics.ContextValueType.Simple || d.Value.DiceCountValue.Value>0));
        }
        // Inspect action graphs only; never follow blueprint links into the cache.
        private static IEnumerable<object> Walk(object value,HashSet<object> seen,int depth)
        {
            if(value==null || depth>16 || value is string || value is BlueprintReferenceBase ||
                value is SimpleBlueprint || value is UnityEngine.Object || value.GetType().IsValueType || !seen.Add(value)) yield break;
            yield return value;
            if(value is IEnumerable list) {
                foreach(var item in list) foreach(var child in Walk(item,seen,depth+1)) yield return child;
            } else foreach(var field in value.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))
                foreach(var child in Walk(field.GetValue(value),seen,depth+1)) yield return child;
        }
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if(Kind!=PsychicAmplificationKind.FocusedForce ||
                !PsychicAmplificationRuntime.TryReservation(evt.ParentRule.Reason?.Context,Kind,out var paid)) return;
            foreach(var damage in evt.DamageBundle.OfType<ForceDamage>()) {
                var dice=damage.Dice.ModifiedValue; var next=LargerDie(dice.Dice);
                if(dice.Rolls<=0 || next==dice.Dice || Applied.TryGetValue(damage,out _)) continue;
                Applied.Add(damage,new object()); damage.Dice.Modify(new DiceFormula(dice.Rolls,next),paid.Source);
            }
        }
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if(Kind!=PsychicAmplificationKind.MindsEye || (evt.AttackType!=AttackType.RangedTouch && evt.AttackType!=AttackType.Ranged) ||
                evt.RuleAttackWithWeapon!=null || !PsychicAmplificationRuntime.TryReservation(evt.Reason?.Context,Kind,out var paid) ||
                Applied.TryGetValue(evt,out _)) return;
            Applied.Add(evt,new object());
            // Join the native stat's insight bucket. Copying a rule modifier would
            // add it after the inner attack-bonus rule and stack with stat insight.
            // Rulebook removes this temporary modifier when this attack completes.
            evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus.AddModifier(4,paid.Source,ModifierDescriptor.Insight));
        }
        public void OnEventDidTrigger(RuleSummonUnit evt)
        {
            if(Kind!=PsychicAmplificationKind.ConjuredArmor || evt.SummonedUnit==null ||
                !PsychicAmplificationRuntime.TryReservation(evt.Context,Kind,out var paid) || paid.Effect==null ||
                Applied.TryGetValue(evt,out _)) return;
            Applied.Add(evt,new object());
            var buff=evt.SummonedUnit.Descriptor.AddBuff(paid.Effect,evt.Context,TimeSpan.FromSeconds(6*Math.Max(1,paid.CasterLevel)));
            if(buff!=null) buff.IsNotDispelable=true;
        }
        public void OnEventDidTrigger(RulePrepareDamage evt) { }
        public void OnEventDidTrigger(RuleAttackRoll evt) { }
        public void OnEventAboutToTrigger(RuleSummonUnit evt) { }
    }
}
