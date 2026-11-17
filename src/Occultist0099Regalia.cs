using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Regalia
    {
        private const string ShieldOfFaith = "183d5bb91dea3a1489a6db6c9cb64445";
        private const string Restoration = "f2115ac1148256b4ba20788f7e966830";
        private const string Undead = Occultist0099Necromancy.Undead;
        private const string Construct = "fd389783027d63343b4a5634bd81645f";
        internal static readonly string[] Donors = { ShieldOfFaith, Restoration, Undead, Construct };
        internal static readonly string[] Tokens = { "FontOfHealingValue", Occultist0099Panoplies.RegaliaToken + "ResonanceBuff",
            Occultist0099Panoplies.RegaliaToken + "Source", "GuardianAuraEffect", "GuardianAuraArea", "GuardianAuraBuff", "GuardianAuraAbility", "GuardianAuraPower",
            "RestoringTouchDamageAbility", "RestoringTouchDrainAbility", "RestoringTouchAbility" };

        private static readonly string[] CureRoots =
        {
            "5590652e1c2225c4ca30c4a699ab3649", "6b90c773a6543dc49b2505858ce33db5", "3361c5df793b4c8448756146a88026ad",
            "41c9016596fe1de4faf67425ed691203", "5d3d689392e4ff740a761ef346815074", "571221cc141bc21449ae96b3944652aa"
        };

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text,
            BlueprintUnitProperty level, BlueprintUnitProperty dc)
        {
            var shield = Occultist0099Illusion.Load<BlueprintAbility>(cache, ShieldOfFaith);
            var restoration = Occultist0099Illusion.Load<BlueprintAbility>(cache, Restoration);
            var undead = Occultist0099Illusion.Load<BlueprintFeature>(cache, Undead);
            var construct = Occultist0099Illusion.Load<BlueprintFeature>(cache, Construct);
            var cures = CureRoots.Select(id => Occultist0099Illusion.Load<BlueprintAbility>(cache, id)).ToArray();
            if (shield == null || restoration == null || undead == null || construct == null || cures.Any(cure => cure == null))
                return null;

            const string fontText = "Your occultist cure spells restore 1 additional hit point per 2 points of mental focus locked in Abjuration and Conjuration, up to half your occultist level.";
            var value = Occultist0099Panoplies.ResonanceValue("FontOfHealingValue", Occultist0099Panoplies.Regalia, 2, true);
            var fontRank = new ContextRankConfig { name = "FontOfHealingRank" };
            SetEnum(fontRank, "m_BaseValueType", "CustomProperty");
            Set(fontRank, "m_CustomProperty", Ref<BlueprintUnitPropertyReference>(value));
            var font = Buff(Occultist0099Panoplies.RegaliaToken + "ResonanceBuff", "Font of Healing", fontText, restoration, fontRank,
                new Occultist0099FontOfHealing { name = "FontOfHealing", Undead = Ref<BlueprintUnitFactReference>(undead),
                    CureSpells = cures.SelectMany(cure => new[] { cure, cure.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility })
                        .Where(cure => cure != null).Distinct().Select(Ref<BlueprintAbilityReference>).ToArray() });
            SetEnum(font, "m_Flags", "StayOnDeath");

            const string auraText = "Spend 3 points of mental focus as a full-round action to project a 20-foot aura for 1 minute. Allies in the aura gain a sacred AC bonus equal to one-quarter of your occultist level, minimum +1. Your speed is halved while the aura persists.";
            var ward = Buff("GuardianAuraEffect", "Guardian Aura", auraText, shield, Scale("GuardianSacredArmor", 4),
                new AddContextStatBonus { name = "GuardianAuraSacredArmor", Stat = StatType.AC, Descriptor = ModifierDescriptor.Sacred, Value = Rank() });
            var areaEffect = new AbilityAreaEffectBuff { CheckConditionEveryRound = true,
                Condition = new ConditionsChecker { Conditions = new Condition[] { new ContextConditionIsAlly() } } };
            Set(areaEffect, "m_Buff", Ref<BlueprintBuffReference>(ward));
            var area = Identify(new BlueprintAbilityAreaEffect
            {
                Fx = new PrefabLink { AssetId = "bbd6decdae32bce41ae8f06c6c5eb893" }, Size = new Feet(20),
                Shape = AreaEffectShape.Cylinder, ComponentsArray = new BlueprintComponent[] { areaEffect }
            }, "GuardianAuraArea", "Guardian Aura", auraText, shield, text);
            SetEnum(area, "m_TargetType", "Ally");
            var emit = new AddAreaEffect { name = "GuardianAuraEmitter" };
            Set(emit, "m_AreaEffect", Ref<BlueprintAbilityAreaEffectReference>(area));
            var host = Buff("GuardianAuraBuff", "Guardian Aura", auraText, shield, emit,
                new AddCondition { name = "GuardianAuraSlowsCaster", Condition = UnitCondition.Slowed });
            var guardian = Power("GuardianAuraAbility", "Guardian Aura", auraText, shield, 3, AbilityRange.Personal,
                Run(Apply(host, 1, DurationRate.Minutes, toCaster: true)));
            guardian.SetIsFullRoundAction(true);

            const string restoringText = "Touch a living creature and restore 1 point of ability damage per 3 occultist levels, minimum 1, for 2 points of mental focus. At level 10 you can spend 3 points to restore ability drain instead.";
            var damage = Restore("RestoringTouchDamageAbility", "Restoring Touch (Ability Damage)", 2, false);
            var drain = Restore("RestoringTouchDrainAbility", "Restoring Touch (Ability Drain)", 3, true);
            var restore = Occultist0099Illusion.Variants("RestoringTouchAbility", "Restoring Touch", restoringText, restoration,
                text, SpellSchool.Conjuration, damage, drain);
            restore.Range = AbilityRange.Touch;
            var source = Occultist0099Panoplies.Carrier(Occultist0099Panoplies.Regalia, Occultist0099Panoplies.RegaliaToken,
                fontText, shield, text, restore);
            return new BlueprintScriptableObject[] { value, font, ward, area, host, guardian, source, damage, drain, restore };

            BlueprintAbility Restore(string token, string label, int cost, bool drainDamage)
            {
                var living = new AbilityTargetHasFact { name = "RestoringTouchLiving", Inverted = true };
                Set(living, "m_CheckedFacts", new[] { Ref<BlueprintUnitFactReference>(undead), Ref<BlueprintUnitFactReference>(construct) });
                var ability = Power(token, label, restoringText, restoration, cost, AbilityRange.Touch, living,
                    Scale("RestoringTouchAmount", 3), Run(new ContextActionHealStatDamage { HealDrain = drainDamage,
                        Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = Rank() } }));
                ability.CanTargetFriends = true;
                if (drainDamage)
                {
                    ability.ComponentsArray = ability.ComponentsArray.Append(new Occultist0099PowerLevel
                        { name = "RestoringTouchDrainLevel", Minimum = 10 }).ToArray();
                    ability.GetComponent<Occultist0099FocusResourceLogic>().CheckCasterRestrictions = true;
                }
                return ability;
            }

            BlueprintAbility Power(string token, string label, string description, BlueprintAbility icon, int cost, AbilityRange range,
                params BlueprintComponent[] components)
            {
                var ability = Occultist0099Panoplies.CreatePower(Occultist0099Panoplies.Regalia, token, label, description, icon,
                    text, cost, AbilityType.SpellLike, range, SpellSchool.Conjuration, level, dc, components);
                ability.NotOffensive = true;
                ability.EffectOnAlly = AbilityEffectOnUnit.Helpful;
                return ability;
            }

            BlueprintBuff Buff(string token, string label, string description, BlueprintAbility icon, params BlueprintComponent[] components)
                => Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Replace, ComponentsArray = components },
                    token, label, description, icon, text);
        }

        private static ContextRankConfig Scale(string name, int step)
        {
            var rank = new ContextRankConfig { name = name };
            SetEnum(rank, "m_BaseValueType", "CasterLevel");
            SetEnum(rank, "m_Progression", "DivStep");
            Set(rank, "m_StepLevel", step);
            Set(rank, "m_UseMin", true);
            Set(rank, "m_Min", 1);
            return rank;
        }

        private static ContextValue Rank() => new ContextValue { ValueType = ContextValueType.Rank };
    }

    [TypeId(Occultist0099Ids.FontOfHealingTypeId)]
    public sealed class Occultist0099FontOfHealing : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleHealDamage>
    {
        public BlueprintAbilityReference[] CureSpells;
        public BlueprintUnitFactReference Undead;

        public void OnEventAboutToTrigger(RuleHealDamage evt)
        {
            var ability = evt.Reason?.Context?.SourceAbilityContext?.Ability;
            if (ability?.Caster?.Unit != Owner || ability.SourceItem != null || !Occultist0099NativeScope.IsPrivateBook(ability.Spellbook) ||
                evt.Target == null || evt.Target.Descriptor.HasFact(Undead.Get()) ||
                !CureSpells.Any(reference => reference.Guid == ability.Blueprint.AssetGuid)) return;
            evt.AdditionalBonus.Add(ModifierDescriptor.UntypedStackable, Context[AbilityRankType.Default]);
        }

        public void OnEventDidTrigger(RuleHealDamage evt) { }
    }
}
