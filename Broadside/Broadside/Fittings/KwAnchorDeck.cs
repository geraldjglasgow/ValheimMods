using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A deck as an anchor's cable sees it (<see cref="KwAnchorShip"/>): its slab under its top, over its outline, with its
    /// hatches' openings left out (<see cref="KwDeckHoles"/>), in its own kit frame. A cable over an opening is in the air;
    /// anywhere else it keeps over the top or under the slab.
    /// </summary>
    public sealed class KwAnchorDeck : KwAnchorSolid
    {
        /// <summary>How deep a deck's slab is under its top.</summary>
        public const float Slab = 0.12f;

        private readonly KwAnchorPose pose;
        private readonly System.Func<float, float> z;
        private readonly List<Vector2> edge;
        private readonly List<Rect> holes;

        public KwAnchorDeck(KwAnchorPose pose, KwDef def, List<Rect> holes) : base("the deck")
        {
            (this.pose, this.holes) = (pose, holes);
            z = KwMesh.DeckHeight(def);
            edge = def.Edge ?? KwShapes.DeckEdge(def.Stations, z, KwShapes.InsideWidth);
            float x0 = def.Stations[0].X, x1 = def.Stations[def.Stations.Count - 1].X, wide = 0.1f;
            foreach (Vector2 e in edge)
                wide = Mathf.Max(wide, e.y);
            float low = Mathf.Min(z(x0), z(x1)) - Slab, high = Mathf.Max(z(x0), z(x1));
            Round(pose, new Vector3(Mathf.Min(x0, x1), -wide, low), new Vector3(Mathf.Max(x0, x1), wide, high));
        }

        /// <summary>The deck's top over a point (windlass kit x, y) as a height there; null off it or over an opening.</summary>
        public float? Top(float x, float y)
        {
            Vector3 k = pose.Local(new Vector3(x, y, 0f));
            if (Mathf.Abs(k.y) >= Half(k.x))
                return null;
            foreach (Rect hole in holes)
            {
                if (hole.Contains(new Vector2(k.x, k.y)))
                    return null;
            }
            return pose.Out(new Vector3(k.x, k.y, z(k.x))).z;
        }

        public override bool Blocks(Vector3 p, float r, bool outboard)
        {
            float? top = Top(p.x, p.y);
            return top.HasValue && p.z < top.Value + r && p.z > top.Value - Slab - r;
        }

        // The deck's half width at x along it; -1 past its ends.
        private float Half(float x)
        {
            for (int i = 0; i + 1 < edge.Count; i++)
            {
                Vector2 a = edge[i], b = edge[i + 1];
                if (x >= Mathf.Min(a.x, b.x) && x <= Mathf.Max(a.x, b.x))
                    return Mathf.Abs(b.x - a.x) < 1e-4f ? Mathf.Max(a.y, b.y) : Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));
            }
            return -1f;
        }
    }
}
