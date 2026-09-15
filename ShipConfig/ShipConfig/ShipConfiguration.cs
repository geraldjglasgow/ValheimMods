using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using SyncedConfig;

namespace ShipConfig
{
    /// <summary>The General section and the registry of per-ship entry sets, one per ship prefab name.</summary>
    public static class ShipConfiguration
    {
        public static ConfigEntry<bool> LockConfiguration { get; private set; }

        /// <summary>Entries per ship prefab name. Bound once and kept for the whole game session.</summary>
        public static Dictionary<string, ShipEntries> Ships { get; } = new Dictionary<string, ShipEntries>();

        private static SyncedConfiguration synced;

        public static void Initialize(SyncedConfiguration config)
        {
            synced = config;
            LockConfiguration = config.BindLocking("General", "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            ShipMultipliers.Initialize(config);
        }

        public static bool TryGet(string name, out ShipEntries entries) => Ships.TryGetValue(name, out entries);

        /// <summary>Binds the entries of one ship if it has none yet. Returns whether new entries were bound.</summary>
        public static bool Register(string name, ShipDefaults defaults)
        {
            if (defaults == null || Ships.ContainsKey(name))
                return false;
            Ships[name] = ShipEntries.Bind(synced, name, defaults);
            return true;
        }

        /// <summary>
        /// Runs registrations with the .cfg written once at the end instead of once per entry (BepInEx saves the
        /// whole file on every new Bind), and only when entries were added.
        /// </summary>
        public static void RegisterBatch(Action register)
        {
            ConfigFile file = synced.Config;
            bool saveOnSet = file.SaveOnConfigSet;
            int before = Ships.Count;
            file.SaveOnConfigSet = false;
            try
            {
                register();
            }
            finally
            {
                file.SaveOnConfigSet = saveOnSet;
            }
            if (saveOnSet && Ships.Count != before)
                file.Save();
        }
    }
}
