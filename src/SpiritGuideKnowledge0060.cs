using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0060 D-F02 / D2-F02. AddSpellKnownTemporary adds every bonded spell at
    // its fixed circle even when the real Oracle spellbook cannot cast that circle
    // yet. The frozen contract wants the grant to follow the class-owned book:
    // from Oracle level 4 only, never above that book's current MaxSpellLevel,
    // re-added when it rises, withdrawn when it falls, and migrated when the
    // Oracle's own book is replaced. This coordinator works on the native
    // UnitPartTemporarySpellsKnown ledger and only touches entries whose source
    // is a 0060 feature; permanent knowledge and other temporary providers are
    // left alone. There is no second spell account and no global RemoveSpell.
    internal static class SpiritGuideKnowledge0060
    {
        private static readonly FieldInfo Entries =
            AccessTools.Field(typeof(UnitPartTemporarySpellsKnown), "m_Entries");

        private static List<UnitPartTemporarySpellsKnown.Entry> Ledger(UnitPartTemporarySpellsKnown part) =>
            (List<UnitPartTemporarySpellsKnown.Entry>)Entries.GetValue(part);

        private static bool Owned(EntityFact source)
        {
            var name = source?.Blueprint?.name;
            return name != null && name.StartsWith(SpiritGuideBridge0060.AssetPrefix, StringComparison.Ordinal) &&
                   name.EndsWith("Spells0060", StringComparison.Ordinal);
        }

        private static readonly BlueprintGuid OracleGuid =
            BlueprintGuid.Parse(SpiritGuideBridge0060.OracleClassId);

        // D2-F02b. The authoritative main book is the one the real Oracle
        // ClassData points at - the same resolution native
        // UnitDescriptor.DemandSpellbook(class) performs. It honours an archetype
        // ReplaceSpellbook / merged-class replacement, and it does not mistake a
        // stale respec book, a second book sharing the Oracle blueprint, or a
        // different class's book for the current one. Only this book receives
        // the bond.
        private static Spellbook CurrentOracleBook(UnitDescriptor owner)
        {
            var oracle = ResourcesLibrary.TryGetBlueprint(OracleGuid) as BlueprintCharacterClass;
            if (oracle == null) return null;
            var classData = owner.Progression?.GetClassData(oracle);
            if (classData?.Spellbook == null) return null;
            return owner.DemandSpellbook(oracle);
        }

        internal static void Reconcile(UnitDescriptor owner)
        {
            if (owner == null) return;
            var part = owner.Unit?.Get<UnitPartTemporarySpellsKnown>();
            if (part == null) return;
            var book = CurrentOracleBook(owner);
            if (book == null) return;
            Reconcile(part, book);
        }

        // book is the current Oracle-owned book. Owned entries are migrated to it:
        // stale-book objects are unbound (and deleted only when no other active
        // entry still owns them), legal circles are (re-)published on it, and
        // every other entry - permanent, independent or another class's - is
        // never rewritten.
        internal static void Reconcile(UnitPartTemporarySpellsKnown part, Spellbook book)
        {
            if (part == null || book == null || Entries.GetValue(part) == null) return;
            var entries = Ledger(part);
            int max = book.MaxSpellLevel;
            foreach (var entry in entries.ToArray())
            {
                if (entry == null || entry.Removed) continue;
                var component = entry.Component;
                if (component == null || component.Spell == null || !Owned(entry.Source)) continue;
                int circle = component.Level;
                bool unlocked = OracleLevel(part, component) >= 4 && circle <= max;

                for (int i = entry.Spells.Count - 1; i >= 0; i--)
                {
                    var spell = entry.Spells[i];
                    if (spell == null) { entry.Spells.RemoveAt(i); continue; }
                    var holder = spell.Spellbook;
                    bool onMain = ReferenceEquals(holder, book);
                    if (onMain && unlocked && spell.SpellLevel == circle) continue;

                    // D2-F02a. "Delete the shared AbilityData object" and
                    // "unbind our own ledger entry" are two different decisions.
                    // A locked circle, an out-of-level object or a stale book
                    // always unbinds our entry; only an object no other active
                    // entry owns may be removed from its book. If another source
                    // still holds the exact object, it must stay alive so the
                    // native RemoveEntry of that last source can delete it
                    // immediately, without waiting for another level-up.
                    bool shared = entries.Any(e => e != null && !ReferenceEquals(e, entry) &&
                                                   !e.Removed && e.Spells.Contains(spell));
                    entry.Spells.RemoveAt(i);
                    // Permanent promotions are not ours to delete.
                    if (spell.IsTemporary && !shared)
                        (holder ?? book).RemoveTemporarySpell(spell);
                }

                if (!unlocked) continue;
                if (entry.Spells.Any(s => s != null && ReferenceEquals(s.Spellbook, book) &&
                                          s.SpellLevel == circle)) continue;
                // Another active source may already have published this exact
                // book/circle/spell. Adopt that object instead of creating a
                // duplicate AbilityData in the same book.
                var existing = entries
                    .Where(e => e != null && !e.Removed && !ReferenceEquals(e, entry))
                    .SelectMany(e => e.Spells)
                    .FirstOrDefault(s => s != null && ReferenceEquals(s.Spellbook, book) &&
                                         s.SpellLevel == circle && s.Blueprint == component.Spell);
                if (existing != null) { entry.Spells.Add(existing); continue; }
                var added = book.AddKnownTemporary(circle, component.Spell);
                if (added != null) entry.Spells.Add(added);
            }
        }

        // The bonded-spell feature itself is granted at Oracle 4, but a retrain or
        // a caller that adds the fact early must not open the bond before 4.
        private static int OracleLevel(UnitPartTemporarySpellsKnown part, AddSpellKnownTemporary component)
        {
            var cls = component.CharacterClass;
            var owner = part.Owner;
            return cls == null || owner == null ? 0 : owner.Progression.GetClassLevel(cls);
        }

        [HarmonyPatch]
        internal static class SourcesAddedPatch
        {
            private static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(UnitPartTemporarySpellsKnown), nameof(UnitPartTemporarySpellsKnown.AddEntry));
            private static void Postfix(UnitPartTemporarySpellsKnown __instance) =>
                Reconcile(__instance.Owner?.Descriptor);
        }

        [HarmonyPatch]
        internal static class SourcesLeveledPatch
        {
            private static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(UnitPartTemporarySpellsKnown), nameof(UnitPartTemporarySpellsKnown.HandleUnitGainLevel));
            private static void Postfix(UnitPartTemporarySpellsKnown __instance) =>
                Reconcile(__instance.Owner?.Descriptor);
        }

        [HarmonyPatch]
        internal static class SourcesLoadedPatch
        {
            private static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(UnitPartTemporarySpellsKnown), "OnPostLoad");
            private static void Postfix(UnitPartTemporarySpellsKnown __instance) =>
                Reconcile(__instance.Owner?.Descriptor);
        }
    }
}
