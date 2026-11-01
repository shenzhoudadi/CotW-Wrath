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

internal static class PsychicTacticalTests
{
    static int count,removed,saveCount;static bool passed,enemy;static EntityFactsManager facts;
    static readonly List<object> damage=new List<object>();static readonly List<RuleCombatManeuver> maneuvers=new List<RuleCombatManeuver>();
    static readonly List<ContextActionApplyBuff> applied=new List<ContextActionApplyBuff>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Tactical: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicForcefulBullRush).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static T Ref<T>(SimpleBlueprint bp) where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    static MechanicsContext Context(Psychic070Tests.Caster a,BlueprintAbility bp,int cl=10,Metamagic mm=0){var ctx=new MechanicsContext(a.Unit,a.Descriptor,bp,null,new TargetWrapper(a.Unit));ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=4,Metamagic=mm});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(ctx);Set(ctx,"m_Ranks",ranks);return ctx;}
    internal static int Run(BlueprintsCache cache){
        var a=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(a.Unit);Set(a.Unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(a.Unit,"<Buffs>k__BackingField",buffs);
        var h=new Harmony("CotW.Tactical.NativeBoundaries");void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicTacticalTests),n){priority=Priority.First});
        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionRemoveSelf),"RunAction"),nameof(Remove));Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"ImprovedEvasion"),nameof(False));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicTacticalTests),nameof(Save)));PsychicRuleTestBoundary.Patch(h,typeof(RuleCombatManeuver),AccessTools.Method(typeof(PsychicTacticalTests),nameof(Maneuver)));
        try{Metadata(cache,a);Strike(cache,a);Doom(cache,a,h);Truth(cache,a,h);}finally{h.UnpatchAll(h.Id);facts.List.Clear();Refresh();}
        PsychicExpansionTests.Inventory["tacticalSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" tactical spell checks; native hit/save actions, source-owned aura entry/exit, suppression lifecycle and durations. Dice, faction and scene endpoints substituted.");return count;
    }
    static void Metadata(BlueprintsCache c,Psychic070Tests.Caster a){
        string[] names={"ForcefulStrike","AuraOfDoom","LitanyOfTruth"};int[][] levels={new[]{4,4,4,4},new[]{4},new[]{4,6}};
        for(int i=0;i<names.Length;i++){
            var bp=Asset<BlueprintAbility>(c,names[i]);var links=bp.GetComponents<SpellListComponent>().ToArray();Check(links.Select(x=>x.SpellLevel).OrderBy(x=>x).SequenceEqual(levels[i]),"Original class levels "+names[i]);
            foreach(var link in links)Check(link.SpellList.SpellsByLevel[link.SpellLevel].Spells.Count(s=>s==bp)==1&&link.SpellList!=a.Book.Blueprint.SpellList,"Reciprocal native lists only "+names[i]);
            Check(bp.Parent==null&&!ReferenceEquals(bp.Icon,null),"Learnable root and icon "+names[i]);
        }
        var doom=Asset<BlueprintAbility>(c,"AuraOfDoom");var apply=(ContextActionApplyBuff)doom.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        foreach(int cl in new[]{1,5,10,20,40})foreach(bool ext in new[]{false,true})Check(apply.DurationValue.Calculate(Context(a,doom,cl,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromMinutes(cl*10*(ext?2:1)),"Doom10minutes/CL and Extend");
    }
    static void Strike(BlueprintsCache c,Psychic070Tests.Caster a){
        var bp=Asset<BlueprintAbility>(c,"ForcefulStrike");var buff=Asset<BlueprintBuff>(c,"ForcefulStrikeBuff");var hit=buff.GetComponent<PsychicForcefulStrikeHit>();
        Check(hit.OnlyHit&&hit.CheckWeaponRangeType&&hit.RangeType==WeaponRangeType.Melee,"Native successful melee trigger");
        foreach(int cl in new[]{1,5,10,11,20,40})foreach(bool pass in new[]{false,true})foreach(bool success in new[]{false,true})foreach(var category in new[]{WeaponCategory.Longsword,WeaponCategory.Longbow,WeaponCategory.UnarmedStrike,WeaponCategory.Claw,WeaponCategory.Touch,WeaponCategory.Ray})foreach(Metamagic mm in new[]{(Metamagic)0,Metamagic.Empower,Metamagic.Maximize,Metamagic.Empower|Metamagic.Maximize,Metamagic.Persistent}){
            var ctx=Context(a,bp,cl,mm);var fact=Fact(buff,ctx);var rt=Runtime(hit,fact);
            bool melee=category!=WeaponCategory.Longbow&&category!=WeaponCategory.Ray;
            var type=new BlueprintWeaponType {Category=category};Set(type,"m_AttackType",category==WeaponCategory.Touch?AttackType.Touch:melee?AttackType.Melee:AttackType.Ranged);
            var weaponBp=new BlueprintItemWeapon();Set(weaponBp,"m_Type",Ref<BlueprintWeaponTypeReference>(type));var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",weaponBp);
            var evt=Empty<RuleAttackWithWeapon>();Set(evt,"Initiator",a.Unit);Set(evt,"Target",a.Unit);Set(evt,"Weapon",weapon);var roll=Empty<RuleAttackRoll>();Set(roll,"<Result>k__BackingField",success?AttackResult.Hit:AttackResult.Miss);Set(evt,"<AttackRoll>k__BackingField",roll);
            var resolve=Empty<RuleAttackWithWeaponResolve>();Set(resolve,"AttackWithWeapon",evt);Set(resolve,"m_Triggered",true);Set(evt,"<ResolveRules>k__BackingField",new List<RuleAttackWithWeaponResolve>{resolve});
            passed=pass;damage.Clear();maneuvers.Clear();removed=0;saveCount=0;
            using(rt.RequestEventContext())((Kingmaker.PubSubSystem.IInitiatorRulebookHandler<RuleAttackWithWeapon>)hit).OnEventDidTrigger(evt);
            bool triggered=success&&melee&&category!=WeaponCategory.Touch;Check(damage.Count==(triggered?1:0)&&removed==(triggered?1:0),"Discharge once only on melee hit");
            if(!triggered){Check(saveCount==0&&maneuvers.Count==0,"Miss/ranged keeps charge without save or bull rush");continue;}
            var info=damage.Single();var dice=(DiceFormula)Get(info,"Dices");Check(dice.Rolls==Math.Min(cl,10)&&dice.Dice==DiceType.D4,"Force CL cap10d4");Check((bool)Get(info,"HalfBecauseSavingThrow")==pass,"Fortitude half branch");
            Check((bool)Get(info,"Empower")==mm.HasFlag(Metamagic.Empower)&&(bool)Get(info,"Maximize")==mm.HasFlag(Metamagic.Maximize),"Force native metamagic");
            Check(maneuvers.Count==(pass?0:1),"Only failed save bull rushes");if(!pass)Check(maneuvers[0].Type==CombatManeuver.BullRush&&maneuvers[0].IgnoreConcealment&&maneuvers[0].AllBonuses.Sum(x=>x.Value)==cl,"Bull rush has uncapped CL bonus only on its own rule");
            Check(saveCount==(pass&&mm.HasFlag(Metamagic.Persistent)?2:1),"Native Persistent retry");
        }
    }
    static void Doom(BlueprintsCache c,Psychic070Tests.Caster a,Harmony h){
        var bp=Asset<BlueprintAbility>(c,"AuraOfDoom");var areaBp=Asset<BlueprintAbilityAreaEffect>(c,"AuraOfDoomArea");var owned=areaBp.GetComponent<AbilityAreaEffectBuff>();var ctx=Context(a,bp);
        Check(areaBp.Size.Value==20&&areaBp.SpellResistance&&!owned.CheckConditionEveryRound&&owned.Buff.Stacking==StackingType.Stack,"20ftSR aura rolls once per entry, independent owned facts");
        Check(Asset<BlueprintBuff>(c,"AuraOfDoomBuff").GetComponent<AddAreaEffect>().AreaEffect==areaBp,"Aura follows host buff");
        h.Patch(AccessTools.Method(typeof(UnitEntityData),"IsEnemy",new[]{typeof(UnitEntityData)}),prefix:new HarmonyMethod(typeof(PsychicTacticalTests),nameof(Enemy)){priority=Priority.First});
        h.Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(PsychicTacticalTests),nameof(AddArea)){priority=Priority.First});
        h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(PsychicTacticalTests),nameof(RemoveArea)){priority=Priority.First});
        var enter=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter");var exit=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit");var round=AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnRound");
        foreach(bool hostile in new[]{false,true})foreach(bool pass in new[]{false,true})foreach(bool persistent in new[]{false,true}){
            facts.List.Clear();Refresh();ctx=Context(a,bp,10,persistent?Metamagic.Persistent:0);enemy=hostile;passed=pass;saveCount=0;
            var area=Area("doom-a",ctx,areaBp);enter.Invoke(owned,new object[]{ctx,area,a.Unit});bool affected=hostile&&!pass;
            Check(facts.List.Count==(affected?1:0)&&saveCount==(hostile?(pass&&persistent?2:1):0),"Faction and Will/Persistent gates");int rolls=saveCount;
            round.Invoke(owned,new object[]{ctx,area});Check(saveCount==rolls,"No save retries each round");
            if(affected){Check(a.Unit.Buffs.RawFacts.Single().SourceAreaEffectId=="doom-a","Native source area link stored");enter.Invoke(owned,new object[]{ctx,area,a.Unit});Check(facts.List.Count==1&&saveCount==rolls,"Repeated entry never duplicates existing owned buff");}
            exit.Invoke(owned,new object[]{ctx,area,a.Unit});Check(facts.List.Count==0,"Native exit clears own effect");
            enter.Invoke(owned,new object[]{ctx,area,a.Unit});Check(saveCount==rolls+(hostile?(pass&&persistent?2:1):0),"Exit/reentry gets fresh save");
        }
        facts.List.Clear();Refresh();passed=false;enemy=true;ctx=Context(a,bp);
        var first=Area("doom-first",ctx,areaBp);var second=Area("doom-second",ctx,areaBp);var other=Fact(owned.Buff,ctx);other.SourceAreaEffectId="other";facts.List.Add(other);Refresh();
        enter.Invoke(owned,new object[]{ctx,first,a.Unit});enter.Invoke(owned,new object[]{ctx,second,a.Unit});Check(facts.List.Count==3,"Overlapping sources coexist");
        exit.Invoke(owned,new object[]{ctx,first,a.Unit});Check(facts.List.Count==2&&facts.List.Contains(other),"First source removal preserves other fear");exit.Invoke(owned,new object[]{ctx,second,a.Unit});Check(facts.List.Count==1&&facts.List.Contains(other),"Last aura preserves unrelated source");facts.List.Clear();Refresh();
    }
    static void Truth(BlueprintsCache c,Psychic070Tests.Caster a,Harmony h){
        var bp=Asset<BlueprintAbility>(c,"LitanyOfTruth");var buff=Asset<BlueprintBuff>(c,"LitanyOfTruthBuff");var ctx=Context(a,bp);var run=bp.GetComponent<AbilityEffectRunAction>();
        Check(bp.SpellResistance&&run.SavingThrowType==SavingThrowType.Will&&bp.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift&&bp.Range==AbilityRange.Close,"SwiftCloseWillSR");
        foreach(bool pass in new[]{false,true}){passed=pass;applied.Clear();using(ctx.GetDataScope(a.Unit))using(SaveScope())run.Actions.Run();Check(applied.Count==(pass?0:1),"Will negates truth");}
        var suppression=buff.GetComponent<SuppressBuffs>();Check(suppression.Schools.SequenceEqual(new[]{SpellSchool.Illusion})&&suppression.Descriptor==SpellDescriptor.None,"Only illusion school suppressed");
        h.Patch(AccessTools.Method(typeof(EntityFact),"Activate"),prefix:new HarmonyMethod(typeof(PsychicTacticalTests),nameof(Activate)));h.Patch(AccessTools.Method(typeof(EntityFact),"Deactivate"),prefix:new HarmonyMethod(typeof(PsychicTacticalTests),nameof(Deactivate)));
        var runtime=Runtime(suppression,Fact(buff,ctx));var illusion=a.Cast(SpellSchool.Illusion,0,2).Context;var nonIllusion=a.Cast(SpellSchool.Abjuration,0,2).Context;Set(illusion,"<SpellSchool>k__BackingField",SpellSchool.Illusion);Set(nonIllusion,"<SpellSchool>k__BackingField",SpellSchool.Abjuration);
        var old=Fact(new BlueprintBuff(),illusion);var unrelated=Fact(new BlueprintBuff(),nonIllusion);facts.List.Add(old);facts.List.Add(unrelated);Refresh();
        using(runtime.RequestEventContext())AccessTools.Method(typeof(SuppressBuffs),"OnActivate").Invoke(suppression,null);
        var part=a.Unit.Get<UnitPartBuffSuppress>();Check(old.IsSuppressed&&!unrelated.IsSuppressed&&facts.List.Contains(old),"Illusion preserved but suppressed; other school untouched");
        var late=Fact(new BlueprintBuff(),illusion);Check(part.IsSuppressed(late),"New illusions also suppressed");facts.List.Add(late);Refresh();part.ApplyChanges();Check(late.IsSuppressed,"Late illusion suppression applied");
        using(runtime.RequestEventContext())AccessTools.Method(typeof(SuppressBuffs),"OnDeactivate").Invoke(suppression,null);Check(!old.IsSuppressed&&!late.IsSuppressed&&a.Unit.Get<UnitPartBuffSuppress>()==null,"All surviving illusions restore on expiry");
        var conceal=buff.GetComponent<DoNotBenefitFromConcealment>();var cr=Runtime(conceal,Fact(buff,ctx));var cp=a.Unit.Ensure<UnitPartConcealment>();cp.Disable=true;
        using(cr.RequestEventContext())AccessTools.Method(typeof(DoNotBenefitFromConcealment),"OnTurnOn").Invoke(conceal,null);Check(cp.Disable,"Concealment disabled by native counter");
        using(cr.RequestEventContext())AccessTools.Method(typeof(DoNotBenefitFromConcealment),"OnTurnOff").Invoke(conceal,null);Check(cp.Disable,"Other no-concealment source preserved");cp.Disable=false;Check(!cp.Disable,"Final release restores concealment");
        facts.List.Clear();Refresh();
    }
    static AreaEffectEntityData Area(string id,MechanicsContext ctx,BlueprintAbilityAreaEffect bp){var a=Empty<AreaEffectEntityData>();Set(a,"<UniqueId>k__BackingField",id);Set(a,"m_Context",ctx);Set(a,"m_Blueprint",bp);return a;}
    static IDisposable SaveScope(){var t=typeof(MechanicsContext).Assembly.GetType("Kingmaker.UnitLogic.Mechanics.Actions.SavingThrowData",true);var s=AccessTools.Method(typeof(Kingmaker.ElementsSystem.ContextData<>).MakeGenericType(t),"Request").Invoke(null,null);AccessTools.Method(t,"Setup").Invoke(s,new object[]{Empty<RuleSavingThrow>()});return (IDisposable)s;}
    static bool Damage(ContextActionDealDamage __instance){damage.Add(AccessTools.Method(typeof(ContextActionDealDamage),"GetDamageInfo").Invoke(__instance,null));return false;}
    static bool Remove(){removed++;return false;}static bool Apply(ContextActionApplyBuff __instance){applied.Add(__instance);return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}static bool False(ref bool __result){__result=false;return false;}
    static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){saveCount++;__result=rule;Check(rule.DifficultyClassBase==23,"Actual save DC from source context");return false;}
    static bool Maneuver(RuleCombatManeuver rule,ref RuleCombatManeuver __result){maneuvers.Add(rule);__result=rule;return false;}
    static bool Enemy(ref bool __result){__result=enemy;return false;}
    static bool AddArea(BlueprintBuff __1,MechanicsContext __2,ref Buff __result){__result=Fact(__1,__2);facts.List.Add(__result);Refresh();return false;}
    static bool RemoveArea(Buff __instance){facts.List.Remove(__instance);Refresh();return false;}
    static bool Activate(EntityFact __instance){Set(__instance,"<IsActive>k__BackingField",true);return false;}static bool Deactivate(EntityFact __instance){Set(__instance,"<IsActive>k__BackingField",false);return false;}
}

