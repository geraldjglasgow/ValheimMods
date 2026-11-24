using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A seer's robe's layers hung over its skirt (the Sunweave's over-robe, the Rootweave's tunic, the Emberweave's lobe
    /// band, the Mossweave's leaf tabs, the Rimegild's tabard, sash and rope tails) tied to the cloth under them, once per
    /// robe mesh (<see cref="RobeClothMesh"/>): each is its own particles a few centimetres outside the skirt's, so on its
    /// own it swung and was pushed by the legs apart from the skirt and the two passed through each other (user, 2026-11-03,
    /// Sunweave: the over-robe "glitching through" the skirt when running). Every moving vertex whose line in along its own
    /// normal meets a triangle of another layer facing the same way, 0.5 to 6 cm under it, is tied to that triangle's three
    /// corners by three triangles the cloth reads and nobody sees (a submesh after the drawn ones, which the renderer has
    /// no material for): MagicaCloth makes distance and bending constraints of them, so the vertex rides the cloth under it
    /// as a tetrahedron. No tie where a corner is within the cloth's reduction distance of the vertex or of another layer
    /// outside it: the reduction merges those into one particle, and a tie across a merge can end up inside out (the
    /// Rootweave's tunic seated, 2 cm through the skirt in every frame).
    /// Measured offline (CASTERS.md, "The robes' skirts as cloth").
    /// </summary>
    internal static class RobeClothTies
    {
        // Metres: the farthest and nearest layer under a vertex it is tied to; the least likeness of the two surfaces'
        // facing (the cosine between their normals); a triangle whose first corner is farther than this is not looked at.
        private const float Reach = 0.06f;
        private const float Least = 0.005f;
        private const float Facing = 0.5f;
        private const float Near = 0.25f;

        /// <summary>The tie triangles for a skirt mesh's vertices, normals and drawn triangles (none: an empty array).</summary>
        public static int[] Of(Vector3[] vertices, Vector3[] normals, int[] triangles, VertexAttribute[] attributes)
        {
            var layers = new Layers(vertices, normals, triangles);
            var ties = new List<int>();
            for (int i = 0; i < vertices.Length && i < normals.Length; i++)
            {
                if (attributes[i].IsMove())
                    Tie(layers, i, ties);
            }
            return ties.ToArray();
        }

        private static void Tie(Layers layers, int vertex, List<int> ties)
        {
            int t = layers.Under(vertex);
            if (t < 0)
                return;
            int[] triangles = layers.Triangles;
            for (int k = 0; k < 3; k++)
            {
                ties.Add(vertex);
                ties.Add(triangles[3 * t + k]);
                ties.Add(triangles[3 * t + (k + 1) % 3]);
            }
        }

        /// <summary>The skirt mesh in rest metres: its points, each vertex's facing and each triangle's.</summary>
        private sealed class Layers
        {
            public readonly int[] Triangles;
            private readonly Vector3[] points, facing, sides;
            private readonly float merged;
            private readonly bool[] crowded;

            public Layers(Vector3[] vertices, Vector3[] normals, int[] triangles)
            {
                Triangles = triangles;
                points = new Vector3[vertices.Length];
                facing = new Vector3[vertices.Length];
                for (int i = 0; i < points.Length && i < normals.Length; i++)
                {
                    points[i] = RobeClothFixed.ToRest(vertices[i]);
                    facing[i] = (RobeClothFixed.ToRest(normals[i]) / RobeClothFixed.Scale).normalized;
                }
                sides = new Vector3[triangles.Length / 3];
                for (int t = 0; t < sides.Length; t++)
                    sides[t] = Vector3.Cross(Corner(t, 1) - Corner(t, 0), Corner(t, 2) - Corner(t, 0)).normalized;
                merged = RobeClothBuild.Merge * RobeClothBuild.Merge;
                crowded = new bool[points.Length];
                for (int i = 0; i < points.Length; i++)
                    crowded[i] = Crowded(i);
            }

            /// <summary>Another layer outside the vertex within the reduction distance: the two merge into one particle.</summary>
            private bool Crowded(int vertex)
            {
                for (int u = 0; u < points.Length; u++)
                {
                    Vector3 apart = points[u] - points[vertex];
                    float square = apart.sqrMagnitude;
                    if (u != vertex && square < merged && square > 1e-8f && Vector3.Dot(apart, facing[vertex]) > 0.3f * Mathf.Sqrt(square))
                        return true;
                }
                return false;
            }

            private Vector3 Corner(int t, int k) => points[Triangles[3 * t + k]];

            /// <summary>The nearest triangle of another layer on the vertex's line in along its normal, or -1.</summary>
            public int Under(int vertex)
            {
                int best = -1;
                float nearest = Reach;
                for (int t = 0; t < sides.Length; t++)
                {
                    if (!Beside(vertex, t))
                        continue;
                    float d = Hit(points[vertex], -facing[vertex], Corner(t, 0), Corner(t, 1), Corner(t, 2));
                    if (d > Least && d < nearest)
                    {
                        nearest = d;
                        best = t;
                    }
                }
                return best;
            }

            /// <summary>
            /// A triangle near the vertex, not its own, facing its way, with no corner the reduction merges into the vertex or
            /// into another layer outside it (a corner moved by a merge can turn the tie inside out).
            /// </summary>
            private bool Beside(int vertex, int t)
            {
                int a = Triangles[3 * t], b = Triangles[3 * t + 1], c = Triangles[3 * t + 2];
                if (a == vertex || b == vertex || c == vertex || (points[a] - points[vertex]).sqrMagnitude > Near * Near)
                    return false;
                Vector3 f = facing[vertex];
                if (Mathf.Abs(Vector3.Dot(sides[t], f)) < Facing || Vector3.Dot(facing[a] + facing[b] + facing[c], f) < 0f)
                    return false;
                return Apart(vertex, a) && Apart(vertex, b) && Apart(vertex, c);
            }

            private bool Apart(int vertex, int corner) =>
                !crowded[corner] && (points[corner] - points[vertex]).sqrMagnitude >= merged;
        }

        /// <summary>How far along the ray it meets the triangle (Moller and Trumbore), or -1.</summary>
        private static float Hit(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 e1 = b - a, e2 = c - a, h = Vector3.Cross(direction, e2);
            float det = Vector3.Dot(e1, h);
            if (Mathf.Abs(det) < 1e-9f)
                return -1f;
            float f = 1f / det;
            Vector3 s = origin - a;
            float u = f * Vector3.Dot(s, h);
            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(direction, q);
            if (u < 0f || u > 1f || v < 0f || u + v > 1f)
                return -1f;
            return f * Vector3.Dot(e2, q);
        }
    }
}
