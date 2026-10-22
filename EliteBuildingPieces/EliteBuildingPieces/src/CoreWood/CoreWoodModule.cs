using BepInEx.Configuration;
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

    /// <summary>
    /// The core wood pieces: settings and the hammer kept in step with the switch. Their words are in
    /// translations/English.yml (<see cref="CoreWoodPiece.Word"/>).
    /// </summary>
    public static class CoreWoodModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CoreWoodSettings.Initialize(synced);
            CoreWoodSettings.Enabled.SettingChanged += (_, _) => CoreWoodHammer.Refresh();
        }
    }
}
