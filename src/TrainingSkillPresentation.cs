using System;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.Common;
using Kingmaker.UI.Tooltip;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace CallOfTheWild.Wrath.Prototype {
 // BaseValue remains the allocation ledger. Only tooltip data displays effective ranks.
 [HarmonyPatch(typeof(StatTooltipData),MethodType.Constructor,new[]{typeof(ModifiableValueSkill)})]
 internal static class TrainingSkillTooltipPatch {
  static void Postfix(StatTooltipData __instance,ModifiableValueSkill skill) {
   if(!T11TrainingOptions.Trained(skill))return;
   int ranks=Math.Max(0,skill.Owner.Stats.BaseAttackBonus.ModifiedValue);
   __instance.BaseValue=ranks;
   StatModifiersBreakdown.AddStoredData(__instance.Breakdown);
   if(skill.ClassSkill&&skill.BaseValue==0&&ranks>0)StatModifiersBreakdown.AddBonus(3,LocalizedTexts.Instance.BonusSources.ClassSkill);
   StatModifiersBreakdown.AddBonus(0,"Training: ranks replaced by BAB ("+ranks+")",addZero:true);
   __instance.Breakdown=StatModifiersBreakdown.Build();
  }
 }
 [HarmonyPatch(typeof(SelectFeature),nameof(SelectFeature.Apply))]
 internal static class TrainingSkillSelectionRefundPatch {
  static void Prefix(SelectFeature __instance,UnitDescriptor unit,out int __state) {
   var c=__instance.Item?.Feature?.GetComponent<TrainingSkillRanks>();
   __state=c==null?0:Math.Max(0,unit.Stats.GetStat(c.Skill).BaseValue);
  }
  static void Postfix(SelectFeature __instance,LevelUpState state,UnitDescriptor unit,int __state) {
   var c=__instance.Item?.Feature?.GetComponent<TrainingSkillRanks>();
   if(c!=null&&__state>0&&T11TrainingOptions.Trained(unit.Stats.GetStat<ModifiableValueSkill>(c.Skill))&&unit.Stats.GetStat(c.Skill).BaseValue==0)state.TotalSkillPoints+=__state;
  }
 }
 [HarmonyPatch(typeof(ApplySkillPoints),nameof(ApplySkillPoints.Apply))]
 internal static class TrainingSkillExistingRefundPatch {
  static void Prefix(UnitDescriptor unit)=>T11TrainingOptions.Refund(unit.Unit);
 }
 [HarmonyPatch(typeof(SpendSkillPoint),nameof(SpendSkillPoint.Check))]
 internal static class TrainingSkillAllocationPatch {
  static void Postfix(SpendSkillPoint __instance,UnitDescriptor unit,ref bool __result) {if(__result&&T11TrainingOptions.Trained(unit.Stats.GetStat<ModifiableValueSkill>(__instance.Skill)))__result=false;}
 }
}
