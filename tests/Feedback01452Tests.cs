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
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

internal static partial class Feedback01449Tests
{
    static LevelUpController controller01452;static UnitEntityData preview01452;static Kingmaker.Game game01452;static bool uiScope01452;
    static void GameResult01452(ref Kingmaker.Game __result){if(uiScope01452)__result=game01452;}
    static void ControllerResult01452(ref LevelUpController __result){if(uiScope01452)__result=controller01452;}
    static bool Preview01452(ref UnitEntityData __result){__result=preview01452;return false;}
    static void BindView01452(Harmony h)
    {
        game01452=Kingmaker.Game.Instance??throw new Exception("Existing managed fixture Game is required; do not replace its actual Player context.");
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game),"Instance"),postfix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(GameResult01452)));
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Game),"LevelUpController"),postfix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(ControllerResult01452)));
        h.Patch(AccessTools.PropertyGetter(typeof(LevelUpController),"Preview"),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(Preview01452)));
        h.Patch(AccessTools.Constructor(typeof(FeatureSelectionViewState),new[]{typeof(FeatureSelectionState),typeof(IFeatureSelection),typeof(IFeatureSelectionItem)}),transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(SameIL)));
        h.Patch(AccessTools.Method(typeof(FeatureSelectionViewState),"RefreshCanSelectState"),transpiler:new HarmonyMethod(typeof(Feedback01449Tests),nameof(SameIL)));
    }
    static void MenuExternal01452(BlueprintsCache c,Harmony h)
    {
        var entry=Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452");if(entry!=null)h.CreateClassProcessor(entry).Patch();
        BindView01452(h);
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Prerequisites.IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(No)));
        var report=new JObject{["mainSHA256"]=Hash01451(Main.Location),["paths"]=new JArray()};
        var br=BloodragerGraph01452(c);var shapes=br.ToDictionary(b=>b,b=>(string)Call(T("ArchetypeBatch01445"),"Definition",b));
        string output=Environment.GetEnvironmentVariable("COTW_01452_OBSERVATION");
        try{
            int failures=0;
            foreach(string id in new[]{"a1a8bf61cadaa4143b2d4966f2d1142e","4d491cf9631f7e9429444f4aed629791","7bd143ead2d6c3a409aad6ee22effe34","d3a4cb7be97a6694290f0dcfbd147113","0cb461f617bd45a89583451d946357ec","e3811e140c234aed9a667c1f83f34221","1ff2d53c2bee429a801eeebb1197bcd1","62f017f07222457489eed83165e25682"}){
                var row=new JObject{["progression"]=id};((JArray)report["paths"]).Add(row);
                try{ActualParentPath01452(c,id,row);}catch(Exception e){row["failure"]=e.ToString();failures++;}
            }
            report["failedPaths"]=failures;Check(failures==0,"all actual parent/generated menu/UI-state/Havoc commit paths pass");
            Check(br.All(b=>ReferenceEquals(c.Load(b.AssetGuid),b)&&shapes[b]==(string)Call(T("ArchetypeBatch01445"),"Definition",b)),"Sorcerer entry leaves every actual Bloodrager progression and nested power graph unchanged");report["unchangedBloodragerGraphAssets"]=br.Length;
            GuardCases01452(c,report);
        }catch(Exception e){report["failure"]=e.ToString();throw;}finally{File.WriteAllText(output,report.ToString());}
        PsychicExpansionTests.Inventory["actualExternalMenu01452Contracts"]=new JObject{["passed"]=true,["observation"]=report};
    }
    static BlueprintScriptableObject[] BloodragerGraph01452(BlueprintsCache c)
    {
        var rows=(string[][])Call(T("BloodlineCompatibility01447"),"Rows",c);var ext=(string[][])AccessTools.Field(T("BloodlineExternal01450"),"Rows").GetValue(null);
        var seen=new HashSet<BlueprintScriptableObject>();
        Action<BlueprintFeatureBase> visit=null;visit=b=>{if(b==null||!seen.Add(b))return;if(b is BlueprintProgression p)foreach(var f in p.LevelEntries.SelectMany(e=>e.Features))visit(f);if(b is BlueprintFeatureSelection s)foreach(var f in s.AllFeatures)visit(f);};
        foreach(var row in rows.Concat(ext).Where(r=>r[1]=="Bloodrager"))visit(N<BlueprintProgression>(c,row[0]));return seen.ToArray();
    }
    static void GuardCases01452(BlueprintsCache c,JObject report)
    {
        if(Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452")==null)return;
        var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var p=N<BlueprintProgression>(c,"a1a8bf61cadaa4143b2d4966f2d1142e");var s=p.LevelEntries.Single(e=>e.Level==1).Features.OfType<BlueprintFeatureSelection>().Single();
        var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),1);Set(u.Progression,"m_Progressions",new Dictionary<BlueprintProgression,ProgressionData>());
        var state=(LevelUpState)Call(typeof(Feat01424Tests),"State",u,1);u.Progression.SureProgressionData(p).Level=1;
        var fields=new[]{"m_Features","m_AllFeatures"};var arrays=fields.Select(f=>AccessTools.Field(s.GetType(),f).GetValue(s)).ToArray();var entries=p.LevelEntries;
        LevelUpHelper.UpdateProgression(state,u.Descriptor,p);Check(ReferenceEquals(entries,p.LevelEntries)&&fields.Select((f,i)=>ReferenceEquals(arrays[i],AccessTools.Field(s.GetType(),f).GetValue(s))).All(x=>x),"complete repeated upgrade has zero menu writes");
        var havoc=N<BlueprintFeature>(c,"5d61fcd8c3d545da9f97e1b2fda8218b");
        try{
            foreach(string f in fields)Set(s,f,((BlueprintFeatureReference[])AccessTools.Field(s.GetType(),f).GetValue(s)).Where(r=>r.Guid!=havoc.AssetGuid).ToArray());
            var missing=fields.Select(f=>AccessTools.Field(s.GetType(),f).GetValue(s)).ToArray();
            c.RemoveCachedBlueprint(havoc.AssetGuid);LevelUpHelper.UpdateProgression(state,u.Descriptor,p);
            Check(fields.Select((f,i)=>ReferenceEquals(missing[i],AccessTools.Field(s.GetType(),f).GetValue(s))).All(x=>x),"absent actual ECM Havoc does not invent an effect or option");c.AddCachedBlueprint(havoc.AssetGuid,havoc);
            var other=(LevelUpState)Call(typeof(Feat01424Tests),"State",(UnitEntityData)H("Unit",c,"d77e67a814d686842802c9cfd8ef8499",1),1);
            LevelUpHelper.UpdateProgression(other,u.Descriptor,p);Check(fields.Select((f,i)=>ReferenceEquals(missing[i],AccessTools.Field(s.GetType(),f).GetValue(s))).All(x=>x),"wrong selected class cannot trigger Sorcerer repair");
            int ranks=s.Ranks;s.Ranks=ranks+1;try{LevelUpHelper.UpdateProgression(state,u.Descriptor,p);Check(s.Ranks==ranks+1&&fields.Select((f,i)=>ReferenceEquals(missing[i],AccessTools.Field(s.GetType(),f).GetValue(s))).All(x=>x),"unknown private graph conflict is isolated and not overwritten");}finally{s.Ranks=ranks;}
            var replacement=new BlueprintProgression{AssetGuid=p.AssetGuid,name="ForeignSameGuid01452",ComponentsArray=Array.Empty<BlueprintComponent>(),LevelEntries=p.LevelEntries};c.RemoveCachedBlueprint(p.AssetGuid);c.AddCachedBlueprint(p.AssetGuid,replacement);
            try{LevelUpHelper.UpdateProgression(state,u.Descriptor,p);Check(ReferenceEquals(c.Load(p.AssetGuid),replacement),"foreign canonical progression identity is not overwritten");}finally{c.RemoveCachedBlueprint(p.AssetGuid);c.AddCachedBlueprint(p.AssetGuid,p);}
        }finally{if(c.Load(havoc.AssetGuid)==null)c.AddCachedBlueprint(havoc.AssetGuid,havoc);for(int i=0;i<fields.Length;i++)Set(s,fields[i],arrays[i]);}
        report["guardCases"]=new JArray("healthy repeated no write","absent real Havoc no fabrication","wrong selected class no repair","unknown private graph rejected","foreign canonical identity retained");
    }
    static void ActualParentPath01452(BlueprintsCache c,string id,JObject report)
    {
        var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");var root=N<BlueprintFeatureSelection>(c,"24bef8d1bee12274686f6da6ccbc8914");
        var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),1);u.Stats.Charisma.BaseValue=18;
        Set(u.Progression,"m_Selections",new Dictionary<BlueprintFeatureSelection,FeatureSelectionData>());Set(u.Progression,"m_Progressions",new Dictionary<BlueprintProgression,ProgressionData>());
        var state=(LevelUpState)Call(typeof(Feat01424Tests),"State",u,1);var pending=state.AddSelection(null,new FeatureSource(cls),root,1);
        var item=root.ExtractSelectionItems(u.Descriptor,u.Descriptor).Single(x=>x.Feature.AssetGuid.ToString()==id);var p=(BlueprintProgression)item.Feature;
        report["actualName"]=p.name;report["actualParentItemIsCanonical"]=ReferenceEquals(c.Load(p.AssetGuid),p);
        // Construct the real cache before the entry patch runs: external
        // progression snapshots must also recover after late publication.
        var cached=u.Progression.SureProgressionData(p);int priorLevel=cached.Level;var priorArchetypes=cached.Archetypes;
        var select=new SelectFeature(pending,item);report["parentAllowed"]=select.Check(state,u.Descriptor);Check((bool)report["parentAllowed"],"actual root item Check admits progression "+p.name);select.Apply(state,u.Descriptor);
        Check(priorLevel==0&&cached.Level==1&&ReferenceEquals(priorArchetypes,cached.Archetypes),"native progression advances once and preserves archetype cache "+p.name);report["preexistingProgressionCacheAdvancedOnce"]=true;
        var derived=state.Selections.Where(s=>s.Source.Blueprint==p&&s.Level==1).ToArray();report["menus"]=new JArray(derived.Select(s=>new JObject{["id"]=(s.Selection as BlueprintFeature)?.AssetGuid.ToString(),["name"]=(s.Selection as BlueprintFeature)?.name,["items"]=new JArray(s.Selection.ExtractSelectionItems(u.Descriptor,u.Descriptor).Select(x=>new JObject{["id"]=x.Feature.AssetGuid.ToString(),["name"]=x.Feature.name,["available"]=new SelectFeature(s,x).Check(state,u.Descriptor)}))}));
        var menu=derived.Single(s=>s.Selection.Items.Any(x=>x.Feature.AssetGuid==Id("BloodlineFamiliar01446")||x.Feature.AssetGuid.ToString()=="5d61fcd8c3d545da9f97e1b2fda8218b"));
        controller01452=(LevelUpController)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(LevelUpController));Set(controller01452,"State",state);Set(controller01452,"Unit",u);preview01452=u;
        uiScope01452=true;try{report["nativeUIStates"]=new JArray(menu.Selection.ExtractSelectionItems(u.Descriptor,u.Descriptor).Select(x=>{var view=new FeatureSelectionViewState(menu,menu.Selection,x);return new JObject{["id"]=x.Feature.AssetGuid.ToString(),["state"]=view.CanSelectState.ToString(),["canSelect"]=view.CanSelect,["nested"]=view.CanSelectNestedFeatures};}));}finally{uiScope01452=false;}
        var ui=(JArray)report["nativeUIStates"];Check(ui.Any(x=>(string)x["id"]=="5d61fcd8c3d545da9f97e1b2fda8218b"&&(bool)x["canSelect"]),"actual native UI state admits Havoc at first level "+p.name);
        Check(ui.Any(x=>(string)x["id"]==Id("BloodlineFamiliar01446").ToString()&&(bool)x["canSelect"])&&ui.Count(x=>new[]{Id("BloodIntensity01446").ToString(),Id("BloodPiercing01446").ToString()}.Contains((string)x["id"])&&!(bool)x["canSelect"])==2,"actual UI shows Familiar and keeps underlevel I/P forbidden "+p.name);
        var havoc=menu.Selection.ExtractSelectionItems(u.Descriptor,u.Descriptor).Single(x=>x.Feature.AssetGuid.ToString()=="5d61fcd8c3d545da9f97e1b2fda8218b");
        var take=new SelectFeature(menu,havoc);Check(take.Check(state,u.Descriptor),"native generated first-power Havoc action Check "+p.name);take.Apply(state,u.Descriptor);
        Check(u.Progression.Features.GetRank(havoc.Feature)==1&&!take.Check(state,u.Descriptor),"native Havoc Apply grants once and disallows replay "+p.name);report["havocAppliedOnce"]=true;
    }
    internal static int Menu01452(BlueprintsCache parent,Harmony h)
    {
        int before=checks;var c=(BlueprintsCache)H("Fresh",parent);NativePowers01451(c);
        h.Patch(AccessTools.PropertyGetter(typeof(Kingmaker.Blueprints.Classes.Prerequisites.IgnorePrerequisites),"Ignore"),prefix:new HarmonyMethod(typeof(Feedback01449Tests),nameof(No)));
        bool late=Environment.GetEnvironmentVariable("COTW_01452_CASE").Contains("late");
        if(late){Call(T("BloodlineMutations01446"),"Register",c);ECM(c,h);}
        else{ECM(c,h);Call(T("BloodlineMutations01446"),"Register",c);}
        var cls=N<BlueprintCharacterClass>(c,"b3a505fb61437dc4097f43c3f8f9a4cf");
        var root=N<BlueprintFeatureSelection>(c,"24bef8d1bee12274686f6da6ccbc8914");
        var p=N<BlueprintProgression>(c,"7bd143ead2d6c3a409aad6ee22effe34");
        var u=(UnitEntityData)H("Unit",c,cls.AssetGuid.ToString(),1);
        u.Stats.Charisma.BaseValue=18;
        Set(u.Progression,"m_Selections",new Dictionary<BlueprintFeatureSelection,FeatureSelectionData>());
        Set(u.Progression,"m_Progressions",new Dictionary<BlueprintProgression,ProgressionData>());
        var state=(LevelUpState)Call(typeof(Feat01424Tests),"State",u,1);
        var pending=state.AddSelection(null,new FeatureSource(cls),root,1);
        var select=new SelectFeature(pending,p);
        var report=new JObject{["mainSHA256"]=Hash01451(Main.Location),["case"]=Environment.GetEnvironmentVariable("COTW_01452_CASE"),["progression"]=p.AssetGuid.ToString(),["parent"]=root.AssetGuid.ToString(),["parentItems"]=new JArray(root.ExtractSelectionItems(u.Descriptor,u.Descriptor).Select(x=>x.Feature.AssetGuid.ToString())),["parentCanSelect"]=select.Check(state,u.Descriptor)};
        string output=Environment.GetEnvironmentVariable("COTW_01452_OBSERVATION")??Path.Combine(Root(),"work/repair01452/menu.observation.json");
        try{
            Check((bool)report["parentCanSelect"],"real Sorcerer bloodline parent admits native progression");
            select.Apply(state,u.Descriptor);
            report["selections"]=new JArray(state.Selections.Select(s=>new JObject{["id"]=(s.Selection as BlueprintFeature)?.AssetGuid.ToString(),["name"]=(s.Selection as BlueprintFeature)?.name,["level"]=s.Level,["source"]=s.Source.Blueprint?.AssetGuid.ToString(),["selected"]=s.SelectedItem?.Feature.AssetGuid.ToString(),["items"]=new JArray(s.Selection.ExtractSelectionItems(u.Descriptor,u.Descriptor).Select(x=>new JObject{["id"]=x.Feature.AssetGuid.ToString(),["name"]=x.Feature.name,["available"]=s.Selection.CanSelect(u.Descriptor,state,s,x)}))}));
            Check(state.Selections.Any(s=>s.Source.Blueprint==p&&s.Level==1&&s.Selection.Items.Any(x=>x.Feature.AssetGuid.ToString()=="5d61fcd8c3d545da9f97e1b2fda8218b")),"actual bloodline selection Apply/UpdateProgression generates first-power menu containing ECM Havoc");
        }catch(Exception e){report["failure"]=e.ToString();throw;}finally{File.WriteAllText(output,report.ToString());}
        PsychicExpansionTests.Inventory["bloodlineActualMenu01452Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-before,["observation"]=report};return checks-before;
    }
}
