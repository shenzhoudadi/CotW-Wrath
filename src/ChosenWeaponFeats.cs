using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class ChosenWeaponFeats {
  internal const string ModuleName="Chosen weapon feats",Deities="59e7a76987fe3b547b9cce045f4db3e4",Focus="1e1f627d26ad36f43bbd26cc2bf8ac7e";
  internal static readonly string[] Tokens={"WeaponOfChosenFeat","ImprovedWeaponOfChosenFeat","GreaterWeaponOfChosenFeat"};
  internal static readonly string[] Choices={"Good","Evil","Lawful","Chaotic","ColdIronSilver"};
  internal static readonly string[] GodAlignment={"882521af8012fc749930b03dc18a69de","351235ac5fc2b7e47801f63d117b656c","092714336606cfc45a37d2ab39fabfa8","8c7d778bc39fec642befc1435b00f613"};
  internal static readonly DamageAlignment[] AlignmentValues={DamageAlignment.Good,DamageAlignment.Evil,DamageAlignment.Lawful,DamageAlignment.Chaotic};
  internal static bool Has(UnitEntityData u,string token)=>u?.Facts.List.Any(f=>f.Blueprint.AssetGuid==Id(token)&&T11TrainingOptions.Active(u,f))==true;
  internal static BlueprintFeature Deity(UnitEntityData u) {
   var pool=Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(BlueprintGuid.Parse(Deities));if(pool==null||u==null)return null;
   var refs=(BlueprintFeatureReference[])Get(pool,"m_AllFeatures")??Array.Empty<BlueprintFeatureReference>();var ids=new HashSet<BlueprintGuid>(refs.Where(r=>r!=null).Select(r=>r.Guid));
   return u.Facts.List.Where(f=>T11TrainingOptions.Active(u,f)&&ids.Contains(f.Blueprint.AssetGuid)).Select(f=>f.Blueprint).OfType<BlueprintFeature>().FirstOrDefault();
  }
  internal static WeaponCategory[] Weapons(BlueprintFeature deity) => deity?.GetComponents<AddStartingEquipment>().SelectMany(c=>(c.CategoryItems??Array.Empty<WeaponCategory>()).Concat(c.BasicItems.OfType<BlueprintItemWeapon>().Select(w=>w.Category))).Distinct().ToArray()??Array.Empty<WeaponCategory>();
  internal static bool Favored(UnitEntityData u,Kingmaker.Items.ItemEntityWeapon weapon)=>weapon!=null&&Weapons(Deity(u)).Contains(weapon.Blueprint.Category);
  internal static DamageAlignment Alignments(BlueprintFeature deity) {
   if(deity!=null&&ChosenDeityAlignments.Native.TryGetValue(deity.AssetGuid.ToString(),out var native))return native;
   var facts=deity?.GetComponents<AddFacts>().SelectMany(c=>((BlueprintUnitFactReference[])Get(c,"m_Facts")??Array.Empty<BlueprintUnitFactReference>()).Where(r=>r!=null).Select(r=>r.Guid)).ToArray()??Array.Empty<BlueprintGuid>();var result=(DamageAlignment)0;
   for(int i=0;i<4;i++)if(facts.Contains(BlueprintGuid.Parse(GodAlignment[i])))result|=AlignmentValues[i];return result;
  }
  internal static bool Allowed(UnitEntityData u,int choice) {var god=Deity(u);if(god==null||Weapons(god).Length==0)return false;var align=Alignments(god);return choice==4?align==0:choice>=0&&choice<4&&(align&AlignmentValues[choice])!=0;}
  internal static int Preference(UnitEntityData u) {
   var p=u.Buffs.Enumerable.Where(b=>b.IsTurnedOn&&!b.IsSuppressed).Select(b=>b.Blueprint.GetComponent<ChosenPreference>()).FirstOrDefault(c=>c!=null&&Allowed(u,c.Choice));if(p!=null)return p.Choice;
   for(int i=0;i<5;i++)if(Allowed(u,i))return i;return -1;
  }
  static PrerequisiteFeature Need(BlueprintFeature f){var p=new PrerequisiteFeature();Set(p,"m_Feature",Ref<BlueprintFeatureReference>(f));return p;}
  internal static void Register(BlueprintsCache cache) {
   var f=new PsychicAssets(cache);f.Native<BlueprintFeatureSelection>(Deities);var focus=f.Native<BlueprintParametrizedFeature>(Focus);
   const string basic="As a swift action, bless your deity's favored weapon for 1 round. Your next attack with a favored weapon counts as magical for damage reduction and striking incorporeal creatures, and can reroll one failed concealment check. This does not grant an attack or damage bonus or ghost touch. Requires Weapon Focus in a favored category and the ability to cast 1st-level divine spells. Favored categories follow the deity's native starting weapons; deity and weapon blueprints are not modified.";
   const string improved="Weapon of the Chosen now affects all favored-weapon attacks until the start of your next turn (1 round in real time). Open the single persistent property selector to select one alignment component belonging to your deity for overcoming damage reduction. Selecting another property directly replaces the active property; only one remains active. The five choices are suboptions of one Sacred Weapon-style menu, not separate entries. A wholly neutral deity instead grants both cold iron and silver. The deity's alignment, not your personal alignment, controls availability. Choose before blessing; changing it affects the next blessing. The default is the first available choice. Your weapon retains its original damage type; other weapons and spell damage receive no benefit.";
   const string greater="With your deity's favored weapon, roll twice and take the better result on the single attack made with a standard attack action or native Vital Strike. This is passive and does not require activating Weapon of the Chosen. It does not reroll critical confirmations, full attacks, charges, opportunity attacks, bonus attacks, Cleave or attacks delivered by other spells or abilities.";
   var buff=f.Buff("WeaponOfChosenBuff","Weapon of the Chosen",basic+"\n"+improved,new ChosenWeaponBlessing());buff.Stacking=StackingType.Replace;SetEnum(buff,"m_Flags","RemoveOnRest");
   var ability=f.Ability("WeaponOfChosenAbility","Weapon of the Chosen",basic,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,new ChosenWeaponRestriction(),new ChosenWeaponArm());ability.NotOffensive=true;ability.LocalizedDuration=Text("WeaponOfChosenAbility","duration","1 round; Improved: until your next turn");
   var prefs=new List<BlueprintBuff>();for(int i=0;i<5;i++){var p=f.Buff("Chosen"+Choices[i]+"PreferenceBuff","Improved Weapon of the Chosen: "+(i==4?"Cold Iron and Silver":Choices[i]),improved,new ChosenPreference{Choice=i});p.Stacking=StackingType.Replace;SetEnum(p,"m_Flags","0");prefs.Add(p);}
   var variants=new List<BlueprintAbility>();for(int i=0;i<5;i++) {
    var actions=prefs.Select(b=>{var a=new ContextActionRemoveBuff();Set(a,"m_Buff",Ref<BlueprintBuffReference>(b));return (Kingmaker.ElementsSystem.GameAction)a;}).ToList();var apply=Apply(prefs[i],0,toCaster:true,fromSpell:false);apply.Permanent=true;apply.IsNotDispelable=true;actions.Add(apply);
    var a=f.Ability("Chosen"+Choices[i]+"PreferenceAbility","Improved Weapon of the Chosen: "+(i==4?"Cold Iron and Silver":Choices[i]),improved,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,new ChosenWeaponRestriction{Choice=i},Run(actions.ToArray()));a.NotOffensive=true;variants.Add(a);
   }
   var v=new AbilityVariants();Set(v,"m_Variants",variants.Select(Ref<BlueprintAbilityReference>).ToArray());var parent=f.Ability("ChosenPreferenceAbility","Improved Weapon of the Chosen: Choose Property",improved,AbilityType.Supernatural,UnitCommand.CommandType.Free,AbilityRange.Personal,v);parent.NotOffensive=true;foreach(var a in variants)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(parent));
   // Preserve legacy ability identities, but native AddFacts migrates the grant to
   // activatable abilities and removes its obsolete applied parent on load.
   var toggles=new List<BlueprintActivatableAbility>();for(int i=0;i<5;i++) {
    var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,DeactivateImmediately=true,Group=ActivatableAbilityGroup.None,DoNotTurnOffOnRest=true,DeactivateIfCombatEnded=false,ComponentsArray=new BlueprintComponent[]{new ChosenToggleRestriction{Choice=i}}},"Chosen"+Choices[i]+"PreferenceToggle","Improved Weapon of the Chosen: "+(i==4?"Cold Iron and Silver":Choices[i]),improved);
    Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(prefs[i]));toggles.Add(toggle);
   }
   // Native Sacred Weapon layout: one menu entry, persistent mutually exclusive
   // conversions. SlotConversion explicitly unhides children inside the menu.
   foreach(var toggle in toggles){toggle.HiddenInUI=true;toggle.ActionBarAutoFillIgnored=true;}
   var conversions=new ActivatableAbilityVariants();Set(conversions,"m_Variants",toggles.Select(Ref<BlueprintActivatableAbilityReference>).ToArray());
   var selector=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,Group=ActivatableAbilityGroup.None,ComponentsArray=new BlueprintComponent[]{conversions,new ActivationDisable()}},"ChosenPreferenceToggle","Improved Weapon of the Chosen: Choose Property",improved);
   var all=prefs.Concat(new[]{buff}).Select(Ref<BlueprintBuffReference>).ToArray();var root=f.Feature(Tokens[0],"Weapon of the Chosen",basic,new PrerequisiteChosenWeapon(),new PrerequisiteCasterTypeSpellLevel{IsArcane=false,RequiredSpellLevel=1},Facts(ability),new SpellFeatCleanup{Buffs=all});
   var better=f.Feature(Tokens[1],"Improved Weapon of the Chosen",improved,Need(root),new PrerequisiteChosenWeapon(),Facts(new[]{selector}.Concat(toggles).Cast<BlueprintUnitFact>().ToArray()),new SpellFeatCleanup{Buffs=all});
   var best=f.Feature(Tokens[2],"Greater Weapon of the Chosen",greater,Need(better),Need(root),new PrerequisiteChosenWeapon(),new GreaterChosenAttack());
   foreach(var bp in f.Assets.Values)Set(bp,"m_Icon",focus.Icon);foreach(var feat in new[]{root,better,best})feat.Groups=new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat};SpellResourceFeats.Commit(cache,f,new[]{root,better,best});
  }
  internal static void Optional(BlueprintsCache cache){var p=cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BrawlerCombatFeatSelectionId)) as BlueprintFeatureSelection;var feats=Tokens.Select(t=>cache.Load(Id(t)) as BlueprintFeature).ToArray();if(p!=null&&feats.All(f=>f?.GetComponent<PrerequisiteChosenWeapon>()!=null))T09CombatFeats.Append(new[]{p},feats);}
 }
 [TypeId("fef3848f67e541f89a03155c034ae56e")]
 public sealed class PrerequisiteChosenWeapon:Prerequisite {
  protected override bool CheckInternal(Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState selectionState,UnitDescriptor unit,Kingmaker.UnitLogic.Class.LevelUp.LevelUpState state) {
   var u=unit?.Unit;if(u==null)return false;var categories=ChosenWeaponFeats.Weapons(ChosenWeaponFeats.Deity(u));
   return categories.Length>0&&unit.Progression.Classes.Any(c=>c.CharacterClass.IsDivineCaster&&c.Spellbook!=null&&!c.Spellbook.IsArcane&&!c.Spellbook.IsMythic&&!c.Spellbook.IsAlchemist)&&unit.Progression.Features.Enumerable.Any(f=>T11TrainingOptions.Active(u,f)&&f.Blueprint.AssetGuid==BlueprintGuid.Parse(ChosenWeaponFeats.Focus)&&f.Param.WeaponCategory.HasValue&&categories.Contains(f.Param.WeaponCategory.Value));
  }
  protected override string GetUITextInternal(UnitDescriptor unit)=>"Worship a deity, receive divine spells, and have Weapon Focus in its favored weapon";
 }
 [TypeId("0ba965872efd44ac911a3c7631bd48ef")]
 public sealed class ChosenWeaponRestriction:BlueprintComponent,IAbilityRestriction {
  public int Choice=-1;
  public bool IsAbilityRestrictionPassed(AbilityData ability){var u=ability?.Caster?.Unit;return Choice<0?ChosenWeaponFeats.Has(u,ChosenWeaponFeats.Tokens[0])&&(ChosenWeaponFeats.Favored(u,u.Body?.PrimaryHand?.MaybeWeapon)||ChosenWeaponFeats.Favored(u,u.Body?.SecondaryHand?.MaybeWeapon)):ChosenWeaponFeats.Has(u,ChosenWeaponFeats.Tokens[1])&&ChosenWeaponFeats.Allowed(u,Choice);}
  public string GetAbilityRestrictionUIText()=>Choice<0?"Wield your deity's favored weapon.":"Choose one property belonging to your deity; cold iron and silver require a wholly neutral deity.";
 }
 [TypeId("eb26a2c837b544eb9f02f4da8dd3c318")]
 public sealed class ChosenPreference:UnitBuffComponentDelegate {
  public int Choice;
  protected override void OnTurnOn() {
   if(Owner.ActivatableAbilities!=null)foreach(var a in Owner.ActivatableAbilities.Enumerable.Where(a=>a.IsOn&&a.Blueprint.Buff?.GetComponent<ChosenPreference>() is ChosenPreference p&&p.Choice!=Choice).ToArray())a.IsOn=false;
   foreach(var b in Owner.Buffs.Enumerable.Where(b=>b!=Buff&&b.Blueprint.GetComponent<ChosenPreference>() is ChosenPreference p&&p.Choice!=Choice).ToArray())b.Remove();
  }
 }
 [TypeId("e6d6288a02444c58b43fb4080881fd2e")]
 public sealed class ChosenWeaponArm:AbilityApplyEffect {
  public override void Apply(AbilityExecutionContext context,TargetWrapper target){var u=context.MaybeCaster;if(u==null)return;var b=u.Buffs.AddBuff(Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(Id("WeaponOfChosenBuff")),context,TimeSpan.FromSeconds(6));if(b!=null)b.IsNotDispelable=true;}
 }
 public sealed class ChosenBlessingState {
  [JsonProperty]public bool Initialized,Improved,Consumed;
  [JsonProperty]public DamageAlignment Alignment;
  [JsonProperty]public PhysicalDamageMaterial Material;
  [JsonIgnore]public RuleAttackWithWeapon ClaimedAttack;
 }
 [TypeId("9c16c4e0c7c24840a1a955596bdad7ac")]
 public sealed class ChosenWeaponBlessing:UnitBuffComponentDelegate<ChosenBlessingState>,IInitiatorRulebookHandler<RuleCalculateWeaponStats>,IInitiatorRulebookHandler<RuleAttackWithWeapon>,IInitiatorRulebookHandler<RuleConcealmentCheck>,ITurnBasedModeHandler,IGlobalSubscriber,ISubscriber {
  bool Ready=>Fact.IsActive&&Fact.IsTurnedOn&&!Buff.IsSuppressed&&!Data.Consumed;
  bool Matches(RuleAttackWithWeapon a)=>Ready&&a?.Initiator==Owner&&!a.IsRend&&ChosenWeaponFeats.Favored(Owner,a.Weapon)&&(Data.Improved||Data.ClaimedAttack==null||ReferenceEquals(Data.ClaimedAttack,a));
  protected override void OnActivate(){if(Data.Initialized)return;Data.Initialized=true;Data.Improved=ChosenWeaponFeats.Has(Owner,ChosenWeaponFeats.Tokens[1]);Data.Consumed=false;Data.ClaimedAttack=null;Data.Alignment=0;Data.Material=0;if(!Data.Improved)return;int c=ChosenWeaponFeats.Preference(Owner);if(c>=0&&c<4)Data.Alignment=ChosenWeaponFeats.AlignmentValues[c];else if(c==4)Data.Material=PhysicalDamageMaterial.ColdIron|PhysicalDamageMaterial.Silver;}
  public void OnEventAboutToTrigger(RuleCalculateWeaponStats e){}
  public void OnEventDidTrigger(RuleCalculateWeaponStats e) {
   if(!Matches(e.AttackWithWeapon)||!ReferenceEquals(e.Weapon,e.AttackWithWeapon.Weapon))return;
   var d=e.DamageDescription.FirstOrDefault();if(d?.TypeDescription==null)return;
   // Never replace a weapon's damage type or mutate a shared damage descriptor.
   // Preserve form, reality, energy/force conversions and existing enchantments.
   var original=e.Weapon.Blueprint.DamageType;
   // A blessing of a physical favored weapon cannot turn its primary packet into
   // force. Preserve all other packet metadata and leave originally-force weapons
   // and separate elemental/enchantment packets untouched.
   if(d.TypeDescription.Type==DamageType.Force&&original.Type==DamageType.Physical) {
    d.TypeDescription=d.TypeDescription.Copy();d.TypeDescription.Type=DamageType.Physical;d.TypeDescription.Physical.Form=original.Physical.Form;
   }
   if(d.TypeDescription.Type!=DamageType.Physical)return;
   d.TypeDescription=d.TypeDescription.Copy();var p=d.TypeDescription.Physical;
   if(p.Enhancement<1){p.Enhancement=1;p.EnhancementTotal++;}d.TypeDescription.Common.Alignment|=Data.Alignment;p.Material|=Data.Material;
  }
  public void OnEventAboutToTrigger(RuleAttackWithWeapon e){if(Matches(e)&&!Data.Improved&&Data.ClaimedAttack==null)Data.ClaimedAttack=e;}
  public void OnEventDidTrigger(RuleAttackWithWeapon e){if(e.AttackRoll?.IsFake==true){if(ReferenceEquals(Data.ClaimedAttack,e))Data.ClaimedAttack=null;return;}if(Matches(e)&&!Data.Improved){Data.Consumed=true;Buff.Remove();}}
  public void OnEventAboutToTrigger(RuleConcealmentCheck e){}
  public void OnEventDidTrigger(RuleConcealmentCheck e){var a=Rulebook.CurrentContext.LastEvent<RuleAttackRoll>();if(!e.Success&&e.Initiator==Owner&&a?.Target==e.Target&&!a.IsFake&&!a.IgnoreConcealment&&Matches(a.RuleAttackWithWeapon)&&a.Weapon==a.RuleAttackWithWeapon.Weapon&&e.Roll.Rerolls?.Any(r=>ReferenceEquals(r.Source,Fact))!=true)e.Roll.Reroll(Fact,true);}
  public void HandleTurnStarted(UnitEntityData unit){if(unit==Owner)Buff.Remove();}
  public void HandleSurpriseRoundStarted(){} public void HandleRoundStarted(int round){} public void HandleUnitControlChanged(UnitEntityData unit){} public void HandleUnitNotSurprised(UnitEntityData unit,RuleSkillCheck check){}
 }
 [TypeId("e9cbe95e16b946038547348eb3bd9556")]
 public sealed class GreaterChosenAttack:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleRollD20>,ISubscriber {
  static readonly ConditionalWeakTable<RuleRollD20,object> Seen=new ConditionalWeakTable<RuleRollD20,object>();
  public void OnEventAboutToTrigger(RuleRollD20 e) {
   if(!T11TrainingOptions.Active(Owner,Fact)||e.Initiator!=Owner||e.IsFake||Seen.TryGetValue(e,out _))return;
   var roll=Rulebook.CurrentContext.PreviousEvent as RuleAttackRoll;var a=roll?.RuleAttackWithWeapon;
   if(roll==null||roll.IsCriticalRoll||roll.IsFake||a==null||a.Initiator!=Owner||roll.Weapon!=a.Weapon||!ChosenWeaponFeats.Favored(Owner,a.Weapon)||a.IsRend||a.IsFullAttack||a.IsCharge||a.IsAttackOfOpportunity||a.ExtraAttack||a.AttackNumber!=0||a.AttackBonusPenalty!=0||!FellingSmashTrigger.IsAttackAction(Owner.Commands?.Standard,a))return;
   Seen.Add(e,new object());e.AddReroll(1,true,Fact);
  }
  public void OnEventDidTrigger(RuleRollD20 e){}
 }
}
