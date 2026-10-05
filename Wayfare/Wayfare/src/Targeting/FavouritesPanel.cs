using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>The left-side list of favourited portals shown while a targeting session is open; clicking an
    /// entry targets that portal exactly like clicking its map icon (<see cref="TargetingSession.Select"/>). The rows
    /// are made once when the panel opens and kept until the map closes: a row is a button, and a button destroyed
    /// and remade every frame never sees both the press and the release of a click. They are refilled in place only
    /// when what they show changes - the portal snapshot (<see cref="PortalRegistry.Version"/>: the server's list
    /// arriving, a rename, a mode change that hides a portal), the favourite set (<see cref="PlayerFavourites.Version"/>),
    /// or the player and their admin rights (the map icons' access rule). Scrolling slides the rows, never remakes them.</summary>
    public static class FavouritesPanel
    {
        private const float RowHeight = 26f;
        private const float PanelWidth = 180f;
        private const float PanelHeight = 400f;

        private sealed class Row
        {
            public GameObject Root;
            public Text Label;
            public ZDOID Target;
        }

        private static RectTransform root;
        private static RectTransform content;
        private static readonly List<Row> rows = new List<Row>();
        private static float scrollOffset;

        // What the rows were last filled from; -1 means not yet, so the next tick fills them.
        private static int shownPortals = -1;
        private static int shownFavourites = -1;
        private static long shownPlayer;
        private static bool shownAdmin;

        public static void Tick()
        {
            if (!TargetingSession.Active || Minimap.instance == null || Minimap.instance.m_largeRoot == null || Player.m_localPlayer == null)
            {
                Clear();
                return;
            }
            EnsureRoot();
            RefreshIfChanged();
            PollScroll();
        }

        /// <summary>Removes the rows and hides the panel; the next open fills it afresh. Called every frame the panel
        /// is not showing, so it returns at once when there is nothing to remove.</summary>
        public static void Clear()
        {
            if (rows.Count == 0 && (root == null || !root.gameObject.activeSelf))
                return;
            DestroyRows(0);
            scrollOffset = 0f;
            shownPortals = -1;
            if (root != null)
                root.gameObject.SetActive(false);
        }

        /// <summary>A list long enough to need it scrolls with the mouse wheel while the pointer is over it - the
        /// same "wheel while hovering" shape OpenKeep's own chest cycling already uses, rather than a full
        /// scrollbar for what is usually a handful of entries.</summary>
        private static void PollScroll()
        {
            float delta = Input.mouseScrollDelta.y;
            if (delta != 0f && RectTransformUtility.RectangleContainsScreenPoint(root, ZInput.pointerPosition, null))
                ScrollBy(-delta * RowHeight);
        }

        /// <summary>Wheel down slides the rows up to bring the later ones into view, never past the first or the last.</summary>
        private static void ScrollBy(float amount)
        {
            float maxScroll = Mathf.Max(0f, rows.Count * RowHeight - PanelHeight);
            scrollOffset = Mathf.Clamp(scrollOffset + amount, 0f, maxScroll);
            content.anchoredPosition = new Vector2(0f, scrollOffset);
        }

        private static void EnsureRoot()
        {
            if (root != null)
            {
                if (!root.gameObject.activeSelf)
                    root.gameObject.SetActive(true);
                return;
            }
            rows.Clear(); // a new map (another world loaded) took the old panel and its rows with it
            shownPortals = -1;
            BuildPanel();
        }

        /// <summary>The panel clips the rows to its box; the rows sit in a strip pinned to its top that scrolling slides up.</summary>
        private static void BuildPanel()
        {
            root = NewRect("Wayfare.Favourites", Minimap.instance.m_largeRoot.transform);
            root.gameObject.AddComponent<RectMask2D>();
            root.anchorMin = root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0f, 0.5f);
            root.anchoredPosition = new Vector2(20f, 0f);
            root.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            content = NewRect("Rows", root);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = Vector2.zero;
        }

        /// <summary>Refills the rows when one of the inputs they are drawn from has changed since the last fill, and
        /// does nothing otherwise - a compare of four numbers per frame.</summary>
        private static void RefreshIfChanged()
        {
            long playerId = Player.m_localPlayer.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            int portals = PortalRegistry.Version;
            int favourites = PlayerFavourites.Version;
            if (portals == shownPortals && favourites == shownFavourites && playerId == shownPlayer && isAdmin == shownAdmin)
                return;
            shownPortals = portals;
            shownFavourites = favourites;
            shownPlayer = playerId;
            shownAdmin = isAdmin;
            Show(Collect(playerId, isAdmin));
        }

        /// <summary>The favourites this player may target now, in snapshot order: the map icons' rules, so a favourite
        /// whose portal went private or admin-only leaves the list until it is open to them again.</summary>
        private static List<PortalInfo> Collect(long playerId, bool isAdmin)
        {
            List<PortalInfo> favourites = new List<PortalInfo>();
            foreach (PortalInfo info in PortalRegistry.Portals)
            {
                if (PlayerFavourites.IsFavourite(info.Id) && PortalAccess.MayTarget(info.Mode, info.Owner, playerId, isAdmin))
                    favourites.Add(info);
            }
            return favourites;
        }

        /// <summary>Fills the rows in place: kept rows take the new names and targets, missing ones are made, surplus
        /// ones removed. A kept row keeps its button, so a refill never swallows a click.</summary>
        private static void Show(List<PortalInfo> favourites)
        {
            for (int i = 0; i < favourites.Count; i++)
            {
                if (i == rows.Count)
                    rows.Add(BuildRow(i));
                rows[i].Target = favourites[i].Id;
                rows[i].Label.text = string.IsNullOrEmpty(favourites[i].Tag) ? "(unnamed portal)" : favourites[i].Tag;
            }
            DestroyRows(favourites.Count);
            ScrollBy(0f);
        }

        private static void DestroyRows(int keep)
        {
            for (int i = rows.Count - 1; i >= keep; i--)
            {
                if (rows[i].Root != null)
                    Object.Destroy(rows[i].Root);
                rows.RemoveAt(i);
            }
        }

        /// <summary>The click reads the row's target when it happens, so a refill that changes the target needs no new listener.</summary>
        private static Row BuildRow(int index)
        {
            RectTransform rect = NewRect("Row", content);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * RowHeight);
            rect.sizeDelta = new Vector2(PanelWidth, RowHeight - 2f);
            Image image = rect.gameObject.AddComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            Tint(button, image);
            Row row = new Row { Root = rect.gameObject, Label = AddLabel(rect) };
            button.onClick.AddListener(() => TargetingSession.Select(row.Target));
            return row;
        }

        /// <summary>The row's colour is the button's tint over a white image, so the row under the pointer, or the one
        /// just clicked, shows in the favourite ring's gold, dimmed - the button does it, nothing here runs per frame.</summary>
        private static void Tint(Button button, Image image)
        {
            image.color = Color.white;
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0f, 0f, 0f, 0.55f);
            colors.highlightedColor = new Color(0.32f, 0.26f, 0.1f, 0.8f);
            colors.pressedColor = new Color(0.5f, 0.4f, 0.15f, 0.9f);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private static Text AddLabel(RectTransform parent)
        {
            RectTransform rect = NewRect("Label", parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 0f);
            rect.offsetMax = new Vector2(-6f, 0f);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, worldPositionStays: false);
            return rect;
        }
    }
}
