using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>The left-side list of favourited portals shown while a targeting session is open; clicking an
    /// entry targets that portal exactly like clicking its map icon. Rebuilt from
    /// <see cref="PlayerFavourites"/>/<see cref="PortalRegistry"/> every tick rather than kept as separate state,
    /// so it never drifts from what is actually targetable right now.</summary>
    public static class FavouritesPanel
    {
        private const float RowHeight = 26f;
        private const float PanelHeight = 400f;

        private static RectTransform root;
        private static readonly List<GameObject> rows = new List<GameObject>();
        private static float scrollOffset;

        public static void Tick()
        {
            if (!TargetingSession.Active || Minimap.instance == null || Minimap.instance.m_largeRoot == null)
            {
                Clear();
                return;
            }
            EnsureRoot();
            PollScroll();
            Rebuild();
        }

        /// <summary>A list long enough to need it scrolls with the mouse wheel while the pointer is over it - the
        /// same "wheel while hovering" shape OpenKeep's own chest cycling already uses, rather than a full
        /// scrollbar for what is usually a handful of entries.</summary>
        private static void PollScroll()
        {
            float delta = Input.mouseScrollDelta.y;
            if (delta == 0f || !RectTransformUtility.RectangleContainsScreenPoint(root, ZInput.pointerPosition, null))
                return;
            scrollOffset = Mathf.Max(0f, scrollOffset - delta * RowHeight);
        }

        public static void Clear()
        {
            foreach (GameObject row in rows)
            {
                if (row != null)
                    Object.Destroy(row);
            }
            rows.Clear();
            scrollOffset = 0f;
            if (root != null)
                root.gameObject.SetActive(false);
        }

        private static void EnsureRoot()
        {
            if (root != null && root.gameObject != null)
            {
                root.gameObject.SetActive(true);
                return;
            }
            GameObject go = new GameObject("Wayfare.Favourites", typeof(RectTransform), typeof(RectMask2D));
            root = (RectTransform)go.transform;
            root.SetParent(Minimap.instance.m_largeRoot.transform, worldPositionStays: false);
            root.anchorMin = root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0f, 0.5f);
            root.anchoredPosition = new Vector2(20f, 0f);
            root.sizeDelta = new Vector2(180f, PanelHeight);
        }

        private static void Rebuild()
        {
            foreach (GameObject row in rows)
            {
                if (row != null)
                    Object.Destroy(row);
            }
            rows.Clear();
            List<PortalInfo> favourites = CollectFavourites();
            float maxScroll = Mathf.Max(0f, favourites.Count * RowHeight - PanelHeight);
            scrollOffset = Mathf.Clamp(scrollOffset, 0f, maxScroll);
            for (int i = 0; i < favourites.Count; i++)
                rows.Add(BuildRow(favourites[i], -i * RowHeight - scrollOffset));
        }

        private static List<PortalInfo> CollectFavourites()
        {
            Dictionary<ZDOID, PortalInfo> byId = Index();
            List<PortalInfo> favourites = new List<PortalInfo>();
            foreach (ZDOID id in PlayerFavourites.All())
            {
                if (byId.TryGetValue(id, out PortalInfo info))
                    favourites.Add(info);
            }
            return favourites;
        }

        private static Dictionary<ZDOID, PortalInfo> Index()
        {
            Dictionary<ZDOID, PortalInfo> byId = new Dictionary<ZDOID, PortalInfo>();
            foreach (PortalInfo info in PortalRegistry.Portals)
                byId[info.Id] = info;
            return byId;
        }

        private static GameObject BuildRow(PortalInfo info, float y)
        {
            GameObject go = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(root, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(180f, RowHeight - 2f);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            AddLabel(rect, string.IsNullOrEmpty(info.Tag) ? "(unnamed portal)" : info.Tag);
            ZDOID target = info.Id;
            go.GetComponent<Button>().onClick.AddListener(() => TargetingSession.Select(target));
            return go;
        }

        private static void AddLabel(RectTransform parent, string text)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 0f);
            rect.offsetMax = new Vector2(-6f, 0f);
            Text label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
        }
    }
}
