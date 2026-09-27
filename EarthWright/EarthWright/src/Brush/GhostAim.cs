using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Finds the ground under the crosshair for the brush. First the game's own placement ray (same origin, mask, length
    /// and reach as <c>Player.PieceRayTest</c>): when it hits terrain the brush aims there, exactly where the game does.
    /// When it hits something else (a rock, tree, cliff, water) or nothing in reach, "no silent blocks": a terrain-only
    /// ray finds the ground behind the object, or else the ground straight under the hit point. Building pieces are
    /// not looked through.
    /// Runs on the player's machine against its own colliders.
    /// </summary>
    public static class GhostAim
    {
        private const float GameRayLength = 50f;
        private static int terrainMask;

        /// <param name="direct">The game's own ray hit terrain (the game's placement checks are valid as they are).</param>
        public static bool TryAim(Player player, out Vector3 point, out bool direct)
        {
            point = Vector3.zero;
            direct = false;
            GameCamera camera = GameCamera.instance;
            if (camera == null || player.m_eye == null)
                return false;
            Vector3 origin = camera.transform.position, forward = camera.transform.forward;
            float reach = Reach(player);
            bool hitSomething = Physics.Raycast(origin, forward, out RaycastHit hit, GameRayLength, RayMask(player)) && hit.collider != null;
            bool inReach = hitSomething && !hit.collider.attachedRigidbody && InReach(player, hit.point, reach);
            if (inReach && hit.collider.GetComponent<Heightmap>() != null)
            {
                point = hit.point;
                direct = true;
                return true;
            }
            if (!BrushSettings.AimThroughObjects.Value || (hitSomething && IsBuilding(hit.collider)))
                return false;
            return Behind(player, origin, forward, reach, out point) || (inReach && Under(hit.point, out point));
        }

        /// <summary>The game's placement reach: the player's place distance plus the piece's extra distance.</summary>
        public static float Reach(Player player)
        {
            float reach = player.m_maxPlaceDistance;
            Piece piece = player.m_placementGhost != null ? player.m_placementGhost.GetComponent<Piece>() : null;
            if (piece != null)
                reach += piece.m_extraPlacementDistance;
            return reach;
        }

        /// <summary>Building pieces (walls, floors, carts, ships) keep the game's refusal: the brush never aims under a building.</summary>
        private static bool IsBuilding(Collider collider) => collider.GetComponentInParent<Piece>() != null;

        private static bool InReach(Player player, Vector3 point, float reach) => Vector3.Distance(player.m_eye.position, point) < reach;

        /// <summary>The mask the game's ray uses for the selected piece (with water for water-aware pieces).</summary>
        private static int RayMask(Player player)
        {
            Piece piece = player.m_placementGhost != null ? player.m_placementGhost.GetComponent<Piece>() : null;
            bool water = piece != null && (piece.m_waterPiece || piece.m_noInWater);
            return water ? player.m_placeWaterRayMask : player.m_placeRayMask;
        }

        private static bool Behind(Player player, Vector3 origin, Vector3 forward, float reach, out Vector3 point)
        {
            point = Vector3.zero;
            if (terrainMask == 0)
                terrainMask = LayerMask.GetMask("terrain");
            float length = Mathf.Max(GameRayLength, reach + 20f);
            if (!Physics.Raycast(origin, forward, out RaycastHit ground, length, terrainMask) || !InReach(player, ground.point, reach))
                return false;
            point = ground.point;
            return true;
        }

        private static bool Under(Vector3 hitPoint, out Vector3 point)
        {
            point = hitPoint;
            if (!Heightmap.GetHeight(hitPoint, out float height))
                return false;
            point.y = height;
            return true;
        }
    }
}
