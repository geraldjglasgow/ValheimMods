using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A corner post's solid (<see cref="KwCabinPosts"/>) from its rings, each four corners in plan at one height from its
    /// foot up (kit axes): its sides between each ring and the next, and a cap a little inside each end, so the cap under a
    /// roof's boards never fights them and two posts meeting end to end leave no face between them. The wood's grain runs up
    /// the post: u (the plank wood's grain) is the height over the keel line, two metres a texture as the dock set's solids;
    /// v is the place across each face from the post's middle (along the cabin) or from the keel line (across it), finer by
    /// as much as the game's material stretches v more than u (its tiling is -0.56 by 0.12), so the grain shows at the
    /// wood's own proportions. Both are the same in every cabin over the same keel line, so a post carried on up through a
    /// cabin built on another's roof continues its grain without a seam.
    /// </summary>
    internal static class KwCabinPostMesh
    {
        private const float Along = 0.5f, Across = Along * 0.56f / 0.12f;

        /// <summary>How far inside its ends a post's caps sit.</summary>
        public const float Under = 0.005f;

        /// <summary>The post from its rings (at least two), its middle along the cabin at <paramref name="middle"/>; its top cap lowered by sink more.</summary>
        public static void Draw(MeshBuilder b, List<Vector3[]> rings, float middle, float sink)
        {
            if (rings.Count < 2)
                return;
            List<Vector3[]> turned = rings.ConvertAll(Anticlockwise);
            for (int i = 0; i + 1 < turned.Count; i++)
            {
                for (int k = 0; k < 4; k++)
                    Side(b, turned[i], turned[i + 1], k, middle);
            }
            Cap(b, turned[turned.Count - 1], -Under - sink, true, middle);
            Cap(b, turned[0], Under, false, middle);
        }

        // Side k between two rings, from corner k to the next: across the cabin it takes v from the keel line, along it from the middle.
        private static void Side(MeshBuilder b, Vector3[] low, Vector3[] high, int k, float middle)
        {
            int n = (k + 1) % 4;
            List<Vector3> quad = new List<Vector3> { low[k], low[n], high[n], high[k] };
            Vector3 run = low[n] - low[k];
            bool across = Mathf.Abs(run.y) >= Mathf.Abs(run.x);
            b.Polygon(quad, quad.ConvertAll(p => new Vector2(p.z * Along, (across ? p.y : p.x - middle) * Across)));
        }

        // A ring's cap moved up or down by lift, facing up (the top) or down.
        private static void Cap(MeshBuilder b, Vector3[] ring, float lift, bool up, float middle)
        {
            List<Vector3> cap = new List<Vector3>();
            for (int i = 0; i < 4; i++)
                cap.Add(ring[up ? i : 3 - i] + new Vector3(0f, 0f, lift));
            b.Polygon(cap, cap.ConvertAll(p => new Vector2(p.x - middle, p.y) * Across));
        }

        // The ring's corners anticlockwise seen from above, as the solids' faces take them.
        private static Vector3[] Anticlockwise(Vector3[] ring)
        {
            float twice = 0f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = ring[i], c = ring[(i + 1) % 4];
                twice += a.x * c.y - c.x * a.y;
            }
            return twice >= 0f ? ring : new[] { ring[3], ring[2], ring[1], ring[0] };
        }
    }
}
