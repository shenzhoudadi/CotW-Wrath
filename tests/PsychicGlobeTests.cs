using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicGlobeTests
{
    static int count;static int dispelRoll;static EntityFactsManager facts;static Game game;
    static readonly Type Module=typeof(PsychicBloodArmorDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicGlobeSpells");
    static readonly MethodInfo Block=AccessTools.Method(Module,"Blocks"),Delivery=AccessTools.Method(Module.GetNestedType("DeliveryPatch",BindingFlags.NonPublic),"Prefix");
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Globe: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static void Refresh(){var cache=Get(facts,"m_Cache");AccessTools.Method(cache.GetType(),"Clear").Invoke(cache,null);}
    static Buff Add(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());Set(b,"<IsTurnedOn>k__BackingField",true);b.RestoreOwnerLink(facts);facts.List.Add(b);Refresh();return b;}
    static bool Blocks(UnitEntityData unit,MechanicsContext ctx,AreaEffectEntityData area=null)=>(bool)Block.Invoke(null,new object[]{unit,ctx,area});
    static bool Allows(AbilityExecutionContext ctx,UnitEntityData target,AbilityApplyEffect effect=null)=>(bool)Delivery.Invoke(null,new object[]{ctx,new TargetWrapper(target),effect});
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",buffs);
        var h=new Harmony("CotW.Globe.NativeFixture");
        try {
            h.CreateClassProcessor(Module.GetNestedType("DeliveryPatch",BindingFlags.NonPublic)).Patch();h.CreateClassProcessor(Module.GetNestedType("AreaPatch",BindingFlags.NonPublic)).Patch();
            Metadata(cache,f);Protection(cache,f);Areas(cache,f,h);Dispel(cache,f,h);
        }finally{h.UnpatchAll(h.Id);facts.List.Clear();Refresh();}
        PsychicExpansionTests.Inventory["globeSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Globe checks; native blocked-delivery hook, area resistance entry, area-owned overlap cleanup, duration and level/type boundaries. Scene creation/removal and dispel dice replaced; not gameplay.");return count;
    }
    static void Metadata(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        foreach(var token in new[]{"GlobeOfInvulnerabilityLesser","GlobeOfInvulnerability"}) {
            var spell=Asset<BlueprintAbility>(cache,token);int level=token.EndsWith("Lesser")?4:6;
            var spawn=(ContextActionSpawnAreaEffect)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();var area=spawn.AreaEffect;
            Check(spell.School==SpellSchool.Abjuration&&!spell.SpellResistance&&spell.Range==AbilityRange.Personal&&spell.CanTargetSelf&&!spell.CanTargetPoint&&!spell.CanTargetFriends&&!spell.CanTargetEnemies&&!spell.IsFullRoundAction,"Personal standard Abjuration, no save/SR");
            Check(!spawn.OnUnit&&area.Size.Value==13&&area.Shape==AreaEffectShape.Cylinder&&area.CanTargetAllies&&area.CanTargetEnemies&&!area.AggroEnemies&&!area.SpellResistance,"Stationary CotW13ft cylinder protects all sides");
            Check(area.GetComponent<AbilityAreaEffectBuff>().Buff.Stacking==StackingType.Stack,"Each area owns independent fact");
            Check(!ReferenceEquals(spell.Icon,null)&&ReferenceEquals(spell.Icon,area.GetComponent<AbilityAreaEffectBuff>().Buff.Icon),"CotW icon on root and buff");
            Check(spell.AvailableMetamagic==(Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken),"CotW metamagic set");
            Check(spell.GetComponents<SpellListComponent>().Count()==2,"Psychic and Wizard/Sorcerer lists");
            foreach(var link in spell.GetComponents<SpellListComponent>())Check(link.SpellLevel==level&&link.SpellList.SpellsByLevel[level].Spells.Count(x=>x==spell)==1,"Correct reciprocal spell list exactly once");
            foreach(int cl in new[]{1,4,6,10,20,30})foreach(bool extend in new[]{false,true}) {
                var ctx=f.Cast(SpellSchool.Abjuration,0,level).Context;Set(ctx,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));ctx.SetParams(new AbilityParams {CasterLevel=cl,SpellLevel=level,Metamagic=extend?Metamagic.Extend:0});Set(ctx,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(ctx),32).ToArray());
                Check(spawn.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(6*cl*(extend?2:1)),"Native CL rounds/Extend");
            }
        }
    }
    static void Protection(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var outsider=new Psychic070Tests.Caster(cache);Set(outsider.Unit,"<UniqueId>k__BackingField","outside-globe");
        foreach(var token in new[]{"GlobeOfInvulnerabilityLesserBuff","GlobeOfInvulnerabilityBuff"}) {
            var protection=Add(Asset<BlueprintBuff>(cache,token));int max=token.Contains("Lesser")?3:4;
            foreach(AbilityType type in Enum.GetValues(typeof(AbilityType)))foreach(int level in Enumerable.Range(0,10))foreach(bool resistance in new[]{false,true}) {
                var cast=f.Cast(SpellSchool.Evocation,0,level);cast.Spell.Blueprint.Type=type;cast.Spell.Blueprint.SpellResistance=resistance;
                bool expected=(type==AbilityType.Spell||type==AbilityType.SpellLike)&&level<=max;
                Check(Blocks(f.Unit,cast.Context)==expected,"Target type/level/SR-independent protection");
                Check(Allows(cast.Context,f.Unit)==!expected,"Actual production delivery prefix matches target protection");
                Check(!Blocks(outsider.Unit,cast.Context),"Casting out of globe remains allowed");
            }
            var low=f.Cast(SpellSchool.Enchantment,0,3).Context;
            foreach(Metamagic meta in new[]{Metamagic.Empower,Metamagic.Quicken,Metamagic.Reach,Metamagic.Persistent}){low.SetParams(new AbilityParams {SpellLevel=3,Metamagic=meta});Check(Blocks(f.Unit,low),"Slot-raising metamagic without Heighten remains third-level");}
            low.SetParams(new AbilityParams {SpellLevel=max+1,Metamagic=Metamagic.Heighten});Check(!Blocks(f.Unit,low),"Effective Heightened level passes lower-level globe");
            low.SetParams(new AbilityParams {SpellLevel=1});
            Set(protection,"<IsTurnedOn>k__BackingField",false);Check(!Blocks(f.Unit,low),"Turned-off protection absent");Set(protection,"<IsTurnedOn>k__BackingField",true);
            Set(protection,"<IsSuppressed>k__BackingField",true);Check(!Blocks(f.Unit,low),"Suppressed protection absent");Set(protection,"<IsSuppressed>k__BackingField",false);
            var apply=AccessTools.Method(typeof(AbilityExecutionProcess),"DoApplyEffect");apply.Invoke(null,new object[]{low,new TargetWrapper(f.Unit),null});Check(true,"Actual patched native delivery exits before effect or game scene services");
            var root=f.Cast(SpellSchool.Evocation,0,2);var sticky=new AbilityEffectStickyTouch();root.Spell.Blueprint.ComponentsArray=new BlueprintComponent[]{sticky};root.Spell.Blueprint.OnEnable();
            Check(Allows(root.Context,f.Unit),"Touch-charge preparation is not blocked");
            var spawn=new AbilityEffectRunAction {Actions=new ActionList {Actions=new GameAction[]{new ContextActionSpawnAreaEffect()}}};Check(Allows(low,f.Unit,spawn),"Persistent area preparation is not blocked");
            Check(!Allows(low,f.Unit,new AbilityEffectRunAction {Actions=new ActionList {Actions=new GameAction[]{new ContextActionSpawnAreaEffect(),new ContextActionApplyBuff()}}}),"Mixed effect chain is not blanket-whitelisted");
            facts.List.Remove(protection);Refresh();Check(!Blocks(f.Unit,low),"Removed protection no longer applies");
        }
        Check(!Blocks(f.Unit,null)&&!Blocks(null,f.Cast(SpellSchool.Evocation,0,1).Context),"Missing context/target preserves native behavior");
    }
    static void Areas(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(PsychicGlobeTests),nameof(AddAreaBuff)){priority=Priority.First});
        h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(PsychicGlobeTests),nameof(RemoveAreaBuff)){priority=Priority.First});
        var state=Empty<PersistentState>();var pool=Empty<EntityPool<AreaEffectEntityData>>();var entities=new List<AreaEffectEntityData>();Set(pool,"m_Entities",entities);Set(state,"AreaEffects",pool);game=Empty<Game>();Set(game,"State",state);
        h.Patch(AccessTools.PropertyGetter(typeof(Game),"Instance"),prefix:new HarmonyMethod(typeof(PsychicGlobeTests),nameof(GameInstance)){priority=Priority.First});
        var lowArea=Asset<BlueprintAbilityAreaEffect>(cache,"GlobeOfInvulnerabilityLesserArea");var highArea=Asset<BlueprintAbilityAreaEffect>(cache,"GlobeOfInvulnerabilityArea");
        var a=Area("globe-a",f.Cast(SpellSchool.Abjuration,0,4).Context,lowArea,100);var b=Area("globe-b",f.Cast(SpellSchool.Abjuration,0,6).Context,highArea,120);entities.AddRange(new[]{a,b});
        var enter=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter");var exit=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit");
        foreach(var source in new[]{a,b,a})enter.Invoke(source.Blueprint.GetComponent<AbilityAreaEffectBuff>(),new object[]{source.Context,source,f.Unit});
        
        Check(f.Unit.Buffs.RawFacts.Count==2,"Native area entry owns two facts; repeated entry not duplicated");
        Check(Blocks(f.Unit,f.Cast(SpellSchool.Evocation,0,4).Context),"Overlapping upper tier blocks fourth level");
        var spell=f.Cast(SpellSchool.Conjuration,0,2).Context;var cloudBp=new BlueprintAbilityAreaEffect {SpellResistance=false};
        foreach(int time in new[]{50,110,150}) {
            var cloud=Area("cloud"+time,spell,cloudBp,time);Check(Blocks(f.Unit,spell,cloud)==(time>=100),"Existing area preserved; newer area blocked by any earlier qualifying globe");
            if(time>=100) {var result=(bool)AccessTools.Method(typeof(AreaEffectEntityData),"TryOvercomeSpellResistance").Invoke(cloud,new object[]{f.Unit});Check(!result,"Native area resistance entry rejects no-SR area while protected");}
        }
        exit.Invoke(highArea.GetComponent<AbilityAreaEffectBuff>(),new object[]{b.Context,b,f.Unit});Check(f.Unit.Buffs.RawFacts.Count==1&&!Blocks(f.Unit,f.Cast(SpellSchool.Evocation,0,4).Context)&&Blocks(f.Unit,spell),"Leaving stronger overlap restores lesser tier");
        exit.Invoke(lowArea.GetComponent<AbilityAreaEffectBuff>(),new object[]{a.Context,a,f.Unit});Check(f.Unit.Buffs.RawFacts.Count==0&&!Blocks(f.Unit,spell),"Last source exit removes protection");
        var noCaster=Empty<MechanicsContext>();var nonSpell=Area("native-pass",noCaster,cloudBp,0);Check((bool)AccessTools.Method(typeof(AreaEffectEntityData),"TryOvercomeSpellResistance").Invoke(nonSpell,new object[]{f.Unit}),"Original native area function remains in control for unrelated effects");
        enter.Invoke(lowArea.GetComponent<AbilityAreaEffectBuff>(),new object[]{a.Context,a,f.Unit});Check(Blocks(f.Unit,spell),"Reentry restores protection");
        exit.Invoke(lowArea.GetComponent<AbilityAreaEffectBuff>(),new object[]{a.Context,a,f.Unit});
    }
    static void Dispel(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony h)
    {
        h.Patch(AccessTools.PropertyGetter(typeof(RulebookEvent).GetNestedType("Dice",BindingFlags.Public|BindingFlags.NonPublic),"D20"),prefix:new HarmonyMethod(typeof(PsychicGlobeTests),nameof(D20)) {priority=Priority.First});
        foreach(int sourceCL in new[]{4,10,20})foreach(int casterCL in new[]{5,10,20})foreach(int roll in new[]{1,10,20}) {
            var globe=f.Cast(SpellSchool.Abjuration,0,4).Context;globe.SetParams(new AbilityParams {CasterLevel=sourceCL,SpellLevel=4});
            var area=Area("dispel-globe",globe,Asset<BlueprintAbilityAreaEffect>(cache,"GlobeOfInvulnerabilityLesserArea"),100);
            var cast=f.Cast(SpellSchool.Abjuration,0,3).Context;cast.SetParams(new AbilityParams {CasterLevel=casterCL,SpellLevel=3});
            var rule=new RuleDispelMagic(f.Unit,f.Unit,area,(RuleDispelMagic.CheckType)1,Kingmaker.EntitySystem.Stats.StatType.Unknown) {Reason=new RuleReason(cast)};
            dispelRoll=roll;rule.OnTrigger(null);
            bool expected=roll+casterCL>=11+sourceCL;
            Check(rule.DC==11+sourceCL&&rule.CasterLevel==casterCL&&rule.Success==expected,"Native area dispel DC/caster-level and seeded roll");
            Check((bool)Get(area,"m_ForceEnded")==expected,"Native successful dispel ForceEnds globe, failure preserves it");
        }
    }
    static bool D20(ref RuleRollD20 __result){__result=Empty<RuleRollD20>();Set(__result,"m_Result",dispelRoll);return false;}
    static AreaEffectEntityData Area(string id,MechanicsContext ctx,BlueprintAbilityAreaEffect bp,int seconds){var a=Empty<AreaEffectEntityData>();Set(a,"<UniqueId>k__BackingField",id);Set(a,"m_IsInGame",true);Set(a,"m_Context",ctx);Set(a,"m_Blueprint",bp);Set(a,"m_CreationTime",TimeSpan.FromSeconds(seconds));return a;}
    static bool AddAreaBuff(BlueprintBuff __1,ref Buff __result){__result=Add(__1);return false;}
    static bool RemoveAreaBuff(Buff __instance){facts.List.Remove(__instance);Refresh();return false;}
    static bool GameInstance(ref Game __result){__result=game;return false;}
}
