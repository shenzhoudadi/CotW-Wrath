using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MediumClass.Medium;
using MediumClass.NewComponents;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    /// <summary>
    /// Astral Beacon (20th level): the buff names one legend and lends that
    /// legend's intermediate, greater and supreme powers for its duration.
    ///
    /// The granting itself stays in <see cref="ApplySpirits.GrantSecondary"/> /
    /// <see cref="ApplySpirits.RevokeSecondary"/> — the same code path the old
    /// all-day version used — so the overwrite powers (Adriel, Robin Hood) and
    /// the Trickster rank scaling live in exactly one place. The returned
    /// <see cref="ApplySpirits.SecondaryGrant"/> is saved with the buff, so the
    /// revoke removes exactly the ranks this lend added.
    /// </summary>
    [TypeId("05e64775-4e25-4b7a-8927-b96900e557ac")]
    public class MediumAstralBeaconComponent : UnitFactComponentDelegate<ApplySpirits.SecondaryGrant>
    {
        public BlueprintCharacterClassReference Spirit;

        public override void OnActivate()
        {
            // One lend at a time: a second beacon (or a Trance of Many pick)
            // replaces this one instead of stacking a second legend's powers.
            SecondaryLend.Retire(Owner, Fact?.Blueprint as BlueprintBuff);
            ApplySpirits.GrantSecondary(Owner, Spirit, 3, Data);
        }

        public override void OnDeactivate() => ApplySpirits.RevokeSecondary(Owner, Spirit, Data);
    }
}
