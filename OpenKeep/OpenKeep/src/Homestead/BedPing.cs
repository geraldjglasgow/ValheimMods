using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The nearest bed pinged when the choice of bed opens (asked 2026-10-04: "ping the closest bed when the map opens,
    /// and have the ping gold ... still have it underneath the click area so you can click it fast"): the game's own
    /// ping marker (<c>PinType.Ping</c>, the one a map ping shows) as a local, unsaved pin at that bed (made directly, as
    /// <see cref="BedPins"/> makes its pins), twice the size
    /// and pulsing, tinted gold after every pin update (the game sets every icon white there). It lies under the bed's
    /// icon and click bubble, which are drawn above every other icon, so a click on it picks the bed. Removed when the
    /// choice ends; nothing is sent to other players.
    /// </summary>
    public static class BedPing
    {
        private static readonly Color Gold = new Color(1f, 0.76f, 0.2f, 1f);

        private static Minimap.PinData pin;
        private static bool wasHidden;

        public static void Show(Minimap map, Vector3 bed)
        {
            Hide(map);
            int ping = (int)Minimap.PinType.Ping;
            if (ping < map.m_visibleIconTypes.Length)
            {
                wasHidden = !map.m_visibleIconTypes[ping];
                map.m_visibleIconTypes[ping] = true;
            }
            pin = new Minimap.PinData
            {
                m_type = Minimap.PinType.Ping,
                m_name = "",
                m_pos = bed,
                m_icon = map.GetSprite(Minimap.PinType.Ping),
                m_save = false,
                m_doubleSize = true,
                m_animate = true,
            };
            map.m_pins.Add(pin);
            map.m_pinUpdateRequired = true;
        }

        public static void Hide(Minimap map)
        {
            if (pin == null)
                return;
            if (map != null)
            {
                map.RemovePin(pin);
                int ping = (int)Minimap.PinType.Ping;
                if (wasHidden && ping < map.m_visibleIconTypes.Length)
                    map.m_visibleIconTypes[ping] = false;
            }
            pin = null;
            wasHidden = false;
        }

        /// <summary>After every pin update: the ping gold again.</summary>
        public static void Tint()
        {
            if (pin != null && pin.m_iconElement != null)
                pin.m_iconElement.color = Gold;
        }
    }
}
