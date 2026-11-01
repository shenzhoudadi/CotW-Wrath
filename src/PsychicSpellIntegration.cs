using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal sealed class PsychicSpellLink
    {
        internal readonly string Guid, Name, Provider;
        internal readonly SpellSchool School;
        internal readonly bool PublishedListRequired;
        // -1 means this spell does not belong on the original class list.
        internal readonly int Psychic, Occultist, Spiritualist;
        internal PsychicSpellLink(string guid, string name, string provider, SpellSchool school, bool published,
            int psychic, int occultist, int spiritualist) {
            Guid=guid; Name=name; Provider=provider; School=school; PublishedListRequired=published;
            Psychic=psychic; Occultist=occultist; Spiritualist=spiritualist;
        }
    }
    internal static class PsychicSpellIntegration
    {
        internal const string ModuleName = "Psychic optional spell integration";
        internal static string LastSummary = "Not scanned";
        internal static bool Available(BlueprintsCache cache, PsychicSpellLink link, out BlueprintAbility ability)
        {
            ability=cache.Load(BlueprintGuid.Parse(link.Guid)) as BlueprintAbility;
            if(ability==null || ability.name!=link.Name || ability.Type!=AbilityType.Spell ||
                ability.Parent!=null || ability.GetComponent<SpellComponent>()?.School!=link.School) return false;
            // Empty disabled placeholders and stale identity-only stubs are not spells.
            bool executable=ability.GetComponent<AbilityEffectRunAction>()!=null || ability.GetComponent<AbilityVariants>()!=null ||
                ability.GetComponent<AbilityEffectStickyTouch>()!=null || ability.GetComponent<AbilityShadowSpell>()!=null ||
                ability.GetComponent<AdditionalAbilityEffectRunActionOnClickedTarget>()!=null;
            if(!executable) return false;
            if(!link.PublishedListRequired) return true;
            // TTT/Tome can create a full blueprint even when the user disables the
            // spell. Require a live, reciprocal provider/native list publication.
            // Never count our own link as proof that a provider enabled a spell.
            var id=ability.AssetGuid;
            return ability.GetComponents<SpellListComponent>().Any(c => {
                var list=c.SpellList;
                return list!=null && list.AssetGuid!=BlueprintGuid.Parse(PsychicClass.ListId) && list.SpellsByLevel!=null &&
                    list.SpellsByLevel.Any(l=>l!=null && l.SpellLevel==c.SpellLevel && l.Spells.Any(s=>s!=null && s.AssetGuid==id));
            });
        }
        internal static void Register(BlueprintsCache cache)
        {
            PsychicEidolonHealing.Integrate(cache);
            var list=cache.Load(BlueprintGuid.Parse(PsychicClass.ListId)) as BlueprintSpellList;
            if(list==null) return;
            if(list.name!="CotWWrathPrototypePsychicSpellList") throw new InvalidOperationException("Psychic private spell list identity mismatch.");
            var before=list.SpellsByLevel.Select(l=>(List<BlueprintAbilityReference>)Get(l,"m_Spells")).ToArray();
            var updated=before.Select(l=>l.ToList()).ToArray();
            int native=0,optional=0,skipped=0;
            foreach(var entry in PsychicSharedSpellCatalog.Entries.Where(e=>e.Psychic>=0)) {
                if(!Available(cache,entry,out var spell)) { skipped++; continue; }
                if(entry.Provider=="Wrath") native++; else optional++;
                if(!updated[entry.Psychic].Any(r=>r.Guid==spell.AssetGuid)) updated[entry.Psychic].Add(Ref<BlueprintAbilityReference>(spell));
            }
            // Do not add SpellListComponents to foreign spells or alter any native
            // class lists. Preserve original spell GUIDs, icons, effects and DLC checks.
            for(int i=0;i<updated.Length;i++) {
                if(updated[i].Count==before[i].Count) continue;
                Set(list.SpellsByLevel[i],"m_Spells",updated[i]);
                Set(list.SpellsByLevel[i],"m_SpellsProxy",null); Set(list.SpellsByLevel[i],"m_SpellsFiltered",null);
            }
            LastSummary=$"Psychic spell links: {native} native, {optional} optional, {skipped} unavailable/disabled. Occultist and Spiritualist mappings reserved; classes not registered.";
        }
    }
}
