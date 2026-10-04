using PatchGuard;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenKeep.Tracker
{
    /// <summary>The mouse wheel over a tracked recipe's header: one more or one less, with Shift to the next ten.</summary>
    public class TrackerWheel : MonoBehaviour, IScrollHandler
    {
        public int Index;

        public void OnScroll(PointerEventData data)
        {
            float delta = data.scrollDelta.y;
            if (Mathf.Approximately(delta, 0f))
                return;
            bool tens = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Guard.Run("tracker wheel", () => TrackerList.Step(Index, delta > 0f ? 1 : -1, tens));
        }
    }
}
