using System.Collections.Generic;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The pieces of the same type joined to a piece: every piece with the same prefab reached through boxes of that
    /// prefab that touch (within <see cref="Touch"/> m), nearest first, up to a cap. A wall run, a roof slope, a row of
    /// floor tiles, a fence.
    /// </summary>
    internal static class SmartSameType
    {
        private const float Touch = 0.15f;

        public static List<int> Around(SmartModel model, Blueprint bp, int start, int cap)
        {
            string prefab = bp.Pieces[start].Prefab;
            List<int> found = new List<int> { start };
            HashSet<int> seen = new HashSet<int> { start };
            for (int next = 0; next < found.Count && found.Count < cap; next++)
            {
                SmartBox box = model.Boxes[found[next]];
                foreach (int other in model.Hash.Near(box.X, box.Z, box.Reach + Touch))
                {
                    if (found.Count >= cap || seen.Contains(other) || bp.Pieces[other].Prefab != prefab || !box.Touches(model.Boxes[other], Touch))
                        continue;
                    seen.Add(other);
                    found.Add(other);
                }
            }
            found.Sort();
            return found;
        }
    }
}
