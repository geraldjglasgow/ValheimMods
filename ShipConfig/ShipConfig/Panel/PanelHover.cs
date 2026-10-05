using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShipConfig
{
    /// <summary>
    /// The panel's tooltips. The panel draws in the HUD's own layer, under the inventory, crafting and every other
    /// window, so a window that overlaps it covers it. The inventory screen also lays a full-screen drop area over
    /// everything, which takes the pointer, so the lines would never see it while the inventory is open. So the hover
    /// is tested here: while the cursor is free, the topmost UI element under the pointer, passing over anything as big
    /// as the screen (a backdrop, not a window), must be one of the panel's lines. That line's game tooltip then starts
    /// as if the pointer had entered it, and its box is lifted above the windows; anything else hides it.
    /// </summary>
    public sealed class PanelHover
    {
        private const int AboveWindows = 650;

        private static readonly List<RaycastResult> hits = new List<RaycastResult>();

        private readonly RectTransform root;
        private PointerEventData pointer;
        private GameObject hovered;

        public PanelHover(RectTransform root) => this.root = root;

        public void Update(bool cursorFree)
        {
            GameObject line = cursorFree ? LineUnderPointer() : null;
            if (line != hovered)
                Enter(line);
            Lift();
        }

        /// <summary>The panel hid: the next hover starts over.</summary>
        public void Reset() => hovered = null;

        private void Enter(GameObject line)
        {
            hovered = line;
            UITooltip tip = line != null ? line.GetComponent<UITooltip>() : null;
            if (tip != null)
                tip.OnHoverStart(line);
            else if (Ours())
                UITooltip.HideTooltip();
        }

        private GameObject LineUnderPointer()
        {
            EventSystem events = EventSystem.current;
            Vector2 at = ZInput.pointerPosition;
            if (events == null || !RectTransformUtility.RectangleContainsScreenPoint(root, at))
                return null;
            pointer ??= new PointerEventData(events);
            pointer.position = at;
            hits.Clear();
            events.RaycastAll(pointer, hits);
            foreach (RaycastResult hit in hits)
            {
                if (!Backdrop(hit.gameObject))
                    return Line(hit.gameObject);
            }
            return null;
        }

        /// <summary>The panel's line the hit is (or is inside), or null when it is anything else.</summary>
        private GameObject Line(GameObject hit)
        {
            Transform node = hit.transform;
            while (node != null && node.parent != root)
                node = node.parent;
            return node != null ? node.gameObject : null;
        }

        /// <summary>As big as the screen: the inventory's drop area or a dimmed backdrop, which covers nothing.</summary>
        private static bool Backdrop(GameObject hit)
        {
            RectTransform rect = hit.transform as RectTransform;
            if (rect == null)
                return false;
            Vector3 scale = rect.lossyScale;
            return rect.rect.width * scale.x >= Screen.width - 1f && rect.rect.height * scale.y >= Screen.height - 1f;
        }

        /// <summary>The game makes the tooltip box under the panel's canvas, in the HUD's layer; it gets a canvas above the windows.</summary>
        private void Lift()
        {
            GameObject box = UITooltip.m_tooltip;
            if (box == null || !Ours() || box.GetComponent<Canvas>() != null)
                return;
            Canvas canvas = box.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = root.GetComponent<Canvas>().sortingLayerID;
            canvas.sortingOrder = AboveWindows;
        }

        private bool Ours() => UITooltip.m_current != null && UITooltip.m_current.transform.IsChildOf(root);
    }
}
