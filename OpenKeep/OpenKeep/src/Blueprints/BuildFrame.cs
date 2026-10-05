using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Where a blueprint goes: the frame's origin on the levelled ground (<see cref="Origin"/>.y is the ground height)
    /// and its yaw. Frame x and z turn with <c>Quaternion.Euler(0, Yaw, 0)</c>, so a yaw equal to the camera's puts the
    /// blueprint's front (its -z side, the door) toward the player.
    /// </summary>
    public struct BuildFrame
    {
        public Vector3 Origin;
        public float Yaw;

        public BuildFrame(Vector3 origin, float yaw)
        {
            Origin = origin;
            Yaw = (yaw % 360f + 360f) % 360f;
        }

        public float Ground => Origin.y;

        public Quaternion Rotation => Quaternion.Euler(0f, Yaw, 0f);

        /// <summary>A frame point (x across, y above the ground, z back) in the world.</summary>
        public Vector3 World(float x, float y, float z) => Origin + Rotation * new Vector3(x, y, z);

        /// <summary>A world point's frame x and z (height ignored).</summary>
        public Vector2 Local(float worldX, float worldZ)
        {
            float t = Yaw * Mathf.Deg2Rad;
            float dx = worldX - Origin.x, dz = worldZ - Origin.z;
            return new Vector2(dx * Mathf.Cos(t) - dz * Mathf.Sin(t), dx * Mathf.Sin(t) + dz * Mathf.Cos(t));
        }

        /// <summary>The world rotation of a piece with the given frame yaw.</summary>
        public Quaternion PieceRotation(float yaw) => Quaternion.Euler(0f, Yaw + yaw, 0f);

        public bool SameAs(BuildFrame other)
        {
            return (Origin - other.Origin).sqrMagnitude < 0.0001f && Mathf.Abs(Mathf.DeltaAngle(Yaw, other.Yaw)) < 0.01f;
        }
    }
}
