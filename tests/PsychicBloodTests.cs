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
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicBloodTests
{
    static int count,applied,removed,limbAdds,limbRemoves;static EntityFactsManager facts;static MechanicsContext context;static BlueprintBuff delivered;
    static ItemEntityArmor worn;static readonly Dictionary<ItemEntity,List<ItemEnchantment>> enchants=new Dictionary<ItemEntity,List<ItemEnchantment>>();
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Blood/Maw: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicBloodArmorDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static EntityFactComponent Runtime(BlueprintComponent c,EntityFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static Buff Fact(BlueprintBuff bp){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",context);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
    static void Patch(Harmony h,Type t,string method,string prefix)=>h.Patch(AccessTools.Method(t,method),prefix:new HarmonyMethod(typeof(PsychicBloodTests),prefix));
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",buffs);
        context=f.Cast(SpellSchool.Transmutation,0,2).Context;Set(context,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        var body=Empty<UnitBody>();Set(f.Descriptor,"<Body>k__BackingField",body);var slot=Empty<ArmorSlot>();Set(body,"Armor",slot);Set(slot,"<Owner>k__BackingField",f.Descriptor);
        var h=new Harmony("CotW.BloodSpells.Managed");
        try {
            Patch(h,typeof(ContextActionApplyBuff),"RunAction",nameof(Apply));Patch(h,typeof(ContextActionRemoveBuff),"RunAction",nameof(Remove));
            h.Patch(AccessTools.PropertyGetter(typeof(ArmorSlot),"MaybeArmor"),prefix:new HarmonyMethod(typeof(PsychicBloodTests),nameof(Armor)));
            h.Patch(AccessTools.PropertyGetter(typeof(ArmorSlot),"HasArmor"),prefix:new HarmonyMethod(typeof(PsychicBloodTests),nameof(HasArmor)));
            h.Patch(AccessTools.PropertyGetter(typeof(ItemEntity),"Enchantments"),prefix:new HarmonyMethod(typeof(PsychicBloodTests),nameof(Enchantments)));
            Patch(h,typeof(ItemEntity),"AddEnchantment",nameof(AddEnchant));Patch(h,typeof(ItemEntity),"RemoveEnchantment",nameof(RemoveEnchant));
            Patch(h,typeof(UnitBody),"AddAdditionalLimb",nameof(AddLimb));Patch(h,typeof(UnitBody),"RemoveAdditionalLimb",nameof(RemoveLimb));
            Blueprints(cache);BloodDamage(cache,f);ArmorLifecycle(cache,f,slot);Maw(cache,f);
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["bloodSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Blood Armor/Savage Maw checks; native tier branches, actual damage values, enhancement max rule and native buff/enchantment/limb cleanup callbacks. Scene equipment/buff boundaries replaced; not gameplay.");return count;
    }
    static void Blueprints(BlueprintsCache cache)
    {
        foreach(string token in new[]{"SavageMaw","BloodArmor"}) {
            var spell=Asset<BlueprintAbility>(cache,token);var run=spell.GetComponent<AbilityEffectRunAction>();var buff=Asset<BlueprintBuff>(cache,token+"Buff");var apply=(ContextActionApplyBuff)run.Actions.Actions.Single();
            Check(spell.Type==AbilityType.Spell&&spell.School==SpellSchool.Transmutation&&spell.ActionType==UnitCommand.CommandType.Standard,"Standard transmutation");
            Check(spell.Range==AbilityRange.Personal&&spell.CanTargetSelf&&!spell.CanTargetFriends&&!spell.CanTargetEnemies&&!spell.CanTargetPoint,"Personal targets");
            Check(!spell.SpellResistance&&run.SavingThrowType==SavingThrowType.Unknown,"No SR/save");
            Check(apply.Buff==buff&&!apply.Permanent&&!apply.AsChild&&!apply.IsNotDispelable&&apply.IsFromSpell&&buff.Stacking==StackingType.Replace,"Independent timed replaceable dispellable spell buff");
            Check(!ReferenceEquals(spell.Icon,null)&&ReferenceEquals(spell.Icon,buff.Icon),"Original CotW PNG propagated");
            var links=spell.GetComponents<SpellListComponent>().ToArray();Check(links.Length==(token=="SavageMaw"?6:3),"All original class lists included");
            Check(links.Count(l=>l.SpellLevel==1)==(token=="SavageMaw"?1:0)&&links.All(l=>l.SpellLevel==1||l.SpellLevel==2),"Ranger1, other classes2");
            foreach(var l in links)Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(x=>x==spell)==1,"Unique reciprocal spell entry");
            foreach(int cl in new[]{1,2,7,15,20,30})foreach(bool extend in new[]{false,true}) {
                context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(context,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(context),32).ToArray());
                Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromMinutes(cl*(extend?2:1)),"Native CL minutes/Extend");
            }
        }
    }
    static RuleDealDamage Damage(Psychic070Tests.Caster f,int physical,int energy,PhysicalDamageForm form,bool fake=false,int? actual=null)
    {
        var p=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1,DiceType.D6)),0,form);var e=new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Fire);
        var evt=new RuleDealDamage(f.Unit,f.Unit,new DamageBundle(p,e));Set(evt,"<ResultList>k__BackingField",new List<DamageValue>{new DamageValue(p,physical,physical,physical,1),new DamageValue(e,energy,energy,energy,1)});Set(evt,"<Result>k__BackingField",actual??physical+energy);Set(evt,"<IsFake>k__BackingField",fake);return evt;
    }
    static void BloodDamage(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintBuff>(cache,"BloodArmorBuff");var c=bp.GetComponent<PsychicBloodArmorDamage>();var runtime=Runtime(c,Fact(bp));
        foreach(var form in new[]{PhysicalDamageForm.Bludgeoning,PhysicalDamageForm.Piercing,PhysicalDamageForm.Slashing,PhysicalDamageForm.Piercing|PhysicalDamageForm.Slashing})foreach(int value in new[]{0,4,5,20})foreach(int prior in new[]{0,1,4,5}) {
            facts.List.Clear();if(prior>0)facts.List.Add(Fact(Asset<BlueprintBuff>(cache,"BloodArmorTier"+prior+"Buff")));Refresh();applied=removed=0;
            using(runtime.RequestEventContext())c.OnEventDidTrigger(Damage(f,value,12,form));
            bool grows=form!=PhysicalDamageForm.Bludgeoning&&value>=5&&prior<5;
            Check(applied==(grows?1:0)&&removed==(grows&&prior>0?1:0),"Physical threshold/one increment/cap, energy excluded");
            if(grows)Check(delivered==Asset<BlueprintBuff>(cache,"BloodArmorTier"+(prior+1)+"Buff"),"Correct next tier");
        }
        foreach(var evt in new[]{Damage(f,20,0,PhysicalDamageForm.Slashing,true),Damage(f,20,0,PhysicalDamageForm.Slashing,false,0),Damage(f,20,0,PhysicalDamageForm.Slashing,false,4)}) {applied=0;using(runtime.RequestEventContext())c.OnEventDidTrigger(evt);Check(applied==0,"Fake or final zero/below-five damage ignored");}
        facts.List.Clear();Refresh();
        // Several hits form a single progression and never add two tiers.
        for(int i=1;i<=8;i++){using(runtime.RequestEventContext())c.OnEventDidTrigger(Damage(f,8,0,PhysicalDamageForm.Piercing));Check(facts.List.OfType<Buff>().Count()==1&&facts.List.Single().Blueprint==Asset<BlueprintBuff>(cache,"BloodArmorTier"+Math.Min(5,i)+"Buff"),"Repeated actual handler calls keep one capped tier");}
        facts.List.Clear();Refresh();
    }
    static void ArmorLifecycle(BlueprintsCache cache,Psychic070Tests.Caster f,ArmorSlot slot)
    {
        var first=Empty<ItemEntityArmor>();var second=Empty<ItemEntityArmor>();enchants[first]=new List<ItemEnchantment>();enchants[second]=new List<ItemEnchantment>();
        var restriction=Asset<BlueprintAbility>(cache,"BloodArmor").GetComponent<PsychicBloodArmorRequirement>();worn=null;Check(!restriction.IsCasterRestrictionPassed(f.Unit),"Armor required before casting");worn=first;Check(restriction.IsCasterRestrictionPassed(f.Unit),"Worn armor accepted");
        for(int rank=1;rank<=5;rank++) {
            var buff=Asset<BlueprintBuff>(cache,"BloodArmorTier"+rank+"Buff");var c=buff.GetComponent<PsychicBloodArmorEnchantment>();var fact=Fact(buff);var rt=Runtime(c,fact);worn=first;
            var original=MakeEnchant(first,(BlueprintItemEnchantment)cache.Load(BlueprintGuid.Parse("a9ea95c5e02f9b7468447bc1010fe152")));enchants[first].Add(original);
            rt.TurnOn();Check(enchants[first].Count==2,"Native activation adds one enhancement");
            using(rt.RequestEventContext()){Check(c.SelectedEnchantment.GetComponent<ArmorEnhancementBonus>().EnhancementValue==rank,"Native selected enchantment correct");Check(GameHelper.GetItemEnhancementBonus(first)==Math.Max(1,rank),"Native armor enhancement max, never sum");}
            worn=second;using(rt.RequestEventContext())c.HandleEquipmentSlotUpdated(slot,first);Check(enchants[first].Count==1&&enchants[first][0]==original&&enchants[second].Count==1,"Equipment swap moves only spell enchantment");
            rt.TurnOff();Check(enchants[second].Count==0&&enchants[first].Count==1,"Suppression/off removes own enhancement, retains preexisting");
            rt.TurnOn();Check(enchants[second].Count==1,"Reactivation restores exactly once");rt.Deactivate();Check(enchants[second].Count==0,"Final deactivation removes temporary enhancement");
            enchants[first].Clear();
        }
        worn=null;
    }
    static void Maw(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var buff=Asset<BlueprintBuff>(cache,"SavageMawBuff");var bite=buff.GetComponent<AddSecondaryAttacks>();var bp=bite.Weapon.Single();Check(bp.AssetGuid.ToString()=="35dfad6517f401145af54111be04d6cf"&&((DiceFormula)Get(bp,"m_DamageDice")).Equals(new DiceFormula(1,DiceType.D4)),"Native Medium 1d4 bite, not Animal Fury 1d6");
        var rt=Runtime(bite,Fact(buff));limbAdds=limbRemoves=0;rt.TurnOn();Check(limbAdds==1&&rt.GetData<AddSecondaryAttacksData>().Added.Count==1,"Native secondary limb registered");rt.TurnOff();Check(limbRemoves==1&&rt.GetData<AddSecondaryAttacksData>().Added.Count==0,"Native secondary limb removed on off");
        var trigger=buff.GetComponent<AddInitiatorAttackWithWeaponTrigger>();Check(trigger.CriticalHit&&trigger.OnlyHit&&trigger.CheckWeaponCategory&&trigger.Category==WeaponCategory.Bite&&!trigger.ActionsOnInitiator,"Confirmed bite critical targets victim");
        var tr=Runtime(trigger,Fact(buff));Set(f.Descriptor.State.Features,"DuelingMastery",new CountableFlag());
        foreach(var category in new[]{WeaponCategory.Bite,WeaponCategory.Claw,WeaponCategory.Longsword})foreach(bool hit in new[]{false,true})foreach(bool critical in new[]{false,true})foreach(bool fortification in new[]{false,true}) {
            var wt=new BlueprintWeaponType {Category=category};var weaponBp=new BlueprintItemWeapon();var wr=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(wt);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(wr,wt,null);Set(weaponBp,"m_Type",wr);
            var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",weaponBp);var roll=Empty<RuleAttackRoll>();Set(roll,"<Result>k__BackingField",hit?AttackResult.Hit:AttackResult.Miss);Set(roll,"<IsCriticalConfirmed>k__BackingField",critical);Set(roll,"<FortificationNegatesCriticalHit>k__BackingField",fortification);
            var evt=Empty<RuleAttackWithWeapon>();Set(evt,"Weapon",weapon);Set(evt,"Initiator",f.Unit);Set(evt,"Target",f.Unit);Set(evt,"<AttackRoll>k__BackingField",roll);
            using(tr.RequestEventContext())Check((bool)AccessTools.Method(typeof(AbstractWeaponTrigger),"IsSuitable",new[]{typeof(RuleAttackWithWeapon)}).Invoke(trigger,new object[]{evt})==(category==WeaponCategory.Bite&&hit&&critical&&!fortification),"Native critical eligibility: bite/hit/confirmation/fortification");
        }
        var bleed=(ContextActionApplyBuff)trigger.Action.Actions.Single();Check(bleed.Buff.AssetGuid.ToString()=="5eb68bfe186d71a438d4f85579ce40c1"&&bleed.Permanent&&!bleed.AsChild&&bleed.IsNotDispelable,"Native heal-removable bleed outlives maw without child dependency");
        var roar=Asset<BlueprintAbility>(cache,"SavageMawRoar");Check(roar.Type==AbilityType.Extraordinary&&roar.ActionType==UnitCommand.CommandType.Swift&&!(bool)Get(roar,"m_IsFullRoundAction"),"Swift extraordinary roar");
        Check(!roar.ComponentsArray.Any(c=>c.GetType().Name.Contains("Weapon")),"No Dazzling weapon/feat requirement");Check(buff.GetComponent<AddFacts>().Facts.Single()==roar,"Roar granted by spell buff");
        var area=roar.GetComponent<AbilityTargetsAround>();Check(((Feet)Get(area,"m_Radius")).Value==30&&(TargetType)Get(area,"m_TargetType")==TargetType.Enemy,"30-foot enemy roar");
        Check(roar.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single() is Demoralize,"Native demoralize algorithm retained");
        facts.List.Add(Fact(buff));Refresh();Check(roar.GetComponent<AbilityCasterHasFacts>().IsCasterRestrictionPassed(f.Unit),"Roar requires live spell fact");removed=0;
        roar.GetComponent<AbilityExecuteActionOnCast>().OnCast((AbilityExecutionContext)context);Check(removed==1&&!facts.List.Any(x=>x.Blueprint==buff),"Native OnCast removes caster spell even before any area target");Check(!roar.GetComponent<AbilityCasterHasFacts>().IsCasterRestrictionPassed(f.Unit),"Roar cannot be reused after spell spent");
        var grant=buff.GetComponent<AddFacts>();var gr=Runtime(grant,Fact(buff));var data=gr.GetData<AddFactsData>();var granted=Empty<Kingmaker.UnitLogic.Abilities.Ability>();Set(granted,"<Blueprint>k__BackingField",roar);data.AppliedFacts.Add(granted);
        var h=new Harmony("CotW.Maw.RemoveGrantedFact");try {h.Patch(AccessTools.Method(typeof(UnitHelper),"RemoveFact",new[]{typeof(UnitEntityData),typeof(EntityFact)}),prefix:new HarmonyMethod(typeof(PsychicBloodTests),nameof(RemoveFact)));removed=0;gr.Deactivate();Check(removed==1&&data.AppliedFacts.Count==0,"Native AddFacts removes exactly granted roar on ending buff");}finally{h.UnpatchAll(h.Id);}
    }
    static bool Apply(ContextActionApplyBuff __instance){applied++;delivered=__instance.Buff;if(__instance.Buff.name.StartsWith("CotWWrathPsychicBloodArmorTier")){Check(__instance.AsChild&&__instance.Permanent&&__instance.IsNotDispelable,"Tier lifetime belongs to parent buff");facts.List.Add(Fact(__instance.Buff));Refresh();}return false;}
    static bool Remove(ContextActionRemoveBuff __instance){removed++;facts.List.RemoveAll(f=>f.Blueprint==__instance.Buff);Refresh();return false;}
    static bool Armor(ref ItemEntityArmor __result){__result=worn;return false;}
    static bool HasArmor(ref bool __result){__result=worn!=null;return false;}
    static ItemEnchantment MakeEnchant(ItemEntity item,BlueprintItemEnchantment bp){var e=Empty<ItemEnchantment>();Set(e,"<Blueprint>k__BackingField",bp);Set(e,"Components",new List<EntityFactComponent>());e.RestoreOwnerLink(new EntityFactsManager(item));foreach(var c in bp.ComponentsArray.OfType<ArmorEnhancementBonus>())((List<EntityFactComponent>)Get(e,"Components")).Add(Runtime(c,e));return e;}
    static bool Enchantments(ItemEntity __instance,ref List<ItemEnchantment> __result){__result=enchants[__instance];return false;}
    static bool AddEnchant(ItemEntity __instance,BlueprintItemEnchantment __0,ref ItemEnchantment __result){__result=MakeEnchant(__instance,__0);enchants[__instance].Add(__result);return false;}
    static bool RemoveEnchant(ItemEntity __instance,ItemEnchantment __0){enchants[__instance].Remove(__0);return false;}
    static bool AddLimb(BlueprintItemWeapon __0,bool __2,ref int __result){Check(__2&&__0.Category==WeaponCategory.Bite,"Secondary bite limb flag");limbAdds++;__result=17;return false;}
    static bool RemoveLimb(int __0){Check(__0==17,"Only own limb index removed");limbRemoves++;return false;}
    static bool RemoveFact(EntityFact __1){removed++;return false;}
}
