using HarmonyLib;
using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// While the camera is out the game's look input turns the camera instead of the player: <c>Player.SetMouseLook</c>
    /// is fed by <c>PlayerController.LateUpdate</c> (and the gyro) with the game's sensitivity, inversion and gamepad
    /// handling already applied, so the camera turns exactly as the player would.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
    public static class CameraLookPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, ref Vector2 mouseLook)
        {
            if (!CameraState.IsOut(__instance))
                return;
            CameraState.Look(mouseLook);
            mouseLook = Vector2.zero;
        }
    }

    /// <summary>
    /// While the camera is out the player's body stays where it stood: walk, jump, crouch, run, autorun, dodge and block
    /// are held at rest in <c>Player.SetControls</c> (fed by <c>PlayerController.FixedUpdate</c>), so the same keys fly
    /// the camera only; a seated player stays seated. Attack is left alone: the game's place mode turns it into building.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class CameraControlsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, ref Vector3 movedir, ref bool block, ref bool blockHold, ref bool jump,
            ref bool crouch, ref bool run, ref bool autoRun, ref bool dodge)
        {
            if (!CameraState.IsOut(__instance))
                return;
            movedir = Vector3.zero;
            block = blockHold = jump = crouch = run = autoRun = dodge = false;
        }
    }
}
