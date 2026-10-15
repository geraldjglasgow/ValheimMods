using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// "Outside the active area" on the server means outside every player's area, since the server now loads them all
    /// (<see cref="PlayerAreas"/>). The game asks it about the server's own position only (ZNetScene.OutsideActiveArea),
    /// which would keep a nest (SpawnArea) near a player from ever spawning on the server. Its other callers, falling
    /// objects and building wear, act only for an object's owner, so they change only for what the server runs.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OutsideActiveArea), typeof(Vector3))]
    internal static class ActiveArea
    {
        private static void Postfix(Vector3 point, ref bool __result)
        {
            if (!__result || !ServerRun.Active)
            {
                return;
            }
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady() && ZNetScene.InActiveArea(point, peer.GetRefPos()))
                {
                    __result = false;
                    return;
                }
            }
        }
    }
}
