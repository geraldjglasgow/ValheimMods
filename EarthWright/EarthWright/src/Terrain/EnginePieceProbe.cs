using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Physics probes for building pieces over the ground (the "skip under buildings" filter). A point counts as covered
    /// when a collider on the game's "piece" layer reaches into a thin column from half a metre below the ground to three
    /// metres above it; a box query rather than a ray, so a tall piece standing on the point is found even though a ray
    /// would start inside it.
    /// </summary>
    public static class EnginePieceProbe
    {
        private static int mask;

        private static int Mask => mask != 0 ? mask : (mask = LayerMask.GetMask("piece"));

        public static bool AnyNear(Vector3 center, float radius)
        {
            Vector3 half = new Vector3(radius, 1000f, radius);
            return Physics.CheckBox(new Vector3(center.x, 0f, center.z), half, Quaternion.identity, Mask, QueryTriggerInteraction.Ignore);
        }

        public static bool Covered(float x, float ground, float z)
        {
            Vector3 center = new Vector3(x, ground + 1.25f, z);
            return Physics.CheckBox(center, new Vector3(0.3f, 1.75f, 0.3f), Quaternion.identity, Mask, QueryTriggerInteraction.Ignore);
        }
    }
}
