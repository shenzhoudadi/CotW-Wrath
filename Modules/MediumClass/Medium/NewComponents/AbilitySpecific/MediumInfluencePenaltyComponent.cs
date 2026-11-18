using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;


namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    [TypeId("4c5cd1ca-7eed-4a21-aa7d-2e79d5953009")]
    class MediumInfluencePenaltyComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleSkillCheck>, IRulebookHandler<RuleSkillCheck>,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
        IInitiatorRulebookHandler<RuleCalculateDamage>, IRulebookHandler<RuleCalculateDamage>,
        ISubscriber, IInitiatorRulebookSubscriber
    {
        public override void OnTurnOn() => RefreshModifiers();

        internal void RefreshModifiers()
        {
            RemoveOwnModifiers();
            var medium = Owner.Get<UnitPartMedium>();
            if (medium == null) return;
            // Only formally selected spirits divide influence penalties. A
            // temporary Trance or Beacon-granted power is not another channel.
            int formalCount = medium.ActiveSpiritClasses.Count();
            bool mitigate = Owner.HasFact(BlueprintTool.Get<BlueprintFeature>(MythicSpirits.MultipleSpiritsGuid));
            var classes = isSecondaryCheck
                ? new[] { medium.SecondarySpirit }
                : medium.ActiveSpiritClasses.ToArray();
            var values = new Dictionary<StatType, int>();
            foreach (var spirit in classes.Where(s => s != null && medium.Spirits.ContainsKey(s)))
            {
                var entry = medium.Spirits[spirit];
                // A legendary spirit's penalty tracks the same shared focus its bonus uses.
                int bonus = SpiritBonusValue.ForSpirit(Owner, spirit);
                foreach (var stat in entry.SpiritPenalty.Stats ?? Array.Empty<StatType>())
                    values[stat] = Math.Max(values.TryGetValue(stat, out var prior) ? prior : 0, bonus);
                if (spirit.Get() == BlueprintTool.Get<BlueprintCharacterClass>(Guids.Marshal)
                    && !Owner.HasFact(BuffRefs.FightDefensivelyBuff.Reference.Get()))
                    (Fact as Buff)?.StoreFact(Owner.Buffs.AddBuff(BuffRefs.FightDefensivelyBuff.Reference.Get(), Context, new TimeSpan(24, 0, 0)));
            }
            ApplyPenalty(StatType.Initiative, 2, formalCount, mitigate);
            Owner.Stats.GetStat(StatType.SaveWill)?.AddModifier(2, Runtime, ModifierDescriptor.UntypedStackable);
            foreach (var value in values)
                ApplyPenalty(value.Key, value.Value, formalCount, mitigate);
        }

        private void ApplyPenalty(StatType stat, int magnitude, int formalCount, bool mitigate)
        {
            int reduced = InfluencePenaltyMath.Magnitude(magnitude, formalCount, mitigate);
            if (reduced > 0)
                Owner.Stats.GetStat(stat)?.AddModifier(-reduced, Runtime, ModifierDescriptor.Penalty);
        }

        public override void OnTurnOff()
        {
            RemoveOwnModifiers();


        }

        private void RemoveOwnModifiers()
        {
            // Full catalogue cleanup also covers restored buffs before their
            // source spirit has finished deserializing.
            foreach (var component in BlueprintTool.Get<BlueprintFeature>(Guids.MediumChannelSpirit).GetComponents<MediumSpiritComponent>())
                foreach (var stat in component.Penalties ?? Array.Empty<StatType>())
                    Owner.Stats.GetStat(stat)?.RemoveModifiersFrom(Runtime);
            Owner.Stats.GetStat(StatType.Initiative)?.RemoveModifiersFrom(Runtime);
            Owner.Stats.GetStat(StatType.SaveWill)?.RemoveModifiersFrom(Runtime);
        }


        private int ChampionSpellPenalty()
        {
            var medium = Owner?.Get<UnitPartMedium>();
            if (medium == null) return 0;
            int formalCount = medium.ActiveSpiritClasses.Count();
            if (formalCount == 0) return 0;
            bool mitigate = Owner.HasFact(BlueprintTool.Get<BlueprintFeature>(MythicSpirits.MultipleSpiritsGuid));

            int maxBonus = 0;
            foreach (var id in new[] { Guids.Champion, Guids.Adriel })
            {
                var spirit = BlueprintTool.GetRef<BlueprintCharacterClassReference>(id);
                if (!medium.IsActiveSpirit(spirit) || !medium.Spirits.TryGetValue(spirit, out var entry)) continue;
                int bonus = SpiritBonusValue.ForSpirit(Owner, spirit);
                if (bonus > maxBonus) maxBonus = bonus;
            }
            if (maxBonus <= 0) return 0;
            return InfluencePenaltyMath.Magnitude(maxBonus, formalCount, mitigate);
        }

        public void OnEventAboutToTrigger(RuleSkillCheck evt)
        {
            if (evt.StatType != StatType.Intelligence) return;
            int penalty = ChampionSpellPenalty();
            if (penalty > 0)
                evt.Bonus.AddModifier(-penalty, Fact, ModifierDescriptor.Penalty);
        }

        public void OnEventDidTrigger(RuleSkillCheck evt) { }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt?.AbilityData?.Blueprint == null || !evt.AbilityData.Blueprint.IsSpell) return;
            int penalty = ChampionSpellPenalty();
            if (penalty > 0)
                evt.AddBonusDC(-penalty, ModifierDescriptor.Penalty);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt?.Weapon == null) return;
            var cat = evt.Weapon.Blueprint.Category;
            if (cat != WeaponCategory.Touch && cat != WeaponCategory.Ray) return;
            int penalty = ChampionSpellPenalty();
            if (penalty > 0)
                evt.AddModifier(-penalty, Fact, ModifierDescriptor.Penalty);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var spell = evt?.ParentRule?.SourceAbility;
            if (spell == null || !spell.IsSpell) return;
            int penalty = ChampionSpellPenalty();
            if (penalty <= 0) return;
            foreach (var damage in evt.DamageBundle)
            {
                if (damage == null || damage.Precision || damage.IgnoreModifiers) continue;
                damage.AddModifier(-penalty, Fact);
                return;
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt) { }

        public bool isSecondaryCheck = false; // never set by production code
    }
}

