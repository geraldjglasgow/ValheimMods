using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The mouse wheel while a terrain entry is selected. The brush reads the raw wheel through a bypass flag; for the
    /// rest of the game (the camera zoom, piece rotation) the wheel reads 0 while EarthWright uses it, or the whole time
    /// the terrain entry is selected, as "Camera Zoom Block" says. The game zooms in place mode whenever the selected
    /// piece cannot rotate, which is true for every terrain piece, so without this Alt+wheel would also zoom.
    /// </summary>
    public static class ScrollInput
    {
        /// <summary>The wheel amount of one step (the game's piece rotation threshold, <c>Player.m_scrollAmountThreshold</c>).</summary>
        private const float NotchThreshold = 0.1f;

        private static bool bypass;
        private static float pending;

        /// <summary>The wheel changes a brush value this frame (modifier held, no modifier needed, or plain wheel on).</summary>
        public static bool Adjusting
        {
            get
            {
                if (!InputGate.Open)
                    return false;
                if (ControlSettings.PlainWheel.Value || ControlSettings.AdjustModifier.Value.MainKey == KeyCode.None)
                    return true;
                return Keys.Held(ControlSettings.AdjustModifier);
            }
        }

        /// <summary>The wheel this frame as the game reads it, past the zoom block. Positive is away from the player.</summary>
        public static float Raw()
        {
            bypass = true;
            try
            {
                return ZInput.GetMouseScrollWheel();
            }
            finally
            {
                bypass = false;
            }
        }

        /// <summary>
        /// +1, -1 or 0 steps for the brush this frame (0 while the brush is not adjusting). Small wheel deltas (touchpads)
        /// add up until they reach the game's own one-notch threshold, as the game does for piece rotation.
        /// </summary>
        public static int Notches()
        {
            if (!Adjusting)
            {
                pending = 0f;
                return 0;
            }
            pending += Raw();
            if (Mathf.Abs(pending) < NotchThreshold)
                return 0;
            int step = pending > 0f ? 1 : -1;
            pending = 0f;
            return step;
        }

        internal static bool HideFromGame()
        {
            if (bypass || !BrushState.Active)
                return false;
            switch (ControlSettings.ZoomBlocking.Value)
            {
                case ZoomBlock.Always: return InputGate.Open;
                case ZoomBlock.WhileAdjusting: return Adjusting;
                default: return false;
            }
        }
    }
}
