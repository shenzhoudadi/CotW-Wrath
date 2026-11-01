using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
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
using Kingmaker.UnitLogic.Parts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicCompulsionSpells
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            const string portentText="Ill fortune surrounds the target for 1 round per caster level. Whenever it deals damage, it must attempt a Will save (DC 10 + half your caster level + your highest Intelligence, Wisdom, or Charisma modifier). Failure halves that damage. There is no initial saving throw; spell resistance applies. This follows Call of the Wild: the save is made for each damage event, including each target of an area spell.";
            var portent=f.Buff("DebilitatingPortentBuff","Debilitating Portent",portentText,
                new PsychicPortentDamage {name="PortentDamageSave"},Descriptors("PortentMind"));
            Set(portent,"m_Icon",f.Native<BlueprintBuff>("e6f2fc5d73d88064583cb828801212f4").Icon);SetEnum(portent,"m_Flags","Harmful");
            var apply=Apply(portent,0);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var rank=new ContextRankConfig {name="PortentDuration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var spell=f.Ability("DebilitatingPortent","Debilitating Portent",portentText,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Medium,
                new SpellComponent {name="PortentSchool",School=SpellSchool.Enchantment},Descriptors("PortentMind"),rank,Run(apply),Fx("PortentVisual","c14a2f46018cb0e41bfeed61463510ff"));
            Setup(spell,portent,"1 round per caster level","");
            spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Piercing;
            yield return new PsychicSpells.Entry {Ability=spell,Level=4};

            const string danceText="A melee touch attack makes the target dance in place, unable to take other actions, for 1d4 + 1 rounds. It takes a -4 penalty to AC and a -10 penalty on Reflex saves, loses the AC bonuses of its held shield, and provokes attacks of opportunity each round. A successful Will save reduces the duration to 1 round. Spell resistance and mind-affecting compulsion immunities apply. A missed touch retains the charge.";
            var dance=f.Buff("IrresistibleDanceBuff","Irresistible Dance",danceText,
                new AddStatBonus {name="DanceAC",Stat=StatType.AC,Value=-4,Descriptor=ModifierDescriptor.UntypedStackable},
                new AddStatBonus {name="DanceReflex",Stat=StatType.SaveReflex,Value=-10,Descriptor=ModifierDescriptor.UntypedStackable},
                new AddCondition {name="DanceConfusion",Condition=UnitCondition.Confusion},
                new PsychicDanceControl {name="DanceInPlace"},new PsychicDanceShield {name="DanceShield"},
                new AddFactContextActions {name="DanceProvokes",Activated=Actions(),Deactivated=Actions(),Dispose=Actions(),NewRound=Actions(new ContextActionProvokeAttackOfOpportunity())},Descriptors("DanceMind"));
            Set(dance,"m_Icon",PsychicOriginalIcons.Load("IrresistibleDance"));SetEnum(dance,"m_Flags","Harmful");
            dance.FxOnStart=new PrefabLink {AssetId="602fa850c4a94d84eb8aa1bcc0d008c7"};
            var shortEffect=Apply(dance,1);shortEffect.AsChild=false;
            var fullEffect=Apply(dance,1);fullEffect.AsChild=false;fullEffect.DurationValue.DiceType=DiceType.D4;fullEffect.DurationValue.DiceCountValue=1;
            var touch=f.Clone(f.Native<BlueprintAbility>("3c17035ec4717674cae2e841a190e757"),"IrresistibleDanceTouch","Irresistible Dance",danceText);
            touch.Range=AbilityRange.Touch;
            var delivery=new AbilityDeliverTouch {name="DanceTouch"};Set(delivery,"m_TouchWeapon",Ref<BlueprintItemWeaponReference>(f.Native<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>("bb337517547de1a4189518d404ec49d4")));
            touch.ComponentsArray=touch.ComponentsArray.Where(c=>c is AbilityTargetHasFact || c.GetType().Name=="AbilityTargetHasNoFactUnless").Concat(new BlueprintComponent[]{
                delivery,new SpellComponent {name="DanceSchool",School=SpellSchool.Enchantment},Descriptors("DanceMind"),Fx("DanceVisual","602fa850c4a94d84eb8aa1bcc0d008c7"),
                Save(SavingThrowType.Will,Saved(new GameAction[]{shortEffect},new GameAction[]{fullEffect}))}).ToArray();
            var cast=f.Clone(f.Native<BlueprintAbility>("ab395d2335d3f384e99dddee8562978f"),"IrresistibleDance","Irresistible Dance",danceText);
            var sticky=new AbilityEffectStickyTouch {name="DanceHeldCharge"};Set(sticky,"m_TouchDeliveryAbility",Ref<BlueprintAbilityReference>(touch));
            cast.ComponentsArray=new BlueprintComponent[]{sticky,new SpellComponent {name="DanceSchool",School=SpellSchool.Enchantment},Descriptors("DanceMind")};
            foreach(var ability in new[]{cast,touch}) {
                Set(ability,"m_Parent",new BlueprintAbilityReference());Setup(ability,dance,"1d4 + 1 rounds","Will partial");
                ability.AvailableMetamagic=spell.AvailableMetamagic|Metamagic.Persistent|Metamagic.Empower|Metamagic.Maximize;
            }
            yield return new PsychicSpells.Entry {Ability=cast,Level=8};
        }
        static SpellDescriptorComponent Descriptors(string name)=>new SpellDescriptorComponent {name=name,Descriptor=SpellDescriptor.MindAffecting|SpellDescriptor.Compulsion};
        static AbilitySpawnFx Fx(string name,string id)=>new AbilitySpawnFx {name=name,PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
        static void Setup(BlueprintAbility ability,BlueprintBuff buff,string duration,string saving) {
            Set(ability,"m_Icon",buff.Icon);ability.CanTargetSelf=false;ability.CanTargetFriends=false;ability.CanTargetEnemies=true;ability.CanTargetPoint=false;
            ability.SpellResistance=true;ability.EffectOnEnemy=ability.EffectOnAlly=AbilityEffectOnUnit.Harmful;
            ability.LocalizedDuration=Text(ability.name,"duration",duration);ability.LocalizedSavingThrow=Text(ability.name,"savingThrow",saving);
        }
    }

    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("2a91d904ab484aa79dc6f0bc5751842a")]
    public sealed class PsychicPortentDamage : UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateDamage>,IUnitSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateDamage evt) {
            var context=Fact.MaybeContext;var caster=context?.MaybeCaster;
            if(evt.Initiator!=Owner || caster==null || !evt.DamageBundle.Any())return;
            var stats=caster.Stats;
            int dc=10+context.Params.CasterLevel/2+Math.Max(stats.Intelligence.Bonus,Math.Max(stats.Wisdom.Bonus,stats.Charisma.Bonus));
            if(Rulebook.Trigger(new RuleSavingThrow(Owner,SavingThrowType.Will,dc) {Reason=Fact}).IsPassed)return;
            foreach(var damage in evt.DamageBundle)damage.Durability*=0.5f;
        }
        public void OnEventDidTrigger(RuleCalculateDamage evt){}
    }

    // Native confusion already handles both combat modes, commands and saved state.
    // Its result event fires before constructing the next command. Keep that
    // lifecycle, overriding only this buff owner's selected confusion action.
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("b47c0c8b639d4c0092293cab2c1234b9")]
    public sealed class PsychicDanceControl : UnitFactComponentDelegate,IConfusionRollResultHandler,IGlobalSubscriber
    {
        protected override void OnTurnOn(){Dance();}
        public void HandleConfusionRollResult(UnitEntityData unit,ConfusionState state){if(unit==Owner)Dance();}
        void Dance(){var part=Owner.Ensure<UnitPartConfusion>();part.State=ConfusionState.DoNothing;part.RetainControl();part.Cmd?.Interrupt();part.Cmd=null;}
        protected override void OnTurnOff(){var part=Owner.Get<UnitPartConfusion>();if(part==null)return;part.Cmd?.Interrupt();part.Cmd=null;part.RoundStartTime=TimeSpan.Zero;part.ReleaseControl();}
    }

    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("40988566f2d54b2583e18ea225fbd581")]
    public sealed class PsychicDanceShield : UnitFactComponentDelegate,ITargetRulebookHandler<RuleCalculateAC>,IUnitSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateAC evt) {
            // Touch and brilliant energy already ignore the shield: do not
            // subtract it a second time. Spell-created shields without a held
            // shield are unaffected, as in CotW.
            if(evt.Target!=Owner || evt.AttackType.IsTouch() || evt.BrilliantEnergy!=null || !Owner.Body.SecondaryHand.HasShield)return;
            var ac=Owner.Stats.AC;
            int bonus=ac.GetDescriptorBonus(ModifierDescriptor.Shield)+ac.GetDescriptorBonus(ModifierDescriptor.ShieldEnhancement)+ac.GetDescriptorBonus(ModifierDescriptor.ShieldFocus);
            if(bonus>0)evt.AddModifier(-bonus,Fact,ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAC evt){}
    }
}
