using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Localization;
using Kingmaker.Modding;

namespace CallOfTheWild.Wrath.Prototype
{
    // Optional, reflected API: the feat assembly must still load without ModMenu/UMM.
    // The per-module registration status page was retired on user request
    // (2026-11-15). This page now carries only the universal spellbook
    // recognition toggle.
    internal sealed class ModMenuIntegration
    {
        private const string Root = "cotw-wrath-prototype.menu";
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private bool attempted;
        private bool absenceReported;
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
        // An API failure is terminal for this session to avoid duplicating partial menus.
        internal void TryRegister(Assembly assembly, OwlcatModification mod, string version,
            Action<string> log, Action<string> error)
        {
            if (failed) return;
            try
            {
                RefreshLocalization();
                if (attempted || LocalizationManager.CurrentPack == null) return;
                if (assembly == null)
                {
                    if (!absenceReported)
                    {
                        absenceReported = true;
                        Notify(log, "ModMenu not loaded; feats remain active. Install and enable ModMenu to show the spellbook toggle.");
                    }
                    return;
                }
                attempted = true;
                var api = new Api(assembly);
                var title = Text("title", "Call of the Wild - Wrath Prototype");
                var description = Text("description",
                    "超凡之弦与原创内容。此页只保留通用法术书识别开关；修改后需重启游戏生效。");
                var builder = api.New.Invoke(null, new object[] { Root, title });
                api.SetMod.Invoke(builder, new object[] { mod, true, false });
                api.SetName.Invoke(builder, new object[] { title });
                api.SetDescription.Invoke(builder, new object[] { description });

                var toggle = api.NewToggle.Invoke(null, new object[] {
                    Root + ".spellbook-recognition",
                    SpellbookRecognitionSettings.Enabled,
                    Text("spellbook",
                        "把其他模组添加的法术书加入所有施法进阶职业的施法职业选择列表（奥术诡术师、龙裔门徒、奥法骑士、博学者、秘术师、冬女巫）。关闭后这些额外选项不再生成。修改后需重启游戏生效。")
                });
                api.OnValueChanged.Invoke(toggle, new object[] { (Action<bool>)SpellbookRecognitionSettings.Set });
                api.AddToggle.Invoke(builder, new[] { toggle });

                api.AddSettings.Invoke(null, new[] { builder });
                Notify(log, "Registered: ModMenu settings (ModMenu " + assembly.GetName().Version + ")");
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

        // Resolve every required public signature before changing the ModMenu registry.
        internal sealed class Api
        {
            internal readonly MethodInfo New, SetMod, SetName, SetDescription,
                NewToggle, OnValueChanged, AddToggle, AddSettings;
            internal Api(Assembly assembly)
            {
                var builder = assembly.GetType("ModMenu.Settings.SettingsBuilder", true);
                var toggle = assembly.GetType("ModMenu.Settings.Toggle", true);
                var menu = assembly.GetType("ModMenu.ModMenu", true);
                New = Required(builder, "New", true, typeof(string), typeof(LocalizedString));
                SetMod = Required(builder, "SetMod", false, typeof(OwlcatModification), typeof(bool), typeof(bool));
                SetName = Required(builder, "SetModName", false, typeof(LocalizedString));
                SetDescription = Required(builder, "SetModDescription", false, typeof(LocalizedString));
                NewToggle = Required(toggle, "New", true, typeof(string), typeof(bool), typeof(LocalizedString));
                OnValueChanged = Required(toggle, "OnValueChanged", false, typeof(Action<bool>));
                AddToggle = Required(builder, "AddToggle", false, toggle);
                AddSettings = Required(menu, "AddSettings", true, builder);
            }
            private static MethodInfo Required(Type type, string name, bool isStatic, params Type[] parameters)
                => type.GetMethod(name, BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance),
                    null, parameters, null) ?? throw new MissingMethodException(type.FullName, name);
        }
    }
}
