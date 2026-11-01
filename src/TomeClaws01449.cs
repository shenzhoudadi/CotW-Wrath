using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;

namespace CallOfTheWild.Wrath.Prototype
{
    // Actual ToTF SorcererClawsOverhaul + published Config/Blueprints.json.
    // Only these ten draconic first powers map to the replacement. Arcana,
    // class skills and prerequisite facts remain in their original parents.
    internal static class TomeClaws01449
    {
        internal const string First="b808e4d9bf7e4a189cdb2f4a25ec0b86";
        internal static readonly string[] Growth={"77b7d401476a4cbe90ca6be7a990ba53","217fc84676094b0e8ccb9eb6bf815ef3","4386eb19ba714b54b719ce77e943d42d"};
        internal static readonly string[] NativeFirst=BloodlineVariants01447.Rows.Skip(3).Take(10).Select(r=>r[2]).ToArray();
        internal static void Adapt(BlueprintsCache cache,string[] row)
        {
            if(row[1]!="Sorcerer"||!NativeFirst.Contains(row[2]))return;
            var p=(BlueprintProgression)cache.Load(Kingmaker.Blueprints.BlueprintGuid.Parse(row[0]));
            var level=p.LevelEntries.Where(e=>e.Level==1).SelectMany(BloodlineCompatibility01447.Features).ToArray();
            bool Has(string id)=>level.Any(f=>f.AssetGuid.ToString()==id||f is Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection s&&s.AllFeatures.Any(a=>a.AssetGuid.ToString()==id));
            if(Has(row[2])||!Has(First))return; // Anchor diagnoses unknown shapes.
            row[2]=First;
        }
    }
}
