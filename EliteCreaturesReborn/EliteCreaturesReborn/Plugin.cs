using BepInEx;
using ConfigReload;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn
{
    /// <summary>
    /// The plugin entry point. It binds the per-player display config, writes and loads the YAML rule file, wires the
    /// server lock through Charter, applies every patch, installs error attribution, and starts the two hot reloads -
    /// ConfigReload for the .cfg and the rule watcher for the YAML. All the behaviour lives in the patched classes.
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Log.Bind(Logger);
            Configuration.BindAll(Config);
            RuleFile.Initialise();
            ServerLock.Setup(Config, PluginInfo.Guid, PluginInfo.Name, PluginInfo.PluginVersion);
            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            ServerLock.Install(harmony);
            Guard.Install(harmony, Logger, typeof(Plugin).Assembly);
            ConfigReloader.Setup(Config, Logger);
            RuleReload.Attach();
            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.PluginVersion} ready.");
        }
    }
}
