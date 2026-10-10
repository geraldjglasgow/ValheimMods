using System;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// How the booth sets a model before it is turned: the pose it rests in and how far back the camera stands, both
    /// worked out once from the box round its meshes as it lies on the ground (axis-aligned, so its sides are the
    /// model's own length, width and thickness).
    /// </summary>
    internal static class BoothPose
    {
        internal const float Fov = 30f;
        private const float Margin = 1.2f;
        private const float Flat = 0.6f;  // thinnest side under this share of the longest: flat or long
        private const float Long = 2.5f;  // longest side over this many times the middle one: long
        private const float Turning = 0.7f; // share of the sphere round the box always in view, whichever way it is turned

        /// <summary>
        /// A flat or long thing (a shield, a sword, a bow) stood up with its broad side to the viewer and its length
        /// running up, a long one leaned across the picture as the game's icons show weapons; anything blockier (a
        /// helmet, a dish) stays as it lies.
        /// </summary>
        internal static Quaternion Rest(Vector3 extents)
        {
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            float[] sizes = { extents.x, extents.y, extents.z };
            int[] order = { 0, 1, 2 };
            Array.Sort(order, (a, b) => sizes[b].CompareTo(sizes[a]));
            float longest = sizes[order[0]], middle = sizes[order[1]], thinnest = sizes[order[2]];
            if (thinnest > longest * Flat) return Quaternion.identity;
            Quaternion facing = Quaternion.Inverse(Quaternion.LookRotation(-axes[order[2]], axes[order[0]]));
            return longest > middle * Long ? Quaternion.AngleAxis(-45f, Vector3.forward) * facing : facing;
        }

        /// <summary>A standing thing turned to face the viewer (its forward, +Z, towards the camera).</summary>
        internal static readonly Quaternion Facing = Quaternion.AngleAxis(180f, Vector3.up);

        /// <summary>
        /// A thing with a pose of its own stands as built, facing the viewer, instead (<see cref="Rest"/> is for things
        /// that lie): a creature, a building piece, or an animated rig that is not an item, such as a workshop creature's model.
        /// </summary>
        internal static bool Standing(GameObject prefab) =>
            prefab.GetComponent<Character>() || prefab.GetComponent<Piece>()
            || (!prefab.GetComponent<ItemDrop>() && prefab.GetComponentInChildren<Animator>(true) && prefab.GetComponentInChildren<SkinnedMeshRenderer>(true));

        /// <summary>
        /// From the middle of the box to the camera: far enough that the resting model fills the picture with a margin,
        /// and never so near that turning it pushes much of it out.
        /// </summary>
        internal static float Distance(Vector3 extents, Quaternion rest)
        {
            Vector3 seen = Turned(rest, extents);
            float half = Fov * 0.5f * Mathf.Deg2Rad;
            float resting = Mathf.Max(seen.x, seen.y) * Margin / Mathf.Tan(half) + seen.z;
            float turning = Turning * extents.magnitude / Mathf.Sin(half);
            return Mathf.Max(0.05f, Mathf.Max(resting, turning));
        }

        // The half-sizes of the box once turned, along the picture's right, up and depth.
        private static Vector3 Turned(Quaternion turn, Vector3 extents)
        {
            Matrix4x4 m = Matrix4x4.Rotate(turn);
            return new Vector3(
                Mathf.Abs(m.m00) * extents.x + Mathf.Abs(m.m01) * extents.y + Mathf.Abs(m.m02) * extents.z,
                Mathf.Abs(m.m10) * extents.x + Mathf.Abs(m.m11) * extents.y + Mathf.Abs(m.m12) * extents.z,
                Mathf.Abs(m.m20) * extents.x + Mathf.Abs(m.m21) * extents.y + Mathf.Abs(m.m22) * extents.z);
        }
    }
}
