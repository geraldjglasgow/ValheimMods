using UnityEngine;
using UnityEngine.UI;
using Wayfare.Core;
using Wayfare.Portals;

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

        private static readonly LocalWord youAreHere = new LocalWord(Words.YouAreHere);

        /// <summary>"You are here" in the player's language, localized once per language: an icon is kept and reused.</summary>
        public static string YouAreHere => youAreHere.Text;

        public RectTransform Root;
        public Image Image;
        public Image Ring;
        public RectTransform Zone;
        public Text Label;
        public Text Here;

        // What the icon last showed, so a redraw every frame writes only what changed: each write is a call into the
        // engine and can mark the canvas for a rebuild. The ring, the "here" line and the label start as Make leaves them.
        private Sprite shownSprite;
        private bool ringShown;
        private bool hereShown;
        private bool labelShown = true;

        // Whether the portal is a favourite, and what that was looked up for: the portal, the favourites' and the
        // portal list's versions (an icon is handed to another portal from the spare pool).
        private ZDOID favouriteFor = ZDOID.None;
        private int favouriteVersion = -1;
        private int favouriteListVersion = -1;
        private bool favourite;

        /// <summary>Draws the icon's look for a portal: the gold portal sprite, the favourite ring, "You are here" and
        /// the tag (null hides it); only what changed since the last frame is written.</summary>
        public void Show(ZDOID id, bool here, string tag)
        {
            Sprite sprite = IconFactory.Portal;
            if (!ReferenceEquals(sprite, shownSprite))
            {
                shownSprite = sprite;
                Image.sprite = sprite;
                Image.color = IconFactory.Gold;
            }
            SetShown(Ring.gameObject, IsFavourite(id), ref ringShown);
            SetShown(Here.gameObject, here, ref hereShown);
            if (here)
                Here.text = YouAreHere;
            SetShown(Label.gameObject, tag != null, ref labelShown);
            if (tag != null)
                Label.text = tag;
        }

        private bool IsFavourite(ZDOID id)
        {
            int version = PlayerFavourites.Version;
            if (id == favouriteFor && version == favouriteVersion && PortalRegistry.Version == favouriteListVersion)
                return favourite;
            favouriteFor = id;
            favouriteVersion = version;
            favouriteListVersion = PortalRegistry.Version;
            favourite = PlayerFavourites.IsFavourite(id);
            return favourite;
        }

        private static void SetShown(GameObject go, bool on, ref bool shown)
        {
            if (on == shown)
                return;
            shown = on;
            go.SetActive(on);
        }

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
