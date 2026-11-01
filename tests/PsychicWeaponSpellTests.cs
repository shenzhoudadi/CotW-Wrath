using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
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
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicWeaponSpellTests
{
    static int count,removed;static EntityFactsManager facts;static MechanicsContext context;
    static HandSlot primary,secondary;static ItemEntityWeapon main,off;static bool savePassed;
    static readonly List<BlueprintBuff> delivered=new List<BlueprintBuff>();
    static readonly Dictionary<ItemEntity,List<ItemEnchantment>> enchants=new Dictionary<ItemEntity,List<ItemEnchantment>>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Weapon spells: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicWeaponTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static T Ref<T>(SimpleBlueprint bp) where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
    static EntityFactComponent Runtime(BlueprintComponent c,EntityFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    static Buff Fact(BlueprintBuff bp){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",context);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static void Params(BlueprintAbility bp,int cl,bool extend=false){context.SetParams(new AbilityParams {CasterLevel=cl,DC=27,Metamagic=extend?Metamagic.Extend:0});var ranks=new int[32];foreach(var r in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(r,"m_Type")]=r.GetValue(context);Set(context,"m_Ranks",ranks);}
    static ItemEntityWeapon Weapon(WeaponCategory cat,bool melee=true){var type=new BlueprintWeaponType {Category=cat};Set(type,"m_IsUnarmed",cat==WeaponCategory.UnarmedStrike);Set(type,"m_IsNatural",new[]{WeaponCategory.Claw,WeaponCategory.Bite,WeaponCategory.Gore,WeaponCategory.OtherNaturalWeapons}.Contains(cat));SetEnum(type,"m_AttackType",melee?"Melee":"Ranged");var bp=new BlueprintItemWeapon();Set(bp,"m_Type",Ref<BlueprintWeaponTypeReference>(type));var w=Empty<ItemEntityWeapon>();Set(w,"m_Blueprint",bp);enchants[w]=new List<ItemEnchantment>();return w;}
    static void SetEnum(object o,string n,string v){var f=AccessTools.Field(o.GetType(),n);f.SetValue(o,Enum.Parse(f.FieldType,v));}
    internal static int Run(BlueprintsCache cache){
        var caster=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(caster.Unit);Set(caster.Unit,"Facts",facts);var collection=new BuffCollection();collection.Initialize(facts);Set(caster.Unit,"<Buffs>k__BackingField",collection);
        context=caster.Cast(SpellSchool.Transmutation,0,2).Context;Set(context,"<ClickedTarget>k__BackingField",new TargetWrapper(caster.Unit));
        primary=Empty<HandSlot>();secondary=Empty<HandSlot>();var body=Empty<UnitBody>();Set(caster.Descriptor,"<Body>k__BackingField",body);
        var h=new Harmony("CotW.WeaponSpell.Managed");void Patch(Type t,string n,string p)=>h.Patch(AccessTools.Method(t,n),prefix:new HarmonyMethod(typeof(PsychicWeaponSpellTests),p));
        try{
            Patch(typeof(UnitBody),"get_PrimaryHand",nameof(Primary));Patch(typeof(UnitBody),"get_SecondaryHand",nameof(Secondary));Patch(typeof(WeaponSlot),"get_MaybeWeapon",nameof(Equipped));Patch(typeof(HandSlot),"get_MaybeWeapon",nameof(Equipped));
            Patch(typeof(ItemEntity),"get_Enchantments",nameof(Enchantments));Patch(typeof(ItemEntity),"AddEnchantment",nameof(Add));Patch(typeof(ItemEntity),"RemoveEnchantment",nameof(RemoveEnchant));Patch(typeof(Buff),"Remove",nameof(RemoveBuff));
            Patch(typeof(RuleSkillCheck),"AdjustDCByDifficulty",nameof(Difficulty));Patch(typeof(ContextActionApplyBuff),"RunAction",nameof(Apply));Patch(typeof(RuleSavingThrow),"get_IsPassed",nameof(Passed));
            string[] roots={"DazzlingBlade","DazzlingBladeMass","VineStrike","FlamesOfTheFaithful","WrathfulWeapon"};int[][] levels={new[]{1,1},new[]{3},new[]{2,2,2,2},new[]{2},new[]{4}};
            for(int i=0;i<roots.Length;i++){
                var bp=Asset<BlueprintAbility>(cache,roots[i]);var links=bp.GetComponents<SpellListComponent>().ToArray();Check(links.Select(l=>l.SpellLevel).OrderBy(x=>x).SequenceEqual(levels[i]),"Original class levels "+roots[i]);Check(bp.Parent==null&&bp.Type==AbilityType.Spell,"Learnable root "+roots[i]);
                foreach(var link in links)Check(link.SpellList.SpellsByLevel[link.SpellLevel].Spells.Count(s=>s==bp)==1,"Unique reciprocal list "+roots[i]);
                Check(!links.Any(l=>l.SpellList==caster.Book.Blueprint.SpellList),"No invented Psychic link "+roots[i]);
                var variants=bp.GetComponent<AbilityVariants>()?.Variants.ToArray()??new[]{bp};
                foreach(var a in variants){Check(a==bp||a.Parent==bp,"Variant points at known spell");foreach(int cl in new[]{1,3,5,12,20,40})foreach(bool extend in new[]{false,true}){
                    Params(a,cl,extend);var apply=a.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromSeconds(cl*(roots[i]=="FlamesOfTheFaithful"?6:60)*(extend?2:1)),"Native duration and Extend "+a.name);Check(!apply.AsChild&&apply.IsFromSpell&&!apply.IsNotDispelable&&apply.Buff.Stacking==StackingType.Replace,"Timed dispellable nonstacking parent");
                }}
            }
            Bindings(cache,caster);Dazzle(cache,caster);Vines(cache,caster);
        }finally{h.UnpatchAll(h.Id);enchants.Clear();}
        PsychicExpansionTests.Inventory["weaponSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" weapon spell checks; native durations, component lifetimes, native persistent data ownership, judgment transitions, weapon restrictions, disarm bonus and save branches. Scene enchantment/buff endpoints substituted.");return count;
    }
    static void Bindings(BlueprintsCache cache,Psychic070Tests.Caster caster){
        var root=Asset<BlueprintAbility>(cache,"WrathfulWeapon");Check(root.GetComponent<AbilityVariants>().Variants.Length==8,"Four properties by two hands");
        foreach(var a in root.GetComponent<AbilityVariants>().Variants){
            main=Weapon(WeaponCategory.Longsword);off=Weapon(WeaponCategory.Dagger);var gate=a.GetComponent<PsychicWeaponTarget>();var b=((ContextActionApplyBuff)a.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single()).Buff;var c=b.GetComponent<PsychicFixedWeaponEnchantment>();var fact=Fact(b);var rt=Runtime(c,fact);var target=c.OffHand?off:main;var other=c.OffHand?main:off;
            Check(gate.IsTargetRestrictionPassed(caster.Unit,new TargetWrapper(caster.Unit)),"Manufactured correct hand accepted");rt.TurnOn();Check(enchants[target].Count==2&&enchants[other].Count==0,"Anchor plus property on selected weapon only");Check(!gate.IsTargetRestrictionPassed(caster.Unit,new TargetWrapper(caster.Unit)),"Existing selected property rejected");
            var data=rt.GetData<BuffEnchantWornItemData>();using(rt.RequestEventContext()){c.OnNewRound();c.HandleEquipmentSlotUpdated(null,null);}Check(data.Enchantments.Count==2,"Round/irrelevant slot stable");
            // Native persistent data owns only enchantments created by this component.
            Check(data.Enchantments.All(e=>ReferenceEquals(e.Owner,target)),"Persistent native data references original weapon");rt.TurnOff();Check(enchants[target].Count==0&&data.Enchantments.Count==0,"Off releases only owned temporary enchantments");rt.TurnOn();Check(enchants[target].Count==2,"Resume exactly once");
            var permanent=MakeEnchant(target,c.Effect.Get());enchants[target].Add(permanent);rt.TurnOff();Check(enchants[target].SequenceEqual(new[]{permanent}),"Foreign same-property enchantment retained");rt.TurnOn();Check(enchants[target].Count==2&&data.Enchantments.Count==1,"Existing foreign property gets only anchor");
            removed=0;if(c.OffHand)off=Weapon(WeaponCategory.Dagger);else main=Weapon(WeaponCategory.Longsword);using(rt.RequestEventContext())c.HandleUnitChangeActiveEquipmentSet(caster.Descriptor);Check(removed==1,"Switching weapon ends spell instead of moving enchantment");rt.TurnOff();Check(enchants[target].SequenceEqual(new[]{permanent}),"Switch cleanup preserves permanent property");
        }
        main=Weapon(WeaponCategory.Longsword);off=null;var flames=Asset<BlueprintBuff>(cache,"FlamesOfTheFaithfulBuff");var fc=flames.GetComponent<PsychicFixedWeaponEnchantment>();var fr=Runtime(fc,Fact(flames));
        Check(fc.Burst.Get().ComponentsArray.Length==1&&fc.Burst.Get().GetComponent<WeaponEnergyBurst>()!=null,"Burst supplement has no second 1d6 fire component");
        fr.TurnOn();Check(enchants[main].Count==2,"No judgment: anchor and flaming");var judgment=Fact(fc.Judgment.Get());facts.List.Add(judgment);Refresh();using(fr.RequestEventContext())fc.HandleUnitGainFact(judgment);Check(enchants[main].Count==3,"Judgment immediately adds critical burst");
        using(fr.RequestEventContext()){fc.OnNewRound();fc.HandleUnitGainFact(judgment);}Check(enchants[main].Count==3,"Repeated judgment notifications never stack");using(fr.RequestEventContext())fc.HandleUnitLostFact(judgment);facts.List.Remove(judgment);Refresh();Check(enchants[main].Count==2,"Lost notification removes burst even while fact still in collection");
        var foreign=MakeEnchant(main,fc.NativeBurst.Get());enchants[main].Add(foreign);using(fr.RequestEventContext())fc.OnNewRound();Check(enchants[main].Count==2&&enchants[main].Contains(foreign),"Existing full flaming burst suppresses own normal fire die");fr.TurnOff();Check(enchants[main].SequenceEqual(new[]{foreign}),"Foreign full burst survives cleanup");fr.TurnOn();Check(enchants[main].Count==2,"Casting on full burst adds no damage duplicates");fr.TurnOff();
        foreach(var category in new[]{WeaponCategory.UnarmedStrike,WeaponCategory.Claw,WeaponCategory.Longsword,WeaponCategory.Longbow}){
            main=Weapon(category,category!=WeaponCategory.Longbow);var gate=Asset<BlueprintAbility>(cache,"DazzlingBlade").GetComponent<PsychicWeaponTarget>();Check(gate.IsTargetRestrictionPassed(caster.Unit,new TargetWrapper(caster.Unit))==(category==WeaponCategory.Longsword),"Dazzling manufactured melee only "+category);
        }main=null;Check(!Asset<BlueprintAbility>(cache,"FlamesOfTheFaithful").GetComponent<PsychicWeaponTarget>().IsCasterRestrictionPassed(caster.Unit),"Empty main hand blocked");
    }
    static void Dazzle(BlueprintsCache cache,Psychic070Tests.Caster caster){
        main=Weapon(WeaponCategory.Longsword);var b=Asset<BlueprintBuff>(cache,"DazzlingBladeBuff");var release=Asset<BlueprintAbility>(cache,"DazzlingBladeRelease");Check(release.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free&&release.Range==AbilityRange.Touch,"Free adjacent discharge");
        Check(b.GetComponent<AddFacts>().Facts.Single()==release&&b.GetComponent<ReplaceAbilityParamsWithContext>().Ability==release,"Temporary button with original cast parameters");Check(release.GetComponent<AbilityExecuteActionOnCast>().Actions.Actions.OfType<ContextActionRemoveBuff>().Single().Buff==b,"Discharge consumes own parent even with immune target");
        Check(typeof(ManeuverDefenceBonus).GetInterfaces().Contains(typeof(Kingmaker.PubSubSystem.ITargetRulebookHandler<RuleCalculateCMD>))&&typeof(PsychicDazzlingBonuses).GetInterfaces().Contains(typeof(Kingmaker.PubSubSystem.ITargetRulebookHandler<RuleCalculateCMD>)),"CMD subscribes as defender, matching native component");var native=b.GetComponent<CMBBonusForManeuver>();var bonus=b.GetComponent<PsychicDazzlingBonuses>();var fact=Fact(b);var rt=Runtime(bonus,fact);
        foreach(int cl in new[]{1,2,3,5,6,9,12,20,40}){
            context.SetParams(new AbilityParams {CasterLevel=cl,DC=27});var ranks=new int[32];ranks[0]=b.GetComponent<ContextRankConfig>().GetValue(context);Set(context,"m_Ranks",ranks);Check(native.Value.Calculate(context)==Math.Min(5,1+cl/3),"Disarm CL scaling and cap");
            foreach(CombatManeuver type in Enum.GetValues(typeof(CombatManeuver))){var evt=new RuleCalculateCMD(caster.Unit,caster.Unit,type);using(rt.RequestEventContext())bonus.OnEventAboutToTrigger(evt);Check(evt.AllBonuses.Sum(x=>x.Value)==(type==CombatManeuver.Disarm?Math.Min(5,1+cl/3):0),"CMD bonus only disarm");}
        }
        // Native stat storage is visible both to Feint's pre-check properties and its actual RuleSkillCheck.
        var bluff=b.GetComponent<AddContextStatBonus>();Check(bluff.Stat==StatType.CheckBluff&&bluff.Descriptor==ModifierDescriptor.Competence,"Bluff-only native competence bonus");
        var stat=new ModifiableValue(caster.Unit.Stats,StatType.CheckBluff);stat.BaseValue=0;caster.Unit.Stats.AllStats.Add(stat);var br=Runtime(bluff,Fact(b));
        foreach(int cl in new[]{1,2,3,5,6,9,12,20,40})foreach(int other in new[]{0,2,7}){
            context.SetParams(new AbilityParams {CasterLevel=cl});var ranks=new int[32];ranks[0]=b.GetComponent<ContextRankConfig>().GetValue(context);Set(context,"m_Ranks",ranks);
            var foreign=stat.AddModifier(other,(EntityFact)null,ModifierDescriptor.Competence);
            br.TurnOn();Check(stat.ModifiedValue==Math.Max(other,Math.Min(5,1+cl/3)),"Native Bluff amount/cap/competence nonstacking without combat gate");
            Check(stat.Modifiers.Any(m=>m.Source?.Blueprint==b),"Native modifier retains source for skill log");
            var skill=new RuleSkillCheck(caster.Unit,StatType.CheckBluff,5) {Take10ForSuccess=true};skill.OnTrigger(null);
            Check(skill.TotalBonus==stat.ModifiedValue&&skill.Success,"Actual native Bluff roll uses the bonus and succeeds");
            Check(skill.StatModifiersAtTheMoment.Any(m=>m.Source?.Blueprint==b)==(Math.Min(5,1+cl/3)>other),"Native skill log displays winning competence source CL="+cl+" other="+other);
            br.TurnOff();Check(stat.ModifiedValue==other,"Expiry/suppression removes own Bluff bonus only");stat.RemoveModifier(foreign);Check(stat.ModifiedValue==0,"No Bluff residue after repeated resume");
        }
        var run=release.GetComponent<AbilityEffectRunAction>();Check(run.SavingThrowType==SavingThrowType.Will&&!release.SpellResistance,"Will partial, no SR discharge");foreach(bool pass in new[]{false,true}){savePassed=pass;delivered.Clear();using(context.GetDataScope(caster.Unit))using(SaveScope())run.Actions.Run();Check(delivered.Single().AssetGuid.ToString()==(pass?"df6d1025da07524429afbae248845ecc":"187f88d96a0ef464280706b63635f2af"),"Native blind/dazzle branch");}
        var mass=Asset<BlueprintAbility>(cache,"DazzlingBladeMass");var area=mass.GetComponent<AbilityTargetsAround>();Check(((Feet)Get(area,"m_Radius")).Value==15&&(TargetType)Get(area,"m_TargetType")==TargetType.Ally&&mass.Range==AbilityRange.Close,"Mass close ally 15-foot burst");var predicate=((Kingmaker.ElementsSystem.ConditionsChecker)Get(area,"m_Condition")).Conditions.Single();using(context.GetDataScope(caster.Unit))Check(predicate.Check(),"Mass valid weapon selected");main=null;using(context.GetDataScope(caster.Unit))Check(!predicate.Check(),"Mass unarmed filtered without rejecting entire cast");
    }
    static void Vines(BlueprintsCache cache,Psychic070Tests.Caster caster){
        var root=Asset<BlueprintAbility>(cache,"VineStrike");var variants=root.GetComponent<AbilityVariants>().Variants;Check(variants.Length==5,"Five natural category options");
        foreach(var a in variants){var buff=((ContextActionApplyBuff)a.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single()).Buff;var trigger=buff.GetComponent<AddInitiatorAttackWithWeaponTrigger>();var rt=Runtime(trigger,Fact(buff));
            foreach(WeaponCategory cat in Enum.GetValues(typeof(WeaponCategory)))foreach(bool hit in new[]{false,true}){
                var weapon=Weapon(cat);var roll=Empty<RuleAttackRoll>();Set(roll,"<Result>k__BackingField",hit?AttackResult.Hit:AttackResult.Miss);var evt=Empty<RuleAttackWithWeapon>();Set(evt,"Weapon",weapon);Set(evt,"Initiator",caster.Unit);Set(evt,"Target",caster.Unit);Set(evt,"<AttackRoll>k__BackingField",roll);
                using(rt.RequestEventContext())Check((bool)AccessTools.Method(typeof(AbstractWeaponTrigger),"IsSuitable",new[]{typeof(RuleAttackWithWeapon)}).Invoke(trigger,new object[]{evt})==(hit&&cat==trigger.Category),"Native hit/category filter "+cat);
            }
            var damage=(ContextActionDealDamage)trigger.Action.Actions[0];Check(damage.DamageType.Physical.Form==Kingmaker.Enums.Damage.PhysicalDamageForm.Piercing&&damage.Value.DiceType==DiceType.D6&&damage.IgnoreCritical&&damage.DisableSneakDamage,"Extra piercing die no crit/precision");
            var conditional=(Conditional)trigger.Action.Actions[1];var lockout=(ContextActionApplyBuff)conditional.IfTrue.Actions[0];var saved=(ContextActionSavingThrow)conditional.IfTrue.Actions[1];var entangle=(ContextActionApplyBuff)((ContextActionConditionalSaved)saved.Actions.Actions.Single()).Failed.Actions.Single();
            Params(a,20,true);Check(lockout.DurationValue.Calculate(context).Seconds==TimeSpan.FromSeconds(6)&&entangle.DurationValue.Calculate(context).Seconds==TimeSpan.FromSeconds(6),"One-round save cooldown and entangle unaffected by Extend");Check(saved.Type==SavingThrowType.Reflex&&lockout.Buff.Stacking==StackingType.Stack,"Reflex and independently owned caster cooldowns");
            using(context.GetDataScope(caster.Unit))Check(conditional.ConditionsChecker.Check(),"First hit save available");var cooldown=Fact(lockout.Buff);facts.List.Add(cooldown);Refresh();using(context.GetDataScope(caster.Unit))Check(!conditional.ConditionsChecker.Check(),"Same caster second hit save blocked");facts.List.Remove(cooldown);Refresh();
            foreach(bool pass in new[]{false,true}){savePassed=pass;delivered.Clear();using(context.GetDataScope(caster.Unit))using(SaveScope())saved.Actions.Run();Check(delivered.Count==(pass?0:1),"Entangle failure only");}
        }
    }
    static IDisposable SaveScope(){var t=typeof(MechanicsContext).Assembly.GetType("Kingmaker.UnitLogic.Mechanics.Actions.SavingThrowData",true);var scope=AccessTools.Method(typeof(Kingmaker.ElementsSystem.ContextData<>).MakeGenericType(t),"Request").Invoke(null,null);AccessTools.Method(t,"Setup").Invoke(scope,new object[]{Empty<RuleSavingThrow>()});return(IDisposable)scope;}
    static ItemEnchantment MakeEnchant(ItemEntity item,BlueprintItemEnchantment bp){var e=Empty<ItemEnchantment>();Set(e,"<Blueprint>k__BackingField",bp);Set(e,"Components",new List<EntityFactComponent>());e.RestoreOwnerLink(new EntityFactsManager(item));return e;}
    static bool Primary(ref HandSlot __result){__result=primary;return false;}static bool Secondary(ref HandSlot __result){__result=secondary;return false;}
    static bool Equipped(WeaponSlot __instance,ref ItemEntityWeapon __result){__result=ReferenceEquals(__instance,primary)?main:off;return false;}
    static bool Enchantments(ItemEntity __instance,ref List<ItemEnchantment> __result){__result=enchants[__instance];return false;}
    static bool Add(ItemEntity __instance,BlueprintItemEnchantment __0,ref ItemEnchantment __result){__result=MakeEnchant(__instance,__0);enchants[__instance].Add(__result);return false;}
    static bool RemoveEnchant(ItemEntity __instance,ItemEnchantment __0){enchants[__instance].Remove(__0);return false;}
    static bool RemoveBuff(){removed++;return false;}
    static bool Apply(ContextActionApplyBuff __instance){delivered.Add(__instance.Buff);return false;}
    static bool Difficulty(int __0,ref int __1,ref int __result){__1=0;__result=__0;return false;}
    static bool Passed(ref bool __result){__result=savePassed;return false;}
}
