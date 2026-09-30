using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A Wind Call arriving on a ship (<see cref="Keys.RpcWindCall"/>). The RPC is registered on every ship that has a
    /// ZDO, in Ship.Awake, and sent to everybody: each machine that has the ship loaded handles it once, the caller
    /// included. The ship's owner stores the called wind in the ship's ZDO (<see cref="WindCallState"/>), which a
    /// ship's owner always has, since the game hands a ship to a player aboard; every client aboard that ship is told
    /// who turned the wind, which way and for how long. A dedicated server that owns the ship stores it and says nothing.
    /// </summary>
    public static class WindCallReceive
    {
        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(Ship __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview != null && nview.GetZDO() != null)
                    nview.Register<Vector3, long>(Keys.RpcWindCall,
                        (sender, direction, caller) => Guard.Run("wind call", () => Receive(__instance, direction, caller)));
            }
        }

        private static void Receive(Ship ship, Vector3 direction, long caller)
        {
            direction.y = 0f;
            if (ship == null || !ship.m_nview.IsValid() || !SailingSkill.Active || direction.sqrMagnitude < 0.01f)
                return;
            direction.Normalize();
            float seconds = WindCallSettings.Duration.Value;
            if (ship.m_nview.IsOwner())
                WindCallState.Write(ship.m_nview.GetZDO(), direction, seconds);
            Player player = Player.m_localPlayer;
            if (player == null || Ship.GetLocalShip() != ship)
                return;
            string which = $"toward the {WindCallState.Compass(direction)} for {SkillPage.Duration(seconds)}";
            player.Message(MessageHud.MessageType.Center, Announcement(ship, player, caller, which));
        }

        /// <summary>"You turn the wind toward the east for 60 s", or the caller's name, or no name when they left the ship.</summary>
        private static string Announcement(Ship ship, Player local, long caller, string which)
        {
            if (caller == local.GetPlayerID())
                return $"You turn the wind {which}";
            Player who = ship.m_players.Find(player => player != null && player.GetPlayerID() == caller);
            return who != null ? $"{who.GetPlayerName()} turns the wind {which}" : $"The wind turns {which}";
        }
    }
}
