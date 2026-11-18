using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.PubSubSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class WildShapeFeats {
  internal const string ModuleName="Wild shape feats";
  internal const string WildFeature="19bb148cb92db224abb431642d10efeb";
  internal static readonly string[] Tokens={"PlanarWildShapeFeat","PowerfulShapeFeat","WeaponShiftFeat","ImprovedWeaponShiftFeat","GreaterWeaponShiftFeat"};
  internal static readonly string[] OriginalIds={"459d575f72f74ef9b9c49e50d9d43a5b","c9b7d65478f54770afbed594c408e811","02dd5acead61414a900d9bbc120ddbc9","078135e382094a0aac1582e95aa9d572","443c99e3863f4ee9b5adf8a5b64c340f"};
  internal static readonly HashSet<BlueprintGuid> BuffIds=new HashSet<BlueprintGuid>(WildShapeCatalog.Forms.Select(f=>BlueprintGuid.Parse(f[1])));
  internal static bool InShape(UnitEntityData u)=>u?.Body?.IsPolymorphed==true&&u.Buffs.Enumerable.Any(b=>BuffIds.Contains(b.Blueprint.AssetGuid)&&b.IsActive&&b.IsTurnedOn&&!b.IsSuppressed);
  internal static bool Has(UnitEntityData u,string token){int i=Array.IndexOf(Tokens,token);return u?.Facts.List.Any(f=>(f.Blueprint.AssetGuid==Id(token)||i>=0&&f.Blueprint.AssetGuid==BlueprintGuid.Parse(OriginalIds[i]))&&T11TrainingOptions.Active(u,f))==true;}
  internal static bool Allowed(UnitEntityData u,bool celestial)=>u!=null&&!u.Alignment.ValueRaw.HasComponent(celestial?AlignmentComponent.Evil:AlignmentComponent.Good);
  static PrerequisiteFeature Need(BlueprintFeature feat){var c=new PrerequisiteFeature();Set(c,"m_Feature",Ref<BlueprintFeatureReference>(feat));return c;}
  static PrerequisiteFeaturesFromList WildNeed(PsychicAssets f){
   var p=new PrerequisiteFeaturesFromList{Amount=1};Set(p,"m_Features",new[]{WildFeature}.Concat(WildShapeCatalog.ShifterFeatures).Select(id=>Ref<BlueprintFeatureReference>(f.Native<BlueprintFeature>(id))).ToArray());return p;
  }
  static PrerequisiteClassLevel ShapeLevel(BlueprintCharacterClass c){var p=T11TrainingOptions.Level(c,8);p.Group=Prerequisite.GroupType.Any;return p;}
  internal static void Register(BlueprintsCache cache){
   var f=new PsychicAssets(cache);var wild=f.Native<BlueprintFeature>(WildFeature);var shifter=f.Native<BlueprintCharacterClass>("a406d6ebea5c46bba3160246be03e96f");var druid=f.Native<BlueprintCharacterClass>("610d836f3a3a9ed42a4349b62f002e96");
   var forms=WildShapeCatalog.Forms.Select(row=>new{Row=row,Ability=f.Native<Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility>(row[0]),Buff=f.Native<BlueprintBuff>(row[1])}).ToArray();
   foreach(var form in forms)if(form.Ability.GetComponent<AbilityResourceLogic>()?.RequiredResource?.AssetGuid.ToString()!=form.Row[3]||form.Buff.GetComponent<Polymorph>()==null)throw new InvalidOperationException("Wild shape native anchor mismatch: "+form.Row[0]);
   var foreign=OriginalIds.Select(id=>cache.Load(BlueprintGuid.Parse(id)) as BlueprintFeature).ToArray();
   var roots=new BlueprintFeature[5];
   const string planar="When using druid wild shape to take an animal form or an eligible shifter major form, you may spend one additional wild shape use to gain the celestial or fiendish template for that form. Good characters may choose only celestial; evil characters only fiendish; neutral characters may choose either. Select the persistent template before transforming. Animal forms gain the native template's spell resistance, energy resistances, damage reduction and daily smite, scaled to your own Hit Dice. Natural-weapon critical confirmation rolls gain +2 against the opposite alignment. Elemental and plant forms do not spend the extra use or gain the template. Master Shapeshifter and native unlimited-major-form features waive all wild-shape cost, including the extra use. Rageshaper Devastating Form and Fiendflesh Shifter Fiendish Aspect are excluded. Requires wild shape and Lore (Religion) 5 ranks.";
   var choices=new List<BlueprintActivatableAbility>();var choiceBuffs=new List<BlueprintBuff>();var effects=new List<BlueprintBuff>();
   if(foreign[0]==null){
    for(int i=0;i<2;i++){
     bool celestial=i==0;var label=celestial?"Celestial":"Fiendish";
     var template=f.Clone(f.Native<BlueprintFeature>(celestial?"ab3559a3cb9dcef49b8b529590e216d6":"8d24793851c6b2e4688e785f6954de68"),"Planar"+label+"Template",label+" Wild Shape",planar);
     // The native fiendish pet template uses CR for SR; a player form uses its
     // own Hit Dice. All cloned ranks use the player, not an animal's monster CR.
     foreach(var rank in template.GetComponents<ContextRankConfig>())SetEnum(rank,"m_BaseValueType","CharacterLevel");
     // Keep daily pools on the learned feat. The native pet template creates
     // them without restoring and removes them on leaving the form; a player
     // must retain spent uses across successive shapes and rests in human form.
     template.ComponentsArray=template.ComponentsArray.Where(c=>!(c is AddAbilityResources)).ToArray();
     var effect=f.Buff("Planar"+label+"Effect",label+" Wild Shape",planar,Facts(template),new PlanarCriticalConfirmation{Celestial=celestial});
     effects.Add(effect);
     var choice=f.Buff("Planar"+label+"Choice",label+" Wild Shape: Selected",planar,new PlanarChoice{Celestial=celestial});choiceBuffs.Add(choice);
     var toggle=f.Add(new BlueprintActivatableAbility{ActivationType=AbilityActivationType.Immediately,Group=ActivatableAbilityGroup.None,DeactivateImmediately=true,DoNotTurnOffOnRest=true,DeactivateIfCombatEnded=false,ComponentsArray=new BlueprintComponent[]{new PlanarChoiceRestriction{Celestial=celestial}}},"Planar"+label+"Toggle",label+" Wild Shape",planar);
     Set(toggle,"m_Buff",Ref<BlueprintBuffReference>(choice));choices.Add(toggle);
    }
    var resources=new[]{"f3f984b315f0439418ce8358bc52af7b","508cba5e167e33c439515713463bd715"}.Select(id=>{var grant=new AddAbilityResources{RestoreAmount=true};Set(grant,"m_Resource",Ref<BlueprintAbilityResourceReference>(f.Native<BlueprintAbilityResource>(id)));return grant;}).ToArray();
    roots[0]=f.Feature(Tokens[0],"Planar Wild Shape",planar,new BlueprintComponent[]{WildNeed(f),new PrerequisiteStatValue{Stat=StatType.SkillLoreReligion,Value=5},Facts(choices.Cast<BlueprintUnitFact>().ToArray()),new SpellFeatCleanup{Buffs=choiceBuffs.Concat(effects).Select(Ref<BlueprintBuffReference>).ToArray()}}.Concat(resources).ToArray());
   }else roots[0]=foreign[0];
   roots[1]=foreign[1]??f.Feature(Tokens[1],"Powerful Shape","During druid or eligible shifter wild shape, treat your size as one category larger for combat maneuver size bonuses to CMB and CMD. This does not change your model, reach, AC, attack bonus or damage dice. At the engine's maximum size no further size bonus is gained. Treat size as one category larger for native grab/trample target-size eligibility, including incoming special attacks. Grab uses the existing CMB/CMD bonuses; its native +4 bonus is not duplicated. Trample uses the native overrun/damage implementation. Size alone does not increase Strength, HD or any saving throw DC. Requires wild shape and druid or shifter level 8; Devastating Form and Fiendish Aspect are excluded.",WildNeed(f),ShapeLevel(druid),ShapeLevel(shifter),new PowerfulShapeBonus());
   roots[2]=foreign[2]??f.Feature(Tokens[2],"Weapon Shift","During any polymorph or wild shape, your natural weapons gain the physical damage form and material properties of the proficient melee weapon melded from your original primary hand. Disarm, trip and sunder weapon properties carry over, granting their +2 on the matching combat maneuver check, following the CotW weapon-property list. Damage dice, critical range and multiplier, reach, double/fragile properties and attack count remain those of your form. Magical special abilities and enhancement bonuses require the later feats. Requires wild shape.",WildNeed(f),new WeaponShiftTransfer());
   roots[3]=foreign[3]??f.Feature(Tokens[3],"Improved Weapon Shift","Weapon Shift also transfers the melded melee weapon instance's current magical special abilities and enhancement bonuses, including permanent additions and temporary spell enchantments. Changes to the original weapon synchronize with your natural weapons. Enhancement bonuses use the native stacking rules; a stronger existing non-stacking bonus remains stronger. Dancing and ranged/thrown-only properties do not transfer. Requires Weapon Shift, wild shape and base attack bonus +6.",WildNeed(f),Need(roots[2]),new PrerequisiteStatValue{Stat=StatType.BaseAttackBonus,Value=6});
   roots[4]=foreign[4]??f.Feature(Tokens[4],"Greater Weapon Shift","Legacy saved feat retained for compatibility. Its enchantment and enhancement transfer is now included in Improved Weapon Shift at BAB +6. Existing saved Greater Weapon Shift facts remain valid and grant the same merged transfer without stacking duplicate effects; this legacy feat is no longer offered for new selection.",WildNeed(f),Need(roots[2]),Need(roots[3]),new PrerequisiteStatValue{Stat=StatType.BaseAttackBonus,Value=8});
   foreach(var bp in f.Assets.Values)Set(bp,"m_Icon",wild.Icon);
   foreach(var feat in roots.Where(x=>f.Assets.Values.Contains(x)))feat.Groups=new[]{FeatureGroup.Feat};
   // Planar resource/action changes are staged and rolled back with registration.
   var oldComponents=forms.Select(x=>x.Buff.ComponentsArray).ToArray();var oldCosts=forms.Select(x=>x.Ability.GetComponent<AbilityResourceLogic>().ResourceCostIncreasingFacts.ToArray()).ToArray();
   try{
    if(foreign[0]==null)foreach(var form in forms.Where(x=>x.Row[2]!="other")){
     var logic=form.Ability.GetComponent<AbilityResourceLogic>();
     foreach(var choice in choiceBuffs.Select(x=>(BlueprintBuff)cache.Load(x.AssetGuid)??x))if(!logic.ResourceCostIncreasingFacts.Any(x=>x.Guid==choice.AssetGuid))logic.ResourceCostIncreasingFacts.Add(Ref<BlueprintUnitFactReference>(choice));
     if(form.Buff.GetComponent<PlanarShapeBinding>()==null){
      var binding=new PlanarShapeBinding{Choices=choiceBuffs.Select(x=>Ref<BlueprintBuffReference>((BlueprintBuff)cache.Load(x.AssetGuid)??x)).ToArray(),Effects=effects.Select(x=>Ref<BlueprintBuffReference>((BlueprintBuff)cache.Load(x.AssetGuid)??x)).ToArray(),name="$CotWPlanarShapeBinding"};
      form.Buff.ComponentsArray=form.Buff.ComponentsArray.Concat(new BlueprintComponent[]{binding}).ToArray();form.Buff.OnEnable();
     }
    }
    // Commit last: a failed native extension or GUID collision leaves both
    // the feat pools and native wild-shape assets at their previous state.
    SpellResourceFeats.Commit(cache,f,roots.Take(4).ToArray());
    // Retain the old blueprint for saved facts; new characters learn the merged feat.
    foreach(var pool in new[]{(Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection)cache.Load(BlueprintGuid.Parse(BlueprintRegistration.BasicFeatsId))})foreach(var field in new[]{"m_Features","m_AllFeatures"})Set(pool,field,((BlueprintFeatureReference[])Get(pool,field)).Where(r=>r.Guid!=roots[4].AssetGuid).ToArray());
   }catch{
    for(int i=0;i<forms.Length;i++){forms[i].Buff.ComponentsArray=oldComponents[i];forms[i].Ability.GetComponent<AbilityResourceLogic>().ResourceCostIncreasingFacts.Clear();forms[i].Ability.GetComponent<AbilityResourceLogic>().ResourceCostIncreasingFacts.AddRange(oldCosts[i]);}
    throw;
   }
  }
 }
 [TypeId("b53761b0d3bb4ba593055b6b0e85b4ac")]
 public sealed class PlanarChoiceRestriction:ActivatableAbilityRestriction {
  public bool Celestial;
  protected override bool IsAvailable()=>WildShapeFeats.Has(Owner,WildShapeFeats.Tokens[0])&&WildShapeFeats.Allowed(Owner,Celestial)&&!WildShapeFeats.InShape(Owner);
 }
 [TypeId("d2657d9e75044a9f80a449d32e39f502")]
 public sealed class PlanarChoice:UnitBuffComponentDelegate,IAlignmentChangeHandler,IGlobalSubscriber {
  public bool Celestial;
  protected override void OnTurnOn(){
   foreach(var a in Owner.ActivatableAbilities.Enumerable.Where(a=>a.IsOn&&a.Blueprint.Buff?.GetComponent<PlanarChoice>() is PlanarChoice c&&c.Celestial!=Celestial).ToArray())a.IsOn=false;
   foreach(var b in Owner.Buffs.Enumerable.Where(b=>b!=Buff&&b.Blueprint.GetComponent<PlanarChoice>() is PlanarChoice c&&c.Celestial!=Celestial).ToArray())b.Remove();
  }
  public void HandleAlignmentChange(UnitEntityData unit,Alignment previous,Alignment current){if(unit==Owner&&!WildShapeFeats.Allowed(unit,Celestial))Buff.Remove();}
 }
 [TypeId("5d8efaf8ea4e459aa21586e98a7fb6d3")]
 public sealed class PlanarShapeBinding:UnitBuffComponentDelegate {
  public BlueprintBuffReference[] Choices,Effects;
  protected override void OnActivate(){
   for(int i=0;i<Choices.Length;i++)if(Owner.HasFact(Choices[i].Get())&&WildShapeFeats.Has(Owner,WildShapeFeats.Tokens[0])&&WildShapeFeats.Allowed(Owner,i==0)){
    var apply=Apply(Effects[i].Get(),0,toCaster:true,fromSpell:false);apply.Permanent=true;apply.AsChild=true;apply.IsNotDispelable=true;
    using(Context.GetDataScope(Owner))apply.RunAction();break;
   }
  }
 }
 [TypeId("1e470884a34d4debad869e9c11ff9b7f")]
 public sealed class PlanarCriticalConfirmation:UnitBuffComponentDelegate,IInitiatorRulebookHandler<Kingmaker.RuleSystem.Rules.RuleAttackRoll> {
  public bool Celestial;
  public void OnEventAboutToTrigger(Kingmaker.RuleSystem.Rules.RuleAttackRoll evt){
   if(evt.Weapon?.Blueprint.Type.IsNatural==true&&evt.Target?.Alignment.ValueRaw.HasComponent(Celestial?AlignmentComponent.Evil:AlignmentComponent.Good)==true)evt.CriticalConfirmationBonus+=2;
  }
  public void OnEventDidTrigger(Kingmaker.RuleSystem.Rules.RuleAttackRoll evt){}
 }
 [TypeId("27d3824d05cf4b039d62848c972ef5a4")]
 public sealed class PowerfulShapeBonus:UnitFactComponentDelegate,IInitiatorRulebookHandler<Kingmaker.RuleSystem.Rules.RuleCalculateBaseCMB>,IInitiatorRulebookHandler<Kingmaker.RuleSystem.Rules.RuleCalculateBaseCMD> {
  internal static int SizeBonus(Size size){var next=size.Shift(1);return next.GetModifiers().CMDAndCMD-size.GetModifiers().CMDAndCMD+next.GetModifiers().AttackAndAC-size.GetModifiers().AttackAndAC;}
  public void OnEventAboutToTrigger(Kingmaker.RuleSystem.Rules.RuleCalculateBaseCMB evt){if(WildShapeFeats.InShape(Owner))evt.AddTemporaryModifier(Owner.Stats.AdditionalCMB.AddModifier(SizeBonus(Owner.State.Size),Fact,ModifierDescriptor.UntypedStackable));}
  public void OnEventDidTrigger(Kingmaker.RuleSystem.Rules.RuleCalculateBaseCMB evt){}
  public void OnEventAboutToTrigger(Kingmaker.RuleSystem.Rules.RuleCalculateBaseCMD evt){if(WildShapeFeats.InShape(Owner))evt.AddModifier(SizeBonus(Owner.State.Size),Fact,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(Kingmaker.RuleSystem.Rules.RuleCalculateBaseCMD evt){}
 }
}
