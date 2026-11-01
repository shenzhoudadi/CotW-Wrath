using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class BloodlineSlots01451
    {
        internal static readonly int[] SorcererLevels={1,3,9,15},BloodragerLevels={1,4,8,12,16};
        static readonly string[] primalistSecondChoices={"dfbd18edfa9443b98b1ed3b643551d31","f820e9272d234162a7e6ef3b64be0f38","c63fcfd424a64d1aa195e7721b33863e","d43b1581b1f64f928e55d7ad6d36cb13","171a7125128f407fb439c1f31d9cec43"};
        static readonly object publicationGate=new object();
        static bool watching,arming,dirty;
        internal static void ObserveAndArm(Action install) {
            lock(publicationGate) {
                if(!watching){AppDomain.CurrentDomain.AssemblyLoad+=ProviderAssemblyLoaded;watching=true;}
                // Harmony may resolve another provider while installing this
                // one. Coalesce that loader re-entry and drain it afterwards.
                if(arming){dirty=true;return;}
                arming=true;try{do{dirty=false;install();}while(dirty);}finally{arming=false;}
            }
        }
        static void ProviderAssemblyLoaded(object sender,AssemblyLoadEventArgs args) {
            string name=args.LoadedAssembly.GetName().Name;
            if(name!="EbonsContentMod"&&name!="TabletopTweaks-Base"&&name!="TomeOfTheFirebird")return;
            // Arm only known publication methods. No blueprint/cache access
            // or reconciliation occurs inside the assembly-load event.
            try{BloodlineCompatibility01447.InstallProviderGuard(null);}
            catch(Exception error){try{ModEntry.LogSpellDiagnostic("[Bloodline compatibility01451] provider hook installation failed; startup retained: "+error);}catch{}}
        }
        static string Source(string id) {
            for(int i=0;i<4;i++)if(id==Id(PrimalSorcerer01444.Token(i,"Progression")).ToString())return PrimalSorcerer01444.Progressions[i];
            return id;
        }
        internal static string[][] Expand(string[][] rows)=>rows.Select(row=>{
            var levels=row[1]=="Bloodrager"?BloodragerLevels:SorcererLevels;
            if(row.Length>levels.Length+2)throw new InvalidOperationException("01451 unexpected bloodline row width: "+row[0]);
            var result=new string[levels.Length+2];Array.Copy(row,result,row.Length);
            for(int i=3;i<levels.Length;i++) {
                var matches=BloodlineSlotAnchors01451.Slots.Where(s=>s[0]==Source(row[0])&&s[1]==levels[i].ToString()).ToArray();
                if(matches.Length>1)throw new InvalidOperationException("01451 duplicate authored slot: "+row[0]+"/"+levels[i]);
                result[i+2]=matches.Length==0?"":matches[0][2];
            }
            return result;
        }).ToArray();
        internal static string[][] ExtraGrowth(string[][] rows)=>rows.SelectMany(row=>BloodlineSlotAnchors01451.Growth.Where(g=>g[0]==Source(row[0])).Select(g=>new[]{row[0],g[1],g[2],g[3]})).ToArray();
        internal static HashSet<BlueprintFeatureSelection> BonusPools(BlueprintsCache cache,IEnumerable<BlueprintProgression> bloodragers) {
            var root=cache.Load(BlueprintGuid.Parse("3a60f0c0442acfb419b0c03b584e1394")) as BlueprintFeatureSelection??throw new InvalidOperationException("01451 Sorcerer bonus parent is missing.");
            var pools=new HashSet<BlueprintFeatureSelection>{root};
            // These are the actual immediate bloodline-specific feat menus,
            // not every nested weapon/sub-school selector below their feats.
            foreach(var child in root.AllFeatures.OfType<BlueprintFeatureSelection>())pools.Add(child);
            foreach(var progression in bloodragers)foreach(var pool in progression.LevelEntries.Where(e=>new[]{6,9,12,15,18}.Contains(e.Level)).SelectMany(BloodlineCompatibility01447.Features).OfType<BlueprintFeatureSelection>())
                if(!pool.name.StartsWith("CotWWrathPsychicBloodlinePowerChoice",StringComparison.Ordinal)&&!primalistSecondChoices.Contains(pool.AssetGuid.ToString()))pools.Add(pool);
            return pools;
        }
        internal static void ReconcileBonusPools(BlueprintsCache cache) {
            var rows=BloodlineCompatibility01447.Rows(cache).Concat(Expand(BloodlineExternal01450.Rows)).Where(r=>r[1]=="Bloodrager");
            var progressions=rows.Select(r=>cache.Load(BlueprintGuid.Parse(r[0])) as BlueprintProgression).Where(p=>p!=null).ToArray();
            var pools=BonusPools(cache,progressions);var root=(BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse("3a60f0c0442acfb419b0c03b584e1394"));
            var sorcererPools=new HashSet<BlueprintFeatureSelection>(root.AllFeatures.OfType<BlueprintFeatureSelection>()){root};
            var before=pools.ToDictionary(p=>p,p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")});
            try {
                foreach(var pool in pools) {
                    var options=BloodlineCompatibility01447.Options(cache,!sorcererPools.Contains(pool));
                    foreach(string field in new[]{"m_Features","m_AllFeatures"}) {
                        var ids=new HashSet<BlueprintGuid>();var allowed=new HashSet<BlueprintGuid>(options.Select(f=>f.AssetGuid));
                        var refs=(((BlueprintFeatureReference[])Get(pool,field))??Array.Empty<BlueprintFeatureReference>()).Where(r=>r==null||!allowed.Contains(r.Guid)||ids.Add(r.Guid)).ToList();
                        foreach(var feature in options)if(!refs.Any(r=>r?.Guid==feature.AssetGuid))refs.Add(Ref<BlueprintFeatureReference>(feature));
                        Set(pool,field,refs.ToArray());
                        foreach(var feature in options)if(refs.Count(r=>r?.Guid==feature.AssetGuid)!=1)throw new InvalidOperationException("01451 bonus mutation link missing/duplicate: "+pool.name+"/"+feature.name);
                    }
                    if(!ReferenceEquals(cache.Load(pool.AssetGuid),pool))throw new InvalidOperationException("01451 bonus pool identity changed: "+pool.name);
                }
            }catch{foreach(var pool in pools){Set(pool,"m_Features",before[pool][0]);Set(pool,"m_AllFeatures",before[pool][1]);}throw;}
        }
        internal static void InstallPublicationGuards(BlueprintsCache cache) {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                string name=assembly.GetName().Name;string[][] targets;
                if(name=="TabletopTweaks-Base") {
                    // Builder .cctors create requisite facts. The safe aggregate
                    // has only a default bool field and is always armed first;
                    // never force those builder initializers during assembly load.
                    var list=new List<string[]>{new[]{"TabletopTweaks.Base.NewContent.ContentAdder+BlueprintsCache_Init_Patch","CreateNewBlueprints"}};
                    if(TTTReady(assembly,cache))list.AddRange(new[]{
                        new[]{"TabletopTweaks.Base.NewContent.Bloodlines.AberrantBloodline","AddSorcererAberrantBloodline"},
                        new[]{"TabletopTweaks.Base.NewContent.Bloodlines.AberrantBloodline","AddBloodragerAberrantBloodline"},
                        new[]{"TabletopTweaks.Base.NewContent.Bloodlines.DestinedBloodline","AddSorcererDestinedBloodline"},
                        new[]{"TabletopTweaks.Base.NewContent.Bloodlines.DestinedBloodline","AddBloodragerDestinedBloodline"}});
                    targets=list.ToArray();
                }
                else if(name=="TomeOfTheFirebird")targets=new[]{new[]{"TomeOfTheFirebird.New_Content.Bloodlines.PheonixBloodline","MakePheonixBloodline"}};
                else continue;
                foreach(var row in targets) {
                    var target=AccessTools.Method(assembly.GetType(row[0],true),row[1])??throw new InvalidOperationException("01451 publication endpoint changed: "+row[0]+"/"+row[1]);
                    var postfix=AccessTools.Method(typeof(BloodlineCompatibility01447),"ProviderConfigured");
                    int count=(Harmony.GetPatchInfo(target)?.Postfixes.AsEnumerable()??Enumerable.Empty<Patch>()).Count(p=>p.owner==BloodlineCompatibility01447.PatchOwner&&p.PatchMethod==postfix);
                    if(count>1)throw new InvalidOperationException("01451 duplicate publication hook: "+row[1]);if(count==1)continue;
                    var harmony=new Harmony(BloodlineCompatibility01447.PatchOwner);try{harmony.Patch(target,postfix:new HarmonyMethod(postfix));}catch{harmony.Unpatch(target,postfix);throw;}
                }
            }
        }
        static bool TTTReady(System.Reflection.Assembly assembly,BlueprintsCache cache) {
            if(cache==null)return false;
            // Main has only uninitialized fields; Core.BlueprintTools' .cctor
            // creates only its empty dictionary. Neither access creates facts.
            var context=AccessTools.Field(assembly.GetType("TabletopTweaks.Base.Main",true),"TTTContext").GetValue(null);if(context==null)return false;
            System.Reflection.Assembly core=null;
            for(var type=context.GetType();type!=null;type=type.BaseType)if(type.Assembly.GetName().Name=="TabletopTweaks-Core"){core=type.Assembly;break;}
            if(core==null)return false;
            var map=AccessTools.Field(core.GetType("TabletopTweaks.Core.Utilities.BlueprintTools",true),"ModBlueprints").GetValue(null) as System.Collections.IDictionary;
            var id=BlueprintGuid.Parse("e2cfd3cedf7c40088b25aa82d6db3c77");
            return map!=null&&map.Contains(id)&&ReferenceEquals(map[id],cache.Load(id));
        }
    }
}
