using HarmonyLib;
using System.Linq;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
namespace CallOfTheWild.Wrath.Prototype
{
    // Change only the native Magus spell-combat hand gate, never the global
    // equipment/free-hand helpers used by other classes and providers.
    [HarmonyPatch(typeof(UnitPartMagus),nameof(UnitPartMagus.HasOneHandedMeleeWeaponAndFreehand))]
    internal static class MagusHands01454
    {
        internal static bool Extended(Kingmaker.EntitySystem.Entities.UnitEntityData unit,bool inserting=false)
        {
            if(unit==null)return false;
            if(Mindblade01454.WithManifest(unit,c=>c.AllowsSpellCombatGrip(inserting)))return true;
            if(!Skirnir01454.Has(unit,Skirnir01454.CombatToken)||Skirnir01454.Shield(unit)==null)return false;
            var weapon=unit.Body.PrimaryHand.MaybeWeapon;
            return weapon!=null&&weapon.Blueprint.IsMelee&&!weapon.Blueprint.Double&&(!weapon.Blueprint.IsTwoHanded||
                (weapon.Blueprint.Category==Kingmaker.Enums.WeaponCategory.BastardSword||weapon.Blueprint.Category==Kingmaker.Enums.WeaponCategory.DwarvenWaraxe)&&unit.Descriptor.Proficiencies.WeaponProficiencies.Contains(weapon.Blueprint.Category));
        }
        internal static System.Reflection.PropertyInfo FactProperty(System.Type type)
        {
            for(var t=type;t!=null;t=t.BaseType){var p=t.GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly).Where(x=>x.Name=="Fact"&&typeof(UnitFact).IsAssignableFrom(x.PropertyType)).ToArray();if(p.Length==1)return p[0];if(p.Length>1)throw new System.InvalidOperationException("01454 ambiguous native component fact API.");}
            throw new System.MissingMemberException(type.FullName,"Fact");
        }
        internal static bool IsCombatToggle(Kingmaker.UnitLogic.UnitFact fact)=>fact!=null&&(fact.Blueprint.AssetGuid==Kingmaker.Blueprints.BlueprintGuid.Parse("8898a573e8a8a184b8186dbc3a26da74")||fact.Blueprint.AssetGuid==PsychicAssets.Id(Skirnir01454.T("SpellCombatToggle")));
        static void Postfix(UnitDescriptor __0,ref bool __result)
        {
            if(__0?.Unit==null)return;var unit=__0.Unit;
            if(Skirnir01454.Has(unit,Skirnir01454.CombatToken)&&Skirnir01454.Shield(unit)==null){__result=false;return;}
            if(__result)return;
            __result=Extended(unit);
        }
    }
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestrictionByEquipmentSet),"IsAvailable")]
    internal static class MagusCombatGripAvailable01454
    {
        static readonly System.Reflection.PropertyInfo Fact=MagusHands01454.FactProperty(typeof(Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestrictionByEquipmentSet));
        static void Postfix(Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.ActivatableAbilityRestrictionByEquipmentSet __instance,ref bool __result)
        {if(__result)return;var fact=(UnitFact)Fact.GetValue(__instance,null);if(MagusHands01454.IsCombatToggle(fact)&&MagusHands01454.Extended(fact.Owner?.Unit))__result=true;}
    }
    [HarmonyPatch(typeof(Kingmaker.UnitLogic.ActivatableAbilities.DeactivateOnGripChanged),"TryDeactivate")]
    internal static class MagusCombatGripLifetime01454
    {
        static readonly System.Reflection.PropertyInfo Fact=MagusHands01454.FactProperty(typeof(Kingmaker.UnitLogic.ActivatableAbilities.DeactivateOnGripChanged));
        static bool Prefix(Kingmaker.UnitLogic.ActivatableAbilities.DeactivateOnGripChanged __instance)
        {if(!__instance.ThisIsSpellCombat)return true;var fact=(UnitFact)Fact.GetValue(__instance,null);return !(MagusHands01454.IsCombatToggle(fact)&&MagusHands01454.Extended(fact.Owner?.Unit,true));}
    }
}
