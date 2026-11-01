using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicBloodSpells
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            const string mawText="Gain a secondary bite attack dealing 1d4 damage (Medium), using the normal natural-attack Strength and size rules, for 1 minute per caster level. While this spell lasts, confirmed critical hits with bite attacks cause 1d4 bleed damage each round. Healing ends the bleed. As a swift action, you can end the spell with Savage Maw: Roar, attempting an Intimidate check to demoralize enemies within 30 feet. The roar follows the native Dazzling Display demoralize rules, without its weapon or feat requirement.";
            var bite=new AddSecondaryAttacks {name="SavageMawSecondaryBite"};Set(bite,"m_Weapon",new[]{Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>("35dfad6517f401145af54111be04d6cf"))});
            var bleed=Apply(f.Native<BlueprintBuff>("5eb68bfe186d71a438d4f85579ce40c1"),0);bleed.Permanent=true;bleed.IsNotDispelable=true;bleed.AsChild=false;
            var critical=new AddInitiatorAttackWithWeaponTrigger {name="SavageMawCriticalBleed",OnlyHit=true,CriticalHit=true,CheckWeaponCategory=true,Category=WeaponCategory.Bite,Action=Actions(bleed)};
            var maw=f.Buff("SavageMawBuff","Savage Maw",mawText,bite,critical);Set(maw,"m_Icon",PsychicOriginalIcons.Load("SavageMaw"));SetEnum(maw,"m_Flags","RemoveOnRest");
            var roar=f.Clone(f.Native<BlueprintAbility>("5f3126d4120b2b244a95cb2ec23d69fb"),"SavageMawRoar","Savage Maw: Roar",mawText);
            // Whitelist the native demoralize action/area/FX. In particular, do
            // not retain Dazzling Display's Weapon Focus equipment restriction.
            roar.ComponentsArray=roar.ComponentsArray.Where(c=>c is AbilityEffectRunAction || c is AbilityTargetsAround || c is AbilitySpawnFx).ToArray();
            roar.Type=AbilityType.Extraordinary;roar.ActionType=UnitCommand.CommandType.Swift;Set(roar,"m_IsFullRoundAction",false);roar.AvailableMetamagic=0;roar.SpellResistance=false;
            Set(roar,"m_Icon",maw.Icon);roar.LocalizedDuration=Text("SavageMawRoar","duration","Demoralize duration depends on the Intimidate check");roar.LocalizedSavingThrow=Text("SavageMawRoar","savingThrow","");
            var required=new AbilityCasterHasFacts {name="SavageMawRoarRequiresMaw"};Set(required,"m_Facts",new[]{Ref<BlueprintUnitFactReference>(maw)});
            var end=new AbilityExecuteActionOnCast {name="SavageMawRoarEndsSpell",Conditions=new ConditionsChecker {Conditions=Array.Empty<Condition>()},Actions=Actions(Remove(maw,true))};
            roar.ComponentsArray=roar.ComponentsArray.Concat(new BlueprintComponent[]{required,end}).ToArray();
            maw.ComponentsArray=maw.ComponentsArray.Concat(new BlueprintComponent[]{Facts(roar)}).ToArray();
            yield return new PsychicSpells.Entry {Level=2,Ability=Spell(f,"SavageMaw","Savage Maw",mawText,maw,"352469f228a3b1f4cb269c7ab0409b8e")};

            const string armorText="For 1 minute per caster level, each damage event that deals at least 5 points of piercing or slashing damage to you gives your worn armor a +1 enhancement bonus, to a maximum of +5. This does not stack with an existing armor enhancement bonus; use the higher value. Count damage after reduction, and increase the bonus only once per damage event. Requires worn armor. Removing your armor suspends the armor bonus; another suit you equip receives your accumulated enhancement. Recasting resets the accumulated bonus.";
            var tiers=new BlueprintBuff[5];
            for(int i=0;i<5;i++) {
                string token="BloodArmorTier"+(i+1);
                var enchant=f.Clone(f.Native<BlueprintArmorEnchantment>("a9ea95c5e02f9b7468447bc1010fe152"),token+"Enchantment","Blood Armor +"+(i+1),armorText);
                Set(enchant,"m_EnchantName",Text(token,"enchantName","Blood Armor +"+(i+1)));Set(enchant,"m_Description",Text(token,"enchantDescription",armorText));Set(enchant,"m_EnchantmentCost",0);
                enchant.ComponentsArray=new BlueprintComponent[]{new ArmorEnhancementBonus {name=token+"Enhancement",EnhancementValue=i+1}};
                var worn=new PsychicBloodArmorEnchantment {name=token+"WornArmor"};Set(worn,"m_Enchantments",new[]{Ref<BlueprintItemEnchantmentReference>(enchant)});Set(worn,"m_ItemType",BuffEnchantArmor.ItemType.Armor);Set(worn,"m_Scaling",new BuffScaling {Modifier=int.MaxValue,StartingMod=1,Minimum=1});
                tiers[i]=f.Buff(token+"Buff","Blood Armor +"+(i+1),armorText,worn);Set(tiers[i],"m_Icon",PsychicOriginalIcons.Load("BloodArmor"));SetEnum(tiers[i],"m_Flags","HiddenInUi");
            }
            GameAction next=ApplyTier(tiers[0]);
            for(int i=0;i<5;i++) {
                var has=new ContextConditionHasFact();Set(has,"m_Fact",Ref<BlueprintUnitFactReference>(tiers[i]));
                next=new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{has}},IfTrue=i==4?Actions():Actions(Remove(tiers[i],false),ApplyTier(tiers[i+1])),IfFalse=Actions(next)};
            }
            var blood=f.Buff("BloodArmorBuff","Blood Armor",armorText,new PsychicBloodArmorDamage {name="BloodArmorPiercingSlashing",Actions=Actions(next)});
            Set(blood,"m_Icon",PsychicOriginalIcons.Load("BloodArmor"));
            var spell=Spell(f,"BloodArmor","Blood Armor",armorText,blood,"53c86872d2be80b48afc218af1b204d7");
            spell.ComponentsArray=spell.ComponentsArray.Concat(new BlueprintComponent[]{new PsychicBloodArmorRequirement {name="BloodArmorRequiresArmor"}}).ToArray();
            yield return new PsychicSpells.Entry {Level=2,Ability=spell};
        }
        static ContextActionApplyBuff ApplyTier(BlueprintBuff buff){var a=Apply(buff,0);a.Permanent=true;a.AsChild=true;a.IsNotDispelable=true;return a;}
        static ContextActionRemoveBuff Remove(BlueprintBuff buff,bool caster){var a=new ContextActionRemoveBuff {ToCaster=caster};Set(a,"m_Buff",Ref<BlueprintBuffReference>(buff));return a;}
        static BlueprintAbility Spell(PsychicAssets f,string token,string name,string text,BlueprintBuff buff,string fx)
        {
            var apply=Apply(buff,0,DurationRate.Minutes);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var rank=new ContextRankConfig {name=token+"Duration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var spell=f.Ability(token,name,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {name=token+"School",School=SpellSchool.Transmutation},rank,Run(apply),new AbilitySpawnFx {name=token+"Visual",PrefabLink=new PrefabLink {AssetId=fx},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
            Set(spell,"m_Icon",buff.Icon);spell.SpellResistance=false;spell.LocalizedDuration=Text(token,"duration","1 minute per caster level");spell.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;return spell;
        }
    }
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("a84cd5c7e2d34d3998edfc68725e5f39")]
    public sealed class PsychicBloodArmorDamage : UnitFactComponentDelegate,ITargetRulebookHandler<RuleDealDamage>
    {
        public ActionList Actions;
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if(evt.IsFake || evt.Target!=Owner || evt.ResultList==null || evt.Result<=0)return;
            int received=evt.ResultList.Where(d=>d.Source is PhysicalDamage p && (p.Form&(PhysicalDamageForm.Piercing|PhysicalDamageForm.Slashing))!=0).Sum(d=>Math.Max(0,d.FinalValue));
            if(received>=5 && evt.Result>=5)Fact.RunActionInContext(Actions,Owner);
        }
    }
    // Same native enchantment data/equipment-change handling, with lifetime tied
    // to TurnOn/TurnOff so suppression also releases temporary enchantments.
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("48cdbfc0307042559f03f39b2c7df119")]
    public sealed class PsychicBloodArmorEnchantment : BuffEnchantArmor
    {
        protected override void OnActivate() { }
        protected override void OnDeactivate() { }
        protected override void OnTurnOn()=>base.OnActivate();
        protected override void OnTurnOff()=>base.OnDeactivate();
    }
    [AllowedOn(typeof(BlueprintAbility),false)]
    [TypeId("f7cc4524b37643a3a80f2c7d62ce3292")]
    public sealed class PsychicBloodArmorRequirement : BlueprintComponent,IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(Kingmaker.EntitySystem.Entities.UnitEntityData caster)=>caster?.Body?.Armor?.HasArmor==true;
        public string GetAbilityCasterRestrictionUIText()=>"Requires worn armor";
    }
}
