using HarmonyLib;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// While the panel is open the game treats it like one of its own windows, using the same checks the game uses for
    /// the inventory and the store: the player does not attack, place or use hotkeys (<c>Player.TakeInput</c>), the
    /// camera does not follow the mouse (<c>PlayerController.InInventoryEtc</c>), the cursor is shown and free
    /// (<c>GameCamera.UpdateMouseCapture</c>) and the wheel scrolls the panel instead of zooming. Walking stays possible
    /// unless a panel field is being typed in. Esc closes the panel instead of opening the game menu.
    /// </summary>
    public static class PanelInputPatches
    {
        [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
        public static class PlayerInput
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                if (PanelWindow.IsOpen)
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.InInventoryEtc))]
        public static class MouseLook
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                if (PanelWindow.IsOpen)
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
        public static class Movement
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                if (PanelWindow.IsOpen && PanelWindow.Typing)
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
        public static class FreeCursor
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!PanelWindow.IsOpen || global::Menu.IsVisible())
                    return;
                ZCursor.LockState = CursorLockMode.None;
                ZCursor.Show();
            }
        }

        [HarmonyPatch(typeof(global::Menu), nameof(global::Menu.Update))]
        public static class Escape
        {
            [HarmonyPrefix]
            public static bool Prefix(global::Menu __instance)
            {
                if (!PanelWindow.IsOpen || __instance.m_root == null || __instance.m_root.gameObject.activeSelf)
                    return true;
                if (!ZInput.GetKeyDown(KeyCode.Escape))
                    return true;
                PanelWindow.Close();
                return false;
            }
        }
    }
}
