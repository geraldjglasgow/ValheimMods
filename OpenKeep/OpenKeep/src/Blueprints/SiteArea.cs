using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The ground a blueprint asks for, in its own frame: the pad (saved relief first, else the last levelled square
    /// holding the point), the paint (the blueprint's painted squares first, else dirt where a piece stands on the
    /// ground), and the areas around it in the world.
    /// </summary>
    public static class SiteArea
    {
        /// <summary>The pad height above the levelled ground at a frame point; false outside the pad.</summary>
        public static bool PadOffset(Blueprint bp, float x, float z, out float offset)
        {
            float? relief = bp.Relief?.At(x, z);
            if (relief.HasValue)
            {
                offset = relief.Value;
                return true;
            }
            for (int i = bp.Levels.Count - 1; i >= 0; i--)
            {
                if (bp.Levels[i].Contains(x, z))
                {
                    offset = bp.Levels[i].Y;
                    return true;
                }
            }
            offset = 0f;
            return false;
        }

        /// <summary>The paint at a frame point: the last painted square holding it, else dirt under the building, else none.</summary>
        public static GroundPaint PaintAt(Blueprint bp, GroundMask mask, float x, float z)
        {
            for (int i = bp.Paints.Count - 1; i >= 0; i--)
            {
                if (bp.Paints[i].Contains(x, z))
                    return bp.Paints[i].Paint;
            }
            return mask.Covered(x, z) ? GroundPaint.Dirt : GroundPaint.None;
        }

        /// <summary>The frame rectangle holding the pad, the painted squares and the pieces.</summary>
        public static Rect Local(Blueprint bp)
        {
            Rect r = bp.PieceBounds;
            foreach (LevelStep s in bp.Levels)
                r = Union(r, Rect.MinMaxRect(s.X - s.Half, s.Z - s.Half, s.X + s.Half, s.Z + s.Half));
            foreach (PaintStep s in bp.Paints)
                r = Union(r, Rect.MinMaxRect(s.X - s.Half, s.Z - s.Half, s.X + s.Half, s.Z + s.Half));
            if (bp.Relief != null)
                r = Union(r, new Rect(bp.Relief.X0, bp.Relief.Z0, bp.Relief.Width - 1, bp.Relief.Depth - 1));
            return r;
        }

        /// <summary>The world rectangle (x, z) holding the frame rectangle grown by <paramref name="margin"/> metres.</summary>
        public static Rect World(Blueprint bp, BuildFrame frame, float margin)
        {
            Rect local = Local(bp);
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (Vector2 c in Corners(local, margin))
            {
                Vector3 w = frame.World(c.x, 0f, c.y);
                minX = Mathf.Min(minX, w.x);
                maxX = Mathf.Max(maxX, w.x);
                minZ = Mathf.Min(minZ, w.z);
                maxZ = Mathf.Max(maxZ, w.z);
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        /// <summary>The four frame corners of a rectangle grown by a margin, counter-clockwise from the front left.</summary>
        public static IEnumerable<Vector2> Corners(Rect r, float margin)
        {
            yield return new Vector2(r.xMin - margin, r.yMin - margin);
            yield return new Vector2(r.xMax + margin, r.yMin - margin);
            yield return new Vector2(r.xMax + margin, r.yMax + margin);
            yield return new Vector2(r.xMin - margin, r.yMax + margin);
        }

        /// <summary>The centre and radius of a circle in the world holding the whole frame rectangle grown by a margin.</summary>
        public static void Circle(Blueprint bp, BuildFrame frame, float margin, out Vector3 centre, out float radius)
        {
            Rect local = Local(bp);
            centre = frame.World(local.center.x, 0f, local.center.y);
            radius = Mathf.Sqrt(local.width * local.width + local.height * local.height) * 0.5f + margin;
        }

        private static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
    }
}
