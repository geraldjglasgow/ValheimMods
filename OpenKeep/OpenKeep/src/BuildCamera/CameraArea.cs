using System;
using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Where the camera may be: around every loaded crafting station (<c>CraftingStation.m_allStations</c>, placement
    /// ghosts excluded by the game), its build range with extensions (<c>GetStationBuildRange</c>) times Range
    /// Multiplier, measured flat as the game measures build range, and as far above or below the station. Read only: no
    /// station's range, marker or base area is changed, so raids and comfort keep the station's real area. While
    /// <see cref="AroundPlayer"/> says so (an entry of the hammer's Blueprints tab selected), the area is instead a ball
    /// round the local player: the camera goes at most <see cref="PlayerRange"/> m from the body in any direction.
    /// </summary>
    public static class CameraArea
    {
        /// <summary>A clamped point lands just inside the edge, so the next frame counts it as inside.</summary>
        private const float Inset = 0.99f;

        /// <summary>How far the blueprint camera may go from the player's body, metres (the user's cap).</summary>
        public const float PlayerRange = 50f;

        /// <summary>Set by the Blueprints module: true while the camera's area is round the player, not the stations.</summary>
        public static Func<bool> AroundPlayer = () => false;

        public static bool Contains(Vector3 point)
        {
            if (AroundPlayer())
                return Player.m_localPlayer != null && FromPlayer(Player.m_localPlayer.transform.position, point) <= 0f;
            foreach (CraftingStation station in CraftingStation.m_allStations)
            {
                if (station != null && Outside(station, point) <= 0f)
                    return true;
            }
            return false;
        }

        /// <summary>The point itself when inside an area, else the nearest point of the nearest area.</summary>
        public static Vector3 Clamp(Vector3 point)
        {
            if (AroundPlayer() && Player.m_localPlayer != null)
                return IntoPlayer(Player.m_localPlayer.transform.position, point);
            return ClampToStations(point);
        }

        private static Vector3 ClampToStations(Vector3 point)
        {
            CraftingStation nearest = null;
            float best = float.MaxValue;
            foreach (CraftingStation station in CraftingStation.m_allStations)
            {
                if (station == null)
                    continue;
                float outside = Outside(station, point);
                if (outside <= 0f)
                    return point;
                if (outside < best)
                {
                    best = outside;
                    nearest = station;
                }
            }
            return nearest != null ? Into(nearest, point) : point;
        }

        private static float Range(CraftingStation station) => station.GetStationBuildRange() * CameraSettings.RangeMultiplier.Value;

        /// <summary>How far the point lies outside the station's area: 0 or less inside; a station without build range has none.</summary>
        private static float Outside(CraftingStation station, Vector3 point)
        {
            float range = Range(station);
            if (range <= 0f)
                return float.MaxValue;
            Vector3 centre = station.transform.position;
            float flat = Utils.DistanceXZ(centre, point) - range;
            float height = Mathf.Abs(point.y - centre.y) - range;
            if (flat <= 0f && height <= 0f)
                return Mathf.Max(flat, height);
            return Mathf.Max(flat, 0f) + Mathf.Max(height, 0f);
        }

        /// <summary>How far the point lies outside the ball round the player: 0 or less inside.</summary>
        private static float FromPlayer(Vector3 centre, Vector3 point) => Vector3.Distance(centre, point) - PlayerRange;

        /// <summary>The point pulled back along its line to the player until it is just inside the ball.</summary>
        private static Vector3 IntoPlayer(Vector3 centre, Vector3 point) => centre + Vector3.ClampMagnitude(point - centre, PlayerRange * Inset);

        private static Vector3 Into(CraftingStation station, Vector3 point)
        {
            float range = Range(station) * Inset;
            Vector3 centre = station.transform.position;
            Vector2 flat = Vector2.ClampMagnitude(new Vector2(point.x - centre.x, point.z - centre.z), range);
            float y = Mathf.Clamp(point.y, centre.y - range, centre.y + range);
            return new Vector3(centre.x + flat.x, y, centre.z + flat.y);
        }
    }
}
