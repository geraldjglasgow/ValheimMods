using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's hull sides as boxes lying on the planking's own face (the user, 2026-10-26: a cannonball that hit
    /// the wood between two holes did nothing; each side had been one flat box a station gap, a straight chord from the
    /// bilge to the sheer up to 0.7 m inside a full hull, so a shot flew on into the hull and met it inside a hole's
    /// passage). Between every two stations each stretch of the section (keel to bilge, bilge to topsides, ...) is one box
    /// on each side (<see cref="KwSkinFit"/>), its outer face on the strakes' face and its thickness inward, laid the way
    /// that keeps it on the drawn face (<see cref="KwSkinSheet"/>): its corners within <see cref="Off"/> of its plane and its
    /// face within <see cref="Stray"/> of the face round it. Where no way does (the strakes climbing a bow or a high stern
    /// make a stretch lean), it is cut in two and each half laid again, a few times at most (<see cref="Deepest"/>). A strip
    /// across the bottom closes the keel between the garboards; stations closer than <see cref="Apart"/> are taken as one. A
    /// cabin's raised sides the same way, sheer to roof. Each box is cut round the openings it carries
    /// (<see cref="KwSkinOpenings"/>), and holes cut them where they show (<see cref="KwHoleBoxes"/>). Worked out once a
    /// shape (<see cref="KwSkinCache"/>). Kit axes of the part; the same on every peer, a dedicated server included. Pure.
    /// </summary>
    public static class KwHullSkin
    {
        /// <summary>The name of a planking part's boxes (the hit router takes their normals in the section's plane, <see cref="KwShipHitClass"/>).</summary>
        public const string Name = "hull";

        private const float Off = 0.02f, Stray = 0.035f, Piece = 0.15f, Apart = 0.12f;
        private const int Deepest = 3;

        /// <summary>A planking part's sides and bottom strip (its def: stations, ends at ribs), cut round its openings; shared, never changed.</summary>
        public static IReadOnlyList<KwSkinBox> Planking(KwDef def, List<KwOpening> openings)
        {
            (bool before, bool after) = (def.RibStart && !def.FlatAft, def.RibEnd && !def.FlatFore);
            return KwSkinCache.Of(KwSkinCache.Key("planking", (before ? 1 : 0) + (after ? 2 : 0), def.Stations, openings), () => Planking(def.Stations, openings, before, after));
        }

        private static List<KwSkinBox> Planking(List<Station> stations, List<KwOpening> openings, bool before, bool after)
        {
            List<KwSkinBox> boxes = new List<KwSkinBox>();
            List<Station> st = Spaced(stations);
            if (st.Count < 2)
                return boxes;
            List<KwSkinBore> bores = KwSkinOpenings.Of(openings);
            Sheet(boxes, KwSkinSheet.Side(st, 1f, before, after), KwPlanks.Lap / 2f, bores, KwOpening.Port);
            Sheet(boxes, KwSkinSheet.Side(st, -1f, before, after), KwPlanks.Lap / 2f, bores, KwOpening.Starboard);
            Sheet(boxes, KwSkinSheet.Keel(st, before, after), 0f, bores, "");
            return boxes;
        }

        /// <summary>A cabin's raised hull sides, on each side at the hull, cut round their openings (none where the hull is not raised); shared, never changed.</summary>
        public static IReadOnlyList<KwSkinBox> Raised(KwCabinSpec spec)
        {
            List<Station> raised = KwCabinShape.Raised(spec);
            List<KwOpening> openings = KwOpeningSurfaces.RaisedOpenings(spec);
            int sides = (spec.Left.AtHull ? 1 : 0) + (spec.Right.AtHull ? 2 : 0);
            return KwSkinCache.Of(KwSkinCache.Key("raised", sides, raised, openings), () => Raised(spec, raised, openings));
        }

        private static List<KwSkinBox> Raised(KwCabinSpec spec, List<Station> raised, List<KwOpening> openings)
        {
            List<KwSkinBox> boxes = new List<KwSkinBox>();
            List<Station> st = Spaced(raised);
            if (st.Count < 2 || !st.Exists(s => s.Half[1].y - s.Half[0].y > 0.01f))
                return boxes;
            List<KwSkinBore> bores = KwSkinOpenings.Of(openings);
            if (spec.Left.AtHull)
                Sheet(boxes, KwSkinSheet.Side(st, 1f, below: true), KwPlanks.Lap / 2f, bores, KwOpening.RaisedLeft);
            if (spec.Right.AtHull)
                Sheet(boxes, KwSkinSheet.Side(st, -1f, below: true), KwPlanks.Lap / 2f, bores, KwOpening.RaisedRight);
            return boxes;
        }

        /// <summary>The stations with any closer than <see cref="Apart"/> to the last kept left out (the last always kept, in place of a close one).</summary>
        public static List<Station> Spaced(List<Station> stations)
        {
            List<Station> kept = new List<Station>();
            for (int i = 0; i < stations.Count; i++)
            {
                bool last = i == stations.Count - 1;
                if (kept.Count == 0 || stations[i].X - kept[kept.Count - 1].X >= Apart)
                    kept.Add(stations[i]);
                else if (last && kept.Count > 1)
                    kept[kept.Count - 1] = stations[i];
            }
            return kept;
        }

        // A sheet's boxes, stretch by stretch: a straight run of gaps one box (KwSkinRuns), else quad by quad; each cut
        // round the wall's openings.
        private static void Sheet(List<KwSkinBox> boxes, KwSkinSheet sheet, float stand, List<KwSkinBore> bores, string wall)
        {
            List<KwSkinBox> made = new List<KwSkinBox>();
            for (int j = sheet.Low; j + 1 < sheet.Width; j++)
            {
                for (int i = sheet.First, n; i < sheet.Last; i += n)
                {
                    n = KwSkinRuns.Of(sheet, i, j, stand, Off, out KwSkinBox run);
                    if (n > 1)
                        made.Add(run);
                    else
                        Patch(made, sheet, new KwSkinPatch(i, j, 0f, 1f, 0f, 1f), stand, 0);
                }
            }
            foreach (KwSkinBox box in made)
                boxes.AddRange(KwSkinOpenings.Cut(box, bores, wall));
        }

        /// <summary>
        /// The boxes over part of a quad: one where it fits, laid along the strakes or (its ends on the stations) up the
        /// sections, the longer way first, or at the sheer along it; else the part cut in two across the first way (a box
        /// along the strakes into strips, one up the sections along) and each half done again, never into pieces under
        /// <see cref="Piece"/>.
        /// </summary>
        public static void Patch(List<KwSkinBox> boxes, KwSkinSheet sheet, KwSkinPatch q, float stand, int depth)
        {
            Vector3 a0 = q.At(sheet, 0f, 0f), a1 = q.At(sheet, 0f, 1f), c0 = q.At(sheet, 1f, 0f), c1 = q.At(sheet, 1f, 1f);
            float along = Vector3.Distance(a0, c0) + Vector3.Distance(a1, c1), up = Vector3.Distance(a0, a1) + Vector3.Distance(c0, c1);
            bool upright = up > along, top = q.J == sheet.Width - 2 && q.T1 >= 1f;
            if (Laid(sheet, q, stand, upright ? KwSkinFit.Upright : KwSkinFit.Along, upright ? KwSkinFit.Along : KwSkinFit.Upright, top, out KwSkinBox box, out bool fits) && fits)
            {
                boxes.Add(box);
                return;
            }
            bool canAlong = along >= 4f * Piece, canUp = up >= 4f * Piece, cutUp = upright ? !canAlong && canUp : canUp;
            if (depth >= Deepest || !(cutUp ? canUp : canAlong))
            {
                boxes.Add(box);
                return;
            }
            Patch(boxes, sheet, q.Half(cutUp, false), stand, depth + 1);
            Patch(boxes, sheet, q.Half(cutUp, true), stand, depth + 1);
        }

        // A box for a part: of the ways (the two given, then along the sheer at the top), one whose face is near a rectangle
        // of the part, else the nearest such that, where it runs past the part, it is still on the face round it (fits),
        // else the nearest; false for a part with no box.
        private static bool Laid(KwSkinSheet sheet, KwSkinPatch q, float stand, int first, int second, bool top, out KwSkinBox best, out bool fits)
        {
            (float over, KwSkinBox box) one = Lay(sheet, q, stand, first);
            (best, fits) = (one.box, one.over <= Stray);
            if (fits)
                return true;
            (float over, KwSkinBox box) two = Lay(sheet, q, stand, second), three = top ? Lay(sheet, q, stand, KwSkinFit.Top) : (float.PositiveInfinity, default);
            if (two.over < one.over)
                (one, two) = (two, one);
            if (three.over < two.over)
                (two, three) = (three, two);
            if (two.over < one.over)
                (one, two) = (two, one);
            (best, fits) = (one.box, false);
            foreach ((float over, KwSkinBox box) in new[] { one, two, three })
            {
                if (over <= Stray || (over < float.MaxValue && Close(sheet, q, box, stand)))
                {
                    (best, fits) = (box, true);
                    break;
                }
            }
            return one.over < float.PositiveInfinity;
        }

        // A part's box laid one way and how far it runs past the part (more than any for one too twisted, infinite for none).
        private static (float Over, KwSkinBox Box) Lay(KwSkinSheet sheet, KwSkinPatch q, float stand, int lay)
        {
            Vector3 a0 = q.At(sheet, 0f, 0f), a1 = q.At(sheet, 0f, 1f), c0 = q.At(sheet, 1f, 0f), c1 = q.At(sheet, 1f, 1f);
            if (!KwSkinFit.Of(a0, a1, c0, c1, sheet.Out(q.I, q.J), stand, lay, out KwSkinBox box, out float off))
                return (float.PositiveInfinity, default);
            return (off <= Off ? KwSkinFit.Over(box, a0, a1, c0, c1) : float.MaxValue, box);
        }

        // Whether the box's face lies within Stray of the drawn face round its quad: its corners, then its edges' middles.
        private static bool Close(KwSkinSheet sheet, KwSkinPatch q, KwSkinBox box, float stand)
        {
            Vector3 mid = KwSkinFit.Plane(box, stand), along = box.Along * (box.Size.z / 2f), up = box.Up * (box.Size.y / 2f);
            foreach ((int a, int b) in Samples)
            {
                if (!sheet.Near(mid + along * a + up * b, q.I, q.J, Stray))
                    return false;
            }
            return true;
        }

        private static readonly (int, int)[] Samples = { (-1, -1), (1, 1), (-1, 1), (1, -1), (0, -1), (0, 1), (-1, 0), (1, 0) };
    }

    /// <summary>Part of a sheet's quad (<see cref="KwSkinSheet"/>): the quad (i, j) and the share of it from s0 to s1 between its rows, t0 to t1 up it.</summary>
    public readonly struct KwSkinPatch
    {
        public KwSkinPatch(int i, int j, float s0, float s1, float t0, float t1) => (I, J, S0, S1, T0, T1) = (i, j, s0, s1, t0, t1);

        public int I { get; }
        public int J { get; }
        public float S0 { get; }
        public float S1 { get; }
        public float T0 { get; }
        public float T1 { get; }

        /// <summary>The sheet's point at a share of this part (s between its rows, t up).</summary>
        public Vector3 At(KwSkinSheet sheet, float s, float t) => sheet.Point(I, J, Mathf.Lerp(S0, S1, s), Mathf.Lerp(T0, T1, t));

        /// <summary>One half: cut up into strips (the lower or upper) or along (the first or second).</summary>
        public KwSkinPatch Half(bool strips, bool second)
        {
            float s = (S0 + S1) / 2f, t = (T0 + T1) / 2f;
            if (strips)
                return second ? new KwSkinPatch(I, J, S0, S1, t, T1) : new KwSkinPatch(I, J, S0, S1, T0, t);
            return second ? new KwSkinPatch(I, J, s, S1, T0, T1) : new KwSkinPatch(I, J, S0, s, T0, T1);
        }
    }
}
