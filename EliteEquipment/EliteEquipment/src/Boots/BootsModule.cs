using EliteEquipment.Core;
using PatchGuard;
using SyncedConfig;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// Entry point of Boots (section 1; decisions in CLAUDE.md, "Boots"): binds the switch, registers the 20 sets and the
    /// words, and puts the split in or out when the switch changes (a server's value arriving included).
    /// </summary>
    public static class BootsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            BootsSettings.Initialize(synced);
            BootSets.Initialize();
            Language.Add("ee_boots_desc", "Boots split from the matching leggings: a fifth of their armour, weight and stats, worn on the feet. They count towards the leggings' set.");
            Language.Add("ee_boots_off", "Boots are switched off on this world");
            BootsSettings.Enabled.SettingChanged += (_, _) => Guard.Run("boots switch", BootsSwitch.Changed);
            WornBootsLink.Register();
        }
    }
}
