using System;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Picking a ghost piece of one site with a ray (for <see cref="SiteGhost.Pick"/>): each shown piece's drawn box
    /// (<see cref="PieceShapes"/> bounds in the prefab's space) turned and moved with the piece in the site's frame, the
    /// ray brought into that box's space instead (turning keeps distances), the nearest hit. A site whose whole area the
    /// ray passes far from is skipped at once.
    /// </summary>
    internal static class GhostPick
    {
        /// <summary>The nearest shown piece hit closer than <paramref name="best"/> (which it then lowers to that hit).</summary>
        public static bool Nearest(Blueprint bp, SiteState s, Func<int, bool> shown, Ray ray, ref float best, out int piece)
        {
            piece = -1;
            BuildFrame frame = s.Frame;
            if (!Near(bp, frame, ray, best))
                return false;
            for (int i = 0; i < bp.Pieces.Count; i++)
            {
                if (!shown(i))
                    continue;
                float hit = Hit(bp.Pieces[i], frame, ray);
                if (hit >= 0f && hit < best)
                {
                    best = hit;
                    piece = i;
                }
            }
            return piece >= 0;
        }

        /// <summary>The distance along the ray to the piece's drawn box, or -1 when it misses (or the piece has no shape).</summary>
        private static float Hit(BlueprintPiece p, BuildFrame frame, Ray ray)
        {
            PieceShape shape = PieceShapes.Of(p.Prefab);
            if (shape?.Template == null)
                return -1f;
            Quaternion back = Quaternion.Inverse(frame.PieceRotation(p.Yaw));
            Vector3 at = frame.World(p.X, p.Y, p.Z);
            Ray local = new Ray(back * (ray.origin - at), back * ray.direction);
            return shape.Bounds.IntersectRay(local, out float distance) ? Mathf.Max(0f, distance) : -1f;
        }

        /// <summary>Seen from above, the ray's first <paramref name="reach"/> metres pass over the circle holding the whole site (plus room for the pieces' size).</summary>
        private static bool Near(Blueprint bp, BuildFrame frame, Ray ray, float reach)
        {
            SiteArea.Circle(bp, frame, 4f, out Vector3 centre, out float radius);
            Vector2 o = new Vector2(ray.origin.x, ray.origin.z);
            Vector2 d = new Vector2(ray.direction.x, ray.direction.z);
            Vector2 c = new Vector2(centre.x, centre.z);
            float t = d.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp(Vector2.Dot(c - o, d) / d.sqrMagnitude, 0f, reach);
            return (o + d * t - c).magnitude <= radius;
        }
    }
}
