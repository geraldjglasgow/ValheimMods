using UnityEngine;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>Frames the large map when a portal opens it for targeting: centred on every portal the player may
    /// target and the one they stand at, zoomed out until all of them fit with <see cref="Margin"/> to spare, in or
    /// out from wherever the player left the zoom. Portals close together still show <see cref="MinSpan"/> metres of
    /// map height.</summary>
    internal static class MapFit
    {
        private const float Margin = 1.3f;    // the portals' extent plus 15 % on every side
        private const float MinSpan = 500f;   // metres of map height at the closest fit

        public static void FitPortals(Minimap map, Player player, Vector3 here)
        {
            Bounds bounds = PortalBounds(player, here);
            Rect rect = map.m_mapImageLarge.rectTransform.rect;
            float aspect = rect.height > 0f ? rect.width / rect.height : 1f;
            float span = Mathf.Max(Mathf.Max(bounds.size.z, bounds.size.x / aspect) * Margin, MinSpan);
            map.LargeZoom = span / (map.m_textureSize * map.m_pixelSize);
            map.m_mapOffset = bounds.center - Flat(player.transform.position);
        }

        private static Bounds PortalBounds(Player player, Vector3 here)
        {
            long playerId = player.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            Bounds bounds = new Bounds(Flat(here), Vector3.zero);
            foreach (PortalInfo info in PortalRegistry.Portals)
            {
                if (PortalAccess.MayTarget(info.Mode, info.Owner, playerId, isAdmin))
                    bounds.Encapsulate(Flat(info.Position));
            }
            return bounds;
        }

        private static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);
    }
}
