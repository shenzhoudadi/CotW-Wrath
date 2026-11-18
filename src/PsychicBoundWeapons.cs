using System;
using Kingmaker.UnitLogic.Buffs.Components;
using System.Linq;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicBoundWeapons {
  internal static readonly string[] Tokens={"EmblemOfGreed","SpiritBoundBlade"};
  internal static void Build(PsychicAssets f){Greed(f);Blade(f);foreach(var a in f.Assets.Where(x=>Tokens.Concat(PsychicStorageSpells.Tokens).Any(t=>x.Key.StartsWith(t))))PsychicRepairComponents.UniqueNames(a.Value);}
  static BlueprintAbility Spell(PsychicAssets f,string token,string name,string text,BlueprintBuff buff,bool personal){var apply=Apply(buff,0,DurationRate.Minutes);apply.DurationValue.BonusValue=new ContextValue{ValueType=ContextValueType.Rank};var a=PsychicLinkedWard.Spell(f,token,name,text,SpellSchool.Transmutation,f.Native<BlueprintAbility>("e84fc922ccf952943b5240293669b171"),Run(apply));a.Range=personal?AbilityRange.Personal:AbilityRange.Touch;a.CanTargetSelf=true;a.CanTargetFriends=!personal;a.CanTargetEnemies=false;a.LocalizedDuration=Text(token,"duration","1 minute per caster level");Set(a,"m_Icon",buff.Icon);return a;}
  static void Greed(PsychicAssets f){
   const string text="Create a +1 flaming glaive for 1 minute per caster level. Your base attack bonus becomes your caster level while wielding it. At caster level 14 it can store one harmful touch spell of 3rd level or lower and release it on your next successful melee hit. At caster level 15 it becomes +2, at 17 flaming burst replaces flaming, and at 19 it becomes +3. Both hands must be empty. The weapon and its bonuses disappear on dismissal, replacement or expiration.";
   var donor=f.Native<BlueprintItemWeapon>("f83415c0e7ea1994d8a7f3dec8f5a861");var plus=new[]{f.Native<BlueprintWeaponEnchantment>("d42fc23b92c640846ac137dc26e000d4"),f.Native<BlueprintWeaponEnchantment>("eb2faccc4c9487d43b3575d7e77ff3f5"),f.Native<BlueprintWeaponEnchantment>("80bb8a737579e35498177e1e3c75899b")};var flame=f.Native<BlueprintWeaponEnchantment>("30f90becaaac51f41bf56641966c4121");var burst=f.Native<BlueprintWeaponEnchantment>("3f032a3cd54e57649a0cdad0434bf221");var items=new BlueprintItemWeapon[4];
   for(int i=0;i<4;i++){var a=f.Clone(donor,"EmblemOfGreedItem"+i,"Emblem of Greed",text);Set(a,"m_Enchantments",new[]{Ref<BlueprintWeaponEnchantmentReference>(plus[i==0?0:i==3?2:1]),Ref<BlueprintWeaponEnchantmentReference>(i>=2?burst:flame)});Set(a,"m_CachedEnchantments",null);Set(a,"m_Cost",0);Set(a,"m_Weight",0f);items[i]=a;}
   var b=f.Buff("EmblemOfGreedBuff","Emblem of Greed",text+PsychicStorageSpells.StorageHelp,new PsychicGreedWeapon{Weapons=items.Select(Ref<BlueprintItemWeaponReference>).ToArray()},new PsychicGreedBAB(),new PsychicStoredSpell{Kind=3},new AddProficiencies{WeaponProficiencies=new[]{WeaponCategory.Glaive}});Set(b,"m_Icon",f.Native<BlueprintAbility>("a26c23a887a6f154491dc2cefdad2c35").Icon);PsychicStorageSpells.Buttons(f,"EmblemOfGreed","Emblem of Greed",text+PsychicStorageSpells.StorageHelp,b,3);var spell=Spell(f,"EmblemOfGreed","Emblem of Greed",text+PsychicStorageSpells.StorageHelp,b,true);spell.ComponentsArray=spell.ComponentsArray.Concat(new[]{(BlueprintComponent)new PsychicGreedHands()}).ToArray();
  }
  static void Blade(PsychicAssets f){
   const string text="Touch a manufactured weapon in the selected hand. For 1 minute per caster level it gains ghost touch and your choice of vicious, keen, cruel, heartseeker or menacing. Identical properties do not stack. Removing or switching away from this weapon ends the spell.";
   const string menacingText="Menacing: while you are adjacent to a creature that your allies are flanking, those flanking allies gain a +2 bonus on attack rolls against it, even if you are not flanking the creature yourself.";
   var anchor=(BlueprintWeaponEnchantment)f.Assets["WeaponSpellAnchorEnchantment"];var ghost=f.Native<BlueprintWeaponEnchantment>("47857e1a5a3ec1a46adf6491b1423b4f");
   var menacingFeature=f.Feature("SpiritBoundBladeMenacingFeature","Menacing",menacingText,new PsychicMenacing());menacingFeature.HideInUI=menacingFeature.HideInCharacterSheetAndLevelUp=true;Set(menacingFeature,"m_Icon",PsychicOriginalIcons.Load("HWMenacing"));
   var add=new AddUnitFeatureEquipment();Set(add,"m_Feature",Ref<BlueprintFeatureReference>(menacingFeature));
   var menacing=f.Clone(f.Native<BlueprintWeaponEnchantment>("66e9e299c9002ea4bb65b6f300e43770"),"SpiritBoundBladeMenacingEnchantment","Menacing",menacingText);menacing.ComponentsArray=new BlueprintComponent[]{add};Set(menacing,"m_EnchantmentCost",0);Set(menacing,"m_EnchantName",Text("SpiritBoundBladeMenacingEnchantment","name","Menacing"));Set(menacing,"m_Description",Text("SpiritBoundBladeMenacingEnchantment","description",menacingText));Set(menacing,"m_Prefix",Text("SpiritBoundBladeMenacingEnchantment","prefix","Menacing"));Set(menacing,"m_Suffix",Text("SpiritBoundBladeMenacingEnchantment","suffix",""));
   var effects=new[]{f.Native<BlueprintWeaponEnchantment>("a1455a289da208144981e4b1ef92cc56"),f.Native<BlueprintWeaponEnchantment>("102a9c8c9b7a75e4fb5844e79deaf4c0"),f.Native<BlueprintWeaponEnchantment>("629c383ffb407224398bb71d1bd95d14"),f.Native<BlueprintWeaponEnchantment>("e252b26686ab66241afdf33f2adaead6"),menacing};string[] labels={"Vicious","Keen","Cruel","Heartseeker","Menacing"};var variants=new BlueprintAbility[10];
   for(int i=0;i<effects.Length;i++)for(int hand=0;hand<2;hand++){string token="SpiritBoundBlade"+labels[i]+(hand==0?"Main":"Off"),title="Spirit-Bound Blade: "+labels[i]+(hand==0?" (Main Hand)":" (Off-Hand)"),description=i==4?text+" "+menacingText:text;var b=f.Buff(token+"Buff",title,description,new PsychicFixedWeaponEnchantment{Anchor=Ref<BlueprintItemEnchantmentReference>(anchor),Effect=Ref<BlueprintItemEnchantmentReference>(ghost),OffHand=hand==1},new PsychicFixedWeaponEnchantment{Anchor=Ref<BlueprintItemEnchantmentReference>(anchor),Effect=Ref<BlueprintItemEnchantmentReference>(effects[i]),OffHand=hand==1});Set(b,"m_Icon",PsychicOriginalIcons.Load(i==0?"HWVicious":i==2?"HWCruel":i==4?"HWMenacing":"SpiritualWeapon"));var a=Spell(f,token,title,description,b,false);a.ComponentsArray=a.ComponentsArray.Concat(new[]{(BlueprintComponent)new PsychicWeaponTarget{OffHand=hand==1}}).ToArray();variants[i*2+hand]=a;}
   var root=f.Clone(variants[0],"SpiritBoundBlade","Spirit-Bound Blade",text);var choices=new AbilityVariants();Set(choices,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());root.ComponentsArray=new BlueprintComponent[]{new SpellComponent{School=SpellSchool.Transmutation},choices};foreach(var a in variants)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(root));
  }
 }
 public sealed class PsychicGreedBABData {[JsonProperty] public ModifiableValue.Modifier Bonus;}
 [TypeId("58e258308567410a9cbe98d577a3c72f")]
 public sealed class PsychicGreedBAB:UnitBuffComponentDelegate<PsychicGreedBABData> {
  protected override void OnTurnOn(){Remove();Data.Bonus=Owner.Stats.BaseAttackBonus.AddModifier((Context?.Params.CasterLevel??0)-Owner.Stats.BaseAttackBonus.ModifiedValue,Fact,ModifierDescriptor.UntypedStackable);}
  void Remove(){if(Data.Bonus!=null)Owner.Stats.BaseAttackBonus.RemoveModifier(Data.Bonus);Data.Bonus=null;}
  protected override void OnTurnOff()=>Remove();
 }
 [TypeId("ee1cd341134d4b31818f2b26175cd0b7")]
 public sealed class PsychicGreedHands:BlueprintComponent,IAbilityTargetRestriction {
  public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target)=>caster!=null&&PsychicForceSword.HandFree(caster.Body.PrimaryHand)&&PsychicForceSword.HandFree(caster.Body.SecondaryHand);
  public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Both hands must be empty and unlocked.";
 }
 // Menacing is not a native Wrath weapon enchantment. CotW's Kingmaker build
 // patched RuleCalculateAttackBonus.FlankingBonus, which Wrath does not expose;
 // add the same +2 to flanking allies through the native rule event instead.
 [TypeId("dfad8430c0924a079b3f8cac50afc0bb")]
 public sealed class PsychicMenacing:UnitFactComponentDelegate,IGlobalRulebookHandler<RuleCalculateAttackBonus>,IGlobalSubscriber {
  public void OnEventAboutToTrigger(RuleCalculateAttackBonus e){
   var attacker=e.Initiator;var target=e.Target;
   if(Owner==null||attacker==null||target==null||attacker==Owner)return;
   if(!attacker.IsAlly(Owner)||target.IsAlly(Owner))return;
   if(!target.CombatState.IsFlanked||!target.CombatState.EngagedBy.Contains(attacker))return;
   if(Owner.DistanceTo(target)>new Feet(7).Meters)return;
   e.AddModifier(2,Fact,ModifierDescriptor.UntypedStackable);
  }
  public void OnEventDidTrigger(RuleCalculateAttackBonus e){}
 }
}
