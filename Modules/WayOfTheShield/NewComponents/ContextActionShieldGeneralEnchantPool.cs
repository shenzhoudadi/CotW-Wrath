using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers;
using Kingmaker.Items;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Way_of_the_shield.NewComponents
{
    [TypeId("1fe5b88460f04fa2873b0c421b77e31b")]
    public class ContextActionShieldGeneralEnchantPool : ContextAction
    {
        public EnchantPoolType EnchantPool = EnchantPoolType.DivineWeaponBond;
        public ActivatableAbilityGroup Group = ActivatableAbilityGroup.DivineWeaponProperty;
        [SerializeField, FormerlySerializedAs("DefaultEnchantmentsWeapon")]
        public BlueprintWeaponEnchantmentReference[] m_DefaultEnchantmentsWeapon = new BlueprintWeaponEnchantmentReference[6];
        [SerializeField, FormerlySerializedAs("DefaultEnchantmentsArmor")]
        public BlueprintArmorEnchantmentReference[] m_DefaultEnchantmentsArmor = new BlueprintArmorEnchantmentReference[6];
        public ContextDurationValue DurationValue;
        public ContextValue EnhancementBudget = new() { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default };
        public ReferenceArrayProxy<BlueprintWeaponEnchantment, BlueprintWeaponEnchantmentReference> DefaultEnchantmentsWeapon => m_DefaultEnchantmentsWeapon;
        public ReferenceArrayProxy<BlueprintArmorEnchantment, BlueprintArmorEnchantmentReference> DefaultEnchantmentsArmor => m_DefaultEnchantmentsArmor;

        public override string GetCaption() => "Enhance caster's shield from the sacred shield bond pool";

        public override void RunAction()
        {
            UnitEntityData caster = Context?.MaybeCaster;
            ItemEntityShield shield = caster?.Body?.SecondaryHand?.MaybeShield;
            if (shield?.ArmorComponent == null || DurationValue == null) return;
            var pool = caster.Ensure<UnitPartEnchantPoolData>();
            pool.ClearEnchantPool(EnchantPool);
            ItemEntityArmor armor = shield.ArmorComponent;
            ItemEntityWeapon weapon = shield.WeaponComponent;
            int armorBase = GameHelper.GetItemEnhancementBonus(armor);
            int weaponBase = weapon == null ? 0 : GameHelper.GetItemEnhancementBonus(weapon);
            int budget = Math.Max(0, Math.Min(6, EnhancementBudget.Calculate(Context)));
            // Another class's shared UI group cannot increase this archetype's bond budget.
            budget = Math.Min(budget, caster.Ensure<UnitPartActivatableAbility>().GetGroupSize(Group));
            int reserve = armorBase <= 0 ? 1 : 0;
            Rounds duration = DurationValue.Calculate(Context);
            var selected = new HashSet<BlueprintGuid>();
            foreach (AddBondProperty property in caster.Buffs.SelectFactComponents<AddBondProperty>())
            {
                if (property.EnchantPool != EnchantPool) continue;
                BlueprintItemEnchantment enchantment = property.Enchant;
                if (enchantment == null || !selected.Add(enchantment.AssetGuid)) continue;
                ItemEntity item = enchantment is BlueprintWeaponEnchantment ? weapon :
                    enchantment is BlueprintArmorEnchantment ? armor : null;
                if (item == null || item.HasEnchantment(enchantment)) continue;
                // Native Fortification and Spell Resistance enchantments have cost zero.
                // The selector's weight is the sacred bond price; do not mutate native assets.
                var choice = caster.ActivatableAbilities.Enumerable.Select(ability => ability.Blueprint)
                    .FirstOrDefault(ability => ability.Group == Group && ability.m_Buff?.Get() == property.OwnerBlueprint);
                if (choice == null || !TrySpendProperty(budget, choice.WeightInGroup, reserve, out int remaining)) continue;
                Add(pool, item, enchantment, duration);
                budget = remaining;
            }
            int weaponIncrease = ComputeWeaponIncrease(weaponBase, budget, m_DefaultEnchantmentsWeapon?.Length ?? 0);
            if (weapon != null && weaponIncrease > 0)
                Add(pool, weapon, m_DefaultEnchantmentsWeapon[weaponIncrease - 1]?.Get(), duration);
            int armorTarget = ComputeArmorTarget(armorBase, budget, m_DefaultEnchantmentsArmor?.Length ?? 0);
            if (armorTarget > armorBase)
                Add(pool, armor, m_DefaultEnchantmentsArmor[armorTarget - 1]?.Get(), duration);
        }

        internal static int ComputeWeaponIncrease(int existing, int remaining, int maximum) =>
            Math.Max(0, Math.Min(Math.Max(0, remaining), Math.Max(0, maximum - Math.Max(0, existing))));
        internal static int ComputeArmorTarget(int existing, int remaining, int maximum) =>
            Math.Min(Math.Max(0, maximum), Math.Max(0, existing) + Math.Max(0, remaining));
        internal static bool TrySpendProperty(int budget, int selectorWeight, int reserve, out int remaining)
        {
            remaining = budget;
            if (selectorWeight <= 0 || selectorWeight > budget - Math.Max(0, reserve)) return false;
            remaining = budget - selectorWeight;
            return true;
        }

        private void Add(UnitPartEnchantPoolData pool, ItemEntity item, BlueprintItemEnchantment enchantment, Rounds duration)
        {
            if (item == null || enchantment == null) return;
            pool.AddEnchant(item, EnchantPool, enchantment, Context, duration);
            // The native pool saves identity/expiry and the item owns unequip cleanup.
            ItemEnchantment applied = item.Enchantments.GetFact(enchantment);
            if (applied != null) applied.RemoveOnUnequipItem = true;
        }
    }
}
