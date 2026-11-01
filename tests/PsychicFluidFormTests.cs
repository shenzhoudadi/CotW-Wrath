using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
internal static class PsychicFluidFormTests
{
    static int count;
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Check(bool b,string why){if(!b)throw new Exception("Fluid Form: "+why);count++;}
    static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint=>(T)cache.Load((BlueprintGuid)AccessTools.Method(typeof(CallOfTheWild.Wrath.Prototype.PsychicBloodArmorDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static EntityFactComponent Runtime(BlueprintComponent c,EntityFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);var facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);
        var spell=Asset<BlueprintAbility>(cache,"FluidForm");var bp=Asset<BlueprintBuff>(cache,"FluidFormBuff");
        var context=f.Cast(SpellSchool.Transmutation,0,6).Context;Set(context,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        var buff=Empty<Buff>();Set(buff,"<Blueprint>k__BackingField",bp);Set(buff,"Components",new List<EntityFactComponent>());Set(buff,"m_Context",context);Set(buff,"<Rank>k__BackingField",1);Set(buff,"<IsActive>k__BackingField",true);buff.RestoreOwnerLink(facts);
        Check(spell.School==SpellSchool.Transmutation&&spell.Range==AbilityRange.Personal&&spell.CanTargetSelf&&!spell.CanTargetFriends&&!spell.CanTargetEnemies&&!spell.CanTargetPoint,"Personal transmutation");
        var apply=(ContextActionApplyBuff)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        Check(apply.Buff==bp&&apply.IsFromSpell&&!apply.Permanent&&!apply.AsChild&&!apply.IsNotDispelable&&bp.Stacking==StackingType.Replace,"Timed independent dispellable nonstacking buff");
        Check(!spell.SpellResistance&&spell.GetComponent<AbilityEffectRunAction>().SavingThrowType==SavingThrowType.Unknown,"No resistance or save row");
        Check(ReferenceEquals(spell.Icon,bp.Icon)&&ReferenceEquals(spell.Icon,((BlueprintAbility)cache.Load(BlueprintGuid.Parse("40681ea748d98f54ba7f5dc704507f39"))).Icon),"Same native icon donor as original CotW");
        var links=spell.GetComponents<SpellListComponent>().ToArray();Check(links.Length==3&&links.Count(l=>l.SpellLevel==6)==2&&links.Count(l=>l.SpellLevel==4)==1,"Psychic/Wizard6, Alchemist4");
        foreach(var l in links)Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(x=>x==spell)==1,"Unique reciprocal list link");
        Check(bp.GetComponent<AddFacts>().Facts.Single().AssetGuid.ToString()=="bf7ee56ec9e43c14fa17727997e91993","Original water subtype supplied by native owned AddFacts");
        foreach(int cl in new[]{1,6,12,20,30})foreach(bool extend in new[]{false,true}){
            context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(context,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(context),32).ToArray());
            Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromMinutes(cl*(extend?2:1)),"Actual native duration/Extend");
        }
        var dr=bp.GetComponent<AddDamageResistancePhysical>();var rt=(AddDamageResistanceBase.ComponentRuntime)Runtime(dr,buff);
        Check(rt.GetCurrentValue()==10&&!dr.UsePool&&!dr.IsStackable,"Native DR 10, no pool/stacking");
        foreach(var form in new[]{PhysicalDamageForm.Bludgeoning,PhysicalDamageForm.Piercing,PhysicalDamageForm.Slashing,PhysicalDamageForm.Piercing|PhysicalDamageForm.Slashing}){
            var damage=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(2,DiceType.D6)),0,form);
            Check(rt.Bypassed(damage,null)==((form&PhysicalDamageForm.Slashing)!=0),"Native bypass checks slash including combination forms");
        }
        Check(rt.Bypassed(new EnergyDamage(new DiceFormula(2,DiceType.D6),DamageEnergyType.Fire),null),"Energy is not reduced by physical DR");
        var stat=new ModifiableValue(f.Unit.Stats,StatType.Reach);stat.BaseValue=5;Set(f.Unit.Stats,"Reach",stat);f.Unit.Stats.AllStats.RemoveAll(x=>x.Type==StatType.Reach);f.Unit.Stats.AllStats.Add(stat);
        var reach=bp.GetComponent<AddStatBonus>();var rr=Runtime(reach,buff);
        Check(f.Unit.Stats.ReachRange.Value==0,"Baseline native extra reach");
        for(int i=0;i<3;i++){
            rr.TurnOn();Check(f.Unit.Stats.ReachRange.Value==10,"Native activation grants exactly ten feet");
            var other=stat.AddModifier(4,(EntityFact)null,ModifierDescriptor.Enhancement);Check(f.Unit.Stats.ReachRange.Value==10,"Same descriptor takes highest instead of stacking");
            rr.TurnOff();Check(f.Unit.Stats.ReachRange.Value==4,"Native off removes only owned reach");stat.RemoveModifier(other);
            Check(f.Unit.Stats.ReachRange.Value==0,"No accumulated reach after repeated on/off");
        }
        PsychicExpansionTests.Inventory["fluidFormAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Fluid Form checks: native DR bypass and runtime reach lifecycle/stacking, duration/Extend, spell lists and original donor. Managed fixture, not gameplay.");return count;
    }
}
