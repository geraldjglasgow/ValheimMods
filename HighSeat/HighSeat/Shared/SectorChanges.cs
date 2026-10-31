using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// Tells <see cref="LoneSectors"/> that an object entered or left a sector. Every ZDO goes through one of these: a new
    /// one (ZDO.Initialize), one received from a player, one that moves into another sector (ZDO.SetSector) and one
    /// destroyed (ZDOMan.HandleDestroyedZDO), so a sector's kept lists are rebuilt before they could hold a ZDO the game
    /// has destroyed and given out again from its pool. Runs whatever the settings (one lookup in a small table), so the
    /// lists are right the moment "Creatures only where shared" is switched on.
    /// </summary>
    [HarmonyPatch]
    internal static class SectorChanges
    {
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddToSector))]
        [HarmonyPostfix]
        private static void Added(ZoneSystem.SectorIndex sectorIndex) => LoneSectors.MarkDirty(sectorIndex.Sector);

        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.RemoveFromSector))]
        [HarmonyPostfix]
        private static void Removed(ZoneSystem.SectorIndex sectorIndex) => LoneSectors.MarkDirty(sectorIndex.Sector);
    }
}
