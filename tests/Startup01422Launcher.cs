using System;
using System.IO;
using System.Reflection;
internal static class Startup01422Launcher {
 static int Main(string[] args) {
  if(args.Length<3)return 2;
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
   var name=new AssemblyName(e.Name).Name;
   if(name=="Assembly-CSharp"&&args.Length>5)return Assembly.LoadFrom(args[5]);
   foreach(var dir in new[]{args[0],Path.GetDirectoryName(args[1]),args.Length>4?args[4]:null,args.Length>3&&args[3]!="old"?Path.GetDirectoryName(args[3]):null}) {if(string.IsNullOrEmpty(dir))continue;var path=Path.Combine(dir,name+".dll");if(File.Exists(path))return Assembly.LoadFrom(path);}return null;
  };
  try {typeof(Startup01422Launcher).GetMethod("Run",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{args});return 0;}
  catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
 static void Run(string[] args) {
  RequiredHarmonyRuntime.Verify(Path.Combine(args[0],"0Harmony.dll"));
  var result=args.Length>6&&args[6]=="medium-only"?new Newtonsoft.Json.Linq.JObject():Feedback01422StartupTests.Run(Assembly.LoadFrom(args[1]),args.Length>3&&args[3]=="old");
  if(args.Length>3&&args[3]!="old")result["mediumListContract"]=Feedback01423MediumTests.Run(Assembly.LoadFrom(args[3]));
  File.WriteAllText(args[2],result.ToString());Console.WriteLine(result.ToString());
 }
}
