using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;

namespace Way_of_the_shield {
 [HarmonyPatch(typeof(WeaponParametersDamageBonus),"CalculatePowerAttackScaling")]
 public static class MountedPowerAttack {
  public static bool UsesTwoHandedPowerAttack(ItemEntityWeapon weapon,RuleCalculateWeaponStats rule) =>
   weapon?.HoldInTwoHands==true || (rule?.Initiator?.GetSaddledUnit()!=null && rule.Weapon==weapon &&
    weapon?.Blueprint?.Double==false && weapon.Blueprint.Category is WeaponCategory.Spear or WeaponCategory.Longspear && !rule.IsSecondary);
  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions){
   var code=instructions.ToList();var getter=AccessTools.PropertyGetter(typeof(ItemEntityWeapon),nameof(ItemEntityWeapon.HoldInTwoHands));
   if(code.Count(c=>c.Calls(getter))!=1)throw new InvalidOperationException("Current native Power Attack grip check changed.");
   int i=code.FindIndex(c=>c.Calls(getter));var arg=new CodeInstruction(OpCodes.Ldarg_1);arg.labels.AddRange(code[i].labels);code[i].labels.Clear();code.Insert(i,arg);
   code[i+1].opcode=OpCodes.Call;code[i+1].operand=AccessTools.Method(typeof(MountedPowerAttack),nameof(UsesTwoHandedPowerAttack));return code;
  }
 }
}
