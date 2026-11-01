using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicSpiritSpells {
  internal static readonly string[] Tokens={"SenseSpiritMagic","SpiritCall","FontOfSpiritMagic"};
  internal static readonly string[][] NativeMaps={
   new[]{"557cd05f067b44e8a916d6324c0c6c42",null,"a995ae02b3e741439b82e5e5d5a2e046"},
   new[]{"26e81d83d13dbf6448b10019c802612d","92918c8de8f0c7b448531cf3a4fbb6ad","e788846aaf449404db11e51182174be8"},
   new[]{"65b5e7f17a1b928418fc9e8a6b55eafa","004eb31f1217d444b91a5404f3c0a385","24dc5eb552a9e2c4cb9a17c355a80d2e"},
   new[]{"50efeb23b314869409365c8061a4082d","d9fbc976432e4494788f5d0996e8a52f","659fbc54fc519b44dacacc78e7d46dec"},
   new[]{"44c130a12d49ebf4a95945feffe5aae7","e9126b55e8734004cbbe8bb5fbb9aedd","bbae401660bbad94c865d71029d8439e"},
   new[]{"f9ee5a9146561ba4bac4cb8a7126f85c","de6758cb6044b3f498ac0fa2b3e440d3","1e6ea0d1d642c8c43ab7e72dd8f607a9"},
   new[]{"379d33ef16e812146b5517601ab25e66","f3e19b0d6d82e2a4d98957af591f5d36","810e2a7009ed3a84c81b398b2763e7a8"},
   new[]{"acff6b0cf279a31439010afea01df912","9be79014f07a3fb4f90ad48e19e23e41","87a3e296757412e45910493e5fed1417"},
   new[]{"2196e093593903f4ebc36467ccbf1d9a","bfbed44eafaa4344189730403b426a88","190ebde2d9d80c94783bcb73b9106d99"},
   new[]{"eae7cee2a5da93442a7563dfeda33432","11a92d0a44576e4439b745ab00f298b5","0bf6f90fdcb864b4486344100391b478"}
  };
  internal static void Build(PsychicAssets f){
   var maps=NativeMaps.Select(row=>new PsychicSpiritMap {Primary=Ref<BlueprintFeatureReference>(f.Native<BlueprintProgression>(row[0])),Wandering=row[1]==null?null:Ref<BlueprintFeatureReference>(f.Native<BlueprintProgression>(row[1])),List=Ref<BlueprintSpellListReference>(f.Native<BlueprintSpellList>(row[2]))}).ToArray();
   var druid=Ref<BlueprintCharacterClassReference>(f.Native<BlueprintCharacterClass>("610d836f3a3a9ed42a4349b62f002e96"));
   string[] names={"Sense Spirit Magic","Spirit Call","Font of Spirit Magic"};
   string[] descriptions={"For 24 hours, gain a +2 circumstance bonus on saves against spells associated with your current primary and wandering spirits. Changing spirits changes the protected spell lists.","For 1 minute per caster level, all casters within 60 feet gain +1 caster level and +4 concentration on spells associated with your primary spirit or your druid domains. Wandering spirits do not qualify. Enemies also benefit. Casting takes a full-round action.","For 1 round per caster level, allies within 30 feet gain +2 on spell resistance checks, concentration checks and DCs for spells associated with your primary and wandering spirits. This increases caster level checks, not caster level itself. Consumes one diamond dust."};
   for(int i=0;i<3;i++){
    string token=Tokens[i],text=descriptions[i]+" Overlapping copies of this spell do not stack.";
    var icon=i==0?f.Native<BlueprintAbility>("b3da3fbee6a751d4197e446c7e852bcb").Icon:PsychicOriginalIcons.Load(token);
    var effect=PsychicWeatherSpells.OwnedBuff(f,token+"Effect",names[i],text,icon,new PsychicSpiritMagicBonus {Mode=i,Spirits=maps,Druid=druid});
    BlueprintBuff host=effect;
    if(i!=0){var owned=i==2?PsychicWeatherSpells.Owned(effect,new ContextConditionIsAlly()):PsychicWeatherSpells.Owned(effect);var area=PsychicWeatherSpells.Area(f,token,names[i],text,new PrefabLink(),AreaEffectShape.Cylinder,i==1?60:30,false,owned);area.AggroEnemies=false;var add=new AddAreaEffect();Set(add,"m_AreaEffect",Ref<BlueprintAbilityAreaEffectReference>(area));host=f.Buff(token+"Buff",names[i],text,add);Set(host,"m_Icon",icon);}
    else host.Stacking=StackingType.Replace;
    var rank=new ContextRankConfig();SetEnum(rank,"m_BaseValueType","CasterLevel");var apply=Apply(host,i==0?1:0,i==0?DurationRate.Days:i==1?DurationRate.Minutes:DurationRate.Rounds);if(i!=0)apply.DurationValue.BonusValue=new ContextValue {ValueType=ContextValueType.Rank};
    var a=f.Ability(token,names[i],text,AbilityType.Spell,UnitCommand.CommandType.Standard,AbilityRange.Personal,new SpellComponent {School=i==0?SpellSchool.Divination:SpellSchool.Enchantment},rank,Run(apply),new AbilitySpawnFx {PrefabLink=new PrefabLink {AssetId=i==0?"8de64fbe047abc243a9b4715f643739f":i==1?"44295e9774b2864488f4a3790b8b0bcf":"930c1a4aa129b8344a40c8c401d99a04"},Time=AbilitySpawnFxTime.OnApplyEffect,Anchor=AbilitySpawnFxAnchor.SelectedTarget});Set(a,"m_Icon",icon);a.CanTargetSelf=true;a.NotOffensive=true;a.SpellResistance=false;a.AvailableMetamagic=Metamagic.Extend|Metamagic.Heighten|Metamagic.Quicken;Set(a,"m_IsFullRoundAction",i==1);a.LocalizedDuration=Text(token,"duration",i==0?"24 hours":i==1?"1 minute per caster level":"1 round per caster level");a.LocalizedSavingThrow=Text(token,"savingThrow","");
    if(i==2){var material=new BlueprintAbility.MaterialComponentData {Count=1};Set(material,"m_Item",Ref<BlueprintItemReference>(f.Native<BlueprintItem>("92752bbbf04dfa1439af186f48aee0e9")));a.MaterialComponent=material;}
   }
  }
 }
 public sealed class PsychicSpiritMap {public BlueprintFeatureReference Primary,Wandering;public BlueprintSpellListReference List;}
 [TypeId("aa210e8f647b4cfd934beb719ef4e28a")]
 public sealed class PsychicSpiritMagicBonus:UnitBuffComponentDelegate,IInitiatorRulebookHandler<RuleSavingThrow>,IInitiatorRulebookHandler<RuleCalculateAbilityParams>,IInitiatorRulebookHandler<RuleSpellResistanceCheck>,ISubscriber {
  public int Mode;public PsychicSpiritMap[] Spirits;public BlueprintCharacterClassReference Druid;
  internal bool Matches(BlueprintAbility spell){
   var caster=Context.MaybeCaster;if(caster==null||spell==null)return false;
   var lists=new List<BlueprintSpellList>();foreach(var map in Spirits)if(caster.HasFact(map.Primary.Get())||(Mode!=1&&map.Wandering!=null&&caster.HasFact(map.Wandering.Get())))lists.Add(map.List.Get());
   if(Mode==1)foreach(var fact in caster.Facts.List.OfType<UnitFact>())foreach(var c in fact.Blueprint.GetComponents<AddSpecialSpellList>())if(c.CharacterClass==Druid.Get()&&caster.Progression.GetClassLevel(Druid.Get())>0&&c.SpellList!=null)lists.Add(c.SpellList);
   var ids=new HashSet<BlueprintGuid>();for(var a=spell;a!=null&&ids.Add(a.AssetGuid);a=a.Parent){}
   return lists.Distinct().Any(l=>l.SpellsByLevel.Any(level=>((List<BlueprintAbilityReference>)Get(level,"m_Spells")).Any(r=>r!=null&&ids.Contains(r.Guid))));
  }
  bool First(BlueprintAbility spell)=>Matches(spell)&&Owner.Buffs.Enumerable.FirstOrDefault(b=>b.IsTurnedOn&&!b.IsSuppressed&&b.Blueprint.GetComponent<PsychicSpiritMagicBonus>() is PsychicSpiritMagicBonus c&&c.Mode==Mode&&MatchesFor(c,b,spell))==Buff;
  static bool MatchesFor(PsychicSpiritMagicBonus c,Kingmaker.UnitLogic.Buffs.Buff b,BlueprintAbility spell){var runtime=b.Components.FirstOrDefault(r=>r.SourceBlueprintComponent==c);if(runtime==null)return false;using(runtime.RequestEventContext())return c.Matches(spell);}
  public void OnEventAboutToTrigger(RuleSavingThrow e){if(Mode==0&&First(e.Reason.Context?.SourceAbility))e.AddModifier(2,Fact,ModifierDescriptor.Circumstance);}
  public void OnEventAboutToTrigger(RuleCalculateAbilityParams e){if(Mode==0||!First(e.Spell))return;if(Mode==1){e.AddBonusCasterLevel(1,ModifierDescriptor.UntypedStackable);e.AddBonusConcentration(4);}else{e.AddBonusDC(2,ModifierDescriptor.UntypedStackable);e.AddBonusConcentration(2);}}
  public void OnEventAboutToTrigger(RuleSpellResistanceCheck e){if(Mode==2&&First(e.Ability))e.AddSpellPenetration(2,ModifierDescriptor.UntypedStackable);}
  public void OnEventDidTrigger(RuleSavingThrow e){} public void OnEventDidTrigger(RuleCalculateAbilityParams e){} public void OnEventDidTrigger(RuleSpellResistanceCheck e){}
 }
}
