using HarmonyLib;
using UnityEngine;
using WindowInput;

namespace HaloMenu.Runtime
{
    /// <summary>An open ring is a game window to the WindowInput library, the way the game's own radial menu holds
    /// input: the cursor is free, the camera does not follow the mouse, the player does not attack, block, use or
    /// press hotbar keys (walking stays), and Esc cancels the ring instead of opening the game menu. A mouse button
    /// that closed a Toggle ring stays swallowed until it is let go, so the click that selects never also swings.</summary>
    public static class RingWindow
    {
        private static int swallowedButton = -1;

        public static void Install(Harmony harmony)
        {
            GameWindow.Install(harmony);
            GameWindow.Add(IsOpen, Cancel);
        }

        public static void SwallowUntilReleased(int mouseButton) => swallowedButton = mouseButton;

        private static bool IsOpen()
        {
            if (RingRegistry.AnyRingOpen)
                return true;
            if (swallowedButton >= 0 && !Input.GetMouseButton(swallowedButton))
                swallowedButton = -1;
            return swallowedButton >= 0;
        }

        private static void Cancel()
        {
            swallowedButton = -1;
            RingRegistry.CurrentlyOpen?.CancelFromOutside();
        }
    }
}
