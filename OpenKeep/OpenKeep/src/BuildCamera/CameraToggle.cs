using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Brings the camera out and back, after the local player's update. Out: Toggle Key or Gamepad Toggle while the
    /// game takes input (no inventory, chat, console, map, menu or build menu), with a build tool in hand, standing in
    /// a station's camera area, with Entry Needs Resting and Entry Min Comfort met; it starts where the game camera is.
    /// Back: the key again, or at once when the tool leaves the hand (Hide, R, or unequipping ends the game's place
    /// mode), the player dies or teleports, takes a seat with its own camera (a ship's helm), the game's free fly
    /// camera starts, the setting is turned off, or no station's area holds the player any more.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class CameraToggle
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            bool pressed = TogglePressed(__instance);
            if (CameraState.Active)
            {
                if (pressed || MustEnd(__instance))
                    CameraState.Exit();
                return;
            }
            if (pressed && CameraSettings.Enabled.Value && CanUse(__instance))
                TryEnter(__instance);
        }

        private static bool TogglePressed(Player player)
        {
            if (!player.TakeInput() || Hud.InRadial() || Hud.IsPieceSelectionVisible())
                return false;
            return Keys.Pressed(CameraPrefs.ToggleKey) || PadToggle.Pressed();
        }

        private static bool CanUse(Player player)
        {
            return player.InPlaceMode() && !player.IsDead() && !player.IsTeleporting() && player.GetAttachCameraPoint() == null
                && GameCamera.instance != null && !GameCamera.InFreeFly();
        }

        private static bool MustEnd(Player player)
        {
            return !CameraSettings.Enabled.Value || !CanUse(player) || !CameraArea.Contains(player.transform.position);
        }

        private static void TryEnter(Player player)
        {
            if (!CameraArea.Contains(player.transform.position))
            {
                Messages.Center(CameraModule.NoStation);
                return;
            }
            string missing = CameraNeeds.Describe(player, CameraSettings.EntryNeedsResting, CameraSettings.EntryMinComfort);
            if (missing != null)
            {
                Messages.Center(Language.Localize(CameraModule.EntryNeeds) + ": " + missing);
                return;
            }
            Transform view = GameCamera.instance.transform;
            CameraState.Enter(player, view);
            CameraState.Position = CameraArea.Clamp(view.position);
        }
    }
}
