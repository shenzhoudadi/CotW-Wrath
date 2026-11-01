using Kingmaker.UnitLogic.Abilities.Components.Base;
using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicMagicalShape {
  static BlueprintItemWeaponReference Weapon(PsychicAssets f,string id)=>Ref<BlueprintItemWeaponReference>(f.Native<BlueprintItemWeapon>(id));
  internal static void Build(PsychicAssets f){
   var donor=f.Native<BlueprintBuff>("0c0afabcfddeecc40a1545a282f2bec8");var icon=f.Native<BlueprintAbility>("9b93040dad242eb43ac7de6bb6547030").Icon;var children=new BlueprintAbility[2];
   for(int i=0;i<2;i++){
    bool hydra=i==1;string token="MagicalBeastShape"+(hydra?"Hydra":"Bulette"),title="Magical Beast Shape: "+(hydra?"Hydra":"Bulette");
    string text="Become a Huge "+(hydra?"hydra":"bulette")+" for 1 minute per caster level: +8 Strength, +2 Constitution, -4 Dexterity and +7 natural armor. "+(hydra?"Five bite attacks, pounce, trip immunity and fast healing5.":"One bite and two claws, pounce, +4 CMD against trip, +10ft speed and 60ft blindsight (the CotW tremorsense adaptation).")+" Native polymorph rules merge equipment and replace the body and natural weapons. Return to Normal ends the form.";
    var b=f.Clone(donor,token+"Buff",title,text);Set(b,"m_Icon",icon);var poly=b.GetComponent<Polymorph>();
    // Optional native reference getters dereference their wrappers. In a cloned
    // runtime donor these can be absent; preserve valid donor values, fill only nulls.
    if(Get(poly,"m_Race")==null)Set(poly,"m_Race",new BlueprintRaceReference());
    if(Get(poly,"m_Portrait")==null)Set(poly,"m_Portrait",new BlueprintPortraitReference());
    if(Get(poly,"m_ReplaceUnitForInspection")==null)Set(poly,"m_ReplaceUnitForInspection",new BlueprintUnitReference());
    Set(poly,"m_Prefab",new UnitViewLink {AssetId=hydra?"b4b64be1ff38b2d44a48e58e9ac3a75d":"46b4f98ae7d8ddf40878914741a42129"});Set(poly,"m_PrefabFemale",new UnitViewLink {AssetId=""});poly.Size=Size.Huge;poly.StrengthBonus=8;poly.DexterityBonus=-4;poly.ConstitutionBonus=2;poly.NaturalArmor=7;
    var bite=Weapon(f,hydra?"61bc14eca5f8c1040900215000cfc218":"61fb13235c614f744ad42ff6141fab0e");var claw=Weapon(f,"75254f19ca6e1d048a88b7545bb65221");Set(poly,"m_MainHand",bite);Set(poly,"m_OffHand",hydra?bite:claw);Set(poly,"m_AdditionalLimbs",hydra?new[]{bite,bite,bite}:new[]{claw});Set(poly,"m_SecondaryAdditionalLimbs",Array.Empty<BlueprintItemWeaponReference>());
    var facts=new BlueprintUnitFact[]{f.Native<BlueprintUnitFact>("c33f2d68d93ceee488aa4004347dffca"),f.Native<BlueprintFeature>("20b57bab6bac9b04493491432bcb6868"),f.Native<BlueprintAbility>("bd09b025ee2a82f46afab922c4decca9")}.ToList();
    if(hydra){facts.Add(f.Native<BlueprintFeature>("c1b26f97b974aec469613f968439e7bb"));facts.Add(f.Native<BlueprintBuff>("37a5e51e9e3a23049a77ba70b4e7b2d2"));}else facts.Add(f.Native<BlueprintFeature>("13c87ac5985cc85498ef9d1ac8b78923"));Set(poly,"m_Facts",facts.Select(Ref<BlueprintUnitFactReference>).ToArray());
    b.ComponentsArray=new BlueprintComponent[]{poly,new SpellDescriptorComponent {Descriptor=SpellDescriptor.Polymorph}};
    if(!hydra)b.ComponentsArray=b.ComponentsArray.Concat(new BlueprintComponent[]{new AddStatBonus {Stat=Kingmaker.EntitySystem.Stats.StatType.Speed,Value=10,Descriptor=ModifierDescriptor.Enhancement},new Blindsense {Blindsight=true,Range=new Feet(60)}}).ToArray();
    var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");var apply=Apply(b,0,DurationRate.Minutes);apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
    var a=f.Ability(token,title,text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {School=SpellSchool.Transmutation},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Polymorph},rank,Run(new ContextActionRemoveBuffsByDescriptor {SpellDescriptor=SpellDescriptor.Polymorph},apply),new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId="6035a889bae45f242908569a7bc25c93"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget});Set(a,"m_Icon",icon);a.CanTargetSelf=true;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Heighten|Metamagic.Extend|Metamagic.Quicken;a.LocalizedDuration=Text(token,"duration","1 minute per caster level");a.LocalizedSavingThrow=Text(token,"savingThrow","");children[i]=a;
   }
   var root=f.Clone(children[0],"MagicalBeastShape","Magical Beast Shape","Choose a Huge bulette or hydra form. Native polymorph equipment and casting restrictions apply; see each form for its traits.");var variants=new AbilityVariants();Set(variants,"m_Variants",children.Select(Ref<BlueprintAbilityReference>).ToArray());root.ComponentsArray=new BlueprintComponent[]{new SpellComponent {School=SpellSchool.Transmutation},new SpellDescriptorComponent {Descriptor=SpellDescriptor.Polymorph},variants};root.LocalizedDuration=Text("MagicalBeastShape","duration","1 minute per caster level");foreach(var a in children)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(root));
  }
 }
}
