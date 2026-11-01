// Original CotW Psychic archetypes (MIT); retain native action and confusion lifecycles.
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    [AllowedOn(typeof(BlueprintAbility),false)]
    [TypeId("c4f82725d2bd4a58a47e61d605720101")]
    public sealed class PhysicalMutationCaster01457 : BlueprintComponent,IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>caster!=null
            && !caster.Descriptor.State.HasCondition(UnitCondition.Fatigued)
            && !caster.Descriptor.State.HasCondition(UnitCondition.Exhausted);
        public string GetAbilityCasterRestrictionUIText()=>"Cannot physically mutate while fatigued or exhausted.";
    }

    // The native controller raises this event after rolling and before creating
    // its command. Change only our confusion source's roll, preserving the
    // native timing, action payment, command and combat-mode implementation.
    [AllowedOn(typeof(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff),false)]
    [TypeId("c4f82725d2bd4a58a47e61d605720102")]
    public sealed class MarauderConfusionControl01457 : UnitFactComponentDelegate,IConfusionRollResultHandler,IGlobalSubscriber
    {
        public bool RerollNormally;
        public ConfusionState ForcedState;
        public void HandleConfusionRollResult(UnitEntityData unit,ConfusionState state)
        {
            var caster=Fact.MaybeContext?.MaybeCaster;
            if(unit!=Owner||caster==null||!Owner.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed
                &&b.Blueprint.AssetGuid==BlueprintGuid.Parse(PsychicMarauder01457.ConfusionId)&&b.Context?.MaybeCaster==caster))return;
            var part=Owner.Get<UnitPartConfusion>();if(part==null||part.State!=state||Owner.Descriptor.State.HasCondition(UnitCondition.AttackNearest))return;
            var selected=state;
            if(RerollNormally){if(state!=ConfusionState.ActNormally)return;
                // A single d3 is the exact distribution of a d100 rejection
                // reroll with the normally-acting quarter removed, without
                // an unbounded loop when another mod forces dice results.
                int roll=Rulebook.Trigger(new RuleRollDice(Owner,new DiceFormula(1,DiceType.D3))).Result;
                selected=roll<=1?ConfusionState.DoNothing:roll==2?ConfusionState.SelfHarm:ConfusionState.AttackNearest;
            }else{
                if(ForcedState==ConfusionState.ActNormally)return;
                selected=ForcedState;
            }
            part.State=selected;part.RetainControl();
        }
    }
}
