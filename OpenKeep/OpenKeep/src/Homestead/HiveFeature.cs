using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Honey per day: binds the beehive keys of section 8. The rate is read at every hive tick (<see cref="HivePatch"/>),
    /// so a changed setting applies within 10 s without a handler, and the feature has no words of its own.
    /// </summary>
    public static class HiveFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            HiveSettings.Initialize(synced);
        }
    }
}
