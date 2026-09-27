using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// Builds the see-through body between the ground and the height a stroke aims for: walls standing on every
    /// outline point from the ground up (or down) to the aimed height, and a lid at the aimed height (a fan for
    /// circle, square and rectangle, a band between the edges for ring and frame). Level aims for its target height,
    /// raise and lower for the ground plus or minus the amount, the admin operations for their own heights. The soft
    /// edge is not drawn: the body shows where the full effect goes.
    /// </summary>
    internal static class VolumeMesh
    {
        /// <summary>False when the stroke's height operation has no surface to aim for (paint, smooth, remove).</summary>
        public static bool HasSurface(HeightOp op)
        {
            return op == HeightOp.Level || op == HeightOp.Raise || op == HeightOp.Lower || op == HeightOp.SetMin
                || op == HeightOp.SetMax || op == HeightOp.Offset || op == HeightOp.Reset;
        }

        /// <summary>The height the stroke aims for at a point whose ground is at <paramref name="ground"/> (amounts as the engine caps them).</summary>
        public static float Top(StrokeParams p, Vector3 point, float ground)
        {
            BrushStroke s = p.Stroke;
            switch (s.Height)
            {
                case HeightOp.Level: return s.Target;
                case HeightOp.Raise: return ground + p.Amount;
                case HeightOp.Lower: return ground - p.Amount;
                case HeightOp.SetMin: return Mathf.Max(ground, s.Target);
                case HeightOp.SetMax: return Mathf.Min(ground, s.Target);
                case HeightOp.Offset: return TerrainRead.BaseHeight(point, ground) + p.Amount;
                case HeightOp.Reset: return TerrainRead.BaseHeight(point, ground);
                default: return ground;
            }
        }

        /// <summary>Fills <paramref name="data"/> from ground loops (outer first); false when there is nothing to draw.</summary>
        public static bool Build(MeshData data, StrokeParams p, List<Vector3[]> loops, Color colour)
        {
            data.Clear(p.Stroke.Center);
            if (!HasSurface(p.Stroke.Height) || loops.Count == 0)
                return false;
            List<Vector3[]> tops = new List<Vector3[]>();
            foreach (Vector3[] loop in loops)
            {
                Vector3[] top = Tops(p, loop);
                Walls(data, loop, top, colour);
                tops.Add(top);
            }
            Color lid = new Color(colour.r, colour.g, colour.b, colour.a * 0.8f);
            if (tops.Count > 1)
                Band(data, tops[0], tops[1], lid);
            else
                Fan(data, CentreTop(p), tops[0], lid);
            return data.Vertices.Count > 0;
        }

        private static Vector3[] Tops(StrokeParams p, Vector3[] loop)
        {
            Vector3[] top = new Vector3[loop.Length];
            for (int i = 0; i < loop.Length; i++)
                top[i] = new Vector3(loop[i].x, Top(p, loop[i], loop[i].y), loop[i].z);
            return top;
        }

        private static Vector3 CentreTop(StrokeParams p)
        {
            Vector3 centre = p.Stroke.Center;
            float ground = GroundSampler.Height(centre, centre.y);
            return new Vector3(centre.x, Top(p, centre, ground), centre.z);
        }

        private static void Walls(MeshData data, Vector3[] ground, Vector3[] top, Color colour)
        {
            int n = ground.Length;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                if (Mathf.Abs(top[i].y - ground[i].y) < 0.01f && Mathf.Abs(top[j].y - ground[j].y) < 0.01f)
                    continue;
                data.Quad(ground[i], ground[j], top[j], top[i], colour);
            }
        }

        private static void Fan(MeshData data, Vector3 centre, Vector3[] rim, Color colour)
        {
            for (int i = 0; i < rim.Length; i++)
                data.Triangle(centre, rim[i], rim[(i + 1) % rim.Length], colour);
        }

        private static void Band(MeshData data, Vector3[] outer, Vector3[] inner, Color colour)
        {
            int n = Mathf.Min(outer.Length, inner.Length);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                data.Quad(outer[i], outer[j], inner[j], inner[i], colour);
            }
        }
    }
}
