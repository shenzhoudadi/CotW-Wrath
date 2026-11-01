using System;
using Kingmaker.PubSubSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
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
    internal static class PsychicTacticalSpells
    {
        internal static readonly string[] Tokens={"ForcefulStrike","AuraOfDoom","LitanyOfTruth"};
        static ContextValue Rank()=>new ContextValue {ValueType=ContextValueType.Rank};
        static ContextRankConfig Scaling(string name,int max=0){var r=new ContextRankConfig {name=name};SetEnum(r,"m_BaseValueType","CasterLevel");if(max>0){Set(r,"m_UseMax",true);Set(r,"m_Max",max);}return r;}
        static AbilitySpawnFx Fx(string id)=>new AbilitySpawnFx {name="TacticalSpellVisual",PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
        static void Display(BlueprintAbility a,string duration,string save=""){a.LocalizedDuration=Text(a.name,"duration",duration);a.LocalizedSavingThrow=Text(a.name,"savingThrow",save);}
        internal static void Build(PsychicAssets f){Strike(f);Doom(f);Truth(f);}
        static void Strike(PsychicAssets f){
            const string text="Charge your next successful melee weapon, unarmed or natural attack with force. That hit deals an additional 1d4 force damage per caster level (maximum 10d4). Fortitude halves this damage and negates a free bull rush with a bonus equal to caster level. The bull rush does not move you. As in Call of the Wild, this swift spell charges you for up to 1 minute or until that hit. Missed attacks do not discharge it; ranged attacks do not trigger it.";
            // Native weapon-triggered damage otherwise copies the attack roll's
            // reason and loses this spell's metamagic/source context.
            var damage=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Force},Value=new ContextDiceValue {DiceType=DiceType.D4,DiceCountValue=Rank(),BonusValue=0},HalfIfSaved=true,IgnoreCritical=true,DisableSneakDamage=true,SetFactAsReason=true};
            var save=new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(damage,Saved(Array.Empty<GameAction>(),new GameAction[]{new PsychicForcefulBullRush()}))};
            var trigger=new PsychicForcefulStrikeHit {name="ForcefulStrikeHit",OnlyHit=true,CheckWeaponRangeType=true,RangeType=WeaponRangeType.Melee,Action=Actions(save,new ContextActionRemoveSelf())};
            var buff=f.Buff("ForcefulStrikeBuff","Forceful Strike",text,Scaling("ForcefulStrikeDamage",10),trigger);Set(buff,"m_Icon",PsychicOriginalIcons.Load("ForcefulStrike"));
            var apply=Apply(buff,1,DurationRate.Minutes);apply.AsChild=false;
            var a=f.Ability("ForcefulStrike","Forceful Strike",text,AbilityType.Spell,UnitCommand.CommandType.Swift,AbilityRange.Personal,new SpellComponent {name="ForcefulStrikeSchool",School=SpellSchool.Evocation},new SpellDescriptorComponent {name="ForcefulStrikeForce",Descriptor=SpellDescriptor.Force},new AbilityCasterMainWeaponIsMelee {name="ForcefulStrikeMeleeRequired"},Scaling("ForcefulStrikeDamage",10),Run(apply),Fx("52d413df527f9fa4a8cf5391fd593edd"));
            a.CanTargetSelf=true;a.CanTargetFriends=false;a.CanTargetEnemies=false;a.SpellResistance=false;Set(a,"m_Icon",buff.Icon);a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Heighten|Metamagic.Persistent;Display(a,"1 minute or until discharged","Fortitude partial (on hit)");
        }
        static void Doom(PsychicAssets f){
            const string text="For 10 minutes per caster level, emit a 20-foot aura of horror. Enemies in the aura must succeed at a Will save or become shaken while within it. Spell resistance applies. A successful save protects the creature until it leaves and reenters; this does not roll a new save every round. Leaving or ending the aura removes only its own shaken effect. Fear, emotion and mind-affecting immunities apply.";
            var descriptors=SpellDescriptor.Fear|SpellDescriptor.Emotion|SpellDescriptor.MindAffecting|SpellDescriptor.Shaken;
            var debuff=f.Clone(f.Native<BlueprintBuff>("25ec6cb6ab1845c48a95f9c20b034220"),"AuraOfDoomShakenBuff","Aura of Doom: Shaken",text);debuff.Stacking=StackingType.Stack;debuff.GetComponent<SpellDescriptorComponent>().Descriptor=descriptors;Set(debuff,"m_Icon",PsychicOriginalIcons.Load("AuraOfDoom"));
            var owned=new AbilityAreaEffectBuff {name="DoomAreaOwnedShaken",Condition=new ConditionsChecker {Conditions=new Condition[]{new PsychicDoomFailedSave()}},CheckConditionEveryRound=false};Set(owned,"m_Buff",Ref<BlueprintBuffReference>(debuff));
            var area=f.Add(new BlueprintAbilityAreaEffect {Shape=AreaEffectShape.Cylinder,Size=new Feet(20),AffectEnemies=true,AggroEnemies=true,SpellResistance=true,Fx=new PrefabLink(),ComponentsArray=new BlueprintComponent[]{owned,new SpellDescriptorComponent {name="DoomAreaDescriptors",Descriptor=descriptors}}},"AuraOfDoomArea","Aura of Doom",text);SetEnum(area,"m_TargetType","Enemy");
            var add=new AddAreaEffect {name="DoomAttachedArea"};Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));
            var host=f.Buff("AuraOfDoomBuff","Aura of Doom",text,add);Set(host,"m_Icon",debuff.Icon);
            var apply=Apply(host,0,DurationRate.TenMinutes);apply.AsChild=false;apply.DurationValue.BonusValue=Rank();
            var a=f.Ability("AuraOfDoom","Aura of Doom",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {name="DoomSchool",School=SpellSchool.Necromancy},new SpellDescriptorComponent {name="DoomDescriptors",Descriptor=descriptors},Scaling("DoomDuration"),Run(apply),Fx("cbfe312cb8e63e240a859efaad8e467c"));
            a.CanTargetSelf=true;a.CanTargetFriends=false;a.CanTargetEnemies=false;a.SpellResistance=false;Set(a,"m_Icon",host.Icon);a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Persistent|Metamagic.Piercing;Display(a,"10 minutes per caster level","Will negates (aura)");
        }
        static void Truth(PsychicAssets f){
            const string text="For 1 round, the target cannot benefit from concealment and its illusion buffs are suppressed. This also suppresses illusion buffs added during the duration. Will negates; spell resistance applies. Suppressed buffs are not dispelled and resume when the litany ends if their own duration has not expired.";
            var suppress=new SuppressBuffs {name="TruthSuppressIllusions",Schools=new[]{SpellSchool.Illusion},Descriptor=SpellDescriptor.None};Set(suppress,"m_Buffs",Array.Empty<BlueprintBuffReference>());
            var buff=f.Buff("LitanyOfTruthBuff","Litany of Truth",text,new DoNotBenefitFromConcealment {name="TruthNoConcealment"},suppress);SetEnum(buff,"m_Flags","Harmful");Set(buff,"m_Icon",f.Native<BlueprintAbility>("b3da3fbee6a751d4197e446c7e852bcb").Icon);buff.FxOnStart=new PrefabLink {AssetId="3cf209e5299921349a1c159f35cfa369"};
            var apply=Apply(buff,1);apply.AsChild=false;
            var a=f.Ability("LitanyOfTruth","Litany of Truth",text,AbilityType.Spell,UnitCommand.CommandType.Swift,AbilityRange.Close,new SpellComponent {name="TruthSchool",School=SpellSchool.Divination},Save(SavingThrowType.Will,Saved(Array.Empty<GameAction>(),new GameAction[]{apply})),Fx("3cf209e5299921349a1c159f35cfa369"));
            a.CanTargetSelf=false;a.CanTargetFriends=false;a.CanTargetEnemies=true;a.SpellResistance=true;a.EffectOnEnemy=AbilityEffectOnUnit.Harmful;Set(a,"m_Icon",buff.Icon);a.AvailableMetamagic=Metamagic.Heighten|Metamagic.Reach|Metamagic.Persistent|Metamagic.Piercing;Display(a,"1 round","Will negates");
        }
    }
    [TypeId("d0f15942e9054bfdb04e0a2e441b2273")]
    public sealed class PsychicForcefulStrikeHit : AddInitiatorAttackWithWeaponTrigger,IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        public new void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public new void OnEventDidTrigger(RuleAttackWithWeapon evt) {
            var category=evt.Weapon?.Blueprint.Category;
            if(category==null || category==WeaponCategory.Touch || category==WeaponCategory.Ray)return;
            base.OnEventDidTrigger(evt);
        }
    }
    [TypeId("f022412131a3406da9aa4074772abbd4")]
    public sealed class PsychicForcefulBullRush : ContextAction
    {
        public override string GetCaption()=>"Forceful strike bull rush with caster-level bonus";
        public override void RunAction(){
            if(Context.MaybeCaster==null || Target.Unit==null)return;
            var rule=new RuleCombatManeuver(Context.MaybeCaster,Target.Unit,CombatManeuver.BullRush,null){IgnoreConcealment=true};
            rule.AddModifier(Context.Params.CasterLevel,null,ModifierDescriptor.UntypedStackable);
            Context.TriggerRule(rule);
        }
    }
    [TypeId("23873e3a9eba45258e506b56887626ad")]
    public sealed class PsychicDoomFailedSave : ContextCondition
    {
        protected override string GetConditionCaption()=>"Enemy fails aura's Will save";
        protected override bool CheckCondition(){
            var caster=Context.MaybeCaster;var target=Target.Unit;
            if(caster==null || target==null || !caster.IsEnemy(target))return false;
            bool Pass()=>Context.TriggerRule(new RuleSavingThrow(target,SavingThrowType.Will,Context.Params.DC){PersistentSpell=Context.HasMetamagic(Metamagic.Persistent)}).IsPassed;
            if(!Pass())return true;
            return Context.HasMetamagic(Metamagic.Persistent)&&!Pass();
        }
    }
}
