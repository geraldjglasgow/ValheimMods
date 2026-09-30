using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// Section 26: the Executioner's Greataxe, the players' two-handed axe made from the Crypt Executioner's axehead. Its
    /// damage (a fully upgraded Bronze Axe's by default) and its recipe at the workbench. Synced.
    /// </summary>
    public static class GreataxeSettings
    {
        // No apostrophe: BepInEx refuses = \n \t \ " ' [ ] in section and key names, and the throw stops the whole Awake.
        public const string Section = "26 - Executioner Greataxe";

        private static ConfigEntry<bool> enabled = null!;
        private static ConfigEntry<float> slash = null!, chop = null!;
        private static ConfigEntry<int> spines = null!, bones = null!, station = null!;

        public static bool On => enabled.Value;
        public static float Slash => slash.Value;
        public static float Chop => chop.Value;
        public static int Spines => spines.Value;
        public static int Bones => bones.Value;
        public static int StationLevel => station.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            enabled = config.Bind(Section, "Recipe", true, "Whether the greataxe can be made at the workbench.");
            slash = config.Bind(Section, "Slash", 55f, "Slash damage (a fully upgraded Bronze Axe: 55).", acceptableValues: Settings.Range(0f, 1000f));
            chop = config.Bind(Section, "Chop", 49f, "Chop damage, for trees (a fully upgraded Bronze Axe: 49).", acceptableValues: Settings.Range(0f, 1000f));
            spines = config.Bind(Section, "Spines", 8,
                "Spines (the bone weapons' drop from skeletons) the recipe takes besides the axehead; without the skeleton arsenal, "
                + "bone fragments take their place.", acceptableValues: new AcceptableValueRange<int>(0, 100));
            bones = config.Bind(Section, "Bone Fragments", 10, "Bone fragments the recipe takes.", acceptableValues: new AcceptableValueRange<int>(0, 100));
            station = config.Bind(Section, "Workbench Level", 3, "Workbench level needed.", acceptableValues: new AcceptableValueRange<int>(1, 5));
        }
    }
}
