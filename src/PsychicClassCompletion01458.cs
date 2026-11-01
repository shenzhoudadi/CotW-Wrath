using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.ActivatableAbilities;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicClassCompletion01458
    {
        internal const string ModuleName="Psychic remaining disciplines and amplifications";
        sealed class Extension{internal BlueprintsCache Cache;internal BlueprintScriptableObject[] Assets;internal string[] Shapes;internal BlueprintFeature[] Choices;}
        static readonly ConditionalWeakTable<BlueprintFeatureSelection,Extension> Extensions=new ConditionalWeakTable<BlueprintFeatureSelection,Extension>();
        internal static BlueprintScriptableObject RegistrationShape(BlueprintScriptableObject original)
        {
            var shaped=MutationMind01457.AmplificationRegistrationShape(original);
            if(!(original is BlueprintFeatureSelection pool)||!Extensions.TryGetValue(pool,out var r))return shaped;
            for(int i=0;i<r.Assets.Length;i++)if(!ReferenceEquals(r.Cache.Load(r.Assets[i].AssetGuid),r.Assets[i])||Definition(r.Assets[i])!=r.Shapes[i])throw new InvalidOperationException("058 declared Psychic extension changed: "+r.Assets[i].name);
            var clone=(BlueprintFeatureSelection)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(shaped,null);var ids=new HashSet<BlueprintGuid>(r.Choices.Select(v=>v.AssetGuid));
            foreach(string field in new[]{"m_Features","m_AllFeatures"}){var actual=(BlueprintFeatureReference[])Get(pool,field);foreach(var f in r.Choices)if(actual.Count(v=>v?.Guid==f.AssetGuid)!=1||!ReferenceEquals(actual.Single(v=>v?.Guid==f.AssetGuid).Get(),f))throw new InvalidOperationException("058 declared amplification link missing or changed.");var refs=(BlueprintFeatureReference[])Get(clone,field);Set(clone,field,refs.Where(v=>v==null||!ids.Contains(v.Guid)).ToArray());}return clone;
        }
        internal static void Register(BlueprintsCache cache)
        {
            if(ArcaneBatch01446.Existing(cache,ModuleName))return;
            var f=new PsychicAssets(cache);var cls=f.Native<BlueprintCharacterClass>(PsychicClass.ClassId);
            var discipline=f.Native<BlueprintFeatureSelection>(Id("DisciplineSelection").ToString());var normal=f.Native<BlueprintFeatureSelection>(Id("PhrenicAmplificationSelection").ToString());var extra=f.Native<BlueprintFeatureSelection>(Id("ExtraPhrenicAmplificationFeat").ToString());
            var disciplines=PsychicRemainingDisciplines01458.Build(f,cls);var modes=new List<BlueprintActivatableAbility>();var amplifications=PsychicCompletionAmplifications01458.Build(f,cls,modes);foreach(var choice in amplifications)choice.IsClassFeature=true;
            var pools=new[]{discipline,normal,extra};var choices=new[]{disciplines.Cast<BlueprintFeature>().ToArray(),amplifications,amplifications};string[] fields={"m_Features","m_AllFeatures"};
            var prior=pools.Select(p=>fields.Select(field=>(BlueprintFeatureReference[])Get(p,field)).ToArray()).ToArray();
            // Mutation Mind can publish before or after this module. Compare
            // original rows through its strict, receipt-checked view only;
            // raw arrays remain the exact transaction rollback objects.
            var originalRows=pools.Select(p=>{var view=MutationMind01457.AmplificationRegistrationShape(p);return fields.Select(field=>(BlueprintFeatureReference[])Get(view,field)).ToArray();}).ToArray();
            void Connect(){for(int i=0;i<pools.Length;i++)foreach(string field in fields){var before=(BlueprintFeatureReference[])Get(pools[i],field)??Array.Empty<BlueprintFeatureReference>();if(before.Any(v=>v==null))throw new InvalidOperationException("058 base pool has an unresolved reference.");foreach(var choice in choices[i])if(before.Any(v=>v.Guid==choice.AssetGuid))throw new InvalidOperationException("058 duplicate pending class choice.");Set(pools[i],field,before.Concat(choices[i].Select(Ref<BlueprintFeatureReference>)).ToArray());}}
            void Undo(){for(int i=0;i<pools.Length;i++)for(int k=0;k<fields.Length;k++)Set(pools[i],fields[k],prior[i][k]);}
            void Verify(){for(int i=0;i<pools.Length;i++){var view=MutationMind01457.AmplificationRegistrationShape(pools[i]);for(int k=0;k<fields.Length;k++){var actual=(BlueprintFeatureReference[])Get(view,fields[k]);var ids=new HashSet<BlueprintGuid>(choices[i].Select(v=>v.AssetGuid));if(actual==null||actual.Any(v=>v==null)||!actual.Where(v=>!ids.Contains(v.Guid)).SequenceEqual(originalRows[i][k]))throw new InvalidOperationException("058 original class-pool rows changed.");foreach(var choice in choices[i])if(actual.Count(v=>v.Guid==choice.AssetGuid)!=1||!ReferenceEquals(actual.Single(v=>v.Guid==choice.AssetGuid).Get(),choice))throw new InvalidOperationException("058 class extension identity/link mismatch.");}}}
            SpellBatch01447.Publish(ModuleName,f,Connect,Undo,Verify);
            var assets=f.Assets.Values.ToArray();var extension=new Extension{Cache=cache,Assets=assets,Shapes=assets.Select(Definition).ToArray(),Choices=amplifications};Extensions.Add(normal,extension);Extensions.Add(extra,extension);
            MimicCatalogue01461.Seal(cache,normal,extra);
        }
    }
}
