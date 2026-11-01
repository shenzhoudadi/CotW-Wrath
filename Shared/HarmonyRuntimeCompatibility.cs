using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
namespace CotW.Runtime {
 // Compile against the game's 2.0.4 API. At runtime Unity/UMM can bind the
 // same assembly identity to its already-loaded Harmony; never load a second
 // copy or replace either file. Validate the API actually used by this module.
 internal static class HarmonyRuntimeCompatibility {
  internal static void Verify()=>Verify(typeof(Harmony).Assembly);
  internal static void Verify(Assembly runtime) {
   var name=runtime?.GetName();
   if(name?.Name!="0Harmony"||name.Version==null||name.Version.Major!=2||name.Version<new Version(2,0,4,0))throw new InvalidOperationException("Unsupported loaded Harmony runtime: "+name);
   Type Required(string type)=>runtime.GetType("HarmonyLib."+type,false)??throw new InvalidOperationException("Loaded Harmony is missing "+type);
   var harmony=Required("Harmony");var method=Required("HarmonyMethod");var instruction=Required("CodeInstruction");var processor=Required("PatchClassProcessor");
   if(harmony.GetConstructor(new[]{typeof(string)})==null||method.GetConstructor(new[]{typeof(MethodInfo)})==null||instruction.GetConstructor(new[]{typeof(OpCode),typeof(object)})==null||instruction.GetField("opcode")?.FieldType!=typeof(OpCode)||instruction.GetField("operand")?.FieldType!=typeof(object)||harmony.GetMethod("CreateClassProcessor",new[]{typeof(Type)})?.ReturnType!=processor||processor.GetMethod("Patch",Type.EmptyTypes)==null||harmony.GetMethod("UnpatchAll",new[]{typeof(string)})==null||harmony.GetMethod("Patch",new[]{typeof(MethodBase),method,method,method,method})==null)
    throw new InvalidOperationException("Loaded Harmony API is incompatible: "+runtime.FullName);
  }
 }
}
