using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The area repair key of section "8. Homestead": it changes what one hammer swing repairs, so it is synced and lockable; read at use time.</summary>
    public static class RepairSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> AreaRepair { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            AreaRepair = synced.Bind(Section, "Area Repair", true,
                "Repairing a piece with the hammer also repairs the damaged pieces touching it: only its direct neighbours (at most 64), not the whole building. "
                + "Each neighbour needs what a repair by hand needs: its crafting station within range of you (a stone wall needs the stonecutter) and access to any ward it stands in. "
                + "The swing costs the stamina and hammer wear of one repair; the neighbours are free. Only a repair the game makes spreads: hitting a piece that needs no repair repairs nothing around it.");
        }
    }
}
