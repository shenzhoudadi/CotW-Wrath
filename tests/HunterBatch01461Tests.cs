using System;using System.Linq;using HarmonyLib;using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.UnitLogic.Abilities.Blueprints;using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Blueprints.Classes.Prerequisites;using Kingmaker.Blueprints.Classes.Selection;using Kingmaker.UnitLogic;using Kingmaker.UnitLogic.Abilities;using Kingmaker.UnitLogic.ActivatableAbilities;using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Entities;
internal static partial class MagusBatch01454Tests{
 static void HunterRegistration061(BlueprintsCache c,Harmony h){
  var cls=N<BlueprintCharacterClass>(c,"34ecd1b5e1b90b9498795791b0855239");var previous=cls.Archetypes.ToArray();var entries=cls.Progression.LevelEntries;var groups=cls.Progression.UIGroups;
  var protectedAssets=new[]{"443365823b7d6d14b8d12f4e7bce1077","0c4afc62e8c5c3845872a0354755267d","470fb1a22e7eb5849999f1101eacc5dc","8abf1c437ebee8048a4a3335efc27eb3","d5640d96888dd20489922045fde35059","49a77c5c5266c42429f7afbb038ada60","ae6af4d58b70a754d868324d1a05eda4"}.Select(id=>N<BlueprintScriptableObject>(c,id)).ToArray();var before=protectedAssets.Select(bp=>(string)Call(T("MagusBatch01454"),"Definition",bp)).ToArray();
  Register(c,"FeralHunter01461");var root=A<BlueprintArchetype>(c,"FeralHunterArchetype01461");
  Check(root!=null&&root.GetParentClass()==cls&&previous.All(a=>cls.Archetypes.Contains(a))&&cls.Archetypes.Count(a=>a==root)==1,"061 real native Feral publication preserves every existing Hunter archetype and adds its own once");
  Check(!string.IsNullOrEmpty(root.LocalizedName.Key)&&!string.IsNullOrEmpty(root.LocalizedDescription.Key),"061 archetype uses real localized class selection labels");
  Check(ReferenceEquals(entries,cls.Progression.LevelEntries)&&groups.All(g=>cls.Progression.UIGroups.Contains(g)),"061 native Hunter progression rows and existing UI groups remain intact");
  Check(before.SequenceEqual(protectedAssets.Select(bp=>(string)Call(T("MagusBatch01454"),"Definition",bp))),"061 Feral never mutates original native focus, polymorph buffs or shared wildshape resource definition");
  Check(root.RemoveFeatures.Where(e=>e.Level==1).SelectMany(e=>e.Features).Any(f=>f.AssetGuid==BlueprintGuid.Parse("715ac15eb8bd5e342bc8a0a3c9e3e38f"))&&!root.RemoveFeatures.SelectMany(e=>e.Features).Any(f=>f.AssetGuid==BlueprintGuid.Parse("873f1f12bb43cda40bcc7d2878d16bf1")),"061 Feral replaces actual companion selection and preserves actual Hunter proficiencies");
  Check(root.AddFeatures.Count(e=>e.Features.Any(f=>f.AssetGuid==BlueprintGuid.Parse("f78260b9a089ccc44b55f0fed08b1752")))==8,"061 native wildshape use increments cover6 through20 exactly eight times");
  foreach(string name in new[]{"Wolf","Leopard","Bear","DireWolf","Smilodon","Mastodon"})Check(A<BlueprintAbility>(c,"FeralHunterWildShape"+name+"Ability01461")!=null&&A<BlueprintBuff>(c,"FeralHunterWildShape"+name+"Buff01461")!=null,"061 complete six-form private native animal catalogue: "+name);
  Register(c,"FeralHunter01461");Check(ReferenceEquals(root,A<BlueprintArchetype>(c,"FeralHunterArchetype01461"))&&cls.Archetypes.Count(a=>a==root)==1,"061 strict repeat registration retains one exact Feral instance");
  Register(c,"TotemBonded01461");var totem=A<BlueprintArchetype>(c,"TotemBondedArchetype01461");Check(totem!=null&&totem.GetParentClass()==cls&&cls.Archetypes.Contains(root)&&previous.All(a=>cls.Archetypes.Contains(a)),"061 actual Totem publication preserves Feral and every native archetype");
  var companion=A<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,"TotemPrimevalCompanion01461");Check(companion.AllFeatures.Count()==6&&companion.AllFeatures.Any(f=>f.AssetGuid==BlueprintGuid.Parse("472091361cf118049a2b4339c4ea836a")),"061 private primeval selector includes five exact allowed native animals and native empty companion");
  Check(totem.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,8,20})&&totem.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,8,20}),"061 Totem replacements match actual Hunter levels1,8,20");
  Register(c,"TotemBonded01461");Check(ReferenceEquals(totem,A<BlueprintArchetype>(c,"TotemBondedArchetype01461")),"061 strict Totem repeat registration preserves exact identity after native row reads");
  Check(before.SequenceEqual(protectedAssets.Select(bp=>(string)Call(T("MagusBatch01454"),"Definition",bp))),"061 both Hunter variants preserve all protected native focus/polymorph/resource definitions");
  HunterWeaponIcon061(c);FeralQualification061(c);TotemResource061(c);HunterEffects061(c,h);
  PsychicExpansionTests.Inventory["hunterBatch01461Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["gameLaunchTested"]=false};
 }
 static void HunterWeaponIcon061(BlueprintsCache c){
  var donor=N<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(c,"daf4ab765feba8548b244e174e7af5be");var kind=donor.Type;var itemField=AccessTools.Field(donor.GetType(),"m_Icon");var typeField=AccessTools.Field(kind.GetType(),"m_Icon");var itemBefore=itemField.GetValue(donor);var typeBefore=typeField.GetValue(kind);
  var typeSprite=(UnityEngine.Sprite)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));var itemSprite=(UnityEngine.Sprite)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));var target=new BlueprintBuff();
  var copy=AccessTools.Method(T("TotemBonded01461"),"Icon",new[]{typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact),typeof(Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon)});
  try{itemField.SetValue(donor,null);typeField.SetValue(kind,typeSprite);copy.Invoke(null,new object[]{target,donor});Check(ReferenceEquals(AccessTools.Field(target.GetType(),"m_Icon").GetValue(target),typeSprite),"061 actual native weapon type supplies missing Gore/Slam item icon without a generic default");itemField.SetValue(donor,itemSprite);copy.Invoke(null,new object[]{target,donor});Check(ReferenceEquals(AccessTools.Field(target.GetType(),"m_Icon").GetValue(target),itemSprite),"061 native item-specific natural attack icon retains priority over its type fallback");}finally{itemField.SetValue(donor,itemBefore);typeField.SetValue(kind,typeBefore);}
 }
 static bool NativePrerequisite061(Prerequisite prerequisite,UnitEntityData owner)=>(bool)AccessTools.Method(prerequisite.GetType(),"CheckInternal").Invoke(prerequisite,new object[]{null,owner.Descriptor,null});
 static BlueprintFeatureReference HunterRef061(BlueprintFeature feature)=>BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(feature);
 static BlueprintFeatureSelection NativeFeatPool061(BlueprintsCache c){
  // PsychicCache057 intentionally carries the old, selective Psychic fixture's
  // feat links. This qualification scenario needs the entire authored native
  // ordinary selection, not an invented selection containing just our target.
  // Deserialize a detached, unedited archive record; don't replace or alter the
  // cache's integrated selection or its registration receipts.
  var id=BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45");JObject data;
  using(var reader=new System.IO.StreamReader(archive.GetEntry(paths[id.ToString()]).Open()))data=(JObject)JObject.Parse(reader.ReadToEnd())["Data"];
  var pool=(BlueprintFeatureSelection)PsychicExpansionTests.Read(data,typeof(BlueprintScriptableObject));pool.AssetGuid=id;pool.name="BasicFeatSelection";
  pool.ComponentsArray=((JArray)data["Components"]).Select(v=>(BlueprintComponent)PsychicExpansionTests.Read(v,typeof(BlueprintComponent))).ToArray();foreach(var component in pool.ComponentsArray)component.OwnerBlueprint=pool;pool.OnEnable();
  var authored=((JArray)data["m_AllFeatures"]).Select(v=>v.ToString().Replace("!bp_","")).ToArray();
  Check(pool.AllFeatures.Select(v=>v.AssetGuid.ToString()).SequenceEqual(authored)&&authored.Length>200,"061 native qualification input retains the entire unedited authored ordinary feat pool and its ordering");
  return pool;
 }
 static void FeralQualification061(BlueprintsCache c){
  var owner=EquipmentUnit(c,4,"34ecd1b5e1b90b9498795791b0855239");owner.Stats.Wisdom.BaseValue=14;var pool=NativeFeatPool061(c);var natural=N<BlueprintFeature>(c,"c806103e27cce6f429e5bf47067966cf");var wolf=N<BlueprintFeature>(c,"19bb148cb92db224abb431642d10efeb");
  Check(!Pick058(owner,pool,natural),"061 actual native ordinary feat pool rejects Natural Spell before Wild Shape");
  Grant(owner,c,"FeralHunterWildShapeWolfFeature01461");Check(!owner.HasFact(wolf)&&Pick058(owner,pool,natural),"061 actual native CanSelect accepts Feral animal Wild Shape without granting original Druid wolf effects");
  owner.Stats.Wisdom.BaseValue=12;Check(!Pick058(owner,pool,natural),"061 Feral qualification preserves Natural Spell Wisdom13 requirement");owner.Stats.Wisdom.BaseValue=14;owner.Stats.Intelligence.BaseValue=2;Check(!Pick058(owner,pool,natural),"061 Feral qualification preserves Natural Spell Intelligence3 requirement");owner.Stats.Intelligence.BaseValue=20;
  var unrelated=A<BlueprintFeature>(c,"MajorAmplificationsFeature01460");var single=new PrerequisiteFeature();Set(single,"m_Feature",HunterRef061(unrelated));Check(!NativePrerequisite061(single,owner),"061 Feral never qualifies an unrelated feature prerequisite");
  var list=new PrerequisiteFeaturesFromList{Amount=2};Set(list,"m_Features",new[]{HunterRef061(wolf),HunterRef061(unrelated)});Check(!NativePrerequisite061(list,owner),"061 native two-feature list counts Feral alias once and still requires the unrelated feature");Grant(owner,unrelated);Check(NativePrerequisite061(list,owner),"061 native prerequisite list accepts the complete real two-feature requirement");Grant(owner,wolf);list.Amount=3;Check(!NativePrerequisite061(list,owner),"061 owning both native wolf and Feral never double counts one list entry");
  var no=new PrerequisiteNoFeature();Set(no,"m_Feature",HunterRef061(wolf));Check(!NativePrerequisite061(no,owner),"061 native inverted Wild Shape prerequisite remains inverted");
  var plain=EquipmentUnit(c,4,"34ecd1b5e1b90b9498795791b0855239");plain.Stats.Wisdom.BaseValue=14;Check(!Pick058(plain,pool,natural)&&NativePrerequisite061(no,plain),"061 unrelated Hunter stays without Wild Shape qualification");Grant(plain,wolf);Check(Pick058(plain,pool,natural),"061 original native Druid feature still qualifies without any Feral feature");
 }
 static void TotemResource061(BlueprintsCache c){
  var resource=A<BlueprintAbilityResource>(c,"TotemSharedStrengthResource01461");var property=A<Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty>(c,"TotemCompanionRankProperty01461");
  foreach(int level in new[]{1,7,8,14,15,20}){
   var owner=EquipmentUnit(c,level,"34ecd1b5e1b90b9498795791b0855239");Check(resource.GetMaxAmount(owner.Descriptor)==level,"061 actual native Shared Strength resource equals Hunter level: "+level);
   Check(property.GetInt(owner)==(level>=15?3:level>=8?2:1),"061 actual native private master-rank property has source class thresholds: "+level);
   Grant(owner,c,"TotemSharedStrengthFeature01461");if(level>=8)Grant(owner,c,"TotemSharedStrength8Feature01461");if(level>=20)Grant(owner,c,"TotemSharedStrength20Feature01461");
   int cap=owner.Ensure<Kingmaker.UnitLogic.Parts.UnitPartActivatableAbility>().GetGroupSize(ActivatableAbilityGroup.HunterAnimalFocus);Check(cap==(level>=20?3:level>=8?2:1),"061 native selected-aspect capacity is one/two/three: "+level);
   var ability=owner.Abilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("TotemSharedStrengthAbility01461")).Data;Check(ability.ActionType==Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift&&new Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility{Ability=ability,Unit=owner}.GetResource()==level,"061 actual native Shared Strength action and daily counter: "+level);
  }
 }
}
