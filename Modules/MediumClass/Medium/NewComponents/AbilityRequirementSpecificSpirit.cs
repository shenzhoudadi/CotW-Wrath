using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.NewComponents
{
    // Restricts an ability to a Medium who currently channels one specific
    // spirit, e.g. Adriel's Death Curse variants.
    [TypeId("c993f7fa-96aa-4a71-8a80-9a4d98013b22")]
    public class AbilityRequirementSpecificSpirit : BlueprintComponent, IAbilityRestriction
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(AbilityRequirementSpecificSpirit));

        public BlueprintCharacterClassReference Spirit;
        public bool Not = false;

        public string GetAbilityRestrictionUIText()
        {
            return !Not
                ? LocalizationText.Get("Requires this spirit to be channeled.", "需要先降灵该英灵。")
                : LocalizationText.Get("Requires this spirit not to be channeled.", "该英灵不能处于降灵状态。");
        }

        public bool IsAbilityRestrictionPassed(AbilityData ability)
        {
            var state = ability.Caster?.Unit?.Get<UnitPartMedium>();
            if (state == null || Spirit?.Get() == null) return Not;
            return state.IsActiveSpirit(Spirit) != Not;
        }
    }
}
