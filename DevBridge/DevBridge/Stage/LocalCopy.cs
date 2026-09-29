using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Stage
{
    /// <summary>
    /// Copies that exist on this machine only and are never networked. A copy is made on the inactive bench, so nothing
    /// in it wakes; it is stripped there, then woken with network views disabled (<see cref="ZNetView.m_forceDisableInit"/>),
    /// the way the game makes its own local copies (the build ghost, the character preview): a view that wakes under it
    /// removes itself and no ZDO is made. Never "ghost init": a ghost view still registers a ZDO, and ZNetScene then
    /// spawns a full networked copy of it on every peer.
    /// </summary>
    internal static class LocalCopy
    {
        // The game's scripts a still copy keeps because they only draw or sound: equipment on a creature's bones, light
        // fading and flicker, sound players. The network view stays only to remove itself as it wakes, since the kept
        // scripts read it (as the game's own local copies do).
        private static readonly Type[] DrawingScripts = { typeof(ZNetView), typeof(VisEquipment), typeof(LightLod), typeof(LightFlicker), typeof(ZSFX) };

        /// <summary>
        /// A still copy: what the prefab looks like and nothing else. The creature, AI, piece, item and network scripts,
        /// the bodies, joints and colliders go before it wakes, so it neither moves, thinks, falls, blocks nor syncs.
        /// Its animators keep their controller, play from the default state and fire no animation events.
        /// </summary>
        internal static GameObject Still(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject copy = OnBench(prefab, position, rotation);
            Purge(copy.GetComponentsInChildren<MonoBehaviour>(true).Where(b => b != null && !Drawing(b)));
            Purge(copy.GetComponentsInChildren<Joint>(true));
            Park(Purge(copy.GetComponentsInChildren<Rigidbody>(true)));
            Park(Purge(copy.GetComponentsInChildren<Collider>(true)));
            foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true)) Hold(animator);
            Wake(copy);
            foreach (Animator animator in copy.GetComponentsInChildren<Animator>().Where(a => a.isActiveAndEnabled && a.runtimeAnimatorController))
                animator.Update(0f); // posed now, so the row measures the first frame's shape
            return copy;
        }

        /// <summary>
        /// A live effect or sound copy, as the LocalEffects library makes them: everything that plays stays, what could
        /// hurt, fly or sync goes (area damage, projectiles, transform sync) and colliders are off. `prepare` runs on
        /// the bench, before it wakes (to swap its sound's clips, say).
        /// </summary>
        internal static GameObject Harmless(GameObject prefab, Vector3 position, Quaternion rotation, Action<GameObject> prepare = null)
        {
            GameObject copy = OnBench(prefab, position, rotation);
            Purge(copy.GetComponentsInChildren<Aoe>(true));
            Purge(copy.GetComponentsInChildren<Projectile>(true));
            Purge(copy.GetComponentsInChildren<ZSyncTransform>(true));
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            prepare?.Invoke(copy);
            Wake(copy);
            return copy;
        }

        private static GameObject OnBench(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject copy = Object.Instantiate(prefab, StageRoot.Bench, false);
            copy.name = prefab.name;
            copy.transform.SetPositionAndRotation(position, rotation);
            return copy;
        }

        /// <summary>Off the bench and into the world: everything in it wakes here, with network views disabled.</summary>
        private static void Wake(GameObject copy)
        {
            bool was = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                copy.transform.SetParent(StageRoot.Root, true);
            }
            finally
            {
                ZNetView.m_forceDisableInit = was;
            }
        }

        private static bool Drawing(MonoBehaviour behaviour) => DrawingScripts.Any(type => type.IsInstanceOfType(behaviour));

        // No events (their receivers are gone), no root motion (nothing takes it up, so the model would walk out of its
        // place), animated every frame whether seen or not, and no warnings for parameters the game's code would set.
        private static void Hold(Animator animator)
        {
            animator.fireEvents = false;
            animator.applyRootMotion = false;
            animator.logWarnings = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        /// <summary>
        /// Removes the components, those that others on the same object require last (a script's RequireComponent);
        /// returns any that something kept still requires.
        /// </summary>
        internal static List<Component> Purge(IEnumerable<Component> parts)
        {
            List<Component> doomed = parts.Where(p => p).ToList();
            for (int left = -1; doomed.Count > 0 && doomed.Count != left; doomed = doomed.Where(p => p).ToList())
            {
                left = doomed.Count;
                foreach (Component part in doomed.Where(p => !Needed(p)).ToList()) Object.DestroyImmediate(part);
            }
            return doomed;
        }

        private static bool Needed(Component part) =>
            part.GetComponents<Component>().Any(other => other && other != part && Requires(other.GetType(), part.GetType()));

        private static bool Requires(Type holder, Type part) =>
            holder.GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>()
                .Any(r => Fits(r.m_Type0, part) || Fits(r.m_Type1, part) || Fits(r.m_Type2, part));

        private static bool Fits(Type required, Type part) => required != null && required.IsAssignableFrom(part);

        /// <summary>A body or collider a kept script needs stays, switched off.</summary>
        private static void Park(List<Component> kept)
        {
            foreach (Component part in kept)
            {
                if (part is Collider collider) collider.enabled = false;
                if (part is Rigidbody body)
                {
                    body.isKinematic = true;
                    body.detectCollisions = false;
                }
            }
        }
    }
}
