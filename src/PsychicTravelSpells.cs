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

namespace CallOfTheWild.Wrath.Prototype
{
 internal static class PsychicTravelSpells
 {
  internal static readonly string[] Tokens={"AirWalk","AirWalkCommunal","OverlandFlight","WindWalk","EarthTremor"};
  static ContextValue Rank()=>new ContextValue {ValueType=ContextValueType.Rank};
  static ContextRankConfig Scaling(bool cap=false){var c=new ContextRankConfig();SetEnum(c,"m_BaseValueType","CasterLevel");if(cap){Set(c,"m_UseMax",true);Set(c,"m_Max",10);}return c;}
  static AbilitySpawnFx Fx()=>new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId="352469f228a3b1f4cb269c7ab0409b8e"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None};
  static void Display(BlueprintAbility a,string duration,string save=""){a.LocalizedDuration=Text(a.name,"duration",duration);a.LocalizedSavingThrow=Text(a.name,"savingThrow",save);}
  internal static BlueprintComponent[] Ground(){var flight=new AddMechanicsFeature {name="FlightAirborne"};SetEnum(flight,"m_Feature","Flying");var result=new BlueprintComponent[]{new SpellImmunityToSpellDescriptor {name="FlightGroundSpellImmunity",Descriptor=SpellDescriptor.Ground},new BuffDescriptorImmunity {name="FlightGroundBuffImmunity",Descriptor=SpellDescriptor.Ground},new AddConditionImmunity {name="FlightTerrainImmunity",Condition=UnitCondition.DifficultTerrain},flight};Set(result[0],"m_CasterIgnoreImmunityFact",new BlueprintUnitFactReference());Set(result[1],"m_IgnoreFeature",new BlueprintUnitFactReference());return result;}
  static BlueprintAbility BuffSpell(PsychicAssets f,string token,string title,string text,BlueprintBuff buff,DurationRate rate,AbilityRange range){var apply=Apply(buff,0,rate);apply.DurationValue.BonusValue=Rank();var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,range,Scaling(),new SpellComponent {School=SpellSchool.Transmutation},Run(apply),Fx());Set(a,"m_Icon",buff.Icon);a.CanTargetSelf=true;a.CanTargetFriends=true;a.CanTargetEnemies=false;a.SpellResistance=false;a.NotOffensive=true;a.EffectOnAlly=AbilityEffectOnUnit.Helpful;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;if(range==AbilityRange.Touch)a.AvailableMetamagic|=Metamagic.Reach;Display(a,rate==DurationRate.Hours?"1 hour per caster level":"10 minutes per caster level");return a;}
  internal static void Build(PsychicAssets f){
   const string air="Walk through the air for 10 minutes per caster level, gaining immunity to difficult terrain and ground-based effects, including pits. This uses the game's airborne mechanics; it does not enable free movement over map walls or gaps.";
   var icon=f.Native<BlueprintAbility>("f3c0b267dd17a2a45a40805e31fe3cd1").Icon;var buff=f.Buff("AirWalkBuff","Air Walk",air,Ground());Set(buff,"m_Icon",icon);
   BuffSpell(f,"AirWalk","Air Walk",air,buff,DurationRate.TenMinutes,AbilityRange.Touch);
   var communal=BuffSpell(f,"AirWalkCommunal","Air Walk, Communal","Grant Air Walk to allies within 30 feet. "+air,buff,DurationRate.TenMinutes,AbilityRange.Personal);var around=new AbilityTargetsAround();Set(around,"m_Radius",new Feet(30));SetEnum(around,"m_TargetType","Ally");Set(around,"m_Condition",new ConditionsChecker {Conditions=Array.Empty<Condition>()});communal.ComponentsArray=communal.ComponentsArray.Concat(new[]{(BlueprintComponent)around}).ToArray();
   const string fly="Fly for 1 hour per caster level. Gain +10 feet movement speed, immunity to difficult terrain and ground-based effects including pits, and +3 dodge AC against melee attacks from grounded creatures while you can move and retain your Dexterity bonus to AC. The flying AC bonus does not stack with native wings. This uses the game's airborne mechanics, not free movement over map walls or gaps.";
   var fb=f.Buff("OverlandFlightBuff","Overland Flight",fly,Ground().Concat(new BlueprintComponent[]{new AddStatBonus {name="FlightMovementBonus",Stat=StatType.Speed,Value=10,Descriptor=ModifierDescriptor.UntypedStackable},new PsychicFlightDefense {name="FlightMeleeDefense"}}).ToArray());Set(fb,"m_Icon",PsychicOriginalIcons.Load("Fly"));BuffSpell(f,"OverlandFlight","Overland Flight",fly,fb,DurationRate.Hours,AbilityRange.Personal);
   const string wind="For 1 hour per caster level, the party travels on the world map at a speed multiplier of at least 7 while a current party member has this effect. Multiple wind walks do not stack. This spell affects party world-map travel only; it does not change combat movement, army travel, encounters, or map access.";
   var wb=f.Buff("WindWalkBuff","Wind Walk",wind,new PsychicWindTravel());Set(wb,"m_Icon",f.Native<BlueprintFeature>("3c53ee4965a13d74e81b37ae34f0861b").Icon);BuffSpell(f,"WindWalk","Wind Walk",wind,wb,DurationRate.Hours,AbilityRange.Personal);
   Earth(f);
  }
  static void Earth(PsychicAssets f){
   const string text="Send an earth tremor in a 20-foot cone, 30-foot line, or 10-foot burst centered on you. Other grounded creatures take 1d4 bludgeoning damage per caster level (maximum 10d4), Reflex half; Medium or smaller creatures that fail fall prone. Rubble patches remain for 1 hour as difficult terrain. Airborne creatures ignore the ground effects. As in Call of the Wild's implementation, the game does not distinguish wood, metal, and earthen floor surfaces for this spell.";
   var icon=f.Native<BlueprintAbility>("29ccc62632178d344ad0be0865fd3113").Icon;
   var buff=PsychicWeatherSpells.OwnedBuff(f,"EarthTremorRubbleBuff","Earth Tremor: Rubble",text,icon,new AddCondition {Condition=UnitCondition.DifficultTerrain},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Ground});
   var area=PsychicWeatherSpells.Area(f,"EarthTremorRubble","Earth Tremor: Rubble",text,f.Native<BlueprintAbilityAreaEffect>("8b4ea698ae053c541beed4e050f32dc3").Fx,AreaEffectShape.Cylinder,5,false,PsychicWeatherSpells.Owned(buff,new PsychicGroundTarget()),new SpellDescriptorComponent {Descriptor=SpellDescriptor.Ground});
   var damage=new ContextActionDealDamage {DamageType=new DamageTypeDescription {Type=DamageType.Physical,Physical=new DamageTypeDescription.PhysicalData {Form=PhysicalDamageForm.Bludgeoning}},Value=new ContextDiceValue {DiceType=DiceType.D4,DiceCountValue=Rank(),BonusValue=0},HalfIfSaved=true,IsAoE=true,DisableSneakDamage=true,IgnoreCritical=true};
   var prone=new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{new PsychicTremorSmall()}},IfTrue=Actions(new ContextActionKnockdownTarget()),IfFalse=Actions()};
   var save=new Conditional {ConditionsChecker=new ConditionsChecker {Conditions=new Condition[]{new PsychicGroundTarget {ExcludeCaster=true}}},IfTrue=Actions(new ContextActionSavingThrow {Type=SavingThrowType.Reflex,Actions=Actions(damage,Saved(Array.Empty<GameAction>(),new GameAction[]{prone}))}),IfFalse=Actions()};
   var choices=new BlueprintAbility[3];string[] names={"Cone","Line","Burst"};
   for(int i=0;i<3;i++){
    var spawn=new PsychicTremorRubble {Area=Ref<BlueprintAbilityAreaEffectReference>(area),Shape=i};
    var a=f.Ability("EarthTremor"+names[i],"Earth Tremor: "+(i==0?"20-foot Cone":i==1?"30-foot Line":"10-foot Burst"),text,AbilityType.Spell,UnitCommand.CommandType.Standard,i==2?AbilityRange.Personal:AbilityRange.Projectile,new SpellComponent {School=SpellSchool.Transmutation},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Ground},Scaling(true),Run(save),new AdditionalAbilityEffectRunActionOnClickedTarget {Action=Actions(spawn)},new AbilitySpawnFx {PrefabLink=area.Fx,Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget,PositionAnchor=AbilitySpawnFxAnchor.None,OrientationAnchor=AbilitySpawnFxAnchor.None});
    if(i==2){var around=new AbilityTargetsAround();Set(around,"m_Radius",new Feet(10));SetEnum(around,"m_TargetType","Any");Set(around,"m_Condition",new ConditionsChecker {Conditions=new Condition[]{new PsychicGroundTarget {ExcludeCaster=true}}});a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)around}).ToArray();}
    else{var projectile=new AbilityDeliverProjectile {Type=i==0?AbilityProjectileType.Cone:AbilityProjectileType.Line};Set(projectile,"m_Length",new Feet(i==0?20:30));Set(projectile,"m_LineWidth",new Feet(5));Set(projectile,"m_Projectiles",new[]{Ref<BlueprintProjectileReference>(f.Native<BlueprintProjectile>("868f9126707bdc5428528dd492524d52"))});a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)projectile}).ToArray();}
    a.CanTargetSelf=i==2;a.CanTargetPoint=i!=2;a.CanTargetEnemies=true;a.CanTargetFriends=true;a.ShouldTurnToTarget=true;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Persistent;Set(a,"m_Icon",icon);Display(a,"Instantaneous; rubble 1 hour","Reflex partial");choices[i]=a;
   }
   var variants=new AbilityVariants();Set(variants,"m_Variants",choices.Select(Ref<BlueprintAbilityReference>).ToArray());var root=f.Ability("EarthTremor","Earth Tremor",text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Unlimited,new SpellComponent {School=SpellSchool.Transmutation},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Ground},variants,Fx());Set(root,"m_Icon",icon);root.AvailableMetamagic=choices[0].AvailableMetamagic;root.SpellResistance=false;Display(root,"Instantaneous; rubble 1 hour","Reflex partial");foreach(var a in choices)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(root));
  }
 }
 [TypeId("36f501a1f27d42799c8c6df595bd47c3")]
 public sealed class PsychicFlightDefense:UnitBuffComponentDelegate,ITargetRulebookHandler<RuleCalculateAC>,ISubscriber
 {
  public void OnEventAboutToTrigger(RuleCalculateAC e){
   if((e.AttackType!=AttackType.Melee&&e.AttackType!=AttackType.Touch)||e.Initiator.State.Features.Flying||!Owner.State.CanMove||e.IsTargetFlatFooted||Context.TriggerRule(new RuleCheckTargetFlatFooted(e.Initiator,Owner)).IsFlatFooted)return;
   // Native wings already supply their own +3 dodge bonus against melee.
   if(Owner.Buffs.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponents<Kingmaker.Designers.Mechanics.Facts.ACBonusAgainstAttacks>().Any(c=>c.AgainstMeleeOnly&&c.Descriptor==ModifierDescriptor.Dodge&&c.ArmorClassBonus==3)))return;
   e.AddModifier(3,Fact,ModifierDescriptor.Dodge);
  }
  public void OnEventDidTrigger(RuleCalculateAC e){}
 }
 [TypeId("a7b6bef0f84348e798b9795ca6ec3d17")]
 public sealed class PsychicWindTravel:BlueprintComponent {}
 [HarmonyLib.HarmonyPatch(typeof(Kingmaker.Assets.Controllers.GlobalMap.GlobalMapMovementController),"CalcPlayerSpeedModifiers")]
 internal static class PsychicWindTravelPatch
 {
  static void Postfix(ref float __result){var party=Kingmaker.Game.Instance?.Player?.PartyCharacters;if(party!=null&&party.Any(u=>u.Value?.Buffs?.Enumerable.Any(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<PsychicWindTravel>()!=null)==true))__result=Math.Max(__result,7f);}
 }
 [TypeId("dc53e13bc9a444a7ab90b8f07c07404b")]
 public sealed class PsychicGroundTarget:ContextCondition
 {
  public bool ExcludeCaster;
  protected override string GetConditionCaption()=>"Grounded target";
  protected override bool CheckCondition()=>Target.Unit!=null&&(!ExcludeCaster||Target.Unit!=Context.MaybeCaster)&&!Target.Unit.State.Features.Flying;
 }
 [TypeId("ffba2e0f19ba48b19adf5e2d85b380c8")]
 public sealed class PsychicTremorSmall:ContextCondition
 {
  protected override string GetConditionCaption()=>"Medium or smaller target";
  protected override bool CheckCondition()=>Target.Unit!=null&&Target.Unit.State.Size<=Size.Medium;
 }
 [TypeId("ad85b07fcb524e4197a5f5ae019589d4")]
 public sealed class PsychicTremorRubble:ContextAction
 {
  public BlueprintAbilityAreaEffectReference Area;public int Shape;
  public override string GetCaption()=>"Create source-owned earth rubble patches";
  public override void RunAction(){
   var caster=Context.MaybeCaster;if(caster==null)return;var origin=caster.Position;var target=Target.Point;
   double dx=target.x-origin.x,dz=target.z-origin.z,length=Math.Sqrt(dx*dx+dz*dz);if(length<0.001){dx=0;dz=1;}else{dx/=length;dz/=length;}
   float[,] offsets=Shape==0?new float[,]{{5,0},{10,5},{10,-5}}:Shape==1?new float[,]{{5,0},{15,0},{25,0}}:new float[,]{{5,5},{5,-5},{-5,5},{-5,-5}};
   for(int i=0;i<offsets.GetLength(0);i++){double forward=new Feet(offsets[i,0]).Meters,side=new Feet(offsets[i,1]).Meters;var point=Shape==2?new UnityEngine.Vector3(origin.x+(float)forward,origin.y,origin.z+(float)side):new UnityEngine.Vector3(origin.x+(float)(dx*forward-dz*side),origin.y,origin.z+(float)(dz*forward+dx*side));var spawn=new ContextActionSpawnAreaEffect {DurationValue=new ContextDurationValue {Rate=DurationRate.Hours,DiceCountValue=0,BonusValue=1}};Set(spawn,"m_AreaEffect",Area);using(Context.GetDataScope(new TargetWrapper(point)))spawn.RunAction();}
  }
 }
}
