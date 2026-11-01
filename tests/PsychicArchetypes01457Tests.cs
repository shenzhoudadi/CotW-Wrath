using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static bool Icon057(string token,ref UnityEngine.Sprite __result)
    {
        if(token=="ElongatedFingers"){
            using(var stream=Main.GetManifestResourceStream("CotW.ElongatedFingers.png"))using(var bytes=new MemoryStream()){
                Check(stream!=null,"059 Long Arm original image is embedded in actual candidate");stream.CopyTo(bytes);
                Check(bytes.ToArray().SequenceEqual(File.ReadAllBytes(Path.Combine(Root(),"outputs/CallOfTheWild.Wrath.Prototype/assets/ElongatedFingers.png"))),"059 actual embedded Long Arm image equals pinned source PNG");
            }
            __result=(UnityEngine.Sprite)FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));return false;
        }
        if(token!="Slimy")return true;
        using(var s=Main.GetManifestResourceStream("CotW.Slimy.png"))using(var m=new MemoryStream()){
            if(s==null)throw new Exception("057 original icon missing from actual candidate DLL");s.CopyTo(m);
            Check(m.ToArray().SequenceEqual(File.ReadAllBytes(Path.Combine(Root(),"outputs/CallOfTheWild.Wrath.Prototype/assets/Slimy.png"))),"actual embedded Slimy image equals original pinned upstream PNG");
        }
        __result=(UnityEngine.Sprite)FormatterServices.GetUninitializedObject(typeof(UnityEngine.Sprite));return false;
    }
    static BlueprintsCache PsychicCache057(BlueprintsCache parent)
    {
        var parentBasic=(BlueprintFeatureSelection)parent.Load(BlueprintGuid.Parse("247a4068296e8be42890143f451b4b45"));
        var c=Fresh(parent);var cls=N<BlueprintCharacterClass>(c,"83fa3b50138c566698801ba5967bbde0");Check(cls!=null,"existing complete Psychic class is required");
        // The054 lazy reader deliberately replaces native rows with raw JSON.
        // This scenario also repeats the original amplification producer, so
        // retain its actual already-published parent basic-feat links as input.
        var basic=N<BlueprintFeatureSelection>(c,"247a4068296e8be42890143f451b4b45");
        foreach(string field in new[]{"m_Features","m_AllFeatures"}){
            var refs=(BlueprintFeatureReference[])AccessTools.Field(parentBasic.GetType(),field).GetValue(parentBasic);
            var copies=refs.Select(v=>BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>((BlueprintFeature)c.Load(v.Guid))).ToArray();Set(basic,field,copies);
        }
        Register(c,"PsychicAmplifications");Check(true,"actual original amplification producer repeats before057 registration with its integrated feat links");
        return c;
    }
    static void Psychic057(BlueprintsCache parent,Harmony h,string chosen)
    {
        EquipmentPatches(h);h.Patch(AccessTools.Method(T("PsychicOriginalIcons"),"Load"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Icon057)));
        var c=PsychicCache057(parent);var cls=N<BlueprintCharacterClass>(c,"83fa3b50138c566698801ba5967bbde0");
        var selection=A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");var old=selection.AllFeatures.ToArray();
        var archetypes=cls.Archetypes.ToArray();var groups=cls.Progression.UIGroups.ToArray();
        string module=chosen=="mutation057"?"MutationMind01457":"PsychicMarauder01457";string rootToken=chosen=="mutation057"?"MutationMindArchetype01457":"PsychicMarauderArchetype01457";
        Register(c,module);var root=A<BlueprintArchetype>(c,rootToken);Check(root!=null&&root.GetParentClass()==cls,"private057 archetype registers under actual Psychic class");
        var tokens=(string[])AccessTools.Field(T(module),"OwnTokens").GetValue(null);var assets=tokens.Select(t=>c.Load(Id(t))).ToArray();
        Check(assets.All(v=>v!=null)&&tokens.Length==tokens.Distinct().Count(),"complete057 declared graph exists without GUID collisions");
        Check(cls.Archetypes.Length==archetypes.Length+1&&archetypes.All(a=>cls.Archetypes.Contains(a)),"all existing Psychic archetypes remain in parent list");
        Check(cls.Progression.UIGroups.Length==groups.Length+1&&groups.All(g=>cls.Progression.UIGroups.Contains(g)),"existing class UI groups remain exact");
        Register(c,module);Check(cls.Archetypes.Count(a=>a.AssetGuid==root.AssetGuid)==1&&tokens.Select(t=>c.Load(Id(t))).SequenceEqual(assets),"repeated057 registration keeps exact references and one parent link");
        var p=new ProgressionData(cls.Progression);p.AddArchetype(root);
        if(chosen=="mutation057"){
            Check(!p.GetLevelEntry(1).Features.Contains(selection)&&p.GetLevelEntry(3).Features.Contains(selection),"native progression removes only first amplification and retains later choices");
            Check(selection.AllFeatures.Length==old.Length+MutationChoiceCount058&&old.All(v=>selection.AllFeatures.Contains(v)),"all old amplification rows remain with exactly "+MutationChoiceCount058+" versioned gated bodily mutations");
            Register(c,"PsychicAmplifications");Check(selection.AllFeatures.Length==old.Length+MutationChoiceCount058,"actual original amplification producer safely repeats with declared057 extension");
        }else{
            Check(new[]{3,11,19,20}.All(l=>!p.GetLevelEntry(l).Features.Contains(selection)),"native Marauder progression removes replaced choices including both mastery choices");
            Check(p.GetLevelEntry(1).Features.Contains(selection)&&p.GetLevelEntry(7).Features.Contains(selection)&&p.GetLevelEntry(15).Features.Contains(selection),"unreplaced Psychic amplifications remain selectable");
            Check(selection.AllFeatures.SequenceEqual(old),"Marauder never changes the shared amplification pool");
        }
        if(chosen=="mutation057")MutationMechanics057(c,root,h);else MarauderMechanics057(c,root,h);
        PsychicExpansionTests.Inventory["psychicArchetypes01457Contracts"]=new JObject{["passed"]=true,["case"]=chosen,["assertions"]=checks,["coverage"]=new JArray(Coverage),["ownedBlueprints"]=new JArray(assets.Select(a=>new JObject{["id"]=a.AssetGuid.ToString(),["name"]=a.name,["type"]=a.GetType().FullName})),["gameLaunchTested"]=false,["boundary"]="Actual native cache/progression, registered parent feat links, equipment/resources/stats and strict registration; Unity icon decoding/rendering is an explicit endpoint. Native round/area/controller, saves and ownership cases remain separate mandatory release coverage."};
    }
}
