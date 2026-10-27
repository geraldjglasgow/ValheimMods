using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The fairlead's meshes for a lead (<see cref="KwAnchorLead"/>), in the windlass's kit frame: a chock is a wooden seat
    /// on the cap or top edge with a metal saddle on it, two horns either side of the cable and a rounded lip under the cable
    /// at each face; a hawse pipe a metal tube through the wall with a thick flange round each mouth and its throat dark;
    /// an arm, when the anchor must hang further out, a metal bar out from the outer lip with a sheave at its end and a knee
    /// down to the hull; a deck lead an eye on a plate on the deck. Wood, metal and the dark throat in separate builders.
    /// </summary>
    public static class KwAnchorLeadShapes
    {
        /// <summary>A chock's seat's half length along the hull (past its horns), so it may sit snug beside a railing post.</summary>
        public const float SeatLong = 0.12f;

        // How far the seat overhangs each face and how high it is; the horns' place, thickness and height; a lip's radius;
        // the pipe's bore, its flange's radius and its collar's depth into the wall.
        private const float SeatOver = 0.04f, SeatHigh = 0.025f, Horn = 0.09f, HornThick = 0.015f, HornHigh = 0.12f;
        private const float LipRadius = 0.022f, Bore = 0.06f, Flange = 0.11f, Collar = 0.04f;
        private const int Round = 12;

        public static void Draw(KwAnchorLead lead, MeshBuilder wood, MeshBuilder metal, MeshBuilder dark)
        {
            KwAnchorSpot spot = lead.Spot!;
            if (spot.Pipe)
                Pipe(spot, metal, dark);
            else
                Chock(spot, wood, metal);
            if (lead.Reach > 0f)
                Arm(lead, metal);
            if (lead.DeckLead is Vector3 eye)
                Eye(eye, lead.Deck[0], lead.Deck[2], metal);
        }

        // The seat on the cap or edge; the saddle between the lips; the horns either side; the lips under the cable.
        private static void Chock(KwAnchorSpot s, MeshBuilder wood, MeshBuilder metal)
        {
            Vector3 up = new Vector3(0f, 0f, 1f), o = s.Outward, a = s.Along;
            float across = (s.Outset - s.Inset) / 2f, half = (s.Inset + s.Outset) / 2f;
            Block(wood, s.At + o * across + up * (SeatHigh / 2f), o, a, half + SeatOver, SeatLong, SeatHigh / 2f);
            float under = KwAnchorSpot.Rise - KwAnchorRoute.Width / 2f, saddle = half + KwAnchorSpot.Lip;
            Block(metal, s.At + o * across + up * ((SeatHigh + under) / 2f), o, a, saddle, 0.06f, (under - SeatHigh) / 2f);
            foreach (float side in new[] { -1f, 1f })
                Block(metal, s.At + o * across + a * (side * Horn) + up * ((SeatHigh + HornHigh) / 2f), o, a, saddle + LipRadius, HornThick, (HornHigh - SeatHigh) / 2f);
            foreach (Vector3 lip in new[] { s.Inner, s.Outer })
            {
                Vector3 c = lip - up * (KwAnchorRoute.Width / 2f + LipRadius);
                Solid.Log(metal, c - a * Horn, c + a * Horn, LipRadius, 8);
            }
        }

        // The tube through the wall, open, a flange with its collar round each mouth, the throat dark at each face.
        private static void Pipe(KwAnchorSpot s, MeshBuilder metal, MeshBuilder dark)
        {
            Vector3 o = s.Outward;
            Tube(metal, s.Inner, s.Outer, o, Bore);
            foreach ((float face, float way) in new[] { (s.Outset, 1f), (-s.Inset, -1f) })
            {
                Vector3 at = s.At + o * face;
                Tube(metal, at - o * (way * Collar), at + o * (way * 0.012f), o, Flange);
                Ring(metal, at + o * (way * 0.012f), o, Bore, Flange);
                Ring(dark, at + o * (way * 0.006f), o, 0f, Bore);
            }
        }

        // The arm from the outer lip out to the hawse, its sheave there and a knee down to the hull.
        private static void Arm(KwAnchorLead lead, MeshBuilder metal)
        {
            KwAnchorSpot s = lead.Spot!;
            Vector3 drop = new Vector3(0f, 0f, KwAnchorRoute.Width / 2f + 0.03f);
            Solid.Log(metal, s.Outer - drop, lead.Hawse - drop, 0.028f, 8);
            Solid.Log(metal, lead.Hawse - drop - s.Along * 0.05f, lead.Hawse - drop + s.Along * 0.05f, 0.03f, 10);
            if (lead.Knee is Vector3 foot)
                Solid.Log(metal, lead.Hawse - drop * 2f, foot, 0.022f, 8);
        }

        // A deck lead: a plate on the deck, a post up from it, the eye round the cable square to its way.
        private static void Eye(Vector3 eye, Vector3 from, Vector3 to, MeshBuilder metal)
        {
            Vector3 up = new Vector3(0f, 0f, 1f), way = to - from;
            way = new Vector3(way.x, way.y, 0f).normalized;
            Vector3 side = new Vector3(-way.y, way.x, 0f), floor = eye - up * 0.14f;
            Block(metal, floor + up * 0.01f, way, side, 0.09f, 0.07f, 0.01f);
            Solid.Log(metal, floor + up * 0.02f, eye - up * 0.05f, 0.016f, 6);
            for (int i = 0; i < 8; i++)
            {
                float a0 = i * Mathf.PI / 4f, a1 = (i + 1) * Mathf.PI / 4f;
                Solid.Log(metal, eye + (side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 0.05f, eye + (side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 0.05f, 0.014f, 5);
            }
        }

        // A box about a middle, along three ways (o, a, up) with its half sizes.
        private static void Block(MeshBuilder b, Vector3 c, Vector3 o, Vector3 a, float ho, float ha, float hz)
        {
            Vector3 u = o * ho, v = a * ha, w = new Vector3(0f, 0f, hz);
            Solid.Hexa(b, new[] { c - u - v - w, c + u - v - w, c + u + v - w, c - u + v - w, c - u - v + w, c + u - v + w, c + u + v + w, c - u + v + w });
        }

        // An open tube from a to b about its axis (along o) of a radius: its wall only.
        private static void Tube(MeshBuilder b, Vector3 from, Vector3 to, Vector3 o, float radius)
        {
            (Vector3 u, Vector3 v) = Square(o);
            for (int i = 0; i < Round; i++)
            {
                Vector3 p = Rim(u, v, i, radius), q = Rim(u, v, i + 1, radius);
                b.Polygon(new List<Vector3> { from + p, to + p, to + q, from + q }, new List<Vector2> { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            }
        }

        // A flat ring (or a disc, from 0) square to o at a point.
        private static void Ring(MeshBuilder b, Vector3 at, Vector3 o, float inner, float outer)
        {
            (Vector3 u, Vector3 v) = Square(o);
            for (int i = 0; i < Round; i++)
            {
                List<Vector3> quad = new List<Vector3> { at + Rim(u, v, i, inner), at + Rim(u, v, i, outer), at + Rim(u, v, i + 1, outer), at + Rim(u, v, i + 1, inner) };
                b.Polygon(quad, Solid.Planar(quad));
            }
        }

        private static (Vector3 U, Vector3 V) Square(Vector3 o) => (new Vector3(-o.y, o.x, 0f).normalized, new Vector3(0f, 0f, 1f));

        private static Vector3 Rim(Vector3 u, Vector3 v, int i, float radius) =>
            (u * Mathf.Cos(i * 2f * Mathf.PI / Round) + v * Mathf.Sin(i * 2f * Mathf.PI / Round)) * radius;
    }
}
