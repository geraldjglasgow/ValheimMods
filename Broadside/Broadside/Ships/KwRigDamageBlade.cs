using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rudder blade for its damage (<see cref="KwRigDamage"/>), plain arithmetic on its tier and its fit
    /// to the post (<see cref="KwRudderSpan"/>), in the blade's own kit axes (the stock's axis at the origin, the blade aft
    /// of it toward -x, z up): its box (as <see cref="KwRudder"/> sizes the blade's collider), its boards' area from the fore
    /// edge to its outline, foot to tip, and the share of it its chunks (<see cref="KwRigMark.Chunk"/>) leave: each chunk
    /// takes its boards from where it broke them off to the outline, over its height, the boards' ends ragged
    /// (<see cref="BrokenAt"/>); a chunk with the whole blade takes everything aft of the stock.
    /// </summary>
    public static class KwRigBlade
    {
        private const int Steps = 48;
        private const float Board = 0.12f, Ragged = 0.1f;

        /// <summary>The blade's box in its kit axes: its middle and its sizes.</summary>
        public static (Vector3 Middle, Vector3 Size) Box(KwRigPiece rudder)
        {
            KwRudderFit fit = rudder.Fit;
            KwRudderSpan span = rudder.Tier!.Span(fit);
            float length = rudder.Tier.Longest() * span.Chord + 0.05f, foot = Mathf.Min(0f, span.Foot), tall = fit.Height + 0.15f * fit.Thick - foot;
            return (new Vector3(-length / 2f, 0f, foot + tall / 2f), new Vector3(length, 0.12f * Mathf.Max(fit.Thick, span.Thick) + 0.06f, tall));
        }

        /// <summary>A point in the rudder piece's kit axes as on its blade turned amidships.</summary>
        public static Vector3 Rest(KwRigPiece rudder, Vector3 kit) => kit - rudder.Tier!.Pivot(rudder.Fit);

        /// <summary>Where the blade's fore edge is (x, a little aft of the stock's axis), as its boards start.</summary>
        public static float Fore(KwRigPiece rudder) => -0.3f * rudder.Tier!.Stock(rudder.Fit);

        /// <summary>
        /// Where a chunk breaks the boards off at a height (kit x; everything aft of it gone), ragged board by board; null
        /// when the chunk does not reach that height.
        /// </summary>
        public static float? BrokenAt(KwRigPiece rudder, KwRigMark chunk, float z)
        {
            if (chunk.D >= 0.5f)
                return -(rudder.Tier!.Stock(rudder.Fit) + 0.02f);
            float board = Mathf.Floor(z / Board), reach = chunk.C * (0.75f + 0.5f * KwWearNoise.Hash((int)board, chunk.Seed));
            if (Mathf.Abs(z - chunk.A) > reach)
                return null;
            return chunk.B + Ragged * (KwWearNoise.Hash((int)board, chunk.Seed + 7) - 0.5f);
        }

        /// <summary>Where the blade is broken off at a height by any of its chunks (the most forward), or null where it is whole.</summary>
        public static float? BrokenAt(KwRigPiece rudder, List<KwRigMark> chunks, float z)
        {
            float? most = null;
            foreach (KwRigMark chunk in chunks)
            {
                float? x = BrokenAt(rudder, chunk, z);
                if (x.HasValue && (!most.HasValue || x.Value > most.Value))
                    most = x;
            }
            return most;
        }

        /// <summary>The most forward place (kit x) any of a blade's chunks breaks it at: nothing fore of it is cut.</summary>
        public static float Forward(KwRigPiece rudder, List<KwRigMark> chunks)
        {
            float most = float.MinValue;
            foreach (KwRigMark chunk in chunks)
                most = Mathf.Max(most, chunk.D >= 0.5f ? -(rudder.Tier!.Stock(rudder.Fit) + 0.02f) : chunk.B + Ragged / 2f);
            return most;
        }

        /// <summary>The heights where a blade's breaks change (each chunk's boards' edges and its ragged ends within them), in order.</summary>
        public static List<float> Heights(KwRigPiece rudder, List<KwRigMark> chunks)
        {
            List<float> heights = new List<float>();
            foreach (KwRigMark chunk in chunks)
            {
                if (chunk.D >= 0.5f)
                    continue;
                for (int board = Mathf.FloorToInt((chunk.A - 1.3f * chunk.C) / Board); board * Board < chunk.A + 1.3f * chunk.C; board++)
                {
                    float reach = chunk.C * (0.75f + 0.5f * KwWearNoise.Hash(board, chunk.Seed));
                    heights.AddRange(new[] { board * Board, (board + 1) * Board, chunk.A - reach, chunk.A + reach });
                }
            }
            heights.Sort();
            return heights;
        }

        /// <summary>The share of the blade's boards its chunks leave (1 whole, 0 none).</summary>
        public static float Left(KwRigPiece rudder, List<KwRigMark> chunks)
        {
            if (chunks.Count == 0 || rudder.Tier == null)
                return 1f;
            if (chunks.Exists(c => c.D >= 0.5f))
                return 0f;
            KwRudderSpan span = rudder.Tier.Span(rudder.Fit);
            float fore = Fore(rudder), dz = (span.Tip - span.Foot) / Steps, whole = 0f, kept = 0f;
            for (int i = 0; i < Steps; i++)
            {
                float z = span.Foot + (i + 0.5f) * dz, aft = -span.AftAt(z), wide = Mathf.Max(0f, fore - aft);
                float? broken = BrokenAt(rudder, chunks, z);
                whole += wide;
                kept += broken.HasValue ? Mathf.Clamp(fore - broken.Value, 0f, wide) : wide;
            }
            return whole > 0f ? kept / whole : 1f;
        }
    }
}
