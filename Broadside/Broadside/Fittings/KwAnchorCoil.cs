using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The anchor cable wound on the windlass's drum (<see cref="KwAnchorShapes"/>), drawn as rope in the game's rope (its
    /// texture along the rope, as on the cable's line): one rope round the barrel between its flanges, a full layer laid a
    /// little unevenly over a bottom one (seen in the grooves), then a few riding turns back over it from one flange, the
    /// last crossing over the others and tucked under. Every point stays within <see cref="Ridden"/> of the axle and
    /// <see cref="Reach"/> plus the rope of the drum's middle, so the coil never meets the flanges or the frame. Smooth
    /// rings of a few sides, about 2,600 triangles; made once for the prefab. Kit axes, the windlass's: x forward, y left
    /// (along the axle), z up.
    /// </summary>
    public static class KwAnchorCoil
    {
        /// <summary>The rope's radius; how far each side of the drum's middle its turns' middles reach, clear of the flanges.</summary>
        public const float Rope = 0.024f, Reach = KwAnchorShapes.FlangeIn - Rope - 0.006f;

        // The rope's middle on the bottom layer, the full layer and the riding turns; the core drawn for the bottom layer
        // and how far along the drum it reaches (a hair inside the flanges); where a tucked end goes, under the core.
        private const float Under = KwAnchorShapes.Drum + Rope, Full = Under + 1.75f * Rope, Riding = Full + 1.75f * Rope;
        private const float Core = Under + 0.6f * Rope, CoreEnd = KwAnchorShapes.FlangeIn - 0.003f, Tucked = Under - 0.5f * Rope;

        /// <summary>The coil's outer surface over its full layer, and over its riding turns, its turns' wandering out
        /// included; how far into it a cable leaving it square lies (bedded between its turns, so no gap shows under it).</summary>
        public const float Outer = Full + Rope + 2f * WanderOut, Ridden = Riding + Rope + 2f * WanderOut, Bed = 0.014f;

        // A turn's pitch; how many riding turns; how far the crossing turn runs along the drum; the turns the full layer
        // takes; where round the drum the rope starts; how far a turn wanders along the drum and out from it.
        private const float Pitch = 2f * Rope, Riders = 3.6f, Cross = 0.17f, Turns = 2f * Reach / Pitch, Start = 2.2f;
        private const float WanderAlong = 0.004f, WanderOut = 0.003f;
        private const int Sides = 6, PerTurn = 12, CoreSides = 16;

        /// <summary>Where along the drum the riding turns begin (they run from there to the flange on the left).</summary>
        public const float RidersFrom = Reach - Pitch * Riders - Rope;

        /// <summary>The coil's outer surface's radius at a place along the drum: over the riding turns or the full layer.</summary>
        public static float Surface(float y) => y >= RidersFrom ? Ridden : Outer;

        /// <summary>The rope's run: turns from its start, place along the drum and radius of its middle, straight between.</summary>
        private static readonly (float S, float Y, float R)[] Knots =
        {
            (0f, -Reach, Tucked), (0.3f, -Reach, Full), (0.3f + Turns, Reach, Full), (0.6f + Turns, Reach, Riding),
            (0.6f + Turns + Riders, Reach - Riders * Pitch, Riding), (1.1f + Turns + Riders, Reach - Riders * Pitch - Cross, Riding),
            (1.45f + Turns + Riders, Reach - Riders * Pitch - Cross - 0.02f, Tucked),
        };

        /// <summary>The coil as lists (kit axes): points, their normals and UVs, and triangles wound for Unity.</summary>
        public sealed class Made
        {
            public readonly List<Vector3> Points = new List<Vector3>(), Normals = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();

            // A ring's vertex (its point, its outward normal, its UV).
            public void Add(Vector3 point, Vector3 normal, Vector2 uv)
            {
                Points.Add(point);
                Normals.Add(normal);
                Uvs.Add(uv);
            }

            // The quads between a band's rings (the first ring's first vertex at `first`, `size` vertices a ring), wound
            // to face inward in kit axes, which Unity's mirrored axes turn to face outward.
            public void Band(int first, int rings, int size)
            {
                for (int i = 0; i + 1 < rings; i++)
                {
                    for (int k = 0; k + 1 < size; k++)
                    {
                        int a = first + i * size + k, b = a + size, c = b + 1, d = a + 1;
                        Triangles.AddRange(new[] { a, b, c, a, c, d });
                    }
                }
            }
        }

        /// <summary>The coil's mesh for the windlass prefab, in Unity axes, smooth.</summary>
        public static Mesh ToMesh()
        {
            Made made = Make();
            Mesh mesh = new Mesh { name = KwAnchor.Prefab + "_coil" };
            mesh.SetVertices(made.Points.ConvertAll(KwMesh.ToUnity));
            mesh.SetNormals(made.Normals.ConvertAll(KwMesh.ToUnity));
            mesh.SetUVs(0, made.Uvs);
            mesh.SetTriangles(made.Triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The coil's geometry: the bottom layer's core, then the rope.</summary>
        public static Made Make()
        {
            Made made = new Made();
            CoreBand(made);
            Tube(made, Centres());
            return made;
        }

        // The bottom layer, seen only in the full layer's grooves: a round of rope whose turns run round the drum.
        private static void CoreBand(Made made)
        {
            int first = made.Points.Count;
            for (int k = 0; k <= CoreSides; k++)
            {
                float a = k * 2f * Mathf.PI / CoreSides;
                Vector3 n = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                foreach (float y in new[] { -CoreEnd, CoreEnd })
                    made.Add(new Vector3(0f, y, KwAnchorShapes.Axle) + n * Core, n, new Vector2(a * Core, y / Pitch));
            }
            made.Band(first, CoreSides + 1, 2);
        }

        // The rope's middle every twelfth of a turn along its run, wandering a little along the drum and out from it.
        private static List<Vector3> Centres()
        {
            List<Vector3> centres = new List<Vector3>();
            float end = Knots[Knots.Length - 1].S;
            int steps = Mathf.CeilToInt(end * PerTurn);
            for (int i = 0; i <= steps; i++)
            {
                float s = end * i / steps, angle = Start + s * 2f * Mathf.PI;
                (float y, float r) = At(s);
                float settled = Mathf.Clamp01(Mathf.Min(s, end - s) / 0.3f);
                y = Mathf.Clamp(y + settled * WanderAlong * Mathf.Sin(angle + 1.3f * s), -Reach, Reach);
                r += settled * (WanderOut * Mathf.Sin(2f * angle + 0.7f * s) + WanderOut * Mathf.Sin(0.9f * s + 0.4f));
                centres.Add(new Vector3(r * Mathf.Cos(angle), y, KwAnchorShapes.Axle + r * Mathf.Sin(angle)));
            }
            return centres;
        }

        // The place along the drum and the radius at s turns along the run, between the knots either side.
        private static (float Y, float R) At(float s)
        {
            int i = 0;
            while (i + 2 < Knots.Length && s > Knots[i + 1].S)
                i++;
            (float s0, float y0, float r0) = Knots[i];
            (float s1, float y1, float r1) = Knots[i + 1];
            float t = Mathf.Clamp01((s - s0) / (s1 - s0));
            return (Mathf.Lerp(y0, y1, t), Mathf.Lerp(r0, r1, t));
        }

        // A round rope along the centres: a ring of Sides round each, square to the run, its seam out from the axle; u the
        // metres along it, v twice round it (each side seen showing the texture's width once, as the cable's line does).
        private static void Tube(Made made, List<Vector3> centres)
        {
            int first = made.Points.Count;
            float along = 0f;
            for (int i = 0; i < centres.Count; i++)
            {
                along += i > 0 ? (centres[i] - centres[i - 1]).magnitude : 0f;
                Vector3 c = centres[i], run = (centres[Mathf.Min(i + 1, centres.Count - 1)] - centres[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 radial = new Vector3(c.x, 0f, c.z - KwAnchorShapes.Axle).normalized;
                Vector3 side = Vector3.Cross(run, radial).normalized, up = Vector3.Cross(side, run);
                for (int k = 0; k <= Sides; k++)
                {
                    float a = k * 2f * Mathf.PI / Sides;
                    Vector3 n = up * Mathf.Cos(a) + side * Mathf.Sin(a);
                    made.Add(c + n * Rope, n, new Vector2(along, 2f * k / Sides));
                }
            }
            made.Band(first, centres.Count, Sides + 1);
        }
    }
}
