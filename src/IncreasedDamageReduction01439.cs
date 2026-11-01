using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Mechanics.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // COTW-0584 changes only the native Increased Damage Reduction defensive
    // power. FeatureListRanks sums FeatureCollection.GetRank for every listed
    // feature, so two references to this feature make each selected rank count
    // as 2 while AsIs leaves that base value unchanged. The component, its
    // prerequisites, the selection list and every other damage-reduction
    // feature stay untouched.
    internal static class IncreasedDamageReduction01439
    {
        internal const string ModuleName = "Increased Damage Reduction";
        internal const string FeatureId = "d10496e92d0799a40bb3930b8f4fda0d";
        internal const string DescriptionKey = "cotw-wrath.stalwart.increased-damage-reduction.description";
        internal const string DescriptionEn = "The stalwart defender's damage reduction from this class increases by 2/—. This increase is always active while the stalwart defender is in a defensive stance. He can select this power up to two times. Its effects stack. The stalwart defender must be at least 6th level before selecting this defensive power.";
        internal const string DescriptionZh = DescriptionEn;

        internal static void Register(BlueprintsCache cache)
        {
            var feature = cache.Load(BlueprintGuid.Parse(FeatureId)) as BlueprintFeature
                ?? throw new InvalidOperationException("Increased Damage Reduction defensive power is missing.");
            var ranks = feature.GetComponents<ContextRankConfig>().ToArray();
            if (ranks.Length != 1)
                throw new InvalidOperationException("Increased Damage Reduction needs exactly one rank config; found " + ranks.Length + ".");
            var rank = ranks[0];
            var snapshot = Snapshot.Capture(feature, rank);
            try
            {
                if (feature.Ranks != 2 || !HasGroup(feature, "DefensivePower"))
                    throw new InvalidOperationException("Increased Damage Reduction is not the native two-rank defensive power.");
                if (IsNative(feature, rank)) WriteDouble(feature, rank);
                else if (!IsDoubled(feature, rank))
                    throw new InvalidOperationException("Increased Damage Reduction rank config is neither the native one-per-rank shape nor the doubled shape: " + Describe(rank) + ".");
                Set(feature, "m_Description", new LocalizedString { Key = DescriptionKey });
                ConfirmCommitted();
                if (!IsDoubled(feature, rank) || DescriptionKeyOf(feature) != DescriptionKey || feature.Ranks != 2 || !snapshot.SameComponents(feature))
                    throw new InvalidOperationException("Increased Damage Reduction scale did not stick without disturbing its components.");
            }
            catch
            {
                snapshot.Restore(feature, rank);
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ConfirmCommitted() { }

        static bool HasGroup(BlueprintFeature feature, string name)
        {
            if (feature.Groups == null) return false;
            foreach (var group in feature.Groups) if (group.ToString() == name) return true;
            return false;
        }

        static bool IsNative(BlueprintFeature feature, ContextRankConfig rank)
            => EnumName(rank, "m_BaseValueType") == "FeatureRank"
               && EnumName(rank, "m_Type") == "Default"
               && EnumName(rank, "m_Progression") == "AsIs"
               && Int(rank, "m_StartLevel") == 0 && Int(rank, "m_StepLevel") == 0
               && !Bool(rank, "m_UseMin") && !Bool(rank, "m_UseMax")
               && List(rank).Length == 0
               && ReferenceGuid(Get(rank, "m_Feature")) == feature.AssetGuid;

        static bool IsDoubled(BlueprintFeature feature, ContextRankConfig rank)
        {
            var list = List(rank);
            return EnumName(rank, "m_BaseValueType") == "FeatureListRanks"
                && EnumName(rank, "m_Type") == "Default"
                && EnumName(rank, "m_Progression") == "AsIs"
                && Int(rank, "m_StartLevel") == 0 && Int(rank, "m_StepLevel") == 0
                && !Bool(rank, "m_UseMin") && !Bool(rank, "m_UseMax")
                && list.Length == 2 && list.All(guid => guid == feature.AssetGuid);
        }

        static void WriteDouble(BlueprintFeature feature, ContextRankConfig rank)
        {
            SetEnum(rank, "m_BaseValueType", "FeatureListRanks");
            Set(rank, "m_FeatureList", new[] { Ref<BlueprintFeatureReference>(feature), Ref<BlueprintFeatureReference>(feature) });
        }

        static string Describe(ContextRankConfig rank)
            => EnumName(rank, "m_BaseValueType") + "/" + EnumName(rank, "m_Progression") + "/list=" + List(rank).Length;

        static string EnumName(object target, string field)
        {
            var value = Get(target, field);
            return value == null ? "" : value.ToString();
        }

        static int Int(object target, string field) => (int)Get(target, field);
        static bool Bool(object target, string field) => (bool)Get(target, field);

        static BlueprintGuid[] List(ContextRankConfig rank)
        {
            var value = Get(rank, "m_FeatureList") as Array;
            if (value == null) return Array.Empty<BlueprintGuid>();
            var result = new BlueprintGuid[value.Length];
            for (int i = 0; i < value.Length; i++) result[i] = ReferenceGuid(value.GetValue(i));
            return result;
        }

        static BlueprintGuid ReferenceGuid(object reference)
            => reference is BlueprintReferenceBase typed ? typed.Guid : BlueprintGuid.Empty;

        static string DescriptionKeyOf(BlueprintFeature feature)
        {
            var value = Get(feature, "m_Description");
            if (value == null) return "";
            var key = value.GetType().GetProperty("Key")?.GetValue(value);
            return key as string ?? "";
        }

        sealed class Snapshot
        {
            readonly object description, baseType, featureRef, featureList, progression, rankType, start, step, useMin, useMax;
            readonly BlueprintComponent[] components;
            internal static Snapshot Capture(BlueprintFeature feature, ContextRankConfig rank) => new Snapshot(feature, rank);
            Snapshot(BlueprintFeature feature, ContextRankConfig rank)
            {
                description = Get(feature, "m_Description");
                components = feature.ComponentsArray;
                baseType = Get(rank, "m_BaseValueType"); featureRef = Get(rank, "m_Feature"); featureList = Get(rank, "m_FeatureList");
                progression = Get(rank, "m_Progression"); rankType = Get(rank, "m_Type");
                start = Get(rank, "m_StartLevel"); step = Get(rank, "m_StepLevel"); useMin = Get(rank, "m_UseMin"); useMax = Get(rank, "m_UseMax");
            }
            internal bool SameComponents(BlueprintFeature feature)
                => ReferenceEquals(feature.ComponentsArray, components)
                   && feature.ComponentsArray != null
                   && feature.ComponentsArray.Length == components.Length
                   && feature.ComponentsArray.Zip(components, ReferenceEquals).All(same => same);
            internal void Restore(BlueprintFeature feature, ContextRankConfig rank)
            {
                feature.ComponentsArray = components;
                Set(feature, "m_Description", description);
                Set(rank, "m_BaseValueType", baseType); Set(rank, "m_Feature", featureRef); Set(rank, "m_FeatureList", featureList);
                Set(rank, "m_Progression", progression); Set(rank, "m_Type", rankType);
                Set(rank, "m_StartLevel", start); Set(rank, "m_StepLevel", step); Set(rank, "m_UseMin", useMin); Set(rank, "m_UseMax", useMax);
            }
        }
    }
}
