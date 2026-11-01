// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/GraveWarden.cs.
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.PubSubSystem;
using System.Runtime.CompilerServices;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class GraveWarden01445
    {
        internal const string ModuleName="Slayer archetype: Grave Warden",RootToken="GraveWardenArchetype01445",ClassId="c75e0971973957d4dbad24bc7957e4fb",UndeadId="734a29b693e9ec346ba2951b27987e33";
        internal static string T(string s)=>"GraveWarden"+s+"01445";
        internal static readonly string[] OwnTokens=new[]{RootToken,T("Resource"),T("HolyWaterFeature"),T("HolyWaterBuff"),T("HolyWaterToggle"),T("DeathWardFeature"),T("DeathWardAbility"),T("DustbringerFeature"),T("DustbringerAbility"),T("DustbringerMark"),T("DustbringerCooldown")};
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(ClassId);if(Existing(cache,RootToken,cls))return;Vacant(cache,OwnTokens);var undead=f.Native<BlueprintFeature>(UndeadId);
            var resource=f.Add(new BlueprintAbilityResource(),T("Resource"),"Holy Water Sprinkler","");var max=Get(resource,"m_MaxAmount");Set(max,"BaseValue",0);Set(max,"IncreasedByLevel",true);Set(max,"LevelIncrease",1);Set(max,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});Set(max,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(max,"m_ClassDiv",Array.Empty<BlueprintCharacterClassReference>());Set(max,"m_ArchetypesDiv",Array.Empty<BlueprintArchetypeReference>());Set(resource,"m_MaxAmount",max);
            const string holy="As a swift action, a grave warden can open a flask of holy water and pour it onto a held or adjacent melee weapon. If the weapon successfully hits an undead creature before the end of the grave warden’s next turn, the undead takes damage as if it took a direct hit from the holy water, taking 2d4 points of damage in addition to the damage from the weapon, if any.\nA grave warden can use this ability once per day per grave warden level.";
            var isUndead=new ContextConditionHasFact();Set(isUndead,"m_Fact",Ref<BlueprintUnitFactReference>(undead));var damage=new ContextActionDealDamage{DamageType=new DamageTypeDescription{Type=DamageType.Energy,Energy=DamageEnergyType.Holy},Value=new ContextDiceValue{DiceType=Kingmaker.RuleSystem.DiceType.D4,DiceCountValue=2,BonusValue=0},IgnoreCritical=true,DisableSneakDamage=true};
            var trigger=new GraveWardenHolyWater01445{Damage=Actions(new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{isUndead}},IfTrue=Actions(damage),IfFalse=Actions()})};
            var holyBuff=f.Buff(T("HolyWaterBuff"),"Holy Water Sprinkler",holy,trigger);Set(holyBuff,"m_Icon",PsychicOriginalIcons.Load("HolyWaterJet"));
            var spend=new ActivatableAbilityResourceLogic{SpendType=ActivatableAbilityResourceLogic.ResourceSpendType.NewRound};Set(spend,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));
            var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.WithUnitCommand,DeactivateIfCombatEnded=true,ComponentsArray=new BlueprintComponent[]{spend,new GraveWardenMelee01445()}},T("HolyWaterToggle"),"Holy Water Sprinkler",holy);Set(toggle,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Swift);Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(holyBuff));Set(toggle,"m_Icon",holyBuff.Icon);var holyFeature=f.Feature(T("HolyWaterFeature"),"Holy Water Sprinkler",holy,Facts(toggle),Grant(resource));Set(holyFeature,"m_Icon",holyBuff.Icon);
            const string wardText="At 7th level, a grave warden learns to perform a short ritual that grants the benefits of death ward, using his slayer level as his caster level. Performing this ritual requires a full-round action and consumes 4 uses of Holy Water Sprinkler ability. The grave warden can protect only himself with this ability.";
            var ward=f.Clone(f.Native<BlueprintAbility>("0413915f355a38146bc6ad40cdf27b3f"),T("DeathWardAbility"),"Death Ward",wardText);ward.Type=AbilityType.SpellLike;ward.ActionType=UnitCommand.CommandType.Standard;Set(ward,"m_IsFullRoundAction",true);ward.Range=AbilityRange.Personal;ward.CanTargetFriends=ward.CanTargetEnemies=ward.CanTargetPoint=false;ward.CanTargetSelf=true;ward.AvailableMetamagic=0;ward.MaterialComponent=new BlueprintAbility.MaterialComponentData();
            var param=new ContextCalculateAbilityParamsBasedOnClass{StatType=StatType.Intelligence};Set(param,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));ward.ComponentsArray=ward.ComponentsArray.Where(c=>!(c is Kingmaker.Blueprints.Classes.Spells.SpellListComponent)&&!(c is AbilityResourceLogic)&&!(c is ContextAbilityParamsCalculator)).Concat(new BlueprintComponent[]{param,Cost(resource,4)}).ToArray();foreach(var rank in ward.GetComponents<ContextRankConfig>()){SetEnum(rank,"m_BaseValueType","ClassLevel");Set(rank,"m_Class",new[]{Ref<BlueprintCharacterClassReference>(cls)});}
            var wardFeature=f.Feature(T("DeathWardFeature"),"Death Ward",wardText,Facts(ward));Set(wardFeature,"m_Icon",ward.Icon);
            const string dustText="At 10th level, a grave warden can slay a studied undead opponent. This functions as the assassinate slayer talent, except it requires a successful Will saving throw instead of a successful Fortitude save, affects only undead, and destroys the target instead of killing it. If the target succeeds at its saving throw, it becomes immune to that grave warden’s dustbringer ability for 24 hours.";
            var icon=f.Native<BlueprintAbility>("a9a52760290591844a96d0109e30e04d").Icon;var mark=f.Buff(T("DustbringerMark"),"Dustbringer Target",dustText);var cooldown=f.Buff(T("DustbringerCooldown"),"Dustbringer Target Cooldown",dustText);mark.Stacking=cooldown.Stacking=StackingType.Stack;Set(mark,"m_Icon",icon);Set(cooldown,"m_Icon",icon);
            var check=new AbilityTargetHasFact{FromCaster=true,Inverted=true};Set(check,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(mark),Ref<BlueprintUnitFactReference>(cooldown)});var targetType=new AbilityTargetHasFact();Set(targetType,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(undead)});
            var ability=f.Ability(T("DustbringerAbility"),"Dustbringer",dustText,AbilityType.Extraordinary,UnitCommand.CommandType.Standard,AbilityRange.Close,check,targetType,Run(Apply(mark,2,fromSpell:false)));ability.CanTargetFriends=false;ability.NotOffensive=true;Set(ability,"m_Icon",icon);
            var ownMark=new CourtBardImmunity01445{Buff=Ref<BlueprintBuffReference>(mark)};var ownCooldown=new CourtBardImmunity01445{Buff=Ref<BlueprintBuffReference>(cooldown),Not=true};var applyCooldown=Apply(cooldown,1,DurationRate.Days,fromSpell:false);applyCooldown.IsNotDispelable=true;var remove=new ContextActionRemoveBuff{OnlyFromCaster=true};Set(remove,"m_Buff",Ref<BlueprintBuffReference>(mark));
            var attempt=new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{ownMark,new ContextConditionIsFlatFooted()}},IfTrue=Actions(new GraveWardenSave01445{Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{new ContextActionKill()}))}),IfFalse=Actions()};
            var cleanup=new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{new CourtBardImmunity01445{Buff=Ref<BlueprintBuffReference>(mark)}}},IfTrue=Actions(new Conditional{ConditionsChecker=new ConditionsChecker{Conditions=new Condition[]{ownCooldown}},IfTrue=Actions(applyCooldown),IfFalse=Actions()},remove),IfFalse=Actions()};
            var dustParams=new ContextCalculateAbilityParamsBasedOnClass{StatType=StatType.Intelligence};Set(dustParams,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));
            var dust=f.Feature(T("DustbringerFeature"),"Dustbringer",dustText,Facts(ability),dustParams,new AddInitiatorAttackRollTrigger{OnlyHit=true,SneakAttack=true,Action=Actions(attempt)},new AddInitiatorAttackRollTrigger{OnlyHit=false,Action=Actions(cleanup)});Set(dust,"m_Icon",icon);
            var root=f.Add(new BlueprintArchetype{LocalizedName=Text(RootToken,"name","Grave Warden"),LocalizedDescription=Text(RootToken,"description","While paladins and inquisitors use their connection with the divine to fight undead hordes and other horrors of the night, a grave warden relies on knowledge, skill with weapons, and tenacity to put an end to these night-born terrors."),RemoveFeatures=new[]{Entry(2,f.Native<BlueprintFeature>("04430ad24988baa4daa0bcd4f1c7d118")),Entry(10,f.Native<BlueprintFeature>("913b9cf25c9536949b43a2651b7ffb66"))},AddFeatures=new[]{Entry(2,holyFeature),Entry(7,wardFeature),Entry(10,dust)}},RootToken,"Grave Warden","");Set(root,"m_ParentClass",cls);Publish(cache,f,OwnTokens,cls,root,new BlueprintFeatureBase[]{holyFeature,wardFeature,dust});
        }
    }
    [TypeId("49a6ff9b2ed9479b88e6550c98f947cb")]
    public sealed class GraveWardenMelee01445:ActivatableAbilityRestriction
    {protected override bool IsAvailable()=>Owner.Body.PrimaryHand.MaybeWeapon?.Blueprint.IsMelee==true;}
    [TypeId("9c895d48d3b444b98300a7b945872995")]
    public sealed class GraveWardenHolyWater01445:Kingmaker.UnitLogic.Buffs.Components.UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleAttackWithWeapon>,IInitiatorRulebookSubscriber
    {
        public ActionList Damage;
        static readonly ConditionalWeakTable<RuleAttackWithWeapon,object> Seen=new ConditionalWeakTable<RuleAttackWithWeapon,object>();
        public void OnEventAboutToTrigger(RuleAttackWithWeapon e){}
        public void OnEventDidTrigger(RuleAttackWithWeapon e){if(!Buff.IsTurnedOn||Buff.IsSuppressed||e.Initiator!=Owner||e.AttackRoll==null||e.AttackRoll.IsFake||!e.AttackRoll.IsHit||e.Weapon!=Owner.Body.PrimaryHand.MaybeWeapon||e.Weapon?.Blueprint.IsMelee!=true||Seen.TryGetValue(e,out _))return;Seen.Add(e,new object());Buff.RunActionInContext(Damage,e.Target);}
    }
    [TypeId("531b0c0ada214ea1a3ac189c49c2b9bc")]
    public sealed class GraveWardenSave01445:ContextAction
    {
        public ActionList Actions;
        public override string GetCaption()=>"Dustbringer Will save";
        internal int StudiedBonus()=>Target?.Unit?.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.AssetGuid.ToString()=="45548967b714e254aa83f23354f174b0"&&b.Context?.MaybeCaster==Context.MaybeCaster).Select(b=>Math.Max(0,b.Context[AbilitySharedValue.DamageBonus])).DefaultIfEmpty(0).Max()??0;
        public override void RunAction(){var save=new ContextActionSavingThrow{Type=SavingThrowType.Will,HasCustomDC=true,CustomDC=Context.Params.DC+StudiedBonus(),Actions=Actions};save.RunAction();}
    }
}
