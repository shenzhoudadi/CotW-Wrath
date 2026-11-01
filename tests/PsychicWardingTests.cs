using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicWardingTests
{
    static int count,applied,saves;static bool passed;static EntityFactsManager facts;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Warding/Daze: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicWardingWeapon).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);
        var h=new Harmony("CotW.Warding.Managed");
        try {
            h.Patch(AccessTools.Method(typeof(ContextActionSavingThrow),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicWardingTests),nameof(Save)));
            h.Patch(AccessTools.PropertyGetter(typeof(MechanicsContext),"SavingThrow"),prefix:new HarmonyMethod(typeof(PsychicWardingTests),nameof(Saving)));
            h.Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),prefix:new HarmonyMethod(typeof(PsychicWardingTests),nameof(Passed)));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicWardingTests),nameof(Apply)));
            Warding(cache,f);Daze(cache,f);
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["wardingSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Warding Weapon/Daze Mass checks; native event subscription and defensive casting, target predicates, seeded saves and duration math. Scene application/expiration and random saves substituted; not gameplay.");return count;
    }
    static void Warding(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintAbility>(cache,"WardingWeapon");var buff=Asset<BlueprintBuff>(cache,"WardingWeaponBuff");var run=bp.GetComponent<AbilityEffectRunAction>();var effect=(ContextActionApplyBuff)run.Actions.Actions.Single();
        Check(bp.School==SpellSchool.Abjuration && !bp.SpellResistance && run.SavingThrowType==SavingThrowType.Unknown,"Abjuration without save or SR");
        Check(bp.Range==AbilityRange.Personal && bp.CanTargetSelf&&!bp.CanTargetFriends&&!bp.CanTargetEnemies&&!bp.CanTargetPoint,"Personal spell");
        Check(effect.Buff==buff && effect.IsFromSpell&&!effect.AsChild&&!effect.Permanent&&!effect.IsNotDispelable&&buff.Stacking==StackingType.Replace,"Timed replaceable dispellable buff");
        Check(buff.ComponentsArray.Length==1 && buff.GetComponent<PsychicWardingWeapon>()!=null,"Only casting protection: no equipment or broad AoO immunity");
        Check(!ReferenceEquals(bp.Icon,null)&&ReferenceEquals(bp.Icon,buff.Icon),"Original CotW icon on spell and buff");
        Lists(bp,new[]{1,2,2});
        var ctx=f.Cast(SpellSchool.Abjuration,0,2).Context;Set(ctx,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        foreach(int cl in new[]{1,2,5,10,20,30})foreach(bool extend in new[]{false,true}) {
            ctx.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(ctx,"m_Ranks",Enumerable.Repeat(bp.GetComponent<ContextRankConfig>().GetValue(ctx),32).ToArray());
            Check(effect.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(cl*(extend?2:1)*6),"Native CL rounds/Extend");
        }
        applied=0;using(ctx.GetDataScope(f.Unit))run.Actions.Run();Check(applied==1,"Native cast action reaches buff delivery");
        var component=buff.GetComponent<PsychicWardingWeapon>();var runtime=Runtime(component,Fact(buff,ctx));
        var own=f.Cast(SpellSchool.Abjuration,0,2).Spell;var other=new Psychic070Tests.Caster(cache);Set(other.Unit,"<UniqueId>k__BackingField","warding-other");Set(other.Unit,"Facts",new EntityFactsManager(other.Unit));var foreign=other.Cast(SpellSchool.Abjuration,0,2).Spell;
        try {
            Check(!Event(f,own),"No protection before activation");runtime.TurnOn();Check(runtime.IsSubscribedOnEventBus && Event(f,own),"Native activation subscribes owner-specific handler");
            foreach(AbilityType type in Enum.GetValues(typeof(AbilityType))) {
                var old=own.Blueprint.Type;own.Blueprint.Type=type;
                Check(Event(f,own)==(type==AbilityType.Spell||type==AbilityType.SpellLike),"Only spells/SLAs protected: "+type);own.Blueprint.Type=old;
            }
            Check(!Event(other,foreign),"Another caster is not protected");
            var cmd=Empty<UnitUseAbility>();Set(cmd,"<Executor>k__BackingField",f.Unit);Set(cmd,"<Ability>k__BackingField",own);
            Check((bool)AccessTools.Method(typeof(UnitUseAbility),"TryCastingDefensively").Invoke(cmd,null),"Unmodified native command receives event and skips defensive roll");
            runtime.TurnOff();Check(!runtime.IsSubscribedOnEventBus && !Event(f,own),"Native suppression/removal unsubscribe restores ordinary behavior");
            runtime.TurnOn();Check(Event(f,own),"Unsuppression reactivates protection");runtime.Deactivate();Check(!Event(f,own),"Final deactivation removes protection");
            bool already=true;using(runtime.RequestEventContext())component.HandleUnitTryCastingDefensively(null,ref already);Check(already,"Never overrides another source's success");
        }finally{runtime.TurnOff();}
    }
    static bool Event(Psychic070Tests.Caster f,AbilityData data){bool success=false;EventBus.RaiseEvent<IUnitTryCastingDefensivelyHandler>(f.Unit,c=>c.HandleUnitTryCastingDefensively(data,ref success));return success;}
    static void Daze(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintAbility>(cache,"DazeMass");var buff=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("9934fedff1b14994ea90205d189c8759"));var run=bp.GetComponent<AbilityEffectRunAction>();
        Check(bp.School==SpellSchool.Enchantment&&bp.SpellResistance,"Enchantment/SR");Check(bp.Range==AbilityRange.Close&&bp.CanTargetPoint&&bp.CanTargetEnemies&&!bp.CanTargetFriends&&!bp.CanTargetSelf,"Enemy burst at close range");
        var around=bp.GetComponent<AbilityTargetsAround>();Check(((Feet)Get(around,"m_Radius")).Value==15&&(TargetType)Get(around,"m_TargetType")==TargetType.Enemy,"15-foot enemy selection");
        Check(bp.GetComponent<SpellDescriptorComponent>().Descriptor==(SpellDescriptor.MindAffecting|SpellDescriptor.Compulsion),"Compulsion/mind-affecting immunities retained");
        Check(buff.GetComponent<BuffStatusCondition>().Condition==UnitCondition.Dazed&&!buff.GetComponent<BuffStatusCondition>().SaveEachRound,"Dazed condition without repeated saves");
        Check(buff.GetComponent<SpellDescriptorComponent>()!=null&&buff.Stacking==StackingType.Replace,"Native immunity descriptors and nonstacking lifecycle retained");
        Lists(bp,new[]{3,4,4,4});
        var ctx=f.Cast(SpellSchool.Enchantment,0,3).Context;Set(ctx,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        string[] blocked={"3bec99efd9a363242a6c8d9957b75e91","fd389783027d63343b4a5634bd81645f","455ac88e22f55804ab87c2467deff1d6","018af8005220ac94a9a4f47b3e9c2b4e","9054d3988d491d944ac144e27b6bc318","706e61781d692a042b35941f14bc41c5"};
        string[] special={"a95311b3dc996964cbaa30ff9965aaf6","625827490ea69d84d8e599a33929fdc6","57614b50e8d86b24395931fffc5e409b","09478937695300944a179530664e42ec"};
        foreach(var kind in new[]{"humanoid"}.Concat(blocked).Concat(special))foreach(bool arcana in new[]{false,true})foreach(bool saved in new[]{false,true}) {
            facts.List.Clear();if(kind!="humanoid")AddFeature(cache,kind);if(arcana)AddFeature(cache,"02707231be1d3a74ba7e38a426c8df37");Refresh();passed=saved;saves=applied=0;
            using(ctx.GetDataScope(f.Unit))run.Actions.Run();bool eligible=!blocked.Contains(kind)&&(!special.Contains(kind)||arcana);
            Check(saves==(eligible?1:0)&&applied==(eligible&&!saved?1:0),"Actual native predicates and seeded save branches: "+kind+" arcana="+arcana+" saved="+saved);
        }
        facts.List.Clear();Refresh();
        var actions=Flatten(run.Actions).ToArray();var effect=actions.OfType<ContextActionApplyBuff>().First();
        Check(actions.OfType<ContextActionSavingThrow>().All(s=>s.Type==SavingThrowType.Will&&!s.HasCustomDC),"Native Will uses caster spell DC");
        Check(effect.Buff==buff&&!effect.Permanent&&!effect.AsChild&&effect.IsFromSpell&&!effect.IsNotDispelable,"Timed dispellable Daze buff");
        foreach(int cl in new[]{1,5,20,40})foreach(bool extend in new[]{false,true}) {ctx.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Check(effect.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(extend?12:6),"Fixed one round, Extend two, independent of CL");}
        Check(!bp.ComponentsArray.Any(c=>c.GetType().Name.Contains("HitDice"))&&!actions.Any(a=>a.GetType().Name.Contains("HitDice")),"No cantrip HD restriction");
        var donor=PsychicExpansionTests.NativeCase<BlueprintAbility>("55f14bc84d7c85446b07a1b5dd6b2b4c");Check(donor.ComponentsArray.Any(c=>c.GetType().Name.Contains("HitDice")),"Audited original Daze has an HD restriction, unlike new spell");
        Check(Asset<BlueprintBuff>(cache,"DazeMassBuff")!=buff,"Historical buff identity remains registered separately");
    }
    static void Lists(BlueprintAbility bp,int[] circles){var links=bp.GetComponents<SpellListComponent>().ToArray();Check(links.Select(l=>l.SpellLevel).OrderBy(l=>l).SequenceEqual(circles),"Exact class circles");foreach(var l in links)Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(a=>a==bp)==1,"Class list reciprocal and unique");}
    static IEnumerable<GameAction> Flatten(ActionList list){foreach(var a in list.Actions){yield return a;if(a is Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional c){foreach(var x in Flatten(c.IfTrue))yield return x;foreach(var x in Flatten(c.IfFalse))yield return x;}if(a is ContextActionSavingThrow s)foreach(var x in Flatten(s.Actions))yield return x;if(a is ContextActionConditionalSaved v){foreach(var x in Flatten(v.Succeed))yield return x;foreach(var x in Flatten(v.Failed))yield return x;}}}
    static void AddFeature(BlueprintsCache cache,string id){var fact=Empty<Feature>();Set(fact,"<Blueprint>k__BackingField",cache.Load(BlueprintGuid.Parse(id)));Set(fact,"Components",new List<EntityFactComponent>());Set(fact,"<IsActive>k__BackingField",true);fact.RestoreOwnerLink(facts);facts.List.Add(fact);}
    static bool Save(ContextActionSavingThrow __instance){saves++;__instance.Actions.Run();return false;}
    static bool Saving(ref RuleSavingThrow __result){__result=Empty<RuleSavingThrow>();return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}
    static bool Apply(){applied++;return false;}
}
