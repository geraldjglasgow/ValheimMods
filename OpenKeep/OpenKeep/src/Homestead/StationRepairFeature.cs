using OpenKeep.Core;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Repair on opening a station: binds the key of section 8 and the two message words. The repair runs when the game
    /// opens a crafting station for the local player (<see cref="StationRepairPatch"/>) and reads the setting there, so a
    /// change applies at the next opening without a handler.
    /// </summary>
    public static class StationRepairFeature
    {
        /// <summary>The centre message after opening a station repaired one item.</summary>
        public const string RepairedOne = "$ok_autorepair_one";

        /// <summary>The same with a count ({0}) of two or more.</summary>
        public const string RepairedMany = "$ok_autorepair_many";

        public static void Initialize(SyncedConfiguration synced)
        {
            StationRepairSettings.Bind(synced);
            Language.Add("ok_autorepair_one", "Repaired 1 item");
            Language.Add("ok_autorepair_many", "Repaired {0} items");
        }
    }
}
