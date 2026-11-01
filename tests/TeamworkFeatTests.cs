using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using UnityEngine;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

// Native fact runtimes, item types, pool selection, attack modifiers and action
// spending run here. World query, faction, time, hit result and final animated
// reaction execution are explicit fixture boundaries, not game acceptance.
internal static class TeamworkFeatTests {
 static RuleAttackWithWeapon lastReaction;static int n,reactions,boundary;static bool tb,flanked,canAttack=true;static TimeSpan now;
 static UnitEntityData[] world=Array.Empty<UnitEntityData>();
 static readonly Type Module=typeof(CoordinatedShotBonus).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkFeats");
 static readonly Type Factory=typeof(CoordinatedShotBonus).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
 static readonly Type Catalog=typeof(CoordinatedShotBonus).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkCatalog");
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string s){if(!b)throw new Exception("Teamwork 01411: "+s);n++;}
 static BlueprintGuid Id(string t)=>(BlueprintGuid)AccessTools.Method(Factory,"Id").Invoke(null,new object[]{t});
 static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>(T)c.Load(Id(t));
 static void Register(BlueprintsCache c)=>AccessTools.Method(Module,"Register").Invoke(null,new object[]{c});
 static bool Yes(ref bool __result){__result=true;return false;}
 static bool No(ref bool __result){__result=false;return false;}
 static bool Skip()=>false;
 static bool Mode(ref bool __result){__result=tb;return false;}
 static bool Flanked(ref bool __result){__result=flanked;return false;}
 static bool CanAttack(ref bool __result){__result=canAttack;return false;}
 static bool Time(ref TimeSpan __result){__result=now;return false;}
 static bool Hit(RuleAttackRoll __instance,ref bool __result){__result=!__instance.AutoMiss;return false;}
 static bool Targets(ref IEnumerable<UnitEntityData> __result){__result=world;return false;}
 static bool ActionBoundary(){boundary++;return false;}
 static bool Reaction(RuleAttackWithWeapon __0,ref RuleAttackWithWeapon __result){reactions++;lastReaction=__0;Check(__0.ExtraAttack&&__0.IsFirstAttack&&__0.AttacksCount==1&&!__0.SkipAnimation,"single animated native ranged reaction rule");__result=__0;return false;}
 static UnitEntityData Unit(BlueprintsCache c,string id){
  var u=(UnitEntityData)AccessTools.Method(typeof(WildShapeFeatTests),"Unit").Invoke(null,new object[]{c,id});
  Set(u,"CombatState",Empty<UnitCombatState>());Set(u.CombatState,"Unit",u);Set(u.CombatState,"Cooldown",new UnitCombatState.Cooldowns());Set(u.CombatState,"m_InCombat",true);
  Set(u.CombatState,"m_EngagedBy",new Dictionary<UnitEntityData,TimeSpan>());Set(u.CombatState,"m_EngagedUnits",new Dictionary<UnitEntityData,TimeSpan>());Set(u,"Commands",new UnitCommands(u));
  Set(u.State.Features,"SoloTactics",new Kingmaker.Utility.CountableFlag());return u;
 }
 static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)AccessTools.Method(typeof(WildShapeFeatTests),"Fact").Invoke(null,new object[]{u,bp});
 static void Call(EntityFact f,Type t,string m,params object[] args){var rt=f.Components.Single(x=>x.SourceBlueprintComponent.GetType()==t);using(rt.RequestEventContext())AccessTools.Method(t,m,args.Select(x=>x.GetType()).ToArray()).Invoke(rt.SourceBlueprintComponent,args);}
 static ItemEntityWeapon Weapon(UnitEntityData u,bool ranged,string id){var w=(ItemEntityWeapon)AccessTools.Method(typeof(WildShapeFeatTests),"Weapon").Invoke(null,new object[]{u,ranged?WeaponCategory.Longbow:WeaponCategory.Longsword,false,PhysicalDamageForm.Piercing,id});Set(w.Blueprint.Type,"m_AttackType",ranged?AttackType.Ranged:AttackType.Melee);w.Blueprint.Type.OnEnable();return w;}
 static RuleAttackWithWeapon Attack(UnitEntityData a,UnitEntityData t,ItemEntityWeapon w,bool charge=false,bool miss=false){var r=Empty<RuleAttackWithWeapon>();Set(r,"Initiator",a);Set(r,"Target",t);Set(r,"Weapon",w);Set(r,"AttacksCount",1);Set(r,"IsCharge",charge);var roll=Empty<RuleAttackRoll>();Set(roll,"Initiator",a);Set(roll,"Target",t);roll.AutoMiss=miss;Set(r,"AttackRoll",roll);return r;}
 static int Bonus(EntityFact f,Type t,UnitEntityData u,UnitEntityData enemy,ItemEntityWeapon w){var r=new RuleCalculateAttackBonus(u,enemy,w,0);Call(f,t,"OnEventAboutToTrigger",r);return r.AllBonuses.Sum(x=>x.Value);}
 static BlueprintFeatureReference Ref(SimpleBlueprint bp){var x=BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(bp);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(x,bp,null);return x;}
 static bool Need(Prerequisite p,UnitEntityData u)=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{null,u.Descriptor,null});
 internal static int Run(BlueprintsCache c){
  n=0;now=TimeSpan.FromSeconds(100);tb=false;var savedTime=Game.Instance.TimeController;Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());var h=new Harmony("CotW01411.TeamworkNative");
  void Patch(System.Reflection.MethodBase m,string s)=>h.Patch(m,prefix:new HarmonyMethod(typeof(TeamworkFeatTests),s));
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(Mode));
  Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"GetTargetsAround",new[]{typeof(Vector3),typeof(float),typeof(bool),typeof(bool)}),nameof(Targets));
  Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Yes));Patch(AccessTools.Method(typeof(UnitEntityData),"CanAttack",new[]{typeof(UnitEntityData)}),nameof(CanAttack));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.PropertyGetter(typeof(UnitCombatState),"IsFlanked"),nameof(Flanked));Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),nameof(Hit));
  Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),nameof(No));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleAttackWithWeapon),AccessTools.Method(typeof(TeamworkFeatTests),nameof(Reaction)));
  TeamworkReactionTestBoundary.Patch(h);
  try{
   var poolIds=(string[])AccessTools.Field(Catalog,"Pools").GetValue(null);
   foreach(string id in poolIds){var p=PsychicExpansionTests.NativeCase<BlueprintFeatureSelection>(id);var row=PsychicExpansionTests.NativeRows.Single(x=>(string)x["blueprint"]["AssetId"]==id);foreach(var name in new[]{"m_Features","m_AllFeatures","Group","Group2"}){var fld=AccessTools.Field(typeof(BlueprintFeatureSelection),name);fld.SetValue(p,PsychicExpansionTests.Read(row["blueprint"]["Data"][name],fld.FieldType));}c.AddCachedBlueprint(p.AssetGuid,p);}
   var originals=poolIds.Select(id=>((BlueprintFeatureReference[])Get(c.Load(BlueprintGuid.Parse(id)),"m_AllFeatures")).Select(x=>x.Guid).ToArray()).ToArray();Register(c);var tokens=(string[])AccessTools.Field(Module,"Tokens").GetValue(null);var feats=tokens.Select(t=>Asset<BlueprintFeature>(c,t)).ToArray();Register(c);
   for(int i=0;i<4;i++){Check(ReferenceEquals(c.Load(feats[i].AssetGuid),feats[i]),"stable registration identity");Check(feats[i].HasGroup(FeatureGroup.TeamworkFeat)&&feats[i].HasGroup(FeatureGroup.Feat)&&feats[i].HasGroup(FeatureGroup.CombatFeat)==(i<3),"correct teamwork/general/combat classification");Check(feats[i].ComponentsArray.Select(x=>x.name).Distinct().Count()==feats[i].ComponentsArray.Length,"component save names unique");}
   for(int j=0;j<poolIds.Length;j++){var p=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(poolIds[j]));foreach(string field in new[]{"m_Features","m_AllFeatures"})foreach(var f in feats)Check(((BlueprintFeatureReference[])Get(p,field)).Count(x=>x.Guid==f.AssetGuid)==1,"all ten native teamwork pools receive every feat once");Check(originals[j].All(x=>((BlueprintFeatureReference[])Get(p,"m_AllFeatures")).Any(z=>z.Guid==x)),"native teamwork options preserved");}
   var repair=typeof(CoordinatedShotBonus).Assembly.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolRepair");foreach(string id in (string[])AccessTools.Field(repair,"NativeIds").GetValue(null)){var p=c.Load(BlueprintGuid.Parse(id))as BlueprintFeatureSelection;if(p==null)continue;foreach(var f in feats.Take(3))Check(((BlueprintFeatureReference[])Get(p,"m_AllFeatures")).Count(x=>x.Guid==f.AssetGuid)==1,"new combat feats in native extra combat lists");if(p.AssetGuid.ToString()!="247a4068296e8be42890143f451b4b45")Check(!((BlueprintFeatureReference[])Get(p,"m_AllFeatures")).Any(x=>x.Guid==feats[3].AssetGuid),"Swarm Scatter absent from combat-only pool");}
   var clone=new BlueprintFeatureSelection{AssetGuid=Id("LateTeamworkFixture"),Group=FeatureGroup.TeamworkFeat};Set(clone,"m_AllFeatures",new[]{Ref((BlueprintFeature)c.Load(BlueprintGuid.Parse("422dab7309e1ad343935f33a4d6e9f11"))),Ref((BlueprintFeature)c.Load(BlueprintGuid.Parse("6337b37f2a7c11b4ab0831d6780bce2a")))});AccessTools.Method(Module,"RepairPool").Invoke(null,new object[]{c,clone});AccessTools.Method(Module,"RepairPool").Invoke(null,new object[]{c,clone});Check(feats.All(f=>((BlueprintFeatureReference[])Get(clone,"m_AllFeatures")).Count(x=>x.Guid==f.AssetGuid)==1),"late generic teamwork clone repaired once");
   bool reject=false;try{Register(new BlueprintsCache());}catch{reject=true;}Check(reject,"missing dependencies fail closed");
   var u=Unit(c,"teamwork-owner");var ally=Unit(c,"teamwork-ally");var ally2=Unit(c,"teamwork-ally2");var target=Unit(c,"teamwork-target");var other=Unit(c,"teamwork-other");world=new[]{u,ally,ally2};Set(u,"m_Position",Vector3.zero);Set(target,"m_Position",new Vector3(6,0,0));Set(ally,"m_Position",new Vector3(5,0,1));Set(ally2,"m_Position",new Vector3(5,0,-1));
   u.Stats.BaseAttackBonus.BaseValue=5;Check(!Need(feats[2].GetComponent<PrerequisiteStatValue>(),u),"Target BAB five rejected");u.Stats.BaseAttackBonus.BaseValue=6;Check(Need(feats[2].GetComponent<PrerequisiteStatValue>(),u),"Target BAB six accepted");Check(!Need(feats[0].GetComponent<PrerequisiteFeature>(),u),"Point Blank Shot required");Fact(u,(BlueprintFeature)c.Load(BlueprintGuid.Parse("0da0c194d6e1d43419eb8d990b28e0ab")));Check(Need(feats[0].GetComponent<PrerequisiteFeature>(),u),"native Point Blank Shot accepted");
   var ranged=Weapon(u,true,"teamwork-bow");var melee=Weapon(u,false,"teamwork-sword");var cs=Fact(u,feats[0]);((Dictionary<UnitEntityData,TimeSpan>)Get(target.CombatState,"m_EngagedBy"))[ally]=now;
   flanked=false;Check(Bonus(cs,typeof(CoordinatedShotBonus),u,target,ranged)==0,"partner needs feat");var allyShot=Fact(ally,feats[0]);Check(Bonus(cs,typeof(CoordinatedShotBonus),u,target,ranged)==1,"ranged plus one with threatening ally");flanked=true;Check(Bonus(cs,typeof(CoordinatedShotBonus),u,target,ranged)==2,"flanked target plus two");Check(Bonus(cs,typeof(CoordinatedShotBonus),u,target,melee)==0,"melee excluded");Set(ally,"m_Position",new Vector3(3,0,0));Check(Bonus(cs,typeof(CoordinatedShotBonus),u,target,ranged)==0,"ally covering firing line excluded");Set(ally,"m_Position",new Vector3(5,0,1));Fact(ally2,feats[0]);((Dictionary<UnitEntityData,TimeSpan>)Get(target.CombatState,"m_EngagedBy"))[ally2]=now;Check(Bonus(cs,typeof(CoordinatedShotBonus),u,target,ranged)==2,"multiple eligible partners do not stack");Set(allyShot,"IsTurnedOn",false);((Dictionary<UnitEntityData,TimeSpan>)Get(target.CombatState,"m_EngagedBy")).Remove(ally2);u.State.Features.SoloTactics.Retain();Check(Bonus(cs,typeof(CoordinatedShotBonus),u,target,ranged)==2,"Solo Tactics waives feat only");u.State.Features.SoloTactics.Release();
   var dc=Fact(u,feats[1]);Fact(ally,feats[1]);var hit=Attack(ally,target,melee,true);Call(dc,typeof(DistractingChargeOpening),"OnEventDidTrigger",hit);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==2,"charge hit opens next attack");Check(Bonus(dc,typeof(DistractingChargeOpening),u,other,ranged)==0,"unrelated target gets no bonus");Call(dc,typeof(DistractingChargeOpening),"OnEventDidTrigger",hit);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==2,"repeated charge opening no stacking");var fake=Attack(u,target,ranged).AttackRoll;Set(fake,"IsFake",true);Call(dc,typeof(DistractingChargeOpening),"OnEventDidTrigger",fake);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==2,"fake preview does not consume");Set(fake,"IsFake",false);fake.AutoMiss=true;Call(dc,typeof(DistractingChargeOpening),"OnEventDidTrigger",fake);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==0,"real miss consumes next roll");Call(dc,typeof(DistractingChargeOpening),"OnEventDidTrigger",hit);now+=TimeSpan.FromSeconds(7);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==0,"real-time six-second expiry");tb=true;Call(dc,typeof(DistractingChargeOpening),"OnEventDidTrigger",hit);now+=TimeSpan.FromSeconds(20);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==2,"turn-based waits for charging ally turn");Call(dc,typeof(DistractingChargeOpening),"HandleTurnStarted",other);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==2,"other turn retains opening");Call(dc,typeof(DistractingChargeOpening),"HandleTurnStarted",ally);Check(Bonus(dc,typeof(DistractingChargeOpening),u,target,ranged)==0,"charging ally next turn clears opening");tb=false;
   var rt=dc.Components.Single(x=>x.SourceBlueprintComponent is DistractingChargeOpening);var data=rt.GetData<ChargeOpeningData>();Call(dc,typeof(DistractingChargeOpening),"OnEventDidTrigger",hit);var saved=JsonConvert.DeserializeObject<ChargeOpeningData>(JsonConvert.SerializeObject(data));Check(saved.Openings.Count==1&&saved.Openings[0].Target.UniqueId==target.UniqueId&&saved.Openings[0].Expires==now+TimeSpan.FromSeconds(6),"opening save preserves unit identities and expiry");
   Fact(u,feats[2]);Fact(ally,feats[2]);AccessTools.Method(typeof(WildShapeFeatTests),"Equip").Invoke(null,new object[]{u.Body.PrimaryHand,ranged});var enabled=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(c,"TargetOpportunityEnabled"),u,8);var rangedHit=Attack(ally,target,ranged);reactions=0;Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",rangedHit);Check(reactions==1&&u.CombatState.Cooldown.SwiftAction==6,"actual native swift payment before single reaction");Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",rangedHit);Check(reactions==1,"spent swift blocks another reaction");u.CombatState.Cooldown.SwiftAction=0;Set(lastReaction,"Initiator",ally);Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",lastReaction);Check(reactions==1,"own generated reaction cannot start a chain even after synchronous resolution");rangedHit.ExtraAttack=true;Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",rangedHit);Check(reactions==2&&u.CombatState.Cooldown.SwiftAction==6,"ordinary extra ranged attack can trigger reaction");u.CombatState.Cooldown.SwiftAction=0;rangedHit.ExtraAttack=false;Set(target,"m_Position",new Vector3(9.15f,0,0));Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",rangedHit);Check(reactions==2&&u.CombatState.Cooldown.SwiftAction==0,"out of thirty feet neither fires nor spends");Set(target,"m_Position",new Vector3(9,0,0));canAttack=false;Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",rangedHit);Check(reactions==2&&u.CombatState.Cooldown.SwiftAction==0,"native CanAttack rejection preserves action");canAttack=true;Set(enabled,"IsTurnedOn",false);Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",rangedHit);Check(reactions==2,"toggle off disables reaction");Set(enabled,"IsTurnedOn",true);u.Commands.Raw[(int)Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift]=Empty<UnitUseAbility>();Call(enabled,typeof(TargetOpportunityReaction),"OnEventDidTrigger",rangedHit);Check(reactions==2,"queued swift excluded");u.Commands.Raw[(int)Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift]=null;
   var ss=Fact(u,feats[3]);var af=Fact(ally,feats[3]);Fact(ally2,feats[3]);Set(ally,"m_Position",new Vector3(1,0,0));Set(ally2,"m_Position",new Vector3(-1,0,0));Call(ss,typeof(SwarmScatterDefense),"OnTurnOn");Check(u.Stats.AC.ModifiedValue==12,"two adjacent allies give plus two AC");Call(ss,typeof(SwarmScatterDefense),"OnNewRound");Check(u.Stats.AC.ModifiedValue==12&&u.Stats.AC.Modifiers.Count(m=>m.Source==ss)==1,"repeated refresh has one modifier");Call(ss,typeof(SwarmScatterDefense),"OnApplyPostLoadFixes");Check(u.Stats.AC.ModifiedValue==12&&u.Stats.AC.Modifiers.Count(m=>m.Source==ss)==1,"post-load recompute no duplicate AC");Set(ally2,"m_Position",new Vector3(-3,0,0));Call(ss,typeof(SwarmScatterDefense),"HandleEntityPositionChanged",ally2);Check(u.Stats.AC.ModifiedValue==11,"movement immediately updates AC");
   var patch=typeof(CoordinatedShotBonus).Assembly.GetType("CallOfTheWild.Wrath.Prototype.SwarmScatterPatch");h.CreateClassProcessor(patch).Patch();h.Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),prefix:new HarmonyMethod(typeof(TeamworkFeatTests),nameof(ActionBoundary)){priority=0});h.Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),prefix:new HarmonyMethod(typeof(TeamworkFeatTests),nameof(ActionBoundary)){priority=0});
   var rats=(string[])AccessTools.Field(Catalog,"RatUnits").GetValue(null);var damage=new ContextActionDealDamage();var nausea=new ContextActionApplyBuff();Set(nausea,"m_Buff",BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>((BlueprintBuff)c.Load(BlueprintGuid.Parse("956331dba5125ef48afe41875a00ca0e"))));
   foreach(string id in rats){var bp=PsychicExpansionTests.NativeCase<BlueprintUnit>(id);Set(other.Descriptor,"Blueprint",bp);var ctx=new MechanicsContext(other,other,(BlueprintScriptableObject)c.Load(BlueprintGuid.Parse("1700dc1592c4a67418d621c7eb4aa4ae")),null);boundary=0;using(ctx.GetDataScope(u)){damage.RunAction();nausea.RunAction();}Check(boundary==0,"native rat variant attack damage and distraction suppressed");}
   var ratContext=new MechanicsContext(other,other,(BlueprintScriptableObject)c.Load(BlueprintGuid.Parse("3ccdcb9a6a9a436408743daa22e0ddd9")),null);Set(nausea,"m_Buff",BlueprintReferenceBase.CreateTyped<BlueprintBuffReference>((BlueprintBuff)c.Load(BlueprintGuid.Parse("e3c01ec2609ca7b42adc2bf37b6a5d8d"))));boundary=0;using(ratContext.GetDataScope(u))nausea.RunAction();Check(boundary==1,"disease or unrelated buff preserved");var unrelated=new MechanicsContext(other,other,feats[3],null);boundary=0;using(unrelated.GetDataScope(u))damage.RunAction();Check(boundary==1,"rat unrelated source damage preserved");Set(af,"IsTurnedOn",false);Call(ss,typeof(SwarmScatterDefense),"OnNewRound");Check(u.Stats.AC.ModifiedValue==10,"no active adjacent ally removes AC");boundary=0;using(ratContext.GetDataScope(u))damage.RunAction();Check(boundary==1,"no adjacent eligible ally no swarm immunity");Call(ss,typeof(SwarmScatterDefense),"OnTurnOff");Check(!u.Stats.AC.Modifiers.Any(m=>m.Source==ss),"feat removal cleans only owned AC");
   PsychicExpansionTests.Inventory["teamwork01411Assertions"]=n;PsychicExpansionTests.Inventory["teamwork01411Assets"]=new JArray(feats.Select(f=>new JObject{["name"]=f.name,["guid"]=f.AssetGuid.ToString()}));return n;
  }finally{h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",savedTime);world=Array.Empty<UnitEntityData>();}
 }
}
