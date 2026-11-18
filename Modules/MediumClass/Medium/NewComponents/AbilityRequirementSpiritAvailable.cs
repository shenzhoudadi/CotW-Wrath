using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utils;

namespace MediumClass.Medium.NewComponents
{
    /// <summary>
    /// "This legend is not the one you already channel." Astral Beacon and
    /// Trance of Many both lend powers of an unchanneled legend, so they need a
    /// per-spirit gate that <see cref="AbilityRequirementHasSpirit"/> (which
    /// only knows "any spirit" / "no spirit") cannot express.
    ///
    /// The same-tier conflict (Adriel shares Champion's powers, Robin Hood
    /// shares Trickster's) is rejected as well: lending a tier whose powers the
    /// channel already grants would stack the same blueprint twice. The Mythic
    /// Multiple Spirits gate is deliberately not applied — borrowing a legend
    /// is not channeling one.
    /// </summary>
    [TypeId("d387c628-b5c6-4510-be39-a9f7af74e914")]
    public class AbilityRequirementSpiritAvailable : BlueprintComponent, IAbilityRestriction
    {
        public BlueprintCharacterClassReference Spirit;

        public string GetAbilityRestrictionUIText() => LocalizationText.Get(
            "Requires a legend that is not currently channeled.",
            "只能选择尚未降灵的职阶。");

        public bool IsAbilityRestrictionPassed(AbilityData ability)
        {
            var state = ability?.Caster?.Unit?.Get<UnitPartMedium>();
            var spirit = Spirit?.Get();
            if (state == null || spirit == null) return false;
            if (state.IsActiveSpirit(Spirit)) return false;
            if (MultiSpiritRules.SameTierConflict(state, Spirit)) return false;
            // Only legends the medium actually registered (the six base ones,
            // plus any third-party legend that shipped a SpiritEntry). Blueprints
            // resolve to one shared instance per GUID, so reference equality is
            // enough — the same test IsActiveSpirit uses.
            return state.Spirits.Any(pair => pair.Key?.Get() == spirit);
        }
    }
}
