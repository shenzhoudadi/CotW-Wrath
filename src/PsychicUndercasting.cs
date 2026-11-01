using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicUndercasting
    {
        // Native temporary known spells retain their own circle, consume ordinary slots,
        // and are excluded from the spells-known quota. Learning one normally promotes
        // it to permanent. No parallel list of permanent learned spells is serialized.
        [ThreadStatic] private static HashSet<Spellbook> reconciling;
        internal static bool Owned(BlueprintAbility ability) => ability != null &&
            ability.name != null && ability.name.StartsWith("CotWWrathPsychic", StringComparison.Ordinal) &&
            ability.GetComponent<PsychicUndercastFamily>() != null;
        internal static void Reconcile(Spellbook book)
        {
            if (!PsychicCasting.IsPsychic(book?.Blueprint)) return;
            if (reconciling == null) reconciling = new HashSet<Spellbook>();
            if (!reconciling.Add(book)) return;
            try {
                var known = book.GetAllKnownSpells().ToArray();
                var required = known.Where(a => (!a.IsTemporary || TemporaryKnowledge01464.Owns(book,a)) && Owned(a.Blueprint))
                    .SelectMany(a => a.Blueprint.GetComponent<PsychicUndercastFamily>().LowerSpells ?? Array.Empty<BlueprintAbilityReference>())
                    .Select(r => r.Get()).Where(Owned).Distinct().ToArray();
                foreach (var extra in known.Where(a => a.IsTemporary && Owned(a.Blueprint) && !required.Contains(a.Blueprint) && !TemporaryKnowledge01464.Owns(book,a)))
                    book.RemoveTemporarySpell(extra);
                foreach (var lower in required) {
                    var family = lower.GetComponent<PsychicUndercastFamily>();
                    if (!book.IsKnownOnLevel(lower, family.Circle)) book.AddKnownTemporary(family.Circle, lower);
                }
            } finally { reconciling.Remove(book); }
        }
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.AddKnown))]
        private static class LearnedPatch {
            [HarmonyPostfix] private static void Postfix(Spellbook __instance) => Reconcile(__instance);
            [HarmonyFinalizer] private static Exception Finalizer(Spellbook __instance, BlueprintAbility spell, Exception __exception) {
                PsychicBurstSpells.Trace(__instance,"AddKnown",spell,__exception);return __exception;
            }
        }
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.RemoveSpell))]
        private static class RemovedPatch { [HarmonyPostfix] private static void Postfix(Spellbook __instance, BlueprintAbility spell) {
            Reconcile(__instance);PsychicBurstSpells.Trace(__instance,"RemoveSpell",spell);
        } }
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.PostLoad))]
        private static class LoadedPatch { [HarmonyPostfix] private static void Postfix(Spellbook __instance) {
            Reconcile(__instance);PsychicBurstSpells.Trace(__instance,"PostLoad");
        } }

        // Native UnitHelper.CopySpellbook copies every known spell via AddKnown,
        // discarding IsTemporary. Level-up previews then mistake free lower
        // versions for paid choices and offer slots that the real unit cannot use.
        // Restore only our families from the original book, after native copying.
        [HarmonyPatch(typeof(UnitHelper), "CopySpellbook")]
        private static class CopiedBookPatch
        {
            [HarmonyPostfix]
            private static void Postfix(UnitEntityData original, UnitEntityData target)
            {
                if(original==null || target==null || ReferenceEquals(original,target)) return;
                foreach(var source in original.Descriptor.Spellbooks.Where(b=>PsychicCasting.IsPsychic(b.Blueprint))) {
                    var destination=target.Descriptor.GetSpellbook(source.Blueprint);
                    if(destination==null || ReferenceEquals(source,destination)) continue;
                    TemporaryKnowledge01464.RestoreCopy(source,destination);
                    var provenance=source.GetAllKnownSpells().Where(a=>Owned(a.Blueprint) && a.MetamagicData==null)
                        .GroupBy(a=>new {a.Blueprint.AssetGuid,a.SpellLevel})
                        .ToDictionary(g=>g.Key,g=>g.All(a=>a.IsTemporary));
                    foreach(var copied in destination.GetAllKnownSpells().Where(a=>Owned(a.Blueprint) && a.MetamagicData==null))
                        if(provenance.TryGetValue(new {copied.Blueprint.AssetGuid,copied.SpellLevel},out var temporary)) copied.IsTemporary=temporary;
                    Reconcile(destination);
                    PsychicBurstSpells.Trace(destination,"CopySpellbook provenance restored");
                }
            }
        }

        private static void OfferReplacements(Spellbook book, LevelUpState state, SpellSelectionData previous, AbilityData[] replaced)
        {
            // Wrath's CharGenSpellsPhaseVM keeps the old selection object and
            // compares its slot counts in TryUpdateSamePhase. Mutating it in
            // place hides the change from that comparison, so no new low-circle
            // page is created. Publish a separate snapshot for the normal UI
            // refresh triggered by SelectSpell; preserve all choices and extras.
            var selection = new SpellSelectionData(previous.Spellbook, previous.SpellList) {
                ExtraMaxLevel = previous.ExtraMaxLevel, ExtraByStat = previous.ExtraByStat,
                ExtraSelected = previous.ExtraSelected == null ? null : (BlueprintAbility[])previous.ExtraSelected.Clone()
            };
            for (int circle = 0; circle < previous.LevelCount.Length; circle++) {
                var entry = previous.LevelCount[circle];
                selection.LevelCount[circle] = entry == null ? null : new SpellSelectionData.SpellSelectionState {
                    SpellSelections = entry.SpellSelections == null ? null : (BlueprintAbility[])entry.SpellSelections.Clone()
                };
            }
            foreach (var group in replaced.GroupBy(a => a.SpellLevel)) {
                int circle = group.Key;
                var old = selection.LevelCount[circle]?.SpellSelections ?? Array.Empty<BlueprintAbility>();
                // SetLevelSpells replaces the array; preserve choices already
                // made in that circle when appending the replacement slots.
                selection.SetLevelSpells(circle, old.Length + group.Count());
                Array.Copy(old, selection.LevelCount[circle].SpellSelections, old.Length);
            }
            state.SpellSelections[state.SpellSelections.IndexOf(previous)] = selection;
            foreach (var lower in replaced) lower.IsTemporary = true;
            Reconcile(book);
        }

        // Older saves may already contain both paid low and high versions. Repair
        // them at the next Psychic level-up, where there is a real selection UI.
        // Loading a save alone neither deletes paid spells nor consumes choices.
        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply))]
        private static class ExistingReplacementsPatch
        {
            [HarmonyPostfix]
            private static void Postfix(LevelUpState state, UnitDescriptor unit)
            {
                var blueprint = state.SelectedClass?.Spellbook;
                if (!PsychicCasting.IsPsychic(blueprint)) return;
                var book = unit.GetSpellbook(blueprint);
                if (book == null) return;
                var known = book.GetAllKnownSpells().Where(a => !a.IsTemporary && a.MetamagicData == null && Owned(a.Blueprint)).ToArray();
                var lowerIds = new HashSet<BlueprintGuid>(known.SelectMany(a =>
                    a.Blueprint.GetComponent<PsychicUndercastFamily>().LowerSpells ?? Array.Empty<BlueprintAbilityReference>()).Select(r => r.Guid));
                var replaced = known.Where(a => lowerIds.Contains(a.Blueprint.AssetGuid)).ToArray();
                if (replaced.Length != 0) OfferReplacements(book, state, state.DemandSpellSelection(blueprint, blueprint.SpellList), replaced);
            }
        }

        // SelectSpell owns the active level-up state and is replayed for both the
        // preview and the committed character. Never grant choices from PostLoad
        // or AddKnown itself (which also handles automatic/temporary grants).
        [HarmonyPatch(typeof(SelectSpell), nameof(SelectSpell.Apply))]
        private static class ReplacementPatch
        {
            [HarmonyPrefix]
            private static void Prefix(SelectSpell __instance, UnitDescriptor unit, out AbilityData[] __state)
            {
                __state = Array.Empty<AbilityData>();
                PsychicLearningDiagnostics.Selection(__instance,unit,"before");
                if (!PsychicCasting.IsPsychic(__instance.Spellbook) || !Owned(__instance.Spell)) return;
                var book = unit.GetSpellbook(__instance.Spellbook);
                if (book == null || book.IsKnown(__instance.Spell)) return;
                var lower = __instance.Spell.GetComponent<PsychicUndercastFamily>().LowerSpells;
                __state = book.GetAllKnownSpells().Where(a => !a.IsTemporary && a.MetamagicData == null &&
                    (lower ?? Array.Empty<BlueprintAbilityReference>()).Any(r => r.Guid == a.Blueprint.AssetGuid)).ToArray();
            }
            [HarmonyPostfix]
            private static void Postfix(SelectSpell __instance, LevelUpState state, UnitDescriptor unit, AbilityData[] __state)
            {
                if (__state == null || __state.Length == 0) return;
                var book = unit.GetSpellbook(__instance.Spellbook);
                if (book == null || !book.IsKnown(__instance.Spell)) return;
                var selection = state.DemandSpellSelection(__instance.Spellbook, __instance.SpellList);
                OfferReplacements(book, state, selection, __state);
            }
            [HarmonyFinalizer]
            private static Exception Finalizer(SelectSpell __instance, UnitDescriptor unit, Exception __exception)
            {
                PsychicLearningDiagnostics.Selection(__instance,unit,"after",__exception);return __exception;
            }
        }
    }
}
