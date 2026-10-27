using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Shapes the rudders share, in kit axes (x forward, y left, z up). On the blade, in its own frame (the stock's axis
    /// at the origin, the blade aft of it toward -x): boards and strips that follow a tier's outline, plates on both faces,
    /// studs, collars and the stock. On the post, in the piece's frame (the post's after face at x = 0, its foot at
    /// z = 0): straps round the post and the knuckles behind it that the stock turns in.
    /// </summary>
    public static class KwRudderShapes
    {
        /// <summary>Kit up: the stock's axis.</summary>
        public static readonly Vector3 Up = new Vector3(0f, 0f, 1f);

        private static readonly float[] Sides = { 1f, -1f };

        /// <summary>A piece of the blade from z0 to z1, half each side, between a fore and an aft line (x at z0 and at z1).</summary>
        public static void Strip(MeshBuilder b, float fore0, float fore1, float aft0, float aft1, float z0, float z1, float half) => Solid.Hexa(b, new[]
        {
            new Vector3(aft0, -half, z0), new Vector3(fore0, -half, z0), new Vector3(fore0, half, z0), new Vector3(aft0, half, z0),
            new Vector3(aft1, -half, z1), new Vector3(fore1, -half, z1), new Vector3(fore1, half, z1), new Vector3(aft1, half, z1),
        });

        /// <summary>A board from z0 to z1: from x = fore aft to the outline, following it.</summary>
        public static void Board(MeshBuilder b, KwRudderKit k, float z0, float z1, float fore, float half)
        {
            List<float> cuts = Cuts(k, z0, z1);
            for (int i = 0; i + 1 < cuts.Count; i++)
                Strip(b, fore, fore, -k.AftAt(cuts[i]), -k.AftAt(cuts[i + 1]), cuts[i], cuts[i + 1], half);
        }

        /// <summary>A strip along the outline from z0 to z1, inset forward of the edge (negative: past it), width wide.</summary>
        public static void EdgeStrip(MeshBuilder b, KwRudderKit k, float z0, float z1, float inset, float width, float half)
        {
            List<float> cuts = Cuts(k, z0, z1);
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a0 = inset - k.AftAt(cuts[i]), a1 = inset - k.AftAt(cuts[i + 1]);
                Strip(b, a0 + width, a1 + width, a0, a1, cuts[i], cuts[i + 1], half);
            }
        }

        /// <summary>A binding under the blade's foot, from the fore edge to just past the after edge, half each side.</summary>
        public static void FootBinding(MeshBuilder b, KwRudderKit k, float below, float above, float half) =>
            Solid.Box(b, new Vector3(-k.AftAt(k.Foot) - 0.01f, -half, k.Foot - below), new Vector3(k.Fore, half, k.Foot + above));

        /// <summary>A plate on each face of the blade (faces at ±half), t thick, from x0 to x1 and z0 to z1.</summary>
        public static void Plates(MeshBuilder b, float x0, float x1, float z0, float z1, float half, float t)
        {
            Solid.Box(b, new Vector3(Mathf.Min(x0, x1), half, z0), new Vector3(Mathf.Max(x0, x1), half + t, z1));
            Solid.Box(b, new Vector3(Mathf.Min(x0, x1), -half - t, z0), new Vector3(Mathf.Max(x0, x1), -half, z1));
        }

        /// <summary>A nail or rivet head on both faces (at ±y) at x, z: r across, standing 0.6 r proud.</summary>
        public static void Studs(MeshBuilder b, float x, float z, float y, float r)
        {
            foreach (float side in Sides)
                Solid.Log(b, new Vector3(x, side * y, z), new Vector3(x, side * (y + 0.6f * r), z), r, 6);
        }

        /// <summary>
        /// The stock: a round post on the blade's axis from its foot (the post's, or the blade's below it) up through the
        /// head to rise over the post's top, and past the yoke or the steering gear's quadrant when it carries one.
        /// </summary>
        public static void Stock(MeshBuilder b, KwRudderKit k, float rise, int sides)
        {
            float top = Mathf.Max(k.HeadBase - 0.02f + rise, k.YokeZ + 0.08f * k.T);
            if (k.Lay.Geared)
                top = Mathf.Max(top, KwGearTrain.HubTop(k.Lay) + 0.02f);
            Solid.Log(b, new Vector3(0f, 0f, k.StockFoot), new Vector3(0f, 0f, top), k.Stock, sides);
        }

        /// <summary>A collar round the stock from z up h, r over it.</summary>
        public static void Collar(MeshBuilder b, KwRudderKit k, float z, float h, float r, int sides = 10) =>
            Solid.Log(b, new Vector3(0f, 0f, z), new Vector3(0f, 0f, z + h), k.Stock + r, sides);

        /// <summary>A strap round the post from z up h, t thick over its sides and fore face, flush with its after face.</summary>
        public static void PostBand(MeshBuilder b, KwRudderFit fit, float z, float h, float t) =>
            Solid.Box(b, new Vector3(0f, -(fit.PostHalf + t), z), new Vector3(fit.PostDepth + t, fit.PostHalf + t, z + h));

        /// <summary>The knuckle behind a strap that the stock turns in: a collar r over the stock and the web to the post.</summary>
        public static void Knuckle(MeshBuilder b, KwRudderKit k, float z, float h, float r, int sides = 10)
        {
            Vector3 axis = k.Pivot;
            Solid.Log(b, axis + new Vector3(0f, 0f, z), axis + new Vector3(0f, 0f, z + h), k.Stock + r, sides);
            float web = 0.6f * k.Stock;
            Solid.Box(b, new Vector3(axis.x, -web, z), new Vector3(0f, web, z + h));
        }

        /// <summary>A gudgeon: a strap round the post and its knuckle, t thick.</summary>
        public static void Gudgeon(MeshBuilder b, KwRudderKit k, float z, float h, float t, int sides = 10)
        {
            PostBand(b, k.Fit, z, h, t);
            Knuckle(b, k, z, h, t, sides);
        }

        /// <summary>A square beam (a squared tiller) from a to c along x, half h0 across at a and h1 at c.</summary>
        public static void Beam(MeshBuilder b, Vector3 a, Vector3 c, float h0, float h1) => Solid.Hexa(b, new[]
        {
            a + new Vector3(0f, -h0, -h0), a + new Vector3(0f, h0, -h0), a + new Vector3(0f, h0, h0), a + new Vector3(0f, -h0, h0),
            c + new Vector3(0f, -h1, -h1), c + new Vector3(0f, h1, -h1), c + new Vector3(0f, h1, h1), c + new Vector3(0f, -h1, h1),
        });

        /// <summary>A sleeve of radius r round the spar from a to c, from <paramref name="at"/> metres along it, length long.</summary>
        public static void Sleeve(MeshBuilder b, Vector3 a, Vector3 c, float at, float length, float r, int sides = 8)
        {
            Vector3 along = (c - a).normalized;
            Solid.Log(b, a + along * at, a + along * (at + length), r, sides);
        }

        // z0, the outline's corners between, z1.
        private static List<float> Cuts(KwRudderKit k, float z0, float z1)
        {
            List<float> cuts = new List<float> { z0 };
            foreach (Vector2 corner in k.Tier.Edge)
            {
                float z = k.Span.Z(corner.x);
                if (z > z0 + 1e-3f && z < z1 - 1e-3f)
                    cuts.Add(z);
            }
            cuts.Add(z1);
            return cuts;
        }
    }
}
