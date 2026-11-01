using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicAreaSpells
    {
        internal static readonly string[] Tokens={"PathOfGlory","Flashfire"};
        static ContextValue Rank(AbilityRankType type=AbilityRankType.Default)=>new ContextValue {ValueType=ContextValueType.Rank,ValueRank=type};
        static ContextRankConfig Scaling(string name, bool damage=false){
            var r=new ContextRankConfig {name=name};SetEnum(r,"m_BaseValueType","CasterLevel");
            if(damage){SetEnum(r,"m_Type","DamageDice");SetEnum(r,"m_Progression","DivStep");Set(r,"m_StepLevel",3);Set(r,"m_UseMin",true);Set(r,"m_Min",1);Set(r,"m_UseMax",true);Set(r,"m_Max",5);}
            return r;
        }
        static BlueprintAbility Spell(PsychicAssets f,string token,string title,string text,BlueprintAbilityAreaEffect area,SpellSchool school,SpellDescriptor descriptor,AbilityRange range){
            var spawn=new ContextActionSpawnAreaEffect {DurationValue=new ContextDurationValue {Rate=DurationRate.Rounds,DiceCountValue=0,BonusValue=Rank()},OnUnit=false};Set(spawn,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,range,new SpellComponent {name=token+"School",School=school},new SpellDescriptorComponent {name=token+"Descriptor",Descriptor=descriptor},Scaling(token+"Duration"),Run(spawn));
            a.CanTargetPoint=true;a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=true;a.ShouldTurnToTarget=true;a.SpellResistance=area.SpellResistance;
            SetEnum(a,"Animation","Point");a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;
            a.LocalizedDuration=Text(token,"duration","1 round per caster level");a.LocalizedSavingThrow=Text(token,"savingThrow","");return a;
        }
        internal static void Build(PsychicAssets f){Glory(f,false);Glory(f,true);Fire(f);}
        static void Glory(PsychicAssets f,bool greater){
            string token=greater?"PathOfGloryGreater":"PathOfGlory", title=greater?"Path of Glory, Greater":"Path of Glory";int heal=greater?5:1;
            string text="Create a glowing 60-foot-long, 5-foot-wide path within close range for 1 round per caster level. Allies standing on it gain fast healing "+heal+". Leaving the path or its ending removes this benefit. Overlapping paths do not stack; the strongest path applies. As in Call of the Wild, this is a fixed line rather than freely drawn squares.";
            var buff=f.Buff(token+"Buff",title,text,new PsychicGloryHealing {name=token+"Healing",Amount=heal});buff.Stacking=StackingType.Stack;Set(buff,"m_Icon",PsychicOriginalIcons.Load("PathOfGlory"));
            var owned=new AbilityAreaEffectBuff {name=token+"OwnedBuff",Condition=new ConditionsChecker {Conditions=new Condition[]{new ContextConditionIsAlly()}},CheckConditionEveryRound=true};Set(owned,"m_Buff",Ref<BlueprintBuffReference>(buff));
            // Wrath's native Wall shape is exactly Size x 5 feet; its targeting preview uses this spawn action.
            var area=f.Add(new BlueprintAbilityAreaEffect {Shape=AreaEffectShape.Wall,Size=new Kingmaker.Utility.Feet(60),AffectEnemies=false,AggroEnemies=false,SpellResistance=false,Fx=f.Native<BlueprintAbilityAreaEffect>("a28d640657feaa645afe7fa545c4e593").Fx,ComponentsArray=new BlueprintComponent[]{owned}},token+"Area",title,text);SetEnum(area,"m_TargetType","Ally");
            var a=Spell(f,token,title,text,area,SpellSchool.Conjuration,SpellDescriptor.RestoreHP,AbilityRange.Close);Set(a,"m_Icon",buff.Icon);a.NotOffensive=true;a.EffectOnAlly=AbilityEffectOnUnit.Helpful;a.EffectOnEnemy=AbilityEffectOnUnit.None;
        }
        static void Fire(PsychicAssets f){
            const string text="Create a 30-foot-long, 5-foot-wide line of flames within medium range for 1 round per caster level. Each creature entering the line or starting its turn there takes 1d6 fire damage per 3 caster levels (minimum 1d6, maximum 5d6) and catches fire. Reflex negates both; spell resistance applies. Burning deals 1d6 fire damage each round until a successful Reflex save extinguishes it, even after leaving the line. The flames can harm allies.";
            var icon=f.Native<BlueprintAbility>("19309b5551a28d74288f4b6f7d8d838d").Icon;
            var burning=f.Clone(f.Native<BlueprintBuff>("e92ecaa76b5db674fa5b0aaff5b21bc9"),"FlashfireBurningBuff","Flashfire: Burning",text);burning.Stacking=StackingType.Replace;Set(burning,"m_Icon",icon);
            var apply=Apply(burning,0);apply.Permanent=true;apply.AsChild=false;apply.NotLinkToAreaEffect=true;
            var damage=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=DamageEnergyType.Fire},Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=Rank(AbilityRankType.DamageDice),BonusValue=0},IsAoE=true,IgnoreCritical=true,DisableSneakDamage=true};
            var save=new ContextActionSavingThrow {Type=SavingThrowType.Reflex,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{damage,apply}))};
            var run=new AbilityAreaEffectRunAction {name="FlashfireEnterAndRound",UnitEnter=Actions(save),Round=Actions(save),UnitMove=Actions(),UnitExit=Actions()};
            var area=f.Add(new BlueprintAbilityAreaEffect {Shape=AreaEffectShape.Wall,Size=new Kingmaker.Utility.Feet(30),AffectEnemies=true,AggroEnemies=true,SpellResistance=true,Fx=new PrefabLink {AssetId="42eec2a08ed8721448fac9b8a51e5912"},ComponentsArray=new BlueprintComponent[]{run,Scaling("FlashfireDamage",true),new SpellDescriptorComponent {name="FlashfireAreaFire",Descriptor=SpellDescriptor.Fire}}},"FlashfireArea","Flashfire",text);SetEnum(area,"m_TargetType","Any");Set(area,"m_TickRoundAfterSpawn",false);
            var a=Spell(f,"Flashfire","Flashfire",text,area,SpellSchool.Evocation,SpellDescriptor.Fire,AbilityRange.Medium);Set(a,"m_Icon",icon);a.EffectOnAlly=a.EffectOnEnemy=AbilityEffectOnUnit.Harmful;a.AvailableMetamagic|=Metamagic.Empower|Metamagic.Maximize|Metamagic.Persistent|Metamagic.Piercing|Metamagic.Selective;
            a.LocalizedSavingThrow=Text("Flashfire","savingThrow","Reflex negates");
        }
    }
    [TypeId("9a62b8d340db45f385afc9e038b1801f")]
    public sealed class PsychicGloryHealing : UnitBuffComponentDelegate,ITickEachRound
    {
        public int Amount;
        public void OnNewRound(){
            if(Owner.State.IsDead || Owner.Damage<=0)return;
            // Area ownership requires coexisting facts. Only the strongest active fact heals.
            var strongest=Owner.Buffs.Enumerable.Where(b=>b.IsActive&&!b.IsSuppressed)
                .Select(b=>new {Buff=b,Healing=b.Blueprint.GetComponent<PsychicGloryHealing>()})
                .Where(x=>x.Healing!=null).OrderByDescending(x=>x.Healing.Amount).FirstOrDefault();
            if(strongest?.Buff==Buff)GameHelper.HealDamage(Owner,Owner,Amount,Fact);
        }
    }
}
