using BepInEx.Configuration;
using SyncedConfig;

namespace FeastMaster
{
    /// <summary>Auto Eat: the server's permission in section 0, the player's own choice in section 8.</summary>
    public static partial class Settings
    {
        public static ConfigEntry<bool> AllowAutoEat { get; private set; }
        public static ConfigEntry<bool> AutoEat { get; private set; }

        private static void BindAutoEat(SyncedConfiguration config)
        {
            AllowAutoEat = config.Bind(FeastMasterData.GlobalSection, "Allow Auto Eat", false,
                "When on, a player whose food runs out automatically eats another of the same food from their inventory, if they have one. Each player can still turn it off for themselves with Auto Eat in section 8.");
            AutoEat = config.Bind(DisplaySection, "Auto Eat", true,
                "Eat another of the same food when one runs out, while the server allows it (Allow Auto Eat in section 0). Per player, not synced from the server.", synced: false);
        }
    }
}
