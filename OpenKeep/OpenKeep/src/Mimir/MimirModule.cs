using PatchGuard;
using SyncedConfig;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Entry point of Mímir's Chest (section 15; decisions in CLAUDE.md, "Mímir's Chest"): binds the switch and puts the chest in
    /// or out of the hammer when the switch changes (a server's value arriving included).
    /// </summary>
    public static class MimirModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            MimirSettings.Initialize(synced);
            MimirSettings.Enabled.SettingChanged += (_, _) => Guard.Run("mimir chest switch", MimirHammer.Refresh);
        }
    }
}
