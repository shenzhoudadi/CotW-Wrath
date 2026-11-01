using System;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Items;

namespace Way_of_the_shield.Tweaks_and_Changes
{
    public static class TWFShieldTweak
    {
        public static bool NativeLightnessVerified { get; private set; }

        // Current Wrath implements this rule itself. Verify real game IL and install no obsolete patch.
        public static void VerifyNativeLightness()
        {
            if (NativeLightnessVerified || !CheckForShieldLightness.GetValue()) return;
            var target = AccessTools.Method(typeof(TwoWeaponFightingAttackPenalty), nameof(TwoWeaponFightingAttackPenalty.OnEventAboutToTrigger));
            var instructions = PatchProcessor.GetOriginalInstructions(target, (ILGenerator)null);
            var getter = AccessTools.PropertyGetter(typeof(BlueprintItemWeapon), nameof(BlueprintItemWeapon.IsLight));
            int index = instructions.FindIndex(i => i.Calls(getter));
            if (index < 0 || instructions.Count(i => i.Calls(getter)) != 1)
                throw new InvalidOperationException("Native TWF light-weapon check was not found.");
            int adjustment = instructions.FindIndex(index + 1, i => i.LoadsConstant(-2L));
            var shieldGetter = AccessTools.PropertyGetter(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.IsShield));
            if (adjustment < 0 || adjustment - index > 30 || instructions.Skip(index + 1).Take(adjustment - index).Any(i => i.Calls(shieldGetter)))
                throw new InvalidOperationException("Native TWF still has an unsupported shield lightness exception.");
            NativeLightnessVerified = true;
            Comment?.Log("Native shield lightness verified: light and heavy shields use their real weapon type; no legacy transpiler installed.");
        }
    }
}

