// Adapted from Holic75/KingmakerRebalance, MIT; see LICENSE-CotW.txt.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // The DLC/Lich prototype is left intact. This independent player spell uses
    // the same repaired native lifecycle as Time Stutter, without granting or
    // requiring that discovery or changing other classes' spell-list capacity.
    internal static class TimeStop01438
    {
        internal const string ModuleName = "Spell: Time Stop";
        internal const string RootToken = "TimeStopSpell01438";
        internal const string HostToken = "TimeStopHost01438";
        internal const string AreaToken = "TimeStopArea01438";
        internal const string EffectToken = "TimeStopEffect01438";
        internal const string DismissToken = "TimeStopDismiss01440";
        internal static readonly string[] OwnTokens = { RootToken, HostToken, AreaToken, EffectToken, DismissToken };
        internal const string WizardListId = "ba0401fdeb4062f40a7aa95b6f07fe89";
        internal const string Description = "Time appears to stop for all other creatures while you act for 1d4+1 rounds. " +
            "Other creatures in the area cannot act, move or attack, and cannot be targeted or damaged until the effect ends. " +
            "You can move and cast spells on yourself, or place area spells whose remaining duration continues after time resumes. " +
            "This personal, standard-action transmutation is a 9th-level wizard, sorcerer, arcanist and psychic spell. " +
            "It has no saving throw or spell resistance. It does not grant Time Stutter or use that discovery's daily resource. " +
            "While this spell is active, use Time Stop: End as a free action to resume time early.";
        sealed class Publication
        {
            internal BlueprintAbility Root;
            internal BlueprintScriptableObject[] Assets;
            internal string[] Definitions;
        }
        sealed class ListChange
        {
            internal SpellLevelList Row;
            internal object Spells, Proxy, Filtered;
        }
        static readonly ConditionalWeakTable<BlueprintsCache, Publication> Published = new ConditionalWeakTable<BlueprintsCache, Publication>();
        static string Definition(BlueprintScriptableObject bp)
        {
            object shape;
            if (bp is BlueprintAbility a) shape = new object[] { a.Type, a.Range, a.ActionType, a.CanTargetSelf, a.CanTargetFriends, a.CanTargetEnemies, a.CanTargetPoint, a.AvailableMetamagic, a.SpellResistance };
            else if (bp is BlueprintBuff b) shape = new object[] { b.Stacking, b.Ranks, b.IsClassFeature, b.TickEachSecond, Get(b, "m_Flags") };
            else if (bp is BlueprintAbilityAreaEffect area) shape = new object[] { area.Shape, area.Size, area.AffectEnemies, area.AffectDead, area.IgnoreSleepingUnits, Get(area, "m_TargetType"), Get(area, "m_Tags") };
            else throw new InvalidOperationException("Unexpected Time Stop graph type.");
            // The optional private Psychic list can appear later. Only its
            // reciprocal list component is authorized to change after commit.
            return Migration01426.Definition(new object[] { bp.name, bp.GetType().FullName, bp.ComponentsArray.Where(c => !(c is SpellListComponent)).ToArray(), shape });
        }
        static BlueprintSpellList[] Lists(BlueprintsCache cache)
        {
            var wizard = cache.Load(BlueprintGuid.Parse(WizardListId)) as BlueprintSpellList
                ?? throw new InvalidOperationException("Time Stop requires the native wizard spell list.");
            var psychic = cache.Load(BlueprintGuid.Parse(PsychicClass.ListId)) as BlueprintSpellList;
            if (psychic != null && psychic.name != "CotWWrathPrototypePsychicSpellList")
                throw new InvalidOperationException("Time Stop found a foreign private Psychic list identity.");
            var lists = psychic == null ? new[] { wizard } : new[] { wizard, psychic };
            foreach (var list in lists)
                if (list.SpellsByLevel == null || list.SpellsByLevel.Length <= 9 || list.SpellsByLevel[9] == null || list.SpellsByLevel[9].SpellLevel != 9 || Get(list.SpellsByLevel[9], "m_Spells") == null)
                    throw new InvalidOperationException("Time Stop requires an existing level-9 row without reshaping the spell list: " + list.AssetGuid);
            return lists;
        }
        static void AppendLists(BlueprintsCache cache, BlueprintAbility root, BlueprintSpellList[] lists)
        {
            var ids = lists.Select(l => l.AssetGuid).ToArray();
            var existing = root.GetComponents<SpellListComponent>().ToArray();
            if (existing.Any(c => !ReferenceEquals(c.OwnerBlueprint, root)))
                throw new InvalidOperationException("Time Stop published spell-list metadata has foreign component ownership.");
            foreach (var component in existing) {
                if (component.GetType() != typeof(SpellListComponent))
                    throw new InvalidOperationException("Time Stop published spell-list metadata component type changed: " + component.GetType().FullName);
                var reference = (BlueprintSpellListReference)Get(component, "m_SpellList");
                var list = reference == null ? null : cache.Load(reference.Guid) as BlueprintSpellList;
                string detail = "list=" + (reference?.Guid.ToString() ?? "null") + "; level=" + component.SpellLevel;
                if (list == null || !ReferenceEquals(component.SpellList, list))
                    throw new InvalidOperationException("Time Stop published spell-list metadata has a missing or replaced list identity; " + detail);
                if (ids.Contains(list.AssetGuid)) {
                    if (component.SpellLevel != 9 || !lists.Any(own => ReferenceEquals(own, list)))
                        throw new InvalidOperationException("Time Stop published own spell-list metadata changed; " + detail);
                    continue;
                }
                // TTT's real AddToSpellList distributes a wizard spell to its
                // specialist/Thassilonian tables. External reciprocal entries
                // are valid additions, not edits to our private effect graph.
                // Validate them without reshaping or repairing a foreign list.
                int level = component.SpellLevel;
                var rows = list.SpellsByLevel;
                if (level < 0 || level > 10 || rows == null || rows.Length <= level || rows[level] == null || rows[level].SpellLevel != level)
                    throw new InvalidOperationException("Time Stop external spell-list metadata has no valid indexed row; " + detail);
                var references = Get(rows[level], "m_Spells") as List<BlueprintAbilityReference>;
                var reciprocal = references?.Where(r => r?.Guid == root.AssetGuid).ToArray();
                if (reciprocal == null || reciprocal.Length != 1 || !ReferenceEquals(reciprocal[0].Get(), root))
                    throw new InvalidOperationException("Time Stop external spell-list metadata is not uniquely reciprocal; " + detail);
            }
            if (existing.GroupBy(c => ((BlueprintSpellListReference)Get(c, "m_SpellList")).Guid).Any(g => g.Count() != 1))
                throw new InvalidOperationException("Time Stop published spell-list metadata has duplicate or foreign component ownership.");
            if (lists.Any(list => ((List<BlueprintAbilityReference>)Get(list.SpellsByLevel[9], "m_Spells")).Count(r => r?.Guid == root.AssetGuid) > 1))
                throw new InvalidOperationException("Time Stop has duplicate own references in a published level-9 row; external row contents are preserved.");
            var beforeComponents = root.ComponentsArray;
            var changes = lists.Select(l => new ListChange { Row = l.SpellsByLevel[9], Spells = Get(l.SpellsByLevel[9], "m_Spells"), Proxy = Get(l.SpellsByLevel[9], "m_SpellsProxy"), Filtered = Get(l.SpellsByLevel[9], "m_SpellsFiltered") }).ToArray();
            try {
                foreach (var list in lists) {
                    var row = list.SpellsByLevel[9]; var before = (List<BlueprintAbilityReference>)Get(row, "m_Spells");
                    if (!before.Any(r => r?.Guid == root.AssetGuid)) {
                        Set(row, "m_Spells", before.Concat(new[] { Ref<BlueprintAbilityReference>(root) }).ToList());
                        Set(row, "m_SpellsProxy", null); Set(row, "m_SpellsFiltered", null);
                    }
                    if (!existing.Any(c => c.SpellList?.AssetGuid == list.AssetGuid)) {
                        var component = new SpellListComponent { name = "TimeStopList" + list.AssetGuid, SpellLevel = 9 };
                        Set(component, "m_SpellList", Ref<BlueprintSpellListReference>(list));
                        component.OwnerBlueprint = root;
                        root.ComponentsArray = root.ComponentsArray.Concat(new BlueprintComponent[] { component }).ToArray();
                    }
                }
            } catch {
                root.ComponentsArray = beforeComponents;
                foreach (var change in changes) { Set(change.Row, "m_Spells", change.Spells); Set(change.Row, "m_SpellsProxy", change.Proxy); Set(change.Row, "m_SpellsFiltered", change.Filtered); }
                throw;
            }
        }
        static void ReconcileLists(BlueprintsCache cache, BlueprintAbility root, BlueprintSpellList[] lists)
        {
            // The caller owns the rollback boundary, including an exception
            // raised by an external postfix after AppendLists has returned.
            var beforeComponents = root.ComponentsArray;
            var changes = lists.Select(l => new ListChange { Row = l.SpellsByLevel[9], Spells = Get(l.SpellsByLevel[9], "m_Spells"), Proxy = Get(l.SpellsByLevel[9], "m_SpellsProxy"), Filtered = Get(l.SpellsByLevel[9], "m_SpellsFiltered") }).ToArray();
            try { AppendLists(cache, root, lists); }
            catch {
                root.ComponentsArray = beforeComponents;
                foreach (var change in changes) { Set(change.Row, "m_Spells", change.Spells); Set(change.Row, "m_SpellsProxy", change.Proxy); Set(change.Row, "m_SpellsFiltered", change.Filtered); }
                throw;
            }
        }
        internal static void Register(BlueprintsCache cache)
        {
            var state = Published.GetValue(cache, _ => new Publication());
            lock (state) {
                if (state.Root != null) {
                    for (int i = 0; i < state.Assets.Length; i++)
                        if (!ReferenceEquals(cache.Load(state.Assets[i].AssetGuid), state.Assets[i]) || Definition(state.Assets[i]) != state.Definitions[i] ||
                            state.Assets[i].ComponentsArray.Any(c => !ReferenceEquals(c.OwnerBlueprint, state.Assets[i])) || state.Assets[i].ElementsArray.Any(e => !ReferenceEquals(e.Owner, state.Assets[i])))
                            throw new InvalidOperationException("Time Stop published graph changed: " + state.Assets[i].name);
                    TimeStopEngine01438.RegisterGraph(cache, (BlueprintAbilityAreaEffect)cache.Load(Id(AreaToken)), (BlueprintBuff)cache.Load(Id(EffectToken)));
                    ReconcileLists(cache, state.Root, Lists(cache)); return;
                }
                foreach (string token in OwnTokens)
                    if (cache.Load(Id(token)) != null) throw new InvalidOperationException("Time Stop private identity already exists without this publication: " + token);
                var lists = Lists(cache); var f = new PsychicAssets(cache);
                var source = f.Native<BlueprintAbility>(TimeStutter01437.NativeAbility);
                if (source.Type != AbilityType.Spell || source.Range != AbilityRange.Personal || !source.CanTargetSelf || source.CanTargetFriends || source.CanTargetEnemies || source.CanTargetPoint)
                    throw new InvalidOperationException("Native Time Stop target graph changed.");
                var graph = TimeStopEngine01438.BuildGraph(f, HostToken, AreaToken, EffectToken, "Time Stop", Description);
                // The host owns this temporary fact, including native post-load
                // reconciliation. Normal Buff.Remove -> AddAreaEffect.ForceEnd
                // owns source-specific area exits, freeze flags and timer cleanup.
                var remove = new ContextActionRemoveBuff { ToCaster = true, OnlyFromCaster = true };
                Set(remove, "m_Buff", Ref<BlueprintBuffReference>(graph.Host));
                var active = new AbilityCasterHasFacts { NeedsAll = true };
                Set(active, "m_Facts", new[] { Ref<BlueprintUnitFactReference>(graph.Host) });
                var dismiss = f.Ability(DismissToken, "Time Stop: End",
                    "End your active Time Stop early as a free action. Time resumes for creatures affected by this casting; unrelated effects and other casters' time effects remain intact.",
                    AbilityType.Supernatural, UnitCommand.CommandType.Free, AbilityRange.Personal, active, Run(remove));
                Set(dismiss, "m_Icon", source.Icon);
                dismiss.LocalizedDuration = Text(DismissToken, "duration", "Instantaneous");
                dismiss.LocalizedSavingThrow = Text(DismissToken, "savingThrow", "None");
                dismiss.SpellResistance = false;
                graph.Host.ComponentsArray = graph.Host.ComponentsArray.Concat(new BlueprintComponent[] { Facts(dismiss) }).ToArray();
                var root = f.Clone(source, RootToken, "Time Stop", Description); TimeStopEngine01438.PrepareAbilityClone(root);
                var run = root.GetComponents<AbilityEffectRunAction>().ToArray();
                if (run.Length != 1 || run[0].Actions?.Actions?.Length != 1 || !(run[0].Actions.Actions[0] is ContextActionApplyBuff apply) || apply.Buff?.AssetGuid != BlueprintGuid.Parse(TimeStutter01437.NativeBuff))
                    throw new InvalidOperationException("Native Time Stop action graph changed.");
                root.ComponentsArray = root.ComponentsArray.Where(c => !(c is SpellListComponent) && !(c is SpellComponent)).Concat(new BlueprintComponent[] { new SpellComponent { School = SpellSchool.Transmutation } }).ToArray();
                Set(apply, "m_Buff", Ref<BlueprintBuffReference>(graph.Host));
                apply.DurationValue = new ContextDurationValue { Rate = DurationRate.Rounds, DiceType = Kingmaker.RuleSystem.DiceType.D4, DiceCountValue = 1, BonusValue = 1 };
                apply.UseDurationSeconds = false; apply.Permanent = false; apply.SameDuration = false; apply.IsFromSpell = true; apply.ToCaster = true;
                root.AvailableMetamagic = Metamagic.Extend | Metamagic.Heighten | Metamagic.Quicken | Metamagic.Empower | Metamagic.Maximize;
                root.LocalizedDuration = Text(RootToken, "duration", "1d4+1 rounds"); root.LocalizedSavingThrow = Text(RootToken, "savingThrow", "None");
                root.SpellResistance = false;
                var staged = f.Assets.Values.ToArray(); bool registeredGraph = false;
                try {
                    Migration01426.Commit(f, Array.Empty<BlueprintFeatureSelection>());
                    if (!staged.All(bp => ReferenceEquals(cache.Load(bp.AssetGuid), bp))) throw new InvalidOperationException("Time Stop cache changed during publication.");
                    var definitions = staged.Select(Definition).ToArray();
                    registeredGraph = TimeStopEngine01438.RegisterGraph(cache, graph.Area, graph.Effect);
                    ReconcileLists(cache, root, lists);
                    state.Assets = staged; state.Definitions = definitions; state.Root = root;
                } catch {
                    if (registeredGraph) TimeStopEngine01438.UnregisterGraph(graph.Area, graph.Effect);
                    foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                    state.Root = null; state.Assets = null; state.Definitions = null; throw;
                }
            }
        }
        internal static void RegisterOptional(BlueprintsCache cache) { if (cache.Load(Id(RootToken)) != null) Register(cache); }
    }
}
