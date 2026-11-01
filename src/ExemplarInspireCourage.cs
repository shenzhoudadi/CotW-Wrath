using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Enums;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace CallOfTheWild.Wrath.Prototype
{
    // Correct only the confirmed shared Inspire Courage rank component. No new
    // save facts, changes to other songs, or compile dependency on BrawlerClassWrath.
    internal static class ExemplarInspireCourage
    {
        internal const string ModuleName = "Exemplar Inspire Courage scaling";
        internal const string ArchetypeId = "0864a8c5eae54557bb25b30ba5c46e66";
        internal const string EffectId = "6d6d9e06b76f5204a8b7856c78607d5d";
        private static ContextRankConfig target;
        private static readonly FieldInfo Classes = AccessTools.Field(typeof(ContextRankConfig), "m_Class");
        private static readonly FieldInfo DisableRankBonus = AccessTools.Field(typeof(ContextRankConfig), "m_DisableRankBonus");
        private static readonly MethodInfo Clone = AccessTools.Method(typeof(object), "MemberwiseClone");
        private static readonly MethodInfo BaseValue = AccessTools.Method(typeof(ContextRankConfig), "GetBaseValue");

        internal static void Register(BlueprintsCache cache)
        {
            target = null;
            var brawler = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BrawlerClassId)) as BlueprintCharacterClass;
            var exemplar = cache.Load(BlueprintGuid.Parse(ArchetypeId)) as BlueprintArchetype;
            var effect = cache.Load(BlueprintGuid.Parse(EffectId)) as BlueprintBuff;
            if (brawler?.name != "RMBrawlerHybridClass" || exemplar?.name != "BrawlerExemplar" ||
                effect?.name != "InspireCourageEffectBuff")
                throw new InvalidOperationException("Exemplar/Inspire Courage blueprint identity differs from the audited source.");
            var parent = (BlueprintCharacterClass)AccessTools.Field(typeof(BlueprintArchetype), "m_ParentClass").GetValue(exemplar);
            var archetypes = (BlueprintArchetypeReference[])AccessTools.Field(typeof(BlueprintCharacterClass), "m_Archetypes").GetValue(brawler);
            if (parent?.AssetGuid != brawler.AssetGuid || archetypes == null || !archetypes.Any(a => a?.Guid == exemplar.AssetGuid))
                throw new InvalidOperationException("Exemplar is not registered on the expected Brawler class.");
            var rank = effect.ComponentsArray.OfType<ContextRankConfig>().SingleOrDefault();
            if (rank == null || !MatchesSchema(rank))
                throw new InvalidOperationException("Inspire Courage rank schema differs from the audited game/Brawler combination; correction not enabled.");
            if (Classes == null || DisableRankBonus == null || Clone == null || BaseValue == null)
                throw new MissingMemberException("Required native rank API is unavailable.");
            target = rank;
        }

        private static bool MatchesSchema(ContextRankConfig rank)
        {
            var classes = (BlueprintCharacterClassReference[])Classes.GetValue(rank);
            var archetypes = (BlueprintArchetypeReference[])AccessTools.Field(typeof(ContextRankConfig), "m_AdditionalArchetypes").GetValue(rank);
            return (ContextRankBaseValueType)AccessTools.Field(typeof(ContextRankConfig), "m_BaseValueType").GetValue(rank) == ContextRankBaseValueType.MaxClassLevelWithArchetype &&
                (ContextRankProgression)AccessTools.Field(typeof(ContextRankConfig), "m_Progression").GetValue(rank) == ContextRankProgression.StartPlusDivStep &&
                (int)AccessTools.Field(typeof(ContextRankConfig), "m_StartLevel").GetValue(rank) == -1 &&
                (int)AccessTools.Field(typeof(ContextRankConfig), "m_StepLevel").GetValue(rank) == 6 &&
                !(bool)AccessTools.Field(typeof(ContextRankConfig), "m_ExceptClasses").GetValue(rank) &&
                rank.Type == AbilityRankType.Default &&
                classes != null && classes.Any(c => c?.Guid == BlueprintGuid.Parse(BlueprintRegistration.BrawlerClassId)) &&
                archetypes != null && archetypes.Any(a => a?.Guid == BlueprintGuid.Parse(ArchetypeId));
        }

        [HarmonyPatch(typeof(ContextRankConfig), "GetBaseValue")]
        private static class RankPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ContextRankConfig __instance, MechanicsContext context, ref int __result)
            {
                if (!ReferenceEquals(__instance, target) || context?.MaybeCaster == null || !MatchesSchema(__instance)) return;
                var brawler = context.MaybeCaster.Descriptor.Progression.Classes.FirstOrDefault(c =>
                    c.CharacterClass.AssetGuid == BlueprintGuid.Parse(BlueprintRegistration.BrawlerClassId));
                if (brawler == null || !brawler.Archetypes.Any(a => a.AssetGuid == BlueprintGuid.Parse(ArchetypeId))) return;
                // Reuse native filtering for every OTHER class (Sensei, Bard, etc.),
                // including rank bonuses. A detached clone never mutates shared blueprints.
                var otherClasses = (ContextRankConfig)Clone.Invoke(__instance, null);
                Classes.SetValue(otherClasses, ((BlueprintCharacterClassReference[])Classes.GetValue(__instance))
                    .Where(c => c?.Guid != BlueprintGuid.Parse(BlueprintRegistration.BrawlerClassId)).ToArray());
                int otherValue = (int)BaseValue.Invoke(otherClasses, new object[] { context });
                int rankBonus = (bool)DisableRankBonus.GetValue(__instance) ? 0 : context.Params.RankBonus;
                int unadjusted = Math.Max(0, brawler.Level + rankBonus);
                // A different mod may already have changed this result. Do not apply
                // a second correction to an unexpected result.
                if (__result != Math.Max(otherValue, unadjusted)) return;
                __result = Math.Max(otherValue, Math.Max(0, brawler.Level - 2) + rankBonus);
            }
        }
    }
}

