using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Entry point of the bed feature of section 8: several beds, respawn at the nearest. It binds the setting; the
    /// feature adds no words of its own (the bed hover uses the game's own "Sleep") and needs no change handler,
    /// since every patch reads the setting when it runs.
    /// </summary>
    public static class BedFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            BedSettings.Bind(synced);
        }
    }
}
