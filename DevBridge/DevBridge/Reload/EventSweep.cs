using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace DevBridge.Reload
{
    /// <summary>
    /// The old copy's handlers on what outlives it: static events of the game, Unity, BepInEx and the other plugins
    /// (SceneManager.sceneLoaded, Application.logMessageReceived, another mod's API events), the other plugins' config
    /// files and entries (a mod reading another's settings through the chainloader), and BepInEx log listeners. Taken
    /// out, or they would keep running old code beside the new copy's. Only types without a static constructor are read,
    /// plus a few Unity types the game has long since initialised, so the sweep never starts a foreign type early.
    /// </summary>
    internal static class EventSweep
    {
        private static readonly HashSet<string> Scanned = new HashSet<string>
        {
            "assembly_valheim", "assembly_utils", "assembly_guiutils", "UnityEngine.CoreModule", "UnityEngine.UIModule",
            "UnityEngine.UI", "Unity.InputSystem", "BepInEx",
        };

        private static readonly HashSet<string> Initialised = new HashSet<string>
        {
            "UnityEngine.SceneManagement.SceneManager", "UnityEngine.Application", "UnityEngine.Camera", "UnityEngine.Canvas",
        };

        internal static void Run(Assembly old, Report report)
        {
            var names = new List<string>();
            foreach (Type type in Assemblies(old).SelectMany(Types)) Statics(type, old, names);
            Configs(old, names);
            report.Count("event handlers", names.Count);
            if (names.Count > 0) report.Leave("event handlers removed from " + Report.Tally(names));
            List<ILogListener> listeners = Logger.Listeners.Where(l => l != null && l.GetType().Assembly == old).ToList();
            foreach (ILogListener listener in listeners) Logger.Listeners.Remove(listener);
            report.Count("log listeners", listeners.Count);
        }

        private static IEnumerable<Assembly> Assemblies(Assembly old)
        {
            HashSet<Assembly> plugins = new HashSet<Assembly>(Others(old).Select(p => p.Instance.GetType().Assembly));
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => a != old && a != typeof(DevBridgePlugin).Assembly
                && !ReloadHistory.IsSuperseded(a) && (Scanned.Contains(a.GetName().Name) || plugins.Contains(a)));
        }

        private static List<PluginInfo> Others(Assembly old) => Chainloader.PluginInfos.Values
            .Where(p => !ReferenceEquals(p.Instance, null) && p.Instance.GetType().Assembly != old).ToList();

        private static IEnumerable<Type> Types(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException partial) { return partial.Types.Where(t => t != null); }
            catch (Exception) { return Enumerable.Empty<Type>(); }
        }

        /// <summary>The type's static delegate fields; a type that does not resolve (a missing reference) is skipped.</summary>
        private static void Statics(Type type, Assembly old, List<string> names)
        {
            try
            {
                if (type.ContainsGenericParameters || (type.TypeInitializer != null && !Initialised.Contains(type.FullName))) return;
                foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsInitOnly && !field.IsLiteral && typeof(Delegate).IsAssignableFrom(field.FieldType)) Strip(field, null, old, names);
            }
            catch (Exception)
            {
                // nothing of the old copy can hang on a type the runtime cannot load
            }
        }

        /// <summary>SettingChanged and ConfigReloaded on every other plugin's ConfigFile and on each of its entries.</summary>
        private static void Configs(Assembly old, List<string> names)
        {
            foreach (PluginInfo other in Others(old))
            {
                ConfigFile file = other.Instance.Config;
                foreach (FieldInfo field in OldCode.DelegateFields(file.GetType())) Strip(field, file, old, names);
                foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in file)
                    foreach (FieldInfo field in OldCode.DelegateFields(pair.Value.GetType())) Strip(field, pair.Value, old, names);
            }
        }

        /// <summary>Takes the old copy's handlers out of one delegate field (a static one when owner is null).</summary>
        private static void Strip(FieldInfo field, object owner, Assembly old, List<string> names)
        {
            try
            {
                Delegate kept = OldCode.Without(field.GetValue(owner) as Delegate, old, out int removed);
                if (removed == 0) return;
                field.SetValue(owner, kept);
                string where = owner is ConfigEntryBase entry ? $"{entry.Definition}.{field.Name}" : $"{field.DeclaringType?.Name}.{field.Name}";
                for (int i = 0; i < removed; i++) names.Add(where);
            }
            catch (Exception)
            {
                // a field reflection cannot read or write is left as it is
            }
        }
    }
}
