// COTW-0610 Unarmed Strike as an off-hand weapon.
//
// Ported from Call of the Wild (Kingmaker) Features/WeaponsFix.cs
// allowTwfWithUnarmedStrike plus NewMechanics/HoldingItemsMechanics.cs.
//
// What the original did
// ---------------------
// One hidden buff carrying UnarmedTwf, turned into a Free-action toggle and
// granted by Two Weapon Fighting only when Improved Unarmed Strike is present.
// The actual rule change was a single helper:
//
//     isOffHandUnarmedAndCanBeIgnored(weapon, unit)
//         => weapon.IsUnarmed
//            && !(unit has ImprovedUnarmedStrike && UnitPartUnarmedTwf.active());
//
// Every attack routine that used to say "an unarmed off hand is not a weapon"
// called that helper, so flipping it made the engine treat the off hand as a
// real light weapon. Nothing else was patched: the native two-weapon penalties,
// Improved/Greater Two-Weapon Fighting and the half-Strength off-hand damage
// then applied on their own.
//
// What Wrath needs
// ----------------
// Wrath keeps the same decision in three places, and they all consult one
// shared predicate, so the port is smaller than the Kingmaker original:
//
//   RestrictionsHelper.CheckHasTwoWeapon
//       requires an actual item in BOTH hands. An empty off hand therefore
//       disables the two-weapon penalty, the Improved/Greater extra attacks
//       and the off-hand damage reduction all at once.
//   RuleCalculateAttacksCount.CalculateSecondaryHand
//       `(!offWeapon.IsUnarmed || mainWeapon == null)` - an unarmed off hand
//       never produces an attack, so there is nothing to penalise either.
//   HandSlot.MaybeWeapon
//       an empty slot falls back to Body.EmptyHandWeapon, but a slot holding an
//       item returns the item first. During a polymorph the hands hold the
//       form's natural weapons, which is why the empty-hand strike disappears.
//
// Two toggles are granted by Improved Unarmed Strike:
//
//   Off-Hand          patches the first two points, so an empty off hand counts
//                     as a light weapon while the toggle is on.
//   Natural Override  covers the third point two ways, because a natural weapon
//                     reaches the hands by two different routes. A Feral Mutagen
//                     claw is an EmptyHandWeaponOverride sitting on
//                     Body.EmptyHandWeapon, so the toggle pushes the unarmed
//                     strike on top with the same component. A polymorph instead
//                     inserts the form's natural weapons as real items in the
//                     hand slots, where no override can reach them, so the slot
//                     getter answers with the empty-hand weapon for those.
//
// The off-hand patch adds exactly one attack and no base-attack-bonus
// iteratives. The native `flag` that decides those is false for every case this
// toggle allows (it only fires for a null main hand or a natural main hand with
// Iterative Natural Attacks), so replicating the private
// CalculatePenalizedAttacksCount formula would only add risk. Pathfinder does
// not grant the off hand BAB iteratives anyway; Improved and Greater Two-Weapon
// Fighting supply the extra off-hand attacks, and those come from the native
// component once CheckHasTwoWeapon agrees.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class UnarmedTwf0610
    {
        internal const string ModuleName = "Unarmed strike off-hand";
        internal const string OffHandToken = "UnarmedOffHand0610";
        internal const string MainHandToken = "UnarmedMainHand0610";

        // Native Feats/ImprovedUnarmedStrike.jbp. It carries the
        // EmptyHandWeaponOverride that makes an empty hand a lethal 1d3 strike,
        // so both toggles lean on it rather than shipping a weapon blueprint.
        internal const string ImprovedUnarmedStrikeId = "7812ad3672a4b9a4fb894ea402095167";
        // Weapons/Items/Standard/NaturalWeapon/Unarmed1d3.jbp, the weapon the
        // Improved Unarmed Strike override installs. The override toggle pushes
        // it again so it outranks any natural weapon sitting on the empty hand.
        internal const string UnarmedStrikeId = "e5ea5042194712040a3b2944e6944d10";
        // Native SorcerousClawsFeature, used only as a claw icon donor so the
        // override toggle does not look identical to the off-hand one.
        internal const string ClawIconId = "74fd013004da46219498021a042b493d";

        internal const string OffHandText =
            "Treat your unarmed strike as an off-hand weapon.\n" +
            "Requires Improved Unarmed Strike. While this is on, an empty off hand counts as a light weapon for Two-Weapon Fighting: the usual two-weapon attack penalties apply, Improved and Greater Two-Weapon Fighting add their extra off-hand attacks, and the off hand adds only half your Strength modifier to damage.";

        internal const string MainHandText =
            "Strike with your unarmed strike instead of the natural weapons your hands carry.\n" +
            "Claws and other natural weapons on your hands are replaced by the unarmed strike your Improved Unarmed Strike already provides; the bite and any other additional limb are unchanged, and a held weapon or shield is left alone. On its own this collapses a pair of claws into a single unarmed strike. Turn on Unarmed Strike: Off-Hand as well to attack with both fists. Requires Improved Unarmed Strike.";

        internal static string LastSummary = "Not registered";

        // Resolved after publication; the patches stay inert until then.
        private static BlueprintUnitFact offHandBuff;
        private static BlueprintUnitFact mainHandBuff;

        internal static void Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) { LastSummary = "Already registered"; return; }
            var f = new PsychicAssets(cache);
            var unarmed = f.Native<BlueprintFeature>(ImprovedUnarmedStrikeId);
            var unarmedStrike = f.Native<BlueprintItemWeapon>(UnarmedStrikeId);
            var clawIcon = f.Native<BlueprintUnitFact>(ClawIconId);

            // Both buffs are markers only. The patches read them through
            // HasFact, so no runtime component is needed and nothing has to be
            // serialised into saves beyond the buff itself.
            var offBuff = f.Add(new BlueprintBuff
            {
                Stacking = StackingType.Replace,
                IsClassFeature = true,
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, OffHandToken + "Buff", "Unarmed Strike: Off-Hand", "");
            SetEnum(offBuff, "m_Flags", "HiddenInUi");

            var mainBuff = f.Add(new BlueprintBuff
            {
                Stacking = StackingType.Replace,
                IsClassFeature = true,
                ComponentsArray = new BlueprintComponent[] { EmptyHandOverride(unarmedStrike) }
            }, MainHandToken + "Buff", "Unarmed Strike: Natural Weapon Override", "");
            SetEnum(mainBuff, "m_Flags", "HiddenInUi");

            var offToggle = Toggle(f, OffHandToken, "Unarmed Strike: Off-Hand", OffHandText, offBuff, unarmed);
            var mainToggle = Toggle(f, MainHandToken, "Unarmed Strike: Natural Weapon Override", MainHandText, mainBuff, clawIcon);

            var prior = unarmed.ComponentsArray;
            var grant = Facts(offToggle, mainToggle);
            MagusBatch01454.Publish(ModuleName, f,
                () =>
                {
                    unarmed.ComponentsArray = (prior ?? Array.Empty<BlueprintComponent>())
                        .Concat(new BlueprintComponent[] { grant }).ToArray();
                    offHandBuff = offBuff;
                    mainHandBuff = mainBuff;
                },
                () =>
                {
                    unarmed.ComponentsArray = prior;
                    offHandBuff = null;
                    mainHandBuff = null;
                },
                () => Verify(cache, unarmed, offToggle, mainToggle));
            LastSummary = "Registered two toggles on Improved Unarmed Strike: unarmed strike as an off-hand weapon, and unarmed strike overriding a main-hand natural weapon.";
        }

        private static BlueprintActivatableAbility Toggle(PsychicAssets f, string token, string name, string text,
            BlueprintBuff buff, BlueprintUnitFact icon)
        {
            // Immediately + Free + DeactivateImmediately=false is the shape of a
            // persistent switch: one click turns it on and it stays on, including
            // through rest, until clicked again.
            var toggle = f.Add(new BlueprintActivatableAbility
            {
                ActivationType = AbilityActivationType.Immediately,
                Group = ActivatableAbilityGroup.None,
                DeactivateImmediately = false,
                DoNotTurnOffOnRest = true,
                DeactivateIfCombatEnded = false,
                DeactivateIfOwnerDisabled = false,
                DeactivateIfOwnerUnconscious = false,
                ComponentsArray = Array.Empty<BlueprintComponent>()
            }, token, name, text);
            Set(toggle, "m_Buff", Ref<BlueprintBuffReference>(buff));
            Set(toggle, "m_ActivateWithUnitCommand", UnitCommand.CommandType.Free);
            ArcaneBatch01446.Icon(toggle, icon);
            return toggle;
        }

        // The claw a Feral Mutagen alchemist sees is not a hand-slot item: it is
        // an EmptyHandWeaponOverride pushing Claw1d6 onto Body.EmptyHandWeapon.
        // Reading MaybeWeapon and swapping the result for Body.EmptyHandWeapon
        // therefore returns the very same claw. The only way to win against it
        // is the same mechanism: push the unarmed strike on top, non-permanent,
        // so ending the toggle pops back to whatever was there before.
        private static EmptyHandWeaponOverride EmptyHandOverride(BlueprintItemWeapon weapon)
        {
            var component = new EmptyHandWeaponOverride
            {
                name = "UnarmedMainHandOverride0610",
                IsPermanent = false,
                IsMonkUnarmedStrike = false
            };
            Set(component, "m_Weapon", Ref<BlueprintItemWeaponReference>(weapon));
            return component;
        }

        private static void Verify(BlueprintsCache cache, BlueprintFeature unarmed,
            BlueprintActivatableAbility offToggle, BlueprintActivatableAbility mainToggle)
        {
            if (unarmed == null) throw new InvalidOperationException("0610 Improved Unarmed Strike vanished.");
            var granted = GrantedFacts(unarmed);
            if (!granted.Contains(offToggle.AssetGuid) || !granted.Contains(mainToggle.AssetGuid))
                throw new InvalidOperationException("0610 Improved Unarmed Strike does not grant both toggles.");
            foreach (var toggle in new[] { offToggle, mainToggle })
            {
                var loaded = cache.Load(toggle.AssetGuid) as BlueprintActivatableAbility;
                if (!ReferenceEquals(loaded, toggle))
                    throw new InvalidOperationException("0610 toggle publication failed: " + toggle.name);
                if (loaded.Buff == null)
                    throw new InvalidOperationException("0610 toggle lost its buff: " + toggle.name);
            }
        }

        // ---------------------------------------------------------------- rules

        internal static bool IsOffHandUnarmed(UnitEntityData unit)
        {
            if (unit == null) return false;
            var descriptor = unit.Descriptor;
            if (descriptor == null || offHandBuff == null) return false;
            if (!descriptor.State.Features.ImprovedUnarmedStrike) return false;
            if (!descriptor.HasFact(offHandBuff)) return false;
            var body = unit.Body;
            if (body == null || !body.HandsAreEnabled || body.EmptyHandWeapon == null) return false;
            // A weapon gripped in both hands leaves no off hand to fight with.
            // Without this the shared predicate would start penalising the main
            // attack of anyone holding a longsword two-handed.
            var main = body.PrimaryHand?.MaybeWeapon;
            if (main != null && main.HoldInTwoHands) return false;
            var secondary = body.SecondaryHand;
            if (secondary == null) return false;
            // HasItem, not HasWeapon: a shield or any other held object occupies
            // the hand just as much as a weapon does.
            if (secondary.HasItem) return false;
            // The blueprint test, not an identity test against EmptyHandWeapon:
            // a Feral Mutagen claw also lives on the empty-hand slot, and the
            // override toggle can replace it with an unarmed strike. Only a
            // weapon the engine itself calls unarmed is a fist. The same test
            // rejects the second head of a double weapon, which has no item of
            // its own in the secondary slot.
            var offHand = secondary.MaybeWeapon;
            return offHand != null && offHand.Blueprint != null && offHand.Blueprint.IsUnarmed;
        }

        internal static bool IsNaturalOverrideActive(UnitDescriptor owner)
        {
            if (owner == null || mainHandBuff == null) return false;
            if (!owner.State.Features.ImprovedUnarmedStrike) return false;
            return owner.HasFact(mainHandBuff);
        }

        internal static void Report(Exception exception)
        {
            try
            {
                var message = exception?.ToString();
                if (message == null || !Reported.Add(message)) return;
                Console.WriteLine("[CotW-UnarmedTwf] " + message);
            }
            catch (Exception) { }
        }

        private static readonly HashSet<string> Reported = new HashSet<string>();
    }

    // The single predicate the native two-weapon components share. Turning it on
    // for an unarmed off hand is what makes the penalty, the Improved/Greater
    // extra attacks and the half-Strength damage apply without touching any of
    // them individually.
    [HarmonyPatch(typeof(RestrictionsHelper), nameof(RestrictionsHelper.CheckHasTwoWeapon))]
    internal static class UnarmedTwoWeapon0610Patch
    {
        private static void Postfix(UnitEntityData unit, ref bool __result)
        {
            if (__result) return;
            try
            {
                if (!UnarmedTwf0610.IsOffHandUnarmed(unit)) return;
                // The main hand has to offer something to fight with; an empty
                // hand still reports the empty-hand weapon, so this only rejects
                // a disabled or missing primary slot.
                var main = unit.Body.PrimaryHand?.MaybeWeapon;
                __result = main != null;
            }
            catch (Exception exception) { UnarmedTwf0610.Report(exception); }
        }
    }

    // Native refuses an unarmed off hand outright, so the attack itself has to be
    // added here before the shared predicate can penalise it.
    [HarmonyPatch(typeof(RuleCalculateAttacksCount), "CalculateSecondaryHand")]
    internal static class UnarmedOffHandAttack0610Patch
    {
        private static void Postfix(RuleCalculateAttacksCount __instance)
        {
            try
            {
                var initiator = __instance?.Initiator;
                if (initiator == null) return;
                var result = __instance.Result?.SecondaryHand;
                if (result == null || result.AdditionalAttacks > 0) return;
                // Only the off-hand toggle produces this attack. The override
                // toggle is a weapon substitution, not a second attack: with it
                // alone the pair of claws collapses into a single unarmed strike,
                // and the off hand only joins in once the player also turns on
                // "Unarmed Strike: Off-Hand" (and pays the two-weapon price).
                if (!UnarmedTwf0610.IsOffHandUnarmed(initiator)) return;
                result.AdditionalAttacks++;
            }
            catch (Exception exception) { UnarmedTwf0610.Report(exception); }
        }
    }

    // The override toggle. A polymorph puts the form's natural weapons into the
    // hand slots as real items, where the empty-hand override cannot reach them,
    // so those slots are answered with the empty-hand weapon instead. Natural
    // weapons on additional limbs are separate slots and stay untouched.
    [HarmonyPatch(typeof(HandSlot), nameof(HandSlot.MaybeWeapon), MethodType.Getter)]
    internal static class UnarmedMainHand0610Patch
    {
        private static void Postfix(HandSlot __instance, ref ItemEntityWeapon __result)
        {
            try
            {
                if (__result == null || __result.Blueprint == null) return;
                // The unarmed blueprint is itself flagged natural (Weapons/Types/
                // Special/Unarmed has m_IsNatural and m_IsUnarmed both true), so
                // IsUnarmed has to be excluded or the check would keep firing on
                // its own result.
                if (!__result.Blueprint.IsNatural || __result.Blueprint.IsUnarmed) return;
                var owner = __instance?.Owner;
                if (owner == null) return;
                var body = owner.Body;
                if (body == null) return;
                if (body.PrimaryHand != __instance && body.SecondaryHand != __instance) return;
                if (!UnarmedTwf0610.IsNaturalOverrideActive(owner)) return;
                var unarmed = body.EmptyHandWeapon;
                if (unarmed != null && !ReferenceEquals(unarmed, __result)) __result = unarmed;
            }
            catch (Exception exception) { UnarmedTwf0610.Report(exception); }
        }
    }
}
