using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Class.LevelUp;

internal static partial class Feedback01449Tests
{
    static readonly string Scrapper01453="6c4429ef31104fc9a0ab5c353e0c38b4";
    static JObject scan01453;
    static object Get01453(object value,string field)=>AccessTools.Field(value.GetType(),field)?.GetValue(value);
    static bool Active01453=>Environment.GetEnvironmentVariable("COTW_01453_CASE")!=null;
    static void Scan01453(BlueprintsCache c,Harmony h,Assembly ecm)
    {
        // Call the real released Configure, including its real scanner. Only
        // existing host IO, visibility and sprite endpoints are adapted.
        Permissions01450(ecm,h,new[]{"EbonsContentMod.Archetypes.EldritchScrapper"});
        var tracked=((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintFeatureSelection>().Where(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoiceSorcerer")).ToArray();
        var prior=tracked.ToDictionary(s=>s,s=>s.ComponentsArray.ToArray());
        Call(ecm.GetType("EbonsContentMod.Archetypes.EldritchScrapper",true),"Configure");
        var archetype=N<BlueprintArchetype>(c,Scrapper01453);
        Check(archetype.name=="EldritchScrapper","actual ECM Configure publishes canonical Eldritch Scrapper");
        var changes=tracked.Where(s=>!prior[s].SequenceEqual(s.ComponentsArray)).ToArray();
        scan01453=new JObject{["actualReleasedConfigure"]=true,["actualArchetype"]=archetype.AssetGuid.ToString(),["changedChoices"]=new JArray(changes.Select(s=>new JObject{["id"]=s.AssetGuid.ToString(),["name"]=s.name,["components"]=new JArray(s.ComponentsArray.Select(x=>new JObject{["type"]=x.GetType().FullName,["checkInProgression"]=(x as Prerequisite)?.CheckInProgression,["group"]=(x as Prerequisite)?.Group.ToString(),["archetype"]=(Get01453(x,"m_Archetype") as BlueprintArchetypeReference)?.Guid.ToString(),["class"]=(Get01453(x,"m_CharacterClass") as BlueprintCharacterClassReference)?.Guid.ToString()}))}))};
        if(Environment.GetEnvironmentVariable("COTW_01450_CASE").Contains("late")){
            Check(changes.Length>0,"actual late ECM scanner changes published CotW choices");
            Check(changes.Any(s=>s.name=="CotWWrathPsychicBloodlinePowerChoiceSorcerer2489c8c4a53a111d4094d239054b26e32d3a4cb7be97a6694290f0dcfbd14711301446"),"actual scanner reaches exact user-log Abyssal ninth-level choice");
            Check(changes.All(s=>s.ComponentsArray.Length==1&&s.ComponentsArray[0] is PrerequisiteNoArchetype p&&p.CheckInProgression&&p.Group==Prerequisite.GroupType.All&&(Get01453(p,"m_Archetype") as BlueprintArchetypeReference)?.Guid.ToString()==Scrapper01453),"actual scanner authors precise expected native prerequisites");
        }
    }
    static int Finish01453(BlueprintsCache c,Harmony h,int before)
    {
        var report=scan01453??new JObject();report["mainSHA256"]=Hash01451(Main.Location);
        bool old=Environment.GetEnvironmentVariable("COTW_01453_CASE").Contains("old");
        Exception registration=null;try{Call(T("BloodlineMutations01446"),"Register",c);}catch(Exception e){registration=e;}
        report["registrationException"]=registration?.ToString();
        try{
            if(old){
                Check(registration!=null&&registration.ToString().Contains("01446 published resource changed: CotWWrathPsychicBloodlinePowerChoiceSorcerer2489c8c4a53a111d4094d239054b26e32d3a4cb7be97a6694290f0dcfbd14711301446"),"actual052 DLL reproduces exact user component-fingerprint error after real ECM scanner");
                Exception menus=null;try{MenuExternal01452(c,h);}catch(Exception e){menus=e;}
                Check(menus!=null,"old actual native menu pipeline remains incomplete after provider callback and fallback");
                var observation=JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("COTW_01452_OBSERVATION")));
                Check((int)observation["failedPaths"]==8,"old scanner failure blocks all eight real parent/menu/UI paths");
                report["negativeControl"]=true;report["actualMenus"]=observation;
            }else{
                if(registration!=null)throw new Exception("Actual ECM scan registration failed",registration);
                MenuExternal01452(c,h);report["actualMenus"]=JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("COTW_01452_OBSERVATION")));
                Guards01453(c,report);
            }
            report["passed"]=true;
        }finally{File.WriteAllText(Environment.GetEnvironmentVariable("COTW_01453_OBSERVATION"),report.ToString());}
        PsychicExpansionTests.Inventory["actualECMScan01453Contracts"]=new JObject{["passed"]=true,["observation"]=report};
        return checks-before;
    }
    static void Guards01453(BlueprintsCache c,JObject report)
    {
        string module=(string)AccessTools.Field(T("BloodlineMutations01446"),"ModuleName").GetValue(null);
        var choices=((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintFeatureSelection>().Where(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoiceSorcerer")).ToArray();
        var protectedChoices=choices.Where(s=>s.ComponentsArray.Any(x=>x is PrerequisiteNoArchetype)).ToArray();Check(protectedChoices.Length>0,"real ECM restrictions survive publication/reconciliation");
        var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var scrapper=N<BlueprintArchetype>(c,Scrapper01453);
        foreach(var s in protectedChoices){
            Check(s.ComponentsArray.Length>0&&s.ComponentsArray.All(x=>x.GetType()==typeof(PrerequisiteNoArchetype)&&(bool)Call(T("BloodlineProvider01453"),"Restriction",c,x)),"all source exclusions retained with exact native fields");
            Check(s.ComponentsArray.All(x=>ReferenceEquals(x.OwnerBlueprint,s)&&!string.IsNullOrEmpty(x.name)),"every published original/copied/mirrored restriction has native owner and component name");
            var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),15);var state=(LevelUpState)Call(typeof(Feat01424Tests),"State",u,15);
            Check(s.MeetsPrerequisites(null,u.Descriptor,state,true,false),"native progression prerequisite allows ordinary Sorcerer "+s.AssetGuid);
            u.Progression.GetClassData(cls).Archetypes.Add(scrapper);
            Check(!s.MeetsPrerequisites(null,u.Descriptor,state,true,false),"native progression prerequisite excludes real Eldritch Scrapper "+s.AssetGuid);
        }
        var core=choices.Single(s=>s.name=="CotWWrathPsychicBloodlinePowerChoiceSorcerer2489c8c4a53a111d4094d239054b26e32d3a4cb7be97a6694290f0dcfbd14711301446");
        void Reject(string name,Action alter,Action restore){try{alter();Exception error=null;try{Call(T("ArcaneBatch01446"),"Existing",c,module);}catch(Exception e){error=e;}Check(error!=null,"strict receipt rejects "+name);}finally{restore();}Check((bool)Call(T("ArcaneBatch01446"),"Existing",c,module),"receipt restored after "+name);}
        var p=(PrerequisiteNoArchetype)core.ComponentsArray.Single();var components=core.ComponentsArray;
        Reject("duplicate exact known prerequisite",()=>core.ComponentsArray=new BlueprintComponent[]{p,p},()=>core.ComponentsArray=components);
        Reject("unknown component",()=>core.ComponentsArray=new BlueprintComponent[]{p,new PrerequisiteClassLevel{Level=3}},()=>core.ComponentsArray=components);
        Reject("wrong progression flag",()=>p.CheckInProgression=false,()=>p.CheckInProgression=true);
        Reject("wrong Any group",()=>p.Group=Prerequisite.GroupType.Any,()=>p.Group=Prerequisite.GroupType.All);
        bool hide=p.HideInUI;Reject("hidden restriction",()=>p.HideInUI=!hide,()=>p.HideInUI=hide);
        var pc=Get01453(p,"m_CharacterClass");Reject("wrong class",()=>Set(p,"m_CharacterClass",BlueprintReferenceBase.CreateTyped<BlueprintCharacterClassReference>(N<BlueprintCharacterClass>(c,"d77e67a814d686842802c9cfd8ef8499"))),()=>Set(p,"m_CharacterClass",pc));
        var pa=Get01453(p,"m_Archetype");Reject("wrong archetype",()=>Set(p,"m_Archetype",BlueprintReferenceBase.CreateTyped<BlueprintArchetypeReference>(N<BlueprintArchetype>(c,"7229db6fc0b07af4180e783eed43c4d9"))),()=>Set(p,"m_Archetype",pa));
        int ranks=core.Ranks;Reject("unrelated authored field",()=>core.Ranks++,()=>core.Ranks=ranks);
        var orphan=new BlueprintFeatureSelection{AssetGuid=core.AssetGuid,name=core.name,ComponentsArray=new BlueprintComponent[]{p}};
        Check(!(bool)Call(T("BloodlineProvider01453"),"LateRestriction",c,orphan,Array.Empty<BlueprintComponent>()),"same name/GUID foreign object never qualifies as scanned canonical choice");
        var br=((SimpleBlueprint[])H("Loaded",c)).OfType<BlueprintFeatureSelection>().First(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoiceBloodrager"));var bc=br.ComponentsArray;
        try{br.ComponentsArray=new BlueprintComponent[]{p};Check(!(bool)Call(T("BloodlineProvider01453"),"LateRestriction",c,br,Array.Empty<BlueprintComponent>()),"known Sorcerer exclusion on Bloodrager is not an allowed scan edit");}finally{br.ComponentsArray=bc;}
        var ep=N<BlueprintProgression>(c,"0cb461f617bd45a89583451d946357ec").LevelEntries.Single(e=>e.Level==1).Features.OfType<BlueprintFeatureSelection>().Single();
        var healthy=N<BlueprintProgression>(c,"e3811e140c234aed9a667c1f83f34221").LevelEntries.Single(e=>e.Level==1).Features.OfType<BlueprintFeatureSelection>().Single();
        int er=ep.Ranks;var oldLists=new[]{Get01453(healthy,"m_Features"),Get01453(healthy,"m_AllFeatures")};var havoc=N<BlueprintFeature>(c,"5d61fcd8c3d545da9f97e1b2fda8218b");
        try{
            ep.Ranks++;foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(healthy,field,((BlueprintFeatureReference[])Get01453(healthy,field)).Concat(new[]{BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>(havoc)}).ToArray());
            Exception isolated=null;try{Call(T("BloodlineCompatibility01447"),"Reconcile",c);}catch(Exception e){isolated=e;}
            Check(isolated!=null&&ep.Ranks==er+1,"independent bad optional receipt is reported and never overwritten");
            Check(new[]{"m_Features","m_AllFeatures"}.All(f=>((BlueprintFeatureReference[])Get01453(healthy,f)).Count(r=>r.Guid==havoc.AssetGuid)==1),"healthy optional receipt still reconciles after unrelated receipt failure");
            Call(T("ArcaneBatch01446"),"ValidateSorcererTarget01453",c,N<BlueprintProgression>(c,"e3811e140c234aed9a667c1f83f34221"),true);
            Exception bad=null;try{Call(T("ArcaneBatch01446"),"ValidateSorcererTarget01453",c,N<BlueprintProgression>(c,"0cb461f617bd45a89583451d946357ec"),true);}catch(Exception e){bad=e;}
            Check(bad!=null,"complete-looking bad target still fails full integrity check");
        }finally{ep.Ranks=er;Set(healthy,"m_Features",oldLists[0]);Set(healthy,"m_AllFeatures",oldLists[1]);}
        var arrays=choices.ToDictionary(s=>s,s=>new object[]{Get01453(s,"m_Features"),Get01453(s,"m_AllFeatures"),s.ComponentsArray});
        Call(T("BloodlineMutations01446"),"Register",c);
        Check(choices.All(s=>ReferenceEquals(s.ComponentsArray,arrays[s][2])&&ReferenceEquals(Get01453(s,"m_Features"),arrays[s][0])&&ReferenceEquals(Get01453(s,"m_AllFeatures"),arrays[s][1])),"repeated healthy register does not replace unchanged arrays");
        report["nativeRestrictionChecks"]=protectedChoices.Length*2;report["exactProtectedChoices"]=protectedChoices.Length;report["negativeCases"]=new JArray("duplicate","unknown component","progression flag","group","hidden","class","archetype","ranks","foreign object","Bloodrager scope");report["independentReceiptIsolation"]=true;report["completeTargetIntegrityRequired"]=true;
    }
}
