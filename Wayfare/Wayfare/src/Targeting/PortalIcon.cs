using UnityEngine;
using UnityEngine.UI;
using Wayfare.Core;

namespace Wayfare.Targeting
{
    /// <summary>One portal's icon on the map layer: the game's portal icon in gold (<see cref="IconFactory"/>), a ring
    /// around it for a favourite, the tag below, a small red "You are here" above the portal the player stands at, and
    /// an invisible click area (<see cref="Zone"/>) as wide as the icon at the top of its pulse, which holds still while
    /// the icon grows and shrinks, so a click near the icon counts (as OpenKeep's bed icons). Nothing here takes the
    /// pointer: clicks still reach the map, and <see cref="MapOverlay.TryHitTest"/> measures them against the area.</summary>
    public sealed class PortalIcon
    {
        private const float RingOverhang = 0.2f;

        public RectTransform Root;
        public Image Image;
        public Image Ring;
        public RectTransform Zone;
        public Text Label;
        public Text Here;

        public static PortalIcon Make(RectTransform layer)
        {
            GameObject go = new GameObject("Wayfare.PortalIcon", typeof(RectTransform), typeof(Image));
            RectTransform root = (RectTransform)go.transform;
            root.SetParent(layer, worldPositionStays: false);
            root.anchorMin = root.anchorMax = Vector2.zero; // the map's lower-left corner, where pin positions start
            root.pivot = new Vector2(0.5f, 0.5f);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return new PortalIcon
            {
                Root = root, Image = image, Ring = MakeRing(root), Zone = MakeZone(root),
                Label = MakeText(root, below: true), Here = MakeHere(root),
            };
        }

        private static Image MakeRing(RectTransform parent)
        {
            GameObject go = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = new Vector2(-RingOverhang, -RingOverhang);
            rect.anchorMax = new Vector2(1f + RingOverhang, 1f + RingOverhang);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image ring = go.GetComponent<Image>();
            ring.sprite = IconFactory.Favourite;
            ring.raycastTarget = false;
            go.SetActive(false);
            return ring;
        }

        private static RectTransform MakeZone(RectTransform parent)
        {
            GameObject go = new GameObject("ClickArea", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Text MakeHere(RectTransform parent)
        {
            Text here = MakeText(parent, below: false);
            here.text = Localization.instance != null ? Localization.instance.Localize(Words.YouAreHere) : "You are here";
            here.fontSize = 11;
            here.fontStyle = FontStyle.Bold;
            here.color = IconFactory.HereRed;
            here.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            here.gameObject.SetActive(false);
            return here;
        }

        /// <summary>A line of text centred under the icon (the tag) or over it ("You are here").</summary>
        private static Text MakeText(RectTransform parent, bool below)
        {
            GameObject go = new GameObject(below ? "Label" : "Here", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, below ? 0f : 1f);
            rect.pivot = new Vector2(0.5f, below ? 1f : 0f);
            rect.anchoredPosition = new Vector2(0f, below ? -2f : 2f);
            rect.sizeDelta = new Vector2(160f, 20f);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.alignment = below ? TextAnchor.UpperCenter : TextAnchor.LowerCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
