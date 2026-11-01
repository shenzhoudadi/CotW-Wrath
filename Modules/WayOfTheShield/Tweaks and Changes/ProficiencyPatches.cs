using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;

namespace Way_of_the_shield.ProficiencyRework
{
    public static class ProficiencyPatches
    {
        private static readonly MethodInfo ArmorAddModifier = AccessTools.Method(typeof(ItemEntityArmor), "AddModifier");
        private static readonly MethodInfo ContainsArmor = AccessTools.Method(typeof(UnitProficiency), nameof(UnitProficiency.Contains), new[] { typeof(ArmorProficiencyGroup) });
        private static readonly MethodInfo ContainsWeapon = AccessTools.Method(typeof(UnitProficiency), nameof(UnitProficiency.Contains), new[] { typeof(WeaponCategory) });
        private static readonly MethodInfo ProficiencyOwner = AccessTools.PropertyGetter(typeof(AddProficiencies), "Owner");

        [HarmonyPatch(typeof(UnitEntityData), "OnTurnOn")]
        public static class FixTheArmorBug_ThisPatchDoesNotWantToLoad
        {
            [HarmonyPostfix]
            public static void Postfix(UnitEntityData __instance) => RefreshArmorPenalties(__instance?.Descriptor);
        }

        public static void RefreshArmorPenalties(UnitDescriptor owner)
        {
            if (owner?.Body == null || !AllowEquipNonProfficientItems.GetValue()) return;
            if (owner.Body.Armor.HasArmor) owner.Body.Armor.Armor.RecalculateStats();
            owner.Body.SecondaryHand.MaybeShield?.ArmorComponent?.RecalculateStats();
        }

