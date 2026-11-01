using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using System;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicAspectSpells
    {
        internal static readonly string[] Tokens={"ThreefoldAspect","BlisteringInvective","WallOfNausea","WallOfBlindness","RighteousVigor"};
        static ContextValue Rank()=>new ContextValue {ValueType=ContextValueType.Rank};
        static ContextRankConfig Scaling(){var r=new ContextRankConfig {name="AspectSpellDuration"};SetEnum(r,"m_BaseValueType","CasterLevel");return r;}
        static void Display(BlueprintAbility a,string duration,string save=""){a.LocalizedDuration=Text(a.name,"duration",duration);a.LocalizedSavingThrow=Text(a.name,"savingThrow",save);}
        static AbilitySpawnFx Fx(string id)=>new AbilitySpawnFx {name="AspectCastVisual",PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
        internal static void Build(PsychicAssets f){Aspect(f);Invective(f);Wall(f,false);Wall(f,true);Vigor(f);}
        static void Aspect(PsychicAssets f){
            const string text="For 24 hours, switch between three idealized ages or your natural age as a standard action. Young grants +2 enhancement to Dexterity and Constitution and -2 Wisdom. Adult grants +2 enhancement to Wisdom and Intelligence and -2 Dexterity. Elder grants +4 enhancement to Wisdom and Intelligence and -2 Strength and Dexterity. Only one aspect applies at a time. Natural removes these adjustments. The spell changes ability scores without changing your character model.";
            var icon=f.Native<BlueprintAbility>("3e4ab69ada402d145a5e0ad3ad4b8564").Icon;
            AddStatBonus B(StatType stat,int value)=>new AddStatBonus {name="Aspect"+stat,Stat=stat,Value=value,Descriptor=value>0?ModifierDescriptor.Enhancement:ModifierDescriptor.UntypedStackable};
            var buffs=new[]{f.Buff("ThreefoldAspectYoungBuff","Threefold Aspect: Young",text,B(StatType.Dexterity,2),B(StatType.Constitution,2),B(StatType.Wisdom,-2)),f.Buff("ThreefoldAspectAdultBuff","Threefold Aspect: Adult",text,B(StatType.Wisdom,2),B(StatType.Intelligence,2),B(StatType.Dexterity,-2)),f.Buff("ThreefoldAspectElderBuff","Threefold Aspect: Elder",text,B(StatType.Wisdom,4),B(StatType.Intelligence,4),B(StatType.Strength,-2),B(StatType.Dexterity,-2))};
            foreach(var b in buffs)Set(b,"m_Icon",icon);
            var host=f.Buff("ThreefoldAspectBuff","Threefold Aspect",text);Set(host,"m_Icon",icon);
            var variants=new BlueprintAbility[4];var labels=new[]{"Young","Adult","Elder","Natural"};
            for(int i=0;i<4;i++){
                var gate=new AbilityCasterHasFacts {name="AspectRequiresHost"};Set(gate,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(host)});
                var change=new PsychicChangeAspect {Host=Ref<BlueprintBuffReference>(host),Aspects=buffs.Select(Ref<BlueprintBuffReference>).ToArray(),Choice=i};
                var a=f.Ability("ThreefoldAspect"+labels[i],"Threefold Aspect: "+labels[i],text,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,gate,Run(change),Fx("352469f228a3b1f4cb269c7ab0409b8e"));
                Set(a,"m_Icon",icon);a.SpellResistance=false;Display(a,"Until Threefold Aspect ends or another aspect is chosen");variants[i]=a;
            }
            var options=new AbilityVariants {name="AspectChoices"};Set(options,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());
            var menu=f.Ability("ThreefoldAspectChange","Threefold Aspect: Change Age",text,AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,options);Set(menu,"m_Icon",icon);Display(menu,"Until Threefold Aspect ends");
            foreach(var a in variants)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(menu));host.ComponentsArray=new BlueprintComponent[]{Facts(menu)};
            var apply=Apply(host,1,DurationRate.Days);apply.IsNotDispelable=true;
            var spell=f.Ability("ThreefoldAspect","Threefold Aspect",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {name="AspectSchool",School=SpellSchool.Transmutation},Run(apply),Fx("352469f228a3b1f4cb269c7ab0409b8e"));
            Set(spell,"m_Icon",icon);spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;Display(spell,"24 hours");
        }
        static void Invective(PsychicAssets f){
            const string text="Attempt to demoralize each enemy within 30 feet using the native Intimidate check. Each successful check deals 1d10 fire damage; Reflex negates catching fire but not this initial damage. Burning deals 1d6 fire damage per round until a successful Reflex save extinguishes it. The demoralize effect follows the game's fear and Intimidate rules.";
            var a=f.Clone(f.Native<BlueprintAbility>("5f3126d4120b2b244a95cb2ec23d69fb"),"BlisteringInvective","Blistering Invective",text);
            var native=a.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions?.OfType<Demoralize>().FirstOrDefault()
                ??throw new InvalidOperationException("Blistering Invective requires the native Demoralize action on 5f3126d4120b2b244a95cb2ec23d69fb; registration cancelled before publication.");
            var burning=f.Clone(f.Native<BlueprintBuff>("e92ecaa76b5db674fa5b0aaff5b21bc9"),"BlisteringInvectiveBurningBuff","Blistering Invective: Burning",text);burning.Stacking=StackingType.Replace;
            var icon=f.Native<BlueprintAbility>("cf6c901fb7acc904e85c63b342e9c949").Icon;Set(burning,"m_Icon",icon);
            var burn=Apply(burning,0);burn.Permanent=true;burn.AsChild=false;
            var damage=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=DamageEnergyType.Fire},Value=new ContextDiceValue {DiceType=DiceType.D10,DiceCountValue=1,BonusValue=0},IsAoE=true,IgnoreCritical=true,DisableSneakDamage=true};
            var follow=new PsychicInvectiveDemoralize {Demoralize=native,Success=Actions(damage,new ContextActionSavingThrow {Type=SavingThrowType.Reflex,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{burn}))})};
            a.ComponentsArray=a.ComponentsArray.Where(c=>c is AbilityTargetsAround||c is AbilitySpawnFx).Concat(new BlueprintComponent[]{Run(follow),new SpellComponent {name="InvectiveSchool",School=SpellSchool.Evocation},new SpellDescriptorComponent {name="InvectiveFire",Descriptor=SpellDescriptor.Fire}}).ToArray();
            a.Type=AbilityType.Spell;a.ActionType=UnitCommand.CommandType.Standard;Set(a,"m_IsFullRoundAction",false);a.SpellResistance=false;Set(a,"m_Icon",icon);a.AvailableMetamagic=Metamagic.Empower|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Maximize|Metamagic.Persistent;Display(a,"Instantaneous; burning until extinguished","Reflex partial");
        }
        static void Wall(PsychicAssets f,bool blind){
            string token=blind?"WallOfBlindness":"WallOfNausea",title=blind?"Wall of Blindness":"Wall of Nausea";
            string text="Create a 30-foot-long, 5-foot-wide wall within medium range for 1 round per caster level. Creatures crossing the wall must make a Fortitude save or "+(blind?"become permanently blinded.":"become nauseated for 1 round, then make a DC 12 Mobility check or fall prone.")+" Spell resistance applies. Constructs, undead and elementals are unaffected. It affects allies too. Creating the wall on a creature causes it to fail. Standing still within it does not repeat the effect.";
            var icon=blind?f.Native<BlueprintAbility>("46fd02ad56c35224c9c91c88cd457791").Icon:PsychicOriginalIcons.Load("WallOfNausea");
            var debuff=f.Clone(blind?f.Native<BlueprintBuff>("187f88d96a0ef464280706b63635f2af"):f.Native<BlueprintBuff>("956331dba5125ef48afe41875a00ca0e"),token+"Debuff",title,text);Set(debuff,"m_Icon",icon);debuff.Stacking=StackingType.Replace;
            var descriptor=blind?SpellDescriptor.Blindness:SpellDescriptor.Nauseated|SpellDescriptor.MindAffecting;
            foreach(var d in debuff.GetComponents<SpellDescriptorComponent>())d.Descriptor=blind?SpellDescriptor.Blindness:SpellDescriptor.Nauseated;
            // Keep both historical GUIDs registered for existing saves. New blindness
            // applications use the native shared status; nausea remains non-poison.
            var nativeStatus=blind?f.Native<BlueprintBuff>("187f88d96a0ef464280706b63635f2af"):f.Native<BlueprintBuff>("956331dba5125ef48afe41875a00ca0e");
            Set(debuff,"m_DisplayName",Get(nativeStatus,"m_DisplayName"));Set(debuff,"m_Icon",nativeStatus.Icon);
            var apply=Apply(blind?nativeStatus:debuff,1);apply.Permanent=blind;apply.NotLinkToAreaEffect=true;apply.AsChild=false;
            var effects=blind?new GameAction[]{apply}:new GameAction[]{apply,new ContextActionSkillCheck {Stat=StatType.SkillMobility,UseCustomDC=true,CustomDC=12,Success=Actions(),Failure=Actions(new ContextActionKnockdownTarget()),FailureDiffMoreOrEqual5Less10=Actions(),FailureDiffMoreOrEqual10=Actions()}};
            var immune=new[]{f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f"),f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33"),f.Native<BlueprintFeature>("198fd8924dabcb5478d0f78bd453c586")};
            var checks=immune.Select(b=>{var c=new ContextConditionHasFact();Set(c,"m_Fact",Ref<BlueprintUnitFactReference>(b));return (Condition)c;}).ToArray();
            var effect=new Conditional {ConditionsChecker=new ConditionsChecker {Operation=Operation.Or,Conditions=checks},IfTrue=Actions(),IfFalse=Actions(new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(Saved(Array.Empty<GameAction>(),effects))})};
            var area=f.Clone(f.Native<BlueprintAbilityAreaEffect>("2a90aa7f771677b4e9624fa77697fdc6"),token+"Area",title,text);
            area.Shape=AreaEffectShape.Wall;area.Size=new Feet(30);area.SpellResistance=true;area.AffectEnemies=true;area.AggroEnemies=true;SetEnum(area,"m_TargetType","Any");Set(area,"m_TickRoundAfterSpawn",true);
            area.ComponentsArray=new BlueprintComponent[]{new PsychicUnoccupiedWall {name=token+"Crossing",UnitEnter=Actions(effect),UnitExit=Actions(),UnitMove=Actions(),Round=Actions()},new SpellDescriptorComponent {name=token+"AreaDescriptor",Descriptor=descriptor}};
            var spawn=new ContextActionSpawnAreaEffect {DurationValue=new ContextDurationValue {Rate=DurationRate.Rounds,DiceCountValue=0,BonusValue=Rank()}};Set(spawn,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Medium,new SpellComponent {name=token+"School",School=blind?SpellSchool.Necromancy:SpellSchool.Illusion},new SpellDescriptorComponent {name=token+"Descriptor",Descriptor=descriptor},Scaling(),Run(spawn));
            a.CanTargetPoint=true;a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=true;a.ShouldTurnToTarget=true;a.SpellResistance=true;a.EffectOnAlly=a.EffectOnEnemy=AbilityEffectOnUnit.Harmful;SetEnum(a,"Animation","Point");Set(a,"m_Icon",icon);a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing;Display(a,"Wall: 1 round per caster level; "+(blind?"blindness: permanent":"nausea: 1 round"),"Fortitude negates");
        }
        static void Vigor(PsychicAssets f){
            const string text="For 1 round per caster level, each successful melee weapon, unarmed or natural attack grants a cumulative +1 morale bonus on attack rolls (maximum +4) and 1d8 temporary hit points (maximum 20 remaining from this spell). Any missed attack resets the attack bonus to +0 without removing temporary hit points. Ranged and touch-spell hits grant no stacks. Temporary hit points are consumed normally by damage and disappear when the spell ends.";
            var b=f.Buff("RighteousVigorBuff","Righteous Vigor",text,new PsychicRighteousVigor {name="VigorCumulativeHits",HitPoints=new ContextDiceValue {DiceType=DiceType.D8,DiceCountValue=1,BonusValue=0}},new SpellDescriptorComponent {name="VigorBuffMind",Descriptor=SpellDescriptor.MindAffecting});Set(b,"m_Icon",PsychicOriginalIcons.Load("RighteousVigor"));
            var apply=Apply(b,0);apply.DurationValue.BonusValue=Rank();
            var fx=f.Native<BlueprintAbility>("4c349361d720e844e846ad8c19959b1e").GetComponent<AbilitySpawnFx>();
            var a=f.Ability("RighteousVigor","Righteous Vigor",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Touch,new SpellComponent {name="VigorSchool",School=SpellSchool.Enchantment},new SpellDescriptorComponent {name="VigorMind",Descriptor=SpellDescriptor.MindAffecting},Scaling(),Run(apply),fx);Set(a,"m_Icon",b.Icon);a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=false;a.NotOffensive=true;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;Display(a,"1 round per caster level");
        }
    }
    [TypeId("d530f5c77ea147df96aed7a3a96c9151")]
    public sealed class PsychicChangeAspect : ContextAction
    {
        public BlueprintBuffReference Host; public BlueprintBuffReference[] Aspects;public int Choice;
        public override string GetCaption()=>"Change Threefold Aspect";
        public override void RunAction(){
            var unit=Context.MaybeCaster;var host=unit?.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint==Host.Get()&&b.IsActive&&!b.IsSuppressed);if(host==null)return;
            foreach(var b in unit.Buffs.Enumerable.Where(b=>Aspects.Any(r=>b.Blueprint==r.Get())).ToArray())b.Remove();
            if(Choice<0||Choice>=Aspects.Length)return;
            var child=unit.Descriptor.AddBuff(Aspects[Choice].Get(),host.Context,null);if(child!=null)host.StoreFact(child);
        }
    }
    [TypeId("ec5b656cabbd4ed397a9c71292f5f5b2")]
    public sealed class PsychicInvectiveDemoralize : ContextAction
    {
        public Demoralize Demoralize;public ActionList Success;
        public override string GetCaption()=>"Demoralize and scorch on success";
        // Scope to this one native action. Do not patch or replace global Demoralize.
        public override void RunAction(){
            var capture=new Capture {Caster=Context.MaybeCaster};EventBus.Subscribe(capture);
            try{Demoralize.RunAction();}finally{EventBus.Unsubscribe(capture);}
            if(capture.Succeeded)Success.Run();
        }
        sealed class Capture : IGlobalRulebookHandler<RuleSkillCheck>,ISubscriber
        {
            public UnitEntityData Caster;public bool Succeeded;
            public void OnEventAboutToTrigger(RuleSkillCheck e){}
            public void OnEventDidTrigger(RuleSkillCheck e){if(e.Initiator==Caster&&e.StatType==StatType.CheckIntimidate)Succeeded=e.Success;}
        }
    }
    [TypeId("b458e60ac4c94d899056caea0301a01c")]
    public sealed class PsychicUnoccupiedWall : AbilityAreaEffectRunAction
    {
        static readonly System.Reflection.FieldInfo Created=AccessTools.Field(typeof(AreaEffectEntityData),"m_CreationTime");
        internal static bool Initial(AreaEffectEntityData area)=>Game.Instance.TimeController.GameTime-(TimeSpan)Created.GetValue(area)<TimeSpan.FromSeconds(0.1);
        protected override void OnUnitEnter(MechanicsContext context,AreaEffectEntityData area,UnitEntityData unit){if(!Initial(area)&&!area.IsEnded)base.OnUnitEnter(context,area,unit);}
        protected override void OnRound(MechanicsContext context,AreaEffectEntityData area){if(Initial(area)&&area.InGameUnitsInside.Any())area.ForceEnd();}
    }
    public sealed class PsychicVigorState
    {
        [JsonProperty] public ModifiableValue.Modifier Attack,Temporary;
        [JsonProperty] public int StoredAttack,StoredHP;
    }
    [TypeId("e53e4a613d32423c968e707772ad126e")]
    public sealed class PsychicRighteousVigor : UnitBuffComponentDelegate<PsychicVigorState>,IInitiatorRulebookHandler<RuleAttackRoll>,ISubscriber
    {
        public ContextDiceValue HitPoints;
        protected override void OnTurnOn(){
            if(Data.Attack?.AppliedTo==null)Data.Attack=Owner.Stats.AdditionalAttackBonus.AddModifier(Data.StoredAttack,Fact,ModifierDescriptor.Morale);
            if(Data.Temporary?.AppliedTo==null)Data.Temporary=Owner.Stats.TemporaryHitPoints.AddModifier(Data.StoredHP,Fact,ModifierDescriptor.Morale);
            Data.StoredAttack=Data.StoredHP=0;
        }
        protected override void OnTurnOff(){
            Data.StoredAttack=Current(Data.Attack);Data.StoredHP=Current(Data.Temporary);Data.Attack?.Remove();Data.Temporary?.Remove();Data.Attack=null;Data.Temporary=null;
        }
        static int Current(ModifiableValue.Modifier m)=>m?.AppliedTo==null?0:Math.Max(0,m.ModValue);
        public void OnEventAboutToTrigger(RuleAttackRoll e){}
        public void OnEventDidTrigger(RuleAttackRoll e){
            if(e.Initiator!=Owner||e.IsCriticalRoll)return;
            if(!e.IsHit){SetAttack(0);return;}
            if(e.Weapon?.Blueprint.IsMelee!=true||e.Weapon.Blueprint.Category==WeaponCategory.Touch||e.Weapon.Blueprint.Category==WeaponCategory.Ray)return;
            SetAttack(Math.Min(4,Current(Data.Attack)+1));
            int hp=Math.Min(20,Current(Data.Temporary)+HitPoints.Calculate(Context));Data.Temporary?.Remove();Data.Temporary=Owner.Stats.TemporaryHitPoints.AddModifier(hp,Fact,ModifierDescriptor.Morale);
        }
        void SetAttack(int value){Data.Attack?.Remove();Data.Attack=Owner.Stats.AdditionalAttackBonus.AddModifier(value,Fact,ModifierDescriptor.Morale);}
    }
}
