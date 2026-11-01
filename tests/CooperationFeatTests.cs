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
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using UnityEngine;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

internal static class CooperationFeatTests {
 static readonly Type Module=typeof(BrokenWingGambit).Assembly.GetType("CallOfTheWild.Wrath.Prototype.CooperationFeats");
 static readonly Type Sharing=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BondedSpellSharing");
 static int n,aoo,ranged,saves,applications,lastDC;static bool tb,pass,reach=true,canAttack=true;static TimeSpan now;static UnitEntityData enemy;static UnitEntityData[] world=Array.Empty<UnitEntityData>();static RuleAttackWithWeapon lastReaction;
 static object Get(object o,string f)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).GetValue(o);
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool b,string s){if(!b)throw new Exception("Cooperation01413: "+s);n++;}
 static object Helper(string method,params object[] args)=>AccessTools.Method(typeof(TeamworkFeatTests),method).Invoke(null,args);
 static object Share(string method,params object[] args)=>AccessTools.Method(Sharing,method).Invoke(null,args);
 static T Asset<T>(BlueprintsCache c,string t)where T:SimpleBlueprint=>Psychic034Tests.Asset<T>(c,t);
 static UnitEntityData Unit(BlueprintsCache c,string id){var u=(UnitEntityData)Helper("Unit",c,id);Set(u.Descriptor,"Stats",new CharacterStats(u.Descriptor));Set(u.Descriptor,"m_Spellbooks",new Dictionary<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook,Spellbook>());return u;}
 static Feature Fact(UnitEntityData u,BlueprintFeature bp)=>(Feature)Helper("Fact",u,bp);
 static void Call(EntityFact f,Type type,string method,params object[] args)=>Helper("Call",f,type,method,args);
 static bool Yes(ref bool __result){__result=true;return false;}
 static bool No(ref bool __result){__result=false;return false;}
 static bool Skip()=>false;
 static bool Ally(UnitEntityData __instance,UnitEntityData __0,ref bool __result){__result=(__instance==enemy)==(__0==enemy);return false;}
 static bool Mode(ref bool __result){__result=tb;return false;}
 static bool Time(ref TimeSpan __result){__result=now;return false;}
 static bool Targets(ref IEnumerable<UnitEntityData> __result){__result=world;return false;}
 static bool Hit(RuleAttackRoll __instance,ref bool __result){__result=!__instance.AutoMiss;return false;}
 static bool Reach(ref bool __result){__result=reach;return false;}
 static bool CanAttack(ref bool __result){__result=canAttack;return false;}
 static bool Aoo(ref bool __result){aoo++;__result=true;return false;}
 static bool Reaction(RuleAttackWithWeapon __0,ref RuleAttackWithWeapon __result){ranged++;lastReaction=__0;Check(__0.ExtraAttack&&__0.IsFirstAttack&&__0.AttacksCount==1&&!__0.SkipAnimation,"one native animated ranged response");__result=__0;return false;}
 static bool Save(RuleSavingThrow __0,ref RuleSavingThrow __result){saves++;lastDC=__0.DifficultyClassBase;__result=__0;return false;}
 static bool Passed(ref bool __result){__result=pass;return false;}
 static bool Apply(ContextActionApplyBuff __instance){applications++;Check(__instance.Buff.AssetGuid.ToString()=="09d39b38bb7c6014394b6daced9bacd3"&&__instance.DurationValue.BonusValue.Value==1&&__instance.IsNotDispelable,"native stunned buff, one round and nondispellable");return false;}
 static RuleAttackWithWeapon Attack(UnitEntityData u,UnitEntityData t,ItemEntityWeapon w,bool miss=false)=>(RuleAttackWithWeapon)Helper("Attack",u,t,w,false,miss);
 static int Bonus(Feature f,UnitEntityData u,UnitEntityData t,ItemEntityWeapon w)=>(int)Helper("Bonus",f,typeof(BrokenWingGambit),u,t,w);
 static bool Need(Prerequisite p,UnitEntityData u)=>(bool)Helper("Need",p,u);
 internal static int Run(BlueprintsCache c){
  n=0;now=TimeSpan.FromSeconds(100);tb=false;var savedTime=Game.Instance.TimeController;Set(Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());var h=new Harmony("CotW01413.NativeCooperation");
  void Patch(System.Reflection.MethodBase m,string s)=>h.Patch(m,prefix:new HarmonyMethod(typeof(CooperationFeatTests),s));
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.Method(typeof(CombatController),"IsInTurnBasedCombat"),nameof(Mode));
  Patch(AccessTools.Method(typeof(Kingmaker.Designers.GameHelper),"GetTargetsAround",new[]{typeof(Vector3),typeof(float),typeof(bool),typeof(bool)}),nameof(Targets));
  Patch(AccessTools.Method(typeof(UnitEntityData),"IsAlly",new[]{typeof(UnitEntityData)}),nameof(Ally));Patch(AccessTools.Method(typeof(UnitEntityData),"CanAttack",new[]{typeof(UnitEntityData)}),nameof(CanAttack));
  Patch(AccessTools.PropertyGetter(typeof(UnitState),"CanAct"),nameof(Yes));Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),nameof(Hit));
  Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),nameof(Skip));Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Armies.TacticalCombat.TacticalCombatHelper),"IsActive"),nameof(No));
  Patch(AccessTools.Method(typeof(UnitHelper),"IsAttackOfOpportunityReach"),nameof(Reach));Patch(AccessTools.PropertyGetter(typeof(UnitCombatState),"CanAttackOfOpportunity"),nameof(Yes));Patch(AccessTools.Method(typeof(UnitCombatState),"AttackOfOpportunity"),nameof(Aoo));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleAttackWithWeapon),AccessTools.Method(typeof(CooperationFeatTests),nameof(Reaction)));PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(CooperationFeatTests),nameof(Save)));
  TeamworkReactionTestBoundary.Patch(h);
  Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));
  try{
   AccessTools.Method(Module,"Register").Invoke(null,new object[]{c});var tokens=(string[])AccessTools.Field(Module,"Tokens").GetValue(null);var feats=tokens.Select(t=>Asset<BlueprintFeature>(c,t)).ToArray();AccessTools.Method(Module,"Register").Invoke(null,new object[]{c});
   for(int i=0;i<5;i++){Check(ReferenceEquals(c.Load(feats[i].AssetGuid),feats[i]),"repeat registration preserves identity");Check(feats[i].HasGroup(FeatureGroup.Feat)&&feats[i].HasGroup(FeatureGroup.TeamworkFeat)==(i<4)&&feats[i].HasGroup(FeatureGroup.CombatFeat)==(i>=2),"five roots correctly classified");Check(feats[i].ComponentsArray.Select(x=>x.name).Distinct().Count()==feats[i].ComponentsArray.Length,"unique save component names");}
   var repair=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolRepair");foreach(string id in (string[])AccessTools.Field(repair,"NativeIds").GetValue(null)){var p=c.Load(BlueprintGuid.Parse(id))as BlueprintFeatureSelection;if(p==null)continue;foreach(var f in feats.Skip(2))foreach(string field in new[]{"m_Features","m_AllFeatures"})Check(((BlueprintFeatureReference[])Get(p,field)).Count(x=>x.Guid==f.AssetGuid)==1,"three combat roots in native combat selection once");}
   foreach(string id in (string[])AccessTools.Field(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TeamworkCatalog"),"Pools").GetValue(null)){var p=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(id));foreach(var f in feats.Take(4))foreach(string field in new[]{"m_Features","m_AllFeatures"})Check(((BlueprintFeatureReference[])Get(p,field)).Count(x=>x.Guid==f.AssetGuid)==1,"four teamwork roots in all ten native lists once");}
   var u=Unit(c,"01413-baiter");var ally=Unit(c,"01413-partner");enemy=Unit(c,"01413-enemy");var other=Unit(c,"01413-unbonded");world=new[]{u,ally};Set(u,"m_Position",Vector3.zero);Set(ally,"m_Position",new Vector3(1,0,0));Set(enemy,"m_Position",new Vector3(2,0,0));
   u.Stats.SkillPersuasion.BaseValue=4;Check(!Need(feats[2].GetComponent<PrerequisiteStatValue>(),u),"four skill ranks rejected");u.Stats.SkillPersuasion.BaseValue=5;Check(Need(feats[2].GetComponent<PrerequisiteStatValue>(),u),"five skill ranks accepted");
   u.Stats.BaseAttackBonus.BaseValue=15;var bab=feats[4].GetComponents<PrerequisiteStatValue>().Single(x=>x.Stat==StatType.BaseAttackBonus);Check(!Need(bab,u),"BAB15 rejected");u.Stats.BaseAttackBonus.BaseValue=16;Check(Need(bab,u),"BAB16 accepted");Check(!Need(feats[4].GetComponent<PrerequisiteFeature>(),u),"native Power Attack required");Fact(u,(BlueprintFeature)c.Load(BlueprintGuid.Parse("9972f33f977fc724c838e59641b2fca5")));Check(Need(feats[4].GetComponent<PrerequisiteFeature>(),u),"Power Attack accepted");
   var bond=Fact(u,feats[0]);var bond2=Fact(ally,feats[0]);var sharing=Fact(u,feats[1]);var enabled=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(c,"BondedShareEnabled"),u,10);
   var cls=(BlueprintCharacterClass)c.Load(BlueprintGuid.Parse("ba34257984f4c41408ce1dc2004e342e"));var book=(Spellbook)AccessTools.Method(typeof(SpellResourceFeatTests),"Book").Invoke(null,new object[]{u,true,cls});
   var spellBP=new BlueprintAbility{Type=AbilityType.Spell,Range=AbilityRange.Personal,CanTargetSelf=true,ComponentsArray=Array.Empty<BlueprintComponent>(),name="01413PersonalFixture"};var spell=(AbilityData)AccessTools.Method(typeof(SpellResourceFeatTests),"Spell").Invoke(null,new object[]{u,book,2,spellBP});
   foreach(string patch in new[]{"BondedSpellRangePatch","BondedSpellAnchorPatch","BondedSpellTargetPatch","BondedSpellTargetReasonPatch","BondedSpellUnitValidationPatch","BondedSpellDeliveryPatch","BondedSpellBuffTargetPatch"})h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype."+patch)).Patch();
   Check(spell.Range==AbilityRange.Touch&&spell.TargetAnchor==AbilityTargetAnchor.Unit,"personal spell becomes touch/unit without blueprint mutation");Check(spellBP.Range==AbilityRange.Personal&&!spellBP.CanTargetFriends,"shared native blueprint remains personal/self");Check(spell.CanTarget(new TargetWrapper(ally))&&!spell.CanTarget(new TargetWrapper(other))&&!spell.CanTarget(new TargetWrapper(enemy)),"only permanently bonded ally target allowed");
   Check(spell.CantTargetReason(new TargetWrapper(ally))==""&&spell.CantTargetReason(new TargetWrapper(other)).Contains("Bonded Mind"),"target reason follows same eligibility as CanTarget");Check((bool)AccessTools.Method(typeof(AbilityData),"ValidateUnit").Invoke(spell,new object[]{ally})&&!(bool)AccessTools.Method(typeof(AbilityData),"ValidateUnit").Invoke(spell,new object[]{other}),"native affected-unit validation agrees with bonded target selection");Set(ally.State.Features,"IsUntargetable",new CountableFlag());ally.State.Features.IsUntargetable.Retain();Check(!spell.CanTarget(new TargetWrapper(ally)),"native untargetable state remains respected");ally.State.Features.IsUntargetable.Release();
   bond2.IsTemporary=true;u.State.Features.SoloTactics.Retain();Check(!spell.CanTarget(new TargetWrapper(ally)),"temporary feat and Solo Tactics cannot establish bond");bond2.IsTemporary=false;u.State.Features.SoloTactics.Release();
   Set(enabled,"IsTurnedOn",false);Check(spell.Range==AbilityRange.Touch,"legacy toggle off does not disable permanent passive sharing");Set(enabled,"IsTurnedOn",true);sharing.IsTemporary=true;Check(spell.Range==AbilityRange.Personal,"temporary Share Spells excluded");sharing.IsTemporary=false;spell.OverrideRange=AbilityRange.Close;Check(spell.Range==AbilityRange.Close,"existing range override preserved");spell.OverrideRange=null;
   var execution=new AbilityExecutionContext(spell,new AbilityParams{CasterLevel=9,SpellLevel=2},new TargetWrapper(ally));Share("Record",execution,new TargetWrapper(ally));Set(enabled,"IsTurnedOn",false);var action=new ContextActionApplyBuff{ToCaster=true};Check((UnitEntityData)AccessTools.Method(typeof(ContextActionApplyBuff),"GetBuffTarget").Invoke(action,new object[]{execution})==ally&&execution.MaybeCaster==u,"caster-directed initial buff moves to recipient; caster identity retained across later toggle changes");var child=new MechanicsContext(u,ally,Asset<BlueprintBuff>(c,"BondedShareEnabled"),execution);Check((UnitEntityData)AccessTools.Method(typeof(ContextActionApplyBuff),"GetBuffTarget").Invoke(action,new object[]{child})==u,"ongoing child buff contexts preserve original caster");Set(enabled,"IsTurnedOn",true);
   var prereq=feats[1].GetComponent<PrerequisiteBondedSharing>();Check(!Need(prereq,u),"requires actual sharing capability and caster level");var familiarId=(string[])AccessTools.Field(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.CooperationCatalog"),"Familiars").GetValue(null);foreach(string id in familiarId){var familiar=Fact(u,(BlueprintFeature)c.Load(BlueprintGuid.Parse(id)));Check((bool)AccessTools.Method(Module,"SharingCapability").Invoke(null,new object[]{u}),"native selected familiar qualifies");Set(familiar,"IsTurnedOn",false);}
   var petBP=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse("12345678901234567890123456789012"),ComponentsArray=new BlueprintComponent[]{new AddPet{Type=PetType.AnimalCompanion}}};Fact(u,petBP);Check((bool)AccessTools.Method(Module,"SharingCapability").Invoke(null,new object[]{u}),"animal companion learned fact qualifies without active pet entity");
   Set(book,"MaxCasterLevel",20);Set(book,"m_BaseLevelInternal",1);((Dictionary<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook,Spellbook>)Get(u.Descriptor,"m_Spellbooks"))[book.Blueprint]=book;Check(book.CasterLevel==1&&Need(prereq,u),"real caster level one plus native companion capability meets Share Spells prerequisite");Set(book,"m_BaseLevelInternal",0);Check(!Need(prereq,u),"caster level zero rejected even with companion and permanent bond");Set(book,"m_BaseLevelInternal",1);bond.IsTemporary=true;Check(!Need(prereq,u),"temporary Bonded Mind cannot qualify for feat");bond.IsTemporary=false;
   var melee=(ItemEntityWeapon)Helper("Weapon",u,false,"01413-melee");var bow=(ItemEntityWeapon)Helper("Weapon",ally,true,"01413-bow");AccessTools.Method(typeof(WildShapeFeatTests),"Equip").Invoke(null,new object[]{ally.Body.PrimaryHand,melee});var offer=Fact(u,feats[2]);var allyWing=Fact(ally,feats[2]);var wingEnabled=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(c,"BrokenWingEnabled"),u,10);var ownHit=Attack(u,enemy,melee);Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);Check(Bonus(offer,enemy,u,melee)==2&&Bonus(offer,enemy,ally,melee)==0,"opponent plus two only against offering character");
   var stats=new RuleCalculateWeaponStats(enemy,melee,Attack(enemy,u,melee),null);Call(offer,typeof(BrokenWingGambit),"OnEventAboutToTrigger",stats);Check(stats.DamageModifiers.Modifiers.Sum(x=>x.Value)==2,"native weapon damage plus two");
   var incoming=Attack(enemy,u,melee,true);aoo=0;Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",incoming);Check(aoo==1&&Bonus(offer,enemy,u,melee)==0,"miss accepts and consumes one offer; eligible ally gets native AoO");Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",incoming);Check(aoo==1,"offer cannot be reused by later attacks");Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);reach=false;Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",incoming);Check(aoo==1,"out-of-reach ally does not receive synthetic opportunity attack");reach=true;
   Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);now+=TimeSpan.FromSeconds(7);Check(Bonus(offer,enemy,u,melee)==0,"real-time offer expires at six seconds");tb=true;Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);var data=offer.Components.Single(x=>x.SourceBlueprintComponent is BrokenWingGambit).GetData<GambitData>();data.Offers[0].RemainingTurnEnds=2;now+=TimeSpan.FromSeconds(20);Check(Bonus(offer,enemy,u,melee)==2,"turn-based offer ignores real-time expiry");var end=Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.GambitTurnEndPatch");AccessTools.Method(end,"End").Invoke(null,new object[]{u});Check(Bonus(offer,enemy,u,melee)==2,"current owner turn end preserves offer");AccessTools.Method(end,"End").Invoke(null,new object[]{u});Check(Bonus(offer,enemy,u,melee)==0,"end of next owner turn removes offer");tb=false;
   Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);var saved=JsonConvert.DeserializeObject<GambitData>(JsonConvert.SerializeObject(data));Check(saved.Offers.Single().Enemy.UniqueId==enemy.UniqueId&&saved.Offers[0].Expires==now+TimeSpan.FromSeconds(6),"save preserves offer target and expiry");
   Fact(ally,feats[3]);var pawEnabled=Psychic034Tests.Fact(ally,Asset<BlueprintBuff>(c,"WoundedPawEnabled"),ally,10);AccessTools.Method(typeof(WildShapeFeatTests),"Equip").Invoke(null,new object[]{ally.Body.PrimaryHand,bow});ranged=0;Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",incoming);Check(ranged==1&&aoo==1&&ally.CombatState.Cooldown.SwiftAction==6,"ranged response spends actual swift and excludes same-opening AoO");Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",incoming);Check(ranged==1,"spent swift blocks repeated ranged response");ally.CombatState.Cooldown.SwiftAction=0;Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);Set(enemy,"m_Position",new Vector3(12,0,0));Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",incoming);Check(ranged==1&&ally.CombatState.Cooldown.SwiftAction==0,"thirty-foot range rejects without action spending");Set(enemy,"m_Position",new Vector3(2,0,0));
   Set(lastReaction,"Initiator",u);Set(lastReaction,"Weapon",melee);Set(lastReaction,"AttackRoll",ownHit.AttackRoll);Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",lastReaction);Check(data.Offers.Count==0,"generated ranged reaction cannot offer another gambit");Set(wingEnabled,"IsTurnedOn",false);Call(offer,typeof(BrokenWingGambit),"OnEventDidTrigger",ownHit);Check(data.Offers.Count==0,"consent off prevents fresh offers");
   Fact(u,feats[4]);var armed=Psychic034Tests.Fact(u,Asset<BlueprintBuff>(c,"StunningAssaultArmed"),u,1);var ab=new RuleCalculateAttackBonusWithoutTarget(u,melee,0);Call(armed,typeof(StunningAssaultEffect),"OnEventAboutToTrigger",ab);Check(ab.AllBonuses.Sum(x=>x.Value)==-5,"melee attack minus five");var rab=new RuleCalculateAttackBonusWithoutTarget(u,bow,0);Call(armed,typeof(StunningAssaultEffect),"OnEventAboutToTrigger",rab);Check(!rab.AllBonuses.Any(),"ranged attack unaffected");var cmb=new RuleCalculateCMB(u,enemy,CombatManeuver.Trip);Call(armed,typeof(StunningAssaultEffect),"OnEventAboutToTrigger",cmb);Check(cmb.AllBonuses.Sum(x=>x.Value)==-5,"all maneuver checks minus five");
   Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));saves=applications=0;pass=true;Call(armed,typeof(StunningAssaultEffect),"OnEventDidTrigger",ownHit);Check(saves==1&&applications==0&&lastDC==26,"BAB16 means Fort DC26; success no stun");pass=false;Call(armed,typeof(StunningAssaultEffect),"OnEventDidTrigger",ownHit);Check(saves==2&&applications==1,"failed save applies native one-round stun");Call(armed,typeof(StunningAssaultEffect),"OnEventDidTrigger",Attack(u,enemy,bow));Call(armed,typeof(StunningAssaultEffect),"OnEventDidTrigger",Attack(u,enemy,melee,true));Set(ownHit.AttackRoll,"IsFake",true);Call(armed,typeof(StunningAssaultEffect),"OnEventDidTrigger",ownHit);Check(saves==2,"ranged, miss and preview cause no saving throw");Set(armed,"IsTurnedOn",false);Call(armed,typeof(StunningAssaultEffect),"OnEventAboutToTrigger",new RuleCalculateCMB(u,enemy,CombatManeuver.Trip));Check(!Asset<BlueprintAbility>(c,"StunningAssaultAbility").GetComponents<AbilityVariants>().Any(),"stun has one activation and no early-off variants");
   PsychicExpansionTests.Inventory["cooperation01413Assertions"]=n;PsychicExpansionTests.Inventory["cooperation01413Assets"]=new JArray(feats.Select(f=>new JObject{["name"]=f.name,["guid"]=f.AssetGuid.ToString()}));Console.WriteLine("PASS: "+n+" new cooperation/assault native selection, permanent prerequisites, targeting, offers, action payments and save/DC checks; animated execution and final buff delivery bounded.");return n;
  }finally{h.UnpatchAll(h.Id);Set(Game.Instance,"TimeController",savedTime);world=Array.Empty<UnitEntityData>();enemy=null;}
 }
}
