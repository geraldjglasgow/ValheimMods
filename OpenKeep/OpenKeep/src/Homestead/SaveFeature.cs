using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Quick world save: binds the key of section 8. The setting is read at every save (<see cref="SaveValueCopy"/>,
    /// <see cref="SaveObjectCopy"/>), so a changed value applies to the next save without a handler.
    /// </summary>
    public static class SaveFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            SaveSettings.Bind(synced);
        }
    }
}
