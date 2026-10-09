using System.Collections.Generic;
using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// Dragging lines in the bench window: a press that moves past the event system's drag threshold carries the picks of
    /// one list (the line pressed when it was not picked), a see-through icon with their count follows the mouse, a folder
    /// line that would take them is framed green, and letting go on a folder line or a list's empty space hands them to
    /// that list (<see cref="IBenchPane.Drop"/>): a move within one side, a share or a copy across. Anywhere else nothing
    /// happens. Only this machine; the window's lists do the rest.
    /// </summary>
    public static class BenchDrag
    {
        private const float GhostSize = 40f;

        private static BenchPaneView from;
        private static List<BenchEntry> dragged;
        private static RectTransform ghost;
        private static BenchRowView marked;

        public static bool Active => from != null;

        public static void Begin(BenchPaneView pane, List<BenchEntry> entries, Sprite icon, PointerEventData data)
        {
            End();
            if (entries.Count == 0 || BenchUi.Window == null)
                return;
            from = pane;
            dragged = entries;
            ghost = Ghost(icon, entries.Count);
            Move(data);
        }

        private static RectTransform Ghost(Sprite icon, int count)
        {
            RectTransform root = BenchCopies.Rect("OpenKeep_BenchDrag", BenchUi.Window);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(GhostSize, GhostSize);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.75f;
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            TMP_Text number = BenchCopies.Text(BenchUi.Title, root, "Count", 18f, TextAlignmentOptions.BottomRight);
            BenchCopies.Fill(number.rectTransform, -6f);
            number.text = count > 1 ? count.ToString() : "";
            root.SetAsLastSibling();
            return root;
        }

        public static void Move(PointerEventData data)
        {
            if (ghost != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(BenchUi.Window, data.position, null, out Vector2 local))
                ghost.anchoredPosition = local + new Vector2(14f, -14f);
        }

        public static void Hover(BenchPaneView pane, BenchRowView row)
        {
            Unmark();
            if (!Active || row.Entry == null || !pane.Model.Accepts(from.Model, row.Entry))
                return;
            marked = row;
            row.DropMark(true);
        }

        public static void Leave(BenchRowView row)
        {
            if (marked == row)
                Unmark();
        }

        private static void Unmark()
        {
            if (marked != null)
                marked.DropMark(false);
            marked = null;
        }

        /// <summary>Letting go over a list's line (<paramref name="onto"/>) or its empty space (null).</summary>
        public static void DropOn(BenchPaneView pane, BenchEntry onto)
        {
            if (!Active || pane == null)
                return;
            IBenchPane source = from.Model;
            List<BenchEntry> entries = dragged;
            End();
            if (pane.Model.Accepts(source, onto))
                BlueprintSafe.Run("OpenKeep blueprint bench drop", () => pane.Model.Drop(source, entries, onto));
            else if (source is LibraryPane && pane.Model is PoolPane)
                Messages.TopLeft(Language.Localize(BenchWords.OnlyOwn));
        }

        public static void End()
        {
            Unmark();
            if (ghost != null)
                Object.Destroy(ghost.gameObject);
            ghost = null;
            from = null;
            dragged = null;
        }
    }
}
