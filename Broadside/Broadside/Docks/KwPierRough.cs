using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The plain wood dock, the rough and cheap tier (the user, 2026-11-03: "The normal wood docks need to look more ...
    /// rugged, maybe with some smaller pieces too, more un-uniform pieces. It's the crappiest tier of dock."; core wood and
    /// fine wood keep <see cref="KwPierShape"/>'s neat look). Its deck: rows of boards across it, each row its own width
    /// (<see cref="MinWidth"/> to <see cref="MaxWidth"/>) with uneven gaps between rows (a wider one now and then), each row
    /// cut into short boards of their own lengths (many butt joints), every board a little askew, tilted, cupped and at its
    /// own height (a centimetre or so), its ends ragged past the deck's sides; now and then a row's end board broken short
    /// at a side (only at the outer edge, over the joist there); and a few short offcuts nailed across the boards. Where
    /// another dock meets a side, or a ladder hangs from it (<see cref="KwPierGaps.Openings"/>), the boards end flush and
    /// whole; the first and last rows lie square, so sections carried on meet edge to edge. Its frame is
    /// <see cref="KwPierRoughFrame"/>'s. Everything from the section's seed, its draws the same whatever meets it (a branch
    /// built later changes only the boards' ends at it, not the deck), so every peer and every load builds the same. Only
    /// the mesh is uneven: the colliders stay <see cref="KwPierBody"/>'s flat deck.
    /// </summary>
    public static class KwPierRough
    {
        private const float MinWidth = 0.14f, MaxWidth = 0.3f, MinLength = 0.45f, MaxLength = 1.7f, ShortestEnd = 0.3f, Joint = 0.004f;
        private const float Gap = 0.006f, GapMost = 0.02f, WideGap = 0.025f, WideGapMost = 0.05f, WideShare = 0.12f;
        private const float Lift = 0.008f, Tilt = 0.008f, Cup = 0.006f, Yaw = 0.008f, Thin = 0.05f, Thick = 0.06f;
        private const float OverLeast = -0.03f, OverMost = 0.05f, BrokenShare = 0.07f, BrokenLeast = 0.1f, BrokenMost = 0.35f;
        private const float PatchEvery = 9f, PatchIn = 0.45f, PatchThick = 0.025f, Flushed = 0.05f;

        /// <summary>A rough wood section's deck and frame into the mesh, its curbs and board ends as the gaps say.</summary>
        public static void Build(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierGaps gaps)
        {
            KwPierUv uv = KwPierUv.For(kind, spec.Seed);
            System.Random random = new System.Random(spec.Seed);
            List<Vector2> rows = Rows(spec.Length, random);
            for (int i = 0; i < rows.Count; i++)
                Row(mesh, spec, rows[i], i == 0 || i == rows.Count - 1, random, uv, gaps);
            Patches(mesh, spec, random, uv);
            KwPierRoughFrame.Build(mesh, spec, kind, uv, random, gaps);
        }

        /// <summary>A number from a to b, drawn.</summary>
        public static float Draw(System.Random random, float a, float b) => a + (b - a) * (float)random.NextDouble();

        /// <summary>Whether a side is open (another dock, a ladder) anywhere along a span of it, so what reaches past the edge there ends flush.</summary>
        public static bool Flush(KwPierGaps gaps, int edge, float from, float to)
        {
            foreach (Vector2 open in gaps.Openings(edge))
            {
                if (open.x < to + Flushed && open.y > from - Flushed)
                    return true;
            }
            return false;
        }

        // The rows' spans along the section (from, to): widths and gaps drawn until it is filled, then all scaled to fill it
        // exactly, with or without the last row, whichever is scaled less.
        private static List<Vector2> Rows(float length, System.Random random)
        {
            List<float> sizes = new List<float>();
            float total = 0f;
            while (total < length)
            {
                float width = Draw(random, MinWidth, MaxWidth), gap = random.NextDouble() < WideShare ? Draw(random, WideGap, WideGapMost) : Draw(random, Gap, GapMost);
                sizes.Add(width);
                sizes.Add(gap);
                total += width + gap;
            }
            sizes.RemoveAt(sizes.Count - 1);
            float all = Sum(sizes), fewer = sizes.Count > 2 ? all - sizes[sizes.Count - 1] - sizes[sizes.Count - 2] : float.MaxValue;
            if (Mathf.Abs(length / fewer - 1f) < Mathf.Abs(length / all - 1f))
            {
                sizes.RemoveRange(sizes.Count - 2, 2);
                all = fewer;
            }
            return Spans(sizes, length / all, length);
        }

        private static float Sum(List<float> sizes)
        {
            float sum = 0f;
            foreach (float size in sizes)
                sum += size;
            return sum;
        }

        private static List<Vector2> Spans(List<float> sizes, float scale, float length)
        {
            List<Vector2> rows = new List<Vector2>();
            float z = 0f;
            for (int i = 0; i < sizes.Count; i += 2)
            {
                float end = i + 2 >= sizes.Count ? length : z + sizes[i] * scale;
                rows.Add(new Vector2(z, end));
                if (i + 1 < sizes.Count)
                    z = end + sizes[i + 1] * scale;
            }
            return rows;
        }

        // One row of boards across the deck: its ends past the sides (flush where open, its cuts the same either way, so a
        // side opened later moves nothing else), cut into lengths, now and then its end board at one side broken short; a
        // square row (the first and last) lies straight.
        private static void Row(KwPierMesh mesh, KwPierSpec spec, Vector2 span, bool square, System.Random random, KwPierUv uv, KwPierGaps gaps)
        {
            float half = spec.Width * 0.5f;
            bool broken = random.NextDouble() < BrokenShare, left = random.NextDouble() < 0.5;
            float shorter = Draw(random, BrokenLeast, BrokenMost), jag = Draw(random, 0.03f, 0.08f);
            float overLeft = Draw(random, OverLeast, OverMost), overRight = Draw(random, OverLeast, OverMost);
            bool flushLeft = Flush(gaps, KwPierGaps.Left, span.x, span.y), flushRight = Flush(gaps, KwPierGaps.Right, span.x, span.y);
            List<float> cuts = Cuts(-half - overLeft, half + overRight, random);
            broken &= !square && !(left ? flushLeft : flushRight);
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i] + (i > 0 ? Joint : 0f), b = cuts[i + 1] - (i + 2 < cuts.Count ? Joint : 0f);
                Vector2 frame = new Vector2(a, b), ends = Vector2.zero;
                (a, b) = (i == 0 && flushLeft ? -half : a, i + 2 == cuts.Count && flushRight ? half : b);
                if (broken && left && i == 0)
                    (a, ends.x) = (Mathf.Min(a + shorter, b - 0.15f), jag);
                if (broken && !left && i + 2 == cuts.Count)
                    (b, ends.y) = (Mathf.Max(b - shorter, a + 0.15f), jag);
                Board(mesh, new Vector4(a, b, span.x, span.y), frame, ends, square, random, uv);
            }
        }

        // Where a row is cut into boards across the deck, from one end to the other: lengths of their own, the last never
        // shorter than ShortestEnd.
        private static List<float> Cuts(float from, float to, System.Random random)
        {
            List<float> cuts = new List<float> { from };
            float x = from;
            while (true)
            {
                x += Draw(random, MinLength, MaxLength);
                if (to - x < ShortestEnd)
                    break;
                cuts.Add(x);
            }
            cuts.Add(to);
            return cuts;
        }

        // A board from x a to b across the row from z0 to z1 (box: a, b, z0, z1), its own height, tilt, cup and skew about the
        // length it was cut to (frame: a board ended flush at an open side keeps the lie of the rest); an end broken short is
        // ragged (ends: how much shorter its one corner is at the start and at the end).
        private static void Board(KwPierMesh mesh, Vector4 box, Vector2 frame, Vector2 ends, bool square, System.Random random, KwPierUv uv)
        {
            float lift = Draw(random, -Lift, Lift), roll = Draw(random, -Tilt, Tilt), pitch = Draw(random, -Tilt, Tilt);
            float cup = Draw(random, 0f, Cup), yaw = Draw(random, -Yaw, Yaw), thick = Draw(random, Thin, Thick);
            float width = box.w - box.z, middle = (frame.x + frame.y) * 0.5f, span = frame.y - frame.x;
            yaw = square ? 0f : yaw;
            Vector3[] c = new Vector3[8];
            for (int k = 0; k < 4; k++)
            {
                bool far = k == 2 || k == 3, wide = k == 1 || k == 2;
                float x = far ? box.y - (wide ? ends.y : 0f) : box.x + (wide ? 0f : ends.x);
                float z = (wide ? box.w : box.z) + (x - middle) * yaw;
                float y = lift + roll * (wide ? 0.5f : -0.5f) + pitch * ((x - middle) / span);
                (c[k], c[k + 4]) = (new Vector3(x, y - thick, z), new Vector3(x, y, z));
            }
            KwPierBlock.Make(mesh, c, uv, uv.Board(box.y - box.x, width), width > 0.2f ? cup : 0f);
        }

        // A few short offcuts nailed across the boards (one per PatchEvery m² or so), well in from the sides and ends.
        private static void Patches(KwPierMesh mesh, KwPierSpec spec, System.Random random, KwPierUv uv)
        {
            float area = spec.Width * spec.Length / PatchEvery;
            int count = Mathf.FloorToInt(area) + (random.NextDouble() < area - Mathf.Floor(area) ? 1 : 0);
            float half = spec.Width * 0.5f - PatchIn;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = new Vector3(Draw(random, -half, half), Draw(random, 0.002f, 0.01f), Draw(random, PatchIn, spec.Length - PatchIn));
                Vector3 size = new Vector3(Draw(random, 0.11f, 0.18f), PatchThick, Draw(random, 0.35f, 0.7f));
                float turn = Draw(random, -0.35f, 0.35f);
                if (half > 0f && spec.Length > 2f * PatchIn + 0.2f)
                    Patch(mesh, at, size, turn, uv);
            }
        }

        // One offcut: its grain along the dock turned by so much, lying on the deck at its height.
        private static void Patch(KwPierMesh mesh, Vector3 at, Vector3 size, float turn, KwPierUv uv)
        {
            Vector3 grain = new Vector3(Mathf.Sin(turn), 0f, Mathf.Cos(turn)), side = new Vector3(Mathf.Cos(turn), 0f, -Mathf.Sin(turn));
            Vector3 start = at - grain * (size.z * 0.5f), end = at + grain * (size.z * 0.5f), w = side * (size.x * 0.5f), up = Vector3.up * size.y;
            Vector3[] c = { start - w, start + w, end + w, end - w, start - w + up, start + w + up, end + w + up, end - w + up };
            KwPierBlock.Make(mesh, c, uv, uv.Board(size.z, size.x));
        }
    }
}
