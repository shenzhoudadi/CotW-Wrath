using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    // Native spells additionally exclude Plant in their precast target check.
    // Scope the exception to the actual AbilityData being checked: a component
    // knows its shared blueprint, but cannot identify an item/non-Psychic cast.
    internal static class NatureTargeting01448
    {
        [ThreadStatic] internal static AbilityData Current;
        [ThreadStatic] internal static RuleCastSpell CurrentCast;
        internal static readonly BlueprintGuid PlantGuid=BlueprintGuid.Parse(MagaambyanTelepath01447.PlantId);
        internal static bool CanPay(AbilityData ability,UnitEntityData caster)
        {
            if(ability==null||caster==null||ability.Caster?.Unit!=caster||ability.StickyTouch!=null||!PsychicCasting.IsPsychic(ability)||(ability.SpellDescriptor&SpellDescriptor.MindAffecting)==0)return false;
            // RuleCastSpell checks CanTarget AFTER paying. Its last two points
            // must remain valid for that cast, including later delivery scans.
            var paid=ReferenceEquals(CurrentCast?.Spell,ability)?CurrentCast.Context:ContextData<MechanicsContext.Data>.Current?.Context?.SourceAbilityContext;
            if(ReferenceEquals(paid?.Ability,ability)&&paid.HasMetamagic(MagaambyanTelepath01447.Paid))return true;
            if(!caster.Buffs.Enumerable.Any(b=>b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<NaturesCommand01447>()!=null))return false;
            var wis=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(BlueprintGuid.Parse(PsychicClass.WisdomPoolId));
            var cha=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(BlueprintGuid.Parse(PsychicClass.CharismaPoolId));
            var pool=wis!=null&&caster.Resources.ContainsResource(wis)?wis:cha;
            return pool!=null&&caster.Resources.HasEnoughResource(pool,2);
        }
        internal static bool PlantAllowed(AbilityData ability,UnitEntityData caster,UnitEntityData target)
        {
            if(target==null||!target.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(PlantGuid))||!CanPay(ability,caster))return false;
            return MagaambyanTelepath01447.LanguageAllowed(caster,ability.Blueprint);
        }
        // PrestigePlus previews UnitPartSpellResistance.IsImmune from its
        // CanTarget postfix before RuleCastSpell can pay. This is only a
        // read-only preview for the exact scoped AbilityData, never a general
        // unpaid execution exemption or a change to another spell's immunity.
        internal static bool ImmunityPreview(UnitEntityData target,MechanicsContext context)
        {
            var ability=Current;
            return ability!=null&&ReferenceEquals(context?.SourceAbilityContext?.Ability,ability)&&PlantAllowed(ability,context.MaybeCaster,target);
        }
    }
    [HarmonyPatch(typeof(RuleCastSpell),nameof(RuleCastSpell.OnTrigger))]
    internal static class NaturePaidTarget01448Patch
    {
        static void Prefix(RuleCastSpell __instance,out RuleCastSpell __state){__state=NatureTargeting01448.CurrentCast;NatureTargeting01448.CurrentCast=__instance;}
        static Exception Finalizer(RuleCastSpell __state,Exception __exception){NatureTargeting01448.CurrentCast=__state;return __exception;}
    }
    [HarmonyPatch]
    internal static class NatureTargetScope01448Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(AbilityData),nameof(AbilityData.CanTarget),new[]{typeof(TargetWrapper)});
            yield return AccessTools.Method(typeof(AbilityData),nameof(AbilityData.CantTargetReason),new[]{typeof(TargetWrapper)});
            yield return AccessTools.Method(typeof(AbilityData),"ValidateUnit",new[]{typeof(UnitEntityData)});
        }
        static void Prefix(AbilityData __instance,out AbilityData __state){__state=NatureTargeting01448.Current;NatureTargeting01448.Current=__instance;}
        static Exception Finalizer(AbilityData __state,Exception __exception){NatureTargeting01448.Current=__state;return __exception;}
    }
    [HarmonyPatch(typeof(AbilityTargetHasFact),nameof(AbilityTargetHasFact.IsTargetRestrictionPassed))]
    internal static class NatureTargetFact01448Patch
    {
        static void Postfix(AbilityTargetHasFact __instance,UnitEntityData caster,TargetWrapper target,ref bool __result)
        {
            if(__result||!__instance.Inverted||__instance.FromCaster)return;
            var ability=NatureTargeting01448.Current;
            var facts=__instance.CheckedFacts;bool plantListed=false;
            foreach(var fact in facts)if(fact?.AssetGuid==NatureTargeting01448.PlantGuid){plantListed=true;break;}
            if(!plantListed)return;
            if(ability==null||!ability.Blueprint.TargetRestrictions.Any(r=>ReferenceEquals(r,__instance))||!NatureTargeting01448.PlantAllowed(ability,caster,target?.Unit))return;
            // Remove only Plant from this negative list. Construct, undead,
            // animal/humanoid restrictions and unrelated protective facts stay.
            foreach(var fact in facts)if(fact!=null&&fact.AssetGuid!=NatureTargeting01448.PlantGuid&&target.Unit.HasFact(fact))return;
            __result=true;
        }
    }
}
