using EliteEquipment.Core;
using PatchGuard;
using SyncedConfig;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// Entry point of Shields (section 2): binds the switch, registers the words, and puts the recipes and the game
    /// shields' remade icons in or out when the switch changes (a server's value arriving included).
    /// </summary>
    public static class ShieldsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            ShieldsSettings.Initialize(synced);
            foreach (ShieldSet set in ShieldSets.All)
            {
                Language.Add(set.Word, set.Name);
                Language.Add(set.Word + "_desc", set.Description);
            }
            ShieldsSettings.Enabled.SettingChanged += (_, _) => Guard.Run("shields switch", ShieldsSwitch.Changed);
        }
    }
}
