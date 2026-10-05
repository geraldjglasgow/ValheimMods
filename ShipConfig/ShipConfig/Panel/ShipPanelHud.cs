using System;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// Drives the ship panel from the HUD's own object (added at Hud.Awake): builds it the first time the local player
    /// is aboard a ship, shows it while they are (alive, the large map closed, the Ship Panel setting on), tests the
    /// hover every frame, and measures and redraws it four times a second. Everything here is the local player's own
    /// view; nothing is networked.
    /// </summary>
    public class ShipPanelHud : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;

        private readonly Speedometer speedometer = new Speedometer();
        private ShipPanel panel;
        private float nextRefresh;

        private void Update()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                // Logged once; the panel stays off for the session rather than failing every frame.
                Guard.Report(e, "ship panel");
                enabled = false;
                panel?.SetVisible(false);
            }
        }

        private void Tick()
        {
            Ship ship = ShownFor();
            if (ship != null && panel == null && Hud.instance != null && Hud.instance.m_rootObject != null)
                panel = ShipPanel.Create(Hud.instance.m_rootObject.transform);
            if (panel == null)
                return;
            panel.SetVisible(ship != null);
            if (ship == null)
            {
                Hidden();
                return;
            }
            bool cursorFree = ZCursor.LockState != CursorLockMode.Locked && ZCursor.IsVisible;
            panel.Interactive(cursorFree);
            panel.Hover.Update(cursorFree);
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            speedometer.Sample(ship);
            panel.Refresh(ship, speedometer.Speed);
            panel.Place();
        }

        /// <summary>The panel hid: speed, hover and refresh start over when it shows again.</summary>
        private void Hidden()
        {
            speedometer.Reset();
            panel.Hover.Reset();
            nextRefresh = 0f;
        }

        /// <summary>The ship the local player is aboard while the panel should show, else null.</summary>
        private static Ship ShownFor()
        {
            Player player = Player.m_localPlayer;
            if (!PanelSettings.ShipPanel.Value || player == null || player.IsDead() || Minimap.IsOpen())
                return null;
            return Ship.GetLocalShip();
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    public static class HudAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hud __instance) =>
            Guard.Run("ship panel setup", () => __instance.gameObject.AddComponent<ShipPanelHud>());
    }
}
