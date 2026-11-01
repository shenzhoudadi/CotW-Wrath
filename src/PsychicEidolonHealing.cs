using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.RuleSystem;
using Kingmaker.Utility;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;
namespace CallOfTheWild.Wrath.Prototype {
 internal static class PsychicEidolonHealing {
  internal static readonly string[] Names={"RejuvenateEidolonLesser","RejuvenateEidolon","RejuvenateEidolonGreater"};
  internal const string ListId="d1ae05197677491eb236e0aa97080da1";
  internal static void Build(PsychicAssets f){
   var icons=new[]{f.Native<BlueprintAbility>("47808d23c67033d4bbab86a1070fd62f"),f.Native<BlueprintAbility>("6e81a6679a0889a429dec9cedcf3729c"),f.Native<BlueprintAbility>("0d657aa811b310e4bbd8586e60156a2d")};
   for(int i=0;i<3;i++){
    int cap=new[]{5,10,20}[i],dice=1+2*i;string name=new[]{"Rejuvenate Eidolon, Lesser","Rejuvenate Eidolon","Rejuvenate Eidolon, Greater"}[i];
    var heal=new ContextActionHealTarget {Value=new ContextDiceValue{DiceType=DiceType.D10,DiceCountValue=dice,BonusValue=new ContextValue{ValueType=ContextValueType.Rank}}};
    var a=PsychicLinkedWard.Spell(f,Names[i],name,$"Touch your eidolon to heal {dice}d10 + 1 hit point per caster level (maximum +{cap}). Only your own Making Friends eidolon is a legal target. This optional integration adds the spell to that mod's summoner spell list when available; ordinary animal companions are not eidolons.",SpellSchool.Conjuration,icons[i],Run(heal),new SpellDescriptorComponent{Descriptor=SpellDescriptor.RestoreHP|SpellDescriptor.Cure},new PsychicEidolonTarget());
    a.CanTargetSelf=false;a.AvailableMetamagic=Metamagic.Empower|Metamagic.Maximize|Metamagic.Heighten|Metamagic.Quicken|Metamagic.Reach;
    var rank=a.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>();Set(rank,"m_UseMax",true);Set(rank,"m_Max",cap);
   }
  }
  internal static void Integrate(BlueprintsCache cache){
   var list=cache.Load(BlueprintGuid.Parse(ListId)) as BlueprintSpellList;
   if(list==null||list.name!="SummonerSpellbook.SpellList"||list.SpellsByLevel==null)return;
   for(int i=0;i<Names.Length;i++){
    var spell=cache.Load(Id(Names[i])) as BlueprintAbility;if(spell==null)continue;var level=list.SpellsByLevel.SingleOrDefault(x=>x.SpellLevel==i*2+1);if(level==null)continue;
    var refs=((List<BlueprintAbilityReference>)Get(level,"m_Spells")).ToList();if(refs.Any(x=>x.Guid==spell.AssetGuid))continue;refs.Add(Ref<BlueprintAbilityReference>(spell));Set(level,"m_Spells",refs);Set(level,"m_SpellsProxy",null);Set(level,"m_SpellsFiltered",null);
   }
  }
 }
 [TypeId("4fae3dd48c454a6b92540ee85b97a020")]
 public sealed class PsychicEidolonTarget:BlueprintComponent,IAbilityTargetRestriction {
  public bool IsTargetRestrictionPassed(UnitEntityData caster,TargetWrapper target){var master=caster?.Get<UnitPartPetMaster>();return target?.Unit!=null&&master!=null&&master.Pets.Any(p=>p.Entity==target.Unit&&p.EntityPart!=null&&(int)p.EntityPart.Type==604767);}
  public string GetAbilityTargetRestrictionUIText(UnitEntityData caster,TargetWrapper target)=>"Requires your own Making Friends eidolon.";
 }
}
