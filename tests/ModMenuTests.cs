using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.Modding;

internal static class ModMenuTests
{
    private static string managed, modPath, menuPath, ummPath;
    private static int assertions;
    private static readonly List<string> logs = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static Type entry;
    private static Dictionary<string, string> states;
    private static string[] names;
    private static string version;
    private static Array Snapshot() => (Array)entry.GetMethod("GetModuleStatuses", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    private static string Name(object row) => (string)row.GetType().GetField("Name", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(row);

    private static int Main(string[] args)
    {
        if (args.Length != 5) return 2;
        managed = Path.GetFullPath(args[0]); modPath = Path.GetFullPath(args[1]);
        menuPath = Path.GetFullPath(args[2]); ummPath = Path.GetFullPath(args[3]);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
            var name = new AssemblyName(e.Name).Name;
            if (name == "UnityModManager") return Assembly.LoadFrom(ummPath);
            foreach (var dir in new[] { managed, Path.GetDirectoryName(modPath), Path.GetDirectoryName(menuPath) })
            {
                var path = Path.Combine(dir, name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try {
            Run();
            File.WriteAllText(args[4], "{\"assertionsPassed\":" + assertions +
                ",\"moduleCount\":" + names.Length + ",\"moduleNames\":[" + string.Join(",", names.Select(n => "\"" + n.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")) + "]" +
                ",\"scope\":\"Production adapter with fake public ModMenu API; real caller module catalogue and adapter; real 2.0.14 API signature check; no Unity or game menu rendering\"}");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        assertions++;
    }
    private static object Fresh(Type type)
    {
        ModMenu.ModMenu.Reset(); logs.Clear(); errors.Clear();
        return Activator.CreateInstance(type, true);
    }
    private static void Attempt(object adapter, Assembly api)
    {
        entry.GetMethod("RegisterMenu", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { adapter, api, null, version,
                new Action<string>(logs.Add), new Action<string>(errors.Add) });
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var mod = Assembly.LoadFrom(modPath);
        Check(!mod.GetReferencedAssemblies().Any(a => a.Name == "ModMenu" || a.Name == "UnityModManager"), "Optional dependency became a hard reference");
        var type = mod.GetType("CallOfTheWild.Wrath.Prototype.ModMenuIntegration", true);
        entry = mod.GetType("CallOfTheWild.Wrath.Prototype.ModEntry", true);
        version = mod.GetName().Version.ToString(3);
        states = (Dictionary<string, string>)entry.GetField("moduleStates", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var catalogue = mod.GetType("CallOfTheWild.Wrath.Prototype.ModuleCatalog", true);
        names = ((System.Collections.IEnumerable)catalogue.GetField("Definitions", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Cast<object>().Select(Name).ToArray();
        Check(names.Length > 0 && names.Distinct().Count() == names.Length, "Unique nonempty module catalogue");
        Check(names.Length == states.Count && names.All(states.ContainsKey), "Actual caller state dictionary matches catalogue");
        Check(Snapshot().Length == names.Length, "Actual caller snapshot matches catalogue");
        foreach (var name in names) states[name] = "Registered";
        states["Extended Bane"] = "Failed"; states["Brawler combat feat integration"] = "Not detected";
        var fake = Assembly.GetExecutingAssembly();
        var adapter = Fresh(type);
        LocalizationManager.CurrentPack = null;
        Attempt(adapter, fake);
        Check(ModMenu.ModMenu.Calls == 0 && errors.Count == 0, "Menu built before localization");
        LocalizationManager.CurrentPack = new LocalizationPack();
        Attempt(adapter, null); Attempt(adapter, null);
        Check(logs.Count == 1 && errors.Count == 0 && ModMenu.ModMenu.Calls == 0, "Missing dependency handling/log flood");
        Attempt(adapter, fake);
        Check(ModMenu.ModMenu.Calls == 1 && errors.Count == 0, "Late dependency not registered");
        var page = ModMenu.ModMenu.Page;
        Check(page.SaveDependency && !page.AllowDisabling, "Unsafe disable/save dependency flags");
        Check(page.Key == "cotw-wrath-prototype.menu", "Wrong stable page identity");
        Check(page.Rows.Count == names.Length + 1, "Missing version or status rows");
        Check(Text(page.Name) == "Call of the Wild - Wrath Prototype", "Missing English title");
        Check(Text(page.Rows[0].Label) == "Version " + version, "Wrong version");
        for (var i = 0; i < names.Length; i++)
            Check(Text(page.Rows[i + 1].Caption) == states[names[i]], "Incorrect module state");
        page.Rows[0].Click();
        Check(logs.Last().Contains("Extended Bane: Failed") && logs.Last().Contains("Not detected"), "Diagnostics suppress failure or absent Brawler");
        for (var i = 1; i < page.Rows.Count; i++) {
            page.Rows[i].Click();
            Check(logs.Last().StartsWith(Text(page.Rows[i].Label) + ":"), "Button closure writes wrong module");
        }
        Attempt(adapter, fake); Attempt(adapter, fake);
        Check(ModMenu.ModMenu.Calls == 1, "Repeated settings open creates duplicate page");
        LocalizationManager.CurrentPack = new LocalizationPack { Locale = Locale.zhCN };
        Attempt(adapter, fake);
        Check(Text(page.Name) == "Call of the Wild - Wrath Prototype" && Text(page.Rows[2].Caption) == "Failed", "Locale change loses English strings");
        Check(ModMenu.ModMenu.Calls == 1, "Locale change creates duplicate page");
        states["Extended Bane"] = "Registered"; states["Brawler combat feat integration"] = "Registered";
        Attempt(adapter, fake);
        Check(Text(page.Rows[2].Caption) == "Registered" && Text(page.Rows[4].Caption) == "Registered", "Late registration leaves stale status");
        page.Rows[0].Click();
        Check(!logs.Last().Contains("Failed") && !logs.Last().Contains("Not detected"), "Write log captured stale states");
        Check(ModMenu.ModMenu.Calls == 1, "Status refresh duplicated the page");
        adapter = Fresh(type);
        Attempt(adapter, typeof(string).Assembly); Attempt(adapter, typeof(string).Assembly);
        Check(errors.Count == 1 && ModMenu.ModMenu.Calls == 0, "Incompatible API not isolated or floods logs");
        adapter = Fresh(type); ModMenu.ModMenu.ThrowOnAdd = true;
        Attempt(adapter, fake); Attempt(adapter, fake);
        Check(errors.Count == 1 && ModMenu.ModMenu.Calls == 1, "Partial registration retried and duplicated");
        Check(errors[0].Contains("feat registration is unaffected") && errors[0].Contains("simulated menu failure"), "Failure diagnostic lost");
        Check(page.Rows.Skip(1).Select(r => Text(r.Label)).SequenceEqual(names), "Names/order originate from real caller catalogue");
        Check(page.Rows.Skip(1).Any(r => Text(r.Label) == "Way of the Shield"), "Bundled shield status is visible");
        // A broken optional API, including a broken error callback, cannot escape.
        adapter = Fresh(type);
        entry.GetMethod("RegisterMenu", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null,
            new object[] { adapter, typeof(string).Assembly, null, version,
                new Action<string>(_ => { throw new Exception("logger failure"); }),
                new Action<string>(_ => { throw new Exception("error logger failure"); }) });
        Attempt(adapter, fake);
        Check(ModMenu.ModMenu.Calls == 0, "Failed optional integration remains isolated and terminal");
        // Verify the exact reflection contract against the downloaded release, without executing Unity UI code.
        var real = Assembly.LoadFrom(menuPath);
        var contract = type.GetNestedType("Api", BindingFlags.NonPublic);
        Activator.CreateInstance(contract, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { real }, null);
        Check(real.GetName().Version == new Version(2, 0, 14, 0), "Unexpected reference release");
        Console.WriteLine("PASS: " + assertions + " ModMenu adapter assertions; actual 2.0.14 public API signatures resolve. No Unity/menu rendering test.");
    }
    private static string Text(LocalizedString text) => LocalizationManager.CurrentPack.GetText(text.Key);
}

// Fixture only: these types are in the test EXE, never the shipped mod DLL.
namespace ModMenu
{
    public static class ModMenu
    {
        public static int Calls;
        public static bool ThrowOnAdd;
        public static Settings.SettingsBuilder Page;
        public static void Reset() { Calls = 0; ThrowOnAdd = false; Page = null; }
        public static void AddSettings(Settings.SettingsBuilder page) {
            Calls++; Page = page;
            if (ThrowOnAdd) throw new InvalidOperationException("simulated menu failure");
        }
        public static void AddSettings(string wrongOverload) { throw new Exception("Wrong overload"); }
    }
}
namespace ModMenu.Settings
{
    public class SettingsBuilder
    {
        public string Key;
        public LocalizedString Name, Description;
        public bool SaveDependency, AllowDisabling;
        public readonly List<Button> Rows = new List<Button>();
        public static SettingsBuilder New(string key, LocalizedString title) => new SettingsBuilder { Key = key, Name = title };
        public SettingsBuilder SetMod(OwlcatModification mod, bool causesSaveDependency = true, bool allowDisabling = false) {
            SaveDependency = causesSaveDependency; AllowDisabling = allowDisabling; return this;
        }
        public SettingsBuilder SetModName(LocalizedString name) { Name = name; return this; }
        public SettingsBuilder SetModDescription(LocalizedString description) { Description = description; return this; }
        public SettingsBuilder AddButton(Button button) { Rows.Add(button); return this; }
    }
    public class Button
    {
        public LocalizedString Label, Caption, Detail;
        public Action Click;
        public static Button New(LocalizedString description, LocalizedString buttonText, Action onClick)
            => new Button { Label = description, Caption = buttonText, Click = onClick };
        public Button WithLongDescription(LocalizedString text) { Detail = text; return this; }
    }
}

