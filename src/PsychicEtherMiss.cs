using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 // Ether's physical obstruction is a separate percentile roll, never native concealment.
 [HarmonyPatch(typeof(RuleAttackRoll),"TryOvercomeTargetConcealmentAndMissChance")]
 internal static class PsychicEtherMissPatch {
  static readonly BlueprintGuid Ether=Id("CondensedEtherBuff");
  internal static int Chance(UnitEntityData a,UnitEntityData b){
   if(a==null||b==null)return 0;
   if(!a.Buffs.Enumerable.Concat(b.Buffs.Enumerable).Any(x=>x.Blueprint.AssetGuid==Ether&&x.IsTurnedOn&&!x.IsSuppressed))return 0;
   return a.DistanceTo(b)>new Feet(5).Meters?50:20;
  }
  internal sealed class RollState {internal int Chance;internal RuleRollD100 Roll;}
  [HarmonyPrefix] static bool Prefix(RuleAttackRoll __instance,ref bool __result,out RollState __state){
   __state=null;int chance=Chance(__instance.Initiator,__instance.Target);if(chance==0)return true;
   var roll=Rulebook.Trigger(new RuleRollD100(__instance.Initiator));
   __state=new RollState {Chance=chance,Roll=roll};
   if((int)roll>chance)return true;
   __result=false;return false;
  }
  [HarmonyPostfix] static void Postfix(RuleAttackRoll __instance,RollState __state){
   if(__state==null)return;
   // Preserve stronger native non-concealment miss-roll diagnostics if one ran.
   if(__instance.MissChanceRoll==null||__instance.MissChanceValue<__state.Chance){Set(__instance,"<MissChanceValue>k__BackingField",__state.Chance);Set(__instance,"<MissChanceRoll>k__BackingField",__state.Roll);}
  }
 }
}
