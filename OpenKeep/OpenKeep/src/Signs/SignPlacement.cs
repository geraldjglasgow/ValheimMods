using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>
    /// Where a container's sign sits: the centre of the container's mesh renderer bounds in x and z, their top in y
    /// plus <c>Height</c>, plus the prefab's YAML offset turned into world space; yaw = container yaw + <c>Rotation</c>
    /// + the YAML rotation. The pose names the sign's bottom, so a placed sign is shifted until the bottom of its
    /// own renderer bounds sits on the pose; the pivot-to-bottom distance of the vanilla sign is remembered from the
    /// first sign measured, so later signs are created in the right place at once.
    /// </summary>
    public static class SignPlacement
    {
        private const float Tolerance = 0.02f;
        private const float YawTolerance = 1f;
        private static float? pivotToBottom;

        public static SignPose Compute(Container container)
        {
            SignRule rule = SignRules.RuleFor(container);
            Transform root = Root(container);
            Bounds bounds = RendererBounds(root.gameObject, root.position);
            float yaw = root.eulerAngles.y + SignsSettings.Rotation.Value + rule.Rotation;
            Vector3 bottom = new Vector3(bounds.center.x, bounds.max.y + SignsSettings.Height.Value, bounds.center.z) + root.rotation * rule.Offset;
            return new SignPose(bottom, Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>The pivot position that puts the sign's bottom on the pose, as far as the pivot offset is known.</summary>
        public static Vector3 PivotFor(SignPose pose) => pose.Bottom - Vector3.up * (pivotToBottom ?? 0f);

        /// <summary>Moves a freshly created sign so its renderer bottom sits on the pose; remembers the pivot offset.</summary>
        public static void AlignBottom(GameObject sign, ZDO zdo, SignPose pose)
        {
            Bounds bounds = RendererBounds(sign, sign.transform.position);
            if (bounds.size == Vector3.zero)
                return;
            pivotToBottom = bounds.min.y - sign.transform.position.y;
            float delta = pose.Bottom.y - bounds.min.y;
            if (Mathf.Abs(delta) < 0.001f)
                return;
            Vector3 position = sign.transform.position + Vector3.up * delta;
            sign.transform.position = position;
            zdo.SetPosition(position);
        }

        /// <summary>True when a loaded sign already sits on the pose, within 2 cm and 1 degree.</summary>
        public static bool Matches(GameObject sign, SignPose pose)
        {
            Transform transform = sign.transform;
            Bounds bounds = RendererBounds(sign, transform.position);
            Vector3 bottom = new Vector3(transform.position.x, bounds.min.y, transform.position.z);
            if (Vector3.Distance(bottom, pose.Bottom) > Tolerance)
                return false;
            return Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, pose.Rotation.eulerAngles.y)) <= YawTolerance;
        }

        /// <summary>The container's net object (the same object for a chest).</summary>
        public static Transform Root(Container container)
        {
            return container.m_nview != null ? container.m_nview.transform : container.transform;
        }

        /// <summary>World bounds of the active mesh and skinned mesh renderers; a point at the fallback when there are none.</summary>
        public static Bounds RendererBounds(GameObject root, Vector3 fallback)
        {
            Bounds bounds = new Bounds(fallback, Vector3.zero);
            bool any = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer) || !renderer.enabled)
                    continue;
                if (any)
                    bounds.Encapsulate(renderer.bounds);
                else
                    bounds = renderer.bounds;
                any = true;
            }
            return bounds;
        }
    }
}
