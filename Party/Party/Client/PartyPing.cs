using UnityEngine;
using Party.Server;

namespace Party.Client
{
    /// <summary>Party-only map pings, routed here by <see cref="Hooks.PingPatch"/>.</summary>
    public static class PartyPing
    {
        // How long a ping pin lasts (roughly vanilla's shout-ping lifetime).
        private const float PinLifetimeSeconds = 60f;

        public static void Send(Vector3 position)
        {
            if (ZRoutedRpc.instance != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcPing, position);
        }

        public static void OnDeliver(string senderName, Vector3 position) =>
            TempPartyPins.Add(position, $"{senderName} (party)", Minimap.PinType.Ping, PinLifetimeSeconds);
    }
}
