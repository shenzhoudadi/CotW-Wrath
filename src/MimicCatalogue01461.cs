using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.PsychicRemainingDisciplines01458;

namespace CallOfTheWild.Wrath.Prototype
{
    // Immutable published graphs remain fingerprinted. A late provider publishes
    // only additional assets; the native selection getters receive a cached view.
    // No shared feat pool, existing component or metamagic provider is rewritten.
    internal static class MimicCatalogue01461
    {
        internal const string RootToken="MimicMetamagicFeature01461";
        const string MimicName="Mimic Metamagic";
        // One composed label per metamagic, shared by its feature, buff, toggle
        // and first-pick selector. Key and source survive a rebuild so the text
        // can be recomputed once the localization pack is installed.
        sealed class Composed{internal string Key;internal BlueprintFeature Source;}
        static List<Composed> ComposedLabels=new List<Composed>();
        // "<translated Mimic Metamagic><separator><the name the owning mod gives
        // that metamagic>" so nobody has to translate the pairs by hand. The
        // prefix comes from the optional QL source, never from a frozen string,
        // and the suffix is read through the live pack, which is why the label is
        // also recomputed in ApplyLabels().
        internal static void ApplyLabels()
        {
            var labels=ComposedLabels;if(labels.Count==0)return;
            if(LocalizationManager.CurrentPack==null)return;
            foreach(var item in labels)Override(item.Key,Compose(item.Source));
        }
        static string Compose(BlueprintFeature feat)
        {
            string prefix=QlTexts01460.Resolve(Key(RootToken,"name"),MimicName),suffix=Native(feat);
            if(string.IsNullOrWhiteSpace(suffix))return prefix;
            return prefix+(LocalizationManager.CurrentPack?.Locale==Locale.zhCN?"：":": ")+suffix;
        }
        // A provider may register its feat before the pack exists, so the pack is
        // the authority and the resolved property is only the last resort.
        static string Native(BlueprintFeature feat)
        {
            var pack=LocalizationManager.CurrentPack;
            if(pack!=null&&Get(feat,"m_DisplayName") is LocalizedString label&&!string.IsNullOrEmpty(label.Key))
            {
                string text=pack.GetText(label.Key,false);
                if(!string.IsNullOrWhiteSpace(text))return text;
            }
            return feat.Name;
        }
        sealed class State
        {
            internal BlueprintsCache Cache;internal BlueprintCharacterClass Class;internal BlueprintFeature[] Stock;
            internal Dictionary<string,BlueprintScriptableObject> Assets=new Dictionary<string,BlueprintScriptableObject>();
            internal BlueprintFeature[] Leaves;internal BlueprintFeatureSelection[] Roots;internal BlueprintFeatureSelection[] Pools;
            internal readonly Dictionary<BlueprintFeatureSelection,BlueprintFeatureReference[]> Views=new Dictionary<BlueprintFeatureSelection,BlueprintFeatureReference[]>();
            internal readonly List<string> Modules=new List<string>();
            internal int Generation,Seen,Revision;internal bool Busy,Sealed;
        }
        static readonly ConditionalWeakTable<BlueprintsCache,State> States=new ConditionalWeakTable<BlueprintsCache,State>();
        static readonly ConditionalWeakTable<BlueprintFeatureSelection,State> Owners=new ConditionalWeakTable<BlueprintFeatureSelection,State>();
        static BlueprintFeature[] Snapshot(BlueprintsCache cache)
        {
            var loaded=(IDictionary)Get(cache,"m_LoadedBlueprints");var gate=Get(cache,"m_Lock");
            var blueprint=AccessTools.Field(loaded.GetType().GetGenericArguments()[1],"Blueprint");object[] entries;
            for(int attempt=0;;attempt++)try{lock(gate){entries=new object[loaded.Count];loaded.Values.CopyTo(entries,0);}break;}
                catch(Exception e)when(attempt<2&&(e is ArgumentException||e is InvalidOperationException||e is IndexOutOfRangeException)){}
            return entries.Where(e=>e!=null).Select(e=>blueprint.GetValue(e)).OfType<BlueprintFeature>().ToArray();
        }
        internal static BlueprintFeature[] Build(PsychicAssets f,BlueprintCharacterClass cls,List<BlueprintActivatableAbility> modes,BlueprintFeature[] stock)
        {
            var state=new State{Cache=f.Cache,Class=cls,Stock=stock,Busy=true};
            BuildGraph(state,f,modes);
            States.Remove(f.Cache);States.Add(f.Cache,state);state.Busy=false;
            return state.Roots.Cast<BlueprintFeature>().ToArray();
        }
        static void BuildGraph(State s,PsychicAssets f,List<BlueprintActivatableAbility> modes)
        {
            var sources=s.Stock.Concat(Snapshot(s.Cache)).Where(v=>v!=null).Distinct().ToArray();
            var available=new Dictionary<Metamagic,BlueprintFeature>();
            int Presentation(BlueprintFeature feature)
            {
                int score=0;
                if(!string.IsNullOrWhiteSpace(feature.Name))score+=4;
                if(!string.IsNullOrWhiteSpace(feature.Description))score+=2;
                if(feature.Icon!=null)score+=1;
                return score;
            }
            foreach(var feat in sources.OrderBy(v=>v.AssetGuid.ToString(),StringComparer.Ordinal))
            {
                var components=feat.GetComponents<AddMetamagicFeat>().ToArray();if(components.Length!=1)continue;
                var flag=components[0].Metamagic;uint bit=unchecked((uint)(int)flag);
                if(bit==0||(bit&(bit-1))!=0||flag==Metamagic.Heighten)continue;
                try{int cost=PsychicCompletionAmplifications01458.MetaCost(flag);if(cost>9||cost<=0)continue;}
                catch(Exception error){ModEntry.LogSpellDiagnostic("Mimic skips unreadable metamagic cost for "+feat.AssetGuid+": "+error.Message);continue;}
                // Equal presentation keeps the earlier GUID-ordered feat.
                if(!available.TryGetValue(flag,out var current)||Presentation(feat)>Presentation(current))available[flag]=feat;
            }
            if(available.Count<7)throw new InvalidOperationException("Mimic requires the native metamagic catalogue.");
            var all=new Dictionary<string,BlueprintScriptableObject>(s.Assets);
            T Asset<T>(string token,Func<T> create)where T:BlueprintScriptableObject{
                if(all.TryGetValue(token,out var old))return (T)old;var bp=create();all.Add(token,bp);return bp;
            }
            var labels=new List<Composed>();var names=new Dictionary<BlueprintFeature,LocalizedString>();
            LocalizedString Name(string token,BlueprintFeature feat){var label=Text(token,"label",Compose(feat));labels.Add(new Composed{Key=label.Key,Source=feat});return label;}
            void Dress(BlueprintUnitFact target,BlueprintUnitFact source,LocalizedString name){Set(target,"m_DisplayName",name);Set(target,"m_Description",Get(source,"m_Description"));Icon(target,source);}
            var leaves=new List<BlueprintFeature>();
            foreach(var row in available.OrderBy(p=>unchecked((uint)(int)p.Key)))
            {
                string token="MimicMetamagicOption"+unchecked((uint)(int)row.Key);int cost=PsychicCompletionAmplifications01458.MetaCost(row.Key);
                var name=Name(token+"01461",row.Value);
                var buff=Asset(token+"Buff01458",()=>f.Buff(token+"Buff01458",row.Value.Name,row.Value.Description,new MimicMetamagic01458{Metamagic=row.Key,Price=2*Math.Max(1,cost),LevelCost=cost}));
                Dress(buff,row.Value,name);
                var toggle=Asset(token+"01458Toggle",()=>PsychicCompletionAmplifications01458.Toggle(f,token+"01458",buff));Dress(toggle,row.Value,name);modes?.Add(toggle);
                var leaf=Asset(token+"Feature01458",()=>{var b=f.Feature(token+"Feature01458",row.Value.Name,row.Value.Description,Facts(toggle));if(row.Key==TopplingSpell.Metamagic)b.ComponentsArray=b.ComponentsArray.Concat(new[]{new TopplingSpellDamage()}).ToArray();return b;});
                Dress(leaf,row.Value,name);names.Add(leaf,name);leaves.Add(leaf);
            }
            var roots=new List<BlueprintFeatureSelection>();var views=new Dictionary<BlueprintFeatureSelection,BlueprintFeatureReference[]>();
            // One selector per metamagic. Picking it grants that metamagic and then
            // asks for the second one out of the remaining list, which is how each
            // use of the amplification adds two options. Its own
            // PrerequisiteNoFeature keeps the pair distinct and also stops a
            // metamagic from being taken twice across uses.
            var selectors=new List<BlueprintFeatureSelection>();
            foreach(var leaf in leaves)
            {
                var meta=leaf.GetComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>().Facts.OfType<BlueprintActivatableAbility>().Single().Buff.GetComponent<MimicMetamagic01458>().Metamagic;
                string token="MimicMetamagicFirstFlag"+unchecked((uint)(int)meta)+"01461";
                var selector=Asset(token,()=>{var no=new PrerequisiteNoFeature();Set(no,"m_Feature",Ref<BlueprintFeatureReference>(leaf));var bp=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true,ComponentsArray=new BlueprintComponent[]{no,Facts(leaf)}},token,leaf.Name,leaf.Description);var choices=leaves.Where(v=>v!=leaf).Select(Ref<BlueprintFeatureReference>).ToArray();Set(bp,"m_Features",choices);Set(bp,"m_AllFeatures",choices);return bp;});
                Dress(selector,leaf,names[leaf]);views.Add(selector,leaves.Where(v=>v!=leaf).Select(Ref<BlueprintFeatureReference>).ToArray());selectors.Add(selector);
            }
            // A single repeatable entry replaces the numbered "Mimic Metamagic 1..N"
            // chain. Ranks is what the native Extra Mercy style selections use to
            // stay selectable after the first pick; the pool greys it out once the
            // ranks run out, and every already-owned metamagic stays unavailable
            // because the leaves keep Ranks 1.
            var root=Asset(RootToken,()=>{var prerequisite=new PrerequisiteFeature();var reference=new BlueprintFeatureReference();Set(reference,"deserializedGuid",BlueprintGuid.Parse(PsychicClass.MajorId));Set(prerequisite,"m_Feature",reference);var bp=f.Add(new BlueprintFeatureSelection{Ranks=Math.Max(1,leaves.Count/2),IsClassFeature=true,ComponentsArray=new BlueprintComponent[]{prerequisite}},RootToken,MimicName,D("MimicMetamagicSelection"));Icon(bp,leaves[0]);var choices=selectors.Select(Ref<BlueprintFeatureReference>).ToArray();Set(bp,"m_Features",choices);Set(bp,"m_AllFeatures",choices);return bp;});
            roots.Add(root);views.Add(root,selectors.Select(Ref<BlueprintFeatureReference>).ToArray());
            ComposedLabels=labels;
            s.Assets=all;s.Leaves=leaves.ToArray();s.Roots=roots.ToArray();s.Views.Clear();foreach(var pair in views)s.Views.Add(pair.Key,pair.Value);
        }
        internal static void Seal(BlueprintsCache cache,params BlueprintFeatureSelection[] pools)
        {
            if(!States.TryGetValue(cache,out var s))throw new InvalidOperationException("Mimic staged catalogue is missing.");
            s.Pools=pools;s.Sealed=true;s.Seen=s.Generation;Bind(s);
        }
        static void Bind(State s){foreach(var pool in s.Views.Keys.Concat(s.Pools)){Owners.Remove(pool);Owners.Add(pool,s);}}
        internal static void Changed(BlueprintsCache cache,SimpleBlueprint bp)
        {if(bp is BlueprintFeature&&States.TryGetValue(cache,out var s)&&s.Sealed)Interlocked.Increment(ref s.Generation);}
        static void Refresh(State s)
        {
            if(s.Busy||s.Generation==s.Seen)return;s.Busy=true;int generation=s.Generation;
            try{
                ArcaneBatch01446.Existing(s.Cache,PsychicClassCompletion01458.ModuleName);
                foreach(var module in s.Modules)if(!ArcaneBatch01446.Existing(s.Cache,module))throw new InvalidOperationException("Mimic late receipt missing: "+module);
                // Build a detached draft. Native getters must not expose staged
                // choices while OnEnable/cache callbacks run during publication.
                var draft=new State{Cache=s.Cache,Class=s.Class,Stock=s.Stock,Assets=new Dictionary<string,BlueprintScriptableObject>(s.Assets)};
                var f=new PsychicAssets(s.Cache);BuildGraph(draft,f,null);
                if(f.Assets.Count!=0){string module="Mimic late catalogue 01461 "+(++s.Revision);ArcaneBatch01446.Publish(module,f,()=>{},()=>{},()=>{foreach(var bp in f.Assets.Values)if(!ReferenceEquals(s.Cache.Load(bp.AssetGuid),bp))throw new InvalidOperationException("Mimic late asset missing.");});s.Modules.Add(module);}
                s.Assets=draft.Assets;s.Leaves=draft.Leaves;s.Roots=draft.Roots;s.Views.Clear();foreach(var pair in draft.Views)s.Views.Add(pair.Key,pair.Value);
                Bind(s);s.Seen=generation;
            }catch(Exception error){s.Seen=generation;ModEntry.LogSpellDiagnostic("Optional Mimic catalogue refresh failed; prior choices retained. "+error);}
            finally{s.Busy=false;}
        }
        internal static void View(BlueprintFeatureSelection pool,bool all,ref ReferenceArrayProxy<BlueprintFeature,BlueprintFeatureReference> result)
        {
            if(!Owners.TryGetValue(pool,out var s)||!s.Sealed)return;Refresh(s);
            if(s.Views.TryGetValue(pool,out var choices)){result=choices;return;}
            if(s.Pools.Contains(pool)){
                var actual=(BlueprintFeatureReference[])Get(pool,all?"m_AllFeatures":"m_Features");var ids=new HashSet<BlueprintGuid>((actual??Array.Empty<BlueprintFeatureReference>()).Where(r=>r!=null).Select(r=>r.Guid));
                result=(actual??Array.Empty<BlueprintFeatureReference>()).Concat(s.Roots.Where(r=>!ids.Contains(r.AssetGuid)).Select(Ref<BlueprintFeatureReference>)).ToArray();
            }
        }
    }
    [HarmonyPatch(typeof(BlueprintsCache),nameof(BlueprintsCache.AddCachedBlueprint))]
    internal static class MimicCatalogueAdded01461Patch
    {static void Postfix(BlueprintsCache __instance,SimpleBlueprint __1)=>MimicCatalogue01461.Changed(__instance,__1);}
    [HarmonyPatch(typeof(BlueprintFeatureSelection),"get_AllFeatures")]
    internal static class MimicCatalogueAll01461Patch
    {static void Postfix(BlueprintFeatureSelection __instance,ref ReferenceArrayProxy<BlueprintFeature,BlueprintFeatureReference> __result)=>MimicCatalogue01461.View(__instance,true,ref __result);}
    [HarmonyPatch(typeof(BlueprintFeatureSelection),"get_Features")]
    internal static class MimicCatalogueFeatures01461Patch
    {static void Postfix(BlueprintFeatureSelection __instance,ref ReferenceArrayProxy<BlueprintFeature,BlueprintFeatureReference> __result)=>MimicCatalogue01461.View(__instance,false,ref __result);}
}
