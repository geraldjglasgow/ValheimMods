using System;
using BundlePrefabs;
using EliteCrafting.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The essence in the Rune Table's bowl looks like a cloud chamber (user request 2026-10-08: "can we have the essence
    /// have kinda of like a cloudchamber effect above it?"): faint vapour over the essence and rising off it, thin cyan
    /// tracks appearing at random (straight, thick, curly, forked) in a dome up to 0.4 m over it ("needs to extend further
    /// up out of the bowl") and fading, droplets falling through it. Cyan, the glyphs' colour, drawn over the pale essence
    /// rather than added to it ("hard to notice because its the same colors as the white"). An endless loop from the embedded bundle
    /// <c>ecf_tablefx</c> (ValheimAssets <c>Assets/Effects/ecf_essence_chamber</c>), dressed in the game's particle shaders
    /// (<c>BundleEffects</c>), authored for the full bowl's surface. <see cref="TableBowl"/> puts it on the essence and
    /// <see cref="Fit"/> sizes it to the surface: smaller and with fewer tracks (the same number per square metre) while
    /// the bowl is low. Local only, never networked. Without the bundle the essence shows without it.
    /// </summary>
    internal static class BowlChamber
    {
        private const string Effect = "ecf_essence_chamber";

        // The essence's radius the effect is authored for: the full bowl's surface (TableBowl: 0.231 m x its margin).
        private const float AuthoredRadius = 0.217f;

        private static GameObject? _effect;
        private static bool _tried;

        public static GameObject? Make(Transform bowl)
        {
            GameObject? template = Prepared();
            return template != null ? Object.Instantiate(template, bowl, false) : null;
        }

        /// <summary>Scaled to an essence surface of this radius, its emission thinned by the surface's share of the full one.</summary>
        public static void Fit(GameObject chamber, float radius)
        {
            float scale = radius / AuthoredRadius;
            chamber.transform.localScale = Vector3.one * scale;
            ParticleSystem[] systems = chamber.GetComponentsInChildren<ParticleSystem>(true);
            ParticleSystem[] authored = _effect!.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length && i < authored.Length; i++)
            {
                Thin(systems[i].emission, authored[i].emission, scale * scale);
            }
        }

        // The tracks come in chance bursts, the haze and drizzle at a rate: both scaled.
        private static void Thin(ParticleSystem.EmissionModule emission, ParticleSystem.EmissionModule authored, float share)
        {
            emission.rateOverTimeMultiplier = authored.rateOverTimeMultiplier * share;
            for (int i = 0; i < emission.burstCount; i++)
            {
                ParticleSystem.Burst burst = emission.GetBurst(i);
                burst.probability = authored.GetBurst(i).probability * share;
                emission.SetBurst(i, burst);
            }
        }

        private static GameObject? Prepared()
        {
            if (_tried || ZNetScene.instance == null)
            {
                return _effect;
            }
            _tried = true;
            try
            {
                _effect = BundleEffects.Prepare(EmbeddedBundle.Load(typeof(BowlChamber).Assembly, TableVortex.Bundle), Effect);
            }
            catch (Exception e)
            {
                Log.Warn($"the Rune Table's essence chamber did not load, the essence shows without it: {e.Message}");
            }
            return _effect;
        }
    }
}
