using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Rested sooner: binds the resting key of section 8. The wait is read in the Resting effect's own tick
    /// (<see cref="RestPatch"/>), so a changed setting applies at once without a handler, and the feature has no words
    /// of its own (the game shows no countdown for Resting).
    /// </summary>
    public static class RestFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            RestSettings.Bind(synced);
        }
    }
}
