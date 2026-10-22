using PatchGuard;
using SyncedConfig;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// Entry point of Shields (section 2): binds the switch, and puts the recipes and the game shields' remade icons in or
    /// out when the switch changes (a server's value arriving included). The words are in translations/English.yml.
    /// </summary>
    public static class ShieldsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            ShieldsSettings.Initialize(synced);
            ShieldsSettings.Enabled.SettingChanged += (_, _) => Guard.Run("shields switch", ShieldsSwitch.Changed);
        }
    }
}
