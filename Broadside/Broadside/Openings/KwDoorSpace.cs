using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A door leaf's own measures for drawing it (<see cref="KwDoorMesh"/>, <see cref="KwDoorIron"/>): a point is (a, h, d),
    /// a across from the hinge (0 to <see cref="Wide"/>), h up from the leaf's foot (0 to <see cref="High"/>) and d out
    /// through it (the boards from -<see cref="Half"/> on the inner face to +<see cref="Half"/> on the outer), turned into
    /// the leaf's hinge axes (<see cref="KwLeafPlan"/>, standing on its foot by <see cref="KwLeafPlan.Rise"/>) and handed to
    /// where it goes. Its faces wear the game's door wood at the game's own scale, the grain along a way given per solid.
    /// </summary>
    public sealed class KwDoorSpace
    {
        /// <summary>Metres of the door wood's texture along its grain and across it (as the game's own door wears it).</summary>
        public const float Along = 3.6f, Across = 4.2f;

        private readonly KwLeafPlan leaf;
        private readonly Func<Vector3, Vector3> place;

        public KwDoorSpace(KwLeafPlan leaf, Func<Vector3, Vector3> place) => (this.leaf, this.place) = (leaf, place);

        public float Wide => leaf.Size.y;
        public float High => leaf.Size.x;
        public float Half => leaf.Size.z / 2f;

        /// <summary>A door point as the point its builder takes.</summary>
        public Vector3 At(Vector3 p) => place(new Vector3(leaf.Rise * (p.y - High / 2f), p.x, p.z));

        /// <summary>A box between two corners.</summary>
        public void Box(MeshBuilder b, Vector3 min, Vector3 max, Vector3 grain, Vector2 shift)
        {
            List<Vector3> foot = new List<Vector3>
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
            };
            Prism(b, foot, new Vector3(0f, 0f, max.z - min.z), grain, shift);
        }

        /// <summary>An outline pushed along a way: its two ends and the sides between.</summary>
        public void Prism(MeshBuilder b, List<Vector3> ring, Vector3 lift, Vector3 grain, Vector2 shift) =>
            Solid(b, ring, ring.ConvertAll(p => p + lift), grain, shift);

        /// <summary>A solid between two outlines of as many points: both of them and the sides between.</summary>
        public void Solid(MeshBuilder b, List<Vector3> lower, List<Vector3> upper, Vector3 grain, Vector2 shift)
        {
            Face(b, lower, grain, shift);
            Face(b, upper, grain, shift);
            for (int i = 0; i < lower.Count; i++)
            {
                int j = (i + 1) % lower.Count;
                Face(b, new List<Vector3> { lower[i], lower[j], upper[j], upper[i] }, grain, shift);
            }
        }

        /// <summary>One flat face, the wood's grain along a way on it (a face square to the grain, an end, takes its own two ways).</summary>
        public void Face(MeshBuilder b, List<Vector3> ring, Vector3 grain, Vector2 shift)
        {
            Vector3 n = Normal(ring), across = Vector3.Cross(n, grain);
            if (across.sqrMagnitude < 0.04f)
            {
                grain = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                across = Vector3.Cross(n, grain);
            }
            Vector3 g = grain.normalized, c = across.normalized;
            b.Polygon(ring.ConvertAll(At), ring.ConvertAll(p => new Vector2(Vector3.Dot(p, g) / Along + shift.x, Vector3.Dot(p, c) / Across + shift.y)));
        }

        /// <summary>Points round a circle in the face's plane (a, h) at a depth.</summary>
        public static List<Vector3> Circle(float a, float h, float d, float r, int sides)
        {
            List<Vector3> ring = new List<Vector3>();
            for (int i = 0; i < sides; i++)
            {
                float t = (i + 0.5f) * 2f * Mathf.PI / sides;
                ring.Add(new Vector3(a + Mathf.Cos(t) * r, h + Mathf.Sin(t) * r, d));
            }
            return ring;
        }

        // A flat outline's way out (Newell's sum), a unit; none for a ring with no area.
        private static Vector3 Normal(List<Vector3> ring)
        {
            Vector3 n = Vector3.zero;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 p = ring[i], q = ring[(i + 1) % ring.Count];
                n += new Vector3((p.y - q.y) * (p.z + q.z), (p.z - q.z) * (p.x + q.x), (p.x - q.x) * (p.y + q.y));
            }
            return n.normalized;
        }
    }
}
