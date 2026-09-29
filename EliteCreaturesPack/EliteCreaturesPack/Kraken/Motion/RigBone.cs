using UnityEngine;

namespace EliteCreaturesPack.Kraken.Motion
{
    /// <summary>
    /// A bone of the kraken's model turned about an axis of the head (or tentacle) it belongs to, whatever axes the
    /// model's import gave the bone itself: the turn is made in the world about the owner's axis, on top of the bone's
    /// rest pose under its parent as it is now.
    /// </summary>
    public sealed class RigBone
    {
        private readonly Quaternion _rest;
        private readonly Vector3 _restScale;

        public RigBone(Transform bone)
        {
            Bone = bone;
            _rest = bone.localRotation;
            _restScale = bone.localScale;
        }

        public Transform Bone { get; }

        /// <summary>At rest under its parent, turned <paramref name="degrees"/> about <paramref name="axis"/> of <paramref name="owner"/>.</summary>
        public void Turn(Transform owner, Vector3 axis, float degrees)
        {
            Quaternion rest = Bone.parent != null ? Bone.parent.rotation * _rest : _rest;
            Bone.rotation = Quaternion.AngleAxis(degrees, owner.TransformDirection(axis)) * rest;
        }

        /// <summary>Its rest size times <paramref name="factor"/>.</summary>
        public void Scale(float factor) => Bone.localScale = _restScale * factor;

        /// <summary>A bone anywhere under <paramref name="root"/> by name, or null.</summary>
        public static RigBone? Find(Transform root, string name)
        {
            Transform? bone = Named(root, name);
            return bone != null ? new RigBone(bone) : null;
        }

        /// <summary>A transform anywhere under <paramref name="root"/> by name, or null.</summary>
        public static Transform? Named(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
