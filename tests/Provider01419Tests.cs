using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class Provider01419Tests {
 static readonly Type module=typeof(CrueltyFocusDC).Assembly.GetType("CallOfTheWild.Wrath.Prototype.ProviderResourceFeats");
 static readonly Type assets=module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets");
 static int n,deliveries,paramsCalls;static UnitEntityData deliveredTarget;static MechanicsContext deliveredContext;static Buff applied;static BlueprintAbility lastParamsSource;
 static Action replay;
 static void Set(object o,string f,object v)=>(AccessTools.Field(o.GetType(),f)??AccessTools.Field(o.GetType(),"<"+f+">k__BackingField")).SetValue(o,v);
 static T Ref<T>(SimpleBlueprint b)where T:BlueprintReferenceBase,new(){var r=BlueprintReferenceBase.CreateTyped<T>(b);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,b,null);return r;}
 static BlueprintGuid Id(string token)=>(BlueprintGuid)AccessTools.Method(assets,"Id").Invoke(null,new object[]{token});
 static string Constant(string name)=>(string)AccessTools.Field(module,name).GetRawConstantValue();
 static void Check(bool b,string msg){if(!b)throw new Exception("Provider01419: "+msg);n++;}
 static int BonusDC(RuleCalculateAbilityParams r)=>(AccessTools.Field(typeof(RuleCalculateAbilityParams),"m_BonusDC").GetValue(r)as Kingmaker.RuleSystem.Rules.ModifiableBonus)?.TotalValue??0;
 static T Add<T>(BlueprintsCache c,T b,string id)where T:BlueprintScriptableObject{b.AssetGuid=BlueprintGuid.Parse(id);b.OnEnable();c.AddCachedBlueprint(b.AssetGuid,b);return b;}
 static BlueprintFeature Feature(BlueprintsCache c,string id,string name)=>Add(c,new BlueprintFeature{name=name,Ranks=1,ComponentsArray=Array.Empty<BlueprintComponent>()},id);
 static void Register(BlueprintsCache c)=>AccessTools.Method(module,"Register").Invoke(null,new object[]{c});
 static bool Ready(BlueprintsCache c)=>(bool)AccessTools.Method(module,"Available").Invoke(null,new object[]{c});
 static T Asset<T>(BlueprintsCache c,string token)where T:SimpleBlueprint=>(T)c.Load(Id(token));
 static object Helper(string name,params object[] args)=>AccessTools.Method(typeof(SpellAndSword01417Tests),name).Invoke(null,args);
 static EntityFactComponent Runtime(EntityFact f,Type t)=>f.Components.Single(c=>c.SourceBlueprintComponent.GetType()==t);
 static Feature Fact(UnitEntityData u,BlueprintFeature f)=>(Feature)Helper("Fact",u,f);
 static void Focus(Feature fact,RuleCalculateAbilityParams rule){var r=Runtime(fact,typeof(CrueltyFocusDC));using(r.RequestEventContext())((CrueltyFocusDC)r.SourceBlueprintComponent).OnEventAboutToTrigger(rule);}
 static void Hit(Buff b,RuleAttackWithWeapon rule){var r=Runtime(b,typeof(ChannelCorruptionHit));using(r.RequestEventContext())((ChannelCorruptionHit)r.SourceBlueprintComponent).OnEventDidTrigger(rule);}
 static bool Skip()=>false;
 static bool HitResult(RuleAttackRoll __instance,ref bool __result){__result=!__instance.AutoMiss;return false;}
 static string Label(Kingmaker.Localization.LocalizedString value)=>value?.Key??"Touch of Corruption";
 static string Name(Kingmaker.Blueprints.Facts.BlueprintUnitFact b)=>b.name;
 static IEnumerable<CodeInstruction> Labels(IEnumerable<CodeInstruction> code){foreach(var i in code){if(i.operand is System.Reflection.MethodInfo m){if(m.DeclaringType==typeof(Kingmaker.Localization.LocalizedString)&&m.Name=="op_Implicit")i.operand=AccessTools.Method(typeof(Provider01419Tests),nameof(Label));if(m.Name=="get_Name"&&typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact).IsAssignableFrom(m.DeclaringType)){i.opcode=System.Reflection.Emit.OpCodes.Call;i.operand=AccessTools.Method(typeof(Provider01419Tests),nameof(Name));}}yield return i;}}
 internal static void PatchLabels(Harmony h)=>h.Patch(AccessTools.Method(module,"Register"),transpiler:new HarmonyMethod(typeof(Provider01419Tests),nameof(Labels)));
 static bool Deactivate(EntityFact __instance){if(!(__instance is Buff))return true;Set(__instance,"IsActive",false);return false;}
 static bool AddBuff(BuffCollection __instance,BlueprintBuff __0,MechanicsContext __1,TimeSpan? __2,ref Buff __result){var owner=__instance.Owner.Unit;applied=Psychic034Tests.Fact(owner,__0,owner,12);Set(applied,"m_Context",__1.CloneFor(__0,owner.Descriptor,owner.Descriptor));__result=applied;return false;}
 // Provider parameter/scene endpoints are controlled; native AbilityData,
 // execution context, buff action scope and resource rank calculation run.
 static bool Params(AbilityData __instance,ref AbilityParams __result){if(__instance.Blueprint.GetComponent<ChannelCorruptionArm>()!=null)return true;paramsCalls++;lastParamsSource=__instance.Blueprint;__result=new AbilityParams{CasterLevel=12,DC=21};return false;}
 public sealed class Delivery:ContextAction {
  public override string GetCaption()=>"Offline provider action boundary";
  public override void RunAction(){deliveries++;deliveredTarget=Target.Unit;deliveredContext=Context;replay?.Invoke();}
 }
 internal static int Run(BlueprintsCache c) {
  n=0;var h=new Harmony("CotW01419.ProviderContracts");h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(Provider01419Tests),nameof(Skip)));
  PatchLabels(h);
  try {
   Check(!Ready(c),"absent providers inactive");Register(c);foreach(string t in (string[])AccessTools.Field(module,"Tokens").GetValue(null))Check(c.Load(Id(t))==null,"no feat without provider");
   var lesser=Feature(c,"7dc2d7e4d12940c191531b273c062c60","LesserSpellSynthesis");var lesserResource=Add(c,new BlueprintAbilityResource{name="LesserSpellSynthesisRes",ComponentsArray=Array.Empty<BlueprintComponent>()},"a0a77ed0e29b4bfbb8d9b78cc85bde6e");Register(c);Check(Asset<BlueprintFeature>(c,"ExtraSpellSynthesisFeat")==null,"PrestigePlus Lesser alone does not register formal synthesis feat");
   var synthesis=Feature(c,Constant("Synthesis"),"SpellSynthesis");Check(!Ready(c),"partial synthesis waits");
   var sr=Add(c,new BlueprintAbilityResource{name="SpellSynthesisRes",ComponentsArray=Array.Empty<BlueprintComponent>()},Constant("SynthesisResource"));Register(c);Check(Ready(c),"late resource detected");
   var extra=Asset<BlueprintFeature>(c,"ExtraSpellSynthesisFeat");Check(extra.Ranks==10&&extra.ReapplyOnLevelUp,"repeatable resource feat");
   var u=(UnitEntityData)Helper("Unit",c,"01419provider");Set(u,"Parts",new EntityPartsManager(u));var ef=Fact(u,extra);var runtime=Runtime(ef,typeof(IncreaseResourceAmount));var increase=(IncreaseResourceAmount)runtime.SourceBlueprintComponent;
   foreach(int rank in new[]{1,2,5,10}){Set(ef,"Rank",rank);int bonus=0;using(runtime.RequestEventContext())increase.CalculateMaxResourceAmount(sr,ref bonus);Check(bonus==rank,"native rank bonus "+rank);bonus=4;using(runtime.RequestEventContext())increase.CalculateMaxResourceAmount(new BlueprintAbilityResource(),ref bonus);Check(bonus==4,"other resource untouched");}
   Set(ef,"IsActive",false);int inactive=0;using(runtime.RequestEventContext())increase.CalculateMaxResourceAmount(sr,ref inactive);Check(inactive==0,"inactive feat no bonus");Set(ef,"IsActive",true);
   var touch=Feature(c,Constant("TouchFeature"),"AntipaladinTouchOfCorruptionFeature");var tr=Add(c,new BlueprintAbilityResource{name="AntipaladinTouchOfCorruptionResource",ComponentsArray=Array.Empty<BlueprintComponent>()},Constant("TouchResource"));
   var cruelty=Feature(c,Constant("CrueltySelection"),"AntipaladinCrueltySelection");var ids=(string[])AccessTools.Field(module,"TouchSources").GetValue(null);var sources=new List<BlueprintAbility>();var variants=new AbilityVariants();
   var parent=Add(c,new BlueprintAbility{name="AntipaladinTouchOfCorruptionBase",ComponentsArray=new BlueprintComponent[]{variants}},Constant("TouchParent"));
   foreach(var id in ids){var cs=new List<BlueprintComponent>();if(id!=ids.Last()){var learned=Feature(c,Id("01419ProviderCruelty"+id).ToString(),"Cruelty"+id);var show=new AbilityShowIfCasterHasFact();Set(show,"m_UnitFact",Ref<BlueprintUnitFactReference>(learned));cs.Add(show);}cs.Add(new AbilityEffectRunAction{Actions=new ActionList{Actions=new GameAction[]{new Delivery()}}});cs.Add(new ContextRankConfig());sources.Add(Add(c,new BlueprintAbility{name="AntipaladinTouchOfCorruption"+id,Type=AbilityType.Supernatural,ComponentsArray=cs.ToArray()},id));}
   Set(variants,"m_Variants",sources.Take(1).Select(Ref<BlueprintAbilityReference>).ToArray());Register(c);Check(Asset<BlueprintFeature>(c,"ChannelCorruptionFeat")==null,"partial catalogue waits");Set(variants,"m_Variants",sources.Select(Ref<BlueprintAbilityReference>).ToArray());
   var arrays=sources.ToDictionary(a=>a,a=>a.ComponentsArray);Register(c);var focus=Asset<BlueprintFeature>(c,"AbilityFocusCrueltyFeat");var channel=Asset<BlueprintFeature>(c,"ChannelCorruptionFeat");Check(focus!=null&&channel!=null,"late complete provider creates roots");
   var roots=new[]{extra,focus,channel};var basic=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));var before=basic.AllFeatures.Select(f=>f.AssetGuid).ToArray();Register(c);Check(basic.AllFeatures.Select(f=>f.AssetGuid).SequenceEqual(before),"repeat preserves order");foreach(var root in roots){Check(ReferenceEquals(c.Load(root.AssetGuid),root),"persistent identity");Check(basic.AllFeatures.Count(f=>f==root)==1,"ordinary entry unique");Check(root.GetComponents<PrerequisiteFeature>().All(p=>p.Feature!=null),"prerequisite references resolve");}
   var ff=Fact(u,focus);foreach(var source in sources){var r=new RuleCalculateAbilityParams(u,source,null);Focus(ff,r);Check(BonusDC(r)==(source==sources.Last()?0:2),"DC scope "+source.AssetGuid);Check(ReferenceEquals(source.ComponentsArray,arrays[source]),"provider unchanged");}
   var unrelated=new RuleCalculateAbilityParams(u,new BlueprintAbility(),null);Focus(ff,unrelated);Check(BonusDC(unrelated)==0,"unrelated ability excluded");
   foreach(var root in new[]{extra,channel}){var p=root.GetComponent<PrerequisiteFeature>();Check(!p.CheckRaw(null,u.Descriptor,null),"missing prerequisite");Fact(u,p.Feature);Check(p.CheckRaw(null,u.Descriptor,null),"existing prerequisite");}
   var focusPrereq=focus.GetComponent<PrerequisiteFeaturesFromList>();Check(!focusPrereq.CheckRaw(null,u.Descriptor,null),"no learned cruelty rejects focus");Fact(u,(BlueprintFeature)sources[0].GetComponent<AbilityShowIfCasterHasFact>().UnitFact);Check(focusPrereq.CheckRaw(null,u.Descriptor,null),"one learned cruelty qualifies without selection fact");
   var menu=Asset<BlueprintAbility>(c,"ChannelCorruptionAbility");var arms=menu.GetComponent<AbilityVariants>().Variants.ToArray();Check(arms.Length==sources.Count,"15 variants, one root");foreach(var a in arms){var arm=a.GetComponent<ChannelCorruptionArm>();Check(arm.Source.Get()!=null&&arm.Charge.Get()!=null,"charge refs resolve");Check(arm.AllCharges.Length==arms.Length,"replacement clears all variants");Check(a.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift&&a.Range==AbilityRange.Personal,"swift self arm");Check(a.GetComponent<AbilityResourceLogic>().CalculateCost(new AbilityData(a,u.Descriptor))==2,"two uses upfront");Check(a.GetComponent<AbilityResourceLogic>().RequiredResource==tr,"reuse resource");Check(!ReferenceEquals(arm.Charge.Get().GetComponent<ChannelCorruptionHit>().Action,arm.Source.Get().GetComponent<AbilityEffectRunAction>().Actions),"private action graph");}
   h.Patch(AccessTools.Method(typeof(AbilityData),"CalculateParams"),prefix:new HarmonyMethod(typeof(Provider01419Tests),nameof(Params)));
   h.CreateClassProcessor(module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.ChannelCorruptionPreview")).Patch();
   h.Patch(AccessTools.Method(typeof(BuffCollection),"AddBuff",new[]{typeof(BlueprintBuff),typeof(MechanicsContext),typeof(TimeSpan?)}),prefix:new HarmonyMethod(typeof(Provider01419Tests),nameof(AddBuff)));
   h.Patch(AccessTools.Method(typeof(EntityFact),"Deactivate"),prefix:new HarmonyMethod(typeof(Provider01419Tests),nameof(Deactivate)));
   h.Patch(AccessTools.PropertyGetter(typeof(RuleAttackRoll),"IsHit"),prefix:new HarmonyMethod(typeof(Provider01419Tests),nameof(HitResult)));
   var target=(UnitEntityData)Helper("Unit",c,"01419victim");var sword=(Kingmaker.Items.ItemEntityWeapon)Helper("Weapon",u,WeaponCategory.Longsword,"01419sword");var bow=(Kingmaker.Items.ItemEntityWeapon)Helper("Weapon",u,WeaponCategory.Longbow,"01419bow");
   var arm0=arms[0].GetComponent<ChannelCorruptionArm>();var context=new AbilityExecutionContext(new AbilityData(arms[0],u.Descriptor),new AbilityParams{CasterLevel=1,DC=10},u);
   paramsCalls=deliveries=0;arm0.Apply(context,u);Check(paramsCalls==1&&lastParamsSource==arm0.Source.Get(),"source parameters calculated once");Check(applied.Context.Params.DC==21&&applied.Context.Params.CasterLevel==12&&applied.Context.SourceAbility==arm0.Source.Get(),"source context retained");
   foreach(var a in arms)Check(new AbilityData(a,u.Descriptor).CalculateParams().DC==21,"native option parameter path uses actual source endpoint including focus");
   var hit=(RuleAttackWithWeapon)Helper("Attack",u,target,sword,false);var miss=(RuleAttackWithWeapon)Helper("Attack",u,target,sword,true);var ranged=(RuleAttackWithWeapon)Helper("Attack",u,target,bow,false);
   Hit(applied,ranged);Check(deliveries==0&&applied.Active,"ranged does not consume");Set(hit.AttackRoll,"IsFake",true);Hit(applied,hit);Check(deliveries==0&&applied.Active,"preview does not consume");Set(hit.AttackRoll,"IsFake",false);
   var guard=Runtime(applied,typeof(ChannelCorruptionHit)).GetData<ChannelCorruptionState>();replay=()=>Hit(applied,hit);Hit(applied,hit);replay=null;Check(deliveries==1&&deliveredTarget==target&&deliveredContext.SourceAbility==arm0.Source.Get(),"source action native buff scope");Check(guard.Used,"consumed before delivery");Check(deliveries==1&&!applied.Active,"no nested replay and buff removed");arm0.Apply(context,u);Hit(applied,miss);Check(deliveries==1&&!applied.Active,"miss consumes with no effect");
   Check(JsonConvert.DeserializeObject<ChannelCorruptionState>(JsonConvert.SerializeObject(new ChannelCorruptionState{Used=true})).Used,"guard JSON roundtrip");using(var icon=module.Assembly.GetManifestResourceStream("CotW.ChannelCorruption.png"))Check(icon!=null,"original icon embedded");
   var newTokens=new[]{"ExtraSpellSynthesisFeat","AbilityFocusCrueltyFeat","ChannelCorruptionFeat","ChannelCorruptionAbility"}.Concat(ids.SelectMany(id=>new[]{"ChannelCorruption"+id+"Buff","ChannelCorruption"+id+"Ability"})).ToArray();
   foreach(var token in newTokens)Check(c.Load(Id(token))!=null,"actual registered identity "+token);
   PsychicExpansionTests.Inventory["provider01419NewBlueprints"]=newTokens.Length;
  }finally{h.UnpatchAll(h.Id);}
  PsychicExpansionTests.Inventory["provider01419Assertions"]=n;PsychicExpansionTests.Inventory["provider01419FeatRoots"]=3;Console.WriteLine("PASS: "+n+" provider feat contracts: absent/late/partial, rank/DC/cost/charge/preview/miss/identity.");return n;
 }
}
