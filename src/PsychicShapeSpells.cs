using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Components;
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.ResourceLinks;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicShapeSpells {
  internal static readonly string[] Tokens={"UndeadAnatomy1","UndeadAnatomy2","UndeadAnatomy3","GiantForm1","GiantForm2"};
  internal static readonly string[] Forms={"UndeadAnatomy1","UndeadAnatomy2","UndeadAnatomy3","GiantForm1Troll","GiantForm1Fire","GiantForm1Frost","GiantForm2Storm","GiantForm2Athach"};
  static AddStatBonus Stat(StatType stat,int value,ModifierDescriptor descriptor=ModifierDescriptor.Size)=>new AddStatBonus {Stat=stat,Value=value,Descriptor=descriptor};
  static BlueprintComponent Hand(PsychicAssets f,string id){var c=new EmptyHandWeaponOverride();Set(c,"m_Weapon",Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>(id)));return c;}
  static BlueprintComponent Limb(PsychicAssets f,string id){var c=new AddAdditionalLimb();Set(c,"m_Weapon",Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>(id)));return c;}
  static BlueprintComponent[] Body(Size size,int strength,int dex,int con,int armor){var c=new ChangeUnitSize {Size=size};SetEnum(c,"m_Type","Value");return new BlueprintComponent[]{c,Stat(StatType.Strength,strength),Stat(StatType.Dexterity,dex),Stat(StatType.Constitution,con),Stat(StatType.AC,armor,ModifierDescriptor.NaturalArmor),new SpellDescriptorComponent {Descriptor=SpellDescriptor.Polymorph}};}
  static BlueprintAbility Form(PsychicAssets f,string token,string title,string text,UnityEngine.Sprite icon,params BlueprintComponent[] components){
   text+=" Lasts 1 minute per caster level. As in Call of the Wild, this changes size and combat traits while retaining equipment, spellcasting and the character model. Empty hands use the form's natural weapons. Replaces other polymorph effects; Return to Normal ends this form.";
   var b=f.Buff(token+"Buff",title,text,components.Concat(new[]{(BlueprintComponent)new PsychicShapeReplacement()}).ToArray());Set(b,"m_Icon",icon);
   var remove=new ContextActionRemoveBuff();Set(remove,"m_Buff",Ref<BlueprintBuffReference>(b));var back=f.Ability(token+"Return","Return to Normal", "End "+title+".",AbilityType.Supernatural,UnitCommand.CommandType.Standard,AbilityRange.Personal,Run(remove));Set(back,"m_Icon",icon);back.CanTargetSelf=true;back.LocalizedDuration=Text(token+"Return","duration","Instantaneous");back.LocalizedSavingThrow=Text(token+"Return","savingThrow","");
   b.ComponentsArray=b.ComponentsArray.Concat(new[]{(BlueprintComponent)Facts(back)}).ToArray();
   var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");var apply=Apply(b,0,DurationRate.Minutes);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
   var excluded=new[]{Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("734a29b693e9ec346ba2951b27987e33")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("fd389783027d63343b4a5634bd81645f")),Ref<BlueprintUnitFactReference>(f.Native<BlueprintFeature>("198fd8924dabcb5478d0f78bd453c586"))};var check=new AbilityTargetHasFact {Inverted=true};Set(check,"m_CheckedFacts",excluded);
   var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {School=SpellSchool.Transmutation},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Polymorph},rank,check,Run(new ContextActionRemoveBuffsByDescriptor {SpellDescriptor=SpellDescriptor.Polymorph},apply),new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId="6035a889bae45f242908569a7bc25c93"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget});a.CanTargetSelf=true;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;Set(a,"m_Icon",icon);a.LocalizedDuration=Text(token,"duration","1 minute per caster level");a.LocalizedSavingThrow=Text(token,"savingThrow","");return a;
  }
  static void Root(PsychicAssets f,string token,string title,BlueprintAbility[] children){var root=f.Clone(children[0],token,title,"Choose a form. Each form retains equipment and spellcasting; see the individual form for its traits.");var variants=new AbilityVariants();Set(variants,"m_Variants",children.Select(Ref<BlueprintAbilityReference>).ToArray());root.ComponentsArray=new BlueprintComponent[]{new SpellComponent {School=SpellSchool.Transmutation},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Polymorph},variants};foreach(var a in children)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(root));}
  internal static void Build(PsychicAssets f){
   var undeadIcon=f.Native<BlueprintProgression>("a1a8bf61cadaa4143b2d4966f2d1142e").Icon;var giantIcon=f.Native<BlueprintFeature>("2a6a2f8e492ab174eb3f01acf5b7c90a").Icon;
   for(int i=1;i<=3;i++){var parts=Body(i==1?Size.Medium:i==2?Size.Large:Size.Huge,i*2,-(i-1)*2,0,i*2).ToList();parts.Add(Hand(f,"767e6932882a99c4b8ca95c88d823137"));parts.Add(Limb(f,"a000716f88c969c499a535dadcf09286"));parts.Add(Facts(f.Native<BlueprintFeature>("d5ee498e19722854198439629c1841a5")));
    if(i>=2){parts.Add(new SavingThrowBonusAgainstDescriptor {SpellDescriptor=SpellDescriptor.MindAffecting|SpellDescriptor.Disease|SpellDescriptor.Poison|SpellDescriptor.Sleep|SpellDescriptor.Paralysis|SpellDescriptor.Stun,ModifierDescriptor=ModifierDescriptor.UntypedStackable,Value=(i-1)*4,Bonus=0});parts.Add(new AddDamageResistancePhysical {Value=5,BypassedByForm=i==2,Form=PhysicalDamageForm.Bludgeoning});}
    Form(f,"UndeadAnatomy"+i,"Undead Anatomy "+new[]{"I","II","III"}[i-1],"Assume undead combat traits: "+(i==1?"Medium, +2 Strength and +2 natural armor.":i==2?"Large, +4 Strength, -2 Dexterity, +4 natural armor, DR5/bludgeoning and +4 against mind-affecting, disease, poison, sleep, paralysis and stun.":"Huge, +6 Strength, -4 Dexterity, +6 natural armor, DR5/- and +8 against mind-affecting, disease, poison, sleep, paralysis and stun.")+" Gain a bite and empty-hand slams. Native negative energy affinity reverses cure/inflict and channel healing and protects against negative energy. Other undead-type immunities and targeting are not granted.",undeadIcon,parts.ToArray());
   }
   var base1=Body(Size.Large,6,-2,4,4);var base2=Body(Size.Huge,8,-2,6,6).Concat(new[]{(BlueprintComponent)Stat(StatType.Speed,10,ModifierDescriptor.Enhancement)}).ToArray();
   var troll=Form(f,"GiantForm1Troll","Giant Form I: Troll","Large; +6 Strength, +4 Constitution, -2 Dexterity, +4 natural armor. Empty-hand claws, bite, regeneration5 interrupted by acid/fire, and rend2d6.",giantIcon,base1.Concat(new[]{Hand(f,"de21b6c00e6adaa409a6e7c2ae9f87f4"),Limb(f,"1f8a2e1e5e078014baebc90c2c46796f"),Facts(f.Native<BlueprintFeature>("1f037cb6eb8b35a468a41d4abbc3cac5")),new AddEffectRegeneration {Heal=5,CancelDamageEnergyTypes=new[]{DamageEnergyType.Acid,DamageEnergyType.Fire},CancelDamageAlignmentTypes=Array.Empty<DamageAlignment>(),CancelDamageMaterials=Array.Empty<PhysicalDamageMaterial>()}}).ToArray());
   var fire=Form(f,"GiantForm1Fire","Giant Form I: Fire Giant","Large; +6 Strength, +4 Constitution, -2 Dexterity, +4 natural armor. Empty-hand slams, fire resistance20 and cold vulnerability.",giantIcon,Body(Size.Large,6,-2,4,4).Concat(new[]{Hand(f,"767e6932882a99c4b8ca95c88d823137"),new AddDamageResistanceEnergy {Value=20,Type=DamageEnergyType.Fire},new AddEnergyVulnerability {Type=DamageEnergyType.Cold}}).ToArray());
   var frost=Form(f,"GiantForm1Frost","Giant Form I: Frost Giant","Large; +6 Strength, +4 Constitution, -2 Dexterity, +4 natural armor. Empty-hand slams, cold resistance20 and fire vulnerability.",giantIcon,Body(Size.Large,6,-2,4,4).Concat(new[]{Hand(f,"767e6932882a99c4b8ca95c88d823137"),new AddDamageResistanceEnergy {Value=20,Type=DamageEnergyType.Cold},new AddEnergyVulnerability {Type=DamageEnergyType.Fire}}).ToArray());Root(f,"GiantForm1","Giant Form I",new[]{troll,fire,frost});
   var storm=Form(f,"GiantForm2Storm","Giant Form II: Storm Giant","Huge; +8 Strength, +6 Constitution, -2 Dexterity, +6 natural armor, +10ft speed. Empty-hand slams and electricity resistance20.",giantIcon,base2.Concat(new[]{Hand(f,"767e6932882a99c4b8ca95c88d823137"),new AddDamageResistanceEnergy {Value=20,Type=DamageEnergyType.Electricity}}).ToArray());
   var secondary=new AddSecondaryAttacks();Set(secondary,"m_Weapon",new[]{Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>("f8c9fc15f5966f74298cfb2b9bde0986"))});
   var athach=Form(f,"GiantForm2Athach","Giant Form II: Athach","Huge; +8 Strength, +6 Constitution, -2 Dexterity, +6 natural armor, +10ft speed. Empty-hand slams, bite, secondary claw and native Athach poison on bite.",giantIcon,Body(Size.Huge,8,-2,6,6).Concat(new BlueprintComponent[]{Stat(StatType.Speed,10,ModifierDescriptor.Enhancement),Hand(f,"340ab07dff974a841b76f22ba4c3cf84"),Limb(f,"d2f99947db522e24293a7ec4eded453f"),secondary,Facts(f.Native<BlueprintFeature>("366f54decfc4c08438fa66427cd92939"))}).ToArray());Root(f,"GiantForm2","Giant Form II",new[]{storm,athach});
  }
 }
}

namespace CallOfTheWild.Wrath.Prototype {
 [TypeId("7b012be984f344179c94d788ae1b8e02")]
 public sealed class PsychicShapeReplacement:Kingmaker.UnitLogic.Buffs.Components.UnitBuffComponentDelegate,Kingmaker.PubSubSystem.IPolymorphActivatedHandler,Kingmaker.PubSubSystem.IGlobalSubscriber {
  public void OnPolymorphActivated(Kingmaker.EntitySystem.Entities.UnitEntityData unit,Kingmaker.UnitLogic.Buffs.Polymorph polymorph){if(unit==Owner)Buff.Remove();}
 }
}
