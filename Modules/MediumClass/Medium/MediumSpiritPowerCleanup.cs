using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using MediumClass.Medium.NewComponents.AbilitySpecific;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;

namespace MediumClass.Medium
{
    internal static class MediumSpiritPowerCleanup
    {
        [ThreadStatic] private static object removingFact;
        internal static bool IsRemoving(object fact) => fact != null && ReferenceEquals(removingFact, fact);
        internal static void RemoveOwnedFact(UnitEntityData owner, Kingmaker.EntitySystem.EntityFact fact)
        {
            var previous=removingFact;
            removingFact=fact;
            try { owner.RemoveFact(fact); }
            finally { removingFact=previous; }
        }
        private static readonly HashSet<string> OwnedIds = new HashSet<string>(typeof(Guids)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => Normalize((string)f.GetRawConstantValue())), StringComparer.OrdinalIgnoreCase);

        private static string Normalize(string id) => id?.Replace("-", "") ?? "";

        // Resolve all definitions before removing facts. Selection can already be
        // empty during native detach, and it is not evidence that cleanup finished.
        internal static BlueprintFeature[] Catalogue(UnitEntityData owner)
        {
            var refs = new List<BlueprintFeatureReference>();
            var state = owner.Get<UnitPartMedium>();
            if (state != null)
                foreach (var entry in state.Spirits.Values)
                    refs.AddRange(new[] { entry.SpiritLesserPower, entry.SpiritIntermediatePower,
                        entry.SpiritIntermediatePowerMove, entry.SpiritIntermediatePowerSwift,
                        entry.OverwriteIntermediatePower, entry.SpiritGreaterPower,
                        entry.OverwriteGreaterPower, entry.SpiritSupremePower });
            // The class catalogue also covers old orphaned powers after a runtime
            // table/selection was lost. It includes Prowler's Adriel definitions.
            foreach (var entry in BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit)
                .GetComponents<MediumSpiritComponent>())
                refs.AddRange(new[] { entry.SpiritLesserPower, entry.SpiritIntermediatePower,
                    entry.SpiritIntermediatePowerMove, entry.SpiritIntermediatePowerSwift,
                    entry.SpiritOverwriteIntermediate, entry.SpiritGreaterPower,
                    entry.SpiritOverwriteGreater, entry.SpiritSupremePower });
            var prohibited = new[] { Normalize(Guids.MediumSpellcasterFeatProhibitArchmage),
                Normalize(Guids.MediumSpellcasterFeatProhibitHierophant) };
            return refs.Select(r => r?.Get()).Where(b => b != null
                && !prohibited.Contains(Normalize(b.AssetGuid.ToString()))).Distinct().ToArray();
        }

        internal static void RevokeAll(UnitEntityData owner, BlueprintFeature[] powers = null)
        {
            powers = powers ?? Catalogue(owner);
            foreach (var power in powers)
            {
                int ranks = owner.Progression.Features.GetRank(power);
                for (int i = 0; i < ranks; i++) owner.RemoveFact(power);
            }
            RevokeOrphanAbilities(owner, powers);
        }

        internal static void RevokeSeanceOrphans(UnitEntityData owner)
        {
            var boons = BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit)
                .GetComponents<MediumSpiritComponent>().Select(c => c.SpiritSeanceBoon?.Get())
                .Where(b => b != null).Distinct().ToArray();
            RevokeOrphanAbilities(owner, boons);
        }

        internal static void RevokeOrphanAbilities(UnitEntityData owner, BlueprintFeature[] powers)
        {
            // Native AddFacts normally disposes its children. Repair saved orphan
            // abilities too, using exact mod identities reachable from spirit
            // powers; never sweep native weapon training/proficiencies or all abilities.
            // Shared seance can transfer a boon to another provider. Never remove
            // its buttons while the recipient still owns the granting feature.
            var abilities = new HashSet<BlueprintUnitFact>(powers.Where(p => !owner.HasFact(p)).SelectMany(p => p.GetComponents<AddFacts>())
                .SelectMany(c => c.Facts)
                .Where(b => (b is BlueprintAbility || b is BlueprintActivatableAbility)
                    && OwnedIds.Contains(Normalize(b.AssetGuid.ToString()))));
            foreach (var fact in owner.Facts.List.Where(f => f.Blueprint is BlueprintUnitFact b
                && abilities.Contains(b)).ToArray())
            {
                // AbilityCollection otherwise retains slotted abilities as disabled
                // facts. Level-up can add such slots even if the player did not.
                var previous = removingFact;
                removingFact = fact;
                try { owner.RemoveFact(fact); }
                finally { removingFact = previous; }
            }
        }
    }
}
