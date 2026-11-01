using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    // Navigation keeps the original native toggle facts and converted spell data.
    // Only the visible list is grouped: old quickbar entries remain valid.
    internal static class PsychicAmplificationMenu
    {
        internal const string RootToken="PhrenicPoolMenu";
        private static readonly string[] Tokens={RootToken,"DefensivePrognosticationMenu","OverpoweringMindMenu","PsychofeedbackMenu"};
        private static readonly BlueprintGuid RootId=Id(RootToken), FeedbackId=Id("PsychofeedbackMenu");
        private static readonly HashSet<BlueprintGuid> Heads=new HashSet<BlueprintGuid>(Tokens.Select(Id));
        private static readonly ConditionalWeakTable<ActionBarSlotVM,ActionBarSlotVM> Anchors=new ConditionalWeakTable<ActionBarSlotVM,ActionBarSlotVM>();
        internal static bool IsHead(BlueprintActivatableAbility bp)=>bp!=null && Heads.Contains(bp.AssetGuid);
        internal static void Build(PsychicAssets f,List<BlueprintActivatableAbility> modes,BlueprintFeature defense,BlueprintFeature mind,BlueprintFeature feedback)
        {
            BlueprintActivatableAbility Head(string token,string title,IEnumerable<BlueprintActivatableAbility> children) {
                const string description="Open the phrenic amplification menu. Choosing a mode switches immediately. Opening a menu costs no action or points. Use Back to Phrenic Pool to return. Use Psychofeedback from a known spell's conversion options.";
                var buff=f.Buff(token+"Navigation",title,description);
                SetEnum(buff,"m_Flags","HiddenInUi");
                var variants=new ActivatableAbilityVariants {name="PhrenicMenuChildren"};
                Set(variants,"m_Variants",children.Select(Ref<BlueprintActivatableAbilityReference>).ToArray());
                var result=f.Add(new BlueprintActivatableAbility {ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,
                    DeactivateIfOwnerDisabled=true,DeactivateIfOwnerUnconscious=true,ComponentsArray=token=="PsychofeedbackMenu"?Array.Empty<BlueprintComponent>():new BlueprintComponent[]{variants}},token,title,description);
                Set(result,"m_Buff",Ref<BlueprintBuffReference>(buff));Set(result,"m_ActivateWithUnitCommand",UnitCommand.CommandType.Free);
                return result;
            }
            void Grant(BlueprintFeature feature,BlueprintActivatableAbility head) {
                var facts=feature.GetComponent<AddFacts>();
                if(facts==null) feature.ComponentsArray=feature.ComponentsArray.Concat(new BlueprintComponent[]{Facts(head)}).ToArray();
                else Set(facts,"m_Facts",((BlueprintUnitFactReference[])Get(facts,"m_Facts")).Concat(new[]{Ref<BlueprintUnitFactReference>(head)}).ToArray());
            }
            var defenses=modes.Where(m=>m.Buff.GetComponent<PsychicAmplificationComponent>()?.Kind==PsychicAmplificationKind.DefensivePrognostication).ToArray();
            var minds=modes.Where(m=>m.Buff.GetComponent<PsychicAmplificationComponent>()?.Kind==PsychicAmplificationKind.OverpoweringMind).ToArray();
            var d=Head(Tokens[1],"Defensive Prognostication",defenses);Grant(defense,d);
            var m=Head(Tokens[2],"Overpowering Mind",minds);Grant(mind,m);
            var p=Head(Tokens[3],"Psychofeedback",Array.Empty<BlueprintActivatableAbility>());Grant(feedback,p);
            // Keep the 0.11.0 identity and any old pinned shortcut compatible,
            // but do not offer this redundant conversion entry in the new menu.
            Head(RootToken,"Phrenic Pool",new[]{d,m}.Concat(modes.Except(defenses).Except(minds)));
        }
        private static ActivatableAbility Menu(MechanicActionBarSlot slot) {
            var ability=(slot as MechanicActionBarSlotActivableAbility)?.ActivatableAbility;
            return IsHead(ability?.Blueprint)?ability:null;
        }
        internal static int PoolAmount(UnitEntityData unit) {
            var resources=unit?.Descriptor.Resources;if(resources==null)return 0;
            foreach(string id in new[]{PsychicClass.WisdomPoolId,PsychicClass.CharismaPoolId}) {
                var pool=ResourcesLibrary.TryGetBlueprint<BlueprintAbilityResource>(id);
                if(pool!=null && resources.ContainsResource(pool))return resources.GetResourceAmount(pool);
            }
            return 0;
        }
        internal static List<MechanicActionBarSlot> Children(ActivatableAbility head) {
            var unit=head.Owner;
            var result=new List<MechanicActionBarSlot>();
            if(head.Blueprint.AssetGuid==FeedbackId) {
                var book=unit.Spellbooks.FirstOrDefault(b=>b.Blueprint.AssetGuid.ToString()==PsychicClass.BookId);
                if(book!=null) for(int level=2;level<=9;level++) {
                    var known=book.GetKnownSpells(level).FirstOrDefault();if(known==null)continue;
                    foreach(var spell in known.GetConversions().Where(a=>a.Blueprint.name.StartsWith("CotWWrathPsychicPsychofeedback",StringComparison.Ordinal)))
                        result.Add(new MechanicActionBarSlotSpontaneusConvertedSpell {Spell=spell,Unit=unit});
                }
            } else foreach(var ability in head.GetConversions().Where(a=>!a.TemporarilyDisabled))
                result.Add(new MechanicActionBarSlotActivableAbility {ActivatableAbility=ability,Unit=unit,IsHiddenOverride=false});
            if(head.Blueprint.AssetGuid==RootId)foreach(var mode in unit.Facts.List.OfType<ActivatableAbility>().Where(a=>a.Blueprint.Buff?.GetComponent<PsychicAmplificationComponent>()?.Kind>=PsychicAmplificationKind.WillOfTheDead||a.Blueprint.Buff?.GetComponent<MimicMetamagic01458>()!=null))
                if(!result.OfType<MechanicActionBarSlotActivableAbility>().Any(slot=>ReferenceEquals(slot.ActivatableAbility,mode)))result.Add(new MechanicActionBarSlotActivableAbility{ActivatableAbility=mode,Unit=unit,IsHiddenOverride=false});
            return result;
        }
        private static void Open(ActionBarSlotVM clicked) {
            var menu=Menu(clicked.MechanicActionBarSlot);if(menu==null)return;
            bool nested=Anchors.TryGetValue(clicked,out var anchor);if(!nested)anchor=clicked;
            if(!nested && anchor.ConvertedVm.Value!=null && !anchor.ConvertedVm.Value.IsDisposed) {anchor.CloseConvert();return;}
            var slots=Children(menu);
            bool submenu=menu.Blueprint.AssetGuid!=RootId;
            if(submenu) {
                var root=menu.Owner.Facts.List.OfType<ActivatableAbility>().FirstOrDefault(a=>a.Blueprint.AssetGuid==RootId);
                if(root!=null)slots.Insert(0,new MechanicActionBarSlotActivableAbility {ActivatableAbility=root,Unit=menu.Owner,IsHiddenOverride=false});
            }
            // Native conversion views close every other conversion view on opening.
            // Reuse the outer slot's popup rather than creating a doomed nested view.
            anchor.CloseConvert();
            var popup=new ActionBarConvertedVM(slots,anchor.CloseConvert);
            foreach(var child in popup.Slots)Anchors.Add(child,anchor);
            if(submenu && popup.Slots.Count>0 && Menu(popup.Slots[0].MechanicActionBarSlot)?.Blueprint.AssetGuid==RootId) {
                popup.Slots[0].Name.Value="Back to Phrenic Pool";
                popup.Slots[0].Tooltip.Value=new Kingmaker.UI.MVVM._VM.Tooltip.Templates.TooltipTemplateSimple(
                    "Back to Phrenic Pool","Return to your learned phrenic amplifications. This costs no action or points.");
            }
            anchor.ConvertedVm.Value=popup;
        }
        [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.OnMainClick))]
        internal static class ClickPatch {
            private static bool Prefix(ActionBarSlotVM __instance) {if(Menu(__instance.MechanicActionBarSlot)==null)return true;Open(__instance);return false;}
        }
        [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.OnShowConvertRequest))]
        internal static class ExpandPatch {
            private static bool Prefix(ActionBarSlotVM __instance) {if(Menu(__instance.MechanicActionBarSlot)==null)return true;Open(__instance);return false;}
        }
        [HarmonyPatch(typeof(MechanicActionBarSlotActivableAbility),nameof(MechanicActionBarSlotActivableAbility.GetResource))]
        internal static class CounterPatch {
            private static void Postfix(MechanicActionBarSlotActivableAbility __instance,ref int __result) {
                if(__instance.ActivatableAbility?.Blueprint.AssetGuid==RootId)__result=PoolAmount(__instance.Unit);
            }
        }
        [HarmonyPatch(typeof(ActionBarSlotVM),nameof(ActionBarSlotVM.UpdateResource))]
        internal static class AvailablePatch {
            private static void Postfix(ActionBarSlotVM __instance) {
                if(Menu(__instance.MechanicActionBarSlot)==null)return;
                // Navigation also works at zero points, including switching a mode off.
                __instance.IsPossibleActive.Value=true;__instance.HasConvert.Value=true;
            }
        }
        [HarmonyPatch(typeof(ActionBarVM),"CollectAbilities")]
        internal static class GroupPatch {
            private static void Postfix(ActionBarVM __instance,UnitEntityData unit) {
                if(!unit.Facts.List.OfType<ActivatableAbility>().Any(a=>a.Blueprint.AssetGuid==RootId))return;
                foreach(var slot in __instance.GroupAbilities.ToArray()) {
                    var bp=(slot.MechanicActionBarSlot as MechanicActionBarSlotActivableAbility)?.ActivatableAbility?.Blueprint;
                    if(bp!=null && bp.AssetGuid!=RootId && (IsHead(bp)||bp.Buff?.GetComponent<PsychicAmplificationComponent>()!=null||bp.Buff?.GetComponent<MimicMetamagic01458>()!=null)) {
                        __instance.GroupAbilities.Remove(slot);slot.Dispose();
                    }
                }
            }
        }
    }
}
