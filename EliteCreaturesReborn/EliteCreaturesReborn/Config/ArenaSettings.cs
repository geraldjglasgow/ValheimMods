using BepInEx.Configuration;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The arena's one setting (features/arena.md section 5): whether Hel's hall stands over the world centre and Hel's
    /// Trial can be run there. Like the raids' switch (<see cref="RaidSettings"/>) it changes the game, so it is not a
    /// player's own: <see cref="Rules.ServerLock"/> registers it with the mod's Charter, and while the server binds its
    /// players (the rule file's `lock to server`) the server's value holds for every player. A change arrives as the
    /// entry's SettingChanged, from the .cfg's hot reload or from the server; the hall (Arena/Hall) builds and removes
    /// itself on it. Everything else about the arena is a fixed number in the code.
    /// </summary>
    public static class ArenaSettings
    {
        public const string Section = "12 - Arena";

        public static ConfigEntry<bool> Arena = null!;

        /// <summary>True when the hall stands and a trial can be run; the server's value while it binds.</summary>
        public static bool Enabled => Arena != null && Arena.Value;

        public static void Bind(ConfigFile config)
        {
            Arena = config.Bind(Section, "Arena", false,
                "Hel's Arena is subject to change without warning: it works and is tested, but its final shape is still "
                + "being decided. "
                + "Hel's hall stands high above the world centre and Hel's Trial can be run there: wake Hel on her throne "
                + "and fight endless waves of the dead, stronger each wave, with a boss every tenth, until every player "
                + "has fallen or left. Players get in through Helgrind, Hel's gate, a portal piece in the hammer's Misc "
                + "tab (Black Forge: 1 Sacrificial Blood, 2 Frostcore, 10 Bloodgold, 5 Memorial Coal), and out through "
                + "the swirl at the end of the hall's gate passage; 'elite arena' (admin) takes you in and out too. With "
                + "false there is no hall, Helgrind leaves the hammer and gates already built stay closed, 'elite arena' "
                + "says the arena is off, and arena creatures still in the world vanish. Set on the server: while it "
                + "binds its players (the rule file's 'lock to server'), the server's value holds for everyone.");
        }
    }
}
