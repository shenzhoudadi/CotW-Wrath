using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
internal static class PsychicBladeTutorTests
{
    static int count;static EntityFactsManager facts;static MechanicsContext context;
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Check(bool b,string why){if(!b)throw new Exception("Blade Tutor: "+why);count++;}
    static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint=>(T)cache.Load((BlueprintGuid)AccessTools.Method(typeof(CallOfTheWild.Wrath.Prototype.PsychicBloodArmorDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static Buff Fact(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());Set(b,"m_Context",context);Set(b,"<Rank>k__BackingField",1);Set(b,"<IsActive>k__BackingField",true);b.RestoreOwnerLink(facts);return b;}
    static ItemEntityWeapon Weapon(AttackType attack){var type=new BlueprintWeaponType();Set(type,"m_AttackType",attack);var bp=new BlueprintItemWeapon();var reference=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(reference,type,null);Set(bp,"m_Type",reference);var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",bp);return weapon;}
    static RuleCalculateAttackBonusWithoutTarget Rule(Psychic070Tests.Caster f,ItemEntityWeapon weapon){var ws=Empty<RuleCalculateWeaponStats>();Set(ws,"Weapon",weapon);Set(ws,"m_Triggered",true);Set(ws,"<Enhancement>k__BackingField",new EnhancementValue());return new RuleCalculateAttackBonusWithoutTarget(f.Unit,ws,5){AttackBonusStat=StatType.Wisdom};}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);context=f.Cast(SpellSchool.Conjuration,0,2).Context;Set(context,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        var spell=Asset<BlueprintAbility>(cache,"BladeTutor");var bp=Asset<BlueprintBuff>(cache,"BladeTutorBuff");var buff=Fact(bp);facts.List.Add(buff);
        var stat=new ModifiableValue(f.Unit.Stats,StatType.AdditionalAttackBonus);Set(f.Unit.Stats,"AdditionalAttackBonus",stat);f.Unit.Stats.AllStats.Add(stat);Set(f.Unit.Stats,"BaseAttackBonus",new ModifiableValue(f.Unit.Stats,StatType.BaseAttackBonus){BaseValue=10});
        Check(spell.Range==AbilityRange.Personal&&spell.NeedEquipWeapons&&!spell.SpellResistance&&spell.School==SpellSchool.Conjuration,"Personal weapon-equipped conjuration, no SR");
        Check(spell.GetComponents<SpellListComponent>().Select(l=>l.SpellLevel).OrderBy(x=>x).SequenceEqual(new[]{1,2,2}),"Magus1 Wizard2 Paladin2");
        foreach(var l in spell.GetComponents<SpellListComponent>())Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(s=>s==spell)==1,"Learned root once per list");
        Check(!spell.GetComponents<SpellListComponent>().Any(l=>l.SpellList==f.Book.Blueprint.SpellList),"No unsupported Psychic list entry");
        Check(ReferenceEquals(spell.Icon,bp.Icon)&&bp.Stacking==StackingType.Replace,"Original icon shared and no stacking");
        var apply=spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
        Check(apply.Buff==bp&&!apply.AsChild&&!apply.Permanent&&!apply.IsNotDispelable,"Independent dispellable timed state");
        var sourceIds=new[]{"5898bcf75a0942449a5dc16adc97b279","bfe8300529214c7ca4ae0024f834f085","8af258b1dd322874ba6047b0c24660c7","b93ed538543945378613b228dfdd4578","e81cd772a7311554090e413ea28ceea1","6ffd93355fb3bcf4592a5d976b1d32a9","6948b379c0562714d9f6d58ccbfa8faa"};
        var sources=sourceIds.Select(id=>Fact(new BlueprintBuff {AssetGuid=BlueprintGuid.Parse(id)})).ToArray();var hostile=Fact(new BlueprintBuff {AssetGuid=BlueprintGuid.Parse("b30e4126c1614bc68e520df846ae2d3b")});
        var prefix=AccessTools.Method(typeof(CallOfTheWild.Wrath.Prototype.PsychicBloodArmorDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicBladeTutor+AttackPatch"),"Prefix");
        var harmony=new Harmony("CotW.BladeTutor.NativeCalculation");var target=AccessTools.Method(typeof(RuleCalculateAttackBonusWithoutTarget),"OnTrigger");harmony.Patch(target,prefix:new HarmonyMethod(prefix));
        try{
            foreach(int cl in new[]{1,4,5,9,10,19,20,30}){
                context.SetParams(new AbilityParams {CasterLevel=cl});Set(context,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(context),32).ToArray());
                Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromMinutes(cl),"Native minute duration CL"+cl);
                context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=Metamagic.Extend});Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromMinutes(cl*2),"Extend");
                foreach(var source in sources)foreach(int penalty in new[]{0,1,2,6})foreach(bool statPenalty in new[]{false,true})foreach(var attack in new[]{AttackType.Melee,AttackType.Ranged,AttackType.RangedTouch}){
                    var evt=Rule(f,Weapon(attack));var modifier=statPenalty?stat.AddModifier(-penalty,source,ModifierDescriptor.UntypedStackable):null;
                    if(!statPenalty)evt.AddModifier(-penalty,source,ModifierDescriptor.UntypedStackable);
                    evt.AddModifier(-4,hostile,ModifierDescriptor.UntypedStackable);
                    evt.OnTrigger(null);int expected=attack==AttackType.Melee?Math.Min(penalty,1+cl/5):0;
                    Check(evt.AllBonuses.Where(m=>m.Fact==buff).Sum(m=>m.Value)==expected,"Actual patched native attack calculation caps eligible penalty");
                    Check(evt.Result==10-5+10-4-penalty+expected,"Final AB preserves iterative and hostile penalties");
                    prefix.Invoke(null,new object[]{evt});Check(evt.AllBonuses.Count(m=>m.Fact==buff)==(expected>0?1:0),"No duplicate compensation");
                    if(modifier!=null)stat.RemoveModifier(modifier);
                }
            }
            context.SetParams(new AbilityParams {CasterLevel=20});
            var mixed=Rule(f,Weapon(AttackType.Melee));mixed.AddModifier(-2,sources[1],ModifierDescriptor.UntypedStackable);mixed.AddModifier(-3,sources[6],ModifierDescriptor.UntypedStackable);var defensive=stat.AddModifier(-4,sources[5],ModifierDescriptor.UntypedStackable);mixed.OnTrigger(null);
            Check(mixed.AllBonuses.Single(m=>m.Fact==buff).Value==5,"Combined penalties receive single total budget");stat.RemoveModifier(defensive);
            var offset=Rule(f,Weapon(AttackType.Melee));offset.AddModifier(-3,sources[1],ModifierDescriptor.UntypedStackable);offset.AddModifier(3,sources[1],ModifierDescriptor.UntypedStackable);offset.OnTrigger(null);Check(!offset.AllBonuses.Any(m=>m.Fact==buff),"Same-source penalty cancellation gives no excess bonus");
            var focused=Fact(new BlueprintBuff {AssetGuid=BlueprintGuid.Parse("c632e516f383442489abb8ca810d639a")});
            foreach(bool withPower in new[]{false,true})foreach(bool withDefense in new[]{false,true}){
                var evt=Rule(f,Weapon(AttackType.Melee));if(withPower)evt.AddModifier(-3,sources[1],ModifierDescriptor.UntypedStackable);
                if(withDefense)evt.AddModifier(-4,sources[5],ModifierDescriptor.UntypedStackable);
                var refund=stat.AddModifier(3,focused,ModifierDescriptor.None);evt.OnTrigger(null);
                Check(evt.AllBonuses.Where(m=>m.Fact==buff).Sum(m=>m.Value)==(withDefense?4:0),"Native Focused Strike refund only cancels Power Attack, no overcompensation");stat.RemoveModifier(refund);
            }
            Set(buff,"<IsActive>k__BackingField",false);var inactive=Rule(f,Weapon(AttackType.Melee));inactive.AddModifier(-4,sources[1],ModifierDescriptor.UntypedStackable);inactive.OnTrigger(null);Check(!inactive.AllBonuses.Any(m=>m.Fact==buff),"Inactive buff excluded");
            facts.List.Remove(buff);var expired=Rule(f,Weapon(AttackType.Melee));expired.AddModifier(-4,sources[1],ModifierDescriptor.UntypedStackable);expired.OnTrigger(null);Check(!expired.AllBonuses.Any(m=>m.Fact==buff),"Expired/removed buff cannot contribute");
        }finally{harmony.UnpatchAll(harmony.Id);}
        PsychicExpansionTests.Inventory["bladeTutorAssertions"]=count;Console.WriteLine("PASS: "+count+" Blade Tutor checks: real patched native AB calculation, source/CL/range boundaries, no double compensation, duration/Extend and lists; constructed facts/weapon stats, not gameplay.");return count;
    }
}
