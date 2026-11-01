using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class SpiritualGuardian01433
    {
        internal const string ModuleName = "Spiritual Guardian";
        internal const string Token = "SpiritualGuardianFeat";
        internal static readonly BlueprintGuid FeatId = Id(Token);
        static readonly HashSet<BlueprintGuid> GuardianSpells = new HashSet<BlueprintGuid>(
            new[] { "SpiritualWeapon", "SpiritualAlly" }.SelectMany(token =>
                new[] { token }.Concat(Enumerable.Range(0, 5).Select(form => token + "Form" + form))).Select(Id));
        internal static bool Has(UnitEntityData caster) => caster?.Facts?.List.Any(f =>
            f.Blueprint.AssetGuid == FeatId && f.IsTurnedOn) == true;
        internal static bool Applies(UnitEntityData caster, int kind) => (kind == 0 || kind == 1) && Has(caster);

        // These are the two original CotW guardian spells and their five
        // authored weapon-form leaves. A matching name/parent on a foreign
        // spell, a real summon, Mage's Sword or Twilight Knife is insufficient.
        internal static bool GuardianSpell(BlueprintAbility spell)
            => spell != null && GuardianSpells.Contains(spell.AssetGuid);

        internal static void Register(BlueprintsCache cache)
        {
            var f = new PsychicAssets(cache);
            var pool = f.Native<BlueprintFeatureSelection>(BlueprintRegistration.BasicFeatsId);
            for (int kind = 0; kind < 2; kind++) {
                string token = kind == 0 ? "SpiritualWeapon" : "SpiritualAlly";
                var spell = cache.Load(Id(token)) as BlueprintAbility;
                var actor = cache.Load(Id(token + "ActorBuff")) as BlueprintBuff;
                var forms = spell?.GetComponents<AbilityVariants>().ToArray();
                if (spell?.name != "CotWWrathPsychic" + token || actor?.name != "CotWWrathPsychic" + token + "ActorBuff" ||
                    actor.GetComponent<PsychicSpiritAttackStats>()?.Kind != kind || forms?.Length != 1 ||
                    !forms[0].Variants.Select(a => a?.AssetGuid).SequenceEqual(Enumerable.Range(0, 5).Select(i => (BlueprintGuid?)Id(token + "Form" + i))))
                    throw new InvalidOperationException("Spiritual Guardian requires the registered " + token + " guardian graph.");
                for (int form = 0; form < 5; form++)
                    if (!(cache.Load(Id(token + "Form" + form)) is BlueprintAbility leaf) ||
                        leaf.name != "CotWWrathPsychic" + token + "Form" + form || leaf.Parent?.AssetGuid != spell.AssetGuid)
                        throw new InvalidOperationException("Spiritual Guardian guardian form is missing or foreign: " + token + "Form" + form);
            }
            var feat = f.Feature(Token, ModuleName,
                "Your Spiritual Weapon and Spiritual Ally use their caster level in place of your base attack bonus, potentially granting additional iterative attacks. They gain +2 on damage rolls and caster level checks to overcome spell resistance. These benefits apply only to your own castings of those two guardian spells, including their weapon-form choices. Mage's Sword, Twilight Knife and other summoned creatures are unaffected. The attack base is fixed when the guardian is cast. This feat has no class prerequisite and can be selected once.",
                new SpiritualGuardianPenetration01433());
            feat.Groups = new[] { FeatureGroup.Feat };
            feat.IsClassFeature = false;
            Set(feat, "m_Icon", PsychicOriginalIcons.Load("SpiritualAlly"));
            var not = new PrerequisiteNoFeature();
            Set(not, "m_Feature", Ref<BlueprintFeatureReference>(feat));
            feat.ComponentsArray = feat.ComponentsArray.Concat(new BlueprintComponent[] { not }).ToArray();
            if (cache.Load(feat.AssetGuid) is BlueprintFeature existing &&
                (existing.Ranks != 1 || existing.IsClassFeature || existing.HideInUI || existing.HideInCharacterSheetAndLevelUp ||
                 existing.Groups == null || !existing.Groups.SequenceEqual(new[] { FeatureGroup.Feat })))
                throw new InvalidOperationException("Spiritual Guardian feature metadata collision; preserved.");
            Migration01426.Commit(f, new[] { pool }, feat);
        }
    }

    // Native first-cast SR, controller redirection and Spiritual Ally AoOs all
    // trigger this same native rule on the caster. One local subscriber covers
    // all three paths; adding a second bonus in either controller would double it.
    [TypeId("97f90f88894848cdb23ba8d5c563c2d9")]
    public sealed class SpiritualGuardianPenetration01433 : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleSpellResistanceCheck>, ISubscriber
    {
        public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
        {
            if (evt.Initiator != Owner || evt.Context?.MaybeCaster != Owner ||
                !SpiritualGuardian01433.GuardianSpell(evt.Ability)) return;
            // Guard even against duplicate active facts supplied externally.
            if (Owner.Facts.List.FirstOrDefault(f => f.Blueprint.AssetGuid == SpiritualGuardian01433.FeatId && f.IsTurnedOn) != Fact) return;
            evt.AddSpellPenetration(2, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }
    }
}
