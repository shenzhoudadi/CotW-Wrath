using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Prowler;
using MediumClass.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    /// <summary>
    /// Spirit attunement rules not covered by simple StatType modifiers:
    /// - Archmage: caster level checks (SR &amp; dispel), spell attack rolls, spell DCs, Intelligence checks.
    /// - Champion / Adriel: Strength checks.
    /// - Guardian: Constitution checks.
    /// - Trickster: Dexterity checks (Robin Hood is handled by RobinHoodDexterityChecks).
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("a1b2c3d4-1111-4222-8333-444455556666")]
    public class SpiritAttunementRulesComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleSkillCheck>, IRulebookHandler<RuleSkillCheck>,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
        IInitiatorRulebookHandler<RuleSpellResistanceCheck>, IRulebookHandler<RuleSpellResistanceCheck>,
        IInitiatorRulebookHandler<RuleDispelMagic>, IRulebookHandler<RuleDispelMagic>,
        ISubscriber, IInitiatorRulebookSubscriber
    {
        private static int SpiritBonusFor(UnitEntityData owner, string spiritGuid)
        {
            var medium = owner?.Get<UnitPartMedium>();
            if (medium == null) return 0;
            var spirit = BlueprintTool.GetRef<BlueprintCharacterClassReference>(spiritGuid);
            if (!medium.IsActiveSpirit(spirit)) return 0;
            return SpiritBonusValue.ForSpirit(owner, spirit);
        }

        private int MaxBonusFor(params string[] spiritGuids)
        {
            int best = 0;
            foreach (var guid in spiritGuids)
                best = Math.Max(best, SpiritBonusFor(Owner, guid));
            return best;
        }

        public void OnEventAboutToTrigger(RuleSkillCheck evt)
        {
            int bonus = evt.StatType switch
            {
                StatType.Intelligence => MaxBonusFor(Guids.Archmage),
                StatType.Strength => MaxBonusFor(Guids.Champion, Guids.Adriel),
                StatType.Constitution => MaxBonusFor(Guids.Guardian),
                StatType.Dexterity => MaxBonusFor(Guids.Trickster),
                _ => 0
            };
            if (bonus > 0)
                evt.Bonus.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleSkillCheck evt) { }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt?.AbilityData?.Blueprint == null || !evt.AbilityData.Blueprint.IsSpell) return;
            int archmage = SpiritBonusFor(Owner, Guids.Archmage);
            if (archmage > 0)
                evt.AddBonusDC(archmage, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (evt?.Weapon == null) return;
            var cat = evt.Weapon.Blueprint.Category;
            if (cat != WeaponCategory.Touch && cat != WeaponCategory.Ray) return;
            int archmage = SpiritBonusFor(Owner, Guids.Archmage);
            if (archmage > 0)
                evt.AddModifier(archmage, Fact, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

        public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
        {
            int archmage = SpiritBonusFor(Owner, Guids.Archmage);
            if (archmage > 0)
                evt.AddSpellPenetration(archmage, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }

        public void OnEventAboutToTrigger(RuleDispelMagic evt)
        {
            if (evt.Check != RuleDispelMagic.CheckType.CasterLevel && evt.Check != RuleDispelMagic.CheckType.SkillCasterLevel)
                return;
            int archmage = SpiritBonusFor(Owner, Guids.Archmage);
            if (archmage > 0)
                evt.Bonus += archmage;
        }

        public void OnEventDidTrigger(RuleDispelMagic evt) { }
    }

    /// <summary>
    /// Archmage Intermediate (Arcane Surge):
    /// Raises CL and DC by max(1, spiritBonus / 2), and adds (1 + spiritBonus / 2) per damage die.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("b2c3d4e5-2222-4333-8444-555566667777")]
    public class ArcaneSurgeScalingComponent : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IRulebookHandler<RuleCalculateAbilityParams>,
        IInitiatorRulebookHandler<RuleCalculateDamage>, IRulebookHandler<RuleCalculateDamage>,
        ISubscriber, IInitiatorRulebookSubscriber
    {
        private bool IsMediumFamilySpellbook(BlueprintSpellbook book)
        {
            if (book == null) return false;
            var id = book.AssetGuid;
            return MediumSpiritSpellbookRules.IsMediumBook(book)
                || id == BlueprintGuid.Parse(Guids.ArchmageSpellbook)
                || id == BlueprintGuid.Parse(Guids.HierophantSpellbook);
        }

        private int HalfSpiritBonus() => Math.Max(1, SpiritBonusValue.Of(Owner) / 2);

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (!IsMediumFamilySpellbook(evt?.Spellbook?.Blueprint)) return;
            int bonus = HalfSpiritBonus();
            evt.AddBonusDC(bonus, ModifierDescriptor.UntypedStackable);
            evt.AddBonusCasterLevel(bonus, ModifierDescriptor.UntypedStackable);
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var spell = evt?.ParentRule?.SourceAbility;
            if (spell == null || !spell.IsSpell) return;
            var book = evt.Reason?.Context?.SourceAbilityContext?.Ability?.Spellbook?.Blueprint
                ?? evt.ParentRule?.Reason?.Context?.SourceAbilityContext?.Ability?.Spellbook?.Blueprint;
            if (book != null && !IsMediumFamilySpellbook(book)) return;

            int perDie = 1 + SpiritBonusValue.Of(Owner) / 2;
            foreach (var damage in evt.DamageBundle)
            {
                if (damage == null || damage.Precision || damage.IgnoreModifiers) continue;
                int dice = damage.Dice.ModifiedValue.Rolls;
                if (dice > 0)
                    damage.AddModifier(dice * perDie, Fact);
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt) { }
    }

    /// <summary>
    /// Guardian Lesser (Guardian's Shield):
    /// While toggled on, party members and animal companions within (5 + 5 * floor(spiritBonus / 2)) ft
    /// take half damage from any source, with the Guardian taking the redirected half.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("c3d4e5f6-3333-4444-8555-666677778888")]
    public class GuardianShieldOtherComponent : UnitFactComponentDelegate,
        IGlobalRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>, ISubscriber
    {
        private static readonly RulebookEvent.CustomDataKey RedirectedKey =
            new RulebookEvent.CustomDataKey("MediumClass.GuardianShieldOtherRedirected");

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt == null || evt.Target == null || Owner == null) return;
            if (evt.Target == Owner || evt.RedirectionTarget != null) return;
            if (evt.TryGetCustomData<bool>(RedirectedKey, out var already) && already) return;
            if (Owner.Descriptor.State.IsDead || !Owner.Descriptor.State.CanAct
                || Owner.Descriptor.State.HasCondition(UnitCondition.Confusion))
                return;
            if (!evt.Target.IsPlayerFaction || (!evt.Target.IsDirectlyControllable && !evt.Target.IsPet))
                return;

            float radiusFeet = 5f + 5f * (SpiritBonusValue.Of(Owner) / 2);
            if (Owner.DistanceTo(evt.Target) > radiusFeet.Feet().Meters) return;

            evt.SetCustomData(RedirectedKey, true);
            evt.RedirectionTarget = Owner;
            evt.RedirectedPercent = 50;
        }

        public void OnEventDidTrigger(RuleDealDamage evt) { }
    }

    /// <summary>
    /// Guardian Greater (Guardian's Determination):
    /// Expands threat range by (5 + (spiritBonus - 1) * 5) ft and allows teleporting into weapon reach for AoOs.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("d4e5f6a7-4444-4555-8666-777788889999")]
    public class GuardianDeterminationReachComponent : UnitFactComponentDelegate
    {
        internal static float ExtraThreatMeters(UnitEntityData unit)
        {
            var buff = BlueprintTool.Get<BlueprintBuff>(Guids.GuardianGreaterReachBuff);
            if (unit == null || buff == null || !unit.HasFact(buff)) return 0f;
            int spiritBonus = Math.Max(1, SpiritBonusValue.Of(unit));
            return (5f + Math.Max(0, spiritBonus - 1) * 5f).Feet().Meters;
        }
    }

    [HarmonyPatch(typeof(UnitHelper), nameof(UnitHelper.GetThreatRange))]
    internal static class GuardianDeterminationThreatRangePatch
    {
        private static void Postfix(UnitEntityData unit, WeaponSlot hand, ref float? __result)
        {
            if (!__result.HasValue || hand?.Weapon?.Blueprint == null || hand.Weapon.Blueprint.IsRanged) return;
            float extra = GuardianDeterminationReachComponent.ExtraThreatMeters(unit);
            if (extra > 0f)
                __result += extra;
        }
    }

    [HarmonyPatch(typeof(UnitCombatState), nameof(UnitCombatState.AttackOfOpportunity))]
    internal static class GuardianDeterminationTeleportAoOPatch
    {
        private static void Prefix(UnitCombatState __instance, UnitEntityData target, bool simulate)
        {
            if (simulate || __instance?.Unit == null || target == null) return;
            var unit = __instance.Unit;
            float extra = GuardianDeterminationReachComponent.ExtraThreatMeters(unit);
            if (extra <= 0f) return;

            var hand = unit.GetThreatHand();
            if (hand?.Weapon == null || hand.Weapon.Blueprint.IsRanged) return;
            if (!__instance.CanAttackOfOpportunity || !unit.Descriptor.State.CanAct) return;
            if (!__instance.CanActInCombat && !unit.Descriptor.State.HasCondition(UnitCondition.AttackOfOpportunityBeforeInitiative)) return;

            float baseReach = hand.Weapon.AttackRange.Meters;
            float totalReach = unit.View.Corpulence + target.View.Corpulence + baseReach;
            float extendedReach = totalReach + extra;
            float dist = unit.DistanceTo(target);
            if (dist <= totalReach || dist > extendedReach) return;
            if (!unit.HasLOS(target)) return;

            Vector3 dir = unit.Position - target.Position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            float targetDist = Math.Max(unit.View.Corpulence + target.View.Corpulence + 0.2f, totalReach - 0.5f);
            Vector3 dest = target.Position + dir * targetDist;
            float orientation = Quaternion.LookRotation(target.Position - dest).eulerAngles.y;

            bool prevPrevent = __instance.PreventAttacksOfOpporunityNextFrame;
            if (unit.RiderPart?.SaddledUnit != null)
                unit.RiderPart.SaddledUnit.Translocate(dest, orientation);
            unit.Translocate(dest, orientation);
            __instance.PreventAttacksOfOpporunityNextFrame = prevPrevent;
        }
    }

    /// <summary>
    /// Guardian Determination temporary HP buff: 2 * medium level temporary HP for 1 minute.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("e5f6a7b8-5555-4666-8777-888899990000")]
    public class GuardianDeterminationTempHpComponent : UnitFactComponentDelegate<GuardianDeterminationTempHpComponent.TempHpData>
    {
        public class TempHpData
        {
            public ModifiableValue.Modifier Modifier;
        }

        public override void OnActivate()
        {
            int amount = Math.Max(1, ProwlerSpiritRules.SpiritClassLevel(Owner) * 2);
            Data.Modifier = Owner?.Stats?.TemporaryHitPoints?.AddModifier(amount, Runtime, ModifierDescriptor.UntypedStackable);
        }

        public override void OnDeactivate()
        {
            Data.Modifier?.Remove();
            Data.Modifier = null;
        }
    }

    /// <summary>
    /// Trickster Greater (Swift Break):
    /// Target is treated as flat-footed for 1 round unless immune to being flat-footed.
    /// </summary>
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("f6a7b8c9-6666-4777-8888-999900001111")]
    public class TricksterSwiftBreakComponent : UnitFactComponentDelegate,
        ITargetRulebookHandler<RuleCheckTargetFlatFooted>, IRulebookHandler<RuleCheckTargetFlatFooted>,
        ISubscriber, ITargetRulebookSubscriber
    {
        private static bool IsFlatFootedImmune(UnitEntityData target)
        {
            if (target?.Facts == null) return false;
            foreach (var fact in target.Facts.List)
            {
                if (!fact.IsActive) continue;
                var ff = fact.Blueprint?.GetComponent<FlatFootedIgnore>();
                if (ff != null && ff.Type == FlatFootedIgnoreType.UncannyDodge)
                    return true;
            }
            return false;
        }

        public void OnEventAboutToTrigger(RuleCheckTargetFlatFooted evt)
        {
            if (evt?.Target == Owner && !IsFlatFootedImmune(Owner))
                evt.ForceFlatFooted = true;
        }

        public void OnEventDidTrigger(RuleCheckTargetFlatFooted evt) { }
    }
}
