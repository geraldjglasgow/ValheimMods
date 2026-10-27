using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the Doorway and Window tools' ghost snaps on a side (the user's request, 2026-10-25: "windows are on the side of
    /// a ship, but they're equally spaced", and "an easy way to centre things in a free stretch of a wall"), all in the
    /// aimed part's measures. A window's sill: level with a sill of the side's windows within <see cref="SillReach"/> of the
    /// aim, else the type's own height over the deck inside within that reach, else on a 5 cm step. Along: on a side with
    /// openings of the tool's kind, whole spacings from the one nearest the aim (a place another stands on left out); the
    /// middle of a free stretch of the side (<see cref="KwOpeningRun"/>) within <see cref="CentreReach"/> of the aim, where
    /// the opening fits in it; whichever of the two is nearer the aim wins, the spacing when the two are within
    /// <see cref="Together"/> of each other; with neither, a 5 cm step. Both count only in the aimed part's section
    /// (<see cref="KwOpeningLine.Section"/>: the hull's planking from rib to rib together, or an end part alone), the line
    /// and stretches given being that section's. Pure.
    /// </summary>
    public static class KwOpeningSnap
    {
        /// <summary>How near the aim a sill or a stretch's middle snaps, and the step when nothing does.</summary>
        public const float SillReach = 0.3f, CentreReach = 0.4f, Step = 0.05f;

        /// <summary>How near a stretch's middle and a whole spacing stand to count as one place: the spacing's then.</summary>
        public const float Together = 0.15f;

        /// <summary>A window's sill for an aimed sill: level with the side's, at the type's height over the deck, or stepped; and in words.</summary>
        public static (float Sill, string Words) Sill(float aimed, KwOpeningLine line, float? floor, float above)
        {
            float best = SillReach + 0.001f, sill = Round(aimed, Step);
            string words = "";
            foreach (KwLineOpening o in line.Openings)
            {
                if (Mathf.Abs(o.Sill - aimed) < best)
                    (best, sill, words) = (Mathf.Abs(o.Sill - aimed), o.Sill, "level with the others");
            }
            if (words.Length == 0 && floor != null && Mathf.Abs(floor.Value + above - aimed) <= SillReach)
                (sill, words) = (floor.Value + above, $"{above:0.0#} m over the deck");
            return (sill, words);
        }

        /// <summary>
        /// The place along for an aimed place: whole spacings from the line's nearest opening or a free stretch's middle
        /// (the opening wide as given with its frames), whichever is nearer; and what it is, in words.
        /// </summary>
        public static (float U, string Words) Along(float aimed, KwOpeningLine line, float spacing, List<KwFreeSpan> spans, float wide)
        {
            (float u, float off, string words) = (Round(aimed, Step), float.MaxValue, "");
            float? grid = Grid(aimed, line, spacing);
            if (grid != null)
                (u, off, words) = (grid.Value, Mathf.Abs(grid.Value - aimed), $"Equally spaced, {spacing:0.0#} m apart");
            foreach (KwFreeSpan span in spans)
            {
                float d = Mathf.Abs(span.Centre - aimed);
                bool apart = grid == null || Mathf.Abs(span.Centre - grid.Value) > Together;
                if (span.Length >= wide - 0.001f && d <= CentreReach && d < off - 0.001f && apart)
                    (u, off, words) = (span.Centre, d, KwOpeningSpans.Words(span));
            }
            return (u, words);
        }

        /// <summary>The place whole spacings from the side's opening nearest the aim nearest the aim, where no other stands; null on a side with none.</summary>
        public static float? Grid(float aimed, KwOpeningLine line, float spacing)
        {
            KwLineOpening? nearest = line.Nearest(aimed);
            if (nearest == null || spacing < KwOpeningSpacing.Closest - 0.001f)
                return null;
            int k = Mathf.RoundToInt((aimed - nearest.U) / spacing);
            float? best = null;
            foreach (int step in new[] { k, k - 1, k + 1, k - 2, k + 2 })
            {
                float at = nearest.U + step * spacing;
                if (!Taken(line, at) && (best == null || Mathf.Abs(at - aimed) < Mathf.Abs(best.Value - aimed)))
                    best = at;
            }
            return best;
        }

        // Another of the side's openings stands there.
        private static bool Taken(KwOpeningLine line, float u) => line.Openings.Exists(o => Mathf.Abs(o.U - u) < 0.05f);

        public static float Round(float value, float step) => Mathf.Round(value / step) * step;
    }
}
