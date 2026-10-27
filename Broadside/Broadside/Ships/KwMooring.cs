using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How an anchored ship rides to its anchor (<see cref="KwShipAnchor"/>), as accelerations, so a ship of any size moves
    /// the same: its speed over the ground is dragged away hard (<see cref="Drag"/>, its swinging round too,
    /// <see cref="Turn"/>); where its cable leaves it (its hawse: the fairlead, or the stem head) is pulled toward the anchor
    /// by a soft spring once it is more than <see cref="Slack"/> from it, and the wind pushes on the whole ship. So the ship
    /// lies downwind of its anchor with its hawse into the wind (bow on for a fairlead at the bow, more across the wind the
    /// further aft it is), a metre or two past the slack in a full wind, and swings round as the wind turns: at full wind
    /// the push and the spring meet 1.25 m past the slack, and the drag damps the spring past critical, so it settles
    /// without bouncing.
    /// </summary>
    public static class KwMooring
    {
        /// <summary>How far the hawse may wander from the anchor before the cable pulls (m).</summary>
        public const float Slack = 3f;

        /// <summary>The cable's pull per metre past the slack (m/s² per m), the drag on the speed over the ground and on
        /// the swing (per second), and the wind's push at full wind (m/s²).</summary>
        public const float Spring = 0.2f, Drag = 1.2f, Turn = 0.8f, Windage = 0.25f;

        /// <summary>The cable's pull on the hawse toward the anchor, level, per kilogram of ship: none within the slack.</summary>
        public static Vector3 Pull(Vector3 hawse, Vector3 anchor)
        {
            Vector3 off = new Vector3(anchor.x - hawse.x, 0f, anchor.z - hawse.z);
            float reach = off.magnitude;
            return reach <= Slack ? Vector3.zero : off * ((reach - Slack) * Spring / reach);
        }

        /// <summary>The share of a speed a step of dt keeps under a drag of this rate.</summary>
        public static float Kept(float rate, float dt) => Mathf.Exp(-rate * dt);

        /// <summary>The wind's push on the ship, level, per kilogram: its way times its strength (0 to 1).</summary>
        public static Vector3 Wind(Vector3 way, float strength) => new Vector3(way.x, 0f, way.z) * (Mathf.Clamp01(strength) * Windage);
    }
}
