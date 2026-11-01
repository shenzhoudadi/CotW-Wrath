using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype {
 [HarmonyPatch]
 internal static class SwarmScatterPatch {
  static readonly System.Reflection.MethodInfo ContextGetter=AccessTools.DeclaredPropertyGetter(typeof(ContextAction),"Context");
  static readonly System.Reflection.MethodInfo TargetGetter=AccessTools.DeclaredPropertyGetter(typeof(ContextAction),"Target");
  internal static bool Protected(MechanicsContext context,UnitEntityData target)=>
   target!=null&&TeamworkFeats.Has(target,TeamworkFeats.Tokens[3])&&TeamworkFeats.Adjacent(target)>0&&
   context?.MaybeCaster!=null&&TeamworkCatalog.RatUnits.Contains(context.MaybeCaster.Blueprint.AssetGuid.ToString())&&
   (TeamworkCatalog.RatSources.Contains(context.SourceAbility?.AssetGuid.ToString())||TeamworkCatalog.RatSources.Contains(context.AssociatedBlueprint?.AssetGuid.ToString()));
  static bool Protected(ContextAction action)=>Protected((MechanicsContext)ContextGetter.Invoke(action,null),((TargetWrapper)TargetGetter.Invoke(action,null))?.Unit);
  [HarmonyPatch(typeof(ContextActionDealDamage),nameof(ContextActionDealDamage.RunAction))][HarmonyPrefix]
  static bool Damage(ContextActionDealDamage __instance)=>!Protected(__instance);
  [HarmonyPatch(typeof(ContextActionApplyBuff),nameof(ContextActionApplyBuff.RunAction))][HarmonyPrefix]
  static bool Distraction(ContextActionApplyBuff __instance)=>__instance.ToCaster||__instance.Buff?.AssetGuid.ToString()!="956331dba5125ef48afe41875a00ca0e"||!Protected(__instance);
 }
}
