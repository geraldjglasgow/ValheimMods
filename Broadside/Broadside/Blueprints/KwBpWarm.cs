using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// When the local player spawns, the saved ships' hammer entries are read and priced one a frame
    /// (<see cref="KwBpEntries.Warm"/>), so taking up the hammer finds them ready: done at that moment, all fourteen of
    /// the user's ships took 88 ms in one frame (2026-10-27). Clients only.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class KwBpWarm
    {
        private static bool warming;

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside blueprint warm", Start, __instance);

        private static void Start(Player player)
        {
            if (player != Player.m_localPlayer || warming || !KwShipBody.Drawn)
                return;
            warming = true;
            KwBpRunner.Run("Broadside blueprint warm", KwBpEntries.Warm(), () => warming = false);
        }
    }
}
