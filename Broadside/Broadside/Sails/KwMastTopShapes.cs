using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The shapes of what a mast carries besides its sails (<see cref="KwMastTops"/>), part of the mast's own mesh and so of
    /// its collider on the dock and of the launched ship's look (<see cref="KwSailShapes.Rig"/>), in the plank wood. A
    /// crow's nest: a round plank floor round the mast, eight posts and two rails round it, four brackets under it. A
    /// fighting top: a floor whose straight side is at the mast's face and which reaches to the side away from the sails
    /// (<see cref="Outline"/>), on two trestle beams and two brackets; a square one railed on its three open sides, a round
    /// one with a low board wall round its curve. Climbing pegs through the mast, every 38 cm from the deck to the highest
    /// platform, and a staff over the masthead for a pennant. Kit axes as <see cref="KwSailLayout"/>.
    /// </summary>
    public static class KwMastTopShapes
    {
        private const float Floor = 0.08f, TopFloor = 0.1f, Post = 0.03f, PegEvery = 0.38f, PegOut = 0.16f;

        public static void Shape(MeshBuilder b, KwMastSpec mast)
        {
            KwMastTops tops = mast.Tops;
            if (tops.Nest)
                Nest(b, mast.Mast, tops.NestAt);
            if (tops.HasTop)
                Top(b, mast, tops);
            if (tops.Any)
                Pegs(b, mast.Mast, tops.Highest);
            if (tops.HasPennant)
                Staff(b, mast.Mast, tops.StaffTop(mast.Mast));
        }

        // The crow's nest: its floor, posts and rails, and its brackets from the mast.
        private static void Nest(MeshBuilder b, float mast, float at)
        {
            float rn = KwMastTops.NestRadius(mast), rail = rn - 0.04f;
            Slab(b, Circle(rn, 16), at - Floor, at);
            for (int k = 0; k < 8; k++)
            {
                Vector3 p = Round(k * 45f + 22.5f, rail);
                Solid.Log(b, p + new Vector3(0f, 0f, at), p + new Vector3(0f, 0f, at + KwMastTops.NestRail), Post, 6);
            }
            foreach (float z in new[] { at + 0.5f, at + KwMastTops.NestRail })
                KwRound.Ring(b, new Vector3(0f, 0f, z), Vector3.forward, rail, 0.03f, 16, 5);
            Brackets(b, mast, at - Floor, 0.75f * rn, 45f);
        }

        // A fighting top: its floor, what stands on it and what holds it up.
        private static void Top(MeshBuilder b, KwMastSpec mast, KwMastTops tops)
        {
            float at = tops.TopAt;
            List<Vector2> outline = Outline(mast, tops);
            Slab(b, outline, at - TopFloor, at);
            if (tops.Top == 1)
                Rail(b, outline, at);
            else
                Wall(b, outline, at);
            float r = KwMastTops.RadiusAt(mast.Mast, at), far = outline[outline.Count / 2].x;
            foreach (float y in new[] { -(r + 0.07f), r + 0.07f })
                Solid.Box(b, new Vector3(Mathf.Min(far, outline[0].x), y - 0.05f, at - TopFloor - 0.12f), new Vector3(Mathf.Max(far, outline[0].x), y + 0.05f, at - TopFloor));
            Brackets(b, mast.Mast, at - TopFloor - 0.12f, 0.7f * Mathf.Abs(far), KwMastTops.TopWay(mast) < 0f ? 180f : 0f, 2);
        }

        /// <summary>
        /// A fighting top's outline seen from above, its straight side first (at the mast's face toward the sails) and then
        /// round its far side: a rectangle for a square top, a half ellipse for a round one; 1.2 half widths along the ship.
        /// </summary>
        public static List<Vector2> Outline(KwMastSpec mast, KwMastTops tops)
        {
            float way = KwMastTops.TopWay(mast), h = KwMastTops.TopHalf(mast.Mast), depth = 1.2f * h;
            float face = -way * (KwMastTops.RadiusAt(mast.Mast, tops.TopAt) - 0.02f);
            List<Vector2> outline = new List<Vector2> { new Vector2(face, -h) };
            if (tops.Top == 1)
            {
                outline.AddRange(new[] { new Vector2(face + way * depth, -h), new Vector2(face + way * depth, h), new Vector2(face, h) });
                return outline;
            }
            for (int i = 1; i <= 12; i++)
            {
                float t = Mathf.PI * i / 12f;
                outline.Add(new Vector2(face + way * depth * Mathf.Sin(t), -h * Mathf.Cos(t)));
            }
            return outline;
        }

        // A square top's posts at every corner and every 0.6 m along its open sides (the outline from its first point round
        // to its last, not along its straight side), with a top rail and a middle rail.
        private static void Rail(MeshBuilder b, List<Vector2> rim, float at)
        {
            for (int i = 0; i + 1 < rim.Count; i++)
            {
                Vector2 p = rim[i], q = rim[i + 1];
                int posts = Mathf.Max(1, Mathf.CeilToInt((q - p).magnitude / 0.6f));
                for (int k = 0; k <= posts; k++)
                {
                    Vector2 at2 = Vector2.Lerp(p, q, (float)k / posts);
                    Solid.Log(b, new Vector3(at2.x, at2.y, at), new Vector3(at2.x, at2.y, at + KwMastTops.TopRail), Post, 6);
                }
                foreach (float z in new[] { at + 0.45f, at + KwMastTops.TopRail })
                    Solid.Log(b, new Vector3(p.x, p.y, z), new Vector3(q.x, q.y, z), 0.03f, 6);
            }
        }

        // A round top's low wall of boards round its curve, a rail along its top.
        private static void Wall(MeshBuilder b, List<Vector2> rim, float at)
        {
            const float high = 0.65f;
            for (int i = 0; i + 1 < rim.Count; i++)
            {
                Vector2 p = rim[i], q = rim[i + 1];
                List<Vector3> board = new List<Vector3> { new Vector3(p.x, p.y, at), new Vector3(q.x, q.y, at), new Vector3(q.x, q.y, at + high), new Vector3(p.x, p.y, at + high) };
                b.Polygon(board, Solid.Planar(board));
                Solid.Log(b, new Vector3(p.x, p.y, at + high), new Vector3(q.x, q.y, at + high), 0.035f, 6);
            }
        }

        // Brackets from the mast up and out to a floor's underside, count of them round the mast from the angle given.
        private static void Brackets(MeshBuilder b, float mast, float under, float reach, float from, int count = 4)
        {
            float low = under - 0.7f, r = KwMastTops.RadiusAt(mast, low);
            for (int k = 0; k < count; k++)
            {
                float angle = count == 4 ? from + 90f * k : from + (k == 0 ? -40f : 40f);
                Solid.Log(b, Round(angle, r) + new Vector3(0f, 0f, low), Round(angle, reach) + new Vector3(0f, 0f, under), 0.035f, 6);
            }
        }

        // Pegs through the mast, sticking out to both sides, from just over the deck to just under the highest floor.
        private static void Pegs(MeshBuilder b, float mast, float highest)
        {
            for (float z = 0.45f; z < highest - 0.2f; z += PegEvery)
            {
                float out1 = KwMastTops.RadiusAt(mast, z) + PegOut;
                Solid.Log(b, new Vector3(0f, -out1, z), new Vector3(0f, out1, z), 0.024f, 6);
            }
        }

        // The pennant's staff over the masthead cap up to its top, with a round truck on it.
        private static void Staff(MeshBuilder b, float mast, float top)
        {
            KwRound.Taper(b, new Vector3(0f, 0f, mast + 0.07f), new Vector3(0f, 0f, top), 0.035f, 0.022f, 6);
            KwRound.Taper(b, new Vector3(0f, 0f, top), new Vector3(0f, 0f, top + 0.05f), 0.05f, 0.05f, 8);
        }

        /// <summary>A flat floor of this outline (seen from above) between two heights: its top, its underside and its edges.</summary>
        public static void Slab(MeshBuilder b, List<Vector2> outline, float low, float high)
        {
            List<Vector3> top = outline.ConvertAll(p => new Vector3(p.x, p.y, high)), under = outline.ConvertAll(p => new Vector3(p.x, p.y, low));
            b.Polygon(top, top.ConvertAll(p => new Vector2(p.x, p.y) * 0.5f));
            b.Polygon(under, under.ConvertAll(p => new Vector2(p.x, p.y) * 0.5f));
            for (int i = 0; i < outline.Count; i++)
            {
                int j = (i + 1) % outline.Count;
                List<Vector3> edge = new List<Vector3> { under[i], under[j], top[j], top[i] };
                b.Polygon(edge, Solid.Planar(edge));
            }
        }

        /// <summary>A circle of that radius seen from above, its points round from straight ahead.</summary>
        public static List<Vector2> Circle(float radius, int points)
        {
            List<Vector2> circle = new List<Vector2>();
            for (int i = 0; i < points; i++)
            {
                Vector3 p = Round(360f * i / points, radius);
                circle.Add(new Vector2(p.x, p.y));
            }
            return circle;
        }

        // A level point at an angle round the mast (0 ahead, 90 to port) and a distance out.
        private static Vector3 Round(float degrees, float distance)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * distance, Mathf.Sin(a) * distance, 0f);
        }
    }
}
