using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// A solid still copy (/place colliders=1): it keeps the bundle prefab's own non-trigger box, sphere, capsule and
    /// mesh colliders (the workshop's col_* parts), so the player stands on it and the game's pathfinding sees it. They
    /// stay static (no rigidbody), local and unsaved like the rest of the copy. Each keeps the layer it has in the
    /// prefab; one on Default goes on static_solid, the layer of the game's rocks and dungeon walls.
    /// </summary>
    internal static class Solid
    {
        internal static bool Keeps(Collider collider) =>
            !collider.isTrigger && (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider || collider is MeshCollider);

        /// <summary>The kept colliders off Default and onto static_solid, before the copy wakes.</summary>
        internal static void Lay(GameObject copy)
        {
            int solid = Mathf.Max(LayerMask.NameToLayer("static_solid"), 0);
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true).Where(Keeps))
                if (collider.gameObject.layer == 0) collider.gameObject.layer = solid;
        }

        /// <summary>The colliders that collide now (enabled, not triggers).</summary>
        internal static List<Collider> Of(GameObject root) =>
            root.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToList();

        /// <summary>
        /// Its colliders off while it is measured and stood in the row, so the ground found under it is never itself;
        /// on again when disposed.
        /// </summary>
        internal static IDisposable Aside(GameObject root)
        {
            List<Collider> off = Of(root);
            foreach (Collider collider in off) collider.enabled = false;
            return new Back(off);
        }

        private sealed class Back : IDisposable
        {
            private readonly List<Collider> colliders;

            internal Back(List<Collider> colliders) => this.colliders = colliders;

            public void Dispose()
            {
                foreach (Collider collider in colliders.Where(c => c)) collider.enabled = true;
            }
        }
    }
}
