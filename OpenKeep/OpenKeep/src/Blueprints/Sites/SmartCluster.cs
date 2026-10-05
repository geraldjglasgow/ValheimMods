using System.Collections.Generic;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Smart select for a piece that belongs to no building (a fence, a road or quay tile, a stretch of town wall):
    /// the pieces joined to it through boxes that touch (within <see cref="Touch"/> m), nearest first, leaving out every
    /// piece that belongs to a building on the storey, up to a cap.
    /// </summary>
    internal static class SmartCluster
    {
        private const float Touch = 0.15f;

        public static List<int> Around(SmartModel model, SmartLevel level, int start, int cap)
        {
            List<int> found = new List<int> { start };
            HashSet<int> seen = new HashSet<int> { start };
            for (int next = 0; next < found.Count && found.Count < cap; next++)
            {
                SmartBox box = model.Boxes[found[next]];
                foreach (int other in model.Hash.Near(box.X, box.Z, box.Reach + Touch))
                {
                    if (found.Count < cap && level.Owners[other] == 0 && !seen.Contains(other) && box.Touches(model.Boxes[other], Touch))
                    {
                        seen.Add(other);
                        found.Add(other);
                    }
                }
            }
            found.Sort();
            return found;
        }
    }
}
