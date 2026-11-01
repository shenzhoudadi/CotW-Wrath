using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.TurnBasedMode;
using Kingmaker.TurnBasedMode.Controllers;
using Kingmaker.UI;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using Kingmaker.Utility;
using UnityEngine;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

// Native prerequisites, stat replacement and action cooldowns execute unchanged.
// Unity rendering/LOS, time, hit result and final CMB resolution are bounded inputs.
internal static class T09FeatTests {
 static int n,trips,markersMade,markersRemoved;static bool tb,los=true;static Concealment concealment;
 static readonly List<Buff> markers=new List<Buff>();
 static UnitEntityData mounted,mountRider;
 static readonly Type Module=typeof(ArtfulDodgeDefense).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T09CombatFeats");
 static object Get(object o,string f)=>AccessTools.Field(o.GetType(),f).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")??throw new Exception("Fixture field missing: "+f)).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string m){if(!b)throw new Exception("T09: "+m);n++;}
 static void Register(BlueprintsCache c)=>AccessTools.Method(Module,"Register").Invoke(null,new object[]{c});
 static BlueprintGuid Id(string token)=>(BlueprintGuid)AccessTools.Method(typeof(ArtfulDodgeDefense).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token});
 static T Ref<T>(SimpleBlueprint bp)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
 static bool Mode(ref bool __result){__result=tb;return false;}
 static bool Yes(ref bool __result){__result=true;return false;}
 static bool No(ref bool __result){__result=false;return false;}
 static bool Skip()=>false;
 static bool Time(ref TimeSpan __result){__result=TimeSpan.Zero;return false;}
 static bool Hit(RuleAttackRoll __instance,ref bool __result){__result=!__instance.AutoMiss;return false;}
 static bool LOS(ref bool __result){__result=los;return false;}
 static bool Conceal(ref Concealment __result){__result=concealment;return false;}
 static bool Rider(UnitEntityData __0,ref UnitEntityData __result){if(__0!=mounted)return true;__result=mountRider;return false;}
 static bool Maneuver(RuleCombatManeuver __0,ref RuleCombatManeuver __result){Check(__0.Type==CombatManeuver.Trip,"native trip maneuver used");trips++;__result=__0;return false;}
 static bool AddMarker(UnitEntityData __0,BlueprintBuff __1,MechanicsContext __2,TimeSpan? __3,ref Buff __result){
  if(__1.AssetGuid!=Id("OsyluthGuileTargetMarkerBuff"))return true;
  Check(__3==null,"permanent marker has no six-second duration");
  __result=Psychic034Tests.Fact(__0,__1,__2.MaybeCaster,1);Set(__result,"UniqueId","guile-marker-"+(++markersMade));__result.EndTime=TimeSpan.MaxValue;markers.Add(__result);return false;
 }
 static bool RemoveMarker(Buff __instance){
  if(__instance.Blueprint.AssetGuid!=Id("OsyluthGuileTargetMarkerBuff"))return true;
  var u=__instance.Owner;u.Facts.List.Remove(__instance);Set(__instance,"IsTurnedOn",false);Refresh(u);markersRemoved++;return false;
 }
 sealed class Predictions:IActionsPredictionHandler{
  internal CombatAction Swift;internal int Calls;internal CombatAction.ActionState State;internal bool CanUse;
  public void PredictionChanged(){Calls++;State=Swift.PredictionState;CanUse=Swift.CanUse;}
 }
 static UnitEntityData Unit(BlueprintsCache c,string id){
  var u=Psychic034Tests.Unit(c,id).Unit;
  var fc=new FeatureCollection();fc.Initialize(u.Facts);Set(u.Progression,"<Features>k__BackingField",fc);
  Set(u,"<CombatState>k__BackingField",Empty<UnitCombatState>());Set(u.CombatState,"Unit",u);Set(u.CombatState,"Cooldown",new UnitCombatState.Cooldowns());Set(u.CombatState,"m_InCombat",true);
  Set(u.CombatState,"m_EngagedBy",new Dictionary<UnitEntityData,TimeSpan>());Set(u.CombatState,"m_EngagedUnits",new Dictionary<UnitEntityData,TimeSpan>());
  Set(u,"<Commands>k__BackingField",new UnitCommands(u));Set(u.State,"m_Conditions",new sbyte[128]);Set(u.State,"m_ConditionsImmunity",new sbyte[128]);Set(u.State,"m_ConditionsExceptions",new List<UnitConditionExceptions>[128]);
  return u;
 }
 static void Refresh(UnitEntityData u)=>AccessTools.Method(Get(u.Facts,"m_Cache").GetType(),"Clear").Invoke(Get(u.Facts,"m_Cache"),null);
 static Feature Feature(UnitEntityData u,BlueprintFeature bp){var f=Empty<Feature>();Set(f,"Components",new List<EntityFactComponent>());f.RestoreOwnerLink(u.Facts);Set(f,"<Rank>k__BackingField",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(f,new object[]{bp});Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);u.Facts.List.Add(f);Refresh(u);return f;}
 static void Call(EntityFact f,Type type,string method,params object[] args){var rt=f.Components.Single(x=>x.SourceBlueprintComponent.GetType()==type);using(rt.RequestEventContext())AccessTools.Method(type,method).Invoke(rt.SourceBlueprintComponent,args);}
 static int AC(EntityFact f,Type type,UnitEntityData attacker,UnitEntityData defender,AttackType kind){var r=new RuleCalculateAC(attacker,defender,kind);Call(f,type,"OnEventAboutToTrigger",r);return r.AllBonuses.Where(x=>x.Descriptor==ModifierDescriptor.Dodge).Sum(x=>x.Value);}
 static BlueprintFeatureSelection Selection(BlueprintsCache c,string id){var s=c.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection;if(s==null){s=new BlueprintFeatureSelection{name="NativeSelection",AssetGuid=BlueprintGuid.Parse(id),ComponentsArray=Array.Empty<BlueprintComponent>()};Set(s,"m_Features",Array.Empty<BlueprintFeatureReference>());Set(s,"m_AllFeatures",Array.Empty<BlueprintFeatureReference>());c.AddCachedBlueprint(s.AssetGuid,s);}return s;}
 static RuleAttackWithWeapon Attack(UnitEntityData u,UnitEntityData t,BlueprintItemWeapon bp){
  var w=Empty<ItemEntityWeapon>();Set(w,"m_Blueprint",bp);var r=Empty<RuleAttackWithWeapon>();Set(r,"Initiator",u);Set(r,"Target",t);Set(r,"Weapon",w);Set(r,"AttacksCount",1);
  var roll=Empty<RuleAttackRoll>();Set(r,"<AttackRoll>k__BackingField",roll);return r;
 }
 static UnitAttack Command(UnitEntityData u,RuleAttackWithWeapon r){var cmd=Empty<UnitAttack>();Set(cmd,"<Type>k__BackingField",UnitCommand.CommandType.Standard);Set(cmd,"<LastAttackRule>k__BackingField",r);u.Commands.Raw[(int)UnitCommand.CommandType.Standard]=cmd;return cmd;}
 internal static int Run(BlueprintsCache c){
  var h=new Harmony("CotWT09.NativeFeats");var oldTime=Game.Instance.TimeController;var oldCombat=Game.Instance.TurnBasedCombatController;Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());void Patch(System.Reflection.MethodBase m,string s)=>h.Patch(m,prefix:new HarmonyMethod(typeof(T09FeatTests),s));
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));
  Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),nameof(Hit));
  Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(Mode));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),nameof(No));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.Method(typeof(UnitEntityData),"HasLOS",new[]{typeof(UnitEntityData)}),nameof(LOS));Patch(AccessTools.Method(typeof(UnitPartConcealment),"Calculate"),nameof(Conceal));
  // Full level-up/reactivation observers need the live UI/event bus. Numeric
  // prerequisites and feature lookup below are still real engine calls.
  Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));
  Patch(AccessTools.Method(typeof(UnitHelper),"AddBuff",new[]{typeof(UnitEntityData),typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),nameof(AddMarker));
  Patch(AccessTools.Method(typeof(Buff),"Remove"),nameof(RemoveMarker));
  Patch(AccessTools.Method(typeof(UnitHelper),"GetRider",new[]{typeof(UnitEntityData)}),nameof(Rider));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleCombatManeuver),AccessTools.Method(typeof(T09FeatTests),nameof(Maneuver)));
  try{
   var registration=typeof(ArtfulDodgeDefense).Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration");
   var selectionIds=new[]{(string)GetStatic(registration,"BasicFeatsId")}.Concat((string[])GetStatic(registration,"NativeCombatFeatSelectionIds")).ToArray();
   var selections=selectionIds.Select(x=>Selection(c,x)).ToArray();var previous=selections.Select(s=>new[]{((BlueprintFeatureReference[])Get(s,"m_Features")).Select(x=>x.Guid).ToArray(),((BlueprintFeatureReference[])Get(s,"m_AllFeatures")).Select(x=>x.Guid).ToArray()}).ToArray();
   foreach(string id in new[]{"97e216dbb46ae3c4faef90cf6bbe6fd5","9972f33f977fc724c838e59641b2fca5","0f15c6f70d8fb2b49aa6cc24239cc5fa","4c44724ffa8844f4d9bedb5bb27d144a"})if(c.Load(BlueprintGuid.Parse(id))==null)c.AddCachedBlueprint(BlueprintGuid.Parse(id),PsychicExpansionTests.NativeCase<BlueprintFeature>(id));
   Register(c);var feats=new[]{"ArtfulDodgeFeat","OsyluthGuileFeat","FellingSmashFeat"}.Select(t=>Psychic034Tests.Asset<BlueprintFeature>(c,t)).ToArray();
   Register(c);foreach(var f in feats){Check(ReferenceEquals(c.Load(f.AssetGuid),f),"registration stable identity");Check(f.Groups.Contains(FeatureGroup.CombatFeat)&&f.Groups.Contains(FeatureGroup.Feat),"combat and general feat groups");Check(f.Ranks==1,"single-rank feat");Check(f.ComponentsArray.Select(x=>x.name).Distinct().Count()==f.ComponentsArray.Length,"stable unique component save names");}
   for(int i=0;i<selections.Length;i++)for(int j=0;j<2;j++){var all=(BlueprintFeatureReference[])Get(selections[i],j==0?"m_Features":"m_AllFeatures");Check(previous[i][j].All(x=>all.Any(a=>a.Guid==x)),"existing selection preserved");foreach(var f in feats)Check(all.Count(x=>x.Guid==f.AssetGuid)==1,"one entry per new feat in both arrays");}
   var collision=new BlueprintsCache();foreach(var id in selectionIds)Selection(collision,id);foreach(var id in new[]{"97e216dbb46ae3c4faef90cf6bbe6fd5","9972f33f977fc724c838e59641b2fca5","0f15c6f70d8fb2b49aa6cc24239cc5fa",(string)GetStatic(registration,"CombatExpertiseId")})collision.AddCachedBlueprint(BlueprintGuid.Parse(id),new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(id)});
   var foreign=new BlueprintFeature{name="Foreign",AssetGuid=feats[1].AssetGuid};collision.AddCachedBlueprint(foreign.AssetGuid,foreign);bool rejected=false;try{Register(collision);}catch{rejected=true;}Check(rejected&&ReferenceEquals(collision.Load(foreign.AssetGuid),foreign)&&collision.Load(feats[0].AssetGuid)==null,"GUID collision blocks whole T09 transaction without partial assets");
   var missing=new BlueprintsCache();rejected=false;try{Register(missing);}catch{rejected=true;}Check(rejected&&missing.Load(feats[0].AssetGuid)==null,"missing dependency fails closed");
   var u=Unit(c,"0140-owner");var enemy=Unit(c,"0140-enemy");var other=Unit(c,"0140-other");u.Stats.Intelligence.BaseValue=16;u.Stats.Dexterity.BaseValue=8;u.Stats.Strength.BaseValue=13;u.Stats.BaseAttackBonus.BaseValue=6;
   var artful=Feature(u,feats[0]);var guile=Feature(u,feats[1]);var artfulNeed=feats[0].GetComponents<PrerequisiteStatValue>().Single();Check(artfulNeed.CheckUnit(u.Descriptor),"Int 13 qualifies");u.Stats.Intelligence.BaseValue=12;Check(!artfulNeed.CheckUnit(u.Descriptor),"Int 12 denied");u.Stats.Intelligence.BaseValue=16;
   var dex=new PrerequisiteStatValue{Stat=StatType.Dexterity,Value=15};Check(dex.CheckUnit(u.Descriptor),"native stat replacement uses Int for Dex prerequisites");dex.Value=17;Check(!dex.CheckUnit(u.Descriptor),"replacement does not bypass minimum");
   foreach(var type in new[]{"ArtfulDodgeFeaturePrerequisitePatch","ArtfulDodgeListPrerequisitePatch"})h.CreateClassProcessor(typeof(ArtfulDodgeDefense).Assembly.GetType("CallOfTheWild.Wrath.Prototype."+type)).Patch();
   var need=feats[1].GetComponent<PrerequisiteFeature>();Check(Need(need,u),"actual Dodge prerequisite accepts Artful");Check(!u.HasFact(need.Feature),"Artful does not grant the real Dodge fact");
   var state=Empty<FeatureSelectionState>();Set(state,"<SelectedItem>k__BackingField",feats[0]);Check(!Need(need,u,state),"uncommitted selected-child feat cannot satisfy Dodge");
   var list=new PrerequisiteFeaturesFromList{Amount=1};Set(list,"m_Features",new[]{Ref<BlueprintFeatureReference>(need.Feature),Ref<BlueprintFeatureReference>((BlueprintFeature)c.Load(BlueprintGuid.Parse("9972f33f977fc724c838e59641b2fca5")))});
   Check(Need(list,u),"list OR accepts replacement");list.Amount=2;Check(!Need(list,u),"list amount retains other requirement");var powerFeat=Feature(u,(BlueprintFeature)c.Load(BlueprintGuid.Parse("9972f33f977fc724c838e59641b2fca5")));Check(Need(list,u),"two genuine qualifying list entries");Set(list,"m_RestrictIfNot",true);Check(!Need(list,u),"restrict-if-not inversion retained");
   u.Facts.List.Remove(artful);Refresh(u);Check(!Need(need,u)&&!dex.CheckUnit(u.Descriptor),"retraining removes both prerequisite replacements");u.Facts.List.Add(artful);Refresh(u);
   var persuasion=Empty<ModifiableValueSkill>();Set(persuasion,"Type",StatType.SkillPersuasion);Set(persuasion,"m_BaseValue",8);Set(u.Stats,"SkillPersuasion",persuasion);u.Stats.AllStats.Add(persuasion);var ranks=feats[1].GetComponent<PrerequisiteStatValue>();Check(ranks.CheckUnit(u.Descriptor),"Persuasion 8 ranks");Set(persuasion,"m_BaseValue",7);Check(!ranks.CheckUnit(u.Descriptor),"Persuasion 7 ranks denied");
   foreach(var s in feats[2].GetComponents<PrerequisiteStatValue>()){Check(s.CheckUnit(u.Descriptor),"Felling numeric prerequisite met");var value=u.Stats.GetStat(s.Stat);int old=value.BaseValue;value.BaseValue=s.Value-1;Check(!s.CheckUnit(u.Descriptor),"Felling each numeric threshold denied");value.BaseValue=old;}
   foreach(var s in feats[2].GetComponents<PrerequisiteFeature>()){var owned=u.Progression.Features.GetFact(s.Feature);if(owned!=null){u.Facts.List.Remove(owned);Refresh(u);}Check(!Need(s,u),"Felling missing native feat denied");var added=Feature(u,s.Feature);Check(Need(s,u),"Felling native prerequisite accepted");}
   var outgoing=(Dictionary<UnitEntityData,TimeSpan>)Get(u.CombatState,"m_EngagedUnits");var incoming=(Dictionary<UnitEntityData,TimeSpan>)Get(enemy.CombatState,"m_EngagedBy");outgoing.Add(enemy,TimeSpan.Zero);incoming.Add(u,TimeSpan.Zero);
   Check(AC(artful,typeof(ArtfulDodgeDefense),enemy,u,AttackType.Melee)==1,"sole threatening character gets Dodge 1");incoming.Add(other,TimeSpan.Zero);Check(AC(artful,typeof(ArtfulDodgeDefense),enemy,u,AttackType.Melee)==0,"second threatening character removes bonus");incoming.Remove(other);outgoing.Clear();Check(AC(artful,typeof(ArtfulDodgeDefense),enemy,u,AttackType.Melee)==0,"not threatening opponent grants nothing");
   var defensive=Psychic034Tests.Fact(u,PsychicExpansionTests.NativeCase<BlueprintBuff>("6ffd93355fb3bcf4592a5d976b1d32a9"),u,1);u.Stats.Charisma.BaseValue=18;
   Call(guile,typeof(OsyluthGuileDefense),"Choose",enemy);Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==4,"chosen melee attacker plus positive Cha");Check(AC(guile,typeof(OsyluthGuileDefense),other,u,AttackType.Melee)==0,"other attacker excluded");Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Ranged)==0,"ranged excluded");
   defensive.IsSuppressed=true;Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==0,"suppressed defense excluded");defensive.IsSuppressed=false;Set(defensive,"<IsTurnedOn>k__BackingField",false);Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==0,"defensive off excluded");Set(defensive,"<IsTurnedOn>k__BackingField",true);
   u.Stats.Charisma.BaseValue=8;Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==0,"negative Cha no penalty");u.Stats.Charisma.BaseValue=18;
   los=false;Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==0,"no line of sight excluded");los=true;concealment=Concealment.Total;Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==0,"unseen total concealment excluded");concealment=Concealment.None;
   var conditions=(sbyte[])Get(u.State,"m_Conditions");conditions[(int)UnitCondition.Blindness]=1;Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==0,"blind owner excluded");conditions[(int)UnitCondition.Blindness]=0;
   Call(guile,typeof(OsyluthGuileDefense),"Choose",other);Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==0&&AC(guile,typeof(OsyluthGuileDefense),other,u,AttackType.Melee)==4,"reselect replaces prior opponent");
   var ability=Psychic034Tests.Asset<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(c,"OsyluthGuileChoose");Check(ability.ActionType==UnitCommand.CommandType.Free&&ability.CanTargetEnemies&&!ability.CanTargetFriends&&!ability.CanTargetSelf,"free action has enemy-only targeting");
   var context=new MechanicsContext(u,u.Descriptor,ability,null,new TargetWrapper(enemy));using(context.GetDataScope(new TargetWrapper(enemy)))ability.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>().Actions.Actions.OfType<OsyluthGuileChoose>().Single().RunAction();Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==4&&AC(guile,typeof(OsyluthGuileDefense),other,u,AttackType.Melee)==0,"actual ability action chooses feature runtime target");
   Set(defensive,"<IsTurnedOn>k__BackingField",false);var expertiseBuff=Psychic034Tests.Fact(u,PsychicExpansionTests.NativeCase<BlueprintBuff>("e81cd772a7311554090e413ea28ceea1"),u,1);Check(AC(guile,typeof(OsyluthGuileDefense),enemy,u,AttackType.Melee)==4,"CotW Combat Expertise adaptation works");Set(expertiseBuff,"<IsTurnedOn>k__BackingField",false);Set(defensive,"<IsTurnedOn>k__BackingField",true);Call(guile,typeof(OsyluthGuileDefense),"Choose",other);
   var data=guile.Components.Single(x=>x.SourceBlueprintComponent is OsyluthGuileDefense).GetData<OsyluthGuileState>();data.Until=TimeSpan.Zero;tb=false;Check(AC(guile,typeof(OsyluthGuileDefense),other,u,AttackType.Melee)==4,"legacy real-time deadline no longer expires target");tb=true;Check(AC(guile,typeof(OsyluthGuileDefense),other,u,AttackType.Melee)==4,"turn-based target still selected");int beforeMarker=markersMade;Call(guile,typeof(OsyluthGuileDefense),"OnNewRound");Check(data.Target==other&&AC(guile,typeof(OsyluthGuileDefense),other,u,AttackType.Melee)==4&&markersMade==beforeMarker,"next round retains selection and reuses marker");
   var markerBp=Psychic034Tests.Asset<BlueprintBuff>(c,"OsyluthGuileTargetMarkerBuff");Check(markerBp.Stacking==StackingType.Stack&&Convert.ToInt32(Get(markerBp,"m_Flags"))==0&&markerBp.ComponentsArray.Length==0,"visible cosmetic marker supports independent selectors without rule penalties");
   Check(other.Buffs.Enumerable.Count(b=>b.Blueprint==markerBp)==1&&markers.Last().IsNotDispelable,"chosen target has one permanent visible marker");Check(enemy.Buffs.Enumerable.All(b=>b.Blueprint!=markerBp),"previous target marker removed");
   Call(guile,typeof(OsyluthGuileDefense),"Choose",other);Check(markersMade==beforeMarker,"same opponent selection does not duplicate marker");
   var foreignGuile=Feature(enemy,feats[1]);Call(foreignGuile,typeof(OsyluthGuileDefense),"Choose",other);Check(other.Buffs.Enumerable.Count(b=>b.Blueprint==markerBp)==2,"two selectors keep separate target markers");
   Call(guile,typeof(OsyluthGuileDefense),"Choose",enemy);Check(other.Buffs.Enumerable.Count(b=>b.Blueprint==markerBp)==1&&other.Buffs.Enumerable.Single(b=>b.Blueprint==markerBp).Context.MaybeCaster==enemy,"changing target removes only own marker");
   var onEnemy=enemy.Buffs.Enumerable.Single(b=>b.Blueprint==markerBp);onEnemy.Remove();Call(guile,typeof(OsyluthGuileDefense),"OnActivate");Check(data.Target==enemy&&enemy.Buffs.Enumerable.Count(b=>b.Blueprint==markerBp)==1,"reload/activation restores missing own marker once");Call(guile,typeof(OsyluthGuileDefense),"OnActivate");Check(enemy.Buffs.Enumerable.Count(b=>b.Blueprint==markerBp)==1,"repeat activation idempotent");
   var inheritedContext=new MechanicsContext(other,other.Descriptor,feats[1],null,new TargetWrapper(enemy));Set(guile,"m_Context",inheritedContext);Call(guile,typeof(OsyluthGuileDefense),"ClearTarget");Call(guile,typeof(OsyluthGuileDefense),"Choose",enemy);Call(guile,typeof(OsyluthGuileDefense),"OnNewRound");Check(enemy.Buffs.Enumerable.Count(b=>b.Blueprint==markerBp)==1&&enemy.Buffs.Enumerable.Single(b=>b.Blueprint==markerBp).Context.MaybeCaster==u,"inherited foreign feature context still makes exactly one owner marker");Set(guile,"m_Context",null);
   Set(enemy.State,"LifeState",UnitLifeState.Dead);Call(guile,typeof(OsyluthGuileDefense),"HandleUnitLifeStateChanged",enemy,UnitLifeState.Conscious);Check(data.Target==null&&enemy.Buffs.Enumerable.All(b=>b.Blueprint!=markerBp),"target death event immediately clears selection and marker");Set(enemy.State,"LifeState",UnitLifeState.Conscious);Call(guile,typeof(OsyluthGuileDefense),"OnNewRound");Check(data.Target==null,"resurrection does not automatically reselect former opponent");
   Call(guile,typeof(OsyluthGuileDefense),"Choose",enemy);Call(guile,typeof(OsyluthGuileDefense),"OnDeactivate");Check(data.Target==null&&enemy.Buffs.Enumerable.All(b=>b.Blueprint!=markerBp),"feat removal clears target and marker");Call(foreignGuile,typeof(OsyluthGuileDefense),"OnDeactivate");
   foreach(string field in new[]{"Target","Until","MarkerId"})Check(typeof(OsyluthGuileState).GetField(field).IsDefined(typeof(JsonPropertyAttribute),false),"persisted target/legacy expiry/marker field");var saved=JsonConvert.DeserializeObject<OsyluthGuileState>(JsonConvert.SerializeObject(new OsyluthGuileState{Until=TimeSpan.FromSeconds(37),MarkerId="saved-marker"}));Check(saved.Until==TimeSpan.FromSeconds(37)&&saved.MarkerId=="saved-marker","persisted legacy deadline and marker ID managed roundtrip");var legacy=JsonConvert.DeserializeObject<OsyluthGuileState>("{\"Until\":\"00:00:06\"}");Check(legacy.MarkerId==null&&legacy.Until==TimeSpan.FromSeconds(6),"legacy save without marker member still loads");
   var mode=Psychic034Tests.Fact(u,Psychic034Tests.Asset<BlueprintBuff>(c,"FellingSmashModeBuff"),u,1);var power=Psychic034Tests.Fact(u,PsychicExpansionTests.NativeCase<BlueprintBuff>("5898bcf75a0942449a5dc16adc97b279"),u,1);var weapon=Psychic034Tests.Asset<BlueprintItemWeapon>(c,"SpiritualWeaponItem");
   foreach(bool turn in new[]{false,true}){tb=turn;u.CombatState.Cooldown.SwiftAction=0;trips=0;var r=Attack(u,enemy,weapon);Command(u,r);Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",r);Check(trips==1&&u.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Swift),"qualifying hit consumes swift and tries trip in both combat modes");Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",r);Check(trips==1,"spent swift blocks second trigger");}
   var controller=Empty<CombatController>();var turnController=Empty<TurnController>();Set(controller,"m_CurrentTurn",turnController);Set(turnController,"m_Rider",(UnitReference)u);Set(Game.Instance,"TurnBasedCombatController",controller);
   var swiftState=new CombatAction(null,null,CombatAction.ActivityState.Available);var standardState=new CombatAction(null,CombatAction.ActivityState.WillBeUsed,CombatAction.ActivityState.Available);var moveState=new CombatAction(CombatAction.ActivityState.Available,null,null);
   var actions=new ActionsState(new ActionsState.Actions{Swift=swiftState,Standard=standardState,Move=moveState},false);Set(turnController,"m_RiderActionState",actions);u.CombatState.TBM=new UnitCombatState.TBMState(u);
   var prediction=new Predictions{Swift=swiftState};using(EventBus.Subscribe(prediction)){
    tb=true;trips=0;u.CombatState.Cooldown.SwiftAction=0;var r=Attack(u,enemy,weapon);Command(u,r);Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",r);
    Check(trips==1&&u.CombatState.Cooldown.SwiftAction==6,"native SpendAction consumes real swift cooldown");Check(swiftState.AbilityActivityStateCurrent==CombatAction.ActivityState.Used&&swiftState.PredictionState==CombatAction.ActionState.Used&&!swiftState.CanUse,"actual turn Swift current/predicted state committed to Used");
    Check(prediction.Calls==1&&prediction.State==CombatAction.ActionState.Used&&!prediction.CanUse,"real UI prediction event sees committed state");Check(ReferenceEquals(u.CombatState.TBM.ActionState.Swift,swiftState),"native UI consumer gets this exact state via TBM.ActionState");
    Check(ReferenceEquals(actions.Standard,standardState)&&standardState.PredictionState==CombatAction.ActionState.WillBeUsed&&ReferenceEquals(actions.Move,moveState)&&moveState.CanUse,"swift update preserves pending standard and move actions");
    Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",r);Check(trips==1&&prediction.Calls==1,"repeat cannot consume or publish twice");
    AccessTools.Method(typeof(UnitCombatState.Cooldowns),"Clear").Invoke(u.CombatState.Cooldown,null);var nextSwift=new CombatAction(null,null,u.HasSwiftAction()?CombatAction.ActivityState.Available:CombatAction.ActivityState.Unable);Set(turnController,"m_RiderActionState",new ActionsState(new ActionsState.Actions{Swift=nextSwift},false));Check(u.HasSwiftAction()&&nextSwift.PredictionState==CombatAction.ActionState.Available,"native HasSwiftAction/new action state restores availability after the next-turn cooldown reset");
    prediction.Swift=nextSwift;int calls=prediction.Calls;other.CombatState.Cooldown.SwiftAction=0;AccessTools.Method(typeof(FellingSmashTrigger),"SpendSwift").Invoke(null,new object[]{other,mode.Blueprint});Check(other.CombatState.Cooldown.SwiftAction==6&&nextSwift.CanUse&&prediction.Calls==calls,"unit outside current turn pays only its own cooldown without changing displayed actions");
    tb=false;u.CombatState.Cooldown.SwiftAction=0;AccessTools.Method(typeof(FellingSmashTrigger),"SpendSwift").Invoke(null,new object[]{u,mode.Blueprint});Check(u.CombatState.Cooldown.SwiftAction==6&&nextSwift.CanUse&&prediction.Calls==calls,"real time leaves any stale turn UI state untouched");
    tb=true;Set(u.CombatState,"m_InCombat",false);u.CombatState.Cooldown.SwiftAction=0;AccessTools.Method(typeof(FellingSmashTrigger),"SpendSwift").Invoke(null,new object[]{u,mode.Blueprint});Check(u.CombatState.Cooldown.SwiftAction==6&&prediction.Calls==calls,"opening attack preserves legacy swift payment before native combat starts");Set(u.CombatState,"m_InCombat",true);
    mounted=other;mountRider=u;Set(turnController,"m_Mount",(UnitReference)other);var mountSwift=new CombatAction(null,null,CombatAction.ActivityState.Available);Set(turnController,"m_MountActionState",new ActionsState(new ActionsState.Actions{Swift=mountSwift},false));prediction.Swift=mountSwift;other.CombatState.Cooldown.SwiftAction=0;AccessTools.Method(typeof(FellingSmashTrigger),"SpendSwift").Invoke(null,new object[]{other,mode.Blueprint});Check(mountSwift.PredictionState==CombatAction.ActionState.Used&&nextSwift.CanUse&&prediction.Calls==calls+1,"native mounted action mapping consumes mount Swift without consuming rider Swift");mounted=mountRider=null;
   }
   Set(Game.Instance,"TurnBasedCombatController",oldCombat);
   for(int branch=0;branch<10;branch++){
    tb=false;trips=0;u.CombatState.Cooldown.SwiftAction=0;var r=Attack(u,enemy,weapon);Command(u,r);
    if(branch==0)r.IsCharge=true;if(branch==1)r.IsFullAttack=true;if(branch==2)r.IsAttackOfOpportunity=true;if(branch==3)Set(r.AttackRoll,"<AutoMiss>k__BackingField",true);if(branch==4)Set(r,"AttacksCount",2);if(branch==5)Set(r,"AttackBonusPenalty",5);if(branch==6)Set(r,"AttackNumber",1);if(branch==7)Set(r.AttackRoll,"<IsFake>k__BackingField",true);if(branch==8)power.IsSuppressed=true;if(branch==9)Command(u,Attack(u,enemy,weapon));
    Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",r);Check(trips==0&&u.CombatState.Cooldown.SwiftAction==0,"excluded attack/Power Attack/off-command does not spend swift: "+branch);power.IsSuppressed=false;
   }
   var swing=Attack(u,enemy,weapon);Command(u,swing);u.Commands.Raw[(int)UnitCommand.CommandType.Swift]=Empty<UnitAttack>();trips=0;Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",swing);Check(trips==0,"queued swift action is preserved");u.Commands.Raw[(int)UnitCommand.CommandType.Swift]=null;
   swing.ExtraAttack=true;Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",swing);Check(trips==0&&u.CombatState.Cooldown.SwiftAction==0,"explicit extra-attack marker excluded");swing.ExtraAttack=false;mode.IsSuppressed=true;Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",swing);Check(trips==0,"suppressed toggle does not trigger");mode.IsSuppressed=false;
   var rangedType=new BlueprintWeaponType{Category=WeaponCategory.Longbow};Set(rangedType,"m_AttackType",AttackType.Ranged);var rangedBp=new BlueprintItemWeapon();Set(rangedBp,"m_Type",Ref<BlueprintWeaponTypeReference>(rangedType));var rangedAttack=Attack(u,enemy,rangedBp);Command(u,rangedAttack);Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",rangedAttack);Check(trips==0&&u.CombatState.Cooldown.SwiftAction==0,"ranged weapon excluded by actual weapon getter");
   foreach(var id in new[]{"efc60c91b8e64f244b95c66b270dbd7c","c714cd636700ac24a91ca3df43326b00","11f971b6453f74d4594c538e3c88d499"}){
    var vital=PsychicExpansionTests.NativeCase<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(id);Check(vital.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityCustomVitalStrike>()!=null,"audited native Vital Strike component");
    var a=Empty<Kingmaker.UnitLogic.Abilities.AbilityData>();Set(a,"Blueprint",vital);var cmd=Empty<UnitUseAbility>();Set(cmd,"Type",UnitCommand.CommandType.Standard);Set(cmd,"Ability",a);u.Commands.Raw[(int)UnitCommand.CommandType.Standard]=cmd;
    var rule=Attack(u,enemy,weapon);Set(rule,"AttacksCount",0);Set(rule,"IsFirstAttack",true);var reason=Empty<RuleReason>();Set(reason,"Ability",a);rule.Reason=reason;
    foreach(bool turn in new[]{false,true}){tb=turn;trips=0;u.CombatState.Cooldown.SwiftAction=0;Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",rule);Check(trips==1&&u.CombatState.Cooldown.SwiftAction==6,"native Vital Strike single hit qualifies in both modes");}
    trips=0;u.CombatState.Cooldown.SwiftAction=0;Set(reason,"Ability",Empty<Kingmaker.UnitLogic.Abilities.AbilityData>());Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",rule);Check(trips==0&&u.CombatState.Cooldown.SwiftAction==0,"unrelated proc during Vital Strike excluded");Set(reason,"Ability",a);Set(rule,"IsFirstAttack",false);Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",rule);Check(trips==0,"non-first ability attack excluded");
    Set(rule,"IsFirstAttack",true);Set(a,"Blueprint",ability);Call(mode,typeof(FellingSmashTrigger),"OnEventDidTrigger",rule);Check(trips==0,"other standard abilities excluded");
   }
   foreach(var token in new[]{"SpiritualWeapon","MagesSword","TwilightKnife"})Check(Psychic034Tests.Asset<BlueprintBuff>(c,token+"ActorBuff").GetComponents<SpiritWeaponBlueVisual>().Count()==1,"each floating spirit has one blue visual component");Check(Psychic034Tests.Asset<BlueprintBuff>(c,"SpiritualAllyActorBuff").GetComponents<SpiritWeaponBlueVisual>().Count()==1,"Ally now has requested blue human spirit visuals");
   foreach(var source in new[]{new Color(1,.6f,.1f,.3f),new Color(0,0,0,1),new Color(3,2,1,.8f)}){var result=(Color)AccessTools.Method(typeof(SpiritWeaponBlueVisual),"Blue").Invoke(null,new object[]{source});float max=Math.Max(source.r,Math.Max(source.g,source.b));Check(Math.Abs(result.b-max)<.001&&Math.Abs(result.g-max*.48f)<.001&&Math.Abs(result.r-max*.12f)<.001&&result.a==source.a,"warm/HDR/black colors converted to blue preserving intensity and alpha");}
   var fx=Empty<FxHandle>();var visualBuff=Psychic034Tests.Fact(u,Psychic034Tests.Asset<BlueprintBuff>(c,"SpiritualWeaponActorBuff"),u,1);Set(visualBuff,"m_ParticleEffect",fx);Call(visualBuff,typeof(SpiritWeaponBlueVisual),"OnSpawnFx");Check(((Delegate)Get(fx,"OnSpawn")).GetInvocationList().Single().Method.Name=="Apply","native deferred spawn handle receives blue callback");fx.HandleDestroy();Check(Get(fx,"OnSpawn")==null,"native destroyed handle drops callback");
   var restore=Empty<SpiritBlueFxRestore>();Set(restore,"actions",new List<Action>());int restored=0;AccessTools.Method(typeof(SpiritBlueFxRestore),"Add").Invoke(restore,new object[]{(Action)(()=>restored++)});AccessTools.Method(typeof(SpiritBlueFxRestore),"OnDisable").Invoke(restore,null);AccessTools.Method(typeof(SpiritBlueFxRestore),"Restore").Invoke(restore,null);Check(restored==1,"pooled FX cleanup restores exactly once");
   PsychicExpansionTests.Inventory["t09FeatIds"]=new JObject(feats.Select(f=>new JProperty(f.name,f.AssetGuid.ToString())));PsychicExpansionTests.Inventory["t09Assets"]=new JArray(new[]{"ArtfulDodgeFeat","OsyluthGuileFeat","FellingSmashFeat","OsyluthGuileChoose","FellingSmashModeBuff","FellingSmashToggle","OsyluthGuileTargetMarkerBuff"}.Select(t=>{var bp=c.Load(Id(t));return new JObject{["token"]=t,["guid"]=bp.AssetGuid.ToString(),["name"]=bp.name,["type"]=bp.GetType().Name};}));PsychicExpansionTests.Inventory["feat0140Assertions"]=n;PsychicExpansionTests.Inventory["repair0141Assertions"]=n;
  }finally{h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",oldTime);Set(Game.Instance,"TurnBasedCombatController",oldCombat);tb=false;los=true;concealment=Concealment.None;mounted=mountRider=null;}
  Console.WriteLine("PASS: "+n+" T09 prerequisite/selection/AC/turn-expiry/native swift/action/blue callback checks; rendering, LOS and final maneuver resolution bounded.");return n;
 }
 static object GetStatic(Type type,string field)=>AccessTools.Field(type,field).GetValue(null);
 static bool Need(Prerequisite p,UnitEntityData u,FeatureSelectionState state=null)=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{state,u.Descriptor,null});
}
