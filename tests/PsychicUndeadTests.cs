using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicUndeadTests
{
    static int count,applied,removed;static bool passed;static EntityFactsManager facts;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Undead spells: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicForceSword).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);f.RestoreOwnerLink(facts);return f;}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);
        var h=new Harmony("CotW.Undead.Managed");
        try {
            h.Patch(AccessTools.PropertyGetter(typeof(MechanicsContext),"SavingThrow"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Saving)));
            h.Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Passed)));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Apply)));
            h.Patch(AccessTools.Method(typeof(ContextActionRemoveSelf),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Remove)));
            foreach(string token in new[]{"HaltUndead","ControlUndead"}) {
                bool halt=token=="HaltUndead";var bp=Asset<BlueprintAbility>(cache,token);var buff=Asset<BlueprintBuff>(cache,token+"Buff");
                Check(bp.School==SpellSchool.Necromancy && bp.SpellResistance && bp.GetComponent<SpellDescriptorComponent>()==null,"Necromancy with SR; no inherited mind-affecting tags");
                Check(bp.Range==(halt?AbilityRange.Medium:AbilityRange.Close) && bp.CanTargetPoint==halt && bp.CanTargetEnemies&&!bp.CanTargetSelf&&!bp.CanTargetFriends,"CotW target configuration");
                Check(buff.Stacking==StackingType.Replace && Get(buff,"m_Flags").ToString().Contains("Harmful") && buff.FxOnRemove!=null,"Replace, harmful, complete removal lifecycle");
                Check(!ReferenceEquals(bp.Icon,null)&&ReferenceEquals(bp.Icon,buff.Icon),"Original icon shared with buff");
                var links=bp.GetComponents<SpellListComponent>().ToArray();Check(links.Length==(halt?2:1) && links.All(c=>c.SpellLevel==(halt?3:7)),"Only CotW native class spell lists, correct circles");
                foreach(var link in links)Check(link.SpellList.SpellsByLevel[link.SpellLevel].Spells.Count(x=>x==bp)==1,"Native list includes spell exactly once");
                var run=bp.GetComponent<AbilityEffectRunAction>();Check(run.SavingThrowType==SavingThrowType.Will,"Will negates");
                var effect=(ContextActionApplyBuff)((ContextActionConditionalSaved)run.Actions.Actions.Single()).Failed.Actions.Single();
                Check(effect.Buff==buff && effect.IsFromSpell && !effect.Permanent && !effect.IsNotDispelable && !effect.AsChild,"Timed dispellable buff has independent duration");
                var ctx=f.Cast(SpellSchool.Necromancy,0,halt?3:7).Context;Set(ctx,"AssociatedBlueprint",bp);Set(ctx,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
                foreach(bool saved in new[]{false,true}) {passed=saved;applied=0;using(ctx.GetDataScope(f.Unit))run.Actions.Run();Check(applied==(saved?0:1),"Native saved/failed branches");}
                foreach(int cl in new[]{1,3,7,14,20,30})foreach(bool extend in new[]{false,true}) {
                    ctx.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(ctx,"m_Ranks",Enumerable.Repeat(bp.GetComponent<ContextRankConfig>().GetValue(ctx),32).ToArray());
                    Check(effect.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(cl*(extend?2:1)*(halt?6:60)),"Native caster level and Extend duration");
                }
                if(halt) {
                    Check(buff.GetComponent<BuffStatusCondition>().Condition==UnitCondition.CantAct && buff.GetComponent<BuffStatusCondition>().SaveEachRound,"Native action restriction and repeated save; no paralysis condition immunity conflict");
                    Check(buff.GetComponent<SpellDescriptorComponent>().Descriptor==SpellDescriptor.MovementImpairing,"Undead spell does not inherit paralysis/mind descriptor immunity");
                    var around=bp.GetComponent<AbilityTargetsAround>();Check(((Feet)Get(around,"m_Radius")).Value==10 && (TargetType)Get(around,"m_TargetType")==TargetType.Enemy,"Ten-foot enemy burst");
                    var condition=(Kingmaker.ElementsSystem.ConditionsChecker)Get(around,"m_Condition");
                    using(ctx.GetDataScope(f.Unit)){Check(!condition.Check(),"Living creature excluded");AddUndead(cache);Check(condition.Check(),"Undead included by actual native predicate");}
                    Damage(buff,ctx,f);Control(buff,ctx,f,h);facts.List.Clear();Refresh();
                } else {
                    var checks=bp.GetComponents<AbilityTargetHasFact>().ToArray();Check(checks.Length==2 && !checks.All(c=>c.Inverted),"Undead required and summons excluded");
                    Check(!checks.All(c=>c.IsTargetRestrictionPassed(f.Unit,new TargetWrapper(f.Unit))),"Living target rejected");AddUndead(cache);
                    Check(checks.All(c=>c.IsTargetRestrictionPassed(f.Unit,new TargetWrapper(f.Unit))),"Natural undead accepted");
                    facts.List.Add(Fact((BlueprintBuff)cache.Load(BlueprintGuid.Parse("8728e884eeaa8b047be04197ecf1a0e4")),ctx));Refresh();
                    Check(!checks.All(c=>c.IsTargetRestrictionPassed(f.Unit,new TargetWrapper(f.Unit))),"Summoned undead rejected");
                    Check(buff.ComponentsArray.Length==1 && buff.GetComponent<ChangeFaction>()!=null && Get(buff.GetComponent<ChangeFaction>(),"m_Type").ToString()=="ToCaster","Native domination owns faction restoration and serialization");
                    Check(!(bool)Get(buff.GetComponent<ChangeFaction>(),"m_AllowDirectControl"),"Native domination AI, not player party recruitment");
                    facts.List.Clear();Refresh();
                }
            }
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["undeadSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" undead spell checks; native targeting, seeded save branches, durations and damage callback. Scene buff installation/removal and faction AI not executed; not gameplay.");return count;
    }
    static EntityFactComponent[] activeControl;
    static void Control(BlueprintBuff bp,MechanicsContext ctx,Psychic070Tests.Caster f,Harmony h)
    {
        int before=count;
        h.Patch(AccessTools.Method(typeof(UnitState),"UpdateStatusEffect"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Skip)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game),"CurrentMode"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Mode)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitState),"IsHelpless"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(False)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitState),"IsAble"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(True)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitState),"AnimationManager"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Animation)));
        h.Patch(AccessTools.PropertyGetter(typeof(UnitDescriptor),"Encumbrance"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Encumbrance)));
        h.Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(Skip)));
        h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(PsychicUndeadTests),nameof(RemoveControl)));
        var state=f.Unit.State;Set(state,"<Owner>k__BackingField",f.Descriptor);
        Set(state,"m_Conditions",new sbyte[128]);Set(state,"m_ConditionsImmunity",new sbyte[128]);Set(state,"m_ConditionsExceptions",new List<UnitConditionExceptions>[128]);
        var fact=Fact(bp,ctx);
        // Real native racial immunity component and native condition counters.
        var immune=Runtime(new AddConditionImmunity {Condition=UnitCondition.Paralyzed},fact);immune.TurnOn();
        var old=Runtime(new BuffStatusCondition {Condition=UnitCondition.Paralyzed,SaveEachRound=true,SaveType=SavingThrowType.Will},fact);
        old.TurnOn();Check(!state.HasCondition(UnitCondition.Paralyzed)&&state.CanAct&&state.CanMove,"Reproduced old cosmetic hold under native paralysis immunity");old.TurnOff();
        Check(bp.GetComponents<AddCondition>().Select(c=>c.Condition).OrderBy(x=>x).SequenceEqual(new[]{UnitCondition.CantMove,UnitCondition.Helpless}),"Separate movement/helpless restrictions");
        var status=bp.GetComponent<BuffStatusCondition>();activeControl=bp.ComponentsArray.Where(c=>c is BuffStatusCondition||c is AddCondition).Select(c=>Runtime(c,fact)).ToArray();
        foreach(int immunitySources in new[]{1,2})foreach(bool otherSource in new[]{false,true}){
            // Additional immunity sources must survive hold cleanup as well.
            var secondImmunity=Runtime(new AddConditionImmunity {Condition=UnitCondition.Paralyzed},fact);
            if(immunitySources==2)secondImmunity.TurnOn();
            if(otherSource)state.AddCondition(UnitCondition.CantMove);
            for(int repeat=0;repeat<3;repeat++){
                foreach(var rt in activeControl)rt.TurnOn();
                Check(!state.CanAct&&!state.CanActInTurnBased&&!state.CanMove,"Native realtime/turn-based/movement guards block held undead");
                Check(state.HasCondition(UnitCondition.Helpless)&&!state.HasCondition(UnitCondition.Paralyzed),"Held vulnerability without enabling paralysis");
                Check(((sbyte[])Get(state,"m_ConditionsImmunity"))[(int)UnitCondition.Paralyzed]==immunitySources,"Racial paralysis immunity untouched");
                passed=false;removed=0;
                using(activeControl[0].RequestEventContext())((Kingmaker.Controllers.Units.ITickEachRound)status).OnNewRound();
                Check(removed==0&&!state.CanAct,"Failed round save retains restrictions");
                passed=true;
                using(activeControl[0].RequestEventContext())((Kingmaker.Controllers.Units.ITickEachRound)status).OnNewRound();
                Check(removed==1&&state.CanAct&&state.CanActInTurnBased,"Successful round save releases native action guards");
                Check(state.CanMove==!otherSource,"Cleanup preserves unrelated movement restriction");
                Check(!state.HasCondition(UnitCondition.Helpless),"No helpless residue after release");
            }
            if(otherSource)state.RemoveCondition(UnitCondition.CantMove);
            if(immunitySources==2)secondImmunity.TurnOff();
        }
        immune.TurnOff();Check(((sbyte[])Get(state,"m_Conditions")).All(x=>x==0),"Balanced native condition counters after repeated lifecycle");
        PsychicExpansionTests.Inventory["haltControlAssertions"]=count-before;
    }
    static bool Skip()=>false;
    static bool True(ref bool __result){__result=true;return false;}
    static bool False(ref bool __result){__result=false;return false;}
    static bool Mode(ref Kingmaker.GameModes.GameModeType __result){__result=Kingmaker.GameModes.GameModeType.Default;return false;}
    static bool Animation(ref Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager __result){__result=null;return false;}
    static bool Encumbrance(ref Kingmaker.UnitLogic.Encumbrance __result){__result=0;return false;}
    static bool RemoveControl(){removed++;foreach(var rt in activeControl)rt.TurnOff();return false;}
    static void AddUndead(BlueprintsCache cache) {var fact=Empty<Feature>();Set(fact,"<Blueprint>k__BackingField",cache.Load(BlueprintGuid.Parse("734a29b693e9ec346ba2951b27987e33")));Set(fact,"Components",new List<EntityFactComponent>());Set(fact,"<IsActive>k__BackingField",true);fact.RestoreOwnerLink(facts);facts.List.Add(fact);Refresh();}
    static void Damage(BlueprintBuff bp,MechanicsContext ctx,Psychic070Tests.Caster f)
    {
        var fact=Fact(bp,ctx);var c=bp.GetComponent<AddIncomingDamageTrigger>();var runtime=Runtime(c,fact);
        foreach(int damage in new[]{0,1,20}) {var evt=Empty<RuleDealDamage>();Set(evt,"<Result>k__BackingField",damage);Set(evt,"m_DamageBundle",new DamageBundle());removed=0;using(runtime.RequestEventContext())using(ctx.GetDataScope(f.Unit))c.OnEventDidTrigger(evt);Check(removed==(damage>0?1:0),"Native damage trigger removes only after positive damage");}
        var attack=bp.GetComponent<AddTargetAttackRollTrigger>();Check(!attack.OnlyHit && !attack.CriticalHit && !attack.OnlyMelee && attack.ActionOnSelf.Actions.Single() is ContextActionRemoveSelf,"Any hostile attack roll also ends hold, including miss");
    }
    static bool Saving(ref RuleSavingThrow __result){__result=Empty<RuleSavingThrow>();return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}
    static bool Apply(){applied++;return false;}
    static bool Remove(){removed++;return false;}
}
