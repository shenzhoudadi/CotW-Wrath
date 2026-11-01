using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;
namespace CallOfTheWild.Wrath.Prototype {
 [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.CalculateParams))]
 internal static class ChannelCorruptionPreview {
  [HarmonyPrefix]
  private static bool Prefix(AbilityData __instance,ref AbilityParams __result) {
   var source=__instance?.Blueprint?.GetComponent<ChannelCorruptionArm>()?.Source?.Get();
   if(source==null||source==__instance.Blueprint||__instance.Caster==null)return true;
   // Use exactly the source query performed by the unchanged arm action. The
   // arm itself has no cruelty DC handler; calculating its default DC loses +2.
   __result=new AbilityData(source,__instance.Caster).CalculateParams();return false;
  }
 }
}
