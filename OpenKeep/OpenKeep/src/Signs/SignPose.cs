using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>Where a sign belongs: the world point its bottom centre sits on and its rotation.</summary>
    public readonly struct SignPose
    {
        public SignPose(Vector3 bottom, Quaternion rotation)
        {
            Bottom = bottom;
            Rotation = rotation;
        }

        public Vector3 Bottom { get; }

        public Quaternion Rotation { get; }
    }
}
