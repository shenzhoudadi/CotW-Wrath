using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    // Native UnitPartTemporarySpellsKnown owns the serialized source ledger;
    // native AbilityData owns promotion to permanent knowledge. Respect every
    // live entry in that ledger, including another provider's overlapping grant.
    internal static class TemporaryKnowledge01464
    {
        static readonly FieldInfo EntriesField=AccessTools.Field(typeof(UnitPartTemporarySpellsKnown),"m_Entries");
        // CopySpellbook also serves level-up previews. Its native AddKnown
        // calls erase IsTemporary. A weak, nonserialized copy receipt restores
        // only these exact source-owned entries until the preview is discarded.
        static readonly ConditionalWeakTable<Spellbook,HashSet<AbilityData>> Copies=new ConditionalWeakTable<Spellbook,HashSet<AbilityData>>();
        sealed class AbilityIdentity:IEqualityComparer<AbilityData>
        {
            internal static readonly AbilityIdentity Instance=new AbilityIdentity();
            public bool Equals(AbilityData a,AbilityData b)=>ReferenceEquals(a,b);
            public int GetHashCode(AbilityData value)=>RuntimeHelpers.GetHashCode(value);
        }
        static IEnumerable<AbilityData> Native(Spellbook book)
        {
            var part=book?.Owner?.Unit.Get<UnitPartTemporarySpellsKnown>();
            if(part==null)return Enumerable.Empty<AbilityData>();
            return ((List<UnitPartTemporarySpellsKnown.Entry>)EntriesField.GetValue(part))
                .Where(e=>!e.Removed).SelectMany(e=>e.Spells).Where(a=>a!=null&&ReferenceEquals(a.Spellbook,book));
        }
        internal static bool Owns(Spellbook book,AbilityData spell)=>Native(book).Any(a=>ReferenceEquals(a,spell))||
            Copies.TryGetValue(book,out var copied)&&copied.Contains(spell);

        internal static void RestoreCopy(Spellbook original,Spellbook destination)
        {
            var live=new HashSet<AbilityData>(original.GetAllKnownSpells(),AbilityIdentity.Instance);
            var owned=Native(original).Concat(Copies.TryGetValue(original,out var prior)?prior:Enumerable.Empty<AbilityData>())
                .Where(a=>live.Contains(a)&&a.IsTemporary&&a.MetamagicData==null)
                .Select(a=>new {a.Blueprint.AssetGuid,a.SpellLevel}).ToArray();
            Copies.Remove(destination);
            if(owned.Length==0)return;
            var receipt=Copies.GetValue(destination,_=>new HashSet<AbilityData>(AbilityIdentity.Instance));
            foreach(var copied in destination.GetAllKnownSpells().Where(a=>a.MetamagicData==null&&
                owned.Any(v=>v.AssetGuid==a.Blueprint.AssetGuid&&v.SpellLevel==a.SpellLevel)))
            {copied.IsTemporary=true;receipt.Add(copied);}
        }
        [HarmonyPatch]
        internal static class PruneSourcesPatch
        {
            static MethodBase TargetMethod()=>AccessTools.Method(typeof(UnitPartTemporarySpellsKnown),nameof(UnitPartTemporarySpellsKnown.RemoveEntry),new[]{typeof(Kingmaker.EntitySystem.EntityFact),typeof(AddSpellKnownTemporary)});
            static void Prefix(UnitPartTemporarySpellsKnown __instance)
            {
                // The native removal uses AbilityData value equality. A source
                // may still retain an old object after RemoveSpell/relearning;
                // it must not delete a different, now-permanent known object.
                var live=new Dictionary<Spellbook,HashSet<AbilityData>>();
                foreach(var entry in (List<UnitPartTemporarySpellsKnown.Entry>)EntriesField.GetValue(__instance))
                    entry.Spells.RemoveAll(a=>{
                        var book=a?.Spellbook;if(book==null||!PsychicCasting.IsPsychic(book.Blueprint))return false;
                        if(!live.TryGetValue(book,out var known))live.Add(book,known=new HashSet<AbilityData>(book.GetAllKnownSpells(),AbilityIdentity.Instance));
                        return !known.Contains(a);
                    });
            }
        }
        static void Reconcile(UnitPartTemporarySpellsKnown part)
        {
            foreach(var book in part.Owner.Descriptor.Spellbooks.Where(b=>PsychicCasting.IsPsychic(b.Blueprint)))PsychicUndercasting.Reconcile(book);
        }
        [HarmonyPatch]
        internal static class SourcesAddedPatch
        {
            static MethodBase TargetMethod()=>AccessTools.Method(typeof(UnitPartTemporarySpellsKnown),nameof(UnitPartTemporarySpellsKnown.AddEntry));
            static void Postfix(UnitPartTemporarySpellsKnown __instance)=>Reconcile(__instance);
        }
        [HarmonyPatch]
        internal static class SourcesRemovedPatch
        {
            static MethodBase TargetMethod()=>AccessTools.Method(typeof(UnitPartTemporarySpellsKnown),nameof(UnitPartTemporarySpellsKnown.RemoveEntry),new[]{typeof(Kingmaker.EntitySystem.EntityFact),typeof(AddSpellKnownTemporary)});
            static void Postfix(UnitPartTemporarySpellsKnown __instance)=>Reconcile(__instance);
        }
        [HarmonyPatch]
        internal static class SourcesLoadedPatch
        {
            static MethodBase TargetMethod()=>AccessTools.Method(typeof(UnitPartTemporarySpellsKnown),"OnPostLoad");
            static void Postfix(UnitPartTemporarySpellsKnown __instance)=>Reconcile(__instance);
        }
        [HarmonyPatch]
        internal static class SourcesLeveledPatch
        {
            static MethodBase TargetMethod()=>AccessTools.Method(typeof(UnitPartTemporarySpellsKnown),nameof(UnitPartTemporarySpellsKnown.HandleUnitGainLevel));
            static void Postfix(UnitPartTemporarySpellsKnown __instance)=>Reconcile(__instance);
        }
    }
}
