using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;

internal static partial class Feedback01449Tests
{
    static int NativePowers01451(BlueprintsCache cache) {
        var input=JObject.Parse(File.ReadAllText(Path.Combine(Root(),(Main.GetType("CallOfTheWild.Wrath.Prototype.MagusBatch01454")!=null?"work/psychic01458/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineProvider01453")!=null?"work/repair01453/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452")!=null?"work/repair01452/":"work/repair01451/")+"native-power-input.json")));int hydrated=0;
        foreach(var row in input["rows"]) {
            var bp=row["blueprint"];var fact=cache.Load(BlueprintGuid.Parse((string)bp["AssetId"])) as BlueprintScriptableObject;
            if(fact==null)continue;
            fact.ComponentsArray=((JArray)bp["Data"]["Components"]??new JArray()).Select(x=>(BlueprintComponent)PsychicExpansionTests.Read(x,typeof(BlueprintComponent))).ToArray();fact.OnEnable();hydrated++;
        }
        return hydrated;
    }
    static bool Select01451(BlueprintsCache c,BlueprintCharacterClass cls,int level,BlueprintFeatureSelection pool,BlueprintFeature feature,BlueprintProgression progression=null,BlueprintArchetype archetype=null)
    {
        var unit=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),level);
        if(archetype!=null)unit.Progression.GetClassData(cls).Archetypes.Add(archetype);
        if(progression!=null)H("Fact",unit,progression);
        var state=(LevelUpState)Call(typeof(Feat01424Tests),"State",unit,level);
        var pending=state.AddSelection(null,default(FeatureSource),pool,level);
        return pool.CanSelect(unit.Descriptor,state,pending,feature);
    }
    internal static int Slots01451(BlueprintsCache parent,Harmony h)
    {
        int before=checks;var c=(BlueprintsCache)H("Fresh",parent);int nativeHydrated=NativePowers01451(c);
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Prerequisites.IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(No)));
        bool late=Environment.GetEnvironmentVariable("COTW_01451_CASE").Contains("late-assembly");
        bool loadedBefore=AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name=="EbonsContentMod");
        Assembly provider;
        if(late){Call(T("BloodlineMutations01446"),"Register",c);provider=ECM(c,h);}
        else{provider=ECM(c,h);Call(T("BloodlineMutations01446"),"Register",c);}
        var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var progression=N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34");
        var root=N<BlueprintFeatureSelection>(c,"3a60f0c0442acfb419b0c03b584e1394");var children=root.AllFeatures.OfType<BlueprintFeatureSelection>().ToArray();
        var intensity=A<BlueprintFeature>(c,"BloodIntensity01446");var piercing=A<BlueprintFeature>(c,"BloodPiercing01446");var havoc=N<BlueprintFeature>(c,"5d61fcd8c3d545da9f97e1b2fda8218b");var familiar=A<BlueprintFeatureSelection>(c,"BloodlineFamiliar01446");
        var initial=progression.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Single(s=>s.AllFeatures.Contains(familiar));
        var fifteen=progression.LevelEntries.Where(e=>e.Level==15).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().ToArray();
        var observation=new JObject{["currentMainSHA256"]=Hash01451(Main.Location),["actualECMAssemblySHA256"]=Hash01451(provider.Location),["innerBonusPools"]=children.Length,["innerPoolsMissingIntensity"]=new JArray(children.Where(s=>!s.AllFeatures.Contains(intensity)).Select(s=>s.AssetGuid.ToString())),["innerPoolsMissingPiercing"]=new JArray(children.Where(s=>!s.AllFeatures.Contains(piercing)).Select(s=>s.AssetGuid.ToString())),["nativeSorcererOneHasHavoc"]=initial.AllFeatures.Contains(havoc),["nativeSorcererOneCanSelectHavoc"]=Select01451(c,cls,1,initial,havoc),["fifteenSelectors"]=new JArray(fifteen.Select(s=>new JObject{["id"]=s.AssetGuid.ToString(),["containsIntensity"]=s.AllFeatures.Contains(intensity),["containsPiercing"]=s.AllFeatures.Contains(piercing),["containsHavoc"]=s.AllFeatures.Contains(havoc)})),["boundary"]="Actual native CanSelect, actual original ECM Configure and native cache; controlled unit/level-up state and engine icon/log endpoints. Scene UI/game save observations are separate."};
        observation["ECMAssemblyLoadedBeforeRegistration"]=loadedBefore;observation["lateAssemblyCase"]=late;
        observation["exactNativePowerComponentsHydratedBeforeProviders"]=nativeHydrated;
        observation["celestialFifteenSelectors"]=new JArray(N<BlueprintProgression>(c,"aa79c65fa0e11464d9d100b038c50796").LevelEntries.Where(e=>e.Level==15).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Select(s=>new JObject{["id"]=s.AssetGuid.ToString(),["containsHavoc"]=s.AllFeatures.Contains(havoc),["containsIntensity"]=s.AllFeatures.Contains(intensity),["containsPiercing"]=s.AllFeatures.Contains(piercing)}));
        observation["allNativeFifteenHavocOnly"]=new JArray(((string[][])AccessTools.Field(T("BloodlineAnchors01446"),"Rows").GetValue(null)).Where(r=>r[1]=="Sorcerer").Select(r=>N<BlueprintProgression>(c,r[0])).Where(p=>p.LevelEntries.Where(e=>e.Level==15).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Any(s=>s.AllFeatures.Contains(havoc)&&!s.AllFeatures.Contains(intensity)&&!s.AllFeatures.Contains(piercing))).Select(p=>p.name));
        string report=Environment.GetEnvironmentVariable("COTW_01451_OBSERVATION")??Path.Combine(Root(),"work/prototype-build/01451-observation-"+System.Diagnostics.Process.GetCurrentProcess().Id+".json");File.WriteAllText(report,observation.ToString());
        Check(children.Length>=9,"actual Sorcerer parent exposes the nine native bloodline feat submenus");
        foreach(var pool in children)foreach(var feature in new[]{intensity,piercing})Check(pool.AllFeatures.Count(x=>x==feature)==1,"01451 actual inner Sorcerer bloodline bonus pool contains exactly one mutation: "+pool.name+"/"+feature.name);
        Check(initial.AllFeatures.Count(x=>x==havoc)==1&&Select01451(c,cls,1,initial,havoc),"01451 native Sorcerer1 offers and can select actual ECM Havoc");
        Check(fifteen.Any(s=>new[]{intensity,piercing,havoc}.All(f=>s.AllFeatures.Contains(f))),"01451 independent Sorcerer15 power offers every legal mutation");
        if(late)Check(!loadedBefore,"late-assembly case really starts before ECM is loaded");
        NativeMatrix01451(c,h);
        PsychicExpansionTests.Inventory["bloodlineSlots01451Contracts"]=new JObject{["passed"]=true,["initialProbe"]=observation,["assertions"]=checks-before};return checks-before;
    }
    static string Hash01451(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
}
