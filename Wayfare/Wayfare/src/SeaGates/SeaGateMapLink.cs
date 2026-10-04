using UnityEngine;
using UnityEngine.UI;

namespace Wayfare.SeaGates
{
    /// <summary>The picker's line from its gate to the gate's current destination on the large map: one thin image under
    /// the pin root, stretched and turned between the two icons, below every icon.</summary>
    internal static class SeaGateMapLink
    {
        private const float Thickness = 3f;
        private static readonly Color LinkColor = new Color(1f, 0.82f, 0.3f, 0.7f);

        private static RectTransform line;

        internal static void Show(Transform parent, Vector3 fromWorld, Vector3 toWorld)
        {
            RectTransform rect = Get(parent);
            Vector3 a = parent.InverseTransformPoint(fromWorld);
            Vector3 b = parent.InverseTransformPoint(toWorld);
            Vector2 d = new Vector2(b.x - a.x, b.y - a.y);
            rect.localPosition = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
            rect.sizeDelta = new Vector2(d.magnitude, Thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            if (!rect.gameObject.activeSelf)
                rect.gameObject.SetActive(true);
        }

        internal static void Hide()
        {
            if (line != null && line.gameObject.activeSelf)
                line.gameObject.SetActive(false);
        }

        private static RectTransform Get(Transform parent)
        {
            if (line != null && line.parent == parent)
                return line;
            if (line != null)
                Object.Destroy(line.gameObject);
            GameObject go = new GameObject("Wayfare.SeaGateLink", typeof(RectTransform), typeof(Image));
            line = (RectTransform)go.transform;
            line.SetParent(parent, worldPositionStays: false);
            line.SetAsFirstSibling();
            line.anchorMin = line.anchorMax = line.pivot = new Vector2(0.5f, 0.5f);
            Image image = go.GetComponent<Image>();
            image.color = LinkColor;
            image.raycastTarget = false;
            return line;
        }
    }
}
