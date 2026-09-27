using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The copy-floor-height key: casts the camera ray against building pieces only (terrain and rocks are looked
    /// through) and answers the top of the piece that was hit: the hit point when its top face was aimed at, otherwise
    /// the top of its collider. Local: reads this machine's copy of the pieces.
    /// </summary>
    public static class FloorPick
    {
        private const float RayLength = 60f;
        private static int pieceMask;

        public static bool TryHeight(out float height)
        {
            height = 0f;
            GameCamera camera = GameCamera.instance;
            if (camera == null)
                return false;
            if (pieceMask == 0)
                pieceMask = LayerMask.GetMask("piece", "piece_nonsolid");
            Transform eye = camera.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, RayLength, pieceMask) || hit.collider == null)
                return false;
            if (hit.collider.GetComponentInParent<Piece>() == null)
                return false;
            height = hit.normal.y > 0.7f ? hit.point.y : hit.collider.bounds.max.y;
            return true;
        }
    }
}
