using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cannons' reload lines, on the captain's own screen while he steers a launched Broadside ship with cannons aboard
    /// (<see cref="KwBroadside.Tick"/>, the same cached list of cannons aboard): one line for each side that has cannons,
    /// "Port guns 3/3 ready" and "Starboard guns 1/3, 4.2 s, 1 empty" (<see cref="KwReloadCount"/>,
    /// <see cref="KwArmWords.Side"/>), worked out every <see cref="Every"/> seconds, so the seconds count down smoothly, and
    /// written only when they changed. On Broadside's own ship panel (<see cref="KwShipPanel"/>, rows "cannons.port" and
    /// "cannons.starboard", under the ship's health), which shows aboard and hides with the large map. Hidden off the helm
    /// or without cannons. Read from the cannons' ZDOs alone: nothing is sent or saved, and they are right whoever owns the
    /// cannons. On a game that draws only (the broadside's ticker).
    /// </summary>
    public static class KwReloadRows
    {
        /// <summary>Seconds between two counts (10 a second).</summary>
        public const float Every = 0.1f;

        private static readonly string[] Ids = { "cannons.port", "cannons.starboard" };
        private static readonly long[] written = { -1L, -1L };
        private static float next;
        private static bool shown;

        /// <summary>Each frame: the lines counted and written when due, or hidden off the helm or without cannons.</summary>
        public static void Tick(Ship? ship, List<KwArm>? aboard)
        {
            if (ship == null || aboard == null || aboard.Count == 0)
            {
                Hide();
                return;
            }
            if (shown && Time.time < next)
                return;
            (next, shown) = (Time.time + Every, true);
            KwReloadCount.Count(ship.transform, aboard, out KwSideCount port, out KwSideCount starboard);
            Write(0, port);
            Write(1, starboard);
        }

        // A side's line: written when its count changed, kept shown otherwise, hidden while the side has no cannons.
        private static void Write(int side, in KwSideCount c)
        {
            KwPanelLine row = KwShipPanel.Row(Ids[side]);
            if (c.All == 0)
            {
                row.Hide();
                written[side] = -1L;
                return;
            }
            if (c.Shown == written[side] && row.Shown)
            {
                row.Keep();
                return;
            }
            written[side] = c.Shown;
            bool port = side == 0;
            row.Set(KwArmWords.SideLabel(port), KwArmWords.Side(c), KwArmWords.SideLabel(port), KwArmWords.SideTip(port));
        }

        // Off the helm: both lines hidden once.
        private static void Hide()
        {
            if (!shown)
                return;
            shown = false;
            for (int side = 0; side < Ids.Length; side++)
            {
                KwShipPanel.Row(Ids[side]).Hide();
                written[side] = -1L;
            }
        }
    }
}
