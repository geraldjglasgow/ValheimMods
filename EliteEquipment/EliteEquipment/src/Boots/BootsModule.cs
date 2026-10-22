using PatchGuard;
using SyncedConfig;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// Entry point of Boots (section 1; decisions in CLAUDE.md, "Boots"): binds the switch, registers the 20 sets (their
    /// words are in translations/English.yml), and puts the split in or out when the switch changes (a server's value
    /// arriving included).
    /// </summary>
    public static class BootsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            BootsSettings.Initialize(synced);
            BootSets.Initialize();
            BootsSettings.Enabled.SettingChanged += (_, _) => Guard.Run("boots switch", BootsSwitch.Changed);
            WornBootsLink.Register();
        }
    }
}
