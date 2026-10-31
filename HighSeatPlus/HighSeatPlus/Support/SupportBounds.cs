using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// Where an object's colliders are, as far as the game's support search could see them: the world bounds of its
    /// active, enabled colliders that are not triggers, have no rigidbody and sit on one of the layers the search uses
    /// (the same names WearNTear gives its ray mask). Used by the hooks that tell <see cref="SupportCells"/> where
    /// something changed. One reused list, so a call allocates nothing.
    /// </summary>
    internal static class SupportBounds
    {
        private static readonly List<Collider> Colliders = new List<Collider>();
        private static int _mask;

        /// <summary>True with <paramref name="bounds"/> set when <paramref name="root"/> has any such collider.</summary>
        public static bool Of(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            root.GetComponentsInChildren(false, Colliders);
            foreach (Collider collider in Colliders)
            {
                if (!Seen(collider))
                {
                    continue;
                }
                if (any)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                else
                {
                    bounds = collider.bounds;
                    any = true;
                }
            }
            Colliders.Clear();
            return any;
        }

        // A collider the support search would meet and not skip.
        private static bool Seen(Collider collider)
        {
            if (_mask == 0)
            {
                _mask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");
            }
            return collider.enabled && !collider.isTrigger && collider.attachedRigidbody == null &&
                (_mask & (1 << collider.gameObject.layer)) != 0;
        }
    }
}
