using System;
using System.Collections.Generic;
using System.Reflection;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;

namespace MediumClass.Medium
{
    internal static class RobinHoodRules
    {
        internal static readonly StatType[] AttunementStats = {
            StatType.SkillMobility, StatType.SkillStealth, StatType.SkillThievery,
            StatType.CheckBluff, StatType.SaveReflex, StatType.Initiative
        };
        internal static bool AllowedAlignment(Alignment alignment) =>
            alignment == Alignment.NeutralGood || alignment == Alignment.ChaoticGood
            || alignment == Alignment.TrueNeutral || alignment == Alignment.ChaoticNeutral;
        internal static bool IsBow(WeaponCategory category) => category == WeaponCategory.Longbow || category == WeaponCategory.Shortbow;
        internal static bool IsBow(ItemEntityWeapon weapon) => weapon != null && IsBow(weapon.Blueprint.Category);
        internal static int DistanceBonus(float feet, int wisdomBonus) =>
            float.IsNaN(feet) || float.IsInfinity(feet) || feet < 0 ? 0 :
            (int)Math.Floor((feet + 0.00001f) / 10f) + (feet > 30.00001f ? Math.Max(0, wisdomBonus) : 0);
        internal static bool HasPower(UnitDescriptor owner,string id) {
            var fact=owner?.Unit.Facts.Get(BlueprintTool.Get<BlueprintFeature>(id));
            // Native OnDeactivate runs before IsActive is cleared.
            return fact!=null && fact.IsActive && !fact.Deactivating;
        }
        internal static bool HasLesser(UnitDescriptor owner) => HasPower(owner,Guids.RobinHoodLesser);
        internal static IEnumerable<(int Level, BlueprintAbility Spell)> KnownSpells(UnitDescriptor owner)
        {
            if (!HasLesser(owner)) yield break;
            string[] ids = { "3e9d1119d43d07c4c8ba9ebfd1671952", "82962a820ebc0e7408b8582fdc3f4c0c",
                "7ec0ffdd8779c344f85337109af0c6c5", "ecaa0def35b38f949bd1976a6c9539e0" };
            for(int i=0;i<ids.Length;i++) yield return (i+1,BlueprintTool.Get<BlueprintAbility>(ids[i]));
        }
        internal static int Attunement(UnitEntityData owner)
        {
            var medium=owner?.Get<UnitPartMedium>();
            var spirit=BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.RobinHood);
            if(medium==null || !medium.IsActiveSpirit(spirit) || !medium.Spirits.TryGetValue(spirit,out var entry)) return 0;
            return owner.Progression.Features.GetRank(entry.SpiritBonus.SpiritBonusFeature)+MultiSpiritRules.EffectiveSpiritFocus(medium,spirit);
        }
    }

    [TypeId("905ca2f7281e5135907141b2b1e6d941")]
    public class RobinHoodAlignmentRequirement : BlueprintComponent, IAbilityRestriction
    {
        public string GetAbilityRestrictionUIText() => "Channeling Robin Hood requires a non-evil, non-lawful alignment.";
        public bool IsAbilityRestrictionPassed(AbilityData ability) => ability.Caster != null && RobinHoodRules.AllowedAlignment(ability.Caster.Alignment.ValueRaw);
    }

    [TypeId("a50339ad7c6b542eaf7649b0f0b8340e")]
    public class RobinHoodLesserComponent : UnitFactComponentDelegate
    {
        public override void OnTurnOn() => Owner.Ensure<UnitPartMediumPreparedSpells>().Sync();
        public override void OnDeactivate() => Owner.Get<UnitPartMediumPreparedSpells>()?.Sync();
    }

    [TypeId("415e1b3c9f1b53149e6e819b29f6254e")]
    public class RobinHoodDexterityChecks : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleSkillCheck>
    {
        public void OnEventAboutToTrigger(RuleSkillCheck evt)
        {
            // A check bonus must not change Dexterity itself (and thus AC, attacks or all skills).
            if(evt.StatType==StatType.Dexterity) {
                int bonus=RobinHoodRules.Attunement(Owner);
                // RuleSkillCheck.TotalBonus reads its own Bonus value; generic
                // RulebookEvent modifiers are not included in this rule's total.
                if(bonus>0) evt.Bonus.AddModifier(bonus,Fact,ModifierDescriptor.UntypedStackable);
            }
        }
        public void OnEventDidTrigger(RuleSkillCheck evt) { }
    }

    [TypeId("69c9d49cf79d5806993268ca10a8c4ca")]
    public class RobinHoodDistantArcher : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>
    {
        private int Bonus(UnitEntityData target) => target==null ? 0 :
            RobinHoodRules.DistanceBonus(Owner.DistanceTo(target)/1.Feet().Meters,Owner.Stats.Wisdom.Bonus);
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt) {
            if(RobinHoodRules.IsBow(evt.Weapon)) evt.AddModifier(Bonus(evt.Target),Fact,ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt) {
            if(RobinHoodRules.IsBow(evt.Weapon) && evt.AttackWithWeapon?.Target!=null)
                evt.DamageModifiers.Add(new Modifier(Bonus(evt.AttackWithWeapon.Target),Fact,ModifierDescriptor.UntypedStackable));
        }
        public void OnEventDidTrigger(RuleCalculateWeaponStats evt) { }
    }

    [TypeId("16d6d57303405776950865ddc55b5c2b")]
    public class RobinHoodUnseenAttacker : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleAttackRoll>, IInitiatorRulebookHandler<RuleCheckTargetFlatFooted>
    {
        public void OnEventAboutToTrigger(RuleAttackRoll evt) => evt.ForceFlatFooted=true;
        public void OnEventDidTrigger(RuleAttackRoll evt) { }
        public void OnEventAboutToTrigger(RuleCheckTargetFlatFooted evt) => evt.ForceFlatFooted=true;
        public void OnEventDidTrigger(RuleCheckTargetFlatFooted evt) { }
    }

    [HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.AttackRange), MethodType.Getter)]
    internal static class RobinHoodBowRangePatch
    {
        private static void Postfix(ItemEntityWeapon __instance,ref Feet __result) {
            if(RobinHoodRules.IsBow(__instance) && RobinHoodRules.HasLesser(__instance.Wielder)) __result=(__result.Value*2).Feet();
        }
    }
}
