using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Localization;
using Kingmaker.Modding;

namespace CallOfTheWild.Wrath.Prototype
{
    // Optional, reflected API: the feat assembly must still load without ModMenu/UMM.
    internal sealed class ModMenuIntegration
    {
        private const string Root = "cotw-wrath-prototype.menu";
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private bool attempted;
        private bool absenceReported;
        private ModuleStatus[] currentModules;
        private bool failed;

        internal static Assembly FindAssembly() => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == "ModMenu");

        internal void RefreshLocalization()
        {
            if (LocalizationManager.CurrentPack == null) return;
            foreach (var entry in strings)
                LocalizationManager.CurrentPack.PutString(entry.Key, entry.Value);
        }

        private LocalizedString Text(string suffix, string text)
        {
            var key = Root + "." + suffix;
            strings[key] = text;
            LocalizationManager.CurrentPack.PutString(key, text);
            return new LocalizedString { Key = key };
        }

        // Called only on the main thread after blueprint registration has finished.
        // Missing dependencies can arrive later; retry when the settings window opens.
        // An API failure is terminal for this session to avoid duplicating partial menus.
        internal void TryRegister(Assembly assembly, OwlcatModification mod, string version,
            ModuleStatus[] modules, Action<string> log, Action<string> error)
        {
            if (failed) return;
            try
            {
                if (modules == null || modules.Any(m => m?.Definition == null) ||
                    modules.Select(m => m.Definition.Name).Distinct().Count() != modules.Length)
                    throw new ArgumentException("Invalid module status snapshot.");
                currentModules = modules;
                for (int i = 0; i < modules.Length; i++) {
                    var key = Root + ".module-" + i + ".status";
                    if (strings.ContainsKey(key)) strings[key] = modules[i].State;
                }
                RefreshLocalization();
                if (attempted || LocalizationManager.CurrentPack == null) return;
                if (assembly == null)
                {
                    if (!absenceReported)
                    {
                        absenceReported = true;
                        Notify(log, "ModMenu not loaded; feats remain active. Install and enable ModMenu to show the status page.");
                    }
                    return;
                }
                attempted = true;
                var api = new Api(assembly);
                var title = Text("title", "Call of the Wild - Wrath Prototype");
                var description = Text("description", "Registration status for the current game session. Click a status to write details to the game log. No gameplay settings are changed here.");
                var builder = api.New.Invoke(null, new object[] { Root, title });
                api.SetMod.Invoke(builder, new object[] { mod, true, false });
                api.SetName.Invoke(builder, new object[] { title });
                api.SetDescription.Invoke(builder, new object[] { description });
                AddRow(api, builder, "version", "Version " + version, "Write log",
                    "Write current registration results and provider details to the game log. Restart after changing installed mods.",
                    () => Notify(log, "CotW Wrath " + version + "\n" + string.Join("\n", currentModules.Select(m => m.Definition.Name + ": " + m.State))));
                for (var i = 0; i < modules.Length; i++)
                {
                    var definition = modules[i].Definition;
                    var index = i;
                    AddRow(api, builder, "module-" + i, definition.Name, modules[i].State,
                        definition.Detail + "\nIf registration failed, check the startup log for the exception.",
                        () => Notify(log, definition.Name + ": " + currentModules[index].State + "\n" + definition.Detail));
                }
                api.AddSettings.Invoke(null, new[] { builder });
                Notify(log, "Registered: ModMenu status page (ModMenu " + assembly.GetName().Version + ")");
            }
            catch (Exception exception)
            {
                failed = true;
                attempted = true;
                Notify(error, "ModMenu integration failed; feat registration is unaffected.\n" +
                    (exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception));
            }
        }

        // Optional diagnostics must not escape into cache, locale or settings hooks.
        private static void Notify(Action<string> callback, string message)
        { try { callback?.Invoke(message); } catch { } }

        private void AddRow(Api api, object builder, string suffix, string label, string buttonLabel,
            string detail, Action onClick)
        {
            var button = api.NewButton.Invoke(null, new object[] {
                Text(suffix + ".label", label), Text(suffix + ".status", buttonLabel), onClick });
            api.LongDescription.Invoke(button, new object[] { Text(suffix + ".detail", detail) });
            api.AddButton.Invoke(builder, new[] { button });
        }

        // Resolve every required public signature before changing the ModMenu registry.
        internal sealed class Api
        {
            internal readonly MethodInfo New, SetMod, SetName, SetDescription, NewButton,
                LongDescription, AddButton, AddSettings;
            internal Api(Assembly assembly)
            {
                var builder = assembly.GetType("ModMenu.Settings.SettingsBuilder", true);
                var button = assembly.GetType("ModMenu.Settings.Button", true);
                var menu = assembly.GetType("ModMenu.ModMenu", true);
                New = Required(builder, "New", true, typeof(string), typeof(LocalizedString));
                SetMod = Required(builder, "SetMod", false, typeof(OwlcatModification), typeof(bool), typeof(bool));
                SetName = Required(builder, "SetModName", false, typeof(LocalizedString));
                SetDescription = Required(builder, "SetModDescription", false, typeof(LocalizedString));
                NewButton = Required(button, "New", true, typeof(LocalizedString), typeof(LocalizedString), typeof(Action));
                LongDescription = Required(button, "WithLongDescription", false, typeof(LocalizedString));
                AddButton = Required(builder, "AddButton", false, button);
                AddSettings = Required(menu, "AddSettings", true, builder);
            }
            private static MethodInfo Required(Type type, string name, bool isStatic, params Type[] parameters)
                => type.GetMethod(name, BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance),
                    null, parameters, null) ?? throw new MissingMethodException(type.FullName, name);
        }
    }
}
