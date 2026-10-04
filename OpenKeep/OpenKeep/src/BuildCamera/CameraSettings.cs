using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// The gameplay keys of section "11. Build Camera": where the camera may go, how far it builds and picks up, and what
    /// it costs in comfort. All synced and lockable; read at use time.
    /// </summary>
    public static class CameraSettings
    {
        public const string Section = CameraModule.Section;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> RangeMultiplier { get; private set; }
        public static ConfigEntry<float> ExtraReach { get; private set; }
        public static ConfigEntry<bool> CameraPickup { get; private set; }
        public static ConfigEntry<bool> EntryNeedsResting { get; private set; }
        public static ConfigEntry<int> EntryMinComfort { get; private set; }
        public static ConfigEntry<bool> PickupNeedsResting { get; private set; }
        public static ConfigEntry<int> PickupMinComfort { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "With a hammer, hoe or cultivator in hand near a crafting station (a workbench, forge, stonecutter...), Toggle Key detaches the camera from you. "
                + "Fly it with the movement keys, Jump up, Crouch down, Run faster; build, remove and repair from it as usual. Toggle Key again, Hide (R) or putting the tool away brings it back.");
            RangeMultiplier = synced.Bind(Section, "Range Multiplier", 1f,
                "The camera comes out and stays within this many times a station's build range around any crafting station (measured flat as the game does, and as far above or below it). "
                + "Only the camera's area: pieces still need the station's real range, and the station's base area (raids, comfort) is unchanged.",
                acceptableValues: new AcceptableValueRange<float>(0.25f, 5f));
            ExtraReach = synced.Bind(Section, "Extra Reach", 5f,
                "Metres the camera reaches beyond your own building reach (the game's 5 m, or what another mod makes it), measured from the camera, for placing, removing and repairing.",
                acceptableValues: new AcceptableValueRange<float>(0f, 45f));
            CameraPickup = synced.Bind(Section, "Camera Pickup", true,
                "While the camera is out, items lying within the game's auto pickup range of it come into your inventory, as they do around you (only with the game's auto pickup on).");
            BindNeeds(synced);
        }

        private static void BindNeeds(SyncedConfiguration synced)
        {
            EntryNeedsResting = synced.Bind(Section, "Entry Needs Resting", false,
                "The camera comes out only while you have the game's Resting effect (by a fire, under a roof).");
            EntryMinComfort = synced.Bind(Section, "Entry Min Comfort", 0,
                "The comfort level you need where you stand to bring the camera out. 0: none.",
                acceptableValues: new AcceptableValueRange<int>(0, 50));
            PickupNeedsResting = synced.Bind(Section, "Pickup Needs Resting", false,
                "Camera Pickup works only while you have the game's Resting effect.");
            PickupMinComfort = synced.Bind(Section, "Pickup Min Comfort", 0,
                "The comfort level you need where you stand for Camera Pickup. 0: none.",
                acceptableValues: new AcceptableValueRange<int>(0, 50));
        }
    }
}
