using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;
using MediumClass.Utils;
using System.Linq;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    // Adriel's Death Curse: while Adriel is the channeled spirit, weapon
    // attacks against the medium's own challenge target deal 1d6 of each
    // enabled energy type as extra damage. Each energy toggle buff carries
    // its own copy configured for that energy, so each active toggle adds one
    // die of its energy. At most two toggles may be active (see
    // MediumDeathCurseRules); the damage path enforces the same limit, so a third
    // toggle created outside the toggle action still adds nothing. The component
    // re-checks the channel on every attack so a stale buff left over from a
    // previous seance stays inert.
    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [AllowMultipleComponents]
    [TypeId("3d4f1c8f-0e7c-4a55-9d21-6b2e6d5b0c74")]
    public class MediumDeathCurseComponent : UnitFactComponentDelegate, IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>, ISubscriber, IInitiatorRulebookSubscriber, IDeathCurseToggle
    {
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(MediumDeathCurseComponent));

        public DamageEnergyType[] Energies = new DamageEnergyType[0];

        DamageEnergyType[] IDeathCurseToggle.ToggleEnergies => Energies;

        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (Energies == null || Energies.Length == 0) return;
            if (evt.DamageBundle.Weapon == null) return;
            var medium = Owner.Get<UnitPartMedium>();
            if (medium == null || !medium.IsActiveSpirit(
                BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Adriel))) return;
            if (!IsOwnChallengeTarget(evt.Target)) return;
            foreach (var energy in Energies.Distinct())
                if (MediumDeathCurseRules.IsEnergyEffective(Owner, energy))
                    evt.Add(new EnergyDamage(new DiceFormula(1, DiceType.D6), energy));
        }

        public void OnEventDidTrigger(RulePrepareDamage evt)
        {
        }

        private bool IsOwnChallengeTarget(UnitEntityData target)
        {
            var challengeBuff = BlueprintTool.Get<BlueprintBuff>(Guids.MediumAdrielChallengeBuffTarget);
            foreach (var buff in target.Buffs.Enumerable)
            {
                if (buff.Blueprint != challengeBuff || !buff.IsActive) continue;
                // A restored save can lose the caster link; treat unknown
                // ownership as a match, matching the native challenge checks.
                var caster = buff.Context?.MaybeCaster;
                if (caster == null || caster == Owner) return true;
            }
            return false;
        }
    }
}
