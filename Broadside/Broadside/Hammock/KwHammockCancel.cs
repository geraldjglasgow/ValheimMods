using HarmonyLib;
using Hotkeys;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// Esc cancels the hammock's waiting first end instead of opening the game menu (<c>Menu.Update</c> skipped for that
    /// frame), and so does the build menu's button (a right click) instead of opening the build menu
    /// (<c>Player.UpdateBuildGuiInput</c> skipped for that frame), as the Dock tool's (<see cref="KwPierCancel"/>). Neither
    /// while anything is being typed, a menu is up, or no first end waits. Local to this player.
    /// </summary>
    [HarmonyPatch]
    public static class KwHammockCancel
    {
        [HarmonyPatch(typeof(Menu), "Update"), HarmonyPrefix]
        private static bool Escape(Menu __instance) => !Guard.Run("Broadside hammock cancel", () => Takes(__instance));

        [HarmonyPatch(typeof(Player), "UpdateBuildGuiInput"), HarmonyPrefix]
        private static bool RightClick(Player __instance) => !Guard.Run("Broadside hammock cancel", () => Clicked(__instance));

        // Esc down with the game menu hidden and a first end waiting.
        private static bool Takes(Menu menu)
        {
            if (KwHammockTool.Pending == null || menu.m_root == null || menu.m_root.gameObject.activeSelf || Typing.Active || !ZInput.GetKeyDown(UnityEngine.KeyCode.Escape))
                return false;
            KwHammockTool.Cancel(KwHammockWords.Cancelled);
            return true;
        }

        // The build menu's button down on the local player's frame with a first end waiting and the build menu shut.
        private static bool Clicked(Player player)
        {
            if (player != Player.m_localPlayer || KwHammockTool.Pending == null || Hud.IsPieceSelectionVisible() || Typing.Active
                || !ZInput.GetButtonDown("BuildMenu"))
                return false;
            KwHammockTool.Cancel(KwHammockWords.Cancelled);
            return true;
        }
    }
}
