using System;
using System.Linq;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Mechanics.Properties;

namespace Way_of_the_shield.NewComponents
{
    public class TotalShieldBonusGetter : PropertyValueGetter
    {
        protected override int GetBaseValue(UnitEntityData unit)
        {
            var shield = unit?.Body?.SecondaryHand?.MaybeShield;
            if (shield?.ArmorComponent == null || ShieldRuntimePatches.IsShieldDenied(unit)) return 0;
            int focus = StatModifiersBreakdown.GetDisplayArmorModifiers(unit.Stats.AC, false, false)
                .Where(modifier => modifier.ModDescriptor == ModifierDescriptor.Focus && modifier.ModValue > 0)
                .Sum(modifier => modifier.ModValue);
            return shield.Blueprint.ArmorComponent.Type.ArmorBonus + GameHelper.GetItemEnhancementBonus(shield.ArmorComponent) + focus;
        }
    }

    [TypeId("bea1b0d5f18a4b5facf9cd2b3f1e7a99")]
    public class HolyShieldDynamicBonus : AddStatBonusAbilityValue,
        ITargetRulebookHandler<RuleCalculateAC>, ITickEachRound
    {
        internal static int ComputeTransferredShieldModifier(int casterTotal, int recipientEnhancementAndFocus) =>
            Math.Max(0, casterTotal - Math.Max(0, recipientEnhancementAndFocus));
        private void Refresh()
        {
            var stat = Owner?.Stats?.GetStat(Stat);
            if (stat == null) return;
            stat.RemoveModifiersFrom(Runtime);
            int adjustment = 0;
            var shield = Owner.Body?.SecondaryHand?.MaybeShield;
            if (shield?.ArmorComponent != null && !ShieldRuntimePatches.IsShieldDenied(Owner))
                adjustment = StatModifiersBreakdown.GetDisplayArmorModifiers(Owner.Stats.AC, false, false)
                    .Where(modifier => modifier.ModValue > 0 &&
                        (modifier.ModDescriptor == ModifierDescriptor.Focus ||
                         modifier.ModDescriptor == ModifierDescriptor.ShieldEnhancement &&
                         (modifier.ItemSource == shield || modifier.ItemSource == shield.ArmorComponent)))
                    .Sum(modifier => modifier.ModValue);
            // The game splits worn shield enhancement/focus into separate stacking descriptors.
            // Transfer one total shield bonus without counting the recipient's enhancement twice.
            int bonus = ComputeTransferredShieldModifier(Value?.Calculate(Context) ?? 0, adjustment);
            if (bonus > 0) stat.AddModifierUnique(bonus, Runtime, Descriptor);
        }
        public void OnEventAboutToTrigger(RuleCalculateAC evt) { Refresh(); }
        public void OnEventDidTrigger(RuleCalculateAC evt) { }
        void ITickEachRound.OnNewRound() { Refresh(); }
        protected override void OnTurnOn() { Refresh(); }
    }
}