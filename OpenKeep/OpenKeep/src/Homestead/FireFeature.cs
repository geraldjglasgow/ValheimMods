using PatchGuard;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Fires of the Homestead section: binds Build On Wood and re-applies it at once when it changes (a cfg edit, the
    /// server's value arriving at login, the player's own value back after logout). The feature shows no words of its
    /// own: a refused placement keeps the game's message.
    /// </summary>
    public static class FireFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            FireSettings.Bind(synced);
            FireSettings.BuildOnWood.SettingChanged += (_, _) => Guard.Run("build on wood", FirePlacement.ApplyAll);
        }
    }
}
