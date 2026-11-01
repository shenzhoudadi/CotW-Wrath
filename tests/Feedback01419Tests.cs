using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class Feedback01419Tests
{
    static int n,trips; static RuleCombatManeuver last;
    static ContextActionDealDamage action; static RuleDealDamage damage;
    static UnitEntityData caster,target;
    static T Empty<T>() where T:class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string name,object value)=>(AccessTools.Field(o.GetType(),name)??AccessTools.Field(o.GetType(),"<"+name+">k__BackingField")).SetValue(o,value);
    static void Check(bool b,string m){if(!b)throw new Exception("Feedback01419: "+m);n++;}
    static object Helper(string name,params object[] args)=>AccessTools.Method(typeof(SpellAndSword01417Tests),name).Invoke(null,args);
    static bool Skip()=>false;
    static bool Trip(RuleCombatManeuver __0,ref RuleCombatManeuver __result){trips++;last=__0;__result=__0;return false;}
    sealed class NativeDamageAction:GameAction
    {
        public override string GetCaption()=>"Offline native damage rule capture";
        public override void RunAction(){try{damage=action.GetDamageRule(caster,target);}catch(Exception e){Console.Error.WriteLine("Native force damage construction: "+e);foreach(var frame in new System.Diagnostics.StackTrace(e).GetFrames())Console.Error.WriteLine(frame.GetMethod()+" IL_"+frame.GetILOffset().ToString("x4"));throw;}}
    }
    static void Deliver(Feature fact,RuleDealDamage rule)
    {
        var runtime=fact.Components.Single(c=>c.SourceBlueprintComponent is TopplingSpellDamage);
        using(runtime.RequestEventContext())((TopplingSpellDamage)runtime.SourceBlueprintComponent).OnEventDidTrigger(rule);
    }
    internal static int Run(BlueprintsCache cache)
    {
        n=trips=0; var h=new Harmony("CotW01419.NativeFeedback");
        h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(Feedback01419Tests),nameof(Skip)));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleCombatManeuver),AccessTools.Method(typeof(Feedback01419Tests),nameof(Trip)));
        try {
            var production=typeof(TopplingSpellDamage).Assembly;
            foreach(var name in new[]{"LinnormFeats","SpellAndSwordFeats"})AccessTools.Method(production.GetType("CallOfTheWild.Wrath.Prototype."+name),"Register").Invoke(null,new object[]{cache});
            caster=(UnitEntityData)Helper("Unit",cache,"01419-caster");target=(UnitEntityData)Helper("Unit",cache,"01419-target");
            Set(caster,"Parts",new Kingmaker.EntitySystem.EntityPartsManager(caster));Set(target,"Parts",new Kingmaker.EntitySystem.EntityPartsManager(target));
            var book=(Spellbook)Helper("Book",caster,true);
            var spell=Psychic034Tests.Asset<BlueprintAbility>(cache,"ForcefulStrike");
            var buffBlueprint=Psychic034Tests.Asset<BlueprintBuff>(cache,"ForcefulStrikeBuff");
            var feat=(Feature)Helper("Fact",caster,Psychic034Tests.Asset<BlueprintFeature>(cache,"TopplingSpellFeat"));
            var saving=buffBlueprint.GetComponent<PsychicForcefulStrikeHit>().Action.Actions.OfType<ContextActionSavingThrow>().Single();
            action=saving.Actions.Actions.OfType<ContextActionDealDamage>().Single();
            var weapon=(Kingmaker.Items.ItemEntityWeapon)Helper("Weapon",caster,Kingmaker.Enums.WeaponCategory.Longsword,"01419-weapon");
            // A weapon-triggered action inherits ContextAttackData. Exercise the
            // real native GetDamageRule reason selection, not a manually supplied
            // spell reason; that was the gap in the previous direct-damage tests.
            foreach(var mask in new[]{(Metamagic)(1<<28),(Metamagic)0})
            foreach(int cl in new[]{1,7,15}) {
                var ability=(AbilityData)Helper("Learn",caster,book,spell,4);ability.MetamagicData=new MetamagicData();ability.MetamagicData.Add(mask);
                var context=new AbilityExecutionContext(ability,new AbilityParams{CasterLevel=cl,SpellLevel=4,Metamagic=mask},new TargetWrapper(caster));
                var buff=Psychic034Tests.Fact(caster,buffBlueprint,caster,cl);
                Set(buff,"m_Context",context.CloneFor(buffBlueprint,caster.Descriptor,caster.Descriptor));
                var roll=Empty<RuleAttackRoll>();roll.Reason=new RuleReason(new MechanicsContext(caster,caster.Descriptor,new BlueprintAbility{Type=AbilityType.Spell,ComponentsArray=Array.Empty<BlueprintComponent>()}));
                var stats=Empty<RuleCalculateWeaponStats>();Set(stats,"Weapon",weapon);Set(roll,"WeaponStats",stats);
                using(ContextAttackData.Request().Setup(roll,null,0,0))
                    buff.RunActionInContext(new ActionList{Actions=new GameAction[]{new NativeDamageAction()}},new TargetWrapper(target));
                Check(damage.SourceAbility==spell,"native force damage source ability retained");
                Check(damage.Reason?.Context?.SourceAbility==spell,"force damage reason belongs to spell buff rather than weapon attack");
                Check(damage.Reason.Context.HasMetamagic((Metamagic)(1<<28))==(mask!=0),"recipe mask retained on real damage reason");
                int before=trips;Set(damage,"Result",5);Deliver(feat,damage);
                Check(trips==before+(mask==0?0:1),"Toppling on discharged force damage");
                if(mask!=0) {
                    Check(last.Type==CombatManeuver.Trip&&last.ReplaceAttackBonus==cl&&last.ReplaceBaseStat==StatType.Charisma,"source spellbook attribute and CL, including uncapped CL");
                    Deliver(feat,damage);Check(trips==before+1,"same discharge target trips once");
                }
                Set(damage,"Result",0);Deliver(feat,damage);Check(trips==before+(mask==0?0:1),"zero force damage cannot trip");
            }
            var nativeOpportunity=new UnitAttackOfOpportunity(target);
            Check(nativeOpportunity.IsOneFrameCommand,"native AoO settles as one-frame command; animation is an idle presentation action");
            Check(typeof(UnitAttackOfOpportunity).GetMethod("OnRun").DeclaringType==typeof(UnitAttackOfOpportunity),"native presentation path retained");
        } finally {h.UnpatchAll(h.Id);action=null;damage=null;caster=target=null;}
        PsychicExpansionTests.Inventory["feedback01419Assertions"]=n;
        Console.WriteLine("PASS: "+n+" 01419 native Forceful Strike damage reasons, metamagic/CL/attribute/once-only checks; native AoO presentation retained. Final maneuver/scene endpoints bounded.");return n;
    }
}
