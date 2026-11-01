using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicForceTests
{
    private static int count,reflexChecks,prone;
    private static bool passed,reflexPassed;
    private static RuleSavingThrow saving;
    private static readonly List<ContextActionDealDamage> damage=new List<ContextActionDealDamage>();
    private static readonly List<ContextActionApplyBuff> buffs=new List<ContextActionApplyBuff>();
    private static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    private static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
    private static bool Saved(ref RuleSavingThrow __result){__result=saving;return false;}
    private static bool Passed(ref bool __result){__result=passed;return false;}
    private static bool Damage(ContextActionDealDamage __instance){damage.Add(__instance);return false;}
    private static bool Buff(ContextActionApplyBuff __instance){buffs.Add(__instance);return false;}
    private static bool Prone(){prone++;return false;}
    private static bool Reflex(ContextActionSavingThrow __instance){Check(__instance.Type==SavingThrowType.Reflex && damage.Count==1,"Secondary Reflex occurs after damage");reflexChecks++;bool old=passed;passed=reflexPassed;try{__instance.Actions.Run();}finally{passed=old;}return false;}
    internal static int Run(BlueprintsCache cache)
    {
        var h=new Harmony("CotW.ForceSpells.NativeBranches");saving=Empty<RuleSavingThrow>();
        void Patch(MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(AccessTools.Method(typeof(PsychicForceTests),n)) {priority=Priority.First});
        Patch(AccessTools.PropertyGetter(typeof(MechanicsContext),"SavingThrow"),nameof(Saved));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));
        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Buff));
        Patch(AccessTools.Method(typeof(ContextActionKnockdownTarget),"RunAction"),nameof(Prone));Patch(AccessTools.Method(typeof(ContextActionSavingThrow),"RunAction"),nameof(Reflex));
        try {
            var ctx=Empty<MechanicsContext>();var caster=new Psychic070Tests.Caster(cache);
            Set(ctx,"m_CasterRef",Get(caster.Cast(SpellSchool.Evocation,SpellDescriptor.Force,5).Context,"m_CasterRef"));
            foreach(bool storm in new[]{false,true}) {
                var bp=Asset<BlueprintAbility>(cache,storm?"TelekineticStorm":"BurstOfForce");var run=bp.GetComponent<AbilityEffectRunAction>();var rank=bp.GetComponent<ContextRankConfig>();var targets=bp.GetComponent<AbilityTargetsAround>();
                Check(bp.Range==AbilityRange.Personal && run.IgnoreCaster && bp.CanTargetSelf && bp.CanTargetEnemies && bp.CanTargetFriends==!storm,"Caster-centered AoE permits intended sides but excludes self");
                Check(((Kingmaker.Utility.Feet)Get(targets,"m_Radius")).Value==(storm?40:20) && (TargetType)Get(targets,"m_TargetType")== (storm?TargetType.Enemy:TargetType.Any) && !((ConditionsChecker)Get(targets,"m_Condition")).HasConditions,"Exact native radius and side filter");
                Check(run.SavingThrowType==SavingThrowType.Fortitude && bp.SpellResistance && bp.GetComponent<SpellComponent>().School==SpellSchool.Evocation && (SpellDescriptor)bp.GetComponent<SpellDescriptorComponent>().Descriptor==SpellDescriptor.Force,"Force, evocation, Fortitude and spell resistance");
                Check(bp.GetComponent<SpellListComponent>().SpellLevel==(storm?9:5) && bp.GetComponents<SpellListComponent>().Count()==(storm?1:2),"Correct Psychic/Wizard level links");
                foreach(int cl in new[]{1,14,15,16,20,21,25,30})foreach(bool intensified in new[]{false,true})foreach(bool fort in new[]{false,true})foreach(bool reflex in new[]{false,true}) {
                    ctx.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=intensified?Metamagic.Intensified:0});
                    int dice=rank.GetValue(ctx),cap=(storm?20:15)+(intensified?5:0);Set(ctx,"m_Ranks",Enumerable.Repeat(dice,32).ToArray());
                    damage.Clear();buffs.Clear();prone=reflexChecks=0;passed=fort;reflexPassed=reflex;
                    using(ctx.GetDataScope(null))run.Actions.Run();
                    Check(damage.Count==1 && damage[0].Value.DiceCountValue.Calculate(ctx)==Math.Min(cl,cap) && damage[0].Value.DiceType==DiceType.D6 && damage[0].DamageType.Type==DamageType.Force && damage[0].HalfIfSaved && damage[0].IsAoE,"Capped/intensified force damage retains native save-half behavior");
                    Check(reflexChecks==(!storm&&!fort?1:0) && prone==(!storm&&!fort&&!reflex?1:0),"Only failed Fortitude on Burst triggers Reflex; both failures cause prone");
                    Check(buffs.Count==(storm&&!fort?2:0),"Storm adds both conditions only after failed Fortitude");
                    foreach(var b in buffs)Check(b.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(6) && b.Buff.GetComponent<AddCondition>()!=null,"Storm's native conditions last exactly one round");
                }
                var descriptors=storm?((ContextActionConditionalSaved)run.Actions.Actions[1]).Failed.Actions.Cast<ContextActionApplyBuff>().First().Buff.GetComponent<SpellDescriptorComponent>():null;
                Check(!storm || (SpellDescriptor)descriptors.Descriptor==SpellDescriptor.Daze,"Force daze does not acquire mind-affecting immunity");
                Check(!ReferenceEquals(bp.Icon,null) && bp.GetComponent<AbilitySpawnFx>()!=null && !string.IsNullOrEmpty(bp.LocalizedSavingThrow.Key),"Visual and save metadata supplied");
            }
        } finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["forceSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" force spell checks; native ranks/conditional actions, seeded saves and final delivery boundaries; not game combat.");return count;
    }
}
