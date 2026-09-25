using BepInEx.Configuration;

namespace EliteCrafting.Config
{
    /// <summary>How stones marked <c>confirm: true</c> ask first (applying-stones.md section 4).</summary>
    public enum ConfirmMode
    {
        HoldShift,
        Dialog,
        Off,
    }

    /// <summary>How much the tooltip affix block shows (display.md section 3).</summary>
    public enum TooltipDetail
    {
        Compact,
        Standard,
        Full,
    }

    /// <summary>
    /// The <c>com.EliteCrafting.cfg</c> settings (configuration.md section 2). Gameplay switches are Charter clauses:
    /// synced from the server and locked while it binds. Display, glow, confirm and diagnostics are per player and never
    /// leave the machine. Everything that decides what rolls is YAML, not here. Hot reloaded by ConfigReload.
    /// </summary>
    public static class ModSettings
    {
        public const string General = "1 - General";
        public const string Stones = "2 - Stones";
        public const string Drops = "3 - Drops";
        public const string Commands = "4 - Commands";
        public const string Display = "5 - Display (per player)";
        public const string Glow = "6 - Ground glow (per player)";
        public const string Diagnostics = "7 - Diagnostics";
        public const string Salvage = "8 - Salvage";
        public const string EliteCreatures = "9 - Elite Creatures Reborn";

        // gameplay, synced and lockable
        public static ConfigEntry<bool> AffixEffects { get; private set; } = null!;
        public static ConfigEntry<bool> ModifyEquippedItems { get; private set; } = null!;
        public static ConfigEntry<bool> StoneDrops { get; private set; } = null!;
        public static ConfigEntry<bool> MagicItemDrops { get; private set; } = null!;
        public static ConfigEntry<bool> ReadOnlyCommandsForEveryone { get; private set; } = null!;

        // per player, never synced
        public static ConfigEntry<ConfirmMode> ConfirmDestructiveStones { get; private set; } = null!;
        public static ConfigEntry<bool> ColoredItemNames { get; private set; } = null!;
        public static ConfigEntry<TooltipDetail> TooltipDetailLevel { get; private set; } = null!;
        public static ConfigEntry<bool> ShowDormantAffixes { get; private set; } = null!;
        public static ConfigEntry<bool> GroundGlow { get; private set; } = null!;
        public static ConfigEntry<float> GlowIntensity { get; private set; } = null!;
        public static ConfigEntry<float> GlowRange { get; private set; } = null!;
        public static ConfigEntry<int> GlowMaxLights { get; private set; } = null!;
        public static ConfigEntry<float> GlowRefreshSeconds { get; private set; } = null!;
        public static ConfigEntry<bool> GlowStones { get; private set; } = null!;
        public static ConfigEntry<bool> LogRolls { get; private set; } = null!;
        public static ConfigEntry<bool> LogEffectRebuilds { get; private set; } = null!;

        /// <summary>Synced: Elite Creatures Reborn's stars (and later its world tier) raise our drops (ecr-integration.md).</summary>
        public static ConfigEntry<bool> EcrSynergy { get; private set; } = null!;

