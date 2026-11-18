using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Publication is independent of earlier batches. A normal return from a
    // builder is insufficient: every staged identity and link must be present.
    internal static class ArcaneBatch01446
    {
        sealed class Receipt { internal BlueprintScriptableObject[] Assets; internal string[] Shapes; internal Action Verify; internal Dictionary<BlueprintFeatureSelection,BlueprintFeatureReference[][]> ProviderLists; internal Dictionary<BlueprintFeatureSelection,BlueprintComponent[]> ProviderComponents; }
        static readonly ConditionalWeakTable<BlueprintsCache,Dictionary<string,Receipt>> Receipts = new ConditionalWeakTable<BlueprintsCache,Dictionary<string,Receipt>>();
        internal static bool Existing(BlueprintsCache cache,string module) {
            if(!Receipts.GetValue(cache,_=>new Dictionary<string,Receipt>()).TryGetValue(module,out var r))return false;
            for(int i=0;i<r.Assets.Length;i++) {
                var asset=r.Assets[i];var actual=ArchetypeBatch01445.Definition(asset);
                if(!ReferenceEquals(cache.Load(asset.AssetGuid),asset)||(BlueprintRegistration.StrictPublishedGraphChecks&&actual!=r.Shapes[i]&&!ProviderOnlyAppend(cache,r,asset,r.Shapes[i]))) {
                    int at=0;while(at<actual.Length&&at<r.Shapes[i].Length&&actual[at]==r.Shapes[i][at])at++;
                    string Slice(string value)=>value.Substring(Math.Max(0,at-60),Math.Min(value.Length-Math.Max(0,at-60),220));
                    throw new InvalidOperationException("01446 published resource changed: "+asset.name+"; firstDifference="+at+" expected="+Slice(r.Shapes[i])+" actual="+Slice(actual)+"; nativePrerequisites="+BloodlineProvider01453.Describe(asset));
                }
            }
            r.Verify();return true;
        }
        internal static void Publish(string module,PsychicAssets f,Action connect,Action undo,Action verify) {
            var assets=f.Assets.Values.ToArray();
            foreach(var bp in assets)if(f.Cache.Load(bp.AssetGuid)!=null)throw new InvalidOperationException("01446 GUID occupied: "+bp.AssetGuid);
            try {
                foreach(var bp in assets)PsychicRepairComponents.UniqueNames(bp);
                f.Enable();foreach(var bp in assets)f.Cache.AddCachedBlueprint(bp.AssetGuid,bp);
                foreach(var bp in assets)if(!ReferenceEquals(f.Cache.Load(bp.AssetGuid),bp))throw new InvalidOperationException("01446 publication missing: "+bp.name);
                connect();verify();
                var r=new Receipt{Assets=assets,Shapes=assets.Select(ArchetypeBatch01445.Definition).ToArray(),Verify=verify,ProviderLists=BloodlineReceipt(module)?ProviderSnapshot(assets):null,ProviderComponents=BloodlineReceipt(module)?ComponentSnapshot(assets):null};
                Receipts.GetValue(f.Cache,_=>new Dictionary<string,Receipt>()).Add(module,r);
            } catch {undo();foreach(var bp in assets)if(ReferenceEquals(f.Cache.Load(bp.AssetGuid),bp))f.Cache.RemoveCachedBlueprint(bp.AssetGuid);throw;}
        }
        internal static void Icon(Kingmaker.Blueprints.Facts.BlueprintUnitFact target,Kingmaker.Blueprints.Facts.BlueprintUnitFact donor)=>Set(target,"m_Icon",Get(donor,"m_Icon"));
        static bool ProviderSelector(BlueprintFeatureSelection s)=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoice",StringComparison.Ordinal);
        static bool BloodlineReceipt(string module)=>module==BloodlineMutations01446.ModuleName||module.StartsWith(BloodlineExternal01450.Prefix,StringComparison.Ordinal);
        static Dictionary<BlueprintFeatureSelection,BlueprintFeatureReference[][]> ProviderSnapshot(BlueprintScriptableObject[] assets)=>assets.OfType<BlueprintFeatureSelection>().Where(ProviderSelector).ToDictionary(s=>s,s=>new[]{((BlueprintFeatureReference[])Get(s,"m_Features"))?.ToArray(),((BlueprintFeatureReference[])Get(s,"m_AllFeatures"))?.ToArray()});
        static Dictionary<BlueprintFeatureSelection,BlueprintComponent[]> ComponentSnapshot(BlueprintScriptableObject[] assets)=>assets.OfType<BlueprintFeatureSelection>().Where(ProviderSelector).ToDictionary(s=>s,s=>s.ComponentsArray.ToArray());
        static bool ProviderOnlyAppend(BlueprintsCache cache,Receipt receipt,BlueprintScriptableObject bp,string expected)
        {
            if(!(bp is BlueprintFeatureSelection selector)||receipt.ProviderLists==null||!receipt.ProviderLists.TryGetValue(selector,out var prior))return false;
            bool bloodrager=selector.name.StartsWith("CotWWrathPsychicBloodlinePowerChoiceBloodrager",StringComparison.Ordinal);
            var allowed=new HashSet<BlueprintGuid>(BloodlineCompatibility01447.Options(cache,bloodrager).Select(f=>f.AssetGuid));
            var fields=new[]{"m_Features","m_AllFeatures"};
            for(int i=0;i<fields.Length;i++){
                var now=(BlueprintFeatureReference[])Get(selector,fields[i]);var old=prior[i];
                if(now==null){if(old!=null)return false;continue;}
                if(now.Any(r=>r==null)||(old?.Any(r=>r==null)??false))return false;
                int length=old?.Length??0;
                if(now.Length<length)return false;
                // External selection sorting can reorder the same authored
                // options. Preserve every original occurrence; only verified
                // provider options may have additional occurrences.
                var counts=now.GroupBy(r=>r.Guid).ToDictionary(g=>g.Key,g=>g.Count());
                foreach(var group in (old??Array.Empty<BlueprintFeatureReference>()).GroupBy(r=>r.Guid)) {
                    if(!counts.TryGetValue(group.Key,out int count)||count<group.Count())return false;
                    counts[group.Key]-=group.Count();
                }
                if(counts.Any(p=>p.Value!=0&&!allowed.Contains(p.Key)))return false;
            }
            // Compare a non-published shallow view, restoring only the two
            // audited lists. Every other component/field remains fingerprinted.
            var view=(BlueprintFeatureSelection)HarmonyLib.AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(selector,null);
            for(int i=0;i<fields.Length;i++)Set(view,fields[i],prior[i]);
            if(receipt.ProviderComponents.TryGetValue(selector,out var components)&&BloodlineProvider01453.LateRestriction(cache,selector,components))view.ComponentsArray=components;
            return ArchetypeBatch01445.Definition(view)==expected;
        }
        // ECM can publish after the initial cache callback. Validate the entire
        // old receipt before adding verified provider-owned leaf options only
        // to our own selectors. Both lists and receipt roll back on failure.
        internal static void ReconcileBloodlineProviderOptions(BlueprintsCache cache)
        {
            ReconcileReceipts(cache,false);
        }
        internal static void ReconcileSorcererProviderOptions01452(BlueprintsCache cache)
        {
            ReconcileReceipts(cache,true);
        }
        internal static void ValidateSorcererTarget01453(BlueprintsCache cache,BlueprintProgression target,bool requireReceipt)
        {
            if(!Existing(cache,BloodlineMutations01446.ModuleName))throw new InvalidOperationException("01453 shared mutation receipt missing.");
            if(!BloodlineExternal01450.Rows.Any(r=>r[1]=="Sorcerer"&&r[0]==target.AssetGuid.ToString()))return;
            if(!Existing(cache,BloodlineExternal01450.Prefix+target.AssetGuid)&&requireReceipt)throw new InvalidOperationException("01453 target mutation receipt missing: "+target.AssetGuid);
        }
        static void ReconcileReceipts(BlueprintsCache cache,bool sorcererOnly) {
            var errors=new List<Exception>();
            foreach(var module in Receipts.GetValue(cache,_=>new Dictionary<string,Receipt>()).Keys.Where(BloodlineReceipt).ToArray())try{ReconcileProviderReceipt(cache,module,sorcererOnly);}catch(Exception error){errors.Add(error);}
            // Retain the historical single-failure exception contract, while
            // still attempting every independent receipt before reporting it.
            if(errors.Count==1)ExceptionDispatchInfo.Capture(errors[0]).Throw();
            if(errors.Count!=0)throw new AggregateException("01453 provider receipt failures; independent receipts retained.",errors);
        }
        static void ReconcileProviderReceipt(BlueprintsCache cache,string module,bool sorcererOnly=false)
        {
            if(!Receipts.GetValue(cache,_=>new Dictionary<string,Receipt>()).TryGetValue(module,out var receipt))return;
            Existing(cache,module);
            var selectors=receipt.Assets.OfType<BlueprintFeatureSelection>().Where(ProviderSelector).Where(s=>!sorcererOnly||s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoiceSorcerer",StringComparison.Ordinal)).ToArray();
            if(selectors.Length==0)return;
            var old=selectors.ToDictionary(s=>s,s=>new[]{Get(s,"m_Features"),Get(s,"m_AllFeatures")});var shapes=receipt.Shapes;var priorLists=receipt.ProviderLists;var priorComponents=receipt.ProviderComponents;
            var oldComponents=selectors.ToDictionary(s=>s,s=>s.ComponentsArray);
            try {
                foreach(var selector in selectors) {
                    BloodlineProvider01453.MirrorInitialRestriction(cache,selector);
                    bool bloodrager=selector.name.StartsWith("CotWWrathPsychicBloodlinePowerChoiceBloodrager",StringComparison.Ordinal);
                    var options=BloodlineCompatibility01447.Options(cache,bloodrager);
                    if(options.Length!=0)foreach(string field in new[]{"m_Features","m_AllFeatures"}){
                        var ids=new HashSet<BlueprintGuid>();var allowed=new HashSet<BlueprintGuid>(options.Select(f=>f.AssetGuid));
                        var refs=(((BlueprintFeatureReference[])Get(selector,field))??Array.Empty<BlueprintFeatureReference>()).Where(r=>r==null||!allowed.Contains(r.Guid)||ids.Add(r.Guid)).ToList();
                        foreach(var option in options)if(!refs.Any(r=>r?.Guid==option.AssetGuid))refs.Add(Ref<BlueprintFeatureReference>(option));
                        var original=(BlueprintFeatureReference[])Get(selector,field);
                        if(original==null||!original.SequenceEqual(refs))Set(selector,field,refs.ToArray());
                    }
                }
                receipt.Verify();receipt.Shapes=receipt.Assets.Select(ArchetypeBatch01445.Definition).ToArray();receipt.ProviderLists=ProviderSnapshot(receipt.Assets);receipt.ProviderComponents=ComponentSnapshot(receipt.Assets);
            }catch{foreach(var selector in selectors){Set(selector,"m_Features",old[selector][0]);Set(selector,"m_AllFeatures",old[selector][1]);selector.ComponentsArray=oldComponents[selector];}receipt.Shapes=shapes;receipt.ProviderLists=priorLists;receipt.ProviderComponents=priorComponents;throw;}
        }
    }
}
