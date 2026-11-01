using System.Linq;using Kingmaker.EntitySystem.Entities;using Kingmaker.Blueprints;using Kingmaker.Blueprints.JsonSystem;using Kingmaker.UnitLogic;
namespace CallOfTheWild.Wrath.Prototype{
 internal static class PsychicMajor01460{
  internal static bool Has(UnitEntityData owner)=>owner?.Facts.List.Any(f=>f.Blueprint.AssetGuid==BlueprintGuid.Parse(PsychicClass.MajorId))==true;
  internal static bool Allows(UnitEntityData owner,PsychicAmplificationComponent c)=>c.MinimumLevel==11&&(c.Kind==PsychicAmplificationKind.SynapticShock||c.Kind==PsychicAmplificationKind.SpaceRendingSpell)?Has(owner):owner.Progression.GetClassLevel(c.CharacterClass.Get())>=c.MinimumLevel;
 }
 [TypeId("45ec5160b9324c20ac57a16f60000301")]
 public sealed class MajorGrant01460:Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel{
  protected override void OnPostLoad(){
   // Native restoration consults PlayerUpgradeActions only for a stale
   // AppliedFact, and ignores entirely new component data. This component
   // owns only our empty marker; retain native level/source accounting.
   if(Data.AppliedFact!=null&&!Owner.HasFact(Data.AppliedFact))Data.AppliedFact=null;
   base.OnPostLoad();HandleUnitGainLevel();
  }
 }
}
