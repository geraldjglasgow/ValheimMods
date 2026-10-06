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
            public bool Leader;
        }

        private static readonly List<ActivePin> active = new List<ActivePin>();

        public static void Add(Vector3 position, string label, Minimap.PinType type, float lifetimeSeconds, bool leaderColor = false)
        {
            if (Minimap.instance == null)
                return;
            Minimap.PinData pin = Minimap.instance.AddPin(position, type, label, save: false, isChecked: false, 0L);
            active.Add(new ActivePin { Pin = pin, ExpiresAt = Time.time + lifetimeSeconds, Leader = leaderColor });
        }

        /// <summary>After <c>Minimap.UpdatePins</c>, which makes the icons and sets them white on each pass.</summary>
        public static void Recolor()
        {
            for (int i = 0; i < active.Count; i++)
            {
                UnityEngine.UI.Image icon = active[i].Pin?.m_iconElement;
                if (icon == null)
                    continue;
                Color color = ColorHelper.MemberColor(active[i].Leader);
                if (icon.color != color)
                    icon.color = color;
            }
        }

        public static void Tick()
        {
            if (active.Count == 0)
                return;
            float now = Time.time;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (now < active[i].ExpiresAt)
                    continue;
                if (active[i].Pin != null && Minimap.instance != null)
                    Minimap.instance.RemovePin(active[i].Pin);
                active.RemoveAt(i);
            }
        }
    }
}
