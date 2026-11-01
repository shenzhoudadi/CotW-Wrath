using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Way_of_the_shield.NewComponents
{
    public class RestrictionOtherActivatables : ActivatableAbilityRestriction
    {
        public BlueprintActivatableAbilityReference[] m_ActivatableAbilities;
        public bool Require;
        protected override bool IsAvailable()
        {
            bool RestrictionIsFound = false;
            if (m_ActivatableAbilities != null)
                foreach (var blueprintActivatableAbilityReference in m_ActivatableAbilities)
                    if ((blueprintActivatableAbilityReference.Guid == BlueprintGuid.Parse("a86db1a253d0446b857938314af1ae51") && Owner.Get<OffHandParry.OffHandParryUnitPart>()?.activated == true) ||
                        Owner.ActivatableAbilities.Enumerable.Any(activatable => activatable.Blueprint == blueprintActivatableAbilityReference.Get() &&
                            (activatable.IsOn || Owner.HasFact(activatable.Blueprint.m_Buff?.Get()))))
                    { RestrictionIsFound = true; break; }
                
            return Require ? RestrictionIsFound : !RestrictionIsFound;
        }
    }
}
