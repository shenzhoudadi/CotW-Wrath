using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0119 / Lore reuse. The Lore spirit's Arcane Enlightenment chain is
    // authored once by LoreSpirit0118 on the Shaman class. The two consumers -
    // the Wizard Spirit Whisperer (0117) and the Oracle Spirit Guide (0060) -
    // clone those native selections, and the memberwise clone keeps the
    // shaman-bound spell-learning component, the shaman "can cast this circle"
    // prerequisite and the Intelligence/Charisma ability-score gates. That is
    // why neither archetype could ever pick a spell. This helper rebinds a
    // private clone onto the consumer's own class, its own candidate list and
    // its own ability-score gates. It never edits the shared native blueprint.
    internal static class LoreArcane0119
    {
        internal const string ShamanId = "145f1d3d360a7ad48bd95d392c81b38e";
        internal const string WizardListId = "ba0401fdeb4062f40a7aa95b6f07fe89";

        internal static bool Has(object obj, string field) =>
            obj != null && AccessTools.Field(obj.GetType(), field) != null;

        internal static void SetField(object obj, string field, object value)
        {
            var target = AccessTools.Field(obj?.GetType(), field);
            if (target == null) return;
            PsychicAssets.Set(obj, field, value);
        }

        // include minus exclude, circle by circle. The caller owns the identity
        // (its own private salt and asset prefix), so the same helper serves both
        // bridges without sharing a blueprint between them.
        internal static BlueprintSpellList Candidate(
            Func<BlueprintSpellList, string, string, string, BlueprintSpellList> add,
            string token, BlueprintSpellList include, BlueprintSpellList exclude)
        {
            var list = add(new BlueprintSpellList(), token, "", "");
            var excluded = new HashSet<BlueprintGuid>(
                (exclude?.SpellsByLevel ?? Array.Empty<SpellLevelList>())
                    .Where(e => e != null)
                    .SelectMany(e => (IEnumerable<BlueprintAbility>)e.Spells ?? Array.Empty<BlueprintAbility>())
                    .Where(s => s != null)
                    .Select(s => s.AssetGuid));
            list.SpellsByLevel = Enumerable.Range(0, 10).Select(circle =>
            {
                var entry = new SpellLevelList(circle);
                var spells = (include?.SpellsByLevel ?? Array.Empty<SpellLevelList>())
                    .Where(e => e != null && e.SpellLevel == circle)
                    .SelectMany(e => (IEnumerable<BlueprintAbility>)e.Spells ?? Array.Empty<BlueprintAbility>())
                    .Where(s => s != null && !excluded.Contains(s.AssetGuid))
                    .Select(s => PsychicAssets.Ref<BlueprintAbilityReference>(s))
                    .ToList();
                PsychicAssets.Set(entry, "m_Spells", spells);
                return entry;
            }).ToArray();
            return list;
        }

        // Re-points one cloned Arcane Enlightenment option. `circle` is the
        // option's fixed spell level; the two ability-score gates become
        // "first >= 10 + circle" and "second >= 10 + circle".
        internal static void Rebind(BlueprintParametrizedFeature feature, BlueprintCharacterClass cls,
            BlueprintSpellList list, StatType first, StatType second, int circle)
        {
            if (feature == null || cls == null) return;
            var classRef = PsychicAssets.Ref<BlueprintCharacterClassReference>(cls);
            var listRef = list == null ? null : PsychicAssets.Ref<BlueprintSpellListReference>(list);

            if (Has(feature, "m_SpellcasterClass")) SetField(feature, "m_SpellcasterClass", classRef);
            if (listRef != null && Has(feature, "m_SpellList")) SetField(feature, "m_SpellList", listRef);

            var kept = new List<BlueprintComponent>();
            var gates = new List<object>();
            foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component == null) continue;
                var name = component.GetType().Name;
                if (name == "LearnSpellParametrized")
                {
                    SetField(component, "m_SpellcasterClass", classRef);
                    if (listRef != null) SetField(component, "m_SpellList", listRef);
                }
                else if (name == "PrerequisiteClassSpellLevel")
                {
                    SetField(component, "m_CharacterClass", classRef);
                }
                else if (name == "PrerequisiteStatValue")
                {
                    gates.Add(component);
                    continue;
                }
                kept.Add(component);
            }

            // Keep the donor's authored gates when it has both, otherwise top the
            // list up so a clone can never end up with a single requirement.
            for (int i = 0; i < 2; i++)
            {
                object gate;
                if (i < gates.Count) gate = gates[i];
                else gate = new PrerequisiteStatValue();
                SetField(gate, "Stat", i == 0 ? first : second);
                SetField(gate, "Value", 10 + circle);
                kept.Add((BlueprintComponent)gate);
            }
            feature.ComponentsArray = kept.ToArray();
        }

        // True when the spirit grants powers on levels after the archetype's own
        // axis. Only those spirits (Lore) carry the repeated selections; every
        // other spirit keeps the plain 1/8/20 shape.
        internal static bool HasHigherPowers(BlueprintProgression progression) =>
            progression != null && (progression.LevelEntries ?? Array.Empty<LevelEntry>())
                .Any(e => e != null && ((IEnumerable<BlueprintFeatureBase>)e.Features ?? Array.Empty<BlueprintFeatureBase>()).Any());
    }
}
