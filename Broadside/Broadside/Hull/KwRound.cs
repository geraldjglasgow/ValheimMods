using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>Round parts for rigging: a log that tapers from one end to the other, and a ring (a torus) round an axis.</summary>
    public static class KwRound
    {
        /// <summary>A round spar from a (radius ra) to c (radius rc), its grain along it, its ends closed.</summary>
        public static void Taper(MeshBuilder b, Vector3 a, Vector3 c, float ra, float rc, int sides = 8)
        {
            Vector3 axis = (c - a).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.z) < 0.9f ? Vector3.forward : Vector3.right).normalized, v = Vector3.Cross(axis, u);
            float length = (c - a).magnitude * 0.5f;
            for (int i = 0; i < sides; i++)
            {
                Vector3 p = Around(u, v, i, sides), q = Around(u, v, i + 1, sides);
                float s0 = (float)i / sides, s1 = (float)(i + 1) / sides;
                b.Polygon(new List<Vector3> { a + p * ra, c + p * rc, c + q * rc, a + q * ra },
                    new List<Vector2> { new Vector2(0f, s0), new Vector2(length, s0), new Vector2(length, s1), new Vector2(0f, s1) });
            }
            foreach ((Vector3 end, float r) in new[] { (a, ra), (c, rc) })
            {
                List<Vector3> cap = new List<Vector3>();
                for (int i = 0; i < sides; i++)
                    cap.Add(end + Around(u, v, i, sides) * r);
                b.Polygon(cap, Solid.Planar(cap));
            }
        }

        /// <summary>A ring of the given radius round a centre and axis, its tube the given radius.</summary>
        public static void Ring(MeshBuilder b, Vector3 centre, Vector3 axis, float radius, float tube, int around = 10, int sides = 5)
        {
            axis = axis.normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.z) < 0.9f ? Vector3.forward : Vector3.right).normalized, v = Vector3.Cross(axis, u);
            for (int i = 0; i < around; i++)
            {
                Vector3 d0 = Around(u, v, i, around), d1 = Around(u, v, i + 1, around);
                for (int j = 0; j < sides; j++)
                {
                    List<Vector3> quad = new List<Vector3>
                    {
                        Tube(centre, d0, axis, radius, tube, j, sides), Tube(centre, d1, axis, radius, tube, j, sides),
                        Tube(centre, d1, axis, radius, tube, j + 1, sides), Tube(centre, d0, axis, radius, tube, j + 1, sides),
                    };
                    b.Polygon(quad, Solid.Planar(quad));
                }
            }
        }

        private static Vector3 Around(Vector3 u, Vector3 v, int i, int count)
        {
            float a = i * 2f * Mathf.PI / count;
            return u * Mathf.Cos(a) + v * Mathf.Sin(a);
        }

        // A point of a ring's tube: out from the ring's centre along d to the tube's middle, then round the tube.
        private static Vector3 Tube(Vector3 centre, Vector3 d, Vector3 axis, float radius, float tube, int j, int sides)
        {
            float a = j * 2f * Mathf.PI / sides;
            return centre + d * (radius + tube * Mathf.Cos(a)) + axis * (tube * Mathf.Sin(a));
        }
    }
}
