using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Player.OnDeath</c> postfix on the dying player's own client (the game runs the method only there): after
    /// the game has recorded the death point, the nearest owned bed becomes the spawn point. Here and not at the
    /// respawn, because the player and its custom data exist only until the game's respawn request destroys them.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static class BedDeathPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !BedSettings.NearestBedRespawn.Value)
                return;
            BedRespawn.Choose(__instance.transform.position);
        }
    }
}
