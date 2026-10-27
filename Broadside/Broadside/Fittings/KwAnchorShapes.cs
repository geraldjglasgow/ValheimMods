using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The anchor windlass's meshes (<see cref="KwAnchor"/>), in kit axes (x forward, y left, z up, metres): the windlass on
    /// the deck (two sleepers, a cheek on each, the drum between them on its axle with a flange at each end of its barrel, a
    /// crank at each end and a rail over the cheeks), and an iron stock anchor hanging from its ring (the ring's middle at
    /// the origin, its shank straight down, its stock fore and aft under the ring, its arms port and starboard at the
    /// crown). The cable wound on the drum between its flanges is <see cref="KwAnchorCoil"/>.
    /// </summary>
    public static class KwAnchorShapes
    {
        /// <summary>The drum's axle height and its barrel's radius; the coil's outer surface round it (<see cref="KwAnchorCoil"/>).</summary>
        public const float Axle = 0.52f, Drum = 0.095f, Coiled = KwAnchorCoil.Outer;

        /// <summary>The barrel's flanges: their inner and outer faces each side of the drum's middle, and their radius.</summary>
        public const float FlangeIn = 0.335f, FlangeOut = 0.37f, Flange = 0.245f;

        /// <summary>The anchor's length from its ring's middle to the crown's underside.</summary>
        public const float Length = 1.04f;

        public static void Windlass(MeshBuilder b)
        {
            foreach (float s in new[] { -1f, 1f })
            {
                Block(b, -0.34f, 0.34f, s * 0.38f, s * 0.54f, 0f, 0.08f);
                Cheek(b, s);
                Solid.Log(b, new Vector3(0f, s * 0.40f, Axle), new Vector3(0f, s * 0.62f, Axle), 0.045f);
                Block(b, -0.03f, 0.03f, s * 0.60f, s * 0.64f, Axle - 0.03f, Axle + 0.30f);
                Solid.Log(b, new Vector3(0f, s * 0.64f, Axle + 0.27f), new Vector3(0f, s * 0.82f, Axle + 0.27f), 0.025f, 6);
            }
            Solid.Log(b, new Vector3(0f, -0.40f, Axle), new Vector3(0f, 0.40f, Axle), Drum, 10);
            foreach (float s in new[] { -1f, 1f })
                Solid.Log(b, new Vector3(0f, s * FlangeIn, Axle), new Vector3(0f, s * FlangeOut, Axle), Flange, 14);
            Block(b, -0.06f, 0.06f, -0.52f, 0.52f, 0.80f, 0.88f);
        }

        // A cheek: a board standing on its sleeper, narrowing to its top, the axle through it.
        private static void Cheek(MeshBuilder b, float s)
        {
            float y0 = Mathf.Min(s * 0.40f, s * 0.52f), y1 = Mathf.Max(s * 0.40f, s * 0.52f);
            Solid.Hexa(b, new[]
            {
                new Vector3(-0.17f, y0, 0.08f), new Vector3(0.17f, y0, 0.08f), new Vector3(0.17f, y1, 0.08f), new Vector3(-0.17f, y1, 0.08f),
                new Vector3(-0.10f, y0, 0.80f), new Vector3(0.10f, y0, 0.80f), new Vector3(0.10f, y1, 0.80f), new Vector3(-0.10f, y1, 0.80f),
            });
        }

        private static void Block(MeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1) =>
            Solid.Box(b, new Vector3(Mathf.Min(x0, x1), Mathf.Min(y0, y1), Mathf.Min(z0, z1)), new Vector3(Mathf.Max(x0, x1), Mathf.Max(y0, y1), Mathf.Max(z0, z1)));

        // The anchor's straight bars (its shank, its stock fore and aft under the ring, its crown) and, on each side, its
        // arm from the crown out to the elbow and up to the tip, and the tip's way.
        private static readonly (Vector3 Min, Vector3 Max)[] Bars =
        {
            (new Vector3(-0.035f, -0.035f, -0.98f), new Vector3(0.035f, 0.035f, -0.07f)),
            (new Vector3(-0.36f, -0.03f, -0.18f), new Vector3(0.36f, 0.03f, -0.12f)),
            (new Vector3(-0.05f, -0.07f, -Length), new Vector3(0.05f, 0.07f, -0.95f)),
        };

        private const float RingRadius = 0.075f, RingBar = 0.016f, ArmIn = 0.035f, ArmOut = 0.032f;

        private static (Vector3 Crown, Vector3 Elbow, Vector3 Tip) Arm(float s) =>
            (new Vector3(0f, 0f, -0.98f), new Vector3(0f, s * 0.24f, -0.94f), new Vector3(0f, s * 0.38f, -0.70f));

        public static void Anchor(MeshBuilder b)
        {
            Ring(b);
            foreach ((Vector3 min, Vector3 max) in Bars)
                Solid.Box(b, min, max);
            foreach (float s in new[] { -1f, 1f })
            {
                (Vector3 crown, Vector3 elbow, Vector3 tip) = Arm(s);
                Solid.Log(b, crown, elbow, ArmIn, 6);
                Solid.Log(b, elbow, tip, ArmOut, 6);
                Solid.Hexa(b, Fluke(tip, (tip - elbow).normalized));
            }
        }

        /// <summary>
        /// The anchor's outermost points in its kit (the ring's middle at the origin, the shank down, the stock along x,
        /// the arms along y): every bar's and fluke's corners, the ring round its middle, the arms round their ends; what
        /// must be clear of the hull for the anchor to be (<see cref="KwAnchorHang"/>).
        /// </summary>
        public static List<Vector3> Points()
        {
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f, r = RingRadius + RingBar;
                points.Add(new Vector3(r * Mathf.Cos(a), 0f, r * Mathf.Sin(a)));
            }
            foreach ((Vector3 min, Vector3 max) in Bars)
            {
                for (int i = 0; i < 8; i++)
                    points.Add(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
            }
            foreach (float s in new[] { -1f, 1f })
                ArmPoints(points, s);
            return points;
        }

        // One arm's ends widened by its radius (across and up), and its fluke's corners.
        private static void ArmPoints(List<Vector3> points, float s)
        {
            (Vector3 crown, Vector3 elbow, Vector3 tip) = Arm(s);
            foreach (Vector3 end in new[] { crown, elbow, tip })
            {
                foreach (Vector3 off in new[] { Vector3.right, -Vector3.right, Vector3.forward, -Vector3.forward })
                    points.Add(end + off * ArmIn);
            }
            points.AddRange(Fluke(tip, (tip - elbow).normalized));
        }

        // The ring at the shank's head: eight short round bars round the origin, upright and fore and aft.
        private static void Ring(MeshBuilder b)
        {
            const int segments = 8;
            const float radius = RingRadius;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * 2f * Mathf.PI / segments, a1 = (i + 1) * 2f * Mathf.PI / segments;
                Solid.Log(b, new Vector3(radius * Mathf.Cos(a0), 0f, radius * Mathf.Sin(a0)), new Vector3(radius * Mathf.Cos(a1), 0f, radius * Mathf.Sin(a1)), RingBar, 5);
            }
        }

        // A fluke: a flat iron blade at an arm's tip, along the arm and broad fore and aft; its eight corners.
        private static Vector3[] Fluke(Vector3 tip, Vector3 along)
        {
            Vector3 across = Vector3.right, flat = Vector3.Cross(along, across).normalized, middle = tip - along * 0.07f;
            Vector3[] corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                int k = i % 4;
                float d = k == 1 || k == 2 ? 0.09f : -0.09f, x = k >= 2 ? 0.10f : -0.10f, n = i >= 4 ? 0.015f : -0.015f;
                corners[i] = middle + along * d + across * x + flat * n;
            }
            return corners;
        }
    }
}
