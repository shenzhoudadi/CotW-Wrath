using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
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
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Commands.Base;
using CallOfTheWild.Wrath.Prototype;

// Native stat calculation, prerequisites, action getters, training rank and CMB
// calculations execute. Equipment view/upgrade observers and scene UI are bounds.
internal static class T11TrainingTests {
 static int n;static readonly Type Module=typeof(TrainingSkillRanks).Assembly.GetType("CallOfTheWild.Wrath.Prototype.T11TrainingOptions");
 static object Get(object o,string field)=>(AccessTools.Field(o.GetType(),field)??AccessTools.Field(o.GetType(),"<"+field+">k__BackingField")).GetValue(o);
 static void Set(object o,string field,object value)=>(AccessTools.Field(o.GetType(),field)??AccessTools.Field(o.GetType(),"<"+field+">k__BackingField")??throw new Exception("T11 field: "+field)).SetValue(o,value);
 static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool ok,string message){if(!ok)throw new Exception("T11: "+message);n++;}
 static string Const(string key)=>(string)AccessTools.Field(Module,key).GetValue(null);
 static BlueprintGuid Id(string token)=>(BlueprintGuid)AccessTools.Method(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"),"Id").Invoke(null,new object[]{token});
 static T Asset<T>(BlueprintsCache cache,string token) where T:SimpleBlueprint=>(T)cache.Load(Id(token));
 static bool Skip()=>false;
 static bool Difficulty(int originalDC,ref int dcAdjustment,ref int __result){dcAdjustment=0;__result=originalDC;return false;}
 static bool SizeBoundary(ref Size __result){__result=Size.Medium;return false;}
 static void Refresh(UnitEntityData unit){var c=Get(unit.Facts,"m_Cache");AccessTools.Method(c.GetType(),"Clear").Invoke(c,null);}
 static UnitEntityData Unit(BlueprintsCache cache,string name){var u=Psychic034Tests.Unit(cache,name).Unit;u.Progression.Classes.Clear();var stats=new CharacterStats(u.Descriptor);Set(u.Descriptor,"Stats",stats);foreach(var stat in stats.AllStats.OfType<ModifiableValueAttributeStat>())stat.BaseValue=10;var features=new FeatureCollection();features.Initialize(u.Facts);Set(u.Progression,"Features",features);Set(u.Progression,"m_CharacterLevel",(int?)10);var bp=Psychic034Tests.Asset<BlueprintUnit>(cache,"SpiritualWeaponUnit");Set(u.Descriptor,"Blueprint",bp);Set(u.Descriptor,"OriginalBlueprint",bp);Set(u.Descriptor,"Proficiencies",new UnitProficiency());Set(u.Descriptor,"Body",new UnitBody(u.Descriptor));return u;}
 static Feature Fact(UnitEntityData unit,BlueprintFeature bp){var f=Empty<Feature>();Set(f,"Components",new List<EntityFactComponent>());f.RestoreOwnerLink(unit.Facts);f.Param=new FeatureParam();Set(f,"Rank",1);AccessTools.Method(typeof(EntityFact),"Setup").Invoke(f,new object[]{bp});Set(f,"IsActive",true);Set(f,"IsTurnedOn",true);unit.Facts.List.Add(f);Refresh(unit);return f;}
 static void Call(Feature fact,Type type,string method,object evt=null){foreach(var runtime in fact.Components.Where(c=>c.SourceBlueprintComponent.GetType()==type))using(runtime.RequestEventContext())AccessTools.Method(type,method).Invoke(runtime.SourceBlueprintComponent,evt==null?null:new[]{evt});}
 static bool Need(Prerequisite p,UnitEntityData unit)=>(bool)AccessTools.Method(p.GetType(),"CheckInternal").Invoke(p,new object[]{null,unit.Descriptor,null});
 static BlueprintReferenceBase Ref(SimpleBlueprint bp,Type type){var r=(BlueprintReferenceBase)Activator.CreateInstance(type);Set(r,"deserializedGuid",bp.AssetGuid);AccessTools.Property(typeof(BlueprintReferenceBase),"Cached").SetValue(r,bp,null);return r;}
 static int Total(RulebookEvent rule)=>(int)AccessTools.Property(typeof(RulebookEvent),"TotalBonusValue").GetValue(rule,null);
 static void Native<T>(BlueprintsCache cache,string guid) where T:BlueprintScriptableObject,new() {if(cache.Load(BlueprintGuid.Parse(guid))==null){var bp=PsychicExpansionTests.NativeCase<T>(guid);cache.AddCachedBlueprint(bp.AssetGuid,bp);}}
 internal static int Run(BlueprintsCache cache) {
  n=0;var h=new Harmony("CotW.T11.NativeRegression");
  h.Patch(AccessTools.Method(typeof(FeatureCollection),"CheckStatPrerequisitesAndUpdate"),prefix:new HarmonyMethod(typeof(T11TrainingTests),nameof(Skip)));
  h.Patch(AccessTools.Method(typeof(RuleSkillCheck),"AdjustDCByDifficulty"),prefix:new HarmonyMethod(typeof(T11TrainingTests),nameof(Difficulty)));
  h.Patch(AccessTools.PropertyGetter(typeof(UnitState),"Size"),prefix:new HarmonyMethod(typeof(T11TrainingTests),nameof(SizeBoundary)));
  foreach(string patch in new[]{"TrainingSkillRankPatch","DazzlingTrainingActionPatch","DazzlingTrainingFullRoundPatch"})h.CreateClassProcessor(Module.Assembly.GetType("CallOfTheWild.Wrath.Prototype."+patch)).Patch();
  try {
   foreach(var row in PsychicExpansionTests.NativeRows.Where(row=>new[]{Const("Fighter"),Const("Magus"),Const("Armor"),Const("TrainingPool"),Const("MagusPool"),Const("ScionPool"),Const("Familiar"),Const("Demoralize"),Const("Display"),"4c936de4249b61e419a3fb775b9f2581","3aa4cbdd4af5ba54888b0dc7f07f80c4","70a139f0a4c6c534eaa34feea0d08622","ee9ab0117ca06b84f9c66469f4428c61","0b55d725ded1ae549bb858fba1d84114"}.Contains((string)row["blueprint"]["AssetId"]) || ((string)row["name"]).StartsWith("WeaponTraining") || ((string)row["name"]).EndsWith("FamiliarBondFeature"))) {
    string id=(string)row["blueprint"]["AssetId"],kind=(string)row["blueprint"]["Data"]["$type"];
    if(kind.EndsWith(", BlueprintCharacterClass"))Native<BlueprintCharacterClass>(cache,id);
    else if(kind.EndsWith(", BlueprintFeatureSelection"))Native<BlueprintFeatureSelection>(cache,id);
    else if(kind.EndsWith(", BlueprintAbility"))Native<BlueprintAbility>(cache,id);
    else if(kind.EndsWith(", BlueprintFeature"))Native<BlueprintFeature>(cache,id);
    else if(kind.EndsWith(", BlueprintStatProgression"))Native<BlueprintStatProgression>(cache,id);
    var blueprint=cache.Load(BlueprintGuid.Parse(id));
    foreach(var property in ((JObject)row["blueprint"]["Data"]).Properties().Where(x=>new[]{"m_AllFeatures","m_Features","m_BaseAttackBonus","BaseAttackBonus","Bonuses","m_IsFullRoundAction","ActionType"}.Contains(x.Name))) {
     var field=AccessTools.Field(blueprint.GetType(),property.Name);if(field!=null)field.SetValue(blueprint,PsychicExpansionTests.Read(property.Value,field.FieldType));
    }
   }
   var training=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(Const("TrainingPool")));
   var magus=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(Const("Magus")));
   var fighter=(BlueprintCharacterClass)cache.Load(BlueprintGuid.Parse(Const("Fighter")));
   var nativeFamiliar=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(Const("Familiar")));
   var familiarIds=nativeFamiliar.AllFeatures.Select(x=>x.AssetGuid).ToArray();
   var groups=training.AllFeatures.Where(f=>f.GetComponent<WeaponGroupAttackBonus>()!=null).ToArray();Check(groups.Length==13,"actual thirteen native weapon groups, not handmade prerequisite inputs");
   var register=AccessTools.Method(Module,"Register");register.Invoke(null,new object[]{cache});
   var first=Asset<BlueprintFeature>(cache,"DazzlingIntimidationFeat");register.Invoke(null,new object[]{cache});Check(ReferenceEquals(first,Asset<BlueprintFeature>(cache,"DazzlingIntimidationFeat")),"repeat module registration preserves identity");
   var empty=new BlueprintsCache();bool rejected=false;try{register.Invoke(null,new object[]{empty});}catch{rejected=true;}Check(rejected&&empty.Load(Id("DazzlingIntimidationFeat"))==null,"missing native dependencies leave no partial module");
   foreach(string id in new[]{"3aa4cbdd4af5ba54888b0dc7f07f80c4","70a139f0a4c6c534eaa34feea0d08622","ee9ab0117ca06b84f9c66469f4428c61","0b55d725ded1ae549bb858fba1d84114"})foreach(string token in new[]{"DazzlingIntimidationFeat","VersatileTrainingSelection"})Check(((BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id))).AllFeatures.Count(x=>x.AssetGuid==Id(token))==1,"native advanced subpool has exactly one "+token);
   Check(!training.AllFeatures.Any(x=>new[]{Id("DazzlingIntimidationFeat"),Id("VersatileTrainingSelection"),Id("AdaptableTrainingSelection")}.Contains(x.AssetGuid)),"ordinary weapon group selection has no direct training roots");
   var roots=new[]{"DazzlingIntimidationFeat","VersatileTrainingSelection","AdaptableTrainingSelection","MagusFamiliarSelection","MagusManeuverMasterySelection"};
   foreach(var token in roots){var bp=Asset<BlueprintFeature>(cache,token);Check(bp.ComponentsArray.All(c=>c.OwnerBlueprint==bp)&&bp.ComponentsArray.Select(c=>c.name).Distinct().Count()==bp.ComponentsArray.Length,"stable owned component names: "+token);}
   foreach(string id in new[]{"247a4068296e8be42890143f451b4b45","66befe7b24c42dd458952e3c47c93563","41c8486641f7d6d4283ca9dae4147a9f","da03141df23f3fe45b0c7c323a8e5a0e","79c6421dbdb028c4fa0c31b8eea95f16","c5158a6622d0b694a99efb1d0025d2c1"})foreach(string token in roots.Take(3)){var pool=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id));Check(pool.AllFeatures.Count(x=>x!=null&&x.AssetGuid==Id(token))==0,"training roots excluded from ordinary feat pools "+token);}
   foreach(var id in new[]{Const("MagusPool"),Const("ScionPool")}){var pool=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(id));foreach(var token in roots.Skip(3))Check(pool.AllFeatures.Count(x=>x!=null&&x.AssetGuid==Id(token))==1,"both native arcana pools get one "+token);}
   var familiar=Asset<BlueprintFeatureSelection>(cache,"MagusFamiliarSelection");Check(familiar.AllFeatures.Select(x=>x.AssetGuid).SequenceEqual(familiarIds)&&nativeFamiliar.AllFeatures.Select(x=>x.AssetGuid).SequenceEqual(familiarIds),"all existing familiar choices reused; wizard source untouched");
   var owner=Unit(cache,"0143-training");owner.Progression.Classes.Add(new ClassData(fighter){Level=5});owner.Stats.BaseAttackBonus.BaseValue=5;
   var axe=groups.Single(x=>x.GetComponent<WeaponGroupAttackBonus>().WeaponGroup.ToString()=="Axes");var axeFact=Fact(owner,axe);
   Check(first.GetComponents<Prerequisite>().All(p=>Need(p,owner)),"fighter five with actual training qualifies for Dazzling");owner.Progression.Classes[0].Level=4;Check(!first.GetComponents<Prerequisite>().All(p=>Need(p,owner)),"fighter four denied");owner.Progression.Classes[0].Level=5;
   var armor=(BlueprintFeature)cache.Load(BlueprintGuid.Parse(Const("Armor")));var adaptable=Asset<BlueprintFeatureSelection>(cache,"AdaptableTrainingSelection");Check(!adaptable.GetComponents<Prerequisite>().All(p=>Need(p,owner)),"armor training genuinely required");var armorFact=Fact(owner,armor);Check(adaptable.GetComponents<Prerequisite>().All(p=>Need(p,owner)),"actual armor training qualifies");
   var skillChoices=Asset<BlueprintFeatureSelection>(cache,"VersatileTrainingSelection").AllFeatures;
   Check(skillChoices.Length==6&&adaptable.AllFeatures.Length==3,"six weapon-linked and three armor choices count as two options");
   var selected=skillChoices.Single(x=>x.GetComponent<TrainingSkillRanks>().Skill==StatType.SkillAthletics);
   var unsupported=skillChoices.Single(x=>x.GetComponent<TrainingSkillRanks>().Skill==StatType.SkillStealth);
   Check(Need(selected.GetComponent<PrerequisiteFeaturesFromList>(),owner)&&!Need(unsupported.GetComponent<PrerequisiteFeaturesFromList>(),owner),"Axes skill eligibility uses real group facts");
   var trained=Fact(owner,selected);var skill=owner.Stats.GetStat<ModifiableValueSkill>(StatType.SkillAthletics);skill.BaseValue=0;owner.Stats.Strength.BaseValue=14;
   skill.BaseValue=8;Call(trained,typeof(AddClassSkill),"OnTurnOn");Call(trained,typeof(TrainingSkillRanks),"OnTurnOn");Check(skill.BaseValue==0,"training refunds previous eight allocated ranks");
   foreach(int ranks in new[]{0,1,3,8})foreach(int bab in new[]{3,5,9,15}) {
    skill.BaseValue=ranks;owner.Stats.BaseAttackBonus.BaseValue=bab;skill.UpdateValue();
    Check(skill.BaseValue==ranks,"calculation never fabricates allocated ranks");Check(skill.ModifiedValue==bab+3+2,"native class skill and ability modifier retain BAB rank substitution");
    var rule=new RuleSkillCheck(owner,StatType.SkillAthletics,10);Check(rule.TotalBonus==skill.ModifiedValue,"native skill check observes same displayed total");
   }
   var bonus=skill.AddModifier(2,trained,ModifierDescriptor.Competence);skill.UpdateValue();Check(skill.ModifiedValue==15+3+2+2,"other skill modifiers remain native");bonus.Remove();
   var conflict=adaptable.AllFeatures.Single(x=>x.GetComponent<TrainingSkillRanks>().Skill==StatType.SkillAthletics);Check(!conflict.GetComponents<PrerequisiteNoFeature>().All(p=>Need(p,owner)),"same skill cannot also take armor training");
   Set(trained,"IsTurnedOn",false);Call(trained,typeof(TrainingSkillRanks),"OnTurnOff");Call(trained,typeof(AddClassSkill),"OnTurnOff");skill.UpdateValue();Check(skill.BaseValue==8&&skill.ModifiedValue==8+2,"base ledger remains native after removal; only owned class skill flag drops");
   var rebuilt=Fact(owner,selected);Call(rebuilt,typeof(AddClassSkill),"OnTurnOn");Call(rebuilt,typeof(TrainingSkillRanks),"OnTurnOn");Check(skill.ModifiedValue==15+3+2,"recreated native feature recalculates without serialized overwritten ranks");
   owner.Stats.BaseAttackBonus.BaseValue=6;Check(skill.ModifiedValue==6+3+2,"native BAB dependency automatically updates selected skill");
   var d=Fact(owner,first);var item=Empty<ItemEntityWeapon>();var type=new BlueprintWeaponType();Set(type,"m_FighterGroupFlags",WeaponFighterGroupFlags.Axes);var itemBp=new BlueprintItemWeapon();Set(itemBp,"m_Type",Ref(type,typeof(BlueprintWeaponTypeReference)));Set(item,"m_Blueprint",itemBp);Set(item,"HoldingSlot",owner.Body.PrimaryHand);Set(owner.Body.PrimaryHand,"m_FakeItem",item);Set(owner.Body.PrimaryHand,"m_IsItemFake",true);
   var part=owner.Ensure<UnitPartWeaponTraining>();Get(part,"m_weaponTrainings");((List<EntityFact>)Get(part,"m_weaponTrainings")).Add(axeFact);Set(axeFact,"Rank",3);
   Check(part.GetMaxWeaponRank()==3,"actual equipped group uses native weapon training rank");
   var demoralize=(BlueprintAbility)cache.Load(BlueprintGuid.Parse(Const("Demoralize")));var display=(BlueprintAbility)cache.Load(BlueprintGuid.Parse(Const("Display")));
   var demoralizeData=new AbilityData(demoralize,owner.Descriptor);var displayData=new AbilityData(display,owner.Descriptor);
   Check(demoralizeData.ActionType==UnitCommand.CommandType.Move,"real Demoralize getter becomes move action");Check(displayData.ActionType==UnitCommand.CommandType.Standard&&!displayData.RequireFullRoundAction,"native Dazzling Display becomes standard, not full round");
   var intimidate=new RuleSkillCheck(owner,StatType.CheckIntimidate,10);Call(d,typeof(DazzlingTrainingBonus),"OnEventAboutToTrigger",intimidate);Check(intimidate.Bonus.ModifiedValue==3,"native Intimidate bonus receives real training rank");var ordinary=new RuleSkillCheck(owner,StatType.SkillPersuasion,10);Call(d,typeof(DazzlingTrainingBonus),"OnEventAboutToTrigger",ordinary);Check(ordinary.Bonus.ModifiedValue==0,"ordinary Persuasion checks not all boosted");
   Set(owner.Body.PrimaryHand,"m_FakeItem",null);Check(demoralizeData.ActionType==demoralize.ActionType&&displayData.RequireFullRoundAction,"unequipped weapon restores native costs");Set(owner.Body.PrimaryHand,"m_FakeItem",item);Set(d,"IsTurnedOn",false);Check(demoralizeData.ActionType==demoralize.ActionType&&displayData.RequireFullRoundAction,"inactive or retrained Dazzling restores native costs");Set(d,"IsTurnedOn",true);
   var master=Asset<BlueprintFeatureSelection>(cache,"MagusManeuverMasterySelection");Check(master.AllFeatures.Length==5,"five maneuver choices, Dirty Trick variants one choice");
   var magusOwner=Unit(cache,"0143-magus");var target=Unit(cache,"0143-maneuver-target");magusOwner.Progression.Classes.Add(new ClassData(magus){Level=8});magusOwner.Progression.Classes.Add(new ClassData(fighter){Level=2});magusOwner.Stats.BaseAttackBonus.BaseValue=8;
   var trip=master.AllFeatures.Single(x=>x.GetComponent<MagusManeuverTraining>().Maneuvers.Contains(CombatManeuver.Trip));var tripFact=Fact(magusOwner,trip);Check(magus.BaseAttackBonus.GetBonus(8)==6,"actual native Magus BAB progression");
   foreach(var chosen in master.AllFeatures){var fact=chosen==trip?tripFact:Fact(magusOwner,chosen);var c=chosen.GetComponent<MagusManeuverTraining>();foreach(CombatManeuver maneuver in Enum.GetValues(typeof(CombatManeuver))) {
    var rule=new RuleCalculateCMB(magusOwner,target,maneuver);Call(fact,typeof(MagusManeuverTraining),"OnEventAboutToTrigger",rule);Check(Total(rule)==(c.Maneuvers.Contains(maneuver)?2:0),"native chosen maneuver bonus "+maneuver);rule.Base.OnTrigger(null);Check(rule.Base.Result==8,"native mixed-class baseline preserved");
   }}
   magusOwner.Stats.BaseAttackBonus.BaseValue=10;var full=new RuleCalculateCMB(magusOwner,target,CombatManeuver.Trip);Call(tripFact,typeof(MagusManeuverTraining),"OnEventAboutToTrigger",full);Check(Total(full)==0,"full BAB transformation does not receive extra mastery");
   magusOwner.Stats.BaseAttackBonus.BaseValue=8;full=new RuleCalculateCMB(magusOwner,target,CombatManeuver.Trip){ReplaceBAB=12};Call(tripFact,typeof(MagusManeuverTraining),"OnEventAboutToTrigger",full);Check(Total(full)==0&&full.ReplaceBAB==12,"higher external BAB replacement preserved");
   var one=new RuleCalculateCMB(magusOwner,target,CombatManeuver.Trip);Call(tripFact,typeof(MagusManeuverTraining),"OnEventAboutToTrigger",one);Call(tripFact,typeof(MagusManeuverTraining),"OnEventAboutToTrigger",one);Check(Total(one)==2,"repeat event callback cannot double mastery");Set(tripFact,"IsTurnedOn",false);one=new RuleCalculateCMB(magusOwner,target,CombatManeuver.Trip);Call(tripFact,typeof(MagusManeuverTraining),"OnEventAboutToTrigger",one);Check(Total(one)==0,"retraining removes chosen maneuver bonus");
   var all=new JArray();foreach(var token in roots.Concat(skillChoices.Select(x=>x.GetComponent<TrainingSkillRanks>().Skill).Select(s=>"VersatileTraining"+s)).Concat(adaptable.AllFeatures.Select(x=>"AdaptableTraining"+x.GetComponent<TrainingSkillRanks>().Skill)).Concat(master.AllFeatures.Select(x=>"MagusManeuverMastery"+x.GetComponent<MagusManeuverTraining>().Maneuvers[0]))) {var bp=Asset<BlueprintFeature>(cache,token);all.Add(new JObject{["token"]=token,["guid"]=bp.AssetGuid.ToString(),["name"]=bp.name,["type"]=bp.GetType().Name});}
   PsychicExpansionTests.Inventory["t11Training0143Assets"]=all;PsychicExpansionTests.Inventory["t11Training0143Assertions"]=n;
   Console.WriteLine("PASS: "+n+" T11 native skill rank/class flag/dependency, training/action, familiar and mixed-class maneuver checks; scene UI, upgrade observers and full saves bounded.");return n;
  } finally {h.UnpatchAll(h.Id);}
 }
}
