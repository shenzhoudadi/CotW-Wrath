using System.Linq;using HarmonyLib;using Kingmaker.Blueprints;
using Kingmaker.RuleSystem.Rules.Abilities;using Kingmaker.UnitLogic.Abilities;using Kingmaker.Utility;
namespace CallOfTheWild.Wrath.Prototype{
 internal static class PsychicSurge01460{
  internal static bool KnownVersion(AbilityData ability){
   var family=ability?.Blueprint.GetComponent<PsychicUndercastFamily>();if(family==null)return false;
   return ability.Spellbook.GetAllKnownSpells().Any(a=>!a.IsTemporary&&a.MetamagicData?.NotEmpty!=true&&(a.Blueprint==ability.Blueprint||a.Blueprint.GetComponent<PsychicUndercastFamily>()?.LowerSpells?.Any(r=>r.Guid==ability.Blueprint.AssetGuid)==true));
  }
  internal static AbilityData Promote(AbilityData low){
   // A fresh execution copy selects the actual higher blueprint, not merely
   // its DC/slot number. The original UI/known spell and all shared assets
   // remain unchanged. ConvertedFrom retains native lower-slot payment.
   return PsychicAdvancedRuntime01458.SurgeConversions(low).FirstOrDefault()??low;
  }
 }
 [HarmonyPatch(typeof(RuleCastSpell),MethodType.Constructor,new[]{typeof(AbilityData),typeof(TargetWrapper)})]
 internal static class PsychicSurgeCast01460Patch{
  static void Prefix(ref AbilityData __0){__0=PsychicSurge01460.Promote(__0);}
 }
}
