using HaloMenu.Runtime;
using HarmonyLib;
using UnityEngine;

namespace HaloMenu.Patches
{
    /// <summary>Keeps the hardware cursor unlocked and visible while a ring is open, the same way GameCamera
    /// already does for InventoryGui, Menu and its own radial menu - skips the vanilla method entirely for that
    /// one frame so nothing else in it can re-lock the cursor out from under the ring.</summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class CursorCapturePatch
    {
        private static bool Prefix()
        {
            if (!RingRegistry.AnyRingOpen)
                return true;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
            return false;
        }
    }
}
