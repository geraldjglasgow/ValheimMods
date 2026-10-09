using BepInEx.Configuration;
using EliteBuildingPieces.Core;
using SyncedConfig;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// Section "1. Core Wood": one switch, on by default (installing the mod is the choice), synced and locked. It only
    /// puts the pieces in the hammer or takes them out: the prefabs are always registered, so pieces already built load.
    /// </summary>
    public static class CoreWoodSettings
    {
        public static ConfigEntry<bool> Enabled { get; private set; }

        public static bool On => Enabled != null && Enabled.Value;

        public static void Initialize(SyncedConfiguration synced)
        {
            Enabled = synced.Bind("1. Core Wood", "Core Wood Pieces", true,
                "Adds four core wood pieces to the hammer's Building tab, after the wood gate: a wall (4 core wood), a large " +
                "wall twice as wide (8), a door (6) and a double door (12). The walls snap and hold like the stakewall; the " +
                "doors open away from you like the wood gate. Off: they leave the hammer, pieces already built stay.");
        }
    }

    /// <summary>The core wood pieces: settings, words, and the hammer kept in step with the switch.</summary>
    public static class CoreWoodModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CoreWoodSettings.Initialize(synced);
            CoreWoodSettings.Enabled.SettingChanged += (_, _) => CoreWoodHammer.Refresh();
            AddWords();
        }

        private static void AddWords()
        {
            Language.Add("ebp_corewood_wall", "Core wood wall");
            Language.Add("ebp_corewood_wall_desc", "Core wood logs laid between two posts. Snaps like the stakewall.");
            Language.Add("ebp_corewood_wall_large", "Large core wood wall");
            Language.Add("ebp_corewood_wall_large_desc", "A core wood wall twice as wide, in one piece.");
            Language.Add("ebp_corewood_door", "Core wood door");
            Language.Add("ebp_corewood_door_desc", "A core wood gate the width of a core wood wall. Opens away from you.");
            Language.Add("ebp_corewood_door_double", "Core wood double door");
            Language.Add("ebp_corewood_door_double_desc", "Two core wood gates that open together, the width of a large core wood wall.");
        }
    }
}
