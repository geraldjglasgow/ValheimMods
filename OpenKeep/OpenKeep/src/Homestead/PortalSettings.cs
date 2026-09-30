using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The portal keys of section "8. Homestead", read at use time. How long a jump takes changes the game, so those
    /// are synced and lockable; whether a jump shows the teleport screen is each player's own choice.
    /// </summary>
    public static class PortalSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> QuickPortals { get; private set; }
        public static ConfigEntry<float> QuickPortalRange { get; private set; }
        public static ConfigEntry<float> QuickPortalSeconds { get; private set; }
        public static ConfigEntry<bool> ScreenOnlyWhenLoading { get; private set; }
        public static ConfigEntry<bool> QuickAreaLoading { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            QuickPortals = synced.Bind(Section, "Quick Portals", true,
                "The closer together two portals are, the quicker the jump: the game's 8 s of a portal jump shrink with the distance, down to Quick Portal Seconds for portals side by side. "
                + "Every long jump counts, from any portal mod or the console, not dungeon doors. The game still waits for a far area to load. Off: the game's 8 s.");
            QuickPortalRange = synced.Bind(Section, "Quick Portal Range", 10000f,
                "Metres (on the map) between the two portals at which a jump takes the game's full 8 s; closer is quicker, in proportion. With 10000 a 1 km jump takes about 1.2 s; lower it (4000 to 5000) for a bigger difference between near and far portals.",
                acceptableValues: new AcceptableValueRange<float>(10f, 20000f));
            QuickPortalSeconds = synced.Bind(Section, "Quick Portal Seconds", 0.5f,
                "Seconds a jump takes between portals side by side.",
                acceptableValues: new AcceptableValueRange<float>(0f, 8f));
            ScreenOnlyWhenLoading = synced.Bind(Section, "Portal Screen Only When Loading", true,
                "A portal jump to a place already loaded around you (about 100-150 m) keeps the screen clear: no black screen, no teleport swirl. Farther jumps show the game's teleport screen while the area loads. Off: every jump shows it, as in the game.", false);
            QuickAreaLoading = synced.Bind(Section, "Quick Area Loading", true,
                "After a long portal jump, and while you wait to respawn, the land around you loads as fast as your PC allows instead of the game's one piece (64 m square) every 0.1 s. With a high simulation distance this saves several seconds per jump. Off: the game's pace.", false);
        }
    }
}
