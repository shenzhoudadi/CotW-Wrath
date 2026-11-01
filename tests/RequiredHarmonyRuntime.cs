using System;
using System.IO;
using System.Security.Cryptography;
using HarmonyLib;

internal static class RequiredHarmonyRuntime {
 internal const string RequiredSha256="DE50CD610EA22854C8930EC5433276713AEE1BB0ECD785D6B15A0661C2FA9FD5";
 internal static void Verify(string expectedPath){
  var runtime=typeof(Harmony).Assembly;
  using(var hash=SHA256.Create()){
   var actual=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(runtime.Location))).Replace("-","");
   var expected=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(expectedPath))).Replace("-","");
   if(runtime.GetName().Name!="0Harmony"||runtime.GetName().Version!=new Version(2,0,4,0)||actual!=RequiredSha256||expected!=RequiredSha256)
    throw new Exception("Regression requires actual game Harmony 2.0.4 bytes; test-local DLL shadowing is forbidden.");
   Console.WriteLine("Required Harmony runtime: "+runtime.FullName+" SHA256="+actual+" from "+runtime.Location);
  }
 }
}
