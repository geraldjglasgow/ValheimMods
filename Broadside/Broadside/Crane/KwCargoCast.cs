using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How far a hook or a crate can go straight down before it meets something solid (the ground, a floor, a deck, a
    /// rock), cast from a little above it so a surface it already touches still counts. Its own colliders, and the
    /// crane's, are passed over, as are surfaces a cast starts inside (what the hook was swung into sideways). Also the
    /// water's level under a point.
    /// </summary>
    public static class KwCargoCast
    {
        public const float Far = 500f;
        private const float Start = 0.3f, Radius = 0.12f;
        private static readonly RaycastHit[] hits = new RaycastHit[32];

        /// <summary>The free drop under a hook at this point.</summary>
        public static float FreeBelow(Vector3 at, Transform crane)
        {
            int count = Physics.SphereCastNonAlloc(at + Vector3.up * Start, Radius, Vector3.down, hits, Far, KwCargo.Solid, QueryTriggerInteraction.Ignore);
            return Nearest(count, crane, null) - Start;
        }

        /// <summary>The free drop under a box (centre, half sizes, turn), passing over the colliders of <paramref name="own"/> and <paramref name="crane"/>.</summary>
        public static float FreeBelow(Vector3 centre, Vector3 half, Quaternion turn, Transform own, Transform? crane)
        {
            int count = Physics.BoxCastNonAlloc(centre + Vector3.up * Start, half, Vector3.down, hits, turn, Far, KwCargo.Solid, QueryTriggerInteraction.Ignore);
            return Nearest(count, own, crane) - Start;
        }

        private static float Nearest(int count, Transform own, Transform? other)
        {
            float best = Far;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                Collider found = hit.collider;
                bool inside = hit.distance <= 0f && hit.point == Vector3.zero;
                if (found == null || inside || found.transform.IsChildOf(own) || other != null && found.transform.IsChildOf(other))
                    continue;
                best = Mathf.Min(best, hit.distance);
            }
            return best;
        }

        /// <summary>The water's surface under a point (calm, or with its waves), or null where there is no water.</summary>
        public static float? Water(Vector3 at, float waves)
        {
            float level = Floating.GetLiquidLevel(new Vector3(at.x, ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f, at.z), waves, LiquidType.Water);
            return level > -1000f ? level : (float?)null;
        }
    }
}
