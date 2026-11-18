using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Owlcat.Runtime.Core.Logging;
using Way_of_the_shield;

internal static class BlueprintLauncher
{
 static int Main(string[] args)
 {
  if(args.Length!=4&&args.Length!=5)return 2;
  var runtimePath=Environment.GetEnvironmentVariable("COTW_HARMONY_MATRIX_PATH");
  if(!string.IsNullOrEmpty(runtimePath))Assembly.LoadFrom(runtimePath);
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{if(new AssemblyName(e.Name).Name=="0Harmony"&&!string.IsNullOrEmpty(runtimePath))return Assembly.LoadFrom(runtimePath);foreach(string d in new[]{args[0],Path.GetDirectoryName(args[1]),args.Length==5?Path.GetDirectoryName(args[4]):args[0]}){string p=Path.Combine(d,new AssemblyName(e.Name).Name+".dll");if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
  try{Assembly.LoadFrom(args[1]);typeof(BlueprintLauncher).Assembly.GetType("BlueprintRegistrationTests").GetMethod("Run").Invoke(null,new object[]{args});return 0;}
  catch(Exception e){while(e!=null){Console.Error.WriteLine(e.GetType().FullName+": "+e.Message);Console.Error.WriteLine(e.StackTrace);e=e.InnerException;}return 1;}
 }
}

public static class BlueprintRegistrationTests
{
 static int checks;static BlueprintRoot root;static UnityEngine.Sprite sprite;static readonly Type[] types=typeof(BlueprintScriptableObject).Assembly.GetTypes();
 static readonly List<string> misses=new();
 static T Empty<T>()where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Blueprint integration: "+why);}
 static FieldInfo Field(Type type,string name)=>AccessTools.Field(type,name)??AccessTools.Field(type,"<"+name+">k__BackingField");
 static void Set(object o,string name,object value)=>(Field(o.GetType(),name)??throw new Exception("Fixture field "+o.GetType()+"."+name)).SetValue(o,value);
 static bool Skip()=>false;
 static bool Icon(ref UnityEngine.Sprite __result){__result=sprite;return false;}
 static bool Root(ref BlueprintRoot __result){__result=root;return false;}
 static bool RecordWarning(string __0,object[] __1){misses.Add(__1==null||__1.Length==0?__0:string.Format(__0,__1));return false;}
 static string Sha(string path){using(var hash=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","");}
 static string[] Ids(BlueprintFeatureSelection pool,string field)=>((BlueprintFeatureReference[])Field(pool.GetType(),field).GetValue(pool)??Array.Empty<BlueprintFeatureReference>()).Where(r=>r!=null).Select(r=>r.Guid.ToString()).ToArray();
 static BlueprintFeatureReference R(string id){var r=new BlueprintFeatureReference();Set(r,"deserializedGuid",BlueprintGuid.Parse(id));return r;}
 static BlueprintFeatureSelection Pool(string id,string name,string[] all,string[] visible){var p=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(id),name=name,Group=FeatureGroup.CombatFeat};Set(p,"m_AllFeatures",all.Select(R).ToArray());Set(p,"m_Features",visible.Select(R).ToArray());return p;}
 static JObject ParentBridge(BlueprintsCache cache,Dictionary<string,BlueprintScriptableObject> native,BlueprintScriptableObject[] owned,string parentPath)
 {
  var parent=Assembly.LoadFrom(parentPath);Check(Sha(parent.Location)==Sha(parentPath),"tested parent integration bytes");
  var repair=parent.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolRepair",true);
  var nativeIds=(string[])AccessTools.Field(repair,"NativeIds").GetValue(null);var optionalIds=(string[])AccessTools.Field(repair,"OptionalIds").GetValue(null);
  var pools=nativeIds.Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection??throw new Exception("Missing native parent combat pool "+id)).ToList();
  string[] anchors={"4c44724ffa8844f4d9bedb5bb27d144a","97e216dbb46ae3c4faef90cf6bbe6fd5","9972f33f977fc724c838e59641b2fca5"};
  foreach(string id in optionalIds){var pool=Pool(id,"OptionalProviderContractPool",anchors.Concat(new[]{"ac57069b6bf8c904086171683992a92a"}).ToArray(),anchors.Take(1).ToArray());cache.AddCachedBlueprint(pool.AssetGuid,pool);pools.Add(pool);}
  var full=Pool("a3efd955dfab4dd79c8cbbdd9cfbd591","FullAnchoredProviderContract",anchors,Array.Empty<string>());cache.AddCachedBlueprint(full.AssetGuid,full);pools.Add(full);
  var sparse=Pool("e5a299b2ec714e2d8a2bdc4d01a3ca8d","RestrictedCombatSubmenuContract",anchors.Take(2).ToArray(),anchors.Take(1).ToArray());cache.AddCachedBlueprint(sparse.AssetGuid,sparse);
  var old=pools.ToDictionary(p=>p,p=>new[]{Ids(p,"m_AllFeatures"),Ids(p,"m_Features")});
  AccessTools.Method(repair,"Reconcile").Invoke(null,new object[]{cache});AccessTools.Method(repair,"Reconcile").Invoke(null,new object[]{cache});
  foreach(var pool in pools)foreach(string field in new[]{"m_AllFeatures","m_Features"}){var current=Ids(pool,field);foreach(string id in Main.CombatFeatIds)Check(current.Count(x=>x==id)==1,"parent native/optional pool has one "+id+" in "+field+" / "+pool.name);Check(old[pool][field=="m_AllFeatures"?0:1].All(current.Contains),"parent preserves old entries "+pool.name+" / "+field);}
  foreach(string field in new[]{"m_AllFeatures","m_Features"})Check(!Ids(sparse,field).Any(Main.CombatFeatIds.Contains),"sparse restricted submenu remains bounded "+field);
  var originalLight=(BlueprintFeature)cache.Load(BlueprintGuid.Parse("3a76e2e292d14c4184165fffdbc0c4e0"));
  var externalCache=new BlueprintsCache();cache.ForEachLoaded((id,bp)=>{if(id!=originalLight.AssetGuid)externalCache.AddCachedBlueprint(id,bp);});
  var bridge=parent.GetType("CallOfTheWild.Wrath.Prototype.WayOfTheShieldIntegration",true);var externalFlag=AccessTools.Field(bridge,"external");var priorFlag=externalFlag.GetValue(null);
  try{AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,externalCache);externalFlag.SetValue(null,true);var register=AccessTools.Method(bridge,"RegisterExternalLightParry");register.Invoke(null,new object[]{externalCache});var feature=(BlueprintFeature)externalCache.Load(originalLight.AssetGuid);Check(feature!=null&&feature.GetComponent<PrerequisiteProficiency>().ArmorProficiencies.SequenceEqual(new[]{Kingmaker.Blueprints.Items.Armors.ArmorProficiencyGroup.LightShield}),"external upstream provider receives missing light-shield entrance");Check(((BlueprintUnitFactReference[])Field(typeof(AddFacts),"m_Facts").GetValue(feature.GetComponent<AddFacts>())).Single().Guid==BlueprintGuid.Parse("e547039b227a42d7a8d98fad72f8717c"),"external entrance shares existing provider parry engine");Check(feature.Groups.Contains(FeatureGroup.Feat)&&feature.Groups.Contains(FeatureGroup.CombatFeat),"external entrance combat metadata");register.Invoke(null,new object[]{externalCache});Check(ReferenceEquals(externalCache.Load(feature.AssetGuid),feature),"external fallback repeated registration retains identity");Check(ReferenceEquals(externalCache.Load(BlueprintGuid.Parse("e547039b227a42d7a8d98fad72f8717c")),cache.Load(BlueprintGuid.Parse("e547039b227a42d7a8d98fad72f8717c"))),"external fallback never duplicates shared parry engine");}
  finally{externalFlag.SetValue(null,priorFlag);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);}
  return new JObject{["nativePools"]=nativeIds.Length,["optionalContracts"]=optionalIds.Length,["threeAnchorClone"]=true,["sparseMenuProtected"]=true,["externalLightParryFallback"]=true,["parentSha256"]=Sha(parentPath)};
 }
 static object Read(JToken value,Type target)
 {
  if(value==null||value.Type==JTokenType.Null)return null;
  if(typeof(BlueprintReferenceBase).IsAssignableFrom(target)){var r=(BlueprintReferenceBase)Activator.CreateInstance(target);Set(r,"deserializedGuid",BlueprintGuid.Parse(((string)value).Replace("!bp_","")));return r;}
  if(target.IsEnum)return Enum.Parse(target,(string)value);
  if(target==typeof(string)||target.IsPrimitive||target==typeof(decimal))return value.ToObject(target);
  if(target==typeof(BlueprintGuid))return BlueprintGuid.Parse((string)value);
  if(typeof(UnityEngine.Object).IsAssignableFrom(target)||target==typeof(UnityEngine.AnimationCurve))return null;
  if(target.IsArray){var values=(JArray)value;var a=Array.CreateInstance(target.GetElementType(),values.Count);for(int i=0;i<values.Count;i++)a.SetValue(Read(values[i],target.GetElementType()),i);return a;}
  if(typeof(IList).IsAssignableFrom(target)&&target.IsGenericType){var list=(IList)Activator.CreateInstance(target);foreach(var child in (JArray)value)list.Add(Read(child,target.GetGenericArguments()[0]));return list;}
  if(value is JObject obj)
  {
   if(obj["$type"]!=null){string name=((string)obj["$type"]).Split(',').Last().Trim();var match=types.Where(t=>t.Name==name&&target.IsAssignableFrom(t)).ToArray();if(match.Length!=1)throw new Exception("Native JSON type "+name+" into "+target);target=match[0];}
   var result=target==typeof(SpellLevelList)?new SpellLevelList((int)obj["SpellLevel"]):Activator.CreateInstance(target,true);
   foreach(var p in obj.Properties().Where(p=>p.Name!="$type")){var field=Field(target,p.Name);if(field!=null&&!field.IsInitOnly)try{field.SetValue(result,Read(p.Value,field.FieldType));}catch(Exception e){throw new Exception("Native JSON "+target.FullName+"."+p.Name+": "+e.Message,e);}}
   if(result is ContextRankConfig&&Field(target,"Archetype")?.GetValue(result)==null)Field(target,"Archetype")?.SetValue(result,new BlueprintArchetypeReference());
   return result;
  }
  return value.ToObject(target);
 }
 static BlueprintScriptableObject BuildRow(JToken row)
 {
  var data=(JObject)row["blueprint"]["Data"];string kind=((string)data["$type"]).Split(',').Last().Trim();
  var target=types.Single(t=>t.Name==kind&&typeof(BlueprintScriptableObject).IsAssignableFrom(t));
  var bp=(BlueprintScriptableObject)Activator.CreateInstance(target);bp.AssetGuid=BlueprintGuid.Parse((string)row["blueprint"]["AssetId"]);bp.name=(string)row["name"];
  bp.ComponentsArray=((JArray)data["Components"]??new JArray()).Select(c=>(BlueprintComponent)Read(c,typeof(BlueprintComponent))).ToArray();
  foreach(var p in data.Properties().Where(p=>p.Name!="$type"))
  {
   if(p.Name=="Components")continue;
   if(bp is BlueprintRoot&&p.Name!="Components"&&p.Name!="SystemMechanics"&&p.Name!="Progression")continue;
   var field=Field(target,p.Name=="Components"?"ComponentsArray":p.Name);
   if(field!=null&&!field.IsInitOnly)try{field.SetValue(bp,Read(p.Value,field.FieldType));}catch(Exception e){throw new Exception("Native blueprint "+bp.name+"."+p.Name+": "+e.Message,e);}
  }
  if(bp is BlueprintUnitFact)Set(bp,"m_Icon",sprite);
  return bp;
 }
 public static void Run(string[] args)
 {
  var actualHarmony=typeof(Harmony).Assembly;
  var runtimePath=Environment.GetEnvironmentVariable("COTW_HARMONY_MATRIX_PATH");var selected=string.IsNullOrEmpty(runtimePath)?Path.Combine(Path.GetFullPath(args[0]),"UnityModManager","0Harmony.dll"):Path.GetFullPath(runtimePath);
  Check(actualHarmony.GetName().Version==AssemblyName.GetAssemblyName(selected).Version,"explicit runtime version");
  Check(Sha(actualHarmony.Location)==Sha(selected),"explicit runtime bytes");
  Check(Path.GetFullPath(actualHarmony.Location).Equals(selected,StringComparison.OrdinalIgnoreCase),"explicit runtime resolver");
  Check(Sha(typeof(Main).Assembly.Location)==Sha(args[1]),"requested compiled module bytes are tested");
  var boundary=new Harmony("CotW.WotS.BlueprintTests.VisualAndLoggingBoundary");sprite=Empty<UnityEngine.Sprite>();
  void Patch(MethodBase m,string name){if(m==null)throw new Exception("Fixture boundary missing "+name);boundary.Patch(m,prefix:new HarmonyMethod(typeof(BlueprintRegistrationTests),name));}
  Patch(AccessTools.Method(typeof(ProfileScope),"New",new[]{typeof(string),typeof(UnityEngine.Object)}),nameof(Skip));
  Patch(AccessTools.Method(typeof(Utilities),"LoadIcon"),nameof(Icon));Patch(AccessTools.Method(typeof(ResourcesLibrary),"GetRoot"),nameof(Root));
  foreach(string method in new[]{"Log","Error"})Patch(AccessTools.Method(typeof(LogChannel),method,new[]{typeof(string),typeof(object[])}),nameof(Skip));
  Patch(AccessTools.Method(typeof(LogChannel),"Warning",new[]{typeof(string),typeof(object[])}),nameof(RecordWarning));
  try
  {
   var input=JObject.Parse(File.ReadAllText(args[2]));var rows=(JArray)input["requiredNative"];var nativeIds=new HashSet<string>(input["allNativeIds"].Select(t=>(string)t));
   Check(nativeIds.Count>230000,"complete actual-game native GUID inventory");
   var cache=new BlueprintsCache();AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
   LocalizationManager.CurrentPack=new LocalizationPack{Locale=Locale.enGB};
   var native=new Dictionary<string,BlueprintScriptableObject>();
   foreach(var row in rows){var bp=BuildRow(row);cache.AddCachedBlueprint(bp.AssetGuid,bp);native.Add(bp.AssetGuid.ToString(),bp);Check(nativeIds.Contains(bp.AssetGuid.ToString()),"fixture identity occurs in installed archive: "+bp.name);}
   root=(BlueprintRoot)native.Values.Single(bp=>bp is BlueprintRoot);Check(root.SystemMechanics.MythicFeatSelection!=null,"actual native root mythic selection resolves");
   foreach(var bp in native.Values)bp.OnEnable();
   Check(native["ca22afeb94442b64fb8536e7a9f7dc11"].GetComponent<AddFacts>()!=null,"actual native defensive feature AddFacts loaded");
   Console.WriteLine("Registering selected module using "+rows.Count+" actual native archive rows");
   JObject startupRetry=args.Length==5?ShieldStartupRetry01444.Run(cache,args[4]):null;
   Main.ConfigureIntegrated(cache);
   Check(native["ca22afeb94442b64fb8536e7a9f7dc11"].GetComponents<AddFacts>().SelectMany(c=>(BlueprintUnitFactReference[])Field(typeof(AddFacts),"m_Facts").GetValue(c)).Any(r=>r.Guid==BlueprintGuid.Parse("eb322fe21ec64a928ace4eaedb6d4339")),"buckler two-handed permission granted through native starting feature");
   var owned=Utilities.Owned.Values.ToArray();Check(owned.Length>50,"all selected builders publish real assets");
   foreach(var bp in owned)
   {
    Check(!nativeIds.Contains(bp.AssetGuid.ToString()),"owned identity does not collide with installed game "+bp.name);
    Check(ReferenceEquals(cache.Load(bp.AssetGuid),bp),"owned cache identity stable "+bp.name);
    Check((bp.ComponentsArray??Array.Empty<BlueprintComponent>()).All(c=>c!=null&&ReferenceEquals(c.OwnerBlueprint,bp)),"native OnEnable initializes every component owner "+bp.name);
    Check((bp.ComponentsArray??Array.Empty<BlueprintComponent>()).Select(c=>c.name).Distinct().Count()==(bp.ComponentsArray??Array.Empty<BlueprintComponent>()).Length,"component names unique "+bp.name);
   }
   var basic=(BlueprintFeatureSelection)native["247a4068296e8be42890143f451b4b45"];var fighter=(BlueprintFeatureSelection)native["41c8486641f7d6d4283ca9dae4147a9f"];
   foreach(string id in Main.CombatFeatIds){var f=(BlueprintFeature)cache.Load(BlueprintGuid.Parse(id));Check(f.Groups.Contains(FeatureGroup.Feat)&&f.Groups.Contains(FeatureGroup.CombatFeat),"combat feature flags "+f.name);Check(f.GetComponents<Prerequisite>().Any(),"real prerequisites "+f.name);foreach(var pool in new[]{basic,fighter})Check(Ids(pool,"m_AllFeatures").Count(x=>x==id)==1,"single feat selection entry "+pool.name+" / "+f.name);}
   foreach(string id in Main.MythicFeatIds){var f=(BlueprintFeature)cache.Load(BlueprintGuid.Parse(id));Check(f.Groups.Contains(FeatureGroup.MythicFeat),"mythic feature flag "+f.name);Check(f.GetComponents<PrerequisiteFeature>().Any(),"mythic prerequisite "+f.name);Check(Ids(root.SystemMechanics.MythicFeatSelection,"m_AllFeatures").Count(x=>x==id)==1,"single native mythic selection entry "+f.name);}
   foreach(string id in new[]{"a86db1a253d0446b857938314af1ae51","7ca07f3bdbf145c2a975b11d9690c0b3","15542aa4e3f542a4bbd66bcb13392798"}){var a=(BlueprintActivatableAbility)cache.Load(BlueprintGuid.Parse(id));Check(a.Buff!=null&&Utilities.Owned.ContainsKey(a.Buff.AssetGuid),"ability references owned working buff "+a.name);Check(a.GetComponents<Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestriction>().Any(),"ability equipment restriction "+a.name);}
   var archetype=(BlueprintArchetype)cache.Load(BlueprintGuid.Parse("56f19f65e28b4c3fb03e425fb047e08a"));var paladin=(BlueprintCharacterClass)native["bfa11238e7ae3544bbeb4d0b92e897ec"];
   Check(ReferenceEquals(Field(archetype.GetType(),"m_ParentClass").GetValue(archetype),paladin)&&((BlueprintArchetypeReference[])Field(paladin.GetType(),"m_Archetypes").GetValue(paladin)).Count(a=>a?.Guid==archetype.AssetGuid)==1,"Paladin Sacred Shield entrance exactly once");
   Check(archetype.AddFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,4,5,8,9,11,13,14,17,20}),"Sacred Shield ten native level entries");
   Check(archetype.RemoveFeatures.Select(e=>e.Level).SequenceEqual(new[]{1,4,5,11,20}),"Sacred Shield exact native replacement levels");
   Check(archetype.StartingItems.Length==4&&archetype.StartingItems.All(x=>x!=null),"starting armor/weapon/shield dependencies resolve");
   foreach(var entry in archetype.AddFeatures)Check(entry.Features.All(f=>f!=null&&Utilities.Owned.ContainsKey(f.AssetGuid)),"archetype feature references resolve at level "+entry.Level);
   var before=((IDictionary)AccessTools.Field(typeof(BlueprintsCache),"m_LoadedBlueprints").GetValue(cache)).Count;
   Main.ConfigureIntegrated(cache);Main.ConfigureIntegrated(cache);
   Check(Utilities.Owned.Count==owned.Length&&((IDictionary)AccessTools.Field(typeof(BlueprintsCache),"m_LoadedBlueprints").GetValue(cache)).Count==before,"whole integration repeated registration is idempotent");
   Check(owned.All(bp=>ReferenceEquals(cache.Load(bp.AssetGuid),bp)),"repeated whole integration retains exact owned objects");
   var targets=Harmony.GetAllPatchedMethods().Where(m=>Harmony.GetPatchInfo(m).Owners.Contains("CotW.WayOfTheShield")).OrderBy(m=>m.DeclaringType.FullName).ThenBy(m=>m.Name).Select(m=>m.DeclaringType.FullName+"::"+m).ToArray();
   Check(targets.Length>=35,"actual installed curated runtime targets recorded");
   JObject bridgeEvidence=args.Length==5?ParentBridge(cache,native,owned,args[4]):null;
   var report=new JObject{["assertions"]=checks,["nativeRows"]=rows.Count,["nativeIds"]=nativeIds.Count,["ownedAssets"]=owned.Length,["runtimeTargetCount"]=targets.Length,["runtimeTargets"]=JArray.FromObject(targets),["parentIntegration"]=bridgeEvidence,["missingWarnings"]=JArray.FromObject(misses),["assets"]=JArray.FromObject(owned.Select(bp=>new{guid=bp.AssetGuid.ToString(),name=bp.name,type=bp.GetType().FullName})),["passed"]=true,["scope"]="Actual installed native archive JSON and native method bodies; complete real ConfigureIntegrated builders, OnEnable, caches, prerequisites and runtime Harmony registration. Unity icon loading, profiling/log emission and root access are visual/service boundaries; root contents and all gameplay fields are native records. Optional pool clones test provider contracts, not external assembly execution. No Unity scene, combat, full leveling UI or save reload acceptance."};
   report["shieldStartupRetry01444"]=startupRetry;
   File.WriteAllText(args[3],report.ToString());Console.WriteLine("PASS "+checks+" whole-module native blueprint/runtime checks; "+owned.Length+" assets; "+targets.Length+" installed targets; shield startup recovery "+startupRetry?["assertions"]);
  }
  finally{Main.harmony?.UnpatchAll("CotW.WayOfTheShield");boundary.UnpatchAll(boundary.Id);}
 }
}
