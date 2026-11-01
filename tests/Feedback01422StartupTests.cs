using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.BinaryFormat;
using Kingmaker.Modding;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Owlcat.QA.Validation;

// Exercises the installed game's actual cache, dictionary CopyTo/enumerator, Load
// lock and cache write. Only pack deserialization/profiling/Unity logging are test
// boundaries. Timing patches retain the native bodies. The old ForEachLoaded hook
// schedules native Load between MoveNext calls; the new CopyTo hook also schedules
// a ToyBox-shaped direct dictionary writer which does not acquire the native lock.
internal static class Feedback01422StartupTests {
 static int checks, callbackCount, nativeLoads, referenceLoads, buildCalls, snapshotAttempts, snapshotMutationsRemaining, snapshotInserts;
 static BlueprintsCache activeCache;
 static SimpleBlueprint pendingBlueprint;
 static ManualResetEvent loaderStart, loaderAttempt, loaderDone;
 static bool schedule, callbackHeldNativeLock, workerBlocked, injectSnapshotFailure;
 static SpellLevelList[] failingRows;
 static bool directCopyWriter;
 static ManualResetEvent directStart,directFirstWrite,directStop;
 static int directWrites,directCopyCalls;
 static Type compatibility, postfixType;
 static readonly List<string> logMessages = new List<string>();
 static void Check(bool value,string label) { if(!value) throw new Exception("Feedback01422Startup: "+label);checks++; }
 static object Get(object o,string name)=>AccessTools.Field(o.GetType(),name).GetValue(o);
 static void Set(object o,string name,object value)=>AccessTools.Field(o.GetType(),name).SetValue(o,value);
 static T Empty<T>() where T:class=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static BlueprintGuid Id(int n)=>new BlueprintGuid(new Guid(n,0x1422,0x1422,new byte[]{1,2,3,4,5,6,7,8}).ToByteArray());
 static BlueprintAbilityReference Ref(int n) {var r=new BlueprintAbilityReference();Set(r,"deserializedGuid",Id(n));return r;}
 static SpellLevelList Row(int level,params BlueprintAbilityReference[] refs) {var r=new SpellLevelList(level);Set(r,"m_Spells",refs.ToList());return r;}
 static BlueprintAbilityReference[] Spells(SpellLevelList row)=>((List<BlueprintAbilityReference>)Get(row,"m_Spells")??new List<BlueprintAbilityReference>()).ToArray();
 static BlueprintSpellList List(BlueprintsCache cache,int id,string name,params SpellLevelList[] rows) {
  var list=new BlueprintSpellList {AssetGuid=Id(id),name=name,SpellsByLevel=rows};cache.AddCachedBlueprint(list.AssetGuid,list);return list;
 }
 static SpellLevelList[] Build(SpellLevelList[] rows,int required=0)=>(SpellLevelList[])AccessTools.Method(compatibility,"Build").Invoke(null,new object[]{rows,required});
 static void InvokePostfix()=>AccessTools.Method(postfixType,"Postfix").Invoke(null,null);
 static bool SkipProfiling()=>false;
 static bool LogError(string __0) {logMessages.Add(__0??"");return false;}
 static bool Deserialize(ref SimpleBlueprint bp) {bp=pendingBlueprint;return false;}
 static void ObserveLoad(BlueprintsCache __instance,BlueprintGuid guid) {if(ReferenceEquals(__instance,activeCache)){Interlocked.Increment(ref nativeLoads);if(guid!=pendingBlueprint?.AssetGuid)Interlocked.Increment(ref referenceLoads);}}
 static void ScheduleLoad(BlueprintsCache __instance,ref Action<BlueprintGuid,SimpleBlueprint> action) {
  if(!ReferenceEquals(__instance,activeCache))return;
  if(!schedule)return;
  var original=action;
  action=(id,bp)=>{
   original(id,bp);
   if(Interlocked.Increment(ref callbackCount)!=1)return;
   ScheduleNativeWorker();
  };
 }
 static void ScheduleNativeWorker() {
  callbackHeldNativeLock=Monitor.IsEntered(Get(activeCache,"m_Lock"));
  loaderStart.Set();
  if(!loaderAttempt.WaitOne(5000))throw new Exception("Native loader worker was not scheduled");
  if(callbackHeldNativeLock)workerBlocked=!loaderDone.WaitOne(50);
  else if(!loaderDone.WaitOne(5000))throw new Exception("Unprotected native loader did not complete");
 }
 static bool ActiveValues(object values) {
  if(activeCache==null)return false;
  var entries=Get(activeCache,"m_LoadedBlueprints");
  var field=values.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).FirstOrDefault(f=>f.FieldType==entries.GetType());
  return field!=null&&ReferenceEquals(field.GetValue(values),entries);
 }
 static void ScheduleCopy(object __instance) {
  if(!ActiveValues(__instance))return;
  if(injectSnapshotFailure) {
   snapshotAttempts++;
   if(snapshotMutationsRemaining>0) {snapshotMutationsRemaining--;activeCache.AddCachedBlueprint(Id(600000+(++snapshotInserts)),new BlueprintAbility {name="01422UnlockedExternalAdd"});}
   return;
  }
  if(schedule&&Interlocked.Increment(ref callbackCount)==1)ScheduleNativeWorker();
  if(directCopyWriter) {directCopyCalls++;directStart.Set();if(!directFirstWrite.WaitOne(5000))throw new Exception("Direct cache writer failed to start");}
 }
 static void FinishCopy(object __instance) {if(directCopyWriter&&ActiveValues(__instance))directStop.Set();}
 static void ThrowForOneList(SpellLevelList[] source) {buildCalls++;if(ReferenceEquals(source,failingRows))throw new InvalidOperationException("Injected malformed external list failure");}
 static void ThrowBeforeReconcile() {throw new InvalidOperationException("Injected optional integration initialization failure");}
 static Exception Root(Exception e) {while(e is TargetInvocationException&&e.InnerException!=null)e=e.InnerException;return e;}

 internal static JObject Run(Assembly assembly,bool baseline=false) {
  checks=callbackCount=nativeLoads=referenceLoads=buildCalls=snapshotAttempts=snapshotMutationsRemaining=snapshotInserts=directWrites=directCopyCalls=0;logMessages.Clear();schedule=injectSnapshotFailure=directCopyWriter=false;failingRows=null;
  compatibility=assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellListCompatibility",true);
  postfixType=assembly.GetType("CallOfTheWild.Wrath.Prototype.SpellListCompatibilityAfterLoad",true);
  var oldCache=ResourcesLibrary.BlueprintsCache;
  var managerField=AccessTools.Field(typeof(OwlcatModificationsManager),"s_Instance");var oldManager=managerField.GetValue(null);
  var manager=Empty<OwlcatModificationsManager>();Set(manager,"<AppliedModifications>k__BackingField",Array.Empty<OwlcatModification>());managerField.SetValue(null,manager);
  var h=new Harmony("CotW.Feedback01422.NativeCacheBoundaries");
  h.Patch(AccessTools.Method(typeof(Kingmaker.Utility.ProfileScope),"New",new[]{typeof(string),typeof(UnityEngine.Object)}),prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(SkipProfiling)));
  h.Patch(AccessTools.Method(typeof(Owlcat.Runtime.Core.Logging.LogChannel),"Error",new[]{typeof(string),typeof(object[])}),prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(LogError)));
  h.Patch(AccessTools.Method(typeof(ReflectionBasedSerializer),"Blueprint",new[]{typeof(SimpleBlueprint).MakeByRefType()}),prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(Deserialize)));
  h.Patch(AccessTools.Method(typeof(BlueprintsCache),"ForEachLoaded"),prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(ScheduleLoad)));
  h.Patch(AccessTools.Method(typeof(BlueprintsCache),"Load"),prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(ObserveLoad)));
  try {
   var cache=new BlueprintsCache();activeCache=cache;AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
   // Match the user's ToyBox reported order of magnitude without loading Unity assets.
   for(int i=0;i<256008;i++)cache.AddCachedBlueprint(Id(i),null);
   var a=Ref(400000);var b=Ref(400001);
   var list=List(cache,300000,"01422ConcurrentThassilonian",Row(0),Row(3,a),Row(3,b,a));
   pendingBlueprint=new BlueprintAbility {AssetGuid=Id(300001),name="01422ActualNativeLoad",ComponentsArray=Array.Empty<BlueprintComponent>()};
   var entries=(IDictionary)Get(cache,"m_LoadedBlueprints");var entryType=entries.GetType().GetGenericArguments()[1];var entry=Activator.CreateInstance(entryType);Set(entry,"Offset",1u);entries[pendingBlueprint.AssetGuid]=entry;
   if(!baseline) {
    var map=entries.Values.GetType().GetInterfaceMap(typeof(ICollection));var copy=map.TargetMethods[Array.FindIndex(map.InterfaceMethods,m=>m.Name=="CopyTo")];
    h.Patch(copy,prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(ScheduleCopy)),postfix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(FinishCopy)));
   }
   string file=Path.GetTempFileName();
   using(var pack=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.Read)) {
    pack.SetLength(2);Set(cache,"m_PackFile",pack);Set(cache,"m_PackSerializer",Empty<ReflectionBasedSerializer>());
    loaderStart=new ManualResetEvent(false);loaderAttempt=new ManualResetEvent(false);loaderDone=new ManualResetEvent(false);Exception workerError=null;SimpleBlueprint loaded=null;
    var worker=new Thread(()=>{try{if(!loaderStart.WaitOne(5000))throw new Exception("Snapshot never scheduled loader");loaderAttempt.Set();loaded=cache.Load(pendingBlueprint.AssetGuid);}catch(Exception e){workerError=e;}finally{loaderDone.Set();}}){IsBackground=true};
    schedule=true;callbackHeldNativeLock=workerBlocked=false;worker.Start();Exception failure=null;
    try{InvokePostfix();}catch(Exception e){failure=Root(e);}finally{schedule=false;loaderStart.Set();Check(worker.Join(10000),"native concurrent loader finishes without deadlock");}
    if(workerError!=null)throw new Exception("Native concurrent loader failed",workerError);
    Check(ReferenceEquals(loaded,pendingBlueprint),"unmodified native Load returned deserialized blueprint");
    Check(ReferenceEquals(Get(entries[pendingBlueprint.AssetGuid],"Blueprint"),pendingBlueprint),"native Load wrote actual cache entry");
    Check(nativeLoads==1&&referenceLoads==0,"snapshot/list merge never resolves spell references");
    if(baseline) {
     Check(failure is InvalidOperationException&&failure.StackTrace.Contains("MoveNext")&&failure.StackTrace.Contains("ForEachLoaded"),"0.14.21 production postfix reproduces user's native dictionary failure; actual="+failure);
     Check(!callbackHeldNativeLock,"0.14.21 enumeration did not share native Load lock");
     var nativeFailure=failure;callbackCount=0;loaderStart.Reset();loaderAttempt.Reset();loaderDone.Reset();
     var directOldEntry=entries[pendingBlueprint.AssetGuid];var directOldGuid=pendingBlueprint.AssetGuid;workerError=null;
     worker=new Thread(()=>{try{if(!loaderStart.WaitOne(5000))throw new Exception("Old direct writer was not scheduled");loaderAttempt.Set();entries[directOldGuid]=directOldEntry;}catch(Exception e){workerError=e;}finally{loaderDone.Set();}}){IsBackground=true};
     schedule=true;worker.Start();failure=null;try{InvokePostfix();}catch(Exception e){failure=Root(e);}finally{schedule=false;loaderStart.Set();Check(worker.Join(10000),"old ToyBox-shaped direct dictionary writer finishes");}
     if(workerError!=null)throw workerError;
     Check(failure is InvalidOperationException&&failure.StackTrace.Contains("MoveNext")&&failure.StackTrace.Contains("ForEachLoaded"),"0.14.21 also reproduces exact direct-existing-entry overwrite failure");
     Check(nativeLoads==1,"ToyBox-shaped old reproduction bypasses native Load entirely");
     return new JObject { ["passed"]=true,["assertions"]=checks,["oldFailureReproduced"]=true,["oldDirectToyBoxWriteFailureReproduced"]=true,["nativeCacheEntries"]=entries.Count,["exception"]=nativeFailure.ToString(),["directWriteException"]=failure.ToString() };
    }
    if(failure!=null)throw failure;
    Check(callbackHeldNativeLock&&workerBlocked,"snapshot owns native Load lock; competing native Load waits");
    Check(list.SpellsByLevel.Length==11&&Spells(list.SpellsByLevel[3]).Select(r=>r.Guid).SequenceEqual(new[]{a.Guid,b.Guid}),"postfix merges stable spell GUID union after concurrent native load and retains native slots");
   }
   File.Delete(file);loaderStart.Dispose();loaderAttempt.Dispose();loaderDone.Dispose();
   // Latest ToyBox bypasses m_Lock and assigns loadedBlueprints[guid] directly.
   // Keep that actual dictionary writer active across native ValueCollection.CopyTo.
   var directList=List(cache,300002,"01422ToyBoxDirectWriter",Row(0),Row(6,a),Row(6,b));
   directStart=new ManualResetEvent(false);directFirstWrite=new ManualResetEvent(false);directStop=new ManualResetEvent(false);Exception directError=null;
   var directEntry=entries[pendingBlueprint.AssetGuid];var directGuid=pendingBlueprint.AssetGuid;
   var directThread=new Thread(()=>{try{if(!directStart.WaitOne(5000))throw new Exception("Native CopyTo never started direct writer");while(!directStop.WaitOne(0)){entries[directGuid]=directEntry;Interlocked.Increment(ref directWrites);directFirstWrite.Set();}}catch(Exception e){directError=e;directFirstWrite.Set();}}){IsBackground=true};
   directCopyWriter=true;directThread.Start();try{InvokePostfix();}finally{directCopyWriter=false;directStop.Set();Check(directThread.Join(10000),"direct external writer finishes without deadlock");}
   if(directError!=null)throw directError;
   Check(directWrites>1&&directCopyCalls==1,"repeated unlocked existing-entry writes overlap one actual native ValueCollection.CopyTo");
   Check(directList.SpellsByLevel.Length==11&&Spells(directList.SpellsByLevel[6]).Length==2,"ToyBox-shaped direct dictionary writes do not skip startup reconciliation or truncate native slots");
   directStart.Dispose();directFirstWrite.Dispose();directStop.Dispose();
   var shape=TestShapes(a,b);
   // Catch at both boundaries: one broken list cannot prevent a following list repair,
   // and a permanently uncooperative dictionary writer cannot escape the startup hook.
   cache=new BlueprintsCache();activeCache=cache;AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,cache);
   var broken=List(cache,300010,"01422FaultyExternal",Row(0),Row(2,a),Row(2,b));failingRows=broken.SpellsByLevel;
   var healthy=List(cache,300011,"01422HealthyAfterFault",Row(0),Row(4,a),Row(4,b));Set(healthy,"m_MaxLevel",9);
   h.Patch(AccessTools.Method(compatibility,"Build"),prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(ThrowForOneList)));
   InvokePostfix();Check(ReferenceEquals(broken.SpellsByLevel,failingRows),"failed list remains transactionally unchanged");
   Check(healthy.SpellsByLevel.Length==11&&Spells(healthy.SpellsByLevel[4]).Length==2,"one external list exception does not prevent later list repair");
   Check((int)Get(healthy,"m_MaxLevel")==0,"rewriting rows invalidates native cached maximum level");
   failingRows=null;InvokePostfix();Check(broken.SpellsByLevel.Length==11,"failed list can be repaired on a later call");
   var once=healthy.SpellsByLevel;InvokePostfix();Check(ReferenceEquals(once,healthy.SpellsByLevel),"repeated valid list reconciliation keeps same array");
   var nativeTemplate=List(cache,300012,"01422CanonicalNativeTemplate",Enumerable.Range(0,11).Select(i=>Row(i,i==4?new[]{a}:Array.Empty<BlueprintAbilityReference>())).ToArray());var templateRows=nativeTemplate.SpellsByLevel;
   InvokePostfix();Check(ReferenceEquals(templateRows,nativeTemplate.SpellsByLevel),"untouched legal native eleven-row template is preserved");
   var late=List(cache,300013,"01422AfterTwoUnlockedAdds",Row(0),Row(4,a),Row(4,b));
   injectSnapshotFailure=true;snapshotMutationsRemaining=2;InvokePostfix();injectSnapshotFailure=false;
   Check(snapshotAttempts==3&&late.SpellsByLevel.Length==11,"actual unlocked dictionary growth invalidates two CopyTo arrays; third succeeds");
   snapshotAttempts=0;snapshotMutationsRemaining=3;injectSnapshotFailure=true;InvokePostfix();Check(true,"native CopyTo failure from repeated unlocked Add never escapes startup postfix");injectSnapshotFailure=false;
   Check(snapshotAttempts==3,"uncooperative cache writer retry is bounded to three snapshots");
   Check(buildCalls>=3,"per-list production Build path really executed");
   h.Patch(AccessTools.Method(compatibility,"Reconcile"),prefix:new HarmonyMethod(typeof(Feedback01422StartupTests),nameof(ThrowBeforeReconcile)));
   InvokePostfix();Check(true,"outer startup postfix isolates an unexpected optional integration exception");
   return new JObject {
    ["passed"]=true,["assertions"]=checks,["nativeCacheEntries"]=256011,["actualNativeCopyTo"]=true,["actualNativeLoad"]=true,["workerBlockedByNativeLock"]=workerBlocked,["directDictionaryWrites"]=directWrites,["directWriterSnapshotCalls"]=directCopyCalls,
    ["perListFailureIsolated"]=true,["snapshotFailureIsolated"]=true,["boundedSnapshotAttempts"]=snapshotAttempts,["actualUnlockedAddRetry"]=true,["shapeContracts"]=shape,
    ["boundaries"]="Actual game BlueprintsCache/Dictionary/ValueCollection.CopyTo/Load/BlueprintSpellList and production after-LoadPackTOC postfix. Timing boundaries retain native bodies; concurrent native Load obeys m_Lock, a second ToyBox-shaped worker directly overwrites dictionary entries without it. Unlocked growth exercises real native CopyTo bounds errors. Pack serializer supplies a fixture blueprint; native Load still seeks, locks, enables and writes. Profiling and Unity log boundaries bypassed; no game/UMM startup or complete external mod assembly execution."
   };
  } finally {schedule=injectSnapshotFailure=directCopyWriter=false;h.UnpatchAll(h.Id);AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache").SetValue(null,oldCache);managerField.SetValue(null,oldManager);activeCache=null;pendingBlueprint=null;failingRows=null;}
 }

 static JObject TestShapes(BlueprintAbilityReference a,BlueprintAbilityReference b) {
  int before=checks;
  foreach(int max in new[]{4,6,9}) {
   var rows=Enumerable.Range(0,11).Select(i=>Row(i,i==max?new[]{a}:Array.Empty<BlueprintAbilityReference>())).Concat(new[]{Row(max,b,a),Row(max,a)}).ToArray();
   var result=Build(rows);Check(result.Length==rows.Length,"low-caster or ordinary list retains native and external reserved array slots above "+max);
   Check(result.Select((r,i)=>r.SpellLevel==i).All(x=>x),"appended source indices 11/12 become legal numeric rows for "+max);
   Check(Spells(result[max]).Select(r=>r.Guid).SequenceEqual(new[]{a.Guid,b.Guid}),"all appended external spell identities retained once for "+max);
   Check(Spells(rows[max]).Length==1&&rows.Length==13,"donor rows and array unmodified for "+max);
   var nativeList=new BlueprintSpellList {AssetGuid=Id(450000+max),name="01422NativeValidation"+max,ComponentsArray=Array.Empty<BlueprintComponent>(),SpellsByLevel=rows};
   using(var validation=new ValidationContext(nativeList.name)) {
    nativeList.Validate(validation,-1);
    var errors=validation.Errors.Where(e=>e.Contains("Spell list has wrong level value")).ToArray();
    Check(errors.Any(e=>e.Contains("index 11"))&&errors.Any(e=>e.Contains("index 12")),"native validator reproduces supplied logs indices 11/12 for "+max);
    validation.Clear();nativeList.SpellsByLevel=result;nativeList.Validate(validation,-1);
    Check(!validation.Errors.Any(e=>e.Contains("Spell list has wrong level value")),"actual native validator accepts repaired numeric levels for "+max);
   }
  }
  var sparse=Build(new[]{Row(0),Row(2,a)},4);Check(sparse.Length==11&&sparse[4].SpellLevel==4,"explicit insertion retains the native 0..10 index contract");
  var mythic=Build(new[]{Row(0),Row(10,a)});Check(mythic.Length==11&&Spells(mythic[10]).Single().Guid==a.Guid,"occupied native mythic level ten preserved");
  var reserved=Build(new[]{Row(0),Row(2,a),Row(11),Row(12)});Check(reserved!=null&&reserved.Length>=11&&reserved.Select((r,i)=>r.SpellLevel==i).All(x=>x),"empty out-of-order reserved rows do not grant unsupported spell circles");
  Check(Build(new[]{Row(0),Row(2,a),Row(11,b)})==null,"nonempty unsupported external level rejected without destructive clamping");
  Check(Build(new[]{Row(-1,a)})==null,"negative nonempty external level rejected");
  Check(Build(new[]{Row(0),Row(4,a)})[4]!=Build(new[]{Row(0),Row(4,a)})[4],"each output owns independent row objects");
  var nulls=Build(new[]{(SpellLevelList)null,Row(0),Row(2,a,null,a)});Check(nulls.Length==11&&Spells(nulls[2]).Length==1,"null rows/references and duplicate identities normalized without losing native slots");
  return new JObject { ["assertions"]=checks-before,["lowCasterMaxima"]=new JArray(4,6),["nativeMythicTenRetained"]=true,["indices11And12Normalized"]=true,["actualNativeValidation"]=true };
 }
}
