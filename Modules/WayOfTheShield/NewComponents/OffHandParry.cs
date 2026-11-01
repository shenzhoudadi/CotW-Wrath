using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Kingmaker;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json;
using System.Reflection;
using TurnBased.Controllers;
using System.Text;

namespace Way_of_the_shield.NewComponents
{
    public static class OffHandParry
    {
        public static BlueprintItemWeapon LightShieldWeapon => ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>("62c90581f9892e9468f0d8229c7321c4");

        public sealed class OffHandParryData
        {
            [JsonProperty] public bool Initialized;
            [JsonProperty] public List<int> AttackPenalties = new();
            [JsonProperty] public int Used;
        }

        // Runtime links are reconstructed from the persisted buff component data.
        [TypeId("5cba6db2b5cb41238fd66d884ef59365")]
        public class OffHandParryUnitPart : OldStyleUnitPart, ITargetRulebookHandler<RuleAttackRoll>, IInventoryHandler, ITurnBasedModeHandler, IGlobalSubscriber
        {
            [JsonProperty] public OffHandParryData TurnState;
            [JsonProperty] public TimeSpan TurnExpires;
            public WeaponCategory? Category;
            public bool activated, riposte;
            public static bool flag;
            public static UnitEntityData CalculatingFor;
            public static bool IsCalculatingFor(UnitEntityData unit) => flag && ReferenceEquals(CalculatingFor, unit);
            public ItemEntityWeapon weapon;
            public bool Triggered;
            public OffHandParryData State;
            OffHandParryData componentState;
            bool retained;
            RuleAttackRoll claimed;
            public int n => State == null ? 0 : Math.Max(0, (State.AttackPenalties?.Count ?? 0) - State.Used);
            public bool HasParries => activated && n > 0;

            public void TryActivate(WeaponCategory category, bool counterattack, OffHandParryData state)
            {
                if (activated && ReferenceEquals(State, state)) return;
                Deactivate();
                bool previous = flag;
                var previousUnit = CalculatingFor;
                flag = true;
                CalculatingFor = Owner.Unit;
                try
                {
                    var w = Owner?.Body?.SecondaryHand.MaybeWeapon;
                    if (w?.Blueprint.Type.Category != category || state == null) return;
                    var now = Game.Instance.TimeController.GameTime;
                    if (TurnState == null) { TurnState = state; TurnExpires = now + new Rounds(1).Seconds; }
                    else if (!CombatController.IsInTurnBasedCombat() && now >= TurnExpires) { TurnState = new OffHandParryData(); TurnExpires = now + new Rounds(1).Seconds; }
                    Category = category; weapon = w; State = TurnState; componentState = state; riposte = counterattack;
                    if (!State.Initialized)
                    {
                        var rule = Rulebook.Trigger(new RuleCalculateAttacksCount(Owner));
                        State.AttackPenalties = UnitAttack.EnumerateAttacks(rule, false)
                            .Where(x => x.Hand == Owner.Body.SecondaryHand).Select(x => x.AttackBonusPenalty).ToList();
                        State.Used = 0; State.Initialized = true;
                    }
                    State.AttackPenalties ??= new List<int>();
                    State.Used = Math.Max(0, Math.Min(State.Used, State.AttackPenalties.Count));
                    SynchronizeComponent();
                    var mechanics = Owner.Unit.Ensure<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>();
                    mechanics.ShieldDenied.Retain(); mechanics.ForceDualWieldingPenalties.Retain(); retained = true;
                    activated = true;
                    ShieldAcState.Refresh(Owner);
                    // The original attack command may not exist during load or passive activation.
                    var attack = Owner.Unit.Commands?.Attack;
                    if (attack != null && AccessTools.Field(typeof(UnitAttack), "m_AllAttacks")?.GetValue(attack) is List<AttackHandInfo> attacks)
                        attacks.RemoveAll(x => x.Hand == Owner.Body.SecondaryHand);
                }
                catch
                {
                    Deactivate(); throw;
                }
                finally { flag = previous; CalculatingFor = previousUnit; }
            }
            public void Deactivate()
            {
                if (retained)
                {
                    var mechanics = Owner?.Unit.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>();
                    mechanics?.ShieldDenied.Release(); mechanics?.ForceDualWieldingPenalties.Release(); retained = false;
                }
                Category = null; activated = false; riposte = false; weapon = null; Triggered = false; claimed = null; State = null; componentState = null;
                ShieldAcState.Refresh(Owner);
            }
            public void TryDeactivate(WeaponCategory category) => Deactivate();
            public void Refresh()
            {
                var hand = Owner?.Body?.SecondaryHand;
                var equippedWeapon = hand?.MaybeShield?.WeaponComponent ?? hand?.MaybeWeapon;
                if (activated && (equippedWeapon != weapon ||
                    (!CombatController.IsInTurnBasedCombat() && Game.Instance.TimeController.GameTime >= TurnExpires))) Deactivate();
            }
            public void TryEquip(ItemSlotVM slot) { }
            public void TryDrop(ItemSlotVM slot) { }
            public void OnEventAboutToTrigger(RuleAttackRoll evt)
            {
                Refresh();
                if (evt == null || evt.IsFake || evt.Parry != null || evt.Weapon?.Blueprint.IsMelee != true || !activated || Owner?.State.CanAct != true || !HasParries) return;
                if (Rulebook.Trigger(new RuleCheckTargetFlatFooted(evt.Initiator, evt.Target)).IsFlatFooted) return;
                evt.TryParry(Owner, weapon, State.AttackPenalties[State.Used]);
                claimed = evt; Triggered = true;
                int difference = evt.Initiator.Descriptor.State.Size - Owner.State.Size;
                if (difference > 0) evt.AddTemporaryModifier(Owner.Stats.AdditionalAttackBonus.AddModifier(-4 * difference, ModifierDescriptor.Penalty));
            }
            public void OnEventDidTrigger(RuleAttackRoll evt)
            {
                if (!ReferenceEquals(claimed, evt)) return;
                claimed = null;
                if (activated && Triggered && evt.Parry?.Initiator == Owner && evt.Parry.IsTriggered && State != null)
                {
                    State.Used = Math.Min(State.AttackPenalties.Count, State.Used + 1);
                    SynchronizeComponent();
                    if (evt.Result == AttackResult.Parried && riposte) Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(Owner, evt.Initiator, false);
                }
                Triggered = false;
            }
            void SynchronizeComponent() { if (componentState != null && State != null) { componentState.AttackPenalties = State.AttackPenalties; componentState.Used = State.Used; componentState.Initialized = State.Initialized; } }
            public void HandleTurnStarted(UnitEntityData unit)
            {
                if (unit != Owner.Unit) return;
                Deactivate(); TurnState = new OffHandParryData(); TurnExpires = Game.Instance.TimeController.GameTime + new Rounds(1).Seconds;
            }
            public void HandleSurpriseRoundStarted() { }
            public void HandleRoundStarted(int round) { }
            public void HandleUnitControlChanged(UnitEntityData unit) { }
            public void HandleUnitNotSurprised(UnitEntityData unit, RuleSkillCheck check) { }
        }

