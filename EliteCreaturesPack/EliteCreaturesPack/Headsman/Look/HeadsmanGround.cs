using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>The floor under a point, for what the Executioner puts on the ground (the slam's chunks, the shatter).</summary>
    public static class HeadsmanGround
    {
        private static readonly int Floors = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");

        /// <summary>The floor under a point (the crypt's, or the ground's), or the point itself when there is none close.</summary>
        public static Vector3 Floor(Vector3 point) =>
            Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 3f, Floors) ? hit.point : point;
    }
}
