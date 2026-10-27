using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's wall slabs cut round the openings in them (<see cref="KwSkinOpenings"/>, <see cref="KwHoleGaps"/>),
    /// so the crew can see, walk and shoot through an opening where its look shows one. A slab is a rectangle in its own
    /// measures (along its face and up it); each opening's bounds there are taken out of it, and what is left comes back as
    /// rectangles that cover the rest of the slab and nothing of an opening (slivers thinner than <see cref="Least"/> left
    /// out). A planking part's openings as its look cuts them. Pure.
    /// </summary>
    public static class KwOpeningBoxes
    {
        /// <summary>
        /// The thinnest piece of a slab kept as a box: a hair, since a piece is as long as the slab it was cut from (a sliver
        /// left out between two of a hole's overlapping rectangles was a crack across the whole box).
        /// </summary>
        public const float Least = 0.002f;

        /// <summary>A planking part's openings that fit its sides, as its look cuts them; none for a part with none.</summary>
        public static List<KwOpening> Planking(KwSpan span)
        {
            List<KwOpening> fitting = new List<KwOpening>();
            if (span.Openings.Length == 0)
                return fitting;
            foreach (KwOpeningWall wall in KwOpeningSurfaces.Planking(span.Stations(), KwOpeningText.Read(span.Openings)))
                fitting.AddRange(wall.Openings);
            return fitting;
        }

        /// <summary>A rectangle less some others, as rectangles covering the rest of it.</summary>
        public static List<Rect> Apart(Rect slab, List<Rect> holes)
        {
            List<Rect> pieces = new List<Rect> { slab };
            foreach (Rect hole in holes)
            {
                List<Rect> next = new List<Rect>();
                foreach (Rect piece in pieces)
                    Cut(piece, hole, next);
                pieces = next;
            }
            pieces.RemoveAll(r => r.width < Least || r.height < Least);
            return pieces;
        }

        // A rectangle a hole overlaps cut into the strips below and above the hole (its whole width) and the pieces either
        // side of the hole between them; one it does not overlap kept whole.
        private static void Cut(Rect r, Rect hole, List<Rect> into)
        {
            if (!r.Overlaps(hole))
            {
                into.Add(r);
                return;
            }
            float z0 = Mathf.Max(r.yMin, hole.yMin), z1 = Mathf.Min(r.yMax, hole.yMax);
            if (hole.yMin > r.yMin)
                into.Add(Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, z0));
            if (hole.yMax < r.yMax)
                into.Add(Rect.MinMaxRect(r.xMin, z1, r.xMax, r.yMax));
            if (hole.xMin > r.xMin)
                into.Add(Rect.MinMaxRect(r.xMin, z0, hole.xMin, z1));
            if (hole.xMax < r.xMax)
                into.Add(Rect.MinMaxRect(hole.xMax, z0, r.xMax, z1));
        }
    }
}
