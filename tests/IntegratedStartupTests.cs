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
        if(args[5]=="bundle"||args[5]=="conflict") {
            var integration=cotw.GetType("CallOfTheWild.Wrath.Prototype.MediumIntegration",true);
            Call(integration,"Prepare");
            var error=(Exception)AccessTools.Field(integration,"preparationError").GetValue(null);
            if(args[5]=="conflict") {
                Check(error!=null&&error.ToString().Contains("Standalone Medium Class"),"old external Medium assembly is rejected before registration");
            } else {
                if(error!=null)throw error;
                var mainType=medium.GetType("MediumClass.Main",true);
                Check((bool)AccessTools.Property(mainType,"Integrated").GetValue(null),"packaged loader invokes real Medium preparation");
                Check(AccessTools.Field(mainType,"integratedHandler").GetValue(null)!=null,"bundled initialization handler retained");
                Check(eagerCalls==0,"bundled entry avoids premature UI initialization");
                var owner=new Harmony("CallOfTheWildWrathPrototype.Medium");var targets=owner.GetPatchedMethods().Count();
                StartupBlueprintCoreCoverage.VerifyInstalled(medium,owner);
                Check(true,"all five packaged BlueprintCore patches installed on actual targets");
                Call(integration,"Prepare");Check(targets>0&&owner.GetPatchedMethods().Count()==targets,"bundled preparation is idempotent");
                owner.UnpatchAll(owner.Id);
                StartupBlueprintCoreCoverage.VerifyRemoved(owner);
                Check(true,"packaged BlueprintCore patches removed with their owner");
            }
            fixture.UnpatchAll(fixture.Id);
            File.WriteAllText(args[6],"{\"passed\":"+assertions+",\"blueprintCorePatchesInstalled\":"+(args[5]=="bundle"?StartupBlueprintCoreCoverage.PatchCount:0)+",\"gameTested\":false}");
            Console.WriteLine("PASS: "+assertions+" packaged bootstrap checks ("+args[5]+"); blueprint registration/Unity assets not executed.");return;
        }
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
        if(PsychicPatchCoverage01458Tests.Rows(cotw).Length!=0)Check(PsychicPatchCoverage01458Tests.Verify(cotw,core)==(cotw.GetType("CallOfTheWild.Wrath.Prototype.UrbanBloodrageBuffConsumerPatch")!=null?40:cotw.GetType("CallOfTheWild.Wrath.Prototype.TemporaryKnowledge01464")!=null?37:cotw.GetType("CallOfTheWild.Wrath.Prototype.UrbanBloodrageFact01463Patch")!=null?31:cotw.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461")!=null?27:cotw.GetType("CallOfTheWild.Wrath.Prototype.PsychicSurgeCast01460Patch")!=null?19:cotw.GetType("CallOfTheWild.Wrath.Prototype.PhysicalMutationCount01459Patch")!=null?17:15),"all exact versioned058/059/060/061 production hooks installed once by actual core installer");
        if(args[5]=="missingbc")
            a.Unpatch(AccessTools.Method(typeof(AssetBundle),"LoadAsset",new[]{typeof(string),typeof(Type)}),HarmonyPatchType.Prefix,a.Id);
        StartupBlueprintCoreCoverage.VerifyInstalled(medium,a);
        Check(true,"all five BlueprintCore classes installed, none skipped");
        Check(eagerCalls==0,"Integrated startup avoids early UI initialization");
        Check(core.GetPatchedMethods().Count()>=44&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Blueprints.JsonSystem.StartGameLoader),"LoadPackTOC"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(AbilityData),"CalculateParams"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress),"ApplyChanges"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitCommandController),"TickCommandTurnBased"))&&b.GetPatchedMethods().Count()==48&&b.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.RuleAttackRoll),"TryOvercomeTargetConcealmentAndMissChance"))&&b.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.EntitySystem.Entities.AreaEffectEntityData),"IsSuitableTargetType"))&&b.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Assets.Controllers.GlobalMap.GlobalMapMovementController),"CalcPlayerSpeedModifiers"))&&b.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.UnitLogic.Buffs.BuffCollection),"PrepareFactForAttach",new[]{typeof(Kingmaker.UnitLogic.Buffs.Buff)}))&&b.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.UnitLogic.UnitHelper),"CopySpellbook"))&&b.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartConcealment),"Calculate")),"CotW early core includes bonded-spell, companion lifecycle and gambit hook classes; accepted Psychic targets unchanged");
        var thought=AccessTools.Method(typeof(Kingmaker.RuleSystem.Rules.Abilities.RuleCalculateAbilityParams),"OnTrigger");
        Check(Harmony.GetPatchInfo(thought).Owners.Contains(b.Id)&&!Harmony.GetPatchInfo(thought).Owners.Contains(a.Id),"one thought-penalty owner in integrated mode");
        var cast=AccessTools.PropertyGetter(typeof(AbilityData),"CanBeCastByCaster");
        Check(!Harmony.GetPatchInfo(cast).Owners.Contains(a.Id),"Medium installs no duplicate component transpiler");
        Check(a.GetPatchedMethods().Any(m=>m.Name=="PrepareFactForDetach"),"spirit cleanup patches retained");
        root=new BlueprintRoot {UIRoot=(UIRoot)FormatterServices.GetUninitializedObject(typeof(UIRoot))};root.UIRoot.TooltipColors=new TooltipColors();
        Call(cui,"Install",b);Call(cui,"Install",b);
        Check(eagerCalls==2,"only shared UI pair installed, idempotently");
        var logical=(Metamagic)(1<<29);
        Check(UIUtilityTexts.GetMetamagicName(logical)=="Logical"&&UIUtilityTexts.GetMetamagicList(logical)=="Logical","single Logical label");
        foreach(var name in new[]{"GetMetamagicName","GetMetamagicList"})
            Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(UIUtilityTexts),name)).Owners.SequenceEqual(new[]{b.Id}),"shared UI has only CotW owner");
        Check(core.GetPatchedMethods().Count()>=44&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Blueprints.JsonSystem.StartGameLoader),"LoadPackTOC"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(AbilityData),"CalculateParams"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress),"ApplyChanges"))&&core.GetPatchedMethods().Contains(AccessTools.Method(typeof(Kingmaker.Controllers.Units.UnitCommandController),"TickCommandTurnBased"))&&b.GetPatchedMethods().Count()==50,"89 distinct CotW targets include bonded-spell/gambit and accepted Psychic hooks");
        a.UnpatchAll(a.Id);b.UnpatchAll(b.Id);core.UnpatchAll(core.Id);fixture.UnpatchAll(fixture.Id);
        StartupBlueprintCoreCoverage.VerifyRemoved(a);
        Check(true,"all five BlueprintCore hooks cleaned up with their owner");
        File.WriteAllText(args[6],"{\"assertionsPassed\":"+assertions+",\"order\":\""+args[5]+"\",\"harmony\":\""+typeof(Harmony).Assembly.GetName().Version+"\",\"blueprintCorePatchesInstalled\":5,\"gameTested\":false,\"scope\":\"Actual integrated installers and all five merged BlueprintCore patch classes; game UI static initializer from visibility-only game copy, private AssetBundle native endpoint replaced by a managed trap in a separate Unity fixture; patched LoadAsset body unchanged.\"}");
        Console.WriteLine("PASS: "+assertions+" startup assertions, "+args[5]+", Harmony "+typeof(Harmony).Assembly.GetName().Version+". No game test.");
    }
}
