using BepInEx.Configuration;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The arena's one setting (features/arena.md section 5): whether Hel's hall stands over the world centre and Hel's
    /// Trial can be run there. Like the raids' switch (<see cref="RaidSettings"/>) it changes the game, so it is not a
    /// player's own: <see cref="Rules.ServerLock"/> registers it with the mod's Charter, and while the server binds its
    /// players (the .cfg's `Lock to server`) the server's value holds for every player. A change arrives as the
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
                + "Hel's arena stands far out over the western ocean, loaded only for players who go there, and Hel's "
                + "Trial can be run in it: wake Hel on her throne and fight the dead in waves, a boss every fifth, in the "
                + "trial chosen at the gate - a biome's (the Meadows, then each biome whose boss the world has killed), "
                + "won by beating its boss, or Infinite, the highest open biome harder each round. Players get in "
                + "through Helgrind, Hel's gate, a portal piece in the hammer's Misc tab (workbench: 20 Wood, 20 Stone, "
                + "1 Eikthyr trophy; E on it picks the trial), and out through "
                + "the swirl at the end of the hall's gate passage; 'elite arena' (admin) takes you in and out too. With "
                + "false there is no hall, Helgrind leaves the hammer and gates already built stay closed, 'elite arena' "
                + "says the arena is off, and arena creatures still in the world vanish. Set on the server: while it "
                + "binds its players ('Lock to server' in 1 - General), the server's value holds for everyone.");
        }
    }
}
