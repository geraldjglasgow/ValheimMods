using EliteEquipment.Core;
using PatchGuard;
using SyncedConfig;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// Entry point of Weapons (section 3): binds the switch, registers the words, and puts the recipes and the game
    /// weapons' remade icons in or out when the switch changes (a server's value arriving included).
    /// </summary>
    public static class WeaponsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            WeaponsSettings.Initialize(synced);
            foreach (WeaponSet set in WeaponSets.All)
            {
                Language.Add(set.Asset, set.Name);
                Language.Add(set.Asset + "_desc", set.Description);
            }
            WeaponsSettings.Enabled.SettingChanged += (_, _) => Guard.Run("weapons switch", WeaponsSwitch.Changed);
        }
    }
}
