using BepInEx.Configuration;
using SyncedConfig;

namespace Hearthhold
{
    /// <summary>
    /// The whole .cfg: the server lock and one switch per optional feature, all synced. Stars, professions and the
    /// aging cask are always on; their numbers are fixed in code (see the mod's CLAUDE.md), not settings.
    /// </summary>
    public static class Settings
    {
        public const string General = "1 - General";
        public const string Features = "2 - Features";

        public static ConfigEntry<bool> LockConfiguration { get; private set; }
        public static ConfigEntry<bool> DailyFortune { get; private set; }
        public static ConfigEntry<bool> ShippingCrate { get; private set; }
        public static ConfigEntry<bool> TraderFriendship { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            LockConfiguration = config.BindLocking(General, "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            DailyFortune = config.Bind(Features, "Daily Fortune", true,
                "Each day the Norns grant good or bad fortune, the same for everyone, which nudges every star roll up or down.");
            ShippingCrate = config.Bind(Features, "Shipping Crate", true,
                "The Shipping Crate piece: what you leave in it is bought at dawn for coins, more for more stars. Off: the piece cannot be built and existing crates no longer pay.");
            TraderFriendship = config.Bind(Features, "Trader Friendship", true,
                "Give a starred dish to Haldor, Hildir or the Bog Witch once a day to earn their friendship and a small discount.");
        }
    }
}
