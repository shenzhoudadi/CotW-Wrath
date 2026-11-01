using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicAfflictionTests
{
    private static int count;
    private static bool passed;
    private static RuleSavingThrow saving;
    private static MechanicsContext context;
    private static readonly List<ContextActionApplyBuff> applied=new List<ContextActionApplyBuff>();
    private static readonly List<ContextActionDealDamage> damaged=new List<ContextActionDealDamage>();
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);
    private static void Set(object o,string f,object v)=>AccessTools.Field(o.GetType(),f).SetValue(o,v);
    private static readonly Assembly Production=typeof(PsychicForceSword).Assembly;
    private static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(Production.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
    private static void Patch(Harmony h,MethodBase method,string name)=>h.Patch(method,prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicAfflictionTests),name)){priority=Priority.First});
    private static bool Saved(ref RuleSavingThrow __result){__result=saving;return false;}
    private static bool Passed(ref bool __result){__result=passed;return false;}
    private static bool Apply(ContextActionApplyBuff __instance){applied.Add(__instance);return false;}
    private static bool Damage(ContextActionDealDamage __instance){damaged.Add(__instance);return false;}
    private static bool NativeConcealment(ref Concealment __result){__result=baseConcealment;return false;}
    private static Concealment baseConcealment;
    internal static int Run(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        Console.WriteLine("Affliction spells: native rank/duration, seeded save branches, periodic callbacks and concealment");
        var h=new Harmony("CotW.Affliction.NativeBoundaries");
        context=Empty<MechanicsContext>();Set(context,"m_CasterRef",Get(f.Cast(SpellSchool.Divination,SpellDescriptor.MindAffecting,5).Context,"m_CasterRef"));Set(context,"m_MainTarget",new Kingmaker.Utility.TargetWrapper(f.Unit));context.SetParams(new AbilityParams());saving=Empty<RuleSavingThrow>();
        Patch(h,AccessTools.PropertyGetter(typeof(MechanicsContext),"SavingThrow"),nameof(Saved));
        Patch(h,AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));
        Patch(h,AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));
        Patch(h,AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));
        try {
            foreach(string token in new[]{"PainStrike","PainStrikeMass","Synesthesia","SynesthesiaMass"}) {
                var spell=Asset<BlueprintAbility>(cache,token);bool pain=token.StartsWith("Pain"),mass=token.EndsWith("Mass");
                var run=spell.GetComponent<AbilityEffectRunAction>();var rank=spell.GetComponent<ContextRankConfig>();
                Check(run.SavingThrowType==(pain?SavingThrowType.Fortitude:SavingThrowType.Will) && spell.SpellResistance,"Save type and SR: "+token);
                Check(spell.GetComponent<SpellComponent>().School==(pain?SpellSchool.Evocation:SpellSchool.Illusion),"CotW school: "+token);
                Check((SpellDescriptor)spell.GetComponent<SpellDescriptorComponent>().Descriptor==(pain?SpellDescriptor.Evil:SpellDescriptor.MindAffecting),"No reused foreign descriptor bit: "+token);
                Check(spell.Range==(mass?AbilityRange.Medium:AbilityRange.Close) && spell.CanTargetEnemies && !spell.CanTargetSelf,"Range/targeting: "+token);
                var targets=spell.GetComponent<AbilityTargetsAround>();
                Check(mass?(targets!=null && (TargetType)Get(targets,"m_TargetType")==TargetType.Enemy && ((Kingmaker.Utility.Feet)Get(targets,"m_Radius")).Value==15 && Get(targets,"m_Condition")!=null):targets==null,"15-foot enemy-only area, valid native predicate: "+token);
                foreach(bool success in new[]{false,true}) {
                    passed=success;applied.Clear();damaged.Clear();using(context.GetDataScope(null))run.Actions.Run();
                    Check(applied.Count==(success?0:1) && damaged.Count==0,"Save negates the whole debuff; no upfront tick: "+token);
                }
                var apply=(ContextActionApplyBuff)((ContextActionConditionalSaved)run.Actions.Actions.Single()).Failed.Actions.Single();
                foreach(int cl in new[]{1,5,10,11,20})foreach(bool extend in new[]{false,true}) {
                    context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:(Metamagic)0});
                    int value=rank.GetValue(context);Set(context,"m_Ranks",Enumerable.Repeat(value,32).ToArray());
                    Check(value==(pain?Math.Min(cl,10):cl),"Native CL rank: "+token+"/"+cl);
                    Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromSeconds(6*value*(extend?2:1)),"Native Extend duration: "+token+"/"+cl);
                }
                var buff=apply.Buff;Check(buff.Stacking==StackingType.Replace && Get(buff,"m_Flags").ToString().Contains("Harmful"),"Repeated casts replace the same debuff: "+token);
                if(pain) {
                    var wizard=spell.GetComponents<SpellListComponent>().Single(x=>x.SpellList.AssetGuid.ToString()=="ba0401fdeb4062f40a7aa95b6f07fe89");
                    Check(wizard.SpellLevel==(mass?5:3) && wizard.SpellList.SpellsByLevel[wizard.SpellLevel].Spells.Count(x=>x==spell)==1,"Wizard reciprocal circle: "+token);
                }
            }
            var painBuff=Asset<BlueprintBuff>(cache,"PainStrikeBuff");var periodic=painBuff.GetComponent<AddFactContextActions>();
            applied.Clear();damaged.Clear();using(context.GetDataScope(null))periodic.Activated.Run();
            Check(applied.Count==1 && applied[0].Buff.AssetGuid.ToString()=="4e42460798665fd4cb9173ffa7ada323" && applied[0].Permanent && applied[0].AsChild && applied[0].IsNotDispelable,"Native sickness is a parent-owned child, not a permanently independent status");
            using(context.GetDataScope(null)){periodic.NewRound.Run();periodic.NewRound.Run();}
            Check(damaged.Count==2 && damaged.All(x=>x.DamageType.Type==DamageType.Direct && x.Value.DiceType==DiceType.D6 && x.Value.DiceCountValue.Calculate(context)==1 && x.IgnoreCritical && !x.HalfIfSaved),"One noncritical 1d6 direct damage action per round, without another save");
            applied.Clear();damaged.Clear();using(context.GetDataScope(null)){periodic.Deactivated.Run();periodic.Dispose.Run();}
            Check(applied.Count==0 && damaged.Count==0,"Ending Pain Strike does not apply another debuff or damage tick");
            var senses=Asset<BlueprintBuff>(cache,"SynesthesiaBuff");
            Check(senses.GetComponents<AddStatBonus>().Select(x=>Tuple.Create(x.Stat,x.Value)).OrderBy(x=>x.Item1).SequenceEqual(new[]{Tuple.Create(StatType.AC,-4),Tuple.Create(StatType.SaveReflex,-4)}.OrderBy(x=>x.Item1)),"Exactly AC and Reflex -4, no unintended attack or Fortitude penalty");
            Check(senses.GetComponent<BuffAllSkillsBonus>().Value==-4 && senses.GetComponent<BuffAllSkillsBonus>().Multiplier.Calculate(context)==1,"All skills -4 once");
            Check(senses.GetComponents<AddCondition>().Select(x=>x.Condition).OrderBy(x=>x).SequenceEqual(new[]{UnitCondition.Slowed,UnitCondition.SpellCastingIsDifficult}.OrderBy(x=>x)),"Speed and casting difficulty; no blindness, stagger, or total-concealment overwrite");
            Synapse(cache);Concealment(cache,f,h);
        } finally {h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["afflictionAssertions"]=count;
        Console.WriteLine("PASS: "+count+" affliction checks; save outcomes seeded and final damage/buff installation intercepted; not gameplay.");return count;
    }
    private static void Synapse(BlueprintsCache cache)
    {
        var cast=Asset<BlueprintAbility>(cache,"SynapseOverload");var effect=Asset<BlueprintAbility>(cache,"SynapseOverloadTouch");
        Check(cast.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility==effect && cast.GetComponent<AbilityEffectRunAction>()==null,"Cast holds the new delivery, without dealing damage a second time");
        Check(effect.ComponentsArray.Any(x=>x.GetType().Name=="AbilityDeliverTouch") && effect.GetComponent<SpellListComponent>()==null,"Only the cast is learnable; delivery retains native touch attack");
        Check(cast.GetComponent<SpellListComponent>().SpellLevel==5,"Synapse is Psychic five");
        foreach(var spell in new[]{cast,effect})Check(spell.GetComponent<SpellComponent>().School==SpellSchool.Divination && (SpellDescriptor)spell.GetComponent<SpellDescriptorComponent>().Descriptor==SpellDescriptor.MindAffecting && spell.SpellResistance && (spell.AvailableMetamagic&Metamagic.Intensified)!=0,"Both cast and delivery preserve mind/SR/metamagic contracts");
        var rank=effect.GetComponent<ContextRankConfig>();var run=effect.GetComponent<AbilityEffectRunAction>();
        Check(run.SavingThrowType==SavingThrowType.Fortitude,"Fortitude partial");
        foreach(int cl in new[]{1,14,15,16,19,20,25})foreach(bool intense in new[]{false,true})foreach(bool success in new[]{false,true}) {
            context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=intense?Metamagic.Intensified:(Metamagic)0});
            int cap=intense?20:15;int value=rank.GetValue(context);Set(context,"m_Ranks",Enumerable.Repeat(value,32).ToArray());
            passed=success;applied.Clear();damaged.Clear();using(context.GetDataScope(null))run.Actions.Run();
            Check(damaged.Count==1 && damaged[0].Value.DiceCountValue.Calculate(context)==Math.Min(cl,cap) && damaged[0].Value.DiceType==DiceType.D6 && damaged[0].DamageType.Type==DamageType.Energy && damaged[0].DamageType.Energy==DamageEnergyType.Electricity && !damaged[0].HalfIfSaved,"Native damage cap/metamagic/save invariance: "+cl+"/"+intense+"/"+success);
            Check(applied.Count==(success?0:1),"Save gates only stagger");
            if(!success)Check(applied[0].Buff.AssetGuid.ToString()=="df3950af5a783bd4d91ab73eb8fa0fd3" && applied[0].DurationValue.Calculate(context).Seconds==TimeSpan.FromMinutes(1),"Native stagger for exactly one minute");
        }
    }
    private static void Concealment(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony h)
    {
        var manager=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",manager);var buffs=new BuffCollection();buffs.Initialize(manager);Set(f.Unit,"<Buffs>k__BackingField",buffs);
        var buff=Empty<Buff>();Set(buff,"<Blueprint>k__BackingField",Asset<BlueprintBuff>(cache,"SynesthesiaBuff"));Set(buff,"Components",new List<EntityFactComponent>());buff.RestoreOwnerLink(manager);
        Set(buff,"<IsTurnedOn>k__BackingField",true);
        // Seed only the original calculation (weather/scene visibility). Execute
        // the actual installed production postfix and real native ignore flags.
        Patch(h,AccessTools.Method(typeof(UnitPartConcealment),"Calculate"),nameof(NativeConcealment));
        var owner=new Harmony("CotW.Affliction.ProductionConcealment");owner.CreateClassProcessor(Production.GetType("CallOfTheWild.Wrath.Prototype.PsychicSynesthesiaConcealmentPatch")).Patch();
        try {
            var senses=f.Unit.Ensure<UnitPartConcealment>();
            foreach(bool active in new[]{false,true})foreach(bool attack in new[]{false,true})foreach(var original in new[]{Kingmaker.Enums.Concealment.None,Kingmaker.Enums.Concealment.Partial,Kingmaker.Enums.Concealment.Total})foreach(int mode in new[]{0,1,2}) {
                manager.List.Clear();if(active)manager.List.Add(buff);var cached=Get(manager,"m_Cache");AccessTools.Method(cached.GetType(),"Clear").Invoke(cached,null);
                Set(senses,"m_IgnoreAllCount",mode==1?1:0);Set(senses,"m_IgnorePartialCount",mode==2?1:0);baseConcealment=original;
                var actual=UnitPartConcealment.Calculate(f.Unit,f.Unit,attack);
                var expected=active&&attack&&original==Kingmaker.Enums.Concealment.None&&mode==0?Kingmaker.Enums.Concealment.Partial:original;
                Check(actual==expected,"Scoped outgoing miss chance preserves stronger results/ignore flags/other units: "+active+"/"+attack+"/"+original+"/"+mode);
            }
            Set(senses,"m_IgnoreAllCount",0);Set(senses,"m_IgnorePartialCount",0);baseConcealment=Kingmaker.Enums.Concealment.None;
            Set(buff,"<IsTurnedOn>k__BackingField",false);Check(UnitPartConcealment.Calculate(f.Unit,f.Unit,true)==Kingmaker.Enums.Concealment.None,"Turned-off status immediately stops the penalty");
        } finally {owner.UnpatchAll(owner.Id);}
    }
}