        [AllowedOn(typeof(BlueprintUnitFact), false)]
        [TypeId("ba113232f466438cadd7263f03a536be")]
        public class OffHandParryComponent : UnitFactComponentDelegate<OffHandParryData>
        {
            public WeaponCategory category;
            public bool riposte;
            protected override void OnActivate() => Owner.Parts.Ensure<OffHandParryUnitPart>().TryActivate(category, riposte, Data);
            protected override void OnTurnOn() { if (Fact.IsActive) Owner.Parts.Ensure<OffHandParryUnitPart>().TryActivate(category, riposte, Data); }
            protected override void OnApplyPostLoadFixes() { if (Fact.IsActive && Fact.IsTurnedOn) Owner.Parts.Ensure<OffHandParryUnitPart>().TryActivate(category, riposte, Data); }
            protected override void OnDeactivate() => Owner.Parts.Get<OffHandParryUnitPart>()?.Deactivate();
            protected override void OnTurnOff() => Owner.Parts.Get<OffHandParryUnitPart>()?.Deactivate();
        }

        [HarmonyPatch(typeof(RuleCalculateAttacksCount), nameof(RuleCalculateAttacksCount.PrepareHandVariables))]
        public static class RuleCalculateAttacksCount_OnTrigger_patch
        {

            [HarmonyPatch(typeof(RuleCalculateAttacksCount), nameof(RuleCalculateAttacksCount.OnTrigger))]
            [HarmonyPostfix]
            public static void Postfix(RuleCalculateAttacksCount __instance)
            {
                OffHandParryUnitPart part = __instance.Initiator?.Parts.Get<OffHandParryUnitPart>();
                if (part is not null && part.activated && !OffHandParryUnitPart.IsCalculatingFor(__instance.Initiator))
                {
                    RuleCalculateAttacksCount.AttacksCount result = __instance.Result.SecondaryHand;
                    result.AdditionalAttacks = 0;
                    result.HasteAttacks = 0;
                    result.PenalizedAttacks = 0;
                }
            }


            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
#if DEBUG
                if (Settings.Debug.GetValue())
                    Comment.Log("RuleCalculateAttacksCount PrepareHandVariables transpiler"); 
#endif
                List<CodeInstruction> _instructions = instructions.ToList();

