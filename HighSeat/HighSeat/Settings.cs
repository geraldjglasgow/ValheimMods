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
        private const string BigServers = "2 - Big servers";

        public static ConfigEntry<bool> ServerRunsCreatures = null!;
        public static ConfigEntry<int> FrameBudgetMs = null!;
        public static ConfigEntry<int> MaxPlayers = null!;
        public static ConfigEntry<bool> BigServerSync = null!;
        public static ConfigEntry<bool> OnlyWhereShared = null!;
        public static ConfigEntry<bool> NightSkipVote = null!;
        public static ConfigEntry<bool> KeptPlaces = null!;
        public static ConfigEntry<bool> DropsForPlayersNearby = null!;

        public static void Bind(ConfigFile config)
        {
            ServerRunsCreatures = config.Bind(General, "Server runs creatures", true,
                "The server loads every player's area and runs every creature and spawner in it, tamed creatures and bosses " +
                "included. Off: the game's own rule, where the nearest player's game runs each creature.");
            FrameBudgetMs = config.Bind(General, "Server frame budget ms", 50,
                new ConfigDescription("While the server's frames take longer than this, creatures it does not run yet are " +
                    "left to players as the game does; those it runs stay with it.", new AcceptableValueRange<int>(20, 500)));
            BindBigServers(config);
        }

        private static void BindBigServers(ConfigFile config)
        {
            MaxPlayers = config.Bind(BigServers, "Max players", 50,
                new ConfigDescription("How many players the server takes at once (the game's own limit is 10). One more is " +
                    "turned away with the game's \"server is full\".", new AcceptableValueRange<int>(1, 1000)));
            BigServerSync = config.Bind(BigServers, "Big server sync", true,
                "Every player is sent the world every round (20 a second) with their own share of the send rate: only what " +
                "changed since the last round, and things far from them less often; the server's sends also skip the game's " +
                "extra copies of each package. Off: the game's own loop, which serves one player a frame.");
            OnlyWhereShared = config.Bind(BigServers, "Creatures only where shared", false,
                "On: the server runs only creatures near two or more players; one near a single player runs on that player's " +
                "game as without HighSeat, so their hits need no round trip. Spawners stay on the server. Off: the server " +
                "runs every creature near any player. Needs Server runs creatures.");
            NightSkipVote = config.Bind(BigServers, "Night skip vote", true,
                "The night is skipped once more than half of the players online vote for it: lying in bed is a vote, and so " +
                "is saying !sleep in chat. Off: the game's own rule, every player in bed.");
            KeptPlaces = config.Bind(BigServers, "Kept places", true,
                "The server keeps the ground and buildings of the busiest places and of fresh death spots loaded (creatures " +
                "unloaded), so creatures there start at once when a player arrives.");
            DropsForPlayersNearby = config.Bind(BigServers, "Boss drops for players nearby", true,
                "A drop the game gives once per player (a boss's own item, such as Eikthyr's antlers) counts the players " +
                "within 100 m of the creature when it dies, at least one. Off: every player online, as the game does.");
        }
    }
}
