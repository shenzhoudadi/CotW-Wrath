using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicExpansion
    {
        internal const string ModuleName="Psychic spells and disciplines";
        internal static void Register(BlueprintsCache cache)
        {
            var f=new PsychicAssets(cache);
            var character=f.Native<BlueprintCharacterClass>(PsychicClass.ClassId);
            var list=f.Native<BlueprintSpellList>(PsychicClass.ListId);
            var progression=f.Native<BlueprintProgression>(PsychicClass.ProgressionId);
            var entries=PsychicSpells.Build(f);
            entries.AddRange(PsychicBurstSpells.Build(f));
            entries.Add(PsychicUtilitySpells.CountlessEyes(f));
            entries.Add(PsychicRestorativeSpells.ForceSword(f));
            entries.Add(PsychicRestorativeSpells.Surgery(f));
            entries.AddRange(PsychicAfflictionSpells.Build(f));
            entries.AddRange(PsychicForceSpells.Build(f));
            entries.AddRange(PsychicCrushSpells.Build(f));
            entries.AddRange(PsychicInsightSpells.Build(f));
            entries.AddRange(PsychicComfortSpells.Build(f));
            entries.AddRange(PsychicWardingSpells.Build(f));
            entries.AddRange(PsychicBloodSpells.Build(f));
            entries.AddRange(PsychicGlobeSpells.Build(f));
            entries.AddRange(PsychicCompulsionSpells.Build(f));
            entries.AddRange(PsychicFluidForm.Build(f));
            PsychicChannelVigor.Build(f);
            PsychicBladeTutor.Build(f);
            PsychicSecondWind.Build(f);
            PsychicGhostbaneDirge.Build(f);
            PsychicBloodyClaws.Build(f);
            PsychicBattleSpells.Build(f);
            PsychicWeaponSpells.Build(f);
            PsychicTacticalSpells.Build(f);
            PsychicAreaSpells.Build(f);
            PsychicAspectSpells.Build(f);
            PsychicWeatherSpells.Build(f);
            PsychicCurseSpells.Build(f);
            PsychicSupportSpells.Build(f);
            PsychicTravelSpells.Build(f);
            PsychicOrbSpells.Build(f);
            PsychicCataclysmSpells.Build(f);
            PsychicShapeSpells.Build(f);
            PsychicUndeadSpells.Build(f);
            PsychicSilence.Build(f);
            PsychicConduitSpells.Build(f);PsychicMagicalShape.Build(f);
            PsychicFieryShuriken.Build(f);PsychicVoidOrb.Build(f);PsychicHexVulnerability.Build(f);PsychicSpiritWeapons.Build(f);PsychicStorageSpells.Build(f);PsychicBoundWeapons.Build(f);entries.Add(new PsychicSpells.Entry{Ability=f.Find<BlueprintAbility>("Contingency"),Level=6}); entries.Add(new PsychicSpells.Entry {Ability=f.Find<BlueprintAbility>("MagesSword"),Level=7}); entries.Add(new PsychicSpells.Entry {Ability=f.Find<BlueprintAbility>("TwilightKnife"),Level=3}); PsychicLinkedWard.Build(f); PsychicFireSeeds.Build(f);PsychicSpiritSpells.Build(f);entries.Add(PsychicTemporalStasis.Build(f));
            var discipline=PsychicDisciplines.Build(f,character);
            foreach(var entry in entries) {
                var component=new SpellListComponent { name="PsychicExclusiveSpellList",SpellLevel=entry.Level }; Set(component,"m_SpellList",Ref<BlueprintSpellListReference>(list));
                entry.Ability.ComponentsArray=entry.Ability.ComponentsArray.Concat(new BlueprintComponent[] { component }).ToArray();
            }
            var commonLists=new PsychicCommonSpellLists(f);
            PsychicPresentation.Expansion(f);
            // Repair only the affected runtime graphs; published GUIDs stay fixed.
            foreach(var pair in f.Assets.Where(x=>new[]{"Suffocation","ControlConstruct","ScouringWinds","UndeadAnatomy","GiantForm","MagicalBeastShape","Babble"}.Any(t=>x.Key.StartsWith(t))))PsychicRepairComponents.UniqueNames(pair.Value);
            f.Enable();
            if(f.Assets.Values.Select(a=>a.AssetGuid).Distinct().Count()!=f.Assets.Count) throw new InvalidOperationException("Duplicate Psychic extension identity.");
            var existing=f.Assets.Values.Select(a=>cache.Load(a.AssetGuid)).ToArray();
            var nativeFogAreas=PsychicWeatherSpells.NativeFogAreaIds.Select(id=>f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect>(id)).ToArray();
            // A reloaded cache keeps the previously published instance, so patches must
            // reference that canonical object rather than this build's staged copy.
            T Canonical<T>(T staged) where T : BlueprintScriptableObject => cache.Load(staged.AssetGuid) as T ?? staged;
            var nativeFog=Canonical(f.Find<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>("NativeFogCloudConcealmentBuff"));
            var shadowArea=Canonical(f.Find<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect>("ShadowyMistFogArea"));
            var shadowBuff=f.Native<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(PsychicWeatherSpells.ShadowyMistBuffId);
            var oldNativeFogComponents=nativeFogAreas.Select(area=>area.ComponentsArray).ToArray();
            var oldShadowComponents=shadowBuff.ComponentsArray;
            // GetComponents can return null on freshly loaded native areas; ComponentsArray is always materialized here.
            Func<bool> nativeFogInstalled=()=>nativeFogAreas.All(area=>area.ComponentsArray.OfType<Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff>().Count(c=>c.Buff==nativeFog)==1) &&
                shadowBuff.ComponentsArray.OfType<Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect>().Count(c=>c.AreaEffect==shadowArea)==1;
            // Reinstalling is idempotent: our previous owners are dropped from the snapshot first, so a
            // reloaded cache keeps exactly one source-owned patch per native area.
            Action installNativeFog=()=>{
                for(int i=0;i<nativeFogAreas.Length;i++){
                    var owner=PsychicWeatherSpells.Owned(nativeFog);owner.name="CotWNativeFogAreaOwner_"+nativeFogAreas[i].AssetGuid;
                    nativeFogAreas[i].ComponentsArray=oldNativeFogComponents[i].Where(c=>c.name!=owner.name).Concat(new BlueprintComponent[]{owner}).ToArray();
                }
                var emitter=new Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect {name="CotWShadowyMistFogEmitter"};Set(emitter,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(shadowArea));
                shadowBuff.ComponentsArray=oldShadowComponents.Where(c=>c.name!="CotWShadowyMistFogEmitter").Concat(new BlueprintComponent[]{emitter}).ToArray();
                if(!nativeFogInstalled())throw new InvalidOperationException("Native Fog Cloud concealment patches failed.");
            };
            Action restoreNativeFog=()=>{
                for(int i=0;i<nativeFogAreas.Length;i++)nativeFogAreas[i].ComponentsArray=oldNativeFogComponents[i];
                shadowBuff.ComponentsArray=oldShadowComponents;
            };
            int index=0;
            foreach(var expected in f.Assets.Values) {
                var actual=existing[index++];
                if(actual!=null && (actual.GetType()!=expected.GetType() || actual.name!=expected.name ||
                    Signature(((BlueprintScriptableObject)actual).ComponentsArray)!=Signature(expected.ComponentsArray)))
                    throw new InvalidOperationException("Psychic extension GUID conflict; preserving existing content: "+expected.AssetGuid);
            }
            if(existing.Any(a=>a!=null) && existing.Any(a=>a==null)) throw new InvalidOperationException("Psychic extension assets are only partially present.");
            var oldLists=list.SpellsByLevel.Select(l=>(List<BlueprintAbilityReference>)Get(l,"m_Spells")).ToArray();
            var first=progression.LevelEntries.Single(e=>e.Level==1);
            var oldFeatures=first.Features.ToArray(); var oldUI=(BlueprintFeatureBaseReference[])Get(progression,"m_UIDeterminatorsGroup");
            var expectedSelection=Id("DisciplineSelection");
            if(existing.All(a=>a!=null)) {
                commonLists.Verify(true);
                if(!nativeFogInstalled()) try { installNativeFog(); } catch { restoreNativeFog(); throw; }
                if(!oldFeatures.Any(a=>a.AssetGuid==expectedSelection) || oldFeatures.Any(a=>a.AssetGuid==BlueprintGuid.Parse(PsychicClass.PoolSelectionId)) ||
                    entries.Any(e=>oldLists[e.Level].Count(r=>r.Guid==e.Ability.AssetGuid)!=1))
                    throw new InvalidOperationException("Psychic extension shared links are incomplete or duplicated.");
                return;
            }
            if(oldFeatures.Count(a=>a.AssetGuid==BlueprintGuid.Parse(PsychicClass.PoolSelectionId))!=1 || oldFeatures.Any(a=>a.AssetGuid==expectedSelection) ||
                entries.Any(e=>oldLists[e.Level].Any(r=>r.Guid==e.Ability.AssetGuid)))
                throw new InvalidOperationException("Psychic extension cannot replace an unexpected prototype progression.");
            var added=new List<BlueprintScriptableObject>();
            commonLists.Verify(false);
            try {
                foreach(var asset in f.Assets.Values) { cache.AddCachedBlueprint(asset.AssetGuid,asset); added.Add(asset); }
                installNativeFog();
                for(int level=0;level<10;level++) ReplaceSpells(list.SpellsByLevel[level],oldLists[level].Concat(entries.Where(e=>e.Level==level).Select(e=>Ref<BlueprintAbilityReference>(e.Ability))).ToList());
                commonLists.Apply();
                first.SetFeatures(oldFeatures.Select(a=>a.AssetGuid==BlueprintGuid.Parse(PsychicClass.PoolSelectionId)?discipline:a).ToArray());
                Set(first,"m_FeaturesList",null);
                Set(progression,"m_UIDeterminatorsGroup",(oldUI??Array.Empty<BlueprintFeatureBaseReference>()).Select(r=>r.Guid==BlueprintGuid.Parse(PsychicClass.PoolSelectionId)?Ref<BlueprintFeatureBaseReference>(discipline):r).ToArray());
            } catch {
                restoreNativeFog();
                commonLists.Restore();
                for(int level=0;level<10;level++) ReplaceSpells(list.SpellsByLevel[level],oldLists[level]);
                first.SetFeatures(oldFeatures); Set(first,"m_FeaturesList",null); Set(progression,"m_UIDeterminatorsGroup",oldUI);
                foreach(var asset in added) cache.RemoveCachedBlueprint(asset.AssetGuid); throw;
            }
        }
        private static void ReplaceSpells(SpellLevelList level,List<BlueprintAbilityReference> spells) {
            Set(level,"m_Spells",spells); Set(level,"m_SpellsProxy",null); Set(level,"m_SpellsFiltered",null);
        }
        // Compare component values and nested actions as well as names/types. Blueprint
        // references are identities, not entire cached object graphs.
        internal static string Signature(object value) => Describe(value,new HashSet<object>());
        private static string Describe(object value,HashSet<object> ancestors)
        {
            if(value==null) return "null";
            if(value is BlueprintReferenceBase reference) return "ref:"+reference.Guid;
            if(value is SimpleBlueprint blueprint) return "bp:"+blueprint.AssetGuid;
            var type=value.GetType();
            if(type.IsPrimitive || type.IsEnum || value is string || value is decimal) return type.FullName+":"+value;
            if(value is UnityEngine.Object) return type.FullName;
            if(!ancestors.Add(value)) return "cycle:"+type.FullName;
            try {
                if(value is IEnumerable sequence) return "["+string.Join(",",sequence.Cast<object>().Select(v=>Describe(v,ancestors)))+"]";
                var fields=new List<FieldInfo>();
                for(var t=type;t!=null && t!=typeof(object) && t!=typeof(BlueprintComponent) && t!=typeof(UnityEngine.Object);t=t.BaseType)
                    fields.AddRange(t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(f=>!f.IsStatic));
                return type.FullName+"{"+string.Join(";",fields.OrderBy(f=>f.DeclaringType.FullName+f.Name).Select(f=>f.Name+"="+Describe(f.GetValue(value),ancestors)))+"}";
            } finally { ancestors.Remove(value); }
        }
    }
}
