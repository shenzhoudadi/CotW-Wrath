using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using CallOfTheWild.Wrath.Prototype;

internal static class Feedback01422CloneTests
{
    static int checks;
    static readonly Assembly module = typeof(PsychicBloodyClawsHit).Assembly;
    static readonly Type factory = module.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets", true);
    static readonly Type expansion = module.GetType("CallOfTheWild.Wrath.Prototype.PsychicExpansion", true);
    static object Get(object value, string field) => AccessTools.Field(value.GetType(), field).GetValue(value);
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("Feedback01422 clone source: " + reason); checks++; }
    static string Signature(object value) => (string)AccessTools.Method(expansion, "Signature").Invoke(null, new[] { value });
    static T Copy<T>(T value) => (T)AccessTools.Method(factory, "CopyData").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { value });
    static object Maker(BlueprintsCache cache) => Activator.CreateInstance(factory, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { cache }, null);
    static void Build(object maker, string name) => AccessTools.Method(module.GetType("CallOfTheWild.Wrath.Prototype." + name, true), "Build").Invoke(null, new[] { maker });
    static Exception Failure(Action action)
    {
        try { action(); return null; }
        catch (Exception error) { while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException; return error; }
    }

    // Exercise the actual registration entry after corrupting only a private copy
    // of a native donor graph. Snapshot published cache/list/progression state;
    // a failed private build must never publish its staged replacements.
    sealed class PublishedState
    {
        readonly IDictionary loaded;
        readonly Dictionary<BlueprintGuid, object> entries;
        readonly Dictionary<BlueprintScriptableObject, BlueprintComponent[]> components;
        readonly Dictionary<BlueprintSpellList, SpellLevelList[]> lists;
        readonly Dictionary<SpellLevelList, object> spells;
        readonly Dictionary<SpellLevelList, BlueprintGuid[]> spellIds;
        readonly Dictionary<BlueprintProgression, string> progressions;
        internal PublishedState(BlueprintsCache cache)
        {
            loaded = (IDictionary)Get(cache, "m_LoadedBlueprints");
            var blueprints = loaded.Keys.Cast<BlueprintGuid>().ToArray().Select(cache.Load).OfType<BlueprintScriptableObject>().ToArray();
            entries = loaded.Keys.Cast<BlueprintGuid>().ToDictionary(id => id, id => loaded[id]);
            components = blueprints.ToDictionary(b => b, b => b.ComponentsArray);
            lists = blueprints.OfType<BlueprintSpellList>().ToDictionary(b => b, b => b.SpellsByLevel);
            var rows = lists.Values.Where(a => a != null).SelectMany(a => a).Where(r => r != null).Distinct().ToArray();
            spells = rows.ToDictionary(r => r, r => Get(r, "m_Spells"));
            spellIds = rows.ToDictionary(r => r, r => ((IEnumerable<BlueprintAbilityReference>)Get(r, "m_Spells") ?? Array.Empty<BlueprintAbilityReference>()).Select(a => a == null ? default(BlueprintGuid) : a.Guid).ToArray());
            progressions = blueprints.OfType<BlueprintProgression>().ToDictionary(p => p, p => Signature(p.LevelEntries));
        }
        internal void Verify(string name)
        {
            Check(loaded.Count == entries.Count && entries.All(p => loaded.Contains(p.Key) && Equals(p.Value, loaded[p.Key])), name + ": all published blueprint cache entries preserved");
            Check(components.All(p => ReferenceEquals(p.Key.ComponentsArray, p.Value)), name + ": existing component arrays preserved");
            Check(lists.All(p => ReferenceEquals(p.Key.SpellsByLevel, p.Value)) && spells.All(p => ReferenceEquals(Get(p.Key, "m_Spells"), p.Value)), name + ": published list and row references preserved");
            Check(spellIds.All(p => p.Value.SequenceEqual(((IEnumerable<BlueprintAbilityReference>)Get(p.Key, "m_Spells") ?? Array.Empty<BlueprintAbilityReference>()).Select(a => a == null ? default(BlueprintGuid) : a.Guid))), name + ": all preexisting spell references preserved");
            Check(progressions.All(p => p.Value == Signature(p.Key.LevelEntries)), name + ": all class progression entries preserved");
        }
    }

    static void Reject(BlueprintsCache cache, string builder, string prefix)
    {
        var state = new PublishedState(cache);
        var error = Failure(() => Build(Maker(cache), builder));
        Check(error is InvalidOperationException && error.Message.StartsWith(prefix) && error.Message.Contains("registration cancelled before publication"), builder + ": missing native shape explicitly rejected, without synthetic replacement");
        state.Verify(builder);
    }

    static void RejectModule(BlueprintsCache cache, string prefix)
    {
        var state = new PublishedState(cache);
        bool reported = false, succeeded = true; Exception failure = null;
        Action register = () => AccessTools.Method(expansion, "Register").Invoke(null, new object[] { cache });
        Action<string, bool, Exception> report = (name, ok, error) => { reported = true; succeeded = ok; failure = error; };
        var escaped = Failure(() => AccessTools.Method(module.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration", true), "RegisterModule").Invoke(null, new object[] { "Psychic spells and disciplines", register, report }));
        while (failure is TargetInvocationException && failure.InnerException != null) failure = failure.InnerException;
        Check(escaped == null && reported && !succeeded && failure is InvalidOperationException && failure.Message.StartsWith(prefix), "actual module wrapper contains missing-source failure and reports failure");
        state.Verify("actual full registration failure");
    }

    internal static int Run(BlueprintsCache cache)
    {
        checks = 0;
        var intimidation = (BlueprintAbility)cache.Load(BlueprintGuid.Parse("5f3126d4120b2b244a95cb2ec23d69fb"));
        var bleed = (BlueprintBuff)cache.Load(BlueprintGuid.Parse("75039846c3d85d940aa96c249b97e562"));
        var intimidationBefore = intimidation.ComponentsArray;
        var bleedBefore = bleed.ComponentsArray;
        var originalIntimidation = Signature(intimidationBefore);
        var originalBleed = Signature(bleedBefore);
        try
        {
            foreach (var kind in new[] { "missing component", "null actions", "missing action" })
            {
                intimidation.ComponentsArray = Copy(intimidationBefore);
                var action = intimidation.ComponentsArray.OfType<AbilityEffectRunAction>().First();
                if (kind == "missing component") intimidation.ComponentsArray = intimidation.ComponentsArray.Where(c => !(c is AbilityEffectRunAction)).ToArray();
                else if (kind == "null actions") action.Actions = null;
                else action.Actions.Actions = action.Actions.Actions.Where(a => !(a is Demoralize)).ToArray();
                intimidation.OnEnable();
                Reject(cache, "PsychicAspectSpells", "Blistering Invective requires");
                RejectModule(cache, "Blistering Invective requires");
            }
            intimidation.ComponentsArray = intimidationBefore; intimidation.OnEnable();
            foreach (var kind in new[] { "missing component", "null actions", "missing action" })
            {
                bleed.ComponentsArray = Copy(bleedBefore);
                var round = bleed.ComponentsArray.OfType<AddFactContextActions>().First();
                if (kind == "missing component") bleed.ComponentsArray = bleed.ComponentsArray.Where(c => !(c is AddFactContextActions)).ToArray();
                else if (kind == "null actions") round.NewRound = null;
                else round.NewRound.Actions = round.NewRound.Actions.Where(a => !(a is ContextActionDealDamage)).ToArray();
                bleed.OnEnable();
                Reject(cache, "PsychicBloodyClaws", "Bloody Claws requires");
                Reject(cache, "PsychicBattleSpells", "Touch of Bloodletting requires");
                // Bloody Claws is built first in the real full registration.
                RejectModule(cache, "Bloody Claws requires");
            }
            bleed.ComponentsArray = Copy(bleedBefore);
            var duplicateRound = bleed.ComponentsArray.OfType<AddFactContextActions>().First();
            var first = duplicateRound.NewRound.Actions.OfType<ContextActionDealDamage>().First();
            duplicateRound.NewRound.Actions = duplicateRound.NewRound.Actions.Concat(new GameAction[] { Copy(first) }).ToArray();
            bleed.OnEnable();
            var duplicateSignature = Signature(bleed.ComponentsArray);
            foreach (var pair in new[] { new[] { "PsychicBloodyClaws", "BloodyClawsEffectBuff" }, new[] { "PsychicBattleSpells", "TouchOfBloodlettingBuff" } })
            {
                var state = new PublishedState(cache); var maker = Maker(cache);
                Build(maker, pair[0]);
                var staged = (BlueprintBuff)((IDictionary)Get(maker, "Assets"))[pair[1]];
                var ticks = staged.ComponentsArray.OfType<AddFactContextActions>().First().NewRound.Actions.OfType<ContextActionDealDamage>().ToArray();
                Check(ticks.Length == 1 && !ReferenceEquals(ticks[0], first) && Signature(ticks[0].DamageType) == Signature(first.DamageType), pair[0] + ": duplicate damage actions tolerate first valid native source with an independent clone");
                Check(duplicateSignature == Signature(bleed.ComponentsArray), pair[0] + ": duplicate donor graph unchanged");
                state.Verify(pair[0] + " successful private staging");
            }
            bleed.ComponentsArray = bleedBefore; bleed.OnEnable();
            intimidation.ComponentsArray = Copy(intimidationBefore);
            var intimidateActions = intimidation.ComponentsArray.OfType<AbilityEffectRunAction>().First();
            var demoralize = intimidateActions.Actions.Actions.OfType<Demoralize>().First();
            intimidateActions.Actions.Actions = intimidateActions.Actions.Actions.Concat(new GameAction[] { Copy(demoralize) }).ToArray();
            intimidation.OnEnable();
            var intact = Signature(intimidation.ComponentsArray); var valid = Maker(cache);
            Build(valid, "PsychicAspectSpells");
            var invective = (BlueprintAbility)((IDictionary)Get(valid, "Assets"))["BlisteringInvective"];
            var cloned = invective.ComponentsArray.OfType<AbilityEffectRunAction>().First().Actions.Actions.OfType<PsychicInvectiveDemoralize>().First().Demoralize;
            Check(!ReferenceEquals(cloned, demoralize) && Signature(cloned) == Signature(demoralize), "duplicate Demoralize uses intact independent first native action");
            Check(intact == Signature(intimidation.ComponentsArray), "duplicate Intimidate donor graph unchanged");
        }
        finally
        {
            intimidation.ComponentsArray = intimidationBefore; intimidation.OnEnable();
            bleed.ComponentsArray = bleedBefore; bleed.OnEnable();
        }
        Check(Signature(intimidationBefore) == originalIntimidation && Signature(bleedBefore) == originalBleed, "all native source components and action fields restored");
        PsychicExpansionTests.Inventory["feedback01422CloneAssertions"] = checks;
        Console.WriteLine("PASS: " + checks + " 01422 clone dependency guards, real registration isolation, published state preservation and duplicate donor contracts.");
        return checks;
    }
}
