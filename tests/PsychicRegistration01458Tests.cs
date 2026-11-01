using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.FactLogic;

internal static partial class MagusBatch01454Tests
{
    static object Field058(object value,string field)=>AccessTools.Field(value.GetType(),field).GetValue(value);
    static void Registration058(BlueprintsCache parent,Harmony h)
    {
        var serializationOwners058=new System.Collections.Generic.List<BlueprintScriptableObject>();var owned058=new System.Collections.Generic.Dictionary<BlueprintGuid,SimpleBlueprint>();void Record058(SimpleBlueprint bp){Check(bp!=null&&bp.AssetGuid!=default(BlueprintGuid),"058 recorded owned identity is published");if(owned058.TryGetValue(bp.AssetGuid,out var before))Check(before.name==bp.name&&before.GetType()==bp.GetType(),"058 identity/type/name stays stable across native caches: "+bp.name);else owned058.Add(bp.AssetGuid,bp);}
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),"AddCachedBlueprint"),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(CacheFail057)));
        h.Patch(AccessTools.Method(T("ArchetypeBatch01445"),"Definition"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(DefinitionFail057)));
        foreach(string fault in new[]{"occupied","cache","connect","missing"}){
            var c=PsychicCache057(parent);var pools=new[]{A<BlueprintFeatureSelection>(c,"DisciplineSelection"),A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection"),A<BlueprintFeatureSelection>(c,"ExtraPhrenicAmplificationFeat")};var before=pools.Select(p=>new[]{Field058(p,"m_Features"),Field058(p,"m_AllFeatures")}).ToArray();var prior=Loaded(c).ToArray();
            BlueprintFeature foreign=null;if(fault=="occupied"){foreign=new BlueprintFeature{AssetGuid=Id("PainDiscipline01458"),name="Foreign058Occupied"};foreign.OnEnable();c.AddCachedBlueprint(foreign.AssetGuid,foreign);}
            if(fault=="cache"){cacheFault057=Id("DarkHalfBuff01458");failCache057=true;}
            if(fault=="connect"){definitionFault057=Id("PainDiscipline01458");failDefinition057=true;}
            if(fault=="missing")c.RemoveCachedBlueprint(pools[1].AssetGuid);
            Check(Reject057(()=>Register(c,"PsychicClassCompletion01458")),"actual core058 producer rejects real "+fault+" publication fault");
            for(int i=0;i<pools.Length;i++)Check(ReferenceEquals(before[i][0],Field058(pools[i],"m_Features"))&&ReferenceEquals(before[i][1],Field058(pools[i],"m_AllFeatures")),"actual core058 rollback preserves both original arrays for "+fault+" / "+i);
            Check(!Loaded(c).Except(prior).OfType<BlueprintScriptableObject>().Any(v=>v!=foreign&&v.name.EndsWith("01458",StringComparison.Ordinal)),"failed core058 producer leaves no partial owned graph: "+fault);
            failCache057=failDefinition057=false;if(foreign!=null)c.RemoveCachedBlueprint(foreign.AssetGuid);if(fault=="missing")c.AddCachedBlueprint(pools[1].AssetGuid,pools[1]);Register(c,"PsychicClassCompletion01458");
            Check(A<BlueprintProgression>(c,"PainDiscipline01458")!=null&&A<BlueprintFeature>(c,"WillOfTheDeadFeature01458")!=null,"same real cache retries completely after "+fault);
            Register(c,"PsychicClassCompletion01458");Register(c,"PsychicAmplifications");
            foreach(var bp in Loaded(c).Where(v=>v.name.StartsWith("CotWWrathPsychic",StringComparison.Ordinal)&&(v.name.Contains("01458")||(Main.GetType("CallOfTheWild.Wrath.Prototype.MimicCatalogue01461")!=null&&v.name.Contains("MimicMetamagic")&&v.name.Contains("FirstFlag")&&v.name.EndsWith("01461",StringComparison.Ordinal)))))Record058(bp);
        }
        foreach(bool mutationFirst in new[]{false,true}){
            var c=PsychicCache057(parent);if(mutationFirst)Register(c,"MutationMind01457");Register(c,"PsychicClassCompletion01458");if(!mutationFirst)Register(c,"MutationMind01457");
            Register(c,"PsychicAmplifications");Register(c,"MutationMind01457");Register(c,"PsychicClassCompletion01458");
            var normal=A<BlueprintFeatureSelection>(c,"PhrenicAmplificationSelection");var extra=A<BlueprintFeatureSelection>(c,"ExtraPhrenicAmplificationFeat");foreach(var pool in new[]{normal,extra})foreach(string field in new[]{"m_Features","m_AllFeatures"}){
                var refs=(BlueprintFeatureReference[])Field058(pool,field);var own=refs.Single(v=>v.Guid==Id("WillOfTheDeadFeature01458"));Set(pool,field,refs.Concat(new[]{own}).ToArray());Check(Reject057(()=>Register(c,"PsychicAmplifications")),"original producer rejects duplicated058 link in both pools/fields: "+mutationFirst+" / "+pool.name+" / "+field);Set(pool,field,refs);
            }
            var ability=A<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(c,"PainfulReminderAbility01458");var type=ability.Type;ability.Type=Kingmaker.UnitLogic.Abilities.Blueprints.AbilityType.SpellLike;
            Check(Reject057(()=>Register(c,"PsychicClassCompletion01458"))&&Reject057(()=>Register(c,"PsychicAmplifications")),"core and original receipt both reject changed058 owned graph: "+mutationFirst);ability.Type=type;Register(c,"PsychicClassCompletion01458");Register(c,"PsychicAmplifications");
            Check(normal.AllFeatures.Count(v=>v.AssetGuid==Id("ElongatedFingersBodilyMutationFeature01457"))==1&&extra.AllFeatures.Count(v=>v.AssetGuid==Id("ElongatedFingersBodilyMutationFeature01457"))==1,"both actual producer orders retain exactly one fingers choice in each declared parent");
            foreach(string token in new[]{"ElongatedFingersBodilyMutationFeature01457","ElongatedFingersBodilyMutationBuff01457","PhysicalMutationControllerBuff01458","PhysicalMutationToggle01458"})Record058(c.Load(Id(token)));
            serializationOwners058.AddRange(((string[])AccessTools.Field(T("MutationMind01457"),"OwnTokens").GetValue(null)).Select(t=>(BlueprintScriptableObject)c.Load(Id(t))));
        }
        var bridgeCache=PsychicCache057(parent);Check(!(bool)Call(T("PsychicDisciplineBridge01458"),"Register",bridgeCache),"optional bridge before prerequisites publishes nothing");Register(bridgeCache,"MagaambyanTelepath01447");Register(bridgeCache,"PsychicClassCompletion01458");var originalPool=A<BlueprintFeatureSelection>(bridgeCache,"MagaambyanDisciplineSelection01447");var originals=originalPool.AllFeatures.ToArray();
        Check((bool)Call(T("PsychicDisciplineBridge01458"),"Register",bridgeCache),"actual optional bridge registers after both producers publish");var added=originalPool.AllFeatures.Except(originals).OfType<BlueprintProgression>().ToArray();
        Check(added.Length==3&&originals.All(v=>originalPool.AllFeatures.Contains(v)),"Magaambyan actual parent retains all prior choices and exactly three aliases");foreach(var alias in added)Check(alias.LevelEntries.SelectMany(e=>e.Features).All(f=>!f.GetComponents<AddKnownSpell>().Any())&&alias.LevelEntries.Any(e=>e.Level==5)&&alias.LevelEntries.Any(e=>e.Level==13),"actual Magaambyan alias retains powers and excludes discipline bonus spells: "+alias.name);
        Check((bool)Call(T("PsychicDisciplineBridge01458"),"Register",bridgeCache)&&originalPool.AllFeatures.Except(originals).Count()==3,"actual optional bridge repeats with exact links and identities");Register(bridgeCache,"MagaambyanTelepath01447");foreach(var alias in added)Record058(alias);
        var reports058=new System.Collections.Generic.List<Tuple<string,bool,Exception>>();
        Call(T("BlueprintRegistration"),"RegisterOptional",bridgeCache,new Action<string,bool,Exception>((name,ok,error)=>reports058.Add(Tuple.Create(name,ok,error))));
        string bridgeName058=(string)AccessTools.Field(T("PsychicDisciplineBridge01458"),"ModuleName").GetValue(null);
        Check(reports058.Count(v=>v.Item1==bridgeName058&&v.Item2&&v.Item3==null)==1,"actual RegisterOptional caller reports exactly one successful058 discipline bridge");
        Check(added.All(v=>originalPool.AllFeatures.Count(f=>f.AssetGuid==v.AssetGuid)==1),"actual RegisterOptional caller preserves each058 alias exactly once");
        serializationOwners058.AddRange(owned058.Values.OfType<BlueprintScriptableObject>());NewTypeRoundtrip058(serializationOwners058);
        PsychicExpansionTests.Inventory["psychicRegistration01458Contracts"]=new JObject{["passed"]=true,["assertions"]=checks,["coverage"]=new JArray(Coverage),["ownedBlueprints"]=new JArray(owned058.OrderBy(v=>v.Key.ToString()).Select(v=>new JObject{["id"]=v.Key.ToString(),["name"]=v.Value.name,["type"]=v.Value.GetType().FullName})),["gameLaunchTested"]=false,["boundary"]="Real native cache insertion/rollback/retry, original producer strict fingerprints, both selection arrays, both Mutation/Core orders and actual Magaambyan bridge publication. Complete Unity level-up and save transport remain scene boundaries."};
    }
    static void NewTypeRoundtrip058(System.Collections.Generic.IEnumerable<BlueprintScriptableObject> owners)
    {
        var names=new[]{"OptionalDisciplineSpell01458","RestorePainfulReminder01458","AgonizingTargets01458","SelectAgonizingTarget01458","PsychicDamageLink01458","LiveOnCasterLevel01458","WarpedBrain01458","HallucinogenicAura01458","DarkHalf01458","DarkHalfReturn01458","DarkHalfCaster01458","DualAmplification01458","MimicMetamagic01458","SynapticShock01458","SelectSynapticTarget01458","SpaceRendingCaster01458","EndSpaceRending01458","PhysicalMutationDuration01458","PhysicalMutationController01458","PhysicalMutationRestriction01458","StartPhysicalMutation01458"};
        var expected=new System.Collections.Generic.HashSet<Type>(names.Select(T));var done=new System.Collections.Generic.HashSet<Type>();var json=Kingmaker.Blueprints.JsonSystem.Json.Serializer;
        foreach(var owner in owners.Distinct()){
            var graph=(System.Collections.Generic.IEnumerable<object>)Call(T("PsychicRemainingDisciplines01458"),"Graph",owner.ComponentsArray,null);
            foreach(var original in graph.Where(v=>expected.Contains(v.GetType())&&!done.Contains(v.GetType())).ToArray()){
                var savedNodes=owner.ElementsArray.ToArray();var before=Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead;
                try{
                    Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=new BlueprintJsonWrapper(owner);var declared=original is BlueprintComponent?typeof(BlueprintComponent):typeof(Kingmaker.ElementsSystem.GameAction);string text;
                    using(var writer=new StringWriter())using(var output=new Newtonsoft.Json.JsonTextWriter(writer)){json.Serialize(output,original,declared);text=writer.ToString();}
                    object restored;using(var reader=new Newtonsoft.Json.JsonTextReader(new StringReader(text)))restored=json.Deserialize(reader,declared);
                    Check(restored!=null&&restored.GetType()==original.GetType(),"actual native polymorphic serializer restores new058 component/action TypeId: "+original.GetType().Name);
                    if(restored is BlueprintComponent component)Check(ReferenceEquals(component.OwnerBlueprint,owner),"actual native new058 component JSON restores exact blueprint owner: "+component.GetType().Name);done.Add(original.GetType());
                }finally{owner.ElementsArray.Clear();owner.ElementsArray.AddRange(savedNodes);Set(owner,"m_AllElements",owner.ElementsArray);Kingmaker.Blueprints.JsonSystem.Json.BlueprintBeingRead=before;}
            }
        }
        Check(expected.SetEquals(done)&&done.Count==21,"all twenty-one new058 component/action TypeIds execute actual native polymorphic JSON roundtrip");
    }
}
