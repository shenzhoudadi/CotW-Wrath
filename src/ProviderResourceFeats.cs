// CotW feat rules; optional providers supply their existing class abilities.
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class ProviderResourceFeats {
  internal const string ModuleName="Optional resource feats";
  internal const string Synthesis="3b214434d91e468da6d9f517adef1524", SynthesisResource="c62fe8e4602d4995a4b740aa8951d545";
  internal const string TouchFeature="e6107a52292a4b3094921c58ccc6daa2", TouchParent="7f66e36c6eb24ce3bc9df4b4d7d083c0", TouchResource="811ac0978a9a4c02ab4d7c247ef66ec1", CrueltySelection="402fccae3c2147e78da4ff9f6f061461";
  internal static readonly string[] Tokens={"ExtraSpellSynthesisFeat","AbilityFocusCrueltyFeat","ChannelCorruptionFeat"};
  internal static readonly string[] TouchSources={"51260ab82925472bad218e2bec7bd06e","0182deb3a98448e3a0bf91946afa0b4c","f420d59f8a7e450a96d56959aedefb07","74a01a6843d643a1931239aef91ff96e","48474e1ba16e458a99f3899e04eeb1d8","f54a720ab7d8426686fc7f4508883647","e36af7ff13914518a3c4e1498c9ca60c","d78e7a967ba64030b57d7433245dd0f0","0374deaf7aac4726bdc8b1077beb2f09","f0ac86e4d5384271b8baa8af8d81f03a","5968e38116774f0780ac770f50b14c96","e18293b88e5c4a6b8be50044f775c35f","4351c465ce4b4eaf85b303faca46891f","07d8a7cf308445239fda67b43ee05118","236d2120673d43b5bbfa39e67c1700ac"};
  internal static bool Available(BlueprintsCache c)=>Optional<BlueprintFeature>(c,Synthesis)!=null&&Optional<BlueprintAbilityResource>(c,SynthesisResource)!=null||TouchReady(c);
  static bool TouchReady(BlueprintsCache c) {
   var parent=Optional<BlueprintAbility>(c,TouchParent);var variants=parent?.GetComponent<AbilityVariants>();
   return Optional<BlueprintFeature>(c,TouchFeature)!=null&&Optional<BlueprintAbilityResource>(c,TouchResource)!=null&&Optional<BlueprintFeature>(c,CrueltySelection)!=null&&variants!=null&&TouchSources.All(id=>variants.Variants.Any(a=>a?.AssetGuid==BlueprintGuid.Parse(id)&&a.GetComponent<AbilityEffectRunAction>()!=null));
  }
  static T Optional<T>(BlueprintsCache c,string id)where T:SimpleBlueprint=>c.Load(BlueprintGuid.Parse(id))as T;
  static PrerequisiteFeature Requires(BlueprintFeature b){var p=new PrerequisiteFeature();Set(p,"m_Feature",Ref<BlueprintFeatureReference>(b));return p;}
  internal static void Register(BlueprintsCache cache) {
   var f=new PsychicAssets(cache);var roots=new List<BlueprintFeature>();
   var synthesis=Optional<BlueprintFeature>(cache,Synthesis);var synthesisResource=Optional<BlueprintAbilityResource>(cache,SynthesisResource);
   if(synthesis!=null&&synthesisResource!=null) {
    var increase=new IncreaseResourceAmount{Value=1};Set(increase,"m_Resource",Ref<BlueprintAbilityResourceReference>(synthesisResource));
    var extra=f.Feature(Tokens[0],"Extra Spell Synthesis","You can perform one additional spell synthesis per day. You can select this feat multiple times; each rank adds one daily use. Requires Mystical Mayhem Spell Synthesis. This feat does not increase PrestigePlus Lesser Spell Synthesis. Rest to refresh available uses.",Requires(synthesis),increase);
    extra.Ranks=10;extra.ReapplyOnLevelUp=true;extra.Groups=new[]{FeatureGroup.Feat};Set(extra,"m_Icon",synthesis.Icon);roots.Add(extra);
   }
   var touch=Optional<BlueprintFeature>(cache,TouchFeature);var parent=Optional<BlueprintAbility>(cache,TouchParent);var resource=Optional<BlueprintAbilityResource>(cache,TouchResource);
   if(TouchReady(cache)) {
    var sources=parent.GetComponent<AbilityVariants>().Variants.Where(a=>a!=null&&TouchSources.Contains(a.AssetGuid.ToString())).ToArray();
    // Missing or partially initialized providers stay optional. Recheck later.
    if(sources.Length>0&&sources.All(a=>a.GetComponent<AbilityEffectRunAction>()!=null)) {
     var cruelty=Optional<BlueprintFeature>(cache,CrueltySelection);
     var crueltySources=sources.Where(a=>a.GetComponent<AbilityShowIfCasterHasFact>()!=null).ToArray();
     if(cruelty!=null&&crueltySources.Length>0) {
      var learned=crueltySources.Select(a=>a.GetComponent<AbilityShowIfCasterHasFact>().UnitFact as BlueprintFeature).Distinct().ToArray();
      if(learned.Any(a=>a==null))throw new InvalidOperationException("Unexpected MCE cruelty prerequisite structure.");
      var requiresCruelty=new PrerequisiteFeaturesFromList{Amount=1};Set(requiresCruelty,"m_Features",learned.Select(Ref<BlueprintFeatureReference>).ToArray());
      var focus=f.Feature(Tokens[1],"Ability Focus: Cruelty","The saving throw DC of your antipaladin cruelties increases by 2. Requires at least one learned cruelty. Applies to the supported MCE Touch of Corruption cruelty variants, including Channel Corruption. Other abilities are unchanged.",requiresCruelty,new CrueltyFocusDC{Abilities=crueltySources.Select(Ref<BlueprintAbilityReference>).ToArray()});
      focus.Groups=new[]{FeatureGroup.Feat};Set(focus,"m_Icon",cruelty.Icon);roots.Add(focus);
     }
     const string text="As a swift action, spend two uses of Touch of Corruption to charge your next melee weapon, unarmed or natural attack for 1 round. A hit applies the selected Touch of Corruption effect without another touch attack or resource cost. A miss expends the charge with no effect. Choose a cruelty you have learned. Only one charge can be armed; replacing or expiring it gives no refund. Requires MCE Touch of Corruption.";
     var buffs=new List<BlueprintBuff>();var abilities=new List<BlueprintAbility>();
     foreach(var source in sources) {
      string token="ChannelCorruption"+source.AssetGuid;
      var hit=new ChannelCorruptionHit{Action=CopyData(source.GetComponent<AbilityEffectRunAction>().Actions)};
      var buff=f.Buff(token+"Buff","Channel "+source.Name,text,new[]{(BlueprintComponent)hit}.Concat(source.GetComponents<ContextRankConfig>().Select(CopyData)).ToArray());
      SetEnum(buff,"m_Flags","RemoveOnRest");Set(buff,"m_Icon",source.Icon);buffs.Add(buff);
      var cost=new AbilityResourceLogic{Amount=2,ResourceCostIncreasingFacts=new List<BlueprintUnitFactReference>(),ResourceCostDecreasingFacts=new List<BlueprintUnitFactReference>()};Set(cost,"m_IsSpendResource",true);Set(cost,"m_RequiredResource",Ref<BlueprintAbilityResourceReference>(resource));
      var arm=new ChannelCorruptionArm{Source=Ref<BlueprintAbilityReference>(source),Charge=Ref<BlueprintBuffReference>(buff)};
      var checks=source.ComponentsArray.Where(c=>c is AbilityCasterHasFacts||c is AbilityShowIfCasterHasFact||c is AbilityCasterAlignment).Select(CopyData);
      var ability=f.Ability(token+"Ability","Channel "+source.Name,text,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,new BlueprintComponent[]{arm,cost,new AbilityCasterMainWeaponIsMelee()}.Concat(checks).ToArray());
      Set(ability,"m_Icon",source.Icon);ability.NotOffensive=true;abilities.Add(ability);
     }
     foreach(var arm in abilities.Select(a=>a.GetComponent<ChannelCorruptionArm>()))arm.AllCharges=buffs.Select(Ref<BlueprintBuffReference>).ToArray();
     var variants=new AbilityVariants();Set(variants,"m_Variants",abilities.Select(Ref<BlueprintAbilityReference>).ToArray());
     var menu=f.Ability("ChannelCorruptionAbility","Channel Corruption",text,AbilityType.Supernatural,UnitCommand.CommandType.Swift,AbilityRange.Personal,variants);menu.NotOffensive=true;
     foreach(var a in abilities)Set(a,"m_Parent",Ref<BlueprintAbilityReference>(menu));
     Set(menu,"m_Icon",PsychicOriginalIcons.Load("ChannelCorruption"));
     var channel=f.Feature(Tokens[2],"Channel Corruption",text,Requires(touch),Facts(menu),new SpellFeatCleanup{Buffs=buffs.Select(Ref<BlueprintBuffReference>).ToArray()});channel.Groups=new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat};Set(channel,"m_Icon",menu.Icon);roots.Add(channel);
    }
   }
   if(roots.Count>0){SpellResourceFeats.Commit(cache,f,roots.ToArray());CombatFeatPoolRepair.Reconcile(cache);}
  }
 }
 [TypeId("32a7a774161b46e58d0c8024c5d8dc16")]
 public sealed class CrueltyFocusDC:UnitFactComponentDelegate,IInitiatorRulebookHandler<RuleCalculateAbilityParams>,IInitiatorRulebookSubscriber {
  public BlueprintAbilityReference[] Abilities=Array.Empty<BlueprintAbilityReference>();
  public void OnEventAboutToTrigger(RuleCalculateAbilityParams e){if(Fact.Active&&e.Spell!=null&&Abilities.Any(a=>a?.Guid==e.Spell.AssetGuid))e.AddBonusDC(2,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleCalculateAbilityParams e){}
 }
 [TypeId("e4ee0896d61d48c9b06dfcfd93b0a4ea")]
 public sealed class ChannelCorruptionArm:AbilityApplyEffect {
  public BlueprintAbilityReference Source;
  public BlueprintBuffReference Charge;
  public BlueprintBuffReference[] AllCharges=Array.Empty<BlueprintBuffReference>();
  public override void Apply(AbilityExecutionContext context,TargetWrapper target) {
   var owner=context.MaybeCaster;var source=Source?.Get();var charge=Charge?.Get();if(owner==null||source==null||charge==null)return;
   // Calculate the existing provider ability's parameters, without casting it
   // or spending its resource again. This also includes Ability Focus once.
   var data=new AbilityData(source,owner.Descriptor);
   var sourceContext=data.CreateExecutionContext(owner,data.CalculateParams());
   SpellResourceFeats.Clear(owner,AllCharges.Select(b=>b.Guid));
   var buff=owner.Buffs.AddBuff(charge,sourceContext,TimeSpan.FromSeconds(6));if(buff!=null)buff.IsNotDispelable=true;
  }
 }
 public sealed class ChannelCorruptionState {[JsonProperty]public bool Used;}
 [TypeId("8cc9f64fb7554311905e729c82bfe9ab")]
 public sealed class ChannelCorruptionHit:UnitBuffComponentDelegate<ChannelCorruptionState>,IInitiatorRulebookHandler<RuleAttackWithWeapon>,IInitiatorRulebookSubscriber {
  public ActionList Action=new ActionList{Actions=Array.Empty<GameAction>()};
  public void OnEventAboutToTrigger(RuleAttackWithWeapon e){}
  public void OnEventDidTrigger(RuleAttackWithWeapon e) {
   if(!Buff.Active||Data.Used||e.Initiator!=Owner||e.AttackRoll==null||e.AttackRoll.IsFake||e.Weapon?.Blueprint.IsMelee!=true||e.Weapon.Blueprint.Category==WeaponCategory.Touch||e.Weapon.Blueprint.Category==WeaponCategory.Ray)return;
   Data.Used=true;
   try{if(e.AttackRoll.IsHit&&e.Target!=null)Buff.RunActionInContext(Action,e.Target);}finally{Buff.Remove();}
  }
 }
}
