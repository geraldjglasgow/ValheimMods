using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Capacity
{
    /// <summary>Section "5. Capacity": the switch is synced, the hover display is per player.</summary>
    public static class CapacitySettings
    {
        public const string Section = "5. Capacity";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> HoverContents { get; private set; }
        public static ConfigEntry<int> HoverLines { get; private set; }
        public static ConfigEntry<HoverFill> HoverFill { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "Master switch of the Capacity module: container sizes from OpenKeep.Containers.yml and the hover contents. Off restores the vanilla sizes where no item is in the way.");
            HoverContents = synced.Bind(Section, "Hover Contents", true,
                "Hovering a container adds its contents to the hover text: up to Hover Lines lines of 'name x count', then 'and n more'. Only for containers you could open.", synced: false);
            HoverLines = synced.Bind(Section, "Hover Lines", 8,
                "How many content lines the hover text shows.", synced: false, acceptableValues: new AcceptableValueRange<int>(0, 40));
            HoverFill = synced.Bind(Section, "Hover Fill", Capacity.HoverFill.Fraction,
                "The hover text's first extra line: Fraction '12 / 24 slots', Percent '50% full', Off.", synced: false);
        }
    }
}
