using SyncedConfig;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// Entry point of the Recipe Tracker module: recipes pinned to the HUD with their materials, counted against what
    /// the player has. Binds section "13. Recipe Tracker" (all unsynced); the words are in translations/English.yml,
    /// the patches in <see cref="TrackerPatches"/>, the tracked list lives in the character's custom data
    /// (<see cref="TrackerList"/>).
    /// </summary>
    public static class TrackerModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            TrackerSettings.Bind(synced);
        }
    }
}
