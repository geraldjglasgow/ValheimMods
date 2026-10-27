using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The view while building a ship. With a kit piece in the hammer (or the size window open) the camera may zoom out
    /// <see cref="Reach"/> times as far as the game lets (the kit's tools do not turn, so the game zooms them with the
    /// wheel), and holding the middle mouse button while moving the mouse pans the view: the camera slides across the
    /// view, the player stays, and mouse look waits meanwhile. With the size window open (the cursor free), holding the
    /// right mouse button turns the view as mouse look does. The pan is dropped when no kit piece is in hand, and there is
    /// none while the table's build camera is out (<see cref="KwBpCamera"/>: it flies itself). Local only.
    /// </summary>
    [HarmonyPatch]
    public static class KwBuildView
    {
        private const float Reach = 3f, PanSpeed = 0.0025f, MaxPan = 40f;
        private static float gameReach = -1f;
        private static Vector3 pan;

        /// <summary>A kit piece is in the hammer, or the size window is open.</summary>
        public static bool Active
        {
            get
            {
                Player? me = Player.m_localPlayer;
                if (me == null)
                    return false;
                Piece? piece = me.InPlaceMode() ? me.GetSelectedPiece() : null;
                return KwForm.IsOpen || (piece != null && piece.name.StartsWith("KW_"));
            }
        }

        private static bool Panning => ZInput.GetMouseButton(2) && Active && !KwBpCamera.Out;
        private static bool Turning => ZInput.GetMouseButton(1) && KwForm.IsOpen;

        [HarmonyPatch(typeof(GameCamera), "UpdateCamera"), HarmonyPrefix]
        private static void Prefix(GameCamera __instance) => Guard.Run("Broadside view reach", Stretch, __instance);

        [HarmonyPatch(typeof(GameCamera), "UpdateCamera"), HarmonyPostfix]
        private static void Postfix(GameCamera __instance) => Guard.Run("Broadside view pan", Slide, __instance);

        // The camera's farthest zoom: the game's, or Reach times it while building.
        private static void Stretch(GameCamera camera)
        {
            if (gameReach < 0f)
                gameReach = camera.m_maxDistance;
            camera.m_maxDistance = Active ? gameReach * Reach : gameReach;
        }

        // The pan grows while the middle button is held (across the view, faster the farther out the camera is) and moves
        // the camera, which the game places afresh every frame.
        private static void Slide(GameCamera camera)
        {
            if (!Active || KwBpCamera.Out)
            {
                pan = Vector3.zero;
                return;
            }
            if (Panning)
            {
                Vector2 delta = ZInput.GetMouseDelta() * PanSpeed * (camera.m_distance + 2f);
                pan -= camera.transform.right * delta.x + camera.transform.up * delta.y;
                pan = Vector3.ClampMagnitude(pan, MaxPan);
            }
            camera.transform.position += pan;
        }

        // Mouse look waits while panning; with the size window open it works while the right button is held.
        [HarmonyPatch(typeof(PlayerController), "InInventoryEtc"), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void InWindow(ref bool __result)
        {
            if (Panning)
                __result = true;
            else if (Turning)
                __result = false;
        }

        // With the size window open the wheel zooms the view, also for a tool the wheel otherwise turns.
        [HarmonyPatch(typeof(Player), nameof(Player.CanRotatePiece)), HarmonyPostfix]
        private static void NoTurnInWindow(ref bool __result)
        {
            if (KwForm.IsOpen)
                __result = false;
        }

        // The size window holds the player's input (typing goes to its boxes); turning the view gets mouse look back.
        [HarmonyPatch(typeof(PlayerController), "TakeInput"), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void TakeInput(bool look, ref bool __result)
        {
            if (look && Turning)
                __result = true;
        }
    }
}
