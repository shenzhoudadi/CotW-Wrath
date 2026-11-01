using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using CallOfTheWild.Wrath.Prototype;

internal static partial class MagusBatch01454Tests
{
    static BlueprintGuid cacheFault057,definitionFault057;
    static bool failCache057,failDefinition057;
    static void CacheFail057(BlueprintGuid guid){if(failCache057&&guid==cacheFault057){failCache057=false;throw new InvalidOperationException("057 injected real post-cache failure");}}
    static void DefinitionFail057(BlueprintScriptableObject bp){if(failDefinition057&&bp.AssetGuid==definitionFault057){failDefinition057=false;throw new InvalidOperationException("057 injected real post-connect failure");}}
    static bool Reject057(Action action){try{action();return false;}catch(Exception e){while(e is TargetInvocationException)e=e.InnerException;if(e is InvalidOperationException)return true;throw;}}
    static void Faults057(BlueprintsCache parent,Harmony h)
    {
        EquipmentPatches(h);h.Patch(AccessTools.Method(T("PsychicOriginalIcons"),"Load"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(Icon057)));
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(CacheFail057)));
        h.Patch(AccessTools.Method(T("ArchetypeBatch01445"),"Definition"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(DefinitionFail057)));
        foreach(string module in new[]{"MutationMind01457","PsychicMarauder01457"})foreach(string fault in new[]{"occupied","cache","connect","missing"}){
            var c=PsychicCache057(parent);var cls=N<BlueprintCharacterClass>(c,"83fa3b50138c566698801ba5967bbde0");var pool=A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");
            var oldArchetypes=AccessTools.Field(cls.GetType(),"m_Archetypes").GetValue(cls);var oldUI=cls.Progression.UIGroups;var oldPool=AccessTools.Field(pool.GetType(),"m_Features").GetValue(pool);var oldAll=AccessTools.Field(pool.GetType(),"m_AllFeatures").GetValue(pool);
            string token=(string)AccessTools.Field(T(module),"RootToken").GetRawConstantValue();var tokens=(string[])AccessTools.Field(T(module),"OwnTokens").GetValue(null);
            BlueprintFeature foreign=null;
            if(fault=="occupied"){foreign=new BlueprintFeature{AssetGuid=Id(token),name="Foreign057Root"};foreign.OnEnable();c.AddCachedBlueprint(foreign.AssetGuid,foreign);}
            if(fault=="cache"){cacheFault057=Id(token);failCache057=true;}
            if(fault=="connect"){definitionFault057=Id(token);failDefinition057=true;}
            if(fault=="missing")c.RemoveCachedBlueprint(pool.AssetGuid);
            Check(Reject057(()=>Register(c,module)),"actual057 producer rejects injected "+fault+" fault: "+module);
            Check(ReferenceEquals(oldArchetypes,AccessTools.Field(cls.GetType(),"m_Archetypes").GetValue(cls))&&ReferenceEquals(oldUI,cls.Progression.UIGroups),"native class and UI arrays roll back after "+fault+": "+module);
            Check(ReferenceEquals(oldPool,AccessTools.Field(pool.GetType(),"m_Features").GetValue(pool))&&ReferenceEquals(oldAll,AccessTools.Field(pool.GetType(),"m_AllFeatures").GetValue(pool)),"both original amplification arrays roll back after "+fault+": "+module);
            Check(tokens.All(t=>c.Load(Id(t))==(t==token?foreign:null)),"failed057 producer leaves no partial private graph: "+fault+" "+module);
            failCache057=failDefinition057=false;if(foreign!=null)c.RemoveCachedBlueprint(foreign.AssetGuid);if(fault=="missing")c.AddCachedBlueprint(pool.AssetGuid,pool);
            Register(c,module);Check(tokens.All(t=>c.Load(Id(t))!=null),"same native cache retries completely after "+fault+": "+module);
        }
        foreach(bool reverse in new[]{false,true}){
            var c=PsychicCache057(parent);var cls=N<BlueprintCharacterClass>(c,"83fa3b50138c566698801ba5967bbde0");var prior=cls.Archetypes.ToArray();
            foreach(string module in reverse?new[]{"PsychicMarauder01457","MutationMind01457"}:new[]{"MutationMind01457","PsychicMarauder01457"})Register(c,module);
            Register(c,"PsychicAmplifications");Register(c,"MutationMind01457");Register(c,"PsychicMarauder01457");Check(cls.Archetypes.Length==prior.Length+2&&prior.All(a=>cls.Archetypes.Contains(a)),"actual both registration orders and original producer repeat preserve all class links: "+reverse);
            var extended=A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");
            foreach(string field in new[]{"m_Features","m_AllFeatures"}){
                var refs=(BlueprintFeatureReference[])AccessTools.Field(extended.GetType(),field).GetValue(extended);var own=refs.Single(v=>v.Guid==Id("SlimyBodilyMutationFeature01457"));
                Set(extended,field,refs.Concat(new[]{own}).ToArray());Check(Reject057(()=>Register(c,"PsychicAmplifications")),"original producer rejects duplicated declared mutation link in "+field);Set(extended,field,refs);
            }
            var changed=A<BlueprintFeature>(c,"SlimyBodilyMutationFeature01457");int ranks=changed.Ranks;changed.Ranks=ranks+1;Check(Reject057(()=>Register(c,"PsychicAmplifications")),"original producer rejects mutated private extension definition");changed.Ranks=ranks;Register(c,"PsychicAmplifications");
            var custom=Loaded(c).OfType<BlueprintScriptableObject>().SelectMany(b=>b.ComponentsArray).Where(v=>v is PhysicalMutationCaster01457||v is MarauderConfusionControl01457).ToArray();Check(custom.Length==5,"all five authored057 custom component instances participate in native roundtrip");
            foreach(var original in custom){
                var serializer=Kingmaker.Blueprints.JsonSystem.Json.Serializer;var owner=original.OwnerBlueprint;var nodes=owner.ElementsArray.ToArray();var previous=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
                try{
                    Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(owner);string text;
                    using(var writer=new StringWriter())using(var json=new JsonTextWriter(writer)){serializer.Serialize(json,original,typeof(BlueprintComponent));text=writer.ToString();}
                    BlueprintComponent restored;using(var reader=new JsonTextReader(new StringReader(text)))restored=serializer.Deserialize<BlueprintComponent>(reader);
                    Check(restored.GetType()==original.GetType()&&ReferenceEquals(restored.OwnerBlueprint,owner),"actual native polymorphic serializer restores057 TypeId and blueprint owner: "+original.GetType().Name);
                    if(original is MarauderConfusionControl01457 control)Check(((MarauderConfusionControl01457)restored).RerollNormally==control.RerollNormally&&((MarauderConfusionControl01457)restored).ForcedState==control.ForcedState,"actual native serializer preserves confusion mode fields");
                }finally{owner.ElementsArray.Clear();owner.ElementsArray.AddRange(nodes);Set(owner,"m_AllElements",owner.ElementsArray);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=previous;}
            }
        }
        PsychicExpansionTests.Inventory["psychicArchetypes01457Contracts"]=new JObject{["passed"]=true,["case"]="faults057",["assertions"]=checks,["coverage"]=new JArray(Coverage),["ownedBlueprints"]=new JArray(),["gameLaunchTested"]=false,["boundary"]="Actual cache publication, UI and pool rollback/retry and native component JSON serialization. No full save file transport or Unity rendering."};
    }
}
