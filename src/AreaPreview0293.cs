// COTW-0293 - area-effect abilities should show which units will be affected.
//
// Why the game does not do this by itself
// ---------------------------------------
// The affected-unit marker is drawn by Kingmaker.UI.Selection.UIDecal. Every
// frame it asks the casting ability WouldCurrentlyTarget(pos, unit), and that
// answer comes from one of two branches inside AbilityData:
//
//   * AbilityDeliverProjectile.WouldTargetUnit, when the spell carries a
//     projectile of the matching type;
//   * IAbilityAoERadiusProvider.WouldTargetUnit, when the blueprint carries an
//     AoE radius component - and the only native implementation,
//     AbilityAoERadius, is a plain circle test.
//
// A spell that only spawns an area effect has neither. BladeBarrier, the four
// ElementalWall variants, the Angel wall, the twenty kineticist walls, and this
// port's own Flashfire / Path of Glory all carry only
// AbilityEffectRunAction -> ContextActionSpawnAreaEffect, so both branches are
// skipped and WouldCurrentlyTarget answers false for every unit. The marker is
// also only raised by an active AbilityRange visual, and Wrath ships circle,
// line, cone and grid visuals but no wall visual, so HandleAoEMove never fires
// either. Two gaps, one symptom: nothing is highlighted while such a spell is
// being aimed.
//
// What this file does
// -------------------
//   * recognises a spawn-area ability by walking its component graph for a
//     ContextActionSpawnAreaEffect whose area is a Wall or Cylinder, cached per
//     blueprint. Shape decides, not the spell name: Flashfire is a 30ft wall
//     even though nothing in its name says so.
//   * answers WouldCurrentlyTarget with the same geometry the real area uses.
//     AreaEffectView.InitShape builds
//         Wall     -> ScriptZoneBox.Bounds = (Size, 20ft, 5ft)
//         Cylinder -> ScriptZoneCylinder.Radius = Size
//     centred on the clicked point and yawed to LookRotation(point - caster),
//     so the preview cannot drift from the actual effect.
//   * supplies the missing range visual by instantiating the area's own Fx
//     prefab - the exact thing the player will see when the spell lands -
//     and raises HandleAoEMove so UIDecal starts drawing markers.
//
// An ability that already has an AoE radius provider is left completely alone:
// the native circle already answers and already raises the marker, so adding a
// second visual there would only double it.
//
// No blueprint is modified. No component is added to native content and
// BlueprintAbility.AoERadius stays 0, so tooltips, target validity and the
// native AoE circle all behave exactly as before.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Blueprints;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.AbilityTarget;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects.SriptZones;
using UnityEngine;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class AreaPreview0293
    {
        // Mirrors AreaEffectView.InitShape for AreaEffectShape.Wall. The wall is
        // 5ft thick along the cast direction and 20ft tall; only Size (the
        // length across the cast direction) comes from the blueprint.
        internal static readonly float ThicknessMeters = 5.Feet().Meters;
        internal static readonly float HeightMeters = 20.Feet().Meters;

        // A null value marks "already inspected, spawns no usable area", so the
        // component walk happens once per ability instead of once per unit per
        // frame.
        private static readonly Dictionary<BlueprintGuid, BlueprintAbilityAreaEffect> Areas =
            new Dictionary<BlueprintGuid, BlueprintAbilityAreaEffect>();
        private static readonly HashSet<string> Reported = new HashSet<string>();

        private static ScriptZoneBox probe;
        private static GameObject holder;
        internal static Action<string> Log = message => { };

        internal static void Install(Action<string> log)
        {
            if (log != null) Log = log;
            EnsureHolder();
        }

        internal static void Report(Exception exception) => ReportOnce(exception?.ToString() ?? "unknown");
        internal static void Report(string message) => ReportOnce(message);

        private static void ReportOnce(string message)
        {
            try
            {
                if (message == null || !Reported.Add(message)) return;
                Log("[CotW-Area] " + message);
            }
            catch (Exception) { }
        }

        // --- area recognition -------------------------------------------------

        internal static bool TryArea(BlueprintAbility ability, out BlueprintAbilityAreaEffect area)
        {
            area = null;
            if (ability == null) return false;
            if (Areas.TryGetValue(ability.AssetGuid, out area)) return area != null;
            area = Inspect(ability);
            Areas[ability.AssetGuid] = area;
            return area != null;
        }

        private static BlueprintAbilityAreaEffect Inspect(BlueprintAbility ability)
        {
            try
            {
                // A provider means the native circle already answers and already
                // raises the marker; this file must not add a second visual.
                if (ability.GetAoERadiusProvider(false) != null) return null;
                // Same graph walk the rest of this port uses: it reaches a spawn
                // action nested inside Conditional or ConditionalSaved without a
                // hand-written recursion.
                foreach (var spawn in PsychicRemainingDisciplines01458
                    .Graph(ability.ComponentsArray).OfType<ContextActionSpawnAreaEffect>())
                {
                    var area = spawn.AreaEffect;
                    if (area == null) continue;
                    if (area.Shape != AreaEffectShape.Wall && area.Shape != AreaEffectShape.Cylinder) continue;
                    if (area.Size.Meters <= 0f) continue;
                    return area;
                }
            }
            catch (Exception exception) { Report(exception); }
            return null;
        }

        // --- containment ------------------------------------------------------

        internal static bool Contains(AbilityData ability, Vector3 targetPos, UnitEntityData unit, BlueprintAbilityAreaEffect area)
        {
            if (ability == null || unit == null || area == null) return false;
            var caster = ability.Caster?.Unit;
            if (caster == null) return false;
            // WouldCurrentlyTarget gates on ValidateUnit before reaching the
            // provider branch; the parts that matter for a preview are these.
            if (unit.Descriptor == null || unit.Descriptor.State.IsDead || !unit.IsInGame) return false;
            if (area.Shape == AreaEffectShape.Cylinder)
                return GameHelper.IsUnitInRange(unit, targetPos, area.Size.Meters, false);
            var box = Probe();
            if (box == null) return false;
            box.Bounds = new Bounds(Vector3.zero, new Vector3(area.Size.Meters, HeightMeters, ThicknessMeters));
            var direction = targetPos - caster.Position;
            direction.y = 0f;
            var transform = box.transform;
            transform.position = targetPos;
            transform.rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction)
                : Quaternion.identity;
            return box.Contains(unit.Position, unit.Corpulence);
        }

        private static ScriptZoneBox Probe()
        {
            if (probe != null) return probe;
            try
            {
                var instance = new GameObject("CotWAreaProbe0293");
                UnityEngine.Object.DontDestroyOnLoad(instance);
                probe = instance.AddComponent<ScriptZoneBox>();
            }
            catch (Exception exception) { Report("probe: " + exception); }
            return probe;
        }

        // --- range visual -----------------------------------------------------

        internal static void EnsureHolder()
        {
            if (holder != null) return;
            try
            {
                holder = new GameObject("CotWAreaPreview0293");
                UnityEngine.Object.DontDestroyOnLoad(holder);
                holder.AddComponent<AreaRangeVisual0293>();
                Log("[CotW-Area] range visual installed.");
            }
            catch (Exception exception)
            {
                holder = null;
                Report("install: " + exception);
            }
        }
    }

    // Port of CotW's AbilityWallRange, widened to every spawned area. Wrath
    // keeps all three hooks it used (CanEnable, SetFirstSpecs and
    // SetRangeToWorldPosition are still protected virtual).
    //
    // The ground indicator is the area's own Fx prefab rather than a cloned
    // decal. Cloning AbilityLineRange.Range produced a GameObject that never
    // rendered: the line visual is a screen-space decal whose projection is
    // driven by the UI range component, and a detached copy has none of that
    // state. The area Fx is authored to be spawned exactly where
    // AreaEffectView puts it, so it needs no scaling at all.
    internal sealed class AreaRangeVisual0293 : Kingmaker.UI.AbilityTarget.AbilityRange
    {
        private GameObject visual;
        private BlueprintAbilityAreaEffect visualArea;

        private void Awake()
        {
            // AbilityRange toggles Range directly in HandleAbilityShowSelection
            // without a null check, so a container must exist before OnEnable.
            Range = new GameObject("CotWAreaRange0293");
            Range.transform.SetParent(transform, false);
            Range.SetActive(false);
        }

        // Runs for every spell the player selects, so it must never throw:
        // an exception here would break target selection for all abilities.
        protected override bool CanEnable()
        {
            try
            {
                if (TacticalCombatHelper.IsActive) return false;
                if (!base.CanEnable()) return false;
                BlueprintAbilityAreaEffect area;
                return AreaPreview0293.TryArea(Ability.Blueprint, out area);
            }
            catch (Exception exception) { AreaPreview0293.Report(exception); return false; }
        }

        protected override void SetFirstSpecs()
        {
            try
            {
                BlueprintAbilityAreaEffect area;
                if (!AreaPreview0293.TryArea(Ability.Blueprint, out area)) return;
                EnsureVisual(area);
            }
            catch (Exception exception) { AreaPreview0293.Report(exception); }
        }

        protected override void SetRangeToWorldPosition()
        {
            try
            {
                if (Ability == null || Range == null) return;
                BlueprintAbilityAreaEffect area;
                if (!AreaPreview0293.TryArea(Ability.Blueprint, out area)) return;
                var caster = Ability.Caster?.Unit;
                if (caster == null) return;
                var clicks = Game.Instance.ClickEventsController;
                var target = Game.Instance.SelectedAbilityHandler.GetTarget(clicks.PointerOn, clicks.WorldPosition, Ability);
                var point = target != null ? target.Point : clicks.WorldPosition;
                var direction = point - caster.Position;
                direction.y = 0f;
                // AreaEffectView puts the area at the clicked point and yaws it
                // to LookRotation(point - caster); the Fx prefab is authored for
                // that transform, so it is reused unchanged.
                var yaw = direction.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(direction).eulerAngles.y
                    : 0f;
                Range.transform.position = point;
                Range.transform.eulerAngles = new Vector3(0f, yaw, 0f);
                // The marker is the feature; the ground visual is decoration, so
                // the marker is raised even when no Fx could be created.
                EventBus.RaiseEvent<IShowAoEAffectedUIHandler>(handler =>
                    handler.HandleAoEMove(point, Ability, IShowAoEAffectedUIHandler.AoeType.Any));
            }
            catch (Exception exception) { AreaPreview0293.Report(exception); }
        }

        // Runs once per activation. Creating the visual from the per-frame
        // position update instead would rebuild it every frame whenever the Fx
        // is missing or destroys itself.
        private void EnsureVisual(BlueprintAbilityAreaEffect area)
        {
            if (visualArea == area) return;
            Clear();
            visualArea = area;
            var fx = area.Fx;
            if (fx == null || string.IsNullOrEmpty(fx.AssetId))
            {
                AreaPreview0293.Report("area " + area.name + " has no Fx; marker only.");
                return;
            }
            try
            {
                visual = fx.CreateGameObjectFromLink(Range.transform);
                if (visual == null)
                {
                    AreaPreview0293.Report("area " + area.name + " Fx " + fx.AssetId + " did not load; marker only.");
                    return;
                }
                visual.name = "CotWAreaFx0293";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                AreaPreview0293.Report("area " + area.name + " shape=" + area.Shape + " size=" +
                    area.Size.Value.ToString("0.#") + " fx=" + fx.AssetId +
                    " renderers=" + visual.GetComponentsInChildren<Renderer>(true).Length +
                    " particles=" + visual.GetComponentsInChildren<ParticleSystem>(true).Length);
            }
            catch (Exception exception)
            {
                visual = null;
                AreaPreview0293.Report("fx " + fx.AssetId + ": " + exception);
            }
        }

        private void Clear()
        {
            if (visual != null) Destroy(visual);
            visual = null;
            visualArea = null;
        }
    }

    // WouldCurrentlyTarget already refused every unit, because neither native
    // branch applies to a spawned area. Only a false answer is replaced, so
    // projectile and radius abilities keep their native behaviour untouched.
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.WouldCurrentlyTarget))]
    internal static class AreaWouldCurrentlyTarget0293Patch
    {
        private static void Postfix(AbilityData __instance, Vector3 __0, UnitEntityData __1, ref bool __result)
        {
            if (__result || __instance == null || __1 == null) return;
            try
            {
                BlueprintAbilityAreaEffect area;
                if (!AreaPreview0293.TryArea(__instance.Blueprint, out area)) return;
                __result = AreaPreview0293.Contains(__instance, __0, __1, area);
            }
            catch (Exception exception) { AreaPreview0293.Report(exception); }
        }
    }

    // Kept as a guard only: TryArea refuses any ability that already has a
    // provider, so a circle can only appear here if another mod adds one to an
    // area spell after the answer was cached. Traverse is used instead of
    // ___Ability on purpose - a failed field injection would throw while
    // patches are being installed, which aborts mod loading.
    [HarmonyPatch(typeof(AbilityAoERange), "CanEnable")]
    internal static class AreaSuppressCircle0293Patch
    {
        private static void Postfix(AbilityAoERange __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            try
            {
                var ability = Traverse.Create(__instance).Field("Ability").GetValue<AbilityData>();
                if (ability == null) return;
                BlueprintAbilityAreaEffect area;
                if (AreaPreview0293.TryArea(ability.Blueprint, out area)) __result = false;
            }
            catch (Exception exception) { AreaPreview0293.Report(exception); }
        }
    }
}
