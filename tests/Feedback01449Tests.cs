using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Abilities.Components;
using System.Reflection.Emit;

internal static partial class Feedback01449Tests
{
    static readonly Assembly Main=typeof(NaturesCommand01447).Assembly;
    static Type T(string s)=>Main.GetType("CallOfTheWild.Wrath.Prototype."+s,true);
    static object Call(Type t,string m,params object[] a)=>AccessTools.Method(t,m).Invoke(null,a);
    static object H(string m,params object[] a)=>Call(typeof(Arcane01446Tests),m,a);
    static BlueprintGuid Id(string s)=>(BlueprintGuid)Call(T("PsychicAssets"),"Id",s);
    static TAsset A<TAsset>(BlueprintsCache c,string token)where TAsset:SimpleBlueprint=>(TAsset)c.Load(Id(token));
    static TAsset N<TAsset>(BlueprintsCache c,string id)where TAsset:SimpleBlueprint=>(TAsset)c.Load(BlueprintGuid.Parse(id));
    static void Set(object o,string s,object v)=>AccessTools.Field(o.GetType(),s).SetValue(o,v);
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Feedback01449: "+why);checks++;}
    internal static int NatureToggle(BlueprintsCache parent,Harmony h)
    {
        int before=checks;
        var c=(BlueprintsCache)H("Fresh",parent);
        if(c.Load(Id("PhrenicAmplificationSelection"))==null)Call(T("PsychicAmplifications"),"Register",c);
        Call(T("MagaambyanTelepath01447"),"Register",c);
        foreach(string name in new[]{"NatureSpellImmunity01447Patch","NatureBuffImmunity01447Patch","NatureAmplificationSwitch01447Patch","NatureTargetScope01448Patch","NatureTargetFact01448Patch","NaturePaidTarget01448Patch"})Feedback01450Tests.PatchOnce(h,T(name));
        var cls=N<BlueprintCharacterClass>(c,(string)AccessTools.Field(T("PsychicClass"),"ClassId").GetRawConstantValue());
        var caster=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var plant=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);
        foreach(var type in typeof(BlueprintsCache).Assembly.GetTypes().Where(t=>t.Name=="ReplaceSourceBone"||t.Name=="ReplaceCastSource"))foreach(var method in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.Name=="OnTurnOn"||m.Name=="OnTurnOff"))h.Patch(method,prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(Skip)));
        H("Fact",plant,N<BlueprintFeature>(c,"706e61781d692a042b35941f14bc41c5"));Set(plant,"m_Position",new UnityEngine.Vector3(3,0,0));
        var book=(Spellbook)H("Book",caster,cls);var ability=new AbilityData(N<BlueprintAbility>(c,"3c17035ec4717674cae2e841a190e757"),caster.Descriptor);Set(ability,"m_CachedSpellbook",book);
        var pool=N<BlueprintAbilityResource>(c,(string)AccessTools.Field(T("PsychicClass"),"WisdomPoolId").GetRawConstantValue());caster.Stats.Wisdom.BaseValue=24;caster.Resources.Add(pool,true);caster.Resources.Restore(pool,99);
        H("Fact",caster,A<BlueprintFeature>(c,"NaturesCommandFeature01447"));var toggle=caster.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("NaturesCommandToggle01447"));
        Check(!ability.CanTarget(new TargetWrapper(plant)),"inactive actual ability rejects plants");toggle.IsOn=true;toggle.TryStart();
        Console.WriteLine("01449 NATURE NATIVE toggle: on="+toggle.IsOn+" started="+toggle.IsStarted+" applied="+toggle.AppliedBuff+" active="+toggle.AppliedBuff?.IsActive+" turned="+toggle.AppliedBuff?.IsTurnedOn+" pay="+Call(T("NatureTargeting01448"),"CanPay",ability,caster));
        Check(toggle.IsStarted&&toggle.AppliedBuff!=null,"actual native activatable starts and applies Nature mode");
        Check(ability.CanTarget(new TargetWrapper(plant)),"actual native toggle grants pre-cast targeting");
        toggle.OnNewRound();Check(ability.CanTarget(new TargetWrapper(plant)),"native round preserves mode without preview payment");
        toggle.TurnOffImmediately();Check(!ability.CanTarget(new TargetWrapper(plant)),"actual native switch-off restores target restriction");
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.NatureDiagnostics01449")!=null){
            h.Patch(AccessTools.Method(typeof(ModEntry),"TraceNatureTarget"),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(TraceEndpoint)));
            h.CreateClassProcessor(T("NatureTargetDiagnostic01449Patch")).Patch();traceMessages.Clear();traceThrows=false;
            toggle.IsOn=true;toggle.TryStart();caster.Resources.Spend(pool,caster.Resources.GetResourceAmount(pool));
            for(int i=0;i<10;i++)Check(!ability.CanTarget(new TargetWrapper(plant)),"native repeated insufficient-resource target stays denied "+i);
            Check(traceMessages.Count==1&&traceMessages[0].Contains(ability.Blueprint.AssetGuid.ToString())&&traceMessages[0].Contains(book.Blueprint.AssetGuid.ToString())&&traceMessages[0].Contains("canPay=False"),"actual native query emits one precise, free diagnostic for repeated failure");
            caster.Resources.Restore(pool,2);Check(ability.CanTarget(new TargetWrapper(plant))&&traceMessages.Count==1,"successful target emits no rejection trace");
            for(int i=0;i<80;i++){
                var fake=new BlueprintAbility{AssetGuid=Id("DiagnosticLimit"+i+"01449"),name="DiagnosticLimit"+i,Type=AbilityType.Spell,Range=AbilityRange.Personal,CanTargetSelf=true,CanTargetFriends=false,CanTargetEnemies=false,ComponentsArray=new BlueprintComponent[]{new Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent{Descriptor=Kingmaker.Blueprints.Classes.Spells.SpellDescriptor.MindAffecting}}};fake.OnEnable();
                var data=new AbilityData(fake,caster.Descriptor);Set(data,"m_CachedSpellbook",book);Check(!data.CanTarget(new TargetWrapper(plant)),"native personal-only target remains illegal under diagnostic "+i);
            }
            Check(traceMessages.Count==64&&caster.Resources.GetResourceAmount(pool)==2,"per-caster diagnostic cap is64 and previews never spend points");
            var other=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var otherBook=(Spellbook)H("Book",other,cls);other.Resources.Add(pool,true);other.Stats.Wisdom.BaseValue=24;other.Resources.Spend(pool,other.Resources.GetResourceAmount(pool));H("Fact",other,A<BlueprintFeature>(c,"NaturesCommandFeature01447"));var otherToggle=other.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("NaturesCommandToggle01447"));otherToggle.IsOn=true;otherToggle.TryStart();
            var otherAbility=new AbilityData(ability.Blueprint,other.Descriptor);Set(otherAbility,"m_CachedSpellbook",otherBook);traceThrows=true;
            try{Check(!otherAbility.CanTarget(new TargetWrapper(plant)),"optional logging failure cannot change or interrupt actual native rejection");}finally{traceThrows=false;otherToggle.TurnOffImmediately();toggle.TurnOffImmediately();}
            PsychicExpansionTests.Inventory["natureDiagnostics01449Contracts"]=new JObject{["passed"]=true,["nativeTogglePath"]=true,["nativeRejectionTrace"]=true,["maximumLogsPerCaster"]=64,["successfulQuerySilent"]=true,["diagnosticFailureIsolated"]=true,["noResourceSpend"]=true,["userReportedFailureReproduced"]=false,["boundary"]="Actual native toggle and CanTarget execute; diagnostic IO is captured and an IO failure injected. The old01448 ZIP also passes this Dominate Monster/toggle fixture; the user's exact spell/plant rejection path is not established by these logs, and no additional rule bypass is claimed."};
        }
        return checks-before;
    }
    static bool Skip()=>false;
    static readonly List<string> traceMessages=new List<string>();static bool traceThrows;
    static bool TraceEndpoint(string __0){if(traceThrows)throw new IOException("01449 injected diagnostic sink failure");traceMessages.Add(__0);return false;}
    static string Root()=>TestPaths.EvidenceRoot;
    static Assembly tome;
    static JObject tomeGuids;
    static int tomeFactoryCalls;
    static readonly List<string> tomeMessages=new List<string>();
    static bool TomeLog(string __0){tomeMessages.Add(__0);return false;}
    static bool TomeFactory(MethodBase __originalMethod,string __0,ref object __result)
    {
        var method=(MethodInfo)__originalMethod;string name=__0;string guid=(string)tomeGuids["NewBlueprints"][name];
        if(guid==null)throw new Exception("Actual ToTF embedded GUID unavailable: "+name);
        var newMethod=method.ReturnType.GetMethods(BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).Single(m=>m.Name=="New"&&m.GetParameters().Length==(method.Name=="MakeFeature"?3:2));
        __result=newMethod.Invoke(null,method.Name=="MakeFeature"?new object[]{name,guid,Array.Empty<FeatureGroup>()}:new object[]{name,guid});tomeFactoryCalls++;return false;
    }
    static bool False(ref bool __result){__result=false;return false;}
    static bool SpriteEndpoint(object __instance,object __0){Set(__instance,"Value",__0);return false;}
    static void ExternalSpriteBoundary(Assembly a,Harmony h)
    {
        var sprite=a.GetType("BlueprintCore.Utils.Assets.Asset`1",true).MakeGenericType(typeof(UnityEngine.Sprite));
        h.Patch(AccessTools.Constructor(sprite,new[]{typeof(UnityEngine.Sprite)}),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(SpriteEndpoint)));
    }
    // The released ToTF targets publicized game fields. Re-emitting the exact
    // IL in Harmony's skip-visibility dynamic body supplies Mono's access model
    // to the standalone CLR host. No field read/write is substituted.
    static IEnumerable<CodeInstruction> SameIL(IEnumerable<CodeInstruction> source)=>source;
    static void TomeOverhaul(BlueprintsCache c,Harmony h)
    {
        var native=JObject.Parse(File.ReadAllText(Path.Combine(Root(),"work/prototype-build/01449-native.json")));
        foreach(var row in native["rows"]){var bp=row["blueprint"];var guid=BlueprintGuid.Parse((string)bp["AssetId"]);if(c.Load(guid)!=null)continue;
            string kind=((string)bp["Data"]["$type"]).Split(',').Last().Trim();var type=typeof(BlueprintsCache).Assembly.GetTypes().Single(t=>t.Name==kind);
            var value=(BlueprintScriptableObject)PsychicExpansionTests.Read(bp["Data"],type);value.name=(string)row["name"];value.AssetGuid=guid;
            var icon=AccessTools.Field(value.GetType(),"m_Icon");if(icon?.FieldType==typeof(UnityEngine.Sprite))icon.SetValue(value,System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite)));
            value.OnEnable();c.AddCachedBlueprint(guid,value);
        }
        Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/references/UnityModManager.dll"));
        Assembly.LoadFrom(Path.Combine(Root(),Environment.GetEnvironmentVariable("COTW_01450_CASE")?.StartsWith("external")==true?W50+"runtime-adapter/TabletopTweaks-Core.dll":"work/repair01440/dependencies/TabletopTweaks-Core/TabletopTweaks-Core/TabletopTweaks-Core.dll"));
        tome=Assembly.LoadFrom(Path.Combine(Root(),(Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null?W50:"work/repair01449/")+"runtime-adapter/TomeOfTheFirebird.dll"));
        var resource=tome.GetManifestResourceNames().Single(n=>n.EndsWith("Config.Blueprints.json"));
        using(var stream=tome.GetManifestResourceStream(resource))using(var reader=new StreamReader(stream))tomeGuids=JObject.Parse(reader.ReadToEnd());
        ExternalSpriteBoundary(tome,h);
        foreach(var m in tome.GetType("BlueprintCore.Utils.LogWrapper",true).GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Where(m=>m.ReturnType==typeof(void)&&m.GetParameters().Length>0&&m.GetParameters()[0].ParameterType==typeof(string)))h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(TomeLog)));
        var flagClosure=tome.GetType("BlueprintCore.Blueprints.Configurators.UnitLogic.Buffs.BaseBuffConfigurator`2+<>c__DisplayClass5_0",true).MakeGenericType(typeof(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff),tome.GetType("BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs.BuffConfigurator",true));
        foreach(var m in flagClosure.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(m=>m.Name.Contains("b__")))h.Patch(m,transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(SameIL)));
        var factory=tome.GetType("TomeOfTheFirebird.Helpers.MakerTools",true);
        foreach(var m in factory.GetMethods().Where(m=>m.Name=="MakeBuff"||m.Name=="MakeToggle"||m.Name=="MakeFeature"&&m.GetParameters()[1].ParameterType==typeof(string)))h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(TomeFactory)));
        h.Patch(AccessTools.Method(tome.GetType("TomeOfTheFirebird.Settings",true),"IsDisabled",new[]{typeof(string)}),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(False)));
        var overhaul=AccessTools.Method(tome.GetType("TomeOfTheFirebird.Modified_Content.Bloodlines.DraconicBloodlineModifications",true),"SorcererClawsOverhaul");h.Patch(overhaul,transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(SameIL)));
        tomeFactoryCalls=0;overhaul.Invoke(null,null);
        File.WriteAllText(Path.Combine(Root(),(Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null?W50:"work/repair01449/")+"tome-managed-messages-"+System.Diagnostics.Process.GetCurrentProcess().Id+".json"),new JArray(tomeMessages).ToString());
        Check(tomeFactoryCalls==15,"actual released ToTF claw overhaul executes all fifteen feature/buff/toggle builders");
        var p=N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34");
        Check(p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).Any(f=>f.AssetGuid.ToString()=="b808e4d9bf7e4a189cdb2f4a25ec0b86"),"actual ToTF overhaul publishes the same replacement GUID as user log");
    }
    static Assembly ECM(BlueprintsCache c,Harmony h)
    {
        Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/references/UnityModManager.dll"));
        Assembly.LoadFrom(Path.Combine(Root(),"work/repair01440/dependencies/TabletopTweaks-Core/TabletopTweaks-Core/TabletopTweaks-Core.dll"));
        var a=Assembly.LoadFrom(Path.Combine(Root(),"work/batch01447/dependencies/ECM/EbonsContentMod.dll"));
        var log=AccessTools.Field(a.GetType("EbonsContentMod.Main",true),"log");log.SetValue(null,System.Runtime.Serialization.FormatterServices.GetUninitializedObject(log.FieldType));
        foreach(var m in log.FieldType.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(m=>m.ReturnType==typeof(void)&&m.Name=="Log"))h.Patch(m,prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(Skip)));
        ExternalSpriteBoundary(a,h);
        foreach(string id in new[]{"24bef8d1bee12274686f6da6ccbc8914","62b33ac8ceb18dd47ad4c8f06849bc01"}){var s=N<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,id);Set(s,"m_Features",s.AllFeatures.Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>).ToArray());}
        foreach(var p in ((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintProgression>())foreach(var e in p.LevelEntries){var warm=e.Features.Count;}
        Call(a.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true),"Configure");return a;
    }
    internal static int Bloodline(BlueprintsCache parent,Harmony h,bool external)
    {
        int before=checks;
        var c=(BlueprintsCache)H("Fresh",parent);Assembly ecm=null;bool late=Environment.GetEnvironmentVariable("COTW_01449_CASE")=="bloodline-late";
        if(external){TomeOverhaul(c,h);if(!late)ecm=ECM(c,h);Call(typeof(Feedback01448Tests),"NativeProxyClone",N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34"));}
        var priorFirst=new Dictionary<string,string[]>();
        var coreIds=new HashSet<string>(((string[][])AccessTools.Field(T("BloodlineAnchors01446"),"Rows").GetValue(null)).Concat((string[][])AccessTools.Field(T("BloodlineVariants01447"),"Rows").GetValue(null)).Select(r=>r[0]));
        if(external)foreach(var p in ((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintProgression>().Where(p=>coreIds.Contains(p.AssetGuid.ToString()))){
            var first=p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>(BlueprintFeatureBase[])Call(T("BloodlineCompatibility01447"),"Features",e)).ToArray();
            bool Claws(BlueprintFeatureBase f)=>f.AssetGuid.ToString()=="b808e4d9bf7e4a189cdb2f4a25ec0b86"||f is Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection s&&s.AllFeatures.Any(x=>x.AssetGuid.ToString()=="b808e4d9bf7e4a189cdb2f4a25ec0b86");
            if(first.Any(Claws))priorFirst[p.AssetGuid.ToString()]=first.Where(f=>!Claws(f)).Select(f=>f.AssetGuid.ToString()).ToArray();
        }
        Call(T("BloodlineMutations01446"),"Register",c);
        if(external&&late){ecm=ECM(c,h);Call(T("BloodlineCompatibility01447"),"Reconcile",c);}
        foreach(var token in Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null?new[]{"BloodHavoc01446"}:new[]{"BloodHavoc01446","BloodIntensity01446","BloodPiercing01446"})foreach(var suffix in new[]{"","Buff","Toggle","Resource"})Check(c.Load(Id(token+suffix))==null,"removed CotW mutation asset stays absent: "+token+suffix);
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null)foreach(string token in new[]{"BloodIntensity01446","BloodPiercing01446"})foreach(string suffix in new[]{"","Buff","Toggle","Resource"})Check(c.Load(Id(token+suffix))!=null,"new explicit request restores the original positive asset instead of old retirement negative: "+token+suffix);
        var familiar=A<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");
        Check(familiar!=null&&familiar.AllFeatures.Count()==13,"Familiar exists with actual thirteen native companions");
        Console.WriteLine("01449 BLOODLINE actual provider="+external+" Familiar="+familiar.AssetGuid);
        var rows=(string[][])Call(T("BloodlineCompatibility01447"),"Rows",c);
        int count=0;
        foreach(var row in rows.Where(r=>r[2]!="")){
            var p=N<BlueprintProgression>(c,row[0]);var choice=p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>().Single(s=>s.AllFeatures.Contains(familiar));
            Check(choice.AllFeatures.Count(x=>x==familiar)==1,"single Familiar for "+p.name);
            if(external)Check(choice.AllFeatures.Any(x=>x.AssetGuid.ToString()==(row[1]=="Bloodrager"?"c33eddceeb3d4600be9139037e6015b6":"5d61fcd8c3d545da9f97e1b2fda8218b")),"ECM's actual own feature is present for "+p.name);
            count++;
        }
        Check(count==70,"45 native/21 Seeker/4 Primal Familiar first-power paths");
        Call(T("BloodlineMutations01446"),"Register",c);Call(T("BloodlineCompatibility01447"),"Reconcile",c);
        if(external){NativeSelections(c,h);ProviderQualification(c,h,ecm);TomeGrowth(c,priorFirst);FingerprintControls(c);}
        PsychicExpansionTests.Inventory["bloodlineIntegration01449Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-before,["actualTomeOverhaul"]=external,["actualECMConfigure"]=external,["actualLateECMConfigure"]=external&&late,["nativeProxyClone"]=external,["ownMutationAssetsAbsent"]=Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")==null,["ownHavocAbsentIntensityPiercingRestored"]=Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null};
        return checks-before;
    }
    static bool No(ref bool __result){__result=false;return false;}
    static void NativeSelections(BlueprintsCache c,Harmony h)
    {
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Prerequisites.IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(No)));
        // The older general fixture only hydrated common blueprint fields.
        // Restore the actual native archetype entry arrays for this parent-menu
        // test; do not invent a new parent menu or modify production registration.
        var seekerRow=PsychicExpansionTests.NativeRows.Single(r=>(string)r["blueprint"]["AssetId"]=="7229db6fc0b07af4180e783eed43c4d9");
        var seekerArchetype=N<BlueprintArchetype>(c,"7229db6fc0b07af4180e783eed43c4d9");
        foreach(string field in new[]{"AddFeatures","RemoveFeatures"})Set(seekerArchetype,field,PsychicExpansionTests.Read(seekerRow["blueprint"]["Data"][field],typeof(LevelEntry[])));
        var rows=(string[][])Call(T("BloodlineCompatibility01447"),"Rows",c);var familiar=A<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");
        var seekerIds=new HashSet<string>(((string[][])AccessTools.Field(T("BloodlineVariants01447"),"Rows").GetValue(null)).Select(r=>r[0]));
        int native=0,seeker=0,primal=0;
        foreach(var row in rows.Where(r=>r[2]!="")){
            var p=N<BlueprintProgression>(c,row[0]);var cls=N<BlueprintCharacterClass>(c,row[1]=="Sorcerer"?"b3a505fb61437dc4097f43c3f8f9a4cf":"d77e67a814d686842802c9cfd8ef8499");
            var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),1);BlueprintArchetype archetype=null;
            if(seekerIds.Contains(row[0])){archetype=N<BlueprintArchetype>(c,"7229db6fc0b07af4180e783eed43c4d9");seeker++;}
            else if(Enumerable.Range(0,4).Any(i=>row[0]==Id("Primal"+new[]{"Air","Earth","Fire","Water"}[i]+"Progression01444").ToString())){archetype=A<BlueprintArchetype>(c,"PrimalSorcererArchetype01444");primal++;}else native++;
            if(archetype!=null)u.Progression.GetClassData(cls).Archetypes.Add(archetype);
            var state=(Kingmaker.UnitLogic.Class.LevelUp.LevelUpState)Call(typeof(Feat01424Tests),"State",u,1);
            var choice=p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>().Single(s=>s.AllFeatures.Contains(familiar));
            var pending=state.AddSelection(null,default(FeatureSource),choice,1);
            Check(choice.CanSelect(u.Descriptor,state,pending,familiar),"actual archetype-bearing native CanSelect Familiar: "+p.name);
            if(archetype!=null){
                var parents=archetype.AddFeatures.SelectMany(e=>e.Features).OfType<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>().Where(s=>s.AllFeatures.Contains(p)).ToArray();
                Check(parents.Length==1,"exactly one actual archetype parent for "+p.name+"; actual "+string.Join(",",archetype.AddFeatures.SelectMany(e=>e.Features).Select(x=>x.name)));
                var parent=parents[0];
                var parentPending=state.AddSelection(null,default(FeatureSource),parent,1);
                Check(parent.CanSelect(u.Descriptor,state,parentPending,p),"actual archetype parent bloodline menu admits "+p.name);
            }
        }
        Check(native==45&&seeker==21&&primal==4,"exact actual selection counts with each relevant archetype attached");
        PsychicExpansionTests.Inventory["bloodlineReference01448Contracts"]=new JObject{["passed"]=true,["nativeLevelUpCanSelect"]=true,["nativeBloodlines"]=native,["nativeRowsAudited"]=46,["seekerBloodlines"]=seeker,["primalBloodlines"]=primal,["nativeShallowCloneProxy"]=true,["actualProviderConfigurator"]=true,["archetypesAttached"]=true,["actualParentSelection"]=true,["boundary"]="Actual native CanSelect on all70 child choices and25 archetype parent pools. ToTF's released claw overhaul executes with a same-field CLR access adapter, resource factory/log/icon boundaries; ECM Configure is the unmodified release DLL. Complete level-up commit/UI and game rendering remain scene observations."};
    }
    static void ProviderQualification(BlueprintsCache c,Harmony h,Assembly ecm)
    {
        int before=checks;
        var helper=ecm.GetType("EbonsContentMod.Utilities.ComponentHelperators",true);Call(helper,"CreateBloodlineSpellList");
        var provider=N<BlueprintFeature>(c,"5d61fcd8c3d545da9f97e1b2fda8218b");var source=provider.GetType();
        var rows=(string[][])Call(T("BloodlineCompatibility01447"),"Rows",c);var ids=new HashSet<string>(((string[][])AccessTools.Field(T("BloodlineVariants01447"),"Rows").GetValue(null)).Select(r=>r[0]));
        foreach(string element in new[]{"Air","Earth","Fire","Water"})ids.Add(Id("Primal"+element+"Progression01444").ToString());
        int qualified=0,damageCases=0;
        foreach(var row in rows.Where(r=>ids.Contains(r[0]))){
            var p=N<BlueprintProgression>(c,row[0]);var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var book=(Spellbook)H("Book",u,cls);
            var grants=p.LevelEntries.SelectMany(e=>e.Features).SelectMany(f=>f.GetComponents<AddKnownSpell>().Where(k=>k.Spell!=null).Select(k=>new{Feature=(BlueprintFeature)f,Spell=k.Spell})).ToArray();
            Check(grants.Length>0,"real variant bloodline has a spell grant "+p.name);var g=grants.FirstOrDefault(x=>x.Spell.GetComponent<AbilityVariants>()==null&&(bool)Call(T("ArcaneCast01446"),"Damage",x.Spell))??grants[0];
            H("Fact",u,p);Check(!(bool)Call(helper,"GetIsBloodlineSpell",g.Spell,u),"ungranted variant spell does not qualify "+p.name);H("Fact",u,g.Feature);
            Check((bool)Call(helper,"GetIsBloodlineSpell",g.Spell,u),"actual ECM qualification recognizes owned variant and granted spell "+p.name);qualified++;
            var unrelated=((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintAbility>().First(a=>a.IsSpell&&!grants.Any(x=>x.Spell==a||a.Parent==x.Spell));
            Check(!(bool)Call(helper,"GetIsBloodlineSpell",unrelated,u),"unrelated spell does not qualify "+p.name);
            if(g.Spell.GetComponent<AbilityVariants>()!=null||!(bool)Call(T("ArcaneCast01446"),"Damage",g.Spell))continue;
            var fact=(Feature)H("Fact",u,provider);var victim=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var cast=(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell)H("Cast",u,victim,book,g.Spell,19);
            var damage=new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(new Kingmaker.RuleSystem.DiceFormula(4,Kingmaker.RuleSystem.DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Fire);
            var precision=new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(new Kingmaker.RuleSystem.DiceFormula(2,Kingmaker.RuleSystem.DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Cold){Precision=true};
            var rule=(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)H("Damage",cast,victim,new Kingmaker.RuleSystem.Rules.Damage.BaseDamage[]{damage,precision});var runtime=fact.Components.Single(r=>r.SourceBlueprintComponent.GetType().FullName=="EbonsContentMod.Components.BloodHavoc");
            using(runtime.RequestEventContext())AccessTools.Method(runtime.SourceBlueprintComponent.GetType(),"OnEventAboutToTrigger",new[]{typeof(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)}).Invoke(runtime.SourceBlueprintComponent,new object[]{rule});
            Check(damage.Bonus==4&&precision.Bonus==0,"ECM's real own handler adds per die once and preserves precision exclusion "+p.name);damageCases++;
        }
        Check(qualified==25&&damageCases>=5,"all25 variant query positives and available actual damaging-spell cases execute");
        var endpoint=AccessTools.Method(helper,"GetIsBloodlineSpell",new[]{typeof(BlueprintAbility),typeof(UnitEntityData)});var owner=(string)AccessTools.Field(T("BloodlineCompatibility01447"),"PatchOwner").GetRawConstantValue();
        for(int i=0;i<4;i++)Call(T("BloodlineCompatibility01447"),"Reconcile",c);
        Check(Harmony.GetPatchInfo(endpoint).Postfixes.Count(p=>p.owner==owner)==1,"ECM qualification patch remains singular after repeat reconciliation");
        var handler=AccessTools.Method(ecm.GetType("EbonsContentMod.Components.BloodHavoc"),"OnEventAboutToTrigger",new[]{typeof(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)});
        Check(!(Harmony.GetPatchInfo(handler)?.Prefixes.Any(p=>p.owner==owner)??false),"CotW does not take over or suppress ECM's damage handler");
        var registration=ecm.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true);
        string key="EbonsContentMod.BloodlineMutationSelection.7bd143ead2d6c3a409aad6ee22effe34.Level1";
        var providerChoice=BlueprintGuid.Parse(new Guid((string)Call(registration,"CreateDeterministicGuid",key)).ToString("N"));
        if(Environment.GetEnvironmentVariable("COTW_01449_CASE")!="bloodline-late")Check(c.Load(providerChoice)!=null,"actual deterministic ECM wrapper asset is retained after our integration");
        Check(N<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,"3a60f0c0442acfb419b0c03b584e1394").AllFeatures.Contains(provider),"ECM's own public feat entry is not stripped or replaced by CotW");
        PsychicExpansionTests.Inventory["ecmMutations01449Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-before,["variantQualifications"]=qualified,["actualECMDamageCases"]=damageCases,["ecmOwnFeatureAndHandler"]=true,["noGrantAndUnrelatedNegatives"]=true,["singleQualificationPatch"]=true,["noDamagePrefix"]=true,["ecmFeatEntryRetained"]=true};
        PsychicExpansionTests.Inventory["bloodlineCompatibility01447Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-before,["seekerBloodlines"]=21,["primalBloodlines"]=4,["actualECMComponent"]=true,["actualECMGuidFunction"]=true,["singleDamageBonus"]=true,["repeatOptionReconciliation"]=true,["authority"]="ECM owns Havoc;01450 restores CotW Intensity/Piercing by the new explicit request; previous removal remains historical",["boundary"]="ECM's unmodified release qualification query and damage handler execute on all25 variants with actual native grants, negative grant/unrelated-spell checks, repeated reconciliation, and source-owned feat retained. Actual late Configure runs in its separate fresh-process case."};
    }
    static void TomeGrowth(BlueprintsCache c,Dictionary<string,string[]> priorFirst)
    {
        int paths=0;var first="b808e4d9bf7e4a189cdb2f4a25ec0b86";var tiers=new[]{"77b7d401476a4cbe90ca6be7a990ba53","217fc84676094b0e8ccb9eb6bf815ef3","4386eb19ba714b54b719ce77e943d42d"};
        foreach(var row in ((string[][])AccessTools.Field(T("BloodlineAnchors01446"),"Rows").GetValue(null)).Concat((string[][])AccessTools.Field(T("BloodlineVariants01447"),"Rows").GetValue(null))){
            var p=N<BlueprintProgression>(c,row[0]);var selection=p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>().FirstOrDefault(s=>s.AllFeatures.OfType<BlueprintProgression>().Any(b=>b.LevelEntries.SelectMany(e=>e.Features).Any(x=>x.AssetGuid.ToString()==first)));
            if(selection==null)continue;paths++;var branch=selection.AllFeatures.OfType<BlueprintProgression>().Single(b=>b.LevelEntries.SelectMany(e=>e.Features).Any(x=>x.AssetGuid.ToString()==first));
            for(int i=0;i<3;i++){
                int level=new[]{5,7,11}[i];Check(!new ProgressionData(p).GetLevelEntry(level).Features.Any(x=>x.AssetGuid.ToString()==tiers[i]),"TOF claw upgrade is forfeited in Familiar/provider branch "+p.name+level);
                Check(new ProgressionData(branch).GetLevelEntry(level).Features.Count(x=>x.AssetGuid.ToString()==tiers[i])==1,"TOF original claw branch retains tier once "+p.name+level);
            }
            var retained=p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).Select(x=>x.AssetGuid.ToString()).ToArray();var before=priorFirst[p.AssetGuid.ToString()];
            Check(before.All(id=>retained.Count(x=>x==id)==before.Count(x=>x==id)),"all actual non-power parent facts retain exact counts "+p.name);
        }
        Check(paths==20,"ten native and ten Seeker TOF-overhauled first powers preserve exact continuation paths");
        PsychicExpansionTests.Inventory["tomeClaws01449Contracts"]=new JObject{["passed"]=true,["paths"]=paths,["growthRows"]=paths*3,["sameReplacementAsUserLog"]=true,["actualReleasedOverhaul"]=true,["nonPowerParentFactsPreserved"]=true};
    }
    static void FingerprintControls(BlueprintsCache c)
    {
        var p=N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34");var familiar=A<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");
        var selector=p.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>().Single(s=>s.AllFeatures.Contains(familiar));
        bool Rejected(){try{Call(T("BloodlineCompatibility01447"),"Reconcile",c);return false;}catch(Exception error){while(error is TargetInvocationException)error=error.InnerException;if(error is InvalidOperationException&&error.Message.Contains("published resource changed"))return true;throw;}}
        var old=(BlueprintFeatureReference[])AccessTools.Field(selector.GetType(),"m_AllFeatures").GetValue(selector);var own=new BlueprintFeature{AssetGuid=Id("ForeignFingerprintControl01449"),name="ForeignFingerprintControl01449",ComponentsArray=Array.Empty<BlueprintComponent>()};own.OnEnable();c.AddCachedBlueprint(own.AssetGuid,own);
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null){
            foreach(string field in new[]{"m_Features","m_AllFeatures"}){var list=(BlueprintFeatureReference[])AccessTools.Field(selector.GetType(),field).GetValue(selector);Set(selector,field,list.Reverse().ToArray());}
            var reordered=(BlueprintFeatureReference[])AccessTools.Field(selector.GetType(),"m_AllFeatures").GetValue(selector);Call(T("BloodlineCompatibility01447"),"Reconcile",c);
            Check(((BlueprintFeatureReference[])AccessTools.Field(selector.GetType(),"m_AllFeatures").GetValue(selector)).Select(r=>r.Guid).SequenceEqual(reordered.Select(r=>r.Guid)),"pure provider menu sorting preserves original multiset and actual new order");old=reordered;
        }
        var foreign=old.Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(own)}).ToArray();Set(selector,"m_AllFeatures",foreign);Check(Rejected()&&ReferenceEquals(AccessTools.Field(selector.GetType(),"m_AllFeatures").GetValue(selector),foreign),"foreign append is rejected and not overwritten by provider reconciliation");Set(selector,"m_AllFeatures",old);
        int rank=selector.Ranks;selector.Ranks=rank+1;Check(Rejected()&&selector.Ranks==rank+1,"unrelated owned selector rank change is rejected without waiving fingerprints");selector.Ranks=rank;
        var missing=old.Skip(1).ToArray();Set(selector,"m_AllFeatures",missing);Check(Rejected(),"removing the original power option is rejected");Set(selector,"m_AllFeatures",old);
        var provider=N<BlueprintFeature>(c,"5d61fcd8c3d545da9f97e1b2fda8218b");Set(selector,"m_AllFeatures",old.Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(provider)}).ToArray());Call(T("BloodlineCompatibility01447"),"Reconcile",c);
        var current=(BlueprintFeatureReference[])AccessTools.Field(selector.GetType(),"m_AllFeatures").GetValue(selector);
        Check(current.Select(r=>r.Guid).SequenceEqual(old.Select(r=>r.Guid))&&current.Count(r=>r.Guid==provider.AssetGuid)==1,"only duplicate known ECM leaf is normalized and every original option/order retained");
        Call(T("BloodlineMutations01446"),"Register",c);Check(ReferenceEquals(c.Load(selector.AssetGuid),selector),"verified provider normalization commits a stable receipt without republishing assets");
        PsychicExpansionTests.Inventory["bloodlineFingerprint01449Contracts"]=new JObject{["passed"]=true,["foreignAppendRejected"]=true,["unrelatedRankRejected"]=true,["originalRemovalRejected"]=true,["knownProviderDuplicateNormalized"]=true,["noForeignOverwrite"]=true,["samePublishedIdentities"]=true,["permutationPreserved"]=Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineExternal01450")!=null};
    }
    internal static int VersatileLifecycle(BlueprintsCache parent)
    {
        int before=checks;var c=(BlueprintsCache)H("Fresh",parent);Call(T("Admixture01446"),"Register",c);
        var wizard=(UnitEntityData)H("Unit",c,"ba34257984f4c41408ce1dc2004e342e",19);wizard.Stats.Intelligence.BaseValue=24;
        var cls=N<BlueprintCharacterClass>(c,"ba34257984f4c41408ce1dc2004e342e");var book=(Spellbook)H("Book",wizard,cls);var victim=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);
        H("Fact",wizard,A<BlueprintFeature>(c,"AdmixtureVersatileFeature01446"));
        var toggle=wizard.ActivatableAbilities.Enumerable.Single(a=>a.Blueprint.AssetGuid==Id("AdmixtureFireVersatileToggle01446"));
        var pool=A<BlueprintAbilityResource>(c,"AdmixtureVersatileResource01446");int count=wizard.Resources.GetResourceAmount(pool);
        toggle.IsOn=true;toggle.TryStart();Check(toggle.IsStarted&&toggle.AppliedBuff!=null,"native Versatile toggle starts its actual mode buff");toggle.OnNewRound();
        Check(wizard.Resources.GetResourceAmount(pool)==count&&toggle.ResourceCount==count,"native never-spend activation/round and actual actionbar resource count");
        var wrapper=N<BlueprintAbility>(c,"ab395d2335d3f384e99dddee8562978f");var original=(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell)H("Cast",wizard,victim,book,wrapper,19);original.Context.Params.SpellLevel=1;
        var current=AccessTools.Field(typeof(BardMasterpieces01435Tests),"current");var previous=current.GetValue(null);var events=new Kingmaker.RuleSystem.RulebookEventContext();events.PushEvent(original);current.SetValue(null,events);
        try{Kingmaker.PubSubSystem.RulebookEventBus.OnEventAboutToTrigger(original);original.SetSuccess(true);Kingmaker.PubSubSystem.RulebookEventBus.OnEventDidTrigger(original);}finally{events.PopEvent(original);current.SetValue(null,previous);}
        Check(wizard.Resources.GetResourceAmount(pool)==count-1&&toggle.ResourceCount==count-1,"native actual rule event bus charges Versatile once");
        var sticky=wrapper.GetComponent<AbilityEffectStickyTouch>();var part=wizard.Ensure<Kingmaker.UnitLogic.Parts.UnitPartTouch>();part.Init(sticky.TouchDeliveryAbility,original.Spell,original.Context);
        Check(part.Ability.StickyTouch==original.Spell&&part.Ability.OverrideParams.Metamagic==original.Context.Params.Metamagic,"native held touch inherits paid Versatile params and original ability identity");
        var deliveryContext=new AbilityExecutionContext(part.Ability.Data,part.Ability.OverrideParams,new TargetWrapper(victim),null,wizard);
        var mode=toggle.AppliedBuff.Blueprint.GetComponent<ArcaneCastMode01446>();var delivery=(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell));
        foreach(var pair in new[]{new KeyValuePair<string,object>("Spell",part.Ability.Data),new KeyValuePair<string,object>("Context",deliveryContext),new KeyValuePair<string,object>("Initiator",wizard),new KeyValuePair<string,object>("SpellTarget",new TargetWrapper(victim))})AccessTools.Field(delivery.GetType(),pair.Key).SetValue(delivery,pair.Value);
        using((IDisposable)H("Scope",toggle.AppliedBuff,mode))mode.OnEventAboutToTrigger(delivery);
        Check(wizard.Resources.GetResourceAmount(pool)==count-1,"native held-touch delivery cannot charge a second Versatile use");
        toggle.TurnOffImmediately();Check(!toggle.IsStarted&&!wizard.Buffs.Enumerable.Any(b=>b.Blueprint==A<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(c,"AdmixtureFireVersatileBuff01446")),"actual native switch-off removes only the mode");
        var damage=new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(new Kingmaker.RuleSystem.DiceFormula(1,Kingmaker.RuleSystem.DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Electricity);
        var calc=(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)H("Damage",delivery,victim,new Kingmaker.RuleSystem.Rules.Damage.BaseDamage[]{damage});new Kingmaker.RuleSystem.Rules.Damage.RulePrepareDamage(calc.ParentRule).OnTrigger(new Kingmaker.RuleSystem.RulebookEventContext());
        Check(damage.EnergyType==Kingmaker.Enums.Damage.DamageEnergyType.Fire,"paid native held touch still converts damage after the mode is off");
        var later=(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell)H("Cast",wizard,victim,book,wrapper,19);Check(later.Context.Params.Metamagic==0,"next unpaid spell has no conversion metadata");
        H("Fact",wizard,A<BlueprintFeature>(c,"AdmixtureAuraFeature01446"));var aura=wizard.ActivatableAbilities.Enumerable.First(a=>a.Blueprint.AssetGuid==Id("AdmixtureFireAuraToggle01446"));var rounds=A<BlueprintAbilityResource>(c,"AdmixtureAuraResource01446");count=wizard.Resources.GetResourceAmount(rounds);aura.IsOn=true;aura.TryStart();Check(aura.IsStarted&&aura.AppliedBuff!=null,"native elemental aura carrier starts");aura.OnNewRound();Check(wizard.Resources.GetResourceAmount(rounds)==count-1,"native elemental aura spends exactly one round");aura.TurnOffImmediately();Check(!aura.IsStarted&&!wizard.Buffs.Enumerable.Any(b=>b.Blueprint==A<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(c,"AdmixtureFireAuraHost01446")),"native aura stop removes its own carrier");
        PsychicExpansionTests.Inventory["arcaneLifecycle01446"]=new JObject{["passed"]=true,["nativeToggleStartStop"]=true,["nativeRuleEventBus"]=true,["nativeResourceCounter"]=true,["nativeHeldTouchTransfer"]=true,["nativeAuraRoundSpend"]=true,["heldTouchMode"]="Versatile Evocation; removed Intensity lifecycle superseded by explicit retirement tests",["sceneBoundary"]="Native mode/buff/resource/event bus and held-touch transfer execute; final combat and area/avatar creation are controlled engine endpoints."};
        return checks-before;
    }
    internal static int Retirement(BlueprintsCache parent)
    {
        int before=checks;var c=(BlueprintsCache)H("Fresh",parent);Call(T("BloodlineMutations01446"),"Register",c);
        var retired=new[]{"BloodHavoc01446","BloodIntensity01446","BloodIntensity01446Buff","BloodIntensity01446Toggle","BloodIntensity01446Resource","BloodPiercing01446","BloodPiercing01446Buff","BloodPiercing01446Toggle","BloodPiercing01446Resource"};
        foreach(string token in retired)Check(c.Load(Id(token))==null,"retired actual CotW blueprint remains absent "+token);
        foreach(string name in new[]{"BloodIntensityRank01446Patch","ArcaneDamage01446Patch","BloodPiercingResistance01446Patch","BloodIntensityDamageInfo01446Patch"})Check(Main.GetType("CallOfTheWild.Wrath.Prototype."+name)==null,"removed own mechanic has no Harmony patch type "+name);
        var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var victim=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),19);var book=(Spellbook)H("Book",u,cls);var bp=N<BlueprintAbility>(c,"2d81362af43aeac4387a3d4fced489c3");
        var cast=(Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell)H("Cast",u,victim,book,bp,19);var original=cast.Context.Params;
        foreach(var kind in new[]{ArcaneMode01446.Intensity,ArcaneMode01446.Piercing}){Call(T("ArcaneCast01446"),"Begin",new ArcaneCastMode01446{Kind=kind,Resource=null},cast);Check(ReferenceEquals(cast.Context.Params,original)&&cast.Context.Params.Metamagic==0,"legacy serialized mode is inert even with no resource "+kind);}
        var damage=new Kingmaker.RuleSystem.Rules.Damage.EnergyDamage(new Kingmaker.RuleSystem.DiceFormula(4,Kingmaker.RuleSystem.DiceType.D6),Kingmaker.Enums.Damage.DamageEnergyType.Fire);var rule=(Kingmaker.RuleSystem.Rules.Damage.RuleCalculateDamage)H("Damage",cast,victim,new Kingmaker.RuleSystem.Rules.Damage.BaseDamage[]{damage});
        new BloodHavoc01446().OnEventAboutToTrigger(rule);Check(damage.Bonus==0,"legacy serialized CotW Havoc type has no active damage behavior");
        Check((int)AccessTools.Field(T("TopplingSpell"),"Metamagic").GetRawConstantValue()==(1<<28),"Toppling's existing bit28 remains untouched");
        Check(AccessTools.Field(T("ArcaneCast01446"),"Piercing")==null&&AccessTools.Field(T("ArcaneCast01446"),"Intensity")==null,"removed mutations no longer own bits27/28");
        PsychicExpansionTests.Inventory["bloodlineRetirement01449Contracts"]=new JObject{["passed"]=true,["retiredAssets"]=new JArray(retired),["retiredAssetCount"]=retired.Length,["retiredHarmonyTargets"]=4,["legacyComponentsInert"]=true,["noOldMetamagicBits"]=true,["topplingUnchanged"]=true,["reason"]="User explicitly requested removal of CotW Havoc/Intensity/Piercing; this mandatory negative contract supersedes only their old positive behavior contract."};
        return checks-before;
    }
}
// Test host only. Exact native field operations for a publicized-reference
// external DLL running on CLR, where game Mono's access model is unavailable.
public static class NativeFieldBridge01449
{
    static FieldInfo Field(string type,string name)=>typeof(BlueprintsCache).Assembly.GetType(type,true).GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)??throw new Exception("Native adapter field changed "+type+"::"+name);
    public static T Read<T>(object value,string type,string name)=>(T)Field(type,name).GetValue(value);
    public static void Write<T>(object value,T data,string type,string name)=>Field(type,name).SetValue(value,data);
    public static bool HasRanks(object value,string type)=>(bool)typeof(BlueprintsCache).Assembly.GetType(type,true).GetMethod("get_HasRanks",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Invoke(value,null);
}
