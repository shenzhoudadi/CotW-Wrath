using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class BondedSpellSharing {
  sealed class Recipient { internal UnitEntityData Unit; }
  static readonly ConditionalWeakTable<AbilityExecutionContext,Recipient> Casts=new ConditionalWeakTable<AbilityExecutionContext,Recipient>();
  static readonly BlueprintGuid ShareId=PsychicAssets.Id("ShareSpellsFeat"),BondId=PsychicAssets.Id("BondedMindFeat");
  internal static bool Candidate(AbilityData a)=>a?.Blueprint.Type==AbilityType.Spell&&a.Blueprint.Range==AbilityRange.Personal&&a.Spellbook!=null&&a.Caster?.Unit!=null&&a.Spellbook.Owner==a.Caster&&a.SourceItem==null&&!a.IsAlchemistSpell&&!a.AlchemistInfusion&&!a.ArcanistShareTransmutation&&!a.ForceTargetOwner&&!a.OverrideRange.HasValue&&a.MagicHackData==null&&!a.ShouldDelegateToMount;
  internal static bool BondedCaster(UnitEntityData unit){
   if(unit==null)return false;bool share=false,bond=false;var facts=unit.Facts.List;
   for(int i=0;i<facts.Count;i++)if(facts[i] is Feature f&&CompanionSpellSharing.Active(f)){
    var id=f.Blueprint.AssetGuid;if(id==ShareId)share=true;else if(id==BondId)bond=true;if(share&&bond)return true;
   }
   return false;
  }
  internal static bool Eligible(AbilityData a)=>Candidate(a)&&(BondedCaster(a.Caster.Unit)||CompanionSpellSharing.BookAllowed(a));
  internal static bool HandlesTarget(AbilityData a,TargetWrapper target)=>Candidate(a)&&(CompanionSpellSharing.OwnNativePet(a,target?.Unit)||BondedCaster(a.Caster.Unit)||CompanionSpellSharing.BookAllowed(a));
  internal static bool RecipientAllowed(AbilityData a,TargetWrapper target){
   var unit=target?.Unit;if(unit==null||unit==a.Caster.Unit||unit.State.IsDead||unit.State.Features.IsUntargetable||!unit.IsAlly(a.Caster.Unit))return false;
   bool pet=CompanionSpellSharing.RecipientAllowed(a,unit);
   if(!pet&&!(BondedCaster(a.Caster.Unit)&&CooperationFeats.Permanent(unit,"BondedMindFeat")))return false;
   var original=a.Blueprint;var deliver=a.GetDeliverBlueprint(false);
   for(int pass=0;pass<2;pass++){
    if(pass==1&&ReferenceEquals(deliver,original))break;
    var restrictions=(pass==0?original:deliver).TargetRestrictions;
    for(int i=0;i<restrictions.Length;i++)if(!(pet&&CompanionSpellSharing.TypeRestriction(restrictions[i]))&&!restrictions[i].IsTargetRestrictionPassed(a.Caster.Unit,target))return false;
   }
   return true;
  }
  internal static void Record(AbilityExecutionContext context,TargetWrapper target){if(Eligible(context?.Ability)&&RecipientAllowed(context.Ability,target)&&!Casts.TryGetValue(context,out _))Casts.Add(context,new Recipient{Unit=target.Unit});}
  internal static UnitEntityData BuffRecipient(MechanicsContext context)=>context is AbilityExecutionContext root&&Casts.TryGetValue(root,out var r)?r.Unit:null;
 }
 [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.Range),MethodType.Getter)]
 internal static class BondedSpellRangePatch {
  [HarmonyPostfix, HarmonyPriority(Priority.Last)] static void Postfix(AbilityData __instance,ref AbilityRange __result){if(__result==AbilityRange.Personal&&BondedSpellSharing.Eligible(__instance))__result=AbilityRange.Touch;}
 }
 [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.TargetAnchor),MethodType.Getter)]
 internal static class BondedSpellAnchorPatch {
  [HarmonyPostfix, HarmonyPriority(Priority.Last)] static void Postfix(AbilityData __instance,ref AbilityTargetAnchor __result){if(BondedSpellSharing.Eligible(__instance))__result=AbilityTargetAnchor.Unit;}
 }
 [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.CanTarget))]
 internal static class BondedSpellTargetPatch {
  // Run before optional broad companion-sharing prefixes. This is the single
  // decision for our personal-spell route, including rejecting unrelated books.
  [HarmonyPrefix, HarmonyPriority(Priority.First)] static bool Prefix(AbilityData __instance,TargetWrapper __0,ref bool __result){if(!BondedSpellSharing.HandlesTarget(__instance,__0)||__0?.Unit==__instance.Caster.Unit)return true;__result=BondedSpellSharing.RecipientAllowed(__instance,__0);return false;}
 }
 [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.CantTargetReason))]
 internal static class BondedSpellTargetReasonPatch {
  [HarmonyPrefix, HarmonyPriority(Priority.First)] static bool Prefix(AbilityData __instance,TargetWrapper __0,ref string __result){if(!BondedSpellSharing.HandlesTarget(__instance,__0)||__0?.Unit==__instance.Caster.Unit)return true;__result=BondedSpellSharing.RecipientAllowed(__instance,__0)?"":"Requires a living, targetable allied creature with permanently learned Bonded Mind, or your own companion and its granting class's spellbook, and a valid spell target.";return false;}
 }
 [HarmonyPatch(typeof(AbilityData),"ValidateUnit")]
 internal static class BondedSpellUnitValidationPatch {
  [HarmonyPrefix, HarmonyPriority(Priority.First)] static bool Prefix(AbilityData __instance,UnitEntityData __0,ref bool __result){var target=new TargetWrapper(__0);if(!BondedSpellSharing.HandlesTarget(__instance,target)||__0==__instance.Caster.Unit)return true;__result=BondedSpellSharing.RecipientAllowed(__instance,target);return false;}
 }
 [HarmonyPatch(typeof(AbilityEffectRunAction),nameof(AbilityEffectRunAction.Apply))]
 internal static class BondedSpellDeliveryPatch {
  [HarmonyPrefix, HarmonyPriority(Priority.Last)] static void Prefix(AbilityExecutionContext __0,TargetWrapper __1)=>BondedSpellSharing.Record(__0,__1);
 }
 [HarmonyPatch(typeof(ContextActionApplyBuff),nameof(ContextActionApplyBuff.GetBuffTarget))]
 internal static class BondedSpellBuffTargetPatch {
  [HarmonyPostfix, HarmonyPriority(Priority.Last)] static void Postfix(ContextActionApplyBuff __instance,MechanicsContext __0,ref UnitEntityData __result){var recipient=__instance.ToCaster?BondedSpellSharing.BuffRecipient(__0):null;if(recipient!=null)__result=recipient;}
 }
}