                CodeInstruction[] toSearch =
                    new CodeInstruction[]
                    {
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitDescriptor).GetField(nameof(UnitDescriptor.State))),
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitState).GetField(nameof(UnitState.Features))),
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitMechanicFeatures).GetField(nameof(UnitMechanicFeatures.ShieldBash))),
                        new CodeInstruction(OpCodes.Call),
                        new CodeInstruction(OpCodes.Brtrue_S)
                    };

                int index = IndexFinder(instructions, toSearch);
                if (index == -1) { return instructions; };

                _instructions.InsertRange(index - 1,
                    new CodeInstruction[]
                    {
                        new CodeInstruction (OpCodes.Ldarg_0),
                        CodeInstruction.Call(typeof(OffHandParry), nameof(OffHandParry.BucklerOrBash))
                    });

                return _instructions;

            }

//            [HarmonyPatch(nameof(RuleCalculateAttacksCount.AddExtraAttacks))]
//            [HarmonyTranspiler]
//            public static IEnumerable<CodeInstruction> Transpiler2(IEnumerable<CodeInstruction> instructions)
//            {
//#if DEBUG
//                if (Settings.Debug.GetValue())
//                    Comment.Log("Entered RuleCalculateAttacksCount AddExtraAttacks transpiler"); 
//#endif
//                List<CodeInstruction> _instructions = instructions.ToList();

//                CodeInstruction[] toSearch =
//                    new CodeInstruction[]
//                    {
//                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitDescriptor).GetField(nameof(UnitDescriptor.State))),
//                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitState).GetField(nameof(UnitState.Features))),
//                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitMechanicFeatures).GetField(nameof(UnitMechanicFeatures.ShieldBash))),
//                        new CodeInstruction(OpCodes.Call),
//                        new CodeInstruction(OpCodes.Brtrue_S)
//                    };

//                int index = IndexFinder(instructions, toSearch);
//                if (index == -1) { return instructions; };

//                _instructions.InsertRange(index - 1,
//                    new CodeInstruction[]
//                    {
//                        new CodeInstruction (OpCodes.Ldarg_0),
//                        CodeInstruction.Call(typeof(OffHandParry), nameof(OffHandParry.BucklerOrBash))
//                    });

//                return _instructions;

