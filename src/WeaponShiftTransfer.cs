using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype {
 public sealed class WeaponShiftData {
  [JsonProperty] public List<ItemEnchantment> Applied=new List<ItemEnchantment>();
  [JsonProperty] public string Signature;
 }
 [TypeId("d01788c0693747b69fb39711998fd236")]
 public sealed class WeaponShiftTransfer:UnitFactComponentDelegate<WeaponShiftData>,IPolymorphActivatedHandler,IPolymorphDeactivatedHandler,IGlobalSubscriber,IInitiatorRulebookHandler<RuleCalculateWeaponStats>,IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>,IInitiatorRulebookHandler<RuleCalculateCMB> {
  internal static readonly HashSet<string> Materials=new HashSet<string>{"e5990dc76d2a613409916071c898eee8","0ae8fc9f2e255584faf4d14835224875","ab39e7d59dd12f4429ffef5dca88dc7b"};
  internal static readonly HashSet<string> Magic=new HashSet<string>{"57315bc1e1f62a741be0efde688087e9","0ca43051edefcad4b9b2240aa36dc8d4","66e9e299c9002ea4bb65b6f300e43770","633b38ff1d11de64a91d490c683ab1c8","0f20d79b7049c0f4ca54ca3d1ea44baa","30f90becaaac51f41bf56641966c4121","3f032a3cd54e57649a0cdad0434bf221","421e54078b7719d40915ce0672511d0b","564a6924b246d254c920a7c44bf2a58b","b606a3f5daa76cc40add055613970d2a","47857e1a5a3ec1a46adf6491b1423b4f","28a9964d81fedae44bae3ca45710c140","102a9c8c9b7a75e4fb5844e79deaf4c0","7bda5277d36ad114f9f9fd21d0dab658","914d7ee77fb09d846924ca08bccee0ff","f1c0c50108025d546b2554674ea1c006","690e762f7704e1f4aa1ac69ef0ce6a96","d05753b8df780fc4bb55b318f06af453","a1455a289da208144981e4b1ef92cc56"};
  internal static readonly HashSet<WeaponCategory> Trip=new HashSet<WeaponCategory>{WeaponCategory.Sickle,WeaponCategory.Flail,WeaponCategory.HeavyFlail,WeaponCategory.Scythe,WeaponCategory.Kama,WeaponCategory.Fauchard,WeaponCategory.HookedHammer};
  internal static readonly HashSet<WeaponCategory> Disarm=new HashSet<WeaponCategory>{WeaponCategory.Flail,WeaponCategory.HeavyFlail,WeaponCategory.Nunchaku,WeaponCategory.Sai};
  internal static ItemEntityWeapon Source(UnitEntityData u){
   if(!WildShapeFeats.InShape(u))return null;
   // CurrentHandsEquipmentSet points at polymorph claws/bite. The melded
   // original equipment remains in HandsEquipmentSets at the native index.
   int index=u.Body.CurrentHandEquipmentSetIndex;
   if(index<0||index>=u.Body.HandsEquipmentSets.Count)return null;
   var w=u.Body.HandsEquipmentSets[index]?.PrimaryHand?.MaybeItem as ItemEntityWeapon;
   return w?.Blueprint.IsMelee==true&&!w.Blueprint.Type.IsNatural&&u.Proficiencies.Contains(w.Blueprint.Category)?w:null;
  }
  internal static IEnumerable<ItemEntityWeapon> Natural(UnitEntityData u)=>new[]{u.Body.PrimaryHand?.MaybeWeapon,u.Body.SecondaryHand?.MaybeWeapon}.Concat(u.Body.AdditionalLimbs.Select(s=>s.MaybeWeapon)).Where(w=>w?.Blueprint.Type.IsNatural==true).Distinct();
  internal static bool Transfer(BlueprintItemEnchantment e,bool improved,bool greater){
   if(e==null)return false;
   if(Materials.Contains(e.AssetGuid.ToString()))return true;
   if(!(improved||greater)||!(e is BlueprintWeaponEnchantment))return false;
   // Inspect actual instance enchantments, not a fixed native GUID list.
   // These properties require a manufactured/ranged/thrown weapon and are
   // explicitly excluded by Weapon Shift. Other weapon properties, including
   // third-party weapon-enchantment components, use the native item engine.
   return !new[]{"Dancing","Returning","Throwing","Distance","Seeking","EndlessAmmunition"}.Any(x=>
    e.name?.IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0||e.ComponentsArray.Any(c=>c.GetType().Name.IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0));
  }
  void Clear(){foreach(var e in Data.Applied.ToArray())e?.Owner?.RemoveEnchantment(e);Data.Applied.Clear();Data.Signature=null;}
  bool refreshing;
  internal void Refresh(){
   if(refreshing)return;
   try{refreshing=true;RefreshCore();}finally{refreshing=false;}
  }
  void RefreshCore(){
   var source=Source(Owner);if(source==null){Clear();return;}
   bool improved=WildShapeFeats.Has(Owner,WildShapeFeats.Tokens[3]),greater=WildShapeFeats.Has(Owner,WildShapeFeats.Tokens[4]);
   bool merged=improved||greater;
   var eligible=source.Enchantments.Where(e=>Transfer(e.Blueprint,improved,greater));
   var enchants=(merged?eligible:eligible.GroupBy(e=>e.Blueprint).Select(g=>g.Last())).ToArray();var weapons=Natural(Owner).ToArray();
   string signature=source.UniqueId+"/"+string.Join(",",enchants.Select(e=>e.Blueprint.AssetGuid+":"+e.UniqueId).OrderBy(x=>x))+"/"+string.Join(",",weapons.Select(w=>w.UniqueId));
   if(signature==Data.Signature&&(!merged||Data.Applied.Count==weapons.Length*enchants.Length)&&Data.Applied.All(e=>e!=null&&e.Owner!=null&&e.Owner.Enchantments.Contains(e))&&weapons.All(w=>enchants.All(src=>w.Enchantments.Any(e=>e.Blueprint==src.Blueprint))))return;
   Clear();foreach(var w in weapons)foreach(var src in enchants)if(merged||!w.Enchantments.Any(e=>e.Blueprint==src.Blueprint)){
    // Keep the source spell's context/ranks (e.g. caster-level enhancement),
    // while the native enchantment's Owner is the natural weapon instance.
    var added=w.AddEnchantment(src.Blueprint,src.ParentContext??Context);added.RemoveOnUnequipItem=true;Data.Applied.Add(added);
   }Data.Signature=signature;
  }
  protected override void OnTurnOn()=>Refresh();
  protected override void OnTurnOff()=>Clear();
  protected override void OnApplyPostLoadFixes(){Data.Signature=null;Refresh();}
  public void OnPolymorphActivated(UnitEntityData unit,Polymorph polymorph){if(unit==Owner)Refresh();}
  public void OnPolymorphDeactivated(UnitEntityData unit,Polymorph polymorph){if(unit==Owner)Clear();}
  public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt){Refresh();}
  public void OnEventDidTrigger(RuleCalculateWeaponStats evt){
   var source=Source(Owner);if(source==null||evt.Weapon?.Blueprint.Type.IsNatural!=true)return;
   var physical=evt.DamageDescription.FirstOrDefault(d=>d.TypeDescription?.Type==DamageType.Physical);
   if(physical!=null&&source.Blueprint.DamageType.Type==DamageType.Physical){
    var copy=CopyData(physical.TypeDescription);copy.Physical.Form=source.Blueprint.DamageType.Physical.Form;physical.TypeDescription=copy;
   }
  }
  public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)=>Refresh();
  public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt){}
  public void OnEventAboutToTrigger(RuleCalculateCMB evt){
   var source=Source(Owner);if(source!=null&&(evt.Type==CombatManeuver.Trip&&Trip.Contains(source.Blueprint.Category)||evt.Type==CombatManeuver.Disarm&&Disarm.Contains(source.Blueprint.Category)))evt.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable);
  }
  public void OnEventDidTrigger(RuleCalculateCMB evt){}
 }
}
