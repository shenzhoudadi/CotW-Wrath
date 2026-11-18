using System;
using System.Linq;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicSupportSpells
    {
        internal static readonly string[] Tokens={"CondensedEther","BarrowHaze","BowSpirit","AlliedCloak","SmiteAbomination"};
        static ContextValue Rank()=>new ContextValue {ValueType=ContextValueType.Rank};
        static ContextRankConfig Scaling(){var c=new ContextRankConfig {name="SupportSpellDuration"};SetEnum(c,"m_BaseValueType","CasterLevel");return c;}
        static AbilitySpawnFx Fx(string id)=>new AbilitySpawnFx {name="SupportCastFx",PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
        static void Display(BlueprintAbility a,string duration){a.LocalizedDuration=Text(a.name,"duration",duration);a.LocalizedSavingThrow=Text(a.name,"savingThrow","");}
        static AbilityCasterHasFacts Needs(BlueprintBuff b){var c=new AbilityCasterHasFacts {name="RequiresSourceSpell"};Set(c,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(b)});return c;}
        static BlueprintAbility Host(PsychicAssets f,string token,string title,string text,BlueprintBuff buff,SpellSchool school,string fx){
            var apply=Apply(buff,0);apply.DurationValue.BonusValue=Rank();
            var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {School=school},Scaling(),Run(apply),Fx(fx));
            Set(a,"m_Icon",buff.Icon);a.NotOffensive=true;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;Display(a,"1 round per caster level");return a;
        }
        internal static void Build(PsychicAssets f){Cloud(f,false);Cloud(f,true);Bow(f);Cloak(f);Smite(f);}
        static void Cloud(PsychicAssets f,bool haze){
            string token=haze?"BarrowHaze":"CondensedEther",title=haze?"Barrow Haze":"Condensed Ether";
            string text=haze?"Create a 20-foot-radius bank of dark fog within medium range for 1 minute per caster level. It grants concealment within 5 feet and total concealment beyond 5 feet, on incoming and outgoing attacks. You see through your own barrow haze; other casters' clouds still affect you.":"Condense ether in a 20-foot-radius cloud within medium range for 1 minute per caster level. Creatures, including incorporeal creatures, move at half speed and take -2 to AC and Reflex saves. Attacks into or out of the cloud have a 20% miss chance within 5 feet and 50% beyond 5 feet. This is not concealment; Blind-Fight, true seeing, blindsight and effects that ignore concealment do not reduce it. Ordinary ranged weapon attacks into or out of it miss; rays are not blocked. Overlapping copies do not stack their penalties or slow.";
            var icon=haze?PsychicWeatherSpells.FogIcon(f):f.Native<BlueprintFeature>("3c53ee4965a13d74e81b37ae34f0861b").Icon;
            var fog=haze?PsychicWeatherSpells.ConcealmentBuff(f,"BarrowHazeFog","Barrow Haze: Concealment",PsychicWeatherSpells.FogConcealmentText,new PsychicHazeConcealment {name="HazeIncomingPartial",Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Partial},new PsychicHazeConcealment {name="HazeIncomingDistant",Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Total,CheckDistance=true,DistanceGreater=new Feet(5)}):null;
            var components=haze?Array.Empty<BlueprintComponent>():new BlueprintComponent[]{new AddCondition {name="EtherSlow",Condition=UnitCondition.Slowed},new PsychicEtherPenalty {name="EtherPenalties"},new PsychicWeatherMiss {name="EtherRangedBlock",Incoming=true,Outgoing=true}};
            var buff=PsychicWeatherSpells.OwnedBuff(f,token+"Buff",title,text,icon,components);
            var area=PsychicWeatherSpells.Area(f,token,title,text,new PrefabLink {AssetId="597682efc0419a142a3174fd6bb408f7"},AreaEffectShape.Cylinder,20,false,haze?new[]{PsychicWeatherSpells.Owned(fog),PsychicWeatherSpells.Owned(buff)}:new[]{PsychicWeatherSpells.Owned(buff)});
            PsychicWeatherSpells.AreaSpell(f,token,title,text,area,icon,haze?SpellSchool.Necromancy:SpellSchool.Transmutation,AbilityRange.Medium,DurationRate.Minutes);
        }
        static void Bow(PsychicAssets f){
            const string text="For 1 round per caster level, a bow spirit lets you make one additional attack with your equipped longbow or shortbow as a swift action each turn. Composite bows are included. The attack uses your ordinary bow attack and damage modifiers and does not provoke an attack of opportunity. Changing to another weapon disables the attack. The temporary ability is removed when the spell ends.";
            var buff=f.Buff("BowSpiritBuff","Bow Spirit",text);Set(buff,"m_Icon",PsychicOriginalIcons.Load("BowSpirit"));
            AbilityCasterMainWeaponCheck BowCheck()=>new AbilityCasterMainWeaponCheck {Category=new[]{WeaponCategory.Longbow,WeaponCategory.Shortbow}};
            var shot=f.Ability("BowSpiritShot","Bow Spirit: Shoot",text,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Weapon,Needs(buff),BowCheck(),Run(new PsychicBowSpiritAttack()));
            Set(shot,"m_Icon",buff.Icon);shot.CanTargetSelf=false;shot.CanTargetFriends=true;shot.CanTargetEnemies=true;shot.SpellResistance=false;shot.AvailableMetamagic=0;Display(shot,"Instantaneous");
            buff.ComponentsArray=new BlueprintComponent[]{Facts(shot)};
            var a=Host(f,"BowSpirit","Bow Spirit",text,buff,SpellSchool.Conjuration,"0c07afb9ee854184cb5110891324e3ad");a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)BowCheck()}).ToArray();
        }
        static void Cloak(PsychicAssets f){
            const string text="Animate your cloak for 1 round per caster level, gaining a +2 shield bonus to AC. Once per round, use a free action to choose +2 on your next melee weapon attack, or +2 AC against the next melee weapon attack against you. The aid expires after 1 round or after that attack, hit or miss. The shield bonus does not stack with other shield bonuses.";
            var icon=f.Native<BlueprintAbility>("e418c20c8ce362943a8025d82c865c1c").Icon;
            var cooldown=f.Buff("AlliedCloakCooldown","Allied Cloak: Recovering",text);Set(cooldown,"m_Icon",icon);
            var host=f.Buff("AlliedCloakBuff","Allied Cloak",text,new AddStatBonus {Stat=StatType.AC,Descriptor=ModifierDescriptor.Shield,Value=2});Set(host,"m_Icon",icon);
            var choices=new BlueprintAbility[2];
            for(int i=0;i<2;i++){
                bool defense=i==1;string token="AlliedCloak"+(defense?"Defense":"Attack");
                var b=f.Buff(token+"Buff",defense?"Allied Cloak: Defend":"Allied Cloak: Aid Attack",text,new PsychicCloakAid {Defense=defense});Set(b,"m_Icon",icon);
                var apply=Apply(b,1,fromSpell:false);apply.IsNotDispelable=true;var cd=Apply(cooldown,1,fromSpell:false);cd.IsNotDispelable=true;
                var a=f.Ability(token,defense?"Allied Cloak: Defend":"Allied Cloak: Aid Attack",text,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,Needs(host),new PsychicCloakReady {Cooldown=Ref<BlueprintBuffReference>(cooldown)},Run(apply,cd));
                Set(a,"m_Icon",icon);a.SpellResistance=false;Display(a,"1 round or until the next melee attack");choices[i]=a;
            }
            var variants=new AbilityVariants();Set(variants,"m_Variants",choices.Select(Ref<BlueprintAbilityReference>).ToArray());
            var menu=f.Ability("AlliedCloakAid","Allied Cloak: Aid",text,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,Needs(host),variants);Set(menu,"m_Icon",icon);menu.SpellResistance=false;Display(menu,"1 round or until used");
            foreach(var a in choices)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(menu));
            host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)Facts(menu)}).ToArray();Host(f,"AlliedCloak","Allied Cloak",text,host,SpellSchool.Abjuration,"c4d861e816edd6f4eab73c55a18fdadd");
        }
        static void Smite(PsychicAssets f){
            const string text="Mark one hostile undead within close range for 1 round per caster level (maximum 20 rounds). Against that target, gain an attack bonus and deflection AC bonus equal to the higher of your Wisdom and Charisma modifiers (minimum 0), damage equal to caster level (maximum 20), and ignore its damage reduction. It does not stack with your paladin Smite Evil on that target: the later mark replaces the earlier one. No alignment requirement or paladin resource is used.";
            var native=f.Native<BlueprintBuff>("b6570b8cbb32eaf4ca8255d0ec3310b0");
            var b=f.Clone(native,"SmiteAbominationBuff","Smite Abomination",text);
            b.ComponentsArray=b.ComponentsArray.Where(c=>c.GetType().Name!="RemoveBuffOnLoad"&&!(c is AddFactContextActions)).Concat(new BlueprintComponent[]{new ACBonusAgainstTarget {CheckCaster=true,Descriptor=ModifierDescriptor.Deflection,Value=new ContextValue {ValueType=ContextValueType.Shared,ValueShared=AbilitySharedValue.StatBonus}},new PsychicSmiteExclusion {NativeSmite=Ref<BlueprintBuffReference>(native)}}).ToArray();b.FxOnRemove=b.FxOnRemove??new PrefabLink();
            var a=f.Ability("SmiteAbomination","Smite Abomination",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Close,new SpellComponent {School=SpellSchool.Evocation},new PsychicSmiteTarget(),Run(new PsychicSmiteApply {Mark=Ref<BlueprintBuffReference>(b),NativeSmite=Ref<BlueprintBuffReference>(native)}),Fx("6e799a804a9ce4044a70eba38890cf5a"));
            Set(a,"m_Icon",f.Native<BlueprintAbility>("7bb9eb2042e67bf489ccd1374423cdec").Icon);a.CanTargetSelf=false;a.CanTargetFriends=false;a.CanTargetEnemies=true;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;Display(a,"1 round per caster level (maximum 20 rounds)");
        }
    }
    [TypeId("49f82c945e7a4aa2a6104278a1f72428")]
    public sealed class PsychicBowSpiritAttack:ContextAction
    {
        public override string GetCaption()=>"Bow spirit extra primary bow attack";
        public override void RunAction(){
            var caster=Context.MaybeCaster;var target=Target.Unit;var weapon=caster?.Body?.PrimaryHand?.MaybeWeapon;
            if(target==null||weapon==null||(weapon.Blueprint.Category!=WeaponCategory.Longbow&&weapon.Blueprint.Category!=WeaponCategory.Shortbow))return;
            Context.TriggerRule(new RuleAttackWithWeapon(caster,target,weapon,0){Reason=Context,ExtraAttack=true,IsFullAttack=false,AttackNumber=0,AttacksCount=1});
        }
    }
    [TypeId("16f2c8d902bd4ecc8878decd38e31e54")]
    public sealed class PsychicHazeConcealment:AddConcealment {}
    [TypeId("ba6bfc05f8a64781b5c521c25dd29033")]
    public sealed class PsychicEtherPenalty:UnitBuffComponentDelegate,ITargetRulebookHandler<RuleCalculateAC>,IInitiatorRulebookHandler<RuleSavingThrow>,ISubscriber
    {
        bool First()=>Owner.Buffs.Enumerable.FirstOrDefault(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<PsychicEtherPenalty>()!=null)==Buff;
        public void OnEventAboutToTrigger(RuleCalculateAC e){if(First())e.AddModifier(-2,Fact,ModifierDescriptor.Penalty);}
        public void OnEventAboutToTrigger(RuleSavingThrow e){if(e.Type==SavingThrowType.Reflex&&First())e.AddModifier(-2,Fact,ModifierDescriptor.Penalty);}
        public void OnEventDidTrigger(RuleCalculateAC e){} public void OnEventDidTrigger(RuleSavingThrow e){}
    }
    [TypeId("fe0f3ce9480545cd8670fa5d4f9719bc")]
    public sealed class PsychicCloakReady:BlueprintComponent,IAbilityCasterRestriction
    {
        public BlueprintBuffReference Cooldown;
        public bool IsCasterRestrictionPassed(UnitEntityData caster)=>!caster.Buffs.Enumerable.Any(b=>b.Blueprint==Cooldown.Get()&&b.IsTurnedOn&&!b.IsSuppressed);
        public string GetAbilityCasterRestrictionUIText()=>"The cloak can aid you only once per round.";
    }
    [TypeId("25cc19b775274e07b513e4d63f4cbb61")]
    public sealed class PsychicCloakAid:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,ITargetRulebookHandler<RuleCalculateAC>,IInitiatorRulebookHandler<RuleAttackWithWeapon>,ITargetRulebookHandler<RuleAttackWithWeapon>,ISubscriber
    {
        public bool Defense;
        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget e){if(!Defense&&e.Weapon?.Blueprint.IsMelee==true)e.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable);}
        public void OnEventAboutToTrigger(RuleCalculateAC e){if(Defense&&(e.AttackType==AttackType.Melee||e.AttackType==AttackType.Touch))e.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable);}
        public void OnEventDidTrigger(RuleAttackWithWeapon e){if(e.Weapon?.Blueprint.IsMelee==true&&(Defense?e.Target==Owner:e.Initiator==Owner))Buff.Remove();}
        public void OnEventAboutToTrigger(RuleAttackWithWeapon e){} public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget e){} public void OnEventDidTrigger(RuleCalculateAC e){}
    }
    [TypeId("b6418b19a5f64abf9743dc10e9567258")]
    public sealed class PsychicSmiteTarget:BlueprintComponent,IAbilityTargetRestriction
    {
        public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>target.Unit!=null&&target.Unit.Descriptor.IsUndead;
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Select an undead enemy.";
    }
    [TypeId("83246bfe26f94af5a057451e09d5d64b")]
    public sealed class PsychicSmiteApply:ContextAction
    {
        public BlueprintBuffReference Mark,NativeSmite;
        public override string GetCaption()=>"Apply an undead smite using Wisdom or Charisma";
        public override void RunAction(){
            var caster=Context.MaybeCaster;var target=Target.Unit;if(caster==null||target==null||!target.Descriptor.IsUndead)return;
            foreach(var old in target.Buffs.Enumerable.Where(b=>(b.Blueprint==NativeSmite.Get()||b.Blueprint==Mark.Get())&&b.Context?.MaybeCaster==caster).ToArray())old.Remove();
            Context[AbilitySharedValue.StatBonus]=Math.Max(0,Math.Max(caster.Stats.Wisdom.Bonus,caster.Stats.Charisma.Bonus));Context[AbilitySharedValue.DamageBonus]=Math.Min(20,Context.Params.CasterLevel);
            var apply=Apply(Mark.Get(),Math.Min(20,Context.Params.CasterLevel));apply.RunAction();
        }
    }
    [TypeId("d4ff6b4b02224848a8096af9db11b0ac")]
    public sealed class PsychicSmiteExclusion:UnitBuffComponentDelegate,IUnitBuffHandler,ISubscriber
    {
        public BlueprintBuffReference NativeSmite;
        public void HandleBuffDidAdded(Buff added){if(added.Owner==Owner&&added.Blueprint==NativeSmite.Get()&&added.Context?.MaybeCaster==Context.MaybeCaster)Buff.Remove();}
        public void HandleBuffDidRemoved(Buff removed){}
    }
}
