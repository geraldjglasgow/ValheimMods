using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// A collider's wire shape in the world, sized the way the physics engine sizes it: a box scaled on each axis, a
    /// sphere by the largest scale, a capsule's radius by the larger of its two cross scales; anything else (mesh
    /// colliders, character controllers) as its world bounds.
    /// </summary>
    internal static class ColliderWire
    {
        internal static Vector3[] Of(Collider collider)
        {
            switch (collider)
            {
                case BoxCollider box:
                    return Shapes.Box(box.transform.TransformPoint(box.center), Vector3.Scale(box.size, Scale(box)) * 0.5f, box.transform.rotation);
                case SphereCollider sphere:
                    return Sphere(sphere);
                case CapsuleCollider capsule:
                    return Capsule(capsule);
                default:
                    return Shapes.Box(collider.bounds.center, collider.bounds.extents, Quaternion.identity);
            }
        }

        private static Vector3[] Sphere(SphereCollider sphere)
        {
            Vector3 scale = Scale(sphere);
            float radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            return Shapes.Sphere(sphere.transform.TransformPoint(sphere.center), radius, sphere.transform.rotation);
        }

        // direction 0, 1, 2 is the capsule's axis along local x, y, z; the other two span its cross-section.
        private static Vector3[] Capsule(CapsuleCollider capsule)
        {
            Vector3 scale = Scale(capsule);
            int axis = Mathf.Clamp(capsule.direction, 0, 2), a = (axis + 1) % 3, b = (axis + 2) % 3;
            float radius = capsule.radius * Mathf.Max(scale[a], scale[b]);
            float half = Mathf.Max(0f, capsule.height * scale[axis] * 0.5f - radius);
            Quaternion rotation = capsule.transform.rotation;
            return Shapes.Capsule(capsule.transform.TransformPoint(capsule.center), rotation * Unit(axis), rotation * Unit(a),
                rotation * Unit(b), radius, half);
        }

        private static Vector3 Scale(Collider collider)
        {
            Vector3 scale = collider.transform.lossyScale;
            return new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private static Vector3 Unit(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
    }
}
