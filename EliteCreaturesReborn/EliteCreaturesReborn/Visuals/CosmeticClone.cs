using EliteCreaturesReborn.Config;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Visuals
{
    /// <summary>
    /// Clones a resolved vanilla effect prefab as a purely local, cosmetic object: never networked, never a damage
    /// source. Every gameplay component (its Aoe, its ZNetView, its transform sync) is stripped on spawn, so the clone
    /// can only ever be seen, never felt - which is what lets a player turn effects down to nothing without changing a
    /// single point of damage. Density from the per-player setting thins the particles; zero density spawns nothing.
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
        public static void Flash(GameObject? prefab, Vector3 position, float radius)
        {
            float density = Density();
            if (prefab == null || density <= 0f)
            {
                return;
            }
            GameObject clone = Instantiate(prefab, position);
            Strip(clone, endless: false);
            Thin(clone, density);
            clone.transform.localScale *= Mathf.Max(radius, 0.01f) / BaselineRadius;
            Object.Destroy(clone, Mathf.Max(radius, 3f));
        }

        private static GameObject Instantiate(GameObject prefab, Vector3 position)
        {
            ZNetView.StartGhostInit();
            GameObject clone = Object.Instantiate(prefab, position, Quaternion.identity);
            ZNetView.FinishGhostInit();
            return clone;
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
