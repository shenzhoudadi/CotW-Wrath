using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using MediumClass.Medium;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediumClass.Medium.NewComponents.AbilitySpecific
{
    /// <summary>
    /// Spirit Affinity "Spiritual Defense": while wearing light or no armor and carrying no shield,
    /// the medium adds its spirit bonus to AC as a shield bonus, and that bonus applies against
    /// touch attacks as well.
    ///
    /// Applied as a persistent modifier on Owner.Stats.AC (like TwoWeaponDefense / ShieldFocus) so
    /// the character sheet, tooltips, CMD and combat AC all see it, and refreshed on equipment,
    /// buff and spirit changes. A small patch lets the Spiritual Defense modifier pass into Touch AC
    /// and its breakdown while keeping ordinary shield bonuses out of touch AC.
    /// </summary>
    [TypeId("c1d0b7f4-6a2e-4c58-9d31-5b8f0e7a2c64")]
    class MediumSpiritualDefenseComponent : UnitFactComponentDelegate,
        IUnitActiveEquipmentSetHandler,
        IUnitEquipmentHandler,
        IUnitBuffHandler,
        IGlobalSubscriber,
        ISubscriber
    {
        public override void OnActivate()
        {
            base.OnActivate();
            Refresh();
        }

        public override void OnTurnOn()
        {
            base.OnTurnOn();
            Refresh();
        }

        public override void OnTurnOff()
        {
            base.OnTurnOff();
            Owner?.Stats?.AC?.RemoveModifiersFrom(Runtime);
        }

        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
        {
            if (slot?.Owner == Owner) Refresh();
        }

        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit)
        {
            if (unit == Owner) Refresh();
        }

        public void HandleBuffDidAdded(Buff buff)
        {
            if (buff?.Owner == Owner) Refresh();
        }

        public void HandleBuffDidRemoved(Buff buff)
        {
            if (buff?.Owner == Owner) Refresh();
        }

        internal void Refresh()
        {
            var ac = Owner?.Stats?.AC;
            if (ac == null) return;
            ac.RemoveModifiersFrom(Runtime);
            if (Fact == null || !Fact.Active || !Eligible(Owner)) return;
            int value = SpiritBonusValue.Of(Owner);
            if (value <= 0) return;
            ac.AddModifierUnique(value, Runtime, ModifierDescriptor.Shield);
        }

        internal static void RefreshFor(UnitEntityData owner)
        {
            if (owner?.Facts == null) return;
            foreach (var fact in owner.Facts.List.ToArray())
            {
                if (!fact.IsActive) continue;
                fact.CallComponents<MediumSpiritualDefenseComponent>(c => c.Refresh());
            }
        }

        /// <summary>Light or no armor, and no shield in the off hand.</summary>
        private static bool Eligible(UnitEntityData owner)
        {
            try
            {
                if (RestrictionsHelper.CheckHasShield(owner)) return false;
                if (!RestrictionsHelper.CheckHasArmor(owner)) return true;
                return RestrictionsHelper.CheckArmor(owner, ArmorProficiencyGroup.Light);
            }
            catch
            {
                // A body that cannot be inspected must not break the AC calculation.
                return false;
            }
        }

        internal static bool IsSpiritualDefenseModifier(ModifiableValue.Modifier modifier) =>
            modifier?.Source is EntityFact fact
            && fact.Blueprint?.GetComponent<MediumSpiritualDefenseComponent>() != null
            && modifier.ModDescriptor == ModifierDescriptor.Shield
            && modifier.ModValue > 0;
    }

    [HarmonyPatch(typeof(ModifiableValueArmorClass), "OnUpdate")]
    internal static class ModifiableValueArmorClass_SpiritualDefenseTouch_Patch
    {
        private static void Postfix(ModifiableValueArmorClass __instance)
        {
            var modifiers = __instance.GetModifiers(ModifierDescriptor.Shield);
            if (modifiers == null) return;
            int bonus = 0;
            foreach (var mod in modifiers)
            {
                if (MediumSpiritualDefenseComponent.IsSpiritualDefenseModifier(mod))
                    bonus = Math.Max(bonus, mod.ModValue);
            }
            if (bonus <= 0) return;

            int touchRaw = __instance.ApplyModifiersFiltered(__instance.BaseValue, ModifiableValueArmorClass.FilterAllowedForTouch);
            int ffTouchRaw = __instance.ApplyModifiersFiltered(__instance.BaseValue, ModifiableValueArmorClass.FilterAllowedForFlatFootedTouch);
            __instance.Touch = Math.Min(__instance.ModifiedValue, touchRaw + bonus);
            __instance.FlatFootedTouch = Math.Min(__instance.ModifiedValue, ffTouchRaw + bonus);
            __instance.TouchPermanent = Math.Min(__instance.PermanentValue, touchRaw + bonus);
            __instance.FlatFootedTouchPermanent = Math.Min(__instance.PermanentValue, ffTouchRaw + bonus);
        }
    }

    [HarmonyPatch(typeof(StatModifiersBreakdown), nameof(StatModifiersBreakdown.GetDisplayArmorModifiers))]
    internal static class StatModifiersBreakdown_SpiritualDefenseTouch_Patch
    {
        private static void Postfix(ModifiableValueArmorClass acStat, bool touch, ref IEnumerable<ModifiableValue.Modifier> __result)
        {
            if (!touch || acStat == null || __result == null) return;
            var defenseMods = acStat.GetModifiers(ModifierDescriptor.Shield)?
                .Where(MediumSpiritualDefenseComponent.IsSpiritualDefenseModifier)
                .ToArray();
            if (defenseMods == null || defenseMods.Length == 0) return;
            __result = __result.Concat(defenseMods);
        }
    }
}
