using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>The ground (terrain, rock or a built floor) under a point, for an effect drawn on the ground there.</summary>
    public static class GroundPoint
    {
        private const float Above = 0.5f;
        private const float Down = 6f;

        private static int mask;

        /// <summary>The first ground below the point (looked for from half a metre above it), else the point itself.</summary>
        public static Vector3 Below(Vector3 point)
        {
            if (mask == 0)
                mask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            return Physics.Raycast(point + Vector3.up * Above, Vector3.down, out RaycastHit hit, Down, mask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : point;
        }
    }
}
