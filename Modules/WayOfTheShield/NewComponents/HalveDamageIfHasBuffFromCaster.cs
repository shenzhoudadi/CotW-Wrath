using System.Linq;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Buffs.Components;

namespace Way_of_the_shield.NewComponents
{
    public class HalveDamageIfHasBuffFromCaster : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleCalculateDamage>
    {
        public BlueprintBuffReference m_Buff;
        public BlueprintBuff TheBuff => m_Buff?.Get();
        private static readonly RulebookEvent.CustomDataKey AppliedKey = new("CotWWrath.BastionHalfDamage");
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            UnitEntityData caster = Buff?.MaybeContext?.MaybeCaster;
            BlueprintBuff allyBuff = TheBuff;
            if (caster == null || allyBuff == null || evt.Target==caster || evt.Target?.Buffs == null || evt.DamageBundle == null) return;
            // Use the actual aura recipient as well as its legacy child marker: native
            // area/buff restore paths can restore the visible aura before its child.
            if (!evt.Target.Buffs.Enumerable.Any(buff => buff.IsTurnedOn &&
                (buff.Blueprint == allyBuff || buff.Blueprint.AssetGuid.ToString() is "f7e1a37ceeaa41628cd1b04731eaee69" or "14bf28d3c74e4fb0a952d8f114633f66") &&
                buff.MaybeContext?.MaybeCaster == caster)) return;
            if (evt.TryGetCustomData(AppliedKey, out bool applied) && applied) return;
            evt.SetCustomData(AppliedKey, true);
            foreach (BaseDamage damage in evt.DamageBundle)
                damage.AddDecline(new(DamageDeclineType.ByHalf, Buff));
        }
        public void OnEventDidTrigger(RuleCalculateDamage evt) { }
    }
}
