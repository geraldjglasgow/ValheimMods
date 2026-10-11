using BepInEx.Configuration;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The raids' one setting (features/raids.md section 6): whether the Raiders Chest is in the hammer and can sound a
    /// raid. Unlike the rest of the .cfg it changes the game, so it is not a player's own: <see cref="Rules.ServerLock"/>
    /// registers it with the mod's Charter, and while the server binds its players (the rule file's `lock to server`) the
    /// server's value holds for every player. Everything else about raids is a fixed number (<see cref="Raids.RaidTable"/>).
    /// </summary>
    public static class RaidSettings
    {
        public const string Section = "11 - Raids";

        public static ConfigEntry<bool> RaidersChest = null!;

        /// <summary>True when the Raiders Chest is in the hammer and a built one can sound a raid; the server's value
        /// while it binds. A test raid by command does not ask.</summary>
        public static bool ChestEnabled => RaidersChest == null || RaidersChest.Value;

        public static void Bind(ConfigFile config)
        {
            RaidersChest = config.Bind(Section, "Raiders Chest", true,
                "The Raiders Chest is in the hammer (Misc): fill it with gold and sound a raid on your own base; win and the "
                + "gold comes back with more. With false it is not, and a chest already built is an ordinary coin chest that "
                + "cannot sound a raid. Set on the server: while it binds its players (the rule file's 'lock to server'), "
                + "the server's value holds for everyone.");
        }
    }
}
