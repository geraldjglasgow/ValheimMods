using BepInEx.Configuration;
using SyncedConfig;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// Section "1. Boots": one switch, on by default (installing the mod is the choice), synced and locked; read at use
    /// time. PackPanel reads this entry through the chainloader (section and key are its contract) and lays out a Feet
    /// slot while it is on.
    /// </summary>
    public static class BootsSettings
    {
        public const string Section = "1. Boots";
        public const string Key = "Separate Boots";

        public static ConfigEntry<bool> Enabled { get; private set; }

        public static bool On => Enabled != null && Enabled.Value;

        public static void Initialize(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, Key, true,
                "Splits the game's leg armour into leggings and boots worn on the feet. Each of the 20 leggings keeps 80% of " +
                "its armour, weight and stat modifiers and 80% of its recipe; the matching boots (crafted beside it, same " +
                "station and level) carry the other 20%. Resistances and equip effects stay on the leggings; the boots count " +
                "towards the set, which needs one more piece. Off: the leggings are the game's own and boots cannot be worn " +
                "(boots already made stay in the inventory).");
        }
    }
}
