using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// Where one redraw looks: a sphere of radius round the player, or round the free-fly camera while it is on (or the
    /// camera while the player is dead), with the networked objects in it found once and shared by the categories.
    /// </summary>
    internal sealed class OverlayArea
    {
        internal readonly Vector3 Centre;
        internal readonly float Radius;
        internal readonly int Layers;
        internal readonly string From;
        private List<ZNetView> views;

        private OverlayArea(Vector3 centre, float radius, int layers, string from)
        {
            (Centre, Radius, Layers, From) = (centre, radius, layers, from);
        }

        /// <summary>The area round the player or the camera; null when there is neither.</summary>
        internal static OverlayArea Around(float radius, int layers)
        {
            GameCamera camera = GameCamera.instance;
            if (camera && camera.m_freeFly) return new OverlayArea(camera.transform.position, radius, layers, "free camera");
            if (Player.m_localPlayer) return new OverlayArea(Player.m_localPlayer.transform.position, radius, layers, "player");
            return camera ? new OverlayArea(camera.transform.position, radius, layers, "camera") : null;
        }

        /// <summary>True when the point is within the radius (plus margin, for things whose reach crosses into it).</summary>
        internal bool Holds(Vector3 point, float margin = 0f) => (point - Centre).sqrMagnitude <= (Radius + margin) * (Radius + margin);

        /// <summary>The loaded networked objects in the area, for components the game keeps no list of.</summary>
        internal List<ZNetView> Views => views ?? (views = Scan());

        private List<ZNetView> Scan()
        {
            var found = new List<ZNetView>();
            if (!ZNetScene.instance) return found;
            foreach (ZNetView view in ZNetScene.instance.m_instances.Values)
            {
                if (view && view.IsValid() && Holds(view.transform.position)) found.Add(view);
            }
            return found;
        }

        /// <summary>
        /// The ground under a point (the heightmap, no physics), or the given height where there is none or it is a long
        /// way off (a dungeon sits thousands of metres above the world's ground).
        /// </summary>
        internal static float Ground(Vector3 point, float fallback) =>
            Heightmap.GetHeight(point, out float height) && Mathf.Abs(height - fallback) < 1000f ? height : fallback;
    }
}
