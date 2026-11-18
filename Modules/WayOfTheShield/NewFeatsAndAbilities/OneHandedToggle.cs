using Kingmaker.Blueprints.Classes;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using System;
using Way_of_the_shield.Tweaks_and_Changes;

namespace Way_of_the_shield.NewFeatsAndAbilities
{
    // RETIRED: the forced one-handed grip toggle is no longer published or
    // granted. Main.ConfigureIntegrated no longer calls CachePatch_Postfix and
    // the runtime grip patch no longer consults ForceOneHanded. The component
    // types below are kept only so old saves containing their TypeIds can still
    // deserialize; nothing creates or grants these blueprints at runtime.
    public static class OneHandedToggle
    {
        private static bool initialized;
        public static bool CanUseOneHandedGrip(ItemEntityWeapon weapon, UnitEntityData owner)
            => weapon != null && !weapon.Blueprint.Double
                && !(weapon.Blueprint.IsRanged && weapon.Blueprint.IsTwoHanded)
                && weapon.CanTakeOneHand(owner);
        public static void CachePatch_Postfix()
        {
            if (initialized) return;
            var icon = LoadIcon("Icon_OneHandedToggle");
            var buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("549016f34f4349fdb6b20ecb11e79a73");
            if (buff == null)
            {
                buff = new BlueprintBuff
                {
                    m_DisplayName = new LocalizedString { Key = "OneHandedBuff_DisplayName" },
                    m_Description = new LocalizedString { Key = "OneHandedAbility_Description" },
                    m_Icon = icon,
                    FxOnRemove = new(),
                    FxOnStart = new()
                };
                buff.AddComponent(new EquipmentForceOneHandedComponent());
                buff.AddToCache("549016f34f4349fdb6b20ecb11e79a73", "OneHandedBuff");
            }
            var ability = ResourcesLibrary.TryGetBlueprint<BlueprintActivatableAbility>("15542aa4e3f542a4bbd66bcb13392798");
            if (ability == null)
            {
                ability = new BlueprintActivatableAbility
                {
                    m_DisplayName = new LocalizedString { Key = "OneHandedAbility_DisplayName" },
                    m_Description = new LocalizedString { Key = "OneHandedAbility_Description" },
                    m_Buff = buff.ToReference<BlueprintBuffReference>(),
                    m_Icon = icon,
                    ActivationType = AbilityActivationType.Immediately,
                    DoNotTurnOffOnRest = true
                };
                ability.AddComponent(new EquipmentOneHandedRestriction());
                ability.AddToCache("15542aa4e3f542a4bbd66bcb13392798", "OneHandedToggleAbility");
            }
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("21a2cfc0fc894dd9ac1ab49e4c35ba38");
            if (feature == null)
            {
                feature = new BlueprintFeature
                {
                    m_DisplayName = new LocalizedString { Key = "OneHandedFeature_DisplayName" },
                    IsClassFeature = true,
                    HideInUI = true,
                    HideInCharacterSheetAndLevelUp = true
                };
                feature.AddComponent(new AddFacts { m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() } });
                feature.AddToCache("21a2cfc0fc894dd9ac1ab49e4c35ba38", "OneHandedToggleFeature");
            }
            if (!RetrieveBlueprint("ca22afeb94442b64fb8536e7a9f7dc11", out BlueprintFeature defensive, "FightDefensively", "adding native one-handed grip toggle")) return;
            ShieldBashAbilityTweak.AddFactOnce(defensive, feature.ToReference<BlueprintUnitFactReference>());
            initialized = true;
        }

        [TypeId("169f9b1e57814e9bad069cc1247b067c")]
        public class EquipmentOneHandedRestriction : ActivatableAbilityRestriction
        {
            protected override bool IsAvailable()
            {
                var slot = Owner?.Body?.PrimaryHand;
                return slot?.MaybeItem is ItemEntityWeapon weapon && CanUseOneHandedGrip(weapon, Owner);
            }
        }

        [TypeId("cde185e2d1d048c39a6a5d32878146f2")]
        public class EquipmentForceOneHandedComponent : UnitBuffComponentDelegate<EquipmentForceOneHandedComponent.ComponentData>, IUnitEquipmentHandler, IUnitActiveEquipmentSetHandler
        {
            public class ComponentData { [Newtonsoft.Json.JsonIgnore] public bool Retained; }
            protected override void OnTurnOn()
            {
                if (!Data.Retained) { Owner.Ensure<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>().ForceOneHanded.Retain(); Data.Retained = true; }
                ApplyGrip();
            }
            protected override void OnTurnOff()
            {
                if (Data.Retained) { Owner.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.ForceOneHanded.Release(); Data.Retained = false; }
                Owner.Body.CurrentHandsEquipmentSet.ResetGripPreferences();
            }
            private void ApplyGrip()
            {
                var set = Owner?.Body?.CurrentHandsEquipmentSet;
                if (set?.PrimaryHand.MaybeItem is not ItemEntityWeapon weapon) return;
                if (CanUseOneHandedGrip(weapon, Owner)) set.TryChangeGrip(GripType.OneHanded);
                else if (weapon.Blueprint.IsRanged && weapon.Blueprint.IsTwoHanded) set.ResetGripPreferences();
            }
            public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
            {
                if (slot?.Owner == Owner.Descriptor) ApplyGrip();
            }
            public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit)
            {
                if (unit == Owner.Descriptor) ApplyGrip();
            }
        }
    }
}

