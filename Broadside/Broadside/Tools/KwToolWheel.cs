using HarmonyLib;
using Hotkeys;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Broadside's one patch on the mouse wheel, on the game's inner reader (<c>ZInput.Internal_GetMouseScrollWheel</c>,
    /// behind <c>ZInput.GetMouseScrollWheel</c>, read every frame by the camera's zoom and the pieces' turning; one patch per
    /// hot method). Not on the public reader: that is 21 bytes of code the runtime may copy into its callers, and the
    /// camera's copy went past the patch, so Alt + wheel on the Auto-decorate tool still zoomed the camera (user,
    /// 2026-11-03: "the scroll wheel should not zoom in and out of your game while you are holding down Alt while the auto
    /// decorate tool is open"). Shared by the tools that take the wheel while in hand: the Dock
    /// tool (<see cref="KwPierTool"/>: plain wheel the material, Alt + wheel the width) and the Auto-decorate tool
    /// (<see cref="KwDecorTool"/>: Alt + wheel only, the plain wheel left to the camera). While a tool owns it the game
    /// reads 0 and the tool reads the wheel itself once a frame (<see cref="Notches"/>), small turns (touchpads) added up
    /// to the game's one-notch amount. Alt is read the Hotkeys library's way (an Alt left stuck by Alt + Tab never
    /// counts). Owned only while asked every frame (<see cref="Own"/>), so a tool put away gives the wheel straight back;
    /// one tool is in hand at a time. Local to this player.
    /// </summary>
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.Internal_GetMouseScrollWheel))]
    public static class KwToolWheel
    {
        private const float Notch = 0.1f;
        private static bool raw;
        private static int ownedAt = -10;
        private static float turned;

        [HarmonyPostfix]
        private static void Postfix(ref float __result)
        {
            if (__result != 0f && !raw && Time.frameCount - ownedAt <= 1)
                __result = 0f;
        }

        /// <summary>Whether the tool owns the wheel this frame.</summary>
        public static void Own(bool owns)
        {
            if (owns)
                ownedAt = Time.frameCount;
            else
                (ownedAt, turned) = (-10, 0f);
        }

        /// <summary>+1, -1 or 0 notches turned this frame (up, away from the player, positive), read past the tool's own hold.</summary>
        public static int Notches()
        {
            raw = true;
            try
            {
                turned += ZInput.GetMouseScrollWheel();
            }
            finally
            {
                raw = false;
            }
            if (Mathf.Abs(turned) < Notch)
                return 0;
            int step = turned > 0f ? 1 : -1;
            turned = 0f;
            return step;
        }

        /// <summary>Either Alt held, as the Hotkeys library reads a modifier.</summary>
        public static bool Alt => Hotkey.KeyHeld(KeyCode.LeftAlt) || Hotkey.KeyHeld(KeyCode.RightAlt);
    }
}
