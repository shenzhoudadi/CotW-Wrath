using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicAmplificationRuntime
    {
        internal sealed class PaidCast { internal PsychicAmplificationKind Kind; internal EntityFact Source; internal BlueprintBuff Effect; internal int CasterLevel; }
        // Effects follow the paid execution, including projectiles after a mode switch.
        // No blueprint/AbilityData mutation and no state serialized into a save.
        private static readonly ConditionalWeakTable<MechanicsContext,Dictionary<PsychicAmplificationKind,PaidCast>> Paid = new ConditionalWeakTable<MechanicsContext,Dictionary<PsychicAmplificationKind,PaidCast>>();
        internal static bool IsDefense(BlueprintAbility spell) => spell!=null &&
            (spell.AssetGuid==PsychicAssets.Id("IntellectFortress") ||
             Enumerable.Range(1,5).Any(n=>spell.AssetGuid==PsychicAssets.Id("MentalBarrier"+n)) ||
             Enumerable.Range(1,3).Any(n=>spell.AssetGuid==PsychicAssets.Id("ThoughtShield"+n)));
        internal static void Reserve(MechanicsContext context,PsychicAmplificationKind kind,EntityFact source,BlueprintBuff effect=null)
        {
            if((int)kind<3) return;
            Paid.GetOrCreateValue(context).Add(kind,new PaidCast { Kind=kind,Source=source,Effect=effect,CasterLevel=context.Params.CasterLevel });
        }
        internal static void Cancel(MechanicsContext context) => Paid.Remove(context);
        private static bool TryPaid(MechanicsContext context,PsychicAmplificationKind kind,out EntityFact source)
        {
            source=null;
            if(!TryReservation(context,kind,out var paid)) return false;
            source=paid.Source;return true;
        }
        internal static bool TryReservation(MechanicsContext context,PsychicAmplificationKind kind,out PaidCast paid)
        {
            paid=null;
            var root=context?.SourceAbilityContext;
            return root!=null && !root.IsDuplicateSpellApplied && Paid.TryGetValue(root,out var casts) && casts.TryGetValue(kind,out paid);
        }
        internal static void Reroll(RuleRollDice roll,RulebookEventContext events)
        {
            if(roll is RuleRollD20 && events.PreviousEvent is RuleSpellResistanceCheck resistance &&
                resistance.Initiator==roll.Initiator && TryPaid(resistance.Context,PsychicAmplificationKind.RelentlessCasting,out var source))
                roll.AddReroll(1,true,source);
        }
        internal static void Extend(ContextDurationValue duration,MechanicsContext context,ref Rounds result)
        {
            if(!TryPaid(context,PsychicAmplificationKind.OngoingDefense,out _)) return;
            var ability=context.SourceAbilityContext.Ability?.Blueprint;
            if(!IsDefense(ability)) return;
            // Only the actual defensive buff's duration, not an incidental effect in
            // the same cast/area. Native Calculate already applied Extend Spell.
            if(ability.GetComponent<AbilityEffectRunAction>()?.Actions.Actions.OfType<ContextActionApplyBuff>()
                .Any(a=>ReferenceEquals(a.DurationValue,duration))==true)
                result=new Rounds(result.Value+1);
        }
    }
    [HarmonyPatch(typeof(RuleRollDice),nameof(RuleRollDice.OnTrigger))]
    internal static class PsychicRelentlessCastingPatch
    {
        private static void Prefix(RuleRollDice __instance,RulebookEventContext context) { PsychicAmplificationRuntime.Reroll(__instance,context); PsychicPageantryFocus.Apply(__instance,context); }
    }
    [HarmonyPatch(typeof(ContextDurationValue),nameof(ContextDurationValue.Calculate))]
    internal static class PsychicOngoingDefensePatch
    {
        private static void Postfix(ContextDurationValue __instance,MechanicsContext context,ref Rounds __result) => PsychicAmplificationRuntime.Extend(__instance,context,ref __result);
    }
}
