using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Section "15. Custom Storage Chests": one switch, off by default (user, 2026-10-07), synced and locked; read at use
    /// time. Off: Mímir's Chest cannot be built.
    /// </summary>
    public static class MimirSettings
    {
        public const string Section = "15. Custom Storage Chests";

        public static ConfigEntry<bool> Enabled { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Custom Storage Chests", false,
                "OpenKeep's own storage chests in the hammer. Today one: Mímir's Chest (at a workbench: 10 fine wood, 4 silver, " +
                "10 iron nails, 6 leather scraps), a chest that never fills. It grows a row at a time up to 32 rows of 8 " +
                "(256 stacks), each stack holding up to 9999 of an item that stacks, with a search field, quick filters and " +
                "sorting. Off: the hammer does not offer it; chests already built keep working.");
        }
    }
}
