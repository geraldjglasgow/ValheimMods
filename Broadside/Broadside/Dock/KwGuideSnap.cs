using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Keel block guides join end to end easily: a guide's ghost within <see cref="Reach"/> of another guide's end turns to
    /// lie in line with that guide and snaps the matching end of its own onto it (its start onto the other's far end, or
    /// its far end onto the other's start), so a row of guides runs straight. The game snaps only within half a metre and
    /// keeps the ghost's own turn, so on a 4 m board lying flat the player had to aim at the end exactly and the next guide
    /// often met the last at an angle. Called from the kit's snap rules (<see cref="KwSnapRules"/>) for a guide's ghost.
    /// </summary>
    public static class KwGuideSnap
    {
        public const string Prefab = "KW_dockguide";
        private const float Reach = 2.5f;

        /// <summary>The ghost's end and the other guide's end to meet, the ghost turned in line; false with no guide end near.</summary>
        public static bool Find(Transform ghost, List<Transform> theirs, List<Transform> mine, out Transform? a, out Transform? b)
        {
            a = b = null;
            Transform? end = NearestEnd(ghost.position, theirs);
            Transform? guide = end != null ? end.parent : null;
            if (end == null || guide == null)
                return false;
            ghost.rotation = guide.rotation;
            bool farEnd = end.localPosition.z > 0.5f * KwDock.GuideLength;
            foreach (Transform point in mine)
            {
                bool start = point.localPosition.z < 0.5f * KwDock.GuideLength;
                if (point.name == "Guide end" && start == farEnd)
                    a = point;
            }
            b = end;
            return a != null;
        }

        // The nearest "Guide end" of another guide within reach of the ghost's place.
        private static Transform? NearestEnd(Vector3 at, List<Transform> theirs)
        {
            Transform? nearest = null;
            float best = Reach;
            foreach (Transform point in theirs)
            {
                float d = Vector3.Distance(point.position, at);
                if (point.name != "Guide end" || d >= best || point.parent == null || Utils.GetPrefabName(point.parent.gameObject) != Prefab)
                    continue;
                (nearest, best) = (point, d);
            }
            return nearest;
        }
    }
}
