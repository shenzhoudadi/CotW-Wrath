using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Martial.Defence;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.LevelClassScores.AbilityScores;
using Way_of_the_shield;
using Way_of_the_shield.NewFeatsAndAbilities;
using static Way_of_the_shield.NewComponents.OffHandParry;

public static partial class FeatRuntimeTests {
 static void Feedback01410(){
  var u=Unit("native-denial-lifecycle");var shield=Equip(u,ArmorProficiencyGroup.Buckler);
  var ac=new ModifiableValueArmorClass(u.Stats);Set(u.Stats,"AC",ac);u.Stats.AllStats.Add(ac);ac.BaseValue=10;
  ac.AddModifier(8,(EntityFact)null,ModifierDescriptor.Armor);ac.AddItemModifier(3,shield.ArmorComponent,ModifierDescriptor.Shield);
  var sheet=AccessTools.Method(typeof(Kingmaker.UI.Common.StatModifiersBreakdown),"GetDisplayModifiersInternal");
  Func<ModifiableValue.Modifier[]> detail=()=>((System.Collections.Generic.IEnumerable<(ModifiableValue.Modifier,Kingmaker.UI.IUIDataProvider)>)sheet.Invoke(null,new object[]{ac,true})).Select(x=>x.Item1).ToArray();
  Check(detail().Any(m=>m.ItemSource==shield.ArmorComponent),"native generic sheet tooltip shows shield before denial");
  int display=ac.ModifiedValue,notifications=0;Action<ModifiableValue,int> observer=(s,old)=>{display=s.ModifiedValue;notifications++;};
  AccessTools.Method(typeof(ModifiableValue),"add_OnChanged").Invoke(ac,new object[]{observer});
  var panel=Empty<CharInfoArmorClassVM>();
  panel.AC=PanelStat(ac.ModifiedValue);panel.FlatFooted=PanelStat(ac.FlatFooted);panel.Touch=PanelStat(ac.Touch);panel.FlatFootedTouch=PanelStat(ac.FlatFootedTouch);
  AccessTools.Method(typeof(ShieldAcState),"BindPanel").Invoke(null,new object[]{panel,u.Descriptor});
  var b=Fact(u,BoringBucklerTweaks.Buckler1h_Component.ShieldForbiddance,u);
  var runtime=b.Components.Single(c=>c.SourceBlueprintComponent is AddMechanicsFeature);
  AccessTools.Method(runtime.GetType(),"OnTurnOn").Invoke(runtime,null);
  Console.WriteLine("Denial native lifecycle stat="+ac.ModifiedValue+" display="+display+" notifications="+notifications+" owner="+b.Owner.GetType());
  Check(display==18&&ac.ModifiedValue==18,"native denial enable updates subscribed panel without manual refresh");
  Check(!detail().Any(m=>m.ItemSource==shield.ArmorComponent)&&detail().Any(m=>m.ModDescriptor==ModifierDescriptor.Armor),"native character-sheet tooltip filters denied shield and retains armor");
  Check(panel.AC.StatValue.Value==18&&panel.FlatFooted.StatValue.Value==ac.FlatFooted&&panel.Touch.StatValue.Value==ac.Touch&&panel.FlatFootedTouch.StatValue.Value==ac.FlatFootedTouch,"open native AC VM and snapshot children follow denial");
  AccessTools.Method(runtime.GetType(),"OnTurnOff").Invoke(runtime,null);
  Check(display==21&&ac.ModifiedValue==21&&notifications==2,"native denial expire restores subscribed panel exactly once");
  Check(detail().Any(m=>m.ItemSource==shield.ArmorComponent),"native generic sheet tooltip restores shield after expiry");
  Check(panel.AC.StatValue.Value==21&&panel.FlatFooted.StatValue.Value==ac.FlatFooted,"open native panel restores worn shield after expiry");
  var twf=(BlueprintFeature)cache.Load(BlueprintGuid.Parse("6948b379c0562714d9f6d58ccbfa8faa"));
  // Reuse the native mechanics component installed by the previous native-count scenario.
  var tf=Feature(u,twf,1);Subscribe(tf,true);
  var mf=Feature(u,Mythic_BucklerParry.Mythic_BucklerParryFeature,1);
  var context=new MechanicsContext(u,u,mf.Blueprint,null);context.RecalculateRanks();Set(mf,"m_Context",context);
  Subscribe(mf,true);Set(tf,"Rank",4);nativeCount=true;
  var part=u.Get<OffHandParryUnitPart>();
  try{
   part.HandleTurnStarted(u);part.TryActivate(WeaponCategory.WeaponLightShield,false,new OffHandParryData());
   Console.WriteLine("Existing context rank="+context[AbilityRankType.Default]+" full-TWF roster="+string.Join(",",part.State.AttackPenalties));
  Check(part.n==6&&part.State.AttackPenalties.SequenceEqual(new[]{0,0,0,0,5,10}),"stale feature context still calculates current full-TWF six parries");
   Check(context[AbilityRankType.Default]==0,"existing context remains unchanged");
  }finally{nativeCount=false;part.Deactivate();Subscribe(mf,false);Subscribe(tf,false);}
 }
 static CharInfoStatVM PanelStat(int value){var vm=Empty<CharInfoStatVM>();vm.StatValue=new UniRx.IntReactiveProperty(value);return vm;}
}
