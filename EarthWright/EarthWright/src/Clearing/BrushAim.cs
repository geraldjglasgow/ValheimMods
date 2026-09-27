using EarthWright.Brush;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Where the brush stands this frame. The Brush module's centre is only current while a terrain entry is selected
    /// and it found ground under the crosshair this frame; otherwise the game's own ghost is the best guess.
    /// </summary>
    internal static class BrushAim
    {
        /// <summary>The brush centre is current (a terrain entry is selected and the crosshair is on the ground).</summary>
        public static bool Tracking => BrushState.Active && BrushState.HasAim;

        /// <summary>The brush centre while tracking, else the visible ghost; false when neither is there.</summary>
        public static bool TryCenter(out Vector3 center)
        {
            center = BrushState.Center;
            if (Tracking)
                return true;
            GameObject ghost = LocalTool.Ghost;
            if (ghost == null)
                return false;
            center = ghost.transform.position;
            return true;
        }
    }
}
