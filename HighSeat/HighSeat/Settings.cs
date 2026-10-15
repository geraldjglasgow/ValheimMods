using BepInEx.Configuration;

namespace HighSeat
{
    /// <summary>
    /// The .cfg. Every setting acts on the server only (a dedicated server, or the player hosting), so nothing is synced:
    /// a player's own copy changes nothing.
    /// </summary>
    internal static class Settings
    {
        private const string General = "1 - General";

        public static ConfigEntry<bool> ServerRunsCreatures = null!;
        public static ConfigEntry<int> FrameBudgetMs = null!;

        public static void Bind(ConfigFile config)
        {
            ServerRunsCreatures = config.Bind(General, "Server runs creatures", true,
                "The server loads every player's area and runs every creature and spawner in it, tamed creatures and bosses " +
                "included. Off: the game's own rule, where the nearest player's game runs each creature.");
            FrameBudgetMs = config.Bind(General, "Server frame budget ms", 50,
                new ConfigDescription("While the server's frames take longer than this, creatures it does not run yet are " +
                    "left to players as the game does; those it runs stay with it.", new AcceptableValueRange<int>(20, 500)));
        }
    }
}
