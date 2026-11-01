using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using TurnBased.Controllers;

namespace CallOfTheWild.Wrath.Prototype
{
    // End/next-tick fields remain genuine native timers. Only their event-queue
    // visibility is paused, then actual elapsed time is credited exactly once.
    public sealed class TimeStopPausedTimer01438
    {
        [JsonProperty] public string FactId;
        [JsonProperty] public TimeSpan LastSettled;
    }
    public sealed class TimeStopFreezeState01438
    {
        [JsonProperty] public bool OwnsTimers;
        [JsonProperty] public List<TimeStopPausedTimer01438> Timers = new List<TimeStopPausedTimer01438>();
    }
    internal static class TimeStopTimers01438
    {
        sealed class PausedUnit
        {
            internal readonly Dictionary<Buff, TimeStopFreezeState01438> Markers = new Dictionary<Buff, TimeStopFreezeState01438>();
            internal readonly Dictionary<Buff, TimeStopPausedTimer01438> Timers = new Dictionary<Buff, TimeStopPausedTimer01438>();
            internal TimeStopFreezeState01438 Coordinator;
        }
        static readonly PropertyInfo NextTick = AccessTools.Property(typeof(Buff), "NextTickTime");
        static readonly ConditionalWeakTable<UnitEntityData, PausedUnit> Paused = new ConditionalWeakTable<UnitEntityData, PausedUnit>();
        internal static TimeSpan Now => CombatController.IsInTurnBasedCombat() && Game.Instance.TurnBasedCombatController != null
            ? Game.Instance.TurnBasedCombatController.TurnStartTime : Game.Instance.TimeController.GameTime;
        static bool Ordinary(Buff buff) => buff != null && !buff.IsDisposed && !TimeStopEngine01438.IsProtectedTimerBuff(buff.Blueprint);
        // Native UnitEntityData.OnPostLoad rebuilds the buff queue before
        // UnitDescriptor.PostLoad assigns Descriptor.Unit. A descriptor is
        // therefore not proof that its implicit UnitEntityData conversion is
        // non-null. In this phase these observers must leave native timers alone.
        static UnitEntityData OwnerUnit(Buff buff) => buff?.Owner?.Unit;
        internal static bool IsPaused(Buff buff) {
            var owner = OwnerUnit(buff);
            return owner != null && Paused.TryGetValue(owner, out var state) && state.Markers.Count != 0 && state.Timers.ContainsKey(buff);
        }
        internal static bool TryTimeLeft(Buff buff, out TimeSpan remaining)
        {
            remaining = default;
            var owner = OwnerUnit(buff);
            if (owner == null || !Paused.TryGetValue(owner, out var state) || state.Markers.Count == 0 || !state.Timers.TryGetValue(buff, out var timer)) return false;
            remaining = buff.IsPermanent ? TimeSpan.MaxValue : buff.EndTime <= timer.LastSettled ? TimeSpan.Zero : buff.EndTime - timer.LastSettled;
            return true;
        }
        static void Add(PausedUnit state, Buff buff, TimeSpan now)
        {
            if (!Ordinary(buff) || string.IsNullOrEmpty(buff.UniqueId) || state.Timers.ContainsKey(buff)) return;
            var timer = new TimeStopPausedTimer01438 { FactId = buff.UniqueId, LastSettled = now };
            state.Timers.Add(buff, timer); state.Coordinator.Timers.Add(timer);
        }
        static void Adopt(UnitEntityData owner, PausedUnit state, TimeStopFreezeState01438 data)
        {
            // A saved coordinator can be restored after another overlapping
            // marker's post-load hook. Prefer its persisted offsets, retaining
            // any genuinely new facts added during load fixing.
            var records = new Dictionary<string, TimeStopPausedTimer01438>();
            foreach (var record in data.Timers ?? new List<TimeStopPausedTimer01438>())
                if (record != null && !string.IsNullOrEmpty(record.FactId) && !records.ContainsKey(record.FactId)) records.Add(record.FactId, record);
            foreach (var timer in state.Timers.Values)
                if (!records.ContainsKey(timer.FactId)) records.Add(timer.FactId, timer);
            if (state.Coordinator != null && !ReferenceEquals(state.Coordinator, data)) { state.Coordinator.OwnsTimers = false; state.Coordinator.Timers = new List<TimeStopPausedTimer01438>(); }
            data.OwnsTimers = true; data.Timers = new List<TimeStopPausedTimer01438>(); state.Coordinator = data; state.Timers.Clear();
            var now = Now;
            foreach (var buff in owner.Buffs.RawFacts) if (Ordinary(buff) && !string.IsNullOrEmpty(buff.UniqueId)) {
                if (!records.TryGetValue(buff.UniqueId, out var timer)) timer = new TimeStopPausedTimer01438 { FactId = buff.UniqueId, LastSettled = now };
                state.Timers.Add(buff, timer); data.Timers.Add(timer);
            }
        }
        internal static void Retain(Buff marker, TimeStopFreezeState01438 data)
        {
            var owner = OwnerUnit(marker);
            if (owner == null || !TimeStopEngine01438.IsRegisteredFreeze(marker.Blueprint)) return;
            var state = Paused.GetValue(owner, _ => new PausedUnit());
            if (state.Markers.ContainsKey(marker)) return;
            state.Markers.Add(marker, data);
            if (state.Coordinator == null) {
                if (data.OwnsTimers) Adopt(owner, state, data);
                else {
                    data.OwnsTimers = true; data.Timers = new List<TimeStopPausedTimer01438>(); state.Coordinator = data;
                    var now = Now; foreach (var buff in owner.Buffs.RawFacts) Add(state, buff, now);
                }
            } else if (data.OwnsTimers && !ReferenceEquals(state.Coordinator, data)) Adopt(owner, state, data);
            owner.Buffs.UpdateNextEvent();
        }
        static TimeSpan Plus(TimeSpan value, TimeSpan delta) => value == TimeSpan.MaxValue || value.Ticks > long.MaxValue - delta.Ticks ? TimeSpan.MaxValue : value + delta;
        static void Settle(Buff buff, TimeStopPausedTimer01438 timer, TimeSpan now)
        {
            if (buff.IsDisposed || now <= timer.LastSettled) return;
            var delta = now - timer.LastSettled;
            if (buff.EndTime < TimeSpan.MaxValue) buff.EndTime = Plus(buff.EndTime, delta);
            var next = (TimeSpan)NextTick.GetValue(buff, null);
            if (next < TimeSpan.MaxValue) NextTick.SetValue(buff, Plus(next, delta), null);
            timer.LastSettled = now;
        }
        internal static void Release(Buff marker, TimeStopFreezeState01438 data)
        {
            var owner = OwnerUnit(marker);
            if (owner == null) return;
            if (!Paused.TryGetValue(owner, out var state)) {
                if (!data.OwnsTimers) return;
                // Recovery when a loaded owning marker is removed before its
                // normal post-load hook: rebuild real surviving markers once.
                state = Paused.GetValue(owner, _ => new PausedUnit()); Adopt(owner, state, data);
                foreach (var buff in owner.Buffs.RawFacts.Where(b => !ReferenceEquals(b, marker) && b.IsTurnedOn && !b.IsSuppressed && !b.IsDisposed && TimeStopEngine01438.IsRegisteredFreeze(b.Blueprint)))
                    foreach (var runtime in buff.Components.Where(c => c.SourceBlueprintComponent is TimeStopFreezeMarker01438)) state.Markers[buff] = runtime.GetData<TimeStopFreezeState01438>();
            }
            state.Markers.Remove(marker);
            if (state.Markers.Count != 0) {
                if (ReferenceEquals(state.Coordinator, data)) {
                    var next = state.Markers.First().Value;
                    next.OwnsTimers = true; next.Timers = data.Timers; state.Coordinator = next;
                    data.OwnsTimers = false; data.Timers = new List<TimeStopPausedTimer01438>();
                }
                return;
            }
            // The normal clock remains the native turn start during TB buff
            // processing, including StartTurn's pre-Prepare tick. During scene
            // teardown the persisted player clock can outlive TimeController.
            TimeSpan now;
            var game = Game.Instance;
            if (game?.TimeController != null) now = Now;
            else if (game?.State?.PlayerState != null) now = game.State.PlayerState.GameTime;
            else now = state.Timers.Count == 0 ? TimeSpan.Zero : state.Timers.Values.Max(t => t.LastSettled);
            Paused.Remove(owner); // Native queue reads below must see thaw.
            foreach (var pair in state.Timers) if (Ordinary(pair.Key)) Settle(pair.Key, pair.Value, now);
            if (state.Coordinator != null) { state.Coordinator.OwnsTimers = false; state.Coordinator.Timers = new List<TimeStopPausedTimer01438>(); }
            data.OwnsTimers = false; data.Timers = new List<TimeStopPausedTimer01438>();
            owner.Buffs.UpdateNextEvent();
        }
        internal static void Added(Buff buff)
        {
            var owner = OwnerUnit(buff);
            if (owner == null || !Paused.TryGetValue(owner, out var state) || state.Markers.Count == 0) return;
            Add(state, buff, Now); owner.Buffs.UpdateNextEvent();
        }
        internal static void Removed(Buff buff)
        {
            var owner = OwnerUnit(buff);
            if (owner == null || !Paused.TryGetValue(owner, out var state) || !state.Timers.TryGetValue(buff, out var timer)) return;
            state.Timers.Remove(buff); state.Coordinator.Timers.Remove(timer);
        }
        internal static void BeforeRefresh(BuffCollection collection, Buff incoming)
        {
            var owner = collection?.Owner?.Unit;
            if (owner == null || incoming == null || TimeStopEngine01438.IsProtectedTimerBuff(incoming.Blueprint) || !Paused.TryGetValue(owner, out var state)) return;
            var existing = collection.GetBuff(incoming.Blueprint);
            if (existing != null && state.Timers.TryGetValue(existing, out var timer)) Settle(existing, timer, Now);
        }
    }
    [HarmonyPatch(typeof(Buff), nameof(Buff.NextEventTime), MethodType.Getter)]
    internal static class TimeStopPausedQueue01438Patch
    {
        static bool Prefix(Buff __instance, ref TimeSpan __result) { if (!TimeStopTimers01438.IsPaused(__instance)) return true; __result = TimeSpan.MaxValue; return false; }
    }
    [HarmonyPatch(typeof(Buff), nameof(Buff.TimeLeft), MethodType.Getter)]
    internal static class TimeStopPausedTimeLeft01438Patch
    {
        static bool Prefix(Buff __instance, ref TimeSpan __result)
        {
            if (!TimeStopTimers01438.TryTimeLeft(__instance, out var remaining)) return true;
            __result = remaining; return false;
        }
    }
    [HarmonyPatch(typeof(BuffCollection), nameof(BuffCollection.TickBuff))]
    internal static class TimeStopPausedTick01438Patch
    {
        static bool Prefix(Buff __0) => !TimeStopTimers01438.IsPaused(__0);
    }
    [HarmonyPatch(typeof(BuffCollection), "PrepareFactForAttach", new[] { typeof(Buff) })]
    internal static class TimeStopRefresh01438Patch
    {
        static void Prefix(BuffCollection __instance, Buff __0) => TimeStopTimers01438.BeforeRefresh(__instance, __0);
    }
    [HarmonyPatch(typeof(ContextActionApplyBuff), "CalculateDuration")]
    internal static class TimeStopTurnWindow01438Patch
    {
        static void Postfix(ContextActionApplyBuff __instance, MechanicsContext __0, ref TimeSpan? __result)
        {
            if (__result.HasValue && __result.Value < TimeSpan.MaxValue - TimeSpan.FromSeconds(6) &&
                CombatController.IsInTurnBasedCombat() && TimeStopEngine01438.IsOwnHost(__instance.Buff) &&
                __0?.SourceAbilityContext?.Ability?.ActionType == UnitCommand.CommandType.Standard)
                __result = __result.Value + TimeSpan.FromSeconds(6);
        }
    }
}
