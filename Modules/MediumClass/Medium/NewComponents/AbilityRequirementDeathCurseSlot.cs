using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Utils;

namespace MediumClass.Medium.NewComponents
{
    // UI side of the two-energy limit: the button of a third energy is unavailable and says why.
    // Switching an active energy off always passes. The toggle action re-checks on execution.
    [TypeId("19866215-af68-4eaa-9aac-0eb6e6454c4d")]
    public class AbilityRequirementDeathCurseSlot : BlueprintComponent, IAbilityRestriction
    {
        public BlueprintBuffReference m_Buff;

        public string GetAbilityRestrictionUIText() => MediumDeathCurseRules.LimitMessage();

        public bool IsAbilityRestrictionPassed(AbilityData ability) =>
            MediumDeathCurseRules.CanToggle(ability.Caster?.Unit, m_Buff?.Get());
    }
}
