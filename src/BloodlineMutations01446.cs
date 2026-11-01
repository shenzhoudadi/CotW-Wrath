// Adapted from original CotW MechanicsFixes/BloodlinesFix.cs (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
using static CallOfTheWild.Wrath.Prototype.ArchetypeBatch01445;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class BloodlineMutations01446
    {
        internal const string ModuleName="Bloodline Familiar and ECM mutation compatibility";
        internal const string RootToken="BloodlineFamiliar01446", SorcererId="b3a505fb61437dc4097f43c3f8f9a4cf",BloodragerId="d77e67a814d686842802c9cfd8ef8499",MagusId="45a4607686d96a1498891b3286121780",ScionId="d078b2ef073f2814c9e338a789d97b73";
        internal static readonly string[] FeatureTokens={"BloodHavoc01446","BloodIntensity01446","BloodPiercing01446",RootToken};
        internal static BlueprintAbilityResource Resource(PsychicAssets f,string token,BlueprintCharacterClass sorc,BlueprintCharacterClass rage,BlueprintCharacterClass magus,int start,int step) {
            var r=f.Add(new BlueprintAbilityResource(),token,"","");var a=Get(r,"m_MaxAmount");
            Set(a,"IncreasedByLevelStartPlusDivStep",true);Set(a,"BaseValue",0);Set(a,"StartingLevel",start);Set(a,"StartingIncrease",1);Set(a,"LevelStep",step);Set(a,"PerStepIncrease",1);
            Set(a,"m_ClassDiv",new[]{Ref<BlueprintCharacterClassReference>(sorc),Ref<BlueprintCharacterClassReference>(rage),Ref<BlueprintCharacterClassReference>(magus)});Set(a,"m_ArchetypesDiv",new[]{Ref<BlueprintArchetypeReference>(f.Native<BlueprintArchetype>(ScionId))});Set(a,"m_Class",Array.Empty<BlueprintCharacterClassReference>());Set(a,"m_Archetypes",Array.Empty<BlueprintArchetypeReference>());Set(r,"m_MaxAmount",a);return r;
        }
        internal static BlueprintActivatableAbility Toggle(PsychicAssets f,string token,string title,string text,BlueprintBuff buff,BlueprintAbilityResource resource,BlueprintUnitFact icon,bool rounds=false) {
            var cost=new ActivatableAbilityResourceLogic{SpendType=rounds?ActivatableAbilityResourceLogic.ResourceSpendType.NewRound:ActivatableAbilityResourceLogic.ResourceSpendType.Never};Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));
            var t=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=!rounds,DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,ComponentsArray=new BlueprintComponent[]{cost}},token,title,text);
            Set(t,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);Set(t,"m_Buff",Ref<BlueprintBuffReference>(buff));ArcaneBatch01446.Icon(t,icon);ArcaneBatch01446.Icon(buff,icon);return t;
        }
        static PrerequisiteClassLevel Level(BlueprintCharacterClass cls,int level){var p=new PrerequisiteClassLevel{Level=level,Group=Prerequisite.GroupType.Any};Set(p,"m_CharacterClass",Ref<BlueprintCharacterClassReference>(cls));return p;}
        internal static void Register(BlueprintsCache cache) {
            if(ArcaneBatch01446.Existing(cache,ModuleName)){BloodlineCompatibility01447.Reconcile(cache);return;}
            // This bundled producer clones the still-native elemental bloodlines.
            // If it failed earlier, wait for the real callback retry rather than
            // publish mutations first and change the inputs of its later clone.
            if(!(cache.Load(Id(PrimalSorcerer01444.RootToken)) is BlueprintArchetype))throw new InvalidOperationException("01447 bloodline mutations require the bundled Primal Sorcerer producer to finish first.");
            var f=new PsychicAssets(cache);var sorc=f.Native<BlueprintCharacterClass>(SorcererId);var rage=f.Native<BlueprintCharacterClass>(BloodragerId);var magus=f.Native<BlueprintCharacterClass>(MagusId);
            var sourceFamiliar=f.Native<BlueprintFeatureSelection>("363cab72f77c47745bf3a8807074d183");
            var empower=f.Native<BlueprintFeature>("a1de1e4f92195b442adb946f0e2b9d4e");
            var intensityIcon=empower;var piercingIcon=empower;var familiarIcon=f.Native<BlueprintUnitFact>("1194129055923ae46a6b876ff6a30358");
            var rows=BloodlineCompatibility01447.Rows(cache);var growthRows=BloodlineCompatibility01447.Growth(rows);
            var progressions=rows.Select(row=>f.Native<BlueprintProgression>(row[0])).ToArray();
            BlueprintFeature Paid(int i,string name,string text,BlueprintFeature icon,int start,int step,int sorcLevel,int rageLevel) {
                string token=FeatureTokens[i];var resource=Resource(f,token+"Resource",sorc,rage,magus,start,step);
                var mode=new ArcaneCastMode01446{Kind=i==1?ArcaneMode01446.Intensity:ArcaneMode01446.Piercing,Resource=Ref<BlueprintAbilityResourceReference>(resource)};
                var buff=f.Buff(token+"Buff",name,text,mode);var toggle=Toggle(f,token+"Toggle",name,text,buff,resource,icon);
                var feature=f.Feature(token,name,text,Grant(resource),Facts(toggle),Level(sorc,sorcLevel),Level(rage,rageLevel));var sprite=PsychicOriginalIcons.Load(i==1?"IntensifiedSpell":"PiercingSpell");foreach(var item in new BlueprintUnitFact[]{feature,buff,toggle})Set(item,"m_Icon",sprite);return feature;
            }
            var intensity=Paid(1,"Blood Intensity",ArcaneText01446.Intensity,intensityIcon,3,4,3,8);var piercing=Paid(2,"Blood Piercing",ArcaneText01446.Piercing,piercingIcon,4,5,9,4);
            var familiar=f.Clone(sourceFamiliar,FeatureTokens[3],"Bloodline Familiar",ArcaneText01446.Familiar);familiar.ComponentsArray=Array.Empty<BlueprintComponent>();familiar.Groups=Array.Empty<FeatureGroup>();familiar.IsClassFeature=true;ArcaneBatch01446.Icon(familiar,familiarIcon);
            foreach(string field in new[]{"m_Features","m_AllFeatures"})Set(familiar,field,CopyData(Get(sourceFamiliar,field)));
            var features=new[]{intensity,piercing};
            PublishRows(cache,ModuleName,f,rows,growthRows,familiar,features);
            BloodlineCompatibility01447.Reconcile(cache);
        }
        internal static void PublishRows(BlueprintsCache cache,string module,PsychicAssets f,string[][] rows,string[][] growthRows,BlueprintFeature familiar,BlueprintFeature[] features) {
            var empower=f.Native<BlueprintFeature>("a1de1e4f92195b442adb946f0e2b9d4e");
            var progressions=rows.Select(row=>f.Native<BlueprintProgression>(row[0])).ToArray();
            var replacements=new Dictionary<string,BlueprintFeatureSelection>();
            var slots=new Dictionary<Tuple<int,int,string>,BlueprintFeatureSelection>();
            var oldEntries=progressions.Select(p=>p.LevelEntries).ToArray();var oldGroups=progressions.Select(p=>p.UIGroups).ToArray();var oldDeterm=progressions.Select(p=>Get(p,"m_UIDeterminatorsGroup")).ToArray();
            var removed=progressions.Select(p=>new HashSet<string>(growthRows.Where(g=>g[0]==p.AssetGuid.ToString()).Select(g=>g[2]+":"+g[3]))).ToArray();
            for(int n=0;n<progressions.Length;n++) {
                var p=progressions[n];var row=rows[n];bool br=row[1]=="Bloodrager";
                var levels=br?BloodlineSlots01451.BloodragerLevels:BloodlineSlots01451.SorcererLevels;
                for(int i=0;i<row.Length-2;i++) {
                    if(row[i+2].Length==0)continue;var power=f.Native<BlueprintFeatureBase>(row[i+2]);int level=levels[i];
                    var anchor=BloodlineCompatibility01447.Anchor(p,level,power);
                    var growth=growthRows.Where(g=>g[0]==p.AssetGuid.ToString()&&g[1]==power.AssetGuid.ToString()).ToArray();
                    // Several Wrath bloodlines share the initial fact but have
                    // different later facts (e.g. coloured bloodrager claws).
                    // Such original branches must never share a choice graph.
                    // Optional rows commit independently. They cannot reuse a
                    // staged choice GUID from a different row's receipt even
                    // when normal and Seeker share their original power.
                    bool independent=module.StartsWith(BloodlineExternal01450.Prefix,StringComparison.Ordinal);
                    string key=row[1]+i+power.AssetGuid+(growth.Length==0&&!independent?"":p.AssetGuid.ToString());
                    if(replacements.TryGetValue(key,out var prior)){slots.Add(Tuple.Create(n,level,anchor.AssetGuid.ToString()),prior);continue;}
                    var selection=f.Add(new BlueprintFeatureSelection{Ranks=1,IsClassFeature=true},"BloodlinePowerChoice"+key+"01446","","");
                    // A Primalist's removed automatic power is not another
                    // exchange allowance. Its existing conditional take-power
                    // grant remains the original feature, not a bonus feat menu.
                    if(br)selection.ComponentsArray=power.GetComponents<PrerequisiteNoFeature>().Where(p=>((BlueprintFeatureReference)Get(p,"m_Feature"))?.Guid==BlueprintGuid.Parse("d8b8d1dd83393484cbacf6c8830080ae")).Select(p=>(BlueprintComponent)CopyData(p)).ToArray();
                    else selection.ComponentsArray=BloodlineProvider01453.SourceRestrictions(cache,power,anchor);
                    // Keep the original native/CotW labels by reference. Resolving
                    // text during registration would freeze the current locale.
                    Set(selection,"m_DisplayName",Get(power,"m_DisplayName"));Set(selection,"m_Description",Get(power,"m_Description"));ArcaneBatch01446.Icon(selection,power);
                    if(Get(selection,"m_Icon")==null)ArcaneBatch01446.Icon(selection,empower);
                    var alternatives=(i==0?new BlueprintFeature[]{familiar}:Array.Empty<BlueprintFeature>()).Concat(features).Concat(BloodlineCompatibility01447.Options(cache,br)).Distinct().ToArray();
                    BlueprintFeature original=(BlueprintFeature)power;
                    if(growth.Length!=0) {
                        // CotW first folds every upgrade/repeated rank into the
                        // original-power branch. Replacing that branch must also
                        // forfeit its later grants. Keep native facts unchanged.
                        var branch=f.Add(new BlueprintProgression{IsClassFeature=true},"BloodlineOriginalGrowth"+key+"01446","","");
                        Set(branch,"m_DisplayName",Get(power,"m_DisplayName"));Set(branch,"m_Description",Get(power,"m_Description"));ArcaneBatch01446.Icon(branch,selection);
                        foreach(string field in new[]{"m_Classes","m_Archetypes"})Set(branch,field,CopyData(Get(p,field)));
                        branch.GiveFeaturesForPreviousLevels=true;
                        branch.LevelEntries=new[]{Entry(level,power)}.Concat(growth.GroupBy(g=>int.Parse(g[2])).Select(g=>Entry(g.Key,g.Select(x=>f.Native<BlueprintFeatureBase>(x[3])).ToArray()))).GroupBy(e=>e.Level).Select(g=>Entry(g.Key,g.SelectMany(BloodlineCompatibility01447.Features).ToArray())).OrderBy(e=>e.Level).ToArray();
                        branch.UIGroups=Array.Empty<UIGroup>();original=branch;
                    }
                    var opts=new[]{original}.Concat(alternatives).ToArray();Set(selection,"m_Features",opts.Select(Ref<BlueprintFeatureReference>).ToArray());Set(selection,"m_AllFeatures",opts.Select(Ref<BlueprintFeatureReference>).ToArray());replacements.Add(key,selection);slots.Add(Tuple.Create(n,level,anchor.AssetGuid.ToString()),selection);
                }
            }
            var pools=BloodlineSlots01451.BonusPools(cache,progressions.Where((_,n)=>rows[n][1]=="Bloodrager"));
            var poolOld=pools.ToDictionary(p=>p,p=>new[]{Get(p,"m_Features"),Get(p,"m_AllFeatures")});
            BlueprintFeatureBase Replace(BlueprintFeatureBase bp,int n,int level)=>slots.TryGetValue(Tuple.Create(n,level,bp.AssetGuid.ToString()),out var choice)?choice:bp;
            BlueprintFeatureBase[] UI(BlueprintFeatureBase bp,int n)=>bp==null?new BlueprintFeatureBase[]{null}:slots.Where(s=>s.Key.Item1==n&&(s.Key.Item3==bp.AssetGuid.ToString()||s.Value.AllFeatures.Any(a=>a.AssetGuid==bp.AssetGuid||a is BlueprintProgression b&&b.LevelEntries.Any(e=>e.Features.Any(x=>x.AssetGuid==bp.AssetGuid))))).Select(s=>(BlueprintFeatureBase)s.Value).Distinct().DefaultIfEmpty(bp).ToArray();
            Action undo=()=>{for(int n=0;n<progressions.Length;n++){progressions[n].LevelEntries=oldEntries[n];progressions[n].UIGroups=oldGroups[n];Set(progressions[n],"m_UIDeterminatorsGroup",oldDeterm[n]);}foreach(var p in pools){Set(p,"m_Features",poolOld[p][0]);Set(p,"m_AllFeatures",poolOld[p][1]);}};
            Action connect=()=>{
                foreach(var pool in pools)foreach(string field in new[]{"m_Features","m_AllFeatures"}) {
                    var refs=(((BlueprintFeatureReference[])Get(pool,field))??Array.Empty<BlueprintFeatureReference>()).ToList();
                    foreach(var feature in features)if(!refs.Any(r=>r?.Guid==feature.AssetGuid))refs.Add(Ref<BlueprintFeatureReference>(feature));
                    Set(pool,field,refs.ToArray());
                }
                for(int n=0;n<progressions.Length;n++) {
                    var p=progressions[n];if(!ReferenceEquals(cache.Load(p.AssetGuid),p))throw new InvalidOperationException("01446 native progression replaced during registration.");
                    foreach(var growth in growthRows.Where(g=>g[0]==p.AssetGuid.ToString()))if(oldEntries[n].Where(e=>e.Level==int.Parse(growth[2])).SelectMany(BloodlineCompatibility01447.Features).Count(bp=>bp.AssetGuid.ToString()==growth[3])!=1)throw new InvalidOperationException("01446 continuation anchor changed: "+p.name+"/"+growth[2]+"/"+growth[3]);
                    p.LevelEntries=oldEntries[n].Select(e=>Entry(e.Level,BloodlineCompatibility01447.Features(e).Where(bp=>!removed[n].Contains(e.Level+":"+bp.AssetGuid)).Select(bp=>Replace(bp,n,e.Level)).ToArray())).ToArray();
                    p.UIGroups=(oldGroups[n]??Array.Empty<UIGroup>()).Select(g=>{if(g==null)return null;var copy=new UIGroup();Set(copy,"m_Features",g.Features.SelectMany(x=>UI(x,n)).Select(x=>x==null?null:Ref<BlueprintFeatureBaseReference>(x)).ToList());return copy;}).ToArray();
                    Set(p,"m_UIDeterminatorsGroup",((BlueprintFeatureBaseReference[])oldDeterm[n]??Array.Empty<BlueprintFeatureBaseReference>()).SelectMany(r=>UI(r?.Get(),n)).Select(x=>x==null?null:Ref<BlueprintFeatureBaseReference>(x)).ToArray());
                }
            };
            Action verify=()=>{
                foreach(var p in progressions)if(!ReferenceEquals(cache.Load(p.AssetGuid),p))throw new InvalidOperationException("01446 published native progression identity changed.");
                foreach(var p in pools)if(!ReferenceEquals(cache.Load(p.AssetGuid),p))throw new InvalidOperationException("01446 published native feat pool identity changed.");
                for(int n=0;n<progressions.Length;n++)if(progressions[n].LevelEntries.Any(e=>BloodlineCompatibility01447.Features(e).Any(bp=>removed[n].Contains(e.Level+":"+bp.AssetGuid))))throw new InvalidOperationException("01446 continuation escaped original-power branch.");
                foreach(var slot in slots)if(progressions[slot.Key.Item1].LevelEntries.Where(e=>e.Level==slot.Key.Item2).SelectMany(BloodlineCompatibility01447.Features).Count(x=>x==slot.Value)!=1)throw new InvalidOperationException("01446 power selection link missing: "+progressions[slot.Key.Item1].name+"/"+slot.Key.Item2+"/"+slot.Key.Item3+" expected "+slot.Value.AssetGuid+" actual "+string.Join(",",progressions[slot.Key.Item1].LevelEntries.Where(e=>e.Level==slot.Key.Item2).SelectMany(BloodlineCompatibility01447.Features).Select(x=>x.AssetGuid.ToString())));
                foreach(var pool in pools)foreach(var feature in features)foreach(string field in new[]{"m_Features","m_AllFeatures"})if(((BlueprintFeatureReference[])Get(pool,field)).Count(r=>r.Guid==feature.AssetGuid)!=1)throw new InvalidOperationException("01446 bonus-feat link missing or duplicated.");
            };
            ArcaneBatch01446.Publish(module,f,connect,undo,verify);
        }
        // Filled from the actual latest-game native archive, not assumed KM GUIDs.

    }
}

