using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Components;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
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
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicAreaTests
{
    static int count,saveCount,healed;static bool passed,ally,dead;static EntityFactsManager facts;static UnitEntityData unit;static int wounds=20;
    static readonly List<object> damage=new List<object>();static readonly List<ContextActionApplyBuff> applied=new List<ContextActionApplyBuff>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Area spells: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicGloryHealing).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    static MechanicsContext Context(Psychic070Tests.Caster a,BlueprintScriptableObject bp,int cl=10,Metamagic mm=0){var ctx=new MechanicsContext(a.Unit,a.Descriptor,bp,null,new TargetWrapper(a.Unit));ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=3,Metamagic=mm});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(ctx);Set(ctx,"m_Ranks",ranks);return ctx;}
    internal static int Run(BlueprintsCache cache){
        var a=new Psychic070Tests.Caster(cache);unit=a.Unit;Set(unit,"m_IsInGame",true);Set(unit,"<HoldingState>k__BackingField",Empty<SceneEntitiesState>());facts=new EntityFactsManager(unit);Set(unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(unit,"<Buffs>k__BackingField",buffs);
        var h=new Harmony("CotW.AreaSpells.NativeBoundaries");void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicAreaTests),n){priority=Priority.First});
        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"ImprovedEvasion"),nameof(False));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicAreaTests),nameof(Save)));
        Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Ally));
        Patch(AccessTools.PropertyGetter(typeof(AreaEffectEntityData),"InGameUnitsInside"),nameof(Inside));
        Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(AddArea));
        Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(RemoveArea));
        Patch(AccessTools.PropertyGetter(typeof(UnitState),"IsDead"),nameof(Dead));Patch(AccessTools.PropertyGetter(typeof(UnitEntityData),"Damage"),nameof(Wounds));
        Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"HealDamage",new[]{typeof(UnitEntityData),typeof(UnitEntityData),typeof(int),typeof(EntityFact)}),nameof(Heal));
        try{Metadata(cache,a);Glory(cache,a);Fire(cache,a);}finally{h.UnpatchAll(h.Id);facts.List.Clear();Refresh();}
        PsychicExpansionTests.Inventory["areaSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" area spell checks; native duration/target/list setup, source-owned entry/exit and faction refresh, strongest-only healing, Reflex branches, native damage info and burning cleanup. Scene endpoints and save dice substituted.");return count;
    }
    static void Metadata(BlueprintsCache c,Psychic070Tests.Caster a){
        string[] names={"PathOfGlory","PathOfGloryGreater","Flashfire"};int[][] levels={new[]{2,2},new[]{4,4},new[]{3}};
        for(int i=0;i<names.Length;i++){
            var bp=Asset<BlueprintAbility>(c,names[i]);var links=bp.GetComponents<SpellListComponent>().ToArray();Check(links.Select(x=>x.SpellLevel).OrderBy(x=>x).SequenceEqual(levels[i]),"Original class levels "+names[i]);
            foreach(var link in links)Check(link.SpellList.SpellsByLevel[link.SpellLevel].Spells.Count(s=>s==bp)==1&&link.SpellList!=a.Book.Blueprint.SpellList,"Unique reciprocal original lists only");
            Check(bp.Parent==null&&!ReferenceEquals(bp.Icon,null)&&bp.CanTargetPoint,"Learnable point-target root with icon");
            var spawn=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionSpawnAreaEffect>().Single();var area=spawn.AreaEffect;
            Check(area.Shape==AreaEffectShape.Wall&&area.Size.Value==(i==2?30:60)&&!spawn.OnUnit&&!string.IsNullOrEmpty(area.Fx.AssetId),"Native wall geometry and persistent visual");
            Check(bp.SpellResistance==(i==2)&&area.SpellResistance==(i==2),"SR only on hostile fire");
            foreach(int cl in new[]{1,3,5,6,12,15,20,40})foreach(bool ext in new[]{false,true})Check(spawn.DurationValue.Calculate(Context(a,bp,cl,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromSeconds(6*cl*(ext?2:1)),"CL rounds and Extend");
        }
    }
    static void Tick(){foreach(var buff in unit.Buffs.Enumerable.ToArray()){var component=buff.Blueprint.GetComponent<PsychicGloryHealing>();if(component==null)continue;var rt=Runtime(component,buff);using(rt.RequestEventContext())component.OnNewRound();}}
    static void Glory(BlueprintsCache c,Psychic070Tests.Caster a){
        var enter=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter");var exit=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit");var round=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnRound");
        foreach(string token in new[]{"PathOfGlory","PathOfGloryGreater"}){
            var areaBp=Asset<BlueprintAbilityAreaEffect>(c,token+"Area");var owned=areaBp.GetComponent<AbilityAreaEffectBuff>();var ctx=Context(a,Asset<BlueprintAbility>(c,token));var area=Area("path",ctx,areaBp);int amount=token.EndsWith("Greater")?5:1;
            Check(owned.CheckConditionEveryRound&&owned.Buff.Stacking==StackingType.Stack,"Ownership facts coexist and faction updates");
            foreach(bool friendly in new[]{false,true}){
                facts.List.Clear();Refresh();ally=friendly;enter.Invoke(owned,new object[]{ctx,area,unit});Check(facts.List.Count==(friendly?1:0),"Only allies enter healing path");
                if(!friendly)continue;Check(unit.Buffs.RawFacts.Single().SourceAreaEffectId=="path","Native ownership ID");
                enter.Invoke(owned,new object[]{ctx,area,unit});Check(facts.List.Count==1,"Repeated entry creates no duplicate");
                foreach(bool isDead in new[]{false,true})foreach(int hp in new[]{0,1,20}){dead=isDead;wounds=hp;healed=0;Tick();Check(healed==(!isDead&&hp>0?amount:0),"Fast healing only living injured target");}dead=false;wounds=20;
                ally=false;round.Invoke(owned,new object[]{ctx,area});Check(facts.List.Count==0,"Faction change removes source-owned healing");ally=true;round.Invoke(owned,new object[]{ctx,area});Check(facts.List.Count==1,"Ally resumes inside area");
                exit.Invoke(owned,new object[]{ctx,area,unit});healed=0;Tick();Check(facts.List.Count==0&&healed==0,"Exit/end leaves no status or healing");
            }
        }
        facts.List.Clear();Refresh();ally=true;dead=false;wounds=20;var low=Asset<BlueprintAbilityAreaEffect>(c,"PathOfGloryArea");var high=Asset<BlueprintAbilityAreaEffect>(c,"PathOfGloryGreaterArea");var context=Context(a,Asset<BlueprintAbility>(c,"PathOfGlory"));
        var bps=new[]{low,low,high,high};var areas=bps.Select((bp,i)=>Area("overlap-"+i,context,bp)).ToArray();
        for(int i=0;i<4;i++){enter.Invoke(bps[i].GetComponent<AbilityAreaEffectBuff>(),new object[]{context,areas[i],unit});healed=0;Tick();Check(healed==(i<2?1:5),"Overlap never sums same or different tiers");}
        for(int i=3;i>=0;i--){exit.Invoke(bps[i].GetComponent<AbilityAreaEffectBuff>(),new object[]{context,areas[i],unit});healed=0;Tick();Check(healed==(i>2?5:i>0?1:0),"Removing strongest restores lower source without residue");}
    }
    static void Fire(BlueprintsCache c,Psychic070Tests.Caster a){
        var bp=Asset<BlueprintAbilityAreaEffect>(c,"FlashfireArea");var run=bp.GetComponent<AbilityAreaEffectRunAction>();
        Check(!(bool)Get(bp,"m_TickRoundAfterSpawn")&&!run.UnitExit.HasActions&&!run.UnitMove.HasActions,"No initial double tick or movement spam");
        foreach(int cl in new[]{1,3,5,6,9,12,15,20,40})foreach(bool pass in new[]{false,true})foreach(var mm in new[]{(Metamagic)0,Metamagic.Empower|Metamagic.Maximize,Metamagic.Persistent})foreach(bool round in new[]{false,true}){
            var ctx=Context(a,bp,cl,mm);var area=Area("fire",ctx,bp);passed=pass;saveCount=0;damage.Clear();applied.Clear();
            if(round)AccessTools.Method(typeof(AbilityAreaEffectRunAction),"OnRound").Invoke(run,new object[]{ctx,area});else AccessTools.Method(typeof(AbilityAreaEffectRunAction),"OnUnitEnter").Invoke(run,new object[]{ctx,area,unit});
            Check(damage.Count==(pass?0:1)&&applied.Count==(pass?0:1),"Reflex negates both on native entry/round");Check(saveCount==(pass&&mm.HasFlag(Metamagic.Persistent)?2:1),"Native Persistent rerolls successful save");
            if(pass)continue;var d=damage.Single();var dice=(DiceFormula)Get(d,"Dices");Check(dice.Rolls==Math.Min(5,Math.Max(1,cl/3))&&dice.Dice==DiceType.D6,"Per-three-level damage cap");
            Check(!(bool)Get(d,"HalfBecauseSavingThrow")&&(bool)Get(d,"Empower")==mm.HasFlag(Metamagic.Empower)&&(bool)Get(d,"Maximize")==mm.HasFlag(Metamagic.Maximize),"Full failed-save damage and native metamagic");
            Check(applied.Single().Permanent&&!applied.Single().AsChild&&applied.Single().NotLinkToAreaEffect,"Burn persists independently after leaving fire");
        }
        var burn=Asset<BlueprintBuff>(c,"FlashfireBurningBuff");var native=(BlueprintBuff)c.Load(BlueprintGuid.Parse("e92ecaa76b5db674fa5b0aaff5b21bc9"));
        Check(burn.Stacking==StackingType.Replace&&burn.ComponentsArray.Select(x=>x.GetType()).SequenceEqual(native.ComponentsArray.Select(x=>x.GetType())),"Burn uses native complete fire-elemental lifecycle and does not stack");
        var actions=burn.GetComponent<AddFactContextActions>().NewRound.Actions;var tick=actions.OfType<ContextActionDealDamage>().Single();Check(tick.Value.DiceType==DiceType.D6&&tick.Value.DiceCountValue.Value==1,"Ongoing burn stays 1d6");
        var save=actions.OfType<ContextActionSavingThrow>().Single();Check(save.Type==SavingThrowType.Reflex,"Native subsequent extinguish save");
        var conditional=save.Actions.Actions.OfType<ContextActionConditionalSaved>().Single();Check(conditional.Succeed.Actions.OfType<ContextActionRemoveSelf>().Any(),"Successful later save removes burning");
    }
    static AreaEffectEntityData Area(string id,MechanicsContext ctx,BlueprintAbilityAreaEffect bp){var a=Empty<AreaEffectEntityData>();Set(a,"<UniqueId>k__BackingField",id);Set(a,"m_Context",ctx);Set(a,"m_Blueprint",bp);
        var ft=AccessTools.Field(typeof(AreaEffectEntityData),"m_UnitsInside");var list=(System.Collections.IList)Activator.CreateInstance(ft.FieldType);var info=Activator.CreateInstance(ft.FieldType.GetGenericArguments()[0],true);Set(info,"Reference",(UnitReference)unit);list.Add(info);ft.SetValue(a,list);return a;}
    static bool Damage(ContextActionDealDamage __instance){damage.Add(AccessTools.Method(typeof(ContextActionDealDamage),"GetDamageInfo").Invoke(__instance,null));return false;}
    static bool Apply(ContextActionApplyBuff __instance){applied.Add(__instance);return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}static bool False(ref bool __result){__result=false;return false;}
    static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){saveCount++;__result=rule;Check(rule.DifficultyClassBase==23,"Original spell save DC");return false;}
    static bool Ally(ref bool __result){__result=ally;return false;}static bool Inside(ref IEnumerable<UnitEntityData> __result){__result=new[]{unit};return false;}
    static bool AddArea(BlueprintBuff __1,MechanicsContext __2,ref Buff __result){__result=Fact(__1,__2);facts.List.Add(__result);Refresh();return false;}
    static bool RemoveArea(Buff __instance){facts.List.Remove(__instance);Refresh();return false;}
    static bool Dead(ref bool __result){__result=dead;return false;}static bool Wounds(ref int __result){__result=wounds;return false;}static bool Heal(int __2){healed+=__2;return false;}
}
