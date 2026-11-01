using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // The provider notification is an optimization, not the only chance to
    // reconcile a menu. Check the actual progression before native level-up
    // snapshots it. Native prerequisite checks and grants remain untouched.
    [HarmonyPatch(typeof(LevelUpHelper),nameof(LevelUpHelper.UpdateProgression))]
    internal static class BloodlineMenu01452
    {
        static readonly HashSet<string> Known=new HashSet<string>(BloodlineAnchors01446.Rows.Concat(BloodlineVariants01447.Rows).Concat(BloodlineExternal01450.Rows).Where(r=>r[1]=="Sorcerer").Select(r=>r[0]).Concat(Enumerable.Range(0,4).Select(i=>Id(PrimalSorcerer01444.Token(i,"Progression")).ToString())));
        static readonly ConditionalWeakTable<BlueprintsCache,HashSet<BlueprintGuid>> Reported=new ConditionalWeakTable<BlueprintsCache,HashSet<BlueprintGuid>>();
        [ThreadStatic]static bool active;
        static void Prefix(LevelUpState __0,UnitDescriptor __1,BlueprintProgression __2)
        {
            if(active||__0?.SelectedClass?.AssetGuid.ToString()!=BloodlineMutations01446.SorcererId||__1==null||__2==null||!Known.Contains(__2.AssetGuid.ToString()))return;
            var cache=ResourcesLibrary.BlueprintsCache;if(cache==null)return;
            active=true;
            try {
                if(cache.Load(Id(BloodlineMutations01446.RootToken))==null)return;
                if(!ReferenceEquals(cache.Load(__2.AssetGuid),__2))throw new InvalidOperationException("Noncanonical progression; menu not overwritten.");
                var options=BloodlineCompatibility01447.Options(cache,false);
                // An absent or placeholder provider is not a Havoc effect.
                if(!options.Any(f=>f.AssetGuid.ToString()==BloodlineCompatibility01447.HavocIds[0]))return;
                var row=BloodlineCompatibility01447.Rows(cache).Concat(BloodlineSlots01451.Expand(BloodlineExternal01450.Rows)).FirstOrDefault(r=>r[0]==__2.AssetGuid.ToString());
                if(row==null)return;
                bool complete=Complete(cache,__2,row,options);
                ArcaneBatch01446.ValidateSorcererTarget01453(cache,__2,complete);
                if(complete)return;
                var prior=__2.LevelEntries;
                var errors=new List<Exception>();
                try{ArcaneBatch01446.ReconcileSorcererProviderOptions01452(cache);}catch(Exception error){errors.Add(error);}
                try{BloodlineExternal01450.ReconcileSorcerer01452(cache);}catch(Exception error){errors.Add(error);}
                // An unrelated optional receipt can fail independently. A full
                // shared/target receipt validation, not just option counts,
                // is still required before using this target's repaired menu.
                ArcaneBatch01446.ValidateSorcererTarget01453(cache,__2,true);
                if(!Complete(cache,__2,row,options))throw new InvalidOperationException("Actual Sorcerer power menu still incomplete after reconciliation.");
                if(errors.Count!=0&&Reported.GetValue(cache,_=>new HashSet<BlueprintGuid>()).Add(__2.AssetGuid))try{ModEntry.LogSpellDiagnostic("[Bloodline menu01453] Target receipt verified; independent optional errors retained. progression="+__2.AssetGuid+"; "+new AggregateException(errors));}catch{}
                if(!ReferenceEquals(prior,__2.LevelEntries)) {
                    // Existing ProgressionData keeps calculated level entries.
                    // Refresh its native snapshot without changing its Level,
                    // archetypes, previously selected facts, or save files.
                    var data=__1.Progression.GetAllProgressions.FirstOrDefault(p=>ReferenceEquals(p.Key,__2)).Value;
                    data?.PostLoad(__2);
                }
            } catch(Exception error) {
                if(Reported.GetValue(cache,_=>new HashSet<BlueprintGuid>()).Add(__2.AssetGuid))try{ModEntry.LogSpellDiagnostic("[Bloodline menu01452] Sorcerer upgrade reconciliation failed; native upgrade retained. progression="+__2.AssetGuid+" name="+__2.name+"; "+error);}catch{}
            } finally{active=false;}
        }
        static bool Complete(BlueprintsCache cache,BlueprintProgression p,string[] row,BlueprintFeature[] options)
        {
            for(int i=2;i<row.Length;i++) {
                if(row[i].Length==0)continue;int level=BloodlineSlots01451.SorcererLevels[i-2];
                var menus=p.LevelEntries.Where(e=>e.Level==level).SelectMany(BloodlineCompatibility01447.Features).OfType<BlueprintFeatureSelection>().Where(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoiceSorcerer",StringComparison.Ordinal)).ToArray();
                if(menus.Length!=1)return false;
                var menu=menus[0];if(!ReferenceEquals(cache.Load(menu.AssetGuid),menu))return false;
                var wanted=options.Select(f=>f.AssetGuid).Concat(i==2?new[]{Id(BloodlineMutations01446.RootToken)}:Array.Empty<BlueprintGuid>()).ToArray();
                foreach(string field in new[]{"m_Features","m_AllFeatures"}) {
                    var refs=(BlueprintFeatureReference[])Get(menu,field);
                    if(refs==null||wanted.Any(id=>refs.Count(r=>r?.Guid==id)!=1))return false;
                }
            }
            return true;
        }
    }
}
