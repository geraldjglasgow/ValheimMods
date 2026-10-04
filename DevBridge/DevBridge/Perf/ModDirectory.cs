using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace DevBridge.Perf
{
    /// <summary>
    /// Which mod a method belongs to: the plugin whose assembly declares it (a mod's merged libraries live in its own
    /// assembly, so they count as the mod), else the assembly's own name. DevBridge itself is never a mod here.
    /// </summary>
    internal sealed class ModDirectory
    {
        private readonly Dictionary<Assembly, List<PluginInfo>> plugins;
        private readonly Dictionary<Assembly, PerfMod> mods = new Dictionary<Assembly, PerfMod>();
        private readonly List<PerfMod> all = new List<PerfMod>();

        internal ModDirectory()
        {
            plugins = Chainloader.PluginInfos.Values
                .Where(p => p.Instance != null && p.Instance.GetType().Assembly != Own)
                .GroupBy(p => p.Instance.GetType().Assembly)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        internal static Assembly Own => typeof(ModDirectory).Assembly;

        internal IEnumerable<Assembly> PluginAssemblies => plugins.Keys;

        internal IReadOnlyList<PerfMod> All => all;

        internal PerfMod Of(Assembly assembly)
        {
            if (mods.TryGetValue(assembly, out PerfMod mod)) return mod;
            string name = assembly.GetName().Name;
            if (plugins.TryGetValue(assembly, out List<PluginInfo> infos))
                mod = new PerfMod(all.Count, string.Join(" + ", infos.Select(p => p.Metadata.Name)), infos.Select(p => p.Metadata.GUID).Append(name).ToArray());
            else mod = new PerfMod(all.Count, name, name);
            all.Add(mod);
            return mods[assembly] = mod;
        }

        /// <summary>True when there is no filter, or the filter is the mod's name, a plugin GUID or the assembly name.</summary>
        internal static bool Matches(PerfMod mod, string filter) =>
            filter == null || Same(mod.Name, filter) || mod.Aliases.Any(alias => Same(alias, filter));

        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
