using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MediumClass.Prowler;
using MediumClass.Utilities;
using MediumClass.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    /// <summary>
    /// Inspiring Spirit: allies gain a morale bonus equal to the Medium's spirit bonus
    /// on saving throws against charm, compulsion, and fear effects.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("c4d8f219-4a12-4b67-9e30-1f8a2b3c4d5e")]
    public class InspiringSpiritSaveBonusComponent : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleSavingThrow>, IRulebookHandler<RuleSavingThrow>,
        ISubscriber, IInitiatorRulebookSubscriber
    {
        private const SpellDescriptor InspiringMask =
            SpellDescriptor.Charm | SpellDescriptor.Compulsion | SpellDescriptor.Fear;

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var descriptor = evt.Reason?.Context?.SpellDescriptor ?? SpellDescriptor.None;
            if ((descriptor & InspiringMask) == 0) return;

            var caster = Buff?.Context?.MaybeCaster;
            int bonus = SpiritBonusValue.Of(caster);
            if (bonus > 0)
                evt.AddModifier(bonus, Fact, ModifierDescriptor.Morale);
        }

        public void OnEventDidTrigger(RuleSavingThrow evt) { }
    }

    /// <summary>
    /// Legendary Skill: grants extra skill ranks equal to Medium level (capped at character level).
    /// Uses UntypedStackable so it stacks independently with Trickster's Edge.
    /// </summary>
    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [TypeId("d5e9a320-5b23-4c78-af41-209b3c4d5e6f")]
    public class AddLegendarySkillRank : UnitFactComponentDelegate, IUnitReapplyFeaturesOnLevelUpHandler
    {
        public StatType Skill;

        public override void OnTurnOn() => Refresh();

        public void HandleUnitReapplyFeaturesOnLevelUp() => Refresh();

        private void Refresh()
        {
            if (!Skill.IsSkill()) return;
            var stat = Owner?.Stats?.GetStat(Skill);
            if (stat == null) return;
            stat.RemoveModifiersFrom(Runtime);

            int medLevel = ProwlerSpiritRules.SpiritClassLevel(Owner);
            int charLevel = Owner.Progression?.CharacterLevel ?? medLevel;
            int baseRanks = stat.BaseValue;
            int extra = Math.Max(0, Math.Min(charLevel, baseRanks + medLevel) - baseRanks);
            if (extra > 0)
                stat.AddModifier(extra, Runtime, ModifierDescriptor.UntypedStackable);
        }

        public override void OnTurnOff()
        {
            Owner?.Stats?.GetStat(Skill)?.RemoveModifiersFrom(Runtime);
        }
    }

    /// <summary>
    /// Resets Legendary Skill buffs and restores its 1/channel resource when channeling starts or ends.
    /// </summary>
    [TypeId("e6fab431-6c34-4d89-b052-31ac4d5e6f70")]
    public class ContextActionResetLegendarySkill : ContextAction
    {
        public override string GetCaption() => "Reset Legendary Skill selection and resource.";

        public override void RunAction()
        {
            var unit = Context?.MaybeCaster;
            if (unit == null) return;
            foreach (var skill in StatTypeHelper.Skills)
            {
                var buff = BlueprintTool.Get<BlueprintBuff>(VariantId.Of("LegendarySkillBuff", skill.ToString()));
                if (buff != null) unit.Buffs.RemoveFact(buff);
            }
            var resource = BlueprintTool.Get<BlueprintAbilityResource>(Guids.MediumSpiritAffinityLegendarySkillResource);
            if (resource != null)
                unit.Descriptor.Resources.Restore(resource, 1);
        }
    }

    internal static class ForcefulMetamagicHelper
    {
        internal static readonly Metamagic[] SupportedMetamagics =
        {
            Metamagic.Empower,
            Metamagic.Maximize,
            Metamagic.Quicken,
            Metamagic.Extend,
            Metamagic.Reach,
            Metamagic.Persistent,
            Metamagic.Selective,
            Metamagic.Bolstered,
            Metamagic.Piercing,
            Metamagic.Intensified,
        };

        internal static int AdjustedCost(UnitDescriptor unit, Metamagic metamagic)
        {
            int cost = metamagic.DefaultCost();
            if (unit?.State?.Features == null) return Math.Max(0, cost);
            var f = unit.State.Features;
            bool favorite = metamagic switch
            {
                Metamagic.Empower => f.FavoriteMetamagicEmpower,
                Metamagic.Maximize => f.FavoriteMetamagicMaximize,
                Metamagic.Extend => f.FavoriteMetamagicExtend,
                Metamagic.Reach => f.FavoriteMetamagicReach,
                Metamagic.Quicken => f.FavoriteMetamagicQuicken,
                Metamagic.Selective => f.FavoriteMetamagicSelective,
                Metamagic.Bolstered => f.FavoriteMetamagicBolstered,
                Metamagic.Piercing => f.FavoriteMetamagicPiercing,
                Metamagic.Intensified => f.FavoriteMetamagicIntensified,
                Metamagic.Persistent => f.FavoriteMetamagicPersistent,
                _ => false
            };
            if (favorite) cost--;
            return Math.Max(0, cost);
        }

        internal static bool HasMetamagicFeat(UnitDescriptor unit, Metamagic metamagic)
        {
            if (unit?.Progression?.Features == null) return false;
            foreach (var feat in unit.Progression.Features.Enumerable)
            {
                if (feat?.Blueprint == null) continue;
                foreach (var comp in feat.Blueprint.GetComponents<AddMetamagicFeat>())
                {
                    if (comp.Metamagic == metamagic) return true;
                }
            }
            return false;
        }
    }

    [AllowedOn(typeof(BlueprintAbility), false)]
    [TypeId("f70bc542-7d45-4e9a-c163-42bd5e6f7081")]
    public class AbilityShowIfHasMetamagic : BlueprintComponent, IAbilityVisibilityProvider
    {
        public Metamagic Metamagic;

        public bool IsAbilityVisible(AbilityData ability) =>
            ForcefulMetamagicHelper.HasMetamagicFeat(ability?.Caster, Metamagic);
    }

    [AllowedOn(typeof(BlueprintAbility), false)]
    [TypeId("081cd653-8e56-4fab-d274-53ce6f708192")]
    public class ForcefulMetamagicRestriction : BlueprintComponent, IAbilityRestriction
    {
        public Metamagic Metamagic;

        public string GetAbilityRestrictionUIText() =>
            LocalizationText.Get(
                "Requires enough influence capacity for the metamagic adjustment.",
                "剩余共鸣上限不足以承受该超魔专长的法术环级调整。");

        public bool IsAbilityRestrictionPassed(AbilityData ability)
        {
            var caster = ability?.Caster;
            if (caster == null) return false;
            int cost = ForcefulMetamagicHelper.AdjustedCost(caster, Metamagic);
            return MediumInfluenceRules.CanAccept(caster, cost);
        }
    }

    /// <summary>
    /// Forceful Metamagic buff: applies the selected metamagic to the next Medium psychic spell
    /// without increasing spell slot or casting time, then accepts influence equal to the
    /// metamagic's normal level adjustment and removes itself.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("192de764-9f67-40bc-e385-64df708192a3")]
    public class ForcefulMetamagicBuffComponent : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>,
        ISubscriber, IInitiatorRulebookSubscriber
    {
        public Metamagic Metamagic;

        private bool AppliesTo(AbilityData spell) =>
            spell != null
            && MediumPsychicCasting.IsPsychic(spell)
            && spell.Blueprint.AvailableMetamagic.HasMetamagic(Metamagic)
            && !spell.HasMetamagic(Metamagic);

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (AppliesTo(evt.AbilityData))
                evt.AddMetamagic(Metamagic);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
            if (!AppliesTo(evt.Spell)) return;
            int cost = ForcefulMetamagicHelper.AdjustedCost(Owner?.Descriptor, Metamagic);
            MediumInfluenceRules.TryAccept(Owner?.Descriptor, cost);
            Buff?.Remove();
        }

        public void OnEventDidTrigger(RuleCastSpell evt) { }
    }
}
