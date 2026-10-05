using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The piece under the crosshair that can be copied: the first thing the camera's view meets (the game's own build
    /// hover layers, terrain included, so nothing is picked through a hill), when it is a player-built piece a blueprint
    /// can hold (<see cref="BlueprintCapture.Copyable"/>: never a ship, a cart or a construction site's post), within
    /// <see cref="BlueprintRules.AimRange"/>. Kept for the frame by <see cref="Update"/> and asked again on a click.
    /// </summary>
    public static class CopyAim
    {
        private static int mask;

        public static Piece Piece { get; private set; }

        public static void Update() => Piece = Pick();

        public static void Reset() => Piece = null;

        /// <summary>The copyable piece under the crosshair now, or null.</summary>
        public static Piece Pick()
        {
            GameCamera camera = GameCamera.instance;
            if (mask == 0)
                mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
            if (camera == null || !Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, BlueprintRules.AimRange, mask))
                return null;
            Piece piece = hit.collider.GetComponentInParent<Piece>();
            return BlueprintCapture.Copyable(piece) ? piece : null;
        }
    }
}
