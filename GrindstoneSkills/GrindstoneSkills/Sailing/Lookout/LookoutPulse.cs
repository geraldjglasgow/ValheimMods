using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A lookout pulse arriving on a ship (<see cref="Keys.RpcLookout"/>). The RPC is registered on every ship that has
    /// a ZDO, in Ship.Awake, and sent to everybody: each machine that has the ship loaded handles it once, the sender
    /// included. Every client with a player draws the ring and plays the ping from the ship, so anyone nearby sees
    /// that a pulse went out; the players aboard that ship also get the name tags of the enemies it reached and a
    /// message with the count. A dedicated server has no player and does nothing.
    /// </summary>
    public static class LookoutPulse
    {
        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(Ship __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview != null && nview.GetZDO() != null)
                    nview.Register(Keys.RpcLookout, sender => Guard.Run("lookout pulse", () => Receive(__instance)));
            }
        }

        private static void Receive(Ship ship)
        {
            Player player = Player.m_localPlayer;
            if (ship == null || player == null)
                return;
            Vector3 center = ship.transform.position;
            float radius = LookoutSettings.Radius.Value;
            LookoutRing.Draw(center, radius);
            LookoutSound.Play(center);
            if (Ship.GetLocalShip() != ship)
                return;
            int found = LookoutReveal.Reveal(player, center, radius, LookoutSettings.Duration.Value);
            player.Message(MessageHud.MessageType.TopLeft, Report(found, radius));
        }

        private static string Report(int found, float radius)
        {
            if (found == 0)
                return $"Lookout: no enemies within {radius:0} m";
            return found == 1 ? $"Lookout: 1 enemy within {radius:0} m" : $"Lookout: {found} enemies within {radius:0} m";
        }
    }
}
