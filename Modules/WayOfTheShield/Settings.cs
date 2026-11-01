using System;
using System.Collections.Generic;
namespace Way_of_the_shield {
 public static class Settings {
  public sealed class Flag {readonly bool value;public Flag(bool v){value=v;}public bool GetValue()=>value;public static implicit operator bool(Flag f)=>f?.value==true;}
  public static readonly Flag Debug=new(false),AllowEquipNonProfficientItems=new(true),CheckForShieldLightness=new(true),AddBucklerParry=new(true),AddShieldedDefense=new(true),AllowTwoHanded_as_OneHandedWhenBuckler=new(true),AllowBucklerBashWhenTwoHandedWithUnhindering=new(false),AllowTwoHandedSpears_as_OneHandedWhenMounted=new(true),AllowShieldBashToAllWhoProficient=new(true),BuffSacredShieldEnhacementArray=new(true);
  // Explicitly exclude unrequested upstream changes. These values are retained
  // only for the original conditional descriptions and compatibility checks.
  public static readonly Flag ForbidCloseFlanking=new(false),AllowCloseFlankingToEnemies=new(false),ConcealmentAttackBonusOnBackstab=new(false),DenyShieldBonusOnBackstab=new(false),FlatFootedOnBackstab=new(false),EnableSoftCover=new(false),ChangeBackToBack=new(false),AddSoftCoverDenialToImprovedPreciseShot=new(false),ChangeShieldWall=new(false),ChangeShieldSpell=new(false),RemoveBucklerProficiencies=new(false),GiveImmediateRepositioningToTSS=new(false),RemoveTotalCoverFeatureFromTSS=new(false),FixRapiers=new(false);
  internal static bool Enabled(string key){string k=key.Replace("-","").Replace("_","");foreach(var f in typeof(Settings).GetFields())if(f.FieldType==typeof(Flag)&&f.Name.Replace("_","").Equals(k,StringComparison.OrdinalIgnoreCase))return ((Flag)f.GetValue(null)).GetValue();return false;}
 }
}
