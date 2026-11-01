using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Designers.Mechanics.Facts;

namespace CallOfTheWild.Wrath.Prototype {
 [HarmonyPatch]
 internal static class WildShapeCompatibility {
  static readonly System.Reflection.MethodInfo ActionContext=AccessTools.DeclaredPropertyGetter(typeof(ContextAction),"Context");
  static readonly System.Reflection.MethodInfo ActionTarget=AccessTools.DeclaredPropertyGetter(typeof(ContextAction),"Target");
  static readonly System.Reflection.MethodInfo ComponentFact=AccessTools.DeclaredPropertyGetter(typeof(UnitFactComponentDelegate<Kingmaker.EntitySystem.EmptyComponentData>),"Fact");
  internal static bool Unlimited(AbilityData ability)=>ability?.Caster!=null&&
   WildShapeCatalog.Forms.Any(f=>f[0]==ability.Blueprint.AssetGuid.ToString())&&
   (ability.Caster.State.Features.MasterShapeshifter||
    ability.Blueprint.GetComponent<AbilityResourceLogic>()?.ResourceCostDecreasingFacts.Any(r=>
     r.Guid.ToString()!="934670ef88b281b4da5596db8b00df2f"&&ability.Caster.HasFact(r.Get()))==true);
  [HarmonyPatch(typeof(AbilityResourceLogic),nameof(AbilityResourceLogic.CalculateCost))][HarmonyPostfix]
  static void Cost(AbilityData __0,ref int __result){if(Unlimited(__0))__result=0;}
  // Keep the ordinary native availability path (amount zero), and the native
  // spend path. Both call CalculateCost, so no separate refund or use counter.
  [HarmonyPatch(typeof(ItemEntity),nameof(ItemEntity.AddEnchantment))][HarmonyPostfix]
  static void EnchantmentAdded(ItemEntity __instance)=>Changed(__instance);
  [HarmonyPatch(typeof(ItemEntity),nameof(ItemEntity.RemoveEnchantment))][HarmonyPostfix]
  static void EnchantmentRemoved(ItemEntity __instance)=>Changed(__instance);
  static void Changed(ItemEntity item){
   var u=item?.HoldingSlot?.Owner?.Unit;
   if(u==null||!ReferenceEquals(WeaponShiftTransfer.Source(u),item))return;
   foreach(var f in u.Facts.List.OfType<UnitFact>().ToArray())f.CallComponents<WeaponShiftTransfer>(c=>c.Refresh());
  }
  internal static bool Powerful(UnitEntityData u)=>WildShapeFeats.InShape(u)&&WildShapeFeats.Has(u,WildShapeFeats.Tokens[1]);
  internal static Size EffectiveSize(UnitEntityData u)=>Powerful(u)?u.State.Size.Shift(1):u.State.Size;
  internal static bool GrabAllowed(UnitEntityData attacker,UnitEntityData target)=>attacker!=null&&target!=null&&EffectiveSize(target)<=EffectiveSize(attacker);
  internal static bool TrampleAllowed(UnitEntityData attacker,UnitEntityData target)=>attacker!=null&&target!=null&&EffectiveSize(target)<EffectiveSize(attacker);
  [HarmonyPatch(typeof(ContextActionCombatManeuverCustom),nameof(ContextActionCombatManeuverCustom.RunAction))][HarmonyPrefix]
  static bool Grab(ContextActionCombatManeuverCustom __instance){
   if(__instance.Type!=CombatManeuver.Grapple||!(__instance.Success?.Actions?.Any(a=>a is ContextActionGrapple)==true))return true;
   var a=((Kingmaker.UnitLogic.Mechanics.MechanicsContext)ActionContext.Invoke(__instance,null))?.MaybeCaster;
   var t=((Kingmaker.Utility.TargetWrapper)ActionTarget.Invoke(__instance,null))?.Unit;
   return !(Powerful(a)||Powerful(t))||GrabAllowed(a,t);
  }
  [HarmonyPatch(typeof(ManeuverTrigger),nameof(ManeuverTrigger.OnEventDidTrigger))][HarmonyPrefix]
  static bool Trample(ManeuverTrigger __instance,RuleCombatManeuver __0){
   if(__instance.ManeuverType!=CombatManeuver.Overrun)return true;
   var id=((UnitFact)ComponentFact.Invoke(__instance,null))?.Blueprint.AssetGuid.ToString();
   if(id!="0e6233b7c8a14f01b80a83731868454c"&&id!="9292099e5fd70f84fb07fbb9b8b6a5a5")return true;
   return !(Powerful(__0.Initiator)||Powerful(__0.Target))||TrampleAllowed(__0.Initiator,__0.Target);
  }
 }
}
