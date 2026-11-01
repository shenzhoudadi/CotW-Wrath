using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules.Abilities;
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
internal static class PsychicChannelVigorTests
{
    static int count;static EntityFactsManager facts;static MechanicsContext context;
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Check(bool b,string why){if(!b)throw new Exception("Channel Vigor: "+why);count++;}
    static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint=>(T)cache.Load((BlueprintGuid)AccessTools.Method(typeof(CallOfTheWild.Wrath.Prototype.PsychicBloodArmorDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static EntityFactComponent Runtime(BlueprintComponent c,EntityFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    static Buff Fact(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());Set(b,"m_Context",context);Set(b,"<Rank>k__BackingField",1);Set(b,"<IsActive>k__BackingField",true);b.RestoreOwnerLink(facts);return b;}
    static void ClearCache(){var c=AccessTools.Field(facts.GetType(),"m_Cache").GetValue(facts);AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static ModifiableValue Stat(Psychic070Tests.Caster f,StatType type){var stat=new ModifiableValue(f.Unit.Stats,type);stat.BaseValue=0;f.Unit.Stats.AllStats.RemoveAll(x=>x.Type==type);f.Unit.Stats.AllStats.Add(stat);return stat;}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);context=f.Cast(SpellSchool.Transmutation,0,3).Context;Set(context,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        var root=Asset<BlueprintAbility>(cache,"ChannelVigor");var variants=root.GetComponent<AbilityVariants>().Variants.ToArray();var buffs=new[]{"Limbs","Mind","Spirit","Torso"}.Select(s=>Asset<BlueprintBuff>(cache,"ChannelVigor"+s+"Buff")).ToArray();
        Check(variants.Length==4,"Four native suboptions");var lists=root.GetComponents<SpellListComponent>().ToArray();Check(lists.Length==4&&lists.All(l=>l.SpellLevel==3),"Four CotW class lists at third level");
        foreach(var l in lists)Check(l.SpellList.SpellsByLevel[3].Spells.Count(s=>s==root)==1,"Root is learnable exactly once");
        Check(!lists.Any(l=>l.SpellList.AssetGuid.ToString()==f.Book.Blueprint.SpellList.AssetGuid.ToString()),"No invented Psychic spell list entry");
        foreach(var spell in variants){
            Check(spell.Parent==root&&spell.Range==AbilityRange.Personal&&spell.CanTargetSelf&&!spell.CanTargetFriends&&!spell.CanTargetEnemies,"Personal child cast and parent identity");
            Check(spell.School==SpellSchool.Transmutation&&!spell.SpellResistance,"Transmutation no SR");
            var actions=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions;var removes=actions.OfType<ContextActionRemoveBuff>().ToArray();var apply=actions.OfType<ContextActionApplyBuff>().Single();
            Check(removes.Length==4&&removes.Select(a=>a.Buff).ToHashSet().SetEquals(buffs),"Only four own effects are removed; no native Haste deletion");
            Check(apply==actions.Last()&&buffs.Contains(apply.Buff)&&!apply.AsChild&&!apply.IsNotDispelable&&!apply.Permanent&&apply.IsFromSpell,"Remove old effect before independent timed application");
            Check(ReferenceEquals(spell.Icon,root.Icon)&&ReferenceEquals(apply.Buff.Icon,root.Icon),"Original Guidance icon propagated to choices and statuses");
            foreach(int cl in new[]{1,5,12,20,30})foreach(bool extended in new[]{false,true}){
                context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extended?Metamagic.Extend:0});Set(context,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(context),32).ToArray());
                Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromSeconds(6*cl*(extended?2:1)),"Native CL rounds/Extend");
            }
        }
        foreach(var bp in buffs.Skip(1))foreach(var bonus in bp.GetComponents<AddStatBonus>()){
            var stat=Stat(f,bonus.Stat);var runtime=Runtime(bonus,Fact(bp));
            for(int repeat=0;repeat<3;repeat++){
                runtime.TurnOn();Check(stat.ModifiedValue==bonus.Value,"Actual native stat gain "+bonus.Stat);
                var weaker=stat.AddModifier(2,(EntityFact)null,ModifierDescriptor.Competence);Check(stat.ModifiedValue==bonus.Value,"Competence bonuses take higher");
                runtime.TurnOff();Check(stat.ModifiedValue==2,"Native off retains other source");stat.RemoveModifier(weaker);Check(stat.ModifiedValue==0,"No residual own bonus");
            }
        }
        var haste=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("17113651fa8b40c2a7bbf9721462e646"));
        var c=buffs[0].GetComponent<CallOfTheWild.Wrath.Prototype.PsychicVigorHasteStats>();
        var statTypes=new[]{StatType.AC,StatType.SaveReflex,StatType.AdditionalAttackBonus};var stats=statTypes.Select(t=>Stat(f,t)).ToArray();var rt=Runtime(c,Fact(buffs[0]));
        for(int repeat=0;repeat<3;repeat++){
            var external=Fact(haste);rt.TurnOn();Check(stats.All(v=>v.ModifiedValue==1),"Limbs alone grants three bonuses");
            facts.List.Add(external);ClearCache();using(rt.RequestEventContext())c.HandleUnitGainFact(external);Check(stats.All(v=>v.ModifiedValue==0),"Yield to Haste arriving later");
            // Native lost notification may happen before the departing fact is detached.
            using(rt.RequestEventContext())c.HandleUnitLostFact(external);Check(stats.All(v=>v.ModifiedValue==1),"Resume without waiting for source list removal");
            facts.List.Remove(external);ClearCache();rt.TurnOff();Check(stats.All(v=>v.ModifiedValue==0),"Own cleanup leaves no modifiers");
            facts.List.Add(external);ClearCache();rt.TurnOn();Check(stats.All(v=>v.ModifiedValue==0),"Haste present before Limbs also prevents duplicate stats");
            var second=Fact(haste);facts.List.Add(second);ClearCache();using(rt.RequestEventContext())c.HandleUnitLostFact(external);Check(stats.All(v=>v.ModifiedValue==0),"Another Haste source still blocks duplicate stats");
            facts.List.Remove(external);ClearCache();using(rt.RequestEventContext())c.HandleUnitLostFact(second);Check(stats.All(v=>v.ModifiedValue==1),"Last Haste ending resumes own bonuses");facts.List.Remove(second);ClearCache();rt.TurnOff();Check(stats.All(v=>v.ModifiedValue==0),"Repeated cleanup is balanced");
        }
        var ranged=buffs[1].GetComponent<AttackTypeAttackBonus>();Check(ranged.Type==WeaponRangeType.Ranged&&ranged.AttackBonus*ranged.Value.Calculate(context)==4&&ranged.Descriptor==ModifierDescriptor.Competence&&!ranged.AllTypesExcept,"Native ranged-only competence predicate and nonzero value");
        var concentration=buffs[3].GetComponent<ConcentrationBonus>();var crt=Runtime(concentration,Fact(buffs[3]));var ev=new RuleCalculateAbilityParams(f.Unit,variants[3],f.Book);
        using(crt.RequestEventContext())concentration.OnEventAboutToTrigger(ev);
        Check((int)AccessTools.Field(typeof(RuleCalculateAbilityParams),"m_BonusConcentration").GetValue(ev)==6,"Native concentration callback adds six");
        Check(buffs[0].GetComponent<ChangeImpatience>().Delta==1,"Native haste animation tempo counter");
        PsychicExpansionTests.Inventory["channelVigorAssertions"]=count;Console.WriteLine("PASS: "+count+" Channel Vigor checks: variant/list wiring, exact removal scope, native duration/stat lifecycle and haste coexistence, concentration callback; managed objects, not gameplay.");return count;
    }
}
