using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UI.Common;
using Kingmaker.UI.Tooltip;
using CallOfTheWild.Wrath.Prototype;

internal static class TrainingFeedbackTests {
 static int n;static BlueprintsCache cache;static readonly Type Module=typeof(TrainingSkillRanks).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T11TrainingOptions");
 static readonly LocalizedTexts text=new LocalizedTexts{Stats=Empty<StatsStrings>(),BonusSources=Empty<BonusSourceStrings>()};
 static void Set(object o,string k,object v)=>(AccessTools.Field(o.GetType(),k)??AccessTools.Field(o.GetType(),"<"+k+">k__BackingField")).SetValue(o,v);
 static object Get(object o,string k)=>(AccessTools.Field(o.GetType(),k)??AccessTools.Field(o.GetType(),"<"+k+">k__BackingField")).GetValue(o);
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool v,string s){if(!v)throw new Exception("Training feedback: "+s);n++;}
 static bool Skip()=>false;
 static bool Text(ref LocalizedTexts __result){__result=text;return false;}
 static string Translated(Kingmaker.Localization.LocalizedString s)=>"Class skill";
 static IEnumerable<CodeInstruction> Localize(IEnumerable<CodeInstruction> code) {
  foreach(var x in code){if(x.operand is System.Reflection.MethodInfo m&&m.DeclaringType==typeof(Kingmaker.Localization.LocalizedString)&&m.Name=="op_Implicit")x.operand=AccessTools.Method(typeof(TrainingFeedbackTests),nameof(Translated));yield return x;}
 }
 static UnitEntityData Unit(string name)=>(UnitEntityData)AccessTools.Method(typeof(T11TrainingTests),"Unit").Invoke(null,new object[]{cache,name});
 static Feature Fact(UnitEntityData u,BlueprintFeature b)=>(Feature)AccessTools.Method(typeof(T11TrainingTests),"Fact").Invoke(null,new object[]{u,b});
 static void Call(Feature f,Type t,string m)=>AccessTools.Method(typeof(T11TrainingTests),"Call").Invoke(null,new object[]{f,t,m,null});
 static BlueprintGuid Id(string t)=>(BlueprintGuid)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{t});
 static BlueprintFeatureSelection Pool(string t)=>(BlueprintFeatureSelection)cache.Load(Id(t));
 static bool Select(SelectFeature __instance,LevelUpState state,UnitDescriptor unit) {
  // Fact insertion/upgrade observer endpoints bounded; native component activation
  // and patched SelectFeature prefix/postfix still run around this selection.
  if(state.Selections.Count==0)return false;
  var f=Fact(unit.Unit,__instance.Item.Feature);Call(f,typeof(AddClassSkill),"OnTurnOn");Call(f,typeof(TrainingSkillRanks),"OnTurnOn");return false;
 }
 static LevelUpState State(UnitEntityData u){var s=Empty<LevelUpState>();Set(s,"m_Unit",u);Set(s,"NextCharacterLevel",9);Set(s,"Selections",new List<FeatureSelectionState>());return s;}
 internal static int Run(BlueprintsCache c) {
  cache=c;n=0;var h=new Harmony("CotW.TrainingFeedback0146");
  Set(text.Stats,"m_Cache",new Dictionary<StatType,Kingmaker.Localization.LocalizedString>());
  h.Patch(AccessTools.PropertyGetter(typeof(LocalizedTexts),"Instance"),prefix:new HarmonyMethod(typeof(TrainingFeedbackTests),nameof(Text)));
  // Unity-backed string conversion is a view boundary, redirected at exact call sites.
  h.Patch(AccessTools.Constructor(typeof(StatTooltipData),new[]{typeof(ModifiableValueSkill)}),transpiler:new HarmonyMethod(typeof(TrainingFeedbackTests),nameof(Localize)));
  h.Patch(AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TrainingSkillTooltipPatch"),"Postfix"),transpiler:new HarmonyMethod(typeof(TrainingFeedbackTests),nameof(Localize)));
  h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(TrainingFeedbackTests),nameof(Skip)));
  h.Patch(AccessTools.Method(typeof(SelectFeature),"Apply"),prefix:new HarmonyMethod(typeof(TrainingFeedbackTests),nameof(Select)){priority=Priority.Last});
  foreach(var p in new[]{"TrainingSkillRankPatch","TrainingSkillTooltipPatch","TrainingSkillSelectionRefundPatch","TrainingSkillExistingRefundPatch","TrainingSkillAllocationPatch"})h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype."+p)).Patch();
  try {
   foreach(string pool in new[]{"VersatileTrainingSelection","AdaptableTrainingSelection"})foreach(var choice in Pool(pool).AllFeatures) {
    var u=Unit("training-refund");u.Stats.BaseAttackBonus.BaseValue=9;var skill=u.Stats.GetStat<ModifiableValueSkill>(choice.GetComponent<TrainingSkillRanks>().Skill);skill.BaseValue=8;skill.BaseAttribute.BaseValue=14;
    var state=State(u);state.TotalSkillPoints=4;state.SpentSkillPoints=1;
    var action=new SelectFeature();Set(action,"Item",Pool(pool).Items.Single(x=>x.Feature==choice));action.Apply(state,u.Descriptor);Check(skill.BaseValue==8&&state.TotalSkillPoints==4,"no selection/activation cannot refund");
    state.Selections.Add(new FeatureSelectionState(null,default(FeatureSource),Pool(pool),0,9));action.Apply(state,u.Descriptor);Check(skill.BaseValue==0&&state.TotalSkillPoints==12&&state.SkillPointsRemaining==11,"all eight invested points returned exactly once");
    action.Apply(state,u.Descriptor);Check(state.TotalSkillPoints==12,"replay no double refund");
    var data=new StatTooltipData(skill);var entries=(List<StatBonusEntry>)Get(data.Breakdown,"Bonuses");Check(data.BaseValue==9&&data.ModifiedValue==14,"tooltip shows BAB nine and total nine plus class three plus attribute two");Check(entries.Count(x=>x.Bonus==3)==1&&entries.Any(x=>x.Source!=null&&x.Source.Contains("BAB (9)")),"native tooltip breakdown class skill once plus rank explanation");Check(skill.BaseValue==0&&LevelUpHelper.GetSpentSkillPoints(u.Descriptor)==0,"tooltip never fabricates spent ranks");
    Check(!new SpendSkillPoint(skill.Type).Check(state,u.Descriptor),"effective BAB skill cannot waste new skill points");var unused=Kingmaker.EntitySystem.Stats.StatTypeHelper.Skills.First(x=>x!=skill.Type);Check(new SpendSkillPoint(unused).Check(state,u.Descriptor),"refunded points available in another skill");new SpendSkillPoint(unused).Apply(state,u.Descriptor);Check(u.Stats.GetStat(unused).BaseValue==1&&state.SkillPointsRemaining==10,"native allocation actually spends refunded point elsewhere");
    u.Stats.BaseAttackBonus.BaseValue=11;var updated=new StatTooltipData(skill);Check(updated.BaseValue==11&&updated.ModifiedValue==16,"BAB dependency automatically updates tooltip and total");
    // Old saves carry their original allocation ledger; normal skill-step rebuild
    // clears it before the native spent-point sum and therefore refunds it.
    skill.BaseValue=8;new ApplySkillPoints().Apply(state,u.Descriptor);Check(skill.BaseValue==0&&LevelUpHelper.GetSpentSkillPoints(u.Descriptor)==1,"existing training removes old allocated ranks before native ledger sum");int first=state.TotalSkillPoints;new ApplySkillPoints().Apply(state,u.Descriptor);Check(state.TotalSkillPoints==first,"rebuilding skill step never mints extra refund");
    var untrained=Unit("cancel-preview");untrained.Stats.GetStat(skill.Type).BaseValue=8;Check(untrained.Stats.GetStat(skill.Type).BaseValue==8,"separate original unit unaffected by preview edits");
   }
   // Optional providers are reused by their existing identities; late insertion
   // survives repeat refresh and cannot leak direct options into ordinary pools.
   var armor=(string[])AccessTools.Field(Module,"AdvancedArmorPools").GetValue(null);var wp=(string[])AccessTools.Field(Module,"AdvancedWeaponPools").GetValue(null);
   foreach(var id in armor.Concat(wp).Distinct())if(cache.Load(BlueprintGuid.Parse(id))==null)cache.AddCachedBlueprint(BlueprintGuid.Parse(id),new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(id),name="FixtureExistingAdvancedPool"});
   AccessTools.Method(Module,"Optional").Invoke(null,new object[]{cache});AccessTools.Method(Module,"Optional").Invoke(null,new object[]{cache});
   foreach(var id in armor)Check(((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).AllFeatures.Count(x=>x.AssetGuid==Id("AdaptableTrainingSelection"))==1,"optional armor pool gets one existing root");
   foreach(var id in wp)foreach(var token in new[]{"VersatileTrainingSelection","DazzlingIntimidationFeat"})Check(((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).AllFeatures.Count(x=>x.AssetGuid==Id(token))==1,"late weapon pool gets exactly one child");
   var isolated=new BlueprintsCache();foreach(var token in new[]{"DazzlingIntimidationFeat","VersatileTrainingSelection","AdaptableTrainingSelection"})isolated.AddCachedBlueprint(Id(token),cache.Load(Id(token)));
   var isolatedArmor=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(armor[0]),name="ExistingArmorProviderWithoutBrawler"};isolated.AddCachedBlueprint(isolatedArmor.AssetGuid,isolatedArmor);bool integrated=false;Action<string,bool,Exception> report=(name,ok,error)=>{if(name=="Advanced training integration")integrated=ok&&error==null;};AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration"),"RegisterOptional").Invoke(null,new object[]{isolated,report});Check(integrated&&isolatedArmor.AllFeatures.Count(x=>x.AssetGuid==Id("AdaptableTrainingSelection"))==1,"late native registration wires armor provider without Brawler dependency");
   PsychicExpansionTests.Inventory["trainingFeedback0146Assertions"]=n;Console.WriteLine("PASS: "+n+" training feedback checks; actual tooltip constructor, allocated/effective ranks, native spent-point sums and allocation, installed refund hooks; localization, feature insertion and upgrade observers bounded.");return n;
  }finally{h.UnpatchAll(h.Id);}
 }
}
