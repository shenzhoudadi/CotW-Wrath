using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
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

internal static class PsychicBloodyClawsTests
{
    static int count,applied,removed,tick;static EntityFactsManager facts;static MechanicsContext context;static Buff last;static Psychic070Tests.Caster actor;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Check(bool b,string why){if(!b)throw new Exception("Bloody Claws: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string token) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicBloodyClawsHit).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"<IsActive>k__BackingField",true);Set(b,"Components",new List<EntityFactComponent>());Set(b,"m_Context",ctx);b.RestoreOwnerLink(facts);return b;}
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact b){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(b);return rt;}
    internal static int Run(BlueprintsCache cache)
    {
        actor=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(actor.Unit);Set(actor.Unit,"Facts",facts);var collection=new BuffCollection();collection.Initialize(facts);Set(actor.Unit,"<Buffs>k__BackingField",collection);
        context=actor.Cast(SpellSchool.Necromancy,0,4).Context;Set(context,"<ClickedTarget>k__BackingField",new TargetWrapper(actor.Unit));
        var spell=Asset<BlueprintAbility>(cache,"BloodyClaws");var buff=Asset<BlueprintBuff>(cache,"BloodyClawsBuff");var effect=Asset<BlueprintBuff>(cache,"BloodyClawsEffectBuff");var hit=buff.GetComponent<PsychicBloodyClawsHit>();var rt=Runtime(hit,Fact(buff,context));
        Check(spell.School==SpellSchool.Necromancy&&spell.Range==AbilityRange.Touch&&spell.CanTargetSelf&&spell.CanTargetFriends&&!spell.CanTargetEnemies&&!spell.SpellResistance,"Friendly touch necromancy without SR");
        Check(spell.GetComponents<SpellListComponent>().Select(l=>l.SpellLevel).OrderBy(x=>x).SequenceEqual(new[]{3,4}),"Ranger3 Druid4");
        foreach(var l in spell.GetComponents<SpellListComponent>())Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(s=>s==spell)==1,"Unique native spell list link");
        Check(ReferenceEquals(spell.Icon,buff.Icon)&&ReferenceEquals(buff.Icon,effect.Icon)&&!ReferenceEquals(spell.Icon,null),"Original CotW donor icon propagated");
        Check(buff.Stacking==StackingType.Replace&&effect.Stacking==StackingType.Replace&&hit.Effect.Get()==effect,"Replace source and wound states");
        var apply=(ContextActionApplyBuff)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        Check(apply.Buff==buff&&!apply.AsChild&&!apply.Permanent&&!apply.IsNotDispelable,"Independent dispellable CL minutes");
        foreach(int cl in new[]{1,5,10,20,40})foreach(bool extend in new[]{false,true}){
            context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(context,"m_Ranks",Enumerable.Repeat(spell.GetComponent<ContextRankConfig>().GetValue(context),32).ToArray());
            Check(apply.DurationValue.Calculate(context).Seconds==TimeSpan.FromMinutes(cl*(extend?2:1)),"Native CL duration/Extend");
        }
        var targetCheck=spell.GetComponent<AbilityTargetHasFact>();Check(targetCheck.IsTargetRestrictionPassed(actor.Unit,new TargetWrapper(actor.Unit)),"Living target accepted");
        foreach(string id in new[]{"734a29b693e9ec346ba2951b27987e33","fd389783027d63343b4a5634bd81645f","198fd8924dabcb5478d0f78bd453c586"}){
            var feature=Empty<UnitFact>();Set(feature,"<Blueprint>k__BackingField",cache.Load(BlueprintGuid.Parse(id)));Set(feature,"Components",new List<EntityFactComponent>());feature.RestoreOwnerLink(facts);facts.List.Add(feature);Refresh();Check(!targetCheck.IsTargetRestrictionPassed(actor.Unit,new TargetWrapper(actor.Unit)),"Undead/construct/elemental excluded separately");facts.List.Remove(feature);Refresh();
        }
        Check(effect.GetComponent<SpellDescriptorComponent>().Descriptor==SpellDescriptor.Bleed&&effect.GetComponent<AddHealTrigger>().OnHealDamage,"Native bleed descriptor and healing cleanup");
        Check(effect.GetComponent<Kingmaker.Designers.Mechanics.Facts.CombatStateTrigger>()!=null,"Native combat-end cleanup retained");
        var h=new Harmony("CotW.BloodyClaws.Managed");
        var helper=AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)});var capture=AccessTools.Method(typeof(Psychic070Tests),"CaptureDefense");var captureOwner=Harmony.GetPatchInfo(helper).Prefixes.Single(p=>p.PatchMethod==capture).owner;h.Unpatch(helper,capture);
        try{
            h.Patch(AccessTools.Method(typeof(BuffCollection),"AddBuff",new[]{typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(PsychicBloodyClawsTests),nameof(AddBuff)));
            h.Patch(AccessTools.Method(typeof(ContextActionRemoveSelf),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicBloodyClawsTests),nameof(RemoveSelf)));
            h.Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),prefix:new HarmonyMethod(typeof(PsychicBloodyClawsTests),nameof(Damage)));
            foreach(bool natural in new[]{false,true})foreach(bool unarmed in new[]{false,true})foreach(var form in new[]{PhysicalDamageForm.Bludgeoning,PhysicalDamageForm.Slashing,PhysicalDamageForm.Piercing})foreach(bool success in new[]{false,true})foreach(int actual in new[]{0,1})foreach(int cl in new[]{1,6,20})foreach(int bonus in new[]{-4,0,6}){
                facts.List.Clear();Refresh();applied=0;last=null;context.SetParams(new AbilityParams {CasterLevel=cl});var evt=Attack(natural,unarmed,form,success,actual,bonus);
                using(rt.RequestEventContext())hit.OnEventDidTrigger(evt);
                int expected=natural&&!unarmed&&form!=PhysicalDamageForm.Bludgeoning&&success&&actual>0?Math.Min(cl/2,Math.Max(1,4+bonus)):0;
                Check(applied==(expected>0?1:0),"Eligibility "+natural+"/"+unarmed+"/"+form+"/"+success+"/"+actual+" CL="+cl+" bonus="+bonus+" applied="+applied+" expected="+expected+" isHit="+evt.AttackRoll.IsHit+" physical="+(evt.MeleeDamage.DamageBundle.WeaponDamage is PhysicalDamage)+" cap="+evt.WeaponStats.WeaponDamageDice.ModifiedValue.MaxValue(0));
                if(expected>0){Check(last.Context.Params.CasterLevel==expected&&last.IsFromSpell&&last.IsNotDispelable,"Wound potency snapshotted independently with native flags");Check(context.Params.CasterLevel==cl,"Original caster context not changed");}
            }
            foreach(int cl in new[]{20,10,30}){
                context.SetParams(new AbilityParams {CasterLevel=cl});var evt=Attack(true,false,PhysicalDamageForm.Slashing,true,5,30);applied=0;
                using(rt.RequestEventContext())hit.OnEventDidTrigger(evt);
                Check(applied==(cl==10?0:1),"Weaker bleed cannot replace stronger, stronger replaces once");
            }
            Check(facts.List.OfType<Buff>().Count(b=>b.Blueprint==effect)==1&&last.Context.Params.CasterLevel==15,"Repeated hits retain only strongest wound");
            var round=effect.GetComponent<AddFactContextActions>();var deal=round.NewRound.Actions.OfType<ContextActionDealDamage>().Single();
            Set(last.Context,"m_Ranks",Enumerable.Repeat(effect.GetComponent<ContextRankConfig>().GetValue(last.Context),32).ToArray());tick=0;using(last.Context.GetDataScope(actor.Unit))deal.RunAction();Check(tick==15,"Native wound rank resolves fixed round bleed");
            removed=0;var heal=effect.GetComponent<AddHealTrigger>();var hr=Runtime(heal,last);var healRule=new RuleHealDamage(actor.Unit,actor.Unit,new DiceFormula(1,DiceType.D6),0);Set(healRule,"<Value>k__BackingField",3);
            using(hr.RequestEventContext())heal.OnEventDidTrigger(healRule);Check(removed==1,"Native healing trigger removes wound");
        }finally{h.UnpatchAll(h.Id);new Harmony(captureOwner).Patch(helper,prefix:new HarmonyMethod(capture));}
        PsychicExpansionTests.Inventory["bloodyClawsAssertions"]=count;Console.WriteLine("PASS: "+count+" Bloody Claws checks: actual attack callback, cap/eligibility/strongest wound, native context/rank, target predicates, heal cleanup. Scene buff endpoint and final round damage substituted; not gameplay.");return count;
    }

    static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
    static RuleAttackWithWeapon Attack(bool natural,bool unarmed,PhysicalDamageForm form,bool success,int actual,int bonus){
        var type=new BlueprintWeaponType {Category=natural?WeaponCategory.Claw:WeaponCategory.Longsword};Set(type,"m_IsNatural",natural);Set(type,"m_IsUnarmed",unarmed);var bp=new BlueprintItemWeapon();var reference=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(reference,type,null);Set(bp,"m_Type",reference);var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",bp);
        var stats=Empty<RuleCalculateWeaponStats>();Set(stats,"<WeaponDamageDice>k__BackingField",new ModifiableDiceFormula(new DiceFormula(1,DiceType.D4)));var mods=new ModifiableBonus();mods.Add(new Modifier(bonus,ModifierDescriptor.UntypedStackable));Set(stats,"DamageModifiers",mods);
        var physical=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1,DiceType.D4)),bonus,form);var damage=new RuleDealDamage(actor.Unit,actor.Unit,new DamageBundle(weapon,physical));Set(damage,"<ResultList>k__BackingField",new List<DamageValue>{new DamageValue(damage.DamageBundle.WeaponDamage,actual,actual,actual,1)});
        var roll=Empty<RuleAttackRoll>();Set(roll,"<Result>k__BackingField",success?AttackResult.Hit:AttackResult.Miss);var evt=Empty<RuleAttackWithWeapon>();Set(evt,"Weapon",weapon);Set(evt,"Initiator",actor.Unit);Set(evt,"Target",actor.Unit);Set(evt,"<AttackRoll>k__BackingField",roll);Set(evt,"<MeleeDamage>k__BackingField",damage);Set(evt,"<WeaponStats>k__BackingField",stats);return evt;
    }
    static bool AddBuff(BlueprintBuff __0,MechanicsContext __1,TimeSpan? __2,ref Buff __result){Check(__2==null,"Wound outlives source buff");applied++;facts.List.RemoveAll(b=>b.Blueprint==__0);var clone=__1.CloneFor(__0,actor.Descriptor,null);Check(clone.Params.CasterLevel==__1.Params.CasterLevel,"Native BuffCollection context cloning retains snapshotted potency");last=Fact(__0,clone);facts.List.Add(last);__result=last;return false;}
    static bool RemoveSelf(){removed++;return false;}
    static bool Damage(ContextActionDealDamage __instance){tick=__instance.Value.Calculate(last.Context);return false;}
}
