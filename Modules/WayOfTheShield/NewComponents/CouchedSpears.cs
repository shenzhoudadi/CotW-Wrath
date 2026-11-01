using Kingmaker.Blueprints.Classes;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using System;
using Way_of_the_shield.Tweaks_and_Changes;

namespace Way_of_the_shield.NewComponents
{
    public class CouchedSpears
    {
        [TypeId("a7d850915c644a3aa8332d4b76b61764")]
        public class CouchedSpears_Component : CanUse2hWeaponAs1hBase, IUnitMountHandler
        {
            [ThreadStatic] private static UnitEntityData dismounting;
            public override bool CanHoldWeaponWithGrip(ItemEntityWeapon weapon, GripType gripType) => gripType != GripType.TwoHanded;
            public override bool IsApplicableToOffHand => false;
            public override bool CanBeUsedOn(ItemEntityWeapon weapon, HandSlot slotToInsert = null, ItemEntity itemBeingInserted = null)
            {
                return weapon != null && !weapon.Blueprint.Double && Fact.Owner?.Unit != dismounting && Fact.Owner?.Unit.GetSaddledUnit() != null
                    && (weapon.Blueprint.Category == WeaponCategory.Spear || weapon.Blueprint.Category == WeaponCategory.Longspear);
            }
            public void HandleUnitMount(UnitEntityData unit, UnitEntityData mount)
            {
                if (unit == Owner) Owner.Body.CurrentHandsEquipmentSet.ResetGripPreferences();
            }
            public void HandleUnitDismount(UnitEntityData unit, UnitEntityData mount)
            {
                if (unit != Owner) return;
                var previous = dismounting;
                try
                {
                    dismounting = Owner;
                    var set = Owner.Body.CurrentHandsEquipmentSet;
                    var weapon = set.PrimaryHand.MaybeItem as ItemEntityWeapon;
                    if (weapon != null && (weapon.Blueprint.Category == WeaponCategory.Spear || weapon.Blueprint.Category == WeaponCategory.Longspear)
                        && !weapon.CanTakeOneHand(Owner) && set.SecondaryHand.HasItem)
                    {
                        // Return the item through native removal; never destroy it.
                        if (set.SecondaryHand.CanRemoveItem()) set.SecondaryHand.RemoveItem();
                        else if (set.PrimaryHand.CanRemoveItem()) set.PrimaryHand.RemoveItem();
                    }
                    set.ResetGripPreferences();
                }
                finally { dismounting = previous; }
            }
        }
        private static bool initialized;
        public static void AddCouchedSpears1hToMountedCombat()
        {
            if (initialized || !AllowTwoHandedSpears_as_OneHandedWhenMounted.GetValue()) return;
            if (!RetrieveBlueprint("ca22afeb94442b64fb8536e7a9f7dc11", out BlueprintFeature defensive, "FightDefensively", "adding mounted spear grip")) return;
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("d02e3b77b5234c0ea7e181db362d45cd");
            if (feature == null)
            {
                feature = new BlueprintFeature { HideInUI = true, HideInCharacterSheetAndLevelUp = true, IsClassFeature = true };
                feature.AddComponent(new CouchedSpears_Component());
                feature.AddToCache("d02e3b77b5234c0ea7e181db362d45cd", "WayOfTheShield_MountedSpearGripFeature");
            }
            // Riding is sufficient. Mounted Combat is not an extra feat tax.
            ShieldBashAbilityTweak.AddFactOnce(defensive, feature.ToReference<BlueprintUnitFactReference>());
            initialized = true;
        }
    }
}

