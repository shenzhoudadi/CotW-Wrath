using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    // Casting Focus extends only rounds-per-level Conjuration durations cast from
    // the private book. Wrath has no RuleApplyBuff, so the summon side uses the
    // native RuleSummonUnit bonus and the buff side uses CalculateDuration.
    [TypeId(Occultist0099Ids.CastingFocusTypeId)]
    public sealed class Occultist0099CastingFocus : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleSummonUnit>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleSummonUnit evt)
        {
            int rounds = Rounds(Owner, evt.Context);
            if (rounds > 0) evt.BonusDuration += rounds.Rounds();
        }

        public void OnEventDidTrigger(RuleSummonUnit evt) { }

        // The bearer is passed in because the duration hook runs outside this
        // buff's own fact context, where Owner is not readable.
        internal int Rounds(UnitEntityData bearer, MechanicsContext context)
        {
            var ability = context?.SourceAbilityContext?.Ability;
            if (ability == null || ability.SourceItem != null || ability.Blueprint.Type != AbilityType.Spell ||
                ability.Blueprint.School != SpellSchool.Conjuration ||
                !Occultist0099NativeScope.IsPrivateBook(ability.Spellbook)) return 0;
            return Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(bearer, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component => ((Occultist0099FocusFact0099)component).WardingTalismanResonance(Occultist0099Conjuration.School), 0);
        }

        internal static void Extend(ContextActionApplyBuff action, MechanicsContext context, ref TimeSpan? duration)
        {
            if (!duration.HasValue || action.Permanent || action.SameDuration || action.UseDurationSeconds ||
                action.DurationValue?.Rate != DurationRate.Rounds) return;
            var buff = context?.MaybeCaster?.Buffs.Enumerable
                .FirstOrDefault(active => active.IsTurnedOn && !active.IsSuppressed &&
                    active.Blueprint.GetComponent<Occultist0099CastingFocus>() != null);
            var component = buff?.Components.FirstOrDefault(runtime =>
                runtime.SourceBlueprintComponent is Occultist0099CastingFocus)?.SourceBlueprintComponent as Occultist0099CastingFocus;
            if (component == null) return;
            int rounds = component.Rounds(buff.Owner, context);
            if (rounds > 0) duration += TimeSpan.FromSeconds(rounds * 6);
        }
    }

    [HarmonyPatch(typeof(ContextActionApplyBuff), "CalculateDuration")]
    internal static class Occultist0099NativeCastingDuration
    {
        private static void Postfix(ContextActionApplyBuff __instance, MechanicsContext context, ref TimeSpan? __result)
            => Occultist0099CastingFocus.Extend(__instance, context, ref __result);
    }

    // Side Step's range is 10 feet per occultist level. Wrath resolves CustomRange
    // from a fixed field, so the private ability alone gets a level-scaled range.
    [HarmonyPatch(typeof(BlueprintAbility), nameof(BlueprintAbility.GetRange))]
    internal static class Occultist0099NativeSideStepRange
    {
        private static void Postfix(BlueprintAbility __instance, ref Feet __result, AbilityData abilityData)
        {
            if (__instance.AssetGuid != Occultist0099Conjuration.SideStepId) return;
            var caster = abilityData?.Caster?.Unit;
            if (caster == null) return;
            int level = Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(caster, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component => ((Occultist0099FocusFact0099)component).ActualLevel, 0);
            if (level > 0) __result = new Feet(level * 10);
        }
    }
}
