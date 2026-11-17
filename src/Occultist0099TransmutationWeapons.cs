using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Transmutation;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099TransmutationWeapons
    {
        private static readonly string[] Numeric =
        {
            "d704f90f54f813043a525f304f6c0050", "9e9bab3020ec5f64499e007880b37e52", "d072b841ba0668846adeb007f623bd6c",
            "6a6a0901d799ceb49b33d4851ff72132", "746ee366e50611146821d61e391edf16"
        };
        private static readonly (string Token, string Label, int Weight, string Id)[] Options =
        {
            ("Flaming", "Flaming", 1, "30f90becaaac51f41bf56641966c4121"),
            ("Frost", "Frost", 1, "421e54078b7719d40915ce0672511d0b"),
            ("Shock", "Shock", 1, "7bda5277d36ad114f9f9fd21d0dab658"),
            ("GhostTouch", "Ghost Touch", 1, "47857e1a5a3ec1a46adf6491b1423b4f"),
            ("Keen", "Keen", 1, "102a9c8c9b7a75e4fb5844e79deaf4c0"),
            ("Cruel", "Cruel", 1, "629c383ffb407224398bb71d1bd95d14"),
            ("Vicious", "Vicious", 1, "a1455a289da208144981e4b1ef92cc56"),
            ("Disruption", "Disruption", 2, "0f20d79b7049c0f4ca54ca3d1ea44baa"),
            ("Holy", "Holy", 2, "28a9964d81fedae44bae3ca45710c140"),
            ("Unholy", "Unholy", 2, "d05753b8df780fc4bb55b318f06af453"),
            ("Axiomatic", "Axiomatic", 2, "0ca43051edefcad4b9b2240aa36dc8d4"),
            ("Anarchic", "Anarchic", 2, "57315bc1e1f62a741be0efde688087e9"),
            ("Speed", "Speed", 3, "f1c0c50108025d546b2554674ea1c006"),
            ("BrilliantEnergy", "Brilliant Energy", 4, "66e9e299c9002ea4bb65b6f300e43770"),
            ("Bane", "Bane", 4, "1a93ab9c46e48f3488178733be29342a"),
            ("Vorpal", "Vorpal", 5, "2f60bfcba52e48a479e4a69868e24ebc")
        };
        private static readonly (string Token, string Label, string Id, PhysicalDamageMaterial Material)[] Materials =
        {
            ("ColdIron", "Cold Iron", "e5990dc76d2a613409916071c898eee8", PhysicalDamageMaterial.ColdIron),
            ("Silver", "Silver", "0ae8fc9f2e255584faf4d14835224875", PhysicalDamageMaterial.Silver),
            ("Adamantine", "Adamantine", "ab39e7d59dd12f4429ffef5dca88dc7b", PhysicalDamageMaterial.Adamantite)
        };
        internal static IEnumerable<string> Donors => Numeric.Concat(Options.Select(option => option.Id)).Concat(Materials.Select(material => material.Id));

        internal static IEnumerable<string> Tokens() => new[] { "LegacyWeaponAbility", "LegacyWeaponStandardAbility", "LegacyWeaponSwiftAbility",
            "LegacyWeaponBuff", "LegacyWeaponOptions", "PhilosophersTouchAbility", "PhilosophersTouchPower" }
            .Concat(Options.SelectMany(option => new[] { "LegacyWeapon" + option.Token + "Choice", "LegacyWeapon" + option.Token + "ChoiceBuff" }))
            .Concat(Materials.SelectMany(material => new[] { "PhilosophersTouch" + material.Token + "Buff", "PhilosophersTouch" + material.Token + "Burst" }))
            .Concat(new[] { "PhilosophersTouchColdIronTouch", "PhilosophersTouchSilverTouch" });

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, BlueprintUnitProperty level, BlueprintAbility icon,
            Func<string, string, string, LocalizedString> text)
        {
            var numeric = Numeric.Select(id => Load<BlueprintWeaponEnchantment>(cache, id)).ToArray();
            var special = Options.Select(option => Load<BlueprintWeaponEnchantment>(cache, option.Id)).ToArray();
            var materials = Materials.Select(material => Load<BlueprintWeaponEnchantment>(cache, material.Id)).ToArray();
            if (numeric.Where((enchantment, index) => !enchantment?.GetComponents<WeaponEnhancementBonus>()
                    .Any(bonus => bonus.Stack && bonus.EnhancementBonus == index + 1) ?? true).Any() ||
                special.Any(enchantment => enchantment == null) ||
                materials.Where((enchantment, index) => enchantment?.GetComponent<WeaponMaterial>()?.Material != Materials[index].Material).Any())
                return null;

            const string legacyText = "Enhance your primary weapon for 1 minute. Spend the enhancement budget on special properties and add the remainder to the weapon's enhancement, up to +5. A weapon must have at least a +1 enhancement to gain a special property. Spend 1 point of Transmutation focus as a standard action or 2 as a swift action.";
            var slotLock = new LockEquipmentSlot { name = "LegacyWeaponHandLock" };
            SetEnum(slotLock, "m_SlotType", "MainHand");
            var legacyBuff = Identify(new BlueprintBuff { IsClassFeature = true, Stacking = StackingType.Stack,
                ComponentsArray = new BlueprintComponent[] { slotLock, new Occultist0099LegacyWeaponBuff
                    { name = "LegacyWeaponEnhancements", Numeric = numeric.Select(Ref<BlueprintWeaponEnchantmentReference>).ToArray() } } },
                "LegacyWeaponBuff", "Legacy Weapon", legacyText, icon, text);
            var standard = Legacy("LegacyWeaponStandardAbility", "Legacy Weapon", UnitCommand.CommandType.Standard, 1);
            var swift = Legacy("LegacyWeaponSwiftAbility", "Legacy Weapon (Swift)", UnitCommand.CommandType.Swift, 2);
            var legacy = Wrap("LegacyWeaponAbility", "Legacy Weapon", legacyText, icon, text, standard, swift);
            legacy.Type = AbilityType.Supernatural;

            var choices = new List<BlueprintScriptableObject>();
            var toggles = new List<BlueprintActivatableAbility>();
            for (int i = 0; i < Options.Length; i++)
            {
                var option = Options[i];
                string token = "LegacyWeapon" + option.Token + "Choice";
                string title = "Legacy Weapon - " + option.Label;
                string description = "Use " + option.Weight + " point(s) of the enhancement budget on the next Legacy Weapon activation.";
                var marker = Identify(new BlueprintBuff { ComponentsArray = Array.Empty<BlueprintComponent>() }, token + "Buff", title, description, icon, text);
                SetEnum(marker, "m_Flags", "HiddenInUi");
                var toggle = Identify(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately,
                    DeactivateImmediately = true, DoNotTurnOffOnRest = true, Group = ActivatableAbilityGroup.None,
                    ComponentsArray = new BlueprintComponent[] { new Occultist0099LegacyWeaponOption { name = "LegacyWeaponSelection",
                        IgnoreIfStarted = true, Weight = option.Weight, RequiresBludgeoningMelee = option.Token == "Disruption",
                        Enchantment = Ref<BlueprintWeaponEnchantmentReference>(special[i]) } } }, token, title, description, icon, text);
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(marker));
                choices.AddRange(new BlueprintScriptableObject[] { marker, toggle });
                toggles.Add(toggle);
            }
            var carrier = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, HideInUI = true,
                HideInCharacterSheetAndLevelUp = true,
                ComponentsArray = new BlueprintComponent[] { Facts(toggles.Cast<Kingmaker.Blueprints.Facts.BlueprintUnitFact>().Append(legacy).ToArray()) } },
                "LegacyWeaponOptions", "Legacy Weapon Properties", "", icon, text);

            const string touchText = "Spend 1 point of Transmutation focus to treat a primary melee weapon as cold iron or silver for 1 minute per occultist level. At 8th level this affects allies within 15 feet. At 11th level you can choose adamantine.";
            var materialBuffs = Materials.Select((material, index) => Identify(new BlueprintBuff { IsClassFeature = true,
                Stacking = StackingType.Replace, ComponentsArray = new BlueprintComponent[] { new Occultist0099PhilosophersTouchBuff
                    { name = "PhilosophersTouchMaterial", Enchantment = Ref<BlueprintWeaponEnchantmentReference>(materials[index]) } } },
                "PhilosophersTouch" + material.Token + "Buff", "Philosopher's Touch (" + material.Label + ")", touchText, icon, text)).ToArray();
            var leaves = Materials.SelectMany((material, index) => index < 2
                ? new[] { Touch(index, false), Touch(index, true) }
                : new[] { Touch(index, true) }).ToArray();
            var philosophers = Wrap("PhilosophersTouchAbility", "Philosopher's Touch", touchText, icon, text, leaves);
            philosophers.Type = AbilityType.Supernatural;
            return new BlueprintScriptableObject[] { legacy, standard, swift, legacyBuff, carrier, philosophers }
                .Concat(choices).Concat(materialBuffs).Concat(leaves).ToArray();

            BlueprintAbility Legacy(string token, string label, UnitCommand.CommandType action, int cost)
            {
                var apply = Apply(legacyBuff, 1, DurationRate.Minutes, fromSpell: false);
                apply.IsNotDispelable = true;
                var ability = Power(token, label, legacyText, icon, level, text, AbilityRange.Personal,
                    new Occultist0099AegisApply { name = "ApplyLegacyWeapon", ApplyWard = apply },
                    new Occultist0099LegacyWeaponTarget { name = "LegacyWeaponEligibility" });
                ability.Type = AbilityType.Supernatural;
                ability.ActionType = action;
                var resource = ability.GetComponent<Occultist0099FocusResourceLogic>();
                resource.Amount = cost;
                resource.CheckCasterRestrictions = true;
                resource.PayOnEffect = true;
                return ability;
            }

            BlueprintAbility Touch(int index, bool burst)
            {
                var actions = new List<GameAction>();
                foreach (var buff in materialBuffs.Where((_, candidate) => candidate != index))
                {
                    var remove = new ContextActionRemoveBuff();
                    Set(remove, "m_Buff", Ref<BlueprintBuffReference>(buff));
                    actions.Add(remove);
                }
                var apply = Timed(materialBuffs[index], PropertyValue(level), DurationRate.Minutes);
                apply.IsNotDispelable = true;
                actions.Add(apply);
                var components = new List<BlueprintComponent>
                {
                    Run(actions.ToArray()),
                    new Occultist0099PowerLevel { name = "PhilosophersTouchLevel",
                        Minimum = index == 2 ? 11 : burst ? 8 : 1, Maximum = burst ? int.MaxValue : 7 }
                };
                if (burst)
                {
                    var area = new AbilityTargetsAround { name = "PhilosophersTouchAllies" };
                    Set(area, "m_Radius", new Feet(15));
                    SetEnum(area, "m_TargetType", "Ally");
                    Set(area, "m_Condition", new ConditionsChecker { Conditions = Array.Empty<Condition>() });
                    components.Add(area);
                }
                var ability = Power("PhilosophersTouch" + Materials[index].Token + (burst ? "Burst" : "Touch"),
                    "Philosopher's Touch (" + Materials[index].Label + ")", touchText, icon, level, text,
                    burst ? AbilityRange.Personal : AbilityRange.Touch, components.ToArray());
                ability.Type = AbilityType.Supernatural;
                ability.CanTargetFriends = !burst;
                ability.GetComponent<Occultist0099FocusResourceLogic>().CheckCasterRestrictions = true;
                return ability;
            }
        }

        internal static ItemEntityWeapon Primary(UnitEntityData unit) => unit?.Body?.PrimaryHand?.MaybeWeapon ?? unit?.Body?.EmptyHandWeapon;

        internal static int Budget(UnitEntityData unit)
            => Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component => ((Occultist0099FocusFact0099)component).EquipmentBudget(School), 0);

        internal static IEnumerable<Occultist0099LegacyWeaponOption> Selected(UnitEntityData unit, ActivatableAbility excluded = null)
            => unit.ActivatableAbilities.Enumerable.Where(ability => ability != excluded && ability.Active && ability.IsOn && ability.IsStarted)
                .Select(ability => ability.Blueprint.GetComponent<Occultist0099LegacyWeaponOption>()).Where(option => option != null);

        internal static int ExistingEnhancement(ItemEntityWeapon weapon, Occultist0099LegacyWeaponData previous)
        {
            if (previous?.Weapon != weapon || previous.Applied.Count == 0) return GameHelper.GetItemEnhancementBonus(weapon);
            var bonuses = weapon.Enchantments.Where(enchantment => !previous.Applied.Contains(enchantment))
                .SelectMany(enchantment => enchantment.SelectComponents<WeaponEnhancementBonus>()).ToArray();
            int baseBonus = bonuses.Where(bonus => !bonus.Stack).Select(bonus => bonus.EnhancementBonus).DefaultIfEmpty().Max();
            return baseBonus >= 5 ? baseBonus : Math.Min(5, baseBonus + bonuses.Where(bonus => bonus.Stack).Sum(bonus => bonus.EnhancementBonus));
        }

        internal static int Remaining(UnitEntityData unit, ItemEntityWeapon weapon, Occultist0099LegacyWeaponOption[] selected)
        {
            int remaining = Budget(unit) - selected.Sum(option => option.Weight);
            return selected.All(option => option.Allows(weapon)) ? remaining : -1;
        }
    }

    [TypeId(Occultist0099Ids.LegacyWeaponOptionTypeId)]
    public sealed class Occultist0099LegacyWeaponOption : ActivatableAbilityRestriction
    {
        public int Weight;
        public bool RequiresBludgeoningMelee;
        public BlueprintWeaponEnchantmentReference Enchantment;
        internal bool Allows(ItemEntityWeapon weapon) => !RequiresBludgeoningMelee ||
            (weapon.Blueprint.IsMelee && weapon.Blueprint.DamageType != null &&
                (weapon.Blueprint.DamageType.Physical.Form & PhysicalDamageForm.Bludgeoning) != 0);
        protected override bool IsAvailable()
            => Occultist0099TransmutationWeapons.Primary(Owner) is ItemEntityWeapon weapon && Allows(weapon) &&
                Occultist0099TransmutationWeapons.Selected(Owner, Fact).Sum(option => option.Weight) + Weight <= Occultist0099TransmutationWeapons.Budget(Owner);
    }

    [TypeId(Occultist0099Ids.LegacyWeaponTargetTypeId)]
    public sealed class Occultist0099LegacyWeaponTarget : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            var weapon = Occultist0099TransmutationWeapons.Primary(caster);
            if (weapon == null) return false;
            var choices = Occultist0099TransmutationWeapons.Selected(caster).ToArray();
            int remaining = Occultist0099TransmutationWeapons.Remaining(caster, weapon, choices);
            var previous = caster.Buffs.Enumerable.SelectMany(buff => buff.Components)
                .FirstOrDefault(component => component.SourceBlueprintComponent is Occultist0099LegacyWeaponBuff)?.GetData<Occultist0099LegacyWeaponData>();
            return remaining >= 0 && (choices.Length == 0 || remaining + Occultist0099TransmutationWeapons.ExistingEnhancement(weapon, previous) > 0);
        }
        public string GetAbilityCasterRestrictionUIText() => "Choose valid weapon properties and leave at least a +1 weapon enhancement.";
    }

    public sealed class Occultist0099LegacyWeaponData
    {
        [JsonProperty] public ItemEntityWeapon Weapon;
        [JsonProperty] public BlueprintWeaponEnchantmentReference Stackable;
        [JsonProperty] public BlueprintWeaponEnchantmentReference[] Desired = Array.Empty<BlueprintWeaponEnchantmentReference>();
        [JsonProperty] public List<ItemEnchantment> Applied = new List<ItemEnchantment>();
    }

    [TypeId(Occultist0099Ids.LegacyWeaponBuffTypeId)]
    public sealed class Occultist0099LegacyWeaponBuff : UnitBuffComponentDelegate<Occultist0099LegacyWeaponData>,
        IOccultist0099EquipmentWard, IUnitEquipmentHandler, IUnitSubscriber, IUnitActiveEquipmentSetHandler, IUnitEmptyHandWeaponHandler
    {
        public BlueprintWeaponEnchantmentReference[] Numeric;

        bool IOccultist0099EquipmentWard.TryStage(EntityFactComponent previousRuntime)
        {
            var weapon = Occultist0099TransmutationWeapons.Primary(Owner);
            if (weapon == null) return false;
            var previous = previousRuntime?.GetData<Occultist0099LegacyWeaponData>();
            var choices = Occultist0099TransmutationWeapons.Selected(Owner).ToArray();
            int remaining = Occultist0099TransmutationWeapons.Remaining(Owner, weapon, choices);
            int baseline = Occultist0099TransmutationWeapons.ExistingEnhancement(weapon, previous);
            if (remaining < 0 || (choices.Length != 0 && remaining + baseline == 0)) return false;
            int numeric = Math.Min(remaining, Math.Max(0, 5 - baseline));
            Data.Weapon = weapon;
            Data.Stackable = numeric > 0 ? Numeric[numeric - 1] : null;
            Data.Desired = choices.Select(option => option.Enchantment).ToArray();
            return Refresh(previous?.Weapon == weapon ? previous : null);
        }

        void IOccultist0099EquipmentWard.Adopt(EntityFactComponent previousRuntime)
        {
            var previous = previousRuntime?.GetData<Occultist0099LegacyWeaponData>();
            if (previous?.Weapon == Data.Weapon)
                Occultist0099EquipmentEnchantments.Transfer(Data.Weapon, Desired(), previous.Applied, Data.Applied, Data.Stackable?.Get());
        }

        protected override void OnTurnOn() { if (Data.Weapon != null) Restore(); }
        protected override void OnApplyPostLoadFixes() { if (Buff.IsTurnedOn && !Buff.IsSuppressed) Restore(); }
        protected override void OnTurnOff() => Occultist0099EquipmentEnchantments.Clear(Data.Applied);

        private BlueprintItemEnchantment[] Desired() => Data.Desired.Select(reference => reference.Get()).ToArray();
        private bool Refresh(Occultist0099LegacyWeaponData previous = null)
        {
            var stackable = Data.Stackable?.Get();
            if (Data.Stackable != null && stackable == null) return false;
            var previousStackable = stackable == null ? null : previous?.Applied.FirstOrDefault(enchantment => enchantment.Blueprint == stackable);
            return Occultist0099EquipmentEnchantments.Refresh(Data.Weapon, Desired(), Data.Applied, Context, "Legacy Weapon", stackable, previousStackable);
        }
        private void Restore() { if (Data.Weapon == null || Data.Weapon != Occultist0099TransmutationWeapons.Primary(Owner) || !Refresh()) Buff.Remove(); }
        private void CheckWeapon() { if (Data.Weapon != null && Data.Weapon != Occultist0099TransmutationWeapons.Primary(Owner)) Buff.Remove(); }
        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity oldItem) => CheckWeapon();
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit) { if (unit.Unit == Owner) CheckWeapon(); }
        public void HandleUnitEmptyHandWeaponUpdated() => CheckWeapon();
    }

    public sealed class Occultist0099PhilosophersTouchData
    {
        [JsonProperty] public ItemEntityWeapon Weapon;
        [JsonProperty] public List<ItemEnchantment> Applied = new List<ItemEnchantment>();
    }

    [TypeId(Occultist0099Ids.PhilosophersTouchBuffTypeId)]
    public sealed class Occultist0099PhilosophersTouchBuff : UnitBuffComponentDelegate<Occultist0099PhilosophersTouchData>,
        IUnitEquipmentHandler, IUnitSubscriber, IUnitActiveEquipmentSetHandler
    {
        public BlueprintWeaponEnchantmentReference Enchantment;
        protected override void OnTurnOn() => Reconcile();
        protected override void OnApplyPostLoadFixes() { if (Buff.IsTurnedOn && !Buff.IsSuppressed) Reconcile(); }
        protected override void OnTurnOff() => Clear();
        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity oldItem) => Reconcile();
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit) { if (unit.Unit == Owner) Reconcile(); }

        private void Reconcile()
        {
            if (!Buff.IsTurnedOn || Buff.IsSuppressed) return;
            var weapon = Owner.Body.PrimaryHand.MaybeWeapon;
            if (weapon == null || !weapon.Blueprint.IsMelee) { Clear(); return; }
            if (Data.Weapon != weapon) Clear();
            Data.Weapon = weapon;
            if (!Occultist0099EquipmentEnchantments.Refresh(weapon, new[] { Enchantment.Get() }, Data.Applied, Context, "Philosopher's Touch"))
                Buff.Remove();
        }

        private void Clear()
        {
            Occultist0099EquipmentEnchantments.Clear(Data.Applied);
            Data.Weapon = null;
        }
    }
}
