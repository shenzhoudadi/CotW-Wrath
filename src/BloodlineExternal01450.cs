using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Exact official ECM/TTT/ToTF identities; absent providers are not required.
    // Each late progression has its own atomic receipt. A failure cannot undo
    // the already-published native graph or a different optional provider.
    internal static class BloodlineExternal01450
    {
        internal const string Prefix="Bloodline compatibility01450:";
        internal static readonly string[][] Rows=new[]{
            new[]{"0cb461f617bd45a89583451d946357ec","Sorcerer","33a4f8a3bef848a3bb3c799c4ca5fb8d","ce8cf3b14c0c4b14b102f05896931352","9ae9c5afe89f4729b7d1bb6a8194fb2f"},
            new[]{"b972ca31d32c4927a9dbc98a251e14f0","Sorcerer","33a4f8a3bef848a3bb3c799c4ca5fb8d","","9ae9c5afe89f4729b7d1bb6a8194fb2f"},
            new[]{"e3811e140c234aed9a667c1f83f34221","Sorcerer","dcabcd9111d145ea8ff68756728f0e51","7f9fd77cf8d74e13ad01ecb2fa4d2063","edbdfd50913541ddb893885eedc89d60"},
            new[]{"e3c4ef718ca94aba8307a8b2e8ac5700","Sorcerer","dcabcd9111d145ea8ff68756728f0e51","","edbdfd50913541ddb893885eedc89d60"},
            new[]{"1ff2d53c2bee429a801eeebb1197bcd1","Sorcerer","d3726f4b599d4c4db5a82abd961097d5","e9bc0888007e48be9281cf75f4390fea","5df18db49b614148af964fb9e9d5a1c6"},
            new[]{"65128d5d9d274c7a9a1d7bbefb021c96","Sorcerer","d3726f4b599d4c4db5a82abd961097d5","","5df18db49b614148af964fb9e9d5a1c6"},
            new[]{"62f017f07222457489eed83165e25682","Sorcerer","2363df02d1c24cbd96cb13b6200a6c37","830f610edf8249a8a68bb224536e3efe","2ca5b40b9d0645dea55cbaa29c301d0f"},
            new[]{"1a62c90abfa8426f8f6c7962cb162447","Sorcerer","2363df02d1c24cbd96cb13b6200a6c37","","2ca5b40b9d0645dea55cbaa29c301d0f"},
            new[]{"bf87858d5ea2411da3ad02d7a7adca13","Bloodrager","b582cef98c07472488949a33ab52876b","27f57feda3c04040ba804f7e6c7993ff","51e224defb1f44ef94e3eb7ac32f976b"},
            new[]{"6d9fe5a3d0eb4e6eba6c827e1d0f74bf","Bloodrager","b582cef98c07472488949a33ab52876b","27f57feda3c04040ba804f7e6c7993ff","51e224defb1f44ef94e3eb7ac32f976b"}
        }.Concat(BloodlineSlotAnchors01451.NewExternalRows).ToArray();
        internal static readonly string[][] Growth={
            new[]{"0cb461f617bd45a89583451d946357ec","ce8cf3b14c0c4b14b102f05896931352","9","2a13439bd5f54b3bb561fd33d08c865a"},
            new[]{"e3811e140c234aed9a667c1f83f34221","7f9fd77cf8d74e13ad01ecb2fa4d2063","9","6882cd7cecdc42de8ac0cdd5075b9224"},
            new[]{"1ff2d53c2bee429a801eeebb1197bcd1","e9bc0888007e48be9281cf75f4390fea","11","e9bc0888007e48be9281cf75f4390fea"},
            new[]{"1ff2d53c2bee429a801eeebb1197bcd1","e9bc0888007e48be9281cf75f4390fea","17","e9bc0888007e48be9281cf75f4390fea"},
            new[]{"1ff2d53c2bee429a801eeebb1197bcd1","5df18db49b614148af964fb9e9d5a1c6","13","5df18db49b614148af964fb9e9d5a1c6"},
            new[]{"65128d5d9d274c7a9a1d7bbefb021c96","5df18db49b614148af964fb9e9d5a1c6","13","5df18db49b614148af964fb9e9d5a1c6"},
            new[]{"bf87858d5ea2411da3ad02d7a7adca13","27f57feda3c04040ba804f7e6c7993ff","8","27f57feda3c04040ba804f7e6c7993ff"},
            new[]{"bf87858d5ea2411da3ad02d7a7adca13","b582cef98c07472488949a33ab52876b","8","722cda3a485e42c4b6bb6df504f09d8b"},
            new[]{"bf87858d5ea2411da3ad02d7a7adca13","b582cef98c07472488949a33ab52876b","20","c83cc373585e42b58f33f16766f566f9"},
            new[]{"6d9fe5a3d0eb4e6eba6c827e1d0f74bf","27f57feda3c04040ba804f7e6c7993ff","8","27f57feda3c04040ba804f7e6c7993ff"},
            new[]{"6d9fe5a3d0eb4e6eba6c827e1d0f74bf","b582cef98c07472488949a33ab52876b","8","722cda3a485e42c4b6bb6df504f09d8b"},
            new[]{"6d9fe5a3d0eb4e6eba6c827e1d0f74bf","b582cef98c07472488949a33ab52876b","20","c83cc373585e42b58f33f16766f566f9"}
        };
        internal static bool IsVariant(BlueprintGuid guid)=>Rows.Any(r=>r[0]==guid.ToString());
        internal static void Reconcile(BlueprintsCache cache)=>ReconcileRows(cache,false);
        internal static void ReconcileSorcerer01452(BlueprintsCache cache)=>ReconcileRows(cache,true);
        static void ReconcileRows(BlueprintsCache cache,bool sorcererOnly) {
            var errors=new System.Collections.Generic.List<Exception>();
            foreach(var row in BloodlineSlots01451.Expand(Rows).Where(r=>!sorcererOnly||r[1]=="Sorcerer")) {
                try {
                if(!(cache.Load(BlueprintGuid.Parse(row[0])) is BlueprintProgression))continue;
                string module=Prefix+row[0];if(ArcaneBatch01446.Existing(cache,module))continue;
                var f=new PsychicAssets(cache);
                var familiar=f.Native<BlueprintFeature>(Id(BloodlineMutations01446.RootToken).ToString());
                var features=new[]{"BloodIntensity01446","BloodPiercing01446"}.Select(t=>f.Native<BlueprintFeature>(Id(t).ToString())).ToArray();
                BloodlineMutations01446.PublishRows(cache,module,f,new[]{row},Growth.Where(g=>g[0]==row[0]).Concat(BloodlineSlots01451.ExtraGrowth(new[]{row})).ToArray(),familiar,features);
                }catch(Exception error){errors.Add(new InvalidOperationException("01450 optional bloodline failed: "+row[0],error));}
            }
            if(errors.Count!=0)throw new AggregateException("01450 optional bloodline registration failures; published native and other optional paths retained.",errors);
        }
    }
}
