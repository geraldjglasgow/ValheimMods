using UnityEngine;

namespace LocalEffects
{
    /// <summary>
    /// How a local copy is made and made harmless: instantiated with network views disabled, its gameplay parts
    /// removed and its colliders off, then thinned to a density.
    /// </summary>
    internal static class CloneParts
    {
        // Instantiated with network views disabled, the way the game makes its own local-only copies (the build ghost, an
        // item picked up into the inventory): the view removes itself as it wakes and no ZDO is made. Not "ghost init" -
        // a ghost view still registers a real ZDO (that is how the world generator makes objects nobody is near yet), and
        // ZNetScene spawns every ZDO near a player that has no object yet as a full networked copy a frame later, here
        // and on every peer: a second, unscaled copy of each effect, and of a looping one with no timer, a lasting cloud.
        public static GameObject Instantiate(GameObject prefab, Vector3 position)
        {
            bool was = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            try
            {
                return Object.Instantiate(prefab, position, Quaternion.identity);
            }
            finally
            {
                ZNetView.m_forceDisableInit = was;
            }
        }

        /// <summary>Removes what could hurt, fly or sync (area damage, projectile, transform sync, network view).</summary>
        public static void Strip(GameObject clone, bool endless)
        {
            Kill(clone.GetComponentsInChildren<Aoe>(true));
            Kill(clone.GetComponentsInChildren<Projectile>(true));
            Kill(clone.GetComponentsInChildren<ZSyncTransform>(true));
            Kill(clone.GetComponentsInChildren<ZNetView>(true));
            foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
            if (endless)
            {
                Kill(clone.GetComponentsInChildren<TimedDestruction>(true));
            }
        }

        /// <summary>Fewer particles and dimmer lights, in proportion to the density.</summary>
        public static void Thin(GameObject clone, float density)
        {
            foreach (ParticleSystem system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTimeMultiplier *= density;
                emission.rateOverDistanceMultiplier *= density;
            }
            foreach (Light light in clone.GetComponentsInChildren<Light>(true))
            {
                light.intensity *= density;
            }
        }

        /// <summary>Every particle system scales with the root, not with its own transform alone.</summary>
        public static void FollowRoot(GameObject clone)
        {
            foreach (ParticleSystem system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        private static void Kill(Component[] components)
        {
            foreach (Component component in components)
            {
                Object.Destroy(component);
            }
        }
    }
}
