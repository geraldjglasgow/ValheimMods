using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// zones: the 64 m zone squares (ZoneSystem.GetZone) the area touches, draped on the ground: white where the zone is
    /// loaded on this machine, grey where not, and the local player's zone (or the area's centre's, without a player)
    /// in thicker cyan, drawn last so it shows over its neighbours. The reply names that zone and the zone counts.
    /// </summary>
    internal static class ZoneDrawer
    {
        private const float Size = 64f;
        private const int Samples = 16;
        private static readonly Color Loaded = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color Unloaded = new Color(0.5f, 0.5f, 0.5f, 0.45f);
        private static readonly Color Mine = new Color(0.2f, 1f, 1f, 0.95f);

        internal static void Draw(OverlayArea area, Category into)
        {
            Vector2s low = ZoneSystem.GetZone(area.Centre - new Vector3(area.Radius, 0f, area.Radius));
            Vector2s high = ZoneSystem.GetZone(area.Centre + new Vector3(area.Radius, 0f, area.Radius));
            Vector2s mine = ZoneSystem.GetZone(Player.m_localPlayer ? Player.m_localPlayer.transform.position : area.Centre);
            for (int x = low.x; x <= high.x; x++)
            {
                for (int y = low.y; y <= high.y; y++)
                {
                    var zone = new Vector2s(x, y);
                    if (zone == mine) continue;
                    bool loaded = ZoneSystem.instance && ZoneSystem.instance.IsZoneLoaded(zone);
                    into.Count(loaded ? "loaded" : "not_loaded");
                    into.Lines.Add(Outline(zone, area.Centre.y), loaded ? Loaded : Unloaded, 0.05f);
                }
            }
            into.Note("player_zone", $"{mine.x},{mine.y}");
            into.Lines.Add(Outline(mine, area.Centre.y), Mine, 0.1f);
        }

        // The zone's square, Samples points a side, each on the ground under it (or at the given height where none is).
        private static Vector3[] Outline(Vector2s zone, float height)
        {
            Vector3 centre = ZoneSystem.GetZonePos(zone);
            Vector3[] corners = Shapes.Square(centre, Size / 2f);
            var points = new Vector3[Samples * 4 + 1];
            for (int i = 0; i < points.Length; i++)
            {
                int side = Mathf.Min(i / Samples, 3);
                Vector3 point = Vector3.Lerp(corners[side], corners[side + 1], (i - side * Samples) / (float)Samples);
                points[i] = new Vector3(point.x, OverlayArea.Ground(point, height) + 0.15f, point.z);
            }
            return points;
        }
    }
}
