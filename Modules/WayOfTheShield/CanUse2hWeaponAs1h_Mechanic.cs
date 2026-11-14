using HarmonyLib;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Way_of_the_shield.NewComponents;

namespace Way_of_the_shield
{
    // Keep native insertion, animations and events, extending only grip permission.
    [HarmonyPatch]
    internal static class ItemEntityWeapon__HoldInTwoHands__Patch
    {
        internal struct InsertionContext { public HandSlot Slot; public ItemEntity Item; }
        [ThreadStatic] private static InsertionContext insertion;

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.CanInsertItem))]
        [HarmonyPrefix]
        private static void BeginCanInsert(ItemSlot __instance, ItemEntity item, out InsertionContext __state)
        {
            __state = insertion;
            insertion = new InsertionContext { Slot = __instance as HandSlot, Item = item };
        }
        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.CanInsertItem))]
        [HarmonyFinalizer]
        private static void EndCanInsert(InsertionContext __state) => insertion = __state;

        [HarmonyPatch(typeof(HandSlot), "OnItemInserted")]
        [HarmonyPrefix]
        private static void BeginInserted(HandSlot __instance, out InsertionContext __state)
        {
            __state = insertion;
            insertion = new InsertionContext { Slot = __instance, Item = __instance.MaybeItem };
        }
        [HarmonyPatch(typeof(HandSlot), "OnItemInserted")]
        [HarmonyFinalizer]
        private static void EndInserted(InsertionContext __state) => insertion = __state;

        [HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.CanTakeOneHand))]
        [HarmonyPostfix]
        private static void ExtendOneHandPermission(ItemEntityWeapon __instance, UnitEntityData unit, ref bool __result)
        {
            if (__result || __instance == null || __instance.Blueprint.Double) return;
            var owner = unit ?? insertion.Slot?.Owner?.Unit ?? __instance.Wielder?.Unit ?? __instance.Owner?.Unit;
            __result = owner?.Get<UnitPartCanHold2hWeaponIn1h>()?.CanBeUsedOn(__instance, insertion.Slot, insertion.Item) ?? false;
        }

        [HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.HoldInTwoHands), MethodType.Getter)]
        [HarmonyPostfix]
        private static void FixOffhandGrip(ItemEntityWeapon __instance, ref bool __result)
        {
            if (__instance.HoldingSlot is HandSlot slot && !slot.IsPrimaryHand) __result = false;
        }

        [HarmonyPatch(typeof(HandSlot), nameof(HandSlot.IsItemTypeSupported))]
        [HarmonyPrefix]
        private static bool ExtendSupportedTypes(HandSlot __instance, ItemEntity item, ref bool __result)
        {
            if (item is ItemEntityShield) __result = !__instance.IsPrimaryHand;
            else if (item is ItemEntityWeapon weapon)
                __result = __instance.IsPrimaryHand || CanInsertSingleHandSlotForReal(weapon, __instance, item);
            else __result = item?.Blueprint is BlueprintItemEquipmentHandSimple;
            return false;
        }

        [HarmonyPatch(typeof(HandSlot), nameof(HandSlot.IsItemSupported))]
        [HarmonyPrefix]
        private static bool ExtendSupportedPair(HandSlot __instance, ItemEntity item, ref bool __result)
        {
            __result = ItemIsSupported(__instance, item);
            return false;
        }
        private static bool ItemIsSupported(HandSlot slot, ItemEntity item)
        {
            if (item == null || (slot.HasItem && !slot.CanRemoveItem()) || !slot.IsItemTypeSupported(item)) return false;
            var pair = slot.PairSlot;
            if (!pair.HasItem || pair.CanRemoveItem()) return true;
            return PairCanRemain(slot, item);
        }
        private static bool PairCanRemain(HandSlot slot, ItemEntity item)
        {
            var pair = slot.PairSlot;
            var insertedWeapon = item as ItemEntityWeapon;
            if (insertedWeapon != null && !CanInsertSingleHandSlotForReal(insertedWeapon, slot, item)) return false;
            if (pair.MaybeItem is ItemEntityWeapon existing)
                return CanInsertSingleHandSlotForReal(existing, slot, item);
            return pair.MaybeItem is ItemEntityShield || pair.MaybeItem?.Blueprint is BlueprintItemEquipmentHandSimple;
        }

        [HarmonyPatch(typeof(HandsEquipmentSet), nameof(HandsEquipmentSet.GetSuitableSlot))]
        [HarmonyPrefix]
        private static bool GetSuitableExtendedSlot(HandsEquipmentSet __instance, ItemEntity item, ref HandSlot __result)
        {
            if (item is ItemEntityShield)
            {
                __result = ItemIsSupported(__instance.SecondaryHand, item) ? __instance.SecondaryHand : null;
                return false;
            }
            if (item is ItemEntityWeapon && __instance.PrimaryHand.HasItem
                && ItemIsSupported(__instance.SecondaryHand, item)
                && PairCanRemain(__instance.SecondaryHand, item))
                __result = __instance.SecondaryHand;
            else if (ItemIsSupported(__instance.PrimaryHand, item)) __result = __instance.PrimaryHand;
            else if (ItemIsSupported(__instance.SecondaryHand, item)) __result = __instance.SecondaryHand;
            else __result = null;
            return false;
        }

        [HarmonyPatch(typeof(HandsEquipmentSet), nameof(HandsEquipmentSet.IsGripAllowed))]
        [HarmonyPostfix]
        private static void ExtendGripPermission(HandsEquipmentSet __instance, GripType newGripType, ref bool __result)
        {
            if (newGripType == GripType.Auto) return;
            var owner = __instance.PrimaryHand.Owner?.Unit;
            // The forced one-handed grip toggle (OneHandedToggle) was removed.
            // Its MechanicsFeatureExtension.ForceOneHanded slot is kept as an
            // indexed placeholder so the remaining feature flags keep their IDs.
            if (__result) return;
            var weapon = __instance.PrimaryHand.MaybeItem as ItemEntityWeapon;
            if (weapon == null || weapon.Blueprint.Double) return;
            // Shield permission never authorizes a second real weapon.
            if (__instance.SecondaryHand.HasItem && __instance.SecondaryHand.MaybeItem is not ItemEntityShield) return;
            if (newGripType == GripType.TwoHanded && !weapon.CanTakeTwoHands()) return;
            __result = owner?.Get<UnitPartCanHold2hWeaponIn1h>()?.CanHoldWeaponWithGrip(weapon, newGripType) ?? false;
        }

        [HarmonyPatch(typeof(RuleCalculateAttacksCount), nameof(RuleCalculateAttacksCount.CalculateSecondaryHand))]
        [HarmonyPrefix]
        private static void AllowLegalBucklerBash(ref bool isPrimaryTwoHanded, HandSlot offHand, RuleCalculateAttacksCount __instance)
        {
            if (offHand != null)
                isPrimaryTwoHanded = isPrimaryTwoHanded && !OffHandParry.BucklerOrBash(offHand.Owner.State.Features.ShieldBash, __instance);
        }

        public static bool CanInsertSingleHandSlotForReal(ItemEntityWeapon weapon, HandSlot slotToInsert = null, ItemEntity itemBeingInserted = null)
        {
            if (weapon == null) return false;
            var owner = slotToInsert?.Owner?.Unit ?? weapon.Wielder?.Unit ?? weapon.Owner?.Unit;
            var previous = insertion;
            try
            {
                insertion = new InsertionContext { Slot = slotToInsert, Item = itemBeingInserted };
                return weapon.CanTakeOneHand(owner);
            }
            finally { insertion = previous; }
        }
        public static bool CanUseGripIfSecondaryHandOccupied(ItemEntityWeapon weapon, GripType gripType)
            => weapon?.Wielder?.Unit?.Get<UnitPartCanHold2hWeaponIn1h>()?.CanHoldWeaponWithGrip(weapon, gripType) ?? false;
    }

    public abstract class CanUse2hWeaponAs1hBase : UnitBuffComponentDelegate
    {
        public abstract bool CanBeUsedOn(ItemEntityWeapon weapon, HandSlot slotToInsert = null, ItemEntity itemBeingInserted = null);
        public abstract bool CanHoldWeaponWithGrip(ItemEntityWeapon weapon, GripType grip);
        public abstract bool IsApplicableToOffHand { get; }
        protected override void OnTurnOn()
        {
            var part = Owner.Ensure<UnitPartCanHold2hWeaponIn1h>();
            if (!part.buffs.Contains(Fact)) part.buffs.Add(Fact);
        }
        protected override void OnTurnOff() => Owner.Get<UnitPartCanHold2hWeaponIn1h>()?.buffs.Remove(Fact);
    }
    public class UnitPartCanHold2hWeaponIn1h : OldStyleUnitPart
    {
        public List<EntityFact> buffs = new();
        public bool CanBeUsedOn(ItemEntityWeapon weapon, HandSlot slotToInsert = null, ItemEntity itemBeingInserted = null)
        {
            if (weapon == null || weapon.Blueprint.Double) return false;
            bool primary = weapon == itemBeingInserted && slotToInsert != null ? slotToInsert.IsPrimaryHand
                : weapon.HoldingSlot is not HandSlot slot || slot.IsPrimaryHand;
            foreach (var fact in buffs.ToArray())
            {
                if (fact == null || fact.IsDisposed) continue;
                bool result = false;
                fact.CallComponents<CanUse2hWeaponAs1hBase>(c =>
                    result |= (primary || c.IsApplicableToOffHand) && c.CanBeUsedOn(weapon, slotToInsert, itemBeingInserted));
                if (result) return true;
            }
            return false;
        }
        public bool CanHoldWeaponWithGrip(ItemEntityWeapon weapon, GripType gripType)
        {
            if (weapon == null || weapon.Blueprint.Double) return false;
            foreach (var fact in buffs.ToArray())
            {
                if (fact == null || fact.IsDisposed) continue;
                bool result = false;
                fact.CallComponents<CanUse2hWeaponAs1hBase>(c => result |= c.CanBeUsedOn(weapon) && c.CanHoldWeaponWithGrip(weapon, gripType));
                if (result) return true;
            }
            return false;
        }
    }
}

