using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Original CotW spells for native classes; not additions to the Psychic list.
    internal static class PsychicBattleSpells
    {
        internal static readonly string[] Tokens={"FlurryOfSnowballs","TouchOfBloodletting","FlameArrow","Rebuke","SheetLightning"};
        internal static void Build(PsychicAssets f) { Snow(f);Bloodletting(f);Arrows(f);Rebuke(f);Lightning(f); }
        static ContextValue Rank(AbilityRankType type=AbilityRankType.Default)=>new ContextValue {ValueType=ContextValueType.Rank,ValueRank=type};
        static ContextRankConfig Scaling(string name,int max=0,AbilityRankType type=AbilityRankType.Default,bool half=false) {
            var r=new ContextRankConfig {name=name};SetEnum(r,"m_BaseValueType","CasterLevel");Set(r,"m_Type",type);
            if(max>0){Set(r,"m_UseMax",true);Set(r,"m_Max",max);}if(half)SetEnum(r,"m_Progression","Div2");return r;
        }
        static AbilitySpawnFx Fx(string name,string id,AbilitySpawnFxAnchor anchor=AbilitySpawnFxAnchor.SelectedTarget)=>new AbilitySpawnFx {name=name,PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=anchor,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
        static ContextActionDealDamage Damage(DamageEnergyType type,DiceType die,ContextValue number,int bonus=0,bool area=false,bool saved=false,bool half=false)=>new ContextActionDealDamage {
            DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=type},Value=new ContextDiceValue {DiceType=die,DiceCountValue=number,BonusValue=bonus},IsAoE=area,HalfIfSaved=saved,Half=half,IgnoreCritical=true,DisableSneakDamage=true};
        static void Display(BlueprintAbility a,string duration,string save) {a.LocalizedDuration=Text(a.name,"duration",duration);a.LocalizedSavingThrow=Text(a.name,"savingThrow",save);}
        static AbilityTargetsAround Burst(string name,int feet,TargetType target) {
            var b=new AbilityTargetsAround {name=name};Set(b,"m_Radius",new Feet(feet));Set(b,"m_TargetType",target);Set(b,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});return b;
        }
        static Conditional If(Condition condition,GameAction[] yes,GameAction[] no)=>new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new[]{condition}},IfTrue=Actions(yes),IfFalse=Actions(no)};
        static AbilityTargetHasFact Living(PsychicAssets f) {
            var c=new AbilityTargetHasFact {name="LivingTarget",Inverted=true};Set(c,"m_CheckedFacts",new[]{
                Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("198fd8924dabcb5478d0f78bd453c586"))});return c;
        }
        static void Snow(PsychicAssets f) {
            var a=f.Clone(f.Native<BlueprintAbility>("e7c530f8137630f4d9d7ee1aa7b1edc0"),"FlurryOfSnowballs","Flurry of Snowballs","A flurry of snowballs strikes all creatures in a 30-foot cone for 4d6 cold damage. Reflex halves the damage. Spell resistance does not apply. Damage does not increase with caster level.");
            a.ComponentsArray=a.ComponentsArray.Where(c=>c is SpellComponent||c is AbilityDeliverProjectile||c is SpellDescriptorComponent).ToArray();
            var delivery=a.GetComponent<AbilityDeliverProjectile>();Set(delivery,"m_Length",new Feet(30));Set(delivery,"m_Projectiles",new[]{Ref<BlueprintProjectileReference>(f.Native<BlueprintProjectile>("72b45860bdfb81f4284aa005c04594dd"))});
            a.ComponentsArray=a.ComponentsArray.Concat(new BlueprintComponent[]{Save(SavingThrowType.Reflex,Damage(DamageEnergyType.Cold,DiceType.D6,4,area:true,saved:true))}).ToArray();
            a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Quicken|Metamagic.Heighten|Metamagic.Reach|Metamagic.Persistent|Metamagic.Selective;
            Display(a,"Instantaneous","Reflex half");
        }
        static void Bloodletting(PsychicAssets f) {
            const string text="A melee touch attack causes a wounded living creature to bleed for 1 point each round and become exhausted for 1 round per caster level. Will negates; spell resistance applies. Any healing ends both effects. Undead, constructs, elementals and creatures at full hit points cannot be targeted. A missed touch retains the charge. The wound follows normal bleed cleanup outside combat.";
            var buff=f.Clone(f.Native<BlueprintBuff>("75039846c3d85d940aa96c249b97e562"),"TouchOfBloodlettingBuff","Touch of Bloodletting",text);
            var exhausted=f.Clone(f.Native<BlueprintBuff>("46d1b9cc3d0fd36469a471b047d773a2"),"TouchOfBloodlettingExhaustedTemplate","Touch of Bloodletting",text);
            f.Assets.Remove("TouchOfBloodlettingExhaustedTemplate"); // Temporary deep clone, never registered.
            // Reuse native exhaustion components, without mutating their shared source.
            buff.ComponentsArray=buff.ComponentsArray.Concat(exhausted.ComponentsArray.Where(c=>!(c is SpellDescriptorComponent))).ToArray();
            buff.GetComponent<SpellDescriptorComponent>().Descriptor=SpellDescriptor.Bleed|SpellDescriptor.Exhausted;
            var round=buff.GetComponent<AddFactContextActions>();
            var tick=round?.NewRound?.Actions?.OfType<ContextActionDealDamage>().FirstOrDefault()
                ??throw new InvalidOperationException("Touch of Bloodletting requires the native bleed round damage on 75039846c3d85d940aa96c249b97e562; registration cancelled before publication.");
            round.NewRound.Actions=round.NewRound.Actions.Where(a=>!(a is ContextActionDealDamage)||ReferenceEquals(a,tick)).ToArray();
            tick.Value=new ContextDiceValue {DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=1};tick.IgnoreCritical=true;tick.DisableSneakDamage=true;
            buff.Stacking=StackingType.Replace;SetEnum(buff,"m_Flags","Harmful");var icon=f.Native<BlueprintAbility>("6cbb040023868574b992677885390f92").Icon;Set(buff,"m_Icon",icon);buff.FxOnStart=new PrefabLink {AssetId="cbfe312cb8e63e240a859efaad8e467c"};
            var apply=Apply(buff,0);apply.AsChild=false;apply.DurationValue.BonusValue=Rank();
            var touch=f.Clone(f.Native<BlueprintAbility>("3c17035ec4717674cae2e841a190e757"),"TouchOfBloodlettingTouch","Touch of Bloodletting",text);
            var deliver=new AbilityDeliverTouch {name="BloodlettingTouch"};Set(deliver,"m_TouchWeapon",Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>("bb337517547de1a4189518d404ec49d4")));
            touch.ComponentsArray=new BlueprintComponent[]{deliver,new SpellComponent {name="BloodlettingSchool",School=SpellSchool.Necromancy},new SpellDescriptorComponent {name="BloodlettingDescriptors",Descriptor=SpellDescriptor.Bleed|SpellDescriptor.Exhausted},Living(f),new PsychicWoundedTarget(),Scaling("BloodlettingDuration"),Save(SavingThrowType.Will,Saved(Array.Empty<GameAction>(),new GameAction[]{apply})),Fx("BloodlettingVisual","cbfe312cb8e63e240a859efaad8e467c")};
            var cast=f.Clone(f.Native<BlueprintAbility>("ab395d2335d3f384e99dddee8562978f"),"TouchOfBloodletting","Touch of Bloodletting",text);
            var sticky=new AbilityEffectStickyTouch {name="BloodlettingHeldCharge"};Set(sticky,"m_TouchDeliveryAbility",Ref<BlueprintAbilityReference>(touch));
            cast.ComponentsArray=new BlueprintComponent[]{sticky,new SpellComponent {name="BloodlettingSchool",School=SpellSchool.Necromancy},new SpellDescriptorComponent {name="BloodlettingDescriptors",Descriptor=SpellDescriptor.Bleed|SpellDescriptor.Exhausted},Living(f),new PsychicWoundedTarget()};
            foreach(var a in new[]{cast,touch}) {
                Set(a,"m_Parent",new BlueprintAbilityReference());Set(a,"m_Icon",icon);a.Range=AbilityRange.Touch;a.CanTargetSelf=false;a.CanTargetFriends=false;a.CanTargetEnemies=true;a.CanTargetPoint=false;a.SpellResistance=true;
                a.EffectOnEnemy=a.EffectOnAlly=AbilityEffectOnUnit.Harmful;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing;Display(a,"1 round per caster level","Will negates");
            }
        }
        static void Arrows(PsychicAssets f) {
            const string text="For 1 minute per caster level, the touched ally's successful attacks with bows, crossbows, slings, sling staves or javelins deal an additional 1d6 fire damage. This follows Call of the Wild's creature-buff adaptation, with no ammunition counter. The extra damage is not multiplied on a critical hit; repeated castings do not stack.";
            var categories=new[]{WeaponCategory.Longbow,WeaponCategory.Shortbow,WeaponCategory.LightCrossbow,WeaponCategory.HeavyCrossbow,WeaponCategory.LightRepeatingCrossbow,WeaponCategory.HeavyRepeatingCrossbow,WeaponCategory.HandCrossbow,WeaponCategory.Sling,WeaponCategory.SlingStaff,WeaponCategory.Javelin};
            var triggers=new BlueprintComponent[]{new PsychicFlameArrowDamage {name="FlameArrowWeaponDamage",Categories=categories}};
            var buff=f.Buff("FlameArrowBuff","Flame Arrow",text,triggers);Set(buff,"m_Icon",PsychicOriginalIcons.Load("FlameArrow"));buff.FxOnStart=new PrefabLink {AssetId="352469f228a3b1f4cb269c7ab0409b8e"};
            var apply=Apply(buff,0,DurationRate.Minutes);apply.AsChild=false;apply.DurationValue.BonusValue=Rank();
            var a=f.Ability("FlameArrow","Flame Arrow",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Touch,new SpellComponent {name="FlameArrowSchool",School=SpellSchool.Transmutation},Scaling("FlameArrowDuration"),Run(apply),Fx("FlameArrowVisual","352469f228a3b1f4cb269c7ab0409b8e"));
            a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=false;a.SpellResistance=false;Set(a,"m_Icon",buff.Icon);a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten|Metamagic.Reach;Display(a,"1 minute per caster level","");
        }
        static void Rebuke(PsychicAssets f) {
            const string text="Enemies within 20 feet take 1d8 damage per two caster levels (maximum 5d8) and are staggered for 1 round. Against enemies sharing your deity, damage is 1d6 per caster level (maximum 10d6) and they are stunned for 1d4 rounds instead. Half the damage is sonic and half divine. Fortitude halves damage and negates the condition; spell resistance applies. As in Call of the Wild, the sonic and divine halves are rolled separately.";
            var same=new PsychicSameDeity {Deities=Ref<BlueprintFeatureSelectionReference>(f.Native<BlueprintFeatureSelection>("59e7a76987fe3b547b9cce045f4db3e4"))};
            var stagger=Apply(f.Native<BlueprintBuff>("df3950af5a783bd4d91ab73eb8fa0fd3"),1);stagger.AsChild=false;
            var stun=Apply(f.Native<BlueprintBuff>("09d39b38bb7c6014394b6daced9bacd3"),0);stun.AsChild=false;stun.DurationValue.DiceType=DiceType.D4;stun.DurationValue.DiceCountValue=1;
            Func<bool,GameAction[]> effects=special=>new GameAction[]{Damage(DamageEnergyType.Sonic,special?DiceType.D6:DiceType.D8,Rank(special?AbilityRankType.DamageDice:AbilityRankType.Default),area:true,saved:true,half:true),Damage(DamageEnergyType.Divine,special?DiceType.D6:DiceType.D8,Rank(special?AbilityRankType.DamageDice:AbilityRankType.Default),area:true,saved:true,half:true),Saved(Array.Empty<GameAction>(),new GameAction[]{special?stun:stagger})};
            var a=f.Ability("Rebuke","Rebuke",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {name="RebukeSchool",School=SpellSchool.Evocation},new SpellDescriptorComponent {name="RebukeSonic",Descriptor=SpellDescriptor.Sonic},Scaling("RebukeNormalDamage",5,half:true),Scaling("RebukeSameDeityDamage",10,AbilityRankType.DamageDice),Burst("RebukeEnemies",20,TargetType.Enemy),Save(SavingThrowType.Fortitude,If(same,effects(true),effects(false))),Fx("RebukeVisual","2483780330931b64f97cbb6bb7cbd352"));
            a.CanTargetSelf=true;a.CanTargetFriends=false;a.CanTargetEnemies=false;a.SpellResistance=true;Set(a,"m_Icon",PsychicOriginalIcons.Load("Rebuke"));a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Quicken|Metamagic.Heighten|Metamagic.Persistent|Metamagic.Piercing;Display(a,"Instantaneous; staggered 1 round or stunned 1d4 rounds","Fortitude partial");
        }
        static void Lightning(PsychicAssets f) {
            const string text="A flash fills a 20-foot burst within medium range, dealing 1 electricity damage to every creature (no save). Living creatures must save against Fortitude or be dazed for 1 round; success leaves them dazzled for 1 round. Metal armor imposes a -2 penalty on this save. Spell resistance applies. The daze is not mind-affecting.";
            var daze=f.Clone(f.Native<BlueprintBuff>("9934fedff1b14994ea90205d189c8759"),"SheetLightningDazeBuff","Sheet Lightning: Dazed",text);
            daze.ComponentsArray=daze.ComponentsArray.Where(c=>!(c is SpellDescriptorComponent)).ToArray();daze.ComponentsArray=daze.ComponentsArray.Concat(new BlueprintComponent[]{new SpellDescriptorComponent {name="SheetLightningDaze",Descriptor=SpellDescriptor.Daze}}).ToArray();SetEnum(daze,"m_Flags","Harmful");
            var icon=f.Native<BlueprintAbility>("645558d63604747428d55f0dd3a4cb58").Icon;Set(daze,"m_Icon",icon);
            var yes=Apply(f.Native<BlueprintBuff>("df6d1025da07524429afbae248845ecc"),1);yes.AsChild=false;var no=Apply(daze,1);no.AsChild=false;
            var save=new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(Saved(new GameAction[]{yes},new GameAction[]{no}))};
            var metal=new PsychicMetalArmor {Druid=Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>("610d836f3a3a9ed42a4349b62f002e96"))};
            Set(save,"m_ConditionalDCIncrease",new[]{new ContextActionSavingThrow.ConditionalDCIncrease {Condition=new ConditionsChecker {Conditions=new Condition[]{metal}},Value=2}});
            var undead=new ContextConditionHasFact();Set(undead,"m_Fact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33")));
            var construct=new ContextConditionHasFact();Set(construct,"m_Fact",Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f")));
            var living=new Conditional {ConditionsChecker=new ConditionsChecker {Operation=Operation.Or,Conditions=new Condition[]{undead,construct}},IfTrue=Actions(),IfFalse=Actions(save)};
            var a=f.Ability("SheetLightning","Sheet Lightning",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Medium,new SpellComponent {name="SheetLightningSchool",School=SpellSchool.Evocation},new SpellDescriptorComponent {name="SheetLightningElectricity",Descriptor=SpellDescriptor.Electricity},Burst("SheetLightningBurst",20,TargetType.Any),Run(Damage(DamageEnergyType.Electricity,DiceType.Zero,0,1,area:true),living),Fx("SheetLightningVisual","86e72b9ddeef0324d9d23c48594d6b7d",AbilitySpawnFxAnchor.ClickedTarget));
            a.CanTargetPoint=true;a.CanTargetEnemies=true;a.CanTargetFriends=true;a.CanTargetSelf=true;a.SpellResistance=true;Set(a,"m_Icon",icon);a.AvailableMetamagic=Metamagic.Extend|Metamagic.Quicken|Metamagic.Heighten|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing|Metamagic.Selective;Display(a,"1 round","Fortitude partial; no save against damage");
        }
    }
    [AllowedOn(typeof(BlueprintAbility),false)]
    [TypeId("4aec2b5f05854aa896174f928674e8ba")]
    public sealed class PsychicWoundedTarget : BlueprintComponent,IAbilityTargetRestriction
    {
        public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>target?.Unit?.Stats!=null&&target.Unit.HPLeft<target.Unit.Stats.HitPoints.ModifiedValue;
        public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Target must be wounded.";
    }
    [TypeId("8bed0f1f633c4c99bc702d99711a5046")]
    public sealed class PsychicSameDeity : ContextCondition
    {
        public BlueprintFeatureSelectionReference Deities;
        protected override string GetConditionCaption()=>"Caster and target share a deity";
        protected override bool CheckCondition()=>Context.MaybeCaster!=null&&Target.Unit!=null&&Deities.Get().AllFeatures.Any(f=>f!=null&&f.AssetGuid.ToString()!="92c0d2da0a836ce418a267093c09ca54"&&Context.MaybeCaster.HasFact(f)&&Target.Unit.HasFact(f));
    }
    [TypeId("0f4ec5f7579c4c198dbdc82c7f3835ba")]
    public sealed class PsychicMetalArmor : ContextCondition
    {
        public BlueprintCharacterClassReference Druid;
        protected override string GetConditionCaption()=>"Target wears metal armor";
        protected override bool CheckCondition()=>Target.Unit?.Body?.Armor?.MaybeArmor?.Blueprint.GetComponents<EquipmentRestrictionClass>().Any(c=>c.Not&&c.Class==Druid.Get())==true;
    }
}
