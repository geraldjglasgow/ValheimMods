using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The selection box of the Blueprints tab: an invisible area behind the list's buttons, inside its viewport, active
    /// only while the tab is shown. Holding the left button on empty space and dragging draws a gold box (above the
    /// buttons) and picks every blueprint it touches, added to the earlier picks with Ctrl; a plain click on
    /// empty space clears the picks. The box is kept in the list's own space, so the wheel can scroll the list while it
    /// is drawn. Being a child of the list, the area takes the drag the list would otherwise scroll by; the wheel is not
    /// a drag and still reaches the list.
    /// </summary>
    public sealed class TabBox : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private static TabBox area;

        private RectTransform viewport;
        private RectTransform content;
        private RectTransform box;
        private Vector2 start;
        private List<Piece> kept;

        /// <summary>BuildUi.Awake: the area (first child of the viewport, behind the buttons) and the box (last child, above them).</summary>
        public static void Install(BuildUi ui)
        {
            ScrollRect list = ui.m_pieceScrollRect;
            if (list == null || list.viewport == null || list.content == null)
                return;
            RectTransform rect = TabLook.Child(list.viewport, "OpenKeep Box Area");
            rect.SetAsFirstSibling();
            Image catcher = rect.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            catcher.canvasRenderer.cullTransparentMesh = false;
            area = rect.gameObject.AddComponent<TabBox>();
            area.viewport = list.viewport;
            area.content = list.content;
            area.box = TabLook.Frame(list.viewport, "OpenKeep Box");
            area.box.anchorMin = area.box.anchorMax = area.box.pivot = new Vector2(0f, 0f);
            TabLook.Paint(area.box, TabLook.Box);
            area.box.gameObject.SetActive(false);
            rect.gameObject.SetActive(false);
        }

        /// <summary>Per frame: the area is there exactly while the tab is shown.</summary>
        public static void Tick()
        {
            if (area == null)
                return;
            bool shown = BlueprintTab.Showing;
            if (area.gameObject.activeSelf != shown)
                area.gameObject.SetActive(shown);
            if (!shown && area.box.gameObject.activeSelf)
                area.box.gameObject.SetActive(false);
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = true;

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || NamePrompt.Showing)
                return;
            data.eligibleForClick = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, data.pressPosition, data.pressEventCamera, out start);
            kept = BlueprintKeys.Ctrl ? TabPicks.Snapshot() : new List<Piece>();
            box.SetAsLastSibling();
            box.gameObject.SetActive(true);
            Draw(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (kept != null)
                Draw(data);
        }

        public void OnEndDrag(PointerEventData data)
        {
            kept = null;
            box.gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && !BlueprintKeys.Ctrl)
                TabPicks.Clear();
        }

        /// <summary>The box from where the press was (in the list, so scrolling keeps it) to the mouse, and its picks.</summary>
        private void Draw(PointerEventData data)
        {
            Vector2 from = viewport.InverseTransformPoint(content.TransformPoint(start));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, data.position, data.pressEventCamera, out Vector2 to))
                return;
            Rect rect = Rect.MinMaxRect(Mathf.Min(from.x, to.x), Mathf.Min(from.y, to.y), Mathf.Max(from.x, to.x), Mathf.Max(from.y, to.y));
            box.localPosition = rect.min;
            box.sizeDelta = rect.size;
            TabPicks.Box(kept, Touched(rect));
        }

        /// <summary>The blueprints whose buttons overlap the box (both in the viewport's space).</summary>
        private List<Piece> Touched(Rect rect)
        {
            List<Piece> touched = new List<Piece>();
            BuildUi menu = BlueprintTab.Menu;
            foreach (BuildUiPieceButton button in menu != null ? menu.m_pieceButtons : new List<BuildUiPieceButton>())
            {
                if (button != null && button.gameObject.activeInHierarchy && TabPicks.Pickable(button.Piece) && Bounds(button).Overlaps(rect))
                    touched.Add(button.Piece);
            }
            return touched;
        }

        private Rect Bounds(BuildUiPieceButton button)
        {
            RectTransform rect = (RectTransform)button.transform;
            Vector2 min = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.min));
            Vector2 max = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.max));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
