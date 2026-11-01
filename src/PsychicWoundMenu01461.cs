using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;

namespace CallOfTheWild.Wrath.Prototype
{
    // One targeting ability and four actual native toggles share one popup.
    // Target is a variant of the granted parent, never a second granted fact.
    internal static class PsychicWoundMenu01461
    {
        static readonly BlueprintGuid Parent=PsychicAssets.Id("AgonizingWoundMenu01460");
        static readonly HashSet<BlueprintGuid> Modes=new HashSet<BlueprintGuid>(Enumerable.Range(0,4).Select(i=>PsychicAssets.Id("AgonizingWound"+i+"01458Toggle")));
        internal static AbilityData Head(MechanicActionBarSlot slot)
        {
            var a=(slot as MechanicActionBarSlotAbility)?.Ability;
            return a?.Blueprint.AssetGuid==Parent?a:null;
        }
        internal static List<MechanicActionBarSlot> Children(AbilityData head)
        {
            var unit=head.Caster.Unit;
            var slots=new MechanicActionBarSlotAbility{Ability=head,Unit=unit}.GetConvertedAbilityData().GetMechanicSlots(unit).ToList();
            slots.AddRange(unit.ActivatableAbilities.Enumerable.Where(a=>Modes.Contains(a.Blueprint.AssetGuid)&&!a.TemporarilyDisabled)
                .Select(a=>new MechanicActionBarSlotActivableAbility{ActivatableAbility=a,Unit=unit,IsHiddenOverride=false}));
            return slots;
        }
        internal static void Open(ActionBarSlotVM vm)
        {
            var head=Head(vm.MechanicActionBarSlot);if(head==null)return;
            if(vm.ConvertedVm.Value!=null&&!vm.ConvertedVm.Value.IsDisposed){vm.CloseConvert();return;}
            vm.ConvertedVm.Value=new ActionBarConvertedVM(Children(head),vm.CloseConvert);
        }
    }
    [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.OnMainClick))]
    internal static class AgonizingOpen01461Patch
    {static bool Prefix(ActionBarSlotVM __instance){if(PsychicWoundMenu01461.Head(__instance.MechanicActionBarSlot)==null)return true;PsychicWoundMenu01461.Open(__instance);return false;}}
    [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.OnShowConvertRequest))]
    internal static class AgonizingExpand01461Patch
    {static bool Prefix(ActionBarSlotVM __instance){if(PsychicWoundMenu01461.Head(__instance.MechanicActionBarSlot)==null)return true;PsychicWoundMenu01461.Open(__instance);return false;}}
    [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.UpdateResource))]
    internal static class AgonizingMenuAvailable01461Patch
    {static void Postfix(ActionBarSlotVM __instance){if(PsychicWoundMenu01461.Head(__instance.MechanicActionBarSlot)==null)return;__instance.HasConvert.Value=true;__instance.IsPossibleActive.Value=true;}}
}
