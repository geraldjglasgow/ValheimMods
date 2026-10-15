using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The ground under every player's area, on the server: the game builds terrain (heightmaps, colliders) only around
    /// the server's own position and lets a zone go once nobody pokes it for a few seconds. Just before that check
    /// (ZoneSystem.UpdateTTL, ten times a second) every ready player's zones are poked or built too, one new zone per player
    /// per pass as the game does, so creatures out there have ground to stand and path on.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.UpdateTTL))]
    internal static class ZoneKeeping
    {
        private static void Prefix(ZoneSystem __instance)
        {
            if (!ServerRun.Active)
            {
                return;
            }
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady())
                {
                    __instance.CreateLocalZones(peer.GetRefPos());
                }
            }
        }
    }
}
