using UnityEngine;
using UnityEngine.EventSystems;

namespace Party.UI
{
    /// <summary>Drags the panel's RectTransform via Unity's own event system. Only takes effect while <see cref="HealthPanel.CanDrag"/>.</summary>
    public class PartyDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        private RectTransform target;

        private void Awake() => target = GetComponent<RectTransform>();

        public void OnDrag(PointerEventData eventData)
        {
            if (HealthPanel.CanDrag())
                target.anchoredPosition += eventData.delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (HealthPanel.CanDrag())
                HealthPanel.PersistPosition(target.anchoredPosition);
        }
    }
}
