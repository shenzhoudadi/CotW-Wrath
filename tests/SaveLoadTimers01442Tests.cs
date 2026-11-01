using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

// This reproduces the native load ordering missing from the 038 timer tests:
// Facts.RestoreOwnerLink -> Buffs.UpdateNextEvent -> Descriptor.PostLoad.
// A non-null UnitFact.Owner descriptor can still convert to a null unit here.
internal static class SaveLoadTimers01442Tests
{
    static readonly Type Timers = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopTimers01438", true);
    static readonly Type Engine = Timers.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopEngine01438", true);
    static readonly Type State = Timers.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopFreezeState01438", true);
    static int checks, restoredFacts, nativeQueueRebuilds, nativeUnitPostLoads, descriptorBindings, serial;
    static UnitEntityData loadingUnit;
    static bool queueWasReadBeforeBinding;
    static object Get(object o, string name) => (AccessTools.Field(o.GetType(), name) ?? AccessTools.Field(o.GetType(), "<" + name + ">k__BackingField")).GetValue(o);
    static void Set(object o, string name, object value) => (AccessTools.Field(o.GetType(), name) ?? AccessTools.Field(o.GetType(), "<" + name + ">k__BackingField")).SetValue(o, value);
    static object T(string name, params object[] args) => AccessTools.Method(Timers, name).Invoke(null, args);
    static object E(string name, params object[] args) => AccessTools.Method(Engine, name).Invoke(null, args);
    static object Old(string name, params object[] args) => AccessTools.Method(typeof(TimeStopTimers01438Tests), name).Invoke(null, args);
    static void Check(bool ok, string message) { if (!ok) throw new Exception("SaveLoadTimers01442: " + message); checks++; }
    static TimeSpan Now { get => (TimeSpan)AccessTools.Field(typeof(TimeStutter01437Tests), "now").GetValue(null); set => AccessTools.Field(typeof(TimeStutter01437Tests), "now").SetValue(null, value); }
    static TimeSpan Next(Buff buff) => (TimeSpan)AccessTools.Property(typeof(Buff), "NextTickTime").GetValue(buff, null);
    static UnitEntityData Unit(BlueprintsCache cache, string label)
    {
        var unit = (UnitEntityData)Old("Unit", cache, "load01442-" + label);
        if (unit.Descriptor.Brain == null) {
            var brain = new UnitBrain(); brain.PostLoad(unit.Descriptor); Set(unit.Descriptor, "Brain", brain);
        }
        return unit;
    }
    static BlueprintBuff Blueprint(string label)
    {
        var bp = new BlueprintBuff { name = "load01442-" + label, AssetGuid = BlueprintGuid.Parse(Guid.NewGuid().ToString("N")), Stacking = StackingType.Stack, ComponentsArray = Array.Empty<BlueprintComponent>() };
        bp.OnEnable(); return bp;
    }
    static Buff JsonFact(string label, TimeSpan? end, TimeSpan tick)
    {
        var settings = new JsonSerializerSettings { ContractResolver = new OptInContractResolver() };
        var input = new JObject { ["UniqueId"] = "load01442-fact-" + (++serial), ["NextTickTime"] = JToken.FromObject(tick), ["TickTime"] = JToken.FromObject(TimeSpan.FromSeconds(6)) };
        if (end.HasValue) input["m_EndTime"] = JToken.FromObject(end.Value);
        // The game's actual OptIn resolver selects Buff(JsonConstructorMark),
        // and restores its private timer fields. Blueprint asset resolution,
        // context and component dispatch are separate controlled endpoints here.
        var first = JsonConvert.DeserializeObject<Buff>(input.ToString(), settings);
        var saved = JObject.FromObject(first, JsonSerializer.Create(settings));
        foreach (var name in new[] { "Blueprint", "Components", "m_Context" }) saved.Remove(name);
        var restored = JsonConvert.DeserializeObject<Buff>(saved.ToString(), settings);
        Set(restored, "Blueprint", Blueprint(label));
        Check(restored.Manager == null && restored.Owner == null && restored.UniqueId == (string)input["UniqueId"], "actual native JSON constructor restores a detached fact and stable identity");
        Check(restored.EndTime == (end ?? TimeSpan.MaxValue) && Next(restored) == tick, "actual native JSON roundtrip retains finite/permanent and next-tick fields");
        restoredFacts++; return restored;
    }
    static void ObserveQueue(Buff __instance)
    {
        if (loadingUnit != null && ReferenceEquals(((EntityFact)__instance).Owner, loadingUnit) && loadingUnit.Descriptor.Unit == null) queueWasReadBeforeBinding = true;
    }
    static bool BindDescriptor(UnitDescriptor __instance, UnitEntityData __0)
    {
        if (loadingUnit == null || !ReferenceEquals(__instance, loadingUnit.Descriptor)) return true;
        Check(ReferenceEquals(__0, loadingUnit) && __instance.Unit == null && queueWasReadBeforeBinding, "real native UnitEntityData.OnPostLoad reaches descriptor binding after the unbound queue read");
        Set(__instance, "Unit", __0); descriptorBindings++; return false;
    }
    static void Rebuild(UnitEntityData owner)
    {
        owner.Buffs.UpdateNextEvent(); nativeQueueRebuilds++;
    }
    static void QueueBeforeDescriptorBinding(BlueprintsCache cache, Harmony h)
    {
        var unit = Unit(cache, "native-order");
        Check(unit.Facts.List.Count == 0, "isolated load fixture starts with no unrelated facts");
        var finite = JsonFact("finite", Now + TimeSpan.FromSeconds(20), Now + TimeSpan.FromSeconds(6));
        var earlier = JsonFact("earlier", Now + TimeSpan.FromSeconds(3), Now + TimeSpan.FromSeconds(6));
        var permanent = JsonFact("permanent", null, TimeSpan.MaxValue);
        foreach (var fact in new[] { finite, earlier, permanent }) unit.Facts.List.Add(fact);
        Set(unit.Facts, "Owner", null);
        unit.Facts.RestoreOwnerLink(unit);
        Set(unit.Descriptor, "Unit", null);
        Check(ReferenceEquals(finite.Owner, unit.Descriptor) && finite.Owner.Unit == null && ReferenceEquals(((EntityFact)finite).Owner, unit), "native RestoreOwnerLink restores manager/entity before descriptor/unit back-reference");

        // Keep this call first: the unchanged 041 DLL must fail here with the
        // user's exact ConditionalWeakTable null-key exception.
        Rebuild(unit);
        Check(ReferenceEquals(Get(unit.Buffs, "m_NextEvent"), earlier), "actual native queue selects earliest event while descriptor unit is unbound");
        Check(finite.NextEventTime == Next(finite) && permanent.NextEventTime == TimeSpan.MaxValue, "unbound getter observers preserve genuine native timer values");
        var beforeEnd = finite.EndTime; var beforeTick = Next(finite);
        var descriptorMethod = AccessTools.Method(typeof(UnitDescriptor), nameof(UnitDescriptor.PostLoad));
        var queueGetter = AccessTools.PropertyGetter(typeof(Buff), nameof(Buff.NextEventTime));
        h.Patch(descriptorMethod, prefix: new HarmonyMethod(typeof(SaveLoadTimers01442Tests), nameof(BindDescriptor)));
        h.Patch(queueGetter, prefix: new HarmonyMethod(typeof(SaveLoadTimers01442Tests), nameof(ObserveQueue)) { priority = Priority.First });
        loadingUnit = unit; queueWasReadBeforeBinding = false;
        try {
            AccessTools.Method(typeof(UnitEntityData), "OnPostLoad").Invoke(unit, null); nativeUnitPostLoads++;
            Check(queueWasReadBeforeBinding && descriptorBindings == 1 && ReferenceEquals(unit.Descriptor.Unit, unit), "unmodified native unit post-load body completes after descriptor binding endpoint");
            Check(finite.EndTime == beforeEnd && Next(finite) == beforeTick && ReferenceEquals(Get(unit.Buffs, "m_NextEvent"), earlier), "native post-load queue neither edits timer fields nor loses the earliest event");
            Check(unit.CombatState != null && unit.Commands != null, "native post-load continues through actual combat-state and command reconstruction");
        } finally {
            loadingUnit = null;
            h.Unpatch(descriptorMethod, AccessTools.Method(typeof(SaveLoadTimers01442Tests), nameof(BindDescriptor)));
            h.Unpatch(queueGetter, AccessTools.Method(typeof(SaveLoadTimers01442Tests), nameof(ObserveQueue)));
            Set(unit.Descriptor, "Unit", unit);
        }
        Rebuild(unit);
        Check(!(bool)T("IsPaused", finite) && ReferenceEquals(Get(unit.Buffs, "m_NextEvent"), earlier), "bound nonfrozen unit retains ordinary native queue behavior");
        // These JSON timer facts deliberately have no context or runtime
        // components; removing them from the manager is fixture teardown only.
        foreach (var fact in new[] { finite, earlier, permanent }) unit.Facts.List.Remove(fact);
    }
    static void EveryUnboundEntryAndResume(BlueprintsCache cache)
    {
        var caster = Unit(cache, "freeze-caster"); var target = Unit(cache, "freeze-target");
        var ordinary = (Buff)Old("Add", target, Blueprint("frozen-ordinary"), (double?)30);
        AccessTools.Property(typeof(Buff), "NextTickTime").SetValue(ordinary, Now + TimeSpan.FromSeconds(6), null);
        var source = (Tuple<Buff, AreaEffectEntityData>)Old("Freeze", cache, caster, target, false, (Kingmaker.UnitLogic.Abilities.Metamagic)0);
        var marker = target.Buffs.Enumerable.Single(b => b.Blueprint.GetComponents<CallOfTheWild.Wrath.Prototype.TimeStopFreezeMarker01438>().Any());
        var runtime = marker.Components.Single(c => c.SourceBlueprintComponent is CallOfTheWild.Wrath.Prototype.TimeStopFreezeMarker01438);
        var data = runtime.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(m => m.Name == "GetData" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(State).Invoke(runtime, null);
        var beforeData = JsonConvert.SerializeObject(data); var end = ordinary.EndTime; var tick = Next(ordinary); var start = Now;
        var contributions = (int)E("OwnContributions", target);
        Check((bool)T("IsPaused", ordinary) && ordinary.NextEventTime == TimeSpan.MaxValue && contributions == 1, "normal attached frozen facts start paused through actual native queue patch");
        Set(target.Descriptor, "Unit", null);
        try {
            Check(!(bool)T("IsPaused", ordinary), "unbound descriptor never enters the weak-table lookup");
            var remaining = new object[] { ordinary, TimeSpan.FromSeconds(123) };
            Check(!(bool)AccessTools.Method(Timers, "TryTimeLeft").Invoke(null, remaining) && (TimeSpan)remaining[1] == TimeSpan.Zero, "unbound remaining-time lookup returns native fallback without weak-table access");
            T("Retain", marker, data); T("Release", marker, data); T("Added", ordinary); T("Removed", ordinary); T("BeforeRefresh", target.Buffs, ordinary);
            E("SetContribution", marker, true); E("SetContribution", marker, false);
            Rebuild(target);
            Check(ordinary.NextEventTime == (end < tick ? end : tick) && ordinary.EndTime == end && Next(ordinary) == tick, "every unbound timer entry leaves exact finite timer fields and queue semantics unchanged");
            Check(JsonConvert.SerializeObject(data) == beforeData && (int)E("OwnContributions", target) == contributions, "unbound entry attempts neither claim nor clear persisted timer/targeting receipts");
        } finally { Set(target.Descriptor, "Unit", target); }
        Rebuild(target);
        Check((bool)T("IsPaused", ordinary) && ordinary.NextEventTime == TimeSpan.MaxValue, "descriptor rebinding recovers the same live pause without duplicate retain or receipt loss");
        Now = start + TimeSpan.FromSeconds(2);
        Old("End", source);
        Check(!(bool)T("IsPaused", ordinary) && ordinary.EndTime == end + TimeSpan.FromSeconds(2) && Next(ordinary) == tick + TimeSpan.FromSeconds(2), "real source cleanup after rebind credits only the original two-second pause exactly once");
        var after = ordinary.EndTime; T("Release", marker, data);
        Check(ordinary.EndTime == after, "repeated source release cannot credit the interval twice");

        var detached = JsonFact("detached-entries", Now + TimeSpan.FromSeconds(20), TimeSpan.MaxValue);
        var emptyData = Activator.CreateInstance(State); var missingRemaining = new object[] { detached, TimeSpan.FromSeconds(1) };
        Check(!(bool)T("IsPaused", detached) && !(bool)AccessTools.Method(Timers, "TryTimeLeft").Invoke(null, missingRemaining), "completely detached facts also use native fallback");
        T("Retain", detached, emptyData); T("Release", detached, emptyData); T("Added", detached); T("Removed", detached); T("BeforeRefresh", null, detached);
        E("SetContribution", detached, true); E("SetContribution", detached, false);
        Check(detached.NextEventTime == detached.EndTime, "all detached entry paths preserve genuine native event time");
    }
    internal static int RunNative(BlueprintsCache cache, Harmony harmony)
    {
        checks = restoredFacts = nativeQueueRebuilds = nativeUnitPostLoads = descriptorBindings = serial = 0;
        TimeStopTimers01438Tests.EnsurePatches(harmony); var prior = Now;
        try { QueueBeforeDescriptorBinding(cache, harmony); EveryUnboundEntryAndResume(cache); }
        finally { Now = prior; loadingUnit = null; }
        PsychicExpansionTests.Inventory["saveLoadTimers01442Contracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["nativeJsonRestoredBuffs"] = restoredFacts, ["nativeQueueRebuilds"] = nativeQueueRebuilds,
            ["nativeUnitOnPostLoadBodies"] = nativeUnitPostLoads, ["descriptorBindingEndpoints"] = descriptorBindings,
            ["descriptorWithoutUnit"] = true, ["allTimerEntrypointsGuarded"] = true, ["nativeQueueBeforeDescriptorBinding"] = true,
            ["frozenReceiptPreservedAcrossBinding"] = true, ["singleElapsedCreditAfterRebind"] = true,
            ["boundary"] = "Actual game OptInContractResolver and Buff(JsonConstructorMark) roundtrip native private timer/identity fields; blueprint asset, context and runtime-component restoration are controlled endpoints. Actual EntityFactsManager.RestoreOwnerLink, BuffCollection.UpdateNextEvent and unmodified UnitEntityData.OnPostLoad body execute in native order, including the queue read before Descriptor.Unit is assigned and subsequent combat-state/command reconstruction. Only UnitDescriptor.PostLoad's wider stats/body/brain/UI restoration is replaced for this exact fixture with its real Unit assignment; full save archive, Unity scene, portrait rendering and main-menu game process are not launched. Existing timer and area contracts remain responsible for actual runtime components, overlapping coordinators, post-load hooks, native tick/refresh/suppression and turn windows."
        };
        Console.WriteLine("PASS: " + checks + " SaveLoadTimers01442 checks; " + nativeQueueRebuilds + " native queue rebuilds, " + nativeUnitPostLoads + " native unit post-load bodies.");
        return checks;
    }
}
