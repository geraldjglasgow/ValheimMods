using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Auto fuel: binds the keys of section 8. The refill runs in the fire's own tick (<see cref="FuelPatch"/>) and
    /// reads the settings there, so a change applies within two seconds without a handler; the feature shows no words.
    /// </summary>
    public static class FuelFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            FuelSettings.Bind(synced);
        }
    }
}
