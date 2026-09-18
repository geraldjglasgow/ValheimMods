using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ConfigReload;
using HaloMenu.Api;
using HaloMenu.Config;
using HaloMenu.Runtime;
using HarmonyLib;
using PatchGuard;

namespace HaloMenu
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class HaloMenuPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.HaloMenu";
        public const string PluginName = "HaloMenu";
        public const string PluginVersion = "0.1.0";

        public static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            HaloMenuConfig config = HaloMenuConfig.Bind(Config);
            HaloLog.Source = Logger;
            HaloLog.Level = config.LogLevel;

            HaloMenuServiceImpl service = new HaloMenuServiceImpl(Config, config.DefaultRing);
            API.HaloMenuAPI.Provide(service);
            HaloMenuDriver driver = gameObject.AddComponent<HaloMenuDriver>();
            driver.Service = service;

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ConfigReloader.Setup(Config, Logger);

            HaloLog.Info($"Loading [{PluginName} {PluginVersion}]");
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }
    }
}
