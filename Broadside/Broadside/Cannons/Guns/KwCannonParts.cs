using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The parts the ship cannons' guns (<see cref="KwShipCannons"/>) and their balls are drawn from, in kit axes: a barrel
    /// turned about the x axis from its outline, a ball, the dark bore in a muzzle's face, the trunnion axle and the
    /// straps over the trunnions, drawn on both sides, y to the left and right.
    /// </summary>
    public static class KwCannonParts
    {
        private static readonly float[] Sides = { 1f, -1f };
        private static readonly Vector3 Across = new Vector3(0f, 1f, 0f);

        /// <summary>A round shape turned about the x axis through <paramref name="at"/>, from its outline as (x, radius) pairs back to front, closed at both ends.</summary>
        public static void Lathe(MeshBuilder b, Vector3 at, IReadOnlyList<Vector2> outline, int sides)
        {
            for (int i = 0; i + 1 < outline.Count; i++)
            {
                for (int k = 0; k < sides; k++)
                    Quad(b, at, outline[i], outline[i + 1], (float)k / sides, (float)(k + 1) / sides);
            }
            Cap(b, at, outline[0], sides);
            Cap(b, at, outline[outline.Count - 1], sides);
        }

        /// <summary>A ball round a centre, turned in so many bands from back to front.</summary>
        public static void Ball(MeshBuilder b, Vector3 centre, float radius, int bands = 6, int sides = 10)
        {
            Vector2[] outline = new Vector2[bands + 1];
            for (int i = 0; i <= bands; i++)
            {
                float a = Mathf.PI * i / bands;
                outline[i] = new Vector2(-radius * Mathf.Cos(a), radius * Mathf.Sin(a));
            }
            Lathe(b, centre, outline, sides);
        }

        /// <summary>The dark bore in a muzzle's face at x along the barrel: a disc standing just proud of the face.</summary>
        public static void Bore(MeshBuilder b, float face, float radius) =>
            Solid.Log(b, new Vector3(face - 0.03f, 0f, 0f), new Vector3(face + 0.003f, 0f, 0f), radius, 10);

        /// <summary>The trunnion axle across the barrel at the pitch pivot, out to <paramref name="half"/> each side.</summary>
        public static void Trunnions(MeshBuilder b, float radius, float half) =>
            Solid.Log(b, new Vector3(0f, -half, 0f), new Vector3(0f, half, 0f), radius, 10);

        /// <summary>A strap over each trunnion where it lies on its cheek, y out from the middle, round the pitch pivot.</summary>
        public static void CapSquares(MeshBuilder b, Vector3 pivot, float y, float radius, float tube)
        {
            foreach (float side in Sides)
                KwRound.Ring(b, pivot + new Vector3(0f, side * y, 0f), Across, radius, tube, 10, 4);
        }

        private static void Quad(MeshBuilder b, Vector3 at, Vector2 p, Vector2 q, float s0, float s1)
        {
            List<Vector3> quad = new List<Vector3> { Rim(at, p, s0), Rim(at, q, s0), Rim(at, q, s1), Rim(at, p, s1) };
            float u0 = (p.x + p.y) * 0.5f, u1 = (q.x + q.y) * 0.5f;
            b.Polygon(quad, new List<Vector2> { new Vector2(u0, s0), new Vector2(u1, s0), new Vector2(u1, s1), new Vector2(u0, s1) });
        }

        private static void Cap(MeshBuilder b, Vector3 at, Vector2 p, int sides)
        {
            if (p.y < 1e-4f)
                return;
            List<Vector3> ring = new List<Vector3>();
            for (int k = 0; k < sides; k++)
                ring.Add(Rim(at, p, (float)k / sides));
            b.Polygon(ring, Solid.Planar(ring));
        }

        private static Vector3 Rim(Vector3 at, Vector2 p, float share)
        {
            float a = share * 2f * Mathf.PI;
            return at + new Vector3(p.x, p.y * Mathf.Cos(a), p.y * Mathf.Sin(a));
        }
    }
}
