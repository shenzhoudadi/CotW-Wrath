using System;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicVoidOrb {
  internal static void Build(PsychicAssets f){
   const string text="Create a 2-foot-radius sphere of negative energy within close range for 1 round per caster level. It affects the first creature entering or occupying its space each round: living creatures gain one temporary negative level for 24 hours, Fortitude negates; undead gain 2d4 times 5 temporary hit points for 1 hour. Spell resistance applies. As a move action, reposition the sphere while it has not affected a creature this round. Movement retains the original casting parameters and remaining duration. Temporary hit points from repeated applications do not stack. Consumes two diamond dust.";
   var icon=PsychicOriginalIcons.Load("Metamixing");var host=f.Buff("OrbOfTheVoidController","Orb of the Void",text,new PsychicOrbController());Set(host,"m_Icon",icon);
   var hp=f.Buff("OrbOfTheVoidUndeadBuff","Orb of the Void: Temporary Hit Points",text,new ContextCalculateSharedValue {ValueType=AbilitySharedValue.Damage,Value=new ContextDiceValue {DiceType=DiceType.D4,DiceCountValue=2,BonusValue=0},Modifier=5},new TemporaryHitPointsFromAbilityValue {Value=new ContextValue {ValueType=ContextValueType.Shared,ValueShared=AbilitySharedValue.Damage},RemoveWhenHitPointsEnd=true});Set(hp,"m_Icon",icon);
   var drain=new ContextActionDealDamage {EnergyDrainType=EnergyDrainType.Temporary,Duration=new ContextDurationValue {Rate=DurationRate.Days,DiceCountValue=0,BonusValue=1},Value=new ContextDiceValue {DiceType=DiceType.Zero,DiceCountValue=0,BonusValue=1},IgnoreCritical=true,DisableSneakDamage=true};SetEnum(drain,"m_Type","EnergyDrain");Set(drain.Duration,"m_IsExtendable",false);
   var applyHP=Apply(hp,1,DurationRate.Hours);applyHP.NotLinkToAreaEffect=true;Set(applyHP.DurationValue,"m_IsExtendable",false);
   var effect=new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{new PsychicVoidUndead()}},IfTrue=Actions(applyHP),IfFalse=Actions(new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{drain}))})};
   var pulse=new PsychicOrbPulse {Controller=Ref<BlueprintBuffReference>(host),Effects=Actions(effect)};var unique=new UniqueAreaEffect();Set(unique,"m_Feature",Ref<BlueprintUnitFactReference>(host));
   var area=PsychicWeatherSpells.Area(f,"OrbOfTheVoid","Orb of the Void",text,new PrefabLink {AssetId="af6fac67b4ed1b04dae392e92cb228cd"},AreaEffectShape.Cylinder,2,true,unique,new AbilityAreaEffectRunAction {UnitEnter=Actions(pulse),Round=Actions(pulse),UnitMove=Actions(),UnitExit=Actions()});Set(area,"m_TickRoundAfterSpawn",true);
   var place=new PsychicOrbPlace {Controller=Ref<BlueprintBuffReference>(host),Area=Ref<BlueprintAbilityAreaEffectReference>(area)};
   var move=f.Ability("OrbOfTheVoidMove","Orb of the Void: Move",text,AbilityType.Supernatural,UnitCommand.CommandType.Move,AbilityRange.Close,new PsychicOrbReady {Controller=Ref<BlueprintBuffReference>(host)},Run(place));move.CanTargetSelf=move.CanTargetFriends=move.CanTargetEnemies=move.CanTargetPoint=true;move.SpellResistance=false;Set(move,"m_Icon",icon);move.LocalizedDuration=Text("OrbOfTheVoidMove","duration","Remaining spell duration");move.LocalizedSavingThrow=Text("OrbOfTheVoidMove","savingThrow","");host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)Facts(move)}).ToArray();
   var applyHost=Apply(host,0,toCaster:true);applyHost.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");
   var a=f.Ability("OrbOfTheVoid","Orb of the Void",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Close,new SpellComponent {School=SpellSchool.Necromancy},rank,Run(applyHost,place));Set(a,"m_Icon",icon);a.CanTargetSelf=a.CanTargetFriends=a.CanTargetEnemies=a.CanTargetPoint=true;a.SpellResistance=true;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Reach|Metamagic.Quicken|Metamagic.Persistent|Metamagic.Piercing;a.LocalizedDuration=Text("OrbOfTheVoid","duration","1 round per caster level; negative level 24 hours; temporary hit points 1 hour");a.LocalizedSavingThrow=Text("OrbOfTheVoid","savingThrow","Fortitude negates (living creatures)");a.MaterialComponent=new BlueprintAbility.MaterialComponentData {Count=2};Set(a.MaterialComponent,"m_Item",Ref<BlueprintItemReference>(f.Native<Kingmaker.Blueprints.Items.BlueprintItem>("92752bbbf04dfa1439af186f48aee0e9")));
   foreach(var pair in f.Assets.Where(x=>x.Key.StartsWith("OrbOfTheVoid")))PsychicRepairComponents.UniqueNames(pair.Value);
  }
 }
 [TypeId("6f2a2d9b44e345a1b3ef23f8c404e62c")]
 public sealed class PsychicVoidUndead:ContextCondition {
  protected override string GetConditionCaption()=>"Undead creature";
  protected override bool CheckCondition()=>Target.Unit?.Descriptor.IsUndead==true;
 }
}
