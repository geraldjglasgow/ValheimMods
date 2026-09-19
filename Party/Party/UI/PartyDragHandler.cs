using UnityEngine;
using UnityEngine.EventSystems;

namespace Party.UI
{
    /// <summary>
    /// Drags the panel's RectTransform via Unity's own event system, only while <see cref="HealthPanel.CanDrag"/>.
    /// The panel reads <see cref="Dragging"/> so its per-frame layout pass leaves the position alone mid-drag;
    /// the final spot is persisted once, on release.
    /// </summary>
    public class PartyDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static bool Dragging { get; private set; }

        private RectTransform target;
        private Canvas canvas;

        private void Awake()
        {
            target = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData) => Dragging = HealthPanel.CanDrag();

        public void OnDrag(PointerEventData eventData)
        {
            if (Dragging)
                target.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Dragging)
                HealthPanel.PersistPosition(target.anchoredPosition);
            Dragging = false;
        }

        private void OnDisable() => Dragging = false;
    }
}
