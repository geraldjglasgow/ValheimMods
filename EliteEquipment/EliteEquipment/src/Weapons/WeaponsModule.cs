using PatchGuard;
using SyncedConfig;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// Entry point of Weapons (section 3): binds the switch, and puts the recipes and the game weapons' remade icons in or
    /// out when the switch changes (a server's value arriving included). The words are in translations/English.yml.
    /// </summary>
    public static class WeaponsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            WeaponsSettings.Initialize(synced);
            WeaponsSettings.Enabled.SettingChanged += (_, _) => Guard.Run("weapons switch", WeaponsSwitch.Changed);
        }
    }
}
