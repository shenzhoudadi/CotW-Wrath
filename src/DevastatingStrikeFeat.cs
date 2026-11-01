using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class DevastatingStrikeFeat
    {
        internal const string ModuleName = "Devastating Strike";
        internal const string Token = "DevastatingStrikeFeat";
        internal const string VitalStrike = "14a1fc1356df9f146900e1e42142fc9d";
        internal static readonly string[] VitalStrikeAbilities = {
            "efc60c91b8e64f244b95c66b270dbd7c",
            "c714cd636700ac24a91ca3df43326b00",
            "11f971b6453f74d4594c538e3c88d499"
        };

        internal static void Register(BlueprintsCache cache)
        {
            var selections = new[] { BlueprintRegistration.BasicFeatsId }
                .Concat(BlueprintRegistration.NativeCombatFeatSelectionIds)
                .Select(id => cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeatureSelection
                    ?? throw new InvalidOperationException("Devastating Strike selection missing: " + id)).ToArray();
            var vital = cache.Load(BlueprintGuid.Parse(VitalStrike)) as BlueprintFeature
                ?? throw new InvalidOperationException("Devastating Strike requires native Vital Strike.");
            var prerequisite = new PrerequisiteFeature();
            Set(prerequisite, "m_Feature", Ref<BlueprintFeatureReference>(vital));
            var factory = new PsychicAssets(cache);
            var feat = factory.Feature(Token, "Devastating Strike",
                "When you use Vital Strike, Improved Vital Strike or Greater Vital Strike, gain +2 damage for each additional set of weapon damage dice (+2, +4 or +6). This is a weapon damage bonus and is multiplied on a critical hit. Mythic Vital Strike applies its normal native bonus scaling. Additional elemental and precision damage are not increased. Requires base attack bonus +9 and Vital Strike.",
                new PrerequisiteStatValue { Stat = StatType.BaseAttackBonus, Value = 9 },
                prerequisite, new DevastatingStrikeDamageBonus());
            feat.Groups = new[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };
            // Original CotW has no dedicated icon. Reuse the native prerequisite's icon.
            Set(feat, "m_Icon", vital.Icon);
            PsychicRepairComponents.UniqueNames(feat);
            var old = cache.Load(feat.AssetGuid);
            if (old != null && (old.GetType() != typeof(BlueprintFeature) || old.name != feat.name
                || ((BlueprintFeature)old).ComponentsArray == null
                || feat.ComponentsArray.Any(expected => !((BlueprintFeature)old).ComponentsArray.Any(
                    component => component != null && component.GetType() == expected.GetType() && component.name == expected.name))))
                throw new InvalidOperationException("Devastating Strike identity collision; existing asset preserved.");
            var actual = (BlueprintFeature)(old ?? feat);
            var before = selections.Select(s => new[] { Get(s, "m_Features"), Get(s, "m_AllFeatures") }).ToArray();
            factory.Enable();
            bool added = false;
            try
            {
                if (old == null) { cache.AddCachedBlueprint(feat.AssetGuid, feat); added = true; }
                T09CombatFeats.Append(selections, new[] { actual });
            }
            catch
            {
                for (int i = 0; i < selections.Length; i++) {
                    Set(selections[i], "m_Features", before[i][0]);
                    Set(selections[i], "m_AllFeatures", before[i][1]);
                }
                if (added) cache.RemoveCachedBlueprint(feat.AssetGuid);
                throw;
            }
        }

        internal static void Optional(BlueprintsCache cache)
        {
            var selection = cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BrawlerCombatFeatSelectionId)) as BlueprintFeatureSelection;
            var feat = cache.Load(Id(Token)) as BlueprintFeature;
            if (selection != null && feat?.name == "CotWWrathPsychic" + Token
                && feat.GetComponent<DevastatingStrikeDamageBonus>() != null)
                T09CombatFeats.Append(new[] { selection }, new[] { feat });
        }
    }

    [TypeId("a5586e7a9d8c449bb1486af9ccae46f7")]
    public sealed class DevastatingStrikeDamageBonus : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (!Fact.IsActive || !Fact.IsTurnedOn || Fact.IsDisposed || evt.Initiator != Owner) return;
            var attack = evt.AttackWithWeapon;
            var part = Owner.Get<AbilityCustomVitalStrike.VitalStrikePart>();
            var ability = attack?.Reason?.Ability;
            if (part == null || part.Caster != Owner || part.Fact == null || ability?.Blueprint == null
                || !ReferenceEquals(ability.Fact, part.Fact) || attack.Initiator != Owner
                || !ReferenceEquals(evt.Weapon, attack.Weapon) || !attack.IsFirstAttack
                || attack.IsFullAttack || attack.IsAttackOfOpportunity || attack.IsCharge || attack.ExtraAttack
                || attack.AttackBonusPenalty != 0 || attack.AttacksCount > 1) return;
            // The installed game's VitalStrikePart only grants its extra weapon
            // dice to a physical main packet. Nonphysical/no-damage weapons must
            // not gain a feat bonus for dice that the native ability never grants.
            if (evt.Weapon.Blueprint.HasNoDamage || evt.Weapon.Blueprint.DamageType.Type != DamageType.Physical) return;
            int tier = Array.IndexOf(DevastatingStrikeFeat.VitalStrikeAbilities, ability.Blueprint.AssetGuid.ToString());
            var native = ability.Blueprint.GetComponent<AbilityCustomVitalStrike>();
            if (tier < 0 || native == null || native.VitalStrikeMod != tier + 2
                || part.VitalStrikeMod != native.VitalStrikeMod) return;
            // Add before the native weapon-damage packet is built. Native Vital Strike
            // then rolls its extra dice, Mythic adds its own bonus increments, and the
            // final damage calculator applies criticals/DR. No separate damage packet,
            // multiplication rewrite, additional-damage dice or persistent tracker.
            evt.AddDamageModifier(2 * (tier + 1), Fact);
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
    }
}
