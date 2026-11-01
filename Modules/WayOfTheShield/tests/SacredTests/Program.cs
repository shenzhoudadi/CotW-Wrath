using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
class SacredTests
{
    static int checks;
    static void Check(bool pass, string name) { checks++; if (!pass) throw new Exception(name); }
    static int Call(Type type, string method, params object[] values) => (int)type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, values);
    static void Main(string[] args)
    {
        string managed = Path.GetFullPath(args[0]), modulePath = Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
            string file = new AssemblyName(e.Name).Name + ".dll";
            string candidate = Path.Combine(managed, file);
            if (!File.Exists(candidate)) candidate = Path.Combine(Path.GetDirectoryName(modulePath), file);
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        };
        var harmony = Assembly.LoadFrom(Path.Combine(managed, "0Harmony.dll"));
        using (var hash = SHA256.Create())
            Check(harmony.GetName().Version.ToString() == "2.0.4.0" &&
                BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(harmony.Location))).Replace("-", "") ==
                "DE50CD610EA22854C8930EC5433276713AEE1BB0ECD785D6B15A0661C2FA9FD5", "Pinned game Harmony identity and SHA256");
        var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
        var module = Assembly.LoadFrom(modulePath);
        var pool = module.GetType("Way_of_the_shield.NewComponents.ContextActionShieldGeneralEnchantPool", true);
        int[][] cases = {
            new[] {0,0,5,0,0},new[]{0,1,5,1,1},new[]{0,6,5,5,5},new[]{0,6,6,6,6},
            new[]{2,1,5,1,3},new[]{2,4,5,3,5},new[]{4,6,6,2,6},new[]{5,6,5,0,5},
            new[]{6,6,6,0,6},new[]{2,-3,5,0,2},new[]{0,5,0,0,0},new[]{8,6,5,0,5}
        };
        foreach(var c in cases) {
            Check(Call(pool,"ComputeWeaponIncrease",c[0],c[1],c[2])==c[3], "Stacked weapon enhancement clamp");
            Check(Call(pool,"ComputeArmorTarget",c[0],c[1],c[2])==c[4], "Absolute armor enhancement target");
        }
        foreach(var c in new[]{new[]{6,5,0,1,1},new[]{5,3,0,1,2},new[]{5,5,0,1,0},new[]{5,5,1,0,5},new[]{6,0,0,0,6}}) {
            object[] values = {c[0],c[1],c[2],0};
            bool spent = (bool)pool.GetMethod("TrySpendProperty", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,values);
            Check(spent == (c[3] == 1), "Property selector price enforces reserve/zero-cost rejection");
            Check((int)values[3] == c[4], "Cost5/SR3/Fortification5 consume enhancement budget");
        }
        var holy = module.GetType("Way_of_the_shield.NewComponents.HolyShieldDynamicBonus", true);
        foreach(var c in new[]{new[]{5,0,5},new[]{5,2,3},new[]{2,4,0},new[]{0,4,0},new[]{6,3,3},new[]{3,-2,3}})
            Check(Call(holy,"ComputeTransferredShieldModifier",c[0],c[1])==c[2], "Holy Shield does not double enhancement/focus");
        var rankType = game.GetType("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig",true);
        var config = Activator.CreateInstance(rankType);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var progression = rankType.GetField("m_Progression", flags);
        progression.SetValue(config, Enum.Parse(progression.FieldType,"StartPlusDivStep"));
        var start = rankType.GetField("m_StartLevel",flags);
        var step = rankType.GetField("m_StepLevel",flags);
        var apply = rankType.GetMethod("ApplyProgression",flags);
        start.SetValue(config,5); step.SetValue(config,3);
        foreach(var c in new[]{new[]{5,1},new[]{7,1},new[]{8,2},new[]{11,3},new[]{14,4},new[]{17,5},new[]{20,6}})
            Check((int)apply.Invoke(config,new object[]{c[0]})==c[1],"Native divine bond progression");
        start.SetValue(config,0); step.SetValue(config,4);
        foreach(var c in new[]{new[]{1,1},new[]{3,1},new[]{4,2},new[]{8,3},new[]{12,4},new[]{16,5},new[]{20,6}})
            Check((int)apply.Invoke(config,new object[]{c[0]})==c[1],"Native Bastion AC progression");
        Console.WriteLine("PASS " + checks + " Sacred Shield budget/transfer/native progression checks.");
    }
}
