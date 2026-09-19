using BepInEx.Configuration;
using SyncedConfig;

namespace Party
{
    /// <summary>Gameplay settings sync and lock; display settings (<c>synced: false</c>) stay local.</summary>
    public static class PartyConfig
    {
        // ---------------------------------------------------------------- gameplay (synced, lockable)
        public static ConfigEntry<bool> LockConfiguration { get; private set; }
        public static ConfigEntry<int> MaxPartySize { get; private set; }
        public static ConfigEntry<bool> FriendlyFire { get; private set; }
        public static ConfigEntry<int> InviteTimeoutSeconds { get; private set; }
        public static ConfigEntry<int> VitalsUpdatesPerSecond { get; private set; }

        // ---------------------------------------------------------------- display (local only)
        public static ConfigEntry<string> PartyColor { get; private set; }
        public static ConfigEntry<string> LeaderColor { get; private set; }
        public static ConfigEntry<string> PingModifierKey { get; private set; }

        public static ConfigEntry<float> PanelX { get; private set; }
        public static ConfigEntry<float> PanelY { get; private set; }
        public static ConfigEntry<float> PanelScale { get; private set; }
        public static ConfigEntry<float> PanelOpacity { get; private set; }
        public static ConfigEntry<float> BarWidth { get; private set; }
        public static ConfigEntry<float> BarHeight { get; private set; }
        public static ConfigEntry<float> RowSpacing { get; private set; }
        public static ConfigEntry<float> PanelPadding { get; private set; }
        public static ConfigEntry<int> FontSize { get; private set; }
        public static ConfigEntry<int> TitleFontSize { get; private set; }
        public static ConfigEntry<bool> ShowOwnRow { get; private set; }
        public static ConfigEntry<bool> ShowStamina { get; private set; }
        public static ConfigEntry<bool> ShowEitr { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindGameplay(config);
            BindDisplay(config);
        }

        private static void BindGameplay(SyncedConfiguration config)
        {
            LockConfiguration = config.BindLocking("General", "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for the settings below and cannot override them locally.");
            MaxPartySize = config.Bind("General", "Max Party Size", 8,
                "Server only. The most members one party can have, leader included.");
            FriendlyFire = config.Bind("General", "Friendly Fire Protection", true,
                "Server only. When on, party members cannot damage each other even with PvP enabled.");
            InviteTimeoutSeconds = config.Bind("General", "Invite Timeout Seconds", 60,
                "Server only. How long an invite waits for a response before it expires.");
            VitalsUpdatesPerSecond = config.Bind("General", "Vitals Updates Per Second", 3,
                "Server only. How often each client reports health/stamina/eitr for the health panel and map pins.",
                acceptableValues: new AcceptableValueRange<int>(1, 5));
        }

        private static void BindDisplay(SyncedConfiguration config)
        {
            PartyColor = config.Bind("Display", "Party Color", "#55CCFF",
                "Client side. Color of party member names, chat and map pins, as you see them.", synced: false);
            LeaderColor = config.Bind("Display", "Leader Color", "#FFD700",
                "Client side. Color used to mark the party leader, as you see them.", synced: false);
            PingModifierKey = config.Bind("Display", "Ping Modifier Key", "LeftAlt",
                "Client side. Hold this key while pinging the map to send a party-only ping. A KeyCode name.", synced: false);
            BindPanelLayout(config);
        }

        private static void BindPanelLayout(SyncedConfiguration config)
        {
            PanelX = config.Bind("Health Panel", "Panel X", 24f,
                "Client side. In 1920x1080-reference units. Saved automatically when you drag the panel.", synced: false);
            PanelY = config.Bind("Health Panel", "Panel Y", -1f,
                "Client side. In 1920x1080-reference units. Saved automatically when you drag the panel. -1 means unset: upper-middle on first draw.", synced: false);
            PanelScale = config.Bind("Health Panel", "Panel Scale", 1f, "Client side.", synced: false);
            PanelOpacity = config.Bind("Health Panel", "Panel Opacity", 0.85f, "Client side.", synced: false);
            BarWidth = config.Bind("Health Panel", "Bar Width", 260f, "Client side.", synced: false);
            BarHeight = config.Bind("Health Panel", "Bar Height", 16f, "Client side.", synced: false);
            RowSpacing = config.Bind("Health Panel", "Row Spacing", 10f, "Client side.", synced: false);
            PanelPadding = config.Bind("Health Panel", "Panel Padding", 12f, "Client side. Space around the panel's edges.", synced: false);
            FontSize = config.Bind("Health Panel", "Font Size", 16, "Client side. Member name and distance text.", synced: false);
            TitleFontSize = config.Bind("Health Panel", "Title Font Size", 18, "Client side. The party name shown at the top of the panel.", synced: false);
            ShowOwnRow = config.Bind("Health Panel", "Show Own Row", true, "Client side. Whether your own row appears in the panel.", synced: false);
            ShowStamina = config.Bind("Health Panel", "Show Stamina", false, "Client side.", synced: false);
            ShowEitr = config.Bind("Health Panel", "Show Eitr", false, "Client side.", synced: false);
        }
    }
}
