using Kingmaker;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;

namespace Way_of_the_shield.NewComponents
{
    public class AoOOnFarMiss : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        public bool CheckBuff;
        public bool CheckOnCaster;
        public BlueprintBuffReference m_FactToCheck;
        public bool CasterOnly;
        public bool ReduceBuffRanksAfterAOO = true;
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            var caster = Context?.MaybeCaster;
            var roll = evt?.AttackRoll;
            if (caster == null || caster.State.CanAct != true || evt.Weapon?.Blueprint.IsMelee != true || roll == null || roll.IsFake || roll.IsHit) return;
            if (CasterOnly && evt.Target != caster) return;
            if (roll.D20 + roll.AttackBonus - roll.TargetAC > -5) return;
            if (UnityEngine.Vector3.Distance(caster.Position, Owner.Position) > 1.524f + caster.Corpulence + Owner.Corpulence) return;
            bool allowed = !CheckBuff;
            if (CheckBuff)
            {
                var checkerOwner = CheckOnCaster ? caster : evt.Target;
                var checker = checkerOwner?.Buffs.GetBuff(m_FactToCheck?.Get());
                checker?.CallComponents<UpsettingRoundTracker>(c => allowed = c.TrySpend(evt.Initiator));
            }
            if (allowed) Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(caster, evt.Initiator, false);
        }
    }
}
