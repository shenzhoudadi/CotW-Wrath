using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // ECM EldritchScrapper.Configure scans the real Sorcerer root's 1/9/15
    // entries, then mutation Configure runs. Preserve that native restriction
    // in either order. No other provider component is an accepted receipt edit.
    internal static class BloodlineProvider01453
    {
        internal const string Scrapper="6c4429ef31104fc9a0ab5c353e0c38b4";
        internal static string Describe(BlueprintScriptableObject asset)=>string.Join("|",asset.ComponentsArray.Select(c=>c is PrerequisiteNoArchetype p?
            p.GetType().FullName+"{class="+((BlueprintCharacterClassReference)Get(p,"m_CharacterClass"))?.Guid+",archetype="+((BlueprintArchetypeReference)Get(p,"m_Archetype"))?.Guid+",group="+p.Group+",inProgression="+p.CheckInProgression+",hidden="+p.HideInUI+"}":c?.GetType().FullName??"null"));
        static bool CanonicalProvider(BlueprintsCache cache) =>
            cache.Load(BlueprintGuid.Parse(Scrapper)) is BlueprintArchetype a &&
            cache.Load(BlueprintGuid.Parse(BloodlineMutations01446.SorcererId)) is BlueprintCharacterClass cls &&
            ((BlueprintArchetypeReference[])Get(cls,"m_Archetypes")??Array.Empty<BlueprintArchetypeReference>()).Count(r=>r?.Guid==a.AssetGuid&&ReferenceEquals(r.Get(),a))==1;
        static string ComponentShape(BlueprintComponent c)=>ArchetypeBatch01445.Definition(new BlueprintFeature{ name="01453 component view",ComponentsArray=new[]{c} });
        static PrerequisiteNoArchetype Expected(BlueprintsCache cache) {
            var p=new PrerequisiteNoArchetype{CheckInProgression=true,Group=Prerequisite.GroupType.All};
            Set(p,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cache.Load(BlueprintGuid.Parse(BloodlineMutations01446.SorcererId))));
            Set(p,"m_Archetype",Ref<BlueprintArchetypeReference>(cache.Load(BlueprintGuid.Parse(Scrapper))));return p;
        }
        internal static bool Restriction(BlueprintsCache cache,BlueprintComponent c)
        {
            if(c?.GetType()!=typeof(PrerequisiteNoArchetype)||!CanonicalProvider(cache))return false;
            return ComponentShape(c)==ComponentShape(Expected(cache));
        }
        internal static BlueprintComponent[] SourceRestrictions(BlueprintsCache cache,BlueprintFeatureBase power,BlueprintFeatureBase anchor)
        {
            // A shared native power can also be used by Seeker/Primal. Its
            // already-authored native exclusion must survive that wrapping.
            var candidates=new[]{power,anchor}.Distinct().SelectMany(f=>f.GetComponents<PrerequisiteNoArchetype>().Where(c=>
                ((BlueprintCharacterClassReference)Get(c,"m_CharacterClass"))?.Guid.ToString()==BloodlineMutations01446.SorcererId&&
                ((BlueprintArchetypeReference)Get(c,"m_Archetype"))?.Guid.ToString()==Scrapper)).ToArray();
            if(candidates.Any(c=>!Restriction(cache,c)))throw new InvalidOperationException("01453 unexpected ECM source restriction fields: "+power.AssetGuid);
            // The real scanner visits distinct LevelEntries, not distinct
            // powers. ToTF's shared dragon claws can already carry several
            // identical exclusions. Preserve their full authored multiset;
            // never Take(1), deduplicate, or accept new duplicates on a receipt.
            var values=candidates;
            return values.Select(c=>(BlueprintComponent)CopyData(c)).ToArray();
        }
        static bool ScannedChoice(BlueprintsCache cache,BlueprintFeatureSelection choice)
        {
            if(!ReferenceEquals(cache.Load(choice.AssetGuid),choice)||!CanonicalProvider(cache)||
                !(cache.Load(BlueprintGuid.Parse("24bef8d1bee12274686f6da6ccbc8914")) is BlueprintFeatureSelection root))return false;
            // This is the released scanner's actual parent/level membership,
            // not a name-prefix or assumed level test. Initial CotW wrappers
            // have no AddAbilityResources and therefore fail its level1 filter.
            return root.AllFeatures.OfType<BlueprintProgression>().Where(p=>ReferenceEquals(cache.Load(p.AssetGuid),p)).Any(p=>
                new[]{1,9,15}.Select(p.GetLevelEntry).Any(e=>e!=null&&e.Features.Any(f=>ReferenceEquals(f,choice)&&
                    (e.Level==1?f.GetComponent<AddAbilityResources>()!=null:f.GetComponent<AddKnownSpell>()==null))));
        }
        internal static bool LateRestriction(BlueprintsCache cache,BlueprintFeatureSelection choice,BlueprintComponent[] prior)
        {
            var now=choice.ComponentsArray;
            return prior!=null && now.Length==prior.Length+1 && prior.Select((c,i)=>ReferenceEquals(c,now[i])).All(x=>x) &&
                !prior.Any(c=>Restriction(cache,c)) && Restriction(cache,now[now.Length-1]) && ScannedChoice(cache,choice);
        }
        internal static void MirrorInitialRestriction(BlueprintsCache cache,BlueprintFeatureSelection choice)
        {
            if(choice.ComponentsArray.Length!=0||!CanonicalProvider(cache)||
                !(cache.Load(BlueprintGuid.Parse("24bef8d1bee12274686f6da6ccbc8914")) is BlueprintFeatureSelection root))return;
            if(!root.AllFeatures.OfType<BlueprintProgression>().Any(p=>ReferenceEquals(cache.Load(p.AssetGuid),p)&&p.GetLevelEntry(1)?.Features.Any(f=>ReferenceEquals(f,choice))==true))return;
            // The wrapper removed AddAbilityResources from the scanner's direct
            // view. Mirror its exclusion only when the preserved original
            // first-level branch actually has that native component.
            var original=choice.AllFeatures.FirstOrDefault();
            var powers=original is BlueprintProgression growth?growth.GetLevelEntry(1)?.Features:new[]{original};
            if(powers?.Any(f=>f?.GetComponent<AddAbilityResources>()!=null)==true){
                var p=Expected(cache);p.name="$CotWECMScrapper01453";p.OwnerBlueprint=choice;
                choice.ComponentsArray=new BlueprintComponent[]{p};
            }
        }
    }
}
