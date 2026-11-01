using System.Linq;using HarmonyLib;using Kingmaker.Blueprints;using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.ActivatableAbilities;using Kingmaker.UnitLogic.Mechanics.Actions;
namespace CallOfTheWild.Wrath.Prototype{
 [HarmonyPatch(typeof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility),nameof(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility.GetResource))]
 internal static class AgonizingCount01460Patch{
  static void Postfix(Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility __instance,ref int __result){
   var a=__instance.Ability;
   if(a?.Caster==null||(a.Blueprint.AssetGuid!=PsychicAssets.Id("AgonizingWoundTargetAbility01458")&&a.Blueprint.AssetGuid!=PsychicAssets.Id("AgonizingWoundMenu01460")))return;
   var resource=ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.BlueprintAbilityResource>(PsychicAssets.Id("AgonizingWoundResource01458"));if(resource!=null)__result=a.Caster.Resources.GetResourceAmount(resource);
  }
 }
}
