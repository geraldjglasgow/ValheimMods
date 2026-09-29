using System;
using BepInEx.Configuration;
using EliteCreaturesPack.Arsenal;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Crossbow;
using EliteCreaturesPack.DraugrHound;
using EliteCreaturesPack.Haugbui;
using EliteCreaturesPack.Headsman;
using EliteCreaturesPack.Irrbloss;
using EliteCreaturesPack.Kraken;
using EliteCreaturesPack.LeechMatron;
using EliteCreaturesPack.Mimic;
using EliteCreaturesPack.Mountains;
using EliteCreaturesPack.RimeGiant;
using EliteCreaturesPack.Rootling;
using EliteCreaturesPack.Slinger;
using EliteCreaturesPack.Swamp;
using SyncedConfig;

namespace EliteCreaturesPack
{
    /// <summary>
    /// The General section, and the order every creature's section is bound in. Sections are numbered so the .cfg and
    /// configuration managers list them in this order. Every setting changes gameplay, so every one is synced from the
    /// server and locked while <c>Lock Configuration</c> is on there.
    /// </summary>
    public static class Settings
    {
        public const string General = "1 - General";

        public static ConfigEntry<bool> LockConfiguration { get; private set; } = null!;

        /// <summary>
        /// After any setting changes, on the main thread: an edit of the .cfg reloaded, or the server's values arriving.
        /// Each creature puts its new numbers on its prefabs and on the ones already loaded.
        /// </summary>
        public static event Action? Changed;

        public static void Initialize(SyncedConfiguration config)
        {
            LockConfiguration = config.BindLocking(General, "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            MimicSettings.Initialize(config);
            SlingerSettings.Initialize(config);
            RimeGiantSettings.Initialize(config);
            KrakenSettings.Initialize(config);
            XbowSettings.Initialize(config);
            XbowItemSettings.Initialize(config);
            ArsenalSettings.Initialize(config);
            MountainSettings.Initialize(config);
            SwampSettings.Initialize(config);
            HaugbuiSettings.Initialize(config);
            DraugrHoundSettings.Initialize(config);
            RootlingSettings.Initialize(config);
            LeechMatronSettings.Initialize(config);
            IrrblossSettings.Initialize(config);
            HeadsmanSettings.Initialize(config);
            GreataxeSettings.Initialize(config);
            config.Config.SettingChanged += (sender, args) => SafeCall.Run("settings changed", () => Changed?.Invoke());
        }

        /// <summary>A range from <paramref name="min"/> to <paramref name="max"/>, for a number setting.</summary>
        public static AcceptableValueRange<float> Range(float min, float max) => new AcceptableValueRange<float>(min, max);
    }
}
