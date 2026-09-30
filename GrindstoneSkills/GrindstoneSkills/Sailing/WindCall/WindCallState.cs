using System;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A ship's called wind, kept in the ship's ZDO so every client aboard reads the same one, a player who boards
    /// while it blows included, and it outlives a change of owner or a relog: the flat direction it blows toward
    /// (<see cref="Keys.WindCallDirection"/>) and the world time it ends (<see cref="Keys.WindCallUntil"/>). Only the
    /// ship's owner writes it. Read every physics step while the local player is aboard, so the keys are hashed once.
    /// </summary>
    public static class WindCallState
    {
        private static readonly int DirectionHash = Keys.WindCallDirection.GetStableHashCode();
        private static readonly int UntilHash = Keys.WindCallUntil.GetStableHashCode();

        private static readonly string[] Points =
        {
            "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west",
        };

        /// <summary>On the ship's owner: the wind blows toward <paramref name="direction"/> for the next <paramref name="seconds"/>.</summary>
        public static void Write(ZDO zdo, Vector3 direction, float seconds)
        {
            DateTime until = ZNet.instance.GetTime().AddSeconds(seconds);
            zdo.Set(DirectionHash, direction);
            zdo.Set(UntilHash, until.Ticks);
        }

        /// <summary>The direction the ship's called wind blows toward, when one blows now.</summary>
        public static bool TryRead(Ship ship, out Vector3 direction)
        {
            direction = Vector3.zero;
            ZNetView nview = ship.m_nview;
            ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            if (zdo == null || ZNet.instance == null || zdo.GetLong(UntilHash) <= ZNet.instance.GetTime().Ticks)
                return false;
            direction = zdo.GetVec3(DirectionHash, Vector3.zero);
            return direction.sqrMagnitude > 0.5f;
        }

        /// <summary>The compass point a flat direction points to, the world's +z being north as on the map: "north-east".</summary>
        public static string Compass(Vector3 direction)
        {
            float degrees = Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360f);
            return Points[Mathf.RoundToInt(degrees / 45f) % Points.Length];
        }
    }
}
