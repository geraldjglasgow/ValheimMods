using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// How the bed icons look (the user's call, 2026-09-30): yellow on the minimap and the large map wherever OpenKeep
    /// shows the beds (<see cref="BedPins.Showing"/>), so they stand out from the other pins, and during the choice of
    /// bed every bed icon twice the size and pulsing, with the game's own <c>m_doubleSize</c> and <c>m_animate</c> (as
    /// its event pins), and drawn on top of every other icon on the map (asked 2026-10-04, "so it's easier to click"):
    /// each bed icon gets a canvas of its own sorted just above the HUD's, since the game makes an icon again whenever
    /// it is moved out of its pin root, and another mod's icon layer (Wayfare's portals) keeps itself last under the
    /// map. The game sets every icon white on each pin update, so the tint follows it (<see cref="BedPinLookPatch"/>).
    /// The game sizes an icon only when it makes it and while it pulses, so at the end of the choice the icons are made
    /// again at the normal size, and without the canvas.
    /// </summary>
    public static class BedPinLook
    {
        private static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);

        /// <summary>Above the HUD canvas the map belongs to (400 in the game), below the inventory (600).</summary>
        private const int AboveMap = 1;
        private static readonly List<Minimap.PinData> enlarged = new List<Minimap.PinData>();

        public static void Tint(Minimap map)
        {
            if (!BedPins.Showing)
                return;
            foreach (Minimap.PinData pin in BedPins.Drawn)
                Tint(pin);
            Tint(map.m_spawnPointPin);
        }

        /// <summary>Every frame of the choice, so a bed icon made during it is enlarged too.</summary>
        public static void Enlarge(Minimap map)
        {
            foreach (Minimap.PinData pin in BedPins.Drawn)
                Enlarge(pin);
            Enlarge(map.m_spawnPointPin);
        }

        public static void Restore(Minimap map)
        {
            foreach (Minimap.PinData pin in enlarged)
            {
                pin.m_doubleSize = false;
                pin.m_animate = false;
                if (map != null)
                    map.DestroyPinMarker(pin);
            }
            enlarged.Clear();
        }

        /// <summary>The icon's own override-sorting canvas and its click bubble; checked every frame of the choice, as the game remakes icons.</summary>
        private static void OnTop(Minimap.PinData pin)
        {
            RectTransform icon = pin != null ? pin.m_uiElement : null;
            if (icon == null || icon.GetComponent<Canvas>() != null)
                return;
            Canvas map = icon.GetComponentInParent<Canvas>();
            Canvas own = icon.gameObject.AddComponent<Canvas>();
            own.overrideSorting = true;
            if (map != null)
            {
                own.sortingLayerID = map.rootCanvas.sortingLayerID;
                own.sortingOrder = map.rootCanvas.sortingOrder + AboveMap;
            }
            BedBubble.Add(icon, Minimap.instance);
        }

        private static void Tint(Minimap.PinData pin)
        {
            if (pin != null && pin.m_iconElement != null)
                pin.m_iconElement.color = Yellow;
        }

        private static void Enlarge(Minimap.PinData pin)
        {
            OnTop(pin);
            if (pin == null || enlarged.Contains(pin))
                return;
            pin.m_doubleSize = true;
            pin.m_animate = true;
            enlarged.Add(pin);
        }
    }
}
