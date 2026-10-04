using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Keeps the camera out of the ground: a sphere about the size of the game camera's own collision probe is swept
    /// from where the camera is to where it would go, against the <c>terrain</c> layer (the ground, and the floors,
    /// walls and ceilings of caves built into it) and <c>static_solid</c> (rocks, cliffs, dungeon walls). On a hit it
    /// stops just short and slides along the surface for the rest of the move. Building pieces, trees and creatures are
    /// not in the mask, so the camera passes through walls into a house.
    /// </summary>
    public static class CameraCollision
    {
        private const float Radius = 0.3f;
        private const float Skin = 0.05f;
        private const float MinMove = 1e-4f;

        private static int mask;

        private static int Mask => mask != 0 ? mask : (mask = LayerMask.GetMask("terrain", "static_solid"));

        public static Vector3 Move(Vector3 from, Vector3 to)
        {
            Vector3 stop = Sweep(from, to, out Vector3 normal);
            if (normal == Vector3.zero)
                return stop;
            Vector3 slide = Vector3.ProjectOnPlane(to - stop, normal);
            return Sweep(stop, stop + slide, out _);
        }

        /// <summary>As far toward the target as the sphere goes; the normal of what it hit, or zero when nothing.</summary>
        private static Vector3 Sweep(Vector3 from, Vector3 to, out Vector3 normal)
        {
            normal = Vector3.zero;
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < MinMove)
                return from;
            Vector3 direction = delta / distance;
            if (!Physics.SphereCast(from, Radius, direction, out RaycastHit hit, distance + Skin, Mask, QueryTriggerInteraction.Ignore))
                return to;
            normal = hit.normal;
            return from + direction * Mathf.Max(0f, hit.distance - Skin);
        }
    }
}
