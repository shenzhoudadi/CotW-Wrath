using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Controllers;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

// This replaces 037's area-membership omission with the original native scan
// lifecycle. Only candidate enumeration and unreachable non-AllArea geometry
// are scene endpoints; every target/state/enter/exit decision remains native.
internal static class TimeStopLifecycle01438Tests
{
    static readonly Type Engine = typeof(CallOfTheWild.Wrath.Prototype.ForestBlessing01435).Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopEngine01438", true);
    static readonly List<UnitEntityData> Candidates = new List<UnitEntityData>();
    static readonly HashSet<BlueprintAbilityAreaEffect> Observed = new HashSet<BlueprintAbilityAreaEffect>();
    static int checks, scans, enters, exits;
    static object Get(object o, string n) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).GetValue(o);
    static void Set(object o, string n, object v) => (AccessTools.Field(o.GetType(), n) ?? AccessTools.Field(o.GetType(), "<" + n + ">k__BackingField")).SetValue(o, v);
    static object Old(string name, params object[] args) => AccessTools.Method(typeof(TimeStutter01437Tests), name).Invoke(null, args);
    static object E(string name, params object[] args) => AccessTools.Method(Engine, name).Invoke(null, args);
    static T Asset<T>(BlueprintsCache c, string token) => (T)AccessTools.Method(typeof(TimeStutter01437Tests), "Asset").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { c, token });
    static void Check(bool ok, string what) { if (!ok) throw new Exception("TimeStopLifecycle01438: " + what); checks++; }
    static List<UnitEntityData> SceneCandidates(AreaEffectEntityData area) => Candidates;
    static bool No(ref bool __result) { __result = false; return false; }
    static void Enter(BlueprintAbilityAreaEffect __instance) { if (Observed.Contains(__instance)) enters++; }
    static void Exit(BlueprintAbilityAreaEffect __instance) { if (Observed.Contains(__instance)) exits++; }
    static IEnumerable<CodeInstruction> CandidateBoundary(IEnumerable<CodeInstruction> input)
    {
        var code = input.ToList();
        int end = code.FindIndex(i => i.operand is MethodInfo m && m.Name == "FindUnitsInShape");
        if (end < 2 || !(code[end - 1].operand is MethodInfo shape) || shape.Name != "get_Shape" || !(code[end - 2].operand is MethodInfo view) || view.Name != "get_View") throw new Exception("038 native shape candidate lookup changed");
        code.RemoveRange(end - 2, 3); code.Insert(end - 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TimeStopLifecycle01438Tests), nameof(SceneCandidates)))); return code;
    }
    static IEnumerable<CodeInstruction> AllAreaSpatialBoundary(IEnumerable<CodeInstruction> input)
    {
        var code = input.ToList(); int field = code.FindIndex(i => i.operand is FieldInfo f && f.Name == "Shape" && f.DeclaringType == typeof(BlueprintAbilityAreaEffect));
        if (field < 0 || code[field + 1].opcode != OpCodes.Ldc_I4_3 || code[field + 2].opcode != OpCodes.Bne_Un_S || code[field + 3].opcode != OpCodes.Ldc_I4_1 || code[field + 4].opcode != OpCodes.Ret || (int)AreaEffectShape.AllArea != 3) throw new Exception("038 native AllArea branch changed");
        int tail = field + 5; var zero = new CodeInstruction(OpCodes.Ldc_I4_0); zero.labels.AddRange(code[tail].labels); code.RemoveRange(tail, code.Count - tail); code.Add(zero); code.Add(new CodeInstruction(OpCodes.Ret)); return code;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool SceneShould(AreaEffectEntityData area, UnitEntityData unit)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code) => AllAreaSpatialBoundary(code);
        _ = (Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>>)Transpiler;
        throw new NotImplementedException("Native reverse patch must replace this stand-in");
    }
    static IEnumerable<CodeInstruction> ShouldCallBoundary(IEnumerable<CodeInstruction> input)
    {
        var code = input.ToList(); int count = 0;
        foreach (var i in code) if (i.operand is MethodInfo m && m.DeclaringType == typeof(AreaEffectEntityData) && m.Name == "ShouldUnitBeInside") { i.opcode = OpCodes.Call; i.operand = AccessTools.Method(typeof(TimeStopLifecycle01438Tests), nameof(SceneShould)); count++; }
        if (count != 1) throw new Exception("038 native UpdateUnit filter changed"); return code;
    }
    static UnitEntityData Unit(BlueprintsCache cache, string name, BlueprintFaction faction, UnitGroup group, int index)
    {
        var unit = (UnitEntityData)Old("Unit", cache, "038-" + name, 20, 0);
        Set(unit, "m_Group", group); Set(unit, "m_GroupId", group.Id); Set(unit.Descriptor, "m_Faction", faction);
        ((MultiSet<BlueprintFaction>)Get(group, "m_Factions")).Add(faction);
        Set(unit, "m_Position", new UnityEngine.Vector3(index * 4, 0, 0)); return unit;
    }
    static Buff Cast(BlueprintsCache cache, UnitEntityData owner)
    {
        var bp = Asset<BlueprintAbility>(cache, "TimeStutterAbility"); var data = new AbilityData(bp, owner.Descriptor);
        var context = new AbilityExecutionContext(data, new AbilityParams(), new TargetWrapper(owner), null, owner);
        AbilityExecutionProcess.PrepareCast(context);
        using (context.GetDataScope(owner)) bp.GetComponent<AbilityEffectRunAction>().Apply(context, new TargetWrapper(owner));
        return owner.Buffs.Enumerable.Single(b => b.Blueprint == Asset<BlueprintBuff>(cache, "TimeStutterHost"));
    }
    static AreaEffectEntityData Area(string label, Buff host, BlueprintAbilityAreaEffect bp)
    {
        var area = (AreaEffectEntityData)Old("MakeArea", label, host.Context, bp, Array.Empty<UnitEntityData>());
        Set(area, "m_CanAffectAllies", true); Old("BindArea", host, area); return area;
    }
    static int Count(UnitEntityData u, BlueprintBuff effect) => u.Buffs.Enumerable.Count(b => ReferenceEquals(b.Blueprint, effect));
    static void Scan(AreaEffectEntityData area) { AccessTools.Method(typeof(AreaEffectEntityData), "UpdateUnits").Invoke(area, null); scans++; }
    static void End(AreaEffectEntityData area)
    {
        Check(area.IsEnded, "native ForceEnd sets ended flag before end dispatch");
        AccessTools.Method(typeof(AreaEffectEntityData), "HandleEnd").Invoke(area, null);
        Check(area.DestroyMark, "actual native HandleEnd exits/clears and marks source area for destruction");
    }
    internal static int Run(BlueprintsCache cache, Harmony h)
    {
        checks = scans = enters = exits = 0; Candidates.Clear(); Observed.Clear();
        h.CreateClassProcessor(Engine.Assembly.GetType("CallOfTheWild.Wrath.Prototype.TimeStopAreaUntargetable01438Patch", true)).Patch();
        TimeStopTimers01438Tests.EnsurePatches(h);
        h.CreateReversePatcher(AccessTools.Method(typeof(AreaEffectEntityData), "ShouldUnitBeInside"), new HarmonyMethod(typeof(TimeStopLifecycle01438Tests), nameof(SceneShould))).Patch();
        h.Patch(AccessTools.Method(typeof(AreaEffectEntityData), "UpdateUnit"), transpiler: new HarmonyMethod(typeof(TimeStopLifecycle01438Tests), nameof(ShouldCallBoundary)));
        h.Patch(AccessTools.Method(typeof(AreaEffectEntityData), "UpdateUnits"), transpiler: new HarmonyMethod(typeof(TimeStopLifecycle01438Tests), nameof(CandidateBoundary)));
        h.Patch(AccessTools.PropertyGetter(typeof(EntityDataBase), "IsInFogOfWar"), prefix: new HarmonyMethod(typeof(TimeStopLifecycle01438Tests), nameof(No)));
        h.Patch(AccessTools.Method(typeof(BlueprintAbilityAreaEffect), "HandleUnitEnter"), prefix: new HarmonyMethod(typeof(TimeStopLifecycle01438Tests), nameof(Enter)));
        h.Patch(AccessTools.Method(typeof(BlueprintAbilityAreaEffect), "HandleUnitExit"), prefix: new HarmonyMethod(typeof(TimeStopLifecycle01438Tests), nameof(Exit)));
        var friendly = new BlueprintFaction { name = "038-friendly", AssetGuid = BlueprintGuid.Parse("03800000000000000000000000000001") };
        var hostile = new BlueprintFaction { name = "038-hostile", AssetGuid = BlueprintGuid.Parse("03800000000000000000000000000002") };
        var neutral = new BlueprintFaction { name = "038-neutral", AssetGuid = BlueprintGuid.Parse("03800000000000000000000000000003"), Neutral = true };
        var friends = new UnitGroup("038-friends"); var enemies = new UnitGroup("038-enemies"); var neutrals = new UnitGroup("038-neutrals");
        friends.AttackFactions.Add(hostile); enemies.AttackFactions.Add(friendly);
        var caster = Unit(cache, "caster", friendly, friends, 0); var ally = Unit(cache, "ally", friendly, friends, 1); var enemy = Unit(cache, "enemy", hostile, enemies, 2); var bystander = Unit(cache, "neutral", neutral, neutrals, 3);
        Check(!caster.IsEnemy(ally) && caster.IsEnemy(enemy) && bystander.Faction.Neutral, "actual native faction/group paths distinguish ally, enemy and neutral");
        var host = Cast(cache, caster); var bp = Asset<BlueprintAbilityAreaEffect>(cache, "TimeStutterArea"); var effect = Asset<BlueprintBuff>(cache, "TimeStutterEffect");
        var area = Area("038-first", host, bp); Observed.Add(bp); Candidates.AddRange(new[] { caster, ally, enemy, bystander });
        var trace = new JArray(); var watch = Stopwatch.StartNew();
        for (int tick = 0; tick < 1000; tick++) {
            Scan(area);
            if (tick < 8) trace.Add(new JObject { ["scan"] = tick, ["allyBuffs"] = Count(ally, effect), ["enemyBuffs"] = Count(enemy, effect), ["neutralBuffs"] = Count(bystander, effect), ["allyCanMove"] = !ally.State.HasCondition(UnitCondition.CantMove) });
        }
        watch.Stop();
        Check(enters == 4 && exits == 0 && Count(caster, effect) == 0 && new[] { ally, enemy, bystander }.All(u => Count(u, effect) == 1), "1000 real native scans apply once per eligible creature without membership churn");
        foreach (var u in new[] { ally, enemy, bystander }) {
            Check(u.State.HasCondition(UnitCondition.CantMove) && (int)E("OwnContributions", u) == 1 && u.State.Features.IsUntargetable.Count == 1, "actual marker/flag stay exact and freeze movement for " + u.UniqueId);
            Check(!UnitCommand.CommandTargetUntargetable(area, u) && UnitCommand.CommandTargetUntargetable(caster, u), "own area ignores only its own retained flag; unit targeting stays blocked");
        }
        Check(!(bool)E("RegisterGraph", cache, bp, effect) && !UnitCommand.CommandTargetUntargetable(area, ally), "repeat graph registration returns no new rollback receipt and preserves healthy exception");
        var allyFreeze = ally.Buffs.Enumerable.Single(b => b.Blueprint == effect);
        E("SetContribution", allyFreeze, false);
        Check((int)E("OwnContributions", ally) == 0 && ally.State.Features.IsUntargetable.Count == 1, "post-load fixture drops only transient ledger, preserving actual native retained flag");
        foreach (var runtime in allyFreeze.Components.Where(c => c.SourceBlueprintComponent.GetType().FullName == "CallOfTheWild.Wrath.Prototype.TimeStopFreezeMarker01438"))
            using (runtime.RequestEventContext()) {
                AccessTools.Method(runtime.SourceBlueprintComponent.GetType(), "OnApplyPostLoadFixes").Invoke(runtime.SourceBlueprintComponent, null);
                AccessTools.Method(runtime.SourceBlueprintComponent.GetType(), "OnApplyPostLoadFixes").Invoke(runtime.SourceBlueprintComponent, null);
            }
        Check((int)E("OwnContributions", ally) == 1 && (int)E("OwnContributions", enemy) == 1 && ally.State.Features.IsUntargetable.Count == 1 && !UnitCommand.CommandTargetUntargetable(area, ally), "actual marker post-load hook rebuilds once without changing native flags or another unit ledger");
        var foreignBp = (BlueprintAbilityAreaEffect)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(bp, null);
        var foreignArea = Area("038-foreign-same-guid", host, foreignBp);
        Check(UnitCommand.CommandTargetUntargetable(foreignArea, ally), "foreign same-GUID area object does not acquire engine exception"); Old("BindArea", host, area);
        bool rejected=false;
        try { E("RegisterGraph",cache,foreignBp,effect); } catch(Exception fault) { while(fault is TargetInvocationException) fault=fault.InnerException; rejected=fault is InvalidOperationException; }
        Check(rejected && !UnitCommand.CommandTargetUntargetable(area,ally) && !(bool)E("RegisterGraph",cache,bp,effect), "foreign same-GUID registration fails without replacing healthy prior graph receipt");

        var foreignBuffBlueprint = new BlueprintBuff { name = "038-other-mod-untargetable", AssetGuid = BlueprintGuid.Parse("03800000000000000000000000000004"), ComponentsArray = new BlueprintComponent[] { new Kingmaker.UnitLogic.FactLogic.Untargetable() } }; foreignBuffBlueprint.OnEnable();
        var foreignBuff = new Buff(foreignBuffBlueprint, new MechanicsContext(ally, ally.Descriptor, foreignBuffBlueprint), null); ally.Facts.Add(foreignBuff);
        Check(ally.State.Features.IsUntargetable.Count == 2 && UnitCommand.CommandTargetUntargetable(area, ally), "real external Untargetable contribution still blocks own area");
        Scan(area); Check(Count(ally, effect) == 0 && (int)E("OwnContributions", ally) == 0 && ally.State.Features.IsUntargetable.Count == 1 && foreignBuff.IsTurnedOn, "native exit removes only own freeze and never releases external retained flag");
        ally.Facts.Remove(foreignBuff); Scan(area); Check(Count(ally, effect) == 1 && (int)E("OwnContributions", ally) == 1, "removing foreign condition allows actual native re-entry exactly once");
        var personal = enemy.Descriptor.Ensure<UnitPartPersonalEnemy>(); Set(personal, "Enemy", (UnitReference)ally);
        Check(UnitCommand.CommandTargetUntargetable(area, enemy), "native personal-enemy branch after flag conversion remains authoritative"); Set(personal, "Enemy", default(UnitReference));
        Candidates.Remove(ally); Scan(area); Check(Count(ally, effect) == 0 && !ally.State.HasCondition(UnitCondition.CantMove), "actual candidate exit removes own source effect");
        Candidates.Add(ally); Scan(area); Check(Count(ally, effect) == 1, "actual candidate re-entry reapplies own source once");
        var host2 = Cast(cache, ally); var area2 = Area("038-second", host2, bp); Scan(area2); Scan(area);
        Check(Count(enemy, effect) == 2 && enemy.State.Features.IsUntargetable.Count == 2 && (int)E("OwnContributions", enemy) == 2 && !UnitCommand.CommandTargetUntargetable(area, enemy), "two own sources remain stable with exact combined contributions");
        caster.Facts.Remove(host); End(area); Check(Count(enemy, effect) == 1 && (int)E("OwnContributions", enemy) == 1, "native early first-source removal leaves independent source intact");
        ally.Facts.Remove(host2); End(area2); Check(new[] { caster, ally, enemy, bystander }.All(u => Count(u, effect) == 0 && (int)E("OwnContributions", u) == 0 && u.State.Features.IsUntargetable.Count == 0), "last native source exit clears own ledger and flags independently on every unit");
        PsychicExpansionTests.Inventory["timeStopLifecycle01438Contracts"] = new JObject {
            ["passed"] = true, ["assertions"] = checks, ["nativeUpdateUnitsCalls"] = scans, ["nativeEnterCalls"] = enters, ["nativeExitCalls"] = exits,
            ["stableScanCount"] = 1000, ["stableScanMilliseconds"] = watch.Elapsed.TotalMilliseconds, ["firstEightScans"] = trace,
            ["membershipHarmonyTargetCount"] = 1, ["sharedEngineHarmonyTargetCount"] = 6, ["newTypeIdCount"] = 1,
            ["boundary"] = "Original native UpdateUnits/UpdateUnit, candidate exit, native faction groups, CommandTargetUntargetable and AbilityAreaEffectBuff enter/exit execute. ShouldUnitBeInside uses the native reverse-copy with all filters and AllArea return intact; only its unreachable non-AllArea Unity spatial tail is excluded because CLR cannot JIT Unity ECalls. Candidate enumeration, fog visibility and existing 037 scene/rule/UI endpoints are controlled. HandleEnd cleanup is actual; full scene frame timing, visuals and save/load remain game observations."
        };
        Candidates.Clear(); Observed.Clear(); Console.WriteLine("PASS: " + checks + " TimeStopLifecycle01438 checks; " + scans + " native scans."); return checks;
    }
}
