using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.UI.UnitSettings;
using UnityEngine;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities;
namespace CallOfTheWild.Wrath.Prototype
{
    // Keep the new reflection cache outside the old conversion class so its
    // compiler-generated lambda/field identities remain unchanged as well.
    internal static class SkirnirStorageSource01455
    {
        static readonly System.Reflection.FieldInfo OwnedFact=AccessTools.Field(typeof(AbilityData),"m_Fact")??throw new System.MissingFieldException(typeof(AbilityData).FullName,"m_Fact");
        internal static void Bind(AbilityData converted,Ability source)
        {
            if(converted==null||source==null||!ReferenceEquals(converted.Blueprint,source.Blueprint)||!ReferenceEquals(converted.Caster,source.Owner)||converted.Blueprint.GetComponent<SkirnirStoreSpell01454>()==null)throw new System.InvalidOperationException("01455 storage source identity mismatch.");
            OwnedFact.SetValue(converted,source);
        }
    }
    // The native grant restores only a non-null, missing AppliedFact from the
    // game's restore allow-list. A saved null child is never reapplied. Keep
    // the native level predicate, add/remove, source data and level-up handler;
    // only these two Mindblade grants receive a post-load reconciliation.
    [TypeId("ae67496ce92a4fdd9fe13d8ef5b15555")]
    public sealed class MindbladeModeGrant01455:AddFeatureOnClassLevel
    {
        protected override void OnApplyPostLoadFixes()
        {
            base.OnApplyPostLoadFixes();
            if(Fact.Blueprint.AssetGuid!=MagusBatch01454.Identity(Mindblade01454.DualToken)||Class?.AssetGuid!=BlueprintGuid.Parse(MagusBatch01454.MagusId))return;
            if(Feature?.AssetGuid!=MagusBatch01454.Identity("MindbladeDualModeGrant01454")&&Feature?.AssetGuid!=MagusBatch01454.Identity("MindbladeDualMode2Grant01454"))return;
            HandleUnitGainLevel();
        }
    }
    // Native converted slots normally use ConvertedFrom's icon. Manifestation
    // variants are individual weapons, so show the selected leaf's own icon.
    [HarmonyPatch(typeof(MechanicActionBarSlotSpontaneusConvertedSpell),nameof(MechanicActionBarSlotSpontaneusConvertedSpell.GetIcon))]
    internal static class MindbladeVariantIcons01455
    {
        static void Postfix(MechanicActionBarSlotSpontaneusConvertedSpell __instance,ref Sprite __result)
        {if(__instance.Spell?.Blueprint.GetComponent<MindbladeManifestCost01454>()!=null)__result=__instance.Spell.Icon;}
    }
}
