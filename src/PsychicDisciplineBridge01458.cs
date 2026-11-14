using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicDisciplineBridge01458
    {
        internal const string ModuleName="Psychic remaining disciplines: Magaambyan compatibility";
        static readonly ConditionalWeakTable<BlueprintsCache,object> Complete=new ConditionalWeakTable<BlueprintsCache,object>();
        internal static bool Register(BlueprintsCache cache)
        {
            var cls=cache.Load(BlueprintGuid.Parse(PsychicClass.ClassId)) as BlueprintCharacterClass;
            var root=cache.Load(Id(MagaambyanTelepath01447.RootToken)) as BlueprintArchetype;
            if(cls==null||root==null||!ArcaneBatch01446.Existing(cache,PsychicClassCompletion01458.ModuleName))return false;
            if(Complete.TryGetValue(cache,out _))return true;
            if(!Existing(cache,MagaambyanTelepath01447.RootToken,cls))throw new InvalidOperationException("058 Magaambyan original receipt is unavailable.");
            var pool=cache.Load(Id("MagaambyanDisciplineSelection01447")) as BlueprintFeatureSelection??throw new InvalidOperationException("058 original Magaambyan discipline pool is unavailable.");
            var f=new PsychicAssets(cache);string[] keys={"Pain","Psychedelia","Abomination"};
            var choices=keys.Select(key=>{
                var source=f.Native<BlueprintProgression>(Id(key+"Discipline01458").ToString());
                var p=SpellBatch01447.Progression(f,source,"Magaambyan"+source.AssetGuid+"01458",source.Name,source.Description);
                p.LevelEntries=source.LevelEntries.Select(e=>Entry(e.Level,e.Features.Where(v=>!v.GetComponents<AddKnownSpell>().Any()).ToArray())).ToArray();
                p.UIGroups=Array.Empty<UIGroup>();Set(p,"m_UIDeterminatorsGroup",Array.Empty<BlueprintFeatureBaseReference>());return p;
            }).ToArray();
            var before=new[]{Get(pool,"m_Features"),Get(pool,"m_AllFeatures")};string[] fields={"m_Features","m_AllFeatures"};
            void Connect(){for(int k=0;k<2;k++){var prior=(BlueprintFeatureReference[])before[k];if(prior.Any(v=>v==null||choices.Any(c=>v.Guid==c.AssetGuid)))throw new InvalidOperationException("058 invalid original Magaambyan pool or duplicate bridge.");Set(pool,fields[k],prior.Concat(choices.Select(Ref<BlueprintFeatureReference>)).ToArray());}}
            void Undo(){for(int k=0;k<2;k++)Set(pool,fields[k],before[k]);}
            Extend(cache,MagaambyanTelepath01447.RootToken,f,Connect,Undo);Complete.Add(cache,new object());return true;
        }
    }
}
