using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Game.FindSpawnPoint</c> prefix and postfix. Once the spawn point's area is ready the game looks for a
    /// loaded bed of this player within a metre of it (<c>FindBedNearby</c>, <c>Bed.IsCurrent</c>); when there is
    /// none (the bed is gone, or it now belongs to someone else) it clears the point, restarts its load wait and
    /// falls back to the world start on the next call. The postfix sees that clear: the bed is forgotten, and with
    /// the setting on the next nearest bed of the death becomes the point, so the game waits for that area instead.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
    public static class BedRespawnPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Game __instance, ref Vector3? __state)
        {
            PlayerProfile profile = __instance.GetPlayerProfile();
            __state = profile != null && profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : (Vector3?)null;
        }

        [HarmonyPostfix]
        public static void Postfix(Game __instance, bool __result, Vector3? __state)
        {
            PlayerProfile profile = __instance.GetPlayerProfile();
            if (__result || !__state.HasValue || profile == null || profile.HaveCustomSpawnPoint())
                return;
            Vector3 gone = __state.Value;
            BedList.Forget(gone);
            if (!BedSettings.NearestBedRespawn.Value || !BedRespawn.TryNext(gone, out Vector3 next))
                return;
            profile.SetCustomSpawnPoint(next);
            Plugin.Log.LogInfo($"OpenKeep: no bed of yours at {BedPoints.Format(gone)}; trying the bed at {BedPoints.Format(next)}");
        }
    }
}
