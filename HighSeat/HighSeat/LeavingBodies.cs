using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// A player who leaves leaves their body behind for a moment: the server takes over every object of a gone peer and
    /// only queues its destruction (ZDOMan.RemoveOrphanNonPersistentZDOS, DestroyZDO, sent on a later update), so until
    /// then the server owns a Player that is not its own, and the game's Player.LateUpdate sets the server's reference
    /// position (where the host is, for every area pass) to wherever that body stands. In the game the body is near the
    /// host and the jump is small; with every player's area loaded (<see cref="PlayerAreas"/>) it can be across the world,
    /// and the next pass took that area for the host's own and let the host's real area go, the host's own player with it
    /// (measured 2026-11-01: 50 players spread out leaving together left the host on an endless loading screen). So on the
    /// server while it runs creatures, a Player it owns that is not the local player skips LateUpdate (its emote and
    /// placement ghost are nobody's); the game's own FixedUpdate destroys it as before ("Destroying old local player").
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.LateUpdate))]
    internal static class LeavingBodies
    {
        private static bool Prefix(Player __instance)
        {
            if (!ServerRun.Active || ReferenceEquals(__instance, Player.m_localPlayer))
            {
                return true;
            }
            ZNetView view = __instance.m_nview;
            return view == null || !view.IsValid() || !view.IsOwner();
        }
    }
}
