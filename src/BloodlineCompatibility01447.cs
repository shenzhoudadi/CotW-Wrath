using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class BloodlineCompatibility01447
    {
        // Havoc remains ECM-owned. CotW supplies Intensity/Piercing while the
        // corresponding ECM assets remain empty placeholders.
        internal static readonly string[] HavocIds={"5d61fcd8c3d545da9f97e1b2fda8218b","c33eddceeb3d4600be9139037e6015b6"};
        static readonly string[][] MutationIds={
            new[]{HavocIds[0],"ad27605e311141f0a9255c2b27eee7b6","6e9996c9143043169574573584f6010d"},
            new[]{HavocIds[1],"c0076ba5a8cc4a6a82719b8032314e34","d2adc41f46814714b78f4da07f51da38"}
        };
        internal const string PatchOwner="CallOfTheWild.Wrath.Prototype.ECM01447";
        internal static bool Provider(BlueprintFeature feature)=>feature!=null&&MutationIds.SelectMany(x=>x).Contains(feature.AssetGuid.ToString())&&feature.ComponentsArray.Any(c=>c?.GetType().Assembly.GetName().Name=="EbonsContentMod"&&!(c is Prerequisite));
        internal static bool HasProvider(UnitEntityData owner)=>owner.Facts.List.Any(f=>f.IsActive&&f.IsTurnedOn&&Provider(f.Blueprint as BlueprintFeature));
        internal static BlueprintFeature[] Options(BlueprintsCache cache,bool bloodrager)=>MutationIds[bloodrager?1:0].Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeature).Where(Provider).Concat(new[]{"BloodIntensity01446","BloodPiercing01446"}.Select(token=>cache.Load(Id(token)) as BlueprintFeature).Where(f=>f!=null)).ToArray();
        // Extend ECM's own qualification query, not its damage calculation.
        // Its original focus-school and native-bloodline cases still run first.
        static void ProviderSpell(BlueprintAbility spell,UnitEntityData unit,ref bool __result)
        {
            if(__result||spell==null||unit==null)return;
            foreach(var feature in unit.Progression.Features)
                if(feature.Blueprint is BlueprintProgression progression&&IsVariant(progression.AssetGuid))
                    foreach(var grant in progression.LevelEntries.SelectMany(Features))
                        if(unit.HasFact(grant)&&grant.GetComponents<AddKnownSpell>().Any(k=>k.Spell!=null&&(k.Spell.AssetGuid==spell.AssetGuid||k.Spell.AssetGuid==spell.Parent?.AssetGuid))){__result=true;return;}
        }
        static bool IsVariant(BlueprintGuid guid)=>BloodlineVariants01447.Rows.Any(r=>r[0]==guid.ToString())||BloodlineExternal01450.IsVariant(guid)||Enumerable.Range(0,4).Any(i=>Id(PrimalSorcerer01444.Token(i,"Progression"))==guid);
        internal static void InstallProviderGuard(BlueprintsCache cache)
        {
            BloodlineSlots01451.ObserveAndArm(()=>InstallProviderGuardCore(cache));
        }
        static void InstallProviderGuardCore(BlueprintsCache cache)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a=>a.GetName().Name=="EbonsContentMod")) {
                var type=assembly.GetType("EbonsContentMod.Utilities.ComponentHelperators",true);
                var target=AccessTools.Method(type,"GetIsBloodlineSpell",new[]{typeof(BlueprintAbility),typeof(UnitEntityData)})??throw new InvalidOperationException("01449 ECM bloodline qualification endpoint changed.");
                var postfix=AccessTools.Method(typeof(BloodlineCompatibility01447),nameof(ProviderSpell));
                var installed=(Harmony.GetPatchInfo(target)?.Postfixes.AsEnumerable()??Enumerable.Empty<Patch>()).Where(p=>p.owner==PatchOwner&&p.PatchMethod==postfix).ToArray();
                if(installed.Length>1)throw new InvalidOperationException("01449 ECM qualification guard duplicated.");if(installed.Length==1)continue;
                var harmony=new Harmony(PatchOwner);try{harmony.Patch(target,postfix:new HarmonyMethod(postfix));}catch{harmony.Unpatch(target,postfix);throw;}
            }
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a=>a.GetName().Name=="EbonsContentMod")) {
                var target=AccessTools.Method(assembly.GetType("EbonsContentMod.Bloodlines.BloodlineMutations",true),"Configure")??throw new InvalidOperationException("01450 ECM mutation publication endpoint changed.");
                var postfix=AccessTools.Method(typeof(BloodlineCompatibility01447),nameof(ProviderConfigured));
                var installed=(Harmony.GetPatchInfo(target)?.Postfixes.AsEnumerable()??Enumerable.Empty<Patch>()).Count(p=>p.owner==PatchOwner&&p.PatchMethod==postfix);
                if(installed>1)throw new InvalidOperationException("01450 ECM publication guard duplicated.");if(installed==1)continue;
                var harmony=new Harmony(PatchOwner);try{harmony.Patch(target,postfix:new HarmonyMethod(postfix));}catch{harmony.Unpatch(target,postfix);throw;}
            }
            BloodlineSlots01451.InstallPublicationGuards(cache);
        }
        static void ProviderConfigured() {
            try{Reconcile(ResourcesLibrary.BlueprintsCache);}
            catch(Exception error){ModEntry.LogSpellDiagnostic("[Bloodline compatibility01450] ECM publication reconciliation failed; existing content retained: "+error);}
        }
        internal static string[][] Rows(BlueprintsCache cache)
        {
            var list=BloodlineAnchors01446.Rows.Concat(BloodlineVariants01447.Rows).Select(r=>r.ToArray()).ToList();
            for(int i=0;i<4;i++) {
                var p=cache.Load(Id(PrimalSorcerer01444.Token(i,"Progression"))) as BlueprintProgression;
                if(p==null)throw new InvalidOperationException("01447 published Primal producer is missing its progression: "+i);
                var source=BloodlineAnchors01446.Rows.Single(r=>r[0]==PrimalSorcerer01444.Progressions[i]);
                list.Add(new[]{p.AssetGuid.ToString(),"Sorcerer",source[2],source[3],Id(PrimalSorcerer01444.Token(i,"SummoningFeature")).ToString()});
            }
            foreach(var row in list)TomeClaws01449.Adapt(cache,row);
            return BloodlineSlots01451.Expand(list.ToArray());
        }
        internal static string[][] Growth(string[][] rows)
        {
            var list=BloodlineAnchors01446.Growth.Concat(BloodlineVariants01447.Growth).Select(g=>g.ToArray()).ToList();
            for(int i=0;i<4;i++) {
                string id=Id(PrimalSorcerer01444.Token(i,"Progression")).ToString();var row=rows.SingleOrDefault(r=>r[0]==id);
                if(row!=null)list.AddRange(BloodlineAnchors01446.Growth.Where(g=>g[0]==PrimalSorcerer01444.Progressions[i]&&row.Skip(2).Contains(g[1])).Select(g=>new[]{id,g[1],g[2],g[3]}));
            }
            foreach(var row in rows.Where(r=>r[2]==TomeClaws01449.First)) {
                list.RemoveAll(g=>g[0]==row[0]&&TomeClaws01449.NativeFirst.Contains(g[1]));
                for(int i=0;i<3;i++)list.Add(new[]{row[0],TomeClaws01449.First,new[]{5,7,11}[i].ToString(),TomeClaws01449.Growth[i]});
            }
            return list.Concat(BloodlineSlots01451.ExtraGrowth(rows)).ToArray();
        }
        internal static BlueprintFeatureBase[] Features(LevelEntry entry)=>
            (((List<BlueprintFeatureBaseReference>)Get(entry,"m_Features"))??new List<BlueprintFeatureBaseReference>()).Select(r=>r?.Get()??throw new InvalidOperationException("01448 unresolved bloodline feature: "+r?.Guid)).ToArray();
        internal static BlueprintFeatureBase Anchor(BlueprintProgression p,int level,BlueprintFeatureBase power)
        {
            var authored=p.LevelEntries.Where(e=>e.Level==level).SelectMany(Features).ToArray();
            var matches=authored.Where(x=>x.AssetGuid==power.AssetGuid||x is BlueprintFeatureSelection s&&s.AllFeatures.Any(a=>a.AssetGuid==power.AssetGuid)).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("01449 bloodline power anchor changed: "+p.name+"/"+level+" expected "+power.AssetGuid+" matches "+matches.Length+" authored "+string.Join(",",authored.Select(x=>x.name+"="+x.AssetGuid)));
            var result=matches[0];
            if(result.AssetGuid!=power.AssetGuid) {
                var wrapper=(BlueprintFeatureSelection)result;
                if(wrapper.ComponentsArray.Length>1||wrapper.ComponentsArray.Any(c=>!BloodlineProvider01453.Restriction(ResourcesLibrary.BlueprintsCache,c))||wrapper.AllFeatures.Count(a=>a.AssetGuid==power.AssetGuid)!=1||wrapper.AllFeatures.Any(a=>a.AssetGuid!=power.AssetGuid&&!Provider(a)))throw new InvalidOperationException("01449 unsupported external bloodline wrapper: "+wrapper.name);
            }
            return result;
        }
        internal static void Reconcile(BlueprintsCache cache)
        {
            if(cache.Load(Id(BloodlineMutations01446.RootToken))==null)return;
            InstallProviderGuard(cache);
            var errors=new List<Exception>();
            foreach(var action in new Action[]{()=>ArcaneBatch01446.ReconcileBloodlineProviderOptions(cache),()=>BloodlineSlots01451.ReconcileBonusPools(cache),()=>BloodlineExternal01450.Reconcile(cache)})try{action();}catch(Exception error){errors.Add(error);}
            if(errors.Count==1)ExceptionDispatchInfo.Capture(errors[0]).Throw();
            if(errors.Count!=0)throw new AggregateException("01453 independent bloodline reconciliation failures; healthy paths retained.",errors);
        }
    }
    // Serialized identity only: no CotW Havoc asset or selection uses this.
    [TypeId("69dfe8f55f4f43e4883588a4b5f70147")]
    public sealed class BloodHavocProviders01447 : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState,UnitDescriptor unit,LevelUpState state)=>false;
        protected override string GetUITextInternal(UnitDescriptor unit)=>"";
    }
}
