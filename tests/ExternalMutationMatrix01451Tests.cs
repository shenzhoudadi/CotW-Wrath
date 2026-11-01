using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Newtonsoft.Json.Linq;

internal static partial class Feedback01449Tests
{
    static int ExternalMatrix01451(BlueprintsCache cache,Harmony h,Assembly ttt,Assembly ecm)
    {
        int before=checks,slots=0,selects=0,growth=0,qualifications=0;
        var input=JObject.Parse(File.ReadAllText(Path.Combine(Root(),(Main.GetType("CallOfTheWild.Wrath.Prototype.MagusBatch01454")!=null?"work/psychic01458/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineProvider01453")!=null?"work/repair01453/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452")!=null?"work/repair01452/":"work/repair01451/")+"external-matrix-input.json")));
        var intensity=A<BlueprintFeature>(cache,"BloodIntensity01446");var piercing=A<BlueprintFeature>(cache,"BloodPiercing01446");var familiar=A<BlueprintFeatureSelection>(cache,"BloodlineFamiliar01446");
        var wizard=N<BlueprintCharacterClass>(cache,"ba34257984f4c41408ce1dc2004e342e");
        foreach(var item in input["rows"]) {
            var row=item.Select(x=>(string)x).ToArray();bool br=row[1]=="Bloodrager";var levels=br?new[]{1,4,8,12,16}:new[]{1,3,9,15};
            var progression=N<BlueprintProgression>(cache,row[0]);var cls=N<BlueprintCharacterClass>(cache,br?"d77e67a814d686842802c9cfd8ef8499":"b3a505fb61437dc4097f43c3f8f9a4cf");
            bool seeker=!br&&((JArray)input["rows"]).IndexOf(item)%2==1;var archetype=seeker?N<BlueprintArchetype>(cache,"7229db6fc0b07af4180e783eed43c4d9"):null;
            var havoc=N<BlueprintFeature>(cache,br?"c33eddceeb3d4600be9139037e6015b6":"5d61fcd8c3d545da9f97e1b2fda8218b");
            for(int i=0;i<levels.Length;i++) {
                var choices=progression.LevelEntries.Where(e=>e.Level==levels[i]).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Where(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoice",StringComparison.Ordinal)&&s.AllFeatures.Contains(intensity)).ToArray();
                if(string.IsNullOrEmpty(row[i+2])){Check(choices.Length==0,"external Seeker lacks that actual power; no invented trade "+progression.name+"/"+levels[i]);continue;}
                Check(choices.Length==1,"one actual external mutation exchange "+progression.name+"/"+levels[i]);var choice=choices.Single();slots++;
                Check(choice.AllFeatures.Contains(familiar)==(i==0),"external Familiar is initial-only "+progression.name+"/"+levels[i]);
                foreach(var feature in new[]{havoc,intensity,piercing}) {
                    Check(choice.AllFeatures.Count(f=>f==feature)==1,"canonical option present once "+progression.name+"/"+levels[i]+"/"+feature.name);
                    int threshold=feature==havoc?(br?4:1):feature==intensity?(br?8:3):(br?4:9);
                    Check(Select01451(cache,cls,levels[i],choice,feature,null,archetype)==(levels[i]>=threshold),"real external native CanSelect threshold "+progression.name+"/"+levels[i]+"/"+feature.name);selects++;
                    Check(!Select01451(cache,wizard,levels[i],choice,feature),"external wrong-class negative "+progression.name+"/"+feature.name);selects++;
                }
            }
            foreach(var g in input["growth"].Select(x=>x.Select(v=>(string)v).ToArray()).Where(g=>g[0]==row[0])) {
                int source=Array.IndexOf(row,g[1],2);Check(source>=2,"external continuation belongs to an actual power "+progression.name);
                var choice=progression.LevelEntries.Where(e=>e.Level==levels[source-2]).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Single(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoice",StringComparison.Ordinal)&&s.AllFeatures.Contains(intensity));
                var branch=choice.AllFeatures.OfType<BlueprintProgression>().Single(p=>p.LevelEntries.SelectMany(e=>e.Features).Any(f=>f.AssetGuid.ToString()==g[1]));
                int tier=int.Parse(g[2]);Check(new ProgressionData(branch).GetLevelEntry(tier).Features.Count(f=>f.AssetGuid.ToString()==g[3])==1,"real external original branch keeps its exact continuation "+progression.name+"/"+tier);
                Check(!new ProgressionData(progression).GetLevelEntry(tier).Features.Any(f=>f.AssetGuid.ToString()==g[3]),"external mutation cannot retain discarded original continuation "+progression.name+"/"+tier);growth++;
            }
            if(((JArray)input["rows"]).IndexOf(item)>=10) {
                var helper=ecm.GetType("EbonsContentMod.Utilities.ComponentHelperators",true);var owner=(UnitEntityData)H("Unit",cache,cls.AssetGuid.ToString(),19);
                var grants=progression.LevelEntries.SelectMany(e=>e.Features).SelectMany(f=>f.GetComponents<AddKnownSpell>().Where(k=>k.Spell!=null).Select(k=>new{Fact=(BlueprintFeature)f,Spell=k.Spell})).ToArray();Check(grants.Length==4,"actual new TTT Bloodrager has its four spell grants "+progression.name);
                var grant=grants[0];Check(!(bool)Call(helper,"GetIsBloodlineSpell",grant.Spell,owner),"new TTT bloodrager requires real ownership "+progression.name);
                H("Fact",owner,progression);bool original=OriginalECMQuery01450(grant.Spell,owner);Check((bool)Call(helper,"GetIsBloodlineSpell",grant.Spell,owner)==original,"preserve ECM original before grant "+progression.name);
                H("Fact",owner,grant.Fact);Check((bool)Call(helper,"GetIsBloodlineSpell",grant.Spell,owner),"actual ECM query accepts new TTT Bloodrager owned/granted spell "+progression.name);qualifications++;
                var parents=((SimpleBlueprint[])H("Loaded",cache)).OfType<BlueprintFeatureSelection>().Where(p=>p.AllFeatures.Contains(progression)).ToArray();Check(parents.Length>0,"actual TTT Bloodrager primary/second parent is registered "+progression.name);
                Check(parents.Any(p=>Select01451(cache,cls,1,p,progression)),"actual TTT Bloodrager parent admits its progression "+progression.name);selects++;
            }
        }
        Check(slots==54&&growth==14&&qualifications==4,"full14external progression/54power/14growth/4newECM query inventory");
        foreach(string id in new[]{"dfbd18edfa9443b98b1ed3b643551d31","f820e9272d234162a7e6ef3b64be0f38","c63fcfd424a64d1aa195e7721b33863e","d43b1581b1f64f928e55d7ad6d36cb13"}) {
            var selection=N<BlueprintFeatureSelection>(cache,id);Check(!selection.AllFeatures.Contains(intensity)&&!selection.AllFeatures.Contains(piercing),"Primalist take-power/rage fork is not mistaken for a bonus feat pool "+selection.name);
        }
        var aggregate=AccessTools.Method(ttt.GetType("TabletopTweaks.Base.NewContent.ContentAdder+BlueprintsCache_Init_Patch",true),"CreateNewBlueprints");
        Check(Harmony.GetPatchInfo(aggregate).Postfixes.Count(p=>p.owner=="CallOfTheWild.Wrath.Prototype.ECM01447")==1,"one safe TTT completion hook without forcing builder cctors");
        var saved=((SimpleBlueprint[])H("Loaded",cache)).Where(p=>p.name.StartsWith("CotWWrathPsychicBloodline",StringComparison.Ordinal)).ToDictionary(p=>p.AssetGuid,p=>p);
        for(int i=0;i<3;i++)Call(T("BloodlineMutations01446"),"Register",cache);
        Check(saved.All(p=>ReferenceEquals(cache.Load(p.Key),p.Value)),"all external identities survive repeated reconciliation");
        PsychicExpansionTests.Inventory["externalMutationMatrix01451Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-before,["progressions"]=14,["authoredSlots"]=slots,["growthRows"]=growth,["actualNativeCanSelectCalls"]=selects,["newTTTBloodragerECMQueries"]=qualifications,["realTTTPrimaryAndSecondParents"]=true,["primalistForkNotBonusPool"]=true,["singleSafeTTTCompletionHook"]=true,["repeatedIdentities"]=true,["case"]=Environment.GetEnvironmentVariable("COTW_01451_CASE"),["boundary"]="Actual released five previous builders plus both TTT Bloodrager builders execute; native CanSelect and progression lookups. Standalone same-member adapters and explicit config/IO/engine endpoints; safe aggregate hook identity is checked, full aggregate/mod startup is not asserted."};
        return checks-before;
    }
}
