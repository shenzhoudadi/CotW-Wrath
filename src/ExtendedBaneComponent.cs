// Adapted from Holic75/KingmakerRebalance, MIT; see LICENSE-CotW.txt.
using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;

namespace CallOfTheWild.Wrath.Prototype
{
    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [TypeId("3c94f4da34244f718c11180053efa358")]
    public sealed class ExtendedBaneComponent : UnitFactComponentDelegate,
        IResourceAmountBonusHandler, IUnitSubscriber
    {
        public BlueprintAbilityResourceReference Resource;

        public void CalculateMaxResourceAmount(BlueprintAbilityResource resource, ref int bonus)
        {
            if (!Fact.Active || Resource == null || resource == null || resource != Resource.Get())
                return;

            // Read the current stat on each maximum query so equipment and buffs need no cached rank.
            // Only a positive Wisdom bonus adds rounds; a Wisdom penalty never removes rounds.
            bonus += Math.Max(0, Owner.Stats.Wisdom.Bonus);
        }
    }
}
