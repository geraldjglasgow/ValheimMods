using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hung shield drawn as the game's wall item stand draws one (<c>ItemStand.SetVisualItem</c>): the item's own model
    /// (its prefab's "attach" object, or "attachobj" in it) placed on the hook as the item stand's attach point sits on the
    /// stand, the stand's back to the railing and its front (+z) out of the hull, scaled by the item's quality and moved by
    /// the item's own vertical item stand offset, its style set; its colliders taken off, so only the hook is hit. The
    /// item stand's attach point is read from the game's <c>itemstand</c> once. Only drawn, so only where there is a screen.
    /// </summary>
    public static class KwShieldShow
    {
        private static (Vector3 At, Quaternion Turn, Vector3 Scale)? hook;

        /// <summary>The item's model on a hook (the hook's +z out of the hull); null when it has none to show.</summary>
        public static GameObject? Hang(Transform slot, ItemDrop.ItemData item)
        {
            GameObject? attach = item.m_dropPrefab != null ? ItemStand.GetAttachPrefab(item.m_dropPrefab) : null;
            if (attach == null)
                return null;
            (Vector3 at, Quaternion turn, Vector3 scale) = Hook();
            Transform point = new GameObject("shield") { layer = slot.gameObject.layer }.transform;
            point.SetParent(slot, false);
            point.SetLocalPositionAndRotation(at, turn);
            point.localScale = scale;
            GameObject shown = Object.Instantiate(ItemStand.GetAttachGameObject(attach), point, false);
            shown.transform.SetLocalPositionAndRotation(attach.transform.localPosition, attach.transform.localRotation);
            shown.transform.localScale = Vector3.Scale(attach.transform.localScale, item.GetScale(item.m_quality));
            Offset(shown.transform, item);
            foreach (Collider collider in shown.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
            shown.GetComponentInChildren<IEquipmentVisual>()?.Setup(item.m_variant);
            return point.gameObject;
        }

        // The item's own offset for a wall stand, as the item stand takes it (its first one for a vertical stand).
        private static void Offset(Transform shown, ItemDrop.ItemData item)
        {
            foreach (ItemStand.OrientationSettings offset in item.m_shared.m_itemStandOffsets)
            {
                if ((offset.m_orientations & ItemStand.Orientation.Vertical) == ItemStand.Orientation.None)
                    continue;
                shown.localPosition += offset.m_position;
                shown.localRotation *= Quaternion.Euler(offset.m_rotation);
                shown.localScale = Vector3.Scale(shown.localScale, offset.m_scale);
                return;
            }
        }

        // The wall item stand's attach point in its own frame (place, turn and scale); a little in front of its middle
        // without the game's stand.
        private static (Vector3 At, Quaternion Turn, Vector3 Scale) Hook()
        {
            if (hook != null)
                return hook.Value;
            ItemStand? stand = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("itemstand")?.GetComponent<ItemStand>() : null;
            if (stand == null || stand.m_attachOther == null)
                return (new Vector3(0f, 0f, 0.05f), Quaternion.identity, Vector3.one);
            Transform root = stand.transform, point = stand.m_attachOther;
            Vector3 scale = Vector3.Scale(point.lossyScale, new Vector3(1f / root.lossyScale.x, 1f / root.lossyScale.y, 1f / root.lossyScale.z));
            hook = (root.InverseTransformPoint(point.position), Quaternion.Inverse(root.rotation) * point.rotation, scale);
            return hook.Value;
        }
    }
}
