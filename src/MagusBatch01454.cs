// Independent publication for the three bounded 0.14.54 additions. Earlier
// batches and their receipt/compatibility policies remain unchanged.
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class MagusBatch01454
    {
        // Fixed module identities are independent of a particular cache/unit.
        // Keep crypto and hex formatting off UI, cost and equipment hot paths.
        static readonly ConcurrentDictionary<string,BlueprintGuid> Identities=new ConcurrentDictionary<string,BlueprintGuid>(StringComparer.Ordinal);
        internal static BlueprintGuid Identity(string token)=>token==null?PsychicAssets.Id(token):Identities.GetOrAdd(token,PsychicAssets.Id);
        internal const string MagusId="45a4607686d96a1498891b3286121780",PoolId="effc3e386331f864e9e06d19dc218b37";
        sealed class Receipt {internal BlueprintScriptableObject[] Assets;internal string[] Shapes;internal Action Verify;}
        static readonly ConditionalWeakTable<BlueprintsCache,Dictionary<string,Receipt>> Receipts=new ConditionalWeakTable<BlueprintsCache,Dictionary<string,Receipt>>();
        // Reuse the existing deterministic value serializer without changing
        // any old shape definitions or accepting new bloodline receipt edits.
        static readonly MethodInfo Authored=AccessTools.Method(typeof(ArchetypeBatch01445),"Authored");
        sealed class ReferenceIdentity:IEqualityComparer<object>
        {internal static readonly ReferenceIdentity Instance=new ReferenceIdentity();bool IEqualityComparer<object>.Equals(object a,object b)=>ReferenceEquals(a,b);int IEqualityComparer<object>.GetHashCode(object value)=>RuntimeHelpers.GetHashCode(value);}
        static string Values(object value)=>(string)Authored.Invoke(null,new object[]{value,new HashSet<object>(ReferenceIdentity.Instance)});
        internal static string Definition(BlueprintScriptableObject bp)
        {
            var extra=new List<object>();
            if(bp!=null) {
                // These newly authored resources have an independent receipt.
                // Inspect every serialized/public authoring field, including
                // targeting, actionbar, spellbook and feature visibility;
                // native lazy caches are explicitly excluded, not all private
                // fields or unknown mechanical components.
                for(var t=bp.GetType();t!=null&&t!=typeof(BlueprintScriptableObject);t=t.BaseType)
                    foreach(var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(f=>!f.IsStatic).OrderBy(f=>f.Name)) {
                        if(f.Name.StartsWith("m_Cached",StringComparison.Ordinal))continue;
                        bool serialized=f.IsPublic||f.GetCustomAttributes(false).Any(a=>a.GetType().FullName=="UnityEngine.SerializeField"||a.GetType().FullName=="Newtonsoft.Json.JsonPropertyAttribute");
                        if(!serialized)continue;
                        var value=f.GetValue(bp);if(value is LocalizedString text)value=text.Key;
                        extra.Add(new object[]{t.FullName,f.Name,value});
                    }
                return Values(new object[]{bp.name,bp.ComponentsArray,extra.ToArray()});
            }
            if(bp is BlueprintArchetype a)extra.Add(new object[]{a.ReplaceSpellbook,a.IsArcaneCaster,a.ReplaceStartingEquipment,a.StartingItems});
            return ArchetypeBatch01445.Definition(bp)+Values(extra.ToArray());
        }
        internal static bool Existing(BlueprintsCache cache,string module)
        {
            if(!Receipts.GetValue(cache,_=>new Dictionary<string,Receipt>()).TryGetValue(module,out var receipt))return false;
            for(int i=0;i<receipt.Assets.Length;i++){
                var asset=receipt.Assets[i];if(!ReferenceEquals(cache.Load(asset.AssetGuid),asset))throw new InvalidOperationException("01454 published resource identity changed: "+asset.name);
                var actual=Definition(asset);var expected=receipt.Shapes[i];
                if(actual!=expected){int at=0;while(at<actual.Length&&at<expected.Length&&actual[at]==expected[at])at++;int start=Math.Max(0,at-90);throw new InvalidOperationException("01454 published resource changed: "+asset.name+"; firstDifference="+at+"; expected="+expected.Substring(start,Math.Min(240,expected.Length-start))+"; actual="+actual.Substring(start,Math.Min(240,actual.Length-start)));}
            }
            receipt.Verify();return true;
        }
        internal static void Publish(string module,PsychicAssets f,Action connect,Action undo,Action verify)
        {
            var assets=f.Assets.Values.ToArray();
            if(assets.Length==0)throw new InvalidOperationException("01454 empty publication: "+module);
            foreach(var asset in assets)if(f.Cache.Load(asset.AssetGuid)!=null)throw new InvalidOperationException("01454 GUID occupied: "+asset.AssetGuid);
            try{
                foreach(var asset in assets){
                    PsychicRepairComponents.UniqueNames(asset);
                    if(asset is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility ability)SpellBatch01447.ResetAbilityCaches(ability);
                    if(asset is Kingmaker.Blueprints.Items.BlueprintItem)Set(asset,"m_CachedEnchantments",null);
                    var elements=(System.Collections.Generic.IEnumerable<Kingmaker.ElementsSystem.Element>)AccessTools.Method(typeof(ArchetypeBatch01445),"Elements").Invoke(null,new object[]{asset.ComponentsArray,new HashSet<object>()});
                    Set(asset,"m_AllElements",elements.ToList());
                }
                f.Enable();foreach(var asset in assets)f.Cache.AddCachedBlueprint(asset.AssetGuid,asset);
                foreach(var asset in assets)if(!ReferenceEquals(f.Cache.Load(asset.AssetGuid),asset))throw new InvalidOperationException("01454 resource publication failed: "+asset.name);
                connect();verify();
                Receipts.GetValue(f.Cache,_=>new Dictionary<string,Receipt>()).Add(module,new Receipt{Assets=assets,Shapes=assets.Select(Definition).ToArray(),Verify=verify});
            }catch{
                undo();foreach(var asset in assets)if(ReferenceEquals(f.Cache.Load(asset.AssetGuid),asset))f.Cache.RemoveCachedBlueprint(asset.AssetGuid);throw;
            }
        }
        internal static BlueprintFeatureReference[] Append(BlueprintFeatureReference[] old,BlueprintFeature value)
        {
            old=old??Array.Empty<BlueprintFeatureReference>();var same=old.Where(r=>r?.Guid==value.AssetGuid).ToArray();
            if(same.Length==0)return old.Concat(new[]{Ref<BlueprintFeatureReference>(value)}).ToArray();
            if(same.Length!=1||!ReferenceEquals(same[0].Get(),value))throw new InvalidOperationException("01454 choice identity collision: "+value.AssetGuid);
            return old;
        }
        internal static void ConnectArchetype(BlueprintCharacterClass cls,BlueprintArchetype archetype,UIGroup group)
        {
            var prior=(BlueprintArchetypeReference[])Get(cls,"m_Archetypes")??Array.Empty<BlueprintArchetypeReference>();var same=prior.Where(r=>r?.Guid==archetype.AssetGuid).ToArray();
            if(same.Length==0)Set(cls,"m_Archetypes",prior.Concat(new[]{Ref<BlueprintArchetypeReference>(archetype)}).ToArray());
            else if(same.Length!=1||!ReferenceEquals(same[0].Get(),archetype))throw new InvalidOperationException("01454 archetype identity collision.");
            var groups=cls.Progression.UIGroups??Array.Empty<UIGroup>();if(!groups.Contains(group))cls.Progression.UIGroups=groups.Concat(new[]{group}).ToArray();
        }
        internal static void VerifyArchetype(BlueprintCharacterClass cls,BlueprintArchetype archetype,UIGroup group)
        {
            var refs=(BlueprintArchetypeReference[])Get(cls,"m_Archetypes");if(refs.Count(r=>r?.Guid==archetype.AssetGuid&&ReferenceEquals(r.Get(),archetype))!=1||cls.Progression.UIGroups.Count(g=>ReferenceEquals(g,group))!=1||!ReferenceEquals(archetype.GetParentClass(),cls))throw new InvalidOperationException("01454 archetype attachment incomplete.");
        }
        internal static UIGroup Group(params BlueprintFeatureBase[] features){var group=new UIGroup();Set(group,"m_Features",features.Select(Ref<BlueprintFeatureBaseReference>).ToList());return group;}
        internal static void Determinators(BlueprintCharacterClass cls,params BlueprintFeatureBase[] features)
        {
            var refs=(BlueprintFeatureBaseReference[])Get(cls.Progression,"m_UIDeterminatorsGroup")??Array.Empty<BlueprintFeatureBaseReference>();
            foreach(var feature in features){var matches=refs.Where(r=>r?.Guid==feature.AssetGuid).ToArray();if(matches.Length==0)refs=refs.Concat(new[]{Ref<BlueprintFeatureBaseReference>(feature)}).ToArray();else if(matches.Length!=1||!ReferenceEquals(matches[0].Get(),feature))throw new InvalidOperationException("01454 class header identity changed: "+feature.name);}
            Set(cls.Progression,"m_UIDeterminatorsGroup",refs);
        }
        internal static void VerifyDeterminators(BlueprintCharacterClass cls,params BlueprintFeatureBase[] features)
        {foreach(var f in features)if(cls.Progression.UIDeterminatorsGroup.Count(v=>ReferenceEquals(v,f))!=1)throw new InvalidOperationException("01454 class header attachment missing: "+f.name);}
        internal static Action CaptureClass(BlueprintCharacterClass cls)
        {
            var archetypes=Get(cls,"m_Archetypes");var groups=cls.Progression.UIGroups;var headers=Get(cls.Progression,"m_UIDeterminatorsGroup");
            return ()=>{Set(cls,"m_Archetypes",archetypes);cls.Progression.UIGroups=groups;Set(cls.Progression,"m_UIDeterminatorsGroup",headers);};
        }
    }
}
