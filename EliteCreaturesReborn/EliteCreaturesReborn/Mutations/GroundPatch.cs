using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// One laid patch of a ground trail as this machine holds it: where it lies on the ground, how far it reaches, the
    /// local moment it is gone (the moment its drawing has faded out), and what it does to a player standing in it. A
    /// patch outlives the creature that laid it: it is this machine's own record, not the creature's.
    /// </summary>
    internal struct GroundPatch
    {
        /// <summary>
        /// How far above or below the patch a player's feet may be and still stand in it: not on a bridge over it.
        /// </summary>
        private const float Reach = 1.5f;

        public Vector3 Point;
        public float RadiusSq;
        public float Until;
        public float Slow;
        public float Grip;

        /// <summary>True when feet at <paramref name="feet"/> stand inside the patch.</summary>
        public bool Covers(Vector3 feet)
        {
            float dx = feet.x - Point.x;
            float dz = feet.z - Point.z;
            return dx * dx + dz * dz <= RadiusSq && Mathf.Abs(feet.y - Point.y) <= Reach;
        }
    }
}
