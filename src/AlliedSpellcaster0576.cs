// COTW-0576 Allied Spellcaster: same-spell caster level and penetration bonus.
//
// Ported from Call of the Wild (Kingmaker), Rebalance.fixTeamworkFeats and
// NewMechanics/TeamworkMechanics.cs AlliedSpellcasterSameSpellBonus.
//
// What the tabletop feat actually says
// ------------------------------------
//   Whenever you are adjacent to an ally who also has this feat, you receive a
//   +2 bonus on concentration checks and level checks made to overcome spell
//   resistance. If your ally has the same spell prepared (or known with a slot
//   available, if they are a spontaneous spellcaster), this bonus increases to
//   +4 and you receive a +1 bonus to caster level for all level-dependent
//   variables.
//
// What Wrath ships
// ----------------
// Kingmaker.Designers.Mechanics.Facts.AlliedSpellcaster already grants the +2
// concentration and +2 spell penetration, but unconditionally: it never asks
// whether the ally has the same spell, and it never grants a caster level. Its
// description matches that - "+2 bonus on concentration checks and level checks
// made to overcome spell resistance" - so the same-spell clause is genuinely
// missing rather than merely undescribed.
//
// What this component adds
// ------------------------
// Exactly the missing clause, layered on top of the base one, the way CotW did
// it: when an adjacent ally has the feat (or the owner has Solo Tactics) *and*
// that ally can actually cast the spell right now, add +1 caster level,
// +2 concentration and +2 spell penetration. Base +2 plus these +2 is the +4
// the feat promises.
//
// The base component is not required. Owlcat's AlliedSpellcaster can be gone:
// TabletopTweaks-Base removes it and installs its own AlliedSpellcasterTTT
// replacement with the same effects, and only the radius unit differs. The port
// reads whichever one is present and falls back to the feat's own 5 ft when
// neither is.
//
// The +1 caster level also lifts the spell resistance check by one, because
// RuleSpellResistanceCheck computes its total from Context.Params.CasterLevel.
// That is the accepted reading of the rules, not an accident of this port.
//
// One deliberate difference from the original. CotW registered the spell
// resistance half as a global handler without checking who initiated the
// check, so any unit carrying the feat handed +2 penetration to unrelated
// third parties casting in its vicinity. The concentration half did check the
// initiator. This port checks it on both, which is what the feat text
// describes and what the native component already does.
//
// Radius is taken from the native component rather than hardcoded. Wrath reads
// that field as feet and ships 5, which is exactly "adjacent"; CotW's 2 was
// metres for a Kingmaker component that had no unit conversion, so copying that
// number here would widen the feat to 6.6 feet.
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("2c0ea9dc715828b29b5e8684d1f268a7")]
    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [AllowMultipleComponents]
    public sealed class AlliedSpellcasterSameSpell0576 : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>,
        IGlobalRulebookHandler<RuleSpellResistanceCheck>
    {
        public BlueprintUnitFact AlliedSpellcasterFact;
        // Metres, already converted from whichever base component supplied it.
        public float RadiusMeters;

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            try
            {
                if (evt.Initiator != Owner) return;
                if (!HasSameSpellAlly(evt.Spell)) return;
                evt.AddBonusCasterLevel(1);
                evt.AddBonusConcentration(2);
            }
            catch (Exception exception) { AlliedSpellcaster0576.Report(exception); }
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
        {
            try
            {
                if (evt.Initiator != Owner) return;
                if (!HasSameSpellAlly(evt.Ability)) return;
                evt.AddSpellPenetration(2);
            }
            catch (Exception exception) { AlliedSpellcaster0576.Report(exception); }
        }

        public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }

        // Mirrors the native scan: adjacent, not the owner, not an enemy, and
        // carrying the feat - or the owner having Solo Tactics, which is how a
        // lone inquisitor keeps teamwork feats working.
        private bool HasSameSpellAlly(BlueprintAbility spell)
        {
            if (spell == null || Owner == null) return false;
            var soloTactics = Owner.State.Features.SoloTactics;
            foreach (var ally in GameHelper.GetTargetsAround(Owner.Position, RadiusMeters, true, false))
            {
                if (ally == null || ally == Owner) continue;
                if (ally.IsEnemy(Owner)) continue;
                if (!soloTactics && !ally.Descriptor.HasFact(AlliedSpellcasterFact)) continue;
                if (AlliedSpellcaster0576.Available(ally.Descriptor, spell)) return true;
            }
            return false;
        }
    }

    internal static class AlliedSpellcaster0576
    {
        internal const string ModuleName = "Allied Spellcaster same-spell bonus";
        // Native Feats/Teamwork/AlliedSpellcaster.jbp, unchanged in Wrath.
        internal const string FeatureId = "9093ceeefe9b84746a5993d619d7c86f";
        internal static string LastSummary = "Not registered";

        internal static void Register(BlueprintsCache cache)
        {
            var feature = cache.Load(BlueprintGuid.Parse(FeatureId)) as BlueprintFeature;
            if (feature == null) throw new InvalidOperationException("Allied Spellcaster blueprint missing: " + FeatureId);
            if (feature.GetComponent<AlliedSpellcasterSameSpell0576>() != null)
            {
                LastSummary = "Already registered";
                return;
            }
            // The base +2 lives on whichever component the load order left in
            // place, and it is not required: TabletopTweaks-Base removes
            // Owlcat's AlliedSpellcaster and installs its own AlliedSpellcasterTTT
            // replacement, so demanding the original would fail on this machine
            // and, worse, would disable the same-spell clause for every player
            // who runs that mod.
            var radius = BaseRadiusMeters(feature);
            var baseMissing = radius <= 0f;
            // Not fatal: a third mod may have replaced the base bonus entirely.
            // The same-spell clause is still the right thing to add, and the
            // summary below says what was assumed.
            if (baseMissing) radius = 5.Feet().Meters;
            // The repo-wide convention for runtime component injection: rebuild
            // the array instead of calling an AddComponent helper that the
            // blueprint base class does not expose.
            feature.ComponentsArray = feature.ComponentsArray
                .Concat(new BlueprintComponent[]
                {
                    new AlliedSpellcasterSameSpell0576
                    {
                        AlliedSpellcasterFact = feature,
                        RadiusMeters = radius
                    }
                })
                .ToArray();
            LastSummary = "Registered: +1 caster level, +2 concentration and +2 spell penetration when an adjacent ally can cast the same spell (radius " +
                radius.ToString("0.##") + " m" + (baseMissing ? "; no base component found, assumed the feat's own 5 ft" : "") + ").";
            ApplyDescription();
        }

        // Owlcat's component stores the radius in feet and converts on read
        // (`Radius.Feet().Meters`); the TabletopTweaks replacement passes the
        // field straight to GetTargetsAround, so its 5 already means metres.
        // Matching the wrong unit would make the same-spell clause demand a
        // tighter adjacency than the base +2 it is meant to extend.
        private static float BaseRadiusMeters(BlueprintFeature feature)
        {
            foreach (var component in feature.ComponentsArray ?? Array.Empty<BlueprintComponent>())
            {
                if (component == null) continue;
                var type = component.GetType();
                var name = type.Name;
                if (name != "AlliedSpellcaster" && name != "AlliedSpellcasterTTT") continue;
                var field = HarmonyLib.AccessTools.Field(type, "Radius");
                if (field == null) continue;
                int value;
                try { value = Convert.ToInt32(field.GetValue(component)); }
                catch (Exception) { continue; }
                if (value <= 0) continue;
                return name == "AlliedSpellcasterTTT" ? value : value.Feet().Meters;
            }
            return 0f;
        }

        // The native description stops at the +2 clause, so it would now
        // understate what the feat does. The blueprint keeps its own key; only
        // the text behind that key changes.
        //
        // This goes through QlTexts01460.Resolve rather than its Apply sweep,
        // because Apply only touches keys it can find in the mod's authored
        // tables and this one belongs to the base game. Reading the row
        // directly keeps the description editable in the same QL file as
        // everything else.
        private const string DescriptionKey = "7753e2e5-5b3c-4e73-a468-78fbcba4c4ec";

        private const string DefaultDescription =
            "With the aid of an ally, you are skilled at piercing the protections of other creatures with your {g|Encyclopedia:Spell}spells{/g}.\n" +
            "Benefit: Whenever you are adjacent to an ally who also has this {g|Encyclopedia:Feat}feat{/g}, you receive a +2 {g|Encyclopedia:Bonus}bonus{/g} on {g|Encyclopedia:Concentration}concentration{/g} {g|Encyclopedia:Check}checks{/g} and level checks made to overcome {g|Encyclopedia:Spell_Resistance}spell resistance{/g}.\n" +
            "If your ally has the same spell prepared, or knows it and still has a spell slot available to cast it, this bonus increases to +4 and you receive a +1 bonus to caster level for all level-dependent variables.";

        private static void ApplyDescription()
        {
            try
            {
                var pack = Kingmaker.Localization.LocalizationManager.CurrentPack;
                if (pack == null) return;
                var text = QlTexts01460.Resolve(DescriptionKey, DefaultDescription);
                if (string.IsNullOrEmpty(text)) return;
                // PutString does not take the pack lock itself.
                if (pack.GetText(DescriptionKey, false) == text) return;
                lock (PsychicAssets.Get(pack, "m_Lock")) pack.PutString(DescriptionKey, text);
            }
            catch (Exception exception) { Report(exception); }
        }

        internal static void Report(Exception exception) => ReportOnce(exception?.ToString() ?? "unknown");

        internal static void Report(string message) => ReportOnce(message);

        private static readonly System.Collections.Generic.HashSet<string> Reported =
            new System.Collections.Generic.HashSet<string>();

        private static void ReportOnce(string message)
        {
            try
            {
                if (message == null || !Reported.Add(message)) return;
                Console.WriteLine("[CotW-AlliedSpellcaster] " + message);
            }
            catch (Exception) { }
        }

        // "Same spell available" means the ally can actually spend a slot on it
        // right now, not merely that the blueprint sits in some spell list.
        // Wrath has no SpellDuplicates, so variant spells need the parent and
        // child links checked too: a caster who memorised one form of
        // Protection from Energy does have that spell available, and the parent
        // blueprint alone would not match.
        internal static bool Available(UnitDescriptor caster, BlueprintAbility spell)
        {
            if (caster?.Spellbooks == null || spell == null) return false;
            foreach (var book in caster.Spellbooks)
            {
                if (book == null) continue;
                if (CanSpend(book, spell)) return true;
                var variants = spell.AbilityVariants;
                if (variants != null)
                {
                    // Variants is a ReferenceArrayProxy, a struct that is never
                    // null and resolves each entry on index.
                    var children = variants.Variants;
                    for (var index = 0; index < children.Length; index++)
                        if (CanSpend(book, children[index])) return true;
                }
                if (CanSpend(book, spell.Parent)) return true;
            }
            return false;
        }

        private static bool CanSpend(Spellbook book, BlueprintAbility spell)
        {
            if (book == null || spell == null) return false;
            try { return book.CanSpend(spell); }
            catch (Exception exception) { Report(exception); return false; }
        }
    }
}
