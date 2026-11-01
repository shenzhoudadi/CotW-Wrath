using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

// Runs with the existing native037 scene boundaries and native038 membership
// reverse patch. No production removal, fact grant or cleanup body is replaced.
internal static class TimeStopDismiss01440Tests
{
    static int checks, casts, ends;
    static object Call(Type t, string method, params object[] args) => AccessTools.Method(t, method).Invoke(null, args);
    static object Old(string method, params object[] args) => Call(typeof(TimeStutter01437Tests), method, args);
    static object Life(string method, params object[] args) => Call(typeof(TimeStopLifecycle01438Tests), method, args);
    static T Asset<T>(BlueprintsCache c, string token) => (T)AccessTools.Method(typeof(TimeStutter01437Tests), "Asset").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { c, token });
    static void Check(bool ok, string message) { if (!ok) throw new Exception("TimeStopDismiss01440: " + message); checks++; }
    static void Act(BlueprintAbility bp, UnitEntityData caster, UnitEntityData suppliedTarget)
    {
        var data = new AbilityData(bp, caster.Descriptor);
        var context = new AbilityExecutionContext(data, new AbilityParams { CasterLevel = 20, SpellLevel = bp.Type == AbilityType.Spell ? 9 : 0 }, new TargetWrapper(suppliedTarget), null, caster);
        AbilityExecutionProcess.PrepareCast(context);
        // Earlier full-suite rule tests install a queued-dice prefix. Keep the
        // real native duration body here, exactly as in the existing038 suite,
        // and restore the unrelated test's complete patch metadata afterwards.
        var formula = AccessTools.Method(typeof(RulebookEvent.Dice), "D", new[] { typeof(DiceFormula) });
        var legacy = AccessTools.Method(typeof(Psychic070Tests), "ControlledDice");
        var previous = Harmony.GetPatchInfo(formula)?.Prefixes.Where(p => p.PatchMethod == legacy).ToArray() ?? new Patch[0];
        var h = new Harmony("cotw.tests.time-stop-dismiss01440.dice");
        try {
            if (previous.Length != 0) h.Unpatch(formula, legacy);
            using (context.GetDataScope(suppliedTarget)) bp.GetComponent<AbilityEffectRunAction>().Apply(context, new TargetWrapper(suppliedTarget));
        } finally {
            foreach (var p in previous) new Harmony(p.owner).Patch(formula, prefix: new HarmonyMethod(p.PatchMethod) { priority = p.priority, before = p.before, after = p.after, debug = p.debug });
        }
        var restored = Harmony.GetPatchInfo(formula)?.Prefixes.Where(p => p.PatchMethod == legacy).ToArray() ?? new Patch[0];
        if (restored.Length != previous.Length || !previous.All(p => restored.Any(r => r.owner == p.owner && r.priority == p.priority && r.before.SequenceEqual(p.before) && r.after.SequenceEqual(p.after) && r.debug == p.debug)))
            throw new Exception("TimeStopDismiss01440: legacy dice prefix metadata was not restored");
    }
    static Tuple<Buff, AreaEffectEntityData> Cast(BlueprintsCache c, UnitEntityData caster)
    {
        Act(Asset<BlueprintAbility>(c, "TimeStopSpell01438"), caster, caster); casts++;
        var host = caster.Buffs.Enumerable.Single(b => b.Blueprint == Asset<BlueprintBuff>(c, "TimeStopHost01438"));
        var area = (AreaEffectEntityData)Life("Area", "040-dismiss-" + casts, host, Asset<BlueprintAbilityAreaEffect>(c, "TimeStopArea01438"));
        Life("Scan", area); return Tuple.Create(host, area);
    }
    static void End(Tuple<Buff, AreaEffectEntityData> source)
    {
        Check(source.Item1.IsDisposed && source.Item2.IsEnded, "native removal disposes host and requests area end");
        Life("End", source.Item2); ends++;
    }
    internal static int Run(BlueprintsCache cache)
    {
        checks = casts = ends = 0;
        var module = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStop01438", true);
        var engine = module.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopEngine01438", true);
        Call(module, "Register", cache);
        var dismiss = Asset<BlueprintAbility>(cache, "TimeStopDismiss01440"); var hostBp = Asset<BlueprintBuff>(cache, "TimeStopHost01438");
        var effect = Asset<BlueprintBuff>(cache, "TimeStopEffect01438");
        var remove = (ContextActionRemoveBuff)dismiss.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
        var gate = dismiss.GetComponent<AbilityCasterHasFacts>();
        Check(dismiss.Type == AbilityType.Supernatural && dismiss.ActionType == UnitCommand.CommandType.Free && dismiss.Range == AbilityRange.Personal && dismiss.CanTargetSelf && !dismiss.CanTargetFriends && !dismiss.CanTargetEnemies && !dismiss.CanTargetPoint, "button is free, supernatural and personal only");
        Check(!dismiss.SpellResistance && dismiss.AvailableMetamagic == 0 && !dismiss.GetComponents<AbilityResourceLogic>().Any() && !dismiss.GetComponents<SpellListComponent>().Any(), "button costs no slot/resource and is not a new spell root");
        Check(remove.Buff == hostBp && remove.ToCaster && remove.OnlyFromCaster && !remove.RemoveRank && gate.NeedsAll && gate.Facts.Single() == hostBp, "exact native source and owner guards");
        Check(hostBp.GetComponent<AddFacts>().Facts.Single() == dismiss && hostBp.ComponentsArray.Length == 2, "host adds exactly one temporary button alongside native AddAreaEffect");
        Check(dismiss.ComponentsArray.All(x => ReferenceEquals(x.OwnerBlueprint, dismiss)) && dismiss.ElementsArray.All(x => ReferenceEquals(x.Owner, dismiss)), "button components/actions owned by exact child blueprint");
        var faction = new BlueprintFaction { name = "040-faction", AssetGuid = BlueprintGuid.Parse("04000000000000000000000000000001") }; var group = new UnitGroup("040-group");
        var caster = (UnitEntityData)Life("Unit", cache, "040-caster", faction, group, 1);
        var other = (UnitEntityData)Life("Unit", cache, "040-other", faction, group, 2);
        var target = (UnitEntityData)Life("Unit", cache, "040-target", faction, group, 3);
        var candidates = (List<UnitEntityData>)AccessTools.Field(typeof(TimeStopLifecycle01438Tests), "Candidates").GetValue(null);
        candidates.Clear(); candidates.AddRange(new[] { caster, other, target });
        Func<UnitEntityData, int> buttons = u => u.Abilities.Enumerable.Count(a => a.Blueprint == dismiss);
        Func<UnitEntityData, int> frozen = u => u.Buffs.Enumerable.Count(b => b.Blueprint == effect);
        Check(buttons(caster) == 0 && !gate.IsCasterRestrictionPassed(caster), "no button or permission before casting");
        var unrelatedBp = new BlueprintBuff { name = "040-unrelated", AssetGuid = BlueprintGuid.Parse("04000000000000000000000000000002"), ComponentsArray = Array.Empty<BlueprintComponent>() }; unrelatedBp.OnEnable();
        var unrelated = target.Facts.Add(new Buff(unrelatedBp, new MechanicsContext(target, target.Descriptor, unrelatedBp), TimeSpan.FromMinutes(1)));
        var first = Cast(cache, caster);
        Check(buttons(caster) == 1 && gate.IsCasterRestrictionPassed(caster) && buttons(other) == 0 && !gate.IsCasterRestrictionPassed(other), "actual native AddFacts grants only active caster");
        Check(frozen(caster) == 0 && frozen(other) == 1 && frozen(target) == 1, "real native membership excludes caster and freezes other creatures");
        var grant = first.Item1.Components.Single(x => x.SourceBlueprintComponent is AddFacts);
        using (grant.RequestEventContext()) { AccessTools.Method(typeof(AddFacts), "OnApplyPostLoadFixes").Invoke(grant.SourceBlueprintComponent, null); AccessTools.Method(typeof(AddFacts), "OnApplyPostLoadFixes").Invoke(grant.SourceBlueprintComponent, null); }
        Check(buttons(caster) == 1, "native post-load reconciliation grants once without duplicates");
        Act(dismiss, caster, target); End(first);
        Check(buttons(caster) == 0 && !gate.IsCasterRestrictionPassed(caster) && !caster.Buffs.Enumerable.Any(b => b.Blueprint == hostBp), "actual native removal revokes ability and stale caster permission");
        Check(new[] { caster, other, target }.All(u => frozen(u) == 0 && (int)Call(engine, "OwnContributions", u) == 0 && !u.State.HasCondition(UnitCondition.CantMove) && u.State.Features.IsUntargetable.Count == 0), "area end clears every source freeze, movement and untargetable flag");
        Check(!unrelated.IsDisposed && unrelated.IsTurnedOn, "unrelated target buff survives manual end");
        // Overlap two independent casters. Deliberately supply the other caster
        // as target: native ToCaster must still remove only the caller's host.
        var second = Cast(cache, caster); var independent = Cast(cache, other);
        Check(frozen(target) == 2 && buttons(caster) == 1 && buttons(other) == 1, "independent source has separate freeze and button");
        Act(dismiss, caster, other); End(second);
        Check(!independent.Item1.IsDisposed && !independent.Item2.IsEnded && buttons(other) == 1 && frozen(target) == 1 && target.State.HasCondition(UnitCondition.CantMove) && (int)Call(engine, "OwnContributions", target) == 1, "manual end preserves other caster, area and freeze contribution");
        Act(dismiss, caster, other);
        Check(!independent.Item1.IsDisposed && frozen(target) == 1, "stale action execution cannot end another caster's stop");
        Act(dismiss, other, other); End(independent);
        Check(frozen(target) == 0 && buttons(other) == 0 && !unrelated.IsDisposed, "last independent source cleans exactly its own effects");
        var natural = Cast(cache, caster);
        AccessTools.Field(typeof(TimeStutter01437Tests), "now").SetValue(null, natural.Item1.EndTime);
        caster.Buffs.TickBuff(natural.Item1, natural.Item1.EndTime); End(natural);
        Check(buttons(caster) == 0 && !gate.IsCasterRestrictionPassed(caster) && frozen(target) == 0, "natural expiry removes temporary button and all owned effects");
        Check(Asset<BlueprintBuff>(cache, "TimeStutterHost").GetComponent<AddFacts>() == null, "Time Stutter host unchanged");
        Call(module, "Register", cache); Check(ReferenceEquals(dismiss, Asset<BlueprintAbility>(cache, "TimeStopDismiss01440")), "repeat publication retains exact button identity");
        // Prove the fifth node participates in transaction conflict protection.
        var original = ResourcesLibrary.BlueprintsCache;
        var fresh = (BlueprintsCache)Call(typeof(TimeStopSpell01438Tests), "Fresh", cache);
        try {
            AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, fresh);
            fresh.AddCachedBlueprint(dismiss.AssetGuid, dismiss); bool rejected = false;
            try { Call(module, "Register", fresh); } catch (TargetInvocationException e) { rejected = e.InnerException is InvalidOperationException; }
            Check(rejected && ReferenceEquals(fresh.Load(dismiss.AssetGuid), dismiss) && Asset<BlueprintAbility>(fresh, "TimeStopSpell01438") == null, "foreign child collision rejected without partial spell or deleting foreign asset");
        } finally { AccessTools.Field(typeof(ResourcesLibrary), "BlueprintsCache").SetValue(null, original); candidates.Clear(); }
        PsychicExpansionTests.Inventory["timeStopDismiss01440Contracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["additionalItemCount"] = 0, ["newSpellRootCount"] = 0,
            ["newBlueprints"] = new JArray(new JObject { ["token"] = "TimeStopDismiss01440", ["id"] = dismiss.AssetGuid.ToString(), ["type"] = dismiss.GetType().FullName }),
            ["nativeCasts"] = casts, ["nativeAreaEnds"] = ends, ["nativePostLoadReconciliations"] = 2,
            ["legacyTestDicePrefixIsolatedAndRestored"] = true,
            ["sourceSpecificCleanup"] = true, ["unrelatedBuffPreserved"] = true, ["independentSourcePreserved"] = true,
            ["boundary"] = "Real native AddFacts activation/deactivation/post-load hook, caster restriction, PrepareCast/Apply, ContextActionRemoveBuff, Buff.Remove, AddAreaEffect.ForceEnd, UpdateUnits/UpdateUnit and HandleEnd execute. Existing scene candidates, geometry tail, controller dispatch, clock and Unity/UI endpoints remain controlled. ForceEnd requests cleanup; native HandleEnd runs at the area-controller dispatch. Rendered action bar, scene frames and complete game save/load are observation steps in the guide."
        };
        Console.WriteLine("PASS: " + checks + " TimeStopDismiss01440 checks."); return checks;
    }
}
