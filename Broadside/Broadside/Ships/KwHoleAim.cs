using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Which of a launched ship's holes a ray is aimed at, plain arithmetic in the ship's frame: where the ray meets each
    /// hole's plane (through its place, square to its normal), within the hole's radius and a margin round it, in front of
    /// the ray's start and before a farthest distance; the nearest along the ray. A ray running along a face (crossing it at
    /// less than <see cref="Slant"/>) aims at none of its holes. A ray through the middle of a hole meets no collider of the
    /// ship there, so this is how the repair kit's use (<see cref="KwHullPatchSpot"/>) and the hammer's repair
    /// (<see cref="KwShipRepairWork"/>) find a hole.
    /// </summary>
    public static class KwHoleAim
    {
        public const float Slant = 0.15f;

        /// <summary>The index of the hole aimed at and how far along the ray it meets it; -1 for none.</summary>
        public static int Find(List<KwHullHole> holes, Vector3 from, Vector3 way, float margin, float farthest, out float along)
        {
            (int found, along) = (-1, farthest);
            for (int i = 0; i < holes.Count; i++)
            {
                float t = Meet(holes[i], from, way, margin);
                if (t >= 0f && t < along)
                    (found, along) = (i, t);
            }
            return found;
        }

        /// <summary>How far along a ray (its way of length one) it meets a hole within the hole's radius and a margin; -1 when it does not.</summary>
        public static float Meet(KwHullHole hole, Vector3 from, Vector3 way, float margin)
        {
            Vector3 normal = hole.Normal;
            float across = Vector3.Dot(way, normal);
            if (Mathf.Abs(across) < Slant)
                return -1f;
            float t = Vector3.Dot(hole.Place - from, normal) / across;
            if (t < 0f)
                return -1f;
            float reach = hole.Radius + margin;
            return (from + way * t - hole.Place).sqrMagnitude <= reach * reach ? t : -1f;
        }

        /// <summary>The index of the hole whose edge is nearest a point (all in the ship's frame), within reach; -1 for none.</summary>
        public static int Nearest(List<KwHullHole> holes, Vector3 point, float reach)
        {
            (int found, float best) = (-1, reach);
            for (int i = 0; i < holes.Count; i++)
            {
                float edge = Vector3.Distance(holes[i].Place, point) - holes[i].Radius;
                if (edge < best)
                    (found, best) = (i, edge);
            }
            return found;
        }
    }
}
