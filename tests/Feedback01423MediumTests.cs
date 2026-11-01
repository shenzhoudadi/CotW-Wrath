using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Owlcat.QA.Validation;

// Executes the packaged Medium CreateSpellList and its bundled BlueprintCore
// configurator, against a real native cache. The caller supplies a visibility-only
// native DLL fixture for .NET Framework access to fields made public in Mono.
internal static class Feedback01423MediumTests {
 static int checks;
 static readonly List<string> warnings=new List<string>();
 static void Check(bool condition,string message) {if(!condition)throw new Exception("Feedback01423Medium: "+message);checks++;}
 static bool Skip()=>false;
 static bool TttDisabled(ref bool __result) {__result=false;return false;}
 static bool Warn(string msg) {warnings.Add(msg);return false;}
 static object Get(object o,string field)=>AccessTools.Field(o.GetType(),field).GetValue(o);
 static void Set(object o,string field,object value)=>AccessTools.Field(o.GetType(),field).SetValue(o,value);
 internal static JObject Run(Assembly medium) {
  checks=0;warnings.Clear();
  var previousCache=ResourcesLibrary.BlueprintsCache;
  var managerField=AccessTools.Field(typeof(OwlcatModificationsManager),"s_Instance");var previousManager=managerField.GetValue(null);
  var manager=(OwlcatModificationsManager)FormatterServices.GetUninitializedObject(typeof(OwlcatModificationsManager));
  Set(manager,"<AppliedModifications>k__BackingField",Array.Empty<OwlcatModification>());managerField.SetValue(null,manager);
  var h=new Harmony("CotW.Feedback01423.MediumListBoundaries");
  try {
   var cache=new BlueprintsCache();AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
   h.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope),"New",new[]{typeof(string),typeof(UnityEngine.Object)}),prefix:new HarmonyMethod(typeof(Feedback01423MediumTests),nameof(Skip)));
   var logger=medium.GetType("BlueprintCore.Utils.LogWrapper",true);
   foreach(string name in new[]{"Info","Verbose","Error"})h.Patch(AccessTools.Method(logger,name),prefix:new HarmonyMethod(typeof(Feedback01423MediumTests),nameof(Skip)));
   h.Patch(AccessTools.Method(logger,"Warn"),prefix:new HarmonyMethod(typeof(Feedback01423MediumTests),nameof(Warn)));
   h.Patch(AccessTools.Method(medium.GetType("MediumClass.Utils.Settings",true),"IsTTTBaseEnabled"),prefix:new HarmonyMethod(typeof(Feedback01423MediumTests),nameof(TttDisabled)));
   var spellbook=medium.GetType("MediumClass.Medium.MediumSpellbook",true);
   AccessTools.Method(spellbook,"CreateSpellList").Invoke(null,null);
   var id=BlueprintGuid.Parse("ff3e034659b64d21ac57dc0d9e893bdb");
   var list=cache.Load(id) as BlueprintSpellList;
   Check(list!=null&&list.name=="MediumSpellList","actual production configurator publishes original Medium list identity");
   Check(list.SpellsByLevel.Length==11,"Medium reserves all native indices0 through10");
   var names=new[]{
    new[]{"Daze","Flare","Guidance","MageLight","Resistance","Stabilize","Virtue"},
    new[]{"Command","CauseFear","EarPiercingScream","EnlargePerson","ExpeditiousRetreat","ReducePerson","RemoveFear","SummonMonsterISingle","TrueStrike"},
    new[]{"Aid","BearsEndurance","BestowCurse","Blur","BullsStrength","CatsGrace","EaglesSplendor","FalseLife","FindTraps","FoxsCunning","Haste","Heroism","HideousLaughter","Invisibility","MirrorImage","OwlsWisdom","Slow","SummonMonsterIIBase"},
    new[]{"DimensionDoor","DispelMagic","Displacement","FalseLifeGreater","InvisibilityGreater","PhantasmalKiller","RemoveCurse","SummonMonsterIVBase","VampiricTouch"},
    new[]{"BreakEnchantment","CommandGreater","DeathWard","Dismissal","Fear","FreedomOfMovement","PhantasmalPutrefaction","SummonMonsterVBase","TrueSeeing"}
   };
   var refsType=medium.GetType("BlueprintCore.Blueprints.References.AbilityRefs",true);
   for(int level=0;level<list.SpellsByLevel.Length;level++) {
    var row=list.SpellsByLevel[level];Check(row!=null&&row.SpellLevel==level,"numeric index agrees at "+level);
    var refs=((List<BlueprintAbilityReference>)Get(row,"m_Spells"))??new List<BlueprintAbilityReference>();
    if(level<names.Length) {
     var expected=names[level].Select(name=>BlueprintGuid.Parse(AccessTools.Field(refsType,name).GetValue(null).ToString())).ToArray();
     Check(refs.Select(r=>r.Guid).SequenceEqual(expected),"frozen022 native spell sequence is retained at "+level);
    } else Check(refs.Count==0,"reserved higher index grants no spell at "+level);
    foreach(var r in refs)cache.AddCachedBlueprint(r.Guid,new BlueprintAbility {AssetGuid=r.Guid,name="01423MediumNativeReference",Type=AbilityType.Spell,ComponentsArray=Array.Empty<BlueprintComponent>()});
   }
   using(var validation=new ValidationContext("01423 actual Medium list")) {
    list.Validate(validation,-1);
    Check(!validation.Errors.Any(e=>e.Contains("wrong level value")),"actual native validator accepts Medium fixed-index layout");
   }
   Check(list.MaxLevel==4,"actual native maximum remains4 despite capacity11");
   for(int i=0;i<=10;i++)Check(list.GetSpells(i)!=null,"native and external fixed-index reads remain in bounds at "+i);
   Check(!warnings.Any(w=>w.Contains("wrong level value")),"actual bundled BlueprintCore did not warn about invalid indices");
   return new JObject { ["passed"]=true,["assertions"]=checks,["actualCreateSpellList"]=true,["actualBundledBlueprintCore"]=true,["actualNativeCache"]=true,["length"]=list.SpellsByLevel.Length,["maxLevel"]=list.MaxLevel,["oldSpellRowsPreserved"]=true,["boundaries"]="Actual final Medium CreateSpellList, bundled BlueprintCore configuration and native cache/validation/GetSpells/MaxLevel. Visibility-only native assembly fixture retains method bodies. Test-only UMM reference skips its environment initializer; TTT detection forced off, profiler and logging bypassed. Native spell references receive inert cached ability endpoints after configuration. No UMM installation or game startup." };
  } finally {h.UnpatchAll(h.Id);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,previousCache);managerField.SetValue(null,previousManager);}
 }
}
