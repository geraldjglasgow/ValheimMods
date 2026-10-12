using BepInEx.Configuration;
using SyncedConfig;

namespace EliteEquipment.Weapons
{
    /// <summary>Section "3. Weapons": one switch, on by default, synced and locked; read at use time.</summary>
    public static class WeaponsSettings
    {
        public const string Section = "3. Weapons";
        public const string Key = "Weapons";

        public static ConfigEntry<bool> Enabled { get; private set; }

        public static bool On => Enabled != null && Enabled.Value;

        public static void Initialize(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, Key, true,
                "Adds the weapons the game has none of for a biome: Bronze, Iron, Silver and Black Metal Greatswords; " +
                "Bronze, Iron, Silver, Black Metal, Carapace and Bloodgold Dual Axes; Flint Knuckles, Iron and Flametal " +
                "Claws; Iron and Flametal Daggers; Silver and Flametal Atgeirs; Silver and Flametal Axes; a Flametal " +
                "Greataxe; a Black Metal Bow and Spear - each crafted beside a game weapon of its tier; and gives the " +
                "game's weapons of those kinds icons drawn the same way. Off: they cannot be crafted (weapons already " +
                "made stay and still work) and the game's icons come back.");
        }
    }
}
