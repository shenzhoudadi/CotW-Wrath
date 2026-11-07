using System;
using System.IO;
using System.Security.Cryptography;
using HarmonyLib;

internal static class RequiredHarmonyRuntime {
 internal const string RequiredSha256="DF043F05C6E43602FA8E8F38F52A3A2CF962AAB7A65A9D85573EA25F01A63E80";
 internal static void Verify(string expectedPath){
  var runtime=typeof(Harmony).Assembly;
  using(var hash=SHA256.Create()){
   var actual=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(runtime.Location))).Replace("-","");
   var expected=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(expectedPath))).Replace("-","");
   if(runtime.GetName().Name!="0Harmony"||runtime.GetName().Version!=new Version(2,3,6,0)||actual!=RequiredSha256||expected!=RequiredSha256)
    throw new Exception("Regression requires actual game Harmony 2.3.6 bytes; test-local DLL shadowing is forbidden.");
   Console.WriteLine("Required Harmony runtime: "+runtime.FullName+" SHA256="+actual+" from "+runtime.Location);
  }
 }
}
