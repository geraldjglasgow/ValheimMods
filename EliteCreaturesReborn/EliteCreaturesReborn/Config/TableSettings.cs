using BepInEx.Configuration;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The Rendering Table's one setting (the user, 2026-10-20): whether it is in the hammer and renders trophies. It
    /// changes the game, so like the Raiders Chest <see cref="Rules.ServerLock"/> registers it with the mod's Charter and
    /// the server's value holds while it binds. Everything else about the table is a fixed number
    /// (<see cref="Rendering.RenderTiers"/>).
    /// </summary>
    public static class TableSettings
    {
        public const string Section = "16 - Rendering Table";

        public static ConfigEntry<bool> RenderingTable = null!;

        /// <summary>True when the Rendering Table is in the hammer and a built one renders; the server's value while it binds.</summary>
        public static bool TableEnabled => RenderingTable == null || RenderingTable.Value;

        public static void Bind(ConfigFile config)
        {
            RenderingTable = config.Bind(Section, "Rendering Table", true,
                "The Rendering Table is in the hammer (Crafting): it renders a creature trophy into ores or ingredients "
                + "of the trophy's biome, or a boss trophy into a Forge idol of its biome's tier. With false it is "
                + "not, and a table already built renders nothing. Set on the server: while it binds its players ("
                + "'Lock to server' in 1 - General), the server's value holds for everyone.");
        }
    }
}
