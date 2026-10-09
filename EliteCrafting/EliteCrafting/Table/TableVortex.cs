using System;
using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCrafting.Core;
using EliteCrafting.Display;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The Rune Table's vortex: motes and wisps spiralling up from the slab and dissipating above the table, from the
    /// embedded bundle <c>ecf_tablefx</c> (ValheimAssets <c>Assets/Effects/ecf_rune_vortex</c>), dressed in the game's
    /// particle shaders (<c>BundleEffects</c>) and recoloured per stone colour (<see cref="ParticleRetint"/>, one copy kept per
    /// colour). It plays once on its own; the instance is destroyed after <see cref="Life"/>. Local only, never networked.
    /// Without the bundle nothing shows (the sound still plays).
    /// </summary>
    internal static class TableVortex
    {
        internal const string Bundle = "ecf_tablefx";
        private const string Effect = "ecf_rune_vortex";
        private const float Life = 4f;

        // Lower, and plainer at its foot (user 2026-10-08: "it needs to happen ... on the center symbol that is glowing
        // blue"): seen from the player's camera the authored 1.1-1.35 m climb showed over the rack behind the slab, and its
        // faint start was lost on the glowing glyph. The climb is halved (the spiral keeps its turns and timing) and the
        // ring and glow on the glyph get these alphas (authored 0.35 and 0.22).
        private const float Rise = 0.5f;
        private static readonly Dictionary<string, float> FootAlpha = new Dictionary<string, float>
        {
            ["ring"] = 0.8f,
            ["puff"] = 0.5f,
        };

        private static readonly Dictionary<int, GameObject> Templates = new Dictionary<int, GameObject>();
        private static GameObject? _effect;
        private static bool _tried;

        public static void Spawn(Vector3 at, Color colour)
        {
            GameObject? template = Template(colour);
            if (template != null)
            {
                Object.Destroy(Object.Instantiate(template, at, Quaternion.identity), Life);
            }
        }

        private static GameObject? Template(Color32 colour)
        {
            int key = (colour.r << 16) | (colour.g << 8) | colour.b;
            if (Templates.TryGetValue(key, out GameObject template) && template != null)
            {
                return template;
            }
            GameObject? effect = Prepared();
            if (effect == null)
            {
                return null;
            }
            template = PrefabBench.Copy(effect, $"{Effect}_{key:X6}");
            ParticleRetint.Tint(template, colour);
            Templates[key] = template;
            return template;
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
                _effect = BundleEffects.Prepare(EmbeddedBundle.Load(typeof(TableVortex).Assembly, Bundle), Effect);
                Settle(_effect);
            }
            catch (Exception e)
            {
                Log.Warn($"the Rune Table's vortex did not load, its uses only sound: {e.Message}");
            }
            return _effect;
        }

        // Once, before any colour copy: the climb halved, the ring and glow at the foot stronger (see Rise), no near fade.
        private static void Settle(GameObject? effect)
        {
            foreach (ParticleSystem system in effect != null ? effect.GetComponentsInChildren<ParticleSystem>(true) : new ParticleSystem[0])
            {
                Unfade(system.GetComponent<ParticleSystemRenderer>());
                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                if (velocity.enabled)
                {
                    velocity.yMultiplier *= Rise;
                }
                if (FootAlpha.TryGetValue(system.name, out float alpha))
                {
                    ParticleSystem.MainModule main = system.main;
                    Color start = main.startColor.color;
                    main.startColor = new Color(start.r, start.g, start.b, alpha);
                }
            }
        }

        // The game's Custom/Particle (Unlit) fades a particle out within _SoftNearFade metres of whatever lies behind it,
        // _SoftParticles off or not (found 2026-10-08 with DevBridge's studio). Authored at 0.5, the vortex's foot on the
        // slab never showed, only motes half a metre up, over the rack ("it feels like its starting under the table").
        private static void Unfade(ParticleSystemRenderer? renderer)
        {
            if (renderer == null)
            {
                return;
            }
            foreach (Material material in renderer.sharedMaterials.Append(renderer.trailMaterial))
            {
                if (material != null && material.HasProperty("_SoftNearFade"))
                {
                    material.SetFloat("_SoftNearFade", 0f);
                }
            }
        }
    }
}
