using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Utility;

namespace CallOfTheWild.Wrath.Prototype
{
    // The granted child buff is the active mutation, not merely the learned
    // feature. All melee attacks gain reach; weapon assets and ranged attacks
    // remain unchanged. The native touch approach includes its own range path.
    internal static class MutationReach01458
    {
        internal static readonly BlueprintGuid Active=PsychicAssets.Id("ElongatedFingersBodilyMutationBuff01457");
        internal static bool Enabled(UnitDescriptor owner)=>owner?.Buffs.Enumerable.Any(b=>b.Blueprint.AssetGuid==Active&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed)==true;
        internal static bool MeleeTouch(AbilityData ability)=>ability?.Caster!=null&&ability.Range==AbilityRange.Touch&&!ability.HasMetamagic(Metamagic.Reach)&&ability.GetDeliverBlueprint()?.GetComponent<AbilityDeliverTouch>()?.TouchWeapon?.AttackType==AttackType.Touch;
    }
    [HarmonyPatch(typeof(AbilityData),nameof(AbilityData.GetApproachDistance))]
    internal static class MutationTouchApproach01458Patch
    {
        static void Postfix(AbilityData __instance,ref float __result){if(MutationReach01458.MeleeTouch(__instance)&&MutationReach01458.Enabled(__instance.Caster))__result+=5.Feet().Meters;}
    }
    [HarmonyPatch(typeof(UnitDescriptor),nameof(UnitDescriptor.GetWeaponRange))]
    internal static class MutationTouchWeapon01458Patch
    {
        static void Postfix(UnitDescriptor __instance,BlueprintItemWeapon __0,ref Feet __result){if(__0!=null&&(__0.AttackType==AttackType.Melee||__0.AttackType==AttackType.Touch)&&MutationReach01458.Enabled(__instance))__result=new Feet(__result.Value+5);}
    }
}
