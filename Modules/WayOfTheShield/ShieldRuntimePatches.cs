using HarmonyLib;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using System;
using System.Linq;
using System.Reflection;
using Way_of_the_shield.NewComponents;

namespace Way_of_the_shield
{
    // Only shield use changes AC and attack penalties; no facing or flanking changes.
    [HarmonyPatch]
    public static class ShieldRuntimePatches
    {
        // The native delegate hierarchy declares two differently typed "Fact"
        // properties. A hierarchy-wide lookup throws AmbiguousMatchException and
        // prevents this patch class from initializing, including its AC patch.
        private static readonly MethodInfo FightingFact = typeof(UnitFactComponentDelegate<Kingmaker.EntitySystem.EmptyComponentData>)
            .GetProperty("Fact", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .GetGetMethod(true);
        public static bool IsShieldDenied(UnitEntityData unit)
            => unit?.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.ShieldDenied ?? false;

        public static int CalculateEquippedShieldBonus(UnitEntityData unit)
        {
            var shield = unit?.Body?.SecondaryHand?.MaybeShield;
            if (shield == null) return 0;
            // Display modifiers are the effective modifiers after stacking, not every stored bonus.
            return StatModifiersBreakdown.GetDisplayArmorModifiers(unit.Stats.AC, false, false)
                .Where(m => m != null && m.ModValue > 0 &&
                    (((m.ModDescriptor == ModifierDescriptor.Shield || m.ModDescriptor == ModifierDescriptor.ShieldEnhancement)
                        && (m.ItemSource == shield || m.ItemSource == shield.ArmorComponent))
                        || m.ModDescriptor == ModifierDescriptor.Focus))
                .Sum(m => m.ModValue);
        }

        [HarmonyPatch(typeof(RuleCalculateAC), nameof(RuleCalculateAC.OnTrigger))]
        [HarmonyPrefix]
        private static void DenyEquippedShieldAC(RuleCalculateAC __instance)
        {
            ShieldAcState.Refresh(__instance.Target);
        }

        public static int CalculateForcedDualWieldingPenalty(int rank, bool primaryAttack, bool lightOffhand, bool mythic, bool effortless)
        {
            int penalty = rank <= 1 ? (primaryAttack ? -4 : -8) : (mythic ? 0 : -2);
            return penalty + (lightOffhand || effortless ? 0 : -2);
        }

        [HarmonyPatch(typeof(TwoWeaponFightingAttackPenalty), nameof(TwoWeaponFightingAttackPenalty.OnEventAboutToTrigger))]
        [HarmonyPrefix]
        private static bool ApplyReservedShieldHandPenalty(TwoWeaponFightingAttackPenalty __instance, RuleCalculateAttackBonusWithoutTarget evt)
        {
            var unit = evt.Initiator;
            bool reserved=unit?.Get<MechanicsFeatureExtension.MechanicsFeatureExtensionPart>()?.ForceDualWieldingPenalties ?? false;
            if(unit?.Body?.PrimaryHand==null||unit.Body.SecondaryHand==null)return true;
            var primary = unit.Body.PrimaryHand.MaybeWeapon;
            var parry = unit.Get<OffHandParry.OffHandParryUnitPart>();
            var shield = unit.Body.SecondaryHand.MaybeShield;
            var secondary = parry?.activated == true ? parry.weapon : reserved ? shield?.WeaponComponent ?? unit.Body.SecondaryHand.MaybeWeapon : unit.Body.SecondaryHand.MaybeWeapon;
            if (primary == null || (secondary == null && shield == null) || (evt.Weapon != primary && evt.Weapon != secondary)) return true;
            bool master=unit.Descriptor.State.AdditionalFeatures.ShieldMaster;
            if(!reserved){
                // Native Shield Master skips both hands. Its benefit is for attacks
                // made with a shield; retain ordinary TWF on the other weapon.
                if(!master||evt.Weapon.IsShield||secondary==null||!(primary.IsShield||secondary.IsShield)||
                    evt.Reason?.Rule is RuleAttackWithWeapon attack&&!attack.IsFullAttack)return true;
            }
            if(master&&evt.Weapon.IsShield)return false;
            bool light = secondary?.Blueprint.IsLight ?? (shield?.Blueprint.Type.ProficiencyGroup is ArmorProficiencyGroup.Buckler or ArmorProficiencyGroup.LightShield);
            var training = unit.Get<UnitPartWeaponTraining>();
            bool effortless = unit.State.Features.EffortlessDualWielding && secondary != null && training != null && training.IsSuitableWeapon(secondary);
            bool mythic = __instance.MythicBlueprint != null && unit.HasFact(__instance.MythicBlueprint);
            var fact = FightingFact.Invoke(__instance, null) as UnitFact;
            if (fact == null) return true;
            int penalty = CalculateForcedDualWieldingPenalty(fact.GetRank(), evt.Weapon == primary, light, mythic, effortless);
            if (penalty != 0) evt.AddModifier(penalty, fact, ModifierDescriptor.UntypedStackable);
            return false;
        }
    }
}
