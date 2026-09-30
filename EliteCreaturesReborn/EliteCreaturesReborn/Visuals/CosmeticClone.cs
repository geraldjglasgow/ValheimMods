using EliteCreaturesReborn.Config;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Visuals
{
    /// <summary>
    /// Clones a resolved vanilla effect prefab as a purely local, cosmetic object: never networked, never a damage
    /// source (the LocalEffects library's <see cref="LocalEffect"/>). Density from the per-player setting thins the
    /// particles; zero density spawns nothing - which is what lets a player turn effects down to nothing without changing
    /// a single point of damage. A sound clone is the same stripped, local copy, and plays whatever the density.
    /// </summary>
    public static class CosmeticClone
    {
        /// <summary>Spawns the effect under a parent (so it dies with it), stripped and thinned; null when off or absent.</summary>
        public static GameObject? Spawn(GameObject? prefab, Transform parent, Vector3 position, bool endless) =>
            LocalEffect.Attach(prefab, parent, position, endless, Density());

        /// <summary>A free-standing one-shot effect, scaled to a radius, that cleans itself up; used for brief bursts.</summary>
        public static void Flash(GameObject? prefab, Vector3 position, float radius) =>
            LocalEffect.Flash(prefab, position, radius, Density());

        /// <summary>A one-shot burst like <see cref="Flash"/>, drawn whole at <paramref name="scale"/> times its size.</summary>
        public static void FlashWhole(GameObject? prefab, Vector3 position, float radius, float scale) =>
            LocalEffect.FlashWhole(prefab, position, radius, scale, Density());

        /// <summary>A one-shot sound at a point; not thinned by the effect density setting.</summary>
        public static void Sound(GameObject? prefab, Vector3 position) => LocalEffect.Sound(prefab, position);

        /// <summary>An effect's own sound alone at <paramref name="volume"/> of its loudness, nothing of it drawn; not
        /// thinned by the effect density setting either.</summary>
        public static void SoundOnly(GameObject? prefab, Vector3 position, float volume) =>
            LocalEffect.SoundOnly(prefab, position, volume);

        private static float Density() => Mathf.Clamp01(Configuration.EffectDensity.Value);
    }
}
