using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Player.OnSpawned(bool)</c> postfix: after a death the player wakes standing. The game plays its getting-up
    /// animation on every new player: <c>Player.Awake</c> reads the player's own ZDO flag <c>wakeup</c> (true when
    /// unset), sets the animator's <c>wakeup</c> and clears both a second later; the animation state is a cutscene, so
    /// the player cannot move until it ends. <c>Game.SpawnPlayer</c> calls <c>OnSpawned</c> in the same frame as the
    /// player's <c>Awake</c>, before the animator's first update, so clearing the flag, the bool and the timer here
    /// skips it. The ZDO flag goes out with the player's first sync, so other clients see the player standing too.
    /// Only after a death (<c>Game.m_respawnAfterDeath</c>): logging in keeps the game's wake-up.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class BedStandPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, bool spawnValkyrie)
        {
            if (__instance != Player.m_localPlayer || spawnValkyrie || !BedSettings.StandUpOnRespawn.Value)
                return;
            if (Game.instance == null || !Game.instance.m_respawnAfterDeath || __instance.m_nview.GetZDO() == null)
                return;
            __instance.m_wakeupTimer = -1f;
            __instance.m_animator.SetBool("wakeup", false);
            __instance.m_nview.GetZDO().Set(ZDOVars.s_wakeup, false);
        }
    }
}
