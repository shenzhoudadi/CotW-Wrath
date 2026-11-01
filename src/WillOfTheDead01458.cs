using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WillOfTheDead01458
    {
        internal const string UndeadId="734a29b693e9ec346ba2951b27987e33";
        internal static readonly HashSet<BlueprintGuid> Intrinsic=new HashSet<BlueprintGuid>();
        internal static void Initialize(BlueprintsCache cache)
        {
            var undead=cache.Load(BlueprintGuid.Parse(UndeadId)) as BlueprintFeature??throw new InvalidOperationException("058 native Undead type is unavailable.");
            var facts=undead.GetComponents<Kingmaker.UnitLogic.FactLogic.AddFacts>().SelectMany(c=>c.Facts).OfType<BlueprintFeature>().Concat(new[]{undead}).Where(f=>f.GetComponents<SpellImmunityToSpellDescriptor>().Any(i=>(i.Descriptor.Value&SpellDescriptor.MindAffecting)!=0)||f.GetComponents<BuffDescriptorImmunity>().Any(i=>(i.Descriptor.Value&SpellDescriptor.MindAffecting)!=0)).ToArray();
            if(facts.Length==0)throw new InvalidOperationException("058 native Undead mind-immunity components are unavailable.");foreach(var f in facts)Intrinsic.Add(f.AssetGuid);
        }
        internal static bool Undead(UnitEntityData target)=>target?.HasFact(ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(BlueprintGuid.Parse(UndeadId)))==true;
        internal static bool Paid(UnitEntityData target,MechanicsContext context)=>Undead(target)&&(context?.SpellDescriptor&SpellDescriptor.MindAffecting)!=0&&PsychicAmplificationRuntime.TryReservation(context,PsychicAmplificationKind.WillOfTheDead,out _);
        internal static bool Preview(AbilityData data,UnitEntityData target)=>Undead(target)&&PsychicAdvancedRuntime01458.Real(data)&&(data.SpellDescriptor&SpellDescriptor.MindAffecting)!=0&&PsychicAdvancedRuntime01458.Allowed(data,PsychicAmplificationKind.WillOfTheDead.ToString());
        internal static bool Bypass(UnitEntityData target,MechanicsContext context)
        {
            if(Paid(target,context))return true;var ability=NatureTargeting01448.Current;
            return ability!=null&&ReferenceEquals(context?.SourceAbilityContext?.Ability,ability)&&Preview(ability,target);
        }
        internal static bool Inherent(UnitEntityData target,int appliedId)=>target.Facts.List.Where(f=>Intrinsic.Contains(f.Blueprint.AssetGuid)).SelectMany(f=>f.Components).Any(c=>c.SourceBlueprintComponent is SpellImmunityToSpellDescriptor im&&(im.Descriptor.Value&SpellDescriptor.MindAffecting)!=0&&c.GetData<SpellImmunityToSpellDescriptor.ComponentData>().AppliedId==appliedId);
    }
    [HarmonyPatch]
    internal static class WillOfTheDeadSpellImmunity01458Patch
    {
        static MethodBase TargetMethod()=>AccessTools.Method(typeof(UnitPartSpellResistance),"CanApply",new[]{typeof(UnitPartSpellResistance.SpellImmunity),typeof(MechanicsContext)});
        static bool Prefix(UnitPartSpellResistance __instance,UnitPartSpellResistance.SpellImmunity __0,MechanicsContext __1,ref bool __result)
        {
            if(__1==null||__0.Type!=SpellImmunityType.SpellDescriptor)return true;
            var target=MagaambyanTelepath01447.Owner(__instance);
            if(WillOfTheDead01458.Bypass(target,__1)&&WillOfTheDead01458.Inherent(target,__0.Id)&&(__0.SpellDescriptor&__1.SpellDescriptor&~MagaambyanTelepath01447.NativeMindImmunity)==0){__result=false;return false;}return true;
        }
    }
    [HarmonyPatch(typeof(BuffDescriptorImmunity),"IsImmune")]
    internal static class WillOfTheDeadBuffImmunity01458Patch
    {
        static void Postfix(BuffDescriptorImmunity __instance,MechanicsContext __0,ref bool __result)
        {
            if(__result&&WillOfTheDead01458.Intrinsic.Contains(__instance.OwnerBlueprint.AssetGuid)&&(__instance.Descriptor.Value&SpellDescriptor.MindAffecting)!=0&&WillOfTheDead01458.Paid(MagaambyanTelepath01447.Owner(__instance),__0)&&(__instance.Descriptor.Value&__0.SpellDescriptor&~MagaambyanTelepath01447.NativeMindImmunity)==0)__result=false;
        }
    }
    [HarmonyPatch(typeof(AbilityTargetHasFact),nameof(AbilityTargetHasFact.IsTargetRestrictionPassed))]
    internal static class WillOfTheDeadTarget01458Patch
    {
        static void Postfix(AbilityTargetHasFact __instance,UnitEntityData caster,TargetWrapper target,ref bool __result)
        {
            if(__result||!__instance.Inverted||__instance.FromCaster)return;var a=NatureTargeting01448.Current;var facts=__instance.CheckedFacts;var undead=BlueprintGuid.Parse(WillOfTheDead01458.UndeadId);
            if(a==null||!a.Blueprint.TargetRestrictions.Any(r=>ReferenceEquals(r,__instance))||!facts.Any(f=>f?.AssetGuid==undead))return;
            bool allowed=WillOfTheDead01458.Preview(a,target?.Unit)||WillOfTheDead01458.Paid(target?.Unit,NatureTargeting01448.CurrentCast?.Context);if(!allowed)return;
            if(facts.Any(f=>f!=null&&f.AssetGuid!=undead&&target.Unit.HasFact(f)))return;__result=true;
        }
    }
    [HarmonyPatch(typeof(AbilityTargetHasNoFactUnless),nameof(AbilityTargetHasNoFactUnless.IsTargetRestrictionPassed))]
    internal static class WillOfTheDeadUnlessTarget01458Patch
    {
        static void Postfix(AbilityTargetHasNoFactUnless __instance,UnitEntityData caster,TargetWrapper target,ref bool __result)
        {
            if(__result)return;var a=NatureTargeting01448.Current;var undead=BlueprintGuid.Parse(WillOfTheDead01458.UndeadId);var facts=__instance.CheckedFacts;
            if(a==null||a.Caster?.Unit!=caster||!a.Blueprint.TargetRestrictions.Any(r=>ReferenceEquals(r,__instance))||!facts.Any(f=>f?.AssetGuid==undead))return;
            if(!WillOfTheDead01458.Preview(a,target?.Unit)&&!WillOfTheDead01458.Paid(target?.Unit,NatureTargeting01448.CurrentCast?.Context))return;
            // Remove only this innate Undead exclusion. An independent fact in
            // the same native restriction, or any other restriction, survives.
            if(facts.Any(f=>f!=null&&f.AssetGuid!=undead&&target.Unit.HasFact(f)))return;__result=true;
        }
    }
}
