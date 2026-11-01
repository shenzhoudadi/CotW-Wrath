using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class SpellListCompatibility {
  // Some providers append a new SpellLevelList instead of adding to its level.
  // Merge by numeric level and blueprint identity, never by localized name.
  internal static SpellLevelList[] Build(SpellLevelList[] source,int requiredLevel=0) {
   var rows=(source??Array.Empty<SpellLevelList>()).Where(r=>r!=null).ToArray();
   if(requiredLevel<0||requiredLevel>10||rows.Any(r=>r.SpellLevel<0 ||
    (r.SpellLevel>10 && r.SpellsRefs!=null && r.SpellsRefs.Any(s=>s!=null))))return null;
   // The current game's class tables reserve indices 0..10, including empty
   // high-level slots. MM/Ebons and other consumers read those indices directly.
   // Preserve the entire input capacity, including appended duplicate rows: after
   // merging, their spare slots remain empty rather than shortening the array.
   // Capacity is not the highest castable spell level. Never resolve references.
   int length=Math.Max(11,Math.Max(source?.Length??0,requiredLevel+1));
   length=Math.Max(length,rows.Where(r=>r.SpellLevel<=10).Select(r=>r.SpellLevel+1).DefaultIfEmpty(0).Max());
   return Enumerable.Range(0,length).Select(level=>{
    var refs=new List<BlueprintAbilityReference>();var seen=new HashSet<BlueprintGuid>();
    foreach(var row in rows.Where(r=>r.SpellLevel==level))
     foreach(var spell in (List<BlueprintAbilityReference>)Get(row,"m_Spells")??new List<BlueprintAbilityReference>())
      if(spell!=null&&seen.Add(spell.Guid))refs.Add(spell);
    var result=new SpellLevelList(level);Set(result,"m_Spells",refs);return result;
   }).ToArray();
  }
  internal static bool Canonical(SpellLevelList[] rows)=>rows!=null&&rows.Length>=11&&rows.All(r=>r!=null)&&
   rows.Select((r,i)=>r.SpellLevel==i && (i<=10 || r.SpellsRefs==null || !r.SpellsRefs.Any(s=>s!=null))).All(x=>x);
  private static BlueprintSpellList[] Snapshot(BlueprintsCache cache) {
   // ForEachLoaded does not synchronize with Load. ToyBox's background loader
   // writes the dictionary even when it only fills an existing cache entry.
   // Use the very same lock as native Load, and never resolve references here.
   var gate=AccessTools.Field(typeof(BlueprintsCache),"m_Lock")?.GetValue(cache);
   if(gate==null)throw new InvalidOperationException("Blueprint cache synchronization object is unavailable.");
   var loaded=AccessTools.Field(typeof(BlueprintsCache),"m_LoadedBlueprints")?.GetValue(cache) as IDictionary;
   if(loaded==null)throw new InvalidOperationException("Blueprint cache dictionary is unavailable.");
   var blueprint=AccessTools.Field(loaded.GetType().GetGenericArguments()[1],"Blueprint");
   object[] entries=null;
   for(int attempt=0;;attempt++) {
    try {
     lock(gate) {
      // ToyBox also replaces existing entries without taking m_Lock. The native
      // dictionary ValueCollection.CopyTo copies slots without a version-checked
      // enumerator, so those replacements do not invalidate this snapshot.
      entries=new object[loaded.Count];loaded.Values.CopyTo(entries,0);
     }
     break;
    } catch(Exception e) when(attempt<2 && (e is InvalidOperationException || e is ArgumentException || e is IndexOutOfRangeException)) {
     // An unlocked insertion can change the required array size during CopyTo.
     // Discard incomplete copies and retry finitely, never loop until quiescent.
    }
   }
   var lists=new List<BlueprintSpellList>();
   foreach(var entry in entries)if(entry!=null && blueprint.GetValue(entry) is BlueprintSpellList list)lists.Add(list);
   return lists.ToArray();
  }
  internal static int Reconcile(BlueprintsCache cache) {
   if(cache==null)return 0;
   BlueprintSpellList[] lists;
   try {lists=Snapshot(cache);}
   catch(Exception e){ModEntry.LogSpellDiagnostic("Spell list reconciliation skipped: cache snapshot failed. "+e);return 0;}
   int repaired=0;
   foreach(var list in lists) {
    try {
     var before=list.SpellsByLevel;
     if(before==null||before.Length==0||Canonical(before))continue;
     var merged=Build(before);
     if(merged==null){ModEntry.LogSpellDiagnostic("Spell list reconciliation skipped unsupported levels: "+list.AssetGuid);continue;}
     // A later provider may replace the array; do not overwrite its update.
     if(!ReferenceEquals(before,list.SpellsByLevel))continue;
     list.SpellsByLevel=merged;Set(list,"m_MaxLevel",0);repaired++;
    } catch(Exception e){ModEntry.LogSpellDiagnostic("Spell list reconciliation skipped list "+list.AssetGuid+": "+e);}
   }
   return repaired;
  }
 }
 [HarmonyPatch(typeof(StartGameLoader),nameof(StartGameLoader.LoadPackTOC))]
 internal static class SpellListCompatibilityAfterLoad {
  [HarmonyPostfix,HarmonyPriority(Priority.Last)]
  [HarmonyAfter("DragonChanges","DragonLibrary","PrestigePlus","MysticalMayhem")]
  private static void Postfix() {
   try {SpellListCompatibility.Reconcile(ResourcesLibrary.BlueprintsCache);}
   catch(Exception e){ModEntry.LogSpellDiagnostic("Optional spell list reconciliation failed; startup continues. "+e);}
  }
 }
}
