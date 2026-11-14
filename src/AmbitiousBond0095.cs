// COTW-0095 Ambitious Bond: the insinuator's fiendish boon, except the first
// special ability added to a bonded weapon must match the outsider invoked for
// the day - anarchic for a chaotic patron, axiomatic for a lawful one, unholy
// for an evil one.
//
// MicroscopicContentExpansion builds the antipaladin bond as activatable
// weapon-enchantment toggles (ActivatableAbilityGroup 3) that spend the bond's
// enchant pool, so "the first special ability" is really "which toggles may be
// switched on". The gate therefore lives on the toggles and is re-read every
// time one is switched, which is exactly when the bond is configured.
//
// Those toggles are shared with the plain antipaladin and with the Tyrant, so
// the restriction stays inert for every unit that is not an insinuator. The
// lawful property is missing from the antipaladin list altogether, so it is
// cloned from the Tyrant and granted by this module alone.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("b73d4c2f8e1a45d0a6f9c2b5e7d13a84")]
    public sealed class InvokedAlignmentBondRestriction0095 : ActivatableAbilityRestriction
    {
        // Mask over the six outsider alignments an insinuator can invoke.
        public AlignmentMaskType Alignment = AlignmentMaskType.Any;

        // Extra fact requirement; used to keep the borrowed lawful property off
        // insinuators who chose the outsider servant instead of the weapon.
        public BlueprintUnitFactReference RequiresFact;

        protected override bool IsAvailable()
        {
            // Everyone but an insinuator keeps the native behaviour, so the
            // shared antipaladin and Tyrant toggles are untouched.
            if (!AmbitiousBond0095.Bound(Owner)) return true;
            var required = RequiresFact?.Get();
            if (required != null && !Owner.HasFact(required)) return false;
            var invoked = Insinuator0095.InvokedAlignment(Owner);
            return invoked != null && (invoked.Value & Alignment) != 0;
        }
    }

    internal static class AmbitiousBond0095
    {
        internal const string FeatureToken = "InsinuatorAmbitiousBond0095";
        internal const string AxiomaticToken = "InsinuatorWeaponBondAxiomatic0095";

        // MicroscopicContentExpansion antipaladin and Tyrant weapon-bond roots.
        private const string AnarchicChoiceId = "ab9bf366-7ab7-44cd-b1bf-d839c97eca2f";
        private const string UnholyChoiceId = "d170e201-431b-44d8-978b-6d05b344075a";
        private const string AxiomaticChoiceId = "3dada3a7-10b8-42bc-bf9e-8871e0b41be9";
        private const string WeaponBondSwitchId = "dd5c37b7-7b95-4a85-bcd1-9206c6efbd83";

        private static readonly AlignmentMaskType Chaos =
            AlignmentMaskType.ChaoticEvil | AlignmentMaskType.ChaoticNeutral;

        private static readonly AlignmentMaskType Law =
            AlignmentMaskType.LawfulEvil | AlignmentMaskType.LawfulNeutral;

        private static readonly AlignmentMaskType Evil =
            AlignmentMaskType.LawfulEvil | AlignmentMaskType.NeutralEvil | AlignmentMaskType.ChaoticEvil;

        internal const string BondText =
            "At 5th level, an insinuator gains a divine bond with either a bonded weapon or an outsider servant. " +
            "This functions as the antipaladin's fiendish boon with one change: the first special ability added to a bonded weapon " +
            "must match the alignment of the outsider invoked for the day - anarchic for a chaotic patron, axiomatic for a lawful one, " +
            "unholy for an evil one. Additional special abilities may be applied if any equivalent bonus remains.\n" +
            "A neutral patron lends no alignment at all, so only the plain enhancement bonus and the unaligned properties are available.\n" +
            "An outsider servant likewise changes with the invocation, taking the alignment of the patron invoked that day.";

        internal const string AxiomaticText =
            "While a lawful outsider is invoked, the bonded weapon can be given the axiomatic property.\n" +
            "The antipaladin's own bond has no lawful property, so an insinuator borrows this one from the Tyrant's diabolic bond.";

        // The invocation feature marks an insinuator; the plain antipaladin and
        // the Tyrant never have it.
        private static BlueprintFeature Insignia;

        // The borrowed lawful property, or null when the Tyrant is absent.
        private static BlueprintActivatableAbility Axiomatic;

        private static readonly List<Action> Undo = new List<Action>();

        internal static bool Bound(UnitEntityData unit)
            => unit != null && Insignia != null && unit.HasFact(Insignia);

        internal static BlueprintFeature Build(PsychicAssets f, BlueprintsCache cache)
        {
            var components = new List<BlueprintComponent>();
            Axiomatic = null;
            var donor = cache.Load(BlueprintGuid.Parse(AxiomaticChoiceId)) as BlueprintActivatableAbility;
            if (donor != null)
            {
                var toggle = f.Clone(donor, AxiomaticToken, "Ambitious Bond — Axiomatic", AxiomaticText);
                var switchAbility = cache.Load(BlueprintGuid.Parse(WeaponBondSwitchId)) as BlueprintUnitFact;
                var restriction = new InvokedAlignmentBondRestriction0095
                {
                    name = "AmbitiousBondAxiomatic",
                    Alignment = Law
                };
                if (switchAbility != null)
                    restriction.RequiresFact = Ref<BlueprintUnitFactReference>(switchAbility);
                toggle.ComponentsArray = toggle.ComponentsArray
                    .Concat(new BlueprintComponent[] { restriction }).ToArray();
                components.Add(Facts(toggle));
                Axiomatic = toggle;
            }
            return f.Feature(FeatureToken, "Ambitious Bond", BondText, components.ToArray());
        }

        // Called from the module's connect step: the gate has to land on the
        // shared toggles only after the module is committed, and every edit is
        // recorded so a failed publication restores the native behaviour.
        internal static void Connect(BlueprintsCache cache, BlueprintFeature invocationFeature)
        {
            Revert();
            Insignia = invocationFeature;
            Gate(cache, AnarchicChoiceId, "Anarchic", Chaos);
            Gate(cache, UnholyChoiceId, "Unholy", Evil);
        }

        internal static void Revert()
        {
            for (int i = Undo.Count - 1; i >= 0; i--) Undo[i]();
            Undo.Clear();
            Insignia = null;
        }

        private static void Gate(BlueprintsCache cache, string id, string label, AlignmentMaskType alignment)
        {
            var choice = cache.Load(BlueprintGuid.Parse(id)) as BlueprintActivatableAbility;
            if (choice == null)
                throw new InvalidOperationException("Ambitious Bond lost its weapon property: " + id + ".");
            var original = choice.ComponentsArray;
            var restriction = new InvokedAlignmentBondRestriction0095
            {
                name = "AmbitiousBond" + label,
                Alignment = alignment
            };
            choice.ComponentsArray = original.Concat(new BlueprintComponent[] { restriction }).ToArray();
            Undo.Add(() => choice.ComponentsArray = original);
        }

        internal static void Verify(BlueprintsCache cache, BlueprintFeature feature, BlueprintFeature invocationFeature)
        {
            if (Insignia != invocationFeature)
                throw new InvalidOperationException("Ambitious Bond lost its insinuator marker.");
            foreach (var id in new[] { AnarchicChoiceId, UnholyChoiceId })
            {
                var choice = cache.Load(BlueprintGuid.Parse(id)) as BlueprintActivatableAbility;
                if (choice == null || !choice.ComponentsArray.OfType<InvokedAlignmentBondRestriction0095>().Any())
                    throw new InvalidOperationException("Ambitious Bond does not gate the weapon property " + id + ".");
            }
            if (Axiomatic != null && (feature == null || feature.GetComponent<AddFacts>() == null))
                throw new InvalidOperationException("Ambitious Bond does not grant the lawful weapon property.");
        }
    }
}
