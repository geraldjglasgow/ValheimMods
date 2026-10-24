using HarmonyLib;
using PatchGuard;
using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// A Fog Lift arriving on a ship (<see cref="Keys.RpcFogLift"/>). The RPC is registered on every ship that has a ZDO,
    /// in Ship.Awake, and sent to everybody: each machine that has the ship loaded handles it once, the caller included.
    /// The ship's owner stores the end time in the ship's ZDO (<see cref="FogLiftState"/>), which a ship's owner always
    /// has, since the game hands a ship to a player aboard; every client aboard that ship is told who lifted the fog and
    /// for how long, in their own language. A dedicated server that owns the ship stores it and says nothing.
    /// </summary>
    public static class FogLiftReceive
    {
        [HarmonyPatch(typeof(Ship), nameof(Ship.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(Ship __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview != null && nview.GetZDO() != null)
                    nview.Register<long>(Keys.RpcFogLift, (sender, caller) => Guard.Run("fog lift", () => Receive(__instance, caller)));
            }
        }

        private static void Receive(Ship ship, long caller)
        {
            if (ship == null || !ship.m_nview.IsValid() || !SailingSkill.Active)
                return;
            if (ship.m_nview.IsOwner())
                FogLiftState.Write(ship.m_nview.GetZDO(), FogLiftInput.Duration);
            Player player = Player.m_localPlayer;
            if (player == null || Ship.GetLocalShip() != ship)
                return;
            player.Message(MessageHud.MessageType.Center, Announcement(ship, player, caller, SkillPage.Duration(FogLiftInput.Duration)));
        }

        /// <summary>"You lift the fog for 2 min", or the caller's name, or no name when they left the ship.</summary>
        private static string Announcement(Ship ship, Player local, long caller, string time)
        {
            if (caller == local.GetPlayerID())
                return Lang.Format("gs_sail_foglift_you", time);
            Player who = ship.m_players.Find(player => player != null && player.GetPlayerID() == caller);
            return who != null ? Lang.Format("gs_sail_foglift_other", who.GetPlayerName(), time) : Lang.Format("gs_sail_foglift_nobody", time);
        }
    }
}