//            }

            [HarmonyPatch(typeof(HandSlot), nameof(HandSlot.MaybeWeapon), MethodType.Getter)]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler4(IEnumerable<CodeInstruction> instructions)
            {
#if DEBUG
                if (Settings.Debug.GetValue())
                    Comment.Log("Entered HandSlot MaybeWeapon getter transpiler"); 
#endif
                List<CodeInstruction> _instructions = instructions.ToList();

                CodeInstruction[] toSearch =
                    new CodeInstruction[]
                    {
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitDescriptor).GetField(nameof(UnitDescriptor.State))),
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitState).GetField(nameof(UnitState.Features))),
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitMechanicFeatures).GetField(nameof(UnitMechanicFeatures.ShieldBash))),
                        new CodeInstruction(OpCodes.Call),
                        new CodeInstruction(OpCodes.Brtrue_S)
                    };

                int index = IndexFinder(instructions, toSearch);
                if (index == -1) { return instructions; };

                _instructions.InsertRange(index -1,
                    new CodeInstruction[]
                    {
                        //new CodeInstruction (OpCodes.Ldsfld, typeof(OffHandParryUnitPart).GetField(nameof(OffHandParryUnitPart.flag))),
                        new CodeInstruction( OpCodes.Ldarg_0),
                        CodeInstruction.Call(typeof(OffHandParry), nameof(BucklerOrBash4)),
                        //new CodeInstruction(_instructions[index -1]),
                    });

                return _instructions;

            }


            [HarmonyPatch(typeof(CharSheetWeapons), nameof(CharSheetWeapons.Initialize))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler3(IEnumerable<CodeInstruction> instructions)
            {
#if DEBUG
                if (Settings.Debug.GetValue())
                    Comment.Log("CharSheetWeapons Initialize transpiler"); 
#endif
                List<CodeInstruction> _instructions = instructions.ToList();

                CodeInstruction[] toSearch =
                    new CodeInstruction[]
                    {
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitDescriptor).GetField(nameof(UnitDescriptor.State))),
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitState).GetField(nameof(UnitState.Features))),
                        new CodeInstruction(OpCodes.Ldfld, typeof(UnitMechanicFeatures).GetField(nameof(UnitMechanicFeatures.ShieldBash))),
                        new CodeInstruction(OpCodes.Call),
                        new CodeInstruction(OpCodes.Brtrue_S)
                    };

                int index = IndexFinder(instructions, toSearch);
                if (index == -1) { return instructions; };

                _instructions.InsertRange(index - 1,
                    new CodeInstruction[]
                    {
                        new CodeInstruction (OpCodes.Ldarg_1),
                        CodeInstruction.Call(typeof(OffHandParry), nameof(OffHandParry.BucklerOrBash3))
                    });

                return _instructions;

            }


        }

        public static bool BucklerOrBash(bool HasShieldBash, RuleCalculateAttacksCount evt)
        {
            bool result = OffHandParryUnitPart.IsCalculatingFor(evt.Initiator)
                || (HasShieldBash && evt.Initiator.Body.SecondaryHand.HasShield && (evt.Initiator.Body.SecondaryHand.Shield.Blueprint.Type.ProficiencyGroup != ArmorProficiencyGroup.Buckler
                                        || (evt.Initiator.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.BucklerBash
                                                && (!(evt.Initiator.Body.PrimaryHand.Weapon?.HoldInTwoHands ?? false) || (evt.Initiator.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.UnhinderingShield || AllowBucklerBashWhenTwoHandedWithUnhindering.GetValue()))
                                           )
                                      )
                    );

#if DEBUG
            var sb = new StringBuilder().Append($"BucklerOrBash - Parry flag  is {OffHandParryUnitPart.flag}, HasShieldBash is {HasShieldBash}. ");
            if (evt.Initiator.Body.SecondaryHand.MaybeShield is not ItemEntityShield shield)
            {
                sb.Append("No shield in secondary hand.");
            }
            else
            {
                sb.Append($"Uses shield {shield.Name} (proficiency group {shield.Blueprint.Type.ProficiencyGroup}). "); 
                if (shield.Blueprint.Type.ProficiencyGroup != ArmorProficiencyGroup.Buckler)
                    goto Print;
                sb.AppendLine();
                sb.Append($"Has BucklerBash? {evt.Initiator.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.BucklerBash}.");
                var twoHanded = evt.Initiator.Body.PrimaryHand.Weapon?.HoldInTwoHands ?? false;
                sb.Append($"Primary hand is hold with both hands? {twoHanded}");
                if (twoHanded)
                {
                    sb.AppendLine();
                    sb.Append($"Has Unhindering Shield? {evt.Initiator.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.UnhinderingShield}. ");
                    sb.Append($"Stupid setting is enabled? {AllowBucklerBashWhenTwoHandedWithUnhindering.GetValue()}");
                }
            }
            sb.AppendLine();

            Print:
            Comment.Log(sb.Append($"Result is {result}").ToString());
#endif
            return result;
        }
        public static bool BucklerOrBash4(bool flag, HandSlot slot)
        {
            if (flag is true) return flag;
            OffHandParryUnitPart part = slot.Owner.Get<OffHandParryUnitPart>();
            if (part is null || !OffHandParryUnitPart.IsCalculatingFor(slot.Owner.Unit)) return false;
            //Comment.Log("Went through checks for BucklerOrBash4");
            return true;
        }
        public static bool BucklerOrBash3(bool flag, UnitEntityData unit)
        {
            if (flag is false) return flag;
            return flag && (unit.Body.SecondaryHand.Shield.Blueprint.Type.ProficiencyGroup != ArmorProficiencyGroup.Buckler 
                                || (unit.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.BucklerBash
                                    && (!(unit.Body.PrimaryHand.Weapon?.HoldInTwoHands ?? false) || unit.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.UnhinderingShield)
                                )
                            );
        }


        [HarmonyPatch(typeof(ItemEntityShield))]
        public static class ItemEntityShield_patch_WeaponComponentForBucklers
        {
            static void EnsureWeapon(ItemEntityShield shield)
            {
                if (shield?.Blueprint.Type.ProficiencyGroup != ArmorProficiencyGroup.Buckler || LightShieldWeapon == null) return;
                var weapon = shield.WeaponComponent;
                if (weapon == null)
                {
                    weapon = new ItemEntityWeapon(LightShieldWeapon, shield);
                    AccessTools.PropertySetter(typeof(ItemEntityShield), nameof(ItemEntityShield.WeaponComponent)).Invoke(shield, new object[] { weapon });
                }
                weapon.HoldingSlot = shield.HoldingSlot;
                if (shield.Wielder != null && weapon.Wielder != shield.Wielder) weapon.OnDidEquipped(shield.Wielder.Unit);
            }
            [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(BlueprintItemShield) })]
            [HarmonyPostfix]
            public static void Postfix_Constructor(ItemEntityShield __instance) => EnsureWeapon(__instance);
            [HarmonyPatch("OnTurnOn")]
            [HarmonyPrefix]
            public static void Prefix_OnTurnOn(ItemEntityShield __instance) => EnsureWeapon(__instance);
            [HarmonyPatch("OnPostLoad")]
            [HarmonyPostfix]
            public static void Postfix_OnPostLoad(ItemEntityShield __instance) => EnsureWeapon(__instance);
            [HarmonyPatch(nameof(ItemEntityShield.OnDidEquipped))]
            [HarmonyPostfix]
            public static void Postfix_OnDidEquipped(ItemEntityShield __instance) => EnsureWeapon(__instance);
        }
    }
}
