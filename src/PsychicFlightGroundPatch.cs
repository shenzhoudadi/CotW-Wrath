using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 [HarmonyPatch(typeof(AreaEffectEntityData),"IsSuitableTargetType")]
 internal static class PsychicFlightGroundPatch {
  static readonly BlueprintGuid Air=Id("AirWalkBuff"),Flight=Id("OverlandFlightBuff");
  static readonly string[] NativeGroundWithoutDescriptor={"16e0e4c6a16f68c49832340b93706499","eca936a9e235875498d1e74ff7c09ecd","8b4ea698ae053c541beed4e050f32dc3","d59890fb468915946bae085439bd0881"};
  internal static bool Protected(UnitEntityData unit)=>(unit?.Blueprint!=null&&PsychicSpiritWeapons.Tokens.Any(t=>Id(t+"Unit")==unit.Blueprint.AssetGuid))||unit?.Buffs!=null&&unit.Buffs.Enumerable.Any(b=>(b.Blueprint.AssetGuid==Air||b.Blueprint.AssetGuid==Flight||b.Blueprint.GetComponent<FlameJetHover01429>()!=null)&&b.IsTurnedOn&&!b.IsSuppressed);
  internal static bool BlocksTar(UnitEntityData unit,Kingmaker.UnitLogic.Mechanics.MechanicsContext context)=>Protected(unit)&&context?.SourceAbility?.AssetGuid.ToString()=="7d700cdf260d36e48bb7af3a8ca5031f";
  [HarmonyPrefix] static bool Prefix(AreaEffectEntityData __instance,UnitEntityData unit,ref bool __result){
   if(!Protected(unit))return true;
   if((__instance.Context?.SpellDescriptor.HasAnyFlag(SpellDescriptor.Ground)??false)||__instance.Blueprint.GetComponent<AreaEffectPit>()!=null||NativeGroundWithoutDescriptor.Contains(__instance.Blueprint.AssetGuid.ToString())){__result=false;return false;}
   return true;
  }
 }
}
