using System;
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

internal static class PsychicBattleSpellTests
{
    static int count,removed;static bool passed;static ItemEntityArmor worn;
    static readonly List<ContextActionDealDamage> damage=new List<ContextActionDealDamage>();
    static readonly List<object> damageInfo=new List<object>();
    static readonly List<ContextActionApplyBuff> buffs=new List<ContextActionApplyBuff>();
    static readonly List<RuleSavingThrow> saves=new List<RuleSavingThrow>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Battle spells: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicWoundedTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static T Ref<T>(SimpleBlueprint bp) where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
    static void Refresh(EntityFactsManager facts){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static EntityFactsManager Facts(Psychic070Tests.Caster a){var f=new EntityFactsManager(a.Unit);Set(a.Unit,"Facts",f);return f;}
    static void Add(EntityFactsManager facts,BlueprintFeature bp){var f=Empty<Feature>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"<IsActive>k__BackingField",true);f.RestoreOwnerLink(facts);facts.List.Add(f);Refresh(facts);}
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx,EntityFactsManager facts){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    static void Params(MechanicsContext ctx,BlueprintAbility bp,int cl,Metamagic mm=0){ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,Metamagic=mm});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(ctx);Set(ctx,"m_Ranks",ranks);}
    static void Reset(){damage.Clear();damageInfo.Clear();buffs.Clear();saves.Clear();}
    internal static int Run(BlueprintsCache cache)
    {
        var actor=new Psychic070Tests.Caster(cache);var target=new Psychic070Tests.Caster(cache);var af=Facts(actor);var tf=Facts(target);
        var h=new Harmony("CotW.BattleSpells.Managed");
        void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicBattleSpellTests),n));
        Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));
        Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));
        Patch(AccessTools.Method(typeof(ContextActionRemoveSelf),"RunAction"),nameof(Remove));
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));
        Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"ImprovedEvasion"),nameof(False));
        Patch(AccessTools.Method(typeof(ContextActionSavingThrow),"CreateSavingThrow"),nameof(Create));
        PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicBattleSpellTests),nameof(Trigger)));
        try {
            string[] tokens={"FlurryOfSnowballs","TouchOfBloodletting","FlameArrow","Rebuke","SheetLightning"};int[][] levels={new[]{2,2,2},new[]{1,1,2},new[]{3,3},new[]{4},new[]{3,3}};
            for(int i=0;i<tokens.Length;i++) {
                var bp=Asset<BlueprintAbility>(cache,tokens[i]);var links=bp.GetComponents<SpellListComponent>().ToArray();
                Check(links.Select(l=>l.SpellLevel).OrderBy(x=>x).SequenceEqual(levels[i]),"Original spell levels "+tokens[i]);
                foreach(var l in links)Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(a=>a==bp)==1,"Reciprocal unique list "+tokens[i]);
                Check(!links.Any(l=>l.SpellList==actor.Book.Blueprint.SpellList),"No invented Psychic learning "+tokens[i]);
                Check(bp.Parent==null&&!ReferenceEquals(bp.Icon,null),"Learnable root and original icon "+tokens[i]);
            }
            Snow(cache,actor,target);Blood(cache,actor,target,tf);Arrows(cache,actor,target,af);Rebuke(cache,actor,target,af,tf);Lightning(cache,actor,target,tf,h);
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["battleSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" five-spell checks: native damage info/metamagic, seeded saves/branches/DC, weapon callbacks, wounded targets, conditions/healing and durations. Scene buff/final HP boundaries captured; not gameplay.");return count;
    }
    static MechanicsContext Context(Psychic070Tests.Caster a,BlueprintAbility bp){var ctx=a.Cast(bp.School,0,3).Context;Set(ctx,"AssociatedBlueprint",bp);Set(ctx,"<ClickedTarget>k__BackingField",new TargetWrapper(a.Unit));return ctx;}
    static void Snow(BlueprintsCache c,Psychic070Tests.Caster a,Psychic070Tests.Caster target) {
        var bp=Asset<BlueprintAbility>(c,"FlurryOfSnowballs");var ctx=Context(a,bp);var run=bp.GetComponent<AbilityEffectRunAction>();var delivery=bp.GetComponent<AbilityDeliverProjectile>();
        Check(delivery.Length.Value==30&&delivery.Projectiles.Single().AssetGuid.ToString()=="72b45860bdfb81f4284aa005c04594dd","Native 30-foot cold cone projectile");
        Check(!bp.SpellResistance&&run.SavingThrowType==SavingThrowType.Reflex&&!bp.GetComponents<ContextRankConfig>().Any(),"No SR, Reflex, no level scaling");
        foreach(int cl in new[]{1,3,10,20,40})foreach(bool save in new[]{false,true})foreach(Metamagic mm in new[]{(Metamagic)0,Metamagic.Empower,Metamagic.Maximize,Metamagic.Empower|Metamagic.Maximize}){
            Params(ctx,bp,cl,mm);passed=save;Reset();using(ctx.GetDataScope(target.Unit))using(SaveScope())run.Actions.Run();
            Check(damage.Count==1&&damage[0].DamageType.Energy==Kingmaker.Enums.Damage.DamageEnergyType.Cold,"One cold packet");
            var info=damageInfo.Single();var dice=(DiceFormula)Get(info,"Dices");Check(dice.Rolls==4&&dice.Dice==DiceType.D6&&(int)Get(info,"Bonus")==0,"Native fixed4d6");
            Check((bool)Get(info,"HalfBecauseSavingThrow")==save,"Native save half flag");
            Check((bool)Get(info,"Empower")==mm.HasFlag(Metamagic.Empower)&&(bool)Get(info,"Maximize")==mm.HasFlag(Metamagic.Maximize),"Native damage metamagic");
        }
    }
    static void Blood(BlueprintsCache c,Psychic070Tests.Caster a,Psychic070Tests.Caster target,EntityFactsManager facts) {
        var cast=Asset<BlueprintAbility>(c,"TouchOfBloodletting");var bp=Asset<BlueprintAbility>(c,"TouchOfBloodlettingTouch");var buff=Asset<BlueprintBuff>(c,"TouchOfBloodlettingBuff");var ctx=Context(a,bp);var run=bp.GetComponent<AbilityEffectRunAction>();
        Check(cast.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility==bp&&bp.GetComponent<AbilityDeliverTouch>()!=null,"Native sticky cast/delivery pair");
        Check(cast.GetComponent<PsychicWoundedTarget>()!=null&&bp.GetComponent<PsychicWoundedTarget>()!=null&&bp.SpellResistance&&run.SavingThrowType==SavingThrowType.Will,"Wounded gate on both cast and delivery, Will/SR");
        var hp=Empty<ModifiableValueHitPoints>();Set(target.Unit.Stats,"HitPoints",hp);var wound=bp.GetComponent<PsychicWoundedTarget>();
        foreach(int max in new[]{1,10,100,1000})foreach(int loss in new[]{0,1,max,max+1}){
            Set(hp,"<ModifiedValue>k__BackingField",max);target.Descriptor.Damage=loss;Check(wound.IsTargetRestrictionPassed(a.Unit,new TargetWrapper(target.Unit))==(loss>0),"Strict injured target threshold");
        }
        foreach(string id in new[]{"734a29b693e9ec346ba2951b27987e33","fd389783027d63343b4a5634bd81645f","198fd8924dabcb5478d0f78bd453c586"}){
            facts.List.Clear();Refresh(facts);var check=bp.GetComponent<AbilityTargetHasFact>();Check(check.IsTargetRestrictionPassed(a.Unit,new TargetWrapper(target.Unit)),"Living target accepted");Add(facts,(BlueprintFeature)c.Load(BlueprintGuid.Parse(id)));Check(!check.IsTargetRestrictionPassed(a.Unit,new TargetWrapper(target.Unit)),"Each excluded creature type blocked");
        }facts.List.Clear();Refresh(facts);
        foreach(bool save in new[]{false,true}){passed=save;Reset();using(ctx.GetDataScope(target.Unit))using(SaveScope())run.Actions.Run();Check(buffs.Count==(save?0:1),"Will negates all bloodletting effects");}
        var apply=(ContextActionApplyBuff)((ContextActionConditionalSaved)run.Actions.Actions.Single()).Failed.Actions.Single();
        foreach(int cl in new[]{1,5,20,40})foreach(bool extend in new[]{false,true}){Params(ctx,bp,cl,extend?Metamagic.Extend:0);Check(apply.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(cl*6*(extend?2:1)),"Bloodletting timed duration/Extend");}
        Check(buff.Stacking==StackingType.Replace&&buff.GetComponent<SpellDescriptorComponent>().Descriptor==(SpellDescriptor.Bleed|SpellDescriptor.Exhausted),"Bleed/exhaustion immunity tags and replace");
        var native=(BlueprintBuff)c.Load(BlueprintGuid.Parse("46d1b9cc3d0fd36469a471b047d773a2"));Check(buff.GetComponents<AddCondition>().Select(x=>x.Condition).SequenceEqual(native.GetComponents<AddCondition>().Select(x=>x.Condition)),"Native exhaustion conditions copied");
        foreach(var x in buff.ComponentsArray)Check(!native.ComponentsArray.Contains(x),"No shared mutable exhaustion component");
        var fact=Fact(buff,ctx,facts);var heal=buff.GetComponent<AddHealTrigger>();var rt=Runtime(heal,fact);var evt=new RuleHealDamage(a.Unit,target.Unit,new DiceFormula(1,DiceType.D6),0);Set(evt,"<Value>k__BackingField",1);removed=0;using(rt.RequestEventContext())heal.OnEventDidTrigger(evt);Check(removed==1,"Healing removes whole exhaustion/bleed fact");
        Reset();using(ctx.GetDataScope(target.Unit))buff.GetComponent<AddFactContextActions>().NewRound.Run();Check(damageInfo.Count==1&&(int)Get(damageInfo[0],"Bonus")==1&&((DiceFormula)Get(damageInfo[0],"Dices")).Rolls==0,"Native one-point round bleed");
    }
    static void Arrows(BlueprintsCache c,Psychic070Tests.Caster a,Psychic070Tests.Caster target,EntityFactsManager facts) {
        var bp=Asset<BlueprintAbility>(c,"FlameArrow");var buff=Asset<BlueprintBuff>(c,"FlameArrowBuff");var ctx=Context(a,bp);Params(ctx,bp,10);
        Check(!bp.SpellResistance&&bp.CanTargetFriends&&bp.CanTargetSelf&&!bp.CanTargetEnemies&&buff.Stacking==StackingType.Replace,"Friendly nonstacking enchantment");
        var component=buff.GetComponent<PsychicFlameArrowDamage>();Check(component.Categories.Length==10&&component.Categories.Distinct().Count()==10,"Ten disjoint projectile categories");
        Check(!buff.GetComponents<AddInitiatorAttackWithWeaponTrigger>().Any(),"Old separate damage trigger removed, cannot double count");
        var fact=Fact(buff,ctx,facts);var runtime=Runtime(component,fact);
        foreach(WeaponCategory cat in Enum.GetValues(typeof(WeaponCategory)))foreach(Metamagic mm in new[]{(Metamagic)0,Metamagic.Empower,Metamagic.Maximize,Metamagic.Empower|Metamagic.Maximize}) {
            Params(ctx,bp,10,mm);
            var type=new BlueprintWeaponType {Category=cat};var weaponBp=new BlueprintItemWeapon();Set(weaponBp,"m_Type",Ref<BlueprintWeaponTypeReference>(type));var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",weaponBp);
            var stats=Empty<RuleCalculateWeaponStats>();Set(stats,"Weapon",weapon);Set(stats,"DamageDescription",new List<DamageDescription>());
            using(runtime.RequestEventContext())component.OnEventAboutToTrigger(stats);
            bool expected=component.Categories.Contains(cat);Check(stats.DamageDescription.Count==(expected?1:0),"Equipment preview packet for "+cat);
            if(!expected)continue;
            var added=stats.DamageDescription.Single();Check(added.Dice.Rolls==1&&added.Dice.Dice==DiceType.D6&&added.TypeDescription.Energy==Kingmaker.Enums.Damage.DamageEnergyType.Fire&&added.SourceFact==fact,"Preview exact1d6 fire, tracked owner");
            var preview=new List<DamageDescription>{new DamageDescription {Dice=new DiceFormula(1,DiceType.D6),TypeDescription=new DamageTypeDescription {Type=DamageType.Physical}},added};
            var ui=Kingmaker.UI.Common.UIUtilityItem.MechanicDamageToUI(preview,null);
            int min=ui.Sum(v=>(int)AccessTools.Method(typeof(Kingmaker.UI.Common.UIUtilityItem),"MinDiceValue").Invoke(null,new object[]{v}));
            int max=ui.Sum(v=>(int)AccessTools.Method(typeof(Kingmaker.UI.Common.UIUtilityItem),"MaxDiceValue").Invoke(null,new object[]{v}));
            Check(ui.Count==2&&min==2&&max==12&&ui[1].Damage.Dice.Dice==DiceType.D6,"Native character-screen conversion displays2-12 for1d6 weapon plus1d6 fire");
            var d=added.CreateDamage();d.CriticalModifier=4;
            var other=new EnergyDamage(new DiceFormula(1,DiceType.D6),0,Kingmaker.Enums.Damage.DamageEnergyType.Cold);other.CriticalModifier=3;
            var bundle=new DamageBundle(d,other);var deal=new RuleDealDamage(a.Unit,target.Unit,bundle);var prepare=new RulePrepareDamage(deal);
            using(runtime.RequestEventContext())component.OnEventAboutToTrigger(prepare);
            Check(d.CriticalModifier==1&&other.CriticalModifier==3,"Only own fire packet protected from critical multiplication");
            Check(d.EmpowerBonus.Value==(mm.HasFlag(Metamagic.Empower)?1.5f:1f),"Original Empower applies only to spell die");
            Check((d.CalculationType.Value==DamageCalculationType.Maximized)==mm.HasFlag(Metamagic.Maximize),"Original Maximize preserved");
            Check(other.EmpowerBonus.Value==1f&&other.CalculationType.Value!=DamageCalculationType.Maximized&&prepare.DamageBundle.Count()==2,"No extra packet and no changes to other damage");
        }
        var apply=(ContextActionApplyBuff)bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();foreach(int cl in new[]{1,5,20,40})foreach(bool extend in new[]{false,true}){Params(ctx,bp,cl,extend?Metamagic.Extend:0);Check(apply.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromMinutes(cl*(extend?2:1)),"Arrow duration and Extend");}
    }
    static void Rebuke(BlueprintsCache c,Psychic070Tests.Caster a,Psychic070Tests.Caster target,EntityFactsManager af,EntityFactsManager tf) {
        var bp=Asset<BlueprintAbility>(c,"Rebuke");var ctx=Context(a,bp);var run=bp.GetComponent<AbilityEffectRunAction>();var branch=(Conditional)run.Actions.Actions.Single();var condition=(PsychicSameDeity)branch.ConditionsChecker.Conditions.Single();
        Check(bp.SpellResistance&&run.SavingThrowType==SavingThrowType.Fortitude&&(TargetType)Get(bp.GetComponent<AbilityTargetsAround>(),"m_TargetType")==TargetType.Enemy,"Rebuke Fortitude/SR enemies only");
        var gods=condition.Deities.Get().AllFeatures.Where(x=>x!=null&&x.AssetGuid.ToString()!="92c0d2da0a836ce418a267093c09ca54").Take(2).ToArray();Check(gods.Length==2,"Real deity fixture entries resolved");
        foreach(bool same in new[]{false,true})foreach(int cl in new[]{1,2,5,9,10,11,20,40})foreach(bool save in new[]{false,true}){
            af.List.Clear();tf.List.Clear();Refresh(af);Refresh(tf);Add(af,gods[0]);Add(tf,gods[same?0:1]);Params(ctx,bp,cl);passed=save;Reset();
            using(ctx.GetDataScope(target.Unit))using(SaveScope())run.Actions.Run();
            Check(damage.Count==2&&damage.All(d=>d.Half&&d.HalfIfSaved),"Two half packets sonic/divine");Check(damage[0].DamageType.Energy==Kingmaker.Enums.Damage.DamageEnergyType.Sonic&&damage[1].DamageType.Energy==Kingmaker.Enums.Damage.DamageEnergyType.Divine,"Damage types preserved");
            foreach(var info in damageInfo){var dice=(DiceFormula)Get(info,"Dices");Check(dice.Rolls==(same?Math.Min(cl,10):Math.Min(cl/2,5))&&dice.Dice==(same?DiceType.D6:DiceType.D8),"Actual rank/cap per faith branch CL="+cl);Check((bool)Get(info,"HalfBecauseSavingThrow")==save,"Additional half on successful save");}
            Check(buffs.Count==(save?0:1),"Success negates condition");if(!save)Check(buffs[0].Buff.AssetGuid.ToString()==(same?"09d39b38bb7c6014394b6daced9bacd3":"df3950af5a783bd4d91ab73eb8fa0fd3"),"Shared deity stuns, otherwise staggers");
        }
        af.List.Clear();tf.List.Clear();Refresh(af);Refresh(tf);using(ctx.GetDataScope(target.Unit))Check(!branch.ConditionsChecker.Check(),"Two missing deities do not count as shared");
        var atheism=(BlueprintFeature)c.Load(BlueprintGuid.Parse("92c0d2da0a836ce418a267093c09ca54"));Add(af,atheism);Add(tf,atheism);using(ctx.GetDataScope(target.Unit))Check(!branch.ConditionsChecker.Check(),"Atheism is not worship of a shared god");
    }
    static void Lightning(BlueprintsCache c,Psychic070Tests.Caster a,Psychic070Tests.Caster target,EntityFactsManager facts,Harmony h) {
        var bp=Asset<BlueprintAbility>(c,"SheetLightning");var ctx=new MechanicsContext(a.Unit,target.Descriptor,bp,null,new TargetWrapper(target.Unit));var run=bp.GetComponent<AbilityEffectRunAction>();var living=(Conditional)run.Actions.Actions[1];var save=(ContextActionSavingThrow)living.IfFalse.Actions.Single();var bonus=(ContextActionSavingThrow.ConditionalDCIncrease[])Get(save,"m_ConditionalDCIncrease");var metal=(PsychicMetalArmor)bonus.Single().Condition.Conditions.Single();
        var body=Empty<UnitBody>();Set(target.Descriptor,"<Body>k__BackingField",body);var slot=Empty<ArmorSlot>();Set(body,"Armor",slot);Set(slot,"<Owner>k__BackingField",target.Descriptor);h.Patch(AccessTools.PropertyGetter(typeof(ArmorSlot),"MaybeArmor"),prefix:new HarmonyMethod(typeof(PsychicBattleSpellTests),nameof(Armor)));
        Check(bp.SpellResistance&&(TargetType)Get(bp.GetComponent<AbilityTargetsAround>(),"m_TargetType")==TargetType.Any&&((Feet)Get(bp.GetComponent<AbilityTargetsAround>(),"m_Radius")).Value==20,"20-foot area includes allies, SR applies");
        foreach(string race in new[]{"living","734a29b693e9ec346ba2951b27987e33","fd389783027d63343b4a5634bd81645f","198fd8924dabcb5478d0f78bd453c586"})foreach(int armor in new[]{0,1,2})foreach(bool success in new[]{false,true})foreach(bool persistent in new[]{false,true}){
            facts.List.Clear();Refresh(facts);if(race!="living")Add(facts,(BlueprintFeature)c.Load(BlueprintGuid.Parse(race)));worn=null;
            if(armor>0){var armorBp=new BlueprintItemArmor();var restriction=new EquipmentRestrictionClass {Not=armor==2};Set(restriction,"m_Class",metal.Druid);armorBp.ComponentsArray=new BlueprintComponent[]{restriction};worn=Empty<ItemEntityArmor>();Set(worn,"m_Blueprint",armorBp);}
            Params(ctx,bp,10,persistent?Metamagic.Persistent:0);passed=success;Reset();using(ctx.GetDataScope(target.Unit))run.Actions.Run();
            bool isLiving=race=="living"||race=="198fd8924dabcb5478d0f78bd453c586";
            Check(damage.Count==1&&(int)Get(damageInfo.Single(),"Bonus")==1&&!damage[0].HalfIfSaved,"Unconditional one electricity damage");
            Check(saves.Count==(isLiving?(persistent&&success?2:1):0),"Native living-only save and Persistent second save");
            Check(saves.All(s=>s.DifficultyClassBase==23+(armor==2?2:0)),"Metal armor +2DC; no nonmetal penalty");
            Check(buffs.Count==(isLiving?1:0),"Nonliving receives damage only");
            if(isLiving)Check(buffs[0].Buff==(success?(BlueprintBuff)c.Load(BlueprintGuid.Parse("df6d1025da07524429afbae248845ecc")):Asset<BlueprintBuff>(c,"SheetLightningDazeBuff")),"Native failed/successful daze/dazzle branches");
        }
        var daze=Asset<BlueprintBuff>(c,"SheetLightningDazeBuff");Check(daze.GetComponent<SpellDescriptorComponent>().Descriptor==SpellDescriptor.Daze,"Nonmental daze retains actual daze immunity descriptor");
        facts.List.Clear();Refresh(facts);
    }
    static IDisposable SaveScope(){var t=typeof(MechanicsContext).Assembly.GetType("Kingmaker.UnitLogic.Mechanics.Actions.SavingThrowData",true);var scope=AccessTools.Method(typeof(Kingmaker.ElementsSystem.ContextData<>).MakeGenericType(t),"Request").Invoke(null,null);AccessTools.Method(t,"Setup").Invoke(scope,new object[]{Empty<RuleSavingThrow>()});return (IDisposable)scope;}
    static bool Damage(ContextActionDealDamage __instance){damage.Add(__instance);damageInfo.Add(AccessTools.Method(typeof(ContextActionDealDamage),"GetDamageInfo").Invoke(__instance,null));return false;}
    static bool Apply(ContextActionApplyBuff __instance){buffs.Add(__instance);return false;}
    static bool Remove(){removed++;return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}
    static bool False(ref bool __result){__result=false;return false;}
    static bool Create(ContextActionSavingThrow __instance,int dc,bool persistentSpell,ref RuleSavingThrow __result){__result=Empty<RuleSavingThrow>();Set(__result,"Type",__instance.Type);Set(__result,"DifficultyClassBase",dc);__result.PersistentSpell=persistentSpell;return false;}
    static bool Trigger(RuleSavingThrow rule,ref RuleSavingThrow __result){saves.Add(rule);__result=rule;return false;}
    static bool Armor(ref ItemEntityArmor __result){__result=worn;return false;}
}
