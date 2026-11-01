using System;using System.Linq;using System.IO;using System.Collections.Generic;using HarmonyLib;using Kingmaker.Blueprints;using Kingmaker.Blueprints.JsonSystem;
internal static partial class MagusBatch01454Tests{
 static void NewTypeRoundtrip060(BlueprintsCache c){
  bool fresh061=Main.GetType("CallOfTheWild.Wrath.Prototype.PsychicWoundMenu01461")!=null;var names=fresh061?new[]{"SynapticDelay01460","MajorGrant01460"}:new[]{"SelectAgonizingMode01460","SynapticDelay01460","MajorGrant01460"};var expected=new HashSet<Type>(names.Select(T));var done=new HashSet<Type>();var binder=(GuidClassBinder)Json.Serializer.Binder;
  foreach(var type in expected)foreach(var id in type.GetCustomAttributes(typeof(TypeIdAttribute),false).Cast<TypeIdAttribute>())binder.AddToCache(type,id.GuidString);
  var snapshot=new List<BlueprintScriptableObject>();c.ForEachLoaded((id,bp)=>{if(bp is BlueprintScriptableObject obj)snapshot.Add(obj);});
  foreach(var owner in snapshot){var graph=(IEnumerable<object>)Call(T("PsychicRemainingDisciplines01458"),"Graph",owner.ComponentsArray,null);foreach(var original in graph.Where(o=>expected.Contains(o.GetType())&&!done.Contains(o.GetType())).ToArray()){
   var prior=Json.BlueprintBeingRead;var nodes=owner.ElementsArray.ToArray();try{
    Json.BlueprintBeingRead=new BlueprintJsonWrapper(owner);var declared=original is BlueprintComponent?typeof(BlueprintComponent):typeof(Kingmaker.ElementsSystem.GameAction);string text;
    using(var writer=new StringWriter())using(var output=new Newtonsoft.Json.JsonTextWriter(writer)){Json.Serializer.Serialize(output,original,declared);text=writer.ToString();}
    object restored;using(var input=new Newtonsoft.Json.JsonTextReader(new StringReader(text)))restored=Json.Serializer.Deserialize(input,declared);
    Check(restored!=null&&restored.GetType()==original.GetType(),"060 actual native polymorphic blueprint serializer restores exact new TypeId: "+original.GetType().Name);if(restored is BlueprintComponent component)Check(component.OwnerBlueprint==owner,"060 actual new component JSON restores its exact blueprint owner: "+component.GetType().Name);done.Add(original.GetType());
   }finally{Json.BlueprintBeingRead=prior;owner.ElementsArray.Clear();owner.ElementsArray.AddRange(nodes);Set(owner,"m_AllElements",owner.ElementsArray);}
  }}Check(done.SetEquals(expected),"all retained060 serialized identities complete native polymorphic roundtrip;061 retires only the obsolete one-shot wound action");
  string[] legacy={"MajorAmplificationsFeature01460","HallucinogenicAuraHost01460","AgonizingWoundMenu01460","AgonizingWoundMode0Ability01460","AgonizingWoundMode1Ability01460","AgonizingWoundMode2Ability01460","AgonizingWoundMode3Ability01460","SynapticShockPending01460"};var tokens=legacy.Where(t=>!fresh061||!t.StartsWith("AgonizingWoundMode",StringComparison.Ordinal)).ToArray();
  Check(tokens.Select(Id).Distinct().Count()==(fresh061?4:8)&&tokens.All(t=>A<BlueprintScriptableObject>(c,t)!=null)&&(!fresh061||legacy.Except(tokens).All(t=>c.Load(Id(t))==null)),"060 real-cache identities retain every live resource;061 retires exactly four superseded wound shortcuts");
  if(fresh061){
   foreach(string token in legacy.Where(t=>t.StartsWith("AgonizingWoundMode",StringComparison.Ordinal)))Check(c.Load(Id(token))==null,"061 exact superseded wound shortcut is absent from the real cache: "+token);
   Check(Main.GetType("CallOfTheWild.Wrath.Prototype.SelectAgonizingMode01460")==null,"061 obsolete one-shot wound serialized action is actually absent from the DLL");
   Check(!A<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection").AllFeatures.Any(f=>object.ReferenceEquals(f,A<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(c,"AgonizingWoundTargetAbility01458"))),"061 target ability never enters the amplification feature pool");
  }
  PsychicExpansionTests.Inventory["freshSaveRetirementPolicy061"]=fresh061;
  PsychicExpansionTests.Inventory["newBlueprints01460"]=new Newtonsoft.Json.Linq.JArray(tokens.Select(t=>new Newtonsoft.Json.Linq.JObject{["id"]=Id(t).ToString(),["name"]=c.Load(Id(t)).name,["type"]=c.Load(Id(t)).GetType().FullName}));
 }
}
