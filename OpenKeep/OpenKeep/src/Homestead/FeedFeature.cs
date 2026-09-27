using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Auto feed: binds the keys of section 8. Feeding runs in the station's own tick (<see cref="FeedPatch"/>) and
    /// reads the settings there, so a change applies within a second without a handler; the feature shows no words.
    /// </summary>
    public static class FeedFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            FeedSettings.Bind(synced);
        }
    }
}