        // salvage (8): the switch synced, the key per player
        public static ConfigEntry<bool> SalvageEnabled { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> SalvageKey { get; private set; } = null!;

        internal static void Bind(ConfigFile config)
        {
            BindGameplay(config);
            BindDisplay(config);
            BindGlow(config);
            BindDiagnostics(config);
            BindSalvage(config);
            BindSynergy(config);
        }

        private static void BindGameplay(ConfigFile config)
        {
            AffixEffects = Synced(config.Bind(General, "Affix effects", true,
                "Master switch for every affix effect. Off: items keep and show their affixes, nothing applies."));
            ModifyEquippedItems = Synced(config.Bind(Stones, "Modify equipped items", true,
                "Stones may be used on items that are equipped."));
            StoneDrops = Synced(config.Bind(Drops, "Stone drops", true,
                "Creatures drop stones per the economy drop tables."));
            MagicItemDrops = Synced(config.Bind(Drops, "Magic item drops", true,
                "Creatures drop pre-rolled magic gear per the economy drop tables."));
            ReadOnlyCommandsForEveryone = Synced(config.Bind(Commands, "Read-only commands for everyone", true,
                "ecraft inspect, stats, list and help work for every player; off makes them admin-only too."));
        }

        private static void BindDisplay(ConfigFile config)
        {
            ConfirmDestructiveStones = Local(config.Bind(Stones, "Confirm destructive stones", ConfirmMode.HoldShift,
                "How stones that cannot be undone ask first: hold Shift while clicking, a yes/no dialog, or not at all. Per player."));
            ColoredItemNames = Local(config.Bind(Display, "Colored item names", true,
                "Draw magic item names in their rarity color. Per player."));
            TooltipDetailLevel = Local(config.Bind(Display, "Tooltip detail", TooltipDetail.Standard,
                "Compact: affix lines only. Standard: with tiers. Full: with tier ranges and unreadable data. Per player."));
            ShowDormantAffixes = Local(config.Bind(Display, "Show dormant affixes", true,
                "List affixes the server no longer defines, greyed. Per player."));
        }

        private static void BindGlow(ConfigFile config)
        {
            GroundGlow = Local(config.Bind(Glow, "Ground glow", true,
                "Magic items lying in the world glow in their rarity color. Per player."));
            GlowIntensity = Local(config.Bind(Glow, "Glow intensity", 1f,
                new ConfigDescription("Brightness of the glow. Per player.", new AcceptableValueRange<float>(0f, 3f))));
            GlowRange = Local(config.Bind(Glow, "Glow range", 2f,
                new ConfigDescription("Glow radius in metres. Per player.", new AcceptableValueRange<float>(0.5f, 6f))));
            GlowMaxLights = Local(config.Bind(Glow, "Glow max lights", 25,
                new ConfigDescription("Only the nearest this many glowing items hold a live light. Per player.",
                    new AcceptableValueRange<int>(0, 100))));
            GlowRefreshSeconds = Local(config.Bind(Glow, "Glow refresh seconds", 1f,
                new ConfigDescription("How often the nearest lights are re-chosen. Per player.",
                    new AcceptableValueRange<float>(0.25f, 5f))));
            GlowStones = Local(config.Bind(Glow, "Glow stones", false,
                "Stones lying in the world glow in their tint too. Per player."));
        }

        // salvage.md section 8: the switch is synced (grinding is gameplay); the key is each player's own.
        private static void BindSalvage(ConfigFile config)
        {
            SalvageEnabled = Synced(config.Bind(Salvage, "Salvage", true,
                "Grinding magic items into shards with the Salvage key. Fusing shards into stones works either way."));
            SalvageKey = Local(config.Bind(Salvage, "Salvage key", new KeyboardShortcut(UnityEngine.KeyCode.End),
                "The key that grinds the magic item under the pointer in your inventory (Shift + key when confirm is HoldShift). Per player."));
        }

        private static void BindDiagnostics(ConfigFile config)
        {
            LogRolls = Local(config.Bind(Diagnostics, "Log rolls", false,
                "Log every roll (stone, drop, command) with its inputs and result. Per player."));
            LogEffectRebuilds = Local(config.Bind(Diagnostics, "Log effect rebuilds", false,
                "Log each rebuild of the affix effects with the channel totals. Per player."));
        }

        // Gameplay, so a Charter clause: every creature owner rolls with the server's choice (ecr-integration.md 9).
        private static void BindSynergy(ConfigFile config)
        {
            EcrSynergy = Synced(config.Bind(EliteCreatures, "Synergy", false,
                "With Elite Creatures Reborn installed: its elite stars (and later its world tier) raise EliteCrafting's drops. No effect without it."));
        }

        private static ConfigEntry<T> Synced<T>(ConfigEntry<T> entry)
        {
            ServerBinding.Charter.Clause(entry);
            return entry;
        }

        private static ConfigEntry<T> Local<T>(ConfigEntry<T> entry)
        {
            ServerBinding.Charter.Clause(entry, local: true);
            return entry;
        }
    }
}
