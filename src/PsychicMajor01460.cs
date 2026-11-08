using System.Linq;using HarmonyLib;using Kingmaker.EntitySystem.Entities;using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.UnitLogic;
namespace CallOfTheWild.Wrath.Prototype{
 internal static class PsychicMajor01460{
  // The psychic class whose level gates Major Amplifications.
  static BlueprintCharacterClass MajorClass=>ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(BlueprintGuid.Parse(PsychicClass.ClassId));
  // The Esoteric Starseeker (0081) delays Major Amplifications from 11th to 15th
  // level. Detection is strictly by the archetype recorded on the psychic class
  // data: never a global HasFact spoof and never a disguised class level.
  internal static bool IsStarseeker(UnitEntityData owner){var cls=MajorClass;return cls!=null&&owner?.Progression.GetClassData(cls)?.Archetypes.Any(a=>a.AssetGuid==PsychicAssets.Id(EsotericStarseeker0081.RootToken))==true;}
  internal static int RequiredLevel(UnitEntityData owner)=>IsStarseeker(owner)?15:11;
  internal static bool Has(UnitEntityData owner){
   if(owner==null||!owner.Facts.List.Any(f=>f.Blueprint.AssetGuid==BlueprintGuid.Parse(PsychicClass.MajorId)))return false;
   // A normal psychic is granted MajorId at 11th, so the fact already implies the
   // level and this check leaves that path byte-for-byte unchanged; it only holds
   // an Esoteric Starseeker back until 15th.
   var cls=MajorClass;return cls==null||owner.Progression.GetClassLevel(cls)>=RequiredLevel(owner);
  }
  internal static bool Allows(UnitEntityData owner,PsychicAmplificationComponent c)=>c.MinimumLevel==11&&(c.Kind==PsychicAmplificationKind.SynapticShock||c.Kind==PsychicAmplificationKind.SpaceRendingSpell)?Has(owner):owner.Progression.GetClassLevel(c.CharacterClass.Get())>=c.MinimumLevel;
 }
 [TypeId("45ec5160b9324c20ac57a16f60000301")]
 public sealed class MajorGrant01460:Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel{
  protected override void OnActivate(){
   if(ReconcileIfStarseeker())return;
   base.OnActivate();
  }
  protected override void OnPostLoad(){
   // Native restoration consults PlayerUpgradeActions only for a stale
   // AppliedFact, and ignores entirely new component data. This component
   // owns only our empty marker; retain native level/source accounting.
   if(Data.AppliedFact!=null&&!Owner.HasFact(Data.AppliedFact))Data.AppliedFact=null;
   base.OnPostLoad();
   if(!ReconcileIfStarseeker())HandleUnitGainLevel();
  }
  // Diverts only an Esoteric Starseeker's MajorId grant from the native 11th-level
  // threshold to 15th, mirroring the private native Apply (grant when due, remove
  // when not). Returns false for every other owner so the normal psychic runs the
  // untouched native component. The 15th-level archetype progression entry and this
  // component both offer MajorId; the HasFact guard leaves FeatureSet deduplication
  // authoritative, so no rank is granted twice.
  internal bool ReconcileIfStarseeker(){
   if(!PsychicMajor01460.IsStarseeker(Owner))return false;
   if(Owner.Progression.GetClassLevel(Class)>=15){
    if(Data.AppliedFact==null&&!Owner.HasFact(Feature))Data.AppliedFact=Owner.AddFact(Feature);
   }else{
    if(Data.AppliedFact!=null){Owner.RemoveFact(Data.AppliedFact);Data.AppliedFact=null;}
    // Progression, an older save, respec restoration or another legitimate
    // source can leave the marker here without populating AppliedFact. The
    // archetype must not hold MajorId below 15 regardless of that source.
    foreach(var stale in Owner.Facts.List.Where(f=>f.Blueprint==Feature).ToArray())Owner.RemoveFact(stale);
   }
   return true;
  }
 }
 // HandleUnitGainLevel is public and non-virtual, so it cannot be overridden; the
 // PubSub level-up event reaches it through IOwnerGainLevelHandler interface
 // dispatch, which a detour intercepts reliably (the private Apply it calls could
 // otherwise be inlined past a patch). Only MajorGrant01460 instances belonging to
 // an Esoteric Starseeker are diverted; every other AddFeatureOnClassLevel returns
 // true here and runs the native apply unchanged.
 [HarmonyPatch(typeof(Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel),nameof(Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel.HandleUnitGainLevel))]
 internal static class MajorGrantStarseeker0081Patch{
  static bool Prefix(Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel __instance)=>!(__instance is MajorGrant01460 grant)||!grant.ReconcileIfStarseeker();
 }
}
