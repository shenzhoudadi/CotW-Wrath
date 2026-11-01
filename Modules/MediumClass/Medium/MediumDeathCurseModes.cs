using System;
using System.Linq;
using HarmonyLib;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using MediumClass.Medium.NewUnitParts;
using MediumClass.Utilities;

namespace MediumClass.Medium
{
    [TypeId("67804ce4a48d5868b969cd2b0ead8404")]
    public class DeathCurseModeRestriction : ActivatableAbilityRestriction
    {
        public BlueprintBuffReference Energy;
        public override bool IsAvailable() => Owner.Get<UnitPartMedium>()?.IsActiveSpirit(
            BlueprintTool.GetRef<BlueprintCharacterClassReference>(Guids.Adriel))==true
            && MediumDeathCurseRules.CanToggle(Owner,Energy?.Get());
    }

    internal static class MediumDeathCurseModes
    {
        internal static readonly (string Mode,string Buff)[] Modes={
            (Guids.MediumAdrielDeathCurseToggleNegative,Guids.MediumAdrielDeathCurseBuffNegative),
            (Guids.MediumAdrielDeathCurseToggleFire,Guids.MediumAdrielDeathCurseBuffFire),
            (Guids.MediumAdrielDeathCurseToggleCold,Guids.MediumAdrielDeathCurseBuffCold),
            (Guids.MediumAdrielDeathCurseToggleElectricity,Guids.MediumAdrielDeathCurseBuffElectricity),
            (Guids.MediumAdrielDeathCurseToggleAcid,Guids.MediumAdrielDeathCurseBuffAcid)
        };
        internal static readonly string[] LegacyAbilities={Guids.MediumAdrielDeathCurseAbility,
            Guids.MediumAdrielDeathCurseAbilityNegative,Guids.MediumAdrielDeathCurseAbilityFire,Guids.MediumAdrielDeathCurseAbilityCold,
            Guids.MediumAdrielDeathCurseAbilityElectricity,Guids.MediumAdrielDeathCurseAbilityAcid};

        internal static void Restore(UnitDescriptor owner)
        {
            if(owner?.Unit==null) return;
            var power=BlueprintTool.Get<BlueprintFeature>(Guids.MediumAdrielDeathCurse);
            var powerFact=owner.Unit.Facts.Get(power);
            bool granted=powerFact?.IsActive==true && !powerFact.Deactivating;
            // Keep old GUIDs as BlueprintAbility for saved references, but retire their old buttons.
            foreach(var fact in owner.Unit.Facts.List.Where(f=>f is Ability &&
                LegacyAbilities.Any(id=>f.Blueprint.AssetGuid==BlueprintGuid.Parse(id))).ToArray())
                MediumSpiritPowerCleanup.RemoveOwnedFact(owner.Unit,fact);
            // Repair through native AddFacts data, so the new modes stay owned by
            // the power and are revoked with it (not merely added as loose facts).
            if(granted) powerFact.CallComponents<AddFacts>(c=>
                AccessTools.Method(typeof(AddFacts),"UpdateFacts").Invoke(c,new object[]{true}));
            foreach(var pair in Modes) {
                var bp=BlueprintTool.Get<BlueprintActivatableAbility>(pair.Mode);
                var current=owner.Unit.Facts.Get(bp) as ActivatableAbility;
                if(!granted) {
                    if(current!=null) MediumSpiritPowerCleanup.RemoveOwnedFact(owner.Unit,current);
                    owner.Buffs.RemoveFact(BlueprintTool.Get<BlueprintBuff>(pair.Buff));
                    continue;
                }
                // An old energy has no owning ActivatableAbility. Preserve its selection by
                // moving it to the new native mode; a native-owned buff is left untouched.
                var orphan=owner.Buffs.Enumerable.FirstOrDefault(b=>b.Blueprint.AssetGuid==BlueprintGuid.Parse(pair.Buff)
                    && !ReferenceEquals(b,current?.AppliedBuff));
                if(orphan!=null && current!=null) {
                    owner.Buffs.RemoveFact(orphan);
                    current.IsOn=true;
                }
            }
        }
    }
}
