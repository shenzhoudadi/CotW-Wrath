using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    // CotW removes all four elemental descriptors before adding the chosen one.
    // Wrath calculates CL/DC before RuleCastSpell: both native and TTT descriptor
    // bonuses read UnitPartChangeSpellElementalDamage during that earlier step.
    // Expose the conversion through that native service only for this ability's
    // parameter calculation. Never edit a shared spell or leave a unit-wide mode.
    internal static class AdmixtureDescriptors01447
    {
        [ThreadStatic] static Scope current;
        internal static SpellDescriptor Replace(SpellDescriptor original,int energy)=>(original&~ArcaneCast01446.Elements)|ArcaneCast01446.Descriptors[energy];
        internal static ArcaneCastMode01446 Mode(AbilityData ability)
        {
            var bp=ability?.Blueprint;var owner=ability?.Caster?.Unit;
            if(owner?.Buffs==null||bp?.IsSpell!=true||ability.SourceItem!=null||bp.School!=SpellSchool.Evocation)return null;
            foreach(var buff in owner.Buffs.Enumerable) {
                if(!buff.IsActive||!buff.IsTurnedOn)continue;
                var mode=buff.Blueprint.GetComponent<ArcaneCastMode01446>();
                if(mode?.Kind!=ArcaneMode01446.Versatile)continue;
                var pool=mode.Resource?.Get();
                if(pool!=null&&owner.Descriptor.Resources.HasEnoughResource(pool,1)&&
                   ((bp.GetComponent<SpellDescriptorComponent>()?.Descriptor.Value??SpellDescriptor.None)&ArcaneCast01446.Elements)!=0&&ArcaneCast01446.Damage(bp))return mode;
            }
            return null;
        }
        internal static SpellDescriptor ForAbility(AbilityData ability,SpellDescriptor original)
        {
            var mode=Mode(ability);return mode==null?original:Replace(original,mode.EnergyIndex);
        }
        internal static SpellDescriptor ForBlueprint(BlueprintAbility ability,SpellDescriptor original)
        {
            var scope=current;
            return scope?.Mode!=null&&scope.Ability.Blueprint==ability?Replace(original,scope.Mode.EnergyIndex):original;
        }
        internal sealed class Scope : IDisposable
        {
            internal readonly AbilityData Ability;
            internal readonly ArcaneCastMode01446 Mode;
            readonly Scope previous;
            readonly UnitEntityData owner;
            readonly UnitPartChangeSpellElementalDamage part;
            readonly bool created;
            readonly List<Tuple<int,UnitPartChangeSpellElementalDamage.SpellDescriptorEntry>> prior=new List<Tuple<int,UnitPartChangeSpellElementalDamage.SpellDescriptorEntry>>();
            bool disposed;
            // One existing deterministic private buff GUID identifies each of
            // our entries. Preserve every native/third-party entry and its order.
            static bool Ours(UnitPartChangeSpellElementalDamage.SpellDescriptorEntry entry)=>Owned.Any(id=>entry.OwnerGuid==id);
            static readonly string[] Owned=Admixture01446.Names.Select(n=>PsychicAssets.Id(Admixture01446.T(n+"VersatileBuff")).ToString()).ToArray();
            internal Scope(AbilityData ability)
            {
                Ability=ability;Mode=AdmixtureDescriptors01447.Mode(ability);previous=current;owner=ability?.Caster?.Unit;
                if(owner!=null) {
                    part=owner.Get<UnitPartChangeSpellElementalDamage>();
                    if(part==null&&Mode!=null){part=owner.Ensure<UnitPartChangeSpellElementalDamage>();created=true;}
                    if(part!=null) {
                        for(int i=part.Entries.Count-1;i>=0;i--)if(Ours(part.Entries[i])){prior.Add(Tuple.Create(i,part.Entries[i]));part.Entries.RemoveAt(i);}
                        if(Mode!=null)part.Entries.Add(new UnitPartChangeSpellElementalDamage.SpellDescriptorEntry(Owned[Mode.EnergyIndex],Replace(ability.SpellDescriptor,Mode.EnergyIndex)));
                    }
                }
                current=this;
            }
            public void Dispose()
            {
                if(disposed)return;disposed=true;current=previous;
                if(part==null||!ReferenceEquals(owner.Get<UnitPartChangeSpellElementalDamage>(),part))return;
                part.Entries.RemoveAll(Ours);
                foreach(var row in prior.OrderBy(p=>p.Item1))part.Entries.Insert(Math.Min(row.Item1,part.Entries.Count),row.Item2);
                if(created&&part.Entries.Count==0)owner.Remove<UnitPartChangeSpellElementalDamage>();
            }
        }
        internal static Scope Begin(AbilityData ability)=>new Scope(ability);
    }
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.CalculateParams))]
    internal static class AdmixtureParams01447Patch
    {
        static void Prefix(AbilityData __instance,out AdmixtureDescriptors01447.Scope __state)=>__state=AdmixtureDescriptors01447.Begin(__instance);
        static Exception Finalizer(Exception __exception,AdmixtureDescriptors01447.Scope __state){__state?.Dispose();return __exception;}
    }
    [HarmonyPatch(typeof(AbilityData),"get_SpellDescriptor")]
    internal static class AdmixtureAbilityDescriptor01447Patch
    {
        static void Postfix(AbilityData __instance,ref SpellDescriptor __result)=>__result=AdmixtureDescriptors01447.ForAbility(__instance,__result);
    }
    [HarmonyPatch(typeof(BlueprintAbility),"get_SpellDescriptor")]
    internal static class AdmixtureBlueprintDescriptor01447Patch
    {
        static void Postfix(BlueprintAbility __instance,ref SpellDescriptor __result)=>__result=AdmixtureDescriptors01447.ForBlueprint(__instance,__result);
    }
}
