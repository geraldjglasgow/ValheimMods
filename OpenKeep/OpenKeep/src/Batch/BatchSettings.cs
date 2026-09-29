using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Batch
{
    /// <summary>
    /// Section "10. Batch Crafting". Both entries change what one press of Craft makes, so both are synced and lockable.
    /// Values are read at use time so edits, reloads and server pushes apply at once.
    /// </summary>
    public static class BatchSettings
    {
        public const string Section = "10. Batch Crafting";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<int> MaxAmount { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "The Craft tab of every crafting station (and of crafting by hand) shows - amount + left of the Craft button, and Craft makes that many at once through the game's own multi-craft: the same cost, skill gain and bonus rolls per item as crafting them one by one. Off: the game's panel, where Shift + Craft makes 5.");
            MaxAmount = synced.Bind(Section, "Max Amount", 100,
                "The most one press of Craft makes. The amount also stops at what the materials (the inventory, and nearby containers with Reach) and the free inventory space allow.",
                acceptableValues: new AcceptableValueRange<int>(1, 1000));
        }
    }
}
