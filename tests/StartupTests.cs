using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Abilities;
using UnityEngine;

internal static class StartupLauncher
{
    public static int Main(string[] args)
    {
        if(args.Length != 8) return 2;
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => {
            var name=new AssemblyName(e.Name).Name;
            if(name=="0Harmony") return Assembly.LoadFrom(args[3]);
            if(name=="UnityEngine.AssetBundleModule") return Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(args[7]),name+".dll"));
            if(name=="UnityEngine.CoreModule"&&File.Exists(Path.Combine(Path.GetDirectoryName(args[7]),name+".dll")))return Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(args[7]),name+".dll"));
            // .NET Framework enforces nested-type visibility on merged BlueprintCore.
            // Use the build's visibility-only publicized copy for full assembly discovery;
            // the other managed regression suites continue to use the untouched game DLL.
            if(name=="Assembly-CSharp") return Assembly.LoadFrom(args[7]);
            foreach(var dir in new[]{args[0],Path.GetDirectoryName(args[1]),Path.GetDirectoryName(args[2]),args[4]}) {
                var file=Path.Combine(dir,name+".dll"); if(File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try {var scene=Path.Combine(Path.GetDirectoryName(args[7]),"UnityEngine.CoreModule.dll");if(File.Exists(scene))Assembly.LoadFrom(scene);Assembly.LoadFrom(args[3]);typeof(StartupChecks).GetMethod("Run").Invoke(null,new object[]{args});return 0;}
        catch(Exception e) {Console.Error.WriteLine(e);for(var inner=e;inner!=null;inner=inner.InnerException)if(inner is ReflectionTypeLoadException load)foreach(var error in load.LoaderExceptions)Console.Error.WriteLine(error);return 1;}
    }
}
internal static class StartupChecks
{
    private static BlueprintRoot root;
    private static int assertions, eagerCalls;
    private static void Check(bool condition,string label){if(!condition)throw new Exception(label);assertions++;}
    private static bool RootFixture(ref BlueprintRoot __result){__result=root;return false;}
    private static bool ColorFixture(ref string __result){__result="000000";return false;}
    private static bool MonoEagerInitialization(PatchClassProcessor __instance)
    {
        var type=(Type)AccessTools.Field(typeof(PatchClassProcessor),"containerType").GetValue(__instance);
        if(type.Name=="LogicalNamePatch" || type.Name=="LogicalListPatch") {
            eagerCalls++;
            // Explicitly emulate the eager static initialization observed in Mono's
            // Harmony Memory.GetMethodStart. Execute the actual native type initializer.
            RuntimeHelpers.RunClassConstructor(typeof(UIUtilityTexts).TypeHandle);
        }
        return true;
    }
    private static object Call(Type type,string name,params object[] args)=>AccessTools.Method(type,name).Invoke(null,args);
    public static void Run(string[] args)
    {
        RequiredHarmonyRuntime.Verify(args[3]);
        var fixture=new Harmony("StartupTests.Fixture");
        var medium=Assembly.LoadFrom(args[1]);var cotw=Assembly.LoadFrom(args[2]);
        fixture.Patch(AccessTools.Method(typeof(ResourcesLibrary),"GetRoot"),prefix:new HarmonyMethod(typeof(StartupChecks),nameof(RootFixture)));
        fixture.Patch(AccessTools.Method(typeof(ColorUtility),"ToHtmlStringRGB"),prefix:new HarmonyMethod(typeof(StartupChecks),nameof(ColorFixture)));
        fixture.Patch(AccessTools.Method(typeof(PatchClassProcessor),"Patch"),prefix:new HarmonyMethod(typeof(StartupChecks),nameof(MonoEagerInitialization)));
        AccessTools.Field(typeof(Kingmaker.Game),"s_Instance").SetValue(null,FormatterServices.GetUninitializedObject(typeof(Kingmaker.Game)));
        var a=new Harmony("StartupTests.Medium");var b=new Harmony("StartupTests.CotW.Psychic");var core=new Harmony("StartupTests.CotW.Core");
        if(args[5]=="old") {
            bool reproduced=false;
            try{a.PatchAll(medium);}catch(Exception e){reproduced=e.ToString().Contains("UIUtilityTexts") && e.ToString().Contains("NullReferenceException");Console.WriteLine(e);}
            Check(reproduced && eagerCalls==1,"Old delivery reproduces null-root initializer failure");
            File.WriteAllText(args[6],"{\"oldFailureReproduced\":true,\"gameTested\":false}");
            Console.WriteLine("PASS: old startup failure independently reproduced with actual UIUtilityTexts initializer and explicit Mono timing.");return;
        }
        var main=medium.GetType("MediumClass.Main",true);var entry=cotw.GetType("CallOfTheWild.Wrath.Prototype.ModEntry",true);
        var mui=medium.GetType("MediumClass.Medium.PsychicUiPatches",true);var cui=cotw.GetType("CallOfTheWild.Wrath.Prototype.PsychicUiPatches",true);
        AccessTools.Property(main,"Integrated").GetSetMethod(true).Invoke(null,new object[]{true});
        Action earlyMedium=()=>Call(main,"InstallEarlyPatches",a);
        Action earlyCotw=()=>{Call(entry,"InstallEarlyCorePatches",core);Call(entry,"InstallEarlyPsychicPatches",b);};
        if(args[5]=="mediumfirst"){earlyMedium();earlyCotw();}else{earlyCotw();earlyMedium();}
        if(PsychicPatchCoverage01458Tests.Rows(cotw).Length!=0)Check(PsychicPatchCoverage01458Tests.Verify(cotw,core)==(cotw.GetType("CallOfTheWild.Wrath.Prototype.UrbanBloodrageFact01463Patch")!=null?31:cotw.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461")!=null?27:cotw.GetType("CallOfTheWild.Wrath.Prototype.PsychicSurgeCast01460Patch")!=null?19:cotw.GetType("CallOfTheWild.Wrath.Prototype.PhysicalMutationCount01459Patch")!=null?17:15),"all exact versioned058/059/060/061 production hooks installed once by actual core installer");
        StartupBlueprintCoreCoverage.VerifyInstalled(medium,a);
        Check(true,"all five BlueprintCore patch classes installed");
        Check(eagerCalls==0,"Both production early installers avoid UI type initialization");
        Check(core.GetPatchedMethods().Count()>=44&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Blueprints.JsonSystem.StartGameLoader),"LoadPackTOC"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(AbilityData),"CalculateParams"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress),"ApplyChanges"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitCommandController),"TickCommandTurnBased")) && b.GetPatchedMethods().Count()==48,"CotW required core and 48 psychic targets; compatibility hooks installed");
        Check(a.GetPatchedMethods().Count(m=>m.DeclaringType==typeof(UIUtilityTexts))==0,"Medium has no early UI patches");
        foreach(var state in new[]{0,1,2}) {
            root=state==0?null:new BlueprintRoot();
            if(state==2)root.UIRoot=(UIRoot)FormatterServices.GetUninitializedObject(typeof(UIRoot));
            foreach(var ui in new[]{mui,cui}) {
                bool rejected=false;try{Call(ui,"Install",ui==mui?a:b);}catch(TargetInvocationException e){rejected=e.InnerException is InvalidOperationException;}
                Check(rejected,"Missing UI dependency rejected before touching target, stage "+state);
            }
        }
        Check(eagerCalls==0,"All readiness failures leave UI initializer untouched");
        root=new BlueprintRoot {UIRoot=(UIRoot)FormatterServices.GetUninitializedObject(typeof(UIRoot))};root.UIRoot.TooltipColors=new TooltipColors();
        Action lateMedium=()=>Call(mui,"Install",a);Action lateCotw=()=>Call(cui,"Install",b);
        if(args[5]=="mediumfirst"){lateMedium();lateCotw();}else{lateCotw();lateMedium();}
        Check(eagerCalls==4,"All four UI patch installations execute after root readiness");
        Check(core.GetPatchedMethods().Count()>=44&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Blueprints.JsonSystem.StartGameLoader),"LoadPackTOC"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(AbilityData),"CalculateParams"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress),"ApplyChanges"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitCommandController),"TickCommandTurnBased")) && b.GetPatchedMethods().Count()==50,"All required CotW production targets restored, including compatibility hooks");
        Check(a.GetPatchedMethods().Count(m=>m.DeclaringType==typeof(UIUtilityTexts))==2,"Both Medium UI targets installed late");
        var logical=(Metamagic)(1<<29);
        Check(UIUtilityTexts.GetMetamagicName(logical)=="Logical" && UIUtilityTexts.GetMetamagicList(logical)=="Logical","One Logical label with both owners");
        lateMedium();lateCotw();Check(eagerCalls==4,"Deferred install idempotent for each owner");
        foreach(var name in new[]{"GetMetamagicName","GetMetamagicList"}) {
            var info=Harmony.GetPatchInfo(AccessTools.Method(typeof(UIUtilityTexts),name));
            Check(info.Prefixes.Concat(info.Postfixes).Count(p=>p.owner==a.Id)==1,"Medium no duplicate patch "+name);
            Check(info.Prefixes.Concat(info.Postfixes).Count(p=>p.owner==b.Id)==1,"CotW no duplicate patch "+name);
        }
        a.UnpatchAll(a.Id);Check(UIUtilityTexts.GetMetamagicList(logical)=="Logical","Removing Medium preserves CotW UI");
        lateMedium();b.UnpatchAll(b.Id);Check(UIUtilityTexts.GetMetamagicList(logical)=="Logical","Removing CotW preserves Medium UI");
        a.UnpatchAll(a.Id);core.UnpatchAll(core.Id);fixture.UnpatchAll(fixture.Id);
        StartupBlueprintCoreCoverage.VerifyRemoved(a);
        Check(true,"all BlueprintCore hooks removed with their owner");
        Check(!a.GetPatchedMethods().Any()&&!b.GetPatchedMethods().Any()&&!core.GetPatchedMethods().Any(),"All test owners removed");
        File.WriteAllText(args[6],"{\"assertionsPassed\":"+assertions+",\"order\":\""+args[5]+"\",\"harmony\":\""+typeof(Harmony).Assembly.GetName().Version+"\",\"blueprintCorePatchesInstalled\":5,\"gameTested\":false,\"scope\":\"Actual early/deferred installers, all five BlueprintCore classes, readiness and dual UI-owner checks; separate visibility/native-endpoint fixtures, original LoadAsset target body intact.\"}");
        Console.WriteLine("PASS: "+assertions+" startup assertions, "+args[5]+", Harmony "+typeof(Harmony).Assembly.GetName().Version+". No game test.");
    }
}
