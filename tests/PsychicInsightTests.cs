using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
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
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicInsightTests
{
    static int count;static EntityFactsManager facts;
    static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
    static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
    static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}
    static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint=>(T)cache.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicLocateWeakness).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token}));
    static Buff Fact(BlueprintBuff bp){var b=Empty<Buff>();Set(b,"<Blueprint>k__BackingField",bp);Set(b,"Components",new List<EntityFactComponent>());b.RestoreOwnerLink(facts);return b;}
    static EntityFactComponent Runtime(BlueprintComponent c,UnitFact fact){var r=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);r.Setup(c);r.RestoreOwnerLink(fact);return r;}
    internal static int Run(BlueprintsCache cache)
    {
        var f=new Psychic070Tests.Caster(cache);facts=new EntityFactsManager(f.Unit);Set(f.Unit,"Facts",facts);
        var harmony=new Harmony("CotW.InsightSpells.Managed");
        try {
            harmony.CreateClassProcessor(typeof(PsychicLocateWeakness).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicDivideMindPatch")).Patch();
            harmony.CreateClassProcessor(typeof(PsychicLocateWeakness).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicDivideMindActionPatch")).Patch();
            BlueprintChecks(cache,f);Casting(cache,f);Rerolls(cache,f);Criticals(cache,f);
        } finally{harmony.UnpatchAll(harmony.Id);}
        PsychicExpansionTests.Inventory["insightSpellAssertions"]=count;
        Console.WriteLine("PASS: "+count+" Divide Mind/Locate Weakness checks: isolated acceleration and registry cleanup, action types, filtered rerolls and damage calculation. Constructed entities and seeded dice; not gameplay.");return count;
    }
    static void BlueprintChecks(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        foreach(string token in new[]{"DivideMind","LocateWeakness"}) {
            var bp=Asset<BlueprintAbility>(cache,token);var apply=bp.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionApplyBuff>().Single();
            Check(bp.Type==AbilityType.Spell && bp.Range==AbilityRange.Personal && bp.ActionType==UnitCommand.CommandType.Standard && !bp.SpellResistance,"Personal standard spell, no resistance");
            Check(((System.Collections.IDictionary)AccessTools.Field(typeof(PsychicLocateWeakness).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Strings").GetValue(null))[bp.LocalizedSavingThrow.Key].Equals("") && !string.IsNullOrEmpty(bp.LocalizedDuration.Key) && !ReferenceEquals(bp.Icon,null) && !ReferenceEquals(apply.Buff.Icon,null),"No None save row, real duration and icons");
            Check(apply.Buff.Stacking==StackingType.Replace && !apply.Permanent && !apply.IsNotDispelable && apply.IsFromSpell,"Dispellable timed replace buff");
            foreach(var link in bp.GetComponents<SpellListComponent>())Check(link.SpellList.SpellsByLevel[link.SpellLevel].Spells.Count(x=>x==bp)==1,"Reciprocal list registration exactly once");
            foreach(int cl in new[]{1,7,20})foreach(bool extended in new[]{false,true}) {
                var cast=f.Cast(bp.School,0,token=="DivideMind"?9:3);cast.Context.SetParams(new AbilityParams {CasterLevel=cl,Metamagic=extended?Metamagic.Extend:0});Set(cast.Context,"<ClickedTarget>k__BackingField",new Kingmaker.Utility.TargetWrapper(f.Unit));
                var rank=bp.GetComponent<ContextRankConfig>();if(rank!=null)Set(cast.Context,"m_Ranks",Enumerable.Repeat(rank.GetValue(cast.Context),32).ToArray());
                Check(apply.DurationValue.Calculate(cast.Context).Seconds==TimeSpan.FromMinutes((token=="DivideMind"?1:cl)*(extended?2:1)),"Native CL/fixed duration and Extend");
            }
            Check(bp.GetComponents<SpellListComponent>().Count()==(token=="DivideMind"?1:7),"Psychic9; Psychic/Wizard/Magus/Inquisitor/Witch/Bloodrager3 and Ranger2");
        }
    }
    static void Casting(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintBuff>(cache,"DivideMindBuff");var c=bp.GetComponent<PsychicDivideMindMetamagic>();var fact=Fact(bp);Set(fact,"<IsTurnedOn>k__BackingField",true);facts.List.Add(fact);var buffs=new BuffCollection();buffs.Initialize(facts);Set(f.Unit,"<Buffs>k__BackingField",buffs);var runtime=Runtime(c,fact);
        var cast=f.Cast(SpellSchool.Evocation,0,3).Spell;cast.OverrideSpellLevel=3;cast.Blueprint.ActionType=UnitCommand.CommandType.Standard;
        using(runtime.RequestEventContext())AccessTools.Method(typeof(AutoMetamagic),"OnTurnOn").Invoke(c,null);
        var part=f.Unit.Get<UnitPartSpecialMetamagic>();Check(part==null && !cast.HasMetamagic(Metamagic.Quicken) && cast.ActionType==UnitCommand.CommandType.Swift,"Acceleration does not register in global metamagic lookup");
        Set(cast,"m_CachedSpellbook",null);cast.Blueprint.Type=AbilityType.SpellLike;Check(AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,cast),"Spell-like ability without book allowed");Set(cast,"m_CachedSpellbook",f.Book);cast.Blueprint.Type=AbilityType.Spell;
        var original=f.Book.Blueprint;
        foreach(int level in new[]{0,1,5,6,9})foreach(AbilityType type in new[]{AbilityType.Spell,AbilityType.SpellLike,AbilityType.Supernatural,AbilityType.Physical})foreach(var action in new[]{UnitCommand.CommandType.Standard,UnitCommand.CommandType.Move,UnitCommand.CommandType.Free,UnitCommand.CommandType.Swift}) {
            cast.OverrideSpellLevel=level;cast.Blueprint.Type=type;cast.Blueprint.ActionType=action;
            bool expected=level<=5 && (type==AbilityType.Spell||type==AbilityType.SpellLike) && (action==UnitCommand.CommandType.Standard||action==UnitCommand.CommandType.Move);
            Check(AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,cast)==expected,"Eligibility level/type/action "+level+"/"+type+"/"+action);
            Check(cast.ActionType==(expected?UnitCommand.CommandType.Swift:action),"Native action cost uses same filter without recursion");
        }
        cast.OverrideSpellLevel=3;cast.Blueprint.Type=AbilityType.Spell;cast.Blueprint.ActionType=UnitCommand.CommandType.Standard;
        Set(cast.Blueprint,"m_IsFullRoundAction",true);Check(!AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,cast),"Full-round casting excluded");Set(cast.Blueprint,"m_IsFullRoundAction",false);
        Set(f.Book,"Blueprint",new BlueprintSpellbook {IsArcane=true});Check(!AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,cast),"Arcane mixed-class spell excluded");Set(f.Book,"Blueprint",new BlueprintSpellbook());Check(!AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,cast),"Divine mixed-class spell excluded");Set(f.Book,"Blueprint",original);
        var itemFact=Empty<Ability>();Set(itemFact,"<SourceItem>k__BackingField",Empty<ItemEntityWeapon>());Set(cast,"m_Fact",itemFact);Check(!AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,cast),"Item source excluded");Set(cast,"m_Fact",null);
        cast.MetamagicData=new MetamagicData();cast.MetamagicData.Add(Metamagic.Extend);Check(!AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,cast),"Spontaneous full-round metamagic excluded");cast.MetamagicData=null;
        Check(!AutoMetamagic.ShouldApplyTo(c,cast.Blueprint,null),"No data cannot bypass book/level filter through parent recursion");
        var ordinary=new AutoMetamagic {Metamagic=Metamagic.Quicken,Abilities=new List<BlueprintAbilityReference>()};Set(f.Book,"Blueprint",new BlueprintSpellbook {IsArcane=true});Check(AutoMetamagic.ShouldApplyTo(ordinary,cast.Blueprint,cast),"Other native automatic metamagic unchanged");Set(f.Book,"Blueprint",original);
        cast.OverrideSpellLevel=null;Set(cast,"<SpellLevelInSpellbook>k__BackingField",(int?)3);
        Check(cast.SpellLevel==3 && cast.ActionType==UnitCommand.CommandType.Swift && !cast.HasMetamagic(Metamagic.Quicken),"Real spellbook level path accelerates without global query hook");
        var foreign=Fact(new BlueprintBuff());var ordinaryMeta=new AutoMetamagic {Metamagic=Metamagic.Extend,Abilities=new List<BlueprintAbilityReference>()};
        var legacy=f.Unit.Ensure<UnitPartSpecialMetamagic>();legacy.Add(fact,c);legacy.Add(foreign,ordinaryMeta);
        using(runtime.RequestEventContext())AccessTools.Method(typeof(AutoMetamagic),"OnTurnOn").Invoke(c,null);
        Check(!cast.HasMetamagic(Metamagic.Quicken) && cast.HasMetamagic(Metamagic.Extend),"Reactivation removes old Divide Mind registration, retains other buffs");legacy.Remove(foreign,ordinaryMeta);
        Set(fact,"<IsSuppressed>k__BackingField",true);Check(cast.ActionType==UnitCommand.CommandType.Standard,"Suppression immediately stops acceleration");Set(fact,"<IsSuppressed>k__BackingField",false);
        var center=cache.Load(BlueprintGuid.Parse("e66116541ac4546c9084a7bcfa47e5a6")) as BlueprintAbility;
        Check(center!=null,"Center Self blueprint present");{var previous=cast.Blueprint;Set(cast,"Blueprint",center);Check(cast.ActionType==center.ActionType && !cast.HasMetamagic(Metamagic.Quicken),"Center Self keeps its normal action and metamagic query");Set(cast,"Blueprint",previous);}
        var evt=new Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams(f.Unit,cast);
        using(runtime.RequestEventContext())((Kingmaker.PubSubSystem.IInitiatorRulebookHandler<Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams>)c).OnEventAboutToTrigger(evt);
        Check(evt.HasMetamagic(Metamagic.Quicken),"Native parameter callback grants same metamagic");
        using(runtime.RequestEventContext())AccessTools.Method(typeof(AutoMetamagic),"OnTurnOff").Invoke(c,null);
        Set(fact,"<IsTurnedOn>k__BackingField",false);
        Check(f.Unit.Get<UnitPartSpecialMetamagic>()==null && cast.ActionType==UnitCommand.CommandType.Standard,"Native removal restores original action cost immediately");
    }
    static void Rerolls(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintBuff>(cache,"DivideMindBuff");var fact=Fact(bp);var dice=(Queue<int>)AccessTools.Field(typeof(Psychic070Tests),"dice").GetValue(null);
        var parts=bp.GetComponents<ModifyD20>().ToArray();var runtimes=parts.Select(c=>Runtime(c,fact)).ToArray();
        foreach(int sample in Enumerable.Range(0,9)) {
            RulebookEvent parent;bool expected=sample==0||sample==3||sample==4||sample==5;
            if(sample<3){var save=Empty<RuleSavingThrow>();Set(save,"Type",sample==0?SavingThrowType.Will:sample==1?SavingThrowType.Fortitude:SavingThrowType.Reflex);parent=save;}
            else if(sample<8){var skill=Empty<RuleSkillCheck>();Set(skill,"<StatType>k__BackingField",new[]{StatType.Intelligence,StatType.SkillKnowledgeArcana,StatType.SkillKnowledgeWorld,StatType.SkillPersuasion,StatType.Strength}[sample-3]);parent=skill;}
            else parent=Empty<RuleAttackRoll>();
            parent.Reason=Empty<RuleReason>();
            foreach(var pair in new[]{new[]{3,17},new[]{19,2}}){var roll=new RuleRollD20(f.Unit);var ctx=Rulebook.CurrentContext;ctx.PushEvent(parent);ctx.PushEvent(roll);dice.Clear();dice.Enqueue(pair[0]);dice.Enqueue(pair[1]);
                try{for(int i=0;i<parts.Length;i++)using(runtimes[i].RequestEventContext())parts[i].OnEventAboutToTrigger(roll);roll.OnTrigger(ctx);
                    Check(roll.Result==(expected?Math.Max(pair[0],pair[1]):pair[0]) && roll.RollHistory.Count==(expected?2:1),"Native reroll only Will/Int/Int skills, best die "+sample);
                    for(int i=0;i<parts.Length;i++)using(runtimes[i].RequestEventContext())parts[i].OnEventDidTrigger(roll);
                }finally{ctx.PopEvent(roll);ctx.PopEvent(parent);}
            }
        }
    }
    static void Criticals(BlueprintsCache cache,Psychic070Tests.Caster f)
    {
        var bp=Asset<BlueprintBuff>(cache,"LocateWeaknessBuff");var c=bp.GetComponent<PsychicLocateWeakness>();var fact=Fact(bp);var runtime=Runtime(c,fact);
        foreach(var category in new[]{WeaponCategory.Longsword,WeaponCategory.Longbow,WeaponCategory.UnarmedStrike,WeaponCategory.Claw,WeaponCategory.Ray,WeaponCategory.Touch})foreach(bool critical in new[]{false,true})foreach(int immunity in Enumerable.Range(0,5)) {
            var type=new BlueprintWeaponType();type.Category=category;var weaponBp=new BlueprintItemWeapon();Set(weaponBp,"m_Type",BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type));AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(Get(weaponBp,"m_Type"),type,null);
            var weapon=Empty<ItemEntityWeapon>();Set(weapon,"m_Blueprint",weaponBp);var physical=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(2,DiceType.D6)),4,PhysicalDamageForm.Slashing);var extra=new EnergyDamage(new DiceFormula(1,DiceType.D6),DamageEnergyType.Fire);var sneak=new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(3,DiceType.D6)),0,PhysicalDamageForm.Slashing){Precision=true};
            var bundle=new DamageBundle(weapon,physical);bundle.Add(extra);bundle.Add(sneak);
            var attack=Empty<RuleAttackRoll>();var weaponStats=Empty<RuleCalculateWeaponStats>();Set(weaponStats,"Weapon",weapon);Set(attack,"WeaponStats",weaponStats);Set(attack,"<IsCriticalConfirmed>k__BackingField",critical);
            var parent=Empty<RuleDealDamage>();Set(parent,"m_DamageBundle",bundle);Set(parent,"<AttackRoll>k__BackingField",attack);
            var rule=Empty<RuleCalculateDamage>();Set(rule,"ParentRule",parent);rule.CritImmunity=immunity==1;Set(attack,"<ImmuneToCriticalHit>k__BackingField",immunity==2||immunity==4);Set(attack,"<IgnoreCritImmunity>k__BackingField",immunity==4);Set(attack,"<FortificationNegatesCriticalHit>k__BackingField",immunity==3);
            using(runtime.RequestEventContext())c.OnEventAboutToTrigger(rule);
            bool expected=critical&&(immunity==0||immunity==4)&&category!=WeaponCategory.Ray&&category!=WeaponCategory.Touch;
            Check(physical.CalculationType.Value==(expected?DamageCalculationType.Maximized:DamageCalculationType.Normal),"Only confirmed effective weapon criticals maximize "+category);
            Check(extra.CalculationType.Value==DamageCalculationType.Normal && sneak.CalculationType.Value==DamageCalculationType.Normal && physical.Bonus==4,"Elemental/sneak/flat bonus packets preserved");
            if(expected) {int roll=(int)AccessTools.Method(typeof(RuleCalculateDamage),"Roll").Invoke(rule,new object[]{new DiceFormula(4,DiceType.D6),1,physical.CalculationType.Value});Check(roll==24,"Native maximum of supplied 4d6 weapon dice; critical scaling stays native");}
        }
    }
}



