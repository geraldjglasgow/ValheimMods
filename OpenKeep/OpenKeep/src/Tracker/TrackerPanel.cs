using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// The tracker on the HUD: a column under the HUD root (so it hides with the HUD), top left anchored, on a canvas
    /// of its own sorted just above the inventory screen, so it stays on top of the open inventory and its buttons get
    /// the pointer before the inventory's full-screen drop area. Its background is the crafting panel's dark item
    /// background at the chosen opacity. Only the title (the drag handle, built once so a rebuild never ends a drag),
    /// the headers and their buttons catch the pointer, and only while the cursor is free, so clicks elsewhere pass
    /// through to the inventory below. The entries sit in a column of their own under the title.
    /// </summary>
    public sealed class TrackerPanel
    {
        private const int InventoryOrder = 600;
        private const int AboveInventory = 50;
        private static readonly Vector2 DefaultPosition = new Vector2(20f, 330f);

        private readonly RectTransform root;
        private readonly CanvasGroup group;
        private readonly Image background;
        private readonly TMP_Text title;
        private readonly RectTransform list;
        private readonly List<TrackerEntry> entries = new List<TrackerEntry>();

        private TrackerPanel(RectTransform root)
        {
            this.root = root;
            group = root.gameObject.AddComponent<CanvasGroup>();
            background = root.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            TrackerUi.Column(root.gameObject, 4f).padding = new RectOffset(8, 8, 6, 8);
            ContentSizeFitter fit = root.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            title = Title(root);
            list = TrackerUi.Node("OpenKeep_TrackerList", root);
            TrackerUi.Column(list.gameObject, 6f);
        }

        public static TrackerPanel Create(Transform hudRoot)
        {
            RectTransform rect = TrackerUi.Node("OpenKeep_Tracker", hudRoot);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            OwnCanvas(rect.gameObject, hudRoot);
            return new TrackerPanel(rect);
        }

        /// <summary>
        /// Sorted a little above the inventory screen's canvas (600 in the game; the HUD's is 400), below the store,
        /// chat, centre messages, menu and popups (700 and up).
        /// </summary>
        private static void OwnCanvas(GameObject go, Transform hudRoot)
        {
            Canvas hud = hudRoot.GetComponentInParent<Canvas>();
            Canvas inventory = InventoryGui.instance != null ? InventoryGui.instance.GetComponent<Canvas>() : null;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            if (hud != null)
                canvas.sortingLayerID = hud.sortingLayerID;
            canvas.sortingOrder = (inventory != null ? inventory.sortingOrder : InventoryOrder) + AboveInventory;
            go.AddComponent<GraphicRaycaster>();
        }

        public void SetVisible(bool visible)
        {
            if (root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }

        /// <summary>The pointer is caught only while the cursor is free (inventory, map or menu open).</summary>
        public void Interactive(bool free)
        {
            if (group.blocksRaycasts != free)
                group.blocksRaycasts = free;
        }

        /// <summary>The look and the entries again from the settings and the tracked list; the place stays.</summary>
        public void Rebuild()
        {
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                GameObject old = list.GetChild(i).gameObject;
                old.SetActive(false);   // out of this frame's layout; destroyed at its end
                Object.Destroy(old);
            }
            entries.Clear();
            Style();
            List<TrackedRecipe> tracked = TrackerList.Entries;
            for (int i = 0; i < tracked.Count; i++)
            {
                Recipe recipe = tracked[i].Recipe;
                if (recipe != null)
                    entries.Add(TrackerEntry.Build(list, tracked[i], recipe, i));
            }
        }

        public void Count(Player player)
        {
            foreach (TrackerEntry entry in entries)
                entry.Count(player);
        }

        private void Style()
        {
            float scale = TrackerSettings.Scale.Value;
            root.localScale = new Vector3(scale, scale, 1f);
            root.sizeDelta = new Vector2(TrackerStyle.Width, root.sizeDelta.y);
            Image source = GameBackground();
            if (source != null)
            {
                background.sprite = source.sprite;
                background.type = source.type;
            }
            background.color = new Color(0f, 0f, 0f, TrackerSettings.BackgroundOpacity.Value);
            title.font = TrackerStyle.Font;
            title.fontSize = TrackerStyle.Size * 0.8f;
            title.text = Core.Language.Localize(TrackerWords.Title);
        }

        /// <summary>The title row is the drag handle: plain text that catches the pointer.</summary>
        private static TMP_Text Title(RectTransform root)
        {
            TMP_Text text = TrackerUi.Text(root, "OpenKeep_TrackerTitle", TrackerStyle.Size * 0.8f, TextAlignmentOptions.MidlineLeft);
            text.color = TrackerStyle.Dim;
            text.raycastTarget = true;
            text.gameObject.AddComponent<TrackerDrag>().Target = root;
            return text;
        }

        /// <summary>The saved place (else the default one at the left edge), kept on screen once laid out.</summary>
        public void Place()
        {
            Vector2 down = TrackerDrag.Saved(out Vector2 saved) ? saved : DefaultPosition;
            root.anchoredPosition = new Vector2(down.x, -down.y);
            KeepOnScreen();
        }

        /// <summary>Laid out now and pulled back onto the screen if it reaches past an edge (a smaller screen, more entries).</summary>
        public void KeepOnScreen()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            TrackerDrag.Clamp(root);
        }

        /// <summary>The crafting panel's dark item background (the description panel's image), when the inventory is up.</summary>
        private static Image GameBackground()
        {
            InventoryGui gui = InventoryGui.instance;
            Transform description = gui != null && gui.m_recipeDecription != null ? gui.m_recipeDecription.transform.parent : null;
            return description != null ? description.GetComponent<Image>() : null;
        }
    }
}
