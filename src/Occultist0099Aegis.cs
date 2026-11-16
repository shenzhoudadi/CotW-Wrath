using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities;
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
using Kingmaker.Utility;
using Newtonsoft.Json;
using static CallOfTheWild.Wrath.Prototype.Occultist0099Abjuration;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class Occultist0099Aegis
    {
        internal const string MageArmor = "9e1ad5d6f87d19e4d8883d63a6e35568";
        internal static readonly BlueprintGuid WardId = BlueprintGuid.Parse(Occultist0099Ids.Id("AegisArmorBuff"));
        internal static readonly BlueprintGuid StorageEnchantmentId = BlueprintGuid.Parse(Occultist0099Ids.Id("AegisStorageEnchantment"));
        private static readonly string[] NumericDonors =
        {
            "a9ea95c5e02f9b7468447bc1010fe152", "758b77a97640fd747abf149f5bf538d0", "9448d3026111d6d49b31fc85e7f3745a",
            "eaeb89df5be2b784c96181552414ae5a", "6628f9d77fd07b54c911cd8930c0d531"
        };
        private static readonly (string Token, string Label, int Weight, string Guid)[] Options =
        {
            ("Fortification25", "Light Fortification", 1, "1e69e9029c627914eb06608dad707b36"),
            ("Fortification50", "Moderate Fortification", 3, "62ec0b22425fb424c82fd52d7f4c02a5"),
            ("Fortification75", "Heavy Fortification", 5, "9b1538c732e06544bbd955fee570a2be"),
            ("Acid10", "Acid Resistance 10", 1, "dd0e096412423d646929d9b945fd6d4c"),
            ("Acid20", "Acid Resistance 20", 2, "1346633e0ff138148a9a925e330314b5"),
            ("Acid30", "Acid Resistance 30", 4, "e6fa2f59c7f1bb14ebfc429f17d0a4c6"),
            ("Cold10", "Cold Resistance 10", 1, "c872314ecfab32949ad2e0eebd834919"),
            ("Cold20", "Cold Resistance 20", 2, "510d87d2a949587469882061ee186522"),
            ("Cold30", "Cold Resistance 30", 4, "7ef70c319ca74fe4cb5eddea792bb353"),
            ("Electricity10", "Electricity Resistance 10", 1, "1e4dcaf8ffa56c24788e392dae886166"),
            ("Electricity20", "Electricity Resistance 20", 2, "fcfd9515adbd07a43b490280c06203f9"),
            ("Electricity30", "Electricity Resistance 30", 4, "26b91513989a653458986fabce24ba95"),
            ("Fire10", "Fire Resistance 10", 1, "47f45701cc9545049b3745ef949d7446"),
            ("Fire20", "Fire Resistance 20", 2, "e7af6912cc308df4e9ee63c8824f2738"),
            ("Fire30", "Fire Resistance 30", 4, "0e98403449de8ce4c846361c6df30d1f"),
            ("SpellStoring", "Spell Storing", 1, null)
        };
        internal static readonly string[] Donors = NumericDonors.Concat(Options.Where(option => option.Guid != null)
            .Select(option => option.Guid)).Concat(new[] { MageArmor }).ToArray();

        internal static IEnumerable<string> Tokens() => new[] { "AegisAbility", "AegisStandardAbility", "AegisSwiftAbility",
            "AegisArmorBuff", "AegisOptions", "AegisPower" }.Concat(Occultist0099AegisStorage.Tokens)
            .Concat(Options.SelectMany(option => new[] { "Aegis" + option.Token + "Choice", "Aegis" + option.Token + "ChoiceBuff" }));

        internal static BlueprintScriptableObject[] Build(BlueprintsCache cache, Func<string, string, string, LocalizedString> text)
        {
            T Donor<T>(string id) where T : BlueprintScriptableObject
            {
                var guid = BlueprintGuid.Parse(id);
                var result = cache.Load(guid) as T;
                return result?.AssetGuid == guid ? result : null;
            }
            var icon = Donor<BlueprintAbility>(MageArmor);
            var numeric = NumericDonors.Select(Donor<BlueprintArmorEnchantment>).ToArray();
            if (icon?.name != "MageArmor" || numeric.Where((enchant, index) =>
                enchant?.GetComponent<ArmorEnhancementBonus>()?.EnhancementValue != index + 1).Any()) return null;
            var special = Options.Take(Options.Length - 1).Select(option => Donor<BlueprintArmorEnchantment>(option.Guid)).ToList();
            if (special.Any(enchant => enchant?.GetComponent<Kingmaker.Designers.Mechanics.EquipmentEnchants.AddUnitFeatureEquipment>() == null))
                return null;
            var storage = Occultist0099AegisStorage.Build(icon, text);
            special.Add(storage.OfType<BlueprintArmorEnchantment>().Single());

            const string desc = "Enhance your worn armor for 1 minute. Add to its existing enhancement up to +5, spending part of the enhancement budget on selected special properties. Spend 1 point of Abjuration focus as a standard action or 2 as a swift action.";
            var ward = Identify(new BlueprintBuff { Stacking = StackingType.Stack, IsClassFeature = true,
                ComponentsArray = new BlueprintComponent[] { new Occultist0099AegisArmor { name = "OccultistAegisEquipment",
                    Numeric = numeric.Select(Ref<BlueprintArmorEnchantmentReference>).ToArray() },
                    new LockEquipmentSlot { name = "OccultistAegisArmorLock" } } }, "AegisArmorBuff", "Aegis", desc, icon, text);
            var apply = Apply(ward, 1, DurationRate.Minutes, fromSpell: false);
            apply.IsNotDispelable = true;
            apply.AsChild = false;
            var standard = Activation("AegisStandardAbility", "Aegis", UnitCommand.CommandType.Standard, 1);
            var swift = Activation("AegisSwiftAbility", "Aegis (Swift)", UnitCommand.CommandType.Swift, 2);
            var root = Identify(new BlueprintAbility { Type = AbilityType.Supernatural, Range = AbilityRange.Personal,
                ActionType = UnitCommand.CommandType.Standard, CanTargetSelf = true, NotOffensive = true },
                "AegisAbility", "Aegis", desc, icon, text);
            var variants = new AbilityVariants { name = "OccultistAegisVariants" };
            Set(variants, "m_Variants", new[] { Ref<BlueprintAbilityReference>(standard), Ref<BlueprintAbilityReference>(swift) });
            root.ComponentsArray = new BlueprintComponent[] { variants };
            foreach (var cast in new[] { standard, swift }) Set(cast, "m_Parent", Ref<BlueprintAbilityReference>(root));

            var switches = new List<BlueprintScriptableObject>();
            var toggles = new List<BlueprintActivatableAbility>();
            for (int i = 0; i < Options.Length; i++)
            {
                var option = Options[i];
                string token = "Aegis" + option.Token + "Choice";
                string title = "Aegis - " + option.Label;
                string details = "Use " + option.Weight + " point(s) of enhancement on the next Aegis activation.";
                var choiceBuff = Identify(new BlueprintBuff { ComponentsArray = Array.Empty<BlueprintComponent>() },
                    token + "Buff", title, details, icon, text);
                SetEnum(choiceBuff, "m_Flags", "HiddenInUi");
                var toggle = Identify(new BlueprintActivatableAbility { ActivationType = AbilityActivationType.Immediately,
                    DeactivateImmediately = true, DoNotTurnOffOnRest = true, Group = ActivatableAbilityGroup.None,
                    ComponentsArray = new BlueprintComponent[] { new Occultist0099AegisOption
                    { name = "OccultistAegisOption", IgnoreIfStarted = true, Weight = option.Weight, Enchantment = Ref<BlueprintArmorEnchantmentReference>(special[i]) } } },
                    token, title, details, icon, text);
                Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(choiceBuff));
                switches.AddRange(new BlueprintScriptableObject[] { choiceBuff, toggle });
                toggles.Add(toggle);
            }
            var handbook = storage.OfType<BlueprintFeature>().Single(feature => feature.name.EndsWith("AegisStorageFact", StringComparison.Ordinal));
            var grants = new Occultist0099AegisGrants { name = "OccultistAegisGrants" };
            Set(grants, "m_Facts", toggles.Cast<Kingmaker.Blueprints.Facts.BlueprintUnitFact>()
                .Concat(new Kingmaker.Blueprints.Facts.BlueprintUnitFact[] { root, handbook })
                .Select(Ref<BlueprintUnitFactReference>).ToArray());
            var options = Identify(new BlueprintFeature { Ranks = 1, IsClassFeature = true, HideInUI = true,
                HideInCharacterSheetAndLevelUp = true, ComponentsArray = new BlueprintComponent[] { grants } },
                "AegisOptions", "Aegis Properties", "", icon, text);
            return new BlueprintScriptableObject[] { root, standard, swift, ward, options }.Concat(switches).Concat(storage).ToArray();

            BlueprintAbility Activation(string token, string title, UnitCommand.CommandType command, int cost)
                => Identify(new BlueprintAbility { Type = AbilityType.Supernatural, Range = AbilityRange.Personal,
                    ActionType = command, CanTargetSelf = true, NotOffensive = true,
                    ComponentsArray = new BlueprintComponent[]
                    {
                        new Occultist0099AegisApply { name = "OccultistApplyAegis", ApplyWard = CopyData(apply) },
                        new Occultist0099AegisTarget { name = "OccultistAegisArmorRequired" },
                        new SpellComponent { name = "OccultistAegisSchool", School = SpellSchool.Abjuration },
                        new Occultist0099FocusResourceLogic { name = "OccultistAegisCost", Amount = cost,
                            CheckCasterRestrictions = true, PayOnEffect = true }
                    } }, token, title, desc, icon, text);
        }

        internal static int Budget(UnitEntityData unit)
            => Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(unit, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component => ((Occultist0099FocusFact0099)component).AegisBudget(), 0);

        internal static IEnumerable<Occultist0099AegisOption> Selected(UnitEntityData unit, ActivatableAbility excluded = null)
            => unit.ActivatableAbilities.Enumerable.Where(ability => ability != excluded && ability.Active && ability.IsOn && ability.IsStarted)
                .Select(ability => ability.Blueprint.GetComponent<Occultist0099AegisOption>()).Where(option => option != null);

        internal static ItemEntityArmor Armor(UnitEntityData unit) => unit?.Body?.Armor?.MaybeArmor;
    }

    [TypeId(Occultist0099Ids.AegisOptionTypeId)]
    public sealed class Occultist0099AegisOption : ActivatableAbilityRestriction
    {
        public int Weight;
        public BlueprintArmorEnchantmentReference Enchantment;
        protected override bool IsAvailable() => Occultist0099Aegis.Selected(Owner, Fact).Sum(option => option.Weight) + Weight
            <= Occultist0099Aegis.Budget(Owner);
    }

    [TypeId(Occultist0099Ids.AegisGrantsTypeId)]
    public sealed class Occultist0099AegisGrants : AddFacts
    {
        protected override void OnDeactivate()
        {
            var ward = Owner.Buffs.Enumerable.FirstOrDefault(buff => buff.Blueprint.AssetGuid == Occultist0099Aegis.WardId);
            ward?.Remove();
            base.OnDeactivate();
        }
    }

    [TypeId(Occultist0099Ids.AegisTargetTypeId)]
    public sealed class Occultist0099AegisTarget : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
            => Occultist0099Aegis.Armor(caster) != null &&
                Occultist0099Aegis.Selected(caster).Sum(option => option.Weight) <= Occultist0099Aegis.Budget(caster);
        public string GetAbilityCasterRestrictionUIText() => "Wear armor and choose Aegis properties within the enhancement budget.";
    }

    [TypeId(Occultist0099Ids.AegisApplyTypeId)]
    public sealed class Occultist0099AegisApply : AbilityApplyEffect
    {
        public ContextActionApplyBuff ApplyWard;

        public override void Apply(AbilityExecutionContext context, TargetWrapper target)
        {
            var unit = context?.MaybeCaster;
            if (unit == null || target?.Unit != unit ||
                !(context.Ability.ResourceLogic is Occultist0099FocusResourceLogic logic) ||
                !Occultist0099NativeCast.TryGetPaymentIdentity(context, out var identity) || !logic.CanPay(context.Ability, identity)) return;

            var previous = unit.Buffs.Enumerable.FirstOrDefault(buff => buff.Blueprint.AssetGuid == Occultist0099Aegis.WardId);
            var previousData = previous?.Components.First(component => component.SourceBlueprintComponent is Occultist0099AegisArmor)
                .GetData<Occultist0099AegisArmorData>();
            using (context.GetDataScope(target)) ApplyWard.RunAction();
            var ward = unit.Buffs.Enumerable.FirstOrDefault(buff => buff.Blueprint.AssetGuid == Occultist0099Aegis.WardId &&
                buff != previous && buff.Context?.SourceAbilityContext == context);
            if (ward == null) return;

            var runtime = ward.Components.First(component => component.SourceBlueprintComponent is Occultist0099AegisArmor);
            bool paid = false;
            try
            {
                using (runtime.RequestEventContext())
                {
                    var equipment = (Occultist0099AegisArmor)runtime.SourceBlueprintComponent;
                    if (ward.IsTurnedOn && !ward.IsSuppressed && equipment.TrySetup(previousData) &&
                        logic.TryPay(context.Ability, identity))
                    {
                        paid = true;
                        equipment.KeepSharedEnchantments(previousData);
                    }
                }
            }
            finally { if (!paid) ward.Remove(); }
            if (paid) previous?.Remove();
        }
    }

    public sealed class Occultist0099AegisArmorData
    {
        [JsonProperty] public ItemEntityArmor Armor;
        [JsonProperty] public BlueprintArmorEnchantmentReference[] Desired = Array.Empty<BlueprintArmorEnchantmentReference>();
        [JsonProperty] public List<ItemEnchantment> Applied = new List<ItemEnchantment>();
    }

    [TypeId(Occultist0099Ids.AegisArmorTypeId)]
    public sealed class Occultist0099AegisArmor : UnitBuffComponentDelegate<Occultist0099AegisArmorData>,
        IUnitEquipmentHandler, IUnitSubscriber
    {
        public BlueprintArmorEnchantmentReference[] Numeric;

        internal bool TrySetup(Occultist0099AegisArmorData previous)
        {
            var armor = Occultist0099Aegis.Armor(Owner);
            if (armor == null) return false;
            var choices = Occultist0099Aegis.Selected(Owner).ToArray();
            int remaining = Occultist0099Aegis.Budget(Owner) - choices.Sum(option => option.Weight);
            if (remaining < 0) return false;
            var features = armor.Wielder?.State.Features;
            int fakeBonus = (bool)features?.AdvancedFakeEnhancement ? 2 : (bool)features?.FakeEnhancement ? 1 : 0;
            int baseline = EnhancementWithoutPreviousWard(armor, previous, fakeBonus);
            // Wrath adds the wielder's Magic Deceiver bonus to a new native
            // enchantment only when its raw value beats the current effective one.
            int numeric = Math.Min(5, baseline + remaining) - fakeBonus;
            Data.Armor = armor;
            Data.Desired = (numeric > baseline ? new[] { Numeric[numeric - 1] } : Array.Empty<BlueprintArmorEnchantmentReference>())
                .Concat(choices.Select(option => option.Enchantment)).ToArray();
            return TryAddMissing();
        }

        private static int EnhancementWithoutPreviousWard(ItemEntityArmor armor, Occultist0099AegisArmorData previous, int fakeBonus)
        {
            if (previous?.Armor != armor || previous.Applied.Count == 0)
                return Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(armor);
            int baseline = 0;
            foreach (var enchantment in armor.Enchantments)
            {
                if (previous.Applied.Contains(enchantment)) continue;
                var bonus = enchantment.GetComponent<ArmorEnhancementBonus>();
                if (bonus != null && bonus.EnhancementValue > baseline) baseline = bonus.EnhancementValue + fakeBonus;
            }
            return baseline;
        }

        internal void KeepSharedEnchantments(Occultist0099AegisArmorData previous)
        {
            if (previous?.Armor != Data.Armor) return;
            foreach (var enchantment in previous.Applied.ToArray())
            {
                var sameProperty = Data.Armor.Enchantments.Where(current => current.Blueprint == enchantment.Blueprint);
                if (!Data.Desired.Any(reference => reference.Get() == enchantment.Blueprint) ||
                    !sameProperty.Contains(enchantment) || sameProperty.Any(current => !previous.Applied.Contains(current))) continue;
                previous.Applied.Remove(enchantment);
                Data.Applied.Add(enchantment);
            }
        }

        protected override void OnTurnOn() { if (Data.Armor != null) Restore(); }
        protected override void OnApplyPostLoadFixes() { if (Buff.IsTurnedOn && !Buff.IsSuppressed) Restore(); }
        protected override void OnTurnOff() => Clear();

        private void Restore()
        {
            if (Data.Armor == null || Data.Armor != Occultist0099Aegis.Armor(Owner)) { Buff.Remove(); return; }
            if (!TryAddMissing()) Buff.Remove();
        }

        private bool TryAddMissing()
        {
            var items = new ItemEntity[] { Data.Armor };
            var desired = Data.Desired.Select(reference => reference.Get()).Cast<BlueprintItemEnchantment>().ToArray();
            if (desired.Any(enchant => enchant == null)) return false;
            if (MagusEnchantments01454.Current(items, desired, Data.Applied)) return true;
            Clear();
            try
            {
                MagusEnchantments01454.AddMissing(items, desired, Data.Applied, Context);
                return true;
            }
            catch (Exception failure)
            {
                // Native attachment can throw after inserting the enchantment into the item.
                foreach (var enchantment in Data.Armor.Enchantments.Where(enchantment => enchantment.ParentContext == Context))
                    if (!Data.Applied.Contains(enchantment)) Data.Applied.Add(enchantment);
                Clear();
                Kingmaker.PFLog.Default.Error("Occultist Aegis armor enchantment failed: " + failure);
                return false;
            }
        }

        private void Clear()
        {
            foreach (var enchant in Data.Applied.ToArray()) enchant?.Owner?.RemoveEnchantment(enchant);
            Data.Applied.Clear();
        }

        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity oldItem)
        {
            if (slot == Owner.Body.Armor && Data.Armor != Occultist0099Aegis.Armor(Owner)) Buff.Remove();
        }

    }
}
