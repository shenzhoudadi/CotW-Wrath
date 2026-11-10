using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CallOfTheWild.Wrath.Prototype;

internal static partial class Feedback01449Tests
{
    static string W50=>Active01453||Main.GetType("CallOfTheWild.Wrath.Prototype.MagusBatch01454")!=null?"work/psychic01458/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineProvider01453")!=null?"work/repair01453/":(Environment.GetEnvironmentVariable("COTW_01452_CASE")!=null||Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452")!=null)?"work/repair01452/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineSlots01451")!=null?"work/repair01451/":"work/repair01450/";
    static readonly Dictionary<string,string> externalEnglish01450=new Dictionary<string,string>();
    static bool ExternalString01450(string __0,ref LocalizedString __result){if(!externalEnglish01450.ContainsKey(__0))throw new Exception("Missing actual provider locale key "+__0);__result=new LocalizedString();Set(__result,"m_Key",__0);return false;}
    static readonly HashSet<BlueprintGuid> warmedComponents01450=new HashSet<BlueprintGuid>();
    static void WarmComponents01450(SimpleBlueprint __1){if(__1 is BlueprintScriptableObject b&&AccessTools.Field(typeof(BlueprintScriptableObject),"Components").GetValue(b)==null){var empty=b.ComponentsArray;if(empty.Length!=0)throw new Exception("Native lazy component default changed");warmedComponents01450.Add(b.AssetGuid);}}
    static bool ProviderSprite01450(ref UnityEngine.Sprite __result){__result=(UnityEngine.Sprite)FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));return false;}
    static bool DefaultValue01450(string key){if(Active01453&&key=="MysticalMayhem")return false;if(key!="FireShield"&&key!="PhoenixBloodline"&&key!="TabletopTweaks-Base")throw new Exception("Unreviewed config/presence endpoint "+key);return true;}
    static IEnumerable<CodeInstruction> ConfigIO01450(IEnumerable<CodeInstruction> source,MethodBase __originalMethod){
        // Only these already-controlled mod presence/settings IO endpoints;
        // no rule builder or selection predicate is replaced.
        if(__originalMethod.Name=="IsTTTBaseEnabled")return new[]{new CodeInstruction(OpCodes.Ldc_I4_1),new CodeInstruction(OpCodes.Ret)};
        if(__originalMethod.Name!="IsEnabled"&&__originalMethod.Name!="GetModActive")throw new Exception("Unexpected config endpoint");
        return new[]{new CodeInstruction(OpCodes.Ldarg_0),new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Feedback01449Tests),nameof(DefaultValue01450))),new CodeInstruction(OpCodes.Ret)};
    }
    static bool BPCoreError01450(string __0,Exception __1){throw new InvalidOperationException("Actual BlueprintCore provider error: "+__0,__1);}
    static Exception ProviderException01450(Exception __exception){if(__exception!=null)Console.Error.WriteLine("01450 provider inner failure before external finally: "+__exception);return __exception;}
    static bool ProviderError01450(string __0){throw new Exception("Actual external provider registration error: "+__0);}
    static int sortLabelReads01450;
    static JObject nativeEnglish01450;
    static readonly List<object> providerPacks01450=new List<object>();
    static string English01450(LocalizedString value)
    {
        sortLabelReads01450++;string key=value?.Key;if(string.IsNullOrEmpty(key))return "";
        var current=LocalizationManager.CurrentPack;
        var strings=current==null?null:AccessTools.Field(typeof(LocalizationPack),"m_Strings").GetValue(current) as System.Collections.IDictionary;
        if(strings?.Contains(key)==true){var item=strings[key];return (string)AccessTools.Field(item.GetType(),"Text").GetValue(item);}
        foreach(var pack in providerPacks01450){var ids=(System.Collections.IDictionary)AccessTools.Property(pack.GetType(),"Ids").GetValue(pack,null);if(ids.Contains(key))return (string)AccessTools.Field(ids[key].GetType(),"enGB").GetValue(ids[key]);}
        return externalEnglish01450.TryGetValue(key,out string text)?text:(string)nativeEnglish01450?[key]??key;
    }
    static string BlueprintName01450(Kingmaker.Blueprints.Facts.BlueprintUnitFact bp)=>bp==null?null:English01450((LocalizedString)AccessTools.Field(bp.GetType(),"m_DisplayName").GetValue(bp));
    static string BlueprintDescription01450(Kingmaker.Blueprints.Facts.BlueprintUnitFact bp)=>bp==null?null:English01450((LocalizedString)AccessTools.Field(bp.GetType(),"m_Description").GetValue(bp));
    static string LabelEndpoint01450(MethodInfo m)
    {
        if(m.DeclaringType==typeof(LocalizedString)&&m.ReturnType==typeof(string)&&(m.Name=="op_Implicit"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(LocalizedString)||(m.Name==nameof(ToString)||m.Name=="get_Text")&&m.GetParameters().Length==0))return nameof(English01450);
        if(typeof(Kingmaker.Blueprints.Facts.BlueprintUnitFact).IsAssignableFrom(m.DeclaringType)&&m.GetParameters().Length==0){if(m.Name=="get_Name")return nameof(BlueprintName01450);if(m.Name=="get_Description")return nameof(BlueprintDescription01450);}
        return null;
    }
    static readonly List<string> listContexts01450=new List<string>();
    static object ExactList01450(int capacity,string assemblyName,string referenceName){
        var owner=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.FullName==assemblyName);
        var reference=Type.GetType(referenceName,true);var element=owner.GetType("BlueprintCore.Utils.Blueprint`1",true).MakeGenericType(reference);
        var type=typeof(List<>).MakeGenericType(element);var result=Activator.CreateInstance(type,new object[]{capacity});
        var array=(Array)AccessTools.Field(type,"_items").GetValue(result);
        if(result.GetType()!=type||array.GetType().GetElementType()!=element)throw new Exception("Exact provider collection context changed");
        listContexts01450.Add(assemblyName+"/"+referenceName+"/capacity="+capacity);return result;
    }
    static void ListAdd01450(object list,object value){
        var expected=list.GetType().GetGenericArguments().Single();if(value!=null&&!expected.IsInstanceOfType(value))throw new Exception("Provider element identity differs: "+expected.AssemblyQualifiedName+" / "+value.GetType().AssemblyQualifiedName);
        ((System.Collections.IList)list).Add(value);
    }
    static IEnumerable<CodeInstruction> ProviderIL01450(IEnumerable<CodeInstruction> source,MethodBase __originalMethod)
    {
        foreach(var instruction in source){
            if(instruction.operand is ConstructorInfo ctor&&ctor.DeclaringType.IsGenericType&&ctor.DeclaringType.GetGenericTypeDefinition()==typeof(List<>)){
                var argument=ctor.DeclaringType.GetGenericArguments()[0];
                if(argument.IsGenericType&&argument.GetGenericTypeDefinition().FullName=="BlueprintCore.Utils.Blueprint`1"){
                    // Standalone emitter aliases the two mods' embedded generic
                    // collection type. Invoke the same capacity constructor on
                    // the exact intended runtime Type; rule builders are intact.
                    if(ctor.GetParameters().Length!=1||ctor.GetParameters()[0].ParameterType!=typeof(int))throw new Exception("Unreviewed provider collection constructor");
                    var first=new CodeInstruction(OpCodes.Ldstr,__originalMethod.DeclaringType.Assembly.FullName);first.labels.AddRange(instruction.labels);first.blocks.AddRange(instruction.blocks);yield return first;
                    yield return new CodeInstruction(OpCodes.Ldstr,argument.GetGenericArguments().Single().AssemblyQualifiedName);
                    yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Feedback01449Tests),nameof(ExactList01450)));continue;
                }
            }
            if(instruction.operand is MethodInfo add&&add.Name=="Add"&&add.DeclaringType.IsGenericType&&add.DeclaringType.GetGenericTypeDefinition()==typeof(List<>)&&add.DeclaringType.GetGenericArguments()[0].IsGenericType&&add.DeclaringType.GetGenericArguments()[0].GetGenericTypeDefinition().FullName=="BlueprintCore.Utils.Blueprint`1"&&add.DeclaringType.GetGenericArguments()[0].Assembly.GetName().Name==__originalMethod.DeclaringType.Assembly.GetName().Name){
                instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(Feedback01449Tests),nameof(ListAdd01450));
            }else if(instruction.operand is MethodInfo method){var endpoint=LabelEndpoint01450(method);if(endpoint!=null){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(Feedback01449Tests),endpoint);}}
            yield return instruction;
        }
    }
    static void CorePermissions01450(Assembly core,Harmony h)
    {
        var roots=new[]{"TabletopTweaks.Core.Utilities.LocalizationExtensions","TabletopTweaks.Core.Utilities.BloodlineTools","TabletopTweaks.Core.Utilities.Helpers","TabletopTweaks.Core.Utilities.BlueprintTools","TabletopTweaks.Core.Localization.MultiLocalizationPack","TabletopTweaks.Core.Utilities.BlueprintExtentions"};
        Permissions01450(core,h,roots);
    }
    static void Permissions01450(Assembly core,Harmony h,string[] roots)
    {
        var rows=new JArray();
        IEnumerable<Type> Nested(Type t){yield return t;foreach(var child in t.GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic))foreach(var item in Nested(child))yield return item;}
        foreach(var type in roots.Select(r=>core.GetType(r,true)).SelectMany(Nested).Where(t=>!t.ContainsGenericParameters).OrderByDescending(t=>t.FullName.Count(x=>x=='+')).ThenByDescending(t=>Array.FindIndex(roots,r=>t.FullName==r||t.FullName.StartsWith(r+"+"))))
            foreach(var method in type.GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>!m.ContainsGenericParameters&&m.GetMethodBody()!=null)) {
                var il=PatchProcessor.GetOriginalInstructions(method).ToArray();
                if(!il.Any(i=>i.operand is ConstructorInfo ctor&&ctor.DeclaringType.IsGenericType&&ctor.DeclaringType.GetGenericTypeDefinition()==typeof(List<>)&&ctor.DeclaringType.GetGenericArguments()[0].IsGenericType&&ctor.DeclaringType.GetGenericArguments()[0].GetGenericTypeDefinition().FullName=="BlueprintCore.Utils.Blueprint`1"||i.operand is Type hidden&&hidden.Assembly==typeof(BlueprintsCache).Assembly&&!hidden.IsVisible||i.operand is MethodInfo signature&&signature.GetParameters().Any(p=>p.ParameterType.Assembly==typeof(BlueprintsCache).Assembly&&!p.ParameterType.IsVisible)||i.operand is FieldInfo f&&(f.DeclaringType.Assembly==typeof(BlueprintsCache).Assembly||f.DeclaringType.Assembly==typeof(LocalizedString).Assembly)&&(!f.IsPublic||!f.DeclaringType.IsVisible||!f.FieldType.IsVisible)||i.operand is MethodInfo bridge&&(LabelEndpoint01450(bridge)!=null||bridge.DeclaringType==typeof(NativeFieldBridge01449)&&bridge.IsGenericMethod&&bridge.GetGenericArguments().Any(t=>!t.IsVisible))))continue;
                // Mono-equivalent visibility, with only the explicitly recorded
                // localized engine reads redirected to actual English data.
                // Every rule branch and field remains intact; localized reads and exact same-type collection constructor/Add calls are explicit endpoints.
                h.Patch(method,transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ProviderIL01450)));
                rows.Add(new JObject{["method"]=method.ToString(),["type"]=type.FullName,["localizedEngineReadEndpoints"]=il.Count(i=>i.operand is MethodInfo m&&LabelEndpoint01450(m)!=null),["originalInstructions"]=new JArray(il.Select(i=>i.ToString()))});
            }
        File.WriteAllText(Path.Combine(Root(),W50+core.GetName().Name+"-permissions-"+System.Diagnostics.Process.GetCurrentProcess().Id+".json"),rows.ToString());
    }
    static object Context01450(Assembly assembly,string field,string configDirectory,Assembly core,Harmony h)
    {
        var slot=AccessTools.Field(assembly.GetType(assembly.GetName().Name=="TabletopTweaks-Base"?"TabletopTweaks.Base.Main":assembly.GetName().Name=="TabletopTweaks-Core"?"TabletopTweaks.Core.Main":"TomeOfTheFirebird.Main",true),field);
        var context=FormatterServices.GetUninitializedObject(slot.FieldType);slot.SetValue(null,context);
        var logger=AccessTools.Field(context.GetType(),"Logger");logger.SetValue(context,FormatterServices.GetUninitializedObject(logger.FieldType));
        foreach(var m in logger.FieldType.GetMethods(BindingFlags.Public|BindingFlags.Instance).Where(m=>m.ReturnType==typeof(void)&&m.Name.StartsWith("Log")))h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01449Tests),m.Name=="LogError"?nameof(ProviderError01450):nameof(Skip)));
        var blueprints=AccessTools.Field(context.GetType(),"Blueprints");blueprints.SetValue(context,JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(configDirectory,"Blueprints.json")),blueprints.FieldType));
        var config=blueprints.GetValue(context);AccessTools.Field(config.GetType(),"Context").SetValue(config,context);
        var pack=AccessTools.Field(context.GetType(),"ModLocalizationPack");pack.SetValue(context,Activator.CreateInstance(pack.FieldType));AccessTools.Field(pack.FieldType,"Context").SetValue(pack.GetValue(context),context);providerPacks01450.Add(pack.GetValue(context));
        var added=AccessTools.Field(context.GetType(),"AddedContent");if(added!=null)added.SetValue(context,JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(configDirectory,"AddedContent.json")),added.FieldType));
        return context;
    }
    static void Native01450(BlueprintsCache c)
    {
        var native=JObject.Parse(File.ReadAllText(Path.Combine(Root(),"work/prototype-build/01450-native.json")));
        var types=typeof(BlueprintsCache).Assembly.GetTypes().ToLookup(t=>t.Name);
        foreach(var row in native["rows"]){var bp=row["blueprint"];var guid=BlueprintGuid.Parse((string)bp["AssetId"]);
            if(c.Load(guid) is BlueprintScriptableObject existing){if(AccessTools.Field(typeof(BlueprintScriptableObject),"Components").GetValue(existing)==null){existing.ComponentsArray=((JArray)bp["Data"]["Components"]??new JArray()).Select(x=>(BlueprintComponent)PsychicExpansionTests.Read(x,typeof(BlueprintComponent))).ToArray();existing.OnEnable();}continue;}
            // These two unused Sacred Weapon catalogue descendants contain a
            // native archive-only SawtoothSaber enum absent from this game's
            // managed enum. The established Sunsinger contract treats that
            // catalogue as an engine endpoint. They are not bloodline powers;
            // leave them absent so an accidental builder dependency still fails.
            if(guid.ToString()=="45e76b1480d14d959d780f1182a6cabf"||guid.ToString()=="c37574188b0242d2ac04a454a0430dd5")continue;
            string kind=((string)bp["Data"]["$type"]).Split(',').Last().Trim();var type=types[kind].Single();
            BlueprintScriptableObject value;try{value=(BlueprintScriptableObject)PsychicExpansionTests.Read(bp["Data"],type);}catch(Exception error){throw new InvalidOperationException("Exact native dependency failed: "+row["name"]+"/"+guid,error);}value.name=(string)row["name"];value.AssetGuid=guid;
            value.ComponentsArray=((JArray)bp["Data"]["Components"]??new JArray()).Select(x=>(BlueprintComponent)PsychicExpansionTests.Read(x,typeof(BlueprintComponent))).ToArray();
            var icon=AccessTools.Field(value.GetType(),"m_Icon");if(icon?.FieldType==typeof(UnityEngine.Sprite))icon.SetValue(value,FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite)));
            value.OnEnable();c.AddCachedBlueprint(guid,value);
        }
        // Prerequisite/racial fact producers are outside these bloodline builders.
        // These exact external identities are inert controlled dependencies;
        // no bloodline progression, exchange power or mutation is substituted.
        foreach(string id in new[]{"e2cfd3cedf7c40088b25aa82d6db3c77","bba86db1b3ef4ab5a412a5910f2b8947","4ea922baf88e4b8ea37850c3d0e2da2e"})if(c.Load(BlueprintGuid.Parse(id))==null){var f=new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(id),name="ExternalPrerequisite01450"+id,ComponentsArray=Array.Empty<BlueprintComponent>()};f.OnEnable();c.AddCachedBlueprint(f.AssetGuid,f);}
    }
    static int externalSelects01450,externalGrowth01450;
    static void ExternalChoices01450(BlueprintsCache c,Harmony h,string[][] rows,BlueprintFeatureSelection familiar)
    {
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Prerequisites.IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(No)));
        externalSelects01450=externalGrowth01450=0;
        var growth=JObject.Parse(File.ReadAllText(Path.Combine(Root(),W50+"external-anchor-audit.json")))["growth"].Select(r=>r.Select(x=>(string)x).ToArray()).ToArray();
        foreach(var row in rows){
            bool br=row[1]=="Bloodrager";var p=N<BlueprintProgression>(c,row[0]);var cls=N<BlueprintCharacterClass>(c,br?"d77e67a814d686842802c9cfd8ef8499":"b3a505fb61437dc4097f43c3f8f9a4cf");
            bool seeker=!br&&Array.IndexOf(rows,row)%2==1;
            for(int slot=0;slot<3;slot++){
                if(string.IsNullOrEmpty(row[slot+2])){Check(!p.LevelEntries.Where(e=>e.Level==3).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Any(x=>x.AllFeatures.Contains(familiar)),"Seeker does not gain an invented third-level power slot");continue;}
                int level=br?new[]{1,4,8}[slot]:new[]{1,3,9}[slot];
                var choices=p.LevelEntries.Where(e=>e.Level==level).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Where(x=>x.AllFeatures.Contains(A<BlueprintFeature>(c,"BloodIntensity01446"))).ToArray();
                Check(choices.Length==1,"exact external power exchange slot "+p.name+"/"+level);var choice=choices.Single();
                var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),level);if(seeker)u.Progression.GetClassData(cls).Archetypes.Add(N<BlueprintArchetype>(c,"7229db6fc0b07af4180e783eed43c4d9"));
                var state=(Kingmaker.UnitLogic.Class.LevelUp.LevelUpState)Call(typeof(Feat01424Tests),"State",u,level);var pending=state.AddSelection(null,default(Kingmaker.UnitLogic.FeatureSource),choice,level);
                foreach(string token in new[]{"BloodIntensity01446","BloodPiercing01446"}){bool allowed=level>=(token.Contains("Intensity")?(br?8:3):(br?4:9));Check(choice.CanSelect(u.Descriptor,state,pending,A<BlueprintFeature>(c,token))==allowed,"actual native external leaf threshold "+p.name+"/"+level+"/"+token);externalSelects01450++;}
                Check(choice.AllFeatures.Contains(familiar)==(slot==0),"Familiar belongs only in initial-power exchange "+p.name+"/"+level);
                if(slot==0){Check(choice.CanSelect(u.Descriptor,state,pending,familiar),"actual external native CanSelect Familiar "+p.name);externalSelects01450++;}
                foreach(var g in growth.Where(g=>g[0]==row[0]&&g[1]==row[slot+2])){
                    var branch=choice.AllFeatures.OfType<BlueprintProgression>().Single(x=>x.LevelEntries.Any(e=>e.Features.Any(f=>f.AssetGuid.ToString()==g[1])));
                    int tier=int.Parse(g[2]);Check(new Kingmaker.UnitLogic.ProgressionData(branch).GetLevelEntry(tier).Features.Count(x=>x.AssetGuid.ToString()==g[3])==1,"original external branch retains exact growth "+p.name+"/"+tier);
                    Check(!new Kingmaker.UnitLogic.ProgressionData(p).GetLevelEntry(tier).Features.Any(x=>x.AssetGuid.ToString()==g[3]),"replaced external power forfeits only its growth "+p.name+"/"+tier);externalGrowth01450++;
                }
            }
            // Use the actual provider's registered parent, never a fabricated pool.
            var parents=((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintFeatureSelection>().Where(x=>x.AllFeatures.Contains(p)).ToArray();Check(parents.Length>0,"external builder publishes a real parent menu "+p.name);
            var owner=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),1);if(seeker)owner.Progression.GetClassData(cls).Archetypes.Add(N<BlueprintArchetype>(c,"7229db6fc0b07af4180e783eed43c4d9"));
            var parentState=(Kingmaker.UnitLogic.Class.LevelUp.LevelUpState)Call(typeof(Feat01424Tests),"State",owner,1);
            var eligible=parents.Select(parent=>new{Parent=parent,Pending=parentState.AddSelection(null,default(Kingmaker.UnitLogic.FeatureSource),parent,1)}).Any(x=>x.Parent.CanSelect(owner.Descriptor,parentState,x.Pending,p));
            Check(eligible,"at least one actual external parent admits the matching class/archetype "+p.name);externalSelects01450++;
        }
        Check(externalGrowth01450==growth.Length,"all12actual external continuation rows checked");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static bool OriginalECMQuery01450(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility spell,UnitEntityData owner){throw new Exception("Actual original ECM query reverse-copy was not installed");}
    static int externalQualifications01450;
    static void ExternalQualification01450(BlueprintsCache c,Harmony harmony,Assembly ecm,string[][] rows)
    {
        externalQualifications01450=0;var helper=ecm.GetType("EbonsContentMod.Utilities.ComponentHelperators",true);
        harmony.CreateReversePatcher(AccessTools.Method(helper,"GetIsBloodlineSpell",new[]{typeof(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility),typeof(UnitEntityData)}),new HarmonyMethod(typeof(Feedback01449Tests),nameof(OriginalECMQuery01450))).Patch();
        foreach(var row in rows){
            var p=N<BlueprintProgression>(c,row[0]);var cls=N<BlueprintCharacterClass>(c,row[1]=="Bloodrager"?"d77e67a814d686842802c9cfd8ef8499":"b3a505fb61437dc4097f43c3f8f9a4cf");var owner=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);H("Book",owner,cls);
            var grants=p.LevelEntries.SelectMany(e=>e.Features).SelectMany(f=>f.GetComponents<Kingmaker.UnitLogic.FactLogic.AddKnownSpell>().Where(k=>k.Spell!=null).Select(k=>new{Feature=(BlueprintFeature)f,Spell=k.Spell})).ToArray();Check(grants.Length>0,"actual external progression has real spell grants "+p.name);
            var first=grants[0];Check(!(bool)Call(helper,"GetIsBloodlineSpell",first.Spell,owner),"without an external progression or grant the extension does not qualify "+p.name);
            H("Fact",owner,p);bool original=OriginalECMQuery01450(first.Spell,owner);Check((bool)Call(helper,"GetIsBloodlineSpell",first.Spell,owner)==original,"ungranted extension never forges qualification and preserves ECM original owned-progression cases "+p.name);
            H("Fact",owner,first.Feature);Check((bool)Call(helper,"GetIsBloodlineSpell",first.Spell,owner),"actual ECM query recognizes external owned progression and actual granted spell "+p.name);
            var unrelated=((SimpleBlueprint[])H("Loaded",c)).OfType<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>().First(x=>x.IsSpell&&!grants.Any(g=>g.Spell==x||x.Parent==g.Spell));Check(!(bool)Call(helper,"GetIsBloodlineSpell",unrelated,owner),"external progression cannot qualify an unrelated spell "+p.name);externalQualifications01450++;
        }
        Check(externalQualifications01450==10,"all10external ECM qualification paths checked");
    }
    internal static int External01450(BlueprintsCache parent,Harmony h)
    {
        int before=checks;nativeEnglish01450=(JObject)JObject.Parse(File.ReadAllText(TestPaths.Game("Wrath_Data/StreamingAssets/Localization/enGB.json")))["strings"];
        Assembly.LoadFrom(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".nuget/packages/dnlib/3.1.0/lib/net461/dnlib.dll"));
        Assembly.LoadFrom(Path.Combine(Root(),"work/modmenu-release/ModMenu.dll"));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),postfix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(WarmComponents01450)));
        bool expanded51=Environment.GetEnvironmentVariable("COTW_01451_CASE")?.StartsWith("external")==true;
        bool assemblyLate51=expanded51&&Environment.GetEnvironmentVariable("COTW_01451_CASE").Contains("assembly");
        bool tttLast51=expanded51&&Environment.GetEnvironmentVariable("COTW_01451_CASE").Contains("ttt-last");
        var c=(BlueprintsCache)H("Fresh",parent);Native01450(c);if(expanded51)NativePowers01451(c);TomeOverhaul(c,h);
        // Only its identity is read by Phoenix's separate bonus-feat filter;
        // this fixture does not model or substitute Metamagic Rager mechanics.
        var metamagicId=BlueprintGuid.Parse("e4142305e74f438c90b9fe9a545a41af");if(c.Load(metamagicId)==null){var marker=new BlueprintArchetype{AssetGuid=metamagicId,name="ExternalMetamagicRagerIdentity01450",ComponentsArray=Array.Empty<BlueprintComponent>()};marker.OnEnable();c.AddCachedBlueprint(metamagicId,marker);}

        var core=Assembly.LoadFrom(Path.Combine(Root(),W50+"runtime-adapter/TabletopTweaks-Core.dll"));
        CorePermissions01450(core,h);
        var sortEndpoints=core.GetType("TabletopTweaks.Core.Utilities.BlueprintExtentions+<>c",true).GetMethods(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Where(m=>m.Name.StartsWith("<AddFeatures>")&&m.ReturnType==typeof(string)).ToArray();Check(sortEndpoints.Length==2,"actual Core has the two audited sorting label endpoints");
        if(assemblyLate51)Call(T("BloodlineMutations01446"),"Register",c);
        var ttt=Assembly.LoadFrom(Path.Combine(Root(),W50+"runtime-adapter/TabletopTweaks-Base.dll"));
        if(assemblyLate51){var config=JObject.Parse(File.ReadAllText(Path.Combine(Root(),"work/batch01447/sources/Vek17__TabletopTweaks-Base/tree/TabletopTweaks-Base/Config/Blueprints.json")));foreach(string key in new[]{"AberrantBloodlineRequisiteFeature","DestinedBloodlineRequisiteFeature"})Check(c.Load(BlueprintGuid.Parse((string)config["NewBlueprints"][key]))==null,"late TTT assembly hook never executes its eager bloodline cctor: "+key);}
        var ecm=Assembly.LoadFrom(Path.Combine(Root(),W50+"runtime-adapter/EbonsContentMod.dll"));
        foreach(var method in ecm.GetType("BlueprintCore.Utils.LogWrapper",true).GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Where(m=>m.ReturnType==typeof(void)&&m.GetParameters().Length>0&&m.GetParameters()[0].ParameterType==typeof(string)))h.Patch(method,prefix:new HarmonyMethod(typeof(Feedback01449Tests),method.Name=="Error"?nameof(BPCoreError01450):nameof(TomeLog)));

        var modBlueprints=(System.Collections.IDictionary)AccessTools.Field(core.GetType("TabletopTweaks.Core.Utilities.BlueprintTools",true),"ModBlueprints").GetValue(null);
        foreach(string id in new[]{"e2cfd3cedf7c40088b25aa82d6db3c77","bba86db1b3ef4ab5a412a5910f2b8947","4ea922baf88e4b8ea37850c3d0e2da2e"})modBlueprints[BlueprintGuid.Parse(id)]=c.Load(BlueprintGuid.Parse(id));
        Check(ecm.Location.Replace('\\','/').Contains(W50.Replace("work/","")+"runtime-adapter"),"this case loads the audited ECM same-member CLR adapter");
        Check(ttt.Location.Replace('\\','/').Contains(W50.Replace("work/","")+"runtime-adapter"),"this case loads the audited TTT same-member CLR adapter");
        Context01450(core,"TTTContext",Path.Combine(Root(),"work/batch01447/sources/Vek17__TabletopTweaks-Core/tree/TabletopTweaks-Core/Config"),core,h);
        Context01450(ttt,"TTTContext",Path.Combine(Root(),"work/batch01447/sources/Vek17__TabletopTweaks-Base/tree/TabletopTweaks-Base/Config"),core,h);
        Context01450(tome,"TotFContext",Path.Combine(Root(),"work/repair01449/sources/pheonix99__TomeOfTheFirebird/tree/TomeOfTheFirebird/Config"),core,h);
        // Install IO endpoints before re-emitting callers, so CLR cannot inline their original UMM lookup.
        h.Patch(AccessTools.Method(tome.GetType("TomeOfTheFirebird.Settings",true),"IsEnabled",new[]{typeof(string)}),transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ConfigIO01450)));
        h.Patch(AccessTools.Method(tome.GetType("TomeOfTheFirebird.Settings",true),"IsTTTBaseEnabled"),transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ConfigIO01450)));
        h.Patch(AccessTools.Method(ecm.GetType("EbonsContentMod.Utilities.CheckerUtilities",true),"GetModActive"),transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ConfigIO01450)));
        Permissions01450(ttt,h,new[]{"TabletopTweaks.Base.NewContent.Bloodlines.AberrantBloodline","TabletopTweaks.Base.NewContent.Bloodlines.DestinedBloodline"});
        Permissions01450(ecm,h,new[]{"EbonsContentMod.Bloodlines.OrcSorcererBloodline","EbonsContentMod.Bloodlines.ProteanSorcererBloodline","EbonsContentMod.Bloodlines.ConfigureBloodlines"});
        Permissions01450(tome,h,new[]{"TomeOfTheFirebird.New_Content.Bloodlines.PheonixBloodline","TomeOfTheFirebird.Helpers.MakerTools","TomeOfTheFirebird.New_Content.Bloodlines.TotFBloodlineTools","BlueprintCore.Actions.Builder.ContextEx.ActionsBuilderContextEx"});
        // Fire Shield is an external spell producer, outside the bloodline test.
        // Preserve its exact identity as an inert prerequisite leaf, not a power.
        foreach(var file in Directory.GetFiles(Path.Combine(Root(),"work/repair01449/sources/pheonix99__TomeOfTheFirebird/tree/TomeOfTheFirebird/Localization"),"*.json"))foreach(var row in (JToken.Parse(File.ReadAllText(file)) as JArray??new JArray()))externalEnglish01450[(string)row["Key"]]=(string)row["enGB"]??"";
        h.Patch(AccessTools.Method(tome.GetType("BlueprintCore.Utils.LocalizationTool",true),"GetString",new[]{typeof(string)}),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ExternalString01450)));
        var totfIds=((JObject)tomeGuids["NewBlueprints"]).Properties().ToDictionary(x=>x.Name,x=>(string)x.Value);
        AccessTools.Method(tome.GetType("BlueprintCore.Utils.BlueprintTool",true),"AddGuidsByName",new[]{typeof(Dictionary<string,string>)}).Invoke(null,new object[]{new Dictionary<string,string>{{"FireShield",new Guid(totfIds["FireShield"]).ToString("N")}}});
        var shieldId=BlueprintGuid.Parse(totfIds["FireShield"]);
        if(c.Load(shieldId)==null){var shield=new Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility{AssetGuid=shieldId,name="FireShield",ComponentsArray=Array.Empty<BlueprintComponent>()};shield.OnEnable();c.AddCachedBlueprint(shieldId,shield);}

        foreach(var open in new[]{0,1}.Select(i=>tome.GetType("BlueprintCore.Blueprints.Configurators.UnitLogic.Buffs.BaseBuffConfigurator`2+<>c__DisplayClass6_"+i,true))){var closed=open.MakeGenericType(typeof(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff),tome.GetType("BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs.BuffConfigurator",true));foreach(var method in closed.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.Name.Contains("b__")))h.Patch(method,transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(SameIL)));}
        foreach(var method in core.GetType("TabletopTweaks.Core.Utilities.AssetLoader",true).GetMethods().Where(m=>m.Name=="LoadInternal"&&m.ReturnType==typeof(UnityEngine.Sprite)))h.Patch(method,prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ProviderSprite01450)));
        var log=AccessTools.Field(ecm.GetType("EbonsContentMod.Main",true),"log");log.SetValue(null,FormatterServices.GetUninitializedObject(log.FieldType));
        foreach(var m in log.FieldType.GetMethods().Where(m=>m.Name=="Log"&&m.ReturnType==typeof(void)))h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(Skip)));
        ExternalSpriteBoundary(ecm,h);
        bool fault=Environment.GetEnvironmentVariable("COTW_01450_CASE").Contains("fault");bool late=fault||Environment.GetEnvironmentVariable("COTW_01450_CASE").Contains("late");
        if(late&&!assemblyLate51)Call(T("BloodlineMutations01446"),"Register",c);
        BlueprintFeature collision51=null;var collisionId51=Id("BloodlinePowerChoiceBloodrager0"+"9ea4c84279a540a986d60677358ee223"+"540390e4735646019137625a37910df0"+"01446");
        if(expanded51&&Environment.GetEnvironmentVariable("COTW_01451_CASE").Contains("fault")){collision51=new BlueprintFeature{AssetGuid=collisionId51,name="ForeignTTTBloodragerCollision01451",ComponentsArray=Array.Empty<BlueprintComponent>()};collision51.OnEnable();c.AddCachedBlueprint(collisionId51,collision51);}
        if(tttLast51){Console.WriteLine("01451 actual Phoenix and ECM publish before TTT");Call(tome.GetType("TomeOfTheFirebird.New_Content.Bloodlines.PheonixBloodline",true),"MakePheonixBloodline");Call(ecm.GetType("EbonsContentMod.Bloodlines.ConfigureBloodlines",true),"Configure");}
        Console.WriteLine("01450 actual TTT Aberrant builder");Call(ttt.GetType("TabletopTweaks.Base.NewContent.Bloodlines.AberrantBloodline",true),"AddSorcererAberrantBloodline");
        Console.WriteLine("01450 actual TTT Destined builder");Call(ttt.GetType("TabletopTweaks.Base.NewContent.Bloodlines.DestinedBloodline",true),"AddSorcererDestinedBloodline");
        if(expanded51){Console.WriteLine("01451 actual TTT Aberrant Bloodrager builder");Call(ttt.GetType("TabletopTweaks.Base.NewContent.Bloodlines.AberrantBloodline",true),"AddBloodragerAberrantBloodline");Console.WriteLine("01451 actual TTT Destined Bloodrager builder");Call(ttt.GetType("TabletopTweaks.Base.NewContent.Bloodlines.DestinedBloodline",true),"AddBloodragerDestinedBloodline");}
        h.Patch(AccessTools.Method(tome.GetType("BlueprintCore.Utils.LogWrapper",true),"Error",new[]{typeof(string),typeof(Exception)}),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(BPCoreError01450)));
        if(!tttLast51){Console.WriteLine("01450 actual ToTF Phoenix builder");Call(tome.GetType("TomeOfTheFirebird.New_Content.Bloodlines.PheonixBloodline",true),"MakePheonixBloodline");}
        BlueprintFeature collision=null;BlueprintGuid collisionId=Id("BloodlinePowerChoiceSorcerer0"+"33a4f8a3bef848a3bb3c799c4ca5fb8d"+"0cb461f617bd45a89583451d946357ec"+"01446");
        if(fault){collision=new BlueprintFeature{AssetGuid=collisionId,name="ForeignOptionalCollision01450",ComponentsArray=Array.Empty<BlueprintComponent>()};collision.OnEnable();c.AddCachedBlueprint(collisionId,collision);}
        foreach(string type in new[]{"ProteanSorcererBloodline","OrcSorcererBloodline"})h.Patch(AccessTools.Method(ecm.GetType("EbonsContentMod.Bloodlines."+type,true),"Configure"),finalizer:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ProviderException01450)));
        if(Environment.GetEnvironmentVariable("COTW_01452_CASE")=="menu-external-missed"){
            var endpoint=AccessTools.Method(ecm.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true),"Configure");var completion=AccessTools.Method(T("BloodlineCompatibility01447"),"ProviderConfigured");
            new Harmony("CallOfTheWild.Wrath.Prototype.ECM01447").Unpatch(endpoint,completion);
            Check(!Harmony.GetPatchInfo(endpoint).Postfixes.Any(p=>p.PatchMethod==completion),"explicit single missing-completion fault is armed before real ECM Configure");
        }
        if(Active01453)Scan01453(c,h,ecm);
        if(!tttLast51){Console.WriteLine("01450 actual ECM complete bloodline builder");Call(ecm.GetType("EbonsContentMod.Bloodlines.ConfigureBloodlines",true),"Configure");}
        if(Active01453){if(!late)Call(T("BloodlineMutations01446"),"Register",c);return Finish01453(c,h,before);}
        if(fault){
            Check(ReferenceEquals(c.Load(collisionId),collision),"optional GUID collision never overwrites foreign asset");
            var failed=N<BlueprintProgression>(c,"0cb461f617bd45a89583451d946357ec");var good=N<BlueprintProgression>(c,"e3c4ef718ca94aba8307a8b2e8ac5700");
            var familiarFault=A<BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");
            Check(!failed.LevelEntries.SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Any(x=>x.AllFeatures.Contains(familiarFault)),"colliding optional row is not partially connected");
            var goodChoice=good.LevelEntries.SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Single(x=>x.AllFeatures.Contains(familiarFault));
            Check(N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34").LevelEntries.SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Any(x=>x.AllFeatures.Contains(familiarFault)),"optional row failure preserves native graph");
            c.RemoveCachedBlueprint(collisionId);Call(T("BloodlineMutations01446"),"Register",c);
            Check(ReferenceEquals(c.Load(goodChoice.AssetGuid),goodChoice),"retry recovers failed row without republishing successful optional row");
        }

        if(!late)Call(T("BloodlineMutations01446"),"Register",c);
        if(collision51!=null){
            Check(ReferenceEquals(c.Load(collisionId51),collision51),"new TTT optional failure preserves foreign GUID");
            var failed=N<BlueprintProgression>(c,"540390e4735646019137625a37910df0");var good=N<BlueprintProgression>(c,"31598fb4a4e04c269c99160b4d02890a");var f=A<BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");
            Check(!failed.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Any(s=>s.AllFeatures.Contains(f)),"failed new TTT row is not partly connected");
            var saved=good.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Single(s=>s.AllFeatures.Contains(f));
            c.RemoveCachedBlueprint(collisionId51);Call(T("BloodlineMutations01446"),"Register",c);
            Check(ReferenceEquals(c.Load(saved.AssetGuid),saved),"retry restores only failed TTT row and preserves successful secondary identity");
        }
        var rows=JObject.Parse(File.ReadAllText(Path.Combine(Root(),W50+"external-anchor-audit.json")))["rows"].Select(r=>r.Select(x=>(string)x).ToArray()).ToArray();
        var familiar=A<BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");
        if(Environment.GetEnvironmentVariable("COTW_01452_CASE")?.StartsWith("menu-external")==true){MenuExternal01452(c,h);if(Environment.GetEnvironmentVariable("COTW_01452_CASE")=="menu-external-missed")return checks-before;}
        foreach(var row in rows){
            var p=N<BlueprintProgression>(c,row[0]);Check(p!=null,"actual external builder produced "+row[0]);
            var choices=p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Where(s=>s.AllFeatures.Contains(familiar)).ToArray();Check(choices.Length==1,"actual external bloodline exposes Familiar through one initial-power choice: "+p.name);var choice=choices.Single();
            foreach(var token in new[]{"BloodIntensity01446","BloodPiercing01446"})Check(choice.AllFeatures.Count(f=>f==A<BlueprintFeature>(c,token))==1,"external choice contains one restored option "+p.name+token);
            Check(choice.AllFeatures.Count(f=>f.AssetGuid.ToString()==(row[1]=="Bloodrager"?"c33eddceeb3d4600be9139037e6015b6":"5d61fcd8c3d545da9f97e1b2fda8218b"))==1,"actual provider publication callback supplies correct ECM Havoc without manual reconcile: "+p.name);
        }
        ExternalChoices01450(c,h,rows,familiar);
        ExternalQualification01450(c,h,ecm,rows);
        if(expanded51)ExternalMatrix01451(c,h,ttt,ecm);
        var published=((SimpleBlueprint[])H("Loaded",c)).Where(x=>x.name.StartsWith("CotWWrathPsychicBloodline",StringComparison.Ordinal)).ToDictionary(x=>x.AssetGuid,x=>x);
        for(int i=0;i<3;i++)Call(T("BloodlineMutations01446"),"Register",c);
        Check(published.All(x=>ReferenceEquals(c.Load(x.Key),x.Value)),"repeat optional reconciliation preserves every published identity");
        var callback=AccessTools.Method(ecm.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true),"Configure");
        Check(Harmony.GetPatchInfo(callback).Postfixes.Count(x=>x.owner=="CallOfTheWild.Wrath.Prototype.ECM01447")==1,"exactly one automatic ECM publication callback");
        PsychicExpansionTests.Inventory["externalBloodlines01450Contracts"]=new JObject{["passed"]=true,["actualReleasedBuilders"]=new JArray("ECM Orc","ECM Protean","TTT Aberrant","TTT Destined","ToTF Phoenix"),["progressions"]=rows.Length,["nativeLazyComponentWarmups"]=warmedComponents01450.Count,["exactEmbeddedListContexts"]=new JArray(listContexts01450),["actualExternalECMQualifications"]=externalQualifications01450,["nativeCanSelectCalls"]=externalSelects01450,["growthRows"]=externalGrowth01450,["repeatIdentityPreserved"]=true,["singleECMPublicationCallback"]=true,["optionalCollisionAndRetry"]=fault,["lateProvider"]=late,["automaticECMCallback"]=true,["assertions"]=checks-before,["boundary"]="Released provider builder methods and their native cache graphs execute with same-member CLR access adaptations. Native lazy component getters initialize empty arrays; exact intended embedded-generic List capacity constructor and Add use original framework operations through runtime type binding to avoid cross-module emitter aliasing. Prerequisite/racial facts, the Metamagic Rager archetype identity and the exact FireShield spell identity are inert controlled leaves; mod presence, logger, actual English text IO and sprite endpoints are controlled; progression/power/mutation builders are not replaced. Full external mod startup and rendered game UI are separate observations."};
        return checks-before;
    }
}
