using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>Redraws the overlay every few tenths of a second (real time, so a paused game still updates), and turns
    /// it off when the world unloads. Added to the plugin's own object the first time something is shown.</summary>
    internal sealed class OverlayTicker : MonoBehaviour
    {
        private float wait;

        internal static void Ensure()
        {
            GameObject host = DevBridgePlugin.Instance.gameObject;
            if (!host.GetComponent<OverlayTicker>()) host.AddComponent<OverlayTicker>();
        }

        /// <summary>The next redraw a full interval from now, after the route has just drawn.</summary>
        internal static void Drawn()
        {
            OverlayTicker ticker = DevBridgePlugin.Instance.GetComponent<OverlayTicker>();
            if (ticker) ticker.wait = OverlayView.Every;
        }

        // After the game's own updates, so the lines start from where things are this frame.
        private void LateUpdate()
        {
            if (!OverlayView.AnyOn) return;
            if (!ZNetScene.instance)
            {
                OverlayView.Stop();
                return;
            }
            wait -= Time.unscaledDeltaTime;
            if (wait > 0f) return;
            wait = OverlayView.Every;
            OverlayView.Redraw();
        }
    }
}
