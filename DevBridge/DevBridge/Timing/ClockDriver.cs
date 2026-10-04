using UnityEngine;

namespace DevBridge.Timing
{
    /// <summary>
    /// Writes the held scale in LateUpdate, after Game.Update has written the game's own, so each next frame runs at the
    /// held one. The game's own pause wins while it lasts (its 0 stays), and the hold ends when the world closes.
    /// </summary>
    internal sealed class ClockDriver : MonoBehaviour
    {
        internal static void Ensure()
        {
            GameObject host = DevBridgePlugin.Instance.gameObject;
            if (!host.GetComponent<ClockDriver>()) host.AddComponent<ClockDriver>();
        }

        private void LateUpdate()
        {
            if (!ClockHold.Held) return;
            if (!Game.instance) ClockHold.Release("the world closed");
            else if (!GameClock.Paused) Time.timeScale = ClockHold.NextFrame();
        }
    }
}
