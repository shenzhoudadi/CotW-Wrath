// Temporary diagnostics, 0.14.77.
//
// Reports the residual temporary ability lifecycle in Player.log. Every probe
// below hangs off a specific game event that fires once per occurrence:
//
//   * Polymorph.OnActivate / OnDeactivate(before) - how many facts the shape
//     component thinks it applied, so a deactivation with a stale applied list
//     is visible;
//   * UnitBody.CancelPolymorphEffect - whether a turn-back fact survived;
//   * UnitPartTouch.Init / OnDispose and TouchSpellsController - how a sticky
//     touch delivery is created, released and whether its fact survived.
//
// Earlier builds swept every party member from Game.Tick. That rebuilt the
// action bar once per second (icons flickered, the spellbook jumped back to
// level 0, tooltips stopped rendering), so the tick hook and the sweeping probe
// are deliberately gone. Do not add one back; attach a probe to a real event
// instead.
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Parts;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class TransientDiagnostics
    {
        private const string Prefix = "[CotW-Diag] ";
        private static bool patchesReported;
        internal const string TurnBackAbilityId = "a2cb181ee69860b46b82844a3a8569b8";

        internal static void Log(string message)
        {
            try { Console.WriteLine(Prefix + message); } catch (Exception) { }
        }

        internal static string Name(object blueprint)
        {
            var scriptable = blueprint as Kingmaker.Blueprints.BlueprintScriptableObject;
            if (scriptable != null) return scriptable.name + " <" + scriptable.AssetGuid + ">";
            return blueprint == null ? "<null>" : blueprint.ToString();
        }

        internal static string Short(object blueprint)
        {
            var scriptable = blueprint as Kingmaker.Blueprints.BlueprintScriptableObject;
            return scriptable == null ? "<null>" : scriptable.name;
        }

        internal static string FactName(object component) =>
            Name(Read(Read(component, "Fact"), "Blueprint"));

        internal static string Who(UnitEntityData unit) =>
            unit == null ? "<no unit>" : unit.CharacterName + "#" + unit.UniqueId;

        // Reflection keeps the probe compiling even when a member is internal
        // in the pinned game build.
        internal static object Read(object instance, string member)
        {
            if (instance == null) return null;
            try
            {
                var type = instance.GetType();
                var property = AccessTools.Property(type, member);
                if (property != null && property.CanRead) return property.GetValue(instance, null);
                var field = AccessTools.Field(type, member);
                return field?.GetValue(instance);
            }
            catch (Exception) { return null; }
        }

        internal static int Count(object collection)
        {
            if (collection == null) return -1;
            var list = collection as ICollection;
            if (list != null) return list.Count;
            var enumerable = collection as IEnumerable;
            if (enumerable == null) return -1;
            var count = 0;
            foreach (var unused in enumerable) count++;
            return count;
        }

        internal static string DescribeFacts(object facts)
        {
            var enumerable = facts as IEnumerable;
            if (enumerable == null) return "<none>";
            var parts = new List<string>();
            foreach (var fact in enumerable)
            {
                if (fact == null) { parts.Add("<null>"); continue; }
                parts.Add(Short(Read(fact, "Blueprint")) + "#" + Read(fact, "UniqueId"));
            }
            return parts.Count == 0 ? "<empty>" : string.Join(", ", parts.ToArray());
        }

        internal static string TurnBackFacts(UnitDescriptor owner)
        {
            var list = owner?.Facts?.List;
            if (list == null) return "<none>";
            var ids = list.OfType<Ability>()
                .Where(TransientAbilityCleanup.IsTurnBack)
                .Select(a => Short(a.Blueprint) + "#" + a.UniqueId +
                    "/slot=" + (a.Data != null && a.Data.HasActionBarSlot) +
                    "/disabled=" + Read(a.Data, "TemporarilyDisabled"))
                .ToArray();
            return ids.Length == 0 ? "<none>" : string.Join(", ", ids);
        }

        // One-shot: proves whether this build's cleanup postfixes are installed
        // and in which order Harmony runs the competing ones.
        private static void ReportPatches()
        {
            if (patchesReported) return;
            patchesReported = true;
            Describe(AccessTools.Method(typeof(UnitBody), nameof(UnitBody.CancelPolymorphEffect)));
            Describe(AccessTools.Method(typeof(TouchSpellsController), nameof(TouchSpellsController.OnAbilityEffectApplied)));
            Describe(AccessTools.Method(typeof(EntityFactsManager), nameof(EntityFactsManager.ApplyPostLoadFixes)));
        }

        private static void Describe(MethodInfo method)
        {
            try
            {
                if (method == null) { Log("patch info: <method not found>"); return; }
                var info = Harmony.GetPatchInfo(method);
                if (info == null) { Log("patch info " + method.Name + ": none"); return; }
                Func<Patch, string> show = p => p.owner + "/" + p.PatchMethod.DeclaringType?.Name + "." + p.PatchMethod.Name + "@" + p.priority;
                Log("patch info " + method.DeclaringType?.Name + "." + method.Name +
                    " postfixes=[" + string.Join(", ", info.Postfixes.Select(show).ToArray()) + "]" +
                    " prefixes=[" + string.Join(", ", info.Prefixes.Select(show).ToArray()) + "]");
            }
            catch (Exception exception) { Log("patch info failed: " + exception); }
        }

        internal static void ReportPatchesOnFirstShape() => ReportPatches();
    }

    [HarmonyPatch(typeof(Polymorph), "OnActivate")]
    internal static class DiagnosticsPolymorphActivate
    {
        private static void Postfix(Polymorph __instance)
        {
            try
            {
                TransientDiagnostics.ReportPatchesOnFirstShape();
                var data = TransientDiagnostics.Read(__instance, "Data");
                TransientDiagnostics.Log("Polymorph.OnActivate " + TransientDiagnostics.FactName(__instance) +
                    " appliedFacts=" + TransientDiagnostics.Count(TransientDiagnostics.Read(data, "AppliedFacts")));
            }
            catch (Exception exception) { TransientDiagnostics.Log("Polymorph.OnActivate probe: " + exception); }
        }
    }

    [HarmonyPatch(typeof(Polymorph), "OnDeactivate")]
    internal static class DiagnosticsPolymorphDeactivate
    {
        private static void Prefix(Polymorph __instance)
        {
            try
            {
                var data = TransientDiagnostics.Read(__instance, "Data");
                var applied = TransientDiagnostics.Read(data, "AppliedFacts");
                TransientDiagnostics.Log("Polymorph.OnDeactivate(before) " + TransientDiagnostics.FactName(__instance) +
                    " appliedFacts=" + TransientDiagnostics.Count(applied) +
                    " [" + TransientDiagnostics.DescribeFacts(applied) + "]");
            }
            catch (Exception exception) { TransientDiagnostics.Log("Polymorph.OnDeactivate probe: " + exception); }
        }
    }

    [HarmonyPatch(typeof(UnitBody), nameof(UnitBody.CancelPolymorphEffect))]
    internal static class DiagnosticsPolymorphCancel
    {
        private static void Postfix(UnitBody __instance)
        {
            try
            {
                var owner = __instance?.Owner;
                if (owner == null) return;
                TransientDiagnostics.Log("Body.CancelPolymorphEffect " + TransientDiagnostics.Who(owner.Unit) +
                    " polymorphed=" + __instance.IsPolymorphed +
                    " turnBack=[" + TransientDiagnostics.TurnBackFacts(owner) + "]");
            }
            catch (Exception exception) { TransientDiagnostics.Log("CancelPolymorphEffect probe: " + exception); }
        }
    }

    [HarmonyPatch(typeof(UnitPartTouch), nameof(UnitPartTouch.Init))]
    internal static class DiagnosticsTouchInit
    {
        private static void Postfix(UnitPartTouch __instance, BlueprintAbility __0)
        {
            try
            {
                TransientDiagnostics.Log("UnitPartTouch.Init " + TransientDiagnostics.Name(__0) +
                    " partAbility=" + TransientDiagnostics.Name(__instance.Ability?.Blueprint) +
                    " unique=" + __instance.Ability?.UniqueId);
            }
            catch (Exception exception) { TransientDiagnostics.Log("UnitPartTouch.Init probe: " + exception); }
        }
    }

    [HarmonyPatch(typeof(UnitPartTouch), "OnDispose")]
    internal static class DiagnosticsTouchDispose
    {
        private static void Prefix(UnitPartTouch __instance)
        {
            try
            {
                TransientDiagnostics.Log("UnitPartTouch.OnDispose(before) " + TransientDiagnostics.Name(__instance.Ability?.Blueprint) +
                    " unique=" + __instance.Ability?.UniqueId);
            }
            catch (Exception exception) { TransientDiagnostics.Log("UnitPartTouch.OnDispose probe: " + exception); }
        }

        private static void Postfix(UnitPartTouch __instance)
        {
            try
            {
                var facts = TransientDiagnostics.Read(TransientDiagnostics.Read(__instance, "Owner"), "Facts");
                var list = TransientDiagnostics.Read(facts, "List") as IEnumerable;
                var unique = TransientDiagnostics.Read(TransientDiagnostics.Read(__instance, "Ability"), "UniqueId");
                var survived = false;
                if (list != null)
                {
                    foreach (var fact in list)
                        if (Equals(TransientDiagnostics.Read(fact, "UniqueId"), unique)) { survived = true; break; }
                }
                TransientDiagnostics.Log("UnitPartTouch.OnDispose(after) factSurvived=" + survived);
            }
            catch (Exception exception) { TransientDiagnostics.Log("UnitPartTouch.OnDispose probe: " + exception); }
        }
    }

    [HarmonyPatch(typeof(TouchSpellsController), nameof(TouchSpellsController.OnAbilityEffectApplied))]
    internal static class DiagnosticsTouchApplied
    {
        private static void Prefix(AbilityExecutionContext __0)
        {
            try
            {
                var caster = __0?.MaybeCaster;
                if (caster == null) return;
                var part = caster.Get<UnitPartTouch>();
                TransientDiagnostics.Log("TouchSpells.OnAbilityEffectApplied " + TransientDiagnostics.Who(caster) +
                    " applied=" + TransientDiagnostics.Name(__0.Ability?.Blueprint) +
                    " part=" + (part?.Ability == null ? "none" : TransientDiagnostics.Name(part.Ability.Blueprint) + " unique=" + part.Ability.UniqueId));
            }
            catch (Exception exception) { TransientDiagnostics.Log("OnAbilityEffectApplied probe: " + exception); }
        }
    }
}
