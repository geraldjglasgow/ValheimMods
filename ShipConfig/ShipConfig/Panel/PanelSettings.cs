using BepInEx.Configuration;
using SyncedConfig;

namespace ShipConfig
{
    /// <summary>Section Display: the ship panel's switch, each player's own (not synced, never locked).</summary>
    public static class PanelSettings
    {
        public const string Section = "Display";

        public static ConfigEntry<bool> ShipPanel { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            ShipPanel = config.Bind(Section, "Ship Panel", true,
                "Each player's own. A small panel under the minimap while you are aboard a ship: its speed, its speed " +
                "multiplier against vanilla, your map exploration radius and, with GrindstoneSkills, your Sailing " +
                "abilities with their cooldowns (hover a line to read more). Windows such as the inventory draw over it.",
                synced: false);
        }
    }
}
