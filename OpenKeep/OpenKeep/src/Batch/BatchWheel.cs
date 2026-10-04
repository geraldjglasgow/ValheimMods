using PatchGuard;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenKeep.Batch
{
    /// <summary>
    /// The mouse wheel over the stepper's buttons or amount: up is +, down is -, with the clicks' Shift (tens) and Ctrl
    /// (to 1 or the most) rules. On each part of the stepper rather than its row, since the amount field passes the
    /// wheel to its parent and a handler on the row would then step twice.
    /// </summary>
    public class BatchWheel : MonoBehaviour, IScrollHandler
    {
        public void OnScroll(PointerEventData data)
        {
            float delta = data.scrollDelta.y;
            if (Mathf.Approximately(delta, 0f))
                return;
            Guard.Run("batch wheel", () => BatchAmount.Step(delta > 0f ? 1 : -1));
        }
    }
}
