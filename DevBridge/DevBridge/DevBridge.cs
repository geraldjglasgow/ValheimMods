using BepInEx;
using BepInEx.Configuration;
using DevBridge.Logs;
using DevBridge.Routes;
using DevBridge.Server;
using DevBridge.World;
using HarmonyLib;
using UnityEngine;

namespace DevBridge
{
    /// <summary>
    /// Dev-only plugin: a localhost HTTP endpoint that lets a test agent see and drive the running game
    /// (screenshots, UI tree, clicks, keys, console, log, reflection, ZDOs). Never shipped to players.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class DevBridgePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.DevBridge";
        public const string PluginName = "DevBridge";
        public const string PluginVersion = "0.1.0";

        internal static DevBridgePlugin Instance { get; private set; }

        internal int Port => server?.Port ?? 0;

        private HttpBridge server;

        private void Awake()
        {
            Instance = this;
            ConfigEntry<int> port = Config.Bind("Server", "Port", 7780,
                "First localhost port to listen on. When it is taken (a second game or a server on this machine) the next nine are tried.");
            Application.runInBackground = true;
            LogCapture.Install();
            new Harmony(PluginGuid).PatchAll(typeof(DevBridgePlugin).Assembly);
            server = new HttpBridge(RouteTable.Build(), Logger);
            server.Start(port.Value);
        }

        private void Start() => ZdoNames.Prepare();

        private void Update()
        {
            if (!Application.runInBackground) Application.runInBackground = true;
            MainThread.Drain();
        }

        private void OnDestroy() => server?.Stop();
    }
}