        [HarmonyPatch]
        public static class ProficiencyFactsChanged
        {
            [HarmonyTargetMethods]
            public static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(AddProficiencies), "OnTurnOn");
                yield return AccessTools.Method(typeof(AddProficiencies), "OnTurnOff");
            }
            [HarmonyPostfix]
            public static void Postfix(AddProficiencies __instance)
                => RefreshArmorPenalties((ProficiencyOwner.Invoke(__instance, null) as UnitEntityData)?.Descriptor);
        }

        [HarmonyPatch]
        public static class ItemEntity_EquipNonProfficient_Patch
        {
            [HarmonyPrepare]
            public static bool Prepare() => AllowEquipNonProfficientItems.GetValue();
            [HarmonyTargetMethods]
            public static IEnumerable<MethodBase> GetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(ItemEntityShield), "CanBeEquippedInternal");
                yield return AccessTools.DeclaredMethod(typeof(ItemEntityArmor), "CanBeEquippedInternal");
                yield return AccessTools.DeclaredMethod(typeof(ItemEntityWeapon), "CanBeEquippedInternal");
            }
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            {
                var result = instructions.Select(i => new CodeInstruction(i)).ToList();
                int replaced = 0;
                foreach (var instruction in result)
                {
                    if (instruction.Calls(ContainsArmor))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(ProficiencyPatches), nameof(AllowArmorProficiencyCheck));
                        replaced++;
                    }
                    else if (instruction.Calls(ContainsWeapon))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(ProficiencyPatches), nameof(AllowWeaponProficiencyCheck));
                        replaced++;
                    }
                }
                if (replaced != 1)
                    throw new InvalidOperationException("Unexpected proficiency equip IL in " + __originalMethod + ": " + replaced);
                return result;
            }
        }
        public static bool AllowArmorProficiencyCheck(UnitProficiency proficiency, ArmorProficiencyGroup category) => true;
        public static bool AllowWeaponProficiencyCheck(UnitProficiency proficiency, WeaponCategory category) => true;

        [HarmonyPatch(typeof(UIUtilityItem), nameof(UIUtilityItem.GetEquipPossibility), new[] { typeof(ItemEntity) })]
        public static class UIUtilityItemPatch
        {
            [HarmonyPrepare]
            public static bool Prepare() => AllowEquipNonProfficientItems.GetValue();
            [HarmonyPostfix]
            public static void Postfix(ItemEntity item, bool[] __result)
            {
                if (__result == null || __result.Length == 0 || Game.Instance?.SelectionCharacter == null) return;
                var selection = Game.Instance.SelectionCharacter;
                var owner = selection.SelectedUnit.Value.Value ?? selection.CurrentSelectedCharacter;
                if (owner != null) __result[0] = IsProficient(__result[0], owner, item);
            }
        }

        [HarmonyPatch(typeof(ItemEntityArmor), nameof(ItemEntityArmor.RecalculateStats))]
        public static class ItemEntityArmorPatch
        {
            [HarmonyPrepare]
            public static bool Prepare() => AllowEquipNonProfficientItems.GetValue();
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var result = instructions.Select(i => new CodeInstruction(i)).ToList();
                var getter = AccessTools.PropertyGetter(typeof(RuleCalculateArmorCheckPenalty), nameof(RuleCalculateArmorCheckPenalty.Result));
                int index = result.FindIndex(i => i.Calls(getter));
                if (index < 0 || index + 1 >= result.Count || !result[index + 1].IsStloc())
                    throw new InvalidOperationException("Armor ACP result local was not found.");
                var stored = result[index + 1];
                CodeInstruction load;
                if (stored.opcode == OpCodes.Stloc_0) load = new CodeInstruction(OpCodes.Ldloc_0);
                else if (stored.opcode == OpCodes.Stloc_1) load = new CodeInstruction(OpCodes.Ldloc_1);
                else if (stored.opcode == OpCodes.Stloc_2) load = new CodeInstruction(OpCodes.Ldloc_2);
                else if (stored.opcode == OpCodes.Stloc_3) load = new CodeInstruction(OpCodes.Ldloc_3);
                else load = new CodeInstruction(stored.opcode == OpCodes.Stloc_S ? OpCodes.Ldloc_S : OpCodes.Ldloc, stored.operand);
                result.InsertRange(index + 2, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    load,
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ProficiencyPatches), nameof(AddAttackBonusPenaltyFromArmor)))
                });
                return result;
            }
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        public static class BlueprintsCache_Init_Patch_For_Shields_Proficiency
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!RetrieveBlueprint("cb8686e7357a68c42bdd9d4e65334633", out BlueprintFeature feature, "ShieldProficiency", "adding shield weapon proficiencies")) return;
                var categories = new[] { WeaponCategory.WeaponLightShield, WeaponCategory.WeaponHeavyShield, WeaponCategory.SpikedLightShield, WeaponCategory.SpikedHeavyShield };
                var existing = new HashSet<WeaponCategory>(feature.GetComponents<AddProficiencies>().SelectMany(c => c.WeaponProficiencies ?? Array.Empty<WeaponCategory>()));
                var missing = categories.Where(c => !existing.Contains(c)).ToArray();
                if (missing.Length != 0)
                    feature.AddComponent(new AddProficiencies
                    {
                        name = "WayOfTheShield_AddProficiencies_ShieldsWeaponProficiency",
                        ArmorProficiencies = Array.Empty<ArmorProficiencyGroup>(),
                        WeaponProficiencies = missing
                    });
            }
        }

        public static void AddAttackBonusPenaltyFromArmor(ItemEntityArmor armor, int penalty)
        {
            if (penalty >= 0 || armor?.Wielder == null || IsProficient_Short(armor)) return;
            var descriptor = armor.Shield == null ? ModifierDescriptorExtension.NotProficientWithArmor : ModifierDescriptorExtension.NotProficientWithShield;
            // Add to the native item-owned list so every recalc and unequip removes the old penalty.
            ArmorAddModifier.Invoke(armor, new object[] { armor.Wielder.Stats.AdditionalAttackBonus, penalty, descriptor });
        }
        public static bool IsProficient_Short(ItemEntity item)
        {
            var owner = item?.Wielder ?? item?.Owner;
            return owner == null || !owner.Unit.IsDirectlyControllable || IsProficient(true, owner.Unit, item);
        }
        public static bool IsProficient(bool flag, UnitEntityData owner, ItemEntity item)
        {
            if (!flag || owner == null || item == null) return flag;
            if (item is ItemEntityArmor armor) return owner.Proficiencies.Contains(armor.Blueprint.ProficiencyGroup);
            if (item is ItemEntityShield shield) return owner.Proficiencies.Contains(shield.Blueprint.ArmorComponent.ProficiencyGroup);
            if (item is ItemEntityWeapon weapon) return owner.Proficiencies.Contains(weapon.Blueprint.Category);
            return flag;
        }
    }

    [HarmonyPatch(typeof(RuleCalculateAttackBonusWithoutTarget), nameof(RuleCalculateAttackBonusWithoutTarget.OnTrigger))]
    public static class WeaponNoProficiency
    {
        private static readonly RulebookEvent.CustomDataKey Applied = new("WayOfTheShield_Equipment_NonProficientWeaponPenalty");
        public static BonusType penalty = BonusTypeExtenstions.GetBonusType(160);
        [HarmonyPrepare]
        public static bool Prepare() => AllowEquipNonProfficientItems.GetValue();
        [HarmonyPrefix]
        public static void Prefix(RuleCalculateAttackBonusWithoutTarget __instance)
        {
            var weapon = __instance.Weapon;
            var unit = __instance.Initiator;
            if (unit == null || weapon == null || !unit.IsDirectlyControllable || weapon.Blueprint.Category.HasSubCategory(WeaponSubCategory.Natural)) return;
            if (unit.Proficiencies.Contains(weapon.Blueprint.Category)) return;
            if (weapon.IsShield && weapon.Shield?.Blueprint.Type.ProficiencyGroup == ArmorProficiencyGroup.Buckler) return;
            if (unit.Get<NewComponents.OffHandParry.OffHandParryUnitPart>()?.weapon == weapon) return;
            if (__instance.TryGetCustomData(Applied, out bool applied) && applied) return;
            __instance.SetCustomData(Applied, true);
            __instance.AddModifierCopy(new Kingmaker.RuleSystem.Rules.Modifier(-4, penalty, ModifierDescriptor.Penalty));
        }
    }
}

