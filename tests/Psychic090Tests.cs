using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

internal static class Psychic090Tests
{
    private static int count;
    private static object Get(object obj,string field)=>AccessTools.Field(obj.GetType(),field).GetValue(obj);
    private static void Set(object obj,string field,object value)=>AccessTools.Field(obj.GetType(),field).SetValue(obj,value);
    private static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    private static void Check(bool b,string text) {if(!b) throw new Exception(text);count++;}
    private static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint {
        var t=typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
        return (T)cache.Load((BlueprintGuid)AccessTools.Method(t,"Id").Invoke(null,new object[]{token}));
    }
    private static bool HasForce(BlueprintAbility spell)=>(bool)AccessTools.Method(typeof(PsychicLinkedSpellEffects),"HasForceDice").Invoke(null,new object[]{spell});
    internal static int NativeEligibility(string path)
    {
        var native=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path))["requiredNative"].Select(x=>x["blueprint"]).ToArray();int passed=0;
        foreach(var id in new[]{"4ac47ddb9fa1eaf43a1b6809980cfbd2","0a2f7c6aa81bc6548ac7780d8b70bcbc"}) {
            var row=native.Single(j=>(string)j["AssetId"]==id);
            var bp=new BlueprintAbility {ComponentsArray=((Newtonsoft.Json.Linq.JArray)row["Data"]["Components"]).Select(c=>(BlueprintComponent)PsychicExpansionTests.Read(c,typeof(BlueprintComponent))).ToArray()};
            bp.OnEnable();if(!HasForce(bp)) throw new Exception("Installed native force spell not recognized: "+id);passed++;
        }
        return passed;
    }
    internal static int Run(BlueprintsCache cache,Psychic070Tests.Caster f,Harmony harmony)
    {
        Console.WriteLine("Psychic 0.9.0: paid force/rays/summons, mastery and burst variants");
        f.Class.Level=20;
        var force=Asset<BlueprintBuff>(cache,"FocusedForceMode").GetComponent<PsychicAmplificationComponent>();
        var eye=Asset<BlueprintBuff>(cache,"MindsEyeMode").GetComponent<PsychicAmplificationComponent>();
        var armor=Asset<BlueprintBuff>(cache,"ConjuredArmorMode").GetComponent<PsychicAmplificationComponent>();
        var forceEffect=Asset<BlueprintFeature>(cache,"FocusedForceFeature").GetComponent<PsychicLinkedSpellEffects>();
        var eyeEffect=Asset<BlueprintFeature>(cache,"MindsEyeFeature").GetComponent<PsychicLinkedSpellEffects>();
        var armorEffect=Asset<BlueprintFeature>(cache,"ConjuredArmorFeature").GetComponent<PsychicLinkedSpellEffects>();
        RuleCastSpell Cast(PsychicAmplificationComponent c,int circle=2) {
            var rule=f.Cast(c==armor?SpellSchool.Conjuration:SpellSchool.Evocation,c==armor?SpellDescriptor.Summoning:SpellDescriptor.Force,circle);
            var bp=rule.Spell.Blueprint;
            if(c==force) bp.ComponentsArray=bp.ComponentsArray.Concat(new BlueprintComponent[]{new AbilityEffectRunAction {Actions=new Kingmaker.ElementsSystem.ActionList {Actions=new Kingmaker.ElementsSystem.GameAction[]{
                new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Force},Value=new ContextDiceValue {DiceType=DiceType.D4,DiceCountValue=1,BonusValue=1}} }}}}).ToArray();
            if(c==eye) bp.ComponentsArray=bp.ComponentsArray.Concat(new BlueprintComponent[]{new AbilityDeliverProjectile {NeedAttackRoll=true}}).ToArray();
            bp.OnEnable();return rule;
        }
        void Pay(PsychicAmplificationComponent c,RuleCastSpell cast) {using(Psychic070Tests.Scope(c)) {c.OnEventAboutToTrigger(cast);c.OnEventAboutToTrigger(cast);}}
        RulePrepareDamage Damage(RuleCastSpell cast,BaseDamage d) {
            var parent=Empty<RuleDealDamage>();Set(parent,"m_DamageBundle",new DamageBundle(d));parent.Reason=cast.Context;
            Set(parent,"Initiator",f.Unit);Set(parent,"Target",f.Unit);return new RulePrepareDamage(parent);
        }
        foreach(var pair in new[]{new[]{DiceType.D4,DiceType.D6},new[]{DiceType.D6,DiceType.D8},new[]{DiceType.D8,DiceType.D10},new[]{DiceType.D10,DiceType.D12},new[]{DiceType.D12,DiceType.D12},new[]{DiceType.Zero,DiceType.Zero}}) {
            f.Amount=9;var cast=Cast(force);Pay(force,cast);cast.SetSuccess(true);force.OnEventDidTrigger(cast);
            Check(f.Amount==8,"One force charge per cast regardless of duplicated callbacks");
            for(int projectile=0;projectile<3;projectile++) {
                var damage=new ForceDamage(new ModifiableDiceFormula(new DiceFormula(4,pair[0])),3);var rule=Damage(cast,damage);
                forceEffect.OnEventAboutToTrigger(rule);forceEffect.OnEventAboutToTrigger(rule);
                Check(damage.Dice.ModifiedValue.Dice==pair[1] && damage.Dice.ModifiedValue.Rolls==(pair[0]==DiceType.Zero?0:4) && damage.Bonus==3,"Each paid projectile upgrades dice once: "+pair[0]+" => "+damage.Dice.ModifiedValue+" / "+damage.Bonus);
            }
        }
        var ineligible=Cast(force);var action=ineligible.Spell.Blueprint.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionDealDamage>().Single();
        foreach(var die in new[]{DiceType.Zero,DiceType.D12}) {action.Value.DiceType=die;f.Amount=8;Pay(force,ineligible);Check(f.Amount==8 && !HasForce(ineligible.Spell.Blueprint),"No charge for fixed damage or d12-only force spells");}
        action.Value.DiceType=DiceType.D6;action.DamageType.Type=DamageType.Direct;
        Check(!HasForce(ineligible.Spell.Blueprint),"Mind Thrust/direct psychic damage is not force damage");
        var paidForce=Cast(force);Pay(force,paidForce);
        var fire=new EnergyDamage(new DiceFormula(2,DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Fire);
        forceEffect.OnEventAboutToTrigger(Damage(paidForce,fire));Check(fire.Dice.ModifiedValue.Dice==DiceType.D6,"Non-force damage in a paid cast is unchanged");
        var other=Cast(force);var unchanged=new ForceDamage(new ModifiableDiceFormula(new DiceFormula(2,DiceType.D6)),0);
        forceEffect.OnEventAboutToTrigger(Damage(other,unchanged));Check(unchanged.Dice.ModifiedValue.Dice==DiceType.D6,"Next unamplified cast cannot inherit prior reservation");
        foreach(var c in new[]{force,eye,armor}) {
            for(int branch=0;branch<5;branch++) {
                f.Amount=branch==0?0:9;var cast=Cast(c,branch==1?0:2);
                if(branch==2) cast.ForceFail=true;if(branch==3) cast.IsDuplicateSpellApplied=true;if(branch==4) cast.Context.IsDuplicateSpellApplied=true;
                Pay(c,cast);Check(f.Amount==(branch==0?0:9),"New amplification ignores empty pool/cantrip/forced failure/duplicates: "+c.Kind+branch);
            }
            f.Amount=8;var failed=Cast(c);Pay(c,failed);c.OnEventDidTrigger(failed);c.OnEventDidTrigger(failed);
            Check(f.Amount==8,"Each new amplification refunds failed cast exactly once: "+c.Kind);
            var runtime=typeof(PsychicAmplificationComponent).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAmplificationRuntime");
            var args=new object[]{failed.Context,c.Kind,null};Check(!(bool)AccessTools.Method(runtime,"TryReservation").Invoke(null,args),"Failed cast removes its transient reservation");
        }
        RuleAttackRoll Attack(RuleCastSpell cast,AttackType type,bool weapon=false) {
            var rule=Empty<RuleAttackRoll>();Set(rule,"Initiator",f.Unit);Set(rule,"Target",f.Unit);rule.Reason=cast.Context;rule.AttackType=type;
            if(weapon) rule.RuleAttackWithWeapon=Empty<RuleAttackWithWeapon>();return rule;
        }
        f.Amount=8;var rays=Cast(eye);Pay(eye,rays);rays.SetSuccess(true);eye.OnEventDidTrigger(rays);
        Check(f.Amount==6,"Mind's Eye charges two points once for the whole spell");
        var attackStat=new ModifiableValue(f.Unit.Stats,StatType.AdditionalAttackBonus);Set(f.Unit.Stats,"AdditionalAttackBonus",attackStat);
        var existingInsight=attackStat.AddModifier(2,ModifierDescriptor.Insight);
        foreach(var type in new[]{AttackType.RangedTouch,AttackType.Ranged,AttackType.Touch,AttackType.Melee}) {
            var attack=Attack(rays,type);eyeEffect.OnEventAboutToTrigger(attack);eyeEffect.OnEventAboutToTrigger(attack);
            bool ranged=type==AttackType.RangedTouch || type==AttackType.Ranged;
            Check(attackStat.ModifiedValue==(ranged?4:2),"Native attack stat uses larger insight instead of stacking; only ranged spells qualify");
            attack.OnDidTrigger();Check(attackStat.ModifiedValue==2,"Native rule cleanup removes transient attack bonus immediately");
        }
        var stronger=attackStat.AddModifier(6,ModifierDescriptor.Insight);var strongRay=Attack(rays,AttackType.RangedTouch);eyeEffect.OnEventAboutToTrigger(strongRay);
        Check(attackStat.ModifiedValue==6,"Higher existing insight is preserved without an additional four");strongRay.OnDidTrigger();attackStat.RemoveModifier(stronger);
        var regular=Attack(rays,AttackType.Ranged,true);eyeEffect.OnEventAboutToTrigger(regular);Check(attackStat.ModifiedValue==2,"Regular weapon attacks excluded even if carrying a paid spell context");
        var fresh=Attack(Cast(eye),AttackType.RangedTouch);eyeEffect.OnEventAboutToTrigger(fresh);Check(attackStat.ModifiedValue==2,"No ray bonus leaks into a later cast");
        attackStat.RemoveModifier(existingInsight);
        var armorBuff=Asset<BlueprintBuff>(cache,"ConjuredArmorEffect");var rank=armorBuff.GetComponent<ContextRankConfig>();
        foreach(var level in new[]{1,7,8,14,15,20}) {
            f.Class.Level=level;f.Amount=8;var cast=Cast(armor);cast.Context.Params.CasterLevel=level+2;Pay(armor,cast);cast.SetSuccess(true);armor.OnEventDidTrigger(cast);
            Check(rank.GetValue(cast.Context)==(level<8?2:level<15?3:4),"Native Conjured Armor rank uses Psychic class level at threshold "+level);
            for(int summon=0;summon<3;summon++) {
                var evt=Empty<RuleSummonUnit>();Set(evt,"Initiator",f.Unit);Set(evt,"<SummonedUnit>k__BackingField",f.Unit);evt.Context=cast.Context;
                int previous=(int)AccessTools.Field(typeof(Psychic070Tests),"defenseCalls").GetValue(null);
                armorEffect.OnEventDidTrigger(evt);armorEffect.OnEventDidTrigger(evt);
                Check((int)AccessTools.Field(typeof(Psychic070Tests),"defenseCalls").GetValue(null)==previous+1,"Each summoned creature gets armor once");
                Check(ReferenceEquals(AccessTools.Field(typeof(Psychic070Tests),"defenseBlueprint").GetValue(null),armorBuff) &&
                    (TimeSpan?)AccessTools.Field(typeof(Psychic070Tests),"defenseDuration").GetValue(null)==TimeSpan.FromSeconds(6*(level+2)),"Summon armor lasts one round per caster level, independent of class scaling");
            }
            Check(f.Amount==7,"Multiple summons cost just one point");
        }
        f.Class.Level=20;
        TestMastery(cache);TestBursts(cache);
        PsychicExpansionTests.Inventory["psychic090Assertions"]=count;
        Console.WriteLine("PASS: "+count+" 0.9.0 assertions (managed rule objects and explicit live-buff boundary).");return count;
    }
    private static void TestMastery(BlueprintsCache cache)
    {
        var progression=(BlueprintProgression)cache.Load(BlueprintGuid.Parse("b14115fd5dd15c59a6075b10ef73f4ee"));
        var choice=Asset<BlueprintFeatureSelection>(cache,"PhrenicAmplificationSelection");var mastery=Asset<BlueprintFeature>(cache,"PhrenicMasteryFeature");
        var level20=progression.LevelEntries.Single(e=>e.Level==20);
        Check(level20.Features.Count(f=>f==choice)==2 && level20.Features.Count(f=>f==mastery)==1,"Capstone grants two separate choices and one resource feature");
        var state=Empty<LevelUpState>();Set(state,"Selections",new List<FeatureSelectionState>());
        foreach(var feature in level20.Features.OfType<BlueprintFeatureSelection>()) state.AddSelection(null,progression,feature,20);
        Check(state.Selections.Count==2 && !ReferenceEquals(state.Selections[0],state.Selections[1]),"Native level-up state retains two independent level-20 selection slots");
        var increases=mastery.GetComponents<IncreaseResourceAmount>().ToArray();
        var fact=Empty<Feature>();Set(fact,"<Rank>k__BackingField",1);Set(fact,"<IsActive>k__BackingField",true);
        var runtimes=increases.Select(c=>{var runtime=c.CreateRuntimeFactComponent();runtime.Setup(c);runtime.RestoreOwnerLink(fact);return runtime;}).ToArray();
        foreach(var id in new[]{"ddfc9f9a2cbd51b48cbd214dbd75c8fb","0795daa1b7d259c3b5052c5c704cc031"}) {
            var pool=(BlueprintAbilityResource)cache.Load(BlueprintGuid.Parse(id));int max=15;
            for(int i=0;i<increases.Length;i++) using(runtimes[i].RequestEventContext()) increases[i].CalculateMaxResourceAmount(pool,ref max);
            Check(max==21,"Native capstone modifier adds six to each supported discipline pool, without double stacking");
        }
        int unrelated=7;for(int i=0;i<increases.Length;i++) using(runtimes[i].RequestEventContext()) increases[i].CalculateMaxResourceAmount(new BlueprintAbilityResource(),ref unrelated);
        Check(unrelated==7,"Capstone does not increase unrelated resources");
    }
    private static void TestBursts(BlueprintsCache cache)
    {
        foreach(var token in new[]{"BurstOfAdrenaline","BurstOfInsight"}) {
            var spell=Asset<BlueprintAbility>(cache,token);var variants=spell.GetComponent<AbilityVariants>().Variants;
            Check(variants.Length==3 && spell.GetComponent<SpellListComponent>().SpellLevel==1,"Each burst has three choices and a single first-circle learnable entry");
            foreach(var v in variants) {
                Check(v.Parent==spell && v.School==SpellSchool.Transmutation && v.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift && v.Range==AbilityRange.Personal,"Variant retains parent/circle/school/self/swift metadata");
                Check(v.AvailableMetamagic==Metamagic.Heighten && !v.SpellResistance,"Burst keeps CotW metamagic policy and no resistance");
                var apply=v.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();var buff=apply.Buff;
                var stat=buff.GetComponent<AddStatBonus>();Check(stat.Value==8 && stat.Descriptor==ModifierDescriptor.Enhancement,"Burst grants a typed +8 enhancement bonus");
                var ctx=Empty<MechanicsContext>();ctx.SetParams(new AbilityParams());
                Check(!apply.Permanent && apply.DurationValue.Calculate(ctx).Value==1,"Native duration is exactly one round");
                var after=buff.GetComponent<AddFactContextActions>().Deactivated.Actions.OfType<ContextActionApplyBuff>().Single();
                Check(after.ToCaster && after.IsNotDispelable && !after.IsFromSpell && after.DurationValue.Calculate(ctx).Value==1 &&
                    after.Buff.AssetGuid.ToString()==(token=="BurstOfInsight"?"9934fedff1b14994ea90205d189c8759":"e6f2fc5d73d88064583cb828801212f4"),"Removal triggers exact native aftereffect for one non-dispellable round");
                Check(buff.FxOnStart!=null && !string.IsNullOrEmpty(buff.FxOnStart.AssetId) && buff.FxOnRemove!=null && !ReferenceEquals(buff.Icon,null),"Attribute buff includes native start visual, valid removal link and icon");
            }
        }
    }
}
