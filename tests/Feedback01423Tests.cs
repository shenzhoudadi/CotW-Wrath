using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;

// Array extent is a cross-mod contract, independent of a class's highest usable
// spell circle. Mystical Mayhem's GoldRobedWizard reads all native 0..10 slots.
// Exercise production registration first, then the actual after-LoadPackTOC hook
// in an isolated native cache populated from every installed native spell list.
internal static class Feedback01423Tests {
 sealed class Before {
  internal SpellLevelList[] Rows;
  internal Dictionary<int,BlueprintGuid[]> Spells;
  internal string Signature;
 }
 static int checks;
 static Dictionary<BlueprintSpellList,Before> registration;
 static Type helper,afterLoad;
 static void Check(bool value,string label) {if(!value)throw new Exception("Feedback01423: "+label);checks++;}
 static BlueprintAbilityReference[] Refs(SpellLevelList row)=>row==null?Array.Empty<BlueprintAbilityReference>():
  ((List<BlueprintAbilityReference>)AccessTools.Field(typeof(SpellLevelList),"m_Spells").GetValue(row)??new List<BlueprintAbilityReference>()).Where(r=>r!=null).ToArray();
 static string Signature(SpellLevelList[] rows)=>string.Join(";",(rows??Array.Empty<SpellLevelList>()).Select(r=>r==null?"null":r.SpellLevel+":"+string.Join(",",Refs(r).Select(s=>s.Guid.ToString()))));
 static Before Snapshot(BlueprintSpellList list)=>new Before {Rows=list.SpellsByLevel,Signature=Signature(list.SpellsByLevel),
  Spells=(list.SpellsByLevel??Array.Empty<SpellLevelList>()).Where(r=>r!=null).GroupBy(r=>r.SpellLevel).ToDictionary(g=>g.Key,g=>g.SelectMany(Refs).Select(s=>s.Guid).Distinct().ToArray())};
 static BlueprintSpellList[] Lists(BlueprintsCache cache) {var result=new List<BlueprintSpellList>();cache.ForEachLoaded((id,bp)=>{if(bp is BlueprintSpellList list)result.Add(list);});return result.ToArray();}
 static SpellLevelList Row(int level,IEnumerable<BlueprintAbilityReference> refs) {var row=new SpellLevelList(level);AccessTools.Field(typeof(SpellLevelList),"m_Spells").SetValue(row,refs.ToList());return row;}
 static SpellLevelList[] Build(SpellLevelList[] rows,int required=0)=>(SpellLevelList[])AccessTools.Method(helper,"Build").Invoke(null,new object[]{rows,required});
 static void Schema(BlueprintSpellList list,Before before,bool exact=false) {
  var rows=list.SpellsByLevel;
  if(rows!=null&&rows.Length==0&&ReferenceEquals(rows,before.Rows)) {Check(true,list.name+" unchanged empty native placeholder has no published circle contract");return;}
  Check(rows!=null&&rows.Length>=11,list.name+" supports every native/MM slot 0..10");
  Check(rows.Length>=(before.Rows?.Length??0),list.name+" does not shrink input array ("+(before.Rows?.Length??0)+" -> "+rows.Length+")");
  Check(rows.Select((row,index)=>row!=null&&row.SpellLevel==index).All(x=>x),list.name+" each row's numeric circle equals array index");
  Check(rows.Skip(11).All(r=>Refs(r).Length==0),list.name+" extra reserved slots are empty, not new spell circles");
  foreach(var pair in before.Spells) {
   Check(pair.Value.All(id=>rows[pair.Key].SpellsRefs.Any(r=>r!=null&&r.Guid==id)),list.name+" retains all prior spell GUIDs at circle "+pair.Key);
   if(exact)Check(Refs(rows[pair.Key]).Select(r=>r.Guid).Distinct().OrderBy(g=>g.ToString()).SequenceEqual(pair.Value.OrderBy(g=>g.ToString())),list.name+" merge neither moves nor invents spells at circle "+pair.Key);
  }
  if(!ReferenceEquals(before.Rows,rows))Check(Signature(before.Rows)==before.Signature,list.name+" donor rows/references were not mutated");
 }
 // Source-matched indices from GoldRobedWizard.MakeSpellList(): destination has
 // eleven rows, Wizard[i] for i=0..10 and Cleric[i-1] for i=2..8. Native raw refs
 // suffice for this startup failure; school filtering does not change bounds.
 static int GoldRobedFixedReads(BlueprintSpellList wizard,BlueprintSpellList cleric) {
  int reads=0;var goldRows=Enumerable.Range(0,11).Select(i=>new SpellLevelList(i)).ToArray();
  for(int i=0;i<goldRows.Length;i++) {
   var wizardRow=wizard.SpellsByLevel[i];reads++;Check(wizardRow!=null,"MM Wizard slot "+i+" is non-null");
   if(i>1&&i<9){var clericRow=cleric.SpellsByLevel[i-1];reads++;Check(clericRow!=null,"MM Cleric source slot "+(i-1)+" is non-null");}
  }
  return reads;
 }
 static int CollegiateFixedReads(BlueprintsCache cache) {
  int reads=0;
  foreach(string id in new[]{"ba0401fdeb4062f40a7aa95b6f07fe89","bad8638d40639d04fa2f80a1cac67d6b","8443ce803d2d31347897a3d85cc32f53"}) {
   var list=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse(id));for(int i=0;i<11;i++){Check(list.SpellsByLevel[i]!=null,"Ebons CollegiateInitiate "+list.name+" slot "+i);reads++;}
  }
  return reads;
 }
 internal static void Capture(BlueprintsCache cache,Assembly assembly) {
  checks=0;helper=assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellListCompatibility",true);
  afterLoad=assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellListCompatibilityAfterLoad",true);
  registration=Lists(cache).ToDictionary(list=>list,Snapshot);
 }
 internal static JObject VerifyRegistered(BlueprintsCache cache,string evidence) {
  // Deliberately first: when run against the certified 0.14.22 DLL this performs
  // the failing fixed index read against the actually registered Wizard list.
  var wizard=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89"));
  var cleric=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse("8443ce803d2d31347897a3d85cc32f53"));
  int mmReads=GoldRobedFixedReads(wizard,cleric),ecmReads=CollegiateFixedReads(cache),changed=0;
  var native=JObject.Parse(File.ReadAllText(evidence));var nativeTypes=(JObject)native["nativeBlueprintTypes"];
  var audit=new JArray();
  foreach(var list in Lists(cache)) {
   Before before;bool existed=registration.TryGetValue(list,out before);
   if(!existed)before=new Before {Rows=Array.Empty<SpellLevelList>(),Spells=new Dictionary<int,BlueprintGuid[]>(),Signature=""};
   bool modified=!existed||!ReferenceEquals(before.Rows,list.SpellsByLevel)||before.Signature!=Signature(list.SpellsByLevel);
   bool own=list.name!=null&&list.name.StartsWith("CotWWrath");
   if(!modified&&!own)continue;
   Schema(list,before);changed++;
   foreach(var reference in list.SpellsByLevel.SelectMany(Refs)) {
    string nativeType=(string)nativeTypes[reference.Guid.ToString()];
    Check(nativeType=="BlueprintAbility"||cache.Load(reference.Guid) is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility,list.name+" valid ability reference "+reference.Guid);
   }
   audit.Add(new JObject { ["guid"]=list.AssetGuid.ToString(),["name"]=list.name,["beforeRows"]=before.Rows.Length,["afterRows"]=list.SpellsByLevel.Length,["newOrModified"]=modified,["nativeOrOwn"]=own?"CotW":"native/provider" });
  }
  Check(changed>=12,"checks the actual shared-list caller's broad native class list set plus owned Psychic");
  foreach(string id in new[]{"57c894665b7895c499b3dce058c284b3","9f5be2f7ea64fe04eb40878347b147bc","29f3c338532390546bc5347826a655c4"}) {
   var list=(BlueprintSpellList)cache.Load(BlueprintGuid.Parse(id));for(int i=0;i<=10;i++)Check(list.SpellsByLevel[i]!=null,list.name+" low-caster fixed index "+i+" retained");
  }
  int registrationChecks=checks;
  var allNative=AllNative(native);
  Console.WriteLine("PASS: "+checks+" 01423 spell-list extent/index/reference and MM fixed-read contracts.");
  return new JObject { ["passed"]=true,["assertions"]=checks,["productionRegistrationAssertions"]=registrationChecks,["actualRegistrationListsChecked"]=changed,["registrationLists"]=audit,["goldRobedWizardFixedReads"]=mmReads,["ebonsCollegiateFixedReads"]=ecmReads,["allNative"]=allNative,
   ["boundaries"]="Actual production Psychic registration, real game BlueprintsCache/BlueprintSpellList/SpellLevelList, actual CotW after-LoadPackTOC postfix, installed native JSON for all spell lists. MM source-matched fixed indices executed on registered native Wizard/Cleric lists; raw reference identity checks avoid Unity assets and spell execution. Full Mystical Mayhem/ECM startup and Unity scene loading are tester regression steps." };
 }
 static JObject AllNative(JObject evidence) {
  var native=(JArray)evidence["allNativeSpellLists"];Check(native!=null&&native.Count>100,"extractor supplies every installed native spell list, not a hand-picked fixture");
  var originalCache=ResourcesLibrary.BlueprintsCache;var cacheField=AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache");
  int first=checks,listsChecked=0,rowsChecked=0,refsChecked=0;var inputLengths=new HashSet<int>();
  try {
   // Three fresh-cache cycles cover native-only, DragonChanges-shaped duplicate
   // row appends, and that shape with a null slot. The concurrent ToyBox writer
   // remains covered separately by the unchanged real-cache startup suite.
   for(int cycle=0;cycle<3;cycle++) {
    var cache=new BlueprintsCache();cacheField.SetValue(null,cache);var snapshots=new Dictionary<BlueprintSpellList,Before>();
    foreach(var item in native) {
     var bp=item["blueprint"];var rows=(SpellLevelList[])PsychicExpansionTests.Read(bp["Data"]["SpellsByLevel"],typeof(SpellLevelList[]));
     rows=rows??Array.Empty<SpellLevelList>();inputLengths.Add(rows.Length);
     if(cycle>0) {
      var donor=rows.FirstOrDefault(r=>r!=null&&r.SpellLevel<=10&&Refs(r).Length>0);
      if(donor!=null)rows=rows.Concat(new[]{Row(donor.SpellLevel,Refs(donor)),Row(donor.SpellLevel,Refs(donor).Reverse())}).ToArray();
      if(cycle==2)rows=rows.Concat(new SpellLevelList[]{null}).ToArray();
     }
     var list=new BlueprintSpellList {name=(string)item["name"],AssetGuid=BlueprintGuid.Parse((string)bp["AssetId"]),ComponentsArray=Array.Empty<BlueprintComponent>(),SpellsByLevel=rows};
     cache.AddCachedBlueprint(list.AssetGuid,list);snapshots.Add(list,Snapshot(list));
     var built=Build(rows);Check(built!=null,list.name+" production Build accepts current native numeric levels");
     var probe=new BlueprintSpellList {name=list.name+" / Build",SpellsByLevel=built};Schema(probe,snapshots[list],true);
    }
    AccessTools.Method(afterLoad,"Postfix").Invoke(null,null);
    foreach(var pair in snapshots) {
     Schema(pair.Key,pair.Value,true);listsChecked++;rowsChecked+=pair.Key.SpellsByLevel.Length;refsChecked+=pair.Key.SpellsByLevel.Sum(r=>Refs(r).Length);
    }
    var once=snapshots.Keys.ToDictionary(l=>l,l=>l.SpellsByLevel);AccessTools.Method(afterLoad,"Postfix").Invoke(null,null);
    foreach(var pair in once)Check(ReferenceEquals(pair.Key.SpellsByLevel,pair.Value),pair.Key.name+" canonical repeat does not replace rows");
    GoldRobedFixedReads((BlueprintSpellList)cache.Load(BlueprintGuid.Parse("ba0401fdeb4062f40a7aa95b6f07fe89")),(BlueprintSpellList)cache.Load(BlueprintGuid.Parse("8443ce803d2d31347897a3d85cc32f53")));
    CollegiateFixedReads(cache);
   }
   return new JObject { ["passed"]=true,["assertions"]=checks-first,["nativeSpellLists"]=native.Count,["freshCacheCycles"]=3,["listsChecked"]=listsChecked,["rowsChecked"]=rowsChecked,["spellReferencesChecked"]=refsChecked,["nativeInputLengths"]=new JArray(inputLengths.OrderBy(x=>x)),["noShrink"]=true,["indexEqualsLevel"]=true,["donorReferencesRetained"]=true,["canonicalRepeatStable"]=true,["actualAfterLoadPostfix"]=true };
  }finally{cacheField.SetValue(null,originalCache);}
 }
}
