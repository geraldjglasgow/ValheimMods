using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's hull closed at its ends, in boxes across them like its sides' (<see cref="KwSkinBox"/>): a flat
    /// stern's transom (a planking part's <c>FlatFore</c> or <c>FlatAft</c> end, its outer face on the drawn transom board's,
    /// <see cref="KwPlanks.TransomBoard"/>), a keel end (a stem or a pointed stern, where the part ends at no rib, its face on
    /// the end's station, inside the post), and a cabin's raised end the hull closes (a board from the deck to the roof,
    /// <see cref="KwCabinShape"/>). Each stretch of the end's section between the two sides is a box as wide as its
    /// narrower end, and where it widens by more than <see cref="Step"/> a box along the stretch on each side covering the
    /// triangle that leaves, so the wall follows the outline (a row a height, as wide as its widest point, stood out of a
    /// V-shaped stern by up to its own height). The hull's sides were boxes along it, so before any wall a player could walk
    /// out through a flat stern. Kit axes of the part. Pure.
    /// </summary>
    public static class KwShipTransomWall
    {
        private const float Step = 0.05f, Board = 0.02f, Inside = 0.025f;

        /// <summary>A planking part's closed ends: its transoms and its keel ends.</summary>
        public static List<KwSkinBox> Add(KwDef def)
        {
            List<KwSkinBox> boxes = new List<KwSkinBox>();
            List<Station> st = def.Stations;
            if (st.Count < 2)
                return boxes;
            if (def.FlatFore || !def.RibEnd)
                Wall(boxes, st[st.Count - 1].Half, st[st.Count - 1].X, 1f, def.FlatFore ? Board : 0f);
            if (def.FlatAft || !def.RibStart)
                Wall(boxes, st[0].Half, st[0].X, -1f, def.FlatAft ? Board : 0f);
            return boxes;
        }

        /// <summary>A cabin's raised ends the hull closes, as its look boards them (a pointed end's from the deck, a hair inside the hull's end).</summary>
        public static List<KwSkinBox> Raised(KwCabinSpec spec)
        {
            List<KwSkinBox> boxes = new List<KwSkinBox>();
            List<Station> raised = KwCabinShape.Raised(spec);
            if (raised.Count < 2 || !raised.Exists(s => s.Half[1].y - s.Half[0].y > 0.01f))
                return boxes;
            RaisedEnd(boxes, spec, raised[0], spec.Back, -1f);
            RaisedEnd(boxes, spec, raised[raised.Count - 1], spec.Front, 1f);
            return boxes;
        }

        private static void RaisedEnd(List<KwSkinBox> boxes, KwCabinSpec spec, Station at, KwCabinEnd end, float outward)
        {
            if (!KwCabinOutline.Closed(spec, end) || at.Half[1].y - at.Half[0].y <= 0.05f)
                return;
            if (end.Hull == KwCabinEnd.Flat)
                Wall(boxes, at.Half, at.X, outward, Board);
            else
                Wall(boxes, new[] { new Vector2(at.Half[0].x, Mathf.Min(at.Half[0].y, spec.Floor)), at.Half[1] }, at.X - outward * Inside, outward, Board);
        }

        // Across a section at x, its boxes' outer face stand out of x (outward along the part), each stretch of the section.
        private static void Wall(List<KwSkinBox> boxes, Vector2[] half, float x, float outward, float stand)
        {
            for (int j = 0; j + 1 < half.Length; j++)
            {
                Vector2 p = half[j], q = half[j + 1];
                if ((q - p).magnitude < 0.01f)
                    continue;
                float lo = Mathf.Min(p.x, q.x), hi = Mathf.Max(p.x, q.x);
                bool edges = hi - lo > Step;
                if ((edges ? lo : hi) > 0.01f)
                    Across(boxes, x + outward * stand, outward, edges ? lo : hi, p.y, q.y);
                if (!edges)
                    continue;
                foreach (float side in new[] { 1f, -1f })
                    Edge(boxes, x + outward * stand, outward, new Vector2(side * p.x, p.y), new Vector2(side * q.x, q.y), hi - lo);
            }
        }

        // A box across the section, half wide either side of the middle, from height z0 to z1, its outer face at x.
        private static void Across(List<KwSkinBox> boxes, float x, float outward, float half, float z0, float z1)
        {
            float w = Mathf.Max(2f * half + 2f * KwSkinFit.Margin, KwSkinFit.Least), h = Mathf.Max(Mathf.Abs(z1 - z0) + 2f * KwSkinFit.Margin, KwSkinFit.Least);
            Vector3 centre = new Vector3(x - outward * KwSkinFit.Thick / 2f, 0f, (z0 + z1) / 2f);
            boxes.Add(new KwSkinBox(centre, new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f), new Vector3(outward, 0f, 0f), new Vector3(KwSkinFit.Thick, h, w)));
        }

        // A box along a stretch of one side's outline (p to q in across, height), reaching in over the triangle between it
        // and the box across (wide its difference in width).
        private static void Edge(List<KwSkinBox> boxes, float x, float outward, Vector2 p, Vector2 q, float wide)
        {
            Vector2 run = q - p, d = run.normalized, inward = new Vector2(-d.y, d.x);
            inward = Vector2.Dot(inward, new Vector2(-p.x - q.x, 0f)) < 0f ? -inward : inward;
            float reach = Mathf.Max(wide * Mathf.Abs(run.y) / run.magnitude + KwSkinFit.Margin, KwSkinFit.Least);
            Vector2 mid = (p + q) / 2f + inward * (reach / 2f - KwSkinFit.Margin);
            Vector3 centre = new Vector3(x - outward * KwSkinFit.Thick / 2f, mid.x, mid.y);
            boxes.Add(new KwSkinBox(centre, new Vector3(0f, d.x, d.y), new Vector3(0f, inward.x, inward.y), new Vector3(outward, 0f, 0f),
                new Vector3(KwSkinFit.Thick, reach, run.magnitude + 2f * KwSkinFit.Margin)));
        }
    }
}
