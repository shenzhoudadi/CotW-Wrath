using System;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
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
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Newtonsoft.Json;
using System.Collections.Generic;
namespace CallOfTheWild.Wrath.Prototype
{
 internal static class PsychicCataclysmSpells
 {
  internal static readonly string[] Tokens={"Suffocation","SuffocationMass","ControlConstruct","ScouringWinds"};
  static ContextValue Rank()=>new ContextValue {ValueType=ContextValueType.Rank};
  static ContextRankConfig Scaling(int cap=0){var r=new ContextRankConfig();SetEnum(r,"m_BaseValueType","CasterLevel");if(cap>0){Set(r,"m_UseMax",true);Set(r,"m_Max",cap);}return r;}
  static AbilityTargetsAround Around(int feet,TargetType target,params Condition[] conditions){var c=new AbilityTargetsAround();Set(c,"m_Radius",new Feet(feet));Set(c,"m_TargetType",target);Set(c,"m_Condition",new ConditionsChecker {Conditions=conditions});return c;}
  static BlueprintAbility Spell(PsychicAssets f,string token,string title,string text,UnityEngine.Sprite icon,SpellSchool school,SpellDescriptor descriptor,AbilityRange range,string duration,string save,params BlueprintComponent[] extra){
   var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,range,new BlueprintComponent[]{new SpellComponent {School=school},new SpellDescriptorComponent {Descriptor=descriptor}}.Concat(extra).ToArray());a.CanTargetSelf=false;a.CanTargetFriends=false;a.CanTargetEnemies=true;a.SpellResistance=true;a.EffectOnAlly=a.EffectOnEnemy=AbilityEffectOnUnit.Harmful;Set(a,"m_Icon",icon);a.AvailableMetamagic=Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Extend|Metamagic.Persistent|Metamagic.Piercing;a.LocalizedDuration=Text(token,"duration",duration);a.LocalizedSavingThrow=Text(token,"savingThrow",save);return a;
  }
  static AbilitySpawnFx Fx(string id)=>new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId=id},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
  internal static void Build(PsychicAssets f){Suffocate(f);Control(f);Winds(f);}
  static void Suffocate(PsychicAssets f){
   const string text="Extract the air from living creatures that must breathe. Fortitude success causes only 1 round of staggered. On failure, the subject is staggered and begins to suffocate. Starting on its next round, a failed Fortitude save advances one step: reduce hit points to 0, then to -1, then death. Successful later saves delay progression without ending the spell. The single-target spell lasts 3 rounds; the mass spell affects enemies in a 30-foot burst for 1 round per caster level. Undead, constructs and elementals are excluded. This is a death effect.";
   var excluded=new[]{Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("198fd8924dabcb5478d0f78bd453c586"))};
   var icon=f.Native<BlueprintAbility>("cc0aeb74b35cb7147bff6c53538bbc76").Icon;var host=f.Buff("SuffocationBuff","Suffocation",text,new AddCondition {Condition=UnitCondition.Staggered},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Death},new PsychicSuffocation());Set(host,"m_Icon",icon);SetEnum(host,"m_Flags","Harmful");
   for(int i=0;i<2;i++){var apply=Apply(host,3);if(i==1)apply.DurationValue.BonusValue=Rank();var safe=Apply(f.Native<BlueprintBuff>("df3950af5a783bd4d91ab73eb8fa0fd3"),1);Set(safe.DurationValue,"m_IsExtendable",false);var save=new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(Saved(new GameAction[]{safe},new GameAction[]{apply}))};var living=new PsychicBreathingTarget {Excluded=excluded};var guard=new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{living}},IfTrue=Actions(save),IfFalse=Actions()};var target=new AbilityTargetHasFact {Inverted=true};Set(target,"m_CheckedFacts",excluded);
    var a=Spell(f,i==0?"Suffocation":"SuffocationMass",i==0?"Suffocation":"Suffocation, Mass",text,icon,SpellSchool.Necromancy,SpellDescriptor.Death,AbilityRange.Close,i==0?"3 rounds":"1 round per caster level","Fortitude partial",Scaling(),Run(guard),Fx("cbfe312cb8e63e240a859efaad8e467c"));if(i==0)a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)target}).ToArray();else{a.CanTargetPoint=true;a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)Around(30,TargetType.Enemy,living)}).ToArray();}
   }
  }
  static void Control(PsychicAssets f){
   const string text="Control one construct within close range for 1 round per caster level. Each round, the caster must succeed at a Knowledge (Arcana) check against DC 10 + the construct's Hit Dice or control ends. Only one construct can be controlled by this spell per caster. The construct uses normal domination AI and its original allegiance returns when control ends. As in Call of the Wild, maintenance uses the automatic skill check rather than a separate concentration action. This is not mind-affecting; no saving throw or spell resistance applies.";
   var b=f.Clone(f.Native<BlueprintBuff>("c0f4e1c24c9cd334ca988ed1bd9d201f"),"ControlConstructBuff","Control Construct",text);b.ComponentsArray=b.ComponentsArray.OfType<ChangeFaction>().Cast<BlueprintComponent>().Concat(new BlueprintComponent[]{new UniqueBuff(),new PsychicConstructControl()}).ToArray();SetEnum(b,"m_Flags","Harmful");var apply=Apply(b,0);apply.DurationValue.BonusValue=Rank();var target=new AbilityTargetHasFact();Set(target,"m_CheckedFacts",new[]{Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f"))});var a=Spell(f,"ControlConstruct","Control Construct",text,b.Icon,SpellSchool.Transmutation,SpellDescriptor.None,AbilityRange.Close,"1 round per caster level","",Scaling(),Run(apply),target,Fx("09f795c3900b21b47a1254bcb3f263c8"));a.SpellResistance=false;a.AvailableMetamagic&=~(Metamagic.Persistent|Metamagic.Piercing);
  }
  static void Winds(PsychicAssets f){
   const string text="Create a movable 20-foot-radius sandstorm within medium range for 1 round per caster level. On entry and each round, creatures take 3d6 piercing damage (at most once per creature per round). Medium or smaller creatures must pass a DC10 Strength check to move; Small or smaller creatures also need DC15 Strength or fall prone and take2d6 bludgeoning damage. Air-subtype creatures ignore movement/visibility effects, but still take sand damage. The storm grants total fog concealment, blocks sight-based effects, slows movement, and prevents ordinary ranged weapon attacks into or out of it; rays are not automatically blocked. Move it with a move action, retaining remaining duration and original casting parameters. Repositioning does not sweep the path. No save or spell resistance applies; allies can be affected.";
   var icon=f.Native<BlueprintFeature>("f2fa7541f18b8af4896fbaf9f2a21dfe").Icon;var air=Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("dd3d0c7f4f57f304cbdbb68170b1b775"));var nonAir=new ContextConditionHasFact {Not=true};Set(nonAir,"m_Fact",air);
   var fog=PsychicWeatherSpells.OwnedBuff(f,"ScouringWindsFog","Scouring Winds",text,icon,PsychicWeatherSpells.Fog(true).Concat(new BlueprintComponent[]{new AddCondition {Condition=UnitCondition.Slowed},new SpellImmunityToSpellDescriptor {Descriptor=SpellDescriptor.Blindness|SpellDescriptor.SightBased},new BuffDescriptorImmunity {Descriptor=SpellDescriptor.SightBased}}).ToArray());foreach(var c in fog.GetComponents<SpellImmunityToSpellDescriptor>())Set(c,"m_CasterIgnoreImmunityFact",new BlueprintUnitFactReference());foreach(var c in fog.GetComponents<BuffDescriptorImmunity>())Set(c,"m_IgnoreFeature",new BlueprintUnitFactReference());var block=PsychicWeatherSpells.OwnedBuff(f,"ScouringWindsBlock","Scouring Winds",text,icon,new PsychicWeatherMiss {Incoming=true,Outgoing=true});var stuck=PsychicWeatherSpells.OwnedBuff(f,"ScouringWindsStuck","Scouring Winds: Cannot Move",text,icon,new AddCondition {Condition=UnitCondition.CantMove});
   var host=f.Buff("ScouringWindsController","Scouring Winds",text,new PsychicOrbController(),new PsychicScouringLedger());Set(host,"m_Icon",icon);var pulse=new PsychicScouringPulse {Controller=Ref<BlueprintBuffReference>(host),Air=air};var unique=new UniqueAreaEffect();Set(unique,"m_Feature",Ref<BlueprintUnitFactReference>(host));var run=new AbilityAreaEffectRunAction {UnitEnter=Actions(pulse),Round=Actions(pulse),UnitExit=Actions(),UnitMove=Actions()};
   var area=PsychicWeatherSpells.Area(f,"ScouringWinds","Scouring Winds",text,f.Native<BlueprintAbilityAreaEffect>("b21bc337e2beaa74b8823570cd45d6dd").Fx,AreaEffectShape.Cylinder,20,false,run,unique,PsychicWeatherSpells.Owned(fog,nonAir),PsychicWeatherSpells.Owned(block),PsychicWeatherSpells.Owned(stuck,new PsychicScouringMovement {Air=air}));Set(area,"m_TickRoundAfterSpawn",true);
   var place=new PsychicOrbPlace {Controller=Ref<BlueprintBuffReference>(host),Area=Ref<BlueprintAbilityAreaEffectReference>(area)};var ready=new PsychicOrbReady {Controller=Ref<BlueprintBuffReference>(host)};var move=Spell(f,"ScouringWindsMove","Scouring Winds: Move",text,icon,SpellSchool.Evocation,SpellDescriptor.None,AbilityRange.Medium,"Remaining spell duration","",ready,Run(place));move.Type=AbilityType.Supernatural;move.ActionType=UnitCommand.CommandType.Move;move.SpellResistance=false;move.AvailableMetamagic=0;move.CanTargetPoint=true;move.CanTargetFriends=move.CanTargetSelf=true;host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)Facts(move)}).ToArray();
   var apply=Apply(host,0);apply.ToCaster=true;apply.DurationValue.BonusValue=Rank();var spell=Spell(f,"ScouringWinds","Scouring Winds",text,icon,SpellSchool.Evocation,SpellDescriptor.None,AbilityRange.Medium,"1 round per caster level","",Scaling(),Run(apply,place));spell.CanTargetPoint=spell.CanTargetSelf=spell.CanTargetFriends=true;spell.SpellResistance=false;spell.AvailableMetamagic&=~(Metamagic.Persistent|Metamagic.Piercing);spell.AvailableMetamagic|=Metamagic.Empower|Metamagic.Maximize;foreach(var a in new[]{spell,move}){var radius=new AbilityAoERadius();Set(radius,"m_Radius",new Feet(20));SetEnum(radius,"m_TargetType","Any");a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)radius}).ToArray();}
  }
 }
 [TypeId("ba2d1928cd30408fb77acdfe84d2ff46")]
 public sealed class PsychicBreathingTarget:ContextCondition
 {
  public BlueprintUnitFactReference[] Excluded;protected override string GetConditionCaption()=>"Living target that breathes";
  protected override bool CheckCondition()=>Target.Unit!=null&&!Excluded.Any(x=>Target.Unit.Descriptor.HasFact(x.Get()));
 }
 public sealed class PsychicSuffocationState {[JsonProperty] public int Stage;[JsonProperty] public TimeSpan NextTick;[JsonProperty] public bool Initialized;}
 [TypeId("65a30f2f97bf411680d261ea76990895")]
 public sealed class PsychicSuffocation:UnitBuffComponentDelegate<PsychicSuffocationState>,ITickEachRound
 {
  protected override void OnActivate(){if(Data.Initialized)return;Data.Initialized=true;Data.NextTick=Kingmaker.Game.Instance.TimeController.GameTime+TimeSpan.FromSeconds(6);}
  public void OnNewRound(){var now=Kingmaker.Game.Instance.TimeController.GameTime;if(now<Data.NextTick||Owner.State.IsDead)return;Data.NextTick=now+TimeSpan.FromSeconds(6);using(Fact.MaybeContext.GetDataScope(Owner)){var save=Context.TriggerRule(new RuleSavingThrow(Owner,SavingThrowType.Fortitude,Context.Params.DC){PersistentSpell=(Context.Params.Metamagic&Metamagic.Persistent)!=0});if(save.IsPassed)return;Data.Stage++;if(Data.Stage>=3){new ContextActionKill().RunAction();return;}new PsychicSuffocationHP {Value=Data.Stage==1?0:-1}.RunAction();}}
 }
 [TypeId("afb24b9c57dc40f895d1724e53c406cc")]
 public sealed class PsychicSuffocationHP:ContextAction
 {
  public int Value;public override string GetCaption()=>"Suffocation: reduce HP without healing";
  public override void RunAction(){var u=Target.Unit;if(u==null)return;u.Stats.TemporaryHitPoints.RemoveAllModifiers();int amount=Math.Max(0,u.HPLeft-Value);if(amount==0)return;var damage=new DirectDamage(DiceFormula.Zero,amount){IgnoreReduction=true};var rule=new RuleDealDamage(Context.MaybeCaster,u,new DamageBundle(damage)){SourceAbility=Context.SourceAbility,MinHPAfterDamage=Value,IgnoreGrowthByDifficulty=true};Context.TriggerRule(rule);}
 }
 [TypeId("5e564fe5f87d4e22bdbdc028af2e0d15")]
 public sealed class PsychicConstructControl:UnitBuffComponentDelegate,ITickEachRound
 {
  public void OnNewRound(){if(Context.MaybeCaster==null||!Context.TriggerRule(new RuleSkillCheck(Context.MaybeCaster,StatType.SkillKnowledgeArcana,10+Owner.Progression.CharacterLevel){ShowAnyway=true}).Success)Buff.Remove();}
 }
 public sealed class PsychicScouringState {[JsonProperty] public Dictionary<string,TimeSpan> NextAttack=new Dictionary<string,TimeSpan>();}
 [TypeId("4b26cd77ef2b4de696766877cc3a06b3")]
 public sealed class PsychicScouringLedger:UnitBuffComponentDelegate<PsychicScouringState>
 {
  public bool Spend(UnitEntityData target){var now=Kingmaker.Game.Instance.TimeController.GameTime;if(Data.NextAttack.TryGetValue(target.UniqueId,out var next)&&now<next)return false;Data.NextAttack[target.UniqueId]=now+TimeSpan.FromSeconds(6);return true;}
 }
 [TypeId("dca14848df424244acf3f03dd9598f99")]
 public sealed class PsychicScouringPulse:ContextAction
 {
  public BlueprintBuffReference Controller;public BlueprintUnitFactReference Air;public override string GetCaption()=>"Sandstorm pulse with per-target round gate";
  public override void RunAction(){if(Target.Unit==null)return;var host=PsychicOrbController.Find(Context.MaybeCaster,Controller.Get());if(host==null){ContextData<AreaEffectContextData>.Current?.Entity?.ForceEnd();return;}bool hit=false;host.CallComponents<PsychicScouringLedger>(c=>hit=c.Spend(Target.Unit));if(!hit)return;Damage(3,PhysicalDamageForm.Piercing).RunAction();if(Target.Unit.State.Size<=Size.Small&&!Target.Unit.Descriptor.HasFact(Air.Get())&&!Context.TriggerRule(new RuleSkillCheck(Target.Unit,StatType.Strength,15)).Success){new ContextActionKnockdownTarget().RunAction();Damage(2,PhysicalDamageForm.Bludgeoning).RunAction();}}
  static ContextActionDealDamage Damage(int dice,PhysicalDamageForm type)=>new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Physical,Physical=new DamageTypeDescription.PhysicalData {Form=type}},Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=dice,BonusValue=0},IsAoE=true,IgnoreCritical=true,DisableSneakDamage=true};
 }
 [TypeId("f02f656a2404480ebd9797f3dfee9276")]
 public sealed class PsychicScouringMovement:ContextCondition
 {
  public BlueprintUnitFactReference Air;protected override string GetConditionCaption()=>"Medium or smaller fails wind Strength check";
  protected override bool CheckCondition()=>Target.Unit!=null&&Target.Unit.State.Size<=Size.Medium&&!Target.Unit.Descriptor.HasFact(Air.Get())&&!Context.TriggerRule(new RuleSkillCheck(Target.Unit,StatType.Strength,10)).Success;
 }
}
