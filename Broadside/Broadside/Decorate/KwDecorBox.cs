using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rectangle on the ground, turned any way (a footprint, a keep-clear zone): its middle, its own x way across the
    /// ground and half its size along its x and z. Containment and overlap (separating axes), in plain arithmetic; heights
    /// are ignored.
    /// </summary>
    public readonly struct KwDecorBox
    {
        public KwDecorBox(Vector3 centre, Vector3 right, float halfX, float halfZ)
        {
            Vector3 flat = new Vector3(right.x, 0f, right.z);
            Right = flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.right;
            (Centre, HalfX, HalfZ) = (centre, halfX, halfZ);
        }

        public Vector3 Centre { get; }
        public Vector3 Right { get; }
        public float HalfX { get; }
        public float HalfZ { get; }

        /// <summary>Its own z way across the ground.</summary>
        public Vector3 Forward => new Vector3(-Right.z, 0f, Right.x);

        /// <summary>A footprint of a size at a pose: its x and z ranges in the piece's own frame.</summary>
        public static KwDecorBox Of(Vector3 origin, Quaternion turn, float minX, float maxX, float minZ, float maxZ)
        {
            Vector3 right = turn * Vector3.right, forward = turn * Vector3.forward;
            Vector3 middle = origin + right * ((minX + maxX) / 2f) + forward * ((minZ + maxZ) / 2f);
            return new KwDecorBox(middle, right, (maxX - minX) / 2f, (maxZ - minZ) / 2f);
        }

        /// <summary>A world box's footprint (axis-aligned), grown on every side.</summary>
        public static KwDecorBox Around(Bounds bounds, float grow) =>
            new KwDecorBox(bounds.center, Vector3.right, bounds.extents.x + grow, bounds.extents.z + grow);

        /// <summary>The same box grown on every side.</summary>
        public KwDecorBox Grown(float grow) => new KwDecorBox(Centre, Right, HalfX + grow, HalfZ + grow);

        /// <summary>Level metres from a point to the box (0 inside it).</summary>
        public float Distance(Vector3 point)
        {
            Vector3 d = point - Centre, forward = Forward;
            float x = Mathf.Max(0f, Mathf.Abs(d.x * Right.x + d.z * Right.z) - HalfX), z = Mathf.Max(0f, Mathf.Abs(d.x * forward.x + d.z * forward.z) - HalfZ);
            return Mathf.Sqrt(x * x + z * z);
        }

        public bool Contains(Vector3 point)
        {
            Vector3 d = point - Centre;
            return Mathf.Abs(d.x * Right.x + d.z * Right.z) <= HalfX && Mathf.Abs(d.x * Forward.x + d.z * Forward.z) <= HalfZ;
        }

        /// <summary>Whether the two overlap, or come closer than a margin.</summary>
        public bool Overlaps(KwDecorBox other, float margin)
        {
            KwDecorBox a = Grown(margin);
            return !a.Apart(other, a.Right) && !a.Apart(other, a.Forward) && !a.Apart(other, other.Right) && !a.Apart(other, other.Forward);
        }

        // Whether an axis separates the two.
        private bool Apart(KwDecorBox other, Vector3 axis)
        {
            Vector3 d = other.Centre - Centre;
            float gap = Mathf.Abs(d.x * axis.x + d.z * axis.z);
            return gap > Reach(axis) + other.Reach(axis);
        }

        // Half the box's length along an axis across the ground.
        private float Reach(Vector3 axis)
        {
            Vector3 forward = Forward;
            return HalfX * Mathf.Abs(Right.x * axis.x + Right.z * axis.z) + HalfZ * Mathf.Abs(forward.x * axis.x + forward.z * axis.z);
        }
    }
}
