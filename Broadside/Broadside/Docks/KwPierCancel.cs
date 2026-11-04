using HarmonyLib;
using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Esc cancels the Dock tool's waiting first click instead of opening the game menu (<c>Menu.Update</c> skipped for that
    /// frame, as the WindowInput library does for a window), and so does the build menu's button (a right click) instead
    /// of opening the build menu (<c>Player.UpdateBuildGuiInput</c> skipped for that frame). Neither while anything is
    /// being typed, a menu is up, or no first click waits. Local to this player.
    /// </summary>
    [HarmonyPatch]
    public static class KwPierCancel
    {
        private const string Cancelled = "Dock cancelled";

        [HarmonyPatch(typeof(Menu), "Update"), HarmonyPrefix]
        private static bool Escape(Menu __instance) => !Guard.Run("Broadside dock cancel", () => Takes(__instance));

        [HarmonyPatch(typeof(Player), "UpdateBuildGuiInput"), HarmonyPrefix]
        private static bool RightClick(Player __instance) => !Guard.Run("Broadside dock cancel", () => Clicked(__instance));

        // Esc down with the game menu hidden and a first click waiting.
        private static bool Takes(Menu menu)
        {
            if (KwPierTool.Pending == null || menu.m_root == null || menu.m_root.gameObject.activeSelf || Typing.Active || !ZInput.GetKeyDown(KeyCode.Escape))
                return false;
            KwPierTool.Cancel(Cancelled);
            return true;
        }

        // The build menu's button down on the local player's frame with a first click waiting and the build menu shut.
        private static bool Clicked(Player player)
        {
            if (player != Player.m_localPlayer || KwPierTool.Pending == null || Hud.IsPieceSelectionVisible() || Typing.Active
                || !ZInput.GetButtonDown("BuildMenu"))
                return false;
            KwPierTool.Cancel(Cancelled);
            return true;
        }
    }
}
