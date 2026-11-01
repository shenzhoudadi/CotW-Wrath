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



using Kingmaker.PubSubSystem;

using Newtonsoft.Json;

internal static class PsychicAspectTests

{

    static int count,damageCount,applyCount,knockCount;static bool passed,hit;static int roll=5;static UnitEntityData unit;static EntityFactsManager facts;

    static MechanicsContext context;static readonly List<Buff> children=new List<Buff>();static readonly Dictionary<Buff,List<Buff>> stored=new Dictionary<Buff,List<Buff>>();

    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));

    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);

    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);

    static void Check(bool ok,string why){if(!ok)throw new Exception("Aspect batch: "+why);count++;}

    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicChangeAspect).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));

    static T Ref<T>(SimpleBlueprint bp) where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}

    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}

    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}

    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}

    static MechanicsContext Context(Psychic070Tests.Caster a,BlueprintScriptableObject bp,int cl=10,Metamagic mm=0){var ctx=new MechanicsContext(a.Unit,a.Descriptor,bp,null,new TargetWrapper(a.Unit));ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=3,Metamagic=mm});var ranks=new int[32];foreach(var r in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(r,"m_Type")]=r.GetValue(ctx);Set(ctx,"m_Ranks",ranks);return ctx;}

    internal static int Run(BlueprintsCache cache){

        var oldTime=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());

        var a=new Psychic070Tests.Caster(cache);unit=a.Unit;Set(unit,"m_IsInGame",true);Set(unit,"<HoldingState>k__BackingField",Empty<SceneEntitiesState>());facts=new EntityFactsManager(unit);Set(unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(unit,"<Buffs>k__BackingField",buffs);

        var h=new Harmony("CotW.AspectBatch.NativeBoundaries");void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicAspectTests),n){priority=Priority.First});

        Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));

        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));Patch(AccessTools.Method(typeof(ContextActionKnockdownTarget),"RunAction"),nameof(Knock));

        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"ImprovedEvasion"),nameof(False));

        PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicAspectTests),nameof(Save)));

        Patch(AccessTools.Method(typeof(RuleRollDice),"OnTrigger"),nameof(Dice));

        Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),nameof(Hit));

        Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),nameof(Difficulty));

        Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(Add));Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(Remove));

        Patch(AccessTools.Method(typeof(Buff),"StoreFact"),nameof(Store));

        Patch(AccessTools.Method(typeof(ContextActionSkillCheck),"RunAction"),nameof(Mobility));

        try{Metadata(cache,a);Aspect(cache,a);Walls(cache,a);Invective(cache,a);Vigor(cache,a);}finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",oldTime);facts.List.Clear();Refresh();}

        PsychicExpansionTests.Inventory["aspectSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" aspect-batch checks: lists/durations, age stats and child ownership, wall crossings/saves/immunities, scoped native demoralize event delivery, real THP consumption and persistent component data. Scene endpoints and dice seeded.");return count;

    }

    static void Metadata(BlueprintsCache c,Psychic070Tests.Caster a){

        string[] names={"ThreefoldAspect","BlisteringInvective","WallOfNausea","WallOfBlindness","RighteousVigor"};int[][] levels={new[]{5},new[]{2,2,2},new[]{3,3},new[]{4,4,5},new[]{2,3}};

        for(int i=0;i<names.Length;i++){

            var bp=Asset<BlueprintAbility>(c,names[i]);var links=bp.GetComponents<SpellListComponent>().ToArray();Check(links.Select(l=>l.SpellLevel).OrderBy(x=>x).SequenceEqual(levels[i]),"Original lists "+names[i]);Check(bp.Parent==null&&bp.Type==AbilityType.Spell,"Learnable root");

            foreach(var l in links)Check(l.SpellList!=a.Book.Blueprint.SpellList&&l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(s=>s==bp)==1,"Unique reciprocal original list");

            foreach(int cl in new[]{1,5,10,20,40})foreach(bool extend in new[]{false,true}){

                var ctx=Context(a,bp,cl,extend?Metamagic.Extend:0);var action=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();

                var duration=(action as ContextActionApplyBuff)?.DurationValue??(action as ContextActionSpawnAreaEffect)?.DurationValue;

                if(duration!=null)Check(duration.Calculate(ctx).Seconds==TimeSpan.FromSeconds((i==0?86400:cl*6)*(extend?2:1)),"Native duration and Extend");

            }

        }

    }

    static void Aspect(BlueprintsCache c,Psychic070Tests.Caster a){

        var bp=Asset<BlueprintBuff>(c,"ThreefoldAspectBuff");var spell=Asset<BlueprintAbility>(c,"ThreefoldAspect");context=Context(a,spell);var menu=Asset<BlueprintAbility>(c,"ThreefoldAspectChange");Check(bp.GetComponent<AddFacts>().Facts.Single()==menu&&menu.GetComponent<AbilityVariants>().Variants.Length==4,"Temporary single menu with four choices");

        foreach(var st in new[]{StatType.Strength,StatType.Dexterity,StatType.Constitution,StatType.Intelligence,StatType.Wisdom})if(!unit.Stats.AllStats.Any(s=>s.Type==st))unit.Stats.AllStats.Add(new ModifiableValue(unit.Stats,st));

        var host=Fact(bp,context);facts.List.Add(host);Refresh();

        foreach(int i in new[]{0,1,2,3,2,0,3}){

            var v=menu.GetComponent<AbilityVariants>().Variants[i];Check(v.Parent==menu&&v.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,"Every switch standard action");

            using(context.GetDataScope(new TargetWrapper(unit)))v.GetComponent<AbilityEffectRunAction>().Actions.Run();

            Check(children.Count==(i==3?0:1),"Switch replaces old choice or returns to natural");if(i==3)continue;

            var child=children.Single();Check(stored[host].Contains(child)&&child.Context==host.Context,"Child tied to original duration/caster parent");

            var mods=child.Blueprint.GetComponents<AddStatBonus>().ToArray();Check(mods.Length==(i==2?4:3),"Exact stat count");

            foreach(var component in mods){var stat=unit.Stats.GetStat(component.Stat);int start=stat.ModifiedValue;var foreign=stat.AddModifier(6,(EntityFact)null,ModifierDescriptor.Enhancement);var rt=Runtime(component,child);rt.TurnOn();Check(stat.ModifiedValue==start+6+Math.Min(component.Value,0),"Enhancement takes highest; penalty stacks");rt.TurnOff();Check(stat.ModifiedValue==start+6,"Age bonus and penalty cleanly removed");foreign.Remove();}

        }

        facts.List.Remove(host);Refresh();using(context.GetDataScope(new TargetWrapper(unit)))menu.GetComponent<AbilityVariants>().Variants[0].GetComponent<AbilityEffectRunAction>().Actions.Run();Check(children.Count==0,"Orphan button cannot grant an aspect");

    }

    static void Walls(BlueprintsCache c,Psychic070Tests.Caster a){

        foreach(string token in new[]{"WallOfNausea","WallOfBlindness"}){

            var area=Asset<BlueprintAbilityAreaEffect>(c,token+"Area");var logic=area.GetComponent<PsychicUnoccupiedWall>();var ctx=Context(a,Asset<BlueprintAbility>(c,token));

            Check(area.Shape==AreaEffectShape.Wall&&area.Size.Value==30&&area.SpellResistance&&!string.IsNullOrEmpty(area.Fx.AssetId),"Original wall visual/size/SR");Check(!logic.Round.HasActions&&!logic.UnitMove.HasActions&&!logic.UnitExit.HasActions,"No repeat effects without reentry");

            var e=Area(ctx,area,true);damageCount=applyCount=0;AccessTools.Method(typeof(PsychicUnoccupiedWall),"OnUnitEnter").Invoke(logic,new object[]{ctx,e,unit});Check(applyCount==0,"Initial occupants not attacked");AccessTools.Method(typeof(PsychicUnoccupiedWall),"OnRound").Invoke(logic,new object[]{ctx,e});Check((bool)Get(e,"m_ForceEnded"),"Occupied initial wall fails");

            e=Area(ctx,area,false);AccessTools.Method(typeof(PsychicUnoccupiedWall),"OnRound").Invoke(logic,new object[]{ctx,e});Check(!(bool)Get(e,"m_ForceEnded"),"Later round does not destroy wall");

            var condition=logic.UnitEnter.Actions.OfType<Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional>().Single();var immune=condition.ConditionsChecker.Conditions.Cast<Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact>().ToArray();Check(immune.Length==3,"Construct undead elemental exclusions");

            foreach(bool pass in new[]{false,true})foreach(int kind in new[]{-1,0,1,2}){

                facts.List.Clear();Refresh();if(kind>=0){var feature=Empty<UnitFact>();Set(feature,"<Blueprint>k__BackingField",immune[kind].Fact);Set(feature,"Components",new List<EntityFactComponent>());feature.RestoreOwnerLink(facts);facts.List.Add(feature);Refresh();}

                passed=pass;applyCount=0;knockCount=0;using(ctx.GetDataScope(new TargetWrapper(unit))){

                    // Mobility random roll is independently verified below; keep this crossing test at the native save/action boundary.

                    if(token=="WallOfNausea"){

                        var sc=((ContextActionConditionalSaved)((ContextActionSavingThrow)condition.IfFalse.Actions.Single()).Actions.Actions.Single()).Failed.Actions.OfType<ContextActionSkillCheck>().Single();

                        Check(sc.Stat==StatType.SkillMobility&&sc.CustomDC.Value==12&&sc.Failure.Actions.OfType<ContextActionKnockdownTarget>().Any(),"DC12 Mobility failure knocks down");

                    }

                    condition.RunAction();

                }

                Check(applyCount==(!pass&&kind<0?1:0),"Save and creature exclusions");

            }

            var debuff=Asset<BlueprintBuff>(c,token+"Debuff");Check(!debuff.GetComponents<SpellDescriptorComponent>().Any(d=>((SpellDescriptor)d.Descriptor).HasFlag(SpellDescriptor.Poison)),"Nausea not incorrectly poison");

        }facts.List.Clear();Refresh();

    }

    static void Invective(BlueprintsCache c,Psychic070Tests.Caster a){

        var spell=Asset<BlueprintAbility>(c,"BlisteringInvective");var action=(PsychicInvectiveDemoralize)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();Check(action.Demoralize.Buff!=null&&action.Demoralize.GreaterBuff!=null,"Native demoralize retains shaken and frightening variants");

        Check(((Feet)Get(spell.GetComponent<AbilityTargetsAround>(),"m_Radius")).Value==30,"Native 30ft targeting");

        var ctx=Context(a,spell);var stat=new ModifiableValue(unit.Stats,StatType.CheckIntimidate);unit.Stats.AllStats.Add(stat);

        // Substitute only native Demoralize's scene-dependent body; real RuleSkillCheck and native event bus drive the scoped callback.

        var h=new Harmony("CotW.InvectiveScopedCapture");h.Patch(AccessTools.Method(typeof(Demoralize),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicAspectTests),nameof(DemoralizeFixture)));

        try{foreach(bool intimidate in new[]{false,true})foreach(bool reflex in new[]{false,true}){passed=reflex;roll=intimidate?20:0;damageCount=applyCount=0;using(ctx.GetDataScope(new TargetWrapper(unit)))action.RunAction();Check(damageCount==(intimidate?1:0)&&applyCount==(intimidate&&!reflex?1:0),"Demoralize success gates damage; Reflex gates only ignition");}}

        finally{h.UnpatchAll(h.Id);}

        var damage=action.Success.Actions.OfType<ContextActionDealDamage>().Single();Check(damage.Value.DiceType==DiceType.D10&&damage.Value.DiceCountValue.Value==1&&!damage.HalfIfSaved,"Initial damage is 1d10 without Reflex reduction");

        var burn=Asset<BlueprintBuff>(c,"BlisteringInvectiveBurningBuff");Check(burn.GetComponent<AddFactContextActions>().NewRound.Actions.OfType<ContextActionSavingThrow>().Any(),"Native burning round save and removal retained");

    }

    static void Vigor(BlueprintsCache c,Psychic070Tests.Caster a){

        var bp=Asset<BlueprintBuff>(c,"RighteousVigorBuff");var component=bp.GetComponent<PsychicRighteousVigor>();context=Context(a,Asset<BlueprintAbility>(c,"RighteousVigor"));var fact=Fact(bp,context);var rt=Runtime(component,fact);

        var ab=new ModifiableValue(unit.Stats,StatType.AdditionalAttackBonus);Set(unit.Stats,"AdditionalAttackBonus",ab);unit.Stats.AllStats.Add(ab);var hp=new ModifiableValueTemporaryHitPoints(unit.Stats);Set(unit.Stats,"TemporaryHitPoints",hp);unit.Stats.AllStats.Add(hp);

        foreach(var cat in new[]{WeaponCategory.Longsword,WeaponCategory.UnarmedStrike,WeaponCategory.Claw,WeaponCategory.Longbow,WeaponCategory.Touch,WeaponCategory.Ray})foreach(var mm in new[]{(Metamagic)0,Metamagic.Maximize,Metamagic.Empower,Metamagic.Empower|Metamagic.Maximize}){

            context.SetParams(new AbilityParams {CasterLevel=10,Metamagic=mm});rt.TurnOn();var type=new BlueprintWeaponType {Category=cat};Set(type,"m_AttackType",cat==WeaponCategory.Longbow||cat==WeaponCategory.Ray?AttackType.Ranged:AttackType.Melee);var weaponbp=new BlueprintItemWeapon();Set(weaponbp,"m_Type",Ref<BlueprintWeaponTypeReference>(type));var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",weaponbp);

            var evt=new RuleAttackRoll(unit,unit,weapon,0);bool eligible=cat!=WeaponCategory.Longbow&&cat!=WeaponCategory.Touch&&cat!=WeaponCategory.Ray;int gained=mm.HasFlag(Metamagic.Maximize)?8:5;if(mm.HasFlag(Metamagic.Empower))gained+=gained/2;

            for(int i=1;i<=6;i++){hit=true;roll=5;using(rt.RequestEventContext())component.OnEventDidTrigger(evt);Check(ab.ModifiedValue==(eligible?Math.Min(4,i):0)&&hp.ModifiedValue==(eligible?Math.Min(20,i*gained):0),"Hit eligibility/caps/metamagic "+cat+" "+mm);}

            int before=hp.ModifiedValue;hit=false;using(rt.RequestEventContext())component.OnEventDidTrigger(evt);Check(ab.ModifiedValue==0&&hp.ModifiedValue==before,"Miss resets accuracy only");

            int remaining=hp.HandleDamage(7);Check(hp.ModifiedValue==Math.Max(0,before-7)&&remaining==Math.Max(0,7-before),"Native damage consumes only remaining THP");int hpAfter=hp.ModifiedValue;

            var foreign=ab.AddModifier(6,(EntityFact)null,ModifierDescriptor.Morale);hit=true;using(rt.RequestEventContext())component.OnEventDidTrigger(evt);Check(ab.ModifiedValue==6,"Morale accuracy does not stack with stronger source");foreign.Remove();

            hpAfter=hp.ModifiedValue;int abAfter=ab.ModifiedValue;rt.TurnOff();Check(hp.ModifiedValue==0&&ab.ModifiedValue==0,"Off removes own modifiers");

            var data=rt.GetData<PsychicVigorState>();string json=JsonConvert.SerializeObject(data);var restored=JsonConvert.DeserializeObject<PsychicVigorState>(json);Check(restored.StoredHP==hpAfter&&restored.StoredAttack==abAfter,"Persistent suspended data roundtrip");rt.TurnOn();Check(hp.ModifiedValue==hpAfter&&ab.ModifiedValue==abAfter,"Resume keeps remaining HP, not cumulative granted HP");

            hp.HandleDamage(100);rt.TurnOn();Check(hp.ModifiedValue==0,"Repeated turn-on cannot refill exhausted HP");rt.TurnOff();data.StoredAttack=data.StoredHP=0;

        }

    }

    static AreaEffectEntityData Area(MechanicsContext ctx,BlueprintAbilityAreaEffect bp,bool initial){var e=Empty<AreaEffectEntityData>();Set(e,"m_Context",ctx);Set(e,"m_Blueprint",bp);Set(e,"m_CreationTime",Kingmaker.Game.Instance.TimeController.GameTime-TimeSpan.FromSeconds(initial?0:6));var ft=AccessTools.Field(typeof(AreaEffectEntityData),"m_UnitsInside");var list=(System.Collections.IList)Activator.CreateInstance(ft.FieldType);var info=Activator.CreateInstance(ft.FieldType.GetGenericArguments()[0],true);Set(info,"Reference",(UnitReference)unit);list.Add(info);ft.SetValue(e,list);return e;}

    static bool Mobility(ContextActionSkillCheck __instance){__instance.Failure.Run();return false;}

    static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}

    static bool Damage(){damageCount++;return false;}static bool Apply(){applyCount++;return false;}static bool Knock(){knockCount++;return false;}

    static bool Passed(ref bool __result){__result=passed;return false;}static bool False(ref bool __result){__result=false;return false;}static bool Hit(ref bool __result){__result=hit;return false;}

    static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){__result=rule;Check(rule.DifficultyClassBase==23,"Original spell DC");return false;}

    static bool Dice(RuleRollDice __instance){var rule=__instance;rule.Override(rule.DiceFormula.Rolls==0?0:roll);return false;}

    static bool Difficulty(int __0,ref int __1,ref int __result){__1=0;__result=__0;return false;}

    static bool Add(BlueprintBuff __1,MechanicsContext __2,ref Buff __result){__result=Fact(__1,__2);facts.List.Add(__result);children.Add(__result);Refresh();return false;}

    static bool Remove(Buff __instance){facts.List.Remove(__instance);children.Remove(__instance);Refresh();return false;}

    static bool Store(Buff __instance,EntityFact fact){if(!stored.ContainsKey(__instance))stored[__instance]=new List<Buff>();stored[__instance].Add((Buff)fact);return false;}

    static bool DemoralizeFixture(){var e=new RuleSkillCheck(unit,StatType.CheckIntimidate,10){Take10ForSuccess=true};unit.Stats.GetStat(StatType.CheckIntimidate).BaseValue=roll-10;((Queue<int>)AccessTools.Field(typeof(Psychic070Tests),"dice").GetValue(null)).Enqueue(10);e.OnTrigger(null);((Queue<int>)AccessTools.Field(typeof(Psychic070Tests),"dice").GetValue(null)).Clear();RulebookEventBus.OnEventDidTrigger(e);return false;}

}

