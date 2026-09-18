using System.Collections.Generic;
using UnityEngine;

namespace Party.Client
{
    /// <summary>Temporary, party-colored map pins that remove themselves after a while. Used by pings and death notices.</summary>
    public static class TempPartyPins
    {
        private sealed class ActivePin
        {
            public Minimap.PinData Pin;
            public float ExpiresAt;
        }

        private static readonly List<ActivePin> active = new List<ActivePin>();

        public static void Add(Vector3 position, string label, Minimap.PinType type, float lifetimeSeconds, bool leaderColor = false)
        {
            if (Minimap.instance == null)
                return;
            Minimap.PinData pin = Minimap.instance.AddPin(position, type, label, save: false, isChecked: false, 0L);
            if (pin.m_iconElement != null)
                pin.m_iconElement.color = ColorHelper.Parse(leaderColor ? PartyConfig.LeaderColor.Value : PartyConfig.PartyColor.Value);
            active.Add(new ActivePin { Pin = pin, ExpiresAt = Time.time + lifetimeSeconds });
        }

        public static void Tick()
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
