using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A routed RPC for something only players near a spot can see (a cast's flash, a heal's motes), sent to their
    /// machines alone rather than to everybody on the server. The players are the server's player list, which every client
    /// has; each one's position is their character ZDO's, on whichever machine has that ZDO (every machine near them), and
    /// the RPC goes to that ZDO's owner (a player always owns their own). A player whose ZDO this machine does not have is
    /// far away. The sending machine gets it too, at once, unless it draws its own (a heal's flight, whose landing it
    /// handles). The same rule as GrindstoneSkills' <c>NearbyRpc</c>; the two are to become one library.
    /// </summary>
    public static class NearbyCast
    {
        private const float Margin = 20f;

        public static void Send(Vector3 position, float range, bool includeSelf, string method, params object[] parameters)
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null)
                return;
            long self = ZNet.GetUID();
            if (includeSelf)
                rpc.InvokeRoutedRPC(self, method, parameters);
            if (ZNet.instance == null || ZDOMan.instance == null)
                return;
            float reach = (range + Margin) * (range + Margin);
            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
            {
                long peer = PeerNear(info.m_characterID, position, reach);
                if (peer != 0L && peer != self)
                    rpc.InvokeRoutedRPC(peer, method, parameters);
            }
        }

        private static long PeerNear(ZDOID character, Vector3 position, float reach)
        {
            ZDO zdo = character.IsNone() ? null : ZDOMan.instance.GetZDO(character);
            return zdo != null && (zdo.GetPosition() - position).sqrMagnitude <= reach ? zdo.GetOwner() : 0L;
        }
    }
}
