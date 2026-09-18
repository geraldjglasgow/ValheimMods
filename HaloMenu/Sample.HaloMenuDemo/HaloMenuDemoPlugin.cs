using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HaloMenu.API;
using UnityEngine;

namespace Sample.HaloMenuDemo
{
    /// <summary>
    /// Not a real mod: a compile-time and load-time proof that HaloMenu.API.dll is a self-contained contract.
    /// This project's csproj references HaloMenu.API.csproj only - never HaloMenu.csproj - so it compiles whether
    /// or not HaloMenu itself is checked out, and (per the plugin dependency below) it loads and runs correctly
    /// whether or not HaloMenu.dll is present in BepInEx/plugins. Level 1 shows adding entries to the shared
    /// default ring; Level 2 shows owning an independent ring with its own hotkey.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(HaloMenuPluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class HaloMenuDemoPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.Sample.HaloMenuDemo";
        public const string PluginName = "HaloMenu Demo";
        public const string PluginVersion = "0.1.0";
        private const string HaloMenuPluginGuid = "com.HaloMenu";

        private static ManualLogSource log;
        private ConfigEntry<KeyboardShortcut> buildRingHotkey;

        private void Awake()
        {
            log = Logger;
            RegisterDefaultRingEntries();
            CreateOwnRing();
        }

        // Level 1: add entries to the shared default ring.
        private void RegisterDefaultRingEntries()
        {
            HaloMenuAPI.Register(new RingEntry
            {
                Id = "sample.wave",
                Label = "Wave",
                Icon = DemoIcon.Create(new Color(0.3f, 0.6f, 1f)),
                Order = 100,
                IsVisible = () => Player.m_localPlayer != null,
                IsEnabled = () => Player.m_localPlayer != null && !Player.m_localPlayer.IsEncumbered(),
                OnSelect = () => log.LogInfo("Sample: Wave selected"),
            });
            HaloMenuAPI.Register(new RingEntry
            {
                Id = "sample.sit",
                Label = "Sit",
                Icon = DemoIcon.Create(new Color(0.9f, 0.6f, 0.2f)),
                Order = 110,
                OnSelect = () => log.LogInfo("Sample: Sit selected"),
            });
        }

        // Level 2: an independent ring with its own hotkey and segment count.
        private void CreateOwnRing()
        {
            if (!HaloMenuAPI.IsAvailable)
            {
                log.LogInfo("HaloMenu is not loaded; skipping the demo build ring (this is the expected soft-dependency path).");
                return;
            }
            buildRingHotkey = Config.Bind("Demo", "Build Ring Hotkey", new KeyboardShortcut(KeyCode.LeftBracket),
                "Opens the sample build ring.");
            Ring ring = HaloMenuAPI.CreateRing("sample.buildmenu");
            if (ring == null)
                return;
            ring.Hotkey = buildRingHotkey;
            ring.SegmentCount = 6;
            ring.Add(new RingEntry { Id = "sample.build.wall", Label = "Wall", Icon = DemoIcon.Create(Color.gray), Order = 0 });
            ring.Add(new RingEntry { Id = "sample.build.floor", Label = "Floor", Icon = DemoIcon.Create(Color.gray), Order = 1 });
            ring.Add(new RingEntry { Id = "sample.build.roof", Label = "Roof", Icon = DemoIcon.Create(Color.gray), Order = 2 });
            ring.OnSelected += (r, entry) => log.LogInfo($"Sample build ring: {entry.Label} selected");
            ring.OnCancelled += r => log.LogInfo("Sample build ring: cancelled");
        }
    }
}
