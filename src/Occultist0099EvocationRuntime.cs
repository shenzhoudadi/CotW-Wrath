using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Newtonsoft.Json;

namespace CallOfTheWild.Wrath.Prototype
{
    public sealed class Occultist0099RadianceData
    {
        [JsonProperty] public ItemEntityWeapon Weapon;
        [JsonProperty] public List<ItemEnchantment> Applied = new List<ItemEnchantment>();
    }

    // Radiance follows the current main-hand melee weapon, unlike Legacy Weapon which
    // locks the weapon it was cast on. The enchantment application reuses the shared
    // equipment helper; the crit discharge is matched to the exact glowing instance so
    // an off-hand critical cannot consume it.
    [TypeId(Occultist0099Ids.RadianceBuffTypeId)]
    public sealed class Occultist0099RadianceBuff : UnitBuffComponentDelegate<Occultist0099RadianceData>,
        IUnitEquipmentHandler, IUnitSubscriber, IUnitActiveEquipmentSetHandler,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>, IInitiatorRulebookSubscriber
    {
        public BlueprintWeaponEnchantmentReference Enchantment;
        public BlueprintBuffReference Outline;

        protected override void OnTurnOn() => Reconcile();
        protected override void OnApplyPostLoadFixes() { if (Buff.IsTurnedOn && !Buff.IsSuppressed) Reconcile(); }
        protected override void OnTurnOff() => Clear();
        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity oldItem) => Reconcile();
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit) { if (unit.Unit == Owner) Reconcile(); }
        public void HandleUnitEmptyHandWeaponUpdated() => Reconcile();

        private void Reconcile()
        {
            if (!Buff.IsTurnedOn || Buff.IsSuppressed) return;
            var weapon = Owner.Body.PrimaryHand.MaybeWeapon;
            if (weapon == null || !weapon.Blueprint.IsMelee) { Clear(); return; }
            if (Data.Weapon != weapon) Clear();
            Data.Weapon = weapon;
            if (!Occultist0099EquipmentEnchantments.Refresh(weapon, new[] { Enchantment.Get() }, Data.Applied, Context, "Radiance"))
                Buff.Remove();
        }

        private void Clear()
        {
            Occultist0099EquipmentEnchantments.Clear(Data.Applied);
            Data.Weapon = null;
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            var outline = Outline?.Get();
            if (outline == null || Data.Weapon == null || evt.Weapon != Data.Weapon ||
                evt.AttackRoll == null || !evt.AttackRoll.IsCriticalConfirmed || evt.Target == null) return;
            var target = evt.Target;
            using (Context.GetDataScope(target))
                target.Descriptor.AddBuff(outline, Context, TimeSpan.FromSeconds(6 * Kingmaker.RuleSystem.RulebookEvent.Dice.D4));
            // The glow is spent by the discharge, exactly as the power describes.
            Buff.Remove();
        }
    }

    // Intense Focus adds flat damage of the same type to qualifying Evocation damage.
    // Per the contract it covers this class's private spellbook casts and its own
    // private focus powers, never items or a foreign book.
    [TypeId(Occultist0099Ids.IntenseFocusTypeId)]
    public sealed class Occultist0099IntenseFocus : UnitBuffComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateDamage>, IInitiatorRulebookSubscriber
    {
        private sealed class AppliedMark { }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RuleCalculateDamage, AppliedMark> Applied =
            new System.Runtime.CompilerServices.ConditionalWeakTable<RuleCalculateDamage, AppliedMark>();

        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            var ability = evt.Reason?.Context?.SourceAbilityContext?.Ability;
            if (ability == null || ability.SourceItem != null || ability.Blueprint.School != SpellSchool.Evocation) return;
            bool privateSpell = ability.Blueprint.Type == AbilityType.Spell &&
                Occultist0099NativeScope.IsPrivateBook(ability.Spellbook);
            bool privatePower = ability.Blueprint.GetComponent<Occultist0099FocusResourceLogic>()?.School == Occultist0099Evocation.School;
            if (!privateSpell && !privatePower) return;
            // A multi-projectile spell must not add the bonus once per projectile.
            if (evt.ParentRule?.Projectile != null && !evt.ParentRule.Projectile.IsFirstProjectile) return;
            int bonus = Occultist0099NativeScope.InRuntimeContext(
                Occultist0099NativeScope.RuntimeOf(Owner, Occultist0099Ids.FocusFactGuid, typeof(Occultist0099FocusFact0099)),
                component => ((Occultist0099FocusFact0099)component).WardingTalismanResonance(Occultist0099Evocation.School), 0);
            var first = evt.DamageBundle.First;
            if (bonus <= 0 || first == null || Applied.TryGetValue(evt, out _)) return;
            first.AddModifier(bonus, Fact);
            Applied.Add(evt, new AppliedMark());
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt) { }
    }
}