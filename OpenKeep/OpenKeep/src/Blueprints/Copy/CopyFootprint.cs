using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>The size of some pieces' drawn footprint in a frame turned by a yaw: x across its front, y from front to back, metres.</summary>
    internal static class CopyFootprint
    {
        public static Vector2 Of(IEnumerable<FixPiece> boxes, float yaw)
        {
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            Vector3 across = turn * Vector3.right, along = turn * Vector3.forward;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (FixPiece p in boxes)
            {
                Vector2 centre = new Vector2(Vector3.Dot(p.Centre, across), Vector3.Dot(p.Centre, along));
                Vector2 half = new Vector2(CopyTouch.Extent(p, across), CopyTouch.Extent(p, along));
                min = Vector2.Min(min, centre - half);
                max = Vector2.Max(max, centre + half);
            }
            return min.x > max.x ? Vector2.zero : max - min;
        }
    }
}
