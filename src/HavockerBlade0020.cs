// K01 Q-04/Q-09: native blade activation pays once; weapon-delivered child
// contexts alias that receipt, not a second fee/charge system.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class HavockerBlade0020
    {
        sealed class Link {
            internal ItemEntityWeapon Weapon;
            internal HavockerSpellburn0020.CastRecord Receipt;
            internal RuleAttackWithWeapon FirstAttack;
            internal UnitCommand Command;
            internal AbilityExecutionContext CleaveContext;
            internal object Lifetime;
        }
        sealed class Frame : IDisposable {
            internal Link Link;
            internal Frame Previous;
            public void Dispose() { if (ReferenceEquals(Current, this)) Current = Previous; }
        }
        static readonly ConditionalWeakTable<UnitDescriptor, Link> Activations = new ConditionalWeakTable<UnitDescriptor, Link>();
        static readonly ConditionalWeakTable<AbilityExecutionContext, Link> Whirlwinds = new ConditionalWeakTable<AbilityExecutionContext, Link>();
        static readonly ConditionalWeakTable<UnitDescriptor, object> Lifetimes = new ConditionalWeakTable<UnitDescriptor, object>();
        sealed class OpportunityRun { internal UnitCommand.ResultType Result = UnitCommand.ResultType.Fail; }
        static readonly ConditionalWeakTable<UnitAttackOfOpportunity, OpportunityRun> Opportunities = new ConditionalWeakTable<UnitAttackOfOpportunity, OpportunityRun>();
        [ThreadStatic] static Frame Current;
        static bool OwnPart(UnitDescriptor owner) => HavockerSpellburn0020.With(owner, c => c.OwnPart);
        static WeaponKineticBlade OwnBlade(ItemEntityWeapon weapon) => Havocker0020.OwnNode(weapon?.Blueprint) ? weapon.Blueprint.GetComponent<WeaponKineticBlade>() : null;
        static bool OwnWhirlwind(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility ability) => ability?.AssetGuid
            == Id(KineticistClassBridge0020.CloneToken(BlueprintGuid.Parse("80f10dc9181a0f64f97a9f7ac9f47d65"))) && HavockerSpellburn0020.OwnBlast(ability);

        // TTT replaces the vanilla component, so the native controller no
        // longer recognizes its cleave. Bind only this actual cached ability,
        // exact provider/type and audited greater-cleave feature reference.
        static bool TttCleave(Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility ability) {
            if (ability == null || ability.AssetGuid.ToString() != "6447d104a2222c14d9c9b8a36e4eb242"
                || !ReferenceEquals(ResourcesLibrary.TryGetBlueprint<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(ability.AssetGuid), ability)) return false;
            return ability.ComponentsArray.Any(c => c != null && c.GetType().FullName == "TabletopTweaks.Core.NewActions.AbilityCustomCleaveTTT"
                && c.GetType().Assembly.GetName().Name == "TabletopTweaks-Core"
                && c is Kingmaker.UnitLogic.Abilities.Components.Base.AbilityCustomLogic
                && c.GetType().GetField("m_GreaterFeature")?.GetValue(c) is BlueprintFeatureReference greater
                && ReferenceEquals(greater.Get(), ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Classes.BlueprintFeature>(BlueprintGuid.Parse("cc9c862ef2e03af4f89be5088851ea35"))));
        }
        static AbilityExecutionContext CleaveSource(UnitCommand command, RuleAttackWithWeapon rule) {
            var use = command as UnitUseAbility; var context = rule.Reason.Context?.SourceAbilityContext;
            if (use == null || !command.IsRunning || !ReferenceEquals(command.Executor, rule.Initiator)
                || !ReferenceEquals(use.Ability, context?.Ability) || !ReferenceEquals(context?.MaybeCaster, rule.Initiator)) return null;
            return use.Ability.Blueprint.GetComponent<AbilityCustomCleave>() != null || TttCleave(use.Ability.Blueprint) ? context : null;
        }

        internal static void RecordActivation(UnitDescriptor owner, AbilityData ability, HavockerSpellburn0020.CastRecord receipt) {
            var weapon = owner.Body.PrimaryHand.MaybeWeapon; var blade = OwnBlade(weapon);
            if (!OwnPart(owner) || blade == null || !receipt.Paid || !receipt.NativeEvent) return;
            if (OwnWhirlwind(ability.Blueprint)) {
                // Bind the ownership lifetime at the native commitment, even
                // if cleanup happens before the first area attack is delivered.
                Whirlwinds.Remove(receipt.Context);
                Whirlwinds.Add(receipt.Context, new Link { Weapon = weapon, Receipt = receipt, Lifetime = Lifetimes.GetValue(owner, _ => new object()) });
                return;
            }
            if (ability.Blueprint.GetComponent<AbilityKineticBlade>() == null || blade.ActivationAbility != ability.Blueprint) return;
            Activations.Remove(owner); Activations.Add(owner, new Link { Weapon = weapon, Receipt = receipt });
        }
        internal static void Clear(UnitDescriptor owner) {
            if (owner == null) return;
            Activations.Remove(owner);
            Lifetimes.Remove(owner);
            // Remove our marker even if a foreign provider has since taken over
            // the singleton. Never query/remove the foreign settings' marker.
            var marker = Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff>(Id(KineticistClassBridge0020.CloneToken(BlueprintGuid.Parse("426a9c079ee7ac34aa8e0054f2218074"))));
            if (marker != null) owner.Buffs.GetBuff(marker)?.Remove();
        }

        static bool BeginAttack(RuleAttackWithWeapon rule, out IDisposable frame) {
            frame = null; var blade = OwnBlade(rule.Weapon); if (blade == null) return true;
            var owner = rule.Initiator.Descriptor;
            if (!OwnPart(owner)) return false;
            var part = owner.Get<UnitPartKineticist>();
            UnitCommand command = rule.IsAttackOfOpportunity ? owner.Unit.Commands.GetCommand<UnitAttackOfOpportunity>() : owner.Unit.Commands.Standard;
            var source = rule.Reason.Context?.SourceAbilityContext;
            bool whirlwind = OwnWhirlwind(source?.AbilityBlueprint);
            Link link;
            if (whirlwind) {
                // Whirlwind pays its own authored B3/I2 through native Spend.
                // Its individual melee actions have neither IsFirstAttack nor
                // full-attack flags. Only this live paid ability/context may
                // share that receipt; an old blade activation cannot sponsor it.
                var use = command as UnitUseAbility;
                var paid = HavockerSpellburn0020.CommittedReceipt(source);
                if (use == null || !command.IsRunning || !ReferenceEquals(command.Executor, rule.Initiator)
                    || !ReferenceEquals(use.Ability, source.Ability) || !ReferenceEquals(source.MaybeCaster, rule.Initiator)
                    || !HavockerSpellburn0020.OwnBlast(source.AbilityBlueprint) || !ReferenceEquals(paid?.Owner, owner)
                    || !ReferenceEquals(paid.Context, source) || !HavockerSpellburn0020.With(owner, c => c.OwnReceipt(paid))) return false;
                var lifetime = Lifetimes.GetValue(owner, _ => new object());
                if (!Whirlwinds.TryGetValue(source, out link) || !ReferenceEquals(link.Weapon, rule.Weapon)
                    || !ReferenceEquals(link.Receipt, paid) || !ReferenceEquals(link.Lifetime, lifetime)) return false;
                if (link.FirstAttack == null) { link.FirstAttack = rule; link.Command = command; Activations.Remove(owner); part.RemoveBladeActivatedBuff(); }
                if (!ReferenceEquals(link.Command, command)) return false;
                var areaFrame = new Frame { Link = link, Previous = Current }; Current = areaFrame; frame = areaFrame; return true;
            }
            if (!Activations.TryGetValue(owner, out link) || !ReferenceEquals(link.Weapon, rule.Weapon) || !link.Receipt.Paid) return false;
            if (link.FirstAttack == null) {
                if (!part.IsBladeActivated) return false;
                link.FirstAttack = rule; link.Command = command;
                link.CleaveContext = CleaveSource(command, rule);
                // Native AoO sets no IsFirstAttack flag, so its controller does
                // not remove the ready marker. Claim it here for this paid
                // logical attack, otherwise a later standard command skips its
                // required activation and inherits an already-used marker.
                // TTT0.7.15 emits reversed first-attack flags. Receipt identity
                // follows its actual execution context, not those flags.
                if (rule.IsAttackOfOpportunity || link.CleaveContext != null) part.RemoveBladeActivatedBuff();
            } else if (!ReferenceEquals(link.FirstAttack, rule)) {
                if (link.Command == null || !ReferenceEquals(link.Command, command) || !command.IsRunning) return false;
                bool full = link.FirstAttack.IsFullAttack && rule.IsFullAttack && !rule.IsFirstAttack;
                bool cleave = link.CleaveContext != null && ReferenceEquals(link.CleaveContext, CleaveSource(command, rule));
                if (!full && !cleave) return false;
            }
            var next = new Frame { Link = link, Previous = Current }; Current = next; frame = next; return true;
        }

        // The native kinetic controller queues activation only for UnitAttack,
        // charge and cleave, not UnitAttackOfOpportunity. At this precise native
        // commit boundary use the same activation + Spend receipt as those
        // commands. Never settle a second fee in the delivered child.
        static bool PrepareOpportunity(UnitAttackOfOpportunity command) {
            var owner = command.Executor?.Descriptor;
            var weapon = command.Hand?.Weapon; var blade = OwnBlade(weapon);
            if (blade == null || command.Target == null || command.Target.Descriptor.State.IsDead) return true;
            if (!OwnPart(owner)) return false;
            if (Activations.TryGetValue(owner, out var pending) && ReferenceEquals(pending.Weapon, weapon)
                && pending.FirstAttack == null && pending.Receipt.Paid && owner.Get<UnitPartKineticist>().IsBladeActivated) return true;
            var activation = blade.GetActivationAbility(owner);
            if (activation == null || activation.Blueprint.GetComponent<AbilityKineticist>().CalculateBurnCost(activation).Total != 0) return false;
            var cast = Kingmaker.RuleSystem.Rulebook.Trigger(new RuleCastSpell(activation, owner.Unit));
            if (!cast.Success) return false;
            using (cast.Context.GetDataScope(owner.Unit)) activation.Spend();
            return Activations.TryGetValue(owner, out var paid) && ReferenceEquals(paid.Weapon, weapon)
                && paid.FirstAttack == null && paid.Receipt.Paid && paid.Receipt.NativeEvent;
        }

        static bool AuthorizeCast(RuleCastSpell rule) {
            var bp = rule.Spell.Blueprint;
            if (!HavockerSpellburn0020.OwnBlast(bp)) return true;
            var owner = HavockerSpellburn0020.DeclaredCaster(rule.Spell);
            if (!OwnPart(owner)) return false;
            if (bp.GetComponent<AbilityDeliveredByWeapon>() != null) {
                var link = Current?.Link; var blade = OwnBlade(link?.Weapon);
                if (blade?.Blast != bp || !ReferenceEquals(link.Receipt.Owner, owner) || !link.Receipt.Paid) return false;
                HavockerSpellburn0020.LinkPaidContext(rule.Context, link.Receipt); return true;
            }
            // Reject before Cast/effects, not only at the later Spend call.
            return bp.GetComponent<AbilityKineticist>().CalculateBurnCost(rule.Spell).Total == 0;
        }
        internal static void Install(Harmony harmony) {
            harmony.CreateClassProcessor(typeof(AttackPatch)).Patch(); harmony.CreateClassProcessor(typeof(CastPatch)).Patch(); harmony.CreateClassProcessor(typeof(OpportunityPatch)).Patch(); harmony.CreateClassProcessor(typeof(CleaveQueuePatch)).Patch();
        }
        [HarmonyPatch(typeof(KineticistController), nameof(KineticistController.HandleUnitWantRunCommand))]
        internal static class CleaveQueuePatch {
            [HarmonyPostfix] static void Postfix(UnitCommand cmd, ref UnitCommands.CustomHandlerData? customHandler) {
                if (customHandler.HasValue || !(cmd is UnitUseAbility use) || !TttCleave(use.Ability.Blueprint)) return;
                var owner = cmd.Executor?.Descriptor; var blade = OwnBlade(owner?.Body.PrimaryHand.MaybeWeapon);
                if (blade == null || !OwnPart(owner)) return;
                var part = owner.Get<UnitPartKineticist>(); if (part.IsBladeActivated) return;
                var activation = blade.GetActivationAbility(owner); if (activation == null) return;
                customHandler = part.IsActivatingBladeNow
                    ? new UnitCommands.CustomHandlerData { TryReturnToQueue = true }
                    : new UnitCommands.CustomHandlerData { ExecuteBefore = new UnitUseAbility(activation, owner.Unit) };
            }
        }
        [HarmonyPatch(typeof(UnitAttackOfOpportunity), "OnAction")]
        internal static class OpportunityPatch {
            [HarmonyPrefix] static bool Prefix(UnitAttackOfOpportunity __instance, ref UnitCommand.ResultType __result, out OpportunityRun __state) {
                __state = null;
                if (OwnBlade(__instance.Hand?.Weapon) == null) return true;
                if (Opportunities.TryGetValue(__instance, out var previous)) { __result = previous.Result; return false; }
                var attempt = new OpportunityRun(); Opportunities.Add(__instance, attempt);
                if (PrepareOpportunity(__instance)) { __state = attempt; return true; }
                __result = UnitCommand.ResultType.Fail; return false;
            }
            [HarmonyPostfix] static void Postfix(UnitCommand.ResultType __result, OpportunityRun __state) { if (__state != null) __state.Result = __result; }
        }
        [HarmonyPatch(typeof(KineticistController), nameof(KineticistController.OnEventDidTrigger), typeof(RuleAttackWithWeapon))]
        internal static class AttackPatch {
            [HarmonyPrefix] static bool Prefix(RuleAttackWithWeapon evt, out IDisposable __state) => BeginAttack(evt, out __state);
            [HarmonyFinalizer] static Exception Finalizer(Exception __exception, IDisposable __state) { __state?.Dispose(); return __exception; }
        }
        [HarmonyPatch(typeof(RuleCastSpell), nameof(RuleCastSpell.OnTrigger))]
        internal static class CastPatch {
            [HarmonyPrefix] static void Prefix(RuleCastSpell __instance) {
                if (!AuthorizeCast(__instance)) { __instance.ForceFail = true; ModEntry.LogSpellDiagnostic("Havocker private cast rejected before effects: unpaid blade child, incompatible owner or positive final burn."); }
            }
        }
    }
}
