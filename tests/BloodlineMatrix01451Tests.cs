using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Newtonsoft.Json.Linq;

internal static partial class Feedback01449Tests
{
    static int NativeMatrix01451(BlueprintsCache cache,Harmony h)
    {
        int before=checks,slots=0,selects=0,growthChecks=0,missing=0,bonusChecks=0;
        var input=JObject.Parse(File.ReadAllText(Path.Combine(Root(),(Main.GetType("CallOfTheWild.Wrath.Prototype.MagusBatch01454")!=null?"work/psychic01458/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineProvider01453")!=null?"work/repair01453/":Main.GetType("CallOfTheWild.Wrath.Prototype.BloodlineMenu01452")!=null?"work/repair01452/":"work/repair01451/")+"native-matrix-input.json")));
        var intensity=A<BlueprintFeature>(cache,"BloodIntensity01446");var piercing=A<BlueprintFeature>(cache,"BloodPiercing01446");var familiar=A<BlueprintFeatureSelection>(cache,"BloodlineFamiliar01446");
        var wizard=N<BlueprintCharacterClass>(cache,"ba34257984f4c41408ce1dc2004e342e");
        var canonicalRows=input["rows"].ToArray();Check(canonicalRows.Length==71,"independent source inventory covers46native/21Seeker/4Primal progressions");
        foreach(var item in canonicalRows) {
            var row=item["row"].Select(x=>(string)x).ToArray();bool br=row[1]=="Bloodrager";var levels=br?new[]{1,4,8,12,16}:new[]{1,3,9,15};
            var progression=N<BlueprintProgression>(cache,row[0]);var cls=N<BlueprintCharacterClass>(cache,br?"d77e67a814d686842802c9cfd8ef8499":"b3a505fb61437dc4097f43c3f8f9a4cf");
            var havoc=N<BlueprintFeature>(cache,br?"c33eddceeb3d4600be9139037e6015b6":"5d61fcd8c3d545da9f97e1b2fda8218b");
            string archetypeId=(string)item["archetype"];var archetype=archetypeId==null?null:N<BlueprintArchetype>(cache,archetypeId);
            for(int i=0;i<levels.Length;i++) {
                int level=levels[i];var choices=progression.LevelEntries.Where(e=>e.Level==level).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Where(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoice",StringComparison.Ordinal)&&s.AllFeatures.Contains(intensity)).ToArray();
                if(string.IsNullOrEmpty(row[i+2])){Check(choices.Length==0,"missing archetype/native power never becomes an invented exchange: "+progression.name+"/"+level);missing++;continue;}
                Check(choices.Length==1,"exact one authored exchange slot "+progression.name+"/"+level);var choice=choices.Single();slots++;
                Check(choice.AllFeatures.Count(x=>x==familiar)==(i==0?1:0),"Familiar remains an initial-power choice "+progression.name+"/"+level);
                Check(choice.AllFeatures.Any(x=>x.AssetGuid.ToString()==row[i+2]||x is BlueprintProgression p&&p.LevelEntries.SelectMany(e=>e.Features).Any(f=>f.AssetGuid.ToString()==row[i+2])),"original power remains a real alternative "+progression.name+"/"+level);
                foreach(var feature in new[]{havoc,intensity,piercing}) {
                    Check(choice.AllFeatures.Count(x=>x==feature)==1,"every independent power slot contains one canonical mutation "+progression.name+"/"+level+"/"+feature.name);
                    int threshold=feature==havoc?(br?4:1):feature==intensity?(br?8:3):(br?4:9);
                    Check(Select01451(cache,cls,level,choice,feature,null,archetype)==(level>=threshold),"native CanSelect obeys actual class threshold "+progression.name+"/"+level+"/"+feature.name);selects++;
                    Check(!Select01451(cache,wizard,Math.Max(1,level),choice,feature),"wrong class cannot acquire mutation by forcing its pool "+progression.name+"/"+level+"/"+feature.name);selects++;
                    if(threshold>1){Check(!Select01451(cache,cls,threshold-1,choice,feature,null,archetype),"one level below real prerequisite remains ineligible "+progression.name+"/"+feature.name);selects++;}
                }
            }
            Check(!progression.LevelEntries.Where(e=>e.Level==20).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Any(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoice",StringComparison.Ordinal)),"no extra CotW level20 trade was invented "+progression.name);
            foreach(var g in input["growth"].Select(x=>x.Select(v=>(string)v).ToArray()).Where(g=>g[0]==row[0])) {
                int source=Array.IndexOf(row,g[1],2);Check(source>=2,"growth maps to an independently authored power "+progression.name+"/"+g[1]);
                int tier=levels[source-2];var choice=progression.LevelEntries.Where(e=>e.Level==tier).SelectMany(e=>e.Features).OfType<BlueprintFeatureSelection>().Single(s=>s.name.StartsWith("CotWWrathPsychicBloodlinePowerChoice",StringComparison.Ordinal)&&s.AllFeatures.Contains(intensity));
                var original=choice.AllFeatures.OfType<BlueprintProgression>().Single(p=>p.LevelEntries.SelectMany(e=>e.Features).Any(f=>f.AssetGuid.ToString()==g[1]));
                int level=int.Parse(g[2]);Check(new ProgressionData(original).GetLevelEntry(level).Features.Count(f=>f.AssetGuid.ToString()==g[3])==1,"native original-branch level lookup retains exact growth/companion "+progression.name+"/"+level);
                Check(!new ProgressionData(progression).GetLevelEntry(level).Features.Any(f=>f.AssetGuid.ToString()==g[3]),"choosing a mutation cannot retain the discarded power's raw growth "+progression.name+"/"+level);growthChecks++;
            }
        }
        Check(slots==(int)input["eligibleSlots"]&&slots==264,"all264 independently inventoried native slots exercised");
        Check(growthChecks==input["growth"].Count()&&growthChecks==184,"all184 native growth/companion rows exercised");
        var sorc=N<BlueprintCharacterClass>(cache,"b3a505fb61437dc4097f43c3f8f9a4cf");var root=N<BlueprintFeatureSelection>(cache,"3a60f0c0442acfb419b0c03b584e1394");
        foreach(var pool in root.AllFeatures.OfType<BlueprintFeatureSelection>()) {
            Check(!Select01451(cache,sorc,13,root,pool),"without a real bloodline's prerequisite facts its bonus submenu stays closed "+pool.name);
            foreach(int level in new[]{7,13,19}) {
                bool matched=false;
                foreach(var item in canonicalRows.Where(r=>(string)r["row"][1]=="Sorcerer"&&r["archetype"].Type==JTokenType.Null)) {
                    var progression=N<BlueprintProgression>(cache,(string)item["row"][0]);var unit=(UnitEntityData)H("Unit",cache,sorc.AssetGuid.ToString(),level);H("Fact",unit,progression);
                    // Adding a progression identity alone in this fixture does
                    // not run full level-up. Supply only its actually authored
                    // first-level non-selection facts, including requisite tags.
                    foreach(var grant in progression.LevelEntries.Where(e=>e.Level==1).SelectMany(e=>e.Features).OfType<BlueprintFeature>().Where(f=>!(f is BlueprintFeatureSelection)))H("Fact",unit,grant);
                    var state=(LevelUpState)Call(typeof(Feat01424Tests),"State",unit,level);var pending=state.AddSelection(null,default(FeatureSource),root,level);
                    if(!root.CanSelect(unit.Descriptor,state,pending,pool))continue;
                    var child=state.AddSelection(null,default(FeatureSource),pool,level);
                    foreach(var feature in new[]{intensity,piercing}){Check(pool.CanSelect(unit.Descriptor,state,child,feature)==(feature==intensity||level>=9),"actual Sorcerer parent-to-bloodline-bonus path respects7/13/19 thresholds "+pool.name+"/"+level+"/"+feature.name);bonusChecks++;}
                    matched=true;break;
                }
                Check(matched,"an actual native bloodline admits each real bonus submenu "+pool.name+"/"+level);
            }
        }
        Check(bonusChecks>=54,"nine native bonus submenus each checked at all three actual feat levels");
        var published=((SimpleBlueprint[])H("Loaded",cache)).Where(x=>x.name.StartsWith("CotWWrathPsychicBloodline",StringComparison.Ordinal)).ToDictionary(x=>x.AssetGuid,x=>x);
        for(int i=0;i<3;i++)Call(T("BloodlineMutations01446"),"Register",cache);
        Check(published.All(x=>ReferenceEquals(cache.Load(x.Key),x.Value)),"repeat registration preserves every existing mutation graph identity");
        Check(cache.Load(Id("BloodHavoc01446"))==null,"no duplicate CotW Havoc effect is restored for entry repair");
        PsychicExpansionTests.Inventory["nativeMutationMatrix01451Contracts"]=new JObject{["passed"]=true,["assertions"]=checks-before,["progressions"]=71,["authoredSlots"]=slots,["missingSlotsNotInvented"]=missing,["actualNativeCanSelectCalls"]=selects,["growthAndCompanionRows"]=growthChecks,["actualSorcererBonusPathCalls"]=bonusChecks,["threeBonusLevels"]=new JArray(7,13,19),["allRealPrerequisitesExecute"]=true,["repeatIdentityPreserved"]=true,["ownHavocAbsent"]=true,["boundary"]="Native CanSelect, actual parent/child selection state and native ProgressionData.GetLevelEntry on independently inventoried real graphs. Unit/class state and engine services are controlled; complete UI commit/game save lifecycle is separate."};
        return checks-before;
    }
}
