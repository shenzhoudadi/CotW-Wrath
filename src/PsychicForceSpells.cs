using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
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
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicForceSpells
    {
        internal static IEnumerable<PsychicSpells.Entry> Build(PsychicAssets f)
        {
            var icon=f.Native<BlueprintAbility>("3cf0a759bc612264fb9b03aa2f90b24b").Icon;
            var dazed=f.Buff("TelekineticStormDazed","Telekinetic Storm: Dazed","Dazed by telekinetic force for 1 round.",
                new AddCondition {name="TelekineticDazeCondition",Condition=UnitCondition.Dazed},
                new SpellDescriptorComponent {name="TelekineticDazeDescriptor",Descriptor=SpellDescriptor.Daze});
            SetEnum(dazed,"m_Flags","Harmful");Set(dazed,"m_Icon",icon);
            foreach(bool storm in new[]{false,true}) {
                string token=storm?"TelekineticStorm":"BurstOfForce",title=storm?"Telekinetic Storm":"Burst of Force";
                int cap=storm?20:15;
                string text=storm?
                    "Unleash telekinetic force against enemies within 40 feet, dealing 1d6 force damage per caster level (maximum 20d6) and dazing and stunning them for 1 round. Fortitude halves the damage and negates both conditions. Allies and the caster are excluded.":
                    "A burst of telekinetic force deals 1d6 force damage per caster level (maximum 15d6) to all other creatures within 20 feet, including allies. Fortitude halves the damage. A creature that fails its Fortitude save must also succeed at a Reflex save or be knocked prone. The caster is excluded.";
                var rank=new ContextRankConfig {name=token+"DamageDice"};SetEnum(rank,"m_BaseValueType","CasterLevel");
                Set(rank,"m_UseMax",true);Set(rank,"m_Max",cap);Set(rank,"m_AffectedByIntensifiedMetamagic",true);
                var damage=new ContextActionDealDamage {name=token+"ForceDamage",IsAoE=true,HalfIfSaved=true,IgnoreCritical=true,
                    DamageType=new DamageTypeDescription {Type=DamageType.Force},
                    Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=new ContextValue {ValueType=ContextValueType.Rank},BonusValue=0}};
                GameAction[] failed=storm?new GameAction[]{Apply(dazed,1),Apply(f.Native<BlueprintBuff>("09d39b38bb7c6014394b6daced9bacd3"),1)}:
                    new GameAction[]{new ContextActionSavingThrow {name="BurstOfForceReflex",Type=SavingThrowType.Reflex,
                        Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{new ContextActionKnockdownTarget {name="BurstOfForceProne"}}))}};
                var run=Save(SavingThrowType.Fortitude,damage,Saved(Array.Empty<GameAction>(),failed));run.IgnoreCaster=true;
                var targets=new AbilityTargetsAround {name=token+"Targets"};Set(targets,"m_Radius",new Feet(storm?40:20));
                Set(targets,"m_TargetType",storm?TargetType.Enemy:TargetType.Any);Set(targets,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});
                var spell=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,
                    new SpellComponent {name=token+"School",School=SpellSchool.Evocation},
                    new SpellDescriptorComponent {name=token+"Force",Descriptor=SpellDescriptor.Force},rank,targets,run,
                    new AbilitySpawnFx {name=token+"CastVisual",PrefabLink=new PrefabLink {AssetId=storm?"5c60b61bfa134fa47a4628f2ae880e05":"859a6d74aedf5f349a470ab14afb47d3"},
                        Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.Caster,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
                spell.CanTargetSelf=true;spell.CanTargetEnemies=true;spell.CanTargetFriends=!storm;
                spell.EffectOnAlly=AbilityEffectOnUnit.Harmful;spell.EffectOnEnemy=AbilityEffectOnUnit.Harmful;spell.SpellResistance=true;
                spell.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Quicken|Metamagic.Heighten|Metamagic.Intensified|Metamagic.Persistent|Metamagic.Piercing|(storm?0:Metamagic.Selective);
                Set(spell,"m_Icon",icon);
                spell.LocalizedDuration=Text(token,"duration",storm?"Instantaneous; dazed and stunned for 1 round":"Instantaneous");
                spell.LocalizedSavingThrow=Text(token,"savingThrow",storm?"Fortitude partial":"Fortitude half; Reflex negates prone (see text)");
                yield return new PsychicSpells.Entry {Token=token,Level=storm?9:5,Ability=spell};
            }
        }
    }
}
