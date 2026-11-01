using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Martial.Defence;
using Way_of_the_shield.NewComponents;

namespace Way_of_the_shield {
 [HarmonyPatch]
 public static class ShieldAcState {
  [ThreadStatic] static bool refreshing;
  // Native AC VM observes total AC, but its flat-footed/touch children are
  // snapshots. Keep open pages bound to the current stat, with weak UI links.
  static readonly ConditionalWeakTable<UnitEntityData,List<WeakReference>> panels = new();
  public static void Refresh(UnitEntityData unit) {
   if(refreshing||unit?.Stats?.AC==null)return;
   try{refreshing=true;
    foreach(var fact in unit.Buffs.Enumerable.ToArray())
     fact.CallComponents<ShieldedDefenseAcBonus>(c=>c.Refresh());
    unit.Stats.AC.UpdateValue();
    UpdatePanels(unit);
   }finally{refreshing=false;}
  }
  [HarmonyPatch(typeof(CharInfoArmorClassVM),MethodType.Constructor,new[]{typeof(UnitDescriptor)})][HarmonyPrefix]
  static void BeforePanel(UnitDescriptor __0)=>Refresh(__0?.Unit);
  [HarmonyPatch(typeof(CharInfoArmorClassVM),MethodType.Constructor,new[]{typeof(UnitDescriptor)})][HarmonyPostfix]
  static void BindPanel(CharInfoArmorClassVM __instance,UnitDescriptor __0){
   if(__0?.Unit==null)return;
   var list=panels.GetOrCreateValue(__0.Unit);
   list.RemoveAll(w=>!w.IsAlive);
   list.Add(new WeakReference(__instance));
   UpdatePanels(__0.Unit);
  }
  static void UpdatePanels(UnitEntityData unit){
   if(!panels.TryGetValue(unit,out var list))return;
   var ac=unit.Stats.AC;
   list.RemoveAll(w=>!w.IsAlive);
   foreach(var w in list)if(w.Target is CharInfoArmorClassVM p){
    if(p.AC?.StatValue!=null)p.AC.StatValue.Value=ac.ModifiedValue;
    if(p.FlatFooted?.StatValue!=null)p.FlatFooted.StatValue.Value=ac.FlatFooted;
    if(p.Touch?.StatValue!=null)p.Touch.StatValue.Value=ac.Touch;
    if(p.FlatFootedTouch?.StatValue!=null)p.FlatFootedTouch.StatValue.Value=ac.FlatFootedTouch;
   }
  }
  [HarmonyPatch(typeof(ModifiableValue),nameof(ModifiableValue.UpdateValue))][HarmonyPrefix]
  static void RefreshPanel(ModifiableValue __instance){
   if(!refreshing&&__instance is ModifiableValueArmorClass ac)Refresh(ac.Owner?.Unit);
  }
  static bool IsWornShieldModifier(UnitEntityData unit,ModifiableValue.Modifier m){
   var shield=unit?.Body?.SecondaryHand?.MaybeShield;
   if(shield==null||m==null||m.ModValue<=0)return false;
   if(m.ModDescriptor is ModifierDescriptor.Shield or ModifierDescriptor.ShieldEnhancement)
    return m.ItemSource==shield||m.ItemSource==shield.ArmorComponent;
   return m.ModDescriptor==ModifierDescriptor.Focus && (m.ItemSource==shield||m.ItemSource==shield.ArmorComponent||
    m.Source?.Blueprint?.name?.IndexOf("ShieldFocus",StringComparison.OrdinalIgnoreCase)>=0);
  }
  [HarmonyPatch(typeof(ModifiableValue),"ApplyModifiersFiltered")][HarmonyPrefix]
  static void Filter(ModifiableValue __instance,ref Func<ModifiableValue.Modifier,bool> filter){
   if(__instance is not ModifiableValueArmorClass ac)return;
   var unit=ac.Owner?.Unit;
   bool denied=ShieldRuntimePatches.IsShieldDenied(unit);
   bool flat=filter==ModifiableValueArmorClass.FilterAllowedForFlatFooted||filter==ModifiableValueArmorClass.FilterAllowedForFlatFootedTouch;
   if(!denied&&!flat)return;
   var original=filter;
   filter=m=>(original==null||original(m))&&(!denied||!IsWornShieldModifier(unit,m))&&(!flat||m.Source?.Blueprint?.AssetGuid.ToString()!="29397505648a462a874872b132a00b75");
  }
  [HarmonyPatch(typeof(StatModifiersBreakdown),nameof(StatModifiersBreakdown.GetDisplayArmorModifiers))][HarmonyPostfix]
  static void Display(ModifiableValueArmorClass __0,bool __1,ref IEnumerable<ModifiableValue.Modifier> __result){
   if(__result==null)return;
   var unit=__0?.Owner?.Unit;bool denied=ShieldRuntimePatches.IsShieldDenied(unit);
   if(denied||__1)__result=__result.Where(m=>(!denied||!IsWornShieldModifier(unit,m))&&(!__1||m.Source?.Blueprint?.AssetGuid.ToString()!="29397505648a462a874872b132a00b75")).ToArray();
  }
  // Character-sheet StatTooltipData uses the generic tuple builder, rather
  // than GetDisplayArmorModifiers used by combat AC. Filter the same source
  // here without changing the stat's persistent modifier collection.
  [HarmonyPatch(typeof(StatModifiersBreakdown),"GetDisplayModifiersInternal")][HarmonyPostfix]
  static void SheetDisplay(ModifiableValue __0,ref IEnumerable<(ModifiableValue.Modifier,Kingmaker.UI.IUIDataProvider)> __result){
   if(__result==null||__0 is not ModifiableValueArmorClass ac||!ShieldRuntimePatches.IsShieldDenied(ac.Owner?.Unit))return;
   __result=__result.Where(x=>!IsWornShieldModifier(ac.Owner.Unit,x.Item1)).ToArray();
  }
  [HarmonyPatch(typeof(AddMechanicsFeature.Runtime),"OnTurnOn")][HarmonyPostfix]
  static void FlagChanged(AddMechanicsFeature.Runtime __instance){
   if(__instance.Fact?.Blueprint.GetComponents<AddMechanicsFeature>().Any(c=>c.m_Feature==MechanicsFeatureExtension.ShieldDenied)==true)Refresh(__instance.Fact.Owner as UnitEntityData);
  }
  [HarmonyPatch(typeof(AddMechanicsFeature.Runtime),"OnTurnOff")][HarmonyPostfix]
  static void FlagRemoved(AddMechanicsFeature.Runtime __instance)=>FlagChanged(__instance);
 }
}
