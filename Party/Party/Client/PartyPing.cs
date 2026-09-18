using System.Collections.Generic;
using UnityEngine;
using Party.Server;

namespace Party.Client
{
    /// <summary>
    /// Party-only map pings: <see cref="Hooks.PingPatch"/> intercepts a modifier-held vanilla ping and routes it
    /// here instead. The pin is temporary, like a vanilla shout ping, and colored for the party.
    /// </summary>
    public static class PartyPing
    {
        // How long a party ping pin stays on the map: a judgement call, roughly vanilla's own shout-ping lifetime.
        private const float PinLifetimeSeconds = 60f;

        private sealed class ActivePin
        {
            public Minimap.PinData Pin;
            public float ExpiresAt;
        }

        private static readonly List<ActivePin> active = new List<ActivePin>();

        public static void Send(Vector3 position)
        {
            if (ZRoutedRpc.instance != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcPing, position);
        }

        public static void OnDeliver(string senderName, Vector3 position)
        {
            if (Minimap.instance == null)
                return;
            Minimap.PinData pin = Minimap.instance.AddPin(position, Minimap.PinType.Ping, $"{senderName} (party)", save: false, isChecked: false, 0L);
            if (pin.m_iconElement != null)
                pin.m_iconElement.color = ColorHelper.Parse(PartyConfig.PartyColor.Value);
            active.Add(new ActivePin { Pin = pin, ExpiresAt = Time.time + PinLifetimeSeconds });
        }

        public static void Tick(float deltaTime)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (Time.time < active[i].ExpiresAt)
                    continue;
                if (active[i].Pin != null && Minimap.instance != null)
                    Minimap.instance.RemovePin(active[i].Pin);
                active.RemoveAt(i);
            }
        }
    }
}
