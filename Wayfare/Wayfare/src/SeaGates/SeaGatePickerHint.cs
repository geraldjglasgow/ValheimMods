using UnityEngine;
using UnityEngine.UI;

namespace Wayfare.SeaGates
{
    /// <summary>The picker's one line of help (the helmsman's or the crew's, from <see cref="SeaGatePicker"/>) in the
    /// upper left corner of the large map while the picker is open: a child of the map image, drawn after the map's own children, outlined so it reads
    /// on any terrain. Built once per map and only switched on and off after that.</summary>
    internal static class SeaGatePickerHint
    {
        private const float Inset = 18f;
        private const int FontSize = 18;

        private static Text label;

        internal static void Show(string line)
        {
            Minimap map = Minimap.instance;
            if (map == null || map.m_mapImageLarge == null)
                return;
            Text text = Get(map.m_mapImageLarge.transform);
            if (text.text != line)
                text.text = line;
            if (!text.gameObject.activeSelf)
                text.gameObject.SetActive(true);
        }

        internal static void Hide()
        {
            if (label != null && label.gameObject.activeSelf)
                label.gameObject.SetActive(false);
        }

        private static Text Get(Transform parent)
        {
            if (label != null && label.transform.parent == parent)
                return label;
            RectTransform rect = (RectTransform)new GameObject("Wayfare.SeaGateHint", typeof(RectTransform)).transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(Inset, -Inset);
            rect.sizeDelta = new Vector2(640f, 30f);
            label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = FontSize;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            rect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            return label;
        }
    }
}
