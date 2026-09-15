using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>One prefab's entry of the <c>containers:</c> map: whether it gets a sign and where the sign sits.</summary>
    public sealed class SignRule
    {
        public SignRule(bool enabled, Vector3 offset, float rotation)
        {
            Enabled = enabled;
            Offset = offset;
            Rotation = rotation;
        }

        /// <summary>The rule of every prefab that is not listed: a sign at the computed place.</summary>
        public static SignRule Default { get; } = new SignRule(true, Vector3.zero, 0f);

        public bool Enabled { get; }

        /// <summary>Metres from the computed place in the container's own axes: x right, y up, z forward.</summary>
        public Vector3 Offset { get; }

        /// <summary>Degrees added to the container's yaw, on top of the Rotation setting.</summary>
        public float Rotation { get; }
    }
}
