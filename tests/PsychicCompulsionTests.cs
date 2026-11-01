using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
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
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicCompulsionTests
{
    static int count,saves,applies,shieldPenalty;static bool passed,hasShield;static EntityFactsManager facts;static MechanicsContext context;static RuleSavingThrow saving;
    static ContextActionApplyBuff applied;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception("Portent/Dance: "+why);count++;}
    static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicPortentDamage).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
    static EntityFactComponent Runtime(BlueprintComponent c,EntityFact f){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(f);return r;}
    static Buff Fact(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());Set(b,"m_Context",context);Set(b,"<Rank>k__BackingField",1);Set(b,"<IsActive>k__BackingField",true);b.RestoreOwnerLink(facts);return b;}
    static HarmonyMethod Hook(string n)=>new HarmonyMethod(typeof(PsychicCompulsionTests),n){priority=Priority.First};
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);Set(f.Unit,"PreventDirectControl",new CountableFlag());
        context=f.Cast(SpellSchool.Enchantment,SpellDescriptor.MindAffecting|SpellDescriptor.Compulsion,4).Context;Set(context,"<ClickedTarget>k__BackingField",new TargetWrapper(f.Unit));
        var h=new Harmony("CotW.Compulsion.Managed");
        try {
            h.Patch(AccessTools.Method(typeof(RuleSavingThrow),"OnTrigger"),prefix:Hook(nameof(Trigger)));
            h.Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),prefix:Hook(nameof(Passed)));
            h.Patch(AccessTools.PropertyGetter(typeof(MechanicsContext),"SavingThrow"),prefix:Hook(nameof(Saving)));
            h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),prefix:Hook(nameof(Apply)));
            h.Patch(AccessTools.Method(typeof(RuleCalculateAC),"AddModifier",new[]{typeof(int),typeof(EntityFact),typeof(ModifierDescriptor)}),prefix:Hook(nameof(AC)));
            h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Items.UnitBody),"SecondaryHand"),prefix:Hook(nameof(Hand)));
            h.Patch(AccessTools.PropertyGetter(typeof(HandSlot),"HasShield"),prefix:Hook(nameof(Shield)));
            Blueprints(cache,f);Portent(cache,f);Control(cache,f);ShieldRules(cache,f);
        }finally{h.UnpatchAll(h.Id);}
        PsychicExpansionTests.Inventory["compulsionSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Portent/Dance checks; native resource-free events, confusion control retention and event lifecycle, duration calculations and damage modifiers. Save results and final scene application seeded; no gameplay.");return count;
    }
    static void Blueprints(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var p=Asset<BlueprintAbility>(cache,"DebilitatingPortent");var cast=Asset<BlueprintAbility>(cache,"IrresistibleDance");var touch=Asset<BlueprintAbility>(cache,"IrresistibleDanceTouch");
        foreach(var spell in new[]{p,cast,touch}){
            Check(spell.Type==AbilityType.Spell&&spell.School==SpellSchool.Enchantment&&spell.SpellResistance,"Enchantment spell/SR");
            Check(spell.SpellDescriptor==(SpellDescriptor.MindAffecting|SpellDescriptor.Compulsion),"Native mind/compulsion immunity routes");
            Check(spell.CanTargetEnemies&&!spell.CanTargetSelf&&!spell.CanTargetFriends&&!spell.CanTargetPoint,"Single harmful target");
            Check(spell.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,"Standard action");
        }
        Lists(p,new[]{4,4});Lists(cast,new[]{6,8,8});Check(touch.GetComponent<SpellListComponent>()==null,"Touch delivery not learnable twice");
        Check(cast.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility==touch&&touch.GetComponent<AbilityDeliverTouch>()?.TouchWeapon?.AssetGuid.ToString()=="bb337517547de1a4189518d404ec49d4","Native held-charge pair");
        Check(touch.Range==AbilityRange.Touch&&p.Range==AbilityRange.Medium,"Original ranges");
        var donor=(BlueprintAbility)cache.Load(BlueprintGuid.Parse("3c17035ec4717674cae2e841a190e757"));
        var checks=donor.ComponentsArray.Where(c=>c is AbilityTargetHasFact || c is AbilityTargetHasNoFactUnless).ToArray();
        Check(checks.Length>0&&touch.ComponentsArray.Count(c=>c is AbilityTargetHasFact||c is AbilityTargetHasNoFactUnless)==checks.Length,"Native Dominate creature filters retained");
        foreach(var c in checks)Check(!touch.ComponentsArray.Contains(c),"Target checkers cloned, native owners untouched");
        var pb=Asset<BlueprintBuff>(cache,"DebilitatingPortentBuff");var db=Asset<BlueprintBuff>(cache,"IrresistibleDanceBuff");
        Check(ReferenceEquals(p.Icon,pb.Icon)&&ReferenceEquals(cast.Icon,db.Icon)&&ReferenceEquals(touch.Icon,db.Icon),"Spell/status original icons consistent");
        foreach(var b in new[]{pb,db})Check(b.Stacking==StackingType.Replace&&b.FxOnRemove!=null,"Nonstacking, valid native cleanup link");
        var run=p.GetComponent<AbilityEffectRunAction>();Check(run.SavingThrowType==SavingThrowType.Unknown&&run.Actions.Actions.Length==1,"Portent no initial save");
        var pa=(ContextActionApplyBuff)run.Actions.Actions.Single();Check(!pa.AsChild&&!pa.Permanent&&!pa.IsNotDispelable&&pa.IsFromSpell,"Portent timed dispellable buff");
        foreach(int cl in new[]{1,2,7,16,20,35})foreach(bool extend in new[]{false,true}){
            context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extend?Metamagic.Extend:0});Set(context,"m_Ranks",Enumerable.Repeat(p.GetComponent<ContextRankConfig>().GetValue(context),32).ToArray());
            Check(pa.DurationValue.Calculate(context).Seconds==TimeSpan.FromSeconds(cl*6*(extend?2:1)),"Portent native CL/Extend");
        }
        var dr=touch.GetComponent<AbilityEffectRunAction>();Check(dr.SavingThrowType==SavingThrowType.Will,"Dance Will partial at touch");
        var branches=(ContextActionConditionalSaved)dr.Actions.Actions.Single();
        foreach(bool save in new[]{false,true}){
            passed=save;saving=Empty<RuleSavingThrow>();applies=0;using(context.GetDataScope(f.Unit))branches.RunAction();
            Check(applies==1&&applied.Buff==db,"Both save outcomes apply dancing");Check(!applied.AsChild&&!applied.Permanent&&applied.IsFromSpell&&!applied.IsNotDispelable,"Dance native dispel/expiry");
            Check(applied.DurationValue.BonusValue.Value==1&&applied.DurationValue.DiceType==(save?DiceType.Zero:DiceType.D4),"Success one round; failure 1d4+1");
            foreach(bool extend in new[]{false,true}){
                context.SetParams(new AbilityParams {CasterLevel=20,Metamagic=Metamagic.Maximize|(extend?Metamagic.Extend:0)});
                Check(applied.DurationValue.Calculate(context).Seconds==TimeSpan.FromSeconds((save?1:5)*6*(extend?2:1)),"Native Maximize and Extend duration");
            }
        }
        Check(db.GetComponent<AddCondition>().Condition==UnitCondition.Confusion,"No inappropriate stun/daze immunity added");
        Check(db.GetComponents<AddStatBonus>().Single(c=>c.Stat==StatType.AC).Value==-4&&db.GetComponents<AddStatBonus>().Single(c=>c.Stat==StatType.SaveReflex).Value==-10,"AC/reflex penalties");
        Check(db.GetComponent<AddFactContextActions>().NewRound.Actions.Single() is ContextActionProvokeAttackOfOpportunity,"Native per-round AoO action");
    }
    static void Stat(Psychic070Tests.Caster f,StatType type,int value){var s=new ModifiableValueAttributeStat(f.Unit.Stats,type);Set(s,"Type",type);Set(s,"m_BaseValue",value);Set(s,"<ModifiedValue>k__BackingField",value);Set(f.Unit.Stats,type.ToString(),s);}
    static void Portent(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintBuff>(cache,"DebilitatingPortentBuff");var c=bp.GetComponent<PsychicPortentDamage>();var fact=Fact(bp);var runtime=Runtime(c,fact);
        // Real save constructor needs the owner's saving throw stat, while
        // Rulebook delivery/result is replaced with a captured seeded save.
        Set(f.Unit.Stats,"SaveWill",new ModifiableValueSavingThrow(f.Unit.Stats,f.Unit.Stats.Wisdom,StatType.SaveWill));
        foreach(int cl in new[]{1,2,9,20})foreach(var values in new[]{new[]{8,8,8},new[]{22,10,12},new[]{10,24,18},new[]{14,18,26}})foreach(bool save in new[]{false,true}){
            Stat(f,StatType.Intelligence,values[0]);Stat(f,StatType.Wisdom,values[1]);Stat(f,StatType.Charisma,values[2]);context.SetParams(new AbilityParams {CasterLevel=cl});
            var physical=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(1,DiceType.D6)),0,PhysicalDamageForm.Slashing);var fire=new EnergyDamage(new DiceFormula(1,DiceType.D6),0,DamageEnergyType.Fire);fire.Durability=0.5f;
            var deal=new RuleDealDamage(f.Unit,f.Unit,new DamageBundle(physical,fire));var damage=Empty<RuleCalculateDamage>();Set(damage,"ParentRule",deal);Set(damage,"Initiator",f.Unit);
            passed=save;saves=0;using(runtime.RequestEventContext())c.OnEventAboutToTrigger(damage);
            Check(saves==1&&saving.Type==SavingThrowType.Will&&saving.DifficultyClassBase==10+cl/2+(values.Max()-10)/2,"One native save per damage bundle using best mental stat and half CL");
            Check(saving.Reason.Fact==fact,"Save retains originating buff context/immunity source");
            Check(physical.Durability==(save?1f:0.5f)&&fire.Durability==(save?0.5f:0.25f),"Mixed damage halved without replacing prior reductions");
            Set(damage,"Initiator",new Psychic070Tests.Caster(cache).Unit);saves=0;using(runtime.RequestEventContext())c.OnEventAboutToTrigger(damage);Check(saves==0,"Other initiators unaffected");
        }
    }
    static void Control(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintBuff>(cache,"IrresistibleDanceBuff");var runtime=Runtime(bp.GetComponent<PsychicDanceControl>(),Fact(bp));var other=new Psychic070Tests.Caster(cache);
        try {
            runtime.TurnOn();var part=f.Unit.Get<UnitPartConfusion>();Check(part.State==ConfusionState.DoNothing&&f.Unit.PreventDirectControl,"Activation retains native control in place");
            Check(runtime.IsSubscribedOnEventBus,"Native global event subscription");
            foreach(ConfusionState state in Enum.GetValues(typeof(ConfusionState))) {
                part.State=state;EventBus.RaiseEvent<IConfusionRollResultHandler>(h=>h.HandleConfusionRollResult(other.Unit,state));Check(part.State==state,"Foreign confusion untouched");
                EventBus.RaiseEvent<IConfusionRollResultHandler>(h=>h.HandleConfusionRollResult(f.Unit,state));Check(part.State==ConfusionState.DoNothing&&f.Unit.PreventDirectControl,"All native confusion results forced to in-place action");
            }
            runtime.TurnOff();Check(!runtime.IsSubscribedOnEventBus&&!f.Unit.PreventDirectControl&&part.RoundStartTime==TimeSpan.Zero,"Native unsubscription/control release/reset on removal or suppression");
            part.State=ConfusionState.ActNormally;EventBus.RaiseEvent<IConfusionRollResultHandler>(h=>h.HandleConfusionRollResult(f.Unit,ConfusionState.ActNormally));Check(part.State==ConfusionState.ActNormally,"No stale handler after removal");
            runtime.TurnOn();Check(f.Unit.PreventDirectControl,"Reactivation restores dance");runtime.TurnOff();Check(!f.Unit.PreventDirectControl,"Repeated activation has no retained-control leak");
        }finally{runtime.TurnOff();}
    }
    static void ShieldRules(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var body=Empty<Kingmaker.Items.UnitBody>();Set(f.Descriptor,"<Body>k__BackingField",body);
        var ac=Empty<ModifiableValueArmorClass>();Set(f.Unit.Stats,"AC",ac);
        var mods=new[]{ModifierDescriptor.Shield,ModifierDescriptor.ShieldEnhancement,ModifierDescriptor.ShieldFocus,ModifierDescriptor.Armor,ModifierDescriptor.Dodge};
        Set(ac,"ModifierList",mods.Select((m,i)=>new ModifiableValue.Modifier {ModDescriptor=m,ModValue=i+1}).ToDictionary(m=>m.ModDescriptor,m=>new List<ModifiableValue.Modifier>{m}));
        ((List<ModifiableValue.Modifier>)ac.GetModifiers(ModifierDescriptor.Shield)).Add(new ModifiableValue.Modifier {ModDescriptor=ModifierDescriptor.Shield,ModValue=1});
        var b=Asset<BlueprintBuff>(cache,"IrresistibleDanceBuff");var c=b.GetComponent<PsychicDanceShield>();var fact=Fact(b);var runtime=Runtime(c,fact);
        foreach(bool shield in new[]{false,true})foreach(bool brilliant in new[]{false,true})foreach(AttackType type in Enum.GetValues(typeof(AttackType))){
            hasShield=shield;var evt=Empty<RuleCalculateAC>();Set(evt,"Target",f.Unit);Set(evt,"AttackType",type);evt.BrilliantEnergy=brilliant?fact:null;shieldPenalty=0;
            using(runtime.RequestEventContext())c.OnEventAboutToTrigger(evt);
            Check(shieldPenalty==(shield&&!brilliant&&!type.IsTouch()?-6:0),"Held shield only, no double subtraction on touch/brilliant energy: "+type);
        }
    }
    static void Lists(BlueprintAbility b,int[] levels){var links=b.GetComponents<SpellListComponent>().ToArray();Check(links.Select(x=>x.SpellLevel).OrderBy(x=>x).SequenceEqual(levels),"Class spell levels");foreach(var x in links)Check(x.SpellList.SpellsByLevel[x.SpellLevel].Spells.Count(a=>a==b)==1,"Reciprocal unique list membership");}
    static bool Trigger(RuleSavingThrow __instance){saves++;saving=__instance;return false;}
    static bool Passed(ref bool __result){__result=passed;return false;}
    static bool Saving(ref RuleSavingThrow __result){__result=saving;return false;}
    static bool Apply(ContextActionApplyBuff __instance){applies++;applied=__instance;return false;}
    static bool AC(int __0){shieldPenalty+=__0;return false;}
    static bool Hand(ref HandSlot __result){__result=Empty<HandSlot>();return false;}
    static bool Shield(ref bool __result){__result=hasShield;return false;}
}
