using System;
using System.Linq;
using Newtonsoft.Json;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using TurnBased.Controllers;

namespace CallOfTheWild.Wrath.Prototype
{
    [TypeId("bff5c2f39e394d90a53d759903b98e6f")]
    public sealed class SwordplayFocusPrerequisite : Prerequisite
    {
        protected override bool CheckInternal(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state) => unit != null && unit.Unit.Facts.List.OfType<Feature>().Any(f =>
            f.Blueprint.AssetGuid.ToString() == "1e1f627d26ad36f43bbd26cc2bf8ac7e" && T11TrainingOptions.Active(unit.Unit, f) && f.Param?.WeaponCategory.HasValue == true && SpellAndSwordFeats.Blades.Contains(f.Param.WeaponCategory.Value));
        protected override string GetUITextInternal(UnitDescriptor unit) => "Weapon Focus in a supported heavy/light blade category";
    }
    [TypeId("a6dad2bcda504e889f9892622f71488d")]
    public sealed class SwordplayWeaponRestriction : ActivatableAbilityRestriction
    {
        protected override bool IsAvailable() => SpellAndSwordFeats.Blade(Owner.Body.PrimaryHand.MaybeWeapon);
    }
    public sealed class SwordplayStrikeData
    {
        [JsonProperty] public bool Used;
        [JsonProperty] public TimeSpan Expires;
        [JsonIgnore] public RuleAttackWithWeapon Current;
    }
    [TypeId("7e907a6c03ca47b48539539b39e5fe73")]
    public sealed class SwordplayFirstStrike : UnitFactComponentDelegate<SwordplayStrikeData>, IInitiatorRulebookHandler<RuleAttackWithWeapon>, ITurnBasedModeHandler
    {
        internal static SwordplayStrikeData For(UnitEntityData unit) => unit.Facts.List.Where(f => f.Blueprint.AssetGuid == SpellAndSwordFeats.Ids[2] && T11TrainingOptions.Active(unit, f))
            .SelectMany(f => f.Components).FirstOrDefault(c => c.SourceBlueprintComponent is SwordplayFirstStrike)?.GetData<SwordplayStrikeData>();
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (!Fact.IsActive || !Fact.IsTurnedOn || Fact.IsDisposed || evt.Initiator != Owner || evt.IsAttackOfOpportunity) return;
            var now = Game.Instance.TimeController.GameTime;
            if (Data.Used && !CombatController.IsInTurnBasedCombat() && Data.Expires <= now) Data.Used = false;
            if (!Data.Used) { Data.Used = true; Data.Expires = now + TimeSpan.FromSeconds(6); Data.Current = evt; }
        }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { if (ReferenceEquals(Data.Current, evt)) Data.Current = null; }
        public void HandleTurnStarted(UnitEntityData unit) { if (unit == Owner) { Data.Used = false; Data.Current = null; } }
        public void HandleSurpriseRoundStarted() { }
        public void HandleRoundStarted(int round) { }
        public void HandleUnitControlChanged(UnitEntityData unit) { }
        public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck rule) { }
        protected override void OnApplyPostLoadFixes() => Data.Current = null;
    }
    public sealed class SwordplayStyleData { [JsonIgnore] public ModifiableValue.Modifier Armor; }
    [TypeId("00ecf728071349768bd6b11dfd8a1d35")]
    public sealed class SwordplayStyleEffect : UnitBuffComponentDelegate<SwordplayStyleData>, IInitiatorRulebookHandler<RuleCalculateAttackBonus>, ITargetRulebookHandler<RuleAttackWithWeapon>,
        IUnitBuffHandler, IUnitEquipmentHandler, IUnitActiveEquipmentSetHandler, IUnitSubscriber
    {
        bool Ready => Buff.IsActive && Buff.IsTurnedOn && !Buff.IsSuppressed && !Buff.IsDisposed && SpellAndSwordFeats.Has(Owner, 2) && SpellAndSwordFeats.Blade(Owner.Body.PrimaryHand.MaybeWeapon);
        void Clear() { foreach (var m in Owner.Stats.AC.Modifiers.Where(m => ReferenceEquals(m.Source, Fact)).ToArray()) Owner.Stats.AC.RemoveModifier(m); Data.Armor = null; }
        internal void Refresh()
        {
            bool armor = Ready && (SpellAndSwordFeats.Active(Owner, SpellAndSwordFeats.ExpertiseBuff) || SpellAndSwordFeats.Active(Owner, SpellAndSwordFeats.DefensiveBuff));
            if (armor && Data.Armor != null || !armor && Data.Armor == null) return;
            Clear(); if (armor) Data.Armor = Owner.Stats.AC.AddModifier(1, Fact, ModifierDescriptor.Shield);
        }
        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            var attack = Rulebook.CurrentContext.LastEvent<RuleAttackWithWeapon>();
            if (!Ready || evt.Initiator != Owner || !SpellAndSwordFeats.Blade(evt.Weapon) || attack == null || attack.Initiator != Owner || attack.Weapon != evt.Weapon || attack.IsAttackOfOpportunity ||
                !ReferenceEquals(SwordplayFirstStrike.For(Owner)?.Current, attack) || !SpellAndSwordFeats.Active(Owner, SpellAndSwordFeats.ExpertiseBuff)) return;
            var property = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse("8a63b06d20838954e97eb444f805ec89")) as BlueprintUnitProperty;
            int penalty = property?.GetInt(Owner) ?? 0;
            if (penalty > 0) evt.AddModifier(penalty, Fact, ModifierDescriptor.UntypedStackable);
        }
        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (!Ready || !SpellAndSwordFeats.Has(Owner, 3) || evt.Target != Owner || evt.Initiator == null || evt.AttackRoll == null || evt.AttackRoll.IsFake || evt.AttackRoll.IsHit ||
                evt.Weapon?.Blueprint.IsMelee != true || evt.Initiator.State.IsDead || evt.Initiator.IsAlly(Owner) || !Owner.State.CanAct || !Owner.CanAttack(evt.Initiator)) return;
            var ability = ResourcesLibrary.BlueprintsCache.Load(BlueprintGuid.Parse(SpellAndSwordFeats.FeintAbility)) as BlueprintAbility;
            if (ability == null || !new AbilityData(ability, Owner.Descriptor).CanTarget(new TargetWrapper(evt.Initiator))) return;
            using (Fact.MaybeContext.GetDataScope(evt.Initiator)) ability.GetComponent<AbilityEffectRunAction>()?.Actions.Run();
        }
        public void HandleBuffDidAdded(Buff buff) { if (buff.Owner == Owner) Refresh(); }
        public void HandleBuffDidRemoved(Buff buff) { if (buff.Owner == Owner) Refresh(); }
        public void HandleEquipmentSlotUpdated(ItemSlot slot, ItemEntity previousItem) => Refresh();
        public void HandleUnitChangeActiveEquipmentSet(UnitDescriptor unit) => Refresh();
        protected override void OnTurnOn() => Refresh();
        protected override void OnTurnOff() => Clear();
        protected override void OnApplyPostLoadFixes() { Clear(); Refresh(); }
    }
    [HarmonyLib.HarmonyPatch(typeof(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress), nameof(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress.ApplyChanges))]
    internal static class SwordplaySuppressionPatch
    {
        [HarmonyLib.HarmonyPostfix] static void Postfix(Kingmaker.UnitLogic.Parts.UnitPartBuffSuppress __instance)
        {
            var owner = __instance.Owner?.Unit;
            if (owner == null || owner.IsDisposed) return;
            foreach (var buff in owner.Buffs.Enumerable.Where(b => b.Blueprint.AssetGuid == PsychicAssets.Id("SwordplayStyleMode")))
                foreach (var runtime in buff.Components.Where(c => c.SourceBlueprintComponent is SwordplayStyleEffect))
                    using (runtime.RequestEventContext()) ((SwordplayStyleEffect)runtime.SourceBlueprintComponent).Refresh();
        }
    }
}
