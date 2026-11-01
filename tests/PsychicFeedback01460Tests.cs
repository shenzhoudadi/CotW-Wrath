using System;using System.Linq;using HarmonyLib;using Newtonsoft.Json.Linq;
using Kingmaker;using Kingmaker.PubSubSystem;using Kingmaker.EntitySystem.Stats;using Kingmaker.RuleSystem.Rules;using Kingmaker.RuleSystem.Rules.Damage;using Kingmaker.RuleSystem.Rules.Abilities;using Kingmaker.UnitLogic.FactLogic;using Kingmaker.UnitLogic.Abilities.Components;using Kingmaker.UnitLogic.Abilities.Components.Base;using Kingmaker.UnitLogic.Buffs;using Kingmaker.UnitLogic.Buffs.Blueprints;using Kingmaker.UnitLogic.Mechanics;using Kingmaker.UnitLogic.Parts;using Kingmaker.UI.UnitSettings;
using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.EntitySystem.Entities;using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.Abilities.Blueprints;using CallOfTheWild.Wrath.Prototype;
internal static partial class MagusBatch01454Tests{
 static void Feedback060(BlueprintsCache parent,Harmony h){
  string which=Environment.GetEnvironmentVariable("COTW_01454_CASE");if(which=="diagnose060"){Diagnose060(parent,h);return;}
  if(which=="localization060")LocalizationInputs060(h);
  var c=PsychicCache057(parent);WarmMercies058(c);Register(c,"MutationMind01457");Register(c,"PsychicMarauder01457");Register(c,"PsychicClassCompletion01458");
  foreach(var t in Main.GetTypes().Where(t=>t.Name.EndsWith("01460Patch")&&t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(t).Patch();
  if(which=="aura-control060")AuraControl060(c,h);else if(which=="wound-major060")WoundMajor060(c,h);else if(which=="localization060")Localization060(c,h);else if(which=="psychedelia060"||which=="synaptic060"||which=="surge060"){EffectInputs060(h);if(which=="psychedelia060")Psychedelia060(c,h);else if(which=="synaptic060")Synaptic060(c,h);else Surge060(c,h);}else throw new InvalidOperationException("060 case not yet implemented: "+which);
  PsychicExpansionTests.Inventory["psychicFeedback01460Contracts"]=new JObject{["passed"]=true,["case"]=which,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false};
 }
 static void WoundMajor060(BlueprintsCache c,Harmony h){
  var cls=N<BlueprintCharacterClass>(c,Psychic058);var marker=A<BlueprintFeature>(c,"MajorAmplificationsFeature01460");var pool=A<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");
  Check(marker.ComponentsArray.Length==0&&marker.IsClassFeature,"060 Major is an empty class eligibility marker without a selected amplification or bonus");
  Check(cls.Progression.LevelEntries.Single(e=>e.Level==11).Features.Count(f=>f==marker)==1,"060 real class progression exposes exactly one Major marker at11");
  foreach(int level in new[]{1,10,11,13,20}){
   var u=EquipmentUnit(c,level,Psychic058);Check(u.HasFact(marker)==(level>=11),"060 actual native class-level grant gives marker only at11+: "+level);
   foreach(string token in new[]{"SynapticShockFeature01458","SpaceRendingSpellFeature01458","DualAmplificationFeature01458","MimicMetamagic1Feature01458"}){
    var feat=A<BlueprintFeature>(c,token);Check(feat.GetComponents<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeature>().Count(p=>p.Feature==marker)==1&&!feat.GetComponents<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel>().Any(p=>p.Level==11),"060 Major prerequisites reference the marker and remove the bare level11 check: "+token);
    Check(Pick058(u,pool,feat)==(level>=11),"060 native CanSelect Major eligibility at "+level+" / "+token);
   }
   if(level==13){var casting=u.Facts.List.Single(f=>f.Blueprint.AssetGuid==BlueprintGuid.Parse("4896622b699957208e128c3a5361609c"));var runtime=casting.Components.Single(r=>r.SourceBlueprintComponent is Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel);var component=(Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel)runtime.SourceBlueprintComponent;
    foreach(var fact in u.Facts.List.Where(f=>f.Blueprint==marker).ToArray())u.Facts.Remove(fact);Check(!Pick058(u,pool,A<BlueprintFeature>(c,"SynapticShockFeature01458")),"060 level13 alone does not satisfy the marker prerequisite");
    using(runtime.RequestEventContext())AccessTools.Method(component.GetType(),"OnPostLoad").Invoke(component,null);Check(u.HasFact(marker),"060 actual native class-level OnPostLoad restores a missing marker for an older character");using(runtime.RequestEventContext())AccessTools.Method(component.GetType(),"OnPostLoad").Invoke(component,null);Check(u.Facts.List.Count(f=>f.Blueprint==marker)==1,"060 repeated native marker PostLoad does not duplicate the feature");
    foreach(var fact in u.Facts.List.Where(f=>f.Blueprint==marker).ToArray())u.Facts.Remove(fact);Set(runtime,"m_Data",new Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevelData());runtime.PostLoad();Check(u.HasFact(marker),"060 actual native runtime PostLoad restores the entirely new component data missing from059 characters");
   }
   if(level==10){Grant(u,marker);Check(Pick058(u,pool,A<BlueprintFeature>(c,"SynapticShockFeature01458")),"060 actual feature possession is the prerequisite rather than a hidden level surrogate");}
  }
  var owner=EquipmentUnit(c,13,Psychic058);Grant(owner,c,"AgonizingWoundFeature01458");var menu=A<BlueprintAbility>(c,"AgonizingWoundMenu01460");var target=A<BlueprintAbility>(c,"AgonizingWoundTargetAbility01458");var resource=A<BlueprintAbilityResource>(c,"AgonizingWoundResource01458");
  bool wound061=Main.GetType("CallOfTheWild.Wrath.Prototype.PsychicWoundMenu01461")!=null;var variants=menu.GetComponent<AbilityVariants>().Variants.ToArray();Check(variants.Length==(wound061?1:5)&&variants.Count(a=>a==target)==1&&variants.All(a=>a.Parent==menu),"versioned wound menu retains native Target parent;061 removes four obsolete one-shot shortcuts");
  foreach(string token in new[]{"AgonizingWoundTargetAbility01458","AgonizingWoundMenu01460"}){
   var slot=new MechanicActionBarSlotAbility{Ability=new AbilityData(A<BlueprintAbility>(c,token),owner.Descriptor)};int before=owner.Resources.GetResourceAmount(resource);
   Check(slot.GetResource()==before,"060 actual native actionbar slot displays wound resource for "+token);owner.Resources.Spend(resource,1);Check(slot.GetResource()==before-1,"060 actual native actionbar counter follows spent wound use for "+token);
  }
  int remaining=owner.Resources.GetResourceAmount(resource);if(wound061){foreach(int i in Enumerable.Range(0,4))ToggleOn(Toggle(owner,c,"AgonizingWound"+i+"01458Toggle"));}else foreach(var a in variants.Where(a=>a!=target)){Super058(owner,a);foreach(var t in owner.ActivatableAbilities.Enumerable.Where(t=>t.IsOn&&!t.IsStarted).ToArray())t.TryStart();}
  Check(owner.Resources.GetResourceAmount(resource)==remaining,"060 native menu condition selection spends no wound uses");
  Check(owner.ActivatableAbilities.Enumerable.Count(a=>a.IsOn&&a.Blueprint.Buff?.GetComponent<PsychicDamageLink01458>()?.Kind==PsychicDamageKind01458.Agonizing)==1,"060 native shortcuts preserve exactly one existing wound mode");
  var source=owner.Facts.List.Single(f=>f.Blueprint.AssetGuid==Id("AgonizingWoundFeature01458"));var grant=source.Components.Single(r=>r.SourceBlueprintComponent is AddFacts);var parent=owner.Facts.List.Single(f=>f.Blueprint==menu);owner.Facts.Remove(parent);grant.GetData<AddFactsData>().AppliedFacts.RemoveAll(f=>f.Blueprint==menu);grant.ApplyPostLoadFixes();
  Check(owner.Abilities.Enumerable.Count(a=>a.Blueprint==menu)==1&&owner.Resources.GetResourceAmount(resource)==remaining,"060 actual native AddFacts old-state repair restores the new wound parent without payment");grant.ApplyPostLoadFixes();Check(owner.Abilities.Enumerable.Count(a=>a.Blueprint==menu)==1,"060 repeated native wound-menu repair keeps exactly one parent");
  var transitioning=EquipmentUnit(c,10,Psychic058);var castFact=transitioning.Facts.List.Single(f=>f.Blueprint.AssetGuid==BlueprintGuid.Parse("4896622b699957208e128c3a5361609c"));var levelGrant=castFact.Components.Single(r=>r.SourceBlueprintComponent is Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel);transitioning.Progression.Classes[0].Level=11;using(levelGrant.RequestEventContext())((Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel)levelGrant.SourceBlueprintComponent).HandleUnitGainLevel();
  Check(transitioning.HasFact(marker)&&Pick058(transitioning,pool,A<BlueprintFeature>(c,"SynapticShockFeature01458")),"060 actual native class-level gain event enables Major choices at the11th-level transition");
  Check(!pool.AllFeatures.Contains(marker)&&cls.Progression.LevelEntries.Single(e=>e.Level==11).Features.Count(f=>f==pool)==1,"060 empty marker grants no extra amplification choice and never enters its own option list");
  NewTypeRoundtrip060(c);
 }
 static void Diagnose060(BlueprintsCache parent,Harmony h){
  var c=PsychicCache057(parent);WarmMercies058(c);Register(c,"MutationMind01457");Register(c,"PsychicMarauder01457");Register(c,"PsychicClassCompletion01458");
  foreach(var t in Main.GetTypes().Where(t=>t.Name.EndsWith("01460Patch")&&t.GetCustomAttributes(typeof(HarmonyPatch),false).Any()))h.CreateClassProcessor(t).Patch();
  h.CreateClassProcessor(T("PsychicUndercasting").GetNestedTypes(System.Reflection.BindingFlags.NonPublic).Single(t=>t.Name=="LearnedPatch")).Patch();
  var owner=EquipmentUnit(c,20,Psychic058);var target=EquipmentUnit(c,20);var pool=Pool058(c,owner);var book=Book058(c,owner);Known058(c,book,6,"MindThrust6");Grant(owner,c,"UndercastSurgeFeature01458");
  var low=book.GetKnownSpells(1).First(a=>a.Blueprint==A<BlueprintAbility>(c,"MindThrust1"));
  for(int i=1;i<=5;i++){
   var toggle=Toggle(owner,c,"UndercastSurge"+i+"01458Toggle");ToggleOn(toggle);
   var active=owner.Buffs.Enumerable.Where(b=>b.Blueprint.GetComponent<PsychicAmplificationComponent>()?.Kind==PsychicAmplificationKind.UndercastSurge).ToArray();Console.WriteLine("060 SURGE mode="+i+" active="+string.Join(",",active.Select(b=>b.Blueprint.name+":"+b.Blueprint.GetComponent<PsychicAmplificationComponent>().Strength)));
   Console.WriteLine("060 SURGE conversions="+string.Join(",",low.GetConversions().Select(a=>a.Blueprint.name+"/slot="+book.GetSpellLevel(a)+"/effect="+a.SpellLevel)));
   owner.Resources.Restore(pool,99);int before=owner.Resources.GetResourceAmount(pool);var cast=Linked058(low,target);NativeCast058(cast,h);Console.WriteLine("060 SURGE direct="+cast.Spell.Blueprint.name+" points="+(before-owner.Resources.GetResourceAmount(pool))+" params="+cast.Context.Params.SpellLevel+" slot="+book.GetSpellLevel(cast.Spell));
   ToggleOff(toggle);
  }
  var aura=A<BlueprintFeature>(c,"HallucinogenicAuraFeature01458");Console.WriteLine("060 AURA components="+string.Join(",",aura.ComponentsArray.Select(a=>a.GetType().FullName+" base="+a.GetType().BaseType)));
  PsychicExpansionTests.Inventory["psychicDiagnosis01460"]=new JObject{["completed"]=true,["gameLaunchTested"]=false,["observationsOnly"]=true};
 }
}
