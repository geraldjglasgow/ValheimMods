using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Area repair: binds the key of section 8 and the two message words. The repair runs in the game's own hammer
    /// repair (<see cref="RepairPatch"/>) and reads the setting there, so a change applies at the next swing without a
    /// handler.
    /// </summary>
    public static class RepairFeature
    {
        /// <summary>The top-left line after the game's own "repaired" message, one neighbour repaired too.</summary>
        public const string RepairedOne = "$ok_repair_one";

        /// <summary>The same with a count ($1) of two or more.</summary>
        public const string RepairedMany = "$ok_repair_many";

        public static void Initialize(SyncedConfiguration synced)
        {
            RepairSettings.Bind(synced);
        }
    }
}
