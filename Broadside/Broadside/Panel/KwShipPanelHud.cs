using System;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Drives the ship panel from the HUD's own object (added at Hud.Awake): while the local player is aboard a launched
    /// Broadside ship (<c>KW_ship</c>, built: its <see cref="KwSeaShip"/>), alive and with the large map closed, it builds
    /// the panel the first time, shows it, tests the hover every frame and measures and redraws it four times a second
    /// (<see cref="KwShipPanelLines"/>); aboard any Broadside ship it keeps ShipConfig's panel off screen
    /// (<see cref="KwShipConfigPanel"/>). Runs in LateUpdate, after ShipConfig's own Update has shown its panel. Everything
    /// here is the local player's own view, read from synced state; nothing is networked, and a dedicated server has no HUD.
    /// </summary>
    public sealed class KwShipPanelHud : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;

        private readonly KwSpeedometer speedometer = new KwSpeedometer();
        private readonly KwShipConfigPanel shipConfig = new KwShipConfigPanel();
        private KwShipPanelView? view;
        private float nextRefresh, nextLook;
        private bool showing;

        // The ship last looked at and its parts, so a frame asks for no component.
        private Ship? ship;
        private KwShipBody? body;
        private KwSeaShip? sea;
        private WearNTear? wear;

        private void LateUpdate()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                // Logged once; the panel stays off for the session rather than failing every frame.
                Guard.Report(e, "Broadside ship panel");
                enabled = false;
                view?.SetVisible(false);
                shipConfig.Restore();
            }
        }

        private void OnDisable() => shipConfig.Restore();

        private void Tick()
        {
            Look(Ship.GetLocalShip());
            Transform? hudRoot = Hud.instance != null && Hud.instance.m_rootObject != null ? Hud.instance.m_rootObject.transform : null;
            if (body != null && hudRoot != null)
                shipConfig.Hide(hudRoot);
            else
                shipConfig.Restore();
            bool show = sea != null && ship != null && Shown();
            if (show && view == null && hudRoot != null)
                view = KwShipPanelView.Create(hudRoot);
            if (view == null)
                return;
            if (show != showing)
                Showing(show);
            if (show)
                Draw(view, ship!, sea!);
        }

        /// <summary>The local player's ship and its Broadside parts, looked up when the ship changes (a ship still being built: twice a second until it is).</summary>
        private void Look(Ship? current)
        {
            if (current != ship)
            {
                ship = current;
                body = current != null ? current.GetComponent<KwShipBody>() : null;
                wear = body != null ? current!.GetComponent<WearNTear>() : null;
                (sea, nextLook) = (null, 0f);
            }
            if (body == null || sea != null || Time.unscaledTime < nextLook)
                return;
            nextLook = Time.unscaledTime + 0.5f;
            sea = body.GetComponent<KwSeaShip>();
        }

        private static bool Shown()
        {
            Player? player = Player.m_localPlayer;
            return player != null && !player.IsDead() && !Minimap.IsOpen();
        }

        /// <summary>Shown or hidden; once hidden, speed, hover and refresh start over when it shows again.</summary>
        private void Showing(bool show)
        {
            showing = show;
            view!.SetVisible(show);
            if (show)
                return;
            speedometer.Reset();
            view.Hover.Reset();
            nextRefresh = 0f;
        }

        private void Draw(KwShipPanelView panel, Ship current, KwSeaShip built)
        {
            bool cursorFree = ZCursor.LockState != CursorLockMode.Locked && ZCursor.IsVisible;
            panel.Interactive(cursorFree);
            panel.Hover.Update(cursorFree);
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            speedometer.Sample(current);
            KwShipPanelLines.Refresh(panel, current, built, wear, speedometer.Speed);
            panel.Place();
        }
    }

    /// <summary>The ship panel's driver put on the HUD as it wakes (screens only: a dedicated server has no HUD).</summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    public static class KwShipPanelHudPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Hud __instance) =>
            Guard.Run("Broadside ship panel setup", hud => hud.gameObject.AddComponent<KwShipPanelHud>(), __instance);
    }
}
