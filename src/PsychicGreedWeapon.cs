using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class PsychicGreedWeaponData
    {
        [JsonProperty] public ItemEntityWeapon Weapon;
        // Native LockEquipmentSlotData uses the same persisted slot reference.
        [JsonProperty] public ItemSlot LockedSlot;
    }
    [AllowedOn(typeof(BlueprintBuff),false)]
    [TypeId("cd25226d1fda4c5eaccbac84483c69b8")]
    public sealed class PsychicGreedWeapon : UnitFactComponentDelegate<PsychicGreedWeaponData>
    {
        public BlueprintItemWeaponReference[] Weapons;
        internal static int Enhancement(int casterLevel)=>casterLevel>=19?4:casterLevel>=17?3:casterLevel>=15?2:1;
        internal static bool HandFree(ItemSlot slot)=>slot!=null && slot.MaybeItem==null && !slot.Lock.Value && !slot.Disabled;
        protected override void OnActivate()
        {
            if(Data.Weapon!=null) return; // Saved/repeated activation never creates a second item.
            var slot=Owner.Body.PrimaryHand;
            if(!HandFree(slot)) {((Buff)Fact).Remove();return;}
            var weapon=Weapons[Enhancement(Context?.Params?.CasterLevel??1)-1].Get().CreateEntity<ItemEntityWeapon>();
            weapon.MakeNotLootable();weapon.Identify();
            Data.Weapon=weapon;
            // Wrath normally refuses to equip a weapon without proficiency.
            // Summoning this item must work for Psychic while leaving attack
            // proficiency and other slot checks untouched after insertion.
            bool inserted;
            using(ContextData<ItemEntity.CanBeEquippedForce>.Request())
                inserted=slot.CanInsertItem(weapon) && slot.InsertItem(weapon);
            if(!inserted) {Cleanup();((Buff)Fact).Remove();return;}
            slot.Lock.Retain();Data.LockedSlot=slot;
        }
        protected override void OnDeactivate()=>Cleanup();
        private void Cleanup()
        {
            // Release only the lock acquired by this component, never other effects' locks.
            var locked=Data.LockedSlot;Data.LockedSlot=null;locked?.Lock.Release();
            var weapon=Data.Weapon;Data.Weapon=null;
            if(weapon==null) return;
            var slot=weapon.HoldingSlot;
            // A forced equipment change must never remove the replacement weapon.
            if(slot!=null && ReferenceEquals(slot.MaybeItem,weapon))
                using(ContextData<ItemSlot.IgnoreLock>.Request()) slot.RemoveItem();
            weapon.Collection?.Remove(weapon);
        }
    }

}
