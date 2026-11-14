// COTW-0296 Weapon type properties, ported from Call of the Wild (Kingmaker)
// Enchantments/WeaponEnchantments.cs createManeuverEnchantments and
// Features/WeaponsFix.cs addWeaponProperties.
// Each property is a zero-cost weapon enchantment that carries a hidden feature
// with a native ManeuverBonus. The enchantment is added at item level, by
// category, instead of being written into BlueprintWeaponType.m_Enchantments:
// a type-level enchantment would make every such weapon count as magical for
// the native mass-sale/vendor checks, and the category table keeps covering
// weapon types this package cannot enumerate.
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class WeaponProperties0296
    {
        internal const string ModuleName = "Weapon type properties";
        internal static string LastSummary = "Not registered";

        internal static readonly string[] Tokens = { "DisarmProperty0296", "TripProperty0296", "SunderProperty0296" };

        private static readonly CombatManeuver[] Maneuvers =
        {
            CombatManeuver.Disarm, CombatManeuver.Trip, CombatManeuver.SunderArmor
        };

        private static readonly string[] Names = { "Disarm", "Trip", "Sunder" };

        private static readonly string[] Texts =
        {
            "When you use a disarm weapon, you get a +2 bonus on Combat Maneuver Checks to disarm attempts.",
            "When you use a trip weapon, you get a +2 bonus on Combat Maneuver Checks to trip attempts.",
            "When you use a sunder weapon, you get a +2 bonus on Combat Maneuver Checks to sunder attempts."
        };

        // Original CotW lists, kept verbatim. Upstream ships the sunder list
        // empty, so the sunder property exists but no category uses it yet;
        // adding a category here is the only change needed to enable it.
        private static readonly WeaponCategory[][] Categories =
        {
            new[] { WeaponCategory.Flail, WeaponCategory.HeavyFlail, WeaponCategory.Nunchaku, WeaponCategory.Sai },
            new[] { WeaponCategory.Sickle, WeaponCategory.Flail, WeaponCategory.HeavyFlail, WeaponCategory.Scythe, WeaponCategory.Kama, WeaponCategory.Fauchard, WeaponCategory.HookedHammer },
            new WeaponCategory[0]
        };

        // Published only after a successful transaction, so a rolled-back module
        // never hands a half-built enchantment to the item creation patch.
        private static BlueprintWeaponEnchantment[] published;

        internal static BlueprintWeaponEnchantment[] For(WeaponCategory category)
        {
            var current = published;
            if (current == null) return Array.Empty<BlueprintWeaponEnchantment>();
            return Enumerable.Range(0, Tokens.Length)
                .Where(i => current[i] != null && Categories[i].Contains(category))
                .Select(i => current[i])
                .ToArray();
        }

        internal static void Register(BlueprintsCache cache)
        {
            if (MagusBatch01454.Existing(cache, ModuleName)) { LastSummary = "Already registered"; return; }
            var f = new PsychicAssets(cache);
            var list = new BlueprintWeaponEnchantment[Tokens.Length];
            for (var i = 0; i < Tokens.Length; i++)
            {
                var feature = f.Feature(Tokens[i] + "Feature", Names[i], Texts[i],
                    new ManeuverBonus { Type = Maneuvers[i], Bonus = 2 });
                feature.HideInUI = feature.HideInCharacterSheetAndLevelUp = true;
                var add = new AddUnitFeatureEquipment();
                Set(add, "m_Feature", Ref<BlueprintFeatureReference>(feature));
                var enchant = f.Add(new BlueprintWeaponEnchantment(), Tokens[i], Names[i], Texts[i]);
                Set(enchant, "m_EnchantName", Text(Tokens[i], "name", Names[i]));
                Set(enchant, "m_Description", Text(Tokens[i], "description", Texts[i]));
                Set(enchant, "m_Prefix", Text(Tokens[i], "prefix", ""));
                Set(enchant, "m_Suffix", Text(Tokens[i], "suffix", ""));
                Set(enchant, "m_EnchantmentCost", 0);
                Set(enchant, "m_IdentifyDC", 0);
                enchant.ComponentsArray = new BlueprintComponent[] { add };
                list[i] = enchant;
            }
            MagusBatch01454.Publish(ModuleName, f,
                () => published = list,
                () => published = null,
                () => Verify(cache));
            LastSummary = "Registered Disarm, Trip and Sunder weapon properties.";
        }

        private static void Verify(BlueprintsCache cache)
        {
            for (var i = 0; i < Tokens.Length; i++)
            {
                var enchant = cache.Load(Id(Tokens[i])) as BlueprintWeaponEnchantment;
                if (enchant == null) throw new InvalidOperationException("0296 " + Tokens[i] + " is missing.");
                if (enchant.ComponentsArray == null || enchant.ComponentsArray.Length != 1 ||
                    !(enchant.ComponentsArray[0] is AddUnitFeatureEquipment))
                    throw new InvalidOperationException("0296 " + Tokens[i] + " lost its equipment feature.");
            }
        }
    }

    // Weapons of a property category receive the property enchantment when the
    // item is created. The enchantment's AddUnitFeatureEquipment grants the +2
    // maneuver feature only while that weapon's slot is the active hand set, so
    // a melded weapon inside a wild shape contributes nothing and the copy
    // Weapon Shift transfers to the natural weapons is the one that applies.
    [HarmonyPatch(typeof(BlueprintItemWeapon), "CollectEnchantments")]
    internal static class WeaponPropertyEnchants0296
    {
        private static void Postfix(BlueprintItemWeapon __instance, ref IEnumerable<BlueprintItemEnchantment> __result)
        {
            var type = __instance?.Type;
            if (type == null) return;
            var extra = WeaponProperties0296.For(type.Category);
            if (extra.Length == 0) return;
            __result = (__result ?? Array.Empty<BlueprintItemEnchantment>()).Concat(extra).ToArray();
        }
    }
}
