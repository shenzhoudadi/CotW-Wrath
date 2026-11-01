using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using CallOfTheWild.Wrath.Prototype;

internal static class CombatFeatPoolTests
{
    static int count;
    static readonly Type Module = typeof(ChosenWeaponBlessing).Assembly.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolRepair");
    static void Check(bool pass, string message) { if (!pass) throw new Exception("Combat pool repair: " + message); count++; }
    static BlueprintGuid Id(string t) => (BlueprintGuid)AccessTools.Method(typeof(ChosenWeaponBlessing).Assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets"), "Id").Invoke(null, new object[] { t });
    static void Repair(BlueprintsCache c, BlueprintFeatureSelection p) => AccessTools.Method(Module, "Repair").Invoke(null, new object[] { c, p });
    static void Reconcile(BlueprintsCache c) => AccessTools.Method(Module, "Reconcile").Invoke(null, new object[] { c });
    static BlueprintFeatureReference Ref(BlueprintGuid id) { var r = new BlueprintFeatureReference(); r.ReadGuidFromJson(id.ToString()); return r; }
    static BlueprintFeatureReference[] Refs(BlueprintFeatureSelection p, string f) => (BlueprintFeatureReference[])AccessTools.Field(typeof(BlueprintFeatureSelection), f).GetValue(p);
    static void SetRefs(BlueprintFeatureSelection p, string f, BlueprintFeatureReference[] a) => AccessTools.Field(typeof(BlueprintFeatureSelection), f).SetValue(p, a);
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Open(BlueprintFeatureSelection p) { p.ExtractSelectionItems(null,null).ToArray(); }
    static void Roots(BlueprintFeatureSelection p, string label) {
        foreach (var token in new[] { "WeaponOfChosenFeat", "ImprovedWeaponOfChosenFeat", "GreaterWeaponOfChosenFeat" }) {
            foreach (var f in new[] { "m_Features", "m_AllFeatures" }) Check(Refs(p,f).Count(x => x?.Guid == Id(token)) == 1, label + " / " + f + " / " + token);
            Check(p.Items.Any(x => x.Feature.AssetGuid == Id(token)), "native Items contains " + label + " / " + token);
        }
    }
    internal static int Run(BlueprintsCache c) {
        count=0;
        // These rows are taken from ALL installed-game combat selections, independently
        // of the production repair's ID list. Restricted advanced training is excluded.
        var native = PsychicExpansionTests.NativeRows.Where(r => ((string)r["blueprint"]["Data"]["$type"]).EndsWith(", BlueprintFeatureSelection") &&
            new[]{(string)r["blueprint"]["Data"]["Group"],(string)r["blueprint"]["Data"]["Group2"]}.Contains("CombatFeat") &&
            ((JArray)r["blueprint"]["Data"]["m_AllFeatures"]).Count > 100).ToArray();
        Check(native.Length==14,"all fourteen installed native unrestricted combat pools independently extracted");
        foreach(var row in native) {
            string guid=(string)row["blueprint"]["AssetId"];var p=PsychicExpansionTests.NativeCase<BlueprintFeatureSelection>(guid);
            foreach(var field in new[]{"Group","Group2","m_Features","m_AllFeatures"}) {
                var f=AccessTools.Field(typeof(BlueprintFeatureSelection),field);f.SetValue(p,PsychicExpansionTests.Read(row["blueprint"]["Data"][field],f.FieldType));
            }
            c.AddCachedBlueprint(p.AssetGuid,p);
            var original=Refs(p,"m_AllFeatures").Select(x=>x.Guid).ToArray();Repair(c,p);Repair(c,p);Roots(p,(string)row["name"]);
            Check(Refs(p,"m_AllFeatures").Take(original.Length).Select(x=>x.Guid).SequenceEqual(original),"native original list/order retained");
            Check(p.Group.ToString()==(string)row["blueprint"]["Data"]["Group"]&&p.Group2.ToString()==(string)row["blueprint"]["Data"]["Group2"],"native group filters retained");
        }
        var fighter=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse("41c8486641f7d6d4283ca9dae4147a9f"));
        var oldAll=Refs(fighter,"m_AllFeatures");SetRefs(fighter,"m_AllFeatures",oldAll.Where(x=>!new[]{"WeaponOfChosenFeat","ImprovedWeaponOfChosenFeat","GreaterWeaponOfChosenFeat"}.Any(t=>x.Guid==Id(t))).ToArray());
        SetRefs(fighter,"m_Features",Array.Empty<BlueprintFeatureReference>());
        var cacheField=AccessTools.Field(typeof(ResourcesLibrary),"BlueprintsCache");var savedCache=cacheField.GetValue(null);cacheField.SetValue(null,c);
        var corePatch=typeof(ChosenWeaponBlessing).Assembly.GetType("CallOfTheWild.Wrath.Prototype.CombatFeatPoolOpeningPatch");var h=new Harmony("CotW.CombatPool0147.Tests");h.CreateClassProcessor(corePatch).Patch();
        try {
            // Actual original ExtractSelectionItems runs; no selection or prerequisites bypass.
            Open(fighter);Roots(fighter,"native opening repairs replaced Fighter list");
            SetRefs(fighter,"m_AllFeatures",oldAll.Where(x=>!new[]{"WeaponOfChosenFeat","ImprovedWeaponOfChosenFeat","GreaterWeaponOfChosenFeat"}.Any(t=>x.Guid==Id(t))).ToArray());SetRefs(fighter,"m_Features",Array.Empty<BlueprintFeatureReference>());
            Check(!fighter.CanSelect(null,null,null,null),"original CanSelect null item rejection retained");Roots(fighter,"CanSelect hook repairs rebuilt list before validation");
            SetRefs(fighter,"m_AllFeatures",oldAll.Where(x=>!new[]{"WeaponOfChosenFeat","ImprovedWeaponOfChosenFeat","GreaterWeaponOfChosenFeat"}.Any(t=>x.Guid==Id(t))).ToArray());SetRefs(fighter,"m_Features",Array.Empty<BlueprintFeatureReference>());
            var direct=(IEnumerable<IFeatureSelectionItem>)AccessTools.PropertyGetter(typeof(BlueprintFeatureSelection),"Items").Invoke(fighter,null);direct.ToArray();
            Check(Refs(fighter,"m_AllFeatures").Any(x=>x.Guid==Id("WeaponOfChosenFeat")),"direct native Items getter restores pool");Roots(fighter,"direct Items upgrade view path");
            var late=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse("ffaca46008c34b938db5dc228c5f0147"),name="LateCotWCombatClone",Group=FeatureGroup.CombatFeat};
            SetRefs(late,"m_AllFeatures",oldAll.Where(x=>!new[]{"WeaponOfChosenFeat","ImprovedWeaponOfChosenFeat","GreaterWeaponOfChosenFeat"}.Any(t=>x.Guid==Id(t))).ToArray());c.AddCachedBlueprint(late.AssetGuid,late);
            Open(late);Roots(late,"late combat clone native opening");Reconcile(c);Reconcile(c);Roots(late,"late clone reconciliation idempotent");
            foreach(string guid in (string[])AccessTools.Field(Module,"OptionalIds").GetValue(null)) {
                var p=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse(guid),name="OptionalProviderPool"};c.AddCachedBlueprint(p.AssetGuid,p);
                Reconcile(c);Reconcile(c);Roots(p,"optional CotW bonus pool "+guid);c.RemoveCachedBlueprint(p.AssetGuid);
            }
            var sparse=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse("aaaca46008c34b938db5dc228c5f0147"),Group=FeatureGroup.CombatFeat};SetRefs(sparse,"m_AllFeatures",new[]{Ref(BlueprintGuid.Parse("9972f33f977fc724c838e59641b2fca5"))});var before=Refs(sparse,"m_AllFeatures");Open(sparse);Check(ReferenceEquals(before,Refs(sparse,"m_AllFeatures")),"restricted combat submenu not widened");
            foreach(var guid in new[]{"70a139f0a4c6c534eaa34feea0d08622","ee9ab0117ca06b84f9c66469f4428c61","0b55d725ded1ae549bb858fba1d84114"}) {
                var p=(BlueprintFeatureSelection)c.Load(BlueprintGuid.Parse(guid));before=Refs(p,"m_AllFeatures");Repair(c,p);Check(ReferenceEquals(before,Refs(p,"m_AllFeatures")),"advanced training not widened");
            }
            foreach(var group in new[]{FeatureGroup.WizardFeat,FeatureGroup.MagusArcana,FeatureGroup.Feat,FeatureGroup.None}) {
                var p=new BlueprintFeatureSelection{AssetGuid=BlueprintGuid.Parse("abaca46008c34b938db5dc228c5f0147"),Group=group};SetRefs(p,"m_AllFeatures",oldAll);Repair(c,p);Check(ReferenceEquals(oldAll,Refs(p,"m_AllFeatures")),"unrelated group unchanged "+group);
            }
            var missing=new BlueprintsCache();var target=new BlueprintFeatureSelection{AssetGuid=fighter.AssetGuid};Repair(missing,target);Check(target.AllFeatures.Count()==0,"missing module leaves list intact");
        } finally {h.UnpatchAll(h.Id);cacheField.SetValue(null,savedCache);}
        foreach(var token in new[]{"WeaponOfChosenFeat","ImprovedWeaponOfChosenFeat","GreaterWeaponOfChosenFeat"}) {
            var feat=(BlueprintFeature)c.Load(Id(token));var saved=feat.Groups;feat.Groups=null;Repair(c,fighter);
            Check(feat.HasGroup(FeatureGroup.Feat)&&feat.HasGroup(FeatureGroup.CombatFeat),"null metadata restored "+token);
            feat.Groups=new[]{FeatureGroup.Feat};Repair(c,fighter);Check(feat.HasGroup(FeatureGroup.Feat)&&feat.HasGroup(FeatureGroup.CombatFeat),"combat metadata restored "+token);feat.Groups=saved;
        }
        foreach(var token in new[]{"AmplifiedHexFeat","RageCastingFeat","DazzlingIntimidationFeat","VersatileTrainingSelection","AdaptableTrainingSelection","MagusFamiliarSelection","MagusManeuverMasterySelection"}) {
            var f=c.Load(Id(token)) as BlueprintFeature;Check(f!=null&&!f.HasGroup(FeatureGroup.CombatFeat),"not a combat feat "+token);
        }
        PsychicExpansionTests.Inventory["combatPool0147Assertions"]=count;
        return count;
    }
}
