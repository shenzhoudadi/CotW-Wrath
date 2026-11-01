using System;
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

using Kingmaker.UnitLogic.Buffs;
using Newtonsoft.Json;
namespace CallOfTheWild.Wrath.Prototype
{
 internal static class PsychicOrbSpells
 {
  internal static readonly string[] Tokens={"FlamingSphere","FlamingSphereGreater","AggressiveThundercloud","AggressiveThundercloudGreater","BallLightning"};
  internal static void Build(PsychicAssets f){for(int i=0;i<Tokens.Length;i++)Build(f,i);foreach(var pair in f.Assets.Where(p=>Tokens.Any(t=>p.Key.StartsWith(t))))PsychicRepairComponents.Name(pair.Value);}
  static void Build(PsychicAssets f,int index){
   string token=Tokens[index];bool fire=index<2,greater=index==1||index==3,thunder=index==2||index==3,ball=index==4;string title=index==0?"Flaming Sphere":index==1?"Flaming Sphere, Greater":index==2?"Aggressive Thundercloud":index==3?"Aggressive Thundercloud, Greater":"Ball Lightning";
   string text="Create a 2-foot-radius "+(fire?"sphere of fire":"electrical globe")+" within medium range for 1 round per caster level. Once per round it affects the first creature occupying or entering its space (allies can be affected); Reflex negates and spell resistance applies. As a move action, reposition it within medium range while it has not attacked this round. Repositioning retains the original caster level, DC, metamagic, and remaining duration. As in Call of the Wild, repositioning changes its location rather than damaging creatures along the path. "+(ball?"It deals 6d6 electricity damage, increasing to 9d6 at caster level 11, 12d6 at 15, and 15d6 at 19. Metal armor imposes a -4 penalty on the Reflex save.":"It deals "+(greater?"6d6":"3d6")+(fire?" fire":" electricity")+" damage.")+(thunder?" Creatures inside the cloud have 20% fog concealment.":"")+(index==1?" Failed saves also cause burning: 1d6 fire each round until a successful Reflex save against the spell's DC extinguishes it.":"")+(index==3?" The first creature to fail its Reflex save also attempts a Fortitude save or is stunned for 1 round; the stun is a sonic effect and can occur only once per casting.":"");
   var icon=fire?f.Native<BlueprintAbility>("f72f8f03bf0136c4180cd1d70eb773a5").Icon:thunder?f.Native<BlueprintAbility>("fc432e7a63f5a3545a93118af13bcb89").Icon:f.Native<BlueprintFeature>("cb2d9e6355dd33940b2bef49e544b0bf").Icon;
   var host=f.Buff(token+"Controller",title,text,new PsychicOrbController());Set(host,"m_Icon",icon);
   var dice=new ContextValue {ValueType=ContextValueType.Rank,ValueRank=AbilityRankType.DamageDice};
   var damage=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Energy,Energy=fire?DamageEnergyType.Fire:DamageEnergyType.Electricity},Value=new ContextDiceValue {DiceType=DiceType.D6,DiceCountValue=ball?dice:new ContextValue {Value=greater?6:3},BonusValue=0},IsAoE=true,IgnoreCritical=true,DisableSneakDamage=true};
   var effects=new System.Collections.Generic.List<GameAction>{damage};
   if(index==1){var burn=f.Clone(f.Native<BlueprintBuff>("e92ecaa76b5db674fa5b0aaff5b21bc9"),token+"Burning","Flaming Sphere: Burning",text);Set(burn,"m_Icon",icon);var apply=Apply(burn,0);apply.Permanent=true;apply.IsNotDispelable=true;apply.NotLinkToAreaEffect=true;effects.Add(apply);}
   if(index==3){var stun=f.Clone(f.Native<BlueprintBuff>("09d39b38bb7c6014394b6daced9bacd3"),token+"Stun","Aggressive Thundercloud: Stunned",text);stun.ComponentsArray=stun.ComponentsArray.Concat(new[]{(BlueprintComponent)new SpellDescriptorComponent {Descriptor=SpellDescriptor.Sonic}}).ToArray();Set(stun,"m_Icon",icon);effects.Add(new PsychicOrbFirstThunder {Controller=Ref<BlueprintBuffReference>(host),Stun=Ref<BlueprintBuffReference>(stun)});}
   var save=new ContextActionSavingThrow {Type=SavingThrowType.Reflex,Actions=Actions(Saved(Array.Empty<GameAction>(),effects.ToArray()))};
   if(ball){var metal=new PsychicMetalArmor {Druid=Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>("610d836f3a3a9ed42a4349b62f002e96"))};Set(save,"m_ConditionalDCIncrease",new[]{new ContextActionSavingThrow.ConditionalDCIncrease {Condition=new ConditionsChecker {Conditions=new Condition[]{metal}},Value=4}});}
   var pulse=new PsychicOrbPulse {Controller=Ref<BlueprintBuffReference>(host),Effects=Actions(save)};
   var run=new AbilityAreaEffectRunAction {UnitEnter=Actions(pulse),Round=Actions(pulse),UnitExit=Actions(),UnitMove=Actions()};var unique=new UniqueAreaEffect();Set(unique,"m_Feature",Ref<BlueprintUnitFactReference>(host));
   var descriptor=fire?SpellDescriptor.Fire:SpellDescriptor.Electricity;
   var components=new System.Collections.Generic.List<BlueprintComponent>{run,unique,new SpellDescriptorComponent {Descriptor=descriptor}};
   if(thunder){var fog=PsychicWeatherSpells.OwnedBuff(f,token+"Fog",title,text,icon,new AddConcealment {Descriptor=ConcealmentDescriptor.Fog,Concealment=Concealment.Partial});components.Add(PsychicWeatherSpells.Owned(fog));}
   if(ball){var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");SetEnum(rank,"m_Type","DamageDice");SetEnum(rank,"m_Progression","Custom");var rowType=typeof(ContextRankConfig).GetNestedType("CustomProgressionItem",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);var table=Array.CreateInstance(rowType,4);int[] bounds={10,14,18,int.MaxValue};for(int j=0;j<4;j++){var row=Activator.CreateInstance(rowType);Set(row,"BaseValue",bounds[j]);Set(row,"ProgressionValue",6+3*j);table.SetValue(row,j);}Set(rank,"m_CustomProgression",table);components.Add(rank);}
   var area=PsychicWeatherSpells.Area(f,token,title,text,new PrefabLink {AssetId=fire?"af6fac67b4ed1b04dae392e92cb228cd":"cfacbb7d39eaf624382c58bad8ba2df1"},AreaEffectShape.Cylinder,2,true,components.ToArray());Set(area,"m_TickRoundAfterSpawn",true);
   var place=new PsychicOrbPlace {Controller=Ref<BlueprintBuffReference>(host),Area=Ref<BlueprintAbilityAreaEffectReference>(area)};
   var move=f.Ability(token+"Move",title+": Move",text,AbilityType.Supernatural,UnitCommand.CommandType.Move,AbilityRange.Medium,new PsychicOrbReady {Controller=Ref<BlueprintBuffReference>(host)},Run(place));Set(move,"m_Icon",icon);move.CanTargetPoint=true;move.CanTargetFriends=move.CanTargetEnemies=move.CanTargetSelf=true;move.SpellResistance=false;move.AvailableMetamagic=0;move.LocalizedDuration=Text(token+"Move","duration","Remaining spell duration");move.LocalizedSavingThrow=Text(token+"Move","savingThrow","");host.ComponentsArray=host.ComponentsArray.Concat(new[]{(BlueprintComponent)Facts(move)}).ToArray();
   var applyHost=Apply(host,0);applyHost.ToCaster=true;applyHost.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
   var duration=new ContextRankConfig();SetEnum(duration,"m_BaseValueType","CasterLevel");
   var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Medium,new SpellComponent {School=SpellSchool.Evocation},new SpellDescriptorComponent {Descriptor=descriptor},duration,Run(applyHost,place));
   var radius=new AbilityAoERadius();Set(radius,"m_Radius",new Feet(2));SetEnum(radius,"m_TargetType","Any");a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)radius}).ToArray();a.CanTargetPoint=true;a.CanTargetSelf=a.CanTargetFriends=a.CanTargetEnemies=true;a.SpellResistance=true;a.EffectOnAlly=a.EffectOnEnemy=AbilityEffectOnUnit.Harmful;a.ShouldTurnToTarget=true;Set(a,"m_Icon",icon);a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach|Metamagic.Empower|Metamagic.Maximize|Metamagic.Persistent|Metamagic.Piercing;a.LocalizedDuration=Text(token,"duration","1 round per caster level");a.LocalizedSavingThrow=Text(token,"savingThrow",index==3?"Reflex negates; Fortitude negates stun":"Reflex negates");
  }
 }
 public sealed class PsychicOrbState
 {
  [JsonProperty] public TimeSpan NextAttackAt;
  [JsonProperty] public bool ThunderUsed;
 }
 [TypeId("da7558d811ec41cb9930481d0dd1bd12")]
 public sealed class PsychicOrbController:UnitBuffComponentDelegate<PsychicOrbState>
 {
  public bool Ready=>Kingmaker.Game.Instance.TimeController.GameTime>=Data.NextAttackAt;
  public bool SpendAttack(){if(!Ready)return false;Data.NextAttackAt=Kingmaker.Game.Instance.TimeController.GameTime+TimeSpan.FromSeconds(6);return true;}
  public bool SpendThunder(){if(Data.ThunderUsed)return false;Data.ThunderUsed=true;return true;}
  protected override void OnDeactivate(){
   // Match by host identity AND caster. Never remove another caster's copy.
   var state=Kingmaker.Game.Instance?.State;if(state?.AreaEffects==null)return;
   foreach(var area in state.AreaEffects.Where(a=>a.Context?.MaybeCaster==Owner&&a.Blueprint.GetComponent<UniqueAreaEffect>()?.Feature==Buff.Blueprint).ToArray())area.ForceEnd();
   var part=Owner.Get<UnitPartUniqueAreaEffects>();part?.Areas.RemoveAll(a=>a.Feature==Buff.Blueprint);
  }
  internal static Buff Find(UnitEntityData caster,BlueprintBuff bp)=>caster?.Buffs?.Enumerable.FirstOrDefault(b=>b.Blueprint==bp&&b.IsTurnedOn&&!b.IsSuppressed);
 }
 [TypeId("1ecf5379bc954fa19b26e49ab1716d64")]
 public sealed class PsychicOrbReady:BlueprintComponent,IAbilityCasterRestriction
 {
  public BlueprintBuffReference Controller;
  public bool IsCasterRestrictionPassed(UnitEntityData caster){var b=PsychicOrbController.Find(caster,Controller.Get());bool ready=false;b?.CallComponents<PsychicOrbController>(c=>ready=c.Ready);return ready;}
  public string GetAbilityCasterRestrictionUIText()=>"The spell must be active and must not have attacked this round.";
 }
 [TypeId("18a3ea9c7d484144ad84905389a527f7")]
 public sealed class PsychicOrbPlace:ContextAction
 {
  public BlueprintBuffReference Controller;public BlueprintAbilityAreaEffectReference Area;
  public override string GetCaption()=>"Reposition a unique globe with the original spell context";
  public override void RunAction(){
   var host=PsychicOrbController.Find(Context.MaybeCaster,Controller.Get());if(host==null)return;bool ready=false;host.CallComponents<PsychicOrbController>(c=>ready=c.Ready);if(!ready)return;
   var duration=new ContextDurationValue {Rate=DurationRate.Rounds,DiceCountValue=0,BonusValue=Math.Max(1,(int)Math.Ceiling(host.TimeLeft.TotalSeconds/6))};Set(duration,"m_IsExtendable",false);
   var spawn=new ContextActionSpawnAreaEffect {DurationValue=duration};Set(spawn,"m_AreaEffect",Area);using(host.Context.GetDataScope(Target))spawn.RunAction();
  }
 }
 [TypeId("8afa3561fa304d6b89a85e092697ca89")]
 public sealed class PsychicOrbPulse:ContextAction
 {
  public BlueprintBuffReference Controller;public ActionList Effects;
  public override string GetCaption()=>"One globe attack per round across all entries and repositioning";
  public override void RunAction(){var host=PsychicOrbController.Find(Context.MaybeCaster,Controller.Get());if(host==null){ContextData<AreaEffectContextData>.Current?.Entity?.ForceEnd();return;}bool ready=false;host.CallComponents<PsychicOrbController>(c=>ready=c.SpendAttack());if(ready)Effects.Run();}
 }
 [TypeId("6ab5946be75040fcb0fdc4f634fe7228")]
 public sealed class PsychicOrbFirstThunder:ContextAction
 {
  public BlueprintBuffReference Controller,Stun;
  public override string GetCaption()=>"First failed Reflex only: sonic stun with Fortitude save";
  public override void RunAction(){var host=PsychicOrbController.Find(Context.MaybeCaster,Controller.Get());bool first=false;host?.CallComponents<PsychicOrbController>(c=>first=c.SpendThunder());if(!first)return;var apply=Apply(Stun.Get(),1);Set(apply.DurationValue,"m_IsExtendable",false);new ContextActionSavingThrow {Type=SavingThrowType.Fortitude,Actions=Actions(Saved(Array.Empty<GameAction>(),new GameAction[]{apply}))}.RunAction();}
 }
}
