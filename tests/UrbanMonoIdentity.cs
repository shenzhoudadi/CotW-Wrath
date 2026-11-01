using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;

internal static class UrbanMonoIdentity
{
    private static int Main(string[] args)
    {
        if (args.Length != 2) return 2;
        string managed=Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve+=(sender,e)=>{
            string path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
            return File.Exists(path)?Assembly.LoadFrom(path):null;
        };
        return Run(Path.GetFullPath(args[1]));
    }
    private static int Run(string path)
    {
        var assembly=Assembly.LoadFrom(path);const string prefix="CallOfTheWild.Wrath.Prototype.";
        var factory=assembly.GetType(prefix+"PsychicAssets",true);
        var bridge=assembly.GetType(prefix+"UrbanBloodrageBridge01463",true);
        var id=(BlueprintGuid)factory.GetMethod("Id",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"UrbanBloodrageStrengthBuff01463"});
        var own=bridge.GetMethod("Own",BindingFlags.NonPublic|BindingFlags.Static);
        bool positive=(bool)own.Invoke(null,new object[]{new BlueprintBuff{AssetGuid=id}});
        bool native=(bool)own.Invoke(null,new object[]{new BlueprintBuff{AssetGuid=BlueprintGuid.Parse("5eac31e457999334b98f98b60fc73b2f")}});
        bool foreign=(bool)own.Invoke(null,new object[]{new BlueprintBuff{AssetGuid=BlueprintGuid.Parse("00000000000000000000000000001234")}});
        bool empty=(bool)own.Invoke(null,new object[]{null});
        Console.WriteLine("runtime="+(Type.GetType("Mono.Runtime")!=null?"Mono":"CLR")+" own="+positive+" native="+native+" foreign="+foreign+" null="+empty+" typedEqual="+id.Equals(id)+" arrayContains="+new[]{id}.Contains(id));
        return positive&&!native&&!foreign&&!empty?0:1;
    }
}
