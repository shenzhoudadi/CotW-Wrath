using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium;
using MediumClass.NewComponents;
using MediumClass.Prowler;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    /// <summary>
    /// Trance of Many's buff. It lends a legend the medium is not channeling and
    /// keeps lending as the medium levels: 10th grants the intermediate power,
    /// 14th adds the greater one, 18th adds the supreme one. The buff lasts until
    /// the channel ends (<see cref="ApplySpirits.OnDeactivate"/> retires it) or
    /// the medium picks another legend.
    /// </summary>
    [TypeId("3f9d2c74-58ae-4b1a-9d05-6c7e40b8a1f3")]
    public class MediumTranceOfManyComponent : UnitFactComponentDelegate<ApplySpirits.SecondaryGrant>,
        IUnitReapplyFeaturesOnLevelUpHandler
    {
        public BlueprintCharacterClassReference Spirit;

        // Same trio as MediumSpiritComponent: OnActivate covers a fresh
        // application, OnPostLoad and OnTurnOn cover a restored buff. All three
        // route through GrantSecondary, which only adds what the stored grant
        // does not hold yet, so repeating them cannot stack ranks.
        public override void OnActivate()
        {
            // Only one secondary legend at a time: picking another one (or an
            // Astral Beacon) replaces the previous lend instead of stacking.
            SecondaryLend.Retire(Owner, Fact?.Blueprint as BlueprintBuff);
            Apply();
        }

        public override void OnPostLoad() => Apply();
        public override void OnTurnOn() => Apply();

        public void HandleUnitReapplyFeaturesOnLevelUp() => Apply();

        public override void OnDeactivate() => ApplySpirits.RevokeSecondary(Owner, Spirit, Data);

        private void Apply() => ApplySpirits.GrantSecondary(Owner, Spirit, Tiers(Owner), Data);

        /// <summary>10th: intermediate; 14th: + greater; 18th: + supreme.</summary>
        public static int Tiers(UnitEntityData owner)
        {
            int level = ProwlerSpiritRules.SpiritClassLevel(owner);
            return level >= 18 ? 3 : level >= 14 ? 2 : 1;
        }
    }
}
