using Kingmaker.UnitLogic.Abilities.Components.Base;

namespace Way_of_the_shield.NewComponents
{
    public class AbilityTargetEquippedWithShield : BlueprintComponent, IAbilityTargetRestriction
    {
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target) =>
            new LocalizedString { m_Key = "ContextConditionEquippedWithShield_UIText" };
        public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target) =>
            target?.Unit?.Body?.SecondaryHand?.MaybeShield?.ArmorComponent != null;
    }
}