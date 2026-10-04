using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using DevBridge.Server;
using HarmonyLib;

namespace DevBridge.Reload
{
    /// <summary>
    /// Starts the new copy's plugins the way the chainloader does: its PluginInfo goes into Chainloader.PluginInfos
    /// first (BaseUnityPlugin's constructor takes its Info from there and makes Logger and Config from its metadata),
    /// then the component goes onto the BepInEx manager object, so Awake runs now and Start before its first Update.
    /// The PluginInfo keeps the original DLL path, so other mods reading PluginInfos see the plugins folder.
    /// </summary>
    internal static class PluginLoader
    {
        private static readonly MethodInfo SetInstance = AccessTools.PropertySetter(typeof(PluginInfo), nameof(PluginInfo.Instance));
        private static readonly MethodInfo SetLocation = AccessTools.PropertySetter(typeof(PluginInfo), nameof(PluginInfo.Location));
        private static readonly FieldInfo PluginList = AccessTools.Field(typeof(Chainloader), "_plugins");

        /// <summary>Refuses, before anything is torn down, a DLL that lacks one of the running plugins.</summary>
        internal static void Check(NewCopy copy, ReloadTarget target)
        {
            foreach (PluginInfo old in target.Plugins)
                if (!copy.Plugins.Exists(p => p.Metadata.GUID == old.Metadata.GUID))
                    throw new BridgeException($"the new DLL has no plugin {old.Metadata.GUID}; nothing was torn down");
        }

        internal static void Start(NewCopy copy, ReloadTarget target, Report report)
        {
            foreach (PluginInfo old in target.Plugins)
            {
                PluginInfo info = copy.Plugins.Find(p => p.Metadata.GUID == old.Metadata.GUID);
                SetLocation.Invoke(info, new object[] { old.Location });
                Chainloader.PluginInfos[info.Metadata.GUID] = info;
                var plugin = Chainloader.ManagerObject.AddComponent(copy.Types[info.Metadata.GUID]) as BaseUnityPlugin;
                if (!plugin)
                {
                    report.Leave($"{info.Metadata.Name} could not be added to the manager object");
                    continue;
                }
                SetInstance.Invoke(info, new object[] { plugin });
                Remember(plugin);
            }
            foreach (PluginInfo extra in copy.Plugins.FindAll(p => !target.Plugins.Exists(o => o.Metadata.GUID == p.Metadata.GUID)))
                report.Leave($"{extra.Metadata.Name} ({extra.Metadata.GUID}) is new in this DLL and was not started: restart the game for it");
        }

        // The obsolete Chainloader.Plugins list some mods still read; it drops destroyed entries by itself.
        private static void Remember(BaseUnityPlugin plugin)
        {
            if (!(PluginList?.GetValue(null) is List<BaseUnityPlugin> list)) return;
            lock (list) list.Add(plugin);
        }
    }
}
