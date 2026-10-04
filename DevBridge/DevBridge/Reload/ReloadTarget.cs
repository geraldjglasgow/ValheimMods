using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using DevBridge.Server;

namespace DevBridge.Reload
{
    /// <summary>
    /// The plugins one reload replaces: every plugin of the running assembly that holds the named one (one DLL may
    /// hold several, they go and come back together), in the order BepInEx started them, and the DLL to load.
    /// </summary>
    internal sealed class ReloadTarget
    {
        internal Assembly Old;
        internal List<PluginInfo> Plugins;
        internal string File;

        internal string Guid => Plugins[0].Metadata.GUID;

        internal string Names => string.Join(", ", Plugins.Select(p => p.Metadata.Name));

        internal static ReloadTarget Find(string mod, string file)
        {
            PluginInfo info = Lookup(mod);
            if (ReferenceEquals(info.Instance, null))
                throw new BridgeException($"{info.Metadata.Name} is not running (it failed to load); restart the game");
            Assembly old = info.Instance.GetType().Assembly;
            if (old == typeof(DevBridgePlugin).Assembly) throw new BridgeException("DevBridge cannot reload itself; restart the game");
            string path = Fmt.WindowsPath(file ?? info.Location ?? "");
            if (!System.IO.File.Exists(path)) throw new BridgeException($"no DLL at {path}; give file=<path to the built DLL>");
            return new ReloadTarget { Old = old, Plugins = Siblings(old), File = Path.GetFullPath(path) };
        }

        /// <summary>A running plugin by GUID, name or DLL name, ignoring case and spaces.</summary>
        internal static PluginInfo Lookup(string mod)
        {
            string wanted = Plain(mod);
            List<PluginInfo> found = Chainloader.PluginInfos.Values.Where(p => Matches(p, wanted)).ToList();
            if (found.Count == 1) return found[0];
            if (found.Count > 1) throw new BridgeException($"{mod} matches {string.Join(", ", found.Select(p => p.Metadata.GUID))}: give the GUID");
            throw new BridgeException($"no plugin {mod}; /reload?list=1 lists them");
        }

        private static bool Matches(PluginInfo info, string wanted) =>
            Plain(info.Metadata.GUID) == wanted || Plain(info.Metadata.Name) == wanted || Plain(OriginalName(info)) == wanted;

        private static string Plain(string text) => (text ?? "").Replace(" ", "").ToLowerInvariant();

        /// <summary>The DLL's name without the -reload-N a reload gives the assembly.</summary>
        internal static string OriginalName(PluginInfo info) => Path.GetFileNameWithoutExtension(info.Location ?? "");

        /// <summary>The running plugins whose component type lives in the assembly, in their order on the manager object.</summary>
        private static List<PluginInfo> Siblings(Assembly old)
        {
            List<BaseUnityPlugin> order = Chainloader.ManagerObject.GetComponents<BaseUnityPlugin>().ToList();
            return Chainloader.PluginInfos.Values
                .Where(p => !ReferenceEquals(p.Instance, null) && p.Instance.GetType().Assembly == old)
                .OrderBy(p => order.IndexOf(p.Instance)).ToList();
        }

        /// <summary>Plugins that can be reloaded: running, not DevBridge.</summary>
        internal static IEnumerable<PluginInfo> Reloadable() => Chainloader.PluginInfos.Values
            .Where(p => !ReferenceEquals(p.Instance, null) && p.Instance.GetType().Assembly != typeof(DevBridgePlugin).Assembly)
            .OrderBy(p => p.Metadata.Name, StringComparer.OrdinalIgnoreCase);
    }
}
