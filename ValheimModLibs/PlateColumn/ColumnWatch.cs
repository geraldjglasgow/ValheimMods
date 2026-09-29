using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>
    /// Re-lays the column out the frame any box is shown or hidden, whoever does it. The layout group hears of a child
    /// going inactive only through a Graphic on that child itself, and a box's root draws nothing (its background is a
    /// child), so without this a box hidden by a mod whose older copy of the library stands down (it calls its own
    /// <c>Arrange</c>, which does nothing) would leave a gap. Also on the HUD row (<see cref="HudContainer"/>). Runs only
    /// while its row is shown (the inventory open, or the small map on the HUD); cheap: one pass over a handful of
    /// children, no allocation.
    /// </summary>
    internal sealed class ColumnWatch : MonoBehaviour
    {
        private int seen = int.MinValue;

        private void LateUpdate()
        {
            int shape = Shape(transform);
            if (shape != seen)
            {
                seen = shape;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            }
        }

        /// <summary>Which children are active, folded into one number.</summary>
        private static int Shape(Transform boxes)
        {
            int shape = boxes.childCount;
            for (int i = 0; i < boxes.childCount; i++)
            {
                shape = unchecked(shape * 31 + (boxes.GetChild(i).gameObject.activeSelf ? 1 : 0));
            }
            return shape;
        }
    }
}
