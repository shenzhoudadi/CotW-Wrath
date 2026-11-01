using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics;
using Newtonsoft.Json.Linq;
using CallOfTheWild.Wrath.Prototype;

internal static partial class Arcane01446Tests
{
    static int PaidOnly01450(BlueprintsCache parent,Harmony h)
    {
        int before=checks;
        foreach(string patch in new[]{"BloodIntensityRank01446Patch","ArcaneDamage01446Patch","ArcanePrepare01446Patch","BloodPiercingResistance01446Patch","BloodIntensityDamageInfo01446Patch"})h.CreateClassProcessor(T(patch)).Patch();
        h.Patch(AccessTools.Method(typeof(RuleCalculateDamage),"OnTrigger"),prefix:new HarmonyMethod(typeof(Arcane01446Tests),nameof(StopDamage)){priority=Priority.Last});
        h.Patch(AccessTools.Method(typeof(RuleSpellResistanceCheck),"OnTrigger"),prefix:new HarmonyMethod(typeof(Arcane01446Tests),nameof(StopDamage)){priority=Priority.Last});
        CastModes(parent,h);Lifecycle(parent);Composition01450(parent);
        return checks-before;
    }
    static void Composition01450(BlueprintsCache parent)
    {
        int before=checks;var c=Fresh(parent);Register(c,0);Register(c,1);
        var u=Unit(c);var target=Unit(c);var cls=Native<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var book=Book(u,cls);
        var wizard=Native<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");u.Progression.Classes.Add(new ClassData(wizard){Level=1});
        var wizardBook=new Spellbook(u.Descriptor,wizard.Spellbook);Set(wizardBook,"m_BaseLevelInternal",1);((System.Collections.Generic.Dictionary<BlueprintSpellbook,Spellbook>)Get(u.Descriptor,"m_Spellbooks")).Add(wizard.Spellbook,wizardBook);
        var fireball=Native<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");u.Stats.Charisma.BaseValue=24;u.Stats.Strength.BaseValue=18;u.Stats.Intelligence.BaseValue=24;
        foreach(string token in new[]{"BloodIntensity01446","BloodPiercing01446","AdmixtureVersatileFeature01446"})Fact(u,Asset<BlueprintFeature>(c,token));
        var buffs=new[]{"BloodIntensity01446Buff","BloodPiercing01446Buff","AdmixtureColdVersatileBuff01446"}.Select(t=>Asset<BlueprintBuff>(c,t)).ToArray();
        var modes=buffs.Select(b=>b.GetComponent<ArcaneCastMode01446>()).ToArray();var facts=buffs.Select(b=>Buff(u,b)).ToArray();var pools=modes.Select(m=>m.Resource.Get()).ToArray();
        var orders=new[]{new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}};
        var rank=fireball.GetComponents<ContextRankConfig>().Single(r=>(bool)Get(r,"m_AffectedByIntensifiedMetamagic"));
        foreach(var order in orders) {
            foreach(var pool in pools)u.Resources.Restore(pool,99);var amounts=pools.Select(p=>u.Resources.GetResourceAmount(p)).ToArray();
            var cast=Cast(u,target,book,fireball);var original=cast.Context.Params;
            foreach(int repeat in Enumerable.Range(0,2))foreach(int i in order)using(Scope(facts[i],modes[i]))modes[i].OnEventAboutToTrigger(cast);
            Check(pools.Select((p,i)=>u.Resources.GetResourceAmount(p)==amounts[i]-1).All(x=>x),"all three modes pay once in event order "+string.Join(",",order));
            Check(original.Metamagic==0&&(cast.Context.Params.Metamagic&(Metamagic)(1<<28))==0,"original params and Toppling bit28 are untouched");
            Check(rank.GetValue(cast.Context)==17,"Intensity keeps its seven-die cap with conversion and Piercing in every order");
            var energy=new EnergyDamage(new DiceFormula(4,DiceType.D6),DamageEnergyType.Fire);var calc=Damage(cast,target,energy);
            new RulePrepareDamage(calc.ParentRule).OnTrigger(new RulebookEventContext());calc.OnTrigger(new RulebookEventContext());
            Check(energy.EnergyType==DamageEnergyType.Cold&&energy.ReductionPenalty.TotalValue==7,"actual installed conversion and energy penetration preserve independent payloads");
            var sr=new RuleSpellResistanceCheck(cast.Context,target);sr.OnTrigger(new RulebookEventContext());Check(((ModifiableBonus)Get(sr,"m_AdditionalSpellPenetration")).TotalValue==7,"actual installed SR penetration works in every order");
            var child=cast.Context.CloneFor(buffs[0],u.Descriptor,u.Descriptor);
            Check(rank.GetValue(child)==17,"native child context retains paid intensity");
            var childEnergy=new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Fire);var childParent=new RuleDealDamage(u,target,new DamageBundle(childEnergy)){Reason=child};var childRule=new RuleCalculateDamage(childParent){Reason=child};new RulePrepareDamage(childParent).OnTrigger(new RulebookEventContext());childRule.OnTrigger(new RulebookEventContext());
            Check(childEnergy.EnergyType==DamageEnergyType.Cold&&childEnergy.ReductionPenalty.TotalValue==7,"native child context retains conversion and paid piercing");
            var saved=new MechanicsContext(u,u.Descriptor,fireball);saved.SetParams(cast.Context.Params);saved.RemoveSpellDescriptor(SpellDescriptor.Fire);saved.AddSpellDescriptor(SpellDescriptor.Cold);
            var serializer=Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings{ContractResolver=new Kingmaker.EntitySystem.Persistence.JsonUtility.OptInContractResolver(),Converters=Kingmaker.EntitySystem.Persistence.JsonUtility.DefaultJsonSettings.CommonConverters.ToList(),TypeNameHandling=Newtonsoft.Json.TypeNameHandling.None});
            var text=new System.IO.StringWriter();serializer.Serialize(text,saved,typeof(MechanicsContext));var restored=serializer.Deserialize<MechanicsContext>(new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(text.ToString())));
            Check(restored.Params.Metamagic==cast.Context.Params.Metamagic&&(restored.SpellDescriptor&(SpellDescriptor.Fire|SpellDescriptor.Cold))==SpellDescriptor.Cold,"native save context retains payload and converted descriptor independently");
            foreach(int i in order)modes[i].OnEventDidTrigger(cast);
            // Native SetParams explicitly clones. Compare every serialized
            // parameter value, never require identity that the engine discards.
            Check(pools.Select((p,i)=>u.Resources.GetResourceAmount(p)==amounts[i]).All(x=>x)&&Newtonsoft.Json.JsonConvert.SerializeObject(cast.Context.Params)==Newtonsoft.Json.JsonConvert.SerializeObject(original),"failed combined cast refunds all modes once and restores every original parameter value");
        }
        var only=Cast(u,target,book,fireball);only.Context.Params.Metamagic=(Metamagic)((1<<27)|(1<<28));
        Check(rank.GetValue(only.Context)==10,"stamp and Toppling without explicit Intensity payload cannot forge a paid cap");
        var negative=new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Fire);Damage(only,target,negative).OnTrigger(new RulebookEventContext());Check(negative.ReductionPenalty.TotalValue==0,"Toppling cannot forge Blood Piercing even with the mutation stamp");
        PsychicExpansionTests.Inventory["bloodlineComposition01450Contracts"]=new JObject{["passed"]=true,["eventOrders"]=6,["sameOrderDuplicateNotification"]=true,["independentPaymentAndRefund"]=true,["noTopplingCollision"]=true,["originalParamsRetained"]=true,["actualRankAndDamagePrefixes"]=true,["nativeChildContext"]=true,["nativeDescriptorSerialization"]=true,["assertions"]=checks-before};
    }
}
