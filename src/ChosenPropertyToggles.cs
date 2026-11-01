using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;

namespace CallOfTheWild.Wrath.Prototype {
 [TypeId("f24a5040a3a141fe9ace0b02833e69f4")]
 public sealed class ChosenToggleRestriction:ActivatableAbilityRestriction {
  public int Choice;
  protected override bool IsAvailable()=>ChosenWeaponFeats.Has(Owner,ChosenWeaponFeats.Tokens[1])&&ChosenWeaponFeats.Allowed(Owner,Choice);
 }
}
