using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.RuleSystem.Rules;
using BonusType = Kingmaker.RuleSystem.Rules.BonusType;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.TextTools;
using Owlcat.Runtime.Core.Logging;
using UnityEngine;

namespace Way_of_the_shield {
 // Curated infrastructure for the requested upstream features. Do not import
 // unrelated teamwork, flanking, cheats, descriptor replacements or geometry.
 public static class Utilities {
  public static LogChannel Comment;
  public static readonly Dictionary<BlueprintGuid,BlueprintScriptableObject> Owned=new();
  public static void AddComponent(this BlueprintScriptableObject bp,BlueprintComponent c){if(c.OwnerBlueprint!=null&&c.OwnerBlueprint!=bp)c=CopyGraph(c);var old=bp.ComponentsArray??Array.Empty<BlueprintComponent>();string stem=string.IsNullOrEmpty(c.name)?"Shield_"+c.GetType().Name:c.name;string name=stem;int n=0;while(old.Any(x=>x.name==name))name=stem+"_"+(++n);c.name=name;c.OwnerBlueprint=bp;bp.ComponentsArray=old.Concat(new[]{c}).ToArray();}
  sealed class Identity:IEqualityComparer<object>{public new bool Equals(object a,object b)=>ReferenceEquals(a,b);public int GetHashCode(object o)=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);}
  public static T CopyGraph<T>(T value)=>(T)Copy(value,new Dictionary<object,object>(new Identity()));
  static object Copy(object value,Dictionary<object,object> seen){if(value==null)return null;var type=value.GetType();if(type.IsPrimitive||type.IsEnum||value is string||value is decimal||value is Guid||value is DateTime||value is TimeSpan||value is Type||value is SimpleBlueprint||value is UnityEngine.Object||value is Delegate)return value;if(seen.TryGetValue(value,out var prior))return prior;if(value is Array array){var result=Array.CreateInstance(type.GetElementType(),array.Length);seen[value]=result;for(int i=0;i<array.Length;i++)result.SetValue(Copy(array.GetValue(i),seen),i);return result;}if(value is System.Collections.IList list&&type.IsGenericType){var result=(System.Collections.IList)Activator.CreateInstance(type);seen[value]=result;foreach(var x in list)result.Add(Copy(x,seen));return result;}var clone=AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(value,null);seen[value]=clone;for(var t=type;t!=null&&t!=typeof(object);t=t.BaseType)foreach(var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))if(!f.IsStatic)f.SetValue(clone,Copy(f.GetValue(value),seen));return clone;}
  public static void AddToCache(this BlueprintScriptableObject bp,string guid,string name){bp.AssetGuid=BlueprintGuid.Parse(guid);bp.name=Main.modName+"_"+name;bp.AddToCache();}
  public static void AddToCache(this BlueprintScriptableObject bp){
   var cache=ResourcesLibrary.BlueprintsCache;var existing=cache.Load(bp.AssetGuid);
   if(existing!=null&&existing!=bp)throw new InvalidOperationException("Duplicate Way of the Shield blueprint: "+bp.AssetGuid+" ("+existing.name+")");
   if(existing==null)cache.AddCachedBlueprint(bp.AssetGuid,bp);Owned[bp.AssetGuid]=bp;
  }
  public static bool RetrieveBlueprint<T>(string guid,out T blueprint,string name=null,string circumstances=null)where T:BlueprintScriptableObject{blueprint=ResourcesLibrary.TryGetBlueprint<T>(BlueprintGuid.Parse(guid));if(blueprint==null)Comment?.Warning("Missing native/optional blueprint: "+(name??guid));return blueprint!=null;}
  public static void AddFeatureToSelections(this BlueprintFeature feature,IEnumerable<(string guid,string BlueprintName)> selections,string circumstances=""){foreach(var pair in selections)if(RetrieveBlueprint<BlueprintFeatureSelection>(pair.guid,out var pool,pair.BlueprintName,circumstances)){var old=pool.m_AllFeatures??Array.Empty<BlueprintFeatureReference>();if(!old.Any(r=>r?.Guid==feature.AssetGuid))pool.m_AllFeatures=old.Concat(new[]{feature.ToReference<BlueprintFeatureReference>()}).ToArray();var visible=pool.m_Features??Array.Empty<BlueprintFeatureReference>();if(!visible.Any(r=>r?.Guid==feature.AssetGuid))pool.m_Features=visible.Concat(new[]{feature.ToReference<BlueprintFeatureReference>()}).ToArray();}}
  public static int IndexFinder(IEnumerable<CodeInstruction> instructions,CodeInstruction[] pattern,bool before=false){var code=instructions.ToArray();for(int i=0;i<=code.Length-pattern.Length;i++){bool same=true;for(int j=0;j<pattern.Length;j++)if(code[i+j].opcode!=pattern[j].opcode||pattern[j].operand!=null&&!Equals(code[i+j].operand,pattern[j].operand)){same=false;break;}if(same)return before?i:i+pattern.Length;}return -1;}
  public static bool Is(this CodeInstruction a,CodeInstruction b)=>a.opcode==b.opcode&&(b.operand==null||Equals(a.operand,b.operand));
  public static Sprite LoadIcon(string name,float PixelsPerUnit=100,int size=64){if(string.IsNullOrWhiteSpace(name))return null;var asm=typeof(Main).Assembly;string resource=asm.GetManifestResourceNames().FirstOrDefault(x=>x.EndsWith(".Icons."+name+".png",StringComparison.Ordinal));if(resource==null)throw new FileNotFoundException("Way of the Shield icon: "+name);using(var stream=asm.GetManifestResourceStream(resource))using(var data=new MemoryStream()){stream.CopyTo(data);var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(texture,data.ToArray()))throw new InvalidDataException(name);return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),PixelsPerUnit);}}
  public static class EnchantPoolExtensions {public const EnchantPoolType DivineShield=(EnchantPoolType)20;}
  public sealed class BoolSettingCheckerTemplate:TextTemplate {public override int MinParamenters=>2;public override int MaxParamenters=>3;public override string Generate(bool capitalized,List<string> parameters)=>parameters.Count<2?"":Settings.Enabled(parameters[0])?parameters[1]:parameters.Count>2?parameters[2]:"";}
  [HarmonyPatch]public static class BonusTypeExtenstions {public static BonusType GetBonusType(int value)=>(BonusType)value;[HarmonyPatch(typeof(BonusSourceStrings),nameof(BonusSourceStrings.GetText))][HarmonyPrefix]public static bool Name(BonusType bonusType,ref string __result){if((int)bonusType!=160)return true;__result="Non-proficient weapon";return false;}}
  [HarmonyPatch]public static class ModifierDescriptorExtension {
   public const ModifierDescriptor NotProficientWithArmor=(ModifierDescriptor)81,NotProficientWithShield=(ModifierDescriptor)82;
   // Extend existing comparer state, preserving native/other-mod ordering.
   // Never replace its complete list or apply the upstream soft-cover change.
   public static void EnsureOrder(ModifierDescriptorComparer instance){if(instance==null)return;var field=AccessTools.Field(typeof(ModifierDescriptorComparer),"m_Order");var original=(int[])field.GetValue(instance);int length=Math.Max(original.Length,83);var order=Enumerable.Repeat(-1,length).ToArray();Array.Copy(original,order,original.Length);var sorted=(ModifierDescriptorComparer.SortedValues??Array.Empty<ModifierDescriptor>()).ToList();foreach(var value in new[]{NotProficientWithArmor,NotProficientWithShield}){int i=sorted.IndexOf(value);if(i<0){i=sorted.Count;sorted.Add(value);}order[(int)value]=i;}field.SetValue(instance,order);ModifierDescriptorComparer.SortedValues=sorted.ToArray();}
   [HarmonyPatch(typeof(ModifierDescriptorComparer),MethodType.Constructor)][HarmonyPostfix]public static void Created(ModifierDescriptorComparer __instance)=>EnsureOrder(__instance);
   [HarmonyPatch(typeof(AbilityModifiersStrings),nameof(AbilityModifiersStrings.GetName))][HarmonyPrefix]public static bool Name(ModifierDescriptor __0,ref string __result){if(__0==NotProficientWithArmor){__result="Non-proficient armor";return false;}if(__0==NotProficientWithShield){__result="Non-proficient shield";return false;}return true;}
  }
 }
}
