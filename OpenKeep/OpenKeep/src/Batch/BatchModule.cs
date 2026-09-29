using SyncedConfig;

namespace OpenKeep.Batch
{
    /// <summary>
    /// Entry point of the Batch module: the - amount + stepper left of the crafting panel's Craft button. Binds section
    /// "10. Batch Crafting"; the patches are in <see cref="BatchPatches"/>. The module has no words of its own.
    /// </summary>
    public static class BatchModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            BatchSettings.Bind(synced);
        }
    }
}
