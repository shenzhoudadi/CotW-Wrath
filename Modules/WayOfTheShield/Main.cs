using System;
using System.Linq;
using System.Reflection;
using System.IO;
using Newtonsoft.Json.Linq;
using Owlcat.Runtime.Core.Logging;
using Kingmaker.TextTools;
using System.Collections.Generic;
using System.Security.Cryptography;
using Way_of_the_shield.NewFeatsAndAbilities;
using Way_of_the_shield.NewComponents;
using Way_of_the_shield.ProficiencyRework;
using Way_of_the_shield.Tweaks_and_Changes;
using Kingmaker.UnitLogic.ActivatableAbilities;
namespace Way_of_the_shield {
 // One parent CotW entry point owns this curated module. Original module and
 // saved component identities remain, with no second UMM/Owlcat entry point.
 public static class Main {
  public static string modName="Shield",modPath;public static Harmony harmony;
  public static Assembly[] allAssemblies=Array.Empty<Assembly>();public static Assembly TTTCore,TTTBase,UMM,ModMenu;
  public static bool BeenLoaded;public static bool IntegratedModule=>true;
  public static Assembly CheckForMod(string name)=>AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a!=typeof(Main).Assembly&&a.GetName().Name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0);
  public static bool CheckForModEnabled(string name)=>CheckForMod(name)!=null;
  public static void PrepareIntegrated(){if(BeenLoaded)return;modPath=Path.GetDirectoryName(typeof(Main).Assembly.Location);allAssemblies=AppDomain.CurrentDomain.GetAssemblies();TTTCore=CheckForMod("TabletopTweaks-Core");TTTBase=CheckForMod("TabletopTweaks-Base");Utilities.Comment=LogChannelFactory.GetOrCreate("CotW.WayOfTheShield");ApplyLocalization();TextTemplateEngine.AddTemplate("shield_check_set_bool",new Utilities.BoolSettingCheckerTemplate());BeenLoaded=true;}
  public static void ApplyLocalization(){using(var stream=typeof(Main).Assembly.GetManifestResourceStream(typeof(Main).Assembly.GetManifestResourceNames().Single(n=>n.EndsWith(".Localization.enGB.json"))))using(var reader=new StreamReader(stream)){var text=JObject.Parse(reader.ReadToEnd());foreach(var item in text["strings"]??text["Strings"]??new JArray())LocalizationManager.CurrentPack?.PutString((string)item["Key"],(string)item["Value"]);}}
  public static readonly string[] CombatFeatIds={"bacc4f29695d4aec868d31be1c04f29b","39470145204746cdb46cd95bcee4a92c","3a76e2e292d14c4184165fffdbc0c4e0","7da0bf0d789d4528b4c3b707eb98fbd2","836dafe9db094691bce5dc0c6b0305ac","c748ba6f94fb4dd68e5aebbe8562a80b"};
  public static readonly string[] MythicFeatIds={"3d621329f6874035b4be27edc6b84f25","0a1c1d2138964c97ba8f48d7843ea685"};
  public static readonly string[] RuntimePatchTypes={
   "Way_of_the_shield.MechanicsFeatureExtension","Way_of_the_shield.ItemEntityWeapon__HoldInTwoHands__Patch",
   "Way_of_the_shield.ProficiencyRework.ProficiencyPatches+FixTheArmorBug_ThisPatchDoesNotWantToLoad","Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ProficiencyFactsChanged","Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ItemEntity_EquipNonProfficient_Patch","Way_of_the_shield.ProficiencyRework.ProficiencyPatches+UIUtilityItemPatch","Way_of_the_shield.ProficiencyRework.ProficiencyPatches+ItemEntityArmorPatch","Way_of_the_shield.ProficiencyRework.WeaponNoProficiency","Way_of_the_shield.ShieldRuntimePatches",
   "Way_of_the_shield.NewFeatsAndAbilities.UnhinderingShield","Way_of_the_shield.NewComponents.OffHandParry+RuleCalculateAttacksCount_OnTrigger_patch","Way_of_the_shield.NewComponents.OffHandParry+ItemEntityShield_patch_WeaponComponentForBucklers","Way_of_the_shield.NewComponents.BuffSetRanksOnApply","Way_of_the_shield.NewComponents.BuffDynamicDescriptionComponent_Base+Transpiler","Way_of_the_shield.NewComponents.ArrowCatching",
   "Way_of_the_shield.Utilities+BonusTypeExtenstions","Way_of_the_shield.Utilities+ModifierDescriptorExtension","Way_of_the_shield.ShieldAcState","Way_of_the_shield.MountedPowerAttack"
  };
  public static void InstallRuntime(Harmony owner){
   CotW.Runtime.HarmonyRuntimeCompatibility.Verify();
   TWFShieldTweak.VerifyNativeLightness();
   foreach(string name in RuntimePatchTypes)owner.CreateClassProcessor(typeof(Main).Assembly.GetType(name,true)).Patch();
   owner.Patch(AccessTools.Method(typeof(ActivatableAbilityCollection),"OnFactDidAttach",new[]{typeof(ActivatableAbility)}),transpiler:new HarmonyMethod(typeof(ShieldBashAbilityTweak),nameof(ShieldBashAbilityTweak.ActivatableAbilityCollection_OnFactDidAttach_PatchToNotActivateOnAttach)));
   Utilities.ModifierDescriptorExtension.EnsureOrder(ModifierDescriptorComparer.Instance);
  }
  static bool runtimeInstalled,configured;
  static readonly HashSet<MethodInfo> completedBlueprintSteps=new();
  static void RunBlueprintStep(BlueprintBuildStep01444 step,Action build){
   if(completedBlueprintSteps.Contains(build.Method))return;
   BlueprintBuildResults01444.Preflight(step);
   build();
   if(!BlueprintBuildResults01444.IsComplete(step))
    throw new InvalidOperationException("Way of the Shield builder returned without publishing its complete result: "+step);
   completedBlueprintSteps.Add(build.Method);
  }
  public static void ConfigureIntegrated(BlueprintsCache cache){
   if(configured)return;
   if(cache!=ResourcesLibrary.BlueprintsCache)throw new InvalidOperationException("Configure the live blueprint cache only.");
   PrepareIntegrated();
   foreach(string name in RuntimePatchTypes)typeof(Main).Assembly.GetType(name,true);
   // Runtime patches belong to the process, not to a blueprint attempt. A
   // later builder failure must not replay Harmony 2.0.4 patch installation.
   if(!runtimeInstalled){
    harmony=new Harmony("CotW.WayOfTheShield");
    try{InstallRuntime(harmony);runtimeInstalled=true;}
    catch{harmony.UnpatchAll(harmony.Id);throw;}
   }
   // Earlier successful builders may not be reentrant (for example buckler
   // grip publishes a fixed GUID). Resume at the failed step, not from scratch.
   RunBlueprintStep(BlueprintBuildStep01444.Proficiency,ProficiencyPatches.BlueprintsCache_Init_Patch_For_Shields_Proficiency.Postfix);
   RunBlueprintStep(BlueprintBuildStep01444.BucklerParry,BoringBucklerTweaks.AddBucklerParryToBucklerProfficiencyBlueprint);
   RunBlueprintStep(BlueprintBuildStep01444.BucklerGrip,BoringBucklerTweaks.Buckler1h_Component.CachePatch.AddBuckler1hToBlueprint);
   RunBlueprintStep(BlueprintBuildStep01444.ShieldBash,ShieldBashAbilityTweak.BlueprintsCache_Init_Patch_MoveShieldBashAbilityToProficiencyBP);
   RunBlueprintStep(BlueprintBuildStep01444.CouchedSpears,CouchedSpears.AddCouchedSpears1hToMountedCombat);
   RunBlueprintStep(BlueprintBuildStep01444.OneHandedGrip,OneHandedToggle.CachePatch_Postfix);
   RunBlueprintStep(BlueprintBuildStep01444.ShieldedDefense,ShieldedDefense.BlueprintCache_Init_Patch);
   RunBlueprintStep(BlueprintBuildStep01444.ShieldBrace,ShieldBrace.CachePatch.Postfix);
   RunBlueprintStep(BlueprintBuildStep01444.UnhinderingShield,UnhinderingShield.BlueprintsCache_Patch);
   RunBlueprintStep(BlueprintBuildStep01444.UpsettingStyle,UpsettingShieldStyle.BlueprintsCache_Init_Patch);
   RunBlueprintStep(BlueprintBuildStep01444.MythicParry,Mythic_BucklerParry.CreateBlueprint);
   RunBlueprintStep(BlueprintBuildStep01444.SacredShield,NewArchetypes.SacredShield.CreateSacredShieldArchetype);
   foreach(var asset in Utilities.Owned.Values)asset.OnEnable();
   foreach(string id in CombatFeatIds.Concat(MythicFeatIds).Concat(new[]{"56f19f65e28b4c3fb03e425fb047e08a","7ca07f3bdbf145c2a975b11d9690c0b3","a86db1a253d0446b857938314af1ae51"}))if(cache.Load(BlueprintGuid.Parse(id))==null)throw new InvalidOperationException("Missing requested shield content: "+id);
   configured=true;
  }
 }
}
