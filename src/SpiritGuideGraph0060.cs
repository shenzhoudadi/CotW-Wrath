using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static CallOfTheWild.Wrath.Prototype.SpiritGuideBridge0060;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0060 D-F01. A bounded closure for the mutable part of a cloned
    // native ability graph. CopyData keeps BlueprintReference fields shared, so
    // copying only the parent left the Shaman class, the Shaman resource and the
    // Shaman aura rank reachable through Buff -> AddAreaEffect -> Area ->
    // EffectBuff and through AbilityVariants. This walks those edges, clones each
    // native node once per private root and re-points the reference, while
    // shared read-only effects stay shared. It never edits a native blueprint and
    // never clones the whole library.
    internal static class SpiritGuideGraph0060
    {
        private static readonly MethodInfo CreateTyped =
            AccessTools.Method(typeof(BlueprintReferenceBase), "CreateTyped");
        private static readonly PropertyInfo Cached =
            AccessTools.Property(typeof(BlueprintReferenceBase), "Cached");

        // Only these native node kinds carry the mutable class/stat/resource
        // edges this package must rebind. A plain feature is handled separately
        // (and only when it grants a native pool we must not grant).
        private static bool IsGraphNode(BlueprintScriptableObject bp) =>
            bp is BlueprintAbility || bp is BlueprintActivatableAbility ||
            bp is BlueprintBuff || bp is BlueprintAbilityAreaEffect;

        internal static void Privatize(Factory f, BlueprintCharacterClass oracle, BlueprintFeature feature,
            string token, BlueprintAbilityResource resource, bool hexStats)
        {
            int serial = 0;
            var map = new Dictionary<BlueprintGuid, BlueprintScriptableObject>();

            BlueprintScriptableObject CloneNode(BlueprintScriptableObject donor)
            {
                if (donor == null) return null;
                if (donor.name != null && donor.name.StartsWith(AssetPrefix, StringComparison.Ordinal)) return donor;
                if (map.TryGetValue(donor.AssetGuid, out var existing)) return existing;
                var clone = (BlueprintScriptableObject)f.Clone(donor, token + "Node" + serial++,
                    donor.name + " (Spirit Guide)", "Private Spirit Guide adaptation of " + donor.name + ".");
                // Register before recursion: cyclic Buff <-> Area graphs are legal.
                map[donor.AssetGuid] = clone;
                SpiritGuideSpirits0060.StripSpellLists(clone);
                if (clone is BlueprintAbility || clone is BlueprintActivatableAbility)
                    if (Has(clone, "m_Parent")) Set(clone, "m_Parent", null);
                // A cloned fact that carried a native pool must not grant it:
                // this package grants its own resource through the tier/hex.
                // D2-F01: this applies to a plain BlueprintUnitFact too, not
                // only to BlueprintFeature; the Life channel fact is exactly
                // such a carrier.
                if (clone is BlueprintUnitFact && !IsGraphNode(clone))
                    clone.ComponentsArray = (clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                        .Where(c => c == null || c.GetType().Name != "AddAbilityResources").ToArray();
                SpiritGuideSpirits0060.Rebind(clone, oracle, StatType.Charisma, _ => resource);
                if (hexStats) SpiritGuideSpirits0060.SwapStats(clone, true);
                Rewrite(clone, CloneNode, new HashSet<object>(PsychicAssets.IdentityComparer));
                return clone;
            }

            // COTW-0060 D-F01 / D2-F01. AddFacts can hide a native resource
            // carrier behind one plain BlueprintFeature hop:
            //   Life base feature -> AddFacts -> ShamanLifeSpiritChannelEnergyFeature
            //     -> AddFacts -> 5bb0d763... (plain BlueprintUnitFact with only
            //        AddAbilityResources for the native Shaman channel pool)
            //     -> AddFacts -> the two real channel abilities (graph nodes).
            // Replace only handles the four graph node kinds, and the old
            // `is BlueprintFeature` test only looked one level down, so the
            // native feature AND its native pool carrier both stayed reachable.
            // Walk at most two AddFacts hops, clone only native features that
            // actually lead to a pool carrier, and drop the pure carrier itself.
            // The channel abilities are cloned/rebound by Rewrite, never dropped.
            void ProcessAddFacts(BlueprintScriptableObject holder, int depth)
            {
                foreach (var component in holder.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                {
                    if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                    var refs = (BlueprintUnitFactReference[])Get(component, "m_Facts") ?? Array.Empty<BlueprintUnitFactReference>();
                    var kept = new List<BlueprintUnitFactReference>();
                    foreach (var reference in refs)
                    {
                        var target = reference?.Get() as BlueprintScriptableObject;
                        if (target == null) { kept.Add(reference); continue; }
                        if (PureResourceCarrier(target)) continue;
                        if (!IsGraphNode(target) && target is BlueprintFeature &&
                            depth < 2 && HasNativePoolCarrier(target))
                        {
                            var clone = CloneNode(target);
                            kept.Add((BlueprintUnitFactReference)MakeRef(typeof(BlueprintUnitFactReference), clone));
                            ProcessAddFacts(clone, depth + 1);
                            continue;
                        }
                        kept.Add(reference);
                    }
                    Set(component, "m_Facts", kept.ToArray());
                }
            }

            ProcessAddFacts(feature, 0);
            Rewrite(feature, CloneNode, new HashSet<object>(PsychicAssets.IdentityComparer));
        }

        // True only when every component is a native resource grant: there is no
        // ability/feature behaviour to preserve by cloning this fact.
        private static bool PureResourceCarrier(BlueprintScriptableObject bp)
        {
            var components = (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>()).Where(c => c != null).ToArray();
            return components.Length > 0 && components.All(c => c.GetType().Name == "AddAbilityResources");
        }

        private static bool GrantsPool(BlueprintScriptableObject bp) =>
            (bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Any(c => c != null && c.GetType().Name == "AddAbilityResources");

        // A native feature is worth cloning when it grants a native pool itself
        // or its own AddFacts leads to a pure resource carrier one hop down.
        // This is the bounded edge set D2-F01 asked for, not a library walk.
        private static bool HasNativePoolCarrier(BlueprintScriptableObject bp)
        {
            if (GrantsPool(bp)) return true;
            foreach (var component in bp.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component == null || component.GetType().Name != "AddFacts" || !Has(component, "m_Facts")) continue;
                foreach (var reference in (BlueprintUnitFactReference[])Get(component, "m_Facts") ??
                                          Array.Empty<BlueprintUnitFactReference>())
                {
                    var nested = reference?.Get() as BlueprintScriptableObject;
                    if (nested != null && PureResourceCarrier(nested)) return true;
                }
            }
            return false;
        }

        private static void Rewrite(BlueprintScriptableObject root,
            Func<BlueprintScriptableObject, BlueprintScriptableObject> map, HashSet<object> seen)
        {
            // The mutable edges are not all inside components: ActivatableAbility.m_Buff
            // and AbilityAreaEffect's own fields are serialized on the blueprint. Walking
            // only ComponentsArray left the native Shaman Buff/Area/EffectBuff chain
            // reachable, so the aura rank still read the Shaman class.
            for (var t = root.GetType(); t != null && t != typeof(object) &&
                 t != typeof(BlueprintScriptableObject) && t != typeof(SimpleBlueprint); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                  BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.Name == "Components" || field.Name == "m_Components") continue;
                    var current = field.GetValue(root);
                    var replaced = Replace(current, map);
                    if (replaced != null) field.SetValue(root, replaced);
                    else Rewrite(current, map, seen);
                }
            foreach (var component in root.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                Rewrite(component, map, seen);
        }

        private static void Rewrite(object value,
            Func<BlueprintScriptableObject, BlueprintScriptableObject> map, HashSet<object> seen)
        {
            if (value == null) return;
            var type = value.GetType();
            if (value is string || type.IsValueType || value is Type || value is Delegate) return;
            if (value is SimpleBlueprint || value is UnityEngine.Object) return;
            if (!seen.Add(value)) return;

            if (value is Array array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    var item = array.GetValue(i);
                    var replaced = Replace(item, map);
                    if (replaced != null) array.SetValue(replaced, i);
                    else Rewrite(item, map, seen);
                }
                return;
            }
            if (value is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var replaced = Replace(item, map);
                    if (replaced != null) list[i] = replaced;
                    else Rewrite(item, map, seen);
                }
                return;
            }
            for (var t = type; t != null && t != typeof(object) &&
                 t != typeof(BlueprintComponent) && t != typeof(Kingmaker.ElementsSystem.Element); t = t.BaseType)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                  BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic) continue;
                    var current = field.GetValue(value);
                    var replaced = Replace(current, map);
                    if (replaced != null) field.SetValue(value, replaced);
                    else Rewrite(current, map, seen);
                }
        }

        private static object Replace(object value, Func<BlueprintScriptableObject, BlueprintScriptableObject> map)
        {
            if (!(value is BlueprintReferenceBase reference)) return null;
            var target = reference.GetBlueprint() as BlueprintScriptableObject;
            if (target == null || !IsGraphNode(target)) return null;
            var mapped = map(target);
            if (mapped == null || ReferenceEquals(mapped, target)) return null;
            return MakeRef(value.GetType(), mapped);
        }

        private static BlueprintReferenceBase MakeRef(Type referenceType, SimpleBlueprint target)
        {
            var reference = (BlueprintReferenceBase)CreateTyped.MakeGenericMethod(referenceType)
                .Invoke(null, new object[] { target });
            Cached.SetValue(reference, target, null);
            return reference;
        }
    }
}
