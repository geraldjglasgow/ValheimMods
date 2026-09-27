using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The pieces touching a piece, by the game's own connection rule (<c>WearNTear.SetupColliders</c> and
    /// <c>UpdateSupport</c>, which uses it to find the pieces around a new one): every solid collider of the piece as a
    /// box, a box collider oriented with its transform and any other collider its world bounds, grown by 0.3 m in every
    /// dimension (0.15 m on each side), overlapped with the piece layers; a hit that is a trigger or sits on a rigidbody
    /// (ships, carts) does not count, and the hit's <c>WearNTear</c> is the neighbour. Two deviations: the boxes are
    /// measured fresh from the active, enabled colliders only (a disabled collider's bounds lie at the world origin, and
    /// the game's cached boxes never follow a moved piece), and the layers add <c>piece_nonsolid</c>, which the hammer's
    /// repair ray also reaches. Nearest first, each neighbour once, the piece itself excluded.
    /// </summary>
    public static class RepairNeighbours
    {
        /// <summary>The game's margin: every box grows by this much in each dimension before the overlap.</summary>
        public const float Margin = 0.3f;

        private static readonly Collider[] hits = new Collider[256];
        private static readonly List<Collider> own = new List<Collider>();
        private static int mask;

        public static List<WearNTear> Around(WearNTear piece)
        {
            HashSet<WearNTear> found = new HashSet<WearNTear>();
            piece.GetComponentsInChildren(false, own);
            foreach (Collider collider in own)
                if (collider.enabled && Solid(collider))
                    Overlap(piece, collider, found);
            own.Clear();
            List<WearNTear> near = new List<WearNTear>(found);
            Vector3 centre = piece.transform.position;
            near.Sort((a, b) => Distance(a, centre).CompareTo(Distance(b, centre)));
            return near;
        }

        private static void Overlap(WearNTear piece, Collider collider, HashSet<WearNTear> found)
        {
            Box(collider, out Vector3 centre, out Quaternion rotation, out Vector3 half);
            int count = Physics.OverlapBoxNonAlloc(centre, half, hits, rotation, Mask(), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!Solid(hits[i]))
                    continue;
                WearNTear other = hits[i].GetComponentInParent<WearNTear>();
                if (other != null && other != piece)
                    found.Add(other);
            }
        }

        /// <summary>The game's box for one collider: centre, rotation and half of the size plus the margin.</summary>
        private static void Box(Collider collider, out Vector3 centre, out Quaternion rotation, out Vector3 half)
        {
            Vector3 size;
            if (collider is BoxCollider box)
            {
                Transform transform = box.transform;
                rotation = transform.rotation;
                centre = transform.position + transform.TransformVector(box.center);
                size = Vector3.Scale(transform.lossyScale, box.size);
            }
            else
            {
                rotation = Quaternion.identity;
                centre = collider.bounds.center;
                size = collider.bounds.size;
            }
            half = (Abs(size) + Vector3.one * Margin) * 0.5f;
        }

        private static bool Solid(Collider collider)
        {
            return !collider.isTrigger && collider.attachedRigidbody == null;
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static float Distance(WearNTear piece, Vector3 centre) => (piece.transform.position - centre).sqrMagnitude;

        /// <summary>The game's support layers without terrain (no piece lives there), plus the non-solid piece layer.</summary>
        private static int Mask()
        {
            if (mask == 0)
                mask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid", "Default_small");
            return mask;
        }
    }
}
