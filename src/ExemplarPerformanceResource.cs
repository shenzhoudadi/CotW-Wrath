using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.FactLogic;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class ExemplarPerformanceResource
    {
        internal const string ModuleName = "Exemplar performance rounds";
        internal const string ResourceId = "e190ba276831b5c4fa28737e5e49e6a6";
        internal const string FactId = "b92bfc201c6a79e49afd0b5cfbfc269f";
        private const string BardId = "772c83a25e2268e448e841dcd548235f";
        private const string ProviderType = "BrawlerClassWrath.NewMechanics.IncreaseResourcesByClassWithArchetype";

        internal static void Register(BlueprintsCache cache)
        {
            var brawler = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BrawlerClassId)) as BlueprintCharacterClass;
            var exemplar = cache.Load(BlueprintGuid.Parse(ExemplarInspireCourage.ArchetypeId)) as BlueprintArchetype;
            var resource = cache.Load(BlueprintGuid.Parse(ResourceId)) as BlueprintAbilityResource;
            var fact = cache.Load(BlueprintGuid.Parse(FactId)) as BlueprintFeature;
            if (brawler?.name != "RMBrawlerHybridClass" || exemplar?.name != "BrawlerExemplar" ||
                resource?.name != "BardicPerformanceResource" || fact?.name != "BardicPerformanceResourceFact" ||
                ((BlueprintCharacterClass)Get(exemplar, "m_ParentClass"))?.AssetGuid != brawler.AssetGuid ||
                !((BlueprintArchetypeReference[])Get(brawler, "m_Archetypes") ?? Array.Empty<BlueprintArchetypeReference>()).Any(r => r?.Guid == exemplar.AssetGuid))
                throw new InvalidOperationException("Exemplar performance blueprint identity or class registration is not the verified layout.");

            // Retain the original CotW callback: it supplies Brawler level - 2.
            // The native fact supplies 2 + CHA + Bard level. Removing only the
            // redundant native Brawler contribution yields L + CHA for pure
            // Exemplar, without replacing other providers' resource callbacks.
            var providers = (fact.ComponentsArray ?? Array.Empty<BlueprintComponent>())
                .Where(c => c != null && c.GetType().FullName == ProviderType && c.GetType().Assembly.GetName().Name == "BrawlerClassWrath").ToArray();
            if (providers.Length != 1 || !ReferenceEquals(Get(providers[0], "Resource"), resource) ||
                !ReferenceEquals(Get(providers[0], "CharacterClass"), brawler) ||
                !ReferenceEquals(Get(providers[0], "Archetype"), exemplar) || (int)Get(providers[0], "base_value") != -2)
                throw new InvalidOperationException("Exemplar performance resource callback is missing, duplicated or changed; no correction applied.");
            var native = fact.ComponentsArray.OfType<IncreaseResourcesByClass>().ToArray();
            var nativeArchetype = native.Length == 1 ? (BlueprintArchetypeReference)Get(native[0], "m_Archetype") : null;
            if (native.Length != 1 || native[0].BaseValue != 2 || native[0].Stat != StatType.Charisma ||
                ((BlueprintAbilityResourceReference)Get(native[0], "m_Resource"))?.Guid != resource.AssetGuid ||
                ((BlueprintCharacterClassReference)Get(native[0], "m_CharacterClass"))?.Guid != BlueprintGuid.Parse(BardId) ||
                (nativeArchetype != null && nativeArchetype.Guid != default(BlueprintGuid)))
                throw new InvalidOperationException("Native performance bonus schema changed; no correction applied.");

            var field = AccessTools.Field(typeof(BlueprintAbilityResource), "m_MaxAmount");
            var amount = field.GetValue(resource); // Value type: edit a detached boxed copy, commit once.
            if ((int)Get(amount, "BaseValue") != 0 || !(bool)Get(amount, "IncreasedByLevel") ||
                (int)Get(amount, "LevelIncrease") != 1 || (bool)Get(amount, "IncreasedByStat") ||
                (bool)Get(amount, "IncreasedByLevelStartPlusDivStep") || (bool)Get(resource, "m_UseMax"))
                throw new InvalidOperationException("Native performance maximum formula changed; no correction applied.");
            var classes = (BlueprintCharacterClassReference[])Get(amount, "m_Class");
            var archetypes = (BlueprintArchetypeReference[])Get(amount, "m_Archetypes");
            if (classes == null || archetypes == null || classes.Any(r => r == null) || archetypes.Any(r => r == null))
                throw new InvalidOperationException("Invalid performance class/archetype references; no correction applied.");
            int classCount = classes.Count(r => r.Guid == brawler.AssetGuid);
            int archetypeCount = archetypes.Count(r => r.Guid == exemplar.AssetGuid);
            if (classCount == 0 && archetypeCount == 0) return; // Already repaired, including by another provider.
            if (classCount != 1 || archetypeCount != 1)
                throw new InvalidOperationException("Ambiguous performance class/archetype contributions; no correction applied.");
            AccessTools.Field(amount.GetType(), "m_Class").SetValue(amount, classes.Where(r => r.Guid != brawler.AssetGuid).ToArray());
            AccessTools.Field(amount.GetType(), "m_Archetypes").SetValue(amount, archetypes.Where(r => r.Guid != exemplar.AssetGuid).ToArray());
            field.SetValue(resource, amount);
        }

        private static object Get(object obj, string name)
            => (AccessTools.Field(obj.GetType(), name) ?? throw new MissingFieldException(obj.GetType().FullName, name)).GetValue(obj);
    }
}
