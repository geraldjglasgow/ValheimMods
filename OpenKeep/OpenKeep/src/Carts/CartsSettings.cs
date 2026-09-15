using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Carts
{
    /// <summary>Section "6. Carts", all synced and locked; read at use time.</summary>
    public static class CartsSettings
    {
        public const string Section = "6. Carts";

        public static ConfigEntry<bool> CartWorkbench { get; private set; }
        public static ConfigEntry<int> CartStationLevel { get; private set; }
        public static ConfigEntry<float> CartStationRange { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            CartWorkbench = synced.Bind(Section, "Cart Workbench", false,
                "Every cart carries a workbench: crafting (Shift + Use on the cart opens it), repairing and building within Cart Station Range as if a workbench stood there, without roof or fire requirements.");
            CartStationLevel = synced.Bind(Section, "Cart Station Level", 1,
                "The cart's base station level; workbench extensions within the game's extension range still add to it.");
            CartStationRange = synced.Bind(Section, "Cart Station Range", 10f,
                "Build and craft range around the cart, in metres.");
        }
    }
}
