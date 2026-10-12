using BepInEx.Configuration;
using SyncedConfig;

namespace EliteEquipment.Shields
{
    /// <summary>Section "2. Shields": one switch, on by default, synced and locked; read at use time.</summary>
    public static class ShieldsSettings
    {
        public const string Section = "2. Shields";
        public const string Key = "Shields";

        public static ConfigEntry<bool> Enabled { get; private set; }

        public static bool On => Enabled != null && Enabled.Value;

        public static void Initialize(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, Key, true,
                "Adds the shields the game has none of for a biome: the Wood (Meadows, workbench), Silver (Mountains, " +
                "forge), Black Metal (Plains, forge) and Flametal Buckler (Ashlands, black forge), the Bronze Shield " +
                "(Black Forest, forge), the Silver (forge) and Carapace Tower Shield (black forge), each crafted beside " +
                "that biome's shield; and gives the game's bucklers, round and tower shields icons drawn the same way. " +
                "A buckler blocks less than a shield and parries far better (2.5x against 1.5x); a tower shield cannot " +
                "parry and slows you more. Off: they cannot be crafted (shields already made stay and still work) and " +
                "the game's icons come back.");
        }
    }
}
