using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.UnitLogic.FactLogic;
using HarmonyLib;

namespace CallOfTheWild.Wrath.Prototype {
 internal static class WayOfTheShieldIntegration {
  internal const string ModuleName="Way of the Shield";
  internal static readonly string[] CombatIds={"bacc4f29695d4aec868d31be1c04f29b","39470145204746cdb46cd95bcee4a92c","3a76e2e292d14c4184165fffdbc0c4e0","7da0bf0d789d4528b4c3b707eb98fbd2","836dafe9db094691bce5dc0c6b0305ac","c748ba6f94fb4dd68e5aebbe8562a80b"};
  internal static readonly string[] UpstreamRootIds=CombatIds.Where(id=>id!="3a76e2e292d14c4184165fffdbc0c4e0").Concat(new[]{"3d621329f6874035b4be27edc6b84f25","0a1c1d2138964c97ba8f48d7843ea685","56f19f65e28b4c3fb03e425fb047e08a","7ca07f3bdbf145c2a975b11d9690c0b3","a86db1a253d0446b857938314af1ae51"}).ToArray();
  static Type entry;static Exception error;static bool prepared,external,configured;
  internal static void Prepare(){if(prepared)return;prepared=true;try{
   string path=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(ModEntry).Assembly.Location),"../Modules/WayOfTheShield/Way of the shield.dll"));
   var existing=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name.Equals("Way of the shield",StringComparison.OrdinalIgnoreCase));
   if(existing!=null&&!string.Equals(Path.GetFullPath(existing.Location),path,StringComparison.OrdinalIgnoreCase)){external=true;entry=existing.GetType("Way_of_the_shield.Main",true);return;}
   if(!File.Exists(path))throw new FileNotFoundException("Bundled Way of the Shield module is missing. Reinstall the complete CotW archive.",path);
   entry=(existing??Assembly.LoadFrom(path)).GetType("Way_of_the_shield.Main",true);entry.GetMethod("PrepareIntegrated").Invoke(null,null);
  }catch(Exception e){error=e;}}
  internal static void Register(BlueprintsCache cache){if(configured)return;if(!prepared||error!=null)throw new InvalidOperationException("Way of the Shield could not start.",error);
   if(external){if(UpstreamRootIds.Any(id=>cache.Load(BlueprintGuid.Parse(id))==null))throw new InvalidOperationException("An external Way of the Shield provider is loaded but has not registered all its requested roots. CotW did not install a duplicate provider.");RegisterExternalLightParry(cache);}
   else entry.GetMethod("ConfigureIntegrated").Invoke(null,new object[]{cache});
   CombatFeatPoolRepair.Reconcile(cache);configured=true;
  }
  static void RegisterExternalLightParry(BlueprintsCache cache){
   const string id="3a76e2e292d14c4184165fffdbc0c4e0";
   if(cache.Load(BlueprintGuid.Parse(id))!=null)return;
   var parry=cache.Load(BlueprintGuid.Parse("e547039b227a42d7a8d98fad72f8717c")) as BlueprintFeature??throw new InvalidOperationException("External shield provider did not register the shared parry feature.");
   BlueprintRegistration.RegisterFeature(cache,id,"Shield_LightShieldParryFeature","LightShieldParryFeature_DisplayName","LightShieldParryFeature_Description",ShieldedMage.ShieldFocusId,()=>{
    var requirement=new PrerequisiteProficiency{name="LightShieldParryProficiency"};
    AccessTools.Field(typeof(PrerequisiteProficiency),"ArmorProficiencies").SetValue(requirement,new[]{ArmorProficiencyGroup.LightShield});
    AccessTools.Field(typeof(PrerequisiteProficiency),"WeaponProficiencies").SetValue(requirement,Array.Empty<Kingmaker.Enums.WeaponCategory>());
    var grants=new AddFacts{name="LightShieldParrySharedEngine"};AccessTools.Field(typeof(AddFacts),"m_Facts").SetValue(grants,new[]{BlueprintReferenceBase.CreateTyped<BlueprintUnitFactReference>(parry)});
    return new BlueprintComponent[]{requirement,grants};
   },BlueprintRegistration.NativeCombatFeatSelectionIds,new[]{FeatureGroup.Feat,FeatureGroup.CombatFeat});
   ApplyLocalization();
  }
  internal static void ApplyLocalization(){if(!external&&entry!=null)entry.GetMethod("ApplyLocalization")?.Invoke(null,null);
   if(external){Kingmaker.Localization.LocalizationManager.CurrentPack?.PutString("LightShieldParryFeature_DisplayName","Light Shield Parry");Kingmaker.Localization.LocalizationManager.CurrentPack?.PutString("LightShieldParryFeature_Description","You learned how to parry incoming attacks with your light shield.");}
  }
 }
}
