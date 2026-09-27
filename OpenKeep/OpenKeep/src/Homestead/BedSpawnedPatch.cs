using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Player.OnSpawned</c> postfix for the local player (called by <c>Game.SpawnPlayer</c> right after the game
    /// loaded the character's data into the new player): the bed changes made while there was no player are written
    /// now, and the fallback of the finished respawn is dropped.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class BedSpawnedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            BedRespawn.Clear();
            BedList.Flush();
        }
    }
}
