using System;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using CallOfTheWild.Wrath.Prototype;
internal static class Ally01413Tests {
 static int n;
 static void Check(bool b,string s){if(!b)throw new Exception("Ally01413: "+s);n++;}
 static object Helper(string name,params object[] args)=>AccessTools.Method(typeof(WildShapeFeatTests),name).Invoke(null,args);
 internal static int Run(BlueprintsCache c){
  var ius=(BlueprintFeature)c.Load(BlueprintGuid.Parse("7812ad3672a4b9a4fb894ea402095167"));Check(ius!=null,"native Improved Unarmed Strike dependency");
  foreach(string token in new[]{"SpiritualWeapon","SpiritualAlly","MagesSword","TwilightKnife","ArcaneEye"}){
   var unit=Psychic034Tests.Asset<BlueprintUnit>(c,token+"Unit");var feature=unit.AddFacts.Single();
   Check(feature.GetComponents<AddFacts>().Any(a=>a.Facts.Contains(ius))==(token=="SpiritualAlly"),"only Ally receives native unarmed feat: "+token);
  }
  var u=(UnitEntityData)Helper("Unit",c,"01413-unarmed-ally");AccessTools.Field(typeof(UnitMechanicFeatures),"ImprovedUnarmedStrike").SetValue(u.State.Features,new CountableFlag());
  var fist=(ItemEntityWeapon)Helper("Weapon",u,WeaponCategory.UnarmedStrike,false,PhysicalDamageForm.Bludgeoning,"01413-fist");AccessTools.Field(typeof(ItemEntityWeapon),"m_Blueprint").SetValue(fist,Psychic034Tests.Asset<Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon>(c,"SpiritualAllyUnarmedStrikeItem"));Helper("Equip",u.Body.PrimaryHand,fist);
  Check(fist.Blueprint.IsUnarmed&&u.GetThreatHand()==null,"native unarmed form cannot threaten without feat");
  var fact=(Feature)Helper("Fact",u,ius);var runtime=fact.Components.Single(x=>x.SourceBlueprintComponent is AddMechanicsFeature);
  using(runtime.RequestEventContext())AccessTools.Method(runtime.GetType(),"OnTurnOn").Invoke(runtime,null);
  Check(u.State.Features.ImprovedUnarmedStrike&&u.GetThreatHand()==u.Body.PrimaryHand,"native feat activation restores native unarmed threat hand");
  using(runtime.RequestEventContext())AccessTools.Method(runtime.GetType(),"OnTurnOff").Invoke(runtime,null);
  Check(!u.State.Features.ImprovedUnarmedStrike&&u.GetThreatHand()==null,"native flag lifecycle remains reversible");
  PsychicExpansionTests.Inventory["ally01413Assertions"]=n;Console.WriteLine("PASS: "+n+" Ally native unarmed feat/flag/threat checks; final AoO command covered by existing engagement regression boundary.");return n;
 }
}
