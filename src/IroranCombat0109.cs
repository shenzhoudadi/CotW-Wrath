// COTW-0109 B2: Iroran Paladin custom mechanics. Every native donor blueprint
// stays immutable; these components add the archetype's own unarmed tiers,
// Confident Defense dodge, ki pool maximum, Personal Trial insight marks and
// the private Divine Body enchant pool.
// Adapted from Holic75/KingmakerRebalance (MIT), Archetypes/IroranPaladin.cs.
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("acc3ed34e68c36c594a028fdfbe041c8")]
    public sealed class IroranConfidentDefense0109 : UnitFactComponentDelegate,
        IUnitEquipmentHandler, IUnitActiveEquipmentSetHandler, IPolymorphActivatedHandler, IPolymorphDeactivatedHandler,
        IGlobalSubscriber, ISubscriber
    {
        public BlueprintCharacterClassReference Paladin;
        public BlueprintCharacterClassReference Monk;
        public BlueprintArchetypeReference ScaledFist;
        public BlueprintBuffReference ScaledFistCharismaAC;

        protected override void OnTurnOn() => Apply();

        protected override void OnTurnOff() => base.Owner.Stats.GetStat(StatType.AC)?.RemoveModifiersFrom(base.Runtime);

        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
        {
            if (slot?.Owner?.Unit == base.Owner) Apply();
        }

        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit)
        {
            if (unit?.Unit == base.Owner) Apply();
        }

        public void OnPolymorphActivated(UnitEntityData unit, Polymorph polymorph)
        {
            if (unit == base.Owner) Apply();
        }

        public void OnPolymorphDeactivated(UnitEntityData unit, Polymorph polymorph)
        {
            if (unit == base.Owner) Apply();
        }

        private void Apply()
        {
            var stat = base.Owner.Stats.GetStat(StatType.AC);
            if (stat == null) return;
            stat.RemoveModifiersFrom(base.Runtime);
            if (!Eligible()) return;
            int value = Value();
            if (value > 0) stat.AddModifier(value, base.Runtime, ModifierDescriptor.Dodge);
        }

        // Native MonkNoArmorFeatureUnlock equipment rules: polymorphed without
        // kept slots replaces the body, so it counts as being without armor.
        private bool Eligible()
        {
            UnitBody body = base.Owner.Body;
            if (body.IsPolymorphed && !body.IsPolymorphKeepSlots) return true;
            if (body.SecondaryHand.HasShield) return false;
            var armor = body.Armor;
            if (armor.HasArmor && armor.Armor.Blueprint.IsArmor)
                return armor.Armor.Blueprint.ProficiencyGroup == ArmorProficiencyGroup.Light;
            return true;
        }

        private int Value()
        {
            var charisma = base.Owner.Stats.GetStat(StatType.Charisma) as ModifiableValueAttributeStat;
            int modifier = charisma?.Bonus ?? 0;
            int cap = Math.Min(Math.Max(0, modifier), base.Owner.Progression.GetClassLevel(Paladin.Get()) + ScaledFistLevels());
            // Top up only when the native Scaled Fist Charisma AC is actually
            // effective; its higher value is always kept.
            var nativeBuff = ScaledFistCharismaAC?.Get();
            var buff = nativeBuff == null ? null : base.Owner.Buffs.GetBuff(nativeBuff);
            int native = buff != null && buff.IsEnabled ? Math.Max(0, modifier) : 0;
            return Math.Max(0, cap - native);
        }

        private int ScaledFistLevels()
        {
            var archetype = ScaledFist?.Get();
            if (archetype == null) return 0;
            var data = base.Owner.Progression.GetClassData(Monk.Get());
            return data != null && data.Archetypes.Contains(archetype) ? data.Level : 0;
        }
    }

    public sealed class IroranUnarmedData0109
    {
        [JsonProperty] public EntityFact AppliedFact;
    }

    [TypeId("2cdbb8717a1020c3f53bbb09526a4a86")]
    public sealed class IroranUnarmed0109 : UnitFactComponentDelegate<IroranUnarmedData0109>, IOwnerGainLevelHandler
    {
        // One tier fact is held per effective level; the tier facts themselves
        // carry native EmptyHandWeaponOverride components on the native fists.
        public static readonly int[] Thresholds = { 1, 4, 8, 12, 16, 20 };

        public BlueprintCharacterClassReference Paladin;
        public BlueprintCharacterClassReference Monk;
        public BlueprintUnitFactReference[] Tiers;
        public BlueprintUnitFactReference MonkUnarmedStrike;

        protected override void OnActivate() => Refresh();

        protected override void OnDeactivate() => Retract();

        public void HandleUnitGainLevel() => Refresh();

        private void Refresh()
        {
            var desired = Tier();
            var applied = base.Data.AppliedFact;
            if (applied != null && !applied.IsDisposed && applied.Blueprint == desired) return;
            Retract();
            if (desired != null) base.Data.AppliedFact = base.Owner.AddFact(desired);
        }

        private void Retract()
        {
            var applied = base.Data.AppliedFact;
            if (applied == null) return;
            base.Data.AppliedFact = null;
            if (!applied.IsDisposed) base.Owner.RemoveFact(applied);
        }

        private BlueprintUnitFact Tier()
        {
            int effective = base.Owner.Progression.GetClassLevel(Paladin.Get());
            var monkFist = MonkUnarmedStrike?.Get();
            if (monkFist != null && base.Owner.HasFact(monkFist))
                effective += base.Owner.Progression.GetClassLevel(Monk.Get());
            BlueprintUnitFact result = null;
            for (int i = 0; i < Tiers.Length && i < Thresholds.Length; i++)
                if (effective >= Thresholds[i]) result = Tiers[i]?.Get();
            return result;
        }
    }

    public sealed class IroranKiData0109
    {
        [JsonProperty] public bool FirstGrant;
    }

    [TypeId("110825ca74a46cbc00e6fb3c1624ea2f")]
    public sealed class IroranKi0109 : UnitFactComponentDelegate<IroranKiData0109>, IResourceAmountBonusHandler
    {
        public BlueprintCharacterClassReference Paladin;
        public BlueprintCharacterClassReference Monk;
        public BlueprintAbilityResourceReference Ki;
        public BlueprintUnitFactReference ScaledFistKi;

        protected override void OnActivate()
        {
            var ki = Ki?.Get();
            if (ki == null) return;
            // A second source only retains the shared pool: no separate
            // balance is opened and an existing balance is never refilled.
            bool first = !base.Owner.Resources.ContainsResource(ki);
            base.Owner.Resources.Add(ki, first);
            base.Data.FirstGrant = first;
        }

        protected override void OnTurnOn()
        {
            // The native AddAbilityResources-style first fill runs before this
            // fact's handlers are subscribed, so it cannot see our maximum.
            if (!base.Data.FirstGrant) return;
            base.Data.FirstGrant = false;
            base.Owner.Resources.Restore(Ki.Get());
        }

        protected override void OnDeactivate()
        {
            var ki = Ki?.Get();
            if (ki != null) base.Owner.Resources.Remove(ki);
        }

        public void CalculateMaxResourceAmount(BlueprintAbilityResource resource, ref int bonus)
        {
            if (!base.Fact.Active || resource != Ki?.Get()) return;
            int level = base.Owner.Progression.GetClassLevel(Paladin.Get());
            if (level < 4) return;
            int monk = base.Owner.Progression.GetClassLevel(Monk.Get());
            int scaled = base.Owner.HasFact(ScaledFistKi?.Get()) ? monk : 0;
            // The native pool already counts every monk level through its own
            // DivStep, so only the difference to floor((L + F) / 2) is added.
            bonus += (level + scaled) / 2 - monk / 2;
        }
    }

    [TypeId("e8d8ac069fe63e101e6f8cc9a2e12719")]
    public sealed class IroranPersonalTrial0109 : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleSavingThrow>, IInitiatorRulebookSubscriber
    {
        public BlueprintBuffReference Mark;
        public BlueprintCharacterClassReference Paladin;

        public void OnEventAboutToTrigger(RuleSavingThrow evt)
        {
            var mark = Mark?.Get();
            var caster = evt.Reason?.Caster;
            if (mark == null || caster == null) return;
            if (!caster.Buffs.Enumerable.Any(b => b.Blueprint == mark && b.IsEnabled && b.Context?.MaybeCaster == base.Owner)) return;
            int value = 1 + base.Owner.Progression.GetClassLevel(Paladin.Get()) / 4;
            var stats = base.Owner.Stats;
            // UnitPartSavingThrowsReplace can swap the stat after this trigger,
            // so all three saves carry the temporary modifier.
            evt.AddTemporaryModifier(stats.SaveWill.AddModifier(value, base.Runtime, ModifierDescriptor.Insight));
            evt.AddTemporaryModifier(stats.SaveReflex.AddModifier(value, base.Runtime, ModifierDescriptor.Insight));
            evt.AddTemporaryModifier(stats.SaveFortitude.AddModifier(value, base.Runtime, ModifierDescriptor.Insight));
        }

        public void OnEventDidTrigger(RuleSavingThrow evt) { }

        protected override void OnDeactivate()
        {
            var mark = Mark?.Get();
            var unique = base.Owner.Get<UnitPartUniqueBuffs>();
            if (mark == null || unique == null) return;
            foreach (var buff in unique.Buffs.Where(b => b != null && b.Blueprint == mark).ToArray())
            {
                unique.RemoveBuff(buff);
                buff.Remove();
            }
        }
    }

    [TypeId("fd559b389d1d4ce772b40f8a0a9eb0ff")]
    public sealed class IroranTrialMark0109 : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleDealDamage>, IInitiatorRulebookSubscriber
    {
        public BlueprintBuffReference Mark;

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            var mark = Mark?.Get();
            var target = evt?.Target;
            if (mark == null || target == null) return;
            if (!target.Buffs.Enumerable.Any(b => b.Blueprint == mark && b.IsEnabled && b.Context?.MaybeCaster == base.Owner)) return;
            // Damage reduction only: resistances and other reductions keep
            // their native rules, and other units' attacks are untouched.
            evt.IgnoreDamageReduction = true;
        }

        public void OnEventDidTrigger(RuleDealDamage evt) { }
    }

    [TypeId("a89cb716942c0f51bfe6629e190cf58b")]
    public sealed class IroranDivineBody0109 : ContextAction
    {
        public ActivatableAbilityGroup Group;
        public BlueprintItemEnchantmentReference[] DefaultEnchantments = new BlueprintItemEnchantmentReference[5];
        public ContextDurationValue DurationValue;

        public override string GetCaption() => "Add Iroran Divine Body enchants to the caster's unarmed strike";

        public override void RunAction()
        {
            UnitEntityData unit = base.Target.Unit;
            if (unit == null) return;
            var part = unit.Ensure<IroranDivineBodyPart0109>();
            part.ClearOwn();
            // Same real main-hand resolution as the native bond action, so
            // preview, application and caster check agree on the weapon.
            HandSlot handSlot = unit.Body.PrimaryHand;
            ItemEntityWeapon weapon = handSlot.HasWeapon ? handSlot.MaybeWeapon : unit.Body.EmptyHandWeapon;
            if (weapon == null) return;
            int enhancement = GameHelper.GetItemEnhancementBonus(weapon);
            int budget = unit.Ensure<UnitPartActivatableAbility>().GetGroupSize(Group);
            Rounds duration = DurationValue.Calculate(base.Context);
            foreach (AddBondProperty bond in unit.Buffs.Enumerable.SelectMany(buff => buff.SelectComponents<AddBondProperty>()))
            {
                if (bond.EnchantPool != EnchantPoolType.DivineWeaponBond || bond.Enchant == null) continue;
                if (weapon.HasEnchantment(bond.Enchant)) continue;
                budget -= bond.Enchant.EnchantmentCost;
                part.AddEnchant(weapon, bond.Enchant, base.Context, duration);
            }
            int plus = Math.Min(Math.Max(0, 5 - enhancement), budget);
            if (plus > 0)
            {
                var enchantment = DefaultEnchantments[plus - 1]?.Get();
                if (enchantment != null) part.AddEnchant(weapon, enchantment, base.Context, duration);
            }
        }
    }

    [TypeId("a49e81d21e73d3fb838a1f0f14fd966c")]
    public sealed class IroranDivineBodyCasterCheck0109 : BlueprintComponent, IAbilityCasterRestriction
    {
        public bool IsCasterRestrictionPassed(UnitEntityData caster)
        {
            if (caster == null) return false;
            ItemEntityWeapon weapon = caster.Body.PrimaryHand.MaybeWeapon ?? caster.Body.EmptyHandWeapon;
            return weapon != null && weapon.Blueprint.Type.Category == WeaponCategory.UnarmedStrike;
        }

        public string GetAbilityCasterRestrictionUIText() => "Requires an unarmed strike";
    }

    [TypeId("f390fe66e1f702c5048688f1e47ab9c5")]
    public sealed class IroranDivineBodyCleanup0109 : UnitFactComponentDelegate
    {
        protected override void OnDeactivate() => base.Owner.Get<IroranDivineBodyPart0109>()?.ClearOwn();
    }

    public sealed class IroranDivineBodyPart0109 : UnitPart, IUnitEquipmentHandler, IUnitEmptyHandWeaponHandler, IGlobalSubscriber, ISubscriber
    {
        public sealed class Tracked
        {
            [JsonProperty] public EntityRef<ItemEntity> ItemRef;
            [JsonProperty] public readonly List<string> Enchantments = new List<string>();
        }

        [JsonProperty] private readonly List<Tracked> m_Tracked = new List<Tracked>();

        public void AddEnchant(ItemEntity item, BlueprintItemEnchantment enchantment, MechanicsContext parentContext, Rounds duration)
        {
            if (item == null || enchantment == null) return;
            Tracked tracked = m_Tracked.FirstOrDefault(i => i.ItemRef == item);
            if (tracked == null)
            {
                tracked = new Tracked { ItemRef = item };
                m_Tracked.Add(tracked);
            }
            ItemEnchantment added = item.AddEnchantment(enchantment, parentContext, duration);
            if (added != null) tracked.Enchantments.Add(added.UniqueId);
        }

        // Only the enchantment facts this part itself created are removed;
        // other sources' pool entries and permanent item facts are untouched.
        public void ClearOwn()
        {
            foreach (Tracked tracked in m_Tracked.ToArray()) Remove(tracked);
        }

        private void Remove(Tracked tracked)
        {
            var entity = tracked.ItemRef.Entity;
            foreach (string id in tracked.Enchantments.ToArray())
                try
                {
                    var fact = entity?.Facts.FindById(id);
                    if (fact != null) entity.Facts.Remove(fact);
                }
                catch (Exception) { }
            m_Tracked.Remove(tracked);
        }

        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem)
        {
            if (slot?.Owner?.Unit != base.Owner) return;
            foreach (Tracked tracked in m_Tracked.ToArray())
                if (tracked.ItemRef == previousItem) Remove(tracked);
        }

        public void HandleUnitEmptyHandWeaponUpdated()
        {
            // The native pool part does not watch empty-hand swaps; a fist
            // change retires this package's enchants rather than migrating
            // them to the new fist or any equipped weapon.
            ClearOwn();
        }

        protected override void OnPreSave()
        {
            base.OnPreSave();
            m_Tracked.RemoveAll(i => i.ItemRef.Entity == null);
        }
    }
}
