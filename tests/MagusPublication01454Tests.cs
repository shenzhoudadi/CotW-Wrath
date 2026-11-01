using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Blueprints.Classes.Prerequisites;

internal static partial class MagusBatch01454Tests
{
    static BlueprintGuid publicationId;
    static int publicationFault;
    static bool hurricaneConnectFault;
    static void PublicationConnect(string __0,ref Action __2)
    {if(!hurricaneConnectFault||__0!="Infusion: Whip Hurricane")return;var original=__2;__2=()=>{original();hurricaneConnectFault=false;throw new InvalidOperationException("054 injected post-connect Hurricane failure");};}
    static void PublicationWrite(BlueprintGuid guid)
    {if(publicationFault==1&&guid==publicationId){publicationFault=0;throw new InvalidOperationException("054 injected actual-cache post-write failure");}}
    static void PublicationVerify(BlueprintCharacterClass cls,BlueprintArchetype archetype)
    {if(publicationFault==2&&archetype.AssetGuid==publicationId){publicationFault=0;throw new InvalidOperationException("054 injected actual-parent post-connect failure");}}
    static bool Refused(Action action)
    {try{action();return false;}catch(Exception error){while(error is System.Reflection.TargetInvocationException)error=error.InnerException;if(error is InvalidOperationException)return true;throw;}}
    static void Publication(BlueprintsCache parent,Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(BlueprintsCache),nameof(BlueprintsCache.AddCachedBlueprint)),postfix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PublicationWrite)));
        h.Patch(AccessTools.Method(T("MagusBatch01454"),"VerifyArchetype"),prefix:new HarmonyMethod(typeof(MagusBatch01454Tests),nameof(PublicationVerify)));
        foreach(bool reverse in new[]{false,true}){
            var c=Fresh(parent);var cls=N<BlueprintCharacterClass>(c,"45a4607686d96a1498891b3286121780");var old=cls.Archetypes.ToArray();var groups=cls.Progression.UIGroups.ToArray();
            foreach(var module in reverse?new[]{"Skirnir01454","Mindblade01454"}:new[]{"Mindblade01454","Skirnir01454"}){
                var before=Own(c);var archetypes=cls.Archetypes.ToArray();var ui=cls.Progression.UIGroups.ToArray();string root=module=="Mindblade01454"?"MindbladeArchetype01454":"SkirnirArchetype01454";
                publicationId=Id(root);publicationFault=module=="Mindblade01454"?1:2;
                Check(Refused(()=>Register(c,module))&&publicationFault==0,"real module caller rejects injected cache/connect failure: "+module);
                Check(before.SequenceEqual(Own(c))&&archetypes.SequenceEqual(cls.Archetypes)&&ui.SequenceEqual(cls.Progression.UIGroups),"failed real publication restores own assets and exact native class references: "+module);
                Register(c,module);Check(A<BlueprintArchetype>(c,root)!=null,"actual retry succeeds after complete rollback: "+module);
                var owned=Loaded(c).OfType<BlueprintAbility>().First(a=>a.name!=null&&a.name.Contains(module=="Mindblade01454"?"MindbladeManifestDagger01454":"SkirnirShieldEnhancementAbility01454"));bool target=owned.CanTargetPoint;owned.CanTargetPoint=!target;
                Check(Refused(()=>Register(c,module)),"full new-resource receipt refuses unknown targeting mutation: "+module);owned.CanTargetPoint=target;Register(c,module);
                var original=A<BlueprintArchetype>(c,root);var foreign=new BlueprintArchetype{AssetGuid=original.AssetGuid,name="054ForeignIdentityControl"};c.RemoveCachedBlueprint(original.AssetGuid);c.AddCachedBlueprint(original.AssetGuid,foreign);
                Check(Refused(()=>Register(c,module))&&ReferenceEquals(c.Load(original.AssetGuid),foreign),"foreign occupied identity is refused and never overwritten: "+module);c.RemoveCachedBlueprint(original.AssetGuid);c.AddCachedBlueprint(original.AssetGuid,original);Register(c,module);
            }
            Check(old.All(a=>cls.Archetypes.Contains(a))&&cls.Archetypes.Count()==old.Length+2&&groups.All(g=>cls.Progression.UIGroups.Contains(g)),"both actual registration orders preserve every original archetype/UI group");
            foreach(var id in new[]{"9a399ad2c35d4ff449789fcd7f02943b","f10f1ddf993f6224b8b744ac8c89b82d","2803ed6f49730cf4a9616880e0979269"}){
                var source=N<BlueprintFeature>(c,id);var exclusions=source.GetComponents<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteNoArchetype>();Check(exclusions.Count(p=>p.Archetype==A<BlueprintArchetype>(c,"MindbladeArchetype01454"))==1&&exclusions.Count(p=>p.Archetype==A<BlueprintArchetype>(c,"SkirnirArchetype01454"))==1&&exclusions.All(p=>p.OwnerBlueprint==source),"native prestige source retains both independent exact exclusions with correct owners");
            }
        }
        var optional=Fresh(parent);Register(optional,"Mindblade01454");Register(optional,"Skirnir01454");var reports=new System.Collections.Generic.List<Tuple<string,bool,Exception>>();Action<string,bool,Exception> report=(n,ok,e)=>reports.Add(Tuple.Create(n,ok,e));
        Call(T("BlueprintRegistration"),"RegisterOptional",optional,report);Check(!reports.Any(r=>r.Item1=="Mindblade: optional Spell Blending integration"||r.Item1=="Infusion: Whip Hurricane"),"actual optional dispatcher does not report missing providers as registered");
        // Controlled publication of the current official TTT parent/member IDs.
        // This exercises CotW's actual integration caller, not TTT startup.
        var providerSelection=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse("67d5b18ec9344c6da4a6446df6855ad6"),name="TTTSpellBlendingSelectionInput",Group=FeatureGroup.MagusArcana,Groups=new[]{FeatureGroup.MagusArcana},ComponentsArray=Array.Empty<BlueprintComponent>()};
        var leaves=new[]{"c07bac92bcc842948baa0c1aa5d51406","c26af0444449473d80a02681ab34a387"}.Select(s=>new BlueprintFeature{AssetGuid=BlueprintGuid.Parse(s),name="TTTSpellBlendingLeafInput",ComponentsArray=Array.Empty<BlueprintComponent>()}).ToArray();foreach(var leaf in leaves){leaf.OnEnable();optional.AddCachedBlueprint(leaf.AssetGuid,leaf);}var refs=leaves.Select(BlueprintReferenceBase.CreateTyped<BlueprintFeatureReference>).ToArray();Set(providerSelection,"m_Features",refs);Set(providerSelection,"m_AllFeatures",refs);providerSelection.OnEnable();optional.AddCachedBlueprint(providerSelection.AssetGuid,providerSelection);
        var originals=providerSelection.AllFeatures.ToArray();reports.Clear();Call(T("BlueprintRegistration"),"RegisterOptional",optional,report);Check(reports.Count(r=>r.Item1=="Mindblade: optional Spell Blending integration"&&r.Item2)==1&&providerSelection.AllFeatures.SequenceEqual(originals),"actual late optional caller restricts only provider parent and retains both original leaves");
        var exclusion=providerSelection.GetComponents<PrerequisiteNoArchetype>().Single();var mind=EquipmentUnit(optional,19);mind.Progression.Classes[0].AddArchetype(A<BlueprintArchetype>(optional,"MindbladeArchetype01454"));var ordinary=EquipmentUnit(optional,19);Check(!exclusion.Check(null,mind.Descriptor,null)&&exclusion.Check(null,ordinary.Descriptor,null),"actual native external-parent prerequisite rejects Mindblade and retains ordinary Magus");
        var components=providerSelection.ComponentsArray;Call(T("BlueprintRegistration"),"RegisterOptional",optional,report);Check(ReferenceEquals(components,providerSelection.ComponentsArray),"repeated optional callback does not duplicate provider restriction");exclusion.Group=Prerequisite.GroupType.Any;reports.Clear();Call(T("BlueprintRegistration"),"RegisterOptional",optional,report);Check(reports.Any(r=>r.Item1=="Mindblade: optional Spell Blending integration"&&!r.Item2)&&reports.Any(r=>r.Item1=="Magus archetype: Skirnir"&&r.Item2)&&reports.Any(r=>r.Item1=="Magus archetype: Mindblade"&&r.Item2),"actual optional dispatcher isolates malformed provider row and completes both core module checks");exclusion.Group=Prerequisite.GroupType.All;
    }
}
