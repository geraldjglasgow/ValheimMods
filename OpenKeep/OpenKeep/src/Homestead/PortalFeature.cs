using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Quick portals: binds the portal keys of section 8. The jump is hurried in the game's own teleport update
    /// (<see cref="PortalQuickPatch"/>), which reads the settings there, so a change applies at the next jump without a
    /// handler; the feature has no words of its own.
    /// </summary>
    public static class PortalFeature
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            PortalSettings.Bind(synced);
        }
    }
}
