using OpenKeep.Blueprints.Sites;
using UnityEngine;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The ghost piece under the crosshair: the nearest unbuilt piece of any loaded site along the camera's view
    /// (<see cref="SiteGhost.Pick"/>, within <see cref="BlueprintRules.AimRange"/>), kept for the frame by
    /// <see cref="Update"/> and asked again on a click.
    /// </summary>
    public static class PlannerAim
    {
        public static SiteMarker Site { get; private set; }

        public static int Piece { get; private set; } = -1;

        public static bool Any => Site != null && Piece >= 0;

        public static void Update()
        {
            bool hit = Pick(out SiteMarker site, out int piece);
            Site = hit ? site : null;
            Piece = hit ? piece : -1;
        }

        public static void Reset()
        {
            Site = null;
            Piece = -1;
        }

        /// <summary>The site and piece under the crosshair now; false when no ghost piece is there.</summary>
        public static bool Pick(out SiteMarker site, out int piece)
        {
            site = null;
            piece = -1;
            GameCamera camera = GameCamera.instance;
            if (camera == null)
                return false;
            Ray view = new Ray(camera.transform.position, camera.transform.forward);
            if (!SiteGhost.Pick(view, BlueprintRules.AimRange, out site, out piece) || site == null)
                return false;
            Blueprint bp = site.State?.Blueprint;
            return bp != null && piece >= 0 && piece < bp.Pieces.Count;
        }
    }
}
