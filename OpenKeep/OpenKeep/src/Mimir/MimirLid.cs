using UnityEngine;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Opens and closes Mímir's Chest's lid on every client: open while the game's own in-use flag says someone has it
    /// open (the owner's container state, the ZDO's <c>InUse</c> elsewhere), so everybody nearby sees it. The lid swings
    /// on its hinge with an ease in and out. Nothing is sent; it reads only what the game already replicates. Switched
    /// off on a dedicated server (nothing to draw).
    /// </summary>
    public sealed class MimirLid : MonoBehaviour
    {
        /// <summary>The lid's turn about its local X axis when fully open, in degrees (the model's hinge, ValheimAssets Assets/Props/MimirsChest).</summary>
        public const float OpenAngle = -100f;

        private const float Seconds = 0.55f;

        private ZNetView view;
        private Container container;
        private Transform lid;
        private Quaternion closed;
        private float open;

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            container = GetComponent<Container>();
            lid = FindLid(transform);
            if (lid == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                enabled = false;
                return;
            }
            closed = lid.localRotation;
        }

        private void Update()
        {
            if (view == null || !view.IsValid())
                return;
            float target = IsOpen() ? 1f : 0f;
            if (Mathf.Approximately(open, target))
                return;
            open = Mathf.MoveTowards(open, target, Time.deltaTime / Seconds);
            float eased = Mathf.SmoothStep(0f, 1f, open);
            lid.localRotation = closed * Quaternion.Euler(OpenAngle * eased, 0f, 0f);
        }

        private bool IsOpen() =>
            container != null && (container.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse) == 1);

        private static Transform FindLid(Transform root)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
            {
                if (part.name == "lid")
                    return part;
            }
            return null;
        }
    }
}
