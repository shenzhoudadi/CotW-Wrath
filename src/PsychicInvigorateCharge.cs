using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;

namespace CallOfTheWild.Wrath.Prototype
{
    [HarmonyPatch(typeof(AbilityCasterHasNoFacts),nameof(AbilityCasterHasNoFacts.IsCasterRestrictionPassed))]
    internal static class PsychicInvigorateChargePatch
    {
        private static readonly BlueprintGuid Exhausted=BlueprintGuid.Parse("46d1b9cc3d0fd36469a471b047d773a2");
        private static readonly BlueprintGuid Invigorate=PsychicAssets.Id("InvigorateBuff");
        // Keep the native restriction caches, mount checks and other conditions.
        // Only the exhausted fact check for a charge changes, and only while
        // this spell is active. Never remove the underlying exhaustion fact.
        internal static bool HasBlockingFact(UnitDescriptor unit,BlueprintFact fact,AbilityCasterHasNoFacts checker)
        {
            if(fact?.AssetGuid!=Exhausted || !(checker.OwnerBlueprint is BlueprintAbility ability) || ability.GetComponent<AbilityCustomCharge>()==null)
                return unit.HasFact(fact);
            if(!unit.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Invigorate && b.IsTurnedOn && !b.IsSuppressed))return unit.HasFact(fact);
            return unit.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Exhausted && !b.IsSuppressed);
        }
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original=AccessTools.Method(typeof(UnitHelper),"HasFact",new[]{typeof(UnitDescriptor),typeof(BlueprintFact)});
            var replacement=AccessTools.Method(typeof(PsychicInvigorateChargePatch),nameof(HasBlockingFact));
            foreach(var instruction in instructions) {
                if(instruction.Calls(original)) {
                    var self=new CodeInstruction(OpCodes.Ldarg_0);self.labels.AddRange(instruction.labels);self.blocks.AddRange(instruction.blocks);yield return self;
                    yield return new CodeInstruction(OpCodes.Call,replacement);
                }else yield return instruction;
            }
        }
    }
}
