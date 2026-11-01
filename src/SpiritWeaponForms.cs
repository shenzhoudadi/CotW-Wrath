using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class SpiritWeaponForms {
  internal static readonly Dictionary<WeaponCategory,string> NativeItems=new Dictionary<WeaponCategory,string>{
   {WeaponCategory.Kama,"46e685e26290d2c468d96439198e6896"},
   {WeaponCategory.Nunchaku,"c0ce1d36d1d3ae246a7587bb17296f07"},
   {WeaponCategory.Sai,"d60d0d4c78570ef408a0402f9d4313a6"},
   {WeaponCategory.BastardSword,"07531989333442348b7d0102b24af236"},
   {WeaponCategory.DuelingSword,"4697667a33a6774489e5265a955675a5"},
   {WeaponCategory.DwarvenWaraxe,"30711e40771796340ac67172abfd3279"},
   {WeaponCategory.Estoc,"4c863a47d69b63647b3b16bbb01d5ba8"},
   {WeaponCategory.Falcata,"ee28827188e12a64ba75222b37ac8092"},
   {WeaponCategory.Tongi,"b47455bac4f039747ad5e4ddc4e981a4"},
   {WeaponCategory.SlingStaff,"dda1a4f8cbf8ad34ca7845ca17313e86"},
   {WeaponCategory.DoubleAxe,"1d9acaa3c344c1244bcea18095652955"},
   {WeaponCategory.DoubleSword,"b665770f14e49bc49999d7c3c11c1d61"},
   {WeaponCategory.Urgrosh,"c20f347c84b6605479c9a7b28ccb236b"},
   {WeaponCategory.ElvenCurvedBlade,"f58e421cb8b4ed64ba195123df754055"},
   {WeaponCategory.Fauchard,"c91f331cdf7e331468490323a2e1613d"},
   {WeaponCategory.HookedHammer,"8998da2cfe0884f47943bd28823c3a51"},
   {WeaponCategory.Handaxe,"238ac092fad27144c9514f82917fbec9"},
   {WeaponCategory.Kukri,"3125ac6c819db9f4697312710699b637"},
   {WeaponCategory.LightHammer,"79e044277b90a05448a71ae3bcaf581a"},
   {WeaponCategory.LightPick,"f20e85bd5bb8dc74785afc129284bcda"},
   {WeaponCategory.Shortsword,"f717b39c351b8b44388c471d4d272f4e"},
   {WeaponCategory.SpikedLightShield,"b12650bdb547d7e499cdc29e913088cb"},
   {WeaponCategory.Starknife,"d19662d357d752447a801951b7bec798"},
   {WeaponCategory.ThrowingAxe,"b9ed902d07b622b4f8ec223808f754c1"},
   {WeaponCategory.WeaponLightShield,"62c90581f9892e9468f0d8229c7321c4"},
   {WeaponCategory.Battleaxe,"6080738f7e97b5646980a0efad2da676"},
   {WeaponCategory.Flail,"8bdfa4f81bbc7b540919d770095720be"},
   {WeaponCategory.HeavyPick,"e7d3a58b4eb3d7e419ab4cc40f283a32"},
   {WeaponCategory.Longsword,"6fd0a849531617844b195f452661b2cd"},
   {WeaponCategory.Rapier,"1546a05eb151d424eb9132832d5511bb"},
   {WeaponCategory.Scimitar,"5363519e36752d84698e03a86fb33afb"},
   {WeaponCategory.SpikedHeavyShield,"7c8f6712c444cf446a4bd3b8b717cb5c"},
   {WeaponCategory.Trident,"231f325de2b32dd4585707f8d0c87af3"},
   {WeaponCategory.Warhammer,"3f35d5c01e11d564daa59938dec3db4b"},
   {WeaponCategory.WeaponHeavyShield,"ff8047f887565284e93773b4a698c393"},
   {WeaponCategory.Longbow,"201f6150321e09048bd59e9b7f558cb0"},
   {WeaponCategory.Shortbow,"6d40d84e239bdf345b349ff52e3c00a9"},
   {WeaponCategory.Bardiche,"5bfdaaa6416cc604cba121d003db11ef"},
   {WeaponCategory.EarthBreaker,"fc47ddc975f1f804bbef320e1c574cd7"},
   {WeaponCategory.Falchion,"0f0e6834458d01049a408cd304053b1b"},
   {WeaponCategory.Glaive,"f83415c0e7ea1994d8a7f3dec8f5a861"},
   {WeaponCategory.Greataxe,"6efea466862f014469cec6c3f2b85cb7"},
   {WeaponCategory.Greatsword,"2fff2921851568a4d80ed52f76cccdb6"},
   {WeaponCategory.HeavyFlail,"7f7c8e1e4fdd99e438b30ed9622e9e3f"},
   {WeaponCategory.Scythe,"1052a1f7128861942aa0c2ee6078531e"},
   {WeaponCategory.UnarmedStrike,"e5ea5042194712040a3b2944e6944d10"},
   {WeaponCategory.Dagger,"aa514dbf4c3d61f4e9c0738bd4d373cb"},
   {WeaponCategory.LightMace,"355659b342d28e641bf29fe6d8084d19"},
   {WeaponCategory.PunchingDagger,"43ff56218554d8547840e7659816db5e"},
   {WeaponCategory.Sickle,"bfe24b51e4d943a42b0976aaee7e1b7c"},
   {WeaponCategory.Club,"07863393521453a4da9e98626531eb5f"},
   {WeaponCategory.HeavyMace,"766689ccb57d9eb4f8fc88e2e85a919f"},
   {WeaponCategory.Shortspear,"926d02c8af0352b46874791d4de9764f"},
   {WeaponCategory.Dart,"a20308f698383b744bb459847333d64b"},
   {WeaponCategory.HeavyCrossbow,"19a5092244dcf99478dcd73c974828b1"},
   {WeaponCategory.Javelin,"c73e20a04b9cae74ca0d5affffeebd34"},
   {WeaponCategory.LightCrossbow,"511c97c1ea111444aa186b1a58496664"},
   {WeaponCategory.Greatclub,"c926ffbdccc4d124c8e8dedfe2e6f499"},
   {WeaponCategory.Longspear,"f28f6031c2908d84d945865a80f67177"},
   {WeaponCategory.Quarterstaff,"ada85dae8d12eda4bbe6747bb8b5883c"},
   {WeaponCategory.Spear,"4abc27631e2894f4b8b70270e31694f1"},
  };
  internal static string ItemToken(int kind,WeaponCategory category)=>PsychicSpiritWeapons.Tokens[kind]+(category==(kind==3?WeaponCategory.Dagger:WeaponCategory.Longsword)?"Item":category+"Item");
  internal static BlueprintItemWeapon Build(PsychicAssets f,int kind,WeaponCategory category,string title,string description) {
   string token=ItemToken(kind,category);var native=f.Native<BlueprintItemWeapon>(NativeItems[category]);
   var type=f.Clone(native.Type,token+"Type",title,description);
   Set(type,"m_DamageType",new DamageTypeDescription{Type=DamageType.Force});
   Set(type,"m_VisualParameters",CopyData(Get(native.Type,"m_VisualParameters")));
   Set(type,"m_Enchantments",Array.Empty<BlueprintWeaponEnchantmentReference>());
   var weapon=f.Clone(native,token,title,description);Set(weapon,"m_Type",Ref<BlueprintWeaponTypeReference>(type));
   Set(weapon,"m_VisualParameters",CopyData(Get(native,"m_VisualParameters")));
   Set(weapon,"m_Enchantments",Array.Empty<BlueprintWeaponEnchantmentReference>());Set(weapon,"m_CachedEnchantments",null);
   Set(weapon,"m_OverrideDamageType",true);Set(weapon,"m_DamageType",new DamageTypeDescription{Type=DamageType.Force});
   Set(weapon,"m_OverrideDamageDice",true);Set(weapon,"m_DamageDice",kind==2?new DiceFormula(4,DiceType.D6):new DiceFormula(1,kind==3?DiceType.D4:DiceType.D8));
   Set(weapon,"m_Cost",0);Set(weapon,"m_Weight",0f);
   // Double weapons also own their second end; never expose a native item as
   // part of a spell's equipment. Both ends keep native critical properties.
   if(native.SecondWeapon!=null){
    var original=native.SecondWeapon;var secondType=f.Clone(original.Type,token+"SecondType",title,description);Set(secondType,"m_DamageType",new DamageTypeDescription{Type=DamageType.Force});Set(secondType,"m_VisualParameters",CopyData(Get(original.Type,"m_VisualParameters")));Set(secondType,"m_Enchantments",Array.Empty<BlueprintWeaponEnchantmentReference>());
    var second=f.Clone(original,token+"Second",title,description);Set(second,"m_Type",Ref<BlueprintWeaponTypeReference>(secondType));Set(second,"m_VisualParameters",CopyData(Get(original,"m_VisualParameters")));Set(second,"m_Enchantments",Array.Empty<BlueprintWeaponEnchantmentReference>());Set(second,"m_CachedEnchantments",null);Set(second,"m_OverrideDamageType",true);Set(second,"m_DamageType",new DamageTypeDescription{Type=DamageType.Force});Set(second,"m_OverrideDamageDice",true);Set(second,"m_DamageDice",new DiceFormula(1,DiceType.D8));second.Double=false;Set(second,"m_SecondWeapon",new BlueprintItemWeaponReference());Set(second,"m_Cost",0);Set(second,"m_Weight",0f);Set(weapon,"m_SecondWeapon",Ref<BlueprintItemWeaponReference>(second));
   }
   return weapon;
  }
  internal static WeaponCategory? Category(UnitEntityData caster,int choice) {
   switch(choice){case 0:return WeaponCategory.Battleaxe;case 1:return WeaponCategory.Flail;case 2:return WeaponCategory.Warhammer;case 3:return WeaponCategory.Longsword;}
   foreach(var category in ChosenWeaponFeats.Weapons(ChosenWeaponFeats.Deity(caster)))if(NativeItems.ContainsKey(category))return category;
   // Irori supplies an unarmed proficiency instead of starting equipment.
   var god=ChosenWeaponFeats.Deity(caster);if(god?.AssetGuid.ToString()=="23a77a5985de08349820429ce1b5a234")return WeaponCategory.UnarmedStrike;
   return null;
  }
  internal static int Kind(BlueprintItemWeapon weapon) {
   if(weapon==null)return -1;for(int k=0;k<4;k++)if(weapon.name?.StartsWith("CotWWrathPsychic"+PsychicSpiritWeapons.Tokens[k],StringComparison.Ordinal)==true&&weapon.AssetGuid==Id(ItemToken(k,weapon.Category)))return k;return -1;
  }
  internal static bool Equip(UnitEntityData unit,BlueprintItemWeapon blueprint) {
   if(unit?.Body?.PrimaryHand==null||blueprint==null)return false;
   if(unit.Body.PrimaryHand.MaybeWeapon?.Blueprint==blueprint)return true;
   var item=new ItemEntityWeapon(blueprint);
   using(ContextData<ItemEntity.CanBeEquippedForce>.Request())
   using(ContextData<Kingmaker.Items.Slots.ItemSlot.IgnoreLock>.Request()){return unit.Body.PrimaryHand.InsertItem(item);}
  }
 }
 [TypeId("98c952373a244b59ac824eb96cb619c1")]
 public sealed class SpiritFavoredWeaponRestriction:BlueprintComponent,IAbilityCasterRestriction {
  public bool IsCasterRestrictionPassed(UnitEntityData caster)=>SpiritWeaponForms.Category(caster,4).HasValue;
  public string GetAbilityCasterRestrictionUIText()=>"Choose Chaos, Evil, Good or Law when your deity has no favored weapon.";
 }
}
