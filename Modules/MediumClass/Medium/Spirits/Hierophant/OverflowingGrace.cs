using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Utility;
using MediumClass.Utilities;
using MediumClass.Utils;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace MediumClass.Medium.Spirits.Hierophant
{
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("c8a1f3d2-5b6e-4f8a-9c1d-2e3f4a5b6c7d")]
    public class OverflowingGraceBuffComponent : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleSkillCheck>, IRulebookHandler<RuleSkillCheck>,
        ISubscriber, IInitiatorRulebookSubscriber
    {
        public ModifierDescriptor Descriptor = ModifierDescriptor.Sacred;

        private int BonusAmount() => Math.Max(1, SpiritBonusValue.Of(Context?.MaybeCaster ?? Owner));

        public override void OnTurnOn()
        {
            RemoveModifiers();
            int bonus = BonusAmount();
            Owner.Stats.GetStat(StatType.AdditionalAttackBonus)?.AddModifier(bonus, Runtime, Descriptor);
            foreach (var stat in StatTypeHelper.Skills.Concat(StatTypeHelper.Saves))
                Owner.Stats.GetStat(stat)?.AddModifier(bonus, Runtime, Descriptor);
        }

        public override void OnTurnOff() => RemoveModifiers();

        private void RemoveModifiers()
        {
            Owner.Stats.GetStat(StatType.AdditionalAttackBonus)?.RemoveModifiersFrom(Runtime);
            foreach (var stat in StatTypeHelper.Skills.Concat(StatTypeHelper.Saves))
                Owner.Stats.GetStat(stat)?.RemoveModifiersFrom(Runtime);
        }

        public void OnEventAboutToTrigger(RuleSkillCheck evt)
        {
            if (evt == null || !MarshalSpiritRules.AttributeChecks.Contains(evt.StatType)) return;
            evt.Bonus.AddModifier(BonusAmount(), Fact, Descriptor);
        }

        public void OnEventDidTrigger(RuleSkillCheck evt) { }
    }

    [AllowedOn(typeof(BlueprintUnitFact), false)]
    [TypeId("d9b2e4f3-6c7f-4a9b-8d2e-3f4a5b6c7d8e")]
    public class OverflowingGraceTriggerComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleHealDamage>, IRulebookHandler<RuleHealDamage>,
        IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
        ISubscriber, IInitiatorRulebookSubscriber
    {
        private static SpellDescriptor GetDescriptor(Kingmaker.RuleSystem.RulebookEvent evt)
        {
            var descriptor = evt?.Reason?.Context?.SpellDescriptor ?? SpellDescriptor.None;
            if (descriptor == SpellDescriptor.None)
            {
                var bp = evt?.Reason?.Ability?.Blueprint;
                if (bp?.GetComponent<SpellDescriptorComponent>() is SpellDescriptorComponent comp)
                    descriptor |= (SpellDescriptor)comp.Descriptor;
            }
            return descriptor;
        }

        public void OnEventAboutToTrigger(RuleHealDamage evt) { }

        public void OnEventDidTrigger(RuleHealDamage evt)
        {
            if (evt?.Target == null || Owner == null) return;
            if (evt.ValueWithoutReduction <= 0) return;

            bool targetAtFull = evt.Target.Descriptor.Damage <= 0 || evt.Target.HPLeft >= evt.Target.MaxHP;
            if (!targetAtFull) return;

            var descriptor = GetDescriptor(evt);
            bool isChannelNeg = (descriptor & SpellDescriptor.ChannelNegativeHeal) != 0;
            bool isChannelPos = (descriptor & SpellDescriptor.ChannelPositiveHeal) != 0;

            bool alreadyFull = evt.Value <= 0;
            if (alreadyFull && !isChannelPos && !isChannelNeg)
                return;

            bool useProfane;
            if (isChannelNeg)
                useProfane = true;
            else if (isChannelPos)
                useProfane = false;
            else
            {
                var negAff = FeatureRefs.NegativeEnergyAffinity.Reference.Get();
                var lifeDom = FeatureRefs.LifeDominantSoul.Reference.Get();
                useProfane = evt.Target.Descriptor.HasFact(negAff) && !evt.Target.Descriptor.HasFact(lifeDom);
            }

            ApplyToSelfAndAllies(useProfane);
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt) { }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt?.Target == null || Owner == null) return;
            if (evt.Result <= 0) return;
            if (!evt.Target.Descriptor.State.IsDead && evt.Target.HPLeft > 0) return;

            var descriptor = GetDescriptor(evt);
            bool isChannelNeg = (descriptor & SpellDescriptor.ChannelNegativeHarm) != 0;
            bool isChannelPos = (descriptor & SpellDescriptor.ChannelPositiveHarm) != 0;
            if (!isChannelNeg && !isChannelPos) return;

            ApplyToSelfAndAllies(isChannelNeg);
        }

        private void ApplyToSelfAndAllies(bool useProfane)
        {
            var buffBp = BlueprintTool.Get<BlueprintBuff>(useProfane
                ? Guids.HierophantOverflowingGraceProfane
                : Guids.HierophantOverflowingGraceSacred);
            if (buffBp == null) return;

            float radiusMeters = 30f.Feet().Meters;
            var duration = TimeSpan.FromSeconds(6);
            var units = Game.Instance?.State?.Units;
            if (units == null)
            {
                Owner.Buffs.AddBuff(buffBp, Owner, duration);
                return;
            }

            foreach (var unit in units)
            {
                if (unit == null || unit.Descriptor.State.IsDead) continue;
                if (unit == Owner || (unit.IsAlly(Owner) && Owner.DistanceTo(unit) <= radiusMeters))
                    unit.Buffs.AddBuff(buffBp, Owner, duration);
            }
        }
    }

    class OverflowingGrace
    {
        private static readonly string FeatName = "HierophantOverflowingGrace";
        private static readonly string DisplayName = "HierophantOverflowingGrace.Name";
        private static readonly string Description = "HierophantOverflowingGrace.Description";
        private static readonly ModLogger Logger = Logging.GetLogger(nameof(OverflowingGrace));

        public static void ConfigureEnabled()
        {
            Logger.Log("Generating Hierophant Overflowing Grace");
            BuffConfigurator.New(FeatName + "SacredBuff", Guids.HierophantOverflowingGraceSacred)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(AbilityRefs.ChannelEnergy.Reference.Get().Icon)
                .SetStacking(StackingType.Replace)
                .AddComponent<OverflowingGraceBuffComponent>(c => c.Descriptor = ModifierDescriptor.Sacred)
                .Configure();

            BuffConfigurator.New(FeatName + "ProfaneBuff", Guids.HierophantOverflowingGraceProfane)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .SetIcon(AbilityRefs.ChannelNegativeEnergy.Reference.Get().Icon)
                .SetStacking(StackingType.Replace)
                .AddComponent<OverflowingGraceBuffComponent>(c => c.Descriptor = ModifierDescriptor.Profane)
                .Configure();

            FeatureConfigurator.New(FeatName + "OverflowingGrace", Guids.HierophantOverflowingGrace)
                .SetDisplayName(DisplayName)
                .SetDescription(Description)
                .AddComponent<OverflowingGraceTriggerComponent>()
                .Configure();
        }
    }
}
