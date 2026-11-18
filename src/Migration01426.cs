using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Transactions for this batch only. Existing shared modules are unchanged.
    internal static class Migration01426
    {
        // Native Condition.Check stores its last diagnostic result on the blueprint.
        // That result is runtime state, not an authored effect/identity difference.
        static readonly ConcurrentDictionary<Type, FieldInfo[]> DescribeFieldCache = new ConcurrentDictionary<Type, FieldInfo[]>();
        static FieldInfo[] DescribeFields(Type type) => DescribeFieldCache.GetOrAdd(type, t => {
            var fields = new List<FieldInfo>();
            for (var c = t; c != null && c != typeof(object) && c != typeof(BlueprintComponent) && c != typeof(UnityEngine.Object); c = c.BaseType)
                fields.AddRange(c.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(f => !f.IsStatic && !(f.DeclaringType == typeof(Kingmaker.ElementsSystem.Condition) && f.Name == "<LastResult>k__BackingField") &&
                        !(f.DeclaringType == typeof(Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterHasFacts) && f.Name == "m_FactsMissingCache")));
            return fields.OrderBy(f => f.DeclaringType.FullName + f.Name).ToArray();
        });
        internal static string Definition(object value) {
            var sb = new StringBuilder(256);
            DescribeInto(sb, value, new HashSet<object>());
            return sb.ToString();
        }
        internal static string Definition(object value, IEqualityComparer<object> comparer) {
            var sb = new StringBuilder(256);
            DescribeInto(sb, value, new HashSet<object>(comparer));
            return sb.ToString();
        }
        static void DescribeInto(StringBuilder sb, object value, HashSet<object> path) {
            if (value == null) { sb.Append("null"); return; }
            if (value is BlueprintReferenceBase reference) { sb.Append("ref:").Append(reference.Guid); return; }
            if (value is SimpleBlueprint blueprint) { sb.Append("bp:").Append(blueprint.AssetGuid); return; }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal) { sb.Append(type.FullName).Append(":").Append(value); return; }
            if (value is UnityEngine.Object) { sb.Append(type.FullName); return; }
            if (!type.IsValueType && !path.Add(value)) { sb.Append("cycle:").Append(type.FullName); return; }
            try {
                if (value is IEnumerable items) {
                    sb.Append("[");
                    bool first = true;
                    foreach (var x in items) { if (!first) sb.Append(","); first = false; DescribeInto(sb, x, path); }
                    sb.Append("]");
                    return;
                }
                sb.Append(type.FullName).Append("{");
                var fields = DescribeFields(type);
                for (int i = 0; i < fields.Length; i++) {
                    if (i > 0) sb.Append(";");
                    var f = fields[i];
                    var fieldValue = f.GetValue(value);
                    sb.Append(f.Name).Append("=");
                    if (fieldValue == null && typeof(BlueprintReferenceBase).IsAssignableFrom(f.FieldType)) sb.Append("ref:");
                    else DescribeInto(sb, fieldValue, path);
                }
                sb.Append("}");
            } finally { if (!type.IsValueType) path.Remove(value); }
        }
        internal static string Difference(string expected, string actual) {
            if (expected == actual) return "unchanged";
            expected = expected ?? "null"; actual = actual ?? "null";
            int offset = 0; while (offset < expected.Length && offset < actual.Length && expected[offset] == actual[offset]) offset++;
            int equals = expected.Length == 0 ? -1 : expected.LastIndexOf('=', Math.Min(offset, expected.Length - 1));
            int start = equals < 0 ? 0 : Math.Max(expected.LastIndexOf(';', equals), expected.LastIndexOf('{', equals)) + 1;
            string field = equals < 0 ? "shape/identity" : expected.Substring(start, equals - start);
            int excerpt = Math.Max(0, offset - 80);
            return "field=" + field + "; offset=" + offset + "; expected=" + expected.Substring(excerpt, Math.Min(180, expected.Length - excerpt)) +
                "; actual=" + actual.Substring(Math.Min(excerpt, actual.Length), Math.Min(180, Math.Max(0, actual.Length - excerpt)));
        }
        internal static PrerequisiteFeature Require(BlueprintFeature feature, bool any = false) {
            var p = new PrerequisiteFeature { Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All };
            Set(p, "m_Feature", Ref<BlueprintFeatureReference>(feature)); return p;
        }
        internal static void Commit(PsychicAssets assets, BlueprintFeatureSelection[] pools, params BlueprintFeature[] roots) {
            var cache = assets.Cache; var staged = assets.Assets.Values.ToArray();
            foreach (var bp in staged) PsychicRepairComponents.UniqueNames(bp);
            var existing = staged.Select(bp => cache.Load(bp.AssetGuid) as BlueprintScriptableObject).ToArray();
            for (int i = 0; i < staged.Length; i++) {
                var raw = cache.Load(staged[i].AssetGuid);
                if (raw != null && (existing[i] == null || raw.GetType() != staged[i].GetType() || raw.name != staged[i].name ||
                    existing[i].ComponentsArray == null || existing[i].ComponentsArray.Any(c => c == null) ||
                    !existing[i].ComponentsArray.Select(Definition).SequenceEqual(staged[i].ComponentsArray.Select(Definition))))
                    throw new InvalidOperationException("01426 blueprint identity collision: " + staged[i].name);
            }
            bool present = existing.Any(b => b != null);
            if (present && existing.Any(b => b == null)) throw new InvalidOperationException("01426 incomplete existing module; preserved.");
            var previous = pools.Select(p => new[] { Get(p, "m_Features"), Get(p, "m_AllFeatures") }).ToArray();
            try {
                if (!present) { assets.Enable(); foreach (var bp in staged) cache.AddCachedBlueprint(bp.AssetGuid, bp); }
                T09CombatFeats.Append(pools, roots.Select(bp => (BlueprintFeature)cache.Load(bp.AssetGuid)).ToArray());
            }
            catch {
                for (int i = 0; i < pools.Length; i++) { Set(pools[i], "m_Features", previous[i][0]); Set(pools[i], "m_AllFeatures", previous[i][1]); }
                if (!present) foreach (var bp in staged) if (ReferenceEquals(cache.Load(bp.AssetGuid), bp)) cache.RemoveCachedBlueprint(bp.AssetGuid);
                throw;
            }
        }
    }
}
