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

internal static class PsychicOrbTests
{
 static int count,saves,hits,spawns,applies;static bool pass,fortPass;static double now;static EntityFactsManager facts;static MechanicsContext spawnedContext;static TimeSpan spawnedDuration;static readonly Dictionary<string,AreaEffectEntityData> entities=new Dictionary<string,AreaEffectEntityData>();static readonly List<AreaEffectEntityData> ended=new List<AreaEffectEntityData>();
 static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
 static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
 static void Check(bool ok,string why){if(!ok)throw new Exception("Orbs: "+why);count++;}
 static T Asset<T>(BlueprintsCache c,string n) where T:SimpleBlueprint=>(T)c.Load((BlueprintGuid)AccessTools.Method(typeof(PsychicOrbController).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{n}));
 static void Refresh(){var c=Get(facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
 static void Clear(){facts.List.Clear();Refresh();}
 static MechanicsContext Context(UnitEntityData a,BlueprintScriptableObject bp,int cl=10,Metamagic mm=0){var ctx=new MechanicsContext(a,a.Descriptor,bp,null,new TargetWrapper(a));ctx.SetParams(new AbilityParams {CasterLevel=cl,DC=23,SpellLevel=4,Metamagic=mm});var ranks=new int[32];foreach(var rank in bp.GetComponents<ContextRankConfig>())ranks[(int)(AbilityRankType)Get(rank,"m_Type")]=rank.GetValue(ctx);Set(ctx,"m_Ranks",ranks);return ctx;}
 static Buff Fact(BlueprintBuff bp,MechanicsContext ctx,double seconds=60){var f=Empty<Buff>();Set(f,"<Blueprint>k__BackingField",bp);Set(f,"Components",new List<EntityFactComponent>());Set(f,"m_Context",ctx);Set(f,"<Rank>k__BackingField",1);Set(f,"<IsActive>k__BackingField",true);Set(f,"<IsTurnedOn>k__BackingField",true);Set(f,"m_EndTime",(TimeSpan?)TimeSpan.FromSeconds(now+seconds));f.RestoreOwnerLink(facts);var comp=bp.GetComponent<PsychicOrbController>();var rt=comp.CreateRuntimeFactComponent();rt.Setup(comp);rt.RestoreOwnerLink(f);Set(rt,"<IsTurnedOn>k__BackingField",true);((List<EntityFactComponent>)Get(f,"Components")).Add(rt);facts.List.Add(f);Refresh();return f;}
 static readonly string[] tokens={"FlamingSphere","FlamingSphereGreater","AggressiveThundercloud","AggressiveThundercloudGreater","BallLightning"};
 internal static int Run(BlueprintsCache cache){
  var u=new Psychic070Tests.Caster(cache).Unit;facts=new EntityFactsManager(u);Set(u,"Facts",facts);var buffs=new BuffCollection();buffs.Initialize(facts);Set(u,"<Buffs>k__BackingField",buffs);
  var oldTime=Kingmaker.Game.Instance.TimeController;Set(Kingmaker.Game.Instance,"TimeController",Empty<Kingmaker.Controllers.TimeController>());var h=new Harmony("CotW01320.OrbBoundaries");
  void Patch(System.Reflection.MethodBase m,string n)=>h.Patch(m,prefix:new HarmonyMethod(typeof(PsychicOrbTests),n){priority=Priority.First});
  Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Controllers.TimeController),"GameTime"),nameof(Time));Patch(AccessTools.Method(typeof(ContextActionSpawnAreaEffect),"RunAction"),nameof(Spawn));Patch(AccessTools.Method(typeof(ContextActionDealDamage),"RunAction"),nameof(Damage));Patch(AccessTools.Method(typeof(ContextActionApplyBuff),"RunAction"),nameof(Apply));Patch(AccessTools.PropertyGetter(typeof(RuleSavingThrow),"IsPassed"),nameof(Passed));Patch(AccessTools.Method(typeof(AreaEffectEntityData),"ForceEnd"),nameof(End));
  PsychicRuleTestBoundary.Patch(h,typeof(RuleSavingThrow),AccessTools.Method(typeof(PsychicOrbTests),nameof(Save)));
  try{Metadata(cache,u);Lifecycle(cache,u);Cleanup(cache,u);Unique(cache,u,h);}finally{h.UnpatchAll(h.Id);Set(Kingmaker.Game.Instance,"TimeController",oldTime);Clear();}
  PsychicExpansionTests.Inventory["orbSpellAssertions"]=count;Console.WriteLine("PASS: "+count+" orb checks; native CallComponents/data, cooldown, save branches, original casting context, durations, rank thresholds and source cleanup; scene spawn/damage endpoints substituted.");return count;
 }
 static void Metadata(BlueprintsCache c,UnitEntityData u){for(int i=0;i<tokens.Length;i++){
  var token=tokens[i];var root=Asset<BlueprintAbility>(c,token);var host=Asset<BlueprintBuff>(c,token+"Controller");var area=Asset<BlueprintAbilityAreaEffect>(c,token+"Area");var move=Asset<BlueprintAbility>(c,token+"Move");
  Check(root.GetComponents<SpellListComponent>().Count()==3&&root.GetComponents<SpellListComponent>().All(x=>x.SpellLevel==(i==0||i==2?2:4)),"Druid/Magus/Wizard levels");
  foreach(var list in root.GetComponents<SpellListComponent>())Check(list.SpellList.SpellsByLevel[list.SpellLevel].Spells.Count(x=>x==root)==1,"Unique reciprocal list");
  Check(move.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move&&move.Type==AbilityType.Supernatural&&move.GetComponent<PsychicOrbReady>()!=null,"Native move action restriction");Check(host.GetComponent<AddFacts>().Facts.Single()==move&&area.GetComponent<UniqueAreaEffect>().Feature==host,"Temporary ability and native unique area share host");
  var run=area.GetComponent<AbilityAreaEffectRunAction>();Check(run.UnitEnter.Actions.Single()==run.Round.Actions.Single()&&run.UnitMove.Actions.Length==0&&run.UnitExit.Actions.Length==0&&area.Size.Value==2&&area.SpellResistance,"Entry/round only; SR and2ft");
  var save=(ContextActionSavingThrow)((PsychicOrbPulse)run.Round.Actions.Single()).Effects.Actions.Single();Check(save.Type==SavingThrowType.Reflex,"Reflex negates");var split=(ContextActionConditionalSaved)save.Actions.Actions.Single();Check(split.Succeed.Actions.Length==0,"Successful Reflex no damage");var damage=(ContextActionDealDamage)split.Failed.Actions[0];Check(damage.IgnoreCritical&&damage.DisableSneakDamage&&damage.IsAoE&&damage.DamageType.Type==Kingmaker.RuleSystem.Rules.Damage.DamageType.Energy,"No weapon crit/sneak");
  var apply=(ContextActionApplyBuff)root.GetComponent<AbilityEffectRunAction>().Actions.Actions[0];foreach(int cl in new[]{1,5,10,11,14,15,18,19,30})foreach(bool ext in new[]{false,true}){
   var ctx=Context(u,root,cl,ext?Metamagic.Extend:0);Check(apply.ToCaster&&apply.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(cl*6*(ext?2:1)),"Controller CLrounds Extend once");var ac=ctx.CloneFor(area,null,null);ac.Recalculate();Check(ac.Params.CasterLevel==cl&&ac.Params.DC==23,"Native cloned area retains CL/DC");Check(damage.Value.DiceCountValue.Calculate(ac)==(i==4?(cl<11?6:cl<15?9:cl<19?12:15):(i==1||i==3?6:3)),"Damage ranks and cap");
  }
  if(i==1){var burn=(ContextActionApplyBuff)split.Failed.Actions[1];Check(burn.Permanent&&burn.IsNotDispelable&&burn.NotLinkToAreaEffect,"Burn independent of sphere expiry/move");}
  if(i==2||i==3){var fog=area.GetComponent<AbilityAreaEffectBuff>();Check(fog.Buff.Stacking==StackingType.Stack&&fog.Buff.GetComponent<AddConcealment>().Concealment==Concealment.Partial,"Source-owned cloud concealment");}
  if(i==3){var stun=((PsychicOrbFirstThunder)split.Failed.Actions[1]).Stun.Get();Check(stun.GetComponents<SpellDescriptorComponent>().Any(x=>((SpellDescriptor)x.Descriptor&SpellDescriptor.Sonic)!=0),"Sonic stun retains native descriptor");}
  if(i==4){var increases=(ContextActionSavingThrow.ConditionalDCIncrease[])Get(save,"m_ConditionalDCIncrease");Check(increases.Length==1&&increases[0].Value.Calculate(Context(u,root))==4&&increases[0].Condition.Conditions.Single() is PsychicMetalArmor,"Metal armor native conditional DC+4");}
 }}
 static void Lifecycle(BlueprintsCache c,UnitEntityData u){
  for(int i=0;i<tokens.Length;i++)foreach(bool success in new[]{false,true})foreach(bool fort in new[]{false,true}){
   Clear();now=100;pass=success;fortPass=fort;saves=hits=applies=spawns=0;var token=tokens[i];var root=Asset<BlueprintAbility>(c,token);var bp=Asset<BlueprintBuff>(c,token+"Controller");var original=Context(u,root,13,Metamagic.Extend|Metamagic.Maximize);var host=Fact(bp,original,156);var area=Asset<BlueprintAbilityAreaEffect>(c,token+"Area");var run=area.GetComponent<AbilityAreaEffectRunAction>();var ready=Asset<BlueprintAbility>(c,token+"Move").GetComponent<PsychicOrbReady>();var place=Asset<BlueprintAbility>(c,token+"Move").GetComponent<AbilityEffectRunAction>().Actions;
   Check(ready.IsCasterRestrictionPassed(u),"Native CallComponents invokes saved active controller");
   var moveContext=Context(u,Asset<BlueprintAbility>(c,token+"Move"),1);using(moveContext.GetDataScope(new TargetWrapper(new UnityEngine.Vector3(8,0,4))))place.Run();Check(spawns==1&&spawnedContext==original&&spawnedContext.Params.CasterLevel==13&&spawnedContext.Params.DC==23&&spawnedContext.Params.Metamagic==(Metamagic.Extend|Metamagic.Maximize)&&spawnedDuration==TimeSpan.FromSeconds(156),"Move retains original CL/DC/metamagic; Extend not doubled");
   var ctx=Context(u,area,13);using(ctx.GetDataScope(u)){run.UnitEnter.Run();run.Round.Run();run.UnitEnter.Run();}Check(hits==(success?0:1)&&saves==(i==3&&!success?2:1),"One attempt perround even successful save; entry/round/multiple targets cannot duplicate");Check(applies==(!success&&(i==1||i==3&&!fort)?1:0),"Greater burn/stun conditional branch");Check(!ready.IsCasterRestrictionPassed(u),"After attack move blocked");using(moveContext.GetDataScope(u))place.Run();Check(spawns==1,"Direct restricted move cannot bypass cooldown");
   now=105.99;Check(!ready.IsCasterRestrictionPassed(u),"Before fullround cannot attack/move");now=106;Check(ready.IsCasterRestrictionPassed(u),"Exactly6sec refreshed");using(moveContext.GetDataScope(u))place.Run();Check(spawns==2&&spawnedDuration==TimeSpan.FromSeconds(150),"Move retains remaining duration; no refresh");
   using(ctx.GetDataScope(u))run.Round.Run();Check(hits==(success?0:2)&&saves==(i==3&&!success?3:2),"Next round damage; thunder Fort once only");Check(applies==(!success?(i==1?2:i==3&&!fort?1:0):0),"Burn repeated; onecasting thunder limited");
   var rt=((List<EntityFactComponent>)Get(host,"Components")).Single();var data=rt.GetData<PsychicOrbState>();var restored=Newtonsoft.Json.JsonConvert.DeserializeObject<PsychicOrbState>(Newtonsoft.Json.JsonConvert.SerializeObject(data));Check(restored.NextAttackAt==TimeSpan.FromSeconds(112)&&restored.ThunderUsed==(i==3&&!success),"Serialized percast cooldown and firstthunder survive data roundtrip");
   Set(host,"<IsTurnedOn>k__BackingField",false);Check(!ready.IsCasterRestrictionPassed(u),"Inactive parent immediately blocks move");int previous=saves;using(ctx.GetDataScope(u))run.UnitEnter.Run();Check(saves==previous,"Inactive parent cannot deal damage");Clear();Check(!ready.IsCasterRestrictionPassed(u),"Removed parent no move");
  }
  Clear();now=200;var a=Fact(Asset<BlueprintBuff>(c,"FlamingSphereController"),Context(u,Asset<BlueprintAbility>(c,"FlamingSphere")));var b=Fact(Asset<BlueprintBuff>(c,"AggressiveThundercloudController"),Context(u,Asset<BlueprintAbility>(c,"AggressiveThundercloud")));a.CallComponents<PsychicOrbController>(x=>x.SpendAttack());Check(!Asset<BlueprintAbility>(c,"FlamingSphereMove").GetComponent<PsychicOrbReady>().IsCasterRestrictionPassed(u)&&Asset<BlueprintAbility>(c,"AggressiveThundercloudMove").GetComponent<PsychicOrbReady>().IsCasterRestrictionPassed(u),"Different orb types maintain independent cooldown");Clear();
 }
 static void Cleanup(BlueprintsCache c,UnitEntityData u){
  var old=Kingmaker.Game.Instance.State;var state=Empty<PersistentState>();Set(Kingmaker.Game.Instance,"State",state);var other=new Psychic070Tests.Caster(c).Unit;Clear();var host=Fact(Asset<BlueprintBuff>(c,"FlamingSphereController"),Context(u,Asset<BlueprintAbility>(c,"FlamingSphere")));
  var poolType=AccessTools.Field(typeof(PersistentState),"AreaEffects").FieldType;var pool=FormatterServices.GetUninitializedObject(poolType);var list=new List<AreaEffectEntityData>();Set(pool,"m_Entities",list);Set(state,"AreaEffects",pool);
  foreach(var caster in new[]{u,other})foreach(var token in new[]{"FlamingSphere","AggressiveThundercloud"}){var area=Empty<AreaEffectEntityData>();Set(area,"m_IsInGame",true);Set(area,"m_Context",Context(caster,Asset<BlueprintAbilityAreaEffect>(c,token+"Area")));Set(area,"m_Blueprint",Asset<BlueprintAbilityAreaEffect>(c,token+"Area"));Set(area,"<UniqueId>k__BackingField",caster.UniqueId+token);list.Add(area);}
  try{ended.Clear();var rt=((List<EntityFactComponent>)Get(host,"Components")).Single();using(rt.RequestEventContext())AccessTools.Method(typeof(PsychicOrbController),"OnDeactivate").Invoke(host.Blueprint.GetComponent<PsychicOrbController>(),null);Check(ended.Count==1&&ended[0]==list[0],"Parent expiry ends only samecaster sametype area; othercaster and othertype remain");}finally{Set(Kingmaker.Game.Instance,"State",old);Clear();}
 }
 static void Unique(BlueprintsCache c,UnitEntityData u,Harmony h){
  h.Patch(AccessTools.PropertyGetter(typeof(EntityService),"Instance"),prefix:new HarmonyMethod(typeof(PsychicOrbTests),nameof(Service)));
  h.Patch(typeof(EntityService).GetMethods().Single(x=>x.Name=="GetEntity"&&!x.IsGenericMethod&&x.GetParameters().Length==1),prefix:new HarmonyMethod(typeof(PsychicOrbTests),nameof(Resolve)));
  var part=new UnitPartUniqueAreaEffects();var cloud=new UnitPartUniqueAreaEffects.AreaEffectListEntry {Feature=Asset<BlueprintBuff>(c,"AggressiveThundercloudController"),AreaId="cloud"};
  foreach(var name in new[]{"old","new","cloud"}){var e=Empty<AreaEffectEntityData>();Set(e,"<UniqueId>k__BackingField",name);Set(e,"m_Blueprint",Asset<BlueprintAbilityAreaEffect>(c,(name=="cloud"?"AggressiveThundercloud":"FlamingSphere")+"Area"));entities[name]=e;}
  var feature=Asset<BlueprintBuff>(c,"FlamingSphereController");part.Areas.Add(cloud);part.Areas.Add(new UnitPartUniqueAreaEffects.AreaEffectListEntry {Feature=feature,AreaId="old"});part.Areas.Add(new UnitPartUniqueAreaEffects.AreaEffectListEntry {Feature=feature,AreaId="missing"});
  ended.Clear();part.NewAreaEffect(entities["new"],feature);Check(ended.SequenceEqual(new[]{entities["old"]})&&part.Areas.Count==2&&part.Areas.Any(x=>x.AreaId=="new")&&part.Areas.Contains(cloud),"Native unique replacement ends old samekind, keeps cloud and purges stale entry");
  ended.Clear();part.RemoveAreaEffect(entities["new"],feature);Check(ended.SequenceEqual(new[]{entities["new"]})&&part.Areas.Count==1&&part.Areas.Contains(cloud),"Native unique removal scopes matching feature");
  entities.Clear();
 }
 static bool Service(ref EntityService __result){__result=Empty<EntityService>();return false;}
 static bool Resolve(string __0,ref EntityDataBase __result){entities.TryGetValue(__0,out var found);__result=found;return false;}
 static bool Time(ref TimeSpan __result){__result=TimeSpan.FromSeconds(now);return false;}
 static bool Spawn(ContextActionSpawnAreaEffect __instance){spawns++;spawnedContext=(MechanicsContext)AccessTools.Property(typeof(ContextAction),"Context").GetValue(__instance,null);spawnedDuration=__instance.DurationValue.Calculate(spawnedContext).Seconds;return false;}
 static bool Damage(){hits++;return false;}
 static bool Apply(ContextActionApplyBuff __instance){applies++;if(!__instance.Permanent){var ctx=(MechanicsContext)AccessTools.Property(typeof(ContextAction),"Context").GetValue(__instance,null);Check(__instance.DurationValue.Calculate(ctx).Seconds==TimeSpan.FromSeconds(6),"Stun duration1round");}return false;}
 static bool Passed(RuleSavingThrow __instance,ref bool __result){__result=__instance.Type==SavingThrowType.Fortitude?fortPass:pass;return false;}
 static bool Save(RuleSavingThrow rule,ref RuleSavingThrow __result){saves++;Check(rule.DifficultyClassBase==23,"Save original spell DC");__result=rule;return false;}
 static bool End(AreaEffectEntityData __instance){ended.Add(__instance);return false;}
}
