using UnityEngine;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// Drives the tracker from the HUD's own object (added at Hud.Awake): builds the panel the first time something is
    /// tracked, shows or hides it every frame (<see cref="TrackerVisibility"/>), rebuilds it when the tracked list or
    /// its look changes, puts it at the Position setting whenever that changes, and recounts the materials twice a
    /// second while it shows.
    /// </summary>
    public class TrackerHud : MonoBehaviour
    {
        private const float CountSeconds = 0.5f;

        private TrackerPanel panel;
        private int builtVersion = -1;
        private string builtStyle = "";
        private string placedAt;
        private float nextCount;

        private void Update()
        {
            bool show = TrackerVisibility.Show();
            if (panel == null && show && Hud.instance != null && Hud.instance.m_rootObject != null)
                panel = TrackerPanel.Create(Hud.instance.m_rootObject.transform);
            if (panel == null)
                return;
            panel.SetVisible(show);
            if (!show)
                return;
            panel.Interactive(TrackerVisibility.CursorFree);
            if (builtVersion != TrackerList.Version)
            {
                Rebuild();
                nextCount = 0f;   // counted at once, so new rows never show empty
            }
            Follow();
            if (Time.unscaledTime < nextCount)
                return;
            nextCount = Time.unscaledTime + CountSeconds;
            // The look is compared at the counting pace, not every frame (the signature is a new string each time).
            if (builtStyle != TrackerStyle.Signature)
                Rebuild();
            panel.Count(Player.m_localPlayer);
        }

        /// <summary>A new Position (the first frame, a drag's end, an edited cfg) moves the panel there.</summary>
        private void Follow()
        {
            string position = TrackerSettings.Position.Value;
            if (position == placedAt)
                return;
            placedAt = position;
            panel.Place();
        }

        private void Rebuild()
        {
            builtVersion = TrackerList.Version;
            builtStyle = TrackerStyle.Signature;
            panel.Rebuild();
            panel.KeepOnScreen();
        }
    }
}
