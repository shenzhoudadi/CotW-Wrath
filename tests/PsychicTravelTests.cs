using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Components;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;

internal static class PsychicTravelTests
{
 static int count,saves,knocks;static bool pass,flat,mobile=true;static EntityFactsManager facts;static readonly List<object> damage=new List<object>();static readonly List<UnityEngine.Vector3> points=new List<UnityEngine.Vector3>();
 static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
 static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
 static void Check(bool ok,string why){if(!ok)throw new Exception("Support: "+why);count++;}
 static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicGroundTarget).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
 static EntityFactComponent Runtime(BlueprintComponent c,UnitFact f){var rt=(EntityFactComponent)AccessTools.Method(c.GetType(),"CreateRuntimeFactComponent").Invoke(c,null);rt.Setup(c);rt.RestoreOwnerLink(f);return rt;}
 static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
 static void Clear(){facts.List.Clear();Refresh();}
 static Buff Fact(BlueprintBuff bp,MechanicsContext ctx){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);f.RestoreOwnerLink(facts);return f;}
 static MechanicsContext Context(UnitEntityData a,BlueprintScriptableObject bp,int cl=10,Metamagic mm=0){var ctx=new MechanicsContext(a,a.Descriptor,bp,null,new TargetWrapper(a));ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=3,Metamagic=mm});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(ctx);Set(ctx,"m_Ranks",ranks);return ctx;}
 static ItemEntityWeapon Weapon(AttackType attack){var type=new BlueprintWeaponType();Set(type,"m_AttackType",attack);var bp=new BlueprintItemWeapon();var r=BlueprintReferenceBase.CreateTyped<BlueprintWeaponTypeReference>(type);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,type,null);Set(bp,"m_Type",r);var w=Empty<ItemEntityWeapon>();Set(w,"m_Blueprint",bp);return w;}

 internal static int Run(BlueprintsCache cache){
  var a=new Psychic070Tests.Caster(cache);var unit=a.Unit;facts=new EntityFactsManager(unit);Set(unit,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(unit,"<Buffs>k__BackingField",buffs);Set(unit.State.Features,"Flying",new CountableFlag());
  var h=new Harmony("CotW01319.TravelRules");void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicTravelTests),n));
  Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionKnockdownTarget),"RunAction"),nameof(Knock));Patch(AccessTools.Method(typeof(ContextActionSpawnAreaEffect),"RunAction"),nameof(Spawn));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"ImprovedEvasion"),nameof(False));Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanMove"),nameof(Mobile));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicTravelTests),nameof(Save)));PsychicRuleTestBoundary.Patch(h,typeof(RuleCheckTargetFlatFooted),AccessTools.Method(typeof(PsychicTravelTests),nameof(Flat)));
  try{Ownership(cache,unit,h);Metadata(cache,unit);Flight(cache,unit);Tremor(cache,unit);Map(cache,unit,h);}finally{h.UnpatchAll(h.Id);Clear();}
  PsychicExpansionTests.Inventory["travelSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" travel/ground checks; native durations, ground predicates, save/damage branches, source geometry, flight AC and real installed map postfix; scene and base-speed boundaries substituted.");return count;
 }
 static void Ownership(BlueprintsCache c,UnitEntityData u,Harmony h){
  var oldTime=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());
  h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),prefix:new HarmonyMethod(typeof(PsychicTravelTests),nameof(Time)));
  h.Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitDescriptor),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(PsychicTravelTests),nameof(Add)){priority=Priority.First});
  h.Patch(AccessTools.Method(typeof(Buff),"Remove"),prefix:new HarmonyMethod(typeof(PsychicTravelTests),nameof(Remove)));
  Set(u,"m_IsInGame",true);Set(u,"<HoldingState>k__BackingField",Empty<SceneEntitiesState>());
  var bp=Asset<BlueprintAbilityAreaEffect>(c,"EarthTremorRubbleArea");var ctx=Context(u,Asset<BlueprintAbility>(c,"EarthTremor"));var owned=bp.GetComponent<AbilityAreaEffectBuff>();
  var area=Empty<AreaEffectEntityData>();Set(area,"<UniqueId>k__BackingField","rubble-one");Set(area,"m_Context",ctx);Set(area,"m_Blueprint",bp);
  try{foreach(bool flying in new[]{false,true}){Clear();Set(u.State.Features,"Flying",new CountableFlag(flying?1:0));AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter").Invoke(owned,new object[]{ctx,area,u});Check(facts.List.Count==(flying?0:1),"Real terrain entry skips native flying flag: flying="+flying+" count="+facts.List.Count);AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitEnter").Invoke(owned,new object[]{ctx,area,u});Check(facts.List.Count==(flying?0:1),"Repeated native entry idempotent");AccessTools.Method(typeof(AbilityAreaEffectBuff),"OnUnitExit").Invoke(owned,new object[]{ctx,area,u});Check(facts.List.Count==0,"Native terrain exit removes owned condition");}}finally{Set(Kingmaker.Game.Instance,"TimeController",oldTime);Clear();Set(u.State.Features,"Flying",new CountableFlag());}
 }
 static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(100);return false;}
 static bool Add(BlueprintBuff __1,MechanicsContext __2,ref Buff __result){__result=Fact(__1,__2);facts.List.Add(__result);Refresh();return false;}
 static bool Remove(Buff __instance){facts.List.Remove(__instance);Refresh();return false;}
 static void Metadata(BlueprintsCache c,UnitEntityData u){
  string[] names={"AirWalk","AirWalkCommunal","OverlandFlight","WindWalk"};int[][] levels={new[]{4,4,4},new[]{5,5,5},new[]{5,5,5},new[]{6,6,7}};
  for(int i=0;i<names.Length;i++){var spell=Asset<BlueprintAbility>(c,names[i]);Check(spell.GetComponents<SpellListComponent>().Select(l=>l.SpellLevel).OrderBy(l=>l).SequenceEqual(levels[i]),"Original native class levels");foreach(var l in spell.GetComponents<SpellListComponent>())Check(l.SpellList.SpellsByLevel[l.SpellLevel].Spells.Count(s=>s==spell)==1,"Reciprocal unique list");var apply=(ContextActionApplyBuff)spell.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();foreach(int cl in new[]{1,5,10,20,30})foreach(bool ext in new[]{false,true})Check(apply.DurationValue.Calculate(Context(u,spell,cl,ext?Metamagic.Extend:0)).Seconds==TimeSpan.FromSeconds(cl*(i<2?600:3600)*(ext?2:1)),"CL duration and Extend");if(i<3){var bp=apply.Buff;Check((SpellDescriptor)bp.GetComponent<SpellImmunityToSpellDescriptor>().Descriptor==SpellDescriptor.Ground&&(SpellDescriptor)bp.GetComponent<BuffDescriptorImmunity>().Descriptor==SpellDescriptor.Ground,"Native Ground spell and buff immunity");Check(bp.GetComponent<AddConditionImmunity>().Condition==UnitCondition.DifficultTerrain&&bp.GetComponent<AddMechanicsFeature>().Feature==AddMechanicsFeature.MechanicsFeatureType.Flying,"Native difficult terrain immunity and counted flight flag");}}
  var around=Asset<BlueprintAbility>(c,"AirWalkCommunal").GetComponent<AbilityTargetsAround>();Check(((Feet)Get(around,"m_Radius")).Value==30&&(TargetType)Get(around,"m_TargetType")==TargetType.Ally,"Communal30 allies");
 }
 static void Flight(BlueprintsCache c,UnitEntityData u){
  var other=new Psychic070Tests.Caster(c).Unit;var bp=Asset<BlueprintBuff>(c,"OverlandFlightBuff");var comp=bp.GetComponent<PsychicFlightDefense>();var b=Fact(bp,Context(u,bp));var rt=Runtime(comp,b);
  foreach(var type in new[]{AttackType.Melee,AttackType.Touch,AttackType.Ranged,AttackType.RangedTouch})foreach(bool flying in new[]{false,true})foreach(bool canMove in new[]{false,true})foreach(bool ff in new[]{false,true})foreach(int wing in new[]{0,3,4}){
   Clear();mobile=canMove;flat=ff;Set(other.State.Features,"Flying",new CountableFlag(flying?1:0));
   if(wing>0){var wb=new BlueprintBuff {ComponentsArray=new BlueprintComponent[]{new Kingmaker.Designers.Mechanics.Facts.ACBonusAgainstAttacks {AgainstMeleeOnly=true,ArmorClassBonus=wing,Descriptor=ModifierDescriptor.Dodge}}};wb.OnEnable();facts.List.Add(Fact(wb,Context(u,bp)));Refresh();}
   var e=new RuleCalculateAC(other,u,type);using(rt.RequestEventContext())comp.OnEventAboutToTrigger(e);Check(e.AllBonuses.Sum(x=>x.Value)==((type==AttackType.Melee||type==AttackType.Touch)&&!flying&&canMove&&!ff&&wing!=3?3:0),"Flight AC type/grounded/mobile/flatfooted; native wings vs unrelated dodge");
  }Clear();mobile=true;flat=false;
 }
 static void Tremor(BlueprintsCache c,UnitEntityData u){
  var caster=new Psychic070Tests.Caster(c).Unit;Set(caster.State.Features,"Flying",new CountableFlag());Set(caster,"m_Position",new UnityEngine.Vector3(10,0,10));var root=Asset<BlueprintAbility>(c,"EarthTremor");var variants=root.GetComponent<AbilityVariants>().Variants;Check(variants.Length==3&&variants.All(x=>x.Parent==root),"One root, three shapes");Check(root.GetComponents<SpellListComponent>().All(x=>x.SpellLevel==3)&&root.GetComponents<SpellListComponent>().Count()==3,"Druid/Magus/Wizard3");
  for(int i=0;i<3;i++){
   var spell=variants[i];if(i<2){var proj=spell.GetComponent<AbilityDeliverProjectile>();Check(proj.Type==(i==0?AbilityProjectileType.Cone:AbilityProjectileType.Line)&&proj.Length.Value==(i==0?20:30),"Native directional delivery geometry");}else Check(((Feet)Get(spell.GetComponent<AbilityTargetsAround>(),"m_Radius")).Value==10,"Native burst10");
   foreach(int cl in new[]{1,5,10,20})foreach(bool success in new[]{false,true})foreach(bool flying in new[]{false,true})foreach(bool self in new[]{false,true})foreach(var size in new[]{Size.Small,Size.Medium,Size.Large}){
    var target=self?caster:u;Set(target.State.Features,"Flying",new CountableFlag(flying?1:0));Set(target.State,"m_Size",(Size?)size);pass=success;knocks=saves=0;damage.Clear();var ctx=Context(caster,spell,cl);using(ctx.GetDataScope(target))spell.GetComponent<AbilityEffectRunAction>().Actions.Run();bool hit=!self&&!flying;Check(saves==(hit?1:0)&&damage.Count==(hit?1:0),"Caster and airborne excluded before save/damage");Check(knocks==(hit&&!success&&size<=Size.Medium?1:0),"Failed save only Medium or smaller prone");if(hit){var d=damage.Single();var dice=(DiceFormula)Get(d,"Dices");Check(dice.Dice==DiceType.D4&&dice.Rolls==Math.Min(cl,10)&&(bool)Get(d,"HalfBecauseSavingThrow")==success,"CL cap and Reflex half native damage info");}
   }
   var rubble=(PsychicTremorRubble)spell.GetComponent<AdditionalAbilityEffectRunActionOnClickedTarget>().Action.Actions.Single();points.Clear();var spawnctx=Context(caster,spell,12);using(spawnctx.GetDataScope(new TargetWrapper(new UnityEngine.Vector3(20,0,10))))rubble.RunAction();Check(points.Count==(i==2?4:3),"Terrain spawned once, exact original patch count");Check(points.All(v=>v.y==0&&!(v.x==10&&v.z==10)),"Caster tile not rubble center; no elevation change");
   var area=rubble.Area.Get();Check(area.Size.Value==5&&area.GetComponents<AbilityAreaEffectRunAction>().Count()==0,"Rubble has no recurring damage/prone");Check(area.GetComponent<AbilityAreaEffectBuff>().CheckConditionEveryRound&&area.GetComponent<AbilityAreaEffectBuff>().Buff.Stacking==StackingType.Stack,"Source-owned overlap and flying refresh");
  }
 }
 static float baseSpeed;
 static IEnumerable<HarmonyLib.CodeInstruction> BaseSpeed(IEnumerable<HarmonyLib.CodeInstruction> unused){yield return new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Ldsfld,AccessTools.Field(typeof(PsychicTravelTests),nameof(baseSpeed)));yield return new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Ret);}
 static void Map(BlueprintsCache c,UnitEntityData u,Harmony h){
  var type=typeof(PsychicWindTravel).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicWindTravelPatch");var method=AccessTools.Method(typeof(Kingmaker.Assets.Controllers.GlobalMap.GlobalMapMovementController),"CalcPlayerSpeedModifiers");
  // Expose a test-only party provider through the concrete game Player.PartyCharacters getter.
  var state=Get(Kingmaker.Game.Instance,"State");var oldState=state;
  var persistent=Empty<Kingmaker.EntitySystem.PersistentState>();var player=Empty<Kingmaker.Player>();persistent.PlayerState=player;Set(Kingmaker.Game.Instance,"State",persistent);Set(player,"m_Party",new List<UnitEntityData>{u});Set(player,"<PartyCharacters>k__BackingField",new List<UnitReference>{(UnitReference)u});Set(player,"m_CharacterListsValid",true);
  h.Patch(method,transpiler:new HarmonyMethod(typeof(PsychicTravelTests),nameof(BaseSpeed)));var hp=new Harmony("CotW01319.RealMapPostfix");hp.CreateClassProcessor(type).Patch();
  try{foreach(float speed in new[]{0.5f,1f,2f,7f,10f})foreach(int copies in new[]{0,1,3})foreach(bool active in new[]{false,true}){Clear();for(int n=0;n<copies;n++){var b=Fact(Asset<BlueprintBuff>(c,"WindWalkBuff"),Context(u,Asset<BlueprintAbility>(c,"WindWalk")));Set(b,"<IsTurnedOn>k__BackingField",active);facts.List.Add(b);}Refresh();baseSpeed=speed;float actual=(float)method.Invoke(null,null);Check(actual==(copies>0&&active?Math.Max(7,speed):speed),"Map speed minimum7, no stacking, inactive/expired do not affect");}Clear();baseSpeed=1;Check((float)method.Invoke(null,null)==1,"No residual map state after removing last spell");facts.List.Add(Fact(Asset<BlueprintBuff>(c,"WindWalkBuff"),Context(u,Asset<BlueprintAbility>(c,"WindWalk"))));Refresh();player.PartyCharacters.Clear();Check((float)method.Invoke(null,null)==1,"Caster removed from current party no longer grants map speed");}
  finally{hp.UnpatchAll(hp.Id);Set(Kingmaker.Game.Instance,"State",oldState);}
 }
 static bool Damage(ContextActionDealDamage __instance){damage.Add(AccessTools.Method(typeof(ContextActionDealDamage),"GetDamageInfo").Invoke(__instance,null));return false;}
 static bool Knock(){knocks++;return false;}static bool Passed(ref bool __result){__result=pass;return false;}static bool False(ref bool __result){__result=false;return false;}static bool Mobile(ref bool __result){__result=mobile;return false;}
 static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){saves++;Check(rule.Type==SavingThrowType.Reflex&&rule.DifficultyClassBase==23,"Native Reflex at original spell DC");__result=rule;return false;}
 static bool Flat(RuleCheckTargetFlatFooted rule,ref RuleCheckTargetFlatFooted __result){Set(rule,"<IsFlatFooted>k__BackingField",flat);__result=rule;return false;}
 static bool Spawn(ContextActionSpawnAreaEffect __instance){Check(__instance.DurationValue.Calculate((MechanicsContext)AccessTools.Property(typeof(ContextAction),"Context").GetValue(__instance,null)).Seconds==TimeSpan.FromHours(1),"Rubble fixed hour");points.Add(((TargetWrapper)AccessTools.Property(typeof(ContextAction),"Target").GetValue(__instance,null)).Point);return false;}
}
