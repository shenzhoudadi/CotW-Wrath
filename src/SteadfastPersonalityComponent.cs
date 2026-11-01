// Adapted from Holic75/KingmakerRebalance, MIT; see LICENSE-CotW.txt.
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;

namespace CallOfTheWild.Wrath.Prototype
{
    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [TypeId("9c2273f9b3304d7d9594e12d8340137f")]
    public sealed class SteadfastPersonalityComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleSavingThrow>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var context = evt.Reason.Context;
            var will = evt.Initiator.Stats.SaveWill;
            if (!SteadfastRules.Applies(
                context != null,
                context != null && (context.SpellDescriptor & SpellDescriptor.MindAffecting) != 0,
                evt.Type == SavingThrowType.Will,
                will.BaseStat.Type == StatType.Wisdom))
                return;

            var adjustment = SteadfastRules.Adjustment(
                evt.Initiator.Stats.Wisdom.Bonus,
                evt.Initiator.Stats.Charisma.Bonus);
            if (adjustment != 0)
                evt.AddTemporaryModifier(will.AddModifier(adjustment, Runtime,
                    ModifierDescriptor.UntypedStackable));
        }

        public void OnEventDidTrigger(RuleSavingThrow evt) { }
    }
}
