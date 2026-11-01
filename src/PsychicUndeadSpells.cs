using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
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
    // CotW's native-class spells: deliberately not added to the Psychic list.
    internal static class PsychicUndeadSpells
    {
        internal static void Build(PsychicAssets f)
        {
            const string haltText="Undead enemies in a 10-foot burst within medium range are held immobile for 1 round per caster level. Will negates; spell resistance applies. A held creature receives another Will save each round. Attacking it or dealing damage ends the effect. This follows the Call of the Wild area adaptation, rather than selecting three individual targets.";
            var halt=f.Clone(f.Native<BlueprintBuff>("11cb2fe4fe9c44b448cfe1788ae1ab59"),"HaltUndeadBuff","Halt Undead",haltText);
            halt.GetComponent<SpellDescriptorComponent>().Descriptor=SpellDescriptor.MovementImpairing;
            // Undead have condition-level paralysis immunity as well as descriptor
            // immunity. Removing the spell tag alone leaves a cosmetic hold buff.
            // Native components own counted restrictions and balanced cleanup;
            // retain the repeated-save component, but do not remove racial immunity.
            halt.GetComponent<Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition>().Condition=Kingmaker.UnitLogic.UnitCondition.CantAct;
            halt.ComponentsArray=halt.ComponentsArray.Concat(new BlueprintComponent[]{
                new AddCondition {name="HaltUndeadNoMovement",Condition=Kingmaker.UnitLogic.UnitCondition.CantMove},
                new AddCondition {name="HaltUndeadHelpless",Condition=Kingmaker.UnitLogic.UnitCondition.Helpless},
                new AddIncomingDamageTrigger {name="HaltUndeadDamageEndsEffect",Actions=Actions(new ContextActionRemoveSelf()),CheckDamageDealt=true,CompareType=CompareOperation.Type.Greater,TargetValue=0,TriggerOnStatDamageOrEnergyDrain=true},
                new AddTargetAttackRollTrigger {name="HaltUndeadAttackEndsEffect",OnlyHit=false,Categories=Array.Empty<Kingmaker.Enums.WeaponCategory>(),ActionsOnAttacker=Actions(),ActionOnSelf=Actions(new ContextActionRemoveSelf())}
            }).ToArray();
            Set(halt,"m_Icon",PsychicOriginalIcons.Load("HaltUndead"));SetEnum(halt,"m_Flags","Harmful");
            var undead=f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33");
            var undeadCondition=new ContextConditionHasFact();Set(undeadCondition,"m_Fact",Ref<BlueprintUnitFactReference>(undead));
            var around=new AbilityTargetsAround {name="HaltUndeadBurst"};Set(around,"m_Radius",new Feet(10));Set(around,"m_TargetType",TargetType.Enemy);Set(around,"m_Condition",new ConditionsChecker {Conditions=new Condition[]{undeadCondition}});
            var spell=Spell(f,"HaltUndead","Halt Undead",haltText,halt,AbilityRange.Medium,DurationRate.Rounds,"1 round per caster level","eb0e36f1de0c05347963262d56d90cf5",around);
            spell.CanTargetPoint=true;spell.CanTargetSelf=false;

            const string controlText="Control one nonsummoned undead creature within close range for 1 minute per caster level. Will negates; spell resistance applies. The creature fights for your side under the normal domination AI, and returns to its original allegiance when the effect ends. This is not a mind-affecting effect. This follows the Call of the Wild single-target adaptation.";
            var control=f.Clone(f.Native<BlueprintBuff>("c0f4e1c24c9cd334ca988ed1bd9d201f"),"ControlUndeadBuff","Control Undead",controlText);
            // The native faction component owns restoration and save data. Do
            // not copy Wrath Dominate Person's repeated save or mental tags.
            control.ComponentsArray=control.ComponentsArray.OfType<ChangeFaction>().Cast<BlueprintComponent>().ToArray();
            if(control.ComponentsArray.Length!=1)throw new InvalidOperationException("Unexpected native domination components.");
            Set(control,"m_Icon",PsychicOriginalIcons.Load("ControlUndead"));SetEnum(control,"m_Flags","Harmful");
            var target=new AbilityTargetHasFact {name="ControlUndeadOnlyUndead"};Set(target,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(undead)});
            var summon=new AbilityTargetHasFact {name="ControlUndeadExcludeSummons",Inverted=true};Set(summon,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(f.Native<BlueprintBuff>("8728e884eeaa8b047be04197ecf1a0e4"))});
            Spell(f,"ControlUndead","Control Undead",controlText,control,AbilityRange.Close,DurationRate.Minutes,"1 minute per caster level","09f795c3900b21b47a1254bcb3f263c8",target,summon);
        }
        private static BlueprintAbility Spell(PsychicAssets f,string token,string name,string text,BlueprintBuff buff,AbilityRange range,DurationRate rate,string duration,string fx,params BlueprintComponent[] extra)
        {
            var apply=Apply(buff,0,rate);apply.AsChild=false;apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
            var rank=new ContextRankConfig {name=token+"Duration"};SetEnum(rank,"m_BaseValueType","CasterLevel");
            var parts=new BlueprintComponent[]{new SpellComponent {name=token+"School",School=SpellSchool.Necromancy},rank,
                Save(SavingThrowType.Will,Saved(Array.Empty<GameAction>(),new GameAction[]{apply})),
                new AbilitySpawnFx {name=token+"Visual",PrefabLink=new PrefabLink {AssetId=fx},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None}};
            var spell=f.Ability(token,name,text,AbilityType.Spell,UnitCommand.CommandType.Standard,range,parts.Concat(extra).ToArray());
            spell.CanTargetSelf=false;spell.CanTargetFriends=false;spell.CanTargetEnemies=true;spell.SpellResistance=true;
            spell.AvailableMetamagic=Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Extend|Metamagic.Persistent|Metamagic.Piercing;
            Set(spell,"m_Icon",buff.Icon);spell.LocalizedDuration=Text(token,"duration",duration);spell.LocalizedSavingThrow=Text(token,"savingThrow","Will negates");
            return spell;
        }
    }
}
