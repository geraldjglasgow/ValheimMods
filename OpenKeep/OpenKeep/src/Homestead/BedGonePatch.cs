using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Game.RemoveCustomSpawnPoint</c> postfix. The game calls it from <c>WearNTear.Destroy</c> on the client that
    /// owns a bed's ZDO when the bed is destroyed (hammer, damage, no support), with the bed's spawn point; the local
    /// character forgets that point too. A bed destroyed on another client is forgotten at the next respawn or load.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.RemoveCustomSpawnPoint))]
    public static class BedGonePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Vector3 point) => BedList.Forget(point);
    }
}
