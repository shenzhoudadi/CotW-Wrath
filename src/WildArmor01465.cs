// COTW-01465 Wild armor enchantment, ported from Call of the Wild (Kingmaker)
// NewMechanics/WildArmor.cs.
// The enchantment itself carries no components. Wrath hides the armor slot and
// an off-hand shield by retaining the slot deactivate flag for the duration of
// the shape (UnitBody.GeneratePolymorphEquipment) and releases them again when
// the shape ends (ClearPolymorphEquipment). An item carrying Wild simply undoes
// the retain, so its slot stays active and keeps granting the armor bonus and
// the enhancement bonus.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WildArmor01465
    {
        internal const string ModuleName = "Wild armor enchantment";
        internal const string Token = "WildArmorEnchant01465";
        // Published identity: an enchantment catalogue resolves the blueprint by
        // this name and by the deterministic token GUID, so neither may drift.
        internal const string BlueprintName = "WildArmorEnchant01465";
        internal const string Description = "Armor with this special ability usually appears to be made from magically hardened animal pelt. The wearer of a suit of armor or a shield with this ability preserves his armor bonus (and any enhancement bonus) while in a wild shape. Armor and shields with this ability usually appear to be covered in leaf patterns. While the wearer is in a wild shape, the armor cannot be seen.";
        internal static string LastSummary = "Not registered";

        internal static BlueprintGuid EnchantId => Id(Token);

        internal static void Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) { LastSummary = "Already registered"; return; }
            var f = new PsychicAssets(cache);
            var enchant = f.Add(new BlueprintArmorEnchantment(), Token, "Wild", Description);
            enchant.name = BlueprintName;
            Set(enchant, "m_EnchantName", Text(Token, "name", "Wild"));
            Set(enchant, "m_Description", Text(Token, "description", Description));
            Set(enchant, "m_Prefix", Text(Token, "prefix", "Wild"));
            Set(enchant, "m_Suffix", Text(Token, "suffix", ""));
            Set(enchant, "m_EnchantmentCost", 3);
            Set(enchant, "m_IdentifyDC", 10);
            MagusBatch01454.Publish(ModuleName, f, () => { }, () => { }, () => Verify(cache));
            LastSummary = "Registered the +3 Wild armor enchantment.";
        }

        private static void Verify(BlueprintsCache cache)
        {
            var enchant = cache.Load(EnchantId) as BlueprintArmorEnchantment;
            if (enchant == null) throw new InvalidOperationException("01465 Wild armor enchantment is missing.");
            if (enchant.EnchantmentCost != 3 || enchant.HiddenInUI)
                throw new InvalidOperationException("01465 Wild armor enchantment lost its +3 cost identity.");
            if (enchant.name != BlueprintName)
                throw new InvalidOperationException("01465 Wild armor enchantment name changed: " + enchant.name);
        }

        private static bool Wild(ItemEntityArmor armor)
            => armor?.Enchantments != null &&
               armor.Enchantments.Any(e => e?.Blueprint != null && e.Blueprint.AssetGuid == EnchantId);

        // While polymorphed CurrentHandsEquipmentSet is the temporary set holding
        // the natural weapons, so the real off-hand is only reachable through the
        // stored set index. The retain/release pair has to hit the very slot the
        // native code flags; otherwise the counter drifts and the shield either
        // stays hidden or keeps its shield bonus after the shape ends.
        private static HandSlot RealSecondaryHand(UnitBody body)
        {
            if (body == null) return null;
            try
            {
                var sets = Get(body, "m_HandsEquipmentSets") as IList;
                var index = Convert.ToInt32(Get(body, "m_CurrentHandsEquipmentSetIndex"));
                if (sets == null || index < 0 || index >= sets.Count) return null;
                return (sets[index] as HandsEquipmentSet)?.SecondaryHand;
            }
            catch (Exception)
            {
                // A shifted private layout must never break polymorph itself.
                return null;
            }
        }

        private static HandSlot WildShield(UnitBody body)
        {
            var hand = RealSecondaryHand(body);
            return hand != null && hand.HasShield && Wild(hand.Shield?.ArmorComponent) ? hand : null;
        }

        internal static void Applied(UnitBody body)
        {
            if (body?.Owner == null) return;
            if (body.Armor != null && body.Armor.HasArmor && Wild(body.Armor.Armor))
                body.Armor.ReleaseDeactivateFlag();
            var shield = WildShield(body);
            if (shield != null) shield.ReleaseDeactivateFlag();
        }

        // ClearPolymorphEquipment releases the flag the native apply retained. A
        // wild slot has to put its own retain back first, or the count goes
        // negative and the armor stays hidden after the shape ends.
        internal static void Cancelled(UnitBody body)
        {
            if (body?.Owner == null || !body.IsPolymorphed) return;
            if (body.Armor != null && body.Armor.HasArmor && Wild(body.Armor.Armor))
                body.Armor.RetainDeactivateFlag();
            var shield = WildShield(body);
            if (shield != null) shield.RetainDeactivateFlag();
        }
    }

    [HarmonyPatch(typeof(UnitBody), nameof(UnitBody.ApplyPolymorphEffect))]
    internal static class WildArmorPolymorphApplied01465
    {
        private static void Prefix(UnitBody __instance, out bool __state) => __state = __instance.IsPolymorphed;

        private static void Postfix(UnitBody __instance, bool __state)
        {
            // The native method logs and returns when the body is already
            // polymorphed; releasing again would unbalance the slot flag.
            if (__state) return;
            WildArmor01465.Applied(__instance);
        }
    }

    [HarmonyPatch(typeof(UnitBody), nameof(UnitBody.CancelPolymorphEffect))]
    internal static class WildArmorPolymorphCancelled01465
    {
        private static void Prefix(UnitBody __instance) => WildArmor01465.Cancelled(__instance);
    }
}
