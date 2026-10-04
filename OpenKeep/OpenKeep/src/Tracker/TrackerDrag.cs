using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// The tracker's title is its handle: dragging it moves the tracker, kept inside the screen, and the place is
    /// written to the Position setting when the drag ends (whole interface units, x right and y down from the top left).
    /// </summary>
    public class TrackerDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Target;

        private Vector2 last;

        public void OnBeginDrag(PointerEventData data)
        {
            if (Target == null || !Local(data, out last))
                last = Vector2.zero;
        }

        public void OnDrag(PointerEventData data)
        {
            if (Target == null || !Local(data, out Vector2 now))
                return;
            Target.anchoredPosition += now - last;
            last = now;
            Clamp(Target);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (Target == null)
                return;
            Clamp(Target);
            Vector2 at = Target.anchoredPosition;
            // Written counting down from the top, as the setting reads; the anchored y counts up.
            TrackerSettings.Position.Value = string.Format(CultureInfo.InvariantCulture, "{0:0},{1:0}", at.x, -at.y);
        }

        private bool Local(PointerEventData data, out Vector2 point)
        {
            RectTransform parent = (RectTransform)Target.parent;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera, out point);
        }

        /// <summary>The whole tracker stays on screen (its top left corner is anchored to the parent's top left).</summary>
        public static void Clamp(RectTransform target)
        {
            Rect area = ((RectTransform)target.parent).rect;
            Vector2 size = Vector2.Scale(target.rect.size, target.localScale);
            float x = Mathf.Clamp(target.anchoredPosition.x, 0f, Mathf.Max(0f, area.width - size.x));
            float y = Mathf.Clamp(target.anchoredPosition.y, -Mathf.Max(0f, area.height - size.y), 0f);
            target.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>The Position setting, when it holds two numbers: x right and y down from the screen's top left.</summary>
        public static bool Saved(out Vector2 position)
        {
            position = Vector2.zero;
            string[] parts = (TrackerSettings.Position.Value ?? "").Split(',');
            if (parts.Length != 2)
                return false;
            NumberStyles style = NumberStyles.Float;
            if (!float.TryParse(parts[0].Trim(), style, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(parts[1].Trim(), style, CultureInfo.InvariantCulture, out float y))
                return false;
            position = new Vector2(x, y);
            return true;
        }
    }
}
