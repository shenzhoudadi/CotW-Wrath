using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class CompanionSpellSharing {
  internal const string Token="CompanionShareSpellsFeature";
  internal const string ProgressionId="f54159e8cfda69f4dbcfdf6ffbbd3293";
  static BlueprintFeature Feature;
  static readonly HashSet<BlueprintGuid> CreatureTypes=new HashSet<BlueprintGuid>(new[]{
   "3bec99efd9a363242a6c8d9957b75e91","a95311b3dc996964cbaa30ff9965aaf6","fd389783027d63343b4a5634bd81645f",
   "455ac88e22f55804ab87c2467deff1d6","018af8005220ac94a9a4f47b3e9c2b4e","625827490ea69d84d8e599a33929fdc6",
   "57614b50e8d86b24395931fffc5e409b","9054d3988d491d944ac144e27b6bc318","706e61781d692a042b35941f14bc41c5",
   "734a29b693e9ec346ba2951b27987e33","09478937695300944a179530664e42ec"
  }.Select(BlueprintGuid.Parse));
  internal static bool Supported(PetType type)=>type==PetType.AnimalCompanion||type==PetType.NightHag||type==PetType.MythicSkeletalChampion||type==PetType.AzataHavocDragon;
  internal static bool Active(Feature f)=>f!=null&&!f.IsTemporary&&f.IsActive&&f.IsTurnedOn&&!f.IsDisposed;
  // A selected companion inherits its selection's actual feature source. Rank
  // sources additionally cover multiclass pet progression and domain grants.
  // Merely owning a companion never authorizes every spellbook on the owner.
  static bool SourceMatches(UnitEntityData owner,BlueprintScriptableObject source,BlueprintCharacterClass cls,int depth){
   if(source==null||depth>8)return false;
   if(source is BlueprintCharacterClass c)return c==cls;
   var facts=owner.Facts.List;bool hasExactSource=false;
   for(int i=0;i<facts.Count;i++)if(facts[i] is Feature f&&Active(f)&&f.Blueprint==source){
    var parent=f.Source?.Blueprint;
    if(parent!=null&&parent!=source){hasExactSource=true;if(SourceMatches(owner,parent,cls,depth+1))return true;}
    if(f.RankToSource!=null)for(int j=0;j<f.RankToSource.Count;j++){var rankSource=f.RankToSource[j].Blueprint;if(rankSource!=null&&rankSource!=source){hasExactSource=true;if(SourceMatches(owner,rankSource,cls,depth+1))return true;}}
   }
   if(hasExactSource)return false;
   // A multi-class domain progression lists classes that *may* grant it,
   // including archetype-only choices. That list is not proof that this unit
   // acquired its companion from every such class. Only an unambiguous sole
   // class is a safe fallback when old rank data lacks a parent source fact.
   if(source is BlueprintProgression p&&owner.Progression.GetClassLevel(cls)>0){
    BlueprintCharacterClass sole=null;foreach(var progressionClass in p.Classes){if(sole!=null&&sole!=progressionClass)return false;sole=progressionClass;}return sole==cls;
   }
   return false;
  }
  static bool FeatureSourceMatches(UnitEntityData owner,Feature f,BlueprintCharacterClass cls){
   if(SourceMatches(owner,f.Source?.Blueprint,cls,0))return true;
   if(f.RankToSource!=null)for(int i=0;i<f.RankToSource.Count;i++)if(SourceMatches(owner,f.RankToSource[i].Blueprint,cls,0))return true;
   return false;
  }
  internal static bool BookAllowed(AbilityData ability,PetType? wanted=null){
   var owner=ability?.Caster?.Unit;var book=ability?.Spellbook;if(owner==null||book==null)return false;
   var cls=book.Blueprint.CharacterClass;
   if(cls!=null&&ClassAllowed(owner,cls,wanted))return true;
   // The native mythic merge changes ClassData.Spellbook to the selected
   // ordinary book. Use that actual mapping, not all books on a mythic caster.
   foreach(var data in owner.Progression.Classes)if(data.Level>0&&data.CharacterClass!=cls&&data.Spellbook==book.Blueprint&&(!data.CharacterClass.IsMythic||book.IsMythic)&&ClassAllowed(owner,data.CharacterClass,wanted))return true;
   return false;
  }
  static bool ClassAllowed(UnitEntityData owner,BlueprintCharacterClass cls,PetType? wanted){
   var facts=owner.Facts.List;
   for(int i=0;i<facts.Count;i++)if(facts[i] is Feature f&&Active(f)){
    var components=f.Blueprint.ComponentsArray;
    for(int j=0;j<components.Length;j++)if(components[j] is AddPet pet&&Supported(pet.Type)&&(!wanted.HasValue||wanted.Value==pet.Type)){
     if(FeatureSourceMatches(owner,f,cls))return true;
     var rank=pet.LevelRank;
     if(rank!=null)for(int k=0;k<facts.Count;k++)if(facts[k] is Feature r&&r.Blueprint==rank&&Active(r)&&FeatureSourceMatches(owner,r,cls))return true;
    }
   }
   return false;
  }
  internal static bool OwnNativePet(AbilityData ability,UnitEntityData target){var pet=target?.Get<UnitPartPet>();return pet!=null&&Supported(pet.Type)&&pet.Master==ability?.Caster?.Unit;}
  internal static bool RecipientAllowed(AbilityData ability,UnitEntityData target){var pet=target?.Get<UnitPartPet>();return pet!=null&&Supported(pet.Type)&&pet.Master==ability?.Caster?.Unit&&BookAllowed(ability,pet.Type);}
  internal static bool TypeRestriction(IAbilityTargetRestriction restriction){
   if(!(restriction is AbilityTargetHasFact facts)||facts.FromCaster||facts.CheckedFacts.Length==0)return false;
   // Only an exclusively creature-type predicate is waived. Mixed fact lists,
   // immunity, size, equipment, alignment and caster restrictions stay intact.
   for(int i=0;i<facts.CheckedFacts.Length;i++){var fact=facts.CheckedFacts.Get(i);if(fact==null||!CreatureTypes.Contains(fact.AssetGuid))return false;}
   return true;
  }
  internal static void EnsureFeature(UnitPartPet pet){
   if(Feature==null||pet?.Owner==null||pet.Master==null||!Supported(pet.Type))return;
   if(!pet.Owner.HasFact(Feature))pet.Owner.AddFact(Feature);
  }
  internal static void Register(BlueprintsCache cache){
   var f=new PsychicAssets(cache);
   var progression=cache.Load(BlueprintGuid.Parse(ProgressionId))as BlueprintProgression;
   if(progression==null)throw new InvalidOperationException("Native animal companion progression is missing.");
   var first=progression.LevelEntries.First(e=>e.Level==1);
   var refs=(List<BlueprintFeatureBaseReference>)Get(first,"m_Features");
   var created=f.Feature(Token,"Share Spells (Companion)","Share Spells (Ex): Your master may cast personal-range spells from the spellcasting class that grants or advances this companion on you as touch spells instead of on themselves. This ability is available from companion level 1 or as soon as the companion is gained. It also applies to native mounts and other supported permanent class companions with a matching source spellbook. Creature-type target restrictions are waived for this sharing; other target restrictions remain. Spell-like abilities, item spells, alchemical extracts and unrelated spellbooks are not shared. Bonded Mind and the Share Spells teamwork feat are not required.");
   Set(created,"m_Icon",f.Native<BlueprintAbility>("403cf599412299a4f9d5d925c7b9fb33").Icon);PsychicRepairComponents.UniqueNames(created);
   var old=cache.Load(created.AssetGuid);
   if(old!=null&&(!(old is BlueprintFeature)||old.name!=created.name))throw new InvalidOperationException("Companion sharing identity collision: "+created.AssetGuid);
   Feature=(BlueprintFeature)(old??created);if(old==null){f.Enable();cache.AddCachedBlueprint(created.AssetGuid,created);}
   if(!refs.Any(r=>r.Guid==Feature.AssetGuid))Set(first,"m_Features",refs.Concat(new[]{Ref<BlueprintFeatureBaseReference>(Feature)}).ToList());
  }
 }
 // Initialize handles new companions, OnPostLoad grants the same visible
 // passive to existing companions. Neither path creates or replaces a pet.
 [HarmonyPatch(typeof(UnitPartPet),nameof(UnitPartPet.Initialize))]
 internal static class CompanionSharingNewPetPatch {
  [HarmonyPostfix]static void Postfix(UnitPartPet __instance)=>CompanionSpellSharing.EnsureFeature(__instance);
 }
 [HarmonyPatch(typeof(UnitPartPet),"OnPostLoad")]
 internal static class CompanionSharingLoadedPetPatch {
  [HarmonyPostfix]static void Postfix(UnitPartPet __instance)=>CompanionSpellSharing.EnsureFeature(__instance);
 }
}
