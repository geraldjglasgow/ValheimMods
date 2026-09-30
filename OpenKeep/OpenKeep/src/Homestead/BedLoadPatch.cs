using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Game.FindSpawnPoint</c> prefix: after a death, with a bed to wake in, the game's load timer
    /// (<c>m_respawnWait</c>, compared with <c>m_respawnLoadDuration</c>) runs <see cref="BedWait.LoadSpeed"/> times as
    /// fast, and on a client of a server it waits for the bed's objects to have arrived (<see cref="BedWait.Hold"/>).
    /// The game still waits for the bed's area to be ready. Joining a world and logging back in are not after a death
    /// and keep the game's wait; so does the world start, which the game takes without that timer.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
    public static class BedLoadPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Game __instance, float dt)
        {
            PlayerProfile profile = __instance.GetPlayerProfile();
            if (!__instance.m_respawnAfterDeath || profile == null || !profile.HaveCustomSpawnPoint())
                return;
            bool fresh = __instance.m_respawnWait <= 0f;
            __instance.m_respawnWait += dt * (BedWait.LoadSpeed() - 1f);
            BedWait.Hold(__instance, dt, profile.GetCustomSpawnPoint(), fresh);
        }
    }
}
