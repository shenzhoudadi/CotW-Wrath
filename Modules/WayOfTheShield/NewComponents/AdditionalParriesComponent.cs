using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using static Way_of_the_shield.NewComponents.OffHandParry;

namespace Way_of_the_shield.NewComponents
{
    [TypeId("ebb8579958c449a0b0bfb7025dc91bab")]
    public class AdditionalParriesComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttacksCount>
    {
        public ContextValue Bonus = 1;
        public ConditionsChecker Conditions;
        public bool Haste, Penalized;
        public void OnEventAboutToTrigger(RuleCalculateAttacksCount evt)
        {
            var part = evt?.Initiator?.Parts.Get<OffHandParryUnitPart>();
            if (!OffHandParryUnitPart.IsCalculatingFor(evt?.Initiator) || part?.weapon == null || part.weapon != evt.Initiator.Body.SecondaryHand.MaybeWeapon || Bonus == null) return;
            // Feature contexts persist from selection/load and their cached rank may
            // predate Improved/Greater TWF. Evaluate this blueprint afresh for this
            // owner, without rewriting the persisted feature or its parent context.
            var context = new MechanicsContext(evt.Initiator, evt.Initiator, OwnerBlueprint, null);
            context.RecalculateRanks();
            using (context.GetDataScope(evt.Initiator))
            {
                if (Conditions?.Conditions?.Length > 0 && !Conditions.Check()) return;
                var number = Bonus.Calculate(context);
                if (number > 0) evt.AddExtraAttacks(number, Haste, Penalized, part.weapon);
            }
        }
        public void OnEventDidTrigger(RuleCalculateAttacksCount evt) { }
    }
}
