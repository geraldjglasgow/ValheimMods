using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The fittings a rope's foot is made fast to (<see cref="KwRigTie"/>), small, in the piece's kit axes, into four meshes by
    /// look: wood, the tier's metal, dark (the deadeyes' holes) and rope. A shroud's: an iron chainplate from over the gunwale
    /// (or the railing's top rail) down the side's outside, bolted at its foot, a strop round the lower deadeye at its top,
    /// and the upper deadeye on the shroud's line, each a round wooden block with three holes, a lanyard of three turns
    /// between them. A cleat: two feet and its horns along the rail, the gunwale or the mast. An eyebolt: a plate, an eye and
    /// a ring through it. No colliders anywhere: they only show.
    /// </summary>
    public static class KwTieShapes
    {
        // A deadeye's half thickness and how far its holes are from its middle; the chainplate's width and thickness.
        private const float Half = 0.024f, Holes = 0.03f, Width = 0.045f, Thick = 0.012f;

        private static readonly Vector3 Up = new Vector3(0f, 0f, 1f);

        public static void Draw(KwRigTie tie, MeshBuilder wood, MeshBuilder metal, MeshBuilder dark, MeshBuilder rope)
        {
            if (tie.Kind == KwTieKind.Deadeye)
            {
                Deadeyes(tie, wood, metal, dark, rope);
                Strap(metal, tie);
            }
            else if (tie.Kind == KwTieKind.Cleat)
                Cleat(wood, rope, tie);
            else
                Eyebolt(metal, tie);
        }

        // The two deadeyes on the shroud's line, square to it and out of the side, the lanyard between their holes.
        private static void Deadeyes(KwRigTie tie, MeshBuilder wood, MeshBuilder metal, MeshBuilder dark, MeshBuilder rope)
        {
            Vector3 lead = (tie.Upper - tie.Seat).normalized;
            Vector3 axis = (tie.Out - lead * Vector3.Dot(tie.Out, lead)).normalized;
            Vector3 across = Vector3.Cross(axis, lead).normalized;
            Vector3[] low = Block(wood, dark, tie.Seat, axis, lead, across), high = Block(wood, dark, tie.Upper, axis, lead, across);
            for (int i = 0; i < 3; i++)
                KwRound.Taper(rope, low[i], high[i], 0.008f, 0.008f, 5);
            KwRound.Ring(metal, tie.Seat, axis, KwRigTie.Block + 0.008f, 0.008f, 14, 4);
        }

        // A round block with three holes through it (dark plugs proud of both faces); the holes' middles.
        private static Vector3[] Block(MeshBuilder wood, MeshBuilder dark, Vector3 middle, Vector3 axis, Vector3 lead, Vector3 across)
        {
            KwRound.Taper(wood, middle - axis * Half, middle + axis * Half, KwRigTie.Block, KwRigTie.Block, 12);
            Vector3[] holes = new Vector3[3];
            for (int k = 0; k < 3; k++)
            {
                float a = (90f + 120f * k) * Mathf.Deg2Rad;
                holes[k] = middle + (lead * Mathf.Sin(a) + across * Mathf.Cos(a)) * Holes;
                KwRound.Taper(dark, holes[k] - axis * (Half + 0.003f), holes[k] + axis * (Half + 0.003f), 0.011f, 0.011f, 6);
            }
            return holes;
        }

        // The chainplate from under the lower deadeye down the side's outside, two bolts through its foot.
        private static void Strap(MeshBuilder metal, KwRigTie tie)
        {
            List<Vector3> p = tie.Strap;
            for (int i = 0; i + 1 < p.Count; i++)
                Bar(metal, p[i], p[i + 1], tie.Way);
            Vector3 foot = p[p.Count - 1], up = p[p.Count - 2];
            foreach (float t in new[] { 0.1f, 0.4f })
            {
                Vector3 bolt = Vector3.Lerp(foot, up, t) + tie.Out * (Thick / 2f);
                KwRound.Taper(metal, bolt, bolt + tie.Out * 0.01f, 0.012f, 0.01f, 6);
            }
        }

        // A flat iron bar from a to c, its width along the side.
        private static void Bar(MeshBuilder b, Vector3 a, Vector3 c, Vector3 way)
        {
            Vector3 w = way.normalized * (Width / 2f), n = Vector3.Cross(c - a, way).normalized * (Thick / 2f);
            Solid.Hexa(b, new[] { a - w - n, a + w - n, a + w + n, a - w + n, c - w - n, c + w - n, c + w + n, c - w + n });
        }

        // Two feet on the face it is fixed to, its horns along it, a turn of the rope round them.
        private static void Cleat(MeshBuilder wood, MeshBuilder rope, KwRigTie tie)
        {
            Vector3 normal = tie.OnRail ? -tie.Out : Up, way = tie.Way, seat = tie.Seat;
            foreach (float s in new[] { -0.05f, 0.05f })
                KwRound.Taper(wood, seat + way * s, seat + way * s + normal * 0.04f, 0.016f, 0.013f, 6);
            Vector3 middle = seat + normal * 0.045f;
            KwRound.Taper(wood, middle, middle + way * 0.11f, 0.017f, 0.009f, 6);
            KwRound.Taper(wood, middle, middle - way * 0.11f, 0.017f, 0.009f, 6);
            KwRound.Ring(rope, middle + normal * 0.004f, normal, 0.045f, 0.008f, 12, 4);
        }

        // A round plate on the deck, the eye standing on it, and a ring through the eye.
        private static void Eyebolt(MeshBuilder metal, KwRigTie tie)
        {
            Vector3 seat = tie.Seat;
            KwRound.Taper(metal, seat, seat + Up * 0.008f, 0.035f, 0.033f, 10);
            KwRound.Taper(metal, seat, seat + Up * 0.03f, 0.01f, 0.01f, 6);
            KwRound.Ring(metal, seat + Up * 0.04f, tie.Way, 0.015f, 0.006f, 10, 5);
            KwRound.Ring(metal, seat + Up * 0.05f, tie.Out, 0.025f, 0.0065f, 12, 5);
        }

        /// <summary>A rope through points (kit) as a round tube of a width, a straight piece between each two (a ghost's ropes).</summary>
        public static void Tube(MeshBuilder rope, List<Vector3> points, float width)
        {
            for (int i = 0; i + 1 < points.Count; i++)
                KwRound.Taper(rope, points[i], points[i + 1], width / 2f, width / 2f, 5);
        }
    }
}
