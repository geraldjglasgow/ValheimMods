using EliteCreaturesReborn.Config;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Visuals
{
    /// <summary>
    /// Clones a resolved vanilla effect prefab as a purely local, cosmetic object: never networked, never a damage
    /// source. Its network view is disabled before it wakes, so the clone never registers with the network at all, and
    /// every gameplay component (its Aoe, its ZNetView, its transform sync) is stripped on spawn, so the clone can only
    /// ever be seen, never felt - which is what lets a player turn effects down to nothing without changing a single
    /// point of damage. Density from the per-player setting thins the particles; zero density spawns nothing.
    /// A sound clone is the same stripped, local copy, and plays whatever the density.
    /// </summary>
    public static class CosmeticClone
    {
        /// <summary>Spawns the effect under a parent (so it dies with it), stripped and thinned; null when off or absent.</summary>
        public static GameObject? Spawn(GameObject? prefab, Transform parent, Vector3 position, bool endless)
        {
            float density = Density();
            if (prefab == null || density <= 0f)
            {
                return null;
            }
            GameObject clone = Instantiate(prefab, position);
            clone.transform.SetParent(parent, worldPositionStays: true);
            Strip(clone, endless);
            Thin(clone, density);
            return clone;
        }

        /// <summary>The authored radius of a typical vanilla ground effect; one-shot flashes scale from this to match.</summary>
        private const float BaselineRadius = 4f;

        /// <summary>A free-standing one-shot effect, scaled to a radius, that cleans itself up; used for brief bursts.</summary>
        public static void Flash(GameObject? prefab, Vector3 position, float radius) => OneShot(prefab, position, radius);

        /// <summary>
        /// A one-shot burst like <see cref="Flash"/>, drawn whole at <paramref name="scale"/> times its radius-matched
        /// size. Most vanilla effects scale each particle system by its own transform alone ("Local" scaling), so scaling
        /// the root - all Flash does - resizes only the root system and leaves the child systems at full size; here every
        /// system follows the root, so the whole burst takes the size.
        /// </summary>
        public static void FlashWhole(GameObject? prefab, Vector3 position, float radius, float scale)
        {
            GameObject? clone = OneShot(prefab, position, radius);
            if (clone == null)
            {
                return;
            }
            FollowRoot(clone);
            clone.transform.localScale *= scale;
        }

        private static GameObject? OneShot(GameObject? prefab, Vector3 position, float radius)
        {
            float density = Density();
            if (prefab == null || density <= 0f)
            {
                return null;
            }
            GameObject clone = Instantiate(prefab, position);
            Strip(clone, endless: false);
            Thin(clone, density);
            clone.transform.localScale *= Mathf.Max(radius, 0.01f) / BaselineRadius;
            Object.Destroy(clone, Mathf.Max(radius, 3f));
            return clone;
        }

        private static void FollowRoot(GameObject clone)
        {
            foreach (ParticleSystem system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        /// <summary>The longest a one-shot sound clone may live, should its own timer be missing.</summary>
        private const float SoundLife = 10f;

        /// <summary>
        /// A one-shot sound at a point: the prefab's own sound player plays it as it wakes, and its own timer removes it.
        /// Not thinned by the effect density setting, which governs what is seen, not what is heard.
        /// </summary>
        public static void Sound(GameObject? prefab, Vector3 position)
        {
            if (prefab == null)
            {
                return;
            }
            GameObject clone = Instantiate(prefab, position);
            Strip(clone, endless: false);
            Object.Destroy(clone, SoundLife);
        }

        // Instantiated with network views disabled, the way the game makes its own local-only copies (the build ghost, an
        // item picked up into the inventory): the view removes itself as it wakes and no ZDO is made. Not "ghost init" -
        // a ghost view still registers a real ZDO (that is how the world generator makes objects nobody is near yet), and
        // ZNetScene spawns every ZDO near a player that has no object yet as a full networked copy a frame later, here
        // and on every peer: a second, unscaled copy of each effect, and of a looping one with no timer, a lasting cloud.
        private static GameObject Instantiate(GameObject prefab, Vector3 position)
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

        private static void Strip(GameObject clone, bool endless)
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

        private static void Kill(Component[] components)
        {
            foreach (Component component in components)
            {
                Object.Destroy(component);
            }
        }

        private static void Thin(GameObject clone, float density)
        {
            Guard.Run("CosmeticClone.Thin", () => ThinParts(clone, density));
        }

        private static void ThinParts(GameObject clone, float density)
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

        private static float Density() => Mathf.Clamp01(Configuration.EffectDensity.Value);
    }
}
