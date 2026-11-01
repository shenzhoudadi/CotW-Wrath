using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Reflection.Emit;
using HarmonyLib;using Kingmaker.Blueprints;using Kingmaker.Blueprints.Classes;using Kingmaker.Blueprints.Classes.Prerequisites;using Kingmaker.Blueprints.Facts;using Kingmaker.EntitySystem;using Kingmaker.UnitLogic;
namespace CallOfTheWild.Wrath.Prototype{
 // Qualify the private animal forms as Wild Shape only in prerequisite checks.
 // Keep the native selection-child exclusions, list counts, inversion and all
 // other ability/stat/class requirements; never grant the stock Druid effects.
 internal static class FeralWildshapePrerequisites01461{
  static readonly BlueprintGuid NativeWolf=BlueprintGuid.Parse("19bb148cb92db224abb431642d10efeb"),OwnWolf=PsychicAssets.Id(FeralHunter01461.WildToken);
  static bool Alias(FeatureCollection features,BlueprintFact feature)=>feature?.AssetGuid==NativeWolf&&features.Enumerable.Any(f=>f.Blueprint.AssetGuid==OwnWolf&&features.HasFact(f.Blueprint));
  internal static bool Feature(FeatureCollection features,BlueprintFact feature)=>features.HasFact(feature)||Alias(features,feature);
  internal static bool Unit(UnitDescriptor unit,BlueprintFact feature)=>UnitHelper.HasFact(unit,feature)||(unit?.Progression?.Features!=null&&Alias(unit.Progression.Features,feature));
  internal static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> source,bool collection){
   int count=0;var result=source.ToArray();foreach(var instruction in result){var method=instruction.operand as MethodInfo;if(method?.Name!="HasFact"||method.GetParameters().Length!=1+(collection?0:1))continue;
    bool match=collection?method.DeclaringType==typeof(EntityFactsProcessor<Feature>)&&method.GetParameters()[0].ParameterType==typeof(BlueprintFact):method==AccessTools.Method(typeof(UnitHelper),nameof(UnitHelper.HasFact),new[]{typeof(UnitDescriptor),typeof(BlueprintFact)});
    if(!match)continue;instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(FeralWildshapePrerequisites01461),collection?nameof(Feature):nameof(Unit));count++;
   }if(count!=1)throw new InvalidOperationException("Feral Wild Shape prerequisite requires exactly one original native ownership test; got "+count);return result;
  }
 }
 [HarmonyPatch(typeof(PrerequisiteFeature),"CheckInternal")]
 internal static class FeralFeaturePrerequisite01461Patch{static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)=>FeralWildshapePrerequisites01461.Replace(source,true);}
 [HarmonyPatch(typeof(PrerequisiteFeaturesFromList),"CheckInternal")]
 internal static class FeralListPrerequisite01461Patch{static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)=>FeralWildshapePrerequisites01461.Replace(source,false);}
 [HarmonyPatch(typeof(PrerequisiteNoFeature),"CheckInternal")]
 internal static class FeralNoFeaturePrerequisite01461Patch{static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)=>FeralWildshapePrerequisites01461.Replace(source,false);}
}
