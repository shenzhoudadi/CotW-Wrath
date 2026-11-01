using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Persistence.Scenes;
using Kingmaker.Controllers.Combat;
using Kingmaker.Utility;
using TurnBased.Controllers;
using CallOfTheWild.Wrath.Prototype;

internal static class TimeStopTimers01438Tests
{
    static readonly Type Engine = typeof(ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopEngine01438", true);
    static readonly Type Timers = Engine.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopTimers01438", true);
    static readonly Type State = Engine.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopFreezeState01438", true);
    static int checks, periodicCalls, queueCalls, directCalls, serial, addedEvents, removedEvents, reboundComponents, turnWindowCasts;
    static bool turnBased;
    static readonly BlueprintArea SceneArea = new BlueprintArea { name = "038 timer scene", IsGlobalMap = false };
    static object Get(object o, string name) => (AccessTools.Field(o.GetType(), name) ?? AccessTools.Field(o.GetType(), "<" + name + ">k__BackingField")).GetValue(o);
    static void Set(object o, string name, object value) => (AccessTools.Field(o.GetType(), name) ?? AccessTools.Field(o.GetType(), "<" + name + ">k__BackingField")).SetValue(o, value);
    static object Old(string name, params object[] args) => AccessTools.Method(typeof(TimeStutter01437Tests), name).Invoke(null, args);
    static object E(string name, params object[] args) => AccessTools.Method(Engine, name).Invoke(null, args);
    static object T(string name, params object[] args) => AccessTools.Method(Timers, name).Invoke(null, args);
    static TAsset Asset<TAsset>(BlueprintsCache c, string token) => (TAsset)AccessTools.Method(typeof(TimeStutter01437Tests), "Asset").MakeGenericMethod(typeof(TAsset)).Invoke(null, new object[] { c, token });
    static TimeSpan Now { get => (TimeSpan)AccessTools.Field(typeof(TimeStutter01437Tests), "now").GetValue(null); set => AccessTools.Field(typeof(TimeStutter01437Tests), "now").SetValue(null, value); }
    static TimeSpan Next(Buff buff) => (TimeSpan)AccessTools.Property(typeof(Buff), "NextTickTime").GetValue(buff, null);
    static void Next(Buff buff, TimeSpan value) => AccessTools.Property(typeof(Buff), "NextTickTime").SetValue(buff, value, null);
    static void Check(bool value, string message) { if (!value) throw new Exception("TimeStopTimers01438: " + message); checks++; }
    static bool Area(ref BlueprintArea __result) { __result = SceneArea; return false; }
    static bool Mode(ref bool __result) { __result = turnBased; return false; }
    sealed class CountRound : GameAction { public override string GetCaption() => "038 native periodic counter"; public override void RunAction() { periodicCalls++; } }
    sealed class BuffEvents : IUnitBuffHandler {
        public void HandleBuffDidAdded(Buff buff) { if (buff.Blueprint.name.StartsWith("038-timer-", StringComparison.Ordinal)) addedEvents++; }
        public void HandleBuffDidRemoved(Buff buff) { if (buff.Blueprint.name.StartsWith("038-timer-", StringComparison.Ordinal)) removedEvents++; }
    }
    static UnitEntityData Unit(BlueprintsCache c, string label)
    {
        var unit = (UnitEntityData)Old("Unit", c, "038-timers-" + label, 20, 0);
        Set(unit, "m_Position", new UnityEngine.Vector3(++serial * 3, 0, 0)); return unit;
    }
    static BlueprintBuff Blueprint(string label, bool periodic = false, StackingType stacking = StackingType.Stack)
    {
        var components = periodic ? new BlueprintComponent[] { new AddFactContextActions { NewRound = new ActionList { Actions = new GameAction[] { new CountRound() } } } } : Array.Empty<BlueprintComponent>();
        var bp = new BlueprintBuff { name = "038-timer-" + label, AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), Stacking = stacking, ComponentsArray = components }; bp.OnEnable(); return bp;
    }
    static Buff Add(UnitEntityData unit, BlueprintBuff bp, double? seconds)
    {
        var buff = new Buff(bp, new MechanicsContext(unit, unit.Descriptor, bp), seconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(seconds.Value) : null); return unit.Facts.Add(buff);
    }
    static Tuple<Buff, AreaEffectEntityData> Freeze(BlueprintsCache c, UnitEntityData caster, UnitEntityData target, bool spell = false, Metamagic meta = 0)
    {
        string hostToken = spell ? "TimeStopHost01438" : "TimeStutterHost", areaToken = spell ? "TimeStopArea01438" : "TimeStutterArea", effectToken = spell ? "TimeStopEffect01438" : "TimeStutterEffect";
        var bp = Asset<BlueprintAbility>(c, spell ? "TimeStopSpell01438" : "TimeStutterAbility"); var data = new AbilityData(bp, caster.Descriptor);
        data.MetamagicData = new MetamagicData(); data.MetamagicData.Add(meta);
        var context = new AbilityExecutionContext(data, new AbilityParams { CasterLevel = 20, SpellLevel = spell ? 9 : 0, Metamagic = meta }, new Kingmaker.Utility.TargetWrapper(caster), null, caster);
        AbilityExecutionProcess.PrepareCast(context);
        using (context.GetDataScope(caster)) bp.GetComponent<AbilityEffectRunAction>().Apply(context, new Kingmaker.Utility.TargetWrapper(caster));
        var host = caster.Buffs.Enumerable.Single(b => b.Blueprint == Asset<BlueprintBuff>(c, hostToken));
        var areaBlueprint = Asset<BlueprintAbilityAreaEffect>(c, areaToken);
        var area = (AreaEffectEntityData)Old("MakeArea", "038-timers-" + ++serial, host.Context, areaBlueprint, new[] { caster, target });
        Old("BindArea", host, area); areaBlueprint.HandleUnitEnter(host.Context, area, target);
        Check(target.Buffs.Enumerable.Any(b => b.Blueprint == Asset<BlueprintBuff>(c, effectToken)) && (int)E("OwnContributions", target) > 0, "native area enter activates exact registered freeze marker");
        return Tuple.Create(host, area);
    }
    static void End(Tuple<Buff, AreaEffectEntityData> source)
    {
        source.Item1.Owner.Facts.Remove(source.Item1);
        Check(source.Item2.IsEnded, "actual source deactivation requests native area end");
        AccessTools.Method(typeof(AreaEffectEntityData), "HandleEnd").Invoke(source.Item2, null);
        Check(source.Item2.DestroyMark, "actual native area end exits and destroys the source");
    }
    static void Queue(UnitEntityData unit) { unit.Buffs.Tick(null); queueCalls++; }
    static void Direct(UnitEntityData unit, Buff buff) { unit.Buffs.TickBuff(buff, Now); directCalls++; }
    static object Runtime(Buff marker) => marker.Components.Single(c => c.SourceBlueprintComponent.GetType().Name == "TimeStopFreezeMarker01438");
    static object Data(object runtime) => runtime.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(m => m.Name == "GetData" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(State).Invoke(runtime, null);
    static void ReloadMarkers(params Buff[] markers) => RestoreMarkers(true, markers);
    static void RestoreMarkers(bool applyFixes, params Buff[] markers)
    {
        foreach (var marker in markers) {
            var runtime = Runtime(marker); var data = Data(runtime); var restored = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data), State);
            foreach (var field in State.GetFields(BindingFlags.Public | BindingFlags.Instance)) field.SetValue(data, field.GetValue(restored));
            var component = AccessTools.Property(runtime.GetType(), "SourceBlueprintComponent").GetValue(runtime, null);
            Check(!string.IsNullOrEmpty((string)AccessTools.Property(runtime.GetType(), "SourceBlueprintComponentName").GetValue(runtime, null)), "published persistent marker has a nonempty native component identity");
            AccessTools.Field(runtime.GetType(), "<SourceBlueprintComponent>k__BackingField").SetValue(runtime, null);
            AccessTools.Method(typeof(Kingmaker.EntitySystem.EntityFact), "PostLoadComponents").Invoke(marker, null);
            Check(ReferenceEquals(AccessTools.Property(runtime.GetType(), "SourceBlueprintComponent").GetValue(runtime, null), component) && ReferenceEquals(Data(runtime), data), "native PostLoadComponents rebinds published marker without losing its serialized data"); reboundComponents++;
        }
        foreach (var pair in new[] { Tuple.Create(Timers, "Paused"), Tuple.Create(Engine, "Units") }) {
            var table = AccessTools.Field(pair.Item1, pair.Item2).GetValue(null); AccessTools.Method(table.GetType(), "Remove").Invoke(table, new object[] { markers[0].Owner.Unit });
        }
        if (applyFixes) foreach (var marker in markers) {
            var runtime = Runtime(marker);
            using ((IDisposable)AccessTools.Method(runtime.GetType(), "RequestEventContext").Invoke(runtime, null)) {
                var component = AccessTools.Property(runtime.GetType(), "SourceBlueprintComponent").GetValue(runtime, null);
                AccessTools.Method(component.GetType(), "OnApplyPostLoadFixes").Invoke(component, null);
                AccessTools.Method(component.GetType(), "OnApplyPostLoadFixes").Invoke(component, null);
            }
        }
    }
    static void QueueAndExpiry(BlueprintsCache cache)
    {
        var start = Now; var caster = Unit(cache, "queue-caster"); var target = Unit(cache, "queue-target"); var other = Unit(cache, "unfrozen");
        var periodic = Add(target, Blueprint("periodic", true), 20); Next(periodic, start + TimeSpan.FromSeconds(6));
        var infinite = Add(target, Blueprint("infinite"), null); Next(infinite, TimeSpan.MaxValue);
        var overdue = Add(target, Blueprint("overdue"), 1); overdue.EndTime = start - TimeSpan.FromSeconds(1); Next(overdue, start - TimeSpan.FromSeconds(2));
        var unpaused = Add(other, Blueprint("ordinary-other"), 20);
        var prefix = AccessTools.Method(Engine.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopPausedTimeLeft01438Patch", true), "Prefix");
        var fallback = new object[] { unpaused, TimeSpan.FromSeconds(321) };
        Check((bool)prefix.Invoke(null, fallback) && (TimeSpan)fallback[1] == TimeSpan.FromSeconds(321), "non-owned prefix fallback preserves another patch's existing result instead of overwriting it");
        var source = Freeze(cache, caster, target); int before = periodicCalls;
        Check(periodic.NextEventTime == TimeSpan.MaxValue && overdue.NextEventTime == TimeSpan.MaxValue && infinite.IsPermanent, "paused queue hides even overdue events without changing permanent state");
        Now = start + TimeSpan.FromSeconds(4);
        Check(periodic.TimeLeft == TimeSpan.FromSeconds(20) && unpaused.TimeLeft == TimeSpan.FromSeconds(16) && overdue.TimeLeft == TimeSpan.Zero && infinite.TimeLeft == TimeSpan.MaxValue, "native remaining-time getter pauses only exact frozen facts while preserving overdue zero and permanent sentinel");
        for (int i = 0; i < 1000; i++) Queue(target);
        Direct(target, periodic); Direct(target, overdue);
        Check(periodicCalls == before && !overdue.IsDisposed && periodic.EndTime == start + TimeSpan.FromSeconds(20) && Next(periodic) == start + TimeSpan.FromSeconds(6), "real queue while-loop terminates repeatedly and direct native TickBuff cannot expire or tick paused facts");
        Check(overdue.EndTime == start - TimeSpan.FromSeconds(1) && Next(overdue) == start - TimeSpan.FromSeconds(2) && infinite.EndTime == TimeSpan.MaxValue && Next(infinite) == TimeSpan.MaxValue, "finite overdue and infinite actual timer fields remain untouched while frozen");
        Check(unpaused.NextEventTime < TimeSpan.MaxValue && unpaused.EndTime == start + TimeSpan.FromSeconds(20), "another unit receives no paused queue or duration change");
        Now = start + TimeSpan.FromSeconds(6); Direct(caster, source.Item1);
        Check(source.Item1.IsDisposed && source.Item2.IsEnded, "own host remains native-tickable and expires during target pause");
        AccessTools.Method(typeof(AreaEffectEntityData), "HandleEnd").Invoke(source.Item2, null);
        Check(periodic.EndTime == start + TimeSpan.FromSeconds(26) && Next(periodic) == start + TimeSpan.FromSeconds(12) && !(bool)T("IsPaused", periodic), "last source credits actual six elapsed seconds once and restores native queue visibility");
        Check(overdue.EndTime == start + TimeSpan.FromSeconds(5) && Next(overdue) == start + TimeSpan.FromSeconds(4), "pre-freeze overdue offsets remain overdue after thaw rather than being revived");
        Queue(target); Check(overdue.IsDisposed && periodicCalls == before, "actual native thawed queue removes overdue fact once without firing paused periodic rounds");
        Check(infinite.IsPermanent && infinite.EndTime == TimeSpan.MaxValue && Next(infinite) == TimeSpan.MaxValue && unpaused.EndTime == start + TimeSpan.FromSeconds(20), "thaw preserves infinite sentinel and unrelated unit exactly");
        Check(periodic.TimeLeft == TimeSpan.FromSeconds(20) && unpaused.TimeLeft == TimeSpan.FromSeconds(14), "thaw returns to genuine native TimeLeft without a remaining-time jump");
        Now = start + TimeSpan.FromSeconds(12); Queue(target);
        Check(periodicCalls == before + 1 && Next(periodic) == start + TimeSpan.FromSeconds(18), "first resumed native periodic deadline runs the actual NewRound action exactly once");
        Queue(target);
        Check(periodicCalls == before + 1, "repeating native queue at the same resumed deadline cannot fire duplicate periodic actions");
    }
    static void OverlapReloadSuppression(BlueprintsCache cache)
    {
        Now += TimeSpan.FromSeconds(20); var start = Now; var a = Unit(cache, "overlap-a"); var b = Unit(cache, "overlap-b"); var target = Unit(cache, "overlap-target");
        var original = Add(target, Blueprint("overlap-original"), 30); Next(original, start + TimeSpan.FromSeconds(6));
        var first = Freeze(cache, a, target); Now = start + TimeSpan.FromSeconds(1); var second = Freeze(cache, b, target);
        Check(first.Item2.UniqueId != second.Item2.UniqueId, "scene allocation gives separate real source-area identities to overlapping casts");
        Now = start + TimeSpan.FromSeconds(2); int observed = addedEvents; var late = Add(target, Blueprint("created-during-freeze"), 10); Next(late, Now + TimeSpan.FromSeconds(6));
        Check(addedEvents == observed + 1 && (bool)T("IsPaused", late) && late.NextEventTime == TimeSpan.MaxValue, "actual native BuffDidAdded event enrolls a newly attached buff during overlapping freeze");
        Check(late.TimeLeft == TimeSpan.FromSeconds(10) && original.TimeLeft == TimeSpan.FromSeconds(30), "newly created buff and preexisting buff retain their distinct remaining durations");
        var both = target.Buffs.Enumerable.Where(f => f.Blueprint == Asset<BlueprintBuff>(cache, "TimeStutterEffect")).ToArray();
        Check(both.Length == 2 && both.Count(f => (bool)State.GetField("OwnsTimers").GetValue(Data(Runtime(f)))) == 1, "two actual overlapping sources retain one persistent coordinator: " + string.Join(";", both.Select(f => f.UniqueId + "/" + f.SourceAreaEffectId + "/" + JsonConvert.SerializeObject(Data(Runtime(f))))));
        var coordinator = both.Single(f => (bool)State.GetField("OwnsTimers").GetValue(Data(Runtime(f)))); var follower = both.Single(f => !ReferenceEquals(f, coordinator));
        Now = start + TimeSpan.FromSeconds(2.5); ReloadMarkers(follower, coordinator);
        Check((int)E("OwnContributions", target) == 2 && (bool)State.GetField("OwnsTimers").GetValue(Data(Runtime(coordinator))) && !(bool)State.GetField("OwnsTimers").GetValue(Data(Runtime(follower))), "noncoordinator-first native post-load order adopts saved coordinator offsets rather than restarting elapsed time");
        Now = start + TimeSpan.FromSeconds(3); End(first);
        Check(original.EndTime == start + TimeSpan.FromSeconds(30) && (bool)T("IsPaused", original), "ending first overlapping source transfers coordinator without prematurely adding time");
        var marker = target.Buffs.Enumerable.Single(f => f.Blueprint == Asset<BlueprintBuff>(cache, "TimeStutterEffect"));
        Now = start + TimeSpan.FromSeconds(4); ReloadMarkers(marker);
        Check((int)E("OwnContributions", target) == 1 && original.NextEventTime == TimeSpan.MaxValue && late.NextEventTime == TimeSpan.MaxValue, "serialized state and real marker post-load hook restore transient ledgers idempotently");
        Check(late.TimeLeft == TimeSpan.FromSeconds(10) && original.TimeLeft == TimeSpan.FromSeconds(30), "actual reload hooks restore frozen remaining-time reads without restarted timers");
        Now = start + TimeSpan.FromSeconds(5);
        var suppress = target.Descriptor.Ensure<UnitPartBuffSuppress>(); suppress.PrepareForSuppression(marker.Blueprint); suppress.ApplyChanges();
        Check(marker.IsSuppressed && original.EndTime == start + TimeSpan.FromSeconds(35) && late.EndTime == start + TimeSpan.FromSeconds(15), "actual native suppression thaws five seconds of original and three seconds of newly created buff");
        Check(!(bool)T("IsPaused", original) && (int)E("OwnContributions", target) == 0, "suppression removes paused and targeting receipts without deleting the source buff");
        Check(late.TimeLeft == TimeSpan.FromSeconds(10) && original.TimeLeft == TimeSpan.FromSeconds(30), "native suppression resumes remaining-time reads continuously");
        Now = start + TimeSpan.FromSeconds(5.5); suppress.PrepareForRelease(marker.Blueprint); suppress.ApplyChanges();
        Check(!marker.IsSuppressed && (bool)T("IsPaused", original) && (int)E("OwnContributions", target) == 1, "native unsuppression begins a fresh independent pause interval");
        Now = start + TimeSpan.FromSeconds(6); End(second);
        Check(original.EndTime == start + TimeSpan.FromSeconds(35.5) && late.EndTime == start + TimeSpan.FromSeconds(15.5), "final early source end credits only the half-second resumed interval without overlap or save-load double counting");
        Check(Next(original) == start + TimeSpan.FromSeconds(11.5) && Next(late) == start + TimeSpan.FromSeconds(11.5), "periodic next-tick fields receive the same exact interval union");
        Check(late.TimeLeft == TimeSpan.FromSeconds(9.5) && original.TimeLeft == TimeSpan.FromSeconds(29.5), "only the unsuppressed half-second gap consumes remaining duration");
    }
    static void NativeRefreshAndClockRollback(BlueprintsCache cache)
    {
        Now += TimeSpan.FromSeconds(20); var start = Now; var caster = Unit(cache, "refresh-caster"); var target = Unit(cache, "refresh-target");
        var bp = Blueprint("native-prolong", false, StackingType.Prolong); var buff = Add(target, bp, 30); Next(buff, start + TimeSpan.FromSeconds(6)); var source = Freeze(cache, caster, target);
        Now = start + TimeSpan.FromSeconds(2); var reused = Add(target, bp, 40);
        Check(ReferenceEquals(reused, buff) && buff.EndTime == start + TimeSpan.FromSeconds(42), "native PrepareFactForAttach Prolong refresh sees settled old timer and authored new duration");
        Now = start + TimeSpan.FromSeconds(4); End(source);
        Check(buff.EndTime == start + TimeSpan.FromSeconds(44) && Next(buff) == start + TimeSpan.FromSeconds(10), "refresh gets only subsequent paused credit while periodic offset gets total elapsed freeze");
        Now = start + TimeSpan.FromSeconds(10); var rollbackCaster = Unit(cache, "rollback-caster"); var rollback = Freeze(cache, rollbackCaster, target); var previous = buff.EndTime;
        Now = start + TimeSpan.FromSeconds(9); End(rollback);
        Check(buff.EndTime == previous && !(bool)T("IsPaused", buff), "backward clock never subtracts duration or leaves a paused receipt");
    }
    static void EarlyLoadedRemovalAndTeardown(BlueprintsCache cache)
    {
        Now += TimeSpan.FromSeconds(30); var start = Now; var a = Unit(cache, "loaded-early-a"); var b = Unit(cache, "loaded-early-b"); var target = Unit(cache, "loaded-early-target");
        var ordinary = Add(target, Blueprint("loaded-early"), 30); Next(ordinary, start + TimeSpan.FromSeconds(6));
        var first = Freeze(cache, a, target); Now = start + TimeSpan.FromSeconds(1); var second = Freeze(cache, b, target);
        var markers = target.Buffs.Enumerable.Where(f => f.Blueprint == Asset<BlueprintBuff>(cache, "TimeStutterEffect")).ToArray();
        Check(markers.Length == 2, "early-load removal fixture owns two distinct source effects");
        Now = start + TimeSpan.FromSeconds(2); RestoreMarkers(false, markers);
        Now = start + TimeSpan.FromSeconds(3); End(first);
        Check((bool)T("IsPaused", ordinary) && ordinary.EndTime == start + TimeSpan.FromSeconds(30), "coordinator removal before marker load fixes recovers and transfers persisted timer state to the surviving source");
        Now = start + TimeSpan.FromSeconds(4); End(second);
        Check(!(bool)T("IsPaused", ordinary) && ordinary.EndTime == start + TimeSpan.FromSeconds(34) && Next(ordinary) == start + TimeSpan.FromSeconds(10), "early-load final removal credits the persisted interval once");
        Now += TimeSpan.FromSeconds(30); start = Now; var teardownCaster = Unit(cache, "teardown-caster"); var teardownTarget = Unit(cache, "teardown-target");
        var teardownBuff = Add(teardownTarget, Blueprint("teardown"), 20); var source = Freeze(cache, teardownCaster, teardownTarget);
        Now = start + TimeSpan.FromSeconds(3); teardownCaster.Facts.Remove(source.Item1);
        var game = Game.Instance; var oldTime = game.TimeController; var oldPlayerTime = game.State.PlayerState.GameTime; game.State.PlayerState.GameTime = Now;
        Set(game, "TimeController", null);
        try { AccessTools.Method(typeof(AreaEffectEntityData), "HandleEnd").Invoke(source.Item2, null); }
        finally { Set(game, "TimeController", oldTime); game.State.PlayerState.GameTime = oldPlayerTime; }
        Check(!(bool)T("IsPaused", teardownBuff) && teardownBuff.EndTime == start + TimeSpan.FromSeconds(23) && source.Item2.DestroyMark, "actual area teardown without TimeController uses persisted player time and leaves no paused receipt");
    }
    static TurnController Turn(UnitEntityData caster)
    {
        var turn = (TurnController)AccessTools.Method(typeof(Gambit01414Tests), "Turn").Invoke(null, new object[] { caster, null });
        Set(Game.Instance.TurnBasedCombatController, "m_CurrentTurn", turn); return turn;
    }
    static void TurnWindows(BlueprintsCache cache, Harmony harmony)
    {
        var game = Game.Instance; var previous = game.TurnBasedCombatController;
        var mode = AccessTools.Method(typeof(CombatController), "IsInTurnBasedCombat"); var oldNo = AccessTools.Method(typeof(TimeStutter01437Tests), "No");
        harmony.Unpatch(mode, oldNo); harmony.Patch(mode, prefix: new HarmonyMethod(typeof(TimeStopTimers01438Tests), nameof(Mode)));
        Set(game, "TurnBasedCombatController", System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(CombatController)));
        try {
            turnBased = true; Now += TimeSpan.FromSeconds(40); var start = Now;
            Set(game.TurnBasedCombatController, "TurnStartTime", start);
            var caster = Unit(cache, "tb-standard-caster"); var target = Unit(cache, "tb-standard-target"); Turn(caster);
            var ordinary = Add(target, Blueprint("tb-target-duration"), 30); Next(ordinary, start + TimeSpan.FromSeconds(6));
            var source = Freeze(cache, caster, target); turnWindowCasts++;
            Check(source.Item1.EndTime == start + TimeSpan.FromSeconds(12), "actual native standard Time Stutter ApplyBuff gives one full next-turn window after current cast");
            caster.SpendAction(UnitCommand.CommandType.Standard, false, 0);
            Check(caster.CombatState.Cooldown.StandardAction == 6, "native standard action spending consumes the current casting turn");
            Now = start + TimeSpan.FromSeconds(6); Queue(caster);
            Check(!source.Item1.IsDisposed && game.TurnBasedCombatController.TurnStartTime == start, "native pre-StartTurn queue uses previous TurnStartTime despite advanced GameTime");
            Set(game.TurnBasedCombatController, "TurnStartTime", Now); Queue(caster);
            AccessTools.Method(typeof(UnitCombatState.Cooldowns), "Clear").Invoke(caster.CombatState.Cooldown, null);
            Check(!source.Item1.IsDisposed && caster.CombatState.Cooldown.StandardAction == 0 && target.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.CantAct), "native next-turn queue and cooldown reset expose a usable standard action while others remain frozen");
            Now = start + TimeSpan.FromSeconds(12); Queue(caster);
            Check(!source.Item1.IsDisposed, "host cannot expire against wall clock before native turn-start update");
            Set(game.TurnBasedCombatController, "TurnStartTime", Now); Queue(caster);
            Check(source.Item1.IsDisposed && source.Item2.IsEnded, "following native turn-start queue expires the protected host");
            AccessTools.Method(typeof(AreaEffectEntityData), "HandleEnd").Invoke(source.Item2, null);
            Check(ordinary.EndTime == start + TimeSpan.FromSeconds(42) && Next(ordinary) == start + TimeSpan.FromSeconds(18) && !(bool)T("IsPaused", ordinary), "actual TB expiry credits the complete twelve-second pause including lagged final turn update");
            foreach (var meta in new[] { Metamagic.Maximize, Metamagic.Maximize | Metamagic.Quicken }) {
                Now += TimeSpan.FromSeconds(40); Set(game.TurnBasedCombatController, "TurnStartTime", Now); var castStart = Now;
                var wizard = Unit(cache, "tb-spell-" + meta); var victim = Unit(cache, "tb-spell-victim-" + meta); Turn(wizard);
                var spellSource = Freeze(cache, wizard, victim, true, meta); turnWindowCasts++;
                bool swift = meta.HasFlag(Metamagic.Quicken);
                Check(spellSource.Item1.EndTime == castStart + TimeSpan.FromSeconds(swift ? 30 : 36), "native Time Stop duration applies TB compensation only to effective standard action " + meta);
                Check(spellSource.Item1.Context.SourceAbilityContext.Ability.ActionType == (swift ? UnitCommand.CommandType.Swift : UnitCommand.CommandType.Standard), "effective native ActionType, including Quicken, controls the one-round compensation " + meta);
                End(spellSource);
            }
            Now += TimeSpan.FromSeconds(40); Set(game.TurnBasedCombatController, "TurnStartTime", Now);
            var foreignCaster = Unit(cache, "tb-native-foreign"); Turn(foreignCaster);
            var native = (BlueprintAbility)cache.Load(BlueprintGuid.Parse("70e55e5b0ac84af78c084eb6cf47ae06")); var nativeData = new AbilityData(native, foreignCaster.Descriptor);
            nativeData.MetamagicData = new MetamagicData(); nativeData.MetamagicData.Add(Metamagic.Maximize);
            var nativeContext = new AbilityExecutionContext(nativeData, new AbilityParams { Metamagic = Metamagic.Maximize }, new TargetWrapper(foreignCaster), null, foreignCaster);
            var nativeApply = (ContextActionApplyBuff)native.GetComponent<AbilityEffectRunAction>().Actions.Actions.Single();
            AbilityExecutionProcess.PrepareCast(nativeContext); var expected = nativeApply.DurationValue.Calculate(nativeContext).Seconds;
            using (nativeContext.GetDataScope(foreignCaster)) native.GetComponent<AbilityEffectRunAction>().Apply(nativeContext, new TargetWrapper(foreignCaster)); turnWindowCasts++;
            var nativeHost = foreignCaster.Buffs.Enumerable.Single(b => b.Blueprint.AssetGuid == BlueprintGuid.Parse("f4d026d3bf5a4e8183ded452e8d87260"));
            Check(expected == TimeSpan.FromSeconds(30) && nativeHost.EndTime == Now + expected, "unregistered native host duration receives no private compensation"); foreignCaster.Facts.Remove(nativeHost);
            turnBased = false; Now += TimeSpan.FromSeconds(40); var realtime = Now;
            var realtimeSource = Freeze(cache, Unit(cache, "rt-caster"), Unit(cache, "rt-target")); turnWindowCasts++;
            Check(realtimeSource.Item1.EndTime == realtime + TimeSpan.FromSeconds(6), "actual realtime Time Stutter retains one-round base duration"); End(realtimeSource);
        } finally {
            turnBased = false; Set(game, "TurnBasedCombatController", previous); harmony.Unpatch(mode, AccessTools.Method(typeof(TimeStopTimers01438Tests), nameof(Mode))); harmony.Patch(mode, prefix: new HarmonyMethod(oldNo));
        }
    }
    internal static void EnsurePatches(Harmony harmony)
    {
        foreach (string patch in new[] { "TimeStopPausedQueue01438Patch", "TimeStopPausedTimeLeft01438Patch", "TimeStopPausedTick01438Patch", "TimeStopRefresh01438Patch", "TimeStopTurnWindow01438Patch" }) {
            var type = Engine.Assembly.GetType("CallOfTheWild.Wrath.Prototype." + patch, true);
            bool installed = Harmony.GetAllPatchedMethods().Any(method => {
                var info = Harmony.GetPatchInfo(method);
                return info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Any(p => p.owner == harmony.Id && p.PatchMethod.DeclaringType == type);
            });
            if (!installed) harmony.CreateClassProcessor(type).Patch();
        }
    }
    internal static int RunNative(BlueprintsCache cache, Harmony harmony)
    {
        checks = periodicCalls = queueCalls = directCalls = serial = addedEvents = removedEvents = reboundComponents = turnWindowCasts = 0; var prior = Now; var observer = new BuffEvents(); EventBus.Subscribe(observer);
        var game = Game.Instance; var previousScene = Get(game, "m_SceneLoader");
        var scene = (SceneLoader)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SceneLoader)); Set(scene, "CurrentlyLoadedArea", SceneArea); Set(game, "m_SceneLoader", scene);
        EnsurePatches(harmony);
        try { QueueAndExpiry(cache); OverlapReloadSuppression(cache); NativeRefreshAndClockRollback(cache); EarlyLoadedRemovalAndTeardown(cache); TurnWindows(cache, harmony); }
        finally { Now = prior; EventBus.Unsubscribe(observer); Set(game, "m_SceneLoader", previousScene); }
        PsychicExpansionTests.Inventory["timeStopTimers01438Contracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["nativeBuffCollectionTickCalls"] = queueCalls, ["directNativeTickBuffAttempts"] = directCalls, ["periodicActionsObserved"] = periodicCalls,
            ["nativeQueueOverdueAndPermanentBoundaries"] = true, ["nativeSuppressionAndUnsuppression"] = true, ["nativeRefresh"] = true, ["serializedMarkerPostLoadHook"] = true,
            ["nativeAddedEvents"] = addedEvents, ["nativeRemovedEvents"] = removedEvents, ["nativeComponentRebindings"] = reboundComponents, ["noncoordinatorFirstLoadOrder"] = true,
            ["nativeTurnWindowCasts"] = turnWindowCasts, ["turnBasedLaggedTurnStartTime"] = true, ["standardSwiftAndForeignDurations"] = true,
            ["nonOwnedPrefixResultPreserved"] = true,
            ["nativeRemainingTimeGetter"] = true,
            ["loadedCoordinatorEarlyRemoval"] = true, ["areaCleanupWithoutTimeController"] = true,
            ["boundary"] = "Native BuffCollection.Tick while-loop, actual TickBuff, PrepareFactForAttach refresh, buff-added events, source cleanup, periodic action dispatch and UnitPartBuffSuppress.ApplyChanges execute. Serialization restores actual component data and native PostLoadComponents rebinds it before its post-load hook after transient caches are cleared, including noncoordinator-first overlapping markers; complete game save/load is not launched. Actual native standard/Quicken casts and queue expiry use a real CombatController/TurnController with controlled TB mode/TurnStartTime/GameTime. Native SpendAction and Cooldowns.Clear execute; full CombatController.StartTurn and TurnController.Prepare AI/scene scheduling do not. A real scene area is supplied as an endpoint; clock, world allocation, rule entry and UI endpoints remain controlled."
        };
        Console.WriteLine("PASS: " + checks + " TimeStopTimers01438 checks; " + queueCalls + " native queue calls."); return checks;
    }
}
